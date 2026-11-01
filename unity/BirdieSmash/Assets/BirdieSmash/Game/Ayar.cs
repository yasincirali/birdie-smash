// Kalıcı ayarlar ve ilerleme. Anahtarlar ve değer biçimi HTML localStorage ile aynı (PlayerPrefs'te string):
//   tuytop_v2       JSON {rally, wins, kariyer, gorev, sampiyon}   (HTML best)
//   tuytop_len      'len5' | 'len11' | 'len21' (varsayılan len5)   tuytop_mod 'k' | 's'   tuytop_saha   tuytop_model
//   tuytop_gear / tuytop_ekonomi (Core/Gear)   tuytop_raket_gor JSON {grip, tel, tel2, desen}   tuytop_antrenman '1'
//   tuytop_puan     mağaza puanı istenme sayısı (0..3, HTML puanBak ile ortak)
//   tuytop_ilerleme (2026-10-26, kariyer haritası) JSON {v, tur: [[maske×6]×4]} (Core/Ilerleme: yıldız bit maskesi). İlk
//                   açılışta tuytop_v2.kariyer'den taşınır; tuytop_v2.kariyer yazılmaya devam eder (= Tur 1'de geçilen
//                   basamak; eski build'e dönen oyuncunun kaydı bozulmasın, geri gelince ikisi birleştirilir).
// 2026-10-26 ekipman v2 ve karakter (adım 4 ve 6; Ek A.6):
//   tuytop_gear     + kumas, renk, grip, kordaj (forma okunur, silinmez, eski build için yazılmaya devam eder; Core/Gear)
//   tuytop_ekonomi  + acik: ["yuva:id"] (Tüy ile alınmış ya da v1'den taşınmış modeller)
//   tuytop_ilerleme + tarz, unvan, ad, mac, smac (Core/Ilerleme)
//   tuytop_gorunum  (yeni) JSON {ten, sac, ayakkabi, kaplama} (Core/Karakter görünüm öğeleri; yoksa hepsi "modelin rengi":
//                   var olan oyuncunun görünümü değişmez). tuytop_raket_gor ayrı kalır.
//                   + tip, sac_model (2026-10-26 karakter tasarımı; Core/Kozmetik): yoksa listenin ilki (Standart, Kısa kesim) =
//                   var olan oyuncunun görünüşü. Sahiplik tuytop_ekonomi.acik'te ("tip:id", "sac_model:id"); ayakkabı MODELİ
//                   tuytop_gear.ayakkabi (Core/Gear, 7. model yuvası), ayakkabı RENGİ burada (ayakkabi).
// HTML → Unity devri (Core/Devir): ilk açılışta kabuğun NSUserDefaults'a yazdığı HTML değerleri bir kez kabul edilir,
// sonra tuytop_html_devir bayrağı konur. Bayraktan sonra kabuk (eski HTML build'i) bir daha yazmaz.
// HTML'de karşılığı olmayan Unity anahtarları: tuytop_easy, tuytop_reduce (elle zorlanan; sistem ayarı ayrıca okunur),
// tuytop_kamera ('yayin' | 'alcak' | 'ust'; 2026-10-26, yasin "kamera açısı seçtirilebilir"), tuytop_ses, tuytop_muzik ('1' varsayılan; 2026-10-30 menü müziği), tuytop_dil, tuytop_yazi ('sistem' | 'normal' | 'buyuk'). HTML'den gelen tuytop_fps ('1' = gizli kare hızı
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
        public static bool easy = true, reduce, reduceSecim, reduceSistem, ses = true, muzik = true, kariyerModu, antrenman, sampiyon, fps, kosIpucu;
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
        // GSE = yalnız ekipman (5 çubuk ve yükseltme önizlemesi bunu anlatır), GS = ekipman + oyun tarzı (maç fiziği bunu
        // okur). Tarz ekipman kartına girmez: güç ekipmandan, tarz karakterden (tasarım §5.1).
        public static Gear gear; public static GearStats GS = GearStats.Notr, GSE = GearStats.Notr;
        public static readonly Dictionary<string, string> GORUNUM = new Dictionary<string, string> { { "ten", "dogal" }, { "sac", "dogal" }, { "ayakkabi", "dogal" }, { "kaplama", "model" }, { "sort", "dogal" } };   // sort: 2026-10-27 şort rengi (Gerçekçi, Sade)
        public static readonly Dictionary<string, string> KOZ = new Dictionary<string, string> { { Kozmetik.TIP, "atlet" }, { Kozmetik.SAC, "kisa" }, { Kozmetik.SAPKA, "yok" }, { Kozmetik.YUZ, "yok" }, { Kozmetik.SEVINC, "yumruk" }, { Kozmetik.SIRT, "yok" }, { Kozmetik.MAC_SONU, "zafer" } };
        // Ayakkabı görünüşü (2026-10-30, Soyunma Oyun/Görünüm ayrımı): taban (güç) Gear "ayakkabi" seçimi Oyun'da; figürde görünen
        // model Görünüm'de, sahip olunan ayakkabılardan. "taban" = tabanla aynı model (eski kayıtların görünüşü değişmez).
        public static string AYK_GOR = "taban";
        public static string AykGorunum { get { return gear == null ? null : AYK_GOR != "taban" && gear.Sahip("ayakkabi", AYK_GOR) ? AYK_GOR : gear.ayakkabi; } }
        // 2026-10-30 Dolap › Raket: kafa ("oval" | "izometrik" | "kalp"; Game/RaketSekil) ve şaft ("model" = FBX'in koyu grisi ya da
        // bir raket rengi). Aynı anahtarda (tuytop_raket_gor) yeni alan: eski kayıtta yoksa varsayılan, eski sürüm bilmediği
        // alanı yok sayar (Json okuyucu yalnız RGOR'daki anahtarları arar).
        public static readonly Dictionary<string, string> RGOR = new Dictionary<string, string> { { "grip", "siyah" }, { "tel", "beyaz" }, { "tel2", "kirmizi" }, { "desen", "tek" }, { "kafa", "oval" }, { "saft", "model" } };
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
            kosIpucu = EskiInt("tuytop_kos_ipucu", 0) == 1;   // 2026-10-29: "Dokun: oraya koş" koşu sende ilk maçta görüldü
            saha = Str("tuytop_saha", "salon"); if (!CourtView.SAHA.ContainsKey(saha)) saha = "salon";
            // 2026-10-27 (yasin build 41: "3B model kaybolmuş, seçmeme rağmen görmedim"): build 40 notu Ayarlar'da "Sade" vaat
            // ediyordu, Resources'ta sade.fbx vardı (ekran turu her turda maçını çekiyor) ama seçenek yoktu ve "sade" kaydı burada
            // oyuncak'a dönüyordu. Artık geçerli model (MODELLER); FBX'i olmayan build'de yine oyuncak'a düşer.
            model = Str("tuytop_model", "oyuncak"); if (model == "sportif" || model == "aday") model = "rocketbox"; if (!ModelVar(model)) model = "oyuncak";
            kamera = Str("tuytop_kamera", "yayin"); if (System.Array.IndexOf(GameRoot.KAMERA_SIRA, kamera) < 0) kamera = "yayin";
            reduceSecim = EskiInt("tuytop_reduce", 0) == 1; ses = EskiInt("tuytop_ses", 1) == 1; muzik = EskiInt("tuytop_muzik", 1) == 1; SistemOku();
            yazi = Str("tuytop_yazi", "sistem"); if (yazi != "normal" && yazi != "buyuk") yazi = "sistem"; fps = Str("tuytop_fps", "0") == "1";
            int pu; puan = int.TryParse(Str("tuytop_puan", "0"), out pu) ? Mathf.Clamp(pu, 0, Ozet.PUAN_AZAMI) : Ozet.PUAN_AZAMI;
            kariyerModu = Str("tuytop_mod", "s") == "k"; antrenman = Str("tuytop_antrenman", "0") == "1";
            // Dil: HTML navigator.language 'tr' ile başlıyorsa Türkçe, değilse İngilizce. Unity'de sistem dili; ayarda elle seçilirse tuytop_dil.
            dil = Str("tuytop_dil", Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en"); if (dil != "tr" && dil != "en") dil = "tr";
            var rg = Json.Obj(Json.Parse(Str("tuytop_raket_gor", null)));
            // desen 'tek' | 'iki' (HTML RGOR.desen ile aynı). 2026-10-26: denetim 'capraz' bekliyordu, yazılan değer 'iki' olduğu
            // için "Ana ve çapraz ayrı" seçimi her açılışta 'tek'e dönüyordu; eski 'capraz' kaydı da 'iki' sayılır.
            if (rg != null) foreach (var k in new List<string>(RGOR.Keys)) { string v = Json.Str(rg, k, null); if (k == "desen" && v == "capraz") v = "iki"; if (v != null && (k == "desen" ? v == "tek" || v == "iki" : k == "kafa" ? System.Array.IndexOf(RaketSekil.SIRA, v) >= 0 : k == "saft" ? v == "model" || Vitrin.RAK_RENK.ContainsKey(v) : Vitrin.RAK_RENK.ContainsKey(v))) RGOR[k] = v; }
            gear = new Gear(kayit, wins); gear.kariyerModu = kariyerModu;
            if (gear.renk == "sampiyon" && !sampiyon) gear.renk = "mavi";   // v1 kuralı (forma = Şampiyon), artık renkte
            // Görünüm (karakter): bilinmeyen değer varsayılana düşer; seviyesi düşmüş gibi görünen değer (bozuk XP) silinmez,
            // ekranda Karakter.Oge ile yine geçerli öğeye çözülür.
            var gv = Json.Obj(Json.Parse(Str("tuytop_gorunum", null)));
            if (gv != null) foreach (var k in new List<string>(GORUNUM.Keys)) { string v = Json.Str(gv, k, null); if (v != null && System.Array.Exists(Karakter.Gorunum(k), o => o.id == v)) GORUNUM[k] = v; }
            // Tip ve saç biçimi: kayıtta yoksa varsayılan; varsa sahip sayılır (Kozmetik.Koru: hiçbir seçim elden alınmaz).
            foreach (var k in Kozmetik.KAT) KOZ[k] = Kozmetik.Koru(gear, k, gv != null ? Json.Str(gv, k, Kozmetik.Varsayilan(k)) : Kozmetik.Varsayilan(k));
            AYK_GOR = gv != null ? Json.Str(gv, "ayk_gor", "taban") : "taban";
            GearHesap();
            Kaydet();
        }
        // Ayarlar › Oyuncu görünümü seçenekleri (kimlik, etiket). Sade ve Tüycük yalnız FBX'i build'deyse (Resources) gösterilir.
        // 2026-10-29 Tüycük (yasin: "Oyuncu modellerimi değiştirme" → "yeni 3d model önerin varsa yeni seçenek olarak ekle"):
        // dördüncü seçenek sona eklendi; var olan sıra ve varsayılan "oyuncak" değişmez. Kayıt değeri "tuycuk"; FBX'i olmayan
        // build'de Yukle onu bilinmeyen model gibi oyuncak'a düşürür.
        public static readonly string[] MODELLER = { "oyuncak", "rocketbox", "sade", "tuycuk" }, MODEL_AD = { "Oyuncak 3B", "Gerçekçi", "Sade", "Tüycük" };
        public static bool ModelVar(string m)
        {
            if (m == "oyuncak" || m == "rocketbox") return true;
            if (m != "sade" && m != "tuycuk") return false; // Tur 2: varlık sınaması küçük <model>_clips.json ile (FBX ile hep birlikte gelir; PlayerView onu da okur): FBX'i
            // yüklemek (ağ + 26 klip, ~2 MB) o modeli seçmeyen oyuncuda da her açılışta Ayar.Yukle'de belleğe alıyordu.
            int v;
            if (!modelVar.TryGetValue(m, out v)) { var js = Resources.Load<TextAsset>("karakter/" + m + "_clips"); v = js != null ? 1 : 0; if (js != null) Resources.UnloadAsset(js); modelVar[m] = v; }
            return v == 1;
        }
        static readonly Dictionary<string, int> modelVar = new Dictionary<string, int>();
        // Beden tipi ve saç biçimi parçaları yalnız oyuncak ailesinin FBX'lerinde (oyuncak_<tip>.fbx) var.
        public static bool VaryantGorunur { get { return model == "oyuncak"; } }
        // 2026-10-27 (yasin: "Gerçekçi de vücut özellikleri değil de sadece aksesuarlar değişmeli"): Gerçekçi'de beden/yüz/saç
        // tek, ama beş ayakkabı modeli (rocketbox.fbx ayk_<varlik>__ayakkabi) ve şort ağı var. Sade'de şort ağı var, ayakkabı
        // modeli yok (renk taban parçasında görünür). Oyuncak'ta şort gövde atlasında: şort rengi orada görünmez, sunulmaz.
        // Tüycük (2026-10-29) Oyuncak'ın beş ayakkabı parçasını aynı ayak kemikleriyle taşır: model görünür. Şortu yok (tek parça gövde).
        public static bool AykModelGorunur { get { return model == "oyuncak" || model == "rocketbox" || model == "tuycuk"; } }
        public static bool SortGorunur { get { return model == "rocketbox" || model == "sade"; } }
        public static void GearHesap() { gear.kariyerModu = kariyerModu; GSE = gear.Hesap(); GS = Karakter.Uygula(GSE, Tarz); AldatmaKur(); }
        // Aldatıcı vuruşlar (2026-10-26, Core/Aldatma): seviye kapısı maça buradan gider (her maç kurulumunda GearHesap çağrılır);
        // ilk fırsat açıklaması bir kez gösterilir, görülenler ilerleme kaydında (tuytop_ilerleme.aldatmaGor).
        static void AldatmaKur()
        {
            Match.OyuncuSv = gear.Seviye(); if (ilerleme == null) return;
            Match.HileGorulen.Clear(); foreach (var id in ilerleme.AldatmaGorulen) Match.HileGorulen.Add(id);
            Match.HileGorulduKaydet = id => { ilerleme.AldatmaGor(id); PlayerPrefs.SetString(Ilerleme.ANAHTAR, ilerleme.Yaz()); };
        }
        // Geçerli oyun tarzı: kayıttaki tarz, seviyesi geldiyse (Core/Karakter.Gecerli).
        public static string Tarz { get { return Karakter.Gecerli(ilerleme != null ? ilerleme.Tarz : "dengeli", gear != null ? gear.Seviye() : 1); } }
        // Görünüm öğesinin rengi (null = modelin kendi rengi). Seviyesi gelmemiş öğe görünmez (varsayılan).
        public static string GorunumRenk(string kat)
        {
            string id; if (!GORUNUM.TryGetValue(kat, out id)) return null; var o = Karakter.Oge(kat, id);
            return o != null && (gear == null || o.sv <= gear.Seviye()) ? o.renk : null;
        }
        public static void Kaydet()
        {
            if (ilerleme != null) { kariyer = ilerleme.Tur1Gecilen(); PlayerPrefs.SetString(Ilerleme.ANAHTAR, ilerleme.Yaz()); }
            kayit.Set("tuytop_v2", Json.Write(new Dictionary<string, object> { { "rally", (double)bestRally }, { "wins", (double)wins }, { "kariyer", (double)kariyer }, { "gorev", (double)gorev }, { "sampiyon", sampiyon } }));
            PlayerPrefs.SetString("tuytop_len", "len" + target); PlayerPrefs.SetString("tuytop_saha", saha); PlayerPrefs.SetString("tuytop_model", model); PlayerPrefs.SetString("tuytop_kamera", kamera);
            PlayerPrefs.SetString("tuytop_mod", kariyerModu ? "k" : "s"); if (antrenman) PlayerPrefs.SetString("tuytop_antrenman", "1");
            PlayerPrefs.SetInt("tuytop_easy", easy ? 1 : 0); PlayerPrefs.SetInt("tuytop_reduce", reduceSecim ? 1 : 0); PlayerPrefs.SetInt("tuytop_ses", ses ? 1 : 0); PlayerPrefs.SetInt("tuytop_muzik", muzik ? 1 : 0); if (kosIpucu) PlayerPrefs.SetInt("tuytop_kos_ipucu", 1);
            PlayerPrefs.SetString("tuytop_dil", dil); PlayerPrefs.SetString("tuytop_puan", puan.ToString()); PlayerPrefs.SetString("tuytop_yazi", yazi); PlayerPrefs.SetString("tuytop_fps", fps ? "1" : "0");
            var rg = new Dictionary<string, object>(); foreach (var kv in RGOR) rg[kv.Key] = kv.Value; PlayerPrefs.SetString("tuytop_raket_gor", Json.Write(rg));
            var gv = new Dictionary<string, object>(); foreach (var kv in GORUNUM) gv[kv.Key] = kv.Value; foreach (var kv in KOZ) gv[kv.Key] = kv.Value; gv["ayk_gor"] = AYK_GOR; PlayerPrefs.SetString("tuytop_gorunum", Json.Write(gv));
            if (gear != null) { gear.GearYaz(); gear.EkoYaz(); }
            PlayerPrefs.Save();
        }
    }
}
