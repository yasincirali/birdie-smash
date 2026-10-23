#!/usr/bin/env bash
# Bulut oturumu (Claude Code web / Projects) kurulumu. Project settings ›
# Cloud environment › Setup script: `bash tool/bulut_kurulum.sh || true`.
# Flutter bulut imajında /opt/flutter'da hazır; burada yalnız karakter hattının
# ihtiyacı (bpy = Blender Python, GUI'siz) kurulur. `blender` MCP (.mcp.json)
# bulutta bağlanamaz, beklenen durum: Blender GUI yalnız yasin'in PC'sinde.
set -u
if python3 -c "import bpy" 2>/dev/null; then
  echo "bpy hazır"
else
  python3 -m pip install -q "bpy==5.2.2" && echo "bpy kuruldu" || echo "bpy kurulamadı (ağ?)"
fi
command -v node >/dev/null && echo "node $(node --version)" || echo "node yok"
[ -x /opt/flutter/bin/flutter ] && echo "flutter: /opt/flutter" || echo "flutter yok (PATH'e bak)"
python3 -c "import scipy" 2>/dev/null || python3 -m pip install -q scipy >/dev/null 2>&1 || true
# 3d-animator skill'inin hareket kütüphanesi. fetch_library.py'nin zip URL'si
# (github archive) bulut proxy'sinde 403; git clone çalışıyor. Paylaşılan
# klasördeki kopya varsa oradan, yoksa clone. Yalnız static/{animations,rigs,models}.
LIB=$HOME/AnimationLibrary/mesh2motion/mesh2motion-app-main/static
if [ ! -d "$LIB/animations" ]; then
  SHARED=/mnt/project-files/birdie-smash/AnimationLibrary/mesh2motion/mesh2motion-app-main/static
  mkdir -p "$LIB"
  if [ -d "$SHARED/animations" ]; then cp -r "$SHARED"/{animations,rigs,models} "$LIB"/ && echo "mesh2motion: paylaşılan klasörden"
  else
    T=$(mktemp -d) && git clone -q --depth 1 https://github.com/Mesh2Motion/mesh2motion-app.git "$T/m" \
      && cp -r "$T/m"/static/{animations,rigs,models} "$LIB"/ && echo "mesh2motion: clone" || echo "mesh2motion alınamadı"
    rm -rf "$T"
  fi
fi
# Gerçekçi oyuncu (tool/karakter/insan.py): MakeHuman veri dosyaları (CC0), yalnız gövde ağı, makro hedefler, iskelet.
MH=$HOME/AnimationLibrary/makehuman/mh
if [ ! -f "$MH/makehuman/data/3dobjs/base.obj" ]; then
  git clone -q --depth 1 --filter=blob:none --sparse https://github.com/makehumancommunity/makehuman "$MH" \
    && git -C "$MH" sparse-checkout set makehuman/data/3dobjs makehuman/data/targets/macrodetails makehuman/data/rigs \
    && echo "makehuman: clone" || echo "makehuman alınamadı"
fi
