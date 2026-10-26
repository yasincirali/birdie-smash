# Oyuncak 3B oyuncu, 2. tasarım (yasin 2026-10-26: "Oyuncak 3B modelin fiziksel olarak gerçekçiye yakınsatıp bir game
# model designer olarak yeniden tasarlamanı istiyorum ... modellerimiz iyi görünmeli"). Eski KayKit Rogue gövdesi (iri baş,
# chibi) oyuncak_kaykit.py'de arşivde (BS_MODEL=oyuncak_kaykit); model kimliği "oyuncak" aynı kalır (Unity ayarı ve
# tuytop_model anahtarı değişmez), Resources/karakter/oyuncak.fbx bunun yerine geçer.
#
# Tasarım kararları (stilize atlet: Badminton League / Tennis Clash / Mario Tennis çizgisi):
#  - Gövde gerçek oranlı: MakeHuman taban ağı + makro hedefler (CC0; insan.govde, genç erkek, kaslı, ince). Baş %7 büyütülür
#    (~7,1 baş boy; gerçekte ~7,5): yakın planda sevimli, yüksek kameradan gövdeyi örtmez (eski oyuncağın sorunu).
#    Eller %6 büyük (raket eli okunsun). Omuz geniş, bel ince: uzaktan V silüeti.
#  - Yüz basit ve stilize: büyütülmüş göz bölgesi (+%16), beyaz göz küresi, büyük koyu iris, göz bebeği ve ışık noktası
#    (ayrı malzeme bölgeleri, doku yok: Unity FBX rengini olduğu gibi alır), üst kirpik şeridi, kalın kaş şeritleri
#    (yüze ışınla oturtulur), hafif koyu dudak. Mimik yok (yasin 2026-10-23).
#  - Saç gerçek geometri: kafa derisinden kabuk, tepe kalın (~2 cm) yanlar kısa, kenarda sıfıra iner (kask kenarı yok),
#    tepeden alna yayılan tutam oluklarıyla; alında hafif kabarık perçem.
#  - Kıyafet gövdenin kendi yüzeyi: bölgeler alanla kesilir (düz, temiz sınır), her bölge kendi kalınlığında şişirilir ve
#    sınırda basamak (dikiş payı) köprülenir: forma bisiklet yaka + kol ağzı şeridi, şortun paçası açık, forma şortun
#    üstünde, beyaz bilek üstü çorap, ayakkabı (parmak girintisi yumuşatılmış saya, burun uzatılmış) + iki katlı taban
#    (beyaz orta taban, renkli dış taban). Altta gizli ten yok: deri bükülünce kıyafetin içinden ten çıkamaz.
#  - Ekipman: kafa bandı (bant) ve raket kolunda bileklik (bilek) ayrı parça, oyun yalnız seçiliyse gösterir.
# İskelet: Mesh2Motion/UE adları (pelvis, spine_01..03, neck_01, head, clavicle/upperarm/lowerarm/hand_l|r, parmaklar,
# thigh/calf/foot/ball_l|r) MakeHuman eklem konumlarına kurulur; deri ağırlıkları MakeHuman'ın kendi ağırlıkları (CC0,
# default_weights.mhw) bu kemiklere toplanarak gelir (bone heat yok, ağırlıksız köşe 0). PlayerView.Calibrate adları bulur.
# Parça etiketleri sade.py ile aynı sözleşme ("<ad>__<etiket>": forma takım rengine boyanır, sort hakemde lacivert,
# bant/bilek ekipman); diğer parçalar FBX malzeme rengini korur. Doku yok (DokuMalzeme sözleşmesi kullanılmaz).
import bpy, bmesh, math, os, json, sys
from mathutils import Vector, Matrix, Quaternion
from mathutils.bvhtree import BVHTree
import insan, aday
R_UE=aday.RIG['ue']
E=lambda k,d: float(os.environ.get(k,d))
BOY=1.80                       # tasarım gerçek metrede; sonda bacak 0,49 birime ölçeklenir (insan/sade/aday ile aynı)
KAS,KILO=E('BS_OY_KAS','.78'),E('BS_OY_KILO','.36')
BAS=E('BS_OY_BAS','1.07')       # baş büyütmesi (1 = gerçek)
GOZ=E('BS_OY_GOZ','1.24')       # göz bölgesi büyütmesi
EL=E('BS_OY_EL','1.06')
ORAN=E('BS_OY_ORAN','.185')      # gövde ağı sadeleştirme oranı (MakeHuman 26,7k üçgen)

# ---------- MakeHuman → UE kemik eşlemesi ----------
def ue_ad(mh):
    """MakeHuman kemiği → UE adı (yüz kemikleri başa, metakarplar ele, ayak parmakları ball'a toplanır)"""
    yan=''; n=mh
    if mh.endswith('.L'): yan='_l'; n=mh[:-2]
    elif mh.endswith('.R'): yan='_r'; n=mh[:-2]
    if n in ('root','spine05','pelvis'): return 'pelvis'
    if n=='spine04': return 'spine_01'
    if n=='spine03': return 'spine_02'
    if n in ('spine02','spine01','breast'): return 'spine_03'
    if n.startswith('neck'): return 'neck_01'
    if n in ('clavicle','shoulder01'): return 'clavicle'+yan
    if n.startswith('upperarm'): return 'upperarm'+yan
    if n.startswith('lowerarm'): return 'lowerarm'+yan
    if n=='wrist' or n.startswith('metacarpal'): return 'hand'+yan
    if n.startswith('finger'):
        f,j=n[6:].split('-'); return ('thumb','index','middle','ring','pinky')[int(f)-1]+'_0'+str(min(int(j),2))+yan   # uç boğum ikinciye: 10 kemik az, FBX ~1 MB küçük
    if n.startswith('upperleg'): return 'thigh'+yan
    if n.startswith('lowerleg'): return 'calf'+yan
    if n=='foot': return 'foot'+yan
    if n.startswith('toe'): return 'ball'+yan
    return 'head'   # head, jaw, eye, levator, oculi, oris, tongue ...
KOL_B=('upperarm','lowerarm','hand','thumb','index','middle','ring','pinky')
BACAK_B=('thigh','calf','foot','ball')
def grup(ue):
    if ue.startswith(KOL_B): return 'kol'
    if ue.startswith(BACAK_B): return 'bacak'
    if ue=='head': return 'bas'
    return 'govde'

def _mat(n,c,r=.6):
    m=bpy.data.materials.new(n); m.use_nodes=True; b=m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value=(*c,1); b.inputs['Roughness'].default_value=r; m.diffuse_color=(*c,1); return m
def _ss(a,b,x):
    t=max(0.,min(1.,(x-a)/(b-a))) if b!=a else (1. if x>=a else 0.); return t*t*(3-2*t)

# ---------- 1) gövde: MakeHuman, stilize oran ----------
def _govde():
    P,F=insan.govde(BOY,KAS,KILO); d,J=insan.eklemler(P)
    W=json.load(open(f'{insan.MH}/rigs/default_weights.mhw'))['weights']
    UW=[{} for _ in P]   # köşe → {ue: ağırlık}
    for bn,lst in W.items():
        u=ue_ad(bn)
        for i,w in lst: UW[i][u]=UW[i].get(u,0)+w
    for a in UW:
        s=sum(a.values())
        if s>0:
            for k in a: a[k]/=s
    j=lambda k: J[k].copy()
    # stilize: baş boyun tabanından, eller bilekten büyür (ağırlıkla yumuşak geçiş); göz bölgesi göz merkezinden
    piv=j('neck03____tail')
    def olc(p,mer,k,w): return mer+(p-mer)*(1+(k-1)*w)
    for i,p in enumerate(P):
        w=UW[i].get('head',0);
        if w>0: P[i]=olc(p,piv,BAS,w)
    for k in list(J):
        if ue_ad(k.split('____')[0])=='head' and not k.startswith('neck'): J[k]=olc(J[k],piv,BAS,1.)
    for yan,mh in (('_r','R'),('_l','L')):
        pv=j(f'wrist.{mh}____head')
        el={'hand','thumb','index','middle','ring','pinky'}
        for i,p in enumerate(P):
            w=sum(v for u,v in UW[i].items() if u.endswith(yan) and u.split('_')[0] in el)
            if w>0: P[i]=olc(P[i],pv,EL,w)
        for k in list(J):
            b=ue_ad(k.split('____')[0])
            if b.endswith(yan) and b.split('_')[0] in el and not k.startswith('wrist'): J[k]=olc(J[k],pv,EL,1.)
    # göz bölgesi: göz merkezinden 2,6 cm içinde düzgün azalan büyütme (göz kapakları ve küre birlikte)
    for mh in ('L','R'):
        c=J[f'eye.{mh}____head'].copy(); R_=.026
        for i,p in enumerate(P):
            dd=(p-c).length
            if dd<R_: P[i]=olc(p,c,GOZ,1-_ss(0,R_,dd))
        for k in (f'eye.{mh}____tail',): J[k]=olc(J[k],c,GOZ,1.)
    # ağız köşeleri 1,6 mm yukarı: hafif, sakin gülümseme (düz ağız sert duruyordu)
    for mh in ('L','R'):
        kc=J[f'oris03.{mh}____head'].copy()
        for i,p in enumerate(P):
            dd=(p-kc).length
            if dd<.014 and p.y>kc.y-.012: P[i]=p+Vector((0,0,E('BS_OY_GULUS','.0016')*(1-_ss(0,.014,dd))))
    # göz aralığı açılır (stilize, uyanık bakış): üst kapak yukarı, alt kapak biraz aşağı; göz küresi yerinde
    gozv={i for g in ('helper-l-eye','helper-r-eye') for f in F.get(g,[]) for i in f}
    for mh,g in (('L','helper-l-eye'),('R','helper-r-eye')):
        gv=[P[i] for f in F[g] for i in f]; c=sum(gv,Vector())/len(gv); r=max((p-c).length for p in gv)
        du,dl=E('BS_OY_KAPAK','.24')*r,E('BS_OY_KAPAK_ALT','.08')*r
        for i,p in enumerate(P):
            if i in gozv or p.y<c.y-.2*r: continue
            dx,dz=p.x-c.x,p.z-c.z; ro=math.hypot(dx*.8,dz)
            if ro>2.0*r: continue
            w=1-_ss(.75*r,2.0*r,ro)
            P[i]=p+Vector((0,0,du*w if dz>0 else -dl*w))
    return P,F,UW,J

