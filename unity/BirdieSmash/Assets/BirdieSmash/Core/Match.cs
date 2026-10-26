// Maç: kurallar, rakip yapay zekâsı, vuruşlar, servis, sayı, nefes, antrenman. HTML sürümünün (index.html: newMatch …
// land, ayakIsi, player) C# karşılığı. 2026-10-24: main fb1b245'e kadar taşındı (rakip kimliği, iki oyuncunun nefesi,
// üç servis, 0,7 sn sayı arası, ilk açılış antrenmanı, kariyer, vuruş akışı zamanlaması). 2026-10-24 ikinci tur:
// main 46943a8 (raket düşüşü, drop/file klibi, overhead 160 ms ileri salınım), ekipman çarpanları (GS, Gear.cs),
// maç görevleri (Gorev.cs), antrenmanın bilgi adımları ve antrenör çağrısı (Ogr.cs). Görünüm katmanı yalnız Events kuyruğunu okur ve Athlete
// alanlarından poz seçer; burada UnityEngine yok ki bot maçıyla Unity'siz test edilebilsin.
using System;
using System.Collections.Generic;

namespace BirdieSmash.Core
{
    public class SvAnim { public string clip; public float t, d; }

    public class Athlete
    {
        public float x, y, vx, vy, px = float.NaN, py;
        public float tx = float.NaN, ty;
        public float sw, swHiz = 4, dive, jump, hop, stun, prepA, splitT, manualT, prepT = float.NaN, nefes = 1;
        public bool yat;
        public string clip = "idle", prepClip, shot = "low", hand = "fore", prepShot, prepHand, svClip, svTur;
        public SvAnim svAnim;
        // ayak işi (HTML ayakIsi): yumuşatılmış hız, adım fazı, seçili ayak klibi ve aday klibin süresi
        public float qx = float.NaN, qy, svx, svy, runPh, adayT, idlePh; public bool yuru; public string ayakKlip, adayAd;
        public float fakeUntil = -9, fakeX;   // aldatılan rakip: bu ana dek ters yöne çıkar
        public float mvx, mvy;   // rakibin koşu hızı (MoveAcc); ben vx/vy kullanırım (RunMe)
        // İmza vuruşu hazırlığı (Core/Imza, 2026-10-26): imzaT > 0 iken görünüm pozu tutar ve ışığı yakar; imzaId hangisi.
        public float imzaT; public string imzaId;
        public float fakeY;   // aldatılan oyuncu (Vex, gölge flick): Kolay'da kendiliğinden koşu bu ana dek yanlış yöne
        public bool HasTarget { get { return !float.IsNaN(tx); } }
    }

    public class Intercept
    {
        public float t, x, y, z, side = float.NaN;
        public string who, opType;
        public bool overhead, kill, lunge, leave;
        public string imza; public float imzaBas; public bool imzaBasladi;   // rakibin imza vuruşu: hazırlığın başlayacağı uçuş anı
    }

    public struct Reach { public bool lunge, over, kill; public float d; }

    // Kaydırma: piksel değerleri 640 px yükseklikli referans ekrana ölçeklenmiş (HTML ile aynı eşikler).
    public struct SwipeInput { public float dx, dy, speed, dist, R, side; public bool tap, curved; }

    public struct ShotPlanR { public string type; public float tx, d, val, hx, hy, hz, sp; public bool byAngle, bh, over; }

    // Imza: rakip imza vuruşunun hazırlığına başladı (type = imza kimliği, k = hazırlık süresi); Okudun: oyuncu imzayı cevapladı.
    public enum Ev { Hit, Net, Tape, Cord, Floor, Point, Text, Split, MatchEnd, Serve, Hawk, Flow, Gorev, Aldat, Perfect, Kurtaris, Imza, Okudun }

    public class GameEvent
    {
        public Ev kind; public string who, type, quality, text; public float x, y, z, k; public bool big, slice, serve; public int onem = 1;
    }

    // Rakip kimliği (HTML STIL): global kısa ad, tarz, forma rengi; forma oyuncununkine yakınsa yedek renk.
    // 2026-10-26 (tasarım Ek A.2): değerler veri/stil.json'dan (KariyerVeri). aldatP: rakibin aldatıcı vuruş olasılığı (Vex),
    // uyum: oyuncunun son 8 vuruşuna göre yer alma (Aria, en çok .3), imza: patron olduğunda imza vuruşu (Core/Imza).
    // kariyer: genel basamak indeksi (tur·6 + basamak; Tur 1'de eski indeks), -1 serbest maç.
    public class Style
    {
        public string id, isim, renk, yedek, ad, ipucu, imza; public string[] ustK, altK; public float[] ustP, altP;
        public float hiz, hata, ev, yorK = 1, geriK = 1, aldatP, uyum; public int kariyer = -1;
        // 2026-10-26: imza yalnız patron bayrağıyla ateşlenir. stil.json'daki taban tarz imzayı taşır (harita/maç kartı adını
        // gösterir), serbest maç o taban nesneyi kullandığı için imza kimliğine bakmak yetmez; bayrağı yalnız KariyerVeri.Rakip koyar.
        public bool patron;
        public Style Clone() { return (Style)MemberwiseClone(); }
    }

    // Kariyer basamağı (veri/kariyer.json): uzunluk 0 = oyuncunun seçtiği maç uzunluğu; gorev = 2. yıldızın sabit görevi.
    public class Kariyer { public string stil, yer, saha, gorev; public float g; public int tur, basamak, uzunluk; public bool patron; }

    public class ServeBox { public string who; public float x0, x1, y0, y1; }

    public class MatchOptions
    {
        public bool easy = true, reduce, ilk; public int target = 21; public Style stil; public string saha = "salon";
        public int bestRally; public bool ogr, ogrKisa; public string formaRenk = "#4fb8ff";
        public GearStats gs;   // ekipman çarpanları (Gear.Hesap); null = nötr (bot testleri, eski imza)
    }

    public partial class Match
    {
        public readonly Athlete me = new Athlete(), op = new Athlete();
        public readonly int[] sc = new int[2];
        public readonly NetWave net = new NetWave();
        public readonly List<GameEvent> events = new List<GameEvent>();
        public GearStats GS = GearStats.Notr;
        public Shuttle s; public Intercept icpt; public ServeBox serveBox; public Style stil;
        public string server = "me", serveMsg = "", saha = "salon";
        public int rally, maxRally, pstreak, target = 21, bestRally;
        public float energy, clock, opHitAt = -9, wait, betweenT, freeze, slow = 1, slowT;
        public bool easy = true, reduce, ilk, between, split, splitPending, over, matchDone, hint, deceive, kisaBekle, flow;
        public Action svPending;
        // maç sonu istatistiği (HTML oInfo)
        public int smashes, kills, perfects, deceives, splits, tepeden, saves, outsLeft, slices, smacSayi, outsBen, smacKarsila, dropSayi;   // smacKarsila: karşılanan rakip smacı; dropSayi: drop ile alınan sayı (kariyer görevleri)   // outsBen: dışarı giden kendi vuruşum (servis hariç), maç sonu ipucu
        bool nefesUyari, nefesIlk, opUyari, rekorBuMac;
        // hakemler (görünüm okur): baş hakemin el hareketi süresi, kime, servis el değişti mi; servis hakeminin yönü
        public float hakemT, hakemT0 = 1.4f, sjT; public string hakemYon, sjYon; public bool hakemSO;
        readonly Random rng;

        // Klip içinde temas anı (karakter.py anahtar zamanları); HTML CLIP_HIT ile aynı.
        public static readonly Dictionary<string, float> CLIP_HIT = new Dictionary<string, float> {
            // 46943a8 (raket düşüşü, drop/file klibi): CLIP_HIT = (hit+0.05)/n, overhead 14 kare.
            {"smash",.6464f},{"clear",.6464f},{"drive",.5062f},{"kill",.4357f},{"low",.5062f},{"lunge",.5062f},{"serve",.5025f},{"serve_gec",.5525f},{"serve_bh",.505f},{"serve_flick",.505f},{"block",.5083f},
            {"bh_over",.505f},{"bh_drive",.5062f},{"bh_kill",.4357f},{"bh_low",.5062f},{"bh_lunge",.5062f},{"jump",.6464f},{"drop",.6464f},{"net",.5062f}};
        // Klip başına poz (kare) sayısı: hazırlıkta "geri salınımın tepesinde bekle" kuralı kare indeksleriyle yazıldı.
        // Görünüm FBX'in <model>_clips.json'undan günceller; varsayılanlar karakter.py CLIPS ile aynı.
        public static readonly Dictionary<string, int> CLIP_N = new Dictionary<string, int> {
            {"idle",8},{"run",8},{"smash",14},{"clear",14},{"drop",14},{"net",8},{"drive",8},{"kill",7},{"low",8},{"lunge",8},{"serve",20},{"serve_gec",20},{"serve_bh",10},{"serve_flick",10},{"block",6},
            {"bh_over",10},{"bh_drive",8},{"bh_kill",7},{"bh_low",8},{"bh_lunge",8},{"split",5},{"chasse_r",6},{"chasse_l",6},{"fwd",6},{"back_r",6},{"back_l",6},{"jump",14},{"yorgun",10}};
        // Raket HEP sağ elde (yasin 2026-10-19): backhand aynalama değil, kendi klibi.
        public static readonly Dictionary<string, string> BH = new Dictionary<string, string> {
            {"smash","bh_over"},{"clear","bh_over"},{"drop","bh_over"},{"net","bh_low"},{"drive","bh_drive"},{"kill","bh_kill"},{"low","bh_low"},{"lunge","bh_lunge"},{"block","bh_drive"},{"jump","bh_over"}};
        // Hazırlık temasa kalan süreyle oynar (HTML ce47f5c, animasyon denetimi 24 Eki): figür geri salınımın tepesinde
        // (PREP_KARE: karakter.py'deki *_PREP anahtarına en yakın kare) bekler, ileri salınım son SALINIM saniyesinde
        // oynar, temas karesi ancak raket topa değdiğinde gelir. Sıçrayarak smaçta salınım kalkıştan başlar.
        public static readonly Dictionary<string, int> PREP_KARE = new Dictionary<string, int> {
            // 46943a8: tüm kliplerde kupa/geri salınım tepesi 2. kare (raket düşüşü sonrası).
            {"smash",2},{"clear",2},{"jump",2},{"bh_over",2},{"drop",2},{"net",2},{"drive",2},{"bh_drive",2},{"kill",2},{"bh_kill",2},{"low",2},{"bh_low",2},{"lunge",2},{"bh_lunge",2},{"block",2}};
        // render turu 24 Eki: overhead'de kupa pozu ile temas arası 7 kare (raket düşüşü); 100 ms'de kareler atlanıyordu,
        // ileri salınım 160 ms (Rambely vd.: kupa→temas ~150–200 ms). Backhand overhead 130 ms.
        public static readonly Dictionary<string, float> SALINIM = new Dictionary<string, float> {
            {"jump",.28f},{"smash",.16f},{"clear",.16f},{"drop",.16f},{"bh_over",.13f}};
        public const float SALINIM_JUMP = .28f, SALINIM_V = .1f;
        public static bool HasClip(string c) { return CLIP_N.ContainsKey(c); }
        public const float JUMP_HIZ = 1f / .53f;   // sıçrama 0,53 sn: kalkıştan 0,28 sn sonra tepe
        // Ayak işi adım boyu (fb1b245): m / döngü; chassé ~0,7 m taşır, eskiden hepsi 1,5 m'de bir dönüp ayak kayıyordu.
        public static readonly Dictionary<string, float> ADIM_BOY = new Dictionary<string, float> { {"chasse_r",.7f},{"chasse_l",.7f},{"fwd",.9f},{"back_r",.8f},{"back_l",.8f} };

