# Gerçekçi model aksesuarları (2026-10-27, yasin: "Gerçekçi de vücut özellikleri değil de sadece aksesuarlar değişmeli
# kafa bandı forma bileklik ayakkabı şort tişört raket"). Beden, yüz ve saç tek (Rocketbox Sports_Male_02, MIT); oyuncunun
# seçtiği kıyafet görünür olsun diye ten ağından bölgeler ayrı derili ağlara alınır:
#  - şort  → rb_sort__sort (oyun: gövde dokusu × şort rengi; doku şortu açık griye boyar, rocketbox_kit SORT_GRI);
#  - ayakkabı → beş model, ayk_<varlik>__ayakkabi (Core/Gear "ayakkabi" yuvasının varlik kimlikleri; oyun tam birini açar,
#    oyuncak ailesindeki sözleşmenin aynısı). İki malzeme yuvası: "ayk_ust" (saya: doku × ayakkabı rengi) ve taban.
#    Klasik tabanı gövde malzemesinde kalır (kauçuk renk dokudan), diğerlerinin tabanı düz renk malzeme (FBX rengi).
# Neden ayrı ağ (doku maskesi + shader değil): Unity Standard bölge maskeli boyamayı bilmez; özel shader iOS build'de ayrıca
# taşınmalı ve değişken ayıklamasına (shader stripping) takılabilir. Ayrı ağ bugünkü forma yolunun aynısı (__forma), çizim
# çağrısı yalnız açık olan parça kadar (+1 şort, ayakkabı tek ağ iki yuva).
# Ayakkabı modelleri ağ olarak (doku varyantı değil): uzak kameradan (oyuncu ~100 px) renk değil biçim okunur. Hepsi
# klasik ayakkabının köşelerinden türer, ağırlıkları aynı: ayak bileği dikişi (çorapla ortak kenar) yerinde kalır, delik
# açılmaz; taban zemine eskisi gibi basar (yere iniş, bilek-taban mesafesi ve klipler değişmez).
#   klasik  : kaynak (beyaz saya, kauçuk taban)
#   hafif   : saya %7 dar (kenar halkası sabit), beyaz ince taban (bant ×0,75)
#   yastik  : taban bandı iki kat yüksek, %12 geniş ve öne/arkaya %5 uzun (kalın yastık), beyaz taban
#   bilekli : ayak bileği üstüne düz kenarlı yakalık (dikişin tepesinden 4,5 cm) (alt halka ayakta, üst halka baldır %60 / ayak %40), kauçuk taban
#   karbon  : saya %5 dar, kömür rengi taban (bant ×1,25)
# Kafa bandı ve bileklik (aday.py'nin simit/silindiri) burada baş ve ön kol derisine oturtulur (_bant, _bilek).
# Dinlenme pozu, model ölçeği (FBX ile aynı birim: boy ~1,11, +y ileri, z yukarı). Ölçüler model birimi; oyunda × 1,75.
import bpy, bmesh, math
from mathutils import Vector

# Taban bandının yüksekliği (zk katı): hafif ince, yastık kalın (yanlardan görünen beyaz yastık), karbon biraz kalın.
TABAN_K = {'klasik': 1., 'hafif': .75, 'yastik': 2.0, 'bilekli': 1., 'karbon': 1.25}
ALT_RENK = {'hafif': (.93, .93, .91), 'yastik': (.95, .95, .94), 'karbon': (.10, .11, .13)}
VARLIK = ('klasik', 'hafif', 'yastik', 'bilekli', 'karbon')


def _ayir(o, yuzler, ad, tag):
    """o'nun verilen yüzlerini yeni nesneye ayırır (ağırlıklar, UV ve armature değiştiricisi gelir)"""
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT'); bpy.ops.object.mode_set(mode='OBJECT')
    for i in yuzler: o.data.polygons[i].select = True
    onc = set(bpy.context.scene.objects)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.separate(type='SELECTED'); bpy.ops.object.mode_set(mode='OBJECT')
    y = next(iter(set(bpy.context.scene.objects) - onc)); y.name = ad + '__' + tag; y.data.name = y.name; y['tag'] = tag
    return y


