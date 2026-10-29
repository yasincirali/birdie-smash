// Mağaza görselleri adım 1 (2026-10-29, Ar-Ge 27 Eki madde 1, yasin "1-3 ü yap"): ekran turunun "mağaza" kipi.
// `-bsekran <klasör> -bskip magaza` ile yalnız bu tur koşar (unity-ekran.yml ikinci oyuncu çalıştırması).
// Neden ekran turundan, elle değil: arayüz hâlâ haftada birkaç kez değişiyor; elle çekilen kare bir haftada eskir, kip
// her sürümde aynı sahneleri yeniden üretir.
// Boy: pencere 1320×2868 (iPhone 6,9", 440×956 pt @3×), güvenli alan taklidi 62/34 pt (×3). Menü ve HUD ekran boyuna göre
// ölçeklenir (UiKok ScaleWithScreenSize, Hud u), yani kare telefondaki yerleşimin 3× hâlidir. 6,5" (1284×2778) aynı kareden
// tool/magaza_gorsel.py ile türer (en-boy farkı %0,4: genişliğe ölçekle, alttan/üstten 6 px kırp); iki ayrı render
// sahneleri (bot ralli, smaç anı) farklı kılardı, iki dilde aynı an olmazdı.
// Başlık yazısı Unity'de değil: kareye tool/magaza_gorsel.py ekler (TR/EN), metin değişince oyun yeniden derlenmez.
// Sahneler (yasin'in onayladığı beş): smaç anı, uzun ralli + seyirci (Turnuva salonu), kariyer haritası, karakter odası,
// imza vuruşu (patron hazırlığı). Her biri iki dilde: <dil>_<NN>_<ad>.png. Oyuncu 3 galibiyeti geçmiş sayılır (öğretici
// yazılar sönük, Hud.Ogretici): mağaza karesi talimat değil oyun göstersin. Ayarlar sonunda geri konur.
// Yazılım GL (llvmpipe) uyarısı: gölge/kenar yumuşatma telefondakinden zayıf olabilir; ilk taslaklar telefondan alınan
// bir kareyle yan yana gösterilir (Ar-Ge şüphe a).
using System.Collections;
using System.IO;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public partial class EkranTuru
    {
        const int MG_W = 1320, MG_H = 2868, MG_UST = 62 * 3, MG_ALT = 34 * 3;

        IEnumerator MagazaTuru()
        {
            int w0 = Screen.width, h0 = Screen.height, wins0 = Ayar.wins; bool kos0 = Ayar.kosIpucu, fps0 = Ayar.fps; string d0 = Ayar.dil;
            Ayar.wins = Mathf.Max(Ayar.wins, 5); Ayar.kosIpucu = true; Ayar.fps = false; Ayar.easy = true; Ayar.reduce = false; Ayar.target = 11;
            Ayar.model = "oyuncak"; Ayar.kamera = "yayin"; root.ModelDegisti(); root.KameraDegisti();
            Guvenli.test = true; Guvenli.ust = MG_UST; Guvenli.alt = MG_ALT; Guvenli.sol = 0; Guvenli.sag = 0;
            yield return Boyut(MG_W, MG_H);
            if (Screen.width != MG_W || Screen.height != MG_H) rap.AppendLine("magaza: pencere " + Screen.width + "x" + Screen.height + " (istenen " + MG_W + "x" + MG_H + "); kareler yine çekilir, betik boyu denetler");
            foreach (var dil in new[] { "tr", "en" })
            {
                Ayar.dil = dil; if (root.ui != null) root.ui.Kirlet();
                yield return MgSmac(dil + "_01_smac");
                yield return MgRalli(dil + "_02_ralli");
                yield return MgKariyer(dil + "_03_kariyer");
                yield return MgKarakter(dil + "_04_karakter");
                Ayar.saha = "turnuva"; root.SahaDegisti();
                yield return MgImza(dil + "_05_imza", KariyerVeri.Global(1, 5));
                root.AnaMenu(); yield return Bekle(10);
            }
            Guvenli.test = false; Ayar.wins = wins0; Ayar.kosIpucu = kos0; Ayar.fps = fps0; Ayar.dil = d0; Ayar.saha = "salon"; root.SahaDegisti();
            yield return Boyut(w0, h0);
        }

        // r47 dersi (2026-10-29): CI'da kare hızı düşük (xvfb + yazılım GL, birkaç fps), maç gerçek zamanla ilerlediği için 90 sn'de
        // ralli 4'te kaldı; elle kurulan smaç topu maç başı rakip kartı sürerken uçmadı; imza hazırlığı 60 sn'de gelmedi.
        // Çözüm: sahneler maçı kare başına 30 adım (1/60 sn) ileri sarar, bot oynar; aranan an gelince sarma durur ve kare
        // normal akışta çekilir. Ralli sayısı, skor ve imza hazırlığı gerçek oyundan gelir (uydurma yok), yalnız beklenmez.
        IEnumerator IleriSar(System.Func<Match, bool> dur, float enCok, bool smacla = false)
        {
            var M = root.match; root.rakipKartT = 0; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < enCok)
            {
                for (int i = 0; i < 30; i++)
                {
                    if (M.over || dur(M)) yield break;
                    if (BotAdim(M, smacla)) yield break;   // smaç atıldı: o anı normal akışta göstermek için dur
                    M.Update(1f / 60f); M.events.Clear();
                }
                yield return null;
            }
        }
        // Mağaza botu: servis kısa/uzun karışık; topa yetişince hep vurur (rastgele bot rallileri 4–5'te bitiriyordu), çoğunlukla
        // güvenli clear/drop (sim: maks ralli 228–409, 12'ye ~680 adım; smaç ~1000 adım, imza ~400–950). smacla: rakipten gelen baş üstü topta enerji doluyken sıçrayarak smaç (true döner).
        bool BotAdim(Match M, bool smacla)
        {
            if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null) { M.SwipeServe((float)(rng.NextDouble() * 40 - 20), -70, 1.6f); return false; }
            Reach R; if (M.s == null || M.s.from != "op" || !M.ReachInfo(out R)) return false;
            if (smacla && R.over && Mathf.Abs(M.me.y - Court.NET) > 3) { M.energy = 1; return M.SwipeShot(new SwipeInput { dx = 20, dy = -230, speed = 2.4f, R = 40, dist = 4, side = 1 }); }
            if (smacla) M.energy = Mathf.Min(M.energy, .9f);   // smaç anına kadar enerji harcanmasın diye sıçrama yok
            M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 120 - 60), dy = -(float)(140 + rng.NextDouble() * 80), speed = 1.2f, R = 40, dist = 4, side = rng.NextDouble() < .5 ? -1 : 1 });
            return false;
        }

        // Smaç anı: Turnuva salonu, skor 9–7 (maçın ortası); bot ralli oynar, rakipten ilk baş üstü topta enerji dolu sıçrayarak
        // smaç. Temastan 1 ve 3 kare sonra iki aday (_a/_b); hangisinin daha iyi okunduğu betikte değil gözle seçilir.
        IEnumerator MgSmac(string ad)
        {
            Ayar.saha = "turnuva"; root.SahaDegisti(); root.StartMatch(false, false, false); botAcik = false;
            var M = root.match; M.sc[0] = 9; M.sc[1] = 7; M.sonTip = null;
            yield return IleriSar(m => false, 60, true);
            bool vurdu = M.sonTip != null; string tip = M.sonTip ?? "-";
            yield return Bekle(1); yield return Cek(ad + "_a"); yield return Bekle(2); yield return Cek(ad + "_b");
            rap.AppendLine("magaza " + ad + " · " + (vurdu ? "vuruş " + tip + " (" + M.sonQ + ")" : "VURUŞ OLMADI") + " · skor " + M.sc[0] + "–" + M.sc[1] + " · " + Screen.width + "x" + Screen.height);
        }

        // Uzun ralli + seyirci: Turnuva salonunda bot oynar; ralli 12'ye varınca, top filenin üstünde uçarken çekilir. En çok
        // 90 sn gerçek zaman (ileri sarılarak dakikalarca oyun); varmazsa o anki kare çekilir ve rapor bunu söyler.
        IEnumerator MgRalli(string ad)
        {
            Ayar.saha = "turnuva"; root.SahaDegisti(); root.StartMatch(false, false, false); botAcik = false;
            var M = root.match;
            yield return IleriSar(m => m.rally >= 12 && m.s != null && !m.s.dead && m.s.z > 1.6f && Mathf.Abs(m.s.y - Court.NET) < 2.5f, 90);
            int r = M.rally; yield return Cek(ad);
            rap.AppendLine("magaza " + ad + " · ralli " + r + " (en uzun " + M.maxRally + ")" + (r >= 12 ? "" : " · HEDEF 12'YE VARMADI"));
        }

        // İmza vuruşu: patron basamağında rakibin imza hazırlığı (ışık + poz) başlayınca çekilir (EkranTuru.ImzaKare'nin ileri
        // saran hâli; o tur gerçek zamanda bekler, burada 60 sn yetmedi).
        IEnumerator MgImza(string ad, int g)
        {
            var eski = Ayar.ilerleme; var q = new Ilerleme(); for (int b = 0; b < 6; b++) q.tur[0][b] = 1; for (int b = 0; b < 5; b++) q.tur[1][b] = 1; Ayar.ilerleme = q;
            bool km = Ayar.kariyerModu; Ayar.kariyerModu = true; Ayar.kariyerSecim = g;
            root.StartMatch(false, false, false); botAcik = false;
            root.match.sc[0] = 4; root.match.sc[1] = 3;   // imza puan aralığı (stil.imzaAra 4) dolsun: uzun rallilerde sayı az düşer
            yield return IleriSar(m => m.op.imzaT > 0 && m.op.imzaId != null, 60);
            bool var_ = root.match.op.imzaT > 0; yield return Bekle(4); yield return Cek(ad + (var_ ? "" : "_yok"));
            rap.AppendLine("magaza " + ad + " · rakip imza hazırlığı " + (var_ ? "VAR (" + root.match.op.imzaId + ")" : "yok (ad _yok ekli)"));
            Ayar.ilerleme = eski; Ayar.kariyerModu = km; Ayar.kariyerSecim = -1;
        }

        // Kariyer haritası: örnek ilerleme (Tur 1 bitmiş, Tur 2 yarısı; EkranTuru.Harita ile aynı); Tur 2 görünür.
        IEnumerator MgKariyer(string ad)
        {
            root.AnaMenu(); yield return Bekle(10); var ui = root.ui; if (ui == null) yield break;
            var eski = Ayar.ilerleme;
            var q = new Ilerleme(); for (int b = 0; b < 6; b++) q.tur[0][b] = b % 2 == 0 ? 7 : 3; q.tur[1][0] = 7; q.tur[1][1] = 3; q.tur[1][2] = 1; Ayar.ilerleme = q;
            ui.Kirlet(); ui.SekmeAc(Sekme.Kariyer); ui.KariyerTurSec(1); yield return Bekle(30); yield return Cek(ad);
            int n; rap.AppendLine("magaza " + ad + " · " + ui.TasmaDenetimi(ad, out n));
            Ayar.ilerleme = eski; ui.Kirlet(); ui.KariyerTurSec(-1); ui.SekmeAc(Sekme.Oyna); yield return Bekle(5);
        }

        // Karakter odası: Soyunma › Karakter (vitrin figürü + seçenekler), oyuncunun kendi kaydıyla; afro saç denemesi vitrinde
        // (alınmamış öğe kayda yazılmaz).
        IEnumerator MgKarakter(string ad)
        {
            var ui = root.ui; if (ui == null) yield break;
            ui.SekmeAc(Sekme.Soyunma); ui.SoyunmaSegment(UiKok.SEG_KARAKTER); yield return Bekle(30);
            ui.KarakterDene(Kozmetik.SAC, "afro"); yield return Bekle(40); yield return Cek(ad);
            int n; rap.AppendLine("magaza " + ad + " · " + ui.TasmaDenetimi(ad, out n));
            ui.SoyunmaSegment(0); ui.SekmeAc(Sekme.Oyna); yield return Bekle(5);
        }
    }
}
