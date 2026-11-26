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

# ---- Saç, yüz ve ayakkabı rafı (2026-11-14, yasin: "klasik, garip duran, dikkat çekici, tüy harcatmaya değecek saç modelleri";
# "ayakkabıda da eksiğimiz var"; öneri artifact WsmAvTCwJa9P1icjwTVioZ, "beğenmediğim yok, yenilerini ekle çeşit olarak").
# Saçlar FBX'e gömülmez (dört tip FBX'ini yeniden üretmek ~2 saat ve her saç her FBX'i büyütür): baş çerçevesinde kabuk ve
# parçalar olarak buraya yazılır, Unity bunları saç seçiliyken kafaya takar (Aksesuar.Kur, karakter.json sac_model[].aks).
# SAC_YER rengindeki alt ağ oyuncunun saç rengiyle boyanır (Aksesuar.SacBoya; "Modelin rengi" seçiliyse bu kahve kalır):
# kabuk saç gibi davranır, brokoli/bulut/alev gibi "saç olmayan" saçlar kendi rengindedir.
SAC_YER = '#5a3a24'
SIY = '#24202b'

def _ss(a, b, x):
    t = max(0., min(1., (x - a) / (b - a))); return t * t * (3 - 2 * t)

def kabuk(ag, renk, alt, rm=lambda e, b: 1., N=20, K=6, c=(0, -.05, -.03), R=(1.10, 1.18, 1.14), kalin=.10, b0=-180, b1=180, yatay=False):
    """Saç kabuğu: baş çerçevesinde elipsoit (c merkez, R yarıçap), boylam b (0 = ön, ±180 = ense) sütunlarında alt kenar alt(b)
    yüksekliğinden tepeye K sıra. rm(e, b) yarıçap çarpanı (dalga, hacim). Alt kenar içe doğru 'kalin' kadar kıvrılır: kabuk
    kenarından bakınca kâğıt değil saç kalınlığı okunur (tasarım dili kural 1)."""
    N, K = D(N, 10), D(K, 3)
    def P(e, b, k=1.):
        er, br = math.radians(e), math.radians(b); s = rm(e, b) * k; sy = k if yatay else s   # yatay: çarpan yalnız x/z'ye (bob alt kenarı yerinde kalır)
        return (c[0] + R[0] * s * math.cos(er) * math.sin(br), c[1] + R[1] * sy * math.sin(er), c[2] + R[2] * s * math.cos(er) * math.cos(br))
    def emin(b): return math.degrees(math.asin(max(-.98, min(.98, (alt(b) - c[1]) / R[1]))))
    tam = b1 - b0 >= 359.9; sut = [b0 + (b1 - b0) * i / N for i in range(N + 1)]
    for i in range(N):
        ba, bb = sut[i], sut[i + 1]; ea, eb = emin(ba), emin(bb)
        for j in range(K):
            e0a, e1a = ea + (88 - ea) * j / K, ea + (88 - ea) * (j + 1) / K
            e0b, e1b = eb + (88 - eb) * j / K, eb + (88 - eb) * (j + 1) / K
            ag.dortgen(renk, P(e0a, ba), P(e0b, bb), P(e1b, bb), P(e1a, ba))
        ag.ucgen(renk, P(e1a, ba), P(e1b, bb), (c[0], c[1] + R[1] * rm(90, 0), c[2]))
        ag.dortgen(renk, P(ea, ba, 1 - kalin), P(eb, bb, 1 - kalin), P(eb, bb), P(ea, ba))
    if not tam:   # yan kesik (yarım kabuk): kenarı da kalın
        for b in (b0, b1):
            e = emin(b)
            for j in range(K): ag.dortgen(renk, P(e + (88 - e) * j / K, b, 1 - kalin), P(e + (88 - e) * j / K, b), P(e + (88 - e) * (j + 1) / K, b), P(e + (88 - e) * (j + 1) / K, b, 1 - kalin))

def kisa_alt(on=-.10, yan=-.42, arka=-.72):
    """Kısa saçın alt kenarı: alında kâkül hizası, kulağın üstünde favori, ensede ense çizgisi."""
    def f(b):
        a = abs(b)
        if a <= 60: return on + (yan - on) * _ss(30, 60, a)
        if a <= 120: return yan
        return yan + (arka - yan) * _ss(120, 165, a)
    return f

def yon_tabani(ax):
    L = math.sqrt(sum(x * x for x in ax)) or 1; ax = [x / L for x in ax]
    t = [1, 0, 0] if abs(ax[0]) < .9 else [0, 1, 0]
    e1 = [ax[1] * t[2] - ax[2] * t[1], ax[2] * t[0] - ax[0] * t[2], ax[0] * t[1] - ax[1] * t[0]]; l1 = math.sqrt(sum(x * x for x in e1)); e1 = [x / l1 for x in e1]
    e2 = [ax[1] * e1[2] - ax[2] * e1[1], ax[2] * e1[0] - ax[0] * e1[2], ax[0] * e1[1] - ax[1] * e1[0]]
    return e1, ax, e2

def egik(ag, renk, c, r, yon, n=8, m=5):
    """Ekseni 'yon' boyunca uzanan elipsoit (r = (en, boy, kalınlık)): tutam, tüy, kulak, pençe."""
    e1, ey, e2 = yon_tabani(yon); g = Ag(); elipsoid(g, renk, (0, 0, 0), r, n, m)
    for k, us in g.alt.items():
        for tri in us: ag.ucgen(k, *[tuple(c[i] + p[0] * e1[i] + p[1] * ey[i] + p[2] * e2[i] for i in range(3)) for p in tri])

def damla(ag, renk, kok, uc, r, n=6):
    """Damla: kökte tombul, uca doğru incelip yuvarlak biten torna gövde (tutam, alev dili, pençe). Sivri uç ve küp topuz yok
    (2026-11-14 önizleme: koni + küçük top uzaktan diken gibi okunuyordu, tasarım dili kural 1)."""
    n = D(n, 5); e1, ax, e2 = yon_tabani([uc[i] - kok[i] for i in range(3)]); L = math.sqrt(sum((uc[i] - kok[i]) ** 2 for i in range(3)))
    prof = ((-.12, .0), (-.04, .80), (.15, 1.0), (.50, .80), (.80, .50), (.95, .26), (1.0, .0))
    hal = [[tuple(kok[k] + ax[k] * L * t + r * f * (math.cos(2 * math.pi * j / n) * e1[k] + math.sin(2 * math.pi * j / n) * e2[k]) for k in range(3)) for j in range(n)] for t, f in prof]
    for s in range(len(prof) - 1):
        for j in range(n): ag.dortgen(renk, hal[s][j], hal[s][(j + 1) % n], hal[s + 1][(j + 1) % n], hal[s + 1][j])

def tutam(ag, renk, kok, uc, r, n=6): damla(ag, renk, kok, uc, r, n)

# ---- Saç ----
def kase():   # kâse kesim: alından enseye düz kesik kenar, kâse gibi hafif açılan alt
    ag = Ag(); kabuk(ag, SAC_YER, lambda b: -.12 if abs(b) < 115 else -.12 - .22 * _ss(115, 170, abs(b)), lambda e, b: 1 + .05 * (1 - math.sin(math.radians(e))), N=22)
    ag.yaz('kase', 0)

def dalgali():   # yandan dalgalı: sol yandan ayrık, sağa yatan üç iri dalga; kâkül sağda alına iner
    ag = Ag(); f = kisa_alt(-.04, -.40, -.70)
    kabuk(ag, SAC_YER, lambda b: f(b) - (.30 * _ss(-20, 35, b) * (1 - _ss(50, 85, b)) if b > -20 else 0),
          lambda e, b: 1 + .13 * max(0., math.sin(math.radians(b * 3 + 40))) ** 1.5 * (.4 + .6 * math.cos(math.radians(e))), N=28, K=7)
    ag.yaz('dalgali', 0)

def perde():   # orta ayrık perde (90'lar): ortadan ikiye ayrılan, yanaklara kıvrılarak inen kâkül
    ag = Ag()
    def alt(b):
        a = abs(b)
        if a < 6: return .30
        if a < 40: return .30 - .85 * _ss(6, 40, a)
        if a < 110: return -.55
        return -.55 - .25 * _ss(110, 165, a)
    kabuk(ag, SAC_YER, alt, lambda e, b: 1 + .06 * _ss(10, 40, abs(b)) * (1 - _ss(40, 90, abs(b))) - .06 * math.exp(-(b / 7) ** 2), N=26, K=6)
    ag.yaz('perde', 0)

