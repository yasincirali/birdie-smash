// Başarımlar ve Game Center (HTML basarimVer/gcGiris/gcDurum, yasin 2026-10-22; Unity'ye 2026-10-25 eşitlik turunda).
// Sıralama tablosu YOK: oyun tek kişilik, başarım kıyas değil ilerleme gösterir. 6 kariyer basamağı + 5 beceri; kimlikler
// App Store Connect'tekiyle (HTML BASARIM) birebir. Kazanılan kimlik önce yerelde tutulur (tuytop_basarim: {id: zaman ms},
// HTML ile aynı biçim; devirle HTML'den gelen liste de buradan okunur), sonra Game Center'a gider.
// Giriş isteği ilk açılışta değil, ilk kariyer basamağı geçilince gelir (oyuncu oyunu sevmeden izin ekranı görmesin);
// açılışta kariyer ≥ 1 ise istenir. Giriş sonradan olsa da yerel liste yeniden gönderilir, kayıp olmaz.
// Unity'nin Social API'si iOS'ta GameCenterPlatform'dur (GameKit); Editor'de ve iOS dışında hiçbir şey çağrılmaz.
// Game Center yetkisi (com.apple.developer.game-center) fastlane beta_unity'de Xcode projesine eklenir.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public static class Basarim
    {
        public const string ANTRENMAN = "bs_antrenman", AKIS = "bs_akis", RALLI20 = "bs_ralli_20", SICRAMA = "bs_sicrayan_smac", GOREV3 = "bs_uc_gorev";
        // 2026-10-26 karakter (tasarım §5.5): tur finalleri bs_tur_2..4 (Tur 1 = bs_kariyer_6) ve Sv 30 "Efsane".
        public const string EFSANE = "bs_efsane";
        static Dictionary<string, object> yerel; static bool istendi;
        public static bool acik;   // Game Center girişi tamam: Ayarlar'da "Başarımlar" görünür

        static void Yukle()
        {
            if (yerel != null) return; yerel = new Dictionary<string, object>();
            string s = PlayerPrefs.HasKey("tuytop_basarim") ? PlayerPrefs.GetString("tuytop_basarim", null) : null;
            var o = Json.Obj(Json.Parse(s)); if (o != null) foreach (var kv in o) yerel[kv.Key] = kv.Value;
        }
        public static bool Var(string id) { Yukle(); return yerel.ContainsKey(id); }
        public static int Sayi { get { Yukle(); return yerel.Count; } }

        public static void Ver(string id)
        {
            if (string.IsNullOrEmpty(id)) return; Yukle(); if (yerel.ContainsKey(id)) return;
            yerel[id] = (double)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PlayerPrefs.SetString("tuytop_basarim", Json.Write(yerel)); PlayerPrefs.Save();
            Gonder(id);
        }
        // HTML basarimEsitle: kariyer basamakları ve 20 ralli rekoru kayıttan (eski oyuncu da başarımını alsın)
        public static void Esitle(int kariyer, int bestRally)
        {
            for (int i = 1; i <= kariyer && i <= Match.KARIYER.Length; i++) Ver("bs_kariyer_" + i);
            if (bestRally >= 20) Ver(RALLI20);
        }
        public static void Giris(int kariyer)
        {
            if (istendi || kariyer < 1) return; istendi = true;
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                Social.localUser.Authenticate((bool ok) =>
                {
                    acik = ok; if (!ok) return; Yukle();
                    foreach (var id in new List<string>(yerel.Keys)) Gonder(id);
                });
            }
            catch (System.Exception) { acik = false; }
#endif
        }
        static void Gonder(string id)
        {
            if (!acik) return;
#if UNITY_IOS && !UNITY_EDITOR
            try { Social.ReportProgress(id, 100.0, (bool ok) => { }); } catch (System.Exception) { }
#endif
        }
        public static void Goster()
        {
            if (!acik) return;
#if UNITY_IOS && !UNITY_EDITOR
            try { Social.ShowAchievementsUI(); } catch (System.Exception) { }
#endif
        }
    }
}
