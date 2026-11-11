# YAPMAN GEREKENLER — Birdie Smash

Yalnız yasin'in yapabileceği adımlar. Sıra önemlidir (Harçlıklarım ile aynı düzen).

## TestFlight'a ilk build için

1. **GitHub deposu**: `yasincirali/birdie-smash` (private) oluştur; Claude Projects ›
   Project settings › Resources'a ekle. İlk commit hazır; depo açılınca push
   edilir (push saatleri dışında). Claude'un GitHub uygulaması depo açamıyor
   (403), bu adım elden.
2. **Apple Developer › Identifiers**: `com.birdiesmash.app` App ID (capability yok).
3. **App Store Connect › Apps › +**: ad "Birdie Smash", bundle id yukarıdaki,
   SKU `birdiesmash`, birincil dil İngilizce (global), kategori Games › Sports.
   TestFlight › Test Information'ı doldur.
4. **GitHub secrets** (yeni depoya): `APPLE_ID`, `APPLE_TEAM_ID`, `ITC_TEAM_ID`,
   `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_CONTENT`, `MATCH_GIT_URL`,
   `MATCH_GIT_BASIC_AUTH` Harçlıklarım ile aynı (ortak sertifika deposu); `MATCH_PASSWORD` için YENİ bir parola
   (match dalı `harcliklarim`, parola Harçlıklarım ile AYNI; Apple sertifika sınırı).
5. **Actions › "iOS — Setup Match (one-time)"** çalıştır.
6. `main`'e ilk birleşme → "iOS — TestFlight" build'i yükler. TestFlight'ta dahili
   gruba kendini ekle; davet linki ASC › TestFlight › harici grup "Public Link".

## Tüy satın alma ve iCloud kaydı (2026-11-01, yasin: "paywall ile tüy aldırma ya da IAP altyapısı"; karar: önce iCloud)

Kod hazır: dört Tüy paketi (Unity IAP, StoreKit), işlem defteri, iCloud anahtar-değer yedeği. Mağazada ürün olmadıkça dükkân
"şu an satın alınamıyor" der, fiyat uydurmaz; aşağıdakiler bitince kendiliğinden açılır.

1. **developer.apple.com › Identifiers › com.birdiesmash.app**: *iCloud* yeteneğini aç (yalnız *Key-value storage* yeter,
   CloudKit kapsayıcısı gerekmez). Kaydet. Sonra match profilini yenile: Actions'ta iOS build'i bir kez koş ya da
   `fastlane match appstore --force` (profil yeni yetkiyi taşımazsa Fastfile iCloud yetkisini eklemez, build yine geçer).
