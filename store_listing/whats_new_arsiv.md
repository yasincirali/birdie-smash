# TestFlight notları arşivi (Unity)

2026-11-08: `whats_new.txt` 28 KB'a çıkmıştı; Fastfile notu 4000 karakterde kesiyordu. TestFlight'a çıkmış eski notlar buraya taşınır,
`whats_new.txt`'de yalnız son birkaç build'in notu kalır. Yeni arşiv en üste eklenir.

## 8 Kasım 2026 ve öncesi

Bu sabah (8 Kasım 07:20 inceleme turu):
- Kaybedince alttaki yazı ile önerilen antrenman düğmesi artık aynı şeyi söylüyor: rakip file önüne bıraktıysa ikisi de file önünü, smaçla sayı topladıysa ikisi de savunmayı önerir. Yeni oyuncuya (ilk üç galibiyete kadar) ileri dersler önerilmez.
- Profil'de tur yıldızları artık yalnız "Tur mühürleri" kartında; kilitli tur "Kilitli" yazar. İstatistikler kısaldı.
- Hızlı maç'ta 5 sayının açıklaması artık yalnız "Yaklaşık 2 dakika".
Nasıl denenir: Bir maçta rakibin file önüne bıraktığı toplara yetişme, kaybet: alttaki yazı "file önüne bıraktı" diyorsa düğme "Antrenman: File dibi" olmalı. Profil'de kilitli bir turun yanında "Kilitli" yazmalı, İstatistikler'de tur adları tekrar etmemeli.

Bu gece (8 Kasım 02:20 inceleme turu):
- Menüde boş "Reklam" kutusu artık yok: reklam gelmediyse yer de ayrılmaz, reklam gelince sekmelerin hemen üstünde açılır.
- Tüy dükkânının altında "Reklamsız" kartı: bir kez alınır, menüdeki reklam şeridini kaldırır, Tüy vermez. Ödüllü reklam isteyene açık kalır.
- Dolap › Raket: vitrindeki "Oyuncuya dön / Raketi incele" düğmesi kalktı (üstteki Görünüm · Raket seçimi aynı işi yapar). Raket artık durmadan dönmüyor: güzel bir açıda durur, biraz bekleyince yavaşça sağa sola salınır.
- İngilizce arayüzde raket satırındaki şaft sertliği ve "inç" artık İngilizce.
- Maçta aynı ıska nedeni ("Kısa kaydırdın" gibi) bir maçta en çok iki kez yazılır; sonra yalnız ıska sesi. İlk üç galibiyete kadar her seferinde yazar.
Nasıl denenir: Oyunu silip yeniden kur, antrenmanı bitir: Oyna sekmesinin altında boş gri kutu olmamalı. Dükkânda (üstteki Tüy sayacı) Reklamsız kartı paketlerin altında olmalı (ürün mağazada açılana kadar "Satın alınamıyor"). Dolap › Raket'te raket ince bir çizgi gibi görünmemeli, 3 saniye dokunmayınca hafifçe salınmalı; oklar hâlâ çevirmeli. Zor zorlukta birkaç kez aynı şekilde ıskala: üçüncüsünden sonra yazı çıkmamalı, ses gelmeli.

Reklam şeridi düzeltmesi (7 Kasım gecesi):
- Menüdeki reklam şeridi artık sekme düğmelerinin üstüne binmiyor; kendi siyah şeridinin içinde, sekmelerin hemen üstünde duruyor.
Nasıl denenir: Oyna, Kariyer ve Dolap sekmelerinde alttaki reklam şeridine bak: sekme ikonları tamamen görünür ve dokunulabilir olmalı, reklam üstündeki siyah şeridin ortasında durmalı.

