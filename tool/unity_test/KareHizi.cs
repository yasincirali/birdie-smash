// 60/120 Hz eşdeğerlik ölçümü (2026-10-26, yasin Ar-Ge önerisi "ProMotion'da maç 120 Hz": "bot maçında 60/120 sonuç farkı
// olmamalı"). Aynı tohumla aynı maçlar iki kez oynanır: (A) 60 Hz, (B) 120 Hz benzetim. Bot kararı ikisinde de 60 Hz'de
// verilir (insan girdisi kare hızına bağlı değil), yalnız Match.Update adımı değişir. Galibiyet, ortalama sayı ve ralli
// uzunluğu karşılaştırılır.
//   mcs -nologo -nowarn:219 -out:karehizi.exe tool/unity_test/KareHizi.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs
//   mono karehizi.exe [maç]
using System;
using BirdieSmash.Core;

static class KareHizi
{
    static void Bot(Match M, Random rng)
    {
        if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .05)
        {
            double u = rng.NextDouble();
            if (u < .45) M.SwipeServe((float)(rng.NextDouble() * 80 - 40), -60, .6f);
            else if (u < .7) M.SwipeServe((float)(rng.NextDouble() * 40 - 20), -70, 1.6f);
            else M.SwipeServe((float)(rng.NextDouble() * 160 - 80), -200, 1.8f);
        }
        Reach R;
        if (M.ReachInfo(out R) && rng.NextDouble() < .12)
        {
            double k = rng.NextDouble(); float sp = k < .2 ? .5f : k < .65 ? 1.2f : 2.2f;
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * 60), side = rng.NextDouble() < .5 ? -1 : 1 });
        }
    }

    static void Oyna(int n, int alt, out double gal, out double sayi, out double ralli)
    {
        int w = 0; double s = 0, r = 0; int rn = 0;
        for (int m = 0; m < n; m++)
        {
            var rng = new Random(700 + m); var M = new Match(300 + m, new MatchOptions { easy = true, target = 5 }); int g = 0, sonR = 0;
            float dt = 1f / (60 * alt);
            while (!M.over && g++ < 60 * 60 * 30)
            {
                Bot(M, rng);
                for (int i = 0; i < alt; i++) M.Update(dt);
                if (M.rally == 0 && sonR > 0) { r += sonR; rn++; }
                sonR = M.rally;
            }
            if (M.sc[0] > M.sc[1]) w++; s += M.sc[0] + M.sc[1];
        }
        gal = 100.0 * w / n; sayi = s / n; ralli = rn > 0 ? r / rn : 0;
    }

    static int Main(string[] a)
    {
        int n = a.Length > 0 ? int.Parse(a[0]) : 200; double g1, s1, r1, g2, s2, r2;
        Oyna(n, 1, out g1, out s1, out r1); Oyna(n, 2, out g2, out s2, out r2);
        Console.WriteLine("60 Hz : galibiyet %{0:F1}  maç başı sayı {1:F2}  ralli {2:F2}", g1, s1, r1);
        Console.WriteLine("120 Hz: galibiyet %{0:F1}  maç başı sayı {1:F2}  ralli {2:F2}", g2, s2, r2);
        return 0;
    }
}
