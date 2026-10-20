// Unity'siz doğrulama: Core'u mono ile derleyip bot maçları koşar.
//   mcs -out:/tmp/botmac.exe tool/unity_test/BotMac.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono /tmp/botmac.exe
// Kontroller: maçlar biter, skor kurala uyar, ralli olur, file ve backhand klipleri görülür, NaN yok.
using System;
using System.Collections.Generic;
using BirdieSmash.Core;

static class BotMac
{
    static int Main()
    {
        int fails = 0; var clipSeen = new Dictionary<string, int>(); int nets = 0, rallies = 0, maxRally = 0, points = 0, meWins = 0, swipes = 0, hits = 0;
        var rng = new Random(7);
        for (int m = 0; m < 12; m++)
        {
            var M = new Match(100 + m, m % 3 != 2, 11);
            float t = 0; int guard = 0;
            while (!M.over && guard++ < 60 * 60 * 25)
            {
                const float dt = 1f / 60f;
                // bot: servis sırası bende ise kaydır; pencere açıksa bazen mükemmel, bazen kenardan, bazen ıska
                if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && rng.NextDouble() < .05)
                    M.SwipeServe((float)(rng.NextDouble() * 160 - 80), -200, (float)(rng.NextDouble() * 2));
                Reach R;
                if (M.ReachInfo(out R) && rng.NextDouble() < .12)
                {
                    swipes++;
                    var sw = new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = -(float)(80 + rng.NextDouble() * 220), speed = (float)(rng.NextDouble() * 2.6), R = 40, dist = (float)(rng.NextDouble() * 60), side = rng.NextDouble() < .5 ? -1 : 1 };
                    if (M.SwipeShot(sw)) hits++;
                }
                if (rng.NextDouble() < .02) M.OnTouchDown();
                M.Update(dt); t += dt;
                foreach (var a in new[] { M.me, M.op })
                {
                    string c; float ph; Match.PickClip(a, a == M.me, M.clock, out c, out ph);
                    int n; clipSeen.TryGetValue(c, out n); clipSeen[c] = n + 1;
                    if (float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(ph)) { Console.WriteLine("NaN oyuncu"); fails++; goto next; }
                }
                if (M.s != null && (float.IsNaN(M.s.x) || float.IsNaN(M.s.z))) { Console.WriteLine("NaN top"); fails++; goto next; }
                foreach (var e in M.events)
                {
                    if (e.kind == Ev.Net) nets++;
                    if (e.kind == Ev.Point) { points++; rallies++; maxRally = Math.Max(maxRally, (int)e.k); }
                }
                M.events.Clear();
            }
            next:
            if (!M.over) { Console.WriteLine("maç bitmedi " + m + " skor " + M.sc[0] + "-" + M.sc[1]); fails++; continue; }
            int a0 = M.sc[0], b0 = M.sc[1];
            bool ok = (Math.Max(a0, b0) >= 11 && Math.Abs(a0 - b0) >= 2) || Math.Max(a0, b0) == 15;
            if (!ok) { Console.WriteLine("skor kurala aykırı " + a0 + "-" + b0); fails++; }
            if (a0 > b0) meWins++;
            Console.WriteLine("maç " + m + (M.easy ? " kolay " : " usta  ") + a0 + "-" + b0 + "  rakip " + M.stil.ad + "  " + t.ToString("0") + " s");
        }
        Console.WriteLine("sayı " + points + ", en uzun ralli " + maxRally + ", file " + nets + ", vuruş " + hits + "/" + swipes + ", kazanılan maç " + meWins + "/12");
        var keys = new List<string>(clipSeen.Keys); keys.Sort();
        Console.WriteLine("klipler: " + string.Join(", ", keys.ConvertAll(k => k + "=" + clipSeen[k]).ToArray()));
        foreach (var need in new[] { "idle", "serve", "split", "smash", "clear", "low", "bh_over", "bh_low", "chasse_r", "chasse_l", "fwd" })
            if (!clipSeen.ContainsKey(need)) { Console.WriteLine("hiç görülmeyen klip: " + need); fails++; }
        if (maxRally < 4) { Console.WriteLine("ralli çok kısa"); fails++; }
        if (nets == 0) { Console.WriteLine("file hiç çalışmadı"); fails++; }
        Console.WriteLine(fails == 0 ? "TAMAM" : "HATA " + fails);
        return fails == 0 ? 0 : 1;
    }
}
