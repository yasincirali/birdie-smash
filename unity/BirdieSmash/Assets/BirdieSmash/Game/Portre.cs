// Karakter portresi (2026-10-31, sanat yönü ilk paketi; yasin "yap"): üst çubuktaki avatar, Profil kartı ve rakip
// avatarları genel bir insan simgesiydi; oyuncunun Dolap'ta kurduğu karakter hiçbir sayfada görünmüyordu. Burada aynı FBX
// sahneden uzakta (+600 m; Vitrin +500, Afiş −500) kurulur, baş kemiğinden saç/şapka tepesine dek kadrajlanır, ayrı bir
// kamerayla saydam zeminli küçük bir RT'ye BİR KEZ çizilir ve figür silinir. Anahtar (model + görünüm özeti) değişmedikçe
// aynı RT döner; her karede çizim yok (bütçe §7.3). Saydam zemin: avatarın forma/rakip rengi dairesi arkada kalır.
// En çok 8 portre bellekte (256² ≈ 256 KB her biri); fazlası en eskiden atılır.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public static class Portre
    {
        static readonly Vector3 YER = new Vector3(600, 0, 0);
        const int BOY = 256, SINIR = 8;
        static Transform kok; static Camera kam; static Light isik;
        static readonly Dictionary<string, RenderTexture> rt = new Dictionary<string, RenderTexture>();
        static readonly List<string> sira = new List<string>();

        public static Texture Benim() { var g = PlayerView.Benim(); return Al("ben|" + Ayar.model + g.Anahtar(), g, true); }
        public static Texture Rakip(Style st)
        {
            if (st == null) return null; var f = Kit.Hex(st.renk); var g = PlayerView.Rakip(st, f);
            return Al("rk|" + Ayar.model + st.isim + st.ad + g.Anahtar(), g, false);
        }

        static Texture Al(string anahtar, PlayerView.Gorunum g, bool ben)
        {
            RenderTexture t; if (rt.TryGetValue(anahtar, out t) && t != null && t.IsCreated()) return t;
            try { t = Ciz(g, ben); } catch (System.Exception e) { Debug.LogWarning("Birdie Smash portre çizilemedi: " + e.Message); return null; }
            if (t == null) return null;
            rt[anahtar] = t; sira.Remove(anahtar); sira.Add(anahtar);
            while (sira.Count > SINIR) { var e = sira[0]; sira.RemoveAt(0); RenderTexture o; if (rt.TryGetValue(e, out o) && o != null) { o.Release(); Object.Destroy(o); } rt.Remove(e); }
            return t;
        }

        static void Kur()
        {
            if (kok != null) return;
            var g = new GameObject("portre"); Object.DontDestroyOnLoad(g); kok = g.transform;
            var cg = new GameObject("portre_kamera"); cg.transform.SetParent(kok, false); kam = cg.AddComponent<Camera>();
            kam.clearFlags = CameraClearFlags.SolidColor; kam.backgroundColor = new Color(0, 0, 0, 0); kam.fieldOfView = 24;
            kam.nearClipPlane = .05f; kam.farClipPlane = 10; kam.enabled = false; kam.allowHDR = false; kam.allowMSAA = true;
            // önden-sol yumuşak anahtar ışık (Afiş'le aynı yön): yüz zeminden koyu kalmasın; menzil kısa, yalnız portre figürü
            var lg = new GameObject("portre_isik"); lg.transform.SetParent(kok, false); isik = lg.AddComponent<Light>();
            isik.type = LightType.Point; isik.range = 4; isik.intensity = 1.7f; isik.shadows = LightShadows.None; isik.renderMode = LightRenderMode.ForcePixel; isik.enabled = false;
        }

        static RenderTexture Ciz(PlayerView.Gorunum g, bool ben)
        {
            Kur();
            var fig = PlayerView.Build(kok, Ayar.model, ben, g.forma, false, g);
            try
            {
                fig.ShowStatic(YER, ben ? 18 : -18, 0);   // üç çeyrek, kameraya dönük (yön 0 = kameraya)
                foreach (var r in fig.GetComponentsInChildren<Renderer>(true)) if (r.gameObject.name == "golge") r.enabled = false;
                Transform bas = null;
                foreach (var tr in fig.GetComponentsInChildren<Transform>(true))
                { string n = tr.name.ToLowerInvariant(); if (n.Contains("__")) continue; if (n == "head" || n.EndsWith(" head")) { bas = tr; break; } }
                float ust = float.MinValue; foreach (var r in fig.GetComponentsInChildren<Renderer>()) if (r.enabled && !r.gameObject.name.StartsWith("raket") && !(r is TrailRenderer)) ust = Mathf.Max(ust, r.bounds.max.y);
                if (ust == float.MinValue) ust = YER.y + 1.8f;
                float bY = bas != null ? bas.position.y : ust - .35f; if (ust < bY + .1f) ust = bY + .3f;
                // kadraj: çene (baş kemiğinin biraz altı) ile tepe arası; tepe payı %8
                float alt = bY - (ust - bY) * .35f, yari = (ust - alt) * .5f * 1.08f, orta = (ust + alt) * .5f;
                float d = yari / Mathf.Tan(kam.fieldOfView * .5f * Mathf.Deg2Rad);
                var hedef = new Vector3(bas != null ? bas.position.x : YER.x, orta, bas != null ? bas.position.z : YER.z);
                kam.transform.position = hedef + new Vector3(0, yari * .12f, d); kam.transform.LookAt(hedef);
                isik.transform.position = hedef + new Vector3(-.6f, .5f, .9f); isik.enabled = true;
                var t = new RenderTexture(BOY, BOY, 16, RenderTextureFormat.ARGB32) { name = "portre", antiAliasing = 4 };
                kam.targetTexture = t; kam.Render(); kam.targetTexture = null; isik.enabled = false;
                return t;
            }
            finally { fig.gameObject.SetActive(false); Object.Destroy(fig.gameObject); }
        }
    }
}