        // Tarzlar ve kariyer veriden (2026-10-26, KariyerVeri; değerler eski koddakiyle birebir). Hızlı maçın rastgele rakibi
        // bugünkü üç tarzdan kalır (Hızlı maç metni öyle söylüyor); yeni tarzlar kariyerde tanıtılır.
        public static Dictionary<string, Style> STIL { get { return KariyerVeri.Stil; } }
        public static readonly string[] STIL_SIRA = { "smasci", "duvar", "file" };
        // Tur 1 basamakları (mahalle/okul = ahşap, il ve bölge = salon, ulusal ve kupa = turnuva): eski KARIYER dizisi.
        // tuytop_v2.kariyer, başarımlar ve HTML devri bu altı basamağı sayar.
        public static Kariyer[] KARIYER { get { return KariyerVeri.Turlar[0].b; } }
        public static Style KariyerRakip(int g) { return KariyerVeri.Rakip(g); }

        public Match(int seed, MatchOptions o)
        {
            rng = new Random(seed); easy = o.easy; net.sakin = o.reduce; target = o.ilk ? 5 : o.target; reduce = o.reduce; ilk = o.ilk; saha = o.saha; bestRally = o.bestRally;
            stil = o.stil ?? STIL[STIL_SIRA[rng.Next(3)]]; GS = o.gs ?? GearStats.Notr;
            if (!o.ogr) gorevler = GorevSec();
            me.x = Court.MID; me.y = Court.L - 2.2f; op.x = Court.MID; op.y = 2.2f;
            if (o.ogr) OgrKur(o.ogrKisa);
            SetupServe();
        }
        // Eski imza (bot testleri): rastgele rakip, serbest maç.
        public Match(int seed, bool easyMode, int targetPoints) : this(seed, new MatchOptions { easy = easyMode, target = targetPoints }) { }

        float R01() { return (float)rng.NextDouble(); }
        static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
        static float Hyp(float a, float b) { return (float)Math.Sqrt(a * a + b * b); }
        // Clear/lift kalkış açıları (2026-10-20, HTML ile aynı): ölçülen elit clear çıkışı 42–51 m/s (arastirma/vurus_biyomekanigi.md §2).
        public const float CLEAR_A = .63f, LIFT_A = .80f;
        // Baş üstü drop açısı .12 → .20 rad (2026-10-26, FizikOlc): 7° ile arka sahadan fileden 1,2 m'ye düşen yörünge yoktu,
        // çözüm bandın 1 cm altına yapışıyordu (rakip drop'larının %35'i bandı sıyırdı, %10'u fileye). 11,5° ile ~14 m/s,
        // bant üstü ~15 cm, uçuş ~1,1 s, top fileden sonra dik düşer (gerçek drop: smaç gibi başlar, fileyi az farkla geçer).
        public const float DROP_A = .20f;
        public static string ClipHand(string c, string hand) { string b; return hand == "back" && BH.TryGetValue(c, out b) ? b : c; }
        // Yazı bütçesi (yasin 2026-10-20): önem 3 her zaman; 2 en az 0,8 s arayla; 1 son yazıdan 1,8 s sonra.
        // Süre gerçek zamanla (realClock) ölçülür, yavaşlatmadan etkilenmez.
        public float realClock; float txtSon = -9, txtBuyukSon = -9;
        public void Say(string who, string type, float x, float y, float z, string text, int onem = 1)
        {
            if (onem < 3) { if (onem == 2 && realClock - txtBuyukSon < .8f) return; if (onem == 1 && realClock - txtSon < 1.8f) return; }
            txtSon = realClock; if (onem >= 2) txtBuyukSon = realClock;
            events.Add(new GameEvent { kind = Ev.Text, who = who, type = type, x = x, y = y, z = z, k = onem, text = text, onem = onem });
        }
        void Emit(Ev k, string who = null, string type = null, float x = 0, float y = 0, float z = 0, float kk = 0, string text = null, string q = null, bool big = false)
        { events.Add(new GameEvent { kind = k, who = who, type = type, x = x, y = y, z = z, k = kk, text = text, quality = q, big = big }); }

        // Forma rengi oyuncununkine yakınsa (RGB uzaklığı < 110) yedek renk: kortta iki oyuncu hiç karışmaz.
        public static string OpRenk(Style st, string forma)
        {
            if (st == null || st.renk == null) return "#ff7a6b";
            int[] a = Rgb(st.renk), b = Rgb(forma); double d = Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
            return d < 110 ? st.yedek : st.renk;
        }
        static int[] Rgb(string h) { h = h.TrimStart('#'); return new[] { Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16) }; }
        public string RakipAd(bool uzun) { return stil.isim != null ? (uzun ? stil.isim + " (" + stil.ad + ")" : stil.isim) : stil.ad; }

        // ---- servis yeri: sunucunun skoru çiftse sağ, tekse sol kare ----
        string ServeSide(string who) { return sc[who == "me" ? 0 : 1] % 2 == 0 ? "R" : "L"; }
        static float[] BoxX(string who, string side)
        {
            float[] right = who == "me" ? new[] { Court.MID, Court.SR } : new[] { Court.SL, Court.MID };
            float[] left = who == "me" ? new[] { Court.SL, Court.MID } : new[] { Court.MID, Court.SR };
            return side == "R" ? right : left;
        }
        void SetupServe()
        {
            if (ogr != null) { var a = OgrA(); if (a != null && a.bilgi == null) server = a.servis != null ? "me" : "op"; if (a != null && a.drill) OgrCagriSec(true); }   // antrenman: servis adımında sen, vuruş adımında antrenör yüksek servisle başlatır
            s = null; me.vx = me.vy = 0; op.mvx = op.mvy = 0; me.tx = float.NaN; me.stun = 0; rally = 0; between = false; icpt = null; serveMsg = ""; ImzaSifirla(); me.fakeUntil = -9;
            string sv = server, side = ServeSide(sv), rv = sv == "me" ? "op" : "me";
            float[] sb = BoxX(sv, side), rb = BoxX(rv, side);
            if (sv == "me") { me.x = (sb[0] + sb[1]) / 2; me.y = Court.NET + Court.SSL + 1.4f; op.x = (rb[0] + rb[1]) / 2; op.y = Court.NET - Court.SSL - 1.6f; }
            else { op.x = (sb[0] + sb[1]) / 2; op.y = Court.NET - Court.SSL - 1.4f; me.x = (rb[0] + rb[1]) / 2; me.y = Court.NET + Court.SSL + 1.6f; }
            serveBox = new ServeBox { who = rv, x0 = rb[0], x1 = rb[1], y0 = rv == "me" ? Court.NET + Court.SSL : 0, y1 = rv == "me" ? Court.L : Court.NET - Court.SSL };
            wait = sv == "op" ? (kisaBekle ? .7f : 1.0f) : 0; hint = sv == "me"; svPending = null; me.svAnim = op.svAnim = null;
            // Rakip servis türünü önceden seçer, duruşu ona göre görünür; flick kısa servisle aynı duruştan.
            float r = R01(); op.svTur = r < .58f ? "kisa" : r < .8f ? "flick" : "yuksek";
            if (ogr != null) op.svTur = "yuksek";
            op.svClip = SERVIS[op.svTur].klip; me.svClip = "serve_bh";
            if (sc[0] + sc[1] == 0 && ogr == null) serveMsg = "Rakip: " + RakipAd(true);
            Emit(Ev.Serve, sv);
        }

        // Çizgi kuralı (BWF Law 1.1 + 13): çizgiler 40 mm ve sınırladıkları alana dahil; mantar (Ø ~26 mm) çizgiye değdiyse İÇERİ.
        public const float LW = .04f, RC = .013f;
        void Sinir(bool serve, out float x0, out float x1, out float y0, out float y1)
        {
            if (serve) { var b = serveBox; x0 = b.x0 == Court.MID ? Court.MID - LW / 2 : b.x0; x1 = b.x1 == Court.MID ? Court.MID + LW / 2 : b.x1; y0 = b.y0; y1 = b.y1; }
            else { x0 = Court.SL; x1 = Court.SR; y0 = 0; y1 = Court.L; }
        }
        // işaretli pay: >0 içeride, <0 dışarıda; mantarın kenarı ile alanın dış kenarı arasındaki en kısa mesafe (m)
        public float SinirPayi(float x, float y, bool serve)
        {
            float x0, x1, y0, y1; Sinir(serve, out x0, out x1, out y0, out y1);
            return Math.Min(Math.Min(x - (x0 - RC), (x1 + RC) - x), Math.Min(y - (y0 - RC), (y1 + RC) - y));
        }
        public bool IsOut(float x, float y, bool serve) { return SinirPayi(x, y, serve) < 0; }
        float LineMargin(float x, float y, bool serve) { float x0, x1, y0, y1; Sinir(serve, out x0, out x1, out y0, out y1); return Math.Min(Math.Min(Math.Abs(x - x0), Math.Abs(x - x1)), Math.Min(Math.Abs(y - y0), Math.Abs(y - y1))); }

