# Kozmetik tasarım dili (Dolap eşyaları)

yasin, 2026-11-12: "Tasarım dilimiz ne olmalı; dilimiz dışında absürt tasarımlar görmemeliyiz. Aşırı detaylı,
render sorunu yaratacak şeyler olmamalı. Ama görsel olarak güzel ve sevimli gözükmeli."

Dil tek cümleyle: **tombul oyuncak**. Oyuncu modeli iri başlı, yuvarlak hatlı bir oyuncak figür; üstüne takılan her
şey aynı kutudan çıkmış gibi durur.

## Kurallar

1. **Biçim.** İri, yuvarlak, az parçalı. Bir eşya en çok üç dört büyük kütleden oluşur; uzaktan (maçta oyuncu
   ekranın küçük bir parçası) silueti tek bakışta okunur. İnce tel, kıl gibi çubuk, kâğıt gibi düz levha yok;
   en ince parça birim başın/kolun ~%4'ünden kalın. Sivri uçlar yuvarlatılır.
2. **Renk.** Düz, doygun, dokusuz. Eşya başına en çok **4 renk**; yakın tonlar tek renge iner. Desen gerekiyorsa
   iri bloklarla (şerit, benek) verilir, ince çizgiyle değil.
3. **Konu.** Neşeli ve tanıdık: yiyecek, oyuncak, hayvan, bayram, masal, dünya kıyafetleri, spor. Absürtlük
   konudan gelir (başta tencere, kolda pizza), biçim dili aynı kalır. Girmez: ölüm ve korku çağrışımı (tabut,
   mezar taşı, kan), gerçekçi silah, yazı/logo/marka, dinî ya da siyasi simge, bir kültürü küçük düşüren karikatür.
4. **Oturma.** Eşya gövdeye girmez, havada da durmaz; tutunduğu yer okunur (bant, kayış, kemer). Dört oyuncak
   tipinde yerel önizleme (`tool/karakter/aks_giy.py`) ve oyun içi ekran turu olmadan kataloğa girmez.
5. **Detay bütçesi.** En çok **600 üçgen** ve 4 renk (renk başına bir çizim çağrısı). `tool/karakter/ozel_aksesuar.py`
   bunu zorlar: renk fazlaysa en yakın tonları birleştirir, üçgen fazlaysa eşyayı daha kaba bölüntüyle yeniden
   kurar, yine sığmazsa "SIĞMADI" yazar ve eşya elle sadeleştirilir.
6. **Yalnız görünüş.** Hiçbir eşya vuruşu, hızı, savunmayı değiştirmez; yeni model dosyası eklenmez.

## Yeni eşya eklerken sor

- Uzaktan ne olduğu anlaşılıyor mu? Küçük ayrıntıyı silince bir şey kayboluyor mu?
- Aynı raftaki öteki eşyalarla aynı oyuncak kutusundan çıkmış gibi mi?
- Bir çocuk bunu sevimli bulur mu, yoksa ürker mi?