Bu gece (8 Kasım 00:20 inceleme turu):
- Oyunu ilk kez açınca reklam izni sorusu artık hemen çıkmıyor: önce antrenman ve ilk maç, soru ilk maçtan sonra menüye dönünce gelir.
- Maç uzunluğu (5, 11, 21 sayı) Ayarlar'dan kalktı; Hızlı maç sayfasında ve kariyerde maç ayrıntısında, zorluğun hemen altında seçilir.
Nasıl denenir: oyunu silip yeniden kur: açılışta izin penceresi olmamalı; antrenmanı ve ilk maçı bitirip menüye dönünce gelebilir (yalnız reklam ayarı açıksa). Kariyer'de bir basamağın Maç ayrıntısı'nı aç: zorluk çiplerinin altında 5 · 11 · 21 sayı çipleri olmalı; birini seçince Hızlı maç'taki seçim de aynı olmalı. Ayarlar'da artık "Oyun" bölümü yok.

Raket, Dolap ve Kariyer yenilendi (7 Kasım akşamı):
- Menüdeki reklam şeridi kendi yerinde, sekmelerin hemen üstünde duruyor; kartların üstüne binmiyor.
- Raketin gerçek ölçüleri: ağırlık sınıfı ve gram (3U, 4U, 5U), denge noktası milimetre olarak (kafa hafif, dengeli, kafa ağır), şaft sertliği, raketin önerdiği gerginlik aralığı, kordaj kalınlığı. Her ölçünün ne demek olduğu ve maçta neyi değiştirdiği yazar.
- Kordaj gerginliği artık hep görünür: Atölye'deki raket kartında ve Dolap > Raket'te. Raketin önerdiği aralığın dışına çıkarsan uyarı çıkar.
- Yeni ücretsiz ayar: sap ölçüsü G4 (kalın), G5, G6 (ince). İnce sap bileği hızlandırır, kalın sap kontrol verir. Eski grip parçalarının adı artık Sargı.
- Dolap'ın üstünde iki bölüm: Görünüm ve Raket. Raketle ilgili her şey tek yerde.
- Kariyer: her tur bir kart, yana kaydırınca sonraki tur. Kartta altı basamak tek satırda, mühüre kaç yıldız kaldığı yazar. Altında sıradaki maç açık; tek dokunuşla oynarsın.
Nasıl denenir: Dolap'ı aç, üstten Raket'e geç: raketin ölçü satırı (ör. 4U · G5 · 290 mm), gerginlik kaydırıcısı ve sap seçimi görünmeli; sayfanın sonunda teknik sayfa olmalı. Gerginliği önerilen aralığın dışına çek: uyarı çıkmalı. Kariyer'i aç: tur kartlarını sağa sola kaydır, bırakınca bir kart ortaya oturmalı ve altındaki maç o tura göre değişmeli. Turuncu Oyna düğmesi maçı doğrudan başlatmalı. Basamak noktasına dokununca maç ayrıntısı açılmalı. Büyük yazıda da deneyin.

Bu gece (7 Kasım 22:15 inceleme turu):
- Oyun telefonuna Türkçe olarak da tanıtılıyor: telefon dili Türkçeyse reklam izni penceresi Türkçe çıkar. App Store sayfasındaki dil satırı İngilizce ve Türkçe der.
Nasıl denenir: telefon dili Türkçeyken oyunu silip yeniden kur; reklam izni penceresi çıkarsa metni Türkçe olmalı. (Pencere yalnız reklam ayarı açıksa gelir; gelmezse sorun değil.)

Bu akşam (7 Kasım 20:15 inceleme turu):
- Rakibin smaçını geri çevirince kortta "Karşıladın" ve smaçın hızı yazar, seyirci nefesini tutar, telefon kısa titrer. Maçta ilk üç kez, sonra yalnız uzun rallilerde.
- Duraklat ekranında bu maçın üç görevi yazar: kalanlar sayacıyla (ör. 1/3), bitenler işaretli ve soluk.
- Game Center düğmesi Ayarlar'dan kalktı; Profil'de Başarımlar başlığının hemen altında.
Nasıl denenir: Orta ya da Zor'da Smaşçı'ya karşı bir maç oyna; onun smaçını döndürünce "Karşıladın · ... km/s" görmeli, ses ve titreşim hissetmelisin. Maçta duraklat: skorun altında üç görev satırı olmalı. Profil'i aç: Game Center düğmesi Başarımlar başlığının altında.

