# Var olan karakter FBX'inin SONUNA yeni klip ekler; eski kareler ve deri ağları olduğu gibi kalır (2026-11-20).
# Neden: unity_fbx.py ile baştan üretim depodaki FBX'i birebir vermiyor. Ölçüm (tuycuk, Blender 5.3; depodaki dosya bulutta
# bpy 5.2.2 ile üretildi): çoğu klipte fark < 0,5 mm, ama iç içe geçme çözücüsü bh_over ve kros_l'de başka çözüm buluyor
# (kemik konumu 16 birim-cm). Baştan üretmek var olan vuruşları sessizce değiştirirdi.
# Yol: (1) hat yalnız yeni kliplerle koşar, (2) bu betik o klipleri depodaki FBX'in armatürüne kare kare kopyalar.
#   BS_MODEL=oyuncak BS_OY_TIP=guclu BS_KLIPLER=idle,bh_drop,... blender -b --python tool/karakter/unity_fbx.py -- <ara> oyuncak_guclu
#   blender -b --python tool/karakter/klip_ekle.py -- <Resources/karakter> <ara> <çıktı> oyuncak_guclu
# Çıktı: <ad>.fbx ve <ad>_clips.json (eski klip tablosu + yeni klipler, kare numaraları eskinin sonundan devam eder).
import bpy, sys, os, json
from mathutils import Quaternion
DEPO, ARA, OUT, AD = sys.argv[sys.argv.index('--') + 1:][:4]
bpy.ops.wm.read_homefile(use_empty=True)
sc = bpy.context.scene; sc.render.fps = 30


def oku(yol):
    once = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=yol, automatic_bone_orientation=False, ignore_leaf_bones=False, anim_offset=0.0)
    return [o for o in bpy.data.objects if o not in once]


A = oku(os.path.join(DEPO, AD + '.fbx')); B = oku(os.path.join(ARA, AD + '.fbx'))
ja = json.load(open(os.path.join(DEPO, AD + '_clips.json'), encoding='utf-8'))
jb = json.load(open(os.path.join(ARA, AD + '_clips.json'), encoding='utf-8'))
aa = next(o for o in A if o.type == 'ARMATURE'); ab = next(o for o in B if o.type == 'ARMATURE')
# kemik başına matrix_basis kopyalanır: iki iskeletin dinlenme pozu aynı olmalı (aynı model, aynı tip)
fark = max((aa.data.bones[b.name].matrix_local.translation - b.matrix_local.translation).length for b in ab.data.bones)
aci = max(aa.data.bones[b.name].matrix_local.to_quaternion().rotation_difference(b.matrix_local.to_quaternion()).angle for b in ab.data.bones)
assert len(aa.data.bones) == len(ab.data.bones) and fark < 1e-4 and aci < 1e-3, ('dinlenme pozu farklı', AD, fark, aci)
eski = set(c['name'] for c in ja['clips']); yeni = [c for c in jb['clips'] if c['name'] not in eski]
assert yeni, 'eklenecek klip yok'
son = max(c['last'] for c in ja['clips']); GAP = 10
ad = lambda o: o.name.split('.')[0]
esB = {ad(o): o for o in B if o.type != 'ARMATURE' and o.animation_data and o.animation_data.action}   # raket pivotu, servis topu vekilleri
esA = {ad(o): o for o in A if ad(o) in esB}
onceki = {}
for c in yeni:
    kay = son + GAP - c['first']
    for f in range(c['first'], c['last'] + 1):
        sc.frame_set(f); bpy.context.view_layer.update()
        for pb in ab.pose.bones:
            loc, q, s = pb.matrix_basis.decompose(); t = aa.pose.bones[pb.name]
            if pb.name in onceki and onceki[pb.name].dot(q) < 0: q = -q   # kuaterniyon sürekliliği (Unity'de takla olmasın)
            onceki[pb.name] = q; t.rotation_mode = 'QUATERNION'; t.location, t.rotation_quaternion, t.scale = loc, q, s
            for yol in ('location', 'rotation_quaternion', 'scale'): t.keyframe_insert(yol, frame=f + kay)
        for n, o in esB.items():
            t = esA[n]; t.rotation_mode = o.rotation_mode; t.location, t.scale = o.location, o.scale
            if o.rotation_mode == 'QUATERNION': t.rotation_quaternion = o.rotation_quaternion
            else: t.rotation_euler = o.rotation_euler
            for yol in ('location', 'rotation_quaternion' if o.rotation_mode == 'QUATERNION' else 'rotation_euler', 'scale'): t.keyframe_insert(yol, frame=f + kay)
    k = dict(c); k['first'] = c['first'] + kay; k['last'] = c['last'] + kay; ja['clips'].append(k); son = k['last']
    print('eklendi', k['name'], k['first'], k['last'])
for o in B: bpy.data.objects.remove(o, do_unlink=True)
sc.frame_start, sc.frame_end = 1, son + GAP; sc.frame_set(1)
os.makedirs(OUT, exist_ok=True); fbx = os.path.join(OUT, AD + '.fbx')
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'EMPTY', 'MESH', 'ARMATURE'},   # unity_fbx.py ile aynı ayarlar
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=False,
    use_mesh_modifiers=True, mesh_smooth_type='FACE', use_armature_deform_only=False, add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_step=1.0,
    bake_anim_simplify_factor=0.01, bake_anim_force_startend_keying=True, path_mode='COPY', embed_textures=True)
json.dump(ja, open(os.path.join(OUT, AD + '_clips.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('FBX', fbx, os.path.getsize(fbx), 'klip', len(ja['clips']), 'kare', son + GAP)
