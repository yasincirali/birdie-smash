// Ekran görüntüsü turu (2026-10-25, yasin build 38: "her turda düzelt ss al"): bulutta Unity Editor'ü yok, cihaz da yok;
// gerçek Unity render'ını görmenin tek yolu CI'da Linux oyuncusunu xvfb altında koşmak (.github/workflows/unity-ekran.yml).
// Yalnız `-bsekran <klasör>` argümanıyla açılır; mağaza build'inde hiç çalışmaz (argüman verilmez).
// Tur: iki model × saha temaları; menü, maç başı (servis), ralliden kareler (bot oynar: BotMac ile aynı karar), yakın
// planlar (ben, rakip, hakemler) ve tanı dosyası (rapor.txt: her figürün renderer'ları, shader'ları, sınırları, yönü).
// 2026-10-26 menü kabuğu (UI Toolkit): her sekme (Oyna, Kariyer, Soyunma odası, Profil), Hızlı maç alt sayfası ve Ayarlar,
// ×1 ve ×2 yazı ölçeğiyle (Ayar.yaziTest; kaydedilmez), ilk açılış Oyna'sı (logo + antrenman ipucu). Sekmeler UiKok'un
// açık yöntemleriyle değişir (SekmeAc, HizliMacAc, AyarlarAc, Geri); UI Toolkit arka tampona çizdiği için ReadPixels yakalar.
// 2026-10-26 kariyer haritası (adım 2–3): örnek ilerlemeyle (kaydedilmez, tur sonunda eskisi geri konur) harita Tur 1,
// Tur 2 kilitli ve açık, final maç kartı (imza vuruşu satırı), ×1 ve ×2 yazı; maçta patronun imza hazırlığı (ışık + poz),
// hareket tam ve hareketi azalt.
// 2026-10-26 ekipman v2 + karakter (adım 4, 6): Soyunma odası Tarz ve Görünüm segmentleri, raket ve kordaj yuva sayfaları,
// örnek ilerlemeyle Profil (ad, unvan, maç/smaç); ×1 ve ×2 yazı.
// 2026-10-26 karakter tasarımı: Karakter › Tip/Saç/Ayakkabı/Ten, yakın planlar (yüz, ayak, tutuş forehand/backhand) ve
// deneme kartları (normal ve x2_ büyük yazı; deneme kayda yazılmaz, segmentten çıkınca silinir).
// 2026-10-27 yön C: perde v2 (gecis_kapanis_*, gecis_kapali, gecis_bekle, gecis_acilis_*, gecis_azalt_*) ve açılış anı
// (acilis_*, acilis_bekle, acilis_patlama_*, acilis_azalt) dondurulmuş anlarla; rapor.txt'de sayılar ve gerçek açılış süresi.
// 2026-10-27 model/varyant turu (Varyantlar): her tip/saç/ayakkabı vitrinde ve her tip maçta, "varyant" rapor satırları.
// Süre: gerçek zaman (GameRoot dt'yi 0,05 ile sınırlar; yavaş yazılım render'ında maç da yavaşlar, kareler yine anlamlı).
// 2026-10-27 koşu ve vuruş tutarlılığı (ekran/kosu): geri çekilme şeridi (<model>_geri_NN: arka sahaya giderken yan dönüş),
// rapor.txt'de kare kare "kosu_geri" ve model başına "kosu_ozet" satırları (Unity pozundan yerdeki ayak kayması), her vuruş ×
// taraf ve ayak işi klibi için 6 karelik poz şeridi (<model>_klip_<klip>.png; "klip_poz" satırı: temas karesi, el yanı).
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    [DefaultExecutionOrder(-100)]
    public class EkranTuru : MonoBehaviour
    {
        string dir; GameRoot root; readonly StringBuilder rap = new StringBuilder(); readonly System.Random rng = new System.Random(11);
        bool botAcik; static volatile int kare; static volatile string adim = "başlangıç";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var a = System.Environment.GetCommandLineArgs(); string d = null;
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-bsekran") d = a[i + 1];
            if (d == null) return;
            var g = new GameObject("ekran_turu"); var t = g.AddComponent<EkranTuru>(); t.dir = d; DontDestroyOnLoad(g);
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir); QualitySettings.vSyncCount = 0;   // xvfb ekranı 0 Hz: dikey eşitleme ilk karede bekletiyordu (tahmin)
            Debug.Log("EkranTuru başladı: " + dir);
            // Bekçi: ana döngü takılırsa nerede takıldığını günlükten görmek için 10 sn'de bir kare sayısı ve son adım.
            var bekci = new System.Threading.Thread(() => { for (int i = 0; i < 300; i++) { System.Threading.Thread.Sleep(10000); Debug.Log("EkranTuru bekçi: kare " + kare + " adım " + adim); } }); bekci.IsBackground = true; bekci.Start();
            yield return null; root = FindFirstObjectByType<GameRoot>(); Debug.Log("EkranTuru GameRoot " + (root != null));
            yield return Cek("00_ilk_kare"); Tani("acilis"); File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
            yield return MenuTuru(); if (root.HudKatman != null) rap.AppendLine("açılış süresi (gerçek, CI yazılım render'ı): " + root.HudKatman.AcilisTani);
            Ayar.antrenman = true; Ayar.easy = true; Ayar.target = 5; Ayar.reduce = false; if (root.ui != null) root.ui.Kirlet();   // r11: Oyna sekmesi ilk açılış kurulumunda kalıyordu
            yield return MenuTuru("", true); Ayar.yaziTest = 2; yield return MenuTuru("x2_", true); Ayar.yaziTest = 0; root.ui.SekmeAc(Sekme.Oyna); yield return Bekle(5);
            // 2026-10-26 yön A: Türkçe Oyna (CI dili İngilizce; Barlow'da ş, ğ, İ, ı karelerde görünsün), sonra eski dile dönülür.
            { string d0 = Ayar.dil; Ayar.dil = "tr"; root.ui.SekmeAc(Sekme.Kariyer); root.ui.SekmeAc(Sekme.Oyna); yield return Bekle(20); yield return Cek("menu_tr_oyna");
              Ayar.yaziTest = 2; yield return Bekle(20); yield return Cek("menu_x2_tr_oyna"); Ayar.yaziTest = 0; Ayar.dil = d0; root.ui.SekmeAc(Sekme.Kariyer); root.ui.SekmeAc(Sekme.Oyna); yield return Bekle(5); }
            yield return PerdeTuru();   // 2026-10-27 yön C: perde v2 ve açılış anı kareleri
            var modeller = new System.Collections.Generic.List<string> { "oyuncak", "rocketbox" };
            if (Resources.Load<GameObject>("karakter/sade") != null) modeller.Add("sade");
            foreach (var mdl in modeller)
            {
                Ayar.model = mdl; Ayar.saha = "salon"; root.ModelDegisti(); root.SahaDegisti();
                yield return Bekle(40); yield return Cek(mdl + "_01_menu");
                yield return Mac(mdl);
            }
            yield return Varyantlar();   // 2026-10-27 model/varyant turu (ekran/model)
            foreach (var s in new[] { "turnuva", "okul", "park" })
            {
                Ayar.model = "oyuncak"; Ayar.saha = s; root.SahaDegisti(); root.StartMatch(false, false, false); botAcik = true;
                yield return Bekle(60); yield return Cek("saha_" + s);
            }
            // 2026-10-26 kadraj: her açı iki ekran oranında: 390×845 (19,5:9, uzun ekran; ad eki yok, eski karelerle aynı ad) ve
            // 375×667 (iPhone SE, 16:9; "_se"). Pencere boyu turun içinde değişir, sonunda eskisine döner.
            int w0 = Screen.width, h0 = Screen.height;
            foreach (var en in new[] { "", "_se" })
            {
                if (en == "_se") yield return Boyut(375, 667);
                foreach (var k in new[] { "alcak", "ust", "yayin" })
                {
                    Ayar.kamera = k; Ayar.saha = "salon"; root.SahaDegisti(); root.KameraDegisti(); root.StartMatch(false, false, false); botAcik = true;
                    yield return Bekle(60); yield return Cek("kamera_" + k + en); Kadraj("kamera_" + k + en);
                    yield return KadrajTepe(k + en, false);   // 2026-10-27 yüksek top (Core/Kadraj)
                }
            }
            yield return KadrajTepeEk(w0, h0);
            yield return Boyut(w0, h0);
            Ayar.model = "oyuncak"; Ayar.saha = "salon"; Ayar.kamera = "yayin"; root.SahaDegisti(); root.KameraDegisti();
            yield return ImzaKare("imza_mermi", KariyerVeri.Global(1, 5), false);
            yield return ImzaKare("imza_duvar_azalt", KariyerVeri.Global(0, 5), true);
            yield return GorevKare();
            yield return SonucKare("sonuc_galibiyet", true, false, 0, "en");   rap.AppendLine("sonuc_galibiyet · " + (root.HudKatman != null ? root.HudKatman.SonucTani : "hud yok"));   // tur 2: matris düz, ikincil düğme stili var olmalı
            yield return SonucKare("sonuc_galibiyet_azalt", true, true, 0, "en");
            yield return SonucKare("sonuc_buyuk_galibiyet", true, false, 2, "en");   // HUD ×1,3 sınırında (Ayar.YaziK); "Büyük" yazı
            yield return SonucKare("sonuc_tr_galibiyet", true, false, 0, "tr");
            yield return SonucKare("sonuc_yenilgi", false, false, 0, "en");   rap.AppendLine("sonuc_yenilgi · " + (root.HudKatman != null ? root.HudKatman.SonucTani : "hud yok"));   // tur 2: matris düz, ikincil düğme stili var olmalı
            yield return FileKare();   // 2026-10-27 ekran/file: file yakın planı (ayrı satır, başka dallar da bu listeye ekliyor)
            yield return DuyarliTuru();   // 2026-10-27 duyarlı yerleşim: SE / 13 / Pro Max / iPad, çentik taklidi, taşma denetimi
            yield return MenuSahne();
            File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
            Application.Quit(0);
        }

        IEnumerator Mac(string mdl)
        {
            root.StartMatch(false, false, false); botAcik = false;
            yield return Bekle(45); yield return Cek(mdl + "_02_servis_once");
            Tani(mdl);
            yield return Yakin(mdl + "_03_ben", root.match.me, true);
            yield return Yakin(mdl + "_04_rakip", root.match.op, false);
            yield return YakinNokta(mdl + "_05_hakemler", CourtView.HAKEM1 + new Vector3(0, .5f, 0), new Vector3(3.2f, .6f, -2.6f));
            yield return YakinNokta(mdl + "_06_servis_hakemi", CourtView.HAKEM2 + new Vector3(0, .5f, 0), new Vector3(-2.8f, .7f, -2.2f));
            botAcik = true; iz = new StringBuilder(); temasSay = 0; temasN.Clear();
            temas = new StringBuilder("kim,klip,sw,once,sonra,uzanW,pay,dirsek,hedefx,hedefy,hedefz,kafax,kafay,kafaz\n");
            iz.AppendLine("kare,t,dt,kim,klip,faz,alt,vAn,x,y,solx,soly,solz,sagx,sagy,sagz,elx,ely,elz,sapEl,pencere,donuk,sw,kafax,kafay,kafaz,topx,topy,topz,kilit,uzanW,uzanSonra,dirsek,donus,donusAyak,kalca");
            for (int i = 0; i < 10; i++) { yield return Bekle(14); yield return Cek(mdl + "_1" + i + "_ralli"); if (i == 2) Kadraj(mdl + "_12_ralli"); }
            yield return Serit(mdl, true); yield return Serit(mdl, false);
            yield return GeriSerit(mdl);   // 2026-10-27 ekran/kosu
            File.WriteAllText(Path.Combine(dir, mdl + "_anim_iz.csv"), iz.ToString()); iz = null; KosuOzet(mdl);
            // Temas örneklemi (2026-10-27 gece, uzanma katmanı): ralli sürer, 70 temasa ya da 60 sn'ye dek yalnız temas kareleri.
            { float t0 = Time.realtimeSinceStartup; while (temasSay < 70 && Time.realtimeSinceStartup - t0 < 60) yield return null; }
            File.WriteAllText(Path.Combine(dir, mdl + "_temas.csv"), temas.ToString()); temas = null;
            botAcik = false;
            yield return KlipSeritleri(mdl);   // 2026-10-27 ekran/kosu
        }
        // Animasyon izi (2026-10-26 gece, build 40 incelemesi): ralli kareleri ve şerit boyunca HER karede iki oyuncunun klibi,
        // fazı, alt katman ağırlığı, ayak bileği/el kemiği dünya konumu, raket sapının ele uzaklığı, raket kafası ve top
        // (temas karesinde kafa–top aralığı: salınım topa değiyor mu) (<model>_anim_iz.csv).
        // Ayak kayması ve raket kayması gerçek Unity pozundan buradan ölçülür (Core taklidi değil).
        StringBuilder iz, temas; int temasSay; readonly Dictionary<bool, int> temasN = new Dictionary<bool, int>();
        // Temas karesi: Athlete.tmN değiştiği ilk karede raket kafasının temas noktasına uzaklığı, katmandan önce (once = klibin
        // kendi pozu) ve sonra; aynı kare, aynı top. Servis dışarıda (uzanma servise uygulanmaz).
        void TemasIz(PlayerView pv, Athlete a, Match M)
        {
            int n; if (!temasN.TryGetValue(pv.isMe, out n)) n = a.tmN; temasN[pv.isMe] = a.tmN;
            if (temas == null || a.tmN == n || a.sw <= 0 || (M.s != null && M.s.serve)) return;
            var h = Kit.W(a.tmX, a.tmY, a.tmZ); var k = pv.TaniKafa; temasSay++;
            temas.AppendLine((pv.isMe ? "me" : "op") + "," + pv.TaniKlip + "," + F(a.sw) + "," + F(pv.TaniUzanOnce) + "," + F(pv.TaniUzanSonra) + "," + F(pv.TaniUzanW) + "," + F(pv.TaniUzanPay) + "," + F(pv.TaniDirsek)
                + "," + F(h.x) + "," + F(h.y) + "," + F(h.z) + "," + F(k.x) + "," + F(k.y) + "," + F(k.z));
        }
        static string F(float v) { return v.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture); }
        void LateUpdate()
        {
            if ((iz == null && temas == null) || root == null || root.match == null || root.ekran != Ekran.Oyun) return;
            var M = root.match;
            foreach (var pv in FindObjectsByType<PlayerView>(FindObjectsSortMode.None))
            {
                if (pv.hakem) continue; var a = pv.isMe ? M.me : M.op; Vector3 l, r, e; float se;
                TemasIz(pv, a, M); if (iz == null) continue;
                if (!pv.TaniAyak(out l, out r, out e, out se)) continue;
                float vAn = 0; var f = typeof(Athlete).GetField("vAn"); if (f != null) vAn = (float)f.GetValue(a);
                iz.AppendLine(Time.frameCount + "," + F(Time.realtimeSinceStartup) + "," + F(Mathf.Min(.05f, Time.unscaledDeltaTime)) + "," + (pv.isMe ? "me" : "op") + "," + pv.TaniKlip + "," + F(pv.TaniFaz) + "," + F(pv.TaniAlt) + "," + F(vAn) + "," + F(a.x) + "," + F(a.y) + ","
                    + F(l.x) + "," + F(l.y) + "," + F(l.z) + "," + F(r.x) + "," + F(r.y) + "," + F(r.z) + "," + F(e.x) + "," + F(e.y) + "," + F(e.z) + "," + F(se) + "," + (M.HitWindow() ? 1 : 0) + "," + (M.freeze > 0 ? 1 : 0)
                    + "," + F(a.sw) + "," + F(pv.TaniKafa.x) + "," + F(pv.TaniKafa.y) + "," + F(pv.TaniKafa.z) + "," + (M.s != null ? F(Kit.W(M.s.x, M.s.y, M.s.z).x) + "," + F(Kit.W(M.s.x, M.s.y, M.s.z).y) + "," + F(Kit.W(M.s.x, M.s.y, M.s.z).z) : ",,") + "," + F(pv.TaniKilit) + "," + F(pv.TaniUzanW) + "," + F(pv.TaniUzanSonra) + "," + F(pv.TaniDirsek)
                    + "," + F(a.donus) + "," + F(a.donusAyak) + "," + F(pv.TaniKalca));
                KosuIz(pv, a, l, r);
            }
        }

        // ---- Koşu (2026-10-27, ekran/kosu) ----
        // Geri çekilme: kendi çerçevede gidiş fileden > 120°, anlık hız > 2 m/s. Unity pozundan yerdeki ayak kayması: alçak
        // ayağın (bilek y < en alçak + 3 cm) kareler arası yatay hızı; bot ölçümü (BotMac KosuYonleri) Core'dan, bu gerçek iskeletten.
        class KosuSay { public int geri, yan, sirt, ileri; public double geriKay, ileriKay; public int geriKayN, ileriKayN; public Vector3 l0, r0; public bool var; }
        readonly Dictionary<bool, KosuSay> kosu = new Dictionary<bool, KosuSay>();
        static bool Geri(Athlete a, bool isMe, out float th)
        {
            float yan = isMe ? a.svx : -a.svx, ileri = isMe ? -a.svy : a.svy; th = Mathf.Atan2(yan, ileri) * Mathf.Rad2Deg;
            return a.vAn > 2 && Mathf.Abs(th) > 120;
        }
        void KosuIz(PlayerView pv, Athlete a, Vector3 l, Vector3 r)
        {
            KosuSay k; if (!kosu.TryGetValue(pv.isMe, out k)) kosu[pv.isMe] = k = new KosuSay();
            float dt = Mathf.Max(1e-3f, Mathf.Min(.05f, Time.unscaledDeltaTime)), th;
            if (k.var && root.match.freeze <= 0)
            {
                bool solAlt = l.y < r.y; Vector3 p = solAlt ? l : r, p0 = solAlt ? k.l0 : k.r0;
                float kay = new Vector2(p.x - p0.x, p.z - p0.z).magnitude / dt; bool yerde = Mathf.Abs(l.y - r.y) > .03f;
                if (Geri(a, pv.isMe, out th))
                {
                    k.geri++; if (Mathf.Abs(a.donusAyak) > 45) k.yan++; if (pv.TaniKlip != null && pv.TaniKlip.StartsWith("back")) k.sirt++;
                    if (yerde && kay < 12) { k.geriKay += kay; k.geriKayN++; }
                }
                else if (a.vAn > 2 && Mathf.Abs(th) < 60 && yerde && kay < 12) { k.ileri++; k.ileriKay += kay; k.ileriKayN++; }
            }
            k.l0 = l; k.r0 = r; k.var = true;
        }
        void KosuOzet(string mdl)
        {
            foreach (var kv in kosu)
            {
                var k = kv.Value;
                rap.AppendLine("kosu_ozet " + mdl + " " + (kv.Key ? "ben" : "rakip") + " · geri çekilme " + k.geri + " kare, yan dönük %" + (100f * k.yan / Mathf.Max(1, k.geri)).ToString("F0")
                    + ", fileye bakıp geri geri (back_*) %" + (100f * k.sirt / Mathf.Max(1, k.geri)).ToString("F0") + ", yerdeki ayak kayması ort " + (k.geriKay / Mathf.Max(1, k.geriKayN)).ToString("F2")
                    + " m/s · öne koşu " + k.ileri + " kare, ayak kayması ort " + (k.ileriKay / Mathf.Max(1, k.ileriKayN)).ToString("F2") + " m/s");
            }
            kosu.Clear(); File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
        }
        // Geri çekilme şeridi: ben arka sahaya giderken (en çok 40 sn beklenir) 12 kare, arka-sağ yukarıdan (dönüş ve gidiş
        // birlikte görünür); her karede klip, faz, gövde/bacak dönüşü, kalça farkı, gidiş açısı ve hız rapora.
        IEnumerator GeriSerit(string mdl)
        {
            float t0 = Time.realtimeSinceStartup, th = 0; var M = root.match;
            while (Time.realtimeSinceStartup - t0 < 40 && !(M.s != null && !M.between && Geri(M.me, true, out th))) yield return null;
            if (!Geri(M.me, true, out th)) { rap.AppendLine("kosu_geri " + mdl + " · 40 sn'de geri çekilme olmadı"); yield break; }
            PlayerView ben = null; foreach (var pv in FindObjectsByType<PlayerView>(FindObjectsSortMode.None)) if (pv.isMe && !pv.hakem) ben = pv;
            for (int i = 0; i < 12; i++)
            {
                var a = M.me; Geri(a, true, out th); var p = Kit.W(a.x, a.y, 0) + Vector3.up * 1.0f;
                rap.AppendLine("kosu_geri " + mdl + " " + i.ToString("00") + " · klip " + (ben != null ? ben.TaniKlip : "-") + " faz " + (ben != null ? ben.TaniFaz.ToString("F2") : "-") + " gövde " + a.donus.ToString("F0") + "° bacak " + a.donusAyak.ToString("F0")
                    + "° kalça " + (ben != null ? ben.TaniKalca.ToString("F0") : "-") + "° gidiş " + th.ToString("F0") + "° hız " + a.vAn.ToString("F1") + " ayakKlip " + a.ayakKlip + " hazırlık " + a.prepA.ToString("F2"));
                yield return YakinNokta(mdl + "_geri_" + i.ToString("00"), p, new Vector3(3.0f, 2.2f, -3.4f));
                yield return null;
            }
            File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
        }
        // Klip şeritleri: her vuruş × taraf ve ayak işi klibi, maç dışı bir figürle (ShowPoz: yalnız klip, katman/IK yok) 6
        // kare yan yana (1920×480). Vuruş kliplerinde kareler hazırlıktan takibe eşit aralıklı, temas karesi dahil; döngülerde
        // faz 0..5/6. "klip_poz" satırı: temas karesi ve sağ elin gövdeye göre yanı (+ sağ: forehand, − sol: backhand çaprazı).
        static readonly string[] KLIP_SERIT = { "smash", "clear", "drop", "jump", "bh_over", "drive", "bh_drive", "kill", "bh_kill", "low", "bh_low", "net", "lunge", "bh_lunge", "block", "serve", "serve_bh", "serve_flick",
            "fwd", "run", "chasse_r", "chasse_l", "kros_r", "kros_l", "back_r", "back_l", "split" };
        IEnumerator KlipSeritleri(string mdl)
        {
            var g = new GameObject("klip_serit"); var pv = PlayerView.Build(g.transform, mdl, true, Kit.Hex("#3aa0ff"));
            Vector3 pos = Kit.W(-1.6f, Court.NET + 3.5f, 0), hedef = pos + Vector3.up * 1.0f;   // yan çizginin 1,6 m dışı, kendi yarım: kortta oyuncu yok, ışık aynı
            yield return null;
            // Süre (koşu turu 2400 sn; r42 832 sn sürdü): ayak işi şeritleri yalnız varsayılan modelde, 2000 sn'den sonra şerit yok.
            foreach (var clip in KLIP_SERIT)
            {
                if (pv.Model == null || Time.realtimeSinceStartup > 2000) { rap.AppendLine("klip_poz " + mdl + " · süre sınırı, kalan şeritler atlandı"); break; }
                if (mdl != "oyuncak" && Match.ADIM_BOY.ContainsKey(clip)) continue;
                int n; if (!Match.CLIP_N.TryGetValue(clip, out n)) n = 8; float hit; bool vurus = Match.CLIP_HIT.TryGetValue(clip, out hit); int hitF = vurus ? Mathf.FloorToInt(hit * n) : -1;
                var ph = new float[6];
                for (int i = 0; i < 6; i++) ph[i] = vurus || !Match.ADIM_BOY.ContainsKey(clip) ? (Mathf.RoundToInt(i * (n - 1) / 5f) + .5f) / n : i / 6f;
                if (hitF >= 0) { int en = 0; for (int i = 1; i < 6; i++) if (Mathf.Abs(ph[i] * n - .5f - hitF) < Mathf.Abs(ph[en] * n - .5f - hitF)) en = i; ph[en] = (hitF + .5f) / n; }
                yield return new WaitForEndOfFrame();
                var cg = new GameObject("klip_kamera"); var c = cg.AddComponent<Camera>(); c.CopyFrom(root.cam); c.ResetProjectionMatrix(); c.fieldOfView = 42;
                cg.transform.position = pos + new Vector3(1.5f, .8f, 3.2f); cg.transform.LookAt(hedef);
                var rt = new RenderTexture(1920, 480, 24); c.targetTexture = rt; var sb = new StringBuilder("klip_poz " + mdl + " " + clip + " · n " + n + " temas karesi " + hitF + " · el yanı");
                for (int i = 0; i < 6; i++)
                {
                    pv.ShowPoz(pos, 0, clip, ph[i]); c.rect = new Rect(i / 6f, 0, 1 / 6f, 1); c.Render();
                    Vector3 l, r, e; float se; if (pv.TaniAyak(out l, out r, out e, out se)) sb.Append(" " + (e.x - pos.x).ToString("F2") + (Mathf.Abs(ph[i] * n - .5f - hitF) < .01f ? "*" : ""));
                }
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(1920, 480, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1920, 480), 0, 0); tex.Apply();
                RenderTexture.active = prev; c.targetTexture = null;
                File.WriteAllBytes(Path.Combine(dir, mdl + "_klip_" + clip + ".png"), tex.EncodeToPNG());
                Destroy(tex); Destroy(cg); rt.Release(); Destroy(rt); rap.AppendLine(sb.ToString());
            }
            Destroy(g); File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
        }
        // Şerit: bir vuruşun hazırlığından takibine ardışık kareler, figürü yandan izleyen ikinci kamerayla (oyun durmaz).
        // Hazırlık başlayınca (prepA > .3) 18 kare, her karede bir çekim; sonra bir araya getirmek turun dışında (Python).
        IEnumerator Serit(string mdl, bool ben)
        {
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 40) { var a0 = ben ? root.match.me : root.match.op; if (a0.prepA > .3f && a0.sw <= 0 && root.match.s != null) break; yield return null; }
            for (int i = 0; i < 18; i++)
            {
                var a = ben ? root.match.me : root.match.op; var p = Kit.W(a.x, a.y, 0) + Vector3.up * 1.0f; float s = ben ? 1 : -1;
                yield return YakinNokta(mdl + "_serit_" + (ben ? "ben" : "rakip") + "_" + i.ToString("00"), p, new Vector3(3.4f, .5f, 1.2f * s));
                yield return null;
            }
        }

        // Menü kareleri: açılış perdesi kalkana kadar bekler (sesler hazırlanınca, en çok 12 sn).
        IEnumerator MenuTuru(string ek = "ilk_", bool hepsi = false)
        {
            var ui = root != null ? root.ui : null; if (ui == null) { Debug.Log("EkranTuru: menü kabuğu yok"); yield break; }
            float t0 = Time.realtimeSinceStartup; while (root.HudKatman != null && root.HudKatman.PerdeVar && Time.realtimeSinceStartup - t0 < 12) yield return null;
            ui.SekmeAc(Sekme.Oyna); yield return Bekle(20); yield return Cek("menu_" + ek + "oyna"); rap.AppendLine("menu_" + ek + "oyna · " + ui.SahneDurumu);
            if (!hepsi) yield break;
            ui.SekmeAc(Sekme.Kariyer); yield return Bekle(20); yield return Cek("menu_" + ek + "kariyer");
            yield return Harita(ui, ek);
            ui.SekmeAc(Sekme.Soyunma); yield return Bekle(30); yield return Cek("menu_" + ek + "soyunma");
            yield return Soyunma(ui, ek);
            ui.SekmeAc(Sekme.Profil); yield return Bekle(20); yield return Cek("menu_" + ek + "profil");
            yield return Profil(ui, ek);
            ui.SekmeAc(Sekme.Oyna); ui.HizliMacAc(); yield return Bekle(20); yield return Cek("menu_" + ek + "hizli"); ui.Geri();
            ui.AyarlarAc(); yield return Bekle(20); yield return Cek("menu_" + ek + "ayarlar"); ui.Geri(); yield return Bekle(5);
            // 2026-10-26 adım 7–8: antrenman sayfası (örnek yıldızlarla), görev kartı, Günün maçı kartı
            var eskiP = Ayar.ilerleme; var dp = new Ilerleme(); dp.DrilKaydet("derin", 3); dp.DrilKaydet("drop", 2); dp.DrilKaydet("kose", 1); dp.DrilKaydet("servis", 2); Ayar.ilerleme = dp;
            ui.AntrenmanAc(); yield return Bekle(20); yield return Cek("menu_" + ek + "antrenman");
            ui.SayfaSonaKaydir(); yield return Bekle(10); yield return Cek("menu_" + ek + "antrenman_alt");   // r13: aldatıcı vuruş listesi
            ui.DrilKartAc("kose"); yield return Bekle(20); yield return Cek("menu_" + ek + "gorev_karti"); ui.Geri(); ui.Geri(); yield return Bekle(5);
            ui.GunlukAc(); yield return Bekle(20); yield return Cek("menu_" + ek + "gunun_maci"); ui.Geri();
            dp.GunlukKazan(Gunluk.Tarih(System.DateTime.Now)); ui.Kirlet(); ui.SekmeAc(Sekme.Kariyer); ui.SekmeAc(Sekme.Oyna); yield return Bekle(20); yield return Cek("menu_" + ek + "oyna_gunluk_alindi");
            Ayar.ilerleme = eskiP; ui.Kirlet(); ui.SekmeAc(Sekme.Kariyer); ui.SekmeAc(Sekme.Oyna); yield return Bekle(5);
        }

        // Harita kareleri: örnek ilerleme (Tur 1'de dört basamak, farklı yıldızlarla), sonra Tur 1 tamam + Tur 2 yarısı.
        IEnumerator Harita(UiKok ui, string ek)
        {
            var eski = Ayar.ilerleme;
            var p = new Ilerleme(); p.tur[0][0] = 7; p.tur[0][1] = 3; p.tur[0][2] = 5; p.tur[0][3] = 1; Ayar.ilerleme = p;
            ui.KariyerTurSec(0); yield return Bekle(20); yield return Cek("menu_" + ek + "harita_tur1");
            ui.KariyerTurSec(1); yield return Bekle(15); yield return Cek("menu_" + ek + "harita_tur2_kilitli");
            ui.KariyerTurSec(0); ui.MacKartAc(KariyerVeri.Global(0, 4)); yield return Bekle(20); yield return Cek("menu_" + ek + "mac_karti");
            ui.Geri(); ui.MacKartAc(KariyerVeri.Global(0, 5)); yield return Bekle(20); yield return Cek("menu_" + ek + "mac_karti_final_kilitli"); ui.Geri();
            var q = new Ilerleme(); for (int b = 0; b < 6; b++) q.tur[0][b] = b % 2 == 0 ? 7 : 3; q.tur[1][0] = 3; q.tur[1][1] = 1; Ayar.ilerleme = q;
            ui.KariyerTurSec(1); yield return Bekle(20); yield return Cek("menu_" + ek + "harita_tur2");
            ui.MacKartAc(KariyerVeri.Global(1, 2)); yield return Bekle(20); yield return Cek("menu_" + ek + "mac_karti_tur2"); ui.Geri();
            // 2026-10-26 adım 9: Tur 3 (Avrupa) ve Tur 4 (Dünya Finalleri) örnek ilerlemeyle, Usta yolu kilitli ve açık
            var r = new Ilerleme(); for (int t = 0; t < 2; t++) for (int b = 0; b < 6; b++) r.tur[t][b] = b % 3 == 0 ? 7 : 3; r.tur[2][0] = 7; r.tur[2][1] = 3; r.tur[2][2] = 1; Ayar.ilerleme = r;
            ui.KariyerTurSec(2); yield return Bekle(20); yield return Cek("menu_" + ek + "harita_tur3");
            ui.MacKartAc(KariyerVeri.Global(2, 3)); yield return Bekle(20); yield return Cek("menu_" + ek + "mac_karti_tur3"); ui.Geri();
            ui.KariyerTurSec(3); yield return Bekle(15); yield return Cek("menu_" + ek + "harita_tur4_kilitli");
            for (int b = 0; b < 6; b++) r.tur[2][b] = 3; r.tur[3][0] = 7; r.tur[3][1] = 3;
            ui.KariyerTurSec(3); yield return Bekle(20); yield return Cek("menu_" + ek + "harita_tur4");
            ui.MacKartAc(KariyerVeri.Global(3, 2)); yield return Bekle(20); yield return Cek("menu_" + ek + "mac_karti_tur4_imza"); ui.Geri();
            for (int b = 0; b < 6; b++) r.tur[3][b] = 3; r.UstaIsle(0, true, true); r.UstaIsle(1, true, false); r.UstaIsle(2, false, true);
            ui.KariyerTurSec(3); yield return Bekle(20); yield return Cek("menu_" + ek + "harita_tur4_usta");
            ui.Kirlet(); ui.SekmeAc(Sekme.Oyna); yield return Bekle(20); yield return Cek("menu_" + ek + "oyna_usta"); ui.SekmeAc(Sekme.Kariyer);
            Ayar.ilerleme = eski; ui.Kirlet(); ui.KariyerTurSec(-1); yield return Bekle(5);
        }

        // Soyunma odası (2026-10-26, ekipman v2 + karakter): Tarz ve Görünüm segmentleri, raket ve kordaj yuva sayfaları (model
        // kartları, 5 çubuk, ▲/▼). Oyuncunun kendi kaydıyla: seçim değiştirilmez, yalnız bakılır; sonunda Ekipman'a dönülür.
        IEnumerator Soyunma(UiKok ui, string ek)
        {
            ui.SoyunmaSegment(1); yield return Bekle(20); yield return Cek("menu_" + ek + "soyunma_tarz");
            ui.SoyunmaSegment(2); yield return Bekle(20); yield return Cek("menu_" + ek + "soyunma_gorunum");
            // 2026-10-26 karakter tasarımı: Karakter alt sayfaları (Tip, Saç, Ayakkabı, Ten), yakın planlar (Yüz, Ayak, Tutuş:
            // kamera yumuşak geçişini bitirsin diye 40 kare) ve deneme kartı (alınmamış öğe vitrinde, kayda yazılmaz).
            ui.KarakterAc(UiKok.KAR_TIP); yield return Bekle(30); yield return Cek("menu_" + ek + "karakter_tip");
            ui.KarakterDene(Kozmetik.TIP, "guclu"); yield return Bekle(30); yield return Cek("menu_" + ek + "karakter_tip_deneme");
            ui.KarakterAc(UiKok.KAR_SAC); yield return Bekle(40); yield return Cek("menu_" + ek + "karakter_sac_yuz");
            ui.KarakterDene(Kozmetik.SAC, "afro"); yield return Bekle(40); yield return Cek("menu_" + ek + "karakter_sac_deneme");
            ui.KarakterAc(UiKok.KAR_AYK); yield return Bekle(40); yield return Cek("menu_" + ek + "karakter_ayakkabi_ayak");
            ui.KarakterDene("ayakkabi", "karbon"); yield return Bekle(30); yield return Cek("menu_" + ek + "karakter_ayakkabi_deneme");
            ui.KarakterAc(UiKok.KAR_TEN); yield return Bekle(30); yield return Cek("menu_" + ek + "karakter_ten");
            ui.SoyunmaSegment(2); ui.VitrinOdak(Vitrin.TUTUS, "drive"); yield return Bekle(40); yield return Cek("menu_" + ek + "yakin_tutus_forehand");
            ui.VitrinOdak(Vitrin.TUTUS, "bh_drive"); yield return Bekle(40); yield return Cek("menu_" + ek + "yakin_tutus_backhand");
            ui.SoyunmaSegment(0); yield return Bekle(10);
            ui.YuvaAc("raket"); yield return Bekle(20); yield return Cek("menu_" + ek + "yuva_raket"); ui.Geri(); yield return Bekle(5);
            ui.YuvaAc("tel"); yield return Bekle(20); yield return Cek("menu_" + ek + "yuva_kordaj"); ui.Geri(); yield return Bekle(5);
        }

        // Profil örnek ilerlemeyle (kaydedilmez, sonunda eskisi geri konur): ad, iki tur bitmiş (Yerel şampiyon + Asya fatihi
        // unvanları), maç/smaç sayısı; ad girilmemiş hâl yukarıdaki "profil" karesinde.
        IEnumerator Profil(UiKok ui, string ek)
        {
            var eski = Ayar.ilerleme;
            var p = new Ilerleme(); for (int t = 0; t < 2; t++) for (int b = 0; b < 6; b++) p.tur[t][b] = 7;
            p.Ad = "Yasin"; p.Unvan = "yerel"; for (int i = 0; i < 12; i++) p.MacSay(3); Ayar.ilerleme = p;
            ui.SekmeAc(Sekme.Oyna); ui.SekmeAc(Sekme.Profil); yield return Bekle(20); yield return Cek("menu_" + ek + "profil_ornek");
            Ayar.ilerleme = eski; ui.SekmeAc(Sekme.Oyna); ui.SekmeAc(Sekme.Profil); yield return Bekle(5);
        }

        // Patron maçında imza hazırlığı: örnek ilerlemeyle Tur 2 finali (Nova, mermi drive) ya da Tur 1 finali (Rocco, duvar
        // blok); bot oynar, hazırlık başlayınca (Athlete.imzaT > 0) birkaç kare sonra çekilir. En çok 60 sn beklenir.
        IEnumerator ImzaKare(string ad, int g, bool azalt)
        {
            var eski = Ayar.ilerleme; var q = new Ilerleme(); for (int b = 0; b < 6; b++) q.tur[0][b] = 1; for (int b = 0; b < 5; b++) q.tur[1][b] = 1; Ayar.ilerleme = q;
            bool r0 = Ayar.reduce, km = Ayar.kariyerModu; Ayar.reduce = azalt; Ayar.kariyerModu = true; Ayar.kariyerSecim = g;
            root.StartMatch(false, false, false); botAcik = true;
            float t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < 60 && !(root.match.op.imzaT > 0 && root.match.op.imzaId != null)) yield return null;
            yield return Bekle(6); yield return Cek(ad + (root.match.op.imzaT > 0 ? "" : "_yok"));
            botAcik = false; Ayar.ilerleme = eski; Ayar.reduce = r0; Ayar.kariyerModu = km; Ayar.kariyerSecim = -1;
        }
        // 2026-10-26: antrenman görevi oynanırken (köşe smacı: hedef köşe, sayaç kartı) ve sonuç ekranı; Günün maçı oynanırken.
        IEnumerator GorevKare()
        {
            root.DrilBaslat("kose"); botAcik = true; yield return Bekle(60 * 6); yield return Cek("gorev_kose_oyun");
            root.DrilBitir(); float t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < 10 && root.ekran != Ekran.Bitti) yield return null;
            yield return Bekle(20); yield return Cek("gorev_sonuc"); botAcik = false;
            root.GunlukBaslat(); botAcik = true; yield return Bekle(60 * 3); yield return Cek("gunun_maci_oyun"); botAcik = false;
        }
        // 2026-10-26 yön A sonuç ekranı: örnek ilerlemeyle (Tur 1 ilk basamak üç yıldız) ikinci basamak maçı 11–7 biter, gerçek
        // EndMatch akışı (GameRoot.EkranBitir) çalışır; yıldızlar düşüp Tüy/XP sayımı bitsin diye 3,5 sn gerçek zaman beklenir.
        // Kayda giden ödül (Tüy, XP) CI'nın geçici kaydıdır; ilerleme ve ayarlar sonunda geri konur.
        IEnumerator SonucKare(string ad, bool won, bool azalt, int yazi, string dil)
        {
            var eski = Ayar.ilerleme; var q = new Ilerleme(); q.tur[0][0] = 7; Ayar.ilerleme = q;
            bool r0 = Ayar.reduce, km = Ayar.kariyerModu; string d0 = Ayar.dil; Ayar.reduce = azalt; Ayar.kariyerModu = true; Ayar.kariyerSecim = KariyerVeri.Global(0, 1);
            string y0 = Ayar.yazi; if (yazi > 0) Ayar.yazi = "buyuk"; Ayar.dil = dil;   // sonuç ekranı IMGUI: yaziTest değil YaziK (×1,3)
            root.StartMatch(false, false, false); botAcik = false;
            { float t1 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t1 < 4) yield return null; }   // r16: maç başı rakip kartı kalksın (gerçek akışta maç sonunda yok)
            root.match.sc[0] = won ? 11 : 7; root.match.sc[1] = won ? 7 : 11; root.EkranBitir(won);
            float t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < 3.5f) yield return null;
            yield return Cek(ad);
            Ayar.ilerleme = eski; Ayar.reduce = r0; Ayar.kariyerModu = km; Ayar.kariyerSecim = -1; Ayar.yazi = y0; Ayar.dil = d0;
            root.AnaMenu(); yield return Bekle(5);
        }
        // ---- ekran/file (2026-10-27, yasin "file de standartlarda olmalı bant kısmı daha sert ama file hareketli olacak") ----
        // File yakın planı: dinlenik (ölçüler köşelerden rapora), ağa smaç (şişkinliğin tepesi ve 0,45 sn sonra yayılan dalga),
        // bant teması (bant az ve hızlı titrer, ağ az oynar), Hareketi azalt'ta aynı dalga (genlik ×0,35, zamanlama aynı).
        // Maç duraklatılır ve 1/240 sn adımlarla elle yürütülür: CI'da kare hızı düşük, an kareye bağlı kalmasın. Oyuncular
        // kadrajdan çekilir. Sayılar: görünen ağ/bant tepesi (m), yenilenen köşe, durulma ve uyku süresi (benzetim sn).
        IEnumerator FileKare()
        {
            Ayar.model = "oyuncak"; Ayar.saha = "salon"; root.SahaDegisti(); root.StartMatch(false, false, false); botAcik = false;
            yield return Bekle(30);
            var M = root.match; var nv = FindFirstObjectByType<NetView>(); if (nv == null) { rap.AppendLine("file: NetView YOK"); yield break; }
            root.paused = true; M.wait = 999; M.s = null; M.me.y = Court.L - .4f; M.op.y = .4f;
            for (int i = 0; i < 2400 && (M.net.active || M.net.glow > 0); i++) M.Update(1 / 240f);
            yield return Bekle(3);
            rap.AppendLine("=== file (ekran/file) · " + nv.Olcu());
            int u0 = nv.UykuKare; yield return Bekle(10); rap.AppendLine("file dinlenik: 10 karede uyuyan " + (nv.UykuKare - u0) + " · son yenilenen köşe " + nv.SonGuncel);
            Vector3 hedef = Kit.W(Court.MID - .5f, Court.NET, 1.05f), ofset = new Vector3(1.5f, .35f, -2.3f);
            yield return YakinNokta("file_01_dinlenik", hedef, ofset);
            // ağa smaç: 50 m/s, 1,0 m yükseklikte, file önünde 0,3 m'den
            yield return FileAt(M, nv, "file_02_ag_tepe", "file_03_ag_dalga", Court.MID - .5f, 1.0f, 50, hedef, ofset, false);
            yield return FileAt(M, nv, "file_04_bant", null, Court.MID - .5f, Court.NetLimit(Court.MID - .5f) - .04f, 20, hedef, ofset, false);
            yield return FileAt(M, nv, "file_05_ag_dalga_azalt", null, Court.MID - .5f, 1.0f, 50, hedef, ofset, true);
            M.net.sakin = Ayar.reduce; root.paused = false; M.wait = 0;
        }
        IEnumerator FileAt(Match M, NetView nv, string ad1, string ad2, float x, float z, float hiz, Vector3 hedef, Vector3 ofset, bool azalt)
        {
            M.net.sakin = azalt; M.between = false; M.wait = 999; M.me.y = Court.L - .4f; M.op.y = .4f;
            var s = new Shuttle { x = x, y = Court.NET + .3f, z = z, vy = -hiz, from = "me", type = "smash", kd = Shuttle.KD };
            s.landPred = Shuttle.Simulate(s.x, s.y, s.z, s.vx, s.vy, s.vz, true, s.kd); s.netPred = null; M.s = s;
            float t = 0, tHit = -1, agTepe = 0, bantTepe = 0, durul = -1, uyku = -1; string tur = "?";
            System.Func<float> agMax = () => { float m = 0; for (int i = 0; i <= 40; i++) { float xx = i / 40f * Court.CW, top = Court.NetLimit(xx); for (int j = 0; j < 10; j++) m = Mathf.Max(m, Mathf.Abs(M.net.Gorunen(xx, top - Court.BANT - (Court.AG_DERIN - Court.BANT) * (j + .5f) / 10))); } return m; };
            System.Func<float> bantMax = () => { float m = 0; for (int i = 0; i <= 40; i++) m = Mathf.Max(m, Mathf.Abs(M.net.BantAt(i / 40f * Court.CW))); return m; };
            // tepe anı: ağda temastan 0,07 sn sonra (şişkinlik en derin), bantta 0,03 sn sonra
            float an1 = z < Court.NetLimit(x) - Court.BANT ? .07f : .03f;
            for (int k = 0; k < 240 * 3 && (tHit < 0 || t - tHit < an1); k++) { if (M.s != null && !M.s.dead) M.Update(1 / 240f); else M.net.Advance(1 / 240f); t += 1 / 240f; if (tHit < 0 && M.s != null && (M.s.netHit || M.s.netCord)) { tHit = t; tur = M.s.agda ? "ağ" : M.s.netCord ? "kordon" : "bant"; } }
            float a1 = agMax(), b1 = bantMax(); yield return Bekle(2);
            int gunc = nv.SonGuncel; yield return YakinNokta(ad1, hedef, ofset);
            string sat = "file " + ad1 + ": tür " + tur + " hız " + hiz + " · tepe anı ağ " + (a1 * 100).ToString("0.0") + " cm bant " + (b1 * 1000).ToString("0.0") + " mm · yenilenen köşe " + gunc + "/792";
            float a2 = -1;
            for (int k = 0; k < 240 * 6; k++)
            {
                if (M.s != null && !M.s.dead) M.Update(1 / 240f); else M.net.Advance(1 / 240f); t += 1 / 240f;
                float am = agMax(), bm = bantMax(); agTepe = Mathf.Max(agTepe, am); bantTepe = Mathf.Max(bantTepe, bm);
                if (ad2 != null && a2 < 0 && t - tHit >= .45f) { a2 = am; yield return Bekle(2); sat += " · 0,45 sn ağ " + (am * 100).ToString("0.0") + " cm köşe " + nv.SonGuncel; yield return YakinNokta(ad2, hedef, ofset); }
                if (am > .001f) durul = -1; else if (durul < 0) durul = t - tHit;
                if (M.net.active || M.net.glow > 0) uyku = -1; else if (uyku < 0) { uyku = t - tHit; break; }
            }
            int u0 = nv.UykuKare; yield return Bekle(5);
            rap.AppendLine(sat + " · en çok ağ " + (agTepe * 100).ToString("0.0") + " cm bant " + (bantTepe * 1000).ToString("0.0") + " mm oran " + (bantTepe / Mathf.Max(1e-5f, agTepe)).ToString("0.000")
                + " · ağ durulma (<1 mm) " + durul.ToString("0.00") + " s uyku " + uyku.ToString("0.00") + " s · sonra 5 karede uyuyan " + (nv.UykuKare - u0) + (azalt ? " · Hareketi azalt" : ""));
            M.s = null;
        }
        // ---- ekran/file sonu ----

        // 2026-10-26 gece (yasin build 40: "menüler sahaya iç içe geçmiş"): maçtan menüye dönüşte menünün arkası. Ana kamera
        // menüde sahneyi çizmemeli (cullingMask 0), Oyna'nın fonu oyuncusuz bulanık salon karesi olmalı; saha değişince fon
        // yenilenmeli; duraklatıp Ana menü'ye çıkınca (bekleyen maç) Oyna "Maça devam et" göstermeli. Her karede kameranın
        // durumu rapora yazılır (görüntüde gözden kaçarsa sayı yakalar).
        IEnumerator MenuSahne()
        {
            var ui = root.ui; if (ui == null) yield break;
            ui.SekmeAc(Sekme.Kariyer); ui.SekmeAc(Sekme.Oyna); yield return Bekle(30); yield return Cek("menu_macsonu_oyna"); rap.AppendLine("menu_macsonu_oyna · " + ui.SahneDurumu);
            ui.SekmeAc(Sekme.Kariyer); yield return Bekle(20); yield return Cek("menu_macsonu_kariyer"); rap.AppendLine("menu_macsonu_kariyer · " + ui.SahneDurumu);
            ui.SekmeAc(Sekme.Soyunma); yield return Bekle(30); yield return Cek("menu_macsonu_soyunma"); rap.AppendLine("menu_macsonu_soyunma · " + ui.SahneDurumu);
            string s0 = Ayar.saha; Ayar.saha = "park"; root.SahaDegisti(); ui.SekmeAc(Sekme.Oyna); yield return Bekle(30); yield return Cek("menu_park_oyna"); rap.AppendLine("menu_park_oyna · " + ui.SahneDurumu);
            Ayar.saha = s0; root.SahaDegisti(); yield return Bekle(5);
            root.StartMatch(false, false, false); botAcik = true; yield return Bekle(60); botAcik = false; root.Duraklat(); yield return Bekle(10); yield return Cek("duraklat");
            root.AnaMenu(); yield return Bekle(30); yield return Cek("menu_bekleyen_oyna"); rap.AppendLine("menu_bekleyen_oyna · " + ui.SahneDurumu + " · bekleyen " + root.bekleyen);
            root.MacaDon(); yield return Bekle(30); yield return Cek("bekleyen_devam"); rap.AppendLine("bekleyen_devam · " + ui.SahneDurumu);
            root.AnaMenu(); yield return Bekle(10);
        }
        // Yüksek top kadrajı (2026-10-27, yasin build 41 "Top yükseldiğinde kadrajdan çıkıyor"; Core/Kadraj). Gerçek bir clear
        // (BotMac FizikOlcumleri'nin clear'ı: dip çizginin 1 m önünden, 2,55 m'den; tepe ~7 m) ve aynalanmış hâli (rakibin dip
        // çizgiden clear'ı) tepe anında çekilir: kadraj_tepe_ben_<açı><ek>.png, kadraj_tepe_rakip_<açı><ek>.png. Rapor satırı
        // topun ekran konumunu iki yoldan yazar (Unity kamerası ve Core/Kadraj modeli: aynı olmalı, BotMac sayıları buna dayanır),
        // üst sınırı (skor kutusunun altı), takip eğimini, görüş çarpanını, işaretin alfasını ve yakın dip çizginin yerini.
        IEnumerator KadrajTepe(string ek, bool azalt)
        {
            bool r0 = Ayar.reduce; Ayar.reduce = azalt; string a = azalt ? "_azalt" : "";
            foreach (var kim in new[] { "ben", "rakip" })
            {
                root.StartMatch(false, false, false); botAcik = false; yield return Bekle(20);
                var M = root.match; bool ben = kim == "ben"; float L = Court.L;
                M.s = new Shuttle { x = Court.MID, y = L - 1f, z = 2.55f, vy = 3, vz = -2, from = "op" }; M.me.x = Court.MID - .4f; M.me.y = L - 1f;
                var P = M.ShotPlan(new SwipeInput { dy = -150, speed = 1.2f, R = 40 }, new Reach(), true); M.s = null;
                float hx = P.hx, hy = ben ? P.hy : L - P.hy, ty = ben ? Court.NET - P.d : Court.NET + P.d, tx = ben ? P.tx : 2 * Court.MID - P.tx, vx, vy, vz;
                if (!ben) { hx = 2 * Court.MID - hx; M.op.x = hx + .4f; M.op.y = hy; }
                Shuttle.Aim(hx, hy, P.hz, tx, ty, P.byAngle, P.val, out vx, out vy, out vz);
                var sf = typeof(Match).GetMethod("StartFlight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                sf.Invoke(M, new object[] { ben ? "me" : "op", vx, vy, vz, hx, hy, P.hz, false, "clear", false, "good", false });
                float t0 = Time.realtimeSinceStartup;
                while (M.s != null && M.s.vz > 0 && Time.realtimeSinceStartup - t0 < 8) yield return null;
                string ad = "kadraj_tepe_" + kim + "_" + ek + a; yield return Cek(ad);
                var s = M.s; if (s == null) { rap.AppendLine(ad + " · top yok"); continue; }
                var w = root.cam.WorldToScreenPoint(Kit.W(s.x, s.y, s.z)); float H = Screen.height, sx, sy, dx, dy;
                BirdieSmash.Core.Kadraj.Izdus(root.kam, root.kadrajEgim, s.x, s.y, s.z, out sx, out sy); BirdieSmash.Core.Kadraj.Izdus(root.kam, root.kadrajEgim, Court.MID, L, 0, out dx, out dy);
                var isr = FindFirstObjectByType<KadrajIsaret>();
                bool gor = sy >= root.kam.ustPx && sy <= H && sx >= 0 && sx <= Screen.width;
                rap.AppendLine("kadraj_tepe " + ad + " · " + Screen.width + "x" + Screen.height + " · top z " + s.z.ToString("F2") + " (" + s.x.ToString("F1") + ", " + s.y.ToString("F1") + ") ekran Unity " + w.x.ToString("F0") + "," + (H - w.y).ToString("F0")
                    + " model " + sx.ToString("F0") + "," + sy.ToString("F0") + " · üst sınır " + root.kam.ustPx.ToString("F0") + " alt " + root.kam.altPx.ToString("F0") + " · " + (gor ? "GÖRÜNÜR" : "DIŞARIDA")
                    + " · eğim " + root.kadrajEgim.ToString("F1") + "° (tavan " + root.kam.egimAzami.ToString("F1") + ") görüş ×" + root.kadrajGenis.ToString("F2") + " · işaret " + (isr != null ? isr.SonAlfa.ToString("F2") : "-")
                    + " · yakinDip " + dy.ToString("F0") + " · azalt " + Ayar.reduce);
                File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
                // Tur 2 (GameRoot.KadrajSifirla): top tepedeyken duraklat › Ana menü; menünün ilk karesinde kamera düz olmalı (salon fonu
                // eğik/uzak çekilmesin). Satır: eğim 0.0, görüş ×1.00, kamera eğimi = çözüm eğimi. Kare: menu_kadraj_donus (fon düz).
                if (ben && !azalt && ek == "yayin")
                {
                    float egOnce = root.kadrajEgim; root.Duraklat(); root.AnaMenu(); root.ui.SekmeAc(Sekme.Kariyer); root.ui.SekmeAc(Sekme.Oyna); yield return Bekle(30); yield return Cek("menu_kadraj_donus");
                    rap.AppendLine("kadraj_menuye_donus · önce eğim " + egOnce.ToString("F1") + "° · şimdi eğim " + root.kadrajEgim.ToString("F1") + "° görüş ×" + root.kadrajGenis.ToString("F2") + " kamera x " + root.cam.transform.eulerAngles.x.ToString("F1") + "° (çözüm " + root.kam.p.ToString("F1") + "°)");
                }
            }
            Ayar.reduce = r0;
        }
        // Hareketi azalt (takip yok, işaret var) 390×845 Yayın'da ve Pro Max (430×932) Yayın'da.
        IEnumerator KadrajTepeEk(int w0, int h0)
        {
            yield return Boyut(w0, h0); Ayar.kamera = "yayin"; root.KameraDegisti(); yield return KadrajTepe("yayin", true);
            yield return Boyut(430, 932); root.KameraDegisti(); yield return KadrajTepe("yayin_pm", false);
        }
        // ---- 2026-10-27 duyarlı yerleşim (yasin build 41: "menüler ve sayfalar responsive, her telefonda açıkça gözükmeli") ----
        // Dört cihaz boyu, pencere pt cinsinden (1 piksel = 1 pt) ve güvenli alan taklidi (Guvenli.test; xvfb'de Screen.safeArea
        // tam ekran): SE 375×667 (çentik yok, durum çubuğu gizli: 0/0), iPhone 13 390×844 (47/34), Pro Max 430×932 (59/34), iPad
        // 768×1024 (24/20). Her boyda ana sayfalar, alt sayfalar ve sonuç ekranı; SE'de ayrıca ×2 yazı ve "Büyük" sonuç. Her
        // menü karesinde UiKok.TasmaDenetimi rapor.txt'ye "taşma <kare> · … · N sorun" satırı ve sorun listesini yazar; sonuç
        // karesinde Hud.SonucTani. Sonda toplam: "duyarlı özet". Kare adı menu_<boy>_<sayfa> (eski menu_* kareleri 390×845,
        // güvenli alansız: karşılaştırma için aynen kalır).
        static readonly string[] D_AD = { "se", "13", "pm", "ipad" };
        static readonly int[] D_W = { 375, 390, 430, 768 }, D_H = { 667, 844, 932, 1024 }, D_UST = { 0, 47, 59, 24 }, D_ALT = { 0, 34, 34, 20 };
        int dKare, dSorun;
        IEnumerator DuyarliTuru()
        {
            var ui = root.ui; if (ui == null) yield break;
            int w0 = Screen.width, h0 = Screen.height; dKare = 0; dSorun = 0;
            for (int i = 0; i < D_AD.Length; i++)
            {
                Guvenli.test = true; Guvenli.ust = D_UST[i]; Guvenli.alt = D_ALT[i]; Guvenli.sol = 0; Guvenli.sag = 0;
                yield return Boyut(D_W[i], D_H[i]); if (Screen.width != D_W[i] || Screen.height != D_H[i]) rap.AppendLine("duyarlı " + D_AD[i] + ": pencere istenen boyda değil, kareler yine çekilir");
                yield return DuyarliSayfalar(ui, "menu_" + D_AD[i] + "_");
                if (D_AD[i] == "se") { Ayar.yaziTest = 2; yield return Bekle(10); yield return DuyarliSayfalar(ui, "menu_se_x2_", true); Ayar.yaziTest = 0; yield return Bekle(10); }
                yield return SonucKare("sonuc_" + D_AD[i], true, false, 0, "en"); rap.AppendLine("sonuc_" + D_AD[i] + " · " + (root.HudKatman != null ? root.HudKatman.SonucTani : "hud yok"));
                if (D_AD[i] == "se") { yield return SonucKare("sonuc_se_buyuk", true, false, 2, "en"); rap.AppendLine("sonuc_se_buyuk · " + (root.HudKatman != null ? root.HudKatman.SonucTani : "hud yok")); }
                root.StartMatch(false, false, false); botAcik = true; yield return Bekle(60); yield return Cek("oyun_" + D_AD[i]); botAcik = false; root.AnaMenu(); yield return Bekle(10);
            }
            Guvenli.test = false; yield return Boyut(w0, h0); ui.SekmeAc(Sekme.Oyna); yield return Bekle(10);
            rap.AppendLine("duyarlı özet · " + dKare + " menü karesi · " + dSorun + " sorun"); File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
        }
        IEnumerator DuyarliSayfalar(UiKok ui, string ek, bool kisaca = false)
        {
            ui.SekmeAc(Sekme.Kariyer); ui.SekmeAc(Sekme.Oyna); yield return DCek(ui, ek + "oyna");
            ui.SekmeAc(Sekme.Kariyer); yield return DCek(ui, ek + "kariyer");
            ui.MacKartAc(KariyerVeri.Global(0, 0)); yield return DCek(ui, ek + "mac_karti"); ui.Geri();
            ui.SekmeAc(Sekme.Soyunma); yield return DCek(ui, ek + "soyunma", 30);
            if (!kisaca) { ui.SoyunmaSegment(UiKok.SEG_KARAKTER); yield return DCek(ui, ek + "karakter", 30); ui.SoyunmaSegment(0); yield return Bekle(5); }
            ui.SekmeAc(Sekme.Profil); yield return DCek(ui, ek + "profil");
            ui.SekmeAc(Sekme.Oyna); ui.HizliMacAc(); yield return DCek(ui, ek + "hizli"); ui.Geri();
            ui.AyarlarAc(); yield return DCek(ui, ek + "ayarlar"); ui.Geri(); yield return Bekle(5);
            if (kisaca) yield break;
            ui.AntrenmanAc(); yield return DCek(ui, ek + "antrenman");
            ui.DrilKartAc("kose"); yield return DCek(ui, ek + "gorev_karti"); ui.Geri(); ui.Geri(); yield return Bekle(5);
            ui.GunlukAc(); yield return DCek(ui, ek + "gunun_maci"); ui.Geri(); yield return Bekle(5);
        }
        IEnumerator DCek(UiKok ui, string ad, int bekle = 20)
        {
            yield return Bekle(bekle); yield return Cek(ad); int n; rap.AppendLine(ui.TasmaDenetimi(ad, out n)); dKare++; dSorun += n;
        }

        // 2026-10-27 yön C "Gün Batımı Salonu" perde v2 ve açılış anı: perdenin anları dondurularak çekilir (Hud.PerdeKare;
        // CI'da kare hızı düşük, gerçek zamanlı yakalama rastgele bir ana düşerdi). Ad eki = perde saati (ms). Her karenin sayıları
        // (top yeri/boyu/yönü, menü örtüsü, kayma, kaynak) rapora. Önce sekme göstergesinin konumu (kayma animasyonu karede
        // görünmez, sayıyla).
        IEnumerator PerdeTuru()
        {
            var hud = root.HudKatman; var ui = root.ui; if (hud == null || ui == null) yield break;
            ui.SekmeAc(Sekme.Kariyer); yield return Bekle(20); rap.AppendLine("sekme göstergesi (Kariyer) · " + ui.GostergeTani);
            ui.SekmeAc(Sekme.Oyna); yield return Bekle(20); rap.AppendLine("sekme göstergesi (Oyna) · " + ui.GostergeTani);
            bool r0 = Ayar.reduce; Ayar.reduce = false;
            foreach (var k in new[] { .03f, .10f, .16f, .24f, .32f, .40f }) { ui.PerdeKaynakCta(); hud.PerdeKare(false, 1, k); yield return PerdeCek("gecis_kapanis_" + Ms(k)); }
            hud.PerdeKare(false, 2, .3f); yield return PerdeCek("gecis_kapali");
            hud.PerdeKare(false, 2, 1f, true); yield return PerdeCek("gecis_bekle");
            foreach (var k in new[] { .08f, .18f, .28f }) { hud.PerdeKare(false, 3, k); yield return PerdeCek("gecis_acilis_" + Ms(k)); }
            Ayar.reduce = true;
            hud.PerdeKare(false, 1, .1f); yield return PerdeCek("gecis_azalt_kapanis_0100");
            hud.PerdeKare(false, 2, .3f); yield return PerdeCek("gecis_azalt_kapali");
            hud.PerdeKare(false, 3, .12f); yield return PerdeCek("gecis_azalt_acilis_0120");
            Ayar.reduce = false;
            foreach (var k in new[] { 0f, .10f, .25f, .40f, .55f, .65f }) { hud.PerdeKare(true, 2, k); yield return PerdeCek("acilis_" + Ms(k)); }
            hud.PerdeKare(true, 2, 0, true); yield return PerdeCek("acilis_bekle");
            foreach (var k in new[] { .08f, .18f }) { hud.PerdeKare(true, 3, k); yield return PerdeCek("acilis_patlama_" + Ms(k)); }
            Ayar.reduce = true; hud.PerdeKare(true, 2, .3f); yield return PerdeCek("acilis_azalt");
            Ayar.reduce = r0; hud.PerdeBirak(); yield return Bekle(10);
        }
        IEnumerator PerdeCek(string ad) { yield return Bekle(4); yield return Cek(ad); rap.AppendLine(ad + " · " + root.HudKatman.PerdeTani); }
        static string Ms(float t) { return Mathf.RoundToInt(t * 1000).ToString("0000"); }
        // 2026-10-27 model/varyant turu (yasin build 41: "3B model kaybolmuş, seçmeme rağmen görmedim"). r42'de tip ve saç
        // denemesi vitrinde uygulanıyordu ama göze farkı küçüktü (Güçlü omuz +%11, Afro saç sınırı kısa kesimden yalnız birkaç
        // cm geniş, tepesi Yüz kadrajından taşıyordu) ve Gerçekçi/Sade'de hiç parça yok. Burada her beden tipi, saç biçimi ve
        // ayakkabı modeli vitrinde (oyuncunun dokunuşuyla aynı yol: deneme) ve her tip maçta (seçim olarak, bir saç ve ayakkabıyla)
        // çekilir; her kare için rapora "varyant" satırı: PlayerView.VaryantOzet (yüklenen FBX, açık saç/ayakkabı parçası, aktif
        // renderer, baş yüksekliği, omuz, saç/ayakkabı sınırı), serinin ilkine göre fark ve beklenen parça açılmadıysa
        // "PARÇA YOK" / renderer yoksa "GÖRÜNMEZ". Sonra Gerçekçi seçiliyken Karakter sayfası (uyarı kartı) ve Ayarlar, Sade
        // maçı, modeller arası gidiş-dönüşten sonra maçta figür. Oyuncunun seçimi sonunda geri konur, kayda yazılmaz.
        static readonly string[,] VARYANT_MAC = { { "atlet", "afro", "karbon" }, { "kadin", "topuz", "hafif" }, { "uzun", "mohikan", "bilekli" }, { "guclu", "rasta", "yastikli" } };
        IEnumerator Varyantlar()
        {
            var ui = root.ui; if (ui == null || Ayar.gear == null) yield break;
            string m0 = Ayar.model, t0 = Ayar.KOZ[Kozmetik.TIP], s0 = Ayar.KOZ[Kozmetik.SAC], a0 = Ayar.gear.ayakkabi;
            rap.AppendLine("=== varyantlar (vitrin: deneme yolu, maç: seçim) · ekran " + Screen.width + "x" + Screen.height);
            Ayar.model = "oyuncak"; Ayar.KOZ[Kozmetik.TIP] = "atlet"; Ayar.KOZ[Kozmetik.SAC] = "kisa"; Ayar.gear.ayakkabi = "klasik"; root.GorunumDegisti(); root.ModelDegisti();
            root.AnaMenu(); yield return Bekle(10); ui.SekmeAc(Sekme.Soyunma); yield return Bekle(20);
            VaryantTaban t = null;
            ui.KarakterAc(UiKok.KAR_TIP); yield return Bekle(20);
            foreach (var o in Kozmetik.Liste(Kozmetik.TIP))
            {
                ui.KarakterDene(Kozmetik.TIP, o.id); yield return Bekle(25); yield return Cek("model_vitrin_tip_" + o.id);
                VaryantSatir("vitrin tip=" + o.id, root.vitrin.Fig, PlayerView.ModelYolu("oyuncak", o.id), null, null, ref t);
            }
            t = null; ui.KarakterAc(UiKok.KAR_SAC); yield return Bekle(30);
            foreach (var o in Kozmetik.Liste(Kozmetik.SAC))
            {
                ui.KarakterDene(Kozmetik.SAC, o.id); yield return Bekle(20); yield return Cek("model_vitrin_sac_" + o.id);
                VaryantSatir("vitrin sac=" + o.id, root.vitrin.Fig, null, o.varlik ?? "yok", null, ref t);
            }
            t = null; ui.KarakterAc(UiKok.KAR_AYK); yield return Bekle(30);
            foreach (var a in Gear.GEAR["ayakkabi"].Values)
            {
                ui.KarakterDene("ayakkabi", a.id); yield return Bekle(25); yield return Cek("model_vitrin_ayk_" + a.id);
                VaryantSatir("vitrin ayk=" + a.id, root.vitrin.Fig, null, null, a.varlik ?? a.id, ref t);
            }
            ui.SoyunmaSegment(0); yield return Bekle(5);
            // maçta: seçim olarak (Ayar.KOZ + ayakkabı modeli), GameRoot.Kur yolu; yakın plan önden ve arkadan
            t = null;
            for (int i = 0; i < VARYANT_MAC.GetLength(0); i++)
            {
                string tip = VARYANT_MAC[i, 0], sac = VARYANT_MAC[i, 1], ayk = VARYANT_MAC[i, 2];
                Ayar.KOZ[Kozmetik.TIP] = tip; Ayar.KOZ[Kozmetik.SAC] = sac; Ayar.gear.ayakkabi = ayk;
                root.StartMatch(false, false, false); botAcik = false; yield return Bekle(45);
                yield return Yakin("model_mac_" + tip, root.match.me, true);
                var ka = Gear.GEAR["ayakkabi"][ayk];
                VaryantSatir("maç tip=" + tip + " sac=" + sac + " ayk=" + ayk, Ben(), PlayerView.ModelYolu("oyuncak", tip), Kozmetik.Bul(Kozmetik.SAC, sac).varlik, ka.varlik ?? ka.id, ref t);
            }
            yield return Dizilis();
            // başka model seçiliyken: Karakter sayfası uyarı kartı (deneme figürü değiştirmez) ve Ayarlar'daki üç model
            Ayar.KOZ[Kozmetik.TIP] = "atlet"; Ayar.KOZ[Kozmetik.SAC] = "kisa"; Ayar.gear.ayakkabi = "klasik";
            Ayar.model = "rocketbox"; root.GorunumDegisti(); root.ModelDegisti(); root.AnaMenu(); yield return Bekle(10);
            ui.SekmeAc(Sekme.Soyunma); yield return Bekle(20); ui.KarakterAc(UiKok.KAR_TIP); yield return Bekle(10); ui.KarakterDene(Kozmetik.TIP, "guclu"); yield return Bekle(30); yield return Cek("model_gercekci_karakter_uyari");
            t = null; VaryantSatir("vitrin gerçekçi tip=guclu (parça beklenmez)", root.vitrin.Fig, "rocketbox", null, null, ref t);
            ui.SoyunmaSegment(0); ui.AyarlarAc(); yield return Bekle(20); yield return Cek("model_ayarlar_gercekci"); ui.Geri(); yield return Bekle(5);
            // Sade maçta ve modeller arası gidiş-dönüş (rocketbox → sade → oyuncak): figür her seferinde çiziliyor mu
            foreach (var mdl in new[] { "sade", "oyuncak" })
            {
                if (!Ayar.ModelVar(mdl)) continue;
                Ayar.model = mdl; root.ModelDegisti(); root.StartMatch(false, false, false); botAcik = false; yield return Bekle(45);
                yield return Cek("model_gecis_" + mdl); t = null; VaryantSatir("maç geçiş model=" + mdl, Ben(), mdl, null, null, ref t);
                root.AnaMenu(); yield return Bekle(20); ui.SekmeAc(Sekme.Soyunma); yield return Bekle(20); yield return Cek("model_gecis_" + mdl + "_vitrin");
                t = null; VaryantSatir("vitrin geçiş model=" + mdl, root.vitrin.Fig, mdl, null, null, ref t);
            }
            Ayar.KOZ[Kozmetik.TIP] = t0; Ayar.KOZ[Kozmetik.SAC] = s0; Ayar.gear.ayakkabi = a0; Ayar.model = m0;
            root.GorunumDegisti(); root.ModelDegisti(); root.AnaMenu(); ui.SekmeAc(Sekme.Oyna); yield return Bekle(5);
            File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
        }
        // Oyuncak oranı turu (2026-10-27, yasin: "Oyuncak 3B daha tıknaz sevimli karikatür tipler olmalıydı"): dört tip yan
        // yana (maç kamerası, önden, arkadan-yüksekten) ve 11 saç bir ızgarada (arkadan, yüksek kameradan, önden). Figürler
        // maç sahnesinde geçici bir düğümün altında kurulur (Ben() onları saymaz), benim figürüm çekim boyunca gizli.
        // Rapor satırı "siluet …": tipin ölçüsü (baş = baş kemiği yüksekliği, omuz, sınır kutusu boyu/eni) ve atlete göre fark;
        // ≥ %10 fark beklenir (yüksek kameradan ayırt edilsin).
        IEnumerator Dizilis()
        {
            var me = root.match != null ? root.match.me : null; var ben = Ben(); if (me == null || ben == null) yield break;
            var dugum = new GameObject("dizilis").transform; dugum.SetParent(root.transform, false);
            ben.gameObject.SetActive(false);
            Vector3 p0 = Kit.W(me.x, me.y, 0) + Vector3.forward * 1.2f;
            var tipler = Kozmetik.Liste(Kozmetik.TIP); var figs = new List<PlayerView>(); VaryantTaban t = null;
            for (int i = 0; i < tipler.Length; i++)
            {
                var g = new PlayerView.Gorunum { forma = Kit.Hex(Ayar.FORMA), cerceve = Kit.Hex("#e9eef0"), tip = tipler[i].id };
                var f = PlayerView.Build(dugum, "oyuncak", true, g.forma, false, g); f.ShowPoz(p0 + Vector3.right * ((i - (tipler.Length - 1) * .5f) * 1.0f), 0, "idle", 0); figs.Add(f);
            }
            yield return Bekle(8);
            for (int i = 0; i < figs.Count; i++)
            {
                VaryantSatir("dizilis tip=" + tipler[i].id, figs[i], PlayerView.ModelYolu("oyuncak", tipler[i].id), null, null, ref t);
                Bounds b; if (Sinir(figs[i], out b)) rap.AppendLine("siluet tip=" + tipler[i].id + " · boy " + (b.max.y - figs[i].transform.position.y).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " m · en " + b.size.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " m (raket hariç)");
            }
            yield return Cek("model_dizilis_mac");
            yield return YakinNokta("model_dizilis_on", p0 + Vector3.up * .8f, new Vector3(0, .5f, 6.5f), 1280, 720);
            yield return YakinNokta("model_dizilis_arka", p0 + Vector3.up * .6f, new Vector3(0, 4.0f, -6.0f), 1280, 720);
            foreach (var f in figs) Destroy(f.gameObject); figs.Clear(); yield return null;
            // saç ızgarası: atlet, 11 saç, 2 sıra (önde 6, arkada 5), 0,85 m arayla
            var saclar = Kozmetik.Liste(Kozmetik.SAC); t = null;
            for (int i = 0; i < saclar.Length; i++)
            {
                int sira = i < 6 ? 0 : 1, k = sira == 0 ? i : i - 6, n = sira == 0 ? 6 : saclar.Length - 6;
                var g = new PlayerView.Gorunum { forma = Kit.Hex(Ayar.FORMA), cerceve = Kit.Hex("#e9eef0"), tip = "atlet", sacModel = saclar[i].id };
                var f = PlayerView.Build(dugum, "oyuncak", true, g.forma, false, g);
                f.ShowPoz(p0 + Vector3.right * ((k - (n - 1) * .5f) * .85f) + Vector3.forward * (sira * 1.1f), 0, "idle", 0); figs.Add(f);
            }
            yield return Bekle(8);
            for (int i = 0; i < figs.Count; i++) VaryantSatir("sac izgara sac=" + saclar[i].id, figs[i], null, saclar[i].varlik ?? "yok", null, ref t);
            var orta = p0 + Vector3.forward * .55f + Vector3.up * 1.2f;
            yield return YakinNokta("model_sac_izgara_arka", orta, new Vector3(0, 1.6f, -5.5f), 1280, 720);
            yield return YakinNokta("model_sac_izgara_yuksek", orta, new Vector3(0, 6.5f, -3.5f), 1280, 720);
            yield return YakinNokta("model_sac_izgara_on", orta, new Vector3(0, .6f, 5.5f), 1280, 720);
            Destroy(dugum.gameObject); ben.gameObject.SetActive(true); yield return Bekle(2);
        }
        static bool Sinir(PlayerView pv, out Bounds b)
        {
            b = new Bounds(); bool var_ = false;
            foreach (var r in pv.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r.name.StartsWith("raket") || r.name == "golge") continue;
                if (!var_) { b = r.bounds; var_ = true; } else b.Encapsulate(r.bounds);
            }
            return var_;
        }
        class VaryantTaban { public float bas, omuz; public Vector3 sac; }
        // Sahnedeki benim figürüm (GameRoot'un doğrudan çocuğu; vitrin ve afiş figürleri kendi düğümlerinin altında).
        PlayerView Ben() { foreach (Transform c in root.transform) { var pv = c.GetComponent<PlayerView>(); if (pv != null && pv.isMe && !pv.hakem) return pv; } return null; }
        void VaryantSatir(string ad, PlayerView pv, string prefab, string sac, string ayk, ref VaryantTaban t)
        {
            if (pv == null) { rap.AppendLine("varyant " + ad + " · FİGÜR YOK"); return; }
            float b, o; Vector3 sb; int n; string oz = pv.VaryantOzet(out b, out o, out sb, out n);
            var ci = System.Globalization.CultureInfo.InvariantCulture; var sorun = new StringBuilder();
            if (n == 0 || !pv.gameObject.activeInHierarchy) sorun.Append(" GÖRÜNMEZ");
            if (prefab != null && pv.PrefabAdi != prefab) sorun.Append(" PARÇA YOK prefab " + pv.PrefabAdi + "≠" + prefab);
            if (sac != null && (pv.AcikSac ?? "yok") != sac) sorun.Append(" PARÇA YOK saç " + (pv.AcikSac ?? "yok") + "≠" + sac);
            if (ayk != null && (pv.AcikAyk ?? "yok") != ayk) sorun.Append(" PARÇA YOK ayk " + (pv.AcikAyk ?? "yok") + "≠" + ayk);
            string fark = "";
            if (t == null) t = new VaryantTaban { bas = b, omuz = o, sac = sb };
            else
            {
                System.Func<float, float, string> Y = (x, x0) => x > 0 && x0 > 0 ? ((x / x0 - 1) * 100).ToString("+0.0;-0.0;0.0", ci) + "%" : "-";
                fark = " · fark (serinin ilkine göre) baş " + Y(b, t.bas) + " omuz " + Y(o, t.omuz) + " saç gen " + Y(Mathf.Max(sb.x, sb.z), Mathf.Max(t.sac.x, t.sac.z)) + " saç boy " + Y(sb.y, t.sac.y);
            }
            rap.AppendLine("varyant " + ad + " · " + oz + fark + (sorun.Length > 0 ? " ·" + sorun : " · tamam"));
        }
        IEnumerator Bekle(int kare) { for (int i = 0; i < kare; i++) yield return null; }
        IEnumerator Boyut(int w, int h)
        {
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
            for (int i = 0; i < 120 && (Screen.width != w || Screen.height != h); i++) yield return null;
            yield return Bekle(10); rap.AppendLine("ekran boyu istendi " + w + "x" + h + " · oldu " + Screen.width + "x" + Screen.height);
        }

        void Update()
        {
            kare = Time.frameCount;
            if (!botAcik || root == null || root.match == null || root.ekran != Ekran.Oyun) return;
            var M = root.match;
            if (M.s == null && M.server == "me" && M.wait <= 0 && !M.between && M.svPending == null && rng.NextDouble() < .08)
            {
                double u = rng.NextDouble();
                if (u < .45) M.SwipeServe((float)(rng.NextDouble() * 80 - 40), -60, .6f);
                else if (u < .7) M.SwipeServe((float)(rng.NextDouble() * 40 - 20), -70, 1.6f);
                else M.SwipeServe((float)(rng.NextDouble() * 160 - 80), -200, 1.8f);
            }
            Reach R;
            if (M.ReachInfo(out R) && rng.NextDouble() < .2)
            {
                double k = rng.NextDouble(); float sp = k < .2 ? .5f : k < .65 ? 1.2f : 2.2f;
                M.SwipeShot(new SwipeInput { dx = (float)(rng.NextDouble() * 200 - 100), dy = -(float)(80 + rng.NextDouble() * 220), speed = sp, R = 40, dist = (float)(rng.NextDouble() * 30), side = rng.NextDouble() < .5 ? -1 : 1 });
            }
        }

        IEnumerator Cek(string ad)
        {
            adim = ad; Debug.Log("EkranTuru kare " + Time.frameCount + " t=" + Time.realtimeSinceStartup.ToString("F1") + " → " + ad);
            yield return new WaitForEndOfFrame();
            // ScreenCapture modülü projede yok: kare sonunda arka tampondan oku (OnGUI HUD'u dahil).
            var t = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false); RenderTexture.active = null;
            t.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); t.Apply();
            File.WriteAllBytes(Path.Combine(dir, ad + ".png"), t.EncodeToPNG()); Destroy(t);
        }

        // Yakın plan: ana kamerayı bir kareliğine figürün önüne al (HUD'suz, ayrı kamera ile RenderTexture'a).
        IEnumerator Yakin(string ad, Athlete o, bool ben)
        {
            var p = Kit.W(o.x, o.y, 0) + Vector3.up * 1.0f;
            float s = ben ? -1 : 1;   // ben -Z tarafındayım, yüzüm +Z'ye (fileye) bakar; önümden çekmek için fileden bak
            yield return YakinNokta(ad, p, new Vector3(1.2f, .6f, 3.2f * -s));
            yield return YakinNokta(ad + "_arka", p, new Vector3(-.8f, .7f, -3.2f * -s));
        }

        IEnumerator YakinNokta(string ad, Vector3 hedef, Vector3 ofset, int w = 720, int h = 960)
        {
            yield return new WaitForEndOfFrame();
            var g = new GameObject("yakin_kamera"); var c = g.AddComponent<Camera>(); c.CopyFrom(root.cam); c.ResetProjectionMatrix(); c.fieldOfView = 40;   // ana kamera özel (eksen dışı) matris taşır, 2026-10-26
            g.transform.position = hedef + ofset; g.transform.LookAt(hedef);
            var rt = new RenderTexture(w, h, 24); c.targetTexture = rt; c.aspect = (float)w / h; c.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = prev; c.targetTexture = null;
            File.WriteAllBytes(Path.Combine(dir, ad + ".png"), tex.EncodeToPNG());
            Destroy(tex); Destroy(g); rt.Release(); Destroy(rt);
        }

        // Tanı: her figürün renderer'ları (tür, açık mı, shader, render kuyruğu, saydamlık), dünya sınırları, baş yüksekliği.
        // Kadraj ölçümü (2026-10-27, kadraj incelemesi): çizgilerin ekran yüksekliği (piksel, yukarıdan), tekler genişliği, file
        // bandı, oyuncu boyları (model sınırının tepesi − ayak) ve kamera. r15/r22/r27 karşılaştırma tablosu bu satırlardan.
        void Kadraj(string ad)
        {
            var c = root.cam; float H = Screen.height;
            System.Func<Vector3, float> y = p => H - c.WorldToScreenPoint(p).y;
            System.Func<Vector3, float> x = p => c.WorldToScreenPoint(p).x;
            var sb = new StringBuilder("kadraj " + ad + " · " + Screen.width + "x" + Screen.height);
            sb.Append(" · uzakDip " + y(Kit.W(Court.MID, 0, 0)).ToString("F0") + " file " + y(Kit.W(Court.MID, Court.NET, 0)).ToString("F0") + " fileUst " + y(Kit.W(Court.MID, Court.NET, Court.NETP)).ToString("F0"));
            sb.Append(" yakinDip " + y(Kit.W(Court.MID, Court.L, 0)).ToString("F0") + " uzakCiftlerEn " + (x(Kit.W(Court.CW, 0, 0)) - x(Kit.W(0, 0, 0))).ToString("F0") + " yakinTeklerEn " + (x(Kit.W(Court.SR, Court.L, 0)) - x(Kit.W(Court.SL, Court.L, 0))).ToString("F0"));
            foreach (var pv in FindObjectsByType<PlayerView>(FindObjectsSortMode.None))
            {
                if (pv.Model == null || pv.name.StartsWith("hakem")) continue; float tepe = -1; 
                foreach (var r in pv.Model.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is TrailRenderer)) tepe = Mathf.Max(tepe, r.bounds.max.y);
                var ayak = pv.transform.position; ayak.y = 0; var bas = ayak; bas.y = tepe;
                sb.Append(" " + pv.name + "Boy " + (y(ayak) - y(bas)).ToString("F0") + "@" + y(ayak).ToString("F0"));
            }
            var e = c.transform.eulerAngles; var m = c.projectionMatrix;
            sb.Append(" · kamera " + c.transform.position.ToString("F2") + " eğim " + e.x.ToString("F1") + " fov " + (2 * Mathf.Atan(1 / m.m11) * Mathf.Rad2Deg).ToString("F1") + " m12 " + m.m12.ToString("F3"));
            rap.AppendLine(sb.ToString()); File.WriteAllText(Path.Combine(dir, "rapor.txt"), rap.ToString());
        }

        void Tani(string mdl)
        {
            rap.AppendLine("=== " + mdl + " · ekran " + Screen.width + "x" + Screen.height + " · " + SystemInfo.graphicsDeviceName);
            foreach (var pv in FindObjectsByType<PlayerView>(FindObjectsSortMode.None))
            {
                rap.AppendLine("-- " + pv.name + " poz " + pv.transform.position + (pv.Model != null ? " model ölçek " + pv.Model.lossyScale : " MODEL YOK"));
                if (pv.Model == null) continue;
                Bounds b = new Bounds(); bool ilk = true;
                foreach (var r in pv.Model.GetComponentsInChildren<Renderer>(true))
                {
                    var m = r.sharedMaterial; string sh = m == null ? "null" : m.shader == null ? "shader null" : m.shader.name;
                    string ek = m == null ? "" : " q" + m.renderQueue + " renk " + (m.HasProperty("_Color") ? m.color.ToString() : "-") + " mode " + (m.HasProperty("_Mode") ? m.GetFloat("_Mode").ToString() : "-") + " tex " + (m.mainTexture != null ? m.mainTexture.name + " " + m.mainTexture.width : "yok");
                    var smr = r as SkinnedMeshRenderer;
                    rap.AppendLine("   " + r.GetType().Name + " " + r.name + " açık " + r.enabled + "/" + r.gameObject.activeInHierarchy + " " + sh + ek + " sınır " + r.bounds.center + " " + r.bounds.size + (smr != null ? " kemik " + smr.bones.Length + " mesh " + (smr.sharedMesh != null ? smr.sharedMesh.vertexCount.ToString() : "YOK") : ""));
                    if (r.enabled) { if (ilk) { b = r.bounds; ilk = false; } else b.Encapsulate(r.bounds); }
                }
                rap.AppendLine("   toplam sınır " + b.center + " boyut " + b.size + " · ileri " + pv.Model.forward + " yukarı " + pv.Model.up);
            }
        }
    }
}
