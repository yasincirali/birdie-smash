// Kalıcı ayarlar ve ilerleme. Anahtarlar ve değer biçimi HTML localStorage ile aynı (PlayerPrefs'te string):
//   tuytop_v2       JSON {rally, wins, kariyer, gorev, sampiyon}   (HTML best)
//   tuytop_len      'len5' | 'len11' | 'len21' (varsayılan len5)   tuytop_mod 'k' | 's'   tuytop_saha   tuytop_model
//   tuytop_gear / tuytop_ekonomi (Core/Gear)   tuytop_raket_gor JSON {grip, tel, tel2, desen}   tuytop_antrenman '1'
//   tuytop_puan     mağaza puanı istenme sayısı (0..3, HTML puanBak ile ortak)
// HTML → Unity devri (Core/Devir): ilk açılışta kabuğun NSUserDefaults'a yazdığı HTML değerleri bir kez kabul edilir,
// sonra tuytop_html_devir bayrağı konur. Bayraktan sonra kabuk (eski HTML build'i) bir daha yazmaz.
// HTML'de karşılığı olmayan Unity anahtarları: tuytop_easy, tuytop_reduce (HTML sistem ayarını okur), tuytop_ses, tuytop_dil.
// Önceki Unity demosu (2026-10-24 ilk tur) tuytop_target / tuytop_rally / tuytop_wins / tuytop_kariyer'i ayrı int yazıyordu:
// ilk açılışta bunlar okunup yeni biçime taşınır, eski anahtar silinmez (geri dönüşte kayıt kaybolmasın).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class PrefsKayit : IKayit
    {
        public string Get(string k) { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k, null) : null; }
        public void Set(string k, string v) { PlayerPrefs.SetString(k, v); PlayerPrefs.Save(); }
    }

    public static class Ayar
    {
        public static bool easy = true, reduce, ses = true, kariyerModu, antrenman, sampiyon;
        public static int target = 5, kariyer, bestRally, wins, gorev, puan; public static int devirSayisi = -1;
        public static string saha = "salon", model = "oyuncak", dil = "tr";
        public static Gear gear; public static GearStats GS = GearStats.Notr;
        public static readonly Dictionary<string, string> RGOR = new Dictionary<string, string> { { "grip", "siyah" }, { "tel", "beyaz" }, { "tel2", "kirmizi" }, { "desen", "tek" } };
        public static string FORMA { get { return GS.forma; } }
        static readonly PrefsKayit kayit = new PrefsKayit();

        static int EskiInt(string k, int d) { try { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k, d) : d; } catch { return d; } }
        static string Str(string k, string d) { try { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k, d) : d; } catch { return d; } }

        public static void Yukle()
        {
            devirSayisi = Devir.Kabul(kayit);   // Kaydet'ten önce: HTML değeri Unity varsayılanıyla ezilmesin
            var b = Json.Obj(Json.Parse(Str("tuytop_v2", null)));
            if (b != null) { bestRally = (int)Json.Num(b, "rally", 0); wins = (int)Json.Num(b, "wins", 0); kariyer = (int)Json.Num(b, "kariyer", 0); gorev = (int)Json.Num(b, "gorev", 0); sampiyon = Json.Bool(b, "sampiyon", false); }
            else { bestRally = EskiInt("tuytop_rally", 0); wins = EskiInt("tuytop_wins", 0); kariyer = EskiInt("tuytop_kariyer", 0); sampiyon = kariyer >= Match.KARIYER.Length; }   // ilk tur Unity kaydını taşı
            string len = Str("tuytop_len", null);
            if (len == "len5" || len == "len11" || len == "len21") target = int.Parse(len.Substring(3));
            else { int t = EskiInt("tuytop_target", 5); target = t == 11 || t == 21 ? t : 5; }   // bilinmeyen (eski len7 dahil) 5 olur
            easy = EskiInt("tuytop_easy", 1) == 1;
            saha = Str("tuytop_saha", "salon"); if (!CourtView.SAHA.ContainsKey(saha)) saha = "salon";
            model = Str("tuytop_model", "oyuncak"); if (model == "sportif" || model == "aday") model = "rocketbox"; if (model != "oyuncak" && model != "rocketbox") model = "oyuncak";
            reduce = EskiInt("tuytop_reduce", 0) == 1; ses = EskiInt("tuytop_ses", 1) == 1;
            int pu; puan = int.TryParse(Str("tuytop_puan", "0"), out pu) ? Mathf.Clamp(pu, 0, Ozet.PUAN_AZAMI) : Ozet.PUAN_AZAMI;
            kariyerModu = Str("tuytop_mod", "s") == "k"; antrenman = Str("tuytop_antrenman", "0") == "1";
            // Dil: HTML navigator.language 'tr' ile başlıyorsa Türkçe, değilse İngilizce. Unity'de sistem dili; ayarda elle seçilirse tuytop_dil.
            dil = Str("tuytop_dil", Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en"); if (dil != "tr" && dil != "en") dil = "tr";
            var rg = Json.Obj(Json.Parse(Str("tuytop_raket_gor", null)));
            if (rg != null) foreach (var k in new List<string>(RGOR.Keys)) { string v = Json.Str(rg, k, null); if (v != null && (k == "desen" ? v == "tek" || v == "capraz" : Vitrin.RAK_RENK.ContainsKey(v))) RGOR[k] = v; }
            gear = new Gear(kayit, wins); gear.kariyerModu = kariyerModu;
            if (gear.forma == "sampiyon" && !sampiyon) gear.forma = "mavi";
            GearHesap();
            Kaydet();
        }
        public static void GearHesap() { gear.kariyerModu = kariyerModu; GS = gear.Hesap(); }
        public static void Kaydet()
        {
            kayit.Set("tuytop_v2", Json.Write(new Dictionary<string, object> { { "rally", (double)bestRally }, { "wins", (double)wins }, { "kariyer", (double)kariyer }, { "gorev", (double)gorev }, { "sampiyon", sampiyon } }));
            PlayerPrefs.SetString("tuytop_len", "len" + target); PlayerPrefs.SetString("tuytop_saha", saha); PlayerPrefs.SetString("tuytop_model", model);
            PlayerPrefs.SetString("tuytop_mod", kariyerModu ? "k" : "s"); if (antrenman) PlayerPrefs.SetString("tuytop_antrenman", "1");
            PlayerPrefs.SetInt("tuytop_easy", easy ? 1 : 0); PlayerPrefs.SetInt("tuytop_reduce", reduce ? 1 : 0); PlayerPrefs.SetInt("tuytop_ses", ses ? 1 : 0);
            PlayerPrefs.SetString("tuytop_dil", dil); PlayerPrefs.SetString("tuytop_puan", puan.ToString());
            var rg = new Dictionary<string, object>(); foreach (var kv in RGOR) rg[kv.Key] = kv.Value; PlayerPrefs.SetString("tuytop_raket_gor", Json.Write(rg));
            if (gear != null) { gear.GearYaz(); gear.EkoYaz(); }
            PlayerPrefs.Save();
        }
    }
}
