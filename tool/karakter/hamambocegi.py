# Hamamböceği (yasin 2026-11-20: "bu tipte olmalı" → "renklendirelim, farklı aksesuarlar ekleyelim, eklemlerini bizim
# uygulamaya uygun hale getirelim, elcik ekleyebiliriz bize özel"). Beşinci model adayı; diğer modellerin dosyalarına dokunmaz.
# Özgün tasarım: iki ayak üstünde çizgi film hamamböceği, iri üzgün gözler, halkalı karın, sırtta kanat, yanda kısa orta bacaklar.
#
# Eklem: Tüycük gibi Oyuncak atlet iskeleti (53 UE adlı kemik) ve onun pişmiş 33 klibi. Ön bacak = kol (upperarm/lowerarm/hand),
# arka bacak = bacak (thigh/calf/foot), orta bacak kemiksiz: gövdeyle birlikte bükülür. Elcik: avuç + 4 parmak + başparmak, her
# parmak kendi üç boğumuna derili; kliplerdeki tutuş parmakları sapa sardıkça elcik de sarar (raket HEP sağ elde).
#
# shortcut: iskelet ve klipler tuycuk.fbx'ten alınır (oyuncak.py MakeHuman verisi ister, PC'de yok). İç içe geçme çözücüsü ve
# tutuş Tüycük'ün yumurta gövdesiyle ölçülmüş hâliyle kalır; bu gövdede el/raket girmesi ölçülmedi. Girme görülürse tuycuk.py
# gibi oyuncak.Oyuncak alt sınıfına çevir (bulutta, BS_MODEL ile unity_fbx.py).
#
# Renk sözleşmesi (unity_fbx "<ad>__<etiket>", PlayerView): hb_govde__govde = doğal renkler, tek malzeme oy_body + renk atlası
# (hamambocegi_govde.png; Tüycük atlasının ayakkabı hücreleri korunur, boş hücrelere bu modelin renkleri); hb_forma__forma =
# göğüs + elcikler, oyun takım rengine boyar; hb_bant__bant, hb_bilek__bilek kuşanılınca görünür. Ayakkabılar (5 model) Tüycük'ten.
# hb_diva__diva (kürk, küpe, ruj, kirpik) yalnız Blender ön izlemesinde: oyunda nasıl seçileceği kararlaştırılmadı, FBX'e girmez.
#
# Blender içinde (figür +y'ye bakar, sağ el +x, model birimi; oyunda ×1,75):
#   p = r"<repo>/tool/karakter/hamambocegi.py"; exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p})
#   bpy.app.driver_namespace["hb_disa_aktar"]()   → Resources/karakter/hamambocegi.fbx, _clips.json, _govde.png
import bpy, bmesh, math, os, json
from mathutils import Vector

KOK = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(KOK, 'unity', 'BirdieSmash', 'Assets', 'Resources', 'karakter')
# Tip (yasin 2026-11-20: "bir erkek hamam böceği ekle renklendir ve oyuna ekle"): sade = hamambocegi.fbx; erkek = ayrı model
# hamambocegi_erkek.fbx (koyu maun kabuk, yeşil göz, geniş göğüs, düz kararlı kaş, bıyık, papyon). Sahnede tek tip durur.
# exec sözlüğüne "HB_TIP": "erkek" ya da ortamda BS_HB_TIP=erkek.
TIP = globals().get('HB_TIP') or os.environ.get('BS_HB_TIP', 'sade')
ERKEK = TIP == 'erkek'
AD, KOL, VERICI = 'hamambocegi' + ('_erkek' if ERKEK else ''), 'Hamambocegi', 'tuycuk'
sc = bpy.context.scene

# ---------- temizlik + verici iskelet ----------
for n in (KOL, 'Iskelet'):
    c = bpy.data.collections.get(n)
    if c:
        for o in list(c.objects): bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(c)