def kut_bob():   # küt bob: düz kâkül, yanda ve arkada çene hizasında düz kesik; alt kısım yüzün dışına açılır
    ag = Ag()
    def alt(b):
        a = abs(b)
        return -.14 if a < 50 else -.14 + (-1.0 + .14) * _ss(50, 66, a)
    kabuk(ag, SAC_YER, alt, lambda e, b: 1 + .85 * max(0., -math.sin(math.radians(e))) * _ss(45, 70, abs(b)) + .03, N=26, K=7, yatay=True)
    ag.yaz('kut_bob', 0)

def duz_tepe():   # düz tepe (flat top): yanlar kısa, tepe dümdüz ve yüksek
    ag = Ag()
    def rm(e, b):
        er = math.radians(e); p = 5.
        return 1 / ((abs(math.cos(er)) ** p + abs(math.sin(er) * 1.0) ** p) ** (1 / p))
    kabuk(ag, SAC_YER, kisa_alt(-.08, -.25, -.55), rm, R=(1.06, 1.42, 1.10), c=(0, -.08, -.03), N=18, K=6)
    ag.yaz('duz_tepe', 0)

def daginik():   # yataktan kalkmış: kısa kabuk, dört yöne dikilmiş tombul tutamlar
    ag = Ag(); kabuk(ag, SAC_YER, kisa_alt(), N=16, K=4)
    for (x, y, z, ux, uy, uz) in ((-.45, .95, .25, -.7, .9, .2), (.10, 1.12, .30, .15, 1, .35), (.55, 1.0, -.05, .85, .8, -.1),
                                 (-.15, 1.05, -.50, -.25, .9, -.75), (.42, .78, -.70, .45, .5, -.9), (-.80, .55, -.25, -1, .45, -.3)):
        L = math.sqrt(ux * ux + uy * uy + uz * uz); tutam(ag, SAC_YER, (x, y, z), (x + ux / L * .55, y + uy / L * .55, z + uz / L * .55), .26)
    ag.yaz('daginik', 0)

def kivircik():   # kıvırcık kısa: başa yakın, iri kıvrım tümsekleri
    ag = Ag(); kabuk(ag, SAC_YER, kisa_alt(-.06, -.38, -.66), lambda e, b: 1 + .14 * abs(math.sin(math.radians(b * 4)) * math.sin(math.radians(e * 4 + 20))) ** .7, N=26, K=8)
    ag.yaz('kivircik', 0)

def bukle():   # dev bukle: kısa saç, alnın ortasında tek kocaman halka bukle
    ag = Ag(); kabuk(ag, SAC_YER, kisa_alt(-.02), N=16, K=4)
    yol = []
    for k in range(12):
        a = math.radians(90 + k * 32); rr = .30 - .016 * k
        yol.append(((rr * math.cos(a) * .9, .22 + rr * math.sin(a), 1.16 + .015 * k), .14 - .005 * k))
    boru(ag, SAC_YER, [((0, .75, .85), .15), ((0, .60, 1.06), .15)] + yol, 8)
    ag.yaz('bukle', 0)

def kalp_topuz():   # kalp topuz: tepede kalp biçimli topuz (iki yuvarlak + aşağı sivrilen gövde, uç saça gömülü)
    ag = Ag(); kabuk(ag, SAC_YER, kisa_alt(), N=16, K=4)
    nok = kalp_nokta((0, 0), .50); g = Ag(); bombe(g, SAC_YER, nok, 0, .06, .20, 2, False); bombe(g, SAC_YER, nok, 0, -.06, -.20, 2, False)
    for k, us in g.alt.items():
        for tri in us: ag.ucgen(k, *[(p[0], p[1] + 1.50, p[2] - .12) for p in tri])
    halka(ag, '#e0457b', .20, .07, 1.08, 12, 4, (0, 0, -.12))   # kurdele bağı: topuzun saça tutunduğu yer
    ag.yaz('kalp_topuz', 0)

def anten():   # uğur böceği antenleri: kısa saçtan çıkan iki kalın sap, uçlarında benekli kırmızı top
    ag = Ag(); kabuk(ag, SAC_YER, kisa_alt(), N=16, K=4)
    for sx in (-1, 1):
        boru(ag, SIY, [((sx * .28, .95, .20), .09), ((sx * .40, 1.35, .30), .085), ((sx * .60, 1.70, .28), .08)], 6)
        top(ag, '#e3262c', (sx * .66, 1.82, .27), .17, 9, 6)
        top(ag, SIY, (sx * .66 + sx * .05, 1.90, .40), .055, 5, 3)
    ag.yaz('anten', 0)

def brokoli():   # brokoli: koyu yeşil sap kabuğu, tepede kabarık çiçek kümeleri
    ag = Ag(); kabuk(ag, '#2f6b3a', kisa_alt(-.02, -.35, -.65), N=14, K=3)
    for (x, y, z, r) in ((0, 1.25, .05, .48), (-.55, 1.0, .10, .36), (.55, 1.0, .05, .36), (0, 1.0, -.55, .40), (-.40, .85, .55, .30), (.40, .85, .55, .30), (-.55, .8, -.45, .30), (.55, .8, -.45, .30)):
        elipsoid(ag, '#5aa548', (x, y, z), (r, r * .85, r), 7, 4)
    for (x, y, z) in ((-.15, 1.62, .20), (.25, 1.55, -.15), (-.35, 1.3, -.35)): top(ag, '#76c25a', (x, y, z), .12, 5, 3)
    ag.yaz('brokoli', 0)

def ibik():   # tavuk ibiği: kısa saç, tepede alından enseye kırmızı ibik
    ag = Ag(); kabuk(ag, SAC_YER, kisa_alt(), N=16, K=4)
    for k, (e, s) in enumerate(((60, .18), (78, .25), (96, .27), (114, .24), (132, .18))):
        er = math.radians(e); y, z = 1.10 * math.sin(er) - .02, 1.10 * math.cos(er)
        elipsoid(ag, '#e3262c', (0, y + s * .7, z), (.12, s * 1.3, s * .85), 8, 5)
    ag.yaz('ibik', 0)

def spagetti():   # spagetti: başın üstünde makarna yığını, iri tel tel sarkan makarnalar, domates sosu ve köfteler
    ag = Ag(); SAR = '#f2c94c'
    kabuk(ag, SAR, kisa_alt(.0, -.20, -.40), lambda e, b: 1 + .05 * abs(math.sin(math.radians(b * 6 + e * 3))), N=18, K=4)
    for k in range(7):   # iri, düz sarkan makarna telleri (zikzak tel uzaktan toka gibi okunuyordu)
        b = math.radians(-150 + k * 50); x, z = math.sin(b), math.cos(b)
        if abs(math.degrees(b)) < 55: continue   # yüzün önüne sarkmaz
        boru(ag, SAR, [((x * 1.18, .08, z * 1.18), .095), ((x * 1.16, -.30, z * 1.16), .095), ((x * 1.12, -.62, z * 1.12), .085)], 6)
        top(ag, SAR, (x * 1.12, -.62, z * 1.12), .085, 6, 3)
    elipsoid(ag, '#d6342f', (0, 1.13, -.05), (.62, .22, .58), 9, 4)
    for (x, z) in ((-.25, .10), (.28, -.05), (0, -.32)): top(ag, '#7a4428', (x, 1.30, z), .16, 7, 4)
    ag.yaz('spagetti', 0)

def ruzgar():   # rüzgâr tüneli: smaçın ortasında donmuş gibi geriye savrulmuş saç; arkaya uzanan iri tutamlar
    ag = Ag()
    kabuk(ag, SAC_YER, kisa_alt(.02, -.40, -.60), lambda e, b: 1 + .55 * _ss(110, 180, abs(b)) * _ss(0, 30, e) * (1 - _ss(60, 88, e)) + .08 * _ss(60, 120, abs(b)), N=24, K=6)
    for (x, y, l, r) in ((-.30, .70, .85, .20), (.30, .70, .85, .20), (0, .35, 1.0, .22), (-.55, .25, .65, .16), (.55, .25, .65, .16)):
        tutam(ag, SAC_YER, (x, y, -1.20), (x * 1.3, y + .18, -1.20 - l), r)
    ag.yaz('ruzgar', 0)

def gokkusagi():   # gökkuşağı: alından enseye dört renkli ibik yay (kısa saçın üstünde, varlik kisa)
    ag = Ag()
    for k, renk in enumerate(('#e94a4a', '#f2c230', '#4fb35a', '#4a7fe0')):
        R = 1.62 - k * .15; yol = []
        for i in range(13):
            a = math.radians(20 + i * (140 / 12)); yol.append(((0, -.05 + R * math.sin(a), .05 + R * math.cos(a) * .95), .085))
        boru(ag, renk, yol, 6)
    ag.yaz('gokkusagi', 0)

