# Kendi aksesuarlarımız (2026-11-01, yasin: "yaratıcılıkta sınır tanıma, skinler bizim için önemli"). Bedava KayKit
# paketlerinde taç, hale, boynuz, kedi kulağı, kanat ve jetpack yok (araştırma: iap_ve_skin_plani_2026-11.md §1); bunlar
# burada yordamsal, düşük çokgenli, düz renkli olarak üretilir ve kaykit_aksesuar.py ile AYNI biçimde
# Resources/karakter/aks/<id>.bytes'a yazılır: oyun tarafı (Aksesuar.Hazir) iki kaynağı ayırt etmez.
#
# Koordinatlar doğrudan Unity birim çerçevesinde (dönüşüm yok):
#   · BAŞ (tur 0): bant düzlemi y = 0, baş yarıçapı 1 (yan/üst/ön elipsoit oranları oyunda uygulanır), +z ön, tepe y ≈ 1.
#     KayKit şapkaları r107 turunda +.2 yukarıda oturdu. r112 (ekran turu): ilk ölçüler (taç bandı .62–.86, kulak/boynuz
#     tabanı .62–.72) saçın içinde kaldı; yakın planda yalnız taç dişlerinin ucu göründü, kedi kulağı hiç görünmedi. Saç
#     tepesi bu çerçevede ~1,1–1,25 (KayKit kabukları başı 1,4–1,6'ya kadar sarıyor). Şimdi taç saçın üstüne oturur
#     (.98–1.24, dişler 1.62), kulak ve boynuz tabanı ~.9'da saça gömülü, gövdesi saçın üstünde; hale 1.78'de. Vitrin
#     yakın planı aksesuar kökünün sınırını da kadraja alır (Vitrin.OdakPoz).
#   · KOL (tur 2, 2026-11-12): aşağıda "Kol" bölümünde.
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

def kesik(ag, renk, alt, ust, r0, r1, n=8):
    """Kesik koni (alt yarıçap r0, üst r1), kapaksız: boynuz gibi zincirlenir, eklemi bir sonraki parça örter."""
    ax = [ust[i] - alt[i] for i in range(3)]; L = math.sqrt(sum(x * x for x in ax)); ax = [x / L for x in ax]
    t = [1, 0, 0] if abs(ax[0]) < .9 else [0, 1, 0]
    e1 = [ax[1] * t[2] - ax[2] * t[1], ax[2] * t[0] - ax[0] * t[2], ax[0] * t[1] - ax[1] * t[0]]; l1 = math.sqrt(sum(x * x for x in e1)); e1 = [x / l1 for x in e1]
    e2 = [ax[1] * e1[2] - ax[2] * e1[1], ax[2] * e1[0] - ax[0] * e1[2], ax[0] * e1[1] - ax[1] * e1[0]]
    P = lambda o, r, i: tuple(o[k] + r * (math.cos(2 * math.pi * i / n) * e1[k] + math.sin(2 * math.pi * i / n) * e2[k]) for k in range(3))
    for i in range(n): ag.dortgen(renk, P(alt, r0, i), P(alt, r0, i + 1), P(ust, r1, i + 1), P(ust, r1, i))

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
    n = 12; R0, R1, y0, y1 = .86, .80, .98, 1.24
    for i in range(n):
        a0 = 2 * math.pi * i / n; a1 = 2 * math.pi * (i + 1) / n
        p = lambda R, y, a: (R * math.cos(a), y, R * math.sin(a))
        ag.dortgen(A, p(R0, y0, a0), p(R0, y0, a1), p(R1, y1, a1), p(R1, y1, a0))
        ag.dortgen(A, p(R1 - .06, y1, a0), p(R1 - .06, y1, a1), p(R0 - .06, y0, a1), p(R0 - .06, y0, a0))
        ag.dortgen(A, p(R1, y1, a0), p(R1, y1, a1), p(R1 - .06, y1, a1), p(R1 - .06, y1, a0))
    for i in range(5):   # beş diş, biri tam önde
        a = math.pi / 2 + 2 * math.pi * i / 5; x, z = .80 * math.cos(a), .80 * math.sin(a)
        koni(ag, A, (x, 1.22, z), (x * 1.06, 1.62, z * 1.06), .17, 6)
        koni(ag, A, (x * 1.06, 1.62, z * 1.06), (x * 1.06, 1.70, z * 1.06), .05, 6)   # diş ucunda top
        koni(ag, K if i % 2 == 0 else M, (x * 1.06, 1.08, z * 1.06), (x * 1.16, 1.12, z * 1.16), .09, 6)   # mücevher
    ag.yaz('tac', 0)

def hale():
    ag = Ag(); halka(ag, '#ffe27a', .60, .07, 1.78, 32, 8); ag.yaz('hale', 0)

def seytan():
    ag = Ag(); K = '#c8262f'
    for s in (-1, 1):   # iki kıvrık boynuz: üç parçalı koni zinciri
        y = [(s * .50, .90, .22), (s * .66, 1.18, .22), (s * .74, 1.40, .16), (s * .70, 1.58, .06), (s * .60, 1.70, -.04)]
        r = [.18, .15, .11, .07]
        for i in range(3): kesik(ag, K, y[i], y[i + 1], r[i], r[i + 1], 8)
        koni(ag, K, y[3], y[4], r[3], 8)
    ag.yaz('seytan', 0)

def kedi():
    ag = Ag(); D, I = '#2b2b33', '#f2a1b5'
    for s in (-1, 1):
        x = s * .58; b = (x - s * .28, .92, .08); c = (x + s * .26, .92, -.06); u = (x + s * .10, 1.62, .02)
        on = .06
        ag.ucgen(D, b, c, u); ag.ucgen(D, (b[0], b[1], b[2] - on), (u[0], u[1], u[2] - on), (c[0], c[1], c[2] - on))
        ag.dortgen(D, b, (b[0], b[1], b[2] - on), (c[0], c[1], c[2] - on), c)
        ag.dortgen(D, c, (c[0], c[1], c[2] - on), (u[0], u[1], u[2] - on), u)
        ag.dortgen(D, u, (u[0], u[1], u[2] - on), (b[0], b[1], b[2] - on), b)
        k = .55; m = tuple((b[i] + c[i] + u[i]) / 3 for i in range(3)); sc = lambda p: tuple(m[i] + (p[i] - m[i]) * k for i in range(3))
        bb, cc, uu = sc(b), sc(c), sc(u); d = .012
        ag.ucgen(I, (bb[0], bb[1], bb[2] + d), (cc[0], cc[1], cc[2] + d), (uu[0], uu[1], uu[2] + d))
    ag.yaz('kedi', 0)

