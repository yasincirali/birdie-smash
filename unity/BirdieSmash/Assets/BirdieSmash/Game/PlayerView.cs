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
                modelAdi = prefab.name; model = Instantiate(prefab, yaw).transform; model.localPosition = Vector3.zero; model.localScale = Vector3.one * SCALE;
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
        // Yön ve dikey hizalama İSKELETTEN (2026-10-26, yasin build 38: "3B olanlar ters görünüyor", "gerçekçi karakterler
        // render edilmiyor"). Unity ekran turu ölçtü: FBX kökünün doğrudan çocukları (bas__/goz__ işaretleri, servis topu
        // vekilleri) derili ağa göre X ekseninde 180° dönük geliyor; işaretten çıkarılan "ileri" bu yüzden tersti (Oyuncak'ta
        // yüz kameraya, rakipte sırt fileye bakıyordu) ve Rocketbox kökü yan yatmış, ağ zeminin 1 m altına gömülüydü.
        // Şimdi: boşta pozunda baş, iki üst kol ve iki ayak kemiğinden karakterin yukarı/sağ/ileri ekseni ölçülür, model
        // yaw'a göre dik ve +Z'ye bakar hâle döndürülür, tabanı y=0'a, ayak ortası eksene oturtulur. Kemik adları üç
        // iskeletin hepsinde bulunur: KayKit (head, upperarm.l, foot.l), Rocketbox (Bip01 Head, Bip01 L UpperArm,
        // Bip01 L Foot), sade/Mesh2Motion (head, upperarm_l, foot_l). İşaretler yalnız kemik bulunamazsa yedek.
        Transform kHead, kHip; readonly List<Transform> kThigh = new List<Transform>(), kCalf = new List<Transform>();
        static bool Sol(string n) { return n.EndsWith(".l") || n.EndsWith("_l") || n.Contains(" l ") || n.Contains("left"); }
        static bool Sag(string n) { return n.EndsWith(".r") || n.EndsWith("_r") || n.Contains(" r ") || n.Contains("right"); }
        void Calibrate()
        {
            Transform la = null, ra = null; var feet = new List<Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant(); if (n.Contains("__")) continue;   // ağ/parça nesneleri kemik değil
                if (kHead == null && (n == "head" || n.EndsWith(" head"))) kHead = t;
                if (kHip == null && (n == "hips" || n == "pelvis" || n.EndsWith(" pelvis"))) kHip = t;
                if (n.Contains("upperarm")) { if (Sol(n)) la = t; else if (Sag(n)) ra = t; }
                if (n.Contains("foot") && !n.Contains("step") && !n.Contains("ik")) feet.Add(t);
                if (n.Contains("thigh") || n.Contains("upperleg")) kThigh.Add(t);
                if (n.Contains("calf") || n.Contains("lowerleg")) kCalf.Add(t);
            }
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen = true;   // sınır gerçek pozdan (kırpılma yok)
            if (kHead == null || la == null || ra == null || feet.Count == 0) { CalibrateIsaret(); return; }
            Vector3 fm = Vector3.zero; foreach (var f in feet) fm += yaw.InverseTransformPoint(f.position); fm /= feet.Count;
            Vector3 up = yaw.InverseTransformPoint(kHead.position) - fm, right = yaw.InverseTransformPoint(ra.position) - yaw.InverseTransformPoint(la.position);
            Vector3 fwd = Vector3.Cross(right, up); if (fwd.sqrMagnitude < 1e-8f || up.sqrMagnitude < 1e-8f) { CalibrateIsaret(); return; }
            var R0 = Quaternion.LookRotation(fwd, up);
            model.localRotation = Quaternion.Inverse(R0) * model.localRotation;
            model.localPosition = Vector3.zero; Zemin(); calib = Quaternion.identity;
            VekilDuzelt(true);
        }
        // Tabanı y=0'a, ayak ortasını yaw eksenine oturt (dönüşten sonra). Derili ağın sınırı (bounds) bir sonraki çizime
        // dek eski pozda kalır; bu yüzden kemik konumlarından: ayak/parmak kemiklerinin en alçağı − taban payı (3,5 cm).
        void Zemin()
        {
            float minY = float.MaxValue; Vector3 fm = Vector3.zero; int k = 0;
            foreach (var t in model.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant(); if (n.Contains("__") || n.Contains("step") || n.Contains("ik")) continue;
                bool ayak = n.Contains("foot"), uc = n.Contains("toe") || n.Contains("ball");
                if (!ayak && !uc) continue;
                var p = yaw.InverseTransformPoint(t.position); minY = Mathf.Min(minY, p.y); if (ayak) { fm += p; k++; }
            }
            if (minY == float.MaxValue) return; if (k > 0) fm /= k;
            model.localPosition -= new Vector3(fm.x, minY - .035f, fm.z);
        }
        // Kökün doğrudan çocukları (işaret, servis topu vekili) ağa göre dönük gelir: bas__isaret'i baş kemiğine en iyi
        // oturtan eksen dönüşü (24 aday) seçilir ve her örneklemeden sonra bu parçalara uygulanır (animasyon her karede
        // yerel değeri yeniden yazar; yazmadığı karede ikinci kez uygulanmasın diye son yazılan değer tutulur).
        Quaternion vekilF = Quaternion.identity; bool vekilVar; readonly List<Transform> vekil = new List<Transform>();
        readonly Dictionary<Transform, Pose> vekilSon = new Dictionary<Transform, Pose>();
        void VekilDuzelt(bool kur)
        {
            if (kur)
            {
                Transform bas = null;
                foreach (Transform c in model) { if (c.GetComponent<MeshRenderer>() == null) continue; if (c.name.StartsWith("bas__")) bas = c; vekil.Add(c); }
                if (bas == null || kHead == null) return;
                Vector3 hedef = model.InverseTransformPoint(kHead.position), m = bas.localPosition; float best = float.MaxValue;
                foreach (var e in new[] { Vector3.zero, new Vector3(180, 0, 0), new Vector3(0, 180, 0), new Vector3(0, 0, 180), new Vector3(90, 0, 0), new Vector3(-90, 0, 0), new Vector3(0, 0, 90), new Vector3(0, 0, -90), new Vector3(90, 180, 0), new Vector3(-90, 180, 0), new Vector3(0, 90, 0), new Vector3(0, -90, 0) })
                { var q = Quaternion.Euler(e); float d = (q * m - hedef).sqrMagnitude; if (d < best) { best = d; vekilF = q; } }
                vekilVar = vekilF != Quaternion.identity;
            }
            if (!vekilVar) return;
            foreach (var t in vekil)
            {
                Pose son; if (vekilSon.TryGetValue(t, out son) && son.position == t.localPosition && son.rotation == t.localRotation) continue;
                t.localPosition = vekilF * t.localPosition; t.localRotation = vekilF * t.localRotation; vekilSon[t] = new Pose(t.localPosition, t.localRotation);
            }
        }
        // Yedek (eski yol): işaretlerden "ileri".
        void CalibrateIsaret()
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
        }

        public static string Etiket(string n)
        {
            int i = n.IndexOf("__"); string tag = i >= 0 ? n.Substring(i + 2) : "";
            int dot = tag.IndexOfAny(new[] { '.', ' ' }); if (dot >= 0) tag = tag.Substring(0, dot); return tag;
        }
        // Parça kanalları (HTML turParlat): 0 forma, 1 raket (cerceve/grip/tel/tel2), 2 bant, 3 bileklik.
        public readonly List<Renderer>[] kanal = { new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
        public Transform Model { get { return model; } }
        string modelAdi;
        // Gömülü doku Unity'de malzemeye bağlanmıyor (ekran turu rapor.txt: Rocketbox ten malzemesi siyah, doku yok): gövde ve
        // baş renk dokusu FBX'ten çıkarılıp Resources'a kondu (<model>_govde / <model>_bas.jpg), malzeme adına göre bağlanır.
        Material DokuMalzeme(Material asil)
        {
            if (asil == null) return null; string n = asil.name.ToLowerInvariant(); string ek = n.Contains("body") ? "_govde" : n.Contains("head") ? "_bas" : null;
            if (ek == null) return null; var tx = Resources.Load<Texture2D>("karakter/" + modelAdi + ek); if (tx == null) return null;
            Material m; if (!dokuM.TryGetValue(modelAdi + ek, out m)) dokuM[modelAdi + ek] = m = Kit.LitTex(Color.white, tx, .18f); return m;
        }
        static readonly Dictionary<string, Material> dokuM = new Dictionary<string, Material>();
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
                if (m == null)
                {
                    var asl = r.sharedMaterials; bool degisti = false;
                    for (int k = 0; k < asl.Length; k++) { var dm = DokuMalzeme(asl[k]); if (dm != null) { asl[k] = dm; degisti = true; } }
                    if (degisti) { r.sharedMaterials = asl; continue; }
                    if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name.Contains("Error")) m = Kit.Lit(Color.gray); else continue;
                }
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
            VekilDuzelt(false);
        }

        // Hakem: oturur (BWF: baş hakem file hizasında yüksek sandalyede, servis hakemi karşıda normal sandalyede; yasin build
        // 38 "hakemler yok": eskiden ayaklığın üstünde ayakta, sırtı korta dönüktü).
        // 2026-10-26 (yasin: "hakemler fizik kurallarına uygun şekilde sandalyelerinde oturmalı"): eski sürüm boşta pozuna
        // sabit ±88° ekliyordu; boşta pozu badminton hazır duruşu (dizler kırık, gövde öne eğik) olduğundan dizler havada,
        // ayaklar ayaklığın üstünde boşlukta kalıyordu. Şimdi oturuş eklem hedeflerinden kurulur, her iskelette aynı:
        //  1) gövde dikleştirilir (kalça→baş ekseni düşey, 6° geriye: sırtlığa yaslanır),
        //  2) kalça eklemleri oturak yüzünün uyluk kalınlığı kadar üstüne, sırtlığın önüne (oturak merkezinin 0,1 m gerisi),
        //  3) iki kemik IK ile ayak bileği destek yüzüne (ayaklık ya da zemin + bilek yüksekliği), diz öne-yukarı,
        //  4) eller uylukların üstüne (dizden 1/3 geride), dirsek dışa-geriye.
        // ayakY: ayakların bastığı yüzeyin dünya yüksekliği; ayakIleri: ayakların oturak merkezinden ilerideki mesafesi.
        Transform[] kBacak, kKol;   // [sol uyluk, sol baldır, sol ayak, sağ uyluk, sağ baldır, sağ ayak]; kol: üst, ön, el
        static Transform Cocuk(Transform t, System.Func<string, bool> ok)
        {
            foreach (Transform c in t) { var n = c.name.ToLowerInvariant(); if (!n.Contains("__") && ok(n)) return c; }
            foreach (Transform c in t) { var r = Cocuk(c, ok); if (r != null) return r; }
            return null;
        }
        void ZincirKur()
        {
            kBacak = new Transform[6]; kKol = new Transform[6];
            foreach (var t in kThigh) { var n = t.name.ToLowerInvariant(); int i = Sol(n) ? 0 : Sag(n) ? 3 : -1; if (i < 0 || kBacak[i] != null) continue;
                kBacak[i] = t; kBacak[i + 1] = Cocuk(t, m => m.Contains("calf") || m.Contains("lowerleg"));
                if (kBacak[i + 1] != null) kBacak[i + 2] = Cocuk(kBacak[i + 1], m => m.Contains("foot") && !m.Contains("ik") && !m.Contains("roll")); }
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant(); if (n.Contains("__") || !n.Contains("upperarm")) continue; int i = Sol(n) ? 0 : Sag(n) ? 3 : -1; if (i < 0 || kKol[i] != null) continue;
                kKol[i] = t; kKol[i + 1] = Cocuk(t, m => m.Contains("forearm") || m.Contains("lowerarm"));
                if (kKol[i + 1] != null) kKol[i + 2] = Cocuk(kKol[i + 1], m => (m.Contains("hand") || m.Contains("wrist")) && !m.Contains("ik") && !m.Contains("slot"));
            }
        }
        // İki kemikli IK: a (omuz/kalça) → b (dirsek/diz) → c (el/bilek) zinciri c hedefe, b kutup yönüne bükülür.
        static void IK(Transform a, Transform b, Transform c, Vector3 hedef, Vector3 kutup)
        {
            if (a == null || b == null || c == null) return;
            float la = (b.position - a.position).magnitude, lb = (c.position - b.position).magnitude; Vector3 d = hedef - a.position;
            float L = Mathf.Clamp(d.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-3f); Vector3 u = d.normalized;
            float ca = Mathf.Clamp((la * la + L * L - lb * lb) / (2 * la * L), -1, 1);
            Vector3 p = Vector3.ProjectOnPlane(kutup, u).normalized; if (p.sqrMagnitude < 1e-6f) p = Vector3.ProjectOnPlane(Vector3.up, u).normalized;
            Vector3 diz = a.position + u * (la * ca) + p * (la * Mathf.Sqrt(1 - ca * ca));
            a.rotation = Quaternion.FromToRotation(b.position - a.position, diz - a.position) * a.rotation;
            b.rotation = Quaternion.FromToRotation(c.position - b.position, a.position + u * L - b.position) * b.rotation;
        }
        public void ShowSit(Vector3 oturak, float yawDeg, float tiltDeg, float ayakY = 0, float ayakIleri = .4f)
        {
            yaw.localRotation = Quaternion.Euler(0, yawDeg, 0) * calib;
            transform.position = Vector3.zero;
            if (anim != null) Sample("idle", .25f, null, 0, 1); cur = "idle";
            if (kHip == null || kThigh.Count == 0) { transform.position = oturak; shadow.position = new Vector3(oturak.x, .003f, oturak.z); return; }
            if (kBacak == null) ZincirKur();
            Vector3 ileri = yaw.forward, sag = yaw.right;
            // Bilek yüksekliği ve boy, ayakta boşta pozundan (taban y=0'da).
            float bilek = kBacak[2] != null ? Mathf.Max(.04f, kBacak[2].position.y) : .08f, boy = kHead != null ? kHead.position.y : 1.6f, olc = boy / 1.6f;
            // 1) gövde dik, hafif geri
            if (kHead != null)
            {
                Vector3 dik = Quaternion.AngleAxis(-6, sag) * Vector3.up;
                kHip.rotation = Quaternion.FromToRotation(kHead.position - kHip.position, dik) * kHip.rotation;
            }
            if (kHead != null && tiltDeg != 0) kHead.rotation = Quaternion.AngleAxis(tiltDeg, sag) * kHead.rotation;
            // 2) kalça eklemlerinin ortası oturağa (uyluk kalınlığı ~0,08 m × ölçek)
            Vector3 kalca = kBacak[0] != null && kBacak[3] != null ? (kBacak[0].position + kBacak[3].position) / 2 : kHip.position;
            Vector3 hedef = oturak + Vector3.up * (.085f * olc) - ileri * .1f;
            transform.position = hedef - kalca;
            // 3) bacaklar: bilek destek yüzüne, dizler öne-yukarı; ayaklar kalça genişliğinde, hafif açık
            for (int s = 0; s < 2; s++)
            {
                var th = kBacak[s * 3]; if (th == null) continue; float yon = s == 0 ? -1 : 1;
                Vector3 bas = oturak + ileri * ayakIleri + sag * (yon * .13f * olc); bas.y = ayakY + bilek;
                IK(th, kBacak[s * 3 + 1], kBacak[s * 3 + 2], bas, ileri + Vector3.up * .6f);
            }
            // 4) eller uylukların üstünde
            for (int s = 0; s < 2; s++)
            {
                var ua = kKol[s * 3]; var th = kBacak[s * 3]; var kn = kBacak[s * 3 + 1]; if (ua == null || th == null || kn == null) continue; float yon = s == 0 ? -1 : 1;
                Vector3 el = Vector3.Lerp(th.position, kn.position, .62f) + Vector3.up * (.09f * olc) + sag * (yon * .02f);
                IK(ua, kKol[s * 3 + 1], kKol[s * 3 + 2], el, sag * yon * .5f - ileri);
            }
            shadow.position = new Vector3(transform.position.x, .003f, transform.position.z);
        }
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
