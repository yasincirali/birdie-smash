# Gerçekçi oyuncu gövdesi (yasin 2026-10-23: "insan anatomisine ve maddenin fiziksel durumuna uygun olacak kadar gerçekçi").
# Kaynak: MakeHuman 1.x taban ağı + makro hedefler + varsayılan iskelet/ağırlıklar (CC0, makehumancommunity/makehuman
# LICENSE.ASSETS.md). Kod/AGPL kısmı kullanılmaz; yalnız veri dosyaları okunur. Kurulum: tool/bulut_kurulum.sh veya
#   git clone --depth 1 --filter=blob:none --sparse https://github.com/makehumancommunity/makehuman $MH && git -C $MH sparse-checkout set makehuman/data/3dobjs makehuman/data/targets/macrodetails makehuman/data/rigs
# Ağ birimi desimetre, Y yukarı; burada metreye ve Z yukarıya çevrilir.
import bpy, json, os, math
from mathutils import Vector, Matrix
MH=os.environ.get('MH_DATA',os.path.expanduser('~/AnimationLibrary/makehuman/mh/makehuman/data'))

def _obj():
    V=[]; F={}; g=None
    for l in open(f'{MH}/3dobjs/base.obj'):
        if l.startswith('v '): x,y,z=map(float,l.split()[1:4]); V.append([x,y,z])
        elif l.startswith('g '): g=l.split()[1]
        elif l.startswith('f '): F.setdefault(g,[]).append([int(t.split('/')[0])-1 for t in l.split()[1:]])
    return V,F

def _hedef(V,ad,w):
    if w<=0: return
    for l in open(f'{MH}/targets/macrodetails/{ad}.target'):
        if not l.strip() or l[0]=='#': continue
        i,x,y,z=l.split(); i=int(i); V[i][0]+=w*float(x); V[i][1]+=w*float(y); V[i][2]+=w*float(z)

def govde(boy=1.0, kas=.6, kilo=.45):
    """Genç, atletik erkek. kas/kilo 0..1 (0.5 = ortalama). boy: metre cinsinden ayakta boy."""
    V,F=_obj()
    def ikili(v):  # MakeHuman makro karışımı: min/average/max arası doğrusal
        return {'min':max(0,1-2*v),'average':1-abs(2*v-1),'max':max(0,2*v-1)}
    K=ikili(kas); W=ikili(kilo)
    for km,kw in K.items():
        for wm,ww in W.items():
            _hedef(V,f'universal-male-young-{km}muscle-{wm}weight',kw*ww)
    for e in ('caucasian','african','asian'): _hedef(V,f'{e}-male-young',1/3)
    for km,kw in K.items():
        for wm,ww in W.items():
            _hedef(V,f'proportions/male-young-{km}muscle-{wm}weight-idealproportions',kw*ww*.6)
    # Y yukarı dm → Z yukarı m; ayak tabanı z=0, boy ölçeklenir
    P=[Vector((-x,z,y)) for x,y,z in V]   # yüz +Y'ye, sağ el +X'e (karakter.py ile aynı eksenler)
    bi=sorted({i for f in F['body'] for i in f}); zmin=min(P[i].z for i in bi); zmax=max(P[i].z for i in bi); s=boy/(zmax-zmin)
    P=[Vector((p.x*s,p.y*s,(p.z-zmin)*s)) for p in P]
    return P,F

def eklemler(P):
    d=json.load(open(f'{MH}/rigs/default.mhskel')); J={}
    for k,ix in d['joints'].items(): J[k]=sum((P[i] for i in ix),Vector())/len(ix)
    return d,J

def kur(boy=1.0,kas=.6,kilo=.45,ad='insan'):
    """Ağ + iskelet + ağırlıklar. Döner: (armatür, {parça: nesne}). Parçalar ayrı nesne ki maske etiketi verilebilsin."""
    P,F=govde(boy,kas,kilo); d,J=eklemler(P)
    W=json.load(open(f'{MH}/rigs/default_weights.mhw'))['weights']
    arm=bpy.data.armatures.new(ad+'_isk'); ao=bpy.data.objects.new(ad+'_isk',arm); bpy.context.scene.collection.objects.link(ao)
    bpy.context.view_layer.objects.active=ao; bpy.ops.object.mode_set(mode='EDIT')
    for k,b in d['bones'].items():
        eb=arm.edit_bones.new(k); eb.head=J[b['head']]; eb.tail=J[b['tail']]
        if (eb.tail-eb.head).length<1e-5: eb.tail=eb.head+Vector((0,0,1e-3))
    for k,b in d['bones'].items():
        if b['parent']: arm.edit_bones[k].parent=arm.edit_bones[b['parent']]; arm.edit_bones[k].use_connect=False
    bpy.ops.object.mode_set(mode='OBJECT')
    parcalar={}
    def ag(grup_listesi,isim):
        fs=[f for g in grup_listesi for f in F.get(g,[])]
        ix=sorted({i for f in fs for i in f}); yeni={o:n for n,o in enumerate(ix)}
        me=bpy.data.meshes.new(isim); me.from_pydata([P[i] for i in ix],[],[[yeni[i] for i in f] for f in fs]); me.update()
        o=bpy.data.objects.new(isim,me); bpy.context.scene.collection.objects.link(o)
        for bn,lst in W.items():
            vg=None
            for i,w in lst:
                if i in yeni:
                    vg=vg or o.vertex_groups.new(name=bn); vg.add([yeni[i]],w,'REPLACE')
        md=o.modifiers.new('isk','ARMATURE'); md.object=ao; o.parent=ao
        for p in me.polygons: p.use_smooth=True
        parcalar[isim]=o; return o,ix
    ag(['body'],ad+'_ten'); ag(['helper-tights'],ad+'_tayt'); ag(['helper-hair'],ad+'_sac')
    ag(['helper-l-eye'],ad+'_gozL'); ag(['helper-r-eye'],ad+'_gozR')
    return ao,parcalar,J