# r112: düz kenarlı melek kanadı yakın planda beyaz bir leke gibi okundu. Arka kenar artık tüy uçları (uç/çentik sırası)
# ve iki iç katman (örtü tüyleri) taşır: siluet tek bakışta "kanat" der. Yelpaze üçgenleme kökten; çentikler köke daha
# yakın olduğu için çokgen kökten yıldız biçimli kalır.
KANAT = [(.12, .05), (.40, .42), (.80, .80), (1.22, 1.0), (1.48, .80), (1.40, .52), (1.30, .56), (1.30, .28), (1.17, .36),
         (1.14, .06), (1.0, .18), (.94, -.12), (.80, .04), (.72, -.24), (.58, -.06), (.48, -.24), (.36, -.08), (.26, -.14)]
YARASA = [(.12, .05), (.5, .5), (1.0, .85), (1.45, .82), (1.3, .45), (1.1, .5), (.98, .15), (.78, .22), (.6, -.08), (.4, .05), (.25, -.15)]

def kanat(id_, renk, nokta, katman=()):
    ag = Ag(); zf = lambda x, y: -.10 - .30 * abs(x) - .05 * max(0, y)
    for s in (-1, 1):
        nn = nokta if s > 0 else list(reversed(ayna(nokta)))
        levha(ag, renk, nn, .045, zf)
        for k, (kr, olcek) in enumerate(katman):   # örtü tüyleri: içe doğru küçülen, öne yakın katmanlar (ton farkı)
            sk = [(.12 + (x - .12) * olcek, .05 + (y - .05) * olcek + .04 * (k + 1)) for x, y in nokta]
            sk = sk if s > 0 else list(reversed(ayna(sk)))
            levha(ag, kr, sk, .03, lambda x, y, k=k: zf(x, y) + .03 * (k + 1))
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


# r154 (ekran turu): kısa saçın tepesi bu çerçevede ~.9 çıktı; ördek, saksı ve dev tüy top havada kaldı, mantar ve bere saçın
# üstünde tabak gibi durdu. Tabanlar .8–.85'e (saça gömülü), kubbeler alnın üstüne (.38–.6) indirildi.
# ---- 2026-11-12 absürt koleksiyon (yasin: "yaratıcı ol ... sadece görsellik ve absürtlük ama modelin üstüne güzel oturmalı,
# iç içe geçmiş olmamalı"). Oturma kuralı: şapkanın iç yüzü saç tepesinin (1,1–1,25) dışında kalır ya da taban saça gömülür
# (taban ~.9–1.0), gövdesi saçın üstündedir; yüz hizasına (y < .35) hiçbir parça inmez. Sırt parçaları sırt yüzeyinin
# arkasında (z <= -.02) durur, omuz enini (|x| <= .5) kolların salınım alanına taşırmaz, altY > -.8 (pelerin gibi sallanmaz).
def elipsoid(ag, renk, c, r, n=12, m=6, e0=-90, e1=90, eksen='y'):
    """Elipsoit kuşağı: enlem e0..e1 (derece; 0..90 = üst kubbe). renk: sabit ya da (i, j) → renk. eksen 'z-' = kutup arkaya."""
    def P(i, j):
        a = 2 * math.pi * i / n; e = math.radians(e0 + (e1 - e0) * j / m)
        x, y, z = math.cos(e) * math.cos(a), math.sin(e), math.cos(e) * math.sin(a)
        if eksen == 'z-': x, y, z = x, z, -y
        return (c[0] + r[0] * x, c[1] + r[1] * y, c[2] + r[2] * z)
    for i in range(n):
        for j in range(m):
            k = renk(i, j) if callable(renk) else renk
            if j == m - 1 and e1 >= 90: ag.ucgen(k, P(i, j), P(i + 1, j), P(i, m))
            elif j == 0 and e0 <= -90: ag.ucgen(k, P(i, 0), P(i + 1, 1), P(i, 1))
            else: ag.dortgen(k, P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1))

def top(ag, renk, c, r, n=8, m=5): elipsoid(ag, renk, c, (r, r, r), n, m)

def disk(ag, renk, c, r, n=12):
    for i in range(n):
        a0 = 2 * math.pi * i / n; a1 = 2 * math.pi * (i + 1) / n
        ag.ucgen(renk, c, (c[0] + r * math.cos(a0), c[1], c[2] + r * math.sin(a0)), (c[0] + r * math.cos(a1), c[1], c[2] + r * math.sin(a1)))

def halka_z(ag, renk, c, R, r, n=14, m=6):
    """Torus, ekseni z (xy düzleminde durur: arkadan bakınca halka)."""
    def P(i, j):
        a = 2 * math.pi * i / n; b = 2 * math.pi * j / m; k = R + r * math.cos(b)
        return (c[0] + k * math.cos(a), c[1] + k * math.sin(a), c[2] + r * math.sin(b))
    for i in range(n):
        for j in range(m): ag.dortgen(renk, P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1))

def tencere():   # ters çevrilmiş tencere: ağzı saçın dışında (iç yarıçap 1,1 > saç), kulplar yanda
    ag = Ag(); G, K = '#aeb6bf', '#3a3f47'
    kesik(ag, G, (0, .45, 0), (0, 1.42, 0), 1.16, 1.10, 16); disk(ag, G, (0, 1.42, 0), 1.10, 16)
    halka(ag, '#8d959e', 1.16, .06, .45, 20, 6)
    for s in (-1, 1): kutu(ag, K, (s * 1.30 - .16, .78, -.2), (s * 1.30 + .16, .9, .2))
    silindir(ag, K, (0, 1.42, 0), (0, 1.52, 0), .16, 8)   # dip düğmesi (kapak tutamağı gibi okunur)
    ag.yaz('tencere', 0)

def mantar():
    ag = Ag(); K, B, C = '#d6342f', '#fff6e6', '#f0e2c4'
    c = (0, .6, 0); r = (1.42, .86, 1.42)
    elipsoid(ag, K, c, r, 16, 6, 0, 90); disk(ag, C, c, 1.42, 16)
    for e, a, q in ((62, 20, .26), (30, 80, .22), (38, 160, .24), (26, 230, .2), (44, 300, .25), (80, 200, .2)):
        er, ar = math.radians(e), math.radians(a)
        p = (c[0] + r[0] * math.cos(er) * math.cos(ar), c[1] + r[1] * math.sin(er), c[2] + r[2] * math.cos(er) * math.sin(ar))
        elipsoid(ag, B, p, (q, q * .45, q), 8, 3)
    ag.yaz('mantar', 0)

