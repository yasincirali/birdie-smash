# Gerçekçi model badminton kıyafeti (2026-10-26, yasin: "gerçekçi modeli de revize etmeni istiyorum, modellerimiz iyi
# görünmeli"). Sports_Male_04'ün kolsuz atleti basketbolcu gibi okunuyordu; Rocketbox kütüphanesinde tişörtlü tek sporcu
# Sports_Male_02/03 (futbolcu: kısa kollu forma, şort, diz çorabı, kramponlu ayakkabı). Ağ ve iskelet 02'den alınır
# (kısa saç, sade yüz; 03'ün kabarık saçı yüksek kameradan kafayı iki katına çıkarıyordu), kıyafet BOYANARAK badmintona
# çevrilir: dokunun UV adaları dinlenme pozundaki dünya konumuna (z) ve baskın kemiğe göre sınıflanır, sonra
#  - forma: çizgiler/yazılar/sponsor silinir; açık gri gövde + koyu gri yaka/raglan dikişi/kol ağzı. Oyun formayı
#    takım rengiyle ÇARPAR, gölge ve kıvrım bu gri + normal haritadan gelir (iki tonlu kesim takım renginde görünür);
#  - şort: düz lacivert (beyaz kavis ve numara silinir);
#  - diz çorabı: z > ÇORAP_UST arası ten (diz şeridinin dokusu ve normali aynalanarak), altı beyaz kısa çorap + lastik;
#  - krampon: beyaz kort ayakkabısı (koyu parçalar açık gri), taban kauçuk rengi. Kramponların çivileri ağda ayrı
#    geometri: çivilerin ucu ile taban plakası arasındaki köşeler aynı düzleme indirilir, kalın düz taban olur (taban
#    zemine eskisi gibi çivi ucu yüksekliğinde değer; bilek-taban mesafesi ve klipler değişmez).
# Kıvrımlar, kas ve yüz kaynağın kendi normal haritasından gelir; doku dosyaları (2048) ~/AnimationLibrary/rocketbox/_kit
# altına bir kez yazılır, Unity için 1024'e iner (`python3 -I rocketbox_kit.py -- <Resources/karakter>`).
import bpy, os, sys, glob, math
import numpy as np
from mathutils import Vector

KAYNAK = os.path.expanduser('~/AnimationLibrary/rocketbox/Sports_Male_02')
KIT = os.path.expanduser('~/AnimationLibrary/rocketbox/_kit')
CORAP_UST, LASTIK = .125, .022     # m (dinlenme pozu, kaynak ölçeği): çorap ağzı ayak bileğinin ~5 cm üstü, lastik bandı
TABAN_PLAKA, CIVI_UCU = .0080, -.0118   # m: krampon plakasının altı ve çivi ucu (yan görünüşten ölçüldü)
FORMA_GRI, YAKA_GRI = .90, .46     # oyun bununla takım rengini çarpar (sportif figürde de %80-90 gri)
SORT_RENK = np.array((.075, .095, .15))   # eski düz lacivert (2026-10-27'ye dek dokuya basılıydı; oyunun varsayılan şort rengi bu)
# 2026-10-27 (yasin: "Gerçekçi de vücut özellikleri değil de sadece aksesuarlar değişmeli ... şort tişört"): şort da forma
# gibi tinte hazır açık gri × kıvrım; oyun seçilen şort rengiyle (yoksa SORT_RENK'e denk lacivertle) çarpar. Şort yüzleri
# rocketbox_aksesuar.py'de ayrı ağa (__sort) alınır, ten ağında bu pikseller artık çizilmez.
SORT_GRI = .86
# Forma kumaşları (Ekipman › Forma kumaşı; işlevi Core/Gear'de): her kumaşın forması ayrı renk dokusu (rocketbox_forma_<id>),
# yalnız forma pikselleri değişir. İri biçim maç kamerasından (oyuncu ~100 px) okunsun, ince desen vitrin yakın planında:
# klasik = düz (rocketbox_govde), hafif = koyu yan paneller + mikro delik, nefes = göğüs/sırtta çift şerit + delikli
# havalandırma bölgesi, esnek = çapraz kuşak + ince kaburga. Parlaklık farkı (esnek parlak, hafif mat) PlayerView'da.
KUMASLAR = ('hafif', 'nefes', 'esnek')
SOKET_AYAK = ('Foot', 'Toe0')

