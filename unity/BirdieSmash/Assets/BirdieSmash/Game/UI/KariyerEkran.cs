// Kariyer haritası (komple-oyun tasarımı §2.3, §3.1; adım 2, 2026-10-26). Adım 1'in düz listesinin yerini aldı.
// Yerleşim (2026-11-07, yatay tur kartları; önceki dikey yol haritasının yerini aldı): üstte başlık ve toplam yıldız, altında
// yatay kaydırılan sekiz tur kartı (tur adı, yıldız, tema, altı basamak noktası, şimdiki maç ya da mühüre kalan yıldız), altında
// seçili turun açık maç kartı (rakip, ipucu, zorluk, Oyna). Basamak noktasına dokunmak maç kartını alt sayfa olarak açar (MacKartKur).
// Durum yalnız renkle verilmez (HIG renk körlüğü): geçilen = ✓, şimdiki = güneş diski + numara + "Şimdi: …" satırı, kilitli = kilit,
// final = kupa; ekran okuyucu her noktada "Geçildi/Şimdi/Açık/Kilitli" der; yıldızlar dolu/çerçeve biçimiyle ve "2/3" yazısıyla.
// Büyük yazıda (×1,35 üstü) kart şeridin %90'ı; basamak noktaları sığmazsa ikinci satıra sarar.
// Tur seçimi sekmenin durumudur (tasarım §2.2: Tur 3'e bakıp Oyna'ya geçen döndüğünde Tur 3'ü görür); maçtan dönünce
// sıradaki basamağın turuna döner (yeni tur açılmış olabilir).
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        int kariyerTur = -1, kartBasamak; float kariyerY = -1;

        void KariyerKur(VisualElement k)
        {
            k.Clear(); var sv = Kaydirma(k); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            var p = Ayar.ilerleme; int sira = p.Siradaki();
            if (kariyerTur < 0 || kariyerTur >= KariyerVeri.TUR) kariyerTur = KariyerVeri.TurOf(sira);
            var bas = Satir("arali"); c.Add(bas);
            var t = Yazi(L.T("Kariyer"), 24, "sekme-baslik"); Erisim.Etiket(t, L.T("Kariyer"), "baslik"); bas.Add(t);
            bas.Add(YildizSayac(p.ToplamYildiz, TurSayisiVeri() * 18, 15));
            // Career.Line satırı kalktı (fikir 22:10 #2): aynı maç başlık altında, tur kartında, maç kartında ve düğmede dört kez yazıyordu.

            // 2026-11-07 (yasin, Dolap/Kariyer alternatifleri, seçim "A: yatay tur kartları"): çip satırı + tur kartı + altı düğümlük
            // dikey yol yerine her tur bir kart, yatay kaydırılır; kartta altı basamak tek satırda (dokununca maç kartı). Altında
            // seçili turun sıradaki maçı doğrudan açık: maça başlamak düğüm → kart → Oyna yerine tek dokunuş. Uzun dikey liste yok.
            var hs = new ScrollView(ScrollViewMode.Horizontal); hs.AddToClassList("tur-serit"); hs.horizontalScrollerVisibility = ScrollerVisibility.Hidden; c.Add(hs);
            var kartlar = new System.Collections.Generic.List<VisualElement>();
            for (int i = 0; i < KariyerVeri.TUR; i++) { var tk = TurKart(i, sira, hs); hs.Add(tk); kartlar.Add(tk); }
            TurSeritYerlestir(hs, kartlar, sv);

            int tr = kariyerTur; var tv = tr < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[tr] : null; bool veri = tv != null && tv.Var;
            // Başka bir turu incelerken sabit şerit oyuncuyu kendi turunun maçına döndürür (UX6); kendi turunda maç zaten aşağıda açık.
            if (tr != KariyerVeri.TurOf(sira)) SiradakiSerit(k, p, sira);
            if (!veri) { c.Add(Bosluk(20)); return; }
            int hg = TurHedef(tr, sira);
            c.Add(Grup(hg == sira ? L.T("Sıradaki maç") : L.T("{0}. basamak", KariyerVeri.BasOf(hg) + 1)));
            c.Add(MacOzet(hg));
            // 2026-10-26: Usta yolu son turun altında (finalden sonra gelir; kilitliyken nedeniyle görünür)
            if (tr == KariyerVeri.TUR - 1) c.Add(UstaKart());
            c.Add(Bosluk(12));
            // Kaydırarak tur seçince sekme yeniden kurulur; sayfanın dikey yeri korunur (inceleme: snap anında sayfa başa fırlıyordu).
            if (kariyerY > 0) { float y0 = kariyerY; sv.schedule.Execute(() => sv.scrollOffset = new Vector2(0, y0)).StartingIn(0); }
            kariyerY = -1;
        }

        const float TUR_ARALIK = 10f;
        // Kart genişliği şerit eksi 36 px: sonraki tur kenarda görünür (önce %84'tü; 375 pt'de altı 44 pt nokta sığmıyor, sarıyordu), kaydırılabileceği belli olur. Seçili kart
        // ortalanır. Bırakınca en yakın kart seçilir (kendi kendine hizalanma); dokunarak da seçilir. Sürükleme dokunuş sayılmaz.
        void TurSeritYerlestir(ScrollView hs, System.Collections.Generic.List<VisualElement> kartlar, ScrollView sayfaSv)
        {
            float w = 0; bool basili = false; float sonX = -1; int sabit = 0;
            // Oturma tween'i (inceleme 11 Kas 18:05 #1): bırakınca hedef karta 240 ms ease-out kayar, tur değiştiyse sekme ancak kart
            // yerine oturduktan sonra kurulur (yeni şerit aynı konumda doğar, sıçrama görünmez). Önce 0,3 sn bekleyip tek karede
            // zıplıyordu. Hafif fiske (kart genişliğinin %15'i) yönündeki komşu karta geçer; önce yarıdan fazla sürüklemek gerekiyordu.
            // Hareketi azaltta tween yok, eskisi gibi anında. Yeni basış tween'i keser.
            float twX0 = 0, twX1 = 0, twT0 = -1; int twIdx = -1; IVisualElementScheduledItem tw = null;
            EventCallback<GeometryChangedEvent> gc = null; gc = e =>
            {
                float vw = hs.layout.width; if (float.IsNaN(vw) || vw <= 0) return;
                hs.UnregisterCallback(gc); w = Mathf.Round(Mathf.Max(vw * .8f, vw - 36));
                foreach (var kt in kartlar) kt.style.width = w;
                hs.schedule.Execute(() => hs.scrollOffset = new Vector2(Mathf.Max(0, kariyerTur * (w + TUR_ARALIK) - (vw - w) / 2), 0)).StartingIn(0);
            };
            hs.RegisterCallback(gc);
            hs.RegisterCallback<PointerDownEvent>(e => { basili = true; twT0 = -1; hs.userData = hs.scrollOffset.x; }, TrickleDown.TrickleDown);
            hs.RegisterCallback<PointerUpEvent>(e => basili = false, TrickleDown.TrickleDown);
            hs.RegisterCallback<PointerCancelEvent>(e => basili = false, TrickleDown.TrickleDown);
            hs.RegisterCallback<PointerCaptureOutEvent>(e => basili = false, TrickleDown.TrickleDown);
            // Kart üstünde başlayan dikey sürükleme sayfaya geçsin (iç içe kaydırma).
            hs.nestedInteractionKind = ScrollView.NestedInteractionKind.ForwardScrolling;
            var it = hs.schedule.Execute(() =>
            {
                if (w <= 0) return; float x = hs.scrollOffset.x;
                if (Mathf.Abs(x - sonX) > .5f) { sonX = x; sabit = 0; return; }
                if (basili || twT0 >= 0 || ++sabit != 2) return;   // 2 yoklama (~0,2 sn; önce 3)
                float vw = hs.layout.width; int idx = Mathf.Clamp(Mathf.RoundToInt((x + (vw - w) / 2) / (w + TUR_ARALIK)), 0, kartlar.Count - 1);
                if (idx == kariyerTur && hs.userData is float x0 && Mathf.Abs(x - x0) > w * .15f) idx = Mathf.Clamp(idx + (x > x0 ? 1 : -1), 0, kartlar.Count - 1);   // fiske
                hs.userData = null;
                float hedef = Mathf.Max(0, idx * (w + TUR_ARALIK) - (vw - w) / 2);
                if (Mathf.Abs(hedef - x) > 1 && !Ayar.reduce)
                {
                    twX0 = x; twX1 = hedef; twIdx = idx; twT0 = Time.realtimeSinceStartup;
                    if (tw == null) tw = hs.schedule.Execute(() =>
                    {
                        if (twT0 < 0) return;
                        float k = Mathf.Clamp01((Time.realtimeSinceStartup - twT0) / .24f), e = 1 - (1 - k) * (1 - k) * (1 - k);   // ease-out kübik
                        hs.scrollOffset = new Vector2(Mathf.Lerp(twX0, twX1, e), 0); sonX = hs.scrollOffset.x;
                        if (k < 1) return;
                        twT0 = -1; sabit = 0;
                        if (twIdx != kariyerTur) { Yerel.Hap("tick"); kariyerY = sayfaSv.scrollOffset.y; KariyerTurSec(twIdx); }
                    }).Every(16);
                    return;
                }
                if (idx != kariyerTur) { Yerel.Hap("tick"); kariyerY = sayfaSv.scrollOffset.y; KariyerTurSec(idx); }
                else if (Mathf.Abs(hedef - x) > 1) hs.scrollOffset = new Vector2(hedef, 0);
            }).Every(100);
            hs.RegisterCallback<DetachFromPanelEvent>(e => { it.Pause(); if (tw != null) tw.Pause(); });   // sekme yeniden kurulunca eski şeridin zamanlayıcısı durur
        }
        // Sürükleme sonrası bırakma dokunuş sayılmasın: basışta kaydedilen konumdan 8 px'den fazla kaydıysa yok say.
        static bool Surukledi(ScrollView hs) { return hs.userData is float x0 && Mathf.Abs(hs.scrollOffset.x - x0) > 8; }

        // Seçili turda açık gösterilecek maç: sıradaki bu turdaysa o; değilse turun ilk kazanılmamış açık basamağı; tur bittiyse final
        // (yeniden oynama); tur kilitliyse ilk basamak (kilit nedeniyle).
        static int TurHedef(int tr, int sira)
        {
            var p = Ayar.ilerleme; if (sira >= 0 && KariyerVeri.TurOf(sira) == tr && p.Acik(sira)) return sira;
            for (int b = 0; b < KariyerVeri.BASAMAK; b++) { int g = KariyerVeri.Global(tr, b); if (p.Acik(g) && !p.Kazanildi(g)) return g; }
            return p.TurAcik(tr) ? KariyerVeri.Global(tr, KariyerVeri.BASAMAK - 1) : KariyerVeri.Global(tr, 0);
        }

        // Tur kartı: "Tur n" + ad + yıldız, tema, altı basamak, durum satırı (şimdiki maç ya da mühüre kalan), seçili kartta hazırlık
        // çubuğu. Seçili = turuncu çerçeve (biçimle de: "seçili" ekran okuyucuda, ✓ yerine çerçeve + hazırlık satırı). Kilitli = kilit +
        // neden. Durum yalnız renkle verilmez (basamakta ✓ / numara / kilit / kupa).
        VisualElement TurKart(int t, int sira, ScrollView hs)
        {
            var p = Ayar.ilerleme; bool secili = t == kariyerTur, acik = p.TurAcik(t);
            var tv = t < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[t] : null; bool veri = tv != null && tv.Var;
            var kb = Kart("tur-kart"); kb.AddToClassList("tur-kart--yatay"); if (secili) kb.AddToClassList("tur-kart--secili"); if (!acik) kb.AddToClassList("tur-kart--kilit");
            var ust = Satir("arali"); ust.pickingMode = PickingMode.Ignore; kb.Add(ust);
            var adk = new VisualElement(); adk.AddToClassList("esnek-yatay"); adk.pickingMode = PickingMode.Ignore; ust.Add(adk);
            adk.Add(Yazi(L.T("Tur {0}", t + 1), 12, "etiket"));
            var ad = Yazi(L.T(TurAd(t)), 20, "kalin"); adk.Add(ad);
            if (!acik) ust.Add(Ikon(Ikonlar.Kilit, 20, SOLUK)); else if (veri) ust.Add(YildizSayac(p.TurYildiz(t), 18, 14));
            if (tv != null && !string.IsNullOrEmpty(tv.tema)) { var tm = Yazi(L.T(tv.tema), 14); tm.AddToClassList("tur-tema"); kb.Add(tm); }
            if (!acik) { var ky = Yazi(KilitNedeni(t), 13, "soluk"); ky.AddToClassList("tur-durum"); kb.Add(ky); }
            string durum = null;
            if (veri)
            {
                var sr = Satir("tur-basamaklar"); kb.Add(sr);
                for (int b = 0; b < KariyerVeri.BASAMAK; b++) sr.Add(BasamakNokta(KariyerVeri.Global(t, b), sira, hs));
                if (acik)
                {
                    int y = p.TurYildiz(t);
                    // "Şimdi: …" yalnız seçili olmayan kartta (başka turu incelerken kendi turunu gösterir); seçili kendi turunda maç
                    // kartı hemen altında, kartta mühüre kalan yazar (fikir 22:10 #2).
                    if (!secili && sira >= 0 && KariyerVeri.TurOf(sira) == t) { var skb = KariyerVeri.Al(sira); durum = L.T("Şimdi: {0}", (KariyerVeri.BasOf(sira) + 1) + ". " + L.T(skb.yer) + " · " + Match.STIL[skb.stil].isim); }
                    else durum = y >= Ilerleme.MUHUR_ALTIN ? L.T("Altın tur mührü") : y >= Ilerleme.MUHUR_BRONZ ? L.T("Altın mühüre {0} yıldız", Ilerleme.MUHUR_ALTIN - y) : L.T("Bronz mühüre {0} yıldız", Ilerleme.MUHUR_BRONZ - y);
                    var dz = Yazi(durum, 13, "soluk"); dz.AddToClassList("tur-durum"); dz.pickingMode = PickingMode.Ignore; kb.Add(dz);
                }
            }
            // Tur hazırlığı (2026-11-07 stüdyo GD2) yalnız seçili kartta: bilgi, kapı değil; eksikse "Atölye'ye git".
            if (secili && tv != null)
            {
                int ep = EkipmanPuani(); bool az = ep < tv.epMin;
                var hz = Satir("arali"); hz.pickingMode = PickingMode.Ignore; kb.Add(hz);
                hz.Add(Yazi(L.T("Ekipman {0}/{1}", ep, tv.epMin), 13, az ? "durum-simdi" : "soluk")); hz.Add(Yazi(az ? L.T("{0} yükseltme eksik", tv.epMin - ep) : L.T("Hazırsın"), 13, az ? "soluk" : "durum-gecti"));
                var iz = new VisualElement(); iz.AddToClassList("stat-iz"); iz.AddToClassList("tur-hazir-iz"); iz.pickingMode = PickingMode.Ignore; kb.Add(iz);
                var dl = new VisualElement(); dl.AddToClassList("stat-dolu"); dl.style.width = Length.Percent(Mathf.Clamp01(ep / (float)Mathf.Max(1, tv.epMin)) * 100); dl.pickingMode = PickingMode.Ignore; iz.Add(dl);
                if (az && acik) { var ag = Dugme(L.T("Atölye'ye git"), () => SekmeAc(Sekme.Atolye), "kucuk-dugme"); ag.style.alignSelf = Align.FlexStart; ag.style.marginTop = 8; kb.Add(ag); }
            }
            int tt = t; kb.AddManipulator(new Clickable(() => { if (Surukledi(hs) || tt == kariyerTur) return; Yerel.Hap("tick"); var psv = hs.parent?.GetFirstAncestorOfType<ScrollView>(); if (psv != null) kariyerY = psv.scrollOffset.y; KariyerTurSec(tt); }));
            int mh = acik ? p.Muhur(t) : 0;
            Erisim.Etiket(kb, L.T("Tur {0}", t + 1) + ", " + L.T(TurAd(t)) + (veri && acik ? ", " + L.T("{0}/{1} yıldız", p.TurYildiz(t), 18) : "") + (!acik ? ", " + KilitNedeni(t) : "")
                + (mh >= 2 ? ", " + L.T("Altın tur mührü") : mh == 1 ? ", " + L.T("Bronz tur mührü") : "") + (durum != null ? ", " + durum : ""), "dugme", secili);
            return kb;
        }

        // Basamak noktası (44 pt): geçilen = ✓, şimdiki = güneş diski üstünde numara, açık = numara, kilitli = kilit, final = kupa
        // (geçilmediyse). Dokununca o basamağın maç kartı.
        VisualElement BasamakNokta(int g, int sira, ScrollView hs)
        {
            var p = Ayar.ilerleme; int b = KariyerVeri.BasOf(g); bool gecti = p.Kazanildi(g), simdi = g == sira && p.Acik(g), acik = p.Acik(g), final = KariyerVeri.Final(g);
            var roz = new VisualElement(); roz.AddToClassList("dugum-rozet"); roz.AddToClassList(gecti ? "dugum-rozet--gecti" : simdi ? "dugum-rozet--simdi" : acik ? "dugum-rozet--acik" : "dugum-rozet--kilit");
            if (simdi) roz.style.backgroundImage = new StyleBackground(Ikonlar.GunesDisk);
            if (gecti) roz.Add(Ikon(Ikonlar.Tik, 20, KOYU));
            else if (final) roz.Add(Ikon(Ikonlar.Kupa, 18, simdi ? KOYU : acik ? ALTIN : SOLUK));
            else if (!acik) roz.Add(Ikon(Ikonlar.Kilit, 16, SOLUK));
            else { var n = Yazi((b + 1).ToString(), 16, "kalin"); n.style.color = simdi ? KOYU : YAZI; n.pickingMode = PickingMode.Ignore; roz.Add(n); }
            int gg = g; roz.AddManipulator(new Clickable(() => { if (Surukledi(hs)) return; Yerel.Hap("tick"); MacKartAc(gg); }));
            var kb = KariyerVeri.Al(g);
            Erisim.Etiket(roz, (b + 1) + ". " + L.T("basamak") + ", " + L.T(kb.yer) + (final ? ", " + L.T("Final") : "") + ", " + L.T(gecti ? "Geçildi" : simdi ? "Şimdi" : acik ? "Açık" : "Kilitli"), "dugme");
            return roz;
        }

        // Açık maç kartı: maç kartı alt sayfasının özü (rakip, ipucu, yıldızlar, zorluk) ve tek dokunuşla Oyna. Ayrıntı (imza vuruşu,
        // saha, yıldız görevleri) "Maç ayrıntısı"nda, alt sayfada.
        VisualElement MacOzet(int g)
        {
            var p = Ayar.ilerleme; var kb = KariyerVeri.Al(g); var st = Match.STIL[kb.stil]; int tr = KariyerVeri.TurOf(g), b = KariyerVeri.BasOf(g);
            bool acik = p.Acik(g), final = KariyerVeri.Final(g);
            var kart = Kart("mac-ozet");
            var rr = Satir("profil-ust"); kart.Add(rr);
            var av = new VisualElement(); av.AddToClassList("avatar"); av.style.backgroundColor = Kit.Hex(st.renk); AvatarIc(av, Portre.Rakip(st), 30); rr.Add(av);
            var ra = new VisualElement(); ra.AddToClassList("esnek-yatay"); rr.Add(ra);
            var ay = Yazi((b + 1) + ". " + L.T(kb.yer) + (final ? " · " + L.T("Final") : ""), 17, "kalin"); ra.Add(ay);
            ra.Add(Yazi(st.isim + " · " + L.T(st.ad), 14, "soluk"));
            if (acik) { var yr = Satir("rakip-satir"); yr.pickingMode = PickingMode.Ignore; ra.Add(yr); yr.Add(UcYildiz(p.Maske(g), 16)); var yy = Yazi(p.Yildiz(g) + "/3", 12, "soluk"); yy.style.marginLeft = 4; yr.Add(yy); }
            Erisim.Etiket(rr, (b + 1) + ". " + L.T(kb.yer) + ", " + L.T("Rakip") + " " + st.isim + ", " + L.T(st.ad) + (acik ? ", " + L.T("{0}/3 yıldız", p.Yildiz(g)) : ""), "metin");
            var ip = Yazi(L.T(st.ipucu), 14, "ipucu"); Erisim.Etiket(ip, L.T(st.ipucu), "metin"); kart.Add(ip);
            if (acik) { var zs = Satir(); kart.Add(zs); ZorlukSatir(zs, kart); }
            if (acik) { string ol = L.T(p.Kazanildi(g) ? "Yeniden oyna" : "Oyna"); var o = Dugme(ol, () => KariyerOyna(g), "dugme--birincil", null, 19); o.AddToClassList("mac-ozet-oyna"); Sigdir(o); Erisim.Etiket(o, ol + ", " + L.T(kb.yer), "dugme"); kart.Add(o); }   // yer adı kartın başlığında; ekran okuyucu yine söyler
            else kart.Add(Pasif(L.T("Kilitli"), b > 0 && p.TurAcik(tr) ? L.T("Önce {0} basamağını kazan", L.T(KariyerVeri.Al(g - 1).yer)) : KilitNedeni(tr)));
            var dy = Dugme(L.T("Maç ayrıntısı"), () => MacKartAc(g), "kucuk-dugme"); dy.AddToClassList("dugme--hayalet"); dy.style.alignSelf = Align.Center; dy.style.marginTop = 8; kart.Add(dy);
            return kart;
        }

        void KariyerOyna(int g)
        {
            Ayar.kariyerModu = true; Ayar.kariyerSecim = g; Ayar.GearHesap(); Ayar.Kaydet(); gez.Kapat(Gezinti.KART); Uygula(gez.sekme, false); root.Oyna();
        }

        // Sabit "Sıradaki" şeridi (2026-11-07 stüdyo UX5): haritada sıradaki maç düğüm → kart → Oyna diye üç dokunuştu ve şimdiki
        // basamak çoğu zaman görünür alanın dışındaydı. Sekme çubuğunun üstünde, kaydırmadan bağımsız tek turuncu dokunuş: maç
        // kartını açar (düğümle aynı yol). Kilitli bir turu incelerken de durur: oyuncuyu kendi turuna döndürür (UX6). Kariyer
        // bitince yok.
        void SiradakiSerit(VisualElement k, Ilerleme p, int sira)
        {
            if (p.HepsiBitti || sira < 0 || !p.Acik(sira)) return;
            int tr = KariyerVeri.TurOf(sira); if (tr >= KariyerVeri.Turlar.Length || !KariyerVeri.Turlar[tr].Var) return;
            var kb = KariyerVeri.Al(sira); var st = Match.STIL[kb.stil]; int b = KariyerVeri.BasOf(sira);
            string bas = L.T("Sıradaki maç"), alt = (b + 1) + ". " + L.T(kb.yer) + " · " + st.isim;
            var d = Dugme(bas, () => MacKartAc(sira), "dugme--birincil", alt, 17); d.AddToClassList("siradaki-serit"); Sigdir(d);
            Erisim.Etiket(d, bas + ", " + L.T(TurAd(tr)) + ", " + alt, "dugme"); k.Add(d);
        }

        static int TurSayisiVeri() { int n = 0; foreach (var t in KariyerVeri.Turlar) if (t.Var) n++; return n; }
        static readonly string[] TUR_YEDEK = { "Yerel yol", "Asya Turu", "Avrupa Turu", "Dünya Finalleri", "Pasifik Turu", "Amerika Turu", "Büyük Kupa", "Efsaneler Kupası" };
        public static string TurAd(int t) { return t < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[t].ad : TUR_YEDEK[t]; }
        static int EkipmanPuani() { int n = 0; foreach (var kv in Ayar.gear.eko.sv) n += kv.Value; return n; }

        string KilitNedeni(int t)
        {
            var p = Ayar.ilerleme;
            if (t > 0 && (t >= KariyerVeri.Turlar.Length || !KariyerVeri.Turlar[t].Var)) return p.TurYakinda(t) ? L.T("Yakında: bu tur sonraki güncellemede gelir") : L.T("{0} finalini kazan · yakında", L.T(TurAd(t - 1)));
            return L.T("{0} finalini kazan", L.T(TurAd(t - 1)));
        }

        // Yıldız sayacı: simge + "11/18" (sayı yazıyla, yalnız simgeye güvenilmez).
        VisualElement YildizSayac(int n, int toplam, float px)
        {
            var r = Satir("yildiz-sayac"); r.pickingMode = PickingMode.Ignore; r.Add(Ikon(Ikonlar.YildizDolu, px + 2, ALTIN)); var y = Yazi(n + "/" + toplam, px, "altin"); y.style.marginLeft = 4; r.Add(y); return r;
        }
        // Üç yıldız: alınan dolu, alınmayan çerçeve.
        VisualElement UcYildiz(int maske, float px)
        {
            var r = Satir("yildizlar"); r.pickingMode = PickingMode.Ignore;
            for (int i = 0; i < 3; i++) { bool var_ = (maske & (1 << i)) != 0; r.Add(Ikon(var_ ? Ikonlar.YildizDolu : Ikonlar.YildizBos, px, var_ ? ALTIN : SOLUK)); }
            return r;
        }

        // ---- maç kartı alt sayfası (tasarım §2.3) ----
        void MacKartKur()
        {
            int g = kartBasamak; if (!KariyerVeri.Var(g)) { AltKapat(); return; }
            var p = Ayar.ilerleme; var kb = KariyerVeri.Al(g); var st = Match.STIL[kb.stil]; int tr = KariyerVeri.TurOf(g), b = KariyerVeri.BasOf(g);
            bool acik = p.Acik(g), final = KariyerVeri.Final(g); int m = p.Maske(g); var im = final || kb.imza ? KariyerVeri.Imza(st.imza) : null;   // Tur 4: önceki patronlar finalden önce de imzalı
            var c = AltSayfaKur(L.T(kb.yer));
            c.Add(Yazi(L.T(TurAd(tr)) + " · " + L.T("{0}. basamak", b + 1) + (final ? " · " + L.T("Final") : ""), 13, "etiket"));
            // rakip: renk yuvarlağı yanında ad ve tarz (renk tek başına anlam taşımaz)
            var rr = Satir("profil-ust"); c.Add(rr);
            var av = new VisualElement(); av.AddToClassList("avatar"); av.style.backgroundColor = Kit.Hex(st.renk); AvatarIc(av, Portre.Rakip(st), 30); rr.Add(av);
            var ra = new VisualElement(); ra.AddToClassList("esnek-yatay"); rr.Add(ra);
            ra.Add(Yazi(st.isim, 20, "kalin")); ra.Add(Yazi(L.T(st.ad), 14, "soluk"));
            Erisim.Etiket(rr, L.T("Rakip") + " " + st.isim + ", " + L.T(st.ad), "metin");
            var ip = Yazi(L.T(st.ipucu), 14, "ipucu"); Erisim.Etiket(ip, L.T(st.ipucu), "metin"); c.Add(ip);
            if (im != null)
            {
                var ik = Kart("imza-kart"); c.Add(ik);
                var ir = Satir("rakip-satir"); ik.Add(ir); var isk = new VisualElement(); isk.AddToClassList("renk-nokta"); isk.style.backgroundColor = Kit.Hex(im.renk); ir.Add(isk);
                ir.Add(Yazi(L.T("İmza vuruşu") + ": " + L.T(im.ad), 15, "kalin"));
                ik.Add(Yazi(L.T(im.cevap), 13, "ipucu"));
                Erisim.Etiket(ik, L.T("İmza vuruşu") + ": " + L.T(im.ad) + ". " + L.T(im.cevap), "metin");
            }
            int uz = kb.uzunluk > 0 ? kb.uzunluk : Ayar.target;
            var bil = Yazi(L.T("Saha") + ": " + L.T(CourtView.SAHA[kb.saha].ad) + " · " + (kb.uzunluk > 0 ? L.T("{0} sayı", uz) : L.T("{0} sayı (senin seçimin)", uz)) + "", 13, "soluk");
            Erisim.Etiket(bil, bil.text, "metin"); c.Add(bil);
            // Kural maçı (2026-11-07): tek satır, Günün maçının çevrilmiş adı ve açıklaması (yeni metin anahtarı yok).
            var kd = kb.degistirici != null ? Gunluk.Deg(kb.degistirici) : null;
            if (kd != null) { var kr = Yazi(L.T(kd.ad) + " · " + L.T(kd.aciklama), 14, "ipucu"); Erisim.Etiket(kr, kr.text, "metin"); c.Add(kr); }
            // Zorluk kartın üstünde seçilir (yasin 2026-11-05 "kariyer patikasını Kolay modda da oynayabilmeliyim"): eskiden kariyer
            // yalnız Hızlı maç/Ayarlar'daki seçimi izliyordu ve kartta yalnız yazıydı; oyuncu kariyeri Usta'ya kilitli sanıyordu.
            // Aynı ayar (tuytop_zorluk, dört kademe): seçim her yerde aynı kalır.
            var zs = Satir(); c.Add(zs);
            ZorlukSatir(zs, c);
            // 2026-11-08: basamak uzunluk vermiyorsa ("senin seçimin") seçim burada da yapılır (UzunlukSatir, aynı anahtar).
            if (kb.uzunluk <= 0) { var us = Satir(); c.Add(us); UzunlukSatir(us, c); }
            c.Add(Grup(L.T("Yıldızlar")));
            var gv = Match.GorevBul(kb.gorev);
            // Yeniden oynamada eksik yıldız üstte (2026-11-07, inceleme 18:20 madde 3): tamamlayıcı oyuncunun sorusu "hangisi eksik";
            // üç satır aynı sırada (Kazan, Görev, Farkla) duruyordu. Kazanılmış basamakta eksikler tam satır, alınanlar tek soluk
            // satırda birleşir. İlk oynayışta üç satır eskisi gibi.
            bool[] yv = { (m & Ilerleme.KAZAN) != 0, (m & Ilerleme.GOREV) != 0, (m & Ilerleme.FARK) != 0 };
            string[] yUzun = { L.T("Kazan"), L.T("Basamak görevi") + ": " + (gv != null ? L.Cumle(gv.ad) : "-"), L.T("Farkla kazan: rakip en çok {0} sayı", Ilerleme.FarkSiniri(uz)) };
            if (!p.Kazanildi(g)) for (int i = 0; i < 3; i++) YildizSatir(c, yv[i], yUzun[i]);
            else
            {
                string[] yKisa = { L.T("Kazan"), L.T("Basamak görevi"), L.T("Farkla kazan") }; string alinan = null;
                for (int i = 0; i < 3; i++) if (!yv[i]) YildizSatir(c, false, yUzun[i]); else alinan = alinan == null ? yKisa[i] : alinan + " · " + yKisa[i];
                if (alinan != null) { var al = Yazi(L.T("Alındı: {0}", alinan), 13, "soluk"); Erisim.Etiket(al, al.text, "metin"); c.Add(al); }
            }
            // "Önerilen ekipman puanı" satırı kalktı (2026-11-07, inceleme 18:20 madde 3): tur giriş kartı aynı bilgiyi hazırlık
            // çubuğu + "Atölye'ye git" ile veriyor (stüdyo GD2); kartta sorun gösterip çözüm göstermeyen eski biçim kalmıştı.
            // 2026-10-27: Oyna / Kilitli kaydırmanın dışında, sayfanın dibinde (Duyarli.cs, kural 4)
            if (acik)
            {
                AltEylem(Dugme(L.T(p.Kazanildi(g) ? "Yeniden oyna" : "Oyna") + ": " + L.T(kb.yer), () => KariyerOyna(g), "dugme--birincil", null, 19));
            }
            else AltEylem(Pasif(L.T("Kilitli"), b > 0 && p.TurAcik(tr) ? L.T("Önce {0} basamağını kazan", L.T(KariyerVeri.Al(g - 1).yer)) : KilitNedeni(tr)));
        }

        void YildizSatir(VisualElement c, bool var_, string ad)
        {
            var r = Satir("basarim"); c.Add(r); if (!var_) r.AddToClassList("kilitli");
            r.Add(Ikon(var_ ? Ikonlar.YildizDolu : Ikonlar.YildizBos, 20, var_ ? ALTIN : SOLUK));
            r.Add(Yazi(ad, 15, "esnek-yatay")); string d = L.T(var_ ? "Alındı" : "Henüz değil"); r.Add(Yazi(d, 12, var_ ? "durum-gecti" : "durum-kilit"));
            Erisim.Etiket(r, L.T("Yıldız") + ": " + ad + ", " + d, "metin");
        }
    }
}
