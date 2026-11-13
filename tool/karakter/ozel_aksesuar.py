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

RENK_TAVAN, UCGEN_TAVAN = 4, 600
DETAY = 1.0
class Fazla(Exception): pass
def D(n, en_az=5): return max(en_az, int(round(n * DETAY)))   # bölüntü sayısı DETAY ile ölçeklenir

class Ag:
    def __init__(s): s.alt = {}   # renk → üçgen listesi [(p0,p1,p2)]
    def ucgen(s, renk, a, b, c): s.alt.setdefault(renk, []).append((a, b, c))
    def dortgen(s, renk, a, b, c, d): s.ucgen(renk, a, b, c); s.ucgen(renk, a, c, d)
    def yaz(s, id_, tur):
        # Tasarım dili bütçesi (2026-11-12, yasin: "aşırı detaylı render sorunu yaratacak şeyler olmamalı"): renk başına bir alt ağ = bir çizim
        # çağrısı, bu yüzden en çok RENK_TAVAN renk (en yakın iki ton birleşir: vurgu renkleri kalır) ve en çok UCGEN_TAVAN üçgen. Aşan kalem
        # daha düşük DETAY ile yeniden kurulur (calistir); oyuncak dili zaten iri ve az yüzlü.
        while len(s.alt) > RENK_TAVAN:
            ks = list(s.alt); en = None
            for i in range(len(ks)):
                for j in range(i + 1, len(ks)):
                    d = sum((a - b) ** 2 for a, b in zip(hex_rgb(ks[i]), hex_rgb(ks[j])))
                    if en is None or d < en[0]: en = (d, ks[i], ks[j])
            a, b = (en[1], en[2]) if len(s.alt[en[1]]) >= len(s.alt[en[2]]) else (en[2], en[1])
            s.alt[a] += s.alt.pop(b)
        if sum(len(u) for u in s.alt.values()) > UCGEN_TAVAN: raise Fazla(sum(len(u) for u in s.alt.values()))
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
        print(id_, 'detay', DETAY, 'üçgen', sum(len(t) for _, t in gruplar), 'köşe', len(pos), 'renk', len(gruplar), 'y', round(min(ys), 2), round(max(ys), 2))

# ---- ilkel biçimler (dışa bakan sarım: Unity sol el, saat yönü ön yüz; Coz iki yüzlü yaptığı için yön kritik değil) ----
def halka(ag, renk, R, r, y, n=20, m=8, c=(0, 0, 0)):
    """Torus: R ana yarıçap, r boru; yatay, merkez y'de."""
    n, m = D(n, 8), D(m, 4)
    def P(i, j):
        a = 2 * math.pi * i / n; b = 2 * math.pi * j / m; k = R + r * math.cos(b)
        return (c[0] + k * math.cos(a), c[1] + y + r * math.sin(b), c[2] + k * math.sin(a))
    for i in range(n):
        for j in range(m): ag.dortgen(renk, P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1))

def koni(ag, renk, taban, uc, r, n=8, kapak=True):
    """taban merkezinden uc noktasına koni; taban dairesi eksene dik."""
    n = D(n, 4)
    ax = [uc[i] - taban[i] for i in range(3)]; L = math.sqrt(sum(x * x for x in ax)); ax = [x / L for x in ax]
    t = [1, 0, 0] if abs(ax[0]) < .9 else [0, 1, 0]
    e1 = [ax[1] * t[2] - ax[2] * t[1], ax[2] * t[0] - ax[0] * t[2], ax[0] * t[1] - ax[1] * t[0]]; l1 = math.sqrt(sum(x * x for x in e1)); e1 = [x / l1 for x in e1]
    e2 = [ax[1] * e1[2] - ax[2] * e1[1], ax[2] * e1[0] - ax[0] * e1[2], ax[0] * e1[1] - ax[1] * e1[0]]
    P = lambda i: tuple(taban[k] + r * (math.cos(2 * math.pi * i / n) * e1[k] + math.sin(2 * math.pi * i / n) * e2[k]) for k in range(3))
    for i in range(n):
        ag.ucgen(renk, P(i), P(i + 1), tuple(uc))
        if kapak: ag.ucgen(renk, P(i + 1), P(i), tuple(taban))

def silindir(ag, renk, alt, ust, r, n=10):
    n = D(n, 5)
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
    n = D(n, 5)
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
    if not callable(renk): n, m = D(n, 6), D(m, 3)
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
    n = D(n, 6)
    for i in range(n):
        a0 = 2 * math.pi * i / n; a1 = 2 * math.pi * (i + 1) / n
        ag.ucgen(renk, c, (c[0] + r * math.cos(a0), c[1], c[2] + r * math.sin(a0)), (c[0] + r * math.cos(a1), c[1], c[2] + r * math.sin(a1)))

def halka_z(ag, renk, c, R, r, n=14, m=6):
    """Torus, ekseni z (xy düzleminde durur: arkadan bakınca halka)."""
    n, m = D(n, 8), D(m, 4)
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
            levha(ag, renk, [(cx + rr * math.cos(2 * math.pi * k / 6), cy + rr * math.sin(2 * math.pi * k / 6)) for k in range(6)], .004, lambda x, y, zf=zf, dz=dz: zf(x, y) + dz)
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
    bombe(ag, H, cember(.50, 14), 0, .04, .05, 1); halka_z(ag, H, (0, 0, .06), .50, .07, 14, 5)
    bombe(ag, S, cember(.45, 14), .04, .07, .09, 1, False); bombe(ag, P, cember(.40, 14), .06, .085, .11, 2, False)
    for r, a in ((.0, 0), (.25, 20), (.25, 95), (.27, 165), (.23, 240), (.27, 305)):
        ar = math.radians(a); c = (r * math.cos(ar), r * math.sin(ar)); zz = bombe_z(.085, .11, r / .40)
        elipsoid(ag, B, (c[0], c[1], zz), (.085, .085, .022), 8, 3)
    for r, a in ((.14, 60), (.16, 200), (.33, 130), (.12, 310)):
        ar = math.radians(a); elipsoid(ag, Y, (r * math.cos(ar), r * math.sin(ar), bombe_z(.085, .11, r / .40)), (.05, .028, .018), 6, 3)
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
    n = D(n, 4)
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
        a, b = (sgn * -.34, -.66, -.16 - .05 * (sgn > 0)), (sgn * .30, .34, -.16 - .05 * (sgn > 0)); n = 6
        for k in range(n):
            p = lambda t: tuple(a[i] + (b[i] - a[i]) * t for i in range(3))
            kesik(ag, KIR if k % 2 == 0 else BEY, p(k / n), p((k + 1) / n), .075, .075, 7)
        ust = b
        for k in range(6):   # kıvrım: yarım çember dışa
            t0, t1 = math.pi * k / 6, math.pi * (k + 1) / 6; R = .16
            q = lambda t: (ust[0] + sgn * (R - R * math.cos(t)), ust[1] + R * math.sin(t) * 1.0, ust[2])
            kesik(ag, BEY if k % 2 == 0 else KIR, q(t0), q(t1), .075, .075, 7)
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
        ic = Ag(); bombe(ic, renk, [(rx * math.cos(2 * math.pi * k / 9), ry * math.sin(2 * math.pi * k / 9)) for k in range(9)], z0, zk, zt, 2 if rx > .2 else 1, renk == K)
        for r_, us in ic.alt.items():
            for t in us: ag.ucgen(r_, *[(q[0] + c[0], q[1] + c[1], q[2]) for q in t])
    elips((0, -.02), .24, .30, zt=.12); elips((0, -.44), .21, .20, zt=.12)                      # gövde, baş (baş dirsek tarafında: kol sarkınca yukarı)
    for sgn in (-1, 1):
        elips((sgn * .34, -.14), .18, .10); elips((sgn * .17, .40), .11, .22)                    # kollar, bacaklar
        top(ag, '#3a2a1c', (sgn * .08, -.48, .12), .028, 6, 3)
        elips((sgn * .43, -.14), .035, .09, S, .08, .10, .11); elips((sgn * .17, .56), .09, .03, S, .08, .10, .11)
    for y, renk in ((-.10, KIR), (.04, YES), (.18, KIR)): top(ag, renk, (0, y, .125), .04, 6, 3)
    for k in range(3):
        a = math.radians(215 + 55 * k); top(ag, S, (.10 * math.cos(a), -.42 - .10 * math.sin(a) * -1 - .10, .125), .024, 5, 3)
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
    halka_z(ag, YES, (0, 0, .09), .44, .13, 14, 6)
    for i in range(10):   # 2026-11-12 tasarım dili: 20 küçük yaprak + 9 meyve yerine 10 iri yaprak + 6 iri meyve
        a = 2 * math.pi * i / 10; rr = .44 + (.11 if i % 2 else -.10); elipsoid(ag, lambda i_, j_: '#3aa65a', (rr * math.cos(a), rr * math.sin(a), .13), (.10, .10, .07), 6, 2)
    for i in range(6):
        a = 2 * math.pi * i / 6 + .3; top(ag, KIR if i % 2 else ALT, (.44 * math.cos(a), .44 * math.sin(a), .22), .06, 6, 3)
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