Bu akşam (7 Kasım 18:20 inceleme turu):
- Ayarlar > Ses'te yeni "Titreşim" düğmesi: titreşimi kapatmak için artık Hareketi azalt'ı açman gerekmiyor; kamera sarsıntısı ve yakınlaşma yerinde kalır.
- Oyunu ilk kez açınca alttaki sekmeler gizli, ekranda yalnız ilk maç var. Antrenmanı bitirince sekmeler gelir. Ayarlar düğmesi hep üstte.
- Kariyer maç kartı kısaldı: "Önerilen ekipman puanı" satırı kalktı (aynı bilgi tur kartında). Kazandığın bir basamağı yeniden oynarken eksik yıldızın en üstte, aldıkların tek satırda yazar.
- Tur töreninde altı rakip tek satırda.
Nasıl denenir: Ayarlar > Ses'te Titreşim'i kapat, bir maç oyna: vuruşta titreşim olmamalı, ses ve görüntü aynen sürmeli; Hareket tam kalmalı. Kariyer'de 1 ya da 2 yıldızlı bir basamağın kartını aç: eksik yıldız ilk satırda, altta soluk "Alındı: ..." satırı olmalı. İlk açılışı görmek istersen oyunu silip yeniden kur: sekmeler antrenmandan sonra gelmeli.

Menüler ve akışlar elden geçti (7 Kasım, stüdyo incelemesi):
- Seçili olan her yerde aynı görünür: turuncu çerçeve ve hafif turuncu zemin. Alt sekmede seçili simge biraz büyür ve yükselir. Koyu ekranlarda (Kariyer, Dolap, Atölye, Profil) üstte soluk bir güneş var.
- Dolap'ta kamera düğmeleri simge oldu; Atölye'de artı ve eksi yazı yerine yeşil yukarı ok ve soluk aşağı ok. Tur düğmeleri daha büyük, kilitli tur net okunur.
- Maç içinde kendi skorun limon yeşili, smaç gücü çubuğu kalın ve parlak, duraklat düğmesi hap şeklinde.
- Seviye atlayınca açılanlar "Dolapta" ve "Atölyede" diye ayrılır; turuncu düğme seni doğrudan yeni eşyaya götürür.
- Kariyer açılınca şimdiki maçına kayar; altta sabit "Sıradaki maç" düğmesi tek dokunuşla maç kartını açar. Tur kartında turun teması, altı rakip ve ekipman hazırlığı görünür.
- Bir turu bitirince tören: kaç yıldız topladığın, yendiğin rakipler, sıradaki tur. 9 yıldızda bronz, 18 yıldızda altın tur mührü kazanırsın (satılmaz, yalnız oynayarak); tur düğmesinde ve Profil'de görünür.
- Dolap'ta hedef eşyanın yanında yaklaşık kaç maç sonra alabileceğin yazar (birkaç maç oynadıktan sonra).
Nasıl denenir: Bir maç kazanıp seviye atla: açılanlar gruplu çıkmalı, turuncu düğme Dolap'a ya da Atölye'ye götürmeli. Kariyer'i aç: liste şimdiki maça kaymış olmalı, alttaki "Sıradaki maç" düğmesi maç kartını açmalı. Büyük yazı açıkken Hızlı maç sayfasını aç: yazılar kesilmemeli, liste sürüyorsa altta solma görünmeli. Profil'de "Tur mühürleri" bölümüne bak.

