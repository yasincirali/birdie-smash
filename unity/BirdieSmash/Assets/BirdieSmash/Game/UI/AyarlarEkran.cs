// Ayarlar (⚙), Nasıl oynanır, Lisanslar (2026-10-26). İçerik bugünkü IMGUI AyarGovde ile aynı, gruplu liste olarak:
//   Oyun: maç uzunluğu (kariyere de uygulanır; zorluk 2026-11-07'de çıktı, Hızlı maç + kariyer kartında; saha yalnız serbest maçta anlamlı olduğu için Hızlı maç'a taşındı).
//   Görünüm: oyuncu görünümü (Ayar.model: Oyuncak 3B, Gerçekçi, Sade; root.ModelDegisti), kamera (Ayar.kamera, GameRoot.KAMERA_SIRA/KAMERA_AD,
//   root.KameraDegisti), yazı boyutu, hareketi azalt (sistem açıksa her zaman açık, kapalıysa buradan zorlanır).
//   Ses. Dil. Game Center · Başarımlar (girişliyse). Yardım: Nasıl oynanır ›, Lisanslar ›. (2026-11-06: "Antrenmanı yeniden oyna" satırı kalktı; aynı eylem
//   Antrenman sekmesinde "Temel ders" kartı ve Nasıl oynanır'da "Temel dersi oyna" düğmesi, tek ad.)
// Anahtarlar aynı (tuytop_*), işleyiciler aynı. Gizli kare hızı göstergesi: başlığa 3 sn içinde 5 dokunuş (HTML ile aynı).
using UnityEngine;
using UnityEngine.UIElements;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class UiKok
    {
        int ayarDokun; float ayarDokunT; string lisans;

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
            c.Add(Grup(L.T("Oyuncu görünümü"))); var m = Satir(); c.Add(m);
            // r56 (2026-10-29): Tüycük'le dört çip tek satırda "Oyun cak 3B" diye bölünüyordu (tarama: kelime 58 > 36 px); 2×2 sarar.
            m.style.flexWrap = Wrap.Wrap;
            // 2026-10-27 (yasin build 41: "3B model kaybolmuş, seçmeme rağmen görmedim"): Sade üçüncü seçenek (build 40 notu
            // vaat ediyordu, FBX vardı, çip yoktu). Karakter odasındaki tip/saç/ayakkabı modeli yalnız Oyuncak 3B'de görünür;
            // başka model seçiliyken bu, küçük gri notla değil, çiplerin hemen altında açıkça yazılır.
            for (int i = 0; i < Ayar.MODELLER.Length; i++)
            {
                // 2026-11-07 (yasin "yap": dört seçenek ikiye): seçenekte yalnız Oyuncak 3B ve Tüycük; Gerçekçi ya da Sade seçmiş
                // oyuncunun seçimi silinmez, kendi çipi de görünmeye devam eder (geri dönüp bırakabilsin). Online kartı dört modeli de tanır.
                string id = Ayar.MODELLER[i]; if (!Ayar.ModelVar(id)) continue; if (!Ayar.ModelSecenek(id) && Ayar.model != id) continue;
                var ci = Cip(L.T(Ayar.MODEL_AD[i]), Ayar.model == id, () => { Ayar.model = id; Ayar.Kaydet(); root.ModelDegisti(); Degisti(); });
                ci.style.flexBasis = new StyleLength(new Length(40, LengthUnit.Percent)); m.Add(ci);
            }
            // 2026-10-27: Gerçekçi'de ayakkabı modeli, şort ve kumaş da görünür; not neyin sabit neyin değiştiğini söyler.
            if (Ayar.model == "rocketbox") c.Add(Yazi(L.T("Gerçekçi: beden, yüz ve saç sabit; ayakkabı, forma, şort, kafa bandı, bileklik ve raket değişir."), 13, "ipucu"));
            // 2026-10-29 Tüycük: tüy rengi forma renginden; ayakkabı modeli görünür (Oyuncak'ın parçaları), beden ve saç tek.
            else if (Ayar.model == "tuycuk") c.Add(Yazi(L.T("Tüycük: beden ve yüz sabit, tüy rengi forma renginden gelir; ayakkabı, kafa bandı, bileklik ve raket değişir."), 13, "ipucu"));
            else if (!Ayar.VaryantGorunur) c.Add(Yazi(L.T("Beden tipi, saç biçimi ve ayakkabı modeli yalnız Oyuncak 3B'de görünür."), 13, "ipucu"));
            c.Add(Grup(L.T("Kamera"))); var km = Satir(); c.Add(km);
            for (int i = 0; i < GameRoot.KAMERA_SIRA.Length; i++)
            {
                string id = GameRoot.KAMERA_SIRA[i];
                km.Add(Cip(L.T(GameRoot.KAMERA_AD[i]), Ayar.kamera == id, () => { Ayar.kamera = id; Ayar.Kaydet(); root.KameraDegisti(); Degisti(); }));
            }
            c.Add(Grup(L.T("Yazı boyutu"))); var yz = Satir(); c.Add(yz);
            string[] Yk = { "sistem", "normal", "buyuk" }, Ya = { "Sistem", "Normal", "Büyük" };
            for (int i = 0; i < 3; i++) { string id = Yk[i]; yz.Add(Cip(L.T(Ya[i]), Ayar.yazi == id, () => { Ayar.yazi = id; Ayar.Kaydet(); Degisti(); })); }
            // 2026-11-10 inceleme (fikir 02:05 #2): "Hareket" (tek çip) ve "Ses" (üç çip) tek "Ses ve his" grubunda, dört anahtar
            // satırı; ad sabit, durum anahtarda. Tek seçimli gruplar (görünüm, kamera, yazı boyutu, dil) çip kalır. Davranış ve
            // tuytop_* anahtarları aynı: Müzik ayrı (2026-10-30, ses kapalıyken müzik de susar, seçim korunur); Titreşim
            // Hareketi azalt'tan bağımsız, açılınca örnek tık (2026-11-07; Duraklat'a eklenmez); sistem hareketi azalttıysa satır
            // pasif, altında "Sistem ayarından".
            c.Add(Grup(L.T("Ses ve his")));
            c.Add(AnahtarSatir(L.T("Ses"), Ayar.ses, () => { Ayar.ses = !Ayar.ses; AudioListener.volume = Ayar.ses ? 1 : 0; Ayar.Kaydet(); Degisti(); }));
            c.Add(AnahtarSatir(L.T("Müzik"), Ayar.muzik, () => { Ayar.muzik = !Ayar.muzik; Ayar.Kaydet(); Degisti(); }));
            c.Add(AnahtarSatir(L.T("Titreşim"), Ayar.titresim, () => { Ayar.titresim = !Ayar.titresim; Ayar.Kaydet(); if (Ayar.titresim) Yerel.Hap("tick"); Degisti(); }));
            c.Add(AnahtarSatir(L.T("Hareketi azalt"), Ayar.reduce, Ayar.reduceSistem ? (System.Action)null : () => { Ayar.reduceSecim = !Ayar.reduceSecim; Ayar.SistemOku(); Ayar.Kaydet(); Degisti(); },
                Ayar.reduceSistem ? L.T("Sistem ayarından") : null));
            c.Add(Grup(L.T("Dil"))); var dl = Satir(); c.Add(dl);
            dl.Add(Cip("Türkçe", Ayar.dil == "tr", () => { Ayar.dil = "tr"; Ayar.Kaydet(); }));
            dl.Add(Cip("English", Ayar.dil == "en", () => { Ayar.dil = "en"; Ayar.Kaydet(); }));
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
            var gz = Dugme(L.T("Gizlilik politikası") + "  ↗", () => { Yerel.Hap("tick"); Application.OpenURL(en
                ? "https://murphysoft.app/birdie-smash/privacy/" : "https://murphysoft.app/birdie-smash/gizlilik/"); });
            Erisim.Etiket(gz, L.T("Gizlilik politikası") + ". " + L.T("Tarayıcıda açılır"), "dugme"); c.Add(gz);
            var ds = Dugme(L.T("Destek") + "  ↗", () => { Yerel.Hap("tick"); Application.OpenURL(en
                ? "https://murphysoft.app/birdie-smash/support/" : "https://murphysoft.app/birdie-smash/destek/"); });
            Erisim.Etiket(ds, L.T("Destek") + ". " + L.T("Tarayıcıda açılır"), "dugme"); c.Add(ds);
            c.Add(Bosluk(8)); c.Add(Yazi(L.T("Sürüm {0}", Application.version), 12, "soluk"));
            c.Add(Bosluk(24));
        }
        // Dil değişince Update tüm menüyü yeniden kurar (sonDil); Ayarlar sayfası açık kalır.

        void Antrenmana() { var once = gez.sekme; gez.Kok(); Uygula(once, false); root.Antrenman(); }

        static readonly string[,] NASIL = {
            {"Koş","Kolay ve Orta'da oyuncu topa kendisi koşar, senin işin yalnız vuruş; kısa bir dokunuşla yerini değiştirebilirsin. Zor ve Usta'da koşmak sende: dokun = oraya koş, yavaş sürükle = parmağı izle. Zor'da geç kalırsan oyuncun topa kendisi koşar, dokunursan yine sen yönlendirirsin"},
            {"Forehand / backhand","Top raket tarafında gelirse forehand, öbür tarafta backhand. Backhand clear kısa kalır, drive yavaşlar, smaç olmaz; topu forehand tarafına alacak şekilde dur"},
            {"Akış","Üst üste 4 tam orta: çizgiler altın olur, hedef büyür, şarkıya armoni girer"},
            {"Derinlik","Topun yerdeki gölgesi ve ona bağlı ince çizgi yüksekliği gösterir. Top sana inerken son saniyede yer çizgisi ve büzülen iniş halkası belirir; yeşilse yetişiyorsun"},
            {"Nefes","Smaç, sıçrama ve uzanma nefes yakar; nefesin düşünce yavaşlarsın ve smaç zayıflar. Clear atıp sayı arasında dinlen. Topu tepeden alırsan smaç dikleşir"},
            {"Ekipman","Raket, tel gerginliği, bileklik, bant ve forma oyunun dinamiklerini, raket sesini ve titreşimi değiştirir"},
            // 2026-10-26 ekipman v2 ve karakter: 5 çubuk ve tarz bir satırla anlatılır (yalnız Unity; HTML donduruldu)
            {"Çubuklar ve tarz","Atölye'de takımının 5 çubuğu var (50 nötr); her modelin kartı artısını ve eksisini tek cümleyle yazar, en iyi parça yok. Tarzın bir artısı ve bir eksisi var, en iyi tarz yok; fark yarattığı an kortta adıyla yazılır. Ücretsiz, istediğin an değiştir"}};
        void NasilKur(VisualElement p)
        {
            SayfaBasi(p, "Nasıl oynanır");
            var sv = Kaydirma(p); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            c.Add(Dugme(L.T("Temel dersi oyna"), Antrenmana, "dugme--birincil"));
            var k = Kart(); c.Add(k);
            k.Add(Yazi(L.T("Kaydır = vur, hız = güç. Antrenman vuruşları öğretir; aşağıdakileri öğretmez."), 15));
            for (int i = 0; i < NASIL.GetLength(0); i++)
            {
                var b = Yazi(L.T(NASIL[i, 0]), 16, "kalin"); b.AddToClassList("nasil-baslik"); Erisim.Etiket(b, L.T(NASIL[i, 0]), "baslik"); k.Add(b);
                k.Add(Yazi(L.T(NASIL[i, 1]), 14));
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
