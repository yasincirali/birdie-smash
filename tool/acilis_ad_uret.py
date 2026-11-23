# Açılış perdesindeki oyun adı (2026-11-19, yasin: "splash gifinde birdie smash ismi gözüküp kaybolmalı mı? oyun adını
# vurgulamak adına"; aynı gün: "daha fancy bir şekilde, tasarım dilini bozmadan"; giriş "smaç vuruşu" seçildi). Üç şeffaf
# PNG katman, hepsi aynı boyda ve hizada: acilis_ad_birdie, acilis_ad_smash, acilis_ad_yay (ayrı girer: önce "Birdie" kayar,
# top durunca "Smash" darbeyle oturur, yay onunla belirir). Aynı dosyaları iki yer okur ki devir görünmesin: BirdieAcilis.mm
# (motor açılmadan, Data/Raw) ve Hud.AdCiz (StreamingAssets). Resources'a konmaz: orası ASTC'ye sıkıştırır, yazı kenarı
# bozulur. Betik "Smash"ın katman içindeki merkezini yazar: darbe ölçeği o noktadan, Hud AD_SMASH_M ve BirdieAcilis.mm
# BS_AD_SMASH_M bu sayıyla aynı tutulur. Kaynak değişince yeniden koş.
# Tasarım kararları (docs/arayuz_tasarim_dili.md §8 palet sabit, §10 oyunun kendi dili, jenerik parıltı yok):
# - Yazı menüdeki marka adıyla aynı (Baloo 2 ExtraBold). "Birdie" --tx krem, "Smash" --gold: iki ton adın iki hecesini
#   ayırır, turuncu (--sun) kullanılmaz çünkü o renk yalnız birincil eylemindir.
# - Koyu bordo kontur (#4a0a22, "Hazırlanıyor…" zemini) + aynı renkte katı derinlik (aşağı taşan blok, bulanık gölge değil):
#   ad dönen halkaların açık sarısına da koyu kırmızısına da düşebilir, kontur her halkada okunurluk verir; derinlik oyunun
#   tombul çizgi film figürleriyle aynı "oyuncak" hissi.
# - Altında tüy topun uçuş yayı: soldan ince başlayıp kalınlaşan, sağda yükselen eğri (badmintonun kendi çizgisi, süs değil:
#   üstteki topun yolunu söyler). Hafif −4° eğim "smaç" enerjisi; daha fazlası okunurluğu düşürür.
# Gerek: python3 + Pillow. Kullanım (depo kökünden): python3 tool/acilis_ad_uret.py
import os, math
from PIL import Image, ImageDraw, ImageFont
A = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'unity', 'BirdieSmash', 'Assets') + os.sep
S = 240                                   # yazı boyu (px)
f = ImageFont.truetype(A + 'Resources/fonts/Baloo2-ExtraBold.ttf', S)
KONTUR, DERIN = 16, 14                    # boyun ~%7'si ve ~%6'sı
# 2026-11-23 açılış A (yasin: "giriş splash gifiyle menü renkleri uyumsuz"): Stadyum Pop'a geçti. "Birdie" beyaz, "Smash"
# menünün tek vurgusu sarı #FFC93C, kontur ve derinlik menüdeki mürekkep #1B2340 (adlar eski kalsın diye değişken adları aynı).
KREM, ALTIN, BORDO = (255, 255, 255, 255), (255, 201, 60, 255), (27, 35, 64, 255)
d = ImageDraw.Draw(Image.new('L', (1, 1)))
k1, k2 = 'Birdie ', 'Smash'
w1 = d.textlength(k1, font=f)
x0, y0, x1, y1 = d.textbbox((0, 0), k1 + k2, font=f, stroke_width=KONTUR)
pad = 40
Wd, Hd = int(x1 - x0) + 2 * pad, int(y1 - y0) + DERIN + 2 * pad + 110   # alt pay: uçuş yayı
ox, oy = pad - x0, pad - y0

# uçuş yayı: yazının altından, ince uçtan kalın uca; konturu ve derinliği yazıyla aynı
def yay(kalin, renk, dy):
    N = 96; C = []
    for i in range(N + 1):
        t = i / N
        C.append((pad + 30 + t * (Wd - 2 * pad - 60), Hd - pad - 30 - 105 * t * t + dy, 3 + (kalin - 3) * math.sin(t * math.pi * .5)))
    L, R = [], []
    for i, (x, y, r) in enumerate(C):   # kenarlar normal boyunca: tek çokgen, segment birleşimi dalgalanmaz
        xa, ya, _ = C[max(i - 1, 0)]; xb, yb, _ = C[min(i + 1, N)]; dx, dyy = xb - xa, yb - ya; n = math.hypot(dx, dyy)
        nx, ny = -dyy / n, dx / n; L.append((x + nx * r, y + ny * r)); R.append((x - nx * r, y - ny * r))
    g.polygon(L + R[::-1], fill=renk)
    for x, y, r in (C[0], C[-1]): g.ellipse((x - r, y - r, x + r, y + r), fill=renk)
def katman(parca):
    global g
    im = Image.new('RGBA', (Wd, Hd), (0, 0, 0, 0)); g = ImageDraw.Draw(im)
    if parca == 'yay':
        for e in range(DERIN, 0, -2): yay(12 + KONTUR, BORDO, e)
        yay(12 + KONTUR, BORDO, 0); yay(12, KREM, 0)
    else:
        k, x, renk = (k1, 0, KREM) if parca == 'birdie' else (k2, w1, ALTIN)
        for e in range(DERIN, 0, -2): g.text((ox + x, oy + e), k, font=f, fill=BORDO, stroke_width=KONTUR, stroke_fill=BORDO)
        g.text((ox + x, oy), k, font=f, fill=renk, stroke_width=KONTUR, stroke_fill=BORDO)
    return im.rotate(4, resample=Image.BICUBIC, expand=True)

L = {p: katman(p) for p in ('birdie', 'smash', 'yay')}
tam = Image.new('RGBA', L['yay'].size, (0, 0, 0, 0))
for v in L.values(): tam.alpha_composite(v)
bb = tam.getbbox()
for p, v in L.items():
    v = v.crop(bb); b = Image.new('RGBA', (v.width + 16, v.height + 16), (0, 0, 0, 0)); b.paste(v, (8, 8))   # süzgeç taşmasın
    out = A + 'StreamingAssets/acilis_ad_' + p + '.png'; b.save(out, optimize=True); print(out, b.size, os.path.getsize(out))
    if p == 'smash':
        x0, y0, x1, y1 = b.getbbox(); print('Smash merkezi (katmana oranla): %.3f, %.3f' % ((x0 + x1) / 2 / b.width, (y0 + y1) / 2 / b.height))
