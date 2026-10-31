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
| bs_efsane | 50 | Legend / Reach level 30. | Efsane / Seviye 30'a ulaş. |
| bs_antrenor | 25 | Coach / Earn all 24 training drill stars. | Antrenör / Antrenman görevlerinin 24 yıldızını topla. |
| bs_gunun_maci | 10 | Match of the Day / Win a Match of the day. | Günün maçı / Bir Günün maçını kazan. |
| bs_usta_6 | 20 | Master Road / Clear 6 stages on the Master road. | Usta yolu / Usta yolunda 6 basamak geç. |
| bs_imza_okur | 15 | Signature Reader / Read 10 signature shots in total. | İmza okuru / Toplam 10 imza vuruşu oku. |

Toplam 1000 puan (Apple sınırı 1000; artık yer yok, yeni başarım için puan kaydırmak gerekir). 2026-10-26 (Unity, karakter adımı): bs_tur_2..4 ve bs_efsane eklendi; kod yalnız Unity build'inde verir. 2026-10-26 (Unity, adım 7–9): bs_antrenor, bs_gunun_maci, bs_usta_6, bs_imza_okur eklendi (15 → 19 başarım); bs_tur_3/4 artık kazanılabilir (Tur 3–4 verisi geldi). Görselleri (`store_listing/game_center/<id>.png`) henüz yok: ekleyince yükle. Tanımlanmamış ID oyunu bozmaz, yalnız Game Center'da görünmez.
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

Hazır dosyalar: `legal/gizlilik.md` + `legal/privacy.md` (kaynak) → `docs/gizlilik.html`
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
4. **Category**: Primary **Games › Sports**; ikinci alt kategori istersen **Arcade**.
5. **Age Ratings › Set Up Age Ratings**: `store_listing/yas_derecesi.md`'deki tabloyu
   adım adım işaretle; son adımda **Not Applicable**, Save. Sonuç **4+** olmalı.

### C. App Privacy

ASC › Birdie Smash › **General › App Privacy**.
1. **Privacy Policy URL** › Edit: English için `privacy.html`, Turkish için
   `gizlilik.html` adresi (A.3; aynı adresler `store_listing/<dil>/privacy_url.txt`'te).
2. **Data Types › Get Started** › "Do you or your third-party partners collect data
   from this app?" → **No, we do not collect data from this app** › Save.
3. Sağ üstte **Publish**. Sayfada "Data Not Collected" görünmeli.
   Gerekçe ve hangi durumda değişeceği: `store_listing/app_privacy.md`.

### D. Sürüm sayfası (iOS App › 1.0 Prepare for Submission)

Her dil için (sağ üst dil menüsü) ilgili klasördeki dosyayı yapıştır:
- **Promotional Text** ← `promotional_text.txt`
- **Description** ← `description.txt`
- **Keywords** ← `keywords.txt` (olduğu gibi; virgülden sonra boşluk ekleme)
- **Support URL** ← açık soru (aşağıda)
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

1. **Destek e-postası ve Support URL.** Support URL ASC'de zorunlu. Seçenekler:
   (a) bir destek e-postası belirle, Claude `docs/destek.html` sayfasını o adresle
   yapsın, URL `https://yasincirali.github.io/birdie-smash/destek.html` olur;
   (b) mevcut bir web sayfan varsa onu ver. E-posta belirlenince gizlilik metninin
   "İletişim" bölümüne de yazılır (yalnız "Sürüm" artar). Şu an metin "App Store'daki
   destek bağlantısı"na yönlendiriyor; adres uydurulmadı.
2. **Copyright satırı** ve App Store'da görünecek **geliştirici adı** (Apple hesabındaki
   ad/şirket adı otomatik gelir).
3. İkinci kategori **Arcade** olsun mu, yalnız Sports mu?