def _kopya(o, ad):
    c = o.copy(); c.data = o.data.copy(); c.name = ad + '__ayakkabi'; c.data.name = c.name; c['tag'] = 'ayakkabi'
    for col in o.users_collection: col.objects.link(c)
    return c


def _kenar(me):
    """sınır kenarlarındaki köşeler (çorapla ortak dikiş) ve köşe başına sınıra kenar adımı (en çok 6)"""
    bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
    sinir = {v.index for v in bm.verts if any(e.is_boundary for e in v.link_edges)}
    adim = {i: 0 for i in sinir}; on = list(sinir)
    for k in range(1, 7):
        yeni = []
        for i in on:
            for e in bm.verts[i].link_edges:
                j = e.other_vert(bm.verts[i]).index
                if j not in adim: adim[j] = k; yeni.append(j)
        on = yeni
    bm.free(); return sinir, adim


def _eksen(P, taraf):
    """bir ayağın köşeleri: (x merkezi, y merkezi) — ayak ekseni +y'ye yakın, yanal eksen x"""
    q = [p for p in P if (p.x > 0) == taraf]
    return sum(p.x for p in q) / len(q), sum(p.y for p in q) / len(q)


def _tabanla(o, zk):
    """taban yuvası: ağ z = zk düzleminde kesilir (bisect: UV ve ağırlıklar kesişimde ara-değerlenir), altı taban (yuva 1),
    üstü saya (0). Yüz başına eşik testi tabanı testere dişi gibi çiziyordu (yüzler düzlemi çaprazlıyor); doku da tabanı
    aynı z eşiğiyle boyar (rocketbox_kit "kenar"), düzlem kesiti dokuyla aynı çizgiye düşer."""
    me = o.data; mw = o.matrix_world; inv = mw.inverted()
    bm = bmesh.new(); bm.from_mesh(me)
    co = inv @ Vector((0, 0, zk)); no = (inv.to_3x3() @ Vector((0, 0, 1))).normalized()
    bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-6, plane_co=co, plane_no=no)
    for f in bm.faces: f.material_index = 1 if (mw @ f.calc_center_median()).z < zk else 0
    bm.to_mesh(me); bm.free(); me.update()


def _malzeme(ad, renk):
    m = bpy.data.materials.get(ad) or bpy.data.materials.new(ad); m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'); b.inputs['Base Color'].default_value = (*renk, 1)
    b.inputs['Roughness'].default_value = .6; m.diffuse_color = (*renk, 1); return m


def _darlas(o, oran, zk, sadece_ust=True):
    """sayayı ayak eksenine doğru yanal daralt; sınır halkası (çorap dikişi) sabit, 4 adımda tam orana çıkar"""
    me = o.data; mw = o.matrix_world; inv = mw.inverted(); _, adim = _kenar(me)
    P = [mw @ v.co for v in me.vertices]; ek = {t: _eksen(P, t) for t in (True, False)}
    for v, p in zip(me.vertices, P):
        if sadece_ust and p.z < zk: continue
        w = min(1., adim.get(v.index, 6) / 4.)
        cx, _ = ek[p.x > 0]; p2 = p.copy(); p2.x = cx + (p.x - cx) * (1 - (1 - oran) * w); v.co = inv @ p2
    me.update()


def _yastik(o, zk):
    me = o.data; mw = o.matrix_world; inv = mw.inverted(); P = [mw @ v.co for v in me.vertices]; ek = {t: _eksen(P, t) for t in (True, False)}
    for v, p in zip(me.vertices, P):
        if p.z > zk * 1.9: continue
        w = 1. if p.z < zk else 1 - (p.z - zk) / (zk * .9)
        cx, cy = ek[p.x > 0]; p2 = p.copy(); p2.x = cx + (p.x - cx) * (1 + .12 * w); p2.y = cy + (p.y - cy) * (1 + .05 * w); v.co = inv @ p2
    me.update()


