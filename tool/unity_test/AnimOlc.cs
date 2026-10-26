// Animasyon ölçümü (2026-10-26 gece incelemesi, build 40): bot maçlarında her karede iki oyuncunun gösterilen klibi
// (Match.PickClip, PlayerView ile aynı karar) ve PlayerView'daki alt beden katmanının ağırlığı (aynı kural, aynı
// yumuşatma) taklit edilir. Ölçülenler:
//  - ayak kayması: gövdenin GERÇEK zamanda (ekranda) kat ettiği hız ile bacakları süren pozun ima ettiği hız farkı.
//    Ayak işi klibi fazı mesafeyle ilerlettiği için ima ettiği hız = min(sp/boy, tavan)·boy; diğer klipler (boşta,
//    hazırlık, vuruş, split'in yerdeki kısmı) yerinde durur, ima ettiği hız 0. Alt katman ağırlığıyla karıştırılır.
//    > 0,3 m/s kayma "görünür" sayılır.
//  - vuruş senkronu: temas olayında vuranın fazı = CLIP_HIT mi; temastan önceki kareyle temas karesi arası kaç poz
//    karesi atlandı (ileri salınım tek karede mi geçti).
//  - klip titremesi: 0,15 sn içinde geri dönen klip değişimi; ışınlanma: servis dizilişi dışında kare başına > 12 m/s.
//   mcs -nologo -nowarn:219 -out:animolc.exe tool/unity_test/AnimOlc.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs
//   mono animolc.exe [maç sayısı] [hz]
using System;
using System.Collections.Generic;
using System.Linq;
using BirdieSmash.Core;

static class AnimOlc
{
    static readonly Random rng = new Random(7);
    static void Bot(Match M)
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
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = rng.NextDouble() < .08 ? 120 : -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * 60), side = rng.NextDouble() < .5 ? -1 : 1 });
        }
    }

    static readonly HashSet<string> AYAK = new HashSet<string> { "chasse_r", "chasse_l", "fwd", "back_r", "back_l", "run", "kros_r", "kros_l" };
    // Ayak işi fazının ilerlediği hız (Match.AyakIsi). Eski Core'la (vAn yok) karşılaştırmak için -DESKI: yumuşatılmış hız.
#if ESKI
    static float Hiz(Athlete a) { return (float)Math.Sqrt(a.svx * a.svx + a.svy * a.svy); }
#else
    static float Hiz(Athlete a) { return a.vAn; }
