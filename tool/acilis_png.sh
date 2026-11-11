#!/usr/bin/env bash
# Açılış perdesinin topu (2026-11-12 inceleme turu, fikir 6): StreamingAssets kopyası iOS'ta motor açılmadan BirdieAcilis.mm'de
# ham PNG olarak çözülüyor (Resources kopyası ASTC'ye sıkışır, ona dokunulmaz). Paletli PNG ~%65 küçük; ölçüm: premultiplied
# ortalama fark 0,74/255 (hedef ≤1). Kaynak değişince yeniden koş. Gerek: pngquant (apt-get install pngquant).
set -euo pipefail
cd "$(dirname "$0")/.."
pngquant --quality=85-98 --speed 1 --strip --force \
  --output unity/BirdieSmash/Assets/StreamingAssets/tuy_don.png unity/BirdieSmash/Assets/Resources/ui/tuy_don.png
ls -l unity/BirdieSmash/Assets/StreamingAssets/tuy_don.png
