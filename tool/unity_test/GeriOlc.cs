// Geri koşu ölçümü (2026-11-14, yasin: "geri geri koşmak öne gitmekle aynı hızda olmamalı hem bizim userımız hem de rakip
// için; önde kalıp arkaya top atılan pozisyonlara benim user bazen yetişemiyor ama bot her şekilde yetişiyor").
//   mcs -nologo -nowarn:219 -out:geri.exe tool/unity_test/GeriOlc.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs
//   mono geri.exe [maç]          (depo kökünden)
// Ölçer: (1) vuruş anında gövdenin temas noktasına yatay uzaklığı (ben/rakip; > 1,35 m = kol boyunu aşan "ışınlanma"),
// (2) "önde kalıp derine gelen top": vuran vurduğunda karşılayan fileye ≤ 2,6 m, top arka 1,5 m'ye iniyor → karşılayan
// döndürebildi mi (ben/rakip ayrı), (3) gövde hızının yönüne göre en yüksek koşu hızı (öne / yana / geri).
// Bot: Denge/BotMac'teki insan benzeri bot (Kolay: kendiliğinden koşu; Usta: UstaKos).
using System;
using System.Collections.Generic;
using System.Linq;
using BirdieSmash.Core;

static class GeriOlc
{
    static Random rng = new Random(5); static int gecToplam, arkaToplam; static int[] smN = new int[2], smArka = new int[2], smArka6 = new int[2], jumpGeri = new int[2], killN = new int[2], killGeri = new int[2], ovN = new int[2], ovArka = new int[2];
    static void Bot(Match M, double pHit)
    {
        if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .05)
        { double u = rng.NextDouble(); if (u < .45) M.SwipeServe((float)(rng.NextDouble() * 80 - 40), -60, .6f); else if (u < .7) M.SwipeServe((float)(rng.NextDouble() * 40 - 20), -70, 1.6f); else M.SwipeServe((float)(rng.NextDouble() * 160 - 80), -200, 1.8f); }
        Reach R;
        if (M.ReachInfo(out R) && rng.NextDouble() < pHit)
        {
            double k = rng.NextDouble(); float sp = k < .2 ? .5f : k < .65 ? 1.2f : 2.2f;
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = rng.NextDouble() < .08 ? 120 : -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * (M.easy ? 60 : 40)), side = rng.NextDouble() < .5 ? -1 : 1, curved = rng.NextDouble() < .1 });
        }
        if (rng.NextDouble() < .02) M.OnTouchDown();
        if (!M.easy) UstaKos(M);
    }
    static Shuttle kosTop; static float kosGecik;
    static void UstaKos(Match M)
    {
        if (M.s == null || M.s.dead) return;
        if (M.s != kosTop) { kosTop = M.s; kosGecik = (float)(.25 + rng.NextDouble() * .12); if (M.s.from == "me") M.RunTo(Court.MID, Court.NET + 3.3f, .8f); }
        if (M.s.from == "op" && kosGecik > 0 && M.clock - M.opHitAt >= kosGecik && M.icpt != null && M.icpt.who == "me")
        { kosGecik = -1; M.RunTo(M.icpt.x + (float)(rng.NextDouble() - .5) * .6f, M.icpt.y + (float)(rng.NextDouble() - .5) * .6f, 1.2f); }
    }
    static float Pct(List<float> a, double p) { if (a.Count == 0) return 0; var s = a.OrderBy(x => x).ToList(); return s[Math.Min(s.Count - 1, (int)(p * s.Count))]; }

    static void Main(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 20;
        foreach (bool easy in new[] { true, false })
        {
            var uz = new[] { new List<float>(), new List<float>() };   // 0 ben, 1 rakip
            int[] derinN = new int[2], derinAl = new int[2]; int w = 0; gecToplam = 0; arkaToplam = 0;
            var vmax = new float[2, 3];   // [kim, öne/yan/geri]
            for (int m = 0; m < n; m++)
            {
                var M = new Match(100 + m, new MatchOptions { easy = easy, sert = !easy, target = 21, stil = Match.KariyerRakip(easy ? 0 : 1) });
                Shuttle son = null; int derinKim = -1; int guard = 0; float opx = 0, opy = 0;
                while (!M.over && guard++ < 60 * 60 * 40)
                {
                    Bot(M, easy ? .12 : .12); M.Update(1f / 60f);
                    // rakibin anlık hızı (Athlete.mvx/mvy yok sayılırsa konum farkı)
                    float ovx = (M.op.x - opx) * 60, ovy = (M.op.y - opy) * 60; opx = M.op.x; opy = M.op.y;
                    for (int k = 0; k < 2; k++)
                    {
                        var a = k == 0 ? M.me : M.op; float vx = k == 0 ? a.vx : ovx, vy = k == 0 ? a.vy : ovy, v = (float)Math.Sqrt(vx * vx + vy * vy);
                        if (v < .5f || v > 12 || M.s == null || M.s.dead || M.between) continue; float ileri = (k == 0 ? -vy : vy) / v;
                        int b = ileri > .7f ? 0 : ileri < -.7f ? 2 : 1; vmax[k, b] = Math.Max(vmax[k, b], v);
                    }
                    if (M.s != null && M.s != son)
                    {
                        if (son != null && derinKim >= 0 && !M.s.serve && (M.s.from == "me" ? 0 : 1) == derinKim) derinAl[derinKim]++;
                        derinKim = -1; son = M.s;
                        if (!M.s.serve)
                        {
                            int k = M.s.from == "me" ? 0 : 1; var a = k == 0 ? M.me : M.op;
                            uz[k].Add((float)Math.Sqrt((a.x - M.s.hx) * (a.x - M.s.hx) + (a.y - M.s.hy) * (a.y - M.s.hy)));
                            // anatomi: temas gövdenin ne kadar gerisinde (+ = top gövdenin arkasında, arka çizgi tarafında), gövdenin fileye hızı
                            float arka = k == 0 ? M.s.hy - a.y : a.y - M.s.hy, vIleri = k == 0 ? -a.vy : ovy;
                            string v = M.s.vurus ?? M.s.type; bool sm = v == "smash" || v == "jump";
                            if (sm) { smN[k]++; if (arka > .3f) smArka[k]++; if (arka > .6f) smArka6[k]++; if (v == "jump" && vIleri < -2f) jumpGeri[k]++; }
                            if (v == "kill") { killN[k]++; if (vIleri < -1.5f) killGeri[k]++; }
                            if (M.s.hz > 1.95f) { ovN[k]++; if (arka > .6f) ovArka[k]++; }
                        }
                        var lp = M.s.landPred; int r = M.s.from == "me" ? 1 : 0; var rc = r == 0 ? M.me : M.op;
                        if (lp != null && lp.x > Court.SL && lp.x < Court.SR)
                        {
                            bool onde = Math.Abs(rc.y - Court.NET) < 2.6f, derin = r == 0 ? lp.y > Court.L - 1.5f && lp.y < Court.L : lp.y < 1.5f && lp.y > 0;
                            if (onde && derin) { derinKim = r; derinN[r]++; }
                        }
                    }
                    M.events.Clear();
                }
                if (M.sc[0] > M.sc[1]) w++; gecToplam += M.gecKalma; arkaToplam += M.arkaSmac;
            }
 for (int k = 0; k < 2; k++) Console.WriteLine("  anatomi " + (k == 0 ? "ben  " : "rakip") + ": smaç " + smN[k] + ", top gövdenin > 0,3 m gerisinde " + smArka[k] + " (> 0,6 m " + smArka6[k] + "), geri koşarken (> 2 m/s) sıçrayış " + jumpGeri[k] + "; kill " + killN[k] + ", geri giderken " + killGeri[k] + "; baş üstü " + ovN[k] + ", top > 0,6 m geride " + ovArka[k]);
            smN = new int[2]; smArka = new int[2]; smArka6 = new int[2]; jumpGeri = new int[2]; killN = new int[2]; killGeri = new int[2]; ovN = new int[2]; ovArka = new int[2];
            Console.WriteLine((easy ? "Kolay" : "Usta ") + " " + n + " maç, galibiyet %" + 100 * w / n);
            string[] ad = { "ben  ", "rakip" };
            for (int k = 0; k < 2; k++)
                Console.WriteLine("  " + ad[k] + " temas uzaklığı p50 " + Pct(uz[k], .5).ToString("F2") + " p90 " + Pct(uz[k], .9).ToString("F2") + " p99 " + Pct(uz[k], .99).ToString("F2") + " en çok " + (uz[k].Count > 0 ? uz[k].Max() : 0).ToString("F2") + ", > 1,35 m " + uz[k].Count(x => x > 1.35f) + "/" + uz[k].Count
                    + (k == 1 ? " geç kalma " + gecToplam + " arkadan smaç yerine " + arkaToplam : "") + " | önde kalıp derin top " + derinAl[k] + "/" + derinN[k] + " (%" + (derinN[k] > 0 ? 100 * derinAl[k] / derinN[k] : 0) + ") | en yüksek hız öne/yan/geri " + vmax[k, 0].ToString("F1") + "/" + vmax[k, 1].ToString("F1") + "/" + vmax[k, 2].ToString("F1"));
        }
    }
}
