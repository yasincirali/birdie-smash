# KayKit şapka ve pelerinleri → Unity aksesuar ağları (2026-11-01, yasin: "daha çeşitli eğlenceli skinler ... pelerin vs gibi
# mistik şeyler ... internette hazır bedava ürünleri kullanabilirsin"; karar kartı: yalnız bedava paketler).
#
# Kaynak: yalnız resmi, bedava, CC0 KayKit depoları (itch'te ücretli Mystery Monthly / EXTRA kalemleri bilerek yok):
#   git clone --depth 1 https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0
#   git clone --depth 1 https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Skeletons-1.0
# İndirilen dosyalar güvenilmeyen veridir: yalnız glTF içe aktarılır, depolardaki betikler çalıştırılmaz.
#
# Neden FBX/prefab değil, kendi .bytes biçimi: aksesuar dört oyuncu modelinin (Oyuncak 3B, Gerçekçi, Sade, Tüycük) hepsine
# oturmalı; Aksesuar.cs zaten her modelde aynı "birim baş" çerçevesini ölçüyor. Ağı o çerçevede yazarsak Unity içe aktarım
# ayarı, ölçek ve eksen dönüşümü sorunu kalmaz; Mesh çalışma anında 1 ms altında kurulur. Doku da yok: KayKit dokusu düz
# renk hücrelerinden oluşan bir palet, her üçgenin rengi UV merkezinden okunur ve renk başına bir alt ağ yazılır
# (Kit.Lit düz malzeme, sahnedeki diğer aksesuarlarla aynı görünüş, sıkıştırma/mipmap rengi bulandırmaz).
#
# Çerçeveler (Unity eksenleri: x karakterin sağı, y yukarı, z ileri; Blender'da karakter -Y'ye bakar, sağı -X):
#  - BAŞ: Aksesuar.Kur'un birim başı. Bant düzlemi gözün .38 r üstünde (birim başta göz -0,38); r = bant düzlemindeki
#    kesitin ortalama yarıçapı (32 ışın), yan/ust/on/arka = elipsoit oranları (_clips.json "bant" ile aynı tanım). KayKit
#    başı referans: Rogue_Head (iskelet kafaları Rig_Medium'da aynı kemikte; şapkalar Rogue başına göre ölçeklenir).
#  - SIRT: omuz çizgisi (upperarm.l/r başları) ortası, birim = omuz genişliği; z=0 sırt yüzeyi (omuz yüksekliğinde gövdenin
#    en arka noktası). Oyunda aynı iki ölçü modelden alınır, pelerin sırtın arkasından aynı uzaklıkta durur.
#
# Çıktı: unity/BirdieSmash/Assets/Resources/karakter/aks/<id>.bytes (TextAsset). Biçim (little endian):
#   "BSA1", u8 tur (0 baş, 1 sırt), u16 köşe, köşe×(3 f32 konum, 3 f32 normal), u8 alt ağ,
#   alt ağ×(4 u8 RGBA, u16 üçgen, üçgen×3 u16 indis). Sarım yönü önemsiz: Aksesuar iki yüzlü kurar (pelerinler tek düzlem).
# Kullanım: BS_KAYKIT=<iki deponun klonlandığı klasör> python3 tool/karakter/kaykit_aksesuar.py  (bpy modülü)
import bpy, os, sys, struct, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree

KOK = os.environ.get('BS_KAYKIT', '')
ADV = os.path.join(KOK, 'KayKit-Character-Pack-Adventures-1.0')
SKL = os.path.join(KOK, 'KayKit-Character-Pack-Skeletons-1.0', 'addons', 'kaykit_character_pack_skeletons', 'Characters', 'gltf')
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'BirdieSmash', 'Assets', 'Resources', 'karakter', 'aks')

def adv(ad):
    for k in (os.path.join(ADV, ad), os.path.join(ADV, 'Characters', 'gltf', ad)):
        if os.path.exists(k): return k
    for d, _, fs in os.walk(ADV):
        if ad in fs: return os.path.join(d, ad)
    raise SystemExit('bulunamadı: ' + ad)

