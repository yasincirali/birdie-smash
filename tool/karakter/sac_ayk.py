# Oyuncak 3B: saç biçimleri, kafa bandı ve ayakkabı çeşitleri (2026-10-26, yasin: "tip seçebilme, garip saçlar falan
# seçilebilme ... parayla ya da tüyle saç modeli alacak. Ayakkabı seçebilecek, özelliklerine ve fiyatına göre").
# Sözleşme: /mnt/project-files/birdie-smash/komple-oyun/karakter_varlik_sozlesmesi.md. Her tipin FBX'i bütün saçları
# (sac_<id>__sac) ve bütün ayakkabıları (ayk_<id>__ayakkabi) taşır, Unity yalnız seçileni açar. Ayrı "parçalar" FBX'i
# yerine tip başına gömülü: saç kafa derisinden kurulur (tipin baş biçimine oturur; ortak bir saç ağı dört kafaya
# uymazdı) ve ayakkabı tipin ayağından; FBX'te animasyon kemik başına olduğu için 16 ek ağ boyutu ~%10 artırır.
# Geometri tamamen bizim (MakeHuman CC0 deriden türetilen kabuk + yordamsal tüp/koni/elipsoid): lisans değişmez.
import bpy, bmesh, math, os
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree
E=lambda k,d: float(os.environ.get(k,d))
def _ss(a,b,x):
    t=max(0.,min(1.,(x-a)/(b-a))) if b!=a else (1. if x>=a else 0.); return t*t*(3-2*t)

# id → (bant uyumu ön kararı yerine ölçülür: sikistir()). Sıra = mağaza sırası (ucuzdan pahalıya değil, görünüş akrabalığı).
SAC_IDS=('kisa','kazima','yan','atkuyrugu','topuz','afro','mohikan','anime','orgu','rasta','mullet')
AYK_IDS=('klasik','hafif','yastik','bilekli','karbon')
BANT_KAL=E('BS_OY_BANTKAL','.006')   # bandın deriden yüksekliği: saç bandın altında bu kalınlığa ezilir (gerçekte de ezer)
BANT_SIK=E('BS_OY_BANTSIK','.011')   # bundan fazla ezmek gereken saçta bant gizlenir (afro, dikenler): ezik göze batar

class Bas:
    """baş çerçevesi: göz ortası gm, göz arası gd, tepe; kafa derisi BVH (tam çözünürlük, sadeleştirmeden önce)"""
    def __init__(s,P,F,UW,J,govde_me=None):
        s.P,s.F,s.UW,s.J=P,F,UW,J
        s.gm=(J['eye.L____head']+J['eye.R____head'])/2; s.gd=(J['eye.L____head']-J['eye.R____head']).length
        s.top=max(p.z for p in P)
        hv=[P[i] for i in range(len(P)) if UW[i].get('head',0)>.9 and P[i].z>s.gm.z-.02]
        s.ymin=min(p.y for p in hv); s.xmax=max(abs(p.x) for p in hv)
        s.Hc=Vector((0,s.gm.y-.085,s.gm.z+.02))
        fs=[f for f in F['body'] if max(UW[i].get('head',0)+UW[i].get('neck_01',0) for i in f)>.3]
        s.bvh=BVHTree.FromPolygons(P,fs)
        # ağırlık kaynağı: baş/boyun/üst sırt derisi (saç köşesi en yakın derinin ağırlığını alır; kemik seti bu üçü)
        s.kv=[i for i in range(len(P)) if UW[i].get('head',0)+UW[i].get('neck_01',0)+UW[i].get('spine_03',0)>.5 and P[i].z>s.gm.z-.32]
        s.kd=KDTree(len(s.kv))
        for n,i in enumerate(s.kv): s.kd.insert(P[i],n)
        s.kd.balance()
        # sadeleştirilmiş gövde (oyunda görünen deri): saç ondan en az ARALIK dışarıda kalmalı (aralik())
        s.bvh_d=None; s.tepe_d=[]
        if govde_me is not None:
            vs=[v.co.copy() for v in govde_me.vertices]
            s.bvh_d=BVHTree.FromPolygons(vs,[list(p.vertices) for p in govde_me.polygons if min(vs[i].z for i in p.vertices)>s.gm.z-.20])
            s.tepe_d=[(v.co.copy(),v.normal.copy()) for v in govde_me.vertices if v.co.z>s.gm.z-.20]
    def yon(s,az,el):
        a,e=math.radians(az),math.radians(el); return Vector((math.sin(a)*math.cos(e),math.cos(a)*math.cos(e),math.sin(e)))
    def yuzey(s,d):
        d=d.normalized(); h=s.bvh.ray_cast(s.Hc+d*.4,-d,.6)
        if h[0] is None: return s.Hc+d*.09,d
        n=h[1] if h[1].dot(d)>0 else -h[1]
        return h[0],n
    def disari(s,p,pay):
        """deriye pay mesafesinden yakın/içerdeki noktayı dışarı it"""
        loc,n,i,dd=s.bvh.find_nearest(p,.2)
        if loc is None: return p
        if n.dot(p-s.Hc)<0: n=-n
        h=(p-loc).dot(n)
        return p+n*(pay-h) if h<pay else p

# ---------- saç çizgisi ve kabuk ----------
def cizgi(B,alin=.057,sakak=.017,ense=-.075,kulakz=.030,ense_gen=None):
    gm=B.gm
    def zh(c):
        # saç çizgisi: alında göz hizasının alin üstü, kulak üstünde kulakz, ensede ense (yumuşak geçiş)
        dy=c.y-gm.y; t=_ss(-.03,-.17,dy); ax=abs(c.x)
        e=ense if ense_gen is None else -.075+(ense+.075)*(1-_ss(ense_gen*.6,ense_gen,ax))
        z=gm.z+(alin+sakak*_ss(.012,.055,ax))*(1-t)+e*t
        w=_ss(.050,.066,ax)*_ss(-.005,-.03,dy)*(1-_ss(-.12,-.16,dy))
        return z*(1-w)+max(z,gm.z+kulakz)*w
    return zh

