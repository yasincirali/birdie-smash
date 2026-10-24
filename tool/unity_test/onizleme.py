# Unity demosunun önizleme videosu: bulutta Unity çalışmadığı için aynı C# simülasyonunun kaydettiği ralliyi
# (RalliKayit.cs → ralli.json) ve Unity'nin içe aldığı AYNI karakter FBX'ini Blender'da, Unity sahnesinin kamera, kort,
# saha teması, tribün, hakem, file ve ışık kurallarıyla çizer. Bu bir Unity render'ı değildir; amaç oyun kuralı, kamera
# ve animasyon zamanlamasını göstermek.
# 2026-10-24: karakter iskeletli (Oyuncak 3B / Gerçekçi). Her figür FBX'ten ayrı içe alınır; eğriler sahne karesinden
# bağımsız, figür başına kendi klip zamanında değerlendirilir (iki oyuncu aynı anda farklı klipte).
#
# Kullanım: python3 -I tool/unity_test/onizleme.py -- <ralli.json> <model.fbx> <model_clips.json> <çıktı_klasörü> [başlangıç bitiş]
# Koordinat: kort (x,y,z) → Blender (x-MID, NET-y, z); Unity'deki W(x,y,z)=(x-MID, z, NET-y) ile aynı yerleşim.
import bpy, sys, json, math, os, random
from mathutils import Vector, Matrix, Quaternion

a = sys.argv[sys.argv.index('--') + 1:]
RALLI, FBX, CLIPS, OUT = a[:4]
F0, F1 = (int(a[4]), int(a[5])) if len(a) > 5 else (0, None)
os.makedirs(OUT, exist_ok=True)

L, CW = 13.4, 6.1; NET = L / 2; MID = CW / 2; SL, SR, SSL = .46, CW - .46, 1.98; NETH, NETP = 1.524, 1.55
def B(x, y, z): return Vector((x - MID, NET - y, z))
def net_limit(x): return NETH + (NETP - NETH) * abs(x - MID) / MID
def hexc(h, a=1.0): h = h.lstrip('#'); return tuple(((int(h[i:i + 2], 16) / 255) ** 2.2) for i in (0, 2, 4)) + (a,)

bpy.ops.wm.read_homefile(use_empty=True)   # factory ayarı yasak (3d-animator kuralı)
sc = bpy.context.scene
R = json.load(open(RALLI)); meta = json.load(open(CLIPS)); clips = {c['name']: c for c in meta['clips']}
frames = R['frames']; F1 = F1 if F1 is not None else len(frames)
SAHA = {'salon': ('#23725f', '#278066', '#1b4f45', '#13262a', .6), 'turnuva': ('#1f7a55', '#22845c', '#1c3d6b', '#101d33', .92),
        'okul': ('#a4703f', '#ab7744', '#7c5230', '#21170f', .3), 'park': ('#2f5674', '#335d7d', '#22394c', '#0c1420', .38)}
KORT, TEK, CEVRE, UST, DOL = SAHA.get(R.get('saha', 'salon'), SAHA['salon'])

_mats = {}
def mat(name, col, rough=.5, metal=0., emit=0.):
    if name in _mats: return _mats[name]
    m = bpy.data.materials.new(name); m.use_nodes = True; p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = col; p.inputs['Roughness'].default_value = rough; p.inputs['Metallic'].default_value = metal
    if emit: p.inputs['Emission Color'].default_value = col; p.inputs['Emission Strength'].default_value = emit
    _mats[name] = m; return m

import bmesh
def box(name, c, size, m):
    me = bpy.data.meshes.new(name); o = bpy.data.objects.new(name, me); sc.collection.objects.link(o)
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1); bm.to_mesh(me); bm.free()
    o.location = c; o.scale = size; me.materials.append(m); return o
def ball(name, c, r, m, s=(1, 1, 1)):
    me = bpy.data.meshes.new(name); bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=r); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); o.location = c; o.scale = s; me.materials.append(m); return o
def cyl(name, c, r, h, m):
    me = bpy.data.meshes.new(name); bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=r, radius2=r, depth=h); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); o.location = c; me.materials.append(m); return o