def bulut():   # bulut: başın üstünde pamuk bulut, yanında ona yapışık küçük bulut (havada durmaz)
    ag = Ag(); BB, GB = '#f4f7ff', '#c9d7f5'
    for (x, y, z, r) in ((0, 1.15, 0, .64), (-.62, .80, .15, .44), (.62, .80, .10, .44), (0, .80, -.62, .48), (0, .66, .55, .38)):
        elipsoid(ag, BB if y > .7 else GB, (x, y, z), (r, r * .82, r), 8, 5)
    for k in range(6):
        b = math.radians(30 + k * 60); elipsoid(ag, GB, (1.0 * math.sin(b), .30, 1.0 * math.cos(b) * (.8 if math.cos(b) > 0 else 1)), (.34, .28, .34), 7, 4)
    for (x, y, z, r) in ((1.05, 1.28, -.10, .30), (1.32, 1.18, -.10, .22), (.85, 1.14, -.10, .22)): elipsoid(ag, BB, (x, y, z), (r, r * .8, r), 7, 4)
    ag.yaz('bulut', 0)

def tuy_topuz():   # tüy top topuzu: kısa saç, tepe arkasında topuz, topuza saplı tüy top tüyleri gibi beş beyaz tüy ve renkli bant
    ag = Ag(); kabuk(ag, SAC_YER, kisa_alt(), N=16, K=4)
    c = (0, 1.20, -.35); elipsoid(ag, SAC_YER, c, (.36, .30, .36), 10, 6)
    halka(ag, '#e99bff', .30, .07, c[1] - .12, 12, 5, (0, 0, c[2]))
    for k in range(7):
        a = 2 * math.pi * k / 7; d = (math.sin(a) * .45, 1., math.cos(a) * .45)
        L = math.sqrt(sum(x * x for x in d)); d = tuple(x / L for x in d)
        egik(ag, '#fbfbf6', (c[0] + d[0] * .62, c[1] + .18 + d[1] * .45, c[2] + d[2] * .62), (.17, .40, .05), d, 6, 3)
    elipsoid(ag, '#e9d7b8', (0, c[1] + .26, c[2]), (.20, .13, .20), 8, 4)   # mantar: tüylerin dibinde
    ag.yaz('tuy_topuz', 0)

def alev():   # alev: turuncu alev dilleri yukarı ve geriye yalar, içte sarı; kabuk kızıl
    ag = Ag(); T, S, K = '#ff7a1a', '#ffd23f', '#d6342f'
    kabuk(ag, T, kisa_alt(.0, -.35, -.62), N=14, K=3)
    for (x, z, h, r, renk) in ((0, .25, .95, .34, T), (-.45, .05, .75, .30, T), (.45, .05, .75, .30, T), (0, -.45, .80, .38, T)):
        damla(ag, renk, (x, .95 - abs(x) * .3, z), (x * 1.25, .95 + h, z - .35), r, 5)
    for (x, z, h, r) in ((0, .20, .60, .18), (0, -.40, .45, .16)): damla(ag, S, (x, 1.12, z + .1), (x * 1.2, 1.12 + h, z - .2), r, 5)
    ag.yaz('alev', 0)

# ---- Yüz (baş çerçevesi; yüz yüzeyi Aksesuar.Yuz ölçüsü: göz y −.4 z 1,0 x ±.32; burun ucu y −.8 z 1,08; dudak y −.9…−1,0 z 1,06;
# yanak x .5 y −.68 z .87; çene y −1,25 z .98) ----
ALTN = '#e2b93b'
def tasi(ag, d=(0, 0, 0), s=1., c=(0, 0, 0)):
    """Önizleme düzeltmesi (2026-11-14, aks_giy gerçek oyuncak başında): ağın tamamını c çevresinde s ile büyütüp d kadar kaydırır."""
    for k, us in ag.alt.items(): ag.alt[k] = [tuple(tuple(c[i] + (p[i] - c[i]) * s + d[i] for i in range(3)) for p in tri) for tri in us]
GOZ_YUK = (0, .07, .02)   # gözlükler: ölçülen göz hizası (−.4) önizlemede camı göz altına koyuyordu; hafif yukarı ve öne
def gozluk_sapi(ag, renk, r=.032):
    for sx in (-1, 1): boru(ag, renk, [((sx * .58, -.38, .93), r), ((sx * .80, -.36, .55), r), ((sx * .92, -.34, .10), r)], 5)

def nefes_bandi():
    ag = Ag(); egik(ag, '#fbfbf6', (0, -.60, 1.10), (.06, .24, .035), (1, 0, -.12), 8, 3); elipsoid(ag, '#d8d2e6', (0, -.60, 1.115), (.07, .045, .02), 6, 2); tasi(ag, (0, .02, .01), 1.5, (0, -.60, 1.10)); ag.yaz('nefes_bandi', 0)

def yara_bandi():   # yanakta çapraz yara bandı
    ag = Ag(); B = '#f2c8a0'
    egik(ag, B, (.50, -.68, .93), (.05, .20, .03), (.8, .6, -.25), 7, 3); egik(ag, B, (.50, -.68, .935), (.05, .20, .03), (.8, -.6, -.25), 7, 3)
    elipsoid(ag, '#fbfbf6', (.50, -.68, .955), (.07, .07, .02), 6, 2); tasi(ag, (0, 0, .02), 1.9, (.50, -.68, .93)); ag.yaz('yara_bandi', 0)

def kalin_kas():
    ag = Ag()
    for sx in (-1, 1): egik(ag, SAC_YER, (sx * .32, -.18, 1.02), (.06, .19, .05), (1, sx * .22, -.25 * sx), 8, 4)
    tasi(ag, (0, .02, .07), 1.3, (0, -.18, 1.02)); ag.yaz('kalin_kas', 0)

def kare_gozluk():   # kalın siyah kare çerçeve (Yuvarlak gözlük ince ve yuvarlak; bu kalın ve köşeli)
    ag = Ag()
    for sx in (-1, 1):
        x0, x1, y0, y1, z0, z1 = sx * .14, sx * .54, -.56, -.26, .99, 1.07
        xa, xb = min(x0, x1), max(x0, x1)
        kutu(ag, SIY, (xa, y1 - .07, z0), (xb, y1, z1)); kutu(ag, SIY, (xa, y0, z0), (xb, y0 + .07, z1))
        kutu(ag, SIY, (xa, y0, z0), (xa + .07, y1, z1)); kutu(ag, SIY, (xb - .07, y0, z0 - .04), (xb, y1, z1 - .04))
    kutu(ag, SIY, (-.15, -.38, 1.02), (.15, -.33, 1.08)); gozluk_sapi(ag, SIY, .035)
    tasi(ag, GOZ_YUK, 1.12, (0, -.40, 1.0)); ag.yaz('kare_gozluk', 0)

def spor_gozluk():   # saran tek parça turuncu cam
    ag = Ag(); yol = []
    for i in range(11):
        b = math.radians(-78 + i * 15.6); yol.append(((1.08 * math.sin(b), -.40, 1.08 * math.cos(b) * .98), .12 if abs(i - 5) < 4 else .09))
    boru(ag, '#ff8a3d', yol, 6); gozluk_sapi(ag, SIY, .03)
    for sx in (-1, 1): elipsoid(ag, '#ffd2b0', (sx * .30, -.34, 1.17), (.10, .025, .02), 6, 2)
    tasi(ag, (0, .09, 0)); ag.yaz('spor_gozluk', 0)

def kecisakal():   # keçi sakalı: çenede kısa, yuvarlak sakal (saç rengini alır)
    ag = Ag(); elipsoid(ag, SAC_YER, (0, -1.24, 1.00), (.18, .17, .13), 8, 5); elipsoid(ag, SAC_YER, (0, -1.42, .95), (.12, .13, .10), 7, 4); tasi(ag, (0, .02, .03), 1.25, (0, -1.3, .98)); ag.yaz('kecisakal', 0)

def monokl():   # sağ gözde altın monokl, kalın çerçeve ve kısa zincir topları
    ag = Ag(); halka_z(ag, ALTN, (.32, -.40, 1.06), .17, .04, 16, 5); disk_z(ag, '#cfe8ff', (.32, -.40, 1.04), .15, 12)
    for k in range(4): top(ag, ALTN, (.46 + .05 * k, -.56 - .09 * k, 1.00 - .07 * k), .03, 5, 3)
    ag.yaz('monokl', 0)

