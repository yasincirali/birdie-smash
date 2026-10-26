// File dokusu topun hızına göre tepki verir (yasin 2026-10-19: "süper gerçekçi olmasına gerek
// yok ama düşünüldüğü belli olmalı"). Her sütun 1B sönümlü dalga: smaç fileyi derin esnetir,
// dalga direğe ~0,35 s'de varır; drop yalnız hafif kıpırdatır. HTML sürümüyle aynı sabitler.
using System;

namespace BirdieSmash.Core
{
    public class NetWave
    {
        public const int N = 33;
        public readonly float[] a = new float[N], v = new float[N];
        // Bant ayrı bir yay (2026-10-26, yasin "file kısmı ve bant kısmı farklı tepki vermeli"): üstte gergin kordonlu 75 mm
        // beyaz bant (BWF Law 1.5) ağ gibi esnemez; çarpılan yerde küçük, sert ve hızlı titrer (ω≈17 rad/s, ~0,4 sn'de
        // söner), kordon boyunca çabuk yayılır. Ağ ise derin ve yavaş esner (aşağıdaki a/v). Eski tek sayı `tape` bandın
        // ortalama sapması olarak kalır (RalliKayit, eski görünüm).
        public readonly float[] ta = new float[N], tv = new float[N];
        public float z = Court.NETH * .5f, tape, tapeV, glow, gx = Court.MID, gz = Court.NETH * .5f;
        public bool active;

        public void Impulse(float x, float zz, float sp, bool tapeOnly)
        {
            int i = (int)Math.Round(x / Court.CW * (N - 1)); gx = x; gz = zz;
            if (tapeOnly)
            {
                tapeV += Math.Min(.9f, sp * .03f); float ku = Math.Min(1f, sp / 30f);
                for (int d = -2; d <= 2; d++) { int j = i + d; if (j <= 0 || j >= N - 1) continue; tv[j] += (.6f + 2.4f * ku) * (float)Math.Exp(-d * d / .8f); }
                glow = Math.Max(glow, .5f * ku); gz = Court.NETH; active = true; return;
            }
            float k = Math.Min(1f, sp / 55f);   // smaç ~50 m/s → tam, drop ~6 m/s → %10
            z = Math.Max(.15f, Math.Min(Court.NETH - .1f, zz)); glow = Math.Max(glow, k);
            for (int d = -2; d <= 2; d++) { int j = i + d; if (j <= 0 || j >= N - 1) continue; v[j] += (.3f + 2.6f * k) * (float)Math.Exp(-d * d / 1.6f); }
            tapeV += .12f + .5f * k;
            for (int d = -3; d <= 3; d++) { int j = i + d; if (j <= 0 || j >= N - 1) continue; tv[j] += (.08f + .35f * k) * (float)Math.Exp(-d * d / 3f); }   // ağ bandı aşağı çeker: küçük, geniş
            active = true;
        }

        public void Step(float dt)
        {
            const float c2 = 120, damp = 2.4f, k = 28; bool act = false;
            for (int i = 1; i < N - 1; i++) { float lap = a[i - 1] - 2 * a[i] + a[i + 1]; v[i] += (c2 * lap - k * a[i] - damp * v[i]) * dt; }
            for (int i = 1; i < N - 1; i++) { a[i] += v[i] * dt; if (Math.Abs(a[i]) > 1e-4f || Math.Abs(v[i]) > 1e-3f) act = true; }
            tapeV += (-60 * tape - 7 * tapeV) * dt; tape += tapeV * dt; glow = Math.Max(0, glow - dt * 1.6f);
            const float tc2 = 900, tk = 300, tdamp = 11;   // gergin kordon: dalga hızlı yayılır, sert ve çabuk söner
            for (int i = 1; i < N - 1; i++) { float lap = ta[i - 1] - 2 * ta[i] + ta[i + 1]; tv[i] += (tc2 * lap - tk * ta[i] - tdamp * tv[i]) * dt; }
            for (int i = 1; i < N - 1; i++) { ta[i] += tv[i] * dt; if (Math.Abs(ta[i]) > 1e-4f || Math.Abs(tv[i]) > 1e-3f) act = true; }
            active = act || Math.Abs(tape) > 1e-4f;
        }

        public void Advance(float dt) { if (!active && glow <= 0) return; float r = dt; while (r > 0) { float h = Math.Min(1f / 240f, r); Step(h); r -= h; } }

        // Bandın x'teki sapması (m): direklerde sıfır.
        public float TapeAt(float x)
        {
            float f = x / Court.CW * (N - 1); int i = (int)Math.Floor(f); float t = f - i;
            return ta[Math.Max(0, Math.Min(N - 1, i))] * (1 - t) + ta[Math.Max(0, Math.Min(N - 1, i + 1))] * t;
        }

        // x,z → fileye dik sapma (m). Bantta sıfıra iner, alt kenar serbest.
        public float At(float x, float zz)
        {
            float f = x / Court.CW * (N - 1); int i = (int)Math.Floor(f); float t = f - i;
            float a0 = a[Math.Max(0, Math.Min(N - 1, i))], a1 = a[Math.Max(0, Math.Min(N - 1, i + 1))];
            float ax = a0 * (1 - t) + a1 * t; const float w = .42f;
            float prof = (float)(Math.Exp(-((zz - z) * (zz - z)) / (2 * w * w)) * (1 - Math.Pow(Math.Max(0, zz) / Court.NETH, 6)));
            return ax * prof;
        }
    }
}