def kabuk(B,zh,kal_f,ad,yumusat=2,alt=.7,sirt=False):
    """kafa derisinden kabuk: saç çizgisi alanla kesilir, köşe normal yönünde kal_f kadar şişer (kenarda sıfıra iner)"""
    import oyuncak as O
    P,F,UW=B.P,B.F,B.UW; gm=B.gm
    kulak=lambda c: abs(c.x)>.078 and c.z<gm.z+.028
    kemik=('head','neck_01')+(('spine_03',) if sirt else ())
    fs=[]
    for f in F['body']:
        cs=[P[i] for i in f]; c=sum(cs,Vector())/len(cs)
        if min(sum(UW[i].get(k,0) for k in kemik) for i in f)<alt: continue
        if max(P[i].z-zh(P[i]) for i in f)>-.004 and not kulak(c): fs.append(f)
    ix=sorted({i for f in fs for i in f}); yeni={o:n for n,o in enumerate(ix)}
    bm=bmesh.new(); V=[bm.verts.new(P[i]) for i in ix]
    for f in fs:
        try: bm.faces.new([V[yeni[i]] for i in f])
        except ValueError: pass
    bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=.0004)   # aynı konumda iki köşe (MakeHuman dikiş): kabukta yarık açıyordu
    O.kes(bm,list(bm.faces),lambda c: c.z-zh(c))
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().z<zh(f.calc_center_median())],context='FACES')
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    # küçük delikler (MakeHuman ağında yinelenen köşeli birkaç yüz faces.new'de düşüyor): kapatılır; yalnız saç çizgisi
    # sınırı kalır (ilk yakın planda tepede elmas biçimli ten lekeleri bunlardı)
    bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=8)
    bm.normal_update()
    sinir=[v.co.copy() for v in bm.verts if v.is_boundary]
    kd=KDTree(len(sinir))
    for i,c in enumerate(sinir): kd.insert(c,i)
    kd.balance()
    yeni_co=[(v,v.co+v.normal*kal_f(v.co.copy(),v.normal.copy(),kd.find(v.co)[2])) for v in bm.verts]
    for v,c in yeni_co: v.co=c
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(ad,me); bpy.context.scene.collection.objects.link(o)
    if yumusat:
        bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=o; o.select_set(True)
        m=o.modifiers.new('y','SMOOTH'); m.factor=.5; m.iterations=yumusat; bpy.ops.object.modifier_apply(modifier=m.name)
    return o

def kal_kisa(B,T0=.003,T=.019,percem=.012,oluk=.0045,frek=(7,14),ramp_r=.022,on_kay=0.,ek=None):
    """eski varsayılan saçın kalınlık kuralı (T0..T yanlardan tepeye, perçem, tepeden yayılan tutam olukları)"""
    gm,gd,top=B.gm,B.gd,B.top; tepe=Vector((0,gm.y-.03,top))
    def f(c,n,db):
        ramp=_ss(0,ramp_r,db)
        y=_ss(gm.z+.4*gd,top-.015,c.z)
        kal=T0+T*y
        on=_ss(gm.y-.04,gm.y+.04,c.y)*_ss(gm.z+.9*gd,top-.03,c.z); kal+=percem*on*(1+on_kay*max(-1,min(1,-c.x/.04)))
        d=c-tepe; az=math.atan2(d.x,-d.y); el=d.length
        kal+=oluk*math.sin(frek[0]*az+frek[1]*el)*y*ramp
        if ek: kal+=ek(c,n,ramp,y)
        return kal*ramp+.0012
    return f

# ---------- yordamsal parçalar ----------
def _cerceve(t,yukari=Vector((0,0,1))):
    t=t.normalized(); a=yukari if abs(t.dot(yukari))<.9 else Vector((1,0,0)); x=(a-t*a.dot(t)).normalized(); return x,t.cross(x)
def tup(bm,yol,rs,kenar=6,uc=True,bas_kapak=False):
    """yol boyunca tüp (paralel taşıma çerçevesi); rs[i]=0 sivri uç"""
    halk=[]; x,y=_cerceve(yol[1]-yol[0])
    for i,p in enumerate(yol):
        t=(yol[min(i+1,len(yol)-1)]-yol[max(i-1,0)]).normalized()
        x=(x-t*x.dot(t)).normalized(); y=t.cross(x)
        if rs[i]<=1e-5: halk.append([bm.verts.new(p)]); continue
        halk.append([bm.verts.new(p+(x*math.cos(2*math.pi*k/kenar)+y*math.sin(2*math.pi*k/kenar))*rs[i]) for k in range(kenar)])
    for a,b in zip(halk,halk[1:]):
        if len(b)==1:
            for k in range(kenar): bm.faces.new((a[k],a[(k+1)%kenar],b[0]))
        elif len(a)==1:
            for k in range(kenar): bm.faces.new((a[0],b[(k+1)%kenar],b[k]))
        else:
            for k in range(kenar): bm.faces.new((a[k],a[(k+1)%kenar],b[(k+1)%kenar],b[k]))
    if uc and len(halk[-1])>1: bm.faces.new(halk[-1])
    if bas_kapak and len(halk[0])>1: bm.faces.new(list(reversed(halk[0])))
def elipsoid(bm,c,r,u=10,v=7,ek=None):
    rows=[]
    for j in range(1,v):
        th=math.pi*j/v; row=[]
        for i in range(u):
            ph=2*math.pi*i/u; d=Vector((math.sin(th)*math.cos(ph),math.sin(th)*math.sin(ph),math.cos(th)))
            k=1+(ek(d) if ek else 0); row.append(bm.verts.new(c+Vector((d.x*r[0],d.y*r[1],d.z*r[2]))*k))
        rows.append(row)
    ust=bm.verts.new(c+Vector((0,0,r[2]))); alt=bm.verts.new(c-Vector((0,0,r[2])))
    for i in range(u): bm.faces.new((ust,rows[0][i],rows[0][(i+1)%u]))
    for a,b in zip(rows,rows[1:]):
        for i in range(u): bm.faces.new((a[i],b[i],b[(i+1)%u],a[(i+1)%u]))
    for i in range(u): bm.faces.new((alt,rows[-1][(i+1)%u],rows[-1][i]))

def _ek_nesne(bm,ad):
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(ad,me); bpy.context.scene.collection.objects.link(o); return o
def _birlestir(ana,ekler):
    ekler=[e for e in ekler if e and e.data.polygons]
    if not ekler: return ana
    bpy.ops.object.select_all(action='DESELECT')
    for e in ekler: e.select_set(True)
    ana.select_set(True); bpy.context.view_layer.objects.active=ana; bpy.ops.object.join(); return ana

