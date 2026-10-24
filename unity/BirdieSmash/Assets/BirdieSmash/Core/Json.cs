// En küçük JSON okur/yazar: HTML localStorage'daki tuytop_gear / tuytop_ekonomi / tuytop_v2 değerleri JSON nesnesidir;
// Unity'de aynı biçim PlayerPrefs'e string olarak yazılır ki iki sürüm aynı kayıt şeklini konuşsun. JsonUtility
// sözlük okuyamadığı ve Core UnityEngine görmediği için elle yazıldı (nesne, dizi, sayı, metin, bool, null).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BirdieSmash.Core
{
    public static class Json
    {
        public static object Parse(string s) { if (string.IsNullOrEmpty(s)) return null; int i = 0; try { return Val(s, ref i); } catch { return null; } }
        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        static object Val(string s, ref int i)
        {
            Ws(s, ref i); char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true) { Ws(s, ref i); string k = Str(s, ref i); Ws(s, ref i); i++; d[k] = Val(s, ref i); Ws(s, ref i); if (s[i] == ',') { i++; continue; } i++; return d; }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true) { l.Add(Val(s, ref i)); Ws(s, ref i); if (s[i] == ',') { i++; continue; } i++; return l; }
            }
            if (c == '"') return Str(s, ref i);
            if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
            if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
            if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
            int a = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(a, i - a), CultureInfo.InvariantCulture);
        }
        static string Str(string s, ref int i)
        {
            var b = new StringBuilder(); i++;
            while (s[i] != '"')
            {
                if (s[i] == '\\') { i++; char e = s[i]; if (e == 'n') b.Append('\n'); else if (e == 't') b.Append('\t'); else if (e == 'u') { b.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; } else b.Append(e); }
                else b.Append(s[i]);
                i++;
            }
            i++; return b.ToString();
        }
        public static string Write(object o)
        {
            if (o == null) return "null";
            if (o is string) { var b = new StringBuilder("\""); foreach (char c in (string)o) { if (c == '"' || c == '\\') b.Append('\\').Append(c); else if (c == '\n') b.Append("\\n"); else if (c < 32) b.Append("\\u").Append(((int)c).ToString("x4")); else b.Append(c); } return b.Append('"').ToString(); }
            if (o is bool) return (bool)o ? "true" : "false";
            if (o is IDictionary<string, object>) { var b = new StringBuilder("{"); bool f = true; foreach (var kv in (IDictionary<string, object>)o) { if (!f) b.Append(','); f = false; b.Append(Write(kv.Key)).Append(':').Append(Write(kv.Value)); } return b.Append('}').ToString(); }
            if (o is System.Collections.IEnumerable) { var b = new StringBuilder("["); bool f = true; foreach (var v in (System.Collections.IEnumerable)o) { if (!f) b.Append(','); f = false; b.Append(Write(v)); } return b.Append(']').ToString(); }
            return Convert.ToDouble(o).ToString("R", CultureInfo.InvariantCulture);
        }
        // yardımcılar: eksik/yanlış türde alan varsayılana düşer (bozuk kayıt oyunu açılmaz hale getirmesin)
        public static Dictionary<string, object> Obj(object o) { return o as Dictionary<string, object>; }
        public static double Num(Dictionary<string, object> d, string k, double def) { object v; return d != null && d.TryGetValue(k, out v) && v is double ? (double)v : def; }
        public static string Str(Dictionary<string, object> d, string k, string def) { object v; return d != null && d.TryGetValue(k, out v) && v is string ? (string)v : def; }
        public static bool Bool(Dictionary<string, object> d, string k, bool def) { object v; return d != null && d.TryGetValue(k, out v) && v is bool ? (bool)v : def; }
    }
}