Reklam bağlandı (test reklamları):
- Menüdeki "Reklam" şeridinde artık gerçek bir reklam şeridi var. Şimdilik Google'ın test reklamları: üzerinde "Test Ad" yazar, tıklamak bir şey satın almaz. Maçta şerit yok.
- Maç sonu kartında, Tüy kazandığın bir maçta, kartta başka öneri yoksa "Ödüle +%50 · reklam izle" düğmesi çıkabilir. Kısa bir video izlersen o maçın Tüy ödülü yarısı kadar artar. Günde en çok bir kez, ilk 3 galibiyetten sonra. İzlemek zorunlu değil, videoyu yarıda kapatırsan ödül yok, başka bir şey değişmez.
- Avrupa'daysan ilk açılışta Google'ın reklam izni penceresi çıkabilir; izin vermesen de oyun aynen çalışır.
Nasıl denenir: Menüde alttaki şeride bak, birkaç saniye içinde "Test Ad" yazan bir reklam görünmeli; Ayarlar ya da alt sayfa açınca şerit gizlenmeli, maçta hiç görünmemeli. 3 galibiyetten sonra Tüy kazandığın bir maçı bitir: kartta reklam düğmesi çıkarsa bas, videoyu sonuna kadar izle, kapanınca Tüy sayısı artmış olmalı. Aynı gün ikinci maçta düğme çıkmamalı.
Küçük metin düzeltmeleri:
- Ayarlar'da üst üste duran iki başlıktan boş olan "Görünüm" kalktı; "Oyuncu görünümü" doğrudan seçeneklerin üstünde.
- Profil'de ad notu artık doğruyu söylüyor: adın cihazında ve kendi iCloud hesabında tutulur, bize gönderilmez.
- Nasıl oynanır > Çubuklar ve tarz: tarzın bir artısı ve bir eksisi olduğu, fark yarattığı anda kortta adıyla yazıldığı anlatılıyor.
Nasıl denenir: Ayarlar'ı aç, en üstte "Oyun"dan sonra tek başlık "Oyuncu görünümü" olmalı. Profil'de ad alanının altındaki notu oku. Ayarlar > Nasıl oynanır'da "Çubuklar ve tarz" satırını oku.

Tarzlar artık kortta hissediliyor (7 Kasım):
- Dört oyun tarzının etkisi büyüdü: Smaşçı'nın smacı %6 hızlı ve enerjisi daha çabuk dolar; Duvar daha uzağa uzanır ama smacı yavaştır; File ustası file önünde tam ortayı çok daha kolay bulur; Aldatıcı'nın slice'ı rakibi daha çok şaşırtır. Her tarzın eksisi de aynı ölçüde gerçek.
- Tarzın sonucu değiştirdiği an kortta adıyla yazılır ("Duvar uzanışı!", "Tam orta · File ustası"), maç sonu Ayrıntılar'da "Tarzın N kez fark yarattı" görünür.
Nasıl denenir: Atölye'de Tarz bölümünden Duvar'ı seç ve bir maç oyna: uzaktaki alçak toplara yetişince "Duvar uzanışı!" yazmalı. Maç sonunda Ayrıntılar'ı aç: "Tarzın N kez fark yarattı" satırı olmalı. Dengeli'de bu satır çıkmamalı.

Bu öğleden sonra (7 Kasım 14:30 inceleme turu):
- Bir parmağın ekranda dururken (ör. sol başparmak kenara dayalı) öbür parmakla yaptığın kaydırma artık vuruş sayılıyor; eskiden bazen hiç vurmuyordu.
- Hızlı maçta yenilince ana düğme "Rövanş" oluyor: aynı rakiple, aynı sahada yeniden oynarsın. Kazanınca eskisi gibi yeni rakip gelir.
- Ralli sürerken gelen akış, imzayı okudun ve görev tamam titreşimleri kısaldı; vuruş titreşimin artık onların içinde kaybolmuyor. Uzun titreşim yalnız seviye atlama, tur finali, rekor gibi maç dışı anlarda.
- Maç sonu Ayrıntılar'da sıfır olan sayılar ("0 aldatma" gibi) artık yazılmıyor.
Nasıl denenir: Telefonu iki elle tut, sol başparmağını ekranın sol kenarında bırak ve sağ başparmakla smaç kaydır: vuruş gelmeli. Hızlı maçta bir maç kaybet: düğmede "Rövanş · rakibin adı" yazmalı, basınca aynı rakip gelmeli. Bir maç kazan: düğme "Bir maç daha" olmalı. Maç sonunda Ayrıntılar'ı aç: yalnız yaptığın vuruş türleri sayılmalı.

