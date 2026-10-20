// Oyuncu: Blender'da kurulan parçalı figür (tool/karakter/unity_fbx.py → Resources/karakter/oyuncu.fbx),
// 22 klip Legacy Animation olarak normalize zamanla örneklenir: klip ve faz Match.PickClip'ten gelir
// (sprite sürümüyle aynı karar), böylece vuruş anı topla kare kare eşleşir. Klip değişiminde 0,08 s
// harmanlama. FBX yoksa basit kapsül figür (proje yine açılır ve oynanır).
// Renk: parça adının "__" sonrası etiketi (forma, bant, bilek, cerceve…) → takım rengi.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class PlayerView : MonoBehaviour
    {
        public bool isMe; Transform model, yaw; Animation anim; string cur, prev; float blend = 1, prevPh;
        Transform fbRacket, shadow; const float SCALE = 1.75f;   // sprite sürümündeki 1,75 kat okunurluk büyütmesi

        public static PlayerView Build(Transform root, bool isMe, Color forma, Color bant, Color bilek)
        {
            var g = new GameObject(isMe ? "ben" : "rakip"); g.transform.SetParent(root, false);
            var pv = g.AddComponent<PlayerView>(); pv.isMe = isMe; pv.Init(forma, bant, bilek); return pv;
        }

        void Init(Color forma, Color bant, Color bilek)
        {
            yaw = new GameObject("yon").transform; yaw.SetParent(transform, false);
            var prefab = Resources.Load<GameObject>("karakter/oyuncu");
            if (prefab != null)
            {
                model = Instantiate(prefab, yaw).transform; model.localPosition = Vector3.zero; model.localScale = Vector3.one * SCALE;
                anim = model.GetComponentInChildren<Animation>();
                Paint(forma, bant, bilek);
                if (anim != null && anim["idle"] != null) Sample("idle", 0, null, 0, 1);
                Calibrate();
            }
            else Fallback(forma);
            shadow = Kit.Prim(PrimitiveType.Quad, "golge", transform, new Vector3(0, .003f, 0), new Vector3(.9f, .9f, 1), Kit.Clear(new Color(0, 0, 0, .35f), Kit.RadialTex())).transform;
            shadow.localRotation = Quaternion.Euler(90, 0, 0);
            yaw.localRotation = Quaternion.Euler(0, isMe ? 0 : 180, 0) * calib;
        }

        Quaternion calib = Quaternion.identity;
        // FBX eksen dönüşümüne güvenme: "ileri"yi iskeletten çıkar (gözler − baş), +Z'ye çevir.
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
            if (f.sqrMagnitude < 1e-6f) return;
            calib = Quaternion.FromToRotation(f.normalized, Vector3.forward);
            if (head.position.y - model.position.y < .3f) Debug.LogWarning("Birdie Smash: karakterin başı beklenenden alçak, FBX eksenlerini kontrol et");
        }

        void Paint(Color forma, Color bant, Color bilek)
        {
            var cache = new Dictionary<string, Material>();
            var cols = new Dictionary<string, Color> {
                {"forma",forma},{"bant",bant},{"bilek",bilek},{"ten",Kit.Hex("#eeb890")},{"sort",Kit.Hex("#1f2433")},{"sac",Kit.Hex("#3b2418")},
                {"beyaz",Kit.Hex("#f4f4f0")},{"cerceve",Kit.Hex("#2b2f3a")},{"tel",new Color(.95f,.95f,.9f)},{"tuy",Kit.Hex("#fbfbf6")},{"mantar",Kit.Hex("#f2b64c")}};
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                string n = r.gameObject.name; int i = n.IndexOf("__"); string tag = i >= 0 ? n.Substring(i + 2) : "ten";
                int dot = tag.IndexOf('.'); if (dot >= 0) tag = tag.Substring(0, dot);
                Material m; if (!cache.TryGetValue(tag, out m)) { Color c; if (!cols.TryGetValue(tag, out c)) c = Color.gray; m = Kit.Lit(c, tag == "cerceve" ? .6f : .2f); cache[tag] = m; }
                var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = m; if (arr.Length == 0) arr = new[] { m }; r.sharedMaterials = arr;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        void Fallback(Color forma)
        {
            model = new GameObject("yedek").transform; model.SetParent(yaw, false);
            Kit.Prim(PrimitiveType.Capsule, "govde", model, new Vector3(0, .8f, 0), new Vector3(.45f, .55f, .35f), Kit.Lit(forma));
            Kit.Prim(PrimitiveType.Sphere, "bas", model, new Vector3(0, 1.55f, 0), Vector3.one * .38f, Kit.Lit(Kit.Hex("#eeb890")));
            fbRacket = new GameObject("raket").transform; fbRacket.SetParent(model, false); fbRacket.localPosition = new Vector3(.3f, 1.2f, 0);
            Kit.Prim(PrimitiveType.Cylinder, "sap", fbRacket, new Vector3(0, .3f, 0), new Vector3(.03f, .3f, .03f), Kit.Lit(Kit.Hex("#2b2f3a")));
            Kit.Prim(PrimitiveType.Cylinder, "kafa", fbRacket, new Vector3(0, .75f, 0), new Vector3(.4f, .005f, .5f), Kit.Lit(Color.white)).transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        void Sample(string a, float pa, string b, float pb, float w)
        {
            if (anim == null) return;
            foreach (AnimationState st in anim) st.enabled = false;
            var A = anim[a]; if (A == null) return;
            A.enabled = true; A.weight = w; A.normalizedTime = Mathf.Clamp01(pa);
            if (b != null && w < 1) { var B = anim[b]; if (B != null) { B.enabled = true; B.weight = 1 - w; B.normalizedTime = Mathf.Clamp01(pb); } }
            anim.Sample();
            foreach (AnimationState st in anim) st.enabled = false;
        }

        public void Show(Athlete o, float clock, float dt)
        {
            float j = Mathf.Sin(Mathf.Min(1, o.jump) * Mathf.PI) * .35f;
            transform.position = Kit.W(o.x, o.y, 0) + Vector3.up * j;
            float lean = Mathf.Clamp(o.vx * .05f, -.3f, .3f) * (isMe ? 1 : -1);
            float dive = o.dive * (o.x < Court.MID ? -1 : 1) * .9f * (isMe ? 1 : -1);
            yaw.localRotation = Quaternion.Euler(0, isMe ? 0 : 180, 0) * Quaternion.Euler(0, 0, -(dive + lean) * Mathf.Rad2Deg * .6f) * calib;
            shadow.position = Kit.W(o.x, o.y, .003f); shadow.localScale = Vector3.one * (.9f - j);
            string clip; float ph; Match.PickClip(o, isMe, clock, out clip, out ph);
            if (anim == null) { if (fbRacket != null) fbRacket.localRotation = Quaternion.Euler(0, 0, o.sw > 0 ? -140 * (1 - o.sw) + 40 : 20 - 40 * o.prepA); return; }
            if (anim[clip] == null) clip = "idle";
            if (clip != cur) { prev = cur; prevPh = lastPh; cur = clip; blend = 0; }
            blend = Mathf.Min(1, blend + dt / .08f); lastPh = ph;
            Sample(cur, ph, prev, prevPh, prev == null ? 1 : blend);
        }
        float lastPh;
    }
}
