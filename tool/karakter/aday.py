# Aday hazır modeller (yasin 2026-10-24: "bedava modeller arasında en yüksek kalitede olanları bana sun, uygulamada nasıl
# gözükür"). Karşılaştırma içindir: paket olduğu gibi gelir (kıyafet, doku), aynı badminton pozları kemik adları
# eşlenerek uygulanır, raket bizim. Seçilen olursa oyuncak.py gibi kalıcı modele dönüşür.
# BS_ADAY=<model yolu>, BS_ADAY_RIG=rocketbox|vrm|ue
import bpy, math, os, glob
from mathutils import Vector, Matrix
import insan

RIG={
 'rocketbox':dict(hips='Bip01 Pelvis',spine='Bip01 Spine1',chest='Bip01 Spine2',head='Bip01 Head',
   ua='Bip01 {S} UpperArm',fa='Bip01 {S} Forearm',el='Bip01 {S} Hand',ul='Bip01 {S} Thigh',ll='Bip01 {S} Calf',ft='Bip01 {S} Foot',S={'r':'R','l':'L'}),
 'vrm':dict(hips='J_Bip_C_Hips',spine='J_Bip_C_Chest',chest='J_Bip_C_UpperChest',head='J_Bip_C_Head',
   ua='J_Bip_{S}_UpperArm',fa='J_Bip_{S}_LowerArm',el='J_Bip_{S}_Hand',ul='J_Bip_{S}_UpperLeg',ll='J_Bip_{S}_LowerLeg',ft='J_Bip_{S}_Foot',S={'r':'R','l':'L'}),
 'ue':dict(hips='pelvis',spine='spine_02',chest='spine_03',head='head',
   ua='upperarm_{S}',fa='lowerarm_{S}',el='hand_{S}',ul='thigh_{S}',ll='calf_{S}',ft='foot_{S}',S={'r':'r','l':'l'}),
}

