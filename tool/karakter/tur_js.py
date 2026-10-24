import json,base64,io,sys,os
from PIL import Image
os.chdir(sys.argv[1])
m=json.load(open('tmeta.json')); cw,ch=m['cell']; N=m['n']; C=m['cols']; R=(N+C-1)//C
sh=Image.new('RGBA',(C*cw,R*ch),(0,0,0,0)); mk=Image.new('RGB',(C*cw,R*ch),0); wr=Image.new('L',(C*cw,R*ch),0); m2=Image.new('RGB',(C*cw,R*ch),0); M2=os.path.exists('tm_0_tel.png')
gx=[]; y0=ch; y1=0
for i in range(N):
    x,y=(i%C)*cw,(i//C)*ch
    t=Image.open(f't_{i}.png').convert('RGBA'); sh.paste(t,(x,y))
    bb=t.split()[3].point(lambda v:255 if v>40 else 0).getbbox(); y0=min(y0,bb[1]); y1=max(y1,bb[3])
    c=[Image.open(f'tm_{i}_{g}.png').convert('RGBA').split()[3] for g in ('forma','cerceve','bant','bilek')]
    mk.paste(Image.merge('RGB',c[:3]),(x,y)); wr.paste(c[3],(x,y))
    if M2: m2.paste(Image.merge('RGB',[Image.open(f'tm_{i}_{g}.png').convert('RGBA').split()[3] for g in ('tel','tel2','grip')]),(x,y))
    f=c[0].point(lambda v:255 if v>128 else 0); px=f.load(); s_=n=0
    for yy in range(0,ch,2):
        for xx in range(0,cw,2):
            if px[xx,yy]: s_+=xx; n+=1
    gx.append(round(s_/n,1) if n else cw/2)
m['kadr']={'gx':gx,'y0':y0,'y1':y1}
sh=sh.quantize(256,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE); mk=mk.quantize(64,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE)
out={}
for k,im in (('sheet',sh),('mask',mk),('wrist',wr))+((('mask2',m2.quantize(32,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE)),) if M2 else ()):
    b=io.BytesIO(); im.save(b,'PNG',optimize=True); out[k]=base64.b64encode(b.getvalue()).decode()
js='const TUR_META='+json.dumps(m,separators=(',',':'))+';\nconst TUR_SRC={'+','.join(f"{k}:'data:image/png;base64,{v}'" for k,v in out.items())+'};\n'
open('tur.js','w').write(js); print({k:len(v)*3//4//1024 for k,v in out.items()},'KB',y0,y1)
