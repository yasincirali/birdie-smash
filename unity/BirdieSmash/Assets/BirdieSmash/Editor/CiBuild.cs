// TestFlight hattının Unity yolu (2026-10-24, yasin: "Unity ile değiştirelim menüleri ve tüm oyunu"):
// game-ci/unity-builder bu yöntemi `buildMethod` ile çağırır, çıktı Xcode projesidir; imzalama, build numarası
// ve yükleme macOS'ta fastlane `beta_unity` lane'indedir (Unity tarafı App Store Connect'i görmez).
// Neden ayarlar burada, ProjectSettings.asset'te değil: depoda ProjectSettings.asset yok (Unity ilk açılışta
// varsayılanla üretir); elle yazılmış bir asset Unity sürümüyle uyuşmazsa sessizce bozulur. Build anında
// PlayerSettings koddan yazılır ki CI'daki taze kopya ile yasin'ın Editor'ü aynı sonucu versin.
// Info.plist'e ITSAppUsesNonExemptEncryption=false burada da yazılır (fastlane ayrıca yazar): anahtar yoksa
// build App Store Connect'te "Missing Compliance"ta bekler, test edilemez.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BirdieSmash.EditorTools
{
    public static class CiBuild
    {
        const string Bundle = "com.birdiesmash.app";
        const string Icon = "Assets/BirdieSmash/Icon/AppIcon.png";   // Flutter ikonunun kopyası (ios/Runner/.../Icon-App-1024x1024@1x.png)

        // unity-builder: -customBuildPath <workspace>/build/iOS/iOS (klasör = Xcode projesi)
        // IL2CPP çökme izi (2026-10-29, Ar-Ge 3): `il2cpp --convert-to-cpp` runner'da ara sıra SIGSEGV ile düşüyor (25 Eki
        // run 45, 29 Eki PR #7 ilk derleme; ikisi de aynı commit'te yeniden denemede geçti). Unity hatayı tek günlük iletisinde
        // komut satırı + "Segmentation fault (core dumped)" olarak basar. İkisi aynı iletideyse build klasörünün yanına
        // il2cpp_cokme.txt yazılır; ios-testflight.yml yalnız bu dosya varsa bir kez yeniden dener. Derleme (C#) hatası bu
        // izi taşımaz, yeniden denenmez: gerçek hata gizlenmesin.
        static bool il2cppCokme;
        static void GunlukDinle(string msg, string iz, LogType tip)
        {
            if (tip == LogType.Log || tip == LogType.Warning || msg == null) return;
            if (msg.IndexOf("il2cpp", StringComparison.OrdinalIgnoreCase) >= 0 && msg.IndexOf("Segmentation fault", StringComparison.Ordinal) >= 0) il2cppCokme = true;
        }

        public static void iOS()
        {
            string path = Arg("-customBuildPath") ?? "build/iOS/iOS";
            Application.logMessageReceivedThreaded += GunlukDinle;
            try
            {
                DemoKurulum.Kur();   // sahne Assets/Scenes/Demo.unity + build listesi + shader/girdi ayarları (CI kopyası taze)
                DokuAyar.Denetle();  // 2026-10-27: her doku ASTC/boy/mip hedefinde mi (değilse yeniden içe aktarır, günlüğe satır basar)
                Ayarla(Arg("-buildVersion"));
                var opt = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = path, target = BuildTarget.iOS, targetGroup = BuildTargetGroup.iOS, options = BuildOptions.None
                };
                if (opt.scenes.Length == 0) throw new Exception("Build listesinde sahne yok (DemoKurulum.Kur)");
                var rep = BuildPipeline.BuildPlayer(opt);
                if (rep.summary.result != BuildResult.Succeeded) throw new Exception("BuildPlayer: " + rep.summary.result + ", hata " + rep.summary.totalErrors);
                PlistSifreleme(Path.Combine(path, "Info.plist")); PlistProMotion(Path.Combine(path, "Info.plist"));
                Debug.Log("Birdie Smash iOS Xcode projesi: " + path);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("Birdie Smash CI build kırıldı: " + e);
                if (il2cppCokme)
                    try { File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), "il2cpp_cokme.txt"), "il2cpp Segmentation fault\n"); Debug.Log("Birdie Smash: IL2CPP çökmesi işaretlendi (il2cpp_cokme.txt)"); }
                    catch (Exception e2) { Debug.LogWarning("il2cpp_cokme.txt yazılamadı: " + e2.Message); }
                EditorApplication.Exit(1);
            }
        }

        // Ekran görüntüsü hattı (unity-ekran.yml, 2026-10-25): aynı sahne Linux oyuncusu olarak (Mono, hızlı derleme);
        // CI xvfb altında `-bsekran` ile koşar (Game/EkranTuru). Mağazaya gitmez.
        public static void Linux()
        {
            try
            {
                string path = Arg("-customBuildPath") ?? "build/StandaloneLinux64/BirdieSmash";
                DemoKurulum.Kur();
                DokuAyar.Denetle();  // ekran turu da aynı boy/mip/süzmeyle çizsin (ASTC yalnız iOS/Android'de; Linux'ta DXT)
                PlayerSettings.productName = "Birdie Smash"; PlayerSettings.companyName = "Birdie Smash";
#pragma warning disable 618
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
#pragma warning restore 618
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
                PlayerSettings.runInBackground = true;   // xvfb'de pencere yöneticisi yok, odak gelmez: yoksa oyuncu ilk karede durur (gdb: PlayerMain usleep)
                PlayerSettings.defaultScreenWidth = 590; PlayerSettings.defaultScreenHeight = 1278;
                var opt = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = path, target = BuildTarget.StandaloneLinux64, targetGroup = BuildTargetGroup.Standalone, options = BuildOptions.None
                };
                var rep = BuildPipeline.BuildPlayer(opt);
                if (rep.summary.result != BuildResult.Succeeded) throw new Exception("BuildPlayer: " + rep.summary.result + ", hata " + rep.summary.totalErrors);
                Debug.Log("Birdie Smash Linux oyuncusu: " + path);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogError("Birdie Smash Linux build kırıldı: " + e); EditorApplication.Exit(1); }
        }