# ---------- poz: karakter.py'nin eklem hedeflerini bu iskelete giydirme ----------
# Pozlar oyuncak figür ölçüsünde yazıldı (UA+FA=.36, TH+SH=.38). Biçimleri korumak için hedefler omuza/kalçaya
# göre bu gövdenin uzuv boyuna ölçeklenir; dirsek/diz aynı iki-kemik IK ve aynı bükülme ipucuyla çözülür.
TOY=dict(UA=.19,FA=.17,TH=.20,SH=.18,SHW=.13,HIPW=.085)
def _ik(A,B,l1,l2,hint):
    d=B-A; L=d.length; mx=l1+l2-1e-3
    if L>mx: B=A+d.normalized()*mx; d=B-A; L=mx
    a=(l1*l1-l2*l2+L*L)/(2*L); h=math.sqrt(max(0,l1*l1-a*a)); u=d.normalized()
    hv=Vector(hint); hv=hv-u*hv.dot(u)
    if hv.length<1e-4: hv=Vector((0,0,-1))-u*Vector((0,0,-1)).dot(u)
    return A+u*a+hv.normalized()*h, B

class Iskelet:
    def __init__(s,ao,J):
        s.ao=ao; s.J=J; B=lambda k:J[k+'____head']
        s.hipR,s.hipL=B('upperleg01.R'),B('upperleg01.L'); s.kneeR=B('lowerleg01.R'); s.ankR=B('foot.R')
        s.shR,s.shL=B('upperarm01.R'),B('upperarm01.L'); s.elR=B('lowerarm01.R'); s.wrR=B('wrist.R')
        s.TH=(s.kneeR-s.hipR).length; s.SH=(s.ankR-s.kneeR).length; s.UA=(s.elR-s.shR).length; s.FA=(s.wrR-s.elR).length
        s.root=(s.hipR+s.hipL)/2+Vector((0,0,.04))
        s.neck=B('neck01'); s.headc=(J['head____head']+J['head____tail'])/2
    def hedef(s,p):
        """p: karakter.py poz sözlüğü → bu gövdenin dünya eklem konumları"""
        k=(s.TH+s.SH)/(TOY['TH']+TOY['SH'])   # bacak oranı: tüm poz bu kadar büyür (boy oyuncakla aynı kalsın diye ~1.3)
        T={}; root=Vector(p['root'])*k; lean=Vector(p.get('lean',(0,0,0)))*k; tw=-math.radians(p.get('twist',0))
        chestT=root+(Vector((0,0,.22))+lean/k)*k; up=(chestT-root).normalized()
        R=Vector((0,0,1)).rotation_difference(up).to_matrix()@Matrix.Rotation(tw,3,'Z')
        T['root']=root; T['Rpel']=Matrix.Rotation(tw*.35,3,'Z'); T['Rgov']=R
        for sd,shm in ((1,s.shR),(-1,s.shL)):
            T['sh'+('R' if sd>0 else 'L')]=root+R@(shm-s.root)
        T['neck']=root+R@(s.neck-s.root); T['head']=T['neck']+(R@(s.headc-s.neck))+Vector(p.get('head',(0,0,0)))
        Rz=Matrix.Rotation(tw,3,'Z')
        for sd,K in ((1,'R'),(-1,'L')):
            shT=chestT+Rz@Vector((sd*TOY['SHW'],0,.04))*k; w=Vector(p['wr'+K])*k; sh=T['sh'+K]
            wr=sh+(w-shT)*((s.UA+s.FA)/((TOY['UA']+TOY['FA'])*k))
            el,wr=_ik(sh,wr,s.UA,s.FA,Vector(p.get('el'+K,(sd,-.3,-.6)))); T['el'+K]=el; T['wr'+K]=wr
            hpT=root+Vector((sd*TOY['HIPW'],0,-.04))*k; hp=root+T['Rpel']@((s.hipR if sd>0 else s.hipL)-s.root)
            a=Vector(p['an'+K])*k; an=hp+(a-hpT)
            kn,an=_ik(hp,an,s.TH,s.SH,Vector(p.get('kn'+K,(0,1,0)))); T['hp'+K]=hp; T['kn'+K]=kn; T['an'+K]=an
            fd=Vector(p.get('ft'+K,(0,1,0))); fd.z=0; T['ft'+K]=fd.normalized()
        T['k']=k; T['look']=Vector(p.get('look',(0,1,0))).normalized(); T['rk']=Vector(p.get('rk',(0,.6,.8))).normalized()
        T['rkFace']=Vector(p.get('rkFace',(1,0,0)))
        return T
    def uygula(s,T):
        ao=s.ao; pb=ao.pose.bones; L=bpy.context.view_layer
        for b in pb: b.matrix_basis=Matrix()
        L.update()
        def set_rot(n,Rw,loc=None):   # dünya dönüşü Rw (rest yönüne göre), baş noktası loc
            b=pb[n]; rest=ao.data.bones[n].matrix_local
            M=(Rw.to_4x4()@Matrix(rest.to_3x3()).to_4x4()); M.translation=loc if loc is not None else b.matrix.translation
            b.matrix=M; L.update()
        def aim(n,hedef):   # en küçük dönüşle kemiği hedefe çevir (burulma ebeveynden sürer)
            b=pb[n]; M=b.matrix.copy(); y=(M.to_3x3()@Vector((0,1,0))).normalized(); d=(hedef-M.translation).normalized()
            q=y.rotation_difference(d); N=q.to_matrix().to_4x4()@Matrix(M.to_3x3()).to_4x4(); N.translation=M.translation; b.matrix=N; L.update()
        # kök: kalça merkezi hedefe, leğen döner
        set_rot('root',T['Rpel'],T['root']-(T['Rpel']@(s.root-ao.data.bones['root'].head_local)))
        for i,n in enumerate(('spine05','spine04','spine03','spine02','spine01')):
            t=(i+1)/5; q=T['Rpel'].to_quaternion().slerp(T['Rgov'].to_quaternion(),t); set_rot(n,q.to_matrix())
        for k in 'RL':
            aim(f'shoulder01.{k}',T['sh'+k])
            for n in ('upperarm01','upperarm02'): aim(f'{n}.{k}',T['el'+k])
            for n in ('lowerarm01','lowerarm02'): aim(f'{n}.{k}',T['wr'+k])
            fa=(T['wr'+k]-T['el'+k]).normalized()
            if k=='R': fa=(fa*.6+T['rk']*.4).normalized()   # bilek rakete doğru hafif kırılır
            aim(f'wrist.{k}',T['wr'+k]+fa)
            for n in ('upperleg01','upperleg02'): aim(f'{n}.{k}',T['kn'+k])
            for n in ('lowerleg01','lowerleg02'): aim(f'{n}.{k}',T['an'+k])
            b=pb[f'foot.{k}']; M=b.matrix.copy(); y=M.to_3x3()@Vector((0,1,0)); yh=Vector((y.x,y.y,0)).normalized(); f=T['ft'+k]
            ang=math.atan2(yh.x*f.y-yh.y*f.x, yh.dot(f)); N=Matrix.Rotation(ang,4,'Z')@Matrix(M.to_3x3()).to_4x4(); N.translation=M.translation; b.matrix=N; L.update()
            # parmaklar: raket elinde sap etrafında kapanır, boş elde yarı açık
            kap=1.35 if k=='R' else .32   # boş el: yarı kapalı parmak uzaktan halka gibi okunuyordu
            for f_ in range(2,6):
                for j in (1,2,3): pb[f'finger{f_}-{j}.{k}'].rotation_mode='XYZ'; pb[f'finger{f_}-{j}.{k}'].rotation_euler=(kap*(.9 if j==1 else 1),0,0)
            for j in (2,3): pb[f'finger1-{j}.{k}'].rotation_mode='XYZ'; pb[f'finger1-{j}.{k}'].rotation_euler=(kap*.5,0,0)
            L.update()
        for n in ('neck01','neck02','neck03'): aim(n,T['head'])
        b=pb['head']; M=b.matrix.copy(); f=(M.to_3x3()@Vector((0,0,1))); f=Vector((f.x,f.y,0)).normalized() if f.length else Vector((0,1,0))
        lk=T['look']; q=Vector((0,1,0)).rotation_difference(lk); set_rot('head',q.to_matrix()@Matrix.Rotation(0,3,'Z'))

