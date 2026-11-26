# Gövde içine girme koruması için model başına baş küresi (2026-11-21, yasin: "animasyonlarda karakterin skinlerinin içinden
# geçmesi konusunun çözülmesi"). Ölçüm: maskotlar Tüycük'ün kliplerini taşır (maskot.py), başları ise çok daha büyük: backhand
# drop/over'da raket eli başın arkasına 20–25 cm, backhand geri adımda 14 cm, round-the-head'de 5 cm giriyordu (bpy kareleri,
# kedi 529/843/1193). Oyuncak tiplerinde servis, clear ve round-the-head'de 8–10 cm. Unity PlayerView.GirmeKoru bu küreyle
# ön kolu ve eli başın dışına iter (üst kol omuzdan döner, raket elle gider).
# Küre: idle'ın ilk karesinde baş kemiğine ağırlıklı gövde köşelerine en küçük kareler küresi, yarıçap köşe uzaklıklarının
# ortancası (yüzeye oturur; %90'lık yarıçap yüzü ve çeneyi şişirip servis ve smaçta kolu boşuna itiyordu).
# Çıktı Resources/karakter/carpisma.json: model → bas [x, y, z, r], hepsi L = |baş − boyun| (eklem arası) biriminde; merkez
# eklem çerçevesinde: s = sol köprücük → sağ köprücük (u'ya dik), u = boyun → baş, f = u × s (Blender sağ el kuralı;
# Unity'de sol el koordinatı yüzünden Vector3.Cross(s, u)). Oranlar ölçekten bağımsız; Unity aynı eklemlerden kurar.
#   python3 -I tool/karakter/carpisma_olc.py   (depo kökünden, bpy 5.x + numpy)
import bpy, json, os
import numpy as np
from mathutils import Vector
R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'BirdieSmash', 'Assets', 'Resources', 'karakter')
out = {}
for f in sorted(os.listdir(R)):
    if not f.endswith('.fbx'): continue
    ad = f[:-4]
    bpy.ops.wm.read_homefile(use_empty=True); bpy.ops.import_scene.fbx(filepath=os.path.join(R, f))
    arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    if not arms: continue
    arm = arms[0]; P = arm.pose.bones; W = arm.matrix_world
    if not all(n in P for n in ('head', 'neck_01', 'clavicle_l', 'clavicle_r')): continue
    govde = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.endswith('__govde')]
    if not govde: continue
    body = max(govde, key=lambda o: len(o.data.vertices)); gn = {g.index: g.name for g in body.vertex_groups}
    hidx = [v.index for v in body.data.vertices if v.groups and gn.get(max((g.weight, g.group) for g in v.groups)[1]) == 'head']
    if len(hidx) < 20: print(ad, 'baş köşesi yok (baş gövdeyle tek parça), atlandı'); continue
    sc = bpy.context.scene; sc.frame_set(1); dg = bpy.context.evaluated_depsgraph_get(); eo = body.evaluated_get(dg); me = eo.to_mesh()
    A = np.array([list(eo.matrix_world @ me.vertices[i].co) for i in hidx]); eo.to_mesh_clear()
    M = np.c_[2 * A, np.ones(len(A))]; x = np.linalg.lstsq(M, (A ** 2).sum(1), rcond=None)[0]; c = Vector(x[:3])
    r = float(np.percentile(np.linalg.norm(A - np.array(c), axis=1), 50))
    J = lambda n: W @ P[n].head
    hd, nk = J('head'), J('neck_01'); u = hd - nk; L = u.length; u.normalize()
    s = J('clavicle_r') - J('clavicle_l'); s = (s - u * s.dot(u)).normalized(); fw = u.cross(s); o = c - hd
    out[ad] = {'bas': [round(o.dot(s) / L, 3), round(o.dot(u) / L, 3), round(o.dot(fw) / L, 3), round(r / L, 3)]}
    print(ad, out[ad])
json.dump(out, open(os.path.join(R, 'carpisma.json'), 'w'), indent=1, sort_keys=True)
