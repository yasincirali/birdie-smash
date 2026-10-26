# Sade oyuncu (yasin 2026-10-26): oyuncak modelin iri başı yüksek oyun kamerasından gövdeyi örtüyor, ayrıntı az okunuyordu;
# "daha sade, normal oranlı bir insan". Gövde: Mesh2Motion mankeni (model-human.glb, CC0; static/models) ve aynı paketin
# UE adlı iskeleti (rig-human.glb: pelvis, spine_01..03, neck_01, head, clavicle/upperarm/lowerarm/hand_l|r, parmaklar,
# thigh/calf/foot/ball_l|r). Kemik adları standart kalır ki Unity tarafı (baş, omuz, ayak) bulabilsin. Paketin hareketleri
# kullanılmaz: aynı badminton poz sözlükleri (karakter.py) bu iskelete eklem hedefi olarak uygulanır; raket hep sağ elde.
# Kurulum: tool/bulut_kurulum.sh Mesh2Motion'ı ~/AnimationLibrary/mesh2motion'a koyar (BS_M2M ile başka klasör).
# Görünüm: düz renkli bölgeler, doku yok. Manken kafası yüzsüz ve küçük; yüksek kameradan forma, şort, kol ve bacak okunur.
# Bölgeler parça adı etiketiyle (unity_fbx.py "<ad>__<etiket>"): forma (gövde + kısa kollar, oyun takım rengine boyar),
# sort, ten (kol, bacak, boyun, baş), beyaz (çorap), taban (ayakkabı), sac (kısa saç kabuğu), bilek/bant (ekipman).
import bpy, bmesh, math, os
from mathutils import Vector, Matrix, Quaternion
import insan, aday
M2M=os.environ.get('BS_M2M',os.path.expanduser('~/AnimationLibrary/mesh2motion/mesh2motion-app-main/static'))
GOVDE=os.environ.get('BS_SADE_GOVDE',os.path.join(M2M,'models','model-human.glb'))
RIG=os.environ.get('BS_SADE_RIG',os.path.join(M2M,'rigs','rig-human.glb'))
R_UE=aday.RIG['ue']

# Kesim düzlemleri paketin metre biriminde, T-pozunda (yüz -y, sağ kol -x; döndürmeden önce). Yüz yüz boyamak alçak
# poligonlu mankende testere dişi sınır veriyordu: ağ önce bu düzlemlerden kesilir (bisect), sınır düz bir halka olur.
KOL=float(os.environ.get('BS_SADE_KOL','.35'))     # kısa kol ucu |x| (omuz .17, dirsek .47): üst kolun yarısı
BEL=float(os.environ.get('BS_SADE_BEL','1.00'))    # forma alt kenarı (kemer plakası .98–1.07)
SORT_ALT=float(os.environ.get('BS_SADE_SORT','.64'))   # şort paçası (diz .53): dizin 11 cm üstü
CORAP=float(os.environ.get('BS_SADE_CORAP','.20'))     # çorap üstü (bilek .10)
AYK=float(os.environ.get('BS_SADE_AYK','.115'))        # ayakkabı ağzı
def yaka(y): return 1.535+.30*y   # yaka düzlemi: önde (y -.10) 1,505, arkada (y .08) 1,56; omuz üstü (trapez) formada kalır

def bolge(c):
    """T-pozunda yüz merkezi → bölge"""
    x,y,z=c; ax=abs(x)
    if z>1.25 and ax>KOL: return 'ten'   # ön kol, el, parmaklar
    if z>yaka(y) and ax<.11: return 'ten'   # boyun ve baş
    if z>=BEL: return 'forma'
    if z>=SORT_ALT: return 'sort'
    if z>=CORAP: return 'ten'
    if z>=AYK: return 'beyaz'
    return 'taban'

def _mat(n,c,r=.6):
    m=bpy.data.materials.new(n); m.use_nodes=True; b=m.node_tree.nodes['Principled BSDF']; b.inputs['Base Color'].default_value=(*c,1); b.inputs['Roughness'].default_value=r; return m