def disk_z(ag, renk, c, r, n=12):
    n = D(n, 6)
    for i in range(n):
        a0 = 2 * math.pi * i / n; a1 = 2 * math.pi * (i + 1) / n
        ag.ucgen(renk, c, (c[0] + r * math.cos(a1), c[1] + r * math.sin(a1), c[2]), (c[0] + r * math.cos(a0), c[1] + r * math.sin(a0), c[2]))

def duduk():   # hakem düdüğü: ağızda sarı düdük, yuvarlak hazne yanda
    ag = Ag(); S = '#ffd23f'
    silindir(ag, S, (0, -.97, 1.04), (.20, -.99, 1.24), .07, 8)
    elipsoid(ag, S, (.30, -1.0, 1.28), (.14, .12, .14), 9, 5); top(ag, '#c99a12', (.30, -1.0, 1.43), .045, 5, 3)
    tasi(ag, (0, 0, 0), 1.35, (0, -.97, 1.04)); ag.yaz('duduk', 0)

def pilot():   # pilot gözlüğü: damla biçimli koyu cam, ince altın çerçeve, çift köprü
    ag = Ag()
    for sx in (-1, 1):
        nok = [(sx * .32 + .19 * math.cos(math.radians(a)), -.42 + (.13 if math.sin(math.radians(a)) > 0 else .18) * math.sin(math.radians(a))) for a in range(0, 360, 30)]
        cx, cy = sx * .32, -.42; g = Ag(); bombe(g, '#3a4a5c', [(x - cx, y - cy) for x, y in nok], .0, .03, .06, 2)
        for k, us in g.alt.items():
            for tri in us: ag.ucgen(k, *[(p[0] + cx, p[1] + cy, p[2] + 1.0) for p in tri])
        boru(ag, ALTN, [((x, y, 1.03), .022) for x, y in nok + nok[:1]], 4)
    for y in (-.30, -.36): silindir(ag, ALTN, (-.14, y, 1.06), (.14, y, 1.06), .02, 4)
    gozluk_sapi(ag, ALTN, .022)
    tasi(ag, GOZ_YUK, 1.1, (0, -.40, 1.0)); ag.yaz('pilot', 0)

def yildiz_nokta(c, R, r, a0=90):
    return [(c[0] + (R if i % 2 == 0 else r) * math.cos(math.radians(a0 + i * 36)), c[1] + (R if i % 2 == 0 else r) * math.sin(math.radians(a0 + i * 36))) for i in range(10)]

def cerceve_cam(ag, renk, nokta, c, z):
    g = Ag(); bombe(g, renk, [(x - c[0], y - c[1]) for x, y in nokta], .0, .05, .09, 2)
    for k, us in g.alt.items():
        for tri in us: ag.ucgen(k, *[(p[0] + c[0], p[1] + c[1], p[2] + z) for p in tri])

def yildiz_gozluk():
    ag = Ag()
    for sx in (-1, 1): cerceve_cam(ag, '#ffd23f', yildiz_nokta((sx * .33, -.40), .25, .12), (sx * .33, -.40), 1.0)
    silindir(ag, '#ffd23f', (-.12, -.36, 1.06), (.12, -.36, 1.06), .03, 5); gozluk_sapi(ag, '#c99a12', .03)
    tasi(ag, GOZ_YUK, 1.25, (0, -.40, 1.0)); ag.yaz('yildiz_gozluk', 0)

def kalp_nokta(c, s):
    return [(c[0] + s * 16 * math.sin(t) ** 3 / 16, c[1] + s * (13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)) / 16) for t in [2 * math.pi * i / 16 for i in range(16)]]

def kalp_gozluk():
    ag = Ag()
    for sx in (-1, 1): cerceve_cam(ag, '#ff5d8f', kalp_nokta((sx * .33, -.40), .21), (sx * .33, -.40), 1.0)
    silindir(ag, '#ff5d8f', (-.12, -.34, 1.06), (.12, -.34, 1.06), .03, 5); gozluk_sapi(ag, '#c2386a', .03)
    tasi(ag, GOZ_YUK, 1.25, (0, -.40, 1.0)); ag.yaz('kalp_gozluk', 0)

def dalgic():   # dalgıç maskesi: iki gözü ve burnu örten oval cam, siyah lastik kayış başı sarar, sağda şnorkel
    ag = Ag()
    nok = [(.62 * math.cos(math.radians(a)), -.42 + .25 * math.sin(math.radians(a)) - (.08 if math.sin(math.radians(a)) < -.5 else 0)) for a in range(0, 360, 24)]
    cerceve_cam(ag, '#9fd8f0', nok, (0, -.42), 1.02)
    boru(ag, '#3c4048', [((x, y, 1.04), .045) for x, y in nok + nok[:1]], 4)
    halka(ag, '#3c4048', 1.03, .05, -.38, 18, 4, (0, 0, -.02))
    boru(ag, '#ff8a3d', [((.20, -.98, 1.08), .055), ((.62, -.90, .82), .055), ((.98, -.40, .40), .055), ((1.02, .35, .30), .055), ((1.00, .75, .30), .07)], 6)
    tasi(ag, (0, .05, 0)); ag.yaz('dalgic', 0)

def sakiz():   # sakız balonu: ağızdan şişmiş pembe balon, parlak benek
    ag = Ag(); top(ag, '#ff8fc0', (0, -1.0, 1.34), .30, 12, 7); top(ag, '#ffd0e6', (-.11, -.89, 1.58), .06, 6, 3); ag.yaz('sakiz', 0)

def tuy_biyik():   # tüy bıyık: dudak üstünden iki yana açılan tüy top tüyleri, ortada mantar düğüm
    ag = Ag()
    for sx in (-1, 1):
        for k, a in enumerate((-25, 0, 25)):
            d = (sx * math.cos(math.radians(a)), math.sin(math.radians(a)) * .6 - .1, -.25)
            egik(ag, '#fbfbf6', (sx * .26 + d[0] * .12, -.88 + d[1] * .12, 1.06), (.08, .22, .03), d, 6, 3)
    elipsoid(ag, '#e9d7b8', (0, -.86, 1.12), (.09, .08, .07), 7, 4)
    tasi(ag, (0, 0, .02), 1.6, (0, -.88, 1.06)); ag.yaz('tuy_biyik', 0)

def vizor():   # robot vizör: gözleri saran camgöbeği şerit, yanlarda gri kulak kutuları
    ag = Ag(); yol = []
    # r ön izleme (2026-11-14): tek boru alında bant gibi okundu; üç sıra yassı bir cam şerit verir, gözleri örter
    for y in (-.30, -.40, -.50):
        yol = [((1.08 * math.sin(math.radians(-80 + i * 20)), y, 1.08 * math.cos(math.radians(-80 + i * 20))), .085) for i in range(9)]
        boru(ag, '#46e0ff', yol, 5)
    for sx in (-1, 1): elipsoid(ag, '#8a90a6', (sx * 1.0, -.40, .05), (.16, .26, .26), 8, 5); top(ag, '#46e0ff', (sx * 1.15, -.40, .05), .06, 5, 3)
    tasi(ag, (0, .09, 0)); ag.yaz('vizor', 0)

# ---- Ayakkabı görünümü (tur 4, 2026-11-14): her ayağa ayrı takılır (Aksesuar.Ayak). Çerçeve: köken ekipman ayakkabısının tabanı
# ortası (ayak yere basarken), +y yukarı, +z burun, birim = ayakkabı boyu (x ayakkabı enine göre gerilir: x ±.5 = ekipman
# ayakkabısının eni). Ekipman ayakkabısı gizlenir; görünüm onun hacmini (x ±.5, z ±.5, y 0–.40) örter. Hiçbir parça y < 0'a
# inmez (zemine girer), bu yüzden yay, tekerlek ve bulut tabanın İÇİNDE durur. Sol ve sağ aynı ağ (eşyalar simetrik).
def ayak_iz(s=1., n=14, burun=.06):
    """Taban izi: süper elips, burun tarafı hafif geniş; (x, z) listesi."""
    out = []
    for i in range(n):
        a = 2 * math.pi * i / n; ca, sa = math.cos(a), math.sin(a)
        x = .5 * s * math.copysign(abs(ca) ** .7, ca) * (1 + burun * max(0, sa)); z = .5 * s * math.copysign(abs(sa) ** .8, sa)
        out.append((x, z))
    return out

def prizma(ag, renk, iz, y0, y1, c=(0, 0)):
    n = len(iz); a = [(c[0] + x, y0, c[1] + z) for x, z in iz]; b = [(c[0] + x, y1, c[1] + z) for x, z in iz]
    mx = (c[0], y0, c[1]); my = (c[0], y1, c[1])
    for i in range(n):
        j = (i + 1) % n; ag.dortgen(renk, a[i], a[j], b[j], b[i]); ag.ucgen(renk, b[i], b[j], my); ag.ucgen(renk, a[j], a[i], mx)

