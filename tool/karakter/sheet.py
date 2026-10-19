# bs/ karelerinden sprite sheet'ler: sheet_<v>.png (renk), mask_<v>.png (R forma, G çerçeve, B kafa bandı), wrist_<v>.png (bileklik)
# + spr.js (base64 data URI'lar ve meta). Maske PNG'lerinde alfa hep 255: tarayıcı alfa-0 pikselin rengini atar (premultiply).
import json, base64, io, sys, os
from PIL import Image
D=sys.argv[1]; meta=json.load(open(f'{D}/meta.json')); cw,ch=meta['cell']; clips=meta['clips']
COLS=max(c['n'] for c in clips.values()); ROWS=len(clips)
out={}
for v in meta['views']:
    sheet=Image.new('RGBA',(COLS*cw,ROWS*ch),(0,0,0,0)); mask=Image.new('RGB',(COLS*cw,ROWS*ch),(0,0,0)); wrist=Image.new('RGB',(COLS*cw,ROWS*ch),(0,0,0))
    for name,c in clips.items():
        for fi in range(c['n']):
            x,y=fi*cw,c['row']*ch
            sheet.paste(Image.open(f'{D}/f_{name}_{v}_{fi}.png').convert('RGBA'),(x,y))
            ch_=[Image.open(f'{D}/m_{name}_{v}_{fi}_{t}.png').convert('RGBA').split()[3] for t in ('forma','cerceve','bant','bilek')]
            mask.paste(Image.merge('RGB',ch_[:3]),(x,y)); wrist.paste(Image.merge('RGB',(ch_[3],ch_[3],ch_[3])),(x,y))
    # 256 renk palet (alfa dahil): HTML'e gömülen boyutu ~3× küçültür; sprite'lar ekranda yarıya ölçeklendiği için fark görünmez
    sheet=sheet.quantize(256,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE); mask=mask.quantize(64,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE); wrist=wrist.convert('L')
    for nm,im in (('sheet',sheet),('mask',mask),('wrist',wrist)):
        b=io.BytesIO(); im.save(b,'PNG',optimize=True); out[f'{nm}_{v}']=b.getvalue(); im.save(f'{D}/{nm}_{v}.png',optimize=True)
meta['cols']=COLS
js='const SPR_META='+json.dumps(meta,separators=(',',':'))+';\nconst SPR_SRC={'+','.join(f"{k}:'data:image/png;base64,{base64.b64encode(b).decode()}'" for k,b in out.items())+'};\n'
open(f'{D}/spr.js','w').write(js)
print({k:len(b)//1024 for k,b in out.items()},'KB; js',len(js)//1024,'KB')
