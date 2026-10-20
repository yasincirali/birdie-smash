# Birdie Smash — Unity demosu

HTML oyununun (`assets/game/index.html`) kurallarının Unity 6'ya taşınmış hâli. Mağaza hattı
hâlâ Flutter + WebView; bu klasör yalnız deneme (dal `unity-demo`, push edilmedi).

## Açma
1. Unity Hub › **Add** › bu klasörü (`unity/BirdieSmash`) seç. Sürüm: **Unity 6** (6000.0.x; başka 6000 sürümü de açar, Hub sorar).
2. İlk açılışta `DemoKurulum` sahneyi (`Assets/Scenes/Demo.unity`) kurar ve açar. Kurmazsa menü: **Birdie Smash › Demo kurulumunu yenile**.
3. Game penceresini dikey bir telefon oranına al (ör. 1080×2340) ve **Play**.

## Oynanış
- Servis: ekranda yukarı kaydır (hızlı = uzun servis).
- Top sana gelince "Şimdi kaydır!" çıkar ve zaman yavaşlar. Yukarı kaydır: yavaş = drop, orta = clear, hızlı = smaç (top alçaksa drive). Aşağı kaydır = file önüne blok. Sağa/sola eğim yönü belirler.
- Topun tam ortasından geçen kaydırma "Tam orta"dır: daha isabetli vurur ve smaç enerjisini doldurur; enerji dolunca hızlı kaydırma sıçrayarak smaça döner.
- Dokunmak güvenli bir clear atar. **Kolay**da koşu otomatik; **Usta**da dokunduğun yere koşarsın.

## Yapı
- `Core/`: saf C# (UnityEngine yok). Kort ölçüleri (BWF), tüy top fiziği (sürtünme, 240 Hz), file dalgası, maç kuralları, rakip yapay zekâsı. HTML'deki mantığın birebir çevirisi.
- `Game/`: görünüm. Kort, esneyen file, tüy top + iz, oyuncular (FBX, Legacy Animation), prosedürel ses, IMGUI HUD, kamera.
- `Resources/karakter/oyuncu.fbx`: `tool/karakter/unity_fbx.py` ile Blender karakterinden üretilir (22 klip). Klip aralıkları `oyuncu_clips.json`; `Editor/KarakterImport.cs` içe alırken böler.
- Doğrulama (Unity'siz): `tool/unity_test/` altında mono ile derleme + 12 bot maçı (`BotMac.cs`).
