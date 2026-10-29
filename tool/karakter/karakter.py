# Birdie Smash oyuncusu — eklemli rig + hareket seti + sprite sheet (yasin 2026-10-19: "hareket modelleme olarak kusursuz").
# Yaklaşım: kemik/skin yerine parçalı (oyuncak figür) gövde; her karede eklem KONUMLARI hedef olarak verilir, dirsek/diz
# iki-kemik IK ile çözülür → uzuv boyları hiç bozulmaz, pozlar insan diliyle ("bilek baş üstünde, dirsek yüksek") yazılır.
# Anahtar pozlar arasında kosinüs geçiş + hız eğrisi (hazırlık yavaş, temas hızlı, devam sönümlü).
# Çıktı: sheet.png (renk), mask.png (R=forma G=raket çerçevesi B=kafa bandı A=bileklik), meta.json.
import bpy, math, sys, json, os
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
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
# Hakem kıyafeti (BWF: lacivert ceket, açık gömlek, koyu pantolon, siyah ayakkabı). Renk sahnede pişer; oyunda boyanmaz.
CEKET=mat('ceket',(0.025,0.04,0.10)); PANT=mat('pantolon',(0.08,0.08,0.10)); SIYAH=mat('ayakkabi',(0.04,0.04,0.05),.35); GOMLEK=mat('gomlek',(0.92,0.94,0.96)); TABLET=mat('tablet',(0.10,0.10,0.12),.3); EKRAN=emis('ekran',(0.35,0.55,0.75))
SAC_GRI=mat('sac_gri',(0.12,0.12,0.12))
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
# Gerçekçi gövde (yasin 2026-10-23: "insan anatomisine uygun olacak kadar gerçekçi"): MakeHuman tabanı (insan.py, CC0),
# aynı poz sözlüklerini kullanır. BS_GERCEK=0 eski oyuncak figürü çizer (hakemler şimdilik oyuncak figürde).
GERCEK=os.environ.get('BS_GERCEK','1')=='1'
MODEL=os.environ.get('BS_MODEL','insan')   # 'oyuncak' = Oyuncak 3B, 2026-10-26 yeniden tasarım: MakeHuman (CC0) atletik gövde + kendi kıyafet/yüz/saç geometrisi (oyuncak.py); 'oyuncak_kaykit' = eski KayKit chibi (arşiv, oyuncak_kaykit.py); 'aday' = Rocketbox (aday.py); 'sade' = Mesh2Motion mankeni (sade.py)
OY=None
def build_gercek(p):
    global OY
    import insan
    if OY is None:
        for o in list(objs): bpy.data.objects.remove(o)
        M_=insan.malzemeler(FORMA,SORT,BEYAZ,BANT,CERCEVE,TEL,SORT)
        if MODEL=='oyuncak': import oyuncak; objs.clear(); OY=oyuncak.Oyuncak(M_)
        elif MODEL=='oyuncak_kaykit': import oyuncak_kaykit; objs.clear(); OY=oyuncak_kaykit.Oyuncak(M_)   # arşiv: eski KayKit chibi (asset ayrıca gerekir)
        elif MODEL=='aday': import aday; objs.clear(); OY=aday.Aday(M_)   # karşılaştırma: hazır modeller (aday.py)
        elif MODEL=='sade': import sade; objs.clear(); OY=sade.Sade(M_)   # sade: Mesh2Motion mankeni (CC0), normal oranlı insan
        elif MODEL=='tuycuk': import tuycuk; objs.clear(); OY=tuycuk.Tuycuk(M_)   # Tüycük (2026-10-29): Oyuncak iskeleti + klipleri, yumurta gövdeli yeni ağ (tuycuk.py)
        else: objs.clear(); OY=insan.Oyuncu(M_,stil=os.environ.get('BS_STIL'))
    if not (p.get('fpR') or p.get('fpL')) or not hasattr(OY,'R') or not hasattr(OY,'ao'): objs[:]=OY.kur_poz(p); return
    AG=_ayak_geo()
    q=dict(p)
    for K in 'RL':
        a=math.radians(p.get('fp'+K,0) or 0)
        if not a: continue
        g=AG[K]; piv=g['hb'] if a>0 else g['fb']   # burun yukarı: topuk ekseni; topuk havada: parmak kökü ekseni
        dz=(g['hz']*math.cos(abs(a))+piv*math.sin(abs(a))-g['hz'])/AG['s']   # taban zeminde kalsın diye bilek bu kadar yükselir (poz birimi)
        # Eksen noktası yatayda da yerinde kalsın: bilek etrafında dönen topuk/parmak kökü bileğe doğru (topuk havadaysa) ya da
        # öne (burun yukarıysa) kayar: piv·(1−cos a) + hz·sin a. Eskiden hamlede arka ayağın parmak ucu topuk kalkarken yerde
        # 19 cm geri sürünüyordu (s5 probu). Bilek ayak yönünde o kadar öne (topuk havada) ya da geri (burun yukarı) kaydırılır.
        dh=(piv*(1-math.cos(abs(a)))+g['hz']*math.sin(abs(a)))/AG['s']*(1 if a<0 else -1)
        fd=Vector(p.get('ft'+K,(0,1,0))); fd.z=0; fd=fd.normalized() if fd.length>1e-6 else Vector((0,1,0))
        an=q['an'+K]; q['an'+K]=(an[0]+fd.x*dh,an[1]+fd.y*dh,an[2]+dz)
    objs[:]=OY.kur_poz(q); _ayak_cevir(p,AG)
# Ayak eğimi (2026-10-26, tüm beden turu): modeller ayağı hep dinlenme eğiminde (düz) basar, yalnız yönünü çevirir. Hamlede
#     "önce topuk", arka ayağın parmak ucunda sürünmesi, smaç temasında arka topuğun kalkması bu yüzden yoktu. Kanal model
#     dosyalarına girmez (dört model aynı kuralı alsın): poz kurulmadan önce bilek, tabanın zeminde kalacağı kadar yükseltilir
#     (topuk ya da parmak kökü ekseni, ölçü modelin kendi ayak ağından), kurulduktan sonra ayak kemiği bilekte döndürülür;
#     topuk kalkıyorsa parmak kemiği ters döndürülür ki parmaklar yerde düz kalsın. Ölçü bir kez alınır (_AG).
_AG={}
def _ayak_kemik(K):
    pb=OY.ao.pose.bones; R=OY.R; ft=R['ft'].format(S=R['S'][K.lower()])
    uc=[b for b in pb if b.parent and b.parent.name==ft]
    return pb[ft],(uc[0] if uc else None)
def _ayak_geo():
    if _AG.get('oy') is OY: return _AG
    _AG.clear(); _AG['oy']=OY
    L=bpy.context.view_layer; mw=OY.ao.matrix_world
    OY.kur_poz(READY); L.update(); z0=(mw@_ayak_kemik('R')[0].head).z
    OY.kur_poz(dict(READY,anR=(READY['anR'][0],READY['anR'][1],READY['anR'][2]+.02))); L.update()
    _AG['s']=max(1e-4,((mw@_ayak_kemik('R')[0].head).z-z0)/.02)   # model birimi / poz birimi (dikeyde)
    OY.kur_poz(READY); L.update(); dg=bpy.context.evaluated_depsgraph_get()
    deri=[o for o in OY.kalici if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)]
    for K in 'RL':
        fb,tb=_ayak_kemik(K); adlar={fb.name}|({tb.name} if tb else set()); pts=[]
        for o in deri:
            gi={g.index:g.name for g in o.vertex_groups}; e=o.evaluated_get(dg); me=e.to_mesh(); M=o.matrix_world
            for v in o.data.vertices:
                ws=[(g.weight,gi.get(g.group)) for g in v.groups]
                if ws and max(ws)[1] in adlar and max(ws)[0]>.5: pts.append(M@me.vertices[v.index].co)
            e.to_mesh_clear()
        A=mw@fb.head; f=(mw@tb.head-A) if tb else (mw.to_3x3()@(fb.matrix.to_3x3()@Vector((0,1,0)))); f.z=0; f.normalize()   # ileri = parmak kemiğine (Rocketbox ayak kemiğinin y'si tabana bakar)
        zs=min(p_.z for p_ in pts) if pts else A.z-.05
        hb=max([(A-p_).dot(f) for p_ in pts if p_.z<zs+.015] or [.05])            # topuğun bilek gerisindeki uzaklığı
        bl=(mw@tb.head) if tb else A+f*.1; fb_=(bl-A).dot(f)                     # parmak kökü (top) bileğin önünde
        _AG[K]=dict(hb=hb,fb=fb_,hz=A.z-zs)
    return _AG
def _ayak_cevir(p,AG):
    ao=OY.ao; pb=ao.pose.bones; L=bpy.context.view_layer
    up=(ao.matrix_world.inverted().to_3x3()@Vector((0,0,1))).normalized()
    for K in 'RL':
        a=math.radians(p.get('fp'+K,0) or 0)
        if not a: continue
        fb,tb=_ayak_kemik(K); M=fb.matrix.copy(); A=M.translation.copy()
        f=(tb.head-A) if tb is not None else M.to_3x3()@Vector((0,1,0)); f=f-up*f.dot(up); f.normalize(); lat=f.cross(up).normalized()
        R=Matrix.Translation(A)@Matrix.Rotation(a,4,lat)@Matrix.Translation(-A); fb.matrix=R@M; L.update()
        if tb is not None and a<0:
            T=tb.matrix.copy(); P=T.translation.copy(); tb.matrix=Matrix.Translation(P)@Matrix.Rotation(-a,4,lat)@Matrix.Translation(-P)@T; L.update()
def build(p):
    """p: poz sözlüğü → sahneye figürü kur"""
    if GERCEK and not p.get('hakem'): return build_gercek(p)
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
    HK=p.get('hakem',False); GV=CEKET if HK else FORMA; KOL=CEKET if HK else SKIN; BAC=PANT if HK else SKIN; AYK=SIYAH if HK else BEYAZ
    capsule('govde',root,chest,0.11,0.13,GV); sphere('karin',(root+chest)/2+Vector((0,0.01,0)),0.12,GV,(1,0.85,1))
    if HK: sphere('yaka',chest+Vector((0,0.085,0.035)),0.045,GOMLEK,(1,0.5,1.2))   # ceket önünde gömlek yakası
    capsule('sort',root-Vector((0,0,0.06)),root+Vector((0,0,0.04)),0.115,0.115,PANT if HK else SORT)
    capsule('boyun',chest+Vector((0,0,0.03)),neck+Vector((0,0,0.02)),0.045,0.045,SKIN)
    sphere('bas',hc,0.14,SKIN,(1,0.95,1.05)); sphere('sac',hc+Vector((0,-0.01,0.03)),0.145,SAC_GRI if p.get('gri') else SAC,(1,0.98,0.95))
    sphere('cene',hc+Vector((0,0.02,-0.08)),0.09,SKIN,(1,0.9,0.8))
    look=Vector(p.get('look',(0,1,0))).normalized(); side=look.cross(Vector((0,0,1))).normalized(); up=side.cross(look).normalized()
    for sx in (-1,1):
        sphere('goz'+str(sx),hc+look*0.12+side*sx*0.055,0.03,BEYAZ); sphere('bebek'+str(sx),hc+look*0.145+side*sx*0.055,0.016,SORT)
        sphere('kulak'+str(sx),hc+side*sx*0.14-Vector((0,0,0.01)),0.03,SKIN,(0.6,1,1))
    bpy.ops.mesh.primitive_torus_add(location=hc+look*0.125-Vector((0,0,0.055)),major_radius=0.035,minor_radius=0.006); sm=bpy.context.object; sm.name='gulus'; sm.rotation_mode='QUATERNION'; sm.rotation_quaternion=look.to_track_quat('Z','Y'); sm.data.materials.append(SORT); sm['tag']='sort'; objs.append(sm)
    # kafa bandı: ayrı nesne (maske); renk oyunda
    if not HK: bpy.ops.mesh.primitive_torus_add(location=hc+Vector((0,0,0.045)),major_radius=0.142,minor_radius=0.016,major_segments=40,minor_segments=8); bd=bpy.context.object; bd.name='kafabandi'; bd.scale=(1,0.97,1); bd.data.materials.append(BANT); bd['tag']='bant'; objs.append(bd)
    # kollar
    for side_,sh,pre in ((1,shR,'sag'),(-1,shL,'sol')):
        wr=Vector(p['wr'+pre[1].upper() if False else ('wrR' if side_>0 else 'wrL')]); hint=Vector(p.get('elR' if side_>0 else 'elL',(side_*1,-0.3,-0.6)))
        el,wr=ik(sh,wr,UA,FA,hint)
        sphere(pre+'_omuz',sh,0.062,GV)
        capsule(pre+'_ustkol',sh,el,0.05,0.042,KOL); capsule(pre+'_onkol',el,wr,0.04,0.034,KOL)
        hd=(wr-el).normalized(); sphere(pre+'_el',wr+hd*0.025,0.045,SKIN,(0.85,1,1.05))
        if HK: pass
        elif side_>0:
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
        capsule(pre+'_uyluk',hp,kn,0.062,0.05,BAC); capsule(pre+'_baldir',kn,an,0.048,0.036,BAC)
        fd=Vector(p.get('ftR' if side_>0 else 'ftL',(0,1,0))); fd.z=0; fd=fd.normalized()
        bpy.ops.mesh.primitive_cube_add(size=1,location=an+fd*0.04-Vector((0,0,0.02))); f=bpy.context.object; f.name=pre+'_ayakkabi'; f.scale=(0.075,0.14,0.05); f.rotation_euler=(0,0,math.atan2(fd.x,fd.y)*-1); f.data.materials.append(AYK); f['tag']='beyaz'
        md=f.modifiers.new('b','BEVEL'); md.width=0.02; md.segments=3; bpy.ops.object.shade_smooth(); objs.append(f)
        bpy.ops.mesh.primitive_cylinder_add(radius=0.07,depth=0.04,location=an+Vector((0,0,0.03))); s2=bpy.context.object; s2.name=pre+'_corap'; s2.data.materials.append(PANT if HK else BEYAZ); s2['tag']='beyaz'; objs.append(s2)
    if p.get('tablet'):   # BWF baş hakemi skoru tablete girer: iki elin arasında, kucakta/masada
        c_=Vector(p['tablet']); bpy.ops.mesh.primitive_cube_add(size=1,location=c_); tb=bpy.context.object; tb.name='tablet'; tb.scale=(0.20,0.14,0.012); tb.rotation_euler=(math.radians(p.get('tabletEgim',25)),0,0); tb.data.materials.append(TABLET); objs.append(tb)
        bpy.ops.mesh.primitive_plane_add(size=1,location=c_+Vector((0,-0.003,0.008))); ek=bpy.context.object; ek.name='tablet_ekran'; ek.scale=(0.18,0.12,1); ek.rotation_euler=(math.radians(p.get('tabletEgim',25)),0,0); ek.data.materials.append(EKRAN); objs.append(ek)

