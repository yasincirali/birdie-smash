# Claude Development Guidelines — Birdie Smash

Sandık ailesinin badminton mobil oyunu (çalışma adı "Tüy Top" idi; yasin
2026-10-19: ad global olsun). Çalışma biçimi, onay eşikleri ve push saatleri
kuralı (hafta içi 09:00–18:30 TR push/PR/merge yok) sandık `CLAUDE.md` ile
aynıdır; burada yalnız farklar.

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
  türetildi; match branch `birdiesmash`, ayrı `MATCH_PASSWORD`). Android hattı yok.
- Doğrulama: `flutter analyze && flutter test && node tool/oyun_sozdizimi.js`.
- Karakter modeli (Blender/bpy, `karakter.py`) ve render'lar
  `/mnt/project-files/birdie-smash/` altında; oyuna girişi (sprite ya da
  three.js) henüz kararlaştırılmadı.
