// Dünya uzayı efektleri (HTML draw(): waves, parts, M.marks, runMark, ogrHedefCiz). 3B karşılıkları:
//   iz (mark): top yere düştüğü yerde koyu leke (3 sn'de söner), ilk .5 sn genişleyen beyaz halka, smaçta 5 limon çatlak.
//   dalga (wave): vuruş noktasında kameraya dönük genişleyen halka (smaçta limon, büyük); hareketi azalt açıkken yok.
//   parçacık: tüy (uzun elips, döner), kıvılcım (yıldız, eklemeli), çizgi (streak: benim vuruşumda dışa fırlayan ışık çizgileri).
//   koşu çizgisi ve erişim halkası: yalnız Usta'da (koşu sende); erişim 1,15 m.
//   antrenman hedef alanı: rakip kortunda soluk limon bant + kesikli kenar ("Hedef" yazısı Hud'da).
// Hareketi azalt: parçacık üçte bir, dalga yok (HTML burst/wave ile aynı).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class Fx : MonoBehaviour
    {
        class Mark { public Vector3 p; public float t; public bool big; public int n; public float hiz = 1; public float[] cr; public Transform leke; public Renderer lekeR; public LineRenderer halka; public LineRenderer[] catlak; }
        class Wave { public Vector3 p; public float t, max; public Color c; public LineRenderer l; }
        readonly List<Mark> marks = new List<Mark>(); readonly List<Wave> waves = new List<Wave>();
        // Dalga havuzu (İnceleme turu 2026-11-10 17:08 #4): her vuruşta yeni LineRenderer yaratılıp ~.45 sn sonra siliniyordu
        // (ralli boyunca GC ve Destroy). Biten dalga kapatılıp burada bekler; aynı anda en çok birkaç dalga olduğu için havuz
        // kendiliğinden küçük kalır. Yere düşüş izi (Iz) sayı başına bir kez, havuza alınmadı.
        readonly Stack<LineRenderer> dalgaHavuz = new Stack<LineRenderer>();
        // 2026-11-13 (inceleme 12:05 #2): iz lekesi her top düşüşünde 2–7 nesne yaratıp 3 sn sonra siliyordu; sayı anı (sevinç,
        // skor nabzı, alkış, kamera) en yüklü kare olduğu için yarat/yok et oraya düşüyordu. İzler havuzda: leke + halka + 5 çatlak
        // bir kez kurulur, sonra açılıp kapanır (aynı anda en çok ~3 iz yaşar). Dalga noktaları da tek dizi + tek SetPositions.
        readonly Stack<Mark> izHavuz = new Stack<Mark>(); readonly Vector3[] dalgaNok = new Vector3[40];
        ParticleSystem tuy, kivilcim, cizgi; Material lekeM, beyazM, catlakM; Camera cam; MaterialPropertyBlock lekeMpb;
        static readonly int RENK = Shader.PropertyToID("_Color");
        LineRenderer kosu, erisim, hedefKenar; Transform kosuNokta, hedefBant; Material hedefM;

        public static Fx Build(Transform root, Camera cam)
        {
            var g = new GameObject("efekt"); g.transform.SetParent(root, false); var f = g.AddComponent<Fx>(); f.cam = cam; f.Init(); return f;
        }
        ParticleSystem PS(string ad, Material m, ParticleSystemRenderMode mode)
        {
            var g = new GameObject(ad); g.transform.SetParent(transform, false); var ps = g.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var mn = ps.main; mn.loop = false; mn.playOnAwake = false; mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.maxParticles = 400; mn.gravityModifier = .35f;
            var e = ps.emission; e.enabled = false; var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) } });
            var r = g.GetComponent<ParticleSystemRenderer>(); r.material = m; r.renderMode = mode; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (mode == ParticleSystemRenderMode.Stretch) { r.velocityScale = .05f; r.lengthScale = 1; }
            return ps;
        }
        void Init()
        {
            tuy = PS("tuy", Kit.Clear(Color.white, TuyTex()), ParticleSystemRenderMode.Billboard);
            var rot = tuy.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-5, 5);
            kivilcim = PS("kivilcim", Kit.Additive(Color.white, YildizTex()), ParticleSystemRenderMode.Billboard);
            cizgi = PS("cizgi", Kit.Additive(Color.white, Kit.RadialTex()), ParticleSystemRenderMode.Stretch); var cm = cizgi.main; cm.gravityModifier = 0;
            // Arcade "pat" eğrisi (2026-11-05, yasin: "patlama, yıldız ve top izi dikkat dağıtmayacak ama ilgi de çekecek"):
            // eskiden parçacık doğduğu boyda sabit kalıp .4–.9 sn boyunca doğrusal soluyor, yerçekimiyle süzülüyordu; ekranda
            // uzun süre asılı kalan yıldız yağmuru oyun alanını kalabalıklaştırıyordu. Şimdi yıldız ilk %12'de büyüyüp parlar
            // (dikkat çeken an), hızını çabuk kaybeder (sürtünme) ve küçülerek söner; tüy de yarı ömürde incelir. Toplam
            // görünür süre ~yarıya indi, ilk an daha canlı.
            Pat(kivilcim, true, .9f); Pat(cizgi, false, 0); Pat(tuy, false, .6f);
            lekeM = Kit.Clear(new Color(10 / 255f, 30 / 255f, 26 / 255f, .45f), Kit.RadialTex());
            // 2026-10-31 (Unity incelemesi Y1): dalga, iz halkası ve çatlak her olayda yeni Material açıyordu; tek sahnede
            // UnloadUnusedAssets çağrılmadığı için maç başına yüzlerce malzeme birikiyordu. Renk zaten LineRenderer köşe
            // rengiyle veriliyor (Sprites/Default köşe × malzeme), bu yüzden üç malzeme paylaşılır; leke solması MPB ile.
            beyazM = Kit.Clear(Color.white); catlakM = Kit.Clear(new Color(.847f, 1, .353f, .6f)); lekeMpb = new MaterialPropertyBlock();
            var mavi = new Color(79 / 255f, 184 / 255f, 1, .5f);
            kosu = Kit.Line("kosu", transform, Kit.Clear(mavi, Kit.DashTex(2, 5)), false, true); kosu.textureMode = LineTextureMode.Tile; kosu.widthMultiplier = .04f;
            kosuNokta = Kit.Prim(PrimitiveType.Quad, "kosu_nokta", transform, Vector3.zero, new Vector3(.3f, .3f, 1), Kit.Clear(new Color(mavi.r, mavi.g, mavi.b, .6f), Kit.RadialTex())).transform; kosuNokta.rotation = Quaternion.Euler(90, 0, 0);
            erisim = Kit.Line("erisim", transform, Kit.Clear(new Color(mavi.r, mavi.g, mavi.b, .16f)), true, true); erisim.widthMultiplier = .025f;
            hedefM = Kit.Clear(new Color(.847f, 1, .353f, .10f));
            hedefBant = Kit.Prim(PrimitiveType.Quad, "hedef_bant", transform, Vector3.zero, Vector3.one, hedefM).transform; hedefBant.rotation = Quaternion.Euler(90, 0, 0);
            hedefKenar = Kit.Line("hedef_kenar", transform, Kit.Clear(new Color(.847f, 1, .353f, .6f), Kit.DashTex(6, 5)), true, true); hedefKenar.textureMode = LineTextureMode.Tile; hedefKenar.widthMultiplier = .04f;
        }
        static void Pat(ParticleSystem ps, bool buyu, float surtunme)
        {
            var so = ps.sizeOverLifetime; so.enabled = true;
            so.size = new ParticleSystem.MinMaxCurve(1, buyu ? new AnimationCurve(new Keyframe(0, .4f), new Keyframe(.12f, 1.25f), new Keyframe(.45f, .9f), new Keyframe(1, 0))
                                                           : new AnimationCurve(new Keyframe(0, 1), new Keyframe(.5f, .85f), new Keyframe(1, 0)));
            var col = ps.colorOverLifetime;
            col.color = new ParticleSystem.MinMaxGradient(new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .45f), new GradientAlphaKey(0, 1) } });
            if (surtunme > 0) { var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = new ParticleSystem.MinMaxCurve(99); lv.drag = new ParticleSystem.MinMaxCurve(surtunme * 3); lv.multiplyDragByParticleSize = false; lv.multiplyDragByParticleVelocity = false; }
        }
        static Texture2D TuyTex()
        {   // uzun elips (HTML ellipse r·.35 × r)
            const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1; float r = Mathf.Sqrt(dx * dx / (.35f * .35f) + dy * dy); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - r) * 4))); }
            t.Apply(); return t;
        }
        static Texture2D YildizTex()
        {   // 8 köşeli yıldız (HTML spark)
            const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                { float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, a = Mathf.Atan2(dy, dx), r = Mathf.Sqrt(dx * dx + dy * dy); float lim = Mathf.Lerp(.3f, 1, Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2)), 3)); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((lim - r) * 6))); }
            t.Apply(); return t;
        }

        // HTML burst(x, y, col, n, spd, kind): hız px/sn → m/sn (1/28), ömür .4–.9 sn
        public void Burst(Vector3 at, Color c, int n, float spd, string kind)
        {
            if (Ayar.reduce) n = Mathf.CeilToInt(n / 3f);
            var ps = kind == "feather" ? tuy : kind == "spark" ? kivilcim : cizgi;
            var ep = new ParticleSystem.EmitParams { position = at, applyShapeToPosition = false };
            for (int i = 0; i < n; i++)
            {
                // Ömür: yıldız .3–.55, tüy .35–.65 sn (eskiden hepsi .4–.9). Yıldız hızı sürtünmeyle düştüğü için başlangıçta biraz
                // daha hızlı (×1,25): patlamanın yarıçapı aynı, yalnız daha çabuk açılıp duruyor.
                bool yildiz = kind == "spark"; float s = spd / 28f * Random.Range(.3f, 1.2f) * (yildiz ? 1.25f : 1); ep.velocity = Random.onUnitSphere * s + Vector3.up * .8f; ep.startColor = c;
                ep.startLifetime = yildiz ? Random.Range(.3f, .55f) : kind == "feather" ? Random.Range(.35f, .65f) : Random.Range(.4f, .9f);
                ep.startSize = kind == "feather" ? Random.Range(.18f, .32f) : Random.Range(.06f, .16f); ep.rotation = Random.Range(0, 360f); ps.Emit(ep, 1);
            }
        }
        // benim vuruşumda dışa fırlayan ışık çizgileri (HTML streak: 600 px/sn, .18 sn)
        public void Streak(Vector3 at, Color c, int n)
        {
            if (Ayar.reduce) return; var ep = new ParticleSystem.EmitParams { position = at };
            for (int i = 0; i < n; i++) { var d = Random.onUnitSphere; d.y *= .5f; ep.velocity = d.normalized * 21; ep.startColor = c; ep.startLifetime = .18f; ep.startSize = .05f; cizgi.Emit(ep, 1); }
        }
        public void Dalga(Vector3 at, Color c, float maxPx)
        {
            if (Ayar.reduce) return; LineRenderer l;
            if (dalgaHavuz.Count > 0) { l = dalgaHavuz.Pop(); l.enabled = true; } else l = Kit.Line("dalga", transform, beyazM, true);
            waves.Add(new Wave { p = at, max = maxPx / 28f, c = c, l = l });
        }
        public void Iz(float x, float y, bool big)
        {
            Mark m;
            if (izHavuz.Count > 0) { m = izHavuz.Pop(); m.leke.gameObject.SetActive(true); }
            else
            {
                m = new Mark { cr = new float[5], catlak = new LineRenderer[5] };
                m.leke = Kit.Prim(PrimitiveType.Quad, "iz", transform, Vector3.zero, Vector3.one, lekeM).transform; m.leke.rotation = Quaternion.Euler(90, 0, 0); m.lekeR = m.leke.GetComponent<Renderer>();
                m.halka = Kit.Line("iz_halka", transform, beyazM, true, true); m.halka.widthMultiplier = .04f;
                for (int i = 0; i < 5; i++) { var l = Kit.Line("catlak", transform, catlakM, false, true); l.widthMultiplier = .03f; l.positionCount = 2; m.catlak[i] = l; }
            }
            m.p = Kit.W(x, y, .007f); m.big = big; m.t = 0; m.hiz = 1; m.n = big ? 5 : 0;
            m.leke.localPosition = m.p; m.leke.localScale = new Vector3((big ? 9 : 5) / 28f * 2.6f, (big ? 9 : 5) / 28f * 2.6f, 1);
            m.halka.enabled = true;
            for (int i = 0; i < 5; i++)
            {
                var l = m.catlak[i]; l.enabled = i < m.n; if (!l.enabled) continue;
                m.cr[i] = Random.value * 6.28f; l.SetPosition(0, m.p); l.SetPosition(1, m.p + new Vector3(Mathf.Cos(m.cr[i]), 0, Mathf.Sin(m.cr[i])) * 18 / 28f);
            }
            marks.Add(m);
        }
        // 2026-11-13 (yasin: "sayı olduktan sonra topun gölgesi servis pozisyonundayken bir süre daha kalıyor"): düşüş lekesi koyu yeşil bir
        // disk ve 3 sn sönüyordu; sayı arası 1,3 sn (kısa .7) olduğu için oyuncular servis yerine geçtikten sonra 1,7–2,3 sn daha
        // yerde "top gölgesi" gibi duruyordu. Servis kurulunca kalan izler .25 sn'de söner: saat hızlanır, saydamlık kesintisiz iner.
        public void IzSondur() { foreach (var m in marks) m.hiz = Mathf.Max(m.hiz, (3 - m.t) / .25f); }
        void IzBirak(Mark m) { m.leke.gameObject.SetActive(false); m.halka.enabled = false; for (int i = 0; i < m.catlak.Length; i++) m.catlak[i].enabled = false; izHavuz.Push(m); }
        // Gölgelendirici ısıtma (2026-11-07, Ar-Ge 02:06 #1): üç parçacık sistemi oyunda ilk kez ilk sert vuruşta
        // çiziliyordu; Metal yeni gölgelendirici varyantını ilk çizimde derlediği için takılma tam ilk smaçta geliyordu.
        // Maç perdesi kapalıyken kameranın önüne her sistemden alfa 0, .05 sn ömürlü tek parçacık bırakılır: GPU bir kez
        // çizer (varyant derlenir), görüntüde hiçbir şey görünmez; akıcılık kapısı perdeyi zaten bu kareler bitene dek tutar.
        public void Isit()
        {
            if (cam == null) return;
            var ep = new ParticleSystem.EmitParams { position = cam.transform.position + cam.transform.forward * 3, applyShapeToPosition = false, velocity = Vector3.zero, startColor = new Color32(255, 255, 255, 0), startLifetime = .05f, startSize = .1f };
            tuy.Emit(ep, 1); kivilcim.Emit(ep, 1); cizgi.Emit(ep, 1);
        }
        public void Temizle()
        {
            foreach (var m in marks) IzBirak(m);
            marks.Clear(); foreach (var w in waves) { w.l.enabled = false; dalgaHavuz.Push(w.l); } waves.Clear();
        }

        public void Tick(Match M, float dt, bool oyun)
        {
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                var m = marks[i]; m.t += dt * m.hiz; float a = Mathf.Max(0, 1 - m.t / 3);
                var mm = m.lekeR;   // 2026-11-11 (fikir 23:05 #8): her karede GetComponent yerine kurulumda bir kez lekeMpb.SetColor(RENK, new Color(10 / 255f, 30 / 255f, 26 / 255f, .45f * a * 1.4f)); mm.SetPropertyBlock(lekeMpb);
                m.halka.enabled = m.t < .5f; if (m.halka.enabled) { Kit.Ellipse(m.halka, m.p, (8 + m.t * 80) / 28f, (8 + m.t * 80) / 28f); m.halka.startColor = m.halka.endColor = new Color(1, 1, 1, (.5f - m.t) * 1.6f); }
                for (int k = 0; k < m.n; k++) m.catlak[k].startColor = m.catlak[k].endColor = new Color(.847f, 1, .353f, .6f * a);
                if (m.t >= 3) { IzBirak(m); marks.RemoveAt(i); }
            }
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var w = waves[i]; w.t += dt * 2.2f;   // HTML dalga ~.45 sn
                if (w.t >= 1) { w.l.enabled = false; dalgaHavuz.Push(w.l); waves.RemoveAt(i); continue; }
                float e = 1 - Mathf.Pow(1 - w.t, 3); var c = w.c; c.a = (1 - w.t) * .8f; w.l.startColor = w.l.endColor = c; w.l.widthMultiplier = (5 * (1 - w.t) + 1) / 28f;
                // kameraya dönük çember
                int n = 40; w.l.positionCount = n; var r = cam.transform.right * w.max * e; var u = cam.transform.up * w.max * e * .5f;
                for (int k = 0; k < n; k++) { float an = k * Mathf.PI * 2 / n; dalgaNok[k] = w.p + r * Mathf.Cos(an) + u * Mathf.Sin(an); }
                w.l.SetPositions(dalgaNok);
            }
            // Usta: koşu çizgisi ve erişim halkası
            bool usta = oyun && M != null && !M.easy && M.s != null;
            kosu.enabled = false; kosuNokta.gameObject.SetActive(false); erisim.enabled = usta;
            if (usta)
            {
                Kit.Ellipse(erisim, Kit.W(M.me.x, M.me.y, .009f), 1.15f, 1.15f);
                if (M.me.HasTarget) { var a0 = Kit.W(M.me.x, M.me.y, .01f); var b0 = Kit.W(M.me.tx, M.me.ty, .01f); if ((b0 - a0).magnitude > .4f) { kosu.enabled = true; kosu.positionCount = 2; kosu.SetPosition(0, a0); kosu.SetPosition(1, b0); kosuNokta.gameObject.SetActive(true); kosuNokta.position = b0; } }
            }
            // antrenman hedef alanı
            float[] h = HedefAlan(oyun ? M : null);
            hedefBant.gameObject.SetActive(h != null); hedefKenar.enabled = h != null;
            if (h != null)
            {
                float x0 = h[0], x1 = h[1], y0 = h[2], y1 = h[3]; hedefBant.position = Kit.W((x0 + x1) / 2, (y0 + y1) / 2, .006f); hedefBant.localScale = new Vector3(x1 - x0, y1 - y0, 1);
                hedefKenar.positionCount = 4; hedefKenar.SetPosition(0, Kit.W(x0, y0, .008f)); hedefKenar.SetPosition(1, Kit.W(x1, y0, .008f)); hedefKenar.SetPosition(2, Kit.W(x1, y1, .008f)); hedefKenar.SetPosition(3, Kit.W(x0, y1, .008f));
            }
        }
        // Hedef alanı [x0, x1, y0, y1]: Temel derste vuruş türünün bandı (kort genişliği), antrenman görevinde görevin alanı
        // (2026-10-26, Core/Dril.DrilAlan: köşe smacında sırayla sol/sağ köşe, kısa serviste servis kutusunun ön bandı).
        // Alanı olmayan görevde (aldatma, savunma, file öldürme, split) bant çizilmez: ölçü düşüş yerinde değil.
        static float[] HedefAlan(Match M)
        {
            var oa = M != null ? M.OgrA() : null; if (oa == null || oa.bilgi != null) return null;
            if (oa.dril != null) return M.DrilAlan();
            if (oa.servis != null) return null; string k = M.OgrIstenen(oa); float[] h;
            if (k == null || !Match.OGR_HEDEF.TryGetValue(k, out h)) return null; hBuf[0] = Court.SL; hBuf[1] = Court.SR; hBuf[2] = h[0]; hBuf[3] = h[1]; return hBuf;
        }
        // Tek tampon (2026-11-11, fikir 23:05 #8): Tick her karede, Hud karede 2+ kez soruyordu; okuyan hemen kullanır, saklamaz.
        static readonly float[] hBuf = new float[4];
        public Vector3? HedefMerkez(Match M)
        {
            var h = HedefAlan(M); if (h == null) return null; return Kit.W((h[0] + h[1]) / 2, (h[2] + h[3]) / 2, .01f);
        }
    }
}
