// Antrenman sayfası, görev kartı, Günün maçı kartı ve Usta yolu kartı (komple-oyun tasarımı §3.5–3.6, §3.2; adım 7–9,
// 2026-10-26). Hepsi Oyna sekmesinden açılır; Usta kartı Kariyer haritasında Tur 4 sekmesinin altında da durur.
// Antrenman (tam sayfa): en üstte Temel ders (15 adımlı etkileşimli antrenman, eski "Tam antrenman" kartı: davranışı aynı,
// root.Antrenman), altında görev satırları (2026-10-30: 21 görev, üç saha grubunda; satırda son 5 oturumun çizgisi). Satırda görevin adı, ne öğrettiği, üç yıldızı (dolu/çerçeve) ve "2/3" yazısı:
// yıldız yalnız simgeyle okunmaz (HIG renk körlüğü). Satıra dokunmak görev kartını (alt sayfa) açar: nasıl yapılır, üç
// yıldızın eşiği sayıyla, ödül kuralı ("yalnız yeni yıldız") ve Başla.
// Günün maçı kartı: rakip (renk noktası + adı + tarzı), değiştirici (adı ve tek cümlelik kuralı), saha, uzunluk, ödülün
// durumu. Kural metni açık: "Seri yok, kaçırırsan bir şey kaybetmezsin." Geri sayım, saat, "son şans" yazısı yok.
// Usta yolu kartı: kilitliyken nedeni ("<son tur> finalini kazan"), açıkken sıradaki basamak (yer, rakip, uzunluk, final
// mi) ve geçilen basamak sayısı. Usta zorluk açıkça yazılır ("koşmak da sende").
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        string kartDril;

        // EkranTuru için (2026-10-26, r13): açık sayfayı en alta kaydır (antrenmanın altındaki aldatıcı vuruş listesi görünsün).
        public void SayfaSonaKaydir() { var sv = sayfa != null ? sayfa.Q<ScrollView>() : null; if (sv != null) sv.verticalScroller.value = sv.verticalScroller.highValue; }
        public void AntrenmanAc() { var once = gez.sekme; if (gez.sekme != Sekme.Oyna) gez.SekmeSec(Sekme.Oyna); gez.Ac(Gezinti.ANTRENMAN); Uygula(once, true); }
        public void DrilKartAc(string id) { var once = gez.sekme; kartDril = id; gez.Ac(Gezinti.DRIL); Uygula(once, true); }
        public void GunlukAc() { var once = gez.sekme; gez.Ac(Gezinti.GUNLUK); Uygula(once, true); }

        // 2026-11-06 13:40 turu (fikir 2, oyuncu gözü YAP; defter "Antrenman: önce oyna, sonra çalış"): üstte iki giriş yan yana
        // (Sonsuz ralli + rekor, Temel ders), altında "Görevler ★ n/63" ve üç saha sekmesi; yalnız seçili sahanın görevleri (21 → 7
        // satır). Son seçilen saha tuytop_antrenman_saha'da (cihaz tercihi). Görev öneren yerler (maç sonu, Dril.Oner) AntrenmanGrup
        // ile doğru sahayı açar. Eski 3 satırlık açıklama paragrafı kalktı: aynı kural görev kartında ("{0} top · skor yok", ödül satırı).
        const string SAHA_ANAHTAR = "tuytop_antrenman_saha";
        int antSaha = -1;
        public void AntrenmanGrup(string grup) { int g = System.Array.IndexOf(Dril.GRUP, grup); if (g >= 0) { antSaha = g; PlayerPrefs.SetInt(SAHA_ANAHTAR, g); } }
        void AntrenmanKur(VisualElement p)
        {
            SayfaBasi(p, "Antrenman");
            var sv = Kaydirma(p); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            var pr = Ayar.ilerleme;
            if (antSaha < 0) antSaha = Mathf.Clamp(PlayerPrefs.GetInt(SAHA_ANAHTAR, 0), 0, Dril.GRUP.Length - 1);
            // Üstte iki giriş: Sonsuz ralli (ödülsüz, yalnız rekor) ve Temel ders (durumu: bitti ya da 15 adım). Büyük yazıda alt alta.
            var ust = Satir("ant-cift"); c.Add(ust);
            int rk = GameRoot.SonsuzRekor;
            ust.Add(Dugme(L.T("Sonsuz ralli"), () => root.SonsuzOyna(), "ikincil-kart", rk > 0 ? L.T("Rekorun {0} vuruş", rk) : L.T("Kaçırana kadar, her 5 vuruşta hızlanır")));
            ust.Add(Dugme(L.T("Temel ders"), () => root.Antrenman(), "ikincil-kart", Ayar.antrenman ? L.T("Bitti") : L.T("15 adım, skor yok")));
            var bas = Satir("arali"); c.Add(bas);
            bas.Add(Yazi(L.T("Görevler"), 13, "etiket")); bas.Add(YildizSayac(pr.AntrenmanYildiz, Dril.ToplamYildiz, 15));
            // 2026-11-20 (inceleme, fikir 17:06 #4; §7 ödül bilgisi kararın verildiği yerde): görev ödülü yalnız görev alt sayfasında
            // 12 pt yazıyordu; oyuncu Antrenman'ı Tüy kaynağı olarak görmüyordu (Günün maçı ödülü listede, görevlerinki değil).
            // Bir kez, başlığın altında tek satır; satır başına rozet yok. Değerler Gear'dan, ekonomi aynı.
            var odS = Satir("rakip-satir"); odS.pickingMode = PickingMode.Ignore; c.Add(odS); odS.Add(Ikon(Ikonlar.Tuy, 14, ALTIN));
            var odY = Yazi(L.T("+{0} Tüy, +{1} XP her yeni yıldız", Gear.DRIL_TUY, Gear.DRIL_XP), 12, "soluk"); odY.style.marginLeft = 4; odS.Add(odY);
            Erisim.Etiket(odS, odY.text, "metin");
            var seg = Satir("segment"); c.Add(seg);
            for (int g = 0; g < Dril.GRUP.Length; g++)
            {
                int gg = g; int y = 0, n = 0; foreach (var d in Dril.Liste) if (d.grup == Dril.GRUP[g]) { n++; y += pr.DrilYildiz(d.id); }
                var cp = Cip(L.T(Dril.GRUP_AD[g]), antSaha == g, () => { if (antSaha == gg) return; AntrenmanGrup(Dril.GRUP[gg]); Uygula(gez.sekme, false); }, y + "/" + n * Dril.YILDIZ);
                cp.AddToClassList("segment-cip"); var eb = cp.userData as ErisimBilgi; if (eb != null) eb.rol = "sekme"; seg.Add(cp);
            }
            foreach (var d in Dril.Liste) if (d.grup == Dril.GRUP[antSaha]) c.Add(DrilSatir(d));
            AldatmaBolum(c);   // 2026-10-26 aldatıcı vuruşlar: kilitliler de görünür, ne açtığıyla (Game/UI/AldatmaListe)
            c.Add(Bosluk(24));
        }

        VisualElement DrilSatir(DrilTanim d)
        {
            var pr = Ayar.ilerleme; int y = pr.DrilYildiz(d.id);
            var kart = Kart("dugum"); string id = d.id; kart.AddManipulator(new Clickable(() => { Tik(); DrilKartAc(id); }));
            var r = Satir("basamak-satir"); r.pickingMode = PickingMode.Ignore; kart.Add(r);
            var orta = new VisualElement(); orta.AddToClassList("basamak-orta"); orta.pickingMode = PickingMode.Ignore; r.Add(orta);
            // 2026-11-10 (fikir 02:05 #6, r147): EN büyük yazıda "Behind-the-back" heceden bölünüyordu; en uzun kelime sığmazsa küçülür.
            var dad = Yazi(L.T(d.ad), 16, "kalin"); KelimeSigdir(dad); orta.Add(dad);
            orta.Add(Yazi(L.T(d.ogretir), 13, "soluk"));
            var yr = Satir("rakip-satir"); yr.pickingMode = PickingMode.Ignore; orta.Add(yr); yr.Add(UcYildiz((1 << y) - 1, 16)); var yy = Yazi(y + "/3", 12, "soluk"); yy.style.marginLeft = 4; yr.Add(yy);
            // İlerleme: son 5 oturumun isabet yüzdesi yalnız yazıyla (2026-11-06 unslop, öneri artifact'i YAP: her satırdaki 6 px'lik
            // mini çubuklar okunmuyor, her karta süs grafik ekliyordu). Yıldız gibi geri alınmaz bir şey değil, yalnız gidişatı gösterir.
            var gec = pr.DrilGecmis(d.id); string gecYazi = null;
            if (gec.Count > 0)
            {
                gecYazi = gec.Count > 1 ? L.T("Son {0} oturum: %{1} → %{2}", gec.Count, gec[0], gec[gec.Count - 1]) : L.T("Son oturum: %{0}", gec[0]);
                var gy = Yazi(gecYazi, 12, "soluk"); gy.style.marginTop = 4; gy.pickingMode = PickingMode.Ignore; orta.Add(gy);
            }
            // 2026-11-06 13:40: "Yeni/Devam/Tamam" yazısı kalktı (21 satırın hepsinde aynı kelime bilgi taşımıyordu); durum yıldız + "n/3".
            string durum = L.T(y >= 3 ? "Tamam" : y > 0 ? "Devam" : "Yeni");   // yalnız ekran okuyucu için
            Erisim.Etiket(kart, L.T("Görev") + ", " + L.T(d.ad) + ", " + L.T(d.ogretir) + ", " + L.T("{0}/3 yıldız", y) + (gecYazi != null ? ", " + gecYazi : "") + ", " + durum, "dugme");
            return kart;
        }

        void DrilKartKur()
        {
            var d = Dril.Bul(kartDril); if (d == null) { AltKapat(); return; }
            var pr = Ayar.ilerleme; int y = pr.DrilYildiz(d.id);
            var c = AltSayfaKur(L.T(d.ad));
            c.Add(Yazi(L.T("Görev") + " · " + L.T("{0} top", d.top) + " · " + L.T("skor yok"), 13, "etiket"));
            var ip = Yazi(L.T(d.ogretir), 15, "ipucu"); Erisim.Etiket(ip, ip.text, "metin"); c.Add(ip);
            var na = Yazi(L.T(d.nasil), 14); Erisim.Etiket(na, na.text, "metin"); c.Add(na);
            c.Add(Grup(L.T("Yıldızlar")));
            for (int i = 0; i < Dril.YILDIZ; i++) YildizSatir(c, y > i, L.T("12 topta {0} isabet", d.esik[i]));
            var od = Yazi(L.T("Her yeni yıldız +{0} Tüy, +{1} XP. Aynı yıldız ikinci kez ödül vermez.", Gear.DRIL_TUY, Gear.DRIL_XP), 12, "soluk"); Erisim.Etiket(od, od.text, "metin"); c.Add(od);
            string id = d.id;
            AltEylem(Dugme(L.T(y > 0 ? "Yeniden oyna" : "Başla"),   // 2026-11-05 unslop: görev adı sayfa başlığında; düğmede tekrarı iki satıra kırıyordu
                 () => { gez.AltKapat(); Uygula(gez.sekme, false); root.DrilOyna(id); }, "dugme--birincil", null, 19));
        }

        // ---- Günün maçı ----
        GunlukMac Bugun() { return Gunluk.Bugun(System.DateTime.Now); }

        VisualElement GunlukOzetKart()
        {
            var g = Bugun(); var st = Match.STIL[g.stil]; bool alindi = Ayar.ilerleme.GunlukAlindi(g.tarih);
            var kart = Kart("siradaki"); kart.AddManipulator(new Clickable(() => { Tik(); GunlukAc(); }));
            var bas = Satir("arali"); kart.Add(bas);
            bas.Add(Yazi(L.T("Günün maçı"), 12, "etiket"));
            bas.Add(alindi ? IkonYazi(true, L.T("Bugün kazandın"), 12, "durum-gecti", SOLUK) : Yazi(L.T("+{0} Tüy", Gunluk.OdulTuy), 12, "altin"));
            kart.Add(Yazi(L.T(g.deg.ad), 18, "kalin"));
            var rk = Satir("rakip-satir"); kart.Add(rk);
            var nok = new VisualElement(); nok.AddToClassList("renk-nokta"); nok.style.backgroundColor = Kit.Hex(st.renk); rk.Add(nok);
            rk.Add(Yazi(L.T("Rakip") + " " + st.isim + " · " + L.T(st.ad) + " · " + L.T("{0} sayı", g.uzunluk), 14, "soluk"));
            if (alindi) kart.Add(Yazi(L.T("Yarın yenisi. Yeniden oynayabilirsin."), 12, "soluk"));
            Erisim.Etiket(kart, L.T("Günün maçı") + ", " + L.T(g.deg.ad) + ", " + L.T("Rakip") + " " + st.isim + ", " + (alindi ? L.T("Bugün kazandın") : L.T("+{0} Tüy", Gunluk.OdulTuy)), "dugme");
            return kart;
        }

        void GunlukKartKur()
        {
            var g = Bugun(); var st = Match.STIL[g.stil]; bool alindi = Ayar.ilerleme.GunlukAlindi(g.tarih);
            var c = AltSayfaKur(L.T("Günün maçı"));
            c.Add(Yazi(L.T("Bugün herkes aynı maçı oynar."), 13, "etiket"));
            var rr = Satir("profil-ust"); c.Add(rr);
            var av = new VisualElement(); av.AddToClassList("avatar"); av.style.backgroundColor = Kit.Hex(st.renk); AvatarIc(av, Portre.Rakip(st), 30); rr.Add(av);
            var ra = new VisualElement(); ra.AddToClassList("esnek-yatay"); rr.Add(ra);
            // 2026-11-10 inceleme (fikir 02:05 #4): saha, uzunluk ve zorluk ayrı satırdı; rakibin tarzıyla aynı satırda (maçın
            // kimliği tek yerde), sayfa altı yazı bloğundan beşe iner, ödül satırı öne çıkar.
            bool usta = g.deg.id == "usta";
            string bilgi = L.T(st.ad) + " · " + L.T(CourtView.SAHA[g.saha].ad) + " · " + L.T("{0} sayı", g.uzunluk) + " · " + L.T(usta ? "Usta" : Ayar.ZorlukAd);
            ra.Add(Yazi(st.isim, 20, "kalin")); var bl = Yazi(bilgi, 14, "soluk"); bl.style.whiteSpace = WhiteSpace.Normal; ra.Add(bl);
            Erisim.Etiket(rr, L.T("Rakip") + " " + st.isim + ", " + bilgi, "metin");
            var dk = Kart("imza-kart"); c.Add(dk);
            string acik = L.T(g.deg.aciklama);
            dk.Add(Yazi(L.T(g.deg.ad), 16, "kalin")); dk.Add(Yazi(acik, 14, "ipucu"));
            Erisim.Etiket(dk, L.T(g.deg.ad) + ". " + acik, "metin");
            string odM = alindi ? L.T("Bugünün ödülü alındı. Yarın yenisi.") : L.T("Kazanınca bir kez +{0} Tüy, +{1} XP · kaybetsen de maç ödülü", Gunluk.OdulTuy, Gunluk.OdulXp);
            var od = alindi ? IkonYazi(true, odM, 14, "durum-gecti", SOLUK) : Yazi(odM, 14, "altin");
            Erisim.Etiket(od, odM, "metin"); c.Add(od);
            // 2026-11-13 (inceleme 12:05 #6): ayrı "Seri yok." satırı kalktı: olmayan kavramı anlatıyordu; kayıp ödülü ödül satırında.
            AltEylem(Dugme(L.T(alindi ? "Yeniden oyna" : "Oyna") + ": " + L.T("Günün maçı"), () => { gez.AltKapat(); Uygula(gez.sekme, false); root.GunlukOyna(); }, "dugme--birincil", null, 19));
        }

        // ---- Usta yolu ----
        VisualElement UstaKart()
        {
            var pr = Ayar.ilerleme; bool acik = UstaYolu.Acik(pr);
            var kart = Kart("tur-kart"); if (!acik) kart.AddToClassList("kilitli");
            var bas = Satir("arali"); kart.Add(bas);
            // Usta'da ad aynı; zorluk başlığın yanında küçük etiket (Kariyer'deki zorluk etiketi gibi, yazıyla: yalnız renk değil).
            // 2026-11-19 inceleme (fikir 07:06 #6, §7 öğe bütçesi): zorluk kartta üç kez yazıyordu (başlık, rakip satırı, açıklama).
            // Artık yalnız bu etikette ve her kademede (önceden yalnız Usta'da; rakip satırından kalkınca bilgi kaybolmasın).
            if (acik) { var bs = Satir(null); bs.Add(Yazi(L.T(Ayar.YolAd), 20, "kalin")); var ue = Yazi(L.T(Ayar.ZorlukAd), 13, "etiket"); ue.style.marginLeft = 8; bs.Add(ue); bas.Add(bs); }
            else bas.Add(Yazi(L.T(Ayar.YolAd), 20, "kalin"));
            // 2026-11-06 (unslop denetimi, öneri artifact'i YAP): altın "0 basamak geçildi" sıfırı başarı gibi gösteriyordu. Sıfırken
            // yazı yok; sonrasında soluk sayaç (ödül anı kariyer kartında, burada yalnız bilgi).
            if (acik && pr.Usta > 0) bas.Add(Yazi(L.T("{0} basamak geçildi", pr.Usta), 13, "soluk"));
            if (!acik)
            {
                // 2026-11-19 inceleme (fikir 07:06 #1): kilit cümlesi "Dünya Finalleri'ni kazan" diyordu; 11 Kas'tan beri kariyer 8 tur
                // ve açılış son turun finali (Ilerleme.UstaAcik). Ad son turdan okunur ki yeni tur eklenince yine doğru kalsın.
                int sn = KariyerVeri.Turlar.Length - 1; string kosul = sn >= 0 ? L.T("{0} finalini kazan", L.T(KariyerVeri.Turlar[sn].ad)) : L.T("Kilitli");
                var kl = Satir("rakip-satir"); kart.Add(kl); kl.Add(Ikon(Ikonlar.Kilit, 18, SOLUK)); var ky = Yazi(kosul, 14, "soluk"); ky.style.marginLeft = 6; kl.Add(ky);
                { var ky2 = Yazi(L.T("Sonu olmayan merdiven. Kaybetmek geri düşürmez."), 13, "soluk"); ky2.style.whiteSpace = WhiteSpace.Normal; kart.Add(ky2); }
                Erisim.Etiket(kart, L.T(Ayar.YolAd) + ", " + L.T("Kilitli") + ", " + kosul, "metin");
                return kart;
            }
            var b = UstaYolu.Basamak(pr.Usta); var st = Match.STIL[b.stil];
            kart.Add(Yazi(L.T("Sıradaki") + ": " + L.T(b.yer) + (b.final ? " · " + L.T("Final") : ""), 16, "kalin"));
            var rk = Satir("rakip-satir"); kart.Add(rk);
            var nok = new VisualElement(); nok.AddToClassList("renk-nokta"); nok.style.backgroundColor = Kit.Hex(st.renk); rk.Add(nok);
            rk.Add(Yazi(L.T("Rakip") + " " + st.isim + " · " + L.T(st.ad) + " · " + L.T("{0} sayı", b.uzunluk), 14, "soluk"));
            { var ac2 = Yazi(L.T("Kaybetmek geri düşürmez."), 12, "soluk"); ac2.style.whiteSpace = WhiteSpace.Normal; kart.Add(ac2); }
            kart.Add(Dugme(L.T("Şampiyon yolunu oyna"), () => root.UstaOyna(), "dugme--birincil", L.T("{0}. basamak", pr.Usta + 1), 18));
            Erisim.Etiket(kart, L.T(Ayar.YolAd) + ", " + L.T(Ayar.ZorlukAd) + ", " + L.T("{0} basamak geçildi", pr.Usta) + ", " + L.T("Sıradaki") + " " + L.T(b.yer), "metin");
            return kart;
        }
    }
}
