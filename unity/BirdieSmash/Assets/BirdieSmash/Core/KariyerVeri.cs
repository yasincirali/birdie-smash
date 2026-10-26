// Kariyer ve rakip tarzı verisi (komple-oyun tasarımı Ek A.4, 2026-10-26): kaynak tool/veri/kariyer.json ve stil.json,
// Unity kopyası Assets/Resources/veri (tool/veri_kopyala.js). Neden kodda değil JSON'da: tasarım "veri tek kaynak" diyor;
// turlar, basamaklar, g değerleri ve tarz parametreleri denge turlarında sık değişecek, mantık (Match) değişmeden.
// Core UnityEngine görmez: metni Kaynak verir (Unity: Game/VeriYukle → Resources; bot testleri: dosyadan). Kaynak
// verilmediyse çalışma klasöründen yukarı doğru tool/veri aranır (mono testleri depo kökünden ya da alt klasörden koşar).
// Veri yoksa açık bir hata atılır: sessizce boş kariyerle açılan oyun, eksik dosyayı gizlerdi.
// Dizin: genel basamak indeksi g = tur·6 + basamak. Tur 1'de bu eski Match.KARIYER indeksiyle aynıdır (tuytop_v2.kariyer
// ve Style.kariyer anlamını korur).
// 2026-10-26 Tur 3–4 ve Usta yolu (adım 9): turun ek zorluğu (tepki, hucum; §3.2 "Tur 3 ve 4'te rakip tepki gecikmesi
// kısalır ve hücum kararı artar": g yalnız hız ve hatayı ölçekliyordu, üst turlar "daha az hata yapan duvar" gibi
// okunuyordu; daha erken çıkış ve daha çok smaç/drive "daha hızlı oyun" olarak okunur). imza: finalde olmayan basamakta da
// imza vuruşu (Tur 4'te önceki patronlar). Usta yolu verisi kariyer.json "usta" (Core/UstaYolu).
using System;
using System.Collections.Generic;
using System.IO;

namespace BirdieSmash.Core
{
    public class ImzaBilgi { public string id, ad, uyari, cevap, renk; public string[] sira; }

    public class KariyerTur { public string id, ad; public int epMin, epMax; public float tepki = 1, hucum = 1; public Kariyer[] b = new Kariyer[0]; public bool Var { get { return b.Length > 0; } } }
    // Usta yolu (kariyer.json "usta"): g = g + adim × çevrim, en çok tavan; tepki/hucum Tur 4 gibi ek zorluk.
    public class UstaVeri { public float g = 1, adim = .02f, tavan = 1.2f, tepki = 1, hucum = 1; public int uzunluk = 5, finalUzunluk = 11; }

    public static class KariyerVeri
    {
        public const int TUR = 4, BASAMAK = 6;
        public static Func<string, string> Kaynak;   // ad ("kariyer" | "stil" | "gear" | "karakter") → JSON metni
        static KariyerTur[] turlar; static Dictionary<string, Style> stil; static Dictionary<string, ImzaBilgi> imza; static string[] sira; static UstaVeri usta;

        public static KariyerTur[] Turlar { get { Yukle(); return turlar; } }
        public static Dictionary<string, Style> Stil { get { Yukle(); return stil; } }
        public static Dictionary<string, ImzaBilgi> Imzalar { get { Yukle(); return imza; } }
        public static string[] StilSira { get { Yukle(); return sira; } }
        public static UstaVeri Usta { get { Yukle(); return usta; } }

        public static int Global(int tur, int b) { return tur * BASAMAK + b; }
        public static int TurOf(int g) { return g / BASAMAK; }
        public static int BasOf(int g) { return g % BASAMAK; }
        public static bool Var(int g) { return g >= 0 && TurOf(g) < Turlar.Length && BasOf(g) < Turlar[TurOf(g)].b.Length; }
        public static Kariyer Al(int g) { return Var(g) ? Turlar[TurOf(g)].b[BasOf(g)] : null; }
        public static bool Final(int g) { return BasOf(g) == BASAMAK - 1; }

        // Basamağın rakibi: tarzın kopyası, hız ×g, hata ÷g (bugünkü kural). İmza vuruşu patronda (final) ve imza bayraklı
        // basamakta (Tur 4). Turun ek zorluğu tepki ve hücum çarpanı olarak biner (Tur 1–2'de 1: eski davranış birebir).
        public static Style Rakip(int g)
        {
            var k = Al(g); var t = Turlar[TurOf(g)]; var b = Stil[k.stil].Clone(); b.hiz *= k.g; b.hata /= k.g; b.kariyer = g;
            b.patron = k.patron || k.imza; if (!b.patron) b.imza = null; b.tepki *= t.tepki; b.hucum *= t.hucum; return b;
        }
        public static ImzaBilgi Imza(string id) { ImzaBilgi b; return id != null && Imzalar.TryGetValue(id, out b) ? b : null; }

        public static void Sifirla() { turlar = null; stil = null; imza = null; sira = null; usta = null; Gear.VeriSifirla(); Karakter.Sifirla(); Dril.Sifirla(); Gunluk.Sifirla(); }
        // 2026-10-26 ekipman v2 ve karakter: gear.json ve karakter.json da aynı kaynaktan (Unity Resources, testte tool/veri).
        public static string Metin(string ad) { return Oku(ad); }