2. **App Store Connect › Agreements, Tax, and Banking**: *Paid Apps* sözleşmesi etkin olmalı (sandık için imzalıysa aynı hesap).
3. **App Store Connect › Birdie Smash › Monetization › In-App Purchases**: dört **Consumable** ürün, kimlikler birebir:

   | Product ID | Ad (TR / EN) | Önerilen fiyat |
   |---|---|---|
   | `com.birdiesmash.app.tuy1000` | Bir avuç Tüy / A handful of Feathers | 0,99 $ |
   | `com.birdiesmash.app.tuy3500` | Tüy kesesi / Feather pouch | 2,99 $ |
   | `com.birdiesmash.app.tuy8000` | Tüy sandığı / Feather chest | 5,99 $ |
   | `com.birdiesmash.app.tuy20000` | Tüy hazinesi / Feather hoard | 11,99 $ |

   Her üründe inceleme ekran görüntüsü olarak TF'deki Tüy dükkânı sayfası yeter. Ürünleri bir sonraki sürüme iliştir.
   Ayrıca bir **Non-Consumable** ürün (2026-11-08, kod hazır; ürün yokken dükkânda "Satın alınamıyor" yazar):

   | Product ID | Ad (TR / EN) | Önerilen fiyat |
   |---|---|---|
   | `com.birdiesmash.app.reklamsiz` | Reklamsız / Ad-free | 2,99 $ (99,99 ₺) |

   Açıklama: "Menüdeki reklam şeridi kalkar. Ödüllü reklam yalnız sen istersen açılır. Tüy vermez." İnceleme notuna:
   "Restore Purchases düğmesi Tüy dükkânının altında". Family Sharing açmak isteğe bağlı (sonradan kapatılamaz).
   **Ürün sayfasında tanıt (isteğe bağlı, 2026-11-10 inceleme):** Reklamsız ürünü oluşunca ASC › In-App Purchases ›
   Reklamsız › "Promote in App Store" aç. Görünen ad "Reklamsız / Ad-free", açıklama (≤45 karakter)
   TR "Menüdeki reklam şeridi kalkar, Tüy vermez" / EN "Removes the menu ad strip. No Feathers.".
   1024×1024 tanıtım görseli ister (ekran görüntüsü değil, yazısız oyun görseli; hazır olunca store_listing/gorsel/ altına).
   Kodda iş yok: Unity IAP ürün sayfasından başlayan alımı uygulama açılınca ProcessPurchase ile işler, şerit kapanır.
   TF sandbox'ta `itms-services://?action=purchaseIntent&bundleId=com.birdiesmash.app&productIdentifier=com.birdiesmash.app.reklamsiz` ile dene.
4. **Sandbox testi**: Ayarlar › App Store › Sandbox hesabı ile TF build'inde üst çubuktaki Tüy sayacına dokun → paket al →
   Tüy cüzdana eklenmeli. Uygulamayı silip yeniden kur → iCloud açıksa ilerleme ve aldığın Tüy geri gelmeli.

## Sonra

- Uygulama ikonu (şu an Flutter varsayılanı).
- App Store'da "Birdie Smash" adının boş olup olmadığını ASC uygulama
  oluştururken gör; doluysa "Birdie Smash!" ya da "Smash Birdie" yedek.
- Karakterin oyuna girişi: sprite sheet (2D canvas) mi, three.js mi — karar.

## Unity build'i TestFlight'a (2026-10-24, yasin: "Unity ile değiştirelim menüleri ve tüm oyunu")

Hat hazır (`ios-testflight.yml`): üç Unity secret'ı girilince TestFlight build'i kendiliğinden
Unity projesinden (`unity/BirdieSmash`) çıkar. Secret'lar yokken eski Flutter + WebView yolu
aynen çalışır; Actions özetinde "Unity secret'ları eksik" notu görünür. Geri dönmek istersen
`UNITY_LICENSE` secret'ını sil, hat Flutter'a döner.

0. **Önce ilerleme devri (2026-10-25):** secret'ları girmeden ÖNCE ilerleme devrini taşıyan HTML build'i
   (25 Eki, "Unity sürümüne hazırlık" notlu) TestFlight'a çıksın; sen ve testçiler onu **bir kez açın**
   (menüde birkaç saniye). O build kariyer/Tüy/ekipman/ayarları iOS kullanıcı ayarlarına da yazar; Unity ilk
   açılışta bir kez okur. Açılmadan Unity'ye geçilirse ilerleme taşınmaz. Unity açıldıktan sonra eski HTML
   build'ine dönülse de Unity ilerlemesinin üstüne yazılmaz (bayrak `tuytop_html_devir`).
   Not: geri dönüş (`UNITY_LICENSE`'ı silmek) Unity'de kazanılan ilerlemeyi HTML'e geri taşımaz.

1. **Unity Hub'ı kur** (unity.com/download), Unity hesabınla giriş yap.
2. **Installs › Install Editor › Archive** (ya da "download archive" sayfası) üzerinden tam olarak
   **6000.0.58f2** sürümünü kur; modüllerden **iOS Build Support**'u işaretle. (Başka 6000.0.x
   da proje açar ama CI bu sürümle derler; aynısı olsun.)
