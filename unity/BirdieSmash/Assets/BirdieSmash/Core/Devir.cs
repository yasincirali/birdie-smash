// HTML → Unity ilerleme devri (Ar-Ge 25 Eki #1, yasin onayı 25 Eki). Son HTML build'i bütün tuytop_* localStorage
// değerlerini kabuğa yollar; kabuk (ios/Runner/AppDelegate.swift) bunları aynı adla NSUserDefaults.standard'a string
// olarak yazar. Unity PlayerPrefs iOS'ta aynı depodur (anahtar adı aynen, GetString NSString okur), yani Ayar.Yukle
// değerleri zaten HTML biçiminde bulur (bu port kaydı HTML biçiminde tutar). Burada yapılan:
//   1. Devir yalnız bir kez: bayrak (tuytop_html_devir) yoksa kabul edilir, sonra bayrak her durumda konur (HTML verisi
//      olmasa da) ki eski bir HTML build'i açılırsa kabuk Unity'deki yeni ilerlemenin üstüne yazmasın.
//   2. HTML'den gelen tuytop_v2 normalize edilir: bilinen alanlar, sayılar ≥ 0 tam sayı, kariyer basamak sayısıyla
//      sınırlı, şampiyon kariyerle tutarlı. JSON nesnesi değilse "{}" olur (bozuk kayıt oyunu açılmaz yapmasın;
//      Yukle o zaman ilk tur Unity int anahtarlarına da düşmez, cihazda o anahtarlar hiç olmadı).
//   3. tuytop_puan (mağaza puanı sayacı) 0..3 tam sayıya iner; tuytop_gear / tuytop_ekonomi / tuytop_raket_gor
//      Gear ve Ayar tarafından alan alan doğrulanır, burada yalnız JSON nesnesi olup olmadığına bakılır.
// Saf C#: tool/unity_test/BotMac.cs bellek kaydıyla sınar.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public static class Devir
    {
        public const string BAYRAK = "tuytop_html_devir";
        // HTML localStorage anahtarları (index.html): devralınanların sayımı için. Bilinmeyen tuytop_* de kayıtta kalır.
        public static readonly string[] HTML_ANAHTAR = { "tuytop_v2", "tuytop_len", "tuytop_mod", "tuytop_saha", "tuytop_model", "tuytop_gear", "tuytop_ekonomi",
            "tuytop_raket_gor", "tuytop_antrenman", "tuytop_basarim", "tuytop_fps", "tuytop_puan" };
        static readonly string[] JSON_NESNE = { "tuytop_gear", "tuytop_ekonomi", "tuytop_raket_gor", "tuytop_basarim" };

        // Dönüş: devralınan HTML anahtarı sayısı; -1 = devir daha önce yapılmış (hiçbir şeye dokunulmaz).
        public static int Kabul(IKayit k)
        {
            if (k.Get(BAYRAK) != null) return -1;
            int n = 0; foreach (var a in HTML_ANAHTAR) if (k.Get(a) != null) n++;
            string v2 = k.Get("tuytop_v2"); if (v2 != null) k.Set("tuytop_v2", V2(v2));
            string p = k.Get("tuytop_puan"); if (p != null) { int c; k.Set("tuytop_puan", Math.Max(0, Math.Min(3, int.TryParse(p, out c) ? c : 3)).ToString()); }   // okunamazsa bir daha sorma
            foreach (var a in JSON_NESNE) { string s = k.Get(a); if (s != null && Json.Obj(Json.Parse(s)) == null) k.Set(a, "{}"); }
            k.Set(BAYRAK, "1");
            return n;
        }

        // HTML best → Unity tuytop_v2 (Ayar.Kaydet ile aynı alanlar).
        public static string V2(string s)
        {
            var b = Json.Obj(Json.Parse(s)); if (b == null) return "{}";
            int kar = Math.Min(Match.KARIYER.Length, Tam(b, "kariyer"));
            return Json.Write(new Dictionary<string, object> { { "rally", (double)Tam(b, "rally") }, { "wins", (double)Tam(b, "wins") }, { "kariyer", (double)kar },
                { "gorev", (double)Tam(b, "gorev") }, { "sampiyon", Json.Bool(b, "sampiyon", false) || kar >= Match.KARIYER.Length } });
        }
        static int Tam(Dictionary<string, object> b, string a) { double v = Json.Num(b, a, 0); return double.IsNaN(v) || v < 0 ? 0 : v > 1e6 ? 1000000 : (int)Math.Floor(v); }
    }
}
