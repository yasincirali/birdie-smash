// Kalıcı ayarlar ve ilerleme. Anahtarlar ve değer biçimi HTML localStorage ile aynı (PlayerPrefs'te string):
//   tuytop_v2       JSON {rally, wins, kariyer, gorev, sampiyon}   (HTML best)
//   tuytop_len      'len5' | 'len11' | 'len21' (varsayılan len5)   tuytop_mod 'k' | 's'   tuytop_saha   tuytop_model
//   tuytop_gear / tuytop_ekonomi (Core/Gear)   tuytop_raket_gor JSON {grip, tel, tel2, desen}   tuytop_antrenman '1'
//   tuytop_puan     mağaza puanı istenme sayısı (0..3, HTML puanBak ile ortak)
//   tuytop_ilerleme (2026-10-26, kariyer haritası) JSON {v, tur: [[maske×6]×4]} (Core/Ilerleme: yıldız bit maskesi). İlk
//                   açılışta tuytop_v2.kariyer'den taşınır; tuytop_v2.kariyer yazılmaya devam eder (= Tur 1'de geçilen
//                   basamak; eski build'e dönen oyuncunun kaydı bozulmasın, geri gelince ikisi birleştirilir).
// HTML → Unity devri (Core/Devir): ilk açılışta kabuğun NSUserDefaults'a yazdığı HTML değerleri bir kez kabul edilir,
// sonra tuytop_html_devir bayrağı konur. Bayraktan sonra kabuk (eski HTML build'i) bir daha yazmaz.
// HTML'de karşılığı olmayan Unity anahtarları: tuytop_easy, tuytop_reduce (elle zorlanan; sistem ayarı ayrıca okunur),
// tuytop_kamera ('yayin' | 'alcak' | 'ust'; 2026-10-26, yasin "kamera açısı seçtirilebilir"), tuytop_ses, tuytop_dil, tuytop_yazi ('sistem' | 'normal' | 'buyuk'). HTML'den gelen tuytop_fps ('1' = gizli kare hızı
// göstergesi açık) ve tuytop_basarim (Game/Basarim) aynı adla okunur.
// Hareketi azalt (2026-10-25 eşitlik turu): HTML yalnız sistemin "Hareketi Azalt"ını izliyordu; Unity ikisini birleştirir:
// reduce = sistem || elle. Eski Unity kayıtlarında tuytop_reduce hep yazıldığı için 0 "zorlanmadı" sayılır.
// Yazı boyutu: HTML Dynamic Type izlemiyordu (px yazı, text-size-adjust:100%); Unity'de "Sistem" iOS'un tercih edilen
// yazı boyutunu izler (IMGUI yerleşimi taşmasın diye 0,9–1,3 arası), "Büyük" 1,3. VoiceOver için UI Toolkit (plan faz 6).
// 2026-10-26: menüler UI Toolkit'e geçti (Game/UI); menüde "Sistem" ×2'ye kadar (MenuYaziK), maç içi HUD ×1,3'te kalır.
// Önceki Unity demosu (2026-10-24 ilk tur) tuytop_target / tuytop_rally / tuytop_wins / tuytop_kariyer'i ayrı int yazıyordu:
// ilk açılışta bunlar okunup yeni biçime taşınır, eski anahtar silinmez (geri dönüşte kayıt kaybolmasın).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class PrefsKayit : IKayit
    {
        public string Get(string k) { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k, null) : null; }
        public void Set(string k, string v) { PlayerPrefs.SetString(k, v); PlayerPrefs.Save(); }
    }

    public static class Ayar
    {
        public static bool easy = true, reduce, reduceSecim, reduceSistem, ses = true, kariyerModu, antrenman, sampiyon, fps;
        public static int target = 5, kariyer, bestRally, wins, gorev, puan; public static int devirSayisi = -1;
        // Kariyer ilerlemesi (yıldızlar, turlar). kariyerSecim: haritadan seçilen basamak (genel indeks); -1 = sıradaki.
        // Kaydedilmez: haritadan bir basamağı yeniden oynamak "sıradaki"yi değiştirmesin.
        public static Ilerleme ilerleme = new Ilerleme(); public static int kariyerSecim = -1;
        // Oyun verisi (Core/KariyerVeri) Resources/veri'den: sahne yüklenmeden, her şeyden önce.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void VeriKaynagi() { KariyerVeri.Kaynak = ad => { var t = Resources.Load<TextAsset>("veri/" + ad); return t != null ? t.text : null; }; KariyerVeri.Sifirla(); }
        public static string saha = "salon", model = "oyuncak", kamera = "yayin", dil = "tr", yazi = "sistem"; static float yaziSistem = 1;
        public static float YaziK { get { return yazi == "normal" ? 1 : yazi == "buyuk" ? 1.3f : Mathf.Clamp(yaziSistem, .9f, 1.3f); } }
        // Menü yazı ölçeği (2026-10-26, UI Toolkit menü kabuğu): flex yerleşimde metin sarılır, kart uzar, gövde kayar; bu
        // yüzden "Sistem" iOS'un tercih edilen boyutunu ×2'ye kadar izler (HIG Dynamic Type). Maç içi HUD IMGUI'de kaldığı
        // için YaziK ×1,3 sınırını korur (oyun alanını kapatmasın; tasarım §2.4 bilinçli sapma). "Büyük" iki yüzeyde aynı 1,3.
        // yaziTest yalnız ekran turu içindir (EkranTuru ×2 karesi), kaydedilmez.
        public static float yaziTest;
        public static float MenuYaziK { get { if (yaziTest > 0) return yaziTest; return yazi == "normal" ? 1 : yazi == "buyuk" ? 1.3f : Mathf.Clamp(yaziSistem, .9f, 2f); } }
        // Sistem ayarları (hareketi azalt, yazı boyutu) açılışta ve uygulama öne her geldiğinde okunur.
        public static void SistemOku() { reduceSistem = Yerel.HareketAzalt(); reduce = reduceSecim || reduceSistem; yaziSistem = Yerel.YaziOlcek(); }
        public static Gear gear; public static GearStats GS = GearStats.Notr;
        public static readonly Dictionary<string, string> RGOR = new Dictionary<string, string> { { "grip", "siyah" }, { "tel", "beyaz" }, { "tel2", "kirmizi" }, { "desen", "tek" } };
        public static string FORMA { get { return GS.forma; } }
        static readonly PrefsKayit kayit = new PrefsKayit();

        static int EskiInt(string k, int d) { try { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k, d) : d; } catch { return d; } }
        static string Str(string k, string d) { try { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k, d) : d; } catch { return d; } }

        public static void Yukle()
        {
            devirSayisi = Devir.Kabul(kayit);   // Kaydet'ten önce: HTML değeri Unity varsayılanıyla ezilmesin
            var b = Json.Obj(Json.Parse(Str("tuytop_v2", null)));
            if (b != null) { bestRally = (int)Json.Num(b, "rally", 0); wins = (int)Json.Num(b, "wins", 0); kariyer = (int)Json.Num(b, "kariyer", 0); gorev = (int)Json.Num(b, "gorev", 0); sampiyon = Json.Bool(b, "sampiyon", false); }
            else { bestRally = EskiInt("tuytop_rally", 0); wins = EskiInt("tuytop_wins", 0); kariyer = EskiInt("tuytop_kariyer", 0); sampiyon = kariyer >= Match.KARIYER.Length; }   // ilk tur Unity kaydını taşı
            ilerleme = Ilerleme.Oku(Str(Ilerleme.ANAHTAR, null), kariyer, sampiyon); kariyer = ilerleme.Tur1Gecilen(); if (kariyer >= Match.KARIYER.Length) sampiyon = true;
            string len = Str("tuytop_len", null);
            if (len == "len5" || len == "len11" || len == "len21") target = int.Parse(len.Substring(3));
            else { int t = EskiInt("tuytop_target", 5); target = t == 11 || t == 21 ? t : 5; }   // bilinmeyen (eski len7 dahil) 5 olur
            easy = EskiInt("tuytop_easy", 1) == 1;
            saha = Str("tuytop_saha", "salon"); if (!CourtView.SAHA.ContainsKey(saha)) saha = "salon";
            model = Str("tuytop_model", "oyuncak"); if (model == "sportif" || model == "aday") model = "rocketbox"; if (model != "oyuncak" && model != "rocketbox") model = "oyuncak";
            kamera = Str("tuytop_kamera", "yayin"); if (System.Array.IndexOf(GameRoot.KAMERA_SIRA, kamera) < 0) kamera = "yayin";
            reduceSecim = EskiInt("tuytop_reduce", 0) == 1; ses = EskiInt("tuytop_ses", 1) == 1; SistemOku();
            yazi = Str("tuytop_yazi", "sistem"); if (yazi != "normal" && yazi != "buyuk") yazi = "sistem"; fps = Str("tuytop_fps", "0") == "1";
            int pu; puan = int.TryParse(Str("tuytop_puan", "0"), out pu) ? Mathf.Clamp(pu, 0, Ozet.PUAN_AZAMI) : Ozet.PUAN_AZAMI;
            kariyerModu = Str("tuytop_mod", "s") == "k"; antrenman = Str("tuytop_antrenman", "0") == "1";
            // Dil: HTML navigator.language 'tr' ile başlıyorsa Türkçe, değilse İngilizce. Unity'de sistem dili; ayarda elle seçilirse tuytop_dil.
            dil = Str("tuytop_dil", Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en"); if (dil != "tr" && dil != "en") dil = "tr";
            var rg = Json.Obj(Json.Parse(Str("tuytop_raket_gor", null)));
            // desen 'tek' | 'iki' (HTML RGOR.desen ile aynı). 2026-10-26: denetim 'capraz' bekliyordu, yazılan değer 'iki' olduğu
            // için "Ana ve çapraz ayrı" seçimi her açılışta 'tek'e dönüyordu; eski 'capraz' kaydı da 'iki' sayılır.
            if (rg != null) foreach (var k in new List<string>(RGOR.Keys)) { string v = Json.Str(rg, k, null); if (k == "desen" && v == "capraz") v = "iki"; if (v != null && (k == "desen" ? v == "tek" || v == "iki" : Vitrin.RAK_RENK.ContainsKey(v))) RGOR[k] = v; }
            gear = new Gear(kayit, wins); gear.kariyerModu = kariyerModu;
            if (gear.forma == "sampiyon" && !sampiyon) gear.forma = "mavi";
            GearHesap();
            Kaydet();
        }
        public static void GearHesap() { gear.kariyerModu = kariyerModu; GS = gear.Hesap(); }
        public static void Kaydet()
        {
            if (ilerleme != null) { kariyer = ilerleme.Tur1Gecilen(); PlayerPrefs.SetString(Ilerleme.ANAHTAR, ilerleme.Yaz()); }
            kayit.Set("tuytop_v2", Json.Write(new Dictionary<string, object> { { "rally", (double)bestRally }, { "wins", (double)wins }, { "kariyer", (double)kariyer }, { "gorev", (double)gorev }, { "sampiyon", sampiyon } }));
            PlayerPrefs.SetString("tuytop_len", "len" + target); PlayerPrefs.SetString("tuytop_saha", saha); PlayerPrefs.SetString("tuytop_model", model); PlayerPrefs.SetString("tuytop_kamera", kamera);
            PlayerPrefs.SetString("tuytop_mod", kariyerModu ? "k" : "s"); if (antrenman) PlayerPrefs.SetString("tuytop_antrenman", "1");
            PlayerPrefs.SetInt("tuytop_easy", easy ? 1 : 0); PlayerPrefs.SetInt("tuytop_reduce", reduceSecim ? 1 : 0); PlayerPrefs.SetInt("tuytop_ses", ses ? 1 : 0);
            PlayerPrefs.SetString("tuytop_dil", dil); PlayerPrefs.SetString("tuytop_puan", puan.ToString()); PlayerPrefs.SetString("tuytop_yazi", yazi); PlayerPrefs.SetString("tuytop_fps", fps ? "1" : "0");
            var rg = new Dictionary<string, object>(); foreach (var kv in RGOR) rg[kv.Key] = kv.Value; PlayerPrefs.SetString("tuytop_raket_gor", Json.Write(rg));
            if (gear != null) { gear.GearYaz(); gear.EkoYaz(); }
            PlayerPrefs.Save();
        }
    }
}
