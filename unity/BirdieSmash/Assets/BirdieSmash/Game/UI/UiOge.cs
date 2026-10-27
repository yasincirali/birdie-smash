// Menü yapı taşları (UI Toolkit, 2026-10-26): yazı, düğme, seçim çipi, kart, simge. Biçim Resources/ui/menu_stil.uss.te,
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
        // 2026-10-26 Yayın Gecesi paleti (menu_stil.uss kök değişkenleriyle aynı değerler; kodda boyanan simgeler ve dokular için).
        // CAMGOBEGI adı geriye uyum için kaldı: seçili/şimdi rengi artık limon (bkz. menu_stil.uss başı); GOK ikincil veri.
        // 2026-10-27 yön C "Gün Batımı Salonu" (yasin karar kartı): değerler USS kökündeki C değerleri. Adlar aynı kaldı (başka
        // dallar bu adları kullanıyor): CAMGOBEGI = TURUNCU = seçili/şimdi/ilerleme; LIMON yalnız "sen"; MERCAN = rakip (değeri
        // orkide, birincil eylemin turuncu ailesinden ayrılsın diye; neden ve ölçü menu_stil.uss başında); KOYU = sıcak
        // koyu yazı/simge (turuncu ve limon üstünde Lc 63 / 86); MUREKKEP = zemin.
        public static readonly Color LIMON = Kit.Hex("#C9EE4B"), CAMGOBEGI = Kit.Hex("#FFA04D"), GOK = Kit.Hex("#8FC6FF"), MERCAN = Kit.Hex("#E99BFF"), ALTIN = Kit.Hex("#F9D272"),
            SOLUK = Kit.Hex("#CBB8C5"), KOYU = Kit.Hex("#2A0F12"), MUREKKEP = Kit.Hex("#1A1221"), YAZI = Kit.Hex("#F3EAEE");
        public static readonly Color TURUNCU = Kit.Hex("#FFA04D"), CTA_MUREKKEP = Kit.Hex("#1E0A0C"), CTA_OK = Kit.Hex("#FF8A57"), RAKIP = Kit.Hex("#E99BFF");
        // Birincil eylem degradesi (yön C: taslakta yatay #FF7A5C → #FFA04D). Dokuda dikey, çünkü 9 dilimde yan dilimler
        // sabit kalır, orta gerilir: dikey degrade her genişlikte aynı görünür. Alt ton #FF7A5C değil #FF9458: alt satır
        // (14–15 pt) koyu yazı orada durur; #FF7A5C üstünde Lc 53 (sınırın altı), #FF9458 üstünde 60,7, altı satırın ortasında 62.
        public static readonly Color CTA_UST = Kit.Hex("#FFB25E"), CTA_ORTA = Kit.Hex("#FFA04D"), CTA_ALT = Kit.Hex("#FF9458");

        Label Yazi(string t, float px, string cls = null)
        {
            var l = new Label(t); l.AddToClassList("yazi"); if (cls != null) l.AddToClassList(cls);
            l.style.fontSize = Mathf.Round(px * K); l.pickingMode = PickingMode.Ignore;
            if (SembolVar(t)) { l.AddToClassList("sembol"); if (temaFontVar) l.style.unityFontDefinition = temaFont; }
            return l;
        }
        // Sembol (2026-10-26, Barlow geçişi): ✓ ✗ ★ ◆ Barlow'da yok (kare kutu çıkar). Bu glifleri taşıyan yazı temanın kendi
        // yazı tipinde kalır (bugünkü görünüş birebir): tema yazı tipi kökten (Barlow atanmamış belge kökü) ilk yerleşimde
        // okunur (UiKok.Update), o ana kadar kurulan .sembol yazılara o an yazılır.
        static bool SembolVar(string t) { return t != null && t.IndexOfAny(SEMBOL) >= 0; }
        static readonly char[] SEMBOL = { '✓', '✗', '★', '☆', '◆' };
        StyleFontDefinition temaFont; bool temaFontVar;
        void TemaFontOku()
        {
            if (temaFontVar || doc == null || doc.rootVisualElement == null) return;
            var fd = doc.rootVisualElement.resolvedStyle.unityFontDefinition;
            if (fd.fontAsset == null && fd.font == null) return;
            temaFont = new StyleFontDefinition(fd); temaFontVar = true;
            kok.Query<Label>(null, "sembol").ForEach(l => l.style.unityFontDefinition = temaFont);
        }
        // Yayın başlığı (2026-10-26, yön A): Barlow Condensed italik, BÜYÜK harf (Türkçe i → İ, ı → I; CultureInfo("tr-TR")
        // iOS'ta değişmez kültür modunda güvenilmez). En uzun kelime satıra sığmazsa yazı küçülür (KelimeSigdir): ×2'de
        // "ŞAMPİYONASI" hece ortasından bölünmesin; boşluktan sarma serbest.
        Label Baslik(string t, float px, string cls = null)
        {
            var l = Yazi(Buyuk(t), px, "baslik"); if (cls != null) l.AddToClassList(cls); KelimeSigdir(l); return l;
        }
        public static string Buyuk(string s)
        {
            if (string.IsNullOrEmpty(s)) return s; if (!L.En) s = s.Replace('i', 'İ').Replace('ı', 'I');
            return s.ToUpperInvariant();
        }
        void KelimeSigdir(Label l, float minOran = .6f)
        {
            float taban = l.style.fontSize.value.value;
            l.RegisterCallback<GeometryChangedEvent>(e => KelimeOlc(l, taban, minOran));
        }
        static void KelimeOlc(Label l, float taban, float minOran)
        {
            float w = l.contentRect.width; if (float.IsNaN(w) || w <= 1 || taban <= 0 || string.IsNullOrEmpty(l.text)) return;
            float simdi = l.resolvedStyle.fontSize; if (float.IsNaN(simdi) || simdi <= 0) return;
            float en = 0; foreach (var k in l.text.Split(' ')) { if (k.Length == 0) continue; var o = l.MeasureTextSize(k, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined); if (o.x > en) en = o.x; }
            float gen = en * taban / simdi; if (float.IsNaN(gen) || gen <= 0) return;
            float hedef = Mathf.Clamp(Mathf.Floor(taban * Mathf.Min(1, (w - 2) / gen) * 2) / 2, Mathf.Round(taban * minOran), taban);
            if (Mathf.Abs(hedef - simdi) > .25f) l.style.fontSize = hedef;
        }
        // Dokulu öğe (eğik plaka, gradyan): arka plan dokusu koddan, dilim değerleri USS'te (sınıf).
        static void Doku(VisualElement v, Texture2D t) { if (t != null) v.style.backgroundImage = new StyleBackground(t); }
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
            if (secili) ic.Add(Ikon(Ikonlar.Tik, 16, MUREKKEP));
            var yz = new VisualElement(); yz.AddToClassList("cip-yazilar"); yz.pickingMode = PickingMode.Ignore; ic.Add(yz);
            yz.Add(Yazi(t, 14, "dugme-yazi")); if (!string.IsNullOrEmpty(alt)) yz.Add(Yazi(alt, 11.5f, "dugme-alt"));
            if (a != null) d.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); a(); }));
            Erisim.Etiket(d, string.IsNullOrEmpty(alt) ? t : t + ", " + alt, "dugme", secili, a == null);
            return d;
        }
        // Sığdır (2026-10-26, r13): dar çiplerde (segment, tutuş) tek kelimelik etiket hece ortasından bölünmesin ("Chara cter",
        // "Shoe s"). Etiket tek satıra alınır; çipin iç genişliğine sığmıyorsa yazı boyutu en çok minOran'a kadar küçülür.
        // Neden kodda: Unity 6000.0'da USS'te otomatik yazı boyutu yok. Hesap her seferinde taban boyuttan yapılır (şimdiki
        // boyuttan değil), çip genişliği yazıdan değil satırdan geldiği için GeometryChanged döngüye girmez.
        void Sigdir(VisualElement d, float minOran = .7f)
        {
            var l = d.Q<Label>(null, "dugme-yazi"); if (l == null) return;
            l.style.whiteSpace = WhiteSpace.NoWrap; l.style.flexShrink = 0f; float taban = l.style.fontSize.value.value;
            d.RegisterCallback<GeometryChangedEvent>(e => SigdirOlc(d, l, taban, minOran));
        }
        static void SigdirOlc(VisualElement d, Label l, float taban, float minOran)
        {
            float w = d.contentRect.width; if (float.IsNaN(w) || w <= 1 || taban <= 0) return;
            var ik = d.Q<Image>(null, "ikon");
            if (ik != null && ik.parent != null && ik.parent.resolvedStyle.flexDirection == FlexDirection.Row) w -= ik.resolvedStyle.width + ik.resolvedStyle.marginLeft + ik.resolvedStyle.marginRight;
            float simdi = l.resolvedStyle.fontSize; if (float.IsNaN(simdi) || simdi <= 0) return;
            var olc = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            float gen = olc.x * taban / simdi; if (float.IsNaN(gen) || gen <= 0) return;
            float hedef = Mathf.Clamp(Mathf.Floor(taban * Mathf.Min(1, (w - 2) / gen) * 2) / 2, Mathf.Round(taban * minOran), taban);
            if (Mathf.Abs(hedef - simdi) > .25f) l.style.fontSize = hedef;
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
        // 2026-10-26 Yayın Gecesi: simgeler (tüy, şimşek, hedef, güneş, çarpı, açık kilit) ve şekil dokuları (eğik düğme,
        // yayın plakası, etiket şeridi, basamak çizgisi, altın kutucuk, ışık süpürmesi, ışın). Hepsi 2× çözünürlükte (1 pt =
        // 2 doku pikseli; USS'te -unity-slice-scale: 0.5), açılışta bir kez; her biri birkaç KB.
        public static Texture2D Tuy, Simsek, Hedef, Gunes, Carpi, AcikKilit, EgikLimon, EgikLimonKucuk, Plaka, Serit, AltinKaro, Parilti, Isinlar, Isik;
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
            // Oyna sekmesinin zemini: USS'te gradyan yok. 2026-10-26: renk ve duraklar yön A'ya göre YayinDokulari'nda.
            Gradyan = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            YayinDokulari();
        }

        // ---- Yayın Gecesi dokuları ----
        static readonly Color NAVY = Kit.Hex("#261A30"), NAVY2 = Kit.Hex("#33233F"), INK = Kit.Hex("#1A1221");   // 2026-10-27 yön C: kart, düğme, zemin (adlar A'dan)
        static void YayinDokulari()
        {
            // Oyna zemini (yön A a-fade): üstte koyu (durum çubuğu ve üst şerit okunsun), ortada saydama yakın (afiş ve salon),
            // altta tam mürekkep (plaka, düğme ve kutucuklar). Eski yeşil-siyah gradyanın yerine.
            var px = new Color32[64]; float[] dur = { 0, .22f, .48f, .82f, 1 }, al = { .85f, .15f, .2f, 1, 1 };
            for (int i = 0; i < 64; i++)
            {
                float u = 1 - i / 63f, a = al[al.Length - 1];   // u: yukarıdan aşağı
                for (int k = 0; k < dur.Length - 1; k++) if (u >= dur[k] && u <= dur[k + 1]) { a = Mathf.Lerp(al[k], al[k + 1], Mathf.SmoothStep(0, 1, (u - dur[k]) / (dur[k + 1] - dur[k]))); break; }
                px[i] = new Color(INK.r, INK.g, INK.b, a);
            }
            Gradyan.SetPixels32(px); Gradyan.Apply();
            Tuy = Ciz((x, y) =>
            {   // eğik yaprak + sap (tüy top tüyü; Tüy para birimi)
                float c = Mathf.Cos(.785f), s = Mathf.Sin(.785f), rx = x * c - y * s, ry = x * s + y * c;
                bool yaprak = (rx * rx) / (.3f * .3f) + ((ry - .2f) * (ry - .2f)) / (.68f * .68f) < 1 && !(Mathf.Abs(rx) < .035f && ry > -.25f && ry < .7f);
                return yaprak || Cizgi(x, y, -.28f, -.28f, -.72f, -.72f, .07f);
            });
            Simsek = Ciz((x, y) => IcindeMi(x, y, new[] { .18f, .9f, -.5f, -.08f, -.02f, -.08f, -.2f, -.9f, .5f, .1f, .02f, .1f }));
            Hedef = Ciz((x, y) => { float r = Mathf.Sqrt(x * x + y * y); return Mathf.Abs(r - .78f) < .1f || Mathf.Abs(r - .46f) < .1f || r < .18f; });
            Gunes = Ciz((x, y) => { float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x); return r < .38f || (r > .52f && r < .9f && Mathf.Abs(Mathf.Repeat(a + Mathf.PI / 8, Mathf.PI / 4) - Mathf.PI / 8) < .13f); });
            Carpi = Ciz((x, y) => Cizgi(x, y, -.55f, -.55f, .55f, .55f, .14f) || Cizgi(x, y, -.55f, .55f, .55f, -.55f, .14f));
            AcikKilit = Ciz((x, y) =>
            {
                bool govde = Mathf.Abs(x) < .58f && y > -.82f && y < .08f && !((x * x + (y + .34f) * (y + .34f)) < .02f);
                float r = Mathf.Sqrt((x - .5f) * (x - .5f) + (y - .25f) * (y - .25f)); bool kulp = y > .2f && Mathf.Abs(r - .3f) < .1f;
                return govde || kulp || (Mathf.Abs(x - .2f) < .1f && y > .05f && y < .25f);
            });
            // OYNA düğmesi (a-cta): eğik kenarlı (sol kenar "/" 14 pt, sağ kenar aynı), dikey gradyan #E6FF73 → #D8FF3A (%55) →
            // #B9E61F, altta 4 pt koyu iç gölge. 9 dilim: yan dilimler 16 pt (32 doku pikseli), yükseklik 80 pt.
            // 2026-10-27 yön C: limon yerine gün batımı degradesi (UiKok.CTA_UST/ORTA/ALT, yukarıda); ad geriye uyum için aynı.
            EgikLimon = Egik(96, 160, 28, UiKok.CTA_UST, UiKok.CTA_ORTA, UiKok.CTA_ALT, 8);
            // Etiket şeridi (a-kick "SIRADAKİ MAÇ"): düz limon, küçük eğim (−12°), 22 pt.
            // 2026-10-27 yön C: dolgusuz altın çerçeve + altın yazı (C taslağı .etk span); ekranda tek doygun dolgu OYNA kalsın.
            EgikLimonKucuk = Sekil(32, 44, (x, y, ic) => { float yt = 44 - y, sol = 9 * (1 - yt / 44), sag = 32 - 9 * (yt / 44); return x >= sol + ic && x <= sag - ic && yt >= ic && yt <= 44 - ic; },
                (x, y) => new Color(UiKok.ALTIN.r, UiKok.ALTIN.g, UiKok.ALTIN.b, .10f), new Color(UiKok.ALTIN.r, UiKok.ALTIN.g, UiKok.ALTIN.b, 1), 3);
            // Yayın plakası (a-plate): üç köşe 14 pt yuvarlak, sağ alt 18 pt kesik; soldan sağa navy2 → navy; 1 pt ince çerçeve.
            Plaka = Sekil(96, 96, (x, y, ic) => PlakaIc(x, y, 96, 96, ic), (x, y) => Color.Lerp(new Color(NAVY2.r, NAVY2.g, NAVY2.b, .97f), new Color(NAVY.r, NAVY.g, NAVY.b, .95f), x / 96f), new Color(1, 1, 1, .12f), 2);
            // Şimdiki basamak (a-pips .cur): limon / soluk limon çapraz çizgi; düz renk yerine desen, yalnız renkle değil.
            Serit = new Texture2D(96, 10, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var sp = new Color32[96 * 10]; for (int y = 0; y < 10; y++) for (int x = 0; x < 96; x++) { bool on = Mathf.Repeat(x + y * .36f, 12) < 6; sp[y * 96 + x] = on ? (Color32)LIMON_D : (Color32)new Color(LIMON_D.r, LIMON_D.g, LIMON_D.b, .35f); }
            Serit.SetPixels32(sp); Serit.Apply();
            // Günün maçı kutucuğu (a-tile--gold): sol üstten altın %16 → %60'ta lacivert. Köşe yuvarlaması USS border-radius'ta.
            AltinKaro = Sekil(64, 64, (x, y, ic) => true, (x, y) => { float k = Mathf.Clamp01(((x / 64f) + (1 - y / 64f)) / 2 / .6f); return Color.Lerp(Color.Lerp(NAVY, UiKok.ALTIN, .16f), NAVY, k); }, Color.clear, 0);
            // Işık süpürmesi (a-cta::after): beyaz, eğik, iki yandan sönen bant.
            Parilti = Sekil(64, 64, (x, y, ic) => true, (x, y) => { float k = (x - (y - 32) * .36f) / 64f; float a = Mathf.Clamp01(1 - Mathf.Abs(k - .5f) * 2); return new Color(1, 1, 1, a * a * .55f); }, Color.clear, 0);
            // Maç sonu ışınları (a-rays): 18 ışın, merkezden dışa sönen; Hud döndürür (hareketi azaltta yok).
            Isinlar = Sekil(256, 256, (x, y, ic) => true, (x, y) =>
            {
                float dx = (x - 128) / 128f, dy = (y - 128) / 128f, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180;
                float isin = Mathf.Repeat(a, 20) < 7 ? 1 : 0; float maske = Mathf.Clamp01((.7f - r) / .5f); return new Color(1, 1, 1, isin * maske * Mathf.Clamp01(r * 4));
            }, Color.clear, 0);
            Isik = Kit.RadialTex(64);
        }
        static readonly Color LIMON_D = Kit.Hex("#FFA04D");   // 2026-10-27 yön C: şimdiki basamak deseni turuncu (seçili/şimdi rengi); ad A'dan
        // Yuvarlak köşeli kutu (r doku pikseli) + ince çerçeve: IMGUI maç sonu kartları ve düğmeleri için (GUIStyle.border = r+2,
        // 9 dilim). IMGUI'de 1 doku pikseli = 1 ekran pikseli olduğu için Hud ölçek değişince yeniden üretir.
        public static Texture2D Kutu(int r, Color dolgu, Color cerceve, float kalin)
        {
            int n = r * 2 + 4;
            return Sekil(n, n, (x, y, ic) =>
            {
                if (x < ic || x > n - ic || y < ic || y > n - ic) return false;
                float cx = Mathf.Clamp(x, r, n - r), cy = Mathf.Clamp(y, r, n - r), dx = x - cx, dy = y - cy; return dx * dx + dy * dy <= (r - ic) * (r - ic);
            }, (x, y) => dolgu, cerceve, kalin);
        }
        // Eğik limon düğme dokusu istenen ekran ölçeğinde (IMGUI: yan dilim = egim + 2 piksel).
        public static Texture2D EgikDugme(int w, int h, int egim) { return Egik(w, h, egim, UiKok.CTA_UST, UiKok.CTA_ORTA, UiKok.CTA_ALT, Mathf.Max(1, h / 20)); }   // 2026-10-27 yön C degradesi
        // Eğik dikdörtgen: sol kenar alttan üste sağa (14 pt), sağ kenar alttan üste sağa; gradyan üst, orta (%55), alt; altta iç gölge.
        static Texture2D Egik(int w, int h, int egim, Color ust, Color orta, Color alt, int golge)
        {
            return Sekil(w, h, (x, y, ic) =>
            {
                float yt = h - y, sol = egim * (1 - yt / h), sag = w - egim * (yt / h);
                return x >= sol + ic && x <= sag - ic && yt >= ic && yt <= h - ic;
            }, (x, y) =>
            {
                float v = 1 - y / (float)h; Color c = v < .55f ? Color.Lerp(ust, orta, v / .55f) : Color.Lerp(orta, alt, (v - .55f) / .45f);
                if (golge > 0 && y < golge) c = Color.Lerp(c, Color.black, .18f); return c;
            }, Color.clear, 0);
        }
        static bool PlakaIc(float x, float y, int w, int h, float ic)
        {
            float r = 28, kes = 36; float yt = h - y;   // yt: yukarıdan
            if (x < ic || x > w - ic || yt < ic || yt > h - ic) return false;
            if (x + yt > w + h - kes - ic * .6f) return false;   // sağ alt kesik
            float cx = x < r ? r : x > w - r ? w - r : x, cy = yt < r ? r : yt > h - r ? h - r : yt;
            bool sagAlt = x > w - r && yt > h - r; if (sagAlt) return true;
            float dx = x - cx, dy = yt - cy; return dx * dx + dy * dy <= (r - ic) * (r - ic);
        }
        // Şekil dokusu: ic(x, y, içe pay) şeklin içinde mi; renk(x, y) dolgu; cerceveKalin > 0 ise kenarda ince çerçeve.
        // 2×2 örnekleme ile kenar yumuşatma. y aşağıdan yukarı (Texture2D satır sırası).
        static Texture2D Sekil(int w, int h, Func<float, float, float, bool> ic, Func<float, float, Color> renk, Color cerceve, float cerceveKalin)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    int say = 0, kenar = 0;
                    for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                        {
                            float fx = x + .25f + .5f * sx, fy = y + .25f + .5f * sy;
                            if (ic(fx, fy, 0)) { say++; if (cerceveKalin > 0 && !ic(fx, fy, cerceveKalin)) kenar++; }
                        }
                    if (say == 0) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                    Color c = renk(x + .5f, y + .5f); if (kenar > 0) c = Color.Lerp(c, new Color(Mathf.Lerp(c.r, cerceve.r, cerceve.a), Mathf.Lerp(c.g, cerceve.g, cerceve.a), Mathf.Lerp(c.b, cerceve.b, cerceve.a), Mathf.Max(c.a, cerceve.a)), kenar / (float)say);
                    c.a *= say / 4f; px[y * w + x] = c;
                }
            t.SetPixels32(px); t.Apply(); return t;
        }
        // Çokgen içinde mi (x0,y0,x1,y1,... simge birimi −1..1; ışın sayımı).
        static bool IcindeMi(float x, float y, float[] p)
        {
            bool ic = false; int n = p.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = p[i * 2], yi = p[i * 2 + 1], xj = p[j * 2], yj = p[j * 2 + 1];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) ic = !ic;
            }
            return ic;
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
