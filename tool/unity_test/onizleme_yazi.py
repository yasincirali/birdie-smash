# Önizleme karelerine HUD yazılarını (skor, vuruş/olay metinleri, "önizleme" etiketi) basar ve mp4 yapar.
# Kullanım: python3 -I onizleme_yazi.py <ralli.json> <kare_klasörü> <çıktı.mp4> [<yakın_plan_klasörü>]
# 2026-10-24 tam geçiş: kareler hangi çözünürlükte çizilirse çizilsin 720 px genişliğe ölçeklenir (yazı boyları buna göre);
# yakın plan klasörü verilirse ralliden sonra 2× yavaşlatılmış tüy top yakın planı eklenir (uçuşta / vuruşta etiketli).
import sys, json, os, subprocess
from PIL import Image, ImageDraw, ImageFont
R = json.load(open(sys.argv[1])); D = sys.argv[2]; OUT = sys.argv[3]; T = D + '_yazili'; os.makedirs(T, exist_ok=True)
YK = sys.argv[4] if len(sys.argv) > 4 else None
FB = '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'; FN = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
f_big, f_mid, f_small = ImageFont.truetype(FB, 64), ImageFont.truetype(FB, 40), ImageFont.truetype(FN, 24)
QL = {'perfect': 'MÜKEMMEL!', 'good': None, 'poor': None}
def hx(h): h = h.lstrip('#'); return tuple(int(h[k:k + 2], 16) for k in (0, 2, 4))
ME_C, OP_C = hx('#4fb8ff'), hx(R.get('renk', '#ff7a6b'))   # skor renkleri formalarla aynı (HTML gibi)
ev = []
for t in R['texts']:
    if 't' in t: ev.append((t['f'], t['t'], t['w'], (255, 214, 90) if t['t'].isupper() else (255, 255, 255)))
    elif QL.get(t.get('q')) and t['w'] == 'me': ev.append((t['f'], QL[t['q']], t['w'], (140, 255, 170)))
def shadow_text(d, xy, s, f, col, a=255):
    x, y = xy; w = d.textlength(s, font=f); d.text((x - w / 2 + 3, y + 3), s, font=f, fill=(0, 0, 0, int(a * .6))); d.text((x - w / 2, y), s, font=f, fill=col + (a,))
n = len(R['frames'])
for i in range(n):
    p = os.path.join(D, 'k%04d.png' % i)
    if not os.path.exists(p): break
    im = Image.open(p).convert('RGB'); im = im.resize((720, round(720 * im.height / im.width)), Image.LANCZOS).convert('RGBA')
    ov = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov); W, H = im.size
    sc = R['frames'][i]['sc']
    d.rounded_rectangle((W / 2 - 170, 40, W / 2 + 170, 130), 24, fill=(10, 16, 28, 150))
    shadow_text(d, (W / 2 - 95, 52), str(sc[0]), f_big, ME_C); shadow_text(d, (W / 2, 52), '–', f_big, (255, 255, 255)); shadow_text(d, (W / 2 + 95, 52), str(sc[1]), f_big, OP_C)
    shadow_text(d, (W / 2 - 95, 132), 'SEN', f_small, (230, 230, 240)); shadow_text(d, (W / 2 + 95, 132), 'RAKİP', f_small, (230, 230, 240))
    if i < 45 and R.get('rakip'):   # rakip kartı: maç başında 1,5 sn (HTML'deki gibi kim olduğunu söyler)
        a = int(255 * min(1, (45 - i) / 10)); d.rounded_rectangle((W / 2 - 250, H * .2, W / 2 + 250, H * .2 + 90), 22, fill=(10, 16, 28, int(a * .7)))
        shadow_text(d, (W / 2, H * .2 + 22), 'Rakip: ' + R['rakip'], f_mid, OP_C, a)
    last = {}   # aynı taraftaki yazılar üst üste binmesin: her yeni satır bir öncekinin en az 52 px üstünde
    for (f, s, w, col) in ev:
        age = (i - f) / 30
        if 0 <= age < 1.1:
            y = (H * (.62 if w == 'me' else .36)) - age * 90
            if w in last: y = min(y, last[w] - 52)
            last[w] = y
            f = f_mid
            if d.textlength(s, font=f) > W - 60: f = ImageFont.truetype(FB, int(40 * (W - 60) / d.textlength(s, font=f)))   # uzun olay yazısı kenardan taşmasın
            shadow_text(d, (W / 2, y), s, f, col, int(255 * min(1, (1.1 - age) * 2.5)))
    shadow_text(d, (W / 2, H - 92), 'Önizleme: aynı C# simülasyonu ve aynı FBX', f_small, (220, 225, 235), 230)
    shadow_text(d, (W / 2, H - 58), 'Blender ile çizildi, Unity çıktısı değil', f_small, (220, 225, 235), 230)
    Image.alpha_composite(im, ov).convert('RGB').save(os.path.join(T, 'k%04d.png' % i))
n_ana = i + 1
if YK:   # yakın plan: aynı kayıt, kamera topu izler; her kare iki kez (2× yavaş)
    hits = {t['f']: t for t in R['texts'] if 'hit' in t}; kareler = sorted(int(f[1:5]) for f in os.listdir(YK) if f.startswith('k') and f.endswith('.png'))
    out_i = n_ana; son_vurus = None
    for fi in kareler:
        if fi in hits: son_vurus = (fi, hits[fi])
        im = Image.open(os.path.join(YK, 'k%04d.png' % fi)).convert('RGB'); im = im.resize((720, round(720 * im.height / im.width)), Image.LANCZOS).convert('RGBA')
        ov = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov); W, H = im.size
        d.rounded_rectangle((40, 40, W - 40, 150), 24, fill=(10, 16, 28, 160))
        shadow_text(d, (W / 2, 52), 'Yakın plan: tüy top', f_mid, (255, 255, 255))
        vur = son_vurus and fi - son_vurus[0] < 6
        alt = ('VURUŞTA: ' + {'smash': 'smaç', 'clear': 'clear', 'drop': 'drop', 'drive': 'drive'}.get(son_vurus[1]['hit'], son_vurus[1]['hit']) + ', mantar yeni yöne döner') if vur else 'UÇUŞTA: mantar önde, etek arkada'
        shadow_text(d, (W / 2, 104), alt, f_small, (255, 214, 90) if vur else (220, 235, 255))
        shadow_text(d, (W / 2, H - 126), '16 tüy · iki iplik bağı · mantar + mavi şerit · 2× yavaş', f_small, (220, 225, 235), 230)
        shadow_text(d, (W / 2, H - 92), 'Önizleme: aynı C# simülasyonu ve aynı ağ kuralları', f_small, (220, 225, 235), 230)
        shadow_text(d, (W / 2, H - 58), 'Blender ile çizildi, Unity çıktısı değil', f_small, (220, 225, 235), 230)
        fr_ = Image.alpha_composite(im, ov).convert('RGB')
        for _ in range(2): fr_.save(os.path.join(T, 'k%04d.png' % out_i)); out_i += 1
    i = out_i - 1
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', '30', '-i', os.path.join(T, 'k%04d.png'), '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '22', '-movflags', '+faststart', OUT], check=True)
print('yazıldı', OUT, i + 1, 'kare')
