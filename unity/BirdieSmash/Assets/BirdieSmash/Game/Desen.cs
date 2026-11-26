// Forma deseni (2026-11-21, yasin: kıyafet katmanı, karar kartında "Önce C, sonra A"). Desen yalnız görünüştür, hiçbir değeri etkilemez.
// C: kumaşın görünüşü işlevinden ayrıldı. Kumaşın oyun etkisi Ayar.gear.kumas'tan gelmeye devam eder; görünüş Ayar.GORUNUM["desen"]:
//    "kumas" = giyilen kumaşın kendi görünüşü (varsayılan, eski kayıtlarda da bu), "duz" = desensiz, "k_<kumas>" = sahip olunan başka
//    bir kumaşın görünüşü. Kumaş görünüşü o kumaşa sahip olunca açılır (Dolap'ta kilitli kare "Atölye'de").
// A: kumaştan bağımsız desenler, seviyeyle açılır (renkler gibi ücretsiz; fiyat kararı dev thread'in).
// Doku kodla üretilir (paket boyutu sıfır), (desen, forma rengi) başına bir kez ve paylaşılır. Koordinatlar KarakterImport'un desen
// UV'si: u = 0 göğüs ortası … 1 sırt ortası (sağ/sol aynalı), v = 0 bel … 1 yaka. Aynalı eşleme yüzünden desenler simetriktir.
using System.Collections.Generic;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class Desen
    {
        const int W = 128, H = 128;
        static readonly Dictionary<string, Texture2D> onbellek = new Dictionary<string, Texture2D>();
        static readonly Color LACI = new Color(.12f, .16f, .29f), LIMON = new Color(.85f, 1f, .35f), BEYAZ = new Color(.96f, .97f, .96f);

        // Seçimi ve giyilen kumaşı tek desen kimliğine indirger ("kumas" → kumaşın görünüşü; klasik = düz).
        public static string Coz(string secim, string kumas)
        {
            if (string.IsNullOrEmpty(secim) || secim == "kumas") return string.IsNullOrEmpty(kumas) || kumas == "klasik" ? "duz" : "k_" + kumas;
            return secim == "k_klasik" ? "duz" : secim;
        }

        // Düz desen null döner: çağıran eski düz renk malzemesini kurar (bugünkü görünüş birebir).
        public static Texture2D Doku(string desen, Color c)
        {
            if (string.IsNullOrEmpty(desen) || desen == "duz") return null;
            string k = desen + "|" + ColorUtility.ToHtmlStringRGB(c); Texture2D t;
            if (onbellek.TryGetValue(k, out t) && t != null) return t;
            t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 2, name = "desen_" + desen };
            var px = new Color32[W * H]; bool acik = c.r * .3f + c.g * .59f + c.b * .11f > .55f; Color kar = acik ? LACI : BEYAZ, vur = acik ? LACI : LIMON;
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) px[y * W + x] = Renk(desen, (x + .5f) / W, (y + .5f) / H, c, kar, vur);
            t.SetPixels32(px); t.Apply(true, true); onbellek[k] = t; return t;
        }

        // 2026-11-23 inceleme (fikir 02:06 #1, takılma): Dolap'ta forma rengine her dokunuş, desen satırındaki ~10 önizlemeyi yeni
        // renkte 128² + mip olarak aynı karede üretiyordu ve hepsi ömür boyu önbellekte kalıyordu. Önizleme 44 pt karo içindir:
        // 64², mip yok, 2×2 alt örnekle (ince şerit 64'te kaybolmasın). Ayrı önbellek, yalnız son iki renk tutulur: yeni sayfa
        // kurulurken eski karolar hâlâ ağaçta olabilir, o yüzden bir önceki renk hemen yok edilmez, ondan eskisi edilir.
        // Figürün dokusu Doku()'dan (128 + mip) gelmeye devam eder.
        const int OW = 64;
        static readonly Dictionary<string, Texture2D> onizleme = new Dictionary<string, Texture2D>();
        static readonly List<string> onizlemeRenk = new List<string>();
        public static Texture2D Onizleme(string desen, Color c)
        {
            if (string.IsNullOrEmpty(desen) || desen == "duz") return null;
            string rk = ColorUtility.ToHtmlStringRGB(c), k = desen + "|" + rk; Texture2D t;
            if (onizleme.TryGetValue(k, out t) && t != null) return t;
            if (onizlemeRenk.Remove(rk)) onizlemeRenk.Add(rk);   // kullanım sırası: geri dönülen renk en yeni sayılır, atılmaz
            else
            {
                onizlemeRenk.Add(rk);
                while (onizlemeRenk.Count > 2)
                {
                    string eski = "|" + onizlemeRenk[0]; onizlemeRenk.RemoveAt(0); var sil = new List<string>();
                    foreach (var kv in onizleme) if (kv.Key.EndsWith(eski)) { if (kv.Value != null) Object.Destroy(kv.Value); sil.Add(kv.Key); }
                    foreach (var s in sil) onizleme.Remove(s);
                }
            }
            t = new Texture2D(OW, OW, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "desen_on_" + desen };
            var px = new Color32[OW * OW]; bool acik = c.r * .3f + c.g * .59f + c.b * .11f > .55f; Color kar = acik ? LACI : BEYAZ, vur = acik ? LACI : LIMON;
            for (int y = 0; y < OW; y++) for (int x = 0; x < OW; x++)
            {
                Color a = Renk(desen, (x + .25f) / OW, (y + .25f) / OW, c, kar, vur) + Renk(desen, (x + .75f) / OW, (y + .25f) / OW, c, kar, vur)
                        + Renk(desen, (x + .25f) / OW, (y + .75f) / OW, c, kar, vur) + Renk(desen, (x + .75f) / OW, (y + .75f) / OW, c, kar, vur);
                px[y * OW + x] = a * .25f;
            }
            t.SetPixels32(px); t.Apply(false, true); onizleme[k] = t; return t;
        }

        static Color Renk(string d, float u, float v, Color c, Color kar, Color vur)
        {
            switch (d)
            {
                // C: kumaş görünüşleri (eski Gerçekçi dokularının dili: hafif yan panel + delik, nefes çift şerit, esnek kuşak)
                case "k_hafif": return Mathf.Abs(u - .5f) < .12f ? (Delik(u, v, 9) ? Koyu(c, .6f) : Acik(c, .4f)) : c;
                case "k_nefes": return Mathf.Abs(u - .08f) < .025f || Mathf.Abs(u - .92f) < .025f ? (Delik(u, v, 12) ? Koyu(c, .55f) : Acik(c, .45f)) : c;
                case "k_esnek": { float b = Mathf.Abs(v - (.95f - .9f * u)); return u < .5f && b < .07f ? Koyu(c, .62f) : c; }   // yalnız önde (r240: çizgi sırtın altına iniyordu)
                // A: desenler
                case "yan_serit": { float s = Mathf.Abs(u - .5f); return s < .025f ? kar : s < .04f ? vur : c; }
                case "omuz": return v > .74f && Mathf.Abs(u - .5f) < .16f ? kar : c;
                case "v_kusak": { if (u > .48f) return c; float b = v - (1.02f - 1.15f * u); return Mathf.Abs(b) < .065f ? kar : b > .065f && b < .085f ? vur : c; }   // yalnız önde
                case "gogus": return v > .56f && v < .68f ? kar : v > .52f && v < .56f ? vur : c;
                case "cift_serit": return v > .26f && v < .31f || v > .36f && v < .41f ? kar : c;
                case "amblem": return u < .17f && v > .42f && v < .74f && Tuy(u / .17f, (v - .42f) / .32f) ? kar : c;   // r240: yakanın altında küçük kalıyordu, büyüdü ve indi
                case "degrade": return Color.Lerp(Koyu(c, .32f), Acik(c, .12f), Mathf.SmoothStep(0, 1, v));
            }
            return c;
        }
        static bool Delik(float u, float v, int n) { float a = u * n * 4 % 1, b = v * n * 3 % 1; return (a - .5f) * (a - .5f) + (b - .5f) * (b - .5f) < .06f; }
        static Color Koyu(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1); }
        static Color Acik(Color c, float k) { return Color.Lerp(c, Color.white, k); }
        // Tüy amblemi: ortada (aynalı eşlemede tek, simetrik) mantar + etek; x 0 = göğüs ortası, 1 = amblem kenarı; y 0 alt … 1 üst.
        static bool Tuy(float x, float y)
        {
            if (x * x + (y - .2f) * (y - .2f) / .5f < .09f) return true;                // mantar
            return y > .28f && y < .95f && x < .16f + .55f * (y - .28f);   // etek
        }
    }
}
