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