# ---------- giysi, saç, göz ----------
# Giysiler gövdenin kendi yüzeyinden türetilir (MakeHuman 'helper-tights' kabuğu): kemik ağırlığına göre forma/şort/çorap
# bölgesi seçilir, normal yönünde birkaç mm şişirilir. Altında kalan ten yüzleri gizlenir ki eklem bükülünce ten forma
# içinden çıkmasın. Ayakkabı: ayak yüzeyinden şişirilmiş kabuk + düz taban (sneaker siluet).
def _agirlik(o):
    vg={g.index:g.name for g in o.vertex_groups}; A=[{} for _ in o.data.vertices]
    for v in o.data.vertices:
        for g in v.groups: A[v.index][vg[g.group]]=g.weight
    return A
def _pay(A,i,on):
    return sum(w for b,w in A[i].items() if b.startswith(on))
FORMA_B=('spine','clavicle','shoulder01','breast','upperarm01','neck01')
SORT_B=('root','pelvis','upperleg01')
AYAK_B=('foot','toe')
def _sec(o,A,kosul):
    return [p.index for p in o.data.polygons if kosul(sum(1 for i in p.vertices if True) and p)]
def _kabuk(kaynak,isim,yuzler,kalinlik,mat,etiket,duzle=0):
    me=kaynak.data; ix=sorted({i for f in yuzler for i in me.polygons[f].vertices}); yeni={o:n for n,o in enumerate(ix)}
    m2=bpy.data.meshes.new(isim); m2.from_pydata([me.vertices[i].co.copy() for i in ix],[],[[yeni[i] for i in me.polygons[f].vertices] for f in yuzler]); m2.update()
    o=bpy.data.objects.new(isim,m2); bpy.context.scene.collection.objects.link(o)
    for g in kaynak.vertex_groups: o.vertex_groups.new(name=g.name)
    for i in ix:
        for g in me.vertices[i].groups: o.vertex_groups[g.group].add([yeni[i]],g.weight,'REPLACE')
    for p in m2.polygons: p.use_smooth=True
    if duzle:   # ayakkabı: parmak girintileri kaybolsun (yumuşatma sonra şişirme)
        sm=o.modifiers.new('duzle','SMOOTH'); sm.factor=.9; sm.iterations=duzle
    d=o.modifiers.new('sis','DISPLACE'); d.direction='NORMAL'; d.strength=kalinlik; d.mid_level=0
    a=o.modifiers.new('isk','ARMATURE'); a.object=kaynak.modifiers['isk'].object; o.parent=kaynak.parent
    sm=o.modifiers.new('yumusat','SUBSURF'); sm.levels=sm.render_levels=1
    o.data.materials.append(mat); o['tag']=etiket; return o
def _yuz_boya(ten,V,gm,gd,A):
    """Yüz ifadesi (yasin 2026-10-23: "yüz tamamlanmamış, bir ifade olmalı"; mimik istemiyor). Köşe rengine boyamak ağın
    sıklığıyla sınırlıydı (kaş lekeli, ağız kırık çıkıyordu): köşelere yalnız yüz koordinatı yazılır, çizgiler gölgelendiricide
    piksel piksel hesaplanır (_yuz_dugum). Koordinat 'yuzuv': x=u yatay, y=v dikey, birim = iki göz merkezi arası
    (gözler u=±0,5, v=0); z=1 yüzün önü (göz merkezine göre kutu içinde ve öne bakan), değilse 0."""
    at=ten.data.attributes.new('yuzuv','FLOAT_VECTOR','POINT')
    for v in ten.data.vertices:
        # yüz bölgesi konumdan: kaş ve ağız köşeleri yüz kemiklerine (orbicularis, oris) ağırlıklı, 'head' ağırlığı orada düşük
        # olduğu için ağırlıkla seçilen maske kaşı ikiye bölüyor, ağzı hiç çizmiyordu
        m=1. if abs(v.co.x-gm.x)<1.3*gd and gm.z-1.7*gd<v.co.z<gm.z+1.0*gd and v.co.y>gm.y-.6*gd and v.normal.y>=.15 else 0.
        at.data[v.index].vector=((v.co.x-gm.x)/gd,(v.co.z-gm.z)/gd,m)