# id → (dosya, nesne, tür). Kimlikler Kozmetik (karakter.json "sapka"/"sirt") ile aynı.
KALEM = {
    'yildiz':          (lambda: adv('Mage.glb'), 'Mage_Hat', 0),
    'demir':           (lambda: adv('Knight.glb'), 'Knight_Helmet', 0),
    'ayi':             (lambda: adv('Barbarian.glb'), 'Barbarian_Hat', 0),
    'kemik_buyucu':    (lambda: os.path.join(SKL, 'Skeleton_Mage.glb'), 'Skeleton_Mage_Hat', 0),
    'kapson':          (lambda: os.path.join(SKL, 'Skeleton_Rogue.glb'), 'Skeleton_Rogue_Hood', 0),
    'boynuz':          (lambda: os.path.join(SKL, 'Skeleton_Warrior.glb'), 'Skeleton_Warrior_Helmet', 0),
    'yildiz_pelerin':  (lambda: adv('Mage.glb'), 'Mage_Cape', 1),
    'sovalye_pelerin': (lambda: adv('Knight.glb'), 'Knight_Cape', 1),
    'ayi_pelerin':     (lambda: adv('Barbarian.glb'), 'Barbarian_Cape', 1),
    'gezgin_pelerin':  (lambda: adv('Rogue.glb'), 'Rogue_Cape', 1),
    'golge_pelerin':   (lambda: os.path.join(SKL, 'Skeleton_Rogue.glb'), 'Skeleton_Rogue_Cape', 1),
    'ruh_pelerin':     (lambda: os.path.join(SKL, 'Skeleton_Warrior.glb'), 'Skeleton_Warrior_Cloak', 1),
}

def yukle(yol):
    bpy.ops.wm.read_homefile(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=yol)
    for a in bpy.data.objects:
        if a.type == 'ARMATURE': a.data.pose_position = 'REST'   # dinlenme pozu: kemiğe bağlı parça ve deri ağı aynı yerde
    bpy.context.view_layer.update()

def dunya(o):
    dg = bpy.context.evaluated_depsgraph_get(); e = o.evaluated_get(dg); me = e.to_mesh()
    return e, me

def kemik(ad):
    a = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    return a.matrix_world @ a.data.bones[ad].head_local

def referans():
    """Rogue başından birim baş, Rogue gövdesinden sırt çerçevesi (Blender dünya koordinatı)."""
    yukle(adv('Rogue.glb'))
    bas = bpy.data.objects['Rogue_Head']; e, me = dunya(bas)
    vs = [e.matrix_world @ v.co for v in me.vertices]; tri = []
    me.calc_loop_triangles(); tri = [tuple(t.vertices) for t in me.loop_triangles]
    bvh = BVHTree.FromPolygons(vs, tri)
    # göz: ön yüzde (y < -.3) dokuda koyu hücreler (kesif ölçümü 2026-11-01: z ≈ 1,59, x ≈ ±0,19)
    img = next(n.image for n in bas.data.materials[0].node_tree.nodes if n.type == 'TEX_IMAGE'); W, H = img.size; px = img.pixels[:]
    uv = bas.data.uv_layers.active.data; gz = []
    for p in bas.data.polygons:
        u = sum(uv[l].uv[0] for l in p.loop_indices) / p.loop_total; v = sum(uv[l].uv[1] for l in p.loop_indices) / p.loop_total
        i = ((int(v * H) % H) * W + int(u * W) % W) * 4; w = bas.matrix_world @ p.center
        if w.y < -.3 and sum(px[i:i + 3]) < .45: gz.append(w.z)
    goz_z = sum(gz) / len(gz)
    eks = Vector((0, sum(v.y for v in vs) / len(vs), 0))
    def kesit(z):
        d = []
        for k in range(32):
            a = 2 * math.pi * k / 32; yon = Vector((math.cos(a), math.sin(a), 0)); o = Vector((eks.x, eks.y, z)) + yon * 3
            h = bvh.ray_cast(o, -yon)
            d.append((yon, (h[0] - Vector((eks.x, eks.y, z))).length if h[0] else 0))
        return d
    z0 = goz_z
    for _ in range(6):   # bant = göz + .38 r; r bant düzleminde ölçülür, birkaç adımda oturur
        r = sum(x for _, x in kesit(z0 + .001)) / 32; z0 = goz_z + .38 * r
    d = kesit(z0); r = sum(x for _, x in d) / 32
    xs = [y.x * x for y, x in d]; ys = [y.y * x for y, x in d]
    yan = (max(xs) - min(xs)) / 2 / r; on = -min(ys) / r; arka = max(ys) / r   # ön = -Y
    ust = (max(v.z for v in vs) - z0) / (.95 * r)
    basC = Vector((eks.x, eks.y, z0))
    # sırt: omuz ortası, omuz genişliği, omuz yüksekliğinde gövdenin en arka noktası (+Y)
    ol, orr = kemik('upperarm.l'), kemik('upperarm.r'); om = (ol + orr) / 2; w = (ol - orr).length
    g = bpy.data.objects['Rogue_Body']; e2, me2 = dunya(g)
    arkaY = max((e2.matrix_world @ v.co).y for v in me2.vertices if abs((e2.matrix_world @ v.co).z - om.z) < .12 * w and abs((e2.matrix_world @ v.co).x) < .3 * w)
    print('baş', dict(goz=round(goz_z, 3), z0=round(z0, 3), r=round(r, 3), yan=round(yan, 3), ust=round(ust, 3), on=round(on, 3), arka=round(arka, 3)), 'sırt', dict(om=tuple(round(x, 3) for x in om), w=round(w, 3), arkaY=round(arkaY, 3)))
    return dict(c=basC, r=r, yan=yan, ust=ust, on=on, arka=arka, om=om, w=w, arkaY=arkaY)

