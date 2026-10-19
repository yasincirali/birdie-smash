# Karakter sprite üretimi

Oyuncu figürü Blender'da (bpy, `pip install bpy`) parçalı gövde + iki-kemik IK ile kurulur; eklem
konumları hedef olarak verilir, uzuv boyları hiç bozulmaz. 10 klip × 2 bakış render edilir,
`sheet.py` sprite sheet + maske (R forma, G raket çerçevesi, B kafa bandı; ayrı bileklik) +
`spr.js` (base64) üretir; `spr.js` oyun dosyasındaki `SPR_META`/`SPR_SRC` bloğunun yerine geçer.

```bash
python3 -I karakter.py -- /tmp/bs full      # ~3 dk CPU (keys: yalnız anahtar pozlar, önizleme)
python3 -I sheet.py /tmp/bs                # sheet_*.png, mask_*.png, wrist_*.png, spr.js
```