# ---------- saç biçimleri ----------
ARALIK=E('BS_OY_SACARALIK','.0036')   # dörtgen köşegeni: simetrik sadeleştirmenin düzlem dışı dörtgenleri 1-2 mm daha taşar
def aralik(o,B):
    """saç köşesini oyunda görünen (sadeleştirilmiş) derinin en az ARALIK dışına iter. Kabuk tam çözünürlüklü deriden
    kurulur; sadeleştirilmiş deri tepede/ensede ondan 3-4 mm taşıyor, ince saçta (kazıma, mohikan, örgü) ten lekeleri
    delip çıkıyordu. Döner: itilen köşe sayısı"""
    if B.bvh_d is None: return 0
    k=0
    for v in o.data.vertices:
        loc,n,i,d=B.bvh_d.find_nearest(v.co,.03)
        if loc is None: continue
        if n.dot(loc-B.Hc)<0: n=-n
        h=(v.co-loc).dot(n)
        if h<ARALIK and h>-.02: v.co=v.co+n*(ARALIK-h); k+=1
    # ters yön: seyrek derinin sivri köşesi saç yüzlerinin arasından çıkabiliyor (köşeler temiz, yüz değil): her deri
    # köşesinin normali boyunca saça uzaklık ölçülür, eksikse çevredeki saç köşeleri yumuşak düşüşle dışarı itilir
    me=o.data
    for _ in range(3):
        Vs=[v.co.copy() for v in me.vertices]; T=BVHTree.FromPolygons(Vs,[list(p.vertices) for p in me.polygons])
        kd=KDTree(len(Vs))
        for i,c in enumerate(Vs): kd.insert(c,i)
        kd.balance(); it={}
        for c,n in B.tepe_d:
            if n.dot(c-B.Hc)<0: n=-n
            h=T.ray_cast(c-n*.012,n,.03)
            if h[0] is None: continue
            d=h[3]-.012
            if d>=ARALIK or d<-.008: continue
            for co,i,dd in kd.find_range(h[0],.012):
                w=(1-_ss(0,.012,dd))*(ARALIK-d+.0004); it[i]=max(it.get(i,0),w)
        if not it: break
        for i,w in it.items():
            v=me.vertices[i]; v.co=v.co+v.normal*w; k+=1
    return k
def aralik_son(saclar,govde,pay):
    """son gövde ağına (birleştirilmiş, ölçeklenmiş) karşı ikinci denetim: deri köşesinin normali boyunca saça uzaklık pay'dan
    azsa çevredeki saç köşeleri dışarı itilir. Kurulum anındaki aralik() yetmiyordu: sadeleştirmenin simetri dikişindeki
    (x=0) köşeler sonraki adımlarda kayıyor, tepede 2-5 mm ten lekesi kalıyordu (poke.py ışın denetimi). Döner: itilen."""
    gv=[v.co.copy() for v in govde.data.vertices]
    zt=max(v.z for v in gv); tepe=[(v.co.copy(),v.normal.copy()) for v in govde.data.vertices if v.co.z>zt-.12]
    tepe+=[(p.center.copy(),p.normal.copy()) for p in govde.data.polygons if p.center.z>zt-.12]
    tepe+=[((gv[e.vertices[0]]+gv[e.vertices[1]])/2,(govde.data.vertices[e.vertices[0]].normal+govde.data.vertices[e.vertices[1]].normal).normalized())
           for e in govde.data.edges if gv[e.vertices[0]].z>zt-.12]
    top=0
    for o in saclar:
        me=o.data; nk=o.get('kabuk_n',len(me.vertices)); Hc=sum((me.vertices[i].co for i in range(nk)),Vector())/max(1,nk)
        for _ in range(6):
            Vs=[v.co.copy() for v in me.vertices]; T=BVHTree.FromPolygons(Vs,[list(p.vertices) for p in me.polygons])
            kd=KDTree(len(Vs))
            for i,c in enumerate(Vs): kd.insert(c,i)
            kd.balance(); it={}
            for c,n in tepe:
                if n.dot(c-Hc)<0: n=-n
                n=(n+(c-Hc).normalized()).normalized()   # dikişte (x=0) deri normali yanlış yöne bakabiliyor: radyal karışım
                h=T.ray_cast(c-n*pay*4,n,pay*10)
                if h[0] is None: continue
                d=h[3]-pay*4
                if d>=pay or d<-pay*3: continue
                for co,i,dd in kd.find_range(h[0],pay*4):
                    w=(1-_ss(0,pay*4,dd))*(pay-d+pay*.15); it[i]=max(it.get(i,0),w)
            if not it: break
            for i,w in it.items():
                v=me.vertices[i]; v.co=v.co+(v.co-Hc).normalized()*w
            top+=len(it)
    return top