class AdayIskelet(insan.Iskelet):
    def __init__(s,ao,R):
        s.ao=ao; s.R=R; b=ao.data.bones; s.n=lambda k,y=None: R[k].format(S=R['S'][y]) if y else R[k]
        H=lambda n:b[n].head_local.copy()
        s.hipR,s.hipL=H(s.n('ul','r')),H(s.n('ul','l')); s.kneeR=H(s.n('ll','r')); s.ankR=H(s.n('ft','r'))
        s.shR,s.shL=H(s.n('ua','r')),H(s.n('ua','l')); s.elR=H(s.n('fa','r')); s.wrR=H(s.n('el','r'))
        s.TH=(s.kneeR-s.hipR).length; s.SH=(s.ankR-s.kneeR).length; s.UA=(s.elR-s.shR).length; s.FA=(s.wrR-s.elR).length
        s.root=(s.hipR+s.hipL)/2; hb=b[s.n('head')]; s.neck=hb.head_local.copy(); s.headc=hb.head_local+Vector((0,0,.11*s.TH/.42))
    # Zemin (2026-10-26, tüm beden turu): ayak artık dinlenme eğimiyle basıyor (aşağıda ft); eskiden baldırın öne eğimini
    # devralıp burnu aşağı (sol ayak −9°) indiği için taban zemine değiyordu. Düz ayakta Rocketbox bileği poz z=0,05'ten
    # 3,7 figür cm yüksekte kalıyor (probe: taban 8 cm oyunda); figür o kadar indirilir (sade.py ile aynı kural, kendi değeri).
    # Yalnız bu sınıf: SadeIskelet kendi dz'sini uygular.
    POZ_Z=('root','shR','shL','neck','head','elR','elL','wrR','wrL','hpR','hpL','knR','knL','anR','anL')
    def hedef(s,p):
        T=super().hedef(p)
        if type(s) is AdayIskelet:
            dz=Vector((0,0,float(os.environ.get('BS_ADAY_DZ','-.037'))))
            for n in s.POZ_Z:
                if n in T: T[n]=T[n]+dz
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
        h=s.n('hips'); set_rot(h,T['Rpel'],T['root']-(T['Rpel']@(s.root-ao.data.bones[h].head_local)))
        for n,t in ((s.n('spine'),.5),(s.n('chest'),1.)): set_rot(n,T['Rpel'].to_quaternion().slerp(T['Rgov'].to_quaternion(),t).to_matrix())
        for k,K in (('r','R'),('l','L')):
            aim(s.n('ua',k),T['el'+K]); aim(s.n('fa',k),T['wr'+K])
            fa=(T['wr'+K]-T['el'+K]).normalized()
            if K=='R': fa=(fa*.6+T['rk']*.4).normalized()
            if K=='R':   # ön kol burulması (insan.on_kol_burgu, oyuncak.py ile aynı kural)
                nua,nfa,nel=s.n('ua','r'),s.n('fa','r'),s.n('el','r')
                if not hasattr(s,'_avuc'):
                    # Rocketbox A-pozunda avuç: el kemiğinin yerel x'i (parmaklar yerel z çevresinde bu yöne kapanır;
                    # kur_poz'daki parmak bükümüyle aynı eksen). Ön kol için aynı dünya yönü.
                    av=ao.data.bones[nel].matrix_local.to_3x3().col[0]; s._avuc={n:insan.avuc_yerel(ao,n,av) for n in (nfa,nel)}
                s.son_burgu=insan.on_kol_burgu(pb,L,T,(nua,nfa,nel),s._avuc,lambda:aim(nfa,T['wrR']),lambda:aim(nel,T['wrR']+fa))
            else: aim(s.n('el',k),T['wr'+K]+fa)
            aim(s.n('ul',k),T['kn'+K]); aim(s.n('ll',k),T['an'+K])
        # Ayak yönü (2026-10-26, tüm beden turu): bu iskelet ft (ayak ucu yönü) kanalını hiç okumuyordu; ayak baldırın dönüşünü
        # devralıyordu. Backhand overhead'de sade/oyuncak sağ ayağı arkaya (ft −0,6,−1) çevirirken Rocketbox'ta ayak öne
        # bakıyor, diz ipucu ayak ucuna göre 177° ters kalıyordu (probe). sade.py ile aynı kural: ayak dinlenme eğimini korur,
        # yalnız düşey eksen çevresinde ft yönüne döner. İleri = parmak kemiğine (Bip01 ayak kemiğinin y'si tabana bakar).
        up=(ao.matrix_world.inverted().to_3x3()@Vector((0,0,1))).normalized()
        for k,K in (('r','R'),('l','L')):
            n=s.n('ft',k); b=pb[n]; db=ao.data.bones[n]; R0=db.matrix_local.to_3x3()
            uc=[c for c in db.children]
            y=(uc[0].head_local-db.head_local) if uc else R0@Vector((0,1,0))
            yh=y-up*y.dot(up); f=Vector(T['ft'+K]); f=f-up*f.dot(up)
            if yh.length<1e-6 or f.length<1e-6: continue
            yh.normalize(); f.normalize(); ang=math.atan2(up.dot(yh.cross(f)),yh.dot(f))
            N=Matrix.Rotation(ang,4,up)@Matrix(R0).to_4x4(); N.translation=b.matrix.translation; b.matrix=N; L.update()
        set_rot(s.n('head'),Vector((0,1,0)).rotation_difference(T['look']).to_matrix())

# Temas katmanı (2026-10-26, yasin: "objelerde iç içe geçmeleri de çözmen lazım"). Ortak poz sözlükleri (karakter.py)
# çubuk figür ölçüsüyle yazıldı; Rocketbox gövdesi daha dolgun olduğundan bazı pozlarda raket gövdeye/bacağa, el karna,
# ön kol başa giriyordu, koşu/hamlede ayak burnu zemine gömülüyordu. Düzeltme yalnız bu modelde, pozdan SONRA ve
# deterministik (aynı poz → aynı sonuç; Unity klibine pişer):
#  1. kollar: el/ön kol noktaları gövdenin (leğen, karın, göğüs, köprücük, uyluk, baş) içindeyse bilek hedefi yüzey
#     normali boyunca dışarı itilir, dirsek iki kemik IK ile yeniden çözülür (en çok 3 tur);
#  2. raket: sap + şaft + kafa çemberi gövdeye/bacağa/sol kola 1 cm'den yakınsa raket avuç çevresinde en küçük açıyla
#     (≤40°) döndürülür; kafa yüzünün yönü aynı dönüşü alır;
#  3. ayaklar: taban zeminin altındaysa ayak bilekte öne/arkaya devrilir (≤40°), burun hâlâ gömülüyse parmak kemiği
#     kalkar; kalan gömülme bilek hedefini yükseltir.
# Ölçüler gerçek derili ağdan alınır (BVH); el/ayak noktaları kemiğe rijit sayılır (ağırlığı >%60 tek kemikte).
GOVDE_K=('Pelvis','Spine','Spine1','Spine2','Neck','L Clavicle','R Clavicle','L Thigh','R Thigh','Head')
def _bolge(n):
    n=n.replace('Bip01 ','')
    if n in GOVDE_K: return 'govde'
    if 'Hand' in n or 'Finger' in n: return 'el'+n[0]
    if 'Forearm' in n: return 'kol'+n[0]
    if 'UpperArm' in n: return 'ust'+n[0]
    return 'bacak'
