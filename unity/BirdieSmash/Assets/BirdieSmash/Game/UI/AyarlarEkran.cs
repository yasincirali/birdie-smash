// Ayarlar (⚙), Nasıl oynanır, Lisanslar (2026-10-26). İçerik bugünkü IMGUI AyarGovde ile aynı, gruplu liste olarak:
//   Oyun: maç uzunluğu (kariyere de uygulanır; zorluk 2026-11-07'de çıktı, Hızlı maç + kariyer kartında; saha yalnız serbest maçta anlamlı olduğu için Hızlı maç'a taşındı).
//   Görünüm: oyuncu görünümü (Ayar.model: Oyuncak 3B, Gerçekçi, Sade; root.ModelDegisti), kamera (Ayar.kamera, GameRoot.KAMERA_SIRA/KAMERA_AD,
//   root.KameraDegisti), yazı boyutu, hareketi azalt (sistem açıksa her zaman açık, kapalıysa buradan zorlanır).
//   Ses. Dil. Game Center · Başarımlar (girişliyse). Yardım: Nasıl oynanır ›, Lisanslar ›. (2026-11-06: "Antrenmanı yeniden oyna" satırı kalktı; aynı eylem
//   Antrenman sekmesinde "Temel ders" kartı ve Nasıl oynanır'da "Temel dersi oyna" düğmesi, tek ad.)
// Anahtarlar aynı (tuytop_*), işleyiciler aynı. Gizli kare hızı göstergesi: başlığa 3 sn içinde 5 dokunuş (HTML ile aynı).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        int ayarDokun; float ayarDokunT; string lisans;
        static readonly string[] DIL_AD = { "Sistem", "Türkçe", "English" };

        // Tek seçimli çip grubunda seçimi yerinde değiştirir: çerçeve sınıfı, ✓, erişim "seçili" ve altındaki tek soluk cümle.
        // Sayfa kurulmaz (2026-11-19, takılma kuralı): dokunuşta ayırma bir ✓ ikonu ve bir string.
        void SecimYerinde(VisualElement satir, int secili, Label acik, string[] acikTablo, string[] adTablo)
        {
            for (int j = 0; j < satir.childCount; j++)
            {
                var d = satir[j]; bool s = j == secili; d.EnableInClassList("cip--secili", s);
                var ic = d.Q(null, "cip-ic"); var tk = d.Q(null, "cip-tik");
                if (s && tk == null && ic != null) { tk = Ikon(Ikonlar.Tik, 16, KOYU); tk.AddToClassList("cip-tik"); ic.Insert(0, tk); }
                else if (!s && tk != null) tk.RemoveFromHierarchy();
                // Açıklaması olmayan grup (Dil) yalnız adı okutur.
                if (j < adTablo.Length && acikTablo == null) Erisim.Etiket(d, L.T(adTablo[j]), "dugme", s);
                else if (j < adTablo.Length && j < acikTablo.Length) Erisim.Etiket(d, L.T(adTablo[j]) + ". " + L.T(acikTablo[j]), "dugme", s);
            }
            if (acik != null && acikTablo != null && secili >= 0 && secili < acikTablo.Length) { acik.text = L.T(acikTablo[secili]); acik.style.display = DisplayStyle.Flex; }
        }

        void AyarlarKur(VisualElement p)
        {
            var t = SayfaBasi(p, "Ayarlar");
            t.RegisterCallback<PointerDownEvent>(e =>
            {
                if (ayarDokun == 0 || Time.unscaledTime - ayarDokunT > 3) { ayarDokun = 0; ayarDokunT = Time.unscaledTime; }
                if (++ayarDokun >= 5) { ayarDokun = 0; if (root.HudKatman != null) root.HudKatman.FpsAc(!Ayar.fps); }
            });
            var sv = Kaydirma(p); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            // 2026-11-07 tek tip bölüm başlığı (yasin "yap", arayüz cilası): Ayarlar'daki 17 pt "bolum" başlıkları diğer ekranlardaki
            // Grup başlığıyla aynı (tek stil, VoiceOver'da başlık rolü). Hiyerarşi boşlukla okunur.
            // 2026-11-08 (inceleme turu): "Oyun" grubu kalktı. Tek içeriği maç uzunluğuydu; artık Hızlı maç sayfasında ve kariyer
            // maç ayrıntısında, oynamadan hemen önce (zorlukla aynı karar, 2026-11-07). Ayar ve anahtar aynı.
            // 2026-11-07 (inceleme 16:05 #5): içeriksiz "Görünüm" başlığı kalktı; hemen altındaki "Oyuncu görünümü" ile iki başlık
            // art arda duruyordu (VoiceOver'da iki boş başlık).
            // 2026-11-10 (fikir 7 Kas 18:05 #4, yasin "üçünü de al"): "Oyuncu görünümü" model çipleri Dolap'a taşındı (UiKok.ModelCipleri,
            // Dolap › Beden ve tek bedenli modellerde bilgi kartı). Kimlik kararı giydirmenin ilk adımı; Ayarlar'la Dolap arasında mekik bitti.
            // Kayıt anahtarı ve değerleri aynı (tuytop_model): kimsenin seçimi değişmez.
            // Oyuncu hareketi (2026-11-14, yasin: "manuel hareket ayrı bir özellik olarak beklesin"): zorluk yalnız rakibi seçer,
            // koşu burada. Üç eşit çip + seçilinin tek soluk cümlesi (kamera/kaydırma kalıbı). Maç sırasında değişmez.
            // Hareket, kaydırma ve kamera (2026-11-19 inceleme, fikir 02:05 Ayarlar #3): hiçbir sekmede görünmezler (salon kamerayı
            // kendisi izler), dokunuşta menü kurulmaz; seçim ve soluk cümle yerinde değişir (SecimYerinde). Degisti() yalnız yazı boyutunda.
            c.Add(Grup(L.T("Oyuncu hareketi"))); var hr = Satir("sigdir-grup"); c.Add(hr);
            Label hrA = null;
            for (int i = 0; i < Ayar.HAREKET_AD.Length; i++)
            {
                int id = i; var hc = Cip(L.T(Ayar.HAREKET_AD[i]), Ayar.hareket == id, () => { Ayar.hareket = id; Ayar.KaydetSonra(); SecimYerinde(hr, id, hrA, HAREKET_ACIK, Ayar.HAREKET_AD); });
                Erisim.Etiket(hc, L.T(Ayar.HAREKET_AD[i]) + ". " + L.T(HAREKET_ACIK[i]), "dugme", Ayar.hareket == id); hr.Add(hc);
            }
            hrA = Yazi(L.T(HAREKET_ACIK[Mathf.Clamp(Ayar.hareket, 0, 2)]), 12, "soluk"); c.Add(hrA);
            // 2026-11-10 inceleme (fikir 02:05 #2): "Hareket" (tek çip) ve "Ses" (üç çip) tek "Ses ve his" grubunda, dört anahtar
            // satırı; ad sabit, durum anahtarda. Tek seçimli gruplar (görünüm, kamera, yazı boyutu, dil) çip kalır. Davranış ve
            // tuytop_* anahtarları aynı: Müzik ayrı (2026-10-30, ses kapalıyken müzik de susar, seçim korunur); Titreşim
            // Hareketi azalt'tan bağımsız, açılınca örnek tık (2026-11-07; Duraklat'a eklenmez); sistem hareketi azalttıysa satır
            // pasif, altında "Sistem ayarından".
            // Kaydırma (2026-11-12 inceleme, fikir 07:05 #5): vuruş eşiği üç kademe; seçili kademenin tek soluk cümlesi (kamera kalıbı).
            c.Add(Grup(L.T("Kaydırma"))); var kd = Satir(); kd.AddToClassList("cip-sarar"); c.Add(kd);
            // 2026-11-19 inceleme (fikir 02:05 Ayarlar #5): Normal'in cümlesi "Varsayılan eşik" idi; geliştirici sözcüğü, en çok seçilen
            // kademe oyuncuya bir şey söylemiyordu. Öbür ikisi gibi elden konuşur.
            string[] Ka = { "Kısa", "Normal", "Uzun" }, KaAcik = { "Kısa ve yavaş kaydırma da vuruş sayılır", "Orta hızda bir kaydırma vuruş sayılır", "Yalnız uzun, hızlı kaydırma vuruş sayılır" };
            Label kdA = null;
            for (int i = 0; i < 3; i++) { int id = i; var kc0 = Cip(L.T(Ka[i]), Ayar.kaydirma == id, () => { Ayar.kaydirma = id; Ayar.KaydetSonra(); SecimYerinde(kd, id, kdA, KaAcik, Ka); }); Erisim.Etiket(kc0, L.T(Ka[i]) + ". " + L.T(KaAcik[i]), "dugme", Ayar.kaydirma == id); kd.Add(kc0); }
            kdA = Yazi(L.T(KaAcik[Mathf.Clamp(Ayar.kaydirma, 0, 2)]), 12, "soluk"); c.Add(kdA);
            c.Add(Grup(L.T("Kamera"))); var km = Satir(); c.Add(km);
            Label kmA = null;
            for (int i = 0; i < GameRoot.KAMERA_SIRA.Length; i++)
            {
                string id = GameRoot.KAMERA_SIRA[i]; int ki = i;
                var kc = Cip(L.T(GameRoot.KAMERA_AD[i]), Ayar.kamera == id, () => { Ayar.kamera = id; Ayar.KaydetSonra(); root.KameraDegisti(); SecimYerinde(km, ki, kmA, KAMERA_ACIK, GameRoot.KAMERA_AD); });
                Erisim.Etiket(kc, L.T(GameRoot.KAMERA_AD[i]) + ". " + L.T(KAMERA_ACIK[i]), "dugme", Ayar.kamera == id); km.Add(kc);
            }
            // 2026-11-10 inceleme (fikir 07:06 #3): çipler yalnız ad söylüyordu, farkı görmek için maça girip çıkmak gerekiyordu.
            // Zorluk satırının kalıbı: seçili açının tek soluk cümlesi (cümleler GameRoot kamera kadraj yorumundan).
            int ks = System.Array.IndexOf(GameRoot.KAMERA_SIRA, Ayar.kamera);
            kmA = Yazi(ks >= 0 ? L.T(KAMERA_ACIK[ks]) : "", 12, "soluk"); kmA.style.display = ks >= 0 ? DisplayStyle.Flex : DisplayStyle.None; c.Add(kmA);
            c.Add(Grup(L.T("Ses ve his")));
            // İnceleme 2026-11-15 12:05 #4: dört ayrı cam düğme yerine tek liste grubu (§6.4); yenileme geçiş bitince (DegistiSonra).
            // Hareketi azalt kendi değerini değiştirdiği için DegistiSonra açılırken bekler, kapanırken hemen yeniler: ikisi de doğru.
            // 2026-11-22 inceleme (fikir 17:06 #1, takılma): Ses, Müzik ve Titreşim de DegistiSonra çağırıyordu; 200 ms sonra dört
            // sekme kirleniyor, üst şerit ve Ayarlar baştan kuruluyordu (art arda üç anahtar = üç tam kurulum). Hiçbir menü yüzeyi
            // bu üç ayarı okumuyor (müzik Sfx.MuzikIsle her karede bakıyor), anahtar kendini yerinde çiziyor: yalnız kayıt.
            // 2026-11-23 inceleme (fikir 17:06 #6, HIG bağlı denetim): Ses kapalıyken müzik, seyirci örneği ve düğme sesi duyulmuyor ama
            // satırlar tam opaktı; oyuncu Seyirci'yi değiştirip hiçbir şey duymuyordu. Üç satır soluk (anahtar-satir--pasif), seçimleri
            // korunur ve dokunulabilir kalır; sınıf Ses dokunuşunda yerinde değişir (sayfa kurulmaz, 22 Kas kararı).
            var sesBagli = new VisualElement[3];
            c.Add(ListeGrup(
                AnahtarSatir(L.T("Ses"), Ayar.ses, () => { Ayar.ses = !Ayar.ses; AudioListener.volume = Ayar.ses ? 1 : 0; Ayar.KaydetSonra(); foreach (var sb in sesBagli) sb.EnableInClassList("anahtar-satir--pasif", !Ayar.ses); }),
                sesBagli[0] = AnahtarSatir(L.T("Müzik"), Ayar.muzik, () => { Ayar.muzik = !Ayar.muzik; Ayar.KaydetSonra(); }),
                AnahtarSatir(L.T("Titreşim"), Ayar.titresim, () => { Ayar.titresim = !Ayar.titresim; Ayar.KaydetSonra(); if (Ayar.titresim) Yerel.Hap("tick"); }),
                AnahtarSatir(L.T("Hareketi azalt"), Ayar.reduce, Ayar.reduceSistem ? (System.Action)null : () => { Ayar.reduceSecim = !Ayar.reduceSecim; Ayar.SistemOku(); Ayar.KaydetSonra(); DegistiSonra(); },
                    Ayar.reduceSistem ? L.T("Sistem ayarından") : null),
                // Seyirci listenin beşinci satırı (2026-11-16 inceleme, fikir 17:05 #6): ayrı "Seyirci" başlığı + üç çip bir ses ayarı
                // için sayfaya bir başlık ve bir satır ekliyordu; değerli satır Kapalı → Az → Normal döner. tuytop_seyirci ve
                // değerleri aynı. Sayfa yenilenmez (değer satırın kendisinde yazılı).
                // 2026-11-22 inceleme (fikir 17:06 #3): ses ayarı sesle doğrulanır; seçilen düzeyde tek kısa alkış (Kapalı'da ve Ses
                // kapalıyken sessiz). Klip menü durgunken ısıtılmış olandır, ısınmadıysa çalmaz (dokunuşta sentez yok).
                sesBagli[1] = DegerSatir(L.T("Seyirci"), new[] { L.T("Kapalı"), L.T("Az"), L.T("Normal") }, Ayar.seyirci, v => { Ayar.seyirci = v; Ayar.KaydetSonra(); root.sfx.SeyirciOrnek(); }),
                // 2026-11-22 (yasin "o sesi de özel seçmek istiyorum"): düğme sesi paketi; her dokunuş sıradakine geçer ve onu çalar.
                sesBagli[2] = DegerSatir(L.T("Düğme sesi"), System.Array.ConvertAll(Sfx.PAKETLER, x => L.T(x)), Ayar.dugmeSes, v => { Ayar.dugmeSes = v; Ayar.KaydetSonra(); root.sfx.UiSes("bas"); })));
            foreach (var sb in sesBagli) sb.EnableInClassList("anahtar-satir--pasif", !Ayar.ses);
            // Sıra anlamla (2026-11-14 inceleme, fikir 22:05 #3): oyun hissi (hareket, kaydırma, kamera) → ses → görünüm (yazı boyutu,
            // dil). Yazı boyutu eskiden kamera ile kaydırmanın arasındaydı; "vuruş tutmuyor" diyen oyuncu hareket ve kaydırmayı yan
            // yana bulur. Yazı boyutu da diğer çipli gruplar gibi seçilinin tek soluk cümlesini alır. Anahtarlar aynı.
            c.Add(Grup(L.T("Yazı boyutu"))); var yz = Satir(); c.Add(yz);
            string[] Yk = { "sistem", "normal", "buyuk" }, Ya = { "Sistem", "Normal", "Büyük" };
            // 2026-11-20 inceleme (fikir 12:06 #2, takılma kuralı): dokunuş Degisti() çağırıyordu; ölçek değişince bir sonraki karede
            // HepsiniKur zaten her şeyi kuruyor, yani Ayarlar ve üst çubuk iki kez kuruluyordu. Artık seçim yerinde değişir; ölçek
            // değiştiyse tek kurulum UiKok'taki MenuYaziK denetiminden gelir, değişmediyse (sistem 1,0 iken Normal) hiç kurulum yok.
            var yzA = Yazi("", 12, "soluk"); int ys = System.Array.IndexOf(Yk, Ayar.yazi); ys = ys < 0 ? 0 : ys;
            for (int i = 0; i < 3; i++) { string id = Yk[i]; int ii = i; var yc = Cip(L.T(Ya[i]), Ayar.yazi == id, () => { Ayar.yazi = id; Ayar.KaydetSonra(); SecimYerinde(yz, ii, yzA, YAZI_ACIK, Ya); }); Erisim.Etiket(yc, L.T(Ya[i]) + ". " + L.T(YAZI_ACIK[i]), "dugme", Ayar.yazi == id); yz.Add(yc); }
            yzA.text = L.T(YAZI_ACIK[ys]); c.Add(yzA);
            c.Add(Grup(L.T("Dil"))); var dl = Satir(); c.Add(dl);
            // "Sistem" (2026-11-12): iOS'un uygulama başına dil ayarını izler; Yazı satırındaki Sistem çipiyle aynı kalıp.
            // 2026-11-20 inceleme (fikir 12:06 #1): sayfa yalnız dil DEĞİŞİNCE yeniden kuruluyor (UiKok sonDil). Telefon Türkçe iken
            // Türkçe → Sistem dokunuşu dili değiştirmiyor, ✓ eski çipte kalıyordu ama Sistem kaydediliyordu. Artık ✓ yerinde taşınır;
            // dil değiştiyse bugünkü tek HepsiniKur yolu her şeyi o dilde kurar.
            dl.Add(Cip(L.T("Sistem"), Ayar.dilSistem, () => { Ayar.dilSistem = true; Ayar.dil = Ayar.SistemDil(); Ayar.KaydetSonra(); SecimYerinde(dl, 0, null, null, DIL_AD); }));
            dl.Add(Cip("Türkçe", !Ayar.dilSistem && Ayar.dil == "tr", () => { Ayar.dilSistem = false; Ayar.dil = "tr"; Ayar.KaydetSonra(); SecimYerinde(dl, 1, null, null, DIL_AD); }));
            dl.Add(Cip("English", !Ayar.dilSistem && Ayar.dil == "en", () => { Ayar.dilSistem = false; Ayar.dil = "en"; Ayar.KaydetSonra(); SecimYerinde(dl, 2, null, null, DIL_AD); }));
            // Game Center düğmesi buradan kalktı (2026-11-07 inceleme, fikir 20:09 #3): başlıksız, "Dil" grubunun parçası gibi
            // okunuyordu ve Profil › Başarımlar'da aynısı vardı. Başarımı gören oyuncu oradan geçer; Ayarlar bir öğe kısa.
            c.Add(Grup(L.T("Yardım")));
            c.Add(Dugme(L.T("Nasıl oynanır") + "  ›", () => SayfaAc(Gezinti.NASIL), null, L.T("Vuruş, servis, nefes")));
            c.Add(Dugme(L.T("Lisanslar ve üçüncü taraf içerik") + "  ›", () => SayfaAc(Gezinti.LISANS)));
            // Gizlilik bağlantısı + sürüm (2026-11-07 inceleme, fikir 00:03 #1): Guideline 5.1.1(i) gizlilik politikasını uygulama
            // içinde de kolay erişilebilir ister; IAP'li oyunda inceleme arar. Sayfa store_listing/*/privacy_url.txt ile aynı adres.
            // 2026-11-07 (murphysoft.app HTTPS ile açıldı): gizlilik ve destek stüdyo sitesinde; Destek sayfası support@murphysoft.app'i
            // ve SSS'yi taşır (e-posta bağlantısı uygulamada değil sayfada: mailto her cihazda posta uygulaması açmaz).
            bool en = Ayar.dil == "en";
            // "  ↗" ekini Dugme ayırır ve etikete "Tarayıcıda açılır" ekler (2026-11-22, fikir 17:06 #2); elle etiket gerekmez.
            c.Add(Dugme(L.T("Gizlilik politikası") + "  ↗", () => { Application.OpenURL(en
                ? "https://murphysoft.app/birdie-smash/privacy/" : "https://murphysoft.app/birdie-smash/gizlilik/"); }));
            c.Add(Dugme(L.T("Destek") + "  ↗", () => { Application.OpenURL(en
                ? "https://murphysoft.app/birdie-smash/support/" : "https://murphysoft.app/birdie-smash/destek/"); }));
            c.Add(Bosluk(8)); c.Add(Yazi(L.T("Sürüm {0}", Application.version), 12, "soluk"));
            // Tanı (2026-11-11, Cokme.cs): yalnız gizli FPS katmanı açıkken. 2026-11-15: önceki oturumun izi her zaman, yeni çökme
            // varsa en üstte; eski çökme kaydı altta. Kayıt cihazda
            // kalır; paylaş düğmesi sistem sayfasıyla metni testçinin seçtiği yere gönderir, "Sil" kaydı kaldırır.
            if (Ayar.fps && Cokme.Son != null)
            {
                c.Add(Grup(L.T("Tanı")));
                string ilk = Cokme.Son.Length > 160 ? Cokme.Son.Substring(0, 160) + "…" : Cokme.Son;
                c.Add(Yazi(ilk, 12, "soluk"));
                c.Add(Dugme(L.T("Çökme kaydını paylaş"), () => { Yerel.Paylas(Cokme.Son); }));
                c.Add(Dugme(L.T("Çökme kaydını sil"), () => { Cokme.Temizle(); Degisti(); }));
            }
            c.Add(Bosluk(24));
        }
        // Dil değişince Update tüm menüyü yeniden kurar (sonDil); Ayarlar sayfası açık kalır.

        void Antrenmana() { var once = gez.sekme; gez.Kok(); Uygula(once, false); root.Antrenman(); }

        static readonly string[] YAZI_ACIK = { "iPhone'un yazı boyutunu izler", "Hep aynı boyut", "En az %130, iPhone daha büyükse onu izler" };   // sistem, normal, buyuk (Ayar.MenuYaziK)
        static readonly string[] KAMERA_ACIK = {   // GameRoot.KAMERA_SIRA sırasıyla
            "TV yayın açısı: yüksekten, kortun tamamı görünür",
            "Omzunun gerisinden: rakip ve file büyük, derinlik hissi güçlü",
            "Kuşbakışına yakın: topun ineceği yer ve çizgiler en net" };

        static readonly string[,] NASIL = {
            {"Koş","Kolay ve Orta'da oyuncu topa kendisi koşar, senin işin yalnız vuruş; kısa bir dokunuşla yerini değiştirebilirsin. Zor ve Usta'da koşmak sende: dokun = oraya koş, yavaş sürükle = parmağı izle. Zor'da geç kalırsan oyuncun topa kendisi koşar, dokunursan yine sen yönlendirirsin"},
            {"Forehand / backhand","Top raket tarafında gelirse forehand, öbür tarafta backhand. Backhand clear kısa kalır, drive yavaşlar, smaç olmaz; topu forehand tarafına alacak şekilde dur"},
            {"Akış","Üst üste 4 tam orta: çizgiler altın olur, hedef büyür, şarkıya armoni girer"},
            {"Derinlik","Topun yerdeki gölgesi ve ona bağlı ince çizgi yüksekliği gösterir. Top sana inerken son saniyede yer çizgisi ve büzülen iniş halkası belirir; kalın ve ortası noktalıysa yetişiyorsun, inceyse yetişmiyorsun"},
            {"Nefes","Smaç, sıçrama ve uzanma nefes yakar; nefesin düşünce yavaşlarsın ve smaç zayıflar. Clear atıp sayı arasında dinlen. Topu tepeden alırsan smaç dikleşir"},
            {"Ekipman","Raket, tel gerginliği, bileklik, bant ve forma oyunun dinamiklerini, raket sesini ve titreşimi değiştirir"},
            // 2026-10-26 ekipman v2 ve karakter: 5 çubuk ve tarz bir satırla anlatılır (yalnız Unity; HTML donduruldu)
            {"Çubuklar ve tarz","Atölye'de takımının 5 çubuğu var (50 nötr); her modelin kartı artısını ve eksisini tek cümleyle yazar, en iyi parça yok. Gelişim puanların çubuğun ucuna ayrı renkle ve +n yazısıyla eklenir. Tarzın bir artısı ve bir eksisi var, en iyi tarz yok; fark yarattığı an kortta adıyla yazılır. Ücretsiz, istediğin an değiştir"}};
        // NASIL satırlarının karşılık görevi (veri/antrenman.json id'leri; null = görev yok). Sıra NASIL ile aynı.
        static readonly string[] NASIL_GOREV = { "split", "drive", null, "derin", null, null, null };
        void NasilKur(VisualElement p)
        {
            SayfaBasi(p, "Nasıl oynanır");
            var sv = Kaydirma(p); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            // 2026-11-22 inceleme (fikir 17:06 #6): düğmenin altında ayrı bir kart "…; aşağıdakileri öğretmez." diyordu (neyin
            // olmadığını söyleyen cümle, "o hâlde neden ders?"). Kontrolün tek cümlesi birincil düğmenin alt satırına indi.
            c.Add(Dugme(L.T("Temel dersi oyna"), Antrenmana, "dugme--birincil", L.T("Kaydır = vur, hız = güç.")));
            var k = Kart(); c.Add(k);
            // 2026-11-12 (inceleme 00:05 #5, sadeleştirme): yedi başlık açık metin olarak diziliyordu, okuyan oyuncu uzun metni
            // geçiyordu. Başlıklar tek satır (≥44 pt, › ile, açıkken döner; yalnız renkle değil), dokununca açılır, aynı anda bir tane açık.
            // Karşılığı olan görev varsa açık maddenin altında "Görevde dene ›" görev kartını açar (kilit/ödül kuralı kartın kendi
            // kuralı). Karşılığı olmayan madde (Akış, Nefes, Ekipman, Çubuklar) yalnız metin.
            var govde = new List<VisualElement>(); var oklar = new List<Label>(); var satirlar = new List<VisualElement>();
            for (int i = 0; i < NASIL.GetLength(0); i++)
            {
                int ii = i; string bas = L.T(NASIL[i, 0]);
                var sr = new VisualElement(); sr.AddToClassList("nasil-satir"); k.Add(sr); satirlar.Add(sr);
                var b = Yazi(bas, 16, "kalin"); b.AddToClassList("nasil-baslik"); b.pickingMode = PickingMode.Ignore; sr.Add(b);
                var ok = Yazi("›", 18, "nasil-ok"); ok.pickingMode = PickingMode.Ignore; sr.Add(ok); oklar.Add(ok);
                Erisim.Etiket(sr, bas, "dugme", false, false, L.T("kapalı"));
                // 2026-11-22 inceleme (fikir 17:06 #5): gövde bir karede açılıyordu. Kapalı gövde hep solgun durur (sönmesi
                // görünmezken biter), açılınca .12 sn belirir; hareketi azaltta sınıf hiç eklenmez.
                var g = new VisualElement(); g.style.display = DisplayStyle.None; g.AddToClassList("ant-liste"); if (!Ayar.reduce) g.AddToClassList("koz-solgun"); k.Add(g); govde.Add(g);
                g.Add(Yazi(L.T(NASIL[i, 1]), 14));
                string gid = NASIL_GOREV[i];
                if (gid != null && Dril.Bul(gid) != null)
                {
                    var dene = Yazi(L.T("Görevde dene ›"), 15, "kalin"); dene.AddToClassList("nasil-dene"); Erisim.Etiket(dene, L.T("Görevde dene"), "dugme");
                    dene.AddManipulator(new Clickable(() => { Tik(); DrilKartAc(gid); })); g.Add(dene);
                }
                sr.AddManipulator(new Clickable(() =>
                {
                    // 2026-11-22 inceleme (fikir 17:06 #5): yalnız titreşim veriyordu, öbür düğmeler Tik() (ses + titreşim, §6.2).
                    // Ok metni "›" ↔ "⌄" değişiyordu (⌄ yazı tipinde doğrulanmadı); artık hep "›", açıkken 90° döner (.15 sn).
                    Tik(); bool ac = govde[ii].style.display == DisplayStyle.None;
                    for (int j = 0; j < govde.Count; j++)
                    {
                        bool a = ac && j == ii, once = govde[j].style.display == DisplayStyle.Flex; govde[j].style.display = a ? DisplayStyle.Flex : DisplayStyle.None; oklar[j].EnableInClassList("nasil-ok--acik", a);
                        if (!Ayar.reduce && a != once) { var gj = govde[j]; if (a) gj.schedule.Execute(() => gj.RemoveFromClassList("koz-solgun")).StartingIn(16); else gj.AddToClassList("koz-solgun"); }
                        Erisim.Etiket(satirlar[j], L.T(NASIL[j, 0]), "dugme", false, false, L.T(a ? "açık" : "kapalı"));
                    }
                }));
            }
            c.Add(Bosluk(24));
        }

        void LisansKur(VisualElement p)
        {
            if (lisans == null) { var t = Resources.Load<TextAsset>("lisanslar"); lisans = t != null ? t.text : "Lisans metni bulunamadı (Resources/lisanslar.txt)."; }
            SayfaBasi(p, "Lisanslar");
            var sv = Kaydirma(p); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            var k = Kart(); c.Add(k); var l = Yazi(lisans, 13); l.enableRichText = false; k.Add(l); c.Add(Bosluk(24));
        }
    }
}
