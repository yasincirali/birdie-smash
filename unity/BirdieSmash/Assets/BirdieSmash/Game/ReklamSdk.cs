using System;
using UnityEngine;
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace BirdieSmash.Game
{
    // AdMob bağlantısı (2026-11-07, yasin: "şerit en başta bağlı olarak çıksın"). Google Mobile Ads Unity eklentisi
    // (Packages/manifest.json, OpenUPM). Yer kararları Game/Reklam.cs'de; ödüllü reklam kuralı Core/OdulluReklam.cs'de.
    //  - Sıra: UMP rıza bilgisi (AB/BK'de gerekirse Google'ın rıza formu) → rıza reklama izin veriyorsa MobileAds.Initialize.
    //    Rıza vermeyen ya da form açılamayan oyuncu reklamsız oynar; hiçbir şey kilitlenmez.
    //  - Şerit: 320×50 standart şerit, menüdeki şerit yerinin ortasına native olarak oturur (UiKok her kare yeri verir).
    //    Maçta, perde altında ve alt sayfa açıkken gizli. Yüklenmezse menüde yer açılmaz (2026-11-08, UiKok.ReklamSeritYeri).
    //  - Ödüllü: maç başında yüklenir, maç sonu kartı OdulluHazir'a bakar. Gösterilirken oyun sesi durur (iOS'ta eklenti Unity'yi
    //    duraklatır; Android'de AudioListener). Ödül yalnız SDK "ödül kazanıldı" deyince; yarıda kapanırsa yok.
    //  - Kimlikler: iOS 2026-11-11'den beri yasin'in AdMob hesabı (mağaza 1.0; birimler "Menu banner" ve "Match end reward").
    //    Android hattı yok, orada Google'ın TEST kimlikleri kalır; Android yayını gelince AdMob'da ayrı Android uygulaması açılır.
    //    TestFlight build'leri de gerçek reklam gösterir: geliştirici ve testçi kendi reklamına dokunmaz (geçersiz trafik).
    //  - Eklenti yalnız iOS/Android oyuncusunda çağrılır: Editor'de ve CI'ın Linux oyuncusunda (ekran turu) SDK yok, yer
    //    tutucu görünür; böylece ekran karesi reklamın içeriğine göre değişmez.
    public static class ReklamSdk
    {
#if UNITY_ANDROID
        const string SERIT_ID = "ca-app-pub-3940256099942544/6300978111", ODULLU_ID = "ca-app-pub-3940256099942544/5224354917";
        public const bool TEST_KIMLIK = true;
#else
        const string SERIT_ID = "ca-app-pub-6645967875947629/1662942089", ODULLU_ID = "ca-app-pub-6645967875947629/2951973024";
        public const bool TEST_KIMLIK = false;
#endif
        const float SERIT_W = 320, SERIT_H = 50;

        static bool basladi, hazir;
        public static bool SeritYuklu { get; private set; }
        public static bool Hazir { get { return hazir; } }

        // Menü her karede çağırır (UiKok); bir kez çalışır. 2026-11-08 (inceleme turu): rıza formu ve iOS izin penceresi
        // oyuncu ilk maçını bitirmeden açılmaz. Önceden ilk açılışta, perde kalkarken, tek topa vurmadan "reklam ortaklarımız…"
        // diyaloğu geliyordu: yeni kurulan arcade oyunu en sık o anda silinir, izin de değer görülmeden reddedilir (ödüllü
        // reklam doluluğu düşer). Badminton League / Tennis Clash da ilk maçı rızasız oynatıp ilk menü dönüşünde sorar.
        // UMP şartı "reklam isteğinden önce rıza"dır; o ana kadar SDK hiç başlamadığı için istek de yok, menüde yer de yok.
        // Koşul: antrenman bitti VE en az bir maç (galibiyet ya da bu sürümden beri sayılan maç). Yarıda bırakan ikinci
        // açılışta da antrenmana döner; rıza yine maçtan sonra gelir. Yeni anahtar yok.
        public static bool IlkMacBitti { get { return Ayar.antrenman && (Ayar.wins > 0 || (Ayar.ilerleme != null && Ayar.ilerleme.Mac > 0)); } }
        public static void Baslat()
        {
            if (basladi || !IlkMacBitti) return; basladi = true;   // 2026-11-08: Reklamsız da başlar; ödüllü reklam isteyene açık kalır, şeridi UiKok istemez
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            try
            {
                ConsentInformation.Update(new ConsentRequestParameters(), hata =>
                {
                    if (hata != null) Debug.LogWarning("Reklam rızası güncellenemedi: " + hata.Message);
                    ConsentForm.LoadAndShowConsentFormIfRequired(f =>
                    {
                        if (f != null) Debug.LogWarning("Reklam rıza formu: " + f.Message);
                        if (ConsentInformation.CanRequestAds()) Ilklendir();
                    });
                });
                if (ConsentInformation.CanRequestAds()) Ilklendir();   // önceki oturumda verilmiş rıza: beklemeden
            }
            catch (Exception e) { Debug.LogWarning("Reklam başlatılamadı: " + e.Message); }
#endif
        }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        static BannerView serit; static bool seritAcik; static int sonX = int.MinValue, sonY = int.MinValue;
        static RewardedAd odullu; static bool odulluYukleniyor; static float odulluSonDeneme = -999;

        static void Ilklendir()
        {
            if (hazir) return; hazir = true;
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.SetiOSAppPauseOnBackground(true);   // tam ekran reklam sürerken oyun ve ses durur
            // Yaş derecesi 4+ (store_listing/yas_derecesi.md): reklam içeriği de genel izleyici (G) sınırında kalır.
            MobileAds.SetRequestConfiguration(new RequestConfiguration { MaxAdContentRating = MaxAdContentRating.G });
            MobileAds.Initialize(durum => OdulluHazirla());
        }
#endif

        // Şerit yeri ekran pikselinde (sol üst köken) ya da null = gizle. UiKok her kare çağırır; değişmeyince iş yapmaz.
        public static void Serit(Rect? px)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!hazir) return;
            if (px == null) { if (serit != null && seritAcik) { serit.Hide(); seritAcik = false; } return; }
            float olc = MobileAds.Utils.GetDeviceScale(); if (olc <= 0) olc = 1;
            // 2026-11-07 (yasin cihaz karesi: şerit kartların üstünde, sola yaslı): Kalite basamağı çözünürlüğü %85 / %70'e indiriyor
            // (Screen.SetResolution); Screen pikseli artık yerel piksel değil. Nokta = Screen pikseli / (ölçek × Screen/yerel genişlik).
            int sysW = Display.main.systemWidth; if (sysW > 0 && Screen.width > 0) olc *= (float)Screen.width / sysW;
            var r = px.Value;
            float kx = r.x + (r.width - SERIT_W * olc) / 2, ky = r.y + (r.height - SERIT_H * olc) / 2;
#if UNITY_IOS
            // 2026-11-07 (yasin TF 100 karesi: şerit ortalandı ama sekme düğmelerinin üstüne biniyor): eklentinin iOS tarafı
            // (GADUPluginUtil positionView:customPosition:) özel konumu ekranın değil GÜVENLİ ALANIN sol üstüne ekliyor; çentikli
            // telefonda şerit üst pay kadar (≈50–60 nokta) aşağı kayıyordu. Yer ekran pikselinde geldiği için payı burada düşüyoruz.
            var sa = Screen.safeArea; kx -= sa.xMin; ky -= Mathf.Max(0, Screen.height - sa.yMax);
#endif
            int x = Mathf.RoundToInt(kx / olc), y = Mathf.RoundToInt(ky / olc);
            if (serit == null)
            {
                serit = new BannerView(SERIT_ID, AdSize.Banner, x, y); sonX = x; sonY = y; seritAcik = true;
                serit.OnBannerAdLoaded += () => SeritYuklu = true;
                serit.OnBannerAdLoadFailed += h => { SeritYuklu = false; Debug.LogWarning("Reklam şeridi yüklenemedi: " + h.GetMessage()); };
                serit.LoadAd(new AdRequest());
                return;
            }
            if (x != sonX || y != sonY) { serit.SetPosition(x, y); sonX = x; sonY = y; }
            if (!seritAcik) { serit.Show(); seritAcik = true; }
#endif
        }

        public static bool OdulluHazir
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            get { return odullu != null && odullu.CanShowAd(); }
#else
            get { return false; }
