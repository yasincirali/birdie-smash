# App Store Erişilebilirlik etiketleri (Accessibility Nutrition Labels)

App Store Connect › App › **Accessibility**. Etiket yalnız uygulamanın **ortak görevleri**
(menüde gezinme, maç oynama, antrenman, Dolap/Atölye) baştan sona o özellikle yapılabiliyorsa
işaretlenir; Apple yanlış beyanı inceleme ve kullanıcı şikâyetiyle düzeltir. Bu yüzden emin
olmadığımız hiçbir şeyi işaretlemiyoruz. Durum 2026-10-30 (Unity sürümü).

| Özellik | İşaretle | Neden | Kanıt |
|---|---|---|---|
| Hareketi Azalt (Reduced Motion) | **Evet** | Sistem ayarı her açılışta ve odak dönüşünde okunur; Ayarlar'da elle de açılır. Açıkken kamera sarsıntısı, yumruk yakınlaşması, ışınlar, yıldız inişi, geri sayım büyümesi, vitrin dönüşü ve tezgâh kamera kayması kapanır; geçişler solmaya döner. | `Game/Ayar.cs` `SistemOku()` (`Yerel.HareketAzalt`, `Plugins/iOS/BirdieYerel.mm` `BirdieHareketAzalt`); `Ayar.reduce` kullanan yerler (Hud, Vitrin, GameRoot). Ekran turu `menu_*` kareleri. |
| Yalnız Renkle Ayırt Etmeme (Differentiate Without Color Alone) | **Evet** | Oyuncu ve rakip halkası biçimle ayrışır (dolu / kesik halka) ve yanında "SEN" / rakip adı yazar; seçili düğme ✓ ile, antrenman sonucu ✓/✗ ve cümleyle, ses düğmesi "Ses açık / Ses kapalı" yazısıyla söylenir; yıldızlar dolu / çerçeve biçimindedir. | `Game/Hud.cs` (halka dokuları `ring`/`ringKesik`, `Chip` ✓, maç sonu yıldız ve koşul satırları, Duraklat ses düğmesi); `sonuc_13`, `duraklat_*` kareleri. |
| Koyu Arayüz (Dark Interface) | Hayır (şimdilik) | Menüler ve maç sonu koyu ("Gün Batımı" mürekkep zemini), ama oyun alanı (saha, tribün) aydınlık ve açık bir koyu mod yok. Apple ölçütü ortak görevlerin koyu arayüzle yapılmasını istiyor; maç ekranı bunu karşılamıyor. Koyu saha seçeneği gelirse yeniden bakılır. | `Resources/ui/menu_stil.uss` kök renkleri; `oyun_*` kareleri. |
| Yeterli Kontrast (Sufficient Contrast) | Hayır (şimdilik) | Menü ve sonuç ekranı metinleri APCA'ya göre seçildi, ama saha üstündeki yüzen yazılar (sayı yazısı, ipuçları) değişken zemin üstünde; tamamı ölçülmedi. | `Hud.cs` yön C yorumları (Lc ≥ 60 notları). |
| Daha Büyük Yazı (Larger Text) | Hayır | Menü sistem yazı boyunu ×2'ye kadar izler, ama maç içi HUD bilerek ×1,3'te durur (oyun alanını kapatmasın). Apple en az %200 istiyor. | `Ayar.cs` `YaziK` / `MenuYaziK`. |
| VoiceOver | Hayır | Etiketler (`Erisim.Etiket`) kayıtlı ama Unity 6 erişilebilirlik ağacına köprü henüz yazılmadı; VoiceOver öğeleri okumuyor. Oyunun kendisi kaydırma ve zamanlama istiyor. | `Game/UI/Erisilebilirlik.cs` başındaki TODO. |
| Sesli Kontrol (Voice Control) | Hayır | VoiceOver ile aynı köprüye bağlı (öğe adları sisteme verilmiyor). | Aynı. |
| Altyazı (Captions) | Uygulanmaz | Konuşma ya da video yok; sesler efekt ve müzik. Boş bırakılır. | — |
| Sesli Betimleme (Audio Descriptions) | Uygulanmaz | Video içerik yok. Boş bırakılır. | — |

ASC'de yapılacak satır `YAPMAN_GEREKENLER.md`'de.