# ---- Bel (tur 3, 2026-11-12): leğen kemiğine bağlı eşyalar (etek, kemer, kuyruk, simit). Çerçeve (Aksesuar.Bel): köken iki kalça ekleminin
# ortasının .25 birim üstü, +y yukarı, +z öne, birim = kalça eklemleri arası. Gövde bu çerçevede yarı genişlik ≈ .75, öne ≈ .65 (göbek), arkaya
# ≈ .45; dört oyuncak tipinde ölçüldü (aks_giy.py). Bacaklar çömelince yana açılır: uzun etek bacağa girer, bu yüzden parçalar kısa ve geniş
# (y >= -.35) tutulur. Elips halkalar merkezden z = +.1 öne kayık (göbek öne taşar).
def elips_halka(ag, renk, rx, rz, r, y, cz=.10, n=24, m=8):
    n, m = D(n, 10), D(m, 4)
    def P(i, j):
        a = 2 * math.pi * i / n; b = 2 * math.pi * j / m; k = 1 + r * math.cos(b) / ((rx + rz) / 2)
        return (rx * k * math.cos(a), y + r * math.sin(b), cz + rz * k * math.sin(a))
    for i in range(n):
        for j in range(m): ag.dortgen(renk, P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1))

def elips_kusak(ag, renk, rx0, rz0, y0, rx1, rz1, y1, cz=.10, n=24, kat=None):
    """İki elips arasında şerit (etek, kemer bandı). kat: (i) → renk (dilimli etek)."""
    P = lambda rx, rz, y, i: (rx * math.cos(2 * math.pi * i / n), y, cz + rz * math.sin(2 * math.pi * i / n))
    for i in range(n): ag.dortgen(kat(i) if kat else renk, P(rx0, rz0, y0, i), P(rx0, rz0, y0, i + 1), P(rx1, rz1, y1, i + 1), P(rx1, rz1, y1, i))

def noel_kemer():   # Noel Baba kemeri: siyah kemer, altın toka, altında kısa kırmızı etek ve beyaz kürk şerit
    ag = Ag(); S = '#22252b'
    elips_kusak(ag, S, .86, .74, -.02, .86, .74, .16)
    elips_kusak(ag, KIR, .87, .75, -.02, 1.06, .96, -.26); elips_halka(ag, BEY, 1.06, .96, .075, -.27, .10, 24, 6)
    kutu(ag, ALT, (-.15, -.06, .82), (.15, .20, .88)); kutu(ag, S, (-.08, .0, .875), (.08, .14, .89))
    ag.yaz('noel_kemer', 3)

def simit_bel():   # şişme ördekli deniz simidi: belde gevşek duran sarı halka, önde ördek başı
    ag = Ag(); S, T, K = '#ffd23f', '#ff8a2a', '#1c1c22'
    elips_halka(ag, S, 1.10, .98, .24, .02, .10, 24, 10)
    top(ag, S, (0, .34, 1.30), .24, 10, 6); kesik(ag, S, (0, .12, 1.22), (0, .30, 1.28), .15, .13, 8)
    koni(ag, T, (0, .32, 1.50), (0, .30, 1.72), .10, 8)
    for sgn in (-1, 1): top(ag, K, (sgn * .11, .40, 1.48), .04, 6, 3)
    koni(ag, S, (0, .05, -1.02), (0, .28, -1.26), .14, 8)   # kuyruk
    ag.yaz('simit_bel', 3)

def tutu():   # bale tütüsü: üç kat kabarık tül, belde saten kuşak
    ag = Ag(); R = ['#ff8fc0', '#ffb3d6', '#ffd0e6']
    elips_kusak(ag, '#e0508f', .85, .73, -.02, .85, .73, .12)
    for k, (y, rr) in enumerate(((-.02, 1.38), (-.08, 1.28), (-.14, 1.16))):
        n = 24; P = lambda r, yy, i: (r * math.cos(2 * math.pi * i / n), yy, .10 + r * .92 * math.sin(2 * math.pi * i / n))
        for i in range(n):
            dal = .05 if i % 2 == 0 else -.05; dal2 = -.05 if i % 2 == 0 else .05
            ag.dortgen(R[k], P(.84, .02 - k * .02, i), P(.84, .02 - k * .02, i + 1), P(rr, y + dal2, i + 1), P(rr, y + dal, i))
    ag.yaz('tutu', 3)

def seytan_kuyruk():   # şeytan kuyruğu: kuyruk sokumundan arkaya ve yukarı kıvrılan ince kuyruk, ucunda ok
    ag = Ag(); K = '#c2242b'
    boru(ag, K, [((0, -.10, -.42), .07), ((0, -.24, -.70), .065), ((0, -.20, -1.00), .06), ((0, .02, -1.22), .055), ((0, .32, -1.30), .05), ((0, .58, -1.20), .045), ((0, .70, -1.04), .04)], 8)
    koni(ag, K, (0, .66, -1.10), (0, .86, -.82), .13, 4)
    ag.yaz('seytan_kuyruk', 3)

# ---- Cadılar Bayramı koleksiyonu (2026-11-12, yasin: "Halloween olabilir ... konsept konsept ilerle") ----
TUR, MOR, SYH = '#f08a1c', '#6a3fb0', '#1d1b22'

def balkabagi():   # balkabağı kafa: başı tümden örten oyma kabak (iç 1,35 > saç), dilimli kabuk, ışıklı gözler ve ağız, sap
    ag = Ag(); c = (0, -.12, 0); r = (1.58, 1.36, 1.58); I = '#ffe27a'
    elipsoid(ag, lambda i, j: TUR if i % 2 == 0 else '#e07a12', c, r, 20, 8)
    zf = lambda x, y: c[2] + r[2] * math.sqrt(max(0, 1 - (x / r[0]) ** 2 - ((y - c[1]) / r[1]) ** 2)) + .025
    for sgn in (-1, 1): levha(ag, I, [(sgn * .22, .10), (sgn * .72, .16), (sgn * .46, .52)] if sgn > 0 else [(-.22, .10), (-.46, .52), (-.72, .16)], .03, zf)
    levha(ag, I, [(-.10, -.10), (.10, -.10), (0, .10)], .03, zf)
    agiz = [(-.70, -.34), (-.48, -.46), (-.34, -.36), (-.16, -.52), (0, -.42), (.16, -.52), (.34, -.36), (.48, -.46), (.70, -.34), (.52, -.72), (.26, -.86), (0, -.90), (-.26, -.86), (-.52, -.72)]
    for i in range(1, len(agiz) - 1):
        a, b_, d = (0, -.62), agiz[i], agiz[(i + 1) % len(agiz)]
    for i in range(len(agiz)):
        b_, d = agiz[i], agiz[(i + 1) % len(agiz)]; ag.ucgen(I, (0, -.62, zf(0, -.62)), (b_[0], b_[1], zf(*b_)), (d[0], d[1], zf(*d)))
    boru(ag, '#4f7a2a', [((0, 1.20, 0), .16), ((.04, 1.44, 0), .12), ((.16, 1.62, .02), .09)], 8); disk(ag, '#4f7a2a', (.16, 1.62, .02), .09, 8)
    ag.yaz('balkabagi', 0)

def hayalet():   # hayalet çarşafı: başı ve omuzları örten beyaz örtü, dalgalı etek, siyah göz ve ağız delikleri
    ag = Ag(); B = '#f4f4f0'
    elipsoid(ag, B, (0, .05, 0), (1.34, 1.24, 1.34), 20, 6, 0, 90)
    n = 20; P = lambda rr, y, i: (rr * math.cos(2 * math.pi * i / n), y, rr * math.sin(2 * math.pi * i / n))
    for (y0, r0, y1, r1) in ((.05, 1.34, -.80, 1.50), (-.80, 1.50, -1.50, 1.78)):
        for i in range(n): ag.dortgen(B, P(r0, y0, i), P(r0, y0, i + 1), P(r1, y1, i + 1), P(r1, y1, i))
    for i in range(n):
        am = 2 * math.pi * (i + .5) / n; ag.ucgen(B, P(1.78, -1.50, i), P(1.78, -1.50, i + 1), (1.86 * math.cos(am), -1.80 if i % 2 == 0 else -1.62, 1.86 * math.sin(am)))
    zf = lambda x, y: math.sqrt(max(0, (1.34 + (.05 - y) * .19 if y < .05 else 1.34 * math.sqrt(max(0, 1 - ((y - .05) / 1.24) ** 2))) ** 2 - x * x)) + .02
    def oval(cx, cy, rx, ry): levha(ag, SYH, [(cx + rx * math.cos(2 * math.pi * k / 10), cy + ry * math.sin(2 * math.pi * k / 10)) for k in range(10)], .03, zf)
    oval(-.36, -.22, .17, .26); oval(.36, -.22, .17, .26); oval(0, -.72, .15, .20)
    ag.yaz('hayalet', 0)

