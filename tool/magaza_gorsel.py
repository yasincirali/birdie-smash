#!/usr/bin/env python3
# Mağaza görselleri adım 1 (2026-10-29, Ar-Ge 27 Eki madde 1): ekran turunun mağaza kipi karelerinden (Game/MagazaTuru.cs,
# CI: ekran-sonuc/rN/magaza/) App Store görselleri üretir. Başlık yazısı burada eklenir, Unity'de değil: metin değişince
# oyun yeniden derlenmez, iki dil aynı karenin üstüne yazılır.
#   python3 tool/magaza_gorsel.py <kare klasörü> [çıktı = store_listing/gorsel]
# Çıktı: <çıktı>/<dil>/<boy>/<NN>_<ad>.jpg, boy 6.9 (1320×2868) ve 6.5 (1284×2778). JPEG %92: PNG kare başı ~4 MB, depo
# büyümesin; App Store ikisini de kabul eder, saydamlık yok.
# Yerleşim: üstte Gün Batımı zemini (#1A1221, menüyle aynı) üstünde başlık (Barlow Condensed ExtraBold Italic, oyunun
# başlık yazısı) ve güneş turuncusu çizgi; altta oyun karesi %80 ölçekte, yuvarlak köşeli. Kare kırpılmaz: HUD (skor,
# ralli) okunur kalır.
# 6,5": aynı tuval 1284 genişliğe ölçeklenir (yükseklik 2790), ortadan 2778'e kırpılır (en-boy farkı %0,4; yalnız zemin
# kırpılır, kare ve başlık değil).
# Smaç sahnesinin iki adayı (_a, _b) vardır; SECIM'de hangisi kullanılacağı yazılır (gözle seçilir).
import os, sys
from PIL import Image, ImageDraw, ImageFont

KOK = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONT = os.path.join(KOK, 'unity', 'BirdieSmash', 'Assets', 'Resources', 'fonts')
ZEMIN, GUNES, YAZI = (0x1A, 0x12, 0x21), (0xFF, 0xA0, 0x4D), (0xF3, 0xEA, 0xEE)
W, H = 1320, 2868
SECIM = {'01_smac': '01_smac_a'}
BASLIK = {
    '01_smac':     {'tr': 'Sıçra, smaçla,\nsayıyı bitir',         'en': 'Jump. Smash.\nWin the point.'},
    '02_ralli':    {'tr': 'Uzun ralliler,\ncoşan tribünler',       'en': 'Long rallies,\nroaring crowds'},
    '03_kariyer':  {'tr': 'Yerel salondan\nDünya Finallerine',     'en': 'From the local hall\nto the World Finals'},
    '04_karakter': {'tr': 'Oyuncunu ve\nraketini seç',            'en': 'Pick your player\nand your racket'},
    '05_imza':     {'tr': 'Her rakibin bir\nimza vuruşu var',      'en': 'Every rival has a\nsignature shot'},
}

def yuvarla(im, r):
    m = Image.new('L', im.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, im.size[0] - 1, im.size[1] - 1), r, fill=255)
    out = Image.new('RGB', im.size, ZEMIN); out.paste(im, (0, 0), m); return out, m

def tuval(kare, metin):
    c = Image.new('RGB', (W, H), ZEMIN); d = ImageDraw.Draw(c)
    f = ImageFont.truetype(os.path.join(FONT, 'BarlowCondensed-ExtraBoldItalic.ttf'), 132)
    ust = 150; d.multiline_text((W // 2, ust), metin, font=f, fill=YAZI, anchor='ma', align='center', spacing=10)
    alt = d.multiline_textbbox((W // 2, ust), metin, font=f, anchor='ma', align='center', spacing=10)[3]
    d.rounded_rectangle((W // 2 - 70, alt + 40, W // 2 + 70, alt + 52), 6, fill=GUNES)
    y0 = alt + 110; kh = H - y0 - 70; k = kh / kare.size[1]; kw = int(kare.size[0] * k)
    kare = kare.convert('RGB').resize((kw, kh), Image.LANCZOS); kare, m = yuvarla(kare, 56)
    c.paste(kare, ((W - kw) // 2, y0), m); return c

def main():
    if len(sys.argv) < 2: print(__doc__ or 'kullanım: magaza_gorsel.py <kare klasörü> [çıktı]'); sys.exit(2)
    src = sys.argv[1]; dst = sys.argv[2] if len(sys.argv) > 2 else os.path.join(KOK, 'store_listing', 'gorsel'); n = 0
    for dil in ('tr', 'en'):
        for sahne, b in BASLIK.items():
            ad = SECIM.get(sahne, sahne); p = os.path.join(src, f'{dil}_{ad}.png')
            if not os.path.exists(p): p = os.path.join(src, f'{dil}_{ad}_yok.png')
            if not os.path.exists(p): print('YOK:', dil, sahne); continue
            kare = Image.open(p)
            if kare.size != (W, H): print(f'UYARI {dil}_{ad}: kare {kare.size}, beklenen {(W, H)}')
            c = tuval(kare, b[dil])
            for boy, (bw, bh) in (('6.9', (W, H)), ('6.5', (1284, 2778))):
                o = c if boy == '6.9' else c.resize((bw, round(H * bw / W)), Image.LANCZOS)
                if o.size[1] != bh: t = (o.size[1] - bh) // 2; o = o.crop((0, t, bw, t + bh))
                od = os.path.join(dst, dil, boy); os.makedirs(od, exist_ok=True)
                o.save(os.path.join(od, f'{sahne}.jpg'), quality=92, optimize=True); n += 1
    print(n, 'görsel →', dst)

if __name__ == '__main__':
    main()
