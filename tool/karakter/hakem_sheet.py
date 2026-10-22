# hk/ karelerinden hakem sprite sayfası: her klip kendi dolu alanına kırpılır, kareler yan yana, klipler alt alta.
# Renkler sahnede pişmiştir (hakem kıyafeti sabit), maske yok. Çıktı hk.js: HK_META + HK_SRC (data URI).
import json, base64, io, sys
from PIL import Image
D=sys.argv[1]; m=json.load(open(f'{D}/hmeta.json')); cw,ch=m['cell']
clips={}; y=0; parts=[]
for name,c in m['clips'].items():
    fr=[Image.open(f'{D}/h_{name}_{i}.png').convert('RGBA') for i in range(c['n'])]
    bb=None
    for im in fr:
        b=im.getbbox()
        if b: bb=b if bb is None else (min(bb[0],b[0]),min(bb[1],b[1]),max(bb[2],b[2]),max(bb[3],b[3]))
    x0,y0,x1,y1=bb; x0=max(0,x0-1); y0=max(0,y0-1); x1=min(cw,x1+1); y1=min(ch,y1+1); bw,bh=x1-x0,y1-y0
    clips[name]={'n':c['n'],'v':c['v'],'y':y,'bx':x0,'by':y0,'bw':bw,'bh':bh}; parts.append((y,[im.crop((x0,y0,x1,y1)) for im in fr])); y+=bh
W=max(c['n']*c['bw'] for c in clips.values()); sheet=Image.new('RGBA',(W,y),(0,0,0,0))
for (yy,fr),c in zip(parts,clips.values()):
    for i,im in enumerate(fr): sheet.paste(im,(i*c['bw'],yy))
sheet=sheet.quantize(256,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE)
b=io.BytesIO(); sheet.save(b,'PNG',optimize=True); sheet.save(f'{D}/hakem_sheet.png')
meta={'pxPerUnit':m['pxPerUnit'],'anchor':m['anchor'],'clips':clips}
js='const HK_META='+json.dumps(meta,separators=(',',':'))+';\nconst HK_SRC=\'data:image/png;base64,'+base64.b64encode(b.getvalue()).decode()+'\';\n'
open(f'{D}/hk.js','w').write(js); print(sheet.size,len(b.getvalue())//1024,'KB')