def orumcek():   # başın tepesinde oturan iri örümcek: gövde saçın üstünde, sekiz bacak saçın dışından aşağı sarkar
    ag = Ag(); K = '#2a2430'
    elipsoid(ag, K, (0, 1.14, -.20), (.44, .34, .52), 10, 6); top(ag, K, (0, 1.12, .34), .26, 8, 5)
    elipsoid(ag, MOR, (0, 1.40, -.24), (.20, .08, .26), 8, 3)
    for sgn in (-1, 1):
        for k, zz in enumerate((.30, .08, -.16, -.40)):
            boru(ag, K, [((sgn * .30, 1.12, zz), .06), ((sgn * .82, 1.46, zz * 1.5), .05), ((sgn * 1.20, 1.02, zz * 1.9), .045), ((sgn * 1.26, .52, zz * 2.0), .03)], 6)
        top(ag, '#e3262c', (sgn * .10, 1.20, .57), .045, 6, 3); top(ag, '#e3262c', (sgn * .20, 1.12, .52), .03, 5, 3)
    ag.yaz('orumcek', 0)

def vampir_dis():   # vampir dişleri (yüz): üst dudağın altından sarkan iki sivri diş (dudak üstü y −.9 z 1,07; ağız ≈ y −1,02)
    ag = Ag()
    for sgn in (-1, 1): koni(ag, BEY, (sgn * .13, -.97, 1.11), (sgn * .13, -1.14, 1.15), .055, 6)   # 2026-11-12 oyun içi kare (r158): yüz yüzeyi yerel ölçümden ~.07 önde, dişler dudağın içinde kalıyordu
    ag.yaz('vampir_dis', 0)

def mumya():   # KATALOGDA DEĞİL (2026-11-12 önizleme: düz elips bantlar yanağa giriyor, üstte havada kalıyor; yüzeye izdüşümle yeniden çizilecek). Mumya sargısı (yüz): başı saran eğik sargı bantları, gözler açıkta, serbest uç yanda sarkar
    ag = Ag(); S = ['#e9e2cf', '#d8cfb6']
    def sargi(y, eg, w, renk, rx=1.20, rz=1.24, a0=0, a1=360):
        n = 28; e = math.radians(eg)
        def P(i, dy):
            a = math.radians(a0 + (a1 - a0) * i / n); x, z = rx * math.cos(a), rz * math.sin(a)
            return (x, y + dy + x * math.sin(e), z + .06)
        for i in range(n): ag.dortgen(renk, P(i, -w), P(i + 1, -w), P(i + 1, w), P(i, w))
    # Yarıçaplar başın o yükseklikteki kesitine göre (aks_giy.py YUZ_OLC): alın ≈ 1,0; yanak hizası yan .78 ön 1,08 (burun); çene yan .6 ön .98
    sargi(.60, 8, .12, S[0], .90, .94); sargi(.34, -10, .12, S[1], 1.03, 1.06); sargi(.08, 7, .11, S[0], 1.07, 1.10); sargi(-.74, -6, .12, S[1], .84, 1.03)
    sargi(-1.00, 9, .11, S[0], .74, .97); sargi(-1.26, -5, .10, S[1], .62, .90)
    levha(ag, S[0], [(.80, -.80), (.92, -.86), (1.00, -1.56), (.86, -1.62)], .02, lambda x, y: .12)
    ag.yaz('mumya', 0)

def supurge():   # cadı süpürgesi: sırtta çapraz, çalı ucu aşağıda, bağ ipi
    ag = Ag(); T, C = '#8a5a2b', '#d9b45a'
    a, b_ = (.40, .70, -.18), (-.24, -.34, -.18); d = [b_[i] - a[i] for i in range(3)]; L = math.sqrt(sum(x * x for x in d)); u = [x / L for x in d]
    q = lambda t: tuple(a[i] + u[i] * t for i in range(3))
    silindir(ag, T, q(-.10), q(L), .035, 6); kesik(ag, C, q(L - .02), q(L + .34), .07, .20, 10); disk(ag, '#c49f48', q(L + .34), .20, 10)
    silindir(ag, '#5a3a1a', q(L + .02), q(L + .08), .085, 10)
    ag.yaz('supurge', 1)

def mezar_tasi():   # KATALOGDA DEĞİL (2026-11-12 tasarım dili: ölüm çağrışımı sevimli oyuncak diline uymuyor). mezar taşı: sırtta kemerli taş levha, çatlak ve yosun; yazı yok
    ag = Ag(); G, G2, Y = '#9aa0a6', '#7c838a', '#5f8f3a'
    ust = [(.34 * math.cos(math.pi * k / 8), .34 + .30 * math.sin(math.pi * k / 8)) for k in range(9)]
    levha(ag, G, [(-.34, -.66), (.34, -.66)] + ust, .14, lambda x, y: -.14)
    levha(ag, G2, [(-.24, -.50), (.24, -.50)] + [(x * .72, .30 + (y - .34) * .72) for x, y in ust], .02, lambda x, y: -.28 + .0)
    levha(ag, '#3a3f47', [(-.03, .10), (.03, .12), (.06, -.10), (-.02, -.30), (-.06, -.28), (.0, -.10)], .01, lambda x, y: -.30)
    for (x, y, r) in ((-.26, -.60, .10), (.20, -.62, .12), (.0, -.66, .09)): elipsoid(ag, Y, (x, y, -.26), (r, r * .6, r * .7), 6, 3)
    ag.yaz('mezar_tasi', 1)

def kabak_kalkan():   # balkabağı kalkanı: bombeli turuncu yüz, oyma gözler ve dişli ağız, tepede sap
    ag = Ag(); I = '#ffe27a'
    bombe(ag, '#c96a10', cember(.56, 18), 0, .05, .07); bombe(ag, TUR, cember(.50, 18), .04, .07, .20, 4, False)
    zz = lambda x, y: bombe_z(.07, .20, min(1, math.hypot(x, y) / .50)) + .012
    def yama(pts):
        cx = sum(p[0] for p in pts) / len(pts); cy = sum(p[1] for p in pts) / len(pts)
        for i in range(len(pts)): a, b_ = pts[i], pts[(i + 1) % len(pts)]; ag.ucgen(I, (cx, cy, zz(cx, cy)), (a[0], a[1], zz(*a)), (b_[0], b_[1], zz(*b_)))
    for sgn in (-1, 1): yama([(sgn * .08, -.16), (sgn * .28, -.20), (sgn * .18, -.02)] if sgn > 0 else [(-.08, -.16), (-.18, -.02), (-.28, -.20)])
    yama([(-.28, .10), (-.18, .16), (-.10, .10), (0, .18), (.10, .10), (.18, .16), (.28, .10), (.18, .30), (0, .34), (-.18, .30)])
    silindir(ag, '#4f7a2a', (0, -.50, .08), (0, -.64, .10), .05, 6)
    kayis(ag); ag.yaz('kabak_kalkan', 2)

def ag_kalkan():   # örümcek ağı kalkanı: koyu disk üstünde beyaz ağ, köşede küçük örümcek
    ag = Ag(); W = '#f1eef6'
    bombe(ag, MOR, cember(.56, 16), 0, .05, .07); bombe(ag, SYH, cember(.50, 16), .04, .07, .16, 3, False)
    zz = lambda x, y: bombe_z(.07, .16, min(1, math.hypot(x, y) / .50)) + .012
    def tel(p, q, w=.012):
        dx, dy = q[0] - p[0], q[1] - p[1]; L = math.hypot(dx, dy) or 1; nx, ny = -dy / L * w, dx / L * w
        ag.dortgen(W, (p[0] - nx, p[1] - ny, zz(*p)), (p[0] + nx, p[1] + ny, zz(*p)), (q[0] + nx, q[1] + ny, zz(*q)), (q[0] - nx, q[1] - ny, zz(*q)))
    for k in range(8):
        a = 2 * math.pi * k / 8; a2 = 2 * math.pi * (k + 1) / 8
        for t in range(4): tel((.12 * t * math.cos(a), .12 * t * math.sin(a)), (.12 * (t + 1) * math.cos(a), .12 * (t + 1) * math.sin(a)))
        for rr in (.16, .30, .44): tel((rr * math.cos(a), rr * math.sin(a)), (rr * .93 * math.cos((a + a2) / 2), rr * .93 * math.sin((a + a2) / 2))); tel((rr * .93 * math.cos((a + a2) / 2), rr * .93 * math.sin((a + a2) / 2)), (rr * math.cos(a2), rr * math.sin(a2)))
    elipsoid(ag, '#e3262c', (.24, -.20, zz(.24, -.20) + .03), (.06, .07, .04), 6, 3)
    for k in range(6): a = math.pi * k / 3; tel((.24, -.20), (.24 + .11 * math.cos(a), -.20 + .11 * math.sin(a)), .008)
    kayis(ag); ag.yaz('ag_kalkan', 2)

