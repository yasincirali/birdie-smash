# Tüycük (yasin 2026-10-29: "Oyuncu modellerimi değiştirme" → "Eğer yeni 3d model önerin varsa yeni seçenek olarak ekle").
# Dördüncü seçilebilir model; Oyuncak 3B, Gerçekçi ve Sade'nin dosyalarına ve FBX'lerine dokunmaz. Tasarım onaylı yönden
# (tuycukler artifact'i, tuycuk() SVG'si): yumurta/armut biçimli tek parça gövde (baş ve gövde aynı yuvarlak beden), iri yuvarlak
# gözler (koyu bebek, beyaz ışık), küçük gülüş, tepede üç yapraklı tüy perçemi, kısa yuvarlak kollar ve parmaklıksız eldiven
# eller (raket HEP sağ elde), kısa bacaklar ve tıknaz ayakkabılar, açık renk karın. Özgün tasarım (hiçbir IP'ye benzetilmedi).
#
# Neden Oyuncak'ın iskeleti (oyuncak.Oyuncak, tip atlet): yasin "animasyonlar değişmesin" dedi. Aynı UE adlı 53 kemik, aynı poz
# sözlükleri (karakter.py), aynı tutuş katmanı (tutus.py) ve aynı iç içe geçme çözücü; yalnız görünen ağ yenidir. Oyuncak
# kurulduktan sonra gövde/forma/saç/bant/bileklik ağları silinir, ayakkabılar (5 model, ayk_<varlik>__ayakkabi; aynı ayak
# kemikleri) kalır: Ayakkabı sayfası bu modelde de modeli gösterir.
# Çözücü ve tutuş YENİ ağla ölçer: yumurta Oyuncak'ın göğsünden ~10 cm önde, el ve raket ona göre dışarı itilir (aynı klipler,
# girme yok). Üst kol ve ön kolun dirsek yarısı bilerek yumurtanın içinden çıkar (kol gövdeden doğar): çözücüde bu köşeler
# 'diger' sayılır, yoksa her pozda dirseği gövdeden çıkarmaya uğraşıp pozu bozardı.
# Deri: yumurta ve ona yapışan her şey (göz, gülüş, karın, perçem, bant) aynı yükseklik işlevinden ağırlık alır (W_govde):
# alttan üste pelvis → spine_01..03 → neck_01 → head. Yüzey ve üstündeki çıkartma aynı noktada aynı ağırlığı taşır, kayma yok.
# Kol, bacak ve eldiven parmakları kemik zinciri boyunca yay uzunluğuyla harmanlanır (eklemde ±yarım genişlik).
# Renk sözleşmesi (unity_fbx "<ad>__<etiket>"): tc_tuy__forma (yumurta + perçem + kol + bacak; oyun takım/forma rengine boyar),
# tc_karin__karin (Unity forma renginin açığı; Unity'siz FBX rengi krem), tc_govde__govde (göz, gülüş, eldiven: Oyuncak'ın renk
# atlası + bu modelin hücreleri, malzeme oy_body → PlayerView.DokuMalzeme tuycuk_govde.png), tc_bant__bant, tc_bilek__bilek.
#   BS_MODEL=tuycuk python3 -I tool/karakter/unity_fbx.py -- <çıktı> tuycuk   → tuycuk.fbx, tuycuk_clips.json, tuycuk_govde.png
import bpy, bmesh, math, os, sys
from mathutils import Vector, Matrix
import insan, oyuncak, tutus
from oyuncak import E, _ss

# yumurta (model birimi, dinlenme; figür +y'ye bakar, sağ el +x). Taban .31 kalçanın 8 cm altı: uyluğun üstü gövdede, görünen
# bacak dizden aşağısı ("kısa bacak"). Tepe .95 (Oyuncak başının tepesi ~.985); perçem 1,08'e çıkar.
Z0, Z1 = E('BS_TC_ALT', '.31'), E('BS_TC_UST', '.95')
GEN = E('BS_TC_EN', '.25')     # en geniş yarıçap (x); Oyuncak omzu .156: kol gövdenin içinden doğar
KY = E('BS_TC_DERIN', '.86')   # derinlik/en: tam yuvarlak değil, önden-arkadan hafif basık (raket yolu kısalmasın)
YUM = E('BS_TC_YUM', '.18')    # yumurta katsayısı: alt geniş, üst dar (armut)
Y0 = .005
_f = lambda u: math.sqrt(max(0., 1 - u * u)) * (1 - YUM * u)
def _rmax():
    return max(_f(-1 + 2 * i / 400) for i in range(401))
