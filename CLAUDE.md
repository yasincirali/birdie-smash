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
  İlerleme devri (2026-10-25): HTML `{tip:'devir'}` → kabuk `birdiesmash/kabuk` kanalı → AppDelegate tuytop_* değerlerini
  aynı adla NSUserDefaults'a yazar (= iOS PlayerPrefs); Unity `Core/Devir.cs` bir kez kabul edip `tuytop_html_devir` koyar,
  bayrak varsa kabuk yazmaz. Mağaza puanı `{tip:'puan'}` (karar oyunda, kabuk yalnız sistem API'si).
- Doğrulama: `flutter analyze && flutter test && node tool/oyun_sozdizimi.js`.
- Araçlar (2026-10-20): skill `3d-animator` (rig/mocap/retarget) + `birdie-karakter`
  (bu hat), `.mcp.json` → `blender` MCP (yalnız PC), bulut `tool/bulut_kurulum.sh`
  (bpy + Mesh2Motion kütüphanesi). Hareket kaynağı önceliği: kendi pozlarımız →
  Mesh2Motion/CMU (CC0/serbest) → Mixamo (elle, yasin'ın hesabı).
- Karakter: `tool/karakter/` (bpy, IK rig, 22 klip × 2 bakış → sprite sheet, oyuna base64 gömülü). Raket HEP sağ elde: backhand ayrı klip, aynalama yok. Ayak işi badminton: split step, chassé, öne adım, arkaya çapraz. Render.lar
  `/mnt/project-files/birdie-smash/` altında; oyuna girişi (sprite ya da
  three.js) henüz kararlaştırılmadı.

- **Arayüz tasarım dili (yasin 2026-11-14):** her menü/HUD değişikliği `docs/arayuz_tasarim_dili.md`'ye uyar:
  yazı hiçbir dilde/boyda üst üste binmez, yan boşluklar ve düğme içi ortalama simetrik. UI PR'ında `ekran/kisa-yazi-*`
  turu `yazi_tasma.txt` + `yazi_cakisma.txt` "toplam 0" ister.

## Skill ve MCP yönlendirmesi (yasin 2026-10-29: "Unity oyuna Unity, Flutter'a Flutter")

Kod yazmadan önce ilgili skill çağrılır; uymuyorsa özette "skill dışı" yazılır.

| İş | Skill | MCP |
|---|---|---|
| Unity oyun/menü/kamera/fizik/iOS hattı (`unity/`) | `birdie-unity` | `mcp-unity` (yalnız PC, açık Editor; kurulum skill'de); bulutta doğrulama `birdie-unity` merdiveni |
| Karakter, rig, klip (`tool/karakter/`) | `birdie-karakter`, `3d-animator` | `blender` (yalnız PC) |
| Flutter kabuğu (`lib/`, `ios/`, WebView, haptic, devir kanalı) | `flutter-architecture`, `flutter-expert` ⚠️, `flutter-testing` | `dart` |
| Menü/ekran tasarımı, UI cilası | `ui-ux-pro-max`, `emil-design-eng` | `ui-ux-pro-mcp` |
| Menü/HUD hareketi, geçiş, animasyon denetimi | `animate`, `apple-design`, `design-motion-principles`; denetim `review-animations`, `improve-animations`, `find-animation-opportunities`; ad bulma `animation-vocabulary` | — |
| Tasarım sistemi, token, bileşen kuralı | `design-system` (yargısı alınır, CSS sözdizimi alınmaz; kural kaynağı `docs/arayuz_tasarim_dili.md`) | `ui-ux-pro-mcp` |
| Hata / çökme / stack trace | `debugging-wizard`, sonra `uw-unity-debugging` | — |
| Kod incelemesi, PR öncesi | `code-reviewer` (+ stüdyo ajanı `unity-specialist`) | — |
| Bir kararı zorlatma / pre-mortem | `the-fool` | — |
| Dokümansız kodu çözme / doküman | `spec-miner` / `code-documenter` | — |
| iOS yerel kod (AppDelegate, devir kanalı, Game Center köprüsü) | `swift-expert` | — |
| Mağaza/tanıtım videosu | `remotion-motion-graphics` | — |
| Yeni skill yazma | `skill-creator` | — |
| Kod keşfi (her iş) | — | `codebase-memory-mcp` önce (`search_graph`, `trace_path`), sonra grep |

`flutter-expert` Bloc/GoRouter önerir; kabuk tek ekran, yönlendirme yok. Sandık'tan alınan skill'ler (2026-11-14, yasin: "Sandık içinde işine yarayabilecek skilleri ve mcpleri al") `.claude/skills/` altında kopyadır; web/CSS örnekleri Unity UI Toolkit'e yargı olarak taşınır. Alınmayanlar: Supabase/SQL/güvenlik (sunucu yok), web sitesi tasarım skill'leri, `brag`/`canvas-design` (büyük, oyunda karşılığı yok). `.mcp.json`'a `codebase-memory-mcp`, `dart`, `ui-ux-pro-mcp` eklendi (yalnız PC yolları; bulutta `tool/bulut_kurulum.sh` kurar). Bulutta
skill'ler ve `codebase-memory`/`dart`/`ui-ux-pro` sunucuları sandikapp'in
`tool/bulut_kurulum.sh` betiğiyle her thread'e açılır.


## Stüdyo ajanları (yasin 2026-11-05: claude-unity-game-studio "en verimli nasıl kullanırız")

`.claude/agents/` 11 rol (kaynak ve neyin neden alınmadığı `.claude/agents/KAYNAK.md`). Ajan kullanıcıya soru sormaz,
bulgu döner; uygulayan çağıran Claude'dur. Oturumun ajan listesinde yoksa (thread'ler `/home/user`'da başlar) aynı
rol `general-purpose` ajana "önce `/home/user/birdie-smash/.claude/agents/<ad>.md`'yi oku, o rol olarak çalış" diye verilir.
Maliyet kuralı: ajan pahalıdır; yalnız aşağıdaki anlarda, en fazla 3 paralel ve dar görevle.

| An | Ajanlar (paralel) | Görev |
|---|---|---|
| Ar-Ge fikir turu (11:40/19:40) | `game-designer`, `art-director` | Rakip oyunla kıyaslı 2–3 fikir; arcade hissi önce |
| Ar-Ge inceleme turu (12:10/20:10) | `ux-designer` + `accessibility-specialist`, `performance-analyst` | Son ekran turu karelerinden kontrast/okunurluk/HIG; kare süresi ve GC |
| Ekonomi/ödül değişikliği | `economy-designer` | Öneri; karar dev thread'in |
| PR açmadan önce (Unity diff) | `unity-specialist` ya da UI ise `unity-ui-specialist`, `qa-tester` | Diff incelemesi + BotMac/ekran turu test adımları |
| Ses, efekt, sevinç, his | `sound-designer`, `technical-artist` | Somut parametreli öneri |

Unity bilgi skill'leri (`unity-ui`, `unity-animation`, `unity-performance`, `unity-platforms`, `uw-game-feel-integrator`,
`eng-unity-mobile-optimization` …) Unity 6 API başvurusudur; `birdie-unity` skill'inden sonra, ilgili konuda okunur.
Projede olmayan paket önerileri (VContainer, UniTask, PrimeTween, Addressables, DOTS) uygulanmaz.