#endif

    class Iz { public float jSon, jGor; public string prepSon; public int prepDegis; public string cur, prev, altKlip; public float altW; public int lastFrame = -1; public float px = float.NaN, py; public float changeT = -9; public string beforeChange; }
    class Sayac
    {
        public int kare, kayan; public double kaymaTop; public Dictionary<string, int> kayanKlip = new Dictionary<string, int>(), klipKare = new Dictionary<string, int>();
        public Dictionary<string, int> neden = new Dictionary<string, int>(); public Dictionary<string, int> titremeCift = new Dictionary<string, int>(); public Dictionary<string, int> uyumsuzCift = new Dictionary<string, int>(); public int dikeyPop; public int prepDegisTop, prepTemas, prepUyumsuz; public Dictionary<string, int> prepCift = new Dictionary<string, int>(); public int temas, temasFazYanlis, salinimAtla, titreme, isinlanma, pencereKare, pencereKayan;
    }

    static int Main(string[] args)
    {
        int nMac = args.Length > 0 ? int.Parse(args[0]) : 12; float hz = args.Length > 1 ? float.Parse(args[1]) : 60; float dt = 1f / hz;
        var S = new Dictionary<string, Sayac> { { "me", new Sayac() }, { "op", new Sayac() } };
        for (int m = 0; m < nMac; m++)
        {
            var M = new Match(300 + m, new MatchOptions { easy = m % 3 != 2, target = 11 });
            var iz = new Dictionary<string, Iz> { { "me", new Iz() }, { "op", new Iz() } };
            int guard = 0; float t = 0;
            while (!M.over && guard++ < 60 * 60 * 30 * (int)(hz / 60))
            {
                Bot(M); bool wasBetween = M.between || M.s == null;
                var faz0 = new Dictionary<string, float>();
                foreach (var w in new[] { "me", "op" }) { var a = w == "me" ? M.me : M.op; string c0; float p0; M.PickClip(a, w == "me", out c0, out p0); faz0[w] = p0; iz[w].prev = c0; }
                bool pencere = M.HitWindow();
                M.Update(dt); t += dt;
                foreach (var w in new[] { "me", "op" })
                {
                    var a = w == "me" ? M.me : M.op; var z = iz[w]; var sc = S[w]; bool isMe = w == "me";
                    string c; float ph; M.PickClip(a, isMe, out c, out ph);
                    // PlayerView alt katmanı (aynı kural: Match.AltHedef, Match.ALT_HIZ üstel yumuşatma)
                    if (a.yuru && a.ayakKlip != null) z.altKlip = a.ayakKlip;
                    float hedef = z.altKlip != null ? Match.AltHedef(a, c) : 0;
                    z.altW += (hedef - z.altW) * (1 - (float)Math.Exp(-dt * Match.ALT_HIZ)); if (z.altW < .01f && hedef == 0) z.altW = 0;
                    // gerçek zamanlı gövde hızı
                    if (!float.IsNaN(z.px))
                    {
                        float v = (float)Math.Sqrt((a.x - z.px) * (a.x - z.px) + (a.y - z.py) * (a.y - z.py)) / dt;
                        bool dizilis = wasBetween && v > 12;
                        if (v > 12 && !dizilis) sc.isinlanma++;
                        if (!dizilis && !(M.between || M.s == null))
                        {
                            float boy; if (a.ayakKlip == null || !Match.ADIM_BOY.TryGetValue(a.ayakKlip, out boy)) boy = 1.5f;
                            float ima = a.yuru && a.ayakKlip != null ? Math.Min(Hiz(a) / boy, Match.AYAK_TUR_MAX) * boy : 0;
                            float bacakAyak = AYAK.Contains(c) ? 1 : z.altW;   // bacakları ayak işinden süren pay
                            if (c == "split" && ph < Match.SPLIT_HAVA || Match.JumpH(a) > .05f) bacakAyak = -1;   // havada (split hop'u, sıçrama): kayma sayılmaz
                            float kay = bacakAyak < 0 ? 0 : bacakAyak * Math.Abs(v - ima) + (1 - bacakAyak) * v;
                            sc.kare++; sc.kaymaTop += kay; Inc(sc.klipKare, c);
                            if (pencere) sc.pencereKare++;
                            if (kay > .3f) { sc.kayan++; Inc(sc.kayanKlip, c); if (pencere) sc.pencereKayan++;
                                if (!AYAK.Contains(c)) Inc(sc.neden, !a.yuru ? "yürümüyor v" + (v < 1 ? "<1" : v < 2 ? "<2" : ">2") : hedef == 0 ? "kural kapalı (" + (a.sw > 0 ? "takip" : "hazırlık") + ")" : z.altW < .9f ? "ağırlık yükseliyor" : "?"); }
                        }
                    }
                    z.px = a.x; z.py = a.y;
                    { float j = Match.JumpH(a, c == "split") * .5f; z.jGor = j > z.jGor ? Math.Min(j, z.jGor + Match.GORSEL_YUKSELIS * dt) : j; j = z.jGor;   // PlayerView: görünen gövde yüksekliği
                      if (Math.Abs(j - z.jSon) > .12f) sc.dikeyPop++; z.jSon = j; }
                    // hazırlık pozu değişimi: aynı yaklaşımda (temas öncesi) hazırlık klibi kaç kez değişti
                    bool prepDal = a.sw <= 0 && a.svAnim == null && a.prepA > .08f && a.prepClip != null && Match.CLIP_HIT.ContainsKey(c);
                    if (prepDal) { if (z.prepSon != null && c != z.prepSon) { z.prepDegis++; Inc(sc.prepCift, z.prepSon + "→" + c); } z.prepSon = c; }
                    else if (a.sw <= 0 && a.prepA <= .08f) { z.prepSon = null; z.prepDegis = 0; }
                    if (c != z.cur)
                    {
                        if (z.cur != null && c == z.beforeChange && t - z.changeT < .15f) { sc.titreme++; Inc(sc.titremeCift, z.beforeChange + "↔" + z.cur); }
                        z.beforeChange = z.cur; z.changeT = t; z.cur = c;
                    }
                }
                foreach (var e in M.events)
                {
                    if (e.kind != Ev.Hit || e.serve) continue;
                    var a = e.who == "me" ? M.me : M.op; string c; float ph; M.PickClip(a, e.who == "me", out c, out ph); var sc = S[e.who];
                    float hit; if (!Match.CLIP_HIT.TryGetValue(c, out hit)) continue; sc.temas++;
                    { var z2 = iz[e.who]; sc.prepDegisTop += z2.prepDegis; if (z2.prepSon != null) { sc.prepTemas++; if (z2.prepSon != c && !(Match.HAMLE.ContainsKey(z2.prepSon) && Match.HAMLE[z2.prepSon] == c) && !(Match.HAMLE.ContainsKey(c) && Match.HAMLE[c] == z2.prepSon)) { sc.prepUyumsuz++; Inc(sc.uyumsuzCift, z2.prepSon + "⇒" + c); } } z2.prepDegis = 0; z2.prepSon = null; }
                    int n; Match.CLIP_N.TryGetValue(c, out n); if (n < 2) n = 8;
                    if (Math.Abs(ph - hit) * n > 1.01f) sc.temasFazYanlis++;
                    // temastan önceki kare aynı klipteyse kaç poz atlandı
                    if (iz[e.who].prev == c || Match.HAMLE.ContainsKey(iz[e.who].prev ?? "")) { int atla = (int)Math.Floor(hit * n) - (int)Math.Floor(faz0[e.who] * n); if (atla > 2) sc.salinimAtla++; }
                }
                M.events.Clear();
            }
        }
        foreach (var w in new[] { "me", "op" })
        {
            var sc = S[w];
            Console.WriteLine(w + ": ralli karesi " + sc.kare + ", görünür kayma (>0,3 m/s) %" + (100.0 * sc.kayan / Math.Max(1, sc.kare)).ToString("0.0") + ", ortalama " + (sc.kaymaTop / Math.Max(1, sc.kare)).ToString("0.000") + " m/s"
                + ", pencerede %" + (100.0 * sc.pencereKayan / Math.Max(1, sc.pencereKare)).ToString("0.0") + " (" + sc.pencereKare + " kare)");
            Console.WriteLine("   kayan kare klibe göre: " + string.Join(", ", sc.kayanKlip.OrderByDescending(k => k.Value).Select(k => k.Key + "=" + k.Value + "/" + sc.klipKare[k.Key])));
            Console.WriteLine("   ayak işi dışı kaymanın nedeni: " + string.Join(", ", sc.neden.OrderByDescending(k => k.Value).Select(k => k.Key + "=" + k.Value)));
            Console.WriteLine("   titreme çiftleri: " + string.Join(", ", sc.titremeCift.OrderByDescending(k => k.Value).Take(8).Select(k => k.Key + "=" + k.Value)));
            Console.WriteLine("   hazırlık klibi değişimi: temas başına " + (sc.prepDegisTop / (double)Math.Max(1, sc.temas)).ToString("0.00") + ", hazırlık ≠ temas klibi " + sc.prepUyumsuz + "/" + sc.prepTemas + "; en sık: " + string.Join(", ", sc.prepCift.OrderByDescending(k => k.Value).Take(6).Select(k => k.Key + "=" + k.Value)));
            Console.WriteLine("   hazırlık ⇒ temas uyumsuz: " + string.Join(", ", sc.uyumsuzCift.OrderByDescending(k => k.Value).Take(10).Select(k => k.Key + "=" + k.Value)));
            Console.WriteLine("   dikey sıçrama (kare başına > 12 cm): " + sc.dikeyPop);
            Console.WriteLine("   temas " + sc.temas + ", temas fazı yanlış " + sc.temasFazYanlis + ", ileri salınımda >2 poz atlayan " + sc.salinimAtla + ", klip titremesi " + sc.titreme + ", ışınlanma " + sc.isinlanma);
        }
        return 0;
    }
    static void Inc(Dictionary<string, int> d, string k) { int n; d.TryGetValue(k, out n); d[k] = n + 1; }
    static float InvLerp(float a, float b, float v) { return Math.Max(0, Math.Min(1, (v - a) / (b - a))); }
    static float SmoothStep(float x) { return x * x * (3 - 2 * x); }
}