R0 = GEN / _rmax()
def r_z(z):
    u = 2 * (z - Z0) / (Z1 - Z0) - 1
    return R0 * _f(u)
def F(p):
    r = max(r_z(p.z), 1e-6); return (p.x / r) ** 2 + ((p.y - Y0) / (KY * r)) ** 2 - 1
def N_(p):
    h = 1e-4; g = Vector(((F(p + Vector((h, 0, 0))) - F(p - Vector((h, 0, 0)))), (F(p + Vector((0, h, 0))) - F(p - Vector((0, h, 0)))),
                         (F(p + Vector((0, 0, h))) - F(p - Vector((0, 0, h))))))
    return g.normalized()
def on_y(x, z):
    """yumurtanın ön yüzeyi (x, z) noktasında"""
    r = r_z(z); return Y0 + KY * math.sqrt(max(0., r * r - x * x))

# gövde ağırlığı: yükseklik düğümleri (kemik, z), alttan pelvis → spine_01..03 → neck_01. Baş kemiği tepede en çok %45 alır
# (BS_TC_BAS): ilk ön izlemede tepe tam başa bağlıydı, smaçta topa bakan baş (yukarı ~60°) yumurtanın tepesini kapak gibi
# arkaya devirip perçemi yatırıyordu. Bu modelde baş ve gövde bir: bakışın kalanını gövde taşır, tepe yalnız yumuşakça eğilir.
# Göz (.62–.78) spine_03 / neck_01 arasında; gözle yüzey aynı işlevden ağırlık aldığı için göz yüzeyde kaymaz.
DUGUM = (('pelvis', .37), ('spine_01', .44), ('spine_02', .50), ('spine_03', .62), ('neck_01', .80))
BAS_PAY, BAS_Z = E('BS_TC_BAS', '.45'), .93
def W_govde(p):
    z = p.z
    if z <= DUGUM[0][1]: return {DUGUM[0][0]: 1.}
    if z >= DUGUM[-1][1]:
        u = BAS_PAY * _ss(DUGUM[-1][1], BAS_Z, z); return {'neck_01': 1 - u, 'head': u}
    for (a, za), (b, zb) in zip(DUGUM, DUGUM[1:]):
        if za <= z <= zb:
            u = _ss(za, zb, z); return {a: 1 - u, b: u}

