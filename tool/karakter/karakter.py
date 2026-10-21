# Birdie Smash oyuncusu — eklemli rig + hareket seti + sprite sheet (yasin 2026-10-19: "hareket modelleme olarak kusursuz").
# Yaklaşım: kemik/skin yerine parçalı (oyuncak figür) gövde; her karede eklem KONUMLARI hedef olarak verilir, dirsek/diz
# iki-kemik IK ile çözülür → uzuv boyları hiç bozulmaz, pozlar insan diliyle ("bilek baş üstünde, dirsek yüksek") yazılır.
# Anahtar pozlar arasında kosinüs geçiş + hız eğrisi (hazırlık yavaş, temas hızlı, devam sönümlü).
# Çıktı: sheet.png (renk), mask.png (R=forma G=raket çerçevesi B=kafa bandı A=bileklik), meta.json.
import bpy, math, sys, json, os
from mathutils import Vector, Matrix
OUT=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else '/tmp/bs'
MODE=sys.argv[sys.argv.index('--')+2] if '--' in sys.argv and len(sys.argv)>sys.argv.index('--')+2 else 'full'   # 'keys' = yalnız anahtar pozlar (önizleme)
os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.read_homefile(use_empty=True)   # factory ayarı değil (3d-animator kuralı)
sc=bpy.context.scene
def mat(name,col,rough=.6):
    m=bpy.data.materials.new(name); m.use_nodes=True; b=m.node_tree.nodes['Principled BSDF']; b.inputs['Base Color'].default_value=(*col,1); b.inputs['Roughness'].default_value=rough; return m
def emis(name,col):
    m=bpy.data.materials.new(name); m.use_nodes=True; nt=m.node_tree; nt.nodes.clear(); e=nt.nodes.new('ShaderNodeEmission'); e.inputs[0].default_value=(*col,1); e.inputs[1].default_value=1; o=nt.nodes.new('ShaderNodeOutputMaterial'); nt.links.new(e.outputs[0],o.inputs[0]); return m
def holdout(name):
    m=bpy.data.materials.new(name); m.use_nodes=True; nt=m.node_tree; nt.nodes.clear(); h=nt.nodes.new('ShaderNodeHoldout'); o=nt.nodes.new('ShaderNodeOutputMaterial'); nt.links.new(h.outputs[0],o.inputs[0]); return m
SKIN=mat('ten',(0.93,0.72,0.56)); FORMA=mat('forma',(0.80,0.80,0.80)); SORT=mat('sort',(0.12,0.14,0.2)); SAC=mat('sac',(0.16,0.1,0.07)); BEYAZ=mat('beyaz',(0.95,0.95,0.93)); CERCEVE=mat('cerceve',(0.85,0.85,0.85),.35); TEL=mat('tel',(0.85,0.85,0.85),.4); BANT=mat('bant',(0.85,0.85,0.85)); BILEK=mat('bilek',(0.85,0.85,0.85)); TUY=mat('tuy',(0.97,0.97,0.95)); MANTAR=mat('mantar',(0.95,0.72,0.3))
WHITE=emis('w',(1,1,1)); HOLD=holdout('h')
objs=[]
def sphere(name,loc,r,m,scale=(1,1,1),tag=None):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r,location=loc,segments=24,ring_count=12); o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(m); bpy.ops.object.shade_smooth(); o['tag']=tag or m.name; objs.append(o); return o
def capsule(name,a,b,r1,r2,m,tag=None):
    a=Vector(a); b=Vector(b); d=b-a; L=max(1e-4,d.length); mid=a+d/2
    bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=r1,radius2=r2,depth=L,location=mid); o=bpy.context.object; o.name=name
    o.rotation_mode='QUATERNION'; o.rotation_quaternion=d.to_track_quat('Z','Y'); o.data.materials.append(m); bpy.ops.object.shade_smooth(); o['tag']=tag or m.name; objs.append(o)
    sphere(name+'_a',a,r1,m,tag=tag); sphere(name+'_b',b,r2,m,tag=tag); return o
def ik(A,B,l1,l2,hint):
    """iki kemik: A'dan B'ye, boylar l1,l2; dirsek hint yönüne bükülür"""
    A=Vector(A); B=Vector(B); d=B-A; L=d.length; mx=l1+l2-1e-3
    if L>mx: B=A+d.normalized()*mx; d=B-A; L=mx
    a=(l1*l1-l2*l2+L*L)/(2*L); h=math.sqrt(max(0,l1*l1-a*a)); u=d.normalized()
    hv=Vector(hint); hv=hv-u*hv.dot(u)
    if hv.length<1e-4: hv=Vector((0,0,-1))-u*Vector((0,0,-1)).dot(u)
    return A+u*a+hv.normalized()*h, B