def _yuz_dugum(nt,taban):
    """'yuzuv' koordinatından kaş, üst kirpik, ağız, dudak ve yanak; taban rengin üstüne karıştırılır. İfade: odaklı, hafif
    gülümseyen (kaşın iç ucu alçak ve düz, ağız köşeleri yukarı). Döner: renk soketi."""
    N=nt.nodes; L=nt.links
    def m(op,a,b=None,c=None):
        n=N.new('ShaderNodeMath'); n.operation=op
        for i,x in enumerate((a,b,c)):
            if x is None: continue
            if isinstance(x,(int,float)): n.inputs[i].default_value=x
            else: L.new(x,n.inputs[i])
        return n.outputs[0]
    at=N.new('ShaderNodeAttribute'); at.attribute_type='GEOMETRY'; at.attribute_name='yuzuv'
    sp=N.new('ShaderNodeSeparateXYZ'); L.new(at.outputs['Vector'],sp.inputs[0]); u,z,on=sp.outputs[0],sp.outputs[1],sp.outputs[2]
    au=m('ABSOLUTE',u)
    def bant(x,c,yar,keskin=2.): return m('MINIMUM',1.,m('MAXIMUM',0.,m('MULTIPLY',m('SUBTRACT',1.,m('DIVIDE',m('ABSOLUTE',m('SUBTRACT',x,c)),yar)),keskin)))
    def kirp(x): return m('MINIMUM',1.,m('MAXIMUM',0.,x))
    def aralik(x,a0,a1,yum): return m('MULTIPLY',kirp(m('DIVIDE',m('SUBTRACT',x,a0-yum),yum)),kirp(m('DIVIDE',m('SUBTRACT',a1+yum,x),yum)))   # [a0,a1] içinde 1, dışında yum genişliğinde söner
    # kaş: iç uç u=.14'te alçak, dışa doğru hafif yay; dış uca doğru incelir
    t=m('DIVIDE',m('SUBTRACT',au,.14),.8); vc=m('ADD',.30,m('SUBTRACT',m('MULTIPLY',t,.26),m('MULTIPLY',m('MULTIPLY',t,t),.20)))
    yar=m('SUBTRACT',.12,m('MULTIPLY',t,.05)); kas=m('MULTIPLY',bant(z,vc,yar,2.2),aralik(au,.15,.92,.05))
    # üst kirpik: göz kapağı kenarı, dış ucu hafif kalkık ve kalın
    x=m('SUBTRACT',au,.5); vk=m('ADD',m('SUBTRACT',.10,m('MULTIPLY',m('MULTIPLY',x,x),.9)),m('MULTIPLY',m('MAXIMUM',x,0.),.16))
    kir=m('MULTIPLY',bant(z,vk,.032,2.2),aralik(x,-.27,.29,.04))
    # ağız: köşeler yukarı (gülümseme), uçlara doğru incelir
    mc=m('ADD',-1.23,m('MULTIPLY',m('MULTIPLY',u,u),.8)); ag=m('MULTIPLY',bant(z,mc,m('SUBTRACT',.06,m('MULTIPLY',au,.07)),2.),aralik(au,-1.,.38,.05))
    dud=m('MULTIPLY',m('MULTIPLY',bant(z,m('SUBTRACT',mc,.075),.06,1.4),aralik(au,-1.,.26,.06)),.45)   # alt dudak
    yan=m('MULTIPLY',m('MULTIPLY',bant(au,.6,.22,1.),bant(z,-.55,.22,1.)),.14)   # yanak
    renk=taban
    for f,c in ((yan,(.95,.50,.46)),(dud,(.72,.38,.36)),(kas,(.09,.06,.045)),(kir,(.05,.035,.035)),(ag,(.26,.09,.09))):
        mx=N.new('ShaderNodeMix'); mx.data_type='RGBA'; L.new(m('MULTIPLY',f,on),mx.inputs['Factor'])
        if isinstance(renk,tuple): mx.inputs[6].default_value=(*renk,1)
        else: L.new(renk,mx.inputs[6])
        mx.inputs[7].default_value=(*c,1); renk=mx.outputs[2]
    return renk
