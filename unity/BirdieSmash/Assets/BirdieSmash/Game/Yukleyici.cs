// Bekleme göstergesi: kendi ekseni etrafında dönen tüy top (HTML tuyDon / #perde .pr-top; yasin 2026-10-23: "uygulama
// içi loading gerekenler için de kendi etrafında dönen tüy topu kullanalım ... ekseni etrafında").
// HTML'de Blender'da çizilmiş 16 karelik eksen dönüşü vardı; Unity'de maçtaki 3B topun aynı ağı (ShuttleView.Feathers/
// Cork/Lines) ayrı bir kamerayla RenderTexture'a çizilir (sahneden 500 m uzakta, kortu görmez). Logo pozu: mantar sağ
// altta (44°), etek sol üstte; eksen dönüşü 2,4 rad/sn (HTML TS_HIZ: daha hızlısı göze geri dönüyor gibi görünüyordu),
// üstüne düzlem içi ±9° salınım (1,2 sn; 2026-10-30 kalktı: top yerinde durur). Hareketi azalt: dönme yok, tek poz. Kamera yalnız görünürken açık.
// 2026-10-27 perde v2 ve açılış anı (Hud): aynı top uçarken de kullanılır. Hud her kare Poz(yön, dönüş) yazar: ekrandaki
// yön (mantar o yöne bakar; uçan top mantarı önde uçar) ve eksen dönüşü (gerçek tüy top uçarken kendi ekseninde döner).
// Poz yazılmayan karede bekleme dönüşüne döner. Hareketi azaltta dönüş yine yok.
using UnityEngine;

namespace BirdieSmash.Game
{
    public class Yukleyici : MonoBehaviour
    {
        // 2026-10-30: Afis de (-500, 0, 0)'daydı; yükleyici kamerası afişin parçasını topun yanında çiziyordu (yasin: "top cismi
        // kesik, tam ve net gözükmüyor"). Ayrı yer.
        static readonly Vector3 YER = new Vector3(-620, 0, 0);
        // 2026-10-30 (yasin: "maç içi değil, daha estetik; sanatsal ve göz doyurucu diye tasarlamıştık ilk halini"): açılış ve
        // perdedeki top yeniden HTML'deki çizilmiş tüy top (Blender, beyaz kontur, fıstık yeşili bant, yarı saydam tüyler;
        // HTML TUY_SPIN şeridi 16 × 300×299, burada 4×4 ızgara Resources/ui/tuy_don). Varsa Hud onu çizer ve 3B kamera hiç
        // açılmaz; yoksa (eski kopya) 3B RT yedeği. Faz hesabı (dönüş, poz, hareketi azalt) aynı.
        public Texture2D serit; public float Faz { get { return faz; } }
        public RenderTexture rt; Camera kam; Transform eksen, top; float faz; bool acik;
        float pozAci, pozDonus; int pozKare = -10;
        // Ekran turu (EkranTuru) için: >= 0 iken dönüş fazı sabit (rad). CI kare hızı düşük; akıcı GIF için faz kare kare yazılır.
        public static float TestFaz = -1;
        public void Poz(float aciDerece, float donusRadSn) { pozAci = aciDerece; pozDonus = donusRadSn; pozKare = Time.frameCount; }

