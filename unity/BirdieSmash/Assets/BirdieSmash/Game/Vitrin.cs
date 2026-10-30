// 360° görünüm (HTML turAc/turKare; yasin 2026-10-23: "avatarımı 360 görmek isterim, raketimi görmek isterim").
// HTML'de Blender'da 24 açıdan çizilmiş sprite sayfası boyanıyordu; Unity'de aynı FBX ayrı bir kamerayla RenderTexture'a
// çizilir (sahneden 500 m uzakta, kortu görmez). Davranış HTML ile aynı: parmakla sürükleyince döner, bırakınca
// yavaşlayarak durur (v ·0,08^dt), boştayken 18°/sn döner (hareketi azalt açıkken dönmez), ok düğmeleri 15° çevirir.
// "Raketi incele": 0,6 sn geçişle raket öne gelir ve kendi ekseninde 24°/sn döner; grip, kordaj ve çapraz tel rengi
// (RAK_RENK, tuytop_raket_gor) yalnız görünümdür. Yükseltmede o yuvanın parçası bir kez limona parlar (turParlat).
// 2026-10-26 karakter tasarımı (yasin: "yakın açıda görüp upgrade lenecekleri için"): yakın plan (odak) Yüz (baş + saç),
// Ayak (ayakkabılar), Tutuş (el + raket sapı). Hedef nokta her karede kemikten okunur (figür döndükçe izlenir), kamera
// üstel yumuşatmayla (~0,4 sn) kayar; hareketi azaltta anında geçer. Tutuşta kendi kendine dönme durur ve figür eli kameraya
// dönük açıya gelir: el eksenden uzakta, dönerken kamera onu izlemeye kalkarsa görüntü baş döndürür. Yüz ve Ayak eksende,
// orada dönme sürer (saçı ve ayakkabıyı 360° görmek işin kendisi). poz: tutuş görünümünde vuruş klibinin temas pozu
// (Forehand drive, Backhand bh_drive, File net, Servis serve). Deneme (deneTip/deneSac/deneAyk): alınmamış bir öğeyi
// vitrinde giydirir, kayda yazılmaz; Soyunma odasından çıkınca (Kapat) silinir.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Vitrin : MonoBehaviour
    {
        public static readonly Dictionary<string, string> RAK_RENK = new Dictionary<string, string> {
            {"beyaz","#f2f2ee"},{"siyah","#26262b"},{"kirmizi","#e23b3b"},{"limon","#d8ff5a"},{"mavi","#3b7be2"},{"turuncu","#ff8a2a"},{"pembe","#e8508f"},{"mor","#8a5ae2"}};
        public static readonly string[] RAK_SIRA = { "beyaz", "siyah", "kirmizi", "limon", "mavi", "turuncu", "pembe", "mor" };
        public static readonly Dictionary<string, string> RAK_AD = new Dictionary<string, string> {
            {"beyaz","Beyaz"},{"siyah","Siyah"},{"kirmizi","Kırmızı"},{"limon","Limon"},{"mavi","Mavi"},{"turuncu","Turuncu"},{"pembe","Pembe"},{"mor","Mor"}};
        static readonly Vector3 YER = new Vector3(500, 0, 0);
        public RenderTexture rt; Camera kam; PlayerView fig; Transform raketPivot; string anah;
        public float a, v, ra, rv, gec, hedefGec; public bool suruk; float parlaT = -1; int parlaK;
        public const int TAM = 0, YUZ = 1, AYAK = 2, TUTUS = 3;
        public int odak { get; private set; }
        public string poz { get; private set; }
        public string deneTip, deneSac, deneAyk, deneSapka, deneYuz;
        Transform bBas, bEl, bParmak; readonly List<Transform> bAyak = new List<Transform>(); Renderer bGrip, bTel, bSac, bGovde; Vector3 camS, hedefS; bool camVar;
        public PlayerView Fig { get { return fig; } }   // ekran turu tanısı (VaryantOzet)
        public Camera Kam { get { return kam; } }       // ekran turu: bölge rengi vitrin RT'sinden örneklenir (2026-10-27)

        public static Vitrin Build(Transform root)
        {
            var g = new GameObject("vitrin"); g.transform.SetParent(root, false); var vt = g.AddComponent<Vitrin>();
            // 2026-10-27 performans turu 2: dönen figürün silüeti MSAA'sız RT'de her karede merdivenleniyordu (18°/sn dönüş, kenar
            // pırpırı). Kalite basamağına göre 4×/2×/yok; çok örnekli tampon iOS'ta "memoryless" (yalnız çözülmüş görüntü bellekte:
            // 512×720×4 B×4 örnek ≈ 5,6 MB karo belleğinde kalır, RAM'e yazılmaz). Afiş de aynı.
            vt.rt = new RenderTexture(512, 720, 16) { name = "vitrin", antiAliasing = Kalite.Basamak == 2 ? 4 : Kalite.Basamak == 1 ? 2 : 1, memorylessMode = RenderTextureMemoryless.MSAA };
            var cg = new GameObject("vitrin_kamera"); cg.transform.SetParent(g.transform, false); vt.kam = cg.AddComponent<Camera>();
            vt.kam.targetTexture = vt.rt; vt.kam.clearFlags = CameraClearFlags.SolidColor; vt.kam.backgroundColor = Kit.Hex("#261A30");   // 2026-10-27 yön C: kart rengi (eski koyu camgöbeği #0e1d20 menüde tek soğuk leke kalıyordu)
            vt.kam.fieldOfView = 30; vt.kam.nearClipPlane = .05f; vt.kam.farClipPlane = 20; vt.kam.enabled = false;
            // dolgu ışığı: noktasal, kamera yanında (sahnenin tavan lambaları buraya ulaşmaz)
            var lg = new GameObject("vitrin_isik"); lg.transform.SetParent(g.transform, false); lg.transform.position = YER + new Vector3(-1.5f, 3, 3); var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 12; l.intensity = 1.6f;
            g.SetActive(false); return vt;
        }

        public void Ac() { gameObject.SetActive(true); kam.enabled = true; durgun = 0; Yenile(); }
        public void Kapat() { kam.enabled = false; gec = hedefGec = 0; parca = null; raKay = false; odak = TAM; poz = null; deneTip = deneSac = deneAyk = deneSapka = deneYuz = null; camVar = false; gameObject.SetActive(false); }
        // Yakın plan seç (aynı odağa ikinci dokunuş Soyunma odasında Tüm boy'a döner). Tutuşa girerken el kameraya döner.
        public void Odak(int o, string klip = null)
        {
            o = Mathf.Clamp(o, TAM, TUTUS); if (o != TAM) hedefGec = 0;
            if (o == TUTUS && odak != TUTUS) { a = TUTUS_ACI; v = 0; }
            odak = o; poz = o == TUTUS ? klip : null;
        }
        const float TUTUS_ACI = -35;   // sağ el (raket) kameraya dönük, gövde 3/4
        // Ekipman ya da raket görünümü değişince figürü yeniden boya (anahtar: görünümün özeti).
        public void Yenile()
        {
            var g = PlayerView.Benim();
            if (deneTip != null) g.tip = deneTip; if (deneSac != null) g.sacModel = deneSac; if (deneAyk != null) g.aykModel = deneAyk; if (deneSapka != null) g.sapka = deneSapka; if (deneYuz != null) g.yuz = deneYuz;
            string k = Ayar.model + g.Anahtar();
            if (k == anah && fig != null) return; anah = k;
            if (fig != null) Destroy(fig.gameObject); if (raketPivot != null) Destroy(raketPivot.gameObject);
            fig = PlayerView.Build(transform, Ayar.model, true, g.forma, false, g); fig.transform.position = YER;
            fig.ShowStatic(YER, 0, 0);
            // raket kopyası: rijit raket parçaları (deri ağı olmayan, etiketi raket kanalında) pivotun altına
            raketPivot = new GameObject("raket360").transform; raketPivot.SetParent(transform, false);
            // 2026-10-30 (yasin: "raketi incele dediğimizde raketin iskeleti gözükmüyor, sadece başı ve grip kısmı"): kopya
            // eskiden boyama kanalından (kanal[1]: cerceve/grip/tel/tel2) kuruluyordu; şaft ve boğaz (raket_koyu__koyu) kendi
            // FBX rengini koruduğu için o kanalda değil, incelemede raket havada kopuk bir kafa ile sap gibi kalıyordu. Artık
            // raketin bütün rijit parçaları (adı raket_ ile başlayan, deri ağı olmayan) alınır.
            var parts = new List<Renderer>();
            if (fig.Model != null) foreach (var r in fig.Model.GetComponentsInChildren<Renderer>(true))
                if (!(r is SkinnedMeshRenderer) && r.enabled && r.gameObject.name.StartsWith("raket_")) parts.Add(r);
            if (parts.Count == 0) foreach (var r in fig.kanal[1]) if (!(r is SkinnedMeshRenderer)) parts.Add(r);
            if (parts.Count > 0)
            {
                var b = parts[0].bounds; foreach (var r in parts) b.Encapsulate(r.bounds);
                Vector3 grip = b.center, head = b.center;
                foreach (var r in parts) { string t = PlayerView.Etiket(r.gameObject.name); if (t == "grip") grip = r.bounds.center; if (t == "tel") head = r.bounds.center; }
                raketPivot.position = YER + new Vector3(0, 1.1f, 0);
                var ic = new GameObject("ic").transform; ic.SetParent(raketPivot, false);
                foreach (var r in parts) { var c = Instantiate(r.gameObject, r.transform.position, r.transform.rotation, ic); c.name = r.gameObject.name; c.transform.localScale = r.transform.lossyScale; foreach (Transform ch in c.transform) Destroy(ch.gameObject); }
                // raketin ortası pivota, sap aşağı / kafa yukarı
                foreach (Transform ch in ic) ch.position += raketPivot.position - b.center;
                var ax = (head - grip).sqrMagnitude > 1e-6f ? (head - grip).normalized : Vector3.up; ic.localRotation = Quaternion.FromToRotation(ax, Vector3.up);
                float boy = Mathf.Max(b.size.x, b.size.y, b.size.z); raketBoy = boy;
                ParcaOlc(ic);
            }
            raketPivot.gameObject.SetActive(false);
            OdakKemikleri();
        }
        // Yakın plan noktaları: baş, ayaklar, sağ el kemiği, raket sapı ve kafası (PlayerView.Calibrate ile aynı ad kuralları).
        void OdakKemikleri()
        {
            bBas = null; bEl = null; bParmak = null; bAyak.Clear(); bGrip = null; bTel = null; bSac = null; bGovde = null; if (fig == null || fig.Model == null) return;
            bSac = fig.SacRenderer();
            foreach (var t in fig.Model.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant(); if (n.Contains("__")) continue;
                if (bBas == null && (n == "head" || n.EndsWith(" head"))) bBas = t;
                if (n.Contains("foot") && !n.Contains("step") && !n.Contains("ik") && !n.Contains("roll")) bAyak.Add(t);
                bool sag = n.EndsWith(".r") || n.EndsWith("_r") || n.Contains(" r ") || n.Contains("right");
                if (bEl == null && sag && n.Contains("hand") && !n.Contains("ik") && !n.Contains("slot")) bEl = t;
                if (bParmak == null && n == "middle_01_r") bParmak = t;
            }
            foreach (var r in fig.Model.GetComponentsInChildren<Renderer>(true)) if (bGovde == null && PlayerView.Etiket(r.gameObject.name) == "govde") bGovde = r;
            foreach (var r in fig.kanal[1])
            {
                if (r == null) continue; string e = PlayerView.Etiket(r.gameObject.name);
                if (bGrip == null && e == "grip") bGrip = r; if (bTel == null && e == "tel") bTel = r;
            }
        }
        // 2026-10-26 (r13 incelemesi): yakın planların uzaklığı kutunun gerçek en-boyuna göre. RT dikey (512×720) ama
        // Soyunma odasında yatay kutuda ScaleAndCrop ile gösteriliyor; görünen dikey açı kutu genişledikçe daralır (büyük yazıda
        // kutu %30 boy, daha da geniş). Eski sabit uzaklıkla Ayak planında yalnız çorap görünüyor, ayakkabı kesiliyordu.
        // kutuOran = kutu genişlik/yükseklik, kutuAlt = alttaki düğmelerin (Yüz·Ayak·Tutuş + oklar) kapladığı oran; ikisini
        // SoyunmaOdasi düzen değişince yazar. Hedef düğmelerin üstündeki boş alana doğru kaydırılır (yarısından biraz fazlası:
        // düğmeler yarı saydam, tamamen kaçmak nesneyi küçültür).
        public float kutuOran = 1.2f, kutuAlt = .35f;
        const float TAN15 = .26795f;   // fieldOfView 30 → yarım açı 15°
        void Gorus(out float gx, out float gy)
        {
            float ro = rt.width / (float)rt.height, ba = Mathf.Max(.2f, kutuOran);
            gx = TAN15 * Mathf.Min(ro, ba); gy = TAN15 * Mathf.Min(1, ro / ba);
        }
        // yariY/yariX: kadrajda kalması gereken yarı yükseklik/genişlik (metre). Dönen: uzaklık ve bakışın aşağı kayması.
        float Uzaklik(float yariY, float yariX, out float kayma)
        {
            float gx, gy; Gorus(out gx, out gy); float pay = Mathf.Clamp(kutuAlt, 0, .6f) * .6f;
            float d = Mathf.Max(yariY / (gy * (1 - pay)), yariX / gx); kayma = d * gy * pay; return d;
        }
        // Odağın kamera konumu ve bakış noktası (dünya). Boy oranlı: her beden tipinde aynı kadraj.
        void OdakPoz(out Vector3 cam, out Vector3 hedef)
        {
            float boy = bBas != null ? Mathf.Max(.8f, bBas.position.y - YER.y) : 1.8f; float kay;
            cam = YER + new Vector3(0, 1.05f, 6.2f); hedef = YER + new Vector3(0, .95f, 0);
            if (odak == YUZ && bBas != null)
            {   // baş + saç (afro, topuz kesilmesin): yarı yükseklik ~16 cm
                var h = bBas.position + Vector3.up * .06f * boy; float yy = .09f * boy, yx = .08f * boy;
                // 2026-10-27 oyuncak oranı (~3,4 baş boyu): pencere boy oranlıydı, iri başta yüz ve saç kadrajdan taşıyordu.
                // Baş büyükse (baş kemiğinden ten tepesine > %14 boy; gerçek oranlı modellerde ~%10, kadraj aynen) pencere
                // başın kendisinden kurulur: çene (kemiğin 0,62·üst altı) ile tepe (+%12 saç payı) arası.
                float bu; if (IriBas(boy, out bu))
                {
                    float alt0 = bBas.position.y - .62f * bu, ust0 = bBas.position.y + 1.12f * bu;
                    h = new Vector3(bBas.position.x, (alt0 + ust0) * .5f, bBas.position.z); yy = (ust0 - alt0) * .5f; yx = .8f * bu;
                }
                // 2026-10-29 Tüycük: baş ve gövde tek yumurta; yüz (gözler) baş kemiğinin altında, boyun hizasında. Baş kemiğinden
                // kurulan pencere gözleri kesip yumurtanın tepesini gösteriyordu. Pencere model biriminden (baş kemiği dinlenmede
                // .78; tuycuk.py): gülüşün altı .54 ile perçem tepesi 1,10 arası.
                if (Tuycuk)
                {
                    float u = (bBas.position.y - YER.y) / .78f, alt0 = YER.y + .54f * u, ust0 = YER.y + 1.10f * u;
                    h = new Vector3(bBas.position.x, (alt0 + ust0) * .5f, bBas.position.z); yy = (ust0 - alt0) * .5f; yx = .30f * u;
                }
                // 2026-10-27 (r42 menu_karakter_sac_deneme): Afro denenince saçın tepesi kadrajın üstünden taşıyordu; sabit pencere
                // kısa saça göre kuruluydu, hacimli saç kesilince kısa kesimden ayırt edilmiyordu. Pencere açık saç parçasının
                // tepesini kapsayacak kadar yukarı büyür, alt kenar (yüz) yerinde kalır; kısa saçta eski kadraj birebir (yalnız büyür).
                if (bSac != null && bSac.gameObject.activeInHierarchy)
                {
                    var sb = bSac.bounds; float ust = sb.max.y + .015f * boy, alt = h.y - yy;   // alt kenar (yüz) yerinde kalır
                    if (ust > h.y + yy) { float yeniY = (ust - alt) * .5f; h.y = (ust + alt) * .5f; yy = Mathf.Max(yy, yeniY); }
                    // genişlik bilerek sabit: figür döndükçe dünya sınırı (AABB) yatayda değişir, kadraj nabız gibi atardı
                }
                float d = Uzaklik(yy, yx, out kay);
                cam = h + new Vector3(0, .05f, 1).normalized * d; hedef = h - Vector3.up * kay;
            }
            else if (odak == AYAK && bAyak.Count > 0)
            {   // r13: iki ayakkabı + biraz bilek, hafif önden-yandan ve yukarıdan (~25°); ayakkabı ortada. Eski kadraj (0,22 · 0,62
                // boy) kutuya kırpılınca 40 cm'lik bir pencere bırakıyor, çorap kutuyu dolduruyordu.
                Vector3 m = Vector3.zero; foreach (var t in bAyak) m += t.position; m /= bAyak.Count; m.y = YER.y + .06f * boy;
                // r14: .10/.15 boy hâlâ dardı (Oyuncak modelin ayakkabısı iri, ayaklar açık duruşta; öndeki ayak düğmelerin
                // altında kaldı). Pencere ~60 × 90 cm: iki ayakkabı ve bileğe kadar baldır.
                float d = Uzaklik(.17f * boy, .26f * boy, out kay);
                cam = m + new Vector3(.35f, .38f, .86f).normalized * d; hedef = m - Vector3.up * kay;
            }
            else if (odak == TUTUS && (bGrip != null || bEl != null)) TutusPoz(boy, out cam, out hedef);
        }
        bool Tuycuk { get { return fig != null && fig.PrefabAdi == "tuycuk"; } }
        // Baş kemiğinden ten tepesine yükseklik (gövde ağı sınırı; saç ayrı parça, dahil değil) boyun %14'ünden büyükse iri baş.
        bool IriBas(float boy, out float ust)
        {
            ust = bGovde != null && bBas != null ? bGovde.bounds.max.y - bBas.position.y : 0; return ust > .14f * boy;
        }
        // r13: forehand'de sap kameraya doğru uzanıyor, yüzün önünü kesiyor, beş parmak okunmuyordu. Kamera artık sapa dik
        // bir yönden bakar (sap kadrajı yatay keser, el ve parmaklar yandan görünür) ve o dik yönlerden, figürün önüne ve
        // dışına en çok bakan, başı ya da gövdeyi elin önüne (perde) ya da hemen arkasına (yüz arkada) almayan seçilir.
        // 72 aday, poz durağan olduğu için seçim karede değişmez; sürükleyince kamera yumuşatmayla kayar.
        void TutusPoz(float boy, out Vector3 cam, out Vector3 hedef)
        {
            var gc = bGrip != null && (bGrip.gameObject.activeInHierarchy || bEl == null) ? bGrip.bounds.center : bEl.position;
            hedef = bEl != null && bGrip != null ? Vector3.Lerp(gc, bEl.position, .35f) : gc;
            Vector3 u = bTel != null && bTel.gameObject.activeInHierarchy ? bTel.bounds.center - gc : bEl != null ? gc - bEl.position : Vector3.up;
            u = u.sqrMagnitude > 1e-6f ? u.normalized : Vector3.up;
            Vector3 e1 = Vector3.Cross(u, Vector3.up); if (e1.sqrMagnitude < 1e-4f) e1 = Vector3.Cross(u, Vector3.forward); e1.Normalize(); var e2 = Vector3.Cross(u, e1);
            Vector3 dis = hedef - YER; dis.y = 0; dis = dis.sqrMagnitude > 1e-4f ? dis.normalized : Vector3.right;
            var tercih = (Vector3.forward * .8f + dis * .6f + Vector3.up * .2f).normalized;
            // oyuncak oranında el 1,45× (avuç ~15 cm): boy oranlı pencere eli kesiyordu; avuç boyundan kurulur (gerçek oranlıda aynen)
            float yyT = .075f * boy, yxT = .1f * boy, ustT; if ((IriBas(boy, out ustT) || Tuycuk) && bParmak != null && bEl != null)   // Tüycük: eldiven Oyuncak eli kadar iri
            { float pl = (bParmak.position - bEl.position).magnitude; yyT = Mathf.Max(yyT, 1.25f * pl); yxT = Mathf.Max(yxT, 1.65f * pl); }
            float kay; float d = Uzaklik(yyT, yxT, out kay); float gx, gy; Gorus(out gx, out gy);
            Vector3 bas = bBas != null ? bBas.position + Vector3.up * .06f * boy : YER + Vector3.up * .93f * boy, govde = bas - Vector3.up * .3f * boy;
            Vector3 en = tercih; float enP = -99;
            // r14: tam dik halkaya ±20° eğik iki halka eklendi (sap yine kadrajı keser ama baştan kaçacak yer artar); eğik küçük ceza.
            for (int i = 0; i < 72; i++)
            {
                float th = (i % 24) * Mathf.PI * 2 / 24; int j = i / 24 - 1; var dir = e1 * Mathf.Cos(th) + e2 * Mathf.Sin(th);
                if (j != 0) dir = (dir * Mathf.Cos(.35f) + u * (Mathf.Sin(.35f) * j)).normalized;
                float p = Vector3.Dot(dir, tercih) - (j != 0 ? .12f : 0);
                p -= Engel(bas, hedef, dir, d, .14f * boy, gx, gy, 1) + Engel(govde, hedef, dir, d, .2f * boy, gx, gy, 0);
                if (dir.y < -.35f) p -= .5f; if ((hedef + dir * d).y < YER.y + .15f) p -= 1;
                if (p > enP) { enP = p; en = dir; }
            }
            cam = hedef + en * d; hedef -= Vector3.up * kay;
        }
        // Bir nokta kamera ile el arasında ve görüş çizgisine yakınsa perde (2); kadrajın içinde kalıyorsa (kadrajda: 1 ise)
        // 0,8 ceza. r14: forehand'de yüz elin yanında kadrajda kalıyordu; yalnız "tam arkada" bakmak yetmedi, baş kadrajın
        // herhangi bir yerinde olmasın. Gövde (forma) arka planda sorun değil, yalnız perde sayılır.
        static float Engel(Vector3 p, Vector3 hedef, Vector3 dir, float d, float yaricap, float gx, float gy, int kadrajda)
        {
            var v = p - hedef; float boyunca = Vector3.Dot(v, dir); float dik = (v - dir * boyunca).magnitude; float derin = d - boyunca;
            if (derin < .05f) return 0;
            if (boyunca > 0 && dik < yaricap * 1.3f) return 2;
            if (kadrajda == 1 && (dik - yaricap * .6f) / derin < Mathf.Max(gx, gy) * 1.1f) return .8f;
            return 0;
        }
        float raketBoy = .7f;

        // ---- Raket tezgâhı (2026-10-30, Dolap › Raket; öneri artifact "bölge kamerası + parlayan çerçeve") ----
        // Araştırma (Wilson Custom Racket Shop): raket bölgelere ayrılır, seçilen bölge büyük görünür. Parça seçilince kendi
        // dönüş durur, raket o parçayı en iyi gösteren açıya yumuşakça döner (kordaj tam karşıdan, grip ve şaft yandan, kafa şekli
        // hafif açıdan) ve kamera o bölgeye yaklaşır; parça bir kez limona parlar. Sürükleyerek döndürmek yine serbest.
        // Ölçüler kopya kurulurken pivot uzayında bir kez alınır (raket rijit). Hareketi azaltta geçiş anında, parlama yok.
        string parca; float raHedef; bool raKay, rakVar; Vector3 rakCamS, rakHedefS; string[] parlaEt;
        readonly Dictionary<string, Bounds> parcaB = new Dictionary<string, Bounds>(); Vector3 yuzN = Vector3.forward;
        public string Parca { get { return parca; } }
        static readonly Dictionary<string, string[]> PARCA_ET = new Dictionary<string, string[]> {
            { "kafa", new[] { "cerceve" } }, { "cerceve", new[] { "cerceve" } }, { "saft", new[] { "koyu" } }, { "grip", new[] { "grip" } }, { "kordaj", new[] { "tel", "tel2" } } };
        // parçanın en iyi göründüğü açı: yüz normaline göre (0 = kordaj tam karşıda)
        static readonly Dictionary<string, float> PARCA_ACI = new Dictionary<string, float> { { "kafa", 12 }, { "cerceve", 55 }, { "saft", 70 }, { "grip", 80 }, { "kordaj", 0 } };
        void ParcaOlc(Transform ic)
        {
            parcaB.Clear();
            foreach (var r in ic.GetComponentsInChildren<Renderer>())
            {
                string e = PlayerView.Etiket(r.gameObject.name); if (e == "") continue; var bw = r.bounds;
                var lb = new Bounds(raketPivot.InverseTransformPoint(bw.center), Vector3.zero);
                foreach (var k in new[] { -1, 1 }) foreach (var l in new[] { -1, 1 }) foreach (var m in new[] { -1, 1 }) lb.Encapsulate(raketPivot.InverseTransformPoint(bw.center + Vector3.Scale(bw.extents, new Vector3(k, l, m))));
                Bounds o; if (parcaB.TryGetValue(e, out o)) { o.Encapsulate(lb); parcaB[e] = o; } else parcaB[e] = lb;
            }
            // kordaj yüzü: tel ağının köşelerinin yatay (xz) dağılımında en dar yön (pivot uzayında; raket ekseni pivotun Y'si).
            // 2026-10-30: ilk sürüm mesh kutusunun en ince eksenini alıyordu; ekran turunda kordaj ve kafa yakın planı raketi
            // yandan (çizgi gibi), grip/şaft yakın planı yüzden gösterdi: o eksen yüzün içindeydi (90° kayma). Köşe dağılımı
            // FBX eksen ve ölçek kuralından bağımsız.
            double sxx = 0, szz = 0, sxz = 0, mx = 0, mz = 0; int n = 0; var pts = new List<Vector3>();
            foreach (var r in ic.GetComponentsInChildren<MeshFilter>())
            {
                string e = PlayerView.Etiket(r.gameObject.name); if ((e != "tel" && e != "tel2") || r.sharedMesh == null) continue;
                foreach (var v in r.sharedMesh.vertices) pts.Add(raketPivot.InverseTransformPoint(r.transform.TransformPoint(v)));
            }
            foreach (var q in pts) { mx += q.x; mz += q.z; n++; }
            if (n > 8)
            {
                mx /= n; mz /= n;
                foreach (var q in pts) { double dx = q.x - mx, dz = q.z - mz; sxx += dx * dx; szz += dz * dz; sxz += dx * dz; }
                // 2x2 kovaryansın küçük öz değerinin öz vektörü
                double tr = sxx + szz, det = sxx * szz - sxz * sxz, l = tr / 2 - System.Math.Sqrt(System.Math.Max(0, tr * tr / 4 - det));
                var nv = System.Math.Abs(sxz) > 1e-12 ? new Vector3((float)(l - szz), 0, (float)sxz) : (sxx < szz ? Vector3.right : Vector3.forward);
                if (nv.sqrMagnitude > 1e-12f) yuzN = nv.normalized;
            }
        }
        // 2026-10-30 r80: kopyalar "raket_tel__tel(Clone)" adını alıyordu, Etiket "tel(Clone)" döndü; hiçbir parça tanınmadı,
        // yüz normali varsayılan (ileri) kaldı, parça kadrajları da tüm rakete düştü (kafa/kordaj yandan, grip/şaft yüzden göründü).
        // Kopyanın adı aslınınki yapılır (yukarıda Instantiate).
        // Parça seç (null = tüm raket: eski 360° dönüş). parla: aynı parça yeniden seçilse de parlasın (şekil değişince).
        public void RaketParca(string p, bool parla = false)
        {
            if (p != null && !PARCA_ET.ContainsKey(p)) p = null; bool yeni = p != parca; parca = p;
            if (p != null && (yeni || parla))
            {
                float phi = Mathf.Atan2(yuzN.x, yuzN.z) * Mathf.Rad2Deg; raHedef = -phi + PARCA_ACI[p]; raKay = true; rv = 0;
                if (!Ayar.reduce) { parlaK = 1; parlaEt = PARCA_ET[p]; parlaT = 0; }
            }
            if (p == null && yeni) { raKay = false; }
            durgun = 0;
        }
        // Tezgâhta dokunulan parça (viewport 0..1, yarıçap viewport genişliği oranı): en yakın parça merkezi.
        public string ParcaYeri(Vector2 vp, float yaricap)
        {
            if (raketPivot == null || !raketPivot.gameObject.activeInHierarchy) return null; string en = null; float enD = yaricap * yaricap;
            foreach (var kv in new[] { new KeyValuePair<string, string>("grip", "grip"), new KeyValuePair<string, string>("koyu", "saft"), new KeyValuePair<string, string>("tel", "kordaj"), new KeyValuePair<string, string>("cerceve", "cerceve") })
            {
                Bounds b; if (!parcaB.TryGetValue(kv.Key, out b)) continue; var p = kam.WorldToViewportPoint(raketPivot.TransformPoint(b.center)); if (p.z < 0) continue;
                float dx = p.x - vp.x, dy = (p.y - vp.y) * kam.aspect, d = dx * dx + dy * dy; if (kv.Key == "cerceve") d *= 1.6f;   // çerçeve kordajla aynı merkezde: kordaj öne
                if (d < enD) { enD = d; en = kv.Value; }
            }
            return en;
        }
        // Tezgâh kamerası: parça yoksa eski kadraj (tüm raket), varsa parçanın kutusuna göre (alt düğme payı Uzaklik'ta).
        void ParcaPoz(out Vector3 cam, out Vector3 hedef)
        {
            hedef = raketPivot.position; cam = hedef + new Vector3(0, 0, raketBoy * 2.4f + .2f); Bounds b;
            // r81/r83: kafa, kordaj ve çerçeve tel kutusuyla (çerçeve kutusu şaftı da kapsadığı için kafa küçük kalıyordu); grip sap
            // kapağı ve şaftın başıyla (yalnız siyah bir çubuk görünüyordu)
            string et = parca == "kordaj" || parca == "kafa" || parca == "cerceve" ? "tel" : parca != null ? PARCA_ET[parca][0] : null;
            if (et == null || !parcaB.TryGetValue(et, out b)) return;
            var m = raketPivot.TransformPoint(b.center); var sz = b.size; float kay;
            // raket dikey: kutunun Y'si ekranda dikey; yatayda kalan en büyük eksen
            // r81 kadraj payı: kordaj ve kafa telden %40 geniş (çerçeve tüpü ve parıltı sığsın), şaft tamamı, grip boyunun %80'i
            // ve yanında yer (yalnız siyah bir çubuk görünüyordu)
            bool ince = parca == "grip" || parca == "saft";
            float yy = sz.y * .5f * (parca == "grip" ? 1.7f : parca == "saft" ? 1.05f : parca == "cerceve" ? 1.6f : 1.4f), yx = Mathf.Max(sz.x, sz.z) * .5f * 1.4f;
            if (ince) yx = Mathf.Max(yx, yy * .45f);
            float d = Uzaklik(Mathf.Max(.02f, yy), Mathf.Max(.02f, yx), out kay);
            cam = m + new Vector3(0, .06f, 1).normalized * d; hedef = m - Vector3.up * kay;
        }
        // Figürdeki raketin RT içindeki yeri (viewport 0..1; HTML TUR.rkE): Hud rakete dokunuşu (sürüklemeden) raket ekranına çevirir.
        public bool RaketYeri(out Vector2 vp)
        {
            vp = Vector2.zero; if (fig == null || gec > .01f) return false; bool var_ = false; Bounds b = new Bounds();
            foreach (var r in fig.kanal[1]) { if (r == null) continue; if (!var_) { b = r.bounds; var_ = true; } else b.Encapsulate(r.bounds); }
            if (!var_) return false; var p = kam.WorldToViewportPoint(b.center); if (p.z < 0) return false; vp = new Vector2(p.x, p.y); return true;
        }

        public void Parlat(string yuva)
        {
            if (Ayar.reduce) return; int kn; var map = new Dictionary<string, int> { { "raket", 1 }, { "tel", 1 }, { "grip", 1 }, { "bileklik", 3 }, { "bant", 2 }, { "forma", 0 } };
            if (!map.TryGetValue(yuva, out kn)) return; parlaK = kn; parlaT = 0;
        }

        // Raket görünümünde raket yüzü en açık açı; geçişte kamera figürden rakete kayar.
        void Update()
        {
            if (fig == null) return; float dt = Mathf.Min(.05f, Time.unscaledDeltaTime); bool rm = Ayar.reduce;
            if (!suruk) { if (Mathf.Abs(v) > 2) v *= Mathf.Pow(.08f, dt); else v = rm || hedefGec > 0 || odak == TUTUS ? 0 : 18; a += v * dt; }
            float gd = hedefGec - gec; gec = rm || Mathf.Abs(gd) < .002f ? hedefGec : gec + Mathf.Sign(gd) * Mathf.Min(Mathf.Abs(gd), dt / .6f);
            if (hedefGec == 1 && !suruk)
            {
                if (raKay) { rv = 0; float fark = Mathf.DeltaAngle(ra, raHedef); ra = rm ? raHedef : ra + fark * (1 - Mathf.Exp(-dt * 6)); if (Mathf.Abs(fark) < .3f) { ra = raHedef; raKay = false; } }
                else if (Mathf.Abs(rv) > 2) rv *= Mathf.Pow(.08f, dt); else rv = rm || parca != null ? 0 : 24;
                ra += rv * dt;
            }
            if (suruk) raKay = false;
            float hit; if (poz != null && odak == TUTUS) fig.ShowPoz(YER, a, poz, Match.CLIP_HIT.TryGetValue(poz, out hit) ? hit : .5f); else fig.ShowStatic(YER, a, 0);
            // geçiş: figür 0–60%'ta solar/kayar, raket 45–100%'te öne gelir
            float fK = Mathf.Clamp01(gec / .6f), rK = Mathf.Clamp01((gec - .45f) / .55f);
            fig.gameObject.SetActive(fK < .999f); raketPivot.gameObject.SetActive(rK > 0);
            raketPivot.rotation = Quaternion.Euler(0, ra, 0) * Quaternion.Euler(-12, 0, 0); raketPivot.localScale = Vector3.one * Mathf.Lerp(.6f, 1, rK);
            Vector3 oc, oh; OdakPoz(out oc, out oh);
            float yk = rm || !camVar ? 1 : 1 - Mathf.Exp(-dt * 7); camS = Vector3.Lerp(camS, oc, yk); hedefS = Vector3.Lerp(hedefS, oh, yk); camVar = true;
            Vector3 rc, rh; ParcaPoz(out rc, out rh); float ryk = rm || !rakVar ? 1 : 1 - Mathf.Exp(-dt * 7); rakCamS = Vector3.Lerp(rakCamS, rc, ryk); rakHedefS = Vector3.Lerp(rakHedefS, rh, ryk); rakVar = true;
            Vector3 figCam = camS, rakCam = rakCamS;
            var cp = Vector3.Lerp(figCam, rakCam, Mathf.SmoothStep(0, 1, gec)); var hedef = Vector3.Lerp(hedefS, rakHedefS, Mathf.SmoothStep(0, 1, gec));
            kam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(hedef - cp));
            if (parlaT >= 0)
            {
                parlaT += dt; float k = parlaT < .9f ? Mathf.Sin(parlaT / .9f * Mathf.PI) : 0; fig.Parlat(parlaK, k);
                if (parlaK == 1) foreach (var r in raketPivot.GetComponentsInChildren<Renderer>()) if (parlaEt == null || System.Array.IndexOf(parlaEt, PlayerView.Etiket(r.gameObject.name)) >= 0) foreach (var m in r.materials) { if (k > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.85f, 1, .35f) * k * 1.2f); } else m.SetColor("_EmissionColor", Color.black); }
                if (parlaT >= .9f) parlaT = -1;
            }
            // Durağan vitrin çizilmez (2026-10-27 performans turu 2): hareketi azaltta, Tutuş yakın planında ve sürükleme
            // bırakılıp yavaşlama bitince görüntü kare kare aynı; kamera yine de figürü her karede 4× MSAA RT'ye çiziyordu.
            // Figür, açılar, geçiş, poz, kamera ve parlama aynıysa iki kare sonra kamera kapanır (RT son görüntüyü tutar);
            // biri değişince aynı karede açılır.
            var kr = kam.transform.rotation;
            bool ayni = !raKay && fig == sonFig && a == sonA && ra == sonRa && gec == sonGec && ReferenceEquals(poz, sonPoz) && odak == sonOdak && cp == sonCp && kr == sonRot && parlaT < 0;
            if (ayni) durgun++; else { durgun = 0; sonFig = fig; sonA = a; sonRa = ra; sonGec = gec; sonPoz = poz; sonOdak = odak; sonCp = cp; sonRot = kr; }
            bool ciz = durgun < 2; if (kam.enabled != ciz) kam.enabled = ciz;
        }
        PlayerView sonFig; float sonA = float.NaN, sonRa, sonGec; string sonPoz; int sonOdak = -1, durgun; Vector3 sonCp; Quaternion sonRot;
    }
}