Bu öğlen (7 Kasım 12:30 inceleme turu):
- Servis anındaki "Servis sende · sağ kare" yazısı artık yalnız ilk maçlarda (3 galibiyete kadar) çıkıyor. Sonrasında kortun altındaki kutu yalnız söyleyecek yeni bir şey varken görünüyor: hata, kural maçı uyarısı ya da maçın ilk servisindeki görevler.
- Servis beklerken arka planda boşa iş yapan yazı kutusu sadeleşti; ralli içindeki küçük takılmalar azalmalı.
Nasıl denenir: 3 galibiyeti geçmiş bir hesapla maç oyna: ilk serviste yalnız "Görev: …" satırı görünmeli, sonraki servislerde kortun altında yazı olmamalı. Servis hatası yap: uyarı yine çıkmalı. Kariyerde Birmingham'ı oyna: altın ralli uyarısı servisten önce görünmeli.

Bu sabah (7 Kasım 10:30 inceleme turu):
- Sevinç artık sayının değerine göre: kendi kazanan vuruşunla ya da 8 vuruştan uzun bir ralliyle aldığın sayıda seçtiğin sevinç tam oynar, rakip topu dışarı ya da fileye atınca kısa sürer. Uzun bir ralliyi kaybeden oyuncu kısaca başını eğer. Sayılar arası tempo hızlandı.
- Kariyerde her turda bir "kural maçı" var: Seul'de Uzun ralli, Birmingham'da Altın ralli, Singapur çeyrek finalinde Yalnız file. Kural maç kartında ve ana sayfadaki Oyna düğmesinin altında yazıyor.
Nasıl denenir: Bir maç oyna: smaçla aldığın sayıda karakterin sevinci tam oynamalı, rakibin dışarı attığı topta kısa kesilmeli. Uzun bir ralliyi kaybedince karakterin kısaca başını eğdiğini gör. Kariyer haritasında Asya Turu'nda Seul'ü aç: kartta "Uzun ralli · 8 ve daha uzun rallinin sayısı 2" satırı olmalı ve maçta uzun ralli 2 sayı vermeli.
Bu sabah (7 Kasım 08:30 inceleme turu):
- Dört beceri unvanının artık üç kademesi var: Smaç ustası, Geçilmez, Aldatıcı ve Okuyucu, oynadıkça II ve III olur (ör. 400 aldatmada Aldatıcı II). Kademe güç vermez, yalnız unvanın yanında görünür; yeni kademe maç sonunda "Yeni unvan" diye haber verilir.
- Profil'de İstatistikler bölümü üstteki üç büyük sayıyı (rekor ralli, smaç, galibiyet) tekrar etmiyor, sayfa kısaldı.
Nasıl denenir: Profil'i aç ve açılmış bir beceri unvanını seç: altında "sonraki kademe 85/400" gibi tek satır görünmeli ve birkaç maçtan sonra sayı artmalı. İstatistikler'de rekor ralli, galibiyet ve smaç olmamalı; bunlar en üstteki kartta duruyor olmalı. Ayarlar'da metni büyüt, Profil taşmamalı.

