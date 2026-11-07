// Kariyer haritası (komple-oyun tasarımı §2.3, §3.1; adım 2, 2026-10-26). Adım 1'in düz listesinin yerini aldı.
// Yerleşim: üstte tur seçici (4 tur; kilitli tur kilit simgesi + "… finalini kazan" yazısıyla), tur başlığı (ad, yıldız,
// önerilen ekipman puanı), ortada yol: 6 düğüm alttan yukarı (1. basamak en altta, final en üstte, kupa simgesiyle),
// düğümler sağa sola kırılarak bir yol gibi dizilir, aralarında yol çizgisi. Şimdiki düğüm görünür alanın alt yarısına
// kaydırılır (başparmak bölgesi). Düğüme dokunmak maç kartını alt sayfa olarak açar (MacKartKur).
// Durum yalnız renkle verilmez (HIG renk körlüğü): geçilen = dolu rozet + ✓ + "Geçildi", şimdiki = halka + "Şimdi" (2026-11-06 unslop: büyük harf etiket cümle düzenine) +
// çerçeve, kilitli = kilit + "Kilitli" + soluk, final = kupa + "Final"; yıldızlar dolu/çerçeve biçimiyle ve "2/3" yazısıyla.
// Büyük yazıda (×1,35 üstü) yol kırılması kalkar, düğümler tam genişlik (metin sarılsın, yan yana sıkışmasın).
// Tur seçimi sekmenin durumudur (tasarım §2.2: Tur 3'e bakıp Oyna'ya geçen döndüğünde Tur 3'ü görür); maçtan dönünce
// sıradaki basamağın turuna döner (yeni tur açılmış olabilir).
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        int kariyerTur = -1, kartBasamak;

        void KariyerKur(VisualElement k)
        {
            k.Clear(); var sv = Kaydirma(k); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            var p = Ayar.ilerleme; int sira = p.Siradaki();
            if (kariyerTur < 0 || kariyerTur >= KariyerVeri.TUR) kariyerTur = KariyerVeri.TurOf(sira);
            var bas = Satir("arali"); c.Add(bas);
            var t = Yazi(L.T("Kariyer"), 24, "sekme-baslik"); Erisim.Etiket(t, L.T("Kariyer"), "baslik"); bas.Add(t);
            bas.Add(YildizSayac(p.ToplamYildiz, TurSayisiVeri() * 18, 15));
            c.Add(Yazi(L.Cumle(Career.Line(p)), 14, "soluk"));

            // tur seçici
            var sec = Satir(); sec.AddToClassList("tur-secici"); c.Add(sec);
            for (int i = 0; i < KariyerVeri.TUR; i++) sec.Add(TurCip(i));

            // tur başlığı
            int tr = kariyerTur; var tv = tr < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[tr] : null; bool acik = p.TurAcik(tr), veri = tv != null && tv.Var;
            var kb = Kart("tur-kart"); c.Add(kb);
            var kr = Satir("arali"); kb.Add(kr);
            kr.Add(Yazi(L.T(TurAd(tr)), 20, "kalin"));
            if (veri) kr.Add(YildizSayac(p.TurYildiz(tr), 18, 14));
            if (!acik)
            {
                var kl = Satir("rakip-satir"); kb.Add(kl); kl.Add(Ikon(Ikonlar.Kilit, 18, SOLUK)); var ky = Yazi(KilitNedeni(tr), 14, "soluk"); ky.style.marginLeft = 6; kl.Add(ky);
            }
            // Tur giriş kartı (2026-11-07 stüdyo GD2): tema cümlesi, altı rakip noktası (yenilen ✓) ve hazırlık çubuğu. "Önerilen 12–24 ·
            // seninki 6" sorun gösterip çözüm göstermiyordu. Çubuk yalnız bilgi, kapı değil (öneri-kilit ayrımı); ekipman puanı alt
            // sınırın altındaysa "Atölye'ye git" (puan yuva seviyelerinin toplamı, Atölye'de yükselir), değilse düğme yok.
            if (tv != null && !string.IsNullOrEmpty(tv.tema)) { var tm = Yazi(L.T(tv.tema), 14); tm.AddToClassList("tur-tema"); kb.Add(tm); }
            if (veri) { var rn = Satir("tur-rakipler"); rn.pickingMode = PickingMode.Ignore; kb.Add(rn); for (int b = 0; b < KariyerVeri.BASAMAK; b++) { int g = KariyerVeri.Global(tr, b); var st = Match.STIL[KariyerVeri.Al(g).stil]; var n = new VisualElement(); n.AddToClassList("tur-rakip-nokta"); n.style.backgroundColor = Kit.Hex(st.renk); if (!p.Kazanildi(g)) n.AddToClassList("tur-rakip-nokta--yok"); else n.Add(Ikon(Ikonlar.Tik, 11, MUREKKEP)); n.pickingMode = PickingMode.Ignore; rn.Add(n); } }
            if (tv != null)
            {
                int ep = EkipmanPuani(); bool az = ep < tv.epMin;
                var hz = Satir("arali"); hz.pickingMode = PickingMode.Ignore; kb.Add(hz);
                hz.Add(Yazi(L.T("Ekipman {0}/{1}", ep, tv.epMin), 13, az ? "durum-simdi" : "soluk")); hz.Add(Yazi(az ? L.T("{0} puan eksik", tv.epMin - ep) : L.T("Hazırsın"), 13, az ? "soluk" : "durum-gecti"));
                var iz = new VisualElement(); iz.AddToClassList("stat-iz"); iz.AddToClassList("tur-hazir-iz"); iz.pickingMode = PickingMode.Ignore; kb.Add(iz);
                var dl = new VisualElement(); dl.AddToClassList("stat-dolu"); dl.style.width = Length.Percent(Mathf.Clamp01(ep / (float)Mathf.Max(1, tv.epMin)) * 100); dl.pickingMode = PickingMode.Ignore; iz.Add(dl);
                if (az && acik) { var ag = Dugme(L.T("Atölye'ye git"), () => SekmeAc(Sekme.Atolye), "kucuk-dugme"); ag.style.alignSelf = Align.FlexStart; ag.style.marginTop = 8; kb.Add(ag); }
            }
            Erisim.Etiket(kb, L.T(TurAd(tr)) + (veri ? ", " + L.T("{0}/{1} yıldız", p.TurYildiz(tr), 18) : "") + (!acik ? ", " + KilitNedeni(tr) : "") + (tv != null && !string.IsNullOrEmpty(tv.tema) ? ", " + L.T(tv.tema) : "") + (tv != null ? ", " + L.T("Ekipman {0}/{1}", EkipmanPuani(), tv.epMin) : ""), "metin");
            SiradakiSerit(k, p, sira);
            if (!veri) { c.Add(Bosluk(20)); return; }

            // 2026-10-26: Usta yolu son turun üstünde (finalden sonra gelir; kilitliyken nedeniyle görünür)
            if (tr == KariyerVeri.TUR - 1) { c.Add(UstaKart()); var uy = new VisualElement(); uy.AddToClassList("yol"); uy.AddToClassList(p.UstaAcik ? "yol--acik" : "yol--kilit"); uy.pickingMode = PickingMode.Ignore; c.Add(uy); }
            // yol: final üstte, 1. basamak altta
            VisualElement simdiki = null;
            for (int b = KariyerVeri.BASAMAK - 1; b >= 0; b--)
            {
                int g = KariyerVeri.Global(tr, b);
                var d = Dugum(g, g == sira && acik); c.Add(d); if (g == sira && acik) simdiki = d;
                if (b > 0) { var yol = new VisualElement(); yol.AddToClassList("yol"); yol.AddToClassList(p.Kazanildi(g - 1) ? "yol--acik" : "yol--kilit"); yol.pickingMode = PickingMode.Ignore; c.Add(yol); }
            }
            c.Add(Bosluk(12));
            // Şimdiki düğüm görünür alanın alt yarısında (≈ %62'de) dursun; yerleşim bitince (içerik yüksekliği belli olunca).
            // 2026-11-07 (UX5, AD notu): 60 ms'lik zamanlayıcı yerleşimden önce çalışabiliyordu, oyuncu (ve ekran turu) haritayı kilitli
            // finallerle açıyordu. Artık içerik yerleşince (GeometryChanged) bir kez, animasyonsuz; zamanlayıcı yedek.
            if (simdiki != null)
            {
                var hedef = simdiki; bool yapildi = false;
                System.Action kaydir = () =>
                {
                    if (yapildi) return; float vh = sv.contentViewport.layout.height, y = hedef.worldBound.y - sv.contentContainer.worldBound.y;
                    if (float.IsNaN(vh) || float.IsNaN(y) || vh <= 0 || hedef.layout.height <= 0) return; yapildi = true;
                    sv.scrollOffset = new Vector2(0, Mathf.Max(0, y + hedef.layout.height / 2 - vh * .62f));
                };
                EventCallback<GeometryChangedEvent> gc = null; gc = e => { kaydir(); if (yapildi) sv.contentContainer.UnregisterCallback(gc); };
                sv.contentContainer.RegisterCallback(gc); sv.schedule.Execute(kaydir).StartingIn(60);
            }
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
        static readonly string[] TUR_YEDEK = { "Yerel yol", "Asya Turu", "Avrupa Turu", "Dünya Finalleri" };
        public static string TurAd(int t) { return t < KariyerVeri.Turlar.Length ? KariyerVeri.Turlar[t].ad : TUR_YEDEK[t]; }
        static int EkipmanPuani() { int n = 0; foreach (var kv in Ayar.gear.eko.sv) n += kv.Value; return n; }

        string KilitNedeni(int t)
        {
            var p = Ayar.ilerleme;
            if (t > 0 && (t >= KariyerVeri.Turlar.Length || !KariyerVeri.Turlar[t].Var)) return p.TurYakinda(t) ? L.T("Yakında: bu tur sonraki güncellemede gelir") : L.T("{0} finalini kazan · yakında", L.T(TurAd(t - 1)));
            return L.T("{0} finalini kazan", L.T(TurAd(t - 1)));
        }

        // Tur seçici çipi: seçili = turuncu çerçeve + tint (2026-11-07; önce açık zemin, ondan önce camgöbeği) (yazı ve ✓ ile), kilitli = kilit simgesi, bitmiş = kupa simgesi.
        VisualElement TurCip(int t)
        {
            var p = Ayar.ilerleme; bool secili = t == kariyerTur, acik = p.TurAcik(t), bitti = acik && p.TurBitti(t);
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("cip"); d.AddToClassList("tur-cip"); if (secili) d.AddToClassList("cip--secili"); if (!acik) d.AddToClassList("tur-cip--kilit");
            var ic = new VisualElement(); ic.AddToClassList("cip-ic"); ic.pickingMode = PickingMode.Ignore; d.Add(ic);
            // 2026-11-07 (AD1, UX6): seçili = turuncu çerçeve + tint (çip dili); simge krem, ✓ turuncu. Kilitli seçili çipte ✓ yok, kilit kalır.
            if (!acik) ic.Add(Ikon(Ikonlar.Kilit, 14, secili ? YAZI : SOLUK)); else if (bitti) ic.Add(Ikon(Ikonlar.Kupa, 14, YAZI)); else if (secili) ic.Add(Ikon(Ikonlar.Tik, 14, TURUNCU));
            ic.Add(Yazi(L.T("Tur {0}", t + 1), 14, "dugme-yazi"));
            // tur mührü (2026-11-07 GD5): bronz = çerçeve yıldız, altın = dolu yıldız (biçim + ekran okuyucu adı)
            int mh = acik ? p.Muhur(t) : 0; if (mh > 0) { var mi = Ikon(mh >= 2 ? Ikonlar.YildizDolu : Ikonlar.YildizBos, 13, mh >= 2 ? ALTIN : BRONZ); mi.style.marginLeft = 4; ic.Add(mi); }
            int tt = t; d.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); KariyerTurSec(tt); }));
            Erisim.Etiket(d, L.T("Tur {0}", t + 1) + ", " + L.T(TurAd(t)) + (!acik ? ", " + L.T("Kilitli") : bitti ? ", " + L.T("tamam") : "") + (mh >= 2 ? ", " + L.T("Altın tur mührü") : mh == 1 ? ", " + L.T("Bronz tur mührü") : ""), "dugme", secili);
            return d;
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

        VisualElement Dugum(int g, bool simdi)
        {
            var p = Ayar.ilerleme; var kb = KariyerVeri.Al(g); var st = Match.STIL[kb.stil]; int b = KariyerVeri.BasOf(g);
            bool gecti = p.Kazanildi(g), acik = p.Acik(g), kilit = !acik, final = KariyerVeri.Final(g); int ys = p.Yildiz(g);
            var kart = Kart("dugum"); kart.AddToClassList(b % 2 == 0 ? "dugum--sol" : "dugum--sag");
            if (kilit) kart.AddToClassList("kilitli"); if (simdi) kart.AddToClassList("simdiki"); if (final) kart.AddToClassList("dugum--final");
            int gg = g; kart.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); MacKartAc(gg); }));
            var r = Satir("basamak-satir"); r.pickingMode = PickingMode.Ignore; kart.Add(r);
            var roz = new VisualElement(); roz.AddToClassList("dugum-rozet"); roz.AddToClassList(gecti ? "dugum-rozet--gecti" : simdi ? "dugum-rozet--simdi" : acik ? "dugum-rozet--acik" : "dugum-rozet--kilit"); roz.pickingMode = PickingMode.Ignore; r.Add(roz);
            // 2026-10-29 açılış dili: şimdiki basamak rozeti perdenin güneş diski (UiOge Ikonlar.GunesDisk), üstünde koyu halka;
            // "şimdi" yine biçimle (disk + halka simgesi + "Şimdi" yazısı), yalnız renkle değil.
            if (simdi) roz.style.backgroundImage = new StyleBackground(Ikonlar.GunesDisk);
            roz.Add(Ikon(gecti ? Ikonlar.Tik : simdi ? Ikonlar.Halka : acik ? Ikonlar.Oyna : Ikonlar.Kilit, 22, gecti ? KOYU : simdi ? KOYU : acik ? Color.white : SOLUK));
            var orta = new VisualElement(); orta.AddToClassList("basamak-orta"); orta.pickingMode = PickingMode.Ignore; r.Add(orta);
            var ad = Satir("rakip-satir"); ad.pickingMode = PickingMode.Ignore; orta.Add(ad);
            // r55 (2026-10-29): kupa simgesi ad satırındayken ×2 Türkçe'de "6. Şampiyonlar kupası" üç satıra kırılıp iki satırlık
            // yükseklikte kesiliyordu (tarama 132>88). Ad satırı her basamakta tek yazı; kupa alttaki stil satırının başına geçti.
            var ay = Yazi((b + 1) + ". " + L.T(kb.yer), 16, "kalin"); ad.Add(ay);
            // r16 (2026-10-26, Barlow): yatay satırda ad tek satır genişliğiyle ölçülüp sonra daralınca iki satıra kırılıyor, yükseklik
            // tek satırda kalıp alttaki rakip satırına biniyordu ("4. Regional / final"). Taban 0 + büyüme: Yoga genişliği önce
            // verir, yüksekliği o genişlikle ölçer.
            ay.style.flexBasis = new StyleLength(0f); ay.style.flexGrow = 1;
            var rk = Satir("rakip-satir"); rk.pickingMode = PickingMode.Ignore; orta.Add(rk);
            if (final) { var ku = Ikon(Ikonlar.Kupa, 14, ALTIN); ku.style.marginRight = 4; rk.Add(ku); }
            var nok = new VisualElement(); nok.AddToClassList("renk-nokta"); nok.style.backgroundColor = Kit.Hex(st.renk); rk.Add(nok);
            // 2026-10-31 (mağaza turu tr_03: "Nova · Drive'cı · Final" iki satıra sarıp 18 pt satırda kesiliyordu): üstteki
            // başlıktaki gibi taban 0 + büyüme, yükseklik verilen genişlikle ölçülür.
            var rky = Yazi(st.isim + " · " + L.T(st.ad) + (final ? " · " + L.T("Final") : ""), 13, "soluk"); rky.style.flexBasis = new StyleLength(0f); rky.style.flexGrow = 1; rky.style.flexShrink = 1; rk.Add(rky);
            // 2026-11-02 (Ar-Ge 19:40 #3, sadeleştirme): kilitli basamakta boş "☆☆☆ 0/3" yok; kilit simgesi ve yol sırası yeter, kart
            // kısalır. Yıldızlar açık ve geçilmiş basamakta. "n. basamaktan sonra" kalır (29 Eki "leveller daha anlaşılır" kararı).
            if (!kilit) { var yr = Satir("rakip-satir"); yr.pickingMode = PickingMode.Ignore; orta.Add(yr); yr.Add(UcYildiz(p.Maske(g), 16)); var yy = Yazi(ys + "/3", 12, "soluk"); yy.style.marginLeft = 4; yr.Add(yy); }
            // Kilitli basamak nedenini söyler (2026-10-29, "leveller daha anlaşılır"): yalnız "Kilitli" yazınca neyin açacağı
            // maç kartına dokunmadan görünmüyordu. Tur açıksa kilidi önceki basamak tutar; tur kilitliyse neden tur seçicide.
            string durum = kilit && b > 0 && p.TurAcik(KariyerVeri.TurOf(g)) ? L.T("{0}. basamaktan sonra", b) : L.T(gecti ? "Geçildi" : simdi ? "Şimdi" : acik ? "Açık" : "Kilitli");
            // r13 (2026-10-26): ×2'de rozet + ad + durum yan yana sıkışıyor, ad hece ortasından bölünüyordu ("Champi ons",
            // "Singap ore"); USS'teki sarma denemesi (flex-basis %70) Yoga'da satırı kırmadı. Büyük yazıda durum orta sütunun
            // altına iner, ad satırın tamamını alır.
            var dz = Yazi(durum, 12, gecti ? "durum-gecti" : simdi ? "durum-simdi" : acik ? "durum-gecti" : "durum-kilit");
            // r48 (2026-10-29): kilit nedeni ("5. basamaktan sonra") sağda uzun bir sütun oluyor, adı üç satıra kırıyordu
            // ("6. Kuala / Lumpur / finali"); neden de büyük yazıdaki gibi orta sütunun altına iner.
            bool nedenli = kilit && durum != L.T("Kilitli");
            // r124 (2026-11-06): sağdaki kısa durum ("Kilitli") dar satırda "Kilitl / i" diye bölünüyordu; sağ sütun daralmaz, tek satır
            if (K >= 1.35f || nedenli) { dz.style.marginTop = 4; orta.Add(dz); } else { dz.style.whiteSpace = WhiteSpace.NoWrap; dz.style.flexShrink = 0f; r.Add(dz); }
            Erisim.Etiket(kart, L.T(TurAd(KariyerVeri.TurOf(g))) + ", " + (b + 1) + ". " + L.T("basamak") + ", " + L.T(kb.yer) + ", " + st.isim + (final ? ", " + L.T("Final") : "") + ", " + L.T("{0}/3 yıldız", ys) + ", " + durum, "dugme", simdi);
            return kart;
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
            c.Add(Grup(L.T("Yıldızlar")));
            var gv = Match.GorevBul(kb.gorev);
            YildizSatir(c, (m & Ilerleme.KAZAN) != 0, L.T("Kazan"));
            YildizSatir(c, (m & Ilerleme.GOREV) != 0, L.T("Basamak görevi") + ": " + (gv != null ? L.Cumle(gv.ad) : "-"));
            YildizSatir(c, (m & Ilerleme.FARK) != 0, L.T("Farkla kazan: rakip en çok {0} sayı", Ilerleme.FarkSiniri(uz)));
            var tv = KariyerVeri.Turlar[tr];
            c.Add(Yazi(L.T("Önerilen ekipman puanı {0}–{1} · seninki {2}", tv.epMin, tv.epMax, EkipmanPuani()), 12, "soluk"));   // 2026-11-05 unslop: "· öneri, kilit değil" üçüncü halkaydı; "Önerilen" zaten söylüyor
            // 2026-10-27: Oyna / Kilitli kaydırmanın dışında, sayfanın dibinde (Duyarli.cs, kural 4)
            if (acik)
            {
                AltEylem(Dugme(L.T(p.Kazanildi(g) ? "Yeniden oyna" : "Oyna") + ": " + L.T(kb.yer), () =>
                {
                    Ayar.kariyerModu = true; Ayar.kariyerSecim = g; Ayar.GearHesap(); Ayar.Kaydet(); gez.Kapat(Gezinti.KART); Uygula(gez.sekme, false); root.Oyna();
                }, "dugme--birincil", null, 19));
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
