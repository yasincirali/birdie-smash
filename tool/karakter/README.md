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

## İki model (2026-10-24, yasin: "iki modeli de seçenek sun, defaultta gerçekçi olmayan olsun")

- **Oyuncak 3B** (varsayılan): `oyuncak.py`, KayKit Adventurers Rogue gövdesi (CC0). Paketin hareketleri değil, aynı
  badminton poz sözlükleri bu iskelete eklem hedefi olarak uygulanır. 360° için temizlik: bel tek parça şort beli,
  kolluk halkaları silinip eldiven ağzı uzatılır, bileklik ayrı halka, saç kapağı ensede kesik saçı örter.
- **Gerçekçi**: bugünkü sportif figür (`BS_STIL=sportif`).

```bash
BS_MODEL=oyuncak BS_OLCEK=2 python3 -I karakter.py -- /tmp/oy full && python3 -I sheet.py /tmp/oy
BS_MODEL=oyuncak python3 -I karakter.py -- /tmp/oy tur && python3 -I tur_js.py /tmp/oy
cd ../.. && python3 -I tool/karakter/gom.py oyuncak /tmp/oy/spr.js /tmp/oy/tur.js
```

Oyunda her model ayrı `<script type="application/json" id="bs_model_<ad>">` bloğunda; oyun yalnız seçili olanı
ayrıştırır (`tuytop_model`, Ayarlar › Oyuncu görünümü). Hızlı önizleme: `stil` modu, 360° için `BS_TUR_N=8 BS_ONIZLEME=1`.
`aday.py`: hazır modelleri (Rocketbox, VRM) aynı pozlarla deneme (karşılaştırma sayfası için).