class Temas:
    PAY_KOL,PAY_RAKET,PAY_ZEMIN=.003,.010,.0015   # figür birimi (oyunda ×1,75)
    def __init__(s,A):
        from mathutils.bvhtree import BVHTree
        s.A=A; s.BVH=BVHTree; ao=A.ao; s.govde=[o for o in A.kalici if str(o.get('tag')) in ('ten','forma')]
        s.dom={}; s.rij={}
        for o in s.govde:
            gn={g.index:g.name for g in o.vertex_groups}; d=[]
            for v in o.data.vertices:
                acc={}
                for g in v.groups: acc[gn[g.group]]=acc.get(gn[g.group],0)+g.weight
                b=max(acc,key=acc.get) if acc else ''; d.append(_bolge(b) if b else 'bacak')
                if acc and acc[b]>(.4 if 'Forearm' in b else .6) and any(k in b for k in ('Hand','Finger','Forearm','Foot','Toe')):
                    s.rij.setdefault(b,[]).append(ao.data.bones[b].matrix_local.inverted()@v.co)
            s.dom[o.as_pointer()]=d   # unity_fbx nesneleri yeniden adlandırır: ada değil nesneye bağlı
        s.n=A.I.n; s.rapor={}
    def _nokta(s,anah):
        ao=s.A.ao; pb=ao.pose.bones; P=[]
        for b,L in s.rij.items():
            if any(k in b for k in anah): M=ao.matrix_world@pb[b].matrix; P+=[M@q for q in L]
        return P
    def _agac(s,tut):
        dg=bpy.context.evaluated_depsgraph_get(); V=[]; F=[]
        for o in s.govde:
            e=o.evaluated_get(dg); me=e.to_mesh(); mw=o.matrix_world; d=s.dom[o.as_pointer()]; b0=len(V)
            V+=[mw@v.co for v in me.vertices]
            F+=[[b0+i for i in f.vertices] for f in me.polygons if all(d[i] in tut for i in f.vertices)]
            e.to_mesh_clear()
        return s.BVH.FromPolygons(V,F)
    @staticmethod
    def _yon(T,loc,nr):
        """en yakın nokta kenar/köşedeyse tek yüzün normali içeri/dışarıyı yanlış söyler: o noktaya değen yüzlerin ortalaması"""
        n=Vector()
        for q in T.find_nearest_range(loc,1e-5): n+=q[1]
        return n.normalized() if n.length>1e-6 else nr
    @staticmethod
    def _derin(T,P,esik=.08):
        en=(0.,None,None)
        for p in P:
            loc,nr,i,d=T.find_nearest(p,esik)
            if loc is None: continue
            nr=Temas._yon(T,loc,nr)
            if (p-loc).dot(nr)<0 and d>en[0]: en=(d,nr.copy(),p)
        return en
    def kollar(s,T):
        I=s.A.I; pb=s.A.ao.pose.bones; mw=s.A.ao.matrix_world
        for tur in range(6):
            I.uygula(T); s.A.parmak()
            Tg=s._agac({'govde'}); deg=False
            ax0=mw@pb[s.n('hips')].head; ax1=mw@pb[s.n('head')].head
            for K in 'RL':
                P=s._nokta((f'{K} Hand',f'{K} Finger',f'{K} Forearm')); d,nr,p=s._derin(Tg,P)
                if d<s.PAY_KOL*.5: continue
                # derin batmada (ön kol gövdenin ortasından geçiyor) en yakın yüzey önü/arkayı karıştırır: itme yönü gövde
                # ekseninden (leğen→baş) noktaya yatay doğrultu, miktarı o doğrultuda dışarıdan atılan ışının yüzeye değdiği yer
                sh=mw@pb[s.n('ua',K.lower())].head; el0=T['el'+K]; w0=T['wr'+K]; f=w0-el0
                a=ax1-ax0; q=ax0+a*max(0.,min(1.,(p-ax0).dot(a)/a.length_squared)); rn=p-q; rn.z=0
                if rn.length<.02: rn=sh-q; rn.z=0
                rn.normalize(); hit=Tg.ray_cast(p+rn*.5,-rn,.5)
                dist=((hit[0]-p).dot(rn) if hit[0] is not None else d)+s.PAY_KOL
                dv=rn*max(dist,d+s.PAY_KOL) if d>.012 else nr*(d+s.PAY_KOL)
                # itme noktanın ön koldaki yerine göre paylaşılır: bileğe yakınsa bilek, dirseğe yakınsa dirsek dışarı
                t=max(0.,min(1.,(p-el0).dot(f)/max(f.length_squared,1e-6)))
                wr=w0+dv*max(t,.35); el_h=el0+dv*(1-t)*1.5; mid=(sh+wr)/2
                T['el'+K],T['wr'+K]=insan._ik(sh,wr,I.UA,I.FA,el_h-mid); deg=True
                s.rapor['kol'+K]=s.rapor.get('kol'+K,0)+1
            if not deg: return
        I.uygula(T); s.A.parmak()
    def _raket_nok(s,palm,rk,yuz):
        rk=rk.normalized(); fn=(yuz-rk*yuz.dot(rk)).normalized(); x=rk.cross(fn); P=[]
        for k in range(49): P.append(palm+rk*(-.11+k*.01))           # dip kapağı → şaft sonu (insan.raket ölçüleri, s=1)
        cy,a_,b_=.4758,.115,.098
        for k in range(40):
            t=2*math.pi*k/40; P.append(palm+x*(math.sin(t)*b_)+rk*(cy+math.cos(t)*a_))
        for u in (-.5,0,.5):
            for v in (-.6,0,.6): P.append(palm+x*(u*b_)+rk*(cy+v*a_))
        return P
    def raket(s,T):
        palm=s.A.avuc()
        To=s._agac({'govde','bacak','elL','kolL','ustL','ustR'})
        def acik(rk,yuz):
            en=9.
            for p in s._raket_nok(palm,rk,yuz):
                loc,nr,i,d=To.find_nearest(p,.05)
                if loc is None: continue
                en=min(en,d if (p-loc).dot(s._yon(To,loc,nr))>=0 else -d)
            return en
        a0=acik(T['rk'],T['rkFace'])
        if a0>=s.PAY_RAKET: return False
        rk=T['rk'].normalized(); u=rk.orthogonal().normalized(); v=rk.cross(u); en=(a0,0,None)
        for deg in range(4,44,4):
            for j in range(12):
                ax=(u*math.cos(j*math.pi/6)+v*math.sin(j*math.pi/6)).normalized(); R=Matrix.Rotation(math.radians(deg),3,ax)
                a=acik(R@rk,R@T['rkFace'])
                if a>en[0]+1e-4: en=(a,deg,R)
            if en[0]>=s.PAY_RAKET: break
        if en[2] is None: return False
        T['rk']=en[2]@rk; T['rkFace']=en[2]@T['rkFace']; s.rapor['raket']=max(s.rapor.get('raket',0),en[1]); return True
    def ayaklar(s,T):
        ao=s.A.ao; pb=ao.pose.bones; L=bpy.context.view_layer; mw=ao.matrix_world; kalan={}
        for K in 'RL':
            ft=pb[s.n('ft',K.lower())]; toe=pb.get(f'Bip01 {K} Toe0')
            zmin=lambda: min(p.z for p in s._nokta((f'{K} Foot',f'{K} Toe')))
            z0=zmin()
            if z0>=-s.PAY_ZEMIN: continue
            M0=ft.matrix.copy(); an=M0.translation; ileri=(M0.to_3x3()@Vector((0,1,0))); yan=Vector((0,0,1)).cross(ileri)
            if yan.length<1e-4: continue
            yan.normalize(); en=(z0,0)
            for a in sorted(range(-40,41,2),key=abs):
                N=Matrix.Translation(an)@Matrix.Rotation(math.radians(a),4,yan)@Matrix.Translation(-an)@M0; ft.matrix=N; L.update()
                z=zmin()
                if z>en[0]+1e-5: en=(z,a)
                if z>=-s.PAY_ZEMIN: break
            ft.matrix=Matrix.Translation(an)@Matrix.Rotation(math.radians(en[1]),4,yan)@Matrix.Translation(-an)@M0; L.update()
            if toe and en[0]<-s.PAY_ZEMIN:   # parmaklar: itişte burun kıvrılır
                T0=toe.matrix.copy(); tp=T0.translation; en2=en
                for a in range(4,52,4):
                    for sg in (1,-1):
                        toe.matrix=Matrix.Translation(tp)@Matrix.Rotation(math.radians(a*sg),4,yan)@Matrix.Translation(-tp)@T0; L.update(); z=zmin()
                        if z>en2[0]+1e-5: en2=(z,a*sg)
                    if en2[0]>=-s.PAY_ZEMIN: break
                toe.matrix=Matrix.Translation(tp)@Matrix.Rotation(math.radians(en2[1]),4,yan)@Matrix.Translation(-tp)@T0; L.update(); en=en2
            s.rapor['ayak'+K]=s.rapor.get('ayak'+K,0)+1
            if en[0]<-s.PAY_ZEMIN: kalan[K]=-en[0]
        return kalan
    def coz(s,T):
        s.kollar(T)
        if s.raket(T): s.A.I.uygula(T); s.A.parmak()
        for tur in range(2):
            kalan=s.ayaklar(T)
            if not kalan: return
            for K,dz in kalan.items(): T['an'+K]=T['an'+K]+Vector((0,0,dz+s.PAY_ZEMIN))
            s.A.I.uygula(T); s.A.parmak()