bpy.data.orphans_purge(do_recursive=True)   # eski malzeme/eylem adları .001 almasın
col = bpy.data.collections.new(KOL); sc.collection.children.link(col)
once = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=os.path.join(RES, VERICI + '.fbx'), automatic_bone_orientation=False,
                         ignore_leaf_bones=False, anim_offset=0.0)   # anim_offset 0: kare numaraları _clips.json ile aynı kalsın
for o in [o for o in bpy.data.objects if o not in once]:
    for uc in list(o.users_collection): uc.objects.unlink(o)
    if o.name.startswith('tc_'): bpy.data.objects.remove(o)   # Tüycük'ün görünen ağı
    else: col.objects.link(o)
kok = col.objects[VERICI]; kok.name = AD
ao = next(o for o in col.objects if o.type == 'ARMATURE'); B = ao.data.bones
sc.render.fps = 30; sc.frame_start = 1; sc.frame_end = int(ao.animation_data.action.frame_range[1])
H = lambda n: B[n].head_local.copy()
T_ = lambda n: B[n].tail_local.copy()

# ---------- renk atlası ----------
RENK = dict(govde='#b5521a', karin='#e39a4f', koyu='#5a2410', kanat='#8a3413', goz='#fbfbfb', iris='#c8862a',
            siyah='#1a0f0a', kurk='#fff6ee', altin='#e8b83a', ruj='#c2183a')
if ERKEK: RENK.update(govde='#6b2f14', karin='#c98a4a', koyu='#2e140a', kanat='#4a1d0c', iris='#3f8f4f')
NA = 8
body = next(m for m in bpy.data.materials if m.name == 'oy_body')
dolu = set()   # ayakkabıların kullandığı hücreler korunur
for o in col.objects:
    if o.type != 'MESH' or not o.name.startswith('ayk_'): continue
    uv = o.data.uv_layers.active.data
    for p in o.data.polygons:
        if o.data.materials[p.material_index] is not body: continue
        for li in p.loop_indices:
            u, v = uv[li].uv; dolu.add(min(NA - 1, int(v * NA)) * NA + min(NA - 1, int(u * NA)))