def _yakalik(o, ao, ten, boy, pay):
    """sınır halkasından (ayak bileği dikişi) yukarı yakalık tüpü. Alt halka = sınır köşeleri + pay (ağırlıkları aynen),
    üst halka baldır eksenine dik TEK düzlemde (dikişin en yüksek noktasının boy kadar üstü; ilk sürüm dikişi izliyordu,
    topuk yüksek olduğu için üst kenar testere dişiydi), yarıçapı o yükseklikte bacağa (ten ağı) atılan ışından + pay.
    Üst halka ağırlığı baldır .6 / ayak .4. UV: alttaki köşenin UV'si (saya dokusunun yaka bölgesi, açık gri)."""
    from mathutils.bvhtree import BVHTree
    me = o.data; mw = o.matrix_world; inv = mw.inverted()
    tw = ten.matrix_world; T = BVHTree.FromPolygons([tw @ v.co for v in ten.data.vertices], [list(f.vertices) for f in ten.data.polygons])
    bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table(); bm.edges.ensure_lookup_table()
    uvl = bm.loops.layers.uv.active; dl = bm.verts.layers.deform.active
    gn = {g.name: g.index for g in o.vertex_groups}
    for k in ('Bip01 L Calf', 'Bip01 R Calf', 'Bip01 L Foot', 'Bip01 R Foot'):
        if k not in gn: gn[k] = o.vertex_groups.new(name=k).index
    sinir_e = [e for e in bm.edges if e.is_boundary]
    vuv = {}
    for e in sinir_e:
        for v in e.verts:
            if v.index not in vuv and v.link_loops: vuv[v.index] = v.link_loops[0][uvl].uv.copy()
    lx = ao.data.bones['Bip01 L Foot'].head_local.x
    def taraf(p): return 'L' if lx * p.x > 0 else 'R'
    eks = {}
    for K in 'LR':
        calf = ao.matrix_world @ ao.data.bones[f'Bip01 {K} Calf'].head_local; ank = ao.matrix_world @ ao.data.bones[f'Bip01 {K} Foot'].head_local
        ax = (calf - ank).normalized()
        hs = [(mw @ v.co - ank).dot(ax) for e in sinir_e for v in e.verts if taraf(mw @ v.co) == K]
        eks[K] = (ank, ax, max(hs) + boy if hs else boy)
    yeni = {}
    for e in sinir_e:
        for v in e.verts:
            if v in yeni: continue
            p = mw @ v.co; K = taraf(p); ank, ax, hu = eks[K]
            c0 = ank + ax * (p - ank).dot(ax); r = p - c0; r = (r - ax * r.dot(ax)).normalized()
            cu = ank + ax * hu; hit = T.ray_cast(cu + r * .12, -r, .12)
            ust = (hit[0] if hit[0] is not None else cu + r * .03) + r * pay
            alt = p + r * pay
            va = bm.verts.new(inv @ alt); vu = bm.verts.new(inv @ ust)
            for g, x in v[dl].items(): va[dl][g] = x
            for g, x in ((f'Bip01 {K} Calf', .6), (f'Bip01 {K} Foot', .4)): vu[dl][gn[g]] = x
            yeni[v] = (va, vu)
    for e in sinir_e:
        a, b = e.verts; (a0, a1), (b0, b1) = yeni[a], yeni[b]
        for q, kay in (((a0, b0, b1, a1), None), ((a, b, b0, a0), None)):   # tüp + sayaya oturduğu dar dikiş şeridi
            try: f = bm.faces.new(q)
            except ValueError: continue
            f.material_index = 0; f.smooth = True
            for l in f.loops: l[uvl].uv = vuv.get((a if l.vert in (a, a0, a1) else b).index, l[uvl].uv)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces: f.smooth = True
    bm.to_mesh(me); bm.free(); me.update()


