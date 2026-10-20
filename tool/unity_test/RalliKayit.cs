// Önizleme videosu için: Unity demosunun çekirdeğiyle (Core) bir maç oynatır, en iyi ralliyi
// 30 kare/sn kaydeder (oyuncu konumu, klip+faz, top, file dalgası, yazılar) → JSON.
// tool/unity_test/onizleme.py bu JSON'u aynı FBX karakterle Blender'da render eder.
//   mcs -out:/tmp/r.exe tool/unity_test/RalliKayit.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono /tmp/r.exe out.json
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BirdieSmash.Core;

static class RalliKayit
{
    static string F(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
    static string Ath(Athlete a, bool isMe, float clock)
    {
        string c; float ph; Match.PickClip(a, isMe, clock, out c, out ph);
        return "{\"x\":" + F(a.x) + ",\"y\":" + F(a.y) + ",\"j\":" + F(a.jump) + ",\"dv\":" + F(a.dive) + ",\"vx\":" + F(a.vx) + ",\"c\":\"" + c + "\",\"p\":" + F(ph) + "}";
    }
    static int Main(string[] args)
    {
        string outp = args.Length > 0 ? args[0] : "ralli.json";
        string best = null; int bestHits = 0; float bestDur = 0; var rng = new Random(11);
        for (int seed = 1; seed < 40 && bestHits < 12; seed++)
        {
            var M = new Match(seed, true, 21); var frames = new List<string>(); int hits = 0; float tail = -1; bool smashEnd = false; var texts = new List<string>();
            const float dt = 1f / 30f; int guard = 0;
            while (!M.over && guard++ < 30 * 600)
            {
                if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && rng.NextDouble() < .06) M.SwipeServe((float)(rng.NextDouble() * 120 - 60), -200, rng.NextDouble() < .5 ? .6f : 1.4f);
                Reach R;
                if (M.ReachInfo(out R) && rng.NextDouble() < .3)
                {
                    double k = rng.NextDouble(); float sp = R.over ? (k < .45 ? 2.3f : k < .8 ? 1.25f : .6f) : (k < .3 ? 2.0f : k < .75 ? 1.2f : .6f);
                    M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 220 - 110), dy = -(float)(120 + rng.NextDouble() * 150), speed = sp, R = 52, dist = (float)(rng.NextDouble() * 30), side = 1 });
                }
                M.Update(dt);
                foreach (var e in M.events)
                {
                    if (e.kind == Ev.Serve) { frames.Clear(); hits = 0; tail = -1; smashEnd = false; texts.Clear(); }
                    if (e.kind == Ev.Hit) hits++;
                    if (e.kind == Ev.Point) { tail = 1.3f; smashEnd = e.who == "me" && e.type == "smash"; }
                    if (e.text != null) texts.Add("{\"f\":" + frames.Count + ",\"t\":\"" + e.text.Replace("\"", "'") + "\",\"w\":\"" + e.who + "\"}");
                    if (e.kind == Ev.Hit) texts.Add("{\"f\":" + frames.Count + ",\"hit\":\"" + e.type + "\",\"w\":\"" + e.who + "\",\"q\":\"" + e.quality + "\"}");
                }
                M.events.Clear();
                var sb = new StringBuilder();
                sb.Append("{\"me\":").Append(Ath(M.me, true, M.clock)).Append(",\"op\":").Append(Ath(M.op, false, M.clock));
                var s = M.s;
                if (s != null) sb.Append(",\"s\":{\"x\":" + F(s.x) + ",\"y\":" + F(s.y) + ",\"z\":" + F(s.z) + ",\"vx\":" + F(s.vx) + ",\"vy\":" + F(s.vy) + ",\"vz\":" + F(s.vz) + ",\"sm\":" + (s.type == "smash" ? 1 : 0) + "}");
                sb.Append(",\"net\":{\"a\":[");
                for (int i = 0; i < NetWave.N; i++) { if (i > 0) sb.Append(','); sb.Append(F(M.net.a[i])); }
                sb.Append("],\"z\":" + F(M.net.z) + ",\"tape\":" + F(M.net.tape) + ",\"glow\":" + F(M.net.glow) + ",\"gx\":" + F(M.net.gx) + ",\"gz\":" + F(M.net.gz) + ",\"from\":\"" + (s != null ? s.from : "") + "\"}");
                sb.Append(",\"sc\":[" + M.sc[0] + "," + M.sc[1] + "]}");
                frames.Add(sb.ToString());
                if (tail >= 0) { tail -= dt; if (tail < 0)
                    {
                        float dur = frames.Count / 30f; int score = hits + (smashEnd ? 3 : 0);
                        if (hits >= 6 && dur < 20 && score > bestHits) { bestHits = score; bestDur = dur; best = "{\"fps\":30,\"frames\":[" + string.Join(",", frames.ToArray()) + "],\"texts\":[" + string.Join(",", texts.ToArray()) + "]}"; Console.WriteLine("aday seed " + seed + " vuruş " + hits + (smashEnd ? " smaçla bitti" : "") + " " + dur.ToString("0.0") + " s"); }
                        tail = -1;
                    } }
            }
        }
        if (best == null) { Console.WriteLine("ralli bulunamadı"); return 1; }
        File.WriteAllText(outp, best); Console.WriteLine("yazıldı " + outp + " " + bestDur.ToString("0.0") + " s"); return 0;
    }
}