# ---------- pozlar ----------
def P_(**k): return k
# READY (2026-10-26, tüm beden turu, denetim §1): diz 55–69° bükük (çeyrek çömelme) ve gövde 6° GERİYE yatıktı; ayaklar omuz
#     genişliğinin altındaydı. Hazır duruş: diz ~40–50°, gövde ~10° öne (ağırlık ön tabanda), ayaklar ~1,2 × omuz, sağ ayak çok
#     hafif geride (vurus_biyomekanigi.md §1). Kök 0,40 → 0,43, lean 0,01 → 0,09, bilekler ±0,12 → ±0,13.
READY=dict(root=(0,0,0.43),lean=(0,0.09,0),twist=8,wrR=(0.24,0.24,0.53),elR=(1,-0.2,-0.5),rk=(0.05,0.45,0.85),rkFace=(1,0,0),wrL=(-0.22,0.20,0.49),elL=(-1,-0.3,-0.5),anR=(0.13,-0.01,0.05),anL=(-0.13,0.05,0.05),knR=(0,1,0),knL=(0,1,0),head=(0,0,0),look=(0,1,0))
def _tam(p):
    """Karışımdan önce sonradan eklenen kanalları açık yaz. 2026-10-24 (animasyon denetimi #8, #9): 'pelvis' (leğen dönüşü,
    twist ile aynı işaret) yazılmamışsa eski kural twist·0,35; 'burgu' (ön kol burulmasının ağırlığı, 0..1) yoksa 0.
    Açık yazılmazsa mix() eksik kanalı karşı pozdan kopyalardı: READY'den S_PREP'e ilk karede tam burgu sıçrardı."""
    if all(k in p for k in ('pelvis','burgu','fpR','fpL','ftR','ftL')): return p
    # fpR/fpL (2026-10-26, tüm beden turu): ayak eğimi, derece; + = burun yukarı (topuk basışı), − = topuk havada (parmak
    # ucunda itiş / arka ayak sürünmesi). Yazılmamışsa 0 = dinlenme eğimi (eski görünüm birebir).
    q=dict(p); q.setdefault('pelvis',.35*p.get('twist',0)); q.setdefault('burgu',0.); q.setdefault('fpR',0.); q.setdefault('fpL',0.)
    # ftR/ftL (2026-10-26, tüm beden turu): ayak ucu yönü yazılmamışsa (0,1,0) (build'in varsayılanı). Eskiden mix() eksik kanalı
    # karşı pozdan kopyaladığı için READY karesi (klibin ilk ve son karesi) vuruşun ayak yönünü taşıyordu: bh_over'ın ilk karesinde
    # sağ ayak 153° arkaya dönük, diz öne (diz yönü 177°); idle'dan klibe geçişte ayak tek karede dönüyordu.
    q.setdefault('ftR',(0,1,0)); q.setdefault('ftL',(0,1,0)); return q
# Raket yönü ve yüzü küre üstünde karışır (2026-10-24): doğrusal karışımda ters yönlü iki anahtarın ortası sıfıra yaklaşıyor,
# raket ara karede rastgele bir yöne bakıyordu (bh_over'da aşağıdan yukarı salınımın ortası; S_DROP→S_HIT de neredeyse ters).
# Büyük çember yolu raket başını gerçekteki gibi yandan dolaştırır; yolu anahtarın küçük sapması seçer (S_DROP'ta raket başı
# hafif dışa, böylece salınım sağ yandan geçer).
KURE=('rk','rkFace','ftR','ftL','knR','knL')   # ayak ve diz yönleri de (2026-10-26): ters yönlü iki anahtarın doğrusal ortası sıfırdan geçip ayağı/dizi tek karede çeviriyordu
def _slerp(va,vb,t):
    a=Vector(va); b=Vector(vb)
    if a.length<1e-6 or b.length<1e-6: return tuple(va[i]+(vb[i]-va[i])*t for i in range(3))
    a.normalize(); b.normalize(); d=max(-1.,min(1.,a.dot(b)))
    if d>.9999 or d<-.9999: return tuple(a.lerp(b,t))
    w=math.acos(d); return tuple((a*math.sin((1-t)*w)+b*math.sin(t*w))/math.sin(w))
def mix(a,b,t):
    a=_tam(a); b=_tam(b); o={}
    # burgu=0 olan pozda yüz yönü işaretsizdir (raket simetrik); avuç anlamı taşıyan komşuya ters bakıyorsa çevrilir ki
    # karışım ters yönden geçmesin (READY (1,0,0) → S_PREP (-1,0,.2) ortada yüzü sıfırlıyordu)
    if 'rkFace' in a and 'rkFace' in b and Vector(a['rkFace']).dot(Vector(b['rkFace']))<0:
        if a['burgu']==0 and b['burgu']>0: a=dict(a,rkFace=tuple(-x for x in a['rkFace']))
        elif b['burgu']==0 and a['burgu']>0: b=dict(b,rkFace=tuple(-x for x in b['rkFace']))
    for k in a:
        va=a[k]; vb=b.get(k,va)
        if k in KURE: o[k]=_slerp(va,vb,t)
        elif isinstance(va,(int,float)): o[k]=va+(vb-va)*t
        elif isinstance(va,(tuple,list)): o[k]=tuple(va[i]+(vb[i]-va[i])*t for i in range(len(va)))
        else: o[k]=va if t<.5 else vb
    for k in b:
        if k not in o: o[k]=b[k]
    return o
def ease(t): return .5-.5*math.cos(math.pi*t)
# Temas çevresinde kosinüs yok (2026-10-24, animasyon denetimi #7): kosinüs her anahtarda hızı sıfırlar, temas karesinde
# kol frenleyip duruyormuş gibi okunuyordu ("el yere konmuş"). Temasa giren parça hızlanır (u²: son kare en uzun adım),
# temastan çıkan parça yavaşlar (1-(1-u)²). Anahtarlar aynı; değişen yalnız aradaki karelerin dağılımı.
def ease_in(t): return t*t
def ease_out(t): return 1-(1-t)*(1-t)
class Klip(list):
    """kare listesi + oyunun ihtiyaç duyduğu kare numaraları: hit (temas karesi), hold (geri salınım tepesi: figür ileri
    salınımdan önce bunda bekler), drop (overhead raket düşüşü). meta.json'a ve degerler.json'a buradan yazılır."""
    hit=hold=drop=None
def _an(ks,t,hit,yavas):
    for j in range(len(ks)-1):
        t0,p0=ks[j]; t1,p1=ks[j+1]
        if t0<=t<=t1:
            u=(t-t0)/(t1-t0) if t1>t0 else 1
            e=ease(u) if (yavas or hit is None) else ease_in(u) if j+1==hit else ease_out(u) if j==hit else ease(u)
            return mix(p0,p1,e)
    return _tam(dict(ks[-1][1]))
ZEMIN=.05   # basan bileğin poz yüksekliği (READY ve ayak işi tabanı)
def _adim_kaldir(out,ks,den):
    """Adım yayı (2026-10-26, tüm beden turu; arastirma/animasyon_denetimi_2026-10-26.md §2): iki anahtarda da yerde olan ayak
    anahtarlar arasında yer değiştiriyorsa ara karelerde yerden kaldırılır. Eskiden bilek yalnız xy'de karışıyordu: vuruş
    kliplerinde basan ayak kare arasında 20–67 cm kayıyordu (hamlede sağ ayak öne 0,7 m, toparlanmada geri 0,7 m, yerde).
    Yay, karenin yol üstündeki konumundan (kolaylaştırılmış e) çıkar: tepe yolun ortasında, yükseklik yolun ~%12'si
    (3,5–7 poz cm = 6–12 cm oyunda). Anahtar karelere dokunmaz (temas pozları aynen kalır). Komşu iki karede adım atan
    ayak için ara kare yoktur: o zaman adım pozun kendisine (hazırlık anahtarına) taşınmalıdır, burada uyarı basılır."""
    for (ta,pa),(tb,pb) in zip(ks,ks[1:]):
        ia,ib=round(ta*den),round(tb*den)
        for K in 'RL':
            A=pa['an'+K]; B=pb['an'+K]; dx,dy=B[0]-A[0],B[1]-A[1]; h=math.hypot(dx,dy)
            if h<.03 or A[2]>ZEMIN+.025 or B[2]>ZEMIN+.025: continue
            if ib-ia<2: print(f'adım yayı: {K} ayağı komşu karelerde {h:.2f} birim yer değiştiriyor (kare {ia}→{ib})'); continue
            lift=min(.07,.025+.12*h)
            for i in range(ia+1,ib):
                c=out[i]['an'+K]; e=max(0.,min(1.,((c[0]-A[0])*dx+(c[1]-A[1])*dy)/(h*h)))
                out[i]['an'+K]=(c[0],c[1],round(c[2]+lift*math.sin(math.pi*e),4))
def sample(keys,n,loop=False,hit=None,hold=None,drop=None,lead=0,yavas=False):
    """keys: [(t, poz)], t 0..1; n kare. hit/hold/drop: anahtar sırası (keys içindeki indeks).
    lead: leğen kanalı bu kadar kare önden gelir. yavas: temasa yavaşlayarak gir (drop vuruşu)."""
    den=n if loop else max(1,n-1)
    # Anahtar zamanları kare ızgarasına oturur (denetim #15): kısa serviste SB2 0,50'deydi = kare 4,5; temas pozu hiçbir
    # karede tam görünmüyordu. Artık her anahtar pozu bir karede birebir çizilir ve oyunun CLIP_HIT'i o kareye eşitlenir.
    ks=[(math.floor(t*den+.5)/den,p) for t,p in keys]
    for (ta,pa),(tb,pb) in zip(ks,ks[1:]):
        assert tb>ta or pa is pb, f'iki farklı anahtar aynı kareye düştü (t={ta})'
    out=Klip(_an(ks,i/den,hit,yavas) for i in range(n))
    if not loop: _adim_kaldir(out,ks,den)
    # Kalça göğüsten önce (denetim #9): leğen kanalı aynı eğriden lead kare ileriden okunur. İleri salınımda kalça bir kare
    # önce açılır, hazırlıkta da önce döner; göğüs (twist) geride kalır, X-factor görünür. Ayaklar yerde, yalnız leğen.
    # Öne alma bekleme karesinden başlar: ilk kare READY ile birebir kalmalı (idle'dan geçişte kalça sıçramasın); bekleme
    # karesinde kalça zaten ileri açılmaya başlamış, göğüs geride (geri salınım sonundaki gerilme).
    if lead:
        i0=round(ks[hold][0]*den) if hold is not None else 0
        for i,p in enumerate(out):
            if i>=i0: p['pelvis']=_an(ks,min(1.,(i+lead)/den),hit,yavas)['pelvis']
    fk=lambda k: None if k is None else round(ks[k][0]*den)
    out.hit,out.hold,out.drop=fk(hit),fk(hold),fk(drop)
    return out
