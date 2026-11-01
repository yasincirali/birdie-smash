# Kendi aksesuarlarımız (2026-11-01, yasin: "yaratıcılıkta sınır tanıma, skinler bizim için önemli"). Bedava KayKit
# paketlerinde taç, hale, boynuz, kedi kulağı, kanat ve jetpack yok (araştırma: iap_ve_skin_plani_2026-11.md §1); bunlar
# burada yordamsal, düşük çokgenli, düz renkli olarak üretilir ve kaykit_aksesuar.py ile AYNI biçimde
# Resources/karakter/aks/<id>.bytes'a yazılır: oyun tarafı (Aksesuar.Hazir) iki kaynağı ayırt etmez.
#
# Koordinatlar doğrudan Unity birim çerçevesinde (dönüşüm yok):
#   · BAŞ (tur 0): bant düzlemi y = 0, baş yarıçapı 1 (yan/üst/ön elipsoit oranları oyunda uygulanır), +z ön, tepe y ≈ 1.
#     KayKit şapkaları r107 turunda +.2 yukarıda oturdu; buradaki ölçüler o tura göre seçildi (taç tepeye, hale başın üstüne).
#   · SIRT (tur 1): birim = omuz eni, köken omuz ortası sırt yüzeyinde, +y yukarı, -z arkaya. altY > -.8 olduğu için kanat ve
#     jetpack sallanmaz (PelerinSalinim yalnız uzun pelerinde).
# Düz gölgeleme: her üçgen kendi normalini taşır (KayKit'in köşeli görünüşü). Python 3, bağımlılık yok.
# Kullanım: python3 tool/karakter/ozel_aksesuar.py [id ...]
import math, os, struct, sys

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'unity', 'BirdieSmash', 'Assets', 'Resources', 'karakter', 'aks')

def hex_rgb(h):
    h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))

class Ag:
    def __init__(s): s.alt = {}   # renk → üçgen listesi [(p0,p1,p2)]
    def ucgen(s, renk, a, b, c): s.alt.setdefault(renk, []).append((a, b, c))
    def dortgen(s, renk, a, b, c, d): s.ucgen(renk, a, b, c); s.ucgen(renk, a, c, d)
    def yaz(s, id_, tur):
        pos, nor, idx, gruplar = [], [], {}, []
        for renk, us in s.alt.items():
            ts = []
            for a, b, c in us:
                u = [b[i] - a[i] for i in range(3)]; v = [c[i] - a[i] for i in range(3)]
                n = (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0]); L = math.sqrt(sum(x * x for x in n)) or 1
                n = tuple(x / L for x in n); uc = []
                for p in (a, b, c):
                    key = (tuple(round(x, 5) for x in p), tuple(round(x, 2) for x in n))
                    if key not in idx: idx[key] = len(pos); pos.append(p); nor.append(n)
                    uc.append(idx[key])
                ts.append(uc)
            gruplar.append((hex_rgb(renk), ts))
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
        print(id_, 'köşe', len(pos), 'renk', len(gruplar), 'y', round(min(ys), 2), round(max(ys), 2))

# ---- ilkel biçimler (dışa bakan sarım: Unity sol el, saat yönü ön yüz; Coz iki yüzlü yaptığı için yön kritik değil) ----
def halka(ag, renk, R, r, y, n=20, m=8, c=(0, 0, 0)):
    """Torus: R ana yarıçap, r boru; yatay, merkez y'de."""
    def P(i, j):
        a = 2 * math.pi * i / n; b = 2 * math.pi * j / m; k = R + r * math.cos(b)
        return (c[0] + k * math.cos(a), c[1] + y + r * math.sin(b), c[2] + k * math.sin(a))
    for i in range(n):
        for j in range(m): ag.dortgen(renk, P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1))

def koni(ag, renk, taban, uc, r, n=8, kapak=True):
    """taban merkezinden uc noktasına koni; taban dairesi eksene dik."""
    ax = [uc[i] - taban[i] for i in range(3)]; L = math.sqrt(sum(x * x for x in ax)); ax = [x / L for x in ax]
    t = [1, 0, 0] if abs(ax[0]) < .9 else [0, 1, 0]
    e1 = [ax[1] * t[2] - ax[2] * t[1], ax[2] * t[0] - ax[0] * t[2], ax[0] * t[1] - ax[1] * t[0]]; l1 = math.sqrt(sum(x * x for x in e1)); e1 = [x / l1 for x in e1]
    e2 = [ax[1] * e1[2] - ax[2] * e1[1], ax[2] * e1[0] - ax[0] * e1[2], ax[0] * e1[1] - ax[1] * e1[0]]
    P = lambda i: tuple(taban[k] + r * (math.cos(2 * math.pi * i / n) * e1[k] + math.sin(2 * math.pi * i / n) * e2[k]) for k in range(3))
    for i in range(n):
        ag.ucgen(renk, P(i), P(i + 1), tuple(uc))
        if kapak: ag.ucgen(renk, P(i + 1), P(i), tuple(taban))