UA,FA=0.19,0.17; TH,SH=0.20,0.18   # üst kol/ön kol, uyluk/baldır
SHW=0.13; HIPW=0.085
def build(p):
    """p: poz sözlüğü → sahneye figürü kur"""
    for o in list(objs): bpy.data.objects.remove(o)
    objs.clear()
    root=Vector(p['root']); lean=Vector(p.get('lean',(0,0,0)))
    # twist > 0 = SAĞ omuz geriye (göğüs sağa döner, sol omuz fileye: overhead hazırlığı). 2026-10-20 düzeltmesi:
    # önceki işaret tersti; overhead hazırlıkta sağ omuz öne, takipte geriye gidiyordu (anatomiye aykırı;
    # bkz. /mnt/project-files/birdie-smash/arastirma/vurus_biyomekanigi.md §2-§5). Negatif twist = sağ omuz fileye
    # (backhand hazırlığı, forehand takibi).
    tw=-math.radians(p.get('twist',0))
    chest=root+Vector((0,0,0.22))+lean
    Rz=Matrix.Rotation(tw,3,'Z')
    shL=chest+Rz@Vector((-SHW,0,0.04)); shR=chest+Rz@Vector((SHW,0,0.04))
    neck=chest+Vector((0,0,0.08)); hc=neck+Vector((0,0,0.14))+Vector(p.get('head',(0,0,0)))
    capsule('govde',root,chest,0.11,0.13,FORMA); sphere('karin',(root+chest)/2+Vector((0,0.01,0)),0.12,FORMA,(1,0.85,1))
    capsule('sort',root-Vector((0,0,0.06)),root+Vector((0,0,0.04)),0.115,0.115,SORT)
    capsule('boyun',chest+Vector((0,0,0.03)),neck+Vector((0,0,0.02)),0.045,0.045,SKIN)
    sphere('bas',hc,0.14,SKIN,(1,0.95,1.05)); sphere('sac',hc+Vector((0,-0.01,0.03)),0.145,SAC,(1,0.98,0.95))
    sphere('cene',hc+Vector((0,0.02,-0.08)),0.09,SKIN,(1,0.9,0.8))
    look=Vector(p.get('look',(0,1,0))).normalized(); side=look.cross(Vector((0,0,1))).normalized(); up=side.cross(look).normalized()
    for sx in (-1,1):
        sphere('goz'+str(sx),hc+look*0.12+side*sx*0.055,0.03,BEYAZ); sphere('bebek'+str(sx),hc+look*0.145+side*sx*0.055,0.016,SORT)
        sphere('kulak'+str(sx),hc+side*sx*0.14-Vector((0,0,0.01)),0.03,SKIN,(0.6,1,1))
    bpy.ops.mesh.primitive_torus_add(location=hc+look*0.125-Vector((0,0,0.055)),major_radius=0.035,minor_radius=0.006); sm=bpy.context.object; sm.name='gulus'; sm.rotation_mode='QUATERNION'; sm.rotation_quaternion=look.to_track_quat('Z','Y'); sm.data.materials.append(SORT); sm['tag']='sort'; objs.append(sm)
    # kafa bandı: ayrı nesne (maske); renk oyunda
    bpy.ops.mesh.primitive_torus_add(location=hc+Vector((0,0,0.045)),major_radius=0.142,minor_radius=0.016,major_segments=40,minor_segments=8); bd=bpy.context.object; bd.name='kafabandi'; bd.scale=(1,0.97,1); bd.data.materials.append(BANT); bd['tag']='bant'; objs.append(bd)
    # kollar
    for side_,sh,pre in ((1,shR,'sag'),(-1,shL,'sol')):
        wr=Vector(p['wr'+pre[1].upper() if False else ('wrR' if side_>0 else 'wrL')]); hint=Vector(p.get('elR' if side_>0 else 'elL',(side_*1,-0.3,-0.6)))
        el,wr=ik(sh,wr,UA,FA,hint)
        sphere(pre+'_omuz',sh,0.062,FORMA)
        capsule(pre+'_ustkol',sh,el,0.05,0.042,SKIN); capsule(pre+'_onkol',el,wr,0.04,0.034,SKIN)
        hd=(wr-el).normalized(); sphere(pre+'_el',wr+hd*0.025,0.045,SKIN,(0.85,1,1.05))
        if side_>0:
            bpy.ops.mesh.primitive_torus_add(location=wr-hd*0.02,major_radius=0.042,minor_radius=0.014,major_segments=24,minor_segments=8); bk=bpy.context.object; bk.name='bileklik'; bk.rotation_mode='QUATERNION'; bk.rotation_quaternion=hd.to_track_quat('Z','Y'); bk.data.materials.append(BILEK); bk['tag']='bilek'; objs.append(bk)
            rd=Vector(p.get('rk',(0,0.6,0.8))).normalized(); g0=wr+hd*0.03; g1=g0+rd*0.20
            capsule('raket_sap',g0,g1,0.014,0.012,SORT); hcn=g1+rd*0.13
            fn=Vector(p.get('rkFace',(1,0,0))); fn=(fn-rd*fn.dot(rd)).normalized()
            q=rd.to_track_quat('Y','Z')
            # çerçeve: elips (Y ekseni boyunca uzun), yüzey normali fn
            m3=Matrix((rd.cross(fn),rd,fn)).transposed()  # sütunlar: x=rd×fn, y=rd, z=fn
            bpy.ops.mesh.primitive_torus_add(location=hcn,major_radius=0.12,minor_radius=0.012,major_segments=40,minor_segments=10); t=bpy.context.object; t.name='raket_kafa'; t.scale=(0.85,1,1); t.rotation_mode='QUATERNION'; t.rotation_quaternion=m3.to_quaternion(); t.data.materials.append(CERCEVE); t['tag']='cerceve'; objs.append(t); bpy.ops.object.shade_smooth()
            bpy.ops.mesh.primitive_circle_add(radius=0.115,fill_type='NGON',location=hcn); c=bpy.context.object; c.name='raket_tel'; c.scale=(0.85,1,1); c.rotation_mode='QUATERNION'; c.rotation_quaternion=m3.to_quaternion(); c.data.materials.append(TEL); c['tag']='tel'; objs.append(c)
        elif p.get('shuttle'):  # servis: sol elde tüy top
            sh_=wr+hd*0.06; sphere('mantar',sh_,0.022,MANTAR); capsule('tuy',sh_,sh_+Vector((0,0,0.075)),0.02,0.045,TUY)
    # bacaklar
    for side_,pre in ((1,'sag'),(-1,'sol')):
        hp=root+Vector((side_*HIPW,0,-0.04)); an=Vector(p['anR' if side_>0 else 'anL']); hint=Vector(p.get('knR' if side_>0 else 'knL',(0,1,0)))
        kn,an=ik(hp,an,TH,SH,hint)
        capsule(pre+'_uyluk',hp,kn,0.062,0.05,SKIN); capsule(pre+'_baldir',kn,an,0.048,0.036,SKIN)
        fd=Vector(p.get('ftR' if side_>0 else 'ftL',(0,1,0))); fd.z=0; fd=fd.normalized()
        bpy.ops.mesh.primitive_cube_add(size=1,location=an+fd*0.04-Vector((0,0,0.02))); f=bpy.context.object; f.name=pre+'_ayakkabi'; f.scale=(0.075,0.14,0.05); f.rotation_euler=(0,0,math.atan2(fd.x,fd.y)*-1); f.data.materials.append(BEYAZ); f['tag']='beyaz'
        md=f.modifiers.new('b','BEVEL'); md.width=0.02; md.segments=3; bpy.ops.object.shade_smooth(); objs.append(f)
        bpy.ops.mesh.primitive_cylinder_add(radius=0.07,depth=0.04,location=an+Vector((0,0,0.03))); s2=bpy.context.object; s2.name=pre+'_corap'; s2.data.materials.append(BEYAZ); s2['tag']='beyaz'; objs.append(s2)