        public static Yukleyici Build(Transform root)
        {
            var g = new GameObject("yukleyici"); g.transform.SetParent(root, false); var y = g.AddComponent<Yukleyici>();
            // 512² + 4× MSAA (256² ve kenar yumuşatmasız: tam ekran perdenin %30'una büyütülünce bulanık ve basamaklıydı).
            y.rt = new RenderTexture(512, 512, 16) { name = "yukleyici", antiAliasing = 4 };
            var cg = new GameObject("yukleyici_kamera"); cg.transform.SetParent(g.transform, false); y.kam = cg.AddComponent<Camera>();
            y.kam.targetTexture = y.rt; y.kam.clearFlags = CameraClearFlags.SolidColor; y.kam.backgroundColor = new Color(0, 0, 0, 0);
            // Kadraj: top (boy L = .21, etek çapı .2) her açıda kareye sığsın. .62 m'de yarım yükseklik .14 idi, köşegen pozda
            // etek kenardan kesiliyordu; .82 m'de .19 (topun en geniş yarı izdüşümü ~.15 + pay).
            y.kam.fieldOfView = 26; y.kam.nearClipPlane = .05f; y.kam.farClipPlane = 3; y.kam.enabled = false;
            cg.transform.SetPositionAndRotation(YER + new Vector3(0, 0, -.82f), Quaternion.identity);
            var lg = new GameObject("yukleyici_isik"); lg.transform.SetParent(g.transform, false); lg.transform.position = YER + new Vector3(-.5f, .6f, -.8f);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 4; l.intensity = 1.4f;
            y.eksen = new GameObject("eksen").transform; y.eksen.SetParent(g.transform, false); y.eksen.position = YER;
            y.top = new GameObject("top").transform; y.top.SetParent(y.eksen, false);
            var tuy = Kit.Lit(new Color(.97f, .97f, .955f), .12f); var ic = Kit.Lit(new Color(.74f, .75f, .76f), .05f); var sap = Kit.Lit(new Color(.98f, .97f, .93f), .2f);
            var mantar = Kit.Lit(Kit.Hex("#f1dcb4"), .35f); var serit = Kit.Lit(new Color(.2f, .27f, .5f), .3f);
            var go = Kit.MeshObj("tuyler", y.top, ShuttleView.Feathers(), tuy); go.GetComponent<MeshRenderer>().sharedMaterials = new[] { tuy, ic, sap };
            var co = Kit.MeshObj("mantar", y.top, ShuttleView.Cork(), mantar); co.GetComponent<MeshRenderer>().sharedMaterials = new[] { mantar, serit };
            Kit.MeshObj("cizgiler", y.top, ShuttleView.Lines(), Kit.Unlit(new Color(.33f, .37f, .40f)));
            // topun ortası pivota: ağ mantardan (z=0) eteğe (−L) uzanır
            // Ağırlık merkezi değil, görünen kütlenin ortası pivota: mantar ucundan (+Rc) etek ağzına (−L) orta nokta. Eksen
            // dönüşü bu noktanın çevresinde: top yerinde döner, kayıyormuş gibi görünmez.
            y.top.localPosition = new Vector3(0, 0, (ShuttleView.L - ShuttleView.Rc) * .5f);
            y.serit = Resources.Load<Texture2D>("ui/tuy_don");
            g.SetActive(false); return y;
        }

        public void Goster(bool on)
        {
            if (on == acik) return; acik = on; gameObject.SetActive(on); kam.enabled = on && serit == null;
        }

        void Update()
        {
            if (!acik) return; float dt = Mathf.Min(.05f, Time.unscaledDeltaTime); bool rm = Ayar.reduce;
            bool poz = Time.frameCount - pozKare <= 1;   // Hud.Update bu Update'ten önce ya da sonra koşabilir: bir kare tolerans
            if (TestFaz >= 0) faz = TestFaz; else if (!rm) faz += dt * (poz ? pozDonus : 2.4f);
            // 2026-10-30: bekleme pozunda düzlem içi ±9° salınım kalktı (yasin: top "tam durmalı", yalnız kendi ekseninde dönmeli).
            float aci = poz ? pozAci : -44;   // ekranda mantar sağ alta bakar
            var yon = new Vector3(Mathf.Cos(aci * Mathf.Deg2Rad), Mathf.Sin(aci * Mathf.Deg2Rad), -.35f);   // biraz kameraya dönük: 3B okunsun
            eksen.rotation = Quaternion.LookRotation(yon.normalized, Vector3.forward * -1);
            top.localRotation = Quaternion.AngleAxis(faz * Mathf.Rad2Deg, Vector3.forward);
        }
    }
}