# ---------- alan kesimi ----------
def _anahtar(c): return (round(c.x,5),round(c.y,5),round(c.z,5))
def kes(bm,yuzler,f,eps=1e-6):
    """f(co) skaler alanının sıfır kümesinden keser (yalnız verilen yüzlerde). Düzlemde bisect_plane ile aynı; eğri
    yaka için de çalışır. Kenar bölünürken köşe ağırlıkları ara değerlenir (bmesh edge_split)."""
    yuzler=[x for x in yuzler if x.is_valid]
    vs={v for x in yuzler for v in x.verts}; val={v:f(v.co) for v in vs}
    for v in vs:
        if abs(val[v])<eps: val[v]=0.
    ed={e for x in yuzler for e in x.edges}
    sifir=set(v for v in vs if val[v]==0.)
    for e in list(ed):
        a,b=e.verts; va,vb=val[a],val[b]
        if va*vb<0:
            t=va/(va-vb); ne,nv=bmesh.utils.edge_split(e,a,t); val[nv]=0.; sifir.add(nv)
    for x in yuzler:
        if not x.is_valid: continue
        z=[v for v in x.verts if v in sifir]
        if len(z)==2:
            a,b=z
            if any(e for e in a.link_edges if b in e.verts): continue   # zaten komşu
            try: bmesh.utils.face_split(x,a,b)
            except Exception: pass

# ---------- 2) kıyafet bölgeleri ----------
class Kesim:
    """Bölge kuralı (dinlenme duruşunda, gerçek metre). Ölçüler eklemlerden: kol ağzı üst kolun %45'i, şort paçası uyluğun
    %66'sı (dizin ~15 cm üstü), çorap bileğin 10 cm üstü, ayakkabı ağzı bileğin 0,5 cm altı, önde alçak."""
    def __init__(s,J):
        s.J=J; g=lambda k: J[k]
        s.sh={k:g(f'upperarm01.{m}____head') for k,m in (('r','R'),('l','L'))}; s.el={k:g(f'lowerarm01.{m}____head') for k,m in (('r','R'),('l','L'))}
        s.hp={k:g(f'upperleg01.{m}____head') for k,m in (('r','R'),('l','L'))}; s.kn={k:g(f'lowerleg01.{m}____head') for k,m in (('r','R'),('l','L'))}
        s.an={k:g(f'foot.{m}____head') for k,m in (('r','R'),('l','L'))}
        s.KOL=E('BS_OY_KOL','.45'); s.MANSET=.035   # kol ağzı şeridi (m, kol boyunca)
        s.ETEK=s.hp['r'].z+E('BS_OY_ETEK','.012')   # forma alt kenarı (yatay)
        s.SORT=E('BS_OY_SORT','.66'); s.CORAP=E('BS_OY_CORAP','.10')
        n=g('neck01____head'); s.yaka0=(n.z-.032,n.y); s.YAKA=.016
        # raglan biyesi: yakadan koltuk altına beyaz şerit (forma "tasarlanmış" görünsün; renk seçimi gövdede kalır)
        s.RA=Vector((.072,n.z-.050)); s.RB=Vector((.165,s.sh['r'].z-.135)); s.BIYE=.0055
        s.SY=s.hp['r'].y; s.SERIT_S=.0065   # şort yan şeridi: kalça ekseninin yanında dikey
    def f_raglan(s,c):
        d=(s.RB-s.RA).normalized(); p=Vector((abs(c.x),c.z))-s.RA; return d.x*p.y-d.y*p.x
    def raglan_bolge(s,c): return s.RB.y+.035<c.z and abs(c.x)<s.RB.x and c.y<s.SY+E('BS_OY_RAGLAN_ON','-.005')
    # koltuk altına girmez (kol kalkınca orada kıvrılıyordu); yalnız sırtta (2026-10-26: bütçe sadeleştirmesinde öndeki kısa
    # biye göğüste kopuk beyaz çizgilere dönüşüyordu; ön yüz temiz bisiklet yaka, sırtta raglan omuz hattı okunur)
    def sort_yan(s,c): return abs(c.x)>abs(s.hp['r'].x)+.02 and c.z<s.ETEK
    def yan(s,c): return 'r' if c.x>0 else 'l'
    def t_kol(s,c,k): a,b=s.sh[k],s.el[k]; d=b-a; return (c-a).dot(d)/d.length_squared
    def t_uyluk(s,c,k): a,b=s.hp[k],s.kn[k]; d=b-a; return (c-a).dot(d)/d.length_squared
    def t_baldir(s,c,k): a,b=s.kn[k],s.an[k]; d=b-a; return (c-a).dot(d)/d.length_squared
    def f_yaka(s,c): z0,y0=s.yaka0; return c.z-(z0-.55*(c.y-y0)+2.6*c.x*c.x)          # >0: boyun (ten)
    def f_ayk(s,c,k): a=s.an[k]+Vector((0,0,-.005)); return (c-a).dot(Vector((0,-.30,1)).normalized())   # >0: çorap/bacak; sayanın ağzı arkada (aşil) alçak, dilde yüksek
    def kesimler(s):
        """(yüz grubu filtresi, alan) listesi"""
        L=[]
        for k in 'rl':
            L.append((('kol',k),lambda c,k=k: s.t_kol(c,k)-s.KOL))
            L.append((('kol',k),lambda c,k=k: (s.t_kol(c,k)-s.KOL)*(s.el[k]-s.sh[k]).length+s.MANSET))
            L.append((('bacak',k),lambda c,k=k: s.t_uyluk(c,k)-s.SORT))
            L.append((('bacak',k),lambda c,k=k: (1-s.t_baldir(c,k))*(s.an[k]-s.kn[k]).length-s.CORAP))
            L.append((('bacak',k),lambda c,k=k: s.f_ayk(c,k)))
        L.append((('govde','bacak'),lambda c: c.z-s.ETEK))
        for sg in (1,-1):
            # kesim alanları kapısız (kapı sınırında sahte sıfır geçişi kırık üçgenler bırakıyordu); bölge kuralı kapılı
            L.append((('govde',),lambda c,sg=sg: s.f_raglan(c)+sg*s.BIYE))
            L.append((('govde','bacak'),lambda c,sg=sg: c.y-s.SY+sg*s.SERIT_S))
        L.append((('govde',),lambda c: s.f_yaka(c)))
        L.append((('govde',),lambda c: s.f_yaka(c)+s.YAKA))
        return L
    def bolge(s,c,g):
        k=s.yan(c)
        if g=='bas': return 'ten'
        if g=='kol':
            t=s.t_kol(c,k)
            if t<s.KOL: return 'serit' if (s.KOL-t)*(s.el[k]-s.sh[k]).length<s.MANSET else 'forma'
            return 'ten'
        if g=='govde':
            fy=s.f_yaka(c)
            if fy>0: return 'ten'
            if fy>-s.YAKA: return 'serit'
            if c.z>=s.ETEK: return 'serit' if s.raglan_bolge(c) and abs(s.f_raglan(c))<s.BIYE else 'forma'
            return 'biye' if s.sort_yan(c) and abs(c.y-s.SY)<s.SERIT_S else 'sort'
        # bacak
        if c.z>=s.ETEK: return 'forma'
        if s.t_uyluk(c,k)<s.SORT: return 'biye' if s.sort_yan(c) and abs(c.y-s.SY)<s.SERIT_S else 'sort'
        if s.f_ayk(c,k)>0: return 'ten' if (1-s.t_baldir(c,k))*(s.an[k]-s.kn[k]).length>s.CORAP else 'corap'
        return 'ayakkabi'

KAT={'biye':.0095,'ten':0.,'forma':.0065,'serit':.0085,'sort':.0085,'corap':.0022,'ayakkabi':.0085}   # bölge kalınlığı (m)
UST={('ten','forma'):'forma',('ten','serit'):'serit',('forma','serit'):'serit',('forma','sort'):'forma',('ten','sort'):'sort',
     ('ten','corap'):'corap',('sort','biye'):'biye',('ten','biye'):'biye',('forma','biye'):'forma'}

def _kat(k,c,K):
    """bölge kalınlığı + bol kesim: kol ağzı ve şort paçası açılır, formanın eteği şortun üstüne biner"""
    if k=='biye': return _kat('sort',c,K)+.0012
    if k=='serit': return _kat('forma',c,K)+.0016   # şerit her yerde formanın 1,6 mm üstünde (kol ağzı bol kesimde ters basamak olmasın)
    t=KAT[k]; s=K.yan(c)
    if k in ('forma','serit'):
        fy=K.f_yaka(c)
        if fy>-.05: t=t+(.0032-t)*_ss(-.05,-.005,fy)   # yaka boyna oturur (kalın basamak boyunda koyu duvar gibi duruyordu)
        if c.z<K.ETEK+.09 and abs(c.x)<.2: t+=.007*_ss(K.ETEK+.09,K.ETEK,c.z)   # etek: şortun üstüne
        tk=K.t_kol(c,s)
        if 0<tk<K.KOL+.01 and abs(c.x)>.15: t+=.005*_ss(K.KOL-.25,K.KOL,tk)    # kol ağzı bol
    if k=='sort':
        tu=K.t_uyluk(c,s)
        if tu>0: t+=.011*_ss(.2,K.SORT,tu)
    return t

# ---------- 3) yüz ve saç parçaları ----------
def _nesne(ad,me,mat,tag):
    o=bpy.data.objects.new(ad,me); bpy.context.scene.collection.objects.link(o); me.materials.clear(); me.materials.append(mat); o['tag']=tag
    for p in me.polygons: p.use_smooth=True
    return o
def _tek(o,kemik):
    vg=o.vertex_groups.new(name=kemik); vg.add(list(range(len(o.data.vertices))),1,'REPLACE')

