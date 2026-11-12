# BSA1 önizleme: birim baş (elipsoit) + kısa saç kabuğu ya da sırt levhası üstünde eşya; Cycles CPU, ızgara.
import bpy, struct, sys, math, os
args = sys.argv[sys.argv.index('--') + 1:]; out = args[0]; ids = args[1:]
AKS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'BirdieSmash', 'Assets', 'Resources', 'karakter', 'aks')
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.device = 'CPU'
def mat(rgb):
    m = bpy.data.materials.new('m'); m.diffuse_color = (*rgb, 1); m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*[c ** 2.2 for c in rgb], 1); return m
def oku(id_):
    b = open(os.path.join(AKS, id_ + '.bytes'), 'rb').read(); tur, n = struct.unpack_from('<BH', b, 4); o = 7
    v = [struct.unpack_from('<6f', b, o + i * 24)[:3] for i in range(n)]; o += n * 24
    g = b[o]; o += 1; faces = []; mats = []; fm = []
    for a in range(g):
        rgb = [x / 255 for x in b[o:o + 3]]; o += 4; tn = struct.unpack_from('<H', b, o)[0]; o += 2
        for i in range(tn): faces.append(struct.unpack_from('<3H', b, o + i * 6)); fm.append(a)
        o += tn * 6; mats.append(mat(rgb))
    return tur, v, faces, mats, fm
# Unity (x sağ, y yukarı, z ileri) → Blender (x sağ, y = -z?, z yukarı): (x, y, z) → (x, -z, y)
def ekle(id_, ox):
    tur, v, faces, mats, fm = oku(id_)
    me = bpy.data.meshes.new(id_); me.from_pydata([(x + ox, -z, y) for x, y, z in v], [], faces); me.update()
    for m in mats: me.materials.append(m)
    for p, a in zip(me.polygons, fm): p.material_index = a
    ob = bpy.data.objects.new(id_, me); sc.collection.objects.link(ob)
    ten = mat((.93, .72, .58)); sac = mat((.17, .11, .08)); forma = mat((.2, .5, .9))
    if tur == 0:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=1, location=(ox, 0, 0)); h = bpy.context.object; h.scale = (.95, 1.2, 1.12); h.data.materials.append(ten)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=1, location=(ox, .12, .12)); k = bpy.context.object; k.scale = (1.04, 1.2, 1.1); k.data.materials.append(sac)
        bpy.ops.mesh.primitive_cylinder_add(radius=.75, depth=1.6, location=(ox, 0, -1.7)); bpy.context.object.data.materials.append(forma)
    else:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(ox, -.22, -.45)); g = bpy.context.object; g.scale = (1.15, .42, 1.2); g.data.materials.append(forma)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=.52, location=(ox, -.2, .62)); bpy.context.object.data.materials.append(ten)
    return tur
turler = [ekle(i, k * 3.4) for k, i in enumerate(ids)]
n = len(ids); cx = (n - 1) * 1.7
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (.5, .5, .55, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 1.0
bpy.ops.object.light_add(type='SUN', rotation=(math.radians(50), 0, math.radians(30))); bpy.context.object.data.energy = 3
arka = turler[0] == 1
cam = bpy.data.cameras.new('c'); co = bpy.data.objects.new('c', cam); sc.collection.objects.link(co); sc.camera = co
cam.type = 'ORTHO'; cam.ortho_scale = n * 3.4 + .4
# şapka: önden-yukarıdan üç çeyrek; sırt: arkadan-yukarıdan (oyun kamerası gibi)
import mathutils
hedef = mathutils.Vector((cx, 0, .9 if not arka else .3))
yon = mathutils.Vector((.45, -1, .55)) if not arka else mathutils.Vector((.4, 1, .6))
co.location = hedef + yon.normalized() * 20
co.rotation_euler = (hedef - co.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.resolution_x = 300 * n; sc.render.resolution_y = 420; sc.render.filepath = out
bpy.ops.render.render(write_still=True)