def tabut():   # KATALOGDA DEĞİL (2026-11-12 tasarım dili: ölüm çağrışımı sevimli oyuncak diline uymuyor). tabut kapağı kalkanı: altıgen ahşap kapak, kenar çıtası, pirinç çiviler ve kulp
    ag = Ag(); A, A2, P = '#7a4a26', '#5d371b', '#d9b45a'
    dis = [(-.24, -.62), (.24, -.62), (.40, -.26), (.26, .66), (-.26, .66), (-.40, -.26)]
    bombe(ag, A2, dis, 0, .06, .08); bombe(ag, A, olc(dis, .84), .05, .09, .13, 2, False)
    for x, y in ((-.18, -.50), (.18, -.50), (-.30, -.24), (.30, -.24), (-.20, .54), (.20, .54)): top(ag, P, (x, y, .09), .03, 5, 3)
    halka_z(ag, P, (0, -.10, .16), .09, .022, 10, 5)
    kayis(ag, -.30, .26, .18); ag.yaz('tabut', 2)

def kazan():   # cadı kazanı: oyuncu kazanın içinde durur; şişkin siyah gövde, kenar halkası, fokurdayan yeşil iksir
    ag = Ag(); Y = '#63d13f'
    for (y0, a0, b0, y1, a1, b1) in ((-.34, .96, .90, -.10, 1.30, 1.22), (-.10, 1.30, 1.22, .12, 1.20, 1.12)): elips_kusak(ag, SYH, a0, b0, y0, a1, b1, y1)
    elips_halka(ag, '#3a3640', 1.20, 1.12, .07, .14, .10, 24, 6)
    elips_kusak(ag, Y, .80, .72, .12, 1.16, 1.08, .12)
    for (a, rr, q) in ((20, 1.0, .09), (95, .96, .07), (160, 1.02, .10), (230, .98, .06), (300, 1.0, .08)):
        ar = math.radians(a); top(ag, '#9bf07a', (rr * math.cos(ar), .15, .10 + rr * .93 * math.sin(ar)), q, 6, 4)
    for sgn in (-1, 1): halka_z(ag, '#3a3640', (sgn * 1.36, -.02, .10), .12, .03, 10, 5)
    ag.yaz('kazan', 3)

def kedi_kuyruk():   # kara kedi kuyruğu: kuyruk sokumundan yukarı soru işareti gibi kıvrılır, ucu beyaz
    ag = Ag(); K = '#1d1b22'
    yol = [((0, -.08, -.42), .085), ((0, -.16, -.72), .085), ((0, -.02, -1.02), .08), ((0, .30, -1.18), .08), ((0, .66, -1.16), .075), ((0, .92, -.98), .07), ((0, .98, -.74), .065)]
    boru(ag, K, yol, 8); boru(ag, BEY, [((0, .98, -.74), .065), ((0, .90, -.56), .06), ((0, .78, -.48), .03)], 8)
    ag.yaz('kedi_kuyruk', 3)

# ---- Periler ve masallar (2026-11-12, yasin: "periler, hobbitler, masallar") ----
def cicek(ag, c, r, renk, eksen='y', orta=ALT):
    """Beş yapraklı çiçek: c merkezinde, eksen yönüne bakan; yapraklar küçük yassı toplar."""
    for k in range(5):
        a = 2 * math.pi * k / 5; u, v = r * .62 * math.cos(a), r * .62 * math.sin(a)
        p = (c[0] + u, c[1], c[2] + v) if eksen == 'y' else (c[0] + u, c[1] + v, c[2])
        elipsoid(ag, lambda i, j: renk, p, (r * .5, r * .28, r * .5) if eksen == 'y' else (r * .5, r * .5, r * .28), 5, 2)
    top(ag, orta, (c[0], c[1] + (r * .12 if eksen == 'y' else 0), c[2] + (r * .12 if eksen != 'y' else 0)), r * .3, 5, 3)

def cicek_tac():   # çiçek taç: saçın üstünde yaprak halkası, dokuz çiçek
    ag = Ag(); R = ['#ff8fc0', '#fbfbf6', '#b58cff']
    halka(ag, '#4f9a45', 1.04, .07, .44, 16, 5)   # saçın çevresine oturur (y .44'te saç yarıçapı ≈ 1,0); tepede havada halka gibi durmaz
    for k in range(6):   # 2026-11-12 tasarım dili: dokuz küçük çiçek + yaprak yerine altı iri çiçek (uzaktan okunur, üçgen bütçesi)
        a = 2 * math.pi * k / 6 + .52; x, z = 1.10 * math.cos(a), 1.10 * math.sin(a)
        cicek(ag, (x, .52, z), .28, R[k % 3])
    ag.yaz('cicek_tac', 0)

def kelebek_kanat():   # kelebek kanatları: üst ve alt lop, koyu kenar, benekler; sırttan arkaya ve yana açık
    ag = Ag(); zf = lambda x, y: -.10 - .30 * abs(x) - .05 * max(0, y)
    ust = [(.10, .10), (.34, .56), (.74, .92), (1.16, .98), (1.38, .74), (1.30, .40), (1.00, .14), (.60, .02)]
    alt = [(.10, .0), (.56, -.04), (.92, -.20), (1.02, -.52), (.84, -.74), (.52, -.66), (.26, -.36)]
    for sgn in (-1, 1):
        for lop, renk, ic in ((ust, '#3b2a6b', '#b58cff'), (alt, '#3b2a6b', '#ff8fc0')):
            cx = sum(p[0] for p in lop) / len(lop); cy = sum(p[1] for p in lop) / len(lop)
            nn = lop if sgn > 0 else list(reversed(ayna(lop))); levha(ag, renk, nn, .04, zf)
            kk = [(cx + (x - cx) * .78, cy + (y - cy) * .78) for x, y in lop]; kk = kk if sgn > 0 else list(reversed(ayna(kk)))
            levha(ag, ic, kk, .02, lambda x, y: zf(x, y) - .045)   # desen arka yüzde: oyun kamerası oyuncuya arkadan bakar
        for (x, y, r) in ((.98, .70, .10), (.70, .52, .07), (.70, -.42, .08)):
            levha(ag, ALT, [(sgn * x + r * math.cos(2 * math.pi * k / 8), y + r * math.sin(2 * math.pi * k / 8)) for k in range(8)], .01, lambda xx, yy: zf(xx, yy) - .068)
    boru(ag, '#3b2a6b', [((0, -.34, -.12), .05), ((0, .0, -.14), .07), ((0, .30, -.12), .05)], 8)
    ag.yaz('kelebek_kanat', 1)

def yaprak_etek():   # yaprak etek: belden sarkan on dört yaprak, iki ton yeşil, önde çiçek
    ag = Ag(); Y = ['#4f9a45', '#63b356']; n = 14
    elips_kusak(ag, '#3d7a36', .85, .73, -.01, .85, .73, .11)
    for i in range(n):
        a0 = 2 * math.pi * (i - .5) / n; a1 = 2 * math.pi * (i + .5) / n; am = (a0 + a1) / 2
        P = lambda rx, rz, y, a: (rx * math.cos(a), y, .10 + rz * math.sin(a))
        ag.dortgen(Y[i % 2], P(.86, .74, .03, a0), P(.86, .74, .03, a1), P(1.10, .98, -.16, a1 + .03), P(1.10, .98, -.16, a0 - .03))
        ag.ucgen(Y[i % 2], P(1.10, .98, -.16, a0 - .03), P(1.10, .98, -.16, a1 + .03), P(1.24, 1.12, -.36, am))
    cicek(ag, (0, .06, .88), .13, '#ff8fc0', 'z')
    ag.yaz('yaprak_etek', 3)