def _bant(bant, ten, ao):
    """kafa bandı başa oturur (2026-10-27): eski bant 0,096 yarıçaplı simit idi; Unity vitrininde başın çevresinde hale
    gibi duruyordu (yanlarda ve arkada 1–2 cm boşluk, alında deriye gömük). Şimdi baş ağına (ten, baş malzemesi) dışarıdan
    içeri ışın atılır: bandın iç yüzü derinin 0,5 mm içinde, dış yüzü 2,5 mm (oyunda ~4 mm) dışında; alında kaşın ~3 cm
    üstünde, arkada 1,3 cm daha alçak (bant ensede aşağı oturur), 2,5 cm (oyunda) yüksek. %100 baş kemiği; malzeme ve
    nesne aynı (Unity "bant" etiketi)."""
    from mathutils.bvhtree import BVHTree
    mw = ten.matrix_world; me = ten.data
    V = [mw @ v.co for v in me.vertices]; F = [list(f.vertices) for f in me.polygons if f.material_index == 1]
    T = BVHTree.FromPolygons(V, F)
    hz = max(v.z for i, v in enumerate(V) if me.vertices[i].groups and any(g.weight > .5 and ten.vertex_groups[g.group].name == 'Bip01 Head' for g in me.vertices[i].groups))
    zon, zarka, yuk, kal, ic, N = hz - .053, hz - .066, .025 / 1.75, .0025, .0005, 48
    cy = sum(v.y for v in V if v.z > zarka - .01 and v.z < zon + .01 and abs(v.x) < .06) / max(1, sum(1 for v in V if v.z > zarka - .01 and v.z < zon + .01 and abs(v.x) < .06))
    halka = {k: [] for k in ('ia', 'da', 'du', 'iu')}
    for i in range(N):
        a = 2 * math.pi * i / N; d = Vector((math.sin(a), math.cos(a), 0))   # a=0: ön (+y)
        z = zarka + (zon - zarka) * (d.y + 1) / 2
        for kat, dz in (('a', -yuk / 2), ('u', yuk / 2)):
            o = Vector((0, cy, z + dz)); hit = T.ray_cast(o + d * .3, -d, .3)
            p = hit[0] if hit[0] is not None else o + d * .055
            halka['i' + kat].append(p - d * ic); halka['d' + kat].append(p + d * kal)
    sira = [halka['ia'], halka['da'], halka['du'], halka['iu']]
    vs = [p for r in sira for p in r]; fs = []
    for r in range(4):
        r2 = (r + 1) % 4
        for i in range(N):
            j = (i + 1) % N; fs.append((r * N + i, r * N + j, r2 * N + j, r2 * N + i))
    inv = bant.matrix_world.inverted(); yeni = bpy.data.meshes.new(bant.data.name)
    yeni.from_pydata([inv @ p for p in vs], [], fs); yeni.update()
    for m in bant.data.materials: yeni.materials.append(m)
    for f in yeni.polygons: f.use_smooth = True
    eski = bant.data; bant.data = yeni; bpy.data.meshes.remove(eski)
    g = bant.vertex_groups.get('Bip01 Head') or bant.vertex_groups.new(name='Bip01 Head'); g.add(list(range(len(vs))), 1, 'REPLACE')
    bm = bmesh.new(); bm.from_mesh(yeni); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(yeni); bm.free()


def _bilek(bil, ten, ao):
    """bileklik ön kola oturur (2026-10-27): eski bileklik 4,3 cm yarıçaplı düz silindirdi, bileğin çevresinde 1 cm'ye
    yakın boşlukla bol duruyordu. Şimdi ön kol ekseni boyunca dört sırada deriye dışarıdan ışın atılır (bilek incelir, sıra
    sıra izler): iç yüz derinin 0,5 mm içinde, dış yüz 3 mm (oyunda ~5 mm) dışında, bilek kemiğinin 1 cm gerisinden 6,5 cm
    (oyunda) yukarı. %100 ön kol kemiği (bileklik bilekle dönmez: gerçek bileklik de ön kolda durur)."""
    from mathutils.bvhtree import BVHTree
    mw = ten.matrix_world; me = ten.data; gi = ten.vertex_groups['Bip01 R Forearm'].index
    hi = ten.vertex_groups['Bip01 R Hand'].index
    V = [mw @ v.co for v in me.vertices]
    kol = {v.index for v in me.vertices if any(g.group in (gi, hi) and g.weight > .2 for g in v.groups)}
    F = [list(f.vertices) for f in me.polygons if all(i in kol for i in f.vertices)]
    T = BVHTree.FromPolygons(V, F)
    b = ao.data.bones; A = ao.matrix_world @ b['Bip01 R Forearm'].head_local; E = ao.matrix_world @ b['Bip01 R Hand'].head_local
    ax = (E - A).normalized(); u = ax.orthogonal().normalized(); w = ax.cross(u)
    boy, kal, ic, N, S = .065 / 1.75, .003, .0005, 32, 4
    sira = []
    for k in range(S):
        c = E - ax * (.01 / 1.75 + boy * k / (S - 1)); r_ = []
        for i in range(N):
            a = 2 * math.pi * i / N; d = u * math.cos(a) + w * math.sin(a); hit = T.ray_cast(c + d * .12, -d, .12)
            r_.append(hit[0] if hit[0] is not None else c + d * .025)
        sira.append(r_)
    ic_ = [[p - (p - (E - ax * (E - p).dot(ax))).normalized() * ic for p in r] for r in sira]
    dis = [[p + (p - (E - ax * (E - p).dot(ax))).normalized() * kal for p in r] for r in sira]
    halkalar = dis + ic_[::-1]   # dış yüz bilekten yukarı, iç yüz yukarıdan bileğe: kapalı kesit
    vs = [p for r in halkalar for p in r]; fs = []; R = len(halkalar)
    for r in range(R):
        r2 = (r + 1) % R
        for i in range(N):
            j = (i + 1) % N; fs.append((r * N + i, r * N + j, r2 * N + j, r2 * N + i))
    inv = bil.matrix_world.inverted(); yeni = bpy.data.meshes.new(bil.data.name)
    yeni.from_pydata([inv @ p for p in vs], [], fs); yeni.update()
    for m in bil.data.materials: yeni.materials.append(m)
    for f in yeni.polygons: f.use_smooth = True
    eski = bil.data; bil.data = yeni; bpy.data.meshes.remove(eski)
    for g in list(bil.vertex_groups): bil.vertex_groups.remove(g)
    bil.vertex_groups.new(name='Bip01 R Forearm').add(list(range(len(vs))), 1, 'REPLACE')
    bm = bmesh.new(); bm.from_mesh(yeni); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(yeni); bm.free()


