# bs/ karelerinden klip başına sprite şeritleri: her klip × bakış için renk (RGBA), maske (R forma, G çerçeve, B kafa bandı)
# ve bileklik görüntüsü + spr.js (base64 data URI'lar ve meta). Maske PNG'lerinde alfa hep 255: tarayıcı alfa-0 pikselin
# rengini atar (premultiply).
# Klip başına ayrı görüntü (2026-10-23): 2× hücrede tek sayfa 3840×12288 olurdu, çözülünce görüntü başına 188 MB; iOS
# WebView'u bunu kaldırmaz. Klip şeridi en çok 3840×1024 (15 MB) ve oyun her klibi ayrı çözüp boyayıp bırakır.
import json, base64, io, sys
from PIL import Image
D=sys.argv[1]; meta=json.load(open(f'{D}/meta.json')); cw,ch=meta['cell']; clips=meta['clips']
COLS=10   # uzun klip alt sıraya taşar
src={}; boy={}
for v in meta['views']:
    src[v]={}
    for name,c in clips.items():
        n=c['n']; w,h=min(n,COLS)*cw,((n+COLS-1)//COLS)*ch
        sheet=Image.new('RGBA',(w,h),(0,0,0,0)); mask=Image.new('RGB',(w,h),(0,0,0)); wrist=Image.new('L',(w,h),0)
        for fi in range(n):
            x,y=(fi%COLS)*cw,(fi//COLS)*ch
            sheet.paste(Image.open(f'{D}/f_{name}_{v}_{fi}.png').convert('RGBA'),(x,y))
            ch_=[Image.open(f'{D}/m_{name}_{v}_{fi}_{t}.png').convert('RGBA').split()[3] for t in ('forma','cerceve','bant','bilek')]
            mask.paste(Image.merge('RGB',ch_[:3]),(x,y)); wrist.paste(ch_[3],(x,y))
        # 256 renk palet (alfa dahil): cel gölgeli figürde renk sayısı zaten az, fark görünmez; gömülen boyut ~3× küçük
        sheet=sheet.quantize(256,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE); mask=mask.quantize(64,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE)
        uri=[]
        for nm,im in (('sheet',sheet),('mask',mask),('wrist',wrist)):
            b=io.BytesIO(); im.save(b,'PNG',optimize=True); uri.append('data:image/png;base64,'+base64.b64encode(b.getvalue()).decode()); boy[nm]=boy.get(nm,0)+b.tell()
        src[v][name]=uri
meta['cols']=COLS
js='const SPR_META='+json.dumps(meta,separators=(',',':'))+';\nconst SPR_SRC='+json.dumps(src,separators=(',',':'))+';\n'
open(f'{D}/spr.js','w').write(js)
print({k:b//1024 for k,b in boy.items()},'KB; js',len(js)//1024,'KB')
