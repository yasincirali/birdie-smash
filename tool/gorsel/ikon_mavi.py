# Uygulama ikonu ve menü logosu Stadyum Pop mavisine (2026-11-26, yasin: "Logoyu da splashteki temaya uygun hale getirelim").
# Açılış perdesi 23 Kas'ta gün batımı halkalarından maviye geçti (Hud.PERDE_C); ikon hâlâ kırmızı-turuncu halkalardaydı ve
# ana ekranda dokunulan ilk görüntü ile açılış farklı renkte kalıyordu. Kompozisyon aynı (halkalar + tüy top), yalnız zemin:
# sıcak tonlu (kırmızı-sarı) doygun pikseller parlaklığa göre PERDE_C dizisine eşlenir; dıştaki en koyu halka = menü zemini
# #2659BE, merkez parıltısı en açık mavi. Tüy top (beyaz/gri), yeşil bant ve siyah ipler doygun-sıcak olmadığı için dokunulmaz.
# Kaynak: git'teki eski ikon (bir kez çalışır; tekrar çalıştırmak maviyi yeniden eşlemez çünkü mavi sıcak değil).
# Kullanım: python3 tool/gorsel/ikon_mavi.py giris.png cikis.png
import colorsys, sys
from PIL import Image

# (eski renk parlaklığı, yeni renk) — eski ikonun sekiz düz rengi (dört halka) + beyaz parıltı, Hud.PERDE_C sırası (dıştan içe).
# İkonda dört halka var, perdede beş: ortadaki perde tonu (#4282e0/#4d8de6) atlandı ki merkez perdedeki gibi açık, kontrast yüksek kalsın.
def hx(h): return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
def lum(c): return (.2126 * c[0] + .7152 * c[1] + .0722 * c[2]) / 255
ESKI = [(168, 36, 75), (194, 49, 82), (240, 84, 90), (255, 111, 94), (255, 140, 85), (255, 166, 90), (255, 207, 106), (255, 224, 138), (255, 255, 255)]
YENI = ["#2659BE", "#2b62c8", "#2f69cf", "#3874d8", "#64a3ef", "#72aef3", "#a6d3ff", "#bfe0ff", "#e8f4ff"]
CIFT = sorted(zip([lum(c) for c in ESKI], [hx(h) for h in YENI]))

def esle(L):
    if L <= CIFT[0][0]: return CIFT[0][1]
    for (l0, c0), (l1, c1) in zip(CIFT, CIFT[1:]):
        if L <= l1:
            t = (L - l0) / (l1 - l0); return tuple(round(a + (b - a) * t) for a, b in zip(c0, c1))
    return CIFT[-1][1]

def sicak(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return s > .22 and (h >= 320 / 360 or h <= 55 / 360)

def main(giris, cikis):
    im = Image.open(giris); kip = im.mode; im = im.convert("RGBA"); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and sicak(r, g, b): px[x, y] = esle(lum((r, g, b))) + (a,)
    # Kaynağın kipi korunur: App Store 1024 ikonu alfa kanalsız olmalı (RGB), menü logosu saydam köşeli (RGBA).
    (im if kip == "RGBA" else im.convert(kip)).save(cikis)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