img = bpy.data.images.load(os.path.join(RES, VERICI + '_govde.png')); img.name = AD + '_govde'
PX = img.size[0]; cel = PX // NA; pix = list(img.pixels)
bos = [i for i in range(NA * NA) if i not in dolu]
assert len(bos) >= len(RENK), 'atlasta boş hücre yetmiyor'
HUCRE = {}
for k, h in RENK.items():
    i = bos.pop(0); HUCRE[k] = ((i % NA + .5) / NA, (i // NA + .5) / NA)
    e = [int(h[j:j + 2], 16) / 255 for j in (1, 3, 5)]
    for yy in range((i // NA) * cel, (i // NA + 1) * cel):
        for xx in range((i % NA) * cel, (i % NA + 1) * cel): q = (yy * PX + xx) * 4; pix[q:q + 4] = [*e, 1.]
img.pixels = pix; img.filepath_raw = os.path.join(RES, AD + '_govde.png'); img.file_format = 'PNG'; img.save(); img.pack()
next(n for n in body.node_tree.nodes if n.type == 'TEX_IMAGE').image = img
forma_m = bpy.data.materials.new('forma')
next(n for n in forma_m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Base Color'].default_value = (.05, .25, .8, 1)
bant_m = bpy.data.materials.new('bant')
next(n for n in bant_m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Base Color'].default_value = (.9, .9, .9, 1)


# ---------- ağ yardımcıları ----------
def ss(a, b, x):
    t = max(0., min(1., (x - a) / (b - a))); return t * t * (3 - 2 * t)


def elips(c, r, nu=16, nv=10, eks=None):
    X, Y, Z = eks or (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    c = Vector(c); V = [c - Z * r[2]]; F = []
    for i in range(1, nv):
        t = math.pi * i / nv
        for j in range(nu):
            a = 2 * math.pi * j / nu
            V.append(c + X * (r[0] * math.sin(t) * math.cos(a)) + Y * (r[1] * math.sin(t) * math.sin(a)) - Z * (r[2] * math.cos(t)))
    V.append(c + Z * r[2]); ust = len(V) - 1
    for j in range(nu): F.append([0, 1 + (j + 1) % nu, 1 + j])
    for i in range(nv - 2):
        for j in range(nu):
            a = 1 + i * nu + j; b = 1 + i * nu + (j + 1) % nu; F.append([a, b, b + nu, a + nu])
    son = 1 + (nv - 2) * nu
    for j in range(nu): F.append([son + j, son + (j + 1) % nu, ust])
    return V, F


def boru(P, R, nt=8):
    """yol boyunca boru, uçlar sivri kapak. iz: köşe başına yol indisi (ağırlık eşlemesi). Paralel taşıma: eklemde burulma yok."""
    P = [Vector(p) for p in P]
    T = [(P[min(len(P) - 1, i + 1)] - P[max(0, i - 1)]).normalized() for i in range(len(P))]
    u = T[0].cross(Vector((0, 0, 1)) if abs(T[0].z) < .9 else Vector((1, 0, 0))).normalized()
    V = []; F = []; iz = []
    for i in range(len(P)):
        u = (u - T[i] * u.dot(T[i])).normalized(); v = T[i].cross(u)
        for j in range(nt):
            a = 2 * math.pi * j / nt; V.append(P[i] + (u * math.cos(a) + v * math.sin(a)) * R[i]); iz.append(i)
    for i in range(len(P) - 1):
        for j in range(nt):
            a = i * nt + j; b = i * nt + (j + 1) % nt; F.append([a, b, b + nt, a + nt])
    V.append(P[0] - T[0] * R[0] * .7); iz.append(0); k = len(V) - 1
    for j in range(nt): F.append([(j + 1) % nt, j, k])
    V.append(P[-1] + T[-1] * R[-1] * .7); iz.append(len(P) - 1); k = len(V) - 1; s0 = (len(P) - 1) * nt
    for j in range(nt): F.append([s0 + j, s0 + (j + 1) % nt, k])
    return V, F, iz


def zincir(noktalar, kemikler, harman=.02, adim=.02):
    """kemik zinciri boyunca örnek noktalar, yay kesri (0..1) ve ağırlık: eklemde ±harman smoothstep"""
    P = [noktalar[0]]; S = [0.]
    for a, b in zip(noktalar, noktalar[1:]):
        n = max(2, int((b - a).length / adim))
        for i in range(1, n + 1): P.append(a.lerp(b, i / n)); S.append(S[-1] + (b - a).length / n)
    L = [0.]
    for a, b in zip(noktalar, noktalar[1:]): L.append(L[-1] + (b - a).length)
    W = []
    for s in S:
        w = {kemikler[0]: 1.}
        for j in range(1, len(kemikler)):
            u = ss(L[j] - harman, L[j] + harman, s)
            if u <= 0: break
            w = {k: x * (1 - u) for k, x in w.items()}; w[kemikler[j]] = w.get(kemikler[j], 0) + u
        W.append(w)
    return P, [s / S[-1] for s in S], W


def halka(c, rx, ry, a, b, nf=32, nk=6):
    """z ekseni çevresinde yatay elips halka; kesit a (dışa) × b (yukarı)"""
    c = Vector(c); V = []; F = []
    for i in range(nf):
        f = 2 * math.pi * i / nf; m = c + Vector((rx * math.sin(f), ry * math.cos(f), 0))
        n = Vector((math.sin(f) / rx, math.cos(f) / ry, 0)).normalized()
        for j in range(nk):
            p = 2 * math.pi * j / nk; V.append(m + n * (a * math.cos(p)) + Vector((0, 0, b * math.sin(p))))
    for i in range(nf):
        for j in range(nk):
            F.append([i * nk + j, i * nk + (j + 1) % nk, ((i + 1) % nf) * nk + (j + 1) % nk, ((i + 1) % nf) * nk + j])
    return V, F


# gövde ağırlığı yükseklikten (Tüycük ile aynı düğümler): gövdeye yapışan her şey aynı işlevden alır, kayma olmaz
DUGUM = (('pelvis', .37), ('spine_01', .44), ('spine_02', .50), ('spine_03', .62))


def W_z(p):
    if p.z <= DUGUM[0][1]: return {'pelvis': 1.}
    if p.z >= DUGUM[-1][1]:
        u = .5 * ss(.68, .78, p.z); return {'spine_03': 1 - u, 'neck_01': u} if u > 0 else {'spine_03': 1.}
    for (a, za), (b, zb) in zip(DUGUM, DUGUM[1:]):
        if za <= p.z <= zb:
            u = ss(za, zb, p.z); return {a: 1 - u, b: u}


class Parca:
    """birikimli ağ: köşe, yüz, ağırlık, yüz başına atlas hücresi; sonunda tek derili nesne"""
    def __init__(s): s.V = []; s.F = []; s.W = []; s.UV = []

    def ekle(s, V, F, W, renk=None):
        b = len(s.V); s.V += V; s.F += [[b + i for i in f] for f in F]
        s.W += W if isinstance(W, list) else [W(p) if callable(W) else W for p in V]
        s.UV += [HUCRE.get(renk, (0, 0))] * len(F)

    def nesne(s, ad, mat, tag):
        me = bpy.data.meshes.new(ad); me.from_pydata([tuple(v) for v in s.V], [], s.F)
        ul = me.uv_layers.new(name='UVMap')
        for p, c in zip(me.polygons, s.UV):
            p.use_smooth = True
            for li in p.loop_indices: ul.data[li].uv = c
        bm = bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(me); bm.free()
        o = bpy.data.objects.new(ad + '__' + tag, me); col.objects.link(o); me.materials.append(mat); o['tag'] = tag
        gr = {}
        for i, w in enumerate(s.W):
            for k, x in w.items():
                if x < 1e-4: continue
                if k not in gr: gr[k] = o.vertex_groups.new(name=k)
                gr[k].add([i], x, 'REPLACE')
        o.parent = ao; o.modifiers.new('isk', 'ARMATURE').object = ao
        return o


gov, frm, diva = Parca(), Parca(), Parca()
BAS = {'head': 1.}

# ---------- gövde ----------
# karın: göğüsten arkaya-aşağı sarkan altı halka (bölütlü görünüm); ucu .19: hamlede kalça inince yere değmesin
UST, ALT = Vector((0, -.01, .585)), Vector((0, -.115, .25))
for i, rx in enumerate((.085, .125, .150, .160, .155, .140)):
    gov.ekle(*elips(ALT.lerp(UST, i / 5), (rx, rx * .78, .060), 18, 8), W_z, 'karin')
frm.ekle(*elips((0, 0, .635), (.168, .120, .092) if ERKEK else (.150, .112, .085), 18, 10), W_z)   # göğüs = forma
gov.ekle(*elips((0, -.075, .705), (.165, .060, .115), 16, 8), W_z, 'koyu')          # boyun kalkanı
eg = Vector((0, .26, .97)).normalized()                                             # kanat ekseni: karın gibi öne yatık
for s in (1, -1):
    gov.ekle(*elips((.058 * s, -.165, .425), (.100, .034, .235), 14, 10,
                    (Vector((1, 0, 0)), eg.cross(Vector((1, 0, 0))), eg)), W_z, 'kanat')

# ---------- baş ve yüz (hepsi baş kemiğinde) ----------
gov.ekle(*elips((0, .02, .86), (.165, .145, .135), 20, 12), BAS, 'govde')
gov.ekle(*elips((0, .135, .80), (.082, .055, .052), 14, 8), BAS, 'karin')           # burun; bıyık ve ağız bu yüzeye oturur
V, F, _ = boru([(-.036, .184, .781), (-.018, .190, .789), (0, .193, .792), (.018, .190, .789), (.036, .184, .781)],
               [.004, .006, .006, .006, .004], 6)
gov.ekle(V, F, BAS, 'siyah')                                                        # ağız: uçları aşağı
for s in (1, -1):
    goz = Vector((.078 * s, .125, .895)); bak = Vector((-.10 * s, 1, -.14)).normalized()
    gov.ekle(*elips(goz, (.066,) * 3, 16, 10), BAS, 'goz')
    gov.ekle(*elips(goz + bak * .037, (.040,) * 3, 12, 8), BAS, 'iris')
    gov.ekle(*elips(goz + bak * .058, (.023,) * 3, 10, 6), BAS, 'siyah')
    gov.ekle(*elips(goz + (bak + Vector((.30, 0, .34))).normalized() * .0745, (.009,) * 3, 8, 4), BAS, 'goz')   # ışık noktası
    # üst kapak: sade'de dış ucu düşük (üzgün); erkekte düz ve yüksek (göz açık), kaş kalın ve iç ucu aşağıda (kararlı)
    kap = goz + (Vector((0, -.016, .042)) if ERKEK else Vector((.016 * s, -.014, .030)))
    gov.ekle(*elips(kap, (.071,) * 3, 16, 10), BAS, 'govde')
    kd = ((-.40 * s, .55, .58), (.05 * s, .50, .80), (.55 * s, .40, .74)) if ERKEK else ((-.40 * s, .50, .77), (.05 * s, .50, .86), (.55 * s, .40, .55))
    kas = [kap + Vector(d).normalized() * .077 for d in kd]
    kas.insert(1, (kas[0] + kas[1]) / 2 + Vector((0, .004, .004))); kas.insert(3, (kas[2] + kas[3]) / 2 + Vector((0, .004, .004)))
    V, F, _ = boru(kas, [.008, .012, .013, .012, .007] if ERKEK else [.005, .008, .009, .008, .005], 6); gov.ekle(V, F, BAS, 'koyu')
    if ERKEK:   # bıyık: burnun üstünden yana, ucu yukarı kıvrık
        V, F, _ = boru([(.004 * s, .191, .808), (.040 * s, .184, .802), (.075 * s, .166, .808), (.100 * s, .150, .832)],
                       [.009, .011, .008, .002], 6); gov.ekle(V, F, BAS, 'siyah')
    ant = [Vector(p) for p in ((.05 * s, .03, .985), (.062 * s, .045, 1.06), (.085 * s, .045, 1.13), (.12 * s, .02, 1.20),
                               (.17 * s, -.03, 1.26), (.23 * s, -.10, 1.30))]      # anten: arkaya kıvrık (topa bakışı örtmesin)
    V, F, _ = boru(ant, [.010, .009, .008, .007, .005, .003], 6); gov.ekle(V, F, BAS, 'koyu')

    # ---------- uzuvlar ----------
    k = 'r' if s > 0 else 'l'
    P, t, W = zincir([H(f'upperarm_{k}'), H(f'lowerarm_{k}'), H(f'hand_{k}')], [f'upperarm_{k}', f'lowerarm_{k}', f'hand_{k}'])
    V, F, iz = boru(P, [.036 - .012 * x + .006 * math.sin(math.pi * min(1, 2 * x)) for x in t], 10)
    gov.ekle(V, F, [W[i] for i in iz], 'govde')
    P, t, W = zincir([H(f'thigh_{k}'), H(f'calf_{k}'), H(f'foot_{k}')], [f'thigh_{k}', f'calf_{k}', f'foot_{k}'], .025)
    V, F, iz = boru(P, [.044 - .018 * x + .008 * math.sin(math.pi * min(1, 2 * x)) for x in t], 10)
    gov.ekle(V, F, [W[i] for i in iz], 'govde')
    diz, bilek = H(f'calf_{k}'), H(f'foot_{k}')                                     # baldır dikenleri: arkaya ve dışa
    for f_ in (.30, .50, .70):
        p = diz.lerp(bilek, f_) + Vector((.012 * s, -.022, 0))
        V, F, _ = boru([p, p + Vector((.5 * s, -.8, -.25)).normalized() * .038], [.008, .001], 6)
        gov.ekle(V, F, {f'calf_{k}': 1.}, 'koyu')
    orta = [Vector(p) for p in ((.13 * s, .01, .52), (.185 * s, .02, .487), (.235 * s, .03, .455), (.218 * s, .058, .415),
                                (.20 * s, .085, .375), (.165 * s, .11, .35))]      # orta bacak: kemiksiz, gövdeyle bükülür
    V, F, iz = boru(orta, [.028, .030, .022, .020, .016, .008], 8); gov.ekle(V, F, [W_z(orta[i]) for i in iz], 'govde')

    # ---------- elcik (forma rengi): avuç + 4 parmak + başparmak, parmaklar kendi boğumlarına derili ----------
    bil, orta_k = H(f'hand_{k}'), H(f'middle_01_{k}'); a = (orta_k - bil).normalized()
    r = H(f'index_01_{k}') - H(f'pinky_01_{k}'); gen = r.length; r = (r - a * r.dot(a)).normalized(); n = a.cross(r)
    frm.ekle(*elips(bil + (orta_k - bil) * .52, (gen * .5 + .016, .023, (orta_k - bil).length * .64), 14, 8, (r, n, a)), {f'hand_{k}': 1.})
    d = (bil - H(f'lowerarm_{k}')).normalized(); ux = d.cross(Vector((0, 0, 1))).normalized(); uy = d.cross(ux)
    V, F = halka((0, 0, 0), .034, .034, .007, .010, 16, 6)                           # manşet: bilekte, ön kol ekseninde
    frm.ekle([bil - d * .012 + ux * q.x + uy * q.y + d * q.z for q in V], F, {f'hand_{k}': 1.})
    for f_, rr in (('index', .0155), ('middle', .016), ('ring', .0155), ('pinky', .014), ('thumb', .0175)):
        pts = [H(f'{f_}_01_{k}'), H(f'{f_}_02_{k}'), H(f'{f_}_03_{k}'), T_(f'{f_}_03_{k}')]
        if f_ != 'thumb': pts[0] = pts[0] - (pts[1] - pts[0]).normalized() * .006   # kökü avucun içine
        P, t, W = zincir(pts, [f'{f_}_01_{k}', f'{f_}_02_{k}', f'{f_}_03_{k}'], .006, .010)
        if f_ == 'thumb': W[0] = {f'hand_{k}': 1.}
        V, F, iz = boru(P, [rr] * len(P), 8); frm.ekle(V, F, [W[i] for i in iz])

    # ---------- diva (yalnız ön izleme): küpe + kirpik ----------
    kc = Vector((.172 * s, .02, .775))
    for i in range(14):
        f_ = 2 * math.pi * i / 14; p = kc + Vector((0, .030 * math.cos(f_), -.030 + .030 * math.sin(f_)))
        diva.ekle(*elips(p, (.007,) * 3, 6, 4), BAS, 'altin')
    for dx, dz in ((.62, .30), (.74, .12), (.80, -.06)):
        p = kap + Vector((dx * s, .55, dz)).normalized() * .070
        V, F, _ = boru([p, p + Vector((dx * s, .30, dz + .45)).normalized() * .030], [.0035, .0008], 5); diva.ekle(V, F, BAS, 'siyah')

# ---------- bant + bileklik (uygulama sözleşmesi: kuşanılınca görünür) ----------
BANT_Z = .935; kf = math.sqrt(1 - ((BANT_Z - .86) / .135) ** 2)
bant = Parca(); bant.ekle(*halka((0, .02, BANT_Z), .165 * kf + .004, .145 * kf + .004, .006, .016, 36, 6), BAS)
e0, w0 = H('lowerarm_r'), H('hand_r'); d = (w0 - e0).normalized(); cb = w0 - d * .045
ux = d.cross(Vector((0, 0, 1))).normalized(); uy = d.cross(ux); V, F = halka((0, 0, 0), .030, .030, .006, .016, 20, 6)
bilek = Parca(); bilek.ekle([cb + ux * q.x + uy * q.y + d * q.z for q in V], F, {'lowerarm_r': 1.})

# ---------- diva: kürk etol (boyun çevresi + önde iki sarkan uç) ve ruj ----------
for i in range(16):
    f_ = 2 * math.pi * i / 16; p = Vector((.185 * math.sin(f_), -.015 + .15 * math.cos(f_), .705 + .012 * math.cos(2 * f_)))
    diva.ekle(*elips(p, (.058, .058, .052), 8, 6), W_z, 'kurk')
for s in (1, -1):
    for i in range(5):
        p = Vector((s * (.105 - .008 * i), .125 - .004 * i, .655 - .052 * i)); diva.ekle(*elips(p, (.052 - .004 * i,) * 3, 8, 6), W_z, 'kurk')
diva.ekle(*elips((0, .190, .788), (.032, .010, .013), 10, 6), BAS, 'ruj')

if ERKEK:   # papyon: göğsün üst önünde düğüm + iki kanat
    gov.ekle(*elips((0, .092, .712), (.016, .014, .016), 8, 6), W_z, 'ruj')
    for s in (1, -1): gov.ekle(*elips((.042 * s, .086, .712), (.034, .012, .026), 10, 6), W_z, 'ruj')

parcalar = [gov.nesne('hb_govde', body, 'govde'), frm.nesne('hb_forma', forma_m, 'forma'),
            bant.nesne('hb_bant', bant_m, 'bant'), bilek.nesne('hb_bilek', bant_m, 'bilek')]
diva_o = None if ERKEK else diva.nesne('hb_diva', body, 'diva')
tri = lambda o: sum(len(p.vertices) - 2 for p in o.data.polygons)
print(AD, [(o.name, tri(o)) for o in parcalar + ([diva_o] if diva_o else [])], 'üçgen (ayakkabı, raket hariç)', sum(tri(o) for o in parcalar))


def disa_aktar(hedef=RES):
    """Unity'ye: <ad>.fbx (unity_fbx.py ile aynı ayarlar) + <ad>_clips.json (Tüycük'ün klip aralıkları, bu modelin parça listesi)"""
    cikan = [o for o in col.objects if o is not diva_o]
    bpy.ops.object.select_all(action='DESELECT')
    for o in cikan: o.hide_set(False); o.select_set(True)
    if diva_o: diva_o.hide_set(True)
    sc.frame_set(1); fbx = os.path.join(hedef, AD + '.fbx')
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'EMPTY', 'MESH', 'ARMATURE'},
        apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=False,
        use_mesh_modifiers=True, mesh_smooth_type='FACE', use_armature_deform_only=False, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_step=1.0,
        bake_anim_simplify_factor=0.01, bake_anim_force_startend_keying=True, path_mode='COPY', embed_textures=True)
    meta = json.load(open(os.path.join(RES, VERICI + '_clips.json'), encoding='utf-8'))
    hc = (H('head') + T_('head')) / 2   # bas__isaret; bant halkası ona göre (Unity Aksesuar şapkayı buna oturtur)
    meta['model'] = AD
    meta['parts'] = sorted(o.name for o in cikan if o.type == 'MESH' and not o.name.endswith('__isaret'))
    meta['bant'] = {'dz': round(BANT_Z - hc.z, 4), 'df': round(.02 - hc.y, 4), 'r': round((.165 + .145) / 2 * kf + .004, 4)}
    json.dump(meta, open(os.path.join(hedef, AD + '_clips.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
    if diva_o: diva_o.hide_set(False)
    print('FBX', fbx, os.path.getsize(fbx), 'kemik', len(B), 'parça', len(meta['parts']), 'klip', len(meta['clips']))


bpy.app.driver_namespace['hb_disa_aktar'] = disa_aktar
