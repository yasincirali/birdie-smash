// Dünya uzayı efektleri (HTML draw(): waves, parts, M.marks, runMark, ogrHedefCiz). 3B karşılıkları:
//   iz (mark): top yere düştüğü yerde koyu leke (3 sn'de söner), ilk .5 sn genişleyen beyaz halka, smaçta 5 limon çatlak.
//   dalga (wave): vuruş noktasında kameraya dönük genişleyen halka (smaçta limon, büyük); hareketi azalt açıkken yok.
//   parçacık: tüy (uzun elips, döner), kıvılcım (yıldız, eklemeli), çizgi (streak: benim vuruşumda dışa fırlayan ışık çizgileri).
//   koşu çizgisi ve erişim halkası: yalnız Usta'da (koşu sende); erişim 1,15 m.
//   antrenman hedef alanı: rakip kortunda soluk limon bant + kesikli kenar ("Hedef" yazısı Hud'da).
// Hareketi azalt: parçacık üçte bir, dalga yok (HTML burst/wave ile aynı).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Fx : MonoBehaviour
    {
        class Mark { public Vector3 p; public float t; public bool big; public float[] cr; public Transform leke; public LineRenderer halka; public LineRenderer[] catlak; }
        class Wave { public Vector3 p; public float t, max; public Color c; public LineRenderer l; }
        readonly List<Mark> marks = new List<Mark>(); readonly List<Wave> waves = new List<Wave>();
        ParticleSystem tuy, kivilcim, cizgi; Material lekeM; Camera cam;
        LineRenderer kosu, erisim, hedefKenar; Transform kosuNokta, hedefBant; Material hedefM;

        public static Fx Build(Transform root, Camera cam)
        {
            var g = new GameObject("efekt"); g.transform.SetParent(root, false); var f = g.AddComponent<Fx>(); f.cam = cam; f.Init(); return f;
        }
        ParticleSystem PS(string ad, Material m, ParticleSystemRenderMode mode)
        {
            var g = new GameObject(ad); g.transform.SetParent(transform, false); var ps = g.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var mn = ps.main; mn.loop = false; mn.playOnAwake = false; mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.maxParticles = 400; mn.gravityModifier = .35f;
            var e = ps.emission; e.enabled = false; var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) } });
            var r = g.GetComponent<ParticleSystemRenderer>(); r.material = m; r.renderMode = mode; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (mode == ParticleSystemRenderMode.Stretch) { r.velocityScale = .05f; r.lengthScale = 1; }
            return ps;
        }
        void Init()
        {
            tuy = PS("tuy", Kit.Clear(Color.white, TuyTex()), ParticleSystemRenderMode.Billboard);
            var rot = tuy.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-5, 5);
            kivilcim = PS("kivilcim", Kit.Additive(Color.white, YildizTex()), ParticleSystemRenderMode.Billboard);
            cizgi = PS("cizgi", Kit.Additive(Color.white, Kit.RadialTex()), ParticleSystemRenderMode.Stretch); var cm = cizgi.main; cm.gravityModifier = 0;
            lekeM = Kit.Clear(new Color(10 / 255f, 30 / 255f, 26 / 255f, .45f), Kit.RadialTex());
            var mavi = new Color(79 / 255f, 184 / 255f, 1, .5f);
            kosu = Kit.Line("kosu", transform, Kit.Clear(mavi, Kit.DashTex(2, 5)), false, true); kosu.textureMode = LineTextureMode.Tile; kosu.widthMultiplier = .04f;
            kosuNokta = Kit.Prim(PrimitiveType.Quad, "kosu_nokta", transform, Vector3.zero, new Vector3(.3f, .3f, 1), Kit.Clear(new Color(mavi.r, mavi.g, mavi.b, .6f), Kit.RadialTex())).transform; kosuNokta.rotation = Quaternion.Euler(90, 0, 0);
            erisim = Kit.Line("erisim", transform, Kit.Clear(new Color(mavi.r, mavi.g, mavi.b, .16f)), true, true); erisim.widthMultiplier = .025f;
            hedefM = Kit.Clear(new Color(.847f, 1, .353f, .10f));
            hedefBant = Kit.Prim(PrimitiveType.Quad, "hedef_bant", transform, Vector3.zero, Vector3.one, hedefM).transform; hedefBant.rotation = Quaternion.Euler(90, 0, 0);
            hedefKenar = Kit.Line("hedef_kenar", transform, Kit.Clear(new Color(.847f, 1, .353f, .6f), Kit.DashTex(6, 5)), true, true); hedefKenar.textureMode = LineTextureMode.Tile; hedefKenar.widthMultiplier = .04f;
        }
        static Texture2D TuyTex()
        {   // uzun elips (HTML ellipse r·.35 × r)
            const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1; float r = Mathf.Sqrt(dx * dx / (.35f * .35f) + dy * dy); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - r) * 4))); }
            t.Apply(); return t;
        }
        static Texture2D YildizTex()
        {   // 8 köşeli yıldız (HTML spark)
            const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                { float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, a = Mathf.Atan2(dy, dx), r = Mathf.Sqrt(dx * dx + dy * dy); float lim = Mathf.Lerp(.3f, 1, Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2)), 3)); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((lim - r) * 6))); }
            t.Apply(); return t;
        }

        // HTML burst(x, y, col, n, spd, kind): hız px/sn → m/sn (1/28), ömür .4–.9 sn
        public void Burst(Vector3 at, Color c, int n, float spd, string kind)
        {
            if (Ayar.reduce) n = Mathf.CeilToInt(n / 3f);
            var ps = kind == "feather" ? tuy : kind == "spark" ? kivilcim : cizgi;
            var ep = new ParticleSystem.EmitParams { position = at, applyShapeToPosition = false };
            for (int i = 0; i < n; i++)
            {
                float s = spd / 28f * Random.Range(.3f, 1.2f); ep.velocity = Random.onUnitSphere * s + Vector3.up * .8f; ep.startColor = c; ep.startLifetime = Random.Range(.4f, .9f);
                ep.startSize = kind == "feather" ? Random.Range(.18f, .32f) : Random.Range(.06f, .16f); ep.rotation = Random.Range(0, 360f); ps.Emit(ep, 1);
            }
        }
        // benim vuruşumda dışa fırlayan ışık çizgileri (HTML streak: 600 px/sn, .18 sn)
        public void Streak(Vector3 at, Color c, int n)
        {
            if (Ayar.reduce) return; var ep = new ParticleSystem.EmitParams { position = at };
            for (int i = 0; i < n; i++) { var d = Random.onUnitSphere; d.y *= .5f; ep.velocity = d.normalized * 21; ep.startColor = c; ep.startLifetime = .18f; ep.startSize = .05f; cizgi.Emit(ep, 1); }
        }
        public void Dalga(Vector3 at, Color c, float maxPx)
        {
            if (Ayar.reduce) return; var l = Kit.Line("dalga", transform, Kit.Clear(Color.white), true); waves.Add(new Wave { p = at, max = maxPx / 28f, c = c, l = l });
        }
        public void Iz(float x, float y, bool big)
        {
            var m = new Mark { p = Kit.W(x, y, .007f), big = big, cr = new float[big ? 5 : 0] }; for (int i = 0; i < m.cr.Length; i++) m.cr[i] = Random.value * 6.28f;
            m.leke = Kit.Prim(PrimitiveType.Quad, "iz", transform, m.p, Vector3.one, lekeM).transform; m.leke.rotation = Quaternion.Euler(90, 0, 0);
            m.leke.localScale = new Vector3((big ? 9 : 5) / 28f * 2.6f, (big ? 9 : 5) / 28f * 2.6f, 1);
            m.halka = Kit.Line("iz_halka", transform, Kit.Clear(Color.white), true, true); m.halka.widthMultiplier = .04f;
            m.catlak = new LineRenderer[m.cr.Length];
            for (int i = 0; i < m.cr.Length; i++) { var l = Kit.Line("catlak", transform, Kit.Clear(new Color(.847f, 1, .353f, .6f)), false, true); l.widthMultiplier = .03f; l.positionCount = 2; l.SetPosition(0, m.p); l.SetPosition(1, m.p + new Vector3(Mathf.Cos(m.cr[i]), 0, Mathf.Sin(m.cr[i])) * 18 / 28f); m.catlak[i] = l; }
            marks.Add(m);
        }
        public void Temizle()
        {
            foreach (var m in marks) { Destroy(m.leke.gameObject); Destroy(m.halka.gameObject); foreach (var l in m.catlak) Destroy(l.gameObject); }
            marks.Clear(); foreach (var w in waves) Destroy(w.l.gameObject); waves.Clear();
        }

        public void Tick(Match M, float dt, bool oyun)
        {
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                var m = marks[i]; m.t += dt; float a = Mathf.Max(0, 1 - m.t / 3);
                var mm = m.leke.GetComponent<Renderer>(); mm.material.color = new Color(10 / 255f, 30 / 255f, 26 / 255f, .45f * a * 1.4f);
                m.halka.enabled = m.t < .5f; if (m.halka.enabled) { Kit.Ellipse(m.halka, m.p, (8 + m.t * 80) / 28f, (8 + m.t * 80) / 28f); m.halka.startColor = m.halka.endColor = new Color(1, 1, 1, (.5f - m.t) * 1.6f); }
                foreach (var l in m.catlak) l.startColor = l.endColor = new Color(.847f, 1, .353f, .6f * a);
                if (m.t >= 3) { Destroy(m.leke.gameObject); Destroy(m.halka.gameObject); foreach (var l in m.catlak) Destroy(l.gameObject); marks.RemoveAt(i); }
            }
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var w = waves[i]; w.t += dt * 2.2f;   // HTML dalga ~.45 sn
                if (w.t >= 1) { Destroy(w.l.gameObject); waves.RemoveAt(i); continue; }
                float e = 1 - Mathf.Pow(1 - w.t, 3); var c = w.c; c.a = (1 - w.t) * .8f; w.l.startColor = w.l.endColor = c; w.l.widthMultiplier = (5 * (1 - w.t) + 1) / 28f;
                // kameraya dönük çember
                int n = 40; w.l.positionCount = n; var r = cam.transform.right * w.max * e; var u = cam.transform.up * w.max * e * .5f;
                for (int k = 0; k < n; k++) { float an = k * Mathf.PI * 2 / n; w.l.SetPosition(k, w.p + r * Mathf.Cos(an) + u * Mathf.Sin(an)); }
            }
            // Usta: koşu çizgisi ve erişim halkası
            bool usta = oyun && M != null && !M.easy && M.s != null;
            kosu.enabled = false; kosuNokta.gameObject.SetActive(false); erisim.enabled = usta;
            if (usta)
            {
                Kit.Ellipse(erisim, Kit.W(M.me.x, M.me.y, .009f), 1.15f, 1.15f);
                if (M.me.HasTarget) { var a0 = Kit.W(M.me.x, M.me.y, .01f); var b0 = Kit.W(M.me.tx, M.me.ty, .01f); if ((b0 - a0).magnitude > .4f) { kosu.enabled = true; kosu.positionCount = 2; kosu.SetPosition(0, a0); kosu.SetPosition(1, b0); kosuNokta.gameObject.SetActive(true); kosuNokta.position = b0; } }
            }
            // antrenman hedef alanı
            float[] h = null; var oa = oyun && M != null ? M.OgrA() : null;
            if (oa != null && oa.servis == null && oa.bilgi == null) { string k = M.OgrIstenen(oa); if (k != null) Match.OGR_HEDEF.TryGetValue(k, out h); }
            hedefBant.gameObject.SetActive(h != null); hedefKenar.enabled = h != null;
            if (h != null)
            {
                float y0 = h[0], y1 = h[1]; hedefBant.position = Kit.W(Court.MID, (y0 + y1) / 2, .006f); hedefBant.localScale = new Vector3(Court.SR - Court.SL, y1 - y0, 1);
                hedefKenar.positionCount = 4; hedefKenar.SetPosition(0, Kit.W(Court.SL, y0, .008f)); hedefKenar.SetPosition(1, Kit.W(Court.SR, y0, .008f)); hedefKenar.SetPosition(2, Kit.W(Court.SR, y1, .008f)); hedefKenar.SetPosition(3, Kit.W(Court.SL, y1, .008f));
            }
        }
        public Vector3? HedefMerkez(Match M)
        {
            var oa = M != null ? M.OgrA() : null; if (oa == null || oa.servis != null || oa.bilgi != null) return null; string k = M.OgrIstenen(oa); float[] h;
            if (k == null || !Match.OGR_HEDEF.TryGetValue(k, out h)) return null; return Kit.W(Court.MID, (h[0] + h[1]) / 2, .01f);
        }
    }
}
