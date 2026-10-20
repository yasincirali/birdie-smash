// Sahnenin tamamı koddan kurulur: kamera, ışık, kort, file, top, iki oyuncu, ses, HUD.
// Akış: girdi (kaydırma) → Match (saf C#) → olaylar (vuruş, file, sayı) → görünüm ve "his"
// (vuruş duraklaması Match'te, kamera sarsıntısı/zoom, tüy patlaması, ses, titreşim burada).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class GameRoot : MonoBehaviour
    {
        public Match match; public Camera cam;
        PlayerView me, op; ShuttleView shuttle; NetView net; Sfx sfx; Hud hud; ParticleSystem feathers; int splitYazi;
        float shake, punch; Vector3 camBase; Quaternion camRot; float camFov; int seed = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (FindFirstObjectByType<GameRoot>() == null) new GameObject("BirdieSmash").AddComponent<GameRoot>(); }

        void Awake()
        {
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c.gameObject.scene.IsValid() && c.transform.root != transform) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.transform.root != transform) l.gameObject.SetActive(false);
            var cg = new GameObject("kamera"); cg.transform.SetParent(transform, false); cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Kit.Hex("#10181d"); cam.nearClipPlane = .1f; cam.farClipPlane = 80;
            var lg = new GameObject("isik"); lg.transform.SetParent(transform, false); var sun = lg.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.intensity = 1.15f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .55f; lg.transform.rotation = Quaternion.Euler(58, -28, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Kit.Hex("#9fb4bf"); RenderSettings.ambientEquatorColor = Kit.Hex("#5d7480"); RenderSettings.ambientGroundColor = Kit.Hex("#2a3a33");
            QualitySettings.shadowDistance = 40;
            CourtView.Build(transform);
            shuttle = ShuttleView.Build(transform); sfx = Sfx.Build(transform);
            me = PlayerView.Build(transform, true, Kit.Hex("#7b5cff"), Kit.Hex("#ffd23a"), Kit.Hex("#23263a"));
            op = PlayerView.Build(transform, false, Kit.Hex("#ff5a52"), Color.white, Color.white);
            feathers = MakeFeathers();
            hud = gameObject.AddComponent<Hud>(); hud.root = this;
            NewMatch(true);
        }

        public void NewMatch(bool easy)
        {
            match = new Match(seed++ * 7919 + (int)(Time.realtimeSinceStartup * 1000), easy, 21);
            if (net != null) Destroy(net.gameObject); net = NetView.Build(transform, match.net);
            input.Reset();
        }

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
            var ep = new ParticleSystem.EmitParams { position = at, applyShapeToPosition = true };
            for (int i = 0; i < n; i++) { ep.velocity = Random.onUnitSphere * speed * Random.Range(.3f, 1f); ep.startColor = c; feathers.Emit(ep, 1); }
        }

        // Dikey ekranda kortun tamamı ve iki oyuncu görünsün: konum sabit, görüş açısı ekrana göre çözülür.
        int lastW, lastH;
        void FitCamera()
        {
            if (Screen.width == lastW && Screen.height == lastH) return; lastW = Screen.width; lastH = Screen.height;
            camBase = new Vector3(0, 10.5f, -Court.L / 2 - 6.2f); var target = new Vector3(0, .4f, .9f);
            camRot = Quaternion.LookRotation(target - camBase); cam.transform.SetPositionAndRotation(camBase, camRot);
            var pts = new List<Vector3>();
            foreach (float x in new[] { 0f, Court.CW }) foreach (float y in new[] { 0f, Court.L }) { pts.Add(Kit.W(x, y, 0)); }
            pts.Add(Kit.W(Court.MID, 0.4f, 2.6f)); pts.Add(Kit.W(Court.MID, Court.L + .8f, 0));
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height), tv = 0;
            foreach (var p in pts) { var l = Quaternion.Inverse(camRot) * (p - camBase); tv = Mathf.Max(tv, Mathf.Abs(l.y / l.z), Mathf.Abs(l.x / l.z) / aspect); }
            camFov = 2 * Mathf.Atan(tv * 1.06f) * Mathf.Rad2Deg; cam.fieldOfView = camFov;
        }

        // ---- kaydırma girdisi: piksel → 640 px yükseklikli referans (HTML eşikleri aynen geçerli) ----
        class Swipe { public bool on, hit, moved; public List<Vector3> tr = new List<Vector3>(); public Vector2 p0; public float t0; public void Reset() { on = hit = moved = false; tr.Clear(); } }
        readonly Swipe input = new Swipe();
        float Ref { get { return 640f / Mathf.Max(1, Screen.height); } }
        Vector2 RefPos(Vector2 screen) { return new Vector2(screen.x * Ref, (Screen.height - screen.y) * Ref); }   // y aşağı

        void SwipeVec(float now, out float dx, out float dy, out float speed)
        {
            var tr = input.tr; var last = tr[tr.Count - 1]; var a = tr[0];
            foreach (var p in tr) if (now - p.z <= 90) { a = p; break; }
            float dt = Mathf.Max(16, last.z - a.z); dx = last.x - a.x; dy = last.y - a.y; speed = Mathf.Sqrt(dx * dx + dy * dy) / dt;
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

        void ReadInput()
        {
            bool down, held, up; Vector2 pos;
            if (Input.touchCount > 0) { var t = Input.GetTouch(0); pos = t.position; down = t.phase == TouchPhase.Began; up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled; held = !down && !up; }
            else { pos = Input.mousePosition; down = Input.GetMouseButtonDown(0); up = Input.GetMouseButtonUp(0); held = Input.GetMouseButton(0) && !down; }
            float now = Time.unscaledTime * 1000; var q = RefPos(pos); var M = match;
            if (down)
            {
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
                    var sp = cam.WorldToScreenPoint(Kit.W(M.s.x, M.s.y, M.s.z)); var sr = RefPos(sp); float side;
                    float d = SegDist(sr, new Vector2(prev.x, prev.y), q, out side), far = Vector2.Distance(q, input.p0), R = HitR();
                    float dx, dy, speed; SwipeVec(now, out dx, out dy, out speed);
                    // Topun üstünden geçmek şart değil: pencere açıkken belirgin kaydırma vuruştur; topu kesmek mükemmeli verir.
                    if (d <= R || (far > 70 && speed > .45f))
                        if (M.SwipeShot(new SwipeInput { dx = dx, dy = dy, speed = speed, dist = d <= R ? d : R * 1.5f, R = R, side = side })) { input.hit = true; Vibrate(M.events); }
                }
            }
            else if (up && input.on)
            {
                input.on = false; input.tr.Add(new Vector3(q.x, q.y, now));
                if (M.s == null) { float dy0 = q.y - input.p0.y, dx0 = q.x - input.p0.x; if (-dy0 > 30) { float dx, dy, speed; SwipeVec(now, out dx, out dy, out speed); M.SwipeServe(dx0, dy0, speed); } return; }
                if (!input.hit && M.HitWindow())
                {
                    float far = Vector2.Distance(q, input.p0);
                    if (far < 24) M.SwipeShot(new SwipeInput { dx = 0, dy = -1, speed = 1, dist = HitR() * 1.5f, R = HitR(), tap = true });   // dokunma = güvenli clear
                    else hud.Say("Iska", Kit.W(M.me.x, M.me.y, 1.6f), Kit.Hex("#9dbab3"));
                }
                else if (M.easy && M.s != null && !input.hit && !M.HitWindow() && Vector2.Distance(q, input.p0) < 18 && now - input.t0 < 250)
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
            FitCamera(); ReadInput();
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime); match.Update(dt);
            foreach (var e in match.events) OnEvent(e); match.events.Clear();
            me.Show(match.me, match.clock, dt); op.Show(match.op, match.clock, dt); shuttle.Show(match.s);
            shake *= Mathf.Pow(.002f, dt); punch = 1 + (punch - 1) * Mathf.Pow(.0005f, dt); if (punch < 1) punch = 1;
            var j = shake > .01f ? (Vector3)(Random.insideUnitCircle * shake * .012f) : Vector3.zero;
            cam.transform.SetPositionAndRotation(camBase + camRot * j, camRot); cam.fieldOfView = camFov / punch;
        }

        void OnEvent(GameEvent e)
        {
            var at = Kit.W(e.x, e.y, e.z); bool mine = e.who == "me";
            switch (e.kind)
            {
                case Ev.Hit:
                    sfx.Hit(e.type, (int)e.k, mine); Burst(at, e.type == "smash" ? 14 : 6, e.type == "smash" ? 3.2f : 1.8f, Color.white);
                    if (e.type == "smash") { shake = Mathf.Max(shake, e.big ? 12 : 6); punch = Mathf.Max(punch, e.big ? 1.09f : 1.05f); }
                    if (e.quality == "perfect" && mine) Burst(at, 12, 3, Kit.Hex("#d8ff5a"));
                    if (e.k > 2 && (int)e.k % 10 == 0) hud.Say("Ralli " + (int)e.k, Kit.W(Court.MID, Court.NET, 2.5f), Kit.Hex("#d8ff5a"), 1.3f);
                    break;
                case Ev.Net: net.sign = mine ? -1 : 1; sfx.Net(e.k); Burst(at, 4 + Mathf.RoundToInt(8 * e.k), .8f + 1.5f * e.k, Color.white); break;
                case Ev.Tape: net.sign = mine ? -1 : 1; hud.Say(e.text, at, Kit.Hex("#ffd27a")); break;
                case Ev.Floor: sfx.Floor(e.big); Burst(at, e.big ? 10 : 4, 1, new Color(.85f, .95f, .9f, .7f)); if (e.big) shake = Mathf.Max(shake, 5); break;
                case Ev.Point: sfx.Point(mine); hud.Say(e.text, Kit.W(Court.MID, mine ? Court.NET - 2 : Court.NET + 2, 1.5f), mine ? Kit.Hex("#d8ff5a") : Kit.Hex("#ff7a6b"), 1.4f); break;
                case Ev.Text: hud.Say(e.text, at, e.who == "op" ? Kit.Hex("#ffd27a") : Color.white); break;
                case Ev.Split: if (++splitYazi <= 2) hud.Say("Split step", Kit.W(e.x, e.y, .4f), Kit.Hex("#7ad7ff"), .8f); break;   // yalnız ilk iki kez öğretici
            }
        }
    }
}
