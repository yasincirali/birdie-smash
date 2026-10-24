// Unity'siz doğrulama: Core'u mono ile derleyip bot maçları koşar.
//   mcs -out:/tmp/botmac.exe tool/unity_test/BotMac.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono /tmp/botmac.exe
// Kontroller (2026-10-24, HTML fb1b245 kuralları): maçlar biter, skor kurala uyar (5/11/21; 7/15/30 tavan), üç servis
// türü de atılır, iki oyuncunun nefesi düşer ve dolar, sayı arası 0,7 sn (rekorda 1,3), hazırlıkta temas karesi temastan
// önce gösterilmez, sıçrayarak smaç temasta tepede, hamlede gövde yatmaz, ilk açılış antrenmanı 3 adım + 5 sayılık maç,
// kariyer düğme etiketi, NaN yok.
using System;
using System.Collections.Generic;
using System.Linq;
using BirdieSmash.Core;

static class BotMac
{
    static int fails = 0;
    static void Fail(string m) { Console.WriteLine("HATA: " + m); fails++; }
    static readonly Random rng = new Random(7);

    // insan benzeri bot: servis türünü rastgele seçer, pencere açıkken çoğu orta, bazen sert/yavaş kaydırır
    static void Bot(Match M, float dt, double pHit = .12)
    {
        if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .05)
        {
            double u = rng.NextDouble();
            if (u < .45) M.SwipeServe((float)(rng.NextDouble() * 80 - 40), -60, .6f);          // yavaş = kısa
            else if (u < .7) M.SwipeServe((float)(rng.NextDouble() * 40 - 20), -70, 1.6f);     // kısa ve sert = flick
            else M.SwipeServe((float)(rng.NextDouble() * 160 - 80), -200, 1.8f);               // uzun ve hızlı = yüksek
        }
        Reach R;
        if (M.ReachInfo(out R) && rng.NextDouble() < pHit)
        {
            double k = rng.NextDouble(); float sp = k < .2 ? .5f : k < .65 ? 1.2f : 2.2f;
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = rng.NextDouble() < .08 ? 120 : -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * 60), side = rng.NextDouble() < .5 ? -1 : 1, curved = rng.NextDouble() < .1 });
        }
        if (rng.NextDouble() < .02) M.OnTouchDown();
    }

    static int Main()
    {
        var clipSeen = new Dictionary<string, int>(); var serveSeen = new Dictionary<string, int>();
        int nets = 0, cords = 0, points = 0, meWins = 0, maxRally = 0, hits = 0, prepEarly = 0, prepFrames = 0, jumpN = 0, opJumpN = 0, tiltBad = 0;
        float minMe = 1, minOp = 1, jumpMin = 9, opJumpMin = 9; var gaps = new List<float>(); var gapsRekor = new List<float>();
        var styles = new Dictionary<string, int>(); int tired = 0;
        int[] targets = { 5, 11, 21 };
        for (int m = 0; m < 18; m++)
        {
            int target = targets[m % 3]; Style st = m % 4 == 3 ? Match.KariyerRakip(m % 6) : null;
            var M = new Match(100 + m, new MatchOptions { easy = m % 5 != 4, target = target, stil = st, bestRally = 6 });
            styles[M.stil.isim] = styles.ContainsKey(M.stil.isim) ? styles[M.stil.isim] + 1 : 1;
            float t = 0, pointAt = -1; bool lastRekor = false; int guard = 0; const float dt = 1f / 60f;
            while (!M.over && guard++ < 60 * 60 * 40)
            {
                Bot(M, dt);
                var sBefore = M.s; bool svBefore = M.me.svAnim != null || M.op.svAnim != null;
                M.Update(dt); t += dt;
                foreach (var a in new[] { M.me, M.op })
                {
                    string c; float ph; M.PickClip(a, a == M.me, out c, out ph);
                    int n; clipSeen.TryGetValue(c, out n); clipSeen[c] = n + 1;
                    if (float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(ph)) { Fail("NaN oyuncu"); goto next; }
                    // hazırlık: temas karesi ancak temasta (sw>0 dalı); hazırlık dalında faz temas kesrinin altında kalmalı
                    float hit; if (a.sw <= 0 && a.svAnim == null && a.prepA > .08f && a.prepClip == c && Match.CLIP_HIT.TryGetValue(c, out hit)) { prepFrames++; int nn; if (Match.CLIP_N.TryGetValue(c, out nn) && ph * nn >= Math.Floor(hit * nn)) prepEarly++; }
                    if (a.dive > 0 && a.yat && a == M.me) tiltBad++;   // hamlede (uzanma) gövde dik: yat yalnız rakibin kıl payı kurtarışında
                }
                minMe = Math.Min(minMe, M.me.nefes); minOp = Math.Min(minOp, M.op.nefes);
                if (M.s != null && (float.IsNaN(M.s.x) || float.IsNaN(M.s.z))) { Fail("NaN top"); goto next; }
                foreach (var e in M.events)
                {
                    if (e.kind == Ev.Net) nets++;
                    if (e.kind == Ev.Cord) cords++;
                    if (e.kind == Ev.Text && e.text == "Rakip yoruldu!") tired++;
                    if (e.kind == Ev.Hit)
                    {
                        hits++;
                        if (M.s != null && M.s.serve) { string k = e.type == "drop" ? "kisa" : (M.s.vz > 0 && Math.Atan2(M.s.vz, Math.Sqrt(M.s.vx * M.s.vx + M.s.vy * M.s.vy)) < .7 ? "flick" : "yuksek"); serveSeen[k] = serveSeen.ContainsKey(k) ? serveSeen[k] + 1 : 1; }
                        if (e.who == "me" && e.big) { jumpN++; jumpMin = Math.Min(jumpMin, Match.JumpH(M.me)); }
                        if (e.who == "op" && M.op.clip == "jump") { opJumpN++; opJumpMin = Math.Min(opJumpMin, Match.JumpH(M.op)); }
                    }
                    if (e.kind == Ev.Point) { points++; maxRally = Math.Max(maxRally, (int)e.k); pointAt = t; lastRekor = !M.kisaBekle; }
                    if (e.kind == Ev.Serve && pointAt >= 0) { (lastRekor ? gapsRekor : gaps).Add(t - pointAt); pointAt = -1; }
                }
                M.events.Clear();
            }
        next:
            if (!M.over) { Fail("maç bitmedi " + m + " skor " + M.sc[0] + "-" + M.sc[1]); continue; }
            int a0 = M.sc[0], b0 = M.sc[1], cap = target == 21 ? 30 : target == 11 ? 15 : 7;
            bool ok = (Math.Max(a0, b0) >= target && Math.Abs(a0 - b0) >= 2) || Math.Max(a0, b0) == cap;
            if (!ok) Fail("skor kurala aykırı " + a0 + "-" + b0 + " hedef " + target);
            if (a0 > b0) meWins++;
            Console.WriteLine("maç " + m.ToString().PadLeft(2) + (M.easy ? " kolay " : " usta  ") + target.ToString().PadLeft(2) + " sayı " + a0 + "-" + b0 + "  rakip " + M.RakipAd(true) + (M.stil.kariyer >= 0 ? " · " + Match.KARIYER[M.stil.kariyer].yer : "") + "  " + t.ToString("0") + " s, en uzun ralli " + M.maxRally);
        }
        Console.WriteLine("sayı " + points + ", en uzun ralli " + maxRally + ", file " + nets + ", bant yuvarlanması " + cords + ", vuruş " + hits + ", kazanılan maç " + meWins + "/18");
        Console.WriteLine("servis türleri: " + string.Join(", ", serveSeen.Select(k => k.Key + "=" + k.Value)));
        Console.WriteLine("rakipler: " + string.Join(", ", styles.Select(k => k.Key + "=" + k.Value)));
        Console.WriteLine("nefes en düşük: ben " + minMe.ToString("0.00") + ", rakip " + minOp.ToString("0.00") + ", 'Rakip yoruldu!' " + tired + " kez");
        if (gaps.Count > 0) Console.WriteLine("sayı → servis dizilişi: medyan " + gaps.OrderBy(x => x).ElementAt(gaps.Count / 2).ToString("0.00") + " s (" + gaps.Count + " sayı)" + (gapsRekor.Count > 0 ? ", rekor/maç sonu " + gapsRekor.OrderBy(x => x).ElementAt(gapsRekor.Count / 2).ToString("0.00") + " s" : ""));
        Console.WriteLine("hazırlık karesi: " + prepFrames + " kare, temastan önce temas karesi " + prepEarly);
        Console.WriteLine("sıçrayarak smaç: ben " + jumpN + " (temasta yükseklik en az " + (jumpN > 0 ? jumpMin.ToString("0.00") : "-") + "), rakip " + opJumpN + " (" + (opJumpN > 0 ? opJumpMin.ToString("0.00") : "-") + ")");
        var keys = clipSeen.Keys.ToList(); keys.Sort();
        Console.WriteLine("klipler: " + string.Join(", ", keys.Select(k => k + "=" + clipSeen[k])));
        foreach (var need in new[] { "idle", "serve_bh", "serve_flick", "split", "smash", "clear", "low", "drive", "bh_over", "bh_low", "chasse_r", "chasse_l", "fwd", "jump" })
            if (!clipSeen.ContainsKey(need)) Fail("hiç görülmeyen klip: " + need);
        foreach (var need in new[] { "kisa", "flick", "yuksek" }) if (!serveSeen.ContainsKey(need)) Fail("servis türü yok: " + need);
        if (maxRally < 4) Fail("ralli çok kısa");
        if (nets == 0) Fail("file hiç çalışmadı");
        if (prepEarly > 0) Fail("hazırlıkta temas karesi temastan önce göründü: " + prepEarly);
        if (jumpN > 0 && jumpMin < .95f) Fail("sıçrayarak smaç temasta tepede değil: " + jumpMin);
        if (opJumpN > 0 && opJumpMin < .95f) Fail("rakip sıçrayarak smaç temasta tepede değil: " + opJumpMin);
        if (tiltBad > 0) Fail("hamlede gövde yattı");
        if (minMe > .9f || minOp > .95f) Fail("nefes düşmüyor");
        if (gaps.Count == 0 || Math.Abs(gaps.OrderBy(x => x).ElementAt(gaps.Count / 2) - .7f) > .05f) Fail("sayı arası 0,7 sn değil");

        Antrenman(); Kariyer();
        Console.WriteLine(fails == 0 ? "TAMAM" : "HATA " + fails);
        return fails == 0 ? 0 : 1;
    }

    // İlk açılış: 3 adım (kısa servis, clear, smaç), sonra 5 sayılık Kolay ilk maç.
    static void Antrenman()
    {
        var M = new Match(9, new MatchOptions { easy = false, ogr = true, ogrKisa = true });
        if (M.ogr.liste.Count != 3 || !M.easy) Fail("ilk antrenman 3 adım ve Kolay olmalı");
        var adlar = M.ogr.liste.Select(a => a.bas).ToArray(); int guard = 0, durN = 0; const float dt = 1f / 60f;
        while (!M.ogr.macaGec && guard++ < 60 * 400)
        {
            var a = M.OgrA();
            if (a != null && a.servis != null && M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null) M.SwipeServe(0, -60, .6f);
            if (a != null && M.ogr.dur)
            {   // oyun durdu: istenen hareket (clear = orta hız yukarı, smaç = hızlı)
                durN++; float sp = a.tip[0] == "clear" ? 1.2f : 2.3f;
                M.SwipeShot(new SwipeInput { dx = 0, dy = -200, speed = sp, R = 40, dist = 5, side = 1 });
            }
            M.Update(dt); M.events.Clear();
        }
        if (!M.ogr.macaGec) { Fail("antrenman bitmedi, adım " + M.ogr.i); return; }
        var I = new Match(10, new MatchOptions { easy = true, ilk = true, target = 21 });
        if (I.target != 5) Fail("ilk maç 5 sayı değil");
        Console.WriteLine("antrenman: " + string.Join(" → ", adlar) + ", oyun " + durN / 1 + " karede durdu, " + (guard / 60f).ToString("0") + " s; ilk maç " + I.target + " sayı " + (I.easy ? "Kolay" : "Usta"));
    }

    // Kariyer: kazanınca "Sıradaki: {yer}", kaybedince "Tekrar dene", serbest maçta "Bir maç daha".
    static void Kariyer()
    {
        string a = Career.AgainLabel(true, true, 1), b = Career.AgainLabel(true, false, 1), c = Career.AgainLabel(false, true, 0), d = Career.AgainLabel(true, true, 6);
        Console.WriteLine("düğmeler: '" + a + "' / '" + b + "' / '" + c + "' / şampiyon '" + d + "'");
        if (a != "Sıradaki: Okul turnuvası" || b != "Tekrar dene" || c != "Bir maç daha" || d != "Bir maç daha") Fail("kariyer düğme etiketi");
        int k = 0; string note; Career.Apply(ref k, 0, true, out note); if (k != 1 || !note.Contains("Okul turnuvası")) Fail("kariyer basamağı açılmadı: " + note);
        Career.Apply(ref k, 1, false, out note); if (k != 1 || !note.Contains("bekliyor")) Fail("kayıpta basamak notu: " + note);
    }
}
