// Kariyer denge ölçümü (komple-oyun tasarımı §3.2 testere dişi; adım 2–3, 2026-10-26). Unity'siz:
//   mcs -nologo -nowarn:219 -out:denge.exe tool/unity_test/Denge.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs
//   mono denge.exe [maç sayısı]          (depo kökünden; veri tool/veri'den okunur)
// Neden ayrı araç, BotMac'te değil: BotMac her koşuda çalışan doğruluk testi (saniyeler); denge ölçümü binlerce maç ister
// ve bir eşikle kırılmaz, sayıları rapor eder (eğri tasarım tablosuna göre okunur, g değerleri kariyer.json'da ayarlanır).
// Bot: BotMac'teki insan benzeri bot (pHit .12, rastgele kaydırma). Mutlak oran botun ustalığına bağlıdır; anlamlı olan
// basamaklar arası eğridir (her basamak bir öncekinden biraz zor, turun 1. basamağı önceki finalden kolay = testere).
// Ekipman: turun önerilen EP aralığının alt ucu ve ortası (EP = 6 yuvanın seviye toplamı, Gear.KSv ile aynı ölçek),
// kariyer modunda (eşit şartlar kapalı). Uzunluk: basamağın verisi; Tur 1'de oyuncu seçer, 5 ile ölçülür.
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

    static GearStats Ekipman(int ep)
    {
        var g = new Gear(new BellekKayit(), 0); g.kariyerModu = true; g.eko.esit = false;
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

    static int Main(string[] a)
    {
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
