// Profil sekmesi (2026-10-26, adım 1): oyuncu kartı (seviye, XP, Tüy, raket), istatistikler (eskiden ana menünün alt
// satırı: rekor ralli, galibiyet, görev; + kariyer basamağı) ve başarımlar (yerel liste, tuytop_basarim; Game Center
// bağlıysa "Game Center · Başarımlar"). Hepsi cihazda hesaplanır, hiçbir yere gönderilmez. Kazanılmamış başarım soluk +
// "Henüz değil" yazısı (yalnız renkle değil).
// 2026-10-26 adım 6 (karakter, tasarım §2.3, §5.5): oyuncu kartında ad (yalnız cihazda; Game Center takma adı okunmaz,
// izin ekranı ve ağ yok), unvan ve tarz; unvan seçimi (kazanılanlar çip, kazanılmayanlar kilit + nasıl alınır); maç ve
// smaç sayısı; "sıradaki açılış" (bir sonraki seviyede ne açılıyor: ara hedef görünür olsun, tasarım §5.7). Ad alanı
// isDelayed: yazarken değil, bitince kaydedilir (her tuşta kayıt ve yeniden kurulum yok).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        // Başarım kimlikleri App Store Connect'teki ile aynı (Basarim); adlar yalnız bu liste için.
        static readonly string[,] BASARIM = {
            { Basarim.ANTRENMAN, "Antrenmanı bitir" }, { Basarim.AKIS, "Akışa gir" }, { Basarim.RALLI20, "20 vuruşluk ralli" },
            { Basarim.SICRAMA, "Sıçrayarak smaç" }, { Basarim.GOREV3, "Bir maçta 3 görev" } };

        void ProfilKur(VisualElement k)
        {
            k.Clear(); var sv = Kaydirma(k); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            var t = Yazi(L.T("Profil"), 24, "sekme-baslik"); Erisim.Etiket(t, L.T("Profil"), "baslik"); c.Add(t);
            var gr = Ayar.gear; var p = Ayar.ilerleme; int L_, kalan, gerek; gr.OyuncuSv(out L_, out kalan, out gerek);
            var un = Karakter.Gosterilen(p, L_, p.AntrenmanYildiz); var tz = Karakter.Tarz(Ayar.Tarz);
            // 2026-11-06 ek tur fikir 5 (inceleme YAP; defter "Profil bir oyuncu kartı olsun"): üst kart ayar listesi gibi okunuyordu
            // ("Sv 2 ile açılır / Pamuk / Saç modeli: …"); oyuncunun gurur duyduğu şey aşağıdaki listede kayboluyordu. Şimdi:
            // büyük portre + ad + unvan + seviye halkası; altında üç büyük sayı (rekor ralli, smaç, galibiyet); kupa rafı (tur
            // finalleri, kazanılmamış siluet, dokununca Kariyer o tura açılır); sıradaki açılış tek cümle. Tam istatistik listesi
            // aşağıda aynen kalır. Büyük yazıda (K ≥ 1,35) üç sayı alt alta.
            var kart = Kart("oyuncu-kart"); c.Add(kart);
            var r = Satir("profil-ust"); kart.Add(r);
            var av = new VisualElement(); av.AddToClassList("avatar"); av.AddToClassList("avatar--buyuk"); av.style.backgroundColor = Kit.Hex(Ayar.FORMA); AvatarIc(av, Portre.Benim(), 54); r.Add(av);
            var ad = new VisualElement(); ad.AddToClassList("esnek-yatay"); r.Add(ad);
            ad.Add(Yazi(p.Ad ?? L.T("Oyuncu"), 22, "kalin")); var unY = Yazi(L.T(un.ad) + Karakter.KademeEki(un, p), 14, "altin"); ad.Add(unY); SozcukSigdir(unY);   // 2026-11-07: ×2'de "Neighbourhoo/d" bölünüyordu
            var svr = Satir("rakip-satir"); svr.style.marginTop = 6; ad.Add(svr);
            var halka = new VisualElement(); halka.AddToClassList("sv-halka"); svr.Add(halka);
            { float hs = Mathf.Round(30 * Mathf.Max(1, K * .85f)); halka.style.width = hs; halka.style.height = hs; halka.style.borderTopLeftRadius = halka.style.borderTopRightRadius = halka.style.borderBottomLeftRadius = halka.style.borderBottomRightRadius = hs / 2; }   // 2026-11-07: ×2'de seviye rakamı 30 pt halkada kesiliyordu (r127)
            var hi = Yazi(L_.ToString(), 13, "kalin"); hi.AddToClassList("sv-halka-ic"); halka.Add(hi);
            var hy = Yazi(gerek > 0 ? L.T("Sonraki seviyeye {0} XP", gerek - kalan) : L.T("En yüksek seviye"), 12, "soluk"); hy.style.marginLeft = 6; hy.style.flexShrink = 1; svr.Add(hy);
            ad.Add(Yazi(L.T(tz.ad) + " · " + L.T(Gear.GEAR["raket"][gr.raket].ad), 13, "soluk"));
            Erisim.Etiket(r, (p.Ad ?? L.T("Oyuncu")) + ", " + L.T(un.ad) + Karakter.KademeEki(un, p) + ", " + L.T("Seviye {0}", L_) + ", " + L.T("Tarz") + " " + L.T(tz.ad), "metin");
            kart.Add(Cubuk(gerek > 0 ? kalan / (float)gerek : 1, CAMGOBEGI));
            // üç büyük sayı
            var uc = Satir("uc-sayi"); if (K >= 1.35f) uc.AddToClassList("uc-sayi--dikey"); kart.Add(uc);
            System.Action<string, string> Buyuk_ = (deger, et) => { var b = new VisualElement(); b.AddToClassList("uc-sayi-oge"); b.Add(Yazi(deger, 24, "rakam")); b.Add(Yazi(et, 12, "soluk")); Erisim.Etiket(b, et + " " + deger, "metin"); uc.Add(b); };
            Buyuk_(Ayar.bestRally.ToString(), L.T("Rekor ralli")); Buyuk_(p.Smac.ToString(), L.T("Smaç")); Buyuk_(Ayar.wins.ToString(), L.T("Galibiyet"));
            // kupa rafı: verisi olan her turun finali
            int kz_ = 0, top_ = 0; var raf = Satir("kupa-raf");
            for (int tu = 0; tu < KariyerVeri.Turlar.Length; tu++)
            {
                if (!KariyerVeri.Turlar[tu].Var) continue; top_++; int g = KariyerVeri.Global(tu, KariyerVeri.BASAMAK - 1); bool var_ = p.Kazanildi(g); if (var_) kz_++;
                var hk = new VisualElement(); hk.AddToClassList("kupa-hucre"); if (!var_) hk.AddToClassList("kupa-hucre--yok");
                hk.Add(Ikon(Ikonlar.Kupa, 28, var_ ? ALTIN : SOLUK)); int tt = tu;
                hk.AddManipulator(new Clickable(() => { Yerel.Hap("tick"); KariyerTurSec(tt); SekmeAc(Sekme.Kariyer); }));
                Erisim.Etiket(hk, L.T(TurAd(tu)) + ", " + L.T(var_ ? "Kazanıldı" : "Kazanılmadı"), "dugme"); raf.Add(hk);
            }
            var rb = Satir("arali"); rb.style.marginTop = 10; kart.Add(rb); rb.Add(Yazi(L.T("Kupa rafı"), 14, "kalin")); rb.Add(Yazi(kz_ + "/" + top_, 13, "soluk"));
            kart.Add(raf);
            // sıradaki açılış (Sv 30'da yok): tek cümle
            for (int s2 = L_ + 1; s2 <= 30; s2++)
            {
                var ac = Karakter.Acilanlar(s2); if (ac.Count == 0) continue;
                var tl = Karakter.AcilanlarTurlu(s2); var parca = new List<string>(); var say = new Dictionary<string, int>();
                foreach (var a in tl) { int n; say.TryGetValue(a[1], out n); say[a[1]] = n + 1; }
                // 2026-11-06: aynı ad iki yuvada açılınca ("Kırmızı, Kırmızı") hangi eşya olduğu yazılır: "Kırmızı (Forma rengi)"
                foreach (var a in tl) parca.Add(say[a[1]] > 1 && a[0] != null ? L.T(a[1]) + " (" + L.T(a[0]) + ")" : L.T(a[1]));
                var sa = Yazi(L.T("{0}. seviyede açılır: {1}", s2, string.Join(", ", parca.ToArray())), 13, "soluk"); sa.style.marginTop = 10; sa.style.whiteSpace = WhiteSpace.Normal;
                Erisim.Etiket(sa, sa.text, "metin"); kart.Add(sa); break;
            }

            GelisimKart(c);   // 2026-11-10 oyuncu gelişimi: oyuncu kartının hemen altında (kartın anlattığı oyuncunun değerleri)

            // ad (yalnız cihazda)
            c.Add(Grup(L.T("Adın")));
            var tf = new TextField { value = p.Ad ?? "", maxLength = Ilerleme.AD_AZAMI, isDelayed = true }; tf.AddToClassList("ad-alan"); tf.style.fontSize = Mathf.Round(16 * K);
            tf.RegisterValueChangedCallback(e => { p.Ad = e.newValue; Ayar.Kaydet(); kirli[(int)Sekme.Profil] = true; var kk = sekmeKok[(int)Sekme.Profil]; kk.schedule.Execute(() => { if (gez.sekme == Sekme.Profil && gez.Ust == null) SekmeKur(Sekme.Profil); }).StartingIn(16); });
            Erisim.Etiket(tf, L.T("Adın") + ", " + L.T("cihazında ve kendi iCloud hesabında tutulur"), "metin_alani", false, false, p.Ad ?? ""); c.Add(tf);
            // Yer tutucu (2026-11-07, yasin "yap", arayüz cilası): boş alan ne yazılacağını söylesin. Unity 6 textEdition.placeholder
            // yerine kendi soluk etiketimiz (mono ile Unity'siz derleme 2021 başvurularıyla yapılıyor); alan boşken ve odak yokken görünür.
            var ph = Yazi(L.T("Adını yaz"), 16, "soluk"); ph.AddToClassList("ad-yer-tutucu"); ph.pickingMode = PickingMode.Ignore; tf.Add(ph);
            System.Action phG = () => ph.style.display = string.IsNullOrEmpty(tf.value) && tf.focusController?.focusedElement != tf ? DisplayStyle.Flex : DisplayStyle.None;
            tf.RegisterCallback<FocusInEvent>(e => ph.style.display = DisplayStyle.None); tf.RegisterCallback<FocusOutEvent>(e => phG()); tf.RegisterValueChangedCallback(e => phG()); phG();
            // 2026-11-07 (inceleme 16:05 #4): ad tuytop_ilerleme içinde BulutKayit ile oyuncunun kendi iCloud'una da gider;
            // "hiçbir yere gönderilmez" yanlıştı. Bize ya da başkasına gitmediği doğru, cümle bunu söyler.
            c.Add(Yazi(L.T("Adın cihazında ve kendi iCloud hesabında tutulur; bize ya da başkasına gönderilmez."), 12, "soluk"));

            // unvanlar
            c.Add(Grup(L.T("Unvan")));
            var kz = Karakter.Kazanilan(p, L_, p.AntrenmanYildiz); var us = Satir("cip-sarar"); c.Add(us);   // 2026-10-31 (yasin IMG_1517): 5 unvan eşit dar çipe sıkışıp harf harf bölünüyordu; çip yazısı kadar geniş, sığmayan alt satıra
            foreach (var u in kz) { string id = u.id; us.Add(Cip(L.T(u.ad) + Karakter.KademeEki(u, p), u.id == un.id, () => { p.Unvan = id; Ayar.Kaydet(); kirli[(int)Sekme.Profil] = true; SekmeKur(Sekme.Profil); })); }
            // Kademe (2026-11-07): yalnız seçili beceri unvanının sıradaki eşiği, tek satır; yeni satır listesi yok.
            int se_ = Karakter.SonrakiEsik(un, p);
            if (se_ > 0) { var ks = Yazi(L.T("{0}: sonraki kademe {1}/{2}", L.T(un.ad) + Karakter.KademeEki(un, p), p.Sayac(un.sayac), se_), 12, "soluk"); ks.style.whiteSpace = WhiteSpace.Normal; Erisim.Etiket(ks, ks.text, "metin"); c.Add(ks); }
            foreach (var u in Karakter.Unvanlar)
            {
                if (kz.Contains(u)) continue; var ur = Satir("basarim"); ur.AddToClassList("kilitli"); c.Add(ur);
                ur.Add(Ikon(Ikonlar.Kilit, 18, SOLUK));
                // 2026-10-31 (SE ×2 turu: ad ve koşul yan yana sığmayıp "champion" harf harf bölünüyordu): ad üstte, koşul altında
                var us2 = new VisualElement(); us2.AddToClassList("esnek-yatay"); us2.pickingMode = PickingMode.Ignore; ur.Add(us2);
                // Beceri unvanı (2026-11-07): koşulun yanında ilerleme ("212/300"); oyuncu ne kadar kaldığını görür, yeni satır yok.
                string nasil = L.T(u.nasil) + (u.sayac != null ? " · " + Mathf.Min(p.Sayac(u.sayac), u.esik) + "/" + u.esik : "");
                us2.Add(Yazi(L.T(u.ad), 15)); us2.Add(Yazi(nasil, 12, "soluk"));
                Erisim.Etiket(ur, L.T("Unvan") + " " + L.T(u.ad) + ", " + L.T("Kilitli") + ", " + nasil, "metin");
            }

            // Tur mühürleri (2026-11-07, yasin karar kartı "Ekle"; GD5): her tur için mühür ya da sıradaki eşik. Fiyatsız prestij, satılmaz.
            c.Add(Grup(L.T("Tur mühürleri")));
            var mk = Kart("muhur-kart"); c.Add(mk);
            for (int mt = 0; mt < KariyerVeri.Turlar.Length; mt++)
            {
                // 2026-11-08 inceleme (fikir 07:05 #2): kilitli tur "0/18" değil "Kilitli" der; tur yıldızları artık yalnız burada.
                if (!KariyerVeri.Turlar[mt].Var) continue; bool mac = p.TurAcik(mt); int mh = mac ? p.Muhur(mt) : 0, ys = p.TurYildiz(mt);
                var mr = Satir("arali"); mr.AddToClassList("muhur-satir"); mr.pickingMode = PickingMode.Ignore; mk.Add(mr);
                var sol = Satir("rakip-satir"); sol.pickingMode = PickingMode.Ignore; mr.Add(sol);
                sol.Add(Ikon(mh >= 2 ? Ikonlar.YildizDolu : Ikonlar.YildizBos, 18, mh >= 2 ? ALTIN : mh == 1 ? BRONZ : SOLUK));
                var ay = Yazi(L.T(TurAd(mt)), 15); ay.style.marginLeft = 8; sol.Add(ay);
                string dr = !mac ? L.T("Kilitli") : mh >= 2 ? L.T("Altın") : mh == 1 ? L.T("Bronz") + " · " + ys + "/18" : ys + "/18";
                mr.Add(Yazi(dr, 13, mh > 0 ? "durum-gecti" : "soluk"));
                Erisim.Etiket(mr, L.T(TurAd(mt)) + ", " + (!mac ? L.T("Kilitli") : mh >= 2 ? L.T("Altın tur mührü") : mh == 1 ? L.T("Bronz tur mührü") : L.T("{0}/{1} yıldız", ys, 18)), "metin");
            }
            mk.Add(Yazi(L.T("Bronz 9 yıldız, altın 18 yıldız. Satılmaz, yalnız oynayarak gelir."), 12, "soluk"));

            c.Add(Grup(L.T("İstatistikler")));
            var iz = Satir("izgara"); c.Add(iz);
            // 2026-11-07 inceleme: Rekor ralli, Galibiyet, Smaç üstteki üç büyük sayıda; ızgarada tekrar edilmez (büyük yazıda üç satır kısa).
            Deger(iz, L.T("Maç"), p.Mac.ToString());
            Deger(iz, L.T("Görev"), Ayar.gorev.ToString());
            Deger(iz, L.T("Kariyer yıldızı"), Ayar.ilerleme.ToplamYildiz + "/" + TurSayisiVeri() * 18);
            // Tur başına yıldızlar ızgaradan kalktı (2026-11-08 inceleme, fikir 07:05 #2): "Tur mühürleri" kartı aynı bilgiyi
            // veriyordu ve kilitli turda iki yer farklı söylüyordu (0/18 ile Kilitli); büyük yazıda ızgara dört satır kısaldı.
            // 2026-10-26 adım 7–9
            Deger(iz, L.T("Antrenman yıldızı"), p.AntrenmanYildiz + "/" + Dril.ToplamYildiz);
            Deger(iz, L.T("Günün maçı"), p.GunlukSay.ToString());
            if (p.UstaAcik) Deger(iz, L.T("Usta yolu"), p.Usta.ToString());
            if (p.Mac == 0 && Ayar.wins > 0) c.Add(Yazi(L.T("Maç ve smaç sayısı bu sürümden sonra oynanan maçları sayar."), 12, "soluk"));

            c.Add(Grup(L.T("Başarımlar")));
            // Game Center listenin sonundan başlığın altına (2026-11-07 inceleme, fikir 20:09 #3): Ayarlar'daki kopya kalktı, tek yer
            // burası; uzun listenin dibinde aranmasın. Metin aynı anahtar ("Game Center'da aç" yeni çeviri isterdi).
            if (Basarim.acik) { c.Add(Dugme(L.T("Game Center · Başarımlar") + "  ›", () => { Yerel.Hap("tick"); Basarim.Goster(); })); c.Add(Bosluk(6)); }
            for (int i = 0; i < BASARIM.GetLength(0); i++) BasarimSatir(c, BASARIM[i, 0], L.T(BASARIM[i, 1]));
            for (int i = 1; i <= Match.KARIYER.Length; i++) BasarimSatir(c, "bs_kariyer_" + i, L.T("Kariyer") + ": " + L.T(Match.KARIYER[i - 1].yer));
            for (int tu = 1; tu < KariyerVeri.Turlar.Length; tu++) if (KariyerVeri.Turlar[tu].Var) BasarimSatir(c, "bs_tur_" + (tu + 1), L.T("{0} finalini kazan", L.T(TurAd(tu))));
            BasarimSatir(c, Basarim.EFSANE, L.T("Seviye 30'a ulaş"));
            BasarimSatir(c, Basarim.ANTRENOR, L.T("Antrenmanın 24 yıldızını topla"));
            BasarimSatir(c, Basarim.GUNUN_MACI, L.T("Bir Günün maçını kazan"));
            BasarimSatir(c, Basarim.USTA6, L.T("Usta yolunda 6 basamak geç"));
            BasarimSatir(c, Basarim.IMZA_OKUR, L.T("Toplam {0} imza vuruşu oku", Basarim.IMZA_OKUR_HEDEF));
            c.Add(Bosluk(20));
        }

        void Deger(VisualElement iz, string ad, string deger)
        {
            var k = Kart("deger-kart"); k.Add(Yazi(deger, 26, "deger")); k.Add(Yazi(ad, 13, "soluk")); Erisim.Etiket(k, ad + ", " + deger, "metin"); iz.Add(k);
        }

        void BasarimSatir(VisualElement c, string id, string ad)
        {
            bool var_ = Basarim.Var(id); var r = Satir("basarim"); if (!var_) r.AddToClassList("kilitli"); c.Add(r);
            r.Add(Ikon(var_ ? Ikonlar.Tik : Ikonlar.Kilit, 20, var_ ? TURUNCU : SOLUK));
            r.Add(Yazi(ad, 15, "esnek-yatay")); string d = L.T(var_ ? "Kazanıldı" : "Henüz değil"); r.Add(Yazi(d, 12, var_ ? "durum-gecti" : "durum-kilit"));
            Erisim.Etiket(r, ad + ", " + d, "metin");
        }
    }
}