# ---------- pozlar ----------
def P_(**k): return k
READY=dict(root=(0,0,0.40),lean=(0,0.01,0),twist=8,wrR=(0.24,0.22,0.52),elR=(1,-0.2,-0.5),rk=(0.05,0.45,0.85),rkFace=(1,0,0),wrL=(-0.22,0.18,0.48),elL=(-1,-0.3,-0.5),anR=(0.12,-0.02,0.05),anL=(-0.12,0.05,0.05),knR=(0,1,0),knL=(0,1,0),head=(0,0,0),look=(0,1,0))
def mix(a,b,t):
    o={}
    for k in a:
        va=a[k]; vb=b.get(k,va)
        if isinstance(va,(int,float)): o[k]=va+(vb-va)*t
        elif isinstance(va,(tuple,list)): o[k]=tuple(va[i]+(vb[i]-va[i])*t for i in range(len(va)))
        else: o[k]=va if t<.5 else vb
    for k in b:
        if k not in o: o[k]=b[k]
    return o
def ease(t): return .5-.5*math.cos(math.pi*t)
def sample(keys,n,loop=False):
    """keys: [(t, poz)], t 0..1; n kare"""
    out=[]
    for i in range(n):
        t=i/(n if loop else max(1,n-1))
        for j in range(len(keys)-1):
            t0,p0=keys[j]; t1,p1=keys[j+1]
            if t0<=t<=t1: out.append(mix(p0,p1,ease((t-t0)/(t1-t0) if t1>t0 else 1))); break
        else: out.append(dict(keys[-1][1]))
    return out
def D(base,**k):
    o=dict(base); o.update(k); return o
# --- hazır: hafif zıplayan bekleyiş (8 kare döngü)
idle=sample([(0,READY),(0.5,D(READY,root=(0,0,0.425),wrR=(0.24,0.22,0.56),wrL=(-0.22,0.18,0.52))),(1,READY)],8,loop=True)
# --- koşu (8 kare döngü): bacaklar karşıt, kollar karşıt, gövde öne eğik, raket yukarıda
run=[]
for i in range(8):
    ph=i/8*2*math.pi; s=math.sin(ph); c=math.cos(ph)
    run.append(D(READY,root=(0,0,0.41+0.02*abs(c)),lean=(0,0.06,0),twist=-10*s,
        anR=(0.11,0.17*s,max(0,0.11*math.sin(ph+0.4))),anL=(-0.11,-0.17*s,max(0,0.11*math.sin(ph+math.pi+0.4))),
        wrR=(0.26,0.16-0.08*s,0.56),wrL=(-0.20,0.05+0.16*s,0.42+0.08*max(0,s))))
