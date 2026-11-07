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
            // Renkler (2026-10-26, yasin "sahalar aslında uygun çizilmeli, renkler de o şekilde olmalı"): gerçek kort gibi tek
            // renk yeşil mat (BWF onaylı matlar yeşil; tekler bandı ayrı renk DEĞİL), mat yanda 0,5 m, dipte 1 m taşar
            // (BWF tesis şartnamesi), dışı koyu zemin. Turnuvada mavi zemin (yayın görüntüsü), okulda ahşap, gecede koyu mavi.
            {"salon",  new Saha{ad="Salon",kort="#2e7a4f",tek="#2e7a4f",cevre="#2b3a42",ust="#11181d",dol=.6f}},
            {"turnuva",new Saha{ad="Turnuva",kort="#237a4b",tek="#237a4b",cevre="#1d3b6e",ust="#0b1424",dol=.92f}},
            {"okul",   new Saha{ad="Okul (ahşap)",kort="#b07a45",tek="#b07a45",cevre="#7c5230",ust="#21170f",tahta=true,dol=.3f}},
            {"park",   new Saha{ad="Gece",kort="#2d5e7d",tek="#2d5e7d",cevre="#1b2a38",ust="#090f18",dol=.38f}}};
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
            var floor = Kit.Prim(PrimitiveType.Cube, "kort", k, new Vector3(0, -.004f, 0), new Vector3(Court.CW + (T.tahta ? .1f : 1f), .01f, Court.L + (T.tahta ? .1f : 2f)), km);
            if (T.tahta) { var tm = floor.GetComponent<Renderer>().material; tm.mainTextureScale = new Vector2(1, 9); }
            // Mat şeritleri (stüdyo incelemesi 2026-11-05, sanat 3): gerçek kort matı boyuna rulolarla serilir; rulolar arası
            // %3 ton farkı düz yeşili "boyanmış zemin"den "serilmiş mat"a çevirir. Tek renk kuralı (2026-10-26) korunur: tekler
            // bandı ayrı renk değil, fark yalnız rulo genişliğinde (≈1,52 m, 4 rulo) ve çizgilerin altında kalır.
            if (!T.tahta) { var ko = Kit.Hex(T.kort); var rm = Kit.Lit(new Color(ko.r * 1.03f, ko.g * 1.03f, ko.b * 1.03f), .3f); float rw = Court.CW / 4;
                for (int ri = 1; ri < 4; ri += 2) Kit.Prim(PrimitiveType.Cube, "mat_rulo", k, Kit.W(rw * (ri + .5f), Court.L / 2, -.0015f), new Vector3(rw, .01f, Court.L + 2f), rm).GetComponent<Renderer>().receiveShadows = true; }
            floor.GetComponent<Renderer>().receiveShadows = true;
            if (!T.tahta && T.tek != T.kort)
            {
                var tek = Kit.Prim(PrimitiveType.Cube, "tekler", k, new Vector3(0, -.0035f, 0), new Vector3(Court.SR - Court.SL, .01f, Court.L), Kit.Lit(Kit.Hex(T.tek), .3f));
                tek.GetComponent<Renderer>().receiveShadows = true;
            }

            // Çizgiler (2026-10-26, yasin build 38 "saha doğru çizilememiş"): Unity ekran turu gösterdi ki çizgi üçgenleri aşağı
            // bakıyordu (sarım ters), yukarıdan bakan kamera arka yüzü kırpıyordu: kortta hiç çizgi görünmüyordu. Sarım
            // düzeltildi, yükseklik 6 mm (zemin 1 mm, mat üstü 1,5 mm; 15 m'deki derinlik hassasiyetinde titremesin).
            var v = new List<Vector3>(); var tr = new List<int>(); const float w = .04f, h = .006f;
            System.Action<float, float, float, float> seg = (x1, y1, x2, y2) =>
            {
                Vector3 a = Kit.W(x1, y1, h), b = Kit.W(x2, y2, h), d = (b - a).normalized, n = Vector3.Cross(Vector3.up, d) * (w / 2);
                int i = v.Count; v.Add(a - n); v.Add(a + n); v.Add(b + n); v.Add(b - n);
                tr.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            };
            // BWF Kural 1: ölçüler çizgilerin dış kenarına, çizgi sınırladığı alana dahil. Bu yüzden çizgi ekseni kendi
            // alanının içine yarım genişlik (2 cm) kayar: dış sınır içeri, tekler kenarı tekler alanına, kısa servis ve
            // çiftler uzun servis çizgisi servis karesine. Orta çizgi iki servis karesinin ortak sınırı, ortadan geçer.
            float L = Court.L, CW = Court.CW, N = Court.NET, e = w / 2;
            seg(0, e, CW, e); seg(0, L - e, CW, L - e); seg(e, 0, e, L); seg(CW - e, 0, CW - e, L);
            seg(Court.SL + e, 0, Court.SL + e, L); seg(Court.SR - e, 0, Court.SR - e, L);
            seg(0, N - Court.SSL - e, CW, N - Court.SSL - e); seg(0, N + Court.SSL + e, CW, N + Court.SSL + e);
            seg(0, .76f + e, CW, .76f + e); seg(0, L - .76f - e, CW, L - .76f - e);
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
            Tribun(k, T); if (Reklam.KENAR_PANO) KenarPano(k); Sandalyeler(k); Isik(k); ArkaPlan(k, sahaId ?? "salon");
            return k.gameObject;
        }

        // Rakibin arkası: pano (0,8 m, 2 m'lik dönüşümlü renk), arkasında 2 basamak, oturan seyirci (gövde + baş, bazen saç).
        static void Tribun(Transform k, Saha T)
        {
            var root = new GameObject("tribun").transform; root.SetParent(k, false); Seyirciler.Clear(); Ledler.Clear();
            string[] giysi = { "#e85d4f", "#3f7fd8", "#f2c14e", "#f4f1ea", "#2c3e50", "#8e5bd1", "#3fb37f", "#ff8a3c", "#d94f8a", "#5fc4d9" }, ten = { "#f1c9a5", "#d9a57a", "#a8714a", "#6e4a2f", "#f5d6bb" };
            var mats = new Dictionary<string, Material>(); System.Func<string, Material> M = hx => { Material mm; if (!mats.TryGetValue(hx, out mm)) { mats[hx] = mm = Kit.Lit(Kit.Hex(hx), .15f); mm.enableInstancing = true; } return mm; };
            // GPU örnekleme (2026-11-06 ek tur fikir 8, perf): seyirci aynı kapsül/küre ağıyla renk başına tek malzeme; örnekleme
            // açıkken aynı ağ + malzeme tek çizimde gider (Standard destekler). Birleştirilmiş mesh değil: seyirci hareket ediyor (fikir 2).
            var rnd = new System.Random(7); float X0 = -1.6f, X1 = Court.CW + 1.6f, pano = -1.3f, DER = .55f, YUK = .42f;
            for (float x = X0; x < X1; x += 2) { float x2 = Mathf.Min(X1, x + 2); Kit.Prim(PrimitiveType.Cube, "pano", root, Kit.W((x + x2) / 2, pano, .4f), new Vector3(x2 - x, .8f, .06f), M(((int)((x - X0) / 2)) % 2 == 1 ? "#1d2b3a" : "#243a4f")); }
            // LED pano yüzü (stüdyo incelemesi 2026-11-05, sanat 2): konsept karedeki ışıklı panolar Unity'ye geçmemişti. Kasa aynı,
            // kortun önüne bakan yüzde nokta vuruşlu (dot-matrix) yazı: oyunun adı ve sayılar, palet C renkleriyle (limon, turuncu,
            // orkide). Işık hesaplamayan malzeme: yayın panosu gibi kendi parlar, gölge/ışık maliyeti yok.
            string[] yazi = { "BIRDIE SMASH", "SMASH 300", "BIRDIE SMASH", "RALLY", "BIRDIE SMASH" }; string[] renk = { "#c4ed3f", "#ff9a3c", "#c4ed3f", "#e99bff", "#c4ed3f" };
            int pi = 0; for (float x = X0; x < X1; x += 2, pi++)
            {
                float x2 = Mathf.Min(X1, x + 2); var tex = Kit.LedTex(yazi[pi % yazi.Length], Kit.Hex(renk[pi % renk.Length]), Kit.Hex("#0d1420"));
                var yuz = Kit.Prim(PrimitiveType.Quad, "led", root, Kit.W((x + x2) / 2, pano + .05f, .46f), new Vector3(x2 - x - .06f, .35f, 1), Kit.Clear(Color.white, tex));
                yuz.transform.rotation = Quaternion.identity; var col = yuz.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                Ledler.Add(new Led { m = yuz.GetComponent<Renderer>().material, varsayilan = tex });
            }
            for (int r = 0; r < 2; r++)
            {
                float ya = pano - .15f - r * DER, z = .55f + r * YUK;
                Kit.Prim(PrimitiveType.Cube, "basamak", root, Kit.W(Court.MID, ya - DER / 2, z / 2), new Vector3(X1 - X0, z, DER), M(r == 0 ? "#1f2e37" : "#16222a"));
                for (float x = X0 + .3f; x < X1 - .2f; x += .55f)
                {
                    if (rnd.NextDouble() > T.dol) continue; float xx = x + (float)(rnd.NextDouble() - .5) * .1f, yy = ya - DER * .5f;
                    var sy = new Seyirci { x = xx, sira = r, faz = (float)rnd.NextDouble() * 6.28f, guc = .6f + (float)rnd.NextDouble() * .4f };
                    sy.govde = Kit.Prim(PrimitiveType.Capsule, "seyirci", root, Kit.W(xx, yy, z + .27f), new Vector3(.36f, .27f, .28f), M(giysi[rnd.Next(giysi.Length)])).transform;
                    sy.bas = Kit.Prim(PrimitiveType.Sphere, "bas", root, Kit.W(xx, yy, z + .66f), Vector3.one * .22f, M(ten[rnd.Next(ten.Length)])).transform;
                    if (rnd.NextDouble() < .55) sy.sac = Kit.Prim(PrimitiveType.Sphere, "sac", root, Kit.W(xx, yy - .015f, z + .7f), new Vector3(.23f, .16f, .23f), M("#1e1610")).transform;
                    sy.g0 = sy.govde.localPosition; sy.b0 = sy.bas.localPosition; if (sy.sac != null) sy.s0 = sy.sac.localPosition;
                    foreach (var c in new Component[] { sy.govde, sy.bas, sy.sac }) if (c != null) { var cl = c.GetComponent<Collider>(); if (cl != null) Object.Destroy(cl); }
                    Seyirciler.Add(sy);
                }
            }
        }

        // Kort kenarı panoları (yasin 2026-11-07: "reklam panosuna ya da sağa sola perspektifler eklenebilir mi?"). Gerçek
        // turnuvada yan çizgilerin dışında da alçak LED panolar durur; ekrandan yer çalmadan reklam/duyuru taşır. Yer: yan çizgiden
        // 1,25 m dışarıda (hakem sandalyelerinin sırtlığı −0,99 / CW+0,79'da, pano onların arkasında kalır), filenin iki yanında
        // 1 m boşluk (hakemler ve servis ölçer açık görünsün), dipten 0,5 m içeride biter. Yüz korta döner; kamera arkadan baktığı
        // için panolar kendiliğinden perspektifle daralır. Ledler'e eklenir: LedYaz olay yazısı arka panoyla birlikte kenarlarda da
        // dönüşümlü görünür. Oyuna etkisi yok (yasin: "oyunu etkilememesi gerekir"): çarpıştırıcı yok (top ve oyuncu fiziği Core'da,
        // sahne nesnesine bakmaz), kortun 1,25 m dışında; kamera arkadan baktığı için kenara kaçan oyuncunun önüne geçmez.
        // Maliyet: 12 yüz + 12 örneklenmiş kasa, ışıksız malzeme, gölge yok. Reklam SDK'sı (oyun içi reklam ağı) gelince doku buradan değişir; şimdilik yalnız kendi yazımız.
        static void KenarPano(Transform k)
        {
            var root = new GameObject("kenar_pano").transform; root.SetParent(k, false);
            const float DIS = 1.25f, BOY = 1.9f, YUK = .62f, BOS = 1f, UC = .5f;
            var kasa = Kit.Lit(Kit.Hex("#1d2b3a"), .15f); kasa.enableInstancing = true;
            string[] yazi = { "BIRDIE SMASH", "RALLY", "SMASH 300", "BIRDIE SMASH" }; string[] renk = { "#c4ed3f", "#e99bff", "#ff9a3c", "#c4ed3f" };
            int pi = 0;
            foreach (int yan in new[] { -1, 1 })
            {
                float x = yan < 0 ? -DIS : Court.CW + DIS; var don = Quaternion.Euler(0, yan < 0 ? -90 : 90, 0);
                foreach (var ar in new[] { new Vector2(UC, Court.NET - BOS), new Vector2(Court.NET + BOS, Court.L - UC) })
                {
                    int n = Mathf.Max(1, Mathf.RoundToInt((ar.y - ar.x) / BOY)); float b = (ar.y - ar.x) / n;
                    for (int i = 0; i < n; i++, pi++)
                    {
                        float y = ar.x + b * (i + .5f);
                        var g = Kit.Prim(PrimitiveType.Cube, "kenar_kasa", root, Kit.W(x + yan * .03f, y, YUK / 2), new Vector3(.06f, YUK, b - .04f), kasa);
                        var cl = g.GetComponent<Collider>(); if (cl != null) Object.Destroy(cl); g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        // Doku önbellekten (LedYaz ile aynı sözlük): her maç kurulumunda 12 doku yerine en çok 3 kez üretilir, sonra hiç.
                        string ys = yazi[pi % yazi.Length], rs = renk[pi % renk.Length]; Texture2D tex;
                        if (!ledOnbellek.TryGetValue(ys + rs, out tex)) { tex = Kit.LedTex(ys, Kit.Hex(rs), Kit.Hex("#0d1420")); ledOnbellek[ys + rs] = tex; }
                        var yuz = Kit.Prim(PrimitiveType.Quad, "kenar_led", root, Kit.W(x - yan * .005f, y, YUK * .55f), new Vector3(b - .1f, YUK * .5f, 1), Kit.Clear(Color.white, tex));
                        yuz.transform.rotation = don; cl = yuz.GetComponent<Collider>(); if (cl != null) Object.Destroy(cl); var yr = yuz.GetComponent<Renderer>(); yr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; yr.receiveShadows = false;
                        Ledler.Add(new Led { m = yuz.GetComponent<Renderer>().material, varsayilan = tex });
                    }
                }
            }
        }

        // ---- Canlı tribün ve LED (2026-11-06 ek tur, fikir 2 ve 3; yasin "10 yaratıcı fikir … uygula") ----
        // Ses ralliye tepki veriyordu (Sfx: 8/12/16 vuruşta "ooo", 10+ ralli sonrası alkış), görüntü vermiyordu: seyirci heykeldi.
        // GameRoot her kare TribunTick'i çağırır; durum (heyecan, sıçrama, dalga) GameRoot'ta, burada yalnız poz:
        //  - boşta: kişi başına fazlı, 1 cm'lik nefes;
        //  - heyecan (ralli 6→12 arası 0→1): öne eğilme (6 cm, korta doğru) ve biraz daha hızlı kıpırdanma;
        //  - sıçrama (sayı sonu, smaçta tam): kişi başına güç ve x'e göre gecikmeyle en çok 15 cm;
        //  - dalga (15+ ralli, bir kez): soldan sağa 7 m/sn, geçtiği kişi 14 cm kalkar.
        // Genlik bilerek küçük: hareket topun arkasında, gözü topa değil sahneye çekmeli (VFX kuralı: dikkat çekici ama dağıtmayan).
        // Hareketi azaltta hiç çağrılmaz (GameRoot), seyirci yerinde kalır.
        public class Seyirci { public Transform govde, bas, sac; public Vector3 g0, b0, s0; public float x, faz, guc; public int sira; }
        public static readonly List<Seyirci> Seyirciler = new List<Seyirci>();
        public static void TribunTick(float t, float heyecan, float sicra, float dalgaX)
        {
            Vector3 one = Kit.W(0, .06f, 0) - Kit.W(0, 0, 0);   // korta doğru 6 cm
            for (int i = 0; i < Seyirciler.Count; i++)
            {
                var s = Seyirciler[i]; if (s.govde == null) continue;
                float y = .01f * Mathf.Sin(t * (1.3f + heyecan * 1.5f) + s.faz);
                float gec = Mathf.Clamp01(sicra * 1.15f - Mathf.Abs(s.x - Court.MID) * .03f);
                y += .15f * s.guc * Mathf.Sin(gec * Mathf.PI) * (sicra > 0 ? 1 : 0);
                if (dalgaX > -50) { float d = Mathf.Abs(s.x - dalgaX); if (d < .8f) y += .14f * Mathf.Cos(d / .8f * Mathf.PI * .5f); }
                Vector3 o = Vector3.up * y + one * heyecan * s.guc;
                s.govde.localPosition = s.g0 + o; s.bas.localPosition = s.b0 + o * 1.15f; if (s.sac != null) s.sac.localPosition = s.s0 + o * 1.15f;
            }
        }
        // LED panolar: önce üretilmiş dokular, yalnız değişince atanır (kare başı doku üretimi yok). null = varsayılan yazı.
        public class Led { public Material m; public Texture2D varsayilan; public Texture2D simdi; }
        public static readonly List<Led> Ledler = new List<Led>();
        static readonly Dictionary<string, Texture2D> ledOnbellek = new Dictionary<string, Texture2D>();
        public static void LedYaz(string yazi, string renk)
        {
            Texture2D tx = null;
            if (yazi != null && !ledOnbellek.TryGetValue(yazi + renk, out tx)) { tx = Kit.LedTex(yazi, Kit.Hex(renk), Kit.Hex("#0d1420")); ledOnbellek[yazi + renk] = tx; }
            for (int i = 0; i < Ledler.Count; i++)
            {
                var l = Ledler[i]; if (l.m == null) continue;
                var hedef = tx != null && i % 2 == 0 ? tx : l.varsayilan;   // dönüşümlü: biri yeni yazı, biri marka (pano dizisi tek renk afişe dönmesin)
                if (l.simdi == hedef) continue; l.simdi = hedef; l.m.mainTexture = hedef;
            }
        }

        // Baş hakem: filenin bir ucunda (x −0,75), 1,5 m oturaklı yüksek sandalye (BWF tesis şartnamesi: oturma yüksekliği
        // fileyle aynı, ~1,55 m), sırtlık ve ayaklık; servis hakemi karşı uçta (x CW+0,6) 0,45 m normal sandalyede,
        // önünde 1,15 m servis ölçeri. HAKEM1/2 = oturak üst yüzü (PlayerView.ShowSit kalçayı buraya oturtur).
        // 2026-10-26: eskiden yüksek sandalyenin oturağı yoktu, hakem ayaklığın üstünde ayakta duruyordu.
        // Ayaklık oturağın 0,5 m altında (2026-10-26): 0,93 m'deyken oturak−ayaklık 0,6 m'ydi, 1,75 m'lik insanın diz–taban
        // boyu ~0,5 m; ayaklar boşlukta sallanıyordu. Ayaklık oturağın önünde 0,42 m (diz izdüşümü).
        public const float BH_X = -.75f, SH_X = Court.CW + .6f, BH_Z = 1.5f, SH_Z = .45f, AYAKLIK_Y = BH_Z - .48f, AYAKLIK_ILERI = .42f;
        public static readonly Vector3 HAKEM1 = Kit.W(BH_X, Court.NET, BH_Z + .03f), HAKEM2 = Kit.W(SH_X, Court.NET - .1f, SH_Z + .025f);
        static void Sandalyeler(Transform k)
        {
            var g = new GameObject("sandalyeler").transform; g.SetParent(k, false); var metal = Kit.Lit(Kit.Hex("#cfd6d4"), .6f); var koyu = Kit.Lit(Kit.Hex("#3a4a52"), .3f);
            float x0 = BH_X, N = Court.NET;
            foreach (float dx in new[] { -.22f, .22f }) foreach (float dy in new[] { -.22f, .22f })
                Kit.Prim(PrimitiveType.Cylinder, "ayak", g, Kit.W(x0 + dx, N + dy, BH_Z / 2), new Vector3(.035f, BH_Z / 2, .035f), metal);
            Kit.Prim(PrimitiveType.Cube, "oturak", g, Kit.W(x0, N, BH_Z), new Vector3(.5f, .05f, .5f), koyu);
            Kit.Prim(PrimitiveType.Cube, "sirtlik", g, Kit.W(x0 - .24f, N, BH_Z + .3f), new Vector3(.04f, .55f, .5f), koyu);
            Kit.Prim(PrimitiveType.Cube, "ayaklik", g, Kit.W(x0 + AYAKLIK_ILERI, N, AYAKLIK_Y - .02f), new Vector3(.3f, .04f, .5f), metal);
            foreach (float dy in new[] { -.22f, .22f }) Kit.Prim(PrimitiveType.Cube, "ayaklik_kol", g, Kit.W(x0 + (.2f + AYAKLIK_ILERI) / 2, N + dy, AYAKLIK_Y - .02f), new Vector3(AYAKLIK_ILERI - .2f + .04f, .025f, .025f), metal);
            for (int i = 1; i <= 3; i++) Kit.Prim(PrimitiveType.Cube, "basamak", g, Kit.W(x0 + .24f, N, BH_Z * i / 4), new Vector3(.06f, .03f, .46f), metal);
            float x1 = SH_X;
            foreach (float dx in new[] { -.17f, .17f }) foreach (float dy in new[] { -.17f, .17f })
                Kit.Prim(PrimitiveType.Cylinder, "ayak", g, Kit.W(x1 + dx, N - .1f + dy, SH_Z / 2), new Vector3(.03f, SH_Z / 2, .03f), metal);
            Kit.Prim(PrimitiveType.Cube, "oturak", g, Kit.W(x1, N - .1f, SH_Z), new Vector3(.4f, .05f, .4f), koyu);
            Kit.Prim(PrimitiveType.Cube, "sirtlik", g, Kit.W(x1 + .19f, N - .1f, SH_Z + .25f), new Vector3(.04f, .45f, .4f), koyu);
            Kit.Prim(PrimitiveType.Cylinder, "olcer", g, Kit.W(Court.CW + .1f, Court.NET + .2f, .575f), new Vector3(.02f, .575f, .02f), Kit.Lit(new Color(.94f, .94f, .94f), .4f));
            Kit.Prim(PrimitiveType.Cube, "olcer_cizgi", g, Kit.W(Court.CW + .1f, Court.NET + .2f, 1.15f), new Vector3(.16f, .02f, .02f), Kit.Lit(new Color(.94f, .94f, .94f), .4f));
        }

        // Arka plan (stüdyo incelemesi 2026-11-05, sanat 2 ve 6): tribünün üstü düz siyah boşluktu ("ışığı kapalı salon").
        // Tribün arkasına dikey degrade duvar (alt açık, üst saha rengine kararır) ve sahaya özgü tek imza: Gece parkında ağaç
        // siluetleri ve yıldızlar. Işık hesaplamayan malzeme, gölge yok; kort ve top okunurluğuna dokunmaz (kortun ≥ 2 m gerisi).
        static readonly Dictionary<string, string[]> ARKA = new Dictionary<string, string[]> {
            {"salon",new[]{"#22313d","#11181d"}},{"turnuva",new[]{"#24457a","#0b1424"}},{"okul",new[]{"#5a3c24","#21170f"}},{"park",new[]{"#2b2457","#10163a"}}};
        static void ArkaPlan(Transform k, string saha)
        {
            string[] r; if (!ARKA.TryGetValue(saha, out r)) r = ARKA["salon"];
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp; Color a = Kit.Hex(r[0]), b = Kit.Hex(r[1]);
            for (int i = 0; i < 64; i++) tex.SetPixel(0, i, Color.Lerp(a, b, Mathf.SmoothStep(0, 1, i / 63f))); tex.Apply();
            const float Y = -3.6f, GEN = 22, YUK = 9;
            Kit.Prim(PrimitiveType.Quad, "arka_duvar", k, Kit.W(Court.MID, Y, YUK / 2 - .2f), new Vector3(GEN, YUK, 1), Kit.Clear(Color.white, tex));
            if (saha != "park") { UstSalon(k, saha, Y + .05f, GEN); return; }
            var sil = Kit.Unlit(Kit.Hex("#0d1426")); var rnd = new System.Random(11);
            float[] xs = { -4.6f, -2.9f, Court.CW + 2.6f, Court.CW + 4.2f, Court.CW + 5.8f };
            foreach (float x in xs)
            {
                float h = 2.2f + (float)rnd.NextDouble() * 1.4f, c = 1.3f + (float)rnd.NextDouble() * .6f;
                Kit.Prim(PrimitiveType.Cylinder, "agac_govde", k, Kit.W(x, Y + .3f, h / 2), new Vector3(.22f, h / 2, .22f), sil);
                Kit.Prim(PrimitiveType.Sphere, "agac_tac", k, Kit.W(x, Y + .3f, h + c * .3f), new Vector3(c, c * 1.1f, c), sil);
            }
            var yild = Kit.Clear(new Color(1, .96f, .85f, .8f), Kit.RadialTex());
            for (int i = 0; i < 26; i++) { float s = .05f + (float)rnd.NextDouble() * .07f;
                Kit.Prim(PrimitiveType.Quad, "yildiz", k, Kit.W(Court.MID + ((float)rnd.NextDouble() - .5f) * GEN * .9f, Y + .05f, 4.2f + (float)rnd.NextDouble() * 4.3f), new Vector3(s, s, 1), yild); }
        }

        // Salonun üstü (2026-11-06, yasin: "yüksek clearlarda daha gerçeğe uygun kaplamalarla doldurulmalı"; öneri artifact'i YAP).
        // Top tepedeyken kamera tribünün üstüne bakıyor, orası tek degrade duvardı. Araştırma (BWF tesis şartnamesi, yayın
        // görüntüleri, muadiller): topun arkası koyu ve mat kalır ki beyaz tüy okunsun; ışık kortun tam üstünde değil iki yanda
        // armatür sıraları; tribünün üstünde ikinci kat oturma ve önünde LED şerit; uzaklaştıkça karanlık. Uygulama:
        //  - İkinci kat tribün: tek dörtgen, kendi ürettiğimiz 256×48 doku (soluk baş noktaları, yukarı doğru kararır). Sahne
        //    nesnesi değil doku: yüzlerce kapsül yerine tek çizim.
        //  - Önünde ince LED şerit (sahanın vurgu rengi, %45 parlaklık): yayın salonlarının ikinci kat bandı.
        //  - Tavan: duvarın üst üçte biri zaten koyu; iki yanda (orta 5 m boş) üç sıra armatür ışığı, sönük ışınsal leke (ışık hesabı yok).
        //  - Sahaya göre tek imza: turnuvada ülke flamaları, okulda okul flamaları ve basketbol potası, salonda akustik paneller.
        // Orta şerit (topun yolu, MID ± 2,5 m) yalnız koyu tribün dokusu taşır; parlak öğe yok. Hepsi tek seferlik basit geometri.
        static readonly Dictionary<string, string> VURGU = new Dictionary<string, string> { {"salon","#7fd3c7"},{"turnuva","#c4ed3f"},{"okul","#ffb35c"} };
        static void UstSalon(Transform k, string saha, float Y, float GEN)
        {
            var g = new GameObject("ust_salon").transform; g.SetParent(k, false);
            string vr; if (!VURGU.TryGetValue(saha, out vr)) vr = VURGU["salon"];
            // ikinci kat: z 2,5–4,3 m
            const float Z0 = 2.5f, Z1 = 4.3f;
            Kit.Prim(PrimitiveType.Quad, "ikinci_kat", g, Kit.W(Court.MID, Y + .02f, (Z0 + Z1) / 2), new Vector3(GEN * .92f, Z1 - Z0, 1), Kit.Clear(Color.white, KalabalikTex(saha)));
            var led = Kit.Hex(vr); led.a = .45f;
            Kit.Prim(PrimitiveType.Quad, "led_serit", g, Kit.W(Court.MID, Y + .03f, Z0 - .12f), new Vector3(GEN * .92f, .14f, 1), Kit.Clear(led));
            // armatür sıraları: iki yanda, üç sıra (z 6,2 / 7,0 / 7,8), ortada boşluk
            var lek = Kit.Additive(new Color(1, .95f, .82f, .5f), Kit.RadialTex());
            for (int sira = 0; sira < 3; sira++)
            {
                float z = 6.2f + sira * .8f, boy = .55f - sira * .1f;
                for (int i = 0; i < 4; i++) foreach (float yon in new[] { -1f, 1f })
                {
                    float x = Court.MID + yon * (2.8f + i * 1.6f); if (Mathf.Abs(x - Court.MID) > GEN / 2 - .6f) continue;
                    Kit.Prim(PrimitiveType.Quad, "armatur", g, Kit.W(x, Y + .04f, z), new Vector3(boy * 1.6f, boy * .5f, 1), lek);
                }
            }
            if (saha == "turnuva")
            {   // ülke flamaları: ikinci katın üstünde, orta şerit dışında; iki renkli dikey bant (bayrak deseni değil, renk çifti)
                string[][] cift = { new[]{"#d8323c","#f4f1ea"}, new[]{"#1f5fbf","#f2c14e"}, new[]{"#2e9a5a","#f4f1ea"}, new[]{"#f4f1ea","#c8102e"}, new[]{"#111111","#f2c14e"}, new[]{"#0b3d91","#d8323c"} };
                int n = 0;
                for (float x = Court.MID - GEN * .44f; x < Court.MID + GEN * .44f; x += 1.15f)
                {
                    if (Mathf.Abs(x - Court.MID) < 2.5f) continue; var c = cift[n++ % cift.Length];
                    Kit.Prim(PrimitiveType.Quad, "flama", g, Kit.W(x - .17f, Y + .03f, Z1 + .55f), new Vector3(.34f, .7f, 1), Kit.Unlit(Kit.Hex(c[0]) * .8f));
                    Kit.Prim(PrimitiveType.Quad, "flama", g, Kit.W(x + .17f, Y + .03f, Z1 + .55f), new Vector3(.34f, .7f, 1), Kit.Unlit(Kit.Hex(c[1]) * .8f));
                }
            }
            else if (saha == "okul")
            {   // okul flamaları (iki uzun, sol ve sağ) + sağda basketbol potası (pano + çember), orta şerit boş
                foreach (float yon in new[] { -1f, 1f })
                    Kit.Prim(PrimitiveType.Quad, "okul_flama", g, Kit.W(Court.MID + yon * 5.4f, Y + .03f, Z1 + 1.1f), new Vector3(.9f, 1.8f, 1), Kit.Unlit(Kit.Hex(yon < 0 ? "#8e2b2b" : "#24406b") * .85f));
                float px = Court.MID + 7.6f;
                Kit.Prim(PrimitiveType.Quad, "pota_pano", g, Kit.W(px, Y + .2f, 3.7f), new Vector3(1.6f, 1.05f, 1), Kit.Unlit(new Color(.82f, .8f, .74f)));
                Kit.Prim(PrimitiveType.Quad, "pota_cerceve", g, Kit.W(px, Y + .21f, 3.55f), new Vector3(.55f, .4f, 1), Kit.Unlit(new Color(.55f, .3f, .2f)));
                Kit.Prim(PrimitiveType.Cylinder, "pota_cember", g, Kit.W(px, Y + .45f, 3.2f), new Vector3(.45f, .01f, .45f), Kit.Unlit(Kit.Hex("#e0662a")));
            }
            else
            {   // salon: akustik duvar panelleri, iki yanda 3×2 ızgara, duvardan biraz açık
                var pm = Kit.Unlit(new Color(.17f, .23f, .28f));
                for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) foreach (float yon in new[] { -1f, 1f })
                    Kit.Prim(PrimitiveType.Quad, "akustik", g, Kit.W(Court.MID + yon * (3.6f + i * 1.9f), Y + .03f, Z1 + .6f + j * .95f), new Vector3(1.6f, .8f, 1), pm);
            }
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        }
        // İkinci kat kalabalığı: koyu zemin üstünde soluk giysi/baş noktaları; satırlar yukarı doğru kararır ve seyrelir.
        static Texture2D KalabalikTex(string saha)
        {
            const int W = 256, H = 48; var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[W * H]; Color zemin = saha == "okul" ? new Color(.16f, .12f, .09f) : new Color(.08f, .11f, .15f);
            for (int i = 0; i < px.Length; i++) px[i] = zemin;
            string[] giysi = { "#e85d4f", "#3f7fd8", "#f2c14e", "#f4f1ea", "#8e5bd1", "#3fb37f", "#ff8a3c", "#5fc4d9" };
            var rnd = new System.Random(5);
            for (int sira = 0; sira < 4; sira++)
            {
                int y0 = 4 + sira * 11; float sol = .55f - sira * .1f;   // yukarı (y büyük) kararır
                for (int x = 2; x < W - 2; x += 4)
                {
                    if (rnd.NextDouble() > .78) continue; var c = Color.Lerp(zemin, Kit.Hex(giysi[rnd.Next(giysi.Length)]), sol);
                    var bas = Color.Lerp(zemin, new Color(.85f, .68f, .52f), sol * .8f); int xx = x + rnd.Next(2);
                    for (int dy = 0; dy < 5; dy++) for (int dx = 0; dx < 3; dx++) px[(y0 + dy) * W + xx + dx] = c;
                    for (int dy = 5; dy < 7; dy++) for (int dx = 0; dx < 2; dx++) px[(y0 + dy) * W + xx + dx] = bas;
                }
            }
            tex.SetPixels(px); tex.Apply(false, true); return tex;
        }

        // Yayın ışığı: tavanda kortun boyunca üç lamba (HTML lamba havuzları NET×.45, NET, NET×1.55), sıcak beyaz.
        static void Isik(Transform k)
        {
            foreach (float y in new[] { Court.NET * .45f, Court.NET, Court.NET * 1.55f })
            {
                var g = new GameObject("lamba"); g.transform.SetParent(k, false); var l = g.AddComponent<Light>(); l.type = LightType.Spot;
                g.transform.position = Kit.W(Court.MID, y, 9); g.transform.rotation = Quaternion.Euler(90, 0, 0);
                // Stüdyo incelemesi (2026-11-05, sanat 3): 62° → 50°, ışık havuzu kortu toplar, kenarlar hafif kararır (doğal vinyet).
                l.spotAngle = 50; l.range = 16; l.intensity = 1.6f; l.color = new Color(1, .965f, .88f); l.shadows = LightShadows.None;
            }
        }
    }
}
