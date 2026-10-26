// Kariyer denge ölçümü (komple-oyun tasarımı §3.2 testere dişi; adım 2–3, 2026-10-26). Unity'siz:
//   mcs -nologo -nowarn:219 -out:denge.exe tool/unity_test/Denge.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs
//   mono denge.exe [maç sayısı]          (depo kökünden; veri tool/veri'den okunur)
// Neden ayrı araç, BotMac'te değil: BotMac her koşuda çalışan doğruluk testi (saniyeler); denge ölçümü binlerce maç ister
// ve bir eşikle kırılmaz, sayıları rapor eder (eğri tasarım tablosuna göre okunur, g değerleri kariyer.json'da ayarlanır).
// Bot: BotMac'teki insan benzeri bot (pHit .12, rastgele kaydırma). Mutlak oran botun ustalığına bağlıdır; anlamlı olan
// basamaklar arası eğridir (her basamak bir öncekinden biraz zor, turun 1. basamağı önceki finalden kolay = testere).
// Ekipman: turun önerilen EP aralığının alt ucu ve ortası (EP = 6 yuvanın seviye toplamı, Gear.KSv ile aynı ölçek),
// kariyer modunda (eşit şartlar kapalı). Uzunluk: basamağın verisi; Tur 1'de oyuncu seçer, 5 ile ölçülür.
// 2026-10-26 ekipman v2 + karakter (tasarım Ek A.5): üç ek kip, ilk argüman ad:
//   mono denge.exe model [maç]   raket modeli × tur → galibiyet % (hedef: en iyi − en kötü ≤ 8 puan)
//   mono denge.exe tarz  [maç]   oyun tarzı × tur → galibiyet % (hedef: fark ≤ 5 puan)
//   mono denge.exe tempo         maç maç ekonomi simülasyonu: "yeni şey" olayları arası en uzun boşluk (hedef ≤ 4 maç)
// Model/tarz kipinde her hücre turun 6 basamağının ortalamasıdır (aynı tohumlar: hücreler arası fark yalnız modelden/tarzdan
// gelir), ekipman turun önerilen EP'sinin ortası. Bot modelin/tarzın hilesini bilerek kullanmaz (slice %10, rastgele): ölçülen
// "kör oyuncuya" üstünlük; yetenekli oyuncunun Kesici/Aldatıcı'dan alacağı pay bunun üstündedir (TestFlight ayarlar).
// Tempo kipi maç oynamaz: galibiyet olasılığı sabit (%60, tasarım §5.7 varsayımı), Odul() gerçek ekonomi fonksiyonu.
using System;
using System.Collections.Generic;
using BirdieSmash.Core;

static class Denge
{
    static Random rng = new Random(11);
    static double PHIT = .12; static bool USTA;