# --- smaç (baş üstü, güçlü): hazırlık (raket sırt kaşır, sol el topu gösterir, gövde yay) → temas (kol tam uzanmış,
#     bilek başın hafif önünde) → devam (raket çapraz aşağı, sol kol çekilmiş) → toparlanma
S_PREP=D(READY,root=(0,-0.03,0.40),lean=(0,-0.07,0.01),twist=62,wrR=(0.20,-0.22,0.86),elR=(1,-0.5,0.3),rk=(-0.2,-0.3,0.93),rkFace=(0,1,0),wrL=(-0.12,0.22,0.95),elL=(-1,0.2,0),anR=(0.16,-0.14,0.05),anL=(-0.10,0.14,0.05),look=(0,0.4,1),head=(0,-0.02,0))
S_HIT=D(READY,root=(0,0.02,0.43),lean=(0,0.05,0.02),twist=-12,wrR=(0.10,0.20,1.22),elR=(1,0.2,0.3),rk=(0,0.55,0.83),rkFace=(0,1,0),wrL=(-0.20,0.16,0.60),elL=(-1,-0.3,-0.4),anR=(0.12,-0.06,0.05),anL=(-0.12,0.12,0.05),look=(0,0.6,0.8))
S_FOL=D(READY,root=(0,0.07,0.37),lean=(0,0.13,-0.02),twist=-40,wrR=(-0.20,0.28,0.42),elR=(1,0.5,-0.3),rk=(-0.4,0.5,-0.75),rkFace=(0,1,0),wrL=(-0.22,0.02,0.40),elL=(-1,-0.5,-0.3),anR=(0.12,0.20,0.05),anL=(-0.12,0.04,0.05),look=(0,1,-0.2))
smash=sample([(0,READY),(0.25,S_PREP),(0.40,S_PREP),(0.56,S_HIT),(0.72,S_FOL),(1,READY)],10)
# --- clear (baş üstü, uzun): aynı yay ama temas başın hafif arkasında, devam öne-yukarı (topu taşır)
C_HIT=D(S_HIT,wrR=(0.12,-0.02,1.27),rk=(0,0.2,0.98),lean=(0,0,0.02))
C_FOL=D(S_FOL,wrR=(-0.14,0.30,0.62),rk=(-0.5,0.75,0.2),root=(0,0.05,0.40),lean=(0,0.07,0),twist=-30,anR=(0.12,0.10,0.05))
clear=sample([(0,READY),(0.28,S_PREP),(0.42,S_PREP),(0.58,C_HIT),(0.75,C_FOL),(1,READY)],10)
# --- drive (omuz hizası). 2026-10-21 (yasin: "drive'larda anatomi gerçekçi değil"; arastirma/reports/Badminton alçak
#     vuruş ve servis.md §drive): eskisi tenis gibi büyük geri salınımdı (el gövdenin arkasında, raket geriye yatık).
#     Gerçekte: sağ ayak sağa adım, gövde fileye neredeyse kare; KISA hazırlık (dirsek bükük ve yukarıda, raket başı
#     yukarıda, bilek geriye kırık); temas sağ omzun önünde; güç omuz iç rotasyonu + pronasyon + bilekten; takip kısa,
#     gövdenin önünden sola geçmez.
DR_PREP=D(READY,twist=14,lean=(0.02,0.0,0),wrR=(0.32,0.04,0.74),elR=(1,-0.3,-0.2),rk=(0.25,-0.35,0.90),rkFace=(0.3,1,0),wrL=(-0.18,0.20,0.60),anR=(0.26,0.02,0.05),anL=(-0.12,0.02,0.05),look=(0.3,1,0.1))
DR_HIT=D(READY,twist=0,lean=(0.03,0.03,-0.01),root=(0.03,0.02,0.38),wrR=(0.36,0.34,0.74),elR=(1,0.1,-0.4),rk=(0.35,0.92,0.15),rkFace=(0,1,0),wrL=(-0.20,0.12,0.56),anR=(0.30,0.06,0.05),anL=(-0.12,0.0,0.05),ftR=(0.7,0.7,0))
DR_FOL=D(READY,twist=-8,lean=(0.02,0.04,0),root=(0.02,0.02,0.39),wrR=(0.22,0.40,0.72),elR=(1,0.3,-0.4),rk=(-0.15,0.95,0.25),rkFace=(0,1,0),wrL=(-0.20,0.08,0.52),anR=(0.28,0.06,0.05),anL=(-0.12,0.0,0.05))
drive=sample([(0,READY),(0.3,DR_PREP),(0.55,DR_HIT),(0.72,DR_FOL),(1,READY)],8)
# --- file öldürme (kısa, yüksek dürtme): raket önde yüksek, bilek kırar
K_PREP=D(READY,wrR=(0.22,0.10,0.95),elR=(1,-0.3,-0.4),rk=(0.1,0.3,0.95),rkFace=(0,1,0),lean=(0,0.03,0),look=(0,0.7,0.7))
K_HIT=D(READY,wrR=(0.16,0.34,0.98),elR=(1,-0.2,-0.3),rk=(0.1,0.95,-0.3),rkFace=(0,0.4,1),lean=(0,0.08,0),root=(0,0.05,0.40),anR=(0.14,0.14,0.05),anL=(-0.12,0.0,0.05),look=(0,0.9,0.4))
kill=sample([(0,READY),(0.3,K_PREP),(0.55,K_HIT),(0.75,D(K_HIT,wrR=(0.12,0.30,0.88),rk=(0.1,0.9,-0.45))),(1,READY)],7)
# --- alçak forehand (lift / file önü). 2026-10-21: eski hazırlıkta el kalçanın ARKASINDAYDI (tenis forehand'ı gibi).
#     Gerçekte raket erken ve önde sunulur: sağ ayak öne adım (topuk önce), gövde dik (20–30° eğim), sol kol dengede
#     geride uzanır; kısa geri salınım = önkol dışa döner, raket başı elin altına düşer; temas sağ ayağın önünde alçakta,
#     dirsek hafif bükük; takip kısa, hedefe doğru ileri-yukarı.
L_PREP=D(READY,twist=10,lean=(0.01,0.03,-0.02),root=(0,0.06,0.36),wrR=(0.28,0.24,0.34),elR=(1,-0.1,-0.3),rk=(0.25,-0.25,-0.93),rkFace=(0,1,0.3),wrL=(-0.26,-0.10,0.48),elL=(-1,-0.4,-0.3),anR=(0.15,0.22,0.06),anL=(-0.12,-0.10,0.05),look=(0.1,1,-0.3))
L_HIT=D(READY,twist=2,lean=(0.01,0.06,-0.03),root=(0.02,0.12,0.33),wrR=(0.26,0.44,0.26),elR=(1,0.2,-0.2),rk=(0.15,0.75,-0.64),rkFace=(0,0.7,0.7),wrL=(-0.28,-0.18,0.44),elL=(-1,-0.5,-0.2),anR=(0.16,0.34,0.05),anL=(-0.12,-0.14,0.05),look=(0.1,1,-0.3))
L_FOL=D(READY,twist=-4,lean=(0,0.05,-0.01),root=(0.02,0.11,0.35),wrR=(0.20,0.48,0.56),elR=(1,0.3,-0.3),rk=(0.05,0.55,0.83),rkFace=(0,1,0.3),wrL=(-0.26,-0.16,0.44),anR=(0.16,0.34,0.05),anL=(-0.12,-0.12,0.05))
low=sample([(0,READY),(0.32,L_PREP),(0.55,L_HIT),(0.75,L_FOL),(1,READY)],8)
# --- hamle (lunge): sağ bacak derin öne, sol bacak gergin arkada, raket alçak ve çok önde, sol kol denge için arkada
LU_HIT=D(READY,twist=10,lean=(0,0.07,-0.04),root=(0,0.24,0.28),wrR=(0.22,0.72,0.16),elR=(1,0.4,0.6),rk=(0.1,0.95,-0.2),rkFace=(0,0.3,1),wrL=(-0.26,-0.20,0.42),elL=(-1,-0.5,0.2),anR=(0.14,0.48,0.05),anL=(-0.12,-0.24,0.07),knR=(0,1,0),knL=(0,1,0),ftL=(-0.3,1,0),look=(0,1,-0.4))
LU_PREP=D(READY,twist=15,lean=(0,0.08,-0.03),root=(0,0.10,0.37),wrR=(0.28,0.20,0.30),elR=(1,-0.2,0.5),rk=(0.1,0.8,-0.5),rkFace=(0,1,0),wrL=(-0.24,0.0,0.50),anR=(0.14,0.22,0.08),anL=(-0.12,-0.10,0.05))
lunge=sample([(0,READY),(0.35,LU_PREP),(0.6,LU_HIT),(0.78,D(LU_HIT,wrR=(0.16,0.66,0.30),rk=(0,0.9,0.4))),(1,READY)],8)
# --- servisler (2026-10-21, yasin: "kısa servisi bir badminton sporcusu gibi atmalı, uzun servis de buna göre; kısa servis
#     pozisyonunda flick de atabilmeliyim"; arastirma/reports/Badminton alçak vuruş ve servis.md §servis).
# kısa servis (backhand): sağ ayak önde, ayaklar omuz genişliğinde kademeli, gövde fileye kare; backhand tutuş,
#     raket bel önünde, başı aşağı, dirsek yukarı-öne; tüy top sol elde tüylerinden, raketin hemen önünde ve 1,15 m
#     altında. Geri salınım birkaç santim; başparmakla kısa itiş; takip raket yüzü fileye bakar, göğüs hizasının altında kalır.
SB0=D(READY,root=(0,0.02,0.42),twist=-8,lean=(0,0.02,0),anR=(0.07,0.12,0.05),anL=(-0.13,-0.10,0.05),ftR=(0,1,0),ftL=(-0.3,1,0),
      wrR=(-0.02,0.20,0.44),elR=(1,0.6,0.3),rk=(-0.18,0.20,-0.96),rkFace=(0,1,0),wrL=(-0.04,0.34,0.48),elL=(-1,0.2,-0.6),shuttle=True,look=(0,1,-0.25))