def silindir(ag, renk, alt, ust, r, n=10):
    koni(ag, renk, alt, ust, r, n, False)   # yan yüz için iki koni yerine düz şerit:
    ag.alt[renk] = ag.alt[renk][:-n]
    ax = [ust[i] - alt[i] for i in range(3)]; L = math.sqrt(sum(x * x for x in ax)); ax = [x / L for x in ax]
    t = [1, 0, 0] if abs(ax[0]) < .9 else [0, 1, 0]
    e1 = [ax[1] * t[2] - ax[2] * t[1], ax[2] * t[0] - ax[0] * t[2], ax[0] * t[1] - ax[1] * t[0]]; l1 = math.sqrt(sum(x * x for x in e1)); e1 = [x / l1 for x in e1]
    e2 = [ax[1] * e1[2] - ax[2] * e1[1], ax[2] * e1[0] - ax[0] * e1[2], ax[0] * e1[1] - ax[1] * e1[0]]
    P = lambda o, i: tuple(o[k] + r * (math.cos(2 * math.pi * i / n) * e1[k] + math.sin(2 * math.pi * i / n) * e2[k]) for k in range(3))
    for i in range(n):
        ag.dortgen(renk, P(alt, i), P(alt, i + 1), P(ust, i + 1), P(ust, i))
        ag.ucgen(renk, P(alt, i + 1), P(alt, i), tuple(alt)); ag.ucgen(renk, P(ust, i), P(ust, i + 1), tuple(ust))

def kutu(ag, renk, a, b):
    x0, y0, z0 = a; x1, y1, z1 = b
    v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    for q in ((0, 1, 2, 3), (5, 4, 7, 6), (4, 0, 3, 7), (1, 5, 6, 2), (3, 2, 6, 7), (4, 5, 1, 0)): ag.dortgen(renk, *(v[i] for i in q))

def levha(ag, renk, nokta, kalin, zf):
    """Düz çokgen (x, y) → kalınlıklı levha; z = zf(x, y) (kanadın arkaya kıvrımı). Yelpaze üçgenleme ilk noktadan."""
    on = [(x, y, zf(x, y)) for x, y in nokta]; ar = [(x, y, zf(x, y) - kalin) for x, y in nokta]; n = len(nokta)
    for i in range(1, n - 1): ag.ucgen(renk, on[0], on[i], on[i + 1]); ag.ucgen(renk, ar[0], ar[i + 1], ar[i])
    for i in range(n): j = (i + 1) % n; ag.dortgen(renk, on[i], ar[i], ar[j], on[j])

def ayna(nokta): return [(-x, y) for x, y in nokta]

# ---- kalemler ----
def tac():
    ag = Ag(); A, K, M = '#f5c542', '#d64545', '#3f7be0'
    n = 10; R0, R1, y0, y1 = .62, .58, .62, .86
    for i in range(n):
        a0 = 2 * math.pi * i / n; a1 = 2 * math.pi * (i + 1) / n
        p = lambda R, y, a: (R * math.cos(a), y, R * math.sin(a))
        ag.dortgen(A, p(R0, y0, a0), p(R0, y0, a1), p(R1, y1, a1), p(R1, y1, a0))
        ag.dortgen(A, p(R1 - .06, y1, a0), p(R1 - .06, y1, a1), p(R0 - .06, y0, a1), p(R0 - .06, y0, a0))
        ag.dortgen(A, p(R1, y1, a0), p(R1, y1, a1), p(R1 - .06, y1, a1), p(R1 - .06, y1, a0))
    for i in range(5):   # beş diş, biri tam önde
        a = math.pi / 2 + 2 * math.pi * i / 5; x, z = .58 * math.cos(a), .58 * math.sin(a)
        koni(ag, A, (x, .84, z), (x * 1.04, 1.14, z * 1.04), .12, 6)
        koni(ag, K if i % 2 == 0 else M, (x * 1.12, .70, z * 1.12), (x * 1.2, .74, z * 1.2), .07, 6)   # mücevher
    ag.yaz('tac', 0)

