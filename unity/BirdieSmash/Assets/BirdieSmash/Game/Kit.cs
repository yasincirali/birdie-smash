// Ortak yardımcılar: koordinat çevirisi, malzeme, prosedürel doku ve mesh.
// Neden kodla: demo sahnesi tek satır sahne dosyası olmadan, her Unity sürümünde aynı kurulsun;
// sanatçı varlıkları geldikçe bu fabrikalar yerini prefab'lara bırakır.
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public static class Kit
    {
        // Oyun koordinatı (x eni, y boyu, z yükseklik) → Unity dünyası. Ben -Z tarafındayım, file Z=0.
        public static Vector3 W(float x, float y, float z) { return new Vector3(x - Court.MID, z, Court.NET - y); }
        public static Vector2 FromWorld(Vector3 p) { return new Vector2(p.x + Court.MID, Court.NET - p.z); }

        static Shader std, spr;
        public static Material Lit(Color c, float smooth = .25f)
        {
            if (std == null) std = Shader.Find("Standard");
            var m = new Material(std); m.color = c; m.SetFloat("_Glossiness", smooth); return m;
        }
        // Saydam, iki yüzlü, köşe rengi çarpan (file, iz, gölge): Sprites/Default her yapıda bulunur.
        public static Material Clear(Color c, Texture tex = null)
        {
            if (spr == null) spr = Shader.Find("Sprites/Default");
            var m = new Material(spr); m.color = c; if (tex != null) m.mainTexture = tex; return m;
        }

        public static Texture2D RadialTex(int n = 64)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - r); a = a * a * (3 - 2 * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(); return t;
        }

        // File gözü: 1 px çizgili kare desen; mesh UV'si 4 cm'de bir tekrarlar.
        public static Texture2D NetTex()
        {
            const int n = 16; var t = new Texture2D(n, n, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 4;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    bool line = x == 0 || y == 0 || x == n - 1 || y == n - 1;
                    px[y * n + x] = line ? new Color32(255, 255, 255, 200) : new Color32(255, 255, 255, 0);
                }
            t.SetPixels32(px); t.Apply(true); return t;
        }

        public static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(t); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }

        public static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material m, bool shadows = false)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        public static Color Hex(string h) { Color c; return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.magenta; }
    }
}
