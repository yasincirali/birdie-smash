// Menü kabuğu (komple-oyun tasarımı adım 1, 2026-10-26): IMGUI menüleri UI Toolkit'e taşındı; maç içi HUD, duraklat,
// maç sonu ve smaç perdesi IMGUI'de kalır (Hud; tasarım §6 adım 1 ve §7.2: HUD az öğeli, Adım 3'ten sonra taşınır).
// Ekran haritası (§2.1): alt sekme çubuğu Oyna · Kariyer · Soyunma odası · Profil; Ayarlar sekme değil, üst şeritte ⚙
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
        readonly VisualElement[] sekmeKok = new VisualElement[4]; readonly VisualElement[] sekmeDugme = new VisualElement[4];
        readonly bool[] kirli = { true, true, true, true }; bool ustKirli = true;
        readonly Gezinti gez = new Gezinti();
        float kenarX = -1; float K = 1; Rect sonSafe; float sonPanelW = -1, sonOrtu = -1; bool sonGorunur = true, sonReduce, kalkanAcik, kuruldu; string sonDil;
        static readonly string[] SEKME_AD = { "Oyna", "Kariyer", "Soyunma odası", "Profil" };

        public Sekme AktifSekme { get { return gez.sekme; } }
        // Gizli kare hızı göstergesinin ekran adı (Hud.FpsEkran; HTML ile aynı adlar, çevrilmez).
        public string FpsAdi
        {
            get
            {
                string u = gez.Ust; if (u == Gezinti.AYAR) return "Ayarlar"; if (u == Gezinti.NASIL) return "Nasıl oynanır"; if (u == Gezinti.LISANS) return "Lisanslar"; if (u == Gezinti.HIZLI) return "Hızlı maç"; if (u == Gezinti.KART) return "Maç kartı"; if (u == Gezinti.YUVA) return "Yuva"; if (u == Gezinti.ANTRENMAN) return "Antrenman"; if (u == Gezinti.DRIL) return "Görev kartı"; if (u == Gezinti.GUNLUK) return "Günün maçı";
                if (gez.sekme == Sekme.Soyunma) return root.vitrin.hedefGec >= 1 ? "Raket 360°" : soyunmaSeg == 1 ? "Tarz" : soyunmaSeg == 2 ? "Görünüm" : soyunmaSeg == SEG_KARAKTER ? "Karakter" : "Ekipman";
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
            zemin = kok.Q<VisualElement>("zemin"); alan = kok.Q<VisualElement>("alan"); ust = kok.Q<VisualElement>("ust"); icerik = kok.Q<VisualElement>("icerik");
            sayfa = kok.Q<VisualElement>("sayfa"); sekmeCubugu = kok.Q<VisualElement>("sekmeler"); altKatman = kok.Q<VisualElement>("alt-katman"); kalkan = kok.Q<VisualElement>("kalkan");
            // 2026-10-26 Yayın Gecesi: gövde yazı tipi Barlow (Resources/fonts, OFL). Sınıf kuralları (menu_stil.uss) kalın ve başlık
            // dosyalarını seçer; kökteki satır içi değer, USS çözülemezse ya da tema yoksa da Barlow'a düşülsün diye.
            var barlow = Resources.Load<Font>("fonts/Barlow-Medium");
            if (barlow != null) kok.style.unityFontDefinition = FontDefinition.FromFont(barlow);
            else if (temaYok)
            {   // tema yoksa yazı tipi de yok: yerleşik çalışma zamanı yazı tipi (Unity 2022.2+ adı LegacyRuntime.ttf)
                try { var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (f != null) kok.style.unityFontDefinition = FontDefinition.FromFont(f); } catch (System.Exception) { }
            }
            ZeminKur();
            for (int i = 0; i < 4; i++) { sekmeKok[i] = new VisualElement(); sekmeKok[i].AddToClassList("sekme-kok"); sekmeKok[i].AddToClassList("tam"); icerik.Add(sekmeKok[i]); }
            altKatman.RegisterCallback<PointerDownEvent>(e => { if (e.target == altKatman) AltKapat(); });
            kalkan.RegisterCallback<PointerDownEvent>(e => { if (root.HudKatman != null) root.HudKatman.PerdeAtla(); e.StopPropagation(); });
            // iOS sol kenardan kaydırma = Geri (tam sayfada; maçta yok, menü maçta gizli). Kenar 24 pt, 70 pt sağa çekiş.
            sayfa.RegisterCallback<PointerDownEvent>(e => { kenarX = e.position.x - sayfa.worldBound.x < 24 ? e.position.x : -1; }, TrickleDown.TrickleDown);
            sayfa.RegisterCallback<PointerMoveEvent>(e => { if (kenarX >= 0 && e.position.x - kenarX > 70) { kenarX = -1; Geri(); } }, TrickleDown.TrickleDown);
            sayfa.RegisterCallback<PointerUpEvent>(e => kenarX = -1, TrickleDown.TrickleDown);
            DuyarliGuncelle();   // 2026-10-27: boyut sınıfı (kısa / geniş) ilk kurulumdan önce: kodda yazılan boyutlar ona bakar (Duyarli.cs)
            HepsiniKur(); kuruldu = true;
        }

        // UXML yoksa aynı ağaç (adlar menu.uxml ile aynı).
        VisualElement KodIskelet()
        {
            var k = Y("kok", "kok tam", null); Y("zemin", "tam", k).pickingMode = PickingMode.Ignore;
            var g = Y("govde", "govde tam", k); var a = Y("alan", "alan", g); Y("ust", "ust-serit", a); Y("icerik", "icerik", a); Y("sayfa", "sayfa tam", a);
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
            if (!gor) { if (root.afis.Acik) root.afis.Kapat(); SahneCiz(true, false); return; }
            if (!temaFontVar) TemaFontOku();
            float k = Ayar.MenuYaziK; if (Mathf.Abs(k - K) > .001f || sonDil != Ayar.dil) { K = k; sonDil = Ayar.dil; HepsiniKur(); }
            if (Ayar.reduce != sonReduce) { sonReduce = Ayar.reduce; kok.EnableInClassList("az-hareket", sonReduce); }
            GuvenliAlan();
            var hud = root.HudKatman; float o = hud != null ? hud.PerdeOrtu : 0; bool pv = hud != null && hud.PerdeVar;
            if (Mathf.Abs(o - sonOrtu) > .01f || (o == 0 && sonOrtu != 0)) { sonOrtu = o; kok.style.opacity = 1 - o; }
            if (pv != kalkanAcik) { kalkanAcik = pv; kalkan.style.display = pv ? DisplayStyle.Flex : DisplayStyle.None; if (!pv) salonKirli = true; }   // perde kalkınca fon bir kez daha (açılışta sahne perde altında kuruluyor olabilir)
            SahneCiz(pv || o > 0, true);   // perde açılırken/kapanırken menü solar: altında maç sahnesi görünmeli
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
            altBosluk = Mathf.Max(0, sa.yMin) * s; sekmeCubugu.style.paddingBottom = altBosluk; if (altSayfa != null) altSayfa.style.paddingBottom = altBosluk + 16;
            SutunYaz(pw);
        }

        // ---- gezinti ----
        public void SekmeAc(Sekme s) { var once = gez.sekme; bool d = gez.SekmeSec(s); if (d) Yerel.Hap("tick"); Uygula(once, d); }
        public void AyarlarAc() { var once = gez.sekme; gez.Ac(Gezinti.AYAR); Uygula(once, true); }
        public void HizliMacAc() { var once = gez.sekme; gez.Ac(Gezinti.HIZLI); Uygula(once, true); }
        // Kariyer haritası: düğüme dokununca maç kartı (alt sayfa); tur seçici (EkranTuru da kullanır).
        public void MacKartAc(int g) { var once = gez.sekme; kartBasamak = g; gez.Ac(Gezinti.KART); Uygula(once, true); }
        public void KariyerTurSec(int t) { kariyerTur = t; kirli[(int)Sekme.Kariyer] = true; if (gez.sekme == Sekme.Kariyer && gez.Ust == null) SekmeKur(Sekme.Kariyer); }
        void SayfaAc(string s) { var once = gez.sekme; gez.Ac(s); Uygula(once, true); }
        public void Geri() { var once = gez.sekme; if (gez.Geri()) { Yerel.Hap("tick"); Uygula(once, true); } }
        void AltKapat() { var once = gez.sekme; gez.AltKapat(); Uygula(once, false); }
        // Maçtan ana menüye dönüş (GameRoot.AnaMenu): açık sayfalar kapanır, Oyna sekmesi.
        public void MenuyeDon() { var once = gez.sekme; kariyerTur = -1; gez.SekmeSec(Sekme.Oyna); Uygula(once, false); Kirlet(); }   // harita yeniden sıradakinin turunu açar (yeni tur açılmış olabilir)

        void Uygula(Sekme once, bool anim)
        {
            if (kok == null || sekmeKok[0] == null) return;   // iskelet kurulmadan çağrıldı (gezinti durumu yine de güncel)
            var s = gez.sekme; string ustS = gez.Ust; bool tamSayfa = ustS != null && !Gezinti.AltSayfa(ustS);
            // Yığındaki son tam sayfa (alt sayfa açıkken altında duran)
            string tam = null; if (tamSayfa) tam = ustS; else if (gez.Derinlik > 1) tam = gez.Acik(Gezinti.LISANS) ? Gezinti.LISANS : gez.Acik(Gezinti.NASIL) ? Gezinti.NASIL : gez.Acik(Gezinti.AYAR) ? Gezinti.AYAR : gez.Acik(Gezinti.ANTRENMAN) ? Gezinti.ANTRENMAN : null;
            for (int i = 0; i < 4; i++)
            {
                bool on = (int)s == i; sekmeKok[i].style.display = on && tam == null ? DisplayStyle.Flex : DisplayStyle.None;
                if (sekmeDugme[i] != null)
                {
                    sekmeDugme[i].EnableInClassList("sekme--secili", on); var b = sekmeDugme[i].userData as ErisimBilgi; if (b != null) b.secili = on;
                    var im = sekmeDugme[i].Q<Image>(null, "sekme-ikon"); if (im != null) im.tintColor = on ? KOYU : SOLUK;   // seçili: limon dolgu üstünde koyu simge
                }
            }
            ust.style.display = tam == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (tam != null) { SayfaKur(tam); sayfa.style.display = DisplayStyle.Flex; if (anim) Giris(sayfa); } else { sayfa.style.display = DisplayStyle.None; sayfa.Clear(); }
            string altS = gez.AcikAltSayfa;
            if (altS != null) { AltSayfaYenidenKur(altS); altKatman.style.display = DisplayStyle.Flex; if (anim) Giris(altSayfa); } else { altKatman.style.display = DisplayStyle.None; altKatman.Clear(); altSayfa = null; }
            bool koyu = s != Sekme.Oyna || tam != null; kok.EnableInClassList("koyu", koyu);
            // 2026-10-26 gece: kök her sekmede opak (Oyna: mürekkep, salon fonu üstünde; diğerleri yön A a-bgnight #0B1130), tam
            // sayfa mürekkep. Satır içi (USS'te değil): r24'te USS'teki değişiklikle bütün sayfa biçimsiz çıktı, kök neden
            // ayrılana kadar zemin koddan (eski USS değerleri saydamdı; satır içi değer onları ezer).
            kok.style.backgroundColor = koyu ? GECE : INK_; sayfa.style.backgroundColor = INK_;
            // Vitrin kamerası yalnız Soyunma odası görünürken çizer (bütçe §7.3); çıkınca sahnedeki oyuncu yeni görünüme geçer.
            bool vitrinGerek = s == Sekme.Soyunma && tam == null && root.ekran == Ekran.Menu;
            if (!vitrinGerek && root.vitrin.gameObject.activeSelf) { root.vitrin.Kapat(); if (root.ekran == Ekran.Menu) root.GorunumDegisti(); }
            else if (vitrinGerek && !root.vitrin.gameObject.activeSelf) { root.vitrin.Ac(); kirli[(int)Sekme.Soyunma] = true; }   // Kapat raket görünümünü sıfırladı: gövde ona göre
            // 2026-10-26 Yayın Gecesi: Oyna'nın VS afişi yalnız Oyna görünürken çizer (Vitrin ile aynı kural).
            AfisGuncelle(s == Sekme.Oyna && tam == null && root.ekran == Ekran.Menu);
            if (tam == null && kirli[(int)s]) SekmeKur(s);
            if (tam == null && s != once && anim) Giris(sekmeKok[(int)s]);
            if (ustKirli) UstKur();
        }

        // Giriş: 24 pt sağdan, solarak, .22 sn (HTML pIn ile aynı); hareketi azaltta USS geçişi 0 sn, yalnız görünür olur.
        void Giris(VisualElement v)
        {
            if (v == null || Ayar.reduce) return; v.AddToClassList("giris");
            v.schedule.Execute(() => v.RemoveFromClassList("giris")).StartingIn(33);
        }

        // Durum değişti: hepsi kirli, görünen hemen yeniden kurulur.
        public void Kirlet() { for (int i = 0; i < 4; i++) kirli[i] = true; ustKirli = true; var once = gez.sekme; Uygula(once, false); }

        void HepsiniKur()
        {
            sekmeCubugu.Clear(); kok.EnableInClassList("buyuk-yazi", K >= 1.35f);   // büyük yazıda çipler ve ızgaralar alt alta (USS)
            bool etiket = K <= 1.5f;   // ×1,5 üstünde sekme etiketi kalkar, simge + ekran okuyucu etiketi kalır (§2.4)
            var ik = new[] { Ikonlar.Oyna, Ikonlar.Kupa, Ikonlar.Raket, Ikonlar.Kisi };
            for (int i = 0; i < 4; i++)
            {
                var s = (Sekme)i; var d = new VisualElement(); d.AddToClassList("sekme"); sekmeDugme[i] = d;
                var cizgi = new VisualElement(); cizgi.AddToClassList("sekme-cizgi"); cizgi.pickingMode = PickingMode.Ignore; d.Add(cizgi);
                var im = Ikon(ik[i], 22, s == gez.sekme ? KOYU : SOLUK); im.AddToClassList("sekme-ikon"); d.Add(im);
                string ad = L.T(SEKME_AD[i]); if (etiket) d.Add(Yazi(ad, 11, "sekme-yazi"));
                d.AddManipulator(new Clickable(() => SekmeAc(s))); Erisim.Etiket(d, ad, "sekme", s == gez.sekme);
                sekmeCubugu.Add(d);
            }
            if (altBosluk > 0) sekmeCubugu.style.paddingBottom = altBosluk;
            for (int i = 0; i < 4; i++) kirli[i] = true; ustKirli = true; Uygula(gez.sekme, false);
        }

        void SekmeKur(Sekme s)
        {
            var k = sekmeKok[(int)s]; kirli[(int)s] = false;
            switch (s)
            {
                case Sekme.Oyna: OynaKur(k); break;
                case Sekme.Kariyer: KariyerKur(k); break;
                case Sekme.Soyunma: SoyunmaKur(k); break;
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
            var av = new VisualElement(); av.AddToClassList("ust-avatar"); av.pickingMode = PickingMode.Ignore; av.style.backgroundColor = Kit.Hex(Ayar.FORMA); av.Add(Ikon(Ikonlar.Kisi, 24, KOYU)); hap.Add(av);
            var sv = new VisualElement(); sv.AddToClassList("ust-sv"); sv.pickingMode = PickingMode.Ignore; hap.Add(sv);
            sv.Add(Yazi(L.T("Sv {0}", L_), 18, "rakam"));
            var c = Cubuk(gerek > 0 ? kalan / (float)gerek : 1, LIMON, 5); c.AddToClassList("ust-xp"); sv.Add(c);
            hap.AddManipulator(new Clickable(() => SekmeAc(Sekme.Profil)));
            Erisim.Etiket(hap, L.T("Seviye {0}", L_) + (gerek > 0 ? ", " + L.T("Sonraki seviyeye {0} XP", gerek - kalan) : "") + ", " + L.T("Profil"), "dugme");
            var tuy = new VisualElement(); tuy.AddToClassList("ust-hap"); tuy.AddToClassList("tuy-hap"); tuy.pickingMode = PickingMode.Ignore; ust.Add(tuy);
            tuy.Add(Ikon(Ikonlar.Tuy, 20, ALTIN)); var tz = Yazi(Sayi(gr.eko.tuy), 20, "rakam"); tz.AddToClassList("ust-tuy"); tuy.Add(tz);
            Erisim.Etiket(tuy, L.T("{0} Tüy", gr.eko.tuy), "metin");
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
            // 2026-10-26 gece: en arkada bulanık salon görüntüsü (SalonCek); canlı sahne menünün arkasında artık çizilmez.
            salonImg = new Image { scaleMode = ScaleMode.ScaleAndCrop, tintColor = new Color(.78f, .8f, .9f, 1) }; salonImg.AddToClassList("tam"); salonImg.AddToClassList("salon"); salonImg.pickingMode = PickingMode.Ignore; zemin.Add(salonImg);
            var fl = new Image { image = Ikonlar.Isik, scaleMode = ScaleMode.StretchToFill, tintColor = new Color(LIMON.r, LIMON.g, LIMON.b, .32f) }; fl.AddToClassList("isik-sol"); fl.pickingMode = PickingMode.Ignore; zemin.Add(fl);
            var fr = new Image { image = Ikonlar.Isik, scaleMode = ScaleMode.StretchToFill, tintColor = new Color(GOK.r, GOK.g, GOK.b, .30f) }; fr.AddToClassList("isik-sag"); fr.pickingMode = PickingMode.Ignore; zemin.Add(fr);
            var zi = new Image { image = Ikonlar.Gradyan, scaleMode = ScaleMode.StretchToFill }; zi.pickingMode = PickingMode.Ignore; zi.AddToClassList("tam"); zemin.Add(zi);
            afisImg = new Image { image = root.afis.rt, scaleMode = ScaleMode.ScaleAndCrop }; afisImg.AddToClassList("afis"); afisImg.pickingMode = PickingMode.Ignore; zemin.Add(afisImg);
            vsYazi = new Label("VS"); vsYazi.AddToClassList("yazi"); vsYazi.AddToClassList("vs"); vsYazi.pickingMode = PickingMode.Ignore; zemin.Add(vsYazi);
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
            if (menu && sekmeKok[0] != null && gez.sekme == Sekme.Oyna)
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
        static readonly Color GECE = new Color(11 / 255f, 17 / 255f, 48 / 255f, 1), INK_ = new Color(7 / 255f, 11 / 255f, 31 / 255f, 1);
        void OnDestroy() { if (salonRt != null) { salonRt.Release(); Destroy(salonRt); } }

        // Tam sayfa başı: "‹ Geri" (44 pt) + başlık.
        Label SayfaBasi(VisualElement ebeveyn, string baslik)
        {
            var bas = new VisualElement(); bas.AddToClassList("sayfa-basi"); ebeveyn.Add(bas);
            var g = new VisualElement(); g.AddToClassList("dugme"); g.AddToClassList("geri-dugme");
            g.Add(Ikon(Ikonlar.Geri, 18, Color.white)); g.Add(Yazi(L.T("Geri"), 15, "kalin")); g.AddManipulator(new Clickable(Geri)); Erisim.Etiket(g, L.T("Geri"), "dugme"); bas.Add(g);
            var t = Yazi(L.T(baslik), 19, "sayfa-baslik"); t.pickingMode = PickingMode.Position; Erisim.Etiket(t, L.T(baslik), "baslik"); bas.Add(t);
            return t;
        }

        void SayfaKur(string s)
        {
            sayfa.Clear();
            if (s == Gezinti.AYAR) AyarlarKur(sayfa); else if (s == Gezinti.NASIL) NasilKur(sayfa); else if (s == Gezinti.LISANS) LisansKur(sayfa); else if (s == Gezinti.ANTRENMAN) AntrenmanKur(sayfa);
        }

        void Tik() { root.sfx.Tik(); Yerel.Hap("tick"); }

        // Ekran okuyucu köprüsü için (Erisilebilirlik.cs TODO): görünür etiketli öğeler.
        public void ErisimAgaci(List<Erisim.Dugum> liste) { liste.Clear(); Erisim.Topla(kok, liste); }
    }
}
