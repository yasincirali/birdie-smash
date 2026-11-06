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
    // Vuruş laboratuvarı ölçümü (sonuç kartı): ad (VurusAdi), hedefte mi, fileye takıldı / dışarı, fileyi kaç m aşarak geçti
    // (NaN = fileye varmadı), düştüğü yerin fileye (dip false) ya da dip çizgiye (dip true) uzaklığı (m), çıkış hızı (km/s).
    public class DrilOlcum { public string ad, neden; public bool hedef, file, disari, dip; public float gec = float.NaN, mesafe, hiz; }

    public class DrilTanim
    {
        public string id, ad, ogretir, nasil, besle, olcu; public string[] tip; public float[] alan; public bool kose; public int top = 12; public int[] esik = { 4, 7, 10 };
        // 2026-10-30 vuruş laboratuvarı (yasin: "net droplarımı, clearlerimi, file önü ya da back court smashlerimi detaylıca
        // geliştirebilmeliyim"): görevler sahadaki yere göre gruplanır (grup: arka / file / orta) ve aynı vuruşun çeşitleri ayrı
        // çalışılır. İsteğe bağlı koşullar (yoksa koşul yok): yon duz/capraz (vuruş noktasıyla düşüş arasındaki yan kayma),
        // vurus [a, b] (vuruş noktasının fileye uzaklığı, m: "orta sahadan"), tepe [a, b] (uçuşun tepe yüksekliği, m: savunma
        // clear yüksek, atak clear alçak), gec (fileyi en çok kaç m aşarak geçsin: file dibi, drive), servis kisa/flick.
        // olcu "cesit": antrenör rastgele besler, aynı vuruşu üst üste atmadan içeri düşürmek sayılır.
        public string grup = "arka", yon, servis; public float[] vurus, tepe; public float gec = float.NaN;
        public int Yildiz(int isabet) { int n = 0; foreach (var e in esik) if (isabet >= e) n++; return n; }
    }

    public static class Dril
    {
        public const int YILDIZ = 3;
        static DrilTanim[] liste;
        public static DrilTanim[] Liste { get { Yukle(); return liste; } }
        public static int ToplamYildiz { get { return Liste.Length * YILDIZ; } }
        public static void Sifirla() { liste = null; }
        public static DrilTanim Bul(string id) { if (id == Sonsuz.ID) return Sonsuz.Tanim; foreach (var d in Liste) if (d.id == id) return d; return null; }

        static void Yukle()
        {
            if (liste != null) return;
            var o = Json.Obj(Json.Parse(KariyerVeri.Metin("antrenman"))); if (o == null) throw new InvalidOperationException("Birdie Smash: veri/antrenman.json okunamadı");
            var l = new List<DrilTanim>();
            foreach (var x in (List<object>)o["gorevler"])
            {
                var d = Json.Obj(x);
                var t = new DrilTanim { id = Json.Str(d, "id", ""), ad = Json.Str(d, "ad", ""), ogretir = Json.Str(d, "ogretir", ""), nasil = Json.Str(d, "nasil", ""), besle = Json.Str(d, "besle", null), olcu = Json.Str(d, "olcu", "alan"), kose = Json.Bool(d, "kose", false), top = (int)Json.Num(d, "top", 12),
                    grup = Json.Str(d, "grup", "arka"), yon = Json.Str(d, "yon", null), servis = Json.Str(d, "servis", null), vurus = Cift(d, "vurus"), tepe = Cift(d, "tepe"), gec = d.ContainsKey("gec") ? (float)Json.Num(d, "gec", 0) : float.NaN };
                if (Array.IndexOf(GRUP, t.grup) < 0) throw new InvalidOperationException("Birdie Smash: antrenman.json " + t.id + " grubu: " + t.grup);
                var tl = d.ContainsKey("tip") ? d["tip"] as List<object> : null; if (tl != null) { t.tip = new string[tl.Count]; for (int i = 0; i < tl.Count; i++) t.tip[i] = (string)tl[i]; }
                var al = d.ContainsKey("alan") ? d["alan"] as List<object> : null; if (al != null) { t.alan = new float[4]; for (int i = 0; i < 4; i++) t.alan[i] = (float)(double)al[i]; }
                var el = d.ContainsKey("esik") ? d["esik"] as List<object> : null; if (el != null) { t.esik = new int[el.Count]; for (int i = 0; i < el.Count; i++) t.esik[i] = (int)(double)el[i]; }
                if (t.esik.Length != YILDIZ || t.esik[YILDIZ - 1] > t.top) throw new InvalidOperationException("Birdie Smash: antrenman.json " + t.id + " eşikleri");
                if (t.olcu == "alan" && (t.alan == null || t.tip == null)) throw new InvalidOperationException("Birdie Smash: antrenman.json " + t.id + " alan/tip yok");
                l.Add(t);
            }
            liste = l.ToArray();
        }
        static float[] Cift(Dictionary<string, object> d, string k) { var a = d.ContainsKey(k) ? d[k] as List<object> : null; return a == null || a.Count != 2 ? null : new[] { (float)(double)a[0], (float)(double)a[1] }; }
        // Gruplar ve görünen adları (Antrenman sayfası bu sırayla gösterir).
        public static readonly string[] GRUP = { "arka", "file", "orta" };
        public static readonly string[] GRUP_AD = { "Arka saha", "File önü", "Orta saha ve karışık" };
        // Savunma ve atak clear'i ayıran tepe yüksekliği (m). Ölçüm (Denge "lab", 2026-10-30): oyuncunun clear'leri tepe 4,1–7,9 m;
        // orta-hızlı kaydırma (sp 1,0–1,4) 6 m'nin üstüne, hızlı ve uzun kaydırma (sp ≥ 1,6) altına çıkar.
        public const float TEPE_SAVUNMA = 6.0f;

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

    // 2026-11-06 Sonsuz ralli (ek tur fikri 6, oyuncu gözü YAP; yasin "arcade önce"): Antrenman'da 30 saniyelik "bir el daha"
    // modu. Antrenör hiç durmadan besler, her 5 isabetli vuruşta bir kademe: top yassılaşıp hızlanır, yana açılır, yeni besleme
    // türü girer (clear → drive → file → yarım → smaç). İlk kaçan ya da dışarı/fileye giden topta biter. Skor = isabetli vuruş.
    // Görev altyapısının (ogr + dril) üstünde: skor yok, antrenör geri oynamaz kuralları aynı. Yıldız listesine (Liste) girmez,
    // ToplamYildiz değişmez. Ödül YOK (ekonomi kıt kuralı, inceleme 6 Kas 13:30): yalnız kişisel rekor (tuytop_sonsuz, buluta da
    // gider) ve 10/20/30 eşiğinin ilk geçişi sonuçta bir kez söylenir. Game Center başarımı eklenmedi: App Store Connect'te
    // başarım puanı 1000 sınırında (Basarim.cs), yeni kimlik yer açmadan girmez. İlk sürüm tek zorluk (Kolay koşu yardımı).
    public static class Sonsuz
    {
        public const string ID = "sonsuz", ANAHTAR = "tuytop_sonsuz";
        public const int ADIM = 5, TAVAN = 6;   // her 5 isabette bir kademe; 6. kademeden (30 vuruş) sonra hız sabit
        public static readonly int[] ESIK = { 10, 20, 30 };
        public static readonly DrilTanim Tanim = new DrilTanim { id = ID, ad = "Sonsuz ralli", ogretir = "Kaçırana kadar her topu içeri düşür.",
            nasil = "Antrenör durmadan besler. Her 5 vuruşta top hızlanır ve yana açılır. İlk kaçan top oyunu bitirir.", besle = ID, olcu = ID, grup = "orta",
            top = 100000, esik = new[] { 0, 0, 0 } };
        public static int Kademe(int isabet) { return Math.Min(TAVAN, isabet / ADIM); }
        // Kademenin besleme havuzu: yeni tür sırayla girer, oyuncu önce ritmi bulsun.
        static readonly string[] SIRA = { "clear", "drive", "file", "yarim", "smash" };
        public static int Havuz(int kademe) { return Math.Min(SIRA.Length, 1 + kademe); }
        public static string Tur(int i) { return SIRA[i]; }
        // Bu skorla ilk kez geçilen eşik (önceki rekor altındaydı); yoksa 0. En büyüğü söylenir.
        public static int YeniEsik(int skor, int eskiRekor) { int e = 0; foreach (var x in ESIK) if (skor >= x && eskiRekor < x) e = x; return e; }
    }

    public partial class Match
    {
        DrilTanim ogrDril;
        public DrilTanim Dril { get { return ogr != null ? ogr.dril : null; } }
        // Görevin top başı durumu: köşe (0 sol, 1 sağ), bu topta başarı, oyuncu vurdu mu, split/aldatma sayacının top başı değeri.
        int drKose, drSplit0, drAldat0; bool drBasari, drVurdu;
        string drBesleSu, drNeden, drOncekiVurus;   // bu topun beslemesi (çeşitle/karışık), kaçan topun nedeni, çeşitle: önceki vuruş
        // Vuruş laboratuvarı sonuç kartı (Hud): oyuncunun her topu yere ya da fileye düşünce bir ölçüm; No her ölçümde artar.
        public DrilOlcum drOlcum; public int drOlcumNo;
        public bool DrilBitti { get { var d = Dril; return d != null && (ogr.dene >= d.top || ogr.bitisT > 0 || ogr.bitti); } }

        // Görev tek adımlı bir antrenmandır (OgrKur çağırır).
        void DrilKur(DrilTanim d)
        {
            var a = new OgrAdim { bas = d.ad, met = d.nasil, neden = d.ogretir, dril = d, besle = d.besle, servis = d.olcu == "servis" ? d.servis ?? "kisa" : null, slice = d.olcu == "aldat" };
            a.tip = d.olcu == "kill" ? new[] { "kill" } : d.olcu == "karsila" ? new[] { "block", "lift" } : d.tip; a.iz = DrilIz(d);
            ogr.liste = new List<OgrAdim> { a }; ogr.dril = d;
        }

        // Bu topun hedef alanı [x0, x1, y0, y1] (rakip yarısı); köşe görevinde sırayla sol ve sağ köşe. null = alan yok.
        public float[] DrilAlan()
        {
            var d = Dril; if (d == null || d.alan == null || d.olcu == "aldat" || d.olcu == "karsila" || d.olcu == "kill" || d.olcu == "split" || d.olcu == "cesit") return null;
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
            drBasari = false; drVurdu = false; drNeden = null; drSplit0 = splits; drAldat0 = deceives; drKose = ogr.dene % 2;
            if (ogr.dene == 0) drOncekiVurus = null;
            // Çeşitle: antrenör rastgele besler, aynı beslemeyi üst üste vermez (oyuncu her topta yeniden okusun).
            string b = d.besle;
            if (b == "karisik") b = ogr.dene % 2 == 0 ? "clear" : "drive";
            else if (b == "cesit") { string yeni; do yeni = CESIT_BESLE[(int)(R01() * CESIT_BESLE.Length) % CESIT_BESLE.Length]; while (yeni == drBesleSu && ogr.dene > 0); b = yeni; }
            else if (b == Sonsuz.ID)
            {   // kademe havuzundan, iki tür varsa aynısını üst üste vermeden; ilk top hep clear (ritmi bul)
                int n = Sonsuz.Havuz(Sonsuz.Kademe(ogr.isabet)); string yeni = "clear";
                if (ogr.dene > 0 && n > 1) do yeni = Sonsuz.Tur((int)(R01() * n) % n); while (yeni == drBesleSu);
                b = yeni;
            }
            drBesleSu = b;
            if (b == "smash") { op.x = Court.MID; op.y = 3.0f; }
            else if (b == "gevsek") { op.x = Court.MID; op.y = Court.NET - 1.6f; }
            else if (b == "file") { op.x = Court.MID; op.y = Court.NET - 1.0f; }
            else if (b == "yarim") { op.x = Court.MID; op.y = Court.NET - 3.0f; }
            else if (b == "orta") { op.x = Court.MID; op.y = Court.NET - 2.0f; }
        }

        // Antrenörün elle beslemesi (OpServe yerine). Geri sayım ya da süre baskısı yok: top sayısı biter, görev biter.
        void DrilBesle()
        {
            var d = Dril; if (d == null || DrilBitti) return;
            float hx = op.x, hy = op.y, vx, vy, vz; string b = drBesleSu ?? d.besle;
            if (d.olcu == Sonsuz.ID) { SonsuzBesle(b, hx, hy); return; }
            switch (b)
            {
                case "file":
                    {   // file dibine düşen alçak top: oyuncu bandın altında alır (net shot, lift); öldürülecek top değil
                        float hz = .5f, tx = Court.MID + (R01() - .5f) * 1.6f; Shuttle.Aim(hx, hy, hz, tx, Court.NET + .75f, true, 1.2f, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drop", false, "good"); return;
                    }
                case "yarim":
                    {   // orta sahadan ön-orta sahaya düz top (göğüs hizası): push ve drive
                        float hz = 1.1f, tx = Court.MID + (R01() - .5f) * 1.8f, ty = Court.NET + 2.6f + R01() * .5f; Shuttle.Aim(hx, hy, hz, tx, ty, true, .12f, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drive", false, "good"); return;
                    }
                case "orta":
                    {   // orta sahaya yüksek top: oyuncu baş üstünde, fileye yakın alır (dik smaç)
                        float hz = .9f, tx = Court.MID + (R01() - .5f) * 1.6f, ty = Court.NET + 4.1f + R01() * .4f;   // oyuncu ~1 m önde alır (fileden 2,6–3,4 m)
                        Shuttle.Aim(hx, hy, hz, tx, ty, true, LIFT_A, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "clear", false, "good"); return;
                    }
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
                    {   // 2026-10-30: düz ve alçak (açı .1 rad; Aim fileyi geçmezse kendisi yükseltir). Eski 18 m/s hız hedefli besleme
                        // yay çiziyordu, oyuncu topu file önünde 2,9 m'de alıyordu: drive görevinde topların %92'si file öldürme oldu.
                        float hz = 1.25f, tx = Court.MID + (R01() - .5f) * 2.0f; Shuttle.Aim(hx, hy, hz, tx, Court.NET + 4.6f, true, .1f, out vx, out vy, out vz);
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

        // Sonsuz rallinin beslemesi: görevdeki aynı türler, kademe k (0–6) ile yassılaşır (açı küçülür, top daha erken gelir),
        // yana açılır (yay) ve smaç sertleşir. Sayılar görev beslemesinden; kademe 0 görevle aynı.
        void SonsuzBesle(string b, float hx, float hy)
        {
            int k = Sonsuz.Kademe(ogr.isabet); float yay = 1 + .12f * k, vx, vy, vz;
            switch (b)
            {
                case "drive":
                    {
                        float hz = 1.25f + .03f * k, tx = Court.MID + (R01() - .5f) * 2.0f * yay; Shuttle.Aim(hx, hy, hz, KortIci(tx), Court.NET + 4.6f + .1f * k, true, .1f, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drive", false, "good"); return;
                    }
                case "file":
                    {
                        float hz = .5f, tx = Court.MID + (R01() - .5f) * 1.6f * yay; Shuttle.Aim(hx, hy, hz, KortIci(tx), Court.NET + .75f, true, 1.2f - .04f * k, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drop", false, "good"); return;
                    }
                case "yarim":
                    {
                        float hz = 1.1f, tx = Court.MID + (R01() - .5f) * 1.8f * yay, ty = Court.NET + 2.6f + R01() * .5f; Shuttle.Aim(hx, hy, hz, KortIci(tx), ty, true, .12f, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "drive", false, "good"); return;
                    }
                case "smash":
                    {
                        float hz = 2.5f, tx = Court.MID + (R01() - .5f) * 2.4f * yay, ty = Court.NET + 3.4f + R01() * 1.2f;
                        Shuttle.Aim(hx, hy, hz, KortIci(tx), ty, false, 52 * (.72f + .03f * k), out vx, out vy, out vz);
                        if (!FileGecer(hx, hy, hz, vx, vy, vz)) Shuttle.Aim(hx, hy, hz, KortIci(tx), ty, true, .25f, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "smash", false, "good"); return;
                    }
                default:
                    {   // clear: kademeyle savunma yayından atak clear'e iner (LIFT_A → CLEAR_A)
                        float hz = 1.0f, tx = Court.MID + (R01() - .5f) * 2.0f * yay, ty = Court.L - 1.3f - R01() * .7f, a = LIFT_A - (LIFT_A - CLEAR_A) * k / Sonsuz.TAVAN;
                        Shuttle.Aim(hx, hy, hz, KortIci(tx), ty, true, a, out vx, out vy, out vz);
                        StartFlight("op", vx, vy, vz, hx, hy, hz, false, "clear", false, "good"); return;
                    }
            }
        }
        // Yana açılan besleme tek kortun içinde kalsın (çizgiden 0,35 m içeri): antrenörün dışarı topu oyuncuyu haksız bitirir.
        static float KortIci(float x) { return Math.Max(Court.SL + .35f, Math.Min(Court.SR - .35f, x)); }

        // Oyuncunun topu yere düştü (Land, sayıdan önce): bu top başarılı mı.
        void DrilDusus()
        {
            var d = Dril; if (d == null || s.from != "me") return;
            bool ic = !s.netHit && !IsOut(s.x, s.y, s.serve);
            switch (d.olcu)
            {
                case "alan":
                    drNeden = !ic ? null : s.serve || !TipUyar(d, s) ? "Vuruş türü başka" : KosulNeden(d, s) ?? (Icinde(DrilAlan(), s.x, s.y) ? null : "Alana düşmedi");
                    drBasari = ic && drNeden == null; break;
                case "servis":
                    {
                        string tur = d.servis ?? "kisa";
                        drNeden = !ic ? null : !s.serve ? "Top kaçtı" : s.servisTur != tur ? (tur == "kisa" ? "Kısa servis değil: yavaş it" : "Flick değil: kısa ve sert fiske")
                            : tur == "kisa" && !float.IsNaN(s.gecPay) && s.gecPay >= DRIL_BANT ? "Yüksek geçti: daha yavaş it" : Icinde(DrilAlan(), s.x, s.y) ? null : "Alana düşmedi";
                        drBasari = ic && drNeden == null; break;
                    }
                case "cesit":
                    drNeden = !ic ? null : drOncekiVurus != null && s.vurus == drOncekiVurus ? "Aynı vuruş: bu sefer başka bir şey dene" : null;
                    drBasari = ic && drNeden == null; break;
                case "aldat": drBasari = ic && deceives > drAldat0; break;
                // Savunma: geri dönen top ya kısa (servis çizgisinin önü: blok) ya derin (arka 2 m: lift) olmalı; ortaya düşen
                // karşılama maçta rakibe ikinci smacı verir. Kural ölçümle (Denge dril): "her dönüş sayılır" botta %100 ★★★ idi.
                case "karsila": drBasari = ic && drVurdu && (s.y >= Court.NET - Court.SSL || s.y <= 2.0f); break;
                case "kill": drBasari = ic && sonTip == "kill"; break;
                case "split": drBasari = drVurdu && splits > drSplit0; break;
                case Sonsuz.ID: drBasari = ic && !s.serve; break;   // her içeri düşen vuruş sayılır; tür serbest
            }
            if (!s.serve || d.olcu == "servis") drOncekiVurus = s.serve ? "servis" : s.vurus;
            drOlcum = Olc(s, drBasari); drOlcumNo++;
            if (!drBasari) drOlcum.neden = drOlcum.file ? "Fileye takıldı" : drOlcum.disari ? "Dışarı" : drNeden ?? DrilNeden(null);
        }
        static readonly string[] CESIT_BESLE = { "clear", "drive", "file", "smash", "yarim" };
        // Görevin vuruş türü: uçuş sınıfı (type) ya da vuruşun kendi adı (vurus: lift, blok, kill uçuşta başka sınıfla uçar).
        static bool TipUyar(DrilTanim d, Shuttle t) { return d.tip != null && (Array.IndexOf(d.tip, t.type) >= 0 || Array.IndexOf(d.tip, t.vurus) >= 0); }
        // Görevin ek koşulları (yön, vuruş yeri, tepe, file payı); ilk tutmayanın oyuncuya söylenecek nedeni, hepsi tutarsa null.
        static string KosulNeden(DrilTanim d, Shuttle t)
        {
            float yan = Math.Abs(t.x - t.hx), dn = t.hy - Court.NET;
            if (d.vurus != null && (dn < d.vurus[0] || dn > d.vurus[1])) return dn < d.vurus[0] ? "Fileye çok yakın vurdun" : "Çok geriden vurdun: orta sahada al";
            if (d.yon == "duz" && yan > YAN_DUZ) return "Çapraz gitti: düz kaydır";
            if (d.yon == "capraz" && yan < YAN_CAPRAZ) return "Düz gitti: parmağını çaprazına eğ";
            if (d.tepe != null && t.zTepe < d.tepe[0]) return "Alçak kaldı: daha yavaş ve uzun kaydır";
            if (d.tepe != null && t.zTepe > d.tepe[1]) return "Yüksek gitti: daha hızlı kaydır";
            if (!float.IsNaN(d.gec) && !float.IsNaN(t.gecPay) && t.gecPay > d.gec) return "Fileyi yüksek geçti";
            return null;
        }
        // Düz / çapraz: vuruş noktasıyla düşüş arasındaki yan kayma (m). Tek kort eni 5,18 m; arası (1,0–1,6) "orta", adı yönsüz.
        public const float YAN_DUZ = 1.0f, YAN_CAPRAZ = 1.6f;
        // Orta sahadan smaç (dik iner): vuruş fileye bundan yakın. Ölçüm: arka saha beslemesinde oyuncu topu fileden 3,9–4,7 m'de alır (p10–p90).
        public const float DIK_SMAC = 3.6f;

        // Topun görünen adı (sonuç kartı): vuruş + çeşidi (düz/çapraz, savunma/atak, file önünde file dibi, ön sahadan push).
        public static string VurusAdi(Shuttle t)
        {
            if (t.serve) return t.servisTur == "flick" ? "Flick servis" : t.servisTur == "yuksek" ? "Yüksek servis" : "Kısa servis";
            float yan = Math.Abs(t.x - t.hx), dn = t.hy - Court.NET; int y = yan >= YAN_CAPRAZ ? 2 : yan <= YAN_DUZ ? 1 : 0;
            switch (t.vurus)
            {
                case "jump": return "Sıçrayarak smaç";
                case "smash": return dn < DIK_SMAC ? "Dik smaç" : y == 2 ? "Çapraz smaç" : y == 1 ? "Düz smaç" : "Smaç";
                case "kill": return "File öldürme";
                case "clear": return t.hz < 2.0f ? "Lift" : t.zTepe >= BirdieSmash.Core.Dril.TEPE_SAVUNMA ? "Savunma clear" : "Atak clear";   // alttan clear = lift
                case "lift": return "Lift";
                case "drop": return dn < 2.3f ? (y == 2 ? "Çapraz file" : "File dibi") : y == 2 ? "Çapraz drop" : y == 1 ? "Düz drop" : "Drop";
                case "block": return "Blok";
                case "push": return "Push";
                case "drive": return "Drive";
            }
            return t.vurus ?? t.type;
        }
        DrilOlcum Olc(Shuttle t, bool hedef)
        {
            var o = new DrilOlcum { ad = VurusAdi(t), hedef = hedef, file = t.netHit, gec = t.gecPay, hiz = t.v0 * 3.6f };
            if (!t.netHit)
            {
                o.disari = IsOut(t.x, t.y, t.serve);
                // ön yarıya düşen top fileye, arka yarıya düşen dip çizgiye uzaklıkla okunur (rakip yarısı: y 0 dip, 6,7 file)
                o.dip = t.y < Court.NET / 2; o.mesafe = o.dip ? t.y : Court.NET - t.y;
            }
            return o;
        }
        // Kısa servis "bandı sıyırır": file üstünden geçerken bant payı 30 cm'den az.
        public const float DRIL_BANT = .3f;

        // Top bitti (Point başı): say, söyle; son top sayıldıysa görev biter (1,4 sn sonra, Temel dersle aynı kapanış).
        string DrilTop(string winner, string why)
        {
            var d = Dril; ogr.dene++;
            if (d.olcu == Sonsuz.ID)
            {   // ilk kaçan top biter; kademe atlayınca söylenir (oyuncu hızlanmayı hissetmeden önce bilsin)
                if (!drBasari) { if (ogr.bitisT <= 0 && !ogr.bitti) ogr.bitisT = 1.4f; return L_Bitti(why); }
                ogr.isabet++;
                return ogr.isabet % Sonsuz.ADIM == 0 && Sonsuz.Kademe(ogr.isabet) <= Sonsuz.TAVAN && ogr.isabet <= Sonsuz.ADIM * Sonsuz.TAVAN ? ogr.isabet + " · Hızlanıyor" : ogr.isabet.ToString();
            }
            if (drBasari) ogr.isabet++;
            string t = drBasari ? "Hedefte! ★ " + ogr.isabet + "/" + d.top : DrilNeden(why);
            if (ogr.dene >= d.top && ogr.bitisT <= 0 && !ogr.bitti) ogr.bitisT = 1.4f;
            return t;
        }
        string L_Bitti(string why) { string n = DrilNeden(why); return n == "Top kaçtı" || n == "Alana düşmedi" ? "Bitti" : n; }
        // Kaçan topun tek kelimelik nedeni (oyuncu neyi düzelteceğini görsün).
        string DrilNeden(string why)
        {
            var d = Dril; if (!drVurdu && d.olcu != "servis") return d.olcu == "split" && splits <= drSplit0 ? "Split step yok: antrenör vururken dokun" : "Top kaçtı";
            if (s != null && s.netHit) return "Fileye takıldı";
            if (s != null && s.from == "me" && IsOut(s.x, s.y, s.serve)) return "Dışarı";
            if (drNeden != null) return drNeden;
            switch (d.olcu)
            {
                case "alan": case "servis": case "cesit": return s != null && s.from != "me" ? "Top kaçtı" : "Alana düşmedi";
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
