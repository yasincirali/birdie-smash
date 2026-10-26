# Oyuncak 3B oyuncu (yasin 2026-10-24: "iki modeli de seçenek sun, defaultta gerçekçi olmayan olsun; 360 ekranında
# detaylar sırıtmamalı"). Gövde: KayKit Character Pack Adventures 1.0, Rogue (CC0, Kay Lousberg; LICENSE.txt paketle
# gelir). Paketin kendi hareketleri kullanılmaz: aynı badminton poz sözlükleri (karakter.py) bu iskelete eklem hedefi
# olarak uygulanır, böylece smaç, clear, uzanma, split step, backhand klipleri iki modelde de aynıdır; raket hep sağ elde.
# Kurulum: git clone --depth 1 https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0 (BS_KAYKIT).
# Kıyafet ve saç örnek sayfadaki gibi (yasin onaylı; kaynak /mnt/project-files/birdie-smash/karakter-3d-secenek/dk.py):
# mavi forma + beyaz ön yaka, koyu şort, beyaz çorap, kırmızı tabanlı beyaz ayakkabı, raket kolunda beyaz bileklik,
# kısa koyu saç. Forma maskeden boyandığı için gri çizilir (oyun forma rengini seçimle verir).
import bpy, bmesh, math, os
from mathutils import Vector, Matrix
import insan
KK=os.environ.get('BS_KAYKIT',os.path.expanduser('~/AnimationLibrary/kaykit/addons/kaykit_character_pack_adventures/Characters/gltf/Rogue.glb'))
OLC=float(os.environ.get('BS_OY_OLC','.46'))   # .52 sportif figürden 1,3× iri çıkıyordu; .46 ≈ 1,15×. paket ~2,3 birim boyunda; oyunda sportif figürden (~1,05) biraz uzun kalsın diye ~1,2: iri baş uzaktan okunur

def _adalar(o,PX,IW,IH):
    """bağlı yüz adaları: (yüz indeksleri, merkez, boyut, ortalama doku rengi)"""
    me=o.data; uv=me.uv_layers.active.data; bm=bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); gor=set(); out=[]
    for f in bm.faces:
        if f.index in gor: continue
        st=[f]; ada=[]; gor.add(f.index)
        while st:
            x=st.pop(); ada.append(x.index)
            for e in x.edges:
                for y in e.link_faces:
                    if y.index not in gor: gor.add(y.index); st.append(y)
        vs=[me.vertices[i].co for fi in ada for i in me.polygons[fi].vertices]
        c=sum(vs,Vector())/len(vs); sz=Vector([max(p[i] for p in vs)-min(p[i] for p in vs) for i in range(3)])
        col=Vector();
        for fi in ada:
            p=me.polygons[fi]; u=sum(uv[l].uv.x for l in p.loop_indices)/p.loop_total; v=sum(uv[l].uv.y for l in p.loop_indices)/p.loop_total
            j=(min(IH-1,max(0,int(v*IH)))*IW+min(IW-1,max(0,int(u*IW))))*4; col+=Vector(PX[j:j+3])
        out.append((ada,c,sz,col/len(ada)))
    bm.free(); return out
def _sil(o,yuzler):
    if not yuzler: return
    bm=bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table(); bmesh.ops.delete(bm,geom=[bm.faces[i] for i in set(yuzler)],context='FACES'); bm.to_mesh(o.data); bm.free()

