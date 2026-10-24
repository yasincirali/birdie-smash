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
            aim(s.n('el',k),T['wr'+K]+fa)
            aim(s.n('ul',k),T['kn'+K]); aim(s.n('ll',k),T['an'+K])
        set_rot(s.n('head'),Vector((0,1,0)).rotation_difference(T['look']).to_matrix())

class Aday:
    def __init__(s,M,stil=None):
        yol=os.environ.get('BS_ADAY',os.path.expanduser('~/AnimationLibrary/rocketbox/Sports_Male_04/Export/Sports_Male_04.fbx')); R=RIG[os.environ.get('BS_ADAY_RIG','rocketbox')]
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
        if os.environ.get('BS_ADAY_RIG','rocketbox')=='rocketbox': mesh+=rocketbox_giydir(ao,mesh,M,olc)
        s.kalici=mesh; s.M=M; s.I=AdayIskelet(ao,R); s.gecici=[]; s.boy=.49/.49; s.R=R; s.stil={}
    def kur_poz(s,p):
        for o in s.gecici: bpy.data.objects.remove(o)
        s.gecici=[]; T=s.I.hedef(p); s.I.uygula(T); s.T=T; pb=s.ao.pose.bones
        if 'Bip01 R Finger1' in pb:   # parmaklar: raket elinde sapı sarar, boştaki el yarı açık (iskeletin yerel X'i bükme ekseni)
            ac=float(os.environ.get('BS_RB_PARMAK','65'))
            for yan,k in (('R',1.),('L',.35)):
                for f in range(5):
                    for e in ('','1','2'):
                        n=f'Bip01 {yan} Finger{f}{e}'
                        if n in pb: q=pb[n]; q.rotation_mode='XYZ'; q.rotation_euler=(0,0,0); q.rotation_euler[int(os.environ.get('BS_RB_EKSEN','2'))]=math.radians(-ac*k*(.6 if f==0 else 1))
            bpy.context.view_layer.update()
        h=pb[s.R['el'].format(S=s.R['S']['r'])]; palm=h.head+(h.tail-h.head)*.6
        nes,s.kafa=insan.raket(palm,T['rk'],T['rkFace'],1.0,s.M); s.gecici+=nes
        return s.kalici+s.gecici

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

def rocketbox_giydir(ao,mesh,M,olc):
    """Rocketbox Sports_Male_04 (MIT, Microsoft 2020; lisans metni legal/ucuncu_taraf.md). Forma dokunun içinde ayrı ağ
    değil: dokuda mavi olan yüzler ayrı nesneye alınır ve 'forma' etiketi alır (oyun maskeden boyar). Paket bileklik ve
    kafa bandı getirmez: ekipmandaki bileklik ve bant için bizim halkalarımız eklenir."""
    import bmesh
    yeni=[]
    for o in mesh:
        for si,ms in enumerate(o.material_slots):
            m=ms.material; im=None
            if not m or not m.use_nodes: continue
            for n in m.node_tree.nodes:
                if n.type=='TEX_IMAGE' and n.image and 'body_color' in os.path.basename(n.image.filepath.replace('\\','/')).lower(): im=n.image   # adlar 'Map #21' gibi: dosya adına bak
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
                fo['tag']='forma'; fm=_forma_gri(m)
                for k in range(len(fo.data.materials)): fo.data.materials[k]=fm
                yeni.append(fo)
    pb=ao.pose.bones; L=bpy.context.view_layer
    def halka(ad,kemik,merkez,eksen,r,boy,tag,mat):
        bpy.ops.mesh.primitive_cylinder_add(vertices=40,radius=r,depth=boy,location=merkez); h=bpy.context.object; h.name=ad
        h.rotation_mode='QUATERNION'; h.rotation_quaternion=Vector((0,0,1)).rotation_difference(eksen); bpy.ops.object.transform_apply(rotation=True)
        bpy.ops.object.shade_smooth(); h.data.materials.append(mat); h['tag']=tag
        vg=h.vertex_groups.new(name=kemik); vg.add(list(range(len(h.data.vertices))),1,'REPLACE'); md=h.modifiers.new('a','ARMATURE'); md.object=ao; return h
    b=ao.data.bones; fa=b['Bip01 R Forearm']; el=b['Bip01 R Hand']; d=(el.head_local-fa.head_local)
    rb=float(os.environ.get('BS_RB_BILEK','.043'))*olc
    yeni.append(halka('rb_bileklik','Bip01 R Forearm',el.head_local-d*.12,d.normalized(),rb,.09*olc,'bilek',M['bilek']))
    hb=b['Bip01 Head']; hc=hb.head_local+Vector((0,0,float(os.environ.get('BS_RB_BANT_Z','.13'))*olc))   # ölçüler paketin metresiyle × olc
    bpy.ops.mesh.primitive_torus_add(major_radius=float(os.environ.get('BS_RB_BANT','.096'))*olc,minor_radius=.014*olc,major_segments=48,minor_segments=10,location=hc)
    t=bpy.context.object; t.name='rb_bant'; t.scale=(1,1.12,.9); t.rotation_euler=(math.radians(-8),0,0); bpy.ops.object.transform_apply(rotation=True,scale=True)
    bpy.ops.object.shade_smooth(); t.data.materials.append(M['bant']); t['tag']='bant'
    vg=t.vertex_groups.new(name='Bip01 Head'); vg.add(list(range(len(t.data.vertices))),1,'REPLACE'); md=t.modifiers.new('a','ARMATURE'); md.object=ao
    yeni.append(t)
    return yeni