def _sac_kur(B,sid):
    """id → nesne (malzeme/UV/ağırlık sonra). Ölçüler gerçek metre (1,80 m tasarım), baş BAS ile büyütülmüş."""
    gm,gd,top=B.gm,B.gd,B.top; ek=[]
    if sid=='kisa':
        o=kabuk(B,cizgi(B),kal_kisa(B),'sac_kisa')
    elif sid=='kazima':
        # ince kabuklarda taban payı 3,2 mm: gövde sonradan %18,5'e sadeleşir ve tepe eğrisinde tam çözünürlüklü deriden
        # ~3 mm dışarı taşar (ilk yakın planda kazıma/mohikan/örgüde ten lekeleri delip çıkıyordu)
        # makine: tek kalınlık, kısa geçiş; ten rengi değil saç rengi (Unity tint), AO kafa derisini koyulaştırır
        o=kabuk(B,cizgi(B,alin=.055),lambda c,n,db: .0020*_ss(0,.005,db)+.0032,'sac_kazima',yumusat=1)
    elif sid=='yan':
        # yandan ayrık: ayrım çizgisi sağda (x=+2,6 cm, alından tepeye), saç sol yana taranır, perçem sola kayık
        xp=.026
        def ekf(c,n,ramp,y):
            ar=math.exp(-((c.x-xp)/.0045)**2)*_ss(gm.y-.13,gm.y-.06,c.y)*_ss(gm.z+.05,gm.z+.09,c.z)
            return -.010*ar*y+.006*_ss(xp,xp-.07,c.x)*y
        o=kabuk(B,cizgi(B,alin=.053),kal_kisa(B,T=.016,percem=.010,oluk=.0018,frek=(5,22),on_kay=.6,ek=ekf),'sac_yan')
    elif sid in ('atkuyrugu','topuz'):
        # geriye taranmış ince kabuk (kadın tipi saç çizgisi biraz alçak, şakakta yumuşak)
        o=kabuk(B,cizgi(B,alin=.060,sakak=.010),kal_kisa(B,T0=.0035,T=.007,percem=0.,oluk=.0012,frek=(26,0),ramp_r=.012),'sac_'+sid)
        bm=bmesh.new()
        if sid=='atkuyrugu':
            # yüksek at kuyruğu: arka-üstten çıkar, 5,5 cm geride sarkar; omuz hizasına inmez (smaçta sırta girmesin)
            Q,n=B.yuzey(B.yon(180,40)); Q=Q+n*.004; b=Vector((0,-1,0))
            yol=[Q-n*.006,Q+n*.008,Q+n*.018+Vector((0,0,-.008)),Q+b*.036+Vector((0,0,-.040)),Q+b*.042+Vector((0,0,-.095)),
                 Q+b*.038+Vector((0,0,-.150)),Q+b*.030+Vector((0,0,-.200)),Q+b*.022+Vector((0,0,-.235))]
            tup(bm,yol,[.013,.0125,.018,.023,.022,.018,.011,0.],kenar=8)
        else:
            # tepe topuzu: arka-tepede iki katlı elipsoid (alt tutam + topuz)
            Q,n=B.yuzey(B.yon(180,62))
            tup(bm,[Q-n*.004,Q+n*.014],[.022,.020],kenar=8,uc=False)
            elipsoid(bm,Q+n*.036,(.043,.040,.033),u=10,v=7,ek=lambda d: .06*math.sin(5*math.atan2(d.y,d.x)+3*d.z))
        ek.append(_ek_nesne(bm,'sac_'+sid+'_ek'))
    elif sid=='afro':
        # afro: kabuk kafa derisinden bir elipsoide (merkez başın biraz üstü/arkası) kadar dolar: yuvarlak kütle, yüzde ve
        # ensede saç çizgisine iner (sabit kalınlık kase/kask gibi duruyordu)
        ec=B.Hc+Vector((0,-.015,.052)); er=Vector((.135,.140,.128))
        def kf(c,n,db):
            p=Vector(((c-ec).x/er.x,(c-ec).y/er.y,(c-ec).z/er.z)); q=Vector((n.x/er.x,n.y/er.y,n.z/er.z))
            A=q.dot(q); Bq=2*p.dot(q); C=p.dot(p)-1; D=Bq*Bq-4*A*C
            t=(-Bq+math.sqrt(D))/(2*A) if D>0 else 0.
            d=c-ec; az=math.atan2(d.x,-d.y); el=math.atan2(d.z,math.hypot(d.x,d.y))
            topak=.004*math.sin(9*az+2*el)*math.sin(11*el+az)
            return max(.004,t+topak)*_ss(0,.050,db)+.003
        o=kabuk(B,cizgi(B,alin=.062,sakak=.008),kf,'sac_afro',yumusat=3)
    elif sid=='mohikan':
        # yanlar sıfır kazıma, ortada testere dişli yele: alından enseye 15 istasyon, tepe/vadi sırayla
        o=kabuk(B,cizgi(B,alin=.056),lambda c,n,db: .0012*_ss(0,.004,db)+.0032,'sac_mohikan',yumusat=1)
        bm=bmesh.new(); N=15; L=[];R=[];T=[]
        for i in range(N):
            th=62+(170-62)*i/(N-1); d=Vector((0,math.cos(math.radians(th)),math.sin(math.radians(th))))
            p,n=B.yuzey(d); p=p+n*.0005
            tg=Vector((0,-math.sin(math.radians(th)),math.cos(math.radians(th))))   # arkaya doğru teğet
            u=i/(N-1); w=.010+.004*math.sin(math.pi*u)
            h=(.020+.040*math.sin(math.pi*min(1,u*1.25)))*(1. if i%2==0 else .62)
            L.append(bm.verts.new(p-Vector((w,0,0))-n*.002)); R.append(bm.verts.new(p+Vector((w,0,0))-n*.002))
            T.append(bm.verts.new(p+(n*math.cos(math.radians(28))+tg*math.sin(math.radians(28)))*h))
        for i in range(N-1):
            bm.faces.new((L[i],L[i+1],T[i+1],T[i])); bm.faces.new((T[i],T[i+1],R[i+1],R[i])); bm.faces.new((R[i],R[i+1],L[i+1],L[i]))
        bm.faces.new((L[0],T[0],R[0])); bm.faces.new((R[-1],T[-1],L[-1]))
        ek.append(_ek_nesne(bm,'sac_mohikan_ek'))
    elif sid=='anime':
        # dikenli anime: orta kalın kabuk + 12 koni; ön perçem dikenleri kaşın 3,5 cm üstünde biter (gözü kapatmaz)
        o=kabuk(B,cizgi(B,alin=.055),kal_kisa(B,T0=.005,T=.016,percem=.008,oluk=.003),'sac_anime')
        bm=bmesh.new()
        # dikenler geniş tabanlı ve geriye yatık (ilk sürüm ince/dik: boynuz gibi duruyordu)
        DK=[(180,70,.105,.036,(0,-1.2,.3)),(140,60,.095,.034,(.5,-1.1,.1)),(220,60,.095,.034,(-.5,-1.1,.1)),(180,45,.090,.034,(0,-1.2,-.3)),
            (115,42,.075,.030,(.7,-.9,-.4)),(245,42,.075,.030,(-.7,-.9,-.4)),(150,28,.065,.028,(.3,-1,-.7)),(210,28,.065,.028,(-.3,-1,-.7)),
            (90,52,.065,.028,(.9,-.4,-.2)),(270,52,.065,.028,(-.9,-.4,-.2)),
            (20,62,.060,.026,(.3,.6,-.9)),(-15,64,.065,.027,(-.3,.6,-.9)),(0,74,.080,.030,(0,.3,-.3))]
        for az,el,Lk,r,dv in DK:
            p,n=B.yuzey(B.yon(az,el)); d=(n*.9+Vector(dv)).normalized()
            uc=p+d*Lk
            if uc.z<gm.z+.035 and uc.y>gm.y-.06: uc=uc+Vector((0,0,gm.z+.035-uc.z))
            orta=p+n*.012+(uc-p)*.5+Vector((0,0,.004))
            tup(bm,[p-n*.010,orta,uc],[r,r*.55,0.],kenar=6)
        ek.append(_ek_nesne(bm,'sac_anime_ek'))
    elif sid=='orgu':
        # kafaya yatık örgü (cornrow): ince kabuk (aralarda kafa derisi koyu AO ile okunur) + 7 sıra boğumlu örgü,
        # alından enseye; ortadaki iki sıra ensede kısa örgü kuyruk olarak sarkar
        zh=cizgi(B,alin=.058)
        o=kabuk(B,zh,lambda c,n,db: .0010*_ss(0,.004,db)+.0032,'sac_orgu',yumusat=1)
        bm=bmesh.new()
        for fi in (-54,-36,-18,0,18,36,54):
            yol=[]
            for j in range(15):
                th=50+150*j/14; t=math.radians(th); f=math.radians(fi)
                d=Vector((math.sin(f),math.cos(t)*math.cos(f),math.sin(t)*math.cos(f)))
                p,n=B.yuzey(d)
                if p.z<zh(p)+.006: continue
                yol.append(p+n*.0058)
            if len(yol)<3: continue
            if fi in (-18,18):
                a=yol[-1]; yol+=[a+Vector((0,-.012,-.025)),a+Vector((0,-.016,-.055)),a+Vector((0,-.014,-.075))]
            rs=[(.0050 if k%2==0 else .0038) for k in range(len(yol))]
            if fi in (-18,18): rs[-1]=.0025
            tup(bm,yol,rs,kenar=5,bas_kapak=True)
        ek.append(_ek_nesne(bm,'sac_orgu_ek'))
    elif sid=='rasta':
        # kısa dread: kabuk + 18 tüp; yüzün önüne düşmez (yan dreadler geriye yatar), uçlar omuzun üstünde
        o=kabuk(B,cizgi(B,alin=.058),kal_kisa(B,T0=.006,T=.006,percem=.002,oluk=.0015,ramp_r=.010),'sac_rasta')
        bm=bmesh.new()
        # dread başın üstüne yatar, sonra sarkar: her nokta kök + aşağı·u, deriden 9 mm dışarı itilir (ilk sürümde kökler
        # bant hizasındaydı ve dreadler yatay etek gibi dışa açılıyordu)
        kok=[(az,32) for az in range(70,291,28)]+[(az,52) for az in range(80,281,40)]+[(az,70) for az in (110,180,250)]
        for az,el in kok:
            p,n=B.yuzey(B.yon(az,el)); geri=_ss(70,180,az if az<=180 else 360-az)
            Lk=.085+.075*geri; asagi=Vector((0,-.35*(1-geri)-.1,-1)).normalized()
            yol=[p-n*.003]
            for j in range(1,7):
                u=j/6; q=p+asagi*Lk*u+n*.004*u
                yol.append(B.disari(q,.0105))
            tup(bm,yol,[.0070,.0070,.0068,.0065,.0062,.0057,.0045],kenar=5)
        ek.append(_ek_nesne(bm,'sac_rasta_ek'))
    elif sid=='mullet':
        # önü/yanları kısa, ense uzun ve kalın: arka saç çizgisi 13 cm aşağıda (boyun derisi), uçta dışa kıvrılır
        zh=cizgi(B,ense=-.135,ense_gen=.06)
        def ekf(c,n,ramp,y):
            ar=_ss(gm.y-.11,gm.y-.15,c.y)*_ss(gm.z-.02,gm.z-.10,c.z)
            return .014*ar+.006*_ss(gm.z-.07,gm.z-.13,c.z)*_ss(gm.y-.11,gm.y-.15,c.y)
        o=kabuk(B,zh,kal_kisa(B,T=.015,percem=.010,oluk=.003,ek=ekf),'sac_mullet',sirt=True,alt=.6)
    else: raise KeyError(sid)
    nk=len(o.data.vertices); o=_birlestir(o,ek); o['kabuk_n']=nk   # join etkin nesnenin köşelerini başa koyar
    return o