SB1=D(SB0,wrR=(-0.02,0.16,0.43),rk=(-0.18,0.05,-0.98))
SB2=D(SB0,wrR=(-0.02,0.30,0.47),rk=(-0.12,0.62,-0.77),wrL=(-0.12,0.24,0.46),shuttle=False)
SB3=D(SB2,wrR=(-0.02,0.34,0.53),rk=(-0.06,0.88,-0.40),look=(0,1,0))
serve_bh=sample([(0,SB0),(0.22,SB0),(0.40,SB1),(0.50,SB2),(0.68,SB3),(1,READY)],10)
# flick: SB0/SB1 birebir aynı (aldatmanın şartı); temasta geç ve kısa bir bilek/başparmak kırılması raket başını yukarı fırlatır
SF2=D(SB0,wrR=(-0.02,0.29,0.50),rk=(-0.10,0.75,0.65),rkFace=(0,0.6,0.8),wrL=(-0.12,0.24,0.46),shuttle=False)
SF3=D(SF2,wrR=(-0.04,0.28,0.64),rk=(-0.10,0.25,0.96),look=(0,0.8,0.6))
serve_flick=sample([(0,SB0),(0.22,SB0),(0.40,SB1),(0.50,SF2),(0.66,SF3),(1,READY)],10)
# yüksek servis (forehand, teklerde uzun): yan duruş, sol ayak önde, ağırlık arka (sağ) ayakta; sol el topu önde omuz
#     hizasında tutar ve bırakır (atmaz); raket arkada yukarıdan uzun ve gevşek iner; temas ön ayak hizasında alçakta,
#     önkol rotasyonu; takip sol omzun üstüne, ağırlık öne geçer; temastan sonra arka ayak öne kayar.
SH0=D(READY,root=(0,-0.03,0.41),twist=55,lean=(0,-0.03,0),anL=(-0.02,0.20,0.05),anR=(0.14,-0.16,0.05),ftL=(0.2,1,0),ftR=(0.9,0.4,0),
      wrR=(0.30,-0.20,0.74),elR=(1,-0.5,0.2),rk=(0.2,-0.5,0.85),rkFace=(1,0,0),wrL=(0.04,0.28,0.68),elL=(-1,0.2,-0.4),shuttle=True,look=(0,1,0))
SH1=D(SH0,root=(0,-0.02,0.40),twist=45,wrR=(0.30,-0.10,0.36),elR=(1,-0.4,0.2),rk=(0.2,-0.45,-0.87),wrL=(-0.06,0.24,0.62),shuttle=False)
SH2=D(SH1,root=(0,0.04,0.40),twist=12,lean=(0,0.02,0),wrR=(0.18,0.30,0.34),elR=(1,0,0.2),rk=(0.10,0.92,-0.37),rkFace=(0,0.5,0.85),wrL=(-0.14,0.16,0.56))
SH3=D(SH2,root=(0,0.08,0.41),twist=-35,lean=(0,0.04,0),wrR=(-0.22,0.18,0.92),elR=(1,0.4,0.4),rk=(-0.45,-0.2,0.87),rkFace=(0,1,0),wrL=(-0.20,0.02,0.50),anR=(0.12,0.0,0.07))
serve=sample([(0,SH0),(0.18,SH0),(0.38,SH1),(0.55,SH2),(0.75,SH3),(1,READY)],10)
# --- blok (ağa yakın kısa itiş): raket göğüs önünde dik, küçük hareket
# savunma bloğu: geniş alçak duruş, raket bel önünde (diz ile göğüs arası), çok kısa itiş, takip yok
B_LOW=D(READY,root=(0,-0.02,0.34),lean=(0,0.07,-0.02),anR=(0.20,0.04,0.05),anL=(-0.19,0.0,0.05),wrR=(0.04,0.24,0.46),elR=(1,-0.4,-0.3),rk=(-0.1,0.6,0.8),rkFace=(0,1,0),wrL=(-0.20,0.16,0.42))
B_HIT=D(B_LOW,wrR=(0.04,0.34,0.50),rk=(-0.1,0.7,0.7),look=(0,1,0.1))
block=sample([(0,READY),(0.3,B_LOW),(0.6,B_HIT),(0.8,B_LOW),(1,READY)],6)

