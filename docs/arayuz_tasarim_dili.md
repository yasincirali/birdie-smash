# Birdie Smash arayüz tasarım dili

Kaynak: yasin 14 Kasım 2026 ("uygulama genelinde simetri, üst üste binen yazı vs kontrol edelim, tasarım dili kuralları için bir MD oluştur, ona göre yapsın"). Menü (UI Toolkit, `Game/UI/`, `Resources/ui/menu_stil.uss`) ve maç HUD'u (IMGUI, `Game/Hud.cs`) için **her** ekran değişikliği bu kurallara uyar. Kozmetik eşya dili ayrıdır: `docs/kozmetik_tasarim_dili.md`.

Kural ihlali bir tasarım tercihi değil, hatadır. Bir kuraldan bilerek sapılırsa nedeni kodda karar yorumu olarak yazılır ve PR'ın "HIG kontrolü" bölümünde söylenir.

## 1. Yazı asla üst üste binmez (en üst kural)

1. İki yazı, ya da bir yazı ile bir çubuk, simge, rozet veya düğme kenarı, hiçbir dilde (TR, EN) ve hiçbir yazı boyunda (×1, ×2) kesişmez.
2. **Etiket + değer satırı** (`Satir("arali")`): soldaki etiket daralmaz ve kırılmaz (`flexShrink 0`, `white-space: nowrap`). Sağdaki değer daralır ve gerekirse kendi içinde sağa yaslı kırılır (`minWidth 0`). Satırın yüksekliği içerikten gelir; sabit `height` verilmez.
3. **Yazı + çubuk**: çubuk yazının altında, kendi satırında başlar ve aralarında en az 6 pt boşluk olur. Çubuk yazı satırının içine `position: absolute` ile konmaz.
4. **Yüzen yazılar (HUD)**: aynı anda en fazla 2 yazı görünür (`YAZI_EN_COK`). Her karede yerleşim yapılır (`Hud.YerBul`): önce önemli olan yerleşir, çakışan yazı onun altına, yer yoksa üstüne kayar.
5. **Kart ve banner yazıları**: tek satır olması gerekiyorsa `Sigdir` ile küçülür. Boyun alt sınırı 11 pt (`YAZI_MIN`); daha da sığmıyorsa metin kısaltılır, iki satıra kırılıp başka öğenin üstüne taşmaz.
6. **Çeviri payı**: İngilizce metin Türkçeden %30 uzun olabilir; yerleşim buna göre kurulur. Yeni metinler iki dilde de en dar ekranda (375 pt) denenir.

## 2. Simetri ve hizalama

1. **Yan boşluklar eşit**: ekran içeriğinin sol ve sağ boşluğu aynıdır (telefonda 16 pt, iPad'de `Duyarli` sütunu ortalanır). Bir kart ya da satır, ekranın bir kenarına diğerinden yakın durmaz (en fazla 1 pt fark).
2. **Düğme ve çip içi ortalama**: simge ve yazı düğmenin içinde yatayda ve dikeyde ortadadır. Simge + yazı birlikteyse ikisi bir grup olarak ortalanır, simge yazının dikey ortasında durur.
3. **Yuvarlak çerçeveler**: avatar, yüz, kupa ve mühür halkanın tam ortasındadır. Yüz portresinde göz çizgisi halkanın üst yarısında, çene halkanın içinde kalır.
4. **Eşit öğe dizileri** (zorluk çipleri, maç noktaları, 2×2 rakip ızgarası): öğelerin genişlikleri ve aralıkları eşittir; dizinin tamamı kapsayıcısında ortalanır ya da iki kenara yaslanır, yarım kalmış bir son satır sola yığılmaz.
5. **Sol kenar çizgisi**: aynı kartta başlık, açıklama, çubuk ve düğme aynı sol kenardan başlar. Çubuklar (`stat-iz`) yazıdan içeride başlamaz.
6. **Sayılar**: tablo ve sayaçlarda sayılar sağa yaslıdır, değişen sayı yerleşimi kaydırmaz (sabit genişlik ya da `min-width`).

## 3. Boyut, boşluk, renk

- **Dokunma alanı** ≥ 44 × 44 pt (HUD `BH`, menü `.dugme` en az 44). Komşu dokunma alanları arasında ≥ 6 pt boşluk.
- **Boşluk ölçeği**: 4, 6, 8, 12, 16, 24 pt. Ara değer yalnız gerekçeli (karar yorumu).
- **Palet**: "Gün Batımı" sabit (`menu_stil.uss` `:root`). Turuncu `--sun` = seçili/birincil eylem, limon `--lime` = sen, orkide `--coral` = rakip. Yeni ham renk yazılmaz.
- **Anlam yalnız renkle verilmez**: seçili durumda ✓ ya da çerçeve; kilitte kilit simgesi ve "Sv 10 ile açılır" yazısı.
- **Kontrast**: gövde yazısı zemine karşı ≥ 4,5:1, büyük kalın yazı ≥ 3:1.

## 4. Hareket

- Menü geçişleri 150–300 ms. Hareketi azalt açıkken kayma ve ölçek yok, yalnız solma.
- Döndürme ve takla gibi süs hareketi yok. Bir hareket bir anlam taşır (geldi, seçildi, kazandın).

## 5. Doğrulama (her UI PR'ında)

1. Mono derleme (`birdie-unity` merdiveni).
2. Ekran turu: `ekran/kisa-yazi-*` dalı menü turunu TR + EN, ×1 + ×2 yazı boyunda çeker. Tur `yazi_tasma.txt` (kesik, taşma, bölünen kelime, çevrilmemiş) ve `yazi_cakisma.txt` (yazı–yazı ve yazı–çubuk kesişmesi, yan boşluk asimetrisi) dosyalarını yazar. İkisinin de `toplam 0` olması beklenir; sıfır değilse PR'da nedeni açıklanır.
3. Değişen ekranın bir karesine bakılır (küçük, tek kare).
4. PR'da "HIG kontrolü" bölümü yazılır.
