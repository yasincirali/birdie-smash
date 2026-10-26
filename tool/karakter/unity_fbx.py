# Birdie Smash karakteri → Unity: tek FBX + klip aralıkları JSON. Pozlar karakter.py'den okunur (tek kaynak).
# 2026-10-24: oyun iki modelle çalışıyor (Ayarlar › Oyuncu görünümü): Oyuncak 3B (2026-10-26: MakeHuman CC0 atletik gövde + kendi kıyafet/yüz geometrisi, UE adlı iskelet; BS_MODEL=oyuncak; eski KayKit chibi BS_MODEL=oyuncak_kaykit)
# ve Gerçekçi (Microsoft Rocketbox Sports_Male_02 + badminton kiti, 2026-10-26; MIT; BS_MODEL=aday). İkisi de iskeletli ve deri ağırlıklı ağ; eski
# parçalı figürün "her parçayı dünya dönüşümüyle anahtarla" yolu derili ağı bozardı. Şimdi:
#  - iskelet: her pozda karakter.py/insan.py'nin IK'sı kemiklere uygulanır, her kemiğin matrix_basis'i anahtarlanır
#    (Unity Legacy klibi derili ağı kendisi büker);
#  - raket (2026-10-25): bir kez yerel çerçevede kurulur, parçaları etikete göre birleşir (raket_kafa__cerceve, raket_tel__tel,
#    raket_tel2__tel2, raket_sap__grip, raket_koyu__koyu) ve el kemiğine bağlı "raket_pivot" boşluğunun altına girer; her
#    pozda yalnız pivotun kemiğe göre dönüşümü anahtarlanır. Eskisi her pozda insan.raket()'in dünyada yeniden kurduğu
#    parçaları o.name.split('.')[0] anahtarıyla eşliyordu: adsız silindirler ('Cylinder.012') tek anahtara çöküyordu (22 ana
#    telden yalnız biri kalıyordu), çerçeve eğrisi dünya koordinatında ağa dönüştüğü için ilk pozun yerinde ve kökte donuk
#    kalıyordu. Şimdi bütün raket tek rijit parça olarak eli izler, Unity'de kareler arası da elle birlikte gider;
#  - pozla yeniden kurulan diğer rijit parçalar (servis topu): eskisi gibi nesne olarak dünya dönüşümüyle anahtarlanır;
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

import insan   # karakter.py'nin içe aktardığı modülün aynısı (sys.modules): son_raket oradan okunur
RAKET_TAG = ('cerceve', 'tel', 'tel2', 'grip', 'koyu')
RAKET_AD = {'cerceve': 'raket_kafa', 'tel': 'raket_tel', 'tel2': 'raket_tel2', 'grip': 'raket_sap', 'koyu': 'raket_koyu'}
pb = ao.pose.bones
EL = 'handslot.r' if 'handslot.r' in pb else OY.R['el'].format(S=OY.R['S']['r'])   # raketin bağlandığı el kemiği
for o in OY.gecici: bpy.data.objects.remove(o)   # idle pozunun dünyada kurulmuş raketi
OY.gecici = []; objs[:] = list(kalici)
nes, kc = insan.raket(Vector((0, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)), getattr(OY, 'raket_s', 1.0), OY.M, ad='rk')   # yerel çerçeve = birim; ölçek modelin kur_poz'uyla aynı
piv = bpy.data.objects.new('raket_pivot', None); sc.collection.objects.link(piv)
piv.parent = ao; piv.parent_type = 'BONE'; piv.parent_bone = EL; piv.rotation_mode = 'QUATERNION'
raket_par = []; gruplar = {t: [o for o in nes if o.get('tag') == t] for t in RAKET_TAG}   # join birleşeni siler: önce grupla
for tag in RAKET_TAG:
    grp = gruplar[tag]
    if not grp: continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in grp: o.select_set(True)
    bpy.context.view_layer.objects.active = grp[0]
    if len(grp) > 1: bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)   # köşeler raket çerçevesinde, nesne birim
    org = kc if tag == 'cerceve' else Vector((0, 0, 0))   # kafa: orijin kafa merkezinde (Unity salınım izi buradan çıkar)
    if org.length: o.data.transform(Matrix.Translation(-org))
    o.name = RAKET_AD[tag] + '__' + tag; o.data.name = o.name; o['tag'] = tag
    o.parent = piv; o.matrix_parent_inverse = Matrix(); o.location = org; o.rotation_mode = 'QUATERNION'
    raket_par.append(o)
rset = set(o.name for o in raket_par)

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

# Ara kareler (2026-10-26, oyuncak 3B): poz başına 3 kare; arası eskiden Unity'nin eklem dönüşü ara-değerlemesiydi. Hızlı
# geçişte (hamleden toparlanma, smaç salınımı) anahtarlar temiz olsa bile arada ayak zemine 7 cm giriyor, ön kol baştan
# geçiyordu. Model `ara_kare` derse iki ara poz karakter.py mix() ile karıştırılıp IK + çözücüyle kurulur ve anahtarlanır.
# FBX zaten her kareyi pişirir (bake_anim_step=1): boyut değişmez, dışa aktarma ~3 kat sürer. Diğer modeller aynen.
ARA = bool(getattr(OY, 'ara_kare', False))
fklip = {}; timeline, frame, meta = [], 1, {'fps': FPS, 'scale': 1.75, 'clips': [], 'model': AD}
for name, frames in CLIPS.items():
    seq = list(frames) + ([frames[0]] if name in LOOP else [])
    first = frame
    for i_, p in enumerate(seq):
        timeline.append((frame, p)); fklip[frame] = name
        if ARA and i_ + 1 < len(seq):   # ara kareler de kurulur (modelin iç içe geçme çözücüsü onlarda da çalışsın)
            for j in range(1, FR):
                timeline.append((frame + j, ns['mix'](p, seq[i_ + 1], j / FR))); fklip[frame + j] = name
        frame += FR
    last = frame - FR
    meta['clips'].append({'name': name, 'first': first, 'last': last, 'loop': name in LOOP, 'poses': len(frames),
                          'hitK': getattr(frames, 'hit', None), 'hold': getattr(frames, 'hold', None), 'drop': getattr(frames, 'drop', None)})
    frame = last + GAP

