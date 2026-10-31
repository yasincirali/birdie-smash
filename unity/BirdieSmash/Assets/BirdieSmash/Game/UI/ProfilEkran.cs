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
            var kart = Kart(); c.Add(kart);
            var r = Satir("profil-ust"); kart.Add(r);
            var av = new VisualElement(); av.AddToClassList("avatar"); av.style.backgroundColor = Kit.Hex(Ayar.FORMA); AvatarIc(av, Portre.Benim(), 30); r.Add(av);
            var ad = new VisualElement(); ad.AddToClassList("esnek-yatay"); r.Add(ad);
            ad.Add(Yazi(p.Ad ?? L.T("Oyuncu"), 19, "kalin")); ad.Add(Yazi(L.T(un.ad), 14, "altin"));
            ad.Add(Yazi(L.T("Seviye {0}", L_) + " · " + L.T("Tarz") + ": " + L.T(tz.ad), 14, "soluk"));
            ad.Add(Yazi(L.T("Raket") + ": " + L.T(Gear.GEAR["raket"][gr.raket].ad) + " · " + L.T("{0} Tüy", gr.eko.tuy), 14, "soluk"));
            Erisim.Etiket(r, (p.Ad ?? L.T("Oyuncu")) + ", " + L.T(un.ad) + ", " + L.T("Seviye {0}", L_) + ", " + L.T("Tarz") + " " + L.T(tz.ad), "metin");
            kart.Add(Cubuk(gerek > 0 ? kalan / (float)gerek : 1, CAMGOBEGI));
            kart.Add(Yazi(gerek > 0 ? L.T("Sonraki seviyeye {0} XP · maç kazandıkça Tüy ve XP gelir", gerek - kalan) : L.T("En yüksek seviye"), 12, "soluk"));
            // sıradaki açılış (Sv 30'da yok)
            for (int s2 = L_ + 1; s2 <= 30; s2++)
            {
                var ac = Karakter.Acilanlar(s2); if (ac.Count == 0) continue;
                var adlar = new List<string>(); foreach (var a in Karakter.AcilanlarTurlu(s2)) adlar.Add(a[0] != null ? L.T(a[0]) + ": " + L.T(a[1]) : L.T(a[1]));
                var sa = Yazi(L.T("Sıradaki açılış · Sv {0}", s2) + ": " + string.Join(" · ", adlar.ToArray()), 13, "ipucu"); Erisim.Etiket(sa, sa.text, "metin"); kart.Add(sa); break;
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
            foreach (var u in kz) { string id = u.id; us.Add(Cip(L.T(u.ad), u.id == un.id, () => { p.Unvan = id; Ayar.Kaydet(); kirli[(int)Sekme.Profil] = true; SekmeKur(Sekme.Profil); })); }
            foreach (var u in Karakter.Unvanlar)
            {
                if (kz.Contains(u)) continue; var ur = Satir("basarim"); ur.AddToClassList("kilitli"); c.Add(ur);
                ur.Add(Ikon(Ikonlar.Kilit, 18, SOLUK));
                // 2026-10-31 (SE ×2 turu: ad ve koşul yan yana sığmayıp "champion" harf harf bölünüyordu): ad üstte, koşul altında
                var us2 = new VisualElement(); us2.AddToClassList("esnek-yatay"); us2.pickingMode = PickingMode.Ignore; ur.Add(us2);
                us2.Add(Yazi(L.T(u.ad), 15)); us2.Add(Yazi(L.T(u.nasil), 12, "soluk"));
                Erisim.Etiket(ur, L.T("Unvan") + " " + L.T(u.ad) + ", " + L.T("Kilitli") + ", " + L.T(u.nasil), "metin");
            }

            c.Add(Grup(L.T("İstatistikler")));
            var iz = Satir("izgara"); c.Add(iz);
            Deger(iz, L.T("Rekor ralli"), Ayar.bestRally.ToString()); Deger(iz, L.T("Galibiyet"), Ayar.wins.ToString());
            Deger(iz, L.T("Maç"), p.Mac.ToString()); Deger(iz, L.T("Smaç"), p.Smac.ToString());
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