def _goz(ad,c,r,mats):
    """göz küresi: kutup öne (+y), halkalar iris/bebek sınırına denk (temiz daire). Döner: [beyaz, iris, bebek, ışık]"""
    out=[]
    bm=bmesh.new(); bmesh.ops.create_uvsphere(bm,u_segments=18,v_segments=12,radius=r)
    bmesh.ops.rotate(bm,verts=bm.verts,cent=(0,0,0),matrix=Matrix.Rotation(-math.pi/2,3,'X'))   # +z kutbu → +y
    bmesh.ops.translate(bm,verts=bm.verts,vec=c)
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free()
    for k in ('goz','iris','bebek'): me.materials.append(mats[k])
    for p in me.polygons:
        d=(p.center-c).normalized().y
        p.material_index=2 if d>E('BS_OY_BEBEK','.955') else 1 if d>E('BS_OY_IRIS','.76') else 0; p.use_smooth=True
    # arka yarı kafanın içinde, hiç görünmez: silinir (performans bütçesi 2026-10-26: göz başına 396 → ~230 üçgen)
    bm=bmesh.new(); bm.from_mesh(me)
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if (f.calc_center_median()-c).normalized().y<-.25],context='FACES')
    bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(ad,me); bpy.context.scene.collection.objects.link(o); o['tag']='goz'
    # ışık noktası: irisin üst-dış yanında küçük beyaz disk (göz kafayla döner; yakın planda canlılık)
    return o

def _serit(bvh,yol,gen,kal,ad,mat,tag,ymin=-1.):
    """yüze ışınla oturan şerit (kaş, kirpik): yol [(x,z)], gen [yarı genişlik]; önden -y yönünde atılan ışın"""
    bm=bmesh.new(); ust=[]; alt=[]
    for (x,z),g in zip(yol,gen):
        hs=[]
        for dz in (g,-g):
            h=bvh.ray_cast(Vector((x,.5,z+dz)),Vector((0,-1,0)))
            if h[0] is None or h[0].y<ymin: hs=None; break
            hs.append(h[0]+h[1]*.0012)
        if not hs: continue
        ust.append(bm.verts.new(hs[0])); alt.append(bm.verts.new(hs[1]))
    for i in range(len(ust)-1): bm.faces.new((alt[i],alt[i+1],ust[i+1],ust[i]))
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free(); o=_nesne(ad,me,mat,tag)
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=o; o.select_set(True)
    sm=o.modifiers.new('k','SOLIDIFY'); sm.thickness=kal; sm.offset=1; bpy.ops.object.modifier_apply(modifier=sm.name)
    return o

def _sac(P,F,UW,J,mat):
    """saç kabuğu: başın tam çözünürlüklü derisinden (sadeleştirilmeden önce), saç çizgisi eğik alan."""
    gm=(J['eye.L____head']+J['eye.R____head'])/2; gd=(J['eye.L____head']-J['eye.R____head']).length
    top=max(p.z for p in P)
    nrm=Vector((0,-.62,1)).normalized()
    def h(c): return (c-gm).dot(nrm)/gd
    kulak=lambda c: abs(c.x)>.078 and c.z<gm.z+.028
    def zh(c):
        # saç çizgisi: alında göz hizasının 6,2 cm üstü, kulak üstünde 3,2 cm, ensede 7,5 cm altı (yumuşak geçiş)
        dy=c.y-gm.y; t=_ss(-.03,-.17,dy); ax=abs(c.x)
        z=gm.z+(.057+.017*_ss(.012,.055,ax))*(1-t)-.075*t        # alın: ortada hafif sivri, şakaklarda geri çekilir (kâse kesimi gibi düz çizgi olmasın)
        # kulak üstü: yan yüzde saç kulağın üstünden dolanır (yumuşak geçiş)
        w=_ss(.050,.066,ax)*_ss(-.005,-.03,dy)*(1-_ss(-.12,-.16,dy))
        return z*(1-w)+max(z,gm.z+.030)*w
    fs=[]
    for f in F['body']:
        cs=[P[i] for i in f]; c=sum(cs,Vector())/len(cs)
        if min(UW[i].get('head',0)+UW[i].get('neck_01',0) for i in f)<.7: continue
        if max(P[i].z-zh(P[i]) for i in f)>-.004 and not kulak(c): fs.append(f)
    ix=sorted({i for f in fs for i in f}); yeni={o:n for n,o in enumerate(ix)}
    bm=bmesh.new(); V=[bm.verts.new(P[i]) for i in ix]
    for f in fs:
        try: bm.faces.new([V[yeni[i]] for i in f])
        except ValueError: pass
    # saç çizgisi alanla kesilir (yüz yüz seçim testere dişi bırakıyordu), altı silinir
    kes(bm,list(bm.faces),lambda c: c.z-zh(c))
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().z<zh(f.calc_center_median())],context='FACES')
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    bm.normal_update()
    # sınırdan uzaklık (kenarda sıfıra iner: kask kenarı yok)
    sinir=[v.co.copy() for v in bm.verts if v.is_boundary]
    from mathutils.kdtree import KDTree
    kd=KDTree(len(sinir))
    for i,c in enumerate(sinir): kd.insert(c,i)
    kd.balance()
    tepe=Vector((0,gm.y-.03,top))
    for v in bm.verts:
        c=v.co; db=kd.find(c)[2]
        ramp=_ss(0,E('BS_OY_SACR','.022'),db)
        y=_ss(gm.z+.4*gd,top-.015,c.z)                 # yanlar kısa (fade), tepe dolgun
        kal=.003+E('BS_OY_SACT','.019')*y
        # perçem: alnın üstü öne/yukarı kabarık
        on=_ss(gm.y-.04,gm.y+.04,c.y)*_ss(gm.z+.9*gd,top-.03,c.z); kal+=E('BS_OY_PERCEM','.012')*on
        # tutamlar: tepeden öne yayılan oluklar, kenara doğru söner
        d=c-tepe; az=math.atan2(d.x,-d.y); el=d.length
        kal+=.0045*math.sin(7*az+14*el)*y*ramp
        v.co=c+v.normal*(kal*ramp+.0012)
    me=bpy.data.meshes.new('oy_sac'); bm.to_mesh(me); bm.free()
    o=_nesne('oy_sac',me,mat,'sac')
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=o; o.select_set(True)
    bpy.ops.object.modifier_add(type='SMOOTH'); o.modifiers[-1].factor=.5; o.modifiers[-1].iterations=2; bpy.ops.object.modifier_apply(modifier=o.modifiers[-1].name)
    return o,gm,gd,h

def _ayakkabi(ad,pts,bal,an,mat,vurgu=None):
    """saya: ayak noktalarının dışbükey zarfı (parmak girintisi yok, sneaker hacmi), burun 1,6 cm uzar ve hafif kalkar,
    yumuşatılıp sadeleştirilir; alt düz (taban bunun altına gelir)"""
    q=[]
    for c in pts:
        c=c.copy(); u=_ss(bal.y-.07,bal.y+.06,c.y); c.y+=.010*u
        c.z=max(0.,c.z); q.append(c)
    bm=bmesh.new()
    for c in q: bm.verts.new(c)
    bmesh.ops.convex_hull(bm,input=bm.verts[:])
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free(); o=_nesne(ad,me,mat,'ayakkabi')
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=o; o.select_set(True)
    for typ,kw in (('REMESH',dict(mode='SMOOTH',octree_depth=5,scale=.9)),('DISPLACE',dict(direction='NORMAL',strength=.003,mid_level=0)),
                   ('SMOOTH',dict(factor=1.,iterations=14)),('DECIMATE',dict(ratio=E('BS_OY_AYKD','.17')))):
        m=o.modifiers.new(typ,typ)
        for a,v in kw.items(): setattr(m,a,v)
        bpy.ops.object.modifier_apply(modifier=m.name)
    zmin=min(v.co.z for v in me.vertices)
    for v in me.vertices:
        v.co.z-=zmin
        if v.co.z<.008: v.co.z*=.3
        v.co.z+=.006*_ss(bal.y,bal.y+.08,v.co.y)*(1-_ss(0,.03,v.co.z))   # burun hafif kalkık (toe spring)
    for p in me.polygons: p.use_smooth=True
    if vurgu:   # topuk kapağı dış taban renginde: arkadan/yandan ayakkabı okunur, beyaz kütle tek düze durmaz
        me.materials.append(vurgu); y0=min(v.co.y for v in me.vertices)
        for p in me.polygons:
            c=p.center
            if c.y<y0+.045-.25*max(0,c.z-.02) and c.z>.012: p.material_index=1
    print('oyuncak: ayakkabı',ad,len(me.polygons))
    return o

def _taban(ad,ayk,K,k,mats,olc):
    """iki katlı taban: ayakkabı ağının alt çevresinin dışbükey zarfı, 4 mm dışa; üst kenar sayanın içinde"""
    vs=[c for c in ayk if (c.x>0)==(k=='r')]
    zmin=min(c.z for c in vs); alt=[c for c in vs if c.z<zmin+.018]
    import mathutils.geometry as G
    pts=[(c.x,c.y) for c in alt]; hull=[pts[i] for i in G.convex_hull_2d(pts)]
    cx=sum(p[0] for p in hull)/len(hull); cy=sum(p[1] for p in hull)/len(hull)
    def dis(p,d):
        v=Vector((p[0]-cx,p[1]-cy)); return Vector((p[0],p[1]))+v.normalized()*d
    # çevreyi eşit aralıklı yeniden örnekle (yumuşak kenar)
    n=24; per=[]
    L=[(Vector(hull[i])-Vector(hull[(i+1)%len(hull)])).length for i in range(len(hull))]; T=sum(L)
    for j in range(n):
        s=T*j/n; i=0
        while s>L[i] and i<len(L)-1: s-=L[i]; i+=1
        a=Vector(hull[i]); b=Vector(hull[(i+1)%len(hull)]); per.append(tuple(a+(b-a)*(s/L[i])))
    TOP,ORT,ALT=zmin+.006,zmin-.013,zmin-.024
    katman=[(TOP,.001),(zmin-.004,.0045),(ORT,.0045),(ALT+.003,.0038),(ALT,.0005)]
    bm=bmesh.new(); hal=[]
    for z,d in katman: hal.append([bm.verts.new((*dis(p,d),z)) for p in per])
    for j in range(len(katman)-1):
        for i in range(n):
            f=bm.faces.new((hal[j][i],hal[j][(i+1)%n],hal[j+1][(i+1)%n],hal[j+1][i])); f.material_index=0 if j<2 else 1
    f=bm.faces.new(list(reversed(hal[-1]))); f.material_index=1
    f=bm.faces.new(hal[0]); f.material_index=0
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free()
    me.materials.append(mats['orta_taban']); me.materials.append(mats['taban'])
    o=bpy.data.objects.new(ad,me); bpy.context.scene.collection.objects.link(o)
    for p in me.polygons: p.use_smooth=False
    return o,ALT

