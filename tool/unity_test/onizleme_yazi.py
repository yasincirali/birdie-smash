# Önizleme karelerine HUD yazılarını (skor, vuruş/olay metinleri, "önizleme" etiketi) basar ve mp4 yapar.
# Kullanım: python3 -I onizleme_yazi.py <ralli.json> <kare_klasörü> <çıktı.mp4>
import sys, json, os, subprocess
from PIL import Image, ImageDraw, ImageFont
R = json.load(open(sys.argv[1])); D = sys.argv[2]; OUT = sys.argv[3]; T = D + '_yazili'; os.makedirs(T, exist_ok=True)
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
    im = Image.open(p).convert('RGB'); im = im.resize((im.width * 2, im.height * 2), Image.LANCZOS).convert('RGBA')
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
            shadow_text(d, (W / 2, y), s, f_mid, col, int(255 * min(1, (1.1 - age) * 2.5)))
    shadow_text(d, (W / 2, H - 92), 'Önizleme: aynı C# simülasyonu ve aynı FBX', f_small, (220, 225, 235), 230)
    shadow_text(d, (W / 2, H - 58), 'Blender ile çizildi, Unity çıktısı değil', f_small, (220, 225, 235), 230)
    Image.alpha_composite(im, ov).convert('RGB').save(os.path.join(T, 'k%04d.png' % i))
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', '30', '-i', os.path.join(T, 'k%04d.png'), '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '22', '-movflags', '+faststart', OUT], check=True)
print('yazıldı', OUT, i + 1, 'kare')