3. **Lisans**: Unity Hub › sağ üstte hesap simgesi › **Manage licenses** › **Add** ›
   **Get a free personal license**. Hub lisansı bilgisayara bir dosya olarak yazar:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf` (ProgramData gizli klasör; adres
     çubuğuna yazarak git)
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf` (Finder › Git › Klasöre Git;
     baştaki `/` önemli, kullanıcı klasöründeki `~/Library` değil)
4. **GitHub › yasincirali/birdie-smash › Settings › Secrets and variables › Actions ›
   New repository secret** ile üç secret ekle:
   - `UNITY_LICENSE`: `Unity_lic.ulf` dosyasını Not Defteri / TextEdit ile aç, **tüm içeriği**
     (`<?xml` ile başlayıp `</root>` ile biten) kopyala, yapıştır.
   - `UNITY_EMAIL`: Unity hesabının e-postası.
   - `UNITY_PASSWORD`: Unity hesabının parolası.
   Bu değerleri sohbete, commit'e ya da bir dosyaya yazma; yalnız GitHub secret alanına.
5. **Birleştirmeden önce dene** (önerilir): Actions › "iOS — TestFlight" › **Run workflow** ›
   dal `unity-demo` seç › Run. ~30–60 dk sürer (ilk build Library önbelleği olmadan en uzunu).
   Yeşil biterse TestFlight'ta "Birdie Smash" altında yeni build görünür; notlar `=== UNITY ===`
   bölümünden gelir. Kırmızıysa Claude'a "Unity build kırıldı" de, log'a bakar.
6. Build iyiyse PR'ı `main`'e birleştir; bundan sonra her `main` push'u Unity build'i yükler.

Notlar:
- **Unity Personal sınırı**: son 12 ayda toplam gelir + alınan yatırım **200.000 USD**'yi
  geçmemeli (Unity 6 koşulu). Geçerse Unity Pro gerekir; o zaman CI'ya Pro seri numarası
  (`UNITY_SERIAL`) eklenir. Unity 6 Personal'da açılış logosu zorunlu değil.
- Lisans dosyası zamanla yenilenebilir; CI "license" hatasıyla kırılırsa Hub'da lisansı
  yenile ve 3–4. adımı tekrarla.