# ---------- saç malzemesi, AO, UV, ağırlık ----------
def _ao_uv(o,B,fib=14,mesafe=.045):
    """pişmiş ortam gölgesi v'ye (14 ışın, yarıküre), tel deseni u'ya (tepe çevresinde açı × 6)"""
    me=o.data; Vs=[v.co.copy() for v in me.vertices]
    bt=BVHTree.FromPolygons(Vs,[list(p.vertices) for p in me.polygons])
    dirs=[]
    for i in range(fib):
        z=(i+.5)/fib; r=math.sqrt(1-z*z); a=i*2.399963; dirs.append(Vector((r*math.cos(a),r*math.sin(a),z)))
    ao=[]
    for v in me.vertices:
        n=v.normal; x,y=_cerceve(n); kapali=0
        for d in dirs:
            w=(x*d.x+y*d.y+n*d.z); o_=v.co+n*.0015
            if bt.ray_cast(o_,w,mesafe)[0] is not None or B.bvh.ray_cast(o_,w,mesafe)[0] is not None: kapali+=1
        ao.append(1-kapali/fib)
    ul=me.uv_layers.new(name='UVMap')
    for p in me.polygons:
        az0=None
        for li in p.loop_indices:
            vi=me.loops[li].vertex_index; c=Vs[vi]-B.Hc; az=math.atan2(c.x,-c.y)
            if az0 is None: az0=az
            while az-az0>math.pi: az-=2*math.pi
            while az-az0<-math.pi: az+=2*math.pi
            ul.data[li].uv=(az/(2*math.pi)*6+.5,.04+.92*ao[vi])
    return sum(ao)/len(ao)

def _agirlik(o,B):
    """saç köşesi en yakın 6 deri köşesinin (baş/boyun/üst sırt) ağırlığını alır: ense saçı boynu, at kuyruğunun ucu
    sırtı izler; baş geriye atılınca saç deriden ayrılmaz (eski kural: tümü baş kemiğine rijit)"""
    gs={k:o.vertex_groups.new(name=k) for k in ('head','neck_01','spine_03')}
    for v in o.data.vertices:
        acc={k:0. for k in gs}; tw=0
        for co,n,d in B.kd.find_n(v.co,6):
            w=1/(d+.004); i=B.kv[n]; s=sum(B.UW[i].get(k,0) for k in gs) or 1
            for k in gs: acc[k]+=w*B.UW[i].get(k,0)/s
            tw+=w
        for k,g in gs.items():
            if acc[k]/tw>1e-3: g.add([v.index],acc[k]/tw,'REPLACE')

