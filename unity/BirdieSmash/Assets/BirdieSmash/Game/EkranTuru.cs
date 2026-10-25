// Ekran görüntüsü turu (2026-10-25, yasin build 38: "her turda düzelt ss al"): bulutta Unity Editor'ü yok, cihaz da yok;
// gerçek Unity render'ını görmenin tek yolu CI'da Linux oyuncusunu xvfb altında koşmak (.github/workflows/unity-ekran.yml).
// Yalnız `-bsekran <klasör>` argümanıyla açılır; mağaza build'inde hiç çalışmaz (argüman verilmez).
// Tur: iki model × saha temaları; menü, maç başı (servis), ralliden kareler (bot oynar: BotMac ile aynı karar), yakın
// planlar (ben, rakip, hakemler) ve tanı dosyası (rapor.txt: her figürün renderer'ları, shader'ları, sınırları, yönü).
// Süre: Time.captureDeltaTime = 1/30; render hızı ne olursa olsun simülasyon aynı adımla ilerler.
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
        bool botAcik;

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
            Directory.CreateDirectory(dir); Time.captureDeltaTime = 1f / 30;
            yield return null; root = FindFirstObjectByType<GameRoot>();
            Ayar.antrenman = true; Ayar.easy = true; Ayar.target = 5; Ayar.reduce = false;
            string[] modeller = { "oyuncak", "rocketbox" };
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
            yield return YakinNokta(mdl + "_05_hakemler", CourtView.HAKEM1 + new Vector3(0, .8f, 0), new Vector3(3.5f, 1.2f, -2.5f));
            botAcik = true;
            for (int i = 0; i < 10; i++) { yield return Bekle(14); yield return Cek(mdl + "_1" + i + "_ralli"); }
            botAcik = false;
        }

        IEnumerator Bekle(int kare) { for (int i = 0; i < kare; i++) yield return null; }

        void Update()
        {
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