#pragma warning disable 618   // BuildTargetGroup imzaları Unity 6'da eski sayılır (uyarı); DemoKurulum ile aynı aile, 2021+ derler
        static void Ayarla(string surum)
        {
            PlayerSettings.productName = "Birdie Smash"; PlayerSettings.companyName = "Birdie Smash";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, Bundle);
            // Sürüm adı ve build numarası fastlane'de Info.plist'e yeniden yazılır (TestFlight'taki son numara + 1);
            // buradaki değer yalnız yerel Editor build'i için anlamlı.
            if (!string.IsNullOrEmpty(surum) && surum != "none") PlayerSettings.bundleVersion = surum;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);   // iOS'ta zorunlu
            // Yalnız dikey (HTML kabuğuyla aynı): otomatik döndürme kapalı.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            // iPad'de dikey kilit için tam ekran şart (Apple: çoklu pencere destekleyen uygulama tüm yönleri ister).
            // 2026-10-26 (build 40, App Store Connect ITMS-90068): Unity varsayılanı iOS 13.0; Nisan 2027'den itibaren 15.0 altı
            // yüklenemez. Flutter hattı (ios/Runner.xcodeproj) zaten 15.0, iki hat aynı alt sınırı taşır.
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;   // imza fastlane match'ten; Xcode otomatik imzaya kalkışmasın
            PlayerSettings.statusBarHidden = true;
            // Açılış (2026-10-26, yasin Ar-Ge önerisi "ekle"): Unity açılış ekranı kapalı (Unity 6 Personal'da serbest);
            // arka plan açılış perdesinin dış halkasıyla aynı renk (Hud PERDE_C[0,0] #a3234b), oyunun ilk karesine siyah
            // kare girmeden geçilsin. iOS açılış görüntüsü de aynı düz renk.
            // 2026-10-27'de yön C ile #1A1221'e çekilmişti; 2026-10-29 (yasin: "html zamanlarındaki gibi olmalı") perde yeniden
            // HTML'deki halkalı diskle açıldığı için açılış rengi yine dış halka #a3234b (Apple HIG: ilk kareyle aynı renk).
            var perde = new Color(0xa3 / 255f, 0x23 / 255f, 0x4b / 255f);
            PlayerSettings.SplashScreen.show = false; PlayerSettings.SplashScreen.showUnityLogo = false; PlayerSettings.SplashScreen.backgroundColor = perde;
            IosAcilisRengi(perde);
            var ikon = AssetDatabase.LoadAssetAtPath<Texture2D>(Icon);
            // Unknown grubu = "Default Icon": Unity tüm iOS boylarını bundan üretir. fastlane ayrıca Flutter'ın
            // AppIcon.appiconset'ini Xcode projesine kopyalar (1024'lük ikon alfa kanalsız kalsın diye).
            if (ikon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { ikon });
            else Debug.LogWarning("Birdie Smash: ikon yok (" + Icon + ")");
        }