# ---- kort: salon zemini, çevre, kort, tekler bandı, BWF çizgileri, direkler (CourtView ile aynı) ----
box('arena', (0, 0, -.07), (400, 400, .1), mat('arena', hexc(UST), .9))
box('cevre', (0, 0, -.012), (CW + 3.2, L + 4.2, .01), mat('cevre', hexc(CEVRE), .8))
box('kort', (0, 0, -.006), (CW + .1, L + .1, .01), mat('kort', hexc(KORT), .7))
box('tekler', (0, 0, -.004), (SR - SL, L, .01), mat('tek', hexc(TEK), .7))
white = mat('cizgi', hexc('#f4f4f0'), .6); LW = .04
def line(x0, y0, x1, y1):
    p0, p1 = B(x0, y0, 0), B(x1, y1, 0); c = (p0 + p1) / 2; c.z = .002
    box('cizgi', c, (abs(p1.x - p0.x) + LW, abs(p1.y - p0.y) + LW, .004), white)
for x in (0, SL, SR, CW): line(x, 0, x, L)
for y in (0, .76, NET - SSL, NET + SSL, L - .76, L): line(0, y, CW, y)
line(MID, 0, MID, NET - SSL); line(MID, NET + SSL, MID, L)
for x in (0, CW): p = B(x, NET, 0); cyl('direk', (p.x, p.y, NETP / 2), .03, NETP, mat('direk', hexc('#d9dde3'), .3, .6))

# ---- tribün yalnız rakibin arkasında: pano + 2 sıra oturan seyirci (CourtView.Tribun ile aynı ölçü) ----
rnd = random.Random(7); giysi = ['#e85d4f', '#3f7fd8', '#f2c14e', '#f4f1ea', '#2c3e50', '#8e5bd1', '#3fb37f', '#ff8a3c', '#d94f8a', '#5fc4d9']; ten = ['#f1c9a5', '#d9a57a', '#a8714a', '#6e4a2f', '#f5d6bb']
X0, X1, PANO, DER, YUK = -1.6, CW + 1.6, -1.3, .55, .42
x = X0
while x < X1:
    x2 = min(X1, x + 2); box('pano', B((x + x2) / 2, PANO, .4), (x2 - x, .06, .8), mat('pano%d' % (int((x - X0) / 2) % 2), hexc('#1d2b3a' if int((x - X0) / 2) % 2 else '#243a4f'), .6)); x += 2
for r in range(2):
    ya = PANO - .15 - r * DER; z = .55 + r * YUK
    box('basamak', B(MID, ya - DER / 2, z / 2), (X1 - X0, DER, z), mat('bas%d' % r, hexc('#1f2e37' if r == 0 else '#16222a'), .8))
    xx = X0 + .3
    while xx < X1 - .2:
        if rnd.random() <= DOL:
            px = xx + (rnd.random() - .5) * .1; yy = ya - DER * .5
            ball('seyirci', B(px, yy, z + .27), .2, mat('g' + giysi[rnd.randrange(10)], hexc(giysi[rnd.randrange(10)]), .8), (.9, .7, 1.35))
            ball('bas', B(px, yy, z + .66), .11, mat('t' + ten[rnd.randrange(5)], hexc(ten[rnd.randrange(5)]), .7))
            if rnd.random() < .55: ball('sac', B(px, yy - .015, z + .7), .115, mat('sac', hexc('#1e1610'), .8), (1, 1, .7))
        xx += .55

# ---- hakem sandalyeleri ve servis ölçeri ----
metal = mat('metal', hexc('#cfd6d4'), .3, .6); koyu = mat('koyu', hexc('#3a4a52'), .6)
for dy in (-.25, .25): cyl('ayak', B(-.72, NET + dy, .72), .02, 1.44, metal)
box('ayaklik', B(-.6, NET, .93), (.5, .6, .04), koyu)
for dx in (-.15, .15): cyl('ayak', B(CW + .55 + dx, NET - .1, .2), .015, .4, metal)
box('oturak', B(CW + .55, NET - .1, .41), (.4, .4, .05), koyu)
cyl('olcer', B(CW + .1, NET + .2, .575), .01, 1.15, mat('olc', hexc('#f0f0f0'), .4)); box('olcer_c', B(CW + .1, NET + .2, 1.15), (.16, .02, .02), mat('olc', hexc('#f0f0f0'), .4))

# ---- file: 66×12 ızgara, NetWave profilinden esner (kazanç 1,8), tel örgü = wireframe ----
NX, NZ, DEPTH, GAIN = 66, 12, .76, 1.8
netme = bpy.data.meshes.new('file'); verts = []; faces = []
for j in range(NZ):
    for i in range(NX):
        x = i / (NX - 1) * CW; z = net_limit(x) - DEPTH + DEPTH * j / (NZ - 1); verts.append(B(x, NET, z))
for j in range(NZ - 1):
    for i in range(NX - 1):
        q = j * NX + i; faces.append((q, q + 1, q + NX + 1, q + NX))
