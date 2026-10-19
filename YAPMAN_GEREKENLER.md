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
   (match branch `birdiesmash`, Harçlıklarım'daki ayrı-branch kararı).
5. **Actions › "iOS — Setup Match (one-time)"** çalıştır.
6. `main`'e ilk birleşme → "iOS — TestFlight" build'i yükler. TestFlight'ta dahili
   gruba kendini ekle; davet linki ASC › TestFlight › harici grup "Public Link".

## Sonra

- Uygulama ikonu (şu an Flutter varsayılanı).
- App Store'da "Birdie Smash" adının boş olup olmadığını ASC uygulama
  oluştururken gör; doluysa "Birdie Smash!" ya da "Smash Birdie" yedek.
- Karakterin oyuna girişi: sprite sheet (2D canvas) mi, three.js mi — karar.