        int Behind() { return Math.Max(0, sc[1] - sc[0]); }
        // Nefes (kondisyon): smaç, sıçrama ve uzanma nefes yakar; nefes .4 altında koşu yavaşlar, smaç zayıflar.
        public float NefesF() { return me.nefes < .4f ? .8f + .5f * me.nefes : 1; }
        void Yor(float v)
        {
            me.nefes = Math.Max(0, me.nefes - v * GS.yorK);
            if (me.nefes < (ilk && !nefesIlk ? .6f : .25f) && !nefesUyari) { nefesUyari = true; nefesIlk = true; Say("me", null, me.x, me.y, 0, "Nefes al: clear at", 2); }
            if (me.nefes > .5f) nefesUyari = false;
        }
        public float MeSpd() { return 5.2f * GS.kosu * NefesF() * (split ? 1.3f : 1); }
        // Rakibin nefesi (HTML bdf1ed5): aynı eğri; yorgun rakip yavaş koşar, isabeti bozulur, smaç yerine clear seçer.
        // Tarzlar farklı yorulur: Smaşçı ×1,3, Duvar ×0,7, File oyuncusu arkaya koşunca ×1,8.
        public float OpNefesF() { return op.nefes < .4f ? .8f + .5f * op.nefes : 1; }
        void OpYor(float v)
        {
            if (ogr != null) return; op.nefes = Math.Max(0, op.nefes - v * stil.yorK);
            if (op.nefes < .3f && !opUyari) { opUyari = true; Say("op", null, op.x, op.y, 0, "Rakip yoruldu!", 2); }
        }
        // HTML'de koşu maliyeti (metre başına .045) yazılmış ama ölçüm noktası (px) aynı karede iki kez güncellendiği için
        // hiç uygulanmıyor: rakip yalnız smaçla yoruluyor. Demo HTML'in bugünkü davranışını taşır; düzeltme kararı yasin'de.
        public static bool OP_KOSU_YORAR = false;
        float opRunX = float.NaN, opRunY;
        public float OpSpd() { return (GS.rakip * stil.hiz * (easy ? 3.2f : 3.6f) + Math.Min(.55f, (sc[0] + sc[1]) * .02f)) * OpNefesF() - Math.Min(.8f, Behind() * .12f); }

        static string HandOf(Athlete o, string who, float sx, float sz) { float side = (sx - o.x) * (who == "me" ? 1 : -1); return side < -(sz > 1.95f ? .6f : .25f) ? "back" : "fore"; }
        // HUD (hale etiketi, servis şeridi, Hawk-Eye kırpması) iç kuralları kopyalamasın diye salt-okur sarmalayıcılar.
        public static string HandOfPublic(Athlete o, string who, float sx, float sz) { return HandOf(o, who, sx, sz); }
        public void SinirPublic(bool serve, out float x0, out float x1, out float y0, out float y1) { Sinir(serve, out x0, out x1, out y0, out y1); }
        public string ServeSideOf(string who) { return ServeSide(who); }
        // Göğüs hizasındaki top düz salınımla vurulur (HTML ce47f5c): alçak vuruş klibi raketi yere indirip temasta fırlatıyordu.
        static string ShotClass(string type, float z, float y)
        {
            if (type == "smash" || z > 1.95f) return "over"; if (type == "drive" && z > .8f) return "drive";
            if (Math.Abs(y - Court.NET) < 2.3f && z > Court.NETH) return "kill"; if (z > .8f && type != "block" && type != "lift") return "drive"; return "low";
        }
        // Pozisyon: forehand tarafı önde; forehand noktasına yetişemiyorsa backhand noktası. Karar bir kez verilir.
        float HitSide(Athlete o, string who, Intercept I)
        {
            if (!float.IsNaN(I.side)) return I.side;
            float m = who == "me" ? 1 : -1, fh = (I.kill || I.lunge ? .3f : .45f) * m, bh = -.3f * m;
            float spd = who == "me" ? 5.2f * GS.kosu : OpSpd(), tt = Math.Max(0, I.t - s.t) * .9f;
            float dF = Hyp(I.x - fh - o.x, I.y - o.y), dB = Hyp(I.x - bh - o.x, I.y - o.y);
            I.side = (dF > spd * tt && dB < dF - .3f) ? bh : fh; return I.side;
        }

        void StartFlight(string from, float vx, float vy, float vz, float hx, float hy, float hz, bool isServe, string type, bool isSuper, string q, bool slice = false)
        {
            s = new Shuttle { x = hx, y = hy, z = hz, vx = vx, vy = vy, vz = vz, from = from, serve = isServe, type = type, super = isSuper, kd = slice ? Shuttle.KD * 1.35f : Shuttle.KD, slice = slice };
            rally++; maxRally = Math.Max(maxRally, rally);
            if (from == "op") { opHitAt = clock; split = false; if (splitPending) { splitPending = false; DoSplit(); } } else { split = false; splitPending = false; }
            var h = from == "me" ? me : op;
            h.sw = 1; h.shot = isServe ? "low" : (h.prepShot == "lunge" ? "lunge" : ShotClass(type, hz, hy));
            {   // sınır bölgesinde gösterilen hazırlık kazanır: poz temasta sıçramasın (file önünde smaç = kısa öldürme salınımı)
                string ps = h.prepShot, c0 = h.shot;
                if (!isServe && h.prepA > .5f && (ps == "low" && c0 == "drive" && hz < 1.3f || ps == "drive" && c0 == "low" && hz > .6f || ps == "kill" && (c0 == "over" && hz <= 1.95f || c0 == "drive" && hz > 1))) h.shot = ps;
            }
            h.hand = isServe ? "fore" : HandOf(h, from, hx, hz);
            string c = isServe ? (h.svClip ?? "serve") : h.shot == "lunge" ? "lunge" : h.shot == "over" ? (type == "jump" || from == "op" && h.jump > 0 ? "jump" : type == "smash" ? "smash" : type == "drop" && HasClip("drop") ? "drop" : "clear") : h.shot == "drive" ? "drive" : h.shot == "kill" ? "kill" : type == "block" ? "block" : type == "drop" && HasClip("net") ? "net" : "low";
            h.clip = ClipHand(c, h.hand);
            // Takip vuruşa göre (fb1b245, denetim #5/#16): smaç/clear 300 ms, drop 200, drive 180, file öldürme 150,
            // net vuruşu 120, blok 100, lift 250; hamle toparlanması 300 ms. Eskiden hepsi 250 ms'ydi.
            h.swHiz = 1 / (isServe ? .25f : h.shot == "lunge" ? .3f : type == "block" ? .1f : h.shot == "over" ? (type == "drop" ? .2f : .3f) : h.shot == "drive" ? .18f : h.shot == "kill" ? .15f : type == "drop" ? .12f : .25f);
            var oth = from == "me" ? op : me; if (oth.sw <= 0) oth.splitT = .3f;   // rakip vururken split step
            if (type == "smash" && !reduce) freeze = Math.Max(freeze, isSuper ? .16f : .08f);
            Emit(Ev.Hit, from, type, hx, hy, hz, rally, null, q ?? "good", isSuper); events[events.Count - 1].slice = slice; events[events.Count - 1].serve = isServe;
            if (rally > 2 && rally % 10 == 0) Say(null, null, Court.MID, Court.NET, 2.5f, "Ralli " + rally, 2);
            PlanIntercept();
            if (from == "op" && !isServe) ImzaDuvar(type);
        }

        // Karşılayan için yolu önceden hesapla: baş üstünde alabiliyorsa yüksekte, değilse yere düşmeden en alçak noktada.
        void PlanIntercept()
        {
            var r = Shuttle.Simulate(s.x, s.y, s.z, s.vx, s.vy, s.vz, true, s.kd);
            string recv = s.from == "me" ? "op" : "me"; var P0 = recv == "me" ? me : op;
            float t0 = s.t; foreach (var p in r.path) p.t += t0; r.t += t0;
            s.landPred = r; if (r.net != null) s.netPred = r.net;
            float react = recv == "me" ? 0 : .18f, spd = recv == "me" ? MeSpd() : OpSpd(); bool blokla = false;
            if (recv == "op") react = ImzaTepki(react, out blokla);   // duvar blok: tepkisiz çıkış; okunan duvar: geç ve ters
            // Aldatma: kesilen/son anda bırakılan vuruşta rakip geç ve ters yöne çıkar (hold & flick).
            if (recv == "op" && deceive)
            {
                react += .3f; op.fakeUntil = clock + .32f; op.fakeX = Clamp(2 * Court.MID - r.x, Court.SL, Court.SR); deceive = false; deceives++;
                Say("op", null, op.x, op.y, 0, "Aldattın!", 2); Emit(Ev.Aldat, "op", null, op.x, op.y);
            }
            Func<PathPt, float, float> need = (p, reach) => Math.Max(0, Hyp(p.x - P0.x, p.y - P0.y) - reach) / spd + react + t0;
            var cands = new List<PathPt>();
            foreach (var p in r.path) if ((recv == "me" ? p.y > Court.NET : p.y < Court.NET) && p.z > .25f && p.z < 3.0f) cands.Add(p);
            PathPt pick = null; bool overhead = false, kill = false, lunge = false;
            if (recv == "me") foreach (var p in cands) if (Math.Abs(p.y - Court.NET) < 2.3f && p.z > Court.NETH + .2f && p.z < 2.6f && need(p, 1.15f) <= p.t) { pick = p; kill = true; break; }
            if (pick == null) foreach (var p in cands) if (p.z > 2.1f && p.z < 2.8f && need(p, 1.15f) <= p.t) { pick = p; overhead = true; break; }
            if (pick == null) for (int i = cands.Count - 1; i >= 0; i--) { var p = cands[i]; if (p.z > .75f) continue; if (need(p, 1.15f) <= p.t) { pick = p; break; } }
            if (pick == null) foreach (var p in cands) if (need(p, 1.15f) <= p.t) { pick = p; break; }
            if (pick == null && recv == "me") for (int i = cands.Count - 1; i >= 0; i--) { var p = cands[i]; if (p.z > 1.3f) continue; if (need(p, 2.1f) <= p.t) { pick = p; lunge = true; break; } }
            icpt = pick == null ? null : new Intercept { t = pick.t, x = pick.x, y = pick.y, z = pick.z, who = recv, overhead = overhead, kill = kill, lunge = lunge };
            if (recv == "op" && icpt != null)
            {
                bool outp = IsOut(r.x, r.y, s.serve); float margin = Math.Abs(SinirPayi(r.x, r.y, s.serve));
                if ((outp && !(margin < .25f && R01() < .5f)) || (r.net != null && !r.net.ok)) icpt.leave = true;
                float q = R01(), acc = 0; var K = overhead ? stil.ustK : stil.altK; var Pp = overhead ? stil.ustP : stil.altP; string pk = K[K.Length - 1];
                for (int i = 0; i < K.Length; i++) { acc += Pp[i]; if (q < acc) { pk = K[i]; break; } }
                icpt.opType = pk;
                if (blokla && !icpt.leave) { icpt.imza = "duvarblok"; icpt.imzaBasladi = true; icpt.opType = "drop"; }
                else ImzaPlanla(t0, cands, need);
            }
        }

