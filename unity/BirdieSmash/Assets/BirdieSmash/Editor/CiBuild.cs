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
                ReklamAyar();
                var opt = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = path, target = BuildTarget.iOS, targetGroup = BuildTargetGroup.iOS, options = BuildOptions.None
                };
                if (opt.scenes.Length == 0) throw new Exception("Build listesinde sahne yok (DemoKurulum.Kur)");
                var rep = BuildPipeline.BuildPlayer(opt);
                if (rep.summary.result != BuildResult.Succeeded) throw new Exception("BuildPlayer: " + rep.summary.result + ", hata " + rep.summary.totalErrors);
                PlistSifreleme(Path.Combine(path, "Info.plist")); PlistProMotion(Path.Combine(path, "Info.plist")); PlistReklam(Path.Combine(path, "Info.plist"));
                IosYerellestirme(path);
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
            // 2026-11-11 (inceleme, fikir 20:05 #5a): IL2CPP C++ yapılandırması Master (Unity varsayılanı Release). Bağlantı anı
            // optimizasyonu Core simülasyonunu (Shuttle.Aim, PlanIntercept) ve OnGUI yolunu kod değişmeden hızlandırır, ikiliyi
            // küçültür; bedeli yalnız Xcode derleme süresi. Kod küçültme (ManagedStrippingLevel) bilerek varsayılanda: yansımayla
            // çağrılan IAP/AdMob kodu silinirse açılışta çöker; link.xml + TestFlight satın alma denemesiyle ayrı adım.
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.iOS, Il2CppCompilerConfiguration.Master);
            // Yalnız dikey (HTML kabuğuyla aynı): otomatik döndürme kapalı.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            // iPad'de dikey kilit için tam ekran şart (Apple: çoklu pencere destekleyen uygulama tüm yönleri ister).
            // 2026-10-26 (build 40, App Store Connect ITMS-90068): Unity varsayılanı iOS 13.0; Nisan 2027'den itibaren 15.0 altı
            // yüklenemez. Flutter hattı (ios/Runner.xcodeproj) zaten 15.0, iki hat aynı alt sınırı taşır.
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresFullScreen = true;
            // İvmeölçer kapalı (2026-11-11 inceleme, fikir 21:05 #3): oyun eğim/sallama okumuyor; Unity iOS varsayılanı 60 Hz örnekleme
            // boşuna sensör ve pil harcıyordu. Eğimli bir özellik gelirse burası açılır.
            PlayerSettings.accelerometerFrequency = 0;
            // Yalnız iPhone (2026-11-06 Ar-Ge 22:03 #1): Unity varsayılanı evrensel ikiliydi; iPad'de Kadraj tasarlanmış düzen değil,
            // son çare kaydırmayla çiziyor ve mağaza 13" iPad karesi istiyordu. Apple yayınlanmış uygulamadan cihaz ailesi kaldırmaya
            // izin vermez, eklemeye izin verir: 1.0 iPhone ile çıkar, iPad sahibi uyumluluk modunda aynı kadrajı oynar. iPad düzeni
            // ayrı karar (o zaman bu satır + iPad kareleri). Flutter yolu da aynı: ios/Runner TARGETED_DEVICE_FAMILY = 1.
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;   // imza fastlane match'ten; Xcode otomatik imzaya kalkışmasın
            PlayerSettings.statusBarHidden = true;
            // Oyuncunun kendi müziği susmasın (2026-10-30 menü müziği): Ambient oturum; menü müziği başka ses varken çalmaz (Yerel).
            PlayerSettings.muteOtherAudioSources = false;
            // Açılış (2026-10-26, yasin Ar-Ge önerisi "ekle"): Unity açılış ekranı kapalı (Unity 6 Personal'da serbest);
            // arka plan açılış perdesinin dış halkasıyla aynı renk (Hud PERDE_C[0,0] #a3234b), oyunun ilk karesine siyah
            // kare girmeden geçilsin. iOS açılış görüntüsü de aynı düz renk.
            // 2026-10-27'de yön C ile #1A1221'e çekilmişti; 2026-10-29 (yasin: "html zamanlarındaki gibi olmalı") perde yeniden
            // HTML'deki halkalı diskle açıldığı için açılış rengi yine dış halka #a3234b (Apple HIG: ilk kareyle aynı renk).
            var perde = new Color(0xa3 / 255f, 0x23 / 255f, 0x4b / 255f);
            PlayerSettings.SplashScreen.show = false; PlayerSettings.SplashScreen.showUnityLogo = false; PlayerSettings.SplashScreen.backgroundColor = perde;
            IosAcilisRengi(perde); IosAcilisTopu();
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

        // Açılış ekranında dönen topun 0. karesi (2026-10-30, yasin "splash gifi ilk açıldığı andan itibaren başlamalı"): düz renk
        // üstünde ortada, yerel perdenin (Plugins/iOS/BirdieAcilis.mm) ilk karesiyle aynı yer ve boy; dokunuştan sonraki ilk
        // görüntüde top zaten durur, motor açılmadan dönmeye başlar. Görsel kare: ekranın kısa kenarının %50'si, top Hud
        // geometrisiyle (merkezden .026·m yukarı, boy .3·m) içine çizilmiş (Editor/acilis_top.png, scratchpad üreteci).
        // "Görsel ve arka plan (göreli)" tipi; alan adları public API'de olmadığı için SerializedObject. Bulunamazsa düz renk kalır.
        const string AcilisTop = "Assets/BirdieSmash/Editor/acilis_top.png";
        static void IosAcilisTopu()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AcilisTop);
            if (tex == null) { Debug.LogWarning("Birdie Smash: açılış topu yok (" + AcilisTop + ")"); return; }
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps == null || ps.Length == 0) return;
            var so = new SerializedObject(ps[0]);
            foreach (var ad in new[] { "iOSLaunchScreenType", "iOSLaunchScreeniPadType" }) { var t = so.FindProperty(ad); if (t != null) t.intValue = 1; else Debug.LogWarning("alan yok: " + ad); }   // 1 = ImageAndBackgroundRelative
            foreach (var ad in new[] { "iOSLaunchScreenFillPct", "iOSLaunchScreeniPadFillPct" }) { var f = so.FindProperty(ad); if (f != null) f.floatValue = 50; else Debug.LogWarning("alan yok: " + ad); }
            so.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.iOS.SetLaunchScreenImage(tex, iOSLaunchScreenImageType.iPhonePortraitImage);
            PlayerSettings.iOS.SetLaunchScreenImage(tex, iOSLaunchScreenImageType.iPadImage);
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

        // ---- AdMob (2026-11-07, yasin: "şerit en başta bağlı olarak çıksın"; Game/ReklamSdk.cs) ----
        // iOS uygulama kimliği 2026-11-11'den beri yasin'in AdMob hesabı (mağaza 1.0); Android hattı olmadığı için Android'de
        // Google'ın TEST kimliği kalır (ReklamSdk.TEST_KIMLIK ile aynı ayrım). Kimlik gizli değildir (her IPA'nın Info.plist'inde düz yazılır).
        // Eklentinin ayar varlığı (Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset) depoda yok: ProjectSettings
        // gibi koddan yazılır. Yansıma ile, çünkü bu dosya eklentisiz Editor'de ve tool/unity_test derlemesinde de derlenmeli.
        // Eklentinin iOS derleme sonrası adımı kimlik boşsa build'i kırar; kimlik yine de yazılmadıysa PlistReklam yazar,
        // fastlane de GADApplicationIdentifier'sız IPA'yı yüklemez (SDK açılışta uygulamayı kapatır).
        public const string ADMOB_IOS_UYG = "ca-app-pub-6645967875947629~7956374345";
        public const string ADMOB_ANDROID_UYG = "ca-app-pub-3940256099942544~3347511713";
        // ATT metni: UMP rıza akışı AdMob panelinde IDFA açıklaması tanımlanırsa sistem izin penceresini açar; anahtar yoksa iOS
        // uygulamayı kapatır. Ton oyunun dili: kısa, suçlamasız, ne olduğunu söyler. İngilizce taban (mağaza dili global);
        // Türkçesi tr.lproj/InfoPlist.strings'te (IosYerellestirme, 2026-11-07).
        const string ATT_METNI = "Used to show you more relevant ads. If you do not allow it, the game works exactly the same.";
        const string ATT_METNI_TR = "Sana daha ilgili reklamlar göstermek için. İzin vermezsen oyun tamamen aynı çalışır.";
        static void ReklamAyar()
        {
            Type t = null;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) { t = a.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings", false); if (t != null) break; }
            if (t == null) { Debug.LogWarning("Birdie Smash: AdMob eklentisi yok (GoogleMobileAdsSettings), reklam ayarı atlandı"); return; }
            var yukle = t.GetMethod("LoadInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var o = yukle != null ? yukle.Invoke(null, null) as UnityEngine.Object : null;
            if (o == null) { Debug.LogWarning("Birdie Smash: GoogleMobileAdsSettings.LoadInstance yok ya da boş döndü"); return; }
            int n = AyarYaz(o, "iosappid", ADMOB_IOS_UYG) + AyarYaz(o, "androidappid", ADMOB_ANDROID_UYG) + AyarYaz(o, "usertrackingusagedescription", ATT_METNI);
            EditorUtility.SetDirty(o); AssetDatabase.SaveAssets();
            Debug.Log("Birdie Smash: AdMob ayarı yazıldı (" + n + "/3 alan, iOS " + ADMOB_IOS_UYG + ")");
        }
        // Özellik ya da alan adı sürümden sürüme değişti (adMobIOSAppId → GoogleMobileAdsIOSAppId): adı içeren ilk yazılabilir üye.
        static int AyarYaz(object o, string parca, string deger)
        {
            const System.Reflection.BindingFlags B = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            foreach (var p in o.GetType().GetProperties(B))
                if (p.PropertyType == typeof(string) && p.CanWrite && p.Name.ToLowerInvariant().Contains(parca)) { p.SetValue(o, deger, null); return 1; }
            foreach (var f in o.GetType().GetFields(B))
                if (f.FieldType == typeof(string) && f.Name.ToLowerInvariant().Contains(parca)) { f.SetValue(o, deger); return 1; }
            Debug.LogWarning("Birdie Smash: AdMob ayarında '" + parca + "' alanı bulunamadı");
            return 0;
        }
        // Eklenti yazmadıysa son çare: uygulama kimliği, ATT metni ve Google'ın SKAdNetwork kimliği (AdMob şartı).
        static void PlistReklam(string plist)
        {
            if (!File.Exists(plist)) return; var t = File.ReadAllText(plist); string ek = "";
            Type gma = null; foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) { gma = a.GetType("GoogleMobileAds.Api.MobileAds", false); if (gma != null) break; }
            if (gma == null) return;   // eklentisiz build: reklam anahtarı yazılmaz
            if (!t.Contains("GADApplicationIdentifier")) ek += "\t<key>GADApplicationIdentifier</key>\n\t<string>" + ADMOB_IOS_UYG + "</string>\n";
            if (!t.Contains("NSUserTrackingUsageDescription")) ek += "\t<key>NSUserTrackingUsageDescription</key>\n\t<string>" + ATT_METNI + "</string>\n";
            if (!t.Contains("SKAdNetworkItems")) ek += "\t<key>SKAdNetworkItems</key>\n\t<array>\n\t\t<dict>\n\t\t\t<key>SKAdNetworkIdentifier</key>\n\t\t\t<string>cstr6suwn9.skadnetwork</string>\n\t\t</dict>\n\t</array>\n";
            if (ek == "") { Debug.Log("Birdie Smash: reklam plist anahtarları eklentiden geldi"); return; }
            int i = t.LastIndexOf("</dict>", StringComparison.Ordinal); if (i < 0) return;
            File.WriteAllText(plist, t.Substring(0, i) + ek + t.Substring(i));
            Debug.Log("Birdie Smash: reklam plist anahtarları CiBuild'den eklendi");
        }

        // ---- iOS yerelleştirme (2026-11-07 inceleme turu) ----
        // Unity'nin Xcode çıktısı yalnız İngilizce taşır: App Store ürün sayfası "Diller: English" der (oyun içi tam Türkçe olsa da;
        // dil listesi paketteki .lproj klasörlerinden okunur) ve ATT penceresi Türkçe cihazda İngilizce çıkar (5.1.1: amaç metni
        // oyuncunun anlayacağı dilde olmalı). Çözüm: Info.plist'e CFBundleLocalizations [en, tr] + en.lproj/tr.lproj
        // InfoPlist.strings (ad + ATT), klasörler Xcode projesine "folder reference" olarak (paketin köküne aynen kopyalanır).
        // PBXProject yalnız iOS modülüyle derlenir: #if UNITY_IOS (iOS hedefinde Editor tanımlı; Linux ekran build'i ve
        // tool/unity_test derlemesi bu bloğu görmez). Hata build'i kırmaz, yalnız uyarı: dil satırı eksik kalır, oyun aynı çalışır.
        // Yeni dil = DILLER'e bir satır (Asya dilleri mağaza metniyle birlikte).
        static readonly string[][] DILLER = {
            new[] { "en", "Birdie Smash", ATT_METNI },
            new[] { "tr", "Birdie Smash", ATT_METNI_TR },
        };
        static void IosYerellestirme(string xcode)
        {
            try
            {
                var plist = Path.Combine(xcode, "Info.plist");
                if (!File.Exists(plist)) { Debug.LogWarning("Birdie Smash: yerelleştirme atlandı, Info.plist yok"); return; }
                var t = File.ReadAllText(plist);
                bool att = t.Contains("NSUserTrackingUsageDescription");
                if (!t.Contains("CFBundleLocalizations"))
                {
                    string dizi = "\t<key>CFBundleLocalizations</key>\n\t<array>\n";
                    foreach (var d in DILLER) dizi += "\t\t<string>" + d[0] + "</string>\n";
                    dizi += "\t</array>\n";
                    int i = t.LastIndexOf("</dict>", StringComparison.Ordinal);
                    if (i >= 0) { t = t.Substring(0, i) + dizi + t.Substring(i); File.WriteAllText(plist, t); }
                }
                var yazilan = new System.Collections.Generic.List<string>();
                foreach (var d in DILLER)
                {
                    var kl = Path.Combine(xcode, d[0] + ".lproj"); Directory.CreateDirectory(kl);
                    string s = "\"CFBundleDisplayName\" = \"" + d[1] + "\";\n";
                    if (att) s += "\"NSUserTrackingUsageDescription\" = \"" + d[2] + "\";\n";
                    File.WriteAllText(Path.Combine(kl, "InfoPlist.strings"), s, new System.Text.UTF8Encoding(false));
                    yazilan.Add(d[0] + ".lproj");
                }
#if UNITY_IOS
                var pp = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(xcode);
                var proj = new UnityEditor.iOS.Xcode.PBXProject(); proj.ReadFromFile(pp);
                var hedef = proj.GetUnityMainTargetGuid();
                foreach (var ad in yazilan)
                {
                    if (proj.ContainsFileByProjectPath(ad)) continue;
                    // Göreli yol (TF 99 kırıldı): Unity Xcode projesini Linux konteynerinde /github/workspace/... altında yazıyor,
                    // macOS koşucusu projeyi başka yere açıyor; mutlak yol "en.lproj couldn't be opened" ile arşivi düşürdü.
                    // Kaynak ağacı Source = proje klasörüne göreli.
                    var g = proj.AddFolderReference(ad, ad, UnityEditor.iOS.Xcode.PBXSourceTree.Source);
                    proj.AddFileToBuild(hedef, g);
                }
                proj.WriteToFile(pp);
                Debug.Log("Birdie Smash: iOS dilleri yazıldı (" + string.Join(", ", yazilan.ToArray()) + ", ATT " + (att ? "var" : "yok") + ")");
#else
                Debug.LogWarning("Birdie Smash: UNITY_IOS tanımsız, .lproj klasörleri Xcode projesine eklenmedi");
#endif
            }
            catch (Exception e) { Debug.LogWarning("Birdie Smash: iOS yerelleştirme atlandı: " + e.Message); }
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
