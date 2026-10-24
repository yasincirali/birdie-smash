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
    public class GearModel
    {
        public string id, ad, n, renk; public bool kilit;
        public float? guc, kontrol, hiz, tatli, ton, kafa, kenar, yavas, rakip, kosu, enerji, toparla; public int? akis;
    }

    public class TelStat { public string ad, n; public float guc, tatli, kontrol, ton; }

    // gsHesap çıktısı: maç fiziğinin okuduğu çarpanlar. Varsayılan nötr (1) — bot testleri ve eski imza bununla koşar.
    public class GearStats
    {
        public float guc = 1, kontrol = 1, hiz = 1, tatli = 1, ton = 1, kafa = 1, kenar = 1, yavas = 1, rakip = 1, kosu = 1, enerji = 1, toparla = 1, yorK = 1;
        public int akis = 4;
        public string forma = "#4fb8ff", raketRenk = "#e9eef0", bant, bileklik;
        public static readonly GearStats Notr = new GearStats();
    }

    public class Yuva { public string k, ad, n; }

    public class Ekonomi
    {
        public int tuy, xp; public string gun = ""; public bool esit;
        public readonly Dictionary<string, int> sv = new Dictionary<string, int> { { "raket", 1 }, { "tel", 1 }, { "grip", 1 }, { "bileklik", 1 }, { "bant", 1 }, { "forma", 1 } };
    }

    public class OdulSonuc { public int tuy, xp, once, sonra; public List<string> ek = new List<string>(); }

    public class Gear
    {
        public static readonly Dictionary<string, Dictionary<string, GearModel>> GEAR = new Dictionary<string, Dictionary<string, GearModel>> {
            {"raket", Sira(
                new GearModel{id="denge",ad="Denge",n="Her şeyden biraz",guc=1,kontrol=1,hiz=1,tatli=1,ton=1,kafa=1,renk="#e9eef0"},
                new GearModel{id="agir",ad="Kafa ağır",n="Smaç +12 · clear uzun · drive −7 · tatlı nokta dar",guc=1.12f,kontrol=.92f,hiz=.93f,tatli=.85f,ton=.9f,kafa=.78f,renk="#ff9f7a"},
                new GearModel{id="hafif",ad="Kafa hafif",n="Drive/file +8 · smaç −7 · clear kısa",guc=.93f,kontrol=1.05f,hiz=1.08f,tatli=1,ton=1.1f,kafa=1.18f,renk="#7ad7ff"},
                new GearModel{id="esnek",ad="Esnek şaft",n="Tatlı nokta geniş · sapma az · smaç −10",guc=.9f,kontrol=1.3f,hiz=1,tatli=1.3f,ton=.95f,kafa=1,renk="#d8ff5a"})},
            {"bileklik", Sira(
                new GearModel{id="yok",ad="Yok",n="—"},
                new GearModel{id="pamuk",ad="Pamuk",n="Sıyırınca top yarı yarıya az kaçar",kenar=.5f,renk="#e9eef0"},
                new GearModel{id="agir",ad="Ağırlıklı",n="Smaç +4 · drive −4",guc=1.04f,hiz=.96f,renk="#3a4a4e"})},
            {"bant", Sira(
                new GearModel{id="yok",ad="Yok",n="—"},
                new GearModel{id="ter",ad="Ter bandı",n="Akış 3 tam ortada açılır",akis=3,renk="#e9eef0"},
                new GearModel{id="odak",ad="Odak bandı",n="Vuruş anı %15 daha yavaş akar · rakip %6 hızlı",yavas=.85f,rakip=1.06f,renk="#ffd23a"})},
            {"forma", Sira(
                new GearModel{id="mavi",ad="Mavi",n="Klasik",renk="#4fb8ff"},
                new GearModel{id="fistik",ad="Fıstık",n="Hafif kumaş: koşu +6",renk="#d8ff5a",kosu=1.06f},
                new GearModel{id="beyaz",ad="Beyaz",n="Enerji %40 hızlı dolar",renk="#f2f6f4",enerji=1.4f},
                new GearModel{id="mor",ad="Mor",n="Uzanma sonrası toparlanma kısa",renk="#b48cff",toparla=.6f},
                new GearModel{id="sampiyon",ad="Şampiyon",n="Kariyeri bitirince açılır · yalnız görünüm",renk="#ffc94a",kilit=true})}};
        static Dictionary<string, GearModel> Sira(params GearModel[] a) { var d = new Dictionary<string, GearModel>(); foreach (var m in a) d[m.id] = m; return d; }
        public static readonly string[] GEAR_SIRA = { "raket", "tel", "bileklik", "bant", "forma" };

        // Tel gerginliği sürekli (18–32 lbs): tel frekansı √gerginlik ile artar; gevşek tel güç ve geniş tatlı nokta, sıkı tel kontrol. 26 nötr.
        public static TelStat TelStats(float T)
        {
            float d = 26 - T;
            return new TelStat { ad = T + " lbs", n = T <= 22 ? "Gevşek: güç artar, tatlı nokta geniş, tok ve uzun çınlama" : T >= 30 ? "Sıkı: kontrol artar, tatlı nokta dar, tiz ve kısa çınlama" : "Orta",
                guc = 1 + d * .012f, tatli = 1 + d * .035f, kontrol = 1 - d * .025f, ton = (float)Math.Sqrt(T / 26) };
        }

        public static readonly Yuva[] YUVA = {
            new Yuva{k="raket",ad="Raket",n="Güç ve kontrol artar; modelin artısı büyür, eksisi küçülür"}, new Yuva{k="tel",ad="Kordaj",n="Tatlı nokta genişler, gerginliğin eksisi azalır"},
            new Yuva{k="grip",ad="Grip",n="Kontrol artar: top daha az sapar"}, new Yuva{k="bileklik",ad="Bileklik",n="Nefes daha yavaş tükenir"},
            new Yuva{k="bant",ad="Kafa bandı",n="Tam orta penceresi genişler"}, new Yuva{k="forma",ad="Forma",n="Koşu hızlanır, formanın etkisi büyür"}};
        public static readonly int[] YUK_FIYAT = { 50, 110, 180, 290, 450, 680, 1000, 1450, 2100 }, YUK_SV = { 1, 2, 4, 6, 9, 12, 16, 20, 24 };
        public static readonly Dictionary<string, string[]> YUK_ETKI = new Dictionary<string, string[]> {
            {"raket",new[]{"guc","kontrol","hiz"}},{"tel",new[]{"tatli","guc"}},{"grip",new[]{"kontrol"}},{"bileklik",new[]{"day","guc","hiz"}},{"bant",new[]{"tatli"}},{"forma",new[]{"kosu","enerji"}}};
        public static readonly Dictionary<string, string> ETKI_AD = new Dictionary<string, string> {
            {"guc","Güç"},{"kontrol","Kontrol"},{"hiz","Drive/file"},{"tatli","Tatlı nokta"},{"kosu","Koşu"},{"enerji","Smaç gücü dolumu"},{"day","Dayanıklılık"}};

        // seçili modeller (HTML G) + ekonomi
        public string raket = "denge", bileklik = "yok", bant = "yok", forma = "mavi"; public float tel = 26;
        public Ekonomi eko = new Ekonomi();
        public bool kariyerModu;
        readonly IKayit kayit;

        public Gear(IKayit k, int eskiGalibiyet)
        {
            kayit = k;
            var g = Json.Obj(Json.Parse(k.Get("tuytop_gear")));
            if (g != null)
            {
                object tv; if (g.TryGetValue("tel", out tv)) { double v = tv is string ? ParseD(((string)tv).Replace("t", "")) : tv is double ? (double)tv : 0; if (v >= 18 && v <= 32) tel = (float)v; }
                raket = Model("raket", Json.Str(g, "raket", raket)); bileklik = Model("bileklik", Json.Str(g, "bileklik", bileklik));
                bant = Model("bant", Json.Str(g, "bant", bant)); forma = Model("forma", Json.Str(g, "forma", forma));
            }
            var e = Json.Obj(Json.Parse(k.Get("tuytop_ekonomi")));
            if (e != null)
            {
                eko.tuy = (int)Json.Num(e, "tuy", 0); eko.xp = (int)Json.Num(e, "xp", 0); eko.gun = Json.Str(e, "gun", ""); eko.esit = Json.Bool(e, "esit", false);
                var sv = Json.Obj(e.ContainsKey("sv") ? e["sv"] : null);
                foreach (var y in YUVA) eko.sv[y.k] = (int)Json.Num(sv, y.k, 1);
            }
            else { eko.tuy = eskiGalibiyet > 0 ? 100 + Math.Min(1000, eskiGalibiyet * 25) : 0; EkoYaz(); }   // eski oyuncu sıfırdan başlamasın
        }
        static double ParseD(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0; }
        string Model(string k, string v) { return GEAR[k].ContainsKey(v) ? v : (k == "raket" ? "denge" : k == "forma" ? "mavi" : "yok"); }

        public void GearYaz()
        {
            kayit.Set("tuytop_gear", Json.Write(new Dictionary<string, object> { { "raket", raket }, { "tel", (double)tel }, { "bileklik", bileklik }, { "bant", bant }, { "forma", forma } }));
        }
        public void EkoYaz()
        {
            var sv = new Dictionary<string, object>(); foreach (var kv in eko.sv) sv[kv.Key] = (double)kv.Value;
            kayit.Set("tuytop_ekonomi", Json.Write(new Dictionary<string, object> { { "tuy", (double)eko.tuy }, { "xp", (double)eko.xp }, { "sv", sv }, { "gun", eko.gun }, { "esit", eko.esit } }));
        }

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
        public GearStats Hesap()
        {
            var r = GEAR["raket"][raket]; var t = TelStats(tel); var b = GEAR["bileklik"][bileklik]; var h = GEAR["bant"][bant]; var f = GEAR["forma"][forma];
            float kr = KSv("raket"), kt = KSv("tel"), kg = KSv("grip"), kb = KSv("bileklik"), kh = KSv("bant"), kf = KSv("forma");
            return new GearStats {
                guc = (1 + .05f * kr) * Olcek(r.guc.Value, kr, .18f, .6f) * Olcek(t.guc, kt, 0, .5f) * Olcek(b.guc ?? 1, kb, .18f, .6f),
                kontrol = (1 + .05f * kr) * Olcek(r.kontrol.Value, kr, .18f, .6f) * Olcek(t.kontrol, kt, 0, .5f) * (1 + .15f * kg),
                hiz = Olcek(r.hiz.Value, kr, .18f, .6f) * Olcek(b.hiz ?? 1, kb, .18f, .6f),
                tatli = Olcek(r.tatli.Value, kr, .18f, .6f) * Olcek(t.tatli, kt, 0, .5f) * (1 + .15f * kt) * (1 + .10f * kh),
                ton = r.ton.Value * t.ton, kafa = r.kafa.Value, kenar = b.kenar ?? 1, akis = h.akis ?? 4, yavas = h.yavas ?? 1, rakip = h.rakip ?? 1,
                kosu = (1 + .04f * kf) * Olcek(f.kosu ?? 1, kf, .12f, 0), enerji = Olcek(f.enerji ?? 1, kf, .12f, 0),
                toparla = f.toparla.HasValue ? 1 - (1 - f.toparla.Value) * (1 + .12f * kf) : 1, yorK = 1 / (1 + .15f * kb),
                forma = f.renk, raketRenk = r.renk, bant = h.renk, bileklik = b.renk };
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
        public OdulSonuc Odul(bool won, bool kariyerIlk, int target, bool easy, int gorevBitti, int perfects, DateTime bugun)
        {
            float f = target == 5 ? 1 : target == 11 ? 2 : target == 21 ? 3.5f : 1, zor = easy ? 1 : 1.25f; int mk = Math.Min(10, perfects);
            var o = new OdulSonuc();
            o.tuy = (int)JsRound(((won ? 40 : 15) * f + gorevBitti * 15 + (gorevBitti == 3 ? 20 : 0) + mk) * zor);
            o.xp = (int)JsRound(((won ? 50 : 20) * f + gorevBitti * 15 + mk * 2) * zor);
            string gun = bugun.Year + "-" + bugun.Month + "-" + bugun.Day;
            if (won && eko.gun != gun) { eko.gun = gun; o.tuy += 50; o.ek.Add("günün ilk galibiyeti +50"); }
            if (kariyerIlk) { o.tuy += 60; o.xp += 40; o.ek.Add("yeni basamak +60"); }
            o.once = Seviye(); eko.tuy += o.tuy; eko.xp += o.xp; EkoYaz(); o.sonra = Seviye();
            return o;
        }
    }
}
