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
        // Ahşap zemin (okul sahası): boyuna tahta şeritleri, şerit başına hafif ton farkı ve ince derz çizgisi.
        public static Texture2D WoodTex(Color baseC)
        {
            const int W = 64, H = 64; var t = new Texture2D(W, H, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * H]; var rnd = new System.Random(3); var tone = new float[8]; for (int i = 0; i < 8; i++) tone[i] = .92f + (float)rnd.NextDouble() * .14f;
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    int sh = x / 8; float k = tone[sh] * (1 + .03f * Mathf.Sin(y * .7f + sh * 3)); if (x % 8 == 0) k *= .8f;
                    px[y * W + x] = new Color(baseC.r * k, baseC.g * k, baseC.b * k, 1);
                }
            t.SetPixels32(px); t.Apply(true); return t;
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

        public static Color Hex(string h) { Color c; return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.magenta; }
    }
}