def ust_kubbe(ag, renk, iz, y0, h, merkez=(0, -.12), halka=3, ac=0.):
    """Ayakkabı sayası: iz kenarından merkeze doğru yükselen kubbe (merkez bilek tarafına kayık). ac > 0: tepe açık (bilek ağzı)."""
    n = len(iz); mx, mz = merkez
    def P(i, t):
        x, z = iz[i % n]; k = 1 - t * (1 - ac) if ac else 1 - t
        return (mx + (x - mx) * k, y0 + h * math.sin(t * math.pi / 2), mz + (z - mz) * k)
    for s in range(halka):
        t0, t1 = s / halka, (s + 1) / halka
        for i in range(n):
            if t1 >= 1 and not ac: ag.ucgen(renk, P(i, t0), P(i + 1, t0), (mx, y0 + h, mz))
            else: ag.dortgen(renk, P(i, t0), P(i + 1, t0), P(i + 1, t1), P(i, t1))

def kanvas():   # kanvas: beyaz kauçuk taban, kırmızı şerit, lacivert bez saya, beyaz lastik burun
    ag = Ag(); iz = ayak_iz()
    prizma(ag, '#f4f1ea', iz, 0, .10); prizma(ag, '#e94a4a', ayak_iz(1.01), .10, .13)
    ust_kubbe(ag, '#2f4a8a', ayak_iz(.96), .13, .30); elipsoid(ag, '#f4f1ea', (0, .14, .30), (.42, .14, .22), 9, 3, 0, 90)
    halka(ag, '#f4f1ea', .17, .045, .42, 12, 4, (0, 0, -.16))
    ag.yaz('kanvas', 4)

def tombul_taban():   # tombul taban: iki kat kalın yumuşak beyaz taban, mavi saya
    ag = Ag(); prizma(ag, '#f4f1ea', ayak_iz(1.04), 0, .12); prizma(ag, '#d8d2e6', ayak_iz(1.04), .12, .14); prizma(ag, '#f4f1ea', ayak_iz(1.02), .14, .22)
    ust_kubbe(ag, '#5a7bd8', ayak_iz(.95), .22, .24); halka(ag, '#f4f1ea', .16, .045, .45, 12, 4, (0, 0, -.16))
    ag.yaz('tombul_taban', 4)

def cizme():   # yağmur çizmesi: sarı lastik, koyu taban, baldıra çıkan konç
    ag = Ag(); S = '#ffcf33'
    prizma(ag, '#3c4048', ayak_iz(1.02), 0, .07); ust_kubbe(ag, S, ayak_iz(.98), .07, .26)
    silindir(ag, S, (0, .18, -.14), (0, .80, -.16), .26, 12); halka(ag, '#e0a91c', .26, .06, .80, 14, 4, (0, 0, -.16))
    ag.yaz('cizme', 4)

def yuksek_bilek():   # yüksek bilekli basketbol botu: kırmızı saya, beyaz taban ve bilek bandı, yanlarda yuvarlak arma
    ag = Ag(); K = '#e94a4a'
    prizma(ag, '#f4f1ea', ayak_iz(1.02), 0, .09); ust_kubbe(ag, K, ayak_iz(.97), .09, .28)
    silindir(ag, K, (0, .20, -.14), (0, .62, -.15), .25, 12); halka(ag, '#f4f1ea', .25, .055, .62, 14, 4, (0, 0, -.15))
    for sx in (-1, 1): elipsoid(ag, '#f4f1ea', (sx * .44, .30, -.12), (.03, .11, .11), 8, 3)
    ag.yaz('yuksek_bilek', 4)

def takunya():   # takunya: tahta Hollanda takunyası, kalkık burun, üstte boyalı kırmızı çiçek
    ag = Ag(); T = '#e2b46a'
    prizma(ag, '#b98545', ayak_iz(1.04), 0, .06); ust_kubbe(ag, T, ayak_iz(1.03), .06, .34, (0, -.18))
    elipsoid(ag, T, (0, .15, .36), (.34, .15, .24), 9, 4)
    for k in range(5): a = math.radians(90 + k * 72); top(ag, '#e94a4a', (.07 * math.cos(a), .38, .12 + .07 * math.sin(a)), .045, 5, 3)
    top(ag, '#f4f1ea', (0, .40, .12), .04, 5, 3)
    ag.yaz('takunya', 4)

def elf():   # elf pabucu: yeşil saya, burnu yukarı kıvrılır, ucunda altın çıngırak; bilekte kırmızı kenar
    ag = Ag(); Y = '#3fa34d'
    prizma(ag, '#2e7d3b', ayak_iz(1.0), 0, .07); ust_kubbe(ag, Y, ayak_iz(.96), .07, .28)
    boru(ag, Y, [((0, .16, .38), .14), ((0, .22, .56), .10), ((0, .36, .70), .07), ((0, .52, .68), .05), ((0, .60, .56), .04)], 7)
    top(ag, '#f2c230', (0, .60, .50), .07, 7, 4); halka(ag, '#d6342f', .18, .06, .40, 12, 4, (0, 0, -.16))
    ag.yaz('elf', 4)

def kabak_pabuc():   # kabak pabuç: dilimli turuncu balkabağı, burunda kıvrık yeşil sap
    ag = Ag(); T = '#ff8a2a'
    prizma(ag, '#5a3a24', ayak_iz(1.0), 0, .06)
    for sx in (-.24, 0, .24): elipsoid(ag, T, (sx, .26, 0), (.30, .25, .52), 10, 5)
    boru(ag, '#3f8f3a', [((0, .40, .25), .06), ((0, .52, .32), .05), ((.08, .58, .22), .04)], 6)
    ag.yaz('kabak_pabuc', 4)

def ordek_terlik():   # ördek terlik: sarı ördek gövdesi, burunda baş, turuncu gaga, gözler
    ag = Ag(); S = '#ffd23f'
    prizma(ag, '#ff8a3d', ayak_iz(1.02), 0, .06); elipsoid(ag, S, (0, .20, -.04), (.54, .30, .58), 11, 5, 0, 90)
    top(ag, S, (0, .44, .26), .22, 10, 6); egik(ag, '#ff8a3d', (0, .40, .50), (.13, .14, .06), (0, 0, 1), 8, 3)
    for sx in (-1, 1): top(ag, SIY, (sx * .11, .52, .42), .04, 5, 3)
    ag.yaz('ordek_terlik', 4)

def tavsan_patik():   # tavşan patik: pofuduk pembe patik, bilekte iki tavşan kulağı
    ag = Ag(); P_ = '#f7c3d6'
    prizma(ag, '#f2a8c4', ayak_iz(1.02), 0, .07)
    for (x, z, r) in ((0, .22, .34), (-.22, -.10, .28), (.22, -.10, .28), (0, -.30, .30)): elipsoid(ag, P_, (x, .26, z), (r, r * .75, r), 8, 5)
    for sx in (-1, 1): egik(ag, P_, (sx * .17, .62, .18), (.13, .36, .07), (sx * .3, 1, .25), 8, 4); egik(ag, '#ff8fb4', (sx * .18, .63, .245), (.07, .25, .02), (sx * .3, 1, .25), 6, 2)
    ag.yaz('tavsan_patik', 4)

def paten():   # tekerlekli paten: kırmızı bot, gri taban plakası, altında dört beyaz tekerlek (tekerlek yere değer, zemine girmez)
    ag = Ag()
    for z in (-.30, .30):
        silindir(ag, '#8a90a6', (-.40, .10, z), (.40, .10, z), .035, 5)
        for sx in (-1, 1): silindir(ag, '#f4f1ea', (sx * .24, .10, z), (sx * .44, .10, z), .10, 8)
    prizma(ag, '#8a90a6', ayak_iz(.98), .14, .19); ust_kubbe(ag, '#e94a4a', ayak_iz(.95), .19, .26)
    silindir(ag, '#e94a4a', (0, .28, -.14), (0, .62, -.15), .24, 10); halka(ag, '#f4f1ea', .24, .05, .62, 12, 4, (0, 0, -.15))
    ag.yaz('paten', 4)

