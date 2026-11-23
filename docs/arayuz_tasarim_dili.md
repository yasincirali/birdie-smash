# Birdie Smash arayüz tasarım dili

> Son güncelleme 15 Kasım 2026 (yasin: "UI menüleri kolaylaştırma, anlaşılır kılma ve estetik olarak güzel gözükme hedefimiz var; Apple HIG ve glassmorphism kullanabilirsin, renk seçimleri için best practice'lerden örnek al; sonraki update'ler buradan örnek alsın"): §7 sadelik ve anlaşılırlık, §8 renk, §9 ekran denetim listesi, §10 gerçek tasarım (AI slop yok) eklendi; §6.1'e katman sırası.

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

Cam kuralları (Apple HIG "Materials" + Liquid Glass ilkesiyle, 15 Kasım güncellemesi):
0. **Katman sırası**: içerik (zemin, kartlar, listeler) altta; gezinme ve kontroller (üst haplar, sekme çubuğu, alt sayfa tutamağı, yüzen düğmeler) üstte. En belirgin cam üst katmandadır: üst haplar ve sekme çubuğu içerikten bir tık daha açık ve parlak kenarlı görünür, kart camı daha sakin kalır. Göz önce nerede olduğunu (gezinme), sonra ne yapacağını (içerik) okur.
1. UI Toolkit'te arka plan bulanıklığı yok. Cam, yarı saydam mor dolgu + üstte açık + altta koyu kenar çizgisiyle kurulur. Gölge, parıltı ya da ışık süpürmesi eklenmez.
2. Opaklık okunurluk içindir. Kart .80'in, düğme .72'nin altına inmez. Arkada salon resmi ya da güneş varken gövde yazısı ≥ 4,5:1 kalır.
3. Cam yalnız menüde kullanılır. Maç HUD'u kortun üstünde okunurluk kuralına bağlıdır (`Hud.cs`) ve cam değildir.
4. Cam içinde cam olmaz. Kartın içindeki düğme `--cam2` olabilir, kartın içinde ikinci bir `.kart` olmaz; iç gruplama boşluk ve ayırıcıyla yapılır.
5. Yeni ham renk yazılmaz, yalnız `--cam*` değişkenleri kullanılır. Koddan üretilen dokular (`UiOge.Plaka`) da aynı iki değeri kullanır (`CAM_UST` / `CAM_ALT`).

### 6.2 Düğmeler

| Tür | Yükseklik | Köşe | Dolgu | Kural |
|---|---|---|---|---|
| Birincil (`.dugme--birincil`) | 56 | 16 | sarı `--sun`, mürekkep yazı ve kontur | Ekranda **bir** tane. Tam genişlik, içeriğin altında. |
| İkincil (`.dugme`) | ≥ 48 | 12 | `--cam2` | Birincilin yanında ya da kartta eylem. |
| Simge düğme (`.ikon-dugme`) | 44 × 44 | 22 (daire) | `--cam2` | Erişilebilirlik etiketi zorunlu. |
| Küçük (`.kucuk-dugme`) | ≥ 44 dokunma | 12 | `--cam2` | Satır içi (Değiştir, Gör). Görünen yazı küçük olsa da dokunma alanı 44. |
| Pasif (`.dugme--pasif`) | türünün boyu | aynı | koyu | Kilit simgesi + nedeni yazar ("Sv 10 ile açılır"). |

Düğme yazısı tek satır. Sığmazsa önce kısa metin aranır; kırılırsa iki satır ortalı ve düğme yüksekliği büyür, yazı kenara değmez. Basılınca %98 ölçek + bir seviye açık dolgu (80 ms). Hareketi azaltta ölçek yok.

### 6.3 Çipler ve seçim

- Çip (`.cip`) ≥ 44 yükseklik, köşe 12, aralık 8. Seçili: sarı dolgu + mürekkep 2 pt kontur ve yazı + ✓ simgesi (§3: anlam yalnız renkle verilmez).
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
4. Sekme çubuğu: 5 sekme, beyaz çubuk; seçili sekme sarı hap + mürekkep simge ve yazı (22 Kas Stadyum Pop).
5. Alt sayfa: tutamak + başlık + içerik + (varsa) tek birincil eylem; aşağı kaydırarak ya da dışına dokunarak kapanır.

