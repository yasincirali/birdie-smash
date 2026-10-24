// Kalıcı ayarlar ve ilerleme: HTML'nin localStorage anahtarlarıyla aynı adlar (tuytop_*), PlayerPrefs'te.
// Neden aynı adlar: ileride WebView kabuğundan Unity'ye geçilirse taşıma tek tabloyla yapılsın.
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class Ayar
    {
        public static bool easy = true, reduce, ses = true, kariyerModu, antrenman;
        public static int target = 21, kariyer, bestRally, wins;
        public static string saha = "salon", model = "oyuncak";
        public const string FORMA = "#4fb8ff";   // HTML varsayılan forma "Mavi" (ekipman ekranı demo'da yok)

        public static void Yukle()
        {
            easy = PlayerPrefs.GetInt("tuytop_easy", 1) == 1; target = PlayerPrefs.GetInt("tuytop_target", 21);
            if (target != 5 && target != 11 && target != 21) target = 21;
            saha = PlayerPrefs.GetString("tuytop_saha", "salon"); if (!CourtView.SAHA.ContainsKey(saha)) saha = "salon";
            model = PlayerPrefs.GetString("tuytop_model", "oyuncak"); if (model == "sportif") model = "rocketbox"; if (model != "oyuncak" && model != "rocketbox") model = "oyuncak";
            reduce = PlayerPrefs.GetInt("tuytop_reduce", 0) == 1; ses = PlayerPrefs.GetInt("tuytop_ses", 1) == 1;
            kariyerModu = PlayerPrefs.GetString("tuytop_mod", "s") == "k"; antrenman = PlayerPrefs.GetString("tuytop_antrenman", "0") == "1";
            kariyer = PlayerPrefs.GetInt("tuytop_kariyer", 0); bestRally = PlayerPrefs.GetInt("tuytop_rally", 0); wins = PlayerPrefs.GetInt("tuytop_wins", 0);
        }
        public static void Kaydet()
        {
            PlayerPrefs.SetInt("tuytop_easy", easy ? 1 : 0); PlayerPrefs.SetInt("tuytop_target", target); PlayerPrefs.SetString("tuytop_saha", saha);
            PlayerPrefs.SetString("tuytop_model", model); PlayerPrefs.SetInt("tuytop_reduce", reduce ? 1 : 0); PlayerPrefs.SetInt("tuytop_ses", ses ? 1 : 0);
            PlayerPrefs.SetString("tuytop_mod", kariyerModu ? "k" : "s"); PlayerPrefs.SetString("tuytop_antrenman", antrenman ? "1" : "0");
            PlayerPrefs.SetInt("tuytop_kariyer", kariyer); PlayerPrefs.SetInt("tuytop_rally", bestRally); PlayerPrefs.SetInt("tuytop_wins", wins);
            PlayerPrefs.Save();
        }
    }
}
