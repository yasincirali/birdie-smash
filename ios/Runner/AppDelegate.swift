import Flutter
import StoreKit
import UIKit

@main
@objc class AppDelegate: FlutterAppDelegate, FlutterImplicitEngineDelegate {
  override func application(
    _ application: UIApplication,
    didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?
  ) -> Bool {
    return super.application(application, didFinishLaunchingWithOptions: launchOptions)
  }

  func didInitializeImplicitFlutterEngine(_ engineBridge: FlutterImplicitEngineBridge) {
    GeneratedPluginRegistrant.register(with: engineBridge.pluginRegistry)
    // Kabuk kanalı (lib/kabuk.dart kabukKanalAdi ile aynı ad). Yeni paket eklenmedi: iki iş de birkaç satır.
    let kanal = FlutterMethodChannel(
      name: "birdiesmash/kabuk", binaryMessenger: engineBridge.applicationRegistrar.messenger())
    kanal.setMethodCallHandler { call, result in
      switch call.method {
      case "devirYaz": result(AppDelegate.devirYaz(call.arguments))
      case "puanIste":
        AppDelegate.puanIste()
        result(nil)
      default: result(FlutterMethodNotImplemented)
      }
    }
  }

  // HTML → Unity ilerleme devri (Ar-Ge 25 Eki #1). Unity PlayerPrefs iOS'ta NSUserDefaults.standard'dır: anahtar
  // adı aynen, SetString değeri NSString. Bu yüzden tuytop_* değerleri aynı adla string yazılır; Unity ilk açılışta
  // kendi biçimiyle okur (Core/Devir.cs) ve tuytop_html_devir bayrağını koyar. Bayrak varsa hiçbir şey yazılmaz:
  // eski bir HTML build'i yeniden açılsa da Unity'deki yeni ilerlemenin üstüne yazılmaz.
  static func devirYaz(_ arguments: Any?) -> Bool {
    guard let veri = arguments as? [String: String] else { return false }
    let d = UserDefaults.standard
    if d.object(forKey: "tuytop_html_devir") != nil { return false }
    for (k, v) in veri where k.hasPrefix("tuytop_") && k != "tuytop_html_devir" {
      d.set(v, forKey: k)
    }
    return true
  }

  // Mağaza puanı (Ar-Ge 23 Eki #1, App Store 5.6.1): yalnız sistem penceresi. Sıklığı oyun ve Apple sınırlar;
  // TestFlight'ta pencere hiç görünmez (Apple davranışı).
  static func puanIste() {
    Task { @MainActor in
      let sahne = UIApplication.shared.connectedScenes
        .first { $0.activationState == .foregroundActive } as? UIWindowScene
      guard let sahne else { return }
      if #available(iOS 16.0, *) {
        AppStore.requestReview(in: sahne)
      } else {
        SKStoreReviewController.requestReview(in: sahne)
      }
    }
  }
}