def saclar(B,mat,halka=None):
    """bütün saç biçimleri: {id: nesne}; halka verilirse bant bölgesi ezilir ve uyum ölçülür. Döner (dict, bant uyumu)"""
    out={}; uyum={}
    for sid in SAC_IDS:
        o=_sac_kur(B,sid); itilen=aralik(o,B); o.name='sac_'+sid; o.data.name=o.name; o['tag']='sac'
        o.data.materials.clear(); o.data.materials.append(mat)
        for p in o.data.polygons: p.use_smooth=True
        uyum[sid]=sikistir(o,halka) if halka else (0.,True)
        _ao_uv(o,B); _agirlik(o,B)
        out[sid]=o
        print('sac:',sid,'itilen',itilen,'üçgen',sum(len(p.vertices)-2 for p in o.data.polygons),'bant ezme cm',round(uyum[sid][0]*100,2),'bant',uyum[sid][1])
    return out,{k:v[1] for k,v in uyum.items()}

# ---------- kafa bandı ----------
def bant(B,mat,halka_r):
    """bant deriye oturur (saça değil): her saç biçimi bandın altında BANT_KAL'a ezilir; kenarlar içe kıvrık (kel kafada
    bile alt kenar deriye 2 mm, saçlıda saçın içinde kalır). Eskisi varsayılan saçın üstüne ölçülüyordu: başka saçta
    ya havada ya gömülü kalırdı."""
    gm=B.gm
    ez=Vector((0,-.30,1)).normalized(); cx=Vector((0,gm.y-.085,gm.z+E('BS_OY_BANT','.050')))
    pts=[B.P[i] for f in B.F['body'] for i in f if B.UW[i].get('head',0)>.8]
    yuk,kal=.024,.0042
    o=halka_r('oy_bant',cx,ez,pts,yuk,kal,mat,'bant',n=32,pay=BANT_KAL,prof=[(-.5,-.9),(-.36,1),(.36,1),(.5,-.9)])
    # halka çerçevesi (sikistir için): her açıda iç yarıçap
    ex=Vector((1,0,0)); ex=(ex-ez*ex.dot(ez)).normalized(); ey=ez.cross(ex); n=32
    r=[0.]*n
    for p in pts:
        d=p-cx; z=d.dot(ez)
        if abs(z)>yuk*.55: continue
        x,y=d.dot(ex),d.dot(ey); i=int(round((math.atan2(y,x)%(2*math.pi))/(2*math.pi)*n))%n; r[i]=max(r[i],math.hypot(x,y))
    for _ in range(3): r=[r[i] if r[i]>0 else max(r[i-1],r[(i+1)%n]) for i in range(n)]
    for _ in range(2): r=[max(r[i],(r[i-1]+r[i]+r[(i+1)%n])/3) for i in range(n)]
    r=[(r[i-1]+2*r[i]+r[(i+1)%n])/4+BANT_KAL for i in range(n)]
    return o,dict(c=cx,ez=ez,ex=ex,ey=ey,r=r,n=n,yuk=yuk,kal=kal)

def _halka_yaricap(h,a):
    n=h['n']; f=(a%(2*math.pi))/(2*math.pi)*n; i=int(f); t=f-i; return h['r'][i%n]*(1-t)+h['r'][(i+1)%n]*t
def sikistir(o,h):
    """bant bölgesindeki saçı bandın dış yüzünün altına (iç yarıçap + kal/2) ezer; gereken en büyük ezme BANT_SIK'i
    aşarsa ezmez ve uyumsuz döner. Ek parçaların (kabuk dışı) bandın 6 mm'den dışından geçen köşeleri sayılmaz: banda
    değmeden üstünden sarkar (bant o saçta gizlenir). Döner (en büyük ezme, uyum)"""
    me=o.data; plan=[]; enb=0.; nk=o.get('kabuk_n',len(me.vertices))
    for v in me.vertices:
        d=v.co-h['c']; z=d.dot(h['ez'])
        if abs(z)>h['yuk']*.5+.006: continue
        x,y=d.dot(h['ex']),d.dot(h['ey']); a=math.atan2(y,x); rr=math.hypot(x,y)
        lim=_halka_yaricap(h,a)+h['kal']*.45
        w=1-_ss(h['yuk']*.5,h['yuk']*.5+.006,abs(z))
        if v.index>=nk and rr>lim+.006: continue   # ek parça bandın dışından geçiyor (at kuyruğu, dread ucu): değmez
        if rr>lim:
            enb=max(enb,(rr-lim)*w if w<1 else rr-lim); plan.append((v,x,y,z,rr,lim,w))
    if enb>BANT_SIK: return enb,False
    for v,x,y,z,rr,lim,w in plan:
        r2=rr+(lim-rr)*w; k=r2/rr
        v.co=h['c']+h['ex']*x*k+h['ey']*y*k+h['ez']*z
    return enb,True

def sac_dokusu(yol,px=128):
    """oyuncak_sac.png: gri tonlu çarpan (Unity saç rengini bununla çarpar). v = pişmiş AO (koyu kökten açık uca),
    u = tel deseni (6 tekrar/tur, yumuşak bantlar). Tümü tiplerde ortak."""
    img=bpy.data.images.new('oyuncak_sac',px,px,alpha=False); pix=[0.]*(px*px*4)
    import random; rnd=random.Random(7); faz=[rnd.random()*6.283 for _ in range(5)]
    for yy in range(px):
        v=(yy+.5)/px; ao=.30+.70*_ss(.02,.98,v)**.8
        for xx in range(px):
            u=(xx+.5)/px
            tel=1-.10*(.5+.5*math.sin(2*math.pi*u*7+faz[0]))-.06*(.5+.5*math.sin(2*math.pi*u*19+faz[1]))-.04*(.5+.5*math.sin(2*math.pi*u*41+faz[2]))
            g=ao*tel; k=(yy*px+xx)*4; pix[k:k+4]=[g,g,g,1.]
    img.pixels=pix; img.filepath_raw=yol; img.file_format='PNG'; img.save(); bpy.data.images.remove(img)

