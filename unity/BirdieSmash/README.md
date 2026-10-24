# Birdie Smash — Unity demosu

HTML oyununun (`assets/game/index.html`, main fb1b245 hâli) kurallarının ve hissinin Unity 6'ya taşınmış hâli.
Mağaza hattı hâlâ Flutter + WebView; bu klasör yalnız deneme (dal `unity-demo`, push edilmedi).

## Açma
1. Unity Hub › **Add** › bu klasörü (`unity/BirdieSmash`) seç. Sürüm: **Unity 6** (6000.0.x; başka 6000 sürümü de açar, Hub sorar).
2. İlk açılışta `DemoKurulum` sahneyi (`Assets/Scenes/Demo.unity`) kurar ve açar. Kurmazsa menü: **Birdie Smash › Demo kurulumunu yenile**.
3. Game penceresini dikey bir telefon oranına al (ör. 1080×2340) ve **Play**.

## Oynanış (HTML ile aynı)
- İlk açılış: 3 adımlı antrenman (kısa servis → clear dibe → smaç), sonra 5 sayılık Kolay maç. Menüden tekrar açılır.
- Servis: yukarı kaydır. Yavaş = kısa servis, hızlı + kısa = flick, hızlı + uzun = yüksek servis.
- Top sana gelince zaman yavaşlar. Yukarı kaydır: yavaş = drop, orta = clear, hızlı = smaç (alçak top = drive). Aşağı = blok. Eğim yön verir.
- Tam orta vuruş isabet ve smaç enerjisi verir; enerji dolunca hızlı kaydırma sıçrayarak smaç olur.
- İki oyuncuda da nefes (dayanıklılık) halkası var; yorulan oyuncu yavaşlar.
- Ayarlar: Kolay/Usta, 5/11/21 sayı, saha (salon, turnuva, okul, park), figür (Oyuncak 3B / Gerçekçi), hareketi azalt, ses.
- Kariyer: 6 basamak; bitiş düğmesi "Sıradaki: {yer}" / "Tekrar dene" / "Bir maç daha".

## Yapı
- `Core/`: saf C# (UnityEngine yok). Kort (BWF), tüy top fiziği, file dalgası, maç kuralları, rakip stilleri (Blaze/Rocco/Mika), antrenman (`Ogr.cs`), kariyer (`Career.cs`), klip/faz seçimi (`Match.PickClip`).
- `Game/`: görünüm. Saha temaları + tribün (yalnız rakip arkası) + yayın ışığı + iki hakem (`CourtView`), oyuncular (`PlayerView`, Legacy Animation, normalize zamanla örnekleme), IMGUI HUD, ayarlar (`Ayar`, PlayerPrefs anahtarları HTML'deki `tuytop_*` adlarıyla aynı).
- `Resources/karakter/oyuncak.fbx` (KayKit Rogue, CC0) ve `rocketbox.fbx` (Microsoft Rocketbox Sports_Male_04, MIT): `tool/karakter/unity_fbx.py` ile üretilir (`BS_MODEL=oyuncak|aday`). Klip aralıkları `<model>_clips.json`; `Editor/KarakterImport.cs` içe alırken böler.
- `Resources/lisanslar.txt`: `legal/ucuncu_taraf.md` kopyası; menüdeki Lisanslar ekranı okur.
- Doğrulama (Unity'siz): `tool/unity_test/` altında mono ile derleme + bot maçları (`BotMac.cs`), ralli kaydı (`RalliKayit.cs`) ve Blender önizlemesi (`onizleme.py`).
