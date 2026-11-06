// Oyuncu kartı (2026-11-07, online raporu "Karakter, kıyafet, raket ve gösteriş"). Online'da gösterişin anlamı rakibin seni
// görmesi: kart maç başında karşıya gider (~200 bayt) ve onun telefonunda aynı görünüşü kurar (karşılaşma kartı, kortta
// rakip figürü). İçerik yalnız görünüş ve kimlik: ad, seviye, lig, unvan, model, kozmetik (Kozmetik.KAT), görünüm renkleri
// (Karakter.GORUNUM_KAT), raket ve ayakkabı MODELİ (görünüşü; derecelide güç sayılmaz, rapor "Skill gelişimi ve adalet").
// Güvenlik: kart karşı telefondan gelen veridir. Okurken yalnız bilinen kategoriler alınır, değerler [a-z0-9_-] ile ≤ 24
// karakter; veride olmayan öğe düşer (görünüm varsayılanı çizer). Böylece yeni sürümün öğesi eski sürümde çökme değil
// varsayılan görünüş olur; uydurma bir kimlik kortta hiçbir şey açmaz. Ad: ≤ 16 karakter, harf/rakam/boşluk/._-.
using System;
using System.Collections.Generic;
using System.Text;

namespace BirdieSmash.Core
{
    public class OyuncuKarti
    {
        public const int SURUM = 1;
        public string ad, model, unvan; public int seviye = 1, kupa = -1;   // kupa -1: yerleşmemiş (lig gösterilmez)
        public readonly Dictionary<string, string> gorunus = new Dictionary<string, string>();

        public static readonly string[] RAKET_KAT = { "raket", "ayakkabi_model" };   // Gear model yuvaları (görünüş)
        public static readonly string[] MODELLER = { "oyuncak", "rocketbox", "sade", "tuycuk" };

        public static bool KatBilinir(string k)
        {
            return Array.IndexOf(Kozmetik.KAT, k) >= 0 || Array.IndexOf(Karakter.GORUNUM_KAT, k) >= 0 || Array.IndexOf(RAKET_KAT, k) >= 0;
        }

        // Biçim: "1|ad|seviye|kupa|model|unvan|kat=id,kat=id". Ayırıcılar (| = ,) temizlenmiş değerlerde geçemez.
        public string Yaz()
        {
            var sb = new StringBuilder(); sb.Append(SURUM).Append('|').Append(AdTemiz(ad)).Append('|').Append(Math.Max(1, Math.Min(99, seviye))).Append('|').Append(Math.Max(-1, kupa))
              .Append('|').Append(Kimlik(model)).Append('|').Append(Kimlik(unvan)).Append('|');
            bool ilk = true;
            foreach (var kv in gorunus) { string v = Kimlik(kv.Value); if (!KatBilinir(kv.Key) || v == "") continue; if (!ilk) sb.Append(','); sb.Append(kv.Key).Append('=').Append(v); ilk = false; }
            return sb.ToString();
        }

        public static OyuncuKarti Oku(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length > 1024) return null;
            var p = s.Split('|'); if (p.Length != 7 || p[0] != SURUM.ToString()) return null;
            var k = new OyuncuKarti { ad = AdTemiz(p[1]) };
            int n; if (int.TryParse(p[2], out n)) k.seviye = Math.Max(1, Math.Min(99, n));
            if (int.TryParse(p[3], out n)) k.kupa = Math.Max(-1, Math.Min(99999, n));
            string m = Kimlik(p[4]); k.model = Array.IndexOf(MODELLER, m) >= 0 ? m : null;
            string u = Kimlik(p[5]); k.unvan = u != "" && Array.Exists(Karakter.Unvanlar, x => x.id == u) ? u : null;
            if (p[6].Length > 0)
                foreach (var cift in p[6].Split(','))
                {
                    int e = cift.IndexOf('='); if (e <= 0) continue;
                    string kat = cift.Substring(0, e), id = Kimlik(cift.Substring(e + 1));
                    if (id != "" && KatBilinir(kat) && !k.gorunus.ContainsKey(kat) && Var(kat, id)) k.gorunus[kat] = id;
                }
            return k;
        }

        // Öğe veride var mı (Kozmetik, Karakter görünüm, Gear modeli). Yoksa kart o kategoriyi taşımaz → varsayılan çizilir.
        static bool Var(string kat, string id)
        {
            if (Array.IndexOf(Kozmetik.KAT, kat) >= 0) return Kozmetik.Bul(kat, id) != null;
            if (Array.IndexOf(Karakter.GORUNUM_KAT, kat) >= 0) return id == "dogal" || id == "model" || Array.Exists(Karakter.Gorunum(kat), o => o.id == id);
            string yuva = kat == "ayakkabi_model" ? "ayakkabi" : kat;
            Dictionary<string, GearModel> d; return Gear.GEAR.TryGetValue(yuva, out d) && d.ContainsKey(id);
        }

        static string Kimlik(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(); foreach (char c in s) { if (sb.Length >= 24) break; if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_' || c == '-') sb.Append(c); }
            return sb.ToString();
        }
        static string AdTemiz(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(); foreach (char c in s.Trim()) { if (sb.Length >= 16) break; if (char.IsLetterOrDigit(c) || c == ' ' || c == '.' || c == '_' || c == '-') sb.Append(c); }
            return sb.ToString().Trim();
        }
    }
}