class Aday:
    def __init__(s,M,stil=None):
        # 2026-10-26: varsayılan Sports_Male_02 + badminton kıyafeti (rocketbox_kit.py). BS_ADAY=.../Sports_Male_04.fbx eski
        # atletli modeli birebir kurar (HTML'deki rocketbox sprite bloğu onunla üretildi).
        yol=os.environ.get('BS_ADAY',os.path.expanduser('~/AnimationLibrary/rocketbox/Sports_Male_02/Export/Sports_Male_02.fbx')); R=RIG[os.environ.get('BS_ADAY_RIG','rocketbox')]
        if yol.lower().endswith('.fbx'): bpy.ops.import_scene.fbx(filepath=yol,automatic_bone_orientation=True,use_image_search=True)
        else: bpy.ops.import_scene.gltf(filepath=yol,bone_heuristic='TEMPERANCE')   # kemik y ekseni çocuğa baksın (aim bunu varsayar)
        tum={os.path.basename(p).lower():p for p in glob.glob(os.path.dirname(os.path.dirname(yol))+'/**/*',recursive=True)}
        for im in bpy.data.images:   # FBX doku yolları Windows yolu: aynı ad diskte aranır
            if im.source=='FILE' and not im.packed_file and not os.path.exists(bpy.path.abspath(im.filepath)):
                b_=os.path.basename(im.filepath.replace('\\','/')).lower()
                if b_ in tum: im.filepath=tum[b_]; im.reload()
        ao=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); s.ao=ao
        if ao.animation_data: ao.animation_data.action=None
        mesh=[o for o in bpy.context.scene.objects if o.type=='MESH']
        s.kit=None
        if os.path.basename(yol)=='Sports_Male_02.fbx' and os.environ.get('BS_RB_KIT','1')=='1':
            import rocketbox_kit; s.kit=rocketbox_kit.dokular(mesh[0]); rocketbox_kit.taban_duzle(mesh[0]); _kit_malzeme(mesh[0],s.kit)
            _kemik_azalt(ao,mesh[0])
        for o in mesh: o['tag']='ten'
        bpy.context.view_layer.update()
        b=ao.data.bones; mw=ao.matrix_world
        bacak=(mw@b[R['ll'].format(S=R['S']['r'])].head_local-mw@b[R['ul'].format(S=R['S']['r'])].head_local).length+(mw@b[R['ft'].format(S=R['S']['r'])].head_local-mw@b[R['ll'].format(S=R['S']['r'])].head_local).length
        olc=.49/bacak   # insan.Oyuncu ile aynı: bacak boyu 0,49
        for o in mesh:
            mw_=o.matrix_world.copy(); o.parent=None; o.matrix_world=mw_
        D=Matrix.Rotation(math.pi,4,'Z')@Matrix.Scale(olc,4)
        for o in [ao]+mesh: o.matrix_world=D@o.matrix_world
        bpy.context.view_layer.update()
        bpy.ops.object.select_all(action='DESELECT')
        for o in [ao]+mesh: o.select_set(True)
        bpy.context.view_layer.objects.active=ao
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for o in mesh:
            if not any(m.type=='ARMATURE' for m in o.modifiers): m=o.modifiers.new('a','ARMATURE'); m.object=ao
            else:
                for m in o.modifiers:
                    if m.type=='ARMATURE': m.object=ao
        if os.environ.get('BS_ADAY_RIG','rocketbox')=='rocketbox': mesh+=rocketbox_giydir(ao,mesh,M,olc,s.kit)
        s.kalici=mesh; s.M=M; s.I=AdayIskelet(ao,R); s.gecici=[]; s.boy=.49/.49; s.R=R; s.stil={}
        s.temas=Temas(s) if os.environ.get('BS_RB_TEMAS','1' if s.kit else '0')=='1' else None
        s.doku_gomme=not s.kit   # kit: Unity dokuları Resources'tan okur (PlayerView.DokuMalzeme); FBX'e gömülmez (boyut)
        s.olc=olc
    def aksesuar(s):
        """2026-10-27 (yasin: Gerçekçi'de beden sabit, aksesuarlar değişsin): klipler pişirildikten SONRA çağrılır
        (unity_fbx.py, dışa aktarmadan önce): şort ve ayakkabı ten ağından ayrılır, beş ayakkabı modeli, başa/ön kola
        oturan bant ve bileklik (rocketbox_aksesuar.py). Pozlama sırasında yapılmaz: Temas katmanı gövdenin köşe
        numaralarını kurulumda tutar, ağ bölünürse kayar. Yalnız kit modelinde (Sports_Male_02); eski 04 aynen."""
        if not s.kit: return []
        import rocketbox_aksesuar
        ten=next(o for o in s.kalici if str(o.get('tag'))=='ten'); return rocketbox_aksesuar.uygula(s.ao,ten,s.olc)
    def parmak(s):
        pb=s.ao.pose.bones
        if 'Bip01 R Finger1' in pb:   # parmaklar: raket elinde sapı sarar, boştaki el yarı açık (iskeletin yerel X'i bükme ekseni)
            # kit: 45° (2026-10-26 ölçümü: 65°'de sapın içinde kalan el köşesi boşta 28 / smaçta 43, 45°'de 14 / 31; sap
            # oyun ölçeğinde 4,7 cm çaplı, parmak uçları sapın içine kapanıyordu; temas oranı %35–46 kalır)
            ac=float(os.environ.get('BS_RB_PARMAK','45' if s.kit else '65'))
            for yan,k in (('R',1.),('L',.35)):
                for f in range(5):
                    for e in ('','1','2'):
                        n=f'Bip01 {yan} Finger{f}{e}'
                        if n in pb: q=pb[n]; q.rotation_mode='XYZ'; q.rotation_euler=(0,0,0); q.rotation_euler[int(os.environ.get('BS_RB_EKSEN','2'))]=math.radians(-ac*k*(.6 if f==0 else 1))
            bpy.context.view_layer.update()
    def avuc(s):
        """raket sapının ekseni: el kemiğinin %60'ı (BS_RB_AVUC: avuç normali boyunca kaydırma, ölçüm için; 2026-10-26
        denemesi: ±1,2 cm kaydırma sapın içindeki el köşesini azaltmadı, yalnız teması bozdu → 0)."""
        pb=s.ao.pose.bones; h=pb[s.R['el'].format(S=s.R['S']['r'])]; p=h.head+(h.tail-h.head)*.6
        off=float(os.environ.get('BS_RB_AVUC','0'))
        if off and hasattr(s.I,'_avuc'):
            n=(h.matrix.to_3x3()@s.I._avuc[h.name]).normalized(); p=p+n*off
        return s.ao.matrix_world@p
    def kur_poz(s,p):
        for o in s.gecici: bpy.data.objects.remove(o)
        s.gecici=[]; T=s.I.hedef(p)
        if s.temas: s.temas.coz(T)
        else: s.I.uygula(T); s.parmak()
        s.T=T; pb=s.ao.pose.bones
        nes,s.kafa=insan.raket(s.avuc(),T['rk'],T['rkFace'],1.0,s.M); s.gecici+=nes
        return s.kalici+s.gecici

