// Menü kabuğu (komple-oyun tasarımı adım 1, 2026-10-26): IMGUI menüleri UI Toolkit'e taşındı; maç içi HUD, duraklat,
// maç sonu ve smaç perdesi IMGUI'de kalır (Hud; tasarım §6 adım 1 ve §7.2: HUD az öğeli, Adım 3'ten sonra taşınır).
// Ekran haritası (§2.1): alt sekme çubuğu Oyna · Kariyer · Dolap (Soyunma) · Atölye · Profil (30 Eki: Atölye Dolap'tan
// ayrıldı, gerekçe SoyunmaOdasi başlığında; ilk hali 4 sekmeydi); Ayarlar sekme değil, üst şeritte ⚙
// (oyuncu oturumda bir kez bile girmez). Ayarlar › Nasıl oynanır / Lisanslar tam sayfa, Hızlı maç kurulumu alt sayfa.
// Gezinti kuralı Core/Gezinti'de (saf C#, test edilir); burası yalnız görünüm.
// Kurulum çalışma zamanında: UIDocument + PanelSettings (Resources/ui/PanelSettings varsa o; Editor'de DemoKurulum üretir,
// shader başvurusu için; yoksa ScriptableObject.CreateInstance), iskelet Resources/ui/menu.uxml (yoksa koddan, aynı adlarla), biçim Resources/ui/menu_stil.uss (2026-10-26 gece: eski adı menu.uss, aşağıda neden),
// varsayılan tema Resources/ui/tema.tss (yoksa yerleşik yazı tipine düşülür). Editor API'si yok: sahne dosyasız kurulum
// (GameRoot gibi) her Unity sürümünde aynı sonucu versin.
// Ölçek: ScaleWithScreenSize, referans 375 pt genişlik (iPhone SE/mini). Neden ConstantPhysicalSize değil (tasarım Ek A.7
// öyle diyordu): Screen.dpi 3× iPhone'larda yuvarlanmış, CI'daki xvfb'de 0 geliyor; genişliğe göre ölçek her telefonda
// panel birimini ≥ 1 pt yapar, böylece 44 birimlik düğme en dar telefonda da ≥ 44 pt kalır. Geniş ekranda (iPad) yüksekliğe
// göre ölçeklenir. 2026-10-27: eşleme ekran boyu değişince yeniden seçilir; kısa ekran (SE) ve geniş ekran (iPad) boyut
// sınıfları, güvenli alanın katman konumlarına yazılması ve afişin boşluğa oturması Duyarli.cs'de.
// Performans (§7): menü ağacı yalnız bir şey değişince yeniden kurulur; Update'te ayırma yok (yalnız karşılaştırma ve
// değişince stil yazma). Maçta menü kökü display:none (çizim ve dokunma yok).
// Perde: IMGUI perdesi ile UI Toolkit panelinin çizim sırası sürüme göre değişebilir; menü perdenin örtüsüyle birlikte solar
// (opaklık = 1 − örtü) ve perde varken dokunuşu bir kalkan yutar. Hangi sıra olursa olsun perde menünün üstünde görünür.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok : MonoBehaviour
    {
        public GameRoot root;
        UIDocument doc; bool temaYok;
        VisualElement kok, zemin, alan, ust, icerik, sayfa, sekmeCubugu, altKatman, altSayfa, kalkan;
        const int SEKME_N = 5;
        readonly VisualElement[] sekmeKok = new VisualElement[SEKME_N]; readonly VisualElement[] sekmeDugme = new VisualElement[SEKME_N];
        readonly bool[] kirli = { true, true, true, true, true }; bool ustKirli = true;
        readonly Gezinti gez = new Gezinti();
        float kenarX = -1; float K = 1; Rect sonSafe; float sonPanelW = -1, sonOrtu = -1; bool sonGorunur = true, sonReduce, kalkanAcik, kuruldu; string sonDil;
        static readonly string[] SEKME_AD = { "Oyna", "Kariyer", "Dolap", "Profil", "Atölye" };
        static readonly Sekme[] SEKME_SIRA = { Sekme.Oyna, Sekme.Kariyer, Sekme.Soyunma, Sekme.Atolye, Sekme.Profil };   // çubukta görünen sıra

        public Sekme AktifSekme { get { return gez.sekme; } }
        // Gizli kare hızı göstergesinin ekran adı (Hud.FpsEkran; HTML ile aynı adlar, çevrilmez).
        public string FpsAdi
        {
            get
            {
                string u = gez.Ust; if (u == Gezinti.SEVIYE) return "Seviye yolu"; if (u == Gezinti.GELISIM) return "Gelişim"; if (u == Gezinti.AYAR) return "Ayarlar"; if (u == Gezinti.NASIL) return "Nasıl oynanır"; if (u == Gezinti.LISANS) return "Lisanslar"; if (u == Gezinti.HIZLI) return "Hızlı maç"; if (u == Gezinti.KART) return "Maç kartı"; if (u == Gezinti.YUVA) return "Yuva"; if (u == Gezinti.ANTRENMAN) return "Antrenman"; if (u == Gezinti.DRIL) return "Görev kartı"; if (u == Gezinti.GUNLUK) return "Günün maçı"; if (u == Gezinti.SEC) return "Oyuncunu seç"; if (u == Gezinti.ARKADAS) return "Arkadaşınla oyna"; if (u == Gezinti.HAREKET) return "Nasıl oynamak istersin?"; if (u == Gezinti.DUKKAN) return "Tüy dükkânı";
                if (gez.sekme == Sekme.Soyunma) return root.vitrin.hedefGec >= 1 ? "Raket tezgâhı" : "Dolap";
                if (gez.sekme == Sekme.Atolye) return "Atölye";
                return gez.sekme == Sekme.Kariyer ? "Kariyer" : gez.sekme == Sekme.Profil ? "Profil" : "Menü";
            }
        }

        public static UiKok Kur(GameRoot r)
        {
            var g = new GameObject("menu_ui"); g.SetActive(false); g.transform.SetParent(r.transform, false);
            var u = g.AddComponent<UiKok>(); u.root = r; u.doc = g.AddComponent<UIDocument>(); u.doc.panelSettings = u.Panel();
            var uxml = Resources.Load<VisualTreeAsset>("ui/menu"); if (uxml != null) u.doc.visualTreeAsset = uxml;
            g.SetActive(true); if (u.doc.rootVisualElement != null) u.Iskelet(); return u;   // kök henüz yoksa Update'te kurulur
        }

        PanelSettings Panel()
        {
            // Editor'de üretilen varlık (DemoKurulum.PanelAyari) shader başvurusunu taşır; yoksa çalışma zamanında üretilir.
            // Ölçek ve tema her iki durumda da buradan yazılır (tek kaynak).
            var p = Resources.Load<PanelSettings>("ui/PanelSettings"); if (p == null) { p = ScriptableObject.CreateInstance<PanelSettings>(); p.name = "menu_panel"; }
            p.scaleMode = PanelScaleMode.ScaleWithScreenSize; p.referenceResolution = new Vector2Int(375, 812);
            p.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; p.match = Screen.height > 0 && (float)Screen.width / Screen.height > .6f ? 1 : 0;
            p.sortingOrder = 10;
            if (p.themeStyleSheet == null) { var tema = Resources.Load<ThemeStyleSheet>("ui/tema"); if (tema != null) p.themeStyleSheet = tema; }
            temaYok = p.themeStyleSheet == null;
            return p;
        }

        void Iskelet()
        {
            Ikonlar.Hazirla(); K = Ayar.MenuYaziK; sonDil = Ayar.dil;
            var r = doc.rootVisualElement; r.pickingMode = PickingMode.Ignore;
            r.style.position = Position.Absolute; r.style.left = 0f; r.style.top = 0f; r.style.right = 0f; r.style.bottom = 0f;   // tam ekran (UIDocument kökü)
            // 2026-10-26 gece (yasin build 40 "menüler sahaya iç içe"; CI r24–r36): biçim sayfası eskiden "ui/menu" adıyla
            // yükleniyordu, oysa aynı yolda menu.uxml de var ve VisualTreeAsset içinde bir StyleSheet alt varlığı (satır içi
            // stiller) taşır. Resources.Load<StyleSheet>("ui/menu") içe aktarma sırasına göre o boş sayfayı döndürebiliyordu:
            // menü kuralsız (saydam, yerleşimsiz, siyah yazı) çıkıyor, maç sahnesi arkadan görünüyordu. Ad ayrıldı (menu_stil),
            // yine de sayfa adıyla doğrulanır; kural yoksa yüksek sesle günlüğe yazılır.
            var uss = StilSayfasi(); if (uss != null) r.styleSheets.Add(uss); else Debug.LogError("Birdie Smash: Resources/ui/menu_stil.uss yok ya da boş");
            kok = r.Q<VisualElement>("kok");
            if (kok == null) { kok = KodIskelet(); r.Add(kok); }
            // Menü kıpırdarken ekran hızı yükselir (Kalite.Hareket, 2026-10-28): dokunma, basılı sürükleme, tekerlek ve
            // her USS geçişi (sekme çizgisi, sayfa kayması, düğme). TrickleDown: alt öğeler olayı tüketse de kök duyar.
            kok.RegisterCallback<PointerDownEvent>(e => Kalite.Hareket(), TrickleDown.TrickleDown);
            kok.RegisterCallback<PointerMoveEvent>(e => { if (e.pressedButtons != 0) Kalite.Hareket(); }, TrickleDown.TrickleDown);
            kok.RegisterCallback<WheelEvent>(e => Kalite.Hareket(), TrickleDown.TrickleDown);
            BasisSesiKur();
            kok.RegisterCallback<TransitionRunEvent>(e => Kalite.Hareket(), TrickleDown.TrickleDown);
            zemin = kok.Q<VisualElement>("zemin"); alan = kok.Q<VisualElement>("alan"); ust = kok.Q<VisualElement>("ust"); icerik = kok.Q<VisualElement>("icerik");
            ust.schedule.Execute(PortreIsit).Every(300);   // Dolap açıkken bugünkü portreyi durgun anda ısıtır (aşağıda)
            sayfa = kok.Q<VisualElement>("sayfa"); sekmeCubugu = kok.Q<VisualElement>("sekmeler"); ReklamKur(kok.Q<VisualElement>("reklam")); altKatman = kok.Q<VisualElement>("alt-katman"); kalkan = kok.Q<VisualElement>("kalkan");
            // 2026-10-26 Yayın Gecesi: gövde yazı tipi (2026-10-29 Tüycükler: Nunito; eskisi Barlow; Resources/fonts, OFL). Sınıf kuralları (menu_stil.uss) kalın ve başlık
            // dosyalarını seçer; kökteki satır içi değer, USS çözülemezse ya da tema yoksa da gövde yazısına düşülsün diye.
            var barlow = Resources.Load<Font>("fonts/Nunito-SemiBold");
            if (barlow != null) kok.style.unityFontDefinition = FontDefinition.FromFont(barlow);
            else if (temaYok)
            {   // tema yoksa yazı tipi de yok: yerleşik çalışma zamanı yazı tipi (Unity 2022.2+ adı LegacyRuntime.ttf)
                try { var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (f != null) kok.style.unityFontDefinition = FontDefinition.FromFont(f); } catch (System.Exception) { }
            }
            ZeminKur();
            for (int i = 0; i < SEKME_N; i++) { sekmeKok[i] = new VisualElement(); sekmeKok[i].AddToClassList("sekme-kok"); sekmeKok[i].AddToClassList("tam"); icerik.Add(sekmeKok[i]); }
            altKatman.RegisterCallback<PointerDownEvent>(e => { if (e.target == altKatman) AltKapatKay(); });
            kalkan.RegisterCallback<PointerDownEvent>(e => { if (root.HudKatman != null) root.HudKatman.PerdeAtla(); e.StopPropagation(); });
            // iOS sol kenardan kaydırma = Geri (tam sayfada; maçta yok, menü maçta gizli). Kenar 24 pt. 2026-11-11: sayfa parmağı
            // izler, bırakınca karar verilir (KenarSurukle notu); hareketi azaltta eski 70 pt eşik ve anında geçiş.
            sayfa.RegisterCallback<PointerDownEvent>(e => { kenarX = e.position.x - sayfa.worldBound.x < 24 ? e.position.x : -1; kenarDx = 0; kenarHiz = 0; kenarT = Time.realtimeSinceStartup; kenarSurukle = false; }, TrickleDown.TrickleDown);
            sayfa.RegisterCallback<PointerMoveEvent>(e => KenarSurukle(e.position.x), TrickleDown.TrickleDown);
            sayfa.RegisterCallback<PointerUpEvent>(e => KenarBirak(), TrickleDown.TrickleDown);
            sayfa.RegisterCallback<PointerCancelEvent>(e => KenarBirak(), TrickleDown.TrickleDown);
            DuyarliGuncelle();   // 2026-10-27: boyut sınıfı (kısa / geniş) ilk kurulumdan önce: kodda yazılan boyutlar ona bakar (Duyarli.cs)
            HepsiniKur(); kuruldu = true;
        }

        // UXML yoksa aynı ağaç (adlar menu.uxml ile aynı).
        VisualElement KodIskelet()
        {
            var k = Y("kok", "kok tam", null); Y("zemin", "tam", k).pickingMode = PickingMode.Ignore;
            var g = Y("govde", "govde tam", k); Y("reklam", "reklam-alani", g); var a = Y("alan", "alan", g); Y("ust", "ust-serit", a); Y("icerik", "icerik", a); Y("sayfa", "sayfa tam", a);
            Y("sekmeler", "sekme-cubugu", g); Y("alt-katman", "alt-katman tam", k); Y("kalkan", "kalkan tam", k);
            return k;
        }

        static VisualElement Y(string ad, string cls, VisualElement ebeveyn) { var v = new VisualElement { name = ad }; foreach (var c in cls.Split(' ')) if (c != "") v.AddToClassList(c); if (ebeveyn != null) ebeveyn.Add(v); return v; }

        // ---- kare başı: ayırma yok ----
        void Update()
        {
            if (root == null) return; if (!kuruldu) { if (doc != null && doc.rootVisualElement != null) Iskelet(); return; }
            bool gor = root.ekran == Ekran.Menu;
            if (gor != sonGorunur)
            {
                sonGorunur = gor; kok.style.display = gor ? DisplayStyle.Flex : DisplayStyle.None;
                if (gor) { Kirlet(); salonKirli = true; }   // maçta Tüy, XP, kariyer değişmiş olabilir; vitrin Uygula'da açılır; salon fonu yeniden çekilir
            }
            if (!gor) { if (root.afis.Acik) root.afis.Kapat(); SahneCiz(true, false); ReklamSdk.Serit(null); return; }
            if (!temaFontVar) TemaFontOku();
            float k = Ayar.MenuYaziK; if (Mathf.Abs(k - K) > .001f || sonDil != Ayar.dil) { K = k; sonDil = Ayar.dil; HepsiniKur(); }
            if (Ayar.reduce != sonReduce) { sonReduce = Ayar.reduce; kok.EnableInClassList("az-hareket", sonReduce); }
            GuvenliAlan();
            if (root.afis != null) root.afis.Dondur = gez.AcikAltSayfa != null;   // alt sayfa afişi örter: dans/nefes durur (Afis.Dondur)
            var hud = root.HudKatman; float o = hud != null ? hud.PerdeOrtu : 0; bool pv = hud != null && hud.PerdeVar;
            if (Mathf.Abs(o - sonOrtu) > .01f || (o == 0 && sonOrtu != 0)) { sonOrtu = o; kok.style.opacity = 1 - o; }
            PerdeKaymaUygula(hud);   // 2026-10-27 perde v2: içerik solarken 12 pt aşağı (yalnız translate)
            if (pv != kalkanAcik) { kalkanAcik = pv; kalkan.style.display = pv ? DisplayStyle.Flex : DisplayStyle.None; if (!pv) salonKirli = true; }   // perde kalkınca fon bir kez daha (açılışta sahne perde altında kuruluyor olabilir)
            SahneCiz(pv || o > 0, true);   // perde açılırken/kapanırken menü solar: altında maç sahnesi görünmeli
            ReklamSeritYeri(pv, o);
            if (Input.GetKeyDown(KeyCode.Escape) && !pv) Geri();   // Android geri tuşu
        }

        // Güvenli alan (Screen.safeArea) panel birimine: kök genişliği / ekran genişliği. Üst ve yanlar kökte, alt sekme
        // çubuğunda ve alt sayfada (zeminleri ev çizgisinin altına uzansın, düğmeler uzanmasın).
        // 2026-10-27: güvenli alan Guvenli.Alan'dan (ekran turu çentik taklidi); ekran boyu değişince ölçek eşlemesi ve boyut
        // sınıfı yenilenir, sınıf değiştiyse menü baştan kurulur (Duyarli.cs).
        float altBosluk; VisualElement govde; Vector3 guvPay;
        void GuvenliAlan()
        {
            if (DuyarliGuncelle()) HepsiniKur();
            float pw = doc.rootVisualElement.layout.width; if (float.IsNaN(pw) || pw <= 0 || Screen.width <= 0) return;
            Reklam.MacGuncelle(root.ekran != Ekran.Menu);   // menüdeyken maç şeridi payı sıfır (Hud çizmiyorsa da)
            var sa = Guvenli.Alan; if (sa == sonSafe && Mathf.Abs(pw - sonPanelW) < .5f) return; sonSafe = sa; sonPanelW = pw;
            float s = pw / Screen.width;
            // 2026-10-27 (duyarlı yerleşim): payı kökün dolgusu (padding) taşıyordu; oysa gövde, alt katman ve perde kalkanı kökün
            // MUTLAK konumlu çocukları (.tam, left/top/right/bottom 0) ve Yoga mutlak çocuğu ebeveynin dolgusuna değil kenarına
            // göre yerleştirir (CSS ile aynı): çentikli telefonda üst şerit çentiğin altına giriyordu (CI karesi güvenli alansız
            // olduğu için görünmüyordu). Pay artık bu katmanların kendi konumunda; zemin tam taşar (süs, kenara kadar).
            float pu = Mathf.Max(0, Screen.height - sa.yMax) * s, ps = Mathf.Max(0, sa.xMin) * s, pr = Mathf.Max(0, Screen.width - sa.xMax) * s;
            kok.style.paddingTop = 0f; kok.style.paddingLeft = 0f; kok.style.paddingRight = 0f;
            if (govde == null) govde = kok.Q<VisualElement>("govde");
            if (govde != null) { govde.style.top = pu; govde.style.left = ps; govde.style.right = pr; }
            altKatman.style.top = pu; altKatman.style.left = ps; altKatman.style.right = pr;
            guvPay = new Vector3(pu, ps, pr);
            altBosluk = Mathf.Max(0, sa.yMin) * s; CubukAltPay(); if (altSayfa != null) altSayfa.style.paddingBottom = altBosluk + 16;
            SutunYaz(pw);
        }

        // Menü reklam şeridi (Game/Reklam.cs): gövdenin sütununda içerik ile sekme çubuğu arasında; içerik onun üstünde biter,
        // kaydırılan liste şeridin altına girmez. ALAN false ise hiç yer kaplamaz.
        // 2026-11-08 (inceleme #1): yer tutucu ("Reklam" yazılı boş kutu) kalktı. #57'den beri SDK ilk maç bitene kadar başlamıyor,
        // yani yeni oyuncunun ilk menüsü hep boş gri kutu görüyordu (rıza reddi, çevrimdışı, dolmayan istek de aynı); App Review
        // 2.1 yer tutucu içeriği bitmemiş ürün sayar, muadiller reklam yoksa yer ayırmaz. Yer artık 0 yükseklikte; şerit ilk
        // yüklendiğinde açılır ve oturum boyunca açık kalır (sonraki yenilemede düşse de menü zıplamasın).
        VisualElement reklamEl; static bool seritAcildi;
        // 2026-11-09 (yasin: "reklam şeridi daha rahatsız etmeyecek benchmarked bir pozisyona alalım, küçük ekranlarda da rahatsız
        // etmesin"). Kıyas: arcade spor oyunları menü şeridini yalnız ana ekranda, ekranın dibinde sabit gösterir; eşya, harita ve
        // ayar ekranlarında şerit yok (oyuncu orada okur, kaydırır). AdMob uygulama kılavuzu da şeridin dokunulan düğmelere bitişik
        // durmamasını ister (yanlışlıkla tıklama politikası). Bu yüzden:
        //  - Şerit yalnız Oyna sekmesinin kendisinde (üstünde tam sayfa ya da alt sayfa yokken). Kariyer, Dolap, Profil, Atölye
        //    şeritsiz; o sekmelerde yer de kapanır ve içerik sekme çubuğuna kadar iner.
        //  - Şerit ile sekme çubuğu arasında ARA kadar boşluk (yanlışlıkla dokunma payı).
        //  - Oyna'da şerit afiş boşluğundan (esnek oyna-sahne) yer alır, düğmeleri itmez. Küçük ekranda (SE, büyük yazı) afiş
        //    boşluğu şerit girince AFIS_MIN'in altına düşecekse şerit o ekranda hiç çıkmaz: içerik kaymaz, kaydırma da gerekmez.
        //    Karar yer açıkken ve kapalıyken aynı ölçüyle verilir (açık: boşluk; kapalı: boşluk − bant), açılıp kapanma titremez.
        // Önceki (2026-11-08) "Dolap ve Atölye'de yok" kararının üstüne yazar.
        // 2026-11-11 (yasin, yerleşim sayfası K29XK1MmhZ292bwGg2ZDqF: "ana ekranda ince gösterelim", seçim B): bant ekranın DİBİNDEN
        // ÜSTÜNE taşındı: güvenli alanın (Dynamic Island) hemen altında, üst şeridin üstünde. Gövdenin ilk çocuğu; şerit bandın
        // üstüne oturur, ARA boşluğu artık üst şerit (Sv/Tüy hapı, Ayarlar) tarafında kalır: sekme düğmelerine bitişik değil.
        // Yükseklik değişmedi: AdMob telefonda 50 pt'den ince şerit vermez. Kalan kurallar (yalnız Oyna, AFIS_MIN, yüklenmezse
        // yer açılmaz, Reklamsız'da hiç yok) aynı.
        const float ARA = 8f, AFIS_MIN = 72f;
        static float Bant { get { return Reklam.PT + ARA; } }
        void ReklamKur(VisualElement r)
        {
            if (r == null) return; r.Clear(); r.pickingMode = PickingMode.Position; reklamEl = r;
            if (!Reklam.ALAN) { r.style.display = DisplayStyle.None; return; }
            r.style.display = DisplayStyle.Flex; r.style.height = seritAcildi ? Bant : 0f;
            Erisim.Etiket(r, L.T("Reklam alanı"), "metin");
        }
        // Oyna'da şerit için yer var mı (yukarıdaki AFIS_MIN kuralı). Sahne henüz yerleşmediyse (ilk kare) evet: ölçü gelince düzelir.
        bool OynadaYerVar()
        {
            var sahne = oynaSahne; if (sahne == null || sahne.panel == null || float.IsNaN(sahne.layout.height)) return true;
            bool acik = seritAcildi && reklamEl != null && reklamEl.resolvedStyle.display == DisplayStyle.Flex;
            return (acik ? sahne.layout.height : sahne.layout.height - Bant) >= AFIS_MIN;
        }
        // AdMob şeridi (Game/ReklamSdk, 2026-11-07): yerin ekrandaki konumu her kare verilir; şerit yalnız menü tam görünürken
        // çıkar: perde/solma yok, alt sayfa (sheet) kapalı. Şerit native bir görünüm, UI Toolkit'in üstünde durur: alt sayfa onu
        // örtemeyeceği için gizlenir. Yüklenince yer açılır (PT + ARA), yerleşim o boya oturana kadar şerit gizli tutulur
        // (içeriğin üstüne binmesin). Şerit bandın üstüne oturur, ARA boşluğu üst şerit tarafında kalır (bant 2026-11-11'den beri üstte).
        void ReklamSeritYeri(bool perde, float ortu)
        {
            ReklamSdk.Baslat();
            // Reklamsız alındıysa (oturum ortasında da): yer kapanır, şerit gizlenir; menü yeniden kurulunca ReklamKur hiç açmaz.
            if (!Reklam.ALAN) { if (reklamEl != null && reklamEl.style.display != DisplayStyle.None) reklamEl.style.display = DisplayStyle.None; ReklamSdk.Serit(null); return; }
            // AdMob telefonda 320×50'den ince şerit vermez (uyarlanabilir şerit de en az 50 pt), PT inmez (2026-11-08).
            bool yerde = gez.sekme == Sekme.Oyna && gez.Ust == null && OynadaYerVar();
            if (reklamEl != null) { var ist = yerde ? DisplayStyle.Flex : DisplayStyle.None; if (reklamEl.style.display != ist) reklamEl.style.display = ist; }
            if (!yerde) { ReklamSdk.Serit(null); return; }
            if (!ReklamSdk.Hazir) return;
            if (ReklamSdk.SeritYuklu && !seritAcildi && reklamEl != null) { seritAcildi = true; reklamEl.style.height = Bant; }
            Rect? yer = null;
            if (!perde && ortu <= .01f && gez.AcikAltSayfa == null && reklamEl != null && reklamEl.resolvedStyle.display == DisplayStyle.Flex)
            {
                float pw = doc.rootVisualElement.layout.width; var wb = reklamEl.worldBound;
                bool oturdu = wb.height >= Bant - .5f;
                if (seritAcildi && !oturdu) { ReklamSdk.Serit(null); return; }
                if (pw > 0 && Screen.width > 0 && wb.width > 0) { float s = pw / Screen.width, ust = wb.yMin; yer = new Rect(wb.x / s, ust / s, wb.width / s, Reklam.PT / s); }
            }
            ReklamSdk.Serit(yer);
        }

        // ---- gezinti ----
        public void SekmeAc(Sekme s) { var once = gez.sekme; bool sayfaYok = gez.Ust == null; bool d = gez.SekmeSec(s); if (d && !BasisCaldi()) { root.sfx.UiSes("sekme"); Yerel.Hap("tick"); } Uygula(once, d); if (!d && sayfaYok) BasaKaydir(); }
        // 2026-11-19 inceleme (fikir 07:06 #2, HIG sistem kalıbı): seçili sekmeye ikinci dokunuş, açık sayfa yoksa listeyi başa
        // kaydırır (iOS sekme çubuğu davranışı; Kariyer 8 tur, Dükkân uzun). Açık sayfa varken ilk dokunuş köke döner (eskisi gibi).
        // .25 sn ease-out, hareketi azaltta anında; yalnız scrollOffset değişir, sekme yeniden kurulmaz.
        void BasaKaydir()
        {
            var k = sekmeKok[(int)gez.sekme]; if (k == null) return; var sv = k.Q<ScrollView>(null, "govde-kaydir") ?? k.Q<ScrollView>();
            if (sv == null || sv.scrollOffset.y <= 0) return;
            if (Ayar.reduce) { sv.scrollOffset = Vector2.zero; return; }
            float y0 = sv.scrollOffset.y, t0 = Time.realtimeSinceStartup;
            sv.schedule.Execute(() => { float t = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / .25f); t = 1 - (1 - t) * (1 - t) * (1 - t); sv.scrollOffset = new Vector2(sv.scrollOffset.x, y0 * (1 - t)); Kalite.Hareket(); })
                .Every(16).Until(() => sv.scrollOffset.y <= 0);
        }
        public void AyarlarAc() { var once = gez.sekme; gez.Ac(Gezinti.AYAR); Uygula(once, true); }
        public void SeviyeYoluAc() { var once = gez.sekme; gez.Ac(Gezinti.SEVIYE); Uygula(once, true); }
        public void GelisimAc() { var once = gez.sekme; gez.Ac(Gezinti.GELISIM); Uygula(once, true); }
        public void HizliMacAc() { var once = gez.sekme; gez.Ac(Gezinti.HIZLI); Uygula(once, true); }
        // Kariyer haritası: düğüme dokununca maç kartı (alt sayfa); tur seçici (EkranTuru da kullanır).
        public void MacKartAc(int g) { var once = gez.sekme; kartBasamak = g; gez.Ac(Gezinti.KART); Uygula(once, true); }
        public void KariyerTurSec(int t) { kariyerTur = t; kirli[(int)Sekme.Kariyer] = true; if (gez.sekme == Sekme.Kariyer && gez.Ust == null) SekmeKur(Sekme.Kariyer); }
        void SayfaAc(string s) { var once = gez.sekme; gez.Ac(s); Uygula(once, true); }
        public void Geri() { var once = gez.sekme; if (gez.Geri()) { if (!BasisCaldi()) { root.sfx.UiSes("geri"); Yerel.Hap("tick"); } Uygula(once, true); } }
        void AltKapat() { var once = gez.sekme; gez.AltKapat(); Uygula(once, false); }
        // 2026-11-11 inceleme (fikir 20:05 #3, HIG standart gezinme): tam sayfada sol kenardan geri kaydırma 70 pt'de sayfayı bir
        // anda kaldırıyordu; parmak sayfayı taşımıyordu. Artık sayfa parmakla sağa kayar; altta dönülecek sekme (yığında tek sayfa
        // varsa) görünür. Bırakınca genişliğin %35'i ya da ~800 pt/sn hız geçildiyse .2 sn ease-out sağa çıkıp Geri, değilse
        // yerine yaylanır. Geri düğmesi ve ekran turu bilerek anında (alt sayfadaki gerekçe: zamanlanmış kapanış sıradakini kapatmasın).
        // Yığında iki sayfa varken (Ayarlar › Lisanslar) altta koyu zemin görünür; alttaki tam sayfayı önceden kurmak iki kez kurmak olurdu.
        float kenarDx, kenarHiz, kenarT; bool kenarSurukle, kenarKapaniyor;
        const float KENAR_ESIK = .35f, KENAR_HIZ = 800f, KENAR_SURE = .2f;
        void KenarSurukle(float x)
        {
            if (kenarX < 0 || kenarKapaniyor) return;
            float dx = Mathf.Max(0, x - kenarX), t = Time.realtimeSinceStartup, dt = t - kenarT;
            if (dt > 0) kenarHiz = Mathf.Lerp(kenarHiz, (dx - kenarDx) / dt, .5f); kenarT = t; kenarDx = dx;
            if (Ayar.reduce) { if (dx > 70) { kenarX = -1; Geri(); } return; }
            if (!kenarSurukle && dx > 8) { kenarSurukle = true; KenarAlt(true); }
            if (!kenarSurukle) return;
            sayfa.style.transitionDuration = new List<TimeValue> { new TimeValue(0f, TimeUnit.Second) };
            sayfa.style.translate = new Translate(dx, 0, 0); Kalite.Hareket();
        }
        void KenarBirak()
        {
            bool surukledi = kenarX >= 0 && kenarSurukle; kenarX = -1; kenarSurukle = false; if (!surukledi || kenarKapaniyor) return;
            float w = Mathf.Max(1, sayfa.resolvedStyle.width); bool kapat = kenarDx > w * KENAR_ESIK || kenarHiz > KENAR_HIZ;
            kenarKapaniyor = true; if (kapat) Yerel.Hap("tick");
            sayfa.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("translate") };
            sayfa.style.transitionDuration = new List<TimeValue> { new TimeValue(KENAR_SURE, TimeUnit.Second) };
            sayfa.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            sayfa.style.translate = new Translate(kapat ? w : 0, 0, 0); Kalite.Hareket();
            sayfa.schedule.Execute(() =>
            {
                kenarKapaniyor = false;
                if (kapat) { var once = gez.sekme; if (gez.Geri()) Uygula(once, false); }   // tık yukarıda; giriş animasyonu yok, sekme zaten görünüyordu
                else KenarAlt(false);
                sayfa.style.translate = StyleKeyword.Null; sayfa.style.transitionDuration = StyleKeyword.Null; sayfa.style.transitionProperty = StyleKeyword.Null; sayfa.style.transitionTimingFunction = StyleKeyword.Null;
            }).StartingIn(Mathf.RoundToInt(KENAR_SURE * 1000) + 16);
        }
        void KenarAlt(bool goster)
        {
            if (gez.Derinlik != 1) return;   // altta başka tam sayfa var: koyu zemin
            sekmeKok[(int)gez.sekme].style.display = goster ? DisplayStyle.Flex : DisplayStyle.None; ust.style.display = goster ? DisplayStyle.Flex : DisplayStyle.None;
        }
        // 2026-11-11 inceleme (fikir 19:05 #1): alt sayfa açılırken yaylanıyor ama kapanırken tek karede yok oluyordu. Kapat, zemine
        // dokunma ve tutamak artık önce sayfayı aşağı kaydırır (.2 sn ease-in), zemin aynı anda solar, sonra AltKapat. Hareketi
        // azaltta kayma yok, .12 sn solma. Geri (ekran turu, gezinti yığını) bilerek anında kalır: zamanlanmış kapanış sıradaki
        // sayfayı kapatmasın. Bekleyen kapanış aynı sayfa hâlâ açıksa çalışır.
        bool altKapaniyor; Color altZemin = new Color(0, 0, 0, -1);
        void AltKapatKay()
        {
            if (!altKapaniyor && Time.frameCount != tikKare && !BasisCaldi()) { tikKare = Time.frameCount; root.sfx.UiSes("geri"); }   // 2026-11-22: alt sayfa kapanışı geri sesiyle; aynı karede düğmenin pop'u çaldıysa ikinci ses yok
            var v = altSayfa; if (v == null) { AltKapat(); return; }
            if (altKapaniyor) return; altKapaniyor = true; Yerel.Hap("tick");
            float sure = Ayar.reduce ? 0.12f : 0.2f;
            v.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("translate"), new StylePropertyName("opacity") };
            v.style.transitionDuration = new List<TimeValue> { new TimeValue(sure, TimeUnit.Second) };
            v.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseIn) };
            if (Ayar.reduce) v.style.opacity = 0f; else v.style.translate = new Translate(0, Mathf.Max(200f, v.resolvedStyle.height + 40f), 0);
            AltZeminOran(0f, sure);
            v.schedule.Execute(() => { altKapaniyor = false; if (altSayfa == v) AltKapat(); AltZeminSifirla(); }).StartingIn(Mathf.RoundToInt(sure * 1000) + 16);
        }
        // Sürükleme: sayfa parmakla iner (geçiş yok); bırakınca geçiş USS'e döner, inline translate kalkar, sayfa yerine yaylanır.
        void AltSurukle(VisualElement v, float dy, bool basili)
        {
            if (v == null || altKapaniyor) return;
            if (basili) { v.style.transitionDuration = new List<TimeValue> { new TimeValue(0f, TimeUnit.Second) }; v.style.translate = new Translate(0, dy, 0); }
            else { v.style.transitionDuration = StyleKeyword.Null; v.style.translate = StyleKeyword.Null; }
            float h = Mathf.Max(1, v.resolvedStyle.height); AltZeminOran(basili ? 1f - Mathf.Clamp01(dy / h) : 1f, basili ? 0f : 0.22f);
            Kalite.Hareket();
        }
        void AltZeminOran(float k, float sure)
        {
            if (altZemin.a < 0) altZemin = altKatman.resolvedStyle.backgroundColor;   // USS rgba(10,6,14,.6); renk tek yerde kalsın
            altKatman.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("background-color") };
            altKatman.style.transitionDuration = new List<TimeValue> { new TimeValue(sure, TimeUnit.Second) };
            altKatman.style.backgroundColor = new Color(altZemin.r, altZemin.g, altZemin.b, altZemin.a * k);
        }
        void AltZeminSifirla() { altKatman.style.backgroundColor = StyleKeyword.Null; altKatman.style.transitionDuration = StyleKeyword.Null; altKatman.style.transitionProperty = StyleKeyword.Null; }
        // Maçtan ana menüye dönüş (GameRoot.AnaMenu): açık sayfalar kapanır, Oyna sekmesi (kariyer maçı sonundan Kariyer, 2026-11-14).
        // Kirlet de Uygula'yı çağırır: Kariyer'de ilk Uygula atlanır, yoksa sekme iki kez kurulur ve basamak anı (bir kez oynar,
        // BasamakAniHazirla değeri hemen yazar) ilk, hemen sökülen kurulumda tükenir. Oyna yolu bugünkü gibi.
        public void MenuyeDon() { MenuyeDon(Sekme.Oyna); }
        public void MenuyeDon(Sekme hedef) { MagazaOnIsit(); var once = gez.sekme; kariyerTur = -1; gez.SekmeSec(hedef); if (hedef == Sekme.Oyna) Uygula(once, false); Kirlet(); if (OyuncuSecGerekli()) OyuncuSecAc(); }   // harita yeniden sıradakinin turunu açar (yeni tur açılmış olabilir)

        void Uygula(Sekme once, bool anim)
        {
            if (kok == null || sekmeKok[0] == null) return;   // iskelet kurulmadan çağrıldı (gezinti durumu yine de güncel)
            var s = gez.sekme; string ustS = gez.Ust; bool tamSayfa = ustS != null && !Gezinti.AltSayfa(ustS);
            // Yığındaki son tam sayfa (alt sayfa açıkken altında duran)
            string tam = null; if (tamSayfa) tam = ustS; else if (gez.Derinlik > 1) tam = gez.Acik(Gezinti.LISANS) ? Gezinti.LISANS : gez.Acik(Gezinti.NASIL) ? Gezinti.NASIL : gez.Acik(Gezinti.AYAR) ? Gezinti.AYAR : gez.Acik(Gezinti.ANTRENMAN) ? Gezinti.ANTRENMAN : null;
            for (int i = 0; i < SEKME_N; i++)
            {
                bool on = (int)s == i; sekmeKok[i].style.display = on && tam == null ? DisplayStyle.Flex : DisplayStyle.None;
                if (sekmeDugme[i] != null)
                {
                    sekmeDugme[i].EnableInClassList("sekme--secili", on); var b = sekmeDugme[i].userData as ErisimBilgi; if (b != null) b.secili = on;
                    var im = sekmeDugme[i].Q<Image>(null, "sekme-ikon"); if (im != null) im.tintColor = on ? CTA_MUREKKEP : SEKME_SOLUK;   // 2026-11-22 Stadyum Pop: beyaz çubuk, seçili sarı hap üstünde mürekkep   // seçili: limon dolgu üstünde koyu simge; 2026-10-27 yön C: dolgu yok, turuncu simge
                }
            }
            GostergeYerlestir(anim && s != once);   // 2026-10-27 yön C: üst çizgi yeni sekmeye yayla kayar
            IsaretGuncelle();
            ust.style.display = tam == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (tam != null) { SayfaKur(tam); sayfa.style.display = DisplayStyle.Flex; if (anim) Giris(sayfa); } else { sayfa.style.display = DisplayStyle.None; sayfa.Clear(); }
            string altS = gez.AcikAltSayfa;
            if (altS != null) { AltSayfaYenidenKur(altS); altKatman.style.display = DisplayStyle.Flex; if (anim) Giris(altSayfa); } else { altKatman.style.display = DisplayStyle.None; altKatman.Clear(); altSayfa = null; }
            // Alt sayfa sekme çubuğunu örter (iOS sayfa kalıbı; 2026-10-30 dil turu: maç kartı, Hızlı maç, görev ve günün maçı
            // sayfalarının alt düğmesi sekmelerin üstüne biniyordu, sekmeler de dokunuşu alıyordu: "dokunma alanı çakışıyor").
            sekmeCubugu.style.visibility = altS != null ? Visibility.Hidden : Visibility.Visible;
            bool koyu = s != Sekme.Oyna || tam != null; kok.EnableInClassList("koyu", koyu);
            // 2026-10-26 gece: kök her sekmede opak (Oyna: mürekkep, salon fonu üstünde; diğerleri yön A a-bgnight #0B1130), tam
            // sayfa mürekkep. Satır içi (USS'te değil): r24'te USS'teki değişiklikle bütün sayfa biçimsiz çıktı, kök neden
            // ayrılana kadar zemin koddan (eski USS değerleri saydamdı; satır içi değer onları ezer).
            // 2026-10-27 yön C: GECE ve INK_ ikisi de C zemini #1A1221 (aşağıda); iOS açılışı ve perde zemini de aynı renk.
            kok.style.backgroundColor = koyu ? GECE : INK_; sayfa.style.backgroundColor = INK_;
            // Vitrin kamerası yalnız Soyunma odası görünürken çizer (bütçe §7.3); çıkınca sahnedeki oyuncu yeni görünüme geçer.
            bool vitrinGerek = s == Sekme.Soyunma && tam == null && root.ekran == Ekran.Menu;
            if (!vitrinGerek && root.vitrin.gameObject.activeSelf) { root.vitrin.Kapat(); if (root.ekran == Ekran.Menu) root.GorunumDegisti(); }
            else if (vitrinGerek && !root.vitrin.gameObject.activeSelf) { root.vitrin.Ac(); kirli[(int)Sekme.Soyunma] = true; }   // Kapat raket görünümünü sıfırladı: gövde ona göre
            // 2026-10-26 Yayın Gecesi: Oyna'nın VS afişi yalnız Oyna görünürken çizer (Vitrin ile aynı kural).
            AfisGuncelle(s == Sekme.Oyna && tam == null && root.ekran == Ekran.Menu);
            if (tam == null && kirli[(int)s]) SekmeKur(s);
            if (tam == null && s != once && anim) Giris(sekmeKok[(int)s], System.Array.IndexOf(SEKME_SIRA, s) < System.Array.IndexOf(SEKME_SIRA, once));
            // portre: Dolap'ta görünüm değişince üst çubuğun avatarı da yenilensin (sekme değişiminde yoklanır)
            string pa = Ayar.model + PlayerView.Benim().Anahtar(); if (pa != portreAnah) { portreAnah = pa; ustKirli = true; kirli[(int)Sekme.Profil] = true; }
            if (ustKirli) UstKur();
        }
        string portreAnah;
        // 2026-11-21 inceleme (fikir 22:06 #2): Dolap'tan çıkış karesinde üst çubuk, Oyna kartı, Kariyer ve Profil portreyi
        // (Portre.Benim) önbellekte bulamayınca FBX figürünü aynı karede çiziyordu; vitrin kapanışı ve sekme girişiyle üst üste.
        // Dolap açıkken görünüm her değiştiğinde portre menü durgunken (son dokunuştan sonra) bir kez çizilir; çıkışta hepsi
        // önbellekten gelir. Vitrin kapalıyken yalnız bir bool okunur; anahtar dizesi yalnız Dolap'ta, 300 ms'de bir.
        string portreIsitilen;
        // 2026-11-26 inceleme (fikir 07:06 #1): üst hap onarımı (02:06 #1) altı yere genellendi. Kariyer sıradaki maç "Sen" yüzü,
        // "buradasın" düğümü, Profil kartı, Gelişim başlığı, Antrenman şeridi ve Arkadaş ekranı Portre.Benim()'i kurulum karesinde
        // eşzamanlı çiziyor ve RT sonradan atılınca boş daire kalıyordu. BenAvatar önbellekten okur (yoksa insan simgesi) ve elemanı
        // bu listeye koyar; PortreIsit panelden düşenleri atar, dokusu ölü/eksik olanları menü durgunken tek çizimle yerinde onarır.
        sealed class BenAv { public VisualElement av; public RenderTexture t; public int px, yas; public bool goruldu; }
        readonly List<BenAv> benAvlar = new List<BenAv>(8);
        int benPortreDeneme;
        void BenAvatar(VisualElement av, int px)
        {
            var t = Portre.BenimHazir() as RenderTexture; AvatarIc(av, t, px);
            benAvlar.Add(new BenAv { av = av, t = t, px = px }); benPortreDeneme = 0;
        }
        void PortreIsit()
        {
            // Canlı dokuda eleman başına iki null/IsCreated okuması (ayırma yok). Çizim başarısızsa en çok 3 deneme; aynı tikte tek ağır iş.
            bool eksik = false;
            for (int i = benAvlar.Count - 1; i >= 0; i--)
            {
                var b = benAvlar[i];
                // panele geç eklenen eleman (kurulum ile ekleme arasına tik düşerse) hemen atılmasın: önce panelde görülmüş olmalı ya da ~3 sn
                if (b.av.panel == null) { if (b.goruldu || ++b.yas > 10) benAvlar.RemoveAt(i); continue; }
                b.goruldu = true;
                if (b.t == null || !b.t.IsCreated()) eksik = true;
            }
            if (eksik && benPortreDeneme < 3 && Kalite.MenuDurgun)
            {
                benPortreDeneme++; var t = Portre.Benim() as RenderTexture;
                if (t != null)
                {
                    benPortreDeneme = 0;
                    for (int i = 0; i < benAvlar.Count; i++) { var b = benAvlar[i]; if (b.t != null && b.t.IsCreated()) continue; b.t = t; b.av.Clear(); b.av.RemoveFromClassList("avatar--portre"); AvatarIc(b.av, t, b.px); }
                }
                return;
            }
            // Atölye vitrin raketi (fikir 02:06 #8): 256² × 4 örnek ilk Atölye dokunuşunun karesinde üretiliyordu; menü durgunken bir kez.
            // 2026-11-26: doku raket görünüşüne bağlı; Dolap'ta görünüş değişince (aşağıda portreIsitilen) eskir, durgunken yenilenir.
            if (vitrinRaketEski && Kalite.MenuDurgun) { VitrinRaket(); return; }
            // 2026-11-26 inceleme (fikir 22:06 #1, takılma): maçtan dönüşte Kirlet bütün sekmeleri kirletiyor, ilk Atölye dokunuşu tam
            // kurulum + giriş animasyonunu aynı karede yapıyordu. Menü durgunken Atölye arka planda bir kez kurulur; oyuncu ondan önce
            // dokunursa bugünkü yol. Yalnız Atölye: Kariyer kurulumu bir kez oynayan basamak/rakip girişini, Profil ilk kez görülen
            // kupanın inişini tüketir (gizliyken oynamış olur); Dolap vitrin açılınca zaten yeniden kurulur. Vitrin açıkken (Dolap'ın
            // deneme raketi) ve seviye anı/tören sürerken kurulmaz.
            if (kirli[(int)Sekme.Atolye] && gez.sekme != Sekme.Atolye && seviyeAni == null && root != null && root.ekran == Ekran.Menu && root.vitrin != null && !root.vitrin.gameObject.activeSelf && Kalite.MenuDurgun) { SekmeKur(Sekme.Atolye); return; }
            // 2026-11-27 inceleme (fikir 07:06 #8, takılma): maç başında bırakılan vitrin RT'si ilk Dolap dokunuşunda figür çizimi ve
            // Dolap kurulumuyla aynı karede yeniden ayrılıyordu. Menü durgunken yalnız ayırma (çizim yok); tikte tek ağır iş.
            if (root != null && root.ekran == Ekran.Menu && root.vitrin != null && !root.vitrin.gameObject.activeSelf && root.vitrin.Bosaltildi && Kalite.MenuDurgun) { root.vitrin.RtIsit(); return; }
            if (root == null || root.vitrin == null || !root.vitrin.gameObject.activeSelf || !Kalite.MenuDurgun) return;
            string pa = Ayar.model + PlayerView.Benim().Anahtar(); if (pa == portreIsitilen) return;
            portreIsitilen = pa; vitrinRaketEski = true; Portre.Benim();
        }

        // ---- Ödül anları (2026-10-31, sanat yönü ilk paketi; yasin "yap") ----
        // Tüy ve seviye eskiden sessizce bir sayı olarak değişiyordu. Menüye dönüşte (üst çubuk yeniden kurulurken) Tüy arttıysa
        // ortadan sayaca altı tüy uçar ve sayı tıklayarak artar; seviye arttıysa bir kez tam ekran "Seviye N" anı açılanları
        // gösterir. İlk kurulumda (önceki değer yok) ve hareketi azaltta uçuş/sayma yok, sayı doğrudan yazılır; seviye anı
        // hareketi azaltta da açılır (bilgi taşır), yalnız büyüme ve sırayla belirme kapalıdır.
        static int gosterilenTuy = -1, gosterilenSv = -1;
        // Ekran turu örnek ilerlemeler kurup bozar: tüy/seviye anı orada kendiliğinden açılırsa sonraki kareleri (dil değişse de)
        // örtüyordu (r93). Tur bunu kapatır, anı yalnız SeviyeAniTur ile bir kez çeker.
        public static bool OdulAnlariKapali;
        public void SeviyeAniTur(int once, int sv) { SeviyeAni(once, sv); }
        // bekTz burada temizlenmez: TurToren açılırken bunu çağırır, seviye kartı törenin "Tamam"ından sonra gelir ve ödülü o uçurur.
        // Kirlet yolunda şerit yeniden kurulur, OdulAnlari bekleyeni sıfırlayıp doğru toplamı yazar.
        public void SeviyeAniKapat() { if (seviyeAni != null) { seviyeAni.RemoveFromHierarchy(); seviyeAni = null; } }
        VisualElement bekTuy; Label bekTz; int bekOnce, bekHedef;
        void BekleyenTuyUcur(VisualElement kaynak)
        {
            var tz = bekTz; bekTz = null; if (tz == null || tz.panel == null) return;
            if (root.ekran != Ekran.Menu) { tz.text = Sayi(bekHedef); return; }
            TuyUcur(bekTuy, tz, bekOnce, bekHedef, kaynak);
        }
        void OdulAnlari(VisualElement tuy, Label tz, int hedef, int sv)
        {
            int once = gosterilenTuy, svOnce = gosterilenSv; gosterilenTuy = hedef; gosterilenSv = sv;
            bekTz = null;
            if (root.ekran != Ekran.Menu || OdulAnlariKapali) return;
            // 2026-11-26 inceleme (fikir 22:06 #4): seviye atlanan dönüşte bütün Tüy (maç + seviye ödülü) kart açılmadan şeride
            // uçuyordu, ardından gelen kart aynı ödülü yeniden "veriyor" gibiydi. Artık ilk uçuş yalnız maç Tüy'ünü sayar; seviye
            // ödülü kart kapanırken karttaki ödül satırından şeride uçar (SeviyeAni kapat → BekleyenTuyUcur; muadil: ödül kartı
            // kapanırken para sayaca). Kart hiç açılmazsa (maça girildi, Kirlet) şerit yeniden kurulunca doğru toplamı yazar.
            int svTuy = once >= 0 && svOnce > 0 && sv > svOnce && !Ayar.reduce ? Gear.SvOdulAralik(svOnce, sv) : 0;
            int ara = hedef - svTuy; if (svTuy > 0 && ara < once) { svTuy = 0; ara = hedef; }
            if (svTuy > 0) { tz.text = Sayi(ara); bekTuy = tuy; bekTz = tz; bekOnce = ara; bekHedef = hedef; }
            if (once >= 0 && ara > once && !Ayar.reduce)
            {
                tz.text = Sayi(once);
                tz.schedule.Execute(() => TuyUcur(tuy, tz, once, ara)).StartingIn(250);   // yerleşim bitsin (hedefin yeri)
            }
            if (once >= 0 && hedef < once && !Ayar.reduce) { azalOnce = once; azalHedef = hedef; azalBas = Time.unscaledTime; }
            if (azalHedef == hedef && Time.unscaledTime - azalBas < AZAL_SN && !Ayar.reduce) TuyAzal(tuy, tz);
            // D7 (2026-10-31): gecikmeli seviye anı, oyuncu 1,1 sn içinde maça girerse maçın üstünde titreşim/ses çalıyordu;
            // çalıştığı anda hâlâ menüde mi diye bakılır.
            int gec = once >= 0 && hedef > once && !Ayar.reduce ? 1100 : 200;
            // Tur töreni önce (turun zirvesi), seviye anı onun "Tamam"ından sonra; ikisi üst üste açılmaz.
            if (torenTur >= 0)
            {
                int tt = torenTur, m0 = torenMuhur0, m1 = torenMuhur1; bool tb = torenBitti; torenTur = -1;
                System.Action sonra = svOnce > 0 && sv > svOnce ? (System.Action)(() => SeviyeAni(svOnce, sv)) : null;
                kok.schedule.Execute(() => { if (root.ekran == Ekran.Menu && !OdulAnlariKapali) TurToren(tt, tb, m0, m1, sonra); }).StartingIn(gec);
                return;
            }
            if (svOnce > 0 && sv > svOnce) kok.schedule.Execute(() => { if (root.ekran == Ekran.Menu && !OdulAnlariKapali) SeviyeAni(svOnce, sv); }).StartingIn(gec);
        }
        // kaynak (2026-11-12 inceleme, dükkân satın alma anı): verilirse tüyler ekran ortasından değil o öğenin (satın alınan
        // paket kartı) ortasından kalkar; hedef aynı dil, aynı süre.
        void TuyUcur(VisualElement tuy, Label tz, int once, int hedef, VisualElement kaynak = null)
        {
            var kb = kok.worldBound; var hb = tuy.worldBound; if (float.IsNaN(hb.x) || kb.width < 1) { tz.text = Sayi(hedef); return; }
            Kalite.Hareket();   // menü durgun çizimi (Kalite, 2026-11-14) uçuş boyunca tam hızda kalsın: uçuş < 1 sn, eşik 3 sn
            Vector2 bas = new Vector2(kb.width * .5f, kb.height * .45f), son = new Vector2(hb.x - kb.x + 18, hb.y - kb.y + hb.height * .5f);
            if (kaynak != null) { var sb = kaynak.worldBound; if (!float.IsNaN(sb.x) && sb.width > 1) bas = new Vector2(sb.center.x - kb.x, sb.center.y - kb.y); }
            for (int i = 0; i < 6; i++)
            {
                var f = Ikon(Ikonlar.Tuy, 26, ALTIN); f.pickingMode = PickingMode.Ignore; f.style.position = Position.Absolute; kok.Add(f);
                var sap = new Vector2((i - 2.5f) * 18, (i % 2 == 0 ? -1 : 1) * 14); int gec = i * 60;
                f.style.left = bas.x + sap.x; f.style.top = bas.y + sap.y; f.style.opacity = 0f;
                var fi = f;
                fi.experimental.animation.Start(0f, 1f, 520 + gec, (v, t) =>
                {
                    float k = Mathf.Clamp01((t * (520 + gec) - gec) / 520f); float e = k * k * (3 - 2 * k);   // yumuşak başla, yumuşak dur
                    var p = Vector2.Lerp(bas + sap, son, e) + new Vector2(0, -60 * Mathf.Sin(e * Mathf.PI));       // hafif yay
                    v.style.left = p.x - 13; v.style.top = p.y - 13; v.style.opacity = k <= 0 ? 0 : k < .85f ? 1 : (1 - k) / .15f;
                    v.style.scale = new Scale(Vector3.one * Mathf.Lerp(1.15f, .7f, e));
                }).OnCompleted(() => fi.RemoveFromHierarchy());
            }
            tz.experimental.animation.Start(0f, 1f, 820, (v, t) =>
            {
                float k = Mathf.Clamp01((t - .35f) / .65f); tz.text = Sayi(Mathf.RoundToInt(Mathf.Lerp(once, hedef, k * (2 - k)))); Kalite.Canli();
            }).OnCompleted(() => { tz.text = Sayi(hedef); root.sfx.Tik(1320); Yerel.Hap("tick"); });
            tuy.experimental.animation.Start(0f, 1f, 820, (v, t) => { float k = t < .6f ? 0 : (t - .6f) / .4f; v.style.scale = new Scale(Vector3.one * (1 + .12f * Mathf.Sin(k * Mathf.PI))); })
                .OnCompleted(() => tuy.style.scale = StyleKeyword.Null);
        }
        // Harcama anı (2026-11-11 inceleme, fikir 22:05 #5): artış tüy uçurup sayarak geliyordu, harcamada (Dolap/Atölye alımı,
        // Gear.Yukselt) üst şerit yeniden kurulup sayı bir anda düşüyordu; oyuncu ne harcadığını göz ucuyla göremiyordu. Sayı
        // 0,35 sn'de aşağı sayar (artıştan hızlı), hap bir kez hafifçe içe basılır. Tık sesi yok (alım sesi zaten çalıyor);
        // kırmızı ya da "−120" uçan yazı yok, harcama ceza gibi okunmasın. Hareketi azaltta sayı doğrudan yazılır (çağıran bakar).
        // Alımdan sonra üst şerit aynı karede iki kez kurulabiliyor (KozAl: UstKur + Degisti); ikinci kurulum yeni etiketi
        // yarıda kalan saymayı sürdürsün diye başlangıç anı statik tutulur, sayma kalan süreden devam eder.
        static int azalOnce, azalHedef = -1; static float azalBas = -9; const float AZAL_SN = .35f;
        void TuyAzal(VisualElement tuy, Label tz)
        {
            float g = Mathf.Clamp01((Time.unscaledTime - azalBas) / AZAL_SN); int ms = Mathf.Max(1, Mathf.RoundToInt((1 - g) * AZAL_SN * 1000)); int o = azalOnce, h = azalHedef;
            tz.text = Sayi(Mathf.RoundToInt(Mathf.Lerp(o, h, g * (2 - g))));
            tz.experimental.animation.Start(0f, 1f, ms, (v, t) => { float k = Mathf.Lerp(g, 1, t); tz.text = Sayi(Mathf.RoundToInt(Mathf.Lerp(o, h, k * (2 - k)))); Kalite.Canli(); })
                .OnCompleted(() => tz.text = Sayi(h));
            tuy.experimental.animation.Start(0f, 1f, ms, (v, t) => { float k = Mathf.Lerp(g, 1, t); v.style.scale = new Scale(Vector3.one * (1 - .04f * Mathf.Sin(k * Mathf.PI))); })
                .OnCompleted(() => tuy.style.scale = StyleKeyword.Null);
        }
        VisualElement seviyeAni;
        // Seviye anı ve tur töreni iskeleti (2026-11-10 inceleme, fikir 02:05 #1): r147 büyük yazıda seviye anının 11 satırı ve
        // "Tamam" düğmesi ezilmişti (sütun ekrandan uzun, satırlar küçülüyordu). İçerik kayan sütunda, düğmeler altta sabit.
        // Kapatma artık yalnız zemine dokununca (önce içeriğe dokunmak da kapatıyordu; kaydırma sürüklemesi kapatmasın diye).
        // 2026-11-23 inceleme (fikir 12:06 #2): ödül anları experimental.animation ile oynuyor, Kalite.Hareket/Canli çağırmıyordu;
        // ProMotion'da 60 Hz'de, dokunuşsuz 3 sn sonra açılınca 30 fps çizimde kalıyordu. Animasyon süresince her güncellemede Canli
        // (bir float yazımı, ayırma yok); 120 Hz koşulları (ısı, düşük güç, basamak) Kalite'de aynen geçerli.
        static void CanliTut(VisualElement v, long ms) { if (!Ayar.reduce) v.schedule.Execute(Kalite.Canli).Every(0).ForDuration(ms); }
        // 2026-11-23 inceleme (fikir 12:06 #3): kart açılışta 220 ms solarak geliyor, kapanışta bir karede yok oluyordu; tören →
        // seviye anı zincirinde sert kesme. Kapanış açılışın tersi: 160 ms solma + 0,98 ölçek (iOS sunum/kapanış 200–240 ms
        // kılavuzunun kısa ucu, çıkış girişten hızlı). Ardından gelen iş (sonraki kart, sekme) solmayı beklemez, kart üstte söner;
        // solarken üstte şeffaf bir kalkan çift dokunuşu yutar. Hareketi azaltta anında kaldırılır.
        void AniSondur(VisualElement o)
        {
            if (Ayar.reduce || o.panel == null) { o.RemoveFromHierarchy(); return; }
            var kalkan = new VisualElement(); kalkan.style.position = Position.Absolute; kalkan.style.left = 0; kalkan.style.top = 0; kalkan.style.right = 0; kalkan.style.bottom = 0; o.Add(kalkan);
            float o0 = o.resolvedStyle.opacity;
            o.experimental.animation.Start(0f, 1f, 160, (v, t) =>
            {
                float e = t * t; v.style.opacity = o0 * (1 - e); if (v.childCount > 0) v[0].style.scale = new Scale(Vector3.one * (1 - .02f * e)); Kalite.Canli();
            }).OnCompleted(() => o.RemoveFromHierarchy());
        }
        VisualElement AniIskelet(VisualElement o, System.Action kapat, out VisualElement eylem)
        {
            o.RegisterCallback<ClickEvent>(e => { if (e.target == o) kapat(); });
            var sut = new VisualElement(); sut.AddToClassList("seviye-sutun"); o.Add(sut);
            var kay = Kaydirma(sut); kay.AddToClassList("seviye-kaydir");
            var ic = new VisualElement(); ic.AddToClassList("seviye-ic"); kay.Add(ic);
            eylem = new VisualElement(); eylem.AddToClassList("seviye-eylem"); sut.Add(eylem);
            return ic;
        }
        void SeviyeDene(string katId)
        {
            if (string.IsNullOrEmpty(katId)) return; int ay = katId.IndexOf('|'); if (ay <= 0) return;
            string kat = katId.Substring(0, ay), id = katId.Substring(ay + 1);
            int sf = kat == Kozmetik.TIP ? KAR_TIP : kat == Kozmetik.SAC ? KAR_SAC : KAR_AYK;
            if (System.Array.IndexOf(KarSayfalar(), sf) < 0) return;
            KarakterAc(sf); KarakterDene(kat, id);
        }
        void SeviyeAni(int once, int sv)
        {
            if (seviyeAni != null) seviyeAni.RemoveFromHierarchy();
            var o = new VisualElement(); o.AddToClassList("seviye-ani"); seviyeAni = o; kok.Add(o);
            bool kapandi = false;
            VisualElement odulSatir = null;
            System.Action kapat = () => { if (kapandi) return; kapandi = true; if (seviyeAni == o) seviyeAni = null; BekleyenTuyUcur(odulSatir); AniSondur(o); };
            VisualElement ey; var ic = AniIskelet(o, kapat, out ey);
            ic.Add(Yazi(Buyuk(L.T("Seviye atladın")), 15, "seviye-ust"));
            var sayi = Yazi(L.T("Sv {0}", sv), 64, "rakam"); sayi.AddToClassList("seviye-sayi"); ic.Add(sayi);
            // 2026-11-10 seviye ödülü (Gear.SvOdul): sayının hemen altında, tüy simgesiyle; açılanlar listesinden önce (eline geçen şey).
            int svTuy = Gear.SvOdulAralik(once, sv);
            if (svTuy > 0) { var or_ = new VisualElement(); or_.AddToClassList("seviye-odul"); or_.Add(Ikon(Ikonlar.Tuy, 22, ALTIN)); or_.Add(Yazi(L.T("+{0} Tüy", Sayi(svTuy)), 22, "rakam")); or_.Add(Yazi(L.T("Seviye ödülü"), 14, "soluk")); ic.Add(or_); odulSatir = or_; }
            // 2026-11-10 oyuncu gelişimi: her seviye bir puan; an yalnız söyler, dağıtım Gelişim sayfasında (Oyna'daki satır götürür).
            Label gy = null;
            if (sv > once && Ayar.GelisimBos > 0) { gy = Yazi(L.T("+{0} gelişim puanı", System.Math.Min(sv - once, Ayar.GelisimBos)), 15, "kalin"); gy.AddToClassList("seviye-gelisim"); ic.Add(gy); }
            var acilan = Karakter.AcilanAralik(once, sv); var liste = new VisualElement(); liste.AddToClassList("seviye-liste"); ic.Add(liste);
            // 2026-11-07 stüdyo incelemesi (UX1, GD4): açılanlar nerede bulunacaklarıyla gruplu ("Dolapta", "Atölyede", "Ayrıca"),
            // grup başına en çok 3 satır; tek turuncu düğme ilk grubun yerine götürür (raket parçasıysa Atölye'de o yuva açık).
            // Önce ham ad listesi + iki eşit düğme vardı, "Dolap'a bak" raket parçası için yanlış sekmeyi açıyordu. "Tamam" hayalet.
            var gr = Karakter.AcilanAralikGruplu(once, sv); string[] GRUP = { "Dolap", "Atolye", "Diger" }, BAS = { "Dolapta", "Atölyede", "Ayrıca" };
            string ilkGrup = null, ilkYuva = null, ilkDene = null;
            for (int g = 0; g < 3; g++)
            {
                var ol = gr.FindAll(x => x[0] == GRUP[g]); if (ol.Count == 0) continue;
                if (g == 0 && ilkDene == null) { var dn = ol.Find(x => x.Length > 3 && x[3] != null); if (dn != null) ilkDene = dn[3]; }
                if (ilkGrup == null && g < 2) { ilkGrup = GRUP[g]; ilkYuva = ol[0][2]; }
                var b = Yazi(L.T(BAS[g]), 13, "soluk"); b.AddToClassList("seviye-grup"); liste.Add(b);
                for (int i = 0; i < ol.Count && i < 3; i++) { var y = Yazi(L.T(ol[i][1]), 17); y.AddToClassList("seviye-oge"); liste.Add(y); }
                if (ol.Count > 3) liste.Add(Yazi(L.T("ve {0} öğe daha", ol.Count - 3), 14, "soluk"));
            }
            // "Dolap'ta dene" yalnız sekmeyi açıyordu, oyuncu açılan eşyayı listede kendisi arıyordu (inceleme 10 Kas 19:49 #5): artık ilk
            // denenebilir eşyanın sayfası açılır ve vitrinde giydirilir (kayda yazılmaz; "Denemeyi bırak" zaten var). Sayfa bu modelde
            // yoksa (tip/saç yalnız varyant görünürken) eski davranış: yalnız sekme.
            if (ilkGrup == "Dolap") { string dn = ilkDene; ey.Add(Dugme(L.T("Dolap'ta dene"), () => { kapat(); SekmeAc(Sekme.Soyunma); SeviyeDene(dn); }, "dugme--birincil")); }
            else if (ilkGrup == "Atolye") { string yv = ilkYuva; ey.Add(Dugme(L.T("Atölye'de gör"), () => { kapat(); if (yv != null) YuvaAc(yv); else SekmeAc(Sekme.Atolye); }, "dugme--birincil")); }
            // 2026-11-10 (Ar-Ge 19:49 #5): seviye puan verdi ama dağıtmak için Oyna'ya dönüp satırı bulmak gerekiyordu. Açılan eşya
            // yokken birincil düğme budur; varken ikinci sırada durur (üç düğme: eşya, puan, tamam).
            bool puanVar = Ayar.GelisimBos > 0;
            // 2026-11-23 inceleme (fikir 12:06 #5): eşya + puan varken eylem sütununda üç düğme alt alta duruyordu (SE'de 3×52 pt,
            // iki eşit hayalet). Puan satırı zaten içerikte: o satır 44 pt dokunulabilir olur ("Dağıt ›"), sütunda en çok iki düğme.
            // Eşya yokken "Puanı dağıt" birincil kalır (tek iş o).
            if (puanVar && ilkGrup != null && gy != null)
            {
                gy.text += " · " + L.T("Dağıt") + " ›"; gy.AddToClassList("seviye-gelisim--dokun");
                gy.AddManipulator(new Clickable(() => { Tik(gy); kapat(); GelisimAc(); }));
                Erisim.Etiket(gy, L.T("+{0} gelişim puanı", System.Math.Min(sv - once, Ayar.GelisimBos)) + ", " + L.T("Puanı dağıt"), "dugme");
            }
            else if (puanVar) ey.Add(Dugme(L.T("Puanı dağıt"), () => { kapat(); GelisimAc(); }, ilkGrup != null ? "dugme--hayalet" : "dugme--birincil"));
            ey.Add(Dugme(L.T("Tamam"), kapat, ilkGrup != null || puanVar ? "dugme--hayalet" : "dugme--birincil"));
            Erisim.Etiket(ic, L.T("Seviye atladın") + ", " + L.T("Sv {0}", sv) + (svTuy > 0 ? ", " + L.T("Seviye ödülü +{0} Tüy", svTuy) : "") + (acilan.Count > 0 ? ", " + L.T("Açıldı") + ": " + string.Join(", ", acilan.ConvertAll(x => L.T(x))) : ""), "metin");
            root.sfx.UiSes("odul"); Yerel.Hap("win");
            if (Ayar.reduce) return;
            o.style.opacity = 0f; o.experimental.animation.Start(0f, 1f, 220, (v, t) => v.style.opacity = t);
            sayi.style.scale = new Scale(Vector3.one * 1.4f);
            sayi.experimental.animation.Start(0f, 1f, 420, (v, t) => { float e = 1 - Mathf.Pow(1 - t, 3); v.style.scale = new Scale(Vector3.one * Mathf.Lerp(1.4f, 1f, e)); });
            int j = 0; foreach (var c in liste.Children()) { var cc = c; int gec = 260 + 60 * j++; cc.style.opacity = 0f; cc.schedule.Execute(() => cc.experimental.animation.Start(0f, 1f, 200, (v, t) => v.style.opacity = t)).StartingIn(gec); }
            CanliTut(o, Mathf.Max(420, 260 + 60 * j + 200));
        }

        // ---- Tur töreni (2026-11-07 stüdyo incelemesi GD1 + tur mührü GD5, yasin karar kartı "Ekle") ----
        // Tur finali bugüne kadar maç sonu kartında tek not cümlesiydi ("Asya Turu tamam!"); 20-40 dakikalık turun zirvesi sıradan
        // galibiyetten ayrışmıyordu. Menüye dönüşte bir kez: "Tur tamam", tur adı, turun yıldızı x/18 ve mühür (bronz 9★ = çerçeve
        // yıldız, altın 18★ = dolu yıldız: biçim ve yazı, yalnız renk değil), turda yenilen altı rakip, tek turuncu düğme "Sıradaki:
        // <tur>" haritayı o tura açar (maça değil). Yalnız mühür kazanıldıysa (eski bir turu yıldızlarken) aynı kart, başlık mühür.
        // Zorunlu bekleme yok; zemine dokunmak kapatır. Hareketi azaltta solma ve büyüme yok. Seviye anı ardından açılır.
        static int torenTur = -1, torenMuhur0, torenMuhur1; static bool torenBitti;
        public static void TorenBekle(int t, bool bitti, int m0, int m1) { torenTur = t; torenBitti = bitti; torenMuhur0 = m0; torenMuhur1 = m1; }
        public void TurTorenTur(int t, bool bitti, int m0, int m1) { TurToren(t, bitti, m0, m1, null); }   // EkranTuru
        static readonly Color BRONZ = Kit.Hex("#D9A066");
        void TurToren(int t, bool bitti, int m0, int m1, System.Action sonra)
        {
            SeviyeAniKapat(); var p = Ayar.ilerleme; if (p == null || t < 0 || t >= KariyerVeri.Turlar.Length) { if (sonra != null) sonra(); return; }
            var o = new VisualElement(); o.AddToClassList("seviye-ani"); seviyeAni = o; kok.Add(o);
            bool kapandi = false;
            System.Action kapat = () => { if (kapandi) return; kapandi = true; if (seviyeAni == o) seviyeAni = null; AniSondur(o); if (sonra != null) sonra(); };
            VisualElement ey; var ic = AniIskelet(o, kapat, out ey);
            bool yeniMuhur = m1 > m0; string muhurAd = m1 >= 2 ? L.T("Altın tur mührü") : L.T("Bronz tur mührü");
            ic.Add(Yazi(Buyuk(bitti ? L.T("Tur tamam") : muhurAd), 15, "seviye-ust"));
            var ad = Yazi(L.T(KariyerEkranTurAd(t)), 30, "kalin"); ad.AddToClassList("toren-ad"); ic.Add(ad);
            int ys = p.TurYildiz(t);
            var mr = Satir("toren-muhur"); mr.pickingMode = PickingMode.Ignore; ic.Add(mr);
            // r147 büyük yazı: mühür yıldızı 56 → 40 (metne yer), satır tam genişlik (USS), metin sütunu kalan yeri alır ve sarar.
            var mi = Ikon(m1 >= 2 ? Ikonlar.YildizDolu : m1 == 1 ? Ikonlar.YildizBos : Ikonlar.YildizBos, K >= 1.35f ? 40 : 56, m1 >= 2 ? ALTIN : m1 == 1 ? BRONZ : SOLUK); mi.style.flexShrink = 0; mr.Add(mi);
            var my = new VisualElement(); my.pickingMode = PickingMode.Ignore; my.style.marginLeft = 10; my.style.flexShrink = 1; my.style.minWidth = 0f; mr.Add(my);   // r137 büyük yazı: alt satır kartın sağından taşıyordu
            if (K >= 1.35f) { my.style.flexGrow = 1; my.style.flexBasis = 0; }
            my.Add(Yazi(L.T("{0}/{1} yıldız", ys, 18), 20, "rakam"));
            string alt = m1 >= 2 ? muhurAd : m1 == 1 ? muhurAd + " · " + L.T("altın için {0} yıldız", Ilerleme.MUHUR_ALTIN - ys) : L.T("bronz mühür için {0} yıldız", Ilerleme.MUHUR_BRONZ - ys);
            var ay = Yazi(alt, 13, yeniMuhur ? "durum-gecti" : "soluk"); ay.style.whiteSpace = WhiteSpace.Normal; my.Add(ay);
            // 2026-11-10 inceleme (fikir 02:05 #3): bronzda "altın için n yıldız" nereye gidileceğini söylemiyordu, oyuncu Kariyer'de
            // altı kartı tek tek açıyordu. Tek dokunulabilir satır: tören kapanır, Kariyer o turda ilk eksik yıldızlı maç kartı
            // açık gelir. Ödül vaadi yok (altın mühür kendi ödülü); altında ve mühürsüzde satır yok. 44 pt ikincil düğme.
            int eksikG = -1, eksikN = 0;
            if (m1 == 1) for (int b = 0; b < KariyerVeri.BASAMAK; b++) { int g = KariyerVeri.Global(t, b); if (KariyerVeri.Var(g) && p.Kazanildi(g) && p.Yildiz(g) < 3) { eksikN++; if (eksikG < 0) eksikG = g; } }
            if (eksikG >= 0)
            {
                int eg = eksikG, et = t;
                var ek = Dugme(L.T("Eksik yıldız: {0} maç", eksikN) + "  ›", () => { kapat(); kariyerTur = et; SekmeAc(Sekme.Kariyer); MacKartAc(eg); }, "toren-eksik", null, 14);
                ic.Add(ek);
            }
            // turda yenilen rakipler (renk noktası + ad; yenilmeyen soluk)
            var yenilenAd = new System.Collections.Generic.List<string>(); var rk = Satir("toren-rakipler"); rk.pickingMode = PickingMode.Ignore; ic.Add(rk);
            for (int b = 0; b < KariyerVeri.BASAMAK; b++)
            {
                int g = KariyerVeri.Global(t, b); var st = KariyerVeri.Rakip(g); bool yen = p.Kazanildi(g); if (yen) yenilenAd.Add(st.isim);
                var h = new VisualElement(); h.AddToClassList("toren-rakip"); h.pickingMode = PickingMode.Ignore; if (!yen) h.AddToClassList("toren-rakip--yok"); rk.Add(h);
                var n = new VisualElement(); n.AddToClassList("toren-nokta"); n.style.backgroundColor = Kit.Hex(st.renk); n.pickingMode = PickingMode.Ignore; h.Add(n);
                if (yen) n.Add(Ikon(Ikonlar.Tik, 14, MUREKKEP));
                if (K < 1.35f) { var ry = Yazi(st.isim, 11, "soluk"); ry.style.whiteSpace = WhiteSpace.NoWrap; h.Add(ry); }   // büyük yazıda altı ad tek satıra sığmıyor (r137): yalnız nokta, ad erişilebilirlik etiketinde
            }
            int sonrakiT = t + 1; bool sonrakiVar = bitti && sonrakiT < KariyerVeri.Turlar.Length && KariyerVeri.Turlar[sonrakiT].Var;
            if (sonrakiVar) ey.Add(Dugme(L.T("Sıradaki: {0}", L.T(KariyerEkranTurAd(sonrakiT))), () => { kapat(); kariyerTur = sonrakiT; SekmeAc(Sekme.Kariyer); }, "dugme--birincil"));
            else if (bitti && sonrakiT < KariyerVeri.TUR) ey.Add(Yazi(L.T("Yakında: bu tur sonraki güncellemede gelir"), 13, "soluk"));
            ey.Add(Dugme(L.T("Tamam"), kapat, sonrakiVar ? "dugme--hayalet" : "dugme--birincil"));
            Erisim.Etiket(ic, (bitti ? L.T("Tur tamam") : muhurAd) + ", " + L.T(KariyerEkranTurAd(t)) + ", " + L.T("{0}/{1} yıldız", ys, 18) + ", " + alt + (yenilenAd.Count > 0 ? ", " + string.Join(", ", yenilenAd) : ""), "metin");
            root.sfx.UiSes("odul"); Yerel.Hap(bitti || m1 >= 2 ? "flow" : "win");
            if (Ayar.reduce) return;
            o.style.opacity = 0f; o.experimental.animation.Start(0f, 1f, 220, (v, k) => v.style.opacity = k);
            mi.style.scale = new Scale(Vector3.one * 1.5f);
            mi.experimental.animation.Start(0f, 1f, 480, (v, k) => { float e = 1 - Mathf.Pow(1 - k, 3); v.style.scale = new Scale(Vector3.one * Mathf.Lerp(1.5f, 1f, e)); });
            int j = 0; foreach (var c in rk.Children()) { var cc = c; int gc = 300 + 70 * j++; cc.style.opacity = 0f; cc.schedule.Execute(() => cc.experimental.animation.Start(0f, 1f, 180, (v, k) => v.style.opacity = k)).StartingIn(gc); }
            CanliTut(o, Mathf.Max(480, 300 + 70 * j + 180));
        }
        static string KariyerEkranTurAd(int t) { return TurAd(t); }

        // Giriş: 24 pt sağdan, solarak, .22 sn (HTML pIn ile aynı); hareketi azaltta USS geçişi 0 sn, yalnız görünür olur.
        // 2026-11-27 inceleme (fikir 07:06 #4, uzamsal tutarlılık): sekme geçişi hep sağdan geliyordu; Profil'den Oyna'ya giderken
        // alt bant göstergesi sola kayarken içerik ters yönden giriyordu. Çubukta soldaki sekmeye geçişte içerik soldan girer.
        void Giris(VisualElement v, bool sol = false)
        {
            if (v == null || Ayar.reduce) return; v.AddToClassList("giris"); v.EnableInClassList("giris--sol", sol);
            v.schedule.Execute(() => { v.RemoveFromClassList("giris"); v.RemoveFromClassList("giris--sol"); }).StartingIn(33);
        }

        // Durum değişti: hepsi kirli, görünen hemen yeniden kurulur.
        public void Kirlet() { SeviyeAniKapat(); for (int i = 0; i < SEKME_N; i++) kirli[i] = true; ustKirli = true; SekmeCubuguGorunur(); var once = gez.sekme; Uygula(once, false); }

        // İlk açılışta sekme çubuğu yok (2026-11-07, inceleme 18:20 madde 2; Tennis Clash ilk maçtan önce menüyü göstermez):
        // Oyna'yı tek "İlk maç" kartına indirmenin değeri, yanında 0 Tüy'le kilitli Atölye/Dolap açıkken yarıya iniyordu.
        // Koşul AnaEkran'ın ilk açılış koşuluyla aynı (antrenman görülmedi, galibiyet yok, bekleyen maç yok); antrenman
        // bitince düşer, yani kaybeden oyuncu menüsüz kalmaz. Devirle gelen galibiyetli oyuncu çubuğu hep görür.
        // Ayarlar düğmesi ve Tüy hapı üst şeritte kalır (Gizlilik/Destek ilk ekrandan erişilebilir, App Store).
        bool IlkAcilis => !Ayar.antrenman && Ayar.wins == 0 && (root == null || !root.bekleyen);
        // 2026-11-23 (yasin "Beyaz alt bant incelmeli", seçim "Alt bant C"): sekme çubuğu kenarlardan ayrık yüzen beyaz hap.
        // Ev çizgisi payı artık çubuğun içine dolgu olarak girmiyor (bant o kadar kalınlaşıyordu); hap, payın bir kısmı kadar
        // yukarıda durur, altında zemin görünür. Akışta kalır: içerik hapın üstünde biter, her sayfaya ayrı alt boşluk gerekmez.
        void CubukAltPay() { if (sekmeCubugu == null) return; sekmeCubugu.style.paddingBottom = 0f; sekmeCubugu.style.marginBottom = Mathf.Max(12f, altBosluk - 14f); }   // 2026-11-23 (yasin "biraz daha aşağı"): kapsülün altı ana ekran çizgisinin hemen üstünde (iPhone 34 pt güvenli alanda 12 pt); daha aşağısı çizginin hareket alanına girer, dokunuş sistemle çakışır. 2026-11-26 (yasin "alt barı biraz yukarı alabiliriz", Dolap'ta): 8 pt yukarı, 34 pt alanda 20 pt; ev çizgisi olmayan cihazda en az 12
        void SekmeCubuguGorunur() { if (sekmeCubugu != null) sekmeCubugu.style.display = IlkAcilis ? DisplayStyle.None : DisplayStyle.Flex; }

        void HepsiniKur()
        {
            sekmeCubugu.Clear(); kok.EnableInClassList("buyuk-yazi", K >= 1.35f);   // büyük yazıda çipler ve ızgaralar alt alta (USS)
            bool etiket = K <= 1.5f;   // ×1,5 üstünde sekme etiketi kalkar, simge + ekran okuyucu etiketi kalır (§2.4)
            var ik = new[] { Ikonlar.SekmeOyna, Ikonlar.SekmeKupa, Ikonlar.SekmeTisort, Ikonlar.SekmeKisi, Ikonlar.SekmeRaket };   // 2026-11-23: çizgi simgeler (UiOge)
            foreach (var s in SEKME_SIRA)
            {
                int i = (int)s; var d = new VisualElement(); d.AddToClassList("sekme"); sekmeDugme[i] = d;
                var cizgi = new VisualElement(); cizgi.AddToClassList("sekme-cizgi"); cizgi.pickingMode = PickingMode.Ignore; d.Add(cizgi);
                // 2026-11-07 (stüdyo AD4): simge 22 → 24, etiket 11 → 12; seçili simge USS'te büyür ve kalkar (stok iOS sekmesi gibiydi).
                // 2026-11-23 yüzen hap (yasin "Alt bant C"): sarı yalnız simgenin arkasındaki kapta, yazı mürekkep; etiket 12 → 11.
                var im = Ikon(ik[i], 24, s == gez.sekme ? CTA_MUREKKEP : SEKME_SOLUK); im.AddToClassList("sekme-ikon");
                var kap = new VisualElement(); kap.AddToClassList("sekme-ikon-kap"); kap.pickingMode = PickingMode.Ignore; kap.Add(im); d.Add(kap);
                string ad = L.T(SEKME_AD[i]); if (etiket) d.Add(Yazi(ad, 11, "sekme-yazi"));
                d.AddManipulator(new Clickable(() => SekmeAc(s))); Erisim.Etiket(d, ad, "sekme", s == gez.sekme);
                sekmeCubugu.Add(d);
            }
            CubukAltPay();
            SekmeCubuguGorunur(); GostergeKur(); IsaretKur();
            // Yazı boyutu Ayarlar'dan değişince açık tam sayfa yeniden kurulur; oyuncu yerini kaybetmesin (2026-11-20, fikir 12:06 #2).
            var eski = sayfa != null ? (sayfa.Q<ScrollView>(null, "govde-kaydir") ?? sayfa.Q<ScrollView>()) : null; Vector2 of = eski != null ? eski.scrollOffset : Vector2.zero;
            for (int i = 0; i < SEKME_N; i++) kirli[i] = true; ustKirli = true; Uygula(gez.sekme, false);
            if (eski != null) KaydirmaGeri(sayfa, of);
        }

        void SekmeKur(Sekme s)
        {
            var k = sekmeKok[(int)s]; kirli[(int)s] = false;
            switch (s)
            {
                case Sekme.Oyna: OynaKur(k); break;
                case Sekme.Kariyer: KariyerKur(k); break;
                case Sekme.Soyunma: SoyunmaKur(k); break;
                case Sekme.Atolye: AtolyeKur(k); break;
                default: ProfilKur(k); break;
            }
        }

        // Üst bilgi şeridi (56 pt): seviye + XP, Tüy, ⚙ Ayarlar.
        // 2026-10-26 Yayın Gecesi (a-top): üç hap. Sol: avatar halkası (limon = senin taraf) + "Sv 7" + XP çizgisi; dokununca
        // Profil (yeni: hap düğme gibi göründüğü için ölü olmasın; Profil sekmesi aynı bilgiyi açar). Orta-sağ: Tüy hapı (altın
        // tüy simgesi + sayı; ekran okuyucuya "1.240 Tüy"). Sağ: ⚙ Ayarlar (44 pt daire). Sayılar dar italik (rakam).
        void UstKur()
        {
            ustKirli = false; ust.Clear(); var gr = Ayar.gear; int L_, kalan, gerek; gr.OyuncuSv(out L_, out kalan, out gerek);
            var sol = new VisualElement(); sol.AddToClassList("ust-sol"); ust.Add(sol);
            var hap = new VisualElement(); hap.AddToClassList("ust-hap"); hap.AddToClassList("profil-hap"); sol.Add(hap);
            var av = new VisualElement(); av.AddToClassList("ust-avatar"); av.pickingMode = PickingMode.Ignore; av.style.backgroundColor = Kit.Hex(Ayar.FORMA); hap.Add(av);
            // 2026-11-26 inceleme (fikir 02:06 #1, r267): avatar Portre.Benim()'in o anki RT'sini tutuyordu; RT sonradan yok edilince ya da
            // içeriği kaybolunca (Portre.Al eskisini Destroy edip yenisini çizer) hap boş beyaz daire kalıyordu, çünkü üst şerit ancak
            // anahtar değişince yeniden kurulur. Artık önbellekten okunur (eşzamanlı FBX çizimi de dokunuş karesine düşmez); yoksa insan
            // simgesi, ve PortreIsit her 300 ms'de tutulan dokunun hâlâ canlı olduğuna bakar, değilse menü durgunken yerinde yeniler.
            BenAvatar(av, 24);
            var sv = new VisualElement(); sv.AddToClassList("ust-sv"); sv.pickingMode = PickingMode.Ignore; hap.Add(sv);
            sv.Add(Yazi(L.T("Sv {0}", L_), 18, "rakam"));
            var c = Cubuk(gerek > 0 ? kalan / (float)gerek : 1, LIMON, 5);   // 2026-10-27 yön C: ilerleme turuncuydu (limon yalnız sen: avatar halkası)   // 2026-11-20 yasin karar kartı "A · Limon": seviye/XP ve gelişim = limon ("senin ilerlemen"), Tüy = altın; turuncu yalnız eylem/seçim (§8)
            c.AddToClassList("ust-xp"); sv.Add(c);   // tur 2: yorumun içinde kalmıştı, üst çubuğun XP şeridi hiç eklenmiyordu
            hap.AddManipulator(new Clickable(() => SekmeAc(Sekme.Profil)));
            Erisim.Etiket(hap, L.T("Seviye {0}", L_) + (gerek > 0 ? ", " + L.T("Sonraki seviyeye {0} XP", gerek - kalan) : "") + ", " + L.T("Profil"), "dugme");
            var tuy = new VisualElement(); tuy.AddToClassList("ust-hap"); tuy.AddToClassList("tuy-hap"); ust.Add(tuy);
            tuy.AddManipulator(new Clickable(() => { Tik(); DukkanAc(); }));   // 2026-11-01 Tüy dükkânı'na açılır
            tuy.Add(Ikon(Ikonlar.Tuy, 20, ALTIN)); var tz = Yazi(Sayi(gr.eko.tuy), 20, "rakam"); tz.AddToClassList("ust-tuy"); tuy.Add(tz);
            // 2026-11-22 Stadyum Pop: hap zaten dükkânı açıyordu ama düğme olduğu görünmüyordu; sağında beyaz "+" kutusu (para
            // çubuğu kalıbı). Beyaz, sarı değil: sarı ekranda yalnız Oyna'nın. Ayrı dokunma alanı yok, bütün hap 44 pt.
            var arti = new VisualElement(); arti.AddToClassList("tuy-arti"); arti.pickingMode = PickingMode.Ignore; arti.Add(Ikon(Ikonlar.Arti, 12, KOYU)); tuy.Add(arti);
            Erisim.Etiket(tuy, L.T("{0} Tüy", gr.eko.tuy) + ", " + L.T("Tüy dükkânı"), "dugme");
            OdulAnlari(tuy, tz, gr.eko.tuy, L_);
            var ayar = new VisualElement(); ayar.AddToClassList("dugme"); ayar.AddToClassList("ikon-dugme"); ayar.Add(Ikon(Ikonlar.Disli, 22, SOLUK));
            ayar.AddManipulator(new Clickable(AyarlarAc)); Erisim.Etiket(ayar, L.T("Ayarlar"), "dugme"); ust.Add(ayar);
        }
        // Binlik ayraçlı sayı (Türkçe nokta, İngilizce virgül): 1.240 Tüy.
        public static string Sayi(int n) { return n.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", L.En ? "," : "."); }

        // ---- 2026-10-26 Yayın Gecesi: Oyna zemini (a-arena) ----
        // Katmanlar (arkadan öne): iki projektör ışığı (sol limon, sağ gök; a-flood), karartma gradyanı (a-fade: üst şerit ve
        // alttaki plaka okunsun, ortada salon görünsün), VS afişi (Afis RT; ScaleAndCrop: her en-boyda kırpılır, esnemez) ve
        // "VS". Zemin yalnız Oyna'da görünür (.kok.koyu #zemin gizli). Büyük yazıda afiş soluklaşır (dekor; içerik kayar).
        Image afisImg; Label vsYazi;
        void ZeminKur()
        {
            // 2026-10-29 (yasin: "menüler düzeltilecek, uygulama içi sanat tasarımı splashteki loading'i örnek alacak"): bulanık
            // salon fotoğrafı ve iki projektör ışığı yerine açılış perdesinin dili, düz ve grafik: mürekkep zemin, afişin
            // arkasında batan güneş diski (perdeyle aynı halkalar, çok yavaş döner; hareketi azaltta durur), altında kart
            // renginde diyagonal bant ve 4 pt turuncu kenarı, kenar boyunca altın raket izi. Fotoğraf her en-boyda farklı
            // kırpılıyor ve figürlerin arkasında gürültü oluyordu; düz şekil afişi öne çıkarır, perde → menü geçişi aynı resim.
            // Salon fonu artık çekilmez (salonImg null: SahneCiz atlar, ana kamera menüde yine yalnız temizler).
            // 2026-11-22 Stadyum Pop (yasin: "stadyum pop tan devam edelim, ama ai unslop olmalı"): batan güneş Gün Batımı
            // dilinindi; gündüz salonunda yerini salon ışığı (üstte yumuşak beyaz) ve oyuncuların bastığı kort alır. Güneş öğesi
            // kalır ama gizli (GunesYerlestir ve döndürme kodu dokunulmadan çalışsın); bant ve altın iz USS'te gizli.
            // 2026-11-26 inceleme (fikir 17:06 #3): Stadyum Pop kesinleşti (23 Kas secim.md); gizli disk yine 20 Hz döndürülüyor,
            // koyu sekme kesiti gizliyken yerleştiriliyordu. Disk, kesit, bant, iz ve döndürme kalktı; GunesDisk dokusu Kariyer'de kalır.
            salonIsik = new Image { image = Ikonlar.Isik, scaleMode = ScaleMode.StretchToFill, tintColor = new Color(1, 1, 1, .22f) }; salonIsik.AddToClassList("salon-isik"); salonIsik.pickingMode = PickingMode.Ignore; zemin.Add(salonIsik);
            kortZemin = new VisualElement(); kortZemin.AddToClassList("kort-zemin"); kortZemin.pickingMode = PickingMode.Ignore; kortZemin.generateVisualContent += KortCiz; zemin.Add(kortZemin);
            zemin.RegisterCallback<GeometryChangedEvent>(e => ZeminYerlestir());
            var zi = new Image { image = Ikonlar.Gradyan, scaleMode = ScaleMode.StretchToFill }; zi.pickingMode = PickingMode.Ignore; zi.AddToClassList("tam"); zemin.Add(zi);
            afisImg = new Image { image = root.afis.rt, scaleMode = ScaleMode.ScaleAndCrop }; afisImg.AddToClassList("afis"); afisImg.pickingMode = PickingMode.Ignore; zemin.Add(afisImg);
            vsYazi = new Label("VS"); vsYazi.AddToClassList("yazi"); vsYazi.AddToClassList("vs"); vsYazi.pickingMode = PickingMode.Ignore; zemin.Add(vsYazi);
            // 2026-11-07 stüdyo incelemesi (AD3): koyu sekmelerde (Kariyer, Dolap, Atölye, Profil, tam sayfalar) dünya hiç yoktu,
            // şablon hissinin ana kaynağıydı. Oyna'nın güneşinin soluk bir kesiti üst şeridin arkasından batar: aynı doku, sabit
            // (dönmez, hareket yok), %20 opak; yazılar sekme başlığından sonra başladığı için kontrast değişmez. Yeni asset yok.
            // Stadyum Pop'ta (22 Kas) bu kesit USS'te gizlendi, 26 Kas'ta kodu da kalktı (yukarıdaki not).
        }
        Image salonIsik; VisualElement kortZemin;
        // Oyna sahnesi (2026-11-27, yasin: "oyna ekranının arkası cool bir sahne olsun, saha çizimi gerçek saha olsun, daha kort hissi,
        // biraz sonra maça girme hissi; tribün ve file de çizili"). Eski kort düz yeşil bir yamuktu (yalnız beş çizgi, file yok, tribün
        // yok). Şimdi tek painter2D çizimi: tribün (5 sıra seyirci, koltuk bandı), reklam şeridi, gerçek oranlı kort (çift/tek yan
        // çizgi, kısa servis, çift uzun servis, orta çizgi) ve çizili file (ağ, üst bant, direkler). Perspektif gerçek bir iğne deliği
        // kamerasından: nokta (u, v) → uzaklık D = d + (1−v)·L, x ölçeği f/D, y = ufuk + f·yükseklik/D; çizgi oranları BWF ölçüsünde
        // (13,4 × 6,1 m; tek yan çizgi 0,46 m içte; kısa servis fileden 1,98 m; çift uzun servis 0,76 m içte). Çizim tasarım
        // prototipinde (390 × 300 birim) yazıldı, her boyda orantıyla ölçeklenir. Seyirci düzeni sabit tohumlu (her açılışta aynı).
        // Yalnız geometri değişince bir kez çizilir; karede iş yok. Ayaklar kortun %36 çizgisinde (ZeminYerlestir), file oyuncuların
        // belinin arkasında kalır: "maça girmeden sahada duruyorlar".
        static readonly Color KORT_YESIL = Kit.Hex("#2FA863"), KORT_CIZGI = new Color(1, 1, 1, .95f), KORT_ZEMIN = Kit.Hex("#12306B");
        static readonly Color[] SEYIRCI = { Kit.Hex("#E8D7C3"), Kit.Hex("#F2B6C8"), Kit.Hex("#9EC5FF"), Kit.Hex("#FFD36B"), Kit.Hex("#C9A6FF"), Kit.Hex("#FFFFFF"), Kit.Hex("#9BE3B8") };
        static readonly Color[] LED = { Kit.Hex("#FFE14D"), Kit.Hex("#FF7AC8"), Kit.Hex("#6FD1FF"), Kit.Hex("#FFFFFF") };
        static void KortCiz(MeshGenerationContext ctx)
        {
            var r = ctx.visualElement.contentRect; if (r.width <= 0 || r.height <= 0) return;
            var p = ctx.painter2D; float sx = r.width / 390f, sy = r.height / 300f, sr = Mathf.Min(sx, sy);
            System.Func<float, float, Vector2> Q = (x, y) => new Vector2(x * sx, y * sy);
            System.Action<Color, float, float, float, float> Dik = (c, x, y, w, h) => { p.fillColor = c; p.BeginPath(); p.MoveTo(Q(x, y)); p.LineTo(Q(x + w, y)); p.LineTo(Q(x + w, y + h)); p.LineTo(Q(x, y + h)); p.ClosePath(); p.Fill(); };
            // tribün: yukarı doğru sıklaşan 5 sıra, her sırada baş (altıgen) ve omuz; boş koltuklar %12
            var rng = new System.Random(7); const int SIRA = 5; const float Y0 = 60, SIRA_H = 15;
            for (int t = 0; t < SIRA; t++)
            {
                float y = Y0 + t * SIRA_H; Dik(new Color(.03f, .06f, .19f, .55f + t * .06f), 0, y, 390, SIRA_H);
                float adim = 10 + t * 1.4f, rr = (3 + t * .35f) * sr, a = .55f + t * .07f;
                for (float x = (t % 2) * adim / 2; x < 390 + adim; x += adim)
                {
                    if (rng.NextDouble() < .12) { rng.NextDouble(); continue; }
                    var col = SEYIRCI[(int)(rng.NextDouble() * SEYIRCI.Length)]; col.a = a;
                    var bas = Q(x, y + SIRA_H * .4f); p.fillColor = col; p.BeginPath();
                    for (int k = 0; k < 6; k++) { float ac = k * Mathf.PI / 3f; var n = bas + new Vector2(Mathf.Cos(ac), Mathf.Sin(ac)) * rr; if (k == 0) p.MoveTo(n); else p.LineTo(n); }
                    p.ClosePath(); p.Fill();
                    p.fillColor = new Color(.12f, .16f, .43f, a); var om = Q(x, y + SIRA_H * .95f); float ow = rr * 1.7f, oh = rr * 1.25f;
                    p.BeginPath(); p.MoveTo(om + new Vector2(-ow, 0)); p.LineTo(om + new Vector2(-ow * .7f, -oh * .9f)); p.LineTo(om + new Vector2(ow * .7f, -oh * .9f)); p.LineTo(om + new Vector2(ow, 0)); p.ClosePath(); p.Fill();
                }
            }
            float by = Y0 + SIRA * SIRA_H + 1; Dik(Kit.Hex("#0A1236"), 0, by, 390, 13);
            for (int i = 0; i * 44 + 6 < 390; i++) { var c = LED[i % 4]; c.a = .9f; Dik(c, 6 + i * 44, by + 3, 34, 7); }
            Dik(new Color(1, 1, 1, .35f), 0, by, 390, 1.5f);
            // kort: iğne deliği kamerası
            const float UFUK = 70, F = 300, KAM_H = 5, MESAFE = 5.5f, UZUN = 13.4f, GENIS = 6.1f;
            System.Func<float, float, Vector2> P = (u, v) => { float D = MESAFE + (1 - v) * UZUN; return Q(195 + (u - .5f) * GENIS * F / D, UFUK + F * KAM_H / D); };
            System.Action<Color, Vector2, Vector2, Vector2, Vector2> Dortgen = (c, a1, b1, c1, d1) => { p.fillColor = c; p.BeginPath(); p.MoveTo(a1); p.LineTo(b1); p.LineTo(c1); p.LineTo(d1); p.ClosePath(); p.Fill(); };
            Dortgen(KORT_ZEMIN, P(-.35f, -.06f), P(1.35f, -.06f), P(1.35f, 1.2f), P(-.35f, 1.2f));
            Dortgen(KORT_YESIL, P(0, 0), P(1, 0), P(1, 1), P(0, 1));
            for (int i = 0; i < 6; i += 2) Dortgen(new Color(1, 1, 1, .05f), P(i / 6f, 0), P((i + 1) / 6f, 0), P((i + 1) / 6f, 1), P(i / 6f, 1));
            p.strokeColor = KORT_CIZGI; p.lineWidth = 2 * sr; p.lineJoin = LineJoin.Round;
            System.Action<float, float, float, float> Cz = (u1, v1, u2, v2) => { p.BeginPath(); p.MoveTo(P(u1, v1)); p.LineTo(P(u2, v2)); p.Stroke(); };
            float yan = .46f / GENIS, sv = 1.98f / UZUN, ls = .76f / UZUN;
            Cz(0, 0, 1, 0); Cz(0, 1, 1, 1); Cz(0, 0, 0, 1); Cz(1, 0, 1, 1); Cz(yan, 0, yan, 1); Cz(1 - yan, 0, 1 - yan, 1);
            Cz(0, ls, 1, ls); Cz(0, 1 - ls, 1, 1 - ls); Cz(0, .5f - sv, 1, .5f - sv); Cz(0, .5f + sv, 1, .5f + sv); Cz(.5f, 0, .5f, .5f - sv); Cz(.5f, .5f + sv, .5f, 1);
            // file: koyu ağ, ince ağ çizgileri, beyaz üst bant, iki direk (yükseklik 1,55 m)
            var nl = P(-.04f, .5f); var nr = P(1.04f, .5f); float nh = F * 1.55f / (MESAFE + UZUN * .5f) * sy;
            p.fillColor = new Color(.04f, .08f, .24f, .5f); p.BeginPath(); p.MoveTo(nl + new Vector2(0, -nh)); p.LineTo(nr + new Vector2(0, -nh)); p.LineTo(nr); p.LineTo(nl); p.ClosePath(); p.Fill();
            p.strokeColor = new Color(1, 1, 1, .28f); p.lineWidth = .8f * sr; float ag = 5 * sr;
            p.BeginPath(); for (float x = nl.x; x <= nr.x; x += ag) { p.MoveTo(new Vector2(x, nl.y - nh)); p.LineTo(new Vector2(x, nl.y)); } for (float y = nl.y - nh; y <= nl.y; y += ag) { p.MoveTo(new Vector2(nl.x, y)); p.LineTo(new Vector2(nr.x, y)); } p.Stroke();
            Dik2(p, Color.white, nl.x, nl.y - nh - sr, nr.x - nl.x, 3.5f * sr);
            var direk = Kit.Hex("#DFE6F5"); Dik2(p, direk, nl.x - 2 * sr, nl.y - nh - 3 * sr, 3.5f * sr, nh + 5 * sr); Dik2(p, direk, nr.x - 1.5f * sr, nl.y - nh - 3 * sr, 3.5f * sr, nh + 5 * sr);
        }
        static void Dik2(Painter2D p, Color c, float x, float y, float w, float h) { p.fillColor = c; p.BeginPath(); p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(x + w, y)); p.LineTo(new Vector2(x + w, y + h)); p.LineTo(new Vector2(x, y + h)); p.ClosePath(); p.Fill(); }
        void ZeminYerlestir()
        {
            var r = zemin.contentRect; if (r.width <= 0 || r.height <= 0) return;
            // kort: afişteki ayakların hizasından (üst %19) plakanın arkasına (%36); salon ışığı üstte, genişliğin 1,4 katı
            if (kortZemin != null) { kortZemin.style.left = 0; kortZemin.style.width = r.width; kortZemin.style.top = 0; kortZemin.style.height = r.height * .4655f; }   // 2026-11-27: sahne tribünle birlikte tepeden başlar; ayaklar %36'da (KortCiz)
            if (salonIsik != null) { float s = r.width * 1.4f; salonIsik.style.width = s; salonIsik.style.height = s * .7f; salonIsik.style.left = (r.width - s) / 2; salonIsik.style.top = -s * .3f; }
        }
        // Afiş: Oyna görünürken açık. Rakip = Oyna'daki büyük düğmenin oynatacağı maçın rakibi (bekleyen maç, Usta yolu ya da
        // kariyerin sıradaki basamağı); ilk açılışta (antrenman görülmedi) afiş hiç çizilmez (aşağıda; eskiden rakipsiz, yalnız sen).
        void AfisGuncelle(bool gerek)
        {
            var a = root.afis; if (a == null) return;
            // 2026-10-26 gece: ilk açılışta (antrenman görülmedi) Oyna'nın kahramanı logo ve ad; afişin "sen" figürü r22'de
            // "Birdie Smash" yazısının ve logonun üstüne biniyordu (menu_ilk_oyna). Afiş ilk maçtan sonra gelir.
            bool goster = gerek && Ayar.antrenman;
            if (afisImg != null) afisImg.style.display = goster ? DisplayStyle.Flex : DisplayStyle.None;
            if (!goster) { if (a.Acik) a.Kapat(); if (vsYazi != null) vsYazi.style.display = DisplayStyle.None; return; }
            Style st = null;
            if (root.bekleyen && root.match != null) st = root.match.stil;
            else if (Ayar.antrenman && UstaYolu.Acik(Ayar.ilerleme) && Ayar.ilerleme.HepsiBitti) st = UstaYolu.Rakip(Ayar.ilerleme.Usta);
            else if (Ayar.antrenman) st = Match.KariyerRakip(Ayar.ilerleme.Siradaki());
            a.Ac(st, st != null ? Kit.Hex(Match.OpRenk(st, Ayar.FORMA)) : Color.white);
            if (vsYazi != null) vsYazi.style.display = st != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ---- 2026-10-26 gece (yasin, build 40, gerçek iPhone: "Menüler de sahaya iç içe geçmiş görünüyor") ----
        // Kök neden: menü kökü saydamdı (Oyna'da tamamen, diğer sekmelerde %92, tam sayfada %96 örtü) ve ana kamera menüde
        // maç sahnesini her kare çizmeyi sürdürüyordu. Oyna'da afişin iki büyük figürü canlı kortun, kortta duran iki küçük
        // oyuncunun ve hakemlerin önüne biniyordu (aynı oyuncu iki kez: afişte büyük, kortta küçük); Kariyer, Soyunma ve
        // Profil'de kort çizgileri ve oyuncular kartların arasından seçiliyordu. Yön A taslağı bunu istemiyor: Oyna'da salon
        // bulanık bir fon (a-arena, arena_blur), diğer sekmelerde düz gece zemini (a-bgnight); figür yalnız afişte.
        // Çözüm: (1) menü kökü her sekmede opak (satır içi, Uygula; ayrıca biçim sayfası yanlış yükleniyordu: StilSayfasi); (2) menü görünür ve perde yokken ana kamera yalnız temizler
        // (cullingMask 0: sahne çizilmez, pil ve GPU da kazanır; kamera kapatılmaz ki arka tampon her karede tanımlı kalsın);
        // (3) Oyna'nın fonu, sahnenin oyuncusuz bir karesi: menü açılınca (ya da saha, bakış, model, ekran boyu değişince)
        // bir kez küçük bir RT'ye çizilir, iki kez yarıya indirilerek bulanıklaştırılır ve Image olarak gösterilir.
        RenderTexture salonRt; Image salonImg; bool salonKirli = true; string salonSaha, salonKamera, salonModel; int salonW, salonH, sahneMaske; bool maskeVar;
        readonly List<Renderer> salonGizli = new List<Renderer>();
        // ciz: ana kamera sahneyi çizsin mi (maçta ve perde varken evet). menu: menü görünür; Oyna'daysa salon fonu gerekirse
        // perde açılırken de çekilir (perde kalkınca canlı korttan bulanık fona sıçrama olmasın).
        void SahneCiz(bool ciz, bool menu)
        {
            var c = root.cam; if (c == null) return;
            if (!maskeVar) { maskeVar = true; sahneMaske = c.cullingMask; }   // r24: −1 (Everything) bekçi değeriyle karışıyordu, maçta maske 0 kalıyordu
            int m = ciz ? sahneMaske : 0; if (c.cullingMask != m) c.cullingMask = m;
            if (menu && salonImg != null && sekmeKok[0] != null && gez.sekme == Sekme.Oyna)   // 2026-10-29: salon fonu kalktı (ZeminKur), null
            {
                if (salonSaha != Ayar.saha || salonKamera != Ayar.kamera || salonModel != Ayar.model || salonW != Screen.width || salonH != Screen.height) salonKirli = true;
                if (salonKirli) SalonCek();
            }
        }
        void SalonCek()
        {
            var c = root.cam; if (Screen.width <= 0 || Screen.height <= 0) return;
            salonKirli = false; salonSaha = Ayar.saha; salonKamera = Ayar.kamera; salonModel = Ayar.model; salonW = Screen.width; salonH = Screen.height;
            int w = 192, h = Mathf.Max(8, Mathf.RoundToInt(192f * Screen.height / Screen.width));
            // Oyuncular (ben, rakip, hakemler: kökün doğrudan çocukları) bu karede gizli; afiş ve vitrin figürleri kendi
            // düğümlerinin altında ve zaten uzakta (±500 m). Görünen renderer'lar kapatılıp geri açılır (etkinlik değişmez:
            // Animator ve PlayerView durumu bozulmaz).
            salonGizli.Clear();
            foreach (Transform t in root.transform)
                if (t.GetComponent<PlayerView>() != null) foreach (var r in t.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; salonGizli.Add(r); }
            var etkin = RenderTexture.active;
            var a = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32); a.filterMode = FilterMode.Bilinear;
            var eskiHedef = c.targetTexture; int eskiMaske = c.cullingMask; c.cullingMask = sahneMaske; c.targetTexture = a; c.Render(); c.targetTexture = eskiHedef; c.cullingMask = eskiMaske;
            foreach (var r in salonGizli) if (r != null) r.enabled = true; salonGizli.Clear();
            var b = RenderTexture.GetTemporary(w / 2, h / 2, 0, RenderTextureFormat.ARGB32); b.filterMode = FilterMode.Bilinear; Graphics.Blit(a, b);
            var d = RenderTexture.GetTemporary(w / 4, h / 4, 0, RenderTextureFormat.ARGB32); d.filterMode = FilterMode.Bilinear; Graphics.Blit(b, d);
            if (salonRt == null || salonRt.width != w / 2 || salonRt.height != h / 2)
            {
                if (salonRt != null) { salonRt.Release(); Destroy(salonRt); }
                salonRt = new RenderTexture(w / 2, h / 2, 0, RenderTextureFormat.ARGB32) { name = "menu_salon", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            }
            Graphics.Blit(d, salonRt);   // dörtte birden yarıya geri: bilinear büyütme blokları yumuşatır
            RenderTexture.active = etkin; RenderTexture.ReleaseTemporary(a); RenderTexture.ReleaseTemporary(b); RenderTexture.ReleaseTemporary(d);
            if (salonImg != null) { salonImg.image = salonRt; salonImg.MarkDirtyRepaint(); }
        }
        // Ekran turu tanısı (EkranTuru.MenuSahne): ana kamera ne çiziyor, salon fonu var mı.
        // 2026-10-29 (yasin: "tüm alternatif dillerde wordingler ve ekranda aldıkları alanlar düzgün olmalı"): ekran turu
        // her karede bu kökün yazılarını ölçer (EkranTuru.YaziTara).
        public VisualElement KokEleman { get { return doc != null ? doc.rootVisualElement : null; } }
        public string SahneDurumu { get { var c = root.cam; return "ana kamera " + (c == null ? "yok" : (c.enabled ? "açık" : "kapalı") + " maske " + c.cullingMask) + " · salon " + (salonRt != null ? salonRt.width + "x" + salonRt.height + " " + salonSaha : "yok")
            + " · stil " + doc.rootVisualElement.styleSheets.count + " " + StilTani(doc.rootVisualElement.styleSheets.count > 0 ? doc.rootVisualElement.styleSheets[0] : null)
            + " · tema " + StilTani(doc.panelSettings != null ? doc.panelSettings.themeStyleSheet : null)
            + " zemin " + zemin.resolvedStyle.position + " kök renk " + kok.resolvedStyle.backgroundColor; } }
        static StyleSheet StilSayfasi()
        {
            var ss = Resources.Load<StyleSheet>("ui/menu_stil");
            if (ss == null || ss.name != "menu_stil") foreach (var a in Resources.LoadAll<StyleSheet>("ui")) if (a.name == "menu_stil") { ss = a; break; }
            if (ss != null) Debug.Log("Birdie Smash: menü biçimi " + StilTani(ss));
            return ss;
        }
        // r24–r36 tanısı: CI'da menü USS'siz çıkıyor; içe aktarılan sayfanın hata bayrağı ve kural sayısı (iç alanlar, yansıma).
        static string StilTani(StyleSheet ss)
        {
            if (ss == null) return "yok";
            var sb = new System.Text.StringBuilder(ss.name);
            var t = typeof(StyleSheet); var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            foreach (var ad in new[] { "importedWithErrors", "importedWithWarnings", "contentHash" }) { var pr = t.GetProperty(ad, bf); if (pr != null) try { sb.Append(" " + ad + "=" + pr.GetValue(ss)); } catch { } }
            foreach (var ad in new[] { "rules", "complexSelectors", "m_Rules", "m_ComplexSelectors", "imports", "m_Imports" }) { var f = t.GetField(ad, bf); if (f != null) try { var v = f.GetValue(ss) as System.Array; sb.Append(" " + ad + "=" + (v == null ? "null" : v.Length.ToString())); } catch { } var pr = t.GetProperty(ad, bf); if (pr != null) try { var v = pr.GetValue(ss) as System.Array; sb.Append(" " + ad + "=" + (v == null ? "null" : v.Length.ToString())); } catch { } }
            return sb.ToString();
        }
        // 2026-10-27 yön C: GECE (diğer sekmeler) ve INK_ (Oyna, tam sayfa) mor-kahve zemin; ikisi aynı ton, Oyna'da salon fonu üstte.
        static readonly Color GECE = new Color(0x26 / 255f, 0x59 / 255f, 0xBE / 255f, 1), INK_ = new Color(0x26 / 255f, 0x59 / 255f, 0xBE / 255f, 1);   // 2026-11-22 Stadyum Pop: kort mavisi #2659BE
        void OnDestroy() { if (salonRt != null) { salonRt.Release(); Destroy(salonRt); } }

        // Tam sayfa başı: "‹ Geri" (44 pt) + başlık.
        Label SayfaBasi(VisualElement ebeveyn, string baslik)
        {
            var bas = new VisualElement(); bas.AddToClassList("sayfa-basi"); ebeveyn.Add(bas);
            var g = new VisualElement(); g.AddToClassList("dugme"); g.AddToClassList("geri-dugme");
            // 2026-11-14 (tasarım dili §1.2, r170 ×2 Dükkân "Bac/k", "Şampiyonlar kupası" bölündü): geri düğmesi etiket gibi
            // daralmaz, kırılmaz; başlık daralır ve sözcük ortasından bölünmeden sarar (SozcukSigdir).
            g.style.flexShrink = 0f;
            var gy = Yazi(L.T("Geri"), 15, "kalin"); gy.style.whiteSpace = WhiteSpace.NoWrap; gy.style.flexShrink = 0f;
            g.Add(Ikon(Ikonlar.Geri, 18, Color.white)); g.Add(gy); g.AddManipulator(new Clickable(Geri)); Erisim.Etiket(g, L.T("Geri"), "dugme"); bas.Add(g);
            var t = Yazi(L.T(baslik), 19, "sayfa-baslik"); t.pickingMode = PickingMode.Position; t.style.flexShrink = 1f; t.style.flexGrow = 1f; t.style.minWidth = 0f; SozcukSigdir(t);   // flexGrow: genişlik = kalan yer (SozcukSigdir kullanılabilir genişlikle ölçer, 2026-11-19)
            Erisim.Etiket(t, L.T(baslik), "baslik"); bas.Add(t);
            return t;
        }

        void SayfaKur(string s)
        {
            sayfa.Clear();
            if (s == Gezinti.AYAR) AyarlarKur(sayfa); else if (s == Gezinti.NASIL) NasilKur(sayfa); else if (s == Gezinti.LISANS) LisansKur(sayfa); else if (s == Gezinti.ANTRENMAN) AntrenmanKur(sayfa); else if (s == Gezinti.DUKKAN) DukkanKur(sayfa);
        }

        // 2026-11-19 inceleme (fikir 17:07 #3, yasin "düğme tıklamaları standart"): dokunuş geri bildirimi bileşende (Dugme/Cip/ham
        // Clickable hepsi Tik). Eylem içinde de Tik çağrılıyordu: aynı dokunuşta iki ses + iki titreşim. Aynı karede ikinci Tik sessiz;
        // böylece 20+ çağrı yerini tek tek ayıklamak gerekmedi. Özel sesler (Yukselt, Tik(1320)…) bu kapıdan geçmez, kalır.
        int tikKare = -1;
        // 2026-11-26 (yasin: "Menüde basma hissi ve sesler de iyileştirilmeli"): ses ve titreşim Clickable'ın içinden, yani parmak
        // KALKINCA çalıyordu; basılma ölçeği ise parmak değer değmez. Göz "bastım" derken kulak ve el 100–300 ms sonra
        // duyuyordu, basış boş hissettiriyordu. Oyun menülerindeki gibi geri bildirim artık basışta: kök TrickleDown'da basılan
        // düğmeyi (dugme/cta/karo/cip/dokunulur) bulur; parmak 50 ms içinde 10 birimden fazla kaymadıysa (kaydırma değil) ses +
        // titreşim hemen, daha kısa dokunuşta kalkış anında (Clickable'dan önce). Ardından gelen Tik aynı basış için sessiz kalır
        // (çift ses yok). Kaydırmaya dönen basış sessiz; klavye/ekran okuyucu yolu Tik'te eskisi gibi çalar.
        VisualElement basEl; Vector2 basP; float basT = -1, basCaldiT = -10; bool basCaldi; int basPid = -1;
        static VisualElement Basilir(VisualElement e)
        {
            for (; e != null; e = e.parent)
                if (e.ClassListContains("gel-dugme")) return null;   // Gelişim ± kendi adım sesini çalar (basılı tutunca tekrar)
                else if (e.ClassListContains("dugme") || e.ClassListContains("sekme") || e.ClassListContains("cta") || e.ClassListContains("karo") || e.ClassListContains("cip") || e.ClassListContains("dokunulur")) return e;
            return null;
        }
        void BasisSesiKur()
        {
            kok.RegisterCallback<PointerDownEvent>(e =>
            {
                basEl = Basilir(e.target as VisualElement); basCaldi = false; basP = e.position; basT = Time.unscaledTime; basPid = e.pointerId;
                if (basEl == null) return; var el = basEl;
                kok.schedule.Execute(() => { if (basEl == el && !basCaldi && basPid >= 0) BasisCal(); }).StartingIn(50);
            }, TrickleDown.TrickleDown);
            kok.RegisterCallback<PointerMoveEvent>(e => { if (basEl != null && !basCaldi && e.pointerId == basPid && ((Vector2)e.position - basP).sqrMagnitude > 100f) basEl = null; }, TrickleDown.TrickleDown);
            kok.RegisterCallback<PointerUpEvent>(e => { if (basEl != null && !basCaldi && e.pointerId == basPid) BasisCal(); basPid = -1; }, TrickleDown.TrickleDown);
            kok.RegisterCallback<PointerCancelEvent>(e => { basEl = null; basPid = -1; }, TrickleDown.TrickleDown);
        }
        // Basışta çalan ses bu eylemin sesiydi: bir kez true döner (sonraki eylem yine kendi sesini çalar).
        bool BasisCaldi() { if (Time.unscaledTime - basCaldiT >= .8f) return false; basCaldiT = -10; return true; }
        void BasisCal()
        {
            basCaldi = true; basCaldiT = Time.unscaledTime; tikKare = Time.frameCount;
            string tur = UiSesTuru(basEl); root.sfx.UiSes(tur); Yerel.Hap(tur == "bas" ? "hit" : "tick");
        }
        // 2026-11-22 (yasin "tüm butonlarda tatmin edici ses"): ses türü öğenin sınıfından (Sfx.UiSes); öğesiz çağrılar pop çalar.
        // Birincil düğme "hit" titreşimi (14 ms), gerisi "tick": göz ve elin en çok bastığı yer en dolgun karşılığı alır.
        void Tik(VisualElement e = null, string tur = null)
        {
            if (Time.frameCount == tikKare) return; tikKare = Time.frameCount;
            if (BasisCaldi()) return;   // bu basışın sesi basışta çaldı (BasisSesiKur)
            tur = tur ?? UiSesTuru(e); root.sfx.UiSes(tur); Yerel.Hap(tur == "bas" ? "hit" : "tick");
        }
        static string UiSesTuru(VisualElement e)
        {
            if (e == null) return "pop";
            if (e.ClassListContains("sekme")) return "sekme";
            if (e.ClassListContains("geri-dugme") || e.ClassListContains("kapat-dugme")) return "geri";
            if (e.ClassListContains("dugme--birincil")) return "bas";
            if (e.ClassListContains("dugme--pasif") || e.ClassListContains("kar-tur-kutu--kilit")) return "kilit";
            if (e.ClassListContains("cip")) return "sec";
            return "pop";
        }

        // ---- 2026-10-27 yön C mikro hareketler (araştırma §4.3; yalnız translate/opacity, ekranda tek döngü afişin nefesi) ----
        // Sekme göstergesi: tek bir 3 pt turuncu çizgi sekme çubuğunun üstünde; seçim değişince yeni sekmeye 180 ms'de kayar
        // (dış öğe x, ease-out), iç öğe 90 ms'de 4 pt yükselip iner: tüy top yayı. Konum sekmenin yerleşiminden okunur (çubuğun
        // yerleşimi ekran/duyarli dalında değişebilir; buradaki hesap ondan bağımsız). Gösterge yerleşene kadar (ilk kare) her
        // sekmenin kendi çizgisi görünür; yerleşince .gosterge-var onları gizler. Hareketi azaltta geçiş 0 sn (USS .az-hareket).
        VisualElement gosterge, gostergeIc;
        void GostergeKur()
        {
            gosterge = new VisualElement(); gosterge.AddToClassList("sekme-gosterge"); gosterge.pickingMode = PickingMode.Ignore;
            gostergeIc = new VisualElement(); gostergeIc.AddToClassList("sekme-gosterge-ic"); gostergeIc.pickingMode = PickingMode.Ignore; gosterge.Add(gostergeIc);
            sekmeCubugu.Insert(0, gosterge); sekmeCubugu.RemoveFromClassList("gosterge-var");   // 2026-11-23: kapsül simgelerin ARKASINDA (ilk çocuk)
            if (!gostergeOlay) { gostergeOlay = true; sekmeCubugu.RegisterCallback<GeometryChangedEvent>(e => GostergeYerlestir(false)); }   // çubuk bir kez kurulur, olay bir kez
        }
        bool gostergeOlay;
        // 2026-11-23 alt bant detayları 2-4 (yasin: "zekice düşünülmüş detaylar", detay sayfası Sn5jD8h6dR7U8MAR8DGoik). Yalnız gerçek
        // veriden, sayı yok, baskı dili yok:
        //  - Atölye noktası: Tüy'ün ve seviyenin yettiği bir yükseltme ya da alınabilir model varsa (GameRoot.YukseltAlinir, maç
        //    sonu kartının "hazır" kararıyla aynı hesap). Dolap ve Profil'de "yeni" kaydı tutulmadığı için nokta yok (uydurma yok).
        //  - Kariyer halkası: kupa simgesinin çevresinde bu turun durak ilerlemesi (Durak n/6), Kariyer tamamsa yok.
        //  - Yarım maç: bitmemiş maç (root.bekleyen) varsa Oyna simgesinde küçük duraklat işareti.
        // Sekme değişince ve çubuk kurulunca hesaplanır (karede değil); öğeler simge kabının çocuğu, dokunuş almaz.
        VisualElement atolyeNokta, devamIsaret;
        void IsaretKur()
        {
            VisualElement Kap(Sekme s) { var d = sekmeDugme[(int)s]; return d != null ? d.Q<VisualElement>(null, "sekme-ikon-kap") : null; }
            var ka = Kap(Sekme.Atolye); atolyeNokta = null; if (ka != null) { atolyeNokta = new VisualElement(); atolyeNokta.AddToClassList("sekme-nokta"); atolyeNokta.pickingMode = PickingMode.Ignore; ka.Add(atolyeNokta); }
            // 2026-11-26 inceleme (fikir 02:06 #4): Kariyer simgesinin arkasındaki ilerleme halkası kalktı. Yeni oyuncuda yalnız gri iz
            // çiziliyordu (beyaz bantta "kilitli/basılı sekme" gibi okundu), dolgusu §8 rol tablosunda olmayan yeşildi; Kariyer A'da
            // ilerlemenin tek ölçüsü sayfanın içindeki duraklar. Alınacak bir şey olursa Atölye'nin tek nokta kalıbı (sekme-nokta).
            var ko = Kap(Sekme.Oyna); devamIsaret = null; if (ko != null) { devamIsaret = new VisualElement(); devamIsaret.AddToClassList("sekme-devam"); devamIsaret.pickingMode = PickingMode.Ignore; ko.Add(devamIsaret); for (int i = 0; i < 2; i++) { var c = new VisualElement(); c.AddToClassList("sekme-devam-cubuk"); c.pickingMode = PickingMode.Ignore; devamIsaret.Add(c); } }
            // 2026-11-26 inceleme (fikir 22:06 #5): yarım maç işareti 8 pt "II" yazısıydı (proje alt sınırı 11 pt; büyük yazıda ölçeklenip
            // simge kabından taşabilirdi, yazıyla simge taklidi). Artık sabit boy iki çubuklu duraklat simgesi; VoiceOver Oyna sekmesinde
            // "yarım kalan maç var" duyar (IsaretGuncelle, ErisimBilgi.deger).
            IsaretGuncelle();
        }
        void IsaretGuncelle()
        {
            if (atolyeNokta != null) atolyeNokta.style.display = root != null && root.YukseltAlinir(Ayar.gear.Seviye()) ? DisplayStyle.Flex : DisplayStyle.None;
            bool yarim = root != null && root.bekleyen;
            if (devamIsaret != null) devamIsaret.style.display = yarim ? DisplayStyle.Flex : DisplayStyle.None;
            var ob = sekmeDugme[(int)Sekme.Oyna] != null ? sekmeDugme[(int)Sekme.Oyna].userData as ErisimBilgi : null; if (ob != null) ob.deger = yarim ? L.T("Yarım kalan maç var") : null;
        }
        void GostergeYerlestir(bool anim)
        {
            if (gosterge == null || gosterge.parent != sekmeCubugu) return; var d = sekmeDugme[(int)gez.sekme]; if (d == null) return;
            var lb = d.layout; if (float.IsNaN(lb.width) || lb.width <= 0) return;
            // 2026-11-23 (yasin: "alt bant ... zekice düşünülmüş detaylar", detay sayfası Sn5jD8h6dR7U8MAR8DGoik): gösterge artık simge
            // arkasındaki sarı kapsülün kendisi (44 birim, .sekme-ikon-kap ile aynı); sekmeden sekmeye ease-out-back yayla kayar,
            // seçili kabın kendi dolgusu gösterge varken söner (USS .gosterge-var). Eski dikey zıplama kalktı (kapsül havaya kalkıyordu).
            float w = 44f, x = lb.x + (lb.width - w) / 2; bool az = !anim || Ayar.reduce;
            gosterge.EnableInClassList("anisiz", az); if (Mathf.Abs(gosterge.resolvedStyle.width - w) > .5f) gosterge.style.width = w;
            float once = gosterge.resolvedStyle.translate.x; gosterge.style.translate = new Translate(x, 0);
            sekmeCubugu.AddToClassList("gosterge-var");
            // 2026-11-26 (yasin: "alt bar ... animasyonlar akıcı, göz alıcı"): kapsül yola çıkarken yatay uzar, varınca yayla toplanır;
            // yeni sekmenin simgesi bir kez sıçrar. Sınıf 110 ms durur (hızlı geçiş), kalkınca taban kuralın ease-out-back'i geri
            // yaylandırır (USS "hedef durumun süresi" kalıbı). Yalnız scale, yerleşim yok; sekme değişiminde bir kez, karede değil.
            if (!az && Mathf.Abs(once - x) > 1f) { gostergeIc.AddToClassList("gosterge-ic--uza"); SekmePop(d); popIs?.Pause(); popIs = gostergeIc.schedule.Execute(PopBitir); popIs.ExecuteLater(110); }
        }
        IVisualElementScheduledItem popIs; VisualElement popIkon;
        void SekmePop(VisualElement d) { popIkon?.RemoveFromClassList("sekme-ikon--pop"); popIkon = d.Q<Image>(null, "sekme-ikon"); popIkon?.AddToClassList("sekme-ikon--pop"); }
        void PopBitir() { gostergeIc.RemoveFromClassList("gosterge-ic--uza"); if (popIkon != null) { popIkon.RemoveFromClassList("sekme-ikon--pop"); popIkon = null; } }
        public string GostergeTani { get { return gosterge == null ? "yok" : "x " + gosterge.resolvedStyle.translate.x.ToString("F0") + " w " + gosterge.resolvedStyle.width.ToString("F0") + " sekme " + gez.sekme; } }
        // Perde v2 kapanışı: menü içeriği solarken 12 pt aşağı kayar (Hud.PerdeKayma); sekme çubuğu yerinde kalır.
        float sonKayma;
        void PerdeKaymaUygula(Hud hud)
        {
            float k = hud != null ? hud.PerdeKayma : 0; if (Mathf.Abs(k - sonKayma) < .01f && !(k == 0 && sonKayma != 0)) return;
            sonKayma = k; icerik.style.translate = new Translate(0, 12 * k);
        }
        // Uçan topun çıkış noktası: düğmenin ortası, panel biriminden ekran pikseline (IMGUI düzeni, sol üst köşe).
        void PerdeKaynakYaz(VisualElement v)
        {
            var hud = root.HudKatman; if (hud == null || v == null || doc == null) return; float pw = doc.rootVisualElement.layout.width;
            if (float.IsNaN(pw) || pw <= 0 || Screen.width <= 0) return; float k = Screen.width / pw; var c = v.worldBound.center;
            hud.PerdeKaynak(new Vector2(c.x * k, c.y * k));
        }
        // Ekran turu: Oyna sekmesindeki büyük düğmeyi kaynak yap (yoksa Hud alt ortayı kullanır).
        public void PerdeKaynakCta() { if (sekmeKok[0] != null) PerdeKaynakYaz(sekmeKok[0].Q<VisualElement>(null, "cta")); }

        // Ekran okuyucu köprüsü için (Erisilebilirlik.cs TODO): görünür etiketli öğeler.
        public void ErisimAgaci(List<Erisim.Dugum> liste) { liste.Clear(); Erisim.Topla(kok, liste); }
    }
}
