# Unity demosunun önizleme videosu: bulutta Unity çalışmadığı için aynı C# simülasyonunun
# kaydettiği ralliyi (RalliKayit.cs → ralli.json) ve Unity'nin içe aldığı AYNI oyuncu.fbx'i
# Blender'da, Unity sahnesinin kamera/kort/file kurallarıyla çizer. Bu bir Unity render'ı
# değildir; amaç oyun kuralı, kamera ve animasyon zamanlamasını göstermek.
#
# Kullanım: python3 -I tool/unity_test/onizleme.py -- <ralli.json> <oyuncu.fbx> <clips.json> <çıktı_klasörü> [başlangıç bitiş]
# Koordinat: kort (x,y,z) → Blender (x-MID, NET-y, z); Unity'deki W(x,y,z)=(x-MID, z, NET-y) ile aynı yerleşim.
import bpy, sys, json, math, os
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

_mats = {}
def mat(name, col, rough=.5, metal=0., emit=0.):
    if name in _mats: return _mats[name]
    m = bpy.data.materials.new(name); m.use_nodes = True; p = m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value = col; p.inputs['Roughness'].default_value = rough; p.inputs['Metallic'].default_value = metal
    if emit: p.inputs['Emission Color'].default_value = col; p.inputs['Emission Strength'].default_value = emit
    _mats[name] = m; return m

def box(name, c, size, m):
    me = bpy.data.meshes.new(name); o = bpy.data.objects.new(name, me); sc.collection.objects.link(o)
    import bmesh; bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1); bm.to_mesh(me); bm.free()
    o.location = c; o.scale = size; me.materials.append(m); return o

# ---- kort: zemin, yeşil kort, BWF çizgileri, direkler ----
box('arena', (0, 0, -.06), (400, 400, .1), mat('arena', hexc('#1d2b3a'), .9))
box('kort', (0, 0, -.005), (CW + .8, L + .8, .01), mat('kort', hexc('#2f8f5b'), .8))
white = mat('cizgi', hexc('#f4f4f0'), .6); LW = .04
def line(x0, y0, x1, y1):
    p0, p1 = B(x0, y0, 0), B(x1, y1, 0); c = (p0 + p1) / 2; c.z = .002
    box('cizgi', c, (abs(p1.x - p0.x) + LW, abs(p1.y - p0.y) + LW, .004), white)
for x in (0, SL, SR, CW): line(x, 0, x, L)
for y in (0, .76, NET - SSL, NET + SSL, L - .76, L): line(0, y, CW, y)
line(MID, 0, MID, NET - SSL); line(MID, NET + SSL, MID, L)
for x in (0, CW):
    p = B(x, NET, 0); o = bpy.data.objects.new('direk', bpy.data.meshes.new('direk')); sc.collection.objects.link(o)
    import bmesh; bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=.03, radius2=.03, depth=NETP); bm.to_mesh(o.data); bm.free()
    o.location = (p.x, p.y, NETP / 2); o.data.materials.append(mat('direk', hexc('#d9dde3'), .3, .6))

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

# ---- karakter: Unity'nin içe aldığı aynı FBX; kaynak parçalar gizli, iki oyuncu statik kopyalarla ----
before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=FBX); src = [o for o in bpy.data.objects if o not in before]
parts = [o for o in src if o.type == 'MESH']
for o in src: o.hide_render = True; o.hide_viewport = False
SCALE = meta.get('scale', 1.75)
def tag_of(n):
    i = n.find('__'); t = n[i + 2:] if i >= 0 else 'ten'; return t.split('.')[0]
def build(isme, forma, bant, bilek):
    cols = {'forma': forma, 'bant': bant, 'bilek': bilek, 'ten': '#eeb890', 'sort': '#1f2433', 'sac': '#3b2418', 'beyaz': '#f4f4f0',
            'cerceve': '#2b2f3a', 'tel': '#f2f2e6', 'tuy': '#fbfbf6', 'mantar': '#f2b64c'}
    out = []
    for p in parts:
        t = tag_of(p.name); c = p.copy(); c.animation_data_clear(); c.parent = None; c.hide_render = False
        c.data = p.data.copy(); c.data.materials.clear()
        c.data.materials.append(mat(('ben_' if isme else 'rk_') + t, hexc(cols.get(t, '#888888')), .6 if t == 'cerceve' else .55, .4 if t == 'cerceve' else 0))
        sc.collection.objects.link(c); out.append((p, c))
    return out
ME = build(True, '#7b5cff', '#ffd23a', '#23263a'); OP = build(False, '#ff5a52', '#ffffff', '#ffffff')

