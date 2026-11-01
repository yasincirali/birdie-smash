// Tüy top, 3B (yasin 2026-10-24: "Demoda tüy top detayı gözükmüyor"). HTML shuttleModel()'in birebir karşılığı:
//   Ölçü: BWF Law 2 (mantar Ø25–28 mm, 16 tüy 62–70 mm, etek ağzı Ø58–68 mm, iki iplik bağı) × G=3,2 okunurluk büyütmesi
//   (HTML: 4,4 fazla büyüktü; 3,2'de oyuncuya oranı 1/14, uzak tarafta tüyler hâlâ seçiliyor). L=.066G, ağız Rt=.032G,
//   tüy dibi Rb=.012G, mantar Rc=.0135G.
//   Tüy: dipte dar (±.42 dilim), uca doğru genişler (±.66 dilim: komşusunun üstüne biner), ucu yuvarlak (karesel eğri),
//   ortasında sap, dipten 1/3 ve 1/2'de iplik bağı. Dış yüz beyaz, iç yüz gölgeli (HTML arka tüyler 178–200 tonu).
//   Mantar: krem (#f1dcb4, ışık sol üstten), kenarında mavi şerit (rgba(40,60,120,.55)).
//   Yön: mantar önde, hız vektörüne ~20 ms'de döner (k = 1 − e^(−dt/0,02)); yeni vuruşta (hız tersine döndü) ±14 rad/s
//   sönümlü salınım (wv += (−wob·260 − wv·18)·dt, eğim wob·0,35 rad ≈ 80 ms); etek kendi ekseninde min(40, v·1,6) rad/s döner.
//   Hızlı uçuşta (v > 18 m/s) en çok 3 soluk kopya bir karelik yolun gerisinde (hareket bulanıklığı, alfa .22/i).
//   Ayrıntı eşiği: etek ağzı ekranda 3,2 px'ten küçükse kenar ve iplik çizgileri kapanır (HTML `ayrinti`).
//   UNITY_EK (HTML'de yok, bayrak SLICE_TAKLA): slice vuruşunda salınım daha güçlü (±22) ve daha uzun sönümlü (·9):
//   HTML metni "top takla atar" der ama çizimi düz vuruşla aynıdır. Kapatınca HTML davranışı birebir.
// Çevre ipuçları (HTML shuttle()): yer gölgesi (alfa .4/(1+z·.35), boy ×(1+z·.3)), z > .25'te kesikli yükseklik çizgisi,
// rakip topu son 1,1 sn'de gölgenin derinliğinde kort boyu yer çizgisi, iniş halkası (kesikli; son .9 sn büzülen halka,
// topa yetişiyorsan limon; son .35 sn dolu), parmak ekrandayken hedef önizleme. Hale ve etiket ekran uzayında (Hud).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class ShuttleView : MonoBehaviour
    {
        public const float G = 3.2f, L = .066f * G, Rt = .032f * G, Rb = .012f * G, Rc = .0135f * G;
        public static bool SLICE_TAKLA = true;
        Transform body; GameObject lines; readonly GameObject[] ghosts = new GameObject[3]; Material[] ghostM = new Material[3];
        Transform shadow; Material shadowM; LineRenderer trail, hLine, gLine, ring, ringS, prevRing, prevX1, prevX2; Transform ringFill; Material ringFillM, ringSM, prevM; LineRenderer erIc, erDis, ayakL, ayakR; Material ayakM, ayakDolguM; Transform ayakDolguL, ayakDolguR;
        readonly List<Vector3> trailPts = new List<Vector3>();
        // İzin son uygulanan biçimi (renk, kalınlık, saydamlık) ve yeniden kullanılan anahtar dizileri.
        readonly Gradient izGrad = new Gradient(); readonly GradientColorKey[] izRenk = new GradientColorKey[2];
        readonly GradientAlphaKey[] izSaydam = new GradientAlphaKey[2]; Color izC = new Color(-1, 0, 0, 0); float izW = -1, izA = -1;
        Shuttle last; Vector3 d = Vector3.forward; float wob, wv, phi, lvx, lvy, lvz;
        public Camera cam; public bool reduce;
        public Vector3 BodyPos { get { return body.position; } }

        public static ShuttleView Build(Transform root)
        {
            var g = new GameObject("tuytop"); g.transform.SetParent(root, false); var sv = g.AddComponent<ShuttleView>(); sv.Init(); return sv;
        }

        void Init()
        {
            body = new GameObject("govde").transform; body.SetParent(transform, false);
            var tuy = Kit.Lit(new Color(.97f, .97f, .955f), .12f); var ic = Kit.Lit(new Color(.74f, .75f, .76f), .05f); var sap = Kit.Lit(new Color(.98f, .97f, .93f), .2f);
            var mantar = Kit.Lit(Kit.Hex("#f1dcb4"), .35f); var serit = Kit.Lit(new Color(.2f, .27f, .5f), .3f);
            var go = Kit.MeshObj("tuyler", body, Feathers(), tuy, true); go.GetComponent<MeshRenderer>().sharedMaterials = new[] { tuy, ic, sap };
            var co = Kit.MeshObj("mantar", body, Cork(), mantar, true); co.GetComponent<MeshRenderer>().sharedMaterials = new[] { mantar, serit };
            lines = Kit.MeshObj("cizgiler", body, Lines(), Kit.Unlit(new Color(.33f, .37f, .40f)));
            var combined = Combined();
            for (int i = 0; i < 3; i++)
            {
                ghostM[i] = Kit.Clear(new Color(1, 1, 1, .22f / (i + 1)));
                ghosts[i] = Kit.MeshObj("hayalet" + (i + 1), transform, combined, ghostM[i]); ghosts[i].SetActive(false);
            }
            shadowM = Kit.Clear(new Color(0, 0, 0, .4f), Kit.RadialTex());
            shadow = Kit.Prim(PrimitiveType.Quad, "golge", transform, Vector3.zero, Vector3.one, shadowM).transform; shadow.rotation = Quaternion.Euler(90, 0, 0);
            trail = Kit.Line("iz", transform, Kit.Additive(Color.white)); trail.numCapVertices = 2;
            var dash = Kit.DashTex(2, 4);
            hLine = Kit.Line("yukseklik", transform, Kit.Clear(new Color(1, 1, 1, .22f), dash)); hLine.textureMode = LineTextureMode.Tile; hLine.widthMultiplier = .02f;
            gLine = Kit.Line("yer_cizgisi", transform, Kit.Clear(new Color(1, 1, 1, .1f)), false, true); gLine.widthMultiplier = .03f;
            var dash34 = Kit.DashTex(3, 4);
            ring = Kit.Line("inis", transform, Kit.Clear(new Color(1, 1, 1, .45f), dash34), true, true); ring.textureMode = LineTextureMode.Tile; ring.widthMultiplier = .05f;
            ringSM = Kit.Clear(new Color(1, 1, 1, .5f)); ringS = Kit.Line("inis_halka", transform, ringSM, true, true); ringS.widthMultiplier = .05f;
            ringFillM = Kit.Clear(new Color(1, 1, 1, .18f)); ringFill = Kit.MeshObj("inis_dolgu", transform, Disc(), ringFillM).transform;
            // Usta erişim halkaları (2026-10-31 U1): oyuncunun ayağında iç halka (düz, limon) = tam vuruş, kesik dış halka (turuncu)
            // = uzanma (Match.USTA_UZAN). Rakip topu bu yana gelirken görünür; anlam biçimde de var (düz/kesik), yalnız renkte değil.
            erIc = Kit.Line("erisim_ic", transform, Kit.Clear(new Color(.77f, .93f, .25f, .35f)), true, true); erIc.widthMultiplier = .035f;
            erDis = Kit.Line("erisim_dis", transform, Kit.Clear(new Color(1, .6f, .24f, .5f), dash34), true, true); erDis.textureMode = LineTextureMode.Tile; erDis.widthMultiplier = .035f;
            // Usta duruş işareti (2026-11-01 Ar-Ge P3): topu karşılamak için durulacak yer yerde iki ayak izi. Varılmadıkça yalnız
            // çerçeve (beyaz), oraya gelince içi dolar (limon): "yerindesin" biçimle de okunur, yalnız renkle değil (HIG renk körlüğü).
            ayakM = Kit.Clear(new Color(1, 1, 1, .55f)); ayakL = Kit.Line("durus_sol", transform, ayakM, true, true); ayakR = Kit.Line("durus_sag", transform, ayakM, true, true);
            ayakL.widthMultiplier = ayakR.widthMultiplier = .025f;
            ayakDolguM = Kit.Clear(new Color(.85f, 1, .35f, .45f));
            ayakDolguL = Kit.MeshObj("durus_dolgu_sol", transform, Disc(), ayakDolguM).transform; ayakDolguR = Kit.MeshObj("durus_dolgu_sag", transform, Disc(), ayakDolguM).transform;
            prevM = Kit.Clear(new Color(.847f, 1, .353f, .8f), dash);
            prevRing = Kit.Line("hedef", transform, prevM, true, true); prevRing.textureMode = LineTextureMode.Tile; prevRing.widthMultiplier = .06f;
            var pm = Kit.Clear(new Color(.847f, 1, .353f, .8f));
            prevX1 = Kit.Line("hedef_x1", transform, pm, false, true); prevX2 = Kit.Line("hedef_x2", transform, pm, false, true); prevX1.widthMultiplier = prevX2.widthMultiplier = .05f;
            Gizle();
        }

        // ---- ağlar: yerel +Z mantar yönü (uçuş yönü), etek −Z'ye açılır ----
        static Vector3 P(float a, float r, float al) { return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -al); }
        static Vector2 Bez(Vector2 p0, Vector2 c, Vector2 p1, float t) { float u = 1 - t; return u * u * p0 + 2 * u * t * c + t * t * p1; }
        // Tek tüy (dilim açısı a etrafında): dip iki köşe, uçta karesel yuvarlak kenar. Yüzey koni üstünde, kalınlığı yok;
        // tek ve çift tüyler 0,4 mm farklı yarıçapta ki üst üste binen kenarlar titreşmesin (z-fighting).
        public static Mesh Feathers()
        {
            const int N = 16, TIP = 6; float dw = Mathf.PI * 2 / N, hb = dw * .42f, ht = dw * .66f, al0 = .004f * G;
            var v = new List<Vector3>(); var outT = new List<int>(); var inT = new List<int>(); var shT = new List<int>();
            for (int i = 0; i < N; i++)
            {
                float a = i * dw, kat = 1 + (i % 2) * .012f; int b0 = v.Count;
                // açı/uzunluk düzleminde tüy taslağı: (açı farkı, yarıçap, boy)
                var pts = new List<Vector3> { new Vector3(-hb, Rb, al0), new Vector3(hb, Rb, al0) };
                Vector2 t1 = new Vector2(ht, L * .9f), t2 = new Vector2(-ht, L * .9f), tc = new Vector2(0, L * 1.1f); Vector2 mid = (t1 + t2) / 2, cc = tc + (tc - mid) * .6f;
                // uç: ağız yarıçapından en çok %4 dışa (HTML tc = Rt·1,04)
                for (int j = 0; j <= TIP; j++) { var bz = Bez(t1, cc, t2, j / (float)TIP); pts.Add(new Vector3(bz.x, Rt * (1 + Mathf.Max(0, bz.y - L * .9f) / (L * .2f) * .04f), bz.y)); }
                foreach (var q in pts) v.Add(P(a + q.x, q.y * kat, q.z));
                // yelpaze: dip ortası merkez (b0..), dış yüz ve iç yüz ayrı alt ağ
                int nP = pts.Count;
                for (int k = 1; k < nP - 1; k++) { outT.AddRange(new[] { b0, b0 + k, b0 + k + 1 }); }
                // iç yüz: aynı köşeler, ters sıra (normaller içe)
                int c0 = v.Count; foreach (var q in pts) v.Add(P(a + q.x, q.y * kat * .995f, q.z));
                for (int k = 1; k < nP - 1; k++) inT.AddRange(new[] { c0, c0 + k + 1, c0 + k });
                // sap: tüyün ortasından ince şerit, dış yüzün 0,6 mm dışında
                float w = .0016f * G / (Rb + Rt) * 2; int s0 = v.Count;
                v.Add(P(a - w * .6f, Rb * kat * 1.01f, al0)); v.Add(P(a + w * .6f, Rb * kat * 1.01f, al0)); v.Add(P(a - w * .25f, Rt * .98f * kat * 1.01f, L * .98f)); v.Add(P(a + w * .25f, Rt * .98f * kat * 1.01f, L * .98f));
                shT.AddRange(new[] { s0, s0 + 2, s0 + 1, s0 + 1, s0 + 2, s0 + 3 });
            }
            var m = new Mesh { name = "tuyler", subMeshCount = 3 }; m.SetVertices(v);
            m.SetTriangles(Wind(v, outT, true), 0); m.SetTriangles(Wind(v, inT, false), 1); m.SetTriangles(Wind(v, shT, true), 2);
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        // Üçgen sırası: dış yüz normali eksenden dışa baksın (Unity saat yönü = ön yüz). Hesapla düzeltilir ki yukarıdaki
        // sıralama hatası ışığı ters çevirmesin.
        static List<int> Wind(List<Vector3> v, List<int> t, bool disa)
        {
            var o = new List<int>(t.Count);
            for (int i = 0; i + 2 < t.Count; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]], n = Vector3.Cross(b - a, c - a), m = (a + b + c) / 3; m.z = 0;
                bool out_ = Vector3.Dot(n, m) > 0; if (out_ == disa) { o.Add(t[i]); o.Add(t[i + 1]); o.Add(t[i + 2]); } else { o.Add(t[i]); o.Add(t[i + 2]); o.Add(t[i + 1]); }
            }
            return o;
        }
        // Mantar: önde yarım küre (Rc), arkada kısa silindir; ön kenarda mavi şerit (ayrı alt ağ).
        public static Mesh Cork()
        {
            const int S = 24, R = 7; var v = new List<Vector3>(); var t0 = new List<int>(); var t1 = new List<int>(); float len = Rc * .75f;
            for (int j = 0; j <= R; j++) { float ph = j / (float)R * Mathf.PI / 2; for (int i = 0; i <= S; i++) { float a = i * Mathf.PI * 2 / S; v.Add(new Vector3(Mathf.Cos(a) * Mathf.Cos(ph) * Rc, Mathf.Sin(a) * Mathf.Cos(ph) * Rc, Mathf.Sin(ph) * Rc)); } }
            for (int j = 0; j < R; j++) for (int i = 0; i < S; i++) { int a = j * (S + 1) + i, b = a + S + 1; t0.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 }); }
            // silindir: dört halka; ikinci bant ince mavi şerit (ekvatorun hemen gerisinde)
            float[] zs = { 0, -.06f * Rc, -.16f * Rc, -len }; int c0 = v.Count;
            foreach (var z in zs) for (int i = 0; i <= S; i++) { float a = i * Mathf.PI * 2 / S; v.Add(new Vector3(Mathf.Cos(a) * Rc, Mathf.Sin(a) * Rc, z)); }
            for (int r = 0; r < 3; r++) for (int i = 0; i < S; i++) { int a = c0 + r * (S + 1) + i, b = a + S + 1; (r == 1 ? t1 : t0).AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            int bc = v.Count; v.Add(new Vector3(0, 0, -len)); int br = c0 + 3 * (S + 1);
            for (int i = 0; i < S; i++) t0.AddRange(new[] { bc, br + i + 1, br + i });
            var m = new Mesh { name = "mantar", subMeshCount = 2 }; m.SetVertices(v);
            m.SetTriangles(WindC(v, t0), 0); m.SetTriangles(WindC(v, t1), 1); m.RecalculateNormals(); return m;
        }
        static List<int> WindC(List<Vector3> v, List<int> t)
        {   // mantar kapalı cisim: normal merkezden (0,0,−len/2) dışa baksın
            var o = new List<int>(); var cen = new Vector3(0, 0, -Rc * .3f);
            for (int i = 0; i + 2 < t.Count; i += 3) { Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]]; bool ok = Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3 - cen) > 0; o.Add(t[i]); o.Add(ok ? t[i + 1] : t[i + 2]); o.Add(ok ? t[i + 2] : t[i + 1]); }
            return o;
        }
        // Kenar ve iplik çizgileri (çizgi topolojisi, 1 px): her tüyün dış hattı + 1/3 ve 1/2'de iki iplik halkası.
        public static Mesh Lines()
        {
            var v = new List<Vector3>(); var idx = new List<int>(); const int N = 16; float dw = Mathf.PI * 2 / N, hb = dw * .42f, ht = dw * .66f, al0 = .004f * G;
            for (int i = 0; i < N; i++)
            {
                float a = i * dw, k = 1.004f + (i % 2) * .012f; Vector2 t1 = new Vector2(ht, L * .9f), t2 = new Vector2(-ht, L * .9f), tc = new Vector2(0, L * 1.1f), cc = tc + (tc - (t1 + t2) / 2) * .6f;
                var hat = new List<Vector3> { P(a + hb, Rb * k, al0), P(a + ht, Rt * k, L * .9f) };
                for (int s = 1; s <= 6; s++) { var q = Bez(t1, cc, t2, s / 6f); hat.Add(P(a + q.x, Rt * k * (1 + (q.y - L * .9f) / (L * .2f) * .04f), q.y)); }
                hat.Add(P(a - hb, Rb * k, al0));
                for (int s = 0; s + 1 < hat.Count; s++) { idx.Add(v.Count); v.Add(hat[s]); idx.Add(v.Count); v.Add(hat[s + 1]); }
            }
            foreach (float u in new[] { .34f, .5f })
                for (int i = 0; i < 48; i++) { float a0 = i * Mathf.PI * 2 / 48, a1 = (i + 1) * Mathf.PI * 2 / 48, r = (Rb + (Rt - Rb) * u) * 1.03f; idx.Add(v.Count); v.Add(P(a0, r, L * u)); idx.Add(v.Count); v.Add(P(a1, r, L * u)); }
            var m = new Mesh { name = "cizgiler" }; m.SetVertices(v); m.SetIndices(idx.ToArray(), MeshTopology.Lines, 0); return m;
        }
        static Mesh Combined()
        {
            var a = Feathers(); var b = Cork(); var ci = new[] { new CombineInstance { mesh = a, subMeshIndex = 0, transform = Matrix4x4.identity }, new CombineInstance { mesh = b, subMeshIndex = 0, transform = Matrix4x4.identity } };
            var m = new Mesh { name = "hayalet" }; m.CombineMeshes(ci, true, true); return m;
        }
        static Mesh Disc()
        {
            const int n = 40; var v = new List<Vector3> { Vector3.zero }; var t = new List<int>();
            for (int i = 0; i <= n; i++) { float a = -i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); }
            for (int i = 1; i <= n; i++) t.AddRange(new[] { 0, i, i + 1 });
            var m = new Mesh { name = "disk" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
        }

        void Gizle()
        {
            body.gameObject.SetActive(false); shadow.gameObject.SetActive(false); foreach (var g in ghosts) g.SetActive(false);
            trail.enabled = hLine.enabled = gLine.enabled = ring.enabled = ringS.enabled = prevRing.enabled = prevX1.enabled = prevX2.enabled = false; if (erIc != null) erIc.enabled = erDis.enabled = false; ringFill.gameObject.SetActive(false); Durus(false, 0, 0, false);
        }
        void Durus(bool on, float x, float y, bool yerinde)
        {
            if (ayakL == null) return;
            ayakL.enabled = ayakR.enabled = on; ayakDolguL.gameObject.SetActive(on && yerinde); ayakDolguR.gameObject.SetActive(on && yerinde);
            if (!on) return;
            const float ara = .12f, rx = .055f, rz = .12f;
            var l = Kit.W(x - ara, y, .009f); var r = Kit.W(x + ara, y, .009f);
            Kit.Ellipse(ayakL, l, rx, rz, 24); Kit.Ellipse(ayakR, r, rx, rz, 24);
            ayakM.color = yerinde ? new Color(.85f, 1, .35f, .9f) : new Color(1, 1, 1, .55f);
            if (yerinde) { ayakDolguL.position = l; ayakDolguR.position = r; ayakDolguL.localScale = ayakDolguR.localScale = new Vector3(rx, 1, rz); }
        }

        // M: maç (iniş halkası, akış), preview: parmak ekrandayken tahmini düşüş (null = yok)
        public void Show(Match M, Shuttle s, float dt, bool previewOn, Vector2 previewAt)
        {
            if (s == null) { Gizle(); last = null; trailPts.Clear(); return; }
            float sp = Mathf.Max(1e-6f, s.Speed);
            // --- yön: etek hızın tersine (mantar önde); yeni vuruşta salınım ---
            bool flip = !s.dead && (last != s || lvx * s.vx + lvy * s.vy + lvz * s.vz < 0);   // yerde salınım yok
            bool takla = SLICE_TAKLA && s.slice;
            if (flip && last != null) wv = (Random.value < .5f ? -1 : 1) * (takla ? 22 : 14);
            if (last != s) trailPts.Clear();
            last = s; lvx = s.vx; lvy = s.vy; lvz = s.vz;
            var tgt = new Vector3(s.vx, s.vz, -s.vy) / sp;   // mantar yönü (dünya)
            float k = reduce ? 1 : 1 - Mathf.Exp(-dt / .02f); d = (d + (tgt - d) * k).normalized; if (d.sqrMagnitude < .5f) d = tgt;
            wv += (-wob * 260 - wv * (takla ? 9 : 18)) * dt; wob += wv * dt; if (reduce) wob = 0;
            phi += dt * Mathf.Min(40, sp * 1.6f) * (s.dead ? 0 : 1);
            var up = Mathf.Abs(d.y) < .9f ? Vector3.up : Vector3.right; var e1 = Vector3.Cross(d, up).normalized;
            var dd = Quaternion.AngleAxis(-wob * .35f * Mathf.Rad2Deg, Vector3.Cross(d, e1)) * d;
            var rot = Quaternion.LookRotation(dd, up) * Quaternion.Euler(0, 0, phi * Mathf.Rad2Deg);
            // Mantar merkezi yerden Rc'den aşağı inmez (eskiden Rc*.6: top yere gömülüyordu).
            var pos = Kit.W(s.x, s.y, Mathf.Max(Rc, s.z));
            if (s.dead)
            {
                // Yere iniş (2026-10-26, "top yerde gerçek gibi"): gerçek top mantarı üstüne düşer, küçük bir sekme yapar,
                // eteğin üstüne devrilir ve mantarla etek ağzı yere değecek biçimde ~16° eğik yatar. Süre/sekme Core'da
                // (Shuttle.YerPoz) ki HTML aynı sayıları kullansın. Sekme görselde G katı: 3,2× büyük çizilen topta
                // fiziksel 5 cm sekme okunmuyor. Devrilme yönü iniş yönü: mantar durur, etek ileri kapaklanır.
                float hop, dev; Shuttle.YerPoz(s.yerT, s.inVz, out hop, out dev); if (reduce) hop = 0;
                var f = new Vector3(s.inVx, 0, -s.inVy); if (f.sqrMagnitude < 1e-4f) { float h = Mathf.Sin(s.x * 12.9898f + s.y * 78.233f) * 43758.5f; h -= Mathf.Floor(h); f = new Vector3(Mathf.Cos(h * 6.283f), 0, Mathf.Sin(h * 6.283f)); }
                f.Normalize();
                var dRest = -f * Mathf.Cos(Shuttle.YER_EGIM) + Vector3.down * Mathf.Sin(Shuttle.YER_EGIM);
                var dc = Vector3.SlerpUnclamped(Vector3.down, dRest, reduce ? 1 : dev).normalized; d = dc;
                var up2 = Mathf.Abs(dc.y) < .9f ? Vector3.up : f;
                rot = Quaternion.LookRotation(dc, up2) * Quaternion.Euler(0, 0, phi * Mathf.Rad2Deg);
                pos = Kit.W(s.x, s.y, Rc + hop * G);
            }
            body.gameObject.SetActive(true); body.SetPositionAndRotation(pos, rot);
            // ayrıntı eşiği: etek ağzı ekranda 3,2 px (640 px referans) altındaysa kenar çizgileri kapanır
            if (cam != null) { float px = PxPerM(pos) * Rt; lines.SetActive(px > 3.2f); }
            // --- hareket bulanıklığı ---
            int gn = !reduce && sp > 18 ? Mathf.Min(3, Mathf.FloorToInt(sp / 18)) : 0;
            var vW = new Vector3(s.vx, s.vz, -s.vy);
            for (int i = 0; i < 3; i++)
            {
                bool on = i < gn && !s.dead; ghosts[i].SetActive(on); if (!on) continue;
                float back = (i + 1) * (1 / 60f) / (gn + 1); ghosts[i].transform.SetPositionAndRotation(pos - vW * back, rot);
            }
            // --- iz: son 8 nokta (smaçta 14); renk: akış (sen) altın, süper turuncu, smaç limon, diğer beyaz .3 ---
            if (!s.dead) { trailPts.Add(pos); int max = s.type == "smash" ? 14 : 8; while (trailPts.Count > max) trailPts.RemoveAt(0); }
            trail.enabled = trailPts.Count > 1;
            if (trail.enabled)
            {
                // 2026-10-27 performans turu 2: her uçuş karesinde ToArray + yeni AnimationCurve + yeni Gradient (yönetilen ve
                // yerel ayırma, karede ~6 nesne) yerine noktalar tek tek (en çok 14, ayırmasız); eğri ve renk yalnız biçim (akış/süper/smaç/düz) değişince.
                int n = trailPts.Count; trail.positionCount = n; for (int i = 0; i < n; i++) trail.SetPosition(i, trailPts[i]);
                Color c; float w, a;
                if (M.flow && s.from == "me") { c = new Color(1, .82f, .23f); w = 2.5f; a = .9f; } else if (s.super) { c = new Color(1, .47f, .16f); w = 10; a = .9f; }
                else if (s.type == "smash") { c = new Color(.85f, 1, .35f); w = 6; a = .8f; } else { c = Color.white; w = 2.5f; a = .3f; }
                if (c != izC || w != izW || a != izA)
                {
                    izC = c; izW = w; izA = a;
                    trail.widthCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, w / 28f));
                    izRenk[0] = new GradientColorKey(c, 0); izRenk[1] = new GradientColorKey(c, 1); izSaydam[0] = new GradientAlphaKey(0, 0); izSaydam[1] = new GradientAlphaKey(a, 1);
                    izGrad.SetKeys(izRenk, izSaydam); trail.colorGradient = izGrad;
                }
            }
            // --- gölge + yükseklik çizgisi ---
            float zz = Mathf.Max(0, s.z); shadow.gameObject.SetActive(true); shadow.position = Kit.W(s.x, s.y, .005f);
            float sh = 5f / 28 * (1 + zz * .3f); shadow.localScale = new Vector3(sh * 2.4f, sh * 2.4f, 1); shadowM.color = new Color(0, 0, 0, .4f / (1 + zz * .35f) * 1.4f);
            hLine.enabled = zz > .25f; if (hLine.enabled) { hLine.positionCount = 2; hLine.SetPosition(0, Kit.W(s.x, s.y, 0)); hLine.SetPosition(1, pos); }
            // --- yer çizgisi: rakip topu, file bu yanında, inişe 1,1 sn'den az ---
            float remL = s.landPred != null && s.from == "op" ? s.landPred.t - s.t : 9;
            gLine.enabled = !s.dead && s.from == "op" && s.y > Court.NET - 1 && remL < 1.1f;
            if (gLine.enabled) { gLine.positionCount = 2; gLine.SetPosition(0, Kit.W(0, s.y, .006f)); gLine.SetPosition(1, Kit.W(Court.CW, s.y, .006f)); }
            // --- iniş halkası ---
            var lp = s.landPred; bool land = s.from == "op" && !s.dead && lp != null && lp.y > Court.NET && !(lp.net != null && !lp.net.ok);
            ring.enabled = land; ringS.enabled = false; ringFill.gameObject.SetActive(false);
            if (land)
            {
                var q = Kit.W(lp.x, lp.y, .008f); float rem = Mathf.Max(0, lp.t - s.t); Kit.Ellipse(ring, q, 9f / 28, 9f / 28);
                bool near = Mathf.Sqrt((lp.x - M.me.x) * (lp.x - M.me.x) + (lp.y - M.me.y) * (lp.y - M.me.y)) <= (M.easy ? 1.45f : 1.15f);
                if (rem < .9f) { float f2 = Mathf.Max(0, rem / .9f); ringS.enabled = true; Kit.Ellipse(ringS, q, (9 + 18 * f2) / 28, (9 + 18 * f2) / 28); ringSM.color = near ? new Color(.85f, 1, .35f, .7f) : new Color(1, 1, 1, .5f); }
                if (rem < .35f) { ringFill.gameObject.SetActive(true); ringFill.position = q; ringFill.localScale = Vector3.one * 9f / 28; ringFillM.color = near ? new Color(.85f, 1, .35f, .25f) : new Color(1, 1, 1, .18f); }
            }
            // --- Usta erişim halkaları ---
            bool er = !M.easy && M.ogr == null && s.from == "op" && !s.dead && s.y > Court.NET - 1;
            erIc.enabled = erDis.enabled = er;
            if (er)
            {
                var mp = Kit.W(M.me.x, M.me.y, .007f); float r1 = M.ErisimIc, r2 = r1 + Match.USTA_UZAN;
                Kit.Ellipse(erIc, mp, r1, r1); Kit.Ellipse(erDis, mp, r2, r2);
            }
            // --- Usta duruş işareti (P3) ---
            { float dx = 0, dy = 0; bool du = er && M.DurusNoktasi(out dx, out dy); Durus(du, dx, dy, du && M.Yerinde()); }
            // --- hedef önizleme ---
            prevRing.enabled = prevX1.enabled = prevX2.enabled = previewOn;
            if (previewOn)
            {
                var q = Kit.W(previewAt.x, previewAt.y, .01f); Kit.Ellipse(prevRing, q, 12f / 28, 12f / 28);
                prevX1.positionCount = prevX2.positionCount = 2; prevX1.SetPosition(0, q + Vector3.left * .2f); prevX1.SetPosition(1, q + Vector3.right * .2f); prevX2.SetPosition(0, q + Vector3.back * .14f); prevX2.SetPosition(1, q + Vector3.forward * .14f);
            }
        }
        // 640 px referans ekranda, verilen dünya noktasında 1 m kaç piksel (HTML P().ppm karşılığı)
        public float PxPerM(Vector3 w)
        {
            var a = cam.WorldToScreenPoint(w); var b = cam.WorldToScreenPoint(w + cam.transform.right); return Vector2.Distance(a, b) * 640f / Mathf.Max(1, Screen.height);
        }
    }
}
