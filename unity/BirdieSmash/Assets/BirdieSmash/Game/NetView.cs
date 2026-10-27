// File: ızgara mesh her karede NetWave'den esnetilir (görsel kazanç 1,8, HTML ile aynı). Çarpma
// noktası kısa süre parlar; bant kordonu ayrı titrer (2026-10-26: bant kendi sert yayıyla, çarpma yerinde; NetWave.ta). Sapma vuranın tersine: top fileyi iter.
//
// 2026-10-27 (yasin "file de standartlarda olmalı bant kısmı daha sert ama file hareketli olacak", BWF Law 1):
//  - ölçüler Core'dan: en CW 6,1 m (direk merkezinden direk merkezine, uçta boşluk yok), derinlik AG_DERIN 0,76 m (bant dahil),
//    üst kenar Court.NetLimit (ortada 1,524, direkte 1,55 m, parabol sarkma), bant BANT 75 mm, ağ gözü AG_GOZ 19 mm (eskiden 40 mm);
//  - ağın satırları: alttan bandın alt kenarına kadar eşit, son satır bandın üstünde (bandın arkasında); bandın alt kenarındaki
//    satır bandın sapmasını taşır (eskiden bant durgunken bandın hemen altındaki satır 3,6 cm oynuyordu: ağ bandın altından taşıyordu);
//  - bant iki kat (kordonun üstünden katlanmış: ön ve arka yüz 3 mm aralıklı, üstte kapanır), üst kenar kordonla daha az oynar;
//  - yalnız hareketli sütunlar (NetWave.lo..hi, bir önceki karenin aralığı dahil ki duran sütun son kez düze yazılsın) ve parlayan
//    bölge yenilenir; file uyurken LateUpdate hiçbir şey yapmaz. Diziler bir kez ayrılır, karede ayırma yok; sınır kutusu sabit.
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class NetView : MonoBehaviour
    {
        public NetWave net; public float sign = -1;
        const int NX = 66, NZ = 12; const float KAT = .003f;
        Mesh mesh, tapeMesh; Vector3[] v, tv; Color[] c; float[] vx, vz, ktop;
        int pl = NX, pr = -1; bool parladi;
        // Tanı (EkranTuru rapor.txt): son yenilemede yazılan köşe sayısı, uyuyan kare sayısı.
        public int SonGuncel, UykuKare;
        static readonly Color AG = new Color(.13f, .14f, .15f, .9f), PARILTI = new Color(1, .85f, .55f, 1);

        public static NetView Build(Transform root, NetWave w)
        {
            var g = new GameObject("file"); g.transform.SetParent(root, false); var nv = g.AddComponent<NetView>(); nv.net = w; nv.Init(); return nv;
        }

        // Sütun başına köşe dizisi (i * NZ + j): bir sütun aralığı köşe dizisinde de bitişik.
        void Init()
        {
            mesh = new Mesh { name = "file" }; mesh.MarkDynamic();
            v = new Vector3[NX * NZ]; c = new Color[v.Length]; vx = new float[v.Length]; vz = new float[v.Length]; ktop = new float[NX];
            var uv = new Vector2[v.Length]; var tri = new int[(NX - 1) * (NZ - 1) * 6]; int k = 0;
            for (int i = 0; i < NX; i++)
            {
                float x = i / (float)(NX - 1) * Court.CW, top = Court.NetLimit(x), alt = top - Court.AG_DERIN, bant = top - Court.BANT; ktop[i] = top;
                for (int j = 0; j < NZ; j++)
                {
                    int n = i * NZ + j; float z = j == NZ - 1 ? top : alt + (bant - alt) * j / (NZ - 2);
                    vx[n] = x; vz[n] = z; uv[n] = new Vector2(x / Court.AG_GOZ, z / Court.AG_GOZ); c[n] = AG;
                    if (i < NX - 1 && j < NZ - 1) { int a = n, b = n + NZ; tri[k++] = a; tri[k++] = a + 1; tri[k++] = b; tri[k++] = b; tri[k++] = a + 1; tri[k++] = b + 1; }
                }
            }
            Yaz(0, NX - 1, false); mesh.vertices = v; mesh.uv = uv; mesh.colors = c; mesh.triangles = tri; mesh.bounds = Kutu();
            // Ağ koyu kordon (2026-10-26, yasin "saha nizami çizilmeli"): beyaz ağ yeşil zeminde soluk bir şerit gibi okunuyor,
            // fileyle kısa servis çizgisi karışıyordu. Gerçek filede ağ koyu, yalnız üst bant beyazdır.
            Kit.MeshObj("ag", transform, mesh, Kit.Clear(Color.white, Kit.NetTex()));
            // Bant: sütun başına 4 köşe (ön alt, ön üst, arka üst, arka alt); üç şerit: ön yüz, üst kat, arka yüz.
            tapeMesh = new Mesh { name = "bant" }; tapeMesh.MarkDynamic(); tv = new Vector3[NX * 4]; var ttri = new int[(NX - 1) * 18]; k = 0;
            for (int i = 0; i < NX - 1; i++)
                for (int s = 0; s < 3; s++) { int a = i * 4 + s, b = a + 4; ttri[k++] = a; ttri[k++] = a + 1; ttri[k++] = b; ttri[k++] = b; ttri[k++] = a + 1; ttri[k++] = b + 1; }
            YazBant(0, NX - 1); tapeMesh.vertices = tv; tapeMesh.triangles = ttri; tapeMesh.bounds = Kutu();
            Kit.MeshObj("bant", transform, tapeMesh, Kit.Clear(new Color(1, 1, 1, .95f)));
        }

        // Sabit sınır kutusu: file düzlemi ± 0,6 m (smaçta görünen şişkinlik ≤ 0,3 m), yeniden hesap yok.
        static Bounds Kutu() { return new Bounds(Kit.W(Court.MID, Court.NET, Court.NETP - Court.AG_DERIN / 2), new Vector3(Court.CW + .2f, Court.AG_DERIN + .2f, 1.2f)); }

        void Yaz(int i0, int i1, bool renk)
        {
            for (int i = i0; i <= i1; i++)
                for (int j = 0; j < NZ; j++)
                {
                    int n = i * NZ + j; float x = vx[n], z = vz[n];
                    v[n] = Kit.W(x, Court.NET + net.Gorunen(x, z), z);   // yön Core'da (NetWave.Impulse yon, 2026-10-26); sign kullanılmaz
                    if (renk) { float gd = Mathf.Exp(-((x - net.gx) * (x - net.gx) + (z - net.gz) * (z - net.gz)) / .18f) * net.glow; c[n] = Color.Lerp(AG, PARILTI, Mathf.Clamp01(gd)); }
                }
        }
        void YazBant(int i0, int i1)
        {
            for (int i = i0; i <= i1; i++)
            {
                float x = i / (float)(NX - 1) * Court.CW, top = ktop[i], wob = net.BantAt(x), ust = wob * .8f;
                // Bant 75 mm (BWF), kordonun üstünden iki kat: üst kenar gergin kordonla daha az oynar, alt kenar ağla birlikte
                // (ağın bant hizasındaki satırı tam wob'da: yırtılma yok).
                tv[i * 4] = Kit.W(x, Court.NET + wob - KAT, top - Court.BANT); tv[i * 4 + 1] = Kit.W(x, Court.NET + ust - KAT, top);
                tv[i * 4 + 2] = Kit.W(x, Court.NET + ust + KAT, top); tv[i * 4 + 3] = Kit.W(x, Court.NET + wob + KAT, top - Court.BANT);
            }
        }

        void LateUpdate()
        {
            // NetWave sütunu → görünüm sütunu (66/33 ≈ 2,03); bir komşu pay.
            int l = NX, r = -1; const float OR = (NX - 1) / (float)(NetWave.N - 1);
            if (net.active && net.lo <= net.hi) { l = Mathf.Max(0, Mathf.FloorToInt((net.lo - 1) * OR)); r = Mathf.Min(NX - 1, Mathf.CeilToInt((net.hi + 1) * OR)); }
            int ul = Mathf.Min(l, pl), ur = Mathf.Max(r, pr); pl = l; pr = r;
            bool parlak = net.glow > 0, renk = parlak || parladi; parladi = parlak;
            if (renk)
            {   // parıltı bölgesi gx ± 1,3 m (exp(−d²/0,18) < 1e-4)
                int gl = Mathf.Max(0, Mathf.FloorToInt((net.gx - 1.3f) / Court.CW * (NX - 1))), gr = Mathf.Min(NX - 1, Mathf.CeilToInt((net.gx + 1.3f) / Court.CW * (NX - 1)));
                ul = Mathf.Min(ul, gl); ur = Mathf.Max(ur, gr);
            }
            if (ul > ur) { SonGuncel = 0; UykuKare++; return; }
            Yaz(ul, ur, renk); YazBant(ul, ur); SonGuncel = (ur - ul + 1) * NZ;
            mesh.SetVertices(v); if (renk) mesh.SetColors(c); tapeMesh.SetVertices(tv);
        }

        // Tanı: dünyadaki köşelerden ölçülen file (rapor.txt; düz dururken çağrılmalı). Kit.W: dünya y = yükseklik.
        public string Olcu()
        {
            float xmin = 9, xmax = -9, zmin = 9; foreach (var p in v) { xmin = Mathf.Min(xmin, p.x); xmax = Mathf.Max(xmax, p.x); zmin = Mathf.Min(zmin, p.y); }
            float ortaUst = tv[(NX / 2) * 4 + 1].y, ortaUst2 = tv[(NX / 2 - 1) * 4 + 1].y, direkUst = tv[1].y, bant = tv[(NX / 2) * 4 + 1].y - tv[(NX / 2) * 4].y;
            float derin = ortaUst - v[(NX / 2) * NZ].y, goz = Court.AG_GOZ;
            return "file ölçü: en " + (xmax - xmin).ToString("0.000") + " m (x " + (xmin + Court.MID).ToString("0.00") + ".." + (xmax + Court.MID).ToString("0.00") + ", direkler 0 ve " + Court.CW + ")"
                + " · üst orta " + Mathf.Min(ortaUst, ortaUst2).ToString("0.000") + " direk " + direkUst.ToString("0.000") + " sarkma " + ((direkUst - Mathf.Min(ortaUst, ortaUst2)) * 1000).ToString("0") + " mm"
                + " · derinlik " + derin.ToString("0.000") + " · bant " + (bant * 1000).ToString("0") + " mm · göz " + (goz * 1000).ToString("0") + " mm · alt kenar en düşük " + zmin.ToString("0.000");
        }
    }
}
