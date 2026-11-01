// Bulut kaydı (iCloud anahtar-değer deposu): açılışta Ayar.Yukle'den önce Esitle(), arka plana geçişte ve maç sonunda Yaz().
// Karar Core/BulutKayit.cs'te (XP büyük olan kazanır, yeni kurulum buluttan yüklenir). iOS dışında ve Editor'de sessiz geçer.
using UnityEngine;
using BirdieSmash.Core;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BirdieSmash.Game
{
    public static class Bulut
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int BirdieBulutAcik();
        [DllImport("__Internal")] static extern string BirdieBulutOku(string anahtar);
        [DllImport("__Internal")] static extern void BirdieBulutYaz(string anahtar, string deger);
        static bool Var { get { try { return BirdieBulutAcik() == 1; } catch { return false; } } }
        static string Oku() { try { return BirdieBulutOku(BulutKayit.BULUT_ANAHTAR); } catch { return null; } }
        static void Gonder(string s) { try { BirdieBulutYaz(BulutKayit.BULUT_ANAHTAR, s); } catch { } }
#else
        static bool Var { get { return false; } }
        static string Oku() { return null; }
        static void Gonder(string s) { }
#endif
        // Ayarlar'da "iCloud'a yedekleniyor" satırı için: yetki ve hesap var mı.
        public static bool Acik { get { return Var; } }
        public static BulutKayit.Karar SonKarar { get; private set; }
        static string sonYazilan;

        static string Metin(string k) { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k, null) : null; }
        static int? Sayi(string k) { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k, 0) : (int?)null; }

        public static void Esitle()
        {
            if (!Var) return;
            string b = Oku(); SonKarar = BulutKayit.Karsilastir(Metin("tuytop_ekonomi"), b);
            if (SonKarar == BulutKayit.Karar.Yukle) { BulutKayit.Ac(b, (k, v) => PlayerPrefs.SetString(k, v), (k, v) => PlayerPrefs.SetInt(k, v)); PlayerPrefs.Save(); sonYazilan = b; }
            else if (SonKarar == BulutKayit.Karar.Yaz) Yaz();
        }

        public static void Yaz()
        {
            if (!Var) return;
            string g = BulutKayit.Goruntu(Metin, Sayi); if (g == null || g == sonYazilan) return;
            Gonder(g); sonYazilan = g;
        }
    }
}
