// Sahnenin tamamı koddan kurulur: kamera, ışık, kort (saha teması), tribün, hakemler, file, top, iki oyuncu, ses, HUD.
// Akış: menü → perde (smaç perdesi kapanır, maç kurulur, açılır) → (ilk açılışta 3 adımlık antrenman → 5 sayılık Kolay
// ilk maç) → maç → maç sonu (Tüy/XP ödülü). Girdi (kaydırma) → Match (saf C#) → olaylar (vuruş, file, sayı, görev) →
// görünüm ve "his" (vuruş duraklaması Match'te; kamera sarsıntısı/zoom, ekran parlaması, tüy/kıvılcım, dalga, yer izi, ses,
// titreşim burada). Hareketi azalt açıkken sarsıntı, zoom ve vuruş duraklaması yok, patlamalar üçte bir, dalga yok.
// 2026-10-25 eşitlik turu (yasin: "html de yapılan her feature unity dönüşümünde de içermeli"): titreşim desenleri
// (Yerel), Game Center başarımları (Basarim), açılış perdesi ve dönen tüy top (Yukleyici; ses ısınması bitene kadar),
// "Bir maç daha"/"Yeni maç" da perdeyle, ana menüye çıkınca maç bekler ("Maça devam et"), duraklatınca tüm ses susar,
// sayı kazanınca üç renkli kıvılcım ve düşüş yerinde toz + dalga (HTML point).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public enum Ekran { Menu, Oyun, Bitti }
    // 2026-10-26 adım 7–9: maçın türü (maç sonu ve "Bir maç daha" buna göre). Normal = kariyer ya da Hızlı maç (Ayar.kariyerModu),
    // Dril = antrenman görevi (Core/Dril), Gunluk = Günün maçı (Core/Gunluk), Usta = Usta yolu (Core/UstaYolu).
    public enum Mod { Normal, Dril, Gunluk, Usta }

    public class GameRoot : MonoBehaviour
    {
        public Match match; public Camera cam; public Ekran ekran = Ekran.Menu; public bool paused;
        public string bittiBaslik, bittiNot, bittiBilgi, bittiDugme, bittiIpucu, bittiYildiz; public List<Ozet.Kart> bittiKart = new List<Ozet.Kart>(); public bool bittiIlk; public float rakipKartT;
        // Geri sayım (2026-10-30, yasin "1 ok" + "maç başlarken de bir geri sayım"): maç başında ve ralli sürerken duraklatmadan
        // dönüşte oyun 3-2-1 boyunca donuk durur, top yerinde görünür. Toplam 1,2 sn: maç başında rakip kartıyla (1,5 sn) üst
        // üste biner, maç başını uzatmaz. Antrenmanda ve öğretici maçta yok; sayı arasındaki duraklatmadan dönüşte yok (top yok).
        public const float GERI = 1.2f; public float geriT; public static bool geriKapali;   // geriKapali: yalnız ekran turu (kare sayımları geri sayımsız)
        PlayerView me, op, hk1, hk2; public ShuttleView shuttle; NetView net; public Sfx sfx; Hud hud; public Vitrin vitrin; public Afis afis; public Fx fx; public Yukleyici yukleyici;
        public bool bekleyen;   // duraklatılmış maç ana menüde bekliyor (HTML ekran(): "Maça devam et")
        // Menü kabuğu (2026-10-26, UI Toolkit; Game/UI/UiKok). Hud maç içi + perde için kalır; menü perdeyi buradan okur.
        public UiKok ui; public Hud HudKatman { get { return hud; } }
        GameObject court; Renderer akisR; string courtId, modelId, gorunumId;
        public OdulSonuc odul; public bool yukseltHazir; bool pencere; float sqT; float psvx, psvy;
        float shake, punch; Vector3 camBase; Quaternion camRot; int seed = 1; Light sun; readonly System.Random rnd = new System.Random();
        bool ogrKayit;
        public Mod mod = Mod.Normal; public string drilId; public GunlukMac gunluk;
        // Maç sonu ekranının mod farkları: skor satırı (görevde "7/12 hedefte"), önerilen antrenman görevi (kayıpta, Dril.Oner).
        public string bittiSkor, bittiDril;
        // 2026-10-26 maç sonu yön A (Hud.Over): ekranın parçaları ayrı alanlarda (eskiden tek başlık + yıldız dizgesi). Başlık
        // (GALİBİYET), üst satır (tur · basamak · yer), yıldız maskesi (kalıcı, -1 = yıldızsız mod) ve bu maçta yeni gelenler,
        // her yıldızın koşulu, açılan şey (ilk galibiyetin notu), büyük düğmenin alt satırı (sıradaki basamak ve rakip).
        // Eski alanlar (bittiBaslik, bittiYildiz) yazılmaya devam eder: başka okuyan yoksa da ekran turu raporu ve geri dönüş için.
        public bool bittiKazandi; public int bittiMaske = -1, bittiYeniMaske, bittiYildizN = 3, bittiG = -1;
        public string bittiBas, bittiUst, bittiAltNot, bittiAcilan, bittiDugmeAlt; public string[] bittiKosul;
        void BittiSifirla() { bittiKazandi = false; bittiMaske = -1; bittiYeniMaske = 0; bittiYildizN = 3; bittiG = -1; bittiBas = bittiUst = bittiAltNot = bittiAcilan = bittiDugmeAlt = null; bittiKosul = null; }
        // Karşılaşılan basamak (g) için üst satır ve sonraki basamağın düğme alt satırı.
        void BittiKariyer(int g, bool won, Match M)
        {
            bittiG = g; var kb = KariyerVeri.Al(g);
            bittiUst = L.T(UiKok.TurAd(KariyerVeri.TurOf(g))) + " · " + (KariyerVeri.BasOf(g) + 1) + "/" + KariyerVeri.BASAMAK + " · " + L.T(kb.yer);
            var gv = M.BasamakGorevi;
            bittiKosul = new[] { L.T("Kazan"), gv != null ? L.Cumle(gv.ad) : L.T("Basamak görevi"), L.T("Rakip ≤ {0}", Ilerleme.FarkSiniri(M.target)) };
            var p = Ayar.ilerleme; int n = won && !p.HepsiBitti ? p.Siradaki() : g;
            if (KariyerVeri.Var(n)) { var nk = KariyerVeri.Al(n); bittiDugmeAlt = L.T(nk.yer) + " · " + Match.STIL[nk.stil].isim; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (FindFirstObjectByType<GameRoot>() == null) new GameObject("BirdieSmash").AddComponent<GameRoot>(); }

        void Awake()
        {
            Application.targetFrameRate = 60; Uyanik(false); Ayar.Yukle();
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c.gameObject.scene.IsValid() && c.transform.root != transform) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.transform.root != transform) l.gameObject.SetActive(false);
            var cg = new GameObject("kamera"); cg.transform.SetParent(transform, false); cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.nearClipPlane = .1f; cam.farClipPlane = 80;
            // Yayın ışığı: anahtar ışık kısık (kortun dışı kararsın), dolgu ortamı koyu; kort üstünü CourtView'in tavan lambaları açar.
            var lg = new GameObject("isik"); lg.transform.SetParent(transform, false); sun = lg.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.intensity = .75f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .5f; lg.transform.rotation = Quaternion.Euler(62, -24, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Kit.Hex("#6f8590"); RenderSettings.ambientEquatorColor = Kit.Hex("#3b4c55"); RenderSettings.ambientGroundColor = Kit.Hex("#1a2420");
            QualitySettings.shadowDistance = 40;
            // Kenar yumuşatma (2026-10-26, yasin "saha nizami çizilmeli"): 4 cm'lik saha çizgileri uzak yarıda 1 pikselin altına
            // iniyor, MSAA yokken kırık/kesik görünüyordu. MSAA, gölge ve çözünürlük cihaza göre Kalite'de.
            Kalite.Kur(gameObject, cam);
            shuttle = ShuttleView.Build(transform); shuttle.cam = cam; sfx = Sfx.Build(transform, cg.transform);
            vitrin = Vitrin.Build(transform); afis = Afis.Build(transform); fx = Fx.Build(transform, cam); yukleyici = Yukleyici.Build(transform);
            hud = gameObject.AddComponent<Hud>(); hud.root = this; hud.Acilis();
            gameObject.AddComponent<KadrajIsaret>().root = this;   // yüksek top: ekran dışı işareti (Core/Kadraj)
            sfx.menude = () => ekran == Ekran.Menu && !hud.PerdeKapali;   // menü müziği: perde kapanırken söner, maçta yok
            sfx.sakin = () => hud.PerdeKapali || ekran != Ekran.Oyun || paused || match == null || match.between || match.s == null;
            Basarim.Esitle(Ayar.kariyer, Ayar.bestRally); Basarim.Giris(Ayar.kariyer);
            match = new Match(1, new MatchOptions { easy = Ayar.easy, target = Ayar.target, saha = Ayar.saha });   // menü arkasında duran sahne
            Kur(match);
            AudioListener.volume = Ayar.ses ? 1 : 0;
            ui = UiKok.Kur(this);   // sahne ve Ayar hazır olduktan sonra: ilk kurulum Ayar ve vitrini okur
        }

        // Ekran yalnız maçta/antrenmanda açık kalır (Ar-Ge 25 Eki #3): menüde, duraklatmada ve maç sonunda sistem ayarı
        // geçerli; masada unutulan telefon kararır, pil bitmez. Oyun sırasında dokunuş seyrek olabildiği için orada kapalı.
        static void Uyanik(bool acik) { Screen.sleepTimeout = acik ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting; }

        // Sahneyi maça göre kur: saha değiştiyse kort, model değiştiyse oyuncular ve hakemler, rakip forması her maçta.
        void Kur(Match M)
        {
            if (court == null || courtId != M.saha)
            {
                if (court != null) Destroy(court); Color bg; court = CourtView.Build(transform, M.saha, out bg); cam.backgroundColor = bg; courtId = M.saha;
                var ak = court.transform.Find(CourtView.AKIS); akisR = ak != null ? ak.GetComponent<Renderer>() : null;
            }
            var gor = PlayerView.Benim(); string gk = gor.Anahtar();
            if (me == null || modelId != Ayar.model || gorunumId != gk)
            {
                if (me != null) Destroy(me.gameObject);
                me = PlayerView.Build(transform, Ayar.model, true, gor.forma, false, gor); gorunumId = gk;
                if (hk1 == null || modelId != Ayar.model)
                {
                    if (hk1 != null) { Destroy(hk1.gameObject); Destroy(hk2.gameObject); }
                    hk1 = PlayerView.Build(transform, Ayar.model, false, Kit.Hex("#0a1530"), true); hk2 = PlayerView.Build(transform, Ayar.model, false, Kit.Hex("#0a1530"), true);
                }
                modelId = Ayar.model;
            }
            if (op != null) Destroy(op.gameObject);
            var opR = Kit.Hex(Match.OpRenk(M.stil, Ayar.FORMA)); op = PlayerView.Build(transform, Ayar.model, false, opR, false, PlayerView.Rakip(M.stil, opR)); op.KordajSeyrek();   // 2026-10-26: rakibin tipi/saçı/ayakkabısı stil.json gorunum
            if (net != null) Destroy(net.gameObject); net = NetView.Build(transform, M.net);
            input.Reset(); lastW = 0;
        }

        public void ModelDegisti() { modelId = null; Kur(match); vitrin.Yenile(); }
        // 2026-10-31 akşam (yasin: "menüdeki tıklamalar geçişler akıcı olmalı"): Dolap'tan her çıkışta Kur(match) rakibi ve
        // fileyi sıfırdan kuruyordu (FBX örnekleme, malzemeler); hiçbir şey değişmese de sekme geçişi bir kare takılıyordu.
        // Görünüm anahtarı ve model aynıysa sahne kurulmaz; ekipman hesabı yine yapılır.
        public void GorunumDegisti()
        {
            Ayar.GearHesap();
            if (me == null || modelId != Ayar.model || gorunumId != PlayerView.Benim().Anahtar()) Kur(match);
            vitrin.Yenile();
        }
        public void SahaDegisti() { if (ekran != Ekran.Oyun) { match.saha = Ayar.saha; Kur(match); } }

        // Perde (HTML start(): perde({hazir, ortunce: maciKur})): disk kapanınca maç kurulur, sonra açılır.
        public void Oyna() { hud.Perde(() => { if (!Ayar.antrenman) StartMatch(true, true, false); else StartMatch(false, false, false); }); }
        public bool Hazir() { return sfx.Hazir; }   // perde ancak vuruş sesleri hazırlanınca açılır (HTML perde o.hazir)
        public void Antrenman() { hud.Perde(() => StartMatch(true, false, false)); }

        public void StartMatch(bool ogr, bool kisa, bool ilk)
        {
            var o = Secenek(ogr); o.ilk = ilk; o.ogrKisa = kisa;
            // Kariyer (2026-10-26, harita): haritadan seçilen açık basamak, yoksa sıradaki. Uzunluk basamaktan (Tur 1'de 0 =
            // oyuncunun seçimi, bugünkü davranış; Asya Turu 5, finali 11).
            if (Ayar.kariyerModu && !ogr)
            {
                int g = Ayar.kariyerSecim >= 0 && Ayar.ilerleme.Acik(Ayar.kariyerSecim) ? Ayar.kariyerSecim : Ayar.ilerleme.Siradaki(); var kb = KariyerVeri.Al(g);
                o.stil = Match.KariyerRakip(g); o.saha = kb.saha; if (kb.uzunluk > 0) o.target = kb.uzunluk;
            }
            else o.stil = KariyerVeri.Serbest(Match.STIL_SIRA[rnd.Next(3)], Ayar.ilerleme);   // 2026-10-27: oyuncunun kariyer seviyesinde (KariyerVeri.Serbest)
            mod = Mod.Normal; MacKur(seed++ * 7919 + (int)(Time.realtimeSinceStartup * 1000), o);
        }
        MatchOptions Secenek(bool ogr)
        {
            Ayar.GearHesap();
            return new MatchOptions { easy = Ayar.easy, target = Ayar.target, reduce = Ayar.reduce, ogr = ogr, bestRally = Ayar.bestRally, saha = Ayar.saha, gs = Ayar.GS, formaRenk = Ayar.FORMA, yardim = Ayar.ilerleme != null ? Ayar.ilerleme.Yardim : 0 };
        }
        // Maç kurulumunun ortak kuyruğu (2026-10-26: dört mod aynı yoldan; davranış eski StartMatch'le birebir).
        void MacKur(int tohum, MatchOptions o)
        {
            match = new Match(tohum, o);
            Kur(match); ekran = Ekran.Oyun; paused = false; Uyanik(true); ogrKayit = false; rakipKartT = o.ogr ? 0 : 1.5f; geriT = o.ogr || geriKapali ? 0 : GERI; erkenN = 0;
            fx.Temizle(); sfx.CrowdStart(); pencere = false; sfx.SuAlti(false); odul = null; hud.MacBasi(); vitrin.Kapat(); afis.Kapat();
            bekleyen = false; AudioListener.pause = false; sfx.Isit();   // tel/raket değiştiyse yeni sesler perde kapalıyken
            bittiSkor = null; bittiDril = null;
        }

        // ---- 2026-10-26 antrenman görevi (adım 7) ----
        // Görev Temel dersin altyapısıyla oynar (ogr + dril): skor yok, antrenör besler, maç sonu ödülü yok; sonuç ekranı ve
        // ödül (yalnız yeni yıldız) DrilSonu'nda. Ayar.antrenman'a dokunmaz: Temel ders görülmediyse görev onu "geçilmiş" saymaz.
        public void DrilOyna(string id) { if (Dril.Bul(id) == null) return; hud.Perde(() => DrilBaslat(id)); }
        public void DrilBaslat(string id)
        {
            var d = Dril.Bul(id); if (d == null) return; var o = Secenek(true); o.dril = d; o.easy = true;   // görev vuruşu sınar, koşuyu değil
            mod = Mod.Dril; drilId = id; MacKur(seed++ * 7919 + (int)(Time.realtimeSinceStartup * 1000), o);
        }
        void DrilSonu()
        {
            var M = match; var o = M.ogr; var d = o.dril; var p = Ayar.ilerleme;
            ekran = Ekran.Bitti; Uyanik(false); sfx.Ugultu(false, 0); sfx.CrowdSus(); bittiIlk = false;
            int y = d.Yildiz(o.isabet), eski = p.DrilYildiz(d.id), yeni = p.DrilKaydet(d.id, y), enIyi = Mathf.Max(y, eski);
            p.DrilGecmisEkle(d.id, o.isabet, Mathf.Min(o.dene, d.top));
            odul = Ayar.gear.DrilOdul(yeni); if (odul.sonra > odul.once) Yerel.Hap("flow");
            if (odul.sonra >= 30) Basarim.Ver(Basarim.EFSANE);
            if (yeni == 0) odul = null;   // ödülsüz tekrar: "+0 Tüy" satırı yazılmaz
            // 2026-10-30: görev 8 → 21 (63 yıldız); başarım App Store Connect metnindeki gibi 24 yıldızda kalır.
            if (p.AntrenmanYildiz >= Basarim.ANTRENOR_YILDIZ) Basarim.Ver(Basarim.ANTRENOR);
            yukseltHazir = false; Ayar.Kaydet();
            bittiBaslik = L.T("Görev bitti") + " · " + L.T(d.ad);
            BittiSifirla(); bittiKazandi = y > 0; bittiBas = L.T("Görev bitti"); bittiUst = L.T("Antrenman") + " · " + L.T(d.ad);
            bittiMaske = (1 << y) - 1; bittiYeniMaske = yeni > 0 ? ((1 << y) - 1) & ~((1 << eski) - 1) : 0;
            bittiKosul = new string[Dril.YILDIZ]; for (int i = 0; i < Dril.YILDIZ; i++) bittiKosul[i] = L.T("{0}/{1} hedefte", d.esik[i], d.top);
            bittiSkor = L.T("{0}/{1} hedefte", o.isabet, d.top);
            bittiYildiz = new string('★', y) + new string('☆', 3 - y) + " · " + L.T("{0}/3 yıldız", y) + (yeni > 0 ? " · " + L.T("{0} yeni", yeni) : enIyi > y ? " · " + L.T("en iyin {0}/3", enIyi) : "");
            // Sonraki yıldızın eşiği sayıyla (ne yapacağını bilsin); üç yıldızda görev tamam, tekrar ödül vermez (açıkça yazılır).
            bittiIpucu = enIyi >= 3 ? L.T("Üç yıldız. Tekrar oynamak ödül vermez, pratik olarak kalır.") : L.T("Sonraki yıldız: 12 topta {0} isabet", d.esik[enIyi]) + " · " + L.T(d.ogretir);
            bittiKart = new List<Ozet.Kart>(); bittiBilgi = ""; hud.ayrinti = false;
            bittiDugme = L.T(enIyi >= 3 ? "Yeniden oyna" : "Tekrar dene");
            sfx.MacSonu(yeni > 0); if (yeni > 0) hud.Konfeti();
        }
        public void DrilBitir() { if (match != null) match.DrilBitir(); }

        // ---- 2026-10-26 Günün maçı (adım 8) ----
        // Tarih başlangıç anında alınır: gece yarısını geçen maç başladığı günün maçıdır (ödül o güne yazılır).
        public void GunlukOyna() { hud.Perde(GunlukBaslat); }
        public void GunlukBaslat()
        {
            var g = Gunluk.Bugun(System.DateTime.Now); var o = Secenek(false);
            o.stil = Gunluk.Rakip(g); o.saha = g.saha; o.target = g.uzunluk; o.degistirici = g.deg.id;
            mod = Mod.Gunluk; gunluk = g; MacKur(g.tohum, o);   // tohum tarihten: herkes aynı maçı (aynı rastgele akış) oynar
        }

        // ---- 2026-10-26 Usta yolu (adım 9) ----
        public void UstaOyna() { if (!UstaYolu.Acik(Ayar.ilerleme)) return; hud.Perde(UstaBaslat); }
        public void UstaBaslat()
        {
            int n = Ayar.ilerleme.Usta; var b = UstaYolu.Basamak(n); var o = Secenek(false);
            o.stil = UstaYolu.Rakip(n); o.saha = b.saha; o.target = b.uzunluk; o.easy = false;   // Usta: koşmak da sende
            mod = Mod.Usta; MacKur(seed++ * 7919 + (int)(Time.realtimeSinceStartup * 1000), o);
        }
        public void OgrGec()
        {
            if (match.ogr == null) return;
            if (match.ogr.dril != null) { match.DrilBitir(); return; }   // görevde "geç" = görevi bitir (o ana kadarki isabetle)
            Ayar.antrenman = true; Ayar.Kaydet(); bool kisa = match.ogr.kisa;
            if (kisa) StartMatch(false, false, true); else StartMatch(false, false, false);
        }
        // Duraklatılmış maçtan ana menüye çıkınca maç bekler (HTML anaMenu: M korunur, menüde "Maça devam et").
        public void AnaMenu() { bekleyen = ekran == Ekran.Oyun && paused && match != null && match.ogr == null; ekran = Ekran.Menu; paused = false; KadrajSifirla(); Uyanik(false); if (ui != null) ui.MenuyeDon(); sfx.SuAlti(false); sfx.Ugultu(false, 0); sfx.CrowdSus(); AudioListener.pause = false; }
        public void MacaDon() { if (!bekleyen) return; bekleyen = false; ekran = Ekran.Oyun; paused = true; Devam(); }
        // Duraklatınca tüm ses susar (HTML AC.suspend), devamda kaldığı yerden sürer.
        public void Duraklat() { if (ekran == Ekran.Oyun && !paused) { paused = true; erkenT = 0; Uyanik(false); input.Reset(); sfx.Ugultu(false, 0); AudioListener.pause = true; } }
        public void Devam() { paused = false; AudioListener.pause = false; input.Reset(); if (ekran == Ekran.Oyun) { Uyanik(true); if (match != null && match.s != null && match.ogr == null && !geriKapali) geriT = GERI; } }
        // D5 (2026-10-31): arka planda iOS uygulamayı uyarısız kapatabilir; bekleyen kayıt ve PlayerPrefs diske yazılır.
        void OnApplicationPause(bool p) { if (p) { Duraklat(); BekleyenKayit(); PlayerPrefs.Save(); } }   // arka plana geçince kendiliğinden duraklar; top bir anda üstüne düşmez
        // Odak kaybı da duraklatır (2026-10-27, oyun incelemesi): Denetim Merkezi/Bildirim Merkezi açılması, gelen arama afişi,
        // Siri ve sistem uyarısı uygulamayı arka plana atmadan yalnız etkinliği alır (iOS willResignActive); oyun sürerken top
        // oyuncunun göremediği anda yere düşüyordu. Duraklat menüde ve maç sonunda hiçbir şey yapmaz. Yalnız telefonda: masaüstü
        // ve CI ekran turunda (Xvfb, pencere odaksız açılabilir) maç durmasın.
        void OnApplicationFocus(bool f) { if (f) Ayar.SistemOku(); else if (Application.isMobilePlatform) Duraklat(); }   // dönüşte sistem "Hareketi Azalt" ve yazı boyutu değişmiş olabilir

        void EndMatch(bool won)
        {
            ekran = Ekran.Bitti; Uyanik(false); var M = match; bittiIlk = M.ilk; sfx.MacSonu(won); sfx.Ugultu(false, 0);
            if (won) Ayar.wins++;
            if (!M.easy && M.ogr == null) Ayar.kosIpucu = true;   // Hud.KosIpucu: koşu sende ilk maç ipucu bir kez
            if (M.ogr == null && Ayar.ilerleme != null) Ayar.ilerleme.MacSay(M.smashes);   // Profil istatistiği (2026-10-26; cihazda)
            // Yardım kademesi (Core/Ilerleme.Yardim): Usta yolu dışındaki her maç. Kayıpta sonuç ekranı söyler (gizli kolaylık değil).
            int yardim0 = Ayar.ilerleme != null ? Ayar.ilerleme.Yardim : 0;
            if (M.ogr == null && Ayar.ilerleme != null && M.stil.usta < 0) Ayar.ilerleme.YardimIsle(won);
            bool yumusadi = Ayar.ilerleme != null && Ayar.ilerleme.Yardim > yardim0;
            string kNot = ""; bool kariyerIlk = false; int yeniYildiz = 0; bittiYildiz = null; int ekTuy = 0, ekXp = 0; string ekAd = null; bittiSkor = null;
            BittiSifirla(); bittiKazandi = won;
            if (KariyerVeri.Var(M.stil.kariyer))
            {   // yıldızlar ve açılış Core/Ilerleme'de; Tur 1 sayısı (tuytop_v2.kariyer, başarımlar) ondan türer
                int g = M.stil.kariyer; var bg = M.BasamakGorevi;
                var r = Ayar.ilerleme.Isle(g, won, bg != null && bg.bitti, M.sc[0], M.sc[1], M.target);
                kariyerIlk = r.ilkGalibiyet; kNot = r.not; yeniYildiz = r.yeniYildiz;
                Ayar.kariyer = Ayar.ilerleme.Tur1Gecilen(); if (Ayar.kariyer >= Match.KARIYER.Length) Ayar.sampiyon = true;
                if (kariyerIlk && KariyerVeri.TurOf(g) == 0) { Basarim.Ver("bs_kariyer_" + Ayar.kariyer); Basarim.Giris(Ayar.kariyer); }   // giriş isteği ilk basamaktan sonra
                if (r.turBitti) Yerel.Hap("flow");   // tur finali: güçlü desen (HAP'ta "success" yok, en uzun desen "flow")
                // 2026-10-26 (tasarım §5.5): Tur 2+ finali yeni başarım (Tur 1 finali bs_kariyer_6 olarak zaten var). Kimlikler
                // App Store Connect'e yasin ekler (YAPMAN_GEREKENLER); eklenmeden de yerel liste tutulur, sonra gönderilir.
                if (r.turBitti && KariyerVeri.TurOf(g) > 0) Basarim.Ver("bs_tur_" + (KariyerVeri.TurOf(g) + 1));
                Ayar.kariyerSecim = won ? -1 : g;   // kazanınca "Sıradaki", kaybedince aynı basamak ("Tekrar dene")
                int n = Ilerleme.Say(r.maske);
                bittiYildiz = new string('★', n) + new string('☆', 3 - n) + " · " + L.T("{0}/3 yıldız", n) + (r.yeniYildiz > 0 ? " · " + L.T("{0} yeni", r.yeniYildiz) : "");
                bittiMaske = r.maske; bittiYeniMaske = r.yeniMaske; BittiKariyer(g, won, M);
            }
            // 2026-10-26 Usta yolu: sıradaki basamağı kazanmak ilerletir (yeniden oynanan eski basamak yok; sayaç tek yönlü).
            // İlk galibiyet ödülü kariyerle aynı (+60 Tüy, +40 XP): Usta'da her galibiyet yeni basamaktır (tempo: "yeni şey").
            if (mod == Mod.Usta && M.stil.usta >= 0)
            {
                var p = Ayar.ilerleme; int n = M.stil.usta; var bg = M.BasamakGorevi; bool gorevOnce = p.UstaGorevBitti(n), siradaki = n == p.Usta;
                yeniYildiz = p.UstaIsle(n, won, bg != null && bg.bitti);   // görev yıldızı kayıpta da (+20 Tüy, kariyerle aynı)
                if (won && siradaki) { kariyerIlk = true; kNot = "Usta yolu: " + (n + 1) + ". basamak geçildi."; if (p.Usta >= 6) Basarim.Ver(Basarim.USTA6); if (UstaYolu.Basamak(n).final) Yerel.Hap("flow"); }
                else if (!won) kNot = "Usta yolu seni bekliyor.";
                if (siradaki)
                {   // iki yıldız: kazan + basamak görevi (biçim ve sayı yazıyla)
                    int ys = (won ? 1 : 0) + (p.UstaGorevBitti(n) ? 1 : 0);
                    bittiYildiz = new string('★', ys) + new string('☆', 2 - ys) + " · " + L.T("{0}/2 yıldız", ys) + (yeniYildiz > 0 ? " · " + L.T("{0} yeni", yeniYildiz) : "");
                    var bg2 = M.BasamakGorevi; bittiYildizN = 2; bittiMaske = (won ? 1 : 0) | (p.UstaGorevBitti(n) ? 2 : 0); bittiYeniMaske = (won && siradaki ? 1 : 0) | (!gorevOnce && p.UstaGorevBitti(n) ? 2 : 0);
                    bittiKosul = new[] { L.T("Kazan"), bg2 != null ? L.Cumle(bg2.ad) : L.T("Basamak görevi") };
                    bittiUst = L.T("Usta yolu") + " · " + L.T("{0}. basamak", n + 1) + " · " + L.T(UstaYolu.Basamak(n).yer);
                }
            }
            // 2026-10-26 Günün maçı: günde bir kez, kazanınca +40 Tüy, +30 XP. Kayıp normal maç ödülünü alır, bugünün ödülü açık kalır.
            if (mod == Mod.Gunluk && gunluk != null && won && Ayar.ilerleme.GunlukKazan(gunluk.tarih))
            { ekTuy = Gunluk.OdulTuy; ekXp = Gunluk.OdulXp; ekAd = "Günün maçı"; kNot = "Günün maçı kazanıldı."; Basarim.Ver(Basarim.GUNUN_MACI); }
            if (M.okudun > 0) { Ayar.ilerleme.ImzaOkuSay(M.okudun); if (Ayar.ilerleme.ImzaOkunan >= Basarim.IMZA_OKUR_HEDEF) Basarim.Ver(Basarim.IMZA_OKUR); }
            // Maç sonu ödülü (HTML ekoOdul): antrenmanda yok (antrenman maç sonu ekranına gelmez). Yeni yıldız +20 Tüy, okunan imza +5 XP.
            odul = Ayar.gear.Odul(won, kariyerIlk, M.target, M.easy, M.gorevBitti, M.perfects, System.DateTime.Now, yeniYildiz, M.okudun, ekTuy, ekXp, ekAd);
            if (odul.sonra > odul.once) Yerel.Hap("flow");   // seviye atladın
            if (odul.sonra >= 30) Basarim.Ver(Basarim.EFSANE);
            // "Ekipmanı yükselt": alınabilecek yükseltme ya da (ekipman v2) seviyesi gelmiş, Tüy'ü yeten model varsa.
            yukseltHazir = false; foreach (var y in Gear.YUVA) { int sv = Ayar.gear.eko.sv[y.k]; if (sv < 10 && Ayar.gear.eko.tuy >= Gear.YUK_FIYAT[sv - 1] && odul.sonra >= Gear.YUK_SV[sv - 1] || Ayar.gear.AlinabilirVar(y.m)) yukseltHazir = true; }
            Ayar.Kaydet();
            bittiBaslik = L.T(won ? "Kazandın" : "Bu sefer rakip aldı") + (kNot != "" ? " · " + L.Cumle(kNot) : "");
            // "Galibiyet" anahtarı tr_en.json'da istatistik etiketi ("Wins"); başlıkta "Victory" olmalı (r16), sözlük HTML'in
            // birebir kopyası olduğu için orada değiştirilmez.
            bittiBas = won ? (L.En ? "Victory" : "Galibiyet") : L.T("Bu sefer rakip aldı");
            if (kNot != "") { if (kariyerIlk) bittiAcilan = L.Cumle(kNot); else bittiAltNot = L.Cumle(kNot); }
            if (yumusadi) bittiAltNot = (bittiAltNot != null ? bittiAltNot + " " : "") + L.T("Sıradaki maçta rakip biraz yavaşlayacak.");
            if (mod == Mod.Gunluk && gunluk != null) bittiUst = L.T("Günün maçı") + " · " + L.T(gunluk.deg.ad);
            else if (bittiUst == null && M.ogr == null) bittiUst = L.T("Hızlı maç") + " · " + L.T("Rakip") + " " + M.RakipAd(false);
            var bitenler = new List<string>(); foreach (var g in M.gorevler) if (g.bitti) bitenler.Add(L.Cumle(g.ad));
            bittiBilgi = L.T("Rakip {0} · Görev {1}/3 ({2}) · En uzun ralli {3} · {4} smaç · {5} file vuruşu · {6} mükemmel · {7} aldatma · {8} split step · {9} tepeden · {10} uzanma · {11} kez dışarıyı bıraktın",
                M.RakipAd(false), M.gorevBitti, bitenler.Count > 0 ? string.Join(", ", bitenler.ToArray()) : L.T("yok"), M.maxRally, M.smashes, M.kills, M.perfects, M.deceives, M.splits, M.tepeden, M.saves, M.outsLeft);
            bittiDugme = L.Cumle(Career.AgainLabel(M.stil.kariyer >= 0, won, Ayar.ilerleme.HepsiBitti ? -1 : Ayar.ilerleme.Siradaki()));
            if (mod == Mod.Usta) bittiDugme = won ? L.T("Sıradaki Usta basamağı") : L.T("Tekrar dene");
            else if (mod == Mod.Gunluk) bittiDugme = L.T("Günün maçını yeniden oyna");
            // Kayıpta bir antrenman görevi önerisi (tasarım §3.6); görev zaten üç yıldızsa önerilmez.
            bittiDril = Dril.Oner(M, won, Ayar.ilerleme);
            // Özet (Ar-Ge 23 Eki #2): iki kart + ipucu; bittiBilgi artık "Ayrıntılar" ardında (Hud.Over).
            bittiKart = Ozet.Kartlar(M); object ia; string ik = Ozet.Ipucu(M, won, out ia);
            bittiIpucu = ik == null ? null : ia == null ? L.T(ik) : L.T(ik, ia is string ? L.Cumle((string)ia) : ia); hud.ayrinti = false;
            if (Ozet.PuanIste(won, kariyerIlk, Ayar.wins, Ayar.puan)) { CancelInvoke("PuanIste"); Invoke("PuanIste", 1.5f); }
            if (won) hud.Konfeti();
        }
        // Yalnız ekran turu (EkranTuru, -bsekran) için: sonuç ekranını maçı sonuna kadar oynatmadan görmek (2026-10-26, yön A
        // sonuç ekranı). Mağaza build'inde çağıran yok; EndMatch'in kendisi değişmez, tur gerçek akışın sonucunu çeker.
        public void EkranBitir(bool won) { EndMatch(won); }
        // "Bir maç daha" / "Yeni maç başlat" da smaç perdesiyle (HTML start()).
        public void TekrarOyna()
        {
            var m = mod;   // perde kapanınca hangi mod: maç sonundaki mod (ekrandaki düğme onu söyledi)
            hud.Perde(() => { if (m == Mod.Dril) DrilBaslat(drilId); else if (m == Mod.Gunluk) GunlukBaslat(); else if (m == Mod.Usta) UstaBaslat(); else StartMatch(false, false, false); });
        }
        // Mağaza puanı (Ar-Ge 23 Eki #1): sonuç ekranından 1,5 sn sonra, oyuncu hâlâ oradaysa; yalnız sistem penceresi.
        // İstenmediyse sayılmaz. Apple ayrıca yılda 3 ile sınırlar; TestFlight'ta pencere hiç çıkmaz. Android hattı yok.
        void PuanIste()
        {
            if (ekran != Ekran.Bitti || Ayar.puan >= Ozet.PUAN_AZAMI) return;
#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.RequestStoreReview(); Ayar.puan++; Ayar.Kaydet();
#endif
        }

        // Dikey ekranda kortun tamamı, iki oyuncu ve rakibin arkasındaki pano görünsün: konum, eğim ve görüş açısı ekrana göre (FitCamera).
        // Kamera açısı (Ayarlar › Kamera, 2026-10-26 yasin "belki kamera açısı seçtirilebilir"): Yayın = TV yayın kamerası
        // (yüksek, arkadan; varsayılan, eski tek açı), Alçak = oyuncunun omzunun gerisinden (rakip ve file büyük, derinlik
        // hissi güçlü), Üstten = kuşbakışına yakın (top yeri ve çizgiler en net). Görüş açısı her açıda aynı kuralla ekrana
        // sığdırılır; dokunma/kaydırma ışın atışıyla çözüldüğü için girdi açıdan bağımsız çalışır.
        public static readonly string[] KAMERA_SIRA = { "yayin", "alcak", "ust" };
        public static readonly string[] KAMERA_AD = { "Yayın", "Alçak", "Üstten" };
        public void KameraDegisti() { lastW = 0; }
        int lastW, lastH; Rect lastSafe;
        // Kadraj (2026-10-26, yasin onayı; inceleme 2026-10-26 §1): görüş açısı eskiden simetrikti; dikey telefonda kort ekranın
        // genişliğine takıldığı için kalan dikey pay üstte boş kalıyordu (Yayın'da ekranın ~%30'u, Alçak'ta yarıya yakını).
        // Onaylanan hedefler (değişmedi): üstte boş bant yok (seyirci başları ve rakibin sıçrama tepesi skor kutusunun hemen
        // altında), uzak yarı ve rakip r15'e göre ~1,3× büyük, yakın dip çizgi smaç çubuğunun üstünde, tekler alanı (+15 cm)
        // her derinlikte ekranda (çiftler koridoru yakın köşelerde taşabilir), iPhone SE ve 19,5:9.
        // İkinci yazım (2026-10-27, kadraj incelemesi, r22 → r27): ilk yazım bu hedeflere görüntü düzlemini KAD_EGIM kadar
        // fazladan aşağı eğip (12/20°) merceği eksen dışı kaydırarak (projeksiyon matrisinde m12) ulaşıyordu. Matris girdiyi
        // bozmuyordu (ScreenPointToRay, WorldToScreenPoint, kırpma aynı matrisi kullanır) ama eğim + kaydırma "mimari düzeltmenin"
        // tersiydi: dikeyler daha çok yatıyor, Alçak 7 m'den 50,6° aşağı 83° geniş açıyla bakıyordu (yakın yarı 305 px, rakip
        // 64 px, file direği 15° yatık). Şimdi düz perspektif kamera: kaydırma yok (camK = 0), konum ve eğim çözülür:
        //  - Her açının bir "dipten geri" uzaklığı (KAD_D) var; yükseklik ve eğim ekrana göre aranır: tekler genişliği ekranı
        //    doldurur, üst nokta skor kutusunun altına oturur, yakın dip çizginin 0,8 m gerisi (oyuncu geri kaçınca ayakları)
        //    smaç çubuğunun üstüne. Yayın uzaktan (dipten 7 m) dar açıyla bakar: TV yayın kamerası gibi, yakın yarı şişmez.
        //  - Eğimin tavanı var (KAD_EGIM_AZAMI): uzun ekranı yalnız eğimle doldurmak oyuncuları üstten basık gösterir
        //    (19,5:9'da Yayın 52°'de rakip boy/en 0,80). Tavana değince fazla pay altta kalır: dip çizginin gerisindeki zemin
        //    (Yayın 19,5:9'da ~30 px). SE'de tavana değilmez, kort ekranı tam doldurur. Uzaklık küçükken (dipten 3–4 m) tavan
        //    kamerayı 5 m'ye indirip açıyı 85°'ye açıyordu (model taraması); Alçak bu yüzden dipten 5 m, genişlikte %10 taşar.
        //  - Uzaklık gölge mesafesine bağlı: Kalite orta basamağı 25 m; Yayın'da rakibin dip gerisi 24,3 m (dipten 7 m sınır).
        //  - Çözüm yoksa (çok geniş ekran, ör. iPad) son çare eski kaydırma: üst nokta yine skor kutusunun altına alınır.
        // Ölçüm (model + CI r27, 390×845 / 375×667; EkranTuru "kadraj" satırları): Yayın 19,5:9 yükseklik 13,7 m, eğim 47°, açı 44°;
        // uzak çiftler eni r15 187 → 268 px (1,43×), 1,75 m rakip 53 → 71 px (1,34×), file bandı 52 → 63 px, direk eğikliği −9°.
        // Üçüncü yazım (2026-10-27 öğleden sonra, yasin build 41 "Top yükseldiğinde kadrajdan çıkıyor"): çözüm Core/Kadraj'a taşındı
        // (aynı sayılar, BotMac mono'da ölçebilsin; 390×845 ve SE'de r42 kadraj satırlarıyla birebir), üstüne yüksek top takibi:
        // kamera topun uçuş yolundan hesaplanan eğimle yukarı bakar, görüş açısı yakın dip çizgiyi smaç çubuğunda tutacak kadar
        // açılır (Kadraj.Takip/Genis; gerekçe ve ölçüm Kadraj.cs başında). Hareketi azaltta eğim yok, yalnız ekran dışı işareti
        // (KadrajIsaret). Konum hiç değişmez; eğim ve görüş açısı her karede yalnız buradan yazılır.
        public static readonly float[] KAD_D = Kadraj.D;                  // KAMERA_SIRA sırasıyla: dipten geri (m)
        public static readonly float[] KAD_GENIS = Kadraj.GENIS;          // tekler(+15 cm) genişliğinin ekrana oranı: Alçak yakın köşede
                                                                         // ~8 px taşar (oyuncu büyür), Üstten'de yanlarda pay
        public static readonly float[] KAD_EGIM_AZAMI = Kadraj.EGIM_AZAMI; // eğim tavanı (derece)
        float camTv = .6f, camK;   // dikey yarım açının tanjantı ve eksen dışı kayma (NDC; + = görüntü yukarı; düz kamerada 0)
        public Kadraj.Kamera kam; public float kadrajEgim, kadrajGenis = 1;   // çözüm ve bu karenin takip eğimi (°) / görüş çarpanı
        readonly Kadraj.Takip takip = new Kadraj.Takip();
        void FitCamera()
        {
            var sa = Guvenli.Alan;   // 2026-10-27: menü ve HUD ile aynı kaynak (ekran turunun çentik taklidi kadrajı da kapsasın)
            if (Screen.width == lastW && Screen.height == lastH && sa == lastSafe) return; lastW = Screen.width; lastH = Screen.height; lastSafe = sa;
            int ki = System.Array.IndexOf(KAMERA_SIRA, Ayar.kamera); if (ki < 0) ki = 0;
            kam = Kadraj.Coz(Screen.width, Screen.height, sa.x, sa.y, sa.width, sa.height, ki);
            camTv = kam.tv; camK = kam.k; takip.Egim(kam, null, 0, true);   // açı/ekran değişince takip sıfırdan
            camBase = new Vector3(0, kam.h, kam.Z); camRot = Quaternion.Euler(kam.p, 0, 0); cam.transform.SetPositionAndRotation(camBase, camRot);
            kadrajEgim = 0; kadrajGenis = 1; Projeksiyon(1);
        }
        // Takip: maçta (sonuç ekranında da, top yere inerken) uçuşun eğimi; menüde ve hareketi azaltta 0. Saat Match.clock:
        // duraklatınca kamera da durur, ağır çekimde yavaşlar.
        void KadrajTakip()
        {
            var M = match; bool kapali = Ayar.reduce || ekran == Ekran.Menu || M == null;
            kadrajEgim = takip.Egim(kam, kapali ? null : M.s, M != null ? M.clock : 0, kapali);
            kadrajGenis = Kadraj.Genis(kam, kadrajEgim); camRot = Quaternion.Euler(kam.p - kadrajEgim, 0, 0);
        }
        // Menüye dönüşte kadraj hemen düz (tur 2 inceleme): Oyna'nın bulanık salon fonu (UiKok.SalonCek) menünün ilk karesinde ana
        // kameradan çekiliyor; UiKok bu Update'ten önce koşarsa duraklatmada (top havadayken) ya da maç sonunda kalan eğim ve
        // görüş açısı fona giriyordu ve perde kalkmadan fon yeniden çekilmiyordu.
        void KadrajSifirla()
        {
            takip.Egim(kam, null, 0, true); kadrajEgim = 0; kadrajGenis = 1; camRot = Quaternion.Euler(kam.p, 0, 0);
            if (cam != null) { cam.transform.SetPositionAndRotation(camBase, camRot); Projeksiyon(1); }
        }
        // Smaç "punch" yakınlaşması ekranın ortasına göre (eksen dışı kaymayla birlikte ölçeklenir). camK = 0'da bu matris Unity'nin
        // kendi fieldOfView izdüşümüyle aynıdır; özel matris yalnız son çare kaydırma ve punch için tutuluyor.
        void Projeksiyon(float z)
        {
            var m = Matrix4x4.Perspective(2 * Mathf.Atan(camTv * kadrajGenis) * Mathf.Rad2Deg, (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height), cam.nearClipPlane, cam.farClipPlane);
            m.m00 *= z; m.m11 *= z; m.m12 = -camK * z; cam.projectionMatrix = m;
        }

        // ---- kaydırma girdisi: piksel → 640 px yükseklikli referans (HTML eşikleri aynen geçerli) ----
        class Swipe { public bool on, hit, moved; public float minD = 1e9f; public List<Vector3> tr = new List<Vector3>(); public Vector2 p0; public float t0; public void Reset() { on = hit = moved = false; minD = 1e9f; tr.Clear(); } }
        readonly Swipe input = new Swipe(); int erkenN;
        float Ref { get { return 640f / Mathf.Max(1, Screen.height); } }
        Vector2 RefPos(Vector2 screen) { return new Vector2(screen.x * Ref, (Screen.height - screen.y) * Ref); }   // y aşağı

        void SwipeVec(out float dx, out float dy, out float speed)
        {
            var tr = input.tr; var last = tr[tr.Count - 1]; var a = tr[0];
            foreach (var p in tr) if (last.z - p.z <= 90) { a = p; break; }   // pencere son harekete göre (HTML swipeVec)
            float dt = Mathf.Max(16, last.z - a.z); dx = last.x - a.x; dy = last.y - a.y; speed = Mathf.Sqrt(dx * dx + dy * dy) / dt;
        }
        // Kavis (slice): izin ilk yarısı ile ikinci yarısının yönü 0,8 rad'dan fazla ayrışıyorsa ve ikinci yarı 25 px'ten uzunsa.
        // Aldatıcı vuruş jesti (2026-10-26, Core/Aldatma.JestOku): tutma süresi, yay yönü, V. Hesap Core'da (BotMac sınar); burada
        // yalnız iz diziye çevrilir. Kaydırmanın eski alanları (tür, hız, kavis) değişmez.
        SwipeInput Jest(SwipeInput si)
        {
            var tr = input.tr; int n = tr.Count; var xs = new float[n]; var ys = new float[n]; var ts = new float[n];
            for (int i = 0; i < n; i++) { xs[i] = tr[i].x; ys[i] = tr[i].y; ts[i] = tr[i].z; }
            Aldatma.JestOku(xs, ys, ts, n, input.p0.x, input.p0.y, input.t0, ref si); return si;
        }
        bool Curved()
        {
            var tr = input.tr; if (tr.Count < 4) return false; int n = tr.Count, m = n / 2; Vector3 a = tr[0], b = tr[m], c = tr[n - 1];
            float a1 = Mathf.Atan2(b.y - a.y, b.x - a.x), a2 = Mathf.Atan2(c.y - b.y, c.x - b.x), da = Mathf.Abs(a2 - a1); if (da > Mathf.PI) da = 2 * Mathf.PI - da;
            return da > .8f && Vector2.Distance(c, b) > 25;
        }
        static float SegDist(Vector2 p, Vector2 a, Vector2 b, out float side)
        {
            Vector2 v = b - a; float L2 = v.sqrMagnitude, t = L2 > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, v) / L2) : 0;
            side = Mathf.Sign(v.x * (p.y - a.y) - v.y * (p.x - a.x)); return Vector2.Distance(p, a + v * t);
        }
        bool CourtPoint(Vector2 screen, out Vector2 c)
        {
            var ray = cam.ScreenPointToRay(screen); c = Vector2.zero; if (Mathf.Abs(ray.direction.y) < 1e-4f) return false;
            float t = -ray.origin.y / ray.direction.y; if (t < 0) return false; c = Kit.FromWorld(ray.origin + ray.direction * t); return true;
        }
        // HTML hitR(p.s) = max(26, 40·s)·(akış 1,25)·(Kolay 1,3); s = ppm/28 (topun derinliğinde metre başına piksel / 28).
        public float HitR()
        {
            var M = match; float sc = 1; if (M.s != null) sc = shuttle.PxPerM(Kit.W(M.s.x, M.s.y, M.s.z)) / 28f;
            return Mathf.Max(26, 40 * sc) * (M.flow ? 1.25f : 1) * (M.easy ? 1.3f : 1);
        }
        public bool previewOn; public Vector2 previewAt; public string previewTip;
        public List<Vector3> ParmakIzi { get { return input.tr; } }
        public bool ParmakVurdu { get { return input.hit; } }
        public Vector2 BallRef() { var M = match; if (M.s == null) return Vector2.zero; return RefPos(cam.WorldToScreenPoint(Kit.W(M.s.x, M.s.y, M.s.z))); }
        public bool Parmak { get { return input.on; } }
        public void CanliOkuma(out string tip, out float sp)
        {
            tip = null; sp = 0; var M = match; if (!input.on || input.tr.Count < 2) return; float dx, dy; SwipeVec(out dx, out dy, out sp);
            var a = M.OgrA(); if (a == null) return;
            if (a.servis != null) { if (M.s != null || M.svPending != null) return; var q = input.tr[input.tr.Count - 1]; float ddx = q.x - input.p0.x, ddy = q.y - input.p0.y; if (-ddy > 30) tip = Match.ServisTuru(ddx, ddy, sp); return; }
            Reach I; if (M.s == null || !M.ReachInfo(out I)) return;
            var pl = M.ShotPlan(new SwipeInput { dx = dx, dy = dy, speed = sp, curved = Curved() }, I, true); tip = pl.type;
        }

        void ReadInput()
        {
            bool down, held, up; Vector2 pos;
            if (Input.touchCount > 0) { var t = Input.GetTouch(0); pos = t.position; down = t.phase == TouchPhase.Began; up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled; held = !down && !up; }
            else { pos = Input.mousePosition; down = Input.GetMouseButtonDown(0); up = Input.GetMouseButtonUp(0); held = Input.GetMouseButton(0) && !down; }
            float now = Time.unscaledTime * 1000; var q = RefPos(pos); var M = match;
            if (erkenT > 0)
            {
                if (now - erkenT > (M.easy ? ERKEN_MS_KOLAY : ERKEN_MS_USTA) || M.s != erkenTop || M.s == null || M.s.dead || input.hit) erkenT = 0;
                else if (M.HitWindow()) { erkenT = 0; if (M.SwipeShot(erkenSw)) { input.hit = true; Yerel.Vurus(M.sonTip, M.sonQ, M.sonSlice); } }
            }
            if (down)
            {
                if (hud.Blocks(new Vector2(pos.x, Screen.height - pos.y))) return;   // HUD düğmesine dokunuş kaydırma değil
                if (M.ogr != null && BilgiDokun(pos)) return;   // antrenman bilgi adımı: işaretli alana dokun
                if (rakipKartT > 0) { rakipKartT = 0; }
                input.Reset(); input.on = true; input.p0 = q; input.t0 = now; input.tr.Add(new Vector3(q.x, q.y, now)); M.OnTouchDown(); sfx.Ugultu(true, 0);
                // Usta koşusu (2026-10-31 U2): vuruş penceresinde de koşu emri alınır, yeter ki dokunuş topun yakınında olmasın
                // (topun yanındaki dokunuş vuruş kaydırmasının başıdır). Eskiden pencere açılınca hareket kilitleniyordu; araştırılan
                // raket oyunlarının hiçbiri vuruş anında hareketi kilitlemiyor. Hedef zaten varana dek sürer (Usta'da manualT
                // yalnız Kolay'ın kendiliğinden koşusunu bastırır).
                Vector2 c; if (!M.easy && M.s != null && (!M.HitWindow() || Vector2.Distance(q, BallRef()) > HitR() * 2.2f) && CourtPoint(pos, out c)) M.RunTo(c.x, c.y, 1.4f);
            }
            else if (held && input.on)
            {
                var prev = input.tr[input.tr.Count - 1]; input.tr.Add(new Vector3(q.x, q.y, now));
                while (input.tr.Count > 2 && now - input.tr[0].z > 300) input.tr.RemoveAt(0);
                if (Vector2.Distance(q, input.p0) > 18) input.moved = true;
                float segSpeed = Vector2.Distance(q, prev) / Mathf.Max(8, now - prev.z); sfx.Ugultu(true, segSpeed);
                Vector2 c; if (!M.easy && M.s != null && !M.HitWindow() && !input.hit && segSpeed < .6f && CourtPoint(pos, out c)) M.RunTo(c.x, c.y, 1.4f);
                // Erken kaydırma tamponu (2026-10-31 U3): pencere açılmadan yapılan belirgin kaydırma saklanır, pencere ERKEN_MS
                // içinde açılırsa vuruş olur (en iyi "good": topu kesmeden geçmiş sayılır). Platform oyunlarının görünmez toleransı;
                // Usta'da süre kısa.
                if (!input.hit && M.s != null && !M.s.dead && M.s.from == "op" && !(M.ogr != null && M.ogr.dur) && !M.HitWindow() && Vector2.Distance(q, input.p0) > 70)
                {
                    float edx, edy, esp; SwipeVec(out edx, out edy, out esp);
                    if (esp > .45f) { erkenSw = Jest(new SwipeInput { dx = edx, dy = edy, speed = esp, dist = HitR() * .9f, R = HitR(), curved = Curved() }); erkenT = now; erkenTop = M.s; }
                }
                if (!input.hit && M.s != null && !M.s.dead && M.s.from == "op" && M.HitWindow())
                {
                    var sr = BallRef(); float side;
                    float d = SegDist(sr, new Vector2(prev.x, prev.y), q, out side), far = Vector2.Distance(q, input.p0), R = HitR();
                    input.minD = Mathf.Min(input.minD, d / R);
                    float dx, dy, speed; SwipeVec(out dx, out dy, out speed);
                    // Hedef önizleme: parmak ekrandayken "şu an bıraksan top nereye düşer" (HTML M.preview)
                    if (far > 14 && speed > .1f) { Reach I; if (M.ReachInfo(out I)) { var pl = M.ShotPlan(new SwipeInput { dx = dx, dy = dy, speed = speed, curved = Curved() }, I, true); previewOn = true; previewAt = new Vector2(Mathf.Clamp(pl.tx, Court.SL, Court.SR), Court.NET - pl.d); previewTip = pl.type; } }
                    // Topun üstünden geçmek şart değil: pencere açıkken belirgin kaydırma (70 px, kavis için yer) vuruştur; topu kesmek mükemmeli verir.
                    bool durdu = M.ogr != null && M.ogr.dur;
                    if (!durdu && (d <= R || (far > 70 && speed > .45f)))
                        if (M.SwipeShot(Jest(new SwipeInput { dx = dx, dy = dy, speed = speed, dist = d <= R ? d : R * 1.5f, R = R, side = side, curved = Curved() }))) { input.hit = true; Yerel.Vurus(M.sonTip, M.sonQ, M.sonSlice); }
                }
            }
            else if (up && input.on)
            {
                input.on = false; input.tr.Add(new Vector3(q.x, q.y, now)); previewOn = false; sfx.Ugultu(false, 0); trSon = now;
                if (M.s == null) { float dy0 = q.y - input.p0.y, dx0 = q.x - input.p0.x; if (-dy0 > 30) { float dx, dy, speed; SwipeVec(out dx, out dy, out speed); M.SwipeServe(dx0, dy0, speed); } return; }
                float far = Vector2.Distance(q, input.p0);
                if (!input.hit && M.HitWindow())
                {
                    if (far < 24) { if (M.SwipeShot(new SwipeInput { dx = 0, dy = -1, speed = 1, dist = HitR() * 1.5f, R = HitR(), tap = true })) Yerel.Vurus(M.sonTip, M.sonQ, M.sonSlice); }   // dokunma = güvenli clear
                    else if (M.ogr != null && M.ogr.dur)
                    {   // antrenmanda top durur: vuruş parmak kalkınca, bütün hareketin hızıyla okunur
                        float dx, dy, speed; SwipeVec(out dx, out dy, out speed);
                        if (M.SwipeShot(Jest(new SwipeInput { dx = dx, dy = dy, speed = speed, dist = Mathf.Min(input.minD, 1.5f) * HitR(), R = HitR(), curved = Curved() }))) Yerel.Vurus(M.sonTip, M.sonQ, M.sonSlice);
                    }
                    else
                    {   // "doğru zamanda vurdum ama geçti": ıskanın nedeni bir kelimeyle
                        float dx, dy, speed; SwipeVec(out dx, out dy, out speed);
                        string neden = far < 70 ? "Kısa kaydırdın" : speed <= .45f ? "Yavaş kaydırdın" : input.minD < 2.2f ? "Kıl payı: topun üstünden geç" : "Topun uzağından geçtin";
                        hud.Say(L.T("Iska · {0}", L.T(neden)), Kit.W(M.me.x, M.me.y, 1.6f), Kit.Hex("#ffd27a")); sfx.Iska();
                    }
                }
                else if (!input.hit && erkenT <= 0 && !M.s.dead && M.s.from == "op" && !M.HitWindow() && far > 60 && erkenN < 3) { erkenN++; hud.Say(L.T("Erken: top parlayınca kaydır"), Kit.W(M.me.x, M.me.y, 1.6f), Kit.Hex("#ffd27a")); }   // maç başına en çok 3 kez
                else if (M.easy && !input.hit && !M.HitWindow() && far < 18 && now - input.t0 < 250)
                { Vector2 c; if (CourtPoint(pos, out c)) M.RunTo(c.x, c.y, .7f); }   // Kolay'da kısa dokunuş yer değiştirir
            }
        }
        public float trSon;
        const float ERKEN_MS_KOLAY = 120, ERKEN_MS_USTA = 80; SwipeInput erkenSw; float erkenT; Shuttle erkenTop;
        // Bilgi adımı (HTML ogrBilgiDokun): nefes halkası (ayak çevresi, 34u+22 × 16u+22 px) ya da alttaki smaç gücü çubuğu.
        bool BilgiDokun(Vector2 pos)
        {
            var M = match; var a = M.OgrA(); if (a == null || a.bilgi == null) return false;
            var r = RefPos(pos); bool ic;
            if (a.bilgi == "nefes") { var p = RefPos(cam.WorldToScreenPoint(Kit.W(M.me.x, M.me.y, 0))); float u = shuttle.PxPerM(Kit.W(M.me.x, M.me.y, 0)) / 28f; ic = Mathf.Pow((r.x - p.x) / (34 * u + 22), 2) + Mathf.Pow((r.y - p.y) / (16 * u + 22), 2) <= 1; }
            else ic = r.y > 640 - 60;
            if (ic) M.OgrBilgiGec(); return true;
        }

        // 2026-10-31 (Unity incelemesi O1): görev tamamlanınca tam kayıt (JSON + ~20 PlayerPrefs anahtarı) vuruş anında yapılıyordu;
        // tek kare takılma. Sayı arasına, duraklatmaya ya da menüye ertelenir; arka plana geçişte de yazılır (OnApplicationPause).
        bool kaydetBekle;
        void BekleyenKayit() { if (!kaydetBekle) return; kaydetBekle = false; Ayar.Kaydet(); }
        void Update()
        {
            FitCamera();
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            if (kaydetBekle && (ekran != Ekran.Oyun || paused || match == null || match.between)) BekleyenKayit();
            if (ekran == Ekran.Oyun && !paused)
            {
                if (rakipKartT > 0) rakipKartT -= dt;
                if (geriT > 0)
                {
                    int once = Mathf.CeilToInt(geriT / (GERI / 3)); geriT -= dt; int sonra = geriT > 0 ? Mathf.CeilToInt(geriT / (GERI / 3)) : 0;
                    if (sonra != once) { if (sonra > 0) { sfx.Tik(660); Yerel.Hap("tick"); } else sfx.Tik(990); }
                    if (geriT <= 0) { geriT = 0; input.Reset(); }
                }
                else { ReadInput(); match.Update(dt); }
                bool w = match.HitWindow(); if (w != pencere) { pencere = w; if (!Ayar.reduce) sfx.SuAlti(w); if (w) { sfx.Tik(); Yerel.Hap("tick"); } }   // vuruş penceresi: su altı
                Gicirti(dt);
                foreach (var e in match.events) OnEvent(e); match.events.Clear();
                if (match.bestRally > Ayar.bestRally) { Ayar.bestRally = match.bestRally; Ayar.Kaydet(); }
                var o = match.ogr;
                if (o != null && o.dril != null) { if (o.macaGec && !ogrKayit) { ogrKayit = true; DrilSonu(); } }   // görev: "Görev bitti" yazısından sonra sonuç ekranı, maça geçmez
                else
                {
                    if (o != null && o.bitti && !ogrKayit) { ogrKayit = true; Ayar.antrenman = true; Ayar.Kaydet(); if (!o.kisa) Basarim.Ver(Basarim.ANTRENMAN); }   // geçilen antrenman sayılmaz
                    if (o != null && o.macaGec) StartMatch(false, false, o.kisa);
                }
            }
            me.Show(match.me, match, dt); op.Show(match.op, match, dt); shuttle.reduce = Ayar.reduce;
            shuttle.Show(match, ekran == Ekran.Menu ? null : match.s, paused || geriT > 0 ? 0 : dt, previewOn && input.on && !input.hit, previewAt);
            fx.Tick(match, paused || geriT > 0 ? 0 : dt, ekran == Ekran.Oyun);
            if (akisR != null) { bool on = ekran == Ekran.Oyun && match.flow; akisR.enabled = on; if (on) akisR.material.color = new Color(1, .886f, .47f, Ayar.reduce ? .9f : .75f + .25f * Mathf.Sin(Time.unscaledTime * 1000 / 180f)); }
            Hakemler();
            shake *= Mathf.Pow(.002f, dt); punch = 1 + (punch - 1) * Mathf.Pow(.0005f, dt); if (punch < 1) punch = 1;
            var j = shake > .01f ? (Vector3)(Random.insideUnitCircle * shake * .012f) : Vector3.zero;
            KadrajTakip(); cam.transform.SetPositionAndRotation(camBase + camRot * j, camRot); Projeksiyon(punch);
        }

        // Baş hakem sayı bitince skoru girer (başı eğilir); servis el değişince yeni servisçinin tarafına döner. Servis hakemi
        // servisi atacak oyuncuya bakar, el değişince ~0,3 sn sonra döner. Ralli sırasında kıpırdamazlar; hareketi azaltta hareketsiz.
        void Hakemler()
        {
            var M = match; float yaw = 90, tilt = 0;
            if (M.hakemT > 0 && !Ayar.reduce) { float e = (M.hakemT0 - M.hakemT) * 1.4f / M.hakemT0; if (M.hakemSO) yaw = 90 + (M.hakemYon == "me" ? 30 : -30) * Mathf.Clamp01(e * 3); else if (e < 1.05f) tilt = 14 * Mathf.Sin(Mathf.Clamp01(e) * Mathf.PI); }
            hk1.ShowSit(CourtView.HAKEM1, yaw, tilt, CourtView.AYAKLIK_Y, CourtView.AYAKLIK_ILERI);
            string yeni = M.server, eski = M.sjYon ?? yeni; float k = eski != yeni && M.sjT > 0 ? Mathf.Clamp01(M.sjT / .5f) : 0;
            float a0 = eski == "me" ? -125 : -55, a1 = yeni == "me" ? -125 : -55;
            hk2.ShowSit(CourtView.HAKEM2, Mathf.Lerp(a0, a1, k), 0, 0, .42f);
        }

        // Ayakkabı gıcırtısı ve toz (HTML runMe): hızlı koşarken yön sertçe değişince; koşu > 2,6 m/sn'de 70 ms'de bir toz.
        float toz;
        void Gicirti(float dt)
        {
            var o = match.me; float sp = Mathf.Sqrt(o.vx * o.vx + o.vy * o.vy), psp = Mathf.Sqrt(psvx * psvx + psvy * psvy);
            toz -= dt; if (sp > 2.6f && toz <= 0) { toz = .07f; fx.Burst(Kit.W(o.x, o.y, .05f), new Color(.86f, .94f, .9f, .55f), 2, 40, "dot"); }
            if (psp > 2.4f && psvx * o.vx + psvy * o.vy < psp * sp * .2f && sqT <= 0) { sqT = .25f; sfx.Squeak(); }
            sqT = Mathf.Max(0, sqT - dt); psvx = o.vx; psvy = o.vy;
        }

        void OnEvent(GameEvent e)
        {
            var at = Kit.W(e.x, e.y, e.z); bool mine = e.who == "me"; bool rm = Ayar.reduce; var M = match;
            Color limon = Kit.Hex("#d8ff5a");
            switch (e.kind)
            {
                case Ev.Hit:
                    sfx.Hit(e.type, e.quality, e.slice, e.big, e.serve, mine, (int)e.k, M.flow, M.s != null ? M.s.Speed : 0);   // hız: vuruşun gücü sese biner (Sfx 2026-10-27)
                    fx.Burst(at, Color.white, e.type == "smash" ? 10 : 5, e.type == "smash" ? 220 : 120, "feather");
                    fx.Dalga(at, e.type == "smash" ? limon : Color.white, e.type == "smash" ? 90 : 46);
                    // File önü kill (2026-10-29, "file önü smaç oyunu değiştirsin"): Core onu smaç diye uçurur, gerçek adı e.text'te.
                    // Kısa ve sert: sarsıntı smaçtan büyük, yakınlaşma küçük (file dibinde kadraj kaymasın), halo rengi altın.
                    bool kill = e.text == "kill";
                    if (e.type == "smash") { if (!rm) { shake = Mathf.Max(shake, e.big ? 12 : kill ? 9 : 6); punch = Mathf.Max(punch, e.big ? 1.09f : kill ? 1.03f : 1.05f); } hud.Flash(e.big ? .5f : .2f, e.big ? new Color(1, .55f, .24f) : kill ? Kit.Hex("#ffd27a") : limon); }
                    if (mine) fx.Streak(at, M.flow ? Kit.Hex("#ffd23a") : Color.white, e.type == "smash" ? 16 : M.flow ? 12 : 6);
                    if (!mine && e.type == "smash") Yerel.Hap("tick");   // rakibin smacı: kısa tık (onun vuruşu titremez)
                    if (e.k >= 20 && M.ogr == null) Basarim.Ver(Basarim.RALLI20);
                    break;
                case Ev.Perfect: sfx.Perfect((int)e.k); fx.Burst(at, limon, 18, 280, "spark"); break;
                case Ev.Flow: sfx.Akis(); Yerel.Hap("flow"); if (M.ogr == null) Basarim.Ver(Basarim.AKIS); break;
                case Ev.Aldat: sfx.Aldat(); Yerel.Hap("tick"); break;   // aldatma okunsun diye elde de hissedilir (hareketi azaltta yok)
                case Ev.Split: sfx.Split(); break;
                case Ev.Kurtaris: break;
                case Ev.Imza: sfx.Imza(e.type); Yerel.Hap("tick"); break;   // ışık ve poz PlayerView'de (Athlete.imzaT), yazı Match.Say'den
                case Ev.Okudun: sfx.Okudun(); Yerel.Hap("flow"); fx.Burst(at, Kit.Hex("#7ad7ff"), 14, 200, "spark"); break;
                case Ev.Gorev:
                    if (e.who == "ogr") { sfx.OgrTamam(); Yerel.Hap("flow"); break; }
                    Ayar.gorev++; kaydetBekle = true; sfx.Gorev(); Yerel.Hap("flow"); if (e.k >= 3 && M.ogr == null) Basarim.Ver(Basarim.GOREV3); hud.Say(L.T("Görev tamam: {0}", L.Cumle(e.text)), Kit.W(Court.MID, Court.NET, 2.4f), Kit.Hex("#ffd23a"), 1.15f, true);
                    fx.Burst(Kit.W(Court.MID, Court.NET - 1, 2.4f), Kit.Hex("#ffd23a"), 16, 220, "spark"); break;
                case Ev.Net: net.sign = mine ? -1 : 1; sfx.Net(e.k, e.big); Yerel.Hap("net"); fx.Burst(at, Color.white, 4 + Mathf.RoundToInt(8 * e.k), 60 + 90 * e.k, "feather"); break;
                case Ev.Cord: net.sign = mine ? -1 : 1; sfx.Net(.3f, true); Yerel.Hap("net"); break;
                case Ev.Tape: net.sign = mine ? -1 : 1; sfx.Bant(); break;
                case Ev.Floor: sfx.Floor(e.big); fx.Iz(e.x, e.y, e.big); if (e.big && !rm) shake = Mathf.Max(shake, 5); break;
                case Ev.Hawk: sfx.Hawk(e.type == "in"); Yerel.Hap("hawk"); hud.Hawk(e.x, e.y, e.type == "in", (int)e.k, M.s != null && M.s.serve); break;
                case Ev.Point:
                    {
                        bool smac = mine && e.type == "smash";
                        sfx.Point(mine, smac, (int)e.k); hud.Pop(mine ? 0 : 1); Yerel.Hap(mine ? "win" : "lose");
                        // düşüş yerinde toz ve kazananın renginde dalga; benim sayımda üst bantta üç renkli kıvılcım (HTML point)
                        if (e.x != 0 || e.y != 0) { var yer = Kit.W(e.x, e.y, .05f); fx.Burst(yer, new Color(.9f, 1, .94f, .7f), 14, 140, "dot"); fx.Dalga(yer, mine ? limon : Kit.Hex(Hud.RAKIP_HUD), 70); }
                        if (mine) hud.SayiKivilcim();
                        if (mine && e.big && M.ogr == null) Basarim.Ver(Basarim.SICRAMA);   // sıçrayarak smaçla alınan sayı
                        if (smac) { if (!rm) shake = Mathf.Max(shake, 10); hud.Flash(.35f, limon); }
                        if (mine && (e.k >= 8 || e.type == "smash")) hud.Kamera();
                        hud.Say(L.Cumle(e.text), Kit.W(Court.MID, mine ? Court.NET - 2 : Court.NET + 2, 1.5f), mine ? limon : Kit.Hex(Hud.RAKIP_HUD), mine ? 1.6f : 1.4f, true);
                        if (M.ogr != null && M.s != null && M.s.ogrHedef != null) { }
                        break;
                    }
                case Ev.Text:
                    {
                        Color c = e.type == "jump" ? Kit.Hex("#ff8a3c") : e.who == "me" && (e.type == "smash" || e.type == "kill" || e.onem >= 2) ? limon : e.type == "flow" ? Kit.Hex("#ffd23a") : e.type == "split" ? Kit.Hex("#7ad7ff") : e.who == "op" || e.who == null && e.onem >= 2 ? Kit.Hex("#ffd27a") : Color.white;
                        if (e.text == "Aldattın!") c = Kit.Hex("#7ad7ff");
                        if (e.text != null && e.text.StartsWith("Hedefte")) sfx.Hedefte();
                        if (e.type == "ogr") { hud.Ogr(L.Cumle(e.text)); if (e.text.StartsWith("Güzel") || e.text.StartsWith("Doğru")) sfx.Iyi(); if (e.text.StartsWith("Güzel")) Yerel.Hap("flow"); }
                        else hud.Say(L.Cumle(e.text), at, c, e.type == "jump" ? 1.3f : e.text == "AKIŞ" ? 1.8f : e.onem >= 3 ? 1.2f : 1, e.type == "flow");
                        break;
                    }
                case Ev.MatchEnd: EndMatch(e.who == "me"); break;
            }
        }
    }
}