        static void Yukle()
        {
            if (turlar != null) return;
            var so = Json.Obj(Json.Parse(Oku("stil"))); var ko = Json.Obj(Json.Parse(Oku("kariyer")));
            if (so == null || ko == null) throw new InvalidOperationException("Birdie Smash: veri/stil.json ya da veri/kariyer.json okunamadı");
            var st = new Dictionary<string, Style>(); var ss = Json.Obj(so["stiller"]);
            foreach (var kv in ss)
            {
                var d = Json.Obj(kv.Value);
                st[kv.Key] = new Style
                {
                    id = kv.Key, isim = Json.Str(d, "isim", null), renk = Json.Str(d, "renk", null), yedek = Json.Str(d, "yedek", null), ad = Json.Str(d, "ad", kv.Key), ipucu = Json.Str(d, "ipucu", ""),
                    ustK = Strs(d, "ustK"), ustP = Nums(d, "ustP"), altK = Strs(d, "altK"), altP = Nums(d, "altP"),
                    hiz = F(d, "hiz", 1), hata = F(d, "hata", 1), ev = F(d, "ev", 3), yorK = F(d, "yorK", 1), geriK = F(d, "geriK", 1),
                    aldatP = F(d, "aldatP", 0), uyum = F(d, "uyum", 0), imza = Json.Str(d, "imza", null)
                };
            }
            var im = new Dictionary<string, ImzaBilgi>(); object io; if (so.TryGetValue("imzalar", out io))
                foreach (var kv in Json.Obj(io)) { var d = Json.Obj(kv.Value); im[kv.Key] = new ImzaBilgi { id = kv.Key, ad = Json.Str(d, "ad", kv.Key), uyari = Json.Str(d, "uyari", ""), cevap = Json.Str(d, "cevap", ""), renk = Json.Str(d, "renk", "#ffffff"), sira = d.ContainsKey("sira") ? Strs(d, "sira") : null }; }
            var tl = new List<KariyerTur>(); var tj = ko["turlar"] as List<object>;
            for (int t = 0; t < tj.Count && t < TUR; t++)
            {
                var d = Json.Obj(tj[t]); var ep = d.ContainsKey("ep") ? Nums(d, "ep") : new float[] { 0, 0 };
                var tur = new KariyerTur { id = Json.Str(d, "id", "tur" + t), ad = Json.Str(d, "ad", ""), epMin = (int)ep[0], epMax = (int)ep[ep.Length - 1], tepki = F(d, "tepki", 1), hucum = F(d, "hucum", 1) };
                var bl = new List<Kariyer>(); var bj = d["basamaklar"] as List<object>;
                for (int i = 0; bj != null && i < bj.Count && i < BASAMAK; i++)
                {
                    var b = Json.Obj(bj[i]);
                    var k = new Kariyer { stil = Json.Str(b, "stil", "duvar"), g = F(b, "g", 1), yer = Json.Str(b, "yer", ""), saha = Json.Str(b, "saha", "salon"), uzunluk = (int)Json.Num(b, "uzunluk", 0), gorev = Json.Str(b, "gorev", null), patron = Json.Bool(b, "patron", false), imza = Json.Bool(b, "imza", false), tur = t, basamak = i };
                    if (!st.ContainsKey(k.stil)) throw new InvalidOperationException("Birdie Smash: kariyer.json bilinmeyen tarz " + k.stil);
                    bl.Add(k);
                }
                if (bl.Count != 0 && bl.Count != BASAMAK) throw new InvalidOperationException("Birdie Smash: tur " + (t + 1) + " altı basamak değil");   // harita ve kayıt 6 sabit
                tur.b = bl.ToArray(); tl.Add(tur);
            }
            var sl = new List<string>(); object so2; if (so.TryGetValue("sira", out so2)) foreach (var x in (List<object>)so2) sl.Add((string)x);
            var u = new UstaVeri(); object uo; if (ko.TryGetValue("usta", out uo))
            {
                var d = Json.Obj(uo); u.g = F(d, "g", u.g); u.adim = F(d, "adim", u.adim); u.tavan = F(d, "tavan", u.tavan); u.tepki = F(d, "tepki", 1); u.hucum = F(d, "hucum", 1);
                u.uzunluk = (int)Json.Num(d, "uzunluk", u.uzunluk); u.finalUzunluk = (int)Json.Num(d, "finalUzunluk", u.finalUzunluk);
            }
            stil = st; imza = im; sira = sl.ToArray(); usta = u; turlar = tl.ToArray();
        }

        static float F(Dictionary<string, object> d, string k, float def) { return (float)Json.Num(d, k, def); }
        static string[] Strs(Dictionary<string, object> d, string k) { var l = d[k] as List<object>; var o = new string[l.Count]; for (int i = 0; i < o.Length; i++) o[i] = (string)l[i]; return o; }
        static float[] Nums(Dictionary<string, object> d, string k) { var l = d[k] as List<object>; var o = new float[l.Count]; for (int i = 0; i < o.Length; i++) o[i] = (float)(double)l[i]; return o; }

        static string Oku(string ad)
        {
            if (Kaynak != null) { var s = Kaynak(ad); if (s != null) return s; }
            // Unity'siz (mono) yedek: çalışma klasöründen ve programın klasöründen yukarı doğru tool/veri.
            foreach (var bas in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
            {
                var d = bas;
                for (int i = 0; i < 8 && !string.IsNullOrEmpty(d); i++)
                {
                    var p = Path.Combine(Path.Combine(Path.Combine(d, "tool"), "veri"), ad + ".json"); if (File.Exists(p)) return File.ReadAllText(p);
                    d = Path.GetDirectoryName(d);
                }
            }
            var env = Environment.GetEnvironmentVariable("BS_VERI"); if (!string.IsNullOrEmpty(env)) { var p = Path.Combine(env, ad + ".json"); if (File.Exists(p)) return File.ReadAllText(p); }
            return null;
        }
    }
}
