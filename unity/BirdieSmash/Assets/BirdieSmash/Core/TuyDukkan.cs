// Tüy dükkânı (2026-11-01, yasin: "paywall ile tüy aldırma ya da IAP altyapısı düşünmeliyiz, sınırımız olmayacak"; karar
// kartı: önce iCloud). Gerçek parayla yalnız Tüy satılır (tüketilen ürün). Rastgele sandık, süreli teklif, geri sayım yok;
// fiyat hep mağazanın yerel fiyatıyla gösterilir (Game/Magaza.cs, StoreKit). Bu dosya saf mantıktır, BotMac'te sınanır.
//
// Defter (tuytop_satin, buluta da gider): işlenmiş işlem kimlikleri ve toplam satın alınan Tüy. StoreKit bir işlemi
// uygulama kapanıp açılınca yeniden teslim edebilir; aynı kimlik ikinci kez Tüy vermez. "toplam" bulut kararında kullanılır:
// başka cihazda satın alınmış Tüy, XP aynı olsa da kaybolmasın (Core/BulutKayit).
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class TuyPaketi { public string id, ad; public int tuy, ek; }   // ek: tabana göre bonus yüzdesi (kartta "+%17")

    public static class TuyDukkan
    {
        public const string ANAHTAR = "tuytop_satin";
        // Ürün kimlikleri App Store Connect'te birebir aynı yazılır (YAPMAN_GEREKENLER.md). Sıra = dükkândaki sıra.
        public static readonly TuyPaketi[] PAKETLER =
        {
            new TuyPaketi { id = "com.birdiesmash.app.tuy1000", ad = "Bir avuç Tüy", tuy = 1000, ek = 0 },
            new TuyPaketi { id = "com.birdiesmash.app.tuy3500", ad = "Tüy kesesi", tuy = 3500, ek = 17 },
            new TuyPaketi { id = "com.birdiesmash.app.tuy8000", ad = "Tüy sandığı", tuy = 8000, ek = 35 },
            new TuyPaketi { id = "com.birdiesmash.app.tuy20000", ad = "Tüy hazinesi", tuy = 20000, ek = 68 },
        };
        public static TuyPaketi Bul(string id) { foreach (var p in PAKETLER) if (p.id == id) return p; return null; }

        public class Defter
        {
            public readonly HashSet<string> islem = new HashSet<string>(); public int toplam;
            public static Defter Oku(string s)
            {
                var d = new Defter(); var o = Json.Obj(Json.Parse(s)); if (o == null) return d;
                d.toplam = (int)Json.Num(o, "toplam", 0); object l;
                if (o.TryGetValue("islem", out l) && l is List<object>) foreach (var x in (List<object>)l) if (x is string) d.islem.Add((string)x);
                return d;
            }
            public string Yaz()
            {
                var l = new List<object>(); foreach (var x in islem) l.Add(x); l.Sort((p, q) => string.CompareOrdinal((string)p, (string)q));
                return Json.Write(new Dictionary<string, object> { { "toplam", (double)toplam }, { "islem", l } });
            }
        }

        // Satın alınan paketi teslim eder: Tüy cüzdana, işlem deftere. Dönüş: verilen Tüy (0 = bilinmeyen ürün ya da tekrar).
        // Kayıt: cüzdan EkoYaz ile, defter kayit'a; çağıran ardından buluta yazar.
        public static int Teslim(Gear g, IKayit k, string urun, string islemKimligi)
        {
            var p = Bul(urun); if (p == null || g == null) return 0;
            var d = Defter.Oku(k.Get(ANAHTAR)); string kim = string.IsNullOrEmpty(islemKimligi) ? null : islemKimligi;
            if (kim != null && d.islem.Contains(kim)) return 0;
            g.eko.tuy += p.tuy; g.EkoYaz();
            if (kim != null) d.islem.Add(kim); d.toplam += p.tuy; k.Set(ANAHTAR, d.Yaz());
            return p.tuy;
        }
        public static int Toplam(string defter) { return Defter.Oku(defter).toplam; }
    }
}
