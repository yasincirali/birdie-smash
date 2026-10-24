# Claude Development Guidelines — Birdie Smash

Bağımsız badminton mobil oyunu (çalışma adı "Tüy Top" idi; yasin 2026-10-19:
ad global olsun, sandık ile hiçbir ilişkisi yok: ayrı marka, bundle
`com.birdiesmash.app`, ayrı mağaza hattı). Çalışma biçimi: değişiklikleri
onay beklemeden yap, sonunda Türkçe özetle; push saatleri kuralı (hafta içi
09:00–18:30 TR push/PR/merge yok) burada da geçerli.

- **Oyun tek dosyadır:** `assets/game/index.html` (HTML + canvas + Web Audio).
  Prototip ve tasarım turları `https://claude.ai/artifact/4e8aG15tCWDe8hdtVCPryQ`
  üzerinde yürür; her yayınlanan sürüm bu dosyaya kopyalanır. Oyun mantığı
  Dart'a taşınmaz. localStorage anahtarları (`tuytop_*`) eski adla kalır ki
  oyuncu ayarı sıfırlanmasın.
- **Flutter kabuğu üç iş yapar:** mağaza/TestFlight hattı, tam ekran dikey
  WebView, `Haptic` JS kanalı → `HapticFeedback`. Oyun `buzz(desen)` ile
  titreşim ister; kanal yoksa `navigator.vibrate`'e düşer (tarayıcı).
- **Ses ve his oyunun içindedir:** raket sesi tel gerilimine ve vuruş türüne
  göre sentezlenir, kabuk yorumlamaz; süre/desen oyundan gelir.
- TestFlight: `main`'e birleşme → `ios-testflight.yml` (Harçlıklarım'dan
  türetildi; match dalı ve `MATCH_PASSWORD` Harçlıklarım ile ortak (Apple sertifika sınırı, 2026-10-19)). Android hattı yok.
- **Unity geçişi (2026-10-24, yasin: "Unity ile değiştirelim menüleri ve tüm oyunu"):**
  oyun + menüler `unity/BirdieSmash` (Unity 6000.0.58f2). `ios-testflight.yml` kapısı
  UNITY_LICENSE/EMAIL/PASSWORD varsa Unity yolunu (unity-builder → fastlane `beta_unity`),
  yoksa Flutter yolunu koşar; ikisi aynı bundle id ve aynı build numarası sırasını paylaşır.
  `store_listing/whats_new.txt` `=== UNITY ===` / `=== HTML ===` bölümlü; her lane kendi bölümünü gönderir.
  Unity'siz doğrulama: mono derleme (Unity modül DLL'leri) + `tool/unity_test/BotMac.cs`.
- Doğrulama: `flutter analyze && flutter test && node tool/oyun_sozdizimi.js`.
- Araçlar (2026-10-20): skill `3d-animator` (rig/mocap/retarget) + `birdie-karakter`
  (bu hat), `.mcp.json` → `blender` MCP (yalnız PC), bulut `tool/bulut_kurulum.sh`
  (bpy + Mesh2Motion kütüphanesi). Hareket kaynağı önceliği: kendi pozlarımız →
  Mesh2Motion/CMU (CC0/serbest) → Mixamo (elle, yasin'ın hesabı).
- Karakter: `tool/karakter/` (bpy, IK rig, 22 klip × 2 bakış → sprite sheet, oyuna base64 gömülü). Raket HEP sağ elde: backhand ayrı klip, aynalama yok. Ayak işi badminton: split step, chassé, öne adım, arkaya çapraz. Render.lar
  `/mnt/project-files/birdie-smash/` altında; oyuna girişi (sprite ya da
  three.js) henüz kararlaştırılmadı.