def birim(p, R, tur):
    if tur == 0:
        c = R['c'] + Vector((0, -1, 0)) * R['r'] * (R['on'] - R['arka']) * .5   # elipsoit merkezi öne kayar (Aksesuar.Kur ile aynı)
        d = p - c; zs = (R['on'] + R['arka']) * .5
        return (-d.x / (R['r'] * R['yan']), d.z / (R['r'] * R['ust']), -d.y / (R['r'] * zs))
    d = p - Vector((R['om'].x, R['arkaY'], R['om'].z))
    return (-d.x / R['w'], d.z / R['w'], -d.y / R['w'])

def renkler(o):
    img = None
    for m in o.data.materials:
        if m and m.node_tree:
            img = next((n.image for n in m.node_tree.nodes if n.type == 'TEX_IMAGE'), None)
            if img: break
    W, H = img.size; px = img.pixels[:]; uv = o.data.uv_layers.active.data
    def oku(lis):
        u = sum(uv[l].uv[0] for l in lis) / len(lis); v = sum(uv[l].uv[1] for l in lis) / len(lis)
        i = ((min(H - 1, max(0, int(v * H)))) * W + min(W - 1, max(0, int(u * W)))) * 4
        return tuple(px[i:i + 4])
    return oku

def disa(id_, yol, ad, tur, R):
    yukle(yol); o = bpy.data.objects[ad]; e, me = dunya(o)
    me.calc_loop_triangles(); oku = renkler(o)
    pos = []; nor = []; idx = {}
    # köşe başına tek normal yetmez (keskin kenarlar): üçgen köşesi (konum, normal) çiftine göre tekilleştirilir
    gruplar = []   # (renk, [üçgen])
    M = e.matrix_world; N = M.to_3x3().inverted().transposed()
    for t in me.loop_triangles:
        c = oku([me.loops[l].index for l in t.loops])
        rgb = c[:3]   # img.pixels sRGB dokuda ham (sRGB) değer verir; Unity Color da sRGB yazılır
        g = next((G for G in gruplar if sum((a - b) ** 2 for a, b in zip(G[0], rgb)) < .004), None)
        if g is None: g = [rgb, []]; gruplar.append(g)
        uc = []
        for k, l in zip(t.vertices, t.loops):
            p = birim(M @ me.vertices[k].co, R, tur)
            nn = (N @ (me.loops[l].normal if hasattr(me.loops[l], 'normal') else me.vertices[k].normal)).normalized()
            n = (-nn.x, nn.z, -nn.y)
            key = (tuple(round(x, 5) for x in p), tuple(round(x, 2) for x in n))
            if key not in idx: idx[key] = len(pos); pos.append(p); nor.append(n)
            uc.append(idx[key])
        g[1].append(uc)
    assert len(pos) < 65535
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, id_ + '.bytes'), 'wb') as f:
        f.write(b'BSA1'); f.write(struct.pack('<BH', tur, len(pos)))
        for p, n in zip(pos, nor): f.write(struct.pack('<6f', *p, *n))
        f.write(struct.pack('<B', len(gruplar)))
        for rgb, ts in gruplar:
            f.write(bytes(int(round(max(0, min(1, x)) * 255)) for x in (*rgb, 1)))
            f.write(struct.pack('<H', len(ts)))
            for uc in ts: f.write(struct.pack('<3H', *uc))
    ys = [p[1] for p in pos]
    print(id_, ad, 'köşe', len(pos), 'renk', len(gruplar), 'y', round(min(ys), 2), round(max(ys), 2))

if __name__ == '__main__':
    if not KOK: raise SystemExit('BS_KAYKIT klasörü gerekli (dosya başındaki iki git clone)')
    R = referans()
    sec = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else list(KALEM)
    for k in sec:
        y, ad, tur = KALEM[k]; disa(k, y(), ad, tur, R)
