// Ekipman ve ekonomi (HTML GEAR, telStats, EKO, gsHesap, ekoOdul; main 46943a8). Saf hesap: kayıt IKayit üzerinden
// (Unity'de PlayerPrefs, testte bellek). Anahtarlar ve JSON biçimi HTML localStorage ile aynı (tuytop_gear,
// tuytop_ekonomi) ki iki sürümün kaydı aynı şekli taşısın. Tasarım notu HTML'de (ekonomi v1, yasin 2026-10-22):
// tek para "Tüy" + XP, altı yuva × 10 seviye, yükseltme modele değil yuvaya, tavan ~%15–18, "eşit şartlar" serbest
// maçta yükseltmeyi saymaz, IAP yok.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public interface IKayit { string Get(string k); void Set(string k, string v); }
    public class BellekKayit : IKayit
    {
        readonly Dictionary<string, string> d = new Dictionary<string, string>();
        public string Get(string k) { string v; return d.TryGetValue(k, out v) ? v : null; }
        public void Set(string k, string v) { d[k] = v; }
    }

    // Bir model: null alan = "bu model o değeri değiştirmez" (HTML'de alanın olmaması).
    // 2026-10-26 ekipman v2: modeller veri/gear.json'dan (Gear.GEAR). sv = açılış seviyesi, fiyat = Tüy (0 = seviye gelince
    // kendiliğinden açık), eski = v2 öncesinde herkese açıktı (var olan oyuncuda açık kalır), kilit = özel koşul ("sampiyon").
    // Yeni özel etkiler (sayıya sığmayan, "özel" satırı): slice (slice sapması ve aldatma süresi çarpanı, Kesici),
    // yorgunSapma (nefes < 0,4 iken isabet sapması çarpanı, Havlu grip), yor (nefes tüketimi çarpanı, Hücum).
    // forma: kumaşın eski (v1) forma kimliği; taşıma ve eski build'e geri yazma için. doku: grip görünümü ("cizgi").
    // 2026-10-26 ayakkabı (7. model yuvası): varlik = FBX parçası ayk_<varlik>__ayakkabi; uzanma ve jumpTatli ayakkabının
    // Uzanma ve Sıçrama çubukları (tarzla aynı GearStats alanları, çarpılarak biner).
    public class GearModel
    {
        public string id, ad, n, renk, forma, doku, kilitAd, varlik; public bool kilit, eski; public int sv = 1, fiyat;
        public float? guc, kontrol, hiz, tatli, ton, kafa, kenar, yavas, rakip, kosu, enerji, toparla, slice, yorgunSapma, yor, uzanma, jumpTatli; public int? akis;
    }

    public class TelStat { public string ad, n; public float guc, tatli, kontrol, ton; }

    // gsHesap çıktısı: maç fiziğinin okuduğu çarpanlar. Varsayılan nötr (1) — bot testleri ve eski imza bununla koşar.
    // 2026-10-26: 14 çarpan (guc … yorK, akis) aynen; yanına "özel" etkiler (ekipman: slice, yorgunSapma) ve oyun tarzının
    // küçük değiştiricileri (Core/Tarz: jumpTatli, fileTatli, smacYor, smacHiz, uzanma, arkaKosu). Hepsi varsayılan 1:
    // nötr ekipman + Dengeli tarz eski fiziği birebir verir (bot testleri ve eski imza bununla koşar).
    public class GearStats
    {
        public float guc = 1, kontrol = 1, hiz = 1, tatli = 1, ton = 1, kafa = 1, kenar = 1, yavas = 1, rakip = 1, kosu = 1, enerji = 1, toparla = 1, yorK = 1;
        public int akis = 4;
        public float slice = 1, yorgunSapma = 1, jumpTatli = 1, fileTatli = 1, smacYor = 1, smacHiz = 1, uzanma = 1, arkaKosu = 1;
        public string forma = "#4fb8ff", raketRenk = "#e9eef0", bant, bileklik, gripDoku, tarz = "dengeli";
        public static readonly GearStats Notr = new GearStats();
        public GearStats Kopya() { return (GearStats)MemberwiseClone(); }
    }

    // k = kayıttaki seviye anahtarı (tuytop_ekonomi.sv; v1'den beri aynı), m = model yuvası (Gear.GEAR anahtarı).
    // "tel" seviyesi Kordaj modelini, "forma" seviyesi forma kumaşını büyütür (adlar kayıtta kalır, eski kayıt bozulmasın).
    public class Yuva { public string k, m, ad, n; }

    // Model durumu (Soyunma odası, yuva sayfası): Secili ⊂ Acik; Alinir = seviye tamam, Tüy ile alınır; Kilitli = seviye
    // gelmedi; Ozel = kariyer koşulu (Şampiyon rengi).
    public enum ModelDurum { Secili, Acik, Alinir, Kilitli, Ozel }

    public class Ekonomi
    {
        public int tuy, xp; public string gun = ""; public bool esit;
        // 2026-10-26 ekipman v2 (Ek A.6): Tüy ile alınmış ya da v1'den taşınmış modeller, "yuva:id" (raket ve bileklikte
        // ikisi de "agir" kimliğini taşıdığı için yuva önekli). Fiyatı 0 olan model seviye gelince listeye girmeden açıktır.
        public readonly HashSet<string> acik = new HashSet<string>();
        public readonly Dictionary<string, int> sv = new Dictionary<string, int> { { "raket", 1 }, { "tel", 1 }, { "grip", 1 }, { "bileklik", 1 }, { "bant", 1 }, { "forma", 1 } };
    }

    public class OdulSonuc { public int tuy, xp, once, sonra; public List<string> ek = new List<string>(); }

    public class Gear
    {
        // ---- veri (tool/veri/gear.json → Resources/veri; Core/KariyerVeri ile aynı kaynak yolu) ----
        // Neden veri: tasarım Ek A.4 "veri tek kaynak"; modeller, açılış seviyeleri ve fiyatlar denge turlarında değişecek,
        // hesap (Hesap) değişmeden. Model sırası dosyadaki sıradır (Soyunma odası listesi o sırayla dizilir).
        // 2026-10-26: "ayakkabi" 7. model yuvası (yasin: "ayakkabı seçebilecek, özelliklerine ve fiyatına göre"). Gear.YUVA'ya
        // (yükseltme) girmez: ekonomi tavanı (%15–18) altı yükseltme yuvasına göre ayarlı; 7. yükseltme tavanı delerdi. Ayakkabı
        // yalnız model seçimidir (sv + Tüy), diğer modeller gibi artısı ve eksisi olan yan seçim. Soyunma odası › Karakter'de.
        public static readonly string[] MODEL_YUVA = { "raket", "kordaj", "grip", "bileklik", "bant", "kumas", "ayakkabi" };
        static Dictionary<string, Dictionary<string, GearModel>> gear; static Dictionary<string, GearModel> renkler;
        public static Dictionary<string, Dictionary<string, GearModel>> GEAR { get { VeriYukle(); return gear; } }
        public static Dictionary<string, GearModel> RENK { get { VeriYukle(); return renkler; } }
        public static void VeriSifirla() { gear = null; renkler = null; }
        static void VeriYukle()
        {
            if (gear != null) return;
            var o = Json.Obj(Json.Parse(KariyerVeri.Metin("gear"))); if (o == null) throw new InvalidOperationException("Birdie Smash: veri/gear.json okunamadı");
            var g = new Dictionary<string, Dictionary<string, GearModel>>(); var mo = Json.Obj(o["modeller"]);
            foreach (var y in MODEL_YUVA)
            {
                var d = new Dictionary<string, GearModel>(); var l = mo.ContainsKey(y) ? mo[y] as List<object> : null;
                if (l == null || l.Count == 0) throw new InvalidOperationException("Birdie Smash: gear.json yuva yok: " + y);
                foreach (var x in l) { var m = ModelOku(Json.Obj(x)); d[m.id] = m; }
                g[y] = d;
            }
            var r = new Dictionary<string, GearModel>(); foreach (var x in (List<object>)o["renkler"]) { var m = ModelOku(Json.Obj(x)); r[m.id] = m; }
            renkler = r; gear = g;
        }
        static float? F(Dictionary<string, object> d, string k) { object v; return d.TryGetValue(k, out v) && v is double ? (float)(double)v : (float?)null; }
        static GearModel ModelOku(Dictionary<string, object> d)
        {
            string kl = Json.Str(d, "kilit", null);
            return new GearModel {
                id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), n = Json.Str(d, "n", ""), renk = Json.Str(d, "renk", null), forma = Json.Str(d, "forma", null), doku = Json.Str(d, "doku", null), varlik = Json.Str(d, "varlik", null),
                sv = (int)Json.Num(d, "sv", 1), fiyat = (int)Json.Num(d, "fiyat", 0), eski = Json.Bool(d, "eski", false), kilit = kl != null, kilitAd = kl,
                guc = F(d, "guc"), kontrol = F(d, "kontrol"), hiz = F(d, "hiz"), tatli = F(d, "tatli"), ton = F(d, "ton"), kafa = F(d, "kafa"), kenar = F(d, "kenar"), yavas = F(d, "yavas"),
                rakip = F(d, "rakip"), kosu = F(d, "kosu"), enerji = F(d, "enerji"), toparla = F(d, "toparla"), slice = F(d, "slice"), yorgunSapma = F(d, "yorgunSapma"), yor = F(d, "yor"), uzanma = F(d, "uzanma"), jumpTatli = F(d, "jumpTatli"),
                akis = d.ContainsKey("akis") && d["akis"] is double ? (int)(double)d["akis"] : (int?)null };
        }
        static readonly Dictionary<string, string> VARSAYILAN = new Dictionary<string, string> { { "raket", "denge" }, { "kordaj", "dayanikli" }, { "grip", "sentetik" }, { "bileklik", "yok" }, { "bant", "yok" }, { "kumas", "klasik" }, { "ayakkabi", "klasik" } };
        public static string Varsayilan(string yuva) { return VARSAYILAN[yuva]; }

        // Tel gerginliği sürekli (18–32 lbs): tel frekansı √gerginlik ile artar; gevşek tel güç ve geniş tatlı nokta, sıkı tel kontrol. 26 nötr.
        // 2026-10-26: gerginlik beceri ayarıdır, ilerleme değil (ekonomi kararı); Kordaj modelinden ayrı kalır, ikisi çarpılır.
        public static TelStat TelStats(float T)
        {
            float d = 26 - T;
            return new TelStat { ad = T + " lbs", n = T <= 22 ? "Gevşek: güç artar, tatlı nokta geniş, tok ve uzun çınlama" : T >= 30 ? "Sıkı: kontrol artar, tatlı nokta dar, tiz ve kısa çınlama" : "Orta",
                guc = 1 + d * .012f, tatli = 1 + d * .035f, kontrol = 1 - d * .025f, ton = (float)Math.Sqrt(T / 26) };
        }

        public static readonly Yuva[] YUVA = {
            new Yuva{k="raket",m="raket",ad="Raket",n="Güç ve kontrol artar; modelin artısı büyür, eksisi küçülür"}, new Yuva{k="tel",m="kordaj",ad="Kordaj",n="Tatlı nokta genişler, gerginliğin eksisi azalır"},
            new Yuva{k="grip",m="grip",ad="Grip",n="Kontrol artar: top daha az sapar"}, new Yuva{k="bileklik",m="bileklik",ad="Bileklik",n="Nefes daha yavaş tükenir"},
            new Yuva{k="bant",m="bant",ad="Kafa bandı",n="Tam orta penceresi genişler"}, new Yuva{k="forma",m="kumas",ad="Forma kumaşı",n="Koşu hızlanır, kumaşın etkisi büyür"}};
        public static Yuva YuvaOf(string k) { foreach (var y in YUVA) if (y.k == k || y.m == k) return y; return null; }
        public static readonly int[] YUK_FIYAT = { 50, 110, 180, 290, 450, 680, 1000, 1450, 2100 }, YUK_SV = { 1, 2, 4, 6, 9, 12, 16, 20, 24 };
        public static readonly Dictionary<string, string[]> YUK_ETKI = new Dictionary<string, string[]> {
            {"raket",new[]{"guc","kontrol","hiz"}},{"tel",new[]{"tatli","guc"}},{"grip",new[]{"kontrol"}},{"bileklik",new[]{"day","guc","hiz"}},{"bant",new[]{"tatli"}},{"forma",new[]{"kosu","enerji"}}};
        public static readonly Dictionary<string, string> ETKI_AD = new Dictionary<string, string> {
            {"guc","Güç"},{"kontrol","Kontrol"},{"hiz","Drive/file"},{"tatli","Tatlı nokta"},{"kosu","Koşu"},{"enerji","Smaç gücü dolumu"},{"day","Dayanıklılık"}};

        // seçili modeller (HTML G) + ekonomi. renk = forma rengi (yalnız görünüm), kumas = forma kumaşı (işlev).
        public string raket = "denge", kordaj = "dayanikli", grip = "sentetik", bileklik = "yok", bant = "yok", kumas = "klasik", renk = "mavi", ayakkabi = "klasik"; public float tel = 26;
        public Ekonomi eko = new Ekonomi();
        public bool kariyerModu;
        readonly IKayit kayit;

        public string Secili(string yuva)
        {
            switch (yuva) { case "raket": return raket; case "kordaj": return kordaj; case "grip": return grip; case "bileklik": return bileklik; case "bant": return bant; case "kumas": return kumas; case "ayakkabi": return ayakkabi; default: return renk; }
        }
        void Ata(string yuva, string id)
        {
            switch (yuva) { case "raket": raket = id; break; case "kordaj": kordaj = id; break; case "grip": grip = id; break; case "bileklik": bileklik = id; break; case "bant": bant = id; break; case "kumas": kumas = id; break; case "ayakkabi": ayakkabi = id; break; default: renk = id; break; }
        }

        // Forma ayrımı (tasarım §4.2, 2026-10-26): v1'de forma hem renk hem işlevdi. Eski kayıtta kumas/renk yoksa forma ikiye
        // bölünür: Mavi → Klasik + Mavi, Fıstık → Hafif + Fıstık, Beyaz → Nefes alan + Beyaz, Mor → Esnek + Mor, Şampiyon →
        // Klasik + Şampiyon (v1'de yalnız görünümdü). Hiçbir oyuncu gücünü ya da rengini kaybetmez. tuytop_gear.forma okunur,
        // silinmez ve yazılmaya devam eder (eski build'e dönen oyuncu için; EskiForma).
        public static string KumasOf(string forma) { foreach (var m in GEAR["kumas"].Values) if (m.forma == forma) return m.id; return "klasik"; }

        public Gear(IKayit k, int eskiGalibiyet)
        {
            kayit = k;
            string gs = k.Get("tuytop_gear"), es = k.Get("tuytop_ekonomi");
            var g = Json.Obj(Json.Parse(gs));
            if (g != null)
            {
                object tv; if (g.TryGetValue("tel", out tv)) { double v = tv is string ? ParseD(((string)tv).Replace("t", "")) : tv is double ? (double)tv : 0; if (v >= 18 && v <= 32) tel = (float)v; }
                raket = Model("raket", Json.Str(g, "raket", raket)); bileklik = Model("bileklik", Json.Str(g, "bileklik", bileklik)); bant = Model("bant", Json.Str(g, "bant", bant));
                kordaj = Model("kordaj", Json.Str(g, "kordaj", kordaj)); grip = Model("grip", Json.Str(g, "grip", grip));
                ayakkabi = Model("ayakkabi", Json.Str(g, "ayakkabi", ayakkabi));   // 2026-10-26: alan yoksa Klasik kort (nötr = bugünkü fizik)
                string fo = Json.Str(g, "forma", null), ku = Json.Str(g, "kumas", null), rn = Json.Str(g, "renk", null);
                kumas = ku != null && GEAR["kumas"].ContainsKey(ku) ? ku : fo != null ? KumasOf(fo) : "klasik";
                renk = rn != null && RENK.ContainsKey(rn) ? rn : fo != null && RENK.ContainsKey(fo) ? fo : "mavi";
            }
            var e = Json.Obj(Json.Parse(es));
            if (e != null)
            {
                eko.tuy = (int)Json.Num(e, "tuy", 0); eko.xp = (int)Json.Num(e, "xp", 0); eko.gun = Json.Str(e, "gun", ""); eko.esit = Json.Bool(e, "esit", false);
                var sv = Json.Obj(e.ContainsKey("sv") ? e["sv"] : null);
                foreach (var y in YUVA) eko.sv[y.k] = (int)Json.Num(sv, y.k, 1);
                var al = e.ContainsKey("acik") ? e["acik"] as List<object> : null; if (al != null) foreach (var x in al) if (x is string) eko.acik.Add((string)x);
            }
            // Açılış seviyeleri ve eski oyuncu (tasarım §4.2): v2 öncesinde bütün modeller serbestti. acik listesi olmayan ama
            // kaydı olan (tuytop_gear ya da tuytop_ekonomi, ya da ekonomiden önceki galibiyetler) oyuncuda v1 modellerinin hepsi
            // açık kalır; açılış seviyesi yalnız yeni oyuncuya ve yeni modellere uygulanır ("elimden aldılar" hissi olmasın).
            bool eskiOyuncu = (g != null || e != null || eskiGalibiyet > 0) && (e == null || !e.ContainsKey("acik"));
            if (eskiOyuncu) foreach (var y in MODEL_YUVA) foreach (var m in GEAR[y].Values) if (m.eski && m.fiyat > 0) eko.acik.Add(y + ":" + m.id);
            // O an seçili model her durumda açık kalır (elle bozulmuş kayıt ya da gelecekteki fiyat değişikliği seçimi elinden almasın).
            foreach (var y in MODEL_YUVA) { var m = GEAR[y][Secili(y)]; if (m.fiyat > 0) eko.acik.Add(y + ":" + m.id); }
            if (e == null) { eko.tuy = eskiGalibiyet > 0 ? 100 + Math.Min(1000, eskiGalibiyet * 25) : 0; EkoYaz(); }   // eski oyuncu sıfırdan başlamasın
        }
        static double ParseD(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0; }
        string Model(string k, string v) { return GEAR[k].ContainsKey(v) ? v : Varsayilan(k); }

        // Eski build'in okuduğu forma kimliği: rengin v1 kumaşı seçili kumaşla aynıysa o renk (görünüm de korunur); değilse
        // kumaşın v1 forma kimliği. Neden işlev öncelikli: eski build tek alanı yazıp geri gelince kumaş yeniden bu alandan
        // türer; işlev kaybı (koşu, enerji) renk kaybından ağır.
        public string EskiForma()
        {
            if (renk == "sampiyon" && kumas == "klasik") return "sampiyon";
            if (KumasOf(renk) == kumas && GEAR["kumas"][kumas].forma == renk) return renk;
            return GEAR["kumas"][kumas].forma ?? "mavi";
        }
        public void GearYaz()
        {
            kayit.Set("tuytop_gear", Json.Write(new Dictionary<string, object> { { "raket", raket }, { "tel", (double)tel }, { "bileklik", bileklik }, { "bant", bant }, { "forma", EskiForma() },
                { "kumas", kumas }, { "renk", renk }, { "grip", grip }, { "kordaj", kordaj }, { "ayakkabi", ayakkabi } }));
        }
        public void EkoYaz()
        {
            var sv = new Dictionary<string, object>(); foreach (var kv in eko.sv) sv[kv.Key] = (double)kv.Value;
            var al = new List<object>(); foreach (var x in eko.acik) al.Add(x); al.Sort((p, q) => string.CompareOrdinal((string)p, (string)q));
            kayit.Set("tuytop_ekonomi", Json.Write(new Dictionary<string, object> { { "tuy", (double)eko.tuy }, { "xp", (double)eko.xp }, { "sv", sv }, { "gun", eko.gun }, { "esit", eko.esit }, { "acik", al } }));
        }

        // ---- model açılışı ve Tüy ile alma (tasarım §4.3: seviye gelince açılır, 300–1.200 Tüy ile alınır) ----
        // Neden alınır, kendiliğinden açılmaz: "2–3 maçta bir yeni bir şey" ara hedefi (ekonomi §3.4). Güç sırası yine tek
        // yerden (seviye) okunur: Tüy yalnız erişimi hızlandırmaz, seviye kapısı aynen durur. Rastgelelik, süre, sert para yok.
        public bool Sahip(string yuva, string id)
        {
            GearModel m; if (!GEAR.ContainsKey(yuva) || !GEAR[yuva].TryGetValue(id, out m)) return false;
            return eko.acik.Contains(yuva + ":" + id) || (m.fiyat == 0 && Seviye() >= m.sv);
        }
        public ModelDurum Durum(string yuva, string id)
        {
            if (Secili(yuva) == id) return ModelDurum.Secili;
            if (Sahip(yuva, id)) return ModelDurum.Acik;
            return Seviye() >= GEAR[yuva][id].sv ? ModelDurum.Alinir : ModelDurum.Kilitli;
        }
        public bool AlOlur(string yuva, string id) { return Durum(yuva, id) == ModelDurum.Alinir && eko.tuy >= GEAR[yuva][id].fiyat; }
        public bool Al(string yuva, string id)
        {
            if (!AlOlur(yuva, id)) return false; eko.tuy -= GEAR[yuva][id].fiyat; eko.acik.Add(yuva + ":" + id); EkoYaz(); return true;
        }
        public bool Sec(string yuva, string id) { if (!Sahip(yuva, id)) return false; Ata(yuva, id); return true; }
        // Forma rengi yalnız görünüm: seviye ya da kariyer koşulu (Şampiyon), Tüy yok (dükkân adım 10'da).
        public bool RenkAcik(string id, bool sampiyon) { GearModel m; return RENK.TryGetValue(id, out m) && (m.kilit ? m.kilitAd == "sampiyon" && sampiyon : Seviye() >= m.sv); }
        // Seviyesi gelmiş, alınabilecek kadar Tüy'ü olan model var mı (maç sonu "Ekipmanı yükselt" ve yuva kartındaki ↑).
        public bool AlinabilirVar(string yuva) { foreach (var m in GEAR[yuva].Values) if (AlOlur(yuva, m.id)) return true; return false; }

        // JS Math.round yarımı yukarı yuvarlar, C# Math.Round bankacı yuvarlaması yapar: 21 sayılık yenilgide 15·3,5 = 52,5 Tüy
        // HTML'de 53, C#'ta 52 olurdu. Ekonomi sayıları HTML ile birebir kalsın diye tek yuvarlama burası.
        public static double JsRound(double v) { return Math.Floor(v + .5); }
        public static int SvEsik(int L) { return (int)JsRound((100 + 60 * Math.Pow(L - 1, 1.35)) / 10) * 10; }
        public void OyuncuSv(out int L, out int kalan, out int gerek) { L = 1; int x = eko.xp; while (L < 30 && x >= SvEsik(L)) { x -= SvEsik(L); L++; } kalan = x; gerek = L < 30 ? SvEsik(L) : 0; }
        public int Seviye() { int L, a, b; OyuncuSv(out L, out a, out b); return L; }
        public float KSv(string k) { int v; eko.sv.TryGetValue(k, out v); return eko.esit && !kariyerModu ? 0 : (Math.Max(1, Math.Min(10, v == 0 ? 1 : v)) - 1) / 9f; }
        static float Olcek(float m, float k, float arti, float eksi) { return m >= 1 ? 1 + (m - 1) * (1 + arti * k) : 1 - (1 - m) * (1 - eksi * k); }

        // Saf hesap (yükseltme önizlemesi bir seviye ilerisini dener). Yuva seviyesi modelin çarpanlarını ölçekler;
        // nötr modelde (Denge, Mavi) yükseltme boşa gitmesin diye raket güç/kontrole +%5, forma koşuya +%4 taban katkı.
        // 2026-10-26 denge düzeltmesi (komple-oyun tasarımı §4.5): taban katkı bütün modellerin güç ve kontrolüne gidiyordu;
        // eksisi olan modelde eksi zaten Sv 10'da %60 küçüldüğü için +%5 onu sıfırlıyordu (Esnek şaft Sv 10'da güç 1,01,
        // kontrol 1,42: "en iyi raket" oluyordu, model seçimi anlamını yitiriyordu). Artık (1) +%5 yalnız modelin nötr
        // (çarpanı 1) olduğu değere uygulanır, nötr modelde yükseltmenin boşa gitmemesi gerekçesi aynen sürer; (2) raket
        // eksisinin küçülmesi Sv 10'da %60 değil %40: eksinin en az %60'ı kalır, her modelin artısı ve eksisi görünür kalır.
        // Sv 10'da: Esnek güç ~0,94, Kafa ağır kontrol ~0,95, Denge güç/kontrol 1,05. Bileklik ve kordaj ölçeği değişmedi
        // (tasarım yalnız raketi ölçtü). HTML gsHesap 2026-10-26'dan beri dondurulmuş sürümdür (yasin: yeni özellik yalnız
        // Unity'de), bu düzeltme oraya taşınmadı: iki sürümün raket değerleri bu noktadan sonra bilerek ayrıdır.
        // 2026-10-26 ekipman v2: Kordaj ve grip modelleri raketle aynı ölçekle (artı +%18, eksi Sv 10'da %40 küçülür) kendi
        // yuvasının seviyesine bağlanır; tel gerginliği ve grip'in +%15 kontrol tabanı aynen kalır. Kumaş = v1 formanın işlevi
        // (aynı formül), renk yalnız görünüm. Nötr modellerde (Dayanıklı, Sentetik, Klasik) her terim 1: v1 sayıları birebir.
        // Hücum'un nefes eksisi (yor > 1) raket seviyesiyle aynı oranda küçülür; Kesici'nin slice etkisi ve Havlu'nun yorgun
        // sapması ölçeklenmez (sayıya sığmayan "özel" etki, seviye tek bir sayıyı büyütür).
        const float RAKET_EKSI = .4f;
        static float Taban(float m, float kr) { return Math.Abs(m - 1) < 1e-4f ? 1 + .05f * kr : 1; }
        public GearStats Hesap()
        {
            var r = GEAR["raket"][raket]; var t = TelStats(tel); var kd = GEAR["kordaj"][kordaj]; var gp = GEAR["grip"][grip]; var b = GEAR["bileklik"][bileklik]; var h = GEAR["bant"][bant]; var f = GEAR["kumas"][kumas];
            var a = GEAR["ayakkabi"][ayakkabi];   // ayakkabı: yuva seviyesi yok, çarpanı olduğu gibi (ölçeklenmez)
            GearModel rn; if (!RENK.TryGetValue(renk, out rn)) rn = RENK["mavi"];
            float kr = KSv("raket"), kt = KSv("tel"), kg = KSv("grip"), kb = KSv("bileklik"), kh = KSv("bant"), kf = KSv("forma");
            return new GearStats {
                guc = Taban(r.guc.Value, kr) * Olcek(r.guc.Value, kr, .18f, RAKET_EKSI) * Olcek(t.guc, kt, 0, .5f) * Olcek(kd.guc ?? 1, kt, .18f, RAKET_EKSI) * Olcek(b.guc ?? 1, kb, .18f, .6f),
                kontrol = Taban(r.kontrol.Value, kr) * Olcek(r.kontrol.Value, kr, .18f, RAKET_EKSI) * Olcek(t.kontrol, kt, 0, .5f) * Olcek(kd.kontrol ?? 1, kt, .18f, RAKET_EKSI) * (1 + .15f * kg) * Olcek(gp.kontrol ?? 1, kg, .18f, RAKET_EKSI),
                hiz = Olcek(r.hiz.Value, kr, .18f, RAKET_EKSI) * Olcek(b.hiz ?? 1, kb, .18f, .6f) * Olcek(gp.hiz ?? 1, kg, .18f, RAKET_EKSI),
                tatli = Olcek(r.tatli.Value, kr, .18f, RAKET_EKSI) * Olcek(t.tatli, kt, 0, .5f) * Olcek(kd.tatli ?? 1, kt, .18f, RAKET_EKSI) * (1 + .15f * kt) * (1 + .10f * kh),
                ton = r.ton.Value * t.ton * (kd.ton ?? 1), kafa = r.kafa.Value, kenar = b.kenar ?? 1, akis = h.akis ?? 4, yavas = h.yavas ?? 1, rakip = h.rakip ?? 1,
                kosu = (1 + .04f * kf) * Olcek(f.kosu ?? 1, kf, .12f, 0) * (a.kosu ?? 1), enerji = Olcek(f.enerji ?? 1, kf, .12f, 0),
                toparla = (f.toparla.HasValue ? 1 - (1 - f.toparla.Value) * (1 + .12f * kf) : 1) * (a.toparla ?? 1),
                yorK = 1 / (1 + .15f * kb) * (r.yor.HasValue ? 1 + (r.yor.Value - 1) * (1 - RAKET_EKSI * kr) : 1) * (a.yor ?? 1),
                uzanma = a.uzanma ?? 1, jumpTatli = a.jumpTatli ?? 1,
                slice = r.slice ?? 1, yorgunSapma = gp.yorgunSapma ?? 1, gripDoku = gp.doku,
                forma = rn.renk, raketRenk = r.renk, bant = h.renk, bileklik = b.renk };
        }

        // ---- oyuncunun gördüğü 5 çubuk (tasarım §4.1, Ek A.1) ----
        // 14 çarpan değişmez; oyuncu bunlardan türeyen 5 çubuğu görür (0–100, 50 = nötr: Denge, 26 lbs, Sv 1). Ölçek ±%30 →
        // ±50 puan, kırpılır. Katsayılar tasarımın ilk tahmini; yuvarlama JsRound. Toplam bir "güç puanı" bilerek yok: tek
        // sayı "en iyi parça" algısı yaratır. Tarz çubuklara girmez (ekipman kartı ekipmanı anlatır; tarzın kendi kartı var).
        public const int GUC = 0, KONTROL = 1, TATLI = 2, CABUK = 3, DAYAN = 4;
        public static readonly string[] CUBUK_AD = { "Güç", "Kontrol", "Tatlı nokta", "Çabukluk", "Dayanıklılık" };
        static int C(double x) { return (int)Math.Max(0, Math.Min(100, JsRound(50 + (x - 1) * 166))); }
        public static int[] Cubuklar(GearStats g)
        {
            return new[] {
                C(g.guc * (1 + (g.enerji - 1) * .25)),
                C(g.kontrol * (1 + (1 - g.kenar) * .2)),
                C(g.tatli * (1 + (4 - g.akis) * .04) * (1 + (1 - g.yavas) * .3)),
                C((g.hiz + g.kosu + (2 - g.toparla)) / 3),
                C(1 / g.yorK) };
        }
        // Ayakkabı çubukları (2026-10-26; Soyunma odası › Karakter › Ayakkabı): takımın 5 çubuğu ayakkabının farkını boğuyordu
        // (Çabukluk üç değerin ortalaması, Uzanma ve Sıçrama hiç yok), bu yüzden ayakkabı kendi 5 çubuğuyla anlatılır, yalnız
        // modelin çarpanından (50 nötr, aynı ölçek C): İlk adım = koşu/ivme (kosu), Çabukluk = uzanma sonrası toparlanma
        // (2 − toparla), Uzanma = uzanma erişimi, Sıçrama = sıçrayarak smaç tatlı noktası, Nefes = 1/yor. Takımın çubuklarına da
        // kosu/toparla/yor yoluyla girer (Çabukluk, Dayanıklılık).
        public static readonly string[] AYK_CUBUK_AD = { "İlk adım", "Çabukluk", "Uzanma", "Sıçrama", "Nefes" };
        public static int[] AyakkabiCubuklar(GearModel m)
        {
            return new[] { C(m.kosu ?? 1), C(2 - (m.toparla ?? 1)), C(m.uzanma ?? 1), C(m.jumpTatli ?? 1), C(1 / (m.yor ?? 1)) };
        }
        // Bir yuvaya başka model takılsaydı (diğer her şey şimdiki gibi) 5 çubuk; yuva kartının ▲/▼ karşılaştırması bununla.
        public int[] CubuklarIle(string yuva, string id)
        {
            string eski = Secili(yuva); Ata(yuva, id); var c = Cubuklar(Hesap()); Ata(yuva, eski); return c;
        }

        // Yükseltme önizlemesi: yuvanın etkilediği değerler ek seviye ileride (eşit şartlar geçici kapalı).
        public Dictionary<string, float> YukDegerler(string k, int ek)
        {
            int eski = eko.sv[k]; bool eskiEsit = eko.esit; eko.esit = false; eko.sv[k] = Math.Min(10, eski + ek); var g = Hesap(); eko.sv[k] = eski; eko.esit = eskiEsit;
            var o = new Dictionary<string, float>();
            foreach (var a in YUK_ETKI[k]) o[a] = a == "day" ? 1 / g.yorK : a == "guc" ? g.guc : a == "kontrol" ? g.kontrol : a == "hiz" ? g.hiz : a == "tatli" ? g.tatli : a == "kosu" ? g.kosu : g.enerji;
            return o;
        }
        public bool YukseltOlur(string k, out int fiyat, out int kapi)
        {
            int sv = eko.sv[k]; fiyat = 0; kapi = 0; if (sv >= 10) return false;
            fiyat = YUK_FIYAT[sv - 1]; kapi = YUK_SV[sv - 1]; return eko.tuy >= fiyat && Seviye() >= kapi;
        }
        public bool Yukselt(string k)
        {
            int f, g; if (!YukseltOlur(k, out f, out g)) return false;
            eko.tuy -= f; eko.sv[k]++; EkoYaz(); return true;
        }

        // Maç sonu ödülü: 5 sayılık maç için değerler; uzun maç sayı başına adil (11 ×2, 21 ×3,5), Usta ×1,25.
        // Kaybetmek de ilerletir; günün ilk galibiyeti +50; seri ve kaçırma cezası yok.
        // 2026-10-26 kariyer haritası: ilk kez alınan her yıldız +20 Tüy (tasarım §3.1; yıldız kilit açmaz, ödül açar), okunan
        // her imza vuruşu +5 XP (§3.3). İkisi de isteğe bağlı parametre: eski çağrılar ve testler aynı sonucu verir.
        public OdulSonuc Odul(bool won, bool kariyerIlk, int target, bool easy, int gorevBitti, int perfects, DateTime bugun, int yeniYildiz = 0, int okudun = 0, int ekTuy = 0, int ekXp = 0, string ekAd = null)
        {
            float f = target == 5 ? 1 : target == 11 ? 2 : target == 21 ? 3.5f : 1, zor = easy ? 1 : 1.25f; int mk = Math.Min(10, perfects);
            var o = new OdulSonuc();
            o.tuy = (int)JsRound(((won ? 40 : 15) * f + gorevBitti * 15 + (gorevBitti == 3 ? 20 : 0) + mk) * zor);
            o.xp = (int)JsRound(((won ? 50 : 20) * f + gorevBitti * 15 + mk * 2) * zor);
            string gun = bugun.Year + "-" + bugun.Month + "-" + bugun.Day;
            if (won && eko.gun != gun) { eko.gun = gun; o.tuy += 50; o.ek.Add("günün ilk galibiyeti +50"); }
            if (kariyerIlk) { o.tuy += 60; o.xp += 40; o.ek.Add("yeni basamak +60"); }
            if (yeniYildiz > 0) { o.tuy += 20 * yeniYildiz; o.ek.Add("yeni yıldız +" + 20 * yeniYildiz); }
            if (okudun > 0) o.xp += 5 * okudun;
            // 2026-10-26: Günün maçı (+40 Tüy, +30 XP, günde bir kez kazanınca) ve Usta yolu ek ödülü çağırandan; ad ek listesine.
            if (ekTuy > 0 || ekXp > 0) { o.tuy += ekTuy; o.xp += ekXp; if (ekAd != null) o.ek.Add(ekAd + " +" + ekTuy); }
            o.once = Seviye(); eko.tuy += o.tuy; eko.xp += o.xp; EkoYaz(); o.sonra = Seviye();
            return o;
        }

        // Antrenman görevi ödülü (2026-10-26, tasarım §3.6): yalnız ilk kez alınan yıldız, yıldız başına +25 Tüy, +30 XP.
        // Aynı görevi tekrar oynamak ödül vermez (yıldız en iyi sonuçtur); maç ödülü (Odul) antrenmanda hiç yoktur.
        public const int DRIL_TUY = 25, DRIL_XP = 30;
        public OdulSonuc DrilOdul(int yeniYildiz)
        {
            var o = new OdulSonuc(); o.once = Seviye(); o.sonra = o.once; if (yeniYildiz <= 0) return o;
            o.tuy = DRIL_TUY * yeniYildiz; o.xp = DRIL_XP * yeniYildiz; o.ek.Add("yeni yıldız +" + o.tuy);
            eko.tuy += o.tuy; eko.xp += o.xp; EkoYaz(); o.sonra = Seviye(); return o;
        }
    }
}
