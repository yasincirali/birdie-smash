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
//   mono denge.exe dril [n] [beceri]  antrenman görevleri: odaklı botla yıldız oranları (hedef ★★★ %30–70)
//   mono denge.exe ustayolu [n]  Usta yolu basamakları, Usta botuyla (hedef %40–55, final %30–45)
//   mono denge.exe olc <tarz> <g,..> <tepki> <hucum> <sayı> <n> [imza] [usta]  tek tarz, birkaç g (ayar turu)
//   mono denge.exe tempo         maç maç ekonomi simülasyonu: "yeni şey" olayları arası en uzun boşluk (hedef ≤ 4 maç)
// Model/tarz kipinde her hücre turun 6 basamağının ortalamasıdır (aynı tohumlar: hücreler arası fark yalnız modelden/tarzdan
// gelir), ekipman turun önerilen EP'sinin ortası. Bot modelin/tarzın hilesini bilerek kullanmaz (slice %10, rastgele): ölçülen
// "kör oyuncuya" üstünlük; yetenekli oyuncunun Kesici/Aldatıcı'dan alacağı pay bunun üstündedir (TestFlight ayarlar).
// Tempo kipi maç oynamaz: galibiyet olasılığı sabit (%60, tasarım §5.7 varsayımı), Odul() gerçek ekonomi fonksiyonu.
using System;
using System.Collections.Generic;
using System.Linq;
using BirdieSmash.Core;

