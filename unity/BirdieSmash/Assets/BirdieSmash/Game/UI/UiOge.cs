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
        // 2026-11-22 Stadyum Pop (yasin: "stadyum pop tan devam edelim, ama ai unslop olmalı"): değerler menu_stil.uss kökü.
        // Adlar yine aynı (başka dallar kullanıyor). TURUNCU / CAMGOBEGI artık sarı #FFC93C: "bas, şimdi, seçili" tek vurgu.
        // MUREKKEP = kort mavisi zemin; KOYU = mürekkep #1B2340 (sarı üstünde 10:1); SEKME_SOLUK beyaz sekme çubuğunda soluk simge.
        public static readonly Color LIMON = Kit.Hex("#C9EE4B"), CAMGOBEGI = Kit.Hex("#FFC93C"), GOK = Kit.Hex("#A9D6FF"), MERCAN = Kit.Hex("#E99BFF"), ALTIN = Kit.Hex("#F9D272"),
            SOLUK = Kit.Hex("#D6E2FF"), KOYU = Kit.Hex("#1B2340"), MUREKKEP = Kit.Hex("#2659BE"), YAZI = Kit.Hex("#FFFFFF");
        public static readonly Color TURUNCU = Kit.Hex("#FFC93C"), CTA_MUREKKEP = Kit.Hex("#1B2340"), CTA_OK = Kit.Hex("#FFC93C"), RAKIP = Kit.Hex("#E99BFF"), SEKME_SOLUK = Kit.Hex("#5A6690");
        // Birincil eylem degradesi (yön C: taslakta yatay #FF7A5C → #FFA04D). Dokuda dikey, çünkü 9 dilimde yan dilimler
        // sabit kalır, orta gerilir: dikey degrade her genişlikte aynı görünür. Alt ton #FF7A5C değil #FF9458: alt satır
        // (14–15 pt) koyu yazı orada durur; #FF7A5C üstünde Lc 53 (sınırın altı), #FF9458 üstünde 60,7, altı satırın ortasında 62.
        // 2026-11-22 Stadyum Pop: sarı, üstte bir tık açık; alt dudak Egik(golge) ile koyu sarı (3B tıknaz düğme, mürekkep konturu USS).
        public static readonly Color CTA_UST = Kit.Hex("#FFD966"), CTA_ORTA = Kit.Hex("#FFC93C"), CTA_ALT = Kit.Hex("#F7BA2C");

        public const float YAZI_MIN = 11;
        // Avatar içi (2026-10-31): karakter portresi (Portre) varsa dairenin içinde, yoksa eski insan simgesi.
        void AvatarIc(VisualElement av, Texture portre, int ikonPx)
        {
            if (portre == null) { av.Add(Ikon(Ikonlar.Kisi, ikonPx, KOYU)); return; }
            av.AddToClassList("avatar--portre"); var im = new Image { image = portre, scaleMode = ScaleMode.ScaleAndCrop }; im.AddToClassList("avatar-resim"); im.pickingMode = PickingMode.Ignore; av.Add(im);
        }
        Label Yazi(string t, float px, string cls = null)
        {
            var l = new Label(t); l.AddToClassList("yazi"); if (cls != null) l.AddToClassList(cls);
            // 2026-10-27 (duyarlı yerleşim): taban ne olursa olsun en az 11 birim (panel birimi telefonda ≥ 1 pt; Apple HIG en küçük
            // okunur boy 11 pt). Eskiden kilit rozetleri 10, "Sistem" ×0,9'da 11'lik etiketler 10 çıkıyordu.
            l.style.fontSize = Mathf.Max(YAZI_MIN, Mathf.Round(px * K)); l.pickingMode = PickingMode.Ignore;
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
        // Tek satıra sığdır (2026-11-16 inceleme, fikir 02:05 #7; r163 "MAHALLE SALONU" plakada iki satırdı, kutucuklar sekme
        // çubuğuna dayanıyordu): bütün yazı taban boyutun %75'ine kadar küçülerek tek satıra sığıyorsa öyle; sığmıyorsa KelimeOlc
        // (sarma serbest, sözcük bölünmez). Hedef hep taban boyuttan hesaplanır: boyut değişince yeniden ölçmek aynı sonucu verir,
        // GeometryChanged döngüsü kurmaz (r133 dersi). Büyük yazıda (K > 1,35) çağrılmaz: orada okunurluk önce, sarma kalır.
        void SatirSigdir(Label l, float minOran = .75f)
        {
            float taban = l.style.fontSize.value.value;
            l.RegisterCallback<GeometryChangedEvent>(e =>
            {
                float w = l.contentRect.width; if (float.IsNaN(w) || w <= 1 || taban <= 0 || string.IsNullOrEmpty(l.text)) return;
                float simdi = l.resolvedStyle.fontSize; if (float.IsNaN(simdi) || simdi <= 0) return;
                var o = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                float gen = o.x * taban / simdi; if (float.IsNaN(gen) || gen <= 0) return;
                float hedef = Mathf.Min(taban, Mathf.Floor(taban * (w - 2) / gen * 2) / 2);
                if (hedef < taban * minOran) { KelimeOlc(l, taban, .6f); return; }
                if (Mathf.Abs(hedef - simdi) > .25f) l.style.fontSize = hedef;
            });
        }
        static void KelimeOlc(Label l, float taban, float minOran)
        {
            float w = l.contentRect.width; if (float.IsNaN(w) || w <= 1 || taban <= 0 || string.IsNullOrEmpty(l.text)) return;
            float simdi = l.resolvedStyle.fontSize; if (float.IsNaN(simdi) || simdi <= 0) return;
            float en = 0; foreach (var k in l.text.Split(' ')) { if (k.Length == 0) continue; var o = l.MeasureTextSize(k, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined); if (o.x > en) en = o.x; }
            float gen = en * taban / simdi; if (float.IsNaN(gen) || gen <= 0) return;
            float hedef = Mathf.Clamp(Mathf.Floor(taban * Mathf.Min(1, (w - 2) / gen) * 2) / 2, Mathf.Min(taban, Mathf.Max(YAZI_MIN, Mathf.Round(taban * minOran))), taban);   // 2026-10-27: küçülme de 11'in altına inmez
            if (Mathf.Abs(hedef - simdi) > .25f) l.style.fontSize = hedef;
        }
        // Dokulu öğe (eğik plaka, gradyan): arka plan dokusu koddan, dilim değerleri USS'te (sınıf).
        static void Doku(VisualElement v, Texture2D t) { if (t != null) v.style.backgroundImage = new StyleBackground(t); }
        // Ana düğme biçimi: üst satır kalın, isteğe bağlı alt satır küçük.
        VisualElement Dugme(string t, Action a, string cls = null, string alt = null, float px = 16)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); if (cls != null) d.AddToClassList(cls);
            // 2026-11-22 inceleme (fikir 17:06 #2): sondaki "  ›" / "  ↗" yazının parçasıydı, VoiceOver her satırda oku da okuyordu
            // ve iki ok kalıbı vardı (Nasıl oynanır satırları oku ayrı öğe çiziyor). Ok burada ayrılır: sağda dokunulmaz ayrı öğe,
            // erişim adı oksuz; "↗" dış bağlantı demek, etikete "Tarayıcıda açılır" kendiliğinden eklenir. Çağrı yerleri aynı.
            string ok = null;
            if (t != null && t.Length > 3 && (t.EndsWith("  ›") || t.EndsWith("  ↗"))) { ok = t.Substring(t.Length - 1); t = t.Substring(0, t.Length - 3); d.AddToClassList("dugme--ok"); }
            var ic = new VisualElement(); ic.AddToClassList("dugme-ic"); ic.pickingMode = PickingMode.Ignore; d.Add(ic);
            ic.Add(Yazi(t, px, "dugme-yazi"));
            if (!string.IsNullOrEmpty(alt)) ic.Add(Yazi(alt, px * .78f, "dugme-alt"));
            if (ok != null) { var ol = Yazi(ok, px + 2, "dugme-ok"); ol.pickingMode = PickingMode.Ignore; d.Add(ol); }
            if (a != null) d.AddManipulator(new Clickable(() => { Tik(d); a(); }));   // 2026-11-06: çip ve büyük düğme gibi her düğme dokunuşta ince tık (hareketi azaltta Hap susar); 2026-11-19 ses de burada (Tik)
            string et = string.IsNullOrEmpty(alt) ? t : t + ", " + alt; if (ok == "↗") et += ". " + L.T("Tarayıcıda açılır");
            Erisim.Etiket(d, et, "dugme", false, a == null);
            return d;
        }
        VisualElement Pasif(string t, string alt = null) { var d = Dugme(t, null, "dugme--pasif", alt); return d; }
        // Seçim çipi: seçili olan ✓ ile de okunur (renk körlüğü); dokununca hafif titreşim (HTML/IMGUI Chip ile aynı).
        VisualElement Cip(string t, bool secili, Action a, string alt = null)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("cip"); if (secili) d.AddToClassList("cip--secili");
            var ic = new VisualElement(); ic.AddToClassList("cip-ic"); ic.pickingMode = PickingMode.Ignore; d.Add(ic);
            if (secili) { var tk = Ikon(Ikonlar.Tik, 16, KOYU); tk.AddToClassList("cip-tik"); ic.Add(tk); }   // 2026-11-07: seçili dili turuncu çerçeve + ✓ (krem dolgu kalktı); segment sekmesinde ✓ gizli (USS); 2026-11-22 Stadyum Pop: sarı dolgu üstünde mürekkep ✓
            var yz = new VisualElement(); yz.AddToClassList("cip-yazilar"); yz.pickingMode = PickingMode.Ignore; ic.Add(yz);
            yz.Add(Yazi(t, 14, "dugme-yazi")); if (!string.IsNullOrEmpty(alt)) yz.Add(Yazi(alt, 11.5f, "dugme-alt"));
            if (a != null) d.AddManipulator(new Clickable(() => { Tik(d); a(); }));
            Erisim.Etiket(d, string.IsNullOrEmpty(alt) ? t : t + ", " + alt, "dugme", secili, a == null);
            Sigdir(d); return d;
        }
        // Çipin seçili hâlini yerinde yazar (çerçeve sınıfı, ✓, erişim durumu); sayfa kurulmadan aç/kapa için (2026-11-22, fikir 12:06 #6).
        void CipDurum(VisualElement d, bool secili)
        {
            d.EnableInClassList("cip--secili", secili); var ic = d.Q(className: "cip-ic"); var tk = d.Q(className: "cip-tik");
            if (secili && tk == null && ic != null) { tk = Ikon(Ikonlar.Tik, 16, KOYU); tk.AddToClassList("cip-tik"); ic.Insert(0, tk); }
            else if (!secili && tk != null) tk.RemoveFromHierarchy();
            var eb = d.userData as ErisimBilgi; if (eb != null) eb.secili = secili;
        }
        // Aç/kapa satırı (2026-11-10 inceleme, fikir 02:05 #2): Ayarlar'da Ses/Müzik/Titreşim/Hareket tek seçimli çiplerle aynı
        // görünüyordu (turuncu çerçeve + ✓) ve çip metni durumla değişiyordu ("Ses açık" ↔ "Ses kapalı"): eylem mi durum mu
        // okunmuyordu, "Titreşim açık" TR'de kesiliyordu. HIG kalıbı: solda sabit ad, sağda anahtar. Durum üç yolla okunur:
        // topun konumu, topta ✓ (renk körlüğü) ve anahtarın solunda "açık/kapalı" yazısı (Duraklat'taki "Ses: açık" yazılı satır
        // kararıyla aynı dil). Bütün satır dokunma alanı (≥ 56 pt). a null ise pasif (ör. sistemden gelen Hareketi azalt).
        // İnceleme 2026-11-15 12:05 #4–#6: (1) satır artık ayrı cam düğme değil, §6.4 liste satırı (.liste-satir, ≥56); dört
        // anahtar ListeGrup'la tek cam kartta, aralarında ayırıcı (iOS Ayarlar kalıbı). (2) Görünen "açık/kapalı" yazısı kalktı:
        // durum topun konumu (biçim, renkten bağımsız) + açıkta ✓ ile görünür, erişim etiketi durumu yine söyler. (3) Dokununca
        // top yerinde kayar (.18 sn, USS translate) ve ray solar; eskiden sayfa baştan kurulup top yeni yerde beliriyordu.
        // Çağıranın sayfa yenilemesi DegistiSonra ile geçiş bitince gelir; hareketi azaltta geçiş 0 sn (USS .az-hareket).
        VisualElement AnahtarSatir(string ad, bool acik, Action a, string not = null)
        {
            var d = new VisualElement(); d.AddToClassList("liste-satir"); d.AddToClassList("anahtar-satir"); if (a == null) d.AddToClassList("anahtar-satir--pasif");
            var yz = new VisualElement(); yz.AddToClassList("anahtar-yazilar"); yz.pickingMode = PickingMode.Ignore; d.Add(yz);
            yz.Add(Yazi(ad, 15, "dugme-yazi")); if (!string.IsNullOrEmpty(not)) yz.Add(Yazi(not, 12, "dugme-alt"));
            var ray = new VisualElement(); ray.AddToClassList("anahtar"); if (acik) ray.AddToClassList("anahtar--acik"); ray.pickingMode = PickingMode.Ignore; d.Add(ray);
            var top = new VisualElement(); top.AddToClassList("anahtar-top"); top.pickingMode = PickingMode.Ignore; ray.Add(top);
            var tik = Ikon(Ikonlar.Tik, 14, MUREKKEP); tik.pickingMode = PickingMode.Ignore; top.Add(tik); tik.style.display = acik ? DisplayStyle.Flex : DisplayStyle.None;
            string etiket = string.IsNullOrEmpty(not) ? ad : ad + ", " + not; bool durum = acik;
            if (a != null) d.AddManipulator(new Clickable(() =>
            {
                Yerel.Hap("tick"); durum = !durum;
                ray.EnableInClassList("anahtar--acik", durum); tik.style.display = durum ? DisplayStyle.Flex : DisplayStyle.None;
                Erisim.Etiket(d, etiket, "dugme", durum, false, L.T(durum ? "açık" : "kapalı"));
                a();
            }));
            Erisim.Etiket(d, etiket, "dugme", acik, a == null, L.T(acik ? "açık" : "kapalı"));
            return d;
        }
        // Değerli satır (2026-11-16 inceleme, fikir 17:05 #6; iOS Ayarlar "Zil sesi: Yansıma ›" kalıbı): solda sabit ad, sağda
        // seçili değer + ›; dokununca sıradakine döner. Üç kademeli küçük ses ayarı için ayrı başlık + çip satırı gerekmesin.
        // Değer yazıyla görünür (renk yok), erişim etiketi yeni değeri söyler; tıkta tick. Bütün satır dokunma alanı (≥ 56 pt).
        VisualElement DegerSatir(string ad, string[] degerler, int secili, Action<int> a)
        {
            var d = new VisualElement(); d.AddToClassList("liste-satir"); d.AddToClassList("anahtar-satir");
            var yz = new VisualElement(); yz.AddToClassList("anahtar-yazilar"); yz.pickingMode = PickingMode.Ignore; d.Add(yz);
            yz.Add(Yazi(ad, 15, "dugme-yazi"));
            int s = Mathf.Clamp(secili, 0, degerler.Length - 1);
            var dg = Yazi(degerler[s] + "  ›", 15, "dugme-alt"); dg.AddToClassList("deger-yazi"); dg.pickingMode = PickingMode.Ignore; d.Add(dg);
            d.AddManipulator(new Clickable(() =>
            {
                Yerel.Hap("tick"); s = (s + 1) % degerler.Length; dg.text = degerler[s] + "  ›";
                Erisim.Etiket(d, ad, "dugme", false, false, degerler[s]); a(s);
            }));
            Erisim.Etiket(d, ad, "dugme", false, false, degerler[s]);
            return d;
        }
        // §6.4 grup: satırlar tek cam kartta, aralarında 1 pt ayırıcı (simgesiz satırda inset 16), son satırdan sonra yok.
        VisualElement ListeGrup(params VisualElement[] satirlar)
        {
            var g = new VisualElement(); g.AddToClassList("liste-grup");
            for (int i = 0; i < satirlar.Length; i++)
            {
                if (i > 0) { var ay = new VisualElement(); ay.AddToClassList("liste-ayirac"); ay.pickingMode = PickingMode.Ignore; g.Add(ay); }
                g.Add(satirlar[i]);
            }
            return g;
        }
        // Anahtar satırının sayfa yenilemesi: top kayarken sayfa baştan kurulmasın, geçiş (.18 sn) bitince. Hareketi azaltta hemen.
        void DegistiSonra() { if (Ayar.reduce || kok == null) Degisti(); else kok.schedule.Execute(() => Degisti()).StartingIn(200); }
        // Sığdır (2026-10-26, r13): dar çiplerde (segment, tutuş) tek kelimelik etiket hece ortasından bölünmesin ("Chara cter",
        // "Shoe s"). Etiket tek satıra alınır; çipin iç genişliğine sığmıyorsa yazı boyutu en çok minOran'a kadar küçülür.
        // Neden kodda: Unity 6000.0'da USS'te otomatik yazı boyutu yok. Hesap her seferinde taban boyuttan yapılır (şimdiki
        // boyuttan değil), çip genişliği yazıdan değil satırdan geldiği için GeometryChanged döngüye girmez.
        // 2026-11-07 dar çip kuralı (yasin "yap", arayüz cilası): her çipte etiket ve alt yazı tek satır; sığmazsa küçülür
        // (11 pt altına inmez). Cip() bunu kendisi çağırır; ikinci çağrı (eski çağıranlar) bir şey eklemez.
        void Sigdir(VisualElement d, float minOran = .7f)
        {
            if (d.ClassListContains("sigdir")) return; d.AddToClassList("sigdir");
            foreach (var sinif in new[] { "dugme-yazi", "dugme-alt" })
            {
                var l = d.Q<Label>(null, sinif); if (l == null) continue;
                l.style.whiteSpace = WhiteSpace.NoWrap; l.style.flexShrink = 0f; float taban = l.style.fontSize.value.value;
                d.RegisterCallback<GeometryChangedEvent>(e => SigdirOlc(d, l, taban, minOran));
            }
        }
        // Sözcük sığdır (2026-11-07, r127 Profil ×2 "Neighbourhoo/d player"): sarılan yazıda en uzun sözcük kaba sığmıyorsa Unity
        // onu harf ortasından böler. Yazı boyutu, en uzun sözcük satıra sığana kadar küçülür (11 pt altına inmez); sarma kalır.
        void SozcukSigdir(Label l)
        {
            float taban = l.style.fontSize.value.value; if (taban <= 0) return;
            l.RegisterCallback<GeometryChangedEvent>(e =>
            {
                float w = l.contentRect.width, simdi = l.resolvedStyle.fontSize; if (float.IsNaN(w) || w <= 1 || float.IsNaN(simdi) || simdi <= 0) return;
                string uzun = ""; foreach (var sz in l.text.Split(' ', '\n')) if (sz.Length > uzun.Length) uzun = sz; if (uzun.Length < 2) return;
                float gen = l.MeasureTextSize(uzun, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x * taban / simdi; if (gen <= 0) return;
                float hedef = Mathf.Clamp(Mathf.Floor(taban * Mathf.Min(1, (w - 2) / gen) * 2) / 2, Mathf.Min(taban, YAZI_MIN), taban);
                // 2026-11-19 inceleme (fikir 02:05 Ayarlar #1, r197 "Settings" 11 pt): büyümeyen etiketin genişliği yazının kendisi;
                // oran hep 1'in az altında kalıp her GeometryChanged'de yarım punto indiriyordu (19 → 11, 16 yerleşim turu). Sözcük
                // şimdiki boyutta zaten sığıyorsa küçültme yok; büyüme (kap genişleyince) serbest.
                if (hedef < simdi && gen * simdi / taban <= w) return;
                if (Mathf.Abs(hedef - simdi) > .25f) l.style.fontSize = hedef;
            });
        }
        static void SigdirOlc(VisualElement d, Label l, float taban, float minOran)
        {
            float w = d.contentRect.width; if (float.IsNaN(w) || w <= 1 || taban <= 0) return;
            var ik = d.Q<Image>(null, "ikon");
            float ikPay = 0;
            if (ik != null && ik.parent != null && ik.resolvedStyle.display != DisplayStyle.None && ik.parent.resolvedStyle.flexDirection == FlexDirection.Row) { ikPay = ik.resolvedStyle.width + ik.resolvedStyle.marginLeft + ik.resolvedStyle.marginRight; w -= ikPay; }
            float simdi = l.resolvedStyle.fontSize; if (float.IsNaN(simdi) || simdi <= 0) return;
            var olc = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            float gen = olc.x * taban / simdi; if (float.IsNaN(gen) || gen <= 0) return;
            // 2026-11-14 (yasin: tasarım dili, r170 İngilizce maç kartı "Medi…"): dört eşit zorluk çipinde seçili çipin ✓'i yazının
            // yerini yiyordu; en küçük boyutta da sığmayan seçili yazı kesiliyordu. Sığmıyorsa ✓ gizlenir, seçili durum turuncu çerçeve,
            // dolgu ve kalın yazıyla okunur (docs/arayuz_tasarim_dili.md §3: "✓ ya da çerçeve"; ekran okuyucuda "seçili" kalır).
            float enAz = Mathf.Min(taban, Mathf.Max(YAZI_MIN, Mathf.Round(taban * minOran)));
            if (ikPay > 0 && ik.ClassListContains("cip-tik") && gen * enAz / taban > w - 2) { ik.style.display = DisplayStyle.None; w += ikPay; }
            float hedef = Mathf.Clamp(Mathf.Floor(taban * Mathf.Min(1, (w - 2) / gen) * 2) / 2, Mathf.Min(taban, Mathf.Max(YAZI_MIN, Mathf.Round(taban * minOran))), taban);   // 2026-10-27: küçülme de 11'in altına inmez
            // Grup ölçüsü (2026-11-14 inceleme, fikir 02:05 #1; r168 İngilizce "Tournament" öbür üç saha adından küçüktü): satır
            // "sigdir-grup" ise her çip kendi hedefini etikete yazar, satırdaki ana yazılar hedeflerin en küçüğünü alır. Eşit
            // seçenek eşit görünür. Her çipin kendi hedefi taban boyuttan hesaplandığı için en küçük sabit kalır, döngü kurmaz.
            if (d.parent != null && d.parent.ClassListContains("sigdir-grup") && l.ClassListContains("dugme-yazi"))
            {
                l.userData = hedef; float enK = hedef;
                foreach (var kd in d.parent.Children()) { var kl = kd.Q<Label>(null, "dugme-yazi"); if (kl != null && kl.userData is float) enK = Mathf.Min(enK, (float)kl.userData); }
                foreach (var kd in d.parent.Children()) { var kl = kd.Q<Label>(null, "dugme-yazi"); if (kl != null && kl != l && kl.userData is float && Mathf.Abs(kl.resolvedStyle.fontSize - enK) > .25f) kl.style.fontSize = enK; }
                hedef = enK;
            }
            if (Mathf.Abs(hedef - simdi) > .25f) l.style.fontSize = hedef;
            // 2026-11-08 (yasin: "alanlarından butonlarda taşan"): en küçük boyutta da sığmayan yazı çipin dışına taşıyordu (r142 sap
            // çipleri "Kontrol +3 · drive −3" 103 > 77 pt). Alt yazı iki satıra sarılır; ana yazı kesilmez, "…" ile biter (tam metin
            // erişilebilirlik etiketinde). Ölçü tek satır genişliği olduğundan sonuç sarmadan bağımsız, döngü kurmaz.
            bool sigmaz = gen * hedef / taban > w - 2, alt = l.ClassListContains("dugme-alt");
            if (alt) { l.style.whiteSpace = sigmaz ? WhiteSpace.Normal : WhiteSpace.NoWrap; l.style.flexShrink = sigmaz ? 1f : 0f; l.style.maxWidth = sigmaz ? new StyleLength(w) : new StyleLength(StyleKeyword.None); }
            else { l.style.flexShrink = sigmaz ? 1f : 0f; l.style.overflow = sigmaz ? Overflow.Hidden : Overflow.Visible; l.style.textOverflow = sigmaz ? TextOverflow.Ellipsis : TextOverflow.Clip; }
        }
        Image Ikon(Texture2D t, float pt, Color c)
        {
            var i = new Image { image = t, scaleMode = ScaleMode.ScaleToFit, tintColor = c }; i.AddToClassList("ikon");
            float s = Mathf.Round(pt * Mathf.Min(K, 1.6f)); i.style.width = s; i.style.height = s; i.pickingMode = PickingMode.Ignore; return i;
        }
        // Durum işareti + yazı (2026-11-20 inceleme, fikir 17:06 #2): metindeki ✓/✗ öneki yazının TAMAMINI .sembol ile tema yazı
        // tipine düşürüyordu (yan yana iki aile, rakam genişliği farklı, kalın sınıf etkisiz). İşaret dokudan ikon, yazı Nunito'da
        // kalır (iOS Label: ikon + metin). Anlam yalnız renkte değil: tik/çarpı biçimi + yazı. Erişim etiketi çağıranda.
        VisualElement IkonYazi(bool tik, string t, float px, string cls, Color c)
        {
            var s = new VisualElement(); s.AddToClassList("ikon-yazi"); s.pickingMode = PickingMode.Ignore;
            var i = Ikon(tik ? Ikonlar.Tik : Ikonlar.Carpi, px, c); i.style.marginRight = 4; i.style.flexShrink = 0f; s.Add(i);
            var l = Yazi(t, px, cls); l.style.whiteSpace = WhiteSpace.Normal; l.style.flexShrink = 1f; s.Add(l);
            return s;
        }
        VisualElement Kart(string cls = null) { var k = new VisualElement(); k.AddToClassList("kart"); if (cls != null) k.AddToClassList(cls); return k; }
        // Yatay sıra; varsayılan sınıf "secim" (çip sırası: büyük yazıda alt alta dizilir), başka sıralar kendi sınıfını verir.
        VisualElement Satir(string cls = "secim") { var s = new VisualElement(); s.AddToClassList("satir"); if (cls != null) s.AddToClassList(cls); return s; }
        Label Grup(string t) { var l = Yazi(t, 13, "grup"); Erisim.Etiket(l, t, "baslik"); return l; }
        // Başlık + sağda sayaç (2026-11-21 inceleme, fikir 17:06 #5/#6): Kupa rafı kalıbı; Profil'de aynı tür bilgi tek biçimde.
        VisualElement GrupSatir(string t, VisualElement sag) { var r = Satir("arali"); r.AddToClassList("grup-satir"); r.Add(Grup(t)); r.Add(sag); return r; }
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
            // İnceleme 2026-11-20 22:10 (fikir 22:06 #6, HIG "kaydırma göstergesini koru"): Unity'nin masaüstü çubuğu gizli, yerine
            // hiçbir şey yoktu; uzun sayfada (Seviye yolu, Kariyer, Ayarlar, büyük yazı) nerede olduğun görünmüyordu. iOS kalıbı: ince
            // kapsül yalnız kaydırırken belirir, durunca 0,6 sn sonra solar. Yalnız değer değişince yazılır; boy değişmedikçe yerleşim
            // yazılmaz (translate + opacity), ayırma yok. Hareketi azaltta solma geçişi yok (USS .az-hareket).
            var gos = new VisualElement(); gos.AddToClassList("kaydir-gosterge"); gos.pickingMode = PickingMode.Ignore; sv.hierarchy.Add(gos);
            float gosBoy = -1; bool gosAcik = false; IVisualElementScheduledItem gosSol = null;
            sv.verticalScroller.valueChanged += v =>
            {
                Kalite.Hareket();   // parmak kalktıktan sonra süren eylemsiz kayma da yüksek hızda (Kalite)
                float vh = sv.contentViewport.layout.height, ch = sv.contentContainer.layout.height, ust = sv.verticalScroller.highValue;
                if (float.IsNaN(vh) || float.IsNaN(ch) || ch <= vh + 1 || ust <= 0) return;
                float boy = Mathf.Round(Mathf.Max(24, vh * vh / ch));
                if (boy != gosBoy) { gosBoy = boy; gos.style.height = boy; }
                gos.style.translate = new Translate(0, (vh - boy) * Mathf.Clamp01(v / ust), 0);
                if (!gosAcik) { gosAcik = true; gos.style.opacity = 1; }
                if (gosSol == null) gosSol = gos.schedule.Execute(() => { gosAcik = false; gos.style.opacity = 0; }).StartingIn(600); else gosSol.ExecuteLater(600);   // her kaydırma solmayı 0,6 sn öteler
            };
            if (tamBoy) sv.contentViewport.RegisterCallback<GeometryChangedEvent>(e => { float h = sv.contentViewport.layout.height; if (!float.IsNaN(h)) sv.contentContainer.style.minHeight = h; });
            ebeveyn.Add(sv); return sv;
        }
    }

    // Koddan çizilen simgeler (beyaz + alfa; boya Image.tintColor). 96 px, 2×2 örnekleme ile kenar yumuşatma.
    public static class Ikonlar
    {
        public static Texture2D Tik, Geri, Ileri, Halka, Disli, Kilit, Oyna, Kupa, Raket, Kisi, Tisort, Gradyan, YildizDolu, YildizBos, Yukari, Asagi, Yuz, Ayak, Tutus, Solma, Buyut, Arkadas, Arti;
        // 2026-11-23 alt bant (yasin: "ikonlar daha minimalist olabilir"): sekme simgeleri tek kalınlıkta çizgi (iOS SF Symbols
        // kalıbı); dolu simgeler menünün içinde (Oyna düğmesi, karolar) kalır. Önizleme menu-ux-ui-2026-11-23/sekme_ikon.png.
        public static Texture2D SekmeOyna, SekmeKupa, SekmeTisort, SekmeKisi, SekmeRaket;
        // 2026-10-26 Yayın Gecesi: simgeler (tüy, şimşek, hedef, güneş, çarpı, açık kilit) ve şekil dokuları (eğik düğme,
        // yayın plakası, etiket şeridi, basamak çizgisi, altın kutucuk, ışık süpürmesi, ışın). Hepsi 2× çözünürlükte (1 pt =
        // 2 doku pikseli; USS'te -unity-slice-scale: 0.5), açılışta bir kez; her biri birkaç KB.
        public static Texture2D Tuy, Simsek, Hedef, Gunes, Carpi, AcikKilit, EgikLimon, EgikLimonKucuk, Plaka, Serit, AltinKaro, Parilti, Isinlar, Isik, GunesDisk, AltinIz;
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
            }, 0, .12f);
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
            // Dolap sekmesi (2026-10-30): tişört; omuzlar ve kollar yatay, yaka oyuğu. Atölye sekmesi raket simgesini alır.
            Tisort = Ciz((x, y) =>
            {
                bool govde = Mathf.Abs(x) < .42f && y > -.78f && y < .55f; bool kol = y < .55f && y > .1f && Mathf.Abs(x) < .82f && (y - .1f) > (Mathf.Abs(x) - .42f) * -.2f && Mathf.Abs(x) - .42f < (y - .05f) * .9f;
                bool yaka = x * x + (y - .62f) * (y - .62f) < .2f * .2f; return (govde || kol) && !yaka;
            }, 0, .11f);
            Kisi = Ciz((x, y) =>
            {
                bool bas = (x * x + (y - .42f) * (y - .42f)) < .29f * .29f; float sx = x / .72f, sy = (y + .82f) / .72f;
                bool omuz = sx * sx + sy * sy < 1 && y > -.86f && y < .02f; return bas || omuz;
            }, 0, .08f);
            const float W = .075f;
            System.Func<float, float, float, float, float, bool> Hal = (x, y, cx, cy, r) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) < W;
            SekmeOyna = Ciz((x, y) => Hat(x, y, W, true, -.38f, .58f, .6f, 0, -.38f, -.58f));
            SekmeKupa = Ciz((x, y) => (Mathf.Abs(x) > .46f && Hal(Mathf.Abs(x), y, .6f, .5f, .16f)) || (y < .28f && Hal(x, y, 0, .28f, .46f))
                || Hat(x, y, W, false, -.46f, .28f, -.46f, .72f, .46f, .72f, .46f, .28f) || Cizgi(x, y, 0, -.18f, 0, -.56f, W) || Cizgi(x, y, -.3f, -.62f, .3f, -.62f, W));
            SekmeTisort = Ciz((x, y) => Hat(x, y, W, false, -.2f, .64f, -.44f, .64f, -.8f, .3f, -.6f, .08f, -.42f, .2f, -.42f, -.7f, .42f, -.7f, .42f, .2f, .6f, .08f, .8f, .3f, .44f, .64f, .2f, .64f)
                || (y < .64f && Hal(x, y, 0, .64f, .2f)));
            SekmeKisi = Ciz((x, y) => Hal(x, y, 0, .36f, .25f) || (y > -.74f && Hal(x, y, 0, -.74f, .6f)));
            SekmeRaket = Ciz((x, y) =>
            {   // 34° eğik: dik raket büyüteç gibi okunuyordu (önizleme); iki tel çizgisi raketi söyler
                float c = Mathf.Cos(-.6f), sn = Mathf.Sin(-.6f), rx = x * c - y * sn, ry = x * sn + y * c;
                float ex = rx / .36f, ey = (ry - .3f) / .48f, e = Mathf.Sqrt(ex * ex + ey * ey);
                return Mathf.Abs(e - 1) * .4f < W || Cizgi(rx, ry, 0, -.18f, 0, -.88f, W) || (e < 1 && (Mathf.Abs(rx) < .045f || Mathf.Abs(ry - .3f) < .045f));
            });
            // Arkadaş maçı (2026-11-21): iki kişi, arkadaki biraz küçük ve sağda (Kisi'nin biçimi; arada boşluk ki iki figür okunur).
            Arkadas = Ciz((x, y) =>
            {
                System.Func<float, float, float, bool> kisi = (cx, cy, k) =>
                {
                    float px = (x - cx) / k, py = (y - cy) / k; bool bas = (px * px + (py - .42f) * (py - .42f)) < .29f * .29f;
                    float sx = px / .72f, sy = (py + .82f) / .72f; return bas || (sx * sx + sy * sy < 1 && py > -.86f && py < .02f);
                };
                bool on = kisi(-.26f, -.06f, .78f), arka = kisi(.36f, .08f, .66f);
                bool kenar = kisi(-.26f, -.06f, .9f) && !on;   // öndekinin çevresinde ince boşluk: arkadaki onun arkasında kalır
                return on || (arka && !kenar && !on);
            });
            Arti = Ciz((x, y) => (Mathf.Abs(x) < .12f && Mathf.Abs(y) < .66f) || (Mathf.Abs(y) < .12f && Mathf.Abs(x) < .66f));
            // Kariyer yıldızları (2026-10-26): alınan dolu, alınmayan yalnız çerçeve (renk körlüğü: biçim farkı, renk değil).
            YildizDolu = Ciz((x, y) => Yildiz(x, y, .92f)); YildizBos = Ciz((x, y) => Yildiz(x, y, .92f) && !Yildiz(x, y, .58f));
            // Stat çubuğunun artı/eksi işareti (2026-10-26, ekipman v2 ▲ ▼): glif değil çizim (yazı tipinde olmayabilir); yönü
            // biçimle verir, rengi ek bilgidir (renk körlüğü).
            Yukari = Ciz((x, y) => y > -.55f && y < .6f && Mathf.Abs(x) < (.6f - y) * .62f); Asagi = Ciz((x, y) => y < .55f && y > -.6f && Mathf.Abs(x) < (y + .6f) * .62f);
            // Vitrin odak düğmeleri (2026-11-07 stüdyo incelemesi AD2): yazı yerine simge; "Face" kategori satırında da yazıyordu.
            // Yüz = halka baş + iki göz + gülüş; Ayak = ayakkabı silueti (taban + burun); Tutuş = sargılı raket sapı (eğik boşluklar).
            // 2026-11-19 Dolap "İncele" (büyüt): dört köşe ayracı, sistemin tam ekran simgesi kalıbı.
            Buyut = Ciz((x, y) => { float ax = Mathf.Abs(x), ay = Mathf.Abs(y); return ax > .25f && ay > .25f && ax < .75f && ay < .75f && (ax > .6f || ay > .6f); });
            Yuz = Ciz((x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y); bool halka = Mathf.Abs(r - .74f) < .12f;
                bool goz = ((x - .27f) * (x - .27f) + (y - .2f) * (y - .2f)) < .012f || ((x + .27f) * (x + .27f) + (y - .2f) * (y - .2f)) < .012f;
                float g = Mathf.Sqrt(x * x + (y + .02f) * (y + .02f)); bool gulus = y < -.12f && Mathf.Abs(g - .4f) < .07f && Mathf.Abs(x) < .32f;
                return halka || goz || gulus;
            });
            Ayak = Ciz((x, y) =>
            {
                bool taban = y > -.62f && y < -.42f && x > -.8f && x < .82f; bool topuk = x > -.8f && x < -.18f && y >= -.42f && y < .4f;
                float ex = (x - .12f) / .7f, ey = (y + .42f) / .42f; bool burun = y >= -.42f && ex * ex + ey * ey < 1;
                bool bag = Cizgi(x, y, -.1f, .08f, .18f, -.06f, .05f);
                return (taban || topuk || burun) && !bag;
            }, .06f, .14f);
            Tutus = Ciz((x, y) =>
            {
                bool sap = Mathf.Abs(x) < .24f && y > -.86f && y < .5f && Mathf.Repeat(y - x * .9f + 2, .3f) > .07f;
                bool govde = Mathf.Abs(x) < .08f && y >= .5f && y < .92f; bool bilezik = Mathf.Abs(x) < .3f && y > .44f && y < .56f;
                return sap || govde || bilezik;
            });
            // Alt sayfa kaydırma solması (2026-11-07 UX2): kart renginde (#261A30) aşağı doğru opaklaşan 1×32 şerit; içerik
            // sürüyorsa listenin altında durur ("Hard" yarım kesik görünüp hata sanılıyordu, Saha bölümü fark edilmiyordu).
            Solma = new Texture2D(1, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int i = 0; i < 32; i++) Solma.SetPixel(0, i, new Color(0x1A / 255f, 0x3F / 255f, 0x95 / 255f,   /* 2026-11-22 Stadyum Pop: alt sayfa #1A3F95 */ Mathf.SmoothStep(1, 0, i / 31f)));
            Solma.Apply(false, true);
            // Oyna sekmesinin zemini: USS'te gradyan yok. 2026-10-26: renk ve duraklar yön A'ya göre YayinDokulari'nda.
            Gradyan = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            YayinDokulari();
        }

        // ---- Yayın Gecesi dokuları ----
        // Cam plaka (2026-11-14 gece, docs/arayuz_tasarim_dili.md §6): menu_stil.uss --cam2 / --cam ile aynı iki değer; doku
        // dikey degrade (üst açık, alt koyu) ve yarı saydam, salonun ışığı arkadan sezilir. Opaklık .84/.80: yazı okunur kalır.
        static readonly Color CAM_UST = new Color(34 / 255f, 84 / 255f, 190 / 255f, .90f), CAM_ALT = new Color(18 / 255f, 46 / 255f, 126 / 255f, .86f);   // 2026-11-22 Stadyum Pop: lacivert-mavi panel
        static readonly Color NAVY = Kit.Hex("#1B4197"), NAVY2 = Kit.Hex("#1E4DB3"), INK = Kit.Hex("#163C8E");   // 2026-11-22 Stadyum Pop: INK = Oyna zemininin alt koyu mavisi   // 2026-10-27 yön C: kart, düğme, zemin (adlar A'dan)
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
            // 2026-10-29 Tüycükler: Egik artık eğik değil, yuvarlak köşeli jöle düğme (aşağıda); ad ve dilimler geriye uyum için aynı.
            // 2026-11-06 ince kenar (yasin: "profesyonel ve lightweight"): alttaki 5 pt koyu dudak kalktı (golge 0); derinlik degradeden.
            // 2026-11-22 Stadyum Pop: koyu sarı alt dudak geri geldi (boyun %10'u; 3B tıknaz düğme), kontur USS'te mürekkep.
            EgikLimon = Egik(96, 160, 28, UiKok.CTA_UST, UiKok.CTA_ORTA, UiKok.CTA_ALT, 16);
            // Etiket şeridi (a-kick "SIRADAKİ MAÇ"): düz limon, küçük eğim (−12°), 22 pt.
            // 2026-10-27 yön C: dolgusuz altın çerçeve + altın yazı (C taslağı .etk span); ekranda tek doygun dolgu OYNA kalsın.
            // 2026-10-29 Tüycükler: eğik şerit yerine hap (yarıçap 20 doku pikseli; USS .serit yan dilimi 22). Dolgusuz altın çerçeve aynı.
            EgikLimonKucuk = Sekil(48, 44, (x, y, ic) => YuvarlakIc(x, y, 48, 44, 20, ic),
                (x, y) => new Color(UiKok.ALTIN.r, UiKok.ALTIN.g, UiKok.ALTIN.b, .10f), new Color(UiKok.ALTIN.r, UiKok.ALTIN.g, UiKok.ALTIN.b, 1), 3);
            // Yayın plakası (a-plate): üç köşe 14 pt yuvarlak, sağ alt 18 pt kesik; soldan sağa navy2 → navy; 1 pt ince çerçeve.
            // 2026-10-29 Tüycükler: sağ alt kesik kalktı, dört köşe yuvarlak; altta 6 doku pikseli (3 pt) koyu "dudak" (jöle kart).
            Plaka = Sekil(96, 96, (x, y, ic) => PlakaIc(x, y, 96, 96, ic), (x, y) => Color.Lerp(CAM_ALT, CAM_UST, y / 95f), new Color(1, 1, 1, .16f), 2);   // 2026-11-06: alt dudak kalktı, eşit 1 pt kenar   // 2026-11-14 gece: cam (§6), üstten alta açık→koyu, kenar .16
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
            // 2026-10-29 (yasin: "uygulama içi sanat tasarımı splashteki loading'i örnek alacak"): açılış perdesinin güneş diski
            // tek dokuda (Hud.PERDE_C renkleri, 24 dilim, iç halkalar 24-gen; yarıçaplar Hud.PERDE_F'in iç halkaları, en dışta
            // ince gül halkası). Menü zemininde, Kariyer'in şimdiki basamak rozetinde aynı disk: açılıştan menüye tek resim dili.
            // Kenarlar 1 piksel yumuşatılır (perde dokusu keskin, ama orada 96 pt; burada ekran boyu).
            {
                const int n = 512, N = 24; var px2 = new Color32[n * n]; var R = new float[5]; R[0] = 1;
                for (int i = 1; i < 5; i++) R[i] = Hud.PERDE_F[i][0] / Hud.PERDE_F[1][0] * .87f;
                float sec = Mathf.PI * 2 / N, pk = 1.5f / n;
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                    {
                        float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx); if (a < 0) a += Mathf.PI * 2;
                        bool dilim = Mathf.FloorToInt(a / sec) % 2 == 0; float lok = Mathf.Repeat(a - Mathf.PI / N, sec) - sec / 2, rg = r * Mathf.Cos(lok) / Mathf.Cos(sec / 2);
                        Color c = Color.clear;   // dıştan içe: her halka bir öncekinin üstüne kenar yumuşatmasıyla biner (ara çizgi kalmaz)
                        for (int i = 0; i < 5; i++)
                        {
                            float ka = Mathf.Clamp01((R[i] - (i == 0 ? r : rg)) / pk + .5f); if (ka <= 0) break;
                            var ci = Kit.Hex(Hud.PERDE_C[i, dilim ? 1 : 0]); c = i == 0 ? new Color(ci.r, ci.g, ci.b, ka) : new Color(Mathf.Lerp(c.r, ci.r, ka), Mathf.Lerp(c.g, ci.g, ka), Mathf.Lerp(c.b, ci.b, ka), c.a);
                        }
                        px2[y * n + x] = c;
                    }
                GunesDisk = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                GunesDisk.SetPixels32(px2); GunesDisk.Apply(false, true);
            }
            // Raket izi (perde P_IZ #F9D272): soldan sağa belirip uca doğru parlayan altın çizgi; bant kenarının yanında.
            AltinIz = Sekil(128, 8, (x, y, ic) => true, (x, y) => { float k = x / 128f, d = 1 - Mathf.Abs(y - 4) / 4f; return new Color(UiKok.ALTIN.r, UiKok.ALTIN.g, UiKok.ALTIN.b, k * k * Mathf.Clamp01(d * 1.6f)); }, Color.clear, 0);
        }
        static readonly Color LIMON_D = Kit.Hex("#FFC93C");   // 2026-11-22 Stadyum Pop sarı   // 2026-10-27 yön C: şimdiki basamak deseni turuncu (seçili/şimdi rengi); ad A'dan
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
        public static Texture2D EgikDugme(int w, int h, int egim) { return Egik(w, h, egim, UiKok.CTA_UST, UiKok.CTA_ORTA, UiKok.CTA_ALT, 0); }   // 2026-11-06: menüyle aynı, dudaksız   // 2026-10-27 yön C degradesi
        // 2026-10-29 Tüycükler (yasin: menüler neşeli dilde, "sadece renk değil"): eskiden eğik dikdörtgendi (sol ve sağ kenar
        // egim kadar yatık). Şimdi yuvarlak köşeli (yarıçap egim + 4, yan dilim egim + 4'e sığar; IMGUI çağıranı aynı kalır),
        // gradyan aynı; altta golge kadar koyu "dudak" (%30 siyah, eski iç gölgeden belirgin): düğme kabarık ve basılabilir okunur.
        static Texture2D Egik(int w, int h, int egim, Color ust, Color orta, Color alt, int golge)
        {
            float r = Mathf.Min(egim + 4, h / 2f, w / 2f);
            return Sekil(w, h, (x, y, ic) => YuvarlakIc(x, y, w, h, r, ic), (x, y) =>
            {
                float v = 1 - y / (float)h; Color c = v < .55f ? Color.Lerp(ust, orta, v / .55f) : Color.Lerp(orta, alt, (v - .55f) / .45f);
                if (golge > 0 && y < golge) c = Color.Lerp(c, Color.black, .30f); return c;
            }, Color.clear, 0);
        }
        static bool YuvarlakIc(float x, float y, float w, float h, float r, float ic)
        {
            if (x < ic || x > w - ic || y < ic || y > h - ic) return false;
            float cx = Mathf.Clamp(x, r, w - r), cy = Mathf.Clamp(y, r, h - r), dx = x - cx, dy = y - cy; return dx * dx + dy * dy <= (r - ic) * (r - ic);
        }
        static bool PlakaIc(float x, float y, int w, int h, float ic) { return YuvarlakIc(x, y, w, h, 28, ic); }   // 2026-10-29: kesik köşe kalktı
        // Şekil dokusu: ic(x, y, içe pay) şeklin içinde mi; renk(x, y) dolgu; cerceveKalin > 0 ise kenarda ince çerçeve.
        // 2×2 örnekleme ile kenar yumuşatma. y aşağıdan yukarı (Texture2D satır sırası).
        // Apply(false, true): GPU'ya yüklenince CPU kopyası bırakılır (2026-10-27 performans; dokular bir kez çizilir, okunmaz).
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
            t.SetPixels32(px); t.Apply(false, true); return t;
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
        // Çoklu çizgi (sekme simgeleri): noktalar x0,y0,x1,y1…; kapali ise son nokta ilkine bağlanır.
        static bool Hat(float x, float y, float yari, bool kapali, params float[] p)
        {
            int n = p.Length / 2, m = kapali ? n : n - 1;
            for (int i = 0; i < m; i++) { int j = (i + 1) % n; if (Cizgi(x, y, p[2 * i], p[2 * i + 1], p[2 * j], p[2 * j + 1], yari)) return true; }
            return false;
        }
        static bool Cizgi(float x, float y, float ax, float ay, float bx, float by, float yari)
        {
            float vx = bx - ax, vy = by - ay, wx = x - ax, wy = y - ay; float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy));
            float dx = wx - vx * t, dy = wy - vy * t; return dx * dx + dy * dy < yari * yari;
        }
        // Kaydırılmış çizim (2026-11-13, yasin: "menülerdeki buton içerisindeki resource'ların kayıp kaymadığını görüntüle"): ayakkabı,
        // kilit, tişört ve kişi simgeleri kendi çerçevelerinde aşağıda (ayakkabı sol altta) çiziliyordu; 44 pt'lik kare düğmede ve
        // kariyer basamağı halkasında simge ortadan kaymış görünüyordu. Biçim aynı kalır, sınır kutusu ortaya taşınır (dx, dy birim
        // çerçevede; ölçü: tool/unity_test yerine 200×200 örnekle bbox merkezi).
        static Texture2D Ciz(Func<float, float, bool> ic, float dx, float dy) { return Ciz((x, y) => ic(x - dx, y - dy)); }
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
            t.SetPixels32(px); t.Apply(false, true); return t;
        }
    }
}