        // Split step: rakip vururken küçük bir hop (fb1b245): 200 ms, sıçramanın ~%20'si; eskiden tek karede tam sıçrama.
        void DoSplit() { split = true; splits++; if (splits <= 2) Say("me", "split", me.x, me.y, .4f, "Split step"); me.hop = 1; Emit(Ev.Split, "me", null, me.x, me.y); if (s != null && s.from == "op") PlanIntercept(); }

        // Dokunma anı: rakip vurduktan hemen sonra (0,3 s) dokunmak split step = daha hızlı çıkış.
        public void OnTouchDown()
        {
            if (s == null || s.dead || split) return;
            if (s.from == "op" && clock - opHitAt < .3f) DoSplit();
            else if (s.from == "me" && icpt != null && icpt.who == "op" && icpt.t - s.t < .3f) splitPending = true;
        }

        // Erişim: raket kolu ~1,15 m (Kolay 1,45); uzanmayla daha geniş ama yalnız alçak topta.
        public bool ReachInfo(out Reach R)
        {
            R = new Reach();
            if (s == null || s.dead || s.from != "op" || s.y <= Court.NET || s.z < .12f || s.z > 3.1f) return false;
            float d = Hyp(s.x - me.x, s.y - me.y); bool kill = s.y - Court.NET < 2.3f && s.z > Court.NETH + .2f;
            float R1 = easy ? 1.45f : 1.15f, R2 = easy ? 2.2f : 1.75f;
            if (d <= R1) { R = new Reach { lunge = false, over = s.z > 2.0f, kill = kill, d = d }; return true; }
            if (d <= R2 && s.z < 1.5f) { R = new Reach { lunge = true, d = d }; return true; }
            return false;
        }
        public bool HitWindow() { Reach r; return ReachInfo(out r); }

        // Vuruş planı: kaydırmadan tür + hedef. Gerçek vuruş ve canlı önizleme (dry) aynı fonksiyonu kullanır.
        public ShotPlanR ShotPlan(SwipeInput sw, Reach I, bool dry)
        {
            float sp = sw.speed, up = -sw.dy, r = Clamp(sw.dx / Math.Max(50, Math.Abs(sw.dy)), -1.3f, 1.3f);
            var P = new ShotPlanR { hx = s.x, hy = s.y, hz = Math.Max(.2f, s.z), over = s.z > 2.0f, sp = sp };
            P.bh = HandOf(me, "me", s.x, s.z) == "back";
            string type;
            if (sw.tap) type = I.lunge ? "lift" : I.kill ? "kill" : "clear";
            else if (I.lunge) type = "lift"; else if (up < 0) type = "block";
            else if (sp > 1.5f && I.kill) type = "kill";
            else if (sp > 1.7f) type = P.over ? (P.bh ? "clear" : (energy >= 1 ? "jump" : "smash")) : P.hz < .8f ? "lift" : "drive";
            // Diz altından drive yok (2026-10-26): bant 1,52 m; 0,8 m altından 22 m/s düz vuruş ya fileye gider ya da fileyi geçmek
            // için dikleşip uzun kaçar (ölçüm: alçak drive'ların %17'si fileye). Gerçekte o yükseklikte sert vuruş hücum lift'idir.
            // Sınır ShotClass'ın drive eşiğiyle aynı (z > 0,8): klip ile uçuş aynı şeyi anlatır.
            else if (sp < .75f) type = "drop"; else type = "clear";
            float tx = Court.MID + r * 2.4f, d, val; bool byAngle = true;
            switch (type)
            {
                case "kill": d = 2.6f; byAngle = false; val = 30 * GS.hiz; if (!dry) Yor(.06f); break;
                case "jump": d = 3.2f; byAngle = false; val = 78 * GS.guc * NefesF(); if (!dry) { energy = 0; Yor(.3f); } break;
                case "smash": d = 4.2f; byAngle = false; val = 52 * GS.guc * NefesF(); if (!dry) Yor(.18f); break;
                case "drive": d = 4.6f; byAngle = false; val = (P.bh ? 18 : 22) * GS.hiz; break;
                case "drop": d = P.over ? 1.2f : 1.0f; val = P.over ? DROP_A : (Math.Abs(P.hy - Court.NET) < 2.5f ? .75f : .95f); break;
                case "block": d = .9f; val = .6f; tx = P.hx + r * 1.2f; break;
                case "lift": tx = Court.MID + r * 1.2f; d = 5.4f; val = LIFT_A + .08f; break;
                default: d = Math.Min(P.bh ? 5.6f : 6.9f, (P.bh ? 4.6f : 5.5f) + (sp - .75f) * 1.4f + (GS.guc - 1) * 3); val = P.over ? CLEAR_A : LIFT_A; break;   // kafa ağır raket clear'i uzatır
            }
            P.type = type; P.tx = tx; P.d = d; P.val = val; P.byAngle = byAngle; return P;
        }

        // Kesişten vuruş: yön = parmağın yönü, hız = güç ve tür, topun merkezine uzaklık = isabet.
        public string sonTip, sonQ; public bool sonSlice;   // son SwipeShot: tür (jump/kill/lift/block dahil), isabet, slice
        public bool SwipeShot(SwipeInput sw)
        {
            Reach I; if (between || s == null || !ReachInfo(out I)) return false;
            var PL = ShotPlan(sw, I, false); string type = PL.type; float d = PL.d, val = PL.val, hx = PL.hx, hy = PL.hy, hz = PL.hz; bool bh = PL.bh;
            float cq = sw.dist / sw.R;
            string q = cq < .4f * GS.tatli ? "perfect" : cq <= 1 ? "good" : easy ? "good" : "poor"; if (I.lunge && q == "perfect") q = "good"; if (sw.tap) q = "good";
            // Slice: kavisli kaydırma = raket topu keserek alır; top takla atar, kısa düşer, rakip aldanır.
            bool slice = sw.curved && q != "poor" && (type == "drop" || type == "clear" || type == "drive" || type == "block"); if (slice) { deceive = true; slices++; }
            float sig = ((q == "perfect" ? .1f : q == "good" ? .35f : .9f) + (I.lunge ? .4f : 0)) / GS.kontrol;
            Func<float> g = () => (R01() + R01() + R01() - 1.5f) * sig * 1.4f;
            float edge = q == "poor" ? sw.side * .8f * GS.kenar : 0;
            // Tepeden alma: smaç 2,4 m üstünde alınırsa dikleşir ve hızlanır.
            bool tepedenV = (type == "smash" || type == "jump") && s.z > 2.4f; if (tepedenV) { val *= 1.08f; d -= .5f; tepeden++; }
            float vx, vy, vz; Shuttle.Aim(hx, hy, hz, PL.tx + g() + edge, Court.NET - d + g() * .8f, PL.byAngle, val, out vx, out vy, out vz);
            if (q == "poor") { float k = 1 + (R01() - .5f) * .16f; vx *= k; vy *= k; vz *= k; }
            if (q == "perfect")
            {
                perfects++; pstreak++; Emit(Ev.Perfect, "me", type, hx, hy, hz, pstreak); energy = Math.Min(1, energy + .34f * GS.enerji); if (!reduce) freeze = Math.Max(freeze, .06f);
                if (pstreak == GS.akis && !flow) { flow = true; Say(null, "flow", Court.MID, Court.NET, 2, "AKIŞ", 3); Emit(Ev.Flow, "me"); }
                Say("me", type, hx, hy, hz, type == "jump" ? "SIÇRAYARAK SMAÇ" : type == "smash" ? "SMAÇ!" : type == "kill" ? "FİLE VURUŞU" : pstreak > 1 ? "Tam orta ×" + pstreak : (bh && (type == "clear" || type == "drive") ? "Backhand, tam orta" : "Tam orta"), (type == "jump" || type == "smash" || type == "kill" || pstreak >= 3) ? 2 : 1);
            }
            else { pstreak = 0; if (flow) { flow = false; Say(null, "flow", Court.MID, Court.NET, 2, "Akış bitti"); } if (q == "poor") Say("me", type, hx, hy, hz, "Sıyırdın"); }
            if (I.lunge) { me.dive = 1; me.yat = false; me.stun = .35f * GS.toparla; saves++; Yor(.15f); if (saves <= 3) Say("me", "lunge", hx, hy, hz, "Uzandın!"); }   // öğretici: maçta ilk 3 kez
            // Kaydırma anı temas anıdır, kalkış önceden bilinmez: figür temasta tepede (jump=.53 → sin(.53π) ≈ 1), sonra iner.
            if (type == "jump") me.jump = .53f;
            if (type == "kill") kills++;
            if (type == "smash" || type == "jump") smashes++;
            if (s.from == "op" && s.type == "smash") smacKarsila++;
            ImzaCevap(type); VurusKaydet(type);
            me.sw = 1; GorevKontrol();
            string ft = type == "jump" || type == "kill" ? "smash" : type == "lift" ? "clear" : type == "block" ? "drop" : type;
            sonTip = type; sonQ = q; sonSlice = slice;   // titreşim deseni (HTML hapHit) uçuş türünü değil vuruşun kendisini ister
            StartFlight("me", vx, vy, vz, hx, hy, hz, false, ft, type == "jump", q, slice);
            OgrOlay(type, null, slice, PL.over, sw.speed);
            return true;
        }

