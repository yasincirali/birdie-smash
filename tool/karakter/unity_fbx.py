# Birdie Smash karakteri → Unity: tek FBX + klip aralıkları JSON. Pozlar karakter.py'den okunur (tek kaynak).
# 2026-10-24: oyun iki modelle çalışıyor (Ayarlar › Oyuncu görünümü): Oyuncak 3B (KayKit Rogue, CC0; BS_MODEL=oyuncak)
# ve Gerçekçi (Microsoft Rocketbox Sports_Male_04, MIT; BS_MODEL=aday). İkisi de iskeletli ve deri ağırlıklı ağ; eski
# parçalı figürün "her parçayı dünya dönüşümüyle anahtarla" yolu derili ağı bozardı. Şimdi:
#  - iskelet: her pozda karakter.py/insan.py'nin IK'sı kemiklere uygulanır, her kemiğin matrix_basis'i anahtarlanır
#    (Unity Legacy klibi derili ağı kendisi büker);
#  - pozla yeniden kurulan rijit parçalar (raket, servis topu): eskisi gibi nesne olarak dünya dönüşümüyle anahtarlanır;
#  - parça adının "__" sonrası etiketi (forma, bant, bilek, cerceve…) Unity'de takım rengini seçer; diğer parçalar
#    FBX'in kendi malzemesini/dokusunu korur (Rocketbox dokusu gömülü, 1024 px'e indirilir).
#  - "bas__isaret" ve "goz__isaret" görünmez işaretler: Unity PlayerView "ileri"yi bunlardan çıkarır.
#   BS_MODEL=oyuncak python3 -I tool/karakter/unity_fbx.py -- <çıktı klasörü> oyuncak
#   BS_MODEL=aday    python3 -I tool/karakter/unity_fbx.py -- <çıktı klasörü> rocketbox
# Çıktı: <ad>.fbx, <ad>_clips.json (Unity AssetPostprocessor bunlarla klipleri böler).
import bpy, sys, os, json, math
from mathutils import Matrix, Vector, Quaternion
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = args[0] if args else '/tmp/bs_unity'
AD = args[1] if len(args) > 1 else 'oyuncu'
os.makedirs(OUT, exist_ok=True)
SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'karakter.py')
src = open(SRC, encoding='utf-8').read()
src = src.split('# ---------- sahne ----------')[0]          # render kısmı gerekmez
src = src.replace('bpy.ops.wm.read_factory_settings(use_empty=True)', 'bpy.ops.wm.read_homefile(use_empty=True)')
sys.argv = [sys.argv[0], '--', OUT, 'keys']
ns = {'__name__': 'karakter', '__file__': SRC}
exec(compile(src, SRC, 'exec'), ns)
CLIPS, build, objs = ns['CLIPS'], ns['build'], ns['objs']
LOOP = {'idle', 'run', 'fwd', 'chasse_r', 'chasse_l', 'back_r', 'back_l', 'yorgun'}
FR, GAP, FPS = 3, 10, 30   # poz başına 3 kare (Unity arası doldurur), klipler arası boşluk
sc = bpy.context.scene; sc.render.fps = FPS

build(CLIPS['idle'][0])
OY = ns['OY']
if OY is None or not hasattr(OY, 'ao'): raise SystemExit('iskeletli model kurulmadı (BS_MODEL=oyuncak|aday)')
ao = OY.ao; kalici = list(OY.kalici)
root = bpy.data.objects.new(AD, None); sc.collection.objects.link(root)
ao.parent = root
for o in kalici:
    mw = o.matrix_world.copy(); o.parent = ao; o.matrix_world = mw
    tag = str(o.get('tag') or 'ten'); o.name = o.name.replace('.', '_') + '__' + tag
kset = set(o.name for o in kalici)   # adlar değiştikten sonra: deri ağları rijit parça sanılmasın
proxies = {}

def proxy_for(o):
    key = o.name.split('.')[0] + '__' + str(o.get('tag') or 'x')
    if key in proxies: return key, proxies[key]
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    for i, m in enumerate(o.data.materials):
        if i < len(me.materials): me.materials[i] = m
        else: me.materials.append(m)
    p = bpy.data.objects.new(key, me); sc.collection.objects.link(p); p.parent = root
    p.rotation_mode = 'QUATERNION'; proxies[key] = p
    for fp, _ in timeline:              # sonradan doğan parça (servis topu): önceki karelerde görünmez
        if fp >= cur_frame[0]: break
        p.scale = (1e-4, 1e-4, 1e-4); p.keyframe_insert('scale', frame=fp)
    return key, p

