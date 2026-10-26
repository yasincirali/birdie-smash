// Profil sekmesi (2026-10-26, adım 1): oyuncu kartı (seviye, XP, Tüy, raket), istatistikler (eskiden ana menünün alt
// satırı: rekor ralli, galibiyet, görev; + kariyer basamağı) ve başarımlar (yerel liste, tuytop_basarim; Game Center
// bağlıysa "Game Center · Başarımlar"). Hepsi cihazda hesaplanır, hiçbir yere gönderilmez. Ad, unvan, tarz ve tur
// yıldızları sonraki adımlarda (tasarım §2.3, §5.5). Kazanılmamış başarım soluk + "Henüz değil" yazısı (yalnız renkle değil).
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
            var gr = Ayar.gear; int L_, kalan, gerek; gr.OyuncuSv(out L_, out kalan, out gerek);
            var kart = Kart(); c.Add(kart);
            var r = Satir("profil-ust"); kart.Add(r);
            var av = new VisualElement(); av.AddToClassList("avatar"); av.style.backgroundColor = Kit.Hex(Ayar.FORMA); av.Add(Ikon(Ikonlar.Kisi, 30, KOYU)); r.Add(av);
            var ad = new VisualElement(); ad.AddToClassList("esnek-yatay"); r.Add(ad);
            ad.Add(Yazi(L.T("Oyuncu"), 19, "kalin")); ad.Add(Yazi(L.T("Seviye {0}", L_) + " · " + L.T("{0} Tüy", gr.eko.tuy), 14, "soluk"));
            ad.Add(Yazi(L.T("Raket") + ": " + L.T(Gear.GEAR["raket"][gr.raket].ad), 14, "soluk"));
            kart.Add(Cubuk(gerek > 0 ? kalan / (float)gerek : 1, CAMGOBEGI));
            kart.Add(Yazi(gerek > 0 ? L.T("Sonraki seviyeye {0} XP · maç kazandıkça Tüy ve XP gelir", gerek - kalan) : L.T("En yüksek seviye"), 12, "soluk"));

            c.Add(Grup(L.T("İstatistikler")));
            var iz = Satir("izgara"); c.Add(iz);
            Deger(iz, L.T("Rekor ralli"), Ayar.bestRally.ToString()); Deger(iz, L.T("Galibiyet"), Ayar.wins.ToString());
            Deger(iz, L.T("Görev"), Ayar.gorev.ToString());
            Deger(iz, L.T("Kariyer"), Ayar.sampiyon ? L.T("Şampiyon") : Mathf.Min(Ayar.kariyer + 1, Match.KARIYER.Length) + "/" + Match.KARIYER.Length);

            c.Add(Grup(L.T("Başarımlar")));
            for (int i = 0; i < BASARIM.GetLength(0); i++) BasarimSatir(c, BASARIM[i, 0], L.T(BASARIM[i, 1]));
            for (int i = 1; i <= Match.KARIYER.Length; i++) BasarimSatir(c, "bs_kariyer_" + i, L.T("Kariyer") + ": " + L.T(Match.KARIYER[i - 1].yer));
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
            r.Add(Ikon(var_ ? Ikonlar.Tik : Ikonlar.Kilit, 20, var_ ? LIMON : SOLUK));
            r.Add(Yazi(ad, 15, "esnek-yatay")); string d = L.T(var_ ? "Kazanıldı" : "Henüz değil"); r.Add(Yazi(d, 12, var_ ? "durum-gecti" : "durum-kilit"));
            Erisim.Etiket(r, ad + ", " + d, "metin");
        }
    }
}