- Unity build'inde Game Center kodu var (Ayarlar/Profil › Game Center · Başarımlar, `Basarim`);
  ama yetki yalnız provizyon profili Game Center taşıyorsa build'e girer (Fastfile sessizce atlar).
  Mağaza açıklaması başarım vaat ettiği için aşağıdaki Game Center adımları gönderimden önce şart
  (Apple kuralı 2023-08-16: Game Center sunan uygulama yetkiyi taşır ve ASC'de kurulu olur).
  HTML sürümündeki ilerleme kabuk devriyle taşınır (`Devir.cs`). (2026-10-31 düzeltme; eskisi: "Game Center yok".)
- Bilgisayarda açmak için: Unity Hub › Add › `unity/BirdieSmash` klasörü (README orada).

## Game Center başarımları (2026-10-23, Ar-Ge önerisi, yasin "yap")

Kod hazır (kabuk `games_services`, oyun `tuytop_basarim`). Başarımlar
App Store Connect'te tanımlanmadan Game Center'da görünmez; oyun yine de
yerelde sayar ve tanım yapılınca bir sonraki girişte hepsini gönderir.
Sıralama tablosu (leaderboard) bilerek yok.

0. **Developer › Identifiers › com.birdiesmash.app › Capabilities › Game Center**
   işaretle, Save. (Şu anki App Store profili bu yeteneği içermiyor; 23 Ekim
   build'i bu yüzden arşivde düştü, Game Center yetkisi geri alındı.) Sonra
   Claude'a "profili yenile" de: match profili zorla yenilenir ve
   `CODE_SIGN_ENTITLEMENTS = Runner/Runner.entitlements` Runner'ın üç
   yapılandırmasına geri eklenir. O build'e kadar oyun başarımları yalnız
   yerelde sayar, kayıp olmaz.
1. **ASC › Birdie Smash › Services (ya da Features) › Game Center**: aç.
   Sürüm sayfasında ("App Store" sekmesi) Game Center kutusunu işaretle.
2. **Achievements › +** ile aşağıdaki 19 başarımı ekle. **Achievement ID
   birebir aynı olmalı** (oyun bu kimlikle gönderir). Hidden: No,
   Achievable More Than Once: No. Görseller `store_listing/game_center/<id>.png`
   (1024×1024). Her birine iki dil ekle: English (birincil) ve Turkish.

| ID | Puan | EN başlık / açıklama | TR başlık / açıklama |
|---|---|---|---|
| bs_kariyer_1 | 20 | Local Hall / Win the local hall stage in Career. | Mahalle salonu / Kariyerde mahalle salonu basamağını kazan. |
| bs_kariyer_2 | 30 | School Tournament / Win the school tournament stage in Career. | Okul turnuvası / Kariyerde okul turnuvası basamağını kazan. |
| bs_kariyer_3 | 40 | Provincial Champion / Win the provincial championship stage in Career. | İl şampiyonu / Kariyerde il şampiyonası basamağını kazan. |
| bs_kariyer_4 | 60 | Regional Final / Win the regional final stage in Career. | Bölge finali / Kariyerde bölge finali basamağını kazan. |
| bs_kariyer_5 | 80 | National League / Win the national league stage in Career. | Ulusal lig / Kariyerde ulusal lig basamağını kazan. |
| bs_kariyer_6 | 150 | Champion / Win the Champions Cup and finish Career. | Şampiyon / Şampiyonlar kupasını kazanıp kariyeri bitir. |
| bs_antrenman | 10 | Coached / Finish the tutorial without skipping it. | Antrenmanlı / Antrenmanı atlamadan bitir. |
| bs_akis | 40 | In the Flow / Reach Flow with perfect hits in a row. | Akışta / Üst üste tam orta vurup Akış'a gir. |
| bs_ralli_20 | 50 | Marathon Rally / Play a rally of 20 shots. | Maraton ralli / 20 vuruşluk bir ralli oyna. |
| bs_sicrayan_smac | 30 | Sky Smash / Win a point with a jump smash. | Gökten smaç / Sıçrayarak smaçla sayı kazan. |
| bs_uc_gorev | 40 | Full Card / Complete all 3 missions in one match. | Tam kart / Bir maçta 3 görevin hepsini bitir. |
| bs_tur_2 | 80 | Conqueror of Asia / Win the Asia Tour final. | Asya fatihi / Asya Turu finalini kazan. |
| bs_tur_3 | 100 | European Champion / Win the Europe Tour final. | Avrupa şampiyonu / Avrupa Turu finalini kazan. |
| bs_tur_4 | 150 | World Champion / Win the World Finals. | Dünya şampiyonu / Dünya Finalleri'ni kazan. |
| bs_tur_5 | 30 | Pacific Champion / Win the Pacific Tour final. | Pasifik şampiyonu / Pasifik Turu finalini kazan. |
| bs_tur_6 | 30 | Champion of the Americas / Win the Americas Tour final. | Amerika şampiyonu / Amerika Turu finalini kazan. |
| bs_tur_7 | 40 | Grand Cup / Win the Grand Cup final. | Büyük Kupa / Büyük Kupa finalini kazan. |
| bs_tur_8 | 60 | Legend of Legends / Win the Legends Cup and finish Career. | Efsanelerin efsanesi / Efsaneler Kupası'nı kazanıp kariyeri bitir. |
| bs_efsane | 50 | Legend / Reach level 30. | Efsane / Seviye 30'a ulaş. |
| bs_antrenor | 25 | Coach / Earn all 24 training drill stars. | Antrenör / Antrenman görevlerinin 24 yıldızını topla. |
| bs_gunun_maci | 10 | Match of the Day / Win a Match of the day. | Günün maçı / Bir Günün maçını kazan. |
| bs_usta_6 | 20 | Master Road / Clear 6 stages on the Master road. | Usta yolu / Usta yolunda 6 basamak geç. |
| bs_imza_okur | 15 | Signature Reader / Read 10 signature shots in total. | İmza okuru / Toplam 10 imza vuruşu oku. |

**2026-11-11 kariyer 8 tur (senin kararın "Şimdi 8 tur"): dört yeni başarım, 19 → 23.** Apple sınırı 1000 puan dolu olduğu için
yukarıdaki tabloda şu beş satırın puanını **düşür**, sonra bs_tur_5..8'i ekle (toplam yine 1000):
bs_kariyer_6 150 → **100**, bs_tur_4 150 → **100**, bs_tur_3 100 → **80**, bs_tur_2 80 → **60**, bs_kariyer_5 80 → **60**.
bs_kariyer_6 açıklamasından "and finish Career / kariyeri bitir" kısmını sil (kariyer artık Efsaneler Kupası'nda bitiyor).
Görseller hazır: `store_listing/game_center/bs_tur_5.png` … `bs_tur_8.png`. Yayınlanmış bir başarımın puanı ASC'de
değiştirilebilir; başarımlar henüz canlıya çıkmadıysa sıra önemli değil.

Toplam 1000 puan (Apple sınırı 1000; artık yer yok, yeni başarım için puan kaydırmak gerekir). 2026-10-26 (Unity, karakter adımı): bs_tur_2..4 ve bs_efsane eklendi; kod yalnız Unity build'inde verir. 2026-10-26 (Unity, adım 7–9): bs_antrenor, bs_gunun_maci, bs_usta_6, bs_imza_okur eklendi (15 → 19 başarım); bs_tur_3/4 artık kazanılabilir (Tur 3–4 verisi geldi). Görselleri 2026-11-08'de eklendi (19/19 hazır; üreteç `tool/gorsel/game_center_rozet.py`). Tanımlanmamış ID oyunu bozmaz, yalnız Game Center'da görünmez.
Açıklamada ASC iki alan ister (kazanılmadan / kazanılınca); ikisine de aynı
cümleyi yazabilirsin. Kaydettikten sonra başarımlar TestFlight build'inde
hemen çalışır; mağaza sürümünde ilk incelemeyle birlikte yayına çıkar.

Deneme: TestFlight build'inde kariyerin ilk basamağını kazan; maç sonunda
Game Center giriş penceresi bir kez gelir. Giriş yapınca Ayarlar'da
"Başarımlar" düğmesi çıkar.

## Karakter/animasyon araçları (2026-10-20, yasin: "gerekli kurulmamış tüm toolları kuralım")

Bulut tarafı hazır: `3d-animator` ve `birdie-karakter` skill'leri repoda, Mesh2Motion
hareket kütüphanesi `/mnt/project-files/birdie-smash/AnimationLibrary/` altında,
`tool/bulut_kurulum.sh` bpy + kütüphaneyi kurar. **Project settings › Cloud
environment › Setup script** alanına `bash tool/bulut_kurulum.sh || true` yaz.

PC tarafı (Blender'ı Claude'un canlı sürmesi için, ~5 dk):
1. Blender 4.2+ kurulu olsun (blender.org).
2. PowerShell: `irm https://www.mcp-for-blender.com/install.ps1 | iex`
   (uv'yi kurar, Claude Code'a `blender` MCP'yi ekler, Blender eklentisini kurar).
   Elle: `winget install astral-sh.uv` → `uvx mcp-for-blender install-addon`.
   Repodaki `.mcp.json` zaten `uvx mcp-for-blender` satırını taşır.
3. Blender → Edit › Preferences › Add-ons → "Interface: MCP for Blender" işaretli.
   Viewport'ta `N` → "MCP for Blender" sekmesi → Start MCP Server.
4. Claude Code'u kapat-aç; `/mcp` listesinde `blender` Connected görünmeli.
5. Mixamo: adobe hesabıyla https://www.mixamo.com'a giriş yap; klipleri elle
   indir (FBX, With Skin, 30 fps). Toplu/script indirme kural dışı.

## Mağaza gönderimi, parça 1: gizlilik, App Privacy, yaş, metinler (2026-10-24, Ar-Ge raporu "yap")

> 2026-11-08: bu bölümün A adımı **geçersiz**. Gizlilik metninin tek kaynağı murphysoft deposu
> (https://murphysoft.app/birdie-smash/privacy/ ve /gizlilik/, Version 2 · 7 Kasım; reklam + satın alma + iCloud).
> Bu depodaki eski kopyalar (`legal/gizlilik.md`, `legal/privacy.md`, `docs/`) "reklam yok, satın alma yok" dediği için
> silindi; github.io adresleri kullanılmaz (404). App Privacy cevapları: `store_listing/app_privacy.md` (1.0 reklamlı satırlar).

Eski: Hazır dosyalar: `legal/gizlilik.md` + `legal/privacy.md` (kaynak) → `docs/gizlilik.html`
+ `docs/privacy.html` (`python3 docs/_build_legal.py` üretir; HTML'i elle düzenleme),
`store_listing/app_privacy.md`, `store_listing/yas_derecesi.md`,
`store_listing/en/*.txt` ve `store_listing/tr/*.txt`. Ekran görüntüleri parça 2'de
(karakter kararından sonra).

### A. Gizlilik sayfasını yayınla (GitHub Pages)

1. Bu dosyalar `main`'e girsin (PR + birleşme, push saatleri dışında). Yalnız
   `docs/`, `store_listing/` ve `.md` değiştiği için TestFlight build'i tetiklenmez
   (`ios-testflight.yml` paths-ignore).
2. GitHub › `yasincirali/birdie-smash` › **Settings › Pages** › Build and deployment:
   Source **Deploy from a branch**, Branch **main**, klasör **/docs** › **Save**.
   (Depo public olduğu için ücretsiz plan yeter.)
3. 1–2 dakika sonra telefonda aç ve kontrol et:
   - İngilizce: **https://yasincirali.github.io/birdie-smash/privacy.html**
   - Türkçe: **https://yasincirali.github.io/birdie-smash/gizlilik.html**
4. Açılmıyorsa Settings › Pages'de yeşil "Your site is live" yazısını bekle.

### B. Uygulama bilgileri (App Information)

ASC › Apps › Birdie Smash › **General › App Information**.
1. Sağ üstteki dil menüsünden **Add Language › Turkish** ekle (birincil dil English kalır).
2. English seçiliyken: **Name** = `store_listing/en/name.txt`, **Subtitle** = `store_listing/en/subtitle.txt`.
3. Turkish seçiliyken: **Name** = `store_listing/tr/name.txt`, **Subtitle** = `store_listing/tr/subtitle.txt`.
4. **Category**: Primary **Games › Sports**, ikinci alt kategori **Games › Arcade** (senin kararın 2026-11-07).
5. **Age Ratings › Set Up Age Ratings**: `store_listing/yas_derecesi.md`'deki tabloyu
   adım adım işaretle; son adımda **Not Applicable**, Save. Sonuç **4+** olmalı.

### C. App Privacy

ASC › Birdie Smash › **General › App Privacy**. (2026-11-10 düzeltme: eski adımlar github.io adresini ve
"veri toplanmıyor" cevabını yazıyordu; 1.0 reklamlı olduğu için ikisi de yanlıştı.)
1. **Privacy Policy URL** › Edit: English için `store_listing/en/privacy_url.txt`, Turkish için
   `store_listing/tr/privacy_url.txt` içindeki murphysoft.app adresi.
2. **Data Types › Get Started** › "Do you or your third-party partners collect data
   from this app?" → **Yes, we collect data**. İşaretlenecek satırlar ve her birinin amacı:
   `store_listing/app_privacy.md` başındaki 2026-11-07 notu (Device ID, Advertising Data,
   Product Interaction, Performance/Crash; amaç Third-Party Advertising; Not Linked to You).
3. Sağ üstte **Publish**.

### D. Sürüm sayfası (iOS App › 1.0 Prepare for Submission)

Her dil için (sağ üst dil menüsü) ilgili klasördeki dosyayı yapıştır:
- **Promotional Text** ← `promotional_text.txt`
- **Description** ← `description.txt`
- **Keywords** ← `keywords.txt` (olduğu gibi; virgülden sonra boşluk ekleme)
- 11 Kas: açıklama "48 rakip" yerine "48 maç, altı oyun tarzı", tanıtım metni "Şampiyonlar kupası" yerine "Efsaneler Kupası" (oyunda altı adlı rakip var, öyle bir kupa yok). Daha önce yapıştırdıysan iki dosyayı iki dilde yeniden yapıştır.
- **Support URL** ← `support_url.txt` (en: murphysoft.app/birdie-smash/support/, tr: .../destek/; sayfa yayında). Destek e-postası support@murphysoft.app.
- **Marketing URL** boş kalabilir. "What's New" ilk sürümde sorulmaz.
- **Copyright**: örn. `2026 <ad soyad ya da şirket adı>` (senin kararın).
- **App Review Information**: iletişim adı/telefon/e-posta senin; **Sign-in required** işaretsiz (hesap yok).
- Fiyat: **Pricing and Availability › Free**.
- Ekran görüntüleri: parça 2.

### E. Erişilebilirlik etiketleri (2026-10-30, Ar-Ge #2)

- [ ] App Store Connect › Birdie Smash › **Accessibility** › iPhone: yalnız **Reduced Motion** ve
  **Differentiate Without Color Alone** işaretle, diğerleri boş kalsın. Gerekçe ve kanıt:
  `store_listing/erisilebilirlik.md`.

### Açık sorular (senin kararın)

1. ~~Destek e-postası ve Support URL~~ Karar (2026-11-08, stüdyo kuralı): support@murphysoft.app ve murphysoft.app/birdie-smash/support/ (yukarıda, `support_url.txt`).
2. **Copyright satırı** ve App Store'da görünecek **geliştirici adı** (Apple hesabındaki
   ad/şirket adı otomatik gelir).
3. ~~İkinci kategori Arcade olsun mu?~~ Karar (2026-11-07): Sports + Arcade (yukarıda B.4).

## Reklam alanı (2026-11-06)

Menüde sekme çubuğunun üstünde 50 pt'lik "Reklam" şeridi yer tutucu olarak duruyor
(`unity/.../Game/Reklam.cs`). Maç üstü şerit 2026-11-07'de kaldırıldı (senin kararın: "Kort kenarı panolar");
maçta reklam yeri kortun yan ve arka LED panoları (şimdilik oyunun kendi yazıları). Panoları gerçek reklama
çevirmek oyun içi reklam ağı (Anzu, Gadsme vb.) ister; o da gizlilik politikası, ATT ve App Privacy güncellemesi demek. Mağazaya 1.0 gönderilmeden önce ikisinden biri:
1. Reklam SDK'sı seçilip bağlanır (AdMob / AppLovin vb.). O zaman gizlilik politikası,
   ATT izin metni ve App Store "App Privacy" etiketi de güncellenir (reklam = veri toplar).
2. Ya da `Reklam.ALAN = false` yapılır; şerit kalkar, oyun eski yerleşime döner.
Boş "Reklam" kutusuyla mağazaya gitmek inceleme reddi riskidir (2.1 eksik işlev).
**Karar (2026-11-07, sen: "şerit en başta bağlı olarak çıksın"): AdMob bağlandı.** Menü şeridi + maç sonu isteğe bağlı
ödüllü reklam (+%50 Tüy, günde en çok 1, ilk 3 galibiyette yok). Şimdilik Google'ın **test** kimlikleri: TestFlight'ta "Test Ad"
yazılı reklam görürsün, gerçek para yok. Mağaza 1.0'dan önce senden:
1. admob.google.com'da hesap aç (aynı Google hesabın olabilir), **Uygulama ekle › iOS › Birdie Smash** (yayında değilse
   "listelenmemiş" seç). Çıkan **Uygulama kimliği** (`ca-app-pub-…~…`) ve iki **reklam birimi**: "Banner" ve "Ödüllü".
   Üç kimliği thread'e yaz (gizli değiller); ben koda girerim (`Game/ReklamSdk.cs`, `Editor/CiBuild.cs`).
2. AdMob › **Gizlilik ve mesajlar**: AB/BK rıza mesajını (GDPR) **ve** iOS "IDFA açıklaması" mesajını yayınla; ikisi de
   **zorunlu** (2026-11-10 inceleme). ATT penceresini kodumuz değil bu mesaj açar; oysa paket izin metnini
   (`NSUserTrackingUsageDescription`, `Editor/CiBuild.cs`) her build'de taşıyor ve App Privacy "Used to Track = Yes" diyor.
   Mesaj yayında değilse inceleme cihazında pencere çıkmaz ve App Review 2.1/5.1.2 ile geri çevirir.
   Gönderimden önce kontrol: TestFlight'ta temiz kurulum, ilk maçtan sonra ATT penceresi çıktı mı?
   (Kişiselleştirilmemiş reklamla çıkmak istersen söyle: izin metni build'den kalkar, etiket "Used to Track = No" olur.)
3. AdMob › Uygulama ayarları › **app-ads.txt**: `murphysoft.app/app-ads.txt` (dosyayı ben koyarım, yayıncı kimliğin lazım).
4. App Store Connect › **App Privacy**: "Data Not Collected" artık doğru değil. `store_listing/app_privacy.md` reklam dalı:
   Identifiers (Device ID), Usage Data (Advertising Data, Product Interaction), Diagnostics; amaç Third-Party Advertising;
   ATT izni verilirse "Used to Track You". Age Rating › Advertising = **Yes**.
5. Gizlilik politikası reklam bölümüyle güncellendi (murphysoft.app/birdie-smash/privacy/, site yayına girince).

Hangisi seçilirse seçilsin App Store Connect › Age Rating formunda "Advertising" cevabını
`store_listing/yas_derecesi.md`'deki dala göre ver (SDK = Yes, ALAN=false = No) ve App Privacy'yi
`store_listing/app_privacy.md` "reklam" dalına göre güncelle (2026-11-07 inceleme).

## Online 1v1 (2026-11-07, yasin: "İlk offline başlatıp online'a 2. fazda dönsek")
1.0 mağazaya **offline** çıkar. Online çekirdek (uzak maç, lig, oyuncu kartı, eşit güç) kodda
hazır ama kapalı; 1.0'da oyuncu online iz görmez. Aşağıdakiler 1.0'ı **bekletmez**, 2. faz
(yayından sonraki güncelleme) için gerekir:
1. Unity Cloud'da Unity Gaming Services projesi aç (Authentication, Relay, Matchmaker, Lobby,
   Leaderboards, Cloud Code). Proje kimliğini thread'e yaz; anahtar/secret repoya yazılmaz.
2. Online oyuncu adı kararı: hazır ad ("Hızlı Martı 27", döngünün önerisi) ya da serbest ad
   (küfür süzgeci + Bildir/Engelle + sunucu gerekir, App Store 1.2).
3. Dereceli maç uzunluğu: 11 sayı tek oyun (öneri) ya da 21.
Rapor: https://claude.ai/artifact/V2uZN5PBYvFSP3GC3RAynf