def D(base,**k):
    o=dict(base); o.update(k); return o
def bukuk(p,aci):
    """Dirseği aci dereceye büker (2026-10-24, denetim #12): kill, net, lunge ve alçak vuruş temasında bilek hedefi kol
    boyunun ötesindeydi, IK kolu 171°'de düz kilitliyordu (robotik). Bilek omuz→bilek doğrusunda geri çekilir; yön ve
    raket aynı kalır, uzanma kosinüs kuralıyla kısalır. Omuz build() ile aynı hesaplanır (soyut figür; insan.hedef kol
    vektörünü oranla taşıdığı için açı iki modelde de korunur)."""
    root=Vector(p['root']); chest=root+Vector((0,0,0.22))+Vector(p.get('lean',(0,0,0)))
    sh=chest+Matrix.Rotation(-math.radians(p.get('twist',0)),3,'Z')@Vector((SHW,0,0.04))
    L=math.sqrt(UA*UA+FA*FA-2*UA*FA*math.cos(math.radians(aci))); d=Vector(p['wrR'])-sh
    if d.length<=L: return p   # zaten bu kadar bükük: uzatmaz
    return D(p,wrR=tuple(round(x,4) for x in sh+d.normalized()*L))
def dongu(n,eksen,D,duty,dl,lift,taban,ust,root=.40,bob=.012,once='R',kn=None,ft=None,kol=0.,tw=0.,pel=0.):
    """Ayak işi döngüsü (2026-10-26, tüm beden turu; 3d-animator locomotion §1-§4): yerinde döngüde basan ayak gövdeye göre
    SABİT HIZLA geriye gider, böylece oyun kökü klibin adım boyu × faz hızıyla ilerlerken ayak yerde durur. Eski kliplerde
    bilek sin(faz) ile gidip geliyordu: basma ortasında hızlı, uçlarda duran ayak = döngü içinde kayma; adımlar da kısaydı
    (rocketbox ölçümü: back_l 0,21 m, chassé 0,54 m döngü; oyuncu ortalama 2,4–3,5 m/s koşuyor, faz 2,6 döngü/sn'de
    kırpılıyordu → %30–80 kayma).
    eksen: gidiş yönü (poz çerçevesi, +y file); D: döngü başına yol (poz birimi); duty: bir ayağın yerde kaldığı oran;
    dl: ikinci ayağın faz gecikmesi (0,5 = koşu/yürüyüş, küçük = chassé dörtnalı); lift: salınım tepesi; taban: {'R','L'}
    ayakların döngü ortasındaki konumu (gövdeye göre); once: fazı 0'da yere basan ayak (bütün ayak işi kliplerinde basış
    fazı 0 hizalı: klipler arası harmanlamada bacaklar çaprazlanmaz); kol: kol sallama genliği (raket önde kalır);
    tw/pel: göğüs/leğen karşı dönüşü (sağ ayak öndeyken göğüs sola)."""
    a=Vector((eksen[0],eksen[1],0)).normalized(); S=D*duty; out=Klip()
    for i in range(n):
        ph=i/n; p=dict(ust); zb=0.; s_=dict()
        for K in 'RL':
            phi=(ph-(0. if K==once else dl))%1.; c=Vector(taban[K]+(0,))
            if phi<duty: u=phi/duty; s=S/2-S*u; z=0.; zb+=math.sin(math.pi*u)
            else: u=(phi-duty)/(1-duty); s=-S/2+S*ease(u); z=lift*math.sin(math.pi*u)
            q=c+a*s; p['an'+K]=(round(q.x,4),round(q.y,4),round(ZEMIN+z,4)); s_[K]=s
        sw=(s_['R']-s_['L'])/max(S,1e-4)   # +1: sağ ayak önde (gidiş yönünde)
        r=ust.get('root',(0,0,root)); p['root']=(r[0],r[1],round(root-bob*min(1.,zb)+bob*.5,4))
        if kn: p['knR'],p['knL']=kn
        if ft: p['ftR'],p['ftL']=ft
        if tw: p['twist']=ust.get('twist',0)+tw*sw
        if pel: p['pelvis']=ust.get('pelvis',.35*ust.get('twist',0))-pel*sw
        if kol:
            wl=ust['wrL']; p['wrL']=(wl[0],round(wl[1]+kol*sw*a.y,4),round(wl[2]+.3*kol*abs(sw),4))   # sağ bacak önde → sol kol önde
            wr_=ust['wrR']; p['wrR']=(wr_[0],round(wr_[1]-.35*kol*sw*a.y,4),wr_[2])
        out.append(p)
    return out
