// Kort: BWF ölçülü zemin ve çizgiler (13,40 × 6,10 m; tekler yan çizgisi 0,46 m içeride; kısa servis 1,98 m; çiftler
// uzun servis 0,76 m), direkler. Çizgiler 4 cm, zeminden 2 mm yukarıda (z-fighting yok).
// 2026-10-24 (HTML ile aynı kararlar):
//  - Saha temaları (yasin 2026-10-21): salon, turnuva, okul (ahşap), gece. Yalnız görünüm değişir, fizik aynı; kariyerde
//    saha basamağın mekânından gelir.
//  - Seyirci yalnız rakibin arkasında (yasin 2026-10-22, HTML 429f48a): dip çizginin gerisinde 0,8 m reklam panosu, üstünden
//    görünen 2 sıra oturan seyirci; yanlarda tribün yok (yayın görüntüsünde yanlar boş). Doluluk sahaya göre.
//  - Yayın ışığı (HTML dd0994d, seçenek C): kortun dışı kararır, kort üstü aydınlık; file çevresinde üç tavan lambası havuzu.
//  - Hakem sandalyeleri: baş hakem filenin bir ucunda yüksek sandalyede, servis hakemi karşı uçta alçak sandalyede,
//    yanında 1,15 m servis ölçeri. Hakem figürleri GameRoot'ta (oyuncularla aynı FBX).
//  - File iki yarı arasında: 3B sahnede derinlik sırası kendiliğinden doğru (HTML'de ressam sırası elle kuruluyordu).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Saha { public string ad, kort, tek, cevre, ust; public bool tahta; public float dol; }

    public static class CourtView
    {
        public static readonly Dictionary<string, Saha> SAHA = new Dictionary<string, Saha> {
            {"salon",  new Saha{ad="Salon",kort="#23725f",tek="#278066",cevre="#1b4f45",ust="#13262a",dol=.6f}},
            {"turnuva",new Saha{ad="Turnuva",kort="#1f7a55",tek="#22845c",cevre="#1c3d6b",ust="#101d33",dol=.92f}},
            {"okul",   new Saha{ad="Okul (ahşap)",kort="#a4703f",tek="#ab7744",cevre="#7c5230",ust="#21170f",tahta=true,dol=.3f}},
            {"park",   new Saha{ad="Gece",kort="#2f5674",tek="#335d7d",cevre="#22394c",ust="#0c1420",dol=.38f}}};
        public static readonly string[] SIRA = { "salon", "turnuva", "okul", "park" };
        public const string AKIS = "akis_cizgileri";

        public static GameObject Build(Transform root, string sahaId, out Color clear)
        {
            Saha T; if (!SAHA.TryGetValue(sahaId ?? "salon", out T)) T = SAHA["salon"];
            var k = new GameObject("kort_" + sahaId).transform; k.SetParent(root, false); clear = Kit.Hex(T.ust);
            var arena = Kit.Prim(PrimitiveType.Plane, "salon", k, new Vector3(0, -.012f, 0), new Vector3(6, 1, 6), Kit.Lit(Kit.Hex(T.ust), .05f));
            arena.GetComponent<Renderer>().receiveShadows = true;
            var cevre = Kit.Prim(PrimitiveType.Cube, "cevre", k, new Vector3(0, -.008f, 0), new Vector3(Court.CW + 3.2f, .01f, Court.L + 4.2f), Kit.Lit(Kit.Hex(T.cevre), .2f));
            cevre.GetComponent<Renderer>().receiveShadows = true;
            Material km = T.tahta ? Kit.LitTex(Color.white, Kit.WoodTex(Kit.Hex(T.kort)), .35f) : Kit.Lit(Kit.Hex(T.kort), .3f);
            var floor = Kit.Prim(PrimitiveType.Cube, "kort", k, new Vector3(0, -.004f, 0), new Vector3(Court.CW + .1f, .01f, Court.L + .1f), km);
            if (T.tahta) { var tm = floor.GetComponent<Renderer>().material; tm.mainTextureScale = new Vector2(1, 9); }
            floor.GetComponent<Renderer>().receiveShadows = true;
            if (!T.tahta)
            {
                var tek = Kit.Prim(PrimitiveType.Cube, "tekler", k, new Vector3(0, -.0035f, 0), new Vector3(Court.SR - Court.SL, .01f, Court.L), Kit.Lit(Kit.Hex(T.tek), .3f));
                tek.GetComponent<Renderer>().receiveShadows = true;
            }

            var v = new List<Vector3>(); var tr = new List<int>(); const float w = .04f, h = .002f;
            System.Action<float, float, float, float> seg = (x1, y1, x2, y2) =>
            {
                Vector3 a = Kit.W(x1, y1, h), b = Kit.W(x2, y2, h), d = (b - a).normalized, n = Vector3.Cross(Vector3.up, d) * (w / 2);
                int i = v.Count; v.Add(a - n); v.Add(a + n); v.Add(b + n); v.Add(b - n);
                tr.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            };
            float L = Court.L, CW = Court.CW, N = Court.NET;
            seg(0, 0, CW, 0); seg(0, L, CW, L); seg(0, 0, 0, L); seg(CW, 0, CW, L);
            seg(Court.SL, 0, Court.SL, L); seg(Court.SR, 0, Court.SR, L);
            seg(0, N - Court.SSL, CW, N - Court.SSL); seg(0, N + Court.SSL, CW, N + Court.SSL);
            seg(0, .76f, CW, .76f); seg(0, L - .76f, CW, L - .76f);
            seg(Court.MID, 0, Court.MID, N - Court.SSL); seg(Court.MID, N + Court.SSL, Court.MID, L);
            var m = new Mesh { name = "cizgiler" }; m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds();
            Kit.MeshObj("cizgiler", k, m, Kit.Lit(new Color(.95f, .97f, .95f), .2f)).GetComponent<Renderer>().receiveShadows = true;
            // Akış (HTML cizgiler(true)): üst üste tam ortada çizgiler altın olur ve nabız atar (alfa .75 + .25·sin(t/180 ms)),
            // 2,4 kat kalın. Ayrı ağ, kapalı başlar; GameRoot akışta açar ve rengini sürer.
            var av = new List<Vector3>(); for (int i = 0; i < v.Count; i += 4)
            {
                Vector3 c0 = (v[i] + v[i + 1]) / 2, c1 = (v[i + 2] + v[i + 3]) / 2, n2 = (v[i + 1] - v[i]) * 1.2f;
                av.Add(c0 - n2 + Vector3.up * .001f); av.Add(c0 + n2 + Vector3.up * .001f); av.Add(c1 + n2 + Vector3.up * .001f); av.Add(c1 - n2 + Vector3.up * .001f);
            }
            var am = new Mesh { name = "akis" }; am.SetVertices(av); am.SetTriangles(tr, 0); am.RecalculateBounds();
            var ag = Kit.MeshObj(AKIS, k, am, Kit.Clear(new Color(1, .886f, .47f, .85f))); ag.GetComponent<Renderer>().enabled = false;

            var post = Kit.Lit(Kit.Hex("#e9eef0"), .5f);
            foreach (float x in new[] { 0f, Court.CW })
                Kit.Prim(PrimitiveType.Cylinder, "direk", k, Kit.W(x, N, Court.NETP / 2), new Vector3(.04f, Court.NETP / 2, .04f), post)
                   .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Tribun(k, T); Sandalyeler(k); Isik(k);
            return k.gameObject;
        }

        // Rakibin arkası: pano (0,8 m, 2 m'lik dönüşümlü renk), arkasında 2 basamak, oturan seyirci (gövde + baş, bazen saç).
        static void Tribun(Transform k, Saha T)
        {
            var root = new GameObject("tribun").transform; root.SetParent(k, false);
            string[] giysi = { "#e85d4f", "#3f7fd8", "#f2c14e", "#f4f1ea", "#2c3e50", "#8e5bd1", "#3fb37f", "#ff8a3c", "#d94f8a", "#5fc4d9" }, ten = { "#f1c9a5", "#d9a57a", "#a8714a", "#6e4a2f", "#f5d6bb" };
            var mats = new Dictionary<string, Material>(); System.Func<string, Material> M = hx => { Material mm; if (!mats.TryGetValue(hx, out mm)) mats[hx] = mm = Kit.Lit(Kit.Hex(hx), .15f); return mm; };
            var rnd = new System.Random(7); float X0 = -1.6f, X1 = Court.CW + 1.6f, pano = -1.3f, DER = .55f, YUK = .42f;
            for (float x = X0; x < X1; x += 2) { float x2 = Mathf.Min(X1, x + 2); Kit.Prim(PrimitiveType.Cube, "pano", root, Kit.W((x + x2) / 2, pano, .4f), new Vector3(x2 - x, .8f, .06f), M(((int)((x - X0) / 2)) % 2 == 1 ? "#1d2b3a" : "#243a4f")); }
            for (int r = 0; r < 2; r++)
            {
                float ya = pano - .15f - r * DER, z = .55f + r * YUK;
                Kit.Prim(PrimitiveType.Cube, "basamak", root, Kit.W(Court.MID, ya - DER / 2, z / 2), new Vector3(X1 - X0, z, DER), M(r == 0 ? "#1f2e37" : "#16222a"));
                for (float x = X0 + .3f; x < X1 - .2f; x += .55f)
                {
                    if (rnd.NextDouble() > T.dol) continue; float xx = x + (float)(rnd.NextDouble() - .5) * .1f, yy = ya - DER * .5f;
                    Kit.Prim(PrimitiveType.Capsule, "seyirci", root, Kit.W(xx, yy, z + .27f), new Vector3(.36f, .27f, .28f), M(giysi[rnd.Next(giysi.Length)]));
                    Kit.Prim(PrimitiveType.Sphere, "bas", root, Kit.W(xx, yy, z + .66f), Vector3.one * .22f, M(ten[rnd.Next(ten.Length)]));
                    if (rnd.NextDouble() < .55) Kit.Prim(PrimitiveType.Sphere, "sac", root, Kit.W(xx, yy - .015f, z + .7f), new Vector3(.23f, .16f, .23f), M("#1e1610"));
                }
            }
        }

        // Baş hakem: filenin bir ucunda (x −0,5) iki ayaklı yüksek sandalye, 1,5 m oturak ve ayaklık; servis hakemi
        // karşı uçta (x CW+0,5) alçak sandalye ve 1,15 m servis ölçeri.
        public static readonly Vector3 HAKEM1 = Kit.W(-.6f, Court.NET, .95f), HAKEM2 = Kit.W(Court.CW + .55f, Court.NET + .35f, 0);
        static void Sandalyeler(Transform k)
        {
            var g = new GameObject("sandalyeler").transform; g.SetParent(k, false); var metal = Kit.Lit(Kit.Hex("#cfd6d4"), .6f); var koyu = Kit.Lit(Kit.Hex("#3a4a52"), .3f);
            float x0 = -.6f;
            foreach (float dy in new[] { -.25f, .25f }) Kit.Prim(PrimitiveType.Cylinder, "ayak", g, Kit.W(x0 - .12f, Court.NET + dy, .72f), new Vector3(.04f, .72f, .04f), metal);
            Kit.Prim(PrimitiveType.Cube, "ayaklik", g, Kit.W(x0, Court.NET, .93f), new Vector3(.5f, .04f, .6f), koyu);
            Kit.Prim(PrimitiveType.Cube, "basamak", g, Kit.W(x0 - .05f, Court.NET, .5f), new Vector3(.08f, .03f, .5f), metal);
            float x1 = Court.CW + .55f;
            foreach (float dx in new[] { -.15f, .15f }) Kit.Prim(PrimitiveType.Cylinder, "ayak", g, Kit.W(x1 + dx, Court.NET - .1f, .2f), new Vector3(.03f, .2f, .03f), metal);
            Kit.Prim(PrimitiveType.Cube, "oturak", g, Kit.W(x1, Court.NET - .1f, .41f), new Vector3(.4f, .05f, .4f), koyu);
            Kit.Prim(PrimitiveType.Cylinder, "olcer", g, Kit.W(Court.CW + .1f, Court.NET + .2f, .575f), new Vector3(.02f, .575f, .02f), Kit.Lit(new Color(.94f, .94f, .94f), .4f));
            Kit.Prim(PrimitiveType.Cube, "olcer_cizgi", g, Kit.W(Court.CW + .1f, Court.NET + .2f, 1.15f), new Vector3(.16f, .02f, .02f), Kit.Lit(new Color(.94f, .94f, .94f), .4f));
        }

        // Yayın ışığı: tavanda kortun boyunca üç lamba (HTML lamba havuzları NET×.45, NET, NET×1.55), sıcak beyaz.
        static void Isik(Transform k)
        {
            foreach (float y in new[] { Court.NET * .45f, Court.NET, Court.NET * 1.55f })
            {
                var g = new GameObject("lamba"); g.transform.SetParent(k, false); var l = g.AddComponent<Light>(); l.type = LightType.Spot;
                g.transform.position = Kit.W(Court.MID, y, 9); g.transform.rotation = Quaternion.Euler(90, 0, 0);
                l.spotAngle = 62; l.range = 16; l.intensity = 1.5f; l.color = new Color(1, .965f, .88f); l.shadows = LightShadows.None;
            }
        }
    }
}