class OyuncakIskelet(insan.Iskelet):
    """insan.Iskelet'in hedef() hesabı aynen; eklemler ve uygula() KayKit kemik adlarıyla"""
    def __init__(s,ao):
        s.ao=ao; b=ao.data.bones; H=lambda n:b[n].head_local.copy()
        s.hipR,s.hipL=H('upperleg.r'),H('upperleg.l'); s.kneeR=H('lowerleg.r'); s.ankR=H('foot.r')
        s.shR,s.shL=H('upperarm.r'),H('upperarm.l'); s.elR=H('lowerarm.r'); s.wrR=H('wrist.r')
        s.TH=(s.kneeR-s.hipR).length; s.SH=(s.ankR-s.kneeR).length; s.UA=(s.elR-s.shR).length; s.FA=(s.wrR-s.elR).length
        s.root=(s.hipR+s.hipL)/2+Vector((0,0,.04*OLC))
        s.neck=H('head'); s.headc=(H('head')+b['head'].tail_local)/2
        # Zemin kalibrasyonu (2026-10-25): poz sözlüğünde basan bilek z=0,05 (soyut figürün tabanı bileğin 4,5 cm altında);
        # hedef() bunu bacak oranıyla (k≈0,46) 2,3 cm'e küçültüyordu, KayKit'in iri ayağında bilek tabandan 6,7 cm yukarıda.
        # Ayakkabı tabanı her klipte zeminin 8–12 cm altındaydı (Unity'de 1,75× ölçekle ~17 cm). Bütün figür (kök, eklem
        # hedefleri) aradaki fark kadar yukarı taşınır: poz biçimi aynı kalır, basan ayağın tabanı zemine oturur.
        k=(s.TH+s.SH)/(insan.TOY['TH']+insan.TOY['SH']); s.dz=float(os.environ.get('BS_OY_DZ',s.ankR.z-.045*k))
    POZ=('root','shR','shL','neck','head','elR','elL','wrR','wrL','hpR','hpL','knR','knL','anR','anL')
    def hedef(s,p):   # "sevimli ama oturaklı" (yasin 24 Eki): kalça biraz alçak, ayaklar yerinde kalır, IK dizi hafif büker
        p=dict(p); r=p['root']; p['root']=(r[0],r[1],r[2]-float(os.environ.get('BS_OY_COK','.014'))); T=super().hedef(p)
        dz=Vector((0,0,s.dz))
        for n in s.POZ: T[n]=T[n]+dz
        # iri baş: ince figürün başının yanından geçen el (smaç, clear hazırlığı) burada yüzün önüne düşüyordu. El ve dirsek
        # baş küresinin içindeyse küreden dışarı itilir; raket kemiğin gerçek pozundan kurulduğu için elle birlikte gider
        hc=T['head']; rH=.62*OLC
        for K in ('R','L'):
            for j in ('wr','el'):
                d=T[j+K]-hc
                if d.length<rH: T[j+K]=hc+(d.normalized() if d.length>1e-4 else Vector((1 if K=='R' else -1,0,0)))*rH
        return T
    def uygula(s,T):
        ao=s.ao; pb=ao.pose.bones; L=bpy.context.view_layer
        for b in pb: b.matrix_basis=Matrix()
        L.update()
        def set_rot(n,Rw,loc=None):
            b=pb[n]; rest=ao.data.bones[n].matrix_local; M=Rw.to_4x4()@Matrix(rest.to_3x3()).to_4x4(); M.translation=loc if loc is not None else b.matrix.translation; b.matrix=M; L.update()
        def aim(n,hedef):
            b=pb[n]; M=b.matrix.copy(); y=(M.to_3x3()@Vector((0,1,0))).normalized(); d=(hedef-M.translation).normalized()
            N=y.rotation_difference(d).to_matrix().to_4x4()@Matrix(M.to_3x3()).to_4x4(); N.translation=M.translation; b.matrix=N; L.update()
        set_rot('hips',T['Rpel'],T['root']-(T['Rpel']@(s.root-ao.data.bones['hips'].head_local)))
        for n,t in (('spine',.5),('chest',1.)): set_rot(n,T['Rpel'].to_quaternion().slerp(T['Rgov'].to_quaternion(),t).to_matrix())
        for k,K in (('r','R'),('l','L')):
            aim(f'upperarm.{k}',T['el'+K]); aim(f'lowerarm.{k}',T['wr'+K])
            fa=(T['wr'+K]-T['el'+K]).normalized()
            if K=='R': fa=(fa*.6+T['rk']*.4).normalized()   # bilek rakete doğru hafif kırılır
            if K=='R':   # ön kol burulması (insan.on_kol_burgu); KayKit T-pozunda avuç aşağı (-z; handslot elin altında)
                if not hasattr(s,'_avuc'): s._avuc={n:insan.avuc_yerel(ao,n,(0,0,-1)) for n in ('lowerarm.r','wrist.r')}
                s.son_burgu=insan.on_kol_burgu(pb,L,T,('upperarm.r','lowerarm.r','wrist.r'),s._avuc,lambda:aim('lowerarm.r',T['wrR']),lambda:aim('wrist.r',T['wrR']+fa))
            else: aim(f'wrist.{k}',T['wr'+K]+fa)
            aim(f'hand.{k}',T['wr'+K]+fa*2)
            aim(f'upperleg.{k}',T['kn'+K]); aim(f'lowerleg.{k}',T['an'+K])
            # Ayak düz basar (2026-10-25): eskiden ayak baldırın eğimini devralıyordu, diz öne gidince burun zemine giriyordu
            # (boşta 2 cm, öne adımda 4 cm). Ayak dinlenme duruşundaki eğimini korur, yalnız ft yönüne döner.
            b=pb[f'foot.{k}']; M=b.matrix.copy(); R0=ao.data.bones[f'foot.{k}'].matrix_local.to_3x3()
            y=R0@Vector((0,1,0)); yh=Vector((y.x,y.y,0)).normalized(); f=T['ft'+K]
            ang=math.atan2(yh.x*f.y-yh.y*f.x,yh.dot(f)); N=Matrix.Rotation(ang,4,'Z')@Matrix(R0).to_4x4(); N.translation=M.translation; b.matrix=N; L.update()
        set_rot('head',Vector((0,1,0)).rotation_difference(T['look']).to_matrix())

