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
            ad.Add(Yazi(p.Ad ?? L.T("Oyuncu"), 22, "kalin")); ad.Add(Yazi(L.T(un.ad) + Karakter.KademeEki(un, p), 14, "altin"));
            var svr = Satir("rakip-satir"); svr.style.marginTop = 6; ad.Add(svr);
            var halka = new VisualElement(); halka.AddToClassList("sv-halka"); svr.Add(halka);
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

            // ad (yalnız cihazda)
            c.Add(Grup(L.T("Adın")));
            var tf = new TextField { value = p.Ad ?? "", maxLength = Ilerleme.AD_AZAMI, isDelayed = true }; tf.AddToClassList("ad-alan"); tf.style.fontSize = Mathf.Round(16 * K);
            tf.RegisterValueChangedCallback(e => { p.Ad = e.newValue; Ayar.Kaydet(); kirli[(int)Sekme.Profil] = true; var kk = sekmeKok[(int)Sekme.Profil]; kk.schedule.Execute(() => { if (gez.sekme == Sekme.Profil && gez.Ust == null) SekmeKur(Sekme.Profil); }).StartingIn(16); });
            Erisim.Etiket(tf, L.T("Adın") + ", " + L.T("yalnız bu cihazda tutulur"), "metin_alani", false, false, p.Ad ?? ""); c.Add(tf);
            c.Add(Yazi(L.T("Ad yalnız bu cihazda tutulur, hiçbir yere gönderilmez."), 12, "soluk"));

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

            c.Add(Grup(L.T("İstatistikler")));
            var iz = Satir("izgara"); c.Add(iz);
            // 2026-11-07 inceleme: Rekor ralli, Galibiyet, Smaç üstteki üç büyük sayıda; ızgarada tekrar edilmez (büyük yazıda üç satır kısa).
            Deger(iz, L.T("Maç"), p.Mac.ToString());
            Deger(iz, L.T("Görev"), Ayar.gorev.ToString());
            Deger(iz, L.T("Kariyer yıldızı"), Ayar.ilerleme.ToplamYildiz + "/" + TurSayisiVeri() * 18);
            // tur başına yıldızlar (tasarım §2.3 Profil): yalnız verisi olan turlar; kilitli tur "Kilitli" yazısıyla
            for (int tu = 0; tu < KariyerVeri.Turlar.Length; tu++)
            {
                if (!KariyerVeri.Turlar[tu].Var) continue; bool ac = Ayar.ilerleme.TurAcik(tu);
                Deger(iz, L.T(TurAd(tu)), ac ? Ayar.ilerleme.TurYildiz(tu) + "/18 ★" : L.T("Kilitli"));
            }
            // 2026-10-26 adım 7–9
            Deger(iz, L.T("Antrenman yıldızı"), p.AntrenmanYildiz + "/" + Dril.ToplamYildiz);
            Deger(iz, L.T("Günün maçı"), p.GunlukSay.ToString());
            if (p.UstaAcik) Deger(iz, L.T("Usta yolu"), p.Usta.ToString());
            if (p.Mac == 0 && Ayar.wins > 0) c.Add(Yazi(L.T("Maç ve smaç sayısı bu sürümden sonra oynanan maçları sayar."), 12, "soluk"));

            c.Add(Grup(L.T("Başarımlar")));
            for (int i = 0; i < BASARIM.GetLength(0); i++) BasarimSatir(c, BASARIM[i, 0], L.T(BASARIM[i, 1]));
            for (int i = 1; i <= Match.KARIYER.Length; i++) BasarimSatir(c, "bs_kariyer_" + i, L.T("Kariyer") + ": " + L.T(Match.KARIYER[i - 1].yer));
            for (int tu = 1; tu < KariyerVeri.Turlar.Length; tu++) if (KariyerVeri.Turlar[tu].Var) BasarimSatir(c, "bs_tur_" + (tu + 1), L.T("{0} finalini kazan", L.T(TurAd(tu))));
            BasarimSatir(c, Basarim.EFSANE, L.T("Seviye 30'a ulaş"));
            BasarimSatir(c, Basarim.ANTRENOR, L.T("Antrenmanın 24 yıldızını topla"));
            BasarimSatir(c, Basarim.GUNUN_MACI, L.T("Bir Günün maçını kazan"));
            BasarimSatir(c, Basarim.USTA6, L.T("Usta yolunda 6 basamak geç"));
            BasarimSatir(c, Basarim.IMZA_OKUR, L.T("Toplam {0} imza vuruşu oku", Basarim.IMZA_OKUR_HEDEF));
            if (Basarim.acik) { c.Add(Bosluk(6)); c.Add(Dugme(L.T("Game Center · Başarımlar"), () => { Yerel.Hap("tick"); Basarim.Goster(); })); }
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