def pirilti():   # peri pırıltısı (yüz): iki yanakta üçer küçük parlak pul (yanak yüzeyi x .5'te z ≈ .84)
    ag = Ag()
    for sgn in (-1, 1):
        for (x, y, z, r, renk) in ((.44, -.66, .97, .07, ALT), (.58, -.76, .88, .052, '#ff8fc0'), (.53, -.54, .93, .046, '#b58cff')): elipsoid(ag, renk, (sgn * x, y, z), (r, r, r * .45), 6, 3)   # r158: yanağın içinde kalıyordu, öne alındı ve irileşti
    ag.yaz('pirilti', 0)

def ugur_bocegi():   # uğur böceği kalkanı: kırmızı kubbe kabuk, siyah benekler ve orta çizgi, bilek tarafında baş
    ag = Ag()
    bombe(ag, SYH, cember(.54, 18), 0, .05, .06); bombe(ag, '#e3262c', cember(.50, 18), .04, .07, .26, 4, False)
    zz = lambda x, y: bombe_z(.07, .26, min(1, math.hypot(x, y) / .50))
    kutu(ag, SYH, (-.012, -.44, .10), (.012, .30, .275))
    for (x, y, r) in ((.22, -.22, .08), (-.22, -.22, .08), (.26, .06, .07), (-.26, .06, .07), (.12, .26, .05), (-.12, .26, .05)): elipsoid(ag, SYH, (x, y, zz(x, y) + .005), (r, r, .02), 8, 3)
    elipsoid(ag, SYH, (0, .48, .10), (.24, .16, .12), 10, 4)
    for sgn in (-1, 1): top(ag, BEY, (sgn * .10, .56, .17), .035, 6, 3); boru(ag, SYH, [((sgn * .08, .60, .12), .012), ((sgn * .16, .72, .16), .012), ((sgn * .22, .74, .18), .012)], 4)
    kayis(ag); ag.yaz('ugur_bocegi', 2)

def tek_boynuz():   # tek boynuz: alnın üstünde burgulu sedef boynuz, tepeden arkaya gökkuşağı yele
    ag = Ag(); G = ['#ff8fc0', '#ffc46b', '#fff07a', '#8fe3a0', '#8fc7ff', '#b58cff']
    kok, uc = (0, .86, .46), (0, 1.86, .86); n = 6
    for k in range(n):
        t0, t1 = k / n, (k + 1) / n; a = tuple(kok[i] + (uc[i] - kok[i]) * t0 for i in range(3)); b_ = tuple(kok[i] + (uc[i] - kok[i]) * t1 for i in range(3))
        kesik(ag, '#fff6e0' if k % 2 == 0 else '#ffe9b8', a, b_, .15 * (1 - t0) + .02, .15 * (1 - t1) + .01, 8)
    halka(ag, ALT, .15, .035, 0, 10, 5, (0, .88, .47))
    for k in range(7):
        a = math.radians(70 - 26 * k); top(ag, G[k % 6], (0, 1.04 * math.sin(a) + .02, -1.02 * math.cos(a) - .08), .17 - .008 * k, 8, 4)   # tepeden enseye (z eksi = arka)
    ag.yaz('tek_boynuz', 0)

def kirmizi_baslik():   # kırmızı başlık: başı arkadan ve yandan saran kukuleta (yüz açık), omuzlara inen kısa pelerin, önde fiyonk
    ag = Ag(); K, K2 = '#c7232b', '#a81c23'; c = (0, -.05, -.02); r = (1.30, 1.30, 1.34); n, m = 20, 10
    def P(i, j):
        lo = 2 * math.pi * i / n; la = -math.pi / 2 + math.pi * j / m
        return (c[0] + r[0] * math.cos(la) * math.sin(lo), c[1] + r[1] * math.sin(la), c[2] + r[2] * math.cos(la) * math.cos(lo))
    for i in range(n):
        for j in range(2, m):
            lo = 360 * (i + .5) / n; la = -90 + 180 * (j + .5) / m; on = min(lo, 360 - lo)
            if on < 52 and -60 < la < 34: continue   # yüz penceresi
            ag.dortgen(K, P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1))
    Q = lambda rr, y, i: (rr * math.sin(2 * math.pi * i / n), y, rr * math.cos(2 * math.pi * i / n) - .02)
    for i in range(n): ag.dortgen(K2 if i % 2 else K, Q(1.02, -1.02, i), Q(1.02, -1.02, i + 1), Q(1.72, -1.72, i + 1), Q(1.72, -1.72, i))
    for sgn in (-1, 1): elipsoid(ag, BEY, (sgn * .20, -1.20, 1.06), (.20, .11, .07), 8, 3)
    top(ag, BEY, (0, -1.20, 1.08), .08, 6, 3)
    ag.yaz('kirmizi_baslik', 0)

def kurbaga():   # kurbağa prens: başın tepesinde oturan kurbağa, iri gözler, küçük altın taç
    ag = Ag(); Y, Y2 = '#5fbf4a', '#7ed267'
    elipsoid(ag, Y, (0, 1.14, -.04), (.56, .38, .52), 12, 6); elipsoid(ag, Y2, (0, 1.06, .22), (.40, .24, .34), 10, 4)
    for sgn in (-1, 1):
        top(ag, Y, (sgn * .26, 1.50, .22), .17, 8, 5); top(ag, BEY, (sgn * .26, 1.52, .32), .11, 8, 4); top(ag, SYH, (sgn * .26, 1.53, .41), .055, 6, 3)
        elipsoid(ag, Y, (sgn * .52, .98, .10), (.20, .13, .30), 8, 4); elipsoid(ag, Y, (sgn * .50, .96, -.36), (.24, .15, .24), 8, 4)
    boru(ag, '#2e7d32', [((-.24, 1.12, .50), .018), ((0, 1.06, .56), .018), ((.24, 1.12, .50), .018)], 4)
    kesik(ag, ALT, (0, 1.50, -.10), (0, 1.62, -.10), .13, .15, 8)
    for k in range(4): a = 2 * math.pi * k / 4 + .4; koni(ag, ALT, (.14 * math.cos(a), 1.62, -.10 + .14 * math.sin(a)), (.16 * math.cos(a), 1.74, -.10 + .16 * math.sin(a)), .04, 4)
    ag.yaz('kurbaga', 0)

def fasulye():   # sihirli fasulye sırığı: belden başın üstüne kıvrılarak çıkan sarmaşık, yapraklar, tepede bulut
    ag = Ag(); Y = '#4f9a45'
    yol = [((.10 * math.sin(k * 1.1), -.62 + k * .22, -.24 - .05 * math.cos(k * 1.1)), .07 - .003 * k) for k in range(11)]
    boru(ag, Y, yol, 8)
    for k in (1, 3, 5, 7, 9):
        (x, y, z), _ = yol[k]; sgn = 1 if k % 4 == 1 else -1
        levha(ag, '#63b356', [(x, y), (x + sgn * .16, y + .12), (x + sgn * .36, y + .08), (x + sgn * .18, y - .04)], .02, lambda xx, yy, z=z: z - .02)
    for (x, y, z, r) in ((0, 1.66, -.30, .22), (-.24, 1.60, -.30, .17), (.24, 1.60, -.30, .17), (.10, 1.76, -.30, .15)): top(ag, BEY, (x, y, z), r, 8, 5)
    ag.yaz('fasulye', 1)

def ejderha_kuyruk():   # ejderha kuyruğu: kalın yeşil kuyruk arkaya ve aşağı iner, ucu yukarı kıvrık; sırt boyunca sarı dikenler
    ag = Ag(); Y = '#3f9b4f'
    yol = [((0, -.06, -.40), .20), ((0, -.20, -.72), .18), ((0, -.30, -1.06), .15), ((0, -.26, -1.40), .12), ((0, -.10, -1.66), .09), ((0, .12, -1.80), .05)]
    boru(ag, Y, yol, 10); koni(ag, Y, (0, .10, -1.79), (0, .30, -1.88), .05, 6)
    for k in range(1, 5):
        (x, y, z), r = yol[k]; koni(ag, ALT, (0, y + r * .8, z), (0, y + r + .16, z - .06), .07, 4)
    ag.yaz('ejderha_kuyruk', 3)

def dev_yuzuk():   # dev altın yüzük: ön kola bilezik gibi geçmiş kalın halka, üstünde kırmızı taş (kol ekseni y; kolun merkezi z ≈ −.13)
    ag = Ag()
    halka(ag, ALT, .25, .085, 0, 18, 8, (0, 0, -.13)); halka(ag, '#d9a81c', .25, .03, .085, 18, 5, (0, 0, -.13)); halka(ag, '#d9a81c', .25, .03, -.085, 18, 5, (0, 0, -.13))
    elipsoid(ag, '#e3262c', (0, 0, .22), (.09, .11, .07), 8, 4); halka_z(ag, '#d9a81c', (0, 0, .20), .10, .02, 10, 4)
    ag.yaz('dev_yuzuk', 2)

