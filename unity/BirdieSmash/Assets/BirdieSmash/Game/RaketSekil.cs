// Raket kafa şekli (2026-10-30, yasin: "raket baş şeklini net görebilmeliyim"; Dolap › Raket › Kafa şekli). Yalnız görünüş:
// vuruş, isabet ve tatlı nokta raket_kafa dönüşümünden ve Gear'den gelir, ağdan değil (Dolap kuralı: oyunu etkilemez).
// FBX'te tek kafa var (insan.raket: elips boru, üstü %6 geniş, 22 ana + 21 çapraz tel, kısa T-bağlantı). "oval" = FBX'in
// kendi ağı, hiçbir şeye dokunulmaz. Diğer şekillerde çerçeve, ana ve çapraz teller yeniden üretilir:
//   · Raketin çerçevesi tel ağından okunur: ana teller kafanın boyuna uzanır (en uzun eksen = sap→kafa), çapraz teller enine,
//     en ince eksen yüzey normali. Kafanın yarı eksenleri telin kapladığı alandan (insan.raket'te teller elipsin %94'ü), boru
//     yarıçapı ve tel kalınlığı aynı oranlarla (.0048 / .00055 · ölçek, a = .115 · ölçek).
//   · Yön: sap tarafı grip ağının merkezinden; kalbin sivri ucu ve izometriğin dar boğazı sapa bakar.
//   · Teller çerçevenin iç kenarına kadar uzanır: her tel çizgisi boru eksen eğrisini kestiği yerden boru payı kadar içeride
//     biter (kalbin çentiğinde ana tel iki parçaya bölünür).
//   · Ağlar her parçanın kendi yerel uzayına çevrilir (parçalar raket_pivot altında rijit kardeşler, aradaki matris sabit),
//     böylece pozlar ve Vitrin kopyası bugünkü gibi çalışır; renk ve parlama aynı etiketle (cerceve/tel/tel2).
using System.Collections.Generic;
using UnityEngine;