netme.from_pydata(verts, [], faces); netob = bpy.data.objects.new('file', netme); sc.collection.objects.link(netob)
wm = netob.modifiers.new('ag', 'WIREFRAME'); wm.thickness = .012; netme.materials.append(mat('ag', hexc('#e9f1ee'), .7))
tapeme = bpy.data.meshes.new('bant'); tv = []; tf = []
for i in range(NX):
    x = i / (NX - 1) * CW; t = net_limit(x); tv += [B(x, NET, t - .038), B(x, NET, t + .002)]
for i in range(NX - 1): tf.append((2 * i, 2 * i + 2, 2 * i + 3, 2 * i + 1))
tapeme.from_pydata(tv, [], tf); tapeob = bpy.data.objects.new('bant', tapeme); sc.collection.objects.link(tapeob)
tapeme.materials.append(mat('bantm', hexc('#ffffff'), .5, emit=.3))

def net_at(nw, x, zz):
    f = x / CW * 32; i = math.floor(f); t = f - i; arr = nw['a']
    a0 = arr[max(0, min(32, i))]; a1 = arr[max(0, min(32, i + 1))]; ax = a0 * (1 - t) + a1 * t; w = .42
    return ax * math.exp(-((zz - nw['z']) ** 2) / (2 * w * w)) * (1 - (max(0, zz) / NETH) ** 6)
def net_update(nw, sign):
    vs = netme.vertices
    for j in range(NZ):
        for i in range(NX):
            x = i / (NX - 1) * CW; z = net_limit(x) - DEPTH + DEPTH * j / (NZ - 1)
            vs[j * NX + i].co = B(x, NET + net_at(nw, x, z) * GAIN * sign, z)
    for i in range(NX):
        x = i / (NX - 1) * CW; t = net_limit(x); wob = math.sin(math.pi * x / CW) * nw['tape'] * .06 * sign
        tapeme.vertices[2 * i].co = B(x, NET + wob, t - .038); tapeme.vertices[2 * i + 1].co = B(x, NET + wob, t + .002)
    netme.update(); tapeme.update()

# ---- karakter: Unity'nin içe aldığı aynı FBX; her figür ayrı kopya, eğriler figürün kendi klip zamanında ----
SCALE = meta.get('scale', 1.75)
def fcurves_of(o):
    ad = o.animation_data; act = ad.action if ad else None
    if not act: return []
    try: return list(act.fcurves)
    except AttributeError: pass
    from bpy_extras import anim_utils
    cb = anim_utils.action_get_channelbag_for_slot(act, ad.action_slot)
    return list(cb.fcurves) if cb else []
def import_char(forma, hakem=False):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=FBX, use_image_search=True); new = [o for o in bpy.data.objects if o not in before]
    root = [o for o in new if o.parent is None and o.type == 'EMPTY'][0]
    chans = []
    for o in new:
        for fc in fcurves_of(o):
            dp = fc.data_path
            if dp.endswith('rotation_quaternion'):
                tgt = o.path_resolve(dp.rsplit('.', 1)[0]) if '.' in dp else o
                tgt.rotation_mode = 'QUATERNION'
            chans.append((o, dp, fc.array_index, fc))
    for o in new:
        if o.animation_data: o.animation_data_clear()
    fm = mat('forma_' + forma, hexc(forma), .55); navy = mat('lacivert', hexc('#0a1530'), .6)
    for o in new:
        if o.type != 'MESH': continue
        n = o.name; t = n[n.find('__') + 2:].split('.')[0] if '__' in n else ''
        skinned = any(m.type == 'ARMATURE' for m in o.modifiers)
        if t in ('isaret', 'bant', 'bilek') or (hakem and not skinned): o.hide_render = True; continue
        if t == 'forma' or (hakem and t == 'sort'):
            o.data = o.data.copy(); o.data.materials.clear(); o.data.materials.append(fm if not hakem or t == 'forma' else navy)
    R0 = root.matrix_world.copy()
    ch = {'root': root, 'R0': R0, 'chans': chans, 'objs': new}
    pose_at(ch, 'idle', 0); bpy.context.view_layer.update()
    hd = [o for o in new if o.name.startswith('bas__')]; ey = [o for o in new if o.name.startswith('goz__')]
    cal = 0
    if hd and ey:
        f = ey[0].matrix_world.translation - hd[0].matrix_world.translation; f.z = 0; cal = -math.atan2(-f.x, f.y) if f.length > 1e-6 else 0
    ch['cal'] = cal; return ch