### 6.6 Köşe ve boşluk ölçeği

- Köşe: 8 (rozet, küçük hap), 12 (düğme, çip, liste satırı), 16 (kart, birincil düğme), 20 (alt sayfa), 22 (yuvarlak hap / simge düğme). Ara değer yazılmaz.
- Boşluk: §3 ölçeği (4, 6, 8, 12, 16, 24). Kartlar arası 12, ekran yan boşluğu 16.

### 6.7 Değişiklik süreci

1. Yeni ekran önce bu bölümdeki bileşenlerle kurulur. Yeni bir sınıf gerekiyorsa adı ve kuralı buraya eklenir.
2. Eski ekranlar dokunuldukça bu standarda taşınır; aynı PR'da o ekranın listeleri `.liste-satir`'a, düğmeleri tablodaki türlere çevrilir.
3. Doğrulama §5'teki gibidir. Ek olarak değişen ekranın karesinde camın okunurluğuna bakılır (arkada salon resmi ya da güneş varken).

## 7. Sadelik ve anlaşılırlık

Kaynak: yasin 15 Kasım 2026. Amaç: oyuncu bir menüyü ilk açışta 3 saniyede "burada ne yaparım" sorusunu yanıtlar. Hedef kitle telefonda kısa seans oynayan arcade spor oyuncusu (Badminton League, Tennis Clash bilen); menüde değil sahada vakit geçirmek ister.

1. **Ekran başına tek iş.** Her sekme ve alt sayfa tek cümleyle anlatılabilir ("Oyna: maça gir", "Dolap: oyuncunu ve raketini giydir"). İkinci bir iş varsa alt sayfaya ya da başka sekmeye gider.
2. **Tek birincil eylem** (§6.2): sarı dolgu ekranda bir kez. Göz önce ona gider; diğer her şey lacivert ikincil ya da metin bağlantısı.
3. **Görünür öğe bütçesi**: ilk ekranda (kaydırmadan) en fazla 1 birincil eylem, 3–4 kart ya da liste grubu, 7 ± 2 dokunulabilir öğe. Fazlası kaydırmanın altına ya da alt sayfaya.
4. **Kademeli açma** (progressive disclosure): ayrıntı (stat tablosu, kural açıklaması, uzun liste) varsayılan olarak kapalı; tek satır özet + `›` ile açılır. Kural metni her açılışta tekrar edilmez (bir kez gösterilir ya da tek kısa satıra iner).
5. **Metin**: başlık 1–3 kelime, açıklama tek satır, cümle düzeni (Baloo başlık hariç büyük harf yok). Düğme yazısı fiil ("Oyna", "Giy", "Yükselt"), tek kelime tercih. "Tamam/Evet" yerine eylemin adı. Aynı şey her ekranda aynı adla anılır (Tüy, Sv, Dolap, Atölye).
6. **Durum hep görünür**: seçili, kilitli, yeni açılan ve yetersiz Tüy durumları ikon + yazıyla aynı yerde ve aynı biçimde. Kilitte "neden" ve "ne zaman" (Sv 10 ile açılır) birlikte.
7. **Geri dönüş ve güven**: geri alınamayan işte (satın alma, sıfırlama) onay; her şey geri alınabiliyorsa onay yok, anında uygulanır. Deneme (vitrinde giy) satın almadan ayrı ve açıkça "deneme" diye yazar.
8. **Dokunuşta anında karşılık**: her dokunuş 100 ms içinde görünür tepki (basılı durum, seçim, ses/hap). Ağır iş (kayıt, figür kurulumu) perde arkasına (`Ayar.KaydetSonra`, CLAUDE.md "takılma yok").
9. **Sistem kalıbı önce**: alt sayfa aşağı çekilince kapanır, sekmeler hep altta, geri sol üstte, basılı tutma tekrar eder (stepper), kaydırma yerini korur. Yeni bir etkileşim icat edilmeden önce iOS'taki karşılığı aranır.
10. **Sessiz süs yok**: yalnız anlam taşıyan hareket ve parıltı (§4). "AI slop" işaretleri (`.claude/skills/unslop/references/signs.md`): her karta gradyan, her başlığa emoji, rastgele rozet, aynı bilgiyi üç kez söyleyen satır, ortalanmış uzun paragraf.

