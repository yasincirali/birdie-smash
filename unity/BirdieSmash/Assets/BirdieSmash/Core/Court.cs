// Kort ve top fiziği: HTML sürümünün (assets/game/index.html) birebir C# karşılığı.
// Neden saf C# (UnityEngine yok): kurallar ve fizik Unity'siz derlenip bot maçıyla
// test edilebilsin (tool/unity_test). Koordinatlar oyunla aynı: x kort eni (0..CW),
// y kort boyu (0 = rakibin arka çizgisi, L = benim arka çizgim), z yükseklik.
// Unity görünümü (GameRoot) bunları (x-MID, z, NET-y) olarak dünyaya çevirir.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public static class Court
    {
        // BWF tekler: 13,40 × 6,10 m; yan tekler çizgisi 0,46 m içeride; kısa servis çizgisi
        // fileden 1,98 m; file ortada 1,524 m, direkte 1,55 m.
        public const float L = 13.4f, CW = 6.1f, NET = L / 2f, SL = .46f, SR = CW - .46f, SSL = 1.98f;
        public const float NETH = 1.524f, NETP = 1.55f, MID = CW / 2f;

        public static float NetLimit(float x) { return NETH + (NETP - NETH) * Math.Abs(x - MID) / MID; }
        public static bool OutSingles(float x, float y) { return x < SL || x > SR || y < 0 || y > L; }
    }

    public class Shuttle
    {
        // Tüy top: yerçekimi + hızın karesiyle sürüklenme, son hız ~6,7 m/s. Top hızlı çıkar,
        // çabuk yavaşlar, neredeyse dik düşer. DT 1/240: tahmin ile canlı uçuş aynı adımı kullanır.
        public const float GR = 9.81f, VT = 6.7f, KD = GR / (VT * VT), DT = 1f / 240f;

        public float x, y, z, vx, vy, vz, kd = KD, t;
        public string from, type;
        public bool serve, super, netDone, dead, netHit;
        public float netK;
        public NetPass netPred;   // tahmin edilen file geçişi (canlı uçuş aynı karara varsın)
        public SimResult landPred;

        public Shuttle Clone() { return (Shuttle)MemberwiseClone(); }
        public float Speed { get { return (float)Math.Sqrt(vx * vx + vy * vy + vz * vz); } }

        public static void Step(ref float x, ref float y, ref float z, ref float vx, ref float vy, ref float vz, float kd, float dt)
        {
            float sp = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz);
            vx -= kd * sp * vx * dt; vy -= kd * sp * vy * dt; vz -= (GR + kd * sp * vz) * dt;
            x += vx * dt; y += vy * dt; z += vz * dt;
        }

        public void Step(float dt) { Step(ref x, ref y, ref z, ref vx, ref vy, ref vz, kd, dt); }

        public static SimResult Simulate(float x, float y, float z, float vx, float vy, float vz, bool keep, float kd)
        {
            var r = new SimResult(); if (keep) r.path = new List<PathPt>();
            float t = 0;
            for (int i = 0; i < 4000; i++)
            {
                float py = y; Step(ref x, ref y, ref z, ref vx, ref vy, ref vz, kd, DT); t += DT;
                if (keep && i % 4 == 0) r.path.Add(new PathPt { t = t, x = x, y = y, z = z });
                if (r.net == null && (py > Court.NET) != (y > Court.NET))
                {
                    float lim = Court.NetLimit(x); r.net = new NetPass { z = z, ok = z > lim, t = t };
                    if (!r.net.ok) break;
                }
                if (z <= 0) break;
            }
            r.x = x; r.y = y; r.t = t; return r;
        }

        // Belirli hedefe vuruş: açı sabitken hızı ya da hız sabitken açıyı ikiye bölmeyle bulur.
        public static void Aim(float fx, float fy, float fz, float tx, float ty, bool byAngle, float val, out float ovx, out float ovy, out float ovz)
        {
            float dx = tx - fx, dy = ty - fy, R = (float)Math.Sqrt(dx * dx + dy * dy); if (R < 1e-3f) R = 1e-3f;
            float ux = dx / R, uy = dy / R;
            Func<float, float, float> range = (v, a) =>
            {
                float c = (float)Math.Cos(a);
                var r = Simulate(fx, fy, fz, ux * v * c, uy * v * c, v * (float)Math.Sin(a), false, KD);
                return (float)Math.Sqrt((r.x - fx) * (r.x - fx) + (r.y - fy) * (r.y - fy));
            };
            float lo, hi, vv, aa;
            if (byAngle) { aa = val; lo = 1; hi = 140; for (int k = 0; k < 26; k++) { float m = (lo + hi) / 2; if (range(m, aa) < R) lo = m; else hi = m; } vv = (lo + hi) / 2; }
            else { vv = val; lo = -1.2f; hi = .9f; for (int k = 0; k < 26; k++) { float m = (lo + hi) / 2; if (range(vv, m) < R) lo = m; else hi = m; } aa = (lo + hi) / 2; }
            float cc = (float)Math.Cos(aa);
            ovx = ux * vv * cc; ovy = uy * vv * cc; ovz = vv * (float)Math.Sin(aa);
        }
    }

    public class PathPt { public float t, x, y, z; }
    public class NetPass { public float z, t; public bool ok; }
    public class SimResult { public float x, y, t; public NetPass net; public List<PathPt> path; }
}
