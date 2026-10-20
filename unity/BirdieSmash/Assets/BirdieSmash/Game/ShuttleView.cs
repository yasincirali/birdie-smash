// Tüy top: mantar başlık önde, etek uçuş yönünün tersine; iz smaçta uzar. Görsel boy ×2.2
// (telefon ekranında okunsun; HTML'de de büyütülmüş çiziliyordu). Gölge yerden yükseldikçe solar.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class ShuttleView : MonoBehaviour
    {
        Transform body, shadow; TrailRenderer trail; const float S = 2.2f;

        public static ShuttleView Build(Transform root)
        {
            var g = new GameObject("tuytop"); g.transform.SetParent(root, false); var sv = g.AddComponent<ShuttleView>(); sv.Init(); return sv;
        }

        void Init()
        {
            body = new GameObject("govde").transform; body.SetParent(transform, false);
            Kit.MeshObj("etek", body, Skirt(), Kit.Lit(new Color(.98f, .98f, .96f), .1f), true);
            Kit.Prim(PrimitiveType.Sphere, "mantar", body, new Vector3(0, 0, .004f * S), Vector3.one * .027f * S, Kit.Lit(Kit.Hex("#f2b64c"), .4f))
               .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            shadow = Kit.Prim(PrimitiveType.Quad, "golge", transform, Vector3.zero, Vector3.one * .18f, Kit.Clear(new Color(0, 0, 0, .45f), Kit.RadialTex())).transform;
            shadow.rotation = Quaternion.Euler(90, 0, 0);
            trail = body.gameObject.AddComponent<TrailRenderer>(); trail.time = .14f; trail.minVertexDistance = .02f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, .05f), new Keyframe(1, 0)); trail.material = Kit.Clear(Color.white);
            trail.colorGradient = new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, alphaKeys = new[] { new GradientAlphaKey(.7f, 0), new GradientAlphaKey(0, 1) } };
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Açık etek: mantardan (r 1,3 cm) 7 cm geriye 3,3 cm'e açılan koni, iki yüzlü malzeme gerekmez (içi de çizilir).
        static Mesh Skirt()
        {
            const int n = 16; var v = new List<Vector3>(); var t = new List<int>(); float r0 = .013f * S, r1 = .033f * S, len = .07f * S;
            for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * r0, Mathf.Sin(a) * r0, 0)); v.Add(new Vector3(Mathf.Cos(a) * r1, Mathf.Sin(a) * r1, -len)); }
            for (int i = 0; i < n; i++) { int a = i * 2; t.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3, a, a + 2, a + 1, a + 2, a + 3, a + 1 }); }
            var m = new Mesh { name = "etek" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
        }

        public void Show(Shuttle s)
        {
            bool on = s != null; if (body.gameObject.activeSelf != on) { body.gameObject.SetActive(on); shadow.gameObject.SetActive(on); if (on) trail.Clear(); }
            if (!on) return;
            body.position = Kit.W(s.x, s.y, Mathf.Max(.03f, s.z));
            var vel = new Vector3(s.vx, s.vz, -s.vy);
            if (vel.sqrMagnitude > .01f) body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(vel.normalized), .5f);
            trail.time = s.type == "smash" ? .26f : .14f; trail.emitting = !s.dead && s.z > .05f;
            shadow.position = Kit.W(s.x, s.y, .004f); float k = Mathf.Clamp01(1 - s.z / 6f);
            shadow.localScale = Vector3.one * (.10f + .1f * k); shadow.GetComponent<Renderer>().material.color = new Color(0, 0, 0, .15f + .35f * k);
        }
    }
}