def _kes(me):
    """kesim düzlemleri + ağ kaynağı (glTF içe aktarımı UV dikişinde köşe ayırır: deri ve yumuşatma iki yanı ayrı sanıyordu)"""
    bm=bmesh.new(); bm.from_mesh(me); n0=len(bm.verts)
    bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=1e-6); n1=len(bm.verts)
    duz=[((0,0,BEL),(0,0,1)),((0,0,SORT_ALT),(0,0,1)),((0,0,CORAP),(0,0,1)),((0,0,AYK),(0,0,1)),((KOL,0,0),(1,0,0)),((-KOL,0,0),(1,0,0)),
         ((0,0,yaka(0)),(0,-.30,1)),((.11,0,0),(1,0,0)),((-.11,0,0),(1,0,0))]
    for co,no in duz:
        g=bm.verts[:]+bm.edges[:]+bm.faces[:]; bmesh.ops.bisect_plane(bm,geom=g,plane_co=co,plane_no=Vector(no).normalized(),dist=1e-5)
    bm.to_mesh(me); print('sade: kaynak',n0,'→',n1,'köşe; kesim sonrası',len(bm.verts)); bm.free()

def _sac(govde,mats):
    """kısa saç: başın üst/arka yüzeyi kopyalanır, normal boyunca 7 mm şişirilir, kalınlık verilir (alt kenar kapalı görünür).
    Saç çizgisi eğik düzlem: alında 1,765, ensede 1,615 (kulak hizası yanlarda)"""
    bm=bmesh.new(); bm.from_mesh(govde.data); bm.verts.ensure_lookup_table()
    alin=lambda v: v.co.z-(1.69-.68*v.co.y)
    sil=[f for f in bm.faces if not (f.calc_center_median().z>1.57 and abs(f.calc_center_median().x)<.11)]
    bmesh.ops.delete(bm,geom=sil,context='FACES')
    g=bm.verts[:]+bm.edges[:]+bm.faces[:]; r=bmesh.ops.bisect_plane(bm,geom=g,plane_co=(0,0,1.69),plane_no=Vector((0,.68,1)).normalized(),dist=1e-5,clear_inner=True)
    bm.normal_update()
    for v in bm.verts: v.co+=v.normal*.007
    me=bpy.data.meshes.new('sade_sac'); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new('sade_sac',me); bpy.context.scene.collection.objects.link(o)
    me.materials.clear(); me.materials.append(mats['sac'])
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    sm=o.modifiers.new('k','SOLIDIFY'); sm.thickness=.006; sm.offset=-1; bpy.ops.object.modifier_apply(modifier=sm.name)
    bpy.ops.object.shade_smooth(); return o

def _halka(ad,merkez,eksen,r,boy,mat,major=None):
    if major:   # torus: kafa bandı
        bpy.ops.mesh.primitive_torus_add(major_radius=major[0],minor_radius=major[1],major_segments=48,minor_segments=10,location=merkez)
    else: bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=r,depth=boy,location=merkez)
    h=bpy.context.object; h.name=ad; h.rotation_mode='QUATERNION'; h.rotation_quaternion=Vector((0,0,1)).rotation_difference(Vector(eksen).normalized())
    bpy.ops.object.transform_apply(rotation=True,scale=True); bpy.ops.object.shade_smooth(); h.data.materials.append(mat); return h

def _tek_kemik(o,ao,kemik):
    vg=o.vertex_groups.new(name=kemik); vg.add(list(range(len(o.data.vertices))),1,'REPLACE'); m=o.modifiers.new('a','ARMATURE'); m.object=ao