# Yön kalibrasyonu (PlayerView.Calibrate ile aynı fikir): gözler − baş, yatayda +Y'ye (fileye) döndürülür
sc.frame_set(clips['idle']['first']); bpy.context.view_layer.update()
head = [o for o in parts if o.name.startswith('bas__')][0]; eyes = [o for o in parts if o.name.startswith('goz') and '__' in o.name]
fwd = sum((e.matrix_world.translation for e in eyes), Vector()) / len(eyes) - head.matrix_world.translation; fwd.z = 0
CAL = math.atan2(fwd.x, fwd.y)   # +Y'ye göre açı → ters döndür
zs = [ (o.matrix_world @ Vector(v)).z for o in parts for v in o.bound_box ]
print('karakter yükseklik %.2f m (ölçek öncesi), taban %.2f' % (max(zs) - min(zs), min(zs)))

def pose(set_, st, isme):
    c = clips.get(st['c'], clips['idle']); f = c['first'] + st['p'] * (c['last'] - c['first'])
    sc.frame_set(int(f), subframe=f - int(f))
    j = math.sin(min(1, st['j']) * math.pi) * .35
    lean = max(-.3, min(.3, st['vx'] * .05)) * (1 if isme else -1)
    dive = st['dv'] * (-1 if st['x'] < MID else 1) * .9 * (1 if isme else -1)
    root = (Matrix.Translation(B(st['x'], st['y'], 0) + Vector((0, 0, j)))
            @ Matrix.Rotation(0 if isme else math.pi, 4, 'Z')
            @ Matrix.Rotation((dive + lean) * .6, 4, 'Y')
            @ Matrix.Rotation(CAL, 4, 'Z') @ Matrix.Scale(SCALE, 4))
    for p, cpy in set_: cpy.matrix_world = root @ p.matrix_world

# ---- tüy top: mantar küre + etek koni, hız yönünde; iz = küçük parlayan küreler ----
import bmesh
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

# ---- ışık + kamera (Unity GameRoot.FitCamera ile aynı yerleşim) ----
sun = bpy.data.objects.new('gunes', bpy.data.lights.new('gunes', 'SUN')); sun.data.energy = 3.2; sun.data.angle = math.radians(6)
sun.rotation_euler = (math.radians(40), math.radians(-15), math.radians(25)); sc.collection.objects.link(sun)
sc.world = bpy.data.worlds.new('w'); sc.world.use_nodes = True; sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.35, .42, .5, 1)
sc.world.node_tree.nodes['Background'].inputs[1].default_value = .9
RX, RY = int(os.environ.get('RX', 360)), int(os.environ.get('RY', 780))
cam = bpy.data.objects.new('kamera', bpy.data.cameras.new('kamera')); sc.collection.objects.link(cam); sc.camera = cam
cpos = Vector((0, -(L / 2 + 6.2), 10.5)); tgt = Vector((0, .9, .4))
cq = (tgt - cpos).to_track_quat('-Z', 'Y'); cam.location = cpos; cam.rotation_mode = 'QUATERNION'; cam.rotation_quaternion = cq
aspect = RX / RY; inv = cq.inverted(); tv_ = 0
pts = [B(x, y, 0) for x in (0, CW) for y in (0, L)] + [B(MID, .4, 2.6), B(MID, L + .8, 0)]
for p in pts:
    l = inv @ (p - cpos); tv_ = max(tv_, abs(l.y / l.z), abs(l.x / l.z) / aspect)
FOV = 2 * math.atan(tv_ * 1.06); cam.data.sensor_fit = 'VERTICAL'; cam.data.angle_y = FOV

sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = int(os.environ.get('SPP', 10)); sc.cycles.use_denoising = True
try: sc.cycles.denoiser = 'OPENIMAGEDENOISE'
except Exception: pass
sc.cycles.max_bounces = 3; sc.render.resolution_x = RX; sc.render.resolution_y = RY; sc.render.film_transparent = False
sc.view_settings.view_transform = 'AgX' if 'AgX' in [v.identifier for v in type(sc.view_settings).bl_rna.properties['view_transform'].enum_items] else 'Filmic'

# ---- kare döngüsü ----
hist = []; punch = 1.0; hits = {t['f']: t for t in R['texts'] if 'hit' in t}
meta_out = []
for fi in range(F0, F1):
    fr = frames[fi]
    pose(ME, fr['me'], True); pose(OP, fr['op'], False)
    s = fr.get('s')
    if s:
        p = B(s['x'], s['y'], s['z']); v = Vector((s['vx'], -s['vy'], s['vz']))
        sh.location = p; sh.rotation_mode = 'QUATERNION'
        sh.rotation_quaternion = (-v).to_track_quat('-Z', 'Y') if v.length > 1e-3 else Quaternion()   # mantar önde, etek arkada
        sh.hide_render = False; for_ = [c.hide_render for c in sh.children]
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
    if h and h['hit'] == 'smash': punch = 1.05 if h.get('q') == 'perfect' else 1.035
    cam.data.angle_y = 2 * math.atan(math.tan(FOV / 2) / punch); punch = 1 + (punch - 1) * .82
    sc.render.filepath = os.path.join(OUT, 'k%04d.png' % fi)
    bpy.ops.render.render(write_still=True)
    print('kare', fi, flush=True)
print('bitti')