#endif
        }

        // Maç başında (GameRoot): yüklü değilse yükle. Başarısız yüklemeden sonra 60 sn beklenir (ağsız oyuncuyu yormaz).
        public static void OdulluHazirla()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!hazir || odulluYukleniyor || OdulluHazir || Time.realtimeSinceStartup - odulluSonDeneme < 60) return;
            odulluYukleniyor = true; odulluSonDeneme = Time.realtimeSinceStartup;
            RewardedAd.Load(ODULLU_ID, new AdRequest(), (ad, hata) =>
            {
                odulluYukleniyor = false;
                if (hata != null || ad == null) { Debug.LogWarning("Ödüllü reklam yüklenemedi: " + (hata != null ? hata.GetMessage() : "boş")); return; }
                odullu = ad; odulluSonDeneme = -999;
            });
#endif
        }

        // bitti(true) yalnız SDK ödül kazanıldı dediyse; kapanınca bir kez çağrılır.
        public static void OdulluGoster(Action<bool> bitti)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!OdulluHazir) { bitti(false); return; }
            var ad = odullu; odullu = null; bool kazandi = false, kapandi = false;
            Action kapat = () =>
            {
                if (kapandi) return; kapandi = true; AudioListener.pause = false;
                ad.Destroy(); odulluSonDeneme = -999; OdulluHazirla(); bitti(kazandi);
            };
            ad.OnAdFullScreenContentClosed += () => kapat();
            ad.OnAdFullScreenContentFailed += h => { Debug.LogWarning("Ödüllü reklam açılamadı: " + h.GetMessage()); kazandi = false; kapat(); };
            AudioListener.pause = true;
            ad.Show(odul => kazandi = true);
#else
            bitti(false);
#endif
        }
    }
}