def giydir(pc,ad,M,J):
    """M: malzemeler. Giysiler ten ağının kendi yüzlerinden (tam ağırlıklı) türetilir; tayt yardımcısının ağırlıkları eksik."""
    ten=pc[ad+'_ten']; A=_agirlik(ten); V=ten.data.vertices
    def pay(p,bas): return sum(_pay(A,i,bas) for i in p.vertices)/len(p.vertices)
    P=ten.data.polygons; out={}
    kol_ust=('upperarm01','shoulder01','clavicle')
    forma=[p.index for p in P if pay(p,FORMA_B)>=.5 and pay(p,('neck01',))<.5]
    sort=[p.index for p in P if pay(p,SORT_B)>=.5 and pay(p,('upperleg01',))<.95 or pay(p,('root','pelvis'))>=.5]
    ayak=[p.index for p in P if pay(p,AYAK_B)>=.5]
    corap=[p.index for p in P if pay(p,('lowerleg02',))>=.5 and P[p.index].center.z<.085]
    out['forma']=_kabuk(ten,ad+'_forma',forma,.007,M['forma'],'forma')
    out['sort']=_kabuk(ten,ad+'_sort',sort,.010,M['sort'],'sort')
    out['corap']=_kabuk(ten,ad+'_corap',corap,.003,M['beyaz'],'beyaz')
    import bmesh
    for k in 'LR':   # ayakkabı: ayağın dışbükey zarfı (parmak girintisi yok, gerçek sneaker hacmi), taban düzlenir, ayak kemiğine bağlı
        pts=[V[i].co.copy() for f in ayak for i in P[f].vertices if (V[i].co.x>0)==(k=='R')]
        zt=min(p.z for p in pts); pts=[p for p in pts if p.z<zt+.05]
        bm=bmesh.new()
        for p_ in pts: bm.verts.new(p_)
        bmesh.ops.convex_hull(bm,input=bm.verts[:]); 
        for v in bm.verts:   # biraz şişir, tabanı düzle (taban 4 mm yerin altına inmesin)
            c=v.co; v.co=Vector((c.x,c.y,c.z))
        me=bpy.data.meshes.new(f'{ad}_ayakkabi{k}'); bm.to_mesh(me); bm.free()
        o=bpy.data.objects.new(me.name,me); bpy.context.scene.collection.objects.link(o)
        rm=o.modifiers.new('r','REMESH'); rm.mode='SMOOTH'; rm.octree_depth=6; rm.scale=.9
        d=o.modifiers.new('sis','DISPLACE'); d.direction='NORMAL'; d.strength=.006; d.mid_level=0
        sm=o.modifiers.new('y','SMOOTH'); sm.factor=1; sm.iterations=6
        o.data.materials.append(M['ayakkabi']); o['tag']='beyaz'
        for p_ in me.polygons: p_.use_smooth=True
        _kemige(o,ten.parent,f'foot.{k}'); out['ayakkabi'+k]=o
    gz=ten.vertex_groups.new(name='gorunur'); gizli=set()
    for f in forma+sort+ayak+corap:
        if all(_pay(A,i,FORMA_B+SORT_B+AYAK_B+('lowerleg02',))>.6 for i in P[f].vertices): gizli.update(P[f].vertices)
    for v in V:
        if v.index not in gizli or any(_pay(A,v.index,('neck01','upperarm02','upperleg02'))>.3 for _ in [0]): gz.add([v.index],1,'REPLACE')
    mk=ten.modifiers.new('gizle','MASK'); mk.vertex_group='gorunur'; ten.modifiers.move(len(ten.modifiers)-1,0)
    pc[ad+'_tayt'].hide_render=True; pc[ad+'_tayt'].hide_viewport=True
    # saç ve kafa bandı göz merkezine göre: eğik düzlem (alnında yüksek, ensede alçak saç çizgisi)
    gozc=[sum((v.co for v in pc[ad+'_'+k].data.vertices),Vector())/len(pc[ad+'_'+k].data.vertices) for k in ('gozL','gozR')]
    gm=(gozc[0]+gozc[1])/2; gd=(gozc[0]-gozc[1]).length   # göz arası (~6,3 cm gerçek)
    hd=[p.index for p in P if pay(p,('head',))>.85]
    nrm=Vector((0,-.55,1)).normalized()
    def h(c): return (c-gm).dot(nrm)/gd
    sac=[p.index for p in P if pay(p,('head',))>.6 and sum(h(V[i].co)>.62 for i in p.vertices)>=2]
    out['sac']=_kabuk(ten,ad+'_sackabuk',sac,.009,M['sac'],'sac')
    tx=bpy.data.textures.new(ad+'_sac_t','CLOUDS'); tx.noise_scale=.012; d=out['sac'].modifiers['sis']; d.texture=tx; d.strength=.016; d.mid_level=.35   # kısa saç dokusu: düz kask gibi durmasın
    kas=[f for f in hd if P[f].normal.y>.6 and all(.17<(V[i].co.z-gm.z)/gd<.32 and .12<abs(V[i].co.x-gm.x)/gd<.95 for i in P[f].vertices)]
    out['kas']=_kabuk(ten,ad+'_kas',kas,.0025,M['sac'],'sac'); out['kas'].hide_render=True   # ince kabuk kaş uzaktan görünmüyordu: yerine tene boyanan kaş (aşağıda)
    _yuz_boya(ten,V,gm,gd,A)
    # bant: saç çizgisinin hemen üstünde, düzleme dik halka; ağ kesitine oturan elips (kulaklar dışarıda kalsın)
    hv=[V[i].co for f in hd for i in P[f].vertices]; dil=[v for v in hv if abs(h(v)-.95)<.06]
    cx=sum(dil,Vector())/len(dil); ex=nrm.cross(Vector((1,0,0))).normalized()*-1; xx=Vector((1,0,0))
    rx=max(abs((v-cx).dot(xx)) for v in dil)+.003; ry=max(abs((v-cx).dot(ex)) for v in dil)+.003
    bpy.ops.mesh.primitive_torus_add(location=cx,major_radius=1,minor_radius=.01,major_segments=48,minor_segments=10); b=bpy.context.object; b.name=ad+'_kafabandi'
    for v in b.data.vertices: v.co=Vector((v.co.x*rx,v.co.y*ry,v.co.z*.011/.01*.9))
    b.rotation_mode='QUATERNION'; b.rotation_quaternion=Vector((0,0,1)).rotation_difference(nrm); b.data.materials.append(M['bant']); b['tag']='bant'; bpy.ops.object.shade_smooth(); _kemige(b,ao:=ten.parent,'head'); out['bant']=b
    pc[ad+'_sac'].hide_render=True; pc[ad+'_sac'].hide_viewport=True
    for k in ('gozL','gozR'):   # yardımcı göz ağı kaba (72 köşe): yerine aynı yerde düzgün küre, iris köşe renginden
        h=pc[ad+'_'+k]; c=sum((v.co for v in h.data.vertices),Vector())/len(h.data.vertices); r=max(h.dimensions)/2*.98
        h.hide_render=True; h.hide_viewport=True
        bpy.ops.mesh.primitive_uv_sphere_add(radius=r,location=c,segments=96,ring_count=48); g=bpy.context.object; g.name=ad+'_'+k+'_kure'
        ca=g.data.color_attributes.new('renk','FLOAT_COLOR','POINT')
        for v in g.data.vertices:
            n_=v.co.normalized(); d=n_.y
            col=((.05,.035,.03,1) if n_.z<.47 else (.74,.52,.40,1)) if n_.z>.40 and d>0 else \
                (1,1,1,1) if (n_.x-.18)**2+(n_.z-.22)**2<.012 else (.02,.015,.01,1) if d>.95 else (.28,.17,.08,1) if d>.82 else (.12,.08,.05,1) if d>.79 else (.90,.89,.87,1)
            # büyük iris + parlama noktası: uzaktan bakışı okunsun. Üstte göz kapağı (kirpik çizgisiyle) irisin üstünü örter:
            # tam açık beyaz küre şaşkın bakıyordu (yasin: "bir yüz ifadesi olmalı"), kapak odaklı bakış verir. 96×48: iris kenarı kırık çıkmasın
            ca.data[v.index].color=col
        g.data.materials.append(M['goz']); bpy.ops.object.shade_smooth(); _kemige(g,ao,'head'); out[k]=g
    # bileklik: ön kol ucunda ten yüzünden halka
    bil=[p.index for p in P if pay(p,('lowerarm02.R',))>.5 and _uzunluk(ao,'lowerarm02.R',p.center)>.72]
    out['bilek']=_kabuk(ten,ad+'_bileklik',bil,.008,M['bilek'],'bilek')
    return out
def _uzunluk(ao,bn,p):
    b=ao.data.bones[bn]; d=b.tail_local-b.head_local; return (p-b.head_local).dot(d)/d.length_squared
def _kemige(o,ao,bone):
    M=o.matrix_world.copy(); o.parent=ao; o.parent_type='BONE'; o.parent_bone=bone
    bpy.context.view_layer.update(); o.matrix_world=M