YUZ_KEMIK=('Eye','Jaw','Lip','Tongue','Mouth','Masseter','Caninus','Cheek','Eyebrow','Nose','Upperlip')
def _kemik_azalt(ao,o):
    """mobil bütçe (yasin 2026-10-26: "her telefonda akıcı"): 28 yüz kemiği (göz, çene, dudak, kaş; oyun yüz
    animasyonu kullanmıyor) ağırlıklarıyla Bip01 Head'e katılır → 80 kemik 52'ye iner; köşe başına en çok 4 kemik
    (Unity mobil kalite ayarı 2'ye kırpsa da sonuç önceden bilinsin), ağırlıklar normalize."""
    vg=o.vertex_groups; bas=vg['Bip01 Head']; sil=[b.name for b in ao.data.bones if any(k in b.name for k in YUZ_KEMIK)]
    for n in sil:
        g=vg.get(n)
        if g is None: continue
        for v in o.data.vertices:
            for e in v.groups:
                if e.group==g.index and e.weight>0: bas.add([v.index],e.weight,'ADD')
        vg.remove(g)
    bpy.ops.object.select_all(action='DESELECT'); ao.select_set(True); bpy.context.view_layer.objects.active=ao
    bpy.ops.object.mode_set(mode='EDIT')
    for n in sil: ao.data.edit_bones.remove(ao.data.edit_bones[n])
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4); bpy.ops.object.vertex_group_normalize_all(lock_active=False)