## 8. Renk

Palet **"Stadyum Pop"** (yasin 22 Kas 2026: "stadyum pop tan devam edelim, ama ai unslop olmalı gerçek bir tasarımcının elinden çıkmış bir ui ve ux tasarımı olmalı"; seçim sayfası `/mnt/project-files/birdie-smash/menu-tasarim-dilleri-2026-11-22/v4-gorsel-yonler.html`). Önceki "Gün Batımı Salonu" (27 Eki) ve Dünya Turu renkleri (22 Kas v3, reddedildi) bununla bitti. Değişken adları geriye uyum için aynı kaldı (`--sun` artık sarı, `--ink` mavi zemin).

**Yön (beş satır):**
- Ürün: çocuk ve genel kitleye, kısa maçlı, kariyerli bir badminton oyunu.
- Karakter: güneşli, sportif, zıplayan.
- Dünya: gündüz badminton salonu; kort mavisi, yeşil kort, beyaz çizgi.
- Anlar: oyuncu ile rakibin afişi (Oyna) ve büyük karakter vitrini (Dolap); renk ve ayrıntı yalnız orada.
- İmza: mürekkep konturlu, altı kalın 3B düğme; ekranda tek sarı.

### 8.1 Rol tablosu (tek anlam, tek renk)

| Rol | Değişken | Değer | Nerede | Nerede DEĞİL |
|---|---|---|---|---|
| Zemin | `--ink` | #2659BE (Oyna'da alta doğru #163C8E) | sayfa arkası | yazı |
| Yüzey (panel) | `--cam`, `--cam2`, `--cam3` | lacivert-mavi .62 / #1E4DB3 / #173F98 | kart, ikincil düğme, basılı | zemin |
| Çizgi / kontur | `--cam-kenar`, `--cta-ink` | #14244F / #1B2340 | basılabilir öğenin konturu ve alt kalınlığı | kart (kart konturusuz) |
| Birincil eylem / seçili / şimdi | `--sun` | #FFC93C, üstünde mürekkep yazı | tek birincil düğme, seçili çip ve sekme hapı | süs, ilerleme çubuğu |
| Sen ve senin ilerlemen | `--lime` | #C9EE4B | oyuncunun skoru, kendi satırı; seviye/XP çubuğu (yasin 20 Kas "A · Limon") | düğme dolgusu |
| Rakip | `--coral` | #E99BFF | rakip skoru ve adı (kart üstünde) | hata |
| Ödül / Tüy | `--gold` | #F9D272, hep tüy simgesiyle | Tüy, kupa, seviye ödülü | seçili durum |
| Bilgi | `--sky` | #A9D6FF | ipucu, stat çubuğu | eylem |
| Ana / ikincil yazı | `--tx` / `--mu` | #FFFFFF / #D6E2FF | gövde / açıklama | — |
| Sekme çubuğu | `--bar` | #FFFFFF, soluk yazı #5A6690 | alt gezinme | içerik kartı |

Kontrast (22 Kas ölçüldü): beyaz yazı zeminde 6,5:1, kartta 9,3:1, ikincil düğmede 7,6:1; `--mu` zeminde 5,0:1, kartta 7,2:1; mürekkep sarıda 10:1; vurgu renkleri kartta 4,7 (orkide) ile 7,0 (limon) arası. Orkide zeminde 2,5:1 olduğu için rakip adı zemine doğrudan yazılmaz, kart üstünde durur.

### 8.2 Kurallar

