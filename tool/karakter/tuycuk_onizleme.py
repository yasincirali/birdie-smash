# Tüycük ön izlemesi (Unity'siz, bpy + Cycles): FBX'e giden pozların aynısı (karakter.py build → Tuycuk.kur_poz: çözücü +
# tutuş), dört anahtar pozda arkadan yüksek oyun kamerası (me, ~53°), önden (op) ve raket eline yakın plan. Poz başına
# ölçüm satırı: çözücüden kalan en derin girme, eldiven–sap en küçük işaretli uzaklık (<0 = sap eldivenin içinde), raket
# sapması (°). Birim oyun cm (model × 175).
#   BS_MODEL=tuycuk python3 -I tool/karakter/tuycuk_onizleme.py -- <çıktı klasörü>
import bpy, sys, os, math
from mathutils import Vector
K = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, K)
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = args[0] if args else '/tmp/tuycuk_onizleme'; os.makedirs(OUT, exist_ok=True)
os.environ.setdefault('BS_MODEL', 'tuycuk')
SRC = os.path.join(K, 'karakter.py'); src = open(SRC, encoding='utf-8').read()
src = src.split('# ---------- hakemler')[0].replace('bpy.ops.wm.read_factory_settings(use_empty=True)', 'bpy.ops.wm.read_homefile(use_empty=True)')
sys.argv = [sys.argv[0], '--', OUT, 'onizle']
ns = {'__name__': 'karakter', '__file__': SRC}; exec(compile(src, SRC, 'exec'), ns)
CLIPS, build, sc, cam = ns['CLIPS'], ns['build'], ns['sc'], ns['cam']
# önizlemede oyunun boyadığı renkler pişer: forma mavi, karın açığı, bant/bileklik kırmızı
FORMA = (0.06, 0.42, 0.85)
def boya():
    for o in ns['objs']:
        if o.type != 'MESH': continue
        t = o.get('tag')
        c = FORMA if t == 'forma' else tuple(x + (1 - x) * .6 for x in FORMA) if t == 'karin' else (0.85, 0.12, 0.18) if t in ('bant', 'bilek') else None
        if c is None: continue
        for m in o.data.materials:
            if m and m.use_nodes and 'Principled BSDF' in m.node_tree.nodes: m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*c, 1)
def poz(ad):
    if ':' in ad: ad, k = ad.split(':'); fr = CLIPS[ad]; return fr, int(k), getattr(fr, 'hit', None)
    fr = CLIPS[ad]; h = getattr(fr, 'hit', None); i = h if h is not None else len(fr) // 2
    return fr, i, h
# temas karesi (idle: orta kare). BS_TC_POZ="ad=klip[:kare],..." başka pozlar (ör. backhand hazırlığı bh_over:2)
POZLAR = [tuple(x.split('=')) for x in os.environ['BS_TC_POZ'].split(',')] if os.environ.get('BS_TC_POZ') else \
    [('idle', 'idle'), ('smash', 'smash'), ('backhand', 'bh_over'), ('lunge', 'lunge')]
sc.render.resolution_x, sc.render.resolution_y = 480, 640; sc.cycles.samples = int(os.environ.get('BS_ORNEK', '24'))
OY = None
for ad, klip in POZLAR:
    fr, i, h = poz(klip); klip = klip.split(':')[0]
    OY = ns['OY']
    if OY is None: build(fr[i]); OY = ns['OY']
    OY.klip_bilgi = (klip, i, h, len(fr)); build(fr[i]); boya()
    for v in ('me', 'op'):
        ns['setcam'](v); cam.data.ortho_scale = 1.55; cam.location = cam.location + Vector((0, 0, .05)); ns['render'](f'{OUT}/{ad}_{v}.png')
    # raket eli yakın plan (önden-yandan)
    pb = OY.ao.pose.bones; c = OY.ao.matrix_world @ pb['hand_r'].head
    for yad, yon in (('el', (0.55, 0.8, 0.35)), ('el2', (0.9, -0.3, 0.4))):
        d = Vector(yon).normalized(); cam.location = c + d * 3; cam.data.ortho_scale = .42
        el = math.atan2(d.z, math.hypot(d.x, d.y)); cam.rotation_euler = (math.pi / 2 - el, 0, math.atan2(d.y, d.x) + math.pi / 2)
        sc.render.resolution_x = sc.render.resolution_y = 480; ns['render'](f'{OUT}/{ad}_{yad}.png')
    sc.render.resolution_x, sc.render.resolution_y = 480, 640
    t = OY.I.tut; ds = getattr(t, 'deri_son', {}) or {}
    print('ÖLÇÜM', ad, klip, 'kare', i, 'girme', {k: round(v * 175, 2) for k, v in OY.cozum.items() if v > 1e-4},
          'sap', {k: round(v * 175, 2) for k, v in ds.items()}, 'kalan°', round(getattr(t, 'kalan', 0.), 1), 'tutuş', getattr(t, 'secilen', '-'))
print('ONIZLEME', OUT)