def uygula(ao, ten, olc):
    """ten: m301 gövde ağı (__ten). Döner: yeni nesneler (rb_sort__sort + beş ayk_<varlik>__ayakkabi), ao'ya bağlı."""
    import rocketbox_kit as K
    sinif = K._sinif(ten)
    sort = _ayir(ten, [i for i, c in sinif.items() if c == K.SORT], 'rb_sort', 'sort')
    m = bpy.data.materials.get('rb_sort') or ten.data.materials[0].copy(); m.name = 'rb_sort'
    for k in range(len(sort.data.materials)): sort.data.materials[k] = m
    sinif = K._sinif(ten)   # ayırmadan sonra yüz numaraları kaydı: yeniden sınıfla
    ayk = _ayir(ten, [i for i, c in sinif.items() if c == K.AYAKKABI], 'ayk_klasik', 'ayakkabi')
    govdeM = ten.data.materials[0]; ust = bpy.data.materials.get('ayk_ust') or govdeM.copy(); ust.name = 'ayk_ust'
    ayk.data.materials.clear(); ayk.data.materials.append(ust); ayk.data.materials.append(govdeM)
    zk = (K.TABAN_PLAKA + .018) * olc   # taban plakası + kenar bandı (rocketbox_kit dokusunda "kenar"), model birimi
    out = [sort]
    for v in VARLIK:
        c = ayk if v == 'klasik' else _kopya(ayk, 'ayk_' + v); out.append(c)
        if v in ALT_RENK: c.data.materials[1] = _malzeme('ayk_alt_' + v, ALT_RENK[v])
        zt = zk * TABAN_K[v]
        if v == 'hafif': _darlas(c, .93, zt)
        elif v == 'karbon': _darlas(c, .95, zt)
        elif v == 'yastik': _yastik(c, zt)
        elif v == 'bilekli': _yakalik(c, ao, ten, .045 / 1.75, .004 / 1.75)   # dikişin tepesinden 4,5 cm (oyunda), 4 mm pay   # oyunda 8 cm boy, 5 mm pay
        _tabanla(c, zt)
    bant = next((o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name.startswith('rb_bant')), None)
    if bant is not None: _bant(bant, ten, ao)
    bil = next((o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name.startswith('rb_bileklik')), None)
    if bil is not None: _bilek(bil, ten, ao)
    for o in out:
        o.parent = ao; o.matrix_parent_inverse = ao.matrix_world.inverted()
        if not any(md.type == 'ARMATURE' for md in o.modifiers): md = o.modifiers.new('a', 'ARMATURE'); md.object = ao
    return out