def _kit_malzeme(o,kit):
    """gövde/baş malzemesi: renk + normal kit dokularından; specular dokusu çıkar (Unity Standard sabit pürüzlülükle
    çiziyor: Blender önizlemesi oyundakiyle aynı ışığı görsün, FBX'e iki doku daha gömülmesin)."""
    for m in o.data.materials:
        nt=m.node_tree; b=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED'); bas='head' in m.name.lower()
        for n in list(nt.nodes):
            if n.type!='TEX_IMAGE' or not n.image: continue
            ad=os.path.basename(n.image.filepath.replace('\\','/')).lower()
            if '_specular' in ad: nt.nodes.remove(n); continue
            k=('bas_' if bas else '')+('normal' if '_normal' in ad else 'renk')
            n.image=bpy.data.images.load(kit[k],check_existing=True)
            if k.endswith('normal'): n.image.colorspace_settings.name='Non-Color'
        for i in ('Specular IOR Level','Roughness'):
            for l in list(b.inputs[i].links): nt.links.remove(l)
        b.inputs['Specular IOR Level'].default_value=.5   # Standard'ın dielektrik F0'ı (%4)
        pt=nt.nodes.new('ShaderNodeTexImage'); pt.image=bpy.data.images.load(kit['bas_parlak' if bas else 'parlak'],check_existing=True)
        pt.image.colorspace_settings.name='Non-Color'; inv=nt.nodes.new('ShaderNodeMath'); inv.operation='SUBTRACT'; inv.inputs[0].default_value=1
        uvn=next((l.from_node for n in nt.nodes if n.type=='TEX_IMAGE' for l in n.inputs['Vector'].links),None)
        if uvn: nt.links.new(uvn.outputs[0] if uvn.type!='UVMAP' else uvn.outputs['UV'],pt.inputs['Vector'])
        nt.links.new(pt.outputs['Color'],inv.inputs[1]); nt.links.new(inv.outputs[0],b.inputs['Roughness'])   # Unity: smoothness = 1 - pürüzlülük

