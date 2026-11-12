# Gerçek oyuncak FBX üstünde kol eşyası oturma önizlemesi (Aksesuar.Kol'un bpy kopyası). Kullanım: giy.py -- out.png olcek wy zoff id...
import bpy, struct, sys, math, os, mathutils
V = mathutils.Vector
args = sys.argv[sys.argv.index('--') + 1:]; out = args[0]; OLCEK = float(args[1]); KAY_Y = float(args[2]); KAY_YAN = float(args[3]); BOS = float(args[4]); ids = args[5:]
R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'BirdieSmash', 'Assets', 'Resources', 'karakter')
TIP = os.environ.get('TIP', 'oyuncak')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=R + '/' + TIP + '.fbx')
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 12; sc.cycles.device = 'CPU'
def mat(rgb):
    m = bpy.data.materials.new('m'); m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*[c ** 2.2 for c in rgb], 1); return m
GOR = {'oy_govde__govde': (.45, .33, .25), 'oy_forma__forma': (.25, .6, .9), 'sac_kisa__sac': (.12, .1, .09), 'ayk_klasik__ayakkabi': (.9, .9, .92)}
for o in list(bpy.data.objects):
    if o.type == 'MESH':
        if o.name in GOR: o.data.materials.clear(); o.data.materials.append(mat(GOR[o.name]))
        elif o.name.startswith('raket') : pass
        else: o.hide_render = True
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
def merkez(ad):
    o = bpy.data.objects[ad]; return sum((o.matrix_world @ v.co for v in o.data.vertices), V()) / len(o.data.vertices)
# dinlenme pozu
arm.data.pose_position = 'REST'; bpy.context.view_layer.update(); dg = bpy.context.evaluated_depsgraph_get()
W = arm.matrix_world
def kemik(ad): return W @ arm.data.bones[ad].head_local
on, el = kemik('lowerarm_l'), kemik('hand_l')
up = V((0, 0, 1)); eks = el - on; L = eks.length; eks /= L
bas, goz = merkez('bas__isaret'), merkez('goz__isaret')
il = goz - bas; il -= up * il.dot(up); il.normalize(); sag = il.cross(up)
def duz(v, n): return v - n * v.dot(n)
dis = duz(-sag, eks)
if dis.length_squared < .04: dis = duz(-il, eks); print('yedek: arka yüz')
dis.normalize(); yan = dis.cross(eks)
orta = on + eks * L * .5; yuzey = None
for ad in ('oy_govde__govde', 'oy_forma__forma'):
    o = bpy.data.objects[ad].evaluated_get(dg); me = o.to_mesh()
    for v in me.vertices:
        d = o.matrix_world @ v.co - orta
        if abs(d.dot(eks)) > .3 * L or abs(d.dot(yan)) > .45 * L: continue
        z = d.dot(dis)
        if z > .8 * L: continue
        if yuzey is None or z > yuzey: yuzey = z
    o.to_mesh_clear()
if yuzey is None or yuzey < .08 * L or yuzey > .5 * L: yuzey = .22 * L
print('L', L, 'yuzey/L', yuzey / L, 'il', il, 'sag', sag, 'eks', eks, 'dis', dis)
# gövde tarafı: omurgadan uzağa yan kaydırma
gov = kemik('spine_02'); ys = 1 if (orta - gov).dot(yan) > 0 else -1
s = L * OLCEK
poz = orta + dis * (yuzey + BOS * L) + eks * (KAY_Y * L) + yan * (ys * KAY_YAN * L)
M0 = mathutils.Matrix((( yan.x * s, eks.x * s, dis.x * s, poz.x), (yan.y * s, eks.y * s, dis.y * s, poz.y), (yan.z * s, eks.z * s, dis.z * s, poz.z), (0, 0, 0, 1)))
B0 = W @ arm.data.bones['lowerarm_l'].matrix_local
arm.data.pose_position = 'POSE'; sc.frame_set(10); bpy.context.view_layer.update()
B1 = W @ arm.pose.bones['lowerarm_l'].matrix
T = B1 @ B0.inverted() @ M0
def oku(id_):
    b = open(os.path.join(R, 'aks', id_ + '.bytes'), 'rb').read(); tur, n = struct.unpack_from('<BH', b, 4); o = 7
    v = [struct.unpack_from('<6f', b, o + i * 24)[:3] for i in range(n)]; o += n * 24
    g = b[o]; o += 1; faces = []; mats = []; fm = []
    for a in range(g):
        rgb = [x / 255 for x in b[o:o + 3]]; o += 4; tn = struct.unpack_from('<H', b, o)[0]; o += 2
        for i in range(tn): faces.append(struct.unpack_from('<3H', b, o + i * 6)); fm.append(a)
        o += tn * 6; mats.append(mat(rgb))
    return v, faces, mats, fm
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (.5, .45, .6, 1)
bpy.ops.object.light_add(type='SUN', rotation=(math.radians(50), 0, math.radians(200))); bpy.context.object.data.energy = 3
cam = bpy.data.cameras.new('c'); co = bpy.data.objects.new('c', cam); sc.collection.objects.link(co); sc.camera = co; cam.type = 'ORTHO'
boy = (W @ arm.pose.bones['head'].matrix).translation.z; cam.ortho_scale = boy * 1.7; hedef = V((0, 0, boy * .55))
sc.render.resolution_x = 260; sc.render.resolution_y = 300
k = 0
for id_ in ids:
    v, faces, mats, fm = oku(id_)
    me = bpy.data.meshes.new(id_); me.from_pydata([(x, y, z) for x, y, z in v], [], [(a, c, b_) for a, b_, c in faces]); me.update()
    for m in mats: me.materials.append(m)
    for p, a in zip(me.polygons, fm): p.material_index = a
    ob = bpy.data.objects.new(id_, me); sc.collection.objects.link(ob); ob.matrix_world = T
    for aci in (35, 100, 180, 300):
        a = math.radians(aci); yon = V((math.sin(a) * il.x - math.cos(a) * sag.x, math.sin(a) * il.y - math.cos(a) * sag.y, .35))
        # aci 90 = tam önden, 180 = figürün solundan
        yon = il * math.sin(a) + (-sag) * (-math.cos(a)) + up * .3
        co.location = hedef + yon.normalized() * 10; co.rotation_euler = (hedef - co.location).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = f'{out}_{k:02d}.png'; bpy.ops.render.render(write_still=True); k += 1
    ob.hide_render = True
