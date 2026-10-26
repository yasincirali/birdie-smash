// Aldatıcı vuruşlar (2026-10-26, yasin: "Badmintonda deceptive trickshot'lar olmalı, üst seviyelerde çıkmalı; ekipmanlar ve
// seviye, aynı zamanda pozisyon uygunsa yapılabilmeli"). Veri tool/veri/aldatma.json (tek kaynak, Resources/veri kopyası);
// tasarım notu /mnt/project-files/birdie-smash/komple-oyun/aldatici_vuruslar.md. Saf C#: BotMac kapıları, etkiyi, riski ve
// rakip sıklığını ölçer.
// Neden üç kapı ayrı ayrı: gerçekte aldatma bir beceri (seviye), bir alet (kafa hafif raket file oyununu, kesici yüz ters
// slice'ı mümkün kılar) ve her şeyden önce bir zaman meselesidir: topa erken yerleşmeyen, uzanan, koşarken vuran ya da nefesi
// bitmiş oyuncu aldatamaz, ancak topu karşıya atar. Bu yüzden kapı kapalıyken vuruş reddedilmez, jestin normal karşılığı
// (drop, lift, drive, slice) çıkar: oyuncu cezalandırılmaz, yalnız aldatma olmaz.
// Konum tek sayıya iner (pay 0–1): her koşulun "sınır" ve "rahat" ucu var; sınırın dışı = olmaz, sınır ile rahat arası
// marjinal. Marjinal konumda etki küçülür ve hata olasılığı artar (risk: kötü vuruşa dönme olasılığı + isabet sapması).
// Bonus sayı yok (İmza kural 4 ile aynı): aldatma yalnız bir vuruştur; rakibi geç ve ters çıkarır, sayıyı oyun verir.
// Okunurluk (İmza felsefesi): (1) vuruş mümkünken HUD halenin altında adı ve jesti yazar, (2) her vuruşun ilk fırsatında tek
// seferlik açıklama (cihazda tutulur, HileGorulen), (3) kilitli vuruşlar Antrenman sayfasında ne açtığıyla listelenir.
// Rakip (Tur 3+ ve Usta yolu): aynı vuruşlardan file ve baş üstü olanları, İmza'nın sıklık diliyle (maçta en çok azami,
// aralarında en az ara sayı) ve görünür hazırlıkla (raket ışığı ≥ 0,4 sn, Kolay'da +0,2) dener; topu geri çevirmek "Okudun!"
// (yalnız geri bildirim, imza sayacına ve XP'ye girmez: imza patronun tek fikridir, aldatma tur rakibinin süsü).
// Kayıt ve oyuncu seviyesi Game'den: OyuncuSv (Ayar.GearHesap yazar), HileGorulen + HileGorulduKaydet (Ayar, Ilerleme).
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class HileTanim
    {
        public string id, ad, kisa, jest, jestKod, baglam, konum, ilk, uyari, ekStat, ekAd, yon, vTip;
        public int sv; public float ekMin, tepki, adim, riskTaban, riskMarj, sapma, vD, vVal, takip, vNefes;
        public bool vAci, vCapraz, slice, rakipte;
        public Dictionary<string, float[]> kosul = new Dictionary<string, float[]>();
    }
    public class HileRakipKural { public int kariyerMin = 12, azami = 3, ara = 3; public float p = .3f, hazirlik = .4f; public Dictionary<string, float> stil = new Dictionary<string, float>(); }
    // Kapı denetimi: ok değilse neden "seviye" | "ekipman" | "konum" | "jest" | "baglam", kosul = konumda tutmayan koşul.
    public class HileDenetim { public bool ok; public string neden, kosul; public float pay; public HileTanim t; }

    public static class Aldatma
    {
        public const float BEKLE = .25f;   // tutma jesti: dokunuştan kaydırmaya en az bu kadar (gerçek sn)
        static HileTanim[] liste; static HileRakipKural rakip; static float okTur = .15f, okUsta = .5f, okTavan = .6f;
        public static HileTanim[] Liste { get { Yukle(); return liste; } }
        public static HileRakipKural Rakip { get { Yukle(); return rakip; } }
        public static void Sifirla() { liste = null; rakip = null; }
        public static HileTanim Bul(string id) { foreach (var t in Liste) if (t.id == id) return t; return null; }
        public static readonly string[] KOSUL = { "file", "z", "sure", "hiz", "nefes", "onde" };
        public static readonly string[] STAT = { "kafa", "slice", "kontrol", "hiz", "guc" };

        static void Yukle()
        {
            if (liste != null) return;
            var o = Json.Obj(Json.Parse(KariyerVeri.Metin("aldatma"))); if (o == null) throw new InvalidOperationException("Birdie Smash: veri/aldatma.json okunamadı");
            var l = new List<HileTanim>();
            foreach (var x in (List<object>)o["vuruslar"])
            {
                var d = Json.Obj(x); var ek = Json.Obj(d["ekipman"]); var et = Json.Obj(d["etki"]); var rk = Json.Obj(d["risk"]); var vu = Json.Obj(d["vurus"]);
                var t = new HileTanim {
                    id = Json.Str(d, "id", "?"), ad = Json.Str(d, "ad", ""), kisa = Json.Str(d, "kisa", ""), jest = Json.Str(d, "jest", ""), jestKod = Json.Str(d, "jestKod", ""),
                    baglam = Json.Str(d, "baglam", "file"), konum = Json.Str(d, "konum", ""), ilk = Json.Str(d, "ilk", ""), uyari = Json.Str(d, "uyari", null), sv = (int)Json.Num(d, "sv", 1),
                    ekStat = Json.Str(ek, "stat", "kafa"), ekMin = (float)Json.Num(ek, "min", 0), ekAd = Json.Str(ek, "ad", ""),
                    tepki = (float)Json.Num(et, "tepki", .2), adim = (float)Json.Num(et, "adim", .2), yon = Json.Str(et, "yon", "ayna"),
                    riskTaban = (float)Json.Num(rk, "taban", 0), riskMarj = (float)Json.Num(rk, "marj", 0), sapma = (float)Json.Num(rk, "sapma", 0),
                    vTip = Json.Str(vu, "tip", "drop"), vD = (float)Json.Num(vu, "d", 1), vVal = (float)Json.Num(vu, "val", .7), vAci = Json.Bool(vu, "aci", true), vCapraz = Json.Bool(vu, "capraz", false),
                    slice = Json.Bool(vu, "slice", false), takip = (float)Json.Num(vu, "takip", 0), vNefes = (float)Json.Num(vu, "nefes", 0), rakipte = Json.Bool(d, "rakipte", false) };
                var ko = Json.Obj(d["kosul"]);
                foreach (var kv in ko)
                {
                    if (Array.IndexOf(KOSUL, kv.Key) < 0) throw new InvalidOperationException("Birdie Smash: aldatma.json bilinmeyen koşul " + kv.Key);   // yazım hatası sessiz kalmasın
                    var a = kv.Value as List<object>; if (a == null || a.Count != 4) throw new InvalidOperationException("Birdie Smash: aldatma.json koşul 4 sayı olmalı: " + t.id + "." + kv.Key);
                    var f = new float[4]; for (int i = 0; i < 4; i++) f[i] = (float)(double)a[i]; t.kosul[kv.Key] = f;
                }
                if (Array.IndexOf(STAT, t.ekStat) < 0) throw new InvalidOperationException("Birdie Smash: aldatma.json bilinmeyen ekipman değeri " + t.ekStat);
                l.Add(t);
            }
            var r = new HileRakipKural(); var ro = Json.Obj(o["rakip"]);
            r.kariyerMin = (int)Json.Num(ro, "kariyerMin", r.kariyerMin); r.azami = (int)Json.Num(ro, "azami", r.azami); r.ara = (int)Json.Num(ro, "ara", r.ara);
            r.p = (float)Json.Num(ro, "p", r.p); r.hazirlik = (float)Json.Num(ro, "hazirlik", r.hazirlik);
            var so = ro != null && ro.ContainsKey("stil") ? Json.Obj(ro["stil"]) : null; if (so != null) foreach (var kv in so) if (kv.Value is double) r.stil[kv.Key] = (float)(double)kv.Value;
            var ok = o.ContainsKey("okuma") ? Json.Obj(o["okuma"]) : null;
            okTur = (float)Json.Num(ok, "tur", okTur); okUsta = (float)Json.Num(ok, "usta", okUsta); okTavan = (float)Json.Num(ok, "tavan", okTavan);
            rakip = r; liste = l.ToArray();
        }

        // Rakibin aldatmayı okuması (0 = hiç, tavan .6): üst tur rakibi daha az aldanır. Tur başına okuma.tur, Usta yolu okuma.usta.
        public static float Okuma(Style st)
        {
            Yukle(); if (st == null) return 0;
            float o = st.usta >= 0 ? okUsta : st.kariyer >= 0 ? KariyerVeri.TurOf(st.kariyer) * okTur : 0;
            return Math.Max(0, Math.Min(okTavan, o));
        }

        public static float Stat(GearStats g, string ad)
        {
            switch (ad) { case "kafa": return g.kafa; case "slice": return g.slice; case "kontrol": return g.kontrol; case "hiz": return g.hiz; default: return g.guc; }
        }
        public static bool Ekipman(HileTanim t, GearStats g) { return Stat(g ?? GearStats.Notr, t.ekStat) >= t.ekMin - 1e-4f; }

        // Aralık [a, b, c, d]: v < a ya da v > d → -1 (olmaz); [b, c] içinde 1; sınır ile rahat arası doğrusal.
        public static float Aralik(float v, float[] r)
        {
            if (v < r[0] || v > r[3]) return -1; float p = 1;
            if (v < r[1]) p = Math.Min(p, (v - r[0]) / Math.Max(1e-4f, r[1] - r[0]));
            if (v > r[2]) p = Math.Min(p, (r[3] - v) / Math.Max(1e-4f, r[3] - r[2]));
            return p;
        }
        // Marjinal konumda kötü vuruşa dönme olasılığı ve isabet sapması çarpanı (pay 1 = tabanda).
        public static float HataP(HileTanim t, float pay) { return Math.Min(.8f, t.riskTaban + t.riskMarj * (1 - Math.Max(0, Math.Min(1, pay)))); }
        public static float Sapma(HileTanim t, float pay) { return 1 + t.sapma * (1 - Math.Max(0, Math.Min(1, pay))); }

        // Jest (GameRoot'un parmak izinden; Unity'siz test edilsin diye burada): x/y referans px (640 yükseklik, y aşağı), t ms.
        // bekle: dokunuştan kaydırmanın başladığı ana kadar (p0'dan 18 px; GameRoot'un "moved" eşiği) geçen süre; iz 300 ms'de
        // kırpıldığı için ilk örnek zaten uzaktaysa bilinmez ve 0 sayılır (yanlışlıkla tutma sayılmasın). Kırık (V): yarılar
        // arası açı > 2,3 rad ve iki kol da belirgin; kavis (yay): 0,8 rad üstü (GameRoot.Curved ile aynı eşik), yönü
        // çapraz çarpımın işaretinden (ekranda sola bükülen yay -1).
        public static void JestOku(float[] x, float[] y, float[] t, int n, float p0x, float p0y, float t0, ref SwipeInput sw)
        {
            sw.bekle = 0; sw.kavis = 0; sw.kirik = false; if (n < 2) return;
            int i0 = -1; for (int i = 0; i < n; i++) if (Hyp(x[i] - p0x, y[i] - p0y) > 18) { i0 = i; break; }
            if (i0 > 0) sw.bekle = Math.Max(0, (t[i0 - 1] - t0) / 1000f);
            int a = Math.Max(0, i0 - 1), c = n - 1, b = (a + c) / 2; if (c - a < 3) return;
            float ux = x[b] - x[a], uy = y[b] - y[a], vx = x[c] - x[b], vy = y[c] - y[b];
            float a1 = (float)Math.Atan2(uy, ux), a2 = (float)Math.Atan2(vy, vx), da = Math.Abs(a2 - a1); if (da > Math.PI) da = (float)(2 * Math.PI) - da;
            float L1 = Hyp(ux, uy), L2 = Hyp(vx, vy);
            if (da > 2.3f && L1 > 20 && L2 > 25) sw.kirik = true;
            else if (da > .8f && L2 > 25) sw.kavis = ux * vy - uy * vx < 0 ? -1 : 1;
        }
        static float Hyp(float a, float b) { return (float)Math.Sqrt(a * a + b * b); }

        // Jest eşleşmesi (vuruşun kendi tür kararından bağımsız): kavis = kavisli ve yavaş; terskavis = sola bükülen yay; tutma
        // + hızlı/yavaş yukarı; kırık (V); yatay = yataya yakın ve hızlı.
        public static bool Jest(HileTanim t, SwipeInput sw)
        {
            if (sw.tap) return false; float up = -sw.dy;
            switch (t.jestKod)
            {
                case "kavis": return sw.curved && !sw.kirik && sw.speed < 1.0f;
                case "terskavis": return sw.curved && !sw.kirik && sw.kavis < 0 && sw.speed < 1.0f;
                case "bekle_hizli": return sw.bekle >= BEKLE && sw.speed >= .9f && up > 0;
                case "bekle_yavas": return sw.bekle >= BEKLE && sw.speed < .75f && up > 0 && !sw.curved;
                case "kirik": return sw.kirik && sw.speed < 1.2f;
                case "yatay": return Math.Abs(sw.dx) > 1.6f * Math.Abs(sw.dy) && sw.speed >= 1.0f;
            }
            return false;
        }
    }

    public partial class Match
    {
        // Oyuncu seviyesi ve görülen ilk açıklamalar (Game yazar; bot testleri doğrudan). Varsayılan Sv 1: eski bot maçları birebir.
        public static int OyuncuSv = 1;
        public static readonly HashSet<string> HileGorulen = new HashSet<string>(); public static Action<string> HileGorulduKaydet;
        public int oyuncuSv = OyuncuSv;
        public static int HileIlkSv { get { int m = 99; foreach (var t in Aldatma.Liste) m = Math.Min(m, t.sv); return m; } }
        public int hileler, hileOkunan; public string sonHile;   // hileler: yapılan aldatıcı vuruş (görev), sonHile: son vuruşun kimliği
        public const float DURGUN_V = 1.0f;   // m/s: bunun altında oyuncu topa "yerleşmiş" sayılır
        Shuttle hileS; public float durgunT; HileDenetim hileEtki; string hileRedSon;
        Intercept hileI; HileTanim hileR; float hileBas; bool hileBasladi; int hileRSon = -99; Shuttle hileTop; string hileUyarildi = "";
        public int hileRSay;

        // ---- oyuncu ----
        // Bir vuruşun üç kapısı, şimdiki top ve konum için (jest hariç). HUD, BotMac ve SwipeShot aynı denetimi kullanır.
        public HileDenetim HileDenetle(HileTanim t)
        {
            var D = new HileDenetim { t = t };
            if (oyuncuSv < t.sv) { D.neden = "seviye"; return D; }
            if (!Aldatma.Ekipman(t, GS)) { D.neden = "ekipman"; return D; }
            Reach I; if (s == null || s.dead || s.from != "op" || !ReachInfo(out I)) { D.neden = "baglam"; return D; }
            string el = HandOf(me, "me", s.x, s.z);
            bool bag = t.baglam == "file" ? !I.over && !I.kill : t.baglam == "ust" ? I.over && el == "fore" : el == "back";
            if (!bag) { D.neden = "baglam"; return D; }
            if (I.lunge) { D.neden = "konum"; D.kosul = "uzanma"; return D; }   // uzanan oyuncu aldatamaz: top yalnız karşıya gider
            float pay = 1;
            foreach (var kv in t.kosul)
            {
                float v = kv.Key == "file" ? s.y - Court.NET : kv.Key == "z" ? s.z : kv.Key == "sure" ? durgunT : kv.Key == "hiz" ? Hyp(me.vx, me.vy) : kv.Key == "nefes" ? me.nefes : me.y - s.y;
                float p = Aldatma.Aralik(v, kv.Value); if (p < 0) { D.neden = "konum"; D.kosul = kv.Key; return D; }
                pay = Math.Min(pay, p);
            }
            D.ok = true; D.pay = pay; return D;
        }
        public HileDenetim HileDenetle(string id) { return HileDenetle(Aldatma.Bul(id)); }

        // HUD ipucu: şu an (jest dışında) yapılabilecek en üst seviyeli vuruş; yoksa null.
        public HileTanim HileFirsat()
        {
            if (s == null || s.dead || s.from != "op" || between || ogr != null && ogr.dur) return null;
            var L = Aldatma.Liste; for (int i = L.Length - 1; i >= 0; i--) if (HileDenetle(L[i]).ok) return L[i];
            return null;
        }

        // SwipeShot'ın başında: jest bir vuruşa uyuyorsa üç kapı; geçerse planı vuruşun planıyla değiştirir. Kapı kapalıysa
        // plan aynen kalır (jestin normal vuruşu). Rakibin aldatmasını geri çevirmek de burada sayılır.
        HileDenetim HileSec(SwipeInput sw, Reach I, ref ShotPlanR P)
        {
            if (hileTop != null && s == hileTop) { hileTop = null; hileOkunan++; Emit(Ev.Okudun, "me", "aldatma", me.x, me.y, 1.2f); Say("me", "okudun", me.x, me.y, 1.2f, "Okudun!", 2); }
            if (sw.tap) return null;
            var L = Aldatma.Liste; HileDenetim red = null;
            for (int i = L.Length - 1; i >= 0; i--)   // üst seviye önce: V çizgisi kavis de sayılır, çift hareket çevirmeden önce
            {
                var t = L[i]; if (!Aldatma.Jest(t, sw)) continue;
                var D = HileDenetle(t); if (D.ok) { HilePlan(t, sw, ref P); return D; }
                if (red == null && D.neden != "seviye" && D.neden != "baglam") red = D;
            }
            // Açık bir vuruşun jesti yapıldı ama ekipman ya da konum tutmadı: neden tek kelimeyle (öğretir; aynı neden üst üste yazılmaz).
            if (red != null) { string n = HileRedYazi(red); if (n != hileRedSon) { hileRedSon = n; Say("me", "aldatma", me.x, me.y, 1.5f, n, 1); } }
            return null;
        }
        public static string HileRedYazi(HileDenetim D)
        {
            if (D.neden == "ekipman") return D.t.ad + " · " + D.t.ekAd + " gerekli";
            switch (D.kosul)
            {
                case "uzanma": return D.t.ad + " · uzanırken olmaz";
                case "file": return D.t.ad + " · fileye çok uzak";
                case "z": return D.t.ad + " · temas yüksekliği uymadı";
                case "sure": return D.t.ad + " · topa geç yerleştin";
                case "hiz": return D.t.ad + " · koşarken olmaz";
                case "nefes": return D.t.ad + " · nefesin yetmedi";
                default: return D.t.ad + " · topun arkasına geç";
            }
        }

        // Vuruşun planı (yön oyuncunun parmağından, tür ve derinlik vuruştan). Çapraz: vurulan kanadın karşı köşesi.
        void HilePlan(HileTanim t, SwipeInput sw, ref ShotPlanR P)
        {
            float r = Clamp(sw.dx / Math.Max(50, Math.Abs(sw.dy)), -1.3f, 1.3f);
            P.type = t.vTip; P.d = t.vD; P.byAngle = t.vAci; P.val = t.vAci ? t.vVal : t.vVal * GS.hiz;
            P.tx = t.vCapraz ? (P.hx < Court.MID ? Court.SR - .8f : Court.SL + .8f) : t.baglam == "file" ? P.hx + r * 1.4f : Court.MID + r * 2.4f;
            P.tx = Clamp(P.tx, Court.SL + .25f, Court.SR - .25f);
        }

        // SwipeShot'ta isabet hesabından sonra: marjinal konum kötü vuruşa dönebilir, sapma büyür; slice vuruşun kendisinden.
        // Var olan slice aldatması (deceive) bu vuruşta yerini vuruşun kendi etkisine bırakır (iki kez sayılmasın).
        void HileUygula(HileDenetim D, ref string q, ref bool slice, ref float sig)
        {
            var t = D.t; sonHile = t.id; hileler++;
            if (R01() < Aldatma.HataP(t, D.pay)) q = "poor";
            sig *= Aldatma.Sapma(t, D.pay); if (q == "poor") sig = Math.Max(sig, .9f / GS.kontrol);
            bool yeni = t.slice && q != "poor"; if (slice && !yeni) slices--; else if (!slice && yeni) slices++; slice = yeni; deceive = false;
            if (t.vNefes > 0) Yor(t.vNefes);
            if (q != "poor") hileEtki = D;   // kötüye dönen aldatma aldatmaz: top yalnız karşıya gider
            Say("me", "aldatma", me.x, me.y, 1.6f, q == "poor" ? t.ad + " · sıyırdın" : t.ad + "!", 2);
        }
        // Uçuş başladıktan sonra: takip (durdurulan ileri salınım: son anda kes'te kısa, arkadan kurtarışta uzun).
        void HileSonra(HileDenetim D) { if (D.t.takip > 0) me.swHiz = 1 / D.t.takip; }

        // PlanIntercept'in rakip dalı: bekleyen aldatma etkisi. Etki = vuruşun tepkisi × (½ + ½·pay) × (1 − rakip okuması) ×
        // ekipmanın slice/aldatma çarpanı (Kesici %15, Aldatıcı tarz %10). Yön: "ayna" rakip topun düştüğü yerin aynasına adım
        // atar, "yerinde" donar (tut-flick, çevirme, son anda kes: rakip yerinden kıpırdayamaz).
        float HileTepki(float react, SimResult r)
        {
            var D = hileEtki; hileEtki = null; if (D == null) return react;
            float k = (.5f + .5f * D.pay) * (1 - Aldatma.Okuma(stil)) * GS.slice;
            react += D.t.tepki * k; op.fakeUntil = clock + D.t.adim * k;
            op.fakeX = D.t.yon == "ayna" ? Clamp(2 * Court.MID - r.x, Court.SL, Court.SR) : op.x;
            deceives++; Emit(Ev.Aldat, "op", D.t.id, op.x, op.y, 0, k); GorevKontrol();
            return react;
        }

        // Update'te, top uçarken: oyuncunun topa yerleşme süresi, ilk fırsat açıklaması, rakibin aldatma hazırlığı.
        void HileTick(Intercept I, float d)
        {
            if (s != hileS) { hileS = s; durgunT = 0; }
            if (s != null && !s.dead && s.from == "op")
            {
                if (Hyp(me.vx, me.vy) < DURGUN_V) durgunT += d; else durgunT = 0;
                if (ogr == null && HitWindow())
                {
                    var f = HileFirsat();
                    if (f != null && !HileGorulen.Contains(f.id)) { HileGorulen.Add(f.id); if (HileGorulduKaydet != null) HileGorulduKaydet(f.id); Say("me", "aldatma", me.x, me.y, 1.8f, f.ilk, 3); }
                }
            }
            if (I != null && I == hileI && !hileBasladi && s != null && s.from == "me" && s.t >= hileBas)
            {
                hileBasladi = true; hileRSay++; hileRSon = sc[0] + sc[1]; op.imzaT = I.t - s.t; op.imzaId = "aldatma";   // ışık + poz: İmza'nın görünümü (renk yok = beyaz)
                if (hileR.uyari != null && !hileUyarildi.Contains("|" + hileR.id + "|")) { hileUyarildi += "|" + hileR.id + "|"; Say("op", "aldatma", op.x, op.y, 1.5f, hileR.uyari, 3); }
            }
        }

        // ---- rakip ----
        // Tur 3+ kariyer ve Usta yolu rakibi; antrenman, görev ve serbest maç hiç. Patronun imzası olan topta denemez.
        public bool RakipHileAcik { get { return stil != null && ogr == null && (stil.usta >= 0 || stil.kariyer >= Aldatma.Rakip.kariyerMin); } }
        void RakipHilePlanla(float t0)
        {
            var I = icpt; if (I == null || I.leave || I.imza != null || mermiIkinci || s.serve || !RakipHileAcik) return;   // mermi drive'ın ikincisi sırasını korur
            var R = Aldatma.Rakip; if (hileRSay >= R.azami || sc[0] + sc[1] - hileRSon < R.ara) return;
            float hz = R.hazirlik + (easy ? .2f : 0); if (I.t - t0 < hz + .1f) return;
            string el = HandOf(op, "op", I.x, I.z); var aday = new List<HileTanim>();
            foreach (var t in Aldatma.Liste)
            {
                if (!t.rakipte) continue;
                if (t.baglam == "file") { float[] f, z; if (I.overhead || !t.kosul.TryGetValue("file", out f) || !t.kosul.TryGetValue("z", out z) || Court.NET - I.y > f[3] || I.z < z[0] || I.z > z[3]) continue; }
                else if (t.baglam == "ust") { if (!I.overhead || el != "fore") continue; }
                else continue;
                aday.Add(t);
            }
            if (aday.Count == 0) return;
            float sk; float p = R.p * (R.stil.TryGetValue(stil.id ?? "", out sk) ? sk : 1);
            if (R01() >= p) return;
            hileR = aday[rng.Next(aday.Count)]; hileI = I; hileBas = I.t - hz - .05f; hileBasladi = false;
            I.opType = hileR.vTip == "lift" ? "clear" : "drop";   // hazırlık pozu buna bakar (drop duruşu; flick de drop duruşundan)
        }

        // OpHit başında: hazırlığı görünmüş aldatma. Kolay'da kendiliğinden koşu bir an yanlış yöne (Gölge flick ile aynı dil);
        // Usta'da koşu oyuncunun, aldatma yalnız vuruşun kendisinde.
        bool RakipHileVurus(Intercept I)
        {
            if (I != hileI || !hileBasladi || hileR == null) return false;
            var t = hileR; hileI = null; hileR = null; op.imzaT = 0; op.imzaId = null;
            float hx = s.x, hy = s.y, hz = Math.Max(.2f, s.z), vx, vy, vz, tx; string type = "drop"; bool sl = false; float fx = me.x, fy = me.y, sure = t.adim;
            switch (t.id)
            {
                case "tutflick":
                    tx = me.x < Court.MID ? Court.SR - .6f : Court.SL + .6f; type = "clear";
                    AlcakYay(hx, hy, hz, tx, Court.L - .7f, .5f, out vx, out vy, out vz); fy = Math.Max(Court.NET + 1.2f, me.y - 1.2f); break;
                case "sahtesmac":
                    tx = Court.MID + (R01() - .5f) * 2.4f; Shuttle.Aim(hx, hy, hz, tx, Court.NET + 1.0f, true, DROP_A, out vx, out vy, out vz);
                    if (!FileGecer(hx, hy, hz, vx, vy, vz)) AlcakYay(hx, hy, hz, tx, Court.NET + 1.0f, .3f, out vx, out vy, out vz);
                    fy = Math.Min(Court.L - .8f, me.y + 1.2f); break;
                case "ciftaksiyon":
                    tx = me.x < Court.MID ? Court.SR - .6f : Court.SL + .6f; AlcakYay(hx, hy, hz, tx, Court.NET + .6f, .55f, out vx, out vy, out vz);
                    fx = Clamp(2 * Court.MID - tx, Court.SL, Court.SR); break;
                default:   // çevirme: bandı sıyıran takla atan top
                    tx = Clamp(hx + (R01() - .5f) * 1.2f, Court.SL + .3f, Court.SR - .3f); AlcakYay(hx, hy, hz, tx, Court.NET + .5f, .5f, out vx, out vy, out vz, SliceKd("op", true)); sl = true; break;   // nişan uçuşun kendi sürüklenmesiyle (Imza.AlcakYay)
            }
            if (easy) { me.fakeUntil = clock + sure; me.fakeX = fx; me.fakeY = fy; }
            StartFlight("op", vx, vy, vz, hx, hy, hz, false, type, false, "good", sl); hileTop = s; return true;
        }
    }
}
