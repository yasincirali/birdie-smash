# Birdie Smash — Unity demosu

HTML oyununun (`assets/game/index.html`, main 46943a8 hâli) kurallarının, görünümünün ve hissinin Unity 6'ya taşınmış hâli.
2026-10-24 tam geçiş: 3B tüy top, ekipman + ekonomi + 360° vitrin, görevler, İngilizce arayüz, akış görselleri, smaç perdesi,
antrenman iz/hız bandı/bilgi adımları/antrenör çağrısı, sentez ses. Game Center bu demoda yok.
TestFlight (2026-10-24, yasin: "Unity ile değiştirelim menüleri ve tüm oyunu"): `ios-testflight.yml` Unity lisans secret'ları
varsa build'i bu projeden çıkarır (ubuntu'da game-ci/unity-builder → `Editor/CiBuild.cs` `BirdieSmash.EditorTools.CiBuild.iOS` →
Xcode projesi; macOS'ta fastlane `beta_unity`). Secret'lar yoksa Flutter + WebView yolu çalışır. Kurulum: `YAPMAN_GEREKENLER.md`.
PlayerSettings (bundle `com.birdiesmash.app`, ad, dikey, IL2CPP, ikon `Icon/AppIcon.png` = Flutter ikonu) build anında koddan yazılır;
depoda ProjectSettings.asset yok.

## Açma
1. Unity Hub › **Add** › bu klasörü (`unity/BirdieSmash`) seç. Sürüm: **Unity 6** (6000.0.x; başka 6000 sürümü de açar, Hub sorar).
2. İlk açılışta `DemoKurulum` sahneyi (`Assets/Scenes/Demo.unity`) kurar ve açar. Kurmazsa menü: **Birdie Smash › Demo kurulumunu yenile**.
3. Game penceresini dikey bir telefon oranına al (ör. 1080×2340) ve **Play**.

## Oynanış (HTML ile aynı)
- İlk açılış: 3 adımlı antrenman (kısa servis → clear dibe → smaç), sonra 5 sayılık Kolay maç. Menüden açılan tam antrenman 15 adım:
  parmak izi + hız bandı, iki bilgi adımı (Nefes halkası, Smaç gücü çubuğu: oyun durur, dokununca geçer) ve son adımda antrenör çağrısı.
- Servis: yukarı kaydır. Yavaş = kısa servis, hızlı + kısa = flick, hızlı + uzun = yüksek servis.
- Top sana gelince zaman yavaşlar. Yukarı kaydır: yavaş = drop, orta = clear, hızlı = smaç (alçak top = drive). Aşağı = blok. Eğim yön verir.
- Tam orta vuruş isabet ve smaç enerjisi verir; enerji dolunca hızlı kaydırma sıçrayarak smaç olur.
- İki oyuncuda da nefes (dayanıklılık) halkası var; yorulan oyuncu yavaşlar.
- Ayarlar: Kolay/Usta, 5/11/21 sayı, saha (salon, turnuva, okul, park), figür (Oyuncak 3B / Gerçekçi), hareketi azalt, ses.
- Kariyer: 6 basamak; bitiş düğmesi "Sıradaki: {yer}" / "Tekrar dene" / "Bir maç daha". Kariyer bitince Şampiyon forması açılır.
- Ekipman (menü › Ekipman): raket (Denge/Kafa ağır/Kafa hafif/Esnek), tel gerginliği 18–32 lbs (güç/tatlı nokta/kontrol ve raket sesinin
  tonu), bileklik, saç bandı, forma; altı yuva Tüy ile 10 seviyeye yükselir (oyuncu seviyesi kapısı). "Eşit şartlar" serbest maçta
  yükseltmeyi kapatır. 360° vitrin: sürükle ya da oklar; raket rengi seçilir. Çarpanlar HTML `gsHesap` ile birebir (BotMac denetler).
- Ödül: maç sonu Tüy + XP (günün ilk galibiyeti, yeni basamak, görevler, mükemmel vuruşlar), seviye atlama, "Ekipmanı yükselt".
- Görevler: her maçta 3 görev; servis şeridinde yazar, tamamlanınca duyurulur.
- Dil: Ayarlar › Dil (Türkçe / English); İngilizce metinler HTML `TR_EN` tablosundan (`Resources/tr_en.json`).

## Yapı
- `Core/`: saf C# (UnityEngine yok). Kort (BWF), tüy top fiziği, file dalgası, maç kuralları, rakip stilleri (Blaze/Rocco/Mika), antrenman (`Ogr.cs`), kariyer (`Career.cs`), klip/faz seçimi (`Match.PickClip`).
- `Core/Gear.cs`, `Core/Gorev.cs`, `Core/Json.cs`: ekipman/ekonomi/ödül (saf C#, `IKayit` üzerinden kalıcılık), görevler, küçük JSON.
- `Game/ShuttleView.cs`: 3B tüy top (16 tüy, iplik bağı, mantar + şerit; BWF ölçüsü × 3,2), yön/salınım/dönüş, iz, gölge, iniş halkası.
  `Game/Sfx.cs`: HTML Web Audio sentezinin çevrim dışı karşılığı (raket sesi tel gerginliği ve vuruş türüne göre). `Game/Fx.cs`: tüy/kıvılcım
  parçacıkları, akış ve antrenman bantları. `Game/Vitrin.cs`: ekipman ekranının 360° kamerası. `Game/Lang.cs`: dil.
- `Game/`: görünüm. Saha temaları + tribün (yalnız rakip arkası) + yayın ışığı + iki hakem (`CourtView`), oyuncular (`PlayerView`, Legacy Animation, normalize zamanla örnekleme), IMGUI HUD, ayarlar (`Ayar`, PlayerPrefs anahtarları HTML'deki `tuytop_*` adlarıyla aynı).
- `Resources/karakter/oyuncak.fbx` (KayKit Rogue, CC0) ve `rocketbox.fbx` (Microsoft Rocketbox Sports_Male_04, MIT): `tool/karakter/unity_fbx.py` ile üretilir (`BS_MODEL=oyuncak|aday`). Klip aralıkları `<model>_clips.json`; `Editor/KarakterImport.cs` içe alırken böler.
- `Resources/lisanslar.txt`: `legal/ucuncu_taraf.md` kopyası; menüdeki Lisanslar ekranı okur.
- Doğrulama (Unity'siz): `tool/unity_test/` altında mono ile derleme + bot maçları (`BotMac.cs`), ralli kaydı (`RalliKayit.cs`) ve Blender önizlemesi (`onizleme.py`; `YAKIN=1` ile tüy top yakın planı).

## HTML'de olmayanlar (UNITY_EK)
- `ShuttleView.SLICE_TAKLA` (açık): slice vuruşunda tüy top daha güçlü sallanır; HTML metni "takla atar" der ama çizimi düz vuruşla aynı.
  `false` yapınca HTML davranışı birebir.
- Ayarlar'da Dil seçimi (HTML tarayıcı dilinden seçiyor).
- Titreşim: Unity'nin `Handheld.Vibrate` tek darbesi; HTML'deki desenler (`Sfx.Desen`) hesaplanıyor ama iOS'ta desen için yerel eklenti gerekir.
- Ses zincirinde HTML'deki kompresör yok (çevrim dışı sentez; örnekler ±1'de kırpılır).
- Vuruş yayları raket başının izi olarak çizilir (HTML 2B yay); 360° vitrin canlı 3B kamera (HTML sprite turu).