def dinozor():   # dinozor ayağı: yeşil tombul ayak, önde üç beyaz pençe, sırtında turuncu dikenler
    ag = Ag(); Y = '#4fae4a'
    prizma(ag, '#2f7a3a', ayak_iz(1.02), 0, .06); ust_kubbe(ag, Y, ayak_iz(1.0), .06, .30)
    for sx in (-.26, 0, .26): tutam(ag, '#f4f1ea', (sx, .12, .40), (sx * 1.15, .08, .66), .11)
    for k, z in enumerate((.16, -.06, -.28)): tutam(ag, '#ff8a3d', (0, .28 + .04 * k, z), (0, .50 + .05 * k, z - .08), .11)
    ag.yaz('dinozor', 4)

def yayli():   # yaylı: tabanın içinde kalın bir yay, üstünde mor bot (yay zemine girmez; zıplamayı değiştirmez)
    ag = Ag(); disk(ag, '#3c4048', (0, .005, 0), .32, 12); prizma(ag, '#3c4048', ayak_iz(.7), 0, .03)
    yol = [((.20 * math.cos(t), .03 + .23 * t / (5 * math.pi), .20 * math.sin(t) - .05), .045) for t in [i * math.pi / 4 for i in range(21)]]
    boru(ag, '#c0c4d6', yol, 5)
    prizma(ag, '#2a1d3a', ayak_iz(1.0), .26, .30); ust_kubbe(ag, '#7a5cd6', ayak_iz(.96), .30, .22)
    ag.yaz('yayli', 4)

def roket():   # roket: kırmızı bot, beyaz burun, mavi lomboz, topukta iki gri egzoz
    ag = Ag()
    prizma(ag, '#8a90a6', ayak_iz(1.0), 0, .08); ust_kubbe(ag, '#e94a4a', ayak_iz(.97), .08, .30)
    elipsoid(ag, '#f4f1ea', (0, .10, .32), (.40, .14, .20), 9, 3, 0, 90)
    for sx in (-1, 1): elipsoid(ag, '#7cc4ff', (sx * .46, .24, 0), (.03, .08, .08), 6, 3); kesik(ag, '#8a90a6', (sx * .16, .16, -.44), (sx * .19, .13, -.62), .08, .12, 8)
    ag.yaz('roket', 4)

def bulut_taban():   # bulut taban: tabanın çevresinde yapışık pamuk bulutlar, üstünde orkide saya
    ag = Ag(); iz = ayak_iz(1.0, 10)
    for i, (x, z) in enumerate(iz): elipsoid(ag, '#f4f7ff', (x * 1.02, .10, z * 1.02), (.16, .11, .16), 6, 4)
    prizma(ag, '#c9d7f5', ayak_iz(.92), .02, .14); ust_kubbe(ag, '#e99bff', ayak_iz(.94), .14, .28)
    ag.yaz('bulut_taban', 4)

YENI_SAC_YUZ_AYAK = {
    'kase': kase, 'dalgali': dalgali, 'perde': perde, 'kut_bob': kut_bob, 'duz_tepe': duz_tepe, 'daginik': daginik, 'kivircik': kivircik,
    'bukle': bukle, 'kalp_topuz': kalp_topuz, 'anten': anten, 'brokoli': brokoli, 'ibik': ibik, 'spagetti': spagetti, 'ruzgar': ruzgar,
    'gokkusagi': gokkusagi, 'bulut': bulut, 'tuy_topuz': tuy_topuz, 'alev': alev,
    'nefes_bandi': nefes_bandi, 'yara_bandi': yara_bandi, 'kalin_kas': kalin_kas, 'kare_gozluk': kare_gozluk, 'spor_gozluk': spor_gozluk,
    'kecisakal': kecisakal, 'monokl': monokl, 'duduk': duduk, 'pilot': pilot, 'yildiz_gozluk': yildiz_gozluk, 'kalp_gozluk': kalp_gozluk,
    'dalgic': dalgic, 'sakiz': sakiz, 'tuy_biyik': tuy_biyik, 'vizor': vizor,
    'kanvas': kanvas, 'tombul_taban': tombul_taban, 'cizme': cizme, 'yuksek_bilek': yuksek_bilek, 'takunya': takunya, 'elf': elf,
    'kabak_pabuc': kabak_pabuc, 'ordek_terlik': ordek_terlik, 'tavsan_patik': tavsan_patik, 'paten': paten, 'dinozor': dinozor,
    'yayli': yayli, 'roket': roket, 'bulut_taban': bulut_taban,
}


# ---- Hayvan tipleri (2026-11-15, yasin "bunları yap": yeni karakter tipi yönü HAYVAN önce) ----
# Yeni FBX yok (tip başına ~3,4 MB olurdu): hayvan = ortak oyuncak bedeni + başı tümden örten hayvan kafası (tur 0, baş çerçevesi)
# + kulak (ayrı kalem: şapka takılınca gizlenir) + kuyruk (tur 3, bel çerçevesi) + gövde atlasında ten hücresinin kürk rengine
# boyanması (PlayerView.TenliAtlas). Tip başına üç küçük .bytes. Kafa saç kabuğu kadar yüksek (tepe ≈ 1,1) ki şapkalar saçlı
# baştaki gibi otursun; yüz düz (burun çıkıntısı yok): yüz aksesuarları yüzeye oturur. Ölçü (aks_giy YUZ_OLC, saçsız baş): tepe
# .7, yan 1.0 (kulak), ön 1.07 (burun y −.9), çene y −1.5 z .85; kabuk hepsini dışarıda bırakır. İlk önizlemede dar ve uzun kabuk (1,25 × 1,50) yumurta gibi okundu; yuvarlak
# ve geniş baş (1,46) maskot gibi duruyor, şapkalar Aksesuar.Kur'da HV_SAPKA ile büyütülür.
HV_C, HV_R = (0, -.36, .03), (1.46, 1.46, 1.38)
def hv_z(x, y, e=.0):
    return HV_C[2] + HV_R[2] * math.sqrt(max(0, 1 - (x / HV_R[0]) ** 2 - ((y - HV_C[1]) / HV_R[1]) ** 2)) + e
def hv_yama(ag, renk, c, rx, ry, e=.03, aci=0, n=12):
    """Kafa yüzeyine yapışık oval yama (göz, yanak, burun çevresi): merkezden yelpaze, kenarda içe dönük etek (havada durmaz)."""
    n = D(n, 8); ca, sa = math.cos(math.radians(aci)), math.sin(math.radians(aci))
    ks = [(c[0] + rx * math.cos(2 * math.pi * i / n) * ca - ry * math.sin(2 * math.pi * i / n) * sa, c[1] + rx * math.cos(2 * math.pi * i / n) * sa + ry * math.sin(2 * math.pi * i / n) * ca) for i in range(n)]
    m = (c[0], c[1], hv_z(c[0], c[1], e)); on = [(x, y, hv_z(x, y, e)) for x, y in ks]; ar = [(x, y, hv_z(x, y, -.02)) for x, y in ks]
    for i in range(n): j = (i + 1) % n; ag.ucgen(renk, m, on[i], on[j]); ag.dortgen(renk, on[i], ar[i], ar[j], on[j])
def hv_kafa(ag, kurk):
    elipsoid(ag, kurk, HV_C, HV_R, 16, 9, -54, 90)
def hv_goz(ag, koyu, isik, x=.56, y=-.40, rx=.16, ry=.22):
    for s in (-1, 1):
        hv_yama(ag, koyu, (s * x, y), rx, ry, .035); hv_yama(ag, isik, (s * x - .05, y + .08), .055, .06, .055, n=8)
def hv_agiz(ag, koyu, y=-.80, w=.15):
    for s in (-1, 1):
        boru(ag, koyu, [((0, y, hv_z(0, y, .03)), .022), ((s * w * .5, y - .09, hv_z(s * w * .5, y - .09, .03)), .022), ((s * w, y - .06, hv_z(s * w, y - .06, .03)), .018)], 5)
def hv_burun(ag, renk, y=-.72, r=(.12, .085, .07)):
    elipsoid(ag, renk, (0, y, hv_z(0, y, .0)), r, 8, 4)

def hv_kedi_kafa():
    ag = Ag(); K, A, S, P = '#f0993e', '#fbe9cf', '#2b2430', '#f2a1b5'
    hv_kafa(ag, K); hv_yama(ag, A, (0, -.86), .50, .33, .02, n=14); hv_goz(ag, S, A); hv_burun(ag, P); hv_agiz(ag, S)
    ag.yaz('hv_kedi_kafa', 0)
def hv_kedi_kulak():
    ag = Ag(); K, P = '#f0993e', '#f2a1b5'
    for s in (-1, 1):
        koni(ag, K, (s * .80, .66, 0), (s * 1.02, 1.52, .0), .50, 8); koni(ag, P, (s * .81, .78, .22), (s * .98, 1.32, .14), .27, 6)
    ag.yaz('hv_kedi_kulak', 0)
