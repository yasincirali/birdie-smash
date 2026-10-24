// 360° görünüm (HTML turAc/turKare; yasin 2026-10-23: "avatarımı 360 görmek isterim, raketimi görmek isterim").
// HTML'de Blender'da 24 açıdan çizilmiş sprite sayfası boyanıyordu; Unity'de aynı FBX ayrı bir kamerayla RenderTexture'a
// çizilir (sahneden 500 m uzakta, kortu görmez). Davranış HTML ile aynı: parmakla sürükleyince döner, bırakınca
// yavaşlayarak durur (v ·0,08^dt), boştayken 18°/sn döner (hareketi azalt açıkken dönmez), ok düğmeleri 15° çevirir.
// "Raketi incele": 0,6 sn geçişle raket öne gelir ve kendi ekseninde 24°/sn döner; grip, kordaj ve çapraz tel rengi
// (RAK_RENK, tuytop_raket_gor) yalnız görünümdür. Yükseltmede o yuvanın parçası bir kez limona parlar (turParlat).
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

        public static Vitrin Build(Transform root)
        {
            var g = new GameObject("vitrin"); g.transform.SetParent(root, false); var vt = g.AddComponent<Vitrin>();
            vt.rt = new RenderTexture(512, 720, 16) { name = "vitrin" };
            var cg = new GameObject("vitrin_kamera"); cg.transform.SetParent(g.transform, false); vt.kam = cg.AddComponent<Camera>();
            vt.kam.targetTexture = vt.rt; vt.kam.clearFlags = CameraClearFlags.SolidColor; vt.kam.backgroundColor = Kit.Hex("#0e1d20");
            vt.kam.fieldOfView = 30; vt.kam.nearClipPlane = .05f; vt.kam.farClipPlane = 20; vt.kam.enabled = false;
            // dolgu ışığı: noktasal, kamera yanında (sahnenin tavan lambaları buraya ulaşmaz)
            var lg = new GameObject("vitrin_isik"); lg.transform.SetParent(g.transform, false); lg.transform.position = YER + new Vector3(-1.5f, 3, 3); var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 12; l.intensity = 1.6f;
            g.SetActive(false); return vt;
        }

        public void Ac() { gameObject.SetActive(true); kam.enabled = true; Yenile(); }
        public void Kapat() { kam.enabled = false; gec = hedefGec = 0; gameObject.SetActive(false); }
        // Ekipman ya da raket görünümü değişince figürü yeniden boya (anahtar: görünümün özeti).
        public void Yenile()
        {
            var g = PlayerView.Benim(); string k = Ayar.model + g.forma + g.cerceve + g.bant + g.bilek + g.grip + g.tel + g.tel2;
            if (k == anah && fig != null) return; anah = k;
            if (fig != null) Destroy(fig.gameObject); if (raketPivot != null) Destroy(raketPivot.gameObject);
            fig = PlayerView.Build(transform, Ayar.model, true, g.forma, false, g); fig.transform.position = YER;
            fig.ShowStatic(YER, 0, 0);
            // raket kopyası: rijit raket parçaları (deri ağı olmayan, etiketi raket kanalında) pivotun altına
            raketPivot = new GameObject("raket360").transform; raketPivot.SetParent(transform, false);
            var parts = new List<Renderer>(); foreach (var r in fig.kanal[1]) if (!(r is SkinnedMeshRenderer)) parts.Add(r);
            if (parts.Count > 0)
            {
                var b = parts[0].bounds; foreach (var r in parts) b.Encapsulate(r.bounds);
                Vector3 grip = b.center, head = b.center;
                foreach (var r in parts) { string t = PlayerView.Etiket(r.gameObject.name); if (t == "grip") grip = r.bounds.center; if (t == "tel") head = r.bounds.center; }
                raketPivot.position = YER + new Vector3(0, 1.1f, 0);
                var ic = new GameObject("ic").transform; ic.SetParent(raketPivot, false);
                foreach (var r in parts) { var c = Instantiate(r.gameObject, r.transform.position, r.transform.rotation, ic); c.transform.localScale = r.transform.lossyScale; foreach (Transform ch in c.transform) Destroy(ch.gameObject); }
                // raketin ortası pivota, sap aşağı / kafa yukarı
                foreach (Transform ch in ic) ch.position += raketPivot.position - b.center;
                var ax = (head - grip).sqrMagnitude > 1e-6f ? (head - grip).normalized : Vector3.up; ic.localRotation = Quaternion.FromToRotation(ax, Vector3.up);
                float boy = Mathf.Max(b.size.x, b.size.y, b.size.z); raketBoy = boy;
            }
            raketPivot.gameObject.SetActive(false);
        }
        float raketBoy = .7f;

        public void Parlat(string yuva)
        {
            if (Ayar.reduce) return; int kn; var map = new Dictionary<string, int> { { "raket", 1 }, { "tel", 1 }, { "grip", 1 }, { "bileklik", 3 }, { "bant", 2 }, { "forma", 0 } };
            if (!map.TryGetValue(yuva, out kn)) return; parlaK = kn; parlaT = 0;
        }

        // Raket görünümünde raket yüzü en açık açı; geçişte kamera figürden rakete kayar.
        void Update()
        {
            if (fig == null) return; float dt = Mathf.Min(.05f, Time.unscaledDeltaTime); bool rm = Ayar.reduce;
            if (!suruk) { if (Mathf.Abs(v) > 2) v *= Mathf.Pow(.08f, dt); else v = rm || hedefGec > 0 ? 0 : 18; a += v * dt; }
            float gd = hedefGec - gec; gec = rm || Mathf.Abs(gd) < .002f ? hedefGec : gec + Mathf.Sign(gd) * Mathf.Min(Mathf.Abs(gd), dt / .6f);
            if (hedefGec == 1 && !suruk) { if (Mathf.Abs(rv) > 2) rv *= Mathf.Pow(.08f, dt); else rv = rm ? 0 : 24; ra += rv * dt; }
            fig.ShowStatic(YER, a, 0);
            // geçiş: figür 0–60%'ta solar/kayar, raket 45–100%'te öne gelir
            float fK = Mathf.Clamp01(gec / .6f), rK = Mathf.Clamp01((gec - .45f) / .55f);
            fig.gameObject.SetActive(fK < .999f); raketPivot.gameObject.SetActive(rK > 0);
            raketPivot.rotation = Quaternion.Euler(0, ra, 0) * Quaternion.Euler(-12, 0, 0); raketPivot.localScale = Vector3.one * Mathf.Lerp(.6f, 1, rK);
            Vector3 figCam = YER + new Vector3(0, 1.05f, 6.2f), rakCam = raketPivot.position + new Vector3(0, 0, raketBoy * 2.4f + .2f);
            var cp = Vector3.Lerp(figCam, rakCam, Mathf.SmoothStep(0, 1, gec)); var hedef = Vector3.Lerp(YER + new Vector3(0, .95f, 0), raketPivot.position, Mathf.SmoothStep(0, 1, gec));
            kam.transform.SetPositionAndRotation(cp, Quaternion.LookRotation(hedef - cp));
            if (parlaT >= 0)
            {
                parlaT += dt; float k = parlaT < .9f ? Mathf.Sin(parlaT / .9f * Mathf.PI) : 0; fig.Parlat(parlaK, k);
                if (parlaK == 1) foreach (var r in raketPivot.GetComponentsInChildren<Renderer>()) foreach (var m in r.materials) { if (k > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.85f, 1, .35f) * k * 1.2f); } else m.SetColor("_EmissionColor", Color.black); }
                if (parlaT >= .9f) parlaT = -1;
            }
        }
    }
}
