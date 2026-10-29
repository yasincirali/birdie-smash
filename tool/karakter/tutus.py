# Raket tutuşu ve parmak pozları (2026-10-26, yasin: "Raket tutuş kısmı modelde net ve detaylı olmalı, 5 parmak da yine
# tutuşa göre badmintonun doğasındaki gibi olmalı"). Kaynaklar ve ölçüler: /mnt/project-files/birdie-smash/arastirma/
# tutus_notlari.md. Yalnız Oyuncak 3B (oyuncak.py OyIskelet) kullanır; poz sözlükleri (karakter.py) değişmez.
#
# Neden ayrı katman: eskiden raket dünyada pozun 'rk'/'rkFace' yönüyle kurulup avuç noktasına konuyordu, el ise ön kol
# yönünde nişanlanıyordu. İkisi arasında bağ yoktu: sap avucun içinden ya da el sırtından geçebiliyor, parmaklar sabit 58°
# kapanıp sapın içine giriyor ya da havada kalıyordu. Şimdi sırası tersine:
#  1. Tutuş, el kemiğinin kendi çerçevesinde bir sap ekseni (yön + avuç noktası) ve raket yüzü tanımlar (el sıkışma,
#     başparmak, panhandle, parmak kontrolü). Eksen anatomiden ölçülür (işaret kökü → hipotenar topuk), tipten tipe kayar.
#  2. Pozun istediği raket çerçevesinden (rk, rkFace) elin dünya yönü çıkar; dönüşün ön kol eksenindeki payı ön kola
#     (insan.on_kol_burgu, burgu=1 gibi), kalan bükülme bileğe verilir ve bilek açıları anatomik sınırla kırpılır.
#     Kırpma olursa raket ele uyar (raket = el ∘ tutuş): sap hiçbir pozda avuçtan kopmaz.
#  3. Parmaklar sapa "kayış gibi" sarılır: her boğum, ekleminden sap yüzeyine teğet olana kadar bükülür (bir sonraki boğum
#     onun ucundan başlar). "Değene kadar bük" tek açıyla bütün parmağı büküp kalın sapta parmağı düz bırakıyordu.
#     Sargı yarıçapı = sap yarıçapı + ölçülen parmak kalınlığı + gevşeklik. Deri ağıyla son denetim: girme varsa o parmağın
#     yarıçapı büyür, avuç girerse sap avuçtan uzaklaşır.
#  4. Gerilim: vuruşlar arası gevşek (parmaklar 2,5 mm açık, avuçla sap arasında boşluk), temasa doğru sıkılaşır, temasta tam
#     kapalı, sonra bırakır (kaynakların ortak kuralı: "gevşek tut, yalnız temasta sık").
#  5. Sol el: serviste top tüylerinden başparmak ve işaret parmağı arasında tutulur (çimdik), diğer parmaklar gevşek kıvrık;
#     servis dışında yarı açık, rahat el.
import math, re, os
import bpy
from mathutils import Vector, Matrix, Quaternion
E=lambda k,d: float(os.environ.get(k,d))
PARMAK=('thumb','index','middle','ring','pinky')

class Adlar:
    """kemik adları. 'ue' (Oyuncak 3B, UE adlı iskelet) varsayılan. 'rb' (2026-10-29, Gerçekçi = Rocketbox Bip01): yasin
    "tutuş da elin içinden geçiyor" dedi; Gerçekçi model parmakları tek açıyla büküp sapı el kemiğinin %60'ına koyuyordu
    (ölçüm: sap deriye 2,3 cm giriyor, avuç ve orta/yüzük parmağı sapın içinde). Aynı tutuş katmanı ad eşlemesiyle
    kullanılır: Finger0..4 = başparmak..serçe, boğumlar '', '1', '2'; köşe grupları kemikle aynı adda."""
    def __init__(s,tur='ue'): s.tur=tur
    def el(s,k): return f'hand_{k}' if s.tur=='ue' else f'Bip01 {k.upper()} Hand'
    def onkol(s,k): return f'lowerarm_{k}' if s.tur=='ue' else f'Bip01 {k.upper()} Forearm'
    def p(s,f,j,k):
        return f'{f}_0{j}_{k}' if s.tur=='ue' else f'Bip01 {k.upper()} Finger{PARMAK.index(f)}'+('','1','2')[j-1]
    def coz(s,nm):
        """köşe grubu / kemik adı → (bölüm, el): bölüm 'avuc' ya da parmak adı; ikisi de değilse None"""
        if s.tur=='ue':
            m=re.match(r'(thumb|index|middle|ring|pinky)_0\d_([rl])$',nm)
            if m: return m.group(1),m.group(2)
            m=re.match(r'hand_([rl])$',nm); return ('avuc',m.group(1)) if m else None
        m=re.match(r'Bip01 ([RL]) Finger(\d)\d?$',nm)
        if m: return PARMAK[int(m.group(2))],m.group(1).lower()
        m=re.match(r'Bip01 ([RL]) Hand$',nm); return ('avuc',m.group(1).lower()) if m else None
