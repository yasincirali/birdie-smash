// Unity'siz doğrulama: Core'u mono ile derleyip bot maçları koşar.
//   mcs -out:/tmp/botmac.exe tool/unity_test/BotMac.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono /tmp/botmac.exe
// Kontroller (2026-10-24, HTML fb1b245 kuralları): maçlar biter, skor kurala uyar (5/11/21; 7/15/30 tavan), üç servis
// türü de atılır, iki oyuncunun nefesi düşer ve dolar, sayı arası 0,7 sn (rekorda 1,3), hazırlıkta temas karesi temastan
// önce gösterilmez, sıçrayarak smaç temasta tepede, hamlede gövde yatmaz, ilk açılış antrenmanı 3 adım + 5 sayılık maç,
// kariyer düğme etiketi, NaN yok.
// 2026-10-25: HTML → Unity ilerleme devri (Core/Devir: bir kez, bayrak, normalize), maç sonu özeti (Core/Ozet: öncelik
// sırası, ipucu), mağaza puanı koşulu, boşta duruşun nefese bağlı hızı.
// 2026-10-24 tam geçiş: ekipman çarpanları HTML gsHesap ile aynı (node ile ölçülen değerler), ağır raket smacı gerçekten
// hızlandırır, görev olayı gelir, tam antrenman 15 adım (iki bilgi + antrenör çağrısı) ve ödül yuvarlaması HTML gibi.
// 2026-10-26 kariyer haritası ve imza vuruşları (adım 2–3): veri tek kaynak (tool/veri = Resources/veri), Tur 1 eski altı
// basamakla birebir, yıldız kuralları ve kayıt taşıması (Core/Ilerleme), basamağın sabit görevi, imza vuruşu: yalnız
// patronda, maçta ≤ 4, aralarında ≥ 4 sayı, hazırlık ≥ 0,4 sn (Kolay 0,6), her imza görülür ve cevaplanabilir.
// Çalıştırma: depo kökünden (KariyerVeri tool/veri'yi çalışma klasöründen yukarı arar).
using System;
using System.Collections.Generic;
using System.Linq;
using BirdieSmash.Core;

static class BotMac
{
    static int fails = 0;
    static void Fail(string m) { Console.WriteLine("HATA: " + m); fails++; }
    static readonly Random rng = new Random(7);