        // Servis (yasin 2026-10-21; araştırma: arastirma/reports/Badminton alçak vuruş ve servis.md). Üç servis, üç klip:
        // kısa (serve_bh, backhand, bandı sıyırır), flick (aynı hazırlık, geç bilek kırılması, dibe), yüksek (serve, yan
        // duruş, uzun salınım). Kural (BWF Law 9): temas 1,15 m altında. Servis klibi temas anına kadar oynar, top temas
        // karesinde çıkar. Hareket: yavaş = kısa, kısa ve sert fiske = flick, uzun ve hızlı = yüksek.
        public class Servis { public string klip; public float sure, z, aci; }
        public static readonly Dictionary<string, Servis> SERVIS = new Dictionary<string, Servis> {
            {"kisa", new Servis{klip="serve_bh",sure=.32f,z=1.0f,aci=.36f}}, {"flick", new Servis{klip="serve_flick",sure=.32f,z=1.02f,aci=.52f}}, {"yuksek", new Servis{klip="serve",sure=.56f,z=.82f,aci=1.0f}}};
        void ServisAt(string who, string tur, float tx, float ty)
        {
            var o = who == "me" ? me : op; var S = SERVIS[tur]; float dir = who == "me" ? -1 : 1, hx = o.x, hy = o.y + dir * .45f, hz = S.z;
            // Ben kısa servis duruşunda beklerim; uzun serviste duruştan forehand'e geçiş klibi oynar (+0,1 sn).
            bool gec = who == "me" && S.klip == "serve"; string klip = gec ? "serve_gec" : S.klip; float sure = S.sure + (gec ? .1f : 0);
            o.svClip = klip; o.svAnim = new SvAnim { clip = klip, t = 0, d = reduce ? sure * .6f : sure };
            svPending = () =>
            {
                float vx, vy, vz; Shuttle.Aim(hx, hy, hz, tx, ty, true, S.aci, out vx, out vy, out vz);
                if (tur == "flick" && (who == "op" || R01() < .75f)) deceive = who == "me";   // çoğu flick rakibi kısa servise hazırlıksız yakalar
                StartFlight(who, vx, vy, vz, hx, hy, hz, true, tur == "kisa" ? "drop" : "clear", false, "good");
                if (tur == "flick" && who == "op") Say("me", null, me.x, me.y, 0, "Flick servis!", 2);
            };
        }
        public static string ServisTuru(float dx, float dy, float speed) { return speed > 1.0f ? (Hyp(dx, dy) < 95 ? "flick" : "yuksek") : "kisa"; }
        public bool SwipeServe(float dx, float dy, float speed)
        {
            if (s != null || between || server != "me" || wait > 0 || over || svPending != null) return false; hint = false;
            float cx = (serveBox.x0 + serveBox.x1) / 2, r = Clamp(dx / Math.Max(50, Math.Abs(dy)), -1.3f, 1.3f);
            string tur = ServisTuru(dx, dy, speed); float d = tur == "kisa" ? Court.SSL + .45f : tur == "flick" ? 6.0f : 6.2f;
            ServisAt("me", tur, cx + r * 1.5f, Court.NET - d); OgrOlay(null, tur, false, false, speed); return true;
        }

        // Usta modu: dokunulan kort noktasına koş.
        public void RunTo(float x, float y, float manualFor)
        {
            me.tx = Clamp(x, -.4f, Court.CW + .4f); me.ty = Clamp(y, Court.NET + .3f, Court.L + .8f); me.manualT = manualFor;
        }

        void Plan(string type, float dxAim, bool overhead, out float tx, out float ty, out bool byAngle, out float val)
        {
            Func<float, float> yN = d => Court.NET + d; tx = Court.MID + dxAim * (Court.SR - Court.SL) * .62f; byAngle = true;
            switch (type)
            {
                case "smash": ty = yN(3.6f + R01() * 1.2f); byAngle = false; val = 52; break;
                case "drive": ty = yN(4.6f); byAngle = false; val = 22; break;
                case "drop": ty = yN(overhead ? 1.2f : 0.9f); val = overhead ? DROP_A : .75f; break;
                default: ty = yN(Court.L / 2 - .55f); val = overhead ? CLEAR_A : LIFT_A; break;
            }
        }