UE=Adlar()

# klip → tutuş. Ön: el sıkışma (forehand). bh: başparmak (backhand, başparmak geniş arka yüzde). kil: file önü öldürme
# (panhandle'a yakın, yüz fileye). parmak: file vuruşu, parmak uçlarıyla kontrol, el sapta yukarı kayar.
KLIP_TUTUS={'bh_over':'bh','bh_drive':'bh','bh_kill':'bh','bh_low':'bh','bh_lunge':'bh','block':'bh','serve_bh':'bh',
            'serve_flick':'bh','kill':'kil','net':'parmak'}
# tutuş parametreleri: yuz = raket yüz normalinin avuç normalinden sap ekseni çevresinde dönüşü (°; + başparmak yanına);
# dip = sapın dip ucundan hipotenar topuğa (model birimi; küçük = sapın dibinden güç tutuşu); bas = başparmak biçimi;
# isaret = işaret parmağının açılması (°, tetik); bosluk = avuçla sap arası ek boşluk (m. birimi).
TIP={'on':    dict(yuz=-8,  dip=.010, bas='sar',  isaret=13, bosluk=.0010, egim=0),
     'bh':    dict(yuz=62,  dip=.016, bas='duz',  isaret=4,  bosluk=.0016, egim=-6),
     'kil':   dict(yuz=-38, dip=.026, bas='sar',  isaret=10, bosluk=.0012, egim=0),
     'parmak':dict(yuz=-18, dip=.040, bas='uc',   isaret=16, bosluk=.0030, egim=4)}
# bilek sınırları (°): fleksiyon/ekstansiyon, ulnar/radyal sapma, bilekte kalan burulma (stilize: gerçekte ~75/70/35/20)
SINIR=dict(flex=E('BS_TT_FLEX','80'),ext=E('BS_TT_EXT','75'),uln=E('BS_TT_ULN','55'),rad=E('BS_TT_RAD','25'),tw=E('BS_TT_TW','35'))

def gerilim(bilgi):
    """(klip, faz, hit, n) → 0..1. Faz kare cinsinden (ara kareler kesirli). Temastan 2 kare önce sıkılaşmaya başlar,
    temasta 1, temastan 1,5 kare sonra gevşer. Vuruşsuz klip: 0,3 (hazır, rahat)."""
    if not bilgi: return .3
    klip,faz,hit,n=bilgi
    if hit is None: return .3
    d=faz-hit
    if d<-2.5: return .3
    if d<0: u=(d+2.5)/2.5; return .3+.7*u*u
    if d<1.5: return 1.
    if d<3.5: return 1-.7*(d-1.5)/2.
    return .3

def _ss(a,b,x):
    t=max(0.,min(1.,(x-a)/(b-a))); return t*t*(3-2*t)

