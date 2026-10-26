// Menü kabuğu (komple-oyun tasarımı adım 1, 2026-10-26): IMGUI menüleri UI Toolkit'e taşındı; maç içi HUD, duraklat,
// maç sonu ve smaç perdesi IMGUI'de kalır (Hud; tasarım §6 adım 1 ve §7.2: HUD az öğeli, Adım 3'ten sonra taşınır).
// Ekran haritası (§2.1): alt sekme çubuğu Oyna · Kariyer · Soyunma odası · Profil; Ayarlar sekme değil, üst şeritte ⚙
// (oyuncu oturumda bir kez bile girmez). Ayarlar › Nasıl oynanır / Lisanslar tam sayfa, Hızlı maç kurulumu alt sayfa.
// Gezinti kuralı Core/Gezinti'de (saf C#, test edilir); burası yalnız görünüm.
// Kurulum çalışma zamanında: UIDocument + PanelSettings (Resources/ui/PanelSettings varsa o; Editor'de DemoKurulum üretir,
// shader başvurusu için; yoksa ScriptableObject.CreateInstance), iskelet Resources/ui/menu.uxml (yoksa koddan, aynı adlarla), biçim Resources/ui/menu.uss,
// varsayılan tema Resources/ui/tema.tss (yoksa yerleşik yazı tipine düşülür). Editor API'si yok: sahne dosyasız kurulum
// (GameRoot gibi) her Unity sürümünde aynı sonucu versin.
// Ölçek: ScaleWithScreenSize, referans 375 pt genişlik (iPhone SE/mini). Neden ConstantPhysicalSize değil (tasarım Ek A.7
// öyle diyordu): Screen.dpi 3× iPhone'larda yuvarlanmış, CI'daki xvfb'de 0 geliyor; genişliğe göre ölçek her telefonda
// panel birimini ≥ 1 pt yapar, böylece 44 birimlik düğme en dar telefonda da ≥ 44 pt kalır. Geniş ekranda (iPad) yüksekliğe
// göre ölçeklenir.
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
                if (gez.sekme == Sekme.Soyunma) return root.vitrin.hedefGec >= 1 ? "Raket 360°" : soyunmaSeg == 1 ? "Tarz" : soyunmaSeg == 2 ? "Görünüm" : "Ekipman";
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
            var uss = Resources.Load<StyleSheet>("ui/menu"); if (uss != null) r.styleSheets.Add(uss); else Debug.LogWarning("Birdie Smash: Resources/ui/menu.uss yok");
            kok = r.Q<VisualElement>("kok");
            if (kok == null) { kok = KodIskelet(); r.Add(kok); }
            zemin = kok.Q<VisualElement>("zemin"); alan = kok.Q<VisualElement>("alan"); ust = kok.Q<VisualElement>("ust"); icerik = kok.Q<VisualElement>("icerik");
            sayfa = kok.Q<VisualElement>("sayfa"); sekmeCubugu = kok.Q<VisualElement>("sekmeler"); altKatman = kok.Q<VisualElement>("alt-katman"); kalkan = kok.Q<VisualElement>("kalkan");
            if (temaYok)
            {   // tema yoksa yazı tipi de yok: yerleşik çalışma zamanı yazı tipi (Unity 2022.2+ adı LegacyRuntime.ttf)
                try { var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (f != null) kok.style.unityFontDefinition = FontDefinition.FromFont(f); } catch (System.Exception) { }
            }
            var zi = new Image { image = Ikonlar.Gradyan, scaleMode = ScaleMode.StretchToFill }; zi.pickingMode = PickingMode.Ignore; zi.AddToClassList("tam"); zemin.Add(zi);
            for (int i = 0; i < 4; i++) { sekmeKok[i] = new VisualElement(); sekmeKok[i].AddToClassList("sekme-kok"); sekmeKok[i].AddToClassList("tam"); icerik.Add(sekmeKok[i]); }
            altKatman.RegisterCallback<PointerDownEvent>(e => { if (e.target == altKatman) AltKapat(); });
            kalkan.RegisterCallback<PointerDownEvent>(e => { if (root.HudKatman != null) root.HudKatman.PerdeAtla(); e.StopPropagation(); });
            // iOS sol kenardan kaydırma = Geri (tam sayfada; maçta yok, menü maçta gizli). Kenar 24 pt, 70 pt sağa çekiş.
            sayfa.RegisterCallback<PointerDownEvent>(e => { kenarX = e.position.x - sayfa.worldBound.x < 24 ? e.position.x : -1; }, TrickleDown.TrickleDown);
            sayfa.RegisterCallback<PointerMoveEvent>(e => { if (kenarX >= 0 && e.position.x - kenarX > 70) { kenarX = -1; Geri(); } }, TrickleDown.TrickleDown);
            sayfa.RegisterCallback<PointerUpEvent>(e => kenarX = -1, TrickleDown.TrickleDown);
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
                if (gor) Kirlet();   // maçta Tüy, XP, kariyer değişmiş olabilir; vitrin Uygula'da açılır
            }
            if (!gor) return;
            float k = Ayar.MenuYaziK; if (Mathf.Abs(k - K) > .001f || sonDil != Ayar.dil) { K = k; sonDil = Ayar.dil; HepsiniKur(); }
            if (Ayar.reduce != sonReduce) { sonReduce = Ayar.reduce; kok.EnableInClassList("az-hareket", sonReduce); }
            GuvenliAlan();
            var hud = root.HudKatman; float o = hud != null ? hud.PerdeOrtu : 0; bool pv = hud != null && hud.PerdeVar;
            if (Mathf.Abs(o - sonOrtu) > .01f || (o == 0 && sonOrtu != 0)) { sonOrtu = o; kok.style.opacity = 1 - o; }
            if (pv != kalkanAcik) { kalkanAcik = pv; kalkan.style.display = pv ? DisplayStyle.Flex : DisplayStyle.None; }
            if (Input.GetKeyDown(KeyCode.Escape) && !pv) Geri();   // Android geri tuşu
        }

        // Güvenli alan (Screen.safeArea) panel birimine: kök genişliği / ekran genişliği. Üst ve yanlar kökte, alt sekme
        // çubuğunda ve alt sayfada (zeminleri ev çizgisinin altına uzansın, düğmeler uzanmasın).
        float altBosluk;
        void GuvenliAlan()
        {
            float pw = doc.rootVisualElement.layout.width; if (float.IsNaN(pw) || pw <= 0 || Screen.width <= 0) return;
            var sa = Screen.safeArea; if (sa == sonSafe && Mathf.Abs(pw - sonPanelW) < .5f) return; sonSafe = sa; sonPanelW = pw;
            float s = pw / Screen.width;
            kok.style.paddingTop = Mathf.Max(0, Screen.height - sa.yMax) * s; kok.style.paddingLeft = Mathf.Max(0, sa.xMin) * s; kok.style.paddingRight = Mathf.Max(0, Screen.width - sa.xMax) * s;
            altBosluk = Mathf.Max(0, sa.yMin) * s; sekmeCubugu.style.paddingBottom = altBosluk; if (altSayfa != null) altSayfa.style.paddingBottom = altBosluk + 16;
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
                if (sekmeDugme[i] != null) { sekmeDugme[i].EnableInClassList("sekme--secili", on); var b = sekmeDugme[i].userData as ErisimBilgi; if (b != null) b.secili = on; }
            }
            ust.style.display = tam == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (tam != null) { SayfaKur(tam); sayfa.style.display = DisplayStyle.Flex; if (anim) Giris(sayfa); } else { sayfa.style.display = DisplayStyle.None; sayfa.Clear(); }
            string altS = gez.AcikAltSayfa;
            if (altS != null) { AltSayfaYenidenKur(altS); altKatman.style.display = DisplayStyle.Flex; if (anim) Giris(altSayfa); } else { altKatman.style.display = DisplayStyle.None; altKatman.Clear(); altSayfa = null; }
            kok.EnableInClassList("koyu", s != Sekme.Oyna || tam != null);
            // Vitrin kamerası yalnız Soyunma odası görünürken çizer (bütçe §7.3); çıkınca sahnedeki oyuncu yeni görünüme geçer.
            bool vitrinGerek = s == Sekme.Soyunma && tam == null && root.ekran == Ekran.Menu;
            if (!vitrinGerek && root.vitrin.gameObject.activeSelf) { root.vitrin.Kapat(); if (root.ekran == Ekran.Menu) root.GorunumDegisti(); }
            else if (vitrinGerek && !root.vitrin.gameObject.activeSelf) { root.vitrin.Ac(); kirli[(int)Sekme.Soyunma] = true; }   // Kapat raket görünümünü sıfırladı: gövde ona göre
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
                var im = Ikon(ik[i], 24, Color.white); im.AddToClassList("sekme-ikon"); d.Add(im);
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
        void UstKur()
        {
            ustKirli = false; ust.Clear(); var gr = Ayar.gear; int L_, kalan, gerek; gr.OyuncuSv(out L_, out kalan, out gerek);
            var sol = new VisualElement(); sol.AddToClassList("ust-sol"); sol.pickingMode = PickingMode.Ignore; ust.Add(sol);
            sol.Add(Yazi(L.T("Seviye {0}", L_), 15, "kalin"));
            var c = Cubuk(gerek > 0 ? kalan / (float)gerek : 1, CAMGOBEGI, 5); c.AddToClassList("ust-xp"); sol.Add(c);
            Erisim.Etiket(sol, L.T("Seviye {0}", L_) + (gerek > 0 ? ", " + L.T("Sonraki seviyeye {0} XP", gerek - kalan) : ""), "metin");
            var tuy = Yazi(L.T("{0} Tüy", gr.eko.tuy), 15, "ust-tuy"); ust.Add(tuy); Erisim.Etiket(tuy, L.T("{0} Tüy", gr.eko.tuy), "metin");
            var ayar = new VisualElement(); ayar.AddToClassList("dugme"); ayar.AddToClassList("ikon-dugme"); ayar.Add(Ikon(Ikonlar.Disli, 24, Color.white));
            ayar.AddManipulator(new Clickable(AyarlarAc)); Erisim.Etiket(ayar, L.T("Ayarlar"), "dugme"); ust.Add(ayar);
        }

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
