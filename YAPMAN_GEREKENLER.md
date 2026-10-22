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
2. **Achievements › +** ile aşağıdaki 11 başarımı ekle. **Achievement ID
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

Toplam 550 puan (Apple sınırı 1000; ileride eklenecekler için pay bırakıldı).
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