# ================= v16: raket HEP sağ elde (yasin 2026-10-19: "rakette el değiştirilmez"). Backhand aynalama değil,
# gerçek backhand pozu: kol gövdeyi çaprazlar, dirsek topa doğru, el sırtı önde, devam kısa. =================
# --- backhand overhead (sırt file'ye döner, dirsek yukarı, bilek kırılır; devam kısa)
BO_PREP=D(READY,twist=-125,lean=(-0.02,-0.06,0.01),wrR=(-0.10,-0.14,0.78),elR=(-0.4,-1,0.8),rk=(-0.2,-0.4,-0.9),rkFace=(0,1,0),wrL=(0.10,0.06,0.52),elL=(1,-0.4,-0.3),anR=(-0.12,-0.24,0.05),anL=(0.04,0.06,0.05),ftR=(-0.6,-1,0),ftL=(0.3,1,0),look=(-0.5,-0.5,0.7))
BO_HIT=D(READY,twist=-105,lean=(-0.02,-0.03,0.02),wrR=(-0.20,-0.10,1.22),elR=(-0.5,-1,0.6),rk=(-0.2,-0.2,0.96),rkFace=(0,-1,0),wrL=(0.10,0.08,0.48),anR=(-0.12,-0.24,0.05),anL=(0.04,0.06,0.05),ftR=(-0.6,-1,0),ftL=(0.3,1,0),look=(-0.4,-0.4,0.8))
BO_FOL=D(READY,twist=-70,lean=(0,0.0,0),wrR=(-0.30,-0.04,1.04),elR=(-0.6,-1,0.4),rk=(-0.6,-0.2,0.75),rkFace=(0,-1,0),wrL=(0.12,0.08,0.48),anR=(-0.08,-0.18,0.05),anL=(-0.02,0.08,0.05))
bh_over=sample([(0,READY),(0.26,BO_PREP),(0.40,BO_PREP),(0.56,BO_HIT),(0.72,BO_FOL),(1,READY)],10)
# --- backhand drive (2026-10-21): başparmak tutuş, dirsek önde ve topa dönük, raket başı arkadan gelir; temas vuran
#     omzun önünde, kol hafif bükük; supinasyon + bilek açılması; geri salınım çok kısa, takip gövdenin önünden
#     SAĞA savrulmaz (eskiden kol gövdeyi çaprazlayıp sağa uzanıyordu). Sağ ayak sola-öne adım atar.
BD_PREP=D(READY,twist=-30,lean=(-0.02,0.0,0),wrR=(-0.08,0.14,0.66),elR=(0.3,1,0.4),rk=(-0.55,-0.45,0.70),rkFace=(0,1,0),wrL=(-0.22,-0.04,0.52),anR=(-0.06,0.14,0.05),anL=(-0.18,-0.04,0.05),look=(-0.4,1,0))
BD_HIT=D(READY,twist=-16,lean=(-0.02,0.04,0),wrR=(-0.28,0.36,0.72),elR=(0.3,1,0.2),rk=(-0.60,0.78,0.15),rkFace=(0,1,0),wrL=(-0.24,-0.08,0.50),anR=(-0.10,0.18,0.05),anL=(-0.18,-0.06,0.05),ftR=(-0.5,0.9,0))
BD_FOL=D(READY,twist=-12,lean=(-0.01,0.04,0),wrR=(-0.28,0.42,0.76),elR=(0.3,1,0.2),rk=(-0.30,0.90,0.30),rkFace=(0,1,0),wrL=(-0.24,-0.08,0.48),anR=(-0.10,0.18,0.05),anL=(-0.18,-0.06,0.05))
bh_drive=sample([(0,READY),(0.3,BD_PREP),(0.55,BD_HIT),(0.72,BD_FOL),(1,READY)],8)
# --- backhand file öldürme (yüksek, kısa dürtme)
BK_PREP=D(READY,twist=-20,wrR=(-0.14,0.08,0.92),elR=(0.5,1,0.2),rk=(-0.3,0.2,0.93),rkFace=(0,1,0),lean=(0,0.03,0),look=(-0.3,0.7,0.6))
BK_HIT=D(READY,twist=-5,wrR=(-0.28,0.34,0.98),elR=(0.4,0.9,0),rk=(-0.4,0.8,-0.4),rkFace=(0,0.4,1),lean=(0,0.08,0),root=(0,0.05,0.40),anR=(0.0,0.14,0.05),anL=(-0.16,0.0,0.05),look=(-0.2,0.9,0.4))
bh_kill=sample([(0,READY),(0.3,BK_PREP),(0.55,BK_HIT),(0.75,D(BK_HIT,wrR=(-0.26,0.30,0.88),rk=(-0.4,0.75,-0.55))),(1,READY)],7)
# --- backhand alçak (file önü / lift, 2026-10-21): sağ ayak sola-öne adım (backhand köşesine de raket ayağı gider),
#     başparmak tutuş, dirsek önce yükselip öne çıkar, raket başı arkadan ve elin ALTINDA gelir; gövde dik, sol kol
#     geride; takip kısa ve hedefe. Eskisinde raket başı önde süpürüyordu.
BL_PREP=D(READY,twist=-22,lean=(-0.01,0.03,-0.02),root=(-0.02,0.06,0.36),wrR=(-0.12,0.22,0.40),elR=(0.3,1,0.6),rk=(-0.45,-0.15,-0.88),rkFace=(0,1,0.3),wrL=(-0.26,-0.14,0.46),elL=(-1,-0.4,-0.3),anR=(-0.06,0.22,0.06),anL=(-0.16,-0.10,0.05),look=(-0.2,1,-0.3))
BL_HIT=D(READY,twist=-12,lean=(-0.01,0.06,-0.03),root=(-0.03,0.12,0.33),wrR=(-0.22,0.44,0.28),elR=(0.3,1,0.6),rk=(-0.30,0.70,-0.65),rkFace=(0,0.7,0.7),wrL=(-0.28,-0.18,0.44),elL=(-1,-0.5,-0.2),anR=(-0.08,0.34,0.05),anL=(-0.16,-0.14,0.05),ftR=(-0.4,1,0),look=(-0.1,1,-0.3))
BL_FOL=D(READY,twist=-10,lean=(0,0.05,-0.01),root=(-0.03,0.11,0.35),wrR=(-0.20,0.48,0.50),elR=(0.3,1,0.4),rk=(-0.25,0.60,0.76),rkFace=(0,1,0.3),wrL=(-0.26,-0.16,0.44),anR=(-0.08,0.34,0.05),anL=(-0.16,-0.12,0.05))
bh_low=sample([(0,READY),(0.32,BL_PREP),(0.55,BL_HIT),(0.75,BL_FOL),(1,READY)],8)
# --- backhand hamle: sol-öne derin, sağ (raket) bacak öne
BLU_PREP=D(READY,twist=-15,lean=(-0.02,0.08,-0.03),root=(-0.03,0.10,0.37),wrR=(-0.22,0.2,0.30),elR=(0,1,0.6),rk=(-0.3,0.8,-0.5),rkFace=(0,1,0),wrL=(0.22,0.0,0.50),anR=(-0.04,0.22,0.08),anL=(-0.14,-0.10,0.05))
BLU_HIT=D(READY,twist=-30,lean=(-0.03,0.07,-0.04),root=(-0.06,0.24,0.28),wrR=(-0.30,0.68,0.14),elR=(0,1,0.6),rk=(-0.3,0.9,-0.2),rkFace=(0,0.3,1),wrL=(0.24,-0.18,0.46),elL=(1,-0.5,0.2),anR=(-0.04,0.42,0.05),anL=(-0.16,-0.20,0.05),ftL=(0.3,1,0),look=(-0.2,1,-0.4))
bh_lunge=sample([(0,READY),(0.35,BLU_PREP),(0.6,BLU_HIT),(0.78,D(BLU_HIT,wrR=(-0.24,0.64,0.30),rk=(-0.3,0.9,0.4))),(1,READY)],8)
# ================= ayak işi (yasin: "adımlamalar badminton sporunun doğasına uygun olmalı") =================
# split step: rakip vururken küçük sıçrama, geniş ve alçak iniş
split=sample([(0,READY),(0.35,D(READY,root=(0,0,0.47),anR=(0.16,0,0.10),anL=(-0.16,0,0.10),wrR=(0.24,0.22,0.58),wrL=(-0.22,0.18,0.54))),(0.65,D(READY,root=(0,0,0.36),anR=(0.20,0,0.05),anL=(-0.20,0,0.05))),(1,READY)],5)
# chassé (yana kayma): öndeki ayak açılır, arkadaki ayak yanına gelir; gövde alçak, raket yukarıda
def chasse(sgn):
    k0=D(READY,root=(0,0,0.39),anR=(sgn*0.14 if sgn>0 else 0.12,0,0.05),anL=(sgn*0.14 if sgn<0 else -0.12,0,0.05),lean=(sgn*0.03,0.02,0))
    k1=D(k0,root=(0,0,0.41),**({'anR':(0.34,0,0.07)} if sgn>0 else {'anL':(-0.34,0,0.07)}))
    k2=D(k0,root=(0,0,0.38),**({'anR':(0.34,0,0.05),'anL':(0.06,0,0.06)} if sgn>0 else {'anL':(-0.34,0,0.05),'anR':(-0.06,0,0.06)}))
    return sample([(0,k0),(0.35,k1),(0.7,k2),(1,k0)],6,loop=True)