class Oyuncak:
    """insan.Oyuncu ile aynı arayüz: kur_poz(p) → çizilecek nesneler; .kafa raket kafası, .ao iskelet, .boy ölçek"""
    def __init__(s,M,stil=None):
        bpy.ops.import_scene.gltf(filepath=KK)
        ao=bpy.data.objects['Rig']; s.ao=ao
        for n in ['Knife_Offhand','1H_Crossbow','2H_Crossbow','Knife','Throwable','Rogue_Cape','Icosphere']:
            o=bpy.data.objects.get(n)
            if o: bpy.data.objects.remove(o)
        if ao.animation_data: ao.animation_data.action=None
        for b in ao.pose.bones: b.matrix_basis=Matrix()
        img=next(i for i in bpy.data.images if i.size[0]>0); IW,IH=img.size; PX=img.pixels[:]
        par={n:bpy.data.objects[n] for n in ('Rogue_Body','Rogue_Head','Rogue_ArmLeft','Rogue_ArmRight','Rogue_LegLeft','Rogue_LegRight')}
        # 360° ve yakın planda sırıtan paket ayrıntıları kalkar (yasin: "detaylar sırıtmamalı"): kemerdeki cepler, toka ve
        # kayışlar (gövdede 40 yüzden küçük adalar), küpeler (başta gri adalar), yakanın iç iki katı (arkada tırtıklı
        # görünüyordu; dış kat yaka olur), kapüşonun ense parçaları (koyu kahve, başın altında).
        bant_c=None
        for n,o in par.items():
            sil=[]
            for ada,c,sz,col in _adalar(o,PX,IW,IH):
                pass   # göğüs kayışı ve yakanın iç katları silinmez: altlarında delik var (koyu çizgi görünüyordu), boyanırlar
                if n=='Rogue_Head' and ((col.z>col.x and col.x>.4) or (c.z<1.3 and col.x<.55 and col.x>.4)): sil+=ada
            if n=='Rogue_Body':   # bel: kemer, toka, cep, tunik eteği ve yatay pul halkalar üst üste koyu basamak yapıyordu; hepsi
                sil+=[f for ada,c,sz,col in _adalar(o,PX,IW,IH) if c.z<.66 for f in ada]   # kalkar, yerine aşağıda tek parça şort beli
            if n.startswith('Rogue_Arm'):   # kolluk ve eldiven katları farklı çaplı halkalar: 360°'de basamak basamak görünüyordu.
                sag=n=='Rogue_ArmRight'; me=o.data   # Her parça kolun ekseni çevresinde ortak çapa çekilir, ince flanş silinir
                for ada,c,sz,col in _adalar(o,PX,IW,IH):
                    x=abs(c.x); hedef=None
                    if .25<x<.41: hedef=.215
                    elif .55<x<.7 and sz.x<.06: sil+=ada; continue   # kolluk halkaları (3 ince kat) silinir; boşluğu uzayan eldiven ağzı kapatır
                    elif .66<x<.78: hedef=.217   # raket kolunda bileklik hafif kabarık kalır
                    if hedef:
                        k=hedef/max(sz.y,sz.z); vs={i for fi in ada for i in me.polygons[fi].vertices}
                        for i in vs: v=me.vertices[i].co; v.y=c.y+(v.y-c.y)*k; v.z=c.z+(v.z-c.z)*k
                        if .66<x<.78 and sz.x>.06:   # eldiven ağzı dirseğe kadar uzar (dış ucu yerinde): ön kol tek pürüzsüz parça olur
                            uc=math.copysign(x+sz.x/2,c.x); kk=(x+sz.x/2-.6)/sz.x
                            for i in vs: v=me.vertices[i].co; v.x=uc+(v.x-uc)*kk
                            if sag: bant_c=(c.y,c.z,uc)
            _sil(o,sil)
        # şort beli: gövdenin alt kenarından bacak köküne inen tek, pürüzsüz elips boru (kalça kemiğine bağlı)
        gv=par['Rogue_Body'].data.vertices; alt=[v.co for v in gv if v.co.z<.66]
        cx=sum(v.x for v in alt)/len(alt); cy=sum(v.y for v in alt)/len(alt); rx=max(abs(v.x-cx) for v in alt); ry=max(abs(v.y-cy) for v in alt); zt=max(v.z for v in alt)
        bm=bmesh.new(); N_=32; halkalar=[]
        for z,k in ((zt+.02,.99),(.56,1.0),(.48,.99),(.42,.95),(.38,.8)):
            halkalar.append([bm.verts.new((cx+rx*k*math.cos(2*math.pi*i/N_),cy+ry*k*math.sin(2*math.pi*i/N_),z)) for i in range(N_)])
        for h0,h1 in zip(halkalar,halkalar[1:]):
            for i in range(N_): bm.faces.new((h0[i],h0[(i+1)%N_],h1[(i+1)%N_],h1[i]))
        bm.faces.new(list(reversed(halkalar[-1])))
        bel_me=bpy.data.meshes.new('Rogue_Bel'); bm.to_mesh(bel_me); bm.free(); bel=bpy.data.objects.new('Rogue_Bel',bel_me); bpy.context.scene.collection.objects.link(bel)
        bel.matrix_world=par['Rogue_Body'].matrix_world.copy()
        vg=bel.vertex_groups.new(name='hips'); vg.add(list(range(len(bel_me.vertices))),1,'REPLACE'); m_=bel.modifiers.new('a','ARMATURE'); m_.object=ao
        bel.data.uv_layers.new(); par['Rogue_Bel']=bel
        # bileklik: uzayan eldiven ağzı ön kolun tamamını kaplıyor; bileklik bilekte ayrı, kısa ve hafif kabarık halka
        by,bz,bu=bant_c; bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.121,depth=.075,location=(bu-math.copysign(.075,bu),by,bz),rotation=(0,math.pi/2,0))
        bb=bpy.context.object; bb.name='Rogue_Bileklik'; bb.matrix_world=par['Rogue_ArmRight'].matrix_world@bb.matrix_world; bpy.ops.object.shade_smooth()
        vg=bb.vertex_groups.new(name='lowerarm.r'); vg.add(list(range(len(bb.data.vertices))),1,'REPLACE'); m_=bb.modifiers.new('a','ARMATURE'); m_.object=ao
        par['Rogue_Bileklik']=bb
        mats=dict(forma=M['forma'],beyaz=M['beyaz'],bilek=M['bilek'])
        def mat(n,c,r=.55):
            m=bpy.data.materials.new(n); m.use_nodes=True; b=m.node_tree.nodes['Principled BSDF']; b.inputs['Base Color'].default_value=(*c,1); b.inputs['Roughness'].default_value=r; return m
        mats['ten']=mat('oy_ten',(0.91,0.49,0.29)); mats['sort']=mat('oy_sort',(0.012,0.014,0.05),.6);   # koyu şort: etek katlarının tırtıklı kenarı aynı renkte kaybolur
        mats['sac']=mat('oy_sac',(0.045,0.022,0.012),.7); mats['taban']=mat('oy_taban',(0.9,0.05,0.08),.6)
        mats['yaka']=mat('oy_yaka',(0.9,0.9,0.88),.5); mats['goz']=mat('oy_goz',(0.03,0.03,0.04),.3)
        def renk(o,f):
            uv=o.data.uv_layers.active.data; u=sum(uv[l].uv.x for l in f.loop_indices)/f.loop_total; v=sum(uv[l].uv.y for l in f.loop_indices)/f.loop_total
            j=(min(IH-1,max(0,int(v*IH)))*IW+min(IW-1,max(0,int(u*IW))))*4; return PX[j:j+3]
        kahve=lambda c: c[0]>.4 and c[0]-c[2]>.2 and c[0]<.85
        def govde(c,p): return 'sort' if p.z<.6 else ('yaka' if p.z>1.12 and abs(p.x)<.22 and p.y<0 else 'forma')
        def kol(c,p):
            x=abs(p.x); return 'forma' if x<.4 else 'ten' if x<.62 else ('bilek' if p.x<0 else 'ten') if x<.7 else 'ten'   # bileklik yalnız raket kolunda (paketin sağ kolu -x)
        def bacak(c,p): return 'sort' if p.z>.44 else 'ten' if p.z>.33 else 'beyaz' if p.z>.035 else 'taban'
        def bas(c,p): return 'sac' if kahve(c) else 'goz' if max(c)<.25 else 'ten'
        def kol_ada(c,n_,sag):
            x=abs(c.x); return 'forma' if x<.4 else 'ten'   # bileklik ayrı nesne (aşağıda)
        def bacak_ada(c,n_): return 'taban' if c.z<.05 else 'sort' if c.z>.3 else 'ten' if .235<c.z<.27 else 'beyaz'
        ADA_KURAL={'Rogue_ArmRight':lambda c,n_:kol_ada(c,n_,True),'Rogue_ArmLeft':lambda c,n_:kol_ada(c,n_,False),'Rogue_LegLeft':bacak_ada,'Rogue_LegRight':bacak_ada}
        kural={'Rogue_Bileklik':lambda c,p:'bilek','Rogue_Bel':lambda c,p:'sort','Rogue_Body':govde,'Rogue_Head':bas,'Rogue_ArmLeft':kol,'Rogue_ArmRight':kol,'Rogue_LegLeft':bacak,'Rogue_LegRight':bacak}
        sira=list(mats)
        for n,o in par.items():
            o.data.materials.clear()
            for k in sira: o.data.materials.append(mats[k])
            if n=='Rogue_Head':   # yüzün yanından ve arkadan sarkan uzun saç kesilir; yerine aşağıdaki kapak
                _sil(o,[f.index for f in o.data.polygons if kahve(renk(o,f)) and f.center.z<1.7])
            for f in o.data.polygons: f.material_index=sira.index(kural[n](renk(o,f),f.center))
            if n in ADA_KURAL:   # kol ve bacak ada ada boyanır: yüz yüz sınır çizince bileklik ve şort kenarı testere dişi çıkıyordu
                for ada,c,sz,col in _adalar(o,PX,IW,IH):
                    k=ADA_KURAL[n](c,len(ada))
                    for fi in ada: o.data.polygons[fi].material_index=sira.index(k)
            bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
            try: bpy.ops.mesh.customdata_custom_splitnormals_clear()
            except Exception: pass
            bpy.ops.object.shade_smooth()   # paketin düz normalleri ve basamaklı gölge yüzü köşeli gösteriyordu; örnekteki yumuşak oyuncak yüzeyi
        # Uzun saç kesilince ensede kafa içi görünüyordu: kafanın içine saç renginde kapak (r .5, arkaya .1: saçın kesik kenarı
        # kapağın içine gömülür, ensede tırtıklı çizgi kalmaz; BS_KAP ile denenir), enseye doğru aşağı inen ve
        # yakaya kadar uzanan yumurta biçimi (küre ensede kafatasından taşıp basamak yapıyordu)
        KR,KY,KZ=(float(x) for x in os.environ.get('BS_KAP','.5,.1,1.64').split(',')); bpy.ops.mesh.primitive_uv_sphere_add(radius=KR,location=(0,KY,KZ),segments=40,ring_count=20); kap=bpy.context.object; kap.name='oy_sackapak'
        for v in kap.data.vertices:
            if v.co.z<0 and v.co.y>0: v.co.z*=1.25   # ense: aşağı uzar, yakaya yumuşak iner
        kap.scale=(1.02,.97,.93); kap.data.materials.append(mats['sac']); bpy.ops.object.shade_smooth()
        ar=kap.modifiers.new('a','ARMATURE'); ar.object=ao; vg=kap.vertex_groups.new(name='head'); vg.add(list(range(len(kap.data.vertices))),1,'REPLACE')
        # 180° dön (oyunun figürü +y'ye bakar, sağ el +x) ve ölçekle; dönüşüm iskelete ve ağlara işlenir ki kemik matrisleri dünya olsun
        for o in par.values():   # ebeveyn çözülür (iskeletle birlikte dönüşüm uygulanınca çocuk iki kez dönüyordu); deri ARMATURE değiştiricisinden
            mw=o.matrix_world.copy(); o.parent=None; o.matrix_world=mw
        bpy.ops.object.select_all(action='DESELECT')
        for o in [ao,kap]+list(par.values()): o.select_set(True)
        bpy.context.view_layer.objects.active=ao
        D=Matrix.Rotation(math.pi,4,'Z')@Matrix.Scale(OLC,4)
        for o in [ao,kap]+list(par.values()): o.matrix_world=D@o.matrix_world   # glTF nesneleri kuaterniyon kipinde: euler yazmak etkisizdi
        bpy.context.view_layer.update()
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        # malzemeye göre ayrı nesne: maske pası nesne etiketiyle çalışır (forma/bilek ayrı boyanır)
        s.kalici=[]
        for o in list(par.values())+[kap]:
            if o.name!='oy_sackapak':
                bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
                bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.separate(type='MATERIAL'); bpy.ops.object.mode_set(mode='OBJECT')
        for o in list(bpy.context.scene.objects):
            if o.type!='MESH' or not (o.name.startswith('Rogue_') or o.name=='oy_sackapak'): continue
            if not o.data.polygons: bpy.data.objects.remove(o); continue
            used={f.material_index for f in o.data.polygons}; m=o.data.materials[min(used)]
            o.data.materials.clear(); o.data.materials.append(m);
            for f in o.data.polygons: f.material_index=0
            k=next(k for k,v in mats.items() if v==m)
            o['tag']={'forma':'forma','bilek':'bilek'}.get(k,k); s.kalici.append(o)
            if os.environ.get('BS_OY_SUBSURF','1')=='1' and o.name.startswith(('Rogue_ArmLeft','Rogue_ArmRight')) and k=='ten':
                sm=o.modifiers.new('ss','SUBSURF'); sm.levels=sm.render_levels=1   # paketin eldiveni 6 köşeli kutu: yakında köşeli görünüyordu
        s.boy=OLC*2.25; s.M=M; s.I=OyuncakIskelet(ao); s.gecici=[]
        s.stil={}
    def kur_poz(s,p):
        for o in s.gecici: bpy.data.objects.remove(o)
        s.gecici=[]; T=s.I.hedef(p); s.I.uygula(T); s.T=T; pb=s.ao.pose.bones
        palm=pb['handslot.r'].head.copy()
        nes,s.kafa=insan.raket(palm,T['rk'],T['rkFace'],1.0,s.M); s.gecici+=nes   # oyuncak oranına göre iri raket (~%85 gerçek boy)
        if p.get('shuttle'):
            k=.85; c=pb['handslot.l'].head+Vector((0,.01,.012))
            bpy.ops.mesh.primitive_uv_sphere_add(radius=.0125*k*1.6,location=c); m_=bpy.context.object; m_.data.materials.append(s.M['mantar']); m_['tag']='mantar'
            bpy.ops.mesh.primitive_cone_add(vertices=16,radius1=.012*k*1.6,radius2=.034*k*1.6,depth=.065*k*1.6,end_fill_type='NOTHING',location=c+Vector((0,0,.035*k*1.6))); t=bpy.context.object; t.data.materials.append(s.M['tuy']); t['tag']='tuy'
            s.gecici+=[m_,t]
        return s.kalici+s.gecici