static class Denge
{
    static Random rng = new Random(11);
    static double PHIT = .12; static bool USTA;
    // 2026-10-26 Usta botu: Usta yolunu oynayan, kariyeri bitirmiş oyuncu. Kolay botundan isabetli (kaydırma topun merkezine
    // en çok 1 R: Usta'da R dışı "poor" olur, Kolay'da olmaz) ve pencereyi daha iyi yakalar (kare başına .12). Koşu UstaKos.
    static double USTA_PHIT = .12, USTA_DIST = 40;

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
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = rng.NextDouble() < .08 ? 120 : -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * (M.easy ? 60 : USTA_DIST)), side = rng.NextDouble() < .5 ? -1 : 1, curved = rng.NextDouble() < .1 });
        }
        if (rng.NextDouble() < .02) M.OnTouchDown();
        if (!M.easy) UstaKos(M);
    }
    // 2026-10-26 Usta botu: Usta'da oyuncu koşar (RunTo). İnsan gibi: rakip vurduktan ~0,25 sn sonra topun düşeceği yere
    // (±30 cm hata), kendi vuruşundan sonra kortun ortasına döner. Mutlak oran bu varsayımlara bağlı; Usta g'si buna göre.
    static Shuttle kosTop; static float kosGecik;
    static void UstaKos(Match M)
    {
        if (M.s == null || M.s.dead) return;
        if (M.s != kosTop)
        {
            kosTop = M.s; kosGecik = (float)(.25 + rng.NextDouble() * .12);
            if (M.s.from == "me") M.RunTo(Court.MID, Court.NET + 3.3f, .8f);
        }
        if (M.s.from == "op" && kosGecik > 0 && M.clock - M.opHitAt >= kosGecik && M.icpt != null && M.icpt.who == "me")
        { kosGecik = -1; M.RunTo(M.icpt.x + (float)(rng.NextDouble() - .5) * .6f, M.icpt.y + (float)(rng.NextDouble() - .5) * .6f, 1.2f); }
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
                Bot(M, 1f / 60f, easy ? PHIT : USTA_PHIT); M.Update(1f / 60f);
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

    // Tempo: yeni oyuncu, kariyeri sırayla oynar, her etkinlikten sonra alabildiğini alır: önce yeni açılmış model (ucuz
    // olan), sonra en ucuz yükseltme. "Yeni şey" = yükseltme, model alımı, yeni basamak, yeni yıldız ödülü, seviye atlayınca
    // açılan model/tarz/renk/görünüm/unvan (tasarım §5.7 kural 1). Günde 8 etkinlik (günün ilk galibiyeti).
    // 2026-10-26 adım 7–9 (eski kip "tempo eski" olarak duruyor, kıyas için): gerçek Ilerleme.Isle (görev yıldızı kayıpta da,
    // %45; farkla kazanma galibiyetlerin %40'ı), 4 tur, Günün maçı (günün ilk etkinliği; kazanırsa +40/+30 ve "yeni şey",
    // kaybederse gün içinde bir kez daha dener), antrenman görevi (her 4. etkinlik, yıldızı eksik ilk görev; yıldız dağılımı
    // Denge dril ölçümünden, beceri 1,25: ★ %99, ★★ %92, ★★★ %53), dört tur bitince Usta yolu (galibiyet %50, her galibiyet
    // yeni basamak; görev yıldızı %45, kayıpta da). Kariyer galibiyeti tur başına TUR_ORAN. Boşluk etkinlik sayısıyla ölçülür (görev de bir etkinlik; ~1 dk).
    // Kariyer galibiyet olasılığı tur başına: Denge tablolarının (pHit .07, n 150) tur ortalamaları, yuvarlak.
    static readonly double[] TUR_ORAN = { .7, .6, .52, .5 };
    static int TEMPO_TOHUM = 7; static string SonIz;
    static int Tempo(int toplam, bool eski, bool sessiz = false)
    {
        var r = new Random(TEMPO_TOHUM); var izler = new List<string>(); string enUzunIz = ""; var g = new Gear(new BellekKayit(), 0); g.kariyerModu = true; var p = new Ilerleme();
        int basamak = 0, enUzun = 0, bosluk = 0, enUzunBas = 0, gunlukKazan = 0, drilYildiz = 0, ustaN = 0, drilDene = 0; var bitis = new List<string>(); var bosluklar = new int[12];
        var gun = new DateTime(2026, 10, 26); int gunlukDene = 0; string gunTarih = null;
        var drils = BirdieSmash.Core.Dril.Liste;
        for (int m = 1; m <= toplam; m++)
        {
            if (m % 8 == 1 && m > 1) gun = gun.AddDays(1);
            if (gunTarih != Gunluk.Tarih(gun)) { gunTarih = Gunluk.Tarih(gun); gunlukDene = 0; }
            bool yeni = false; OdulSonuc o = null; string ne = "kariyer"; int eksik = -1;
            DrilTanim dd = null; if (!eski && m % 4 == 0) foreach (var d in drils) if (p.DrilYildiz(d.id) < 3) { dd = d; break; }
            if (eski)
            {
                bool kariyer = KariyerVeri.Var(basamak) && KariyerVeri.TurOf(basamak) < 2; var k = kariyer ? KariyerVeri.Al(basamak) : null;
                int hedef = k != null && k.uzunluk > 0 ? k.uzunluk : 5; bool won = r.NextDouble() < .6;
                bool ilk = won && kariyer; int yildiz = won && kariyer ? 1 + (r.NextDouble() < .4 ? 1 : 0) : 0;
                o = g.Odul(won, ilk, hedef, true, 0, 3, gun, yildiz, 0); yeni = ilk || yildiz > 0;
                if (ilk) { basamak++; if (KariyerVeri.Final(basamak - 1)) bitis.Add(KariyerVeri.Turlar[KariyerVeri.TurOf(basamak - 1)].ad + " maç " + m + " Sv " + g.Seviye()); }
            }
            else if (!p.GunlukAlindi(gunTarih) && gunlukDene < 2 && p.Mac >= 3)
            {   // Günün maçı (ilk 3 maçtan sonra görünür: ilk açılışta Oyna kartında yok)
                ne = "gunluk"; gunlukDene++; var gm = Gunluk.Bugun(gun); bool won = r.NextDouble() < .55;
                bool ilkGun = won && p.GunlukKazan(gunTarih); if (ilkGun) gunlukKazan++;
                o = g.Odul(won, false, gm.uzunluk, gm.deg.id != "usta", 0, 3, gun, 0, 0, ilkGun ? Gunluk.OdulTuy : 0, ilkGun ? Gunluk.OdulXp : 0, "Günün maçı");
                p.MacSay(2); yeni = ilkGun;
            }
            else if (dd != null)
            {
                ne = "dril"; drilDene++; double u = r.NextDouble(); int y = u < .53 ? 3 : u < .92 ? 2 : u < .99 ? 1 : 0;
                int ny = p.DrilKaydet(dd.id, y); drilYildiz += ny; o = g.DrilOdul(ny); yeni = ny > 0;
            }
            else if (!p.UstaAcik || (bosluk >= 2 && bosluk % 2 == 0 && (eksik = EksikYildiz(p)) >= 0))
            {   // kariyer: sıradaki basamak; Usta'da iki sessiz etkinlikten sonra eksik yıldızı olan ilk basamak (oyuncu takılınca
                // haritadaki boş yıldıza döner, sonra Usta'ya; sırayla: içerik orada, kural yeniden oynamaya izin veriyor)
                int gi = p.UstaAcik ? eksik : p.Siradaki(); if (p.UstaAcik) ne = "yildiz"; var k = KariyerVeri.Al(gi); int hedef = k.uzunluk > 0 ? k.uzunluk : 5; bool won = r.NextDouble() < TUR_ORAN[KariyerVeri.TurOf(gi)];
                bool fark = won && r.NextDouble() < .4; int ben = won ? hedef : hedef - 2, rakip = won ? (fark ? hedef / 2 : hedef - 2) : hedef;
                var res = p.Isle(gi, won, r.NextDouble() < .45, ben, rakip, hedef);
                o = g.Odul(won, res.ilkGalibiyet, hedef, true, res.yeniYildiz > 0 ? 1 : 0, 3, gun, res.yeniYildiz, 0); p.MacSay(2);
                yeni = res.ilkGalibiyet || res.yeniYildiz > 0;
                if (res.ilkGalibiyet) basamak++;
                if (res.turBitti) bitis.Add(KariyerVeri.Turlar[KariyerVeri.TurOf(gi)].ad + " maç " + m + " Sv " + g.Seviye());
            }
            else
            {
                ne = "usta"; var b = UstaYolu.Basamak(p.Usta); bool won = r.NextDouble() < .5, gv = r.NextDouble() < .45;
                int uy = p.UstaIsle(p.Usta, won, gv); if (won) ustaN++;
                o = g.Odul(won, won, b.uzunluk, false, gv ? 1 : 0, 3, gun, uy, 0); p.MacSay(2); yeni = uy > 0;
            }
            for (int L = o.once + 1; L <= o.sonra; L++) if (Karakter.Acilanlar(L).Count > 0) yeni = true;
            for (int tur = 0; tur < 40; tur++)
            {
                string alY = null, alM = null; int ucuz = int.MaxValue;
                foreach (var y in Gear.MODEL_YUVA) foreach (var md in Gear.GEAR[y].Values) if (g.AlOlur(y, md.id) && md.fiyat < ucuz) { ucuz = md.fiyat; alY = y; alM = md.id; }
                if (alY != null) { g.Al(alY, alM); yeni = true; continue; }
                string yk = null; int yf = int.MaxValue;
                foreach (var y in Gear.YUVA) { int f, kp; if (g.YukseltOlur(y.k, out f, out kp) && f < yf) { yf = f; yk = y.k; } }
                if (yk == null) break; g.Yukselt(yk); yeni = true;
            }
            if (yeni) { bosluklar[Math.Min(11, bosluk)]++; bosluk = 0; izler.Clear(); } else { izler.Add(ne); bosluk++; if (bosluk > enUzun) { enUzun = bosluk; enUzunBas = m; enUzunIz = string.Join(",", izler.ToArray()); } }
            if (sessiz) continue;
            if (m == 14 || m == 40 || m == 80 || m == 130 || m == 200 || m == 300 || m == 400 || m == 600)
                Console.WriteLine("  etkinlik " + m.ToString().PadLeft(3) + ": Sv " + g.Seviye() + ", basamak " + (eski ? basamak : Kazanilan(p)) + "/24" + (eski ? "" : ", Usta " + p.Usta + ", görev ★ " + p.AntrenmanYildiz + "/24, Günün maçı " + gunlukKazan) + ", EP " + EP(g) + ", Tüy " + g.eko.tuy + ", en uzun boşluk şimdiye dek " + enUzun);
        }
        if (sessiz) { SonIz = enUzunBas + ": " + enUzunIz; return enUzun; }
        foreach (var b in bitis) Console.WriteLine("  bitti: " + b);
        var dg = "  boşluk dağılımı (yeni şeyden önce kaç sessiz etkinlik): "; for (int i = 0; i < bosluklar.Length; i++) if (bosluklar[i] > 0) dg += i + (i == 11 ? "+" : "") + "→" + bosluklar[i] + "  "; Console.WriteLine(dg);
        Console.WriteLine("  en uzun sessiz aralık " + enUzun + " etkinlik (" + (enUzunBas - enUzun + 1) + "–" + enUzunBas + "), hedef ≤ 4: " + (enUzun <= 4 ? "ok" : "FAZLA") + (eski ? "" : "   (görev denemesi " + drilDene + "; sessiz etkinlikler: " + enUzunIz + ")"));
        return enUzun;
    }
    // Eksik yıldızlı basamak: önce görev yıldızı eksik olanlar (kayıpta da alınır), yoksa fark yıldızı eksikler; içlerinden
    // rastgele (aynı zor basamağa takılıp kalmasın, oyuncu da haritada başka boş yıldıza bakar).
    static readonly Random eksikR = new Random(3);
    static int EksikYildiz(Ilerleme p)
    {
        var gl = new List<int>(); var fl = new List<int>();
        for (int i = 0; i < KariyerVeri.TUR * KariyerVeri.BASAMAK; i++) { if (!KariyerVeri.Var(i)) continue; int m = p.Maske(i); if ((m & Ilerleme.GOREV) == 0) gl.Add(i); else if ((m & Ilerleme.FARK) == 0) fl.Add(i); }
        var l = gl.Count > 0 ? gl : fl; return l.Count == 0 ? -1 : l[eksikR.Next(l.Count)];
    }
    static int Kazanilan(Ilerleme p) { int n = 0; for (int i = 0; i < KariyerVeri.TUR * KariyerVeri.BASAMAK; i++) if (KariyerVeri.Var(i) && p.Kazanildi(i)) n++; return n; }

    // ---- 2026-10-26 antrenman görevleri (adım 7): "odaklı oyuncu" botu, görevin istediği vuruşu dener ----
    // İnsan gibi hatalı: hız (vuruş türünü seçen) ve yön gürültülü, isabet (dist) yarı-normal, zamanlama penceresi içinde
    // rastgele. Mutlak oran botun ayarına bağlıdır; hedef ★★★ %30–70 (tasarım §3.6: "üç yıldız çaba ister, imkânsız değil"),
    // ★ çoğu oyuncuda (≥ %80). BECERI çarpanı gürültüyü ölçekler (1 = iyi oyuncu, 1,25 = ölçü noktası, 1,5 = acemi).
    // Ölçüm (n 150): beceri 1,25'te sekiz görevin ★★★ oranı %43–59; 1'de %55–91, 1,5'te %19–55.
    static double BECERI = 1.25;
    static double N01() { double u = 1 - rng.NextDouble(), v = rng.NextDouble(); return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v); }
    static float splitGecik = -1; static Shuttle sonTop;
    static void DrilBot(Match M, DrilTanim d)
    {
        double b = BECERI;
        if (d.olcu == "servis")
        {
            if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .05)
            {
                if (d.servis == "flick") M.SwipeServe((float)(N01() * 25 * b), (float)(-60 + N01() * 15 * b), (float)Math.Max(.1, 1.4 + N01() * .3 * b));
                else M.SwipeServe((float)(N01() * 25 * b), -60, (float)Math.Max(.1, .55 + N01() * .3 * b));
            }
            return;
        }
        if (M.s != null && M.s != sonTop && M.s.from == "op") { sonTop = M.s; splitGecik = (float)(.2 + N01() * .09 * b); }
        if (d.olcu == "split" && splitGecik > 0 && M.s != null && M.s.from == "op" && M.clock - M.opHitAt >= splitGecik) { M.OnTouchDown(); splitGecik = -1; }
        Reach R;
        if (!M.ReachInfo(out R) || rng.NextDouble() >= .15) return;
        float sp = 1.2f, dy = -200, dx = (float)(N01() * 50 * b); bool cur = false;
        // 2026-10-30 vuruş laboratuvarı: yön isteyen görevde bot parmağını hedefe eğer (düz: vuruş noktasının hizası, çapraz:
        // karşı köşe); dx px → tx = orta + dx/200 · 2,4 m (ShotPlan).
        if (d.yon != null) { float hx = M.s != null ? M.s.x : Court.MID, tx = d.yon == "duz" ? hx : hx < Court.MID ? Court.SR - .8f : Court.SL + .8f; dx = (float)((tx - Court.MID) / 2.4f * 200 + N01() * 45 * b); }
        switch (d.id)
        {
            case "atak_clear": sp = (float)(1.55 + N01() * .12 * b); break;
            case "capraz_drop": case "file_dibi": case "capraz_file": sp = (float)(.45 + N01() * .2 * b); break;
            case "duz_smac": case "capraz_smac": case "dik_smac": sp = (float)(2.1 + N01() * .3 * b); break;
            case "lift": sp = (float)(1.2 + N01() * .3 * b); break;
            case "drive": sp = (float)(2.0 + N01() * .25 * b); break;
            case "push": sp = (float)(1.2 + N01() * .25 * b); break;
            case "blok": dy = 120; break;
            case "cesit": { double u = rng.NextDouble(); sp = (float)((u < .33 ? .45 : u < .66 ? 1.3 : 2.0) + N01() * .2 * b); break; }
            case "derin": sp = (float)(1.3 + N01() * .3 * b); break;
            case "drop": sp = (float)(.45 + N01() * .2 * b); break;
            case "kose":
                {
                    var a = M.DrilAlan(); float tx = a != null ? (a[0] + a[1]) / 2 : Court.MID; dy = -220;
                    dx = (float)((tx - Court.MID) / 2.4f * 220 + N01() * 45 * b); sp = (float)(2.1 + N01() * .3 * b); break;
                }
            case "aldat": cur = rng.NextDouble() < .8; sp = (float)(.9 + N01() * .4 * b); break;
            case "savunma": if (rng.NextDouble() < .5) dy = 120; else sp = (float)(1.2 + N01() * .3 * b); break;
            case "kill": sp = (float)(2.0 + N01() * .35 * b); break;
            default: sp = (float)(1.2 + N01() * .3 * b); break;
        }
        M.SwipeShot(new SwipeInput { dx = dx, dy = dy, speed = Math.Max(.1f, sp), R = 40, dist = (float)Math.Abs(N01() * 22 * b), side = rng.NextDouble() < .5 ? -1 : 1, curved = cur });
    }

    static void DrilTablo(int n)
    {
        Console.WriteLine("Antrenman görevleri, görev başına " + n + " deneme (odaklı bot, beceri " + BECERI + "). İsabet / 12 dağılımı, yıldız oranı.");
        foreach (var d in BirdieSmash.Core.Dril.Liste)
        {
            var dag = new int[d.top + 1]; double sure = 0; int takil = 0;
            for (int m = 0; m < n; m++)
            {
                var M = new Match(7000 + m, new MatchOptions { easy = true, ogr = true, dril = d, target = 5 }); int guard = 0; sonTop = null; splitGecik = -1;
                while (!M.ogr.bitti && guard++ < 60 * 300) { DrilBot(M, d); M.Update(1f / 60f); M.events.Clear(); }
                if (!M.ogr.bitti) takil++; dag[Math.Min(d.top, M.ogr.isabet)]++; sure += guard / 60.0;
            }
            var y = new double[4]; for (int i = 0; i <= d.top; i++) y[d.Yildiz(i)] += dag[i];
            double y1 = (y[1] + y[2] + y[3]) / n, y2 = (y[2] + y[3]) / n, y3 = y[3] / n;
            var l = "  " + d.id.PadRight(8) + " eşik " + string.Join("/", Array.ConvertAll(d.esik, x => x.ToString())).PadRight(8) + string.Format(" ★ %{0,3:0}  ★★ %{1,3:0}  ★★★ %{2,3:0}{3}  süre {4:0} sn{5}  |", y1 * 100, y2 * 100, y3 * 100, y3 >= .3 && y3 <= .7 ? " ok" : " ??", sure / n, takil > 0 ? " TAKILDI " + takil : "");
            for (int i = 0; i <= d.top; i++) l += " " + dag[i]; Console.WriteLine(l);
        }
    }
    // 2026-10-30 vuruş laboratuvarı tanısı: bir görevin her topunun ölçümü (ad, tepe, file payı, düşüş) ve kaçan topların
    // nedeni dağılımı (görev koşulu ayarı için: hangi koşul botu tutuyor).
    static void LabTani(string id, int n)
    {
        var d = BirdieSmash.Core.Dril.Bul(id); var neden = new Dictionary<string, int>(); var adlar = new Dictionary<string, int>(); var fi = typeof(Match).GetField("drNeden", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tepe = new List<float>(); var gec = new List<float>(); var yDus = new List<float>(); var hyL = new List<float>(); var hzL = new List<float>();
        for (int m = 0; m < n; m++)
        {
            var M = new Match(9000 + m, new MatchOptions { easy = true, ogr = true, dril = d, target = 5 }); int guard = 0, no = 0; sonTop = null; splitGecik = -1;
            while (!M.ogr.bitti && guard++ < 60 * 300)
            {
                DrilBot(M, d); M.Update(1f / 60f); M.events.Clear();
                if (M.drOlcumNo != no) { no = M.drOlcumNo; var o = M.drOlcum; var t = M.s; string k = (string)fi.GetValue(M) ?? (o.hedef ? "HEDEF" : o.file ? "file" : o.disari ? "dışarı" : "?");
                    neden[k] = neden.ContainsKey(k) ? neden[k] + 1 : 1; adlar[o.ad + "/" + t.vurus + "/" + t.type] = adlar.ContainsKey(o.ad + "/" + t.vurus + "/" + t.type) ? adlar[o.ad + "/" + t.vurus + "/" + t.type] + 1 : 1;
                    tepe.Add(t.zTepe); if (!float.IsNaN(t.gecPay)) gec.Add(t.gecPay); yDus.Add(t.y); hyL.Add(t.hy - Court.NET); hzL.Add(t.hz); }
            }
        }
        Func<List<float>, string> q = l => { if (l.Count == 0) return "-"; l.Sort(); return string.Format("p10 {0:0.00} p50 {1:0.00} p90 {2:0.00}", l[l.Count / 10], l[l.Count / 2], l[l.Count * 9 / 10]); };
        Console.WriteLine(id + ": nedenler " + string.Join(", ", neden.Select(kv => kv.Key + " " + kv.Value)));
        Console.WriteLine("  adlar " + string.Join(", ", adlar.Select(kv => kv.Key + " " + kv.Value)));
        Console.WriteLine("  tepe " + q(tepe) + " | file payı " + q(gec) + " | düşüş y " + q(yDus) + " | vuruş fileye " + q(hyL) + " | vuruş z " + q(hzL));
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
        if (a.Length > 1 && a[0] == "lab") { LabTani(a[1], a.Length > 2 ? int.Parse(a[2]) : 20); return 0; }
        if (a.Length > 0 && a[0] == "dril") { if (a.Length > 2) BECERI = double.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture); DrilTablo(a.Length > 1 ? int.Parse(a[1]) : 100); return 0; }
        // 2026-10-26 ayar ölçümü: mono denge.exe olc <tarz> <g1,g2,..> <tepki> <hucum> <sayı> <n> [imza] [usta]
        // (tek tarzın birkaç g'deki galibiyet oranı; tur g'lerini ayarlamak için, pHit .07, EP tur ortası 30)
        if (a.Length > 0 && a[0] == "olc")
        {
            PHIT = .07; var ci = System.Globalization.CultureInfo.InvariantCulture; bool imzaVar = Array.IndexOf(a, "imza") > 0, ustaK = Array.IndexOf(a, "usta") > 0;
            foreach (var gs in a[2].Split(','))
            {
                float gg = float.Parse(gs, ci); var st = KariyerVeri.Stil[a[1]].Clone(); st.hiz *= gg; st.hata /= gg; st.tepki *= float.Parse(a[3], ci); st.hucum *= float.Parse(a[4], ci);
                st.patron = imzaVar && st.imza != null; if (!st.patron) st.imza = null; double f, i;
                double o = Oyna(st, int.Parse(a[5]), !ustaK, Ekipman(30), int.Parse(a[6]), 5000, out f, out i);
                Console.WriteLine(string.Format("  {0,-7} g {1:0.00} tepki {2} hucum {3} {4} sayı{5}{6}: %{7:0} ({8:+0.0;-0.0})", a[1], gg, a[3], a[4], a[5], imzaVar ? " imza" : "", ustaK ? " USTA" : "", o * 100, f));
            }
            return 0;
        }
        // 2026-10-26 Usta yolu: mono denge.exe ustayolu [n]  (Usta botu, EP 48; örnek basamaklar: ilk, final, tavan)
        if (a.Length > 0 && a[0] == "ustayolu")
        {
            int nm = a.Length > 1 ? int.Parse(a[1]) : 80; var u = KariyerVeri.Usta;
            Console.WriteLine("Usta yolu, basamak başına " + nm + " maç (Usta botu: pHit " + USTA_PHIT + ", isabet ≤ R, koşu UstaKos), g " + u.g + " + " + u.adim + "/6 basamak, tavan " + u.tavan);
            foreach (int un in new[] { 0, 1, 2, 3, 4, 5, 11, 23, 35, 59 })
            {
                var b = UstaYolu.Basamak(un); double f, i; double o = Oyna(UstaYolu.Rakip(un), b.uzunluk, false, Ekipman(48), nm, 20000 + un * 100, out f, out i);
                Console.WriteLine(string.Format("  {0,3}. {1,-26} {2,-7} g {3:0.00} {4,2} sayı{5}  %{6,3:0} ({7:+0.0;-0.0})", un + 1, b.yer, b.stil, b.g, b.uzunluk, b.final ? " final" : "      ", o * 100, f));
            }
            return 0;
        }
        if (a.Length > 0 && a[0] == "tempo")
        {
            bool es = Array.IndexOf(a, "eski") > 0; int nt = 400; foreach (var x in a) { int v; if (int.TryParse(x, out v)) nt = v; }
            Console.WriteLine(es ? "Tempo (eski: Tur 1–2 yalnız, galibiyet %60, günde 8 maç)" : "Tempo (adım 7–9: 4 tur + Usta yolu, Günün maçı, antrenman görevleri; kariyer %70/60/52/50, Usta %50, Günün maçı %55, günde 8 etkinlik)");
            Tempo(nt, es);
            if (!es)
            {   // tohum dağılımı: en uzun boşluk tek koşunun kuyruğudur; 20 tohumda kaçında ≤ 4
                var say = new SortedDictionary<int, int>(); for (int t = 1; t <= 20; t++) { TEMPO_TOHUM = 100 + t; int e = Tempo(nt, false, true); if (e >= 7) Console.WriteLine("    tohum " + TEMPO_TOHUM + ": " + e + " @ " + SonIz); int c; say.TryGetValue(e, out c); say[e] = c + 1; }
                var sd = "  20 tohumda en uzun boşluk: "; foreach (var kv in say) sd += kv.Key + " etkinlik ×" + kv.Value + "  "; Console.WriteLine(sd);
            }
            return 0;
        }
        int n = a.Length > 0 ? int.Parse(a[0]) : 200; if (a.Length > 1) PHIT = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture); USTA = a.Length > 2 && a[2] == "usta"; string turSec = a.Length > 3 ? a[3] : null;   // 2026-10-26: "3,4" yalnız o turlar (1'den sayılır)
        Console.WriteLine("Kariyer dengesi, basamak başına " + n + " maç (bot pHit " + PHIT + "). Oran = galibiyet %, ± = ort. sayı farkı.");
        foreach (var easy in USTA ? new[] { true, false } : new[] { true })
        {
            Console.WriteLine(easy ? "== Kolay ==" : "== Usta ==");
            for (int t = 0; t < KariyerVeri.Turlar.Length; t++)
            {
                var tr = KariyerVeri.Turlar[t]; if (!tr.Var || turSec != null && Array.IndexOf(turSec.Split(','), (t + 1).ToString()) < 0) continue;
                int epA = tr.epMin, epO = (tr.epMin + tr.epMax) / 2; var gA = Ekipman(epA); var gO = Ekipman(epO);
                Console.WriteLine("  " + tr.ad + " (EP " + epA + " / " + epO + ")");
                for (int b = 0; b < KariyerVeri.BASAMAK; b++)
                {
                    int g = KariyerVeri.Global(t, b); var k = KariyerVeri.Al(g); int hedef = k.uzunluk > 0 ? k.uzunluk : 5;
                    double fA = 0, fO, iA = 0, iO;
                    // Tur seçiliyse (ayar turu) yalnız EP ortası ölçülür: süre yarıya iner; EP etkisi ölçümde gürültü düzeyinde.
                    double oA = turSec != null ? -1 : Oyna(KariyerVeri.Rakip(g), hedef, easy, gA, n, 5000 + g * 1000, out fA, out iA); if (turSec != null) fA = 0;
                    double oO = Oyna(KariyerVeri.Rakip(g), hedef, easy, gO, n, 5000 + g * 1000, out fO, out iO);
                    Console.WriteLine(string.Format("    {0}. {1,-26} {2,-7} g {3:0.00} {4,2} sayı  EP{5,-2} %{6,3:0} ({7:+0.0;-0.0})  EP{8,-2} %{9,3:0} ({10:+0.0;-0.0}){11}",
                        b + 1, k.yer, k.stil, k.g, hedef, epA, oA * 100, fA, epO, oO * 100, fO, k.patron ? "  imza/maç " + iO.ToString("0.0") : ""));
                }
            }
        }
        if (turSec != null) return 0;
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