def _forma_gri(m):
    """formanın dokusu griye çevrilir (oyun formayı maskeden seçilen renge boyar, gölgeyi parlaklıktan alır). Açık mavi
    panel ≈ %80 gri (sportif figürle aynı taban), koyu yan paneller daha koyu gri kalır: iki tonlu kesim renkte de görünür."""
    m=m.copy(); m.name='rb_forma'; nt=m.node_tree; bsdf=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
    lk=next(l for l in nt.links if l.to_socket==bsdf.inputs['Base Color']); src=lk.from_socket; nt.links.remove(lk)
    bw=nt.nodes.new('ShaderNodeRGBToBW'); mr=nt.nodes.new('ShaderNodeMapRange'); cv=nt.nodes.new('ShaderNodeCombineColor')
    a,b=(float(x) for x in os.environ.get('BS_RB_FORMA','0.01,0.12').split(','))
    mr.inputs['From Min'].default_value=a; mr.inputs['From Max'].default_value=b; mr.inputs['To Min'].default_value=float(os.environ.get('BS_RB_GMIN','.62')); mr.inputs['To Max'].default_value=.88
    nt.links.new(src,bw.inputs[0]); nt.links.new(bw.outputs[0],mr.inputs['Value'])
    for i in range(3): nt.links.new(mr.outputs['Result'],cv.inputs[i])
    nt.links.new(cv.outputs[0],bsdf.inputs['Base Color']); return m

