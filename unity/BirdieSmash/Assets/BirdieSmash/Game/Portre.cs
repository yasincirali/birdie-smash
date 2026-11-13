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
        const int BOY = 256, SINIR = 8, KBOY = 160, KSINIR = 80;
        static Transform kok; static Camera kam; static Light isik;
        static readonly Dictionary<string, RenderTexture> rt = new Dictionary<string, RenderTexture>();
        static readonly List<string> sira = new List<string>();
        // Dolap küçük resimleri (2026-10-31, sanat paketi 1): saç/şapka/yüz kartları yalnız adla anlatılıyordu ("Anime", "Ananas"
        // ne demek?). Her kart oyuncunun bugünkü görünüşüne o öğe takılmış bir baş portresi gösterir. Ayrı ve daha büyük bir
        // önbellek (17 saç + 8 şapka + 6 yüz sığsın, 160² ≈ 100 KB her biri, MSAA'sız): portrelerle birbirini atmasınlar.
        // 2026-11-12 (inceleme 17:05): tavan 40 → 80. Katalog 64 resimli karta çıktı (şapka 26, sırt 15, saç 11, yüz 6, kol 6),
        // sezon koleksiyonlarıyla 100'ü geçiyor; Şapka → Sırt → Şapka gezintisinde en eski 24 resim atılıp yeniden çiziliyordu
        // (her biri ana iş parçacığında FBX figürü, 45 ms aralıkla: 26 şapka ≈ 1,2 sn boş kutu). 80 × 100 KB ≈ 8 MB doku, oyun
        // boyunca kalır (Dolap kapanınca bırakan bir kanca yok); bugünkü katalog tamamen, iki büyük kategori birlikte sığar.
        // Disk önbelleği ve yalnız başı etkileyen dar anahtar ayrı iş, önce ekran turunda ölçüm.
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
            if (kat == Kozmetik.SAC) { g.sacModel = id; g.sapka = null; } else if (kat == Kozmetik.SAPKA) g.sapka = id; else if (kat == Kozmetik.YUZ) g.yuz = id; else if (kat == Kozmetik.SIRT) g.sirt = id; else if (kat == Kozmetik.KOL) g.kol = id; else if (kat == Kozmetik.BEL) g.bel = id;
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
            var g = DeneGorunum(kat, id); return Al(DeneAnahtar(kat, id, g), g, true, KBOY, krt, ksira, KSINIR, kat == Kozmetik.SIRT, kat == Kozmetik.KOL);
        }

        // Düşük bellek uyarısı (2026-11-13 inceleme 22:12, fikir #2): Cokme.DusukBellek yalnız UnloadUnusedAssets + GC
        // yapıyordu; bu statik sözlüklerdeki RT'ler onunla serbest kalmaz (~8 MB küçük resim). Uyarıda küçük resim önbelleği
        // en yeni KALAN karta iner: Dolap açıksa görünen kartlar en son çizilenlerdir, kutuları boşalmaz; geri kalanı Dolap
        // gezintisinde mevcut sıralı çizimle (kare başına bir kart) yeniden dolar. Portreler (8 × 256 KB) dokunulmaz: üst
        // çubuk avatarı ve Profil kartı aynı RT'yi gösterir, silinirse kutu boşalır.
        const int KALAN = 16;
        public static void Bosalt()
        {
            while (ksira.Count > KALAN) { var e = ksira[0]; ksira.RemoveAt(0); RenderTexture o; if (krt.TryGetValue(e, out o) && o != null) { o.Release(); Object.Destroy(o); } krt.Remove(e); }
        }

        static Texture Al(string anahtar, PlayerView.Gorunum g, bool ben) { return Al(anahtar, g, ben, BOY, rt, sira, SINIR); }
        static Texture Al(string anahtar, PlayerView.Gorunum g, bool ben, int boy, Dictionary<string, RenderTexture> on, List<string> sr, int sinir, bool arka = false, bool kol = false)
        {
            RenderTexture t; if (on.TryGetValue(anahtar, out t) && t != null && t.IsCreated()) return t;
            // D6 (2026-10-31): RT içeriği kaybolmuşsa (arka plandan dönüş, bellek baskısı) eski nesne yok edilmeden yerine yenisi
            // konuyordu; bellek sızıntısı. Önce eskisi bırakılır.
            if (t != null) { t.Release(); Object.Destroy(t); on.Remove(anahtar); }
            try { t = Ciz(g, ben, boy, arka, kol); } catch (System.Exception e) { Debug.LogWarning("Birdie Smash portre çizilemedi: " + e.Message); return null; }
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

        static float BasOrtaX(Transform fig, float bY, float ust, float yedek)
        {
            float mn = float.MaxValue, mx = float.MinValue; var m = new Mesh();
            try
            {
                foreach (var sk in fig.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!sk.enabled || RaketAlti(sk.transform, fig)) continue;
                    sk.BakeMesh(m, true); var M = Matrix4x4.TRS(sk.transform.position, sk.transform.rotation, Vector3.one); var vs = m.vertices;
                    for (int i = 0; i < vs.Length; i++) { var w = M.MultiplyPoint3x4(vs[i]); if (w.y < bY || w.y > ust + .05f) continue; if (w.x < mn) mn = w.x; if (w.x > mx) mx = w.x; }
                }
            }
            finally { Object.Destroy(m); }
            // akıl kontrolü: baş genişliği 0,1–0,8 m dışındaysa (kol başın üstünde vb.) ölçüm atılır
            return mn <= mx && mx - mn > .1f && mx - mn < .8f ? (mn + mx) * .5f : yedek;
        }
        static bool RaketAlti(Transform t, Transform kok) { for (; t != null && t != kok; t = t.parent) if (t.name.ToLowerInvariant().StartsWith("raket")) return true; return false; }
        // arka (2026-11-01 Sırt): pelerin küçük resmi arkadan üç çeyrek, baştan beline kadar (pelerin sırtta, yüzden görünmez).
        // kol (2026-11-12, inceleme 17:05): kalkan üst kolda, baş kadrajının dışında kalıyordu; altı Kol kartı aynı baş portresini
        // gösteriyordu ("aynı resim, farklı ad" satın almayı durdurur). Kol kartında kadraj kalkanın kendi sınırlarına oturur,
        // kamera figürün dönüşünden bağımsız olarak kalkanın bulunduğu yandan, üç çeyrek önden bakar. Kalkan bulunamazsa baş kadrajı.
        static RenderTexture Ciz(PlayerView.Gorunum g, bool ben, int boy, bool arka = false, bool kol = false)
        {
            Kur();
            var fig = PlayerView.Build(kok, Ayar.model, ben, g.forma, false, g);
            try
            {
                fig.ShowStatic(YER, arka ? 155 : ben ? 18 : -18, 0);   // üç çeyrek, kameraya dönük (yön 0 = kameraya)
                // gölge ve raket portreye girmez (r94: rakip portresinde raket sapı yüzün önünden geçiyordu)
                foreach (var r in fig.GetComponentsInChildren<Renderer>(true)) { if (r.gameObject.name == "golge" || r is TrailRenderer || RaketAlti(r.transform, fig.transform)) r.enabled = false; }
                Transform bas = null;
                foreach (var tr in fig.GetComponentsInChildren<Transform>(true))
                { string n = tr.name.ToLowerInvariant(); if (n.Contains("__")) continue; if (n == "head" || n.EndsWith(" head")) { bas = tr; break; } }
                float ust = float.MinValue; foreach (var r in fig.GetComponentsInChildren<Renderer>()) if (r.enabled && !r.gameObject.name.StartsWith("raket") && !(r is TrailRenderer)) ust = Mathf.Max(ust, r.bounds.max.y);
                if (ust == float.MinValue) ust = YER.y + 1.8f;
                Bounds kb = new Bounds(); bool kalkan = false;
                if (kol && fig.KolKok != null)
                    foreach (var r in fig.KolKok.GetComponentsInChildren<Renderer>()) if (r.enabled) { if (!kalkan) { kb = r.bounds; kalkan = true; } else kb.Encapsulate(r.bounds); }
                if (kalkan)
                {
                    var govde = new Vector3(bas != null ? bas.position.x : YER.x, kb.center.y, bas != null ? bas.position.z : YER.z);
                    var yan = kb.center - govde; yan.y = 0; yan = yan.sqrMagnitude > 1e-6f ? yan.normalized : Vector3.left;
                    var yon = (yan * .8f + Vector3.forward * .6f).normalized;
                    float r2 = Mathf.Max(kb.extents.magnitude * 1.5f, .12f), d2 = r2 / Mathf.Tan(kam.fieldOfView * .5f * Mathf.Deg2Rad);
                    kam.transform.position = kb.center + yon * d2 + Vector3.up * r2 * .15f; kam.transform.LookAt(kb.center);
                    isik.transform.position = kb.center + yon * .9f + new Vector3(0, .5f, 0); isik.enabled = true;
                }
                float bY = bas != null ? bas.position.y : ust - .35f; if (ust < bY + .1f) ust = bY + .3f;
                // kadraj: çene (baş kemiğinin biraz altı) ile tepe arası; tepe payı %8
                float alt = bY - (ust - bY) * (arka ? 2.6f : .35f), yari = (ust - alt) * .5f * 1.08f, orta = (ust + alt) * .5f;
                float d = yari / Mathf.Tan(kam.fieldOfView * .5f * Mathf.Deg2Rad);
                var hedef = new Vector3(bas != null ? bas.position.x : YER.x, orta, bas != null ? bas.position.z : YER.z);
                // 2026-11-13 (yasin: "buton içerisindeki resource'lar kaymasın"): kadraj baş kemiğine göre ortalanıyordu. Kemik boyunda,
                // yüzün gerisinde; figür üç çeyrek döndüğü için (ben +18°, rakip −18°) yüz ekranda yana kayıyordu: Profil'de oyuncunun
                // yüzü halkanın sağına, Kariyer'de rakibin yüzü soluna yaslanıp kesiliyordu. Yatay merkez artık başın görünen ağından
                // (kemiğin üstündeki köşeler: baş + saç) ölçülür; ölçülemezse eski kemik merkezi.
                if (!kalkan) hedef.x = BasOrtaX(fig.transform, bY, ust, hedef.x);
                if (!kalkan)
                {
                    kam.transform.position = hedef + new Vector3(0, yari * .12f, d); kam.transform.LookAt(hedef);
                    isik.transform.position = hedef + new Vector3(-.6f, .5f, .9f); isik.enabled = true;
                }
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
            finally { fig.gameObject.SetActive(false); fig.Sil(); }
        }
    }
}
