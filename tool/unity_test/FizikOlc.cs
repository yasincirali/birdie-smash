// Top fiziği ölçümü (2026-10-26, "topun fiziği badmintonun doğasına uygun olmalı"): her vuruş türünü oyunun kendi kod
// yoluyla (Match.ShotPlan → Shuttle.Aim → Shuttle.Simulate; rakipte OpHit'teki Plan değerleri; servislerde SERVIS) tipik
// temas noktasından atar ve gerçek badmintonla karşılaştırılacak sayıları basar: çıkış hızı/açısı, tepe, file geçişi
// (bant üstü pay), uçuş süresi, iniş yeri, düşüş açısı, inişteki hız, karşılayanın ana noktasına varış süresi.
//   mcs -out:fizik.exe tool/unity_test/FizikOlc.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono fizik.exe
using System; using System.Collections.Generic; using BirdieSmash.Core;
static class FizikOlc
{
    static void Satir(string ad, float hx, float hy, float hz, float vx, float vy, float vz, float kd)
    {
        var r = Shuttle.Simulate(hx, hy, hz, vx, vy, vz, true, kd);
        float v0 = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz), a0 = (float)(Math.Atan2(vz, Math.Sqrt(vx * vx + vy * vy)) * 180 / Math.PI);
        float apex = hz, tA = 0; foreach (var p in r.path) if (p.z > apex) { apex = p.z; tA = p.t; }
        // düşüş açısı ve hız: son iki örnekten
        float x = hx, y = hy, z = hz, wx = vx, wy = vy, wz = vz, t = 0, t2vt = float.NaN;
        while (z > 0 && t < 8) { Shuttle.Step(ref x, ref y, ref z, ref wx, ref wy, ref wz, kd, Shuttle.DT); t += Shuttle.DT; float sp = (float)Math.Sqrt(wx * wx + wy * wy + wz * wz); if (float.IsNaN(t2vt) && sp < 1.5f * Shuttle.VT) t2vt = t; }
        float vh = (float)Math.Sqrt(wx * wx + wy * wy), ang = (float)(Math.Atan2(-wz, vh) * 180 / Math.PI), vl = (float)Math.Sqrt(vh * vh + wz * wz);
        string net = r.net == null ? "  —   " : ((r.net.z - Court.NetLimit((hx + r.x) / 2)) * 100).ToString("+0;-0").PadLeft(4) + "cm";
        string tNet = r.net == null ? "  —  " : r.net.t.ToString("0.00") + "s";
        // karşılayanın tabanı (fileden 3,3 m) hizasına varış
        float yb = hy > Court.NET ? Court.NET - 3.3f : Court.NET + 3.3f, tb = float.NaN; foreach (var p in r.path) if (float.IsNaN(tb) && (hy > Court.NET ? p.y < yb : p.y > yb)) tb = p.t;
        Console.WriteLine(ad.PadRight(22) + v0.ToString("0.0").PadLeft(6) + a0.ToString("0").PadLeft(5) + "°" + apex.ToString("0.00").PadLeft(6) + "m@" + tA.ToString("0.00") + " " + net + " " + tNet + r.t.ToString("0.00").PadLeft(6) + "s" + Math.Abs(r.y - Court.NET).ToString("0.00").PadLeft(6) + "m" + ang.ToString("0").PadLeft(5) + "°" + vl.ToString("0.0").PadLeft(6) + (float.IsNaN(t2vt) ? "   — " : t2vt.ToString("0.00").PadLeft(6)) + (float.IsNaN(tb) ? "   — " : tb.ToString("0.00").PadLeft(6)));
    }

    // Oyuncu (ben) vuruşu: ShotPlan ile tür/hedef, Aim ile hız. Sapma (g) yok: tasarlanan yörünge ölçülür.
    static void Ben(string ad, float hx, float hy, float hz, float swSpeed, float dy, bool kill = false, bool lunge = false, bool tap = false)
    {
        var M = new Match(1, new MatchOptions { easy = false, target = 21 });
        M.s = new Shuttle { x = hx, y = hy, z = hz, vx = 0, vy = 3, vz = -2, from = "op" }; M.me.x = hx - .4f; M.me.y = hy;   // forehand tarafı
        var P = M.ShotPlan(new SwipeInput { dx = 0, dy = dy, speed = swSpeed, dist = 0, R = 40, tap = tap }, new Reach { kill = kill, lunge = lunge }, true);
        float vx, vy, vz; Shuttle.Aim(P.hx, P.hy, P.hz, P.tx, Court.NET - P.d, P.byAngle, P.val, out vx, out vy, out vz);
        Satir("ben " + ad + "(" + P.type + ")", P.hx, P.hy, P.hz, vx, vy, vz, Shuttle.KD);
    }

    static int Main()
    {
        Console.WriteLine("vuruş                    v0  açı   tepe(zaman)  file payı  t_file  uçuş   iniş-file düşüş v_iniş t(<1,5VT) t(taban)");
        float L = Court.L, N = Court.NET;
        Ben("clear arka", Court.MID, L - 1.0f, 2.55f, 1.2f, -150);
        Ben("clear güçlü", Court.MID, L - 1.0f, 2.55f, 1.6f, -150);
        Ben("smaç", Court.MID, L - 2.5f, 2.6f, 2.2f, -150);
        Ben("smaç arka", Court.MID, L - 1.0f, 2.6f, 2.2f, -150);
        Ben("drop baş üstü", Court.MID, L - 1.5f, 2.5f, .5f, -150);
        Ben("drop orta alçak", Court.MID, N + 3.5f, 1.0f, .5f, -150);
        Ben("file vuruşu (net)", Court.MID, N + 0.6f, 1.2f, .5f, -150);
        Ben("file vuruşu alçak", Court.MID, N + 0.6f, .6f, .5f, -150);
        Ben("lift (hamle)", Court.MID, N + .8f, .45f, 1.2f, -150, false, true);
        Ben("lift (orta)", Court.MID, N + 2.0f, .7f, 1.2f, -150);
        Ben("drive", Court.MID, N + 3.0f, 1.2f, 2.2f, -150);
        Ben("öldürme (kill)", Court.MID, N + .8f, 2.0f, 2.0f, -150, true);
        Ben("blok", Court.MID, N + 3.3f, .6f, .9f, 100);
        // sıçrayarak smaç (enerji dolu): ShotPlan type=jump için energy>=1 gerekir
        { var M = new Match(1, new MatchOptions { easy = false }); M.energy = 1; M.s = new Shuttle { x = Court.MID, y = L - 2.5f, z = 2.8f, from = "op" }; M.me.x = Court.MID - .4f;
          var P = M.ShotPlan(new SwipeInput { dy = -150, speed = 2.2f, R = 40 }, new Reach(), true); float vx, vy, vz; Shuttle.Aim(P.hx, P.hy, P.hz, P.tx, N - P.d, P.byAngle, P.val, out vx, out vy, out vz); Satir("ben sıçr.smaç(" + P.type + ")", P.hx, P.hy, P.hz, vx, vy, vz, Shuttle.KD); }
        // rakip: OpHit → Plan (sapmasız)
        foreach (var c in new[] { new { ad = "smash", o = true, y = 2.2f, z = 2.6f }, new { ad = "clear", o = true, y = 1.2f, z = 2.55f }, new { ad = "drop", o = true, y = 1.5f, z = 2.5f }, new { ad = "clear", o = false, y = N - .8f, z = .5f }, new { ad = "drop", o = false, y = N - 1.0f, z = .7f }, new { ad = "drive", o = false, y = N - 3f, z = 1.2f }, new { ad = "drive", o = false, y = N - 2f, z = .2f } })
        {
            float tx = Court.MID, ty, val; bool byAngle = true;
            switch (c.ad) { case "smash": ty = N + 3.6f + .6f; byAngle = false; val = 52; break; case "drive": ty = N + 4.6f; byAngle = false; val = 22; break; case "drop": ty = N + (c.o ? 1.2f : .9f); val = c.o ? .12f : .75f; break; default: ty = N + L / 2 - .55f; val = c.o ? Match.CLEAR_A : Match.LIFT_A; break; }
            float vx, vy, vz; Shuttle.Aim(Court.MID, c.y, c.z, tx, ty, byAngle, val, out vx, out vy, out vz);
            Satir("rakip " + c.ad + (c.o ? " üst" : " alt") + " z" + c.z.ToString("0.0"), Court.MID, c.y, c.z, vx, vy, vz, Shuttle.KD);
        }
        // servisler (SERVIS tablosu, sunucu kısa servis çizgisinin 1,4 m gerisinde, temas .45 m önde)
        foreach (var kv in Match.SERVIS)
        {
            var S = kv.Value; float hy = N + Court.SSL + 1.4f - .45f, d = kv.Key == "kisa" ? Court.SSL + .45f : kv.Key == "flick" ? 6.0f : 6.2f;
            float vx, vy, vz; Shuttle.Aim(Court.MID + .8f, hy, S.z, Court.MID - .8f, N - d, true, S.aci, out vx, out vy, out vz);
            Satir("servis " + kv.Key + " z" + S.z.ToString("0.00"), Court.MID + .8f, hy, S.z, vx, vy, vz, Shuttle.KD);
        }
        // son hız kontrolü: dik bırakılan top
        { float x = 0, y = 0, z = 20, vx = 0, vy = 0, vz = 0; for (int i = 0; i < 240 * 5; i++) Shuttle.Step(ref x, ref y, ref z, ref vx, ref vy, ref vz, Shuttle.KD, Shuttle.DT); Console.WriteLine("serbest düşüş 5 s sonra hız " + (-vz).ToString("0.00") + " m/s (VT " + Shuttle.VT + ")"); }
        return 0;
    }
}
