// Kariyer haritası (komple-oyun tasarımı §2.3, §3.1; adım 2, 2026-10-26). Adım 1'in düz listesinin yerini aldı.
// Yerleşim: üstte tur seçici (4 tur; kilitli tur kilit simgesi + "… finalini kazan" yazısıyla), tur başlığı (ad, yıldız,
// önerilen ekipman puanı), ortada yol: 6 düğüm alttan yukarı (1. basamak en altta, final en üstte, kupa simgesiyle),
// düğümler sağa sola kırılarak bir yol gibi dizilir, aralarında yol çizgisi. Şimdiki düğüm görünür alanın alt yarısına
// kaydırılır (başparmak bölgesi). Düğüme dokunmak maç kartını alt sayfa olarak açar (MacKartKur).
// Durum yalnız renkle verilmez (HIG renk körlüğü): geçilen = dolu rozet + ✓ + "Geçildi", şimdiki = halka + "ŞİMDİ" +
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
            if (tv != null) kb.Add(Yazi(L.T("Önerilen ekipman puanı {0}–{1} · seninki {2}", tv.epMin, tv.epMax, EkipmanPuani()), 13, "soluk"));
            Erisim.Etiket(kb, L.T(TurAd(tr)) + (veri ? ", " + L.T("{0}/{1} yıldız", p.TurYildiz(tr), 18) : "") + (!acik ? ", " + KilitNedeni(tr) : ""), "metin");
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
            if (simdiki != null)
            {
                var hedef = simdiki;
                sv.schedule.Execute(() =>
                {
                    float vh = sv.contentViewport.layout.height, y = hedef.worldBound.y - sv.contentContainer.worldBound.y; if (float.IsNaN(vh) || float.IsNaN(y)) return;
                    sv.scrollOffset = new Vector2(0, Mathf.Max(0, y + hedef.layout.height / 2 - vh * .62f));
                }).StartingIn(60);
            }
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

        // Tur seçici çipi: seçili = açık zemin (2026-10-26 Yayın Gecesi; eskiden camgöbeği) (yazı ve ✓ ile), kilitli = kilit simgesi, bitmiş = kupa simgesi.
        VisualElement TurCip(int t)
        {
            var p = Ayar.ilerleme; bool secili = t == kariyerTur, acik = p.TurAcik(t), bitti = acik && p.TurBitti(t);
            var d = new VisualElement(); d.AddToClassList("dugme"); d.AddToClassList("cip"); d.AddToClassList("tur-cip"); if (secili) d.AddToClassList("cip--secili"); if (!acik) d.AddToClassList("tur-cip--kilit");
            var ic = new VisualElement(); ic.AddToClassList("cip-ic"); ic.pickingMode = PickingMode.Ignore; d.Add(ic);
            var renk = secili ? MUREKKEP : Color.white;
            if (!acik) ic.Add(Ikon(Ikonlar.Kilit, 14, renk)); else if (bitti) ic.Add(Ikon(Ikonlar.Kupa, 14, renk)); else if (secili) ic.Add(Ikon(Ikonlar.Tik, 14, renk));
            ic.Add(Yazi(L.T("Tur {0}", t + 1), 14, "dugme-yazi"));
            int tt = t; d.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); KariyerTurSec(tt); }));
            Erisim.Etiket(d, L.T("Tur {0}", t + 1) + ", " + L.T(TurAd(t)) + (!acik ? ", " + L.T("Kilitli") : bitti ? ", " + L.T("tamam") : ""), "dugme", secili);
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
            roz.Add(Ikon(gecti ? Ikonlar.Tik : simdi ? Ikonlar.Halka : acik ? Ikonlar.Oyna : Ikonlar.Kilit, 22, gecti ? KOYU : simdi ? CAMGOBEGI : acik ? Color.white : SOLUK));
            var orta = new VisualElement(); orta.AddToClassList("basamak-orta"); orta.pickingMode = PickingMode.Ignore; r.Add(orta);
            var ad = Satir("rakip-satir"); ad.pickingMode = PickingMode.Ignore; orta.Add(ad);
            if (final) { ad.Add(Ikon(Ikonlar.Kupa, 16, ALTIN)); }
            var ay = Yazi((b + 1) + ". " + L.T(kb.yer), 16, "kalin"); if (final) ay.style.marginLeft = 4; ad.Add(ay);
            var rk = Satir("rakip-satir"); rk.pickingMode = PickingMode.Ignore; orta.Add(rk);
            var nok = new VisualElement(); nok.AddToClassList("renk-nokta"); nok.style.backgroundColor = Kit.Hex(st.renk); rk.Add(nok);
            rk.Add(Yazi(st.isim + " · " + L.T(st.ad) + (final ? " · " + L.T("Final") : ""), 13, "soluk"));
            var yr = Satir("rakip-satir"); yr.pickingMode = PickingMode.Ignore; orta.Add(yr); yr.Add(UcYildiz(p.Maske(g), 16)); var yy = Yazi(ys + "/3", 12, "soluk"); yy.style.marginLeft = 4; yr.Add(yy);
            string durum = L.T(gecti ? "Geçildi" : simdi ? "ŞİMDİ" : acik ? "Açık" : "Kilitli");
            // r13 (2026-10-26): ×2'de rozet + ad + durum yan yana sıkışıyor, ad hece ortasından bölünüyordu ("Champi ons",
            // "Singap ore"); USS'teki sarma denemesi (flex-basis %70) Yoga'da satırı kırmadı. Büyük yazıda durum orta sütunun
            // altına iner, ad satırın tamamını alır.
            var dz = Yazi(durum, 12, gecti ? "durum-gecti" : simdi ? "durum-simdi" : acik ? "durum-gecti" : "durum-kilit");
            if (K >= 1.35f) { dz.style.marginTop = 4; orta.Add(dz); } else r.Add(dz);
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
            var av = new VisualElement(); av.AddToClassList("avatar"); av.style.backgroundColor = Kit.Hex(st.renk); av.Add(Ikon(Ikonlar.Kisi, 30, KOYU)); rr.Add(av);
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
            var bil = Yazi(L.T("Saha") + ": " + L.T(CourtView.SAHA[kb.saha].ad) + " · " + (kb.uzunluk > 0 ? L.T("{0} sayı", uz) : L.T("{0} sayı (senin seçimin)", uz)) + " · " + L.T(Ayar.easy ? "Kolay" : "Usta"), 13, "soluk");
            Erisim.Etiket(bil, bil.text, "metin"); c.Add(bil);
            c.Add(Grup(L.T("Yıldızlar")));
            var gv = Match.GorevBul(kb.gorev);
            YildizSatir(c, (m & Ilerleme.KAZAN) != 0, L.T("Kazan"));
            YildizSatir(c, (m & Ilerleme.GOREV) != 0, L.T("Basamak görevi") + ": " + (gv != null ? L.Cumle(gv.ad) : "-"));
            YildizSatir(c, (m & Ilerleme.FARK) != 0, L.T("Farkla kazan: rakip en çok {0} sayı", Ilerleme.FarkSiniri(uz)));
            var tv = KariyerVeri.Turlar[tr];
            c.Add(Yazi(L.T("Önerilen ekipman puanı {0}–{1} · seninki {2}", tv.epMin, tv.epMax, EkipmanPuani()) + " · " + L.T("öneri, kilit değil"), 12, "soluk"));
            c.Add(Bosluk(6));
            if (acik)
            {
                c.Add(Dugme(L.T(p.Kazanildi(g) ? "Yeniden oyna" : "Oyna") + ": " + L.T(kb.yer), () =>
                {
                    Ayar.kariyerModu = true; Ayar.kariyerSecim = g; Ayar.GearHesap(); Ayar.Kaydet(); gez.Kapat(Gezinti.KART); Uygula(gez.sekme, false); root.Oyna();
                }, "dugme--birincil", null, 19));
            }
            else c.Add(Pasif(L.T("Kilitli"), b > 0 && p.TurAcik(tr) ? L.T("Önce {0} basamağını kazan", L.T(KariyerVeri.Al(g - 1).yer)) : KilitNedeni(tr)));
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
