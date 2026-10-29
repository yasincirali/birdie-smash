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
                yield return ImzaKare(dil + "_05_imza", KariyerVeri.Global(1, 5), false);
                rap.AppendLine("magaza " + dil + "_05_imza · rakip imza hazırlığı " + (root.match != null && root.match.op.imzaT > 0 ? "VAR" : "yok (ad _yok ekli)"));
                root.AnaMenu(); yield return Bekle(10);
            }
            Guvenli.test = false; Ayar.wins = wins0; Ayar.kosIpucu = kos0; Ayar.fps = fps0; Ayar.dil = d0; Ayar.saha = "salon"; root.SahaDegisti();
            yield return Boyut(w0, h0);
        }

        // Smaç anı: rakip yüksek clear atar (KadrajTepe'deki gibi plandan), oyuncu Kolay'da topun altına koşar, enerji dolu
        // (sıçrayarak smaç), top baş üstü pencereye girince hızlı aşağı kaydırma. Temastan 1 ve 3 kare sonra iki aday kare (_a/_b);
        // CI'da kare hızı düşük, hangisinin daha iyi okunduğu betikte değil gözle seçilir. Skor 9–7 (maçın ortası).
        IEnumerator MgSmac(string ad)
        {
            Ayar.saha = "turnuva"; root.SahaDegisti(); root.StartMatch(false, false, false); botAcik = false;
            { float t1 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t1 < 4) yield return null; }   // maç başı rakip kartı kalksın
            var M = root.match; M.sc[0] = 9; M.sc[1] = 7; float L = Court.L;
            M.s = new Shuttle { x = Court.MID, y = L - 1f, z = 2.55f, vy = 3, vz = -2, from = "op" }; M.me.x = Court.MID - .4f; M.me.y = L - 1f;
            var P = M.ShotPlan(new SwipeInput { dy = -150, speed = 1.2f, R = 40 }, new Reach(), true); M.s = null;
            float hx = 2 * Court.MID - P.hx, hy = L - P.hy, ty = Court.NET + P.d, tx = 2 * Court.MID - P.tx, vx, vy, vz;
            M.op.x = hx + .4f; M.op.y = hy; M.between = false; M.wait = 0;
            Shuttle.Aim(hx, hy, P.hz, tx, ty, P.byAngle, P.val, out vx, out vy, out vz);
            var sf = typeof(Match).GetMethod("StartFlight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sf.Invoke(M, new object[] { "op", vx, vy, vz, hx, hy, P.hz, false, "clear", false, "good", false });
            float t0 = Time.realtimeSinceStartup; bool vurdu = false; string tip = "-";
            while (Time.realtimeSinceStartup - t0 < 12 && M.s != null && !vurdu)
            {
                M.energy = 1; Reach R;
                if (M.s.from == "op" && M.ReachInfo(out R) && R.over)
                    vurdu = M.SwipeShot(new SwipeInput { dx = 20, dy = -230, speed = 2.4f, R = 40, dist = 4, side = 1 });
                if (vurdu) tip = M.sonTip; else yield return null;
            }
            yield return Bekle(1); yield return Cek(ad + "_a"); yield return Bekle(2); yield return Cek(ad + "_b");
            rap.AppendLine("magaza " + ad + " · " + (vurdu ? "vuruş " + tip + " (" + M.sonQ + ")" : "VURUŞ OLMADI (top pencereye girmedi)") + " · " + Screen.width + "x" + Screen.height);
        }

        // Uzun ralli + seyirci: Turnuva salonunda bot oynar; ralli 12'ye varınca, top filenin üstünde uçarken çekilir. En çok
        // 90 sn; varmazsa en uzun rallinin sonundaki an çekilir ve rapor bunu söyler (sayı uydurulmaz: HUD'daki ralli gerçek).
        IEnumerator MgRalli(string ad)
        {
            Ayar.saha = "turnuva"; root.SahaDegisti(); root.StartMatch(false, false, false); botAcik = true;
            var M = root.match; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 90 && !(M.rally >= 12 && M.s != null && !M.s.dead && M.s.z > 1.6f && Mathf.Abs(M.s.y - Court.NET) < 2.5f)) yield return null;
            int r = M.rally; yield return Cek(ad); botAcik = false;
            rap.AppendLine("magaza " + ad + " · ralli " + r + " (en uzun " + M.maxRally + ")" + (r >= 12 ? "" : " · HEDEF 12'YE VARMADI"));
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