# ---------- raket: gerçek ölçüler (BWF sınırı: boy ≤ 68 cm, kafa ≤ 29 cm × 23 cm; tipik 67.5 cm, kafa 23 × 20 cm) ----------
def raket(el, rk, yuz, s, M, ad='raket', tel_kalin=.00055):
    """el: avuç merkezi (sap ortası), rk: sap→kafa yönü, yuz: kafa yüzey normali tahmini, s: gövde ölçeği (boy/1.78)"""
    rk=rk.normalized(); fn=(yuz-rk*yuz.dot(rk)).normalized(); x=rk.cross(fn)
    R=Matrix((x,rk,fn)).transposed()
    def yerel(p): return el+R@Vector(p)
    nes=[]
    def ekle(o,mat,tag): o.data.materials.append(mat); o['tag']=tag; nes.append(o); bpy.ops.object.shade_smooth(); return o
    def silindir(a,b,r,mat,tag,v=16):
        a=yerel(a); b=yerel(b); d=b-a; bpy.ops.mesh.primitive_cylinder_add(vertices=v,radius=r,depth=d.length,location=(a+b)/2); o=bpy.context.object
        o.rotation_mode='QUATERNION'; o.rotation_quaternion=d.to_track_quat('Z','Y'); return ekle(o,mat,tag)
    L=.675*s; gL=.20*s; gr=.0135*s; shL=.26*s; a_=.115*s; b_=.098*s
    y0=-gL*.55   # sapın alt ucu avucun biraz altında
    silindir((0,y0,0),(0,y0+gL,0),gr,M['grip'],'grip',v=8)          # sekizgen sap + grip bandı
    silindir((0,y0-.004*s,0),(0,y0,0),gr*1.08,M['koyu'],'koyu',v=8)  # dip kapağı
    silindir((0,y0+gL,0),(0,y0+gL+.02*s,0),gr*.6,M['koyu'],'koyu')   # sap boynu
    sy=y0+gL+.02*s; silindir((0,sy,0),(0,sy+shL,0),.0042*s,M['koyu'],'koyu',v=12)   # şaft
    cy=sy+shL+a_*.92
    # çerçeve: elips boru (kafa izometrik: üstte geniş)
    cd=bpy.data.curves.new(ad+'_kafa_c','CURVE'); cd.dimensions='3D'; cd.bevel_depth=.0048*s; cd.bevel_resolution=3
    sp=cd.splines.new('POLY'); N=64; sp.points.add(N-1); sp.use_cyclic_u=True
    for i in range(N):
        t=2*math.pi*i/N; xx=math.sin(t)*b_; yy=math.cos(t)*a_; xx*=1+.06*math.cos(t)   # izometrik: kafanın üstü biraz geniş
        sp.points[i].co=(*yerel((xx,cy+yy,0)),1)
    cu=bpy.data.objects.new(ad+'_kafa_c',cd); bpy.context.scene.collection.objects.link(cu)
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=cu; cu.select_set(True); bpy.ops.object.convert(target='MESH'); cer=bpy.context.object; cer.name=ad+'_kafa'; ekle(cer,M['cerceve'],'cerceve')
    # teller: 22 ana, 21 çapraz (gerçek desen), elips içinde
    tr=tel_kalin*s
    for i in range(22):
        xx=(-1+(i+.5)/11)*b_*.94; h=a_*.94*math.sqrt(max(0,1-(xx/(b_*.94))**2))
        silindir((xx,cy-h,0),(xx,cy+h,0),tr,M['tel'],'tel',v=5)
    for j in range(21):
        yy=(-1+(j+.5)/10.5)*a_*.94; w=b_*.94*math.sqrt(max(0,1-(yy/(a_*.94))**2)); o=silindir((-w,cy+yy,tr*.9),(w,cy+yy,tr*.9),tr,M['tel'],'tel2',v=5)   # çapraz teller ayrı etiket: iki renkli kordaj
    # T-bağlantı
    silindir((0,sy+shL-.004*s,0),(0,cy-a_*.9,0),.005*s,M['cerceve'],'cerceve',v=10)
    return nes, yerel((0,cy,0))

def _grip(m):   # grip bandı: sapa sarılmış şerit (çapraz dalga kabartısı)
    nt=m.node_tree; w=nt.nodes.new('ShaderNodeTexWave'); w.wave_type='BANDS'; w.bands_direction='DIAGONAL'; w.inputs['Scale'].default_value=60; tc=nt.nodes.new('ShaderNodeTexCoord'); nt.links.new(tc.outputs['Object'],w.inputs['Vector'])
    bm=nt.nodes.new('ShaderNodeBump'); bm.inputs['Strength'].default_value=.5; nt.links.new(w.outputs['Fac'],bm.inputs['Height']); nt.links.new(bm.outputs['Normal'],nt.nodes['Principled BSDF'].inputs['Normal']); return m
def _kabarti(m,olcek,guc):
    nt=m.node_tree; b=nt.nodes['Principled BSDF']
    if b.inputs['Normal'].links: return
    n=nt.nodes.new('ShaderNodeTexNoise'); n.inputs['Scale'].default_value=olcek; bm=nt.nodes.new('ShaderNodeBump'); bm.inputs['Strength'].default_value=guc
    nt.links.new(n.outputs['Fac'],bm.inputs['Height']); nt.links.new(bm.outputs['Normal'],b.inputs['Normal'])
def _kumas(m):   # spor forma örgüsü: iki yönlü ince dalga
    nt=m.node_tree; b=nt.nodes['Principled BSDF']
    if b.inputs['Normal'].links: return
    tc=nt.nodes.new('ShaderNodeTexCoord'); w1=nt.nodes.new('ShaderNodeTexWave'); w2=nt.nodes.new('ShaderNodeTexWave')
    for w,d in ((w1,'X'),(w2,'Y')): w.inputs['Scale'].default_value=260; w.bands_direction=d; nt.links.new(tc.outputs['Object'],w.inputs['Vector'])
    mx=nt.nodes.new('ShaderNodeMath'); mx.operation='MULTIPLY'; nt.links.new(w1.outputs['Fac'],mx.inputs[0]); nt.links.new(w2.outputs['Fac'],mx.inputs[1])
    bm=nt.nodes.new('ShaderNodeBump'); bm.inputs['Strength'].default_value=.18; nt.links.new(mx.outputs[0],bm.inputs['Height']); nt.links.new(bm.outputs['Normal'],b.inputs['Normal'])
    b.inputs['Sheen Weight'].default_value=.3
