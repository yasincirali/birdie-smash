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
        Antrenman(); TamAntrenman(); Kariyer(); Ekipman(); Devirler(); Ozetler(); Gezintiler();
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
        string a = Career.AgainLabel(true, true, 1), b = Career.AgainLabel(true, false, 1), c = Career.AgainLabel(false, true, 0), d = Career.AgainLabel(true, true, 6);
        Console.WriteLine("düğmeler: '" + a + "' / '" + b + "' / '" + c + "' / şampiyon '" + d + "'");
        if (a != "Sıradaki: Okul turnuvası" || b != "Tekrar dene" || c != "Bir maç daha" || d != "Bir maç daha") Fail("kariyer düğme etiketi");
        int k = 0; string note; Career.Apply(ref k, 0, true, out note); if (k != 1 || !note.Contains("Okul turnuvası")) Fail("kariyer basamağı açılmadı: " + note);
        Career.Apply(ref k, 1, false, out note); if (k != 1 || !note.Contains("bekliyor")) Fail("kayıpta basamak notu: " + note);
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