def ufo():   # başın üstünde süzülen daire (hale gibi boşlukta; saça değmez: alt yüz 1,78)
    ag = Ag(); G, A, C, S = '#9aa7b8', '#5c6676', '#7de3c8', '#ffd84a'
    kesik(ag, A, (0, 1.78, 0), (0, 1.92, 0), .38, 1.18, 16); disk(ag, A, (0, 1.78, 0), .38, 16)
    kesik(ag, G, (0, 1.92, 0), (0, 2.04, 0), 1.18, .52, 16)
    elipsoid(ag, C, (0, 2.04, 0), (.52, .40, .52), 12, 4, 0, 90)
    for i in range(8):
        a = 2 * math.pi * i / 8; top(ag, S, (1.12 * math.cos(a), 1.90, 1.12 * math.sin(a)), .075, 6, 3)
    ag.yaz('ufo', 0)

def ordek():   # lastik ördek: gövdenin altı saça gömülü (taban .85), kendisi saçın üstünde oturur
    ag = Ag(); S, T, K, D = '#ffd23f', '#ff8a2a', '#1c1c22', '#f2b90f'
    elipsoid(ag, S, (0, 1.30, -.05), (.62, .45, .78), 12, 6)
    for sgn in (-1, 1): elipsoid(ag, D, (sgn * .58, 1.32, -.1), (.12, .24, .42), 8, 4)
    koni(ag, S, (0, 1.42, -.66), (0, 1.78, -.98), .22, 8)
    top(ag, S, (0, 1.82, .42), .37, 10, 6)
    koni(ag, T, (0, 1.78, .72), (0, 1.74, 1.08), .15, 8)
    for sgn in (-1, 1): top(ag, K, (sgn * .17, 1.90, .72), .06, 6, 3)
    ag.yaz('ordek', 0)

def kaktus():
    ag = Ag(); P, T, Y, C = '#c9683c', '#5a3a26', '#3f9b4f', '#ff7eb6'
    kesik(ag, P, (0, .80, 0), (0, 1.25, 0), .50, .64, 12); halka(ag, P, .64, .07, 1.25, 14, 6); disk(ag, T, (0, 1.24, 0), .60, 12)
    silindir(ag, Y, (0, 1.23, 0), (0, 2.15, 0), .30, 10); elipsoid(ag, Y, (0, 2.15, 0), (.30, .30, .30), 10, 4, 0, 90)
    for sgn, y0, y1, x in ((1, 1.67, 2.07, .64), (-1, 1.51, 1.83, .62)):
        silindir(ag, Y, (sgn * .25, y0, 0), (sgn * x, y0, 0), .14, 8); top(ag, Y, (sgn * x, y0, 0), .14, 8, 4)
        silindir(ag, Y, (sgn * x, y0, 0), (sgn * x, y1, 0), .14, 8); elipsoid(ag, Y, (sgn * x, y1, 0), (.14, .14, .14), 8, 3, 0, 90)
    top(ag, C, (0, 2.49, 0), .13, 8, 4)
    ag.yaz('kaktus', 0)

def top_sapka():   # dev tüy top: mantar başın tepesinde, tüy eteği yukarı açılır
    ag = Ag(); B, B2, K = '#ffffff', '#e4e8f2', '#d6342f'
    elipsoid(ag, B, (0, .80, 0), (.66, .42, .66), 12, 4, 0, 90)
    halka(ag, K, .56, .07, 1.15, 16, 6)
    n = 16; alt, ust = (0, 1.30, 0), (0, 2.30, 0)
    for i in range(n):
        a0 = 2 * math.pi * i / n; a1 = 2 * math.pi * (i + 1) / n; am = (a0 + a1) / 2
        p = lambda R, y, a: (R * math.cos(a), y, R * math.sin(a))
        ag.dortgen(B if i % 2 == 0 else B2, p(.50, 1.15, a0), p(.50, 1.15, a1), p(1.12, 2.07, a1), p(1.12, 2.07, a0))
        ag.ucgen(B if i % 2 == 0 else B2, p(1.12, 2.07, a0), p(1.12, 2.07, a1), p(1.20, 2.25, am))   # tüy ucu
    ag.yaz('top_sapka', 0)