# ---------- ayakkabılar ----------
# Taban altı tüm çeşitlerde aynı (ALT): ball kemiği taban altından 0,02 birim yukarıda, PlayerView.Zemin 3,5 cm
# sözleşmesi ayakkabıdan bağımsız kalır. Fark yukarıda: orta taban yan duvarının yüksekliği ve taşması, saya hacmi,
# burun, bilek yakası, renk vurguları (atlas; saya Unity'de boyanır).
AYK={
 'klasik': dict(burun=.010,gen=1.00,sis=.003,duvar=.006,tasma=.0045,ust_r=(0.90,0.91,0.92),orta=(0.97,0.97,0.96),dis=(0.80,0.10,0.10),vurgu=(0.80,0.10,0.10),plaka=None,yaka=0.,serit=None),
 'hafif':  dict(burun=.014,gen=.94,sis=.0015,duvar=.003,tasma=.0030,ust_r=(0.90,0.91,0.92),orta=(0.82,0.84,0.86),dis=(0.04,0.06,0.16),vurgu=(0.04,0.06,0.16),plaka=None,yaka=0.,serit=(0.04,0.06,0.16),bagcik=(0.04,0.06,0.16)),
 'yastik': dict(burun=.010,gen=1.04,sis=.0045,duvar=.015,tasma=.0085,ust_r=(0.90,0.91,0.92),orta=(0.97,0.97,0.96),dis=(0.10,0.10,0.11),vurgu=(0.10,0.10,0.11),plaka=None,yaka=0.,serit=None),
 'bilekli':dict(burun=.010,gen=1.02,sis=.0035,duvar=.007,tasma=.0050,ust_r=(0.90,0.91,0.92),orta=(0.93,0.91,0.86),dis=(0.42,0.26,0.12),vurgu=(0.42,0.26,0.12),plaka=None,yaka=.085,serit=None),
 'karbon': dict(burun=.013,gen=.96,sis=.002,duvar=.008,tasma=.0040,ust_r=(0.90,0.91,0.92),orta=(0.03,0.03,0.035),dis=(0.03,0.03,0.035),vurgu=(0.55,0.85,0.05),plaka=(0.55,0.85,0.05),yaka=0.,serit=(0.55,0.85,0.05),bagcik=(0.10,0.10,0.11)),
}
def _saya(ad,pts,bal,an,prm,yaka_pts):
    """saya: ayak noktalarının dışbükey zarfı (oyuncak._ayakkabi ile aynı yol), çeşit parametreleriyle"""
    import oyuncak as O
    q=[]
    cx=sum((c.x for c in pts))/len(pts)
    for c in pts:
        c=c.copy(); u=_ss(bal.y-.07,bal.y+.06,c.y); c.y+=prm['burun']*u
        c.x=cx+(c.x-cx)*prm['gen']; c.z=max(0.,c.z); q.append(c)
    if prm['yaka']>0:   # bilekli: ayak bileği çevresindeki çorap noktaları 5 mm dışa, bileğin yaka kadar üstüne dek
        ax=Vector((an.x,an.y,0))
        for c in yaka_pts:
            if c.z<an.z+prm['yaka']:
                d=Vector((c.x,c.y,0))-ax; q.append(c+(d.normalized()*.014 if d.length>1e-4 else Vector()))
    bm=bmesh.new()
    for c in q: bm.verts.new(c)
    bmesh.ops.convex_hull(bm,input=bm.verts[:])
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(ad,me); bpy.context.scene.collection.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=o; o.select_set(True)
    for typ,kw in (('REMESH',dict(mode='SMOOTH',octree_depth=5,scale=.9)),('DISPLACE',dict(direction='NORMAL',strength=prm['sis'],mid_level=0)),
                   ('SMOOTH',dict(factor=1.,iterations=14)),('DECIMATE',dict(ratio=E('BS_OY_AYKD2','.135')*(1.2 if prm['yaka'] else 1.)))):
        m=o.modifiers.new(typ,typ)
        for a,v in kw.items(): setattr(m,a,v)
        bpy.ops.object.modifier_apply(modifier=m.name)
    zmin=min(v.co.z for v in me.vertices)
    for v in me.vertices:
        v.co.z-=zmin
        if v.co.z<.008: v.co.z*=.3
        v.co.z+=.006*_ss(bal.y,bal.y+.08,v.co.y)*(1-_ss(0,.03,v.co.z))
    return o

def _taban(ad,ayk,k,prm,mi):
    """taban: sayanın alt çevresinin dışbükey zarfı; orta taban yan duvarı duvar kadar yükselir, tasma kadar dışa taşar.
    mi: (orta, dış, plaka) malzeme indeksleri"""
    import mathutils.geometry as G
    vs=[c for c in ayk if (c.x>0)==(k=='r')]
    zmin=min(c.z for c in vs); alt=[c for c in vs if c.z<zmin+.018]
    pts=[(c.x,c.y) for c in alt]; hull=[pts[i] for i in G.convex_hull_2d(pts)]
    cx=sum(p[0] for p in hull)/len(hull); cy=sum(p[1] for p in hull)/len(hull)
    def dis(p,d):
        v=Vector((p[0]-cx,p[1]-cy)); return Vector((p[0],p[1]))+v.normalized()*d
    n=24; per=[]
    L=[(Vector(hull[i])-Vector(hull[(i+1)%len(hull)])).length for i in range(len(hull))]; T=sum(L)
    for j in range(n):
        s=T*j/n; i=0
        while s>L[i] and i<len(L)-1: s-=L[i]; i+=1
        a=Vector(hull[i]); b=Vector(hull[(i+1)%len(hull)]); per.append(tuple(a+(b-a)*(s/L[i])))
    ORT,ALT=zmin-.013,zmin-.024; ta=prm['tasma']; o_,d_,p_=mi
    if prm['plaka']:
        K=[(zmin+prm['duvar'],.001,o_),(zmin-.004,ta,o_),(ORT+.002,ta,p_),(ORT-.0015,ta*.95,d_),(ALT+.003,ta*.85,d_),(ALT,.0005,None)]
    else:
        K=[(zmin+prm['duvar'],.001,o_),(zmin+prm['duvar']*.35-.004,ta*.9,o_),(ORT,ta,d_),(ALT+.003,ta*.85,d_),(ALT,.0005,None)]
    bm=bmesh.new(); hal=[]
    for z,d,_ in K: hal.append([bm.verts.new((*dis(p,d),z)) for p in per])
    for j in range(len(K)-1):
        for i in range(n):
            f=bm.faces.new((hal[j][i],hal[j][(i+1)%n],hal[j+1][(i+1)%n],hal[j+1][i])); f.material_index=K[j][2]
    f=bm.faces.new(list(reversed(hal[-1]))); f.material_index=d_
    f=bm.faces.new(hal[0]); f.material_index=o_
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(ad,me); bpy.context.scene.collection.objects.link(o)
    for p in me.polygons: p.use_smooth=False
    return o,ALT