# ---- Geleneksel kıyafetler koleksiyonu (2026-11-12, yasin: "geleneksel kıyafetler ... app boyutunu artırmadan"): dünyadan başlık, çalgı ve
# kuşaklar; hepsi aynı gövdeye takılan parça, yeni model yok. ----
def fes():   # fes: tepede oturan kırmızı kesik koni (taban saça gömülü), siyah püskül yandan sarkar
    ag = Ag(); K = '#b3202a'
    kesik(ag, K, (0, .62, 0), (0, 1.50, 0), .78, .60, 16); disk(ag, '#c7262f', (0, 1.50, 0), .60, 16)
    top(ag, SYH, (0, 1.52, 0), .05, 6, 3)
    boru(ag, SYH, [((0, 1.53, 0), .035), ((.32, 1.57, .04), .035), ((.64, 1.52, .06), .035), ((.76, 1.30, .07), .035), ((.80, 1.04, .07), .035)], 6)
    kesik(ag, SYH, (.80, 1.08, .07), (.84, .66, .07), .05, .12, 8); top(ag, SYH, (.80, 1.08, .07), .07, 6, 3)
    ag.yaz('fes', 0)

def sombrero():   # sombrero: geniş, kenarı yukarı kıvrık siper (iç 1,12 > saç), yüksek tepe, renkli şerit ve kenar süsü
    ag = Ag(); H, H2 = '#e3c06a', '#c99a3c'; R = ['#d6342f', '#2f9e5b', '#f2f2f2']
    elips_kusak(ag, H, 1.12, 1.12, .46, 2.00, 2.00, .40, 0, 20); elips_kusak(ag, H2, 2.00, 2.00, .40, 2.26, 2.26, .66, 0, 20)
    elips_halka(ag, R[0], 2.28, 2.28, .06, .67, 0, 20, 4)
    for i in range(8):
        a = 2 * math.pi * i / 8; top(ag, R[i % 2], (2.12 * math.cos(a), .54, 2.12 * math.sin(a)), .11, 6, 3)
    boru(ag, H, [((0, .44, 0), 1.14), ((0, .80, 0), 1.00), ((0, 1.20, 0), .78), ((0, 1.52, 0), .62), ((0, 1.72, 0), .40), ((0, 1.80, 0), .12)], 14); disk(ag, H, (0, 1.80, 0), .13, 14)
    elips_kusak(ag, R[0], 1.13, 1.13, .50, 1.07, 1.07, .72, 0, 14)
    ag.yaz('sombrero', 0)

def hasir():   # konik hasır şapka: geniş alçak koni (iç yüz saçın dışında: r 1,0'da y .86), örgü halkaları, tepede düğüm
    ag = Ag(); H, H2 = '#e6cf8a', '#c9ab5c'; R0, y0, y1 = 2.00, .38, 1.42; n = 24
    P = lambda t, i: (R0 * (1 - t) * math.cos(2 * math.pi * i / n), y0 + (y1 - y0) * t, R0 * (1 - t) * math.sin(2 * math.pi * i / n))
    for k in range(6):
        t0, t1 = k / 6, (k + 1) / 6
        for i in range(n): ag.dortgen(H if k % 2 == 0 else H2, P(t0, i), P(t0, i + 1), P(t1, i + 1), P(t1, i))
    elips_halka(ag, H2, R0, R0, .045, y0, 0, 24, 5); top(ag, '#8a5a2b', (0, y1, 0), .07, 6, 3)
    ag.yaz('hasir', 0)

def viking():   # Viking miğferi: çelik kubbe (iç 1,14 > saç), perçinli alın bandı, burun siperi, yana ve yukarı kıvrılan iki boynuz
    ag = Ag(); C, C2, B = '#aeb6bf', '#7f8791', '#f3ead2'
    elipsoid(ag, C, (0, .30, 0), (1.16, 1.02, 1.16), 18, 6, 0, 90); elips_kusak(ag, C2, 1.19, 1.19, .18, 1.19, 1.19, .42, 0, 24)
    boru(ag, C2, [((0, .32, 1.17), .07), ((0, .80, 1.02), .07), ((0, 1.18, .58), .07), ((0, 1.34, 0), .07), ((0, 1.18, -.58), .07), ((0, .80, -1.02), .07), ((0, .32, -1.17), .07)], 6)
    kutu(ag, C2, (-.09, -.22, 1.17), (.09, .20, 1.23))   # burun siperi (yüzün önünde, buruna değmez: burun ucu z 1,08)
    for i in range(10): a = 2 * math.pi * i / 10; top(ag, ALT, (1.20 * math.cos(a), .30, 1.20 * math.sin(a)), .05, 5, 3)
    for sgn in (-1, 1):
        boru(ag, B, [((sgn * 1.02, .62, 0), .21), ((sgn * 1.34, .74, 0), .19), ((sgn * 1.62, .98, 0), .15), ((sgn * 1.74, 1.30, 0), .10), ((sgn * 1.70, 1.58, 0), .05), ((sgn * 1.62, 1.74, 0), .012)], 10)
        halka(ag, C2, .215, .05, 0, 10, 5, (0, 0, 0)) if False else boru(ag, C2, [((sgn * .98, .60, 0), .25), ((sgn * 1.10, .65, 0), .25)], 10)
    ag.yaz('viking', 0)

def kavuk():   # kavuk: kat kat sarılmış iri beyaz sarık (iç 1,1 > saç), ortada kırmızı tepe, önde taşlı sorguç ve tüy
    ag = Ag(); B, B2, K = '#f4f1e8', '#ddd8c9', '#b3202a'
    for k, (R, r, y) in enumerate(((1.26, .30, .42), (1.16, .30, .78), (.94, .28, 1.10))): halka(ag, B if k % 2 == 0 else B2, R, r, y, 22, 8)
    for k, (R, y) in enumerate(((1.58, .44), (1.48, .80), (1.24, 1.12))): halka(ag, '#c9c3b2', R, .028, y, 22, 4)   # sargı kat izleri
    elipsoid(ag, K, (0, 1.14, 0), (.76, .52, .76), 14, 5, 0, 90)
    elipsoid(ag, '#2f9e8f', (0, .86, 1.50), (.12, .15, .07), 8, 4); halka_z(ag, ALT, (0, .86, 1.49), .15, .03, 10, 4)
    levha(ag, '#f4f1e8', [(0, 1.00), (.13, 1.30), (.11, 1.70), (0, 1.96), (-.11, 1.70), (-.13, 1.30)], .06, lambda x, y: 1.50 - (y - 1.0) * .45)
    ag.yaz('kavuk', 0)

def pos_biyik():   # pos bıyık (yüz): burun altından iki yana uzanan, uçları yukarı kıvrık gür bıyık (dudak üstü y −.9 z 1,07; yanak x .5'te z ≈ .84)
    ag = Ag(); S = '#3a2a1e'
    for sgn in (-1, 1):
        boru(ag, S, [((0, -.82, 1.13), .065), ((sgn * .20, -.84, 1.11), .08), ((sgn * .40, -.87, 1.03), .07), ((sgn * .58, -.83, .92), .05),
                     ((sgn * .72, -.71, .81), .032), ((sgn * .77, -.57, .76), .012)], 8)   # r158: ortası dudağın içinde kalıyordu, .08 öne ve biraz yukarı
    elipsoid(ag, S, (0, -.82, 1.13), (.07, .066, .066), 8, 5)   # orta ek yerini kapatır
    ag.yaz('pos_biyik', 0)

def saz():   # saz: sırtta çapraz, armut gövde aşağıda, uzun sap omuz üstüne çıkar, burgular ve teller
    ag = Ag(); A, A2, T = '#b9793a', '#7a4a26', '#f0e2c4'
    a, b_ = (-.30, -.44, -.30), (.62, .82, -.24); d = [b_[i] - a[i] for i in range(3)]; L = math.sqrt(sum(x * x for x in d)); u = [x / L for x in d]
    q = lambda t: tuple(a[i] + u[i] * t for i in range(3))
    boru(ag, A, [(q(-.30), .04), (q(-.25), .22), (q(-.08), .34), (q(.10), .35), (q(.30), .26), (q(.48), .13), (q(.62), .06)], 12)   # armut gövde
    disk_c = q(.02); elipsoid(ag, T, (disk_c[0], disk_c[1], disk_c[2] - .30), (.22, .26, .06), 10, 3)   # göğüs tahtası arkadan görünür
    silindir(ag, A2, q(.58), q(L), .045, 6)
    for k in range(3):
        p = q(L - .06 - k * .08); sgn = 1 if k % 2 == 0 else -1; silindir(ag, T, p, (p[0] + sgn * .12, p[1] - sgn * .06, p[2]), .022, 5)
    ag.yaz('saz', 1)

