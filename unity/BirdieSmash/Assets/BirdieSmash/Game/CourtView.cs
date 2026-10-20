// Kort: BWF ölçülü zemin ve çizgiler (13,40 × 6,10 m; tekler yan çizgisi 0,46 m içeride; kısa servis
// 1,98 m; çiftler uzun servis 0,76 m), direkler. Çizgiler 4 cm, zeminden 2 mm yukarıda (z-fighting yok).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public static class CourtView
    {
        public static void Build(Transform root)
        {
            var arena = Kit.Prim(PrimitiveType.Plane, "salon", root, new Vector3(0, -.01f, 0), new Vector3(4, 1, 5), Kit.Lit(Kit.Hex("#1c2a31"), .1f));
            arena.GetComponent<Renderer>().receiveShadows = true;
            var floor = Kit.Prim(PrimitiveType.Cube, "kort", root, new Vector3(0, -.005f, 0), new Vector3(Court.CW + 1.6f, .01f, Court.L + 2.4f), Kit.Lit(Kit.Hex("#2f8a63"), .35f));
            floor.GetComponent<Renderer>().receiveShadows = true;

            var v = new List<Vector3>(); var tr = new List<int>(); const float w = .04f, h = .002f;
            System.Action<float, float, float, float> seg = (x1, y1, x2, y2) =>
            {
                Vector3 a = Kit.W(x1, y1, h), b = Kit.W(x2, y2, h), d = (b - a).normalized, n = Vector3.Cross(Vector3.up, d) * (w / 2);
                int i = v.Count; v.Add(a - n); v.Add(a + n); v.Add(b + n); v.Add(b - n);
                tr.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            };
            float L = Court.L, CW = Court.CW, N = Court.NET;
            seg(0, 0, CW, 0); seg(0, L, CW, L); seg(0, 0, 0, L); seg(CW, 0, CW, L);            // çiftler dış çizgi
            seg(Court.SL, 0, Court.SL, L); seg(Court.SR, 0, Court.SR, L);                       // tekler yan çizgileri
            seg(0, N - Court.SSL, CW, N - Court.SSL); seg(0, N + Court.SSL, CW, N + Court.SSL); // kısa servis
            seg(0, .76f, CW, .76f); seg(0, L - .76f, CW, L - .76f);                             // çiftler uzun servis
            seg(Court.MID, 0, Court.MID, N - Court.SSL); seg(Court.MID, N + Court.SSL, Court.MID, L); // orta çizgi
            var m = new Mesh { name = "cizgiler" }; m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds();
            var lines = Kit.MeshObj("cizgiler", root, m, Kit.Lit(new Color(.95f, .97f, .95f), .2f));
            lines.GetComponent<Renderer>().receiveShadows = true;

            var post = Kit.Lit(Kit.Hex("#e9eef0"), .5f);
            foreach (float x in new[] { 0f, Court.CW })
                Kit.Prim(PrimitiveType.Cylinder, "direk", root, Kit.W(x, N, Court.NETP / 2), new Vector3(.04f, Court.NETP / 2, .04f), post)
                   .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
}