#pragma warning restore 618

        // Plist'i metin olarak düzenler: UnityEditor.iOS.Xcode yalnız iOS modülü kuruluyken derlenir, bu dosya
        // modülsüz Editor'de (ve tool/unity_test derlemesinde) de kırılmasın.
        static void PlistSifreleme(string plist)
        {
            if (!File.Exists(plist)) { Debug.LogWarning("Info.plist yok: " + plist); return; }
            var t = File.ReadAllText(plist);
            if (t.Contains("ITSAppUsesNonExemptEncryption")) return;
            int i = t.LastIndexOf("</dict>", StringComparison.Ordinal);
            if (i < 0) return;
            File.WriteAllText(plist, t.Substring(0, i) + "\t<key>ITSAppUsesNonExemptEncryption</key>\n\t<false/>\n" + t.Substring(i));
        }

        // iOS varsayılan açılış görüntüsünün arka planı ProjectSettings'te (iOSLaunchScreenBackgroundColor); public API yok,
        // SerializedObject ile yazılır. Alan adı bulunamazsa uyarı verir, build sürer (renk Unity varsayılanı kalır).
        static void IosAcilisRengi(Color c)
        {
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps == null || ps.Length == 0) { Debug.LogWarning("ProjectSettings.asset okunamadı"); return; }
            var so = new SerializedObject(ps[0]); bool ok = false;
            var t = so.FindProperty("iOSLaunchScreenType"); if (t != null) { t.intValue = 0; ok = true; }   // 0 = Default (düz renk)
            var b = so.FindProperty("iOSLaunchScreenBackgroundColor"); if (b != null) { b.colorValue = c; ok = true; }
            var bp = so.FindProperty("iOSLaunchScreeniPadBackgroundColor"); if (bp != null) bp.colorValue = c;
            if (!ok) Debug.LogWarning("iOS açılış rengi alanı yok"); so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ProMotion: iPhone'da 60 Hz üstü için Info.plist'te CADisableMinimumFrameDurationOnPhone = true şart (Apple);
        // Unity'nin Xcode çıktısı yazmıyorsa eklenir.
        static void PlistProMotion(string plist)
        {
            if (!File.Exists(plist)) return; var t = File.ReadAllText(plist);
            if (t.Contains("CADisableMinimumFrameDurationOnPhone")) { Debug.Log("ProMotion anahtarı Unity çıktısında var"); return; }
            int i = t.LastIndexOf("</dict>", StringComparison.Ordinal); if (i < 0) return;
            File.WriteAllText(plist, t.Substring(0, i) + "\t<key>CADisableMinimumFrameDurationOnPhone</key>\n\t<true/>\n" + t.Substring(i));
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