def malzemeler(FORMA,SORT,BEYAZ,BANT,CERCEVE,TEL,KOYU):
    """karakter.py'nin boyanan malzemeleri (forma/bant/çerçeve maskeden boyanır) + gerçekçi ten, göz, saç"""
    def mat(n,c,r=.6,**k):
        m=bpy.data.materials.new(n); m.use_nodes=True; b=m.node_tree.nodes['Principled BSDF']; b.inputs['Base Color'].default_value=(*c,1); b.inputs['Roughness'].default_value=r
        for a,v in k.items(): b.inputs[a].default_value=v
        return m
    ten=mat('ten_g',(0.80,0.56,0.42),.5); nt=ten.node_tree; b=nt.nodes['Principled BSDF']; nt.links.new(_yuz_dugum(nt,(0.80,0.56,0.42)),b.inputs['Base Color'])
    b.inputs['Subsurface Weight'].default_value=.15; b.inputs['Subsurface Radius'].default_value=(.01,.004,.002)
    goz=bpy.data.materials.new('goz_g'); goz.use_nodes=True; nt=goz.node_tree; gb=nt.nodes['Principled BSDF']; at=nt.nodes.new('ShaderNodeAttribute'); at.attribute_name='renk'; nt.links.new(at.outputs['Color'],gb.inputs['Base Color']); gb.inputs['Roughness'].default_value=.15
    sac=mat('sac_g',(0.05,0.035,0.025),.55); nt=sac.node_tree; n=nt.nodes.new('ShaderNodeTexNoise'); n.inputs['Scale'].default_value=400; bm=nt.nodes.new('ShaderNodeBump'); bm.inputs['Strength'].default_value=.6; nt.links.new(n.outputs['Fac'],bm.inputs['Height']); nt.links.new(bm.outputs['Normal'],nt.nodes['Principled BSDF'].inputs['Normal'])
    for m_ in (FORMA,SORT): _kumas(m_)   # doku: ekranda düz plastik gibi duruyordu; ince örgü kabartısı ışığı kırar
    _kabarti(ten,900,.08)
    return dict(ten=ten,goz=goz,sac=sac,forma=FORMA,sort=SORT,beyaz=BEYAZ,ayakkabi=mat('ayk_g',(0.95,0.95,0.95),.35),bant=BANT,bilek=BANT,
        grip=_grip(mat('grip_g',(0.85,0.85,0.85),.75)),tel=TEL,telzemin=mat('telz_g',(0.9,0.9,0.85),.4,Alpha=.12),koyu=KOYU,cerceve=CERCEVE,
        mantar=mat('mantar_g',(0.75,0.6,0.4),.8),tuy=mat('tuy_g',(0.97,0.97,0.95),.6))

# Arcade tarzları (yasin 2026-10-23: "daha arcade karakterler olabilir, doku kalitesi düşüktü"): aynı iskelet ve pozlar,
# kafa/el/ayak kemiği ölçeği (çocuklar ve oyun karakterleri büyük kafa/el ile okunur), cel gölge ve dış çizgi.
# kafa/el/ayak: kemik ölçeği; toon: gölge basamağı (0 = gerçekçi PBR); cizgi: dış çizgi kalınlığı (boyun oranı); kenar: arka ışık
STILLER={'gercek':{}, 'sportif':dict(kafa=1.3,el=1.25,ayak=1.15,toon=3,cizgi=.006,goz=1.25),
    'kafali':dict(kafa=1.65,el=1.4,ayak=1.3,toon=2,cizgi=.009,goz=1.4), 'yumusak':dict(kafa=1.35,el=1.2,ayak=1.15,kenar=1)}
def _toon(m,adim,kenar=False):
    """Principled'ı ışığa bakan cel gölgeye çevirir. Işık kameradan ve yukarıdan (her bakışta aynı okunsun)."""
    nt=m.node_tree; b=nt.nodes.get('Principled BSDF'); out=nt.nodes.get('Material Output')
    if not b or not out or b.inputs['Alpha'].default_value<1: return
    L=nt.links; bc=b.inputs['Base Color']; src=bc.links[0].from_socket if bc.links else None
    g=nt.nodes.new('ShaderNodeNewGeometry'); up=nt.nodes.new('ShaderNodeCombineXYZ'); up.inputs[2].default_value=1.1
    ad=nt.nodes.new('ShaderNodeVectorMath'); ad.operation='ADD'; L.new(g.outputs['Incoming'],ad.inputs[0]); L.new(up.outputs[0],ad.inputs[1])
    nr=nt.nodes.new('ShaderNodeVectorMath'); nr.operation='NORMALIZE'; L.new(ad.outputs[0],nr.inputs[0])
    dt=nt.nodes.new('ShaderNodeVectorMath'); dt.operation='DOT_PRODUCT'; L.new(g.outputs['Normal'],dt.inputs[0]); L.new(nr.outputs[0],dt.inputs[1])
    cr=nt.nodes.new('ShaderNodeValToRGB'); cr.color_ramp.interpolation='EASE' if m.name.startswith('ten') else 'CONSTANT'; e=cr.color_ramp.elements   # ten keskin basamakta yüzü lekeli gösteriyordu: yumuşak geçiş
    sev=[(0,.52),(.35,.8),(.7,1.0)] if adim>=3 else [(0,.6),(.4,1.0)]
    e[0].position,e[0].color=sev[0][0],(sev[0][1],)*3+(1,); e[1].position,e[1].color=sev[1][0],(sev[1][1],)*3+(1,)
    for pz,v in sev[2:]: x=e.new(pz); x.color=(v,)*3+(1,)
    L.new(dt.outputs['Value'],cr.inputs[0])
    mx=nt.nodes.new('ShaderNodeMix'); mx.data_type='RGBA'; mx.blend_type='MULTIPLY'; mx.inputs['Factor'].default_value=1
    L.new(cr.outputs['Color'],mx.inputs[6])
    if src: L.new(src,mx.inputs[7])
    else: mx.inputs[7].default_value=bc.default_value
    em=nt.nodes.new('ShaderNodeEmission'); L.new(mx.outputs[2],em.inputs['Color'])
    L.new(em.outputs[0],out.inputs['Surface'])