chasse_r=chasse(1); chasse_l=chasse(-1)
# öne adım: kısa, alçak adımlar (hamleye hazırlanır), raket önde yukarıda
fwd=[]
for i in range(6):
    ph=i/6*2*math.pi; s_=math.sin(ph)
    fwd.append(D(READY,root=(0,0.02,0.38+0.01*abs(math.cos(ph))),lean=(0,0.07,-0.01),twist=-6*s_,anR=(0.11,0.12*s_,max(0,0.06*math.sin(ph+0.4))),anL=(-0.11,-0.12*s_,max(0,0.06*math.sin(ph+math.pi+0.4))),wrR=(0.24,0.24,0.60),wrL=(-0.2,0.14,0.46)))
# arkaya çapraz adım — sağ (forehand) köşe: kalça sağa döner, sol ayak sağın arkasından geçer, sağ ayak geri; raket hazırlıkta
def back(sgn):
    tw=42*sgn if sgn>0 else -62
    base=D(READY,twist=tw,lean=(0,-0.04,0),wrR=(0.22,-0.08,0.80) if sgn>0 else (-0.06,-0.06,0.76),elR=(1,-0.4,0.3) if sgn>0 else (-1,-0.3,0.6),rk=(0,-0.4,0.9) if sgn>0 else (-0.3,-0.5,-0.7),wrL=(-0.16,0.16,0.70) if sgn>0 else (0.18,0.0,0.55),look=(0.3*sgn,0.5,0.8))
    if sgn>0: k1=D(base,anL=(0.10,-0.16,0.07),anR=(0.14,0.0,0.05)); k2=D(base,anL=(0.10,-0.16,0.05),anR=(0.18,-0.30,0.07))
    else:     k1=D(base,anR=(-0.10,-0.16,0.07),anL=(-0.14,0.0,0.05)); k2=D(base,anR=(-0.10,-0.16,0.05),anL=(-0.18,-0.30,0.07))
    return sample([(0,base),(0.35,k1),(0.7,k2),(1,base)],6,loop=True)
