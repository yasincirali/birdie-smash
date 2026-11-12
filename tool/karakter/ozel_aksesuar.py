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
# dışarı, +y bileğe doğru, birim = ön kol boyu × 1,3. Arka yüz z = 0'da (kola yaslanır), hiçbir parça z < 0'a inmez (kola gömülmez);
# yarıçap <= .7 (gövdeye ve rakete uzanmaz).
def plaka(ag, renk, nokta, z0, z1): levha(ag, renk, nokta, z1 - z0, lambda x, y: z1)
def cember(r, n=16, c=(0, 0), a0=0): return [(c[0] + r * math.cos(a0 + 2 * math.pi * i / n), c[1] + r * math.sin(a0 + 2 * math.pi * i / n)) for i in range(n)]

def kapak():   # tencere kapağı (Tencere şapkasının eşi)
    ag = Ag(); G, K = '#aeb6bf', '#3a3f47'
    plaka(ag, G, cember(.62, 18), 0, .05)
    for i in range(12):   # bombe: alçak koni kuşakları (z ekseni)
        a0 = 2 * math.pi * i / 12; a1 = 2 * math.pi * (i + 1) / 12; p = lambda R, z, a: (R * math.cos(a), R * math.sin(a), z)
        ag.dortgen('#c3cad2', p(.55, .05, a0), p(.55, .05, a1), p(.28, .13, a1), p(.28, .13, a0)); ag.ucgen('#c3cad2', p(.28, .13, a0), p(.28, .13, a1), (0, 0, .15))
    silindir(ag, K, (0, 0, .14), (0, 0, .25), .06, 8); silindir(ag, K, (0, 0, .25), (0, 0, .29), .11, 8)
    ag.yaz('kapak', 2)

def kalkan():   # şövalye kalkanı: sivri uç bileğe doğru (kol sarkınca aşağı bakar)
    ag = Ag(); A, M, B = '#e0b040', '#2f5fb8', '#f2f2f2'
    dis = [(-.5, -.62), (.5, -.62), (.52, -.1), (.36, .4), (0, .78), (-.36, .4), (-.52, -.1)]
    plaka(ag, A, dis, 0, .05); plaka(ag, M, [(x * .84, (y - .02) * .84) for x, y in dis], .05, .08)
    kutu(ag, B, (-.06, -.40, .08), (.06, .46, .10)); kutu(ag, B, (-.28, -.24, .08), (.28, -.12, .10))
    ag.yaz('kalkan', 2)

def dur():   # dur tabelası (yazısız: beyaz şerit)
    ag = Ag(); B, K = '#f2f2f2', '#d6342f'
    plaka(ag, B, cember(.62, 8, a0=math.pi / 8), 0, .04); plaka(ag, K, cember(.54, 8, a0=math.pi / 8), .04, .06)
    kutu(ag, B, (-.36, -.09, .06), (.36, .09, .08))
    ag.yaz('dur', 2)

def pizza():
    ag = Ag(); H, S, P, B = '#d9a25a', '#c8372d', '#f5cf5a', '#a82a22'
    plaka(ag, H, cember(.64, 18), 0, .06); plaka(ag, S, cember(.54, 18), .06, .075); plaka(ag, P, cember(.49, 18), .075, .085)
    for i, (r, a) in enumerate(((.0, 0), (.30, 20), (.30, 95), (.32, 165), (.28, 240), (.33, 305))):
        ar = math.radians(a); plaka(ag, B, cember(.085, 8, (r * math.cos(ar), r * math.sin(ar))), .085, .097)
    ag.yaz('pizza', 2)

def yildiz_kalkan():   # beş köşeli yıldız (içbükey: merkezden üçgenlenir)
    ag = Ag()
    def yildiz(renk, Rd, Ri, z0, z1):
        pt = [((Rd if i % 2 == 0 else Ri) * math.cos(math.pi / 2 + math.pi * i / 5), (Rd if i % 2 == 0 else Ri) * math.sin(math.pi / 2 + math.pi * i / 5)) for i in range(10)]
        for i in range(10):
            (x0, y0), (x1, y1) = pt[i], pt[(i + 1) % 10]
            ag.ucgen(renk, (0, 0, z1), (x0, y0, z1), (x1, y1, z1)); ag.ucgen(renk, (0, 0, z0), (x1, y1, z0), (x0, y0, z0))
            ag.dortgen(renk, (x0, y0, z1), (x0, y0, z0), (x1, y1, z0), (x1, y1, z1))
    yildiz('#6a4cff', .70, .34, 0, .06); yildiz('#ffd84a', .38, .18, .06, .09)
    ag.yaz('yildiz_kalkan', 2)

KALEM = {
    'tac': tac, 'hale': hale, 'seytan': seytan, 'kedi': kedi,
    'melek_kanat': lambda: kanat('melek_kanat', '#f6f3ea', KANAT, (('#dcd5c4', .70), ('#fffdf6', .42))),
    'yarasa_kanat': lambda: kanat('yarasa_kanat', '#3b2547', YARASA),
    'jetpack': jetpack,
    'tencere': tencere, 'mantar': mantar, 'ufo': ufo, 'ordek': ordek, 'kaktus': kaktus, 'top_sapka': top_sapka, 'pervane': pervane, 'simit': simit,
    'kapak': kapak, 'kalkan': kalkan, 'dur': dur, 'pizza': pizza, 'yildiz_kalkan': yildiz_kalkan,
    'kaplumbaga': kaplumbaga, 'kurma': kurma, 'balon': balon, 'top_tupu': top_tupu, 'tavus': tavus,
}

if __name__ == '__main__':
    for k in (sys.argv[1:] or KALEM): KALEM[k]()