def _kenar(m):
    """Yumuşak 3D: yandan gelen ince arka ışık (fresnel) — silüeti sahadan ayırır"""
    nt=m.node_tree; b=nt.nodes.get('Principled BSDF'); out=nt.nodes.get('Material Output')
    if not b or not out or b.inputs['Alpha'].default_value<1: return
    b.inputs['Roughness'].default_value=min(1,b.inputs['Roughness'].default_value+.1)
    lw=nt.nodes.new('ShaderNodeLayerWeight'); lw.inputs['Blend'].default_value=.1; em=nt.nodes.new('ShaderNodeEmission'); em.inputs['Color'].default_value=(1,.97,.9,1); em.inputs['Strength'].default_value=.25
    ad=nt.nodes.new('ShaderNodeMixShader'); nt.links.new(lw.outputs['Facing'],ad.inputs[0]); nt.links.new(b.outputs[0],ad.inputs[1]); nt.links.new(em.outputs[0],ad.inputs[2]); nt.links.new(ad.outputs[0],out.inputs['Surface'])
def _cizgi_mat():
    m=bpy.data.materials.get('cizgi_g')
    if m: return m
    m=bpy.data.materials.new('cizgi_g'); m.use_nodes=True; nt=m.node_tree; nt.nodes.clear(); o=nt.nodes.new('ShaderNodeOutputMaterial')
    g=nt.nodes.new('ShaderNodeNewGeometry'); e=nt.nodes.new('ShaderNodeEmission'); e.inputs['Color'].default_value=(.06,.05,.07,1); t=nt.nodes.new('ShaderNodeBsdfTransparent'); mx=nt.nodes.new('ShaderNodeMixShader')
    nt.links.new(g.outputs['Backfacing'],mx.inputs[0]); nt.links.new(e.outputs[0],mx.inputs[1]); nt.links.new(t.outputs[0],mx.inputs[2]); nt.links.new(mx.outputs[0],o.inputs['Surface']); return m   # ters kabuk: kameraya bakan yüz şeffaf, silüet kenarı koyu
def stillendir(oy,st):
    if not st: return
    seen=set()
    for o in oy.kalici:
        if o.type!='MESH': continue
        for m in o.data.materials:
            if m and m.name not in seen:
                seen.add(m.name)
                if st.get('toon'): _toon(m,st['toon'] if not m.name.startswith('ten') else 2)
                elif st.get('kenar'): _kenar(m)
        if st.get('goz') and o.name.endswith('_kure'): o.scale=(st['goz'],)*3
        if st.get('cizgi') and not any(k in o.name for k in ('_kas','_goz','_tayt')):   # kaş ve göz çizgisi yüzde kara leke yapıyordu
            o.data.materials.append(_cizgi_mat()); sm=o.modifiers.new('cizgi','SOLIDIFY'); sm.thickness=st['cizgi']*oy.boy; sm.offset=1; sm.use_flip_normals=True; sm.use_rim=False
            sm.material_offset=len(o.data.materials)-1; sm.material_offset_rim=len(o.data.materials)-1
            if o.name.endswith('_ten'):   # parmak ve yüzde kalın çizgi içeri taşıp leke yapıyordu: oralarda %30
                vg=o.vertex_groups.new(name='cizgi_k'); ince=[o.vertex_groups[n].index for n in o.vertex_groups.keys() if any(x in n for x in ('finger','thumb','metacarpal','wrist','head','oris','levator','risorius','eye','jaw'))]
                yz=o.data.attributes.get('yuzuv')
                for v in o.data.vertices:
                    w=sum(g.weight for g in v.groups if g.group in ince); vg.add([v.index],.1 if yz and yz.data[v.index].vector[2]>.5 else max(.3,1-.7*min(1,w)),'REPLACE')
                sm.vertex_group='cizgi_k'; sm.thickness_vertex_group=0   # yüzün önünde çizgi %10: ağız köşesi ve burun deliğinde kara çentik (somurtkan) yapıyordu; yüz hattı kenardan gelir.
                # Sıfır olmaz: tenle çakışan şeffaf kabuktan geçen ışın kafanın içine düşüp bandı/saçı yüzde gösteriyordu

class Oyuncu:
    """Bir kez kurulur; her karede poz + o karenin raketi/topu yeniden kurulur. nesneler: etiketli tüm parçalar"""
    def __init__(s,M,boy_bacak=.49,kas=.75,kilo=.45,ad='insan',stil=None):
        ao,pc,J=kur(boy=1.0,kas=kas,kilo=kilo,ad=ad+'_olc'); I=Iskelet(ao,J); oran=boy_bacak/(I.TH+I.SH)
        for o in list(pc.values())+[ao]: bpy.data.objects.remove(o)
        s.boy=oran; s.ao,s.pc,s.J=kur(boy=oran,kas=kas,kilo=kilo,ad=ad); s.I=Iskelet(s.ao,s.J); s.M=M
        s.pc[ad+'_ten'].data.materials.append(M['ten']); s.pc[ad+'_ten']['tag']='ten'
        s.G=giydir(s.pc,ad,M,s.J); s.gecici=[]
        s.kalici=[o for o in list(s.pc.values())+list(s.G.values()) if not o.hide_render]
        s.stil=STILLER.get(stil or 'gercek',{}); stillendir(s,s.stil)
    def kur_poz(s,p):
        for o in s.gecici: bpy.data.objects.remove(o)
        s.gecici=[]; T=s.I.hedef(p); s.I.uygula(T); s.T=T
        pb=s.ao.pose.bones; mw=s.ao.matrix_world
        for k,bs in (('kafa',('head',)),('el',('wrist.L','wrist.R')),('ayak',('foot.L','foot.R'))):
            v=s.stil.get(k)
            if v:
                for n in bs: pb[n].scale=(v,v,v)
        if s.stil: bpy.context.view_layer.update()
        palm=mw@((pb['metacarpal3.R'].head+pb['metacarpal3.R'].tail)/2)
        nes,s.kafa=raket(palm,T['rk'],T['rkFace'],s.boy/1.78,s.M); s.gecici+=nes
        if p.get('shuttle'):   # servis: sol elde tüy top, mantar aşağıda (gerçek: 25 mm mantar, 85 mm boy)
            k=s.boy/1.78; c=mw@((pb['metacarpal3.L'].head+pb['metacarpal3.L'].tail)/2)+Vector((0,.01,.012))
            bpy.ops.mesh.primitive_uv_sphere_add(radius=.0125*k,location=c); m_=bpy.context.object; m_.data.materials.append(s.M['mantar']); m_['tag']='mantar'
            bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=.012*k,radius2=.034*k,depth=.065*k,end_fill_type='NOTHING',location=c+Vector((0,0,.035*k))); t=bpy.context.object; t.data.materials.append(s.M['tuy']); t['tag']='tuy'
            s.gecici+=[m_,t]
        return s.kalici+s.gecici
