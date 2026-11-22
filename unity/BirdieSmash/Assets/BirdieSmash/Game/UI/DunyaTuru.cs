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
        ScrollView turSerit; VisualElement turAltKap;

        // Sekiz tur kartı yatay şeritte; seçili kart görünür alanın ortasına alınır (KarakterOdasi.OrtayaKaydir kalıbı).
        // 2026-11-22 inceleme (fikir 22:06 #2): düz ScrollView bırakınca iki kartın arasında yarım kalabiliyordu. Kaydırma durunca
        // (değer 150 ms değişmez ve parmak kalkmışsa) en yakın kart .25 sn ease-out ile ortaya oturur, bir kez "kaydir" sesi + tick
        // titreşim (Dolap'ta giyilen parçanın sesiyle aynı dil). Seçim değişmez, yalnız konum. Parmak yeniden basarsa kayma durur.
        // Hareketi azaltta anında oturur, ses kalır. Bekçi yalnız değer değişince başlar, durunca biter (sürekli zamanlayıcı yok).
        // PointerUp kaçarsa basili true kalır ve şerit hiç oturmaz: düz ScrollView'a döner, yanlış anda zıplamaz.
        ScrollView TurSerit(int secili, int sira)
        {
            var sv = new ScrollView(ScrollViewMode.Horizontal); sv.AddToClassList("tur-serit");
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            VisualElement sec = null;
            for (int t = 0; t < KariyerVeri.TUR; t++) { var k = TurKart2(t, t == secili, sira); sv.Add(k); if (t == secili) sec = k; }
            if (sec != null) OrtayaKaydir(sv, sec);
            bool basili = false, izliyor = false; float sonDeg = 0;
            sv.RegisterCallback<PointerDownEvent>(e => basili = true, TrickleDown.TrickleDown);
            sv.RegisterCallback<PointerUpEvent>(e => basili = false, TrickleDown.TrickleDown);
            sv.RegisterCallback<PointerCancelEvent>(e => basili = false, TrickleDown.TrickleDown);
            sv.horizontalScroller.valueChanged += v =>
            {
                sonDeg = Time.realtimeSinceStartup; if (izliyor || seritKayiyor) return; izliyor = true;
                sv.schedule.Execute(() =>
                {
                    if (seritKayiyor || basili || Time.realtimeSinceStartup - sonDeg < .15f) return;
                    izliyor = false; SeritOtur(sv, EnYakinKart(sv), true);
                }).Every(50).Until(() => !izliyor);
            };
            return sv;
        }

        static VisualElement EnYakinKart(ScrollView sv)
        {
            float cx = sv.scrollOffset.x + sv.contentViewport.layout.width * .5f, en = float.MaxValue; VisualElement sec = null;
            foreach (var k in sv.contentContainer.Children()) { float d = Mathf.Abs(k.layout.center.x - cx); if (d < en) { en = d; sec = k; } }
            return sec;
        }

        // Kartı görünür alanın ortasına kaydırır; zaten oradaysa (1 px) hiçbir şey yapmaz, ses de çalmaz.
        bool seritKayiyor;
        void SeritOtur(ScrollView sv, VisualElement el, bool ses)
        {
            float g = sv.contentViewport.layout.width; if (el == null || g <= 0 || el.layout.width <= 0) return;
            float x0 = sv.scrollOffset.x, x = Mathf.Clamp(el.layout.center.x - g * .5f, 0, Mathf.Max(0, sv.contentContainer.layout.width - g));
            if (Mathf.Abs(x - x0) < 1) return;
            System.Action bitti = () => { if (ses) { root.sfx.UiSes("kaydir"); Yerel.Hap("tick"); } };
            if (Ayar.reduce) { sv.scrollOffset = new Vector2(x, sv.scrollOffset.y); bitti(); return; }
            float t0 = Time.realtimeSinceStartup, son = x0; seritKayiyor = true;
            sv.schedule.Execute(() =>
            {
                if (!seritKayiyor) return;
                if (Mathf.Abs(sv.scrollOffset.x - son) > 1) { seritKayiyor = false; return; }   // oyuncu yeniden kaydırdı
                float t = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / .25f), e = 1 - (1 - t) * (1 - t) * (1 - t);
                sv.scrollOffset = new Vector2(Mathf.Lerp(x0, x, e), sv.scrollOffset.y); son = sv.scrollOffset.x; Kalite.Hareket();
                if (t >= 1) { seritKayiyor = false; bitti(); }
            }).Every(16).Until(() => !seritKayiyor);
        }

        // 2026-11-22 inceleme (fikir 22:06 #1): kart dokunuşu SekmeKur ile bütün Kariyer sekmesini (sekiz Painter2D sahnesi, Dünya
        // sırası, şerit, alt şerit) baştan kuruyordu; yeni ScrollView seçili kartı tek karede ortaya zıplatıyordu. Artık şerit
        // kalır: eski/yeni kartın seçili sınıfı ve erişim durumu değişir, kart .25 sn ile ortaya kayar, yalnız alt kap (hikâye,
        // aşamalar, rota, Usta) yeniden kurulur ve .12 sn belirir (PR #158 Dolap kategori kalıbı). Basamak animasyonu yeniden
        // oynamaz (eski yolda BasamakAniHazirla aynı sırayı görüp sıfırlıyordu). Şerit panelde değilse eski SekmeKur yolu.
        void TurSecYerinde(int t)
        {
            if (turSerit == null || turAltKap == null || turAltKap.panel == null || t < 0 || t >= turSerit.contentContainer.childCount) { KariyerTurSec(t); return; }
            var cc = turSerit.contentContainer; int once = kariyerTur; kariyerTur = t;
            if (once >= 0 && once < cc.childCount) KartSecili(cc[once], false); KartSecili(cc[t], true);
            basAniDen = basAniA = -1;
            TurAltKur(turAltKap, t, Ayar.ilerleme.Siradaki(), true);
            SeritOtur(turSerit, cc[t], false);
        }
        static void KartSecili(VisualElement k, bool s)
        {
            k.EnableInClassList("tur-kart2--secili", s);
            if (k.userData is ErisimBilgi b) { string sec = ", " + L.T("Seçili"); if (b.etiket.EndsWith(sec)) b.etiket = b.etiket.Substring(0, b.etiket.Length - sec.Length); if (s) b.etiket += sec; b.secili = s; }
        }

        // Seçili turun altı: hikâye/hazırlık, finallerde aşamalar, rota, son turda Usta yolu.
        // sol: içerik yeni iç kapta solgun doğar ve 16 ms sonra belirir (ekrandaki kaba eklense önce 1→0 sönerdi; AntrenmanEkran kalıbı).
        void TurAltKur(VisualElement kap, int tr, int sira, bool sol = false)
        {
            kap.Clear();
            if (sol && !Ayar.reduce) { var ic = new VisualElement(); ic.AddToClassList("ant-liste"); ic.AddToClassList("koz-solgun"); ic.schedule.Execute(() => ic.RemoveFromClassList("koz-solgun")).StartingIn(16); kap.Add(ic); kap = ic; }
            var tv = tr < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[tr] : null; bool veri = tv != null && tv.Var;
            kap.Add(TurAlt(tr, tv));
            if (veri && GrupTuru(tr)) kap.Add(FinalAsama(tr, sira));
            if (veri) kap.Add(TurYol(tr, sira));
            if (veri && tr == KariyerVeri.TUR - 1) kap.Add(UstaKart());   // 2026-10-26: Usta yolu son turun altında
        }

        VisualElement TurKart2(int t, bool secili, int sira)
        {
            var p = Ayar.ilerleme; bool acik = p.TurAcik(t); var tv = t < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[t] : null; bool veri = tv != null && tv.Var;
            int kaz = 0; if (veri) for (int b = 0; b < KariyerVeri.BASAMAK; b++) if (p.Kazanildi(KariyerVeri.Global(t, b))) kaz++;
            bool bitti = veri && kaz == KariyerVeri.BASAMAK, burada = veri && sira >= 0 && KariyerVeri.TurOf(sira) == t && !bitti;
            var k = new VisualElement(); k.AddToClassList("tur-kart2"); k.AddToClassList("dokunulur");
            if (secili) k.AddToClassList("tur-kart2--secili"); if (!acik) k.AddToClassList("tur-kart2--kilit");
            var sh = new VisualElement(); sh.AddToClassList("tur-kart2-sahne"); sh.pickingMode = PickingMode.Ignore; k.Add(sh);
            sh.Add(new MekanSahne(t) { style = { position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0 } });
            if (!acik) { var ort = new VisualElement(); ort.AddToClassList("tur-kart2-ortu"); ort.pickingMode = PickingMode.Ignore; ort.Add(Ikon(Ikonlar.Kilit, 26, YAZI)); sh.Add(ort); }
            var no = Yazi(L.T("Tur {0}", t + 1), 12, "tur-kart2-no"); no.pickingMode = PickingMode.Ignore; sh.Add(no);
            if (bitti) { var bt = new VisualElement(); bt.AddToClassList("tur-kart2-bitti"); bt.pickingMode = PickingMode.Ignore; bt.Add(Ikon(Ikonlar.Tik, 14, KOYU)); bt.Add(Yazi(L.T("Bitti"), 12, "koyu-yazi")); sh.Add(bt); }
            else if (burada) { var bt = new VisualElement(); bt.AddToClassList("tur-kart2-bitti"); bt.AddToClassList("tur-kart2-burada"); bt.pickingMode = PickingMode.Ignore;
                // 2026-11-22 inceleme (fikir 22:06 #3): rozet ile seçili çerçeve aynı turuncu; "ben buradayım" yüzle ayrılır (yalnız renkle anlam yok).
                var av = new VisualElement(); av.AddToClassList("tur-kart2-av"); av.pickingMode = PickingMode.Ignore; av.style.backgroundColor = Kit.Hex(Ayar.FORMA); AvatarIc(av, Portre.Benim(), 14); bt.Add(av);
                bt.Add(Yazi(L.T("Buradasın"), 12, "koyu-yazi")); sh.Add(bt); }

            var gv = new VisualElement(); gv.AddToClassList("tur-kart2-govde"); gv.pickingMode = PickingMode.Ignore; k.Add(gv);
            var ad = Yazi(L.T(TurAd(t)), 20, "tur-kart2-ad"); ad.name = "tur-ad"; SozcukSigdir(ad); gv.Add(ad);
            if (tv != null && tv.bolum.Length > 0) gv.Add(Yazi(L.T(tv.bolum), 12, "soluk"));
            string erisim = L.T("Tur {0}/{1}", t + 1, KariyerVeri.TUR) + ", " + L.T(TurAd(t));   // 2026-11-22 (fikir 22:06 #7): kaydırma çubuğu gizli, kaç tur olduğu sesle de duyulsun
            if (!acik) { var ky = Yazi(KilitNedeni(t), 12, "soluk"); ky.AddToClassList("tur-kart2-kilit-yazi"); gv.Add(ky); erisim += ", " + KilitNedeni(t); }
            else if (veri)
            {
                var r = Satir("arali"); r.pickingMode = PickingMode.Ignore; gv.Add(r);
                r.Add(Yazi(L.T("{0}/6 maç", kaz), 13, "kalin")); r.Add(YildizSayac(p.TurYildiz(t), 18, 13));
                var cb = Cubuk(kaz / 6f, LIMON, 6); cb.style.marginTop = 6; gv.Add(cb);
                int mh = p.Muhur(t); string ms = mh >= 2 ? L.T("Altın tur mührü") : mh == 1 ? L.T("Bronz tur mührü") : L.T("Tur ödülü: mühür");
                var mr = Satir("tur-kart2-odul"); mr.pickingMode = PickingMode.Ignore; mr.Add(Ikon(Ikonlar.Kupa, 14, mh > 0 ? ALTIN : SOLUK)); mr.Add(Yazi(ms, 12, mh > 0 ? "altin" : "soluk")); gv.Add(mr);
                erisim += ", " + L.T("{0}/6 maç", kaz) + ", " + L.T("{0}/{1} yıldız", p.TurYildiz(t), 18) + ", " + ms;
            }
            int tt = t; k.AddManipulator(new Clickable(() => { Tik(k); if (tt != kariyerTur) TurSecYerinde(tt); }));
            Erisim.Etiket(k, erisim + (secili ? ", " + L.T("Seçili") : ""), "dugme", secili);
            return k;
        }

        // Seçili turun altındaki tek satır: hikâye + (eksikse) hazırlık. Ad, yıldız ve kilit kartta; burada tekrar edilmez.
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

        // Finaller (grup + eleme turları: Dünya Finalleri, Efsaneler Kupası): rotanın üstünde turnuva aşamaları Grup · Çeyrek · Yarı ·
        // Final. Grup kutusu üç grup maçının kaçının kazanıldığını yazar. Veriden: ilk basamağın yeri "grup" içeriyorsa bu tur grup
        // turudur (kariyer.json; çeviri anahtarı değil, ham veri).
        static bool GrupTuru(int t) { if (t >= KariyerVeri.Turlar.Length || !KariyerVeri.Turlar[t].Var) return false; var y = KariyerVeri.Al(KariyerVeri.Global(t, 0)).yer; return y != null && y.Contains("grup"); }
        VisualElement FinalAsama(int t, int sira)
        {
            var p = Ayar.ilerleme; var r = Satir("final-asama"); r.pickingMode = PickingMode.Ignore;
            string[] ad = { "Grup", "Çeyrek", "Yarı", "Final" }; int[][] bs = { new[] { 0, 1, 2 }, new[] { 3 }, new[] { 4 }, new[] { 5 } };
            var sb = new System.Text.StringBuilder(L.T("Turnuva aşamaları") + ": ");
            for (int i = 0; i < 4; i++)
            {
                int kz = 0; bool simdi = false, acik = false;
                foreach (var b in bs[i]) { int g = KariyerVeri.Global(t, b); if (p.Kazanildi(g)) kz++; if (g == sira && p.Acik(g)) simdi = true; if (p.Acik(g)) acik = true; }
                bool tamam = kz == bs[i].Length;
                var k = new VisualElement(); k.AddToClassList("asama-kutu"); k.AddToClassList(tamam ? "asama-kutu--tamam" : simdi ? "asama-kutu--simdi" : acik ? "asama-kutu--acik" : "asama-kutu--kilit"); k.pickingMode = PickingMode.Ignore;
                k.Add(Ikon(tamam ? Ikonlar.Tik : i == 3 ? Ikonlar.Kupa : !acik ? Ikonlar.Kilit : Ikonlar.Oyna, 16, tamam ? LIMON : simdi ? TURUNCU : i == 3 ? ALTIN : SOLUK));
                var al = Yazi(L.T(ad[i]), 12, "kalin"); SozcukSigdir(al); k.Add(al);   // r241 ×2: "Çeyrek" dar kutuda bölünüyordu
                if (i == 0) k.Add(Yazi(kz + "/3", 11, "soluk"));
                r.Add(k);
                sb.Append(L.T(ad[i])).Append(' ').Append(L.T(tamam ? "tamam" : simdi ? "şimdi" : acik ? "açık" : "kilitli")).Append(i < 3 ? ", " : "");
            }
            Erisim.Etiket(r, sb.ToString(), "metin");
            return r;
        }

        // Durak ödülü (B'nin kalıbı: her duraktaki kazanç önceden görünür). Değerler Core/Gear.Odul'den: basamağın ilk galibiyeti +60 Tüy,
        // ilk kez alınan her yıldız +20. Uydurma sayı yok; maçın kendi galibiyet Tüy'ü (uzunluğa göre) burada yazılmaz, maç kartında.
        VisualElement DurakOdul(int g)
        {
            var p = Ayar.ilerleme; if (KariyerVeri.Final(g) && KariyerVeri.BasOf(g) == KariyerVeri.BASAMAK - 1 && !p.Kazanildi(g)) return null;   // final durağı ödülünü mühür satırı söyler
            int eksik = 0; for (int i = 0; i < 3; i++) if ((p.Maske(g) & (1 << i)) == 0) eksik++;
            string y; if (!p.Kazanildi(g)) y = "+60"; else if (eksik > 0) y = "+" + 20 * eksik; else return null;
            var r = Satir("durak-odul"); r.pickingMode = PickingMode.Ignore; r.Add(Ikon(Ikonlar.Tuy, 13, ALTIN)); r.Add(Yazi(y, 12, "altin"));
            Erisim.Etiket(r, p.Kazanildi(g) ? L.T("Eksik yıldızlar {0} Tüy", 20 * eksik) : L.T("İlk galibiyet 60 Tüy"), "metin");
            return r;
        }
    }
}