class El:
    """Bir elin dinlenme ölçüleri (el kemiği yerel çerçevesinde): r radyal (başparmak yanı), a uzun eksen (bilek → orta
    parmak kökü), n avuç normali. Parmak kalınlığı ve avuç derinliği deri ağından ölçülür."""
    def __init__(s,ao,k,govde,avuc_dunya,ad=UE):
        s.k=k; B=ao.data.bones; s.ao=ao; s.ad=ad
        hb=B[ad.el(k)]; s.hb=hb; M=hb.matrix_local; Mi=M.inverted(); R3=M.to_3x3(); Ri=R3.inverted()
        W=hb.head_local
        loc=lambda p: Mi@p
        mi,mm,mr,mp=(B[ad.p(f,1,k)].head_local for f in ('index','middle','ring','pinky'))
        a=(Ri@(mm-W)).normalized(); n=(Ri@Vector(avuc_dunya)).normalized(); n=(n-a*n.dot(a)).normalized()
        r=a.cross(n) if k=='r' else n.cross(a)
        if r.dot(Ri@(mi-mp))<0: r=-r
        s.a,s.n,s.r=a,n,r
        s.Lp=(mm-W).length; s.wp=(mi-mp).length
        s.mcp={f:loc(B[ad.p(f,1,k)].head_local) for f in PARMAK}
        s.uzun={f:sum(B[ad.p(f,j,k)].length for j in (1,2,3)) for f in PARMAK}
        s.rot=lambda v: R3@v
        # deri ölçüsü (dinlenme ağı): parmak yarıçapı (kemik çizgisine medyan uzaklık), avucun avuç yüzü derinliği
        gi={g.index:g.name for g in govde.vertex_groups}; me=govde.data
        kal={f:[] for f in PARMAK}; avuc=[]
        for v in me.vertices:
            if not v.groups: continue
            g=max(v.groups,key=lambda x:x.weight); nm=gi[g.group]
            m_=ad.coz(nm)
            if m_ and m_[1]==k and m_[0]!='avuc' and g.weight>.6:
                b=B[nm]; h,t=b.head_local,b.tail_local; d=t-h; u=max(0.,min(1.,(v.co-h).dot(d)/d.length_squared))
                kal[m_[0]].append((v.co-(h+d*u)).length)
            elif nm==ad.el(k) and g.weight>.6:
                q=loc(v.co); avuc.append(q)
        s.yaricap={f:(sorted(x)[len(x)//2] if x else .006) for f,x in kal.items()}
        s.avuc_pts=avuc
        # avuç yüzü derinliği (n yönünde), avuç ortasında ve topukta; sap ekseni bu yüzeyin sap yarıçapı kadar önünde
        def derin(al,rh,R_=.016):
            q=[p for p in avuc if abs(p.dot(a)-al)<R_ and abs(p.dot(r)-rh)<R_*1.3]
            return max((p.dot(n) for p in q),default=.012)
        s.derin=derin
        print('tutus: el',k,'avuç boyu',round(s.Lp*175,1),'cm  genişlik',round(s.wp*175,1),'parmak yarıçapı (cm)',
              {f:round(x*175,2) for f,x in s.yaricap.items()})

    def eksen(s,tip,gr,gev):
        """tutuşun sap ekseni el kemiği yerelinde: (h yön, C avuç noktası eksen üstünde, t_C: C'nin raket yerel y'si)
        A: işaret parmağının proksimal boğum tabanı (V'nin dibi), B: hipotenar topuk. Eksen avuç düzleminde A-B
        doğrultusunda, derinlik = avuç yüzü + sap yarıçapı + boşluk."""
        P=TIP[tip]; a,n,r=s.a,s.n,s.r
        mi=s.mcp['index']; mp=s.mcp['pinky']
        A=mi-a*E('BS_TT_A','.006')                                  # işaret kökünün avuç çizgisi (V'nin dibi)
        B_=a*(E('BS_TT_BA','.42')*s.Lp)+r*(mp.dot(r)-E('BS_TT_BR','.03')*s.wp)   # hipotenar topuk
        ra,aa=A.dot(r),A.dot(a); rb,ab=B_.dot(r),B_.dot(a)
        h=(r*(ra-rb)+a*(aa-ab)).normalized()
        # eğim: sap avuç düzleminden hafif çıkar (+: baş ucu avuçtan uzaklaşır)
        h=(Quaternion(h.cross(n).normalized(),math.radians(P['egim']))@h).normalized()
        al_c=.55*s.Lp; t=(al_c-ab)/max(1e-6,h.dot(a)); C2=B_+h*t; C2=r*C2.dot(r)+a*C2.dot(a)
        d=s.derin(E('BS_TT_TOPUK','.30')*s.Lp,rb)*E('BS_TT_DERIN','1')   # yalnız hipotenar topuk: sap avuç ortasına değil parmak köklerine oturur, avuç ortasında boşluk kalır ("parmaklarla tut")
        C=C2+n*(d+gr+P['bosluk']+gev)
        # raket yerel y: sap dibi y0 (insan.raket), hipotenar topuk dipten 'dip' kadar yukarıda
        tB=(C-(B_+n*C.dot(n))).dot(h)   # C'nin B'ye göre eksen boyunca uzaklığı
        return h,C,tB

def yuz_yerel(s_el,h,tip,isaret):
    """raket yüz normali el yerelinde: avuç normali, sap ekseni çevresinde TIP.yuz kadar başparmak yanına döner"""
    n=s_el.n; n=(n-h*n.dot(h)).normalized(); yon=1 if (h.cross(n)).dot(s_el.r)>0 else -1
    return (Quaternion(h,math.radians(TIP[tip]['yuz'])*yon*isaret)@n).normalized()

# ---------- açı yardımcıları ----------
def swing_twist(q,u):
    qv=Vector((q.x,q.y,q.z)); p=u*qv.dot(u); tw=Quaternion((q.w,p.x,p.y,p.z))
    if tw.magnitude<1e-9: tw=Quaternion()
    tw.normalize(); sw=q@tw.inverted(); return sw,tw
def q_aci(q,u):
    """q'nun u ekseni çevresindeki işaretli açısı (q yalnız u çevresinde dönüşse)"""
    a=2*math.atan2(Vector((q.x,q.y,q.z)).dot(u),q.w); return (a+math.pi)%(2*math.pi)-math.pi

def bilek_kirp(Q,f,r,n,k):
    """Q: bilek eklemi dönüşü (el nötr çerçevesinde). f ön kol ekseni, r radyal, n avuç (aynı çerçevede).
    Burulma ±tw, fleksiyon/ekstansiyon (r ekseni) ve sapma (n ekseni) ayrı ayrı kırpılır. Döner: (Q', aşım derecesi)"""
    sw,tw=swing_twist(Q,f); at=q_aci(tw,f)
    lim=math.radians(SINIR['tw']); at2=max(-lim,min(lim,at))
    # swing'i r ve n bileşenlerine ayır (küçük açı yaklaşımı yerine eksen-açı vektörü)
    ax,ang=sw.to_axis_angle(); v=ax*ang
    fl=v.dot(r); dv=v.dot(n)
    # işaretler: +r çevresinde dönüş el ucunu avuca (+n) mı götürüyor?
    sf=1 if (Quaternion(r,.1)@f).dot(n)>0 else -1
    su=1 if (Quaternion(n,.1)@f).dot(-r)>0 else -1
    fx=fl*sf; ux=dv*su
    fx2=max(-math.radians(SINIR['ext']),min(math.radians(SINIR['flex']),fx))
    ux2=max(-math.radians(SINIR['rad']),min(math.radians(SINIR['uln']),ux))
    v2=r*(fx2*sf)+n*(ux2*su)+f*v.dot(f)
    sw2=Quaternion(v2.normalized(),v2.length) if v2.length>1e-9 else Quaternion()
    asim=math.degrees(max(abs(at-at2),abs(fx-fx2),abs(ux-ux2)))
    return sw2@Quaternion(f,at2),asim,(math.degrees(fx),math.degrees(ux),math.degrees(at))

# ---------- parmak sargısı ----------
# Parmak kinematiği elle (ileri kinematik, depsgraph güncellemesi yok): her boğum açısı denemesinde view_layer.update()
# bütün derili ağı yeniden büküyordu (poz başına ~400 güncelleme). Kemik dünya matrisi = ebeveyn @ dinlenme göreli @ dönüş.
def _seg_eksen(P0,P1,C,h,n=6):
    """[P0,P1] doğru parçasının (C,h) eksenine en küçük uzaklığı"""
    best=1e9
    for i in range(n+1):
        d=P0.lerp(P1,i/n)-C; best=min(best,(d-h*d.dot(h)).length)
    return best

class Parmaklar:
    """parmak boğumlarını sap silindirine sarar, sol elde çimdik kurar. Çözüm her poz için açı sözlüğüdür (kemik adı →
    yerel kuaterniyon); uygula() bunları rotation_quaternion'a yazar."""
    def __init__(s,ao,el_r,el_l,ad=UE):
        s.ao=ao; s.ad=ad; s.el={'r':el_r,'l':el_l}; s.ekR={'r':{},'l':{}}; s.kof={}
        B=ao.data.bones; s.flex={}; s.rel={}; s.boy={}; s.yan={}
        for k,el in s.el.items():
            nw=el.rot(el.n)   # dinlenme (armatür) avuç normali
            for f in PARMAK:
                for j in (1,2,3):
                    nm=ad.p(f,j,k); s.kof[nm]=k; b=B[nm]; d=(b.tail_local-b.head_local).normalized()
                    # başparmak avuca değil işaret parmağının köküne doğru kapanır: avuç normali ile radyalin karışımı
                    nn=(nw-el.rot(el.r)*.6).normalized() if f=='thumb' else nw
                    s.flex[nm]=(b.matrix_local.to_3x3().inverted()@d.cross(nn).normalized()).normalized()   # +açı = kapanma
                    s.yan[nm]=(b.matrix_local.to_3x3().inverted()@nw).normalized()                            # abduksiyon ekseni
                    s.rel[nm]=b.parent.matrix_local.inverted()@b.matrix_local; s.boy[nm]=b.length
        s.Q={}
    # --- ileri kinematik
    def fk(s,Hm,zincir,Q):
        out=[]; M=Hm
        for nm in zincir:
            M=M@s.rel[nm]@Q.get(nm,Quaternion()).to_matrix().to_4x4()
            out.append((M.translation.copy(),(M@Vector((0,s.boy[nm],0))),M))
        return out
    def Hm(s,k):
        return s.ao.matrix_world@s.ao.pose.bones[s.ad.el(k)].matrix
    def uygula(s,k=None):
        pb=s.ao.pose.bones
        for nm,q in s.Q.items():
            if k and s.kof.get(nm)!=k: continue
            pb[nm].rotation_mode='QUATERNION'; pb[nm].rotation_quaternion=q
    def q(s,nm,aci,yan=0.):
        q=Quaternion(s.flex[nm],aci)
        return Quaternion(s.yan[nm],yan)@q if yan else q
    # --- sargı
    def sar(s,k,C,h,Rs,gev,tip):
        """Parmakları sap eksenine (C nokta, h yön; dünya) sar. Rs: sap yarıçapı, gev: gevşeklik payı. Her boğum
        [alt, üst] açı aralığında sap yüzeyine (sap + parmak yarıçapı + gevşeklik) teğet olduğu en küçük açıyı alır."""
        el=s.el[k]; P=TIP[tip]; H=s.Hm(k); out={}
        for f in ('index','middle','ring','pinky'):
            R=Rs+el.yaricap[f]+gev+s.ekR[k].get(f,0.)
            ab=math.radians(P['isaret']) if f=='index' else math.radians({'middle':0,'ring':-3,'pinky':-6}[f])
            ust=dict(index=(85,95,70),middle=(90,105,75),ring=(90,105,75),pinky=(95,105,75))[f]
            zn=[s.ad.p(f,j,k) for j in (1,2,3)]; Q={}
            # Sargı bir en iyileme: MCP açısı a1 ve PIP açısı a2 (DIP = 0,7·PIP, anatomik bağ) taranır; ceza = parmak
            # kemik çizgisinin sap ekseninden R'nin içine girmesi (×60, kare) + R'den uzaklığı (×1, kare; parmak sapa
            # yaslansın). Boğum boğum "değene kadar bük" kuralları çapraz sapta kırılıyordu: sap parmak düzlemini eğik
            # kestiği için boğumun ortası yüzeye hiç gelmeden ucu sapın içine giriyordu (ölçüm: orta parmak −1,2 cm).
            m1,m2,m3=ust; yan=ab
            def E_(a1,a2):
                Q[zn[0]]=s.q(zn[0],a1,yan); Q[zn[1]]=s.q(zn[1],a2); Q[zn[2]]=s.q(zn[2],min(math.radians(m3),.7*a2))
                ch=s.fk(H,zn,Q); e=0.
                for ji,(p0,p1,_) in enumerate(ch):
                    for u in ((.5,.75,1.) if ji==0 else (.25,.5,.75,1.)):
                        d=p0.lerp(p1,u)-C; d=(d-h*d.dot(h)).length-R
                        e+=60*d*d if d<0 else (.35 if f=='index' else 1.)*d*d
                return e
            best=None
            for i1 in range(12):
                a1=math.radians(-10+(m1+10)*i1/11)
                for i2 in range(12):
                    a2=math.radians(m2*i2/11); e=E_(a1,a2)
                    if best is None or e<best[0]: best=(e,a1,a2)
            _,a1,a2=best; st=math.radians(5)
            for _ in range(10):   # yerel inceltme
                imp=False
                for d1,d2 in ((st,0),(-st,0),(0,st),(0,-st)):
                    b1=max(math.radians(-10),min(math.radians(m1),a1+d1)); b2=max(0.,min(math.radians(m2),a2+d2)); e=E_(b1,b2)
                    if e<best[0]: best=(e,b1,b2); a1,a2=b1,b2; imp=True
                if not imp: st*=.5
            if f=='index': a2*=.85   # tetik: orta boğum biraz açık, işaret parmağı sapı kancayla tutar
            Q[zn[0]]=s.q(zn[0],a1,yan); Q[zn[1]]=s.q(zn[1],a2); Q[zn[2]]=s.q(zn[2],min(math.radians(m3),.7*a2))
            out.update({zn[0]:math.degrees(a1),zn[1]:math.degrees(a2),zn[2]:math.degrees(min(math.radians(m3),.7*a2))})
            s.Q.update(Q)
        s._bas(k,C,h,Rs,gev,P['bas'])
        return out
    def _ccd(s,k,zincir,hedef,sinir,serbest0=False,tur=12):
        """CCD: uç (son kemiğin kuyruğu) hedefe; ilk kemik serbest0 ise her eksende döner (≤12°/adım), diğerleri kendi
        kapanma ekseninde [lo,hi]. Açılar s.Q'ya yazılır."""
        H=s.Hm(k); aci={nm:0. for nm in zincir}
        for nm in zincir: s.Q[nm]=Quaternion() if (serbest0 and nm==zincir[0]) else s.q(nm,0.)
        for _ in range(tur):
            for i in reversed(range(len(zincir))):
                nm=zincir[i]; ch=s.fk(H,zincir,s.Q); J,_,M=ch[i]; uc=ch[-1][1]
                v1=uc-J; v2=hedef-J
                if v1.length<1e-7 or v2.length<1e-7: continue
                if serbest0 and i==0:
                    qw=v1.normalized().rotation_difference(v2.normalized()); ang=min(qw.angle,math.radians(12))
                    if ang<1e-6: continue
                    qw=Quaternion(qw.axis,ang)
                    PR=(H@s.rel[nm]).to_3x3().normalized()   # ebeveyn @ dinlenme göreli: yerel dönüş = PR⁻¹ qw PR B
                    B_=PR.inverted()@qw.to_matrix()@PR@s.Q[nm].to_matrix()
                    s.Q[nm]=B_.to_quaternion(); continue
                axw=(M.to_3x3().normalized()@s.flex[nm]).normalized()
                a1=v1-axw*v1.dot(axw); a2=v2-axw*v2.dot(axw)
                if a1.length<1e-7 or a2.length<1e-7: continue
                d=math.atan2(axw.dot(a1.cross(a2)),a1.dot(a2))
                lo,hi=sinir.get(nm,(0.,math.radians(100)))
                aci[nm]=max(lo,min(hi,aci[nm]+d)); s.Q[nm]=s.q(nm,aci[nm])
        return aci
    def _bas(s,k,C,h,Rs,gev,bicim):
        """başparmak: 'sar' (el sıkışma: sapın üstünden orta parmağın orta boğumuna), 'duz' (backhand: ped geniş arka yüzde,
        sap boyunca uzanık), 'uc' (parmak kontrolü: ucu sapın yanında, işaret parmağının karşısında)"""
        el=s.el[k]; H=s.Hm(k); R3=H.to_3x3().normalized()
        nw=(R3@el.n).normalized(); rw=(R3@el.r).normalized()
        nperp=(nw-h*nw.dot(h)).normalized(); rperp=(rw-h*rw.dot(h)).normalized()
        R=Rs+el.yaricap['thumb']+gev+s.ekR[k].get('thumb',0.)
        J=(H@s.rel[s.ad.p('thumb',1,k)]).translation; tJ=(J-C).dot(h)
        if bicim=='duz':
            hed=C+h*(tJ+.62*el.uzun['thumb'])+(rperp*.80+nperp*.20).normalized()*R
        elif bicim=='uc':
            hed=C+h*(tJ+.55*el.uzun['thumb'])+(nperp*.55+rperp*.45).normalized()*(R+.002)
        else:
            hed=C+h*(tJ+.45*el.uzun['thumb'])+(nperp*.70+rperp*.30).normalized()*(R+.2*el.yaricap['middle'])
        zn=[s.ad.p('thumb',j,k) for j in (1,2,3)]
        sin={zn[1]:(math.radians(-5),math.radians(55 if bicim!='duz' else 22)),zn[2]:(math.radians(-10),math.radians(60 if bicim!='duz' else 12))}
        s._ccd(k,zn,hed,sin,serbest0=True,tur=10)
    def rahat(s,k,kat=1.):
        """boş el: hafif kıvrık, serçeye doğru artan (doğal dinlenme kaskadı)"""
        for f,ac in dict(index=(12,20,10),middle=(16,26,14),ring=(20,30,16),pinky=(24,34,18),thumb=(6,10,8)).items():
            for j,a in zip((1,2,3),ac): s.Q[s.ad.p(f,j,k)]=s.q(s.ad.p(f,j,k),math.radians(a*kat))
    def cimdik(s,k,P,u=None):
        """sol el çimdik: işaret ve başparmak pedleri P'nin iki yanında (u yönünde; aralarında tüy duvarı), diğerleri
        gevşek kıvrık"""
        s.rahat(k,1.5)
        el=s.el[k]; R3=s.Hm(k).to_3x3().normalized(); nw=(R3@el.n).normalized(); rw=u if u is not None else (R3@el.r).normalized()
        zi=[s.ad.p('index',j,k) for j in (1,2,3)]; zt=[s.ad.p('thumb',j,k) for j in (1,2,3)]
        s._ccd(k,zi,P+rw*(el.yaricap['index']+.0012),{zi[0]:(0.,math.radians(80)),zi[1]:(0.,math.radians(80)),zi[2]:(0.,math.radians(35))})
        s._ccd(k,zt,P-rw*(el.yaricap['thumb']+.0012),{zt[1]:(math.radians(-5),math.radians(40)),zt[2]:(math.radians(-15),math.radians(30))},serbest0=True)

# ---------- tutuş: el yönü, bilek, raket çerçevesi ----------
def _cerceve(y,z):
    """y ekseni ve ona dik z'den sağ elli 3×3 (sütunlar x, y, z)"""
    y=y.normalized(); z=(z-y*z.dot(y)).normalized(); x=y.cross(z); return Matrix((x,y,z)).transposed()

def _aci_ok(f,T,nd):
    import insan
    n0=f.cross((T['elR']-T['shR']).normalized())
    if n0.length<1e-5: return 0.
    a=insan._aci(f,n0.normalized(),nd); return a or 0.

class Tutus:
    """Oyuncak 3B'nin raket eli. kur(): iskelet ve deri hazır olunca bir kez. Her pozda: hazirla(T) (super().uygula'dan
    önce, ön kol burulma hedefi), el(T) (sonra: bilek, raket çerçevesi T['_rk'], T['_fn'], T['_el']), parmak(T)."""
    def __init__(s,ao,govde,avuc,raket_s,gr_kat,ad=UE):
        s.ao=ao; s.govde=govde; s.ad=ad; s.el={k:El(ao,k,govde,avuc[k],ad) for k in 'rl'}; s.P=Parmaklar(ao,s.el['r'],s.el['l'],ad)
        s.gr=.0135*raket_s*gr_kat; s.y0=-.20*raket_s*.55   # insan.raket: sap yarıçapı ve sap dibinin raket yerel y'si
        s.bilgi=None; s.asim=0.; s.son={}
    def _sinif(s):
        """deri köşesi → 'avuc' / parmak adı (sağ el, baskın ağırlık); bir kez"""
        if hasattr(s,'_vk'): return s._vk
        o=s.govde; gi={g.index:g.name for g in o.vertex_groups}; vk={}
        for v in o.data.vertices:
            if not v.groups: continue
            g=max(v.groups,key=lambda x:x.weight); nm=gi[g.group]
            m_=s.ad.coz(nm)
            if m_ and m_[1]=='r': vk[v.index]=m_[0]
        s._vk=vk; return vk
    def deri(s,T):
        """deri ağında sap silindirine işaretli uzaklık (model birimi; <0 içeride), sapın el bölgesinde. Döner {bölüm: min}"""
        vk=s._sinif(); o=s.govde; dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); me=e.to_mesh(); M=o.matrix_world
        C,h=T['_C'],T['_rk']; tC=s.y0+TIP[s.loc[0]]['dip']+s.loc[3]; lo=s.y0-tC; gL=-s.y0/.55   # sap: raket yerel y0..y0+gL
        out={}
        for i,k in vk.items():
            p=M@me.vertices[i].co; d=p-C; t=d.dot(h)
            if t<lo-.002 or t>lo+gL+.002: continue
            r=(d-h*t).length-s.gr; out[k]=min(out.get(k,9.),r)
        e.to_mesh_clear(); return out
    def tip(s):
        return KLIP_TUTUS.get(s.bilgi[0],'on') if s.bilgi else 'on'
    def gev(s):
        return (1-gerilim(s.bilgi))*E('BS_TT_GEV','.0016')   # gevşek: parmak ve avuç sapa ~2,8 mm (oyunda) uzak
    def yerel(s,tp=None):
        """seçili tutuşun el kemiği yerelindeki sap ekseni, avuç noktası, yüz normali"""
        tp=tp or s.tip(); el=s.el['r']; h,C,tB=el.eksen(tp,s.gr,s.gev()); fn=yuz_yerel(el,h,tp,1)
        return tp,h,C,tB,fn
    def hazirla(s,T):
        # Tutuş adayı: klibin tutuşu, yanında forehand↔backhand karşılığı (0,35 rad ceza). Pozun raket yönü klibin tutuşuyla
        # bileğe sığmıyorsa (bh_over hazırlığı: raket sırtta, başparmak tutuşuyla 90°+ radyal sapma isterdi) oyuncu gibi
        # tutuşu değiştirir; sığıyorsa klibinki kalır (ceza bunu garanti eder).
        el=s.el['r']; tp0=s.tip()
        hw=Vector(T['rk']).normalized(); fw=Vector(T['rkFace']); fw=(fw-hw*fw.dot(hw))
        if fw.length<1e-6: fw=Vector((1,0,0))-hw*hw.x
        fw.normalize()
        # raket iki yüzlü: yüz normalinin işareti serbest. burgu'lu pozda rkFace avucun bakacağı yön (karakter.py notu),
        # değilse bileğin en az büküldüğü (el uzun ekseni ön kola en yakın) seçilir; burgu'lu pozda avucun rkFace'e
        # bakması 0,5 rad (~29°) bükülmeye eşdeğer ödül alır (yön işaretini korusun ama aşırı bileği zorlamasın)
        f=(T['wrR']-T['elR']).normalized(); best=None
        # temas çevresinde (gerilim ≥ 0,9) klibin tutuşu kilitli: vuruşun okunduğu kare (yüksek backhand'de başparmak)
        adaylar=[tp0]+([] if gerilim(s.bilgi)>=.9 else {'on':['bh'],'bh':['on'],'kil':['bh'],'parmak':[]}.get(tp0,[]))
        for tp in adaylar:
            tp_,h,C,tB,fn=s.yerel(tp); Mb=_cerceve(h,fn)
            for sg in (1,-1):
                Rb=_cerceve(hw,fw*sg)@Mb.transposed(); nd=(Rb@el.n).normalized(); ad=(Rb@el.a).normalized()
                tw=_aci_ok(f,T,nd)   # ön kolun anatomik nötrden burulması; ±100° (insan.on_kol_burgu sınırı) ötesi bilekte kalır
                sc=-ad.angle(f)-1.5*max(0.,abs(tw)-math.radians(80))+(.5 if T.get('burgu',0)>0 and nd.dot(Vector(T['rkFace']))>0 else 0.)
                if tp!=tp0: sc-=.35
                if best is None or sc>best[0]: best=(sc,Rb,nd,sg,(tp_,h,C,tB,fn))
        s.Rb,s.nd,s.sg=best[1],best[2],best[3]
        s.loc=best[4]; s.secilen=best[4][0]
        T2=dict(T); T2['burgu']=1.; T2['rkFace']=s.nd.copy()
        return T2
    def el_kur(s,T):
        """bilek: el = istenen yön, eklem sınırında kırpılmış; raket çerçevesi elden"""
        ao=s.ao; pb=ao.pose.bones; B=ao.data.bones; L=bpy.context.view_layer; el=s.el['r']
        nla,nhb=s.ad.onkol('r'),s.ad.el('r'); la=pb[nla]; hb=pb[nhb]
        Rrel=B[nla].matrix_local.to_3x3().inverted()@B[nhb].matrix_local.to_3x3()
        N=la.matrix.to_3x3().normalized()@Rrel
        Q=(N.inverted()@s.Rb).to_quaternion()
        f=(N.inverted()@(la.matrix.to_3x3().normalized()@Vector((0,1,0)))).normalized()
        r=(el.r-f*el.r.dot(f)).normalized(); n=f.cross(r) if f.cross(r).dot(el.n)>0 else -f.cross(r)
        Q2,asim,acilar=bilek_kirp(Q,f,r,n,'r'); s.asim=asim; s.acilar=acilar
        M=(N@Q2.to_matrix()).to_4x4(); M.translation=hb.matrix.translation.copy(); hb.matrix=M; L.update()
        tp,h,C,tB,fn=s.loc
        Hm=ao.matrix_world@hb.matrix; R3=Hm.to_3x3().normalized()
        # Kırpma kalanı: bilek sınırda kaldıysa sap avuçta biraz kayar (gevşek tutuşta raket parmaklar arasında döner;
        # oyuncular bunu bilerek yapar). Kayma en çok BS_TT_KAYMA (20°); fazlasında raket ele uyar. Pozun raket yolu
        # böylece çoğu karede birebir korunur (topla temas noktası değişmez), avuçtaki eksen de anatomiden en çok 20° sapar.
        hd=(R3.inverted()@s.Rb@h).normalized(); fd=(R3.inverted()@s.Rb@fn).normalized()
        qk=_cerceve(h,fn).to_quaternion().rotation_difference(_cerceve(hd,fd).to_quaternion())
        if qk.w<0: qk.negate()   # kısa yol (açı ≤ 180°)
        # Kayma iki parça (2026-10-26 denetimi: bh_over'da bilek isteği 100°'yi aşıyor, tek 20° sınırla raket pozdan 100°
        # sapıp gövdeye 7-9 cm giriyordu): sap ekseni çevresindeki dönüş (burgu) parmaklar arasında sapı çevirmek, yani
        # oyuncunun tutuş değiştirmesi (sekizgen sap, forehand↔backhand geçişi) — BS_TT_BURGUK (70°) kadar serbest;
        # sapın avuçtan dışarı eğilmesi (salınım) gerçekte olmaz, BS_TT_KAYMA (20°) ile sınırlı.
        lim=math.radians(E('BS_TT_KAYMA','20')); limb=math.radians(E('BS_TT_BURGUK','70')); s.kayma=math.degrees(qk.angle)
        if qk.angle>1e-6:
            sw,tw=swing_twist(qk,h); a_tw=q_aci(tw,h); a_sw=sw.angle if sw.w>=0 else (-sw).angle
            if sw.w<0: sw.negate()
            tw2=Quaternion(h,max(-limb,min(limb,a_tw))); sw2=Quaternion(sw.axis,min(a_sw,lim)) if a_sw>1e-6 else Quaternion()
            q2=sw2@tw2; h=(q2@h).normalized(); fn=(q2@fn).normalized()
            s.kalan=math.degrees(max(0.,a_sw-lim)+max(0.,abs(a_tw)-limb))   # raketin pozdan sapması (°)
        else: s.kalan=0.
        hw=(R3@h).normalized(); fw=(R3@fn*s.sg).normalized(); Cw=Hm@C
        tC=s.y0+TIP[tp]['dip']+tB
        T['_rk']=hw; T['_fn']=fw; T['_C']=Cw; T['_el']=Cw-hw*tC
        return T
    def parmak(s,T,sol_top=None,denetim=True):
        """parmak sargısı + deri denetimi: deri ağı sapın içindeyse o parmağın sargı yarıçapı büyür (kemik çizgisine göre
        ölçülen yarıçap eklem şişkinliğini kaçırıyordu), avuç içerdeyse sap avuç normali yönünde dışarı alınır (raket de
        onunla). En çok 3 tur."""
        tp=s.loc[0]; s.P.ekR['r']={}; TOL=E('BS_TT_TOL','.0004'); PAY=.0005
        if sol_top is not None:
            el=s.el['l']; ah=(s.P.Hm('l').to_3x3()@el.a); ah.z=0; s.P.cimdik('l',sol_top,ah.normalized())
        else: s.P.rahat('l')
        for tur in range(4 if denetim else 1):
            s.son=s.P.sar('r',T['_C'],T['_rk'],s.gr,s.gev(),tp)
            s.P.uygula()
            if not denetim: break
            bpy.context.view_layer.update(); dr=s.deri(T); s.deri_son=dr
            kotu={f:v for f,v in dr.items() if v<-TOL}
            if not kotu or tur==3: break
            if 'avuc' in kotu:
                nw=(s.P.Hm('r').to_3x3()@s.el['r'].n).normalized(); nw=(nw-T['_rk']*nw.dot(T['_rk'])).normalized(); d=-kotu.pop('avuc')+PAY
                T['_C']=T['_C']+nw*d; T['_el']=T['_el']+nw*d
            for f,v in kotu.items():
                if f!='thumb': s.P.ekR['r'][f]=s.P.ekR['r'].get(f,0.)-v+PAY
                else: s.P.ekR['r']['thumb']=s.P.ekR['r'].get('thumb',0.)-v+PAY
    def cimdik_noktasi(s):
        """sol el çimdik noktası (dünya): avucun önünde, işaret kökünün ilerisinde, başparmak yanında"""
        el=s.el['l']; H=s.P.Hm('l'); m=el.mcp['index']
        p=m+el.a*(.55*el.uzun['index'])+el.n*(.030)+el.r*(.004)
        return H@p