def clip_frame(c, ph):
    cl = clips.get(c, clips['idle']); n = cl.get('poses', 2)
    if cl.get('loop'): t = ph % 1
    else: t = min(1, max(0, (ph * n - .5) / max(1, n - 1)))   # PlayerView.ClipTime ile aynı
    return cl['first'] + t * (cl['last'] - cl['first'])
def pose_at(ch, c, ph):
    f = clip_frame(c, ph)
    for o, dp, idx, fc in ch['chans']:
        try: o.path_resolve(dp)[idx] = fc.evaluate(f)
        except Exception: pass
def place(ch, st, isme, yaw=None, z0=0., tilt=0.):
    pose_at(ch, st['c'], st['p']) if st else None
    j = st['j'] * .5 if st else 0; tl = (st['tilt'] if st else tilt) * (1 if isme else -1)
    rz = (0 if isme else math.pi) if yaw is None else yaw
    pos = B(st['x'], st['y'], 0) if st else Vector((0, 0, 0))
    ch['root'].matrix_world = (Matrix.Translation(pos + Vector((0, 0, j + z0))) @ Matrix.Rotation(rz, 4, 'Z') @ Matrix.Rotation(tl, 4, 'Y')
                               @ Matrix.Rotation(ch['cal'], 4, 'Z') @ Matrix.Scale(SCALE, 4) @ ch['R0'])
ME = import_char('#4fb8ff'); OP = import_char(R.get('renk', '#ff7a6b'))
HK1 = import_char('#0a1530', True); HK2 = import_char('#0a1530', True)

# ---- tüy top: mantar küre + etek koni, hız yönünde; iz = küçük parlayan küreler ----
sh = bpy.data.objects.new('top', None); sc.collection.objects.link(sh)
def child(name, build_fn, m, loc):
    me = bpy.data.meshes.new(name); bm = bmesh.new(); build_fn(bm); bm.to_mesh(me); bm.free(); me.materials.append(m)
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); o.parent = sh; o.location = loc
    for poly in me.polygons: poly.use_smooth = True
    return o
K = 2.2   # okunurluk büyütmesi, Unity ShuttleView ile aynı
child('mantar', lambda bm: bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=.0135 * K), mat('mantarm', hexc('#f2b64c'), .5), (0, 0, 0))
child('etek', lambda bm: bmesh.ops.create_cone(bm, cap_ends=False, segments=16, radius1=.012 * K, radius2=.034 * K, depth=.062 * K), mat('etekm', hexc('#fbfbf6'), .6, emit=.15), (0, 0, -.04 * K))
trail = []
for i in range(8):
    me = bpy.data.meshes.new('iz'); bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=.02 * K * (1 - i / 9)); bm.to_mesh(me); bm.free()
    me.materials.append(mat('iz%d' % i, (1, .95, .8, 1), .5, emit=3 * (1 - i / 8))); o = bpy.data.objects.new('iz', me); sc.collection.objects.link(o); trail.append(o)

# ---- yayın ışığı: kısık anahtar ışık + kortun boyunca üç tavan lambası (CourtView.Isik) ----
sun = bpy.data.objects.new('gunes', bpy.data.lights.new('gunes', 'SUN')); sun.data.energy = 1.4; sun.data.angle = math.radians(8)
sun.rotation_euler = (math.radians(28), math.radians(-12), math.radians(24)); sc.collection.objects.link(sun)
for yy in (NET * .45, NET, NET * 1.55):
    l = bpy.data.objects.new('lamba', bpy.data.lights.new('lamba', 'SPOT')); l.data.energy = 2600; l.data.spot_size = math.radians(70); l.data.spot_blend = .6
    l.data.color = (1, .965, .88); l.location = B(MID, yy, 9); sc.collection.objects.link(l)
sc.world = bpy.data.worlds.new('w'); sc.world.use_nodes = True; sc.world.node_tree.nodes['Background'].inputs[0].default_value = hexc(UST)
sc.world.node_tree.nodes['Background'].inputs[1].default_value = 1.6
RX, RY = int(os.environ.get('RX', 360)), int(os.environ.get('RY', 780))
cam = bpy.data.objects.new('kamera', bpy.data.cameras.new('kamera')); sc.collection.objects.link(cam); sc.camera = cam
cpos = Vector((0, -(L / 2 + 6.2), 10.5)); tgt = Vector((0, .9, .4))
cq = (tgt - cpos).to_track_quat('-Z', 'Y'); cam.location = cpos; cam.rotation_mode = 'QUATERNION'; cam.rotation_quaternion = cq
aspect = RX / RY; inv = cq.inverted(); tv_ = 0
pts = [B(x, y, 0) for x in (0, CW) for y in (0, L)] + [B(MID, .4, 2.6), B(MID, L + .8, 0), B(MID, -1.3, .9)]
for p in pts:
    l = inv @ (p - cpos); tv_ = max(tv_, abs(l.y / l.z), abs(l.x / l.z) / aspect)