Bu sabah (7 Kasım 06:30 inceleme turu):
- Atölye'de model kartları sadeleşti: her kartta beş çubuk yerine modelin artısını ve eksisini söyleyen tek cümle var. Beş çubuk sayfanın üstündeki "Takımın şimdi" kartında duruyor; model seçince o kart değişiyor.
- Çevrimiçi maç için altyapı (henüz kapalı): servisi atmayan oyuncu için süre ve maçtan çekilme kuralı.
Nasıl denenir: Atölye'yi aç, Raket'e gir: model kartları eskisinden kısa olmalı, her kartta açıklama cümlesi ve düğme olmalı, çubuk olmamalı. Başka bir model seç: üstteki "Takımın şimdi" çubukları değişmeli. Ayarlar'da metni büyüt ve aynı sayfayı aç: kartlar taşmamalı.

Bu gece (7 Kasım 04:30 inceleme turu):
- Rakipler artık sayı alınca kendi sevinçlerini yapıyor: her rakibin kendine özgü sayı ve maç sonu hareketi var (önce hepsi aynı yumruğu yapıyordu).
- Oynayışından kazanılan dört yeni unvan: Smaç ustası, Geçilmez, Aldatıcı, Okuyucu. Profil'de kilitli unvanların yanında ne kadar kaldığı yazıyor; açıldığı maçın sonunda kart haber veriyor.
Nasıl denenir: Kariyerde iki farklı rakiple oyna, rakip sayı aldığında yaptığı hareket rakipten rakibe değişmeli. Profil'i aç, Unvan bölümünde yeni dört unvan kilitli görünmeli ve yanlarında "12/300" gibi ilerleme olmalı; birkaç maçtan sonra sayı artmalı.

Bu gece (7 Kasım 02:30 inceleme turu):
- Mükemmel smaçta yazı artık topun hızını da söylüyor ("SMAÇ! · 187 km/s"); raket gücü ve sıçrayarak smaç sayıya yansır. Maç sonu Ayrıntılar'da en hızlı sayı smacın ve kişisel rekorun.
- Hızlı maç sayfasında saha seçimi tek sırada, her sahanın küçük resmiyle (Salon, Turnuva, Okul, Gece).
- İlk smaçta ve ilk parlamada olabilecek kısa takılma giderildi (efektler açılış ve maç perdesinin arkasında hazırlanıyor).
Nasıl denenir: Uygulamayı kapatıp yeniden aç, ilk maçta ilk sert smacı vur: oyun takılmamalı. Mükemmel zamanlamayla smaç vur: yazının yanında km/s görünmeli. Maç sonunda Ayrıntılar'ı aç: ilk satırda "En hızlı smaç … km/s" olmalı. Hızlı maç sayfasında dört saha tek sırada küçük resimleriyle görünmeli, seçtiğinde ✓ gelmeli.

Bu gece (7 Kasım 00:30 inceleme turu):
- Kaybettiğin maçın sonunda ipucu artık rakibe özel olabilir: rakip sayıların çoğunu smaçla ya da file önüne bırakarak aldıysa adıyla söyler ve tek bir çare verir.
- Ayarlar › Yardım'da "Gizlilik politikası" bağlantısı (tarayıcıda açılır) ve en altta sürüm numarası.
- Zorluk seçimi Ayarlar'dan kalktı; Hızlı maç sayfasında ve kariyer kartında duruyor.
- Duraklatınca oyun arka planda daha az çizim yapar (pil ve ısınma için); duraklat menüsü her zamanki gibi tepki verir.
Nasıl denenir: Kariyerde bir maçı bilerek kaybet, rakibin smaçlarını karşılama: sonuç kartındaki ipucunda rakibin adı geçmeli. Ayarlar'ı aç: Zorluk satırı olmamalı, Yardım'ın altında "Gizlilik politikası ↗" Safari'de açılmalı, altında "Sürüm 1.0" yazmalı. Maçta duraklat: menü düğmeleri hemen tepki vermeli.