def davul():   # davul: sırtta asılı, kasnak arkaya bakar; kırmızı gövde, beyaz deri, zikzak ip, tokmak
    ag = Ag(); K, B, I = '#c7232b', '#f6f0df', '#e8c547'
    silindir(ag, K, (0, -.12, -.16), (0, -.12, -.58), .50, 18); silindir(ag, B, (0, -.12, -.58), (0, -.12, -.62), .52, 18)
    halka_z(ag, I, (0, -.12, -.56), .51, .035, 18, 5); halka_z(ag, I, (0, -.12, -.18), .51, .035, 18, 5)
    for k in range(10):
        a0, a1 = 2 * math.pi * k / 10, 2 * math.pi * (k + .5) / 10; a2 = 2 * math.pi * (k + 1) / 10; R = .515
        boru(ag, B, [((R * math.cos(a0), -.12 + R * math.sin(a0), -.20), .014), ((R * math.cos(a1), -.12 + R * math.sin(a1), -.54), .014), ((R * math.cos(a2), -.12 + R * math.sin(a2), -.20), .014)], 4)
    silindir(ag, '#8a5a2b', (.30, .50, -.66), (-.06, .04, -.66), .028, 6); top(ag, B, (-.08, .02, -.66), .075, 8, 4)
    ag.yaz('davul', 1)

def yelpaze():   # dev yelpaze: sırtta açılmış katlı yelpaze, dilimler iki renk ve körük gibi kırık, altın çıtalar, sapta püskül
    ag = Ag(); R = ['#d6342f', '#f2d9a0']; c = (0, -.52); n = 12; r0, r1 = .24, 1.30
    for k in range(n):
        a0 = math.radians(18 + 144 * k / n); a1 = math.radians(18 + 144 * (k + 1) / n)
        z0, z1 = (-.16, -.22) if k % 2 == 0 else (-.22, -.16)   # körük kıvrımı
        p = lambda a, r, z: (c[0] + r * math.cos(a), c[1] + r * math.sin(a), z - .34 * (r / r1))
        ag.dortgen(R[k % 2], p(a0, r0, z0), p(a1, r0, z1), p(a1, r1, z1), p(a0, r1, z0)); ag.dortgen(R[k % 2], p(a0, r1, z0 - .02), p(a1, r1, z1 - .02), p(a1, r0, z1 - .02), p(a0, r0, z0 - .02))
    for k in range(n + 1):
        a = math.radians(18 + 144 * k / n); z = -.16 if k % 2 == 0 else -.22
        silindir(ag, ALT, (c[0], c[1], -.20), (c[0] + r1 * 1.02 * math.cos(a), c[1] + r1 * 1.02 * math.sin(a), z - .35), .016, 4)
    top(ag, ALT, (c[0], c[1], -.20), .07, 8, 4); boru(ag, '#d6342f', [((0, -.56, -.22), .02), ((.04, -.70, -.24), .02), ((.02, -.78, -.24), .05)], 5)
    ag.yaz('yelpaze', 1)

def kilim():   # kilim kalkanı: dokuma desenli bombeli dikdörtgen, baklava motifleri, iki uçta püskül
    ag = Ag(); K, L, S, B = '#a8322a', '#1f4f7a', '#e8c547', '#f4ecd6'
    dis = [(-.36, -.56), (.36, -.56), (.36, .56), (-.36, .56)]
    bombe(ag, K, dis, 0, .05, .10, 3); z = lambda k: bombe_z(.05, .10, k) + .004
    bak = lambda cx, cy, w, h: [(cx - w, cy), (cx, cy - h), (cx + w, cy), (cx, cy + h)]
    for cy, kr in ((-.30, L), (0, S), (.30, L)):
        plaka(ag, kr, bak(0, cy, .22, .13), z(.5) - .004, z(.5) + .012); plaka(ag, B, bak(0, cy, .10, .06), z(.3), z(.3) + .016)
    for y in (-.50, .50): plaka(ag, S, [(-.32, y - .025), (.32, y - .025), (.32, y + .025), (-.32, y + .025)], .05, .075)
    for y, d in ((-.56, -1), (.56, 1)):
        for k in range(7): x = -.30 + k * .10; silindir(ag, B, (x, y, .03), (x, y + d * .10, .03), .018, 4)
    kayis(ag, -.28, .24, .18); ag.yaz('kilim', 2)

def tef():   # tef kalkanı: ahşap kasnak, gergin deri, kasnakta altı çift zil
    ag = Ag(); A, D, Z = '#b9793a', '#f6ecd0', '#e8c547'
    halka_z(ag, A, (0, 0, .06), .50, .075, 20, 8); bombe(ag, D, cember(.48, 20), .02, .08, .10, 2)
    for k in range(6):
        a = 2 * math.pi * k / 6 + .3; c = (.50 * math.cos(a), .50 * math.sin(a))
        for dz in (.03, .09): bombe(ag, Z, cember(.085, 8, c), dz + .1, dz + .112, dz + .125, 1) if False else elipsoid(ag, Z, (c[0] * 1.14, c[1] * 1.14, dz), (.085, .085, .016), 8, 3, eksen='z-')
    kayis(ag, -.24, .22, .18); ag.yaz('tef', 2)

def kilt():   # İskoç eteği: pilili ekose etek (iki ton pili, yatay çizgiler), deri kemer, önde püsküllü kese
    ag = Ag(); K, Y, S = '#b3202a', '#1f5c3a', '#e8c547'
    elips_kusak(ag, '#4a3323', .86, .74, .06, .86, .74, .18); kutu(ag, S, (-.10, .05, .82), (.10, .19, .86))
    n = 28; P = lambda rx, rz, y, i, d=0: ((rx + d) * math.cos(2 * math.pi * i / n), y, .10 + (rz + d) * math.sin(2 * math.pi * i / n))
    for i in range(n):
        d0, d1 = (.0, .05) if i % 2 == 0 else (.05, .0)
        ag.dortgen(K if i % 2 == 0 else Y, P(.87, .75, .06, i), P(.87, .75, .06, i + 1), P(1.06, .96, -.32, i + 1, d1), P(1.06, .96, -.32, i, d0))
    for y, rr in ((-.08, .075), (-.22, .15)): t = rr / .19; elips_halka(ag, S, .87 + .19 * t + .035, .75 + .21 * t + .035, .012, y, .10, 28, 4)
    elipsoid(ag, '#f4f1e8', (0, -.10, 1.02), (.14, .15, .07), 8, 4); elips = None
    for x in (-.07, 0, .07): kesik(ag, SYH, (x, -.08, 1.08), (x, -.22, 1.09), .012, .03, 5)
    ag.yaz('kilt', 3)

def obi():   # obi kuşağı: geniş ipek kuşak, ince orta kordon, arkada iri fiyonk
    ag = Ag(); M, M2, S = '#7a3fa0', '#9b5cc4', '#e8c547'
    elips_kusak(ag, M, .87, .75, -.06, .87, .75, .26); elips_kusak(ag, S, .885, .765, .07, .885, .765, .13)
    for sgn in (-1, 1): elipsoid(ag, M2, (sgn * .34, .14, -.74), (.34, .24, .13), 10, 5); elipsoid(ag, M2, (sgn * .22, -.20, -.72), (.14, .22, .08), 8, 4)
    elipsoid(ag, M, (0, .12, -.78), (.13, .15, .13), 8, 4)
    ag.yaz('obi', 3)

def pullu_kusak():   # pullu kuşak: bele bağlı tül şerit, iki sıra sallanan altın pul, yanda düğüm
    ag = Ag(); T, S = '#2f9e8f', '#e8c547'
    elips_kusak(ag, T, .87, .75, -.04, .87, .75, .12)
    n = 14   # 2026-11-12 tasarım dili: iki sıra 44 küçük pul yerine tek sıra 14 iri pul
    for i in range(n):
        a = 2 * math.pi * (i + .5) / n; nx, nz = math.cos(a), math.sin(a); p = (.92 * nx, -.14, .10 + .80 * nz)
        elipsoid(ag, lambda i_, j_: S, p, (.10 * abs(nz) + .025, .10, .10 * abs(nx) + .025), 6, 2)
    top(ag, T, (.88, .04, .10), .09, 8, 4); boru(ag, T, [((.90, .02, .10), .04), ((1.00, -.16, .12), .035), ((1.02, -.34, .12), .05)], 6)
    ag.yaz('pullu_kusak', 3)

# ---- Tatlıcı seti (2026-11-13, yasin: "yeni skinler ekleme"; tasarım dili "tombul oyuncak" §3 konu: yiyecek) ----
# Beşinci konsept: dondurma, kek, lolipop, donut, makaron, krem şanti. İri yuvarlak kütleler, pastel düz renk, eşya başına ≤4 renk.
PEM, NANE, KREM, GOFRET = '#ff8fbf', '#9be3c4', '#fff3f6', '#d9a25a'