FOV = 2 * math.atan(tv_ * 1.06); cam.data.sensor_fit = 'VERTICAL'; cam.data.angle_y = FOV

sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = int(os.environ.get('SPP', 10)); sc.cycles.use_denoising = True
try: sc.cycles.denoiser = 'OPENIMAGEDENOISE'
except Exception: pass
sc.cycles.max_bounces = 3; sc.render.resolution_x = RX; sc.render.resolution_y = RY; sc.render.film_transparent = False
sc.view_settings.view_transform = 'AgX' if 'AgX' in [v.identifier for v in type(sc.view_settings).bl_rna.properties['view_transform'].enum_items] else 'Filmic'

# hakemler: baş hakem kortun soluna (x −0,6) sandalyenin ayaklığında, servis hakemi sağda (GameRoot.Hakemler ile aynı açı kuralı)
def hakemler(hk):
    yaw = 90; tilt = 0; e = hk['e']
    if e >= 0:
        if hk['so']: yaw = 90 + (30 if hk['yon'] == 'me' else -30) * min(1, max(0, e * 3))
        elif e < 1.05: tilt = 14 * math.sin(min(1, max(0, e)) * math.pi)
    # Unity yaw (y ekseni, +z'den saat yönü) → Blender (z ekseni, +y'den saat yönünün tersi): -yaw
    place(HK1, None, True, yaw=math.radians(-yaw), z0=.95); HK1['root'].matrix_world = Matrix.Translation(B(-.6, NET, .95)) @ Matrix.Rotation(math.radians(-yaw), 4, 'Z') @ Matrix.Rotation(math.radians(tilt), 4, 'X') @ Matrix.Rotation(HK1['cal'], 4, 'Z') @ Matrix.Scale(SCALE, 4) @ HK1['R0']
    yeni = hk['sv']; eski = hk['sj'] or yeni; k = min(1, hk['sjT'] / .5) if eski != yeni and hk['sjT'] > 0 else 0
    a0 = -125 if eski == 'me' else -55; a1 = -125 if yeni == 'me' else -55; ya = a0 + (a1 - a0) * k
    HK2['root'].matrix_world = Matrix.Translation(B(CW + .55, NET + .35, 0)) @ Matrix.Rotation(math.radians(-ya), 4, 'Z') @ Matrix.Rotation(HK2['cal'], 4, 'Z') @ Matrix.Scale(SCALE, 4) @ HK2['R0']
pose_at(HK1, 'idle', .25); pose_at(HK2, 'idle', .25)

# ---- kare döngüsü ----
hist = []; punch = 1.0; hits = {t['f']: t for t in R['texts'] if 'hit' in t}
for fi in range(F0, F1):
    fr = frames[fi]
    place(ME, fr['me'], True); place(OP, fr['op'], False); hakemler(fr['hk'])
    s = fr.get('s')
    if s:
        p = B(s['x'], s['y'], s['z']); v = Vector((s['vx'], -s['vy'], s['vz']))
        sh.location = p; sh.rotation_mode = 'QUATERNION'
        sh.rotation_quaternion = (-v).to_track_quat('-Z', 'Y') if v.length > 1e-3 else Quaternion()   # mantar önde, etek arkada
        for c in sh.children: c.hide_render = False
        hist.insert(0, p.copy()); hist = hist[:9]
    else:
        for c in sh.children: c.hide_render = True
        hist = []
    fast = s and s.get('sm', 0)
    for i, o in enumerate(trail):
        if i + 1 < len(hist) and (fast or i < 4): o.location = hist[i + 1]; o.hide_render = False
        else: o.hide_render = True
    net_update(fr['net'], -1 if fr['net'].get('from') == 'me' else 1)
    h = hits.get(fi)
    if h and h['hit'] == 'smash': punch = 1.09 if h.get('big') else 1.05
    cam.data.angle_y = 2 * math.atan(math.tan(FOV / 2) / punch); punch = 1 + (punch - 1) * .82
    bpy.context.view_layer.update()
    sc.render.filepath = os.path.join(OUT, 'k%04d.png' % fi)
    bpy.ops.render.render(write_still=True)
    print('kare', fi, flush=True)
print('bitti')