# --- hazır: hafif zıplayan bekleyiş (8 kare döngü)
# idle: hazır duruşta hafif nefes/yaylanma; 2026-10-26: kök ±1,25 → ±0,8 poz cm (diz 55↔69° pompalıyordu; şimdi ~6° salınım)
idle=sample([(0,READY),(0.5,D(READY,root=(0,0,0.438),wrR=(0.24,0.24,0.56),wrL=(-0.22,0.20,0.52))),(1,READY)],8,loop=True)
# --- koşu (8 kare döngü, öne hızlı; 2026-10-26 tüm beden turu): dongu() ile basan ayak sabit hızla geri gider; uçuş evreli koşu
#     (duty 0,38), döngü 1,0 poz (sade modelde ~2,2 m; modelin bacak oranı k ≈ 1,26 ile): 5,2 m/s'de ~2,4 döngü/sn. Gövde öne eğik, kollar karşıt, raket
#     önde yukarıda (vuruşa hazır). Eski klipte bilek sin(faz) ile gidip geliyordu ve oyunda hiç seçilmiyordu (Unity
#     AyakIsi yalnız fwd/chassé/back seçerdi); şimdi hızlı öne koşuda seçilir.
_RUN_UST=D(READY,root=(0,0,.41),lean=(0,0.12,0),twist=0,wrR=(0.26,0.18,0.58),wrL=(-0.20,0.06,0.44))
run=dongu(8,(0,1),1.0,.38,.5,.09,{'R':(0.10,0.0),'L':(-0.10,0.0)},_RUN_UST,root=.41,bob=.02,kol=.12,tw=10,pel=8)
# --- smaç (baş üstü, güçlü): hazırlık (raket sırt kaşır, sol el topu gösterir, gövde yay) → temas (kol tam uzanmış,
#     bilek başın hafif önünde) → devam (raket çapraz aşağı, sol kol çekilmiş) → toparlanma
# 2026-10-24 (animasyon denetimi #6, #8, #9; arastirma/animasyon_denetimi_2026-10-24.md): raket başı hazırlıkta hep baş
#     üstündeydi (en alçak 2,12 m), smacın en tanınan anı olan raket düşüşü yoktu. S_PREP (kupa pozu: dirsek omuz hizasında,
#     raket yukarıda) ile S_HIT arasına S_DROP girer: kalça açılmış, göğüs hâlâ yan, dirsek önde ve omuz hizasında, ön kol
#     dışa dönük, raket başı sırtın arkasında ~1,1 m (King vd. 2020 "racket lowest point"). Klip 10 → 14 kare; yeni
#     karelerin çoğu düşüş ile temas arasında (kare 5 → 9, u² ile hızlanır).
#     rkFace burada AVUÇ yönüdür (burgu=1 olan pozlarda ön kol bu yöne burulur, insan.on_kol_burgu): kupa ve düşüşte avuç
#     başa bakar (raket kenarı önde), temasta hedefe, takipte dışa = sürekli pronasyon. Dönüşün çoğu temastan önceki son iki
#     karede (u² eğrisi). pelvis: hazırlıkta kalça göğsün çok gerisinde değil (X-factor ~37°), düşüşte kalça çoktan açık.
S_PREP=D(READY,root=(0,-0.03,0.40),lean=(0,-0.07,0.01),twist=62,pelvis=25,wrR=(0.20,-0.22,0.86),elR=(1,-0.5,0.3),rk=(-0.2,-0.3,0.93),rkFace=(-1,0,0.2),burgu=1.,wrL=(-0.12,0.22,0.95),elL=(-1,0.2,0),anR=(0.16,-0.14,0.05),anL=(-0.10,0.14,0.05),look=(0,0.4,1),head=(0,-0.02,0))
S_DROP=D(S_PREP,root=(0,-0.01,0.41),lean=(0,-0.03,0.015),twist=40,pelvis=2,wrR=(0.22,-0.07,0.84),elR=(1,0.6,-0.1),rk=(0.22,-0.48,-0.85),rkFace=(-1,-0.5,0),wrL=(-0.16,0.20,0.80),elL=(-1,0,-0.2),anR=(0.15,-0.12,0.05),look=(0,0.5,0.9))
S_HIT=D(READY,root=(0,0.02,0.43),lean=(0,0.05,0.02),twist=-12,pelvis=-24,wrR=(0.10,0.20,1.22),elR=(1,0.2,0.3),rk=(0,0.55,0.83),rkFace=(0.3,1,0),burgu=1.,wrL=(-0.20,0.16,0.60),elL=(-1,-0.3,-0.4),anR=(0.15,-0.12,0.05),fpR=-25,anL=(-0.10,0.14,0.05),look=(0,0.6,0.8))
S_FOL=D(READY,root=(0,0.07,0.37),lean=(0,0.13,-0.02),twist=-40,pelvis=-38,wrR=(-0.20,0.28,0.42),elR=(1,0.5,-0.3),rk=(-0.4,0.5,-0.75),rkFace=(1,0.2,0),burgu=1.,wrL=(-0.22,0.02,0.40),elL=(-1,-0.5,-0.3),anR=(0.12,0.24,0.05),fpR=8,anL=(-0.10,0.14,0.05),fpL=-15,look=(0,1,-0.2))
# 2026-10-26 (tüm beden turu, denetim §2.5): düşüşten temasa sağ ayak yerde 6 cm öne, sol ayak 3 cm, takipte sol ayak yerde
#     0,1 poz (~22 cm) geri kayıyordu. Şimdi sağ ayak temasta yerinde parmak ucunda (fpR −25°: kalça dönerken arka ayak
#     ön tabanda pivot), sol ayak takip boyunca yerinde basar (topuk kalkık); sağ ayak takipte havadan öne gelir (ara kare
#     yayı) ve topuktan basar: smacın 'arka ayak öne geçer' ağırlık aktarımı.
# 14 kare: 0 hazır, 2 kupa (bekleme karesi), 5 düşüş, 9 temas, 11 takip, 13 hazır. Smaç, clear ve jump aynı ızgarada:
#     ilk 6 kare (hazır→düşüş) üçünde birebir aynı, rakip vuruşu temastan önce okuyamaz.
OV=lambda k: k/13
smash=sample([(0,READY),(OV(2),S_PREP),(OV(5),S_DROP),(OV(9),S_HIT),(OV(11),S_FOL),(1,READY)],14,hit=3,hold=1,drop=2,lead=1)
# --- clear (baş üstü, uzun): aynı yay ama temas başın hafif arkasında, devam öne-yukarı (topu taşır)
C_HIT=D(S_HIT,wrR=(0.12,-0.02,1.27),rk=(0,0.2,0.98),lean=(0,0,0.02),pelvis=-20)
C_FOL=D(S_FOL,wrR=(-0.14,0.30,0.62),rk=(-0.5,0.75,0.2),root=(0,0.05,0.40),lean=(0,0.07,0),twist=-30,pelvis=-32,anR=(0.12,0.12,0.05))
clear=sample([(0,READY),(OV(2),S_PREP),(OV(5),S_DROP),(OV(9),C_HIT),(OV(11),C_FOL),(1,READY)],14,hit=3,hold=1,drop=2,lead=1)
# --- drop (denetim #13): clear ile aynı gövde ve ilk 6 kare (aldatma: Badminton Bible "deception"), temasa YAVAŞLAYARAK
#     girer (kosinüs, u² değil) ve takip kısa: raket temas noktasının biraz önünde durur, gövde clear kadar dönmez.
DP_HIT=D(C_HIT,wrR=(0.12,0.04,1.25),rk=(0,0.35,0.94),twist=-6,pelvis=-16)
DP_FOL=D(DP_HIT,wrR=(0.08,0.16,1.10),rk=(-0.1,0.75,0.65),twist=-14,pelvis=-20,root=(0,0.04,0.42),lean=(0,0.07,0.01),wrL=(-0.20,0.10,0.56),look=(0,1,0.3))
drop=sample([(0,READY),(OV(2),S_PREP),(OV(5),S_DROP),(OV(9),DP_HIT),(OV(10),DP_FOL),(1,READY)],14,hit=3,hold=1,drop=2,lead=1,yavas=True)
# --- drive (omuz hizası). 2026-10-21 (yasin: "drive'larda anatomi gerçekçi değil"; arastirma/reports/Badminton alçak
#     vuruş ve servis.md §drive): eskisi tenis gibi büyük geri salınımdı (el gövdenin arkasında, raket geriye yatık).
#     Gerçekte: sağ ayak sağa adım, gövde fileye neredeyse kare; KISA hazırlık (dirsek bükük ve yukarıda, raket başı
#     yukarıda, bilek geriye kırık); temas sağ omzun önünde; güç omuz iç rotasyonu + pronasyon + bilekten; takip kısa,
#     gövdenin önünden sola geçmez.
DR_PREP=D(READY,twist=14,lean=(0.02,0.0,0),wrR=(0.32,0.04,0.74),elR=(1,-0.3,-0.2),rk=(0.25,-0.35,0.90),rkFace=(0.3,1,0),wrL=(-0.18,0.20,0.60),anR=(0.26,0.02,0.05),anL=(-0.12,0.02,0.05),look=(0.3,1,0.1))
DR_HIT=D(READY,twist=0,lean=(0.03,0.03,-0.01),root=(0.03,0.02,0.38),wrR=(0.36,0.34,0.74),elR=(1,0.1,-0.4),rk=(0.35,0.92,0.15),rkFace=(0,1,0),wrL=(-0.20,0.12,0.56),anR=(0.30,0.06,0.05),anL=(-0.12,0.0,0.05),ftR=(0.7,0.7,0))
DR_FOL=D(READY,twist=-8,lean=(0.02,0.04,0),root=(0.02,0.02,0.39),wrR=(0.22,0.40,0.72),elR=(1,0.3,-0.4),rk=(-0.15,0.95,0.25),rkFace=(0,1,0),wrL=(-0.20,0.08,0.52),anR=(0.28,0.06,0.05),anL=(-0.12,0.0,0.05))
drive=sample([(0,READY),(0.3,DR_PREP),(0.55,DR_HIT),(0.72,DR_FOL),(1,READY)],8,hit=2,hold=1,lead=1)
# --- file öldürme (kısa, yüksek dürtme): raket önde yüksek, bilek kırar
# 2026-10-26 (tüm beden turu): öne adım hazırlıktan temasa KOMŞU karelerde atılıyordu (sağ ayak yerde 0,15 poz ≈ 33 cm kayma);
#     adım hazırlığa taşındı (hazır → hazırlık arası ara karede havadan), temas ve takipte ayaklar yerinde. Takip anahtarı
#     0,75 → 0,7: dönüş adımının da ara karesi olsun.
K_PREP=D(READY,wrR=(0.22,0.10,0.95),elR=(1,-0.3,-0.4),rk=(0.1,0.3,0.95),rkFace=(0,1,0),lean=(0,0.05,0),root=(0,0.04,0.41),anR=(0.14,0.14,0.05),anL=(-0.12,0.0,0.05),look=(0,0.7,0.7))
K_HIT=D(READY,wrR=(0.16,0.34,0.98),elR=(1,-0.2,-0.3),rk=(0.1,0.95,-0.3),rkFace=(0,0.4,1),lean=(0,0.08,0),root=(0,0.05,0.40),anR=(0.14,0.14,0.05),anL=(-0.12,0.0,0.05),look=(0,0.9,0.4))
K_HIT=bukuk(K_HIT,120)   # denetim #12: kill dirseği önde ve bükük, kısa "tap" (eskiden 171° düz)
K_FOL=D(K_HIT,wrR=(0.12,0.30,0.88),rk=(0.1,0.9,-0.45))
kill=sample([(0,READY),(0.3,K_PREP),(0.55,K_HIT),(0.7,K_FOL),(1,READY)],7,hit=2,hold=1,lead=1)
# --- alçak forehand (lift / file önü). 2026-10-21: eski hazırlıkta el kalçanın ARKASINDAYDI (tenis forehand'ı gibi).
#     Gerçekte raket erken ve önde sunulur: sağ ayak öne adım (topuk önce), gövde dik (20–30° eğim), sol kol dengede
#     geride uzanır; kısa geri salınım = önkol dışa döner, raket başı elin altına düşer; temas sağ ayağın önünde alçakta,
#     dirsek hafif bükük; takip kısa, hedefe doğru ileri-yukarı.
# 2026-10-26 (tüm beden turu, denetim §2.3): arka (sol) ayak hazırlıktan temasa yerde 0,15–0,24 poz (0,35–0,5 m) GERİYE
#     kayıyordu; adımı atan ön ayak hazırlıkta havada asılı duruyordu. Şimdi hazırlıkta iki ayak yerde (ağırlık sola), adım
#     hazırlıktan temasa ara karede havadan atılır (_adim_kaldir), ön ayak temasta topuktan basar (fpR +14°), arka ayak yerinde
#     kalır ve topuğu kalkar (fpL −20°: itiş), takipte ön taban düzleşir. Diz ön ayak bileğinin üstünde.
L_PREP=D(READY,twist=10,lean=(0.01,0.07,-0.02),root=(0,0.04,0.40),wrR=(0.28,0.24,0.37),elR=(1,-0.1,-0.3),rk=(0.25,-0.25,-0.93),rkFace=(0,1,0.3),wrL=(-0.26,-0.10,0.48),elL=(-1,-0.4,-0.3),anR=(0.14,0.04,0.05),anL=(-0.13,0.05,0.05),fpL=-8,look=(0.1,1,-0.3))
L_HIT=D(READY,twist=2,lean=(0.01,0.08,-0.03),root=(0.02,0.16,0.35),wrR=(0.26,0.44,0.26),elR=(1,0.2,-0.2),rk=(0.15,0.75,-0.64),rkFace=(0,0.7,0.7),wrL=(-0.28,-0.18,0.44),elL=(-1,-0.5,-0.2),anR=(0.16,0.36,0.05),fpR=14,ftR=(0.1,1,0),anL=(-0.12,0.03,0.05),fpL=-20,ftL=(-0.35,1,0),look=(0.1,1,-0.3))
L_FOL=D(READY,twist=-4,lean=(0,0.07,-0.01),root=(0.02,0.15,0.37),wrR=(0.20,0.48,0.56),elR=(1,0.3,-0.3),rk=(0.05,0.55,0.83),rkFace=(0,1,0.3),wrL=(-0.26,-0.16,0.44),anR=(0.16,0.36,0.05),ftR=(0.1,1,0),anL=(-0.12,0.03,0.05),fpL=-15,ftL=(-0.35,1,0))
L_HIT=bukuk(L_HIT,150)   # denetim #12: dirsek hafif bükük (171° kilitliydi)
# 2026-10-26 (fizik denetimi §4.3): low/net'te raket kafası zemine ~5 cm giriyordu; bilek 3,1 poz cm (oyunda ~10 cm) yukarı
L_HIT=D(L_HIT,wrR=(L_HIT['wrR'][0],L_HIT['wrR'][1],L_HIT['wrR'][2]+.031))
low=sample([(0,READY),(0.32,L_PREP),(0.55,L_HIT),(0.75,L_FOL),(1,READY)],8,hit=2,hold=1,lead=1)
# --- net vuruşu (denetim #13): low ile aynı gövde ve aynı ilk 5 kare; takip yok. Raket başı temastan sonra 10-20 cm
#     ileri gider ve orada kalır (eski 'low' takibi raketi baş hizasına kaldırıyordu: lift için doğru, net için değil).
N_FOL=bukuk(D(L_HIT,wrR=(0.26,0.47,0.32),rk=(0.15,0.80,-0.58),fpR=4,fpL=-15,look=(0.1,1,-0.2)),150)
net=sample([(0,READY),(0.32,L_PREP),(0.55,L_HIT),(0.72,N_FOL),(1,READY)],8,hit=2,hold=1,lead=1)
# --- hamle (lunge). 2026-10-26 (tüm beden turu, denetim §2.2): eski klipte arka ayak yerde 0,5 m geriye kayıyor, temasta
#     bacak boyunu aşıp 16 cm havada kalıyordu (IK kırpması); toparlanmada ön ayak yerde 0,7 m geri sürünüyordu; ayak hiç
#     topuktan basmıyordu. Şimdi (vurus_biyomekanigi.md §7 "Lunge"): hazırlıkta iki ayak yerde, ağırlık sol ayakta; ön (sağ)
#     ayak ara karede havadan uzanır, temasta TOPUKTAN basar (fpR +18°), takipte taban düzleşir ve diz en derine iner
#     (~90°, diz ayak bileğinin üstünde, burnu geçmez); arka ayak yerinde kalır, topuğu kalkık parmak ucunda (fpL −30°),
#     ayak ucu dışa; gövde dik (~10–15° öne), sol kol arkada denge. Toparlanma: sağ topuktan geri itiş, ön ayak havadan
#     geri adım (LU_REC), sonra hazır duruş. Ön ayak kökten ~0,5 poz (~1,1 m), iki ayak arası ~1,05 m (sade).
LU_PREP=D(READY,twist=12,lean=(0,0.08,-0.02),root=(0,0.06,0.40),wrR=(0.28,0.20,0.30),elR=(1,-0.2,0.5),rk=(0.1,0.8,-0.5),rkFace=(0,1,0),wrL=(-0.24,0.0,0.50),anR=(0.14,0.06,0.05),anL=(-0.13,0.04,0.05),fpL=-10)
LU_HIT=D(READY,twist=10,lean=(0,0.07,-0.03),root=(0,0.28,0.31),wrR=(0.22,0.72,0.16),elR=(1,0.4,0.6),rk=(0.1,0.95,-0.2),rkFace=(0,0.3,1),wrL=(-0.26,-0.20,0.42),elL=(-1,-0.5,0.2),anR=(0.15,0.50,0.05),fpR=18,ftR=(0.1,1,0),anL=(-0.12,0.02,0.05),fpL=-30,ftL=(-0.5,1,0),knR=(0.05,1,0),knL=(-0.2,1,0),look=(0,1,-0.4))
LU_HIT=bukuk(LU_HIT,150); LU_FOL=bukuk(D(LU_HIT,root=(0,0.32,0.275),wrR=(0.16,0.66,0.30),rk=(0,0.9,0.4),fpR=0,anL=(-0.12,0.04,0.05)),150)   # denetim #12
LU_REC=D(READY,twist=8,lean=(0,0.08,-0.01),root=(0,0.14,0.39),wrR=(0.22,0.40,0.42),wrL=(-0.24,0.02,0.46),anR=(0.14,0.24,0.12),fpR=-10,ftR=(0.1,1,0),anL=(-0.13,0.05,0.05),fpL=-10,look=(0,1,-0.1))
lunge=sample([(0,READY),(0.35,LU_PREP),(0.6,LU_HIT),(0.78,LU_FOL),(6/7,LU_REC),(1,READY)],8,hit=2,hold=1,lead=1)
# --- servisler (2026-10-21, yasin: "kısa servisi bir badminton sporcusu gibi atmalı, uzun servis de buna göre; kısa servis
#     pozisyonunda flick de atabilmeliyim"; arastirma/reports/Badminton alçak vuruş ve servis.md §servis).
# kısa servis (backhand): sağ ayak önde, ayaklar omuz genişliğinde kademeli, gövde fileye kare; backhand tutuş,
#     raket bel önünde, başı aşağı, dirsek yukarı-öne; tüy top sol elde tüylerinden, raketin hemen önünde ve 1,15 m
#     altında. Geri salınım birkaç santim; başparmakla kısa itiş; takip raket yüzü fileye bakar, göğüs hizasının altında kalır.
# 2026-10-21 (2. tur, yasin: "kısa serviste anatomik hareketler sergilenmiyor"): eski pozda el kalça hizasında, dirsek
#     gövdeye yapışık ve raket bacakların arasından dik sarkıyordu. Elit tekler kısa servisinde (Axelsen, Lee Zii Jia;
#     arastirma/research_notes/backhand_servis_anatomi.md) el bel–göbek önünde, DİRSEK DIŞA VE YUKARI kalkık, önkol
#     gövdenin önünde yataya yakın; raket başı sola-aşağı ~35° eğik, yüzü fileye; top sol elde tüylerinden, raket yüzünün
#     hemen önünde. Geri salınım yok; başparmak itişi raketi birkaç santim öne taşır, takip göğüs altında biter.
SB0=D(READY,root=(0,0.03,0.41),twist=-10,lean=(0,0.04,0),anR=(0.04,0.14,0.05),anL=(-0.14,-0.10,0.05),ftR=(-0.1,1,0),ftL=(-0.3,1,0),
      wrR=(0.03,0.24,0.56),elR=(1,0.4,0.5),rk=(-0.80,0.22,-0.55),rkFace=(0,1,0),wrL=(-0.23,0.30,0.40),elL=(-1,-0.2,-0.6),shuttle=True,look=(0,1,-0.15))