def dondurma():   # başa düşmüş dondurma: pembe top başın tepesini tencere gibi sarar (r166: saçın üstünde oturtulan top havada kaldı; kenar 1,12 > saç), damlalar kenardan sarkar, üstte nane top ve ters gofret külah
    ag = Ag()
    elipsoid(ag, PEM, (0, .98, 0), (1.14, .56, 1.14), 14, 6, -25, 90)
    for k in range(7):
        a = 2 * math.pi * k / 7 + .3; elipsoid(ag, PEM, (1.08 * math.cos(a), .66 - .07 * (k % 2), 1.08 * math.sin(a)), (.16, .24 + .07 * (k % 2), .16), 6, 3)
    elipsoid(ag, NANE, (0, 1.58, 0), (.60, .36, .60), 10, 5)
    koni(ag, GOFRET, (0, 1.84, 0), (0, 2.66, 0), .54, 10)
    for y in (2.04, 2.30): halka(ag, '#b98242', .54 * (2.66 - y) / .82 + .01, .025, y, 10, 3)
    ag.yaz('dondurma', 0)

def cupcake():   # kek şapka: kâğıt kalıp başı tencere gibi sarar (r166: saç tepesine oturtulan kalıp havada kaldı; iç 1,12 > saç), üç kat kabarık pembe krema, tepede kiraz, iri renkli şeker
    ag = Ag(); K = '#7cc4ff'
    kesik(ag, K, (0, .42, 0), (0, 1.12, 0), 1.14, 1.26, 16); disk(ag, K, (0, 1.12, 0), 1.26, 16)
    for i in range(8):   # kalıbın oluklu kenarı: iri kabartma şeritler (ince çizgi yerine)
        a = 2 * math.pi * i / 8; kesik(ag, '#5aaef0', (1.17 * math.cos(a), .46, 1.17 * math.sin(a)), (1.29 * math.cos(a), 1.10, 1.29 * math.sin(a)), .07, .07, 5)
    for R, r, y in ((.96, .30, 1.24), (.66, .27, 1.54), (.36, .23, 1.80)): halka(ag, '#ffc6dc', R, r, y, 16, 6)
    top(ag, '#ffc6dc', (0, 1.98, 0), .20, 8, 4)
    top(ag, '#e3262c', (0, 2.22, 0), .17, 8, 5)
    for k in range(7):
        a = 2 * math.pi * k / 7; R = 1.04 if k % 2 == 0 else .74; y = 1.46 if k % 2 == 0 else 1.74
        top(ag, '#e3262c' if k % 3 == 0 else '#5aaef0', (R * math.cos(a), y, R * math.sin(a)), .06, 5, 3)
    for renk in ag.alt: ag.alt[renk] = [tuple((q[0], q[1] - .22, q[2]) for q in t) for t in ag.alt[renk]]   # r167: kalıp saçın tepesinde tünemiş duruyordu; tencere gibi alna yakın (kenar y .20, bant çizgisinin üstünde)
    ag.yaz('cupcake', 0)

def krema_biyik():   # krem şanti bıyık (yüz): pos bıyıkla aynı dudak üstü yolu, kabarık krem topları; ortada küçük kiraz yok (sade)
    ag = Ag()
    for sgn in (-1, 1):
        boru(ag, KREM, [((0, -.82, 1.13), .07), ((sgn * .22, -.84, 1.11), .09), ((sgn * .42, -.86, 1.02), .08), ((sgn * .58, -.80, .91), .06), ((sgn * .66, -.70, .84), .04)], 8)
        for t, (x, y, z, r) in enumerate(((.18, -.80, 1.15, .07), (.38, -.82, 1.07, .065), (.56, -.76, .95, .05))): top(ag, KREM, (sgn * x, y, z), r, 6, 4)
    elipsoid(ag, KREM, (0, -.80, 1.15), (.08, .075, .07), 8, 5)
    ag.yaz('krema_biyik', 0)

def lolipop():   # sırtta dev lolipop: sap belden çapraz, şeker başın arkasında (z <= -.3: enseye değmez), pembe-beyaz halkalar, sapta mavi fiyonk
    ag = Ag(); z = -.36; c = (-.10, .78)
    silindir(ag, BEY, (.20, -.60, -.18), (c[0] + .03, c[1] - .40, z + .04), .035, 6)
    bombe(ag, PEM, cember(.44, 16, c), z - .04, z, z + .02, 1)
    for i, R in enumerate((.36, .24, .12)): halka_z(ag, BEY if i % 2 == 0 else PEM, (c[0], c[1], z + .03), R, .055, 16, 4)
    top(ag, BEY, (c[0], c[1], z + .05), .06, 6, 3)
    for sgn in (-1, 1): elipsoid(ag, '#7cc4ff', (c[0] + .04 + sgn * .09, c[1] - .48, z + .06), (.09, .06, .04), 6, 3)
    top(ag, '#7cc4ff', (c[0] + .04, c[1] - .48, z + .07), .04, 5, 3)
    ag.yaz('lolipop', 1)

def donut():   # donut kalkanı: kabarık hamur halkası, üstünde pembe şeker kaplama, iri renkli şekerler; ortası boş (kol görünür)
    ag = Ag()
    halka_z(ag, GOFRET, (0, 0, .10), .34, .18, 16, 6)
    halka_z(ag, PEM, (0, 0, .17), .34, .145, 16, 5)
    for k in range(9):
        a = 2 * math.pi * k / 9 + .2; r = .34 + .07 * (1 if k % 2 else -1)
        elipsoid(ag, BEY if k % 2 == 0 else '#7cc4ff', (r * math.cos(a), r * math.sin(a), .31), (.05, .022, .02), 5, 2)
    kayis(ag); ag.yaz('donut', 2)

def makaron_kusak():   # makaron kuşağı: krem kuşak, üstünde yan yana dizili altı iri makaron (iki kapak, ortada krema)
    ag = Ag(); R = [PEM, NANE, '#ffd96a']
    elips_kusak(ag, KREM, .87, .75, -.04, .87, .75, .14)
    n = 6
    for i in range(n):
        a = 2 * math.pi * (i + .5) / n; p = (1.02 * math.cos(a), .05, .10 + .90 * math.sin(a)); rk = R[i % 3]
        elipsoid(ag, rk, (p[0], p[1] + .08, p[2]), (.18, .07, .18), 8, 3); elipsoid(ag, rk, (p[0], p[1] - .08, p[2]), (.18, .07, .18), 8, 3)   # önizleme: .14 belde kayboluyordu
        elipsoid(ag, KREM, p, (.16, .04, .16), 8, 2)
    ag.yaz('makaron_kusak', 3)

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
    'noel_kemer': noel_kemer, 'simit_bel': simit_bel, 'tutu': tutu, 'seytan_kuyruk': seytan_kuyruk,
    'balkabagi': balkabagi, 'hayalet': hayalet, 'orumcek': orumcek, 'vampir_dis': vampir_dis, 'supurge': supurge,
    'cicek_tac': cicek_tac, 'kelebek_kanat': kelebek_kanat, 'yaprak_etek': yaprak_etek, 'pirilti': pirilti, 'ugur_bocegi': ugur_bocegi, 'tek_boynuz': tek_boynuz,
    'kirmizi_baslik': kirmizi_baslik, 'kurbaga': kurbaga, 'fasulye': fasulye, 'ejderha_kuyruk': ejderha_kuyruk, 'dev_yuzuk': dev_yuzuk,
    'ejderha_kanat': lambda: kanat('ejderha_kanat', '#3f9b4f', YARASA, (('#e8c547', .72),)),
    'fes': fes, 'sombrero': sombrero, 'hasir': hasir, 'viking': viking, 'kavuk': kavuk, 'pos_biyik': pos_biyik, 'saz': saz, 'davul': davul, 'yelpaze': yelpaze,
    'kilim': kilim, 'tef': tef, 'kilt': kilt, 'obi': obi, 'pullu_kusak': pullu_kusak,
    'kabak_kalkan': kabak_kalkan, 'ag_kalkan': ag_kalkan, 'kazan': kazan, 'kedi_kuyruk': kedi_kuyruk,
    'dondurma': dondurma, 'cupcake': cupcake, 'krema_biyik': krema_biyik, 'lolipop': lolipop, 'donut': donut, 'makaron_kusak': makaron_kusak,
}

if __name__ == '__main__':
    for k in (sys.argv[1:] or KALEM):
        for DETAY in (1.0, .8, .65, .5, .4):
            try: KALEM[k](); break
            except Fazla as e: son = e
        else: print('!! SIĞMADI', k, son)
