// Sahnenin tamamı koddan kurulur: kamera, ışık, kort (saha teması), tribün, hakemler, file, top, iki oyuncu, ses, HUD.
// Akış: menü → (ilk açılışta 3 adımlık antrenman → 5 sayılık Kolay ilk maç) → maç → maç sonu. Girdi (kaydırma) → Match
// (saf C#) → olaylar (vuruş, file, sayı) → görünüm ve "his" (vuruş duraklaması Match'te; kamera sarsıntısı/zoom, tüy
// patlaması, ses, titreşim burada). Hareketi azalt açıkken sarsıntı, zoom ve vuruş duraklaması yok, patlamalar üçte bir.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public enum Ekran { Menu, Oyun, Bitti }

    public class GameRoot : MonoBehaviour
    {
        public Match match; public Camera cam; public Ekran ekran = Ekran.Menu; public bool paused;
        public string bittiBaslik, bittiNot, bittiBilgi, bittiDugme; public bool bittiIlk; public float rakipKartT;
        PlayerView me, op, hk1, hk2; ShuttleView shuttle; NetView net; Sfx sfx; Hud hud; ParticleSystem feathers; GameObject court; string courtId, modelId;
        float shake, punch; Vector3 camBase; Quaternion camRot; float camFov; int seed = 1; Light sun; readonly System.Random rnd = new System.Random();
        bool ogrKayit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (FindFirstObjectByType<GameRoot>() == null) new GameObject("BirdieSmash").AddComponent<GameRoot>(); }

        void Awake()
        {
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep; Ayar.Yukle();
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c.gameObject.scene.IsValid() && c.transform.root != transform) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.transform.root != transform) l.gameObject.SetActive(false);
            var cg = new GameObject("kamera"); cg.transform.SetParent(transform, false); cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.nearClipPlane = .1f; cam.farClipPlane = 80;
            // Yayın ışığı: anahtar ışık kısık (kortun dışı kararsın), dolgu ortamı koyu; kort üstünü CourtView'in tavan lambaları açar.
            var lg = new GameObject("isik"); lg.transform.SetParent(transform, false); sun = lg.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.intensity = .75f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .5f; lg.transform.rotation = Quaternion.Euler(62, -24, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Kit.Hex("#6f8590"); RenderSettings.ambientEquatorColor = Kit.Hex("#3b4c55"); RenderSettings.ambientGroundColor = Kit.Hex("#1a2420");
            QualitySettings.shadowDistance = 40;
            shuttle = ShuttleView.Build(transform); sfx = Sfx.Build(transform);
            feathers = MakeFeathers();
            hud = gameObject.AddComponent<Hud>(); hud.root = this;
            match = new Match(1, new MatchOptions { easy = Ayar.easy, target = Ayar.target, saha = Ayar.saha });   // menü arkasında duran sahne
            Kur(match);
            AudioListener.volume = Ayar.ses ? 1 : 0;
        }

        // Sahneyi maça göre kur: saha değiştiyse kort, model değiştiyse oyuncular ve hakemler, rakip forması her maçta.
        void Kur(Match M)
        {
            if (court == null || courtId != M.saha) { if (court != null) Destroy(court); Color bg; court = CourtView.Build(transform, M.saha, out bg); cam.backgroundColor = bg; courtId = M.saha; }
            if (me == null || modelId != Ayar.model)
            {
                if (me != null) { Destroy(me.gameObject); Destroy(hk1.gameObject); Destroy(hk2.gameObject); }
                me = PlayerView.Build(transform, Ayar.model, true, Kit.Hex(Ayar.FORMA));
                hk1 = PlayerView.Build(transform, Ayar.model, false, Kit.Hex("#0a1530"), true); hk2 = PlayerView.Build(transform, Ayar.model, false, Kit.Hex("#0a1530"), true);
                modelId = Ayar.model;
            }
            if (op != null) Destroy(op.gameObject);
            op = PlayerView.Build(transform, Ayar.model, false, Kit.Hex(Match.OpRenk(M.stil, Ayar.FORMA)));
            if (net != null) Destroy(net.gameObject); net = NetView.Build(transform, M.net);
            input.Reset(); lastW = 0;
        }

        public void ModelDegisti() { modelId = null; Kur(match); }
        public void SahaDegisti() { if (ekran != Ekran.Oyun) { match.saha = Ayar.saha; Kur(match); } }

        public void Oyna() { if (!Ayar.antrenman) StartMatch(true, true, false); else StartMatch(false, false, false); }
        public void Antrenman() { StartMatch(true, false, false); }

        public void StartMatch(bool ogr, bool kisa, bool ilk)
        {
            var o = new MatchOptions { easy = Ayar.easy, target = Ayar.target, reduce = Ayar.reduce, ilk = ilk, ogr = ogr, ogrKisa = kisa, bestRally = Ayar.bestRally, saha = Ayar.saha };
            if (Ayar.kariyerModu && !ogr) { int i = Career.Basamak(Ayar.kariyer); o.stil = Match.KariyerRakip(i); o.saha = Match.KARIYER[i].saha; }
            else o.stil = Match.STIL[Match.STIL_SIRA[rnd.Next(3)]];
            match = new Match(seed++ * 7919 + (int)(Time.realtimeSinceStartup * 1000), o);
            Kur(match); ekran = Ekran.Oyun; paused = false; ogrKayit = false; rakipKartT = ogr ? 0 : 1.5f; erkenN = 0;
        }
        public void OgrGec()
        {
            if (match.ogr == null) return; Ayar.antrenman = true; Ayar.Kaydet(); bool kisa = match.ogr.kisa;
            if (kisa) StartMatch(false, false, true); else StartMatch(false, false, false);
        }
        public void AnaMenu() { ekran = Ekran.Menu; paused = false; hud.panel = "ana"; }
        public void Duraklat() { if (ekran == Ekran.Oyun && !paused) { paused = true; input.Reset(); } }
        public void Devam() { paused = false; }
        void OnApplicationPause(bool p) { if (p) Duraklat(); }   // arka plana geçince kendiliğinden duraklar; top bir anda üstüne düşmez

        void EndMatch(bool won)
        {
            ekran = Ekran.Bitti; var M = match; bittiIlk = M.ilk;
            if (won) Ayar.wins++;
            string kNot = "";
            if (M.stil.kariyer >= 0) Career.Apply(ref Ayar.kariyer, M.stil.kariyer, won, out kNot);
            Ayar.Kaydet();
            bittiBaslik = (won ? "Kazandın" : "Bu sefer rakip aldı") + (kNot != "" ? " · " + kNot : "");
            bittiBilgi = "Rakip " + M.RakipAd(false) + " · En uzun ralli " + M.maxRally + " · " + M.smashes + " smaç · " + M.kills + " file vuruşu · " + M.perfects + " mükemmel · " + M.deceives + " aldatma · " + M.splits + " split step · " + M.tepeden + " tepeden · " + M.saves + " uzanma · " + M.outsLeft + " kez dışarıyı bıraktın";
            bittiDugme = Career.AgainLabel(M.stil.kariyer >= 0, won, Ayar.kariyer);
        }
        public void TekrarOyna() { StartMatch(false, false, false); }

        ParticleSystem MakeFeathers()
        {
            var g = new GameObject("tuyler"); g.transform.SetParent(transform, false); var ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.loop = false; m.playOnAwake = false; m.startLifetime = .5f; m.startSpeed = 2.4f; m.startSize = .07f; m.gravityModifier = .25f; m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 300;
            var e = ps.emission; e.enabled = false; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .05f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) } });
            var r = g.GetComponent<ParticleSystemRenderer>(); r.material = Kit.Clear(Color.white, Kit.RadialTex()); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
        void Burst(Vector3 at, int n, float speed, Color c)
        {
            if (Ayar.reduce) n = Mathf.CeilToInt(n / 3f);
            var ep = new ParticleSystem.EmitParams { position = at, applyShapeToPosition = true };
            for (int i = 0; i < n; i++) { ep.velocity = Random.onUnitSphere * speed * Random.Range(.3f, 1f); ep.startColor = c; feathers.Emit(ep, 1); }
        }

        // Dikey ekranda kortun tamamı, iki oyuncu ve rakibin arkasındaki pano görünsün: konum sabit, görüş açısı ekrana göre.
        int lastW, lastH;
        void FitCamera()
        {
            if (Screen.width == lastW && Screen.height == lastH) return; lastW = Screen.width; lastH = Screen.height;
            camBase = new Vector3(0, 10.5f, -Court.L / 2 - 6.2f); var target = new Vector3(0, .4f, .9f);
            camRot = Quaternion.LookRotation(target - camBase); cam.transform.SetPositionAndRotation(camBase, camRot);
            var pts = new List<Vector3>();
            foreach (float x in new[] { 0f, Court.CW }) foreach (float y in new[] { 0f, Court.L }) { pts.Add(Kit.W(x, y, 0)); }
            pts.Add(Kit.W(Court.MID, 0.4f, 2.6f)); pts.Add(Kit.W(Court.MID, Court.L + .8f, 0)); pts.Add(Kit.W(Court.MID, -1.3f, .9f));
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height), tv = 0;
            foreach (var p in pts) { var l = Quaternion.Inverse(camRot) * (p - camBase); tv = Mathf.Max(tv, Mathf.Abs(l.y / l.z), Mathf.Abs(l.x / l.z) / aspect); }
            camFov = 2 * Mathf.Atan(tv * 1.06f) * Mathf.Rad2Deg; cam.fieldOfView = camFov;
        }

        // ---- kaydırma girdisi: piksel → 640 px yükseklikli referans (HTML eşikleri aynen geçerli) ----
        class Swipe { public bool on, hit, moved; public float minD = 1e9f; public List<Vector3> tr = new List<Vector3>(); public Vector2 p0; public float t0; public void Reset() { on = hit = moved = false; minD = 1e9f; tr.Clear(); } }
        readonly Swipe input = new Swipe(); int erkenN;
        float Ref { get { return 640f / Mathf.Max(1, Screen.height); } }
        Vector2 RefPos(Vector2 screen) { return new Vector2(screen.x * Ref, (Screen.height - screen.y) * Ref); }   // y aşağı

        void SwipeVec(out float dx, out float dy, out float speed)
        {
            var tr = input.tr; var last = tr[tr.Count - 1]; var a = tr[0];
            foreach (var p in tr) if (last.z - p.z <= 90) { a = p; break; }   // pencere son harekete göre (HTML swipeVec)
            float dt = Mathf.Max(16, last.z - a.z); dx = last.x - a.x; dy = last.y - a.y; speed = Mathf.Sqrt(dx * dx + dy * dy) / dt;
        }
        // Kavis (slice): izin ilk yarısı ile ikinci yarısının yönü 0,8 rad'dan fazla ayrışıyorsa ve ikinci yarı 25 px'ten uzunsa.
        bool Curved()
        {
            var tr = input.tr; if (tr.Count < 4) return false; int n = tr.Count, m = n / 2; Vector3 a = tr[0], b = tr[m], c = tr[n - 1];
            float a1 = Mathf.Atan2(b.y - a.y, b.x - a.x), a2 = Mathf.Atan2(c.y - b.y, c.x - b.x), da = Mathf.Abs(a2 - a1); if (da > Mathf.PI) da = 2 * Mathf.PI - da;
            return da > .8f && Vector2.Distance(c, b) > 25;
        }
        static float SegDist(Vector2 p, Vector2 a, Vector2 b, out float side)
        {
            Vector2 v = b - a; float L2 = v.sqrMagnitude, t = L2 > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, v) / L2) : 0;
            side = Mathf.Sign(v.x * (p.y - a.y) - v.y * (p.x - a.x)); return Vector2.Distance(p, a + v * t);
        }
        bool CourtPoint(Vector2 screen, out Vector2 c)
        {
            var ray = cam.ScreenPointToRay(screen); c = Vector2.zero; if (Mathf.Abs(ray.direction.y) < 1e-4f) return false;
            float t = -ray.origin.y / ray.direction.y; if (t < 0) return false; c = Kit.FromWorld(ray.origin + ray.direction * t); return true;
        }
        float HitR() { return 40 * (match.easy ? 1.3f : 1); }
        public Vector2 BallRef() { var M = match; if (M.s == null) return Vector2.zero; return RefPos(cam.WorldToScreenPoint(Kit.W(M.s.x, M.s.y, M.s.z))); }
        public bool Parmak { get { return input.on; } }
        public void CanliOkuma(out string tip, out float sp)
        {
            tip = null; sp = 0; var M = match; if (!input.on || input.tr.Count < 2) return; float dx, dy; SwipeVec(out dx, out dy, out sp);
            var a = M.OgrA(); if (a == null) return;
            if (a.servis != null) { if (M.s != null || M.svPending != null) return; var q = input.tr[input.tr.Count - 1]; float ddx = q.x - input.p0.x, ddy = q.y - input.p0.y; if (-ddy > 30) tip = Match.ServisTuru(ddx, ddy, sp); return; }
            Reach I; if (M.s == null || !M.ReachInfo(out I)) return;
            var pl = M.ShotPlan(new SwipeInput { dx = dx, dy = dy, speed = sp, curved = Curved() }, I, true); tip = pl.type;
        }

        void ReadInput()
        {
            bool down, held, up; Vector2 pos;
            if (Input.touchCount > 0) { var t = Input.GetTouch(0); pos = t.position; down = t.phase == TouchPhase.Began; up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled; held = !down && !up; }
            else { pos = Input.mousePosition; down = Input.GetMouseButtonDown(0); up = Input.GetMouseButtonUp(0); held = Input.GetMouseButton(0) && !down; }
            float now = Time.unscaledTime * 1000; var q = RefPos(pos); var M = match;
            if (down)
            {
                if (hud.Blocks(new Vector2(pos.x, Screen.height - pos.y))) return;   // HUD düğmesine dokunuş kaydırma değil
                if (rakipKartT > 0) { rakipKartT = 0; }
                input.Reset(); input.on = true; input.p0 = q; input.t0 = now; input.tr.Add(new Vector3(q.x, q.y, now)); M.OnTouchDown();
                Vector2 c; if (!M.easy && M.s != null && !M.HitWindow() && CourtPoint(pos, out c)) M.RunTo(c.x, c.y, 1.4f);
            }
            else if (held && input.on)
            {
                var prev = input.tr[input.tr.Count - 1]; input.tr.Add(new Vector3(q.x, q.y, now));
                while (input.tr.Count > 2 && now - input.tr[0].z > 300) input.tr.RemoveAt(0);
                if (Vector2.Distance(q, input.p0) > 18) input.moved = true;
                float segSpeed = Vector2.Distance(q, prev) / Mathf.Max(8, now - prev.z);
                Vector2 c; if (!M.easy && M.s != null && !M.HitWindow() && !input.hit && segSpeed < .6f && CourtPoint(pos, out c)) M.RunTo(c.x, c.y, 1.4f);
                if (!input.hit && M.s != null && !M.s.dead && M.s.from == "op" && M.HitWindow())
                {
                    var sr = BallRef(); float side;
                    float d = SegDist(sr, new Vector2(prev.x, prev.y), q, out side), far = Vector2.Distance(q, input.p0), R = HitR();
                    input.minD = Mathf.Min(input.minD, d / R);
                    float dx, dy, speed; SwipeVec(out dx, out dy, out speed);
                    // Topun üstünden geçmek şart değil: pencere açıkken belirgin kaydırma (70 px, kavis için yer) vuruştur; topu kesmek mükemmeli verir.
                    bool durdu = M.ogr != null && M.ogr.dur;
                    if (!durdu && (d <= R || (far > 70 && speed > .45f)))
                        if (M.SwipeShot(new SwipeInput { dx = dx, dy = dy, speed = speed, dist = d <= R ? d : R * 1.5f, R = R, side = side, curved = Curved() })) { input.hit = true; Vibrate(M.events); }
                }
            }
            else if (up && input.on)
            {
                input.on = false; input.tr.Add(new Vector3(q.x, q.y, now));
                if (M.s == null) { float dy0 = q.y - input.p0.y, dx0 = q.x - input.p0.x; if (-dy0 > 30) { float dx, dy, speed; SwipeVec(out dx, out dy, out speed); M.SwipeServe(dx0, dy0, speed); } return; }
                float far = Vector2.Distance(q, input.p0);
                if (!input.hit && M.HitWindow())
                {
                    if (far < 24) M.SwipeShot(new SwipeInput { dx = 0, dy = -1, speed = 1, dist = HitR() * 1.5f, R = HitR(), tap = true });   // dokunma = güvenli clear
                    else if (M.ogr != null && M.ogr.dur)
                    {   // antrenmanda top durur: vuruş parmak kalkınca, bütün hareketin hızıyla okunur
                        float dx, dy, speed; SwipeVec(out dx, out dy, out speed);
                        M.SwipeShot(new SwipeInput { dx = dx, dy = dy, speed = speed, dist = Mathf.Min(input.minD, 1.5f) * HitR(), R = HitR(), curved = Curved() });
                    }
                    else
                    {   // "doğru zamanda vurdum ama geçti": ıskanın nedeni bir kelimeyle
                        float dx, dy, speed; SwipeVec(out dx, out dy, out speed);
                        string neden = far < 70 ? "Kısa kaydırdın" : speed <= .45f ? "Yavaş kaydırdın" : input.minD < 2.2f ? "Kıl payı: topun üstünden geç" : "Topun uzağından geçtin";
                        hud.Say("Iska · " + neden, Kit.W(M.me.x, M.me.y, 1.6f), Kit.Hex("#ffd27a"));
                    }
                }
                else if (!input.hit && !M.s.dead && M.s.from == "op" && !M.HitWindow() && far > 60 && erkenN < 3) { erkenN++; hud.Say("Erken: top parlayınca kaydır", Kit.W(M.me.x, M.me.y, 1.6f), Kit.Hex("#ffd27a")); }   // maç başına en çok 3 kez
                else if (M.easy && !input.hit && !M.HitWindow() && far < 18 && now - input.t0 < 250)
                { Vector2 c; if (CourtPoint(pos, out c)) M.RunTo(c.x, c.y, .7f); }   // Kolay'da kısa dokunuş yer değiştirir
            }
        }
        static void Vibrate(List<GameEvent> ev)
        {
#if UNITY_IOS || UNITY_ANDROID
            foreach (var e in ev) if (e.kind == Ev.Hit && e.type == "smash") { Handheld.Vibrate(); break; }
#endif
        }

        void Update()
        {
            FitCamera();
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            if (ekran == Ekran.Oyun && !paused)
            {
                ReadInput(); match.Update(dt); if (rakipKartT > 0) rakipKartT -= dt;
                foreach (var e in match.events) OnEvent(e); match.events.Clear();
                if (match.bestRally > Ayar.bestRally) { Ayar.bestRally = match.bestRally; Ayar.Kaydet(); }
                var o = match.ogr;
                if (o != null && o.bitti && !ogrKayit) { ogrKayit = true; Ayar.antrenman = true; Ayar.Kaydet(); }
                if (o != null && o.macaGec) StartMatch(false, false, o.kisa);
            }
            me.Show(match.me, match, dt); op.Show(match.op, match, dt); shuttle.Show(ekran == Ekran.Menu ? null : match.s);
            Hakemler();
            shake *= Mathf.Pow(.002f, dt); punch = 1 + (punch - 1) * Mathf.Pow(.0005f, dt); if (punch < 1) punch = 1;
            var j = shake > .01f ? (Vector3)(Random.insideUnitCircle * shake * .012f) : Vector3.zero;
            cam.transform.SetPositionAndRotation(camBase + camRot * j, camRot); cam.fieldOfView = camFov / punch;
        }

        // Baş hakem sayı bitince skoru girer (başı eğilir); servis el değişince yeni servisçinin tarafına döner. Servis hakemi
        // servisi atacak oyuncuya bakar, el değişince ~0,3 sn sonra döner. Ralli sırasında kıpırdamazlar; hareketi azaltta hareketsiz.
        void Hakemler()
        {
            var M = match; float yaw = 90, tilt = 0;
            if (M.hakemT > 0 && !Ayar.reduce) { float e = (M.hakemT0 - M.hakemT) * 1.4f / M.hakemT0; if (M.hakemSO) yaw = 90 + (M.hakemYon == "me" ? 30 : -30) * Mathf.Clamp01(e * 3); else if (e < 1.05f) tilt = 14 * Mathf.Sin(Mathf.Clamp01(e) * Mathf.PI); }
            hk1.ShowStatic(CourtView.HAKEM1, yaw, tilt);
            string yeni = M.server, eski = M.sjYon ?? yeni; float k = eski != yeni && M.sjT > 0 ? Mathf.Clamp01(M.sjT / .5f) : 0;
            float a0 = eski == "me" ? -125 : -55, a1 = yeni == "me" ? -125 : -55;
            hk2.ShowStatic(CourtView.HAKEM2, Mathf.Lerp(a0, a1, k), 0);
        }

        void OnEvent(GameEvent e)
        {
            var at = Kit.W(e.x, e.y, e.z); bool mine = e.who == "me"; bool rm = Ayar.reduce;
            switch (e.kind)
            {
                case Ev.Hit:
                    sfx.Hit(e.type, (int)e.k, mine); Burst(at, e.type == "smash" ? 14 : 6, e.type == "smash" ? 3.2f : 1.8f, Color.white);
                    if (e.type == "smash" && !rm) { shake = Mathf.Max(shake, e.big ? 12 : 6); punch = Mathf.Max(punch, e.big ? 1.09f : 1.05f); }
                    if (e.quality == "perfect" && mine) Burst(at, 12, 3, Kit.Hex("#d8ff5a"));
                    break;
                case Ev.Net: net.sign = mine ? -1 : 1; sfx.Net(e.k); Burst(at, 4 + Mathf.RoundToInt(8 * e.k), .8f + 1.5f * e.k, Color.white); break;
                case Ev.Cord: net.sign = mine ? -1 : 1; sfx.Net(.3f); break;
                case Ev.Tape: net.sign = mine ? -1 : 1; break;
                case Ev.Floor: sfx.Floor(e.big); Burst(at, e.big ? 10 : 4, 1, new Color(.85f, .95f, .9f, .7f)); if (e.big && !rm) shake = Mathf.Max(shake, 5); break;
                case Ev.Hawk: hud.Say((e.type == "in" ? "İÇERİ · " : "DIŞARI · ") + (int)e.k + " cm", Kit.W(e.x, e.y, .2f), e.type == "in" ? Kit.Hex("#d8ff5a") : Kit.Hex("#ff7a6b"), 1); break;
                case Ev.Point: sfx.Point(mine); hud.Say(e.text, Kit.W(Court.MID, mine ? Court.NET - 2 : Court.NET + 2, 1.5f), mine ? Kit.Hex("#d8ff5a") : Kit.Hex("#ff7a6b"), 1.4f); break;
                case Ev.Text:
                    {
                        Color c = e.type == "jump" ? Kit.Hex("#ff8a3c") : e.who == "me" && (e.type == "smash" || e.type == "kill" || e.onem >= 2) ? Kit.Hex("#d8ff5a") : e.type == "flow" ? Kit.Hex("#ffd23a") : e.type == "split" ? Kit.Hex("#7ad7ff") : e.who == "op" || e.who == null && e.onem >= 2 ? Kit.Hex("#ffd27a") : Color.white;
                        if (e.type == "ogr") hud.Ogr(e.text); else hud.Say(e.text, at, c, e.type == "jump" ? 1.3f : e.onem >= 3 ? 1.2f : 1);
                        break;
                    }
                case Ev.MatchEnd: EndMatch(e.who == "me"); break;
            }
        }
    }
}
