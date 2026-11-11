# Birdie Smash 1.0: App Store gönderim sırası (2026-11-11)

Her adımın sonunda thread'e yalnız **kalın yazılan kelimeyi** yaz; sıradakini ben hazırlarım.
Ayrıntılı gerekçeler `YAPMAN_GEREKENLER.md`'de; bu dosya yalnız sıra ve tıklama yolu.
Geri alınamayan adımlar (ürün/fiyat oluşturma, incelemeye gönderme) senin elinden geçer; ben göndermem.

Repo tarafı bu PR'la hazır: sürüm adı 1.0.0 (ASC sürüm kaydıyla aynı olmalı), Setup Match iş akışına
"force" girdisi (profil yenileme). Mağaza metinleri, App Privacy cevapları, yaş anketi, 23 başarım görseli,
ekran görüntüleri (6.9" ve 6.5", EN/TR) repoda.

## 1. Yetenekler (5 dk) → **yetenek**

developer.apple.com › Certificates, IDs & Profiles › Identifiers › `com.birdiesmash.app`:
1. **Game Center** kutusunu işaretle.
2. **iCloud** kutusunu işaretle › Configure: yalnız *Key-value storage* yeter (CloudKit kapsayıcısı seçme).
3. Sağ üstte **Save** › çıkan uyarıda Confirm.

Neden: bugünkü build günlüğü "Profil Game Center / iCloud yetkisi taşımıyor" diyor; açıklama başarım vaat ettiği
için bu haliyle inceleme 2.1 ile geri çevirebilir. Sen "yetenek" deyince profili ben yenilerim (Setup Match, force),
sonraki TestFlight build'i iki yetkiyi taşır.

## 2. AdMob (15 dk) → **admob** + kimlikler

1. admob.google.com › Apps › **Add app** › iOS › "Is the app listed on a supported app store?" **No** › ad "Birdie Smash".
2. Uygulama sayfasında **Ad units › Add ad unit** › **Banner** › ad "Menu banner" › Create.
3. Bir tane daha › **Rewarded** › ad "Match end reward" › ödül 1, ad "Feathers" › Create.
4. Thread'e dört şeyi yaz (gizli değiller): uygulama kimliği `ca-app-pub-…~…`, banner birimi `ca-app-pub-…/…`,
   ödüllü birim `ca-app-pub-…/…`, yayıncı kimliği `pub-…` (Settings › Account information).
5. **Privacy & messaging** › **European regulations (GDPR)** mesajı oluştur › uygulamayı seç › Publish.
6. Aynı sayfada **IDFA explainer** (iOS ATT) mesajı oluştur › Publish. İkisi de zorunlu: ATT penceresini bu mesaj açar.

Ben: kimlikleri koda girerim, test reklamı kapanır, `murphysoft.app/app-ads.txt` dosyasını koyarım.
Sonra AdMob › Apps › Birdie Smash › **app-ads.txt** sekmesinde "Check for updates" (ben haber veririm).

## 3. Uygulama bilgileri (10 dk) → **bilgi**

ASC › Apps › Birdie Smash › **General › App Information**:
1. Sağ üst dil menüsü › **Add Language › Turkish** (birincil English kalır).
2. English: Name `Birdie Smash`, Subtitle `Badminton: swipe, slice, smash` (`store_listing/en/subtitle.txt`).
   Turkish: `store_listing/tr/name.txt` ve `subtitle.txt`.
3. Category: Primary **Games › Sports**, Secondary **Games › Arcade**.
4. **Content Rights** › "Does your app contain, show, or access third-party content?" **No**.
5. **Age Rating › Edit**: `store_listing/yas_derecesi.md` tablosu; **Advertising = Yes**, geri kalan None/No → 4+.
6. **Digital Services Act** (aynı sayfa ya da Business): sandık'ta seçtiğin tacir (trader) durumu aynen.
7. Save.

## 4. App Privacy (5 dk) → **gizlilik**

ASC › Birdie Smash › **General › App Privacy**:
1. Privacy Policy URL: `https://murphysoft.app/birdie-smash/privacy/` (Turkish için `store_listing/tr/privacy_url.txt`).
2. Data Types › Get Started › **Yes, we collect data** › işaretle: Identifiers › Device ID; Usage Data › Advertising Data,
   Product Interaction; Diagnostics › Crash Data, Performance Data.