timeline, frame, meta = [], 1, {'fps': FPS, 'scale': 1.75, 'clips': [], 'model': AD}
for name, frames in CLIPS.items():
    seq = list(frames) + ([frames[0]] if name in LOOP else [])
    first = frame
    for p in seq: timeline.append((frame, p)); frame += FR
    last = frame - FR
    meta['clips'].append({'name': name, 'first': first, 'last': last, 'loop': name in LOOP, 'poses': len(frames)})
    frame = last + GAP

bones = {}; prevq = {}; pprev = {}; cur_frame = [1]
pb = ao.pose.bones
for f, pose in timeline:
    cur_frame[0] = f
    build(pose)
    for b in pb:
        loc, q, s = b.matrix_basis.decompose()
        if b.name in prevq and prevq[b.name].dot(q) < 0: q = -q       # kuaterniyon sürekliliği (Unity'de takla olmasın)
        prevq[b.name] = q; bones.setdefault(b.name, []).append((f, loc.copy(), q.copy(), s.copy()))
    seen = set()
    for o in list(objs):
        if o.type != 'MESH' or o.name in kset: continue
        key, p = proxy_for(o); seen.add(key)
        loc, q, s = o.matrix_world.decompose()
        if key in pprev and pprev[key].dot(q) < 0: q = -q
        pprev[key] = q
        p.location, p.rotation_quaternion, p.scale = loc, q, s
        for path in ('location', 'rotation_quaternion', 'scale'): p.keyframe_insert(path, frame=f)
    for key, p in proxies.items():
        if key not in seen: p.scale = (1e-4, 1e-4, 1e-4); p.keyframe_insert('scale', frame=f)
for o in list(objs):
    if o.name not in kset: bpy.data.objects.remove(o)
OY.gecici = []
for b in pb:
    b.rotation_mode = 'QUATERNION'
    for f, loc, q, s in bones[b.name]:
        b.location, b.rotation_quaternion, b.scale = loc, q, s
        for path in ('location', 'rotation_quaternion', 'scale'): b.keyframe_insert(path, frame=f)

# yön işaretleri: boşta duruşta baş merkezi ve baş önü (figür +y'ye bakar)
build(CLIPS['idle'][0])
for o in list(objs):
    if o.name not in kset: bpy.data.objects.remove(o)
OY.gecici = []
hb = next((pb[n] for n in ('head', 'Bip01 Head', 'Head') if n in pb), None)
hc = ao.matrix_world @ ((hb.head + hb.tail) / 2) if hb else Vector((0, 0, 1))
for nm, at in (('bas__isaret', hc), ('goz__isaret', hc + Vector((0, .2, 0)))):
    me = bpy.data.meshes.new(nm); me.from_pydata([(0, 0, 0), (.001, 0, 0), (0, .001, 0)], [], [(0, 1, 2)])
    mk = bpy.data.objects.new(nm, me); sc.collection.objects.link(mk); mk.parent = root; mk.location = at

for im in bpy.data.images:                 # gömülecek dokular: en çok 1024 px (Rocketbox 2048)
    try:
        if im.size[0] > 1024: im.scale(1024, max(1, int(im.size[1] * 1024 / im.size[0])))
        if not im.packed_file and im.size[0] > 0: im.pack()
    except Exception as e: print('doku', im.name, e)

sc.frame_start, sc.frame_end = 1, frame
sc.frame_set(1)
fbx = os.path.join(OUT, AD + '.fbx')
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'EMPTY', 'MESH', 'ARMATURE'},
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=False,
    use_mesh_modifiers=True, mesh_smooth_type='FACE', use_armature_deform_only=False, add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0, bake_anim_force_startend_keying=True, path_mode='COPY', embed_textures=True)
meta['parts'] = sorted([o.name for o in kalici] + list(proxies))
meta['hit'] = ns.get('CLIP_HIT_JS') or {}
json.dump(meta, open(os.path.join(OUT, AD + '_clips.json'), 'w'), indent=1, ensure_ascii=False)
print('FBX', fbx, os.path.getsize(fbx), 'kemik', len(pb), 'parça', len(meta['parts']), 'kare', frame, 'klip', len(meta['clips']))
