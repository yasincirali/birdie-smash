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
        GUIStyle Stil(GUIStyle b, int boy, FontStyle fs = (FontStyle)(-1))
        {
            int no; if (!stilNo.TryGetValue(b, out no)) { no = stilNo.Count + 1; stilNo[b] = no; }
            long key = ((long)no << 32) | ((long)(boy + 1) << 4) | ((long)fs + 1 & 15); GUIStyle st;
            if (!stilOn.TryGetValue(key, out st)) { st = new GUIStyle(b); if (boy > 0) st.fontSize = boy; if ((int)fs >= 0) st.fontStyle = fs; stilOn[key] = st; }
            return st;
        }
        GUIStyle leftWrap, leftWrapTaban; // her karede yeni taban S() önbelleğini şişirirdi
        public void StilSifirla() { stilOn.Clear(); stilNo.Clear(); }
        float ralliPop; int sonRalli; float[] pop = new float[2]; float flash; Color flashC; readonly List<Vector3> cams = new List<Vector3>();
        class HawkV { public float x, y, t; public bool icerde, serve; public int cm; } HawkV hawk;
        Texture2D ring, ringK, glow, disk, bar; Texture2D[] perdeTex; Texture2D topIkon;
        // perde: evre 0 yok, 1 kapanıyor, 2 kapalı, 3 açılıyor
        int perdeEvre; float perdeT; System.Action perdeIs; float perdeAci; float perdeMin = .15f; bool perdeAtla, perdeBek;
        static readonly float[][] PERDE_F = { new[] { 2.6f }, new[] { .86f }, new[] { .66f }, new[] { .47f }, new[] { .30f } };
        static readonly string[,] PERDE_C = { { "#a3234b", "#b52a52" }, { "#d84358", "#e85563" }, { "#f9665a", "#ff7a5c" }, { "#ffa04d", "#ffb35a" }, { "#f9d272", "#fde59a" } };

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
        public void Perde(System.Action ortunce) { perdeIs = ortunce; perdeEvre = 1; perdeT = 0; perdeMin = .15f; perdeAtla = false; }
        // Açılış perdesi (HTML perde({kapali:true, min:.6})): oyun kapalı perdeyle açılır, vuruş sesleri hazırlanınca kalkar;
        // hazırken dokunuş beklemeyi kısaltır.
        public void Acilis() { perdeEvre = 2; perdeT = 0; perdeIs = null; perdeMin = .6f; perdeAtla = false; }
        public bool PerdeKapali { get { return perdeEvre == 1 || perdeEvre == 2; } }
        // UI Toolkit menüsü için (UiKok): perde var mı, ne kadar örtüyor (0..1; menü 1 − örtü opaklıkla solar), dokunuşla
        // beklemeyi kısalt. Perdenin çizimiyle aynı eğriler.
        public bool PerdeVar { get { return perdeEvre > 0; } }
        public float PerdeOrtu
        {
            get
            {
                if (perdeEvre == 2) return 1; if (perdeEvre == 0) return 0; bool rm = Ayar.reduce;
                if (perdeEvre == 1) return rm ? Mathf.Clamp01(perdeT / .2f) : 1 - Mathf.Pow(1 - Mathf.Clamp01((perdeT - .12f) / .33f), 3);
                return rm ? 1 - Mathf.Clamp01(perdeT / .25f) : 1 - Mathf.Clamp01(perdeT / .35f);
            }
        }
        public void PerdeAtla() { if (perdeEvre == 2) perdeAtla = true; }
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
            if (konfetiT >= 0) { int once = Mathf.FloorToInt(konfetiT / .13f); konfetiT += d; int sonra = Mathf.FloorToInt(konfetiT / .13f); if (sonra > once && once < 6) { var c = new[] { "#d8ff5a", "#ff7a6b", "#7ad7ff" }[once % 3]; var p = root.cam.ScreenToWorldPoint(new Vector3(Screen.width * (.15f + Random.value * .7f), Screen.height * (.5f + Random.value * .3f), 6)); root.fx.Burst(p, Kit.Hex(c), 30, 300, "spark"); } if (konfetiT > 1) konfetiT = -1; }
            if (kivT >= 0)
            {
                int once = kivT <= 0 ? -1 : Mathf.FloorToInt(kivT / .12f); kivT += d; int sonra = Mathf.FloorToInt(kivT / .12f);
                for (int k = once + 1; k <= sonra && k < 3; k++) { var p = root.cam.ScreenToWorldPoint(new Vector3(Screen.width * (.2f + Random.value * .6f), Screen.height * .7f, 6)); root.fx.Burst(p, Kit.Hex(new[] { "#d8ff5a", "#ff7a6b", "#7ad7ff" }[k]), 22, 260, "spark"); }
                if (kivT > .3f) kivT = -1;
            }
            if (Ayar.fps) FpsOlc();
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
            if (!Ayar.fps || fpsYazi == "") return; var st = new GUIStyle(tiny) { alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Bold, wordWrap = false };
            var sz = st.CalcSize(new GUIContent(fpsYazi)); var r = new Rect(Mathf.Max(safe.xMin, 0) + 6 * u, top + 80 * u, sz.x + 12 * u, sz.y + 6 * u);
            Fill(r, new Color(0, 0, 0, .62f)); var o = GUI.color; GUI.color = Kit.Hex("#d8ff5a"); GUI.Label(new Rect(r.x + 6 * u, r.y + 3 * u, sz.x + 4, sz.y), fpsYazi, st); GUI.color = o;
        }
        void PerdeTick(float d)
        {
            if (perdeEvre == 0) return; perdeT += d; perdeAci += d * 1.2f;
            if (perdeEvre == 1 && perdeT >= (Ayar.reduce ? .2f : .45f)) { perdeEvre = 2; perdeT = 0; if (perdeIs != null) { perdeIs(); perdeIs = null; } }
            else if (perdeEvre == 2)
            {   // HTML perde 'kapali': sesler hazır değilse bekler (8 sn sonra yine açılır); .6 sn'den uzun beklemede dönen top
                bool hazir = root.Hazir() || perdeT > 8; perdeBek = !hazir && perdeT > .6f;
                if (hazir && (perdeT >= (Ayar.reduce && perdeMin < .5f ? .05f : perdeMin) || perdeAtla)) { perdeEvre = 3; perdeT = 0; perdeBek = false; }
            }
            else if (perdeEvre == 3 && perdeT >= (Ayar.reduce ? .25f : .35f)) perdeEvre = 0;
            if (perdeEvre != 2) perdeBek = false; if (root.yukleyici != null) root.yukleyici.Goster(perdeBek);
        }

        static Texture2D Solid(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }
        void Styles()
        {
            float nu = Mathf.Min(Screen.height / 640f, Screen.width / 340f), nf = nu * Ayar.YaziK; if (big != null && Mathf.Abs(nu - u) < .001f && Mathf.Abs(nf - fu) < .001f) return; u = nu; fu = nf; StilSifirla(); // eski boyutun önbelleği yeni tabanla karışmasın
            big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(32 * fu), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            mid = Stil(big, Mathf.RoundToInt(19 * fu));
            small = Stil(big, Mathf.RoundToInt(13 * fu), FontStyle.Normal);
            tiny = Stil(small, Mathf.RoundToInt(11 * fu));
            left = new GUIStyle(small) { alignment = TextAnchor.UpperLeft }; leftB = new GUIStyle(left) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(15 * fu) };
            btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(15 * fu), fontStyle = FontStyle.Bold, wordWrap = true };
            btn.normal.background = Solid(Kit.Hex("#22343a")); btn.hover.background = btn.active.background = Solid(Kit.Hex("#2e464e")); btn.normal.textColor = btn.hover.textColor = btn.active.textColor = Color.white;
            btnD = new GUIStyle(btn); btnD.normal.background = btnD.hover.background = btnD.active.background = Solid(Kit.Hex("#1a2629")); btnD.normal.textColor = btnD.hover.textColor = btnD.active.textColor = new Color(1, 1, 1, .45f);
            btnP = new GUIStyle(btn); btnP.normal.background = Solid(Kit.Hex("#d8ff5a")); btnP.hover.background = btnP.active.background = Solid(Kit.Hex("#e6ff8c")); btnP.normal.textColor = btnP.hover.textColor = btnP.active.textColor = Kit.Hex("#0d1a12");
            chip = Stil(btn, Mathf.RoundToInt(13 * fu), FontStyle.Normal); chipOn = Stil(chip, -1, FontStyle.Bold);
            chipOn.normal.background = chipOn.hover.background = chipOn.active.background = Solid(Kit.Hex("#7ad7ff")); chipOn.normal.textColor = chipOn.hover.textColor = chipOn.active.textColor = Kit.Hex("#07161c");
            card = new GUIStyle(GUI.skin.box); card.normal.background = Solid(new Color(.04f, .08f, .09f, .86f));
            if (ring == null) { ring = Kit.RingTex(128, 5); ringK = Kit.RingTex(128, 3); glow = Kit.RadialTex(64); disk = Kit.RingTex(128, 128); bar = Solid(Color.white); }
        }

        static void Shadowed(Rect r, string t, GUIStyle s, Color c)
        {
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
            W = Screen.width; H = Screen.height; safe = Screen.safeArea;
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
                if (root.paused) Pause(M);
                if (root.ekran == Ekran.Bitti) Over(M);
            }
            if (flash > 0) Fill(new Rect(0, 0, W, H), new Color(flashC.r, flashC.g, flashC.b, flash));
            PerdeCiz(); FpsCiz();
        }

        void Dim(float a) { GUI.color = new Color(0, 0, 0, a); GUI.DrawTexture(new Rect(-W, 0, W * 3, H), Texture2D.whiteTexture); GUI.color = Color.white; }   // panel kayarken kenar açık kalmasın
        float ColW { get { return Mathf.Min(rx - lx, 360 * u); } }
        float ColX { get { return (W - ColW) / 2; } }

        // ---- menü: 2026-10-26 UI Toolkit'e taşındı (Game/UI/UiKok ve ekranları). Burada yalnız perde ve gizli kare hızı kaldı.

        // ---- maç içi ----
        void Play(Match M)
        {
            var gs = Ayar.GS; bool ogr = M.ogr != null;
            Halo(M); Iz(M); if (ogr) OgrIz(M); HedefYazi(M);
            // skor kutusu: sayı alınca büyür (HTML pop: 24·(1+.5·sin(pop·π)))
            float cx = W / 2, sy = top; var kr = new Rect(cx - 62 * u, sy, 124 * u, 34 * u); Fill(kr, new Color(8 / 255f, 16 / 255f, 17 / 255f, .7f));
            if (!ogr)
            {
                var s0 = Stil(big, Mathf.RoundToInt(24 * fu * (1 + .5f * Mathf.Sin(pop[0] * Mathf.PI)))); var s1 = Stil(big, Mathf.RoundToInt(24 * fu * (1 + .5f * Mathf.Sin(pop[1] * Mathf.PI))));
                Shadowed(new Rect(cx - 60 * u, sy - 6 * u, 60 * u, 46 * u), M.sc[0].ToString(), s0, Kit.Hex(gs.forma)); Shadowed(new Rect(cx - 10 * u, sy, 20 * u, 34 * u), "–", Stil(big, Mathf.RoundToInt(24 * fu)), Kit.Hex("#9dbab3"));
                Shadowed(new Rect(cx, sy - 6 * u, 60 * u, 46 * u), M.sc[1].ToString(), s1, Kit.Hex("#ff7a6b"));
                Fill(new Rect(cx + (M.server == "me" ? -54 : 50) * u, sy + 15 * u, 5 * u, 5 * u), M.server == "me" ? Kit.Hex(gs.forma) : Kit.Hex("#ff7a6b"));   // servis noktası
                // sol üst kutu (2026-10-26, yasin Ar-Ge önerisi "ekle"): "{hedef} sayı · rakip" satırı kalktı (duraklat
                // ekranında zaten yazıyor); ralli sayacı ralli 3'ü geçince belirir ve her vuruşta skorun pop diliyle hafifçe
                // büyür (hareketi azaltta büyümez); "Görev n/3" yerine 3 nokta: biten dolu disk, kalan halka (biçimle de okunur).
                var bs = Stil(left, Mathf.RoundToInt(11 * fu), FontStyle.Bold);
                if (M.rally != sonRalli) { if (M.rally > 3 && !Ayar.reduce) ralliPop = 1; sonRalli = M.rally; }
                bool ralliVar = M.rally > 3; float bw = 64 * u, bh = ralliVar ? 40 * u : 22 * u;
                Fill(new Rect(lx - 10 * u, top + 2 * u, bw, bh), new Color(8 / 255f, 16 / 255f, 17 / 255f, .72f));
                float gy = top + 8 * u;
                if (ralliVar)
                {
                    GUI.color = Kit.Hex("#9dbab3"); GUI.Label(new Rect(lx - 4 * u, top + 4 * u, bw + 20 * u, 18 * u), L.T("Ralli {0}", M.rally), Stil(bs, Mathf.RoundToInt(12 * fu * (1 + .25f * Mathf.Sin(ralliPop * Mathf.PI)))));
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
            if (root.ekran == Ekran.Oyun && !root.paused && Button(new Rect(rx - BH, top, BH, BH), "II")) root.Duraklat();   // Duraklat
            float y = top + 62 * u;
            if (root.rakipKartT > 0 && !ogr) y = RakipKart(M, y);
            if (ogr && root.ekran == Ekran.Oyun) y = OgrKart(M, y);   // görev sonuç ekranında kart kalkar (Over üstüne binmesin)
            // smaç gücü çubuğu
            {
                bool dol = M.energy >= 1; string lab = L.T(dol ? "Sıçrayarak smaç hazır" : "Smaç gücü"); var ls = Stil(leftB, Mathf.RoundToInt(12 * fu));
                float lw = ls.CalcSize(new GUIContent(lab)).x, bx = lx + lw + 10 * u, bw = Mathf.Max(40 * u, rx - bx), by = H - bot - 12 * u;
                bool isaret = ogr && M.OgrA() != null && M.OgrA().bilgi == "guc";
                GUI.color = dol ? Kit.Hex("#ff8a3c") : Kit.Hex("#cfe3dc"); GUI.Label(new Rect(lx, by - 8 * u, lw + 4 * u, 22 * u), lab, ls); GUI.color = Color.white;
                Fill(new Rect(bx, by, bw, 8 * u), new Color(1, 1, 1, .12f));
                if (M.energy > 0) Fill(new Rect(bx, by, bw * Mathf.Min(1, M.energy), 8 * u), dol ? new Color(1, .54f, .24f, Ayar.reduce ? 1 : .75f + .25f * Mathf.Sin(Time.unscaledTime * 1000 / 160)) : Kit.Hex("#d8ff5a"));
                if (isaret) { float nab = Ayar.reduce ? 1 : .6f + .4f * Mathf.Sin(Time.unscaledTime * 6); Cerceve(new Rect(lx - 10 * u, by - 13 * u, rx - lx + 20 * u, 26 * u), new Color(.85f, 1, .35f, nab), 3); }
            }
            float hy = H - bot - 34 * u;
            if (M.sc[0] + M.sc[1] < 2 && M.s != null && !ogr) Shadowed(new Rect(lx, hy - 4 * u, rx - lx, 20 * u), L.T(M.easy ? "Top parlayınca kaydır: yön ve güç senin" : "Dokun: oraya koş · Top parlayınca kaydır"), tiny, Kit.Hex("#9dbab3"));
            if (M.s == null && !M.between && M.svPending == null && !ogr) ServisSeridi(M, hy);
            if (M.HitWindow() && (!ogr || !M.ogr.dur) && !M.easy) { }
            NefesYazi(M.me, true, M); NefesYazi(M.op, false, M);
            foreach (var f in floats)
            {
                // HTML txt: yaylı büyüme (k = 1 + 2,7(q−1)³ + 1,7(q−1)²), .8 sn sonra solar
                float q = Mathf.Min(1, f.a * 5), k = 1 + 2.7f * Mathf.Pow(q - 1, 3) + 1.7f * Mathf.Pow(q - 1, 2), a = f.a > .8f ? 1 - (f.a - .8f) / .4f : 1;
                var c = f.c; c.a = Mathf.Clamp01(a); var st = Stil(mid, Mathf.Max(1, Mathf.RoundToInt(19 * fu * f.size * (.5f + .5f * k))));
                float yy;
                if (f.sabit) yy = H * (f.c == Kit.Hex("#ff7a6b") ? .56f : .4f);
                else { var sp = root.cam.WorldToScreenPoint(f.w + Vector3.up * (.6f + f.a * .5f)); if (sp.z < 0) continue; yy = Mathf.Clamp(H - sp.y - 20 * u, top + 60 * u, H - bot - 80 * u); }
                Shadowed(new Rect(lx, yy, rx - lx, 46 * u), f.t, st, c);
            }
            if (ogrMsgT > 0 && ogrMsg != null) { var c = Kit.Hex("#d8ff5a"); c.a = Mathf.Clamp01(ogrMsgT * 2); Shadowed(new Rect(lx, H * .5f, rx - lx, 60 * u), ogrMsg, mid, c); }
        }
        void Cerceve(Rect r, Color c, float k)
        {
            k *= u; Fill(new Rect(r.x, r.y, r.width, k), c); Fill(new Rect(r.x, r.yMax - k, r.width, k), c); Fill(new Rect(r.x, r.y, k, r.height), c); Fill(new Rect(r.xMax - k, r.y, k, r.height), c);
        }
        void ServisSeridi(Match M, float hy)
        {
            var sat = new List<KeyValuePair<string, Color>>(); bool ilk = M.sc[0] + M.sc[1] < 2;
            string side = L.T(M.ServeSideOf(M.server) == "R" ? "sağ" : "sol");
            sat.Add(new KeyValuePair<string, Color>(M.server == "me" ? L.T("Servis sende · {0} kare", side) : L.T("Rakip servis atıyor · {0} kare", side), Color.white));
            if (M.server == "me" && ilk) sat.Add(new KeyValuePair<string, Color>(L.T("Yukarı kaydır: yavaş kısa · sert fiske flick · hızlı uzun yüksek"), Kit.Hex("#cfe3dc")));
            if (!string.IsNullOrEmpty(M.serveMsg)) sat.Add(new KeyValuePair<string, Color>(L.Cumle(M.serveMsg), Kit.Hex("#ffd27a")));
            if (M.sc[0] + M.sc[1] == 0 && M.gorevler.Count > 0) { var ad = new List<string>(); foreach (var g in M.gorevler) ad.Add(L.Cumle(g.ad)); sat.Add(new KeyValuePair<string, Color>(L.T("Görev: ") + string.Join(" · ", ad.ToArray()), Kit.Hex("#ffd23a"))); }
            var st = new GUIStyle(small) { wordWrap = true }; var st0 = new GUIStyle(st) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(14 * fu) };
            float ww = Mathf.Min(rx - lx, 380 * u), hh = 12 * u; foreach (var s in sat) hh += (s.Value == Color.white ? st0 : st).CalcHeight(new GUIContent(s.Key), ww - 16 * u);
            float y0 = H - bot - 30 * u - hh; Fill(new Rect(W / 2 - ww / 2, y0, ww, hh), new Color(9 / 255f, 16 / 255f, 17 / 255f, .74f));
            float yy = y0 + 6 * u; foreach (var s in sat) { var stt = s.Value == Color.white ? st0 : st; float h = stt.CalcHeight(new GUIContent(s.Key), ww - 16 * u); GUI.color = s.Value; GUI.Label(new Rect(W / 2 - ww / 2 + 8 * u, yy, ww - 16 * u, h), s.Key, stt); yy += h; }
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
            Tex(new Rect(px - R * pul, py - R * pul, 2 * R * pul, 2 * R * pul), ring, I.lunge ? Kit.Hex("#ff9f7a") : Kit.Hex("#d8ff5a"));
            Tex(new Rect(px - R * .33f, py - R * .33f, R * .66f, R * .66f), ringK, new Color(1, .82f, .23f, .9f));
            if (M.ogr != null) return;
            string hnd = Match.HandOfPublic(M.me, "me", M.s.x, M.s.z) == "back" ? "backhand" : "forehand";
            string tag = I.kill ? L.T("file üstü: hızlı kaydır") : I.lunge ? L.T("uzak: uzan") : I.over ? (hnd == "backhand" ? L.T("baş üstü backhand: smaç yok") : L.T("baş üstü: hızlı kaydır = smaç")) : hnd + L.T(": kaydır");
            Shadowed(new Rect(px - 120 * u, py - R - 26 * u, 240 * u, 18 * u), tag, new GUIStyle(small) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(12 * fu) }, I.kill ? Kit.Hex("#ffd27a") : I.lunge ? Kit.Hex("#ff9f7a") : Kit.Hex("#d8ff5a"));
        }
        static readonly Dictionary<string, string> PREV_TR = new Dictionary<string, string> { { "lift", "Lift" }, { "kill", "File vuruşu" }, { "jump", "Sıçrayarak smaç" }, { "smash", "Smaç" }, { "block", "Blok" }, { "drive", "Drive" }, { "drop", "Drop" }, { "clear", "Clear" } };
        void HedefYazi(Match M)
        {
            if (root.previewOn && root.Parmak && !root.ParmakVurdu)
            {
                var sp = root.cam.WorldToScreenPoint(Kit.W(root.previewAt.x, root.previewAt.y, .3f)); if (sp.z < 0) return; string t; PREV_TR.TryGetValue(root.previewTip ?? "", out t);
                Shadowed(new Rect(sp.x - 80 * u, H - sp.y - 20 * u, 160 * u, 18 * u), L.T(t ?? root.previewTip), new GUIStyle(small) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(11 * fu) }, new Color(.85f, 1, .35f, .9f));
            }
            if (M.ogr != null) { var c = root.fx.HedefMerkez(M); if (c.HasValue) { var sp = root.cam.WorldToScreenPoint(c.Value); if (sp.z > 0) Shadowed(new Rect(sp.x - 60 * u, H - sp.y - 9 * u, 120 * u, 18 * u), L.T("Hedef"), new GUIStyle(small) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(12 * fu) }, new Color(.85f, 1, .35f, .85f)); } }
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
                Shadowed(new Rect(lx, yy - 44 * K, rx - lx, 20 * K), L.T("Antrenör diyor"), new GUIStyle(small) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(12 * fu) }, Kit.Hex("#cfe3dc"));
                Shadowed(new Rect(lx, yy - 24 * K, rx - lx, 40 * K), L.T(Match.OGR_AD[o.cagri]).ToUpper() + "!", Stil(big, Mathf.RoundToInt(30 * fu * b)), Kit.Hex("#d8ff5a"));
                return;
            }
            if (o.n >= 1 && o.miss < 2) return;   // ustalık: ikinci deneme izsiz
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
        float RakipKart(Match M, float y)
        {
            // HTML rkGir: yukarıdan 10 px, solarak, .22 sn (hareketi azaltta anında)
            float gk = Ayar.reduce ? 1 : Mathf.Clamp01((1.5f - root.rakipKartT) / .22f); gk = 1 - (1 - gk) * (1 - gk); float y0 = y; y -= (1 - gk) * 10 * u;
            GUI.contentColor = GUI.backgroundColor = new Color(1, 1, 1, gk);
            var st = M.stil; float w = ColW, x = ColX, h = 92 * u; var r = new Rect(x, y, w, h); blocks.Add(r); GUI.Box(r, GUIContent.none, card);
            var oc = Kit.Hex(Match.OpRenk(st, Ayar.FORMA)); oc.a = gk; GUI.color = oc; GUI.DrawTexture(new Rect(x, y, 6 * u, h), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(x + 16 * u, y + 8 * u, w - 24 * u, 26 * u), st.isim, Stil(leftB, Mathf.RoundToInt(20 * fu)));
            var kb = KariyerVeri.Al(st.kariyer); var im = st.patron ? KariyerVeri.Imza(st.imza) : null;   // serbest maçta taban tarz imzayı taşır ama atmaz
            GUI.Label(new Rect(x + 16 * u, y + 34 * u, w - 24 * u, 18 * u), kb != null ? L.T(st.ad) + " · " + L.T(kb.yer) : L.T(st.ad), left);
            // Patron (final): ipucu yerine imza vuruşu ve cevabı (tasarım §3.3: kart rakibin tek fikrini söyler; finalde o fikir imzadır)
            GUI.Label(new Rect(x + 16 * u, y + 54 * u, w - 24 * u, 36 * u), im != null ? L.T(im.ad) + ": " + L.T(im.cevap) : L.T(st.ipucu), new GUIStyle(left) { wordWrap = true, fontSize = Mathf.RoundToInt(12 * fu) });
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
            float h1 = ws.CalcHeight(new GUIContent(met), iw), h2 = ns.CalcHeight(new GUIContent(L.T(a.neden)), iw);
            int? hb = M.OgrHedefBolge(a); bool bant = hb.HasValue;
            // canlı okuma: parmak ekrandayken "şu an bıraksan ne çıkar", sonra 2,2 sn "Çıkan"
            string tip; float sp; root.CanliOkuma(out tip, out sp); bool canliVar = root.Parmak && (tip != null || sp > 0); string canli = null; bool ok = false; float igne = -1;
            if (a.bilgi == null && !(a.dril != null && a.dril.olcu == "split"))   // split görevinde vuruş türü ölçülmez: canlı okuma yanıltır
            {
                if (canliVar) { ok = M.OgrUygunCanli(a, tip, false); igne = sp; canli = (ok ? "✓ " : "✗ ") + L.T("Şu an") + ": " + (tip != null ? L.T(Match.OGR_AD.ContainsKey(tip) ? Match.OGR_AD[tip] : tip) : L.T("Yukarı kaydır")); }
                else if (o.sonSp >= 0 && M.realClock - o.sonT < 2.2f) { ok = M.OgrUygunCanli(a, o.sonTip, o.sonSlice); igne = o.sonSp; canli = (ok ? "✓ " : "✗ ") + L.T("Çıkan") + ": " + (o.sonTip != null ? (o.sonSlice ? "Slice" : L.T(Match.OGR_AD.ContainsKey(o.sonTip) ? Match.OGR_AD[o.sonTip] : o.sonTip)) : ""); }
                else if (a.drill && o.cagri != null && M.s != null && M.s.from == "op") canli = L.T("Antrenör: ") + L.T(Match.OGR_AD[o.cagri]);
            }
            float h = 12 * u + 18 * u + 24 * u + h1 + 6 * u + h2 + (bant ? 34 * u : 0) + (canli != null ? 22 * u : 0) + 10 * u + BH + (a.bilgi != null ? BH + 8 * u : 0) + 10 * u;
            var r = new Rect(x, y, w, h); blocks.Add(r); GUI.Box(r, GUIContent.none, card);
            float yy = y + 10 * u;
            GUI.Label(new Rect(x + 12 * u, yy, iw, 18 * u), say, Stil(left, Mathf.RoundToInt(11 * fu))); yy += 18 * u;
            GUI.Label(new Rect(x + 12 * u, yy, iw, 24 * u), L.T(a.bas), leftB); yy += 24 * u;
            GUI.Label(new Rect(x + 12 * u, yy, iw, h1), met, ws); yy += h1 + 6 * u;
            var nc = GUI.color; GUI.color = new Color(1, 1, 1, .7f); GUI.Label(new Rect(x + 12 * u, yy, iw, h2), L.T(a.neden), ns); GUI.color = nc; yy += h2;
            if (bant)
            {   // hız bandı: bölgeler oyunun kendi eşikleri; hedef bölge ▸ ile ve çerçeveyle (yalnız renkle değil)
                yy += 6 * u; var sinir = Match.OgrBolgeSinir(a); var adlar = Match.OgrBolgeAd(a); float bx = x + 12 * u, bwid = iw, b0 = 0;
                for (int i = 0; i < adlar.Length; i++)
                {
                    float b1 = i < sinir.Length ? sinir[i] : Match.OGR_MAX; var zr = new Rect(bx + bwid * b0 / Match.OGR_MAX, yy, bwid * (b1 - b0) / Match.OGR_MAX - 2 * u, 22 * u); bool hed = i == hb.Value;
                    Fill(zr, hed ? new Color(.85f, 1, .35f, .28f) : new Color(1, 1, 1, .08f)); if (hed) Cerceve(zr, new Color(.85f, 1, .35f, .8f), 1.5f);
                    GUI.Label(zr, (hed ? "▸ " : "") + L.T(adlar[i]), Stil(tiny, -1, hed ? FontStyle.Bold : FontStyle.Normal)); b0 = b1;
                }
                if (igne >= 0) { float ix = bx + bwid * Mathf.Min(igne, Match.OGR_MAX) / Match.OGR_MAX; Fill(new Rect(ix - 1.5f * u, yy - 4 * u, 3 * u, 30 * u), Color.white); }
                yy += 28 * u;
            }
            if (canli != null) { GUI.color = canli.StartsWith("✓") ? Kit.Hex("#d8ff5a") : canli.StartsWith("✗") ? Kit.Hex("#ffd27a") : Kit.Hex("#cfe3dc"); GUI.Label(new Rect(x + 12 * u, yy + 2 * u, iw, 20 * u), canli, leftB); GUI.color = Color.white; yy += 22 * u; }
            yy += 10 * u;
            if (a.bilgi != null) { if (Button(new Rect(x + 12 * u, yy, iw, BH), "Anladım, devam", true)) M.OgrBilgiGec(); yy += BH + 8 * u; }
            if (Button(new Rect(x + 12 * u, yy, iw, BH), a.dril != null ? "Görevi bitir" : "Antrenmanı geç")) root.OgrGec();
            return y + h + 8 * u;
        }

        void Pause(Match M)
        {
            Dim(.6f); float x = ColX, w = ColW, y = H * .3f;
            Shadowed(new Rect(x, y, w, 40 * u), L.T("Duraklatıldı"), big, Color.white); y += 46 * u;
            if (M.Dril != null) Shadowed(new Rect(x, y, w, 24 * u), L.T(M.Dril.ad) + " · " + L.T("Top {0}/{1} · Hedefte {2}", Mathf.Min(M.ogr.dene, M.Dril.top), M.Dril.top, M.ogr.isabet), small, Color.white); else
            Shadowed(new Rect(x, y, w, 24 * u), M.sc[0] + " – " + M.sc[1] + " · " + L.T("{0} sayı", M.target) + " · " + L.T("Rakip") + " " + M.RakipAd(false) + (KariyerVeri.Var(M.stil.kariyer) ? " · " + L.T(KariyerVeri.Al(M.stil.kariyer).yer) : ""), small, Color.white); y += 40 * u;
            if (Button(new Rect(x, y, w, BH * 1.1f), "Maça devam et", true)) root.Devam(); y += BH * 1.1f + 10 * u;
            // HTML duraklat satırı: Ses · Maçı baştan başlat · Ana menü (oyun sırasında ses düğmesi yalnız burada)
            if (Chip(new Rect(x, y, w, BH), Ayar.ses ? "Ses açık" : "Ses kapalı", Ayar.ses)) { Ayar.ses = !Ayar.ses; AudioListener.volume = Ayar.ses ? 1 : 0; Ayar.Kaydet(); } y += BH + 10 * u;
            if (Button(new Rect(x, y, w, BH), "Maçı baştan başlat")) root.TekrarOyna(); y += BH + 10 * u;
            if (Button(new Rect(x, y, w, BH), "Ana menü")) root.AnaMenu();
        }

        float overH;   // son karedeki içerik yüksekliği: "Ayrıntılar" açılınca blok yukarı kayar, alt düğmeler ekranda kalır
        void Over(Match M)
        {
            Dim(.6f); float x = ColX, w = ColW, y = Mathf.Max(top + 8 * u, Mathf.Min(H * .18f, H - bot - overH - 8 * u)), y0 = y;
            float th = big.CalcHeight(new GUIContent(root.bittiBaslik), w); var bs = root.bittiBaslik.Length > 24 ? mid : big; th = bs.CalcHeight(new GUIContent(root.bittiBaslik), w);
            Shadowed(new Rect(x, y, w, th), root.bittiBaslik, bs, Color.white); y += th + 8 * u;
            if (root.bittiSkor != null) { Shadowed(new Rect(x, y, w, 44 * u), root.bittiSkor, Stil(big, Mathf.RoundToInt(32 * fu)), Color.white); y += 48 * u; }   // görev: "7/12 hedefte"
            else { Shadowed(new Rect(x, y, w, 60 * u), M.sc[0] + " – " + M.sc[1], Stil(big, Mathf.RoundToInt(48 * fu)), Color.white); y += 64 * u; }
            // Kariyer yıldızları (2026-10-26): dolu/boş yıldız + sayı yazıyla (yalnız simgeye güvenilmez), yeniler ayrıca
            if (!string.IsNullOrEmpty(root.bittiYildiz)) { float yh = small.CalcHeight(new GUIContent(root.bittiYildiz), w); Shadowed(new Rect(x, y, w, yh), root.bittiYildiz, Stil(small, -1, FontStyle.Bold), Kit.Hex("#ffd23a")); y += yh + 6 * u; }
            var od = root.odul;
            if (od != null)
            {   // HTML oOdul: +Tüy · +XP (ekler) · Seviye! · Toplam
                string t = L.T("+{0} Tüy{1} · +{2} XP", od.tuy, "", od.xp) + (od.ek.Count > 0 ? " (" + string.Join(", ", od.ek.ConvertAll(s => L.Cumle(s)).ToArray()) + ")" : "") + (od.sonra > od.once ? L.T(" · Seviye {0}!", od.sonra) : "") + L.T(" · Toplam {0}{1}", Ayar.gear.eko.tuy, " " + L.T("Tüy"));
                float oh = small.CalcHeight(new GUIContent(t), w); Shadowed(new Rect(x, y, w, oh), t, Stil(small, -1, FontStyle.Bold), Kit.Hex("#ffd23a")); y += oh + 6 * u;
            }
            // HTML oYukselt: yükseltme yapılabiliyorsa maç sonundan doğrudan Ekipman paneline götüren düğme
            if (od != null && root.yukseltHazir) { if (Button(new Rect(x, y, w, BH), "Ekipmanı yükselt")) { root.AnaMenu(); if (root.ui != null) root.ui.SekmeAc(Sekme.Soyunma); } y += BH + 10 * u; }
            // Özet (Ar-Ge 23 Eki #2): iki kart (sayı + adı; renk anlam taşımaz) + bir ipucu, tam liste "Ayrıntılar" ardında.
            // Yükseklikler CalcHeight'tan: metin uzasa da üst üste binmez.
            { int n = root.bittiKart.Count; float g = 10 * u, kw = n > 1 ? (w - g) / 2 : w, kh = 0; var vs = Stil(big, Mathf.RoundToInt(28 * fu));
              foreach (var k in root.bittiKart) kh = Mathf.Max(kh, small.CalcHeight(new GUIContent(L.T(k.ad)), kw - 16 * u));
              kh += 36 * u + 14 * u;
              for (int i = 0; i < n; i++) { var r = new Rect(x + i * (kw + g), y, kw, kh); GUI.Box(r, GUIContent.none, card); var k = root.bittiKart[i];
                  GUI.Label(new Rect(r.x, r.y + 6 * u, r.width, 34 * u), k.deger.ToString(), vs); GUI.Label(new Rect(r.x + 8 * u, r.y + 40 * u, r.width - 16 * u, kh - 44 * u), L.T(k.ad), small); }
              y += kh + 10 * u; }
            if (!string.IsNullOrEmpty(root.bittiIpucu)) { float ph = small.CalcHeight(new GUIContent(root.bittiIpucu), w); Shadowed(new Rect(x, y, w, ph), root.bittiIpucu, small, Color.white); y += ph + 10 * u; }
            bool ayrintiVar = !string.IsNullOrEmpty(root.bittiBilgi);   // görev sonucunda ayrıntı listesi yok
            if (ayrintiVar && Button(new Rect(x, y, w, BH), ayrinti ? "Ayrıntıları gizle" : "Ayrıntılar")) ayrinti = !ayrinti; if (ayrintiVar) y += BH + 10 * u;
            if (ayrinti && ayrintiVar) { float ih = small.CalcHeight(new GUIContent(root.bittiBilgi), w); Shadowed(new Rect(x, y, w, ih), root.bittiBilgi, small, new Color(1, 1, 1, .8f)); y += ih + 16 * u; }
            if (ButtonRaw(new Rect(x, y, w, BH * 1.15f), root.bittiDugme, true)) root.TekrarOyna(); y += BH * 1.15f + 10 * u;
            if (root.bittiIlk) { if (Button(new Rect(x, y, w, BH), "Tam antrenman")) root.Antrenman(); y += BH + 10 * u; }
            // 2026-10-26: kayıpta önerilen görev (tek düğme, adıyla) ve görev sonucundan görev listesine dönüş
            var od2 = root.bittiDril != null ? Dril.Bul(root.bittiDril) : null;
            if (od2 != null) { if (ButtonRaw(new Rect(x, y, w, BH), L.T("Antrenman: {0}", L.T(od2.ad)), false)) root.DrilOyna(od2.id); y += BH + 10 * u; }
            if (root.mod == Mod.Dril) { if (Button(new Rect(x, y, w, BH), "Antrenman görevleri")) { root.AnaMenu(); if (root.ui != null) root.ui.AntrenmanAc(); } y += BH + 10 * u; }
            if (Button(new Rect(x, y, w, BH), "Ana menü")) root.AnaMenu();
            overH = y + BH - y0;
        }

        // ---- smaç perdesi (HTML perde): 5 halka × 24 dilim, komşu halkalar ters yönde 1,2 rad/sn döner. Kapanış: disk
        // 120 ms gecikmeyle 330 ms'de büyür (kübik ease-out), bu sırada top hız çizgileriyle sol alttan sağ üste 280 ms'de
        // uçar; maç örtünün altında kurulur; açılış 350 ms'de küçülür (ease-in-out). Hareketi azalt: yalnız solma.
        void PerdeCiz()
        {
            if (perdeEvre == 0) return; if (perdeTex == null) PerdeDoku();
            bool rm = Ayar.reduce; float sc = 1, al = 1;
            if (perdeEvre == 1) { if (rm) al = Mathf.Clamp01(perdeT / .2f); else { float t = Mathf.Clamp01((perdeT - .12f) / .33f); sc = 1 - Mathf.Pow(1 - t, 3); } }
            else if (perdeEvre == 3) { if (rm) al = 1 - Mathf.Clamp01(perdeT / .25f); else { float t = Mathf.Clamp01(perdeT / .35f); sc = 1 - (t < .5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2); } }
            if (sc <= .001f || al <= .001f) { if (perdeEvre == 1 && !rm) UcanTop(); return; }
            float R = Mathf.Sqrt(W * W + H * H) * .55f * sc, rr = R * .62f; var c = new Vector2(W / 2, H / 2); var m = GUI.matrix;
            for (int i = 0; i < 5; i++)
            {
                float r = i == 0 ? R : PERDE_F[i][0] * rr; float ang = rm ? 0 : perdeAci * Mathf.Rad2Deg * (i % 2 == 1 ? -1 : 1);
                GUIUtility.RotateAroundPivot(ang, c); Tex(new Rect(c.x - r, c.y - r, 2 * r, 2 * r), perdeTex[i], new Color(1, 1, 1, al)); GUI.matrix = m;
            }
            blocks.Add(new Rect(0, 0, W, H));
            if (perdeEvre == 1 && !rm) UcanTop();
            if (perdeBek && root.yukleyici != null)
            {   // HTML perdeBekle: ortada dönen tüy top, altında "Hazırlanıyor…"
                float ts = Mathf.Min(W, H) * .26f; GUI.DrawTexture(new Rect(c.x - ts / 2, c.y - ts / 2, ts, ts), root.yukleyici.rt);
                string t = L.T("Hazırlanıyor…"); var ps = Stil(small, -1, FontStyle.Bold); var sz = ps.CalcSize(new GUIContent(t));
                var pr = new Rect(c.x - sz.x / 2 - 14 * u, c.y + ts / 2 + 6 * u, sz.x + 28 * u, sz.y + 10 * u); Fill(pr, new Color(0, 0, 0, .45f)); GUI.Label(pr, t, ps);
            }
        }
        void UcanTop()
        {   // HTML ucan: hız çizgileri + top, sol alttan (−.1W, .85H) sağ üste (1,1W, .12H), 280 ms, kübik ease-out
            float t = Mathf.Clamp01(perdeT / .28f); if (t >= 1) return; float e = 1 - Mathf.Pow(1 - t, 3);
            var p = Vector2.Lerp(new Vector2(-W * .1f, H * .85f), new Vector2(W * 1.1f, H * .12f), e); float ang = Mathf.Atan2(-H * .73f, W * 1.2f);
            float Lw = W * .5f, boy = W * .3f; var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)); var nrm = new Vector2(-dir.y, dir.x);
            for (int i = 0; i < 6; i++) { float off = (i - 2.5f) * Lw * .09f, l = Lw * (.5f + .5f * ((i * 37) % 11) / 11f); var a = p - dir * Lw * .25f + nrm * off; Cizgi(a, a - dir * l, Mathf.Max(1.5f, Lw * .018f), new Color(1, 1, 1, .8f)); }
            if (topIkon == null) topIkon = TopIkon(); var mm = GUI.matrix; GUIUtility.RotateAroundPivot(ang * Mathf.Rad2Deg, p); GUI.DrawTexture(new Rect(p.x - boy * .9f, p.y - boy * .35f, boy, boy * .7f), topIkon); GUI.matrix = mm;
        }
        void PerdeDoku()
        {
            perdeTex = new Texture2D[5]; const int N = 24, n = 256;
            for (int i = 0; i < 5; i++)
            {
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; Color c1 = Kit.Hex(PERDE_C[i, 0]), c2 = Kit.Hex(PERDE_C[i, 1]); var px = new Color32[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                    {
                        float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx); if (a < 0) a += Mathf.PI * 2;
                        int dil = Mathf.FloorToInt(a / (Mathf.PI * 2) * N);
                        bool ic; if (i == 0) ic = r <= 1; else { float aa = a - Mathf.PI / N; float sec = Mathf.PI * 2 / N; float lok = Mathf.Repeat(aa, sec) - sec / 2; ic = r * Mathf.Cos(lok) <= Mathf.Cos(sec / 2); }   // iç halkalar 24-gen
                        float rs = i == 0 ? PERDE_F[0][0] * .62f / 1 : 1; bool dilim = dil % 2 == 0 && r <= (i == 0 ? 1 : 1);
                        px[y * n + x] = ic ? (Color32)(dilim ? c2 : c1) : new Color32(0, 0, 0, 0);
                    }
                t.SetPixels32(px); t.Apply(); perdeTex[i] = t;
            }
        }
        static Texture2D TopIkon()
        {   // perdede uçan top: sağda mantar, solda açılan etek (2B, yalnız bu geçiş için)
            const int w = 96, h = 64; var t = new Texture2D(w, h, TextureFormat.RGBA32, false); var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)w, fy = (y - h / 2f) / (h / 2f); Color c = new Color(0, 0, 0, 0);
                    float mx = .82f, mr = .17f; float dx = (fx - mx) * w / (h / 2f), dd = Mathf.Sqrt(dx * dx + fy * fy);
                    if (fx < .78f) { float yar = Mathf.Lerp(.95f, .25f, fx / .78f); if (Mathf.Abs(fy) <= yar) { float sat = .92f + .08f * Mathf.Sin(fy * 18); c = new Color(sat, sat, sat * .99f, 1); if (Mathf.Abs(fx - .45f) < .015f || Mathf.Abs(fx - .32f) < .015f) c = new Color(.2f, .25f, .33f, 1); } }
                    if (dd < mr * w / (h / 2f) * .35f) c = Color.Lerp(Kit.Hex("#fff6e3"), Kit.Hex("#c9a978"), dd / (mr * w / (h / 2f) * .35f));
                    px[y * w + x] = c;
                }
            t.SetPixels32(px); t.Apply(); return t;
        }
    }
}