namespace BirdieSmash.Game
{
    public static class RaketSekil
    {
        public static readonly string[] SIRA = { "oval", "izometrik", "kalp" };
        public static string Ad(string id) { return id == "izometrik" ? "İzometrik" : id == "kalp" ? "Kalp" : "Oval"; }
        static Texture2D[] ikon;
        // Örnek karedeki simge: şeklin kendi eğrisi (kalın çizgi) + sap; yalnız adla değil biçimle de okunur.
        public static Texture2D Ikon(string id)
        {
            if (ikon == null) ikon = new Texture2D[SIRA.Length];
            int i = Mathf.Max(0, System.Array.IndexOf(SIRA, id)); if (ikon[i] != null) return ikon[i];
            const int N = 96; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N]; var egri = Egri(SIRA[i], .30f, .25f, 160);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float u = (y + .5f) / N * 2 - 1 - .18f, v = (x + .5f) / N * 2 - 1, en = 9;   // u yukarı (kafa), sap aşağıda
                for (int k = 0; k < egri.Count; k++) { var p = egri[k]; float d = Mathf.Sqrt((p.x - u) * (p.x - u) + (p.y - v) * (p.y - v)); if (d < en) en = d; }
                float sap = Mathf.Abs(v) < .045f && u < -.30f && u > -.86f ? 0 : 9; float d0 = Mathf.Min(en - .045f, sap);
                byte a = (byte)(Mathf.Clamp01(.5f - d0 * N * .5f) * 255); px[y * N + x] = new Color32(255, 255, 255, a);
            }
            t.SetPixels32(px); t.Apply(); ikon[i] = t; return t;
        }

        // Boru eksen eğrisi (u: sap→kafa ekseni, kafa merkezi 0; v: enine). a, b: yarı eksenler. Sap tarafı u < 0.
        public static List<Vector2> Egri(string id, float a, float b, int n)
        {
            var l = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.PI * 2 * i / n, c = Mathf.Cos(t), s = Mathf.Sin(t);
                if (id == "kalp")
                {   // klasik kalp eğrisi: x = 16 sin³t, y = 13 cos t − 5 cos 2t − 2 cos 3t − cos 4t; sivri uç (y = −17) sapa
                    float hx = 16 * s * s * s, hy = 13 * c - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t);
                    l.Add(new Vector2((hy + 17) / 28.95f * 2 * a - a, hx / 16 * b * 1.1f));
                }
                else if (id == "izometrik")
                {   // süperelips: üstte köşeli omuzlar (n 3,4), sap tarafında elips (n 2)
                    float k = Mathf.SmoothStep(0, 1, (c + 1) / 2), nn = Mathf.Lerp(2, 3.4f, k), e = 2 / nn;
                    l.Add(new Vector2(a * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e), b * 1.04f * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), e)));
                }
                else l.Add(new Vector2(a * c, b * s * (1 + .06f * c)));
            }
            return l;
        }

        // Modelin raketine şekli uygula ("oval" ya da bilinmeyen: dokunulmaz). Parça bulunamazsa sessizce eski hâl kalır.
        public static void Uygula(Transform model, string id)
        {
            if (model == null || string.IsNullOrEmpty(id) || id == "oval" || System.Array.IndexOf(SIRA, id) < 0) return;
            MeshFilter cer = null, tel = null, tel2 = null; Renderer grip = null;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.gameObject.name;
                if (n.StartsWith("raket_kafa")) cer = mf; else if (n.StartsWith("raket_tel2")) tel2 = mf; else if (n.StartsWith("raket_tel")) tel = mf;
                else if (n.StartsWith("raket_sap")) grip = mf.GetComponent<Renderer>();
            }
            if (cer == null || tel == null || tel.sharedMesh == null || grip == null) return;
            // tel ağının yerel uzayında çerçeve: L (sap→kafa), S (enine), N (yüzey normali)
            var tb = tel.sharedMesh.bounds; Vector3 sz = tb.size; int[] ax = { 0, 1, 2 }; System.Array.Sort(ax, (p, q) => sz[q].CompareTo(sz[p]));
            Vector3 L = Vector3.zero, S = Vector3.zero, N = Vector3.zero; L[ax[0]] = 1; S[ax[1]] = 1; N[ax[2]] = 1;
            var c0 = tb.center; var g = tel.transform.InverseTransformPoint(grip.bounds.center); if (Vector3.Dot(g - c0, L) > 0) L = -L;
            // tel2 enine teller ana tellerle aynı bölgede: kafa merkezi iki ağın ortak merkezi (ana teller boyca tam açıklıkta değil)
            float a = sz[ax[0]] / 2 / .94f, b = sz[ax[1]] / 2 / .94f, olc = a / .115f, r = .0048f * olc, tr = .00055f * olc;
            if (tel2 != null && tel2.sharedMesh != null)
            {
                var t2 = tel2.sharedMesh.bounds; var m2 = tel.transform.worldToLocalMatrix * tel2.transform.localToWorldMatrix;
                var c2 = m2.MultiplyPoint3x4(t2.center); c0 = c0 + L * Vector3.Dot(c2 - c0, L);
            }
            var egri = Egri(id, a, b, 120);
            var cc = c0; System.Func<Vector2, float, Vector3> P = (q, n_) => cc + L * q.x + S * q.y + N * n_;

            // --- çerçeve borusu + T-bağlantı (şaftın ucu u = −.92a, insan.raket ile aynı) ---
            var v = new List<Vector3>(); var nr = new List<Vector3>(); var tri = new List<int>(); const int M = 10; int K = egri.Count;
            for (int i = 0; i < K; i++)
            {
                var p0 = egri[(i + K - 1) % K]; var p1 = egri[(i + 1) % K]; var tg = (p1 - p0).normalized; var yan = new Vector2(-tg.y, tg.x);
                for (int j = 0; j < M; j++)
                {
                    float f = Mathf.PI * 2 * j / M; var dn = (L * yan.x + S * yan.y) * Mathf.Cos(f) + N * Mathf.Sin(f);
                    v.Add(P(egri[i], 0) + dn * r); nr.Add(dn);
                }
            }
            for (int i = 0; i < K; i++) for (int j = 0; j < M; j++)
            {
                int a0 = i * M + j, a1 = i * M + (j + 1) % M, b0 = ((i + 1) % K) * M + j, b1 = ((i + 1) % K) * M + (j + 1) % M;
                tri.Add(a0); tri.Add(b0); tri.Add(a1); tri.Add(a1); tri.Add(b0); tri.Add(b1);
            }
            float uAlt = UMin(egri);   // eğrinin sap tarafı (kalpte sivri uç)
            Silindir(v, nr, tri, P(new Vector2(-.92f * a - .004f * olc, 0), 0), P(new Vector2(uAlt + r * .5f, 0), 0), .005f * olc, 10);
            Ata(cer, tel.transform, v, nr, tri);

            // --- teller: kutu kesitli ince çubuklar, çerçevenin iç kenarına kadar ---
            float vMax = 0, uMin = 99, uMax = -99; foreach (var q in egri) { vMax = Mathf.Max(vMax, Mathf.Abs(q.y)); uMin = Mathf.Min(uMin, q.x); uMax = Mathf.Max(uMax, q.x); }
            var tv = new List<Vector3>(); var tn = new List<Vector3>(); var tt = new List<int>();
            for (int i = 0; i < 22; i++)
            {
                float vv = (-1 + (i + .5f) / 11) * (vMax - r) * .97f;
                foreach (var ar in Kesisim(egri, vv, false, r * 1.1f)) Cubuk(tv, tn, tt, P(new Vector2(ar.x, vv), 0), P(new Vector2(ar.y, vv), 0), L, S, N, tr);
            }
            Ata(tel, tel.transform, tv, tn, tt);
            if (tel2 != null)
            {
                var cv = new List<Vector3>(); var cn = new List<Vector3>(); var ct = new List<int>();
                for (int j = 0; j < 21; j++)
                {
                    float uu = uMin + r + (j + .5f) / 21 * (uMax - uMin - 2 * r);
                    foreach (var ar in Kesisim(egri, uu, true, r * 1.1f)) Cubuk(cv, cn, ct, P(new Vector2(uu, ar.x), tr * .9f), P(new Vector2(uu, ar.y), tr * .9f), S, L, N, tr);
                }
                Ata(tel2, tel.transform, cv, cn, ct);
            }
        }

        static float UMin(List<Vector2> e) { float m = 99; foreach (var q in e) m = Mathf.Min(m, q.x); return m; }

        // Kapalı eğriyi bir doğruyla kes: enine (v = c) ise u aralıkları, boyuna (u = c, uSabit) ise v aralıkları; her uçtan pay içeri.
        static List<Vector2> Kesisim(List<Vector2> e, float c, bool uSabit, float pay)
        {
            var x = new List<float>(); int K = e.Count;
            for (int i = 0; i < K; i++)
            {
                var p = e[i]; var q = e[(i + 1) % K]; float pc = uSabit ? p.x : p.y, qc = uSabit ? q.x : q.y;
                if ((pc - c) * (qc - c) > 0 || Mathf.Approximately(pc, qc)) continue;
                float t = (c - pc) / (qc - pc); x.Add(uSabit ? Mathf.Lerp(p.y, q.y, t) : Mathf.Lerp(p.x, q.x, t));
            }
            x.Sort(); var l = new List<Vector2>();
            for (int i = 0; i + 1 < x.Count; i += 2) { float a = x[i] + pay, b = x[i + 1] - pay; if (b - a > pay) l.Add(new Vector2(a, b)); }
            return l;
        }

        static void Cubuk(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 p0, Vector3 p1, Vector3 boy, Vector3 en, Vector3 nor, float r)
        {
            var yuz = new[] { nor, en, -nor, -en };
            for (int k = 0; k < 4; k++)
            {
                var d = yuz[k]; var w = yuz[(k + 1) % 4]; int b = v.Count;
                v.Add(p0 + d * r - w * r); v.Add(p0 + d * r + w * r); v.Add(p1 + d * r - w * r); v.Add(p1 + d * r + w * r);
                n.Add(d); n.Add(d); n.Add(d); n.Add(d);
                t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b + 1); t.Add(b + 2); t.Add(b + 3);
                t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b + 1); t.Add(b + 3); t.Add(b + 2);   // iki yüzlü: ince tel her açıdan görünsün
            }
        }

        static void Silindir(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, float r, int m)
        {
            var ax = (b - a).normalized; var e1 = Vector3.Cross(ax, Mathf.Abs(ax.y) < .9f ? Vector3.up : Vector3.right).normalized; var e2 = Vector3.Cross(ax, e1);
            int b0 = v.Count;
            for (int j = 0; j < m; j++)
            {
                float f = Mathf.PI * 2 * j / m; var d = e1 * Mathf.Cos(f) + e2 * Mathf.Sin(f);
                v.Add(a + d * r); n.Add(d); v.Add(b + d * r); n.Add(d);
            }
            for (int j = 0; j < m; j++)
            {
                int i0 = b0 + j * 2, i1 = b0 + ((j + 1) % m) * 2;
                t.Add(i0); t.Add(i1); t.Add(i0 + 1); t.Add(i1); t.Add(i1 + 1); t.Add(i0 + 1);
            }
        }

        // Tel yerel uzayında kurulan ağı hedef parçanın yerel uzayına çevirip ata (paylaşılan FBX ağına dokunulmaz).
        static void Ata(MeshFilter hedef, Transform kaynak, List<Vector3> v, List<Vector3> n, List<int> t)
        {
            var m = hedef.transform.worldToLocalMatrix * kaynak.localToWorldMatrix;
            for (int i = 0; i < v.Count; i++) { v[i] = m.MultiplyPoint3x4(v[i]); n[i] = m.MultiplyVector(n[i]).normalized; }
            var mesh = new Mesh { name = hedef.gameObject.name + "_sekil" }; if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0); mesh.RecalculateBounds(); hedef.sharedMesh = mesh;
        }
    }
}