def rocketbox_giydir(ao,mesh,M,olc,kit=None):
    """Rocketbox Sports_Male_02 / _04 (MIT, Microsoft 2020; lisans metni legal/ucuncu_taraf.md). Forma dokunun içinde ayrı
    ağ değil: forma yüzleri (02: rocketbox_kit sınıfları, 04: dokuda mavi olanlar) ayrı nesneye alınır ve 'forma' etiketi
    alır (oyun maskeden / takım rengiyle boyar). Paket bileklik ve
    kafa bandı getirmez: ekipmandaki bileklik ve bant için bizim halkalarımız eklenir."""
    import bmesh
    yeni=[]
    for o in mesh:
        for si,ms in enumerate(o.material_slots):
            m=ms.material; im=None
            if not m or not m.use_nodes: continue
            for n in m.node_tree.nodes:
                if n.type=='TEX_IMAGE' and n.image and 'body_color' in os.path.basename(n.image.filepath.replace('\\','/')).lower(): im=n.image   # adlar 'Map #21' gibi: dosya adına bak
            if kit:   # kit: forma + yaka yüzleri sınıf tablosundan (doku zaten gri, _forma_gri gerekmez)
                import rocketbox_kit as K
                if si!=0: continue   # sınıflar yalnız gövde malzemesinde; ayırmadan sonra yüz numaraları kayar
                sec=[i for i,c in kit['sinif'].items() if c in (K.FORMA,K.YAKA)]
            else:     # Sports_Male_04: dokuda mavi olan yüzler
                if not im: continue
                W,H=im.size; PX=im.pixels[:]; uv=o.data.uv_layers.active.data; sec=[]
                for f in o.data.polygons:
                    if f.material_index!=si: continue
                    u=sum(uv[l].uv.x for l in f.loop_indices)/f.loop_total; v=sum(uv[l].uv.y for l in f.loop_indices)/f.loop_total
                    j=(min(H-1,max(0,int(v*H)))*W+min(W-1,max(0,int(u*W))))*4; r,g,b=PX[j:j+3]
                    if b>r+.08 and b>g: sec.append(f.index)
            if not sec: continue
            bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
            bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT'); bpy.ops.object.mode_set(mode='OBJECT')
            for i in sec: o.data.polygons[i].select=True
            onc=set(bpy.context.scene.objects)
            bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.separate(type='SELECTED'); bpy.ops.object.mode_set(mode='OBJECT')
            for fo in set(bpy.context.scene.objects)-onc:
                fo['tag']='forma'
                if kit: fm=m.copy(); fm.name='rb_forma'
                else: fm=_forma_gri(m)
                for k in range(len(fo.data.materials)): fo.data.materials[k]=fm
                yeni.append(fo)
    pb=ao.pose.bones; L=bpy.context.view_layer
    def halka(ad,kemik,merkez,eksen,r,boy,tag,mat):
        bpy.ops.mesh.primitive_cylinder_add(vertices=24 if kit else 40,radius=r,depth=boy,location=merkez); h=bpy.context.object; h.name=ad
        h.rotation_mode='QUATERNION'; h.rotation_quaternion=Vector((0,0,1)).rotation_difference(eksen); bpy.ops.object.transform_apply(rotation=True)
        bpy.ops.object.shade_smooth(); h.data.materials.append(mat); h['tag']=tag
        vg=h.vertex_groups.new(name=kemik); vg.add(list(range(len(h.data.vertices))),1,'REPLACE'); md=h.modifiers.new('a','ARMATURE'); md.object=ao; return h
    b=ao.data.bones; fa=b['Bip01 R Forearm']; el=b['Bip01 R Hand']; d=(el.head_local-fa.head_local)
    rb=float(os.environ.get('BS_RB_BILEK','.043'))*olc
    yeni.append(halka('rb_bileklik','Bip01 R Forearm',el.head_local-d*.12,d.normalized(),rb,.09*olc,'bilek',M['bilek']))
    hb=b['Bip01 Head']; hc=hb.head_local+Vector((0,0,float(os.environ.get('BS_RB_BANT_Z','.13'))*olc))   # ölçüler paketin metresiyle × olc
    bpy.ops.mesh.primitive_torus_add(major_radius=float(os.environ.get('BS_RB_BANT','.096'))*olc,minor_radius=.014*olc,major_segments=32 if kit else 48,minor_segments=6 if kit else 10,location=hc)
    t=bpy.context.object; t.name='rb_bant'; t.scale=(1,1.12,.9); t.rotation_euler=(math.radians(-8),0,0); bpy.ops.object.transform_apply(rotation=True,scale=True)
    bpy.ops.object.shade_smooth(); t.data.materials.append(M['bant']); t['tag']='bant'
    vg=t.vertex_groups.new(name='Bip01 Head'); vg.add(list(range(len(t.data.vertices))),1,'REPLACE'); md=t.modifiers.new('a','ARMATURE'); md.object=ao
    yeni.append(t)
    return yeni