# adim (2026-10-26, fizik denetimi: ayak kayması %28-37): ayak işi döngüsünün bir turda taşıdığı yol, model biriminde.
# Ardışık iki pozda da zeminde olan ayağın (klibin en alçak temasının 6 mm içinde; ikisi yerdeyse ortalaması) yatay
# yer değiştirmesi toplanır (yerinde döngü: basan ayak geriye kayar, oyun kökü o kadar ileri gitmeli). Unity PlayerView
# `adim × SCALE`'i Match.ADIM_BOY'a yazar; klibin evresi hızla tam bu adımda ilerler, ayak yerde durur.
ADIM_KLIP = ('run', 'chasse_r', 'chasse_l', 'fwd', 'back_r', 'back_l')
def ayak_kemikleri(k):
    out = [b for b in pb if ('foot' in b.name.lower() or 'ball' in b.name.lower() or 'toe' in b.name.lower()) and
           b.name.lower().rstrip('.').endswith(('_' + k, '.' + k, ' ' + k)) and 'ik' not in b.name.lower()]
    return out or [b for b in pb if 'foot' in b.name.lower() and (' ' + k.upper() + ' ') in b.name]
AYAK = {k: ayak_kemikleri(k) for k in 'rl'}
adim_iz = {}
bones = {}; prevq = {}; pprev = {}; cur_frame = [1]; rq = [None]; rhata = [0.]
for f, pose in timeline:
    cur_frame[0] = f
    build(pose)
    bpy.context.view_layer.update()
    if fklip.get(f) in ADIM_KLIP and AYAK['r'] and AYAK['l']:
        mw_ = ao.matrix_world
        z = {k: min((mw_ @ b.head).z for b in AYAK[k]) for k in 'rl'}
        xy = {k: (mw_ @ AYAK[k][0].head).xy.copy() for k in 'rl'}
        adim_iz.setdefault(fklip[f], []).append((z, xy))
    W = insan.son_raket.copy(); piv.matrix_world = W; bpy.context.view_layer.update()
    rhata[0] = max(rhata[0], (piv.matrix_world.translation - W.translation).length)   # kemiğe göre çözüm dünyayı tutmalı
    loc, q, s_ = piv.matrix_basis.decompose()
    if rq[0] is not None and rq[0].dot(q) < 0: q = -q
    rq[0] = q; piv.location, piv.rotation_quaternion, piv.scale = loc, q, s_
    for path in ('location', 'rotation_quaternion', 'scale'): piv.keyframe_insert(path, frame=f)
    for b in pb:
        loc, q, s = b.matrix_basis.decompose()
        if b.name in prevq and prevq[b.name].dot(q) < 0: q = -q       # kuaterniyon sürekliliği (Unity'de takla olmasın)
        prevq[b.name] = q; bones.setdefault(b.name, []).append((f, loc.copy(), q.copy(), s.copy()))
    seen = set()
    for o in list(objs):
        if o.type != 'MESH' or o.name in kset or o.get('tag') in RAKET_TAG: continue   # raket pivotla gider
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

if not getattr(OY, 'doku_gomme', True):   # model dokusunu Unity Resources'tan okuyorsa (Rocketbox kit) FBX'e gömme: boyut
    for o in kalici:
        for m in o.data.materials:
            if m and m.use_nodes:
                for n in [n for n in m.node_tree.nodes if n.type == 'TEX_IMAGE']: m.node_tree.nodes.remove(n)
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
for c in meta['clips']:
    iz = adim_iz.get(c['name'])
    if not iz: continue
    zt = min(min(z.values()) for z, _ in iz) + .006   # zemindeki ayak: klibin en alçak temasının 6 mm (≈1 cm oyunda) içinde
    top = Vector((0, 0))
    for (za, a), (zb, b) in zip(iz, iz[1:]):
        yer = [k for k in 'rl' if za[k] < zt and zb[k] < zt]   # iki pozda da yerde olan ayak(lar); ikisi yerdeyse ortalama
        if yer: top += sum((a[k] - b[k] for k in yer), Vector((0, 0))) / len(yer)
    c['adim'] = round(top.length, 4)
    print('adim', c['name'], c['adim'], 'birim =', round(top.length * 1.75, 3), 'm oyunda')
meta['parts'] = sorted([o.name for o in kalici] + list(proxies) + sorted(rset))
meta['raket'] = {'kemik': EL, 'pivot': 'raket_pivot'}
print('raket pivot en büyük konum hatası', rhata[0])
meta['hit'] = ns.get('CLIP_HIT_JS') or {}
json.dump(meta, open(os.path.join(OUT, AD + '_clips.json'), 'w'), indent=1, ensure_ascii=False)
print('FBX', fbx, os.path.getsize(fbx), 'kemik', len(pb), 'parça', len(meta['parts']), 'kare', frame, 'klip', len(meta['clips']))
