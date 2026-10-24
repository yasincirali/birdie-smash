# Karakter sprite üretimi

Oyuncu figürü Blender'da (bpy, `pip install bpy`) parçalı gövde + iki-kemik IK ile kurulur; eklem
konumları hedef olarak verilir, uzuv boyları hiç bozulmaz. 10 klip × 2 bakış render edilir,
`sheet.py` sprite sheet + maske (R forma, G raket çerçevesi, B kafa bandı; ayrı bileklik) +
`spr.js` (base64) üretir; `spr.js` oyun dosyasındaki `SPR_META`/`SPR_SRC` bloğunun yerine geçer.

```bash
python3 -I karakter.py -- /tmp/bs full      # ~3 dk CPU (keys: yalnız anahtar pozlar, önizleme)
python3 -I sheet.py /tmp/bs                # klip başına renk/maske/bileklik PNG → spr.js
```

Arcade görünüm (2026-10-23, yasin "Sportif arcade"): `BS_STIL=sportif BS_OLCEK=2 python3 -I karakter.py -- /tmp/gA full`
(büyük kafa/el/ayak, cel gölge, dış çizgi, 2× hücre). Yüz çizgileri (kaş, kirpik, ağız) gölgelendiricide `yuzuv`
koordinatından çizilir (`insan.py` `_yuz_dugum`). 360° ve hakemler aynı `BS_STIL` ile `tur` / `hakem` modlarında.

## Vuruş biyomekaniği turu (2026-10-24, `arastirma/animasyon_denetimi_2026-10-24.md` #6–#9, #12–#15)

- **Kare ızgarası:** `sample()` anahtar zamanlarını kare ızgarasına oturtur; her anahtar pozu bir karede birebir çizilir.
  Klip `hit`/`hold`/`drop` kare numaralarını taşır ve `meta.json` klip kaydına yazılır (oyunun `CLIP_HIT`,
  `PREP_KARE` değerleri buradan: `CLIP_HIT = (hit+0,05)/n`, `PREP_KARE = hold`).
- **Temasa hızlanarak:** temasa giren parça u², çıkan parça 1-(1-u)²; diğerleri kosinüs. `drop` klibi `yavas=True`.
- **Raket düşüşü:** `S_DROP` (kupa `S_PREP` → düşüş → temas). smash/clear/jump/drop 14 kare, aynı ızgara
  (0 hazır, 2 kupa = bekleme, 5 düşüş, 9 temas); ilk 6 kare dördünde aynı (aldatma).
- **Kalça önden:** pozda `pelvis` (twist işaretiyle); yoksa eski kural twist·0,35. `lead=1` leğeni bekleme karesinden
  sonra bir kare önden okur.
- **Ön kol burulması:** `burgu=1` olan pozda `rkFace` avuç yönüdür; `insan.on_kol_burgu` üst kol/ön kol/el arasında
  paylaştırır, nötr = dirsek bükme düzlemine dik avuç, ±100°. `burgu` yoksa eski görünüm birebir (360° verisi değişmez).
- **Dirsek:** `bukuk(poz, açı)` bilek hedefini kol boyunun içine çeker (kill 120°, net/alçak/hamle 150°).
- `rk` ve `rkFace` küre üstünde karışır (slerp); ters yönlü anahtarların ortası artık sıfırdan geçmez.
- Yeni klipler `drop` (clear gövdesi, yavaşlayan temas, kısa takip) ve `net` (low gövdesi, takipsiz) listenin sonunda.

## İki model (2026-10-24, yasin: "iki modeli de seçenek sun, defaultta gerçekçi olmayan olsun")

- **Oyuncak 3B** (varsayılan): `oyuncak.py`, KayKit Adventurers Rogue gövdesi (CC0). Paketin hareketleri değil, aynı
  badminton poz sözlükleri bu iskelete eklem hedefi olarak uygulanır. 360° için temizlik: bel tek parça şort beli,
  kolluk halkaları silinip eldiven ağzı uzatılır, bileklik ayrı halka, saç kapağı ensede kesik saçı örter.
- **Gerçekçi**: Microsoft Rocketbox Sports_Male_04 (MIT, `aday.py`; FBX `tool/bulut_kurulum.sh` ile `~/AnimationLibrary/rocketbox`'a
  iner). Oyundaki blok adı `rocketbox`: `BS_MODEL=aday BS_OLCEK=2 python3 -I karakter.py -- /tmp/rb full && python3 -I sheet.py /tmp/rb`.

```bash
BS_MODEL=oyuncak BS_OLCEK=2 python3 -I karakter.py -- /tmp/oy full && python3 -I sheet.py /tmp/oy
BS_MODEL=oyuncak python3 -I karakter.py -- /tmp/oy tur && python3 -I tur_js.py /tmp/oy
cd ../.. && python3 -I tool/karakter/gom.py oyuncak /tmp/oy/spr.js /tmp/oy/tur.js
```

Oyunda her model ayrı `<script type="application/json" id="bs_model_<ad>">` bloğunda; oyun yalnız seçili olanı
ayrıştırır (`tuytop_model`, Ayarlar › Oyuncu görünümü). Hızlı önizleme: `stil` modu, 360° için `BS_TUR_N=8 BS_ONIZLEME=1`.
`aday.py`: hazır modelleri (Rocketbox, VRM) aynı pozlarla deneme (karşılaştırma sayfası için).
