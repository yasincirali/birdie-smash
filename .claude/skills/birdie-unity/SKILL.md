---
name: birdie-unity
description: Birdie Smash Unity 6 projesi (unity/BirdieSmash) üzerinde her iş için önce oku — oyun kuralı, menü/UI Toolkit, kamera, fizik, karakter görünümü, iOS/TestFlight hattı, CI ekran görüntüsü, Unity MCP. Unity'siz doğrulama merdivenini ve bilinen tuzakları verir. Flutter kabuğu için değil (onun için flutter-* skill'leri).
---

# Birdie Smash — Unity çalışma rehberi

Oyun ve menüler `unity/BirdieSmash` (Unity 6000.0.58f2) içinde. HTML oyunu
(`assets/game/index.html`) yalnız Unity lisansı yokken TestFlight'a giden eski
yol ve kuralların ilk kaynağıdır; yeni oyun işi Unity'de yapılır. Flutter kabuğu
(`lib/`, `ios/`) yalnız WebView + haptic + devir kanalıdır.

## Hangi iş nereye

| Klasör (`Assets/BirdieSmash/`) | İçerik | Kural |
|---|---|---|
| `Core/` | Saf C# oyun mantığı: `Match`, `Court`, `Gear`, `Career`, `Ilerleme`, `Kadraj`, `Devir`… | **UnityEngine importu yok.** Böylece mono ile Unity'siz derlenir ve `tool/unity_test/*.cs` ile ölçülür. Yeni kural/denge buraya. |
| `Game/` | MonoBehaviour'lar: `GameRoot`, `PlayerView`, `ShuttleView`, `CourtView`, `Hud`, `Kalite`, `EkranTuru` | Görünüm ve girdi. Hesap `Core`'da kalır. |
| `Game/UI/` | UI Toolkit ekranları (`AnaEkran`, `KarakterOdasi`…), `Erisilebilirlik`, `Duyarli` | Stil `Resources/ui/menu_stil.uss`. |
| `Editor/` | `CiBuild.cs` (iOS/Linux build, PlayerSettings koddan), `DemoKurulum`, import ayarları | ProjectSettings.asset depoda yok; ayar koddan yazılır. |
| `Resources/veri` | Kariyer/ekipman verisi | Tek kaynak `tool/veri` → `node tool/veri_kopyala.js`. |

Karakter modeli ve klipleri: `birdie-karakter` + `3d-animator` skill'leri.

## Doğrulama merdiveni (Unity Editor bulutta yok)

1. **Core derlemesi + bot maçları** (her Core değişikliğinde):
   `mcs -out:/tmp/botmac.exe tool/unity_test/BotMac.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono /tmp/botmac.exe`
   (depo kökünden; mono yoksa `apt-get install -y mono-mcs mono-runtime`).
   Ölçüm araçları aynı klasörde: `Denge`, `FizikOlc`, `AnimOlc`, `KareHizi`, `GecisOlc`…
   Yeni kural = `BotMac.cs`'e kontrol satırı (dosya başı yorumu karar kaydıdır, tarihle ekle).
2. **Game/UI derlemesi:** Unity modül DLL'leriyle mono derlemesi (UnityEngine
   referansı gerektiren dosyalar). DLL'ler yoksa 3. adıma geç.
3. **Gerçek render:** `ekran/<konu>` dalına push → `unity-ekran.yml` Linux
   oyuncusu derler, `-bsekran` turunu koşar, PNG + `rapor.txt`'yi `ekran-sonuc`
   dalına `rN/` olarak yazar (`sha.txt` ile commit'i eşle). İlk derleme ~33 dk,
   önbellekle kısa. `ekran/**`'a yeni push koşan işi iptal eder.
   Push saatleri kuralı bu dal için de geçerli.
4. **TestFlight:** yalnız `main`'e birleşme (`ios-testflight.yml`, Unity
   secret'ları varsa Unity yolu). `store_listing/whats_new.txt` `=== UNITY ===`
   bölümünü güncelle.

## Unity MCP (mcp-unity, yalnız yasin'in PC'si)

Paket (`com.gamelovers.mcp-unity`, CoderGamester, MIT) depoya eklenmedi: üçüncü
taraf paket olduğu için ekleme kararı yasin'in. PC'de kurulum: Unity'de
**Window › Package Manager › + › Add package from git URL** →
`https://github.com/CoderGamester/mcp-unity.git#1.5.0`, sonra **Tools › MCP Unity ›
Server Window** → **Configure Claude Code (Project)**. Araçlar açık Editor'a
WebSocket ile (localhost:8090) bağlanır; bulutta Editor olmadığı için orada
kullanılmaz, doğrulama yukarıdaki merdivenle yapılır.
Sahne/hiyerarşi okuma, konsol hatası, test koşturma, menü öğesi çalıştırma için
grep yerine bunu kullan.

## Bilinen tuzaklar (yaşandı)

- `.uss` dosyasına `.uxml` ile aynı Resources adını verme: `Resources.Load<StyleSheet>`
  boş `inlineStyle` alt varlığını döndürdü, menüler stilsiz çıktı. Stil `menu_stil.uss`,
  `StilSayfasi()` ad kontrolü + kural sayısı logu.
- Kamera düz perspektif, yalnız eğim; off-axis/kaydırılmış izdüşüm geri getirme
  (kort çarpık görünüyordu). Yükseklik/eğim ekran başına çözülür (`Kadraj`).
- İki taraflı merge'lerde kapanış süslüsü düştü, kod yorum içinde kaldı (Hud/UiKok):
  her merge'den sonra derle + grep.
- CI turunda maçla biten bölümden sonra `root.AnaMenu()` çağır, yoksa sonraki
  menü bölümü bozulur.
- iOS deployment target 15.0 (`CiBuild.cs`). Game Center yetkisi profilde yoksa
  Fastfile entitlements'ı söker.
- localStorage/PlayerPrefs anahtarları `tuytop_*` adıyla kalır (oyuncu ayarı
  sıfırlanmasın); HTML → Unity ilerleme devri `Core/Devir.cs`, bir kez.
- Hareketi azalt açıkken eğim/ağır çekim kuralları `BotMac` `Kadrajlar()` /
  `Pencereler()`'de kilitli.

## UI işi

Menü/ekran değişikliğinde `ui-ux-pro-max` skill'i ve `ui-ux-pro-mcp` sunucusu
(yerleşim, hiyerarşi önerisi); seçilmiş palet "Gün Batımı" korunur. HIG
kontrolü (dokunma ≥44 pt, Dynamic Type karşılığı yazı ölçeği, kontrast,
yalnız renkle anlam yok, güvenli alan, hareketi azalt) PR'a ve artifact'e yazılır.