def _halka_r(ad,merkez,ez,pts,yuk,kal,mat,tag,n=40,pay=0.):
    """halka şerit, kesit açı açı ölçülür: her açıda dilimdeki (|z|<yuk/2) noktaların en büyük yarıçapı + pay; komşu
    açılarla yumuşatılır. Kafa bandı saçın, bileklik ön kolun üstüne oturur (elips varsayımı kulak ve ensede boşluk bırakıyordu)"""
    ex=Vector((1,0,0)) if abs(ez.x)<.9 else Vector((0,1,0)); ex=(ex-ez*ex.dot(ez)).normalized(); ey=ez.cross(ex)
    r=[0.]*n
    for p in pts:
        d=p-merkez; z=d.dot(ez)
        if abs(z)>yuk*.55: continue
        x,y=d.dot(ex),d.dot(ey); i=int(round((math.atan2(y,x)%(2*math.pi))/(2*math.pi)*n))%n; r[i]=max(r[i],math.hypot(x,y))
    for _ in range(3):   # boş açıları doldur
        r=[r[i] if r[i]>0 else max(r[i-1],r[(i+1)%n]) for i in range(n)]
    for _ in range(2): r=[max(r[i],(r[i-1]+r[i]+r[(i+1)%n])/3) for i in range(n)]
    r=[(r[i-1]+2*r[i]+r[(i+1)%n])/4+pay for i in range(n)]
    prof=[(-.5,0),(-.36,1),(.36,1),(.5,0)]   # 4 nokta, pahlı kesit (6 noktalı yuvarlak kesit uzak kamerada fark edilmiyordu; bütçe)
    bm=bmesh.new(); hal=[]
    for i in range(n):
        a=2*math.pi*i/n; u=ex*math.cos(a)+ey*math.sin(a)
        hal.append([bm.verts.new(merkez+u*(r[i]+kal*k)+ez*yuk*z) for z,k in prof])
    m=len(prof)
    for i in range(n):
        A,B=hal[i],hal[(i+1)%n]
        for j in range(m-1): bm.faces.new((A[j],B[j],B[j+1],A[j+1]))
        bm.faces.new((A[-1],B[-1],B[0],A[0]))
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    me=bpy.data.meshes.new(ad); bm.to_mesh(me); bm.free(); return _nesne(ad,me,mat,tag)

class OyIskelet(aday.AdayIskelet):
    """UE eşlemesi (aday.AdayIskelet); ayak düz basar, zemin kaydırması tabandan ölçülür, parmaklar avuca kapanır.
    Avuç normali dinlenme duruşunun (MakeHuman A-pozu, avuç içe-aşağı) eklemlerinden hesaplanır."""
    POZ=('root','shR','shL','neck','head','elR','elL','wrR','wrL','hpR','hpL','knR','knL','anR','anL')
    def __init__(s,ao,avuc,dz):
        super().__init__(ao,R_UE); s.avuc_d=avuc
        s._avuc={n:insan.avuc_yerel(ao,n,avuc[n[-1]]) for n in ('lowerarm_r','hand_r','hand_l')}
        s.dz=dz
    def hedef(s,p):
        zmin=E('BS_OY_BILEK','.05'); p=dict(p)
        for K in 'RL':
            a=p['an'+K]
            if a[2]<zmin: p['an'+K]=(a[0],a[1],zmin)
        T=super().hedef(p); dz=Vector((0,0,s.dz))
        for n in s.POZ: T[n]=T[n]+dz
        return T
    def uygula(s,T):
        super().uygula(T)
        ao=s.ao; pb=ao.pose.bones; L=bpy.context.view_layer
        for k,K in (('r','R'),('l','L')):
            b=pb[f'foot_{k}']; M=b.matrix.copy(); R0=ao.data.bones[f'foot_{k}'].matrix_local.to_3x3()
            y=R0@Vector((0,1,0)); yh=Vector((y.x,y.y,0)).normalized(); f=T['ft'+K]
            ang=math.atan2(yh.x*f.y-yh.y*f.x,yh.dot(f)); N=Matrix.Rotation(ang,4,'Z')@Matrix(R0).to_4x4(); N.translation=M.translation; b.matrix=N; L.update()
        ac=math.radians(E('BS_OY_PARMAK','58'))   # 58° (2026-10-26 tutuş ölçümü): 68° parmak uçlarını sapın 1,4 cm içine sokuyordu
        for k,kat in (('r',1.),('l',.35)):
            n_=s.avuc_d[k]
            for f in ('index','middle','ring','pinky','thumb'):
                for j,pay in (('01',.85),('02',1.55)):   # uç boğum ikinciyle birlikte kapanır
                    n=f'{f}_{j}_{k}'
                    if n not in pb: continue
                    bn=ao.data.bones[n]; d=(bn.tail_local-bn.head_local).normalized()
                    ax=d.cross(n_)
                    if ax.length<1e-4: continue
                    ax=(bn.matrix_local.to_3x3().inverted()@ax.normalized()).normalized()
                    q=pb[n]; q.rotation_mode='QUATERNION'; q.rotation_quaternion=Quaternion(ax,ac*kat*pay*(.5 if f=='thumb' else 1))
        L.update()


# ---------- iç içe geçme çözücü (2026-10-26, yasin: "objelerde iç içe geçmeleri de çözmen lazım") ----------
# Pozlar paylaşılan sözlüklerden gelir ve her modelin oranı farklı: aynı bilek hedefi oyuncak 3B'nin göğsüne girebilir.
# Bu model kendi ağıyla denetler: uygula() sonrası derili ağ değerlendirilir, el/ön kol/üst kol ucu gövde, baş ve
# bacaklara; baldır/ayak öbür bacağa; taban zemine; raket (çerçeve, şaft, sap) gövdeye ve zemine karşı ölçülür.
# Girme varsa hedef dışarı itilir (bilek/dirsek/ayak bileği IK'yla yeniden çözülür; raket avuç çevresinde döndürülür) ve
# yeniden uygulanır. İçerik testi: en yakın yüzün içinden, normale ters (|cos|>0,8) yaklaşan nokta "içeride" sayılır
# (açık alt kümelerde kenar yakınındaki işaret hatasını ayıklar; denetim aracı icice.py ile aynı kural).
def kat_kemik(b):
    import re
    if b=='hand_r' or re.match(r'(thumb|index|middle|ring|pinky)_0\d_r$',b): return 'elR'
    if b=='hand_l' or re.match(r'(thumb|index|middle|ring|pinky)_0\d_l$',b): return 'elL'
    return {'lowerarm_r':'onkolR','lowerarm_l':'onkolL','upperarm_r':'kolR','upperarm_l':'kolL','head':'bas','neck_01':'bas',
            'thigh_r':'uylukR','thigh_l':'uylukL','calf_r':'bacakR','foot_r':'bacakR','ball_r':'bacakR',
            'calf_l':'bacakL','foot_l':'bacakL','ball_l':'bacakL'}.get(b,'govde' if b.startswith(('spine','pelvis','clavicle')) else 'diger')
class Carpisma:
    TOL=E('BS_OY_TOL','.0012'); PAY=E('BS_OY_PAY','.0035')
    def __init__(s,ao,parcalar,raket_yerel):
        s.ao=ao; s.par=parcalar; s.vk={}; s.fk={}
        for ix,o in enumerate(parcalar):   # sıra anahtarı: unity_fbx parçaları sonradan yeniden adlandırır
            gi={g.index:g.name for g in o.vertex_groups}
            vk=[kat_kemik(gi[max(v.groups,key=lambda g:g.weight).group]) if len(v.groups) else 'diger' for v in o.data.vertices]
            s.vk[ix]=vk
            fk=[]
            for f in o.data.polygons:
                c=[vk[i] for i in f.vertices]; fk.append((tuple(f.vertices),max(set(c),key=c.count)))
            s.fk[ix]=fk
        s.rk_yerel=raket_yerel
    def degerlendir(s):
        dg=bpy.context.evaluated_depsgraph_get(); V={}
        for ix,o in enumerate(s.par):
            e=o.evaluated_get(dg); me=e.to_mesh(); M=o.matrix_world; V[ix]=[M@v.co for v in me.vertices]; e.to_mesh_clear()
        s.V=V; s._bvh={}
    def bvh(s,kats):
        k=frozenset(kats)
        if k in s._bvh: return s._bvh[k]
        vs=[]; fs=[]
        for ix,o in enumerate(s.par):
            b=len(vs); vs+=s.V[ix]; fs+=[[b+i for i in f] for f,c in s.fk[ix] if c in k]
        t=BVHTree.FromPolygons(vs,fs) if fs else None; s._bvh[k]=t; return t
    def noktalar(s,kat,ua=None):
        out=[]
        for ix,o in enumerate(s.par):
            for p,c in zip(s.V[ix],s.vk[ix]):
                if c!=kat: continue
                if ua is not None:
                    h,t=ua; d=t-h
                    if (p-h).dot(d)/d.length_squared<.55: continue   # koltuk altı gövdeye bitişik: yalnız üst kolun dirsek yarısı
                out.append(p)
        return out
    YONLER=[Vector(v) for v in ((1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1))]
    @staticmethod
    def icinde(T,p):
        """ışın paritesi, 6 yönde çoğunluk: normal işareti uzak/katlanmış yüzde (koltuk altı, etek duvarı) yanılıyordu
        (sap gövdenin 31 cm önündeyken "içeride" çıkıyordu)"""
        tek=0
        for d in Carpisma.YONLER:
            o=p.copy(); n=0
            for _ in range(24):
                loc,nr,i,dist=T.ray_cast(o,d,2.)
                if loc is None: break
                n+=1; o=loc+d*1e-4
            tek+=n%2
        return tek>=4
    @staticmethod
    def en_derin(T,pts):
        best=(0.,None,None)
        if T is None: return best
        for p in pts:
            loc,n,i,d=T.find_nearest(p,.15)
            if loc is None: continue
            if (p-loc).dot(n)< -.8*d and d>best[0] and Carpisma.icinde(T,p): best=(d,n.copy(),p.copy())
        return best
    def raket_dunya(s,palm,rk,yuz):
        rk=rk.normalized(); fn=(yuz-rk*yuz.dot(rk)).normalized(); x=rk.cross(fn); R=Matrix((x,rk,fn)).transposed()
        return [palm+R@q for q in s.rk_yerel]