# sınıflar
TEN, FORMA, YAKA, SORT, BACAK, AYAKKABI = 0, 1, 2, 3, 4, 5


def _sinif(o):
    """yüz başına sınıf (yalnız body malzemesi). UV düzeni Sports_Male_02 (m301): orta sütun forma adası + kol
    kanatları, iki yanda yukarıdan aşağı şort / diz şeridi / çorap / ön kol+el, altta ayakkabı adaları."""
    me = o.data; uv = me.uv_layers.active.data; gn = {g.index: g.name for g in o.vertex_groups}; out = {}
    for f in me.polygons:
        if f.material_index != 0: continue
        acc = {}
        for vi in f.vertices:
            for g in me.vertices[vi].groups: acc[gn[g.group]] = acc.get(gn[g.group], 0) + g.weight
        bone = max(acc, key=acc.get) if acc else ''
        u = sum(uv[l].uv.x for l in f.loop_indices) / f.loop_total; v = 1 - sum(uv[l].uv.y for l in f.loop_indices) / f.loop_total
        if any(bone.endswith(k) for k in SOKET_AYAK): c = AYAKKABI
        elif ('UpperArm' in bone or 'Clavicle' in bone) and v < .62: c = FORMA
        elif u < .275 or u > .725:
            c = SORT if v < .205 else TEN if v < .315 else BACAK if v < .565 else TEN
        elif (.285 < u < .32 or .68 < u < .715) and v < .31: c = SORT     # şort paçası şeridi
        elif (.32 < u < .355 or .645 < u < .68) and v < .31: c = YAKA     # kol ağzı şeridi
        else: c = FORMA
        out[f.index] = c
    return out


def _rasterle(o, W, sinif, xy=False):
    """UV uzayında piksel başına: sınıf (-1 boş), dinlenme dünyası z, yüz normali z (xy=True: ayrıca dinlenme x ve y;
    forma kumaş desenleri gövdedeki yere göre çizilir)"""
    me = o.data; me.calc_loop_triangles(); uv = me.uv_layers.active.data; mw = o.matrix_world
    S = np.full((W, W), -1, np.int8); Z = np.zeros((W, W), np.float32); NZ = np.zeros((W, W), np.float32)
    X = np.zeros((W, W), np.float32) if xy else None; Y = np.zeros((W, W), np.float32) if xy else None
    P = [mw @ v.co for v in me.vertices]
    for t in me.loop_triangles:
        c = sinif.get(t.polygon_index)
        if c is None: continue
        q = np.array([(uv[l].uv.x * W, (1 - uv[l].uv.y) * W) for l in t.loops]); zz = np.array([P[me.loops[l].vertex_index].z for l in t.loops])
        if xy: xx_ = np.array([P[me.loops[l].vertex_index].x for l in t.loops]); yy_ = np.array([P[me.loops[l].vertex_index].y for l in t.loops])
        x0, y0 = np.floor(q.min(0)).astype(int); x1, y1 = np.ceil(q.max(0)).astype(int)
        x0, y0 = max(x0, 0), max(y0, 0); x1, y1 = min(x1, W - 1), min(y1, W - 1)
        if x1 < x0 or y1 < y0: continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
        (ax, ay), (bx, by), (cx, cy) = q; d = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(d) < 1e-9: continue
        l1 = ((by - cy) * (xs - cx) + (cx - bx) * (ys - cy)) / d; l2 = ((cy - ay) * (xs - cx) + (ax - cx) * (ys - cy)) / d; l3 = 1 - l1 - l2
        m = (l1 >= -.02) & (l2 >= -.02) & (l3 >= -.02)
        if not m.any(): continue
        yy, xx = (ys[m] - .5).astype(int), (xs[m] - .5).astype(int)
        S[yy, xx] = c; Z[yy, xx] = l1[m] * zz[0] + l2[m] * zz[1] + l3[m] * zz[2]
        NZ[yy, xx] = (mw.to_3x3() @ t.normal).normalized().z
        if xy:
            X[yy, xx] = l1[m] * xx_[0] + l2[m] * xx_[1] + l3[m] * xx_[2]; Y[yy, xx] = l1[m] * yy_[0] + l2[m] * yy_[1] + l3[m] * yy_[2]
    return (S, Z, NZ, X, Y) if xy else (S, Z, NZ)


