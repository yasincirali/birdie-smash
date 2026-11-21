# Sistem açılış ekranının topu (Editor/acilis_top.png; CiBuild.IosAcilisTopu). 2026-11-14: açılış spininin ilk karesi =
# top .55 boyda, 44°, şerit karesi 0 (BirdieAcilis.mm BS_* / Hud ACILIS_*). Kare ekranın kısa kenarının %50'si (FillPct 50),
# top Hud geometrisiyle: merkezden .026·m yukarı, boy .3·m, mantar (39,36) çevresinde yön + 135°. Doğrulama: --eski
# (boy 1) eski dosyayla premultiplied ortalama fark 0,42/255 verdi. Gerek: python3 + Pillow + numpy.
# Kullanım (depo kökünden): python3 tool/acilis_top_uret.py
import sys, os, math, numpy as np
from PIL import Image
A=os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','unity','BirdieSmash','Assets')+os.sep
strip=Image.open(A+'Resources/ui/tuy_don.png').convert('RGBA')
def gen(fi, ks, out=None, N=1024):
    m=N/.5; k=.3*m*ks; s=k/300; p=(N/2, N/2-.026*m); yon=math.radians(44)
    cork=(p[0]+math.cos(yon)*.56*k, p[1]+math.sin(yon)*.56*k)
    cw,ch=strip.size[0]//4,strip.size[1]//4   # 2026-11-21: şerit 1024² (256² kare, boyut); geometri 300×299 kare biriminde kalır
    fr=strip.crop(((fi%4)*cw,(fi//4)*ch,(fi%4)*cw+cw,(fi//4)*ch+ch)).resize((300,299),Image.LANCZOS)
    a=np.array(fr).astype(np.float32); a[...,:3]*=a[...,3:4]/255; fr=Image.fromarray(a.clip(0,255).astype(np.uint8))
    th=math.radians(44+135); c,sn=math.cos(th),math.sin(th)
    # f = (39,36) + R(-th)(q-cork)/s ; R(-th)=[[c,sn],[-sn,c]]
    aa=c/s; bb=sn/s; dd=-sn/s; ee=c/s
    cc=39-aa*cork[0]-bb*cork[1]; ff=36-dd*cork[0]-ee*cork[1]
    o=fr.transform((N,N),Image.AFFINE,(aa,bb,cc,dd,ee,ff),resample=Image.BICUBIC)
    o=np.array(o).astype(np.float32); al=o[...,3:4]; o[...,:3]=np.where(al>0,o[...,:3]*255/np.maximum(al,1),0)
    img=Image.fromarray(o.clip(0,255).astype(np.uint8),'RGBA')
    if out: img.save(out,optimize=True)
    return np.array(img)
if __name__=='__main__':
    ks=1.0 if '--eski' in sys.argv else .55
    gen(0, ks, A+'BirdieSmash/Editor/acilis_top.png'); print('yazıldı, boy', ks)
