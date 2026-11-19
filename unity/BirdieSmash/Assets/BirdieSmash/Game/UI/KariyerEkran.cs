// Kariyer haritası (komple-oyun tasarımı §2.3, §3.1; adım 2, 2026-10-26). Adım 1'in düz listesinin yerini aldı.
// Yerleşim (2026-11-07, yatay tur kartları; önceki dikey yol haritasının yerini aldı): üstte başlık ve toplam yıldız, altında
// yatay kaydırılan sekiz tur kartı (tur adı, yıldız, tema, altı basamak noktası, şimdiki maç ya da mühüre kalan yıldız), altında
// seçili turun açık maç kartı (rakip, ipucu, zorluk, Oyna). Basamak noktasına dokunmak maç kartını alt sayfa olarak açar (MacKartKur).
// Durum yalnız renkle verilmez (HIG renk körlüğü): geçilen = ✓, şimdiki = güneş diski + numara + "Şimdi: …" satırı, kilitli = kilit,
// final = kupa; ekran okuyucu her noktada "Geçildi/Şimdi/Açık/Kilitli" der; yıldızlar dolu/çerçeve biçimiyle ve "2/3" yazısıyla.
// Büyük yazıda (×1,35 üstü) kart şeridin %90'ı; basamak noktaları sığmazsa ikinci satıra sarar.
// Tur seçimi sekmenin durumudur (tasarım §2.2: Tur 3'e bakıp Oyna'ya geçen döndüğünde Tur 3'ü görür); maçtan dönünce
// sıradaki basamağın turuna döner (yeni tur açılmış olabilir).
// 2026-11-19 (yasin seçimi "Kariyer A", artifact AhLNpXh8XkiHWsZk1kkz3H; araştırma: Tennis Clash kupa yolu, Clash Royale sabit alt
// eylem): yatay tur kartlarının yerine dikey tur yolu. 11-07'de seçilen yatay kartlar ilerlemeyi altı küçük noktada veriyordu,
// tur kartı + maç özeti iki uzun metin bloğu sayfayı aşağı itiyordu. Şimdi: başlık ‹ Tur n · ad ★ y/18 › (oklar turu değiştirir),
// altında altı basamak zikzak bir yol üstünde (geçilen ✓ + yıldızlar, şimdiki büyük güneş diski + "Şimdi", kilitli kilit), yolun
// sonunda mühür (kupa + mühüre kalan yıldız). Düğümün yanında rakip adı ve tarzı. Tek birincil eylem altta sabit "Sıradaki maç"
// şeridi (maç kartını açar: zorluk, uzunluk, Oyna). Yol yukarıdan aşağı (1 → 6 → mühür): okuma ve VoiceOver sırası basamak sırasıyla
// aynı; maketteki aşağıdan yukarı yol kaydırmalı sayfada şimdiki basamağı görünür alana getirmek için ek kaydırma isterdi.
// Yol çizgisi tek bir Painter2D eğrisi (düğüm merkezlerinden), yalnız yerleşim değişince yeniden çizilir; karede iş yok.
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
            BasamakAniHazirla(sira);
            if (kariyerTur < 0 || kariyerTur >= KariyerVeri.TUR) kariyerTur = KariyerVeri.TurOf(sira);
            var bas = Satir("arali"); c.Add(bas);
            var t = Yazi(L.T("Kariyer"), 24, "sekme-baslik"); Erisim.Etiket(t, L.T("Kariyer"), "baslik"); bas.Add(t);
            bas.Add(YildizSayac(p.ToplamYildiz, TurSayisiVeri() * 18, 15));
            // Dünya sırası (2026-11-14 hikâye, Core/Hikaye): kazanılan basamakla yükselir, kayıpta düşmez; 1'den sonra unvanı koru.
            { int ds = Hikaye.OyuncuSira(Hikaye.Galibiyet(p)); string dy = ds == 1 ? L.T("Dünya 1 numarası · unvanını koru") : L.T("Dünya sıralaman: {0}", SiraYaz(ds));
              var dsy = Yazi(dy, 14, "soluk"); dsy.AddToClassList("dunya-sira"); Erisim.Etiket(dsy, dy, "metin"); c.Add(dsy);
              DunyaSiraAkit(dsy, ds); }
            // Career.Line satırı kalktı (fikir 22:10 #2): aynı maç başlık altında, tur kartında, maç kartında ve düğmede dört kez yazıyordu.

            int tr = kariyerTur; var tv = tr < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[tr] : null; bool veri = tv != null && tv.Var;
            c.Add(TurBaslik(tr, tv));
            if (veri) c.Add(TurYol(tr, sira));
            if (veri && tr == KariyerVeri.TUR - 1) c.Add(UstaKart());   // 2026-10-26: Usta yolu son turun altında
            c.Add(Bosluk(12));
            // Tek birincil eylem: kendi turunda da başka turu incelerken de altta sabit (UX5/UX6).
            SiradakiSerit(k, p, sira);
            if (kariyerY > 0) { float y0 = kariyerY; sv.schedule.Execute(() => sv.scrollOffset = new Vector2(0, y0)).StartingIn(0); }
            kariyerY = -1;
        }

        // Tur başlığı: ‹ · "Tur n · bölüm" + ad + yıldız (kilitliyse kilit + neden) · ›. Hikâye cümlesi tek satır altında; hazırlık
        // satırı yalnız ekipman eksikse (bilgi, kapı değil; "Atölye'ye git"). Oklar 44 pt, uçta pasif değil gizli (yer tutar, simetri).
        VisualElement TurBaslik(int t, KariyerTur tv)
        {
            var p = Ayar.ilerleme; bool acik = p.TurAcik(t), veri = tv != null && tv.Var;
            var kb = Kart("tur-baslik"); if (!acik) kb.AddToClassList("tur-kart--kilit");
            var r = Satir("tur-baslik-satir"); kb.Add(r);
            r.Add(TurOk(t - 1, Ikonlar.Geri, L.T("Önceki tur")));
            var orta = new VisualElement(); orta.AddToClassList("esnek-yatay"); orta.AddToClassList("tur-baslik-orta"); orta.pickingMode = PickingMode.Ignore; r.Add(orta);
            orta.Add(Yazi(L.T("Tur {0}", t + 1) + (tv != null && tv.bolum.Length > 0 ? " · " + L.T(tv.bolum) : ""), 12, "etiket"));
            var ad = Yazi(L.T(TurAd(t)), 20, "kalin"); ad.name = "tur-ad"; orta.Add(ad);
            if (!acik) { var ky = Yazi(KilitNedeni(t), 13, "soluk"); orta.Add(ky); }
            else if (veri) orta.Add(YildizSayac(p.TurYildiz(t), 18, 14));
            r.Add(TurOk(t + 1, Ikonlar.Ileri, L.T("Sonraki tur")));
            if (tv != null && tv.hikaye.Length > 0) { var hy = Yazi(L.T(tv.hikaye), 14, "soluk"); hy.AddToClassList("tur-hikaye"); kb.Add(hy); }
            if (acik && tv != null)
            {
                int ep = EkipmanPuani();
                if (ep < tv.epMin)
                {
                    var hz = Satir("arali"); hz.style.alignItems = Align.Center; kb.Add(hz);
                    var ek = Yazi(L.T("Ekipman {0}/{1}", ep, tv.epMin) + " · " + L.T("{0} yükseltme eksik", tv.epMin - ep), 13, "durum-simdi"); ek.AddToClassList("esnek-yatay"); SozcukSigdir(ek); hz.Add(ek);
                    hz.Add(Dugme(L.T("Atölye'ye git"), () => SekmeAc(Sekme.Atolye), "kucuk-dugme"));
                }
            }
            int mh = acik ? p.Muhur(t) : 0;
            Erisim.Etiket(orta, L.T("Tur {0}", t + 1) + ", " + L.T(TurAd(t)) + (veri && acik ? ", " + L.T("{0}/{1} yıldız", p.TurYildiz(t), 18) : "") + (!acik ? ", " + KilitNedeni(t) : "")
                + (mh >= 2 ? ", " + L.T("Altın tur mührü") : mh == 1 ? ", " + L.T("Bronz tur mührü") : ""), "baslik");
            return kb;
        }
        VisualElement TurOk(int hedef, Texture2D ik, string etiket)
        {
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("ikon-dugme"); d.Add(Ikon(ik, 20, Color.white));
            bool var_ = hedef >= 0 && hedef < KariyerVeri.TUR;
            if (!var_) { d.style.visibility = Visibility.Hidden; return d; }
            d.AddManipulator(new Clickable(() => { Tik(); KariyerTurSec(hedef); })); Erisim.Etiket(d, etiket + ": " + L.T(TurAd(hedef)), "dugme");
            return d;
        }

        // Tur yolu: altı basamak satırı + mühür satırı. Çift satırda düğüm solda ve yazı sağında, tek satırda tersi (zikzak, yazılar
        // içe bakar, kenara taşmaz). Yol çizgisi satırların arkasında tek eğri; geçilen kısım altın tonunda (biçimle de: ✓ düğümler).
        VisualElement TurYol(int t, int sira)
        {
            var p = Ayar.ilerleme; var kap = new VisualElement(); kap.AddToClassList("tur-yol");
            var cizgi = new VisualElement(); cizgi.AddToClassList("tur-yol-cizgi"); cizgi.pickingMode = PickingMode.Ignore; kap.Add(cizgi);
            var dugumler = new System.Collections.Generic.List<VisualElement>(); int gecilen = -1;
            for (int b = 0; b <= KariyerVeri.BASAMAK; b++)
            {
                bool muhur = b == KariyerVeri.BASAMAK; int g = muhur ? -1 : KariyerVeri.Global(t, b);
                var sr = Satir("yol-satir"); sr.AddToClassList(b % 2 == 0 ? "yol-satir--sol" : "yol-satir--sag"); kap.Add(sr);
                VisualElement dg; VisualElement yz = new VisualElement(); yz.AddToClassList("yol-yazi"); yz.pickingMode = PickingMode.Ignore;
                if (muhur) { dg = MuhurDugum(t, yz); }
                else
                {
                    dg = BasamakNokta(g, sira); var kb = KariyerVeri.Al(g); var st = Match.STIL[kb.stil]; bool simdi = g == sira && p.Acik(g);
                    if (simdi) dg.AddToClassList("dugum-rozet--buyuk");
                    if (p.Kazanildi(g)) gecilen = b;
                    var ad = Yazi((simdi ? L.T("Şimdi") + " · " : "") + st.isim, 15, "kalin"); if (simdi) ad.AddToClassList("durum-simdi"); yz.Add(ad);
                    var alt = Satir("rakip-satir"); alt.pickingMode = PickingMode.Ignore; yz.Add(alt);
                    alt.Add(Yazi(p.Acik(g) ? L.T(st.ad) : L.T(kb.yer), 13, "soluk"));
                    if (p.Kazanildi(g)) { var ys = UcYildiz(p.Maske(g), 13); ys.style.marginLeft = 6; alt.Add(ys); }
                }
                sr.Add(dg); sr.Add(yz); dugumler.Add(dg);
            }
            int gc = gecilen;
            cizgi.generateVisualContent += mgc => YolCiz(mgc, cizgi, dugumler, gc);
            kap.RegisterCallback<GeometryChangedEvent>(e => cizgi.MarkDirtyRepaint());
            return kap;
        }
        static readonly Color YOL_RENK = new Color(1, 1, 1, .14f), YOL_GECTI = new Color(.976f, .824f, .447f, .55f);
        static void YolCiz(MeshGenerationContext mgc, VisualElement cizgi, System.Collections.Generic.List<VisualElement> d, int gecilen)
        {
            if (d.Count < 2 || float.IsNaN(d[0].layout.width)) return;
            var pz = mgc.painter2D; pz.lineWidth = 6; pz.lineCap = LineCap.Round; pz.lineJoin = LineJoin.Round;
            var m = new Vector2[d.Count];
            for (int i = 0; i < d.Count; i++) m[i] = cizgi.WorldToLocal(d[i].worldBound.center);
            for (int i = 0; i < m.Length - 1; i++)
            {
                Vector2 a = m[i], b = m[i + 1]; float oy = (b.y - a.y) * .5f;
                pz.strokeColor = i <= gecilen ? YOL_GECTI : YOL_RENK;   // düğüm i geçildiyse ondan çıkan kesit yürünmüş yol
                pz.BeginPath(); pz.MoveTo(a); pz.BezierCurveTo(new Vector2(a.x, a.y + oy), new Vector2(b.x, b.y - oy), b); pz.Stroke();
            }
        }
        // Mühür düğümü: kupa; yazı mühür durumu ("Bronz mühüre n yıldız" / "Altın tur mührü"). Dokunuş yok (bilgi).
        VisualElement MuhurDugum(int t, VisualElement yz)
        {
            var p = Ayar.ilerleme; bool acik = p.TurAcik(t); int y = acik ? p.TurYildiz(t) : 0, mh = acik ? p.Muhur(t) : 0;
            var roz = new VisualElement(); roz.AddToClassList("dugum-rozet"); roz.AddToClassList(mh > 0 ? "dugum-rozet--gecti" : "dugum-rozet--kilit"); roz.AddToClassList("dugum-rozet--muhur");
            roz.Add(Ikon(Ikonlar.Kupa, 20, mh > 0 ? KOYU : ALTIN)); roz.pickingMode = PickingMode.Ignore;
            string bas = mh >= 2 ? L.T("Altın tur mührü") : mh == 1 ? L.T("Bronz tur mührü") : L.T("Tur mührü");
            string alt = y >= Ilerleme.MUHUR_ALTIN ? L.T("Tamamlandı") : y >= Ilerleme.MUHUR_BRONZ ? L.T("Altın mühüre {0} yıldız", Ilerleme.MUHUR_ALTIN - y) : L.T("Bronz mühüre {0} yıldız", Ilerleme.MUHUR_BRONZ - y);
            yz.Add(Yazi(bas, 15, "kalin")); yz.Add(Yazi(alt, 13, "soluk"));
            Erisim.Etiket(yz, bas + ", " + alt, "metin");
            return roz;
        }

        // Basamak noktası (44 pt): geçilen = ✓, şimdiki = güneş diski üstünde numara, açık = numara, kilitli = kilit, final = kupa
        // (geçilmediyse). Dokununca o basamağın maç kartı.
        // 2026-11-11 inceleme (fikir 20:05 #2): basamak geçmek haritada hiç an değildi; düğüm sonraki açılışta sessizce ✓ oluyordu.
        // tuytop_kariyer_gorulen = Kariyer'de en son gösterilen sıradaki basamak. Sıradaki ondan ilerideyse bu kurulumda geçilen
        // düğümlerin ✓'i küçükten büyür, yeni sıradaki düğüm yerine oturur; tek tık sesi + tick titreşimi (üç kanal). Değer hemen
        // yazılır: yeniden kurulumlar (dil, yazı boyu) anı tekrarlamaz. İlk kez (anahtar yok) yalnız yazılır, eski oyuncuya geçmiş
        // basamaklar topluca oynatılmaz. Hareketi azaltta ölçek yok, ses ve titreşim kalır. Konfeti/rozet yok (yasin kuralı).
        int basAniDen = -1, basAniA = -1;
        void BasamakAniHazirla(int sira)
        {
            basAniDen = basAniA = -1; if (sira < 0) return;
            int gorulen = PlayerPrefs.GetInt("tuytop_kariyer_gorulen", -1);
            if (gorulen == sira) return;
            PlayerPrefs.SetInt("tuytop_kariyer_gorulen", sira); PlayerPrefs.Save();
            if (gorulen < 0 || sira < gorulen) return;
            basAniDen = gorulen; basAniA = sira;
            if (root != null && root.sfx != null) root.sfx.Tik(1175); Yerel.Hap("tick");
        }
        // 2026-11-15 (inceleme turu, yasin 14 Kas "zaman geçtikçe güçlendiğimi hissetmeliyim"): dünya sırası Kariyer her
        // kurulduğunda son değeri düz yazıyordu; galibiyetten sonraki 1.840 → 1.360 düşüşü hiç görülmüyordu. Son gösterilen
        // sıra tuytop_dunya_gosterilen'de; yeni sıra daha iyiyse sayı basamak animasyonundan sonra 0,6 sn'de eskisinden
        // yenisine akar (ease-out), bitişte tek tık. 1'e inince satır yaylanır. Hareketi azaltta doğrudan yeni değer.
        // Erişim etiketi baştan son değeri okur (ara sayılar okunmaz). Kayıpta sıra düşmediği için geri akış yok.
        void DunyaSiraAkit(Label l, int ds)
        {
            int once = PlayerPrefs.GetInt("tuytop_dunya_gosterilen", -1);
            if (once != ds) { PlayerPrefs.SetInt("tuytop_dunya_gosterilen", ds); PlayerPrefs.Save(); }
            if (once < 0 || ds >= once || Ayar.reduce || l == null) return;
            System.Func<int, string> yaz = n => n == 1 ? L.T("Dünya 1 numarası · unvanını koru") : L.T("Dünya sıralaman: {0}", SiraYaz(n));
            l.text = yaz(once);
            float bas = -1; const float SURE = .6f; int gecikme = basAniA >= 0 ? 450 : 250;
            IVisualElementScheduledItem it = null;
            it = l.schedule.Execute(() =>
            {
                if (l.panel == null) { it.Pause(); return; }
                if (bas < 0) bas = Time.unscaledTime;
                float t = Mathf.Clamp01((Time.unscaledTime - bas) / SURE), e = 1 - (1 - t) * (1 - t) * (1 - t);
                int n = Mathf.RoundToInt(Mathf.Lerp(once, ds, e)); l.text = yaz(t >= 1 ? ds : n);
                if (t >= 1)
                {
                    it.Pause(); if (root != null && root.sfx != null) root.sfx.Tik(1320); Yerel.Hap("tick");
                    if (ds == 1) BasamakBuyut(l, 1.12f, 0);
                }
            }).Every(16).StartingIn(gecikme);
        }
        static void BasamakBuyut(VisualElement v, float bas, int gecikmeMs)
        {
            if (Ayar.reduce) return;
            v.style.scale = new Scale(new Vector2(bas, bas));
            v.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale") };
            v.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(.18f, TimeUnit.Second) };
            v.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseOutBack) };
            v.schedule.Execute(() => { v.style.scale = new Scale(Vector2.one); Kalite.Hareket(); }).StartingIn(gecikmeMs);
        }
        VisualElement BasamakNokta(int g, int sira)
        {
            var p = Ayar.ilerleme; int b = KariyerVeri.BasOf(g); bool gecti = p.Kazanildi(g), simdi = g == sira && p.Acik(g), acik = p.Acik(g), final = KariyerVeri.Final(g);
            var roz = new VisualElement(); roz.AddToClassList("dugum-rozet"); roz.AddToClassList(gecti ? "dugum-rozet--gecti" : simdi ? "dugum-rozet--simdi" : acik ? "dugum-rozet--acik" : "dugum-rozet--kilit");
            if (simdi) roz.style.backgroundImage = new StyleBackground(Ikonlar.GunesDisk);
            if (gecti) { var tik = Ikon(Ikonlar.Tik, 20, KOYU); roz.Add(tik); if (g >= basAniDen && g < basAniA) BasamakBuyut(tik, .6f, 120); }
            if (simdi && g == basAniA && basAniDen >= 0) BasamakBuyut(roz, .8f, 300);
            else if (final) roz.Add(Ikon(Ikonlar.Kupa, 18, simdi ? KOYU : acik ? ALTIN : SOLUK));
            else if (!acik) roz.Add(Ikon(Ikonlar.Kilit, 16, SOLUK));
            else if (simdi && Portre.Benim() != null)
            {
                // 2026-11-19 (Ar-Ge 12:06 #1, OYUNCUYU GÖSTER): "sen buradasın" iğnesi oyuncunun kendi yüzü (harita ilerlemeli
                // muadillerde olduğu gibi); numara sağ altta küçük rozet. Portre üst çubukta zaten çizili, önbellekten gelir (ek
                // kamera çizimi yok). Rakip portresi düğüme eklenmedi: maç kartında var, düğümde ikinci yüz okumayı böler.
                var av = new VisualElement(); av.AddToClassList("dugum-portre"); av.style.backgroundColor = Kit.Hex(Ayar.FORMA); av.pickingMode = PickingMode.Ignore;
                AvatarIc(av, Portre.Benim(), 28); roz.Add(av);
                var no = new VisualElement(); no.AddToClassList("dugum-no"); no.pickingMode = PickingMode.Ignore; roz.Add(no);
                var nl = Yazi((b + 1).ToString(), 11, "kalin"); nl.style.unityTextAlign = TextAnchor.MiddleCenter; nl.style.paddingTop = 0f; nl.style.paddingBottom = 0f; no.Add(nl);
            }
            else
            {
                // 2026-11-14 (tasarım dili, r170): rakam 44 pt halkada kendi kutusundan taşıyordu (×1 22>20, ×2 32 pt rakam 9 pt'ye
                // ezildi). Halka sabit 44 (altı basamak bir satıra sığsın); rakam en çok 22 pt, daralmaz, iç boşluksuz ve ortada.
                var n = Yazi((b + 1).ToString(), 16, "kalin"); n.style.fontSize = Mathf.Min(n.style.fontSize.value.value, 22f);
                n.style.flexShrink = 0f; n.style.paddingTop = 0f; n.style.paddingBottom = 0f; n.style.marginTop = 0f; n.style.marginBottom = 0f;
                n.style.unityTextAlign = TextAnchor.MiddleCenter; n.style.color = simdi ? KOYU : YAZI; n.pickingMode = PickingMode.Ignore; roz.Add(n);
            }
            int gg = g; roz.AddManipulator(new Clickable(() => { Tik(); MacKartAc(gg); }));
            var kb = KariyerVeri.Al(g);
            Erisim.Etiket(roz, (b + 1) + ". " + L.T("basamak") + ", " + L.T(kb.yer) + (final ? ", " + L.T("Final") : "") + ", " + L.T(gecti ? "Geçildi" : simdi ? "Şimdi" : acik ? "Açık" : "Kilitli"), "dugme");
            return roz;
        }

        // Güç + dünya sırası ve (varsa) son karşılaşma satırları: maç özeti ve maç kartı aynı biçim.
        void GucSatirlari(VisualElement ra, int g, out string gs, out string ks)
        {
            int og = Hikaye.OncekiKarsilasma(g);
            gs = L.T("Güç {0} · Dünya {1}", Hikaye.Guc(g), SiraYaz(Hikaye.RakipSira(g)));
            ks = og >= 0 ? L.T("Son karşılaşma: {0} · o gün güç {1}", L.T(KariyerVeri.Al(og).yer), Hikaye.Guc(og)) : null;
            // 2026-11-15 (inceleme turu): iki soluk satır ("Güç 34 · Dünya 610." + "Son karşılaşma: Sevilla · o gün güç 21") tek
            // satır oldu: "Güç 21 → 34 · Dünya 610." Rakibin büyüdüğü tek bakışta okunur, dar ekranda + büyük yazıda kart kısalır.
            // Ekran okuyucu ok işaretini okumasın diye erişim etiketleri (gs, ks) eski iki cümleyle kalır; yer adı orada.
            string gor = og >= 0 ? L.T("Güç {0} → {1} · Dünya {2}", Hikaye.Guc(og), Hikaye.Guc(g), SiraYaz(Hikaye.RakipSira(g))) : gs;
            var gy = Yazi(gor, 13, "soluk"); gy.AddToClassList("rakip-guc"); gy.pickingMode = PickingMode.Ignore; ra.Add(gy);
        }
        // Sıra yazımı: binlik ayırıcı dile göre (TR 1.840, EN 1,840).
        internal static string SiraYaz(int n) { return n >= 1000 ? (n / 1000) + (L.En ? "," : ".") + (n % 1000).ToString("000") : n.ToString(); }

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
            int kG, kY; bool karne = Ayar.Karne(st.id, out kG, out kY);   // karne (fikir 12:05 #4): maç kartıyla aynı biçim
            ra.Add(Yazi(st.isim, 20, "kalin")); ra.Add(Yazi(L.T(st.ad) + (karne ? " · " + kG + "–" + kY : ""), 14, "soluk"));
            string gs, ks; GucSatirlari(ra, g, out gs, out ks);
            Erisim.Etiket(rr, L.T("Rakip") + " " + st.isim + ", " + L.T(st.ad) + ", " + gs + (ks != null ? ", " + ks : "") + (karne ? ", " + L.T("Bu rakiple {0} galibiyet, {1} yenilgi", kG, kY) : ""), "metin");
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
            // 2026-11-13 inceleme (fikir #6): uzunluk seçiliyse ("senin seçimin") çipler hemen altında; satır sayıyı ikinci kez
            // yazmaz (çip değişince sayfa kurulmazsa eski sayıyı gösterip çiple çelişebiliyordu). Sabit uzunlukta bugünkü gibi.
            var bil = Yazi(L.T("Saha") + ": " + L.T(CourtView.SAHA[kb.saha].ad) + (kb.uzunluk > 0 ? " · " + L.T("{0} sayı", uz) : ""), 13, "soluk");
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
