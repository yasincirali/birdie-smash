// Yerel (iOS) hizmetler: Assets/Plugins/iOS/BirdieYerel.mm. Editor'de ve iOS dışında sessiz geçer (Android hattı yok;
// Android'de uzun desen tek Handheld.Vibrate'e düşer).
// Titreşim (HTML HAP + hap() + hapHit, 2026-10-25 eşitlik turu): desenler HTML'deki sayıların birebir kopyası; kabuk
// her darbeyi süresine göre heavy/medium/light/selection'a çeviriyordu, .mm aynısını yapar. Hareketi azalt açıkken
// yalnız sayı sonucu (win/lose) titrer, vuruş titremez (HTML ile aynı). Rakibin vuruşu titremez (o raket senin elinde
// değil); yalnız rakibin smacında kısa bir tık.
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BirdieSmash.Game
{
    public static class Yerel
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BirdieTitresim(int[] desen, int n);
        [DllImport("__Internal")] static extern float BirdieYaziOlcek();
        [DllImport("__Internal")] static extern int BirdieHareketAzalt();
        [DllImport("__Internal")] static extern int BirdieDusukGuc();
        [DllImport("__Internal")] static extern int BirdieDigerSesCaliyor();
#endif
        static readonly Dictionary<string, int[]> HAP = new Dictionary<string, int[]> {
            {"tick",new[]{8}},{"hit",new[]{14}},{"perfect",new[]{16,10,28}},{"smash",new[]{35,15,55}},{"jump",new[]{45,20,30,20,80}},
            {"kill",new[]{30,10,50}},{"net",new[]{20,30,20}},{"win",new[]{20,40,20,40,90}},{"lose",new[]{60}},
            {"flow",new[]{10,30,10,30,10,30,70}},{"hawk",new[]{6,40,6,40,6}},{"poor",new[]{8,20,8}}};

        // HTML hap(k): hareketi azaltta yalnız win/lose
        public static void Hap(string k)
        {
            int[] p; if (!HAP.TryGetValue(k, out p)) return;
            if (Ayar.reduce && k != "win" && k != "lose") return; Cal(p);
        }
        // HTML hapHit(o): benim vuruşum; süre türe, doku tel gerginliğine göre (Sfx.Desen)
        public static void Vurus(string type, string q, bool slice)
        {
            if (Ayar.reduce || Ayar.gear == null) return; Cal(Sfx.Desen(type, q, slice, Ayar.gear.tel));
        }
        static void Cal(int[] p)
        {
            if (p == null || p.Length == 0) return;
#if UNITY_IOS && !UNITY_EDITOR
            try { BirdieTitresim(p, p.Length); } catch { }
#elif UNITY_ANDROID && !UNITY_EDITOR
            int top = 0; for (int i = 0; i < p.Length; i += 2) top += p[i]; if (top >= 22) Handheld.Vibrate();
#endif
        }
        // Dynamic Type oranı (gövde metni / 17 pt); Editor'de 1.
        public static float YaziOlcek()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { float k = BirdieYaziOlcek(); return k > 0 ? k : 1; } catch { return 1; }
#else
            return 1;
#endif
        }
        // Başka uygulama ses çalıyor mu (menü müziği başlamasın; Sfx.MuzikIsle). Editor'de ve iOS dışında hayır.
        public static bool DigerSesCaliyor()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { return BirdieDigerSesCaliyor() == 1; } catch { return false; }
#else
            return false;
#endif
        }
        public static bool HareketAzalt()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { return BirdieHareketAzalt() == 1; } catch { return false; }
#else
            return false;
#endif
        }
        // iOS düşük güç modu; iOS dışında hep kapalı.
        public static bool DusukGuc()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { return BirdieDusukGuc() == 1; } catch { return false; }
#else
            return false;
#endif
        }
    }
}