3. Her birinde amaç **Third-Party Advertising**; Linked to you **No**; Device ID ve Advertising Data için
   Used for tracking **Yes**, diğerleri No.
4. Sağ üstte **Publish**.

## 5. Fiyat ve ülkeler (2 dk) → **fiyat**

ASC › **Pricing and Availability**: Price **Free (0)**, Availability **All countries** (ya da istediğin liste). Save.

## 6. Uygulama içi satın almalar (30 dk) → **iap**

Önce ASC › **Business** › Paid Apps sözleşmesi Active mi bak (sandık için imzalı, aynı hesap).
ASC › Birdie Smash › **Monetization › In-App Purchases › +**. Product ID birebir aynı olmalı:

| Tür | Product ID | Reference Name | Fiyat |
|---|---|---|---|
| Consumable | `com.birdiesmash.app.tuy1000` | Tuy 1000 | 0,99 $ |
| Consumable | `com.birdiesmash.app.tuy3500` | Tuy 3500 | 2,99 $ |
| Consumable | `com.birdiesmash.app.tuy8000` | Tuy 8000 | 5,99 $ |
| Consumable | `com.birdiesmash.app.tuy20000` | Tuy 20000 | 11,99 $ |
| Non-Consumable | `com.birdiesmash.app.reklamsiz` | Reklamsiz | 2,99 $ (99,99 ₺) |

Her üründe: Price Schedule › fiyat; **Localization** English + Turkish (ad ve açıklama `YAPMAN_GEREKENLER.md` tablosunda);
**Review Information** › ekran görüntüsü (TestFlight'ta Tüy dükkânının ekran görüntüsü) ve not
"Restore Purchases button is at the bottom of the Feather shop". Family Sharing kapalı kalsın (sonradan kapatılamaz).
Durum "Ready to Submit" olmalı; incelemeye 9. adımda sürümle birlikte gider.

## 7. Game Center (45 dk elle) → **gamecenter**

1. ASC › Birdie Smash › **Services › Game Center** › aç.
2. **Achievements › +**: 23 başarım, ID/puan/metin `YAPMAN_GEREKENLER.md` › Game Center tablosu (11 Kas puan
   düzeltmeleriyle toplam 1000), görseller `store_listing/game_center/<id>.png`, iki dil.
3. Sürüm sayfasında (8. adım) **Game Center** kutusunu işaretle.

## 8. Sürüm sayfası (15 dk) → **surum**

ASC › iOS App › **1.0 Prepare for Submission**:
1. Sürüm numarası alanı **1.0.0** olsun (build'lerin sürüm adı artık 1.0.0; ASC "1.0" yazıyorsa düzelt).
2. Her dil için: Promotional Text, Description, Keywords, Support URL ← `store_listing/<dil>/*.txt`. Marketing URL boş.
3. **Screenshots › iPhone 6.9"**: `store_listing/gorsel/<dil>/6.9/01…05` sırayla (6.5" isteğe bağlı, aynı klasörün 6.5'i).
4. **Build** › + › 1.0.0 sürümlü en yeni build (bu PR birleştikten sonraki ilk TestFlight build'i ya da daha yenisi).
5. Copyright: `2026 <adın soyadın>`.
6. **App Review Information**: ad, telefon, e-posta senin; **Sign-in required işaretsiz**; Notes:
   "Offline single-player game, no account. Ads: AdMob banner in menus and an optional rewarded ad after a match.
   Restore Purchases is at the bottom of the Feather shop."
7. Version Release: **Manually release this version** (onaydan sonra sen yayınlarsın).
8. Save.
9. **Accessibility** (App Information altında): iPhone için yalnız Reduced Motion ve Differentiate Without Color Alone.

## 9. Gönderim (senin onayın) → **gonderdim**

Sürüm sayfası › **Add for Review**. In-App Purchases sayfasında her ürün › **Add for Review** (aynı Draft Submission).
Sonra **Submit for Review**. Gönderimden önce son kontrolü ben yaparım: 1.0.0 build'de Game Center + iCloud yetkisi,
gerçek AdMob kimlikleri, temiz kurulumda ATT penceresi.
