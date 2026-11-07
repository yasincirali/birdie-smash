# App Store Connect › App Privacy cevapları — Birdie Smash

> **2026-11-07: AdMob bağlandı (Game/ReklamSdk.cs).** Aşağıdaki "Data Not Collected" reklamsız sürüm içindir. Reklamlı
> 1.0 için cevap **Yes, we collect data**: Identifiers › Device ID (yalnız ATT izni verilirse IDFA), Usage Data › Advertising
> Data + Product Interaction, Diagnostics › Performance/Crash (Google SDK); hepsi amaç **Third-Party Advertising**,
> kimliğe bağlı değil (Not Linked to You); Device ID ve Advertising Data için "Used to Track You" = Yes (ATT izniyle).
> Kaynak: Google'ın "AdMob App Store data disclosure" sayfası; SDK sürümü değişince yeniden bakılır.

Tarih: 2026-10-24 · Kod: `main` @ 9e781a2 · Sonuç: **Data Not Collected**

## Sorulacak ilk soru

**"Do you or your third-party partners collect data from this app?"** → **No, we do not collect data from this app.**

Apple'ın tanımı: "collect" = veriyi cihazdan çıkarıp senin ya da ortaklarının, isteği o anda karşılamaktan daha uzun süre okuyabileceği biçimde saklaması. Yalnız cihazda işlenen veri "collected" sayılmaz. Apple'ın kendi topladığı veriyi (Game Center, App Store istatistikleri, Apple'ın çökme raporları) geliştirici bildirmez ("You are not responsible for disclosing data collected by Apple").

## Neden "No" (doğrulama, 2026-10-24)

- **Oyun** (`assets/game/index.html`): içinde tek bir `http://` / `https://` adresi yok. `fetch(` çağrıları yalnız dosyaya gömülü `data:image/...;base64` sprite'larını açıyor (oyuncu, rakip, hakem, ekipman görselleri). `XMLHttpRequest`, `WebSocket`, `sendBeacon`, çerez, analitik yok (`gtag` eşleşmeleri base64 verisinin içindeki rastgele harfler).
- **Kayıt**: yalnız `localStorage`, anahtarlar `tuytop_v2`, `tuytop_ekonomi`, `tuytop_gear`, `tuytop_basarim`, `tuytop_antrenman`, `tuytop_mod`, `tuytop_saha`, `tuytop_len`, `tuytop_raket_gor`. Cihazdan çıkmaz.
- **Kabuk** (`lib/main.dart`, `pubspec.yaml`): bağımlılıklar `webview_flutter` (yerel `assets/game/index.html` dosyasını açar), `games_services` (Game Center), `cupertino_icons`. Ağ, analitik, reklam, çökme raporu paketi yok. `Haptic` kanalı titreşim, `Oyun` kanalı Game Center girişi ve başarım kimliklerini Apple'a iletir.
- **Game Center**: oyun yalnız başarım kimliklerini (`bs_kariyer_1` vb.) Apple'ın hizmetine gönderir; oyuncu kimliğini okumaz/saklamaz, geliştiriciye ulaşan bir şey yok. Apple'ın topladığı veri → bildirilmez.
- **İzinler**: Info.plist'te hiçbir `NS...UsageDescription` yok (kamera, mikrofon, konum, rehber, fotoğraf istenmiyor). Bildirim yok.
- **Reklam / izleme**: reklam SDK'sı yok, IDFA okunmuyor, App Tracking Transparency istemi yok.
- **Satın alma**: StoreKit / uygulama içi satın alma yok. "Tüy" yalnız oyun içinde kazanılır.

## Veri türü başına gerekçe

| Veri türü (ASC adı) | Cevap | Neden |
|---|---|---|
| Contact Info (Name, Email, Phone, Physical Address, Other) | Toplanmaz | Hesap ya da form yok; hiçbir metin girişi yok. |
| Health & Fitness | Toplanmaz | HealthKit yok; "nefes" yalnız oyun içi değer. |
| Financial Info (Payment, Credit, Other) | Toplanmaz | Satın alma yok. |
| Location (Precise, Coarse) | Toplanmaz | Konum izni ve API'si yok. |
| Sensitive Info | Toplanmaz | — |
| Contacts | Toplanmaz | Rehber izni yok. |
| User Content (Emails/Texts, Photos/Videos, Audio, Gameplay Content, Customer Support, Other) | Toplanmaz | Gameplay Content (skor, ilerleme, ekipman) yalnız cihazda `localStorage`'da. Başarım durumu Apple'ın Game Center'ına gider → Apple'ın verisi, bildirilmez. Mikrofon yok. |
| Browsing History | Toplanmaz | WebView yalnız yerel dosyayı açar; dış sayfa yok. |
| Search History | Toplanmaz | Arama yok. |
| Identifiers (User ID, Device ID) | Toplanmaz | Kendi kullanıcı kimliğimiz yok; Game Center oyuncu kimliği okunmuyor/gönderilmiyor; IDFA/IDFV okunmuyor. |
| Purchases (Purchase History) | Toplanmaz | Satın alma yok. |
| Usage Data (Product Interaction, Advertising Data, Other) | Toplanmaz | Analitik yok, reklam yok. |
| Diagnostics (Crash, Performance, Other) | Toplanmaz | Crashlytics/Sentry benzeri bir şey yok. Apple'ın kendi çökme raporları (kullanıcı "Uygulama geliştiricileriyle paylaş"ı açtıysa) Apple'ın verisi, bildirilmez. |
| Surroundings (Environment Scanning) | Toplanmaz | Kamera/AR yok. |
| Body (Hands, Head) | Toplanmaz | Kamera/AR yok. |
| Other Data | Toplanmaz | — |

## Bu cevap ne zaman değişir

Şunlardan biri eklenirse cevap "Yes" olur ve gizlilik metni (`legal/gizlilik.md`, `legal/privacy.md`) aynı değişiklikte güncellenir:
- çökme raporu / analitik SDK'sı (ör. Firebase) → Diagnostics, Usage Data, Identifiers;
- kendi sunucumuza skor/sıralama ya da bulut kaydı (kendi sunucumuz) → Gameplay Content, User ID;
- reklam → Advertising Data, Device ID, büyük ihtimalle "Tracking";
- uygulama içi satın alma → Purchase History (StoreKit'in kendi verisi Apple'ındır; kendi sunucumuzda makbuz tutarsak bildirilir).

iCloud ile ilerleme eşitleme (kabukta `{tip: 'kayit'}` için yer ayrılmış, şu an yok) kullanıcının kendi iCloud hesabına yazılırsa Apple'ın hizmetidir; yine de o gün bu dosya ve gizlilik metni yeniden gözden geçirilir.

## Privacy Manifest notu

Unity hattı (2026-11-07): kendi iOS eklentilerimiz (`BirdieAcilis.mm`, `BirdieBulut.mm`) `NSUserDefaults`'u doğrudan kullanır (PlayerPrefs ve HTML ilerleme devri); bu Apple'ın "required reason API" listesindedir. Gerekçe `Assets/Plugins/iOS/PrivacyInfo.xcprivacy` içinde: UserDefaults CA92.1 (yalnız uygulamanın kendi verisi), takip yok, toplanan veri yok. Unity bu dosyayı derlemede kendi UnityFramework manifestiyle birleştirir. Eski WebView kabuğu notu ("oyun localStorage kullanır, kabuk UserDefaults çağırmıyor") yalnız Flutter hattı için geçerliydi. Yükleme sonrası ITMS-91053 e-postası gelirse eksik kategori buraya eklenir; App Privacy cevabı değişmez.