def hv_kedi_kuyruk():
    ag = Ag(); K, A = '#f0993e', '#fbe9cf'
    yol = [((0, -.08, -.42), .10), ((0, -.16, -.72), .10), ((0, -.02, -1.02), .095), ((0, .30, -1.18), .09), ((0, .66, -1.16), .085), ((0, .92, -.98), .08), ((0, .98, -.74), .075)]
    boru(ag, K, yol, 8); top(ag, A, (0, .95, -.66), .10, 8, 4)
    ag.yaz('hv_kedi_kuyruk', 3)

def hv_panda_kafa():
    ag = Ag(); B, S = '#f7f5ef', '#2e2c33'
    hv_kafa(ag, B)
    for s in (-1, 1):
        hv_yama(ag, S, (s * .56, -.44), .26, .35, .03, s * 22, 12); hv_yama(ag, B, (s * .52, -.38), .10, .11, .05, n=8); hv_yama(ag, S, (s * .51, -.39), .055, .06, .065, n=8)
    hv_burun(ag, S, -.78, (.13, .09, .07)); hv_agiz(ag, S, -.86)
    ag.yaz('hv_panda_kafa', 0)
def hv_panda_kulak():
    ag = Ag(); S = '#2e2c33'
    for s in (-1, 1): elipsoid(ag, S, (s * 1.0, .86, -.02), (.42, .42, .26), 10, 5)
    ag.yaz('hv_panda_kulak', 0)
def hv_panda_kuyruk():
    ag = Ag(); top(ag, '#f7f5ef', (0, -.10, -.56), .20, 8, 5); ag.yaz('hv_panda_kuyruk', 3)

def hv_tavsan_kafa():
    ag = Ag(); K, S, P, B = '#efe9e4', '#2b2430', '#f2a1b5', '#ffffff'
    hv_kafa(ag, K); hv_goz(ag, S, B)
    for s in (-1, 1): hv_yama(ag, P, (s * .82, -.72), .15, .11, .025, n=10)
    hv_burun(ag, P, -.70, (.10, .07, .06)); hv_agiz(ag, S, -.77, .13)
    for s in (-1, 1): hv_yama(ag, B, (s * .055, -.95), .05, .075, .04, n=8)
    ag.yaz('hv_tavsan_kafa', 0)
def hv_tavsan_kulak():
    ag = Ag(); K, P = '#efe9e4', '#f2a1b5'
    for s in (-1, 1):
        egik(ag, K, (s * .52, 1.66, -.06), (.14, .86, .26), (s * .16, 1, -.06), 8, 5); egik(ag, P, (s * .53, 1.66, .06), (.07, .62, .15), (s * .16, 1, -.06), 6, 4)
    ag.yaz('hv_tavsan_kulak', 0)
def hv_tavsan_kuyruk():
    ag = Ag(); top(ag, '#ffffff', (0, -.08, -.58), .24, 8, 5); ag.yaz('hv_tavsan_kuyruk', 3)

# ---- Robot tipi (2026-11-15, yön notu: hayvandan sonra robot; aynı tarif, ön ek yine hv_ = "başı örten tip parçası") ----
# Kask kabuğu hayvanla aynı ölçü (şapka/yüz eşyası sarmalayıcıları değişmeden çalışsın). Yüz bir vizör bandı: büyük yama
# merkezden yelpazeyle kürenin içine gömülüyordu (kiriş), bu yüzden bant ızgara olarak yüzeye serilir.
def hv_bant(ag, renk, x0, x1, y0, y1, e=.035, nx=8, ny=3):
    P = lambda i, j, ee: (x0 + (x1 - x0) * i / nx, y0 + (y1 - y0) * j / ny, hv_z(x0 + (x1 - x0) * i / nx, y0 + (y1 - y0) * j / ny, ee))
    for i in range(nx):
        for j in range(ny): ag.dortgen(renk, P(i, j, e), P(i + 1, j, e), P(i + 1, j + 1, e), P(i, j + 1, e))
        ag.dortgen(renk, P(i, 0, -.02), P(i + 1, 0, -.02), P(i + 1, 0, e), P(i, 0, e)); ag.dortgen(renk, P(i, ny, e), P(i + 1, ny, e), P(i + 1, ny, -.02), P(i, ny, -.02))
    for j in range(ny):
        ag.dortgen(renk, P(0, j, e), P(0, j + 1, e), P(0, j + 1, -.02), P(0, j, -.02)); ag.dortgen(renk, P(nx, j, -.02), P(nx, j + 1, -.02), P(nx, j + 1, e), P(nx, j, e))
RB_M, RB_K, RB_I, RB_T = '#a9bccd', '#1f2a38', '#62e6ff', '#ff8a3c'   # metal, vizör/kablo, ışık, turuncu vurgu
def hv_robot_kafa():
    ag = Ag(); hv_kafa(ag, RB_M); hv_bant(ag, RB_K, -.98, .98, -.72, -.10)
    for s in (-1, 1):
        hv_yama(ag, RB_I, (s * .50, -.40), .17, .20, .07, n=10); hv_yama(ag, RB_T, (s * 1.02, -.92), .085, .085, .03, n=8)
    hv_bant(ag, RB_K, -.30, .30, -1.04, -.90, .03, 4, 1)   # ağız ızgarası: koyu yuva + üç ışık çizgisi
    for x in (-.17, 0, .17): hv_bant(ag, RB_I, x - .045, x + .045, -1.01, -.93, .05, 1, 1)
    ag.yaz('hv_robot_kafa', 0)
def hv_robot_kulak():   # anten + yan kulaklık kapakları; şapka takılınca gizlenir (anten şapkayı delmesin)
    ag = Ag(); silindir(ag, RB_K, (0, 1.04, 0), (0, 1.52, 0), .075, 6); top(ag, RB_T, (0, 1.66, 0), .21, 8, 5)
    for s in (-1, 1): silindir(ag, RB_K, (s * 1.34, -.36, .03), (s * 1.60, -.36, .03), .34, 10); silindir(ag, RB_T, (s * 1.58, -.36, .03), (s * 1.64, -.36, .03), .15, 8)
    ag.yaz('hv_robot_kulak', 0)
def hv_robot_kuyruk():   # şarj kablosu: ucunda fiş
    ag = Ag()
    boru(ag, RB_K, [((0, -.08, -.42), .055), ((0, -.30, -.70), .055), ((0, -.36, -1.0), .055), ((0, -.14, -1.24), .055), ((0, .20, -1.28), .055)], 6)
    kutu(ag, RB_T, (-.12, .18, -1.38), (.12, .40, -1.18))
    for s in (-1, 1): kutu(ag, RB_M, (s * .06 - .02, .40, -1.31), (s * .06 + .02, .56, -1.25))
    ag.yaz('hv_robot_kuyruk', 3)

# --- Yapay zekâ hamam böceği (2026-11-19, yasin: "Char olarak ai hamam böceği karakterini ekleyelim"). Okuma: sevimli, tombul bir
#     böcek ve "yapay zekâ" olduğunu ışıklarıyla söylüyor (iri camgöbeği gözler, alında çip, anten uçlarında ışık). Gerçekçi böcek
#     DEĞİL (tiksindirici ayrıntı yok: kıl, çok bacak, parlak yağlı kabuk yok); belirli bir karakterin kopyası da değil. Tarif
#     robotla aynı: baş kabuğu + anten (şapkayla gizlenir) + belde kanat kabuğu. ≤4 renk, kozmetik tasarım dili "tombul oyuncak".
BK_K, BK_A, BK_S, BK_I, BK_Y = '#9a5630', '#5e2f18', '#1f2a38', '#62e6ff', '#d9a06a'   # kestane kabuk, koyu kabuk, göz yuvası/çip, ışık, açık yüz
def hv_bocek_kafa():
    ag = Ag(); hv_kafa(ag, BK_K); hv_bant(ag, BK_Y, -.96, .96, -1.16, -.02, .015, 8, 4)   # açık yüz plakası (ızgara yama: büyük yelpaze yama kürenin içine batıyor)
    for s in (-1, 1):
        hv_yama(ag, BK_S, (s * .54, -.38), .30, .34, .035, n=12); hv_yama(ag, BK_I, (s * .54, -.40), .19, .23, .06, n=10)
        hv_yama(ag, BK_S, (s * .50, -.44), .075, .10, .075, n=8)   # göz bebeği: bakış içe ve hafif aşağı (sevimli)
    hv_bant(ag, BK_S, -.20, .20, .02, .22, .035, 4, 1)   # alın çipi: koyu yuva + üç ışık
    for x in (-.12, 0, .12): hv_bant(ag, BK_I, x - .035, x + .035, .07, .17, .055, 1, 1)
    hv_agiz(ag, BK_S, -.90, .24)
    ag.yaz('hv_bocek_kafa', 0)
