# Birdie Smash karakteri → Unity: tek FBX (parçalı figür, nesne animasyonu) + klip aralıkları JSON.
# Neden kemiksiz: figür zaten oyuncak gibi parçalı (karakter.py, uzuv boyları IK ile sabit); her parça
# rijit bir nesne olarak her karede dünya dönüşümüyle anahtarlanır, Unity Generic/Legacy klip olarak
# oynatır. Skin/weight yok → bükülme hatası da yok. Pozlar karakter.py'den okunur (tek kaynak).
#   python3 -I tool/karakter/unity_fbx.py -- <çıktı klasörü>
# Çıktı: oyuncu.fbx, oyuncu_clips.json (Unity AssetPostprocessor bunlarla klipleri böler).
import bpy, sys, os, json, math
from mathutils import Matrix, Vector, Quaternion
OUT = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else '/tmp/bs_unity'
os.makedirs(OUT, exist_ok=True)
SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'karakter.py')
src = open(SRC, encoding='utf-8').read()
src = src.split('# ---------- sahne ----------')[0]          # render kısmı gerekmez
# 3d-animator kuralı: headless'ta factory ayarı yok; boş ev dosyası.
src = src.replace('bpy.ops.wm.read_factory_settings(use_empty=True)', 'bpy.ops.wm.read_homefile(use_empty=True)')
sys.argv = [sys.argv[0], '--', OUT, 'keys']
ns = {'__name__': 'karakter'}
exec(compile(src, SRC, 'exec'), ns)
CLIPS, build, objs = ns['CLIPS'], ns['build'], ns['objs']
LOOP = {'idle', 'run', 'fwd', 'chasse_r', 'chasse_l', 'back_r', 'back_l'}   # karakter.py'de sample(loop=True) / döngü
FR, GAP, FPS = 3, 10, 30   # poz başına 3 kare (Unity arası yumuşak doldurur), klipler arası boşluk
sc = bpy.context.scene; sc.render.fps = FPS

root = bpy.data.objects.new('oyuncu', None); sc.collection.objects.link(root)
proxies = {}   # ad → (nesne, ref yerel z boyu)

def local_zext(o):
    zs = [v.co.z for v in o.data.vertices] if o.type == 'MESH' and len(o.data.vertices) else [0, 0]
    return max(zs) - min(zs)

def proxy_for(o):
    key = o.name + '__' + str(o.get('tag') or 'x')
    if key in proxies: return key, proxies[key]
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))      # bevel gibi modifier'lar uygulanmış kopya
    for i, m in enumerate(o.data.materials):
        if i < len(me.materials): me.materials[i] = m
        else: me.materials.append(m)
    p = bpy.data.objects.new(key, me); sc.collection.objects.link(p); p.parent = root
    p.rotation_mode = 'QUATERNION'
    proxies[key] = (p, max(1e-6, local_zext(o)), None)
    # Sonradan doğan parça (servis topu): önceki karelerde görünmez.
    for fp, _ in timeline:
        if fp >= cur_frame[0]: break
        p.scale = (1e-4, 1e-4, 1e-4); p.keyframe_insert('scale', frame=fp)
    return key, proxies[key]

timeline, frame, meta = [], 1, {'fps': FPS, 'scale': 1.75, 'clips': []}
for name, frames in CLIPS.items():
    seq = list(frames) + ([frames[0]] if name in LOOP else [])
    first = frame
    for p in seq:
        timeline.append((frame, p)); frame += FR
    last = frame - FR
    meta['clips'].append({'name': name, 'first': first, 'last': last, 'loop': name in LOOP, 'poses': len(frames)})
    frame = last + GAP

prevq = {}
cur_frame = [1]
for f, pose in timeline:
    cur_frame[0] = f
    build(pose)
    seen = set()
    for o in list(objs):
        if o.type != 'MESH': continue
        key, (p, zref, _) = proxy_for(o)
        seen.add(key)
        M = o.matrix_world.copy()
        ratio = local_zext(o) / zref if zref > 1e-6 else 1.0
        M = M @ Matrix.Diagonal((1, 1, ratio, 1))
        loc, q, s = M.decompose()
        if key in prevq and prevq[key].dot(q) < 0: q = -q          # kuaterniyon sürekliliği (Unity'de takla olmasın)
        prevq[key] = q
        p.location, p.rotation_quaternion, p.scale = loc, q, s
        for path in ('location', 'rotation_quaternion', 'scale'): p.keyframe_insert(path, frame=f)
    for key, (p, _, _) in proxies.items():                        # bu pozda olmayan parça (servis topu): görünmez
        if key not in seen:
            p.scale = (1e-4, 1e-4, 1e-4); p.keyframe_insert('scale', frame=f)
for o in list(objs): bpy.data.objects.remove(o)
objs.clear()
sc.frame_start, sc.frame_end = 1, frame
sc.frame_set(1)
fbx = os.path.join(OUT, 'oyuncu.fbx')
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'EMPTY', 'MESH'},
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=False,
    use_mesh_modifiers=True, mesh_smooth_type='FACE', bake_anim=True, bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=False, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
    bake_anim_force_startend_keying=True, add_leaf_bones=False, path_mode='STRIP')
meta['parts'] = sorted(proxies)
meta['hit'] = {'smash': .56, 'clear': .58, 'drive': .55, 'kill': .55, 'low': .55, 'lunge': .6, 'serve': .65, 'block': .6,
               'bh_over': .56, 'bh_drive': .55, 'bh_kill': .55, 'bh_low': .55, 'bh_lunge': .6, 'jump': .52}
json.dump(meta, open(os.path.join(OUT, 'oyuncu_clips.json'), 'w'), indent=1, ensure_ascii=False)
print('FBX', fbx, os.path.getsize(fbx), 'parça', len(proxies), 'kare', frame, 'klip', len(meta['clips']))
