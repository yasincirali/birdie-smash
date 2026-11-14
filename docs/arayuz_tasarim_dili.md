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

## 6. Bileşen standardı ve cam yüzey

Kaynak: yasin 14 Kasım 2026 gece ("menü kısımlarında butonları, listelerin yerleşimlerini vs hepsini standartlara oturtup, glassmorphism falan kullanıp; MD dosyalarında netleştirelim, sonraki ekleme ve update'ler bu standartlara göre yapılmalı"). Yeni ekran ya da değişiklik aşağıdaki bileşenlerden kurulur. Tabloda olmayan bir bileşen gerekiyorsa önce buraya eklenir, sonra koda yazılır.

### 6.1 Yüzeyler (üç seviye)

| Seviye | Ne için | USS | Değer |
|---|---|---|---|
| Zemin | Sayfa arkası (gece moru + üstte soluk güneş) | `.sayfa`, `.kok.koyu` | opak, cam değil |
| Cam kart | Kart, liste grubu, karo, yol/dal satırı | `.kart`, `.karo`, `.sv-yol`, `.gel-dal` | `--cam` (.80), üst kenar `--cam-ust`, yan `--cam-kenar`, alt `--cam-alt` |
| Cam düğme / hap | İkincil düğme, simge düğme, üst haplar (seviye, Tüy) | `.dugme`, `.ikon-dugme`, `.ust-hap` | `--cam2` (.72), basılı `--cam3` |
| Sekme çubuğu | Alttaki 5 sekme | `.sekme-cubugu` | .86, üstte ışık çizgisi |
| Alt sayfa | Uzun okuma, seçim listesi | `.alt-sayfa` | .97 (neredeyse opak), üstte ışık çizgisi |

Cam kuralları:
1. UI Toolkit'te arka plan bulanıklığı yok. Cam, yarı saydam mor dolgu + üstte açık + altta koyu kenar çizgisiyle kurulur. Gölge, parıltı ya da ışık süpürmesi eklenmez.
2. Opaklık okunurluk içindir. Kart .80'in, düğme .72'nin altına inmez. Arkada salon resmi ya da güneş varken gövde yazısı ≥ 4,5:1 kalır.
3. Cam yalnız menüde kullanılır. Maç HUD'u kortun üstünde okunurluk kuralına bağlıdır (`Hud.cs`) ve cam değildir.
4. Cam içinde cam olmaz. Kartın içindeki düğme `--cam2` olabilir, kartın içinde ikinci bir `.kart` olmaz; iç gruplama boşluk ve ayırıcıyla yapılır.
5. Yeni ham renk yazılmaz, yalnız `--cam*` değişkenleri kullanılır. Koddan üretilen dokular (`UiOge.Plaka`) da aynı iki değeri kullanır (`CAM_UST` / `CAM_ALT`).

### 6.2 Düğmeler

| Tür | Yükseklik | Köşe | Dolgu | Kural |
|---|---|---|---|---|
| Birincil (`.dugme--birincil`) | 56 | 16 | turuncu `--sun`, koyu yazı | Ekranda **bir** tane. Tam genişlik, içeriğin altında. |
| İkincil (`.dugme`) | ≥ 48 | 12 | `--cam2` | Birincilin yanında ya da kartta eylem. |
| Simge düğme (`.ikon-dugme`) | 44 × 44 | 22 (daire) | `--cam2` | Erişilebilirlik etiketi zorunlu. |
| Küçük (`.kucuk-dugme`) | ≥ 44 dokunma | 12 | `--cam2` | Satır içi (Değiştir, Gör). Görünen yazı küçük olsa da dokunma alanı 44. |
| Pasif (`.dugme--pasif`) | türünün boyu | aynı | koyu | Kilit simgesi + nedeni yazar ("Sv 10 ile açılır"). |

Düğme yazısı tek satır. Sığmazsa önce kısa metin aranır; kırılırsa iki satır ortalı ve düğme yüksekliği büyür, yazı kenara değmez. Basılınca %98 ölçek + bir seviye açık dolgu (80 ms). Hareketi azaltta ölçek yok.

### 6.3 Çipler ve seçim

- Çip (`.cip`) ≥ 44 yükseklik, köşe 12, aralık 8. Seçili: turuncu 2 pt çerçeve + hafif turuncu dolgu + kalın yazı + ✓ simgesi (§3: anlam yalnız renkle verilmez).
- Bir sırada 2–4 çip, eşit genişlik (§2.4). 5 ve üstü ise yatay kaydırma değil, iki satır ya da alt sayfa.

### 6.4 Listeler

Standart liste satırı (`.liste-satir`):

```
[ simge 28 ] 12 [ başlık (kalın)          ] 12 [ değer / ✓ / › ]
                [ alt satır (soluk, ≤2)   ]
```

- Satır en az 56 yükseklik, iç boşluk 10 × 14; satırın tamamı dokunulabilir.
- Simge kutusu 28 × 28, solda, dikey ortada. Simgesiz satırda başlık aynı sol kenardan başlar (16).
- Metin sütunu daralır (`minWidth 0`), sağ sütun daralmaz. Sağda yalnız bir şey: değer, ✓, kilit ya da ›.
- Satırlar bir cam kartın içinde gruplanır. Aralarında 1 pt ayırıcı (`.liste-ayirac`), simgenin sağından başlar (inset 54). Grubun son satırında ayırıcı yok.
- Grup başlığı (`.bolum-baslik`): kartın dışında, üstte, soluk kalın küçük yazı. Üstte 18, altta 6 boşluk.
- Uzun liste (> 8 satır) alt sayfa ya da ayrı sayfadır. Ana sekmenin içine sonsuz liste konmaz.

### 6.5 Sayfa iskeleti

1. Üst şerit: solda seviye hapı, sağda Tüy hapı + ayarlar simge düğmesi; yükseklik 44, yan boşluk 16.
2. Sayfa başlığı (`.sayfa-baslik`, Baloo): üst şeridin altında, solda.
3. İçerik: kartlar arası 12, kart içi 14. Ana eylem (birincil düğme) içeriğin sonunda ya da sekme çubuğunun hemen üstünde, tek.
4. Sekme çubuğu: 5 sekme, seçili sekme turuncu simge + yazı + üst çizgi.
5. Alt sayfa: tutamak + başlık + içerik + (varsa) tek birincil eylem; aşağı kaydırarak ya da dışına dokunarak kapanır.

### 6.6 Köşe ve boşluk ölçeği

- Köşe: 8 (rozet, küçük hap), 12 (düğme, çip, liste satırı), 16 (kart, birincil düğme), 20 (alt sayfa), 22 (yuvarlak hap / simge düğme). Ara değer yazılmaz.
- Boşluk: §3 ölçeği (4, 6, 8, 12, 16, 24). Kartlar arası 12, ekran yan boşluğu 16.

### 6.7 Değişiklik süreci

1. Yeni ekran önce bu bölümdeki bileşenlerle kurulur. Yeni bir sınıf gerekiyorsa adı ve kuralı buraya eklenir.
2. Eski ekranlar dokunuldukça bu standarda taşınır; aynı PR'da o ekranın listeleri `.liste-satir`'a, düğmeleri tablodaki türlere çevrilir.
3. Doğrulama §5'teki gibidir. Ek olarak değişen ekranın karesinde camın okunurluğuna bakılır (arkada salon resmi ya da güneş varken).