ARM_HEDEF={'R':('govde','bas','uylukR','uylukL','bacakR','bacakL','kolL','onkolL','elL'),
           'L':('govde','bas','uylukR','uylukL','bacakR','bacakL')}   # boş kol sağ kola karşı itilmez: bh_over dönüşünde göğüs ile
# çapraz raket kolu arasında sıkışıp her turda ters yöne itiliyordu (salınım); kol kola değmesi göğse girmekten az göze batar

class Oyuncak:
    """insan.Oyuncu ile aynı arayüz: kur_poz(p) → çizilecek nesneler; .kafa raket kafası, .ao iskelet, .R kemik eşlemesi"""
    def __init__(s,M,stil=None):
        P,F,UW,J=_govde(); K=Kesim(J)
        insan.RAKET_LOD=dict(N=40,bev=1,tel_v=3,v=8)   # performans bütçesi: raket ~0,7k üçgen (eskisi 2,2k); uzak kamerada fark yok
        mats=dict(forma=M['forma'],sort=_mat('oy_sort',(0.025,0.03,0.075),.7),ten=_mat('oy_ten',(0.72,0.45,0.31),.55),
                  serit=_mat('oy_serit',(0.93,0.93,0.92),.55),corap=_mat('oy_corap',(0.94,0.94,0.93),.8),
                  ayakkabi=_mat('oy_ayakkabi',(0.90,0.91,0.92),.45),orta_taban=_mat('oy_orta_taban',(0.97,0.97,0.96),.5),
                  taban=_mat('oy_taban',(0.80,0.10,0.10),.6),sac=_mat('oy_sac',(0.07,0.045,0.03),.7),
                  goz=_mat('oy_goz',(0.95,0.95,0.94),.25),iris=_mat('oy_iris',(0.22,0.12,0.05),.25),bebek=_mat('oy_bebek',(0.015,0.012,0.01),.2),
                  isik=_mat('oy_isik',(1,1,1),.2),kas=_mat('oy_kas',(0.10,0.065,0.045),.8),kirpik=_mat('oy_kirpik',(0.04,0.03,0.025),.8),
                  dudak=_mat('oy_dudak',(0.64,0.35,0.27),.5),bilek=M['bilek'].copy(),bant=M['bant'].copy())
        mats['biye']=mats['serit']   # şort yan şeridi: aynı beyaz (ayrı bölge, şortun kalınlığında)
        s.mats=mats
        # --- gövde ağı (yalnız 'body' grubu), UE ağırlıkları
        fs=F['body']; ix=sorted({i for f in fs for i in f}); yeni={o:n for n,o in enumerate(ix)}
        me=bpy.data.meshes.new('oy_govde'); me.from_pydata([P[i] for i in ix],[],[[yeni[i] for i in f] for f in fs]); me.update()
        govde=bpy.data.objects.new('oy_govde',me); bpy.context.scene.collection.objects.link(govde)
        vgs={}
        for o_,n_ in yeni.items():
            for u,w in UW[o_].items():
                if w<1e-4: continue
                if u not in vgs: vgs[u]=govde.vertex_groups.new(name=u)
                vgs[u].add([n_],w,'REPLACE')
        # yüzü ve elleri koruyan sadeleştirme (koru grubu: 0 = dokunma)
        kg=govde.vertex_groups.new(name='koru'); gm=(J['eye.L____head']+J['eye.R____head'])/2
        # eller de korunur (2026-10-26 yakın plan: 0,17 oranda parmaklar sivri kıymıklara çöküyordu, sap tutuşu okunmuyordu)
        gad={g.index:g.name for g in govde.vertex_groups}; EL_K=('hand_','thumb_','index_','middle_','ring_','pinky_')
        for v in me.vertices:
            yuz=v.co.y>gm.y-.04 and v.co.z>gm.z-.12 and abs(v.co.x)<.075
            kulak=abs(v.co.x)>.07 and gm.z-.06<v.co.z<gm.z+.06 and gm.y-.16<v.co.y<gm.y-.02
            el=sum(g.weight for g in v.groups if gad[g.group].startswith(EL_K))>.5
            kg.add([v.index],E('BS_OY_YUZKORU','.12') if yuz else E('BS_OY_ELKORU','.4') if el else .45 if kulak else 1.,'REPLACE')
        dm=govde.modifiers.new('d','DECIMATE'); dm.ratio=ORAN; dm.vertex_group='koru'; dm.vertex_group_factor=1.; dm.use_symmetry=True; dm.symmetry_axis='X'   # simetrik çökertme: iki yan aynı kenar akışı, ince şerit üçgen azalır
        bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=govde; govde.select_set(True)
        bpy.ops.object.modifier_apply(modifier=dm.name)
        govde.vertex_groups.remove(govde.vertex_groups['koru'])
        print('oyuncak: gövde üçgen', sum(len(p.vertices)-2 for p in me.polygons))
        # --- bölgeler: kes, sınıflandır
        bm=bmesh.new(); bm.from_mesh(me); dl=bm.verts.layers.deform.active
        gidx={g.index:g.name for g in govde.vertex_groups}
        def yuz_grup(f):
            acc={}
            for v in f.verts:
                for gi,w in v[dl].items(): acc[gidx[gi]]=acc.get(gidx[gi],0)+w
            return grup(max(acc,key=acc.get)) if acc else 'govde'
        for flt,fn in K.kesimler():
            gr=flt[:1] if flt[0] in ('kol','bacak') else flt
            if flt[0] in ('kol','bacak'):
                k=flt[1]; sec=[f for f in bm.faces if yuz_grup(f)==flt[0] and (f.calc_center_median().x>0)==(k=='r')]
            else: sec=[f for f in bm.faces if yuz_grup(f) in flt]
            kes(bm,sec,fn)
        sira=['ten','forma','serit','sort','corap','ayakkabi','biye']
        for f in bm.faces: f.material_index=sira.index(K.bolge(f.calc_center_median(),yuz_grup(f)))
        # dudak: ağız çevresindeki ten yüzleri (yüzün önü, göz altında 5-7 cm)
        for f in bm.faces:
            c=f.calc_center_median()
            if f.material_index==0 and abs(c.x)<E('BS_OY_DUDX','.022') and gm.z-E('BS_OY_DUD1','.086')<c.z<gm.z-E('BS_OY_DUD0','.066') and c.y>gm.y-.005 and f.normal.y>.3: f.material_index=len(sira)
        sira.append('dudak')
        # --- ayakkabı ayrı saya (aşağıda): ayağın bu bölgesi silinir (çıplak ayak şişirilince parmaklar ve oluklar
        #     görünüyordu); çorabın alt kenarı sayanın içinde kalır
        ayi=sira.index('ayakkabi')
        aykco=[v.co.copy() for f in bm.faces if f.material_index==ayi for v in f.verts]
        bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index==ayi],context='FACES')
        bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
        bm.normal_update()
        # --- kalınlık ve basamak: sınır kenarları ayrılır, her bölge kendi kalınlığında şişer, basamak köprülenir
        nrm={_anahtar(v.co):v.normal.copy() for v in bm.verts}
        sinir=[]
        for e in bm.edges:
            if len(e.link_faces)!=2: continue
            a,b=e.link_faces
            if a.material_index==b.material_index: continue
            ma,mb=sira[a.material_index],sira[b.material_index]
            if 'dudak' in (ma,mb): continue
            ust=UST.get((ma,mb)) or UST.get((mb,ma))
            if not ust: continue
            cT=(a if ma==ust else b).calc_center_median(); em=(e.verts[0].co+e.verts[1].co)/2
            sinir.append((e,_anahtar(e.verts[0].co),_anahtar(e.verts[1].co),ma,mb,ust,em-cT))
        bmesh.ops.split_edges(bm,edges=[x[0] for x in sinir])
        konum={}; _anahtar_o={}
        for v in bm.verts: k_=_anahtar(v.co); konum.setdefault(k_,[]).append(v); _anahtar_o[v]=k_
        def bolge_v(v):
            ms=[sira[f.material_index] for f in v.link_faces]; return max(ms,key=lambda m: KAT.get(m,0)) if ms else 'ten'
        for v in bm.verts:
            m=bolge_v(v); m='ten' if m=='dudak' else m
            t=_kat(m,v.co,K)
            if t: v.co=v.co+nrm.get(_anahtar(v.co),v.normal)*t
        kopru=0; hata={}
        for e,ka,kb,ma,mb,ust,yon in sinir:
            def kenar(m):
                # ayrılmış kenar: uçları (ka,kb) anahtarında, tek yüzü m bölgesinde (köşe aramak yanlış eşi buluyordu)
                for v in konum.get(ka,[]):
                    for ed in v.link_edges:
                        w=ed.other_vert(v)
                        if _anahtar_o.get(w)==kb and len(ed.link_faces)==1 and sira[ed.link_faces[0].material_index]==m: return v,w
                return None,None
            a1,b1=kenar(ma); a2,b2=kenar(mb)
            if a1 is None and a2 is None: continue   # ayrılmamış kenar: boşluk yok
            if a1 is None or a2 is None:   # bir yan ayrılmamış (halka ucu): ortak kenarın köşeleriyle üçgen
                hata['tek']=hata.get('tek',0)+1
                x,y=(a2,b2) if a1 is None else (a1,b1)
                ref=[v for v in konum.get(ka,[]) if v is not x]+[v for v in konum.get(kb,[]) if v is not y]
                if a1 is None: a1,b1=(ref[0] if ref else x),(ref[-1] if ref else y)
                else: a2,b2=(ref[0] if ref else x),(ref[-1] if ref else y)
            # yön: ma yanındaki komşu yüz a1→b1 kenarını hangi yönde geziyorsa köprü tersinden gezer (tutarlı normal)
            ileri=False
            for lf in a1.link_faces:
                vs=list(lf.verts)
                if b1 in vs:
                    i=vs.index(a1); ileri=vs[(i+1)%len(vs)] is b1; break
            q=(b1,a1,a2,b2) if ileri else (a1,b1,b2,a2)
            q=tuple(dict.fromkeys(q))   # bir ucu ortak köşe: üçgen (kalınlık sıfıra inen uç)
            try: f=bm.faces.new(q)
            except ValueError: continue
            f.material_index=sira.index(ust); f.smooth=True; kopru+=1
            # yön denetimi: duvar kalın bölgeden ince bölgeye bakar (ters normal kol ağzında kara diş gibi görünüyordu)
            f.normal_update()
            if f.normal.dot(yon)<0: f.normal_flip()
        acik=sum(1 for e in bm.edges if e.is_boundary and sira[e.link_faces[0].material_index] not in ('corap','ten','dudak'))
        print('oyuncak: sınır kenarı',len(sinir),'köprü',kopru,hata,'kıyafette açık kenar',acik)
        me.materials.clear()
        for k in sira: me.materials.append(mats[k])
        bm.to_mesh(me); bm.free()
        for p in me.polygons: p.use_smooth=True
        # --- yüz: göz küreleri, kirpik, kaş
        parca=[govde]
        hb=BVHTree.FromPolygons([P[i] for i in range(len(P))],[f for f in F['body'] if min(UW[i].get('head',0) for i in f)>.5])
        for mh in ('L','R'):
            g=[P[i] for f in F.get(f'helper-{mh.lower()}-eye',[]) for i in f]
            c=sum(g,Vector())/len(g); r=max((p-c).length for p in g)*.97
            o=_goz('oy_goz'+mh,c,r,mats); parca.append(o)
            # ışık noktası
            sx=1 if mh=='R' else -1
            # ışık: küre yerine öne bakan altıgen disk (80 → 4 üçgen; yalnız önden görünür)
            bpy.ops.mesh.primitive_circle_add(vertices=6,fill_type='NGON',radius=r*.17,location=c+Vector((sx*.22*r,1.005*r,.30*r)),rotation=(-math.pi/2+.3,0,0))
            ik=bpy.context.object; ik.name='oy_isik'+mh; ik.data.materials.append(mats['isik']); ik['tag']='isik'; bpy.ops.object.shade_smooth(); parca.append(ik)
            # üst kirpik: göz kapağı kenarı boyunca ince koyu şerit (yüze oturur)
            xs=[c.x+sx*r*t for t in (-1.05,-.7,-.35,0,.35,.7,1.1)]
            zs=[c.z+r*(.50+.20*(1-((t)/1.05)**2))+(.12*r if (sx*t)>.7 else 0) for t in (-1.05,-.7,-.35,0,.35,.7,1.1)]
            parca.append(_serit(hb,list(zip(xs,zs)),ymin=c.y,gen=[r*.10,r*.16,r*.18,r*.18,r*.18,r*.17,r*.12],kal=.0013,ad='oy_kirpik'+mh,mat=mats['kirpik'],tag='kirpik'))
            # kaş: iç uç alçak ve kalın, dışa doğru yay ve incelir
            gd=(J['eye.L____head']-J['eye.R____head']).length
            us=[.16,.30,.45,.60,.75,.90]; ks=[]
            for u in us:
                t=(u-.16)/.74; ks.append((sx*u*gd,gm.z+gd*(.40+.20*t-.16*t*t)))
            parca.append(_serit(hb,ks,[gd*w*E('BS_OY_KASK','.85') for w in (.085,.095,.09,.08,.065,.045)],.0022,'oy_kas'+mh,mats['kas'],'kas',ymin=c.y))
        sac,gm,gd,hfn=_sac(P,F,UW,J,mats['sac']); parca.append(sac)
        # kafa bandı: saç çizgisinin üstünde, saç kabuğunun kesitine oturan elips
        # bant düzlemi alında kaşların üstünde, arkada biraz alçak; kesit saç + deri noktalarından açı açı ölçülür
        ez=Vector((0,-.30,1)).normalized(); cx=Vector((0,gm.y-.085,gm.z+E('BS_OY_BANT','.050')))
        pts=[v.co.copy() for v in sac.data.vertices]+[P[i] for f in F['body'] for i in f if UW[i].get('head',0)>.8]
        bant=_halka_r('oy_bant',cx,ez,pts,.024,.0042,mats['bant'],'bant',n=32); parca.append(bant)
        # bileklik: sağ ön kolun bileğe yakın kesiti
        ar,aw=J['lowerarm01.R____head'],J['wrist.R____head']; d=(aw-ar); dn=d.normalized(); c0=aw-dn*.045
        cc=c0   # kemik ekseni (sadeleştirilmiş ağda dilimin köşe ortalaması boş kalabiliyordu)
        bil=_halka_r('oy_bilek',cc,dn,[v.co.copy() for v in me.vertices if (v.co-c0).length<.08],.042,.006,mats['bilek'],'bilek',n=20,pay=.0015); parca.append(bil)
        # taban
        tabanlar=[]
        aykn={}
        for k in 'rl':
            bal=J[f'toe1-1.{"R" if k=="r" else "L"}____head']
            aykn[k]=_ayakkabi('oy_ayk_'+k,[c for c in aykco if (c.x>0)==(k=='r')],bal,K.an[k],mats['ayakkabi'],mats['taban']); parca.append(aykn[k])
        aykco=[aykn[k].matrix_world@v.co for k in 'rl' for v in aykn[k].data.vertices]
        for k in 'rl':
            o,alt=_taban('oy_taban_'+k,aykco,K,k,mats,1); tabanlar.append(o); parca.append(o)
        s.taban_alt=alt
        # --- iskelet (UE adları, MakeHuman eklemlerinde)
        arm=bpy.data.armatures.new('oy_isk'); ao=bpy.data.objects.new('oy_isk',arm); bpy.context.scene.collection.objects.link(ao)
        bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=ao; ao.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
        eb=arm.edit_bones; jh=lambda k: J[k+'____head'].copy(); jt=lambda k: J[k+'____tail'].copy()
        def kemik(n,h,t,par=None):
            b=eb.new(n); b.head=h; b.tail=t if (t-h).length>1e-4 else h+Vector((0,0,.01))
            if par: b.parent=eb[par]; b.use_connect=False
            return b
        hipc=(jh('upperleg01.R')+jh('upperleg01.L'))/2
        kemik('root',Vector((0,0,0)),Vector((0,.1,0)))
        kemik('pelvis',hipc+Vector((0,0,.02)),jh('spine04'),'root')
        kemik('spine_01',jh('spine04'),jh('spine03'),'pelvis'); kemik('spine_02',jh('spine03'),jh('spine02'),'spine_01')
        kemik('spine_03',jh('spine02'),jh('neck01'),'spine_02'); kemik('neck_01',jh('neck01'),jh('head'),'spine_03')
        kemik('head',jh('head'),jt('head'),'neck_01')
        for k,m in (('r','R'),('l','L')):
            kemik(f'clavicle_{k}',jh(f'clavicle.{m}'),jh(f'upperarm01.{m}'),'spine_03')
            kemik(f'upperarm_{k}',jh(f'upperarm01.{m}'),jh(f'lowerarm01.{m}'),f'clavicle_{k}')
            kemik(f'lowerarm_{k}',jh(f'lowerarm01.{m}'),jh(f'wrist.{m}'),f'upperarm_{k}')
            kemik(f'hand_{k}',jh(f'wrist.{m}'),jh(f'finger3-1.{m}'),f'lowerarm_{k}')
            for fi,fn in enumerate(('thumb','index','middle','ring','pinky')):
                kemik(f'{fn}_01_{k}',jh(f'finger{fi+1}-1.{m}'),jt(f'finger{fi+1}-1.{m}'),f'hand_{k}')
                kemik(f'{fn}_02_{k}',jh(f'finger{fi+1}-2.{m}'),jt(f'finger{fi+1}-3.{m}'),f'{fn}_01_{k}')
            kemik(f'thigh_{k}',jh(f'upperleg01.{m}'),jh(f'lowerleg01.{m}'),'pelvis')
            kemik(f'calf_{k}',jh(f'lowerleg01.{m}'),jh(f'foot.{m}'),f'thigh_{k}')
            tb=sum((jh(f'toe{t}-1.{m}') for t in range(1,6)),Vector())/5
            # ball kemiğinin başı taban altından 3,66 cm (gerçek) yukarıda: Unity PlayerView.Zemin en alçak ayak kemiğini
            # zeminin 3,5 cm üstüne koyar (1,75 ölçekte 2,0 birim = 3,66 cm gerçek); boşta düz basan taban böylece zemine oturur
            bh=Vector((tb.x,tb.y,alt+.035/1.75/((.49)/((jh(f'lowerleg01.{m}')-jh(f'upperleg01.{m}')).length+(jh(f'foot.{m}')-jh(f'lowerleg01.{m}')).length))))
            kemik(f'foot_{k}',jh(f'foot.{m}'),bh,f'calf_{k}')
            tt=sum((jt(f'toe{t}-1.{m}') for t in range(1,6)),Vector())/5
            kemik(f'ball_{k}',bh,Vector((tt.x,tt.y+.02,bh.z)),f'foot_{k}')
        bpy.ops.object.mode_set(mode='OBJECT')
        for b in arm.bones: b.use_deform=b.name!='root'
        # avuç normali (dinlenme): işaret ve serçe kökü düzlemi, başparmağın olduğu yana
        avuc={}
        for k,m in (('r','R'),('l','L')):
            w=jh(f'wrist.{m}'); a=jh(f'finger2-1.{m}')-w; b=jh(f'finger5-1.{m}')-w; n=a.cross(b).normalized()
            th=jt(f'finger1-3.{m}')-w
            if n.dot(th)<0: n=-n
            avuc[k]=n
        # deri: gövde ağırlıkları hazır; diğer parçalar tek kemik
        for o in parca:
            if o is govde: continue
            n=o.name
            if n.startswith('oy_bilek'): _tek(o,'lowerarm_r')
            elif n.startswith(('oy_taban_','oy_ayk_')):
                k=n[-1]; vg=o.vertex_groups.new(name=f'foot_{k}'); vb=o.vertex_groups.new(name=f'ball_{k}'); by=ao.data.bones[f'ball_{k}'].head_local.y
                for v in o.data.vertices:
                    u=_ss(by-.015,by+.02,v.co.y); vg.add([v.index],1-u,'REPLACE'); vb.add([v.index],u,'REPLACE')
            else: _tek(o,'head')
        for o in parca:
            md=o.modifiers.new('isk','ARMATURE'); md.object=ao
        # sol el parmakları kemik değil, dinlenme ağına hafif kapalı pişirilir (bütçe: 43 → 33 kemik, FBX kemik başına
        # ~0,1 MB). Sol el yalnız servis topunu tutar; boğum hareketi uzak kamerada okunmuyordu. Kapanma uygula()'daki
        # sol el değeriyle aynı (68°·0,35), görünüm değişmez. BS_OY_SOLPARMAK=1 eski 10 kemiği geri getirir.
        if E('BS_OY_SOLPARMAK','0')<.5:
            pb=ao.pose.bones; ac=math.radians(E('BS_OY_PARMAK','58')); n_=avuc['l']; L=bpy.context.view_layer
            sol=[f'{f}_{j}_l' for f in ('thumb','index','middle','ring','pinky') for j in ('01','02')]
            for n in sol:
                f=n.split('_')[0]; pay=.85 if '_01_' in n else 1.55
                bn=ao.data.bones[n]; d=(bn.tail_local-bn.head_local).normalized(); ax=d.cross(n_)
                if ax.length<1e-4: continue
                ax=(bn.matrix_local.to_3x3().inverted()@ax.normalized()).normalized()
                q=pb[n]; q.rotation_mode='QUATERNION'; q.rotation_quaternion=Quaternion(ax,ac*.35*pay*(.5 if f=='thumb' else 1))
            L.update(); dg=bpy.context.evaluated_depsgraph_get()
            for o in parca:
                if not any(g.name in sol for g in o.vertex_groups): continue
                ev=o.evaluated_get(dg); em=ev.to_mesh(); co=[v.co.copy() for v in em.vertices]; ev.to_mesh_clear()
                gi={g.index:g.name for g in o.vertex_groups}; hl=o.vertex_groups.get('hand_l') or o.vertex_groups.new(name='hand_l')
                for v,c in zip(o.data.vertices,co):
                    w=sum(g.weight for g in v.groups if gi[g.group] in sol)
                    if w>0: v.co=c; hl.add([v.index],w+sum(g.weight for g in v.groups if gi[g.group]=='hand_l'),'REPLACE')
                for n in sol:
                    if n in o.vertex_groups: o.vertex_groups.remove(o.vertex_groups[n])
            for b in pb: b.matrix_basis=Matrix()
            bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active=ao; ao.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
            for n in sol: arm.edit_bones.remove(arm.edit_bones[n])
            bpy.ops.object.mode_set(mode='OBJECT')
        ag=[v for v in me.vertices if not any(g.weight>1e-4 for g in v.groups)]
        print('oyuncak: ağırlıksız köşe',len(ag),'/',len(me.vertices))
        # --- ölçek: bacak 0,49 (insan.Oyuncu/aday/sade ile aynı)
        bn=ao.data.bones; bacak=(bn['calf_r'].head_local-bn['thigh_r'].head_local).length+(bn['foot_r'].head_local-bn['calf_r'].head_local).length
        olc=E('BS_OY_OLC',.49/bacak); s.olc=olc
        D=Matrix.Scale(olc,4)
        for o in [ao]+parca: o.matrix_world=D@o.matrix_world
        bpy.context.view_layer.update()
        bpy.ops.object.select_all(action='DESELECT')
        for o in [ao]+parca: o.select_set(True)
        bpy.context.view_layer.objects.active=ao
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for o in parca: o.modifiers['isk'].object=ao
        s.avuc=avuc; s.taban_z=alt*olc
        # Performans bütçesi (2026-10-26, yasin: "her telefonda akıcı"; ekranda 4 figür): sabit renkli her şey TEK derili
        # ağda, TEK malzeme + renk atlası; Unity PlayerView.DokuMalzeme adı "body" içeren malzemeye Resources/karakter/
        # oyuncak_govde dokusunu bağlar (C# değişmeden). Ayrı kalanlar yalnız Unity'nin boyadığı/gizlediği etiketler:
        # forma (takım rengi), bant ve bilek (ekipman; "Yok" seçilince renderer kapanır, tek ağda gizlenemez).
        # Şort atlasta lacivert: hakemin "__sort" boyası zaten aynı lacivert, ayrı parça gerekmedi.
        for o in list(bpy.context.scene.objects):
            if o.type!='MESH' or not o.name.startswith('oy_'): continue
            if len({f.material_index for f in o.data.polygons})>1:
                bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
                bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.separate(type='MATERIAL'); bpy.ops.object.mode_set(mode='OBJECT')
        AYRI=('forma','bant','bilek')
        parcalar=[]
        for o in list(bpy.context.scene.objects):
            if o.type!='MESH' or not o.name.startswith('oy_'): continue
            if not o.data.polygons: bpy.data.objects.remove(o); continue
            used={f.material_index for f in o.data.polygons}; m=o.data.materials[min(used)]
            o.data.materials.clear(); o.data.materials.append(m)
            for f in o.data.polygons: f.material_index=0
            o['_k']=next(k for k,v in mats.items() if v==m); parcalar.append(o)
        # atlas: 8×8 hücre, 512 px (hücre 64 px: mip zinciri 3. seviyeye dek komşu renge taşmaz); her yüz rengini hücre
        # ortasından okur. Renkler Blender'da doğrusal, PNG sRGB kodlu (Unity dokuyu sRGB içe alır).
        renk=[]
        for o in parcalar:
            if o['_k'] in AYRI: continue
            c=tuple(round(x,4) for x in o.data.materials[0].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value[:3])
            if c not in renk: renk.append(c)
        N_=8; PX=512; assert len(renk)<=N_*N_
        def srgb(x): return 12.92*x if x<=.0031308 else 1.055*x**(1/2.4)-.055
        img=bpy.data.images.new('oyuncak_govde',PX,PX,alpha=False); pix=[0.]*(PX*PX*4); cel=PX//N_
        for i_,c in enumerate(renk):
            cx_,cy_=i_%N_,i_//N_; e=[srgb(x) for x in c]
            for yy in range(cy_*cel,(cy_+1)*cel):
                for xx in range(cx_*cel,(cx_+1)*cel): k_=(yy*PX+xx)*4; pix[k_:k_+4]=[*e,1.]
        img.pixels=pix   # renk uzayı ataması üretilmiş görüntüyü sıfırlar: varsayılan (sRGB bayt) bırakılır
        args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
        hedef_d=args[0] if args and os.path.isdir(args[0]) else bpy.app.tempdir
        img.filepath_raw=os.path.join(hedef_d,'oyuncak_govde.png'); img.file_format='PNG'; img.save(); img.pack(); s.atlas=img.filepath_raw
        print('oyuncak: atlas',len(renk),'renk →',img.filepath_raw)
        body=bpy.data.materials.new('oy_body'); body.use_nodes=True; nt=body.node_tree; bs=nt.nodes['Principled BSDF']
        tx=nt.nodes.new('ShaderNodeTexImage'); tx.image=img; tx.interpolation='Closest'; nt.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
        bs.inputs['Roughness'].default_value=.6; body.diffuse_color=(.7,.5,.4,1)
        gov=[]
        for o in parcalar:
            if o['_k'] in AYRI: continue
            c=tuple(round(x,4) for x in o.data.materials[0].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value[:3]); i_=renk.index(c)
            uv=((i_%N_+.5)/N_,(i_//N_+.5)/N_)
            ul=o.data.uv_layers[0] if o.data.uv_layers else o.data.uv_layers.new(name='UVMap')
            for l in ul.data: l.uv=uv
            o.data.materials[0]=body; gov.append(o)
        ayri=[o for o in parcalar if o['_k'] in AYRI]   # join birleşenleri siler: önce ayır
        for o in gov:   # join için tek ad: UV katmanı aynı adda olmalı
            o.data.uv_layers[0].name='UVMap'
        bpy.ops.object.select_all(action='DESELECT')
        for o in gov: o.select_set(True)
        ana=next(o for o in gov if o['_k']=='ten'); bpy.context.view_layer.objects.active=ana; bpy.ops.object.join()
        ana.name='oy_govde'; ana.data.name='oy_govde'; ana['tag']='govde'
        son=[ana]
        for o in ayri:
            o.name='oy_'+o['_k']; o.data.name=o.name; o['tag']=o['_k']; son.append(o)
        for o in son:
            if '_k' in o: del o['_k']
        s.kalici=son
        tri=sum(len(p.vertices)-2 for o in son for p in o.data.polygons)
        print('oyuncak: parçalar',sorted((o.name,o['tag'],sum(len(p.vertices)-2 for p in o.data.polygons)) for o in son),'üçgen',tri,'ölçek',round(olc,4),
              'kemik',sum(1 for b in ao.data.bones),'malzeme',len({m for o in son for m in o.data.materials}))
        # zemin: bilek yüksekliği (taban altından) ölçülür; hedef() basan bileği 0,09·k'ya koyar (insan.Iskelet), fark kaydırılır
        k_=(bn['calf_r'].head_local-bn['thigh_r'].head_local).length+(bn['foot_r'].head_local-bn['calf_r'].head_local).length
        k_=k_/(insan.TOY['TH']+insan.TOY['SH'])
        h_ank=bn['foot_r'].head_local.z-s.taban_z
        dz=E('BS_OY_DZ',h_ank-.09*k_+.004)
        s.M=M; s.I=OyIskelet(ao,avuc,dz); s.ao=ao; s.R=R_UE; s.gecici=[]; s.boy=1.0; s.stil={}
        # raket örnek noktaları (yerel çerçevede bir kez): çerçeve, şaft, sap; teller çerçevenin içinde kalır
        # raket ölçeği (2026-10-26): s=1,0 raketi 0,675 birim = oyunda 1,18 m yapıyordu (1,72 m oyuncunun yanında; serviste ve
        # alçak vuruşta kafa zemine 10-20 cm giriyordu). Gerçekçi oranlı gövdede 0,8 (~94 cm): gerçeğinden (67,5 cm) hâlâ iri,
        # yüksek kameradan okunur. unity_fbx.py OY.raket_s'yi okur; diğer modeller 1,0.
        s.raket_s=E('BS_OY_RAKET','.8')
        nes,_=insan.raket(Vector((0,0,0)),Vector((0,1,0)),Vector((0,0,1)),s.raket_s,M,ad='rk_ornek')
        ry=[]
        for o in nes:
            if o.get('tag') in ('cerceve','koyu','grip'):
                vs=[o.matrix_world@v.co for v in o.data.vertices]; ry+=vs[::3] if o.get('tag')=='cerceve' else vs
        for o in nes: bpy.data.objects.remove(o)
        s.C=Carpisma(ao,son,ry); s.cozum={}; s.ara_kare=True   # unity_fbx ara kareleri de kurar (Carpisma notu)
        print('oyuncak: bilek-taban',round(h_ank,4),'dz',round(dz,4))
    def _avuc(s,k):
        """sap ekseni: el kemiği başından orta parmak köküne %55, avuç yönünde (yarım el kalınlığı + sap yarıçapı)"""
        pb=s.ao.pose.bones; h=pb[f'hand_{k}']; p=h.head+(h.tail-h.head)*.55   # el kemiğinin ucu = orta parmak kökü (sol parmak kemiği yok)
        av=(h.matrix.to_3x3()@s.I._avuc[f'hand_{k}']).normalized()
        return p+av*E('BS_OY_AVUC','.02')
    def coz(s,T,tur=20):
        """iç içe geçmeyi hedefleri iterek giderir (yukarıdaki Carpisma notu); s.cozum son pozun kalan derinliklerini tutar"""
        C=s.C; I=s.I; pb=s.ao.pose.bones; mw=s.ao.matrix_world; TOL,PAY=C.TOL,C.PAY
        def kol_ik(K,wr,el_ipucu):
            wr=sinirla(wr,wr0[K]); sh=T['sh'+K]; el,wr=insan._ik(sh,wr,I.UA,I.FA,el_ipucu-(sh+wr)/2); T['el'+K]=el; T['wr'+K]=wr
        def bacak_ik(K,an,kn_it=None):
            an=sinirla(an,an0[K]) if an.z<=an0[K].z+1e-6 else an   # zemin kaldırması sınırsız (yukarı)
            hp=T['hp'+K]; kn0=T['kn'+K]+(kn_it if kn_it is not None else Vector())
            kn,an=insan._ik(hp,an,I.TH,I.SH,kn0-(hp+an)/2); T['kn'+K]=kn; T['an'+K]=an
        # sınırlar (ilk sürümde sap ucu avuca 2 cm'de gövdeye değince atan(d/r) dev açı veriyor, yinelemeyle raket smaçta
        # ters dönüyordu; ters raket ön kolu da başın içine çekiyordu): raket adımda en çok 6°, toplam 25°; avuca 10 cm'den
        # yakın raket noktası sayılmaz (sapın gövdeye değmesi el/ön kol testinde); bilek/ayak bileği hedefi en çok 7 cm itilir.
        rk0=T['rk'].copy(); wr0={K:T['wr'+K].copy() for K in 'RL'}; an0={K:T['an'+K].copy() for K in 'RL'}
        MAXI=E('BS_OY_MAXIT','.07')
        def sinirla(v,v0):
            d=v-v0; return v0+d*(MAXI/d.length) if d.length>MAXI else v
        kalan={}
        for it in range(tur):
            C.degerlendir(); degisti=False; kalan={}
            for K,k in (('R','r'),('L','l')):
                ua=(mw@pb['upperarm_'+k].head,mw@pb['upperarm_'+k].tail)
                hed=C.bvh(ARM_HEDEF[K])
                for kat,agir in (('el'+K,1.),('onkol'+K,.8),('kol'+K,0.)):
                    d,n,q=C.en_derin(hed,C.noktalar(kat,ua if kat.startswith('kol') else None))
                    kalan[kat]=d
                    if os.environ.get('BS_OY_IZ') and d>TOL: print('İZ',it,kat,round(d*175,1),[round(x,2) for x in n])
                    if d>TOL:
                        # gövdenin ortasından geçen el/kol (bh_over dönüş ara karesi): en yakın yüz her turda ön/arka arasında
                        # değişip itme salınıyordu; normal omurgaya doğru bakıyorsa omurga ekseninden (kök→boyun) dışa it
                        a0,a1=T['root'],T['neck']; ax=a1-a0; t_=max(0.,min(1.,(q-a0).dot(ax)/ax.length_squared)); rd=q-(a0+ax*t_); rd.z=0
                        if rd.length>1e-4 and n.dot(rd)<0: n=rd.normalized()
                        it_=n*(d+PAY)
                        if agir>0: kol_ik(K,T['wr'+K]+it_*agir/max(agir,.5),T['el'+K]+it_*(1-agir+.3))
                        else: kol_ik(K,T['wr'+K]+it_*.5,T['el'+K]+it_*1.6)
                        degisti=True; break
                # baldır/ayak öbür bacağa
                O='L' if K=='R' else 'R'
                d,n,q=C.en_derin(C.bvh(('bacak'+O,'uyluk'+O)),C.noktalar('bacak'+K)); kalan['bacak'+K]=d
                if d>TOL:
                    n=Vector((n.x,n.y,0)); n=n.normalized() if n.length>1e-4 else Vector((1 if K=='R' else -1,0,0))
                    ay=T['an'+K]-T['an'+O]; ay.z=0   # öbür bacağa doğru bakan normal: ayak bileklerini ayıran yönde it (salınım)
                    if ay.length>1e-4 and n.dot(ay)<0: n=ay.normalized()
                    if os.environ.get('BS_OY_IZ'): print('İZB',it,K,round(d*175,1),[round(x,2) for x in n],[round(x,3) for x in T['an'+K]],[round(x,3) for x in T['an'+O]])
                    bacak_ik(K,T['an'+K]+n*(d+PAY),n*(d+PAY)*1.5); degisti=True   # diz de itilir (çapraz adımda diz öbür uyluğa değiyor)
                # taban zemine
                zmin=min(p.z for p in C.noktalar('bacak'+K)); kalan['zemin'+K]=-zmin
                if zmin< -TOL: bacak_ik(K,T['an'+K]+Vector((0,0,-zmin+.001))); degisti=True
            # raket: gövdeye ve zemine (sağ el ve ön kol hariç: sap avuçta)
            if not degisti:
                I.uygula(T); C.degerlendir(); palm=mw@s._avuc('r')
                tum=C.raket_dunya(palm,T['rk'],T['rkFace']); pts=[q for q in tum if (q-palm).length>.10]
                # sap ve şaft dibi (avuca 10 cm içi): döndürmek elin içinde döndürmek olur; bilek dışarı itilir
                hed=C.bvh(('govde','bas','uylukR','uylukL','bacakR','bacakL','kolL','onkolL','elL'))
                d2,n2,q2=C.en_derin(hed,[q for q in tum if (q-palm).length<=.10]); kalan['raket_sap']=d2
                if d2>TOL:
                    kol_ik('R',T['wrR']+n2*(d2+PAY),T['elR']+n2*(d2+PAY)*.5); I.uygula(T); continue
                d,n,q=C.en_derin(C.bvh(('govde','bas','uylukR','uylukL','bacakR','bacakL','kolL','onkolL','elL','kolR')),pts)
                zq=min(pts,key=lambda v:v.z)
                if -zq.z>d: d,n,q=-zq.z,Vector((0,0,1)),zq
                kalan['raket']=d
                if d>TOL:
                    r=q-palm; ax=r.cross(n)
                    if ax.length>1e-6:
                        a_=min(math.atan2(d+PAY,r.length),math.radians(6)); Rm=Matrix.Rotation(a_,3,ax.normalized())
                        yeni=(Rm@T['rk']).normalized()
                        if yeni.angle(rk0)<=math.radians(25): T['rk']=yeni; T['rkFace']=Rm@Vector(T['rkFace']); degisti=True
                        elif n.z>.9 and (T['wr'+'R']-wr0['R']).length<MAXI-.002:   # dönüş sınırında zemin: bilek yukarı
                            kol_ik('R',T['wrR']+Vector((0,0,min(d+PAY,.02))),T['elR']); degisti=True
            if not degisti: break
            I.uygula(T)
        s.cozum=kalan
        return T
    def kur_poz(s,p):
        for o in s.gecici: bpy.data.objects.remove(o)
        s.gecici=[]; T=s.I.hedef(p); s.I.uygula(T)
        if E('BS_OY_COZ','1')>.5: T=s.coz(T)
        s.T=T
        mw=s.ao.matrix_world; palm=mw@s._avuc('r')
        nes,s.kafa=insan.raket(palm,T['rk'],T['rkFace'],s.raket_s,s.M); s.gecici+=nes
        if p.get('shuttle'):   # servis: sol elde tüy top (sade.py ile aynı ölçü)
            k=.85; c=mw@s._avuc('l')+Vector((0,.01,.012))
            bpy.ops.mesh.primitive_uv_sphere_add(segments=10,ring_count=6,radius=.0125*k*1.6,location=c); m_=bpy.context.object; m_.data.materials.append(s.M['mantar']); m_['tag']='mantar'   # mantar 10×6 (varsayılan 32×16: 960 → 100 üçgen)
            bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=.012*k*1.6,radius2=.034*k*1.6,depth=.065*k*1.6,end_fill_type='NOTHING',location=c+Vector((0,0,.035*k*1.6))); t=bpy.context.object; t.data.materials.append(s.M['tuy']); t['tag']='tuy'
            s.gecici+=[m_,t]
        return s.kalici+s.gecici
