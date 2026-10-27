// File teması ölçümü + parite (2026-10-26, yasin "file kritik, bant daha sert, file oynak"): aynı durumları Match.Update ile
// yürütür; tool/unity_test/file_temas_parite.js aynı durumları index.html'in kendi kodundan yürütür, iki çıktı (son sütun hariç) aynı olmalı.
// 2026-10-27 (ekran/file): HTML donuk, Unity file dinamiği ayrıldı (NetWave başlığı, parabol sarkma). Sonuç sütunları (cord, alt,
// hit, tIn) hâlâ aynı olmalı; sonY, maxKarsi, maxA artık HTML'le aynı değil. agIci görünen ağ yüzeyine göre (NetWave.Gorunen).
//   mcs -out:ft.exe tool/unity_test/FileTemasOlc.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono ft.exe
using System; using BirdieSmash.Core;
static class FileTemasOlc {
  static readonly float[][] C = {
    // ad: x, z_net(hedef geçiş yüksekliği), hız (yatay, fileye doğru), vz0
    new float[]{3.05f, 1.00f, 50, 0},   // ağ ortası smaç
    new float[]{3.05f, 1.30f,  6, 0},   // ağ üstü yavaş drop
    new float[]{2.00f, 1.20f, 20, 0},   // ağ orta hız
    new float[]{3.05f, 1.47f, 20, 0},   // bant (lim-5 cm)
    new float[]{3.05f, 1.51f, 30, 0},   // kordon (lim-1,4 cm)
    new float[]{3.05f, 1.51f,  4, 0},   // bant üstü yavaş (kordon değil)
    new float[]{3.05f, 0.60f, 25, 0},   // ağ altı
  };
  static int Main(string[] a) {
    bool iz = a.Length > 0;
    for (int ci = 0; ci < C.Length; ci++) { var c = C[ci];
      var M = new Match(1, new MatchOptions { easy = false, target = 21 });
      float y0 = Court.NET + .3f, vy = -c[2];
      var s = new Shuttle { x = c[0], y = y0, z = c[1], vx = 0, vy = vy, vz = c[3], from = "me", type = "drop", kd = Shuttle.KD };
      // file önünde 0,3 m'den düz atış; netPred yok: geçiş kararı canlı yükseklikten (z <= lim)
      s.landPred = Shuttle.Simulate(s.x, s.y, s.z, s.vx, s.vy, s.vz, true, s.kd); s.netPred = null;
      M.s = s; float t = 0, maxUzak = 0, maxA = 0, tAg = 0; bool karsi = false; float minZ = 9, maxIc = -1;
      for (int f = 0; f < 600 && !s.dead; f++) { M.Update(1 / 60f); t += 1 / 60f;
        float uz = -(s.y - Court.NET); if (uz > maxUzak) maxUzak = uz; if (s.agda) tAg = t;
        if (s.netHit && !s.agda && !s.altindan && s.z > Court.NetLimit(s.x) - Court.AG_DERIN) { float ic = (M.net.Gorunen(s.x, s.z) - (s.y - Court.NET)) * s.agYan; if (ic > maxIc) maxIc = ic; }
        foreach (var v in M.net.a) maxA = Math.Max(maxA, Math.Abs(v));
        if (iz && ci == 0) Console.WriteLine("  " + t.ToString("0.000") + " y" + (s.y - Court.NET).ToString("+0.000;-0.000") + " z" + s.z.ToString("0.000") + (s.agda ? " AG" : ""));
      }
      Console.WriteLine(ci + " sonY " + (s.y - Court.NET).ToString("+0.000;-0.000") + " x " + s.x.ToString("0.000") + " tIn " + t.ToString("0.000") + " maxKarsi " + maxUzak.ToString("0.000") + " maxA " + maxA.ToString("0.000") + " agSon " + tAg.ToString("0.000") + " cord " + s.netCord + " alt " + s.altindan + " hit " + s.netHit + " vz " + s.inVz.ToString("0.00") + " agIci " + maxIc.ToString("0.000"));
    }
    return 0; }
}
