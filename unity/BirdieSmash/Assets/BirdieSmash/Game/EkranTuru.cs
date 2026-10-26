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
// Süre: gerçek zaman (GameRoot dt'yi 0,05 ile sınırlar; yavaş yazılım render'ında maç da yavaşlar, kareler yine anlamlı).
using System.Collections;
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
            yield return MenuTuru();
            Ayar.antrenman = true; Ayar.easy = true; Ayar.target = 5; Ayar.reduce = false;
            yield return MenuTuru("", true); Ayar.yaziTest = 2; yield return MenuTuru("x2_", true); Ayar.yaziTest = 0; root.ui.SekmeAc(Sekme.Oyna); yield return Bekle(5);
            var modeller = new System.Collections.Generic.List<string> { "oyuncak", "rocketbox" };
            if (Resources.Load<GameObject>("karakter/sade") != null) modeller.Add("sade");
            foreach (var mdl in modeller)
            {
                Ayar.model = mdl; Ayar.saha = "salon"; root.ModelDegisti(); root.SahaDegisti();
                yield return Bekle(40); yield return Cek(mdl + "_01_menu");
                yield return Mac(mdl);
            }
            foreach (var s in new[] { "turnuva", "okul", "park" })
            {
                Ayar.model = "oyuncak"; Ayar.saha = s; root.SahaDegisti(); root.StartMatch(false, false, false); botAcik = true;
                yield return Bekle(60); yield return Cek("saha_" + s);
            }
            foreach (var k in new[] { "alcak", "ust", "yayin" })
            {
                Ayar.kamera = k; Ayar.saha = "salon"; root.SahaDegisti(); root.KameraDegisti(); root.StartMatch(false, false, false); botAcik = true;
                yield return Bekle(60); yield return Cek("kamera_" + k);
            }
            Ayar.model = "oyuncak"; Ayar.saha = "salon"; Ayar.kamera = "yayin"; root.SahaDegisti(); root.KameraDegisti();
            yield return ImzaKare("imza_mermi", KariyerVeri.Global(1, 5), false);
            yield return ImzaKare("imza_duvar_azalt", KariyerVeri.Global(0, 5), true);
            yield return GorevKare();
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
            botAcik = true;
            for (int i = 0; i < 10; i++) { yield return Bekle(14); yield return Cek(mdl + "_1" + i + "_ralli"); }
            botAcik = false;
        }

        // Menü kareleri: açılış perdesi kalkana kadar bekler (sesler hazırlanınca, en çok 12 sn).
        IEnumerator MenuTuru(string ek = "ilk_", bool hepsi = false)
        {
            var ui = root != null ? root.ui : null; if (ui == null) { Debug.Log("EkranTuru: menü kabuğu yok"); yield break; }
            float t0 = Time.realtimeSinceStartup; while (root.HudKatman != null && root.HudKatman.PerdeVar && Time.realtimeSinceStartup - t0 < 12) yield return null;
            ui.SekmeAc(Sekme.Oyna); yield return Bekle(20); yield return Cek("menu_" + ek + "oyna");
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
            ui.DrilKartAc("kose"); yield return Bekle(20); yield return Cek("menu_" + ek + "gorev_karti"); ui.Geri(); ui.Geri(); yield return Bekle(5);
            ui.GunlukAc(); yield return Bekle(20); yield return Cek("menu_" + ek + "gunun_maci"); ui.Geri();
            dp.GunlukKazan(Gunluk.Tarih(System.DateTime.Now)); ui.SekmeAc(Sekme.Kariyer); ui.SekmeAc(Sekme.Oyna); yield return Bekle(20); yield return Cek("menu_" + ek + "oyna_gunluk_alindi");
            Ayar.ilerleme = eskiP; ui.SekmeAc(Sekme.Kariyer); ui.SekmeAc(Sekme.Oyna); yield return Bekle(5);
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
            ui.SekmeAc(Sekme.Oyna); yield return Bekle(20); yield return Cek("menu_" + ek + "oyna_usta"); ui.SekmeAc(Sekme.Kariyer);
            Ayar.ilerleme = eski; ui.KariyerTurSec(-1); yield return Bekle(5);
        }

        // Soyunma odası (2026-10-26, ekipman v2 + karakter): Tarz ve Görünüm segmentleri, raket ve kordaj yuva sayfaları (model
        // kartları, 5 çubuk, ▲/▼). Oyuncunun kendi kaydıyla: seçim değiştirilmez, yalnız bakılır; sonunda Ekipman'a dönülür.
        IEnumerator Soyunma(UiKok ui, string ek)
        {
            ui.SoyunmaSegment(1); yield return Bekle(20); yield return Cek("menu_" + ek + "soyunma_tarz");
            ui.SoyunmaSegment(2); yield return Bekle(20); yield return Cek("menu_" + ek + "soyunma_gorunum");
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
        IEnumerator Bekle(int kare) { for (int i = 0; i < kare; i++) yield return null; }

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

        IEnumerator YakinNokta(string ad, Vector3 hedef, Vector3 ofset)
        {
            yield return new WaitForEndOfFrame();
            var g = new GameObject("yakin_kamera"); var c = g.AddComponent<Camera>(); c.CopyFrom(root.cam); c.fieldOfView = 40;
            g.transform.position = hedef + ofset; g.transform.LookAt(hedef);
            var rt = new RenderTexture(720, 960, 24); c.targetTexture = rt; c.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(720, 960, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 720, 960), 0, 0); tex.Apply();
            RenderTexture.active = prev; c.targetTexture = null;
            File.WriteAllBytes(Path.Combine(dir, ad + ".png"), tex.EncodeToPNG());
            Destroy(tex); Destroy(g); rt.Release(); Destroy(rt);
        }

        // Tanı: her figürün renderer'ları (tür, açık mı, shader, render kuyruğu, saydamlık), dünya sınırları, baş yüksekliği.
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