SB1=D(SB0,wrR=(0.03,0.22,0.56))
SB2=D(SB0,wrR=(0.03,0.29,0.57),rk=(-0.72,0.45,-0.52),wrL=(-0.26,0.20,0.40),shuttle=False)
SB3=D(SB2,wrR=(0.02,0.34,0.60),rk=(-0.55,0.80,-0.22),rkFace=(0,0.8,0.6),wrL=(-0.24,0.12,0.42),look=(0,1,0))
serve_bh=sample([(0,SB0),(0.22,SB0),(0.40,SB1),(0.50,SB2),(0.68,SB3),(1,READY)],10,hit=3)   # ızgarada SB2 = kare 5 (denetim #15)
# flick: SB0/SB1 birebir aynı (aldatmanın şartı); temasta geç ve kısa bir bilek/başparmak kırılması raket başını yukarı fırlatır
SF2=D(SB0,wrR=(0.03,0.30,0.60),rk=(-0.45,0.55,0.70),rkFace=(0,0.6,0.8),wrL=(-0.26,0.20,0.40),shuttle=False)
SF3=D(SF2,wrR=(0.0,0.30,0.70),rk=(-0.25,0.25,0.93),wrL=(-0.24,0.12,0.42),look=(0,0.8,0.6))
serve_flick=sample([(0,SB0),(0.22,SB0),(0.40,SB1),(0.50,SF2),(0.66,SF3),(1,READY)],10,hit=3)
# yüksek servis (forehand, teklerde uzun): yan duruş, sol ayak önde, ağırlık arka (sağ) ayakta; sol el topu önde omuz
#     hizasında tutar ve bırakır (atmaz); raket arkada yukarıdan uzun ve gevşek iner; temas ön ayak hizasında alçakta,
#     önkol rotasyonu; takip sol omzun üstüne, ağırlık öne geçer; temastan sonra arka ayak öne kayar.
SH0=D(READY,root=(0,-0.03,0.395),twist=55,lean=(0,-0.03,0),anL=(-0.04,0.17,0.05),anR=(0.14,-0.16,0.05),ftL=(0.2,1,0),ftR=(0.9,0.4,0),
      wrR=(0.30,-0.20,0.74),elR=(1,-0.5,0.2),rk=(0.2,-0.5,0.85),rkFace=(1,0,0),wrL=(0.04,0.28,0.68),elL=(-1,0.2,-0.4),shuttle=True,look=(0,1,0))
SH1=D(SH0,root=(0,-0.02,0.39),twist=45,wrR=(0.30,-0.10,0.36),elR=(1,-0.4,0.2),rk=(0.2,-0.45,-0.87),wrL=(-0.06,0.24,0.62),shuttle=False)
SH2=D(SH1,root=(0,0.04,0.40),twist=12,lean=(0,0.02,0),wrR=(0.18,0.30,0.34),elR=(1,0,0.2),rk=(0.10,0.92,-0.37),rkFace=(0,0.5,0.85),wrL=(-0.14,0.16,0.56))
SH3=D(SH2,root=(0,0.08,0.41),twist=-35,lean=(0,0.04,0),wrR=(-0.22,0.18,0.92),elR=(1,0.4,0.4),rk=(-0.45,-0.2,0.87),rkFace=(0,1,0),wrL=(-0.20,0.02,0.50),anR=(0.12,0.0,0.07))
serve=sample([(0,SH0),(0.18,SH0),(0.38,SH1),(0.55,SH2),(0.75,SH3),(1,READY)],20,hit=3,lead=1)
# 20 kare (yasin 2026-10-22 "uzun serviste animasyon takılıyor"): 10 karede temasa kadar 6,5 kare 0,56 sn'ye yayılıyordu
#     = saniyede ~11 poz, her poz 6 ekran karesi bekliyordu. 20 karede ~23 poz/sn. Klip iki sıraya yayılır (sheet.py).
# Benim uzun servisim: servis türünü kaydırma seçtiği için beklerken backhand kısa servis duruşundayım; doğrudan
#     'serve'e geçince duruş tek karede sıçrıyordu. Geçiş klibi aynı salınımı kısa servis duruşundan başlatır: ilk %22'de
#     gövde döner, sol ayak öne, raket arkaya gider. Rakip 'serve'i kullanır (duruşu servis türünü önceden ele verir).
serve_gec=sample([(0,SB0),(0.06,SB0),(0.24,SH0),(0.42,SH1),(0.58,SH2),(0.77,SH3),(1,READY)],20,hit=4,lead=1)
# --- blok (ağa yakın kısa itiş): raket göğüs önünde dik, küçük hareket
# savunma bloğu: geniş alçak duruş, raket bel önünde (diz ile göğüs arası), çok kısa itiş, takip yok
# 2026-10-26: ayaklar ±0,20'den ±0,15'e (blok 6 karede, dönüş komşu karelerde: geniş duruş 0,07 poz ≈ 15 cm kayarak toplanıyordu;
#     şimdi hazır duruştan ~2 poz cm geniş, alçaklık dizden gelir).
B_LOW=D(READY,root=(0,-0.01,0.35),lean=(0,0.07,-0.02),anR=(0.15,0.0,0.05),anL=(-0.15,0.04,0.05),knR=(0.25,1,0),knL=(-0.25,1,0),wrR=(0.04,0.24,0.46),elR=(1,-0.4,-0.3),rk=(-0.1,0.6,0.8),rkFace=(0,1,0),wrL=(-0.20,0.16,0.42))
B_HIT=D(B_LOW,wrR=(0.04,0.34,0.50),rk=(-0.1,0.7,0.7),look=(0,1,0.1))
block=sample([(0,READY),(0.3,B_LOW),(0.6,B_HIT),(0.8,B_LOW),(1,READY)],6,hit=2,hold=1)