Bu gece (6 Kasım 22:30 inceleme turu):
- Sonsuz rallide eski rekorunu geçtiğin vuruşta "Yeni rekor!" yazısı, yükselen iki nota ve titreşim; top hızlanınca kısa bir tık sesi ve hafif titreşim.
- Sonsuz ralli sayacı sade: ralli sırasında yalnız vuruş sayın ve rekorun ("12 vuruş · Rekor 20"); rekoru geçince "Yeni rekor" yazar.
- Kortta aynı anda en çok iki yazı çıkar; sayının neden bittiği (dışarı, ıska nedeni) küçük vuruş etiketlerinin altında kaybolmaz.
- Oyun şimdilik yalnız iPhone için (iPad'de iPhone görünümüyle açılır).
Nasıl denenir: Antrenman › Sonsuz ralli'yi bir kez oyna (rekor olsun), sonra tekrar oyna ve rekoru geç: geçtiğin anda yazı, ses ve titreşim tek sefer gelmeli. Her 5 vuruşta "Hızlanıyor" ile birlikte tık sesi duyulmalı. Normal maçta uzun rallilerde üst üste en çok iki yazı görmelisin.

Bu akşam ayrıca (6 Kasım inceleme turu):
- Maçta ekranın alt kenarından güçlü smaç kaydırması artık uygulamayı kapatmıyor, oyunu durdurmuyor (ev çubuğu ilk kaydırmada yalnız belirir).
- Akış bitince kısa, yumuşak bir "söndü" sesi ve tek tık titreşim; altın çizgiler bir anda değil, yavaşça kayboluyor.
- Vuruş sesi parmakla daha aynı anda gelir (ses gecikmesi kısaldı).
- Ayarlar'daki "Antrenmanı yeniden oyna" kalktı; aynı ders Antrenman sekmesinde "Temel ders", Nasıl oynanır'da "Temel dersi oyna".
Nasıl denenir: maçta alttan hızlı yukarı 10 smaç at, uygulama arka plana düşmemeli; üstten aşağı kaydırınca Denetim Merkezi ilk denemede açılmamalı, ikincide açılmalı. Menüde kenar hareketleri her zamanki gibi. Üst üste 4 tam orta ile akışa gir, sonra sıradan bir vuruşla boz: iki notalık inen ses ve çizgilerin sönmesi. Müzik ve alkışta çıtırtı olmamalı.

Bu sürümde yeni: yazılar daha kısa ve daha okunur; maç içi ipucu koyu zeminde.

Bu sürümde yeni:
- Ayarlar'da zorluk ve maç uzunluğu düğmeleri kısa ("Kolay", "Usta", "5 sayı"…); ne işe yaradıkları altında tek satırda.
- Antrenman görev sayfasının başlığı artık ikiye bölünmüyor; alttaki düğme kısaca "Yeniden oyna" ya da "Başla".
- Antrenman ve öğretici yazılarında büyük harfle bağırma yok ("HIZLI kaydır" yerine "hızlı kaydır").
- Profil'de sonraki seviyede açılacaklar türüne göre gruplu ("Saç modeli: Kazıma, Kel").
- Maç başındaki "Top parlayınca kaydır" ipucu koyu bir zeminde, beyaz ve biraz daha büyük.

Nasıl denenir:
1. Ayarlar'ı aç: Zorluk'ta "Kolay" ve "Usta" yazmalı, altında seçtiğinin açıklaması olmalı. Kolay seçiliyken "Oyuncun topa kendiliğinden koşar" yazmalı. Maç uzunluğunda da aynı.
2. Oyna › Antrenman › bir göreve dokun (ör. Köşe smacı): başlık tek satır olmalı, alttaki düğme "Yeniden oyna" ya da "Başla" demeli.
3. Görev açıklamalarında büyük harfli kelime kalmamalı.
4. Profil'de "Sv n ile açılır" başlığının altında açılacaklar türüne göre gruplu görünmeli.
5. Bir maça başla: alttaki ipucu yazısı koyu bir kutunun içinde rahat okunmalı.
6. Ayarlar'dan yazıyı büyüt: bu ekranlar yine sığmalı.

Önceki sürümden (Hedef eşya ve maç sonu):
- İstediğin eşyayı hedef yap, maç sonu kartında ne kadar yaklaştığını gör; "Hedefin hazır" seni eşyaya götürür.
- Maç sonu kartı ve turuncu düğme her maçtan sonra yerinde; kazanınca oyuncun sana dönüp sıçrıyor, inişte altın halka.

Eski not (Hedef eşya ayrıntısı):
- Dolap'ta Tüy'ün yetmeyen bir eşyayı denerken altta "Hedef yap" düğmesi çıkıyor (Atölye'deki ekipman modellerinde de). Bir seferde tek hedef olur; yenisini seçersen eskisinin yerine geçer.
- Maç sonu kartında hedefin bir çubukla görünüyor: "Hedef: eşyanın adı · kaç Tüy kaldı". Kazandığın Tüy uçarken çubuk da doluyor.
- Tüy'ün yettiği maçta kartta "Hedefin hazır" düğmesi çıkıyor; seni eşyanın olduğu yere götürüyor, satın almayı sen yapıyorsun.
- Seviyesi gelmemiş eşyalar hedef yapılamıyor; seviye ve Tüy ayrı kalıyor.
- Maç sonu kartındaki ödül yazısı kısaldı: en büyük ödül ve "+n ödül daha". Tam liste "Ayrıntılar"ın en başında.

Nasıl denenir:
1. Dolap'ta Tüy'ün yetmeyen bir şapka ya da pelerin dene: altta "Hedef yap" düğmesi olmalı. Dokun: düğme "Hedefin · n Tüy kaldı" olmalı.
2. Bir maç oyna: sonuç kartında Tüy ve XP satırlarının altında "Hedef: …" yazısı ve çubuk olmalı; kalan Tüy sayısı tüyler uçarken azalmalı.
3. Hedefe yetecek kadar Tüy kazandığın maçta "Hedefin hazır" düğmesi çıkmalı; dokununca Dolap açılmalı ve eşya altta "… Tüy ile al" diye beklemeli.
4. Eşyayı al: sonraki maçta hedef satırı olmamalı.
5. Seviyesi gelmemiş bir eşyayı dene: "Hedef yap" çıkmamalı, "Sv n'de açılır" yazmalı.
6. Atölye'de bir yuvanın (raket, kordaj…) Tüy'ü yetmeyen modelinde de "Hedef yap" olmalı.
7. Birden çok ödül aldığın maçta (günün ilk galibiyeti, yeni yıldız…) kartta yalnız en büyüğü ve "+n ödül daha" yazmalı; "Ayrıntılar"a dokununca hepsi "Ödüller:" satırında görünmeli.
8. Ayarlar'dan yazıyı büyüt ve bir maç kazan: hedef satırı ve düğmeler ekrana sığmalı.

Önceki sürümden (maç sonu):
- Maçı kazanınca kamera oyuncuna dönüyor; sonuç kartı altta, Tüy ve toplam aynı satırda, seviye atlayınca turuncu şerit.
- Altta tek büyük düğme ve en çok bir ikinci düğme; "Ekipmanı yükselt" yalnız o maçla yükseltme alınabilir olduysa.

Önceki sürümden (Dolap: Sırt ve şapkalar):
- Sırt kategorisi (pelerinler), altı yeni şapka, nadirlik, parlayan saç boyaları.

Önceki sürümden (Usta ve Sevinç):
- Usta'da akıllı adım, merkeze dönüş ve duruş ayak izi; Dolap'ta Sevinç kategorisi (sayı ve maç sonu hareketi).
- Usta'da erişim halkaları, vuruşta koşabilme; Dolap'ta kombin yuvaları, "Hepsini al", "Oyuncunu seç".
- Uygulamaya dokununca dönen tüy top hemen başlıyor; maça geçiş akıcı.
