// İlk maçtan sonra "Oyuncunu seç" (2026-10-31, Ar-Ge "Usta modu ve Dolap" K1; yasin "ilerle ve devam et"). Araştırma
// (karakter_olusturma.md): kısa oturumlu spor oyunlarında oyuncu ilk dakikada editöre sokulmaz (Tennis Clash, Badminton
// League hazır karakterle başlatır); kimlik bağı ilk maçtan SONRA, tek ekranda, hazır seçeneklerle kurulur. Kurallar:
//   · Bir kez: ilk maçtan menüye dönüşte, görünüş hâlâ varsayılansa ve Oyuncak 3B seçiliyse (beden ve saç yalnız orada).
//     Görünüşünü değiştirmiş eski oyuncuya hiç çıkmaz. Kayıt tuytop_oyuncu_sec ("1" = gösterildi).
//   · Dolap'ın üstünde alt sayfa: vitrin üstte kalır, karta dokunmak oyuncuyu canlı değiştirir. Altı kart yalnız ücretsiz,
//     Sv 1'de açık parçalardan: kilitli öğe, fiyat, Tüy yok (ilk dakikada "satın al" görmesin).
//   · Rastgele (yine yalnız açık parçalar), "Bununla oyna" (kalır, Oyna'ya döner), "Atla" (eski görünüşe döner).
//     Tutamakla aşağı çekip kapatmak seçili kartı bırakır (her şey Dolap'ta yine değişir, geri dönüşsüz bir şey yok).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        const string OYUNCU_SEC_ANAHTAR = "tuytop_oyuncu_sec";
        // ad, tip, saç biçimi, saç rengi, ten, forma, grip, kordaj, ayakkabı rengi (hepsi Sv 1, ücretsiz; veri/karakter.json, gear.json)
        static readonly string[][] HAZIR = {
            new[] { "Şimşek", "atlet", "kisa", "siyah", "t2", "mavi", "siyah", "beyaz", "beyaz" },
            new[] { "Rüzgâr", "kadin", "atkuyrugu", "kahve", "t1", "mor", "beyaz", "limon", "beyaz" },
            new[] { "Güneş", "atlet", "yan", "sari", "t4", "fistik", "turuncu", "beyaz", "siyah" },
            new[] { "Yıldız", "kadin", "yan", "siyah", "t5", "beyaz", "mor", "pembe", "beyaz" },
            new[] { "Kartal", "atlet", "atkuyrugu", "kahve", "t3", "beyaz", "mavi", "limon", "siyah" },
            new[] { "Fırtına", "kadin", "kisa", "sari", "t6", "mavi", "kirmizi", "beyaz", "dogal" },
        };
        Dictionary<string, object> oyuncuSecOnce; int oyuncuSecSecili = -1;

        // MenuyeDon'dan: gösterilmeli mi (bir kez, varsayılan görünüşte, Oyuncak 3B'de).
        bool OyuncuSecGerekli()
        {
            if (OdulAnlariKapali || PlayerPrefs.GetString(OYUNCU_SEC_ANAHTAR, "") == "1" || !Ayar.VaryantGorunur || root.match == null || root.match.ogr != null) return false;
            foreach (var k in Kozmetik.KAT) if (Ayar.KOZ[k] != Kozmetik.Varsayilan(k)) return false;
            foreach (var k in new[] { "ten", "sac", "ayakkabi" }) if (Ayar.GORUNUM[k] != "dogal") return false;
            return Ayar.gear.renk == "mavi";
        }
        public void OyuncuSecAc()
        {
            PlayerPrefs.SetString(OYUNCU_SEC_ANAHTAR, "1"); PlayerPrefs.Save();
            oyuncuSecOnce = KombinSimdi(); oyuncuSecSecili = -1;
            var once = gez.sekme; gez.SekmeSec(Sekme.Soyunma); karAlt = KarGecerli(KAR_TIP); gez.Ac(Gezinti.SEC); Uygula(once, true);
        }
        void HazirUygula(string[] h)
        {
            var gr = Ayar.gear;
            if (Kozmetik.Sahip(gr, Kozmetik.TIP, h[1])) Ayar.KOZ[Kozmetik.TIP] = h[1];
            if (Kozmetik.Sahip(gr, Kozmetik.SAC, h[2])) Ayar.KOZ[Kozmetik.SAC] = h[2];
            Ayar.KOZ[Kozmetik.SAPKA] = Kozmetik.Varsayilan(Kozmetik.SAPKA); Ayar.KOZ[Kozmetik.YUZ] = Kozmetik.Varsayilan(Kozmetik.YUZ);
            Ayar.GORUNUM["sac"] = h[3]; Ayar.GORUNUM["ten"] = h[4]; Ayar.GORUNUM["ayakkabi"] = h[8];
            if (gr.RenkAcik(h[5], Ayar.sampiyon)) gr.renk = h[5];
            Ayar.RGOR["grip"] = h[6]; Ayar.RGOR["tel"] = h[7];
            Ayar.GearHesap(); Ayar.Kaydet(); root.vitrin.Yenile(); kirli[(int)Sekme.Profil] = true;
        }
        void OyuncuSecKur()
        {
            var c = AltSayfaKur(L.T("Oyuncunu seç"));
            c.Add(Yazi(L.T("Birini seç, üstte hemen gör. Her şeyi sonra Dolap'ta değiştirebilirsin."), 13, "etiket"));
            var iz = Satir("izgara"); iz.AddToClassList("hazir-izgara"); c.Add(iz);
            for (int i = 0; i < HAZIR.Length; i++)
            {
                int ii = i; var h = HAZIR[i]; bool on = oyuncuSecSecili == i;
                var k = Kart("hazir-kart"); k.AddToClassList("dugme"); if (on) k.AddToClassList("secili");
                var ust = Satir("rakip-satir"); ust.pickingMode = PickingMode.Ignore; k.Add(ust);
                if (on) { var ik = Ikon(Ikonlar.Tik, 14, TURUNCU); ik.style.marginRight = 4; ust.Add(ik); }
                ust.Add(Yazi(L.T(h[0]), 15, "kalin"));
                // Üç renk noktası (forma, ten, saç) + yazıyla beden/saç: kart yalnız renkle ayırt edilmez.
                var nk = Satir("rakip-satir"); nk.pickingMode = PickingMode.Ignore; k.Add(nk);
                GearModel fm; if (Gear.RENK.TryGetValue(h[5], out fm) && fm.renk != null) nk.Add(Nokta(fm.renk));
                var tn = Karakter.Oge("ten", h[4]); if (tn != null && tn.renk != null) nk.Add(Nokta(tn.renk));
                var sc = Karakter.Oge("sac", h[3]); if (sc != null && sc.renk != null) nk.Add(Nokta(sc.renk));
                string alt = L.T(Kozmetik.Oge(Kozmetik.TIP, h[1]).ad) + " · " + L.T(Kozmetik.Oge(Kozmetik.SAC, h[2]).ad);
                k.Add(Yazi(alt, 12, "ipucu"));
                k.AddManipulator(new Clickable(() => { oyuncuSecSecili = ii; HazirUygula(HAZIR[ii]); Tik(); OyuncuSecKur(); }));
                Erisim.Etiket(k, L.T(h[0]) + ", " + alt, "dugme", on); iz.Add(k);
            }
            c.Add(Dugme(L.T("Rastgele"), () => { oyuncuSecSecili = Random.Range(0, HAZIR.Length); HazirUygula(HAZIR[oyuncuSecSecili]); Tik(); OyuncuSecKur(); }, null, L.T("yalnız açık parçalardan"), 15));
            c.Add(Dugme(L.T("Atla"), OyuncuSecAtla, "kucuk-dugme", null, 14));
            AltEylem(Dugme(L.T("Bununla oyna"), () => { root.sfx.Yukselt(); Yerel.Hap("tick"); OyuncuSecKapat(); }, "dugme--birincil", null, 19));
        }
        // EkranTuru için: bir kartı seç / Atla.
        public void OyuncuSecHazir(int i) { oyuncuSecSecili = i; HazirUygula(HAZIR[i]); OyuncuSecKur(); }
        public void OyuncuSecAtla() { if (oyuncuSecOnce != null) KombinGiy(oyuncuSecOnce); OyuncuSecKapat(); }
        VisualElement Nokta(string hex) { var n = new VisualElement(); n.AddToClassList("renk-nokta"); n.style.backgroundColor = Kit.Hex(hex); n.pickingMode = PickingMode.Ignore; return n; }
        void OyuncuSecKapat() { var once = gez.sekme; gez.AltKapat(); gez.SekmeSec(Sekme.Oyna); Uygula(once, true); Kirlet(); }
    }
}
