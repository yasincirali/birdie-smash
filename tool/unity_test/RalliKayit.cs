// Önizleme videosu için: Unity demosunun çekirdeğiyle (Core) maçlar oynatır, en iyi ralliyi (sayıdan sonraki 0,7 sn ve
// yeni servis dizilişiyle birlikte) 30 kare/sn kaydeder: oyuncu konumu, klip+faz (Match.PickClip, Unity ile aynı karar),
// sıçrama (Match.JumpH), gövde eğimi girdileri, nefes, top, file dalgası, hakem durumu, yazılar → JSON.
// tool/unity_test/onizleme.py bu JSON'u aynı FBX karakterle Blender'da render eder.
//   mcs -out:/tmp/r.exe tool/unity_test/RalliKayit.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono /tmp/r.exe out.json [saha]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BirdieSmash.Core;

static class RalliKayit
{
    static string F(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "'"); }
    static string Ath(Match M, Athlete a, bool isMe)
    {
        string c; float ph; M.PickClip(a, isMe, out c, out ph);
        float lean = Math.Max(-.1f, Math.Min(.1f, a.svx * .02f)), dive = (a.yat ? a.dive : 0) * (a.x < Court.MID ? -1 : 1) * .26f;
        return "{\"x\":" + F(a.x) + ",\"y\":" + F(a.y) + ",\"j\":" + F(Match.JumpH(a)) + ",\"tilt\":" + F(lean + dive) + ",\"n\":" + F(a.nefes) + ",\"c\":\"" + c + "\",\"p\":" + F(ph) + "}";
    }
    static int Main(string[] args)
    {
        string outp = args.Length > 0 ? args[0] : "ralli.json", saha = args.Length > 1 ? args[1] : "turnuva";
        string best = null; int bestScore = 0; float bestDur = 0; var rng = new Random(11);
        for (int seed = 1; seed < 60; seed++)
        {
            var st = Match.STIL[Match.STIL_SIRA[seed % 3]];
            var M = new Match(seed, new MatchOptions { easy = true, target = 21, stil = st, saha = saha, bestRally = 99 });
            var frames = new List<string>(); int hits = 0; float tail = -1; bool smashEnd = false, jump = false; var texts = new List<string>(); var serves = new HashSet<string>();
            const float dt = 1f / 30f; int guard = 0; bool rec = false;
            while (!M.over && guard++ < 30 * 900)
            {
                if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .06)
                { double u = rng.NextDouble(); if (u < .5) M.SwipeServe((float)(rng.NextDouble() * 60 - 30), -60, .6f); else if (u < .75) M.SwipeServe(0, -70, 1.6f); else M.SwipeServe((float)(rng.NextDouble() * 120 - 60), -200, 1.8f); }
                Reach R;
                if (M.ReachInfo(out R) && rng.NextDouble() < .3)
                {
                    double k = rng.NextDouble(); float sp = R.over ? (k < .5 ? 2.3f : k < .8 ? 1.25f : .6f) : (k < .3 ? 2.0f : k < .75 ? 1.2f : .6f);
                    M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 220 - 110), dy = -(float)(120 + rng.NextDouble() * 150), speed = sp, R = 52, dist = (float)(rng.NextDouble() * 25), side = 1 });
                }
                M.Update(dt);
                foreach (var e in M.events)
                {
                    if (e.kind == Ev.Serve)
                    {
                        if (tail >= 0) { tail = 0; }   // yeni servis dizilişi kayda girdi: ralli tamam (aşağıda değerlendirilir)
                        else { frames.Clear(); hits = 0; smashEnd = false; jump = false; texts.Clear(); serves.Clear(); rec = true; }
                    }
                    if (e.kind == Ev.Hit) { hits++; if (e.big) jump = true; if (M.s != null && M.s.serve) serves.Add(e.who + ":" + e.type); }
                    if (e.kind == Ev.Point) { tail = 1.6f; smashEnd = e.who == "me" && e.type == "smash"; }
                    if (e.text != null) texts.Add("{\"f\":" + frames.Count + ",\"t\":\"" + Esc(e.text) + "\",\"w\":\"" + e.who + "\",\"o\":" + e.onem + ",\"k\":\"" + e.kind + "\"}");
                    if (e.kind == Ev.Hit) texts.Add("{\"f\":" + frames.Count + ",\"hit\":\"" + e.type + "\",\"w\":\"" + e.who + "\",\"q\":\"" + e.quality + "\",\"big\":" + (e.big ? 1 : 0) + "}");
                }
                M.events.Clear();
                if (!rec) continue;
                var sb = new StringBuilder();
                sb.Append("{\"me\":").Append(Ath(M, M.me, true)).Append(",\"op\":").Append(Ath(M, M.op, false));
                var s = M.s;
                if (s != null) sb.Append(",\"s\":{\"x\":" + F(s.x) + ",\"y\":" + F(s.y) + ",\"z\":" + F(s.z) + ",\"vx\":" + F(s.vx) + ",\"vy\":" + F(s.vy) + ",\"vz\":" + F(s.vz) + ",\"sm\":" + (s.type == "smash" ? 1 : 0) + "}");
                sb.Append(",\"net\":{\"a\":[");
                for (int i = 0; i < NetWave.N; i++) { if (i > 0) sb.Append(','); sb.Append(F(M.net.a[i])); }
                sb.Append("],\"z\":" + F(M.net.z) + ",\"tape\":" + F(M.net.tape) + ",\"glow\":" + F(M.net.glow) + ",\"gx\":" + F(M.net.gx) + ",\"gz\":" + F(M.net.gz) + ",\"from\":\"" + (s != null ? s.from : "") + "\"}");
                float he = M.hakemT > 0 ? (M.hakemT0 - M.hakemT) * 1.4f / M.hakemT0 : -1;
                sb.Append(",\"hk\":{\"e\":" + F(he) + ",\"so\":" + (M.hakemSO ? 1 : 0) + ",\"yon\":\"" + M.hakemYon + "\",\"sv\":\"" + M.server + "\",\"sj\":\"" + M.sjYon + "\",\"sjT\":" + F(M.sjT) + "}");
                sb.Append(",\"sc\":[" + M.sc[0] + "," + M.sc[1] + "],\"hw\":" + (M.HitWindow() ? 1 : 0) + "}");
                frames.Add(sb.ToString());
                if (tail >= 0) { tail -= dt; if (tail < 0) tail = 0; }
                if (tail == 0)
                {
                    float dur = frames.Count / 30f; int score = hits + (smashEnd ? 3 : 0) + (jump ? 4 : 0) + serves.Count * 2;
                    if (hits >= 6 && dur < 14 && score > bestScore)
                    {
                        bestScore = score; bestDur = dur;
                        best = "{\"fps\":30,\"saha\":\"" + saha + "\",\"rakip\":\"" + Esc(M.RakipAd(true)) + "\",\"renk\":\"" + Match.OpRenk(M.stil, "#4fb8ff") + "\",\"frames\":[" + string.Join(",", frames.ToArray()) + "],\"texts\":[" + string.Join(",", texts.ToArray()) + "]}";
                        Console.WriteLine("aday seed " + seed + " " + M.RakipAd(false) + " vuruş " + hits + (smashEnd ? " smaçla bitti" : "") + (jump ? " sıçrayarak smaç" : "") + " servis " + string.Join("/", serves) + " " + dur.ToString("0.0") + " s");
                    }
                    tail = -1; frames.Clear(); hits = 0; smashEnd = false; jump = false; texts.Clear(); serves.Clear();
                }
            }
        }
        if (best == null) { Console.WriteLine("ralli bulunamadı"); return 1; }
        File.WriteAllText(outp, best); Console.WriteLine("yazıldı " + outp + " " + bestDur.ToString("0.0") + " s"); return 0;
    }
}