class SadeIskelet(aday.AdayIskelet):
    """aday.py'nin UE eşlemesi (pelvis/spine_02/spine_03/head, upperarm/lowerarm/hand, thigh/calf/foot); ek olarak ayak düz
    basar (oyuncak.py ile aynı kural), zemin kalibrasyonu ve parmaklar"""
    POZ=('root','shR','shL','neck','head','elR','elL','wrR','wrL','hpR','hpL','knR','knL','anR','anL')
    def __init__(s,ao):
        super().__init__(ao,R_UE)
        # avuç: T-pozunda aşağı (-z; başparmak öne-aşağı). aday.AdayIskelet Rocketbox'ın el x eksenini alıyordu; burada önceden
        # verilir ki on_kol_burgu doğru ekseni ölçsün
        s._avuc={n:insan.avuc_yerel(ao,n,(0,0,-1)) for n in ('lowerarm_r','hand_r','hand_l')}
        # Zemin (2026-10-26): hedef() basan bileği poz sözlüğündeki z=0,05'ten bacak oranıyla kurar; mankende bilek tabandan
        # yalnız ~5,7 cm yukarıda (1,83 m paket × ölçek). Fark ölçülüp bütün figür kaydırılır; BS_SADE_DZ ile elle verilir.
        s.dz=float(os.environ.get('BS_SADE_DZ','-.054'))
    def hedef(s,p):
        # koşu klibinde basan bilek z=0 (diğer ayak işi 0,05): düz basan manken ayağı 3–4,5 cm zemine giriyordu. Bilek
        # hedefi basma yüksekliğinin altına inmez (yalnız bu model; poz sözlüğü değişmez)
        zmin=float(os.environ.get('BS_SADE_BILEK','.05')); p=dict(p)
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
            # ayak dinlenmedeki eğimini korur, yalnız ft yönüne döner (yoksa baldırın eğimini devralıp burnu zemine sokuyordu)
            b=pb[f'foot_{k}']; M=b.matrix.copy(); R0=ao.data.bones[f'foot_{k}'].matrix_local.to_3x3()
            y=R0@Vector((0,1,0)); yh=Vector((y.x,y.y,0)).normalized(); f=T['ft'+K]
            ang=math.atan2(yh.x*f.y-yh.y*f.x,yh.dot(f)); N=Matrix.Rotation(ang,4,'Z')@Matrix(R0).to_4x4(); N.translation=M.translation; b.matrix=N; L.update()
        # parmaklar: raket elinde sapı sarar, boştaki el yarı kapalı. Dinlenmede (döndürülmüş T-pozu) avuç -z, parmaklar
        # dışa (±x): bükme ekseni parmak yönü × avuç normali, kemiğin yerel çerçevesine çevrilir (matrix_basis yerel)
        ac=math.radians(float(os.environ.get('BS_SADE_PARMAK','70')))
        for k,kat in (('r',1.),('l',.35)):
            for f in ('index','middle','ring','pinky','thumb'):
                for j,pay in (('01',.8),('02',1.),('03',.8)):
                    n=f'{f}_{j}_{k}'
                    if n not in pb: continue
                    bn=ao.data.bones[n]; d=(bn.tail_local-bn.head_local).normalized()
                    ax=d.cross(Vector((0,0,-1)))   # d avuca doğru döner: R(d×n,θ)d = d·cosθ + n·sinθ
                    if ax.length<1e-4: continue
                    ax=(bn.matrix_local.to_3x3().inverted()@ax.normalized()).normalized()
                    q=pb[n]; q.rotation_mode='QUATERNION'; q.rotation_quaternion=Quaternion(ax,ac*kat*pay*(.6 if f=='thumb' else 1))
        L.update()

