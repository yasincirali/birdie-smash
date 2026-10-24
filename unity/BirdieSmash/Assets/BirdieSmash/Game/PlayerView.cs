// Oyuncu: Blender figürünün FBX'i (tool/karakter/unity_fbx.py → Resources/karakter/<model>.fbx). İki model, HTML'deki
// Ayarlar › Oyuncu görünümü ile aynı: "oyuncak" (Oyuncak 3B, KayKit Rogue CC0, varsayılan) ve "rocketbox" (Gerçekçi,
// Microsoft Rocketbox Sports_Male_04, MIT). İkisi de iskeletli; 26 klip Legacy Animation olarak normalize zamanla
// örneklenir: klip ve faz Match.PickClip'ten gelir (HTML player() ile aynı karar), vuruş anı topla kare kare eşleşir.
// Faz → klip zamanı: sprite kare k'yı (k+.5)/n fazında gösterir; FBX'te pozlar eşit aralıklı olduğu için aynı faz aynı
// pozu verir (döngülü kliplerde kapanış pozu ek). Klip değişiminde 0,08 s harmanlama. FBX yoksa basit kapsül figür.
// Renk: parça adının "__" sonrası etiketi: forma = takım rengi; bant/bilek ekipmanı demo'da yok (HTML varsayılanı
// "Yok") → gizli; diğer parçalar FBX'in kendi malzemesini korur (Rocketbox dokusu, KayKit renkleri).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class PlayerView : MonoBehaviour
    {
        public bool isMe; Transform model, yaw; Animation anim; string cur, prev; float blend = 1, prevPh, lastPh;
        Transform fbRacket, shadow; LineRenderer ringBg, ringFg; float ringA;
        const float SCALE = 1.75f;   // sprite sürümündeki 1,75 kat okunurluk büyütmesi
        static readonly Dictionary<string, bool> LOOPS = new Dictionary<string, bool>();
        public bool hakem;

        [System.Serializable] class ClipMeta { public string name; public int poses; public bool loop; }
        [System.Serializable] class Meta { public ClipMeta[] clips; }

        public static PlayerView Build(Transform root, string model, bool isMe, Color forma, bool hakem = false)
        {
            var g = new GameObject(hakem ? "hakem" : isMe ? "ben" : "rakip"); g.transform.SetParent(root, false);
            var pv = g.AddComponent<PlayerView>(); pv.isMe = isMe; pv.hakem = hakem; pv.Init(model, forma); return pv;
        }

        void Init(string modelAd, Color forma)
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
                Paint(forma);
                if (anim != null && anim["idle"] != null) Sample("idle", 0, null, 0, 1);
                Calibrate();
            }
            else Fallback(forma);
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

        void Paint(Color forma)
        {
            var formaM = Kit.Lit(forma, .25f); var cerceve = Kit.Lit(Kit.Hex("#e9eef0"), .6f);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name; int i = n.IndexOf("__"); string tag = i >= 0 ? n.Substring(i + 2) : "";
                int dot = tag.IndexOfAny(new[] { '.', ' ' }); if (dot >= 0) tag = tag.Substring(0, dot);
                // hakemde raket ve top (pozla kurulan rijit parçalar = deri ağı olmayan MeshRenderer'lar) gizli
                if (tag == "isaret" || tag == "bant" || tag == "bilek" || (hakem && !(r is SkinnedMeshRenderer))) { r.enabled = false; continue; }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                Material m = tag == "forma" ? formaM : tag == "cerceve" ? cerceve : null;
                if (m == null) { if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name.Contains("Error")) m = Kit.Lit(Color.gray); else continue; }
                var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = m; if (arr.Length == 0) arr = new[] { m }; r.sharedMaterials = arr;
            }
            if (hakem) { var g = Kit.Lit(Kit.Hex("#0a1530"), .2f); foreach (var r in model.GetComponentsInChildren<Renderer>()) if (r.gameObject.name.Contains("__sort")) r.sharedMaterial = g; }
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
            // Gövde dik (ce47f5c): chassé'de en çok 6°, hamlede yatma yok; yalnız kıl payı kurtarışta (yat) en çok 15°.
            float lean = Mathf.Clamp(o.svx * .02f, -.1f, .1f) * (isMe ? 1 : -1);
            float dive = (o.yat ? o.dive : 0) * (o.x < Court.MID ? -1 : 1) * .26f * (isMe ? 1 : -1);
            yaw.localRotation = Quaternion.Euler(0, isMe ? 0 : 180, 0) * Quaternion.Euler(0, 0, -(dive + lean) * Mathf.Rad2Deg) * calib;
            shadow.position = Kit.W(o.x, o.y, .003f); shadow.localScale = Vector3.one * (.9f - j * .8f);
            ShowRing(o.nefes, dt, M.reduce);
            string clip; float ph; M.PickClip(o, isMe, out clip, out ph);
            if (anim == null) { if (fbRacket != null) fbRacket.localRotation = Quaternion.Euler(0, 0, o.sw > 0 ? -140 * (1 - o.sw) + 40 : 20 - 40 * o.prepA); return; }
            if (anim[clip] == null) clip = "idle";
            if (clip != cur) { prev = cur; prevPh = lastPh; cur = clip; blend = 0; }
            blend = Mathf.Min(1, blend + dt / .08f); lastPh = ph;
            Sample(cur, ph, prev, prevPh, prev == null ? 1 : blend);
        }
    }
}