    // insan benzeri bot: servis türünü rastgele seçer, pencere açıkken çoğu orta, bazen sert/yavaş kaydırır
    static void Bot(Match M, float dt, double pHit = .12)
    {
        if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .05)
        {
            double u = rng.NextDouble();
            if (u < .45) M.SwipeServe((float)(rng.NextDouble() * 80 - 40), -60, .6f);          // yavaş = kısa
            else if (u < .7) M.SwipeServe((float)(rng.NextDouble() * 40 - 20), -70, 1.6f);     // kısa ve sert = flick
            else M.SwipeServe((float)(rng.NextDouble() * 160 - 80), -200, 1.8f);               // uzun ve hızlı = yüksek
        }
        Reach R;
        if (M.ReachInfo(out R) && rng.NextDouble() < pHit)
        {
            double k = rng.NextDouble(); float sp = k < .2 ? .5f : k < .65 ? 1.2f : 2.2f;
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = rng.NextDouble() < .08 ? 120 : -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * 60), side = rng.NextDouble() < .5 ? -1 : 1, curved = rng.NextDouble() < .1 });
        }
        if (rng.NextDouble() < .02) M.OnTouchDown();
    }

    static int Main()
    {
        var clipSeen = new Dictionary<string, int>(); var serveSeen = new Dictionary<string, int>();
        int nets = 0, cords = 0, points = 0, meWins = 0, maxRally = 0, hits = 0, prepEarly = 0, prepFrames = 0, jumpN = 0, opJumpN = 0, tiltBad = 0;
        float minMe = 1, minOp = 1, jumpMin = 9, opJumpMin = 9; var gaps = new List<float>(); var gapsRekor = new List<float>();
        var styles = new Dictionary<string, int>(); int tired = 0, gorevN = 0;
        int[] targets = { 5, 11, 21 };
        for (int m = 0; m < 18; m++)
        {
            int target = targets[m % 3]; Style st = m % 4 == 3 ? Match.KariyerRakip(m % 6) : null;
            var M = new Match(100 + m, new MatchOptions { easy = m % 5 != 4, target = target, stil = st, bestRally = 6 });
            styles[M.stil.isim] = styles.ContainsKey(M.stil.isim) ? styles[M.stil.isim] + 1 : 1;
            float t = 0, pointAt = -1; bool lastRekor = false; int guard = 0; const float dt = 1f / 60f;
            while (!M.over && guard++ < 60 * 60 * 40)
            {
                Bot(M, dt);
                var sBefore = M.s; bool svBefore = M.me.svAnim != null || M.op.svAnim != null;
                M.Update(dt); t += dt;
                foreach (var a in new[] { M.me, M.op })
                {
                    string c; float ph; M.PickClip(a, a == M.me, out c, out ph);
                    int n; clipSeen.TryGetValue(c, out n); clipSeen[c] = n + 1;
                    if (float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(ph)) { Fail("NaN oyuncu"); goto next; }
                    // hazırlık: temas karesi ancak temasta (sw>0 dalı); hazırlık dalında faz temas kesrinin altında kalmalı
                    float hit; if (a.sw <= 0 && a.svAnim == null && a.prepA > .08f && a.prepClip == c && Match.CLIP_HIT.TryGetValue(c, out hit)) { prepFrames++; int nn; if (Match.CLIP_N.TryGetValue(c, out nn) && ph * nn >= Math.Floor(hit * nn)) prepEarly++; }
                    if (a.dive > 0 && a.yat && a == M.me) tiltBad++;   // hamlede (uzanma) gövde dik: yat yalnız rakibin kıl payı kurtarışında
                }
                minMe = Math.Min(minMe, M.me.nefes); minOp = Math.Min(minOp, M.op.nefes);
                if (M.s != null && (float.IsNaN(M.s.x) || float.IsNaN(M.s.z))) { Fail("NaN top"); goto next; }
                foreach (var e in M.events)
                {
                    if (e.kind == Ev.Net) nets++;
                    if (e.kind == Ev.Cord) cords++;
                    if (e.kind == Ev.Gorev) gorevN++;
                    if (e.kind == Ev.Text && e.text == "Rakip yoruldu!") tired++;
                    if (e.kind == Ev.Hit)
                    {
                        hits++;
                        if (M.s != null && M.s.serve) { string k = e.type == "drop" ? "kisa" : (M.s.vz > 0 && Math.Atan2(M.s.vz, Math.Sqrt(M.s.vx * M.s.vx + M.s.vy * M.s.vy)) < .7 ? "flick" : "yuksek"); serveSeen[k] = serveSeen.ContainsKey(k) ? serveSeen[k] + 1 : 1; }
                        if (e.who == "me" && e.big) { jumpN++; jumpMin = Math.Min(jumpMin, Match.JumpH(M.me)); }
                        if (e.who == "op" && M.op.clip == "jump") { opJumpN++; opJumpMin = Math.Min(opJumpMin, Match.JumpH(M.op)); }
                    }
                    if (e.kind == Ev.Point) { points++; maxRally = Math.Max(maxRally, (int)e.k); pointAt = t; lastRekor = !M.kisaBekle; }
                    if (e.kind == Ev.Serve && pointAt >= 0) { (lastRekor ? gapsRekor : gaps).Add(t - pointAt); pointAt = -1; }
                }
                M.events.Clear();
            }
        next:
            if (!M.over) { Fail("maç bitmedi " + m + " skor " + M.sc[0] + "-" + M.sc[1]); continue; }
            int a0 = M.sc[0], b0 = M.sc[1], cap = target == 21 ? 30 : target == 11 ? 15 : 7;
            bool ok = (Math.Max(a0, b0) >= target && Math.Abs(a0 - b0) >= 2) || Math.Max(a0, b0) == cap;
            if (!ok) Fail("skor kurala aykırı " + a0 + "-" + b0 + " hedef " + target);
            if (a0 > b0) meWins++;
            Console.WriteLine("maç " + m.ToString().PadLeft(2) + (M.easy ? " kolay " : " usta  ") + target.ToString().PadLeft(2) + " sayı " + a0 + "-" + b0 + "  rakip " + M.RakipAd(true) + (M.stil.kariyer >= 0 ? " · " + Match.KARIYER[M.stil.kariyer].yer : "") + "  " + t.ToString("0") + " s, en uzun ralli " + M.maxRally);
        }
        Console.WriteLine("sayı " + points + ", en uzun ralli " + maxRally + ", file " + nets + ", bant yuvarlanması " + cords + ", vuruş " + hits + ", kazanılan maç " + meWins + "/18");
        Console.WriteLine("servis türleri: " + string.Join(", ", serveSeen.Select(k => k.Key + "=" + k.Value)));
        Console.WriteLine("rakipler: " + string.Join(", ", styles.Select(k => k.Key + "=" + k.Value)));
        Console.WriteLine("nefes en düşük: ben " + minMe.ToString("0.00") + ", rakip " + minOp.ToString("0.00") + ", 'Rakip yoruldu!' " + tired + " kez");
        if (gaps.Count > 0) Console.WriteLine("sayı → servis dizilişi: medyan " + gaps.OrderBy(x => x).ElementAt(gaps.Count / 2).ToString("0.00") + " s (" + gaps.Count + " sayı)" + (gapsRekor.Count > 0 ? ", rekor/maç sonu " + gapsRekor.OrderBy(x => x).ElementAt(gapsRekor.Count / 2).ToString("0.00") + " s" : ""));
        Console.WriteLine("hazırlık karesi: " + prepFrames + " kare, temastan önce temas karesi " + prepEarly);
        Console.WriteLine("sıçrayarak smaç: ben " + jumpN + " (temasta yükseklik en az " + (jumpN > 0 ? jumpMin.ToString("0.00") : "-") + "), rakip " + opJumpN + " (" + (opJumpN > 0 ? opJumpMin.ToString("0.00") : "-") + ")");
        var keys = clipSeen.Keys.ToList(); keys.Sort();
        Console.WriteLine("klipler: " + string.Join(", ", keys.Select(k => k + "=" + clipSeen[k])));
        foreach (var need in new[] { "idle", "serve_bh", "serve_flick", "split", "smash", "clear", "low", "drive", "bh_over", "bh_low", "chasse_r", "chasse_l", "fwd", "jump" })
            if (!clipSeen.ContainsKey(need)) Fail("hiç görülmeyen klip: " + need);
        foreach (var need in new[] { "kisa", "flick", "yuksek" }) if (!serveSeen.ContainsKey(need)) Fail("servis türü yok: " + need);
        if (maxRally < 4) Fail("ralli çok kısa");
        if (nets == 0) Fail("file hiç çalışmadı");
        if (prepEarly > 0) Fail("hazırlıkta temas karesi temastan önce göründü: " + prepEarly);
        if (jumpN > 0 && jumpMin < .95f) Fail("sıçrayarak smaç temasta tepede değil: " + jumpMin);
        if (opJumpN > 0 && opJumpMin < .95f) Fail("rakip sıçrayarak smaç temasta tepede değil: " + opJumpMin);
        if (tiltBad > 0) Fail("hamlede gövde yattı");
        if (minMe > .9f || minOp > .95f) Fail("nefes düşmüyor");
        if (gaps.Count == 0 || Math.Abs(gaps.OrderBy(x => x).ElementAt(gaps.Count / 2) - .7f) > .05f) Fail("sayı arası 0,7 sn değil");

        if (gorevN == 0) Fail("görev olayı hiç gelmedi");
        Console.WriteLine("görev tamamlandı: " + gorevN + " kez");
        Antrenman(); TamAntrenman(); Kariyer(); Ekipman(); Devirler(); Ozetler(); Gezintiler(); Veri(); Ilerlemeler(); Imzalar();
        Console.WriteLine(fails == 0 ? "TAMAM" : "HATA " + fails);
        return fails == 0 ? 0 : 1;
    }

    // İlk açılış: 3 adım (kısa servis, clear, smaç), sonra 5 sayılık Kolay ilk maç.
    static void Antrenman()
    {
        var M = new Match(9, new MatchOptions { easy = false, ogr = true, ogrKisa = true });
        if (M.ogr.liste.Count != 3 || !M.easy) Fail("ilk antrenman 3 adım ve Kolay olmalı");
        var adlar = M.ogr.liste.Select(a => a.bas).ToArray(); int guard = 0, durN = 0; const float dt = 1f / 60f;
        while (!M.ogr.macaGec && guard++ < 60 * 400)
        {
            var a = M.OgrA();
            if (a != null && a.servis != null && M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null) M.SwipeServe(0, -60, .6f);
            if (a != null && M.ogr.dur)
            {   // oyun durdu: istenen hareket (clear = orta hız yukarı, smaç = hızlı)
                durN++; float sp = a.tip[0] == "clear" ? 1.2f : 2.3f;
                M.SwipeShot(new SwipeInput { dx = 0, dy = -200, speed = sp, R = 40, dist = 5, side = 1 });
            }
            M.Update(dt); M.events.Clear();
        }
        if (!M.ogr.macaGec) { Fail("antrenman bitmedi, adım " + M.ogr.i); return; }
        var I = new Match(10, new MatchOptions { easy = true, ilk = true, target = 21 });
        if (I.target != 5) Fail("ilk maç 5 sayı değil");
        Console.WriteLine("antrenman: " + string.Join(" → ", adlar) + ", oyun " + durN / 1 + " karede durdu, " + (guard / 60f).ToString("0") + " s; ilk maç " + I.target + " sayı " + (I.easy ? "Kolay" : "Usta"));
    }

    // Kariyer: kazanınca "Sıradaki: {yer}", kaybedince "Tekrar dene", serbest maçta "Bir maç daha".
    static void Kariyer()
    {
        // 2026-10-26: Tur 1 bitince sıradaki Asya Turu'nun ilk basamağı (genel indeks 6); oynanacak yeni basamak yoksa -1.
        string a = Career.AgainLabel(true, true, 1), b = Career.AgainLabel(true, false, 1), c = Career.AgainLabel(false, true, 0), d = Career.AgainLabel(true, true, 6), e = Career.AgainLabel(true, true, -1);
        Console.WriteLine("düğmeler: '" + a + "' / '" + b + "' / '" + c + "' / Tur 1 sonrası '" + d + "' / hepsi bitti '" + e + "'");
        if (a != "Sıradaki: Okul turnuvası" || b != "Tekrar dene" || c != "Bir maç daha" || d != "Sıradaki: Jakarta" || e != "Bir maç daha") Fail("kariyer düğme etiketi");
        var p = new Ilerleme(); var r = p.Isle(0, true, false, 5, 3, 5); if (!r.ilkGalibiyet || !r.not.Contains("Okul turnuvası") || p.Siradaki() != 1) Fail("kariyer basamağı açılmadı: " + r.not);
        r = p.Isle(1, false, false, 2, 5, 5); if (p.Siradaki() != 1 || !r.not.Contains("bekliyor")) Fail("kayıpta basamak notu: " + r.not);
    }

    // Tam antrenman (menüden): 15 adım; 4. adım "Nefes" ve "Smaç gücü" bilgi adımı, son adım antrenör çağrısı.
    static void TamAntrenman()
    {
        var M = new Match(11, new MatchOptions { ogr = true });
        int bilgi = M.ogr.liste.Count(a => a.bilgi != null), drill = M.ogr.liste.Count(a => a.drill);
        Console.WriteLine("tam antrenman: " + M.ogr.liste.Count + " adım, " + bilgi + " bilgi, " + drill + " antrenör çağrısı; ilk adımlar " + string.Join(" → ", M.ogr.liste.Take(4).Select(a => a.bas)));
        if (M.ogr.liste.Count != 15 || bilgi != 2 || drill != 1 || M.ogr.liste[3].bilgi != "nefes" || !M.ogr.liste[14].drill) Fail("tam antrenman adımları HTML ile aynı değil");
    }

    // Ekipman: (1) gsHesap çarpanları HTML'den node ile ölçülen değerlerle aynı; (2) aynı tohum ve bot girdisiyle ağır raket +
    // tam seviye smaç çıkış hızını artırır; (3) 21 sayılık yenilgi ödülü 15·3,5 = 52,5 → 53 Tüy (JS yuvarlaması).
    static void Ekipman()
    {
        var g = new Gear(new BellekKayit(), 0); g.raket = "agir"; g.tel = 22; g.bileklik = "agir"; g.bant = "odak"; g.forma = "beyaz";
        foreach (var k in g.eko.sv.Keys.ToList()) g.eko.sv[k] = 10;
        // 2026-10-26: raket taban katkısı yalnız nötr değere, raket eksisi Sv 10'da %40 küçülür (Core/Gear.Hesap; tasarım §4.5).
        // HTML dondurulduğu için (yeni özellik yalnız Unity'de) beklenen değerler artık HTML'den değil bu kuraldan; ilk dört
        // (güç, kontrol, drive, tatlı) değişti, gerisi eski HTML ölçümüyle aynı. Eski HTML: 1.31551, 1.11042, .95645, 1.35557.
        var r = g.Hesap(); float[] html = { 1.25287f, 1.04006f, .94267f, 1.31231f, .82788f, .78f, 1, 4, .85f, 1.06f, 1.04f, 1.448f, 1, .86957f };
        float[] cs = { r.guc, r.kontrol, r.hiz, r.tatli, r.ton, r.kafa, r.kenar, r.akis, r.yavas, r.rakip, r.kosu, r.enerji, r.toparla, r.yorK };
        for (int i = 0; i < cs.Length; i++) if (Math.Abs(cs[i] - html[i]) > 1e-4) Fail("ekipman çarpanı " + i + ": " + cs[i] + " ≠ HTML " + html[i]);
        float notr = SmacHizi(null), agir = SmacHizi(r);
        Console.WriteLine("ekipman: smaç çıkış hızı nötr " + notr.ToString("0.0") + " m/s, ağır raket + 10. seviye " + agir.ToString("0.0") + " m/s (guc ×" + r.guc.ToString("0.00") + ")");
        if (!(agir > notr * 1.1f)) Fail("ağır raket smacı hızlandırmadı");
        var o = new Gear(new BellekKayit(), 0); o.eko.gun = "x"; var od = o.Odul(false, false, 21, true, 0, 0, DateTime.Now);
        if (od.tuy != 53) Fail("21 sayılık yenilgi ödülü 53 Tüy olmalı (JS Math.round): " + od.tuy);
    }
    // Menü gezintisi (Core/Gezinti, 2026-10-26): sayfa yığını, geri sırası, sekme değişince yığın boşalır.
    static void Gezintiler()
    {
        var g = new Gezinti();
        if (g.sekme != Sekme.Oyna || g.Geri()) Fail("gezinti Oyna'da başlamalı, geri bir şey yapmamalı");
        g.Ac(Gezinti.AYAR); g.Ac(Gezinti.NASIL); g.Ac(Gezinti.NASIL);
        if (g.Derinlik != 2 || g.Ust != Gezinti.NASIL) Fail("aynı sayfa iki kez yığına girdi: " + g.Derinlik);
        g.Geri(); if (g.Ust != Gezinti.AYAR) Fail("geri Ayarlar'a dönmeli");
        g.Geri(); if (g.Derinlik != 0 || g.sekme != Sekme.Oyna) Fail("geri yığını boşaltmalı");
        g.SekmeSec(Sekme.Kariyer); g.Ac(Gezinti.HIZLI); if (!Gezinti.AltSayfa(g.Ust)) Fail("Hızlı maç alt sayfa olmalı");
        if (!g.SekmeSec(Sekme.Soyunma) || g.Derinlik != 0) Fail("sekme değişince yığın boşalmalı");
        if (!g.Geri() || g.sekme != Sekme.Oyna) Fail("Oyna dışındaki sekmeden geri Oyna'ya dönmeli");
        Console.WriteLine("gezinti: yığın, geri sırası ve sekme değişimi tamam");
    }
    // HTML → Unity devri: kabuğun yazdığı HTML değerleri bir kez kabul edilir, bayrak konur, ikinci açılış dokunmaz.
    static void Devirler()
    {
        var k = new BellekKayit();
        k.Set("tuytop_v2", "{\"rally\":23.0,\"wins\":7,\"kariyer\":9,\"gorev\":-2,\"fazla\":1}"); k.Set("tuytop_len", "len11"); k.Set("tuytop_gear", "bozuk{"); k.Set("tuytop_puan", "5");
        k.Set("tuytop_ekonomi", "{\"tuy\":340}");
        int n = Devir.Kabul(k);
        if (n != 5) Fail("devralınan anahtar sayısı 5 olmalı: " + n);
        var b = Json.Obj(Json.Parse(k.Get("tuytop_v2")));
        if (b == null || Json.Num(b, "rally", -1) != 23 || Json.Num(b, "wins", -1) != 7 || Json.Num(b, "kariyer", -1) != Match.KARIYER.Length || Json.Num(b, "gorev", -1) != 0 || !Json.Bool(b, "sampiyon", false) || b.ContainsKey("fazla"))
            Fail("tuytop_v2 normalize edilmedi: " + k.Get("tuytop_v2"));
        if (k.Get("tuytop_gear") != "{}") Fail("bozuk tuytop_gear {} olmalı: " + k.Get("tuytop_gear"));
        if (k.Get("tuytop_ekonomi") != "{\"tuy\":340}") Fail("geçerli ekonomi değişti");
        if (k.Get("tuytop_puan") != "3") Fail("puan sayacı 0..3: " + k.Get("tuytop_puan"));
        if (k.Get(Devir.BAYRAK) == null) Fail("devir bayrağı konmadı");
        k.Set("tuytop_v2", "{\"rally\":99}"); if (Devir.Kabul(k) != -1 || k.Get("tuytop_v2") != "{\"rally\":99}") Fail("devir ikinci kez çalıştı");
        var bos = new BellekKayit(); if (Devir.Kabul(bos) != 0 || bos.Get(Devir.BAYRAK) == null) Fail("HTML verisi yokken de bayrak konmalı");
        var g = new Gear(k, 7); if (g.eko == null) Fail("devirden sonra Gear açılmadı");
        Console.WriteLine("devir: 5 HTML anahtarı kabul, bayrak, ikinci açılış dokunmadı");
    }
    // Maç sonu özeti ve puan koşulu (HTML ozetKart/ozetIpucu/puanBak ile aynı kural).
    static void Ozetler()
    {
        var M = new Match(1, new MatchOptions { easy = true, target = 5 });
        M.maxRally = 5; M.smashes = 3; M.kills = 2; M.saves = 1; M.outsBen = 3;
        var k = Ozet.Kartlar(M); object a;
        if (k.Count != 2 || k[0].ad != "Smaç" || k[1].ad != "File vuruşu") Fail("özet kartları öncelik sırasında değil: " + string.Join(",", k.Select(x => x.ad)));
        if (Ozet.Ipucu(M, false, out a) != "{0} vuruşun dışarı gitti: çizgiden biraz içeriye nişan al." || (int)a != 3) Fail("yenilgi ipucu dışarı vuruş olmalı");
        M.maxRally = 12; k = Ozet.Kartlar(M); if (k[0].ad != "En uzun ralli" || k[0].deger != 12) Fail("8+ ralli ilk kart olmalı");
        var B = new Match(2, new MatchOptions { easy = true, target = 5 }); B.maxRally = 3; k = Ozet.Kartlar(B);
        if (k.Count != 1 || k[0].ad != "En uzun ralli") Fail("boş maçta tek kart en uzun ralli olmalı");
        if (Ozet.Ipucu(B, false, out a) != "Dışarı gidecek topu bırak: sayı senin olur.") Fail("yenilgi ipucu bırakma olmalı");
        string w = Ozet.Ipucu(B, true, out a); if (B.gorevler.Count > 0 && w != "Bu maçta kalan görev: {0}") Fail("galibiyet ipucu kalan görev olmalı: " + w);
        if (!Ozet.PuanIste(true, true, 1, 0) || !Ozet.PuanIste(true, false, 3, 2) || Ozet.PuanIste(true, false, 4, 0) || Ozet.PuanIste(false, true, 3, 0) || Ozet.PuanIste(true, true, 3, 3))
            Fail("puan koşulu: basamak ya da 3. galibiyet, en çok 3");
        // boşta duruş: yorgun oyuncunun fazı daha hızlı ilerler
        var I = new Match(3, new MatchOptions { easy = true, target = 5 }); I.op.nefes = .1f; float p0 = I.me.idlePh, q0 = I.op.idlePh;
        for (int i = 0; i < 30; i++) { I.me.nefes = 1; I.op.nefes = .1f; I.Update(1 / 60f); I.events.Clear(); }
        float dm = (I.me.idlePh - p0 + 1) % 1, dn = (I.op.idlePh - q0 + 1) % 1;
        if (!(dn > dm * 1.5f)) Fail("yorgunken boşta duruş hızlanmadı: " + dm + " / " + dn);
        Console.WriteLine("özet: " + string.Join(" + ", Ozet.Kartlar(M).Select(x => x.ad + " " + x.deger)) + "; puan koşulu tamam; boşta hız dolu/yorgun " + dm.ToString("0.000") + "/" + dn.ToString("0.000"));
    }
    // Veri tek kaynak (Ek A.4): tool/veri ile Unity kopyası aynı; Tur 1 eski koddaki altı basamakla birebir (kayıt bu sırayı sayar).
    static void Veri()
    {
        foreach (var ad in new[] { "kariyer", "stil" })
        {
            string a = System.IO.File.ReadAllText("tool/veri/" + ad + ".json"), b = System.IO.File.ReadAllText("unity/BirdieSmash/Assets/Resources/veri/" + ad + ".json");
            if (a != b) Fail("veri kopyası farklı: " + ad + ".json (node tool/veri_kopyala.js)");
        }
        string[] stil = { "file", "smasci", "duvar", "file", "smasci", "duvar" }, yer = { "Mahalle salonu", "Okul turnuvası", "İl şampiyonası", "Bölge finali", "Ulusal lig", "Şampiyonlar kupası" }, saha = { "okul", "okul", "salon", "salon", "turnuva", "turnuva" };
        float[] g = { .88f, .94f, 1, 1.07f, 1.13f, 1.2f };
        for (int i = 0; i < 6; i++) { var k = Match.KARIYER[i]; if (k.stil != stil[i] || k.yer != yer[i] || k.saha != saha[i] || Math.Abs(k.g - g[i]) > 1e-6 || k.uzunluk != 0) Fail("Tur 1 basamağı " + i + " değişti"); }
        var b0 = Match.STIL["smasci"]; if (b0.isim != "Blaze" || b0.ustP[0] != .62f || b0.hata != 1.2f || b0.ev != 3.2f || b0.yorK != 1.3f) Fail("Blaze değerleri veriden farklı okundu");
        var d0 = Match.STIL["duvar"]; if (d0.hiz != 1.08f || d0.hata != .75f || d0.yorK != .7f) Fail("Rocco değerleri veriden farklı okundu");
        var f0 = Match.STIL["file"]; if (f0.geriK != 1.8f || f0.ev != 2.2f) Fail("Mika değerleri veriden farklı okundu");
        if (KariyerVeri.Turlar.Length != 4 || !KariyerVeri.Turlar[1].Var || KariyerVeri.Turlar[1].b[5].uzunluk != 11 || !KariyerVeri.Turlar[1].b[5].patron) Fail("Asya Turu verisi");
        foreach (var t in KariyerVeri.Turlar) foreach (var k in t.b) if (Match.GorevBul(k.gorev) == null || !Match.STIL.ContainsKey(k.stil)) Fail("bilinmeyen görev/tarz: " + k.yer);
        var M = new Match(5, new MatchOptions { stil = Match.KariyerRakip(1), target = 5 });
        if (M.BasamakGorevi == null || M.BasamakGorevi.id != "karsila" || M.gorevler.Count != 3 || M.gorevler.Select(x => x.id).Distinct().Count() != 3) Fail("basamağın sabit görevi ilk görev olmalı");
        if (new Match(5, new MatchOptions { target = 5 }).BasamakGorevi != null) Fail("serbest maçta sabit görev olmamalı");
        if (Match.KariyerRakip(4).imza != null || Match.KariyerRakip(5).imza != "duvarblok" || Match.KariyerRakip(11).imza != "mermi") Fail("imza yalnız patronda");
        Console.WriteLine("veri: tool/veri = Resources/veri, Tur 1 birebir, " + KariyerVeri.Turlar.Count(t => t.Var) + " tur, " + Match.STIL.Count + " tarz, sabit görev tamam");
    }

    // Yıldızlar, açılış ve kayıt taşıması (Core/Ilerleme).
    static void Ilerlemeler()
    {
        var p = Ilerleme.Oku(null, 0, false);
        if (p.Siradaki() != 0 || !p.Acik(0) || p.Acik(1) || p.TurAcik(1) || p.ToplamYildiz != 0) Fail("boş kayıt: yalnız ilk basamak açık");
        var r = p.Isle(0, true, false, 5, 3, 5); if (r.yeniYildiz != 1 || r.maske != Ilerleme.KAZAN) Fail("galibiyet tek yıldız (fark yok: rakip 3 > 2)");
        r = p.Isle(0, true, true, 5, 2, 5); if (r.yeniYildiz != 2 || r.ilkGalibiyet || p.Yildiz(0) != 3) Fail("görev + fark yeni iki yıldız, ilk galibiyet değil");
        r = p.Isle(1, false, true, 3, 5, 5); if (r.yeniYildiz != 1 || p.Kazanildi(1) || p.Maske(1) != Ilerleme.GOREV) Fail("kayıpta görev yıldızı sayılır, basamak geçilmez");
        if (Ilerleme.FarkSiniri(5) != 2 || Ilerleme.FarkSiniri(11) != 5 || Ilerleme.FarkSiniri(21) != 10) Fail("fark sınırı 5→2, 11→5, 21→10");
        r = p.Isle(1, true, false, 11, 5, 11); if (r.maske != (Ilerleme.KAZAN | Ilerleme.FARK | Ilerleme.GOREV)) Fail("11 sayıda rakip 5: farkla kazanılmış");
        // taşıma: eski kariyer sayısı Tur 1'in ilk n basamağı
        var e3 = Ilerleme.Oku(null, 3, false); if (e3.Tur1Gecilen() != 3 || e3.Siradaki() != 3 || !e3.Acik(3) || e3.Acik(4) || e3.ToplamYildiz != 3) Fail("kariyer=3 taşıması");
        var e6 = Ilerleme.Oku(null, 6, true); if (e6.Tur1Gecilen() != 6 || !e6.TurAcik(1) || e6.Siradaki() != 6 || e6.HepsiBitti) Fail("kariyer=6: Tur 1 tamam, Asya Turu açık");
        var es = Ilerleme.Oku(null, 2, true); if (es.Tur1Gecilen() != 6) Fail("şampiyon bayrağı Tur 1'i tamamlar");
        // birleştirme: yeni kayıt + eski build'in ilerlettiği sayı; bilinmeyen alan korunur
        var j = Ilerleme.Oku("{\"v\":1,\"tur\":[[7,3,0,0,0,0],[0,0,0,0,0,0]],\"unvan\":\"x\"}", 4, false);
        if (j.Tur1Gecilen() != 4 || j.Maske(0) != 7 || j.Maske(1) != 3) Fail("eski sayı yeni kayıtla birleşmedi");
        var j2 = Ilerleme.Oku(j.Yaz(), 0, false); if (j2.Yaz() != j.Yaz() || !j.Yaz().Contains("\"unvan\":\"x\"")) Fail("kayıt gidiş-dönüş / bilinmeyen alan: " + j.Yaz());
        var bz = Ilerleme.Oku("{\"tur\":[[0,0,1,0,0,0]]}", 0, false); if (bz.Tur1Gecilen() != 3) Fail("sıra tutarlılığı: geçilen basamağın öncekiler de geçilmiş");
        if (Ilerleme.Oku("bozuk{", 1, false).Tur1Gecilen() != 1) Fail("bozuk kayıt eski sayıya düşmeli");
        // Tur 2 finali: veri yok (Tur 3) → hepsi bitti, Tur 3 "yakında"
        var f = Ilerleme.Oku(null, 6, true); for (int b = 0; b < 6; b++) f.Isle(6 + b, true, false, 5, 4, 5);
        if (!f.HepsiBitti || f.TurAcik(2) || !f.TurYakinda(2) || f.Siradaki() != 11) Fail("Asya Turu bitince Tur 3 yakında");
        var tb = Ilerleme.Oku(null, 5, false).Isle(5, true, false, 5, 4, 5); if (!tb.turBitti || !tb.not.Contains("Asya Turu açıldı")) Fail("Tur 1 finali notu: " + tb.not);
        // ödül: yeni yıldız +20 Tüy, okunan imza +5 XP
        var g0 = new Gear(new BellekKayit(), 0); g0.eko.gun = "x"; var o0 = g0.Odul(true, false, 5, true, 0, 0, DateTime.Now);
        var g1 = new Gear(new BellekKayit(), 0); g1.eko.gun = "x"; var o1 = g1.Odul(true, false, 5, true, 0, 0, DateTime.Now, 2, 3);
        if (o1.tuy - o0.tuy != 40 || o1.xp - o0.xp != 15) Fail("yıldız/imza ödülü: " + (o1.tuy - o0.tuy) + " Tüy, " + (o1.xp - o0.xp) + " XP");
        Console.WriteLine("ilerleme: yıldız kuralları, taşıma (3 / 6 / şampiyon / birleştirme / bozuk), tur açılışı ve ödül tamam");
    }

    // İmza vuruşları: patron maçları bot ile; sıklık, aralık, hazırlık süresi (maç saatiyle, M.clock), cevap.
    static void Imzalar()
    {
        var tur = new Dictionary<string, int>(); var okunan = new Dictionary<string, int>(); int maclar = 0, enCok = 0, hata = 0, normalImza = 0; float enKisa = 9, enKisaKolay = 9;
        var bossler = new List<Style> { Match.KariyerRakip(5), Match.KariyerRakip(11) };
        foreach (var id in new[] { "smasci", "file", "vex", "aria" }) { var st = Match.STIL[id].Clone(); st.kariyer = -1; st.patron = true; bossler.Add(st); }   // imzası olan diğer tarzlar
        for (int m = 0; m < 60; m++)
        {
            var st = bossler[m % bossler.Count]; bool kolay = m % 3 != 2;
            var M = new Match(700 + m, new MatchOptions { easy = kolay, target = 11, stil = st }); maclar++;
            int say = 0, sonPuan = -99; float imzaClock = -1; string bekle = null; int guard = 0;
            while (!M.over && guard++ < 60 * 60 * 30)
            {
                Bot(M, 1f / 60f, .14); M.Update(1f / 60f);
                foreach (var e in M.events)
                {
                    if (e.kind == Ev.Imza)
                    {
                        say++; int pu = M.sc[0] + M.sc[1]; if (pu - sonPuan < Match.IMZA_ARA) { Fail("imzalar arası " + (pu - sonPuan) + " sayı"); hata++; } sonPuan = pu;
                        tur[e.type] = tur.ContainsKey(e.type) ? tur[e.type] + 1 : 1; imzaClock = M.clock; bekle = e.type;
                    }
                    if (e.kind == Ev.Hit && bekle != null && (bekle == "duvarblok" ? e.who == "me" : e.who == "op"))
                    {
                        float h = M.clock - imzaClock; if (kolay) enKisaKolay = Math.Min(enKisaKolay, h); else enKisa = Math.Min(enKisa, h); bekle = null;
                    }
                    if (e.kind == Ev.Point) bekle = null;
                    if (e.kind == Ev.Okudun) okunan[e.type] = okunan.ContainsKey(e.type) ? okunan[e.type] + 1 : 1;
                }
                M.events.Clear();
            }
            if (say > Match.IMZA_AZAMI) Fail("maçta " + say + " imza (en çok 4)"); enCok = Math.Max(enCok, say);
        }
        for (int m = 0; m < 6; m++)
        {   // patron olmayan kariyer basamağı ve serbest maç: hiç imza yok
            var M = new Match(900 + m, new MatchOptions { easy = true, target = 5, stil = m < 3 ? Match.KariyerRakip(m * 2) : null }); int guard = 0;
            while (!M.over && guard++ < 60 * 60 * 20) { Bot(M, 1f / 60f); M.Update(1f / 60f); foreach (var e in M.events) if (e.kind == Ev.Imza) normalImza++; M.events.Clear(); }
        }
        Console.WriteLine("imza: " + maclar + " patron maçı, türler " + string.Join(", ", tur.Select(k => k.Key + "=" + k.Value)) + "; okunan " + string.Join(", ", okunan.Select(k => k.Key + "=" + k.Value))
            + "; maçta en çok " + enCok + "; en kısa hazırlık Usta " + enKisa.ToString("0.00") + " s, Kolay " + enKisaKolay.ToString("0.00") + " s");
        foreach (var id in new[] { "yildirim", "duvarblok", "tuldrop", "mermi", "golgeflick" }) if (!tur.ContainsKey(id)) Fail("imza hiç görülmedi: " + id);
        if (enKisa < Match.IMZA_HAZIRLIK - 1e-3f) Fail("hazırlık 0,4 sn'den kısa: " + enKisa);
        if (enKisaKolay < Match.IMZA_HAZIRLIK + .2f - 1e-3f) Fail("Kolay'da hazırlık 0,6 sn'den kısa: " + enKisaKolay);
        if (okunan.Count == 0) Fail("hiç imza okunmadı (cevap çalışmıyor)");
        if (normalImza > 0) Fail("patron olmayan maçta imza: " + normalImza);
    }

    static float SmacHizi(GearStats gs)
    {
        var rr = new Random(5); float sum = 0; int n = 0; const float dt = 1f / 60f;
        for (int m = 0; m < 4; m++)
        {
            var M = new Match(300 + m, new MatchOptions { easy = true, target = 11, gs = gs }); int guard = 0;
            while (!M.over && guard++ < 60 * 60 * 20)
            {
                if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rr.NextDouble() < .05) M.SwipeServe(0, -200, 1.8f);
                Reach R; if (M.ReachInfo(out R) && rr.NextDouble() < .15) M.SwipeShot(new SwipeInput { dx = (float)(rr.NextDouble() * 100 - 50), dy = -250, speed = 2.3f, R = 40, dist = 5, side = 1 });
                M.Update(dt);
                foreach (var e in M.events) if (e.kind == Ev.Hit && e.who == "me" && e.type == "smash" && M.s != null) { sum += (float)Math.Sqrt(M.s.vx * M.s.vx + M.s.vy * M.s.vy + M.s.vz * M.s.vz); n++; }
                M.events.Clear();
            }
        }
        return n > 0 ? sum / n : 0;
    }
}
