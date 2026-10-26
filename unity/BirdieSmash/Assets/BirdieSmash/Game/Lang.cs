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
            {"Iska · {0}","Miss · {0}"},{"İÇERİ · {0} cm","IN · {0} cm"},{"DIŞARI · {0} cm","OUT · {0} cm"},{"Görev tamam: {0}","Mission done: {0}"},
            // 2026-10-26 UI Toolkit menü kabuğu (sekmeler, Kariyer listesi, Profil, Ayarlar grupları; HTML dondurulduğu için yalnız Unity)
            {"Soyunma odası","Locker room"},{"Profil","Profile"},{"Sıradaki maç","Next match"},{"Kariyer tamam","Career complete"},{"Final","Final"},
            {"Geçildi","Cleared"},{"ŞİMDİ","NOW"},{"Kilitli","Locked"},{"basamak","stage"},{"Yeniden oyna","Play again"},{"Kapat","Close"},
            {"Hızlı maç oyna","Play quick match"},{"Rakip her maçta rastgele: Smaşçı, Duvar ya da File oyuncusu.","The opponent is random each match: Smasher, Wall or Net player."},
            {"İlk kez: 3 adımlık antrenman, sonra 5 sayılık maç","First time: 3-step training, then a 5-point match"},{"Sonraki seviyeye {0} XP","{0} XP to next level"},
            {"Tel gerginliği","String tension"},{"Tüy","Feathers"},{"Oyun","Game"},{"Hareket","Motion"},{"Yardım","Help"},{"Oyuncu","Player"},
            {"İstatistikler","Stats"},{"Kazanıldı","Earned"},{"Henüz değil","Not yet"},{"Antrenmanı bitir","Finish the training"},{"Akışa gir","Get into flow"},
            {"20 vuruşluk ralli","20-shot rally"},{"Sıçrayarak smaç","Jump smash"},{"Bir maçta 3 görev","3 missions in one match"},
            // 2026-10-26 kariyer haritası ve imza vuruşları (adım 2–3; HTML dondurulduğu için yalnız Unity)
            {"Önerilen ekipman puanı {0}–{1} · seninki {2}","Suggested gear score {0}–{1} · yours {2}"},{"{0}/{1} yıldız","{0}/{1} stars"},{"{0}/3 yıldız","{0}/3 stars"},{"{0} yeni","{0} new"},{"{0} finalini kazan","Win the {0} final"},{"{0} finalini kazan · yakında","Win the {0} final · coming soon"},{"Yakında: bu tur sonraki güncellemede gelir","Coming soon: this tour arrives in a later update"},{"Tur {0}","Tour {0}"},{"tamam","complete"},{"Açık","Open"},{"{0}. basamak","Stage {0}"},{"İmza vuruşu","Signature shot"},{"{0} sayı","{0} points"},{"{0} sayı (senin seçimin)","{0} points (your choice)"},{"Yıldızlar","Stars"},{"Yıldız","Star"},{"Kazan","Win"},{"Basamak görevi","Stage mission"},{"Farkla kazan: rakip en çok {0} sayı","Win big: opponent scores {0} or fewer"},{"öneri, kilit değil","a suggestion, not a lock"},{"Önce {0} basamağını kazan","Win {0} first"},{"Alındı","Earned"},{"Kariyer yıldızı","Career stars"},{"yeni yıldız +{0}","new star +{0}"},{"Yerel yol","Local road"},{"Asya Turu","Asia Tour"},{"Avrupa Turu","Europe Tour"},{"Dünya Finalleri","World Finals"},{"Jakarta","Jakarta"},{"Bangkok","Bangkok"},{"Seul","Seoul"},{"Tokyo","Tokyo"},{"Kuala Lumpur yarı finali","Kuala Lumpur semi-final"},{"Kuala Lumpur finali","Kuala Lumpur final"},{"Kariyer tamam! Şampiyon forması ve {0} açıldı.","Career complete! Champion shirt and {0} unlocked."},{"{0} tamam! {1} açıldı.","{0} complete! {1} unlocked."},{"{0} tamam! Sonraki tur yakında.","{0} complete! The next tour is coming soon."},{"Şampiyon! Finali yeniden oynarsın","Champion! You can replay the final"},{"Drive'cı","Driver"},{"Aldatıcı","Trickster"},{"Komple","All-rounder"},{"Düz ve hızlı vurur, orta yükseklikte. Topu yükselt, onu arkaya it","Hits flat and fast at mid height. Lift the shuttle and push her back"},{"Son anda yön değiştirir. Merkezde bekle, acele etme","Changes direction at the last moment. Wait in the centre, don't rush"},{"En çok kullandığın vuruşa göre yer alır. Vuruşlarını karıştır","Positions for the shot you use most. Mix up your shots"},{"Yıldırım smaç","Lightning smash"},{"Yıldırım smaç: çapraz gelir!","Lightning smash: it comes cross-court!"},{"Hazırlıkta raketinde turuncu ışık. Hep çapraz gelir: karşıla","Orange light on the racket while winding up. Always cross-court: return it"},{"Duvar blok","Wall block"},{"Duvar blok: smaç yerine drop at!","Wall block: drop instead of smashing!"},{"Raket önde, mavi ışık: smaçını fileye bloklar. Drop at","Racket up front, blue light: he blocks your smash to the net. Play a drop"},{"Tül drop","Silk drop"},{"Tül drop: erken öne çık!","Silk drop: move in early!"},{"Bilek kırılır, pembe ışık: top fileyi sıyırır. Erken öne çık","Wrist cocks, pink light: the shuttle skims the net. Move in early"},{"Mermi drive","Bullet drive"},{"Mermi drive: blokla ya da kaldır!","Bullet drive: block or lift it!"},{"Yere eğilir, mor ışık: iki düz drive, ikincisi gövdene. Blokla ya da kaldır","Crouches low, purple light: two flat drives, the second at your body. Block or lift"},{"Gölge flick","Shadow flick"},{"Gölge flick: geri adım at!","Shadow flick: step back!"},{"Drop duruşunda durur, yeşil ışık: arka çizgiye flick. Geri adım at","Holds a drop stance, green light: flicks to the back line. Step back"},{"Üç imza","Three signatures"},{"Aria önceki patronların imzalarını sırayla dener!","Aria tries the earlier bosses' signatures in turn!"},{"Duvar blok, mermi drive, gölge flick: sırayla gelir","Wall block, bullet drive, shadow flick: they come in turn"},{"Okudun!","Read it!"},{"3 smaçı karşıla","Return 3 smashes"},{"2 drop sayısı","Win 2 points with drops"},{"İmza vuruşunu oku","Read the signature shot"}};
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
