# App Store Connect › App Privacy cevapları — Birdie Smash

> **2026-11-07: AdMob bağlandı (Game/ReklamSdk.cs).** Aşağıdaki "Data Not Collected" reklamsız sürüm içindir. Reklamlı
> 1.0 için cevap **Yes, we collect data**: Identifiers › Device ID (yalnız ATT izni verilirse IDFA), Usage Data › Advertising
> Data + Product Interaction, Diagnostics › Performance/Crash (Google SDK); hepsi amaç **Third-Party Advertising**,
> kimliğe bağlı değil (Not Linked to You); Device ID ve Advertising Data için "Used to Track You" = Yes (ATT izniyle).
> Kaynak: Google'ın "AdMob App Store data disclosure" sayfası; SDK sürümü değişince yeniden bakılır.

Tarih: 2026-10-24 · Kod: `main` @ 9e781a2 · Sonuç (reklamsız sürüm): **Data Not Collected**. 2026-11-08: reklamlı 1.0 için satırlar aşağıda güncellendi (Unity oyunu, AdMob, uygulama içi satın alma, iCloud); 1.0 cevabı **Yes, we collect data** (üstteki not).

## Sorulacak ilk soru

**"Do you or your third-party partners collect data from this app?"** → **No, we do not collect data from this app.**

Apple'ın tanımı: "collect" = veriyi cihazdan çıkarıp senin ya da ortaklarının, isteği o anda karşılamaktan daha uzun süre okuyabileceği biçimde saklaması. Yalnız cihazda işlenen veri "collected" sayılmaz. Apple'ın kendi topladığı veriyi (Game Center, App Store istatistikleri, Apple'ın çökme raporları) geliştirici bildirmez ("You are not responsible for disclosing data collected by Apple").

## Neden "No" (doğrulama, 2026-10-24)

- **Oyun** (`assets/game/index.html`): içinde tek bir `http://` / `https://` adresi yok. `fetch(` çağrıları yalnız dosyaya gömülü `data:image/...;base64` sprite'larını açıyor (oyuncu, rakip, hakem, ekipman görselleri). `XMLHttpRequest`, `WebSocket`, `sendBeacon`, çerez, analitik yok (`gtag` eşleşmeleri base64 verisinin içindeki rastgele harfler).
- **Kayıt**: yalnız `localStorage`, anahtarlar `tuytop_v2`, `tuytop_ekonomi`, `tuytop_gear`, `tuytop_basarim`, `tuytop_antrenman`, `tuytop_mod`, `tuytop_saha`, `tuytop_len`, `tuytop_raket_gor`. Cihazdan çıkmaz.
- **Oyun** (Unity, `unity/BirdieSmash`; 2026-10-25'ten beri Flutter kabuğu yerine): kendi kodumuz ağ isteği atmaz, analitik/çökme raporu paketi yok. Ağa çıkan üç parça: Google Mobile Ads SDK (`Game/ReklamSdk.cs`), StoreKit üzerinden satın alma (`Game/Magaza.cs`), iCloud anahtar-değer kaydı (`Game/Bulut.cs`, oyuncunun kendi iCloud'u, bize gelmez).
- **Game Center**: oyun yalnız başarım kimliklerini (`bs_kariyer_1` vb.) Apple'ın hizmetine gönderir; oyuncu kimliğini okumaz/saklamaz, geliştiriciye ulaşan bir şey yok. Apple'ın topladığı veri → bildirilmez.
- **İzinler**: Info.plist'te hiçbir `NS...UsageDescription` yok (kamera, mikrofon, konum, rehber, fotoğraf istenmiyor). Bildirim yok.
- **Reklam / izleme**: 1.0'da Google AdMob (menü şeridi + ödüllü reklam). IDFA yalnız App Tracking Transparency izni verilirse Google'a gider; izin yoksa kişiselleştirilmemiş reklam. Veri Google'a gider, bize gelmez; App Privacy'de "Third-Party Advertising" amaçlı bildirilir.
  "Used to Track = Yes" ancak ATT penceresi gerçekten çıkıyorsa doğru: AdMob "IDFA açıklaması" mesajı yayında olmalı (YAPMAN_GEREKENLER AdMob 2. adım, 2026-11-10).
- **Satın alma**: StoreKit ile "Reklamsız" ve Tüy paketleri. Ödeme ve satın alma geçmişi Apple'da; kendi sunucumuz yok, makbuz yalnız cihazda doğrulanır → bizim için "toplanmaz".

## Veri türü başına gerekçe

| Veri türü (ASC adı) | Cevap | Neden |
|---|---|---|
| Contact Info (Name, Email, Phone, Physical Address, Other) | Toplanmaz | Hesap ya da form yok; hiçbir metin girişi yok. |
| Health & Fitness | Toplanmaz | HealthKit yok; "nefes" yalnız oyun içi değer. |
| Financial Info (Payment, Credit, Other) | Toplanmaz | Ödeme Apple'da (StoreKit); kart bilgisi bize gelmez. |
| Location (Precise, Coarse) | Toplanmaz | Konum izni ve API'si yok. |
| Sensitive Info | Toplanmaz | — |
| Contacts | Toplanmaz | Rehber izni yok. |
| User Content (Emails/Texts, Photos/Videos, Audio, Gameplay Content, Customer Support, Other) | Toplanmaz | Gameplay Content (skor, ilerleme, ekipman) yalnız cihazda `localStorage`'da. Başarım durumu Apple'ın Game Center'ına gider → Apple'ın verisi, bildirilmez. Mikrofon yok. |
| Browsing History | Toplanmaz | WebView yalnız yerel dosyayı açar; dış sayfa yok. |
| Search History | Toplanmaz | Arama yok. |
| Identifiers (User ID, Device ID) | **1.0: Device ID toplanır** (Third-Party Advertising, Not Linked, Used to Track = Yes) | Kendi kullanıcı kimliğimiz yok; Game Center kimliği okunmuyor. IDFA yalnız ATT izniyle Google Mobile Ads SDK'ya gider. Reklamsız sürümde: toplanmaz. |
| Purchases (Purchase History) | Toplanmaz | Satın alma StoreKit ile Apple'da; geçmiş bize ya da üçüncü tarafa gitmez. |
| Usage Data (Product Interaction, Advertising Data, Other) | **1.0: Advertising Data + Product Interaction toplanır** (Third-Party Advertising, Not Linked; Advertising Data Used to Track = Yes) | Kendi analitiğimiz yok; Google Mobile Ads SDK reklam gösterimi/tıklaması toplar. |
| Diagnostics (Crash, Performance, Other) | **1.0: Performance + Crash toplanır** (Third-Party Advertising, Not Linked) | Kendi çökme raporumuz yok; Google SDK kendi tanılama verisini toplar. Apple'ın çökme raporları Apple'ın verisi, bildirilmez. |
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