# ================= v16: raket HEP sağ elde (yasin 2026-10-19: "rakette el değiştirilmez"). Backhand aynalama değil,
# gerçek backhand pozu: kol gövdeyi çaprazlar, dirsek topa doğru, el sırtı önde, devam kısa. =================
# --- backhand overhead (sırt file'ye döner, dirsek yukarı, bilek kırılır; devam kısa)
# 2026-10-24 (denetim #8, #14): temasta ve takipte rkFace AVUÇ yönü (burgu=1): avuç fileden öteye, el sırtı fileye. Hazırlıkta
#     burgu yok: bu salınımda dirsek temastan önce düzleşiyor, bükme düzlemi (anatomik nötr) iki karede ~100° dönüyor ve
#     hazırlıktaki avuç hedefi iki modelde sınıra dayanıp temasta 160°'lik tek kare dönüş üretiyordu. Burulma hazırlıktan
#     temasa u² ile girer: supinasyonun çoğu son iki karede. BO_PREP yüzü burgu=0 olduğu için işaretsiz; (-0.15,1,0) ile
#     BO_HIT'e tam ters değil (küre karışımı ortada yön seçebilsin). Takip kısa ve sert: raket temas noktasının hemen
#     ötesinde durur, yatay dışa savrulmaz (kaynak: "tık" diye biten vuruş).
# Boş el ve raket eli (2026-10-26, iç içe geçme denetimi): twist −105…−125'te gövdenin önü −x'e bakar; eski wrL (0.10,0.06/0.08)
#     sırtın arkasında kalıyordu ve READY'ye dönüşte (ara kareler) ön kol gövdenin içinden geçiyordu (oyuncak 3B'de 4–9 cm).
#     Yeni wrL gövdenin sol-önünde (gövde çerçevesinde ~20 cm sol, ~10 cm ön), dirsek dışa. BO_PREP raket eli çeneye 6 cm
#     giriyordu: 4 cm sol omuza doğru açıldı. Tüm modeller için aynı poz; HTML sprite'ı yeniden çizilmeli.
BO_PREP=D(READY,root=(-0.03,-0.08,0.38),twist=-125,lean=(-0.02,-0.06,0.01),wrR=(-0.05,-0.18,0.78),elR=(-0.4,-1,0.8),rk=(-0.2,-0.4,-0.9),rkFace=(-0.15,1,0),wrL=(0.03,-0.22,0.52),elL=(0.6,-0.8,-0.5),anR=(-0.09,-0.19,0.05),anL=(0.04,0.06,0.05),ftR=(-0.6,-1,0),knR=(-0.6,-1,0),ftL=(0.3,1,0),look=(-0.5,-0.5,0.7))
BO_HIT=D(READY,root=(-0.03,-0.07,0.39),twist=-105,lean=(-0.02,-0.03,0.02),wrR=(-0.20,-0.10,1.22),elR=(-0.5,-1,0.6),rk=(-0.2,-0.2,0.96),rkFace=(0,-1,0),burgu=1.,wrL=(-0.07,-0.21,0.50),elL=(0.3,-0.95,-0.5),anR=(-0.09,-0.19,0.05),anL=(0.04,0.06,0.05),ftR=(-0.6,-1,0),knR=(-0.6,-1,0),ftL=(0.3,1,0),look=(-0.4,-0.4,0.8))
BO_FOL=D(BO_HIT,twist=-98,lean=(-0.02,-0.02,0.015),wrR=(-0.21,-0.04,1.16),rk=(-0.25,0.12,0.96),rkFace=(0.3,-1,0))
BO_FOL=bukuk(BO_FOL,158)
# 2026-10-26 (tüm beden turu): kök hazır duruş yüksekliğindeydi (0,43), sağ ayak kökten 0,27 poz arkada: sağ diz 7° (kilitli,
#     arka köşeye uzanan düz bacak). Kök arka ayağa doğru 0,08 kaydı, 0,38–0,39'a indi, sağ ayak 5 poz cm
#     yaklaştı: ağırlık sağ ayakta, diz ~30°. Sağ diz ipucu ayak ucuyla aynı yöne (−0,6,−1): eskiden varsayılan (öne) kalıyor,
#     ayak arkaya dönükken diz 175° ters yöne bakıyordu.
# Bekleme kopyası (eski 0,26→0,40 BO_PREP) kalktı: oyun hazırlıkta hold karesinde bekler; kareler ileri salınıma verildi.
bh_over=sample([(0,READY),(2/9,BO_PREP),(5/9,BO_HIT),(6/9,BO_FOL),(1,READY)],10,hit=2,hold=1,lead=1)
# --- backhand drive (2026-10-21): başparmak tutuş, dirsek önde ve topa dönük, raket başı arkadan gelir; temas vuran
#     omzun önünde, kol hafif bükük; supinasyon + bilek açılması; geri salınım çok kısa, takip gövdenin önünden
#     SAĞA savrulmaz (eskiden kol gövdeyi çaprazlayıp sağa uzanıyordu). Sağ ayak sola-öne adım atar.
# 2. tur (2026-10-21, yasin: "backhand vuruşta anatomik hareketler yok"): eski pozda gövde fileye kare kalıyordu ve
#     sağ ayak yerinde duruyordu. Backhand'de sağ omuz fileye döner (göğüs sola, sırtın bir kısmı fileye), sağ ayak
#     sola-öne çapraz adım atar, el sol göğüs önünde, dirsek alt kaburga hizasında ÖNDE ve topa dönük, raket başı sol
#     omzun arkasında; temas vuran omzun önünde, el sırtı fileye; takip kısa, sağa savrulmaz. Sol kol geride dengede.
BD_PREP=D(READY,twist=-55,lean=(-0.02,0.01,0),root=(-0.03,0.02,0.39),wrR=(-0.14,0.10,0.60),elR=(0.4,1,-0.3),rk=(-0.50,-0.55,0.67),rkFace=(0,1,0),wrL=(-0.18,-0.20,0.48),elL=(-1,-0.5,-0.3),anR=(-0.14,0.20,0.06),anL=(-0.10,-0.12,0.05),ftR=(-0.6,0.8,0),look=(-0.5,1,0.05))
BD_HIT=D(BD_PREP,twist=-40,lean=(-0.02,0.04,0),wrR=(-0.20,0.38,0.64),elR=(0.3,1,-0.2),rk=(-0.70,0.55,0.10),rkFace=(0,1,0),anR=(-0.16,0.24,0.05),look=(-0.4,1,0))
BD_FOL=D(BD_HIT,twist=-32,wrR=(-0.12,0.44,0.66),rk=(-0.15,0.95,0.25))
bh_drive=sample([(0,READY),(0.3,BD_PREP),(0.55,BD_HIT),(0.72,BD_FOL),(1,READY)],8,hit=2,hold=1,lead=1)
# --- backhand file öldürme (yüksek, kısa dürtme)
BK_PREP=D(READY,twist=-20,wrR=(-0.14,0.08,0.92),elR=(0.5,1,0.2),rk=(-0.3,0.2,0.93),rkFace=(0,1,0),lean=(0,0.05,0),root=(0,0.04,0.41),anR=(0.0,0.14,0.05),anL=(-0.16,0.0,0.05),look=(-0.3,0.7,0.6))   # adım hazırlıkta (kill ile aynı gerekçe)
BK_HIT=D(READY,twist=-5,wrR=(-0.28,0.34,0.98),elR=(0.4,0.9,0),rk=(-0.4,0.8,-0.4),rkFace=(0,0.4,1),lean=(0,0.08,0),root=(0,0.05,0.40),anR=(0.0,0.14,0.05),anL=(-0.16,0.0,0.05),look=(-0.2,0.9,0.4))
BK_PREP=bukuk(BK_PREP,140); BK_HIT=bukuk(BK_HIT,120); BK_FOL=bukuk(D(BK_HIT,wrR=(-0.26,0.30,0.88),rk=(-0.4,0.75,-0.55)),135)   # denetim #12
bh_kill=sample([(0,READY),(0.3,BK_PREP),(0.55,BK_HIT),(0.7,BK_FOL),(1,READY)],7,hit=2,hold=1,lead=1)
# --- backhand alçak (file önü / lift, 2026-10-21): sağ ayak sola-öne adım (backhand köşesine de raket ayağı gider),
#     başparmak tutuş, dirsek önce yükselip öne çıkar, raket başı arkadan ve elin ALTINDA gelir; gövde dik, sol kol
#     geride; takip kısa ve hedefe. Eskisinde raket başı önde süpürüyordu.
# 2. tur: backhand ön köşe = sağ bacakla sola-öne hamle (sol bacakla hamle acemi hatası), gövde ~45° döner, sırt
#     fileye yarı dönük; dirsek önce yükselip öne çıkar, raket başı elin ALTINDA ve arkada gelir; sol kol hamlenin tersine
#     geride uzanır; temas öndeki ayağın önünde alçakta; lift'te takip öne-yukarı, file vuruşunda çok kısa.
# 2026-10-26 (tüm beden turu, denetim §2.3): temasta ön diz 21° (neredeyse düz), arka diz 7° (kilitli): iki bacak da gergin
#     bir yarılma; arka ayak 0,2 poz geri kayıyordu, sağ ayak hazırlıkta havada asılıydı. Şimdi forehand file vuruşunun
#     aynası: hazırlıkta iki ayak yerde (sağ ayak ucu sola dönmeye başlar), sağ ayak ara karede sol-öne uzanır, topuktan
#     basar; sol (arka) ayak yerinde, ucu dışa, topuğu kalkık; ön diz ~70°, gövde ~45° dönük ve dik.
BL_PREP=D(READY,twist=-40,lean=(-0.02,0.07,-0.02),root=(-0.03,0.05,0.40),wrR=(-0.16,0.30,0.48),elR=(0.2,1,0.3),rk=(-0.35,-0.10,-0.93),rkFace=(-0.3,0.9,0.3),wrL=(0.10,-0.22,0.46),elL=(1,-0.6,-0.2),anR=(0.10,0.06,0.05),anL=(-0.13,0.04,0.05),fpL=-10,ftR=(-0.3,1,0),ftL=(-0.4,1,0),look=(-0.3,1,-0.3))
BL_HIT=D(BL_PREP,twist=-40,lean=(-0.03,0.08,-0.04),root=(-0.11,0.20,0.32),wrR=(-0.26,0.58,0.26),elR=(0.2,1,0.4),rk=(-0.35,0.55,-0.76),rkFace=(0,0.6,0.8),wrL=(0.18,-0.30,0.42),anR=(-0.18,0.38,0.05),fpR=14,ftR=(-0.5,0.9,0),anL=(-0.12,0.03,0.05),fpL=-25,ftL=(-0.6,0.8,0),knR=(-0.45,1,0),knL=(-0.5,1,0),look=(-0.2,1,-0.4))
BL_FOL=D(BL_HIT,wrR=(-0.22,0.56,0.46),rk=(-0.30,0.70,0.65),rkFace=(0,0.8,0.6),fpR=0,root=(-0.11,0.21,0.32))
BL_HIT=bukuk(BL_HIT,150)   # denetim #12 (BL_FOL ve BLU_HIT kendi bileğini yazar, etkilenmez)
bh_low=sample([(0,READY),(0.32,BL_PREP),(0.55,BL_HIT),(0.75,BL_FOL),(1,READY)],8,hit=2,hold=1,lead=1)
# --- backhand hamle: sol-öne derin, sağ (raket) bacak öne. 2026-10-26: arka ayak yerinde (eskiden 0,3 poz geri kayıyordu),
#     topuk basışı, takipte diz en derinde (~85°), sağ topuktan itişle havadan geri adım (BLU_REC).
BLU_PREP=D(BL_PREP,root=(-0.04,0.06,0.40))
BLU_HIT=D(BL_HIT,root=(-0.14,0.28,0.30),wrR=(-0.30,0.66,0.18),rk=(-0.30,0.90,-0.30),rkFace=(0,0.3,1),anR=(-0.24,0.47,0.05),fpR=18,anL=(-0.11,0.02,0.05),fpL=-30)
BLU_HIT=bukuk(BLU_HIT,150); BLU_FOL=bukuk(D(BLU_HIT,root=(-0.15,0.31,0.27),wrR=(-0.24,0.64,0.30),rk=(-0.3,0.9,0.4),fpR=0,anL=(-0.11,0.04,0.05)),150)   # denetim #12
BLU_REC=D(READY,twist=-20,lean=(-0.01,0.08,-0.01),root=(-0.06,0.14,0.39),wrR=(-0.04,0.40,0.46),wrL=(-0.10,-0.06,0.46),anR=(-0.06,0.24,0.12),fpR=-10,ftR=(-0.3,1,0),anL=(-0.13,0.05,0.05),fpL=-10,look=(-0.1,1,-0.1))
bh_lunge=sample([(0,READY),(0.35,BLU_PREP),(0.6,BLU_HIT),(0.78,BLU_FOL),(6/7,BLU_REC),(1,READY)],8,hit=2,hold=1,lead=1)
# ================= ayak işi (yasin: "adımlamalar badminton sporunun doğasına uygun olmalı") =================
# split step: rakip vururken küçük sıçrama, geniş ve alçak iniş
# split step (2026-10-26, tüm beden turu, denetim §1): eski klipte ayaklar havada 13 cm (kaynak 3–8 cm), iniş 0,70 m geniş
#     (1,8 × omuz; kaynak 1,3–1,5 ×) ve gövde 7 poz cm zıplıyordu. Şimdi: küçük patlayıcı hop (ayaklar ~4 cm), iniş ön tabanla
#     ~1,4 × omuz genişliğinde, dizler yüklenir, sonra hazır duruşa. 5 kare = anahtarlar (0 hazır, 1 havada, 2 iniş, 3 yüklü,
#     4 hazır); oyun klibi rakibin temasında başlatır, iniş ~0,15 s sonra (vurus_biyomekanigi.md §1: 0,15–0,2 s).
SP_HAVA=D(READY,root=(0,0,0.445),anR=(0.15,-0.01,ZEMIN+.03),anL=(-0.15,0.05,ZEMIN+.03),wrR=(0.24,0.24,0.56),wrL=(-0.22,0.20,0.52),knR=(0.2,1,0),knL=(-0.2,1,0))
SP_INIS=D(READY,root=(0,0.01,0.395),lean=(0,0.06,0),anR=(0.19,-0.01,ZEMIN),anL=(-0.19,0.05,ZEMIN),knR=(0.25,1,0),knL=(-0.25,1,0),ftR=(0.15,1,0),ftL=(-0.15,1,0))
# 7 kare (2026-10-26): 5 karede yüklü iniş → hazır duruş komşu karelerdeydi, ayaklar yerde 0,07 poz (~15 cm) kayarak
#     toplanıyordu. Şimdi 0 hazır, 1 kalkış, 2 tepe, 3 iniş (oyunun 0,3 sn'lik split'inde ~0,15 sn), 4 yüklü (ayaklar yükü alırken
#     ±0,165'e kayar: iniş itişinin ön tabanda dönüşü), 5 ara kare (küçük toplama adımı ~4 cm, _adim_kaldir), 6 hazır.
split=sample([(0,READY),(1/6,D(SP_HAVA,root=(0,0,0.44),anR=(0.14,-0.01,ZEMIN+.02),anL=(-0.14,0.05,ZEMIN+.02))),(2/6,SP_HAVA),(3/6,SP_INIS),(4/6,D(SP_INIS,root=(0,0.01,0.40),anR=(0.165,-0.01,ZEMIN),anL=(-0.165,0.05,ZEMIN))),(1,READY)],7)
# chassé (yana kayma, 2026-10-26): dongu() dörtnalı — öndeki ayak açılır ve basar, arkadaki 0,3 döngü sonra yanına gelir
#     (adım-kapa), ayaklar ÇAPRAZLANMAZ (en dar ara ~0,17 poz: modelin dar kalçasıyla ~18 cm, gal.py taraması), kısa uçuş
#     evresi. Döngü 0,44 poz ≈ 0,95 m (sade): 2,4 m/s'de 2,5 döngü/sn. Gövde dik ve sabite yakın (zıplama yok; kaynak: kalça yüksekliği neredeyse sabit),
#     dizler ~35–55°, raket önde yukarıda. Eski klipte kök 0,34'e iniyordu (diz 84–91°), öndeki diz 24°'ye açılıp kilitleniyordu.
def chasse(sgn):
    ust=D(READY,root=(0,0,0.41),lean=(sgn*0.02,0.08,0),wrR=(0.24,0.24,0.58),wrL=(-0.22,0.18,0.52))
    return dongu(8,(sgn,0),.44,.5,.3,.04,{'R':(0.15,-0.01),'L':(-0.15,0.05)},ust,root=.41,bob=.008,once='R' if sgn>0 else 'L',
                 kn=((0.25,1,0),(-0.25,1,0)),ft=((0.2,1,0),(-0.2,1,0)))