def srgb_lin(h):
    c = [int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    return tuple(x / 12.92 if x <= .04045 else ((x + .055) / 1.055) ** 2.4 for x in c)
def _mat(n, h, r=.6):
    return oyuncak._mat(n, srgb_lin(h), r)

def _ag(ad, V, Fc, W, mat, tag, uv=None):
    """köşe/yüz/ağırlık listesinden derili nesne. uv: yüz başına (u,v) atlas hücresi"""
    me = bpy.data.meshes.new(ad); me.from_pydata([tuple(v) for v in V], [], Fc); me.update()
    o = bpy.data.objects.new(ad, me); bpy.context.scene.collection.objects.link(o)
    me.materials.append(mat); o['tag'] = tag
    gr = {}
    for i, w in enumerate(W):
        for b, x in w.items():
            if x < 1e-4: continue
            if b not in gr: gr[b] = o.vertex_groups.new(name=b)
            gr[b].add([i], x, 'REPLACE')
    for p in me.polygons: p.use_smooth = True
    if uv is not None:
        ul = me.uv_layers.new(name='UVMap')
        for p, c in zip(me.polygons, uv):
            for li in p.loop_indices: ul.data[li].uv = c
    return o

class Parca:
    """birikimli ağ: köşe, yüz, ağırlık, yüz başına uv; sonra tek nesne"""
    def __init__(s): s.V = []; s.F = []; s.W = []; s.UV = []; s.isim = []
    def ekle(s, V, Fc, W, uv=(0, 0), isim=''):
        b = len(s.V); s.V += V; s.W += W; s.F += [[b + i for i in f] for f in Fc]; s.UV += [uv] * len(Fc); s.isim += [isim] * len(V)

def _sinir(Fc, V, dis):
    """yüzlerin dönüşünü dışa çevir: dis(p) dış yön"""
    out = []
    for f in Fc:
        a, b, c = (V[f[0]], V[f[1]], V[f[2]])
        n = (b - a).cross(c - a); m = sum((V[i] for i in f), Vector()) / len(f)
        out.append(f if n.dot(dis(m)) >= 0 else list(reversed(f)))
    return out

def yumurta(nz=26, nt=36):
    V = []; Fc = []
    V.append(Vector((0, Y0, Z0)))
    for i in range(1, nz):
        t = (1 - math.cos(math.pi * i / nz)) / 2; z = Z0 + t * (Z1 - Z0); r = r_z(z)
        for j in range(nt):
            a = 2 * math.pi * j / nt; V.append(Vector((r * math.sin(a), Y0 + KY * r * math.cos(a), z)))
    V.append(Vector((0, Y0, Z1)))
    top = len(V) - 1
    for j in range(nt): Fc.append([0, 1 + (j + 1) % nt, 1 + j])
    for i in range(nz - 2):
        for j in range(nt):
            a = 1 + i * nt + j; b = 1 + i * nt + (j + 1) % nt; Fc.append([a, b, b + nt, a + nt])
    l = 1 + (nz - 2) * nt
    for j in range(nt): Fc.append([l + j, l + (j + 1) % nt, top])
    return V, _sinir(Fc, V, lambda m: N_(m) if F(m) > -2 else m - Vector((0, Y0, m.z)))

def tup(P, R, W_fn, nt=14, bas=True, son=True):
    """yol boyunca boru: P noktalar, R yarıçaplar, W_fn(i, k) ağırlık (k: 0..1 halka içi kesir değil, yay kesri).
    Uçlar yarım küre kapak. Paralel taşıma çerçevesi (dirsekte burulma yok)."""
    T = []
    for i in range(len(P)):
        a = P[max(0, i - 1)]; b = P[min(len(P) - 1, i + 1)]; T.append((b - a).normalized())
    u = T[0].cross(Vector((0, 0, 1)) if abs(T[0].z) < .9 else Vector((1, 0, 0))).normalized()
    halkalar = []   # (merkez, yarıçap, u, v, yol indisi)
    for i in range(len(P)):
        if i: u = (u - T[i] * u.dot(T[i])).normalized()
        v = T[i].cross(u); halkalar.append((P[i], R[i], u.copy(), v, i))
    V = []; Wv = []; Fc = []
    def halka(c, r, u, v, wi):
        b = len(V)
        for j in range(nt):
            a = 2 * math.pi * j / nt; V.append(c + (u * math.cos(a) + v * math.sin(a)) * r); Wv.append(wi)
        return b
    ilk = []
    if bas:
        c, r, u0, v0, _ = halkalar[0]
        for al in (40, 70):   # kutuptan gövdeye
            ilk.append(halka(c - T[0] * r * math.cos(math.radians(al)), r * math.sin(math.radians(al)), u0, v0, W_fn(0)))
    gov = [halka(c, r, u_, v_, W_fn(i)) for c, r, u_, v_, i in halkalar]
    sonh = []
    if son:
        c, r, u0, v0, i = halkalar[-1]
        for al in (70, 40):   # gövdeden kutba
            sonh.append(halka(c + T[-1] * r * math.cos(math.radians(al)), r * math.sin(math.radians(al)), u0, v0, W_fn(i)))
    zincir = ilk + gov + sonh
    for a, b in zip(zincir, zincir[1:]):
        for j in range(nt): Fc.append([a + j, a + (j + 1) % nt, b + (j + 1) % nt, b + j])
    if bas:
        c, r = halkalar[0][0], halkalar[0][1]; V.append(c - T[0] * r); Wv.append(W_fn(0)); k = len(V) - 1
        for j in range(nt): Fc.append([zincir[0] + (j + 1) % nt, zincir[0] + j, k])
    else:
        V.append(halkalar[0][0].copy()); Wv.append(W_fn(0)); k = len(V) - 1
        for j in range(nt): Fc.append([zincir[0] + (j + 1) % nt, zincir[0] + j, k])
    c, r = halkalar[-1][0], halkalar[-1][1]; V.append(c + T[-1] * r if son else c.copy()); Wv.append(W_fn(len(P) - 1)); k = len(V) - 1
    for j in range(nt): Fc.append([zincir[-1] + j, zincir[-1] + (j + 1) % nt, k])
    # dönüş: kapalı boru; merkez çizgisinden dışa
    def dis(m):
        best = min(range(len(P)), key=lambda i: (P[i] - m).length); return m - P[best]
    return V, _sinir(Fc, V, dis), Wv

def zincir_yolu(noktalar, kemikler, harman, adim=.012):
    """kemik zinciri boyunca örnek noktalar ve her noktanın ağırlığı: eklemde (yay uzunluğu) ±harman ile smoothstep"""
    P = [noktalar[0]]; S = [0.]
    for a, b in zip(noktalar, noktalar[1:]):
        n = max(2, int((b - a).length / adim))
        for i in range(1, n + 1): P.append(a.lerp(b, i / n)); S.append(S[-1] + (b - a).length / n)
    L = [0.]
    for a, b in zip(noktalar, noktalar[1:]): L.append(L[-1] + (b - a).length)
    def w(i):
        s = S[i]; out = {kemikler[0]: 1.}
        for j in range(1, len(kemikler)):
            u = _ss(L[j] - harman, L[j] + harman, s)
            if u <= 0: break
            prev = kemikler[j - 1]; out = {k: v * (1 - u) for k, v in out.items()}; out[kemikler[j]] = out.get(kemikler[j], 0) + u
        return {k: v for k, v in out.items() if v > 1e-4}
    return P, w

def cikartma(cx, cz, ax, az, h, nr=6, nf=28, yuz=None):
    """yumurta önüne oturan disk (göz, bebek, karın): (x, z) elipsi, yüksekliği h(x, z, ρ) normal boyunca"""
    V = []; Fc = []
    def nok(x, z, rho):
        p = Vector((x, on_y(x, z), z)); n = N_(p); return p + n * h(x, z, rho)
    V.append(nok(cx, cz, 0.))
    for i in range(1, nr + 1):
        rho = i / nr
        for j in range(nf):
            a = 2 * math.pi * j / nf; V.append(nok(cx + ax * rho * math.cos(a), cz + az * rho * math.sin(a), rho))
    for j in range(nf): Fc.append([0, 1 + j, 1 + (j + 1) % nf])
    for i in range(nr - 1):
        for j in range(nf):
            a = 1 + i * nf + j; b = 1 + i * nf + (j + 1) % nf; Fc.append([a, a + nf, b + nf, b])
    return V, _sinir(Fc, V, lambda m: N_(m))

def elipsoid(c, X, Y, Z, rx, ry, rz, nu=16, nv=10):
    V = [c - Z * rz]; Fc = []
    for i in range(1, nv):
        t = math.pi * i / nv; z = -math.cos(t); s_ = math.sin(t)
        for j in range(nu):
            a = 2 * math.pi * j / nu; V.append(c + X * (rx * s_ * math.cos(a)) + Y * (ry * s_ * math.sin(a)) + Z * (rz * z))
    V.append(c + Z * rz); top = len(V) - 1
    for j in range(nu): Fc.append([0, 1 + (j + 1) % nu, 1 + j])
    for i in range(nv - 2):
        for j in range(nu):
            a = 1 + i * nu + j; b = 1 + i * nu + (j + 1) % nu; Fc.append([a, b, b + nu, a + nu])
    l = 1 + (nv - 2) * nu
    for j in range(nu): Fc.append([l + j, l + (j + 1) % nu, top])
    return V, _sinir(Fc, V, lambda m: m - c)

def simit(egri, N_fn, T_fn, a, b, nf=48, nk=8):
    """kapalı eğri çevresinde eliptik kesitli halka (bant, bileklik): egri(φ) merkez, N_fn dış normal, T_fn kesit ikinci ekseni"""
    V = []; Fc = []
    for i in range(nf):
        f = 2 * math.pi * i / nf; c = egri(f); n = N_fn(f, c); t = T_fn(f, c)
        for j in range(nk):
            p = 2 * math.pi * j / nk; V.append(c + n * (a * math.cos(p)) + t * (b * math.sin(p)))
    for i in range(nf):
        for j in range(nk):
            A = i * nk + j; B = i * nk + (j + 1) % nk; C = ((i + 1) % nf) * nk + (j + 1) % nk; D = ((i + 1) % nf) * nk + j; Fc.append([A, B, C, D])
    def dis(m):
        best = min(range(nf), key=lambda i: (egri(2 * math.pi * i / nf) - m).length); return m - egri(2 * math.pi * best / nf)
    return V, _sinir(Fc, V, dis)

# renkler (sRGB): karın kremi, göz akı, bebek ve gülüş mürekkebi (artifact KOYU #231a33), eldiven sütlü beyaz, ışık noktası.
RENK = dict(karin='#fff0d8', goz='#fbfbfb', bebek='#231a33', isik='#ffffff', eldiven='#f6f1e7')

class Tuycuk(oyuncak.Oyuncak):
    def __init__(s, M, stil=None):
        if oyuncak.TIP_ID != 'atlet': raise SystemExit('tuycuk: Oyuncak atlet iskeleti gerekir (BS_OY_TIP boş ya da atlet)')
        super().__init__(M, stil)
        ao = s.ao; B = ao.data.bones; sc = bpy.context.scene
        H = lambda n: B[n].head_local.copy(); T_ = lambda n: B[n].tail_local.copy()
        # --- Oyuncak'ın görünen ağları silinir; ayakkabılar kalır
        ayk = list(s.ayakkabilar.values())
        for o in list(s.kalici):
            if o in ayk: continue
            bpy.data.objects.remove(o)
        body = ayk[0].data.materials[1]   # oy_body: Oyuncak atlası (ayakkabı tabanı/şeridi hücreleri) + bu modelin hücreleri
        img = body.node_tree.nodes['Image Texture'].image if 'Image Texture' in body.node_tree.nodes else next(n.image for n in body.node_tree.nodes if n.type == 'TEX_IMAGE')
        PX = img.size[0]; NA = 8; cel = PX // NA; pix = list(img.pixels)
        bos = [i for i in range(NA * NA) if pix[(((i // NA) * cel + cel // 2) * PX + (i % NA) * cel + cel // 2) * 4 + 3] < .5]
        hucre = {}
        for k in ('goz', 'bebek', 'isik', 'eldiven'):
            i = bos.pop(0); hucre[k] = ((i % NA + .5) / NA, (i // NA + .5) / NA)
            e = [int(RENK[k][j:j + 2], 16) / 255 for j in (1, 3, 5)]
            for yy in range((i // NA) * cel, (i // NA + 1) * cel):
                for xx in range((i % NA) * cel, (i % NA + 1) * cel): q = (yy * PX + xx) * 4; pix[q:q + 4] = [*e, 1.]
        img.pixels = pix; img.name = 'tuycuk_govde'
        args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
        hedef_d = args[0] if args and os.path.isdir(args[0]) else bpy.app.tempdir
        img.filepath_raw = os.path.join(hedef_d, 'tuycuk_govde.png'); img.file_format = 'PNG'; img.save(); img.pack(); s.atlas = img.filepath_raw
        forma = s.mats['forma']; karin_m = _mat('tc_karin', RENK['karin'], .55)
        tuy = Parca(); gov = Parca(); kar = Parca()
        # 1) yumurta gövde
        V, Fc = yumurta(); tuy.ekle(V, Fc, [W_govde(p) for p in V], isim='yumurta')
        # 2) perçem: üç yuvarlak yaprak, tepeye 2 cm gömülü; ortası dik ve hafif arkaya, yanlar ±38°
        tepe = Vector((0, Y0 - .012, Z1 - .02))
        for aci, boy, en, dx in ((0, .080, .030, 0.), (38, .064, .026, .018), (-38, .064, .026, -.018)):
            R = Matrix.Rotation(math.radians(aci), 3, 'Y') @ Matrix.Rotation(math.radians(-12), 3, 'X')
            Z = R @ Vector((0, 0, 1)); X = R @ Vector((1, 0, 0)); Y = R @ Vector((0, 1, 0))
            c = tepe + Vector((dx, 0, 0)) + Z * (boy * .82)
            V, Fc = elipsoid(c, X, Y, Z, en, .013, boy, 12, 8); tuy.ekle(V, Fc, [W_govde(p) for p in V], isim='percem')
        # 3) kollar: omuzdan (yumurtanın içinde) bileğe; üst kol .042 → dirsek .040 → bilek .035. Bilek ucu kapalı, eldiven örter.
        for k in 'rl':
            pts = [H(f'upperarm_{k}'), H(f'lowerarm_{k}'), H(f'hand_{k}')]
            P, w = zincir_yolu(pts, [f'upperarm_{k}', f'lowerarm_{k}', f'hand_{k}'], .022, adim=.018)
            s_ = [0.]
            for a, b in zip(P, P[1:]): s_.append(s_[-1] + (b - a).length)
            R = [.048 - .008 * (x / s_[-1]) for x in s_]   # ilk ön izlemede .042: uzun ince boru gibi okunuyordu (kemik boyu sabit, kalınlık kısaltır)
            V, Fc, Wv = tup(P, R, w, nt=12)
            tuy.ekle(V, Fc, Wv, isim='kol_' + k)
        # 4) bacaklar: kalça (gövde içinde) → diz → ayak bileği (ayakkabının içinde)
        for k in 'rl':
            pts = [H(f'thigh_{k}'), H(f'calf_{k}'), H(f'foot_{k}')]
            P, w = zincir_yolu(pts, [f'thigh_{k}', f'calf_{k}', f'foot_{k}'], .025, adim=.02)
            s_ = [0.]
            for a, b in zip(P, P[1:]): s_.append(s_[-1] + (b - a).length)
            R = [.058 - .010 * (x / s_[-1]) for x in s_]
            V, Fc, Wv = tup(P, R, w, nt=12)
            tuy.ekle(V, Fc, Wv, isim='bacak_' + k)
        # 5) eldivenler: avuç elipsoidi (el kemiği çerçevesi: a bilek → orta parmak kökü, n avuç, r yan) + 4 kalın parmak (yan
        #    yana kaynaşır: eldiven) + başparmak. Her parmak kendi üç boğumuyla derilir: tutus.py parmakları sapa sardıkça eldiven
        #    de sarar; raket sağ eldivende.
        for k in 'rl':
            W = H(f'hand_{k}'); Km = H(f'middle_01_{k}'); a = (Km - W).normalized(); n = Vector(s.avuc[k]); n = (n - a * n.dot(a)).normalized(); r = a.cross(n)
            gen = (H(f'index_01_{k}') - H(f'pinky_01_{k}')).length
            c = W + (Km - W) * .52
            V, Fc = elipsoid(c, r, n, a, gen * .5 + .014, .021, (Km - W).length * .62, 16, 10)
            gov.ekle(V, Fc, [{f'hand_{k}': 1.}] * len(V), hucre['eldiven'], isim='avuc_' + k)
            for f, rr in (('index', .0150), ('middle', .0155), ('ring', .0150), ('pinky', .0135), ('thumb', .0170)):
                pts = [H(f'{f}_01_{k}'), H(f'{f}_02_{k}'), H(f'{f}_03_{k}'), T_(f'{f}_03_{k}')]
                if f != 'thumb': pts[0] = pts[0] - (pts[1] - pts[0]).normalized() * .006   # kökü avucun içine
                P, w = zincir_yolu(pts, [f'{f}_01_{k}', f'{f}_02_{k}', f'{f}_03_{k}'], .006, adim=.010)
                if f == 'thumb':
                    w0 = w; w = lambda i, w0=w0, k=k: ({f'hand_{k}': 1.} if i == 0 else w0(i))
                V, Fc, Wv = tup(P, [rr] * len(P), w, nt=8)
                gov.ekle(V, Fc, Wv, hucre['eldiven'], isim=f'{f}_{k}')
        # 6) yüz: göz akı kubbe (h .018), bebek (göz yüzeyinin üstünde, ortaya ve öne bakar), ışık noktası (iki gözde aynı yan:
        #    tek ışık), gülüş (∪ şerit). Tasarım oranı artifact'ten: göz merkezi gövde boyunun %61'i, aralık ±.075.
        GZ, GX, GA, GB, GH = E('BS_TC_GOZZ', '.70'), .075, .060, .078, .018
        def goz_h(x, z, cx):
            rho = math.sqrt(((x - cx) / GA) ** 2 + ((z - GZ) / GB) ** 2); return .0012 + GH * math.sqrt(max(0., 1 - min(1., rho) ** 2))
        for sx in (1, -1):
            cx = sx * GX
            V, Fc = cikartma(cx, GZ, GA, GB, lambda x, z, rho, cx=cx: goz_h(x, z, cx), nr=6, nf=24)
            gov.ekle(V, Fc, [W_govde(p) for p in V], hucre['goz'], isim='goz')
            bx, bz, ba, bb = cx - sx * .010, GZ - .006, .030, .035
            V, Fc = cikartma(bx, bz, ba, bb, lambda x, z, rho, cx=cx: goz_h(x, z, cx) + .0016 + .004 * math.sqrt(max(0., 1 - rho * rho)), nr=4, nf=22)
            gov.ekle(V, Fc, [W_govde(p) for p in V], hucre['bebek'], isim='bebek')
            ix, iz = bx + .011, bz + .014
            V, Fc = cikartma(ix, iz, .0095, .0095, lambda x, z, rho, cx=cx: goz_h(x, z, cx) + .0016 + .004 + .0018, nr=2, nf=12)
            gov.ekle(V, Fc, [W_govde(p) for p in V], hucre['isik'], isim='isik')
        GU_Z, GU_W, GU_D, GU_K = E('BS_TC_GULUSZ', '.585'), .058, .020, .0075   # ilk ön izlemede .565/.0058: yüksek kameradan seçilmiyordu
        V = []; Fc = []; n = 18
        for i in range(n + 1):
            x = -GU_W + 2 * GU_W * i / n; u = x / GU_W; zc = GU_Z - GU_D * (1 - u * u)
            k_ = GU_K * (.45 + .55 * math.sqrt(max(0., 1 - u ** 6)))
            for dz in (-k_, k_):
                p = Vector((x, on_y(x, zc + dz), zc + dz)); V.append(p + N_(p) * .0016)
        for i in range(n): Fc.append([2 * i, 2 * i + 2, 2 * i + 3, 2 * i + 1])
        Fc = _sinir(Fc, V, lambda m: N_(m)); gov.ekle(V, Fc, [W_govde(p) for p in V], hucre['bebek'], isim='gulus')
        # 7) karın: önde, gülüşün altında; kenarda yüzeye iner (dudak yok)
        V, Fc = cikartma(0., .44, .160, .100, lambda x, z, rho: .0012 + .003 * (1 - rho * rho), nr=6, nf=36)
        kar.ekle(V, Fc, [W_govde(p) for p in V], isim='karin')
        # 8) bant: perçemin altında, yumurtaya oturan halka (yüzey normali + meridyen kesiti); bileklik: sağ ön kolda bileğe yakın
        zb = E('BS_TC_BANTZ', '.855')
        def bant_c(f):
            r = r_z(zb); p = Vector((r * math.sin(f), Y0 + KY * r * math.cos(f), zb)); return p + N_(p) * .006
        def bant_n(f, c): return N_(c)
        def bant_t(f, c):
            n = N_(c); t = Vector((math.cos(f), -KY * math.sin(f), 0)).normalized(); return n.cross(t).normalized()
        V, Fc = simit(bant_c, bant_n, bant_t, .007, .015, nf=40, nk=6)
        bant = _ag('tc_bant', V, Fc, [W_govde(p) for p in V], s.mats['bant'], 'bant')
        e0, w0 = H('lowerarm_r'), H('hand_r'); d = (w0 - e0).normalized(); cb = w0 - d * .042
        u = d.cross(Vector((0, 0, 1))).normalized(); v = d.cross(u)
        rb = .042 - .007 * ((cb - H('upperarm_r')).length / ((H('lowerarm_r') - H('upperarm_r')).length + (w0 - e0).length)) + .004
        V, Fc = simit(lambda f: cb + (u * math.cos(f) + v * math.sin(f)) * rb, lambda f, c: (c - cb).normalized(), lambda f, c: d, .0055, .017, nf=24, nk=6)
        bilek = _ag('tc_bilek', V, Fc, [{'lowerarm_r': 1.}] * len(V), s.mats['bilek'], 'bilek')
        tuy_o = _ag('tc_tuy', tuy.V, tuy.F, tuy.W, forma, 'forma')
        gov_o = _ag('tc_govde', gov.V, gov.F, gov.W, body, 'govde', uv=gov.UV)
        kar_o = _ag('tc_karin', kar.V, kar.F, kar.W, karin_m, 'karin')
        for o in (tuy_o, gov_o, kar_o, bant, bilek):
            md = o.modifiers.new('isk', 'ARMATURE'); md.object = ao
        s.tuy_isim = tuy.isim; s.gov_isim = gov.isim
        s.kalici = [tuy_o, kar_o, gov_o, bant, bilek] + ayk
        gor = set(map(id, s.gorunen()))
        for o in s.kalici:
            if id(o) not in gor: o.hide_render = True; o.hide_set(True)
        s.saclar = {}; s.sac_bant = {}
        s.meta_ek = {'ayakkabi': [{'id': k} for k in s.ayakkabilar]}
        tri = lambda o: sum(len(p.vertices) - 2 for p in o.data.polygons)
        print('tuycuk: parçalar', [(o.name, o['tag'], tri(o)) for o in s.kalici if o not in ayk], 'üçgen (ayakkabı hariç)', sum(tri(o) for o in s.kalici if o not in ayk))
        # --- baş küresi (OyIskelet.hedef: yüz önündeki bilek hedefi dışarı): yumurtanın boyun üstü (z > .80) tepesi
        hv = [p for p, i_ in zip(tuy.V, tuy.isim) if p.z > DUGUM[-1][1] and i_ == 'yumurta']
        hc = sum(hv, Vector()) / len(hv); s.I.bas_kur(hc, sorted((p - hc).length for p in hv)[int(.8 * len(hv))])
        # --- tutuş: eldivenle ölçer (parmak kalınlığı, avuç derinliği, deri denetimi)
        grk = E('BS_OY_SAP', '.78')
        s.I.tut = tutus.Tutus(ao, gov_o, s.avuc, s.raket_s, grk); s.klip_bilgi = None
        # --- çözücü: yeni ağ. Raket örnek noktaları Oyuncak ile aynı (yerel çerçeve)
        nes, _ = insan.raket(Vector((0, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)), s.raket_s, M, ad='rk_ornek')
        ry = []
        for o in nes:
            if o.get('tag') in ('cerceve', 'koyu', 'grip'):
                vs = [o.matrix_world @ v.co for v in o.data.vertices]; ry += vs[::3] if o.get('tag') == 'cerceve' else vs
        for o in nes: bpy.data.objects.remove(o)
        par = [o for o in s.gorunen() if o is not kar_o]   # karın açık bir çıkartma: ışın paritesini bozar, yumurtanın üstünde zaten
        s.C = oyuncak.Carpisma(ao, par, ry)
        # kolun gövdeden doğan kısmı 'diger': üst kol tamamen, ön kolun dirsek yarısı; yüz çıkartmaları (açık yüzey) 'diger'
        for ix, o in enumerate(par):
            vk = s.C.vk[ix]
            if o is tuy_o:
                for i, v in enumerate(o.data.vertices):
                    if not s.tuy_isim[i].startswith('kol_'): continue
                    k = s.tuy_isim[i][-1]; h_, t_ = H(f'lowerarm_{k}'), H(f'hand_{k}'); dd = t_ - h_
                    if vk[i].startswith('kol') or (vk[i].startswith('onkol') and (v.co - h_).dot(dd) / dd.length_squared < .5): vk[i] = 'diger'
            elif o is gov_o:
                for i in range(len(o.data.vertices)):
                    if s.gov_isim[i] in ('goz', 'bebek', 'isik', 'gulus'): vk[i] = 'diger'
            fk = []
            for f in o.data.polygons:
                c = [vk[i] for i in f.vertices]; fk.append((tuple(f.vertices), max(set(c), key=c.count)))
            s.C.fk[ix] = fk
        s.olcum = {}
        print('tuycuk: yumurta', round(Z0, 3), '→', round(Z1, 3), 'en', GEN, 'derinlik', KY, 'baş küresi r', round(s.I.bas_r, 4))
    def coz(s, T, tur=20):
        """Oyuncak çözücüsü, bilek/ayak bileği itme sınırı 7 → 16 cm (model; BS_TC_MAXIT). Ölçüm (2026-10-29, klibin en kötü
        karesi, oyun cm): yumurta göğüsten ~10 cm önde olduğu için 7 cm'lik sınırda el gövdede kalıyordu: bh_over 26,2 → 14,9,
        bh_drive 13,4 → 1,6, bh_kill 15,5 → 5,0, serve 11,7 → 2,7, smash 2,2 → 0,2. bh_over'ın kalan girmesi hazırlık (bekleme)
        karesinde: raket eli sırtın sol üstünde, yumurtanın içinden geçen yol; temas karesinde 0,1 cm."""
        eski = os.environ.get('BS_OY_MAXIT')
        if eski is None: os.environ['BS_OY_MAXIT'] = os.environ.get('BS_TC_MAXIT', '.16')
        try: return super().coz(T, tur)
        finally:
            if eski is None: del os.environ['BS_OY_MAXIT']
    def kur_poz(s, p):
        out = super().kur_poz(p)
        # ölçüm (rapor): klip başına en kötü kalan girme (çözücü, model birimi), sap-deri işaretli uzaklık (tutuş, <0 içeride)
        k = s.klip_bilgi[0] if s.klip_bilgi else '-'; o = s.olcum.setdefault(k, {'girme': 0., 'sap': 9., 'kalan': 0.})
        o['girme'] = max([o['girme']] + [v for kk, v in s.cozum.items() if not kk.startswith('zemin')])
        ds = getattr(s.I.tut, 'deri_son', None)
        if ds: o['sap'] = min(o['sap'], min(ds.values()))
        o['kalan'] = max(o['kalan'], getattr(s.I.tut, 'kalan', 0.))
        return out
    def aksesuar(s):
        """unity_fbx: klipler pişirildikten sonra. Ek parça yok; ölçüm özeti basılır (oyun cm = ×175)"""
        g = sorted(s.olcum.items(), key=lambda kv: -kv[1]['girme'])
        print('tuycuk ölçüm (oyun cm; girme = çözücüden kalan en derin, sap = eldiven-sap en küçük işaretli uzaklık, kalan = raket sapması °)')
        for k, o in g: print('  %-12s girme %5.2f  sap %6.2f  kalan %5.1f' % (k, o['girme'] * 175, o['sap'] * 175 if o['sap'] < 9 else float('nan'), o['kalan']))
        return []
