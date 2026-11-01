// Bulut kaydı (2026-11-01, yasin: "paywall ile tüy aldırma ya da IAP altyapısı düşünmeliyiz"). Tüy paketi tüketilen
// (consumable) üründür: Apple onu geri yüklemez. Kayıt yalnız telefonda kalırsa uygulamayı silen oyuncu parayla aldığı
// Tüy'ü kaybeder. Bu yüzden ilerleme anahtarları iCloud anahtar-değer deposuna tek bir anlık görüntü olarak yansıtılır
// (Game/Bulut.cs + Plugins/iOS/BirdieBulut.mm). Bu dosya saf karar mantığıdır; BotMac'te sınanır.
//
// Ne gider: yalnız ilerleme ve sahiplik (ekonomi, ekipman, ilerleme, kariyer, görünüm, kombinler, rekorlar). Cihaz
// tercihleri (ses, müzik, hareketi azalt, yazı boyu, kamera, dil, FPS) gitmez: her cihazın kendi ayarıdır.
//
// Karar kuralı (geri ödeme açığı olmadan): ilerleme yalnız artar, o yüzden ölçü XP'dir.
//   · Yerelde ekonomi kaydı yoksa (yeni kurulum ya da silinip yeniden kurulmuş) ve bulutta görüntü varsa → buluttan yükle.
//   · Bulutun XP'si yereldekinden büyükse (başka cihazda oynandı) → buluttan yükle.
//   · Yerel kazanır ama bulutta yerelin bilmediği satın alma işlemleri varsa (başka cihazda alındı) → Birlestir: o işlemlerin
//     Tüy'ü yerel cüzdana eklenir, defterler birleşir; yerel ilerlemenin geri kalanı korunur.
//   · Diğer her durumda yerel kazanır ve buluta yazılır. Tüy'e bakılmaz: aynı XP'de bulutta daha çok Tüy olması,
//     oyuncunun yerelde harcadığı anlamına gelir; buluttan yüklemek harcamayı geri verirdi.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public static class BulutKayit
    {
        public const string BULUT_ANAHTAR = "kayit";
        public static readonly string[] METIN = { "tuytop_v2", "tuytop_gear", "tuytop_ekonomi", "tuytop_ilerleme", "tuytop_raket_gor", "tuytop_gorunum",
            "tuytop_antrenman", "tuytop_basarim", "tuytop_kombin", "tuytop_kombin_aktif", "tuytop_oyuncu_sec", "tuytop_model", "tuytop_len", "tuytop_mod",
            "tuytop_saha", "tuytop_puan", "tuytop_html_devir", TuyDukkan.ANAHTAR };
        public static readonly string[] SAYI = { "tuytop_usta_kart", "tuytop_rally", "tuytop_wins", "tuytop_kariyer", "tuytop_target" };

        public enum Karar { Yaz, Yukle, Bos, Birlestir }

        // Yerel anahtar okuyucuları: metin (yoksa null) ve sayı (yoksa null).
        public static string Goruntu(Func<string, string> metin, Func<string, int?> sayi)
        {
            var m = new Dictionary<string, object>(); var s = new Dictionary<string, object>();
            foreach (var k in METIN) { var v = metin(k); if (v != null) m[k] = v; }
            foreach (var k in SAYI) { var v = sayi(k); if (v.HasValue) s[k] = (double)v.Value; }
            if (m.Count == 0 && s.Count == 0) return null;
            return Json.Write(new Dictionary<string, object> { { "s", 1.0 }, { "xp", (double)Xp(metin("tuytop_ekonomi")) }, { "al", (double)TuyDukkan.Toplam(metin(TuyDukkan.ANAHTAR)) }, { "m", m }, { "i", s } });
        }

        public static int Xp(string ekonomi) { var e = Json.Obj(Json.Parse(ekonomi)); return e == null ? -1 : (int)Json.Num(e, "xp", 0); }

        public static Karar Karsilastir(string yerelEkonomi, string bulut, string yerelDefter = null)
        {
            var b = Json.Obj(Json.Parse(bulut)); if (b == null || Json.Obj(b.ContainsKey("m") ? b["m"] : null) == null) return yerelEkonomi == null ? Karar.Bos : Karar.Yaz;
            int bx = (int)Json.Num(b, "xp", -1), yx = Xp(yerelEkonomi);
            if (yerelEkonomi == null || yx < 0) return Karar.Yukle;
            if (bx > yx) return Karar.Yukle;
            return Eksik(yerelDefter, bulut).Count > 0 ? Karar.Birlestir : Karar.Yaz;
        }

        static TuyDukkan.Defter BulutDefter(string bulut)
        {
            var b = Json.Obj(Json.Parse(bulut)); var m = b != null ? Json.Obj(b.ContainsKey("m") ? b["m"] : null) : null;
            return TuyDukkan.Defter.Oku(m != null ? Json.Str(m, TuyDukkan.ANAHTAR, null) : null);
        }
        // Bulutta olup yerel defterde olmayan işlem kimlikleri.
        public static List<string> Eksik(string yerelDefter, string bulut)
        {
            var y = TuyDukkan.Defter.Oku(yerelDefter); var l = new List<string>();
            foreach (var x in BulutDefter(bulut).islem) if (!y.islem.Contains(x)) l.Add(x);
            return l;
        }
        // Birlestir: eksik işlemlerin Tüy'ü (bulut toplamı − yerel toplam, en az 0) cüzdana eklenir; dönüş {yeni ekonomi, yeni defter}.
        public static string[] SatinBirlestir(string yerelEko, string yerelDefter, string bulut)
        {
            var y = TuyDukkan.Defter.Oku(yerelDefter); var bd = BulutDefter(bulut);
            int fark = System.Math.Max(0, bd.toplam - y.toplam);
            foreach (var x in bd.islem) y.islem.Add(x); y.toplam = System.Math.Max(y.toplam, bd.toplam);
            var e = Json.Obj(Json.Parse(yerelEko)) ?? new Dictionary<string, object>();
            e["tuy"] = Json.Num(e, "tuy", 0) + fark;
            return new[] { Json.Write(e), y.Yaz() };
        }

        // Görüntüyü yerele açar: yazıcılar anahtar başına çağrılır. Görüntüde olmayan ilerleme anahtarı silinmez (eski sürümün
        // bilmediği yeni anahtar yerelde kalır).
        public static int Ac(string bulut, Action<string, string> metinYaz, Action<string, int> sayiYaz)
        {
            var b = Json.Obj(Json.Parse(bulut)); if (b == null) return 0; int n = 0;
            var m = Json.Obj(b.ContainsKey("m") ? b["m"] : null); var s = Json.Obj(b.ContainsKey("i") ? b["i"] : null);
            if (m != null) foreach (var k in METIN) { var v = Json.Str(m, k, null); if (v != null) { metinYaz(k, v); n++; } }
            if (s != null) foreach (var k in SAYI) { object v; if (s.TryGetValue(k, out v) && v is double) { sayiYaz(k, (int)(double)v); n++; } }
            return n;
        }
    }
}
