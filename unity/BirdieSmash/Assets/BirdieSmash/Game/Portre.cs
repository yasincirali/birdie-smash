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
        const int BOY = 256, SINIR = 8, KBOY = 160, KSINIR = 40;
        static Transform kok; static Camera kam; static Light isik;
        static readonly Dictionary<string, RenderTexture> rt = new Dictionary<string, RenderTexture>();
        static readonly List<string> sira = new List<string>();
        // Dolap küçük resimleri (2026-10-31, sanat paketi 1): saç/şapka/yüz kartları yalnız adla anlatılıyordu ("Anime", "Ananas"
        // ne demek?). Her kart oyuncunun bugünkü görünüşüne o öğe takılmış bir baş portresi gösterir. Ayrı ve daha büyük bir
        // önbellek (17 saç + 8 şapka + 6 yüz sığsın, 160² ≈ 100 KB her biri, MSAA'sız): portrelerle birbirini atmasınlar.
        static readonly Dictionary<string, RenderTexture> krt = new Dictionary<string, RenderTexture>();
        static readonly List<string> ksira = new List<string>();

        public static Texture Benim() { var g = PlayerView.Benim(); return Al("ben|" + Ayar.model + g.Anahtar(), g, true); }
        public static Texture Rakip(Style st)
        {
            if (st == null) return null; var f = Kit.Hex(st.renk); var g = PlayerView.Rakip(st, f);
            return Al("rk|" + Ayar.model + st.isim + st.ad + g.Anahtar(), g, false);
        }

        // Kozmetik.SAC / SAPKA / YUZ öğesi takılı bugünkü görünüş. Saç küçük resminde şapka çıkarılır (saç görünsün).
        static PlayerView.Gorunum DeneGorunum(string kat, string id)
        {
            var g = PlayerView.Benim();
            if (kat == Kozmetik.SAC) { g.sacModel = id; g.sapka = null; } else if (kat == Kozmetik.SAPKA) g.sapka = id; else if (kat == Kozmetik.YUZ) g.yuz = id;
            return g;
        }
        static string DeneAnahtar(string kat, string id, PlayerView.Gorunum g) { return "kc|" + kat + "|" + id + "|" + Ayar.model + g.Anahtar(); }
        // Önbellekte varsa hemen döner (kart kurulurken çizim beklemesin); yoksa null: çağıran Dene'yi sırayla, kare kare çağırır.
        public static Texture DeneHazir(string kat, string id)
        {
            var g = DeneGorunum(kat, id); RenderTexture t; return krt.TryGetValue(DeneAnahtar(kat, id, g), out t) && t != null && t.IsCreated() ? t : null;
        }
        public static Texture Dene(string kat, string id)
        {
            var g = DeneGorunum(kat, id); return Al(DeneAnahtar(kat, id, g), g, true, KBOY, krt, ksira, KSINIR);
        }

        static Texture Al(string anahtar, PlayerView.Gorunum g, bool ben) { return Al(anahtar, g, ben, BOY, rt, sira, SINIR); }
        static Texture Al(string anahtar, PlayerView.Gorunum g, bool ben, int boy, Dictionary<string, RenderTexture> on, List<string> sr, int sinir)
        {
            RenderTexture t; if (on.TryGetValue(anahtar, out t) && t != null && t.IsCreated()) return t;
            // D6 (2026-10-31): RT içeriği kaybolmuşsa (arka plandan dönüş, bellek baskısı) eski nesne yok edilmeden yerine yenisi
            // konuyordu; bellek sızıntısı. Önce eskisi bırakılır.
            if (t != null) { t.Release(); Object.Destroy(t); on.Remove(anahtar); }
            try { t = Ciz(g, ben, boy); } catch (System.Exception e) { Debug.LogWarning("Birdie Smash portre çizilemedi: " + e.Message); return null; }
            if (t == null) return null;
            on[anahtar] = t; sr.Remove(anahtar); sr.Add(anahtar);
            while (sr.Count > sinir) { var e = sr[0]; sr.RemoveAt(0); RenderTexture o; if (on.TryGetValue(e, out o) && o != null) { o.Release(); Object.Destroy(o); } on.Remove(e); }
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

        static bool RaketAlti(Transform t, Transform kok) { for (; t != null && t != kok; t = t.parent) if (t.name.ToLowerInvariant().StartsWith("raket")) return true; return false; }
        static RenderTexture Ciz(PlayerView.Gorunum g, bool ben, int boy)
        {
            Kur();
            var fig = PlayerView.Build(kok, Ayar.model, ben, g.forma, false, g);
            try
            {
                fig.ShowStatic(YER, ben ? 18 : -18, 0);   // üç çeyrek, kameraya dönük (yön 0 = kameraya)
                // gölge ve raket portreye girmez (r94: rakip portresinde raket sapı yüzün önünden geçiyordu)
                foreach (var r in fig.GetComponentsInChildren<Renderer>(true)) { if (r.gameObject.name == "golge" || r is TrailRenderer || RaketAlti(r.transform, fig.transform)) r.enabled = false; }
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
                // 4× MSAA geçici yüzeye çizilir, MSAA'sız kalıcı dokuya çözülür: bellekte kalan yalnız boy² × 4 bayt
                // D6: Render istisna atarsa geçici yüzey de bırakılsın (finally)
                var gec = RenderTexture.GetTemporary(boy, boy, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
                try
                {
                    kam.targetTexture = gec; kam.Render();
                    var t = new RenderTexture(boy, boy, 0, RenderTextureFormat.ARGB32) { name = "portre" }; t.Create();
                    Graphics.Blit(gec, t); return t;
                }
                finally { kam.targetTexture = null; isik.enabled = false; RenderTexture.ReleaseTemporary(gec); }
            }
            finally { fig.gameObject.SetActive(false); Object.Destroy(fig.gameObject); }
        }
    }
}
