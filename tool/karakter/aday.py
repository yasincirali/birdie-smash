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
        yol=os.environ['BS_ADAY']; R=RIG[os.environ.get('BS_ADAY_RIG','rocketbox')]
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
        s.kalici=mesh; s.M=M; s.I=AdayIskelet(ao,R); s.gecici=[]; s.boy=.49/.49; s.R=R; s.stil={}
    def kur_poz(s,p):
        for o in s.gecici: bpy.data.objects.remove(o)
        s.gecici=[]; T=s.I.hedef(p); s.I.uygula(T); s.T=T; pb=s.ao.pose.bones
        h=pb[s.R['el'].format(S=s.R['S']['r'])]; palm=h.head+(h.tail-h.head)*.6
        nes,s.kafa=insan.raket(palm,T['rk'],T['rkFace'],1.0,s.M); s.gecici+=nes
        return s.kalici+s.gecici