def hv_bocek_kulak():   # iki uzun anten: öne-yana açılıp geriye kıvrılır, ucunda ışık; şapka takılınca gizlenir
    ag = Ag()
    for s in (-1, 1):
        boru(ag, BK_A, [((s * .38, .92, .30), .10), ((s * .62, 1.50, .34), .09), ((s * 1.02, 1.98, .16), .08), ((s * 1.42, 2.20, -.20), .07), ((s * 1.66, 2.14, -.58), .065)], 6)
        top(ag, BK_I, (s * 1.70, 2.10, -.70), .17, 8, 4)
    ag.yaz('hv_bocek_kulak', 0)
def hv_bocek_kuyruk():   # belde kanat kabuğu: iki yarım, ortada dikiş, altta küçük şarj ışığı
    ag = Ag()
    for s in (-1, 1): elipsoid(ag, BK_K, (s * .22, .55, -.60), (.38, .74, .24), 8, 5)
    boru(ag, BK_A, [((0, 1.24, -.60), .04), ((0, .55, -.83), .04), ((0, -.14, -.62), .04)], 5)
    top(ag, BK_I, (0, -.10, -.76), .08, 6, 3)
    ag.yaz('hv_bocek_kuyruk', 3)

HAYVAN = {f.__name__: f for f in (hv_bocek_kafa, hv_bocek_kulak, hv_bocek_kuyruk, hv_robot_kafa, hv_robot_kulak, hv_robot_kuyruk, hv_kedi_kafa, hv_kedi_kulak, hv_kedi_kuyruk, hv_panda_kafa, hv_panda_kulak, hv_panda_kuyruk, hv_tavsan_kafa, hv_tavsan_kulak, hv_tavsan_kuyruk)}


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
# ---- Kort seti (2026-11-21, yasin: "yeni saç kıyafet ve aksesuarların eklenmesi ... oyun boyutunun makul düzeyde tutulması"):
# oyunun kendi dünyası: tüy top, raket çantası, ter havlusu. Her kalem ~20 KB (.bytes), boyut bütçesi maskot klip paylaşımının
# (~20 MB) yanında yok sayılır. Çerçeveler yukarıdaki bölümlerle aynı (baş, sırt, kol, bel).
TUY, MANTAR, IP = '#fbfbf6', '#e9d7b8', '#d6342f'
def dev_tuy():   # dev tüy top: mantar başlık kask gibi başı sarar (yan kenar 1,16 > saç), tepesinden açılan on dört tüylük etek, iki iplik sırası
    ag = Ag()
    kesik(ag, MANTAR, (0, .40, 0), (0, .92, 0), 1.17, 1.08, 16); elipsoid(ag, MANTAR, (0, .92, 0), (1.08, .30, 1.08), 16, 4, 0, 90)
    halka(ag, IP, 1.14, .06, .78, 16, 4)
    for k in range(14):
        a = 2 * math.pi * k / 14; d = (math.sin(a) * .55, 1., math.cos(a) * .55); L = math.sqrt(sum(x * x for x in d)); d = tuple(x / L for x in d)
        egik(ag, TUY, (math.sin(a) * (.62 + .55 * .40), 1.12 + .40 * L * .9, math.cos(a) * (.62 + .55 * .40)), (.30, .50, .06), d, 5, 3)
    halka(ag, IP, .92, .05, 1.32, 14, 3); halka(ag, IP, 1.14, .05, 1.64, 14, 3)   # tüyleri bağlayan iki iplik sırası
    ag.yaz('dev_tuy', 0)

def raket_canta():   # raket çantası: sırtta çapraz termo çanta, açık fermuar şeridi, üst ucundan iki raket sapı bakar
    ag = Ag(); C, F, G = '#26324f', '#cfe84a', '#f2f2f2'
    a, b = (-.42, -.62, -.20), (.40, .26, -.20); d = [b[i] - a[i] for i in range(3)]; L = math.sqrt(sum(x * x for x in d)); u = [x / L for x in d]
    q = lambda t, dz=0: (a[0] + u[0] * t, a[1] + u[1] * t, a[2] + u[2] * t + dz)
    silindir(ag, C, q(.10), q(L - .10), .17, 10); elipsoid(ag, C, q(.10), (.17, .17, .17), 8, 4); elipsoid(ag, C, q(L - .10), (.17, .17, .17), 8, 4)
    silindir(ag, F, q(.12, -.16), q(L - .12, -.16), .03, 5)   # fermuar: çantanın dış yüzünde boydan boya
    for k, dz in ((0, -.05), (1, .05)):   # iki sap: çantanın üst ucundan dışarı, uçlarında kapak
        o = (-.06 + .12 * k) * 1.0; p0 = q(L - .04, dz); p0 = (p0[0] - u[1] * o, p0[1] + u[0] * o, p0[2])
        p1 = (p0[0] + u[0] * .26, p0[1] + u[1] * .26, p0[2]); silindir(ag, G, p0, p1, .035, 6); top(ag, IP if k == 0 else F, p1, .045, 6, 3)
    ag.yaz('raket_canta', 1)

def havlu():   # ter havlusu: sağ yanda bele sıkıştırılmış beyaz havlu (şortun dışında), alt ucunda iki renkli şerit, üstte kıvrılmış kenar
    ag = Ag(); B, S1, S2 = '#f4f4ee', '#3a8fd9', '#cfe84a'
    kutu(ag, B, (.92, -.35, -.30), (1.06, .18, .22))
    kutu(ag, B, (.88, .12, -.31), (1.10, .27, .23))   # bel lastiğinin üstüne kıvrılan kenar
    kutu(ag, S1, (1.055, -.28, -.305), (1.08, -.20, .225)); kutu(ag, S2, (1.055, -.17, -.305), (1.08, -.09, .225))
    ag.yaz('havlu', 3)

def tuy_kalkan():   # tüy top kalkanı: top tabandan bakılmış hali; tırtıklı beyaz tüy halkası, kırmızı iplik bandı, ortada mantar
    ag = Ag()
    dis = [((.50 if i % 2 == 0 else .42) * math.cos(2 * math.pi * i / 24), (.50 if i % 2 == 0 else .42) * math.sin(2 * math.pi * i / 24)) for i in range(24)]
    bombe(ag, TUY, dis, 0, .04, .06); halka_z(ag, IP, (0, 0, .07), .30, .035, 18, 4); halka_z(ag, IP, (0, 0, .07), .40, .025, 18, 4)
    bombe(ag, MANTAR, cember(.20, 14), .06, .12, .22, 3, False)
    kayis(ag); ag.yaz('tuy_kalkan', 2)

def yan_prizma(ag, renk, nokta, t):   # (z, y) dışbükey çokgen, x ekseninde ±t kalınlık (başın ortasından geçen dik düzlemde levha)
    on = [(t, y, z) for z, y in nokta]; ar = [(-t, y, z) for z, y in nokta]; n = len(nokta)
    for i in range(1, n - 1): ag.ucgen(renk, on[0], on[i], on[i + 1]); ag.ucgen(renk, ar[0], ar[i + 1], ar[i])
    for i in range(n): k = (i + 1) % n; ag.dortgen(renk, on[i], ar[i], ar[k], on[k])

def yildirim():   # yıldırım perçem: kısa kesim, tepede öne bakan kalın zikzak şimşek (iki üçgen üst üste kayık; smaç şimşeği)
    ag = Ag(); kabuk(ag, SAC_YER, kisa_alt(-.08, -.42, -.70), N=16, K=4)
    yan_prizma(ag, SAC_YER, [(.60, 1.00), (-.35, 1.00), (.72, 1.72)], .22)            # alt üçgen: tabanı tepeye gömülü, ucu öne-yukarı
    yan_prizma(ag, SAC_YER, [(.98, 1.46), (.14, 1.46), (-.46, 2.26)], .20)            # üst üçgen: geriye-yukarı sivrilir
    ag.yaz('yildirim', 0)

KORT = {'dev_tuy': dev_tuy, 'raket_canta': raket_canta, 'havlu': havlu, 'tuy_kalkan': tuy_kalkan, 'yildirim': yildirim}
KALEM.update(YENI_SAC_YUZ_AYAK); KALEM.update(HAYVAN); KALEM.update(KORT)

if __name__ == '__main__':
    for k in (sys.argv[1:] or KALEM):
        for DETAY in (1.0, .8, .65, .5, .4):
            try: KALEM[k](); break
            except Fazla as e: son = e
        else: print('!! SIĞMADI', k, son)