chasse_r=chasse(1); chasse_l=chasse(-1)
# öne adım (2026-10-26): kısa, alçak, hızlı adımlar (hamleye hazırlanır), raket önde yukarıda. dongu(): döngü 0,74 poz ≈ 1,3 m,
#     duty 0,55 (uçuşsuz). 0,62 poz ≈ 1,35 m (sade): 3 m/s'de 2,2 döngü/sn. Hızlı öne koşuda (≥ ~3 m/s) Unity 'run'a geçer.
_FWD_UST=D(READY,root=(0,0.02,0.41),lean=(0,0.11,-0.01),wrR=(0.24,0.26,0.60),wrL=(-0.20,0.14,0.48))
fwd=dongu(8,(0,1),.62,.55,.5,.05,{'R':(0.11,0.0),'L':(-0.11,0.02)},_FWD_UST,root=.41,bob=.012,kol=.06,tw=6,pel=5)
# arkaya adım (2026-10-26). Eski döngüler oyunda 0,21–0,35 m taşıyordu (oyuncu arkaya ort. 3,5 m/s koşuyor: %37+ kayma).
#     Sağ arka (forehand köşe): gövde sağa döner (göğüs 42°, leğen 30°), raket overhead hazırlığında, sağ ayak önden
#     arkaya basarak kayar; sol arka (backhand / round-the-head köşe): gövde sola (göğüs −62°, leğen −40°). İkisi de
#     dongu(): döngü 0,70 poz ≈ 1,55 m (sade), duty 0,45 (kısa uçuş), 3,5 m/s'de 2,3 döngü/sn. Ayak ve diz gövdenin baktığı yöne
#     dönük (diz ayak ucu doğrultusunda, valgus yok).
def back(sgn):
    tw=42 if sgn>0 else -62
    base=D(READY,twist=tw,pelvis=30 if sgn>0 else -40,lean=(0,-0.03,0),wrR=(0.22,-0.08,0.80) if sgn>0 else (-0.06,-0.06,0.76),elR=(1,-0.4,0.3) if sgn>0 else (-1,-0.3,0.6),rk=(0,-0.4,0.9) if sgn>0 else (-0.3,-0.5,-0.7),wrL=(-0.16,0.16,0.70) if sgn>0 else (0.18,0.0,0.55),look=(0.3*sgn,0.5,0.8))
    yon=((0.55,1,0),(0.35,1,0)) if sgn>0 else ((-0.35,1,0),(-0.55,1,0))
    return dongu(8,(0,-1),.70,.45,.5,.07,{'R':(0.12,-0.02),'L':(-0.12,0.04)},base,root=.40,bob=.015,kn=yon,ft=yon)
back_r=back(1); back_l=back(-1)
# hızlı yana koşu (2026-10-26, yeni): chassé ~2,8 m/s üstünde 3 döngü/sn'yi aşıyor (ayak pırpır ya da kayma). Gerçekte hızlı
#     yan harekette oyuncu leğeni gidiş yönüne çevirir, çapraz adımla koşar, göğüs ve baş fileye/topa yarı dönük kalır.
#     Leğen ±50°, göğüs ±25°; dongu() koşusu, ayaklar gidiş doğrultusunda önden-arkadan geçer. Döngü 0,72 poz ≈ 1,6 m (sade),
#     duty 0,42: 5,2 m/s'de 3,3 döngü/sn. Klipler listenin sonunda (önceki satırlar kaymasın).
def kros(sgn):
    ust=D(READY,root=(0,0,0.41),twist=25*sgn,pelvis=50*sgn,lean=(sgn*0.03,0.04,0),wrR=(0.26,0.18,0.60),wrL=(-0.20,0.10,0.48),look=(-0.3*sgn,1,0.1))
    yon=((sgn*1,0.75,0),(sgn*1,0.75,0))
    return dongu(8,(sgn,0),.72,.42,.5,.08,{'R':(0.0,-0.05),'L':(0.0,0.08)},ust,root=.41,bob=.018,once='R' if sgn>0 else 'L',kn=yon,ft=yon)
kros_r=kros(1); kros_l=kros(-1)
# sıçrayarak smaç: makas — raket bacağı geride, sıçrayışta öne savrulur, sol bacak geriye; iniş sol ayağa. Yükseklik oyunda eklenir (root sabit).
J_TAKE=D(S_PREP,root=(0,-0.03,0.34),anR=(0.16,-0.14,0.05),anL=(-0.10,0.14,0.05))   # ayaklar kupa pozundaki yerinde (eskiden sağ ayak 4 poz cm yerde kayıyordu)
# Havada raket düşüşü (denetim #6): kalkıştan sonra, makas başlamadan; bacaklar toplanmış, kol S_DROP'ta.
J_DROP=D(S_DROP,root=(0,-0.01,0.42),anR=(0.15,-0.10,0.14),anL=(-0.11,0.06,0.15),knR=(0,1,0),knL=(0,1,0))
# 2026-10-26 (tüm beden turu, denetim §2.6): sol diz ipucu (0,−1,0) dizi GERİYE büküyordu (diz yönü −178°); makas açıklığı
#     0,44 poz (~1 m). Şimdi iki diz öne, makas ~0,32 poz (~0,7 m): sağ bacak öne savrulur, sol geride ve bükük (iniş ona).
J_AIR=D(S_HIT,root=(0,0.0,0.42),anR=(0.09,0.16,0.16),anL=(-0.09,-0.16,0.20),fpR=0,knR=(0,1,0),knL=(0,1,0))
J_LAND=D(S_FOL,root=(0,0.04,0.385),anR=(0.14,0.16,0.10),anL=(-0.14,-0.10,0.05),ftL=(-0.7,1,0))   # önce sol ayak dışa dönük iner (§3)
# 14 kare, smaç ızgarası: 2 kupa, 4 çömelme/kalkış, 6 havada düşüş, 9 temas, 11 iniş. Bekleme karesi kupa (2): çömelme
#     ileri hareketin parçası, oyun kalkışı temastan ~250-300 ms önce başlatır.
jump=sample([(0,READY),(OV(2),S_PREP),(OV(4),J_TAKE),(OV(6),J_DROP),(OV(9),J_AIR),(OV(11),J_LAND),(1,READY)],14,hit=4,hold=1,drop=3,lead=1)
# --- yorgun (2026-10-23, Ar-Ge "rakibin nefesi"; yasin: "rakibi koşturup endurance'ı düşürmeliyiz, onu da görelim").
#     Sayı arasında nefesi bitmiş oyuncu: dizler bükük, gövde öne eğik, iki el dizlerin hemen üstünde, raket sağ elden
#     aşağı sarkar, baş aşağıda. Omuzlar nefesle iner kalkar (döngü, 10 kare). Klip listenin SONUNA eklenir ki önceki
#     klip satırları kaymasın ve yalnız bu klip render edilip var olan sheet'e eklenebilsin.
# 2026-10-26 (fizik §4.3): raket aşağı sarkıyordu (rk z −0,95), kafası zemine ~29 cm giriyordu; şimdi dizin önünde yatay, öne-içe
YG0=D(READY,root=(0,0.02,0.33),lean=(0,0.13,-0.05),twist=0,wrR=(0.13,0.16,0.25),elR=(1,-0.3,0.4),rk=(-0.35,0.93,-0.10),rkFace=(0,0,1),
      wrL=(-0.13,0.16,0.25),elL=(-1,-0.3,0.4),anR=(0.13,0.0,0.05),anL=(-0.13,0.0,0.05),knR=(0,1,0),knL=(0,1,0),look=(0,1,-0.55))
YG1=D(YG0,root=(0,0.02,0.345),lean=(0,0.11,-0.03),wrR=(0.13,0.15,0.26),wrL=(-0.13,0.15,0.26),look=(0,1,-0.4))
yorgun=sample([(0,YG0),(0.45,YG1),(1,YG0)],10,loop=True)
CLIPS=dict(idle=idle,run=run,smash=smash,clear=clear,drive=drive,kill=kill,low=low,lunge=lunge,serve=serve,serve_gec=serve_gec,serve_bh=serve_bh,serve_flick=serve_flick,block=block,bh_over=bh_over,bh_drive=bh_drive,bh_kill=bh_kill,bh_low=bh_low,bh_lunge=bh_lunge,split=split,chasse_r=chasse_r,chasse_l=chasse_l,fwd=fwd,back_r=back_r,back_l=back_l,jump=jump,yorgun=yorgun,
    drop=drop,net=net,   # drop/net 2026-10-24 sona eklendi: önceki klip satırları kaymasın
    kros_r=kros_r,kros_l=kros_l)   # 2026-10-26: hızlı yana çapraz koşu (Unity); HTML sprite'ı donuk, satırlar kaymaz
KEYS=dict(ov=[S_PREP,S_DROP,S_HIT,S_FOL,C_HIT,DP_HIT],sv=[SB0,SB1,SB2,SB3],fl=[SF2,SF3],sh=[SH0,SH1,SH2,SH3],dr=[DR_PREP,DR_HIT,DR_FOL],lo=[L_PREP,L_HIT,L_FOL],bdn=[BD_PREP,BD_HIT,BD_FOL],bln=[BL_PREP,BL_HIT,BL_FOL],X0=[],bo=[BO_PREP,BO_HIT,BO_FOL],bd=[BD_PREP,BD_HIT,BD_FOL],bk=[BK_PREP,BK_HIT],bl=[BL_PREP,BL_HIT,BL_FOL],blu=[BLU_PREP,BLU_HIT],split=[split[2],split[4]],chr=[chasse_r[2],chasse_r[4]],chl=[chasse_l[2],chasse_l[4]],fwd=[fwd[1],fwd[4]],bkr=[back_r[2],back_r[4]],bkl=[back_l[2],back_l[4]],jump=[J_TAKE,J_AIR,J_LAND],yorgun=[YG0,YG1])

# ---------- sahne ----------
bpy.ops.object.light_add(type='SUN',location=(1,-2,4)); sun=bpy.context.object; sun.data.energy=3.2; sun.rotation_euler=(math.radians(40),0,math.radians(20))
bpy.ops.object.light_add(type='AREA',location=(-2,-2,2)); fill=bpy.context.object; fill.data.energy=120; fill.data.size=3; fill.rotation_euler=(math.radians(60),0,math.radians(-45))
sc.world=bpy.data.worlds.new('w'); sc.world.use_nodes=True; sc.world.node_tree.nodes['Background'].inputs[0].default_value=(0.5,0.55,0.55,1); sc.world.node_tree.nodes['Background'].inputs[1].default_value=0.6
sc.render.engine='CYCLES'; sc.cycles.samples=24; sc.cycles.use_denoising=True; sc.render.film_transparent=True; sc.view_settings.view_transform='Standard'
try: sc.cycles.device='CPU'
except: pass
CW_,CH_=192,256
if MODE!='stil': CW_,CH_=CW_*int(os.environ.get('BS_OLCEK','1')),CH_*int(os.environ.get('BS_OLCEK','1'))   # 2× hücre (yasin 2026-10-23 "doku kalitesi düşüktü"): oyunda büyütülüp bulanıklaşmasın
sc.render.resolution_x=CW_; sc.render.resolution_y=CH_; sc.render.resolution_percentage=100
bpy.ops.object.camera_add(); cam=bpy.context.object; sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=2.4  # büyük kenar (yükseklik) = 2.4 birim → ~107 px/birim; hamlede raket kadraja sığsın
VIEWS={'me':(0,-1,1.35),'op':(0,1,1.35)}  # arkadan (ben) ve önden (rakip), ~53° yukarıdan
def setcam(v):
    d=Vector(VIEWS[v]).normalized(); cam.location=Vector((0,0,0.5))+d*4; cam.rotation_mode='XYZ'; el=math.atan2(d.z,math.hypot(d.x,d.y)); cam.rotation_euler=(math.pi/2-el,0,math.atan2(d.y,d.x)+math.pi/2)  # baş hep yukarıda (track_quat'ta roll kayıyordu)
from bpy_extras.object_utils import world_to_camera_view as w2c
def anchor():
    bpy.context.view_layer.update()   # setcam sonrası matris güncellenmeden okunursa eski kamerayla izdüşer (hakem görünümünde 128 çıkıyordu)
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
# ---------- hakemler (yasin 2026-10-22: "hakemler daha gerçekçi olmalı, benim sporcularım gibi, sayıda hareket etmeli
# sadece"). Aynı figür, hakem kıyafetiyle. Araştırma: arastirma/hakemler_sayida.md. Baş hakem (yüksek sandalye, file
# hizası, kortun solunda) her sayıda skoru tablete girer; servis el değiştirince ("Service over") o tarafı açık elle,
# o taraftaki koluyla gösterir. Servis hakemi (alçak sandalye, karşı direkte) servisi atacak oyuncuya bakar; servis el
# değiştirince başı ve gövdesi yeni servisçiye döner. Ralli sırasında ikisi de kıpırdamaz.
HK0=dict(READY,hakem=True,gri=True,root=(0,0,0.40),lean=(0,-0.01,0),twist=0,anR=(0.09,0.21,0.20),anL=(-0.09,0.21,0.20),knR=(0,1,1),knL=(0,1,1),ftR=(0,1,0),ftL=(0,1,0),
     wrR=(0.10,0.24,0.47),elR=(1,-0.4,-0.7),wrL=(-0.10,0.24,0.47),elL=(-1,-0.4,-0.7),tablet=(0,0.28,0.45),tabletEgim=35,look=(0,1,-0.05),shuttle=False)
