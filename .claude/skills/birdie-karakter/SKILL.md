---
name: birdie-karakter
description: Birdie Smash oyuncu karakteri ve animasyon hattı (Blender bpy → sprite sheet → canvas). Yeni klip, poz düzeltme, raket/el kuralı, ayak işi, maske/tint, oyuna gömme ve doğrulama gerektiğinde oku. 3D rig, mocap ve retarget genel bilgisi için `3d-animator` skill'i; Blender'ı canlı sürmek için `blender` MCP (yalnız PC).
---

# Birdie Smash karakter hattı

Oyun tek HTML dosyası (`assets/game/index.html`, canvas). Karakter üç boyutlu
**değil**: Blender'da kurulan figür iki bakıştan (arkadan `me`, önden `op`)
sprite sheet olarak render edilir, oyuna base64 gömülür ve piksel boyama
(tint) ile forma/raket/bant renkleri oyun içinde değişir. Üç kalıcı karar:

1. **Raket HEP sağ elde** (yasin 2026-10-19). Backhand = kendi klibi
   (`bh_over`, `bh_drive`, `bh_kill`, `bh_low`, `bh_lunge`), aynalama YOK.
   Klip seçimi oyunda `clipHand(c, hand)` + `BH` haritası; el tarafını
   `hitSide()` pozisyondan seçer (forehand noktası yetişilemiyorsa backhand).
2. **Ayak işi badminton:** `split` (rakip vurunca), `chasse_r/l` (yan),
   `fwd` (öne adım), `back_r/l` (arkaya çapraz), `jump` (makas sıçrama).
   Koşu döngüsü yalnız uzun mesafede. Yeni hareket eklerken önce gerçek
   badminton ayak işine bak; "yürüme döngüsü" ekleme.
3. **Süper gerçekçi değil, düşünülmüş olduğu belli:** 4.3 kafa oranlı sevimli
   figür; ağırlık aktarımı, hazırlık (prep) → temas (hit) → takip (follow)
   üçlüsü her vuruşta var. `CLIP_HIT` temas anı kesrini oyun kullanır.

## Dosyalar

| Dosya | İş |
|---|---|
| `tool/karakter/karakter.py` | bpy; parçalı gövde + iki-kemik IK (`ik(A,B,l1,l2,hint)`), poz sözlüğü (`root, lean, twist, wrR, elR, rk, rkFace, wrL, elL, anR, anL, knR, knL, ftR, ftL, head, look, shuttle`), `sample(keys,n,loop)` kosinüs ease, `D(base,**k)` türetme. `CLIPS` 22 klip, `KEYS` önizleme pozları, `VIEWS` iki kamera. |
| `tool/karakter/sheet.py` | Kareleri sheet'e dizer, paleti 256'ya indirir, maske (R forma, G raket çerçevesi, B bant; ayrı bileklik) + `spr.js` (`SPR_META`, `SPR_SRC` base64) üretir. |
| `tool/karakter/meta.json` | Son üretimin hücre boyu, klip satırları, pxPerUnit, anchor. Oyun bunu `SPR_META` olarak taşır. |
| `assets/game/index.html` | `sprLoad()`, `sprTint(v)`, `player()` (klip seçimi + `drawImage`), `playerVec` (sprite yüklenmezse vektör yedek). |

## İş akışı

```bash
pip install bpy==5.2.2                # bulutta çalışır; GUI yok
python3 -I tool/karakter/karakter.py -- /tmp/bs keys   # yalnız anahtar pozlar (hızlı önizleme)
python3 -I tool/karakter/karakter.py -- /tmp/bs full   # 22 klip × 2 bakış, ~3 dk CPU
python3 -I tool/karakter/sheet.py /tmp/bs              # sheet_*.png, mask_*.png, wrist_*.png, spr.js
```

1. Yeni klip: `karakter.py`'de poz anahtarları yaz (prep/hit/follow), `CLIPS`'e
   ekle, `KEYS`'e önizleme pozu koy, `keys` modunda render al ve **bak**
   (dirsek kilitleme, raketin sol ele geçmesi, ayak yerden kopması).
2. `full` → `sheet.py` → üretilen `spr.js`'i `index.html`'deki
   `SPR_META=…; SPR_SRC={…}` bloğunun yerine koy; `CLIP_HIT`'e temas kesrini,
   gerekiyorsa `BH` haritasına backhand karşılığını yaz.
3. Oyunda klip seçimi `player()` içinde: vuruş (`o.sw>0`) → hazırlık
   (`prepClip`) → split → hız/yön ile ayak işi → idle. Yeni klip buraya
   koşulla girer, başka yere değil.
4. Doğrula: `node tool/oyun_sozdizimi.js` (sözdizimi + boyut), `flutter test`
   (Haptic köprüsü, debug kancası yok), headless Chromium ile bot maçı ve
   yakın plan ekran görüntüsü (tint doğru mu, forehand/backhand dağılımı var mı).
   Debug kancası `window.__tt` yalnız geliştirmede; yayınlanan dosyada
   `grep -c "__tt\|M\.log"` 0 olmalı.

## Boyut ve kalite sınırları

- HTML ≈ 1 MB hedef (22 klip, 192×256 hücre, 256 renk). Hücreyi büyütme;
  klip eklerken kare sayısını kıs (6–10 kare yeter, ease oyunda).
- Maskeyi RGBA'ya sıkıştırma: premultiplied alpha veriyi bozar (yaşandı).
- Kamera: `VIEWS` euler açısı elevation/azimut'tan; `to_track_quat` `op`
  bakışında ters dönüyor (yaşandı).
- Sheet satırı klip başına **bir** kez artar (bakış döngüsünün dışında);
  aksi hâlde 5. klipten sonrası boş kalır (yaşandı).

## Dış kaynaklar

- Hazır hareket: Mixamo (insansı, yasin'in Adobe hesabı), Mesh2Motion
  (insan dışı). Retarget ve ölçüm için `3d-animator` skill'inin
  `references/clips-retarget.md` ve `scripts/bvh_probe.py`.
- Blender'ı canlı sürmek: `blender` MCP (`uvx mcp-for-blender`), yalnız
  yasin'in PC'sinde; bulutta Blender GUI yok, bpy headless kullanılır.
