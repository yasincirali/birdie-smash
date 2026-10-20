// Maç: kurallar, rakip yapay zekâsı, vuruşlar, servis, sayı. HTML sürümünün (index.html:
// newMatch … land) C# karşılığı; ekipman/görev/nefes katmanları demo için dışarıda bırakıldı
// (GS çarpanları 1). Görünüm katmanı yalnız Events kuyruğunu okur ve Athlete alanlarından
// poz seçer; burada UnityEngine yok ki bot maçıyla Unity'siz test edilebilsin.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class Athlete
    {
        public float x, y, vx, vy, px = float.NaN, py;
        public float tx = float.NaN, ty;
        public float sw, dive, jump, stun, prepA, splitT, runPh, manualT;
        public string clip = "idle", prepClip, shot = "low", hand = "fore", prepShot, prepHand;
        public bool HasTarget { get { return !float.IsNaN(tx); } }
    }

    public class Intercept
    {
        public float t, x, y, z, side = float.NaN;
        public string who, opType;
        public bool overhead, kill, lunge, leave;
    }

    public struct Reach { public bool lunge, over, kill; public float d; }

    // Kaydırma: piksel değerleri 640 px yükseklikli referans ekrana ölçeklenmiş (HTML ile aynı eşikler).
    public struct SwipeInput { public float dx, dy, speed, dist, R, side; public bool tap; }

    public enum Ev { Hit, Net, Tape, Floor, Point, Text, Split, MatchEnd, Serve }

    public class GameEvent
    {
        public Ev kind; public string who, type, quality, text; public float x, y, z, k; public bool big;
    }

    public class Style
    {
        public string ad, ipucu; public string[] ustK, altK; public float[] ustP, altP; public float hiz, hata, ev;
    }

    public class ServeBox { public string who; public float x0, x1, y0, y1; }

    public class Match
    {
        public readonly Athlete me = new Athlete(), op = new Athlete();
        public readonly int[] sc = new int[2];
        public readonly NetWave net = new NetWave();
        public readonly List<GameEvent> events = new List<GameEvent>();
        public Shuttle s; public Intercept icpt; public ServeBox serveBox; public Style stil;
        public string server = "me", serveMsg = "";
        public int rally, maxRally, pstreak, target = 21;
        public float energy, clock, opHitAt = -9, wait, betweenT, freeze, slow = 1, slowT;
        public bool easy = true, between, split, splitPending, over, matchDone, hint;
        readonly Random rng;

        public static readonly Dictionary<string, float> CLIP_HIT = new Dictionary<string, float> {
            {"smash",.56f},{"clear",.58f},{"drive",.55f},{"kill",.55f},{"low",.55f},{"lunge",.6f},{"serve",.65f},{"block",.6f},
            {"bh_over",.56f},{"bh_drive",.55f},{"bh_kill",.55f},{"bh_low",.55f},{"bh_lunge",.6f},{"jump",.52f}};
        // Raket HEP sağ elde (yasin 2026-10-19): backhand aynalama değil, kendi klibi.
        public static readonly Dictionary<string, string> BH = new Dictionary<string, string> {
            {"smash","bh_over"},{"clear","bh_over"},{"drive","bh_drive"},{"kill","bh_kill"},{"low","bh_low"},{"lunge","bh_lunge"},{"block","bh_drive"},{"jump","bh_over"}};

        public static readonly Style[] STILS = {
            new Style{ad="Smaşçı",ipucu="Her baş üstü topu smaçlar. Derin clear atıp blokla al",ustK=new[]{"smash","clear","drop"},ustP=new[]{.62f,.23f,.15f},altK=new[]{"clear","drop","drive"},altP=new[]{.45f,.2f,.35f},hiz=1,hata=1.2f,ev=3.2f},
            new Style{ad="Duvar",ipucu="Her şeyi geri çevirir, az hata yapar. Drop ile öne çek",ustK=new[]{"smash","clear","drop"},ustP=new[]{.15f,.6f,.25f},altK=new[]{"clear","drop","drive"},altP=new[]{.65f,.25f,.1f},hiz=1.08f,hata=.75f,ev=3.6f},
            new Style{ad="File oyuncusu",ipucu="Fileye yakın durur, drop sever. Arkasına clear at",ustK=new[]{"smash","clear","drop"},ustP=new[]{.3f,.2f,.5f},altK=new[]{"clear","drop","drive"},altP=new[]{.25f,.6f,.15f},hiz=1,hata=1,ev=2.2f}};

        public Match(int seed, bool easyMode, int targetPoints)
        {
            rng = new Random(seed); easy = easyMode; target = targetPoints;
            stil = STILS[rng.Next(STILS.Length)];
            me.x = Court.MID; me.y = Court.L - 2.2f; op.x = Court.MID; op.y = 2.2f;
            SetupServe();
            serveMsg = "Rakip: " + stil.ad + ". " + stil.ipucu;
        }

        float R01() { return (float)rng.NextDouble(); }
        static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
        static float Hyp(float a, float b) { return (float)Math.Sqrt(a * a + b * b); }
        public static string ClipHand(string c, string hand) { string b; return hand == "back" && BH.TryGetValue(c, out b) ? b : c; }
        void Emit(Ev k, string who = null, string type = null, float x = 0, float y = 0, float z = 0, float kk = 0, string text = null, string q = null, bool big = false)
        { events.Add(new GameEvent { kind = k, who = who, type = type, x = x, y = y, z = z, k = kk, text = text, quality = q, big = big }); }

        // ---- servis yeri: sunucunun skoru çiftse sağ, tekse sol kare ----
        string ServeSide(string who) { return sc[who == "me" ? 0 : 1] % 2 == 0 ? "R" : "L"; }
        static float[] BoxX(string who, string side)
        {
            float[] right = who == "me" ? new[] { Court.MID, Court.SR } : new[] { Court.SL, Court.MID };
            float[] left = who == "me" ? new[] { Court.SL, Court.MID } : new[] { Court.MID, Court.SR };
            return side == "R" ? right : left;
        }
        void SetupServe()
        {
            s = null; me.vx = me.vy = 0; me.tx = float.NaN; me.stun = 0; rally = 0; between = false; icpt = null; serveMsg = "";
            string sv = server, side = ServeSide(sv), rv = sv == "me" ? "op" : "me";
            float[] sb = BoxX(sv, side), rb = BoxX(rv, side);
            if (sv == "me") { me.x = (sb[0] + sb[1]) / 2; me.y = Court.NET + Court.SSL + 1.4f; op.x = (rb[0] + rb[1]) / 2; op.y = Court.NET - Court.SSL - 1.6f; }
            else { op.x = (sb[0] + sb[1]) / 2; op.y = Court.NET - Court.SSL - 1.4f; me.x = (rb[0] + rb[1]) / 2; me.y = Court.NET + Court.SSL + 1.6f; }
            serveBox = new ServeBox { who = rv, x0 = rb[0], x1 = rb[1], y0 = rv == "me" ? Court.NET + Court.SSL : 0, y1 = rv == "me" ? Court.L : Court.NET - Court.SSL };
            wait = sv == "op" ? 1.0f : 0; hint = sv == "me";
            Emit(Ev.Serve, sv);
        }

        bool InServeBox(float x, float y) { var b = serveBox; return x >= b.x0 && x <= b.x1 && y >= b.y0 && y <= b.y1; }
        public bool IsOut(float x, float y, bool serve) { return serve ? !InServeBox(x, y) : Court.OutSingles(x, y); }

        int Behind() { return Math.Max(0, sc[1] - sc[0]); }
        public float MeSpd() { return 5.2f * (split ? 1.3f : 1); }
        public float OpSpd() { return stil.hiz * (easy ? 3.2f : 3.6f) + Math.Min(1.1f, (sc[0] + sc[1]) * .04f) - Math.Min(.8f, Behind() * .12f); }

        static string HandOf(Athlete o, string who, float sx, float sz) { float side = (sx - o.x) * (who == "me" ? 1 : -1); return side < -(sz > 1.95f ? .6f : .25f) ? "back" : "fore"; }
        static string ShotClass(string type, float z, float y)
        {
            if (type == "smash" || z > 1.95f) return "over"; if (type == "drive") return "drive";
            if (Math.Abs(y - Court.NET) < 2.3f && z > Court.NETH) return "kill"; return "low";
        }
        // Pozisyon: forehand tarafı önde; forehand noktasına yetişemiyorsa backhand noktası.
        float HitSide(Athlete o, string who, Intercept I)
        {
            if (!float.IsNaN(I.side)) return I.side;
            float m = who == "me" ? 1 : -1, fh = (I.kill || I.lunge ? .3f : .45f) * m, bh = -.3f * m;
            float spd = who == "me" ? 5.2f : OpSpd(), tt = Math.Max(0, I.t - s.t) * .9f;
            float dF = Hyp(I.x - fh - o.x, I.y - o.y), dB = Hyp(I.x - bh - o.x, I.y - o.y);
            I.side = (dF > spd * tt && dB < dF - .3f) ? bh : fh; return I.side;
        }

        void StartFlight(string from, float vx, float vy, float vz, float hx, float hy, float hz, bool isServe, string type, bool isSuper, string q)
        {
            s = new Shuttle { x = hx, y = hy, z = hz, vx = vx, vy = vy, vz = vz, from = from, serve = isServe, type = type, super = isSuper };
            rally++; maxRally = Math.Max(maxRally, rally);
            if (from == "op") { opHitAt = clock; split = false; if (splitPending) { splitPending = false; DoSplit(); } } else { split = false; splitPending = false; }
            var h = from == "me" ? me : op;
            h.sw = 1; h.shot = isServe ? "low" : (h.prepShot == "lunge" ? "lunge" : ShotClass(type, hz, hy)); h.hand = isServe ? "fore" : HandOf(h, from, hx, hz);
            string c = isServe ? "serve" : h.shot == "lunge" ? "lunge" : h.shot == "over" ? (type == "jump" || isSuper ? "jump" : type == "smash" ? "smash" : "clear") : h.shot == "drive" ? "drive" : h.shot == "kill" ? "kill" : type == "block" ? "block" : "low";
            h.clip = ClipHand(c, h.hand);
            var oth = from == "me" ? op : me; if (oth.sw <= 0) oth.splitT = .3f;   // rakip vururken split step
            if (type == "smash") freeze = Math.Max(freeze, isSuper ? .16f : .08f);
            Emit(Ev.Hit, from, type, hx, hy, hz, rally, null, q ?? "good", isSuper);
            PlanIntercept();
        }

        // Karşılayan için yolu önceden hesapla: baş üstünde alabiliyorsa yüksekte, değilse yere düşmeden en alçak noktada.
        void PlanIntercept()
        {
            var r = Shuttle.Simulate(s.x, s.y, s.z, s.vx, s.vy, s.vz, true, s.kd);
            string recv = s.from == "me" ? "op" : "me"; var P0 = recv == "me" ? me : op;
            float t0 = s.t; foreach (var p in r.path) p.t += t0; r.t += t0;
            s.landPred = r; if (r.net != null) s.netPred = r.net;
            float react = recv == "me" ? 0 : .18f, spd = recv == "me" ? MeSpd() : OpSpd();
            Func<PathPt, float, float> need = (p, reach) => Math.Max(0, Hyp(p.x - P0.x, p.y - P0.y) - reach) / spd + react + t0;
            var cands = new List<PathPt>();
            foreach (var p in r.path) if ((recv == "me" ? p.y > Court.NET : p.y < Court.NET) && p.z > .25f && p.z < 3.0f) cands.Add(p);
            PathPt pick = null; bool overhead = false, kill = false, lunge = false;
            if (recv == "me") foreach (var p in cands) if (Math.Abs(p.y - Court.NET) < 2.3f && p.z > Court.NETH + .2f && p.z < 2.6f && need(p, 1.15f) <= p.t) { pick = p; kill = true; break; }
            if (pick == null) foreach (var p in cands) if (p.z > 2.1f && p.z < 2.8f && need(p, 1.15f) <= p.t) { pick = p; overhead = true; break; }
            if (pick == null) for (int i = cands.Count - 1; i >= 0; i--) { var p = cands[i]; if (p.z > .75f) continue; if (need(p, 1.15f) <= p.t) { pick = p; break; } }
            if (pick == null) foreach (var p in cands) if (need(p, 1.15f) <= p.t) { pick = p; break; }
            if (pick == null && recv == "me") for (int i = cands.Count - 1; i >= 0; i--) { var p = cands[i]; if (p.z > 1.3f) continue; if (need(p, 2.1f) <= p.t) { pick = p; lunge = true; break; } }
            icpt = pick == null ? null : new Intercept { t = pick.t, x = pick.x, y = pick.y, z = pick.z, who = recv, overhead = overhead, kill = kill, lunge = lunge };
            if (recv == "op" && icpt != null)
            {
                bool outp = IsOut(r.x, r.y, s.serve);
                float margin = Math.Min(Math.Min(Math.Abs(r.x - Court.SL), Math.Abs(r.x - Court.SR)), Math.Min(Math.Abs(r.y), Math.Abs(r.y - Court.L)));
                if ((outp && !(margin < .25f && R01() < .5f)) || (r.net != null && !r.net.ok)) icpt.leave = true;
                float q = R01(), acc = 0; var K = overhead ? stil.ustK : stil.altK; var Pp = overhead ? stil.ustP : stil.altP; string pk = K[K.Length - 1];
                for (int i = 0; i < K.Length; i++) { acc += Pp[i]; if (q < acc) { pk = K[i]; break; } }
                icpt.opType = pk;
            }
        }

        void DoSplit() { split = true; me.jump = Math.Max(me.jump, .5f); Emit(Ev.Split, "me", null, me.x, me.y); if (s != null && s.from == "op") PlanIntercept(); }

        // Dokunma anı: rakip vurduktan hemen sonra (0,3 s) dokunmak split step = daha hızlı çıkış.
        public void OnTouchDown()
        {
            if (s == null || s.dead || split) return;
            if (s.from == "op" && clock - opHitAt < .3f) DoSplit();
            else if (s.from == "me" && icpt != null && icpt.who == "op" && icpt.t - s.t < .3f) splitPending = true;
        }

        // Erişim: raket kolu ~1,15 m (Kolay 1,45); uzanmayla daha geniş ama yalnız alçak topta.
        public bool ReachInfo(out Reach R)
        {
            R = new Reach();
            if (s == null || s.dead || s.from != "op" || s.y <= Court.NET || s.z < .12f || s.z > 3.1f) return false;
            float d = Hyp(s.x - me.x, s.y - me.y); bool kill = s.y - Court.NET < 2.3f && s.z > Court.NETH + .2f;
            float R1 = easy ? 1.45f : 1.15f, R2 = easy ? 2.2f : 1.75f;
            if (d <= R1) { R = new Reach { lunge = false, over = s.z > 2.0f, kill = kill, d = d }; return true; }
            if (d <= R2 && s.z < 1.5f) { R = new Reach { lunge = true, d = d }; return true; }
            return false;
        }
        public bool HitWindow() { Reach r; return ReachInfo(out r); }

        // Kesişten vuruş: yön = parmağın yönü, hız = güç ve tür, topun merkezine uzaklık = isabet.
        public bool SwipeShot(SwipeInput sw)
        {
            Reach I; if (between || s == null || !ReachInfo(out I)) return false;
            float sp = sw.speed, up = -sw.dy, r = Clamp(sw.dx / Math.Max(50, Math.Abs(sw.dy)), -1.3f, 1.3f);
            float hx = s.x, hy = s.y, hz = Math.Max(.2f, s.z); bool overS = s.z > 2.0f;
            bool bh = HandOf(me, "me", s.x, s.z) == "back";
            string type;
            if (sw.tap) type = I.lunge ? "lift" : I.kill ? "kill" : "clear";
            else if (I.lunge) type = "lift"; else if (up < 0) type = "block";
            else if (sp > 1.5f && I.kill) type = "kill";
            else if (sp > 1.7f) type = overS ? (bh ? "clear" : (energy >= 1 ? "jump" : "smash")) : "drive";
            else if (sp < .75f) type = "drop"; else type = "clear";
            float tx = Court.MID + r * 2.4f, d, val; bool byAngle = true;
            switch (type)
            {
                case "kill": d = 2.6f; byAngle = false; val = 30; break;
                case "jump": d = 3.2f; byAngle = false; val = 78; energy = 0; break;
                case "smash": d = 4.2f; byAngle = false; val = 52; break;
                case "drive": d = 4.6f; byAngle = false; val = bh ? 18 : 22; break;
                case "drop": d = overS ? 1.2f : 1.0f; val = overS ? .12f : (Math.Abs(hy - Court.NET) < 2.5f ? .75f : .95f); break;
                case "block": d = .9f; val = .6f; tx = hx + r * 1.2f; break;
                case "lift": tx = Court.MID + r * 1.2f; d = 5.4f; val = 1.05f; break;
                default: d = Math.Min(bh ? 5.6f : 6.9f, (bh ? 4.6f : 5.5f) + (sp - .75f) * 1.4f); val = overS ? .85f : 1.0f; break;
            }
            float cq = sw.dist / sw.R;
            string q = cq < .4f ? "perfect" : cq <= 1 ? "good" : easy ? "good" : "poor"; if (I.lunge && q == "perfect") q = "good"; if (sw.tap) q = "good";
            float sig = ((q == "perfect" ? .1f : q == "good" ? .35f : .9f) + (I.lunge ? .4f : 0));
            Func<float> g = () => (R01() + R01() + R01() - 1.5f) * sig * 1.4f;
            float edge = q == "poor" ? sw.side * .8f : 0;
            bool tepeden = (type == "smash" || type == "jump") && s.z > 2.4f; if (tepeden) { val *= 1.08f; d -= .5f; }
            float vx, vy, vz; Shuttle.Aim(hx, hy, hz, tx + g() + edge, Court.NET - d + g() * .8f, byAngle, val, out vx, out vy, out vz);
            if (q == "poor") { float k = 1 + (R01() - .5f) * .16f; vx *= k; vy *= k; vz *= k; }
            if (q == "perfect")
            {
                pstreak++; energy = Math.Min(1, energy + .34f); freeze = Math.Max(freeze, .06f);
                Emit(Ev.Text, "me", type, hx, hy, hz, 0, type == "jump" ? "SIÇRAYARAK SMAÇ" : type == "smash" ? "SMAÇ!" : type == "kill" ? "FİLE VURUŞU" : pstreak > 1 ? "Tam orta ×" + pstreak : (bh ? "Backhand, tam orta" : "Tam orta"));
            }
            else { pstreak = 0; Emit(Ev.Text, "me", type, hx, hy, hz, 0, q == "poor" ? "Sıyırdın" : Label(type, bh)); }
            if (I.lunge) { me.dive = 1; me.stun = .35f; Emit(Ev.Text, "me", "lunge", hx, hy, hz, 0, "Uzandın!"); }
            if (type == "jump") me.jump = 1;
            if (tepeden) Emit(Ev.Text, "me", type, hx, hy, hz, 0, "Tepeden!");
            me.sw = 1;
            string ft = type == "jump" || type == "kill" ? "smash" : type == "lift" ? "clear" : type == "block" ? "drop" : type;
            StartFlight("me", vx, vy, vz, hx, hy, hz, false, ft, type == "jump", q);
            return true;
        }
        static string Label(string type, bool bh)
        {
            switch (type) { case "clear": return (bh ? "Backhand " : "Forehand ") + "clear"; case "drive": return (bh ? "Backhand " : "Forehand ") + "drive"; case "drop": return "Drop"; case "block": return "Blok"; case "lift": return "Kaldırdın"; case "kill": return "FİLE VURUŞU"; case "smash": return "SMAÇ!"; case "jump": return "SIÇRAYARAK SMAÇ"; }
            return type;
        }

        // Servis: kareye doğru kaydır. Yavaş = kısa servis, hızlı = uzun servis.
        public bool SwipeServe(float dx, float dy, float speed)
        {
            if (s != null || between || server != "me" || wait > 0 || over) return false; hint = false;
            float cx = (serveBox.x0 + serveBox.x1) / 2, r = Clamp(dx / Math.Max(50, Math.Abs(dy)), -1.3f, 1.3f);
            bool lng = speed > 1.0f; float hx = me.x, hy = me.y - .5f, hz = 1.0f, d = lng ? 6.2f : Court.SSL + .45f;
            float vx, vy, vz; Shuttle.Aim(hx, hy, hz, cx + r * 1.5f, Court.NET - d, true, lng ? 1.0f : .32f, out vx, out vy, out vz);
            me.sw = 1; StartFlight("me", vx, vy, vz, hx, hy, hz, true, lng ? "clear" : "drop", false, "good"); return true;
        }

        // Usta modu: dokunulan kort noktasına koş.
        public void RunTo(float x, float y, float manualFor)
        {
            me.tx = Clamp(x, -.4f, Court.CW + .4f); me.ty = Clamp(y, Court.NET + .3f, Court.L + .8f); me.manualT = manualFor;
        }

        void Plan(string type, float dxAim, bool overhead, out float tx, out float ty, out bool byAngle, out float val)
        {
            Func<float, float> yN = d => Court.NET + d; tx = Court.MID + dxAim * (Court.SR - Court.SL) * .62f; byAngle = true;
            switch (type)
            {
                case "smash": ty = yN(3.6f + R01() * 1.2f); byAngle = false; val = 52; break;
                case "drive": ty = yN(4.6f); byAngle = false; val = 22; break;
                case "drop": ty = yN(overhead ? 1.2f : 0.9f); val = overhead ? .12f : .75f; break;
                default: ty = yN(Court.L / 2 - .55f); val = overhead ? .85f : 1.0f; break;
            }
        }

        void OpHit()
        {
            var I = icpt; float hx = s.x, hy = s.y, hz = Math.Max(.2f, s.z);
            string q = R01() < .12f ? "poor" : R01() < .3f ? "perfect" : "good";
            bool obh = HandOf(op, "op", hx, hz) == "back";
            string type = I.opType ?? (I.overhead ? "clear" : "drop"); if (obh && type == "smash") type = "clear";
            float vx, vy, vz;
            // Rakip file önünde arada gevşek top bırakır: sana öldürme şansı.
            if (type == "drop" && !I.overhead && hy > Court.NET - 2.6f && R01() < .35f)
            {
                Shuttle.Aim(hx, hy, hz, Court.MID + (R01() - .5f) * 2.4f, Court.NET + 1.0f, true, 1.15f, out vx, out vy, out vz);
                StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drop", false, q); return;
            }
            float tx, ty, val; bool byAngle; Plan(type, (R01() * 2 - 1) * .8f, I.overhead, out tx, out ty, out byAngle, out val);
            if (obh && type == "clear") ty = Math.Max(ty, Court.NET + 1.2f);
            float hata = stil.hata * (.5f + (easy ? .3f : 0) + (obh ? .35f : 0) + Math.Min(.9f, Behind() * .15f));
            Func<float> g = () => (R01() - .5f) * hata;
            Shuttle.Aim(hx, hy, hz, tx + g(), ty + g(), byAngle, val, out vx, out vy, out vz);
            var chk = Shuttle.Simulate(hx, hy, hz, vx, vy, vz, false, Shuttle.KD);
            if (chk.net != null && !chk.net.ok && R01() < .9f) Shuttle.Aim(hx, hy, hz, tx, ty, true, Math.Max(.75f, byAngle ? val : .75f), out vx, out vy, out vz);
            StartFlight("op", vx, vy, vz, hx, hy, hz, false, type, false, q);
        }

        void OpServe()
        {
            float hx = op.x, hy = op.y + .5f, hz = 1.0f; var b = serveBox; bool lng = R01() < .4f;
            float tx = b.x0 + .3f + R01() * (b.x1 - b.x0 - .6f), ty = lng ? b.y1 - .6f : b.y0 + .5f;
            float vx, vy, vz; Shuttle.Aim(hx, hy, hz, tx, ty, true, lng ? 1.0f : .32f, out vx, out vy, out vz);
            StartFlight("op", vx, vy, vz, hx, hy, hz, true, lng ? "clear" : "drop", false, "good");
        }

        // Ralli sistemi: sayıyı alan servisi alır. 21'de 2 fark, 30'da biter.
        void Point(string winner, string why, float x, float y)
        {
            if (between) return; between = true; sc[winner == "me" ? 0 : 1]++; server = winner;
            Emit(Ev.Point, winner, s != null ? s.type : null, x, y, 0, rally, why);
            int a = sc[0], b = sc[1], cap = target == 21 ? 30 : target == 11 ? 15 : 10;
            matchDone = (Math.Max(a, b) >= target && Math.Abs(a - b) >= 2) || Math.Max(a, b) >= cap;
            if (matchDone) { slow = .3f; slowT = .9f; betweenT = 1.4f; }
            else { betweenT = 1.3f; }
        }

        void Land()
        {
            s.dead = true; string hitter = s.from, other = hitter == "me" ? "op" : "me";
            if (!s.netHit) Emit(Ev.Floor, hitter, s.type, s.x, s.y, 0, 0, null, null, s.type == "smash");
            if (s.netHit) { Point(other, hitter == "me" ? "Fileye takıldı" : "Rakip fileye taktı", s.x, s.y); return; }
            if (IsOut(s.x, s.y, s.serve)) Point(other, s.serve ? (hitter == "me" ? "Servis hatası" : "Rakip servis hatası") : (hitter == "me" ? "Dışarı" : "Dışarı! Bıraktın, sayı senin"), s.x, s.y);
            else { if (s.type == "smash" && hitter == "me") op.dive = 1; Point(hitter, hitter == "me" ? (s.super ? "SIÇRAYARAK SMAÇ SAYISI" : s.type == "smash" ? "SMAÇ SAYISI" : "SAYI!") : "Kaçtı", s.x, s.y); }
        }

        // Oyuncu hareketi: hedefe ivmeyle koşar, yaklaşınca frenler. Kolay modda kesişme noktasına kendi koşar.
        void RunMe(float d)
        {
            var o = me; if (s == null) { o.vx = o.vy = 0; return; }   // servis atılana kadar yerinde (kural)
            if (o.stun > 0) { o.stun -= d; o.vx *= .8f; o.vy *= .8f; return; }
            o.manualT = Math.Max(0, o.manualT - d);
            if (easy && o.manualT <= 0)
            {
                var I = icpt;
                if (s.from == "op" && I != null && I.who == "me") { o.tx = Clamp(I.x - HitSide(o, "me", I), -.4f, Court.CW + .4f); o.ty = I.y + (I.lunge ? .6f : .25f); }
                else if (s.from == "op" && s.landPred != null && s.landPred.y > Court.NET) { o.tx = s.landPred.x; o.ty = s.landPred.y + .3f; }
                else if (s.from == "me") { o.tx = Court.MID; o.ty = Court.L - 3.3f; }
            }
            float max = MeSpd(), A = split ? 20 : (easy ? 18 : 14), dvx = 0, dvy = 0;
            if (o.HasTarget) { float dx = o.tx - o.x, dy = o.ty - o.y, dist = Hyp(dx, dy); if (dist > .02f) { float v = Math.Min(max, (float)Math.Sqrt(2 * A * dist)); dvx = dx / dist * v; dvy = dy / dist * v; } else o.tx = float.NaN; }
            float ex = dvx - o.vx, ey = dvy - o.vy, el = Hyp(ex, ey), st = A * d;
            if (el > st) { o.vx += ex / el * st; o.vy += ey / el * st; } else { o.vx = dvx; o.vy = dvy; }
            o.x = Clamp(o.x + o.vx * d, -.6f, Court.CW + .6f); o.y = Clamp(o.y + o.vy * d, Court.NET + .25f, Court.L + 1);
        }

        static void Move(Athlete o, float x, float y, float step) { float dx = x - o.x, dy = y - o.y, d = Hyp(dx, dy); if (d < 1e-3f) return; float k = Math.Min(1, step / d); o.x += dx * k; o.y += dy * k; }

        void Prep(Athlete o, string who)
        {
            float tg = 0; var I = icpt;
            if (s != null && !s.dead && s.from != who && I != null && I.who == who && I.t - s.t < .7f)
            { tg = 1; o.prepShot = I.lunge ? "lunge" : I.kill ? "kill" : (I.z > 1.95f ? "over" : "low"); o.prepHand = HandOf(o, who, I.x, I.z); o.prepClip = ClipHand(o.prepShot == "over" ? "clear" : o.prepShot, o.prepHand); }
            else if (s == null && server == who) { tg = .6f; o.prepShot = "low"; o.prepClip = "serve"; }   // servisçi topu elinde tutar
            if (who == "me" && s != null && !s.dead && s.from == "op")
            { Reach ri; if (ReachInfo(out ri)) { tg = 1; o.prepShot = ri.lunge ? "lunge" : ri.kill ? "kill" : ri.over ? "over" : "low"; o.prepHand = HandOf(o, "me", s.x, s.z); o.prepClip = ClipHand(o.prepShot == "over" ? "clear" : o.prepShot, o.prepHand); } }
            o.prepA += (tg - o.prepA) * Math.Min(1, lastDt * 9);
        }
        float lastDt;

        public void Update(float dt)
        {
            lastDt = dt;
            foreach (var q in new[] { me, op })
            {
                if (q == op && dt > 0 && !float.IsNaN(q.px)) { q.vx = (q.x - q.px) / dt; q.vy = (q.y - q.py) / dt; }
                if (q == op) { q.px = q.x; q.py = q.y; }
                q.runPh = (q.runPh + Hyp(q.vx, q.vy) * dt * .8f) % 1f; q.splitT = Math.Max(0, q.splitT - dt);
            }
            if (freeze > 0) { freeze -= dt; return; }
            if (slowT > 0) { slowT -= dt; if (slowT <= 0) slow = 1; }
            float d = dt * slow * (HitWindow() ? (easy ? .3f : .45f) : 1);
            net.Advance(d);
            if (over) return;
            if (between) { betweenT -= dt; if (betweenT <= 0) { if (matchDone) { over = true; Emit(Ev.MatchEnd, sc[0] > sc[1] ? "me" : "op"); return; } SetupServe(); } }
            clock += d; me.dive = Math.Max(0, me.dive - d * 1.5f); me.jump = Math.Max(0, me.jump - d * 2.2f); op.jump = Math.Max(0, op.jump - d * 2.2f);
            me.sw = Math.Max(0, me.sw - d * 4); op.sw = Math.Max(0, op.sw - d * 4); op.dive = Math.Max(0, op.dive - d * 1.5f);
            if (wait > 0) { wait -= d; if (wait <= 0 && server == "op" && s == null && !between) { wait = 0; OpServe(); } }
            RunMe(d); Prep(me, "me"); Prep(op, "op");
            if (s == null || s.dead) return;
            float rem = d;
            while (rem > 1e-6f)
            {
                float h = Math.Min(Shuttle.DT, rem); float py = s.y;
                if (s.netHit) { s.vy *= (float)Math.Exp(-4 * h); s.vx *= (float)Math.Exp(-4 * h); }
                s.Step(h); s.t += h; rem -= h;
                if (!s.netDone && (py > Court.NET) != (s.y > Court.NET))
                {
                    s.netDone = true; float lim = Court.NetLimit(s.x);
                    if (s.netPred != null ? !s.netPred.ok : s.z <= lim)
                    {
                        float sp = s.Speed, k = Math.Min(1, sp / 55f); net.Impulse(s.x, s.z, sp, false);
                        // Hızlı top fileyi esnetip geri tükürür, yavaş top yapışıp iner; banda çarpan kordondan yuvarlanır.
                        bool tape = lim - s.z < .06f; s.vx *= tape ? .4f : .15f; s.vy = (py > Court.NET ? 1 : -1) * (tape ? .25f : .15f + .85f * k);
                        s.vz = tape ? .3f : Math.Min(0, s.vz * .2f); s.netHit = true; s.netK = k;
                        Emit(Ev.Net, s.from, s.type, s.x, Court.NET, s.z, k, null, null, tape);
                    }
                    else if (lim - s.z > -.045f && s.z > lim) { net.Impulse(s.x, s.z, s.Speed, true); Emit(Ev.Tape, s.from, s.type, s.x, Court.NET, s.z, 0, "Bandı sıyırdı!"); }
                }
                if (s.z <= 0) { s.z = 0; Land(); return; }
            }
            var I = icpt;
            if (s.from == "me")
            {
                if (I != null && I.who == "op" && !I.leave)
                {
                    Move(op, Clamp(I.x + HitSide(op, "op", I), -.4f, Court.CW + .4f), I.y - .3f, OpSpd() * d);
                    if (I.opType == "smash" && I.t - s.t < .32f && I.t - s.t > 0) op.jump = Math.Max(op.jump, 1 - (I.t - s.t) / .32f * .6f);
                    if (s.t >= I.t) { OpHit(); return; }
                }
                else
                {
                    Move(op, s.landPred.x, s.landPred.y - .4f, 2.2f * d);
                    if (op.dive <= 0 && !IsOut(s.landPred.x, s.landPred.y, s.serve) && !(s.landPred.net != null && !s.landPred.net.ok) && s.t > s.landPred.t - .25f)
                    { op.dive = 1; Emit(Ev.Text, "op", null, op.x, op.y, 0, 0, "Kıl payı!"); slow = .35f; slowT = .3f; }
                }
            }
            else Move(op, Court.MID, stil.ev, 2.0f * d);
        }

        // Poz seçimi (HTML player()): vuruş → hazırlık → split → badminton ayak işi → bekleyiş.
        public static void PickClip(Athlete o, bool isMe, float clockNow, out string clip, out float ph)
        {
            float spd = Hyp(o.vx, o.vy); float hit;
            if (o.sw > 0) { clip = o.clip ?? "low"; hit = CLIP_HIT.TryGetValue(clip, out hit) ? hit : .55f; ph = hit + (1 - o.sw) * (1 - hit); }
            else if (o.prepA > .08f && o.prepClip != null) { clip = o.prepClip; hit = CLIP_HIT.TryGetValue(clip, out hit) ? hit : .55f; ph = hit * .9f * Math.Min(1, o.prepA); }
            else if (o.splitT > 0 && spd < 1.5f) { clip = "split"; ph = 1 - o.splitT / .3f; }
            else if (spd > .7f)
            {
                // Oyuncunun kendi bakışında: ileri = fileye, sağ = raket tarafı.
                float fwdv = isMe ? -o.vy : o.vy, side = isMe ? o.vx : -o.vx;
                clip = Math.Abs(side) > Math.Abs(fwdv) * 1.15f ? (side > 0 ? "chasse_r" : "chasse_l") : fwdv > 0 ? "fwd" : (side >= 0 ? "back_r" : "back_l");
                ph = o.runPh;
            }
            else { clip = "idle"; ph = (clockNow * .8f + (isMe ? 0 : .5f)) % 1f; }
        }
    }
}