def pervane():   # pervaneli bere: dilim dilim renkli kubbe (iç yüz saçın dışında), tepede pervane
    ag = Ag(); R = ['#d6342f', '#ffd23f', '#2f6fd6']; K = '#3a3f47'
    elipsoid(ag, lambda i, j: R[(i // 2) % 3], (0, .38, 0), (1.15, .92, 1.15), 12, 5, 0, 90)
    silindir(ag, K, (0, 1.28, 0), (0, 1.50, 0), .05, 6)
    kutu(ag, R[0], (-.62, 1.48, -.10), (-.04, 1.52, .10)); kutu(ag, R[2], (.04, 1.48, -.10), (.62, 1.52, .10))
    top(ag, R[1], (0, 1.54, 0), .09, 6, 4)
    ag.yaz('pervane', 0)

def simit():   # simit hale: susamlı
    ag = Ag(); S, T = '#b5651d', '#f3e3b3'
    halka(ag, S, .62, .16, 1.82, 20, 8)
    for i in range(20):
        a = 2 * math.pi * i / 20 + (.15 if i % 2 else 0); R = .62 + (.07 if i % 2 else -.06)
        elipsoid(ag, T, (R * math.cos(a), 1.97, R * math.sin(a)), (.035, .02, .035), 5, 2)
    ag.yaz('simit', 0)

def kaplumbaga():   # kabuk: sırt yüzeyinden arkaya kubbe, kenarda açık renkli çerçeve
    ag = Ag(); K = ['#2f6d3a', '#3f8f4a', '#58a85f', '#6dbb72']; C = '#e6d9a8'
    c = (0, -.20, -.02)
    elipsoid(ag, lambda i, j: K[min(3, j)], c, (.52, .52, .40), 12, 5, 0, 90, 'z-')
    elipsoid(ag, C, c, (.58, .58, .09), 12, 4, -90, 90, 'z-')
    ag.yaz('kaplumbaga', 1)

def kurma():   # kurmalı oyuncak anahtarı: mil sırttan arkaya, iki halka arkadan okunur
    ag = Ag(); A, K = '#e0b040', '#b88a22'
    silindir(ag, K, (0, -.28, -.02), (0, -.28, -.46), .06, 8)
    kutu(ag, A, (-.07, -.34, -.52), (.07, -.22, -.44))
    for s in (-1, 1): halka_z(ag, A, (s * .23, -.28, -.48), .17, .05, 14, 6)
    ag.yaz('kurma', 1)

def balon():   # üç balon: ipler omuz ortasındaki düğümden, balonlar başın arkasında ve üstünde (kafaya değmez: z <= -.3)
    ag = Ag(); IP = '#f4f1e8'
    for (x, y, z), renk in (((-.46, 1.52, -.62), '#e23b3b'), ((.06, 1.90, -.74), '#ffd23f'), ((.50, 1.46, -.58), '#3f7be0')):
        silindir(ag, IP, (0, -.06, -.10), (x, y - .40, z), .012, 4)
        elipsoid(ag, renk, (x, y, z), (.30, .36, .30), 10, 6); koni(ag, renk, (x, y - .34, z), (x, y - .42, z), .05, 6)
    top(ag, IP, (0, -.06, -.10), .04, 6, 3)
    ag.yaz('balon', 1)

def top_tupu():   # yedek top tüpü: sırtta çapraz, ağzından bir tüy top bakar
    ag = Ag(); T, B, K = '#1f6f8f', '#f2f2f2', '#d6342f'
    a, b = (-.30, -.58, -.17), (.34, .22, -.17); d = [b[i] - a[i] for i in range(3)]; L = math.sqrt(sum(x * x for x in d)); u = [x / L for x in d]
    q = lambda t: tuple(a[i] + u[i] * t for i in range(3))
    silindir(ag, T, q(.06), q(L - .06), .13, 10); silindir(ag, B, q(0), q(.06), .14, 10); silindir(ag, B, q(L - .06), q(L), .14, 10)
    silindir(ag, K, q(L * .45), q(L * .55), .135, 10)   # etiket bandı
    kesik(ag, B, q(L), q(L + .22), .06, .15, 10); elipsoid(ag, K, q(L), (.07, .07, .07), 6, 3)
    ag.yaz('top_tupu', 1)

def tavus():   # tavus kuyruğu: belden yukarı yelpaze, tüyler arkaya yatık (omuzların ve başın arkasında kalır)
    ag = Ag(); Y = ['#1f8a70', '#2aa58a']; G, M = '#e8c547', '#27408b'
    kok = (0, -.62); n = 9
    for i in range(n):
        ac = math.radians(-52 + 104 * i / (n - 1)); dx, dy = math.sin(ac), math.cos(ac); nx, ny = dy, -dx; Lf = 1.22 if i % 2 == 0 else 1.08
        zf = lambda x, y, k=i: -.08 - .30 * math.hypot(x - kok[0], y - kok[1]) - .012 * (k % 2)
        pt = lambda t, wv: (kok[0] + dx * t + nx * wv, kok[1] + dy * t + ny * wv)
        levha(ag, Y[i % 2], [pt(.05, 0), pt(Lf * .55, .10), pt(Lf * .88, .13), pt(Lf, 0), pt(Lf * .88, -.13), pt(Lf * .55, -.10)], .02, zf)
        cx, cy = pt(Lf * .84, 0)
        for rr, renk, dz in ((.085, G, -.024), (.045, M, -.03)):
            levha(ag, renk, [(cx + rr * math.cos(2 * math.pi * k / 8), cy + rr * math.sin(2 * math.pi * k / 8)) for k in range(8)], .004, lambda x, y, zf=zf, dz=dz: zf(x, y) + dz)
    kutu(ag, Y[0], (-.14, -.70, -.14), (.14, -.56, -.04))
    ag.yaz('tavus', 1)


# ---- Kol (tur 2, 2026-11-12): raketsiz ön kola kalkan. Çerçeve (Aksesuar.Kol): köken ön kolun ortasında kol yüzeyinde, +z koldan
# dışarı, +y bileğe doğru, birim = ön kol boyu × 1,7. Arka yüz z = 0'da; yalnız kayışlar z < 0'a iner (kola sarılı okunur).
# Yarıçap <= .6 (gövdeye ve rakete uzanmaz). Yerel oturma önizlemesi: gerçek FBX üstünde bpy ile (Aksesuar.Kol'un kopyası).
def plaka(ag, renk, nokta, z0, z1): levha(ag, renk, nokta, z1 - z0, lambda x, y: z1)
def cember(r, n=16, c=(0, 0), a0=0): return [(c[0] + r * math.cos(a0 + 2 * math.pi * i / n), c[1] + r * math.sin(a0 + 2 * math.pi * i / n)) for i in range(n)]

def bombe(ag, renk, nokta, z0, zk, zt, halka=3, arka=True):
    """Bombeli levha (merkezden yıldız-dışbükey çokgen): arka yüz z0'da düz, kenar z0..zk, ön yüz kenarda zk, merkezde zt (parabol).
    2026-11-12 (yasin: "kalkan güzel gözükmüyor"): düz plaka kâğıt gibi okunuyordu; kalkanlar bombeli, çerçeveli ve kayışlı."""
    n = len(nokta); z = lambda t: zk + (zt - zk) * (1 - t * t)
    for h in range(halka):
        t0, t1 = 1 - h / halka, 1 - (h + 1) / halka
        for i in range(n):
            (x0, y0), (x1, y1) = nokta[i], nokta[(i + 1) % n]
            if t1 > 1e-6: ag.dortgen(renk, (x0 * t0, y0 * t0, z(t0)), (x1 * t0, y1 * t0, z(t0)), (x1 * t1, y1 * t1, z(t1)), (x0 * t1, y0 * t1, z(t1)))
            else: ag.ucgen(renk, (x0 * t0, y0 * t0, z(t0)), (x1 * t0, y1 * t0, z(t0)), (0, 0, zt))
    for i in range(n):
        (x0, y0), (x1, y1) = nokta[i], nokta[(i + 1) % n]
        ag.dortgen(renk, (x0, y0, zk), (x0, y0, z0), (x1, y1, z0), (x1, y1, zk))
        if arka: ag.ucgen(renk, (0, 0, z0), (x1, y1, z0), (x0, y0, z0))

def olc(nokta, k): return [(x * k, y * k) for x, y in nokta]
def bombe_z(zk, zt, k): return zk + (zt - zk) * (1 - k * k)   # bombenin k ölçekli halkadaki yüksekliği (üst katman buraya oturur)

def kayis(ag, y0=-.26, y1=.22, w=.2):   # arkada iki kol kayışı: kalkan kola bağlı okunur (z < 0 kola sarılı)
    for y in (y0, y1): kutu(ag, '#4a3323', (-w, y - .05, -.07), (w, y + .05, .0))

def kapak():   # tencere kapağı (Tencere şapkasının eşi): bombeli çelik, kenar bandı, siyah topuz
    ag = Ag(); G, A, K = '#c3cad2', '#8f98a3', '#2c3037'
    bombe(ag, A, cember(.56, 20), 0, .04, .05); bombe(ag, G, cember(.50, 20), .03, .055, .19, 4, False)
    silindir(ag, K, (0, 0, .18), (0, 0, .27), .05, 8); elipsoid(ag, K, (0, 0, .29), (.10, .10, .045), 10, 4)
    kayis(ag); ag.yaz('kapak', 2)

def kalkan():   # şövalye kalkanı: sivri uç bileğe doğru (kol sarkınca aşağı bakar); altın çerçeve, bombeli mavi alan, beyaz haç
    ag = Ag(); A, M, B = '#e0b040', '#2f5fb8', '#f2f2f2'
    dis = [(-.44, -.56), (0, -.60), (.44, -.56), (.47, -.08), (.33, .36), (0, .70), (-.33, .36), (-.47, -.08)]
    bombe(ag, A, dis, 0, .05, .07); ic = olc(dis, .86); bombe(ag, M, ic, .04, .075, .17, 4, False)
    hac = [(-.055, -.38), (.055, -.38), (.055, -.20), (.24, -.20), (.24, -.09), (.055, -.09), (.055, .42), (-.055, .42), (-.055, -.09), (-.24, -.09), (-.24, -.20), (-.055, -.20)]
    hac = [(x, y + .04) for x, y in hac]; bombe(ag, B, hac, .10, .135, .185, 3, False)
    for x, y in ((-.36, -.44), (.36, -.44), (0, .56)): top(ag, '#b98a22', (x, y, .07), .035, 6, 4)   # perçinler
    kayis(ag); ag.yaz('kalkan', 2)

def dur():   # dur tabelası (yazısız: beyaz şerit); beyaz çerçeve, hafif bombeli kırmızı yüz
    ag = Ag(); B, K = '#f2f2f2', '#d6342f'
    sek = cember(.56, 8, a0=math.pi / 8)
    bombe(ag, B, sek, 0, .05, .06); bombe(ag, K, olc(sek, .88), .04, .07, .12, 3, False)
    bombe(ag, B, [(-.32, -.085), (.32, -.085), (.32, .085), (-.32, .085)], .09, .125, .135, 2, False)
    kayis(ag); ag.yaz('dur', 2)

def pizza():   # kabarık kenar (simit gibi halka), sos, peynir, biberli dilimler
    ag = Ag(); H, S, P, B, Y = '#d9a25a', '#c8372d', '#f5cf5a', '#a82a22', '#5f8f3a'
    bombe(ag, H, cember(.50, 20), 0, .04, .05); halka_z(ag, H, (0, 0, .06), .50, .07, 20, 6)
    bombe(ag, S, cember(.45, 20), .04, .07, .09, 2, False); bombe(ag, P, cember(.40, 20), .06, .085, .11, 3, False)
    for r, a in ((.0, 0), (.25, 20), (.25, 95), (.27, 165), (.23, 240), (.27, 305)):
        ar = math.radians(a); c = (r * math.cos(ar), r * math.sin(ar)); zz = bombe_z(.085, .11, r / .40)
        elipsoid(ag, B, (c[0], c[1], zz), (.075, .075, .02), 8, 3)
    for r, a in ((.14, 60), (.16, 200), (.33, 130), (.12, 310)):
        ar = math.radians(a); elipsoid(ag, Y, (r * math.cos(ar), r * math.sin(ar), bombe_z(.085, .11, r / .40)), (.035, .02, .015), 6, 3)
    kayis(ag); ag.yaz('pizza', 2)

def yildiz_kalkan():   # kabarık beş köşeli yıldız: mor gövde, sarı bombeli iç yıldız, ortada parlak göbek
    ag = Ag()
    def yildiz(Rd, Ri): return [((Rd if i % 2 == 0 else Ri) * math.cos(math.pi / 2 + math.pi * i / 5), (Rd if i % 2 == 0 else Ri) * math.sin(math.pi / 2 + math.pi * i / 5)) for i in range(10)]
    bombe(ag, '#6a4cff', yildiz(.64, .33), 0, .05, .16, 3); bombe(ag, '#ffd84a', yildiz(.40, .20), .10, bombe_z(.05, .16, .62) + .01, .21, 3, False)
    elipsoid(ag, '#fff3b0', (0, 0, .21), (.07, .07, .035), 8, 3)
    kayis(ag, -.16, .14, .16); ag.yaz('yildiz_kalkan', 2)

# ---- Yılbaşı koleksiyonu (2026-11-12, yasin: "Yılbaşı odaklı git, sonrasında da konsept konsept ilerle") ----
KIR, YES, BEY, ALT = '#d6342f', '#2f8f4a', '#fbfbf6', '#f2c230'

def boru(ag, renk, yol, n=12):
    """Eğri boru: yol = [((x, y, z), r), ...]; halkalar yol teğetine dik, çerçeve halkadan halkaya taşınır (ek yerinde boşluk kalmaz)."""
    def nrm(v): L = math.sqrt(sum(x * x for x in v)) or 1; return [x / L for x in v]
    def cap(u, v): return [u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0]]
    m = len(yol); hal = []; e1 = None
    for k, (p, r) in enumerate(yol):
        t = nrm([yol[min(m - 1, k + 1)][0][i] - yol[max(0, k - 1)][0][i] for i in range(3)])
        if e1 is None: e1 = nrm(cap(t, [0, 0, 1] if abs(t[2]) < .9 else [1, 0, 0]))
        else: d = sum(e1[i] * t[i] for i in range(3)); e1 = nrm([e1[i] - t[i] * d for i in range(3)])
        e2 = cap(t, e1)
        hal.append([tuple(p[i] + r * (math.cos(2 * math.pi * j / n) * e1[i] + math.sin(2 * math.pi * j / n) * e2[i]) for i in range(3)) for j in range(n)])
    for k in range(m - 1):
        for j in range(n): ag.dortgen(renk, hal[k][j], hal[k][(j + 1) % n], hal[k + 1][(j + 1) % n], hal[k + 1][j])

def noel_sapka():   # Noel Baba şapkası: kürk bant saçın dışında (iç 1,05), külah yana devrik, ucunda ponpon
    ag = Ag()
    halka(ag, BEY, 1.17, .17, .18, 20, 8)   # bant alın hizasında (bant çizgisi y = 0'ın hemen üstü): şapka başa geçmiş okunur, saçın üstünde havada durmaz
    boru(ag, KIR, [((0, .18, 0), 1.13), ((.02, .70, -.02), 1.04), ((.10, 1.12, -.05), .82), ((.30, 1.46, -.09), .58), ((.62, 1.66, -.13), .40),
                   ((.98, 1.64, -.17), .27), ((1.26, 1.46, -.20), .17), ((1.40, 1.24, -.22), .10)], 14)
    top(ag, BEY, (1.46, 1.10, -.23), .20, 10, 6)
    ag.yaz('noel_sapka', 0)

def geyik():   # ren geyiği: saç bandı (kulaklık yayı gibi kulaktan kulağa), banttan çıkan iki dallı boynuz, bantta kulaklar, çıngırak
    ag = Ag(); K, A = '#7a4a26', '#9a6a3c'
    yay = lambda x: -.25 + 1.25 * math.sqrt(max(0, 1 - (x / .99) ** 2))
    boru(ag, K, [((.99 * math.cos(math.pi * k / 16), -.25 + 1.25 * math.sin(math.pi * k / 16), -.05), .06) for k in range(3, 14)], 6)
    for sgn in (-1, 1):
        x = sgn * .50; y0 = yay(.50)
        boru(ag, K, [((x, y0, -.05), .11), ((sgn * .66, y0 + .50, -.05), .09), ((sgn * .64, y0 + 1.06, -.05), .06), ((sgn * .62, y0 + 1.16, -.05), .03)], 8)
        for (t, dx, dy, r) in ((.36, .34, .26, .07), (.66, -.30, .24, .06), (.86, .28, .20, .05)):
            a = (sgn * (.50 + .16 * min(1, t / .5)), y0 + t, -.05); u = (a[0] + sgn * dx, a[1] + dy, -.05)
            boru(ag, K, [(a, r), (u, r * .7), ((u[0] + sgn * dx * .15, u[1] + dy * .2, -.05), r * .3)], 6)
        ex = sgn * .80; ey = yay(.80)
        elipsoid(ag, A, (ex + sgn * .20, ey + .06, -.05), (.24, .13, .10), 8, 4); elipsoid(ag, '#e9b7a0', (ex + sgn * .21, ey + .06, .01), (.16, .08, .07), 8, 3)
    top(ag, ALT, (-.36, yay(.5) + .40, .10), .10, 8, 4); kutu(ag, '#8a6a10', (-.40, yay(.5) + .36, .17), (-.32, yay(.5) + .38, .21))
    ag.yaz('geyik', 0)

def cam_agaci():   # başta mini çam: saksı saça gömülü (taban .82), üç kat, renkli toplar, tepe yıldızı
    ag = Ag(); T = '#6b4226'
    kesik(ag, KIR, (0, .82, 0), (0, 1.12, 0), .34, .42, 10); disk(ag, T, (0, 1.11, 0), .40, 10)
    silindir(ag, T, (0, 1.10, 0), (0, 1.36, 0), .10, 8)
    for i, (y0, y1, r) in enumerate(((1.30, 1.92, .78), (1.66, 2.20, .60), (1.98, 2.48, .42))):
        koni(ag, YES if i % 2 == 0 else '#3aa65a', (0, y0, 0), (0, y1, 0), r, 12)
        for k in range(5):
            a = 2 * math.pi * k / 5 + i * .6; rr = r * .80; top(ag, (KIR, ALT, '#3f7be0')[(k + i) % 3], (rr * math.cos(a), y0 + (y1 - y0) * .16, rr * math.sin(a)), .075, 6, 4)
    yl = [((.20 if k % 2 == 0 else .09) * math.cos(math.pi / 2 + math.pi * k / 5), 2.60 + (.20 if k % 2 == 0 else .09) * math.sin(math.pi / 2 + math.pi * k / 5)) for k in range(10)]
    levha(ag, ALT, [(0, 2.60)] + yl + [yl[0]], .08, lambda x, y: .04)
    ag.yaz('cam_agaci', 0)

def hediye_sapka():   # başta hediye paketi: kutu saç tepesine oturur (taban .78), kurdele ve fiyonk
    ag = Ag(); K, S = '#2f6fd6', ALT
    kutu(ag, K, (-.62, .78, -.62), (.62, 1.72, .62)); kutu(ag, '#2a62be', (-.67, 1.66, -.67), (.67, 1.86, .67))
    kutu(ag, S, (-.11, .77, -.635), (.11, 1.87, .635)); kutu(ag, S, (-.635, .77, -.11), (.635, 1.87, .11)); kutu(ag, S, (-.11, 1.85, -.68), (.11, 1.875, .68)); kutu(ag, S, (-.68, 1.85, -.11), (.68, 1.875, .11))
    for sgn in (-1, 1):
        elipsoid(ag, S, (sgn * .30, 2.02, 0), (.30, .16, .13), 8, 4); koni(ag, S, (sgn * .16, 1.90, 0), (sgn * .46, 1.80, .30), .07, 6)
    top(ag, '#d9a81c', (0, 1.98, 0), .11, 8, 4)
    ag.yaz('hediye_sapka', 0)

def isik_tac():   # yılbaşı ışıkları: başın üstünde süzülen kablo halkası, renkli ampuller (hale gibi boşlukta, saça değmez)
    ag = Ag(); R = [KIR, ALT, '#3fb56a', '#3f7be0', '#e06ad0']
    halka(ag, '#1f3d2a', .80, .03, 1.86, 24, 5)
    for i in range(10):
        a = 2 * math.pi * i / 10; x, z = .80 * math.cos(a), .80 * math.sin(a)
        kutu(ag, '#2b2b2b', (x - .04, 1.76, z - .04), (x + .04, 1.84, z + .04)); elipsoid(ag, R[i % 5], (x, 1.66, z), (.085, .13, .085), 8, 4)
    ag.yaz('isik_tac', 0)

def kardan_sapka():   # kardan adam: başın tepesinde küçük kardan adam (gövde saça gömülü), silindir şapka, havuç burun, atkı
    ag = Ag(); K = '#22252b'
    top(ag, BEY, (0, 1.12, 0), .48, 12, 6); top(ag, BEY, (0, 1.78, 0), .34, 12, 6)
    halka(ag, KIR, .30, .075, 1.50, 14, 6); kutu(ag, KIR, (.20, 1.20, .22), (.34, 1.50, .30))
    koni(ag, '#ff8a2a', (0, 1.78, .30), (0, 1.76, .62), .06, 6)
    for sgn in (-1, 1): top(ag, K, (sgn * .13, 1.88, .29), .04, 6, 3)
    for y in (1.02, 1.20): top(ag, K, (0, y, .46 if y > 1.1 else .45), .045, 6, 3)
    disk(ag, K, (0, 2.06, 0), .36, 12); silindir(ag, K, (0, 2.04, 0), (0, 2.08, 0), .36, 12); silindir(ag, K, (0, 2.08, 0), (0, 2.38, 0), .23, 12); silindir(ag, KIR, (0, 2.09, 0), (0, 2.15, 0), .24, 12)
    for sgn in (-1, 1): kesik(ag, '#6b4226', (sgn * .40, 1.22, 0), (sgn * .84, 1.50, 0), .03, .02, 5)
    ag.yaz('kardan_sapka', 0)

def hediye_cuvali():   # Noel Baba çuvalı: sırtta dolu çuval, ağzı büzgülü, hediyeler taşıyor
    ag = Ag(); C, C2 = '#b3272b', '#8f1f24'
    elipsoid(ag, C, (0, -.30, -.36), (.50, .52, .36), 12, 6); kesik(ag, C, (0, .10, -.36), (0, .28, -.36), .26, .16, 10)
    halka(ag, ALT, .17, .035, .26, 12, 5, (0, 0, -.36)); kesik(ag, C2, (0, .28, -.36), (0, .40, -.36), .16, .24, 10)
    kutu(ag, '#2f6fd6', (-.20, .30, -.50), (.02, .52, -.28)); kutu(ag, ALT, (-.11, .29, -.51), (-.07, .53, -.27))
    kutu(ag, YES, (.02, .30, -.44), (.20, .46, -.26)); kutu(ag, BEY, (.09, .29, -.45), (.13, .47, -.25))
    koni(ag, BEY, (-.02, .40, -.24), (-.06, .66, -.20), .05, 6)   # şeker baston ucu yerine külah
    for sgn in (-1, 1): silindir(ag, '#6b4226', (sgn * .30, .10, -.12), (sgn * .22, .16, -.34), .025, 5)
    ag.yaz('hediye_cuvali', 1)

def seker_baston():   # sırtta çapraz iki dev şeker baston: kırmızı beyaz halkalı, kıvrık uç omuz üstünde
    ag = Ag()
    for sgn in (-1, 1):
        a, b = (sgn * -.34, -.66, -.16 - .05 * (sgn > 0)), (sgn * .30, .34, -.16 - .05 * (sgn > 0)); n = 8
        for k in range(n):
            p = lambda t: tuple(a[i] + (b[i] - a[i]) * t for i in range(3))
            silindir(ag, KIR if k % 2 == 0 else BEY, p(k / n), p((k + 1) / n), .075, 8)
        ust = b
        for k in range(6):   # kıvrım: yarım çember dışa
            t0, t1 = math.pi * k / 6, math.pi * (k + 1) / 6; R = .16
            q = lambda t: (ust[0] + sgn * (R - R * math.cos(t)), ust[1] + R * math.sin(t) * 1.0, ust[2])
            silindir(ag, BEY if k % 2 == 0 else KIR, q(t0), q(t1), .075, 8)
    kutu(ag, YES, (-.12, -.26, -.30), (.12, -.10, -.10)); top(ag, ALT, (0, -.18, -.31), .06, 6, 3)
    ag.yaz('seker_baston', 1)

def havai_fisek():   # sırtta üç havai fişek roketi: çubuklar belden, başlıklar omuz üstünde, fitiller
    ag = Ag(); T = '#c9a26a'
    for (x, yb, renk, serit) in ((-.30, .26, KIR, ALT), (0, .44, '#3f7be0', BEY), (.30, .26, YES, ALT)):
        silindir(ag, T, (x, -.66, -.16), (x, yb - .30, -.16), .025, 5)
        silindir(ag, renk, (x, yb - .34, -.18), (x, yb + .12, -.18), .11, 10); silindir(ag, serit, (x, yb - .16, -.18), (x, yb - .08, -.18), .115, 10)
        koni(ag, serit, (x, yb + .12, -.18), (x, yb + .38, -.18), .15, 10)
        silindir(ag, '#3a3f47', (x + .06, yb - .34, -.18), (x + .14, yb - .46, -.20), .012, 4); top(ag, '#ffb02e', (x + .15, yb - .47, -.20), .03, 5, 3)
    kutu(ag, '#4a3323', (-.42, -.34, -.24), (.42, -.24, -.10))
    ag.yaz('havai_fisek', 1)

def cam_sirt():   # sırtta süslü çam: saksı belde, ağaç başın arkasında yükselir (z <= -.2: enseye değmez)
    ag = Ag(); T = '#6b4226'; z = -.30
    kesik(ag, KIR, (0, -.66, z), (0, -.40, z), .16, .22, 10); silindir(ag, T, (0, -.42, z), (0, -.20, z), .06, 6)
    for i, (y0, y1, r) in enumerate(((-.24, .30, .46), (.08, .60, .36), (.40, .88, .25))):
        koni(ag, YES if i % 2 == 0 else '#3aa65a', (0, y0, z), (0, y1, z), r, 12)
        for k in range(5):
            a = 2 * math.pi * k / 5 + i * .7; rr = r * .78; top(ag, (KIR, ALT, '#3f7be0')[(k + i) % 3], (rr * math.cos(a), y0 + (y1 - y0) * .18, z + rr * math.sin(a)), .05, 6, 3)
    yl = [((.13 if k % 2 == 0 else .06) * math.cos(math.pi / 2 + math.pi * k / 5), .96 + (.13 if k % 2 == 0 else .06) * math.sin(math.pi / 2 + math.pi * k / 5)) for k in range(10)]
    levha(ag, ALT, [(0, .96)] + yl + [yl[0]], .05, lambda x, y: z + .025)
    for sgn in (-1, 1): silindir(ag, '#4a3323', (sgn * .26, .02, -.06), (sgn * .12, -.44, z + .10), .022, 5)
    ag.yaz('cam_sirt', 1)

def kurabiye():   # zencefilli kurabiye adam: kabarık gövde, şeker kremalı kenar süsü, düğmeler, gülen yüz
    ag = Ag(); K, S = '#b5713a', BEY
    def elips(c, rx, ry, renk=K, z0=0, zk=.05, zt=.10):
        ic = Ag(); bombe(ic, renk, [(rx * math.cos(2 * math.pi * k / 14), ry * math.sin(2 * math.pi * k / 14)) for k in range(14)], z0, zk, zt, 2)
        for r_, us in ic.alt.items():
            for t in us: ag.ucgen(r_, *[(q[0] + c[0], q[1] + c[1], q[2]) for q in t])
    elips((0, -.02), .24, .30, zt=.12); elips((0, -.44), .21, .20, zt=.12)                      # gövde, baş (baş dirsek tarafında: kol sarkınca yukarı)
    for sgn in (-1, 1):
        elips((sgn * .34, -.14), .18, .10); elips((sgn * .17, .40), .11, .22)                    # kollar, bacaklar
        top(ag, '#3a2a1c', (sgn * .08, -.48, .12), .028, 6, 3)
        elips((sgn * .43, -.14), .035, .09, S, .08, .10, .11); elips((sgn * .17, .56), .09, .03, S, .08, .10, .11)
    for y, renk in ((-.10, KIR), (.04, YES), (.18, KIR)): top(ag, renk, (0, y, .125), .04, 6, 3)
    for k in range(5):
        a = math.radians(200 + 35 * k); top(ag, S, (.10 * math.cos(a), -.42 - .10 * math.sin(a) * -1 - .10, .125), .018, 5, 2)
    kayis(ag, -.20, .16, .16); ag.yaz('kurabiye', 2)

def kar_tanesi():   # kar tanesi kalkanı: buz mavisi bombeli disk üstünde altı kollu beyaz kristal
    ag = Ag(); M, B = '#8fd3f4', BEY
    bombe(ag, '#e8f6ff', cember(.56, 18), 0, .04, .05); bombe(ag, M, cember(.50, 18), .03, .06, .15, 3, False)
    for k in range(6):
        a = math.pi / 2 + math.pi * k / 3; c, s_ = math.cos(a), math.sin(a)
        def cubuk(t0, t1, w, ac=0.0):
            ca, sa = math.cos(a + ac), math.sin(a + ac); bx, by = c * t0, s_ * t0
            pts = [(bx - sa * w, by + ca * w), (bx + sa * w, by - ca * w), (bx + ca * (t1 - t0) + sa * w, by + sa * (t1 - t0) - ca * w), (bx + ca * (t1 - t0) - sa * w, by + sa * (t1 - t0) + ca * w)]
            zz = bombe_z(.06, .15, min(1, (t0 + t1) / 2 / .50)) + .012
            for r_, pp in ((B, pts),): 
                on = [(x, y, zz + .02) for x, y in pp]; ar = [(x, y, zz - .03) for x, y in pp]
                ag.dortgen(r_, on[0], on[1], on[2], on[3])
                for i in range(4): j = (i + 1) % 4; ag.dortgen(r_, on[i], ar[i], ar[j], on[j])
        cubuk(.04, .44, .028); cubuk(.24, .40, .022, .8); cubuk(.24, .40, .022, -.8)
    elipsoid(ag, B, (0, 0, .165), (.07, .07, .03), 8, 3)
    kayis(ag); ag.yaz('kar_tanesi', 2)

def celenk():   # yılbaşı çelengi: yeşil halka, kırmızı meyveler, altta büyük fiyonk; ortası boş (kol görünür)
    ag = Ag()
    halka_z(ag, YES, (0, 0, .09), .44, .13, 20, 7)
    for i in range(20):
        a = 2 * math.pi * i / 20; rr = .44 + (.10 if i % 2 else -.09); elipsoid(ag, '#3aa65a', (rr * math.cos(a), rr * math.sin(a), .13), (.07, .07, .06), 6, 3)
    for i in range(9):
        a = 2 * math.pi * i / 9 + .3; top(ag, KIR if i % 3 else ALT, (.44 * math.cos(a), .44 * math.sin(a), .22), .045, 6, 3)
    for sgn in (-1, 1):
        elipsoid(ag, KIR, (sgn * .17, .44, .24), (.17, .10, .06), 8, 4)
        bombe(ag, KIR, [(sgn * .02, .48), (sgn * .16, .74), (sgn * .04, .70), (sgn * .0, .50)] if sgn > 0 else [(-.0, .50), (-.04, .70), (-.16, .74), (-.02, .48)], .18, .22, .22, 1)
    top(ag, '#b3272b', (0, .44, .25), .07, 8, 4)
    kutu(ag, '#4a3323', (-.34, -.05, -.07), (.34, .05, .03)); ag.yaz('celenk', 2)

def noel_sakal():   # Noel Baba sakalı (yüz, tur 0): çene hattı boyunca kabarık toplar + bıyık. Yüz yüzeyi birim başta ölçüldü (aks_giy.py YUZ_OLC):
    # orta hat z 1,07 (dudak) → .98 (çene y −1,2); x .3'te ≈.90; x .5'te .84 → .70; x .7'de .68 → .45. Toplar yüzeyin hemen dışında durur.
    ag = Ag()
    for sgn in (-1, 1):
        for (x, y, z, r) in ((.74, -.92, .62, .19), (.58, -1.10, .76, .22), (.34, -1.26, .92, .23), (.32, -1.50, .90, .20)): elipsoid(ag, BEY, (sgn * x, y, z), (r, r, r * .8), 8, 5)
        elipsoid(ag, BEY, (sgn * .22, -.97, 1.11), (.22, .09, .08), 8, 4)
    for (y, z, r) in ((-1.34, 1.02, .25), (-1.62, .98, .21), (-1.84, .93, .14)): elipsoid(ag, BEY, (0, y, z), (r, r, r * .8), 8, 5)
    ag.yaz('noel_sakal', 0)

def kirmizi_burun():   # ren geyiği burnu: burun ucunu (y ≈ −.72, z 1,08) içine alan parlak kırmızı top
    ag = Ag(); top(ag, '#e3262c', (0, -.72, 1.12), .17, 12, 7); top(ag, '#ff8f8f', (-.06, -.65, 1.255), .035, 6, 3)
    ag.yaz('kirmizi_burun', 0)

KALEM = {
    'tac': tac, 'hale': hale, 'seytan': seytan, 'kedi': kedi,
    'melek_kanat': lambda: kanat('melek_kanat', '#f6f3ea', KANAT, (('#dcd5c4', .70), ('#fffdf6', .42))),
    'yarasa_kanat': lambda: kanat('yarasa_kanat', '#3b2547', YARASA),
    'jetpack': jetpack,
    'tencere': tencere, 'mantar': mantar, 'ufo': ufo, 'ordek': ordek, 'kaktus': kaktus, 'top_sapka': top_sapka, 'pervane': pervane, 'simit': simit,
    'kapak': kapak, 'kalkan': kalkan, 'dur': dur, 'pizza': pizza, 'yildiz_kalkan': yildiz_kalkan,
    'kaplumbaga': kaplumbaga, 'kurma': kurma, 'balon': balon, 'top_tupu': top_tupu, 'tavus': tavus,
    'noel_sapka': noel_sapka, 'geyik': geyik, 'cam_agaci': cam_agaci, 'hediye_sapka': hediye_sapka, 'isik_tac': isik_tac, 'kardan_sapka': kardan_sapka,
    'hediye_cuvali': hediye_cuvali, 'seker_baston': seker_baston, 'havai_fisek': havai_fisek, 'cam_sirt': cam_sirt,
    'kurabiye': kurabiye, 'kar_tanesi': kar_tanesi, 'celenk': celenk, 'noel_sakal': noel_sakal, 'kirmizi_burun': kirmizi_burun,
}

if __name__ == '__main__':
    for k in (sys.argv[1:] or KALEM): KALEM[k]()
