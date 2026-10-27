// Oyna sekmesinin "VS" afişi (2026-10-26, arayüz yönü A "Yayın Gecesi", faz 1; yasin yönü henüz onaylamadı, kadraj bu
// dosyanın başındaki sabitlerle ayarlanır). Kahraman alanı: senin oyuncun solda büyük ve 3/4 önden, sıradaki rakip sağda
// daha geride, mercan kontur ışığıyla; aralarında "VS" yazısı UI'de (UiKok.ZeminKur). Neden ayrı kamera ve RenderTexture,
// ana kameranın menü kadrajı değil: maçtaki oyuncular fileye (birbirine) bakar, kameraya bakan iki figür için ikisini
// ayrı, kameraya dönük kurmak gerekir; ana sahneyi (kort, girdi ışınları, hakemler) değiştirmeden. Vitrin ile aynı yol:
// aynı FBX, sahneden uzakta (−500 m; Vitrin +500 m), saydam zeminli RT, UI Toolkit Image gösterir. Arkada ana kameranın
// salon görüntüsü kalır (zemin gradyanı karartır), bu yüzden afişte zemin yok.
// Yalnız Oyna sekmesi görünürken çizer (UiKok.Uygula; Vitrin gibi bütçe §7.3). Oyuncular boşta duruş klibinde nefes alır
// (idle, yavaş); hareketi azaltta durağan. Raket sağ elde, aynalama yok (CLAUDE.md): rakip ayna değil, kendi tarafına
// dönük kurulur. Gölge diski kapalı (zemin yok, havada koyu leke olurdu).
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Afis : MonoBehaviour
    {
        static readonly Vector3 YER = new Vector3(-500, 0, 0);
        // Kadraj (metre, YER'e göre). Kamera +z'de, −z'ye bakar; yön 0 = kameraya dönük. Ben: sol önde, sağa (rakibe) 28°
        // dönük; rakip: sağda 1,7 m geride, sola 32° dönük. Kahraman alanı ~375 × 440 pt, dikey açı 30°.
        // r16: kamera 3,35 m'de iken baş ve raket kadraj dışındaydı (maket ph0'da baş üstte boşlukla görünür); 4,4 m ve
        // daha alçak bakış: senin oyuncun başından dizine, ayaklar plakanın arkasında.
        public static Vector3 BEN = new Vector3(-.5f, 0, 0), RAKIP = new Vector3(.92f, 0, -1.7f);
        public static Vector3 KAMERA = new Vector3(.1f, 1.1f, 4.4f), BAKIS = new Vector3(.12f, 1.02f, -.5f);
        public static float BEN_YON = 28, RAKIP_YON = -32, ACI = 30;
        // r17: −z'ye bakan kameranın ekran sağı dünya −x'tir; sabitler ekran gözüyle yazıldığı için (sol = ben) dünya
        // konumuna Ek() ile çevrilir (x ve yön işareti döner). r16–r17'de sen sağda, rakip solda çıkıyordu.
        static Vector3 Ek(Vector3 v) { return new Vector3(-v.x, v.y, v.z); }
        public RenderTexture rt; Camera kam; PlayerView ben, rakip; string benAnah, rakipAnah; float ph;
        Light konturRakip, konturBen;

        public static Afis Build(Transform root)
        {
            var g = new GameObject("afis"); g.transform.SetParent(root, false); var a = g.AddComponent<Afis>();
            a.rt = new RenderTexture(640, 748, 16, RenderTextureFormat.ARGB32) { name = "afis", antiAliasing = Kalite.Basamak > 0 ? 2 : 1, memorylessMode = RenderTextureMemoryless.MSAA };   // çok örnekli tampon karo belleğinde (Vitrin notu)
            var cg = new GameObject("afis_kamera"); cg.transform.SetParent(g.transform, false); a.kam = cg.AddComponent<Camera>();
            a.kam.targetTexture = a.rt; a.kam.clearFlags = CameraClearFlags.SolidColor; a.kam.backgroundColor = new Color(0, 0, 0, 0);
            a.kam.fieldOfView = ACI; a.kam.nearClipPlane = .1f; a.kam.farClipPlane = 15; a.kam.enabled = false; a.kam.allowHDR = false;
            cg.transform.position = YER + Ek(KAMERA); cg.transform.rotation = Quaternion.LookRotation(Ek(BAKIS) - Ek(KAMERA));
            // Işıklar: önden sol yumuşak anahtar (salon tavan lambaları buraya ulaşmaz), arkadan kontur: rakibe mercan (rakip
            // rengi, a-rival drop-shadow), sana hafif limon (senin taraf). Menzil kısa: yalnız kendi figürünü boyar.
            // r16: tam arkadan kontur Standard gölgelendiricide görünmüyordu (Fresnel kenar ışığı yok, yalnız sırtı boyar);
            // dış yana ve hafif geriye alındı, güç arttı: kameraya bakan kenar mercan/limon parlar.
            Isik(g.transform, "afis_anahtar", YER + Ek(new Vector3(-2.2f, 2.6f, 3.2f)), Color.white, 1.25f, 10);
            // 2026-10-27 yön C: rakip konturu orkide (UiKok.RAKIP; mercan birincil eylemin turuncu ailesine düşüyordu), sana limon
            // (UiKok.LIMON, C'nin kırılmış limonu). Güçler aynı: orkide mercandan biraz açık, kontur yine okunur.
            a.konturRakip = Isik(g.transform, "afis_kontur_rakip", YER + Ek(RAKIP + new Vector3(1.25f, 1.45f, -.35f)), UiKok.RAKIP, 7f, 2.6f);
            a.konturBen = Isik(g.transform, "afis_kontur_ben", YER + Ek(BEN + new Vector3(-1.2f, 1.5f, -.35f)), UiKok.LIMON, 2.6f, 2.4f);
            g.SetActive(false); return a;
        }
        static Light Isik(Transform p, string ad, Vector3 yer, Color c, float guc, float menzil)
        {
            var lg = new GameObject(ad); lg.transform.SetParent(p, false); lg.transform.position = yer; var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.intensity = guc; l.range = menzil; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
            return l;
        }

        public bool Acik { get { return gameObject.activeSelf; } }
        // rakipSt null: rakip yok (ilk açılış: önce antrenman); afişte yalnız sen.
        public void Ac(Style rakipSt, Color rakipForma)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true); kam.enabled = true; Yenile(rakipSt, rakipForma);
        }
        public void Kapat() { if (kam != null) kam.enabled = false; gameObject.SetActive(false); }

        // Görünüm ya da rakip değişince figürü yeniden kur (anahtar: model + görünüm özeti; rakipte stil adı + forma).
        public void Yenile(Style rakipSt, Color rakipForma)
        {
            var gor = PlayerView.Benim(); string k = Ayar.model + gor.Anahtar();
            if (ben == null || k != benAnah)
            {
                if (ben != null) Destroy(ben.gameObject);
                ben = PlayerView.Build(transform, Ayar.model, true, gor.forma, false, gor); benAnah = k; GolgeKapat(ben);
            }
            string rk = rakipSt == null ? "" : Ayar.model + rakipSt.isim + rakipSt.ad + rakipForma;
            if (rk != rakipAnah)
            {
                if (rakip != null) { Destroy(rakip.gameObject); rakip = null; }
                if (rakipSt != null) { rakip = PlayerView.Build(transform, Ayar.model, false, rakipForma, false, PlayerView.Rakip(rakipSt, rakipForma)); GolgeKapat(rakip); }
                rakipAnah = rk;
            }
            konturRakip.enabled = rakip != null;
            Goster(0);
        }
        static void GolgeKapat(PlayerView p) { var g = p.transform.Find("golge"); if (g != null) g.gameObject.SetActive(false); }

        void Goster(float dt)
        {
            if (!Ayar.reduce) ph += dt * .45f;   // boşta duruş yavaş: nefes (yön A "oyuncu nefes alır")
            if (ben != null) ben.ShowPoz(YER + Ek(BEN), -BEN_YON, "idle", ph);
            if (rakip != null) rakip.ShowPoz(YER + Ek(RAKIP), -RAKIP_YON, "idle", ph + .37f);
        }
        void Update() { Goster(Mathf.Min(.05f, Time.unscaledDeltaTime)); }
    }
}
