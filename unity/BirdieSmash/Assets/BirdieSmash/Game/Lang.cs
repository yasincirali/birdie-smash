// İngilizce arayüz (HTML TR_EN + Tr()): Resources/tr_en.json, HTML'deki sözlüğün birebir kopyası (anahtar Türkçe metin,
// {0} yer tutucu). Türkçe varsayılan ve tek kaynak; sözlükte olmayan metin Türkçe kalır (HTML ile aynı davranış).
// Core bitmiş Türkçe cümle üretir ("Ralli 12", "Görev tamam: 3 slice vuruşu"); bunlar için yer tutuculu anahtarlar
// düzenli ifadeye çevrilir ve tam eşleşen cümle parçalarıyla çevrilir. Böylece Core'a dil bilgisi girmez.
// UNITY_EK: HTML'de Tr() ile sarılmamış ya da yalnız Unity'de olan birkaç metin (Unity'ye özgü, HTML sözlüğüne eklenmedi).
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public static class L
    {
        static Dictionary<string, string> d; static List<KeyValuePair<Regex, string>> kalip;
        static readonly Dictionary<string, string> UNITY_EK = new Dictionary<string, string> {
            {"Hedefte! ★","On target! ★"},{"İz geri geldi","Trail is back"},{"Lisanslar","Licences"},{"Yazı boyutu","Text size"},{"Sistem","System"},{"Normal","Normal"},{"Büyük","Large"},
            {"Game Center · Başarımlar","Game Center · Achievements"},{"Hareket azaltıldı · sistem","Reduced motion · system"},
            {"Dil","Language"},{"Lisanslar ve üçüncü taraf içerik","Licences and third-party content"},{"Duraklatıldı","Paused"},{"Yeni maç başlat","Start a new match"},
            {"Ana menü","Main menu"},{"Tam antrenman","Full training"},{"Antrenmanı geç","Skip training"},{"Anladım, devam","Got it, continue"},
            {"Hareket azaltıldı","Reduced motion"},{"Hareket tam","Full motion"},{"Ses açık","Sound on"},{"Ses kapalı","Sound off"},{"Geri","Back"},
            {"Şimdi kaydır!","Swipe now!"},{"Erken: top parlayınca kaydır","Early: swipe when the shuttle glows"},{"Nefes","Stamina"},{"Yorgun","Tired"},
            {"Kısa kaydırdın","Swipe was short"},{"Yavaş kaydırdın","Swipe was slow"},{"Kıl payı: topun üstünden geç","Close: swipe across the shuttle"},{"Topun uzağından geçtin","You swiped far from the shuttle"},
            {"Iska · {0}","Miss · {0}"},{"İÇERİ · {0} cm","IN · {0} cm"},{"DIŞARI · {0} cm","OUT · {0} cm"},{"Görev tamam: {0}","Mission done: {0}"}};
        public static bool En { get { return Ayar.dil == "en"; } }

        static void Yukle()
        {
            if (d != null) return; d = new Dictionary<string, string>(); kalip = new List<KeyValuePair<Regex, string>>();
            var t = Resources.Load<TextAsset>("tr_en"); var o = t != null ? Json.Obj(Json.Parse(t.text)) : null;
            if (o != null) foreach (var kv in o) if (kv.Value is string) d[kv.Key] = (string)kv.Value;
            foreach (var kv in UNITY_EK) if (!d.ContainsKey(kv.Key)) d[kv.Key] = kv.Value;
            foreach (var kv in d)
                if (kv.Key.Contains("{0}"))
                {
                    string re = "^" + Regex.Replace(Regex.Escape(kv.Key), @"\\\{(\d+)}", m => "(?<p" + m.Groups[1].Value + ">.+?)") + "$";
                    try { kalip.Add(new KeyValuePair<Regex, string>(new Regex(re), kv.Value)); } catch { }
                }
            kalip.Sort((a, b) => b.Key.ToString().Length.CompareTo(a.Key.ToString().Length));   // uzun (daha belirli) kalıp önce
        }
        // HTML Tr(k, ...a): anahtar çevrilir, sonra {i} yer tutucuları doldurulur.
        public static string T(string k, params object[] a)
        {
            string v = k; if (En) { Yukle(); string e; if (d.TryGetValue(k, out e)) v = e; }
            if (a != null && a.Length > 0) v = Regex.Replace(v, @"\{(\d+)\}", m => { int i = int.Parse(m.Groups[1].Value); return i < a.Length ? System.Convert.ToString(a[i]) : m.Value; });
            return v;
        }
        // Core'dan gelen bitmiş cümle: önce tam anahtar, sonra yer tutuculu kalıp; parçalar da (ör. görev adı) çevrilir.
        public static string Cumle(string s)
        {
            if (!En || string.IsNullOrEmpty(s)) return s; Yukle(); string e;
            if (d.TryGetValue(s, out e)) return e;
            foreach (var kv in kalip)
            {
                var m = kv.Key.Match(s); if (!m.Success) continue;
                return Regex.Replace(kv.Value, @"\{(\d+)\}", mm => { var g = m.Groups["p" + mm.Groups[1].Value]; return g.Success ? Cumle(g.Value) : mm.Value; });
            }
            // "A · B" ve "A: B" gibi birleşik cümleler parça parça
            if (s.Contains(" · ")) { var p = s.Split(new[] { " · " }, System.StringSplitOptions.None); for (int i = 0; i < p.Length; i++) p[i] = Cumle(p[i]); return string.Join(" · ", p); }
            return s;
        }
    }
}
