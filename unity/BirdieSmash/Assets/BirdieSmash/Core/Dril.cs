// Antrenman sahası (komple-oyun tasarımı §3.6, adım 7; 2026-10-26). Veri veri/antrenman.json (8 görev, 3'er yıldız).
// Temel ders (15 adımlı etkileşimli antrenman, Ogr.cs) olduğu gibi kalır; görevler onun yanında ayrı kısa sınavlardır.
// Neden Training (ogr) altyapısı üstünde, ayrı bir mod değil: skor yok, antrenör rakip, görev yok, imza yok, maç sonu ödülü
// yok kuralları antrenmanla birebir aynı ve Match'in her yerinde "ogr != null" ile zaten uygulanıyor. Görev o altyapıya tek
// bir adım olarak girer (OgrAdim.dril); adımın kendi akışı burada:
//   · Antrenör topu elle besler (DrilBesle; servis dizilişi yerine, top hemen gelir) ve geri oynamaz: her top tek vuruş.
//     Isınma vuruşu yok; 12 top ~60–90 sn (tasarımdaki süre). Kısa servis görevinde servisi oyuncu atar.
//   · Oyun durmaz (Temel dersteki "top gelince oyun durur" yalnız Temel derste); hız bandı ve hedef alanı görünür.
//   · Her top rallinin sonunda (Point) sayılır; başarı ölçüsü görevin olcu alanı (veri). İsabet ≥ esik[i] → i+1 yıldız.
//   · Yıldız en iyi sonuçtur, geri alınmaz; ödül yalnız ilk kez alınan yıldızda (Gear.DrilOdul: +25 Tüy, +30 XP).
//     Zaman baskısı, seri, rastgele ödül yok.
// Saf C#: Denge "dril" kipi botla her görevin ★★★ oranını ölçer (hedef %30–70), BotMac akışı ve kaydı doğrular.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class DrilTanim
    {
        public string id, ad, ogretir, nasil, besle, olcu; public string[] tip; public float[] alan; public bool kose; public int top = 12; public int[] esik = { 4, 7, 10 };
        public int Yildiz(int isabet) { int n = 0; foreach (var e in esik) if (isabet >= e) n++; return n; }
    }

    public static class Dril
    {
        public const int YILDIZ = 3;
        static DrilTanim[] liste;
        public static DrilTanim[] Liste { get { Yukle(); return liste; } }
        public static int ToplamYildiz { get { return Liste.Length * YILDIZ; } }
        public static void Sifirla() { liste = null; }
        public static DrilTanim Bul(string id) { foreach (var d in Liste) if (d.id == id) return d; return null; }

        static void Yukle()
        {
            if (liste != null) return;
            var o = Json.Obj(Json.Parse(KariyerVeri.Metin("antrenman"))); if (o == null) throw new InvalidOperationException("Birdie Smash: veri/antrenman.json okunamadı");
            var l = new List<DrilTanim>();
            foreach (var x in (List<object>)o["gorevler"])
            {
                var d = Json.Obj(x);
                var t = new DrilTanim { id = Json.Str(d, "id", ""), ad = Json.Str(d, "ad", ""), ogretir = Json.Str(d, "ogretir", ""), nasil = Json.Str(d, "nasil", ""), besle = Json.Str(d, "besle", null), olcu = Json.Str(d, "olcu", "alan"), kose = Json.Bool(d, "kose", false), top = (int)Json.Num(d, "top", 12) };
                var tl = d.ContainsKey("tip") ? d["tip"] as List<object> : null; if (tl != null) { t.tip = new string[tl.Count]; for (int i = 0; i < tl.Count; i++) t.tip[i] = (string)tl[i]; }
                var al = d.ContainsKey("alan") ? d["alan"] as List<object> : null; if (al != null) { t.alan = new float[4]; for (int i = 0; i < 4; i++) t.alan[i] = (float)(double)al[i]; }
                var el = d.ContainsKey("esik") ? d["esik"] as List<object> : null; if (el != null) { t.esik = new int[el.Count]; for (int i = 0; i < el.Count; i++) t.esik[i] = (int)(double)el[i]; }
                if (t.esik.Length != YILDIZ || t.esik[YILDIZ - 1] > t.top) throw new InvalidOperationException("Birdie Smash: antrenman.json " + t.id + " eşikleri");
                if (t.olcu == "alan" && (t.alan == null || t.tip == null)) throw new InvalidOperationException("Birdie Smash: antrenman.json " + t.id + " alan/tip yok");
                l.Add(t);
            }
            liste = l.ToArray();
        }

        // Maç sonu ipucunun görevi (tasarım §3.6: "Dışarı giden 3 vuruş: Derin clear görevine bak"). Yalnız kayıpta ve
        // yalnız henüz üç yıldızı alınmamış göreve; ölçü maçın kendi sayılarından, sabit öncelik (Ozet ile aynı yaklaşım).
        public static string Oner(Match M, bool won, Ilerleme p)
        {
            if (won || M.ogr != null) return null;
            var aday = new List<string>();
            if (M.outsBen >= 2) aday.Add("derin");
            if (M.stil != null && M.stil.id == "smasci" || M.smacKarsila == 0 && M.smashes == 0) aday.Add("savunma");
            if (M.kills == 0) aday.Add("kill");
            if (M.deceives == 0) aday.Add("aldat");
            if (M.splits == 0) aday.Add("split");
            foreach (var id in aday) if (Bul(id) != null && (p == null || p.DrilYildiz(id) < YILDIZ)) return id;
            return null;
        }
    }

    public partial class Match
    {
        DrilTanim ogrDril;
        public DrilTanim Dril { get { return ogr != null ? ogr.dril : null; } }
        // Görevin top başı durumu: köşe (0 sol, 1 sağ), bu topta başarı, oyuncu vurdu mu, split/aldatma sayacının top başı değeri.
        int drKose, drSplit0, drAldat0; bool drBasari, drVurdu;
        public bool DrilBitti { get { var d = Dril; return d != null && (ogr.dene >= d.top || ogr.bitisT > 0 || ogr.bitti); } }

        // Görev tek adımlı bir antrenmandır (OgrKur çağırır).
        void DrilKur(DrilTanim d)
        {
            var a = new OgrAdim { bas = d.ad, met = d.nasil, neden = d.ogretir, dril = d, besle = d.besle, servis = d.olcu == "servis" ? "kisa" : null, slice = d.olcu == "aldat" };
            a.tip = d.olcu == "kill" ? new[] { "kill" } : d.olcu == "karsila" ? new[] { "block", "lift" } : d.tip;
            ogr.liste = new List<OgrAdim> { a }; ogr.dril = d;
        }

        // Bu topun hedef alanı [x0, x1, y0, y1] (rakip yarısı); köşe görevinde sırayla sol ve sağ köşe. null = alan yok.
        public float[] DrilAlan()
        {
            var d = Dril; if (d == null || d.alan == null || d.olcu == "aldat" || d.olcu == "karsila" || d.olcu == "kill" || d.olcu == "split") return null;
            // Kısa servis: alanın genişliği servisin gittiği kutudan (çapraz; skor antrenmanda hep 0, kutu sabit), derinliği veriden.
            if (d.olcu == "servis" && serveBox != null) return new[] { Math.Min(serveBox.x0, serveBox.x1), Math.Max(serveBox.x0, serveBox.x1), d.alan[2], d.alan[3] };
            if (!d.kose || drKose == 0) return d.alan;
            return new[] { Court.CW - d.alan[1], Court.CW - d.alan[0], d.alan[2], d.alan[3] };
        }
        static bool Icinde(float[] a, float x, float y) { return a != null && x >= a[0] && x <= a[1] && y >= a[2] && y <= a[3]; }

        // Top başı (SetupServe): sayaçlar sıfırlanır, köşe değişir, antrenör beslemeye göre durur (smaç: orta sahada, gevşek
        // top: file önünde, diğerleri servis yerinde).
        void DrilTopBasi()
        {
            var d = Dril; if (d == null) return;
            drBasari = false; drVurdu = false; drSplit0 = splits; drAldat0 = deceives; drKose = ogr.dene % 2;
            if (d.besle == "smash") { op.x = Court.MID; op.y = 3.0f; }
            else if (d.besle == "gevsek") { op.x = Court.MID; op.y = Court.NET - 1.6f; }
        }

        // Antrenörün elle beslemesi (OpServe yerine). Geri sayım ya da süre baskısı yok: top sayısı biter, görev biter.
        void DrilBesle()
        {
            var d = Dril; if (d == null || DrilBitti) return;
            float hx = op.x, hy = op.y, vx, vy, vz; string b = d.besle;
            if (b == "karisik") b = ogr.dene % 2 == 0 ? "clear" : "drive";
            switch (b)
            {
                case "smash":
                    {   // gövdeye ya da yanına, maçtaki smaçtan biraz yumuşak (antrenman: %80)
                        float hz = 2.5f, tx = Court.MID + (R01() - .5f) * 2.4f, ty = Court.NET + 3.4f + R01() * 1.2f;
                        Shuttle.Aim(hx, hy, hz, tx, ty, false, 52 * .8f, out vx, out vy, out vz);
                        if (!FileGecer(hx, hy, hz, vx, vy, vz)) Shuttle.Aim(hx, hy, hz, tx, ty, true, .25f, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "smash", false, "good"); return;
                    }
                case "gevsek":
                    {
                        float hz = 1.0f; Shuttle.Aim(hx, hy, hz, Court.MID + (R01() - .5f) * 1.2f, Court.NET + 1.0f, true, 1.15f, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drop", false, "good"); return;
                    }
                case "drive":
                    {
                        float hz = 1.25f, tx = Court.MID + (R01() - .5f) * 2.0f; Shuttle.Aim(hx, hy, hz, tx, Court.NET + 4.6f, false, 18, out vx, out vy, out vz);
                        if (!FileGecer(hx, hy, hz, vx, vy, vz)) Shuttle.Aim(hx, hy, hz, tx, Court.NET + 4.6f, true, .3f, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drive", false, "good"); return;
                    }
                default:
                    {   // yüksek top arka sahaya: oyuncu baş üstünde alır
                        float hz = 1.0f, tx = Court.MID + (R01() - .5f) * 2.0f, ty = Court.L - 1.3f - R01() * .7f;
                        Shuttle.Aim(hx, hy, hz, tx, ty, true, LIFT_A, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "clear", false, "good"); return;
                    }
            }
        }

        // Oyuncunun topu yere düştü (Land, sayıdan önce): bu top başarılı mı.
        void DrilDusus()
        {
            var d = Dril; if (d == null || s.from != "me") return;
            bool ic = !s.netHit && !IsOut(s.x, s.y, s.serve);
            switch (d.olcu)
            {
                case "alan": drBasari = ic && !s.serve && Array.IndexOf(d.tip, s.type) >= 0 && Icinde(DrilAlan(), s.x, s.y); break;
                case "servis": drBasari = ic && s.serve && s.type == "drop" && !float.IsNaN(s.gecPay) && s.gecPay < DRIL_BANT && Icinde(DrilAlan(), s.x, s.y); break;
                case "aldat": drBasari = ic && deceives > drAldat0; break;
                // Savunma: geri dönen top ya kısa (servis çizgisinin önü: blok) ya derin (arka 2 m: lift) olmalı; ortaya düşen
                // karşılama maçta rakibe ikinci smacı verir. Kural ölçümle (Denge dril): "her dönüş sayılır" botta %100 ★★★ idi.
                case "karsila": drBasari = ic && drVurdu && (s.y >= Court.NET - Court.SSL || s.y <= 2.0f); break;
                case "kill": drBasari = ic && sonTip == "kill"; break;
                case "split": drBasari = drVurdu && splits > drSplit0; break;
            }
        }
        // Kısa servis "bandı sıyırır": file üstünden geçerken bant payı 30 cm'den az.
        public const float DRIL_BANT = .3f;

        // Top bitti (Point başı): say, söyle; son top sayıldıysa görev biter (1,4 sn sonra, Temel dersle aynı kapanış).
        string DrilTop(string winner, string why)
        {
            var d = Dril; ogr.dene++;
            if (drBasari) ogr.isabet++;
            string t = drBasari ? "Hedefte! ★ " + ogr.isabet + "/" + d.top : DrilNeden(why);
            if (ogr.dene >= d.top && ogr.bitisT <= 0 && !ogr.bitti) ogr.bitisT = 1.4f;
            return t;
        }
        // Kaçan topun tek kelimelik nedeni (oyuncu neyi düzelteceğini görsün).
        string DrilNeden(string why)
        {
            var d = Dril; if (!drVurdu) return d.olcu == "split" && splits <= drSplit0 ? "Split step yok: antrenör vururken dokun" : "Top kaçtı";
            if (s != null && s.netHit) return "Fileye takıldı";
            if (s != null && s.from == "me" && IsOut(s.x, s.y, s.serve)) return "Dışarı";
            switch (d.olcu)
            {
                case "alan": return s != null && Array.IndexOf(d.tip, s.type) < 0 ? "Vuruş türü başka" : "Alana düşmedi";
                case "servis": return s != null && !s.serve ? "Top kaçtı" : s != null && s.type != "drop" ? "Kısa servis değil: yavaş it" : s != null && !float.IsNaN(s.gecPay) && s.gecPay >= DRIL_BANT ? "Yüksek geçti: daha yavaş it" : "Alana düşmedi";
                case "aldat": return "Aldatmadı: kaydırırken yay çiz";
                case "karsila": return "Ortaya düştü: kısa blokla ya da derine kaldır";
                case "kill": return "File vuruşu değil: hızlı ve sert";
                case "split": return "Split step yok: antrenör vururken dokun";
            }
            return "Top kaçtı";
        }
        // "Görevi bitir": o ana kadarki isabetle biter (yıldız isabete göre; bırakmak hiçbir şey kaybettirmez).
        public void DrilBitir() { if (Dril != null && !ogr.bitti && ogr.bitisT <= 0) ogr.bitisT = .01f; }
        public int DrilSonucYildiz { get { var d = Dril; return d == null ? 0 : d.Yildiz(ogr.isabet); } }
    }
}