def ayakkabilar(aykco,J,an,yaka_pts,mat_f):
    """{id: nesne} — her çift tek nesne; malzeme yuvaları [ayk_ust, orta, dış, vurgu, (plaka)] (atlas sonra eşler).
    Döner (dict, ALT). mat_f(ad, renk, pürüz) → malzeme."""
    import oyuncak as O
    ust=bpy.data.materials.get('ayk_ust') or mat_f('ayk_ust',(0.90,0.91,0.92),.45)
    out={}; ALT=None
    for aid in AYK_IDS:
        prm=AYK[aid]
        ms=[ust,mat_f(f'ayk_{aid}_orta',prm['orta'],.5),mat_f(f'ayk_{aid}_dis',prm['dis'],.6),mat_f(f'ayk_{aid}_vurgu',prm['vurgu'],.5)]
        ms.append(mat_f(f'ayk_{aid}_plaka',prm['plaka'] or prm['dis'],.35))
        ms.append(mat_f(f'ayk_{aid}_bagcik',prm.get('bagcik',(0.95,0.95,0.94)),.7))   # yuva 5: bağcık paneli (saya boyansa da okunur)
        par=[]; sy={}
        for k,m in (('r','R'),('l','L')):
            bal=J[f'toe1-1.{m}____head']
            s=_saya(f'ayk_{aid}_{k}',[c for c in aykco if (c.x>0)==(k=='r')],bal,an[k],prm,[c for c in yaka_pts if (c.x>0)==(k=='r')])
            sy[k]=s; par.append(s)
        co=[sy[k].matrix_world@v.co for k in 'rl' for v in sy[k].data.vertices]
        for k in 'rl':
            t,ALT=_taban(f'ayk_{aid}_t{k}',co,k,prm,(1,2,4 if prm['plaka'] else 2)); par.append(t)
        for o in par:
            yi=[p.material_index for p in o.data.polygons]
            o.data.materials.clear()   # clear() yüz indekslerini sıfırlar: taban katmanları geri yazılır
            for m in ms: o.data.materials.append(m)
            for p,i_ in zip(o.data.polygons,yi): p.material_index=i_
        # saya bölgeleri alanla kesilir (oyuncak.kes; yüz yüz seçim seyrek ağda testere dişli kamuflaj lekesi bırakıyordu):
        # topuk kapağı (vurgu), yan şerit (çeşide göre), bilekli yakanın dolgulu üst kenarı, bağcık paneli
        for k in 'rl':
            me=sy[k].data; y0=min(v.co.y for v in me.vertices); y1=max(v.co.y for v in me.vertices)
            xc=sum(v.co.x for v in me.vertices)/len(me.vertices)
            bl=J[f'toe1-1.{"R" if k=="r" else "L"}____head']; xm=an[k].x*.6+bl.x*.4
            f_topuk=lambda c: (c.y-(y0+.045-.25*max(0,c.z-.02))) if c.z>.012 else 1.
            def zc(c): u=(c.y-y0)/(y1-y0); return .020+.022*(u-.35)
            f_serit=lambda c: abs(c.z-zc(c))-.0065
            f_yaka=lambda c: (an[k].z+prm['yaka']-.016)-c.z
            f_bag=lambda c: max(abs(c.x-xm)-.020,an[k].y+.012-c.y,c.y-(bl.y-.008),.03-c.z)
            alan=[f_topuk]+([f_serit] if prm['serit'] else [])+([f_yaka] if prm['yaka'] else [])+[f_bag]
            bm=bmesh.new(); bm.from_mesh(me)
            for fn in alan: O.kes(bm,list(bm.faces),fn)
            bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>4])
            for f in bm.faces:
                c=f.calc_center_median(); f.smooth=True; f.material_index=0
                if f_topuk(c)<0: f.material_index=3
                if prm['serit'] and .016<c.z<.040 and y0+.06<c.y<y1-.07 and abs(c.x-xc)>.022 and f_serit(c)<0: f.material_index=3
                if prm['yaka'] and f_yaka(c)<0: f.material_index=3
                if f_bag(c)<0 and f.normal.z>.3: f.material_index=5
            bm.to_mesh(me); bm.free()
        bpy.ops.object.select_all(action='DESELECT')
        for o in par: o.select_set(True)
        bpy.context.view_layer.objects.active=par[0]; bpy.ops.object.join()
        o=bpy.context.view_layer.objects.active; o.name='ayk_'+aid; o.data.name=o.name; o['tag']='ayakkabi'
        # ağırlık: ayak/parmak kökü (eski oy_ayk kuralı), bilekli yaka baldıra karışır
        by={k:(sum((J[f'toe{t}-1.{m}____head'] for t in range(1,6)),Vector())/5).y for k,m in (('r','R'),('l','L'))}
        g={n:o.vertex_groups.new(name=n) for n in ('foot_r','ball_r','calf_r','foot_l','ball_l','calf_l')}
        for v in o.data.vertices:
            k='r' if v.co.x>0 else 'l'; u=_ss(by[k]-.015,by[k]+.02,v.co.y)
            wc=_ss(an[k].z+.012,an[k].z+.055,v.co.z) if prm['yaka'] else 0.
            g['foot_'+k].add([v.index],(1-u)*(1-wc),'REPLACE'); g['ball_'+k].add([v.index],u*(1-wc),'REPLACE')
            if wc>0: g['calf_'+k].add([v.index],wc,'REPLACE')
        out[aid]=o
        print('ayakkabı:',aid,'üçgen',sum(len(p.vertices)-2 for p in o.data.polygons))
    return out,ALT
