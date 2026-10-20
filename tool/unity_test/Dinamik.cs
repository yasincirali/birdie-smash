// Top dinamiği ölçümü: bot maçlarında her vuruşun türü, vuranın fileye uzaklığı, temas yüksekliği,
// çıkış hızı, tepe noktası, gerçek uçuş süresi (yavaşlatma hariç, Simulate ile) ve iniş yeri.
//   mcs -out:dinamik.exe tool/unity_test/Dinamik.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono dinamik.exe
using System; using System.Collections.Generic; using System.Linq; using BirdieSmash.Core;
static class Dinamik
{
    class Row { public string who, type; public float dNet, z, v0, apex, t, landNet, side; }
    static int Main()
    {
        var rows = new List<Row>(); var rng = new Random(3); var texts = new Dictionary<string, int>(); float total = 0; int pts = 0;
        for (int m = 0; m < 10; m++)
        {
            var M = new Match(500 + m, true, 11); int guard = 0;
            while (!M.over && guard++ < 60 * 60 * 25)
            {
                const float dt = 1f / 60f;
                if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && rng.NextDouble() < .05) M.SwipeServe((float)(rng.NextDouble() * 120 - 60), -200, (float)(.6 + rng.NextDouble()));
                Reach R;
                if (M.ReachInfo(out R) && M.HitWindow() && rng.NextDouble() < .25)
                {   // insan benzeri: çoğu orta hızda yukarı, bazen sert, bazen yavaş; yön rastgele
                    double u = rng.NextDouble(); float sp = u < .2 ? .5f : u < .7 ? 1.2f : 2.2f;
                    M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 160 - 80), dy = rng.NextDouble() < .1 ? 120 : -(float)(120 + rng.NextDouble() * 160), speed = sp, R = 40, dist = (float)(rng.NextDouble() * 50), side = 1 });
                }
                M.Update(dt); total += dt;
                foreach (var e in M.events)
                {
                    if (e.kind == Ev.Text) { int n; texts.TryGetValue(e.text ?? "?", out n); texts[e.text ?? "?"] = n + 1; }
                    if (e.kind == Ev.Point) pts++;
                    if (e.kind == Ev.Hit && M.s != null)
                    {
                        var s = M.s; var r = Shuttle.Simulate(s.x, s.y, s.z, s.vx, s.vy, s.vz, true, s.kd);
                        float apex = s.z; foreach (var p in r.path) apex = Math.Max(apex, p.z);
                        rows.Add(new Row { who = e.who, type = e.type, dNet = Math.Abs(e.y - Court.NET), z = e.z, v0 = s.Speed, apex = apex, t = r.t, landNet = Math.Abs(r.y - Court.NET) });
                    }
                }
                M.events.Clear();
            }
        }
        Console.WriteLine("toplam " + rows.Count + " vuruş, " + pts + " sayı, " + (total / 60).ToString("0.0") + " dk oyun");
        Console.WriteLine("kim   tür     adet  fileye  temas   v0     tepe   uçuş   iniş(fileye)");
        foreach (var g in rows.GroupBy(r => r.who + " " + r.type).OrderBy(g => g.Key))
        {
            Func<Func<Row, float>, string> md = f => { var a = g.Select(f).OrderBy(x => x).ToList(); return a[a.Count / 2].ToString("0.0").PadLeft(5); };
            Console.WriteLine(g.Key.PadRight(13) + g.Count().ToString().PadLeft(5) + "  " + md(r => r.dNet) + "m " + md(r => r.z) + "m " + md(r => r.v0) + " " + md(r => r.apex) + "m " + md(r => r.t) + "s " + md(r => r.landNet) + "m");
        }
        Console.WriteLine("yazılar (dakikada " + (texts.Values.Sum() / (total / 60)).ToString("0.0") + "):");
        foreach (var kv in texts.OrderByDescending(k => k.Value).Take(14)) Console.WriteLine("  " + kv.Value.ToString().PadLeft(5) + "  " + kv.Key);
        return 0;
    }
}