        void OpHit()
        {
            var I = icpt;
            if (ogr == null && I.imza != null && I.imzaBasladi && ImzaVurus(I)) return;   // patronun imza vuruşu (Core/Imza)
            if (ogr == null && MermiIkinci(I)) return;
            float hx = s.x, hy = s.y, hz = Math.Max(.2f, s.z); float r0 = R01();
            string q = R01() < .12f ? "poor" : R01() < .3f ? "perfect" : "good";
            bool obh = HandOf(op, "op", hx, hz) == "back";
            string type = I.opType ?? (I.overhead ? "clear" : "drop"); if (obh && type == "smash") type = "clear";
            if (type == "drive" && hz <= .8f) type = "clear";   // diz altından drive yok (ShotPlan ile aynı kural): alçak top kaldırılır
            { float n = op.nefes; if (type == "smash" && n < .4f && R01() < (.4f - n) / .4f * .8f) type = "clear"; if (type == "smash") OpYor(.12f); }   // yorgun rakip smaç yerine clear seçer
            float vx, vy, vz;
            if (ogr != null)
            {   // antrenör o adımın istediği topu besler
                var a = OgrA(); string b = a != null && a.drill ? (OgrCagriSec(false) == "drive" ? "drive" : "clear") : (a != null && a.besle != null) ? a.besle : "clear";
                if (b == "gevsek") { Shuttle.Aim(hx, hy, hz, Court.MID + (R01() - .5f) * 1.2f, Court.NET + 1.0f, true, 1.15f, out vx, out vy, out vz); StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drop", false, q); return; }
                type = b;
            }
            // Rakip file önünde arada gevşek top bırakır: sana öldürme şansı.
            if (type == "drop" && !I.overhead && hy > Court.NET - 2.6f && r0 < .35f)
            {
                Shuttle.Aim(hx, hy, hz, Court.MID + (R01() - .5f) * 2.4f, Court.NET + 1.0f, true, 1.15f, out vx, out vy, out vz);
                StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drop", false, q); return;
            }
            float tx, ty, val; bool byAngle; Plan(type, (R01() * 2 - 1) * .8f, I.overhead, out tx, out ty, out byAngle, out val);
            if (obh && type == "clear") ty = Math.Max(ty, Court.NET + 1.2f);   // backhand clear'ı kısa kalır
            // geride kalınca rakip daha çok hata yapar (lastik bant); yorgunken sapma büyür
            float hata = stil.hata * (.5f + (easy ? .3f : 0) + (obh ? .35f : 0) + Math.Min(.9f, Behind() * .15f)) * (1 + (1 - OpNefesF()) * 3);
            Func<float> g = () => (R01() - .5f) * hata;
            Shuttle.Aim(hx, hy, hz, tx + g(), ty + g(), byAngle, val, out vx, out vy, out vz);
            var chk = Shuttle.Simulate(hx, hy, hz, vx, vy, vz, false, Shuttle.KD);
            if (chk.net != null && !chk.net.ok && R01() < .9f) Shuttle.Aim(hx, hy, hz, tx, ty, true, Math.Max(.75f, byAngle ? val : .75f), out vx, out vy, out vz);
            // Kötü vuruş (2026-10-26): Aim artık fileyi hep geçtiği için rakibin file hataları nişan geometrisinden geliyordu
            // ve kayboldu (bot ölçümü: rakip file hatası 29 → 4 sayı). Oyuncuyla aynı kural: "poor" vuruşun hızı ±%8 sapar,
            // file ve dışarı hatası kötü vuruştan doğar.
            if (q == "poor") { float k = 1 + (R01() - .5f) * .16f; vx *= k; vy *= k; vz *= k; }
            // Aldatıcı (Vex, aldatP): son anda yön değiştirir. Kolay'da kendiliğinden koşu bir an ters yöne çıkar (drop'ta
            // arkaya, alçak clear'da öne); Usta'da koşu oyuncunun, aldatma yalnız vuruşun kendisinde (drop/flick karışımı).
            if (stil.aldatP > 0 && easy && (type == "drop" || type == "clear" && hz < 1.6f) && R01() < stil.aldatP)
            { me.fakeUntil = clock + .22f; me.fakeX = me.x; me.fakeY = type == "drop" ? Math.Min(Court.L - .8f, me.y + 1.2f) : Math.Max(Court.NET + 1.2f, me.y - 1.2f); }
            StartFlight("op", vx, vy, vz, hx, hy, hz, false, type, false, q);
        }

        void OpServe()
        {
            var b = serveBox; string tur = op.svTur ?? "kisa";
            float tx = b.x0 + .3f + R01() * (b.x1 - b.x0 - .6f), ty = tur == "kisa" ? b.y0 + .5f : b.y1 - .6f; ServisAt("op", tur, tx, ty);
        }

        // Ralli sistemi: sayıyı alan servisi alır. 21'de 2 fark, 30'da biter (11 → 15, 5 → 7).
        void Point(string winner, string why, float x, float y)
        {
            if (between) return; between = true;
            if (ogr != null && ogr.cagri != null && winner == "op" && s != null && s.from == "op" && OgrA() != null && OgrA().drill) { ogr.cagri = null; ogr.dene++; OgrCagriBitti(); }   // son adım: çağrılan topu kaçırmak da bir deneme
            hakemT = hakemT0 = 1.4f; hakemYon = winner; hakemSO = ogr == null && winner != server;   // servis el değişti mi: baş hakem o zaman yeni servisçinin tarafını gösterir
            if (ogr == null) { sc[winner == "me" ? 0 : 1]++; server = winner; }                      // antrenmanda skor yok
            if (winner == "me" && s != null && s.from == "me" && s.type == "smash") smacSayi++;
            if (winner == "me" && s != null && s.from == "me" && s.type == "drop") dropSayi++;
            ImzaSifirla();
            GorevKontrol();
            Emit(Ev.Point, winner, s != null ? s.type : null, x, y, 0, rally, why, null, s != null && s.super);
            bool rekor = rally > bestRally && rally >= 6;
            if (rally > bestRally) { bestRally = rally; if (rally >= 6) { rekorBuMac = true; Say(null, "rekor", Court.MID, Court.NET, 2, "Yeni ralli rekoru: " + rally, 3); } }
            int a = sc[0], b = sc[1], cap = target == 21 ? 30 : target == 11 ? 15 : 7;
            matchDone = ogr == null && ((Math.Max(a, b) >= target && Math.Abs(a - b) >= 2) || Math.Max(a, b) >= cap);
            if (matchDone) { if (winner == "me" && !reduce) { slow = .3f; slowT = .9f; } betweenT = 1.4f; }   // kaybı ağır çekimde izletmek cezalandırır
            else
            {
                if (Math.Max(a, b) >= target - 1 && a != b && Math.Abs(a - b) <= 1) serveMsg = a > b ? "Maç sayısı" : "Rakibin maç sayısı";
                else if (a == b && a >= target - 1) serveMsg = "Uzatma: 2 fark gerekiyor";
                // Sayılar arası bekleme 1,3 → 0,7 sn (HTML 959beb1): ritim düşmesin; antrenman ve rekor sayısı eski süreyi korur.
                bool kisa = ogr == null && !rekor; kisaBekle = kisa; if (kisa) hakemT = hakemT0 = 1.0f;
                betweenT = kisa ? .7f : 1.3f;
            }
        }
        public bool YeniRekor { get { return rekorBuMac; } }

        void Land()
        {
            s.dead = true; string hitter = s.from, other = hitter == "me" ? "op" : "me";
            if (!s.netHit)
            {
                Emit(Ev.Floor, hitter, s.type, s.x, s.y, 0, 0, null, null, s.type == "smash");
                // Çizgi kamerası: kıl payı topta karar santimle yazılır.
                if (LineMargin(s.x, s.y, s.serve) < .15f) { float pay = SinirPayi(s.x, s.y, s.serve); Emit(Ev.Hawk, hitter, pay >= 0 ? "in" : "out", s.x, s.y, 0, Math.Max(1, (float)Gear.JsRound(Math.Abs(pay) * 100))); }
            }
            if (s.netHit) { Point(other, hitter == "me" ? "Fileye takıldı" : "Rakip fileye taktı", s.x, s.y); return; }
            bool outp = IsOut(s.x, s.y, s.serve);
            if (ogr != null && s.ogrHedef != null && hitter == "me" && !outp && s.y >= s.ogrHedef[0] && s.y <= s.ogrHedef[1]) { ogr.yildiz++; Say("me", null, s.x, s.y, 0, "Hedefte! ★", 2); }
            if (outp) { if (other == "me") outsLeft++; else if (!s.serve) outsBen++; Point(other, s.serve ? (hitter == "me" ? "Servis hatası" : "Rakip servis hatası") : (hitter == "me" ? "Dışarı" : "Dışarı! Bıraktın, sayı senin"), s.x, s.y); }
            else { if (s.type == "smash" && hitter == "me") { op.dive = 1; op.yat = true; } Point(hitter, hitter == "me" ? (s.super ? "SIÇRAYARAK SMAÇ SAYISI" : s.type == "smash" ? "SMAÇ SAYISI" : "SAYI!") : "Kaçtı", s.x, s.y); }
        }

        // Oyuncu hareketi: hedefe ivmeyle koşar, yaklaşınca frenler. Kolay modda kesişme noktasına kendi koşar.
        void RunMe(float d)
        {
            var o = me; if (s == null) { o.vx = o.vy = 0; return; }   // servis atılana kadar yerinde (kural)
            if (o.stun > 0) { o.stun -= d; o.vx *= .8f; o.vy *= .8f; return; }
            o.manualT = Math.Max(0, o.manualT - d);
            if (easy && o.manualT <= 0 && o.fakeUntil > clock) { o.tx = o.fakeX; o.ty = o.fakeY; }   // aldatıldı (Vex, gölge flick)
            else if (easy && o.manualT <= 0)
            {
                var I = icpt;
                if (s.from == "op" && I != null && I.who == "me") { o.tx = Clamp(I.x - HitSide(o, "me", I), -.4f, Court.CW + .4f); o.ty = I.y + (I.lunge ? .6f : .25f); }
                else if (s.from == "op" && s.landPred != null && s.landPred.y > Court.NET) { o.tx = s.landPred.x; o.ty = s.landPred.y + .3f; }
                else if (s.from == "me") { o.tx = Court.MID; o.ty = Court.L - 3.3f; }
            }
            float max = MeSpd(), A = (split ? 20 : (easy ? 18 : 14)) * GS.kosu, dvx = 0, dvy = 0;
            if (o.HasTarget) { float dx = o.tx - o.x, dy = o.ty - o.y, dist = Hyp(dx, dy); if (dist > .02f) { float v = Math.Min(max, (float)Math.Sqrt(2 * A * dist)); dvx = dx / dist * v; dvy = dy / dist * v; } else o.tx = float.NaN; }
            float ex = dvx - o.vx, ey = dvy - o.vy, el = Hyp(ex, ey), st = A * d;
            if (el > st) { o.vx += ex / el * st; o.vy += ey / el * st; } else { o.vx = dvx; o.vy = dvy; }
            o.x = Clamp(o.x + o.vx * d, -.6f, Court.CW + .6f); o.y = Clamp(o.y + o.vy * d, Court.NET + .25f, Court.L + 1);
            Engel(o, true);
        }

        // Ağ teması adımı (2026-10-26): top ağı s.agSure boyunca yarım sinüsle iter (görünen sapma s.agMax'a kadar), ağ topu
        // tutar (yan hız 10/s söner, düşüş ağ sürtünmesiyle ~0,6 m/s'de kalır), yavaş top s.agAsili kadar asılı kalır; sonra
        // ağ topu vuranın tarafına hafifçe geri bırakır. Konum ağın şişkinliğinden okunur: top ağın içinden görünmez.
        void AgAdim(float h)
        {
            s.agT += h; float T = s.agSure, u = s.agT / T;
            float dsp = u < 1 ? s.agMax * (float)Math.Sin(Math.PI * u) : 0;
            s.vx *= (float)Math.Exp(-10 * h); s.vz += (-Shuttle.GR * .5f - 8 * s.vz) * h;
            s.x += s.vx * h; s.z = Math.Max(Court.NetLimit(s.x) - Court.AG_DERIN + .02f, s.z + s.vz * h);
            net.Zorla(s.x, s.z, -s.agYan * dsp, h);
            s.y = Court.NET + net.At(s.x, s.z) * NetWave.GORSEL;
            if (s.agT >= T + s.agAsili) { s.agda = false; s.y = Court.NET + s.agYan * Court.TOP_PAY; s.vy = s.agYan * (.1f + .6f * s.netK); s.vx *= .5f; }
        }

        // Katı engeller (2026-10-26, iç içe geçme yok): oyuncu gövdesi filenin kendi tarafında (merkez fileden ≥ 0,25 m; rakip
        // için de, eskiden yalnız ben sınırlıydım), direklerden ve hakem sandalyelerinden r + OYUNCU_R uzakta. Engele doğru hız
        // bileşeni silinir (itilip geri gelme titremesi olmaz).
        static void Engel(Athlete o, bool isMe)
        {
            if (isMe) o.y = Math.Max(o.y, Court.NET + .25f); else o.y = Math.Min(o.y, Court.NET - .25f);
            Itin(o, isMe, 0, Court.NET, Court.DIREK_R); Itin(o, isMe, Court.CW, Court.NET, Court.DIREK_R);
            Itin(o, isMe, Court.BH_X, Court.NET, Court.BH_R); Itin(o, isMe, Court.SH_X, Court.NET + Court.SH_DY, Court.SH_R);
        }
        static void Itin(Athlete o, bool isMe, float cx, float cy, float r)
        {
            float dx = o.x - cx, dy = o.y - cy, dd = Hyp(dx, dy), m = r + Court.OYUNCU_R; if (dd >= m) return;
            if (dd < 1e-4f) { dx = 0; dy = isMe ? 1 : -1; dd = 1; }
            float nx = dx / dd, ny = dy / dd; o.x = cx + nx * m; o.y = cy + ny * m;
            if (isMe) { float vn = o.vx * nx + o.vy * ny; if (vn < 0) { o.vx -= vn * nx; o.vy -= vn * ny; } }
            else { float vn = o.mvx * nx + o.mvy * ny; if (vn < 0) { o.mvx -= vn * nx; o.mvy -= vn * ny; } }
        }

        static void Move(Athlete o, float x, float y, float step) { float dx = x - o.x, dy = y - o.y, d = Hyp(dx, dy); if (d < 1e-3f) return; float k = Math.Min(1, step / d); o.x += dx * k; o.y += dy * k; }
        // Rakip ivmeyle koşar (2026-10-26, "hareketler badmintona uygun olmalı"): eski Move hızı tek karede 0'dan 3,5 m/s'ye
        // çıkarıp hedefte tek karede sıfırlıyordu (bot ölçümü: karelerin %3,2'sinde ivme > 40 m/s², p99 133 m/s²). Gövde ilk
        // karede tam hızla kayarken ayak işi hızı 120 ms'de yakaladığı için çıkışta ve duruşta ayak kayıyordu; yön değişimi de
        // anlıktı. Şimdi oyuncunun RunMe kuralı: en çok RAKIP_IVME ile hızlanır/yavaşlar, hedefe √(2·A·mesafe) ile frenler.
        // 18 m/s² Kolay moddaki oyuncuyla aynı. Ek süre ~v/A ≈ 0,19 s; PlanIntercept'in 0,18 s tepki payı bunu karşılar
        // (ölçüm: rakibin vuruşta topa uzaklığı değişmedi, bkz. rapor).
        public const float RAKIP_IVME = 18;
        static void MoveAcc(Athlete o, float x, float y, float vmax, float d)
        {
            float dx = x - o.x, dy = y - o.y, dist = Hyp(dx, dy), dvx = 0, dvy = 0, A = RAKIP_IVME;
            if (dist > .01f) { float v = Math.Min(vmax, (float)Math.Sqrt(2 * A * dist)); dvx = dx / dist * v; dvy = dy / dist * v; }
            float ex = dvx - o.mvx, ey = dvy - o.mvy, el = Hyp(ex, ey), st = A * d;
            if (el > st) { o.mvx += ex / el * st; o.mvy += ey / el * st; } else { o.mvx = dvx; o.mvy = dvy; }
            float sx = o.mvx * d, sy = o.mvy * d;
            if (dist <= .01f || Hyp(sx, sy) >= dist && sx * dx + sy * dy > 0) { o.x = x; o.y = y; if (dist <= .01f) { o.mvx = o.mvy = 0; } } else { o.x += sx; o.y += sy; }
            Engel(o, false);
        }

        // Ayak işi (yasin 2026-10-20 "pırpır yapmak yerine anatomiye uygun"): hız 120 ms yumuşatılır; faz mesafeyle,
        // adım boyu klibe göre, en çok 2,6 döngü/sn; yürüme 0,9 m/s'de başlar 0,45'te biter; yön klibi 180 ms aynı kalırsa değişir.
        public static void AyakIsi(Athlete q, float dt, bool isMe)
        {
            if (dt <= 0) return;
            if (float.IsNaN(q.qx)) { q.qx = q.x; q.qy = q.y; q.svx = 0; q.svy = 0; }
            float rx = (q.x - q.qx) / dt, ry = (q.y - q.qy) / dt, a = Math.Min(1, dt / .12f); q.qx = q.x; q.qy = q.y;
            if (Hyp(rx, ry) > 12) { rx = ry = 0; q.svx = q.svy = 0; }   // servis dizilişi oyuncuyu ışınlar: adım sayılmaz
            q.svx += (rx - q.svx) * a; q.svy += (ry - q.svy) * a; float sp = Hyp(q.svx, q.svy);
            q.yuru = q.yuru ? sp > .45f : sp > .9f;
            if (q.yuru)
            {
                float boy; if (q.ayakKlip == null || !ADIM_BOY.TryGetValue(q.ayakKlip, out boy)) boy = 1.5f;
                q.runPh = (q.runPh + Math.Min(sp / boy, 2.6f) * dt) % 1f;
                float fwdv = isMe ? -q.svy : q.svy, side = isMe ? q.svx : -q.svx;   // oyuncunun kendi bakışında: ileri = fileye
                string aday = Math.Abs(side) > Math.Abs(fwdv) * 1.15f ? (side > 0 ? "chasse_r" : "chasse_l") : fwdv > 0 ? "fwd" : (side >= 0 ? "back_r" : "back_l");
                if (q.ayakKlip == null) q.ayakKlip = aday;
                if (aday != q.ayakKlip) { q.adayT = (q.adayAd == aday ? q.adayT : 0) + dt; q.adayAd = aday; if (q.adayT > .18f) { q.ayakKlip = aday; q.adayT = 0; } } else q.adayT = 0;
            }
            else { q.ayakKlip = null; q.runPh = 0; }
        }

        // Hazırlık pozu: top yaklaşırken kol geri alınır; poz, vuruş anındaki sınıflamanın aynısıyla seçilir (temasta poz sıçramaz).
        // Drive ve blok kendi hazırlığıyla (ce47f5c): eskiden alçak vuruş klibinden gelip raket yere iniyordu.
        void Prep(Athlete o, string who, float dt)
        {
            float tg = 0; var I = icpt; o.prepT = float.NaN;
            if (s != null && !s.dead && s.from != who && I != null && I.who == who && I.t - s.t < .7f)
            {
                tg = 1; o.prepT = I.t - s.t; o.prepShot = I.lunge ? "lunge" : ShotClass(who == "op" ? (I.opType ?? "") : "", I.z, I.y);
                o.prepHand = HandOf(o, who, I.x, I.z);
                o.prepClip = ClipHand(o.prepShot == "over" ? (who == "op" && I.opType == "smash" && o.prepHand != "back" ? "jump" : who == "op" && I.opType == "drop" && HasClip("drop") ? "drop" : "clear") : o.prepShot, o.prepHand);
            }
            else if (s == null && server == who) { tg = .6f; o.prepShot = "low"; o.prepClip = "serve"; }   // servisçi topu elinde tutar
            if (who == "me" && s != null && !s.dead && s.from == "op")
            {
                Reach ri; bool has = ReachInfo(out ri), yakin = s.y > Court.NET && Hyp(s.x - o.x, s.y - o.y) < 2.6f;
                if (has || yakin)
                {
                    tg = 1; o.prepShot = has && ri.lunge ? "lunge" : ShotClass(s.type == "smash" && s.z <= .8f ? "block" : "", s.z, s.y);
                    if (o.prepShot == "low" && s.type == "smash") o.prepShot = "block";
                    o.prepHand = HandOf(o, "me", s.x, s.z);
                    o.prepClip = ClipHand(o.prepShot == "over" ? (energy >= 1 && o.prepHand != "back" ? "jump" : "clear") : o.prepShot, o.prepHand);
                }
            }
            // İmza hazırlığı (Core/Imza): rakip imza pozunu tutar (yıldırımda sıçrama, tülde file bileği, mermide eğik drive,
            // gölge flick'te drop duruşu: aldatma budur); duvar blokta top karşıdayken blok duruşu.
            if (who == "op" && I != null && I.who == "op" && I.imza != null && s != null && !s.dead && s.from != who && I.t - s.t < .7f)
            { o.prepClip = ClipHand(ImzaKlip(I.imza), o.prepHand ?? "fore"); if (I.imza == "yildirim" && o.prepHand == "back") o.prepClip = "bh_over"; }
            if (who == "op" && o.imzaT > 0 && o.imzaId == "duvarblok") { tg = 1; o.prepShot = "drive"; o.prepClip = "block"; o.prepHand = "fore"; o.prepT = float.NaN; }
            // rakipte geri salınım sakin akar (~0,4 sn); bizde vuruş penceresi kısa, eski hız kalır. Bırakış hızlı.
            o.prepA += (tg - o.prepA) * Math.Min(1, dt * (tg > 0 && who == "op" ? 4.5f : 9));
        }

        public void Update(float dt)
        {
            realClock += dt;
            if (dt > 0) { if (!float.IsNaN(op.px)) { op.vx = (op.x - op.px) / dt; op.vy = (op.y - op.py) / dt; } op.px = op.x; op.py = op.y; }
            foreach (var q in new[] { me, op }) { AyakIsi(q, dt, q == me); q.splitT = Math.Max(0, q.splitT - dt); }
            if (freeze > 0) { freeze -= dt; return; }
            if (ogr != null && OgrDurdur()) return;   // antrenman: top vuruş yerine gelince oyun durur, parmak hareketi gösterilir
            if (slowT > 0) { slowT -= dt; if (slowT <= 0) slow = 1; }
            float d = dt * slow * (HitWindow() ? (reduce ? .7f : (easy ? .3f : .45f) * GS.yavas) : 1);
            if (hakemT > 0) hakemT -= dt;
            if (sjYon == null) sjYon = server;
            if (sjYon != server && !(hakemT > hakemT0 * 1.1f / 1.4f)) { sjT += dt; if (sjT >= .5f || reduce) { sjYon = server; sjT = 0; } }   // servis hakemi yeni servisçiye ~0,3 sn sonra döner
            if (!over)
            {
                bool dinlen = s == null || between;
                me.nefes = Math.Min(1, me.nefes + dt * (dinlen ? .14f : .03f)); op.nefes = Math.Min(1, op.nefes + dt * (dinlen ? .14f : .015f));   // ralli içi dolum yarı
                if (OP_KOSU_YORAR && !dinlen && !float.IsNaN(opRunX)) { float m = Hyp(op.x - opRunX, op.y - opRunY), geri = Math.Max(0, opRunY - op.y); OpYor(.045f * m + .045f * geri * (stil.geriK - 1)); }
                opRunX = op.x; opRunY = op.y;
            }
            net.Advance(d);
            if (over) return;
            if (between) { betweenT -= dt; if (betweenT <= 0) { if (matchDone) { over = true; Emit(Ev.MatchEnd, sc[0] > sc[1] ? "me" : "op"); return; } string msg = serveMsg; SetupServe(); serveMsg = msg; } }
            OgrTick(dt);
            clock += d; me.dive = Math.Max(0, me.dive - d * 1.5f); me.jump = Math.Max(0, me.jump - d * JUMP_HIZ); op.jump = Math.Max(0, op.jump - d * JUMP_HIZ);
            IdleIlerle(me, d); IdleIlerle(op, d);
            me.sw = Math.Max(0, me.sw - d * me.swHiz); op.sw = Math.Max(0, op.sw - d * op.swHiz); me.hop = Math.Max(0, me.hop - d * 5); op.dive = Math.Max(0, op.dive - d * 1.5f);
            foreach (var o in new[] { me, op }) if (o.svAnim != null) { o.svAnim.t += d; if (o.svAnim.t >= o.svAnim.d) { o.svAnim = null; var f = svPending; svPending = null; if (f != null) f(); } }
            if (wait > 0) { wait -= d; if (wait <= 0 && server == "op" && s == null && !between && svPending == null) { wait = 0; OpServe(); } }
            RunMe(d); Prep(me, "me", dt); Prep(op, "op", dt);
            if (s == null) return;
            if (s.dead) { s.yerT += d; return; }   // yerdeki top: sekme + devrilme süresi (Shuttle.YerPoz)
            float rem = d;
            while (rem > 1e-6f)
            {
                float h = Math.Min(Shuttle.DT, rem); float py = s.y; rem -= h; s.t += h;
                if (s.agda) { AgAdim(h); if (s.agda) continue; }   // ağa gömülü: konumu ağ belirler
                if (s.netHit) { s.vy *= (float)Math.Exp(-4 * h); s.vx *= (float)Math.Exp(-4 * h); }
                s.Step(h);
                // Fileden dönen top filenin düzlemini yeniden geçemez (2026-10-26, iç içe geçme yok): vuranın tarafında, ağa yaslı
                // iner; ağın alt kenarının altına inince serbest.
                if (s.netHit && !s.altindan && s.z > Court.NetLimit(s.x) - Court.AG_DERIN - .05f && (s.y - Court.NET) * s.agYan < Court.TOP_PAY)
                { s.y = Court.NET + s.agYan * Court.TOP_PAY; if (s.vy * s.agYan < 0) s.vy = -s.vy * .2f; }
                if (!s.netDone && (py > Court.NET) != (s.y > Court.NET))
                {
                    s.netDone = true; float lim = Court.NetLimit(s.x);
                    if (s.netPred != null ? !s.netPred.ok : s.z <= lim)
                    {
                        // File teması (2026-10-26, yasin "file kritik, bant daha sert kısım, file ise oynak; fileye düştüğünde oyun
                        // animasyonla ilerlemeli"). Geçiş adımında top düzlemi bir adımlık yol kadar (smaçta ~20 cm) aşmış olur;
                        // eskiden orada kalıp ağın içinden görünüyordu. Şimdi çarpılan parçaya göre:
                        //  - bandın üst 3 cm'i + hızlı top: kordondan karşıya yuvarlanır (oyunda kalır, BWF servis dahil), bant titrer;
                        //  - bant (üst 75 mm, gergin kordon): sert ve kısa seker, hızın çoğu ölür, top vuranın tarafına düşer;
                        //  - ağ: top ağa gömülür, ağ şişer (görünen sapma topun ittiği kadar, smaçta ~30 cm, dropta ~6 cm), top
                        //    yavaşlar ve ağ boyunca biraz kayar, yavaş top kısa bir an asılı kalır, sonra ağ onu vuranın tarafına
                        //    bırakır; ağ dalgası serbestçe sürer, top fileye yaslı yere iner ve yerde sekip yan yatar. Sayı top
                        //    yere değince verilir: düşüş oynar, oyun aniden kesilmez;
                        //  - ağın alt kenarının altı: top altından geçer (hata), ağa dokunmaz.
                        float sp = s.Speed, k = Math.Min(1, sp / 55f), yan = py > Court.NET ? 1 : -1, pen = lim - s.z; s.agYan = yan;
                        if (pen > Court.AG_DERIN) { s.netHit = true; s.altindan = true; s.netK = k; Emit(Ev.Net, s.from, s.type, s.x, Court.NET, s.z, k * .3f); }
                        else if (pen < .03f && Math.Abs(s.vy) > 6)
                        {
                            s.y = Court.NET - yan * Court.TOP_PAY; s.z = lim + .02f; s.vy = -yan * 1.3f; s.vx *= .3f; s.vz = .7f; s.netCord = true; net.Impulse(s.x, lim, sp, true, -yan);
                            Emit(Ev.Cord, s.from, s.type, s.x, Court.NET, s.z, .3f); Say(null, null, s.x, Court.NET, s.z, "File bandı! Top karşıya yuvarlandı", 2);
                        }
                        else if (pen < Court.BANT)
                        {
                            net.Impulse(s.x, lim, sp, true, -yan); net.Impulse(s.x, lim - Court.BANT, sp * .3f, false, -yan);
                            s.y = Court.NET + yan * Court.TOP_PAY; s.vx *= .35f; s.vy = yan * (.25f + .35f * k); s.vz = Math.Max(0, s.vz * .1f) + .25f * (1 - pen / Court.BANT);
                            s.netHit = true; s.netK = k; Emit(Ev.Net, s.from, s.type, s.x, Court.NET, s.z, k, null, null, true);
                        }
                        else
                        {
                            s.agda = true; s.agT = 0; s.agSure = .07f + .08f * k; s.agAsili = (1 - k) * .12f; s.agMax = .05f + .25f * k;
                            s.y = Court.NET; s.vx *= .3f; s.vz = Math.Min(0, s.vz * .2f); s.netHit = true; s.netK = k; net.glow = Math.Max(net.glow, k);
                            Emit(Ev.Net, s.from, s.type, s.x, Court.NET, s.z, k, null, null, false);
                        }
                    }
                    else if (lim - s.z > -.045f && s.z > lim) { net.Impulse(s.x, s.z, s.Speed, true, s.vy > 0 ? 1 : -1); Emit(Ev.Tape, s.from, s.type, s.x, Court.NET, s.z); Say(null, null, s.x, Court.NET, s.z, "Bandı sıyırdı!"); }
                }
                if (s.z <= 0) { s.z = 0; s.inVx = s.vx; s.inVy = s.vy; s.inVz = s.vz; Land(); return; }
            }
            var I = icpt;
            ImzaTick(I, d);
            if (s.from == "me")
            {
                if (I != null && I.who == "op" && !I.leave)
                {
                    if (op.fakeUntil > clock) MoveAcc(op, op.fakeX, op.y, 1.6f, d);
                    else MoveAcc(op, Clamp(I.x + HitSide(op, "op", I), -.4f, Court.CW + .4f), I.y - .3f, OpSpd(), d);
                    // tek sıçrama, tepe temasta (ce47f5c): eskiden temastan önce ve sonra iki zıplama
                    if (I.opType == "smash" && op.prepClip == "jump" && I.t - s.t < SALINIM["jump"] && I.t - s.t > 0 && !(op.jump > 0)) op.jump = 1 - (SALINIM["jump"] - (I.t - s.t)) * JUMP_HIZ;
                    if (s.t >= I.t) { OpHit(); return; }
                }
                else
                {
                    MoveAcc(op, s.landPred.x, s.landPred.y - .4f, 2.2f, d);
                    if (op.dive <= 0 && !IsOut(s.landPred.x, s.landPred.y, s.serve) && !(s.landPred.net != null && !s.landPred.net.ok) && s.t > s.landPred.t - .25f)
                    { op.dive = 1; op.yat = true; Emit(Ev.Kurtaris, "op", null, op.x, op.y); Say("op", null, op.x, op.y, 0, "Kıl payı!"); if (!reduce) { slow = .35f; slowT = .3f; } }
                }
            }
            // Ana nokta fileden stil.ev metre (2026-10-26): eskiden y = ev yazılıyordu, yani rakibin dip çizgisinden. "Fileye yakın
            // durur" denen File oyuncusu (ev 2,2) fileden 4,5 m'de en derinde, Duvar (3,6) en önde bekliyordu; üç değer ancak
            // fileden okununca tarifle uyuşur (Mika 2,2 önde, Blaze 3,2, Rocco 3,6 geride; tekler tabanı fileden ~3–3,5 m).
            else MoveAcc(op, Court.MID, Court.NET - OpEv(), 2.0f, d);   // OpEv: Aria'nın uyumu (Core/Imza), diğerlerinde stil.ev
        }

        // Poz seçimi (HTML player()): servis hazırlığı → vuruş takibi → servis duruşu → yorgun → hazırlık (temasa kalan
        // süreyle) → split → badminton ayak işi → bekleyiş. ph: klip içi faz 0..1; karenin ortası (k+.5)/n o kareyi gösterir.
        public void PickClip(Athlete o, bool isMe, out string clip, out float ph)
        {
            float spd = Hyp(o.svx, o.svy), hit; string who = isMe ? "me" : "op";
            if (o.svAnim != null) { clip = o.svAnim.clip; hit = CLIP_HIT.TryGetValue(clip, out hit) ? hit : .5f; ph = hit * Math.Min(.999f, o.svAnim.t / o.svAnim.d); }   // servis: klip başından temasa
            else if (o.sw > 0) { clip = o.clip ?? "low"; hit = CLIP_HIT.TryGetValue(clip, out hit) ? hit : .55f; ph = hit + (1 - o.sw) * (1 - hit); }
            else if (s == null && !between && server == who && o.svClip != null) { clip = o.svClip; ph = 0; }   // servis duruşu
            else if (!isMe && (s == null || between) && o.nefes < .4f && spd < .4f) { clip = "yorgun"; ph = (clock * .55f) % 1f; }   // nefesi bitmiş rakip sayı arasında eller dizde
            else if (o.prepA > .08f && o.prepClip != null)
            {
                clip = o.prepClip; hit = CLIP_HIT.TryGetValue(clip, out hit) ? hit : .55f; int hold, n;
                if (!PREP_KARE.TryGetValue(clip, out hold) || !CLIP_N.TryGetValue(clip, out n)) ph = hit * .9f * Math.Min(1, o.prepA);
                else
                {
                    int hitF = (int)Math.Floor(hit * n); float sal; if (!SALINIM.TryGetValue(clip, out sal)) sal = SALINIM_V; float f = Math.Min(1, o.prepA) * hold;
                    if (!float.IsNaN(o.prepT) && o.prepT < sal) f = hold + (hitF - hold) * (1 - o.prepT / sal);
                    ph = (Math.Min(hitF - 1, (float)Math.Floor(f)) + .5f) / n;   // tam kare (46943a8): komşu kare karışmaz   // temas karesi ancak gerçek temasta (o.sw dalı) gelir
                }
            }
            // Split yalnız yerindeyken (2026-10-26): eşik 1,5 m/s'ydi; split pozu ayakları yerinde tutar, gövde 1,5 m/s'ye kadar
            // kayıyordu (bot ölçümü: split karelerinin rakipte %70'i, bende %44'ü ayak kaymalı). Hareket başlayınca ayak işi klibi.
            else if (o.splitT > 0 && spd < .6f) { clip = "split"; ph = 1 - o.splitT / .3f; }
            else if (o.yuru && o.ayakKlip != null) { clip = o.ayakKlip; ph = o.runPh; }
            else { clip = "idle"; ph = (o.idlePh + (isMe ? 0 : .5f)) % 1f; }
        }
        // Boşta duruş nefese bağlı (Ar-Ge 23 Eki animasyon B4): dolu nefeste eski hız (0,8 döngü/sn), nefes azaldıkça
        // %90'a kadar hızlanır; uzun ralliden sonra beden de soluklanır. Faz birikir (saatle çarpılsa nefes değişince
        // poz sıçrardı). Genlik görünümde: PlayerView boşta pozuna yorgun klibini nefesle karıştırır.
        static void IdleIlerle(Athlete o, float d) { o.idlePh = (o.idlePh + d * .8f * (1 + .9f * (1 - o.nefes))) % 1f; }
        // Görünüm için sıçrama yüksekliği oranı (0..1.2): tam sıçrama sin(jump·π), split step hop'u bunun %20'si.
        public static float JumpH(Athlete o) { return (float)(Math.Sin(Math.Min(1, o.jump) * Math.PI) + Math.Sin(o.hop * Math.PI) * .2); }
    }
}
