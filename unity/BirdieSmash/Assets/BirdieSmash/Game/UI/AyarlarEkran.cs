// Ayarlar (⚙), Nasıl oynanır, Lisanslar (2026-10-26). İçerik bugünkü IMGUI AyarGovde ile aynı, gruplu liste olarak:
//   Oyun: zorluk, maç uzunluğu (kariyere de uygulanır; saha yalnız serbest maçta anlamlı olduğu için Hızlı maç'a taşındı).
//   Görünüm: oyuncu görünümü (Ayar.model: Oyuncak 3B, Gerçekçi, Sade; root.ModelDegisti), kamera (Ayar.kamera, GameRoot.KAMERA_SIRA/KAMERA_AD,
//   root.KameraDegisti), yazı boyutu, hareketi azalt (sistem açıksa her zaman açık, kapalıysa buradan zorlanır).
//   Ses. Dil. Game Center · Başarımlar (girişliyse). Yardım: Nasıl oynanır ›, Antrenmanı yeniden oyna, Lisanslar ›.
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
            c.Add(Yazi(L.T("Oyun"), 17, "bolum")); OyunSecenek(c, false);
            c.Add(Yazi(L.T("Görünüm"), 17, "bolum"));
            c.Add(Grup(L.T("Oyuncu görünümü"))); var m = Satir(); c.Add(m);
            // 2026-10-27 (yasin build 41: "3B model kaybolmuş, seçmeme rağmen görmedim"): Sade üçüncü seçenek (build 40 notu
            // vaat ediyordu, FBX vardı, çip yoktu). Karakter odasındaki tip/saç/ayakkabı modeli yalnız Oyuncak 3B'de görünür;
            // başka model seçiliyken bu, küçük gri notla değil, çiplerin hemen altında açıkça yazılır.
            for (int i = 0; i < Ayar.MODELLER.Length; i++)
            {
                string id = Ayar.MODELLER[i]; if (!Ayar.ModelVar(id)) continue;
                m.Add(Cip(L.T(Ayar.MODEL_AD[i]), Ayar.model == id, () => { Ayar.model = id; Ayar.Kaydet(); root.ModelDegisti(); Degisti(); }));
            }
            // 2026-10-27: Gerçekçi'de ayakkabı modeli, şort ve kumaş da görünür; not neyin sabit neyin değiştiğini söyler.
            if (Ayar.model == "rocketbox") c.Add(Yazi(L.T("Gerçekçi: beden, yüz ve saç sabit; ayakkabı, forma, şort, kafa bandı, bileklik ve raket değişir."), 13, "ipucu"));
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
            c.Add(Grup(L.T("Hareket")));
            string ht = Ayar.reduceSistem ? "Hareket azaltıldı · sistem" : Ayar.reduce ? "Hareket azaltıldı" : "Hareket tam";
            c.Add(Cip(L.T(ht), Ayar.reduce, Ayar.reduceSistem ? (System.Action)null : () => { Ayar.reduceSecim = !Ayar.reduceSecim; Ayar.SistemOku(); Ayar.Kaydet(); Degisti(); }));
            c.Add(Yazi(L.T("Ses"), 17, "bolum"));
            c.Add(Cip(L.T(Ayar.ses ? "Ses açık" : "Ses kapalı"), Ayar.ses, () => { Ayar.ses = !Ayar.ses; AudioListener.volume = Ayar.ses ? 1 : 0; Ayar.Kaydet(); Degisti(); }));
            c.Add(Yazi(L.T("Dil"), 17, "bolum")); var dl = Satir(); c.Add(dl);
            dl.Add(Cip("Türkçe", Ayar.dil == "tr", () => { Ayar.dil = "tr"; Ayar.Kaydet(); }));
            dl.Add(Cip("English", Ayar.dil == "en", () => { Ayar.dil = "en"; Ayar.Kaydet(); }));
            if (Basarim.acik) { c.Add(Bosluk(10)); c.Add(Dugme(L.T("Game Center · Başarımlar"), () => { Yerel.Hap("tick"); Basarim.Goster(); })); }
            c.Add(Yazi(L.T("Yardım"), 17, "bolum"));
            c.Add(Dugme(L.T("Nasıl oynanır") + "  ›", () => SayfaAc(Gezinti.NASIL), null, L.T("Vuruş, servis, nefes")));
            c.Add(Dugme(L.T("Antrenmanı yeniden oyna"), Antrenmana));
            c.Add(Dugme(L.T("Lisanslar ve üçüncü taraf içerik") + "  ›", () => SayfaAc(Gezinti.LISANS)));
            c.Add(Bosluk(24));
        }
        // Dil değişince Update tüm menüyü yeniden kurar (sonDil); Ayarlar sayfası açık kalır.

        void Antrenmana() { var once = gez.sekme; gez.Kok(); Uygula(once, false); root.Antrenman(); }

        static readonly string[,] NASIL = {
            {"Koş","Kolay modda oyuncu topa kendisi koşar, senin işin yalnız vuruş; kısa bir dokunuşla yerini değiştirebilirsin. Usta modda koşmak sende: dokun = oraya koş, yavaş sürükle = parmağı izle"},
            {"Forehand / backhand","Top raket tarafında gelirse forehand, öbür tarafta backhand. Backhand clear kısa kalır, drive yavaşlar, smaç olmaz; topu forehand tarafına alacak şekilde dur"},
            {"Akış","Üst üste 4 tam orta: çizgiler altın olur, hedef büyür, şarkıya armoni girer"},
            {"Derinlik","Topun yerdeki gölgesi ve ona bağlı ince çizgi yüksekliği gösterir. Top sana inerken son saniyede yer çizgisi ve büzülen iniş halkası belirir; yeşilse yetişiyorsun"},
            {"Nefes","Smaç, sıçrama ve uzanma nefes yakar; nefesin düşünce yavaşlarsın ve smaç zayıflar. Clear atıp sayı arasında dinlen. Topu tepeden alırsan smaç dikleşir"},
            {"Ekipman","Raket, tel gerginliği, bileklik, bant ve forma oyunun dinamiklerini, raket sesini ve titreşimi değiştirir"},
            // 2026-10-26 ekipman v2 ve karakter: 5 çubuk ve tarz bir satırla anlatılır (yalnız Unity; HTML donduruldu)
            {"Çubuklar ve tarz","Soyunma odasında her parçanın 5 çubuğu var (50 nötr), ▲ artısını ▼ eksisini gösterir; en iyi parça yok. Tarz güç vermez, oyun biçimini değiştirir: ücretsiz, istediğin an değiştir"}};
        void NasilKur(VisualElement p)
        {
            SayfaBasi(p, "Nasıl oynanır");
            var sv = Kaydirma(p); sv.AddToClassList("govde-kaydir"); var c = sv.contentContainer; c.AddToClassList("sekme-icerik");
            c.Add(Dugme(L.T("Antrenmanı yeniden oyna"), Antrenmana, "dugme--birincil"));
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