class Sade:
    """insan.Oyuncu ile aynı arayüz: kur_poz(p) → çizilecek nesneler; .kafa raket kafası, .ao iskelet, .R kemik eşlemesi"""
    def __init__(s,M,stil=None):
        onc=set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=GOVDE); govde=next(o for o in set(bpy.data.objects)-onc if o.type=='MESH')
        onc=set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=RIG)   # varsayılan sezgi: kemik ucu tek çocuğun başında (aim bunu varsayar; aşağıda denetlenir)
        yeni=set(bpy.data.objects)-onc; ao=next(o for o in yeni if o.type=='ARMATURE'); s.ao=ao
        for o in yeni:
            if o is not ao: bpy.data.objects.remove(o)   # paketin örnek küresi (Icosphere)
        if ao.animation_data: ao.animation_data.action=None
        for b in ao.pose.bones: b.matrix_basis=Matrix()
        bpy.context.view_layer.update()
        # paket kemik uçlarını çocuk başına koyuyor mu? (aim, kemiğin y ekseninin çocuğa baktığını varsayar)
        for b in ao.data.bones:
            for c in b.children:
                if len(b.children)==1 and (c.head_local-b.tail_local).length>.005: print('sade: uyarı, kemik ucu çocukta değil',b.name,c.name)
        # bölgeler: düzlemlerden kes, yüz merkezine göre malzeme
        mats=dict(forma=M['forma'],sort=_mat('sade_sort',(0.02,0.025,0.06),.65),ten=_mat('sade_ten',(0.80,0.55,0.40),.55),
                  beyaz=M['beyaz'],taban=_mat('sade_ayakkabi',(0.78,0.08,0.10),.5),sac=_mat('sade_sac',(0.05,0.03,0.02),.75),
                  bilek=M['bilek'].copy(),bant=M['bant'].copy())   # insan.malzemeler'de bilek ve bant aynı malzeme: etiket karışmasın
        me=govde.data; _kes(me); me.materials.clear(); sira=['forma','sort','ten','beyaz','taban']
        for k in sira: me.materials.append(mats[k])
        for f in me.polygons: f.material_index=sira.index(bolge(f.center))
        try:
            bpy.ops.object.select_all(action='DESELECT'); govde.select_set(True); bpy.context.view_layer.objects.active=govde
            bpy.ops.mesh.customdata_custom_splitnormals_clear()
        except Exception: pass
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(50))   # plaka kenarları keskin kalır, yüzeyler yumuşak
        sac=_sac(govde,mats); sac['tag']='sac'
        # deri: kök (zeminde, +y'ye uzanan 0,92 m) ve uç (leaf) kemikleri ağırlık almasın; ayak uçları kök kemiğine yapışıyordu
        for b in ao.data.bones: b.use_deform=not (b.name=='root' or 'leaf' in b.name)
        bpy.ops.object.select_all(action='DESELECT'); govde.select_set(True); ao.select_set(True); bpy.context.view_layer.objects.active=ao
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        ag=[v for v in me.vertices if not any(g.weight>1e-4 for g in v.groups)]
        print('sade: ağırlıksız köşe',len(ag),'/',len(me.vertices))
        if len(ag)>.02*len(me.vertices): raise SystemExit('sade: otomatik ağırlık başarısız (bone heat)')
        # uç (leaf) kemikleri silinir: ağırlık almıyorlar ama unity_fbx.py her kemiği her karede anahtarlıyor (66 → 53
        # kemik; FBX 6,2 MB'tan 6 MB sınırının altına iner)
        bpy.ops.object.select_all(action='DESELECT'); ao.select_set(True); bpy.context.view_layer.objects.active=ao
        bpy.ops.object.mode_set(mode='EDIT')
        for eb in [eb for eb in ao.data.edit_bones if 'leaf' in eb.name]: ao.data.edit_bones.remove(eb)
        bpy.ops.object.mode_set(mode='OBJECT')
        _tek_kemik(sac,ao,'head')
        # ekipman: bileklik raket kolunda (sağ, -x) bilekte; kafa bandı alında, saçın kenarında
        b=ao.data.bones; w=b['hand_r'].head_local; e=b['lowerarm_r'].head_local; d=(w-e)
        bil=_halka('sade_bilek',w-d*.1,d,.042,.06,mats['bilek']); _tek_kemik(bil,ao,'lowerarm_r'); bil['tag']='bilek'
        bant=_halka('sade_bant',Vector((0,-.004,1.715)),(0,.32,1),0,0,mats['bant'],major=(.088,.012)); bant.scale=(1.0,1.18,1); bpy.ops.object.transform_apply(scale=True); _tek_kemik(bant,ao,'head'); bant['tag']='bant'
        parca=[govde,sac,bil,bant]
        # 180° dön (oyunun figürü +y'ye bakar, sağ el +x) ve ölçekle (bacak boyu 0,49, insan.Oyuncu/aday ile aynı)
        bacak=(b['calf_r'].head_local-b['thigh_r'].head_local).length+(b['foot_r'].head_local-b['calf_r'].head_local).length
        olc=float(os.environ.get('BS_SADE_OLC',.49/bacak))
        for o in parca: mw=o.matrix_world.copy(); o.parent=None; o.matrix_world=mw
        D=Matrix.Rotation(math.pi,4,'Z')@Matrix.Scale(olc,4)
        for o in [ao]+parca: o.matrix_world=D@o.matrix_world
        bpy.context.view_layer.update()
        bpy.ops.object.select_all(action='DESELECT')
        for o in [ao]+parca: o.select_set(True)
        bpy.context.view_layer.objects.active=ao
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for o in parca:
            for m in o.modifiers:
                if m.type=='ARMATURE': m.object=ao
        # malzemeye göre ayrı nesne (unity_fbx.py etiketi parça adına yazar; maske pası nesne etiketiyle çalışır)
        govde.name='sade_govde'
        bpy.ops.object.select_all(action='DESELECT'); govde.select_set(True); bpy.context.view_layer.objects.active=govde
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.separate(type='MATERIAL'); bpy.ops.object.mode_set(mode='OBJECT')
        s.kalici=[]
        for o in list(bpy.context.scene.objects):
            if o.type!='MESH' or not o.name.startswith('sade_'): continue
            if not o.data.polygons: bpy.data.objects.remove(o); continue
            used={f.material_index for f in o.data.polygons}; m=o.data.materials[min(used)]
            o.data.materials.clear(); o.data.materials.append(m)
            for f in o.data.polygons: f.material_index=0
            k=o.get('tag') or next(k for k,v in mats.items() if v==m); o['tag']=k; o.name='sade_'+k; o.data.name=o.name; s.kalici.append(o)
        print('sade: parçalar',sorted((o.name,len(o.data.vertices)) for o in s.kalici),'ölçek',round(olc,4))
        s.M=M; s.I=SadeIskelet(ao); s.R=R_UE; s.gecici=[]; s.boy=1.0; s.stil={}; s.olc=olc
    def _avuc(s,k):
        """sap ekseni: el kemiği başından orta parmak köküne %55, avuç yönünde yarım el kalınlığı + sap yarıçapı (3d-animator
        rigging §4.2: avuç noktasının altı; yukarıda kalınca sap elin üstünde süzülüyordu)"""
        pb=s.ao.pose.bones; h=pb[f'hand_{k}']; m=pb[f'middle_01_{k}']; p=h.head+(m.head-h.head)*.55
        av=(h.matrix.to_3x3()@s.I._avuc[f'hand_{k}']).normalized()
        return p+av*float(os.environ.get('BS_SADE_AVUC','.02'))
    def kur_poz(s,p):
        for o in s.gecici: bpy.data.objects.remove(o)
        s.gecici=[]; T=s.I.hedef(p); s.I.uygula(T); s.T=T
        mw=s.ao.matrix_world; palm=mw@s._avuc('r')
        nes,s.kafa=insan.raket(palm,T['rk'],T['rkFace'],1.0,s.M); s.gecici+=nes
        if p.get('shuttle'):   # servis: sol elde tüy top (oyuncak.py ile aynı ölçü)
            k=.85; c=mw@s._avuc('l')+Vector((0,.01,.012))
            bpy.ops.mesh.primitive_uv_sphere_add(radius=.0125*k*1.6,location=c); m_=bpy.context.object; m_.data.materials.append(s.M['mantar']); m_['tag']='mantar'
            bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=.012*k*1.6,radius2=.034*k*1.6,depth=.065*k*1.6,end_fill_type='NOTHING',location=c+Vector((0,0,.035*k*1.6))); t=bpy.context.object; t.data.materials.append(s.M['tuy']); t['tag']='tuy'
            s.gecici+=[m_,t]
        return s.kalici+s.gecici
