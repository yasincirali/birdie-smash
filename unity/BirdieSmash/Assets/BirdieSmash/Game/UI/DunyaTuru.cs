// Kariyer "Dünya Turu" (2026-11-22, yasin seçimi; artifact 19MR43zwtCSMgJxmpkLeNM "önerin gibi ilerle": A Dünya Turu + B'nin durak
// ödülleri + finallerde C'nin turnuva tablosu). Kalıp kaynağı Tennis Clash / Golf Clash: kariyer şehir şehir bir dünya turu, her tur
// kendi mekânının görseliyle büyük bir kart, yeni şehri açmak hedef. 11-19'daki tek başlık ‹ Tur n › yerine sekiz tur kartı yatay
// şeritte yan yana: oyuncu nerede olduğunu, neyin bittiğini ve sıradakinin neye benzediğini tek bakışta görür (oklarla tek tek
// gezmek gerekmez). Kart dokunuşu turu seçer (KariyerTurSec, eski okların işi); seçili kart turuncu çerçeve + ✓ değil, "Seçili"
// erişim adı ve çerçeve kalınlığıyla da ayrılır. Kilitli tur da seçilip incelenebilir (eski davranış).
// Mekân görseli doku değil, Painter2D ile çizilen basit sahne (gökyüzü bantları, tohumlu şehir silueti ya da salon tavanı/tribün,
// perspektif kort ve file): yeni varlık dosyası yok, boyut büyümez, her tur hep aynı görünür. Yalnız yerleşim değişince çizilir.
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    // Tur başına sahne: gökyüzü üst/alt, siluet rengi, salon mu, güneş/ay. Sıra kariyer.json tur sırasıyla aynı.
    public class MekanSahne : VisualElement
    {
        struct Tema { public Color ust, alt, siluet, pencere, zemin, kort; public bool salon, gunes; public int tohum; }
        static Tema T(string u, string a, string s, string p, string z, string k, bool salon, bool gunes, int tohum)
        { return new Tema { ust = Kit.Hex(u), alt = Kit.Hex(a), siluet = Kit.Hex(s), pencere = Kit.Hex(p), zemin = Kit.Hex(z), kort = Kit.Hex(k), salon = salon, gunes = gunes, tohum = tohum }; }
        static readonly Tema[] TEMA = {
            T("#3A2A24", "#6B4A35", "#2A1E19", "#FFE7B0", "#B9824C", "#2F8A57", true, false, 3),     // Yerel yol: mahalle salonu, ahşap zemin
            T("#FF9A76", "#FFD3A5", "#6B3A5A", "#FFE9C8", "#3E9A5C", "#2F8A57", false, true, 11),   // Asya: gün batımı
            T("#6C86A8", "#C7D3E0", "#3B4F6B", "#FFFFFF", "#4A7E6A", "#2F6A57", false, false, 5),  // Avrupa: bulutlu gri-mavi
            T("#0E1433", "#26306A", "#151B40", "#FFE9B0", "#2A3570", "#3A5FCF", true, false, 9),   // Dünya Finalleri: gece arenası
            T("#4FB3E8", "#BDE7F7", "#2A6F8F", "#FFFFFF", "#3E9A5C", "#2F8A57", false, true, 21),  // Pasifik: açık gün
            T("#F2784B", "#FFC56E", "#5A2E3A", "#FFE1A8", "#3E8A5C", "#2F7A57", false, true, 17),  // Amerika: akşam
            T("#1E1633", "#4A3566", "#2A1F44", "#F9D272", "#2E2850", "#3A5FCF", false, false, 13), // Büyük Kupa: gece, altın pencereler
            T("#2BB3A3", "#BFF0E4", "#1C5E57", "#FFFFFF", "#3E9A6C", "#2F8A67", false, true, 7) }; // Efsaneler: Bali
        readonly int tur;
        public MekanSahne(int tur) { this.tur = Mathf.Clamp(tur, 0, TEMA.Length - 1); pickingMode = PickingMode.Ignore; generateVisualContent += Ciz; }

        void Ciz(MeshGenerationContext mgc)
        {
            float W = contentRect.width, H = contentRect.height; if (float.IsNaN(W) || W < 2 || H < 2) return;
            var t = TEMA[tur]; var pz = mgc.painter2D; uint s = (uint)(t.tohum * 2654435761u + 1);
            System.Func<float> r = () => { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s % 10000) / 10000f; };
            float hz = H * .55f;
            for (int i = 0; i < 10; i++) Dik(pz, 0, hz * i / 10f, W, hz / 10f + 1, Color.Lerp(t.ust, t.alt, i / 9f));   // gökyüzü: 10 bant, Painter2D degrade çizmez
            if (t.salon)
            {
                for (int i = 0; i < 6; i++) { float x = W * (i + .5f) / 6; Daire(pz, new Vector2(x, hz * .18f), 4, t.pencere); }   // tavan ışıkları
                for (int row = 0; row < 4; row++) for (float x = 3; x < W; x += 6 + r() * 3) Daire(pz, new Vector2(x, hz * .5f + row * hz * .11f + r() * 2), 1.5f + r(), new Color(1, 1, 1, .25f + r() * .3f));   // tribün
            }
            else
            {
                if (t.gunes) Daire(pz, new Vector2(W * .8f, hz * .3f), H * .1f, new Color(1, .95f, .8f, .85f));
                float x = 0; while (x < W) { float w = 10 + r() * 20, h = hz * (.2f + r() * .5f); Dik(pz, x, hz - h, w, h, t.siluet); if (r() > .55f) for (int k = 0; k < 4; k++) Dik(pz, x + 3 + (k % 2) * 6, hz - h + 5 + (k / 2) * 7, 2.5f, 3.5f, t.pencere * new Color(1, 1, 1, .5f)); x += w + 1; }
            }
            Dik(pz, 0, hz, W, H - hz, t.zemin);
            // perspektif kort: üst kenar dar, alt kenar ekran dışına taşar; orta çizgi, servis çizgisi, file
            Vector2 a = new Vector2(W * .22f, hz + 3), b = new Vector2(W * .78f, hz + 3), c = new Vector2(W * 1.12f, H), d = new Vector2(-W * .12f, H);
            pz.fillColor = t.kort; pz.BeginPath(); pz.MoveTo(a); pz.LineTo(b); pz.LineTo(c); pz.LineTo(d); pz.ClosePath(); pz.Fill();
            pz.strokeColor = new Color(1, 1, 1, .8f); pz.lineWidth = 1.5f;
            pz.BeginPath(); pz.MoveTo(a); pz.LineTo(b); pz.LineTo(c); pz.LineTo(d); pz.ClosePath(); pz.Stroke();
            pz.BeginPath(); pz.MoveTo(new Vector2(W * .5f, hz + 3)); pz.LineTo(new Vector2(W * .5f, H)); pz.Stroke();
            float fy = hz + (H - hz) * .35f; Dik(pz, W * .12f, fy, W * .76f, 6, new Color(1, 1, 1, .22f));
            pz.BeginPath(); pz.MoveTo(new Vector2(W * .12f, fy)); pz.LineTo(new Vector2(W * .88f, fy)); pz.Stroke();
        }
        static void Dik(Painter2D pz, float x, float y, float w, float h, Color c)
        { pz.fillColor = c; pz.BeginPath(); pz.MoveTo(new Vector2(x, y)); pz.LineTo(new Vector2(x + w, y)); pz.LineTo(new Vector2(x + w, y + h)); pz.LineTo(new Vector2(x, y + h)); pz.ClosePath(); pz.Fill(); }
        static void Daire(Painter2D pz, Vector2 m, float rr, Color c)
        { pz.fillColor = c; pz.BeginPath(); pz.Arc(m, rr, Angle.Degrees(0), Angle.Degrees(360)); pz.ClosePath(); pz.Fill(); }
    }

    public partial class UiKok
    {
        // 2026-11-23 (yasin "Career akışını hiç beğenmiyorum; önündeki turnuva ya da yarışları daha net görmeli, ilerleme ux tasarımı
        // çok çirkin"; seçenek sayfası Q3WbQ4HGx1Pv6VhfwXpBpV, Kariyer seçilmedi → önerilen A "Sıradaki maç önde"). 11-22'deki sekiz
        // geniş tur kartı şeridi sayfanın yarısını kaplıyor, sıradaki maç en altta kesik kalıyordu; bir kartta beş ilerleme ölçüsü üst
        // üste biniyordu (6/6 maç, 15/18 yıldız, çubuk, mühür, "8 yükseltme eksik"). Şimdi üç blok, yukarıdan aşağı:
        // 1) sıradaki maç kartı (KariyerSiradaki): mekân sahnesi, sen ve rakip yüzü, tek Oyna;
        // 2) seçili turun duraklar çizgisi (TurAltKur): altı durak bağlı bir çizgide, altında Grup–Çeyrek–Yarı–Final etiketleri;
        //    ilerleme tek ölçü "Durak n/6"; yıldız ve mühür bu sayfadan kalktı (maç kartında ve maç sonucunda duruyor);
        // 3) sekiz tur kutusu (TurKutular): dokunulan tur 2. bloğa gelir.
        // Mekân sahnesi (MekanSahne) ve durak ödülü (DurakOdul) 11-22 Dünya Turu'ndan aynen kaldı.
        VisualElement turKutular, turAltKap;

        // Sıradaki maç: kariyerin tek birincil eylemi sayfanın en üstünde (eski sabit "Sıradaki maç" şeridinin ve en alttaki kesik
        // durağın yerine). Oyna doğrudan maça gider (eski yol şerit → maç kartı → Oyna iki dokunuştu); zorluk/yıldız görevi
        // görmek isteyen yüzler satırına dokunur, maç kartı açılır. Kariyer bitince Usta yolu kartı bu yerde.
        VisualElement KariyerSiradaki(Ilerleme p, int sira)
        {
            if (p.HepsiBitti || sira < 0) return UstaKart();
            int tr = KariyerVeri.TurOf(sira); bool veri = tr < KariyerVeri.Turlar.Length && KariyerVeri.Turlar[tr].Var;
            var k = Kart("kar-hero");
            var sh = new VisualElement(); sh.AddToClassList("kar-hero-sahne"); sh.pickingMode = PickingMode.Ignore; k.Add(sh);
            sh.Add(new MekanSahne(tr) { style = { position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0 } });
            var no = Yazi(L.T("Tur {0}", tr + 1), 12, "kar-hero-no"); sh.Add(no);
            var gv = new VisualElement(); gv.AddToClassList("kar-hero-govde"); k.Add(gv);
            if (!veri || !p.Acik(sira))
            {
                var ad = Yazi(L.T(TurAd(tr)), 20, "kalin"); SozcukSigdir(ad); gv.Add(ad);
                var ky = Yazi(KilitNedeni(tr), 14, "soluk"); ky.style.whiteSpace = WhiteSpace.Normal; gv.Add(ky);
                Erisim.Etiket(gv, L.T(TurAd(tr)) + ", " + KilitNedeni(tr), "metin");
                return k;
            }
            var kb = KariyerVeri.Al(sira); var st = KariyerVeri.Rakip(sira);
            var ust = new VisualElement(); ust.AddToClassList("kar-hero-ust"); gv.Add(ust);
            var et = Yazi(L.T(TurAd(tr)) + " · " + L.T(kb.yer), 13, "soluk"); et.AddToClassList("esnek-yatay-sifir"); et.style.whiteSpace = WhiteSpace.Normal; ust.Add(et);
            var od = DurakOdul(sira); if (od != null) ust.Add(od);
            // Sen – VS – rakip: sen limon halkayla, rakip orkide (rol renkleri §8); adlar yazıyla da var, renk tek başına anlam taşımaz.
            // VS beyaz + mürekkep konturu: sarı yalnız Oyna'da kalsın (ekranda tek sarı eylem).
            var vs = new VisualElement(); vs.AddToClassList("kar-vs"); vs.AddToClassList("dokunulur"); gv.Add(vs);
            var ben = new VisualElement(); ben.AddToClassList("kar-yuz"); ben.AddToClassList("kar-yuz--sen"); ben.style.backgroundColor = Kit.Hex(Ayar.FORMA); ben.pickingMode = PickingMode.Ignore; AvatarIc(ben, Portre.Benim(), 26); vs.Add(ben);
            var sa = new VisualElement(); sa.AddToClassList("kar-vs-ad"); sa.pickingMode = PickingMode.Ignore; vs.Add(sa); sa.Add(Yazi(L.T("Sen"), 16, "kalin"));
            var v = Yazi("VS", 20, "kar-vs-yazi"); vs.Add(v);   // r260: ortak .vs sınıfı mutlak konumlu (Oyna sahnesi) ve büyük yazıda gizli; burada akışta kendi sınıfı
            var ra = new VisualElement(); ra.AddToClassList("kar-vs-ad"); ra.AddToClassList("kar-vs-ad--rakip"); ra.pickingMode = PickingMode.Ignore; vs.Add(ra);
            var rn = Yazi(st.isim, 16, "kalin"); SozcukSigdir(rn); ra.Add(rn); var rt = Yazi(L.T(st.ad), 12, "soluk"); SozcukSigdir(rt); ra.Add(rt);
            var rav = new VisualElement(); rav.AddToClassList("kar-yuz"); rav.AddToClassList("kar-yuz--rakip"); rav.style.backgroundColor = Kit.Hex(st.renk); rav.pickingMode = PickingMode.Ignore; vs.Add(rav);
            RakipAvatarIc(rav, st, 26, 20);
            vs.AddManipulator(new Clickable(() => { Tik(vs); MacKartAc(sira); }));
            Erisim.Etiket(vs, L.T("Maç ayrıntısı") + ": " + L.T("Sen") + ", " + st.isim + ", " + L.T(st.ad), "dugme");
            var oy = Dugme(L.T("Oyna"), () => KariyerOyna(sira), "dugme--birincil", null, 19); oy.AddToClassList("kar-oyna");
            Erisim.Etiket(oy, L.T("Oyna") + ": " + L.T(kb.yer) + ", " + st.isim, "dugme"); gv.Add(oy);
            return k;
        }

        // Tur kutusu dokunuşu sekmeyi baştan kurmaz (11-22 TurSecYerinde kalıbı): kutuların seçili sınıfı değişir, yalnız duraklar
        // bloğu yeniden kurulur, .12 sn belirir ve görünür alana gelir (kutular sayfanın dibinde, blok üstünde). Basamak animasyonu
        // yeniden oynamaz. Kutular panelde değilse eski SekmeKur yolu.
        void TurSecYerinde(int t)
        {
            var iz = turKutular?.Q(null, "kar-tur-izgara");
            if (iz == null || turAltKap == null || turAltKap.panel == null || t < 0 || t >= iz.childCount) { KariyerTurSec(t); return; }
            int once = kariyerTur; kariyerTur = t;
            if (once >= 0 && once < iz.childCount) KutuSecili(iz[once], false); KutuSecili(iz[t], true);
            basAniDen = basAniA = -1;
            TurAltKur(turAltKap, t, Ayar.ilerleme.Siradaki(), true);
            var sv = turAltKap.GetFirstAncestorOfType<ScrollView>(); var hedef = turAltKap;
            if (sv != null) sv.schedule.Execute(() => { if (hedef.panel != null) sv.ScrollTo(hedef); }).StartingIn(32);
        }
        static void KutuSecili(VisualElement k, bool s)
        {
            k.EnableInClassList("kar-tur-kutu--secili", s);
            if (k.userData is ErisimBilgi b) { string sec = ", " + L.T("Seçili"); if (b.etiket.EndsWith(sec)) b.etiket = b.etiket.Substring(0, b.etiket.Length - sec.Length); if (s) b.etiket += sec; b.secili = s; }
        }

        // Seçili turun bloğu: başlık "Tur n · ad" + tek ölçü (Durak n/6 ya da Tamamlandı), hikâye/hazırlık, duraklar çizgisi;
        // son turda altında Usta yolu (kariyer bitmediyse; bittiyse Usta kartı en üstte).
        // sol: içerik yeni iç kapta solgun doğar ve 16 ms sonra belirir (ekrandaki kaba eklense önce 1→0 sönerdi; AntrenmanEkran kalıbı).
        void TurAltKur(VisualElement kap, int tr, int sira, bool sol = false)
        {
            kap.Clear();
            if (sol && !Ayar.reduce) { var ic = new VisualElement(); ic.AddToClassList("koz-solgun"); ic.schedule.Execute(() => ic.RemoveFromClassList("koz-solgun")).StartingIn(16); kap.Add(ic); kap = ic; }
            var p = Ayar.ilerleme; var tv = tr < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[tr] : null; bool veri = tv != null && tv.Var, acik = p.TurAcik(tr);
            var kart = Kart("kar-tur"); kap.Add(kart);
            var bas = new VisualElement(); bas.AddToClassList("kar-tur-bas"); kart.Add(bas);
            var ad = Yazi(L.T("Tur {0}", tr + 1) + " · " + L.T(TurAd(tr)), 17, "kalin"); ad.AddToClassList("esnek-yatay-sifir"); ad.style.whiteSpace = WhiteSpace.Normal; bas.Add(ad);
            string olcu = null;
            if (veri && acik)
            {
                int kaz = 0; for (int b = 0; b < KariyerVeri.BASAMAK; b++) if (p.Kazanildi(KariyerVeri.Global(tr, b))) kaz++;
                olcu = kaz >= KariyerVeri.BASAMAK ? L.T("Tamamlandı") : L.T("Durak {0}/{1}", kaz + 1, KariyerVeri.BASAMAK);
                var ol = Yazi(olcu, 14, kaz >= KariyerVeri.BASAMAK ? "altin" : "kalin"); ol.AddToClassList("kar-tur-olcu"); bas.Add(ol);
            }
            Erisim.Etiket(bas, ad.text + (olcu != null ? ", " + olcu : ""), "baslik");
            kart.Add(TurAlt(tr, tv));
            if (!acik) { var ky = Yazi(KilitNedeni(tr), 13, "soluk"); ky.AddToClassList("kar-tur-not"); ky.style.whiteSpace = WhiteSpace.Normal; kart.Add(ky); }
            if (veri) kart.Add(TurDuraklar(tr, sira));
            if (veri && tr == KariyerVeri.TUR - 1 && !p.HepsiBitti) kap.Add(UstaKart());   // 2026-10-26: Usta yolu son turun altında
        }

        // Duraklar çizgisi: altı düğüm yan yana (BasamakNokta: geçilen ✓, şimdiki sarı halka + yüz, açık numara, kilitli kilit,
        // final kupa; dokununca o durağın maç kartı), arkalarında tek düz çizgi (YolCiz, geçilen kesit altın). Altında aşama
        // etiketleri düğüm gruplarına yayılır: grup turunda Grup (1–3) · Çeyrek · Yarı · Final, diğerlerinde Duraklar (1–4) · Yarı ·
        // Final (kariyer.json'da 5. basamak yarı final, 6. final). Şimdiki aşama sarı + kalın (yalnız renk değil). Düğüm 44 pt sabit:
        // en dar telefonda (320 pt) altı düğüm sığsın diye kart iç boşluğu 6.
        VisualElement TurDuraklar(int t, int sira)
        {
            var p = Ayar.ilerleme; var kap = new VisualElement(); kap.AddToClassList("kar-adim");
            var cizgi = new VisualElement(); cizgi.AddToClassList("kar-adim-cizgi"); cizgi.pickingMode = PickingMode.Ignore; kap.Add(cizgi);
            var sr = new VisualElement(); sr.AddToClassList("kar-adim-dugumler"); kap.Add(sr);
            var dugumler = new System.Collections.Generic.List<VisualElement>(); int gecilen = -1;
            for (int b = 0; b < KariyerVeri.BASAMAK; b++)
            {
                int g = KariyerVeri.Global(t, b); var slot = new VisualElement(); slot.AddToClassList("kar-adim-slot"); sr.Add(slot);
                var dg = BasamakNokta(g, sira, slot); slot.Add(dg); dugumler.Add(dg); if (p.Kazanildi(g)) gecilen = b;
            }
            int gc = gecilen;
            cizgi.generateVisualContent += mgc => YolCiz(mgc, cizgi, dugumler, gc);
            kap.RegisterCallback<GeometryChangedEvent>(e => cizgi.MarkDirtyRepaint());
            bool grup = GrupTuru(t);
            string[] ad = grup ? new[] { "Grup", "Çeyrek", "Yarı", "Final" } : new[] { "Duraklar", "Yarı", "Final" };
            int[] n = grup ? new[] { 3, 1, 1, 1 } : new[] { 4, 1, 1 };
            var et = new VisualElement(); et.AddToClassList("kar-adim-etiket"); et.pickingMode = PickingMode.Ignore; kap.Add(et);
            for (int i = 0, b0 = 0; i < ad.Length; b0 += n[i], i++)
            {
                bool simdi = false, tamam = true, acik = false;
                for (int b = b0; b < b0 + n[i]; b++) { int g = KariyerVeri.Global(t, b); if (!p.Kazanildi(g)) tamam = false; if (p.Acik(g)) acik = true; if (g == sira && p.Acik(g)) simdi = true; }
                var l = Yazi(L.T(ad[i]), 12, simdi ? "durum-simdi" : tamam || acik ? "kalin" : "soluk"); if (simdi) l.AddToClassList("kalin");
                l.style.flexGrow = n[i]; l.style.flexBasis = 0; l.style.unityTextAlign = TextAnchor.MiddleCenter; SozcukSigdir(l); et.Add(l);
            }
            return kap;
        }

        // Sekiz tur kutusu (4 × 2; büyük yazıda 2 sütun): rozet (✓ bitti, sarı numara = buradasın, numara = açık, kilit) + tur adı.
        // Seçili kutu beyaz çerçeve (sarı "şimdi"ye ayrıldı) ve "Seçili" erişim adı. Başlık "Dünya turu n / 8" = bulunduğun tur.
        VisualElement TurKutular(int secili, int sira)
        {
            var p = Ayar.ilerleme; var kap = Kart("kar-turlar");
            int simdi = p.HepsiBitti || sira < 0 ? KariyerVeri.TUR : KariyerVeri.TurOf(sira) + 1;
            var bas = new VisualElement(); bas.AddToClassList("kar-tur-bas"); kap.Add(bas);
            var ba = Yazi(L.T("Dünya turu"), 17, "kalin"); ba.AddToClassList("esnek-yatay-sifir"); bas.Add(ba);
            var sy = Yazi(Mathf.Min(simdi, KariyerVeri.TUR) + " / " + KariyerVeri.TUR, 14, "soluk"); sy.AddToClassList("kar-tur-olcu"); bas.Add(sy);
            Erisim.Etiket(bas, L.T("Dünya turu") + ", " + L.T("Tur {0}/{1}", Mathf.Min(simdi, KariyerVeri.TUR), KariyerVeri.TUR), "baslik");
            var iz = new VisualElement(); iz.AddToClassList("kar-tur-izgara"); kap.Add(iz);
            for (int t = 0; t < KariyerVeri.TUR; t++) iz.Add(TurKutu(t, t == secili, sira));
            return kap;
        }
        VisualElement TurKutu(int t, bool secili, int sira)
        {
            var p = Ayar.ilerleme; bool acik = p.TurAcik(t); var tv = t < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[t] : null; bool veri = tv != null && tv.Var;
            int kaz = 0; if (veri) for (int b = 0; b < KariyerVeri.BASAMAK; b++) if (p.Kazanildi(KariyerVeri.Global(t, b))) kaz++;
            bool bitti = veri && kaz == KariyerVeri.BASAMAK, burada = veri && sira >= 0 && KariyerVeri.TurOf(sira) == t && !bitti;
            var k = new VisualElement(); k.AddToClassList("kar-tur-kutu"); k.AddToClassList("dokunulur");
            if (secili) k.AddToClassList("kar-tur-kutu--secili"); if (!acik) k.AddToClassList("kar-tur-kutu--kilit");
            var r = new VisualElement(); r.AddToClassList("kar-tur-rozet"); r.pickingMode = PickingMode.Ignore; k.Add(r);
            r.AddToClassList(bitti ? "kar-tur-rozet--bitti" : burada ? "kar-tur-rozet--simdi" : acik ? "kar-tur-rozet--acik" : "kar-tur-rozet--kilit");
            if (bitti) r.Add(Ikon(Ikonlar.Tik, 16, KOYU));
            else if (!acik) r.Add(Ikon(Ikonlar.Kilit, 14, SOLUK));
            else
            {
                // rozet 30 pt sabit; rakam en çok 18 pt (BasamakNokta'daki 44 pt halka kuralı), daralmaz, ortada
                var nl = Yazi((t + 1).ToString(), 14, "kalin"); nl.style.fontSize = Mathf.Min(nl.style.fontSize.value.value, 18f); nl.style.flexShrink = 0f;
                nl.style.paddingTop = 0f; nl.style.paddingBottom = 0f; nl.style.unityTextAlign = TextAnchor.MiddleCenter; nl.style.color = burada ? KOYU : YAZI; r.Add(nl);
            }
            var ad = Yazi(L.T(TurAd(t)), 12, "kar-tur-ad"); ad.pickingMode = PickingMode.Ignore; SozcukSigdir(ad); k.Add(ad);
            int tt = t; k.AddManipulator(new Clickable(() => { Tik(k); if (tt != kariyerTur) TurSecYerinde(tt); }));
            string d = L.T(bitti ? "Bitti" : burada ? "Buradasın" : acik ? "Açık" : "Kilitli");
            Erisim.Etiket(k, L.T("Tur {0}/{1}", t + 1, KariyerVeri.TUR) + ", " + L.T(TurAd(t)) + ", " + d + (secili ? ", " + L.T("Seçili") : ""), "dugme", secili);
            return k;
        }

        // Seçili turun altındaki tek satır: hikâye + (eksikse) hazırlık. Ad ve ölçü başlıkta; burada tekrar edilmez.
        VisualElement TurAlt(int t, KariyerTur tv)
        {
            var kb = new VisualElement(); kb.AddToClassList("tur-alt");
            if (tv != null && tv.hikaye.Length > 0) { var hy = Yazi(L.T(tv.hikaye), 14, "soluk"); hy.AddToClassList("tur-hikaye"); kb.Add(hy); }
            if (Ayar.ilerleme.TurAcik(t) && tv != null)
            {
                int ep = EkipmanPuani();
                if (ep < tv.epMin)
                {
                    var hz = Satir("arali"); hz.style.alignItems = Align.Center; kb.Add(hz);
                    // 2026-11-21 inceleme (#4): tek sayı; "Ekipman 0/3 · 3 yükseltme eksik" aynı eksiği iki kez söylüyordu.
                    var ek = Yazi(L.T("{0} yükseltme eksik", tv.epMin - ep), 13, "durum-simdi"); ek.AddToClassList("esnek-yatay"); SozcukSigdir(ek); hz.Add(ek);
                    hz.Add(Dugme(L.T("Atölye'ye git"), () => SekmeAc(Sekme.Atolye), "kucuk-dugme"));
                }
            }
            return kb;
        }

        // Grup turu (Dünya Finalleri, Efsaneler Kupası): ilk basamağın yeri "grup" içeriyorsa (kariyer.json ham verisi, çeviri
        // anahtarı değil).
        static bool GrupTuru(int t) { if (t >= KariyerVeri.Turlar.Length || !KariyerVeri.Turlar[t].Var) return false; var y = KariyerVeri.Al(KariyerVeri.Global(t, 0)).yer; return y != null && y.Contains("grup"); }

        // Durak ödülü (B'nin kalıbı: her duraktaki kazanç önceden görünür). Değerler Core/Gear.Odul'den: basamağın ilk galibiyeti +60 Tüy,
        // ilk kez alınan her yıldız +20. Uydurma sayı yok; maçın kendi galibiyet Tüy'ü (uzunluğa göre) burada yazılmaz, maç kartında.
        // 2026-11-23: rota kalktı; ödül sıradaki maç kartının üst satırında (final durağında yok: ödülü tur sonu töreni söyler).
        VisualElement DurakOdul(int g)
        {
            var p = Ayar.ilerleme; if (KariyerVeri.Final(g) && KariyerVeri.BasOf(g) == KariyerVeri.BASAMAK - 1 && !p.Kazanildi(g)) return null;
            int eksik = 0; for (int i = 0; i < 3; i++) if ((p.Maske(g) & (1 << i)) == 0) eksik++;
            string y; if (!p.Kazanildi(g)) y = "+60"; else if (eksik > 0) y = "+" + 20 * eksik; else return null;
            var r = Satir("durak-odul"); r.pickingMode = PickingMode.Ignore; r.Add(Ikon(Ikonlar.Tuy, 13, ALTIN)); r.Add(Yazi(y, 12, "altin"));
            Erisim.Etiket(r, p.Kazanildi(g) ? L.T("Eksik yıldızlar {0} Tüy", 20 * eksik) : L.T("İlk galibiyet 60 Tüy"), "metin");
            return r;
        }
    }
}
