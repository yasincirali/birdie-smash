// Ekran üstü (IMGUI, paket gerektirmez; son ürün UI Toolkit'e taşınır, VoiceOver/TalkBack etiketi IMGUI'de yok: bilinen eksik).
// HTML ile aynı yüzeyler:
//   Menü: Hızlı maç / Kariyer kartı (basamak çubuğu), Oyna, kısayollar Ayarlar · Ekipman · Nasıl oynanır, rekor/galibiyet/görev.
//   Ekipman: 360° figür (sürükle, ‹ › 15°, "Raketi incele"), raket görünümü (grip, kordaj, desen, çapraz tel), seviye ve Tüy,
//   altı yuva yükseltmesi (önizleme çubukları: gri şimdi, yeşil yükseltince), "Eşit şartlar", modeller ve tel gerginliği,
//   istatistik satırı. Ayarlar (+ Unity: dil). Nasıl oynanır. Lisanslar.
//   Maç: skor kutusu (sayı alınca büyür), servis noktası, sol üst kutu (Ralli 3'ten sonra · 3 görev noktası), smaç gücü çubuğu
//   ("Sıçrayarak smaç hazır" turuncu nabız), servis şeridi (altta), vuruş halesi + etiket, hedef önizleme adı, parmak izi,
//   çizgi kamerası dairesi, tribün flaşları, ekran parlaması, antrenman kartı (hız bandı ve iğne, bilgi adımı çerçevesi,
//   antrenör çağrısı, iz), maç sonu (Tüy/XP ödülü, yükseltme hazır), duraklat, smaç perdesi.
// Dokunma alanı: her düğme en az 44/640 ekran yüksekliği (iPhone SE 667 pt dahil ≥ 44 pt); güvenli alan Screen.safeArea.
// Metinler L.T / L.Cumle ile çevrilir (Türkçe varsayılan).
// 2026-10-25 eşitlik turu (yasin: "html de yapılan her feature unity dönüşümünde de içermeli"): açılış perdesi (sesler
// hazırlanana kadar kapalı, dönen tüy top + "Hazırlanıyor…"), menü logosu, "Maça devam et", panel ve rakip kartı girişi
// (.22 sn; hareketi azaltta yok), sayı kıvılcımı, duraklatmada Ses, gizli kare hızı göstergesi (Ayarlar başlığına 3 sn içinde
// 5 dokunuş), 360°'de rakete dokununca raket görünümü, seçimlerde titreşim. Yazı boyutu: HTML'de yoktu (tarayıcı Dynamic
// Type'ı oyun kanvasına taşımıyordu); burada metin ölçeği fu = u · Ayar.YaziK (sistem: iOS tercih edilen boyut, ×0,9–1,3).
// Yerleşim u ile kalır: ×2'ye kadar ölçek sabit yükseklikli IMGUI satırlarını taşırır; tam Dynamic Type UI Toolkit'le (faz 6).
// 2026-10-26 (komple-oyun adım 1): menü, Ekipman (Soyunma odası), Ayarlar, Nasıl oynanır ve Lisanslar UI Toolkit'e taşındı
// (Game/UI). Burada kalanlar: maç içi HUD, duraklat, maç sonu, smaç perdesi, gizli kare hızı göstergesi (tasarım §7.2: HUD az
// öğeli, Adım 3'ten sonra taşınır). Yukarıdaki "Menü / Ekipman / Ayarlar" satırları o yüzeylerin tarihçesidir.
// Aynı turda düzeltilen hata: menüdeki Tik() kendini çağırıyordu (sonsuz özyineleme; ‹ › ve renk seçimi çöküyordu),
// UI'de sfx.Tik + titreşim olarak yazıldı.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Hud : MonoBehaviour
    {
        public GameRoot root; public bool ayrinti;
        GUIStyle big, mid, small, tiny, btn, btnP, chip, chipOn, card, left, leftB, btnD; float u, fu;
        class Float { public string t; public Vector3 w; public float a; public Color c; public float size; public bool sabit; }
        readonly List<Float> floats = new List<Float>(); readonly List<Rect> blocks = new List<Rect>();
        string ogrMsg; float ogrMsgT;
        // Biçim önbelleği (2026-10-26, 120 Hz önerisinin ön şartı "60'ta uzun kare sıfır"): OnGUI her karede onlarca
        // new GUIStyle üretiyordu; çöp toplayıcı ara ara uzun kare yapıyordu. Aynı (taban, boy, kalınlık) biçimi bir kez
        // kurulur. Dönen biçim paylaşılır: üstünde alan DEĞİŞTİRME (gerekirse yeni anahtar ekle).
        readonly Dictionary<long, GUIStyle> stilOn = new Dictionary<long, GUIStyle>(); readonly Dictionary<GUIStyle, int> stilNo = new Dictionary<GUIStyle, int>();
        // Kare başı ayırmasız ölçü ve yazı (2026-10-27 performans turu 2): OnGUI karede en az iki kez (Repaint + girdi) koşar;
        // her CalcSize/CalcHeight için yeni GUIContent, her skor/ralli için ToString ve L.T kalıbı (Regex) maçta karede ~40
        // küçük nesne ayırıyordu (GC artışı → ara ara takılma). Tek GUIContent yeniden kullanılır (her çağrı ölçüyü hemen
        // döndürür, saklamaz); 0–99 sayıları önceden yazılı; ralli yazısı ralli ya da dil değişince bir kez kurulur.
        static readonly GUIContent ic = new GUIContent();
        static GUIContent Ic(string t) { ic.text = t; ic.image = null; ic.tooltip = null; return ic; }
        static readonly string[] SAYI = new string[100];
        static string Sayi(int n) { if (n < 0 || n >= SAYI.Length) return n.ToString(); return SAYI[n] ?? (SAYI[n] = n.ToString()); }
        string ralliYazi; int ralliYaziN = -1; bool ralliYaziEn;
        string RalliYazi(int n) { if (n != ralliYaziN || L.En != ralliYaziEn || ralliYazi == null) { ralliYaziN = n; ralliYaziEn = L.En; ralliYazi = L.T("Ralli {0}", n); } return ralliYazi; }
        GUIStyle Stil(GUIStyle b, int boy, FontStyle fs = (FontStyle)(-1))
        {
            int no; if (!stilNo.TryGetValue(b, out no)) { no = stilNo.Count + 1; stilNo[b] = no; }
            long key = ((long)no << 32) | ((long)(boy + 1) << 4) | ((long)fs + 1 & 15); GUIStyle st;
            if (!stilOn.TryGetValue(key, out st)) { st = new GUIStyle(b); if (boy > 0) st.fontSize = boy; if ((int)fs >= 0) { st.fontStyle = fs; Agirlik(st); } stilOn[key] = st; }
            return st;
        }
        // Tek satıra sığdır (2026-10-26, r16: "THE OPPONENT GOT IT THIS TIME" iki satıra kırılıp skorun üstüne biniyordu;
        // "SIRADAKİ: İL ŞAMPİYONASI" düğmenin dairesine taşıyordu): boyu 2 px adımlarla küçült, en çok tabanın %55'ine; yine
        // sığmazsa kelime arasından kırılır (kelime ortası değil: GUIStyle wordWrap).
        GUIStyle Sigdir(GUIStyle b, string t, float w)
        {
            int boy = b.fontSize, alt = Mathf.Max(10, Mathf.RoundToInt(b.fontSize * .55f)); var st = b; var c = Ic(t);
            while (boy > alt && st.CalcSize(c).x > w) { boy -= 2; st = Stil(b, boy); }
            return st;
        }
        GUIStyle leftWrap, leftWrapTaban; // her karede yeni taban S() önbelleğini şişirirdi
        public void StilSifirla() { stilOn.Clear(); stilNo.Clear(); glifSt.Clear(); }
        float ralliPop; int sonRalli; float[] pop = new float[2]; float flash; Color flashC; readonly List<Vector3> cams = new List<Vector3>();
        class HawkV { public float x, y, t; public bool icerde, serve; public int cm; } HawkV hawk;
        Texture2D ring, ringK, ringKesik, glow, disk, bar; Texture2D[] perdeTex;
        // Maç göstergesi Tüycükler diline (2026-10-31 Ar-Ge UI/UX #1, yasin "yap"; defter Pnfx5sknsw9PuWQ2MB9fGY): skor kutusu,
        // sol üst ralli/görev kutusu ve smaç rayı keskin köşeli düz dikdörtgendi, menüler ve duraklat yuvarlak kartlarla
        // konuşurken maç başka oyun gibi duruyordu. Renkler ve anlam aynı kaldı: senin sayın forma rengin (Dolap seçimi, bilerek
        // limon değil), rakip orkide, görev 3 nokta; yalnız biçim yuvarlandı ve smaç rayı 8 → 12 birim kalınlaştı (maç hızında
        // ince şerit zor okunuyordu). Geri sayıma halka eklenmedi: rakam zaten büyüyüp oturuyor, ikinci hareket gürültü olurdu.
        // Dokular u değişince yeniden üretilir (IMGUI'de 1 doku pikseli = 1 ekran pikseli); eskisi yok edilir (sızıntı olmasın).
        GUIStyle hudKutu, hudRay; Texture2D hudKutuT, hudRayT;
        void HudKutular()
        {
            if (hudKutuT != null) Destroy(hudKutuT); if (hudRayT != null) Destroy(hudRayT);
            int r = Mathf.Max(3, Mathf.RoundToInt(8 * u));   // dilim r+2 ≤ en alçak kutunun (22·u) yarısı
            hudKutuT = Ikonlar.Kutu(r, new Color(8 / 255f, 16 / 255f, 17 / 255f, .7f), new Color(1, 1, 1, .1f), Mathf.Max(1, u * .75f));
            hudKutu = AKutu(hudKutuT, r + 2);
            int rr = Mathf.Max(2, Mathf.RoundToInt(6 * u) - 2);   // ray 12·u: dilim rr+2 = yarı yükseklik, iki uç tam yuvarlak
            hudRayT = Ikonlar.Kutu(rr, Color.white, new Color(1, 1, 1, 0), 0);
            hudRay = AKutu(hudRayT, rr + 2);
        }
        void Boya(Rect r, GUIStyle st, Color c) { if (Event.current.type != EventType.Repaint) return; var g = GUI.color; GUI.color = c; st.Draw(r, false, false, false, false); GUI.color = g; }
        // Yuvarlak duraklat düğmesi (kare "II" yazılı düğme yerine): koyu disk + ince halka + iki çubuk; ≥ 44 pt (BH).
        bool DuraklatDugme(Rect r)
        {
            blocks.Add(r); bool bas = r.Contains(Event.current.mousePosition) && Input.GetMouseButton(0);
            Tex(r, disk, new Color(8 / 255f, 16 / 255f, 17 / 255f, bas ? .85f : .7f)); Tex(r, ringK, new Color(1, 1, 1, .22f));
            float cw = r.width * .11f, chh = r.height * .36f, cx = r.center.x, cy = r.center.y - chh / 2;
            Fill(new Rect(cx - cw * 1.6f, cy, cw, chh), Color.white); Fill(new Rect(cx + cw * .6f, cy, cw, chh), Color.white);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }
        // perde: evre 0 yok, 1 kapanıyor, 2 kapalı, 3 açılıyor
        int perdeEvre; float perdeT; System.Action perdeIs; float perdeAci; float perdeMin = .15f; bool perdeAtla, perdeBek;
        public static readonly float[][] PERDE_F = { new[] { 2.6f }, new[] { .86f }, new[] { .66f }, new[] { .47f }, new[] { .30f } };
        public static readonly string[,] PERDE_C = { { "#a3234b", "#b52a52" }, { "#d84358", "#e85563" }, { "#f9665a", "#ff7a5c" }, { "#ffa04d", "#ffb35a" }, { "#f9d272", "#fde59a" } };

        // 2026-10-27 yön C (koordinatör: maç HUD'u da C'ye uysun): rakip vurgusu (skor, servis noktası, rakibin sayı yazısı ve
        // dalgası) mercan #ff7a6b yerine orkide; limon "sen" olarak kalır. Skor kutusunda (kort üstünde %70 koyu) orkide Lc −53,6
        // (büyük kalın rakam için ≥ 45 geçer; eski mercan −42 geçmiyordu), duvar üstünde −62,6. Kutlama kıvılcımlarının mercanı
        // turuncu oldu: rakip rengi kutlamada görünmesin.
        public const string RAKIP_HUD = "#E99BFF";
        public void Say(string t, Vector3 world, Color c, float size = 1, bool sabit = false) { if (string.IsNullOrEmpty(t)) return; floats.Add(new Float { t = t, w = world, c = c, size = size, sabit = sabit }); if (floats.Count > 6) floats.RemoveAt(0); }
        public void Ogr(string t) { ogrMsg = t; ogrMsgT = 1.8f; }
        public bool Blocks(Vector2 guiPos) { if (perdeEvre > 0) return true; foreach (var r in blocks) if (r.Contains(guiPos)) return true; return false; }
        public void MacBasi() { pop[0] = pop[1] = 0; cams.Clear(); hawk = null; flash = 0; floats.Clear(); }
        public void Pop(int i) { pop[i] = 1; }
        public void Flash(float a, Color c) { if (Ayar.reduce) a *= .4f; if (a > flash) { flash = a; flashC = c; } }
        public void Hawk(float x, float y, bool icerde, int cm, bool serve) { hawk = new HawkV { x = x, y = y, icerde = icerde, cm = cm, serve = serve }; }
        // Tribün flaşları (HTML M.cams): sayı kazanınca 26 küçük flaş ekranın üst bandında, .5 sn.
        public void Kamera() { for (int k = 0; k < 26; k++) cams.Add(new Vector3(Random.value, 8 + Random.value * 48, -Random.value * .6f)); }
        float konfetiT = -1; public void Konfeti() { konfetiT = 0; }
        bool bosalt; float hazirT = -1; const int AKICI_KARE = 8; const float AKICI_AZAMI = 2f;
        public void Perde(System.Action ortunce) { perdeIs = ortunce; perdeEvre = 1; perdeT = 0; perdeMin = .15f; perdeAtla = false; perdeAcilis = false; acilisBek = 0; }
        // Açılış perdesi (HTML perde({kapali:true, min:.6})): oyun kapalı perdeyle açılır, vuruş sesleri hazırlanınca kalkar;
        // hazırken dokunuş beklemeyi kısaltır.
        public void Acilis() { perdeEvre = 2; perdeT = 0; hazirT = -1; acilisIyi = 0; perdeIs = null; perdeMin = .6f; perdeAtla = false; perdeAcilis = true; acilisBek = 0; perdeAci = 0; acilisT0 = Time.realtimeSinceStartup; acilisAcT = acilisSonT = -1; }
        public bool PerdeKapali { get { return perdeEvre == 1 || perdeEvre == 2; } }
        // UI Toolkit menüsü için (UiKok): perde var mı, ne kadar örtüyor (0..1; menü 1 − örtü opaklıkla solar), dokunuşla
        // beklemeyi kısalt. Perdenin çizimiyle aynı eğriler.
        public bool PerdeVar { get { return perdeEvre > 0; } }
        public float PerdeOrtu
        {
            get
            {
                if (perdeEvre == 2) return 1; if (perdeEvre == 0) return 0;
                float sc, al; DiskOlcek(out sc, out al); return Ayar.reduce ? al : sc;   // menü diskin örttüğü kadar solar (HTML: disk menünün üstünde)
            }
        }
        public void PerdeAtla() { if (perdeEvre == 2) perdeAtla = true; }
        // Menü içeriğinin kapanışta aşağı kayması (perde v2'den kaldı; HTML perdesinde kayma yok, 2026-10-29'dan beri hep 0).
        public float PerdeKayma { get { return 0; } }
        // Uçan topun çıkış noktası (ekran pikseli, IMGUI düzeni: sol üst köşe): OYNA'ya basılınca UiKok yazar; yoksa alt orta.
        public void PerdeKaynak(Vector2 guiNokta) { perdeKaynak = guiNokta; kaynakVar = true; }
        // Sayı kazanınca üç kıvılcım patlaması 120 ms arayla ekranın üst üçte birinde (HTML burst ×3, 22 parça, 260 px/sn)
        float kivT = -1; public void SayiKivilcim() { kivT = 0; }

        // GUILayout kullanılmıyor (her şey Rect ile): Layout geçişi atlanır, OnGUI karede bir kez az çalışır (2026-10-26, §7.2).
        void Awake() { useGUILayout = false; }

        void Update()
        {
            float d = Time.unscaledDeltaTime; for (int i = floats.Count - 1; i >= 0; i--) { floats[i].a += d; if (floats[i].a > 1.2f) floats.RemoveAt(i); }
            if (ogrMsgT > 0) ogrMsgT -= d;
            ralliPop = Mathf.Max(0, ralliPop - d * 4); pop[0] = Mathf.Max(0, pop[0] - d * 3); pop[1] = Mathf.Max(0, pop[1] - d * 3); flash = Mathf.Max(0, flash - d * 2.2f);
            for (int i = cams.Count - 1; i >= 0; i--) { var c = cams[i]; c.z += d; cams[i] = c; if (c.z > .5f) cams.RemoveAt(i); }
            if (hawk != null) { hawk.t += d; if (hawk.t > 1.5f) hawk = null; }
            if (konfetiT >= 0) { int once = Mathf.FloorToInt(konfetiT / .13f); konfetiT += d; int sonra = Mathf.FloorToInt(konfetiT / .13f); if (sonra > once && once < 6) { var c = new[] { "#d8ff5a", "#ffa04d", "#7ad7ff" }[once % 3]; var p = root.cam.ScreenToWorldPoint(new Vector3(Screen.width * (.15f + Random.value * .7f), Screen.height * (.5f + Random.value * .3f), 6)); root.fx.Burst(p, Kit.Hex(c), 30, 300, "spark"); } if (konfetiT > 1) konfetiT = -1; }
            if (kivT >= 0)
            {
                int once = kivT <= 0 ? -1 : Mathf.FloorToInt(kivT / .12f); kivT += d; int sonra = Mathf.FloorToInt(kivT / .12f);
                for (int k = once + 1; k <= sonra && k < 3; k++) { var p = root.cam.ScreenToWorldPoint(new Vector3(Screen.width * (.2f + Random.value * .6f), Screen.height * .7f, 6)); root.fx.Burst(p, Kit.Hex(new[] { "#d8ff5a", "#ffa04d", "#7ad7ff" }[k]), 22, 260, "spark"); }
                if (kivT > .3f) kivT = -1;
            }
            if (Ayar.fps) FpsOlc();
            SonucZaman();
            PerdeTick(d);
        }
        // ---- gizli kare hızı göstergesi (HTML FPS): üst satır son 1 sn, alt satır o ekranın toplamı; ekran değişince sıfırlanır.
        // Metin geliştirici içindir, çevrilmez (HTML ile aynı).
        readonly List<Vector2> fpsK = new List<Vector2>(); string fpsEk = "", fpsYazi = ""; int fpsN, fpsTak; float fpsT, fpsEn, fpsYaz;
        string FpsEkran()
        {
            if (perdeEvre > 0) return "Perde"; if (root.ekran == Ekran.Bitti) return "Maç sonu"; if (root.ekran == Ekran.Oyun) return root.paused ? "Duraklatma" : "Maç";
            return root.ui != null ? root.ui.FpsAdi : "Menü";
        }
        void FpsOlc()
        {
            float now = Time.realtimeSinceStartup * 1000, d = Time.unscaledDeltaTime * 1000; string ek = FpsEkran();
            if (ek != fpsEk) { fpsEk = ek; fpsN = fpsTak = 0; fpsT = fpsEn = 0; }
            if (d > 0 && d < 1000) { fpsK.Add(new Vector2(now, d)); fpsN++; fpsT += d; if (d > 50) fpsTak++; if (d > fpsEn) fpsEn = d; }
            while (fpsK.Count > 0 && fpsK[0].x < now - 1000) fpsK.RemoveAt(0);
            if (now - fpsYaz > 500)
            {
                fpsYaz = now; float en = 0; int tak = 0; foreach (var q in fpsK) { if (q.y > en) en = q.y; if (q.y > 50) tak++; }
                fpsYazi = fpsK.Count + " fps · en uzun " + Mathf.RoundToInt(en) + " ms · takılma " + tak + "\n" + ek + ": ort " + (fpsT > 0 ? Mathf.RoundToInt(fpsN / (fpsT / 1000)) : 0) + " fps · en uzun " + Mathf.RoundToInt(fpsEn) + " ms · takılma " + fpsTak + " · " + Mathf.RoundToInt(fpsT / 1000) + " sn";
            }
        }
        public void FpsAc(bool on) { Ayar.fps = on; Ayar.Kaydet(); fpsK.Clear(); fpsEk = ""; fpsYazi = ""; Yerel.Hap("tick"); }
        void FpsCiz()
        {
            if (!Ayar.fps || fpsYazi == "") return; var st = new GUIStyle(tiny) { alignment = TextAnchor.UpperLeft, font = fKalin, wordWrap = false };
            var sz = st.CalcSize(Ic(fpsYazi)); var r = new Rect(Mathf.Max(safe.xMin, 0) + 6 * u, top + 80 * u, sz.x + 12 * u, sz.y + 6 * u);
            Fill(r, new Color(0, 0, 0, .62f)); var o = GUI.color; GUI.color = Kit.Hex("#d8ff5a"); GUI.Label(new Rect(r.x + 6 * u, r.y + 3 * u, sz.x + 4, sz.y), fpsYazi, st); GUI.color = o;
        }
        void PerdeTick(float d)
        {
            if (perdeEvre == 0) return;
            Kalite.Hareket();   // perde ve açılış süresince menü tarafında da yüksek kare hızı (Kalite)
            if (perdeSabit >= 0) { perdeT = perdeSabit; TopPozYaz(); return; }   // ekran turu: kare dondurulmuş (PerdeKare)
            perdeT += d; perdeAci += d * 1.2f;
            // Yerel açılış perdesi (2026-10-30, yasin "splash gifi ilk açıldığı andan itibaren başlamalı"; BirdieAcilis.mm): iOS
            // perdesi motor açılmadan döner; açıkken açı onun süresinden yazılır (aynı 1,2 rad/sn), Unity perdesi kesintisiz
            // (< 50 ms) 6 kare çizince ya da perde açılmaya başlarken kaldırılır. Altında birebir aynı görüntü olduğu için devir
            // görünmez; ilk karelerin takılması (doku üretimi, motorun ısınması) yerel perdenin arkasında kalır.
            // 2026-10-31 akşam: yerel perde artık kapalı evrenin tamamında üstte kalır (önceden 6 akıcı karede kalkıyordu; sonraki
            // takılmalar Unity perdesinde görünüyordu) ve Unity perdeyi açmaya başladığı karede kalkar. Maça geçişte de kullanılır.
            float ys = Yerel.AcilisSure();
            if (ys >= 0)
            {
                if (!Ayar.reduce) perdeAci = ys * 1.2f;
                if (perdeEvre != 2) Yerel.AcilisBitir();
            }
            if (perdeEvre == 1 && perdeT >= (Ayar.reduce ? .2f : .45f))
            {
                perdeEvre = 2; perdeT = 0; acilisIyi = 0; hazirT = -1;
                Yerel.PerdeGoster(perdeAci, .6f, L.T("Hazırlanıyor…"), Ayar.YaziK);
                if (perdeIs != null) { perdeIs(); perdeIs = null; bosalt = true; }
            }
            else if (perdeEvre == 2)
            {
                // 2026-10-31 (Unity incelemesi Y1): oyun tek sahnede yaşıyor ve koddan üretilen malzeme/doku/mesh hiç
                // boşaltılmıyordu (figür yeniden kurulumu, Dolap, eski efektler). Perde kapalıyken, maç kurulduktan bir kare
                // sonra (Destroy kare sonunda işlenir) kullanılmayanlar boşaltılır; olası takılma perdenin arkasında kalır.
                if (bosalt) { bosalt = false; Resources.UnloadUnusedAssets(); }
                // HTML perde 'kapali': sesler hazır değilse bekler (8 sn sonra yine açılır); .6 sn'den uzun beklemede dönen top
                bool hazir = root.Hazir() || perdeT > 8;
                perdeBek = !hazir && perdeT > .6f;
                float min = Ayar.reduce && perdeMin < .5f ? .05f : perdeMin;
                // Akıcılık kapısı: perde ancak son AKICI_KARE kare 25 ms'nin altındaysa açılır (maç kurulumu, ilk gölgelendirici
                // derlemesi, boşaltma perdenin arkasında biter; açılış animasyonu takılmaz). Yavaş cihaz kapıyı hiç geçemezse
                // hazır olduktan AKICI_AZAMI sn sonra yine açılır.
                if (hazir && hazirT < 0) hazirT = perdeT;
                bool akici = acilisIyi >= AKICI_KARE || perdeSabit >= 0 || (hazirT >= 0 && perdeT - hazirT > AKICI_AZAMI);
                if (hazir && akici && (perdeT >= min || perdeAtla)) { perdeEvre = 3; perdeT = 0; perdeBek = false; Yerel.AcilisBitir(); if (perdeAcilis) acilisAcT = Time.realtimeSinceStartup; }
            }
            else if (perdeEvre == 3 && perdeT >= (Ayar.reduce ? .25f : .35f)) { perdeEvre = 0; kaynakVar = false; if (perdeAcilis && acilisSonT < 0) acilisSonT = Time.realtimeSinceStartup; }
            if (perdeEvre != 2) perdeBek = false;
            TopPozYaz();
        }

        // D4 (2026-10-31): Styles() boyut/yazı ölçeği her değiştiğinde 7 yeni 1×1 doku açıyor, eskileri yok etmiyordu. Renk başına bir doku.
        static readonly Dictionary<Color, Texture2D> solidler = new Dictionary<Color, Texture2D>();
        static Texture2D Solid(Color c) { Texture2D t; if (solidler.TryGetValue(c, out t) && t != null) return t; t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); solidler[c] = t; return t; }
        void Styles()
        {
            float nu = Mathf.Min(Screen.height / 640f, Screen.width / 340f), nf = nu * Ayar.YaziK; if (big != null && Mathf.Abs(nu - u) < .001f && Mathf.Abs(nf - fu) < .001f) return; u = nu; fu = nf; StilSifirla(); // eski boyutun önbelleği yeni tabanla karışmasın
            FontYukle();
            big = new GUIStyle(GUI.skin.label) { font = fSiyah, fontSize = Mathf.RoundToInt(32 * fu), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            mid = Stil(big, Mathf.RoundToInt(19 * fu));
            small = new GUIStyle(big) { font = fYari, fontSize = Mathf.RoundToInt(13 * fu) };
            tiny = Stil(small, Mathf.RoundToInt(11 * fu));
            left = new GUIStyle(small) { alignment = TextAnchor.UpperLeft }; leftB = new GUIStyle(left) { font = fKalin, fontSize = Mathf.RoundToInt(15 * fu) };
            btn = new GUIStyle(GUI.skin.button) { font = fKalin, fontSize = Mathf.RoundToInt(15 * fu), wordWrap = true };
            btn.normal.background = Solid(Kit.Hex("#22343a")); btn.hover.background = btn.active.background = Solid(Kit.Hex("#2e464e")); btn.normal.textColor = btn.hover.textColor = btn.active.textColor = Color.white;
            btnD = new GUIStyle(btn); btnD.normal.background = btnD.hover.background = btnD.active.background = Solid(Kit.Hex("#1a2629")); btnD.normal.textColor = btnD.hover.textColor = btnD.active.textColor = new Color(1, 1, 1, .45f);
            btnP = new GUIStyle(btn); btnP.normal.background = Solid(Kit.Hex("#d8ff5a")); btnP.hover.background = btnP.active.background = Solid(Kit.Hex("#e6ff8c")); btnP.normal.textColor = btnP.hover.textColor = btnP.active.textColor = Kit.Hex("#0d1a12");
            chip = Stil(btn, Mathf.RoundToInt(13 * fu), FontStyle.Normal); chipOn = Stil(chip, -1, FontStyle.Bold);
            chipOn.normal.background = chipOn.hover.background = chipOn.active.background = Solid(Kit.Hex("#7ad7ff")); chipOn.normal.textColor = chipOn.hover.textColor = chipOn.active.textColor = Kit.Hex("#07161c");
            card = new GUIStyle(GUI.skin.box); card.normal.background = Solid(new Color(.04f, .08f, .09f, .86f));
            HudKutular();
            if (ring == null) { ring = Kit.RingTex(128, 5); ringK = Kit.RingTex(128, 3); ringKesik = Kit.RingKesikTex(128, 6, 12); glow = Kit.RadialTex(64); disk = Kit.RingTex(128, 128); bar = Solid(Color.white); }
        }

        // 2026-10-30 (yasin: "maç içi sayılardan sonra çıkan yazı alttan üstten kesik gözüküyor ... başka boyuttaki ekranlarda da
        // düzgün gözükmeli"): IMGUI etiketi kutusunun dışını kırpar. Kutular sabit (ör. 46·u) ama yazı boyu fu = u · YaziK ile ve
        // vuruş yazısının büyüme çarpanıyla (×1,6 · 1,05) büyüyor; büyük yazıda satır yüksekliği kutuyu aşıp harfin tepesi ve
        // altı kesiliyordu, iki satıra kırılan uzun yazı ise ortadan kırpılıyordu. Artık yazının gerçek yüksekliği kutudan
        // büyükse kutu hizasına göre büyür (ortalı: iki yana, üst hizalı: aşağı, alt hizalı: yukarı). Kırpılma sayısı ekran
        // turunun raporuna gider (Kirpilan).
        public static int Kirpilan;
        static Rect Sigdir(Rect r, string t, GUIStyle s)
        {
            float need = s.CalcHeight(Ic(t), r.width); if (need <= r.height + .5f) return r;
            Kirpilan++; float ek = need - r.height; var a = s.alignment;
            bool ust = a == TextAnchor.UpperLeft || a == TextAnchor.UpperCenter || a == TextAnchor.UpperRight, alt = a == TextAnchor.LowerLeft || a == TextAnchor.LowerCenter || a == TextAnchor.LowerRight;
            return new Rect(r.x, ust ? r.y : alt ? r.y - ek : r.y - ek / 2, r.width, need);
        }
        // Nunito/Baloo'da olmayan simgeler (✓ ✗ ● ○ ◆ ▸): bu yazılar o stilin varsayılan yazı tipli kopyasıyla çizilir
        // (2026-10-30: HUD Nunito'ya geçince işaretler kutu görünmesin; ✓/✗ anlamı yalnız renkte kalmasın diye işaret gerekli).
        static readonly Dictionary<GUIStyle, GUIStyle> glifSt = new Dictionary<GUIStyle, GUIStyle>();
        static GUIStyle Glif(GUIStyle s, string t)
        {
            if (s.font == null || t == null || t.IndexOfAny(GLIF) < 0) return s; GUIStyle g;
            if (!glifSt.TryGetValue(s, out g)) { g = new GUIStyle(s) { font = null, fontStyle = FontStyle.Bold }; glifSt[s] = g; } return g;
        }
        static readonly char[] GLIF = { '✓', '✗', '●', '○', '◆', '▸', '★', '▶' };
        static void Etk(Rect r, string t, GUIStyle s) { s = Glif(s, t); GUI.Label(Sigdir(r, t, s), t, s); }   // sabit boylu etiket, aynı kural
        static void Shadowed(Rect r, string t, GUIStyle s, Color c)
        {
            s = Glif(s, t); r = Sigdir(r, t, s);
            var o = GUI.color; GUI.color = new Color(0, 0, 0, c.a * .6f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, s);
            GUI.color = c; GUI.Label(r, t, s); GUI.color = o;
        }
        bool Button(Rect r, string t, bool primary = false) { blocks.Add(r); return GUI.Button(r, L.T(t), primary ? btnP : btn); }
        bool ButtonRaw(Rect r, string t, bool primary = false, bool kapali = false) { blocks.Add(r); if (kapali) { GUI.Label(r, t, btnD); return false; } return GUI.Button(r, t, primary ? btnP : btn); }
        bool Chip(Rect r, string t, bool on) { blocks.Add(r); bool b = GUI.Button(r, (on ? "✓ " : "") + L.T(t), on ? chipOn : chip); if (b) Yerel.Hap("tick"); return b; }   // seçili olan yalnız renkle değil işaretle de okunur
        void Fill(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = o; }
        void Tex(Rect r, Texture t, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, t); GUI.color = o; }

        float H, W, top, bot, lx, rx, BH; Rect safe;
        void Layout()
        {
            W = Screen.width; H = Screen.height; safe = Guvenli.Alan;   // 2026-10-27: ekran turu çentik taklidi (Game/UI/Duyarli.cs)
            top = H - safe.yMax + 8 * u; bot = safe.yMin + 10 * u; lx = Mathf.Max(safe.xMin, 0) + 16 * u; rx = W - Mathf.Max(W - safe.xMax, 0) - 16 * u;
            BH = Mathf.Max(46 * u, 44 * H / 640f);
        }
        // 640 px referans → ekran pikseli (GameRoot girdi ve HTML eşikleri bu ölçekte)
        float RefK { get { return H / 640f; } }

        void OnGUI()
        {
            var M = root.match; Styles(); Layout(); blocks.Clear();
            if (perdeEvre > 0 && Event.current.isMouse)
            {   // perde altındaki düğmeler dokunuşu almaz (açılış perdesi saniyelerce kapalı kalabilir); kapalıyken dokunuş beklemeyi kısaltır
                if (perdeEvre == 2 && Event.current.type == EventType.MouseDown) perdeAtla = true; Event.current.Use();
            }
            if (M == null) { PerdeCiz(); return; }
            if (root.ekran == Ekran.Menu) { }   // menü UI Toolkit'te (UiKok); panel girişi orada USS geçişiyle
            else
            {
                Play(M);
                if (root.geriT > 0 && !root.paused && root.ekran == Ekran.Oyun) GeriCiz();
                if (root.paused) Pause(M);
                if (root.ekran == Ekran.Bitti) Over(M);
            }
            if (flash > 0) Fill(new Rect(0, 0, W, H), new Color(flashC.r, flashC.g, flashC.b, flash));
            PerdeCiz(); FpsCiz();
        }

        // Geri sayım rakamı (GameRoot.geriT): Tüycükler dili, Baloo; her rakam büyüyüp oturur, hareketi azaltta yalnız görünür.
        // Boy kısa kenara göre (her ekranda ortada, kesilmez: clipping yok, kutu yazının iki katı).
        GUIStyle geriSt; float geriPx;
        void GeriCiz()
        {
            float px = Mathf.Min(W, H) * .26f;
            if (geriSt == null || Mathf.Abs(px - geriPx) > .5f)
            {
                geriPx = px; var f = fSiyah != null ? fSiyah : Resources.Load<Font>("fonts/Baloo2-ExtraBold");
                geriSt = new GUIStyle(GUI.skin.label) { font = f, fontSize = Mathf.RoundToInt(px), alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
                geriSt.padding = new RectOffset(0, 0, 0, 0);
            }
            float adim = GameRoot.GERI / 3; int n = Mathf.Clamp(Mathf.CeilToInt(root.geriT / adim), 1, 3);
            float e = 1 - (root.geriT - (n - 1) * adim) / adim;   // bu rakamın 0..1 evresi
            float k = Ayar.reduce ? 1 : 1.35f - .35f * (1 - (1 - Mathf.Clamp01(e / .35f)) * (1 - Mathf.Clamp01(e / .35f)));
            float a = Ayar.reduce ? 1 : Mathf.Clamp01(e / .12f) * (e > .85f ? Mathf.Clamp01((1 - e) / .15f) : 1);
            var r0 = new Rect(W / 2 - px, H * .42f - px, px * 2, px * 2);
            var m = GUI.matrix; GUIUtility.ScaleAroundPivot(new Vector2(k, k), r0.center);
            GUI.color = new Color(0, 0, 0, .45f * a); GUI.Label(new Rect(r0.x + 3, r0.y + 4, r0.width, r0.height), Sayi(n), geriSt);
            GUI.color = new Color(.79f, .93f, .29f, a); GUI.Label(r0, Sayi(n), geriSt); GUI.color = Color.white; GUI.matrix = m;
        }
        void Dim(float a) { GUI.color = new Color(0, 0, 0, a); GUI.DrawTexture(new Rect(-W, 0, W * 3, H), Texture2D.whiteTexture); GUI.color = Color.white; }   // panel kayarken kenar açık kalmasın
        float ColW { get { return Mathf.Min(rx - lx, 360 * u); } }
        float ColX { get { return (W - ColW) / 2; } }

        // ---- menü: 2026-10-26 UI Toolkit'e taşındı (Game/UI/UiKok ve ekranları). Burada yalnız perde ve gizli kare hızı kaldı.

        // ---- maç içi ----
        void Play(Match M)
        {
            var gs = Ayar.GS; bool ogr = M.ogr != null;
            Halo(M); Iz(M); TopIzi(M); if (ogr) OgrIz(M); HedefYazi(M);
            // skor kutusu: sayı alınca büyür (HTML pop: 24·(1+.5·sin(pop·π)))
            float cx = W / 2, sy = top; var kr = new Rect(cx - 62 * u, sy, 124 * u, 34 * u); Boya(kr, hudKutu, Color.white);
            if (!ogr)
            {
                var s0 = Stil(big, Mathf.RoundToInt(24 * fu * (1 + .5f * Mathf.Sin(pop[0] * Mathf.PI)))); var s1 = Stil(big, Mathf.RoundToInt(24 * fu * (1 + .5f * Mathf.Sin(pop[1] * Mathf.PI))));
                Shadowed(new Rect(cx - 60 * u, sy - 6 * u, 60 * u, 46 * u), Sayi(M.sc[0]), s0, Kit.Hex(gs.forma)); Shadowed(new Rect(cx - 10 * u, sy, 20 * u, 34 * u), "–", Stil(big, Mathf.RoundToInt(24 * fu)), Kit.Hex("#9dbab3"));
                Shadowed(new Rect(cx, sy - 6 * u, 60 * u, 46 * u), Sayi(M.sc[1]), s1, Kit.Hex(RAKIP_HUD));
                Fill(new Rect(cx + (M.server == "me" ? -54 : 50) * u, sy + 15 * u, 5 * u, 5 * u), M.server == "me" ? Kit.Hex(gs.forma) : Kit.Hex(RAKIP_HUD));   // servis noktası
                // sol üst kutu (2026-10-26, yasin Ar-Ge önerisi "ekle"): "{hedef} sayı · rakip" satırı kalktı (duraklat
                // ekranında zaten yazıyor); ralli sayacı ralli 3'ü geçince belirir ve her vuruşta skorun pop diliyle hafifçe
                // büyür (hareketi azaltta büyümez); "Görev n/3" yerine 3 nokta: biten dolu disk, kalan halka (biçimle de okunur).
                var bs = Stil(left, Mathf.RoundToInt(11 * fu), FontStyle.Bold);
                if (M.rally != sonRalli) { if (M.rally > 3 && !Ayar.reduce) ralliPop = 1; sonRalli = M.rally; }
                bool ralliVar = M.rally > 3; float bw = 64 * u, bh = ralliVar ? 40 * u : 22 * u;
                Boya(new Rect(lx - 10 * u, top + 2 * u, bw, bh), hudKutu, Color.white);
                float gy = top + 8 * u;
                if (ralliVar)
                {
                    GUI.color = Kit.Hex("#9dbab3"); Etk(new Rect(lx - 4 * u, top + 4 * u, bw + 20 * u, 18 * u), RalliYazi(M.rally), Stil(bs, Mathf.RoundToInt(12 * fu * (1 + .25f * Mathf.Sin(ralliPop * Mathf.PI)))));
                    gy = top + 26 * u;
                }
                for (int i = 0; i < 3; i++)
                {
                    bool bitti = i < M.gorevBitti; var r = new Rect(lx - 3 * u + i * 16 * u, gy, 10 * u, 10 * u);
                    Tex(r, bitti ? disk : ringK, new Color(1, .82f, .23f, bitti ? .95f : .7f));
                }
                GUI.color = Color.white;
            }
            else Shadowed(kr, L.T("Antrenman"), mid, Color.white);
            // tribün flaşları
            foreach (var c in cams) { if (c.z < 0) continue; float a = 1 - c.z / .5f; Tex(new Rect(c.x * W - 10 * RefK, H - (H - (top + c.y * RefK)) - 10 * RefK, 20 * RefK, 20 * RefK), glow, new Color(1, 1, 1, a)); }
            if (hawk != null) HawkCiz(M);
            if (root.ekran == Ekran.Oyun && !root.paused && DuraklatDugme(new Rect(rx - BH, top, BH, BH))) root.Duraklat();   // Duraklat
            float y = top + 62 * u;
            if (root.rakipKartT > 0 && !ogr && root.ekran == Ekran.Oyun) y = RakipKart(M, y);   // r17: sonuç ekranının altından okunuyordu
            if (ogr && root.ekran == Ekran.Oyun) y = OgrKart(M, y);
            if (!ogr && root.ekran == Ekran.Oyun) y = UstaKart(M, y);   // görev sonuç ekranında kart kalkar (Over üstüne binmesin)
            // smaç gücü çubuğu
            {
                bool dol = M.energy >= 1; string lab = L.T(dol ? "Sıçrayarak smaç hazır" : "Smaç gücü"); var ls = Stil(leftB, Mathf.RoundToInt(12 * fu));
                float lw = ls.CalcSize(Ic(lab)).x, bx = lx + lw + 10 * u, bw = Mathf.Max(40 * u, rx - bx), by = H - bot - 12 * u;
                bool isaret = ogr && M.OgrA() != null && M.OgrA().bilgi == "guc";
                GUI.color = dol ? Kit.Hex("#ff8a3c") : Kit.Hex("#cfe3dc"); Etk(new Rect(lx, by - 8 * u, lw + 4 * u, 22 * u), lab, ls); GUI.color = Color.white;
                // ray 12·u (eskisi 8·u), merkezi aynı yerde; dolgu tam boy yuvarlak çizilip doluluk genişliğinde kırpılır (uç dolunca yuvarlanır)
                var ry = new Rect(bx, by - 2 * u, bw, 12 * u); Boya(ry, hudRay, new Color(1, 1, 1, .14f));
                if (M.energy > 0)
                {
                    GUI.BeginGroup(new Rect(ry.x, ry.y, ry.width * Mathf.Min(1, M.energy), ry.height));
                    Boya(new Rect(0, 0, ry.width, ry.height), hudRay, dol ? new Color(1, .54f, .24f, Ayar.reduce ? 1 : .75f + .25f * Mathf.Sin(Time.unscaledTime * 1000 / 160)) : Kit.Hex("#d8ff5a"));
                    GUI.EndGroup();
                }
                if (isaret) { float nab = Ayar.reduce ? 1 : .6f + .4f * Mathf.Sin(Time.unscaledTime * 6); Cerceve(new Rect(lx - 10 * u, by - 13 * u, rx - lx + 20 * u, 26 * u), new Color(.85f, 1, .35f, nab), 3); }
            }
            if (M.Dril != null && root.ekran == Ekran.Oyun) VurusKart(M, H - bot - 30 * u);
            float hy = H - bot - 34 * u;
            if (M.sc[0] + M.sc[1] < 2 && M.s != null && !ogr && (Ogretici || KosIpucu(M)))
            {
                // 2026-10-26 r11: İngilizce ve büyük yazıda ipucu iki satıra iner; sabit 20 px kutu üst satırı kesiyordu.
                // Yükseklik metinden ölçülür, alt kenar smaç çubuğu etiketinin hemen üstünde sabit kalır.
                string ip = L.T(!Ogretici ? "Dokun: oraya koş" : M.easy ? "Top parlayınca kaydır: yön ve güç senin" : "Dokun: oraya koş · Top parlayınca kaydır");
                // 2026-10-29: tek satır (Sigdir); iki satıra inince smaç çubuğu etiketine biniyor ve alttan kesik görünüyordu.
                var ist = Sigdir(Stil(tiny, -1, FontStyle.Bold), ip, rx - lx); float ih = ist.CalcSize(Ic(ip)).y; Shadowed(new Rect(lx, hy + 14 * u - ih, rx - lx, ih + 2 * u), ip, ist, Kit.Hex("#cfe3dc"));
            }
            if (M.s == null && !M.between && M.svPending == null && !ogr) ServisSeridi(M, hy);
            if (M.HitWindow() && (!ogr || !M.ogr.dur) && !M.easy) { }
            NefesYazi(M.me, true, M); NefesYazi(M.op, false, M);
            foreach (var f in floats)
            {
                // HTML txt: yaylı büyüme (k = 1 + 2,7(q−1)³ + 1,7(q−1)²), .8 sn sonra solar
                float q = Mathf.Min(1, f.a * 5), k = 1 + 2.7f * Mathf.Pow(q - 1, 3) + 1.7f * Mathf.Pow(q - 1, 2), a = f.a > .8f ? 1 - (f.a - .8f) / .4f : 1;
                var c = f.c; c.a = Mathf.Clamp01(a); var st = Stil(mid, Mathf.Max(1, Mathf.RoundToInt(19 * fu * f.size * (.5f + .5f * k))));
                float yy;
                if (f.sabit) yy = H * (f.c == Kit.Hex(RAKIP_HUD) ? .56f : .4f);
                else { var sp = root.cam.WorldToScreenPoint(f.w + Vector3.up * (.6f + f.a * .5f)); if (sp.z < 0) continue; yy = Mathf.Clamp(H - sp.y - 20 * u, top + 60 * u, H - bot - 80 * u); }
                // 2026-10-26 r11: görev kartı ekranın üstünü kaplar; "Alana düşmedi" gibi yazı kartın düğmesinin üstüne biniyordu.
                // Kartla çakışan yazı kartın hemen altına iner (blocks = bu karede çizilen kart ve düğmeler).
                if (ogr) foreach (var bl in blocks) if (bl.width > (rx - lx) * .6f && yy < bl.yMax && yy + 46 * u > bl.yMin) yy = bl.yMax + 4 * u;
                st = Sigdir(st, f.t, rx - lx);   // tek satır kalsın (iki satıra kırılınca skorun/kartın üstüne biniyordu)
                Shadowed(new Rect(lx, yy, rx - lx, 46 * u), f.t, st, c);
            }
            if (ogrMsgT > 0 && ogrMsg != null) { var c = Kit.Hex("#d8ff5a"); c.a = Mathf.Clamp01(ogrMsgT * 2); Shadowed(new Rect(lx, H * .5f, rx - lx, 60 * u), ogrMsg, mid, c); }
        }
        void Cerceve(Rect r, Color c, float k)
        {
            k *= u; Fill(new Rect(r.x, r.y, r.width, k), c); Fill(new Rect(r.x, r.yMax - k, r.width, k), c); Fill(new Rect(r.x, r.y, k, r.height), c); Fill(new Rect(r.xMax - k, r.y, k, r.height), c);
        }
        // Öğretici yazılar (2026-10-29, Ar-Ge 1, yasin "yap"): maç başı ipucu, servis talimatı ve topun yanındaki vuruş etiketi
        // 3 galibiyete kadar görünür, sonra söner. On maçtan sonra oyuncu onları okumuyor ama göz tam vuruş anında yazıya
        // kayıyordu; servis şeridi kortun altını kapatıyordu. wins < 3: kaybeden yeni oyuncu henüz öğrenmemiştir, yazılar
        // kalır. Halka, rengi ve biçimi kalır (anlamı onlar taşır); ilk servisteki görev listesi kalır (her maçta yeni bilgi).
        // Antrenman kendi kartıyla öğretir, bu kuraldan etkilenmez; ayarda seçenek yok (bilinçli: tek davranış).
        public static bool Ogretici { get { return Ayar.wins < 3; } }
        // Koşu sende (Kolay kapalı ya da Usta yolu) ilk kez oynanıyorsa, 3 galibiyeti geçmiş oyuncuya da "Dokun: oraya koş"
        // o maçta bir kez görünür: yeni bir denetimdir. Maç bitince tuytop_kos_ipucu yazılır (GameRoot.EndMatch).
        static bool KosIpucu(Match M) { return !M.easy && !Ayar.kosIpucu; }
        // Servis şeridi (2026-10-29, yasin: "maç içinde çıkan yazı alttan kesiliyor ama yine de büyük alan kaplıyor, makul geri
        // bildirimlerle vermeliyiz"): eskiden 4–6 satırlık sarılan kutu (servis + kaydırma talimatı + "Rakip: …" + görev listesi)
        // kortun alt çeyreğini örtüyor, büyük yazıda ve İngilizcede alttan kesiliyordu. Şimdi ortada tek bir hap: her satır tek
        // satıra sığdırılır (Sigdir), en çok üç satır. "Rakip: …" satırı kalktı (rakip kartı ve skor tabelası zaten söylüyor);
        // görevler yalnız ilk serviste tek satır kısaltılmış; talimat yalnız Öğretici dönemde ve ilk serviste.
        void ServisSeridi(Match M, float hy)
        {
            var sat = new List<KeyValuePair<string, Color>>(); bool ilk = M.sc[0] + M.sc[1] == 0;
            string side = L.T(M.ServeSideOf(M.server) == "R" ? "sağ" : "sol");
            sat.Add(new KeyValuePair<string, Color>(M.server == "me" ? L.T("Servis sende · {0} kare", side) : L.T("Rakip servis atıyor · {0} kare", side), Color.white));
            if (!string.IsNullOrEmpty(M.serveMsg) && !M.serveMsg.StartsWith("Rakip: ")) sat.Add(new KeyValuePair<string, Color>(L.Cumle(M.serveMsg), Kit.Hex("#ffd27a")));
            else if (M.server == "me" && M.sc[0] + M.sc[1] < 2 && Ogretici) sat.Add(new KeyValuePair<string, Color>(L.T("Yukarı kaydır: yavaş kısa · hızlı uzun"), Kit.Hex("#cfe3dc")));
            if (ilk && M.gorevler.Count > 0) { var ad = new List<string>(); foreach (var g in M.gorevler) ad.Add(L.Cumle(g.ad)); sat.Add(new KeyValuePair<string, Color>(L.T("Görev: ") + string.Join(" · ", ad.ToArray()), Kit.Hex("#ffd23a"))); }
            var st0 = Stil(small, Mathf.RoundToInt(14 * fu), FontStyle.Bold); var st = Stil(small, Mathf.RoundToInt(12 * fu));
            float enW = Mathf.Min(rx - lx, 380 * u) - 24 * u, ww = 0, hh = 10 * u; var stiller = new GUIStyle[sat.Count];
            for (int i = 0; i < sat.Count; i++) { var b0 = i == 0 ? st0 : st; var f = Sigdir(b0, sat[i].Key, enW); stiller[i] = f; var sz = f.CalcSize(Ic(sat[i].Key)); ww = Mathf.Max(ww, Mathf.Min(sz.x, enW)); hh += sz.y; }
            ww += 24 * u; float y0 = H - bot - 30 * u - hh; var kutu = new Rect(W / 2 - ww / 2, y0, ww, hh);
            Fill(kutu, new Color(9 / 255f, 16 / 255f, 17 / 255f, .74f));
            float yy = y0 + 5 * u;
            for (int i = 0; i < sat.Count; i++) { float h = stiller[i].CalcSize(Ic(sat[i].Key)).y; GUI.color = sat[i].Value; GUI.Label(new Rect(kutu.x + 12 * u, yy, ww - 24 * u, h), sat[i].Key, new GUIStyle(stiller[i]) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow }); yy += h; }
            GUI.color = Color.white;
        }
        // Vuruş halesi (HTML shuttle(): reachInfo varken): R·1,3 ışıma, nabız atan halka (uzanmada turuncu), iç altın halka R·.33,
        // etiket (antrenmanda susar: kart başka vuruş istiyorken "smaç" yazıp çelişmesin).
        void Halo(Match M)
        {
            Reach I; if (M.s == null || !M.ReachInfo(out I)) return;
            var sp = root.cam.WorldToScreenPoint(Kit.W(M.s.x, M.s.y, M.s.z)); if (sp.z < 0) return; float px = sp.x, py = H - sp.y, R = root.HitR() * RefK;
            float pul = Ayar.reduce ? 1 : 1 + Mathf.Sin(Time.unscaledTime * 1000 / 60) * .06f;
            Tex(new Rect(px - R * 1.3f, py - R * 1.3f, R * 2.6f, R * 2.6f), glow, new Color(.85f, 1, .35f, .35f));
            // Biçim de anlam taşır (2026-10-29, renk körü; etiket 3 galibiyetten sonra söndüğü için tek ipucu halka):
            // düz halka = normal vuruş, kesik halka = uzan (uzak top), çift halka = file üstü (öldürücü vuruş).
            // Hareketi azalt açıkken nabız durur, biçim kalır.
            Color hc = I.kill ? Kit.Hex("#ffd27a") : I.lunge || I.uzan ? Kit.Hex("#ff9f7a") : Kit.Hex("#d8ff5a");
            Tex(new Rect(px - R * pul, py - R * pul, 2 * R * pul, 2 * R * pul), I.lunge || I.uzan ? ringKesik : ring, hc);
            if (I.kill) { float r2 = R * .8f * pul; Tex(new Rect(px - r2, py - r2, 2 * r2, 2 * r2), ringK, hc); }
            Tex(new Rect(px - R * .33f, py - R * .33f, R * .66f, R * .66f), ringK, new Color(1, .82f, .23f, .9f));
            if (M.ogr != null) return;
            if (Ogretici)
            {
            string hnd = Match.HandOfPublic(M.me, "me", M.s.x, M.s.z) == "back" ? "backhand" : "forehand";
            string tag = I.kill ? L.T("file üstü: hızlı kaydır") : I.lunge || I.uzan ? L.T("uzak: uzan") : I.over ? (hnd == "backhand" ? L.T("baş üstü backhand: smaç yok") : L.T("baş üstü: hızlı kaydır = smaç")) : hnd + L.T(": kaydır");
            // r11: topun yanındaki etiket sağ kenarda kesiliyordu; kutu ekran kenarları içinde tutulur.
            float tx = Mathf.Clamp(px - 120 * u, lx, Mathf.Max(lx, rx - 240 * u));
            Shadowed(new Rect(tx, py - R - 26 * u, 240 * u, 18 * u), tag, Stil(small, Mathf.RoundToInt(12 * fu), FontStyle.Bold), hc);
            }
            // Aldatıcı vuruş penceresi (2026-10-26, Core/Aldatma): şu an seviye, ekipman ve konum tutuyorsa vuruşun adı ve jesti,
            // etiketin üstünde. ◆ işareti + metin (yalnız renkle değil), mor = slice/aldatma rengi (Kesici raket). Pencereyi öğretir;
            // jest yapılmazsa normal vuruş çıkar, ceza yok. Büyük yazıda sarılır, kutu ekran kenarı içinde.
            var hf = M.HileFirsat();
            if (hf != null)
            {
                // Metin ve stil vuruş/yazı boyu değişince kurulur; pencere açıkken her kare yeni dize ve GUIStyle ayırmaz (2026-10-28).
                int hb = Mathf.RoundToInt(11 * fu);
                if (hf != hfSon || hb != hfBoy || hfStil == null) { hfSon = hf; hfBoy = hb; hfYazi = "• " + L.T(hf.ad) + " · " + L.T(hf.jest); hfStil = new GUIStyle(Stil(small, hb, FontStyle.Bold)) { wordWrap = true }; }
                float hw = Mathf.Min(rx - lx, 300 * u), hx = Mathf.Clamp(px - hw / 2, lx, Mathf.Max(lx, rx - hw)), hh = hfStil.CalcHeight(Ic(hfYazi), hw);
                Shadowed(new Rect(hx, py - R - (Ogretici ? 28 : 8) * u - hh, hw, hh), hfYazi, hfStil, HF_RENK);
            }
        }
        HileTanim hfSon; int hfBoy; string hfYazi; GUIStyle hfStil; static readonly Color HF_RENK = new Color(0xc9 / 255f, 0xa0 / 255f, 1f);   // #c9a0ff
        static readonly Dictionary<string, string> PREV_TR = new Dictionary<string, string> { { "lift", "Lift" }, { "kill", "File vuruşu" }, { "jump", "Sıçrayarak smaç" }, { "smash", "Smaç" }, { "block", "Blok" }, { "drive", "Drive" }, { "push", "Push" }, { "drop", "Drop" }, { "clear", "Clear" } };
        void HedefYazi(Match M)
        {
            if (root.previewOn && root.Parmak && !root.ParmakVurdu)
            {
                var sp = root.cam.WorldToScreenPoint(Kit.W(root.previewAt.x, root.previewAt.y, .3f)); if (sp.z < 0) return; string t; PREV_TR.TryGetValue(root.previewTip ?? "", out t);
                Shadowed(new Rect(sp.x - 80 * u, H - sp.y - 20 * u, 160 * u, 18 * u), L.T(t ?? root.previewTip), Stil(small, Mathf.RoundToInt(11 * fu), FontStyle.Bold), new Color(.85f, 1, .35f, .9f));
            }
            if (M.ogr != null) { var c = root.fx.HedefMerkez(M); if (c.HasValue) { var sp = root.cam.WorldToScreenPoint(c.Value); if (sp.z > 0) Shadowed(new Rect(sp.x - 60 * u, H - sp.y - 9 * u, 120 * u, 18 * u), L.T("Hedef"), Stil(small, Mathf.RoundToInt(12 * fu), FontStyle.Bold), new Color(.85f, 1, .35f, .85f)); } }
        }
        // Parmak izi (HTML swipeTrail): son 220 ms, vuruşta limon ve kalın, eklemeli.
        void Iz(Match M)
        {
            var tr = root.ParmakIzi; if (tr.Count < 2) return; float now = Time.unscaledTime * 1000; bool hit = root.ParmakVurdu;
            for (int i = 1; i < tr.Count; i++)
            {
                var a = tr[i - 1]; var b = tr[i]; float age = (now - b.z) / 220; if (age > 1) continue;
                var c = hit ? new Color(.85f, 1, .35f, (1 - age) * .9f) : new Color(1, 1, 1, (1 - age) * .55f); float wpx = ((hit ? 14 : 9) * (1 - age) + 1) * RefK;
                Cizgi(new Vector2(a.x * RefK, a.y * RefK), new Vector2(b.x * RefK, b.y * RefK), wpx, c);
            }
        }
        // Vuruş laboratuvarı (2026-10-30): oyuncunun son iki topunun yolu kortta kalır (yenisi limon, öncekisi soluk beyaz),
        // düştüğü yer noktayla. Dünya noktaları tutulur, her karede yeniden yansıtılır (kamera kayınca iz yerinde kalsın).
        readonly List<List<Vector3>> izler = new List<List<Vector3>>(); Shuttle izTop;
        void TopIzi(Match M)
        {
            if (M.Dril == null) { izler.Clear(); izTop = null; return; }
            var s = M.s;
            if (Event.current.type == EventType.Repaint && s != null && s.from == "me" && s.z > 0)
            {
                if (s != izTop) { izTop = s; izler.Add(new List<Vector3>()); if (izler.Count > 2) izler.RemoveAt(0); }
                var l = izler[izler.Count - 1]; var p = Kit.W(s.x, s.y, s.z); if (l.Count == 0 || (l[l.Count - 1] - p).sqrMagnitude > .04f) l.Add(p);
            }
            for (int k = 0; k < izler.Count; k++)
            {
                var l = izler[k]; bool yeni = k == izler.Count - 1; var c = yeni ? new Color(.85f, 1, .35f, .8f) : new Color(1, 1, 1, .35f);
                Vector2 on = Vector2.zero; bool acik = false;
                foreach (var w in l)
                {
                    var sp = root.cam.WorldToScreenPoint(w); if (sp.z <= 0) { acik = false; continue; }
                    var q = new Vector2(sp.x, H - sp.y); if (acik) Cizgi(on, q, (yeni ? 3 : 2) * u, c); on = q; acik = true;
                }
                if (acik && (!yeni || izTop == null || izTop.dead || izTop.z <= 0)) Tex(new Rect(on.x - 5 * u, on.y - 5 * u, 10 * u, 10 * u), disk, c);
            }
        }
        // Vuruş sonuç kartı: her topun ardından 3,5 sn; ilk satır ✓/✗ ve vuruşun adı, ikinci satır ölçüler (file payı, çizgiye
        // uzaklık, hız). Durum yalnız renkle değil işaretle de okunur; satırlar Sigdir ile sığar (Dynamic Type).
        int vkNo = -1; float vkT; string vk1, vk2;
        void VurusKart(Match M, float alt)
        {
            var o = M.drOlcum; if (o == null) return;
            if (M.drOlcumNo != vkNo)
            {
                vkNo = M.drOlcumNo; vkT = Time.unscaledTime;
                vk1 = (o.hedef ? "✓ " : "✗ ") + L.T(o.ad) + " · " + (o.hedef ? L.T("hedefte") : L.T(o.neden ?? "Alana düşmedi"));
                var p = new List<string>();
                if (!o.file && !float.IsNaN(o.gec)) p.Add(L.T("Fileyi {0} cm geçti", Mathf.Max(0, Mathf.RoundToInt(o.gec * 100))));
                if (!o.file && !o.disari) p.Add(L.T(o.dip ? "Dip çizgiye {0} m" : "Fileye {0} m", o.mesafe.ToString("0.0", L.En ? System.Globalization.CultureInfo.InvariantCulture : new System.Globalization.CultureInfo("tr-TR"))));
                p.Add(L.T("{0} km/s", Mathf.RoundToInt(o.hiz)));
                vk2 = string.Join(" · ", p.ToArray());
            }
            float t = Time.unscaledTime - vkT; if (t > 3.5f) return;
            float a = Ayar.reduce ? 1 : Mathf.Clamp01(Mathf.Min(t / .15f, (3.5f - t) / .4f));
            float w = rx - lx, iw = w - 24 * u; var s1 = Glif(Sigdir(leftB, vk1, iw), vk1); var s2 = Sigdir(Stil(left, Mathf.RoundToInt(12 * fu)), vk2, iw);
            float h1 = s1.CalcSize(Ic(vk1)).y, h2 = s2.CalcSize(Ic(vk2)).y, h = h1 + h2 + 16 * u; var r = new Rect(lx, alt - h, w, h);
            Fill(r, new Color(9 / 255f, 16 / 255f, 17 / 255f, .72f * a));
            Fill(new Rect(r.x, r.y, 4 * u, h), o.hedef ? new Color(.85f, 1, .35f, a) : new Color(1, .82f, .48f, a));
            GUI.color = o.hedef ? new Color(.85f, 1, .35f, a) : new Color(1, .82f, .48f, a); GUI.Label(new Rect(r.x + 14 * u, r.y + 7 * u, iw, h1), vk1, s1);
            GUI.color = new Color(.81f, .89f, .86f, a); GUI.Label(new Rect(r.x + 14 * u, r.y + 8 * u + h1, iw, h2), vk2, s2); GUI.color = Color.white;
        }
        void Cizgi(Vector2 a, Vector2 b, float w, Color c)
        {
            var d = b - a; float l = d.magnitude; if (l < .5f) return; float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            var m = GUI.matrix; GUIUtility.RotateAroundPivot(ang, a); Fill(new Rect(a.x, a.y - w / 2, l, w), c); GUI.matrix = m;
        }
        void KesikCizgi(Vector2 a, Vector2 b, float w, Color c, float on, float off)
        {
            var d = b - a; float l = d.magnitude; if (l < 1) return; var n = d / l;
            for (float t = 0; t < l; t += on + off) Cizgi(a + n * t, a + n * Mathf.Min(l, t + on), w, c);
        }
        // Antrenman: canlı parmak izi (kesikli yol + onu izleyen nokta), bilgi adımı çerçevesi, antrenör çağrısı.
        void OgrIz(Match M)
        {
            var o = M.ogr; var a = M.OgrA(); if (a == null || root.paused) return; float now = Time.unscaledTime, nab = Ayar.reduce ? 1 : .6f + .4f * Mathf.Sin(now * 6), K = RefK;
            if (a.bilgi != null)
            {
                if (a.bilgi == "nefes")
                {
                    var sp = root.cam.WorldToScreenPoint(Kit.W(M.me.x, M.me.y, 0)); float uu = root.shuttle.PxPerM(Kit.W(M.me.x, M.me.y, 0)) / 28f * K; float px = sp.x, py = H - sp.y;
                    Tex(new Rect(px - 34 * uu, py - 16 * uu, 68 * uu, 32 * uu), ringK, new Color(.85f, 1, .35f, nab));
                    Ok(new Vector2(px + 40 * uu, py - 40 * uu), new Vector2(px + 26 * uu, py - 14 * uu), new Color(.85f, 1, .35f, nab), false);
                }
                else Ok(new Vector2(W / 2, H - bot - 80 * K), new Vector2(W / 2, H - bot - 30 * K), new Color(.85f, 1, .35f, nab), false);
                return;
            }
            if (a.drill)
            {
                if (o.cagri == null || M.s == null || M.s.dead || M.s.from != "op") return;
                var p = root.cam.WorldToScreenPoint(Kit.W(M.me.x, M.me.y, 0)); float yy = Mathf.Min(H - p.y - 120 * K, H * .62f), b = Ayar.reduce ? 1 : 1 + .06f * Mathf.Max(0, 1 - (M.realClock - o.cagriT) * 3);
                Shadowed(new Rect(lx, yy - 44 * K, rx - lx, 20 * K), L.T("Antrenör diyor"), Stil(small, Mathf.RoundToInt(12 * fu), FontStyle.Bold), Kit.Hex("#cfe3dc"));
                Shadowed(new Rect(lx, yy - 24 * K, rx - lx, 40 * K), UiKok.Buyuk(L.T(Match.OGR_AD[o.cagri])) + "!", Stil(big, Mathf.RoundToInt(30 * fu * b)), Kit.Hex("#d8ff5a"));
                return;
            }
            // görev: hareket yalnız ilk topta bir kez gösterilir (Core OgrDurdur gösterim topu; serviste ilk servis), sonra oyuncu dener
            if (a.dril != null) { if (a.iz == null || (a.servis != null ? o.dene > 0 : !o.dur)) return; }
            else if (o.n >= 1 && o.miss < 2) return;   // ustalık: ikinci deneme izsiz
            Vector2 o0;
            if (a.servis != null) { if (M.s != null || M.svPending != null) return; var p = root.cam.WorldToScreenPoint(Kit.W(M.me.x, M.me.y, .9f)); o0 = new Vector2(p.x, H - p.y + 30 * K); }
            else { if (!o.dur || M.s == null) return; var p = root.cam.WorldToScreenPoint(Kit.W(M.s.x, M.s.y, M.s.z)); o0 = new Vector2(p.x, H - p.y + (a.iz == "asagi" ? -50 : 45) * K); }
            var BOY = new Dictionary<string, float> { { "yavas", 90 }, { "fiske", 60 }, { "uzun", 170 }, { "hizli", 150 }, { "yay", 150 }, { "asagi", 100 } };
            var HIZ = new Dictionary<string, float> { { "yavas", 1.7f }, { "fiske", .45f }, { "uzun", .9f }, { "hizli", .42f }, { "yay", .9f }, { "asagi", .5f } };
            float boy = BOY[a.iz ?? "uzun"] * K, hiz = HIZ[a.iz ?? "uzun"], yon = a.iz == "asagi" ? 1 : -1;
            Vector2 e = new Vector2(o0.x, o0.y + yon * boy), c = new Vector2(o0.x + (a.iz == "yay" ? boy * .55f : 0), (o0.y + e.y) / 2);
            System.Func<float, Vector2> nk = t => { float uu = 1 - t; return uu * uu * o0 + 2 * uu * t * c + t * t * e; };
            for (int i = 0; i < 24; i += 2) Cizgi(nk(i / 24f), nk((i + 1) / 24f), 3 * K, new Color(1, 1, 1, .75f));
            Ok(nk(.97f), e, Color.white, true);
            if (a.dril != null) { var bs = Stil(small, Mathf.RoundToInt(13 * fu), FontStyle.Bold); string bt = L.T("Böyle kaydır, sonra sen dene"); float lx2 = Mathf.Clamp(o0.x + 24 * K, lx, rx - 200 * K); Shadowed(new Rect(lx2, (o0.y + e.y) / 2 - 10 * K, 200 * K, 20 * K), bt, bs, Kit.Hex("#d8ff5a")); }
            float dur = hiz + .7f, tt = Ayar.reduce ? 1 : Mathf.Min(1, (now % dur) / hiz); var q = nk(tt); float r = 11 * K;
            Tex(new Rect(q.x - r - 2, q.y - r - 2, 2 * r + 4, 2 * r + 4), disk, new Color(8 / 255f, 16 / 255f, 17 / 255f, .8f)); Tex(new Rect(q.x - r, q.y - r, 2 * r, 2 * r), disk, new Color(.85f, 1, .35f, .95f));
        }
        void Ok(Vector2 a, Vector2 b, Color c, bool kisa)
        {
            float k = 12 * RefK, ang = Mathf.Atan2(b.y - a.y, b.x - a.x); if (!kisa) Cizgi(a, b, 3 * RefK, c);
            Cizgi(b, b - new Vector2(Mathf.Cos(ang - .5f), Mathf.Sin(ang - .5f)) * k, 3 * RefK, c); Cizgi(b, b - new Vector2(Mathf.Cos(ang + .5f), Mathf.Sin(ang + .5f)) * k, 3 * RefK, c);
        }
        // Çizgi kamerası (HTML hawkEye): ekranın üstünde yaylı büyüyen daire, içinde düşüş noktası 1 m = 300 px büyütmeyle,
        // çizgiler (4 cm, alanın içinde), .36 sn sonra mantar izi (Ø 26 mm) ve karar santimle.
        void HawkCiz(Match M)
        {
            var h = hawk; float e = Mathf.Min(1, h.t / .18f), k = 1 + 2.7f * Mathf.Pow(e - 1, 3) + 1.7f * Mathf.Pow(e - 1, 2), fade = h.t > 1.25f ? 1 - (h.t - 1.25f) / .25f : 1;
            float R = 74 * RefK * k, cx = W / 2, cy = H * .22f; var sa = CourtView.SAHA[M.saha];
            Tex(new Rect(cx - R - 4, cy - R - 4, 2 * R + 8, 2 * R + 8), disk, new Color(8 / 255f, 16 / 255f, 17 / 255f, .9f * fade));
            var kc = Kit.Hex(sa.kort); kc.a = fade; Tex(new Rect(cx - R, cy - R, 2 * R, 2 * R), disk, kc);
            float S = 300 * RefK * k, lw = .04f * S; float x0, x1, y0, y1; M.SinirPublic(h.serve, out x0, out x1, out y0, out y1);
            System.Action<float, float, float, float> blok = (ax, ay, bw, bh) =>
            {   // daireye kırpılmış dikdörtgen: kaba kırpma (yalnız daire içi)
                var r = new Rect(cx + ax, cy + ay, bw, bh); float dx = Mathf.Max(Mathf.Abs(r.center.x - cx) - r.width / 2, 0), dy = Mathf.Max(Mathf.Abs(r.center.y - cy) - r.height / 2, 0);
                if (dx * dx + dy * dy > R * R) return; var rr = Rect.MinMaxRect(Mathf.Max(r.xMin, cx - R * .92f), Mathf.Max(r.yMin, cy - R * .92f), Mathf.Min(r.xMax, cx + R * .92f), Mathf.Min(r.yMax, cy + R * .92f)); if (rr.width > 0 && rr.height > 0) Fill(rr, new Color(1, 1, 1, fade));
            };
            blok((x0 - h.x) * S, -R, lw, 2 * R); blok((x1 - h.x) * S - lw, -R, lw, 2 * R); blok(-R, -(y0 - h.y) * S - lw, 2 * R, lw); blok(-R, -(y1 - h.y) * S, 2 * R, lw);
            if (h.t > .36f) { float q = Mathf.Min(1, (h.t - .36f) / .12f), rc = .013f * S * q; Tex(new Rect(cx - rc, cy - rc, 2 * rc, 2 * rc), disk, h.icerde ? new Color(.85f, 1, .35f, .95f * fade) : new Color(1, .48f, .42f, .95f * fade)); }
            Tex(new Rect(cx - R - 2, cy - R - 2, 2 * R + 4, 2 * R + 4), ringK, new Color(1, 1, 1, fade));
            Shadowed(new Rect(cx - 100 * u, cy - R - 22 * u, 200 * u, 16 * u), L.T("ÇİZGİ KAMERASI"), tiny, new Color(.62f, .73f, .7f, fade));
            if (h.t > .36f) Shadowed(new Rect(cx - 150 * u, cy + R + 6 * u, 300 * u, 28 * u), L.T(h.icerde ? "İÇERİ" : "DIŞARI") + " · " + h.cm + " cm", Stil(mid, Mathf.RoundToInt(20 * fu)), h.icerde ? new Color(.85f, 1, .35f, fade) : new Color(1, .48f, .42f, fade));
        }
        void NefesYazi(Athlete o, bool isMe, Match M)
        {
            if (o.nefes >= .4f || M.ogr != null) return;
            var sp = root.cam.WorldToScreenPoint(Kit.W(o.x + (isMe ? .9f : -.9f), o.y, 0)); if (sp.z < 0) return;
            Shadowed(new Rect(sp.x - 50 * u, H - sp.y - 10 * u, 100 * u, 20 * u), L.T(isMe ? "Nefes" : "Yorgun"), small, Kit.Hex("#ff9f7a"));
        }
        // İlk Usta maçı kartı (2026-10-31 Ar-Ge U4, yasin "ilerle ve devam et"): Usta'ya ilk giren oyuncu ana ekrandaki tek satır
        // dışında koşunun kendisinde olduğunu, nasıl koşulduğunu ve iki halkanın anlamını bilmiyordu. Yalnız ilk Usta maçında,
        // ilk sayı bitene dek görünür (oyunu durdurmaz, ralli başlayınca da okunur); sonra tuytop_usta_kart ile bir daha çıkmaz.
        const string USTA_KART = "tuytop_usta_kart"; int ustaKart = -1; GUIStyle ustaKartSt;
        public void UstaKartSifirla() { PlayerPrefs.DeleteKey(USTA_KART); ustaKart = -1; }   // ekran turu
        float UstaKart(Match M, float y)
        {
            if (M.easy || M.Dril != null) return y;
            if (ustaKart < 0) ustaKart = PlayerPrefs.GetInt(USTA_KART, 0);
            if (ustaKart > 0) return y;
            if (M.sc[0] + M.sc[1] > 0) { ustaKart = 1; PlayerPrefs.SetInt(USTA_KART, 1); PlayerPrefs.Save(); return y; }
            if (ustaKartSt == null || ustaKartSt.fontSize != Mathf.RoundToInt(13 * fu)) ustaKartSt = new GUIStyle(left) { wordWrap = true, fontSize = Mathf.RoundToInt(13 * fu) };
            float w = ColW, x = ColX, iw = w - 32 * u;
            string bas = L.T("Usta: koşu senin"), metin = L.T("Ayak izlerinin yanına dokun, oyuncun yerini alır; başka yere dokunursan oraya koşar. Top parlayınca kaydır.") + "\n" + L.T("Düz halkanın içindeysen tam vuruş, kesik halkadaysan uzanırsın.");
            var bs = Stil(leftB, Mathf.RoundToInt(18 * fu)); float bh = bs.CalcHeight(Ic(bas), iw), mh = ustaKartSt.CalcHeight(Ic(metin), iw), h = bh + mh + 24 * u;
            var r = new Rect(x, y, w, h); GUI.Box(r, GUIContent.none, card);
            var o = GUI.color; GUI.color = Kit.Hex("#d8ff5a"); GUI.DrawTexture(new Rect(x, y, 6 * u, h), Texture2D.whiteTexture); GUI.color = o;
            Etk(new Rect(x + 16 * u, y + 8 * u, iw, bh), bas, bs);
            GUI.Label(new Rect(x + 16 * u, y + 12 * u + bh, iw, mh), metin, ustaKartSt);
            return y + h + 8 * u;
        }
        float RakipKart(Match M, float y)
        {
            // HTML rkGir: yukarıdan 10 px, solarak, .22 sn (hareketi azaltta anında)
            float gk = Ayar.reduce ? 1 : Mathf.Clamp01((1.5f - root.rakipKartT) / .22f); gk = 1 - (1 - gk) * (1 - gk); float y0 = y; y -= (1 - gk) * 10 * u;
            GUI.contentColor = GUI.backgroundColor = new Color(1, 1, 1, gk);
            var st = M.stil; float w = ColW, x = ColX, h = 92 * u; var r = new Rect(x, y, w, h); blocks.Add(r); GUI.Box(r, GUIContent.none, card);
            var oc = Kit.Hex(Match.OpRenk(st, Ayar.FORMA)); oc.a = gk; GUI.color = oc; GUI.DrawTexture(new Rect(x, y, 6 * u, h), Texture2D.whiteTexture); GUI.color = Color.white;
            Etk(new Rect(x + 16 * u, y + 8 * u, w - 24 * u, 26 * u), st.isim, Stil(leftB, Mathf.RoundToInt(20 * fu)));
            var kb = KariyerVeri.Al(st.kariyer); var im = st.patron ? KariyerVeri.Imza(st.imza) : null;   // serbest maçta taban tarz imzayı taşır ama atmaz
            Etk(new Rect(x + 16 * u, y + 34 * u, w - 24 * u, 18 * u), kb != null ? L.T(st.ad) + " · " + L.T(kb.yer) : L.T(st.ad), left);
            // Patron (final): ipucu yerine imza vuruşu ve cevabı (tasarım §3.3: kart rakibin tek fikrini söyler; finalde o fikir imzadır)
            Etk(new Rect(x + 16 * u, y + 54 * u, w - 24 * u, 36 * u), im != null ? L.T(im.ad) + ": " + L.T(im.cevap) : L.T(st.ipucu), new GUIStyle(left) { wordWrap = true, fontSize = Mathf.RoundToInt(12 * fu) });
            GUI.contentColor = GUI.backgroundColor = Color.white;
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) root.rakipKartT = 0;   // dokununca hemen kapanır
            return y0 + h + 8 * u;
        }
        float OgrKart(Match M, float y)
        {
            var o = M.ogr; var a = M.OgrA(); if (a == null) return y;
            float w = ColW, x = ColX, iw = w - 24 * u;
            string ilr = a.drill ? L.T(" · Top {0}/{1} · Doğru {2}", o.dene, Match.OGR_CAGRI, o.isabet) : a.bilgi != null ? "" : " · " + new string('●', o.n) + new string('○', Match.OGR_TEKRAR - o.n) + " " + o.n + "/" + Match.OGR_TEKRAR;
            string say = L.T("Antrenman {0}/{1}{2}", o.i + 1, o.liste.Count, ilr);
            // 2026-10-26 antrenman görevi: sayaç top/isabet ve yıldız eşikleri (sayıyla; yıldız simgesi tek başına okunmaz)
            if (a.dril != null) say = L.T("Görev · Top {0}/{1} · Hedefte {2} · Yıldız eşikleri {3}", Mathf.Min(o.dene, a.dril.top), a.dril.top, o.isabet, string.Join("/", System.Array.ConvertAll(a.dril.esik, e => e.ToString())));
            string met = (o.izsiz ? L.T("Bir daha, bu sefer iz yok. ") : "") + (a.servis == null && a.bilgi == null && !a.drill && a.dril == null && !o.dur ? L.T("Top sana gelince oyun duracak. ") : "") + L.T(a.met);
            if (leftWrap == null || leftWrapTaban != left) { leftWrap = new GUIStyle(left) { wordWrap = true }; leftWrapTaban = left; } var ws = leftWrap; var ns = Stil(ws, Mathf.RoundToInt(11 * fu));
            float h1 = ws.CalcHeight(Ic(met), iw), h2 = ns.CalcHeight(Ic(L.T(a.neden)), iw);
            int? hb = M.OgrHedefBolge(a); bool bant = hb.HasValue;
            // canlı okuma: parmak ekrandayken "şu an bıraksan ne çıkar", sonra 2,2 sn "Çıkan"
            string tip; float sp; root.CanliOkuma(out tip, out sp); bool canliVar = root.Parmak && (tip != null || sp > 0); string canli = null; bool ok = false; float igne = -1;
            if (a.bilgi == null && !(a.dril != null && a.dril.olcu == "split"))   // split görevinde vuruş türü ölçülmez: canlı okuma yanıltır
            {
                if (canliVar) { ok = M.OgrUygunCanli(a, tip, false); igne = sp; canli = (ok ? "✓ " : "✗ ") + L.T("Şu an") + ": " + (tip != null ? L.T(Match.OGR_AD.ContainsKey(tip) ? Match.OGR_AD[tip] : tip) : L.T("Yukarı kaydır")); }
                else if (o.sonSp >= 0 && M.realClock - o.sonT < 2.2f) { ok = M.OgrUygunCanli(a, o.sonTip, o.sonSlice); igne = o.sonSp; canli = (ok ? "✓ " : "✗ ") + L.T("Çıkan") + ": " + (o.sonTip != null ? (o.sonSlice ? "Slice" : L.T(Match.OGR_AD.ContainsKey(o.sonTip) ? Match.OGR_AD[o.sonTip] : o.sonTip)) : ""); }
                else if (a.drill && o.cagri != null && M.s != null && M.s.from == "op") canli = L.T("Antrenör: ") + L.T(Match.OGR_AD[o.cagri]);
            }
            // Dar kart (2026-10-29, yasin: "antrenman kısmında instructionlar ekranı kaplıyor hiç bir şey görünmüyor"): tam kart
            // ekranın üst yarısını, yani hedef köşelerin ve rakip sahasının olduğu yeri örtüyordu. Talimat yalnız adımın ilk topundan
            // önce ya da "?" ile açılır; oynarken tek satır başlık + sayaç, hız bandı ve canlı okuma kalır (asıl geri bildirim).
            // Bilgi adımları (Anladım, devam) oyun durduğu için tam kalır.
            string adimAnah = o.i + "/" + (a.dril != null ? a.dril.id : "");
            if (adimAnah != ogrAdimAnah) { ogrAdimAnah = adimAnah; ogrDetay = false; }
            bool ilkTop = a.dril != null || a.drill ? o.dene == 0 : o.n == 0 && o.miss == 0;
            // 2026-10-30 (yasin: "ilk vuruşta da yazılar çok yer kaplıyor"): ilk top da dar karttır; tam kartın sayaç satırı,
            // "neden" paragrafı ve ayrı Görevi bitir satırı yerine başlık satırının altında yalnız talimat (met) durur. Neden ve
            // yıldız eşikleri "?" ile açılan tam kartta kalır.
            bool kompakt = a.bilgi == null && !ogrDetay;
            if (kompakt) return OgrKartDar(M, a, y, x, w, iw, bant, hb, canli, igne, ilkTop ? met : null);
            float h = 12 * u + 18 * u + 24 * u + h1 + 6 * u + h2 + (bant ? 34 * u : 0) + (canli != null ? 22 * u : 0) + 10 * u + BH + (a.bilgi != null ? BH + 8 * u : 0) + 10 * u;
            var r = new Rect(x, y, w, h); blocks.Add(r); GUI.Box(r, GUIContent.none, card);
            if (a.bilgi == null && Button(new Rect(x + w - 12 * u - BH, y + 6 * u, BH, BH), "−")) ogrDetay = false;
            float yy = y + 10 * u;
            Etk(new Rect(x + 12 * u, yy, iw, 18 * u), say, Stil(left, Mathf.RoundToInt(11 * fu))); yy += 18 * u;
            Etk(new Rect(x + 12 * u, yy, iw, 24 * u), L.T(a.bas), leftB); yy += 24 * u;
            GUI.Label(new Rect(x + 12 * u, yy, iw, h1), met, ws); yy += h1 + 6 * u;
            var nc = GUI.color; GUI.color = new Color(1, 1, 1, .7f); GUI.Label(new Rect(x + 12 * u, yy, iw, h2), L.T(a.neden), ns); GUI.color = nc; yy += h2;
            if (bant) { yy += 6 * u; HizBandi(a, hb.Value, igne, x + 12 * u, yy, iw); yy += 28 * u; }
            if (canli != null) { GUI.color = canli.StartsWith("✓") ? Kit.Hex("#d8ff5a") : canli.StartsWith("✗") ? Kit.Hex("#ffd27a") : Kit.Hex("#cfe3dc"); Etk(new Rect(x + 12 * u, yy + 2 * u, iw, 20 * u), canli, leftB); GUI.color = Color.white; yy += 22 * u; }
            yy += 10 * u;
            if (a.bilgi != null) { if (Button(new Rect(x + 12 * u, yy, iw, BH), "Anladım, devam", true)) M.OgrBilgiGec(); yy += BH + 8 * u; }
            if (Button(new Rect(x + 12 * u, yy, iw, BH), a.dril != null ? "Görevi bitir" : "Antrenmanı geç")) root.OgrGec();
            return y + h + 8 * u;
        }

        string ogrAdimAnah; bool ogrDetay;
        // hız bandı: bölgeler oyunun kendi eşikleri; hedef bölge ▸ ile ve çerçeveyle (yalnız renkle değil)
        void HizBandi(OgrAdim a, int hb, float igne, float bx, float yy, float bwid)
        {
            var sinir = Match.OgrBolgeSinir(a); var adlar = Match.OgrBolgeAd(a); float b0 = 0;
            for (int i = 0; i < adlar.Length; i++)
            {
                float b1 = i < sinir.Length ? sinir[i] : Match.OGR_MAX; var zr = new Rect(bx + bwid * b0 / Match.OGR_MAX, yy, bwid * (b1 - b0) / Match.OGR_MAX - 2 * u, 22 * u); bool hed = i == hb;
                Fill(zr, hed ? new Color(.85f, 1, .35f, .28f) : new Color(1, 1, 1, .08f)); if (hed) Cerceve(zr, new Color(.85f, 1, .35f, .8f), 1.5f);
                string ad = (hed ? "› " : "") + L.T(adlar[i]); GUI.Label(zr, ad, Glif(Sigdir(Stil(tiny, -1, hed ? FontStyle.Bold : FontStyle.Normal), ad, zr.width - 2), ad)); b0 = b1;
            }
            if (igne >= 0) { float ix = bx + bwid * Mathf.Min(igne, Match.OGR_MAX) / Match.OGR_MAX; Fill(new Rect(ix - 1.5f * u, yy - 4 * u, 3 * u, 30 * u), Color.white); }
        }
        // Dar antrenman kartı: [başlık · sayaç] [?] [Bitir] tek satır (44 pt düğmeler), altında hız bandı ve canlı okuma.
        float OgrKartDar(Match M, OgrAdim a, float y, float x, float w, float iw, bool bant, int? hb, string canli, float igne, string met = null)
        {
            var o = M.ogr; float bw = BH, bitW = Mathf.Max(BH, btn.CalcSize(Ic(L.T("Bitir"))).x + 24 * u);   // düğmenin kendi yazı stiliyle (small ile ölçünce "Finis h" kırılıyordu)
            var ms = met != null && leftWrap != null ? Stil(leftWrap, Mathf.RoundToInt(12 * fu)) : null;   // leftWrap: çağıran OgrKart kurar (sarmalı)
            float hm = ms != null ? ms.CalcHeight(Ic(met), iw) + 4 * u : 0;
            float h = 8 * u + BH + hm + (bant ? 30 * u : 0) + (canli != null ? 22 * u : 0) + 8 * u;
            var r = new Rect(x, y, w, h); blocks.Add(r); Fill(r, new Color(9 / 255f, 16 / 255f, 17 / 255f, .62f));
            float yy = y + 8 * u, tw = iw - bw - bitW - 12 * u;
            string sayac = a.dril != null ? L.T("Top {0}/{1} · Hedefte {2}", Mathf.Min(o.dene, a.dril.top), a.dril.top, o.isabet) : a.drill ? L.T("Top {0}/{1} · Doğru {2}", o.dene, Match.OGR_CAGRI, o.isabet) : o.n + "/" + Match.OGR_TEKRAR;
            string bas = L.T(a.bas); var bs = Sigdir(leftB, bas, tw); Etk(new Rect(x + 12 * u, yy, tw, BH * .58f), bas, bs);
            var ss = Sigdir(Stil(left, Mathf.RoundToInt(11 * fu)), sayac, tw); Etk(new Rect(x + 12 * u, yy + BH * .55f, tw, BH * .45f), sayac, ss);
            if (Button(new Rect(x + w - 12 * u - bitW - bw - 6 * u, yy, bw, BH), "?")) ogrDetay = true;
            if (Button(new Rect(x + w - 12 * u - bitW, yy, bitW, BH), "Bitir")) root.OgrGec();
            yy += BH;
            if (ms != null) { GUI.Label(new Rect(x + 12 * u, yy + 2 * u, iw, hm), met, ms); yy += hm; }
            if (bant) { yy += 6 * u; HizBandi(a, hb.Value, igne, x + 12 * u, yy, iw); yy += 24 * u; }
            if (canli != null) { GUI.color = canli.StartsWith("✓") ? Kit.Hex("#d8ff5a") : canli.StartsWith("✗") ? Kit.Hex("#ffd27a") : Kit.Hex("#cfe3dc"); Etk(new Rect(x + 12 * u, yy + 2 * u, iw, 20 * u), canli, Sigdir(leftB, canli, iw)); GUI.color = Color.white; }
            return y + h + 8 * u;
        }

        // Duraklat (2026-10-30 Ar-Ge #1, yasin "yap"): maç sonu ekranının Tüycükler dili. Eskisi Unity'nin varsayılan yazısı ve
        // ekran boyu dört düz renk bloktu (r71 duraklat). Koyu mürekkep örtü, Baloo başlık, Nunito bilgi satırı; "Maça devam et"
        // maç sonundaki büyük turuncu düğme (alt satırında skor), diğerleri lacivert yuvarlak ADugme, hepsi kart genişliğinde.
        // Ses düğmesi durumu yazıyla söyler (yalnız renkle değil). Dikey: blok ortada, sığmazsa üstten başlar, alt güvenli alanı aşmaz.
        float bastanOnayT = -1;
        void Pause(Match M)
        {
            float s = Mathf.Min(W / 390f, H / 844f), kt = W / H < .62f ? Mathf.Max(s, 11f / 12f * W / 375f) : s, k = kt * Ayar.YaziK; YayinStil(s, k);
            var o0 = GUI.color; GUI.color = new Color(A_INK.r, A_INK.g, A_INK.b, .86f); GUI.DrawTexture(new Rect(-W, 0, W * 3, H), Texture2D.whiteTexture); GUI.color = o0;
            float x = ColX, w = ColW, bh = Mathf.Max(BH, 50 * s), gap = 10 * s;
            string bas = L.T("Duraklatıldı"), bilgi;
            if (M.Dril != null) bilgi = L.T(M.Dril.ad) + " · " + L.T("Top {0}/{1} · Hedefte {2}", Mathf.Min(M.ogr.dene, M.Dril.top), M.Dril.top, M.ogr.isabet);
            else bilgi = L.T("{0} sayı", M.target) + " · " + L.T("Rakip") + " " + M.RakipAd(false) + (KariyerVeri.Var(M.stil.kariyer) ? " · " + L.T(KariyerVeri.Al(M.stil.kariyer).yer) : "");
            string skor = M.Dril != null ? null : Sayi(M.sc[0]) + " – " + Sayi(M.sc[1]);
            var bst = Sigdir(aSkor, bas, w); float bhY = bst.CalcHeight(Ic(bas), w), sh = skor == null ? 0 : aSkor.CalcHeight(Ic(skor), w), ih = aIpucu.CalcHeight(Ic(bilgi), w);
            string ct = UiKok.Buyuk(L.T("Maça devam et")); float cth = Sigdir(aCta, ct, w - 100 * s).CalcHeight(Ic(ct), w - 100 * s), ctaH = Mathf.Max(80 * s, cth + 22 * s);
            float blok = bhY + 6 * s + sh + ih + 22 * s + ctaH + 12 * s + 3 * bh + 2 * gap;
            float y = Mathf.Max(top + BH + 8 * s, Mathf.Min((H - blok) / 2, H - bot - blok));
            Yaz(new Rect(x, y, w, bhY), bas, bst, A_YAZI, 1); y += bhY + 6 * s;
            if (skor != null)
            {   // skor tabelası Baloo: senin sayın limon (yön C: limon = sen), rakip yazı rengi
                string a0 = Sayi(M.sc[0]), b0 = " – " + Sayi(M.sc[1]); float aw = aSkor.CalcSize(Ic(a0)).x, bw = aSkor.CalcSize(Ic(b0)).x, sx = W / 2 - (aw + bw) / 2;
                Yaz(new Rect(sx, y, aw + 2, sh), a0, aSkor, A_LIMON, 1); Yaz(new Rect(sx + aw, y, bw + 2, sh), b0, aSkor, A_YAZI, 1); y += sh;
            }
            Yaz(new Rect(x, y, w, ih), bilgi, aIpucu, A_SOLUK, 1); y += ih + 22 * s;
            if (BuyukDugme(new Rect(x, y, w, ctaH), ct, null, s, 1)) { bastanOnayT = -1; root.Devam(); } y += ctaH + 12 * s;
            // HTML duraklat satırı: Ses · Maçı baştan başlat · Ana menü (oyun sırasında ses düğmesi yalnız burada)
            if (ADugme(new Rect(x, y, w, bh), L.T(Ayar.ses ? "Ses açık" : "Ses kapalı"), 1)) { Ayar.ses = !Ayar.ses; AudioListener.volume = Ayar.ses ? 1 : 0; Ayar.Kaydet(); Yerel.Hap("tick"); } y += bh + gap;
            // "Maçı baştan başlat" onayı (2026-10-31 Ar-Ge UI/UX #2, yasin "yap"): Devam'ın hemen altında tek dokunuşta skoru
            // siliyordu. Maç ilerlemişse (skor 0–0 değil / antrenmanda atılmış top var) ilk dokunuş düğmeyi turuncu çerçeveye ve
            // "Emin misin? Bir kez daha dokun" yazısına çevirir (renk + yazı, yalnız renk değil); 2,5 sn içinde ikinci dokunuş
            // başlatır, süre geçerse düğme eski hâline döner. 0–0'da onay yok (kaybedilecek bir şey yok). Simge sırası önerisi
            // reddedildi: yazılı üç satır her dilde daha okunur.
            bool ilerledi = M.Dril != null ? M.ogr != null && M.ogr.dene > 0 : M.sc[0] + M.sc[1] > 0, onay = bastanOnayT >= 0 && Time.unscaledTime - bastanOnayT < 2.5f;
            var rb = new Rect(x, y, w, bh);
            bool bastan = ADugme(rb, L.T(onay ? "Emin misin? Bir kez daha dokun" : "Maçı baştan başlat"), 1);
            if (onay) KutuCiz(rb, aKutuLimon, 1);   // lacivert kutunun üstüne turuncu çerçeve
            if (bastan)
            {
                if (!ilerledi || onay) { bastanOnayT = -1; root.TekrarOyna(); }
                else { bastanOnayT = Time.unscaledTime; Yerel.Hap("tick"); }
            }
            y += bh + gap;
            if (ADugme(new Rect(x, y, w, bh), L.T("Ana menü"), 1)) root.AnaMenu();
        }

        // ---- maç sonu (2026-10-26, arayüz yönü A "Yayın Gecesi", tasarım ph4; yasin yönü henüz onaylamadı) ----
        // Sıra yukarıdan: üst satır (tur · basamak · yer), büyük başlık (GALİBİYET limon; kayıpta "Bu sefer rakip aldı" beyaz,
        // suçlamayan ton aynı), skor (senin sayın limon, solda halka + "SEN", sağda rakip halkası + adı: yalnız renkle değil),
        // üç büyük yıldız ve koşulları (✓/✗ simgesi + yazı; dolu/çerçeve biçim), Tüy sayarak artar, XP çubuğu dolar, seviye
        // atladıysan "SEVİYE ATLADIN · Sv n", ilk galibiyetin açtığı şey limon çerçeveli kartta, öneri satırı, turun basamak
        // çizgisi. Altta (başparmak bölgesi) büyük eğik düğme (eski birincil düğmenin metni ve işi: Sıradaki maç / Tekrar dene /
        // Bir maç daha) ve alt satırında sıradaki basamak · rakip; ikincil düğmeler (Ekipmanı yükselt, Tam antrenman, önerilen
        // görev, Antrenman görevleri) aynı koşullarla; en altta Ayrıntılar · Ana menü yan yana. Ayrıntılar açıkken yıldız ve
        // ödül alanının yerine iki özet kartı ve tam liste gelir (eski ekranda özet kartları hep açıktı; yer kalmadı).
        // Hareket: ışınlar döner (30 sn'de bir tur), başlık yaylanır, yıldızlar sırayla iner (her yenisi hafif titreşim), Tüy
        // sayar. Hareketi azalt: ışın ve iniş yok, sayılar son değerinde, bütün içerik .25 sn'de solarak gelir.
        // Yazı: 2026-10-29 Baloo 2 + Nunito (OFL, Resources/fonts; eskisi Barlow); boyut fu ile (Dynamic Type ×1,3'e kadar, HUD sınırı). ★ ✓ ✗ glif değil doku.
        Font fSiyah, fEkstra, fYari, fKalin; float aS = -1, aK = -1;
        GUIStyle aBas, aUst, aSkor, aAd, aKosul, aSatir, aSayi, aLv, aUnl, aCta, aCtaAlt, aDug, aIpucu, aKutuS, aKutuIkon, aKutuLimon, aCtaS, aIz;
        float overT0 = -1; bool overAcik; int yildizHap;
        // 2026-10-27 yön C "Gün Batımı Salonu" (yasin karar kartı): menü USS kökündeki değerler. Adlar A'dan; anlam: A_LIMON yalnız
        // "sen" (senin skorun, halkan, "SEN", GALİBİYET başlığı, senin özet sayıların), A_TURUNCU ilerleme ve kazanım (ışınlar,
        // ✓, XP, seviye, açılan kart, basamak çizgisi), A_MERCAN rakip (değeri orkide: birincil eylemin turuncusundan ayrı).
        static readonly Color A_INK = Kit.Hex("#1A1221"), A_LIMON = Kit.Hex("#C9EE4B"), A_LIMON_KOYU = Kit.Hex("#141A08"), A_ALTIN = Kit.Hex("#F9D272"), A_MERCAN = Kit.Hex("#E99BFF"),
            A_YAZI = Kit.Hex("#F3EAEE"), A_SOLUK = Kit.Hex("#CBB8C5"), A_NAVY = Kit.Hex("#261A30"), A_NAVY3 = Kit.Hex("#33233F");
        static readonly Color A_TURUNCU = Kit.Hex("#FFA04D"), A_CTA_MUREKKEP = Kit.Hex("#1E0A0C"), A_CTA_OK = Kit.Hex("#FF8A57");
        GUIStyle AStil(Font f, float px, TextAnchor hiza, bool sar)
        {
            var st = new GUIStyle(GUI.skin.label) { font = f, fontSize = Mathf.Max(8, Mathf.RoundToInt(px)), alignment = hiza, wordWrap = sar, richText = false, fontStyle = FontStyle.Normal, clipping = TextClipping.Overflow };
            st.normal.textColor = Color.white; st.padding = new RectOffset(0, 0, 0, 0); st.margin = new RectOffset(0, 0, 0, 0); return st;
        }
        void FontYukle()
        {
            if (fSiyah != null) return;
            fSiyah = Resources.Load<Font>("fonts/Baloo2-ExtraBold"); fEkstra = Resources.Load<Font>("fonts/Baloo2-ExtraBold");
            fYari = Resources.Load<Font>("fonts/Nunito-Bold"); fKalin = Resources.Load<Font>("fonts/Nunito-ExtraBold");
        }
        // Maç içi stillerin ağırlığı (2026-10-30 Ar-Ge #1): GUIStyle'lar yazı tipi atamadığı için Unity'nin varsayılan yazısıyla
        // ve yapay kalın/eğik görünümle çiziliyordu. Nunito tek ağırlıklı dosyalar: Bold istenen Nunito-ExtraBold, Normal Nunito-Bold;
        // Baloo tek ağırlık. fontStyle hep Normal (yapay kalınlık yok).
        void Agirlik(GUIStyle st)
        {
            if (st.font == null) return;
            if (st.font == fYari || st.font == fKalin) st.font = st.fontStyle == FontStyle.Bold || st.fontStyle == FontStyle.BoldAndItalic ? fKalin : fYari;
            st.fontStyle = FontStyle.Normal;
        }
        GUIStyle AKutu(Texture2D t, int b) { var st = new GUIStyle(); st.normal.background = t; st.border = new RectOffset(b, b, b, b); return st; }
        void YayinStil(float s, float k)
        {
            if (Mathf.Abs(s - aS) < .001f && Mathf.Abs(k - aK) < .001f && aBas != null) return; aS = s; aK = k;
            FontYukle();
            Ikonlar.Hazirla();
            aBas = AStil(fSiyah, 74 * k, TextAnchor.MiddleCenter, true); aUst = AStil(fKalin, 12 * k, TextAnchor.MiddleCenter, true);
            aSkor = AStil(fSiyah, 44 * k, TextAnchor.MiddleCenter, false); aAd = AStil(fEkstra, 12 * k, TextAnchor.UpperCenter, false);
            aKosul = AStil(fKalin, 12 * k, TextAnchor.UpperLeft, true); aSatir = AStil(fYari, 16 * k, TextAnchor.MiddleLeft, false);
            aSayi = AStil(fSiyah, 26 * k, TextAnchor.MiddleRight, false); aLv = AStil(fEkstra, 16 * k, TextAnchor.MiddleCenter, false);
            aUnl = AStil(fKalin, 15 * k, TextAnchor.MiddleLeft, true); aCta = AStil(fSiyah, 40 * k, TextAnchor.UpperLeft, false);
            aCtaAlt = AStil(fKalin, 15 * k, TextAnchor.UpperLeft, false);   // 2026-10-27 yön C: alt satır kalın 15 (SemiBold 14 %78 APCA sınırındaydı)
            // 2026-10-27 tur 2: aDug ataması yukarıdaki yorumun sonuna yapışmıştı (yorum içinde kaldı): aDug null, sonuç ekranının
            // ikincil düğmeleri (Tekrar/Ana menü) OnGUI'de NullReference atıyordu, ekrandan çıkılamıyordu.
            aDug = AStil(fKalin, 16 * k, TextAnchor.MiddleCenter, true);
            aIpucu = AStil(fYari, 13 * k, TextAnchor.UpperCenter, true); aIz = AStil(fKalin, 12 * k, TextAnchor.MiddleLeft, false);
            int r = Mathf.Max(4, Mathf.RoundToInt(16 * s)), ri = Mathf.Max(3, Mathf.RoundToInt(9 * s));
            aKutuS = AKutu(Ikonlar.Kutu(r, new Color(A_NAVY.r, A_NAVY.g, A_NAVY.b, .92f), new Color(1, 1, 1, .12f), Mathf.Max(1, s)), r + 2);
            aKutuIkon = AKutu(Ikonlar.Kutu(ri, A_NAVY3, new Color(1, 1, 1, 0), 0), ri + 2);
            aKutuLimon = AKutu(Ikonlar.Kutu(r, new Color(A_TURUNCU.r, A_TURUNCU.g, A_TURUNCU.b, .08f), new Color(A_TURUNCU.r, A_TURUNCU.g, A_TURUNCU.b, .6f), Mathf.Max(1.5f, 1.5f * s)), r + 2);
            int eg = Mathf.Max(4, Mathf.RoundToInt(14 * s)), ch = Mathf.Max(20, Mathf.RoundToInt(80 * s));
            aCtaS = AKutu(Ikonlar.EgikDugme(eg * 2 + 8, ch, eg), 0); aCtaS.border = new RectOffset(eg + 4, eg + 4, 0, 0);
        }
        static float GeriYay(float x) { const float c1 = 1.70158f, c3 = c1 + 1; x = Mathf.Clamp01(x) - 1; return 1 + c3 * x * x * x + c1 * x * x; }
        // Yıldızın iniş anı (sn, ekran açılışından): 0,45 + 0,3·i. Titreşim Hud.Update'te (OnGUI karede birkaç kez çalışır).
        static float YildizAn(int i) { return .45f + .3f * i; }
        void SonucZaman()
        {
            bool b = root.ekran == Ekran.Bitti;
            if (b && !overAcik) { overT0 = Time.unscaledTime; yildizHap = 0; sigK = 1; }
            overAcik = b; if (!b || Ayar.reduce) return;
            float t = Time.unscaledTime - overT0;
            for (int i = 0; i < 3; i++) if ((root.bittiYeniMaske & (1 << i)) != 0 && (yildizHap & (1 << i)) == 0 && t >= YildizAn(i) + .2f) { yildizHap |= 1 << i; Yerel.Hap("tick"); }
        }

        // Sığdırma çarpanı (2026-10-26, r16 "Büyük" yazı): sonuç ekranı IMGUI'de kaydırılamaz; ×1,3 yazıda içerik SIRADAKİ
        // düğmesini ekran dışına itiyordu. Bir karede ölçülen yükseklik ekranı aşarsa sonraki kare her şey 0,05 adımla
        // küçülür (en çok ×0,7; düğmeler yine ≥ 44 pt, BH). Yalnız küçülür, ekran her açılışta 1'den başlar (sallanma yok).
        float sigK = 1;
        public string SonucTani { get; private set; }   // ekran turu (EkranTuru.DuyarliTuru): ölçü, yazı ve taşma
        void Over(Match M)
        {
            // 2026-10-27 (duyarlı yerleşim): yerleşim ölçüsü s en-boyun küçüğüyle (390 × 844 tasarım karesi) kalır, ama YAZI telefonda
            // (en/boy < 0,62) 11 pt'nin altına inmez: SE'de (375 × 667) s = 0,79 idi, 12'lik satırlar (üst satır, SEN/rakip adı,
            // yıldız koşulları) 9,5 pt çıkıyordu. W/375 ≈ ekranın pt başına pikseli (en dar iPhone 375 pt; daha geniş telefonda
            // biraz fazla), 11/12 · W/375 tabanı 12'lik satırı en az 11 pt yapar. Sığmazsa sigK yalnız boşluğu ve süsü küçültür.
            var R = root; float s = Mathf.Min(W / 390f, H / 844f) * sigK, kt = W / H < .62f ? Mathf.Max(s, 11f / 12f * W / 375f) : s, k = kt * Ayar.YaziK; YayinStil(s, k);
            float t = overT0 < 0 ? 99 : Time.unscaledTime - overT0; bool rm = Ayar.reduce;
            var o0 = GUI.color; GUI.color = new Color(A_INK.r, A_INK.g, A_INK.b, .82f); GUI.DrawTexture(new Rect(-W, 0, W * 3, H), Texture2D.whiteTexture); GUI.color = o0;
            // r16: .82'de maç başı rakip kartı ve skor tabelası arkadan okunuyordu; yayın grafiği gibi tam kapalı zemin.
            o0 = GUI.color; GUI.color = new Color(A_INK.r, A_INK.g, A_INK.b, .5f); GUI.DrawTexture(new Rect(-W, 0, W * 3, H), Texture2D.whiteTexture); GUI.color = o0;
            float w = Mathf.Min(rx - lx, 420 * s), x = (W - w) / 2, y = top + 14 * s, al = rm ? Mathf.Clamp01(t / .25f) : 1;
            bool ayr = ayrinti && !string.IsNullOrEmpty(R.bittiBilgi);
            // ışınlar (a-rays): yalnız galibiyette, hareketi azaltta yok
            if (R.bittiKazandi && !rm)
            {
                var c = new Vector2(W / 2, y + 120 * s); float sz = 900 * s; var m = GUI.matrix; GUIUtility.RotateAroundPivot(Time.unscaledTime * 12f, c);
                Tex(new Rect(c.x - sz / 2, c.y - sz / 2, sz, sz), Ikonlar.Isinlar, new Color(A_TURUNCU.r, A_TURUNCU.g, A_TURUNCU.b, .10f * Mathf.Clamp01(t / .4f)));   // yön C: güneş ışınları turuncu, %13 → %10
                GUI.matrix = m;   // tur 2: bu satır da yorumun içinde kalmıştı: ışınların dönüşü sonuç ekranının tamamını döndürüyordu
            }
            if (!string.IsNullOrEmpty(R.bittiUst)) { string us = UiKok.Buyuk(R.bittiUst); float uh = aUst.CalcHeight(Ic(us), w); Yaz(new Rect(x, y, w, uh), us, aUst, A_SOLUK, al); y += uh + 4 * s; }
            // başlık: yaylanarak (1,25 → 1), hareketi azaltta yalnız solma
            {
                string bs = UiKok.Buyuk(R.bittiBas ?? R.bittiBaslik ?? ""); var st = Sigdir(R.bittiKazandi && bs.Length <= 12 ? aBas : Stil(aBas, Mathf.RoundToInt(46 * k)), bs, w);
                float th = st.CalcHeight(Ic(bs), w); var r = new Rect(x, y, w, th);
                float e = rm ? 1 : GeriYay(t / .35f), sc = rm ? 1 : Mathf.LerpUnclamped(1.25f, 1, e); var m = GUI.matrix; GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), r.center);
                var g = GUI.color; GUI.color = new Color(0, 0, 0, .45f * al); GUI.Label(new Rect(r.x, r.y + 5 * s, r.width, r.height), bs, st);
                GUI.color = new Color(1, 1, 1, al) * (R.bittiKazandi ? A_LIMON : A_YAZI); GUI.Label(r, bs, st); GUI.color = g; GUI.matrix = m;
                y += th + 2 * s;
            }
            if (!string.IsNullOrEmpty(R.bittiAltNot)) { float nh = aIpucu.CalcHeight(Ic(R.bittiAltNot), w); Yaz(new Rect(x, y, w, nh), R.bittiAltNot, aIpucu, A_SOLUK, al); y += nh + 6 * s; }
            // skor: halka + sayı – sayı + halka; adlar halkaların altında
            if (R.bittiSkor != null) { float sh = aSkor.CalcHeight(Ic(R.bittiSkor), w); Yaz(new Rect(x, y, w, sh), R.bittiSkor, aSkor, A_YAZI, al); y += sh + 10 * s; }
            else
            {
                string a = Sayi(M.sc[0]), b = Sayi(M.sc[1]); float aw = aSkor.CalcSize(Ic(a)).x, bw = aSkor.CalcSize(Ic(b)).x, dw = 30 * s, f = 48 * s, gp = 12 * s;
                float tw = f + gp + aw + dw + bw + gp + f, cx = W / 2 - tw / 2, sh = Mathf.Max(f, aSkor.CalcHeight(Ic(a), 200));
                Yuz(new Rect(cx, y + (sh - f) / 2, f, f), A_LIMON, al, false); cx += f + gp;
                Yaz(new Rect(cx, y, aw, sh), a, aSkor, A_LIMON, al); cx += aw; Yaz(new Rect(cx, y, dw, sh), "–", aSkor, A_SOLUK, al); cx += dw;
                Yaz(new Rect(cx, y, bw, sh), b, aSkor, A_YAZI, al); cx += bw + gp; Yuz(new Rect(cx, y + (sh - f) / 2, f, f), A_MERCAN, al, true);
                float nx0 = W / 2 - tw / 2, ah = aAd.CalcHeight(Ic("S"), 100);
                Yaz(new Rect(nx0 - 20 * s, y + sh + 3 * s, f + 40 * s, ah), UiKok.Buyuk(L.T("Sen")), aAd, A_LIMON, al);
                Yaz(new Rect(cx - 30 * s, y + sh + 3 * s, f + 60 * s, ah), UiKok.Buyuk(M.RakipAd(false)), aAd, A_SOLUK, al);
                y += sh + ah + 12 * s;
            }
            var od = R.odul;
            if (ayr) y = AyrintiCiz(x, y, w, s, al);
            else
            {
                if (R.bittiMaske >= 0) y = YildizCiz(x, y, w, s, t, rm, al);
                if (od != null) y = OdulCiz(od, x, y, w, s, t, rm, al);
                if (!string.IsNullOrEmpty(R.bittiAcilan))
                {
                    float ik = 26 * s, ph = 14 * s, tw = w - ik - ph * 3, uh = Mathf.Max(aUnl.CalcHeight(Ic(R.bittiAcilan), tw), ik) + 16 * s; var r = new Rect(x, y, w, uh);
                    float ua = rm ? al : Mathf.Clamp01((t - 1.9f) / .3f); KutuCiz(r, aKutuLimon, ua);
                    Tex(new Rect(r.x + ph, r.y + (uh - ik) / 2, ik, ik), Ikonlar.AcikKilit, new Color(A_TURUNCU.r, A_TURUNCU.g, A_TURUNCU.b, ua));
                    Yaz(new Rect(r.x + ph * 2 + ik, r.y + 8 * s, tw, uh - 16 * s), R.bittiAcilan, aUnl, A_YAZI, ua); y += uh + 10 * s;
                }
                if (!string.IsNullOrEmpty(R.bittiIpucu)) { float ph = aIpucu.CalcHeight(Ic(R.bittiIpucu), w); Yaz(new Rect(x, y, w, ph), R.bittiIpucu, aIpucu, A_SOLUK, al); y += ph + 8 * s; }
                if (R.bittiG >= 0) y = YolCiz(x, y, w, s, al);
            }
            // ---- alt blok: büyük düğme, ikincil düğmeler, Ayrıntılar · Ana menü ----
            var od2 = R.bittiDril != null ? Dril.Bul(R.bittiDril) : null; bool ayrintiVar = !string.IsNullOrEmpty(R.bittiBilgi);
            bool yuk = od != null && R.yukseltHazir; int nIk = (yuk ? 1 : 0) + (R.bittiIlk ? 1 : 0) + (od2 != null ? 1 : 0) + (R.mod == Mod.Dril ? 1 : 0);
            float bh = Mathf.Max(BH, 50 * s), gap = 8 * s;
            string ct = UiKok.Buyuk(CtaBaslik(R.bittiDugme, R.bittiDugmeAlt)); float cth = Sigdir(aCta, ct, w - 100 * s).CalcHeight(Ic(ct), w - 100 * s), cah = string.IsNullOrEmpty(R.bittiDugmeAlt) ? 0 : aCtaAlt.CalcHeight(Ic(R.bittiDugmeAlt), w);
            float ctaH = Mathf.Max(80 * s, cth + cah + 22 * s), blokH = ctaH + 12 * s + nIk * (bh + gap) + bh;
            // r16: kayıpta iki ikincil düğme (Ekipmanı yükselt + Antrenman: X) alt satırı ekran dışına itiyordu. Sığmazsa ikincil
            // düğmeler ikişer yan yana dizilir (sıra ve işlev aynı; yazı küçülerek sığar, hücre yine ≥ 44 pt).
            bool cift = nIk > 1 && y + 6 * s + blokH > H - bot; if (cift) blokH -= (nIk / 2) * (bh + gap);
            float yb = Mathf.Max(y + 6 * s, H - bot - blokH);
            if (BuyukDugme(new Rect(x, yb, w, ctaH), ct, R.bittiDugmeAlt, s, al)) root.TekrarOyna(); yb += ctaH + 12 * s;
            int ki = 0; float kw = cift ? (w - gap) / 2 : w;
            if (yuk) { if (ADugme(IkincilYer(ref ki, ref yb, x, kw, bh, gap, cift, nIk), L.T("Ekipmanı yükselt"), al)) { root.AnaMenu(); if (root.ui != null) root.ui.SekmeAc(Sekme.Atolye); } }
            if (R.bittiIlk) { if (ADugme(IkincilYer(ref ki, ref yb, x, kw, bh, gap, cift, nIk), L.T("Tam antrenman"), al)) root.Antrenman(); }
            if (od2 != null) { if (ADugme(IkincilYer(ref ki, ref yb, x, kw, bh, gap, cift, nIk), L.T("Antrenman: {0}", L.T(od2.ad)), al)) root.DrilOyna(od2.id); }
            if (R.mod == Mod.Dril) { if (ADugme(IkincilYer(ref ki, ref yb, x, kw, bh, gap, cift, nIk), L.T("Antrenman görevleri"), al)) { root.AnaMenu(); if (root.ui != null) root.ui.AntrenmanAc(); } }
            if (ayrintiVar)
            {
                float hw = (w - gap) / 2;
                if (ADugme(new Rect(x, yb, hw, bh), L.T(ayrinti ? "Ayrıntıları gizle" : "Ayrıntılar"), al)) { ayrinti = !ayrinti; Yerel.Hap("tick"); }
                if (ADugme(new Rect(x + hw + gap, yb, hw, bh), L.T("Ana menü"), al)) root.AnaMenu();
            }
            else if (ADugme(new Rect(x, yb, w, bh), L.T("Ana menü"), al)) root.AnaMenu();
            float son = yb + bh, sinir = H - bot; SonucTani = "sonuç " + W + "x" + H + " · s " + s.ToString("F2") + " sigK " + sigK.ToString("F2") + " yazı ×" + k.ToString("F2") + " (12'lik satır " + Mathf.RoundToInt(12 * k) + " px)"
                + " · matris " + (GUI.matrix.isIdentity ? "düz" : "DÖNÜK") + " · ikincil düğme stili " + (aDug != null ? "var" : "YOK")   // tur 2: yoruma yapışan satırların kilidi
                + " · alt düğme dibi " + son.ToString("F0") + " / sınır " + sinir.ToString("F0") + (son > sinir + 1 ? " · TAŞIYOR" : "") + " · üst " + top.ToString("F0") + " alt pay " + bot.ToString("F0");
            if (Event.current.type == EventType.Repaint && son > sinir + 1 && sigK > .7f)
                sigK = Mathf.Max(.7f, Mathf.Floor(sigK * (sinir - top) / Mathf.Max(1, son - top) * 20) / 20);
        }
        // i. ikincil düğmenin yeri: tek sütunda alt alta; çift sütunda ikişer (tek kalan son düğme tam genişlik).
        static Rect IkincilYer(ref int i, ref float yb, float x, float kw, float bh, float gap, bool cift, int n)
        {
            Rect r;
            if (!cift) { r = new Rect(x, yb, kw, bh); yb += bh + gap; }
            else if (i % 2 == 0 && i == n - 1) { r = new Rect(x, yb, kw * 2 + gap, bh); yb += bh + gap; }
            else { r = new Rect(x + (i % 2) * (kw + gap), yb, kw, bh); if (i % 2 == 1) yb += bh + gap; }
            i++; return r;
        }
        void Yaz(Rect r, string t, GUIStyle st, Color c, float a) { var g = GUI.color; GUI.color = new Color(c.r, c.g, c.b, c.a * a); GUI.Label(r, t, st); GUI.color = g; }
        void KutuCiz(Rect r, GUIStyle st, float a) { if (Event.current.type != EventType.Repaint) return; var g = GUI.color; GUI.color = new Color(1, 1, 1, a); st.Draw(r, false, false, false, false); GUI.color = g; }
        // Oyuncu halkası (yüz resmi yok: rakip portreleri sonraki faz; şimdilik kişi simgesi, renkli halka: sen limon, rakip mercan).
        void Yuz(Rect r, Color halka, float a, bool rakip)
        {
            Tex(r, disk, new Color(A_NAVY3.r, A_NAVY3.g, A_NAVY3.b, a)); Tex(r, ring, new Color(halka.r, halka.g, halka.b, a));
            float p = r.width * .2f; Tex(new Rect(r.x + p, r.y + p, r.width - 2 * p, r.height - 2 * p), Ikonlar.Kisi, new Color(A_YAZI.r, A_YAZI.g, A_YAZI.b, .85f * a));
        }
        // Üç büyük yıldız (a-bigstars): ortadaki büyük; alınan dolu altın + parıltı, alınmayan çerçeve; altında koşul (✓/✗ + yazı).
        float YildizCiz(float x, float y, float w, float s, float t, bool rm, float al)
        {
            var R = root; int n = Mathf.Clamp(R.bittiYildizN, 1, 3); float col = Mathf.Min(112 * s, w / n), rowH = 92 * s, x0 = W / 2 - col * n / 2, kh = 0;
            for (int i = 0; i < n; i++)
            {
                bool var_ = (R.bittiMaske & (1 << i)) != 0, yeni = (R.bittiYeniMaske & (1 << i)) != 0; float sz = (n == 3 && i == 1 ? 92 : 72) * s;
                float a = rm ? al : yeni ? Mathf.Clamp01((t - YildizAn(i)) / .28f) : Mathf.Clamp01((t - .25f) / .3f);
                float sc = rm || !yeni ? 1 : Mathf.LerpUnclamped(1.8f, 1, GeriYay(a)); var c = new Vector2(x0 + col * (i + .5f), y + rowH - sz / 2);
                var m = GUI.matrix; GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), c);
                if (var_) Tex(new Rect(c.x - sz * .8f, c.y - sz * .8f, sz * 1.6f, sz * 1.6f), Ikonlar.Isik, new Color(A_ALTIN.r, A_ALTIN.g, A_ALTIN.b, .45f * a));
                Tex(new Rect(c.x - sz / 2, c.y - sz / 2, sz, sz), var_ ? Ikonlar.YildizDolu : Ikonlar.YildizBos, var_ ? new Color(A_ALTIN.r, A_ALTIN.g, A_ALTIN.b, a) : new Color(1, 1, 1, .35f * a));
                GUI.matrix = m;
                string ks = R.bittiKosul != null && i < R.bittiKosul.Length ? R.bittiKosul[i] : ""; float ik = 13 * s, tw = col - ik - 8 * s, h = aKosul.CalcHeight(Ic(ks), tw);
                float tx = c.x - Mathf.Min(tw, aKosul.CalcSize(Ic(ks)).x) / 2 + ik / 2 + 2 * s, ty = y + rowH + 8 * s, ka = rm ? al : Mathf.Clamp01((t - .3f) / .3f);
                Tex(new Rect(tx - ik - 3 * s, ty + 1 * s, ik, ik), var_ ? Ikonlar.Tik : Ikonlar.Carpi, var_ ? new Color(A_TURUNCU.r, A_TURUNCU.g, A_TURUNCU.b, ka) : new Color(A_SOLUK.r, A_SOLUK.g, A_SOLUK.b, ka));
                Yaz(new Rect(tx, ty, tw, h), ks, aKosul, var_ ? A_YAZI : A_SOLUK, ka); kh = Mathf.Max(kh, h);
            }
            return y + rowH + 8 * s + kh + 14 * s;
        }
        // Ödül kartı (a-rew): Tüy sayarak (yıldızlardan sonra .9 sn), XP çubuğu aynı sürede dolar, seviye atladıysan satır.
        float OdulCiz(OdulSonuc od, float x, float y, float w, float s, float t, bool rm, float al)
        {
            float p = 14 * s, ik = 30 * s, rh = 34 * s, t1 = YildizAn(Mathf.Max(0, Ilerleme.Say(root.bittiYeniMaske))) + .1f, e = rm ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - t1) / .9f));
            bool lv = od.sonra > od.once; string ek = od.ek.Count > 0 ? string.Join(" · ", od.ek.ConvertAll(q => L.Cumle(q)).ToArray()) : null;
            string top_ = L.T(" · Toplam {0}{1}", Ayar.gear.eko.tuy, " " + L.T("Tüy")); if (top_.StartsWith(" · ")) top_ = top_.Substring(3);
            // Seviye çubuğu sayıyla da okunur (2026-10-29): "Sv 4 · 120/300 XP"; seviye atlanınca açılanlar adıyla
            // (en çok 3 ad + "+n"), Profil'e gitmeden.
            int L0, kalan0, gerek0; Ayar.gear.OyuncuSv(out L0, out kalan0, out gerek0);
            string svYazi = L.T("Sv {0}", L0) + (gerek0 > 0 ? " · " + kalan0 + "/" + gerek0 + " XP" : "");
            string acil = null;
            if (lv) { var ac = Karakter.AcilanAralik(od.once, od.sonra); if (ac.Count > 0) { var ad3 = ac.GetRange(0, Mathf.Min(3, ac.Count)).ConvertAll(q => L.T(q)); acil = L.T("Açıldı: {0}", string.Join(" · ", ad3.ToArray()) + (ac.Count > 3 ? " +" + (ac.Count - 3) : "")); } }
            string alt = (acil != null ? acil + "\n" : "") + svYazi + " · " + (ek != null ? ek + " · " + top_ : top_); float ah = aIpucu.CalcHeight(Ic(alt), w - 2 * p);
            float h = p + rh + 6 * s + rh + (lv ? 26 * s : 0) + 6 * s + ah + p; var r = new Rect(x, y, w, h); KutuCiz(r, aKutuS, al);
            float yy = y + p, rx_ = x + w - p;
            // 2026-10-29 (yasin: "Tüy kazanımları güzel bir animasyonla gösterilmeli, uçuşup bakiyeye giren tüycükler gibi"):
            // sayaç artık tüyler kutuya kondukça artar (TuyUcus); kutu her konuşta hafifçe şişer. Hareketi azaltta sayaç eskisi gibi.
            int tn = TuyAdet(od.tuy); float var_ = rm || od.tuy <= 0 ? 1 : TuyVaran(t, t1, tn) / (float)tn;
            var kutuR = new Rect(x + p, yy + (rh - ik) / 2, ik, ik); float sis = rm ? 1 : 1 + .22f * TuyNabiz(t, t1, tn);
            var mk = GUI.matrix; GUIUtility.ScaleAroundPivot(new Vector2(sis, sis), kutuR.center);
            KutuCiz(kutuR, aKutuIkon, al); Tex(new Rect(x + p + ik * .2f, yy + (rh - ik) / 2 + ik * .2f, ik * .6f, ik * .6f), Ikonlar.Tuy, new Color(A_ALTIN.r, A_ALTIN.g, A_ALTIN.b, al));
            GUI.matrix = mk; tuyHedef = kutuR.center;
            Yaz(new Rect(x + p + ik + 10 * s, yy, w * .5f, rh), L.T("Tüy"), aSatir, A_YAZI, al);
            Yaz(new Rect(rx_ - 120 * s, yy, 120 * s, rh), "+" + Mathf.RoundToInt(od.tuy * (rm ? e : var_)), aSayi, A_ALTIN, al); yy += rh + 6 * s;
            KutuCiz(new Rect(x + p, yy + (rh - ik) / 2, ik, ik), aKutuIkon, al); Yaz(new Rect(x + p, yy + (rh - ik) / 2, ik, ik), "XP", Stil(aLv, Mathf.RoundToInt(13 * aK)), A_YAZI, al);
            string xs = "+" + Mathf.RoundToInt(od.xp * e); float xw = aSayi.CalcSize(Ic("+" + od.xp)).x + 4 * s;
            int kalan = kalan0, gerek = gerek0;
            float son = gerek > 0 ? kalan / (float)gerek : 1, bas = lv ? 0 : Mathf.Clamp01((kalan - od.xp) / (float)Mathf.Max(1, gerek)), dol = lv ? (e < .5f ? Mathf.Lerp(.3f, 1, e * 2) : Mathf.Lerp(0, son, (e - .5f) * 2)) : Mathf.Lerp(bas, son, e);
            var bar = new Rect(x + p + ik + 10 * s, yy + rh / 2 - 5 * s, rx_ - xw - 10 * s - (x + p + ik + 10 * s), 10 * s);
            Fill(bar, new Color(1, 1, 1, .12f * al)); Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(dol), bar.height), new Color(A_TURUNCU.r, A_TURUNCU.g, A_TURUNCU.b, al));
            Yaz(new Rect(rx_ - xw, yy, xw, rh), xs, aSayi, A_YAZI, al); yy += rh;
            if (lv)
            {
                float la = rm ? al : Mathf.Clamp01((t - t1 - .9f) / .25f); string ls = UiKok.Buyuk(L.T("Seviye atladın")) + " · " + L.T("Sv {0}", od.sonra);
                float lw = aLv.CalcSize(Ic(ls)).x, ik2 = 14 * s, lx0 = x + (w - lw - ik2 - 4 * s) / 2;
                Tex(new Rect(lx0, yy + 6 * s + (20 * s - ik2) / 2, ik2, ik2), Ikonlar.Yukari, new Color(A_TURUNCU.r, A_TURUNCU.g, A_TURUNCU.b, la)); Yaz(new Rect(lx0 + ik2 + 4 * s, yy + 6 * s, lw, 20 * s), ls, aLv, A_TURUNCU, la); yy += 26 * s;
            }
            Yaz(new Rect(x + p, yy + 6 * s, w - 2 * p, ah), alt, aIpucu, A_SOLUK, al);
            if (!rm && od.tuy > 0) TuyUcus(t, t1, tn, new Vector2(W / 2f, y - 70 * s), tuyHedef, s, al);   // kartın üstünde (sonra çizilir)
            return y + h + 10 * s;
        }
        // Uçan tüyler (2026-10-29): yıldızların altından (kartın üstü) çıkar, yayla dönerek Tüy kutusuna konar. Adet tutarla
        // büyür ama 4–14 arasında kalır (çok tüy ekranı boğar, az tüy "ödül" demez). Her tüyün çıkışı .06 sn arayla, uçuşu
        // .55 sn; ilki eski sayaçla aynı anda (t1 + .1) konar, sonuncusu t1 + .9'da: süre eskisiyle aynı. Yön ve yay tüy
        // başına sabit sözde rastgele (i'den), kare hızından bağımsız: ekran turu aynı anı hep aynı çizer.
        Vector2 tuyHedef;
        const float TUY_UCUS = .55f;
        static int TuyAdet(int tuy) { return Mathf.Clamp(4 + tuy / 10, 4, 14); }
        static float TuyKonus(int i, float t1, int n) { return t1 + .1f + .8f * (n <= 1 ? 0 : i / (float)(n - 1)); }
        static int TuyVaran(float t, float t1, int n) { int k = 0; for (int i = 0; i < n; i++) if (t >= TuyKonus(i, t1, n)) k++; return k; }
        static float TuyNabiz(float t, float t1, int n)
        {
            float en = 0; for (int i = 0; i < n; i++) { float d = t - TuyKonus(i, t1, n); if (d >= 0 && d < .25f) en = Mathf.Max(en, 1 - d / .25f); }
            return en;
        }
        void TuyUcus(float t, float t1, int n, Vector2 bas, Vector2 hedef, float s, float al)
        {
            if (Event.current.type != EventType.Repaint) return;
            for (int i = 0; i < n; i++)
            {
                float k = TuyKonus(i, t1, n), u = (t - (k - TUY_UCUS)) / TUY_UCUS; if (u <= 0 || u >= 1) continue;
                float h1 = Mathf.Repeat(Mathf.Sin(i * 12.9898f) * 43758.5453f, 1), h2 = Mathf.Repeat(Mathf.Sin(i * 78.233f) * 12345.678f, 1);
                var p0 = bas + new Vector2((h1 - .5f) * 160 * s, (h2 - .5f) * 30 * s);
                var p1 = new Vector2(Mathf.Lerp(p0.x, hedef.x, .35f) + (h2 - .5f) * 120 * s, Mathf.Min(p0.y, hedef.y) - (40 + 50 * h1) * s);   // yay tepesi
                float e = u * u * (3 - 2 * u), a = 1 - e;
                var q = a * a * p0 + 2 * a * e * p1 + e * e * hedef;
                float sz = Mathf.Lerp(26, 16, e) * s, gor = Mathf.Clamp01(u / .15f) * al, don = (h1 - .5f) * 80 + 540 * u * (h2 < .5f ? 1 : -1);
                var m = GUI.matrix; GUIUtility.RotateAroundPivot(don, q);
                Tex(new Rect(q.x - sz / 2, q.y - sz / 2, sz, sz), Ikonlar.Tuy, new Color(A_ALTIN.r, A_ALTIN.g, A_ALTIN.b, gor));
                GUI.matrix = m;
            }
        }
        // Turun basamak çizgisi (a-road): geçilen limon, şimdiki çizgili, gelecek soluk; yanında "3/6" (yalnız renkle değil).
        float YolCiz(float x, float y, float w, float s, float al)
        {
            var p = Ayar.ilerleme; int g = root.bittiG, tr = KariyerVeri.TurOf(g), sira = p.HepsiBitti ? -1 : p.Siradaki(), gec = 0;
            for (int j = 0; j < KariyerVeri.BASAMAK; j++) if (p.Kazanildi(KariyerVeri.Global(tr, j))) gec++;
            string sol = UiKok.Buyuk(L.T(UiKok.TurAd(tr))), sag = gec + "/" + KariyerVeri.BASAMAK; float sw = aIz.CalcSize(Ic(sol)).x, gw = aIz.CalcSize(Ic(sag)).x, h = 16 * s;
            Yaz(new Rect(x, y, sw, h), sol, aIz, A_SOLUK, al); Yaz(new Rect(x + w - gw, y, gw, h), sag, aIz, A_SOLUK, al);
            float bx = x + sw + 10 * s, bw = w - sw - gw - 20 * s, pw = (bw - 5 * s * (KariyerVeri.BASAMAK - 1)) / KariyerVeri.BASAMAK;
            for (int j = 0; j < KariyerVeri.BASAMAK; j++)
            {
                int gj = KariyerVeri.Global(tr, j); var r = new Rect(bx + j * (pw + 5 * s), y + h / 2 - 2.5f * s, pw, 5 * s);
                if (gj == sira) Tex(r, Ikonlar.Serit, new Color(1, 1, 1, al)); else Fill(r, p.Kazanildi(gj) ? new Color(A_TURUNCU.r, A_TURUNCU.g, A_TURUNCU.b, al) : new Color(1, 1, 1, .14f * al));
            }
            return y + h + 10 * s;
        }
        // Ayrıntılar: iki özet kartı (sayı + adı) ve tam liste (eski ekranın içeriği).
        float AyrintiCiz(float x, float y, float w, float s, float al)
        {
            var R = root; int n = R.bittiKart.Count; float g = 10 * s, kw = n > 1 ? (w - g) / 2 : w, kh = 0; var vs = Stil(aSkor, Mathf.RoundToInt(30 * aK));
            foreach (var kk in R.bittiKart) kh = Mathf.Max(kh, aIpucu.CalcHeight(Ic(L.T(kk.ad)), kw - 16 * s));
            kh += 40 * s + 14 * s;
            for (int i = 0; i < n; i++)
            {
                var r = new Rect(x + i * (kw + g), y, kw, kh); KutuCiz(r, aKutuS, al); var kk = R.bittiKart[i];
                Yaz(new Rect(r.x, r.y + 6 * s, r.width, 38 * s), kk.deger.ToString(), vs, A_LIMON, al); Yaz(new Rect(r.x + 8 * s, r.y + 46 * s, r.width - 16 * s, kh - 50 * s), L.T(kk.ad), aIpucu, A_YAZI, al);
            }
            if (n > 0) y += kh + 10 * s;
            float ih = aIpucu.CalcHeight(Ic(R.bittiBilgi), w - 28 * s); var bk = new Rect(x, y, w, ih + 24 * s); KutuCiz(bk, aKutuS, al);
            Yaz(new Rect(x + 14 * s, y + 12 * s, w - 28 * s, ih), R.bittiBilgi, aIpucu, A_YAZI, al);
            return y + bk.height + 10 * s;
        }
        // Büyük eğik düğme (a-cta, IMGUI): limon eğik doku, solda başlık + alt satır, sağda koyu daire içinde ok. Basılıyken .96.
        // "Sıradaki: İl şampiyonası" düğmede iki kez yazılıyordu (alt satır zaten "İl şampiyonası · Rocco"): alt satır varsa
        // başlık iki noktadan öncesi (a-cta: "SIRADAKİ"); yoksa tamamı.
        static string CtaBaslik(string t, string alt)
        {
            if (string.IsNullOrEmpty(t)) return ""; int i = t.IndexOf(": "); return i > 0 && !string.IsNullOrEmpty(alt) ? t.Substring(0, i) : t;
        }
        bool BuyukDugme(Rect r, string t, string alt, float s, float al)
        {
            blocks.Add(r); bool bas = r.Contains(Event.current.mousePosition) && Input.GetMouseButton(0);
            var m = GUI.matrix; if (bas && !Ayar.reduce) GUIUtility.ScaleAroundPivot(new Vector2(.96f, .96f), r.center);
            KutuCiz(r, aCtaS, al);
            float ci = 52 * s; var cst = Sigdir(aCta, t, r.width - ci - 48 * s); float th = cst.CalcHeight(Ic(t), r.width - ci - 48 * s), ah = string.IsNullOrEmpty(alt) ? 0 : aCtaAlt.CalcHeight(Ic(alt), r.width), ty = r.y + (r.height - th - ah) / 2;
            Yaz(new Rect(r.x + 24 * s, ty, r.width - ci - 48 * s, th), t, cst, A_CTA_MUREKKEP, al);
            if (ah > 0) Yaz(new Rect(r.x + 24 * s, ty + th, r.width - ci - 48 * s, ah), alt, aCtaAlt, A_CTA_MUREKKEP, al);   // yön C: tam opak (Lc ≥ 60)
            var cr = new Rect(r.xMax - 24 * s - ci, r.y + (r.height - ci) / 2, ci, ci); Tex(cr, disk, new Color(A_CTA_MUREKKEP.r, A_CTA_MUREKKEP.g, A_CTA_MUREKKEP.b, al));
            Tex(new Rect(cr.x + ci * .3f, cr.y + ci * .3f, ci * .4f, ci * .4f), Ikonlar.Ileri, new Color(A_CTA_OK.r, A_CTA_OK.g, A_CTA_OK.b, al));
            GUI.matrix = m;
            bool b = GUI.Button(r, GUIContent.none, GUIStyle.none); if (b) Yerel.Hap("tick"); return b;
        }
        // İkincil düğme (a-res2): lacivert yuvarlak kutu, kalın yazı; ≥ 44 pt (BH) ve ≥ 50 tasarım pikseli.
        bool ADugme(Rect r, string t, float al)
        {
            blocks.Add(r); bool bas = r.Contains(Event.current.mousePosition) && Input.GetMouseButton(0);
            KutuCiz(r, aKutuS, al); if (bas) Fill(r, new Color(1, 1, 1, .06f));
            Yaz(new Rect(r.x + 8, r.y, r.width - 16, r.height), t, Sigdir(aDug, t, r.width - 16), A_YAZI, al);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        // ---- smaç perdesi (HTML perde): 5 halka × 24 dilim, komşu halkalar ters yönde 1,2 rad/sn döner. Kapanış: disk
        // 120 ms gecikmeyle 330 ms'de büyür (kübik ease-out), bu sırada top hız çizgileriyle sol alttan sağ üste 280 ms'de
        // uçar; maç örtünün altında kurulur; açılış 350 ms'de küçülür (ease-in-out). Hareketi azalt: yalnız solma.
        // 2026-10-29 (yasin: "Splash ve oyuna geçiş animasyonunu bozmuşsun, html zamanlarındaki o gif gibi olmalı"): 27 Eki'deki
        // perde v2 (yön C: koyu zemin, diyagonal bant, küçük güneş diski, BIRDIE SMASH yazısı, inen top) geri alındı; perde ve
        // açılış yeniden HTML perde() ile kare kare aynı. Yön C kararı menü renkleri içindi, perdeyi değiştirmek istenmemişti.
        // Geçiş: disk R = köşegen × .55, 120 ms gecikmeyle 330 ms'de 0 → 1 büyür (kübik ease-out); aynı anda tüy top hız
        // çizgileriyle (−.1W, .85H) → (1.1W, .12H) 280 ms'de uçar (mantar önde, ease-out). Kapalıyken halkalar döner; ses/sahne
        // hazır değilse .6 sn sonra ortada dönen top + "Hazırlanıyor…". Açılma 350 ms küçülme (ease-in-out).
        // Açılış (soğuk açılış): kapalı diskle başlar (iOS açılış rengi = dış halka #a3234b, CiBuild), en az .6 sn, sonra aynı küçülme.
        // Hareketi azalt: ölçek/dönme/uçuş yok, 200 ms belirme ve 250 ms solma. Top HTML'deki çizilmiş tüy top (Yukleyici.serit, 2026-10-30).
        bool perdeAcilis, kaynakVar; int acilisIyi; float acilisBek, perdeSabit = -1; Vector2 perdeKaynak;
        float acilisT0 = -1, acilisAcT = -1, acilisSonT = -1;   // ekran turu: gerçek açılışın süreleri (sn)
        public string AcilisTani { get { return acilisT0 < 0 ? "yok" : "kapalı " + (acilisAcT < 0 ? "?" : (acilisAcT - acilisT0).ToString("F2")) + " sn · toplam " + (acilisSonT < 0 ? "?" : (acilisSonT - acilisT0).ToString("F2")) + " sn (hedef ≥ .95: .60 + .35; ses geç hazırsa hazır + .35)"; } }
        static float EaseOut3(float x) { return 1 - Mathf.Pow(1 - Mathf.Clamp01(x), 3); }
        static float EaseInOut3(float x) { x = Mathf.Clamp01(x); return x < .5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2; }
        // diskin ölçeği (0..1) ve opaklığı bu karede
        void DiskOlcek(out float sc, out float al)
        {
            sc = 1; al = 1; bool rm = Ayar.reduce;
            if (perdeEvre == 1) { if (rm) al = Mathf.Clamp01(perdeT / .2f); else sc = EaseOut3((perdeT - .12f) / .33f); }
            else if (perdeEvre == 3) { if (rm) al = 1 - Mathf.Clamp01(perdeT / .25f); else sc = 1 - EaseInOut3(perdeT / .35f); }
        }
        const float UCUS_SN = .28f;
        static Vector2 UcusA { get { return new Vector2(-Screen.width * .1f, Screen.height * .85f); } }
        static Vector2 UcusB { get { return new Vector2(Screen.width * 1.1f, Screen.height * .12f); } }

        // Uçan / bekleyen topun bu karedeki yeri (IMGUI pikseli), boyu, ekrandaki yönü (derece, y yukarı; mantar bu yöne bakar),
        // eksen dönüşü (rad/sn) ve opaklığı. serbest: Yukleyici kendi bekleme dönüşünü yapar (poz yazılmaz).
        bool TopDurum(out Vector2 p, out float boy, out float aci, out float donus, out float al, out bool serbest)
        {
            p = Vector2.zero; boy = 0; aci = 0; donus = 0; al = 1; serbest = false; float W_ = Screen.width, H_ = Screen.height;
            if (perdeEvre == 0) return false;
            // 2026-10-30 (yasin: "Açılış perdesi içerisindeki top kendi ekseni etrafında dönerek ve tam durması lazım"): açılışta
            // top kapalı disk boyunca ortada durur ve kendi ekseninde döner (yer değiştirmez, sallanmaz); açılırken diskle birlikte
            // küçülür. Geçişte (maça giriş) eskisi gibi yalnız bekleme uzarsa görünür. Boy tam piksel: RT kesirli dikdörtgene
            // çizilince kenarları bulanıklaşıyordu.
            bool acilisTop = perdeAcilis && (perdeEvre == 2 || perdeEvre == 3);
            if (perdeBek || acilisTop)
            {
                float s = 1;
                if (perdeEvre == 3) { float sc, a; DiskOlcek(out sc, out a); if (Ayar.reduce) al = a; else s = sc; }
                p = new Vector2(Mathf.Round(W_ / 2), Mathf.Round(H_ / 2 - Mathf.Min(W_, H_) * .026f)); boy = Mathf.Round(Mathf.Min(W_, H_) * .3f * s); serbest = true; return boy > 2 && al > 0;
            }
            if (perdeAcilis || perdeEvre != 1 || Ayar.reduce) return false;
            float t = perdeT / UCUS_SN; if (t >= 1) return false;
            p = Vector2.Lerp(UcusA, UcusB, EaseOut3(t)); var d = UcusB - UcusA;
            aci = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg; boy = W_ * .3f; return true;
        }
        // Update'te: Yukleyici kamerası yalnız top görünürken çizer; poz her kare yazılır (yazılmazsa kendi dönüşüne döner).
        void TopPozYaz()
        {
            var y = root.yukleyici; if (y == null) return;
            Vector2 p; float boy, aci, donus, al; bool serbest; bool gor = TopDurum(out p, out boy, out aci, out donus, out al, out serbest);
            y.Goster(gor); if (gor && !serbest) y.Poz(aci, donus);
        }

        void PerdeCiz()
        {
            if (perdeEvre == 0) return; if (perdeTex == null) PerdeDoku();
            if (Event.current.type == EventType.Repaint) acilisIyi = Time.unscaledDeltaTime < .025f ? acilisIyi + 1 : 0;
            blocks.Add(new Rect(0, 0, W, H));
            float sc, al; DiskOlcek(out sc, out al);
            if (sc > .001f && al > .001f)
            {
                float R = Mathf.Sqrt(W * W + H * H) * .55f * sc, rr = R * .62f; var c = new Vector2(W / 2, H / 2); var m = GUI.matrix;
                for (int i = 0; i < 5; i++)
                {
                    float r = i == 0 ? R : PERDE_F[i][0] * rr; float ang = Ayar.reduce ? 0 : perdeAci * Mathf.Rad2Deg * (i % 2 == 1 ? -1 : 1);
                    GUIUtility.RotateAroundPivot(ang, c); Tex(new Rect(c.x - r, c.y - r, 2 * r, 2 * r), perdeTex[i], new Color(1, 1, 1, al)); GUI.matrix = m;
                }
            }
            TopCiz();
            if (perdeBek && root.yukleyici != null)
            {   // HTML perdeBekle: ortada dönen tüy top, altında "Hazırlanıyor…" (VoiceOver'a duyurma IMGUI'de yok, bilinen eksik)
                Vector2 p; float boy, aci, donus, a_; bool serbest; TopDurum(out p, out boy, out aci, out donus, out a_, out serbest);
                string t = L.T("Hazırlanıyor…"); var ps = Stil(small, -1, FontStyle.Bold); var sz = ps.CalcSize(Ic(t));
                var pr = new Rect(W / 2 - sz.x / 2 - 14 * u, H / 2 + boy * .85f, sz.x + 28 * u, sz.y + 10 * u); Fill(pr, new Color(74 / 255f, 10 / 255f, 34 / 255f, .88f)); GUI.Label(pr, t, ps);
            }
        }
        void TopCiz()
        {
            var y = root.yukleyici; if (y == null || y.rt == null) return;
            Vector2 p; float boy, aci, donus, al; bool serbest; if (!TopDurum(out p, out boy, out aci, out donus, out al, out serbest) || al <= 0) return;
            if (!serbest)
            {   // HTML ucan: topun arkasında 6 beyaz hız çizgisi (L = W/2), uçuş yönünde
                float Lw = W * .5f, an = -aci * Mathf.Deg2Rad; var dir = new Vector2(Mathf.Cos(an), Mathf.Sin(an)); var nrm = new Vector2(-dir.y, dir.x);
                for (int i = 0; i < 6; i++)
                {
                    float off = (i - 2.5f) * Lw * .09f, l = Lw * (.5f + .5f * ((i * 37) % 11) / 11f); var a = p - dir * Lw * .25f + nrm * off;
                    Cizgi(a, a - dir * l, Mathf.Max(1.5f, Lw * .018f), new Color(1, 1, 1, .8f));
                }
            }
            var o = GUI.color; GUI.color = new Color(1, 1, 1, al);
            if (y.serit != null) SeritCiz(y, p, boy * SERIT_BOY, serbest ? 44f : -aci, y.Faz);
            else GUI.DrawTexture(new Rect(p.x - boy / 2, p.y - boy / 2, boy, boy), y.rt);
            GUI.color = o;
        }
        // HTML tuyCiz ile aynı kare: 300×299 kare, mantar (39,36)'da, karede mantarın yönü −135° (sol üst). yon: ekrandaki yön
        // (derece, y aşağı, saat yönü +; bekleme 44° = mantar sağ altta, HTML TS_LOGO). Kare indeksi eksen fazından: 16 tüy
        // olduğu için 22,5°'de tekrar eder, 16 kare o aralığı böler. Top ortalanır: mantar, merkezden yön boyunca topun yarı
        // boyu (.56 kare; mantar (39,36) → etek ucu ~(280,270)) ileride. HTML'deki +.2 boy kayması yok (yasin: "top ortada").
        const float SERIT_BOY = 1f;   // çizilmiş top karenin ~%90'ı: RT dönemindeki topla aynı görünen boy (r61 karesi ~115 px / 390)
        void SeritCiz(Yukleyici y, Vector2 p, float k, float yon, float faz)
        {
            const float AXP = Mathf.PI * 2 / 16; int fi = Mathf.FloorToInt(Mathf.Repeat(faz, AXP) / AXP * 16) % 16;
            float s = k / 300f, r = yon * Mathf.Deg2Rad; var cork = p + new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * .56f * k;
            var m = GUI.matrix; GUIUtility.RotateAroundPivot(yon + 135f, cork);
            var uv = new Rect((fi % 4) * .25f, 1 - (fi / 4 + 1) * .25f, .25f, .25f);
            GUI.DrawTextureWithTexCoords(new Rect(cork.x - 39 * s, cork.y - 36 * s, 300 * s, 299 * s), y.serit, uv, true);
            GUI.matrix = m;
        }
        // Halka dokuları. 2026-10-29: 512² ve kenar yumuşatmalı (HTML tuvali keskin çiziyordu; 256² ikili maske tam ekran
        // diskte basamaklı kenar ve dilim sınırı veriyordu, r58 karesi). Kenar ve dilim sınırı piksel uzaklığıyla harmanlanır.
        void PerdeDoku()
        {
            perdeTex = new Texture2D[5]; const int N = 24, n = 512; float sec = Mathf.PI * 2 / N, yar = n / 2f;
            for (int i = 0; i < 5; i++)
            {
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; Color c1 = Kit.Hex(PERDE_C[i, 0]), c2 = Kit.Hex(PERDE_C[i, 1]); var px = new Color32[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                    {
                        float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx); if (a < 0) a += Mathf.PI * 2;
                        float kenar;   // kenara içeriden uzaklık (piksel)
                        if (i == 0) kenar = (1 - r) * yar;
                        else { float lok = Mathf.Repeat(a - Mathf.PI / N, sec) - sec / 2; kenar = (Mathf.Cos(sec / 2) - r * Mathf.Cos(lok)) * yar; }   // iç halkalar 24-gen
                        float al = Mathf.Clamp01(kenar + .5f); if (al <= 0) { px[y * n + x] = new Color32(0, 0, 0, 0); continue; }
                        float f = a / sec; int dil = Mathf.FloorToInt(f); float sinir = Mathf.Min(f - dil, dil + 1 - f) * sec * r * yar;   // dilim sınırına uzaklık (piksel)
                        Color bu = dil % 2 == 0 ? c2 : c1, kom = dil % 2 == 0 ? c1 : c2; var c = Color.Lerp(kom, bu, Mathf.Clamp01(sinir + .5f)); c.a = al;
                        px[y * n + x] = c;
                    }
                t.SetPixels32(px); t.Apply(false, true); perdeTex[i] = t;   // CPU kopyası bırakılır: 5 × 1 MB, yalnız GPU'da
            }
        }
        // ---- ekran turu (EkranTuru): perdenin bir anını dondur (zaman, evre, açılış mı, hareketi azalt dışarıdan), sonra bırak.
        public void PerdeKare(bool acilis, int evre, float t, bool bek = false)
        {
            perdeAcilis = acilis; perdeEvre = evre; perdeT = t; perdeSabit = t; perdeIs = null; perdeAtla = false; perdeBek = bek;
            acilisBek = 0; perdeAci = t * 1.2f; TopPozYaz();
        }
        public void PerdeBirak() { perdeSabit = -1; perdeEvre = 0; perdeBek = false; kaynakVar = false; TopPozYaz(); }
        public string PerdeTani
        {
            get
            {
                Vector2 p; float boy, aci, donus, al; bool serbest; bool g = TopDurum(out p, out boy, out aci, out donus, out al, out serbest);
                return "evre " + perdeEvre + (perdeAcilis ? " açılış" : " geçiş") + " t " + perdeT.ToString("F2") + " ortü " + PerdeOrtu.ToString("F2") + " kayma " + PerdeKayma.ToString("F2")
                    + " · top " + (g ? "(" + Mathf.RoundToInt(p.x) + "," + Mathf.RoundToInt(p.y) + ") boy " + Mathf.RoundToInt(boy) + " açı " + Mathf.RoundToInt(aci) + " dönüş " + donus.ToString("F1") + " op " + al.ToString("F2") + (serbest ? " serbest" : "") : "yok")
                    + " · kaynak " + (kaynakVar ? "(" + Mathf.RoundToInt(perdeKaynak.x) + "," + Mathf.RoundToInt(perdeKaynak.y) + ")" : "yok") + " · bekle " + perdeBek + " · azalt " + Ayar.reduce;
            }
        }
    }
}