1. **60-30-10**: ekranın ~%60'ı zemin, ~%30'u panel, en fazla ~%10'u vurgu renkleri (sarı, limon, orkide, altın). Bir ekranda aynı anda en fazla iki vurgu rengi baskın olur; sarı her zaman "bas" ya da "seçili" demektir.
2. **Saf siyah yok**: çizgi ve koyu yazı mürekkep #1B2340, zemin doygun ama orta koyulukta mavi (beyaz yazı 6,5:1). Beyaz yalnız yazı, sekme çubuğu ve kort çizgisi içindir; mavi zeminde koyu doygun renk (ör. saf kırmızı) kullanılmaz.
3. **Basılabilir = konturlu**: kart düz, konturusuz lacivert paneldir; düğme, çip ve sekme mürekkep konturlu ve altı kalındır (3B). Kontur kalınlığı seçili ve seçili değilken aynıdır, yalnız renk değişir (yerleşim kaymaz). Gölge (box-shadow) yok.
4. **Kontrast (ölçüldü, 22 Kas, sayılar 8.1'de)**. Alt sınır gövde 4,5:1, büyük kalın 3:1, simge ve çerçeve 3:1. `--mu`'dan daha soluk bir yazı rengi eklenmez; pasif öğe opaklıkla değil "kilit + yazı" ile ayrılır, opaklık gerekiyorsa en az .6 ve yazı yine 4,5:1.
5. **Renk körlüğü**: sen/rakip ayrımı limon–orkide (parlaklıkları da farklı) + konum (sen solda/altta) + ad; seçili sarı dolgu + mürekkep kontur + ✓; kazanç/kayıp ok ve işaretle (▲ ▼, +/−). Kırmızı–yeşil ikilisi tek başına anlam taşımaz.
6. **Sarı kıtlığı**: sarı yalnız birincil eylem, seçili durum ve aktif sekme içindir (Supercell'in "en önemli düğme sarı" kalıbı). İlerleme çubukları sarı olmaz: seviye ve gelişim limon, Tüy altın (hep tüy simgesiyle, sarıyla karışmasın). Başlıkta, süste, rozette sarı yoksa göz eylemi saniyede bulur.
7. **Kozmetik renkler ayrıdır**: oyuncunun seçtiği forma/raket renkleri arayüz rolü taşımaz; menüde yalnız kendi örnek yuvarlağında görünür (`docs/kozmetik_tasarim_dili.md`).

## 9. Ekran denetim listesi (her menü değişikliğinde PR'a)

Kısa "HIG kontrolü" bölümüne şu sorular yanıtlanır:
1. Bu ekranın tek işi ne? Tek birincil eylem var mı?
2. İlk ekranda kaç dokunulabilir öğe ve kaç kart var (§7.3 bütçesi)?
3. Tekrar eden ya da her açılışta okunan uzun metin kaldı mı (§7.4)?
4. Cam katman sırası doğru mu: gezinme üstte, içerik sakin; cam içinde cam yok (§6.1)?
5. Renkler rol tablosuna uyuyor mu; sarı yalnız eylem/seçili mi; anlam yalnız renkle mi verilmiş (§8)?
6. Dokunma ≥44, yazı ×2'de taşma/çakışma yok, hareketi azalt, erişim etiketi (§1, §3, §5)?
7. Dokunuşta takılma yok: kayıt `KaydetSonra`, sayfa yerinde güncelleniyor mu (§7.8)?
8. `unslop` işaret listesi ve §10.2 temiz mi; yeni metin `no-ai-slop` süzgecinden geçti mi?

## 10. Gerçek tasarım: AI slop yok

Kaynak: yasin 15 Kasım 2026 22:20 ("AI slop görseller istemiyoruz; gerçek bir tasarım, arkasında düşünülmüş incelik ve zarafette bir oyun UI ve menü tasarımı istiyoruz"). Denetim iki skill'le yapılır: görsel/arayüz için `unslop` (`references/signs.md`), oyuncuya görünen metin (EN menü, mağaza, TestFlight notu) için `no-ai-slop` (petergyang/no-ai-slop, MIT; Türkçe metinde aynı yargı).

Zarafet ölçütü: her öğenin bir nedeni vardır ve neden kod yorumunda yazar. "Güzel dursun" tek başına neden değildir.

1. **Oyunun kendi dili**: arayüz badminton ve "Gün Batımı Salonu"ndan beslenir: tüy topun yayı, file çizgisi, salon ışığı, skor tabelası. Jenerik "mobil oyun" kalıbı (parlak gradyanlı düğme, her yerde yıldız patlaması, metalik çerçeve, sandık açma ışıması) kullanılmaz.
2. **AI slop görsel işaretleri** (görürsen kaldır): amaçsız mor-mavi gradyan lekeleri; her karta aynı parıltı/gölge; emoji ya da karışık stilde simge (simge seti tek: aynı çizgi kalınlığı, aynı köşe yuvarlaklığı, `Ikonlar`); ortalanmış uzun paragraf; her başlığın yanında rozet; anlamsız 3B "glossy" yüzey; stok illüstrasyon havası; aynı bilgiyi üç kez söyleyen kart; rastgele eğik yerleşim.
3. **İncelik ayrıntıları** (bunlar kaliteyi taşır): optik hizalama (simge yazının x-yüksekliğine ortalı, ▶ gibi üçgen simgeler sağa 1 pt kaydırılır); sayılar sabit genişlikte (değişince titremez); başlık ile gövde arasında net ağırlık farkı (Baloo başlık, Nunito gövde, üç boy yeterli); ışık hep yukarıdan (cam üst kenarı açık, alt kenar koyu, tutarlı); basılı durum her düğmede aynı (%98 + bir ton açık, 80 ms); boş durumlar bile tasarlanmış (tek cümle + tek eylem, "veri yok" değil).
4. **Az ama doğru**: bir ekranda en fazla bir "an" (ödül, yükseltme, yeni eşya) hareketli olur; geri kalan sakin durur. Sakinlik zarafetin yarısıdır.
5. **Metin insan sesi**: kısa, somut, oyuncuya hitap eden ("Raketini seç", "3 maç kaldı"). Pazarlama dili, abartı ("muhteşem", "inanılmaz"), "sadece X değil, Y" kalıbı, üçlü sıfat dizisi yok (`no-ai-slop`).
6. **Kanıt gerçek kareyle**: tasarım kararı mock-up'la değil ekran turu karesiyle (`ekran-sonuc`) doğrulanır; önce/sonra artifact'i gerçek oyun karesi gösterir.


## 11. Ekran kalıpları (yasin seçimi, 19 Kasım 2026)

Kaynak: yasin 19 Kasım 2026 10:17 ("Üçünde önerilerini uygula kurallara uygun şekilde"), seçenek sayfası
`https://claude.ai/artifact/AhLNpXh8XkiHWsZk1kkz3H` (üç ekranda A). Araştırma: Tennis Clash kupa yolu, Clash Royale sabit alt
gezinme ve açılır panel, Brawl Stars iri simge ve bölmeli yerleşim, Apple HIG sheet.

1. **Dolap = sahne + çekmece.** Figür alanın ~%60'ı; liste altta, üç duraklı çekmecede (İncele: çekmece kapalı · yarım ·
   tam). Kategoriye dokunmak kamerayı o bölgeye götürür, eşyaya dokunmak figürde dener, tek sarı düğme Giy/Al. Raket
   şeridin son kategorisi. iPad'de çekmece sağ panel, figür solda tam boy. Durak geçişi animasyonsuz: vitrin RT'si kutuyla
   boyutlanır, her karede boy değiştirmek takılma demektir (§7.8).
   **Yuvalar** (23 Kasım 2026, yasin seçimi "Dolap A", `https://claude.ai/artifact/Q3WbQ4HGx1Pv6VhfwXpBpV`): kategoriler
   yazılı şeritte değil, figürün iki yanında yuva (64 × 48 pt; liste ikiye bölünür, ilk yarı solda). Dokunulan yuva
   çekmecenin içeriğini seçer; seçili yuva sarı çerçeve + köşede ✓ rozeti. Hareketler giyilmez, ayrı yuva sağ üstte
   (İncele sol üstte). Yuva sığmadığında yazılı şerit döner: tam durak, büyük yazı, kısa ekranda 8 yuva; ikisi aynı anda yok.
2. **Kariyer = Dünya Turu + dikey rota** (22 Kasım 2026, yasin "önerin gibi ilerle"; seçenekler
   `https://claude.ai/artifact/19MR43zwtCSMgJxmpkLeNM`, A + B'nin durak ödülleri + C'nin turnuva aşamaları; kalıp Tennis
   Clash / Golf Clash şehir turu). Üstte sekiz tur kartı yatay şeritte (kart 232 pt: Painter2D mekân sahnesi 112 pt, "Tur n"
   etiketi, Bitti/Buradasın rozeti, ad, maç sayısı + yıldız + ilerleme çubuğu + mühür satırı; kilitlide örtü + kilit +
   neden). Kart dokunuşu turu seçer, seçili kart sarı çerçeve. Altında hikâye satırı; grup turlarında (ilk durak "grup")
   Grup · Çeyrek · Yarı · Final kutuları. Rota: altı durak zikzak (durak adı şehir, altında rakip; geçilen ✓ + yıldız,
   şimdiki büyük disk + "Şimdi"), her durağın ucunda o duraktan alınacak Tüy (Gear.Odul: ilk galibiyet +60, eksik yıldız
   başı +20; uydurma sayı yok), sonunda mühür. Tek birincil eylem altta sabit "Sıradaki maç". Yol yukarıdan aşağı:
   okuma ve ekran okuyucu sırası basamak sırasıyla aynı.
3. **Atölye = seçici + çubuk.** Seçeneği çip şeridinde seç, önizlenen seçeneğin etkisini çubukla göster (orta çizgi = nötr,
   artı sağa limon, eksi sola soluk, ▲/▼ ve yüzde yazısı yanında). Kilitli seçenek de önizlenir; düğme kaç seviye kaldığını
   söyler. Uzun kart listesi yerine bir kart.
4. **Basma dili tek.** Her düğme, çip, ikon düğmesi ve CTA basınca .06 sn'de %95, bırakınca .26 sn ease-out-back; dokunuşta
   tick titreşimi (UiOge). Hareketi azaltta küçülme yok, renk değişimi kalır.
5. **Menü sesleri türe göre** (22 Kasım 2026, yasin "tüm butonlarda tatmin edici ses"; `Sfx.UiSes`). Her dokunuş bir ses
   çalar, tür öğenin sınıfından gelir: `pop` (varsayılan düğme, kart), `bas` (birincil düğme; "hit" titreşimi), `sec` (çip),
   `sekme` (alt gezinme), `geri` (geri, alt sayfa kapanışı), `kilit` (pasif/kilitli; hata vızıltısı değil), `kaydir`
   (giyilen parça), `odul` (satın alma, seviye, tur mührü). Sesler sentezlenir, önbellekte durur, açılışta ısıtılır; düzey
   oyun içi vuruşların altında. Yeni dokunulur öğe kendi sesini bu listeden alır, yeni bip yazılmaz.
6. **Basılabilir her şey tıknaz** (22 Kasım 2026 Stadyum Pop, §8). Düğme, çip, ikon düğmesi ve sekme: 2 pt mürekkep kontur,
   alt kenar 4 pt (birincil 5, büyük Oyna 6); basınca 1–2 pt aşağı oturur. Birincil sarı dolgu + mürekkep yazı, ikincil
   lacivert-mavi + beyaz yazı. Kart konturusuz kalır: basılabilir olanla olmayan biçimden ayrılır.
7. **Maç içi göstergeler menüyle aynı dili konuşur** (22 Kasım 2026, yasin: güç göstergesi yeni tasarımla tek bütün). Renk
   rolleri §8.1'deki gibi: oyuncuya ait gösterge (vuruş gücü yayı, top izi) limon, rakibe ait orkide, ödül altın; sarı
   yalnız seçim/birincil eylem, kortta kullanılmaz. Parlama, nabız, beyaz flaş, gradyan yok; düz renk + yarı saydamlık. Çizgi
   1,5 pt, vurgu 2 pt, uçlar yuvarlak. Hızlı dolum (ease-out), yavaş sönüm; hareketi azaltta dolum anlık. Anlam yalnız renkle
   verilmez (ör. güç = yanan dilim sayısı).
