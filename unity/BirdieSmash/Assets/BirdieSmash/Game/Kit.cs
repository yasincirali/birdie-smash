using System.Collections.Generic;
// Ortak yardımcılar: koordinat çevirisi, malzeme, prosedürel doku ve mesh.
// Neden kodla: demo sahnesi tek satır sahne dosyası olmadan, her Unity sürümünde aynı kurulsun;
// sanatçı varlıkları geldikçe bu fabrikalar yerini prefab'lara bırakır.
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public static class Kit
    {
        // Oyun koordinatı (x eni, y boyu, z yükseklik) → Unity dünyası. Ben -Z tarafındayım, file Z=0.
        public static Vector3 W(float x, float y, float z) { return new Vector3(x - Court.MID, z, Court.NET - y); }
        public static Vector2 FromWorld(Vector3 p) { return new Vector2(p.x + Court.MID, Court.NET - p.z); }

        static Shader std, spr;
        public static Material Lit(Color c, float smooth = .25f)
        {
            if (std == null) std = Shader.Find("Standard");
            var m = new Material(std); m.color = c; m.SetFloat("_Glossiness", smooth); return m;
        }
        public static Material LitTex(Color c, Texture t, float smooth = .25f) { var m = Lit(c, smooth); m.mainTexture = t; return m; }
        // Standard: teğet uzayı normal haritası (doku içe aktarımda "Normal map" türünde olmalı: KarakterImport) ve
        // metal/parlaklık haritası (R metal, A smoothness). Anahtar kelimeyi açmadan doku atamak hiçbir şey çizmez.
        public static void Bump(Material m, Texture n, float scale = 1f)
        {
            if (n == null) return; m.SetTexture("_BumpMap", n); m.SetFloat("_BumpScale", scale); m.EnableKeyword("_NORMALMAP");
        }
        public static void Gloss(Material m, Texture mg, float scale = 1f)
        {
            if (mg == null) return; m.SetTexture("_MetallicGlossMap", mg); m.SetFloat("_GlossMapScale", scale); m.EnableKeyword("_METALLICGLOSSMAP");
        }
        // Ahşap zemin (okul sahası): boyuna tahta şeritleri, şerit başına hafif ton farkı ve ince derz çizgisi.
        // 2026-10-27 performans turu 2: doku 1×9 tekrarla kortun boyunca yatık açıda görülür; bilinear'da mip sınırı uzak
        // yarıda yatay bir bant gibi kayıyor, derzler pırpır ediyordu. Trilinear + 4× anizotropi (QualitySettings
        // anisotropicFiltering Kalite'de "doku başına" açık); kort çizgileri doku değil ağ, onları MSAA yumuşatır.
        public static Texture2D WoodTex(Color baseC)
        {
            const int W = 64, H = 64; var t = new Texture2D(W, H, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 4;
            var px = new Color32[W * H]; var rnd = new System.Random(3); var tone = new float[8]; for (int i = 0; i < 8; i++) tone[i] = .92f + (float)rnd.NextDouble() * .14f;
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    int sh = x / 8; float k = tone[sh] * (1 + .03f * Mathf.Sin(y * .7f + sh * 3)); if (x % 8 == 0) k *= .8f;
                    px[y * W + x] = new Color(baseC.r * k, baseC.g * k, baseC.b * k, 1);
                }
            t.SetPixels32(px); t.Apply(true, true); return t;
        }

        // Saydam, iki yüzlü, köşe rengi çarpan (file, iz, gölge): Sprites/Default her yapıda bulunur.
        public static Material Clear(Color c, Texture tex = null)
        {
            if (spr == null) spr = Shader.Find("Sprites/Default");
            var m = new Material(spr); m.color = c; if (tex != null) m.mainTexture = tex; return m;
        }

        public static Texture2D RadialTex(int n = 64)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - r); a = a * a * (3 - 2 * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(); return t;
        }

        // File gözü: 1 px çizgili kare desen; mesh UV'si 4 cm'de bir tekrarlar.
        public static Texture2D NetTex()
        {
            const int n = 16; var t = new Texture2D(n, n, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 4;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    bool line = x == 0 || y == 0 || x == n - 1 || y == n - 1;
                    px[y * n + x] = line ? new Color32(255, 255, 255, 200) : new Color32(255, 255, 255, 0);
                }
            t.SetPixels32(px); t.Apply(true); return t;
        }

        public static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(t); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }

        public static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material m, bool shadows = false)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        // Işıksız düz renk (tüy kenarı, iplik bağı çizgileri): Unlit/Color derinlik yazar, ışıktan etkilenmez.
        static Shader unl, add;
        public static Material Unlit(Color c) { if (unl == null) unl = Shader.Find("Unlit/Color"); var m = new Material(unl ?? spr ?? Shader.Find("Sprites/Default")); m.color = c; return m; }
        // Eklemeli (HTML globalCompositeOperation='lighter'): iz ve hale. Yoksa Sprites/Default'a düşer.
        public static Material Additive(Color c, Texture t = null)
        {
            if (add == null) add = Shader.Find("Legacy Shaders/Particles/Additive");
            if (add == null) return Clear(c, t);
            var m = new Material(add); m.SetColor("_TintColor", c * .5f); if (t != null) m.mainTexture = t; return m;
        }
        // Kesikli çizgi dokusu (HTML setLineDash): u boyunca dolu/boş; LineRenderer textureMode=Tile ile tekrarlar.
        // LED pano dokusu (2026-11-05): 3×5 nokta yazı tipi, her harf 4 sütun; yanan nokta renkli, sönük nokta zeminden biraz açık
        // (gerçek LED panoda sönük diyot da seçilir). Nokta 2×2 piksel + 1 piksel ara, Point süzgeç: uzaktan bile keskin.
        static readonly Dictionary<char, string> LED_HARF = new Dictionary<char, string> {
            {'A',"010101111101101"},{'B',"110101110101110"},{'D',"110101101101110"},{'E',"111100110100111"},{'H',"101101111101101"},
            {'I',"111010010010111"},{'L',"100100100100111"},{'M',"101111111101101"},{'R',"110101110101101"},{'S',"011100010001110"},
            {'Y',"101101010010010"},{'0',"111101101101111"},{'3',"111001011001111"},{' ',"000000000000000"},
            // 2026-11-11 (Ar-Ge 07:05, 1): 5 Kas yazı tipi yalnız "BIRDIE SMASH" harflerini taşıyordu; 6 Kas canlı panolar "MATCH POINT"ı
            // "MAH I", "RALLY 15"i "RALLY" yazıyordu (bilinmeyen harf sessizce atlanır). Tam Latin alfabe + rakamlar + ! · -.
            {'C',"111100100100111"},{'F',"111100110100100"},{'G',"111100101101111"},{'J',"001001001101111"},{'K',"101101110101101"},
            {'N',"110101101101101"},{'O',"010101101101010"},{'P',"110101110100100"},{'Q',"010101101110011"},{'T',"111010010010010"},
            {'U',"101101101101111"},{'V',"101101101101010"},{'W',"101101111111101"},{'X',"101101010101101"},{'Z',"111001010100111"},
            {'1',"010110010010111"},{'2',"111001111100111"},{'4',"101101111001001"},{'5',"111100111001111"},{'6',"111100111101111"},
            {'7',"111001001010010"},{'8',"111101111101111"},{'9',"111101111001111"},{'!',"010010010000010"},{'·',"000000010000000"},
            {'-',"000000111000000"}};
        // Türkçe büyük harf ASCII karşılığına (ToUpperInvariant sonrası): 3×5 noktada noktalı/kuyruklu harf okunmaz.
        static char LedKarakter(char c)
        {
            c = char.ToUpperInvariant(c);
            switch (c) { case 'Ş': return 'S'; case 'İ': case 'I': return 'I'; case 'Ü': return 'U'; case 'Ö': return 'O'; case 'Ç': return 'C'; case 'Ğ': return 'G'; default: return c; }
        }
        public static Texture2D LedTex(string yazi, Color on, Color bg)
        {
            const int SUT = 50, SAT = 9, P = 3; var t = new Texture2D(SUT * P, SAT * P, TextureFormat.RGBA32, false); t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[t.width * t.height]; Color32 zemin = bg * .6f + Color.black * .4f; for (int i = 0; i < px.Length; i++) px[i] = zemin;   // Color32: doku başına ~65 KB yerine ~16 KB geçici dizi (performans incelemesi 2026-11-05)
            var lit = new bool[SUT, SAT]; int gen = yazi.Length * 4 - 1, x0 = Mathf.Max(0, (SUT - gen) / 2);
            for (int c = 0; c < yazi.Length; c++) { string g; if (!LED_HARF.TryGetValue(LedKarakter(yazi[c]), out g)) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("LED glifi yok: '" + yazi[c] + "' (" + yazi + ")");   // sessiz atlama yarım kelimeyi 6 gün sakladı
#endif
                    continue; }
                for (int r = 0; r < 5; r++) for (int q = 0; q < 3; q++) if (g[r * 3 + q] == '1') { int xx = x0 + c * 4 + q; if (xx < SUT) lit[xx, 2 + r] = true; } }
            Color32 sonuk = Color.Lerp(bg, on, .12f), yan = on;
            for (int a = 0; a < SUT; a++) for (int b = 0; b < SAT; b++) { Color32 c = lit[a, b] ? yan : sonuk; int yy = (SAT - 1 - b) * P;
                for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) px[(yy + j) * t.width + a * P + i] = c; }
            t.SetPixels32(px); t.Apply(); return t;
        }
        public static Texture2D DashTex(int on = 3, int off = 4)
        {
            int n = on + off; var t = new Texture2D(n * 4, 1, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Point;
            for (int i = 0; i < n * 4; i++) t.SetPixel(i, 0, i / 4 < on ? Color.white : new Color(1, 1, 1, 0)); t.Apply(); return t;
        }
        // Çember dokusu (IMGUI hale ve halkaları için): kalınlık ölçeğe göre piksel.
        public static Texture2D RingTex(int n, float kalin)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; var px = new Color32[n * n]; float R = n / 2f - 1;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                { float r = Mathf.Sqrt((x + .5f - n / 2f) * (x + .5f - n / 2f) + (y + .5f - n / 2f) * (y + .5f - n / 2f)); float a = Mathf.Clamp01(kalin / 2 - Mathf.Abs(r - (R - kalin / 2)) + .5f); px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255)); }
            t.SetPixels32(px); t.Apply(); return t;
        }
        // Kesik halka (2026-10-29, Ar-Ge 1): uzanma halesi yalnız turuncu rengiyle "düz halka"dan ayrılıyordu; renk körü oyuncu için
        // biçim de ayrışır (HIG: yalnız renkle anlam yok). parca = kesik sayısı; her parçanın %60'ı dolu, köşeler yumuşak.
        public static Texture2D RingKesikTex(int n, float kalin, int parca)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; var px = new Color32[n * n]; float R = n / 2f - 1, yay = n * Mathf.PI / parca;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = x + .5f - n / 2f, dy = y + .5f - n / 2f, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Clamp01(kalin / 2 - Mathf.Abs(r - (R - kalin / 2)) + .5f);
                    float f = Mathf.Repeat((Mathf.Atan2(dy, dx) + Mathf.PI) / (2 * Mathf.PI) * parca, 1), kenar = Mathf.Min(f, .6f - f) * yay;   // parça kenarına uzaklık (px)
                    a *= Mathf.Clamp01(kenar + .5f); px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(); return t;
        }
        // flat: yere yatık (iniş halkası, yer çizgisi); değilse kameraya dönük (iz, yükseklik çizgisi).
        public static LineRenderer Line(string name, Transform parent, Material m, bool loop = false, bool flat = false)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); var l = g.AddComponent<LineRenderer>(); l.sharedMaterial = m; l.loop = loop; l.useWorldSpace = true;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false; l.numCapVertices = 0;
            if (flat) { g.transform.rotation = Quaternion.Euler(90, 0, 0); l.alignment = LineAlignment.TransformZ; } else l.alignment = LineAlignment.View;
            return l;
        }
        public static void Ellipse(LineRenderer l, Vector3 c, float rx, float rz, int n = 48)
        {
            l.positionCount = n; for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; l.SetPosition(i, c + new Vector3(Mathf.Cos(a) * rx, 0, Mathf.Sin(a) * rz)); }
        }

        // D1 (2026-10-31): HUD her OnGUI'de (kare başına birkaç kez) aynı sabit dizileri ayrıştırıyordu. Sonuç dize başına önbellekte;
        // dizeler kaynakta sabit (palet), sözlük küçük kalır. Yalnız ana iş parçacığından çağrılır.
        static readonly System.Collections.Generic.Dictionary<string, Color> hexOnbellek = new System.Collections.Generic.Dictionary<string, Color>();
        public static Color Hex(string h)
        {
            Color c; if (h == null) return Color.magenta; if (hexOnbellek.TryGetValue(h, out c)) return c;
            if (!ColorUtility.TryParseHtmlString(h, out c)) c = Color.magenta; if (hexOnbellek.Count < 512) hexOnbellek[h] = c; return c;
        }
    }
}