def _blur(a, s):
    from scipy.ndimage import gaussian_filter
    return gaussian_filter(a, s, mode='nearest')


def _ayrinti(L, m, s=10):
    """sınıf içi kıvrım/gölge oranı: parlaklık / aynı tondaki komşuların ortalaması. Baskı (çizgi, yazı) iki ton
    arasında kalır; ton sınırındaki karışık pikseller 1'e çekilir ki silinen çizginin izi kalmasın."""
    from scipy.ndimage import binary_dilation
    r = np.ones_like(L)
    for lo, hi in ((0, .22), (.22, .55), (.55, 2)):
        k = m & (L >= lo) & (L < hi)
        if not k.any(): continue
        mean = _blur(L * k, s) / np.maximum(_blur(k.astype(np.float32), s), 1e-4)
        r[k] = (L / np.maximum(mean, 1e-4))[k]
    from scipy.ndimage import sobel
    kenar = binary_dilation((np.abs(sobel(L, 0)) + np.abs(sobel(L, 1))) > .55, iterations=2)
    r[kenar & m] = 1
    r = np.clip(r, .72, 1.18)
    r = _blur(r, 1.2)
    return r


def _kivrim(N, m, guc=.8):
    """kumaş kıvrım gölgesi kaynağın normal haritasından (diverjans: içbükey kıvrım koyu, tümsek açık). Baskılı formada
    renk dokusunun gölgesi çizgi/yazı sınırlarında iz bırakıyordu; normal haritada baskı yok, yalnız kıvrım var."""
    from scipy.ndimage import sobel
    nx, ny = N[..., 0] * 2 - 1, N[..., 1] * 2 - 1
    d = sobel(_blur(nx, 1.5), 1) / 8 - sobel(_blur(ny, 1.5), 0) / 8   # satır aşağı, v yukarı
    c = 1 + guc * _blur(np.where(m, d, 0), 4.0) * 10   # geniş kıvrım (AO gibi); ince kıvrımı normal harita çizer
    return np.clip(c, .80, 1.06)


def _doldur(img, m):
    """ada dışını en yakın ada pikseliyle doldur (mip seviyelerinde dikiş rengi sızmasın)"""
    from scipy.ndimage import distance_transform_edt
    _, (iy, ix) = distance_transform_edt(~m, return_indices=True)
    return img[iy, ix]


def _oku(p):
    from PIL import Image
    return np.asarray(Image.open(p).convert('RGB')).astype(np.float32) / 255


def _yaz(a, p):
    from PIL import Image
    Image.fromarray((np.clip(a, 0, 1) * 255 + .5).astype(np.uint8)).save(p)


