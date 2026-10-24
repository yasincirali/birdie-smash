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
# Oyuncak 3B oyuncu (tool/karakter/oyuncak.py): KayKit Character Pack Adventures 1.0 (CC0, Kay Lousberg), Rogue gövdesi.
KK=$HOME/AnimationLibrary/kaykit
if [ ! -f "$KK/addons/kaykit_character_pack_adventures/Characters/gltf/Rogue.glb" ]; then
  T=$(mktemp -d) && git clone -q --depth 1 https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0.git "$T/k" \
    && mkdir -p "$KK" && mv "$T/k/addons" "$KK/addons" && echo "kaykit: clone" || echo "kaykit alınamadı"
fi
# Gerçekçi oyuncu (tool/karakter/aday.py, BS_MODEL=aday): Microsoft Rocketbox Sports_Male_04 (MIT). Repo çok büyük;
# yalnız bu avatarın FBX'i ve 6 dokusu raw.githubusercontent'ten alınır.
RB=$HOME/AnimationLibrary/rocketbox/Sports_Male_04
if [ ! -f "$RB/Export/Sports_Male_04.fbx" ]; then
  U=https://raw.githubusercontent.com/microsoft/Microsoft-Rocketbox/master/Assets/Avatars/Professions/Sports_Male_04
  mkdir -p "$RB/Export" "$RB/Textures" && curl -sSfL -o "$RB/Export/Sports_Male_04.fbx" "$U/Export/Sports_Male_04.fbx" \
    && for t in body head; do for k in color normal specular; do curl -sSfL -o "$RB/Textures/m026_${t}_${k}.tga" "$U/Textures/m026_${t}_${k}.tga"; done; done \
    && echo "rocketbox: indirildi" || echo "rocketbox alınamadı"
fi
# Unity MCP (CoderGamester/mcp-unity, MIT): sunucu bulutta derlenip kaydedilir. Araçları yalnız mcp-unity paketi
# kurulu, açık bir Unity Editor'a (WebSocket, UNITY_HOST/UNITY_PORT, varsayılan localhost:8090) bağlanınca çalışır;
# bulutta Editor yok (Unity indirme/lisans sunucuları ağ politikasında kapalı), yani burada bağlantı bekler.
MU=$HOME/mcp-unity
if [ ! -f "$MU/Server~/build/index.js" ]; then
  { [ -d "$MU" ] || git clone -q --depth 1 https://github.com/CoderGamester/mcp-unity.git "$MU"; } \
    && (cd "$MU/Server~" && npm ci --no-audit --no-fund -s && npm run -s build) >/dev/null 2>&1 \
    && echo "mcp-unity: derlendi" || echo "mcp-unity derlenemedi"
fi
if [ -f "$MU/Server~/build/index.js" ] && command -v claude >/dev/null; then
  for d in /home/user /home/user/birdie-smash; do
    (cd "$d" 2>/dev/null && claude mcp add -s local mcp-unity -- node "$MU/Server~/build/index.js" >/dev/null 2>&1) || true
  done
  echo "mcp-unity: kayıtlı"
fi
# Babysitter (a5c-ai, MIT): uzun işleri adım adım, kalite kapılı yürüten orkestrasyon eklentisi; /babysitter:* komutları
# ve babysit skill'i yeni oturumlarda görünür.
if command -v claude >/dev/null; then
  command -v babysitter >/dev/null || npm install -g @a5c-ai/babysitter@6.0.0 --no-audit --no-fund -s >/dev/null 2>&1
  claude plugin marketplace add a5c-ai/babysitter-claude >/dev/null 2>&1 || true
  claude plugin install --scope user babysitter@a5c.ai >/dev/null 2>&1 || true
  claude plugin list 2>/dev/null | grep -q "babysitter@a5c.ai" && echo "babysitter: kurulu" || echo "babysitter kurulamadı"
fi