back_r=back(1); back_l=back(-1)
# sıçrayarak smaç: makas — raket bacağı geride, sıçrayışta öne savrulur, sol bacak geriye; iniş sol ayağa. Yükseklik oyunda eklenir (root sabit).
J_TAKE=D(S_PREP,root=(0,-0.03,0.34),anR=(0.16,-0.18,0.05),anL=(-0.10,0.14,0.05))
J_AIR=D(S_HIT,root=(0,0.0,0.42),anR=(0.12,0.22,0.16),anL=(-0.12,-0.22,0.22),knR=(0,1,0),knL=(0,-1,0))
J_LAND=D(S_FOL,root=(0,0.04,0.36),anR=(0.14,0.16,0.10),anL=(-0.14,-0.10,0.05),ftL=(-0.7,1,0))   # önce sol ayak dışa dönük iner (§3)
jump=sample([(0,READY),(0.22,S_PREP),(0.34,J_TAKE),(0.52,J_AIR),(0.70,J_LAND),(1,READY)],10)
CLIPS=dict(idle=idle,run=run,smash=smash,clear=clear,drive=drive,kill=kill,low=low,lunge=lunge,serve=serve,serve_bh=serve_bh,serve_flick=serve_flick,block=block,bh_over=bh_over,bh_drive=bh_drive,bh_kill=bh_kill,bh_low=bh_low,bh_lunge=bh_lunge,split=split,chasse_r=chasse_r,chasse_l=chasse_l,fwd=fwd,back_r=back_r,back_l=back_l,jump=jump)
KEYS=dict(sv=[SB0,SB1,SB2,SB3],fl=[SF2,SF3],sh=[SH0,SH1,SH2,SH3],dr=[DR_PREP,DR_HIT,DR_FOL],lo=[L_PREP,L_HIT,L_FOL],bdn=[BD_PREP,BD_HIT,BD_FOL],bln=[BL_PREP,BL_HIT,BL_FOL],X0=[],bo=[BO_PREP,BO_HIT,BO_FOL],bd=[BD_PREP,BD_HIT,BD_FOL],bk=[BK_PREP,BK_HIT],bl=[BL_PREP,BL_HIT,BL_FOL],blu=[BLU_PREP,BLU_HIT],split=[split[1],split[3]],chr=[chasse_r[2],chasse_r[4]],chl=[chasse_l[2],chasse_l[4]],fwd=[fwd[1],fwd[4]],bkr=[back_r[2],back_r[4]],bkl=[back_l[2],back_l[4]],jump=[J_TAKE,J_AIR,J_LAND])

# ---------- sahne ----------
bpy.ops.object.light_add(type='SUN',location=(1,-2,4)); sun=bpy.context.object; sun.data.energy=3.2; sun.rotation_euler=(math.radians(40),0,math.radians(20))
bpy.ops.object.light_add(type='AREA',location=(-2,-2,2)); fill=bpy.context.object; fill.data.energy=120; fill.data.size=3; fill.rotation_euler=(math.radians(60),0,math.radians(-45))
sc.world=bpy.data.worlds.new('w'); sc.world.use_nodes=True; sc.world.node_tree.nodes['Background'].inputs[0].default_value=(0.5,0.55,0.55,1); sc.world.node_tree.nodes['Background'].inputs[1].default_value=0.6
sc.render.engine='CYCLES'; sc.cycles.samples=24; sc.cycles.use_denoising=True; sc.render.film_transparent=True; sc.view_settings.view_transform='Standard'
try: sc.cycles.device='CPU'
except: pass
CW_,CH_=192,256; sc.render.resolution_x=CW_; sc.render.resolution_y=CH_; sc.render.resolution_percentage=100
bpy.ops.object.camera_add(); cam=bpy.context.object; sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=2.4  # büyük kenar (yükseklik) = 2.4 birim → ~107 px/birim; hamlede raket kadraja sığsın
VIEWS={'me':(0,-1,1.35),'op':(0,1,1.35)}  # arkadan (ben) ve önden (rakip), ~53° yukarıdan
def setcam(v):
    d=Vector(VIEWS[v]).normalized(); cam.location=Vector((0,0,0.5))+d*4; cam.rotation_mode='XYZ'; el=math.atan2(d.z,math.hypot(d.x,d.y)); cam.rotation_euler=(math.pi/2-el,0,math.atan2(d.y,d.x)+math.pi/2)  # baş hep yukarıda (track_quat'ta roll kayıyordu)
from bpy_extras.object_utils import world_to_camera_view as w2c
def anchor():
    p=w2c(sc,cam,Vector((0,0,0))); return [p.x*CW_,(1-p.y)*CH_]
def render(path):
    sc.render.filepath=path; bpy.ops.render.render(write_still=True)
def set_mats(tag):
    """tag=None: normal; tag='forma'...: o etiket beyaz emisyon, kalan holdout"""
    for o in objs:
        if o.type!='MESH': continue
        if tag is None:
            for i,m in enumerate(o.data.materials): o.data.materials[i]=o['orig'][i] if 'orig' in o else m
        else:
            if 'orig' not in o: o['orig']=[m for m in o.data.materials]
            tags=o.get('tag')
            for i in range(len(o.data.materials)): o.data.materials[i]=WHITE if tags==tag else HOLD
# Bant ve bileklik renk pasında görünmez (oyun maskeden boyar; "yok" seçilince hiç çizilmez)
def hide_bands(h):
    for o in objs:
        if o.get('tag') in ('bant','bilek'): o.hide_render=h
meta={'cell':[CW_,CH_],'clips':{},'views':list(VIEWS),'pxPerUnit':CH_/2.4}
if MODE=='keys':
    sc.cycles.samples=12; setcam('me'); i=0
    for name,ps in KEYS.items():
        for j,p in enumerate(ps):
            build(p); hide_bands(False); render(f'{OUT}/key_{name}{j}_me.png'); setcam('op'); render(f'{OUT}/key_{name}{j}_op.png'); setcam('me'); i+=1
    print('KEYS DONE',i)
else:
    row=0
    for name,frames in CLIPS.items():
        meta['clips'][name]={'n':len(frames),'row':row}
        for v in VIEWS:
            setcam(v); meta.setdefault('anchor',{})[v]=anchor()
            for fi,p in enumerate(frames):
                build(p); hide_bands(True); set_mats(None); render(f'{OUT}/f_{name}_{v}_{fi}.png')
                hide_bands(False); sc.cycles.samples=4; sc.cycles.use_denoising=False
                for tag in ('forma','cerceve','bant','bilek'): set_mats(tag); render(f'{OUT}/m_{name}_{v}_{fi}_{tag}.png')
                set_mats(None); sc.cycles.samples=24; sc.cycles.use_denoising=True
        row+=1
    json.dump(meta,open(f'{OUT}/meta.json','w'))
    print('DONE')