    static void Bot(Match M, float dt, double pHit)
    {
        if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .05)
        {
            double u = rng.NextDouble();
            if (u < .45) M.SwipeServe((float)(rng.NextDouble() * 80 - 40), -60, .6f);
            else if (u < .7) M.SwipeServe((float)(rng.NextDouble() * 40 - 20), -70, 1.6f);
            else M.SwipeServe((float)(rng.NextDouble() * 160 - 80), -200, 1.8f);
        }
        Reach R;
        if (M.ReachInfo(out R) && rng.NextDouble() < pHit)
        {
            double k = rng.NextDouble(); float sp = k < .2 ? .5f : k < .65 ? 1.2f : 2.2f;
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = rng.NextDouble() < .08 ? 120 : -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * 60), side = rng.NextDouble() < .5 ? -1 : 1, curved = rng.NextDouble() < .1 });
        }
        if (rng.NextDouble() < .02) M.OnTouchDown();
    }

    static GearStats Ekipman(int ep, string raket = "denge")
    {
        var g = new Gear(new BellekKayit(), 0); g.kariyerModu = true; g.eko.esit = false; g.raket = raket;
        string[] y = { "raket", "tel", "grip", "bileklik", "bant", "forma" };
        for (int i = 0; i < y.Length; i++) g.eko.sv[y[i]] = 1;
        for (int n = 6, i = 0; n < ep; n++, i++) { var k = y[i % y.Length]; if (g.eko.sv[k] < 10) g.eko.sv[k]++; }
        return g.Hesap();
    }

    // Oran ve ortalama sayı farkı.
    static double Oyna(Style st, int target, bool easy, GearStats gs, int n, int tohum, out double fark, out double imza)
    {
        int w = 0; double f = 0, im = 0;
        for (int m = 0; m < n; m++)
        {
            var M = new Match(tohum + m, new MatchOptions { easy = easy, target = target, stil = st, gs = gs }); int guard = 0;
            while (!M.over && guard++ < 60 * 60 * 40)
            {
                Bot(M, 1f / 60f, PHIT); M.Update(1f / 60f);
                foreach (var e in M.events) if (e.kind == Ev.Imza) im++;
                M.events.Clear();
            }
            if (M.sc[0] > M.sc[1]) w++; f += M.sc[0] - M.sc[1];
        }
        fark = f / n; imza = im / n; return (double)w / n;
    }

    // Turun 6 basamağının ortalama galibiyet oranı (kariyer uzunluğu, Kolay).
    static double TurOran(int t, GearStats gs, int n)
    {
        double top = 0; int say = 0; double f, i;
        for (int b = 0; b < KariyerVeri.BASAMAK; b++)
        {
            int g = KariyerVeri.Global(t, b); var k = KariyerVeri.Al(g); if (k == null) continue;
            top += Oyna(KariyerVeri.Rakip(g), k.uzunluk > 0 ? k.uzunluk : 5, true, gs, n, 5000 + g * 1000, out f, out i); say++;
        }
        return say > 0 ? top / say : 0;
    }

    static void Tablo(string baslik, string[] satir, Func<string, int, GearStats> ekipman, int n, double hedef)
    {
        Console.WriteLine(baslik + " (hücre = 6 basamak × " + n + " maç, Kolay, EP tur ortası)");
        var turlar = new List<int>(); for (int t = 0; t < KariyerVeri.Turlar.Length; t++) if (KariyerVeri.Turlar[t].Var) turlar.Add(t);
        var ust = "  " + "".PadRight(10); foreach (var t in turlar) ust += KariyerVeri.Turlar[t].ad.PadLeft(16); Console.WriteLine(ust);
        var en = new double[turlar.Count]; var ek = new double[turlar.Count];
        for (int j = 0; j < turlar.Count; j++) { en[j] = -1; ek[j] = 1e9; }
        foreach (var id in satir)
        {
            var l = "  " + id.PadRight(10);
            for (int j = 0; j < turlar.Count; j++)
            {
                int t = turlar[j]; var tr = KariyerVeri.Turlar[t];
                double o = TurOran(t, ekipman(id, (tr.epMin + tr.epMax) / 2), n) * 100;
                en[j] = Math.Max(en[j], o); ek[j] = Math.Min(ek[j], o); l += ("%" + o.ToString("0")).PadLeft(16);
            }
            Console.WriteLine(l);
        }
        var s = "  " + "fark".PadRight(10); for (int j = 0; j < turlar.Count; j++) s += ((en[j] - ek[j]).ToString("0.0") + (en[j] - ek[j] <= hedef ? " ok" : " FAZLA")).PadLeft(16);
        Console.WriteLine(s + "   (hedef ≤ " + hedef + ")");
    }

    // Tempo: yeni oyuncu, kariyeri sırayla oynar (galibiyet %60), her maç sonunda alabildiğini alır: önce yeni açılmış model
    // (ucuz olan), sonra en ucuz yükseltme. "Yeni şey" = yükseltme, model alımı, yeni basamak, yeni yıldız ödülü, seviye
    // atlayınca açılan model/tarz/renk/görünüm/unvan (tasarım §5.7 kural 1). Günde 8 maç (günün ilk galibiyeti).
    static void Tempo(int toplam)
    {
        var r = new Random(7); var g = new Gear(new BellekKayit(), 0); g.kariyerModu = true;
        int basamak = 0, enUzun = 0, bosluk = 0, enUzunBas = 0; var bitis = new List<string>(); var bosluklar = new int[12];
        var gun = new DateTime(2026, 10, 26);
        for (int m = 1; m <= toplam; m++)
        {
            if (m % 8 == 1 && m > 1) gun = gun.AddDays(1);
            bool kariyer = KariyerVeri.Var(basamak); var k = kariyer ? KariyerVeri.Al(basamak) : null;
            int hedef = k != null && k.uzunluk > 0 ? k.uzunluk : 5; bool won = r.NextDouble() < .6;
            bool ilk = won && kariyer; int yildiz = won && kariyer ? 1 + (r.NextDouble() < .4 ? 1 : 0) : 0;
            var o = g.Odul(won, ilk, hedef, true, 0, 3, gun, yildiz, 0);
            bool yeni = ilk || yildiz > 0;
            for (int L = o.once + 1; L <= o.sonra; L++) if (Karakter.Acilanlar(L).Count > 0) yeni = true;
            if (ilk) { basamak++; if (KariyerVeri.Final(basamak - 1)) bitis.Add(KariyerVeri.Turlar[KariyerVeri.TurOf(basamak - 1)].ad + " maç " + m + " Sv " + g.Seviye()); }
            for (int tur = 0; tur < 40; tur++)
            {
                string alY = null, alM = null; int ucuz = int.MaxValue;
                foreach (var y in Gear.MODEL_YUVA) foreach (var md in Gear.GEAR[y].Values) if (g.AlOlur(y, md.id) && md.fiyat < ucuz) { ucuz = md.fiyat; alY = y; alM = md.id; }
                if (alY != null) { g.Al(alY, alM); yeni = true; continue; }
                string yk = null; int yf = int.MaxValue;
                foreach (var y in Gear.YUVA) { int f, kp; if (g.YukseltOlur(y.k, out f, out kp) && f < yf) { yf = f; yk = y.k; } }
                if (yk == null) break; g.Yukselt(yk); yeni = true;
            }
            if (yeni) { bosluklar[Math.Min(11, bosluk)]++; bosluk = 0; } else { bosluk++; if (bosluk > enUzun) { enUzun = bosluk; enUzunBas = m; } }
            if (m == 14 || m == 40 || m == 80 || m == 130 || m == 200 || m == 400) Console.WriteLine("  maç " + m.ToString().PadLeft(3) + ": Sv " + g.Seviye() + ", basamak " + basamak + "/24, EP " + EP(g) + ", Tüy " + g.eko.tuy + ", en uzun boşluk şimdiye dek " + enUzun);
        }
        foreach (var b in bitis) Console.WriteLine("  bitti: " + b);
        var d = "  boşluk dağılımı (yeni şeyden önce kaç sessiz maç): "; for (int i = 0; i < bosluklar.Length; i++) if (bosluklar[i] > 0) d += i + (i == 11 ? "+" : "") + "→" + bosluklar[i] + "  "; Console.WriteLine(d);
        Console.WriteLine("  en uzun sessiz aralık " + enUzun + " maç (maç " + (enUzunBas - enUzun + 1) + "–" + enUzunBas + "), hedef ≤ 4: " + (enUzun <= 4 ? "ok" : "FAZLA"));
    }
    static int EP(Gear g) { int s = 0; foreach (var v in g.eko.sv.Values) s += v; return s; }

    static int Main(string[] a)
    {
        if (a.Length > 0 && a[0] == "model")
        {
            int nm = a.Length > 1 ? int.Parse(a[1]) : 40; var ids = new List<string>(Gear.GEAR["raket"].Keys);
            Tablo("Raket modeli × tur", ids.ToArray(), (id, ep) => Ekipman(ep, id), nm, 8); return 0;
        }
        if (a.Length > 0 && a[0] == "tarz")
        {
            int nm = a.Length > 1 ? int.Parse(a[1]) : 40; var ids = new List<string>(); foreach (var t in Karakter.Tarzlar) ids.Add(t.id);
            Tablo("Oyun tarzı × tur", ids.ToArray(), (id, ep) => Karakter.Uygula(Ekipman(ep), id), nm, 5); return 0;
        }
        if (a.Length > 0 && a[0] == "tempo") { Console.WriteLine("Tempo simülasyonu (galibiyet %60, Kolay, 5/11/21 basamak uzunluğu, günde 8 maç)"); Tempo(a.Length > 1 ? int.Parse(a[1]) : 400); return 0; }
        int n = a.Length > 0 ? int.Parse(a[0]) : 200; if (a.Length > 1) PHIT = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture); USTA = a.Length > 2 && a[2] == "usta";
        Console.WriteLine("Kariyer dengesi, basamak başına " + n + " maç (bot pHit " + PHIT + "). Oran = galibiyet %, ± = ort. sayı farkı.");
        foreach (var easy in USTA ? new[] { true, false } : new[] { true })
        {
            Console.WriteLine(easy ? "== Kolay ==" : "== Usta ==");
            for (int t = 0; t < KariyerVeri.Turlar.Length; t++)
            {
                var tr = KariyerVeri.Turlar[t]; if (!tr.Var) continue;
                int epA = tr.epMin, epO = (tr.epMin + tr.epMax) / 2; var gA = Ekipman(epA); var gO = Ekipman(epO);
                Console.WriteLine("  " + tr.ad + " (EP " + epA + " / " + epO + ")");
                for (int b = 0; b < KariyerVeri.BASAMAK; b++)
                {
                    int g = KariyerVeri.Global(t, b); var k = KariyerVeri.Al(g); int hedef = k.uzunluk > 0 ? k.uzunluk : 5;
                    double fA, fO, iA, iO;
                    double oA = Oyna(KariyerVeri.Rakip(g), hedef, easy, gA, n, 5000 + g * 1000, out fA, out iA);
                    double oO = Oyna(KariyerVeri.Rakip(g), hedef, easy, gO, n, 5000 + g * 1000, out fO, out iO);
                    Console.WriteLine(string.Format("    {0}. {1,-26} {2,-7} g {3:0.00} {4,2} sayı  EP{5,-2} %{6,3:0} ({7:+0.0;-0.0})  EP{8,-2} %{9,3:0} ({10:+0.0;-0.0}){11}",
                        b + 1, k.yer, k.stil, k.g, hedef, epA, oA * 100, fA, epO, oO * 100, fO, k.patron ? "  imza/maç " + iO.ToString("0.0") : ""));
                }
            }
        }
        // Henüz turda olmayan tarzlar: Asya Turu'nun ortası (g 1.15) ile bilgi amaçlı.
        Console.WriteLine("== Bilgi: tura girmemiş tarzlar, g 1.15, 5 sayı, Kolay, EP 18 ==");
        foreach (var id in new[] { "vex", "aria", "nova", "smasci", "duvar", "file" })
        {
            var st = KariyerVeri.Stil[id].Clone(); st.hiz *= 1.15f; st.hata /= 1.15f; st.imza = null; double f, i;
            double o = Oyna(st, 5, true, Ekipman(18), n, 90000, out f, out i);
            Console.WriteLine(string.Format("    {0,-7} %{1,3:0} ({2:+0.0;-0.0})", id, o * 100, f));
        }
        return 0;
    }
}