HK_YAZ=D(HK0,wrR=(0.03,0.29,0.49),look=(0,1,-0.55),head=(0,0.01,-0.01))         # başını eğer, sağ işaret parmağıyla tablete dokunur
HK_SOL=D(HK0,wrL=(-0.46,0.16,0.62),elL=(-0.3,-0.2,-1),look=(-0.55,1,0.0),twist=-6)  # sol kol açık elle sol tarafı (rakibin yarısı) gösterir
HK_SAG=D(HK0,wrR=(0.46,0.16,0.62),elR=(0.3,-0.2,-1),look=(0.55,1,0.0),twist=6)
SJ0=dict(READY,hakem=True,root=(0,0,0.27),lean=(0,0.03,0),twist=0,anR=(0.10,0.21,0.04),anL=(-0.10,0.21,0.04),knR=(0,1,1),knL=(0,1,1),ftR=(0,1,0),ftL=(0,1,0),
     wrR=(0.11,0.17,0.27),elR=(1,-0.3,-0.4),wrL=(-0.11,0.17,0.27),elL=(-1,-0.3,-0.4),look=(0,1,-0.2),shuttle=False)
SJ_SAG=D(SJ0,look=(0.75,1,-0.25),twist=14,head=(0.01,0,0)); SJ_SOL=D(SJ0,look=(-0.75,1,-0.25),twist=-14,head=(-0.01,0,0))
HCLIPS={'hk':dict(hk_dur=[HK0],hk_yaz=sample([(0,HK0),(0.3,HK_YAZ),(0.7,HK_YAZ),(1,HK0)],8),
                  hk_sol=sample([(0,HK0),(0.25,HK_SOL),(0.75,HK_SOL),(1,HK0)],12),hk_sag=sample([(0,HK0),(0.25,HK_SAG),(0.75,HK_SAG),(1,HK0)],12)),
        'sj':dict(sj_sag=[SJ_SAG],sj_sol=[SJ_SOL],sj_saga=sample([(0,SJ_SOL),(1,SJ_SAG)],7),sj_sola=sample([(0,SJ_SAG),(1,SJ_SOL)],7))}
# Oyun kamerası benim arkamda. Oyunun (x sağ, y ekranda aşağı, z yukarı) eksenleri sol-elli, Blender sağ-elli: bu
# yüzden baş hakem (kortun solunda korta bakar) kameraya SAĞ yanını, servis hakemi SOL yanını gösterir. Benim yarım
# baş hakemin sağı, servis hakeminin solu (ilk denemede tersi alınmıştı: hakem tableti dışarı bakıyordu).
# Yükseklik oyuncu görünümüyle aynı (~53°).
VIEWS.update({'hk':(1,0,1.35),'sj':(-1,0,1.35)})
if MODE=='hakem':
    hm={'cell':[CW_,CH_],'pxPerUnit':CH_/2.4,'clips':{},'anchor':{}}
    for v,cl in HCLIPS.items():
        setcam(v); hm['anchor'][v]=anchor()
        for name,frames in cl.items():
            hm['clips'][name]={'n':len(frames),'v':v}
            for fi,p in enumerate(frames): build(p); set_mats(None); render(f'{OUT}/h_{name}_{fi}.png')
    json.dump(hm,open(f'{OUT}/hmeta.json','w')); print('HAKEM DONE'); sys.exit(0)
# Ekipman ekranında 360° görünüm (2026-10-23, yasin: "avatarımı 360 görmek isterim, raketimi görmek isterim"). Hazır
# duruş, 24 açı (15°), ~32° yukarıdan (göz hizasında bacaklar figürü uzatıyordu), daha büyük hücre. Aynı tint maskeleri: oyunda seçilen forma,
# raket, bant ve bileklik renkleri burada da boyanır. Raket kafasının her açıdaki ekran yeri meta'ya yazılır: "raketi
# yakından gör" ayrı render istemez, o noktaya 2× kırpar.
if MODE=='tur':
    TWD,THD=480,640; sc.render.resolution_x=TWD; sc.render.resolution_y=THD; cam.data.ortho_scale=2.3; N=int(os.environ.get('BS_TUR_N','24'))   # TH adı uyluk boyu, kullanma; 480×640: "raketi yakından gör" 2× büyütmede bulanıklaşmasın
    TUR=D(READY,twist=0,rk=(0.18,0.30,0.94),wrR=(0.24,0.20,0.56),look=(0,1,0.05))
    tm={'cell':[TWD,THD],'n':N,'raket':[],'cols':8}
    build(TUR)
    for i in range(N):
        a_=2*math.pi*i/N; d=Vector((math.sin(a_),-math.cos(a_),0.62)).normalized()   # i=0: önden değil arkadan değil, oyuncuya karşıdan (yüz kameraya)
        d=Vector((-d.x,-d.y,d.z)); cam.location=Vector((0,0,0.5))+d*4; el=math.atan2(d.z,math.hypot(d.x,d.y)); cam.rotation_euler=(math.pi/2-el,0,math.atan2(d.y,d.x)+math.pi/2)
        bpy.context.view_layer.update()
        rkp=OY.kafa if (GERCEK and OY) else [o for o in objs if o.name.startswith('raket_kafa')][0].matrix_world.translation; q=w2c(sc,cam,rkp); tm['raket'].append([round(q.x*TWD,1),round((1-q.y)*THD,1)])
        hide_bands(True); set_mats(None); render(f'{OUT}/t_{i}.png')
        hide_bands(False); sc.cycles.samples=4; sc.cycles.use_denoising=False
        for tag in (() if os.environ.get('BS_ONIZLEME') else ('forma','cerceve','bant','bilek','tel','tel2','grip')): set_mats(tag); render(f'{OUT}/tm_{i}_{tag}.png')   # BS_ONIZLEME: yalnız renk geçişi   # tel/tel2/grip: raket ekranındaki renk seçimi oyuncuda da görünsün
        set_mats(None); sc.cycles.samples=24; sc.cycles.use_denoising=True
    json.dump(tm,open(f'{OUT}/tmeta.json','w')); print('TUR DONE'); sys.exit(0)
if MODE=='raket':   # raket 360° (yasin 2026-10-23: "rakete dokununca raketin 360 ekranı, teller gerçekçi, grip ve kordaj rengi")
    import insan
    RW,RH=300,640; sc.render.resolution_x=RW; sc.render.resolution_y=RH; N=24
    for o in list(objs): bpy.data.objects.remove(o)
    objs.clear(); M=insan.malzemeler(FORMA,SORT,BEYAZ,BANT,CERCEVE,TEL,SORT); S_=1.0
    nes,kc=insan.raket(Vector((0,0,0)),Vector((0,0,1)),Vector((0,-1,0)),S_,M,tel_kalin=.0008); objs[:]=nes   # gerçek boy (67,5 cm), yüz kameraya; tel yakın ekranda seçilsin diye biraz kalın
    zc=.228; cam.data.ortho_scale=.74   # sap ucu -0,11, kafa tepesi 0,565
    rm={'cell':[RW,RH],'n':N,'cols':12}
    for i in range(N):
        a_=2*math.pi*i/N; d=Vector((math.sin(a_),-math.cos(a_),0.18)).normalized(); cam.location=Vector((0,0,zc))+d*3
        el=math.atan2(d.z,math.hypot(d.x,d.y)); cam.rotation_euler=(math.pi/2-el,0,math.atan2(d.y,d.x)+math.pi/2); bpy.context.view_layer.update()
        set_mats(None); sc.cycles.samples=32; sc.cycles.use_denoising=True; render(f'{OUT}/r_{i}.png')
        sc.cycles.samples=4; sc.cycles.use_denoising=False
        for tag in ('tel','tel2','grip','cerceve'): set_mats(tag); render(f'{OUT}/rm_{i}_{tag}.png')
        set_mats(None)
    json.dump(rm,open(f'{OUT}/rmeta.json','w')); print('RAKET DONE'); sys.exit(0)
if MODE=='stil':   # arcade tarz önizlemesi: aynı pozlar, 2× hücre (doku), arkadan/önden + yakın ön görünüm
    FORMA.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(0.06,0.42,0.85,1); BANT_=BANT.node_tree.nodes.get('Principled BSDF')   # önizlemede oyunun boyadığı renkler pişer
    if BANT_: BANT_.inputs['Base Color'].default_value=(0.85,0.12,0.18,1)
    R2=int(os.environ.get('BS_OLCEK','2')); sc.render.resolution_x=CW_*R2; sc.render.resolution_y=CH_*R2; sc.cycles.samples=32
    pz=dict(hazir=READY,smac=CLIPS['smash'][len(CLIPS['smash'])//2],uzan=CLIPS['lunge'][len(CLIPS['lunge'])//2],kos=CLIPS['run'][2])
    for ad,p in pz.items():
        build(p); hide_bands(False); set_mats(None)
        for v in ('me','op'): setcam(v); render(f'{OUT}/s_{ad}_{v}.png')
    build(READY); hide_bands(False); set_mats(None); sc.render.resolution_x=480; sc.render.resolution_y=640; cam.data.ortho_scale=1.35
    d=Vector((0.55,0.75,0.35)).normalized(); cam.location=Vector((0,0,0.5))+d*4; el=math.atan2(d.z,math.hypot(d.x,d.y)); cam.rotation_euler=(math.pi/2-el,0,math.atan2(d.y,d.x)+math.pi/2)
    render(f'{OUT}/s_yakin.png')
    pb=OY.ao.pose.bones; mw=OY.ao.matrix_world
    for ad_,bn,osc,yon in (('yuz','head',.28,(0.25,1,0.12)),('elR','wrist.R',.24,(0.6,0.8,0.3)),('elL','wrist.L',.24,(-0.5,0.8,0.3))):
        bn={'head':OY.R['head'],'wrist.R':OY.R['el'].format(S='R'),'wrist.L':OY.R['el'].format(S='L')}[bn] if MODEL in ('aday','sade','oyuncak','tuycuk') else bn; bn=bn if bn in pb else bn.lower(); osc=osc*(2.2 if MODEL=='oyuncak_kaykit' and bn=='head' else 1.6 if MODEL=='oyuncak_kaykit' else 1)   # oyuncak_kaykit: küçük harf kemik, iri baş/el; yeni oyuncak UE adlı, gerçek oranlı
        c=mw@pb[bn].head if bn!='head' else mw@((pb[bn].head+pb[bn].tail)/2); d=Vector(yon).normalized(); cam.location=c+d*3; cam.data.ortho_scale=osc
        el=math.atan2(d.z,math.hypot(d.x,d.y)); cam.rotation_euler=(math.pi/2-el,0,math.atan2(d.y,d.x)+math.pi/2); sc.render.resolution_x=sc.render.resolution_y=480; render(f'{OUT}/s_{ad_}.png')
    print('STIL DONE'); sys.exit(0)
if MODE=='keys':
    sc.cycles.samples=12; setcam('me'); i=0
    for name,ps in KEYS.items():
        for j,p in enumerate(ps):
            build(p); hide_bands(False); render(f'{OUT}/key_{name}{j}_me.png'); setcam('op'); render(f'{OUT}/key_{name}{j}_op.png'); setcam('me'); i+=1
    print('KEYS DONE',i)
else:
    row=0; ONLY=MODE[5:].split(',') if MODE.startswith('only:') else None   # 'only:a,b' = yalnız bu klipler render edilir, meta tümü
    COLS=10   # 10'dan uzun klip alt alta birden çok sıra kaplar (sheet.py ile aynı)
    for name,frames in CLIPS.items():
        meta['clips'][name]={'n':len(frames),'row':row,**{k:getattr(frames,k) for k in ('hit','hold','drop') if getattr(frames,k,None) is not None}}   # oyun temas/bekleme karesini buradan okuyabilir
        row+=(len(frames)+COLS-1)//COLS-1
        if ONLY and name not in ONLY:
            for v in meta['views']: setcam(v); meta.setdefault('anchor',{})[v]=anchor()
            row+=1; continue
        for v in meta['views']:   # hk/sj hakem modunun; oyuncu klipleri yalnız me/op
            setcam(v); meta.setdefault('anchor',{})[v]=anchor()
            for fi,p in enumerate(frames):
                build(p); hide_bands(True); set_mats(None); render(f'{OUT}/f_{name}_{v}_{fi}.png')
                hide_bands(False); sc.cycles.samples=4; sc.cycles.use_denoising=False
                for tag in ('forma','cerceve','bant','bilek'): set_mats(tag); render(f'{OUT}/m_{name}_{v}_{fi}_{tag}.png')
                set_mats(None); sc.cycles.samples=24; sc.cycles.use_denoising=True
        row+=1
    json.dump(meta,open(f'{OUT}/meta.json','w'))
    print('DONE')
