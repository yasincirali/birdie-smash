// File: ızgara mesh her karede NetWave'den esnetilir (görsel kazanç 1,8, HTML ile aynı). Çarpma
// noktası kısa süre parlar; bant kordonu ayrı titrer (2026-10-26: bant kendi sert yayıyla, çarpma yerinde; NetWave.ta). Sapma vuranın tersine: top fileyi iter.
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class NetView : MonoBehaviour
    {
        public NetWave net; public float sign = -1;
        const int NX = 66, NZ = 12; const float DEPTH = .76f, GAIN = 1.8f;
        Mesh mesh, tapeMesh; Vector3[] v, tv; Color[] c;

        public static NetView Build(Transform root, NetWave w)
        {
            var g = new GameObject("file"); g.transform.SetParent(root, false); var nv = g.AddComponent<NetView>(); nv.net = w; nv.Init(); return nv;
        }

        void Init()
        {
            mesh = new Mesh { name = "file" }; mesh.MarkDynamic();
            v = new Vector3[NX * NZ]; c = new Color[v.Length]; var uv = new Vector2[v.Length]; var tri = new int[(NX - 1) * (NZ - 1) * 6]; int k = 0;
            for (int j = 0; j < NZ; j++) for (int i = 0; i < NX; i++)
                {
                    float x = i / (float)(NX - 1) * Court.CW, top = Court.NetLimit(x), z = top - DEPTH + DEPTH * j / (NZ - 1);
                    uv[j * NX + i] = new Vector2(x / .04f, z / .04f);
                    if (i < NX - 1 && j < NZ - 1) { int a = j * NX + i; tri[k++] = a; tri[k++] = a + NX; tri[k++] = a + 1; tri[k++] = a + 1; tri[k++] = a + NX; tri[k++] = a + NX + 1; }
                }
            Refresh(); mesh.vertices = v; mesh.uv = uv; mesh.colors = c; mesh.triangles = tri; mesh.RecalculateBounds();
            // Ağ koyu kordon (2026-10-26, yasin "saha nizami çizilmeli"): beyaz ağ yeşil zeminde soluk bir şerit gibi okunuyor,
            // fileyle kısa servis çizgisi karışıyordu. Gerçek filede ağ koyu, yalnız üst bant beyazdır.
            Kit.MeshObj("ag", transform, mesh, Kit.Clear(Color.white, Kit.NetTex()));
            tapeMesh = new Mesh { name = "bant" }; tapeMesh.MarkDynamic(); tv = new Vector3[NX * 2]; var ttri = new int[(NX - 1) * 6]; k = 0;
            for (int i = 0; i < NX - 1; i++) { int a = i * 2; ttri[k++] = a; ttri[k++] = a + 1; ttri[k++] = a + 2; ttri[k++] = a + 2; ttri[k++] = a + 1; ttri[k++] = a + 3; }
            RefreshTape(); tapeMesh.vertices = tv; tapeMesh.triangles = ttri; tapeMesh.RecalculateBounds();
            Kit.MeshObj("bant", transform, tapeMesh, Kit.Clear(new Color(1, 1, 1, .95f)));
        }

        void Refresh()
        {
            for (int j = 0; j < NZ; j++) for (int i = 0; i < NX; i++)
                {
                    float x = i / (float)(NX - 1) * Court.CW, top = Court.NetLimit(x), z = top - DEPTH + DEPTH * j / (NZ - 1);
                    float dy = net.At(x, z) * GAIN * sign; var p = Kit.W(x, Court.NET + dy, z); v[j * NX + i] = p;
                    float gd = Mathf.Exp(-((x - net.gx) * (x - net.gx) + (z - net.gz) * (z - net.gz)) / .18f) * net.glow;
                    c[j * NX + i] = Color.Lerp(new Color(.13f, .14f, .15f, .9f), new Color(1, .85f, .55f, 1), Mathf.Clamp01(gd));
                }
        }
        void RefreshTape()
        {
            for (int i = 0; i < NX; i++)
            {
                float x = i / (float)(NX - 1) * Court.CW, top = Court.NetLimit(x), wob = (Mathf.Sin(Mathf.PI * x / Court.CW) * net.tape * .02f + net.TapeAt(x) * .05f) * sign;
                // Bant 75 mm (BWF); üst kenar kordonla biraz geri kalır (kordon gergin), alt kenar ağla birlikte daha çok oynar.
                tv[i * 2] = Kit.W(x, Court.NET + wob * 1.2f, top - .073f); tv[i * 2 + 1] = Kit.W(x, Court.NET + wob * .7f, top + .002f);
            }
        }

        void LateUpdate()
        {
            if (!net.active && net.glow <= 0 && !dirty) return;
            dirty = net.active || net.glow > 0;
            Refresh(); mesh.vertices = v; mesh.colors = c; RefreshTape(); tapeMesh.vertices = tv;
        }
        bool dirty = true;
    }
}
