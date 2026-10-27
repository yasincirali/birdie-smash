// Unity'siz doğrulama: Core'u mono ile derleyip bot maçları koşar.
//   mcs -out:/tmp/botmac.exe tool/unity_test/BotMac.cs unity/BirdieSmash/Assets/BirdieSmash/Core/*.cs && mono /tmp/botmac.exe
// Kontroller (2026-10-24, HTML fb1b245 kuralları): maçlar biter, skor kurala uyar (5/11/21; 7/15/30 tavan), üç servis
// türü de atılır, iki oyuncunun nefesi düşer ve dolar, sayı arası 0,7 sn (rekorda 1,3), hazırlıkta temas karesi temastan
// önce gösterilmez, sıçrayarak smaç temasta tepede, hamlede gövde yatmaz, ilk açılış antrenmanı 3 adım + 5 sayılık maç,
// kariyer düğme etiketi, NaN yok.
// 2026-10-27 oyun incelemesi (OyunIncelemesi()): Hızlı maç rakibi oyuncunun sıradaki kariyer basamağının g'sinde
// (KariyerVeri.Serbest; ilk açılışın ilk maçı da), Usta yolu dışındaki Usta maçında rakip g × usta.carpan (Match.UstaGucu),
// Kolay ve Usta yolu dokunulmaz; denge kapısı: botla (pHit .07) Tur 1 ilk basamağı ≥ %70, Tur 1 Usta (Usta botu) ≥ %20.
// 2026-10-25: HTML → Unity ilerleme devri (Core/Devir: bir kez, bayrak, normalize), maç sonu özeti (Core/Ozet: öncelik
// sırası, ipucu), mağaza puanı koşulu, boşta duruşun nefese bağlı hızı.
// 2026-10-24 tam geçiş: ekipman çarpanları HTML gsHesap ile aynı (node ile ölçülen değerler), ağır raket smacı gerçekten
// hızlandırır, görev olayı gelir, tam antrenman 15 adım (iki bilgi + antrenör çağrısı) ve ödül yuvarlaması HTML gibi.
// 2026-10-26 kariyer haritası ve imza vuruşları (adım 2–3): veri tek kaynak (tool/veri = Resources/veri), Tur 1 eski altı
// basamakla birebir, yıldız kuralları ve kayıt taşıması (Core/Ilerleme), basamağın sabit görevi, imza vuruşu: yalnız
// patronda, maçta ≤ 4, aralarında ≥ 4 sayı, hazırlık ≥ 0,4 sn (Kolay 0,6), her imza görülür ve cevaplanabilir.
// 2026-10-26 ekipman v2 ve karakter (adım 4 ve 6): forma kumaş/renk ayrımı ve eski kayıt taşıması, açılış seviyeleri ve
// Tüy ile model alma, Sv 10'da her raket/kordaj/grip modelinin artısı ve eksisi kalır, 5 çubuk (nötr 50), tarz kuralları
// (Dengeli etkisiz, her tarzın artısı ve eksisi, etki ≤ %20, seviye kapısı), unvanlar, ad/tarz/unvan kaydı.
// 2026-10-26 karakter tasarımı: tip ve saç biçimi yalnız görünüş (güç alanı yok, GearStats değişmez), fiyat/seviye kapısı
// (Tüy seviyeyi aşmaz), başta 1–2 tip ücretsiz, garip saçlar pahalı, alınan geri alınmaz, seçili öğe korunur (taşıma);
// ayakkabı 7. model yuvası: çubuk toplamı nötr (±1), her modelin artısı ve eksisi, Klasik kort nötr = eski fizik birebir,
// eski tuytop_gear (ayakkabısız) Klasik kort; rakiplerin görünüş verisi geçerli kimliklerden.
// 2026-10-26 aldatıcı vuruşlar (Core/Aldatma, Aldatmalar()): her vuruş seviyesinin altında, ekipmansız, kötü konumda ve
// uzanırken reddedilir, üç kapı tutunca kabul; etki uygulanır ve üst tur rakibinde küçülür; marjinal konumda kötü vuruş ve
// dışarı/file artar; jest okuma (tutma, V, yay yönü); görev havuzu; rakip aldatması Tur 3+/Usta, sıklık ve hazırlık sınırı.
// 2026-10-26 vuruş anı ağır çekimi (Pencereler()): hız tek karede değil ~0,12 sn'de iner, ~0,15 sn'de çıkar; taban
// Kolay .45 / Usta .6, Odak bandı aynı oranla; Hareketi azalt zaman çarpanını değiştirmez.
// 2026-10-27 fizik turu (FizikOlcumleri(), Pencereler() ek): uçuş gerçek tüy topla tutarlı (son hız 6,5–6,9 m/s, hız
// mesafeyle e^(−x/4,6) düşer, clear Tartaglia biçimi: iniş çıkışın ≥ 1,6 katı, düşüş ≥ 75°, arka çizgiye 1 m'den yakın;
// smaç inişte çıkışın %25'inden yavaş; drop ve file vuruşu kısa servis çizgisinin önüne), rakibin slice'ı (çevirme)
// kendi sürüklenmesiyle nişanlanır ve fileye gitmez; ağır çekim rampası pencereden önce biter, vuruşla kapanan pencere
// PENCERE_VURUS_OUT'ta çıkar (smaç ilk 0,1 sn'de gerçek hızının ≥ %90'ında görünür).
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
        OyunIncelemesi();   // 2026-10-27 (ayrı satır: ek kontrol listesi başka dallarda da büyüyor)
        Console.WriteLine("görev tamamlandı: " + gorevN + " kez");
        Antrenman(); TamAntrenman(); Kariyer(); Ekipman(); Devirler(); Ozetler(); Gezintiler(); Veri(); Ilerlemeler(); Imzalar(); EkipmanV2(); Tarzlar();
        Turlar34(); Drillar(); Gunlukler(); UstaYollari(); Karakterler(); Aldatmalar(); Pencereler(); FizikOlcumleri(); AyakIsleri();
        KosuYonleri();   // 2026-10-27 ekran/kosu (ayrı satır)
        Console.WriteLine(fails == 0 ? "TAMAM" : "HATA " + fails);
        return fails == 0 ? 0 : 1;
    }

    // 2026-10-27 oyun incelemesi: seviyeye göre rakip, Usta gücü, denge kapısı (kariyer.json notu).
    static void OyunIncelemesi()
    {
        var p = new Ilerleme(); var t = Match.STIL["smasci"]; var sb = KariyerVeri.Serbest("smasci", p); float g0 = KariyerVeri.Al(0).g;
        if (Math.Abs(sb.hiz - t.hiz * g0) > 1e-5 || Math.Abs(sb.hata - t.hata / g0) > 1e-5 || sb.kariyer != -1 || sb.patron || t.hiz != Match.STIL["smasci"].hiz) Fail("Hızlı maç rakibi sıradaki basamağın g'sinde olmalı (taban değişmeden)");
        if (KariyerVeri.Serbest("duvar", null).hiz != Match.STIL["duvar"].hiz) Fail("ilerleme yoksa taban tarz");
        float k = KariyerVeri.Usta.carpan; var st = Match.KariyerRakip(2);
        var mu = new Match(3, new MatchOptions { easy = false, stil = st, target = 5 }); var mk = new Match(3, new MatchOptions { easy = true, stil = st, target = 5 });
        var my = new Match(3, new MatchOptions { easy = false, stil = UstaYolu.Rakip(0), target = 5 });
        if (Math.Abs(mu.stil.hiz - st.hiz * k) > 1e-5 || Math.Abs(mu.stil.hata - st.hata / k) > 1e-5 || mk.stil.hiz != st.hiz || my.stil.hiz != UstaYolu.Rakip(0).hiz || st.hiz != Match.KariyerRakip(2).hiz) Fail("Usta gücü: yalnız Usta yolu dışındaki Usta maçı, kaynak tarz değişmez");
        var gm = new Match(3, new MatchOptions { easy = true, stil = st, target = 5, degistirici = "usta" }); if (Math.Abs(gm.stil.hiz - st.hiz * k) > 1e-5) Fail("Günün maçı Usta: rakip gücü Usta çarpanıyla");
        int kz = 0, ku = 0, n = 24;
        for (int m = 0; m < n; m++)
        {
            foreach (bool easy in new[] { true, false })
            {
                var M = new Match(900 + m, new MatchOptions { easy = easy, stil = Match.KariyerRakip(easy ? 0 : 1), target = 5 }); int guard = 0;
                while (!M.over && guard++ < 60 * 60 * 20) { Bot(M, 1f / 60f, easy ? .07 : .12); if (!easy) UstaKos(M); M.Update(1f / 60f); M.events.Clear(); }
                if (M.sc[0] > M.sc[1]) { if (easy) kz++; else ku++; }
            }
        }
        if (kz < n * .7) Fail("Tur 1 ilk basamak Kolay botla %" + (100 * kz / n) + " (≥ %70)");
        if (ku < n * .2) Fail("Tur 1 2. basamak Usta botla %" + (100 * ku / n) + " (≥ %20)");
        Console.WriteLine("oyun incelemesi: Hızlı maç rakibi g " + g0 + " (sıradaki basamak), Usta çarpanı " + k + "; Tur 1/1 Kolay %" + (100 * kz / n) + ", Tur 1/2 Usta %" + (100 * ku / n));
    }
    // Usta botu (Denge.UstaKos ile aynı): rakip vurduktan ~0,25 sn sonra topun alınacağı yere (±30 cm), vuruştan sonra ortaya.
    static Shuttle kosTop; static float kosGecik;
    static void UstaKos(Match M)
    {
        if (M.s == null || M.s.dead) return;
        if (M.s != kosTop) { kosTop = M.s; kosGecik = (float)(.25 + rng.NextDouble() * .12); if (M.s.from == "me") M.RunTo(Court.MID, Court.NET + 3.3f, .8f); }
        if (M.s.from == "op" && kosGecik > 0 && M.clock - M.opHitAt >= kosGecik && M.icpt != null && M.icpt.who == "me")
        { kosGecik = -1; M.RunTo(M.icpt.x + (float)(rng.NextDouble() - .5) * .6f, M.icpt.y + (float)(rng.NextDouble() - .5) * .6f, 1.2f); }
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
        var g = new Gear(new BellekKayit(), 0); g.raket = "agir"; g.tel = 22; g.bileklik = "agir"; g.bant = "odak"; g.kumas = "nefes";   // 2026-10-26: v1 "beyaz" forması = Nefes alan kumaşı
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
        foreach (var ad in System.IO.Directory.GetFiles("tool/veri", "*.json").Select(f => System.IO.Path.GetFileNameWithoutExtension(f)))
        {
            string a = System.IO.File.ReadAllText("tool/veri/" + ad + ".json"), b = System.IO.File.ReadAllText("unity/BirdieSmash/Assets/Resources/veri/" + ad + ".json");
            if (a != b) Fail("veri kopyası farklı: " + ad + ".json (node tool/veri_kopyala.js)");
        }
        string[] stil = { "file", "smasci", "duvar", "file", "smasci", "duvar" }, yer = { "Mahalle salonu", "Okul turnuvası", "İl şampiyonası", "Bölge finali", "Ulusal lig", "Şampiyonlar kupası" }, saha = { "okul", "okul", "salon", "salon", "turnuva", "turnuva" };
        float[] g = { .62f, .66f, .77f, .75f, .79f, .84f };   // 2026-10-27: g yeniden ölçüldü (kariyer.json notu); yer, sıra, tarz, saha birebir
        for (int i = 0; i < 6; i++) { var k = Match.KARIYER[i]; if (k.stil != stil[i] || k.yer != yer[i] || k.saha != saha[i] || Math.Abs(k.g - g[i]) > 1e-6 || k.uzunluk != 0) Fail("Tur 1 basamağı " + i + " değişti"); }
        var b0 = Match.STIL["smasci"]; if (b0.isim != "Blaze" || b0.ustP[0] != .62f || b0.hata != 1.2f || b0.ev != 3.2f || b0.yorK != 1.3f) Fail("Blaze değerleri veriden farklı okundu");
        var d0 = Match.STIL["duvar"]; if (d0.hiz != 1.08f || d0.hata != .75f || d0.yorK != .7f) Fail("Rocco değerleri veriden farklı okundu");
        var f0 = Match.STIL["file"]; if (f0.geriK != 1.8f || f0.ev != 2.2f) Fail("Mika değerleri veriden farklı okundu");
        if (KariyerVeri.Turlar.Length != 4 || !KariyerVeri.Turlar[1].Var || KariyerVeri.Turlar[1].b[5].uzunluk != 11 || !KariyerVeri.Turlar[1].b[5].patron) Fail("Asya Turu verisi");
        if (!KariyerVeri.Turlar[2].Var || !KariyerVeri.Turlar[3].Var) Fail("Tur 3–4 verisi yok");
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
        // 2026-10-26: Tur 3–4 verisi geldi. Asya Turu bitince Avrupa Turu açık (yakında değil); Dünya Finalleri bitince hepsi
        // bitti ve Usta yolu açık (notu "Usta yolu açıldı").
        var f = Ilerleme.Oku(null, 6, true); for (int b = 0; b < 6; b++) f.Isle(6 + b, true, false, 5, 4, 5);
        if (f.HepsiBitti || !f.TurAcik(2) || f.TurYakinda(2) || f.Siradaki() != 12 || f.UstaAcik) Fail("Asya Turu bitince Avrupa Turu açılmalı");
        IlerlemeSonuc son = null; for (int g = 12; g < 24; g++) son = f.Isle(g, true, false, 5, 4, 5);
        if (!f.HepsiBitti || !f.UstaAcik || !son.turBitti || !son.not.Contains("Usta yolu açıldı") || f.Siradaki() != 23) Fail("Dünya Finalleri bitince Usta yolu açılmalı: " + son.not);
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

    // Ekipman v2 (tasarım §4, Ek A.6): kayıt taşıması, açılış ve alma, Sv 10'da artı/eksi, 5 çubuk.
    static void EkipmanV2()
    {
        // (1) v1 kaydı: forma ikiye bölünür, v1 modellerinin hepsi açık kalır, seçim korunur
        var k = new BellekKayit();
        k.Set("tuytop_gear", "{\"raket\":\"esnek\",\"tel\":24,\"bileklik\":\"pamuk\",\"bant\":\"ter\",\"forma\":\"fistik\"}");
        k.Set("tuytop_ekonomi", "{\"tuy\":500,\"xp\":0,\"sv\":{\"raket\":3,\"tel\":1,\"grip\":2,\"bileklik\":1,\"bant\":1,\"forma\":4},\"gun\":\"\",\"esit\":false}");
        var g = new Gear(k, 3);
        if (g.kumas != "hafif" || g.renk != "fistik" || g.raket != "esnek" || g.tel != 24 || g.eko.sv["forma"] != 4) Fail("v1 kaydı taşınmadı: " + g.kumas + "/" + g.renk + "/" + g.raket);
        foreach (var x in new[] { "raket:agir", "raket:hafif", "bant:odak", "bileklik:agir", "kumas:esnek", "kumas:nefes" }) { var p = x.Split(':'); if (!g.Sahip(p[0], p[1])) Fail("eski oyuncu v1 modelini kaybetti: " + x); }
        if (g.Sahip("raket", "kesici") || g.Sahip("kordaj", "multi")) Fail("yeni model eski oyuncuya bedava açıldı");
        g.GearYaz(); g.EkoYaz(); var j = Json.Obj(Json.Parse(k.Get("tuytop_gear")));
        if (Json.Str(j, "forma", "") != "fistik" || Json.Str(j, "kumas", "") != "hafif" || Json.Str(j, "renk", "") != "fistik") Fail("tuytop_gear forma/kumas/renk yazılmadı: " + k.Get("tuytop_gear"));
        var g2 = new Gear(k, 3); if (g2.kumas != "hafif" || !g2.Sahip("raket", "agir") || g2.eko.acik.Count != g.eko.acik.Count) Fail("v2 kaydı gidiş-dönüş");
        string[,] es = { { "mavi", "klasik", "mavi" }, { "beyaz", "nefes", "beyaz" }, { "mor", "esnek", "mor" }, { "sampiyon", "klasik", "sampiyon" }, { "yok_boyle", "klasik", "mavi" } };
        for (int i = 0; i < es.GetLength(0); i++)
        {
            var kk = new BellekKayit(); kk.Set("tuytop_gear", "{\"forma\":\"" + es[i, 0] + "\"}"); var gg = new Gear(kk, 0);
            if (gg.kumas != es[i, 1] || gg.renk != es[i, 2]) Fail("forma " + es[i, 0] + " → " + gg.kumas + "+" + gg.renk);
        }
        // eski build tuytop_gear'ı kumas/renk olmadan geri yazdı: forma'dan yeniden türer; işlev öncelikli eski forma
        g.kumas = "nefes"; g.renk = "mavi"; if (g.EskiForma() != "beyaz") Fail("eski forma işlevi korumalı: " + g.EskiForma());
        g.kumas = "klasik"; g.renk = "kirmizi"; if (g.EskiForma() != "mavi") Fail("v1'de olmayan renk: kumaşın forması");
        g.kumas = "esnek"; g.renk = "mor"; if (g.EskiForma() != "mor") Fail("aynı çift kendi forması");
        // (2) yeni oyuncu: açılış seviyesi + Tüy ile alma; seviye kapısı Tüy ile aşılmaz
        var y = new Gear(new BellekKayit(), 0); y.eko.tuy = 5000;
        if (y.Sahip("raket", "hafif") || y.Durum("raket", "hafif") != ModelDurum.Kilitli || y.Al("raket", "hafif") || y.Sec("raket", "hafif")) Fail("Sv 1'de Kafa hafif kilitli olmalı");
        if (!y.Sahip("raket", "denge") || y.Durum("raket", "denge") != ModelDurum.Secili || !y.RenkAcik("beyaz", false) || y.RenkAcik("kirmizi", false) || y.RenkAcik("sampiyon", false) || !y.RenkAcik("sampiyon", true)) Fail("başlangıç modelleri ve renkler");
        y.eko.xp = 0; for (int L = 1; L < 3; L++) y.eko.xp += Gear.SvEsik(L);
        if (y.Seviye() != 3 || y.Durum("raket", "hafif") != ModelDurum.Alinir || !y.Al("raket", "hafif") || y.eko.tuy != 5000 - Gear.GEAR["raket"]["hafif"].fiyat || !y.Sec("raket", "hafif") || y.raket != "hafif") Fail("Sv 3'te Kafa hafif alınıp seçilmeli");
        if (y.Al("raket", "hafif")) Fail("aynı model iki kez alındı");
        y.EkoYaz(); var y2 = new Gear(new BellekKayit(), 0); if (y2.Sahip("raket", "hafif")) Fail("bellek kaydı paylaşıldı");
        // fiyat 300–1.200, açılış seviyeleri tasarım §5.2 ile aynı
        var sv = new Dictionary<string, int> { { "raket:hafif", 3 }, { "raket:agir", 5 }, { "raket:esnek", 8 }, { "raket:kesici", 12 }, { "raket:hucum", 18 }, { "kordaj:multi", 6 }, { "kordaj:ince", 11 }, { "grip:havlu", 4 }, { "grip:kalin", 9 },
            { "bileklik:pamuk", 2 }, { "bileklik:agir", 7 }, { "bant:ter", 3 }, { "bant:odak", 10 }, { "kumas:hafif", 5 }, { "kumas:nefes", 8 }, { "kumas:esnek", 12 } };
        foreach (var kv in sv) { var p = kv.Key.Split(':'); var m = Gear.GEAR[p[0]][p[1]]; if (m.sv != kv.Value || m.fiyat < 300 || m.fiyat > 1200) Fail("açılış/fiyat " + kv.Key + ": Sv " + m.sv + ", " + m.fiyat); }
        // (3) Sv 10'da her raket/kordaj/grip modelinin artısı ve eksisi kalır (aynı seviyedeki nötr modele göre)
        var t10 = new Gear(new BellekKayit(), 0); foreach (var kk in t10.eko.sv.Keys.ToList()) t10.eko.sv[kk] = 10;
        Console.WriteLine("ekipman v2: 5 çubuk (Güç/Kontrol/Tatlı/Çabukluk/Dayanıklılık), Sv 1 → Sv 10, diğer yuvalar nötr:");
        foreach (var yu in new[] { "raket", "kordaj", "grip", "bileklik", "bant", "kumas" })
            foreach (var m in Gear.GEAR[yu].Values)
            {
                var a1 = new Gear(new BellekKayit(), 0); var a10 = new Gear(new BellekKayit(), 0); foreach (var kk in a10.eko.sv.Keys.ToList()) a10.eko.sv[kk] = 10;
                Set(a1, yu, m.id); Set(a10, yu, m.id); var n10 = new Gear(new BellekKayit(), 0); foreach (var kk in n10.eko.sv.Keys.ToList()) n10.eko.sv[kk] = 10;
                var h = a10.Hesap(); var n = n10.Hesap();
                Console.WriteLine("  " + (yu + "/" + m.id).PadRight(18) + string.Join(" ", Gear.Cubuklar(a1.Hesap()).Select(v => v.ToString().PadLeft(3))) + "  →" + string.Join(" ", Gear.Cubuklar(h).Select(v => v.ToString().PadLeft(3))) + "   " + m.n);
                if (yu != "raket" && yu != "kordaj" && yu != "grip" || m.id == Gear.Varsayilan(yu)) continue;
                float[] r = { h.guc / n.guc, h.kontrol / n.kontrol, h.hiz / n.hiz, h.tatli / n.tatli, n.yorK / h.yorK };
                bool eksi = r.Any(v => v < 1 - 1e-4), arti = r.Any(v => v > 1 + 1e-4) || h.slice > 1 || h.yorgunSapma < 1;
                if (!eksi || !arti) Fail("Sv 10'da " + yu + "/" + m.id + " artı/eksi kaybı: " + string.Join(",", r.Select(v => v.ToString("0.000"))));
            }
        var b0 = Gear.Cubuklar(new Gear(new BellekKayit(), 0).Hesap()); if (b0.Any(v => v != 50)) Fail("nötr çubuklar 50 değil: " + string.Join(",", b0));
        var gk = Gear.Cubuklar(GearStats.Notr); if (gk.Any(v => v != 50)) Fail("GearStats.Notr çubukları 50 değil");
        // (4) özel etkiler fiziğe gider: Kesici slice çarpanı, Havlu yorgun sapma, Hücum nefes
        var kz = new Gear(new BellekKayit(), 0); kz.raket = "kesici"; if (kz.Hesap().slice != 1.15f) Fail("Kesici slice ×1,15");
        kz.raket = "hucum"; if (!(kz.Hesap().yorK > 1.07f)) Fail("Hücum nefes ×1,08"); kz.grip = "havlu"; if (kz.Hesap().yorgunSapma != .5f || kz.Hesap().gripDoku != "cizgi") Fail("Havlu yorgun sapma");
        Console.WriteLine("ekipman v2: v1 kaydı taşındı (forma → kumaş + renk, v1 modelleri açık), açılış/alma, Sv 10 artı-eksi, nötr çubuk 50 tamam");
    }
    static void Set(Gear g, string yuva, string id)
    {
        switch (yuva) { case "raket": g.raket = id; break; case "kordaj": g.kordaj = id; break; case "grip": g.grip = id; break; case "bileklik": g.bileklik = id; break; case "bant": g.bant = id; break; default: g.kumas = id; break; }
    }

    // Oyun tarzı, unvan, karakter kaydı (tasarım §5, Ek A.6).
    static void Tarzlar()
    {
        var T = Karakter.Tarzlar; if (T.Length != 5 || T[0].id != "dengeli" || T[0].etki.Count != 0) Fail("5 tarz, ilki etkisiz Dengeli");
        var sv = new Dictionary<string, int> { { "dengeli", 1 }, { "smasci", 4 }, { "duvar", 8 }, { "file", 12 }, { "aldatici", 18 } };
        foreach (var t in T)
        {
            if (!sv.ContainsKey(t.id) || sv[t.id] != t.sv) Fail("tarz açılışı " + t.id + " Sv " + t.sv);
            if (t.id == "dengeli") continue;
            // iyi yön: smacYor ve toparla küçüldükçe, diğerleri büyüdükçe
            Func<KeyValuePair<string, float>, bool> iyi = kv => kv.Key == "smacYor" || kv.Key == "toparla" ? kv.Value < 1 : kv.Value > 1;
            if (!t.etki.Any(kv => iyi(kv))) Fail("tarzın artısı yok: " + t.id);
            if (!t.etki.Any(kv => !iyi(kv))) Fail("tarzın eksisi yok: " + t.id);
            if (t.etki.Values.Any(v => Math.Abs(v - 1) > .2001f)) Fail("tarz etkisi %20'den büyük: " + t.id);
            var u = Karakter.Uygula(GearStats.Notr, t.id); if (u.tarz != t.id || GearStats.Notr.tarz != "dengeli") Fail("Uygula kopya değil");
        }
        var d = Karakter.Uygula(GearStats.Notr, "dengeli"); if (d.enerji != 1 || d.tatli != 1 || d.slice != 1 || d.uzanma != 1) Fail("Dengeli nötr değil");
        if (Karakter.Gecerli("aldatici", 17) != "dengeli" || Karakter.Gecerli("aldatici", 18) != "aldatici" || Karakter.Gecerli("yok", 30) != "dengeli") Fail("tarz seviye kapısı");
        // tarz maça gider: Smaşçı enerjisi, Duvar uzanma erişimi (aynı tohum, aynı girdi)
        var sm = Karakter.Uygula(GearStats.Notr, "smasci"); if (Math.Abs(sm.enerji - 1.2f) > 1e-5 || sm.smacYor != 1.15f) Fail("Smaşçı etkisi");
        var dv = new Match(3, new MatchOptions { easy = true, target = 5, gs = Karakter.Uygula(GearStats.Notr, "duvar") }); if (dv.GS.uzanma != 1.06f) Fail("tarz maça gitmedi");
        // unvanlar
        var p = new Ilerleme(); var u0 = Karakter.Kazanilan(p, 1, 0);
        if (u0.Count != 1 || u0[0].id != "mahalle" || Karakter.Gosterilen(p, 1, 0).id != "mahalle") Fail("başlangıç unvanı Mahalle oyuncusu");
        for (int b = 0; b < 6; b++) p.tur[0][b] = Ilerleme.KAZAN;
        if (Karakter.Gosterilen(p, 1, 0).id != "yerel" || Karakter.Kazanilan(p, 30, 0).Count != 3 || Karakter.Gosterilen(p, 30, 0).id != "efsane") Fail("Tur 1 → Yerel şampiyon, Sv 30 → Efsane");
        p.Unvan = "mahalle"; if (Karakter.Gosterilen(p, 30, 0).id != "mahalle") Fail("seçilen unvan gösterilmeli"); p.Unvan = "dunya"; if (Karakter.Gosterilen(p, 30, 0).id != "efsane") Fail("kazanılmamış seçili unvan düşmeli");
        // kayıt: tarz, unvan, ad, sayaçlar; bilinmeyen alan korunur
        p.Tarz = "duvar"; p.Unvan = "yerel"; p.Ad = "  Yasin\u0007 Birdie Smash Oyuncusu  "; p.MacSay(4); p.MacSay(2);
        var q = Ilerleme.Oku(p.Yaz(), 0, false);
        if (q.Tarz != "duvar" || q.Unvan != "yerel" || q.Ad != "Yasin Birdie Sma" || q.Mac != 2 || q.Smac != 6 || q.Tur1Gecilen() != 6) Fail("karakter kaydı: " + q.Yaz());
        if (Ilerleme.AdTemizle("   ") != null || new Ilerleme().Tarz != "dengeli" || new Ilerleme().Ad != null) Fail("boş ad / varsayılan tarz");
        Console.WriteLine("tarz: " + string.Join(", ", T.Select(t => t.ad + " Sv" + t.sv)) + "; unvan " + Karakter.Unvanlar.Length + "; açılanlar Sv 4: " + string.Join(", ", Karakter.Acilanlar(4)) + "; Sv 18: " + string.Join(", ", Karakter.Acilanlar(18)));
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

    // ---- 2026-10-26 adım 7–9 ----
    // Tur 3–4: imza bayrağı, tur ek zorluğu, Tur 1–2 dokunulmadı (tepki/hucum 1), uzunluklar.
    static void Turlar34()
    {
        for (int t = 0; t < 2; t++) if (KariyerVeri.Turlar[t].tepki != 1 || KariyerVeri.Turlar[t].hucum != 1) Fail("Tur " + (t + 1) + " ek zorluk almamalı");
        var t3 = KariyerVeri.Turlar[2]; var t4 = KariyerVeri.Turlar[3];
        if (!(t3.tepki < 1 && t3.hucum > 1 && t4.tepki < t3.tepki && t4.hucum > t3.hucum)) Fail("Tur 3–4 ek zorluğu artmalı");
        if (t3.b[0].stil != "vex" || !t3.b[5].patron || t3.b[5].stil != "vex" || t3.b[5].uzunluk != 11) Fail("Avrupa Turu: Vex yeni rakip ve patron");
        if (t4.b[0].stil != "aria" || !t4.b[5].patron || t4.b[5].stil != "aria") Fail("Dünya Finalleri: Aria yeni rakip ve patron");
        foreach (var k in t4.b) if (k.uzunluk != 11) Fail("Dünya Finalleri 11 sayı: " + k.yer);
        int imzali = 0; foreach (var k in t4.b) if (k.imza) { imzali++; var r = KariyerVeri.Rakip(KariyerVeri.Global(3, k.basamak)); if (!r.patron || r.imza == null) Fail("imza bayraklı basamakta imza yok: " + k.yer); }
        if (imzali != 3) Fail("Dünya Finalleri'nde 3 imzalı basamak (Rocco, Nova, Vex) olmalı: " + imzali);
        var r0 = KariyerVeri.Rakip(KariyerVeri.Global(3, 3)); if (r0.patron || r0.imza != null) Fail("imzasız basamakta imza var");
        var r1 = KariyerVeri.Rakip(KariyerVeri.Global(2, 0)); if (Math.Abs(r1.tepki - Match.STIL["vex"].tepki * t3.tepki) > 1e-5 || Math.Abs(r1.hucum - t3.hucum) > 1e-5) Fail("tur ek zorluğu rakibe binmedi");
        var y1 = KariyerVeri.Rakip(0); if (y1.tepki != Match.STIL[y1.id].tepki || y1.hucum != 1) Fail("Tur 1 rakibi değişti");
        foreach (var t in new[] { t3, t4 }) foreach (var k in t.b) if (k.yer.Length == 0 || k.yer.Contains("All England")) Fail("mekân adı");
        Console.WriteLine("Tur 3–4: Vex/Aria, " + imzali + " imzalı basamak, tepki " + t3.tepki + "/" + t4.tepki + ", hücum " + t3.hucum + "/" + t4.hucum + ", Tur 1–2 aynı");
    }

    // Antrenman görevleri: her görev 12 topta biter (takılmaz), yıldız eşikleri, kayıt (en iyi, geri alınmaz), ödül yalnız yeni yıldız.
    static void Drillar()
    {
        var R = new Random(5);
        if (Dril.Liste.Length != 8 || Dril.ToplamYildiz != 24) Fail("8 görev, 24 yıldız olmalı");
        foreach (var d in Dril.Liste)
        {
            var M = new Match(31, new MatchOptions { easy = true, ogr = true, dril = d, target = 5 }); int guard = 0; float t = 0;
            if (M.Dril != d || M.ogr == null || M.gorevler.Count != 0) Fail("görev antrenman altyapısında açılmadı: " + d.id);
            while (!M.ogr.bitti && guard++ < 60 * 240)
            {
                if (d.olcu == "servis") { if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null) M.SwipeServe(0, -60, .5f); }
                else { Reach re; if (M.ReachInfo(out re) && R.NextDouble() < .2) M.SwipeShot(new SwipeInput { dx = 0, dy = -200, speed = d.id == "drop" ? .5f : d.id == "kill" || d.id == "kose" ? 2.1f : 1.3f, R = 40, dist = 5, curved = d.id == "aldat" }); }
                if (d.olcu == "split" && M.s != null && M.s.from == "op" && M.clock - M.opHitAt > .15f) M.OnTouchDown();
                M.Update(1f / 60f); t += 1f / 60f; M.events.Clear();
                if (M.over) { Fail("görev maç sonuna gitti: " + d.id); break; }
            }
            if (!M.ogr.bitti || M.ogr.dene < d.top) Fail("görev 12 topta bitmedi: " + d.id + " top " + M.ogr.dene + " süre " + t.ToString("0"));
            if (t > 150) Fail("görev çok uzun: " + d.id + " " + t.ToString("0") + " sn");
            if (M.sc[0] + M.sc[1] != 0) Fail("görevde skor yazıldı: " + d.id);
            if (d.olcu == "servis" && M.DrilAlan() == null) Fail("kısa servis alanı yok");
        }
        var D = Dril.Bul("derin"); if (D.Yildiz(D.esik[0] - 1) != 0 || D.Yildiz(D.esik[0]) != 1 || D.Yildiz(D.esik[2]) != 3 || D.Yildiz(12) != 3) Fail("yıldız eşikleri");
        // "Görevi bitir": o ana kadarki isabetle biter
        var B = new Match(32, new MatchOptions { easy = true, ogr = true, dril = D, target = 5 }); for (int i = 0; i < 120; i++) { B.Update(1f / 60f); B.events.Clear(); }
        B.DrilBitir(); for (int i = 0; i < 300 && !B.ogr.bitti; i++) { B.Update(1f / 60f); B.events.Clear(); } if (!B.ogr.bitti) Fail("görevi bitir çalışmadı");
        // kayıt ve ödül
        var p = new Ilerleme();
        if (p.DrilKaydet("derin", 2) != 2 || p.DrilKaydet("derin", 1) != 0 || p.DrilYildiz("derin") != 2 || p.DrilKaydet("derin", 3) != 1 || p.AntrenmanYildiz != 3) Fail("görev yıldızı en iyi sonuç olmalı, geri alınmamalı");
        var p2 = Ilerleme.Oku(p.Yaz(), 0, false); if (p2.DrilYildiz("derin") != 3 || p2.AntrenmanYildiz != 3) Fail("görev yıldızı kayıtta kayboldu: " + p.Yaz());
        var g = new Gear(new BellekKayit(), 0); int tuy0 = g.eko.tuy; var o = g.DrilOdul(2); if (o.tuy != 2 * Gear.DRIL_TUY || o.xp != 2 * Gear.DRIL_XP || g.eko.tuy != tuy0 + 50) Fail("görev ödülü");
        if (g.DrilOdul(0).tuy != 0) Fail("tekrar oynanan görev ödül vermemeli");
        // kayıpta öneri: dışarı giden vuruşlar → derin clear; üç yıldızlıysa önerilmez
        var K = new Match(33, new MatchOptions { easy = true, target = 5 }); K.outsBen = 3; if (Dril.Oner(K, false, new Ilerleme()) != "derin") Fail("kayıpta derin clear önerilmeli");
        if (Dril.Oner(K, true, null) != null) Fail("galibiyette öneri olmamalı");
        var p3 = new Ilerleme(); p3.DrilKaydet("derin", 3); if (Dril.Oner(K, false, p3) == "derin") Fail("üç yıldızlı görev önerildi");
        Console.WriteLine("antrenman görevleri: 8 görev 12 topta biter, yıldız en iyi sonuç, ödül yalnız yeni yıldız (+" + Gear.DRIL_TUY + " Tüy)");
    }

    // Günün maçı: altın tarih, aynı gün aynı maç, 60 günde arka arkaya aynı değiştirici/rakip yok, hepsi görünür, kancalar.
    static void Gunlukler()
    {
        var a = Gunluk.Bugun(new DateTime(2026, 10, 26, 8, 0, 0)); var b = Gunluk.Bugun(new DateTime(2026, 10, 26, 23, 59, 0));
        if (a.tarih != "2026-10-26" || a.deg.id != b.deg.id || a.stil != b.stil || a.tohum != b.tohum || a.g != b.g || a.saha != b.saha) Fail("aynı gün aynı maç olmalı");
        // altın değer: aritmetik değişirse "herkes aynı maç" eski sürümle bozulur (bilinçli değişiklikte burası güncellenir)
        Console.WriteLine("günün maçı 2026-10-26: " + a.deg.id + " · " + a.stil + " · " + a.saha + " · g " + a.g + " · tohum " + a.tohum);
        if (a.deg.id != ALTIN_GUN[0] || a.stil != ALTIN_GUN[1] || a.saha != ALTIN_GUN[2]) Fail("Günün maçı altın değeri değişti: " + a.deg.id + "/" + a.stil + "/" + a.saha);
        var degSay = new Dictionary<string, int>(); var stilSay = new Dictionary<string, int>(); GunlukMac once = null;
        for (int i = 0; i < 60; i++)
        {
            var m = Gunluk.Bugun(new DateTime(2026, 10, 1).AddDays(i));
            if (once != null && (once.deg.id == m.deg.id || once.stil == m.stil)) Fail("arka arkaya aynı değiştirici/rakip: " + m.tarih);
            if (m.g < .72f - 1e-4 || m.g > .88f + 1e-4 || m.uzunluk != m.deg.uzunluk) Fail("günün maçı g/uzunluk: " + m.tarih);
            int c; degSay.TryGetValue(m.deg.id, out c); degSay[m.deg.id] = c + 1; stilSay.TryGetValue(m.stil, out c); stilSay[m.stil] = c + 1; once = m;
        }
        if (degSay.Count != 6 || stilSay.Count != 6) Fail("60 günde her değiştirici ve rakip görünmeli: " + degSay.Count + "/" + stilSay.Count);
        foreach (var kv in degSay) if (kv.Value != 10) Fail("değiştirici 6 günlük bloklarla dönmeli: " + kv.Key + " " + kv.Value);
        // kancalar: altın ralli (her 3. sayı 2), usta (easy kapalı), nefes (yorulma hızlı), patron (imza sık), serbest maç değişmez
        var g = Gunluk.Bugun(new DateTime(2026, 10, 1)); var st = Gunluk.Rakip(g);
        if (st.kariyer != -1 || (g.deg.id != "patron" && st.imza != null)) Fail("günün rakibi kariyer dışı, imzasız olmalı");
        foreach (var id in new[] { "altin", "uzun", "file", "usta", "nefes", "patron" })
        {
            var M = new Match(41, new MatchOptions { easy = true, target = 11, stil = Match.STIL["duvar"].Clone(), degistirici = id });
            if (M.degistirici != id) Fail("değiştirici kurulmadı: " + id);
            if (id == "usta" && M.easy) Fail("Usta günü Kolay kaldı");
            if (id == "nefes" && !(M.GS.yorK > 1.3f)) Fail("nefes değiştiricisi yorulmayı hızlandırmadı");
            if (id == "patron" && (!M.stil.patron || M.stil.imzaAra != 2 || Match.STIL["duvar"].imzaAra == 2)) Fail("patron provası kaynağı değiştirdi ya da kurulmadı");
            int guard = 0, iki = 0, sayi = 0; var R = new Random(9);
            while (!M.over && guard++ < 60 * 60 * 30)
            {
                Bot(M, 1f / 60f); int s0 = M.sc[0] + M.sc[1]; M.Update(1f / 60f); int d = M.sc[0] + M.sc[1] - s0; if (d > 0) { sayi++; if (d == 2) iki++; if (d > 2) Fail("sayı 2'den büyük"); }
                M.events.Clear();
            }
            if (!M.over) Fail("değiştiricili maç bitmedi: " + id);
            if (id == "altin" && Math.Abs(iki - sayi / 3) > 1) Fail("altın ralli her 3. sayı 2 değil: " + iki + "/" + sayi);
            if ((id == "nefes" || id == "usta" || id == "patron") && iki != 0) Fail("çift sayı olmamalı: " + id);
            if (Math.Max(M.sc[0], M.sc[1]) > 15) Fail("tavan aşıldı: " + id);
        }
        var serbest = new Match(41, new MatchOptions { easy = true, target = 11 }); if (serbest.degistirici != null) Fail("serbest maçta değiştirici");
        var p = new Ilerleme(); if (!p.GunlukKazan("2026-10-26") || p.GunlukKazan("2026-10-26") || !p.GunlukAlindi("2026-10-26") || p.GunlukAlindi("2026-10-27") || !p.GunlukKazan("2026-10-28") || p.GunlukSay != 2) Fail("günün ödülü günde bir kez");
        if (Ilerleme.Oku(p.Yaz(), 0, false).GunlukSon != "2026-10-28") Fail("günlük kayıt");
        Console.WriteLine("günün maçı: aynı gün aynı maç, 60 günde 6×10 değiştirici, arka arkaya tekrar yok, ödül günde bir");
    }
    static readonly string[] ALTIN_GUN = { "uzun", "vex", "salon" };   // 2026-10-26 ilk ölçüm (Gunluk aritmetiği sabit kaldıkça aynı)

    // Usta yolu: belirlenimci sıra (24'lük çevrimde her basamak bir kez), g tavanı, final 11 sayı + imza, Usta zorluk, kayıt.
    static void UstaYollari()
    {
        var gor = new HashSet<int>(); for (int n = 0; n < UstaYolu.CEVRIM; n++) gor.Add(UstaYolu.Kaynak(n)); if (gor.Count != 24) Fail("Usta çevrimi 24 basamağı dolaşmalı");
        if (UstaYolu.Kaynak(30) != UstaYolu.Kaynak(30) || UstaYolu.Basamak(7).yer != UstaYolu.Basamak(7).yer) Fail("Usta yolu belirlenimci değil");
        var u = KariyerVeri.Usta; if (Math.Abs(UstaYolu.Basamak(0).g - u.g) > 1e-4 || UstaYolu.Basamak(100000).g > u.tavan + 1e-4) Fail("Usta g kuralı");
        var f = UstaYolu.Basamak(5); if (!f.final || f.uzunluk != u.finalUzunluk || UstaYolu.Basamak(4).final) Fail("her 6. basamak final");
        var r = UstaYolu.Rakip(5); if (r.usta != 5 || r.kariyer != -1 || (r.imza != null) != r.patron) Fail("Usta rakibi");
        var rn = UstaYolu.Rakip(4); if (rn.imza != null || rn.patron) Fail("final dışı Usta basamağında imza");
        for (int n = 0; n < 48; n++) { var gid = UstaYolu.GorevId(n); if (Match.GorevBul(gid) == null) Fail("Usta görevi yok: " + n); if (gid == "okudun" && UstaYolu.Rakip(n).imza == null) Fail("imzasız Usta basamağında imza görevi"); }
        var M = new Match(51, new MatchOptions { easy = false, target = 5, stil = UstaYolu.Rakip(2) }); if (M.BasamakGorevi == null || M.BasamakGorevi.id != UstaYolu.GorevId(2)) Fail("Usta basamak görevi ilk görev olmalı");
        var p = new Ilerleme(); if (p.UstaAcik) Fail("Usta yolu baştan açık olmamalı");
        if (p.UstaIsle(0, false, true) != 1 || p.UstaIsle(0, false, true) != 0 || p.Usta != 0 || p.UstaIsle(0, true, true) != 1 || p.Usta != 1 || p.UstaIsle(0, true, true) != 0 || p.UstaYildiz != 2) Fail("Usta yıldızları: görev kayıpta da, bir kez; geçilen basamak sayılmaz");
        var p2 = Ilerleme.Oku(p.Yaz(), 0, false); if (p2.Usta != 1 || p2.UstaYildiz != 2) Fail("Usta kaydı");
        p.ImzaOkuSay(3); if (Ilerleme.Oku(p.Yaz(), 0, false).ImzaOkunan != 3) Fail("imza okuma sayacı");
        if (!Career.Line(Tamam()).StartsWith("Usta yolu: 1. basamak")) Fail("kariyer bitince satır Usta yolunu göstermeli: " + Career.Line(Tamam()));
        Console.WriteLine("Usta yolu: 24'lük çevrim, g " + u.g + "→" + u.tavan + ", 6'da bir final, kazan + görev yıldızı, kayıt");
    }
    static Ilerleme Tamam() { var p = Ilerleme.Oku(null, 6, true); for (int g = 6; g < 24; g++) p.Isle(g, true, false, 5, 4, 5); return p; }

    // ---- 2026-10-26 karakter tasarımı (tip, saç biçimi, ayakkabı modeli) ----
    static Gear SvGear(int sv, int tuy) { var g = new Gear(new BellekKayit(), 0); g.eko.xp = 0; for (int L = 1; L < sv; L++) g.eko.xp += Gear.SvEsik(L); g.eko.tuy = tuy; return g; }
    static void Karakterler()
    {
        // (1) veri: yalnız görünüş, ilk öğe ücretsiz varsayılan, başta 1–2 tip ücretsiz, fiyatlar makul, garip saç pahalı
        var veri = Json.Obj(Json.Parse(System.IO.File.ReadAllText("tool/veri/karakter.json")));
        string[] guc = { "etki", "guc", "kontrol", "hiz", "tatli", "kosu", "enerji", "toparla", "yor", "uzanma", "jumpTatli", "slice", "akis" };
        foreach (var k in new[] { "tipler", "sac_model" }) foreach (var x in (List<object>)veri[k]) { var d = Json.Obj(x); foreach (var alan in guc) if (d.ContainsKey(alan)) Fail("kozmetikte güç alanı: " + k + "/" + Json.Str(d, "id", "?") + "." + alan); }
        var T = Kozmetik.Liste(Kozmetik.TIP); var S = Kozmetik.Liste(Kozmetik.SAC);
        if (T.Length != 4 || T[0].id != "atlet" || T[0].varlik != null) Fail("4 tip, ilki Atlet (oyuncak.fbx)");
        int bedava = T.Count(t => t.fiyat == 0 && t.sv == 1); if (bedava < 1 || bedava > 2) Fail("başta 1–2 tip ücretsiz olmalı: " + bedava);
        if (Kozmetik.Bul(Kozmetik.TIP, "kadin") == null || Kozmetik.Bul(Kozmetik.TIP, "kadin").fiyat != 0 || Kozmetik.Bul(Kozmetik.TIP, "kadin").sv != 1) Fail("kimlik satılmaz: Atletik kadın baştan ücretsiz olmalı");
        if (S.Length < 10 || S[0].fiyat != 0 || S.Count(o => o.varlik == null) != 1) Fail("en az 10 saç biçimi, ilki ücretsiz, bir tane kazıtılmış (parçasız)");
        foreach (var o in T.Concat(S)) { if (o.fiyat < 0 || o.fiyat > 1000 || (o.sv == 1 && o.fiyat > 0)) Fail("kozmetik fiyat/kapı: " + o.id + " " + o.fiyat + " Sv " + o.sv); if (o.paket != null) Fail("paket alanı şimdilik boş olmalı (IAP yok): " + o.id); }
        int garipMin = S.Where(o => o.garip).Select(o => o.fiyat).DefaultIfEmpty(0).Min(), sadeMax = S.Where(o => !o.garip).Select(o => o.fiyat).Max();
        if (S.Count(o => o.garip) < 2 || garipMin <= sadeMax) Fail("garip saçlar en pahalılar olmalı: " + garipMin + " ≤ " + sadeMax);
        if (S.Distinct().Count() != S.Select(o => o.id).Distinct().Count() || S.Where(o => o.varlik != null).Select(o => o.varlik).Distinct().Count() != S.Count(o => o.varlik != null)) Fail("saç kimlik/varlık tekrarı");
        // (2) kozmetik GearStats'a dokunmaz: aynı ekipmanla hesap birebir (seçim Ayar'da, Gear'de değil; Kozmetik.Al yalnız Tüy)
        var g0 = SvGear(30, 9000); var h0 = g0.Hesap();
        if (!Kozmetik.Al(g0, Kozmetik.SAC, "mohikan", "kisa") || g0.eko.tuy != 9000 - Kozmetik.Bul(Kozmetik.SAC, "mohikan").fiyat) Fail("Sv 30'da Mohikan alınmalı");
        var h1 = g0.Hesap(); if (Gear.Cubuklar(h0).Zip(Gear.Cubuklar(h1), (p, q) => p - q).Any(v => v != 0) || h0.kosu != h1.kosu || h0.guc != h1.guc) Fail("kozmetik alımı gücü değiştirdi");
        // (3) seviye kapısı + Tüy: Sv 1'de Uzun ve ince kilitli (Tüy yetse de), Sv 4'te alınır, iki kez alınmaz, ücretsizler kendiliğinden
        var y = SvGear(1, 5000);
        if (Kozmetik.Durum(y, Kozmetik.TIP, "uzun", "atlet") != ModelDurum.Kilitli || Kozmetik.Al(y, Kozmetik.TIP, "uzun", "atlet") || y.eko.tuy != 5000) Fail("Sv 1'de Uzun ve ince kilitli olmalı");
        if (!Kozmetik.Sahip(y, Kozmetik.TIP, "kadin") || !Kozmetik.Sahip(y, Kozmetik.SAC, "atkuyrugu") || !Kozmetik.Sahip(y, Kozmetik.SAC, "yan") || Kozmetik.Sahip(y, Kozmetik.SAC, "afro")) Fail("başlangıç kozmetikleri");
        var y4 = SvGear(4, 320);
        if (Kozmetik.Durum(y4, Kozmetik.TIP, "uzun", "atlet") != ModelDurum.Alinir || !Kozmetik.Al(y4, Kozmetik.TIP, "uzun", "atlet") || y4.eko.tuy != 20 || !Kozmetik.Sahip(y4, Kozmetik.TIP, "uzun")) Fail("Sv 4'te Uzun ve ince 300 Tüy ile alınmalı");
        if (Kozmetik.Al(y4, Kozmetik.TIP, "uzun", "atlet") || Kozmetik.Durum(y4, Kozmetik.TIP, "uzun", "uzun") != ModelDurum.Secili) Fail("aynı tip iki kez alındı / seçili durumu");
        var y4b = SvGear(5, 100); if (Kozmetik.AlOlur(y4b, Kozmetik.SAC, "topuz", "kisa") || Kozmetik.Al(y4b, Kozmetik.SAC, "topuz", "kisa") || y4b.eko.tuy != 100) Fail("Tüy yetmeden alındı");
        // (4) kayıt ve taşıma: alınan kalıcı (tuytop_ekonomi.acik), seçili öğe korunur, bilinmeyen varsayılana düşer
        var kk = new BellekKayit(); var gk = new Gear(kk, 0); gk.eko.xp = 0; for (int L = 1; L < 9; L++) gk.eko.xp += Gear.SvEsik(L); gk.eko.tuy = 1000;
        Kozmetik.Al(gk, Kozmetik.SAC, "afro", "kisa"); var gk2 = new Gear(kk, 0); if (!Kozmetik.Sahip(gk2, Kozmetik.SAC, "afro") || gk2.eko.tuy != 1000 - Kozmetik.Bul(Kozmetik.SAC, "afro").fiyat) Fail("alınan saç kayıttan dönmedi");
        var gy = SvGear(1, 0); if (Kozmetik.Koru(gy, Kozmetik.SAC, "mohikan") != "mohikan" || !Kozmetik.Sahip(gy, Kozmetik.SAC, "mohikan")) Fail("kayıttaki seçili saç elden alındı");
        if (Kozmetik.Koru(gy, Kozmetik.TIP, "yok_boyle") != "atlet" || Kozmetik.Koru(gy, Kozmetik.TIP, null) != "atlet") Fail("bilinmeyen tip varsayılana düşmeli");
        // (5) ayakkabı: 7. model yuvası, yükseltme yuvası değil; eski kayıt Klasik kort; çubuklar nötr toplamlı yan seçim
        if (Gear.YuvaOf("ayakkabi") != null || Gear.YUVA.Length != 6) Fail("ayakkabı yükseltme yuvası olmamalı (ekonomi tavanı)");
        var ek = new BellekKayit(); ek.Set("tuytop_gear", "{\"raket\":\"agir\",\"forma\":\"mavi\"}"); var eg = new Gear(ek, 2);
        if (eg.ayakkabi != "klasik" || eg.Durum("ayakkabi", "klasik") != ModelDurum.Secili) Fail("eski kayıtta ayakkabı Klasik kort olmalı");
        eg.ayakkabi = "karbon"; eg.GearYaz(); if (new Gear(ek, 2).ayakkabi != "karbon") Fail("ayakkabı kaydı gidiş-dönüş"); 
        if (!new Gear(ek, 2).Sahip("ayakkabi", "karbon")) Fail("kayıttaki seçili ayakkabı elden alındı");
        var n0 = new Gear(new BellekKayit(), 0); var hn = n0.Hesap(); var nt = new Gear(new BellekKayit(), 0); nt.ayakkabi = "klasik";
        if (Gear.Cubuklar(hn).Any(v => v != 50) || hn.uzanma != 1 || hn.jumpTatli != 1 || hn.kosu != 1 || hn.toparla != 1 || hn.yorK != 1) Fail("Klasik kort nötr değil (eski fizik bozulur)");
        var sv = new Dictionary<string, int> { { "klasik", 1 }, { "hafif", 4 }, { "yastikli", 7 }, { "bilekli", 10 }, { "karbon", 18 } };
        Console.WriteLine("ayakkabı: İlk adım/Çabukluk/Uzanma/Sıçrama/Nefes (50 nötr):");
        foreach (var m in Gear.GEAR["ayakkabi"].Values)
        {
            var c = Gear.AyakkabiCubuklar(m); int top = c.Sum(v => v - 50);
            Console.WriteLine("  " + m.id.PadRight(9) + string.Join(" ", c.Select(v => v.ToString().PadLeft(3))) + "  toplam " + (top >= 0 ? "+" : "") + top + "  Sv " + m.sv + " · " + m.fiyat + " Tüy   " + m.n);
            if (!sv.ContainsKey(m.id) || sv[m.id] != m.sv) Fail("ayakkabı açılışı " + m.id + " Sv " + m.sv);
            if (m.id == "klasik") { if (c.Any(v => v != 50) || m.fiyat != 0) Fail("Klasik kort nötr ve ücretsiz"); continue; }
            if (Math.Abs(top) > 1) Fail("ayakkabı çubuk toplamı nötr değil: " + m.id + " " + top);
            if (!c.Any(v => v > 50) || !c.Any(v => v < 50)) Fail("ayakkabının artısı ve eksisi olmalı: " + m.id);
            if (m.fiyat < 300 || m.fiyat > 1200) Fail("ayakkabı fiyatı 300–1.200: " + m.id);
            var ga = new Gear(new BellekKayit(), 0); ga.ayakkabi = m.id; var ha = ga.Hesap();
            if (Math.Abs(ha.kosu - (m.kosu ?? 1)) > 1e-5 || Math.Abs(ha.uzanma - (m.uzanma ?? 1)) > 1e-5 || Math.Abs(ha.jumpTatli - (m.jumpTatli ?? 1)) > 1e-5 || Math.Abs(ha.yorK - (m.yor ?? 1)) > 1e-5) Fail("ayakkabı çarpanı fiziğe gitmedi: " + m.id);
        }
        var ya = SvGear(3, 5000); if (ya.Al("ayakkabi", "hafif") || ya.Sec("ayakkabi", "hafif")) Fail("Sv 3'te Hafif hız kilitli olmalı");
        var yb = SvGear(4, 5000); if (!yb.Al("ayakkabi", "hafif") || yb.eko.tuy != 4650 || !yb.Sec("ayakkabi", "hafif") || yb.ayakkabi != "hafif") Fail("Sv 4'te Hafif hız 350 Tüy");
        // ayakkabı maça gider: Hafif hız oyuncunun koşusunu hızlandırır (aynı tohum)
        var mh = new Match(3, new MatchOptions { easy = true, target = 5, gs = yb.Hesap() }); if (Math.Abs(mh.GS.kosu - 1.048f) > 1e-4) Fail("ayakkabı koşusu maça gitmedi");
        // tarz ayakkabının üstüne biner (Duvar uzanma × Pro karbon uzanma)
        var gc = new Gear(new BellekKayit(), 0); gc.ayakkabi = "karbon"; var tc = Karakter.Uygula(gc.Hesap(), "duvar"); if (Math.Abs(tc.uzanma - 1.06f * 1.024f) > 1e-4) Fail("tarz ve ayakkabı uzanması çarpılmalı");
        // (6) rakip görünüşü: her rakibin verisi var ve kimlikler geçerli
        foreach (var id in KariyerVeri.Stil.Keys)
        {
            var r = Kozmetik.Rakip(id); if (r == null) { Fail("rakip görünüşü yok: " + id); continue; }
            if (Kozmetik.Bul(Kozmetik.TIP, r.tip) == null || Kozmetik.Bul(Kozmetik.SAC, r.sac) == null || !Gear.GEAR["ayakkabi"].ContainsKey(r.ayakkabi) || (r.sacRenk != null && !Karakter.Gorunum("sac").Any(o => o.id == r.sacRenk)) || (r.ten != null && !Karakter.Gorunum("ten").Any(o => o.id == r.ten))) Fail("rakip görünüşünde bilinmeyen kimlik: " + id);
        }
        // (7) açılış listesi: tip, saç modeli ve ayakkabı türüyle
        var ac4 = Karakter.AcilanlarTurlu(4); if (!ac4.Any(a => a[0] == "Tip" && a[1] == "Uzun ve ince") || !ac4.Any(a => a[0] == "Ayakkabı" && a[1] == "Hafif hız")) Fail("Sv 4 açılışı tip ve ayakkabı içermeli");
        if (Karakter.AcilanlarTurlu(16).Any(a => a[0] == "Ayakkabı")) Fail("ayakkabı RENGİ 'Ayakkabı rengi' etiketiyle olmalı");
        Console.WriteLine("karakter: " + T.Length + " tip (" + bedava + " ücretsiz), " + S.Length + " saç biçimi (garip ≥ " + garipMin + " Tüy), 5 ayakkabı nötr toplamlı, kozmetik güç vermez, kapı/taşıma/rakip tamam");
    }

    // 2026-10-26 aldatıcı vuruşlar (Core/Aldatma): her vuruş seviyesinin altında, ekipmansız ve kötü konumda reddedilir, üçü
    // tutunca kabul edilir; etki (rakip geç ve yanlış/donuk çıkar) uygulanır ve üst tur rakibinde küçülür; marjinal konumda
    // hata olasılığı artar; rakibin aldatması Tur 3+ ve Usta yolunda, maçta ≤ azami, aralarında ≥ ara sayı, hazırlık ≥ 0,4 sn.
    static readonly GearStats HILE_IYI = new GearStats { kafa = 1.18f, slice = 1.15f, kontrol = 1.05f, hiz = 1.08f };
    static readonly GearStats HILE_KOTU = new GearStats { kafa = .78f, slice = 1, kontrol = .92f, hiz = .93f };
    static Match HileSahne(HileTanim t, int sv, GearStats gs, int seed, Style st = null, float durgun = 1, float nefes = 1)
    {
        Match.OyuncuSv = sv; var M = new Match(seed, new MatchOptions { easy = false, target = 21, gs = gs, stil = st ?? Match.STIL["duvar"] }); Match.OyuncuSv = 1;
        float x = Court.MID, y, z, mx, my;
        if (t.baglam == "file") { y = Court.NET + .75f; z = t.id == "ciftaksiyon" ? 1.5f : 1.4f; mx = x - .3f; my = y + .4f; }
        else if (t.baglam == "ust") { y = Court.NET + 4; z = 2.45f; mx = x - .3f; my = y + .4f; }
        else { y = Court.NET + 3; z = 1.9f; mx = x + .6f; my = y - .2f; }
        M.me.x = mx; M.me.y = my; M.me.vx = M.me.vy = 0; M.me.nefes = nefes; M.durgunT = durgun;
        M.s = new Shuttle { x = x, y = y, z = z, from = "op", type = "clear" }; return M;
    }
    static SwipeInput HileJest(HileTanim t)
    {
        switch (t.jestKod)
        {
            case "kavis": return new SwipeInput { dx = 10, dy = -100, speed = .5f, curved = true, kavis = 1, R = 40, dist = 4 };
            case "terskavis": return new SwipeInput { dx = 10, dy = -100, speed = .5f, curved = true, kavis = -1, R = 40, dist = 4 };
            case "bekle_hizli": return new SwipeInput { dx = 0, dy = -150, speed = 1.3f, bekle = .4f, R = 40, dist = 4 };
            case "bekle_yavas": return new SwipeInput { dx = 0, dy = -120, speed = .5f, bekle = .4f, R = 40, dist = 4 };
            case "kirik": return new SwipeInput { dx = 80, dy = -60, speed = .6f, curved = true, kirik = true, R = 40, dist = 4 };
            default: return new SwipeInput { dx = 200, dy = -40, speed = 1.6f, R = 40, dist = 4 };
        }
    }
    static void Aldatmalar()
    {
        var L = Aldatma.Liste; if (L.Length < 5) Fail("aldatıcı vuruş en az 5 olmalı: " + L.Length);
        for (int i = 1; i < L.Length; i++) if (L[i].sv <= L[i - 1].sv) Fail("aldatma seviyeleri artan sırada değil: " + L[i].id);
        if (L[0].sv < 5) Fail("aldatma üst seviyede açılmalı (ilk Sv " + L[0].sv + ")");
        foreach (var t in L)
        {
            var J = HileJest(t);
            // kapılar: seviye, ekipman, konum (nefes yok), uzanma; üçü tutunca kabul
            var D = HileSahne(t, t.sv - 1, HILE_IYI, 1).HileDenetle(t); if (D.ok || D.neden != "seviye") Fail(t.id + ": seviyesinin altında kabul edildi (" + D.neden + ")");
            D = HileSahne(t, 30, HILE_KOTU, 1).HileDenetle(t); if (D.ok || D.neden != "ekipman") Fail(t.id + ": ekipmansız kabul edildi (" + D.neden + ")");
            D = HileSahne(t, 30, HILE_IYI, 1, null, 1, .1f).HileDenetle(t); if (D.ok || D.neden != "konum") Fail(t.id + ": nefessiz konumda kabul edildi (" + D.neden + ")");
            if (t.baglam != "bh") { D = HileSahne(t, 30, HILE_IYI, 1, null, 0).HileDenetle(t); if (D.ok || D.kosul != "sure") Fail(t.id + ": topa yerleşmeden kabul edildi (" + D.kosul + ")"); }
            var Mu = HileSahne(t, 30, HILE_IYI, 1); Mu.me.x = Mu.s.x + (t.baglam == "bh" ? 2.0f : -1.9f); Mu.s.z = Math.Min(Mu.s.z, 1.3f); D = Mu.HileDenetle(t); if (D.ok) Fail(t.id + ": uzanırken kabul edildi");
            D = HileSahne(t, 30, HILE_IYI, 1).HileDenetle(t); if (!D.ok || D.pay < .99f) Fail(t.id + ": ideal konumda reddedildi (" + D.neden + "/" + D.kosul + ")");
            if (!Aldatma.Jest(t, J)) Fail(t.id + ": kendi jesti tanınmadı");
            foreach (var u in L) if (u != t && Aldatma.Jest(u, J) && u.baglam == t.baglam && u.sv > t.sv) Fail(t.id + ": jesti " + u.id + " ile çakışıyor");
            // vuruş: seviye/ekipman yoksa normal vuruş, varsa aldatma + etki
            var M0 = HileSahne(t, t.sv - 1, HILE_IYI, 2); M0.SwipeShot(J); if (M0.sonHile != null || M0.events.Any(e => e.kind == Ev.Aldat && e.type == t.id)) Fail(t.id + ": seviyesiz vuruş aldattı");
            int yapildi = 0, etki = 0; float enK = 9, enB = 0;
            for (int k = 0; k < 40; k++)
            {
                var M = HileSahne(t, 30, HILE_IYI, 10 + k); float c0 = M.clock; if (!M.SwipeShot(J)) { Fail(t.id + ": vuruş olmadı"); break; }
                if (M.sonHile == t.id) yapildi++;
                if (M.events.Any(e => e.kind == Ev.Aldat && e.type == t.id)) { etki++; float f = M.op.fakeUntil - c0; enK = Math.Min(enK, f); enB = Math.Max(enB, f); if (f <= 0) Fail(t.id + ": rakip yanlış adım atmadı"); }
            }
            if (yapildi != 40) Fail(t.id + ": ideal konumda " + yapildi + "/40 aldatma");
            if (etki < 40 * (1 - t.riskTaban) - 8) Fail(t.id + ": etki az uygulandı " + etki + "/40");
            // üst tur rakibi daha az aldanır (aynı tohum, ideal konum; kötüye dönmeyen vuruşlar)
            float alt = 0, ust = 0; int na = 0, nu = 0;
            for (int k = 0; k < 30; k++)
            {
                var Ma = HileSahne(t, 30, HILE_IYI, 60 + k, Match.KariyerRakip(0)); Ma.SwipeShot(J); if (Ma.events.Any(e => e.kind == Ev.Aldat)) { alt += Ma.op.fakeUntil - Ma.clock; na++; }
                var Mb = HileSahne(t, 30, HILE_IYI, 60 + k, Match.KariyerRakip(20)); Mb.SwipeShot(J); if (Mb.events.Any(e => e.kind == Ev.Aldat)) { ust += Mb.op.fakeUntil - Mb.clock; nu++; }
            }
            if (na == 0 || nu == 0 || ust / nu >= alt / na * .8f) Fail(t.id + ": üst tur rakibinde etki küçülmedi " + (na > 0 ? alt / na : 0).ToString("0.00") + " → " + (nu > 0 ? ust / nu : 0).ToString("0.00"));
            // risk: marjinal konum (yerleşme süresi sınıra yakın) daha çok kötü vuruş ve dışarı/file
            float[] su; t.kosul.TryGetValue("sure", out su); float marj = su != null ? su[0] + (su[1] - su[0]) * .1f : 1; float marjNefes = t.kosul["nefes"][0] + (t.kosul["nefes"][1] - t.kosul["nefes"][0]) * .1f;
            int kotuI = 0, kotuM = 0, hataI = 0, hataM = 0, N = 150;
            for (int k = 0; k < N; k++)
            {
                var Mi = HileSahne(t, 30, HILE_IYI, 300 + k); Mi.SwipeShot(J); if (Mi.sonQ == "poor") kotuI++; if (Hata(Mi)) hataI++;
                var Mm = HileSahne(t, 30, HILE_IYI, 300 + k, null, su != null && su[0] > 0 ? marj : 1, marjNefes); var Dm = Mm.HileDenetle(t); if (!Dm.ok || Dm.pay > .2f) { Fail(t.id + ": marjinal sahne kurulamadı " + Dm.pay); break; }
                Mm.SwipeShot(J); if (Mm.sonQ == "poor") kotuM++; if (Hata(Mm)) hataM++;
            }
            if (kotuM <= kotuI + N / 10) Fail(t.id + ": marjinal konumda kötü vuruş artmadı " + kotuI + " → " + kotuM);
            if (hataM < hataI) Fail(t.id + ": marjinal konumda hata azaldı " + hataI + " → " + hataM);
            Console.WriteLine("aldatma " + t.id.PadRight(11) + " Sv " + t.sv.ToString().PadLeft(2) + "  " + t.ekStat + "≥" + t.ekMin + "  etki " + etki + "/40 yanlış adım " + enK.ToString("0.00") + "–" + enB.ToString("0.00") + " s, Tur1→Tur4 " + (alt / na).ToString("0.00") + "→" + (ust / nu).ToString("0.00") + "  kötü vuruş ideal " + kotuI + " marjinal " + kotuM + "/" + N + ", dışarı/file " + hataI + " → " + hataM);
        }
        // jest okuma: tutma, V, yay yönü
        {
            var sw = new SwipeInput(); float[] x = { 100, 100, 101, 100, 104, 115, 130 }, y = { 300, 300, 301, 300, 280, 240, 200 }, tt = { 0, 100, 200, 350, 370, 390, 410 };
            Aldatma.JestOku(x, y, tt, x.Length, 100, 300, 0, ref sw); if (sw.bekle < .3f || sw.kirik) Fail("tutma jesti okunmadı " + sw.bekle);
            float[] vx = { 100, 120, 140, 160, 140, 120, 100 }, vy = { 300, 270, 240, 210, 180, 150, 120 }, vt = { 0, 16, 32, 48, 64, 80, 96 };
            sw = new SwipeInput(); Aldatma.JestOku(vx, vy, vt, vx.Length, 100, 300, 0, ref sw); if (sw.kirik || sw.kavis == 0) Fail("yay okunmadı");
            float[] kx = { 100, 140, 180, 220, 180, 140, 100 }, ky = { 300, 295, 290, 285, 280, 275, 270 };
            sw = new SwipeInput(); Aldatma.JestOku(kx, ky, vt, kx.Length, 100, 300, 0, ref sw); if (!sw.kirik) Fail("V jesti okunmadı");
            float[] sx = { 100, 100, 100, 100, 70, 40, 10 }, sy = { 300, 270, 240, 210, 200, 190, 180 };   // yukarı, sonra sola bükülen yay
            sw = new SwipeInput(); Aldatma.JestOku(sx, sy, vt, sx.Length, 100, 300, 0, ref sw); if (sw.kavis != -1) Fail("sola bükülen yay -1 değil: " + sw.kavis);
            if (Aldatma.Jest(Aldatma.Bul("cevirme"), new SwipeInput { tap = true, curved = true, speed = .5f })) Fail("dokunma aldatma sayıldı");
        }
        // görev: havuza yalnız açılmış oyuncuda
        int gorAcik = 0, gorKapali = 0;
        for (int m = 0; m < 40; m++)
        {
            Match.OyuncuSv = 30; var A = new Match(40 + m, new MatchOptions { easy = true, target = 5 }); Match.OyuncuSv = 1; if (A.gorevler.Any(g => g.id == "hile")) gorAcik++;
            var B = new Match(40 + m, new MatchOptions { easy = true, target = 5 }); if (B.gorevler.Any(g => g.id == "hile")) gorKapali++;
        }
        if (gorAcik == 0 || gorKapali > 0) Fail("aldatma görevi havuzu: açık " + gorAcik + ", kapalı " + gorKapali);
        // rakip: Tur 3+ ve Usta'da sıklık sınırı; alt tur ve serbest maçta hiç; hazırlık görünür; okunabilir
        var R = Aldatma.Rakip; int toplam = 0, enCok = 0, okunan = 0, altTur = 0, aralikHata = 0; float enKisa = 9;
        var turler = new Dictionary<string, int>();
        for (int m = 0; m < 36; m++)
        {
            Style st; if (m % 6 == 5) { st = Match.STIL["vex"].Clone(); st.usta = 2; st.kariyer = -1; } else st = Match.KariyerRakip(12 + m % 12);
            var M = new Match(1200 + m, new MatchOptions { easy = m % 2 == 0, target = 11, stil = st }); int g = 0, son = -99, say = 0;
            while (!M.over && g++ < 60 * 60 * 30)
            {
                int once = M.hileRSay; Bot(M, 1f / 60f, .14); M.Update(1f / 60f); M.events.Clear();
                if (M.hileRSay > once) { int pu = M.sc[0] + M.sc[1]; if (pu - son < R.ara) aralikHata++; son = pu; say++; enKisa = Math.Min(enKisa, M.op.imzaT - (M.easy ? .2f : 0)); }
            }
            toplam += say; enCok = Math.Max(enCok, say); okunan += M.hileOkunan;
        }
        for (int m = 0; m < 8; m++)
        {
            var M = new Match(1300 + m, new MatchOptions { easy = true, target = 5, stil = m < 6 ? Match.KariyerRakip(m * 2) : null }); int g = 0;
            while (!M.over && g++ < 60 * 60 * 20) { Bot(M, 1f / 60f); M.Update(1f / 60f); M.events.Clear(); }
            altTur += M.hileRSay;
        }
        Console.WriteLine("rakip aldatma: 36 Tur 3+/Usta maçında " + toplam + " (maçta en çok " + enCok + "), okunan " + okunan + ", en kısa hazırlık " + enKisa.ToString("0.00") + " s; alt tur/serbest " + altTur);
        if (toplam == 0) Fail("rakip hiç aldatmadı");
        if (enCok > R.azami) Fail("rakip maçta " + enCok + " aldatma (en çok " + R.azami + ")");
        if (aralikHata > 0) Fail("rakip aldatmaları arası " + R.ara + " sayıdan az: " + aralikHata);
        if (enKisa < R.hazirlik - 1e-3f) Fail("rakip aldatma hazırlığı " + R.hazirlik + " sn'den kısa: " + enKisa);
        if (altTur > 0) Fail("Tur 1–2 ya da serbest maçta rakip aldattı: " + altTur);
        if (okunan == 0) Fail("rakibin aldatması hiç okunmadı");
        Match.OyuncuSv = 1;
    }
    static bool Hata(Match M) { var p = M.s != null ? M.s.landPred : null; return p == null || (p.net != null && !p.net.ok) || M.IsOut(p.x, p.y, false); }

    // Vuruş anı ağır çekimi: yumuşak iniş/çıkış, yeni taban, Hareketi azalt zorluğu değiştirmez (2026-10-26).
    static void Pencereler()
    {
        const float dt = 1f / 60f;
        foreach (var kolay in new[] { true, false })
            foreach (var reduce in new[] { false, true })
            {
                var M = new Match(5, new MatchOptions { easy = kolay, reduce = reduce });
                float taban = kolay ? .45f : .6f, k1 = M.PencereAdim(dt, true), tIn = dt;
                if (Math.Abs(M.PencereTaban() - taban) > 1e-4f) Fail("pencere tabanı " + M.PencereTaban() + " (beklenen " + taban + ")");
                if (k1 <= taban + .05f) Fail("ağır çekim tek karede indi: " + k1);
                while (M.pencereK > taban + 1e-4f && tIn < 1) { M.PencereAdim(dt, true); tIn += dt; }
                if (Math.Abs(tIn - Match.PENCERE_IN) > dt * 1.5f) Fail("iniş süresi " + tIn.ToString("0.000") + " sn");
                float u1 = M.PencereAdim(dt, false), tOut = dt;
                if (u1 >= 1 - .05f) Fail("ağır çekim tek karede kalktı: " + u1);
                while (M.pencereK < 1 - 1e-4f && tOut < 1) { M.PencereAdim(dt, false); tOut += dt; }
                if (Math.Abs(tOut - Match.PENCERE_OUT) > dt * 1.5f) Fail("çıkış süresi " + tOut.ToString("0.000") + " sn");
                Console.WriteLine("pencere " + (kolay ? "kolay" : "usta ") + (reduce ? " azalt" : "      ") + ": taban " + taban + ", ilk kare " + k1.ToString("0.000") + ", iniş " + tIn.ToString("0.000") + " sn, çıkış " + tOut.ToString("0.000") + " sn");
            }
        var O = new Match(5, new MatchOptions { easy = true, gs = new GearStats { yavas = .85f } });
        if (Math.Abs(O.PencereTaban() - .45f * .85f) > 1e-4f) Fail("Odak bandı oranı bozuldu: " + O.PencereTaban());
        // Gerçek Update yolu: Hareketi azalt açıkken de pencerede hız tabana iner (eski .7 dalı yok).
        var R = new Match(9, new MatchOptions { easy = true, reduce = true }); float enAz = 1; int g = 0;
        while (!R.over && g++ < 60 * 120) { Bot(R, dt, .05); R.Update(dt); enAz = Math.Min(enAz, R.pencereK); R.events.Clear(); }
        if (Math.Abs(enAz - .45f) > 1e-3f) Fail("Hareketi azalt açıkken pencere hızı " + enAz + " (beklenen .45)");
        // 2026-10-27: rampa pencereden önce biter (pencere açıldığı karede hız tabanda), vuruşla kapanan pencere hızlı çıkar.
        foreach (var kolay in new[] { true, false })
        {
            int acilis = 0, tamTaban = 0, vurus = 0, gecCikis = 0; float oranEnAz = 9, oranTop = 0; int oranN = 0;
            for (int m = 0; m < 4; m++)
            {
                var M = new Match(70 + m, new MatchOptions { easy = kolay, target = 11 }); int gg = 0; bool w0 = false;
                float izT = 0, izOyun = 0; Shuttle izS = null;
                while (!M.over && gg++ < 60 * 60 * 15)
                {
                    if (!kolay && M.s != null && M.s.from == "op" && M.icpt != null && M.icpt.who == "me") M.RunTo(M.icpt.x + .4f, M.icpt.y, .5f);
                    var sOnce = M.s; Bot(M, dt, .2);
                    M.Update(dt); bool w = M.HitWindow();
                    if (w && !w0) { acilis++; if (M.pencereK <= M.PencereTaban() + .02f) tamTaban++; else if (Environment.GetEnvironmentVariable("PENCERE_IZ") != null) Console.WriteLine("  geç açılış k=" + M.pencereK.ToString("0.00") + " z=" + M.s.z.ToString("0.00") + " y-file=" + (M.s.y - Court.NET).ToString("0.00") + " v=" + M.s.Speed.ToString("0.0") + " tür=" + M.s.type + " t=" + M.s.t.ToString("0.00")); }
                    w0 = w;
                    // benim vuruşum: pencere PENCERE_VURUS_OUT (+1 kare) içinde 1'e döner; smaçta ilk 0,1 sn gerçek zamanda geçen
                    // oyun zamanı (top uçuşunun s.t'si). Vuruş donması (hit-stop, smaçta .08 sn) kasıtlıdır: süre top akınca başlar.
                    if (M.s != null && M.s != sOnce && M.s.from == "me" && !M.s.serve) { vurus++; izT = -2; izS = M.s; }
                    else if (izS != null && M.s == izS && (M.s.dead || M.s.netHit || M.slow < 1)) izS = null;   // kısa uçuş (öldürme, file) ve "Kıl payı" ağır çekimi (slow, ayrı kural): ölçülmez
                    else if (izS != null && M.s == izS && M.freeze <= 0)
                    {
                        if (izT == -2) { izT = 0; izOyun = M.s.t; }
                        else
                        {
                            izT += dt;
                            if (izT > Match.PENCERE_VURUS_OUT + dt * 1.5f && izT < Match.PENCERE_VURUS_OUT + 2.5f * dt && M.pencereK < 1) gecCikis++;
                            if (izT >= .1f - 1e-4f) { if (izS.type == "smash") { float o = (M.s.t - izOyun) / izT; oranEnAz = Math.Min(oranEnAz, o); oranTop += o; oranN++; } izS = null; }
                        }
                    }
                    M.events.Clear();
                }
            }
            float pay = acilis > 0 ? tamTaban / (float)acilis : 0;
            Console.WriteLine("pencere " + (kolay ? "kolay" : "usta ") + " maçta: " + acilis + " açılış, tabanda açılan %" + (pay * 100).ToString("0") + "; " + vurus + " vuruş, geç çıkış " + gecCikis + "; smaç ilk 0,1 sn oyun/gerçek zaman ort. " + (oranN > 0 ? (oranTop / oranN).ToString("0.00") : "-") + " en az " + (oranN > 0 ? oranEnAz.ToString("0.00") : "-") + " (" + oranN + " smaç)");
            if (acilis < 50 || pay < .75f) Fail("ağır çekim rampası pencere açılmadan bitmiyor: %" + (pay * 100).ToString("0"));
            if (gecCikis > 0) Fail("vuruştan sonra pencere geç kalktı: " + gecCikis);
            if (oranN == 0 || oranEnAz < .9f) Fail("smaç çıkışta ağır çekimde kalıyor: " + oranEnAz);
        }
    }

    // Top fiziği gerçek tüy topla (2026-10-27): Cohen vd. 2015 New J. Phys. 17 063001; arastirma/vurus_biyomekanigi.md §12.
    // Ayrıntılı tablo: tool/unity_test/FizikOlc.cs.
    static void FizikOlcumleri()
    {
        Func<float, float, float, float, float, float, float, float[]> uc = (hx, hy, hz, vx, vy, vz, kd) =>
        {   // tepe, tepeye süre, uçuş süresi, iniş-file, düşüş açısı (°), iniş hızı, 3,17 m yol sonrası hız / çıkış hızı
            float x = hx, y = hy, z = hz, wx = vx, wy = vy, wz = vz, t = 0, ap = hz, tA = 0, yol = 0, v0 = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz), vYari = float.NaN;
            while (z > 0 && t < 8)
            {
                float px = x, py = y, pz = z; Shuttle.Step(ref x, ref y, ref z, ref wx, ref wy, ref wz, kd, Shuttle.DT); t += Shuttle.DT;
                yol += (float)Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py) + (z - pz) * (z - pz)); if (z > ap) { ap = z; tA = t; }
                if (float.IsNaN(vYari) && yol >= Shuttle.VT * Shuttle.VT / Shuttle.GR * (float)Math.Log(2)) vYari = (float)Math.Sqrt(wx * wx + wy * wy + wz * wz) / v0;
            }
            float vh = (float)Math.Sqrt(wx * wx + wy * wy);
            return new[] { ap, tA, t, Math.Abs(y - Court.NET), (float)(Math.Atan2(-wz, vh) * 180 / Math.PI), (float)Math.Sqrt(vh * vh + wz * wz), vYari };
        };
        Func<float, float, float, float, float, bool, float[]> ben = (hy, hz, sp, dy, energy, kill) =>
        {
            var M = new Match(1, new MatchOptions { easy = false }); M.energy = energy;
            M.s = new Shuttle { x = Court.MID, y = hy, z = hz, vy = 3, vz = -2, from = "op" }; M.me.x = Court.MID - .4f; M.me.y = hy;
            var P = M.ShotPlan(new SwipeInput { dy = dy, speed = sp, R = 40 }, new Reach { kill = kill }, true);
            float vx, vy, vz; Shuttle.Aim(P.hx, P.hy, P.hz, P.tx, Court.NET - P.d, P.byAngle, P.val, out vx, out vy, out vz);
            var r = uc(P.hx, P.hy, P.hz, vx, vy, vz, Shuttle.KD); Array.Resize(ref r, 8); r[7] = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz); return r;
        };
        // son hız
        { float x = 0, y = 0, z = 30, vx = 0, vy = 0, vz = 0; for (int i = 0; i < 240 * 6; i++) Shuttle.Step(ref x, ref y, ref z, ref vx, ref vy, ref vz, Shuttle.KD, Shuttle.DT); if (-vz < 6.5f || -vz > 6.9f) Fail("son hız " + (-vz) + " m/s (tüy top 6,5–6,9)"); }
        float L = Court.L, N = Court.NET;
        var cl = ben(L - 1.0f, 2.55f, 1.2f, -150, 0, false);
        var sm = ben(L - 2.5f, 2.6f, 2.2f, -150, 0, false);
        var dr = ben(L - 1.5f, 2.5f, .5f, -150, 0, false);
        var nt = ben(N + .6f, 1.2f, .5f, -150, 0, false);
        Console.WriteLine("fizik: clear tepe " + cl[0].ToString("0.0") + " m @" + cl[1].ToString("0.00") + " s, uçuş " + cl[2].ToString("0.00") + " s, düşüş " + cl[4].ToString("0") + "°, iniş " + cl[5].ToString("0.0") + " m/s, dip çizgiye " + (L / 2 - cl[3]).ToString("0.00") + " m; smaç " + sm[7].ToString("0") + "→" + sm[5].ToString("0.0") + " m/s, 3,2 m'de ×" + sm[6].ToString("0.00") + "; drop " + dr[3].ToString("0.00") + " m, file vuruşu " + nt[3].ToString("0.00") + " m (tepe " + nt[0].ToString("0.00") + ")");
        if (cl[0] < 5 || cl[0] > 9.5f) Fail("clear tepesi " + cl[0] + " m");
        if (cl[2] - cl[1] < 1.6f * cl[1]) Fail("clear Tartaglia değil: çıkış " + cl[1] + " s, iniş " + (cl[2] - cl[1]) + " s");
        if (cl[4] < 75) Fail("clear düşüşü yatık: " + cl[4] + "°");
        if (L / 2 - cl[3] > 1.0f || cl[3] > L / 2) Fail("clear dip çizgiye düşmüyor: " + cl[3]);
        if (sm[5] > .25f * sm[7]) Fail("smaç inişte yavaşlamıyor: " + sm[7] + " → " + sm[5]);
        if (Math.Abs(sm[6] - .5f) > .08f) Fail("hız 3,2 m'de yarılanmıyor: ×" + sm[6]);
        if (dr[3] > Court.SSL || nt[3] > Court.SSL) Fail("drop/file vuruşu kısa düşmüyor: " + dr[3] + " / " + nt[3]);
        if (nt[0] > Court.NETH + .35f) Fail("file vuruşu fazla yüksek: " + nt[0]);
        // Rakibin slice'ı (çevirme file, Tur 3+): kendi sürüklenmesiyle nişanlanır, fileye gitmez. Maçta seyrek çıkar (rakip
        // file önünde topu alçakta, z < 0,75 alır; çevirme 0,95–1,75 ister), o yüzden vuruş doğrudan kurulur (yansımayla:
        // RakipHileVurus özel). Konum ve yükseklik aldatma.json "cevirme" koşulunun bandını tarar.
        int cev = 0, cevFile = 0; var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var tC = Aldatma.Bul("cevirme"); var mVur = typeof(Match).GetMethod("RakipHileVurus", bf);
        for (int k = 0; k < 60; k++)
        {
            var M = new Match(1500 + k, new MatchOptions { easy = k % 2 == 0, target = 11, stil = Match.KariyerRakip(12 + k % 12) });
            float hy = Court.NET - (.2f + 1.4f * (k % 6) / 5f), hz = .95f + .8f * (k / 6 % 5) / 4f;
            M.s = new Shuttle { x = Court.MID + (k % 3 - 1) * .8f, y = hy, z = hz, vx = 0, vy = -2, vz = -1, from = "me" }; M.op.x = M.s.x + .3f; M.op.y = hy - .4f;
            var I = new Intercept { t = 0, x = M.s.x, y = hy, z = hz, who = "op", opType = "drop" }; M.icpt = I;
            typeof(Match).GetField("hileR", bf).SetValue(M, tC); typeof(Match).GetField("hileI", bf).SetValue(M, I); typeof(Match).GetField("hileBasladi", bf).SetValue(M, true);
            if (!(bool)mVur.Invoke(M, new object[] { I }) || M.s.from != "op" || !M.s.slice) { Fail("çevirme vuruşu kurulamadı"); break; }
            cev++; if (M.s.landPred.net != null && !M.s.landPred.net.ok) cevFile++;
        }
        Console.WriteLine("rakip slice (çevirme): " + cev + " vuruş, fileye " + cevFile);
        if (cev == 0) Fail("rakip slice'ı hiç görülmedi");
        else if (cevFile > cev / 10) Fail("rakip slice'ı fileye gidiyor: " + cevFile + "/" + cev);
    }

    // Ayak işi (2026-10-26 gece, build 40 incelemesi; ayrıntılı ölçüm tool/unity_test/AnimOlc): ayak işi klibinin fazı
    // gövdenin bu karede kat ettiği yolla ilerler (faz hızı × adım boyu = gövde hızı; eskiden yumuşatılmış hızla, kalkış
    // ve duruşta 1,5–2 m/s kayma), vuruş donmasında poz donar, tabana varışta gövde sallanmaz (boşta ↔ öne adım titremesi),
    // görünen gövde dikeyde ışınlanmaz (sıçrayarak smaçta yay ortasından başlıyordu; Match.GORSEL_YUKSELIS).
    static void AyakIsleri()
    {
        const float dt = 1f / 60f; int kare = 0, kayan = 0, titreme = 0, donmaKayma = 0, dikey = 0;
        for (int m = 0; m < 4; m++)
        {
            var M = new Match(400 + m, new MatchOptions { easy = m != 3, target = 5 }); int g = 0;
            var px = new float[2]; var py = new float[2]; var ph0 = new float[2]; var son = new string[2]; var once = new string[2]; var degT = new float[2]; var jg = new float[2]; float t = 0;
            for (int i = 0; i < 2; i++) { px[i] = float.NaN; degT[i] = -9; }
            while (!M.over && g++ < 60 * 60 * 20)
            {
                Bot(M, dt); bool donuk = M.freeze > 0; M.Update(dt); t += dt;
                for (int i = 0; i < 2; i++)
                {
                    var a = i == 0 ? M.me : M.op; string c; float ph; M.PickClip(a, i == 0, out c, out ph);
                    float v = float.IsNaN(px[i]) ? 0 : Hyp(a.x - px[i], a.y - py[i]) / dt;
                    float boy; bool ayak = Match.ADIM_BOY.TryGetValue(c, out boy);
                    if (ayak && son[i] == c && v < 12 && !M.between && M.s != null)
                    {
                        // 2026-10-27 (ekran/kosu): faz işaretli ve klibin eksenine düşen hızla ilerler (ters gidişte geri sarar);
                        // karşılaştırma gövde hızının o eksendeki bileşeniyle. Eksene dik bileşen (yön farkı) KosuYonleri'nde.
                        float dph = ph - ph0[i]; dph -= (float)Math.Floor(dph + .5f);
                        float ey, ei; Match.KlipEkseni(c, out ey, out ei); double r = a.donusAyak * Math.PI / 180;
                        float vx = (a.x - px[i]) / dt, vy = (a.y - py[i]) / dt, yan = i == 0 ? vx : -vx, ileri = i == 0 ? -vy : vy;
                        float vEk = yan * (ey * (float)Math.Cos(r) + ei * (float)Math.Sin(r)) + ileri * (-ey * (float)Math.Sin(r) + ei * (float)Math.Cos(r));
                        kare++; if (Math.Abs(dph * boy / dt - vEk) > .3f) kayan++;
                    }
                    // donmada vuruş (temas karesi) ve karşı oyuncunun split'i başlayabilir; boşta pozuna düşmek ya da ayak işi klibine geçmek olmaz
                    if (donuk && son[i] != null && c != son[i] && !M.between && (c == "idle" || Match.ADIM_BOY.ContainsKey(c))) donmaKayma++;
                    if (c != son[i]) { if (son[i] != null && (c == "idle" && once[i] == "fwd" || c == "fwd" && once[i] == "idle") && c == once[i] && t - degT[i] < .15f) titreme++; once[i] = son[i]; son[i] = c; degT[i] = t; }
                    { float j = Match.JumpH(a, c == "split") * .5f, j1 = j > jg[i] ? Math.Min(j, jg[i] + Match.GORSEL_YUKSELIS * dt) : j; if (Math.Abs(j1 - jg[i]) > .12f) dikey++; jg[i] = j1; }   // PlayerView gövde yüksekliği
                    px[i] = a.x; py[i] = a.y; ph0[i] = ph;
                }
                M.events.Clear();
            }
        }
        float oran = 100f * kayan / Math.Max(1, kare);
        Console.WriteLine("ayak işi: " + kare + " kare, faz/gövde hızı farkı > 0,3 m/s %" + oran.ToString("0.0") + ", boşta↔öne adım titremesi " + titreme + ", donmada boşta/ayak işine geçiş " + donmaKayma + ", görünen gövdede kare başına > 12 cm dikey sıçrama " + dikey);
        if (oran > 3) Fail("ayak işi fazı gövde hızını izlemiyor: %" + oran.ToString("0.0"));
        if (titreme > 40) Fail("tabanda boşta↔öne adım titremesi: " + titreme);
        if (donmaKayma > 0) Fail("vuruş donmasında boşta/ayak işi klibine geçti: " + donmaKayma);
        if (dikey > 0) Fail("gövde dikeyde ışınlandı (sıçrama yayı ortasından başladı): " + dikey);
    }
    // Koşu yönü (2026-10-27, ekran/kosu; yasin build 41: "oyuncu arka arkaya koşarken garip koşuyor"). AyakIsleri faz
    // hızının BÜYÜKLÜĞÜNE bakıyordu; burada bacakları süren klibin gidiş VEKTÖRÜ (klip ekseni, Athlete.donusAyak kadar
    // dönük; faz hızı × adım boyu) gövdenin gerçek hız vektörüyle karşılaştırılır: fark yerdeki ayağın kayması. Vuruş
    // klibinde koşarken bacaklar alt katmandan (Match.AltHedef > .5 ise ayakKlip/runPh) sayılır. Ölçülenler (60 ve 120 Hz):
    //  - vektör kayma ortalaması ve > 0,3 m/s kare oranı (önce ben 2,0 m/s / %87, rakip 1,0 m/s / %75);
    //  - ters adım: bacaklar gövdenin gittiği yönün tersine adımlıyor (önce maç başına ~300 kare: hamleden geri çekilmede ileri koşu);
    //  - geri çekilme (gidiş fileden > 120°, > 2 m/s): yan dönük (|donusAyak| > 45°) pay, fileye bakıp geri geri (back_*) pay
    //    (önce %0 / %82–86);
    //  - temasta gövde dönüşü (vuruş klipleri fileye bakarak çizildi) ve dönüş hızı tavanı (Match.DON_AZAMI: tek karede dönme yok).
    static void KosuYonleri()
    {
        foreach (float hz in new[] { 60f, 120f })
        {
            float dt = 1f / hz; var R = new Random(7);
            int kare = 0, kay3 = 0, ters = 0, geri = 0, geriYan = 0, geriSirt = 0, donAsim = 0; double kayTop = 0; var temas = new List<float>();
            for (int m = 0; m < 6; m++)
            {
                var M = new Match(300 + m, new MatchOptions { easy = m % 3 != 2, target = 11 }); int g = 0;
                var px = new[] { float.NaN, float.NaN }; var py = new float[2]; var ph0 = new float[2]; var c0 = new string[2]; var d0 = new float[2];
                while (!M.over && g++ < (int)(hz * 60 * 30))
                {
                    if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && R.NextDouble() < .05) M.SwipeServe((float)(R.NextDouble() * 160 - 80), -200, 1.8f);
                    Reach rc; if (M.ReachInfo(out rc) && R.NextDouble() < .12) M.SwipeShot(new SwipeInput { dx = (float)(R.NextDouble() * 200 - 100), dy = -(float)(80 + R.NextDouble() * 220), speed = R.NextDouble() < .5 ? 1.2f : 2.2f, R = 40, dist = (float)(R.NextDouble() * 60), side = R.NextDouble() < .5 ? -1 : 1 });
                    M.Update(dt);
                    for (int i = 0; i < 2; i++)
                    {
                        var a = i == 0 ? M.me : M.op; bool isMe = i == 0; string c; float ph; M.PickClip(a, isMe, out c, out ph);
                        if (!Match.ADIM_BOY.ContainsKey(c) && a.ayakKlip != null && Match.AltHedef(a, c) > .5f) { c = a.ayakKlip; ph = a.runPh; }
                        bool isin = !float.IsNaN(px[i]) && Hyp(a.x - px[i], a.y - py[i]) > 12 * dt;   // servis dizilişi ışınlar: dönüş sıfırlanır
                        if (!isin && !float.IsNaN(px[i]) && (Math.Abs(a.donusAyak - d0[i]) > Match.DON_AZAMI * dt + .01f || Math.Abs(a.donusAyak - a.donus) > Match.DON_KALCA + .01f)) donAsim++;
                        float ey, ei, boy; Match.KlipEkseni(c, out ey, out ei);
                        if (!float.IsNaN(px[i]) && c == c0[i] && (ey != 0 || ei != 0) && Match.ADIM_BOY.TryGetValue(c, out boy) && !M.between && M.s != null)
                        {
                            float vx = (a.x - px[i]) / dt, vy = (a.y - py[i]) / dt, yan = isMe ? vx : -vx, ileri = isMe ? -vy : vy, v = Hyp(yan, ileri);
                            if (v < 12)
                            {
                                float dph = ph - ph0[i]; dph -= (float)Math.Floor(dph + .5f);
                                double r = a.donusAyak * Math.PI / 180; float fc = (float)Math.Cos(r), fs = (float)Math.Sin(r);
                                float ax = ey * fc + ei * fs, ay = -ey * fs + ei * fc, cv = dph * boy / dt, k = Hyp(yan - ax * cv, ileri - ay * cv);
                                kare++; kayTop += k; if (k > .3f) kay3++;
                                if (v > .6f && (ax * yan + ay * ileri) * cv < 0) ters++;
                                if (v > 2 && Math.Abs(Math.Atan2(yan, ileri)) > 120 * Math.PI / 180) { geri++; if (Math.Abs(a.donusAyak) > 45) geriYan++; if (c.StartsWith("back")) geriSirt++; }
                            }
                        }
                        px[i] = a.x; py[i] = a.y; ph0[i] = ph; c0[i] = c; d0[i] = a.donusAyak;
                    }
                    foreach (var e in M.events) if (e.kind == Ev.Hit && !(M.s != null && M.s.serve)) temas.Add(Math.Abs((e.who == "me" ? M.me : M.op).donus));
                    M.events.Clear();
                }
            }
            temas.Sort(); float t90 = temas.Count > 0 ? temas[(int)(temas.Count * .9)] : 0, ort = (float)(kayTop / Math.Max(1, kare)), o3 = 100f * kay3 / Math.Max(1, kare);
            float yanP = 100f * geriYan / Math.Max(1, geri), sirtP = 100f * geriSirt / Math.Max(1, geri);
            Console.WriteLine("koşu yönü " + hz + " Hz: " + kare + " kare, vektör kayma ort " + ort.ToString("0.00") + " m/s, > 0,3 m/s %" + o3.ToString("0.0") + ", ters adım " + ters
                + "; geri çekilme " + geri + " kare: yan dönük %" + yanP.ToString("0") + ", fileye bakıp geri geri %" + sirtP.ToString("0") + "; temasta gövde dönüşü p90 " + t90.ToString("0.0") + "°, dönüş tavanı aşımı " + donAsim);
            if (ort > .45f) Fail("koşu: bacak klibinin yönü gövdenin gidişini izlemiyor, ort. kayma " + ort.ToString("0.00") + " m/s");
            if (o3 > 30) Fail("koşu: ayak işi karelerinin %" + o3.ToString("0") + "'inde > 0,3 m/s kayma");
            if (ters > 0) Fail("koşu: ters adım (bacaklar gidişin tersine) " + ters + " kare");
            if (geri < 200) Fail("koşu: geri çekilme ölçülemedi (" + geri + " kare)");
            else { if (yanP < 70) Fail("koşu: arka sahaya yan dönmeden gidiyor (yan dönük %" + yanP.ToString("0") + ")"); if (sirtP > 10) Fail("koşu: arka sahaya fileye bakıp geri geri (%" + sirtP.ToString("0") + ")"); }
            if (t90 > 5) Fail("koşu: temasta gövde dönük (p90 " + t90.ToString("0.0") + "°)");
            if (donAsim > 0) Fail("koşu: dönüş hızı/kalça ayrışması tavanı aşıldı " + donAsim);
        }
    }
    static float Hyp(float x, float y) { return (float)Math.Sqrt(x * x + y * y); }
}
