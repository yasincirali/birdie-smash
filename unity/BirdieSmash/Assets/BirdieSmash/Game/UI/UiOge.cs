// Menü yapı taşları (UI Toolkit, 2026-10-26): yazı, düğme, seçim çipi, kart, simge. Biçim Resources/ui/menu.uss'te,
// burada yalnız davranış ve yazı boyutu.
// Neden yazı boyutu kodda: USS'te em/rem yok (yalnız px ve %), Dynamic Type çarpanı (Ayar.MenuYaziK) stil dosyasından
// verilemiyor. Her yazı oluşturulurken taban boyut × K yazılır; K değişince menü baştan kurulur (yalnız o an, karede değil).
// Neden düğme Button değil VisualElement + Clickable: varsayılan temanın .unity-button kuralları (çerçeve, gri zemin,
// :hover) ile yarışmamak için; dokunma alanı (≥ 44 pt) ve basılı görünüm (:active) USS'te.
// Neden simgeler dokudan: ✓ ⚙ 🔒 gibi glifler varsayılan yazı tipinde olmayabilir (cihazda kare kutu görünür riski);
// simgeler açılışta bir kez koddan çizilir (Kit dokuları gibi), Image.tintColor ile boyanır. Seçili durum yalnız renkle
// verilmez: çipte ✓ simgesi, sekmede üst çizgi + kalın yazı.
using System;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        public static readonly Color LIMON = Kit.Hex("#d8ff5a"), CAMGOBEGI = Kit.Hex("#7ad7ff"), MERCAN = Kit.Hex("#ff7a6b"), ALTIN = Kit.Hex("#ffd23a"),
            SOLUK = Kit.Hex("#9dbab3"), KOYU = Kit.Hex("#0d1a12");

        Label Yazi(string t, float px, string cls = null)
        {
            var l = new Label(t); l.AddToClassList("yazi"); if (cls != null) l.AddToClassList(cls);
            l.style.fontSize = Mathf.Round(px * K); l.pickingMode = PickingMode.Ignore; return l;
        }
        // Ana düğme biçimi: üst satır kalın, isteğe bağlı alt satır küçük.
        VisualElement Dugme(string t, Action a, string cls = null, string alt = null, float px = 16)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); if (cls != null) d.AddToClassList(cls);
            var ic = new VisualElement(); ic.AddToClassList("dugme-ic"); ic.pickingMode = PickingMode.Ignore; d.Add(ic);
            ic.Add(Yazi(t, px, "dugme-yazi"));
            if (!string.IsNullOrEmpty(alt)) ic.Add(Yazi(alt, px * .78f, "dugme-alt"));
            if (a != null) d.AddManipulator(new Clickable(a));
            Erisim.Etiket(d, string.IsNullOrEmpty(alt) ? t : t + ", " + alt, "dugme", false, a == null);
            return d;
        }
        VisualElement Pasif(string t, string alt = null) { var d = Dugme(t, null, "dugme--pasif", alt); return d; }
        // Seçim çipi: seçili olan ✓ ile de okunur (renk körlüğü); dokununca hafif titreşim (HTML/IMGUI Chip ile aynı).
        VisualElement Cip(string t, bool secili, Action a, string alt = null)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("cip"); if (secili) d.AddToClassList("cip--secili");
            var ic = new VisualElement(); ic.AddToClassList("cip-ic"); ic.pickingMode = PickingMode.Ignore; d.Add(ic);
            if (secili) ic.Add(Ikon(Ikonlar.Tik, 16, Kit.Hex("#07161c")));
            var yz = new VisualElement(); yz.AddToClassList("cip-yazilar"); yz.pickingMode = PickingMode.Ignore; ic.Add(yz);
            yz.Add(Yazi(t, 14, "dugme-yazi")); if (!string.IsNullOrEmpty(alt)) yz.Add(Yazi(alt, 11.5f, "dugme-alt"));
            if (a != null) d.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); a(); }));
            Erisim.Etiket(d, string.IsNullOrEmpty(alt) ? t : t + ", " + alt, "dugme", secili, a == null);
            return d;
        }
        Image Ikon(Texture2D t, float pt, Color c)
        {
            var i = new Image { image = t, scaleMode = ScaleMode.ScaleToFit, tintColor = c }; i.AddToClassList("ikon");
            float s = Mathf.Round(pt * Mathf.Min(K, 1.6f)); i.style.width = s; i.style.height = s; i.pickingMode = PickingMode.Ignore; return i;
        }
        VisualElement Kart(string cls = null) { var k = new VisualElement(); k.AddToClassList("kart"); if (cls != null) k.AddToClassList(cls); return k; }
        // Yatay sıra; varsayılan sınıf "secim" (çip sırası: büyük yazıda alt alta dizilir), başka sıralar kendi sınıfını verir.
        VisualElement Satir(string cls = "secim") { var s = new VisualElement(); s.AddToClassList("satir"); if (cls != null) s.AddToClassList(cls); return s; }
        Label Grup(string t) { var l = Yazi(t, 13, "grup"); Erisim.Etiket(l, t, "baslik"); return l; }
        VisualElement Bosluk(float pt) { var b = new VisualElement(); b.style.height = pt; b.pickingMode = PickingMode.Ignore; return b; }
        // İlerleme çubuğu (XP, basamak): yüzde 0..1.
        VisualElement Cubuk(float oran, Color c, float h = 8)
        {
            var z = new VisualElement(); z.AddToClassList("cubuk"); z.style.height = h; z.pickingMode = PickingMode.Ignore;
            var d = new VisualElement(); d.AddToClassList("cubuk-dolu"); d.style.width = Length.Percent(Mathf.Clamp01(oran) * 100); d.style.backgroundColor = c; z.Add(d);
            return z;
        }
        // Dikey kaydırmalı gövde; içerik en az görünür alan kadar uzun (alttaki "boşluk" esnesin, birincil düğme başparmak
        // bölgesine insin).
        ScrollView Kaydirma(VisualElement ebeveyn, bool tamBoy = false)
        {
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.AddToClassList("kaydir"); sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; sv.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
            if (tamBoy) sv.contentViewport.RegisterCallback<GeometryChangedEvent>(e => { float h = sv.contentViewport.layout.height; if (!float.IsNaN(h)) sv.contentContainer.style.minHeight = h; });
            ebeveyn.Add(sv); return sv;
        }
    }

    // Koddan çizilen simgeler (beyaz + alfa; boya Image.tintColor). 96 px, 2×2 örnekleme ile kenar yumuşatma.
    public static class Ikonlar
    {
        public static Texture2D Tik, Geri, Ileri, Halka, Disli, Kilit, Oyna, Kupa, Raket, Kisi, Gradyan, YildizDolu, YildizBos, Yukari, Asagi;
        static bool hazir;
        public static void Hazirla()
        {
            if (hazir) return; hazir = true;
            Tik = Ciz((x, y) => Cizgi(x, y, -.62f, .02f, -.15f, -.45f, .15f) || Cizgi(x, y, -.15f, -.45f, .66f, .52f, .15f));
            Geri = Ciz((x, y) => Cizgi(x, y, .28f, .62f, -.26f, 0, .13f) || Cizgi(x, y, -.26f, 0, .28f, -.62f, .13f));
            Ileri = Ciz((x, y) => Cizgi(x, y, -.28f, .62f, .26f, 0, .13f) || Cizgi(x, y, .26f, 0, -.28f, -.62f, .13f));
            Halka = Ciz((x, y) => { float r = Mathf.Sqrt(x * x + y * y); return Mathf.Abs(r - .68f) < .14f; });
            Disli = Ciz((x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x); float dis = .7f + .22f * Mathf.Clamp01((Mathf.Cos(8 * a) - .05f) * 3);
                return r < dis && r > .3f;
            });
            Kilit = Ciz((x, y) =>
            {
                bool govde = Mathf.Abs(x) < .58f && y > -.82f && y < .08f && !((x * x + (y + .34f) * (y + .34f)) < .02f);
                float r = Mathf.Sqrt(x * x + (y - .1f) * (y - .1f)); bool kulp = y > .05f && Mathf.Abs(r - .36f) < .11f;
                return govde || kulp;
            });
            Oyna = Ciz((x, y) => { float x0 = -.42f, x1 = .66f; if (x < x0 || x > x1) return false; float h = .62f * (x1 - x) / (x1 - x0); return Mathf.Abs(y) < h; });
            Kupa = Ciz((x, y) =>
            {
                bool kase = (y > .3f && y < .78f && Mathf.Abs(x) < .52f) || (y <= .3f && (x * x + (y - .3f) * (y - .3f)) < .52f * .52f && y > -.2f);
                bool sap = Mathf.Abs(x) < .09f && y <= -.1f && y > -.55f; bool taban = Mathf.Abs(x) < .4f && y <= -.55f && y > -.78f;
                bool kulak = Mathf.Abs(Mathf.Sqrt((Mathf.Abs(x) - .55f) * (Mathf.Abs(x) - .55f) + (y - .42f) * (y - .42f)) - .2f) < .07f && Mathf.Abs(x) > .52f;
                return kase || sap || taban || kulak;
            });
            Raket = Ciz((x, y) =>
            {
                float ex = x / .42f, ey = (y - .28f) / .56f, e = Mathf.Sqrt(ex * ex + ey * ey);
                bool cerceve = Mathf.Abs(e - 1) * .45f < .07f; bool sap = Cizgi(x, y, 0, -.3f, 0, -.92f, .09f);
                bool tel = e < 1 && (Mathf.Repeat(x + 1, .21f) < .035f || Mathf.Repeat(y + 1, .21f) < .035f);
                return cerceve || sap || tel;
            });
            Kisi = Ciz((x, y) =>
            {
                bool bas = (x * x + (y - .42f) * (y - .42f)) < .29f * .29f; float sx = x / .72f, sy = (y + .82f) / .72f;
                bool omuz = sx * sx + sy * sy < 1 && y > -.86f && y < .02f; return bas || omuz;
            });
            // Kariyer yıldızları (2026-10-26): alınan dolu, alınmayan yalnız çerçeve (renk körlüğü: biçim farkı, renk değil).
            YildizDolu = Ciz((x, y) => Yildiz(x, y, .92f)); YildizBos = Ciz((x, y) => Yildiz(x, y, .92f) && !Yildiz(x, y, .58f));
            // Stat çubuğunun artı/eksi işareti (2026-10-26, ekipman v2 ▲ ▼): glif değil çizim (yazı tipinde olmayabilir); yönü
            // biçimle verir, rengi ek bilgidir (renk körlüğü).
            Yukari = Ciz((x, y) => y > -.55f && y < .6f && Mathf.Abs(x) < (.6f - y) * .62f); Asagi = Ciz((x, y) => y < .55f && y > -.6f && Mathf.Abs(x) < (y + .6f) * .62f);
            // Oyna sekmesinin zemini: üstte saydam (kort görünür), altta koyu (kartlar okunur). USS'te gradyan yok.
            Gradyan = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[64]; for (int i = 0; i < 64; i++) { float t = 1 - i / 63f; float a = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .18f) / .62f)) * .9f; px[i] = new Color(8 / 255f, 16 / 255f, 17 / 255f, a); }
            Gradyan.SetPixels32(px); Gradyan.Apply();
        }
        // Beş köşeli yıldız (dış yarıçap r, iç .45r): çokgen içinde mi (ışın sayımı).
        static bool Yildiz(float x, float y, float r)
        {
            bool ic = false; int n = 10; y += .06f;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float ai = Mathf.PI / 2 + i * Mathf.PI / 5, aj = Mathf.PI / 2 + j * Mathf.PI / 5, ri = i % 2 == 0 ? r : r * .45f, rj = j % 2 == 0 ? r : r * .45f;
                float xi = Mathf.Cos(ai) * ri, yi = Mathf.Sin(ai) * ri, xj = Mathf.Cos(aj) * rj, yj = Mathf.Sin(aj) * rj;
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) ic = !ic;
            }
            return ic;
        }
        static bool Cizgi(float x, float y, float ax, float ay, float bx, float by, float yari)
        {
            float vx = bx - ax, vy = by - ay, wx = x - ax, wy = y - ay; float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy));
            float dx = wx - vx * t, dy = wy - vy * t; return dx * dx + dy * dy < yari * yari;
        }
        static Texture2D Ciz(Func<float, float, bool> ic)
        {
            const int n = 96; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    int say = 0;
                    for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++) { float fx = (x + .25f + .5f * sx) / n * 2 - 1, fy = (y + .25f + .5f * sy) / n * 2 - 1; if (ic(fx, fy)) say++; }
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(say * 255 / 4));
                }
            t.SetPixels32(px); t.Apply(); return t;
        }
    }
}
