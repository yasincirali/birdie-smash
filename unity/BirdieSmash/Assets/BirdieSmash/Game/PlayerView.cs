// Oyuncu: Blender figürünün FBX'i (tool/karakter/unity_fbx.py → Resources/karakter/<model>.fbx). İki model, HTML'deki
// Ayarlar › Oyuncu görünümü ile aynı: "oyuncak" (Oyuncak 3B, KayKit Rogue CC0, varsayılan) ve "rocketbox" (Gerçekçi,
// Microsoft Rocketbox Sports_Male_04, MIT). İkisi de iskeletli; 26 klip Legacy Animation olarak normalize zamanla
// örneklenir: klip ve faz Match.PickClip'ten gelir (HTML player() ile aynı karar), vuruş anı topla kare kare eşleşir.
// Faz → klip zamanı: sprite kare k'yı (k+.5)/n fazında gösterir; FBX'te pozlar eşit aralıklı olduğu için aynı faz aynı
// pozu verir (döngülü kliplerde kapanış pozu ek). Klip değişiminde 0,08 s harmanlama; vuruş klibinden boşta/ayak
// klibine dönüşte 0,2 s (Ar-Ge 23 Eki animasyon B1: takip pozundan hazır duruşa "ışınlanma" yok; sprite'ta harmanlama
// hayalet yapıyordu, iskelette yapmaz, bu yüzden Blender'da yeniden render gerekmedi). FBX yoksa basit kapsül figür.
// Renk: parça adının "__" sonrası etiketi (HTML sprRenk/tint maskesi karşılığı): forma = takım rengi, cerceve = raket
// modelinin rengi, bant/bilek = seçili kafa bandı/bileklik (ekipman "Yok" ise gizli), grip/tel/tel2 = raket görünümü
// (tuytop_raket_gor; tel2 yalnız "ana ve çapraz ayrı" desende farklı). Rakipte raket #e9eef0, bant/bilek yok.
// Diğer parçalar FBX'in kendi malzemesini korur (Rocketbox dokusu, KayKit renkleri).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class PlayerView : MonoBehaviour
    {
        public bool isMe; Transform model, yaw; Animation anim; string cur, prev; float blend = 1, blendS = .08f, prevPh, lastPh;
        Transform fbRacket, shadow; LineRenderer ringBg, ringFg; float ringA;
        const float SCALE = 1.75f;   // sprite sürümündeki 1,75 kat okunurluk büyütmesi
        static readonly Dictionary<string, bool> LOOPS = new Dictionary<string, bool>();
        public bool hakem;

        [System.Serializable] class ClipMeta { public string name; public int poses; public bool loop; }
        [System.Serializable] class Meta { public ClipMeta[] clips; }

        // Görünüm: null alan = FBX'in kendi rengi (cerceve/grip/tel) ya da gizli (bant/bilek).
        public class Gorunum { public Color forma; public Color? cerceve, bant, bilek, grip, tel, tel2; }
        public static Gorunum Benim()
        {
            var gs = Ayar.GS; var r = Ayar.RGOR;
            return new Gorunum { forma = Kit.Hex(gs.forma), cerceve = Kit.Hex(gs.raketRenk), bant = gs.bant != null ? Kit.Hex(gs.bant) : (Color?)null, bilek = gs.bileklik != null ? Kit.Hex(gs.bileklik) : (Color?)null,
                grip = Kit.Hex(Vitrin.RAK_RENK[r["grip"]]), tel = Kit.Hex(Vitrin.RAK_RENK[r["tel"]]), tel2 = Kit.Hex(Vitrin.RAK_RENK[r["desen"] == "iki" ? r["tel2"] : r["tel"]]) };
        }
        public static PlayerView Build(Transform root, string model, bool isMe, Color forma, bool hakem = false, Gorunum gor = null)
        {
            var g = new GameObject(hakem ? "hakem" : isMe ? "ben" : "rakip"); g.transform.SetParent(root, false);
            var pv = g.AddComponent<PlayerView>(); pv.isMe = isMe; pv.hakem = hakem; pv.Init(model, gor ?? new Gorunum { forma = forma, cerceve = Kit.Hex("#e9eef0") }); return pv;
        }

        void Init(string modelAd, Gorunum gor)
        {
            yaw = new GameObject("yon").transform; yaw.SetParent(transform, false);
            var prefab = Resources.Load<GameObject>("karakter/" + modelAd) ?? Resources.Load<GameObject>("karakter/oyuncak");
            var js = Resources.Load<TextAsset>("karakter/" + modelAd + "_clips");
            if (js != null)
            {
                var meta = JsonUtility.FromJson<Meta>(js.text);
                if (meta != null && meta.clips != null) foreach (var c in meta.clips) { if (c.poses > 0) Match.CLIP_N[c.name] = c.poses; LOOPS[c.name] = c.loop; }
            }
            if (prefab != null)
            {
                model = Instantiate(prefab, yaw).transform; model.localPosition = Vector3.zero; model.localScale = Vector3.one * SCALE;
                anim = model.GetComponentInChildren<Animation>();
                Paint(gor);
                if (anim != null && anim["idle"] != null) Sample("idle", 0, null, 0, 1);
                Calibrate();
            }
            else Fallback(gor.forma);
            shadow = Kit.Prim(PrimitiveType.Quad, "golge", transform, new Vector3(0, .003f, 0), new Vector3(.9f, .9f, 1), Kit.Clear(new Color(0, 0, 0, .35f), Kit.RadialTex())).transform;
            shadow.localRotation = Quaternion.Euler(90, 0, 0);
            if (!hakem) { ringBg = Ring("nefes_zemin", .11f); ringFg = Ring("nefes", .08f); }
            yaw.localRotation = Quaternion.Euler(0, isMe ? 0 : 180, 0) * calib;
        }

        LineRenderer Ring(string n, float w)
        {
            var g = new GameObject(n); g.transform.SetParent(transform, false); var lr = g.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.widthMultiplier = w; lr.material = Kit.Clear(Color.white); lr.numCapVertices = 4; lr.positionCount = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; return lr;
        }
        // Nefes halkası (HTML nefesHalka): doluyken görünmez; azaldıkça iki uçtan öne (kameraya) doğru kısalır,
        // %40 altında turuncu (yanına "Nefes"/"Yorgun" yazılır: anlam yalnız renkle verilmez), %20 altında nabız atar.
        void ShowRing(float n, float dt, bool reduce)
        {
            if (ringBg == null) return;
            ringA = Mathf.Clamp01(ringA + (n < .985f ? 1 : -1) * dt * 3.6f);
            bool on = ringA > 0; ringBg.enabled = ringFg.enabled = on; if (!on) return;
            const float R = .55f; const int N = 48;
            ringBg.positionCount = N + 1; for (int i = 0; i <= N; i++) { float a = i * Mathf.PI * 2 / N; ringBg.SetPosition(i, new Vector3(Mathf.Cos(a) * R, .01f, Mathf.Sin(a) * R)); }
            ringBg.startColor = ringBg.endColor = new Color(.03f, .06f, .07f, .55f * ringA);
            int m = Mathf.Max(2, Mathf.RoundToInt(N * n)); float c0 = -Mathf.PI / 2, half = n * Mathf.PI;   // kameraya bakan nokta: dünya -z
            ringFg.positionCount = n > .01f ? m + 1 : 0;
            for (int i = 0; i <= m && n > .01f; i++) { float a = c0 - half + 2 * half * i / m; ringFg.SetPosition(i, new Vector3(Mathf.Cos(a) * R, .012f, Mathf.Sin(a) * R)); }
            float nab = n < .2f && !reduce ? .55f + .45f * Mathf.Sin(Time.unscaledTime * 1000 / 110f) : 1;
            var col = n < .4f ? Kit.Hex("#ff9f7a") : Kit.Hex("#7ad7ff"); col.a = ringA * (n < .4f ? nab : 1);
            ringFg.startColor = ringFg.endColor = col;
        }
        public float RingAlpha { get { return ringA; } }

        Quaternion calib = Quaternion.identity;
        // FBX eksen dönüşümüne güvenme: "ileri"yi işaretlerden çıkar (goz__isaret − bas__isaret), +Z'ye çevir.
        void Calibrate()
        {
            Transform head = null; var eyes = new List<Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>())
            {
                if (t.name.StartsWith("bas__")) head = t;
                if (t.name.StartsWith("goz") && t.name.Contains("__")) eyes.Add(t);
            }
            if (head == null || eyes.Count == 0) return;
            Vector3 e = Vector3.zero; foreach (var t in eyes) e += t.position; e /= eyes.Count;
            Vector3 f = yaw.InverseTransformDirection(e - head.position); f.y = 0;
            if (f.sqrMagnitude < 1e-8f) return;
            calib = Quaternion.FromToRotation(f.normalized, Vector3.forward);
            if (head.position.y - model.position.y < .3f) Debug.LogWarning("Birdie Smash: karakterin başı beklenenden alçak, FBX eksenlerini kontrol et");
        }

        public static string Etiket(string n)
        {
            int i = n.IndexOf("__"); string tag = i >= 0 ? n.Substring(i + 2) : "";
            int dot = tag.IndexOfAny(new[] { '.', ' ' }); if (dot >= 0) tag = tag.Substring(0, dot); return tag;
        }
        // Parça kanalları (HTML turParlat): 0 forma, 1 raket (cerceve/grip/tel/tel2), 2 bant, 3 bileklik.
        public readonly List<Renderer>[] kanal = { new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
        public Transform Model { get { return model; } }
        void Paint(Gorunum g)
        {
            var formaM = Kit.Lit(g.forma, .25f); var M = new Dictionary<string, Material> { { "forma", formaM } };
            if (g.cerceve.HasValue) M["cerceve"] = Kit.Lit(g.cerceve.Value, .6f);
            if (g.bant.HasValue) M["bant"] = Kit.Lit(g.bant.Value, .2f); if (g.bilek.HasValue) M["bilek"] = Kit.Lit(g.bilek.Value, .2f);
            if (g.grip.HasValue) M["grip"] = Kit.Lit(g.grip.Value, .15f); if (g.tel.HasValue) M["tel"] = Kit.Lit(g.tel.Value, .5f); if (g.tel2.HasValue) M["tel2"] = Kit.Lit(g.tel2.Value, .5f);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string tag = Etiket(r.gameObject.name);
                // hakemde raket ve top (pozla kurulan rijit parçalar = deri ağı olmayan MeshRenderer'lar) gizli
                if (tag == "isaret" || (tag == "bant" && !g.bant.HasValue) || (tag == "bilek" && !g.bilek.HasValue) || (hakem && !(r is SkinnedMeshRenderer))) { r.enabled = false; continue; }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                int kn = tag == "forma" ? 0 : tag == "cerceve" || tag == "grip" || tag == "tel" || tag == "tel2" ? 1 : tag == "bant" ? 2 : tag == "bilek" ? 3 : -1; if (kn >= 0) kanal[kn].Add(r);
                Material m; M.TryGetValue(tag, out m);
                if (m == null) { if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name.Contains("Error")) m = Kit.Lit(Color.gray); else continue; }
                var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = m; if (arr.Length == 0) arr = new[] { m }; r.sharedMaterials = arr;
            }
            if (hakem) { var hg = Kit.Lit(Kit.Hex("#0a1530"), .2f); foreach (var r in model.GetComponentsInChildren<Renderer>()) if (r.gameObject.name.Contains("__sort")) r.sharedMaterial = hg; }
            // Salınım yayı (HTML player(): sw > .3 iken omuz çevresinde 3 beyaz yay, alfa sw·.5): 3B'de raket kafasının izi.
            // Hareketi azalt açıkken çizilmez (HTML ile aynı).
            if (!hakem) foreach (var t in model.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("raket_kafa")) { kafa = t; break; }
            if (kafa != null)
            {
                yay = kafa.gameObject.AddComponent<TrailRenderer>(); yay.time = .12f; yay.minVertexDistance = .03f; yay.emitting = false;
                yay.widthCurve = new AnimationCurve(new Keyframe(0, .14f), new Keyframe(1, 0)); yay.material = Kit.Additive(Color.white);
                yay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; yay.receiveShadows = false;
            }
        }
        Transform kafa; TrailRenderer yay;
        // Yükseltme parlaması (HTML turParlat): o kanalın parçaları limona yanıp söner; k 0..1.
        public void Parlat(int kn, float k)
        {
            foreach (var r in kanal[kn]) foreach (var m in r.materials)
            {
                if (k > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.85f, 1, .35f) * k * 1.2f); }
                else { m.SetColor("_EmissionColor", Color.black); m.DisableKeyword("_EMISSION"); }
            }
        }

        void Fallback(Color forma)
        {
            model = new GameObject("yedek").transform; model.SetParent(yaw, false);
            Kit.Prim(PrimitiveType.Capsule, "govde", model, new Vector3(0, .8f, 0), new Vector3(.45f, .55f, .35f), Kit.Lit(forma));
            Kit.Prim(PrimitiveType.Sphere, "bas", model, new Vector3(0, 1.55f, 0), Vector3.one * .38f, Kit.Lit(Kit.Hex("#eeb890")));
            if (hakem) return;
            fbRacket = new GameObject("raket").transform; fbRacket.SetParent(model, false); fbRacket.localPosition = new Vector3(.3f, 1.2f, 0);
            Kit.Prim(PrimitiveType.Cylinder, "sap", fbRacket, new Vector3(0, .3f, 0), new Vector3(.03f, .3f, .03f), Kit.Lit(Kit.Hex("#2b2f3a")));
            Kit.Prim(PrimitiveType.Cylinder, "kafa", fbRacket, new Vector3(0, .75f, 0), new Vector3(.4f, .005f, .5f), Kit.Lit(Color.white)).transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        static float ClipTime(string clip, float ph)
        {
            bool loop; int n;
            if (LOOPS.TryGetValue(clip, out loop) && loop) return Mathf.Repeat(ph, 1);
            if (!Match.CLIP_N.TryGetValue(clip, out n) || n < 2) return Mathf.Clamp01(ph);
            return Mathf.Clamp01((ph * n - .5f) / (n - 1));
        }

        void Sample(string a, float pa, string b, float pb, float w)
        {
            if (anim == null) return;
            foreach (AnimationState st in anim) st.enabled = false;
            var A = anim[a]; if (A == null) return;
            A.enabled = true; A.weight = w; A.normalizedTime = ClipTime(a, pa);
            if (b != null && w < 1) { var B = anim[b]; if (B != null) { B.enabled = true; B.weight = 1 - w; B.normalizedTime = ClipTime(b, pb); } }
            anim.Sample();
            foreach (AnimationState st in anim) st.enabled = false;
        }

        // Hakem: sabit konum ve yön, boşta pozu; yaw derece.
        public void ShowStatic(Vector3 pos, float yawDeg, float tiltDeg)
        {
            transform.position = pos; yaw.localRotation = Quaternion.Euler(tiltDeg, yawDeg, 0) * calib;
            shadow.position = new Vector3(pos.x, .003f, pos.z);
            if (anim != null && cur != "idle") { cur = "idle"; Sample("idle", .25f, null, 0, 1); }
        }

        public void Show(Athlete o, Match M, float dt)
        {
            // Sıçrama: sin(jump·π) (tepe temasta), split step küçük hop (%20). 0,5 m ≈ HTML'deki 16 px/28 px-m ölçeği.
            float j = Match.JumpH(o) * .5f;
            transform.position = Kit.W(o.x, o.y, 0) + Vector3.up * j;
            // Gövde dik (ce47f5c): hamlede yatma yok; yalnız kıl payı kurtarışta (yat) en çok 15°. Koşu eğimi ±0,08 rad (~4,6°;
            // Ar-Ge 23 Eki B3: render edilmiş figürü düz resim gibi yatırmak sahte görünür, ayaklar yerden kalkar).
            float lean = Mathf.Clamp(o.svx * .02f, -.08f, .08f) * (isMe ? 1 : -1);
            float dive = (o.yat ? o.dive : 0) * (o.x < Court.MID ? -1 : 1) * .26f * (isMe ? 1 : -1);
            yaw.localRotation = Quaternion.Euler(0, isMe ? 0 : 180, 0) * Quaternion.Euler(0, 0, -(dive + lean) * Mathf.Rad2Deg) * calib;
            shadow.position = Kit.W(o.x, o.y, .003f); shadow.localScale = Vector3.one * (.9f - j * .8f);
            ShowRing(o.nefes, dt, M.reduce);
            if (yay != null)
            {
                yay.emitting = o.sw > .3f && !M.reduce; float a = Mathf.Clamp01(o.sw) * .5f;
                yay.colorGradient = new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, alphaKeys = new[] { new GradientAlphaKey(a, 0), new GradientAlphaKey(0, 1) } };
            }
            string clip; float ph; M.PickClip(o, isMe, out clip, out ph);
            if (anim == null) { if (fbRacket != null) fbRacket.localRotation = Quaternion.Euler(0, 0, o.sw > 0 ? -140 * (1 - o.sw) + 40 : 20 - 40 * o.prepA); return; }
            if (anim[clip] == null) clip = "idle";
            if (clip != cur)
            {
                bool takip = cur != null && Match.CLIP_HIT.ContainsKey(cur) && !Match.CLIP_HIT.ContainsKey(clip);   // vuruş → hazır duruş/ayak
                prev = cur; prevPh = lastPh; cur = clip; blend = 0; blendS = takip ? .2f : .08f;
            }
            blend = Mathf.Min(1, blend + dt / blendS); lastPh = ph;
            // Boşta duruşun genliği nefese bağlı (B4; hız Match.IdleIlerle'de): nefes %70'in altına inince yorgun klibi
            // (eller dizde) %35'e kadar karışır, beden soluklanır. Harmanlama bitmeden karıştırılmaz.
            if (cur == "idle" && blend >= 1 && o.nefes < .7f && anim["yorgun"] != null)
            { Sample("idle", ph, "yorgun", ph, 1 - .35f * Mathf.Clamp01((.7f - o.nefes) / .7f)); return; }
            Sample(cur, ph, prev, prevPh, prev == null ? 1 : blend);
        }
    }
}
