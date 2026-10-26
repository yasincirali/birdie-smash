// Kariyer ve rakip tarzı verisi (komple-oyun tasarımı Ek A.4, 2026-10-26): kaynak tool/veri/kariyer.json ve stil.json,
// Unity kopyası Assets/Resources/veri (tool/veri_kopyala.js). Neden kodda değil JSON'da: tasarım "veri tek kaynak" diyor;
// turlar, basamaklar, g değerleri ve tarz parametreleri denge turlarında sık değişecek, mantık (Match) değişmeden.
// Core UnityEngine görmez: metni Kaynak verir (Unity: Game/VeriYukle → Resources; bot testleri: dosyadan). Kaynak
// verilmediyse çalışma klasöründen yukarı doğru tool/veri aranır (mono testleri depo kökünden ya da alt klasörden koşar).
// Veri yoksa açık bir hata atılır: sessizce boş kariyerle açılan oyun, eksik dosyayı gizlerdi.
// Dizin: genel basamak indeksi g = tur·6 + basamak. Tur 1'de bu eski Match.KARIYER indeksiyle aynıdır (tuytop_v2.kariyer
// ve Style.kariyer anlamını korur).
using System;
using System.Collections.Generic;
using System.IO;

namespace BirdieSmash.Core
{
    public class ImzaBilgi { public string id, ad, uyari, cevap, renk; public string[] sira; }

    public class KariyerTur { public string id, ad; public int epMin, epMax; public Kariyer[] b = new Kariyer[0]; public bool Var { get { return b.Length > 0; } } }

    public static class KariyerVeri
    {
        public const int TUR = 4, BASAMAK = 6;
        public static Func<string, string> Kaynak;   // ad ("kariyer" | "stil" | "gear" | "karakter") → JSON metni
        static KariyerTur[] turlar; static Dictionary<string, Style> stil; static Dictionary<string, ImzaBilgi> imza; static string[] sira;

        public static KariyerTur[] Turlar { get { Yukle(); return turlar; } }
        public static Dictionary<string, Style> Stil { get { Yukle(); return stil; } }
        public static Dictionary<string, ImzaBilgi> Imzalar { get { Yukle(); return imza; } }
        public static string[] StilSira { get { Yukle(); return sira; } }

        public static int Global(int tur, int b) { return tur * BASAMAK + b; }
        public static int TurOf(int g) { return g / BASAMAK; }
        public static int BasOf(int g) { return g % BASAMAK; }
        public static bool Var(int g) { return g >= 0 && TurOf(g) < Turlar.Length && BasOf(g) < Turlar[TurOf(g)].b.Length; }
        public static Kariyer Al(int g) { return Var(g) ? Turlar[TurOf(g)].b[BasOf(g)] : null; }
        public static bool Final(int g) { return BasOf(g) == BASAMAK - 1; }

        // Basamağın rakibi: tarzın kopyası, hız ×g, hata ÷g (bugünkü kural). İmza vuruşu yalnız patronda (final).
        public static Style Rakip(int g)
        {
            var k = Al(g); var b = Stil[k.stil].Clone(); b.hiz *= k.g; b.hata /= k.g; b.kariyer = g; b.patron = k.patron; if (!k.patron) b.imza = null; return b;
        }
        public static ImzaBilgi Imza(string id) { ImzaBilgi b; return id != null && Imzalar.TryGetValue(id, out b) ? b : null; }

        public static void Sifirla() { turlar = null; stil = null; imza = null; sira = null; Gear.VeriSifirla(); Karakter.Sifirla(); }
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
                var tur = new KariyerTur { id = Json.Str(d, "id", "tur" + t), ad = Json.Str(d, "ad", ""), epMin = (int)ep[0], epMax = (int)ep[ep.Length - 1] };
                var bl = new List<Kariyer>(); var bj = d["basamaklar"] as List<object>;
                for (int i = 0; bj != null && i < bj.Count && i < BASAMAK; i++)
                {
                    var b = Json.Obj(bj[i]);
                    var k = new Kariyer { stil = Json.Str(b, "stil", "duvar"), g = F(b, "g", 1), yer = Json.Str(b, "yer", ""), saha = Json.Str(b, "saha", "salon"), uzunluk = (int)Json.Num(b, "uzunluk", 0), gorev = Json.Str(b, "gorev", null), patron = Json.Bool(b, "patron", false), tur = t, basamak = i };
                    if (!st.ContainsKey(k.stil)) throw new InvalidOperationException("Birdie Smash: kariyer.json bilinmeyen tarz " + k.stil);
                    bl.Add(k);
                }
                if (bl.Count != 0 && bl.Count != BASAMAK) throw new InvalidOperationException("Birdie Smash: tur " + (t + 1) + " altı basamak değil");   // harita ve kayıt 6 sabit
                tur.b = bl.ToArray(); tl.Add(tur);
            }
            var sl = new List<string>(); object so2; if (so.TryGetValue("sira", out so2)) foreach (var x in (List<object>)so2) sl.Add((string)x);
            stil = st; imza = im; sira = sl.ToArray(); turlar = tl.ToArray();
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