def dokular(o):
    """o: kaynak ölçeğinde (içe aktarıldığı gibi) Sports_Male_02 ağı. Döner: {'renk','normal','bas_renk','bas_normal',
    'sinif'} (yollar + yüz sınıfları). Dokular önbellekte varsa yeniden üretilmez (sınıflar her seferinde hesaplanır)."""
    os.makedirs(KIT, exist_ok=True)
    sinif = _sinif(o)
    yol = {k: os.path.join(KIT, f'rb_{k}.png') for k in ('renk', 'normal', 'parlak', 'bas_parlak') + tuple('forma_' + q for q in KUMASLAR)}
    src = lambda n: glob.glob(os.path.join(KAYNAK, 'Textures', f'*_{n}.tga'))[0]
    ham = os.path.join(KIT, 'surum.txt'); SURUM = '11'
    if not (all(os.path.exists(p) for p in yol.values()) and os.path.exists(ham) and open(ham).read() == SURUM):
        C = _oku(src('body_color')); N = _oku(src('body_normal')); W = C.shape[0]
        S, Z, NZ, X, Y = _rasterle(o, W, sinif, xy=True); cov = S >= 0
        L = C @ np.array((.3, .59, .11), np.float32)
        out = C.copy(); nout = N.copy()
        # forma + yaka: düz gri × kıvrım. Yaka = kaynağın turuncu yaka/raglan şeritleri (göğüsteki kırmızı logo hariç)
        fm = (S == FORMA) | (S == YAKA)
        r = _kivrim(N, fm)
        kirmizi = (C[..., 0] > .42) & (C[..., 1] < .36) & (C[..., 2] < .32)
        H = W; yy, xx = np.mgrid[0:H, 0:W] / H
        logo = (xx > .405) & (xx < .595) & (yy > .585) & (yy < .70)
        yaka = ((S == YAKA) | (fm & kirmizi & ~logo))
        from scipy.ndimage import binary_closing, binary_opening
        yaka = binary_opening(binary_closing(yaka, iterations=2), iterations=1) & fm
        g = np.where(yaka, YAKA_GRI, FORMA_GRI) * r
        g = np.where(fm, _blur(g, .6), g)
        out[fm] = g[fm, None]
        # şort
        sm = S == SORT
        r = _kivrim(N, sm); out[sm] = (SORT_GRI * r[sm, None])
        # bacak: ten / lastik / beyaz çorap. Baldır teni ön kol adasından örneklenir (aynı kıllı, düz ten; diz şeridini
        # aynalamak baldıra ikinci bir diz kapağı gölgesi basıyordu), üst kenarda diz şeridinin son satırlarına karışır.
        from scipy.ndimage import map_coordinates
        bm = S == BACAK; km = S == TEN
        rows = np.where(bm.any(1))[0]; krow = np.where((km & (yy < .305) & (yy > .215)).any(1))[0]   # diz şeridi (şort paçasının kırmızı biyesi hariç)
        if len(rows) and len(krow):
            ten = np.zeros_like(C); tn = np.zeros_like(N)
            r0, r1 = rows.min(), rows.max(); f0, f1 = int(.665 * H), int(.825 * H)
            for c0, c1, g0, g1 in ((0, W // 2, .065, .235), (W // 2, W, .765, .935)):   # sol / sağ bacak → aynı taraftaki ön kol
                cols = np.where(bm[:, c0:c1].any(0))[0] + c0
                if not len(cols): continue
                a0, a1 = cols.min(), cols.max()
                ry, rx = np.mgrid[r0:r1 + 1, a0:a1 + 1]
                sy = f0 + (ry - r0) / max(1, r1 - r0) * (f1 - f0); sx = g0 * W + (rx - a0) / max(1, a1 - a0) * (g1 - g0) * W
                for k in range(3):
                    ten[r0:r1 + 1, a0:a1 + 1, k] = map_coordinates(C[..., k], [sy, sx], order=1)
                    tn[r0:r1 + 1, a0:a1 + 1, k] = map_coordinates(N[..., k], [sy, sx], order=1)
            # renk eşleme: ön kol teni diz şeridinin ortalamasına çekilir (dikişte ton farkı olmasın)
            diz = C[km & (yy < .305) & (yy > .215)].mean(0); kol = ten[bm].mean(0)
            ten = ten * (diz / np.maximum(kol, 1e-3))[None, None, :]
            k1 = krow.max() - 2; ust = np.clip(1 - (np.arange(H) - r0) / (.03 * H), 0, 1)[:, None, None]   # ilk %3: diz satırı
            ten = ten * (1 - ust) + C[k1][None, :, :] * ust
            w = np.clip((Z - CORAP_UST) / .012 + .5, 0, 1)   # 1 = ten
            r = _ayrinti(L, bm, 8)
            beyaz = np.array((.93, .93, .91)) * r[..., None]
            ls = np.clip((Z - (CORAP_UST - LASTIK)) / .004, 0, 1) * (1 - w)   # lastik bandı: hafif gri, örgü çizgisi
            serit = .9 + .06 * np.sin(Z * 2 * math.pi / .004)
            beyaz = beyaz * (1 - ls[..., None] * (1 - serit[..., None] * .96))
            yeni = ten * w[..., None] + beyaz * (1 - w[..., None])
            out[bm] = yeni[bm]
            nout[bm] = (tn * w[..., None] + N * (1 - w[..., None]))[bm]
        # ayakkabı: beyaz üst, açık gri parçalar, kauçuk taban (alt yüz + taban kenarı)
        am = S == AYAKKABI
        r = _ayrinti(L, am, 6)
        koyu = _blur((L < .35).astype(np.float32), 1.0)
        ust = (np.array((.95, .95, .94)) * (1 - koyu[..., None]) + np.array((.70, .73, .78)) * koyu[..., None]) * r[..., None]
        taban = (Z < TABAN_PLAKA + .004) | (NZ < -.5)
        kenar = (Z < TABAN_PLAKA + .018) & ~taban
        kau = np.array((.62, .42, .26)) * r[..., None]
        ara = np.array((.96, .96, .95)) * r[..., None]
        ay = np.where(taban[..., None], kau, np.where(kenar[..., None], ara, ust))
        out[am] = ay[am]
        nout[am & (taban | kenar)] = (.5, .5, 1.)   # krampon dişleri normalde de silinsin
        # parlaklık (Unity Standard smoothness = 1 - pürüzlülük): ten kaynağın specular haritasından (kıllı ön kol mat,
        # düz ten hafif ıslak), polyester forma hafif parlak, şort mat, pamuk çorap en mat, ayakkabı derisi parlak.
        sp = _oku(src('body_specular'))[..., 0]
        P_ = np.clip(.18 + sp / .18 * .16, .16, .36)
        P_ = np.where(fm, .34, P_); P_ = np.where(sm, .24, P_)
        if bm.any(): P_ = np.where(bm, w * P_ + (1 - w) * .12, P_)
        P_ = np.where(am, np.where(taban, .22, np.where(kenar, .40, .48)), P_)
        P_ = _doldur(P_, cov)
        hs = _oku(src('head_specular'))[..., 0]
        HP = np.clip(.16 + hs / .25 * .30, .16, .62)   # T bölgesi/dudak ~.35, yanak ~.25, saç ~.22, göz ~.6
        _yaz(np.repeat(P_[..., None], 3, -1), yol['parlak']); _yaz(np.repeat(HP[..., None], 3, -1), yol['bas_parlak'])
        out = _doldur(out, cov); nout = _doldur(nout, cov)
        _yaz(out, yol['renk']); _yaz(nout, yol['normal'])
        for q in KUMASLAR: _yaz(_doldur(_kumas(out, fm, X, Y, Z, q), cov), yol['forma_' + q])
        np.save(os.path.join(KIT, 'sinif.npy'), S)
        open(ham, 'w').write(SURUM)
    return dict(renk=yol['renk'], normal=yol['normal'], parlak=yol['parlak'], bas_renk=src('head_color'), bas_normal=src('head_normal'),
                bas_parlak=yol['bas_parlak'], sinif=sinif)


def _kumas(out, fm, X, Y, Z, q):
    """forma piksellerine kumaş deseni (kaynak ölçeği, dinlenme pozu: yüz -y'ye bakar, gövde |x| < ~0,19 m, koltuk altı
    z ~1,42). Çarpan 1'in altında kalır: oyun dokuyu takım rengiyle çarpar, desen her renkte koyu-açık olarak okunur."""
    H, W = fm.shape; yy, xx = np.mgrid[0:H, 0:W]; k = np.ones((H, W), np.float32)
    ax = np.abs(X); govde = fm & (ax < .19)
    def yumusak(m, s=1.2): return np.clip(_blur(m.astype(np.float32), s), 0, 1)
    if q == 'hafif':
        yan = govde & (ax > .115) & (Z < 1.43) & (Z > .97)
        k *= 1 - .42 * yumusak(yan)
        delik = ((xx % 8) - 3.5) ** 2 + ((yy % 8) - 3.5) ** 2 < 2.6 ** 2
        k = np.where(fm, k * np.where(delik, .84, 1.), k)
    elif q == 'nefes':
        serit = govde & (((Z > 1.315) & (Z < 1.345)) | ((Z > 1.37) & (Z < 1.39)))
        k *= 1 - .45 * yumusak(serit)
        hava = govde & (Z > 1.0) & (Z < 1.29) & (ax < .09)
        delik = ((xx % 12) - 5.5) ** 2 + ((yy % 12) - 5.5) ** 2 < 3.2 ** 2
        k = np.where(hava & delik, k * .80, k)
    elif q == 'esnek':
        d = (X * .78 - (Z - 1.2) * .62)   # sağ omuzdan sol kalçaya kuşak (önde ve arkada aynı)
        kusak = govde & (np.abs(d) < .028)
        k *= 1 - .36 * yumusak(kusak, 2.0)
        kab = .965 + .035 * np.cos(2 * np.pi * (xx + yy) / 7.0)   # ince çapraz kaburga
        k = np.where(fm, k * kab, k)
    o2 = out.copy(); o2[fm] = out[fm] * k[fm, None]; return o2


def taban_duzle(o):
    """krampon çivileri: plaka altındaki köşeler çivi ucu düzlemine iner (kaynak ölçeği, dinlenme pozu)"""
    mw = o.matrix_world; inv = mw.inverted(); gi = {g.name: g.index for g in o.vertex_groups}
    ayak = {gi[n] for n in gi if any(n.endswith(k) for k in SOKET_AYAK)}; n = 0
    for v in o.data.vertices:
        if not any(g.group in ayak and g.weight > .3 for g in v.groups): continue
        p = mw @ v.co
        if p.z < TABAN_PLAKA: p.z = CIVI_UCU; v.co = inv @ p; n += 1
    o.data.update(); return n


def unity_cikti(hedef, olcu=1024):
    """Resources/karakter: rocketbox_govde.jpg / rocketbox_bas.jpg (renk), *_n.png (teğet uzayı normal, OpenGL +Y:
    Rocketbox kaynağı ve Unity aynı kural), *_mg.png (Standard _MetallicGlossMap: R metal=0, A smoothness)."""
    from PIL import Image
    src = lambda n: glob.glob(os.path.join(KAYNAK, 'Textures', f'*_{n}.tga'))[0]
    for ad, p, tur in ((('govde', os.path.join(KIT, 'rb_renk.png'), 'jpg'), ('bas', src('head_color'), 'jpg'),
                       ('govde_n', os.path.join(KIT, 'rb_normal.png'), 'png'), ('bas_n', src('head_normal'), 'png'),
                       ('govde_mg', os.path.join(KIT, 'rb_parlak.png'), 'mg'), ('bas_mg', os.path.join(KIT, 'rb_bas_parlak.png'), 'mg'))
                      + tuple(('forma_' + q, os.path.join(KIT, f'rb_forma_{q}.png'), 'jpg') for q in KUMASLAR)):
        im = Image.open(p).convert('RGB').resize((olcu, olcu), Image.LANCZOS)
        out = os.path.join(hedef, 'rocketbox_' + ad + ('.jpg' if tur == 'jpg' else '.png'))
        if tur == 'jpg': im.save(out, quality=92)
        elif tur == 'png': im.save(out, optimize=True)
        else:
            a = im.convert('L').resize((olcu // 2, olcu // 2), Image.LANCZOS); z = Image.new('L', a.size, 0)
            Image.merge('RGBA', (z, z, z, a)).save(out, optimize=True)   # yumuşak geçişli: 512 yeter
        print(out, os.path.getsize(out))


if __name__ == '__main__':
    a = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    bpy.ops.wm.read_homefile(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(KAYNAK, 'Export', 'Sports_Male_02.fbx'), automatic_bone_orientation=True)
    o = next(x for x in bpy.context.scene.objects if x.type == 'MESH'); dokular(o)
    if a: unity_cikti(a[0])