def hale():
    ag = Ag(); halka(ag, '#ffe27a', .52, .055, 1.38, 28, 8); ag.yaz('hale', 0)

def seytan():
    ag = Ag(); K = '#c8262f'
    for s in (-1, 1):   # iki kıvrık boynuz: üç parçalı koni zinciri
        p0 = (s * .42, .62, .18); p1 = (s * .55, .9, .14); p2 = (s * .52, 1.12, .02)
        silindir(ag, K, p0, p1, .13, 8); koni(ag, K, p1, p2, .13, 8)
    ag.yaz('seytan', 0)

def kedi():
    ag = Ag(); D, I = '#2b2b33', '#f2a1b5'
    for s in (-1, 1):
        x = s * .5; b = (x - s * .2, .72, .05); c = (x + s * .2, .72, -.05); u = (x + s * .08, 1.2, .02)
        on = .06
        ag.ucgen(D, b, c, u); ag.ucgen(D, (b[0], b[1], b[2] - on), (u[0], u[1], u[2] - on), (c[0], c[1], c[2] - on))
        ag.dortgen(D, b, (b[0], b[1], b[2] - on), (c[0], c[1], c[2] - on), c)
        ag.dortgen(D, c, (c[0], c[1], c[2] - on), (u[0], u[1], u[2] - on), u)
        ag.dortgen(D, u, (u[0], u[1], u[2] - on), (b[0], b[1], b[2] - on), b)
        k = .55; m = tuple((b[i] + c[i] + u[i]) / 3 for i in range(3)); sc = lambda p: tuple(m[i] + (p[i] - m[i]) * k for i in range(3))
        bb, cc, uu = sc(b), sc(c), sc(u); d = .012
        ag.ucgen(I, (bb[0], bb[1], bb[2] + d), (cc[0], cc[1], cc[2] + d), (uu[0], uu[1], uu[2] + d))
    ag.yaz('kedi', 0)

KANAT = [(.12, .05), (.45, .42), (.85, .78), (1.25, .95), (1.42, .72), (1.3, .38), (1.12, .12), (.88, -.08), (.62, -.2), (.35, -.18)]
YARASA = [(.12, .05), (.5, .5), (1.0, .85), (1.45, .82), (1.3, .45), (1.1, .5), (.98, .15), (.78, .22), (.6, -.08), (.4, .05), (.25, -.15)]

def kanat(id_, renk, nokta, ikinci=None):
    ag = Ag(); zf = lambda x, y: -.10 - .30 * abs(x) - .05 * max(0, y)
    for s in (-1, 1):
        nn = nokta if s > 0 else list(reversed(ayna(nokta)))
        levha(ag, renk, nn, .045, zf)
        if ikinci:   # tüy sırası: biraz içeride, öne yakın ikinci katman (koyu ton)
            sk = [(x * .78, y * .78 - .04) for x, y in nokta]; sk = sk if s > 0 else list(reversed(ayna(sk)))
            levha(ag, ikinci, sk, .03, lambda x, y: zf(x, y) + .035)
    kutu(ag, renk, (-.14, -.06, -.16), (.14, .16, -.06))   # iki kanadı bağlayan kök
    ag.yaz(id_, 1)

def jetpack():
    ag = Ag(); G, K, A, T = '#b9c2cc', '#4a5260', '#ff8a2a', '#ffd84a'
    kutu(ag, G, (-.3, -.42, -.36), (.3, .12, -.06))
    for s in (-1, 1):
        x = s * .2
        silindir(ag, K, (x, -.52, -.42), (x, .05, -.42), .11, 10)
        koni(ag, A, (x, -.52, -.42), (x, -.78, -.42), .1, 8)    # alev dış (altY > -.8: sallanmaz)
        koni(ag, T, (x, -.53, -.42), (x, -.68, -.42), .06, 8)    # alev çekirdek
    kutu(ag, K, (-.34, .02, -.38), (.34, .1, -.04))
    ag.yaz('jetpack', 1)

KALEM = {
    'tac': tac, 'hale': hale, 'seytan': seytan, 'kedi': kedi,
    'melek_kanat': lambda: kanat('melek_kanat', '#f6f3ea', KANAT, '#d9d4c6'),
    'yarasa_kanat': lambda: kanat('yarasa_kanat', '#3b2547', YARASA),
    'jetpack': jetpack,
}

if __name__ == '__main__':
    for k in (sys.argv[1:] or KALEM): KALEM[k]()
