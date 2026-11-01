// Oyuncu: Blender figürünün FBX'i (tool/karakter/unity_fbx.py → Resources/karakter/<model>.fbx). Ayarlar › Oyuncu görünümü:
// "oyuncak" (Oyuncak 3B, varsayılan; 2026-10-26'dan beri MakeHuman CC0 tabanlı figür, 2026-10-27'den beri ~3,4 baş boylu tıknaz
// oyuncak oranı ve dört silüeti ayrı tip; eskiden KayKit Rogue CC0),
// "rocketbox" (Gerçekçi, Microsoft Rocketbox Sports_Male_04, MIT), 2026-10-27'den beri "sade" (Sade, Mesh2Motion CC0) ve
// 2026-10-29'dan beri "tuycuk" (Tüycük: Oyuncak atlet iskeleti ve klipleri, kendi yumurta gövdeli ağı; tool/karakter/tuycuk.py;
// gövde __forma takım rengi, __karin onun açığı, ayakkabılar Oyuncak'ın ayk_* parçaları). Hepsi iskeletli; 26 klip Legacy Animation olarak normalize zamanla
// örneklenir: klip ve faz Match.PickClip'ten gelir (HTML player() ile aynı karar), vuruş anı topla kare kare eşleşir.
// Faz → klip zamanı: sprite kare k'yı (k+.5)/n fazında gösterir; FBX'te pozlar eşit aralıklı olduğu için aynı faz aynı
// pozu verir (döngülü kliplerde kapanış pozu ek). Klip değişiminde 0,08 s harmanlama; vuruş klibinden boşta/ayak
// klibine dönüşte 0,2 s (Ar-Ge 23 Eki animasyon B1: takip pozundan hazır duruşa "ışınlanma" yok; sprite'ta harmanlama
// hayalet yapıyordu, iskelette yapmaz, bu yüzden Blender'da yeniden render gerekmedi). FBX yoksa basit kapsül figür.
// Renk: parça adının "__" sonrası etiketi (HTML sprRenk/tint maskesi karşılığı): forma = takım rengi, cerceve = raket
// modelinin rengi, bant/bilek = seçili kafa bandı/bileklik (ekipman "Yok" ise gizli), grip/tel/tel2 = raket görünümü
// (tuytop_raket_gor; tel2 yalnız "ana ve çapraz ayrı" desende farklı). Rakipte raket #e9eef0, bant/bilek yok.
// Diğer parçalar FBX'in kendi malzemesini korur (Rocketbox dokusu, KayKit renkleri).
// 2026-10-26 karakter görünümü (adım 6): ten (__ten), saç (__sac) ve ayakkabı (__taban) yalnız oyuncu seçtiyse boyanır; null
// = modelin kendi rengi (var olan oyuncu ve rakipler değişmez). Dokulu parça (Gerçekçi model teni: DokuMalzeme) boyanmaz:
// dokunun üstüne düz renk yüzü siler; ten tonu orada bilinçli olarak yok (Soyunma odası bunu yazar). Etiketi olmayan modelde
// (yeni karakter hattında etiket değişirse) hiçbir şey boyanmaz, çökme yok. Grip "cizgi" dokusu (Havlu) koddan çizilir.
// 2026-10-26 karakter tasarımı (tip, saç biçimi, ayakkabı modeli; Core/Kozmetik, varlık sözleşmesi
// /mnt/project-files/birdie-smash/komple-oyun/karakter_varlik_sozlesmesi.md): yalnız "Karakter varyantları" bölümündeki
// yöntemler (ModelYolu, Varyant, AyakkabiBoya, ShowPoz) ve Gorunum alanları; hareket/ayak kodu bu bölüme girmez.
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public class PlayerView : MonoBehaviour
    {
        public bool isMe; Transform model, yaw; Animation anim; string cur, prev, prev2; float blend = 1, blendS = .08f, prevPh, lastPh, prev2Ph, prev2Pay;
        Transform fbRacket, shadow; LineRenderer ringBg, ringFg; float ringA;
        const float SCALE = 1.75f;   // sprite sürümündeki 1,75 kat okunurluk büyütmesi
        static readonly Dictionary<string, bool> LOOPS = new Dictionary<string, bool>();
        public bool hakem;

        // adim (2026-10-26): ayak işi klibinin bir döngüde taşıdığı yol, modelin kendi ölçeğinde metre (basan ayaktan ölçülür).
        // Varsa Match.ADIM_BOY'u ezer (oyun ölçeği = adim × SCALE); yoksa 0 gelir ve tahmin değerler kalır.
        [System.Serializable] class ClipMeta { public string name; public int poses; public bool loop; public float adim; }
        // 2026-10-26 karakter varlık sözleşmesi §5: tip (varsayılan saç/ayakkabı, saç rengi) ve saç başına bant uyumu.
        [System.Serializable] class TipMeta { public string id, sac, ayakkabi, sacRenk; public float boy; }
        [System.Serializable] class SacMeta { public string id; public bool bant = true; }
        // bant: kafa bandı halkası, bas__isaret'e göre model biriminde (dz yukarı, df ileri, r yarıçap; unity_fbx.py Blender'da ölçer).
        // yan/ust/on/arka: baş elipsoidi, r'ye oran (bant yüksekliğinde yan, ön, arka; bant merkezinden tepe). 0 = küre (1).
        // Oyuncak başı yumurta: küre çerçevede (r66) gözlük/boya/bıyık başın içinde kalıyordu. Yalnız kafatası (ön 1,25r, arka
        // 1,03r, yan ,83r, tepe ,74r; r67) şapkayı saçın içine gömdü: şapka saçın üstüne oturur, oranlar baş + kısa saçtan
        // (yan ,92r, tepe 1,11r, arka 1,13r) küçük payla; ön yüzden (saçsız). Gerçekçi/Sade/Tüycük küreyle oturuyor, alan yok.
        [System.Serializable] public class BantMeta { public float dz, df, r, yan, ust, on, arka; }
        [System.Serializable] class Meta { public ClipMeta[] clips; public TipMeta tip; public SacMeta[] sac; public BantMeta bant; }

        // Görünüm: null alan = FBX'in kendi rengi (cerceve/grip/tel) ya da gizli (bant/bilek).
        public class Gorunum
        {
            public Color forma; public Color? cerceve, bant, bilek, grip, tel, tel2, ten, sac, ayakkabi, sort; public bool gripCizgi;
            // 2026-10-27 Gerçekçi aksesuarları: sort = şort rengi (null = modelin varsayılanı), kumas = forma kumaşı (Gear "kumas";
            // dokulu modelde formanın deseni ve parlaklığı, null = klasik).
            public string kumas;
            // 2026-10-26: tip (Kozmetik.TIP), saç biçimi (Kozmetik.SAC) ve ayakkabı modeli (Gear "ayakkabi") kimlikleri; null =
            // varsayılan (listenin ilki: hakemler ve eski çağrılar bugünkü görünüşle kalır).
            public string tip, sacModel, aykModel;
            // 2026-10-30 Soyunma › Görünüm: şapka ve yüz aksesuarı kimlikleri (Kozmetik.SAPKA / YUZ; null ya da "yok" = takılı değil).
            public string sapka, yuz;
            // 2026-10-30 Dolap › Raket: kafa şekli (RaketSekil; null/"oval" = FBX'in kendi kafası) ve şaft rengi (null = FBX rengi).
            public string kafa; public Color? saft;
            // Sahnedeki figürü yeniden kurmak gerekip gerekmediğinin özeti (GameRoot, Vitrin).
            public string Anahtar() { return "" + forma + cerceve + bant + bilek + grip + tel + tel2 + ten + sac + ayakkabi + sort + gripCizgi + "|" + tip + "|" + sacModel + "|" + aykModel + "|" + kumas + "|" + sapka + "|" + yuz + "|" + kafa + saft; }
        }
        static Color? Renk(string h) { return h != null ? Kit.Hex(h) : (Color?)null; }
        public static Gorunum Benim()
        {
            var gs = Ayar.GS; var r = Ayar.RGOR; string kp = Ayar.GorunumRenk("kaplama");
            return new Gorunum { forma = Kit.Hex(gs.forma), cerceve = Kit.Hex(kp ?? gs.raketRenk), bant = gs.bant != null ? Kit.Hex(gs.bant) : (Color?)null, bilek = gs.bileklik != null ? Kit.Hex(gs.bileklik) : (Color?)null,
                grip = Kit.Hex(Vitrin.RAK_RENK[r["grip"]]), tel = Kit.Hex(Vitrin.RAK_RENK[r["tel"]]), tel2 = Kit.Hex(Vitrin.RAK_RENK[r["desen"] == "iki" ? r["tel2"] : r["tel"]]),
                ten = Renk(Ayar.GorunumRenk("ten")), sac = Renk(Ayar.GorunumRenk("sac")), ayakkabi = Renk(Ayar.GorunumRenk("ayakkabi")), gripCizgi = gs.gripDoku == "cizgi",
                tip = Ayar.KOZ[Kozmetik.TIP], sacModel = Ayar.KOZ[Kozmetik.SAC], aykModel = Ayar.AykGorunum, sapka = Ayar.KOZ[Kozmetik.SAPKA], yuz = Ayar.KOZ[Kozmetik.YUZ],
                sort = Renk(Ayar.GorunumRenk("sort")), kumas = Ayar.gear != null ? Ayar.gear.kumas : null,
                kafa = r["kafa"], saft = r["saft"] != "model" && Vitrin.RAK_RENK.ContainsKey(r["saft"]) ? Kit.Hex(Vitrin.RAK_RENK[r["saft"]]) : (Color?)null };
        }
        // Rakip (2026-10-26): tip, saç biçimi/rengi, ten ve ayakkabı stil.json'daki gorunum'dan (rakip kimliği: Blaze hep dikenli
        // kızıl saçlı). Forma ve raket bugünkü gibi; veri yoksa bugünkü görünüş.
        public static Gorunum Rakip(Style st, Color forma)
        {
            var g = new Gorunum { forma = forma, cerceve = Kit.Hex("#e9eef0") }; var r = Kozmetik.Rakip(st != null ? st.id : null); if (r == null) return g;
            g.tip = r.tip; g.sacModel = r.sac; g.aykModel = r.ayakkabi;
            var o = r.sacRenk != null ? Karakter.Oge("sac", r.sacRenk) : null; if (o != null) g.sac = Renk(o.renk);
            o = r.ten != null ? Karakter.Oge("ten", r.ten) : null; if (o != null) g.ten = Renk(o.renk);
            o = r.aykRenk != null ? Karakter.Oge("ayakkabi", r.aykRenk) : null; if (o != null) g.ayakkabi = Renk(o.renk);
            return g;
        }
        // Uzak raketin kordajı (2026-10-30, yasin: "rakibin bazen raketi içi beyaz dolgu şeklinde gözüküyor"): maç kamerasından
        // rakip ~10 m ötede; 22 + 21 tel arası (~6 mm) ekranda bir pikselden dar, çoklu örnekleme telleri tek bir beyaz yüzeye
        // ortalıyor, raket dolu disk gibi görünüyordu (gölgesi de dolu). Gerçek yayında uzak kordaj ince bir pus: teller saydam
        // (alfa .3, ışıksız) ve gölge düşürmez. Yalnız maçtaki rakipte; yakın plan (vitrin, afiş) ve senin raketin aynı kalır.
        public void KordajSeyrek()
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string tag = Etiket(r.gameObject.name); if (tag != "tel" && tag != "tel2") continue;
                var c = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color") ? r.sharedMaterial.color : r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                r.sharedMaterial = Kit.Clear(new Color(c.r, c.g, c.b, .3f)); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        public static PlayerView Build(Transform root, string model, bool isMe, Color forma, bool hakem = false, Gorunum gor = null)
        {
            var g = new GameObject(hakem ? "hakem" : isMe ? "ben" : "rakip"); g.transform.SetParent(root, false);
            var pv = g.AddComponent<PlayerView>(); pv.isMe = isMe; pv.hakem = hakem; pv.Init(model, gor ?? new Gorunum { forma = forma, cerceve = Kit.Hex("#e9eef0") }); return pv;
        }

        // 2026-10-31 (Unity incelemesi D9): UnityEngine.Object'te `??` Unity'nin null karşılaştırmasını atlar; Resources.Load
        // bugün gerçek null döndürdüğü için çalışıyordu, ama yok edilmiş nesnede sessizce yanlış dala gider. Açık kontrol.
        static T IlkBulunan<T>(params string[] yollar) where T : Object
        {
            foreach (var y in yollar) { var o = Resources.Load<T>(y); if (o != null) return o; }
            return null;
        }
        void Init(string modelAd, Gorunum gor)
        {
            yaw = new GameObject("yon").transform; yaw.SetParent(transform, false);
            string yol = ModelYolu(modelAd, gor.tip);
            var prefab = IlkBulunan<GameObject>("karakter/" + yol, "karakter/" + modelAd, "karakter/oyuncak");
            var js = IlkBulunan<TextAsset>("karakter/" + yol + "_clips", "karakter/" + modelAd + "_clips");
            if (js != null)
            {
                var meta = JsonUtility.FromJson<Meta>(js.text); tipMeta = meta != null && meta.tip != null && !string.IsNullOrEmpty(meta.tip.id) ? meta.tip : null; sacMeta = meta != null ? meta.sac : null; bantMeta = meta != null && meta.bant != null && meta.bant.r > 0 ? meta.bant : null;
                if (meta != null && meta.clips != null) foreach (var c in meta.clips) { if (c.poses > 0) Match.CLIP_N[c.name] = c.poses; LOOPS[c.name] = c.loop; if (c.adim > 0 && Match.ADIM_BOY.ContainsKey(c.name)) { Match.ADIM_BOY[c.name] = c.adim * SCALE; adimBoy[c.name] = c.adim * SCALE; } }
            }
            if (prefab != null)
            {
                PrefabAdi = prefab.name; modelAdi = DokuAdi(prefab.name, modelAd); model = Instantiate(prefab, yaw).transform; model.localPosition = Vector3.zero; model.localScale = Vector3.one * SCALE;
                anim = model.GetComponentInChildren<Animation>();
                tenAtlas = gor.ten; Varyant(gor); Paint(gor); if (!hakem) RaketSekil.Uygula(model, gor.kafa);   // ten önce: Varyant gövde atlasını Paint'ten önce bağlıyor
                if (anim != null && anim["idle"] != null) Sample("idle", 0, null, 0, 1);
                Calibrate(); AltKatmanKur();
                if (!hakem) aksesuar = Aksesuar.Kur(model, kHead, Vector3.up, bantMeta, gor.sapka, gor.yuz, gor.forma, gor.sac);
                if (!hakem && kThigh.Count > 0)
                {   // ayak kilidi: boşta pozunda (taban y=0'da) bilek yüksekliği
                    ZincirKur(); bilek0 = new float[2];
                    for (int i = 0; i < 2; i++) bilek0[i] = kBacak[i * 3 + 2] != null ? Mathf.Max(.02f, yaw.InverseTransformPoint(kBacak[i * 3 + 2].position).y) : -1;
                }
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
            string anahtar = modelAdi + ek; bool tenli = ek == "_govde" && tenAtlas.HasValue && modelAdi.StartsWith("oyuncak");
            if (tenli) anahtar += "#" + ColorUtility.ToHtmlStringRGB(tenAtlas.Value);
            Material m;
            if (tenli) { tenSira.Remove(anahtar); tenSira.Add(anahtar); }
            if (!dokuM.TryGetValue(anahtar, out m) || m == null)
            {
                if (tenli)
                {
                    tx = TenliAtlas(tx, tenAtlas.Value);
                    // 2026-10-31 (Unity incelemesi O3): her model × ton bir atlas kopyası (~1,3 MB) üretir ve statik sözlük
                    // onu sonsuza dek tutuyordu. En eski tonlar sözlükten düşer; yok etmek yerine bırakılır, çünkü sahnedeki bir
                    // figür hâlâ kullanıyor olabilir. Menüye dönüşteki UnloadUnusedAssets kullanılmayanı boşaltır.
                    while (tenSira.Count > TEN_LRU) { dokuM.Remove(tenSira[0]); tenSira.RemoveAt(0); }
                }
                dokuM[anahtar] = m = Kit.LitTex(Color.white, tx, .18f);
                // 2026-10-26: Gerçekçi model düz/çamurlu görünüyordu (yalnız renk dokusu): kıvrım/kas/yüz normalden,
                // ten/kumaş/ayakkabı farkı parlaklık haritasından. Dosya yoksa (oyuncak, sade) eski malzeme birebir.
                Kit.Bump(m, Resources.Load<Texture2D>("karakter/" + modelAdi + ek + "_n"));
                Kit.Gloss(m, Resources.Load<Texture2D>("karakter/" + modelAdi + ek + "_mg"));
            }
            return m;
        }
        static readonly Dictionary<string, Material> dokuM = new Dictionary<string, Material>();
        static readonly List<string> tenSira = new List<string>(); const int TEN_LRU = 6;
        // 2026-10-31 (yasin: "Ten renkleri değiştirilemiyor"): Oyuncak ailesinde ten ayrı parça değil, gövde ağının renk
        // atlasında bir hücre. "__ten" etiketi olmadığı için ten tonu hiçbir şeye bağlanmıyordu. Atlas GPU'da bir RT'ye
        // kopyalanıp okunur (doku Read/Write kapalı ve ASTC: DokuAyar), ten hücreleri seçilen tona boyanır, sonuç model+ton
        // başına bir kez üretilir. Ton yoksa ("Modelin rengi") atlas aynen.
        // r93 düzeltmesi: ilk yazım her modelde teni 221,179,151 sandı; Blender'da ağın UV'leri ölçülünce (yüzey alanı payı)
        // modeller üç ten hücresini farklı rollerde kullanıyor: oyuncak 129,97,63 (%55), güçlü 209,160,142 (%67), kadın ve
        // uzun 221,179,151 (%36 / %61). Diğer ikisi o modelde küçük ayrıntı (dudak, kulak içi). Bu yüzden atlas başına asıl
        // ten hücresi tabloda; üç hücre de seçilen tona asıl tene oranlarıyla boyanır (ayrıntı tenden açık/koyu kalır).
        // Atlas yeniden üretilip roller değişirse tabloyu tool/karakter ölçümüyle güncelle.
        Color? tenAtlas; public static string TenTani = "yok";   // ekran turu tanısı
        static readonly Color32[] TEN_HUCRE = { new Color32(221, 179, 151, 255), new Color32(209, 160, 142, 255), new Color32(129, 97, 63, 255) };
        static Color32 AsilTen(string atlas)
        {
            if (atlas == "oyuncak_govde") return TEN_HUCRE[2];
            if (atlas == "oyuncak_guclu_govde") return TEN_HUCRE[1];
            return TEN_HUCRE[0];
        }
        static bool Yakin(Color32 a, Color32 b) { return Mathf.Abs(a.r - b.r) <= 8 && Mathf.Abs(a.g - b.g) <= 8 && Mathf.Abs(a.b - b.b) <= 8; }
        static byte Oran(byte ton, byte hucre, byte asil) { return (byte)Mathf.Clamp(Mathf.RoundToInt(ton * (float)hucre / Mathf.Max(1, asil)), 0, 255); }
        static Texture2D TenliAtlas(Texture2D kaynak, Color ten)
        {
            int w = kaynak.width, h = kaynak.height;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(kaynak, rt); var once = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true, false) { name = kaynak.name + "_ten", filterMode = kaynak.filterMode, anisoLevel = kaynak.anisoLevel, wrapMode = kaynak.wrapMode };
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0, false); RenderTexture.active = once; RenderTexture.ReleaseTemporary(rt);
            Color32 tn = ten, asil = AsilTen(kaynak.name); var yeni = new Color32[TEN_HUCRE.Length];
            for (int k = 0; k < TEN_HUCRE.Length; k++) { var c = TEN_HUCRE[k]; yeni[k] = new Color32(Oran(tn.r, c.r, asil.r), Oran(tn.g, c.g, asil.g), Oran(tn.b, c.b, asil.b), 255); }
            var px = t.GetPixels32(); int say = 0;
            for (int i = 0; i < px.Length; i++) for (int k = 0; k < TEN_HUCRE.Length; k++) if (Yakin(px[i], TEN_HUCRE[k])) { px[i] = yeni[k]; if (Yakin(TEN_HUCRE[k], asil)) say++; break; }
            TenTani = kaynak.name + " " + w + "x" + h + " asıl ten " + asil.r + "," + asil.g + "," + asil.b + " → " + tn.r + "," + tn.g + "," + tn.b + " eşleşen " + say;
            if (say == 0) { Object.Destroy(t); return kaynak; }   // atlasta ten hücresi yok: dokuya dokunma
            t.SetPixels32(px); t.Apply(true, true); return t;
        }
        void Paint(Gorunum g)
        {
            tenAtlas = g.ten;
            // Dokulu modelde (Gerçekçi) forma = gövde dokusu × takım rengi + normal: forma dokusu açık gri gövde, koyu gri
            // yaka/raglan/kol ağzı; takım rengi gölgesi ve iki tonlu kesimiyle görünür. Doku yoksa eski düz renk birebir.
            // 2026-10-26 (oyun tasarımı incelemesi, r22 oyuncak_12/15: iki oyuncu da lacivert): oyuncak ailesinin _govde
            // dokusu renk ATLASI (oyuncak.py, 8×8 düz hücre); forma ağı atlasa girmez ama UV'si bir hücreye (lacivert şort)
            // düşüyordu → takım rengi × lacivert = iki takım da koyu lacivert. Doku × renk yalnız tinte hazır (açık gri
            // forma dokusu) modelde: ölçüt normal haritası (_govde_n) — bugün yalnız Gerçekçi. Atlas modeli düz takım rengi.
            var govdeTx = modelAdi != null && Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_n") != null
                ? Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde") : null;
            // 2026-10-27 forma kumaşı görünür (yasin: "forma tişört"): dokulu modelde kumaşın kendi forma dokusu
            // (<model>_forma_<kumas>: rocketbox_kit.py _kumas; hafif yan panel + delik, nefes çift şerit, esnek çapraz kuşak) ve
            // parlaklığı. Dosya yoksa (klasik, ya da doku üretilmemiş model) gövde dokusu: bugünkü görünüş birebir.
            var formaTx = govdeTx != null && g.kumas != null ? Resources.Load<Texture2D>("karakter/" + modelAdi + "_forma_" + g.kumas) : null;
            var formaM = govdeTx != null ? Kit.LitTex(g.forma, formaTx ?? govdeTx, .25f) : Kit.Lit(g.forma, .25f);
            if (govdeTx != null)
            {
                Kit.Bump(formaM, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_n"));
                Kit.Gloss(formaM, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_mg"), KumasParlak(g.kumas));
            }
            var M = new Dictionary<string, Material> { { "forma", formaM } };
            // Şort (2026-10-27): Gerçekçi'de ayrı derili ağ (rb_sort__sort, doku açık gri) → doku × şort rengi, seçim yoksa eski
            // lacivert (SORT_VARSAYILAN; doku 2026-10-27'ye dek bu rengi basılı taşıyordu). Düz renkli modelde (Sade) yalnız
            // oyuncu seçtiyse boyanır; seçim yoksa FBX'in kendi rengi. Hakem şortu aşağıda ayrıca lacivert (#0a1530).
            if (govdeTx != null)
            {
                var sm = Kit.LitTex(g.sort ?? Kit.Hex(SORT_VARSAYILAN), govdeTx, .2f); Kit.Bump(sm, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_n"));
                Kit.Gloss(sm, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_mg")); M["sort"] = sm;
            }
            else if (g.sort.HasValue) M["sort"] = Kit.Lit(g.sort.Value, .2f);
            // Karın (2026-10-29, Tüycük: tc_karin__karin): gövde forma rengiyle boyandığı için karın aynı rengin açığı (beyaza %60);
            // rakip ve hakem de kendi renklerinin açığını alır. Etiket yalnız Tüycük'te var: diğer modeller birebir aynı.
            M["karin"] = Kit.Lit(Color.Lerp(g.forma, Color.white, .6f), .3f);
            aykDoku = govdeTx;
            if (g.cerceve.HasValue) M["cerceve"] = Kit.Lit(g.cerceve.Value, .6f);
            if (g.saft.HasValue) M["koyu"] = Kit.Lit(g.saft.Value, .45f);   // şaft, boğaz ve dip kapağı (raket_koyu__koyu)
            if (g.bant.HasValue) M["bant"] = Kit.Lit(g.bant.Value, .2f); if (g.bilek.HasValue) M["bilek"] = Kit.Lit(g.bilek.Value, .2f);
            if (g.grip.HasValue) M["grip"] = g.gripCizgi ? Kit.LitTex(g.grip.Value, CizgiDoku(), .1f) : Kit.Lit(g.grip.Value, .15f); if (g.tel.HasValue) M["tel"] = Kit.Lit(g.tel.Value, .5f); if (g.tel2.HasValue) M["tel2"] = Kit.Lit(g.tel2.Value, .5f);
            if (g.ten.HasValue) M["ten"] = Kit.Lit(g.ten.Value, .3f); if (g.sac.HasValue) M["sac"] = Kit.Lit(g.sac.Value, .2f); if (g.ayakkabi.HasValue) M["taban"] = Kit.Lit(g.ayakkabi.Value, .25f);
            bool bantGizle = sacBantGizle;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string tag = Etiket(r.gameObject.name);
                // hakemde raket ve top (pozla kurulan rijit parçalar = deri ağı olmayan MeshRenderer'lar) gizli
                if (tag == "ayakkabi") { AyakkabiBoya(r, g.ayakkabi); continue; }
                if (tag == "sac" && r.gameObject.name.StartsWith("sac_")) { SacBoya(r, g.sac); continue; }
                if (tag == "isaret" || (tag == "bant" && (!g.bant.HasValue || bantGizle)) || (tag == "bilek" && !g.bilek.HasValue) || (hakem && !(r is SkinnedMeshRenderer))) { r.enabled = false; continue; }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                int kn = tag == "forma" ? 0 : tag == "cerceve" || tag == "grip" || tag == "tel" || tag == "tel2" ? 1 : tag == "bant" ? 2 : tag == "bilek" ? 3 : -1; if (kn >= 0) kanal[kn].Add(r);
                Material m; M.TryGetValue(tag, out m);
                if (m != null && (tag == "ten" || tag == "sac" || tag == "taban") && DokuluMu(r)) m = null;   // dokulu parça kendi dokusunu korur
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
        BantMeta bantMeta; Transform aksesuar; public Transform AksesuarKok { get { return aksesuar; } }
        readonly Gradient yayGrad = new Gradient(); readonly GradientAlphaKey[] yayS = new GradientAlphaKey[2]; float yayA = -1;
        static readonly GradientColorKey[] YAY_RENK = { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) };
        // Varsayılan şort: SORT_RENK (rocketbox_kit, düz lacivert .075/.095/.15) ÷ doku grisi .86 (gamma uzayında çarpım).
        public const string SORT_VARSAYILAN = "#161c2c";
        // Kumaşın parlaklık çarpanı (_GlossMapScale): esnek (likralı) parlak, hafif (delikli örgü) mat. Bilinmeyen = 1.
        public static float KumasParlak(string k) { return k == "esnek" ? 1.35f : k == "hafif" ? .8f : k == "nefes" ? .9f : 1f; }
        Texture2D aykDoku;
        bool DokuluMu(Renderer r) { foreach (var a in r.sharedMaterials) if (DokuMalzeme(a) != null) return true; return false; }
        // Havlu grip: sapı saran açık-koyu şerit (16×64, tekrar eden; bir kez üretilir, tüm figürler paylaşır).
        static Texture2D cizgi;
        static Texture2D CizgiDoku()
        {
            if (cizgi != null) return cizgi; cizgi = new Texture2D(16, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "grip_cizgi" };
            var px = new Color32[16 * 64]; for (int y = 0; y < 64; y++) for (int x = 0; x < 16; x++) { byte v = (byte)(((y + x / 2) / 6) % 2 == 0 ? 255 : 170); px[y * 16 + x] = new Color32(v, v, v, 255); }
            cizgi.SetPixels32(px); cizgi.Apply(); return cizgi;
        }
        // Yükseltme parlaması (HTML turParlat): o kanalın parçaları limona yanıp söner; k 0..1.
        public void Parlat(int kn, float k)
        {
            foreach (var r in kanal[kn]) foreach (var m in r.materials)
            {
                if (k > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.85f, 1, .35f) * k * 1.2f); }
                else { m.SetColor("_EmissionColor", Color.black); m.DisableKeyword("_EMISSION"); }
            }
        }

        // ---- Karakter varyantları (2026-10-26; Core/Kozmetik, varlık sözleşmesi karakter_varlik_sozlesmesi.md) ----
        // Tip: Oyuncak modelin beden tipi ayrı FBX (oyuncak_<varlik>.fbx, aynı iskelet ailesi ve klip adları). Gerçekçi ve sade
        // modelde tip yok (tek beden; Soyunma odası bunu yazar). Dosya yoksa (karakter hattı henüz üretmediyse) taban model, hata
        // yok: Init önce bu yolu, sonra taban modeli dener. Beden yalnız görünüş: vuruş menzili ve fizik Core'da, boydan bağımsız.
        public static string ModelYolu(string modelAd, string tip)
        {
            if (modelAd != "oyuncak") return modelAd;
            var o = Kozmetik.Oge(Kozmetik.TIP, tip); return o != null && o.varlik != null ? modelAd + "_" + o.varlik : modelAd;
        }
        // Doku adı: tip FBX'inin kendi gövde dokusu yoksa taban modelinki (oyuncak_govde). Varsayım: tipler aynı UV yerleşimini
        // paylaşır (aynı taban ağdan şekil anahtarıyla türer); sözleşme başka derse tipin kendi dokusu konur, kod değişmez.
        static string DokuAdi(string prefabAd, string modelAd)
        {
            if (prefabAd == modelAd || Resources.Load<Texture2D>("karakter/" + prefabAd + "_govde") != null) return prefabAd;
            return Resources.Load<Texture2D>("karakter/" + modelAd + "_govde") != null ? modelAd : prefabAd;
        }
        // Saç ve ayakkabı parçaları: sac_<varlik>__sac, ayk_<varlik>__ayakkabi. Tam olarak biri açık, diğerleri SetActive(false)
        // (çizim çağrısı sıfır, deri hesabı da yok). Seçilen parça bu FBX'te yoksa varsayılanın (listenin ilki) parçası, o da
        // yoksa ilk bulunan: figür hiçbir zaman saçsız/ayakkabısız kalmaz. Kazıtılmış (varlik null) bilerek hiçbirini açmaz.
        // 2026-10-27: Karakter v3'ten (51f396a) beri dört oyuncak FBX'inin her birinde 11 saç + 5 ayakkabı parçası var (eski not
        // "bugünkü oyuncak.fbx'te yok" geçersiz). Sade modelde parça yok: sözlükler boş, hiçbir şey değişmez. 2026-10-27'den beri
        // Gerçekçi'de beş ayakkabı parçası var (rocketbox_aksesuar.py; saç yok: saç dokuda, beden tek).
        bool sacBantGizle; TipMeta tipMeta; SacMeta[] sacMeta;
        readonly Dictionary<string, float> adimBoy = new Dictionary<string, float>();
        // Açılan parçalar (tanı ve vitrin kadrajı): istenen bulunamayıp varsayılana düşüldüyse istenenden farklıdır.
        public string AcikSac { get; private set; }
        public string AcikAyk { get; private set; }
        public string PrefabAdi { get; private set; }
        void Varyant(Gorunum g)
        {
            var sac = new Dictionary<string, GameObject>(); var ayk = new Dictionary<string, GameObject>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                int i = t.name.IndexOf("__"); if (i <= 4) continue; string bas = t.name.Substring(0, i), tag = Etiket(t.name);
                if (tag == "sac" && bas.StartsWith("sac_")) sac[bas.Substring(4)] = t.gameObject;
                else if (tag == "ayakkabi" && bas.StartsWith("ayk_")) ayk[bas.Substring(4)] = t.gameObject;
            }
            // Seçim yoksa (hakem, eski çağrı) ya da seçilen parça bu FBX'te yoksa tipin varsayılanı (sözleşme §6.2: _clips.json tip).
            string sm = g.sacModel; if (Aksesuar.Var(g.sapka) && sm != null && Aksesuar.HACIMLI.Contains(sm)) sm = "kisa";   // şapkanın altında hacimli saç kısa kesime döner
            var so = sm != null ? Kozmetik.Bul(Kozmetik.SAC, sm) : null; var sv = Kozmetik.Oge(Kozmetik.SAC, null);
            string sacVars = tipMeta != null && !string.IsNullOrEmpty(tipMeta.sac) ? tipMeta.sac : sv != null ? sv.varlik : null;
            string acik = VaryantAc(sac, so != null ? so.varlik : sacVars, so != null && so.varlik == null, sacVars); AcikSac = acik;
            bool bant = so == null || so.bant; if (acik != null && sacMeta != null) foreach (var m in sacMeta) if (m != null && m.id == acik) bant = m.bant;   // FBX'in ölçtüğü kazanır
            sacBantGizle = acik != null && !bant;   // bant yalnız o saç gerçekten takılıysa gizlenir
            GearModel am = null; if (g.aykModel != null) Gear.GEAR["ayakkabi"].TryGetValue(g.aykModel, out am);
            GearModel av; Gear.GEAR["ayakkabi"].TryGetValue(Gear.Varsayilan("ayakkabi"), out av);
            string aykVars = tipMeta != null && !string.IsNullOrEmpty(tipMeta.ayakkabi) ? tipMeta.ayakkabi : av != null ? av.varlik ?? av.id : null;
            AcikAyk = VaryantAc(ayk, am != null ? am.varlik ?? am.id : aykVars, false, aykVars);
        }
        // Tanı satırı (2026-10-27, yasin build 41: "3B model kaybolmuş, seçmeme rağmen görmedim"): hangi FBX yüklendi, hangi
        // saç/ayakkabı parçası açık, kaç renderer gerçekten çiziliyor (açık + etkin, iz hariç), baş yüksekliği ve omuz genişliği
        // (kemikten: AABB figür döndükçe değişir, kemik aralığı değişmez), açık saç ve ayakkabı parçasının sınırı. Ekran turu
        // her tip/saç/ayakkabı için vitrinde ve maçta yazar; bir seçimin görünüşe gerçekten yansıyıp yansımadığı sayıyla okunur.
        public string VaryantOzet() { float b, o; Vector3 sb; int n; return VaryantOzet(out b, out o, out sb, out n); }
        public string VaryantOzet(out float basY, out float omuzG, out Vector3 sacBoyut, out int aktif)
        {
            basY = omuzG = -1; sacBoyut = Vector3.zero; aktif = 0;
            if (model == null) return "MODEL YOK";
            int n = 0; Bounds sacB = new Bounds(), aykB = new Bounds(); bool sv = false, av = false; Transform bas = null, ol = null, or_ = null;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r is TrailRenderer || !r.enabled) continue; n++; string tag = Etiket(r.gameObject.name);
                if (tag == "sac") { if (!sv) { sacB = r.bounds; sv = true; } else sacB.Encapsulate(r.bounds); }
                if (tag == "ayakkabi" || tag == "taban") { if (!av) { aykB = r.bounds; av = true; } else aykB.Encapsulate(r.bounds); }
            }
            foreach (var t in model.GetComponentsInChildren<Transform>())
            {
                string k = t.name.ToLowerInvariant(); if (k.Contains("__")) continue;
                if (bas == null && (k == "head" || k.EndsWith(" head"))) bas = t;
                if (ol == null && (k == "upperarm_l" || k == "upperarm.l" || k.EndsWith(" l upperarm"))) ol = t;
                if (or_ == null && (k == "upperarm_r" || k == "upperarm.r" || k.EndsWith(" r upperarm"))) or_ = t;
            }
            aktif = n; if (bas != null) basY = bas.position.y - transform.position.y; if (ol != null && or_ != null) omuzG = Vector3.Distance(ol.position, or_.position); if (sv) sacBoyut = sacB.size;
            var ci = System.Globalization.CultureInfo.InvariantCulture; System.Func<Vector3, string> V = v => v.x.ToString("0.00", ci) + "×" + v.y.ToString("0.00", ci) + "×" + v.z.ToString("0.00", ci);
            return "prefab " + PrefabAdi + " · tip " + (tipMeta != null ? tipMeta.id : "-") + " · saç " + (AcikSac ?? "yok") + " · ayk " + (AcikAyk ?? "yok") + " · aktif renderer " + n
                + " · baş " + (bas != null ? (bas.position.y - transform.position.y).ToString("0.000", ci) : "-") + " · omuz " + (ol != null && or_ != null ? Vector3.Distance(ol.position, or_.position).ToString("0.000", ci) : "-")
                + " · saç sınır " + (sv ? V(sacB.size) : "-") + " · ayk sınır " + (av ? V(aykB.size) : "-");
        }
        // Vitrinin Yüz planı: açık saç parçası (afro, topuz kadrajdan taşmasın diye kadraj saçın sınırına göre kurulur).
        public Renderer SacRenderer()
        {
            if (model == null) return null;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is TrailRenderer) && Etiket(r.gameObject.name) == "sac") return r;
            return null;
        }
        static string VaryantAc(Dictionary<string, GameObject> d, string istenen, bool hicbiri, string varsayilan)
        {
            if (d.Count == 0) return null;
            string ac = hicbiri ? null : istenen != null && d.ContainsKey(istenen) ? istenen : varsayilan != null && d.ContainsKey(varsayilan) ? varsayilan : null;
            if (ac == null && !hicbiri) foreach (var k in d.Keys) { ac = k; break; }
            foreach (var kv in d) kv.Value.SetActive(kv.Key == ac);
            return ac;
        }
        // Ayakkabı (sözleşme §3): iki malzeme yuvası. "ayk_ust" (saya) seçili ayakkabı rengiyle boyanır (null = FBX'in açık
        // rengi), "oy_body" (taban, bağcık, topuk) gövde atlasına bağlanır (DokuMalzeme), boyanmaz: kontrast kaybolmasın.
        // Eski (__taban) etiketli modellerde boyama aynen M["taban"] ile sürer.
        // 2026-10-27 Gerçekçi (rocketbox_aksesuar.py): beş ayakkabı modeli aynı yuva adlarıyla; saya dokulu: gövde dokusu (beyaz
        // saya, gri parçalar) × renk + normal (seçim yoksa beyaz = dokunun kendisi), taban "m301_body" (klasik: kauçuk, doku) ya
        // da "ayk_alt_*" (FBX'in düz rengi: beyaz ince/kalın taban, kömür karbon) boyanmaz.
        void AyakkabiBoya(Renderer r, Color? c)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var arr = r.sharedMaterials; bool degisti = false;
            for (int k = 0; k < arr.Length; k++)
            {
                if (arr[k] == null) continue;
                if (arr[k].name.StartsWith("ayk_ust") && aykDoku != null)
                {
                    var um = Kit.LitTex(c ?? Color.white, aykDoku, .3f); Kit.Bump(um, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_n"));
                    Kit.Gloss(um, Resources.Load<Texture2D>("karakter/" + modelAdi + "_govde_mg")); arr[k] = um; degisti = true;
                }
                else if (arr[k].name.StartsWith("ayk_ust")) { if (c.HasValue) { arr[k] = Kit.Lit(c.Value, .3f); degisti = true; } }
                else { var dm = DokuMalzeme(arr[k]); if (dm != null) { arr[k] = dm; degisti = true; } }
            }
            if (degisti) r.sharedMaterials = arr;
        }
        // Saç (sözleşme §3): tel/AO dokusu (oyuncak_sac, gri tonlu) × saç rengi; seçim yoksa tipin varsayılan rengi (_clips.json
        // tip.sacRenk), o da yoksa FBX malzemesi. Doku yoksa düz renk (AO kaybolur, görünüş yine düzgün).
        static readonly Dictionary<string, Material> sacM = new Dictionary<string, Material>();
        void SacBoya(Renderer r, Color? c)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Color? renk = c ?? (tipMeta != null && !string.IsNullOrEmpty(tipMeta.sacRenk) ? Kit.Hex(tipMeta.sacRenk) : (Color?)null);
            var tx = Resources.Load<Texture2D>("karakter/oyuncak_sac"); if (!renk.HasValue && tx == null) return;
            if (tx != null) tx.wrapMode = TextureWrapMode.Repeat;   // tel dokusu UV'de 1'i aşar; .meta içe aktarma ayarına bağlı kalmasın
            Color rk = renk ?? (r.sharedMaterial != null ? r.sharedMaterial.color : Color.gray);
            // 2026-10-31 (Unity incelemesi Y1): 16 saç parçasının her biri ayrı Material açıyordu (figür başına 16, her
            // saç/renk değişikliğinde yeniden). Renk + doku başına tek malzeme paylaşılır; saç malzemesine sonradan
            // yazan kod yok (parlatma yalnız raket, kanal boyası forma/raket), bu yüzden paylaşmak görünüşü değiştirmez.
            string sk = ColorUtility.ToHtmlStringRGBA(rk) + (tx != null ? "t" : "d"); Material m;
            if (!sacM.TryGetValue(sk, out m) || m == null) sacM[sk] = m = tx != null ? Kit.LitTex(rk, tx, .2f) : Kit.Lit(rk, .2f);
            var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = m; if (arr.Length == 0) arr = new[] { m }; r.sharedMaterials = arr;
        }
        // Vitrin tutuş görünümü (Soyunma odası › Raket: Forehand · Backhand · File · Servis): klibin temas pozunda durur; yalnız
        // poz değişince örneklenir (karede ayırma ve örnekleme yok).
        string pozK; float pozP = -1;
        public void ShowPoz(Vector3 pos, float yawDeg, string clip, float ph)
        {
            transform.position = pos; yaw.localRotation = Quaternion.Euler(0, yawDeg, 0) * calib;
            if (shadow != null) shadow.position = new Vector3(pos.x, .003f, pos.z);
            if (anim == null) return; if (D(clip) == null) clip = "idle";
            if (cur == clip && pozK == clip && Mathf.Abs(pozP - ph) < 1e-4f) return;
            cur = clip; pozK = clip; pozP = ph; Sample(clip, ph, null, 0, 1);
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

        // Alt beden katmanı (2026-10-26, yasin "model karakterlerin tüm hareketleri badmintona uygun olmalı"): hazırlık ve vuruş
        // klipleri bütün bedeni sürüyordu; oyuncu topa koşarken de bacaklar vuruş duruşunda donup zeminde kayıyordu (bot
        // ölçümü, tool/unity_test: hazırlık/vuruş karelerinin ~%45'inde gövde 0,3 m/s'den hızlı, ortalama 0,9–1,4 m/s). Spor
        // oyunlarının olağan çözümü: üst beden vuruştan, leğen ve bacaklar ayak işi klibinden. Ayak işi klibinin kopyası
        // (<ad>_alt) yalnız leğeni (özyinelemesiz) ve uyluk zincirlerini sürer, katman 1'dedir; Legacy Animation katman 1'in
        // ağırlığını yalnız onun sürdüğü kemiklerden düşer, omurga/kollar/raket vuruş klibinde kalır. Ağırlık gövde hızıyla
        // açılır (Match.ALT_HIZ üstel yumuşatma: pop yok; eşik ve istisnalar Match.AltHedef'te). Bacakları vuruşun kendisi olan kliplerde
        // (hamle, sıçrama, servis, split, yorgun) kapalı, istisnalar orada. Kemikler bulunamazsa kurulmaz: eski davranış birebir. HTML'de karşılığı yok (sprite tek
        // parça); bu yalnız görünüm, oyun kuralı değil.
        static readonly string[] AYAK_KLIP = { "chasse_r", "chasse_l", "fwd", "back_r", "back_l", "run", "kros_r", "kros_l" };   // run/kros_*: 2026-10-26 hız bantları
        bool altVar; float altW, altPh; string altKlip;
        void AltKatmanKur()
        {
            if (anim == null || kHip == null || kThigh.Count < 2) return;
            var kok = new List<Transform>();   // iç içe uyluk kemikleri (twist) bir kez: üstteki özyinelemeli eklenince altı da gelir
            foreach (var t in kThigh) { bool alt = false; for (var p = t.parent; p != null; p = p.parent) if (kThigh.Contains(p)) { alt = true; break; } if (!alt) kok.Add(t); }
            foreach (var c in AYAK_KLIP)
            {
                var st = anim[c]; if (st == null || st.clip == null) continue;
                anim.AddClip(st.clip, c + "_alt"); var a = anim[c + "_alt"]; if (a == null) continue;
                a.AddMixingTransform(kHip, false); foreach (var t in kok) a.AddMixingTransform(t, true);
                a.layer = 1; altVar = true;
            }
            durum.Clear(); durumlar = null;   // yeni _alt durumları eklendi
        }

        // Durum önbelleği (2026-10-27 performans turu 2): anim["ad"] her çağrıda yerelden yeni bir AnimationState sarmalayıcısı
        // döndürür, foreach (AnimationState in anim) bir sayaç nesnesi, alt + "_alt" yeni bir dizgi: figür başına karede ~12
        // ayırma, dört figür (iki oyuncu + iki hakem) ve afiş/vitrinle karede ~60. Durumlar ad başına bir kez sorulur (yoksa null
        // da saklanır: klip listesi yalnız AltKatmanKur'da değişir, orada önbellek sıfırlanır), tüm durumlar dizide tutulur.
        readonly Dictionary<string, AnimationState> durum = new Dictionary<string, AnimationState>(); AnimationState[] durumlar;
        static readonly Dictionary<string, string> ALT_AD = new Dictionary<string, string>();
        static string AltAd(string klip) { string a; if (!ALT_AD.TryGetValue(klip, out a)) ALT_AD[klip] = a = klip + "_alt"; return a; }
        AnimationState D(string ad)
        {
            if (anim == null || ad == null) return null; AnimationState st;
            if (!durum.TryGetValue(ad, out st)) { st = anim[ad]; durum[ad] = st; }
            return st;
        }
        AnimationState[] Durumlar()
        {
            if (durumlar != null) return durumlar;
            var l = new List<AnimationState>(); foreach (AnimationState st in anim) l.Add(st); durumlar = l.ToArray(); return durumlar;
        }
        int ornekNo;   // her Sample'da artar: ShowSit "pozum hâlâ benim" diye bakar
        // c (prev2): harmanlama sürerken gelen değişimde sönen üçüncü poz; eski karışımın (1 − w) payının cPay kadarı (Show notu).
        void Sample(string a, float pa, string b, float pb, float w, string alt = null, float aPh = 0, float aW = 0, string c = null, float pc = 0, float cPay = 0)
        {
            if (anim == null) return;
            ornekNo++; var tum = Durumlar(); for (int i = 0; i < tum.Length; i++) tum[i].enabled = false;
            var A = D(a); if (A == null) return;
            A.enabled = true; A.weight = w; A.normalizedTime = ClipTime(a, pa);
            if (b != null && w < 1)
            {
                var B = D(b);
                if (B != null)
                {
                    float wb = 1 - w; var C2 = c != null && cPay > .001f && c != a && c != b ? D(c) : null;
                    if (C2 != null) { C2.enabled = true; C2.weight = wb * cPay; C2.normalizedTime = ClipTime(c, pc); wb *= 1 - cPay; }
                    B.enabled = true; B.weight = wb; B.normalizedTime = ClipTime(b, pb);
                }
            }
            if (alt != null && aW > .01f) { var C = D(AltAd(alt)); if (C != null) { C.enabled = true; C.weight = aW; C.normalizedTime = ClipTime(alt, aPh); } }
            anim.Sample();
            for (int i = 0; i < tum.Length; i++) tum[i].enabled = false;
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
            // Uzanma eğimi için sağ omzun atalarından omurga kemikleri, alttan üste (spine_01.. / Bip01 Spine..).
            kOmurga.Clear();
            for (var t = kKol[3] != null ? kKol[3].parent : null; t != null && t != model; t = t.parent)
                if (t.name.ToLowerInvariant().Contains("spine")) kOmurga.Insert(0, t);
        }
        readonly List<Transform> kOmurga = new List<Transform>();
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
        // Oturuş bir kez (2026-10-27 performans turu 2): hakemler rallide kıpırdamaz ama her karede idle örneklenip dört IK
        // çözülüyordu (iki figür × iki Sample + kalça/baş/4 zincir). Girdiler (oturak, açılar, ayak yeri) aynı, araya başka
        // örnekleme girmemiş (ornekNo) ve kök yerinden oynamamışsa poz zaten kemiklerde: dönülür. Skor girişi ve servis
        // dönüşü açıyı değiştirdiği karelerde aynen yeniden kurulur.
        Vector3 sitO, sitKonum; Vector4 sitK; Quaternion sitYaw; int sitNo = -1;
        public void ShowSit(Vector3 oturak, float yawDeg, float tiltDeg, float ayakY = 0, float ayakIleri = .4f)
        {
            var sk = new Vector4(yawDeg, tiltDeg, ayakY, ayakIleri);
            if (sitNo == ornekNo && sitO == oturak && sitK == sk && transform.position == sitKonum && yaw.localRotation == sitYaw) return;
            ShowSitKur(oturak, yawDeg, tiltDeg, ayakY, ayakIleri);
            sitO = oturak; sitK = sk; sitNo = ornekNo; sitKonum = transform.position; sitYaw = yaw.localRotation;
        }
        void ShowSitKur(Vector3 oturak, float yawDeg, float tiltDeg, float ayakY, float ayakIleri)
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

        float jGor;
        public void Show(Athlete o, Match M, float dt)
        {
            // kendi bacağının adımı ve adım tavanı (Match.Athlete.adimBoy/ayakTurMax notu): tavan chassé adımı 0,99 m'den
            // kısaysa o oranda yükselir (en çok 6 tur/sn); gerçek oranlı modellerde adım ≥ 0,99, tavan aynen 3,6.
            // Tur 2: her karede yazılır (adım boyu yoksa null/NaN): menü arkasındaki maç model değişiminden sonra da sürüyor,
            // eski figürün sözlüğü sporcuda kalıyordu.
            { float ch; o.adimBoy = adimBoy.Count > 0 ? adimBoy : null; o.ayakTurMax = adimBoy.TryGetValue("chasse_r", out ch) && ch > .05f ? Mathf.Clamp(Match.AYAK_TUR_MAX * .99f / ch, Match.AYAK_TUR_MAX, 6f) : float.NaN; }
            // Sıçrama: sin(jump·π) (tepe temasta), split step küçük hop (%20). 0,5 m ≈ HTML'deki 16 px/28 px-m ölçeği.
            string clip; float ph; M.PickClip(o, isMe, out clip, out ph);   // önce: split klibi kendi hop'unu taşır (Match.JumpH hopsuz)
            float j = Match.JumpH(o, clip == "split" && anim != null && D("split") != null) * .5f;
            jGor = j > jGor ? Mathf.MoveTowards(jGor, j, Match.GORSEL_YUKSELIS * dt) : j; j = jGor;   // yükseliş tavanı: ışınlanma yok
            transform.position = Kit.W(o.x, o.y, 0) + Vector3.up * j;
            // Gövde dik (ce47f5c): hamlede yatma yok; yalnız kıl payı kurtarışta (yat) en çok 15°. Koşu eğimi ±0,08 rad (~4,6°;
            // Ar-Ge 23 Eki B3: render edilmiş figürü düz resim gibi yatırmak sahte görünür, ayaklar yerden kalkar).
            // 2026-10-27 (ekran/kosu): gövde gidişe döner (Match.AyakIsi, Athlete.donus; arka sahaya yan dönük, ön köşeye
            // köşeye bakarak). Eğim gövdenin kendi yanına göre: dönükken dünya x'inden eğilmek öne/arkaya yatırırdı.
            float donR = o.donus * Mathf.Deg2Rad, yanK = isMe ? o.svx : -o.svx, ileriK = isMe ? -o.svy : o.svy;
            float lean = Mathf.Clamp((yanK * Mathf.Cos(donR) - ileriK * Mathf.Sin(donR)) * .02f, -.08f, .08f);
            float dive = (o.yat ? o.dive : 0) * (o.x < Court.MID ? -1 : 1) * .26f * (isMe ? 1 : -1);
            yaw.localRotation = Quaternion.Euler(0, (isMe ? 0 : 180) + o.donus, 0) * Quaternion.Euler(0, 0, -(dive + lean) * Mathf.Rad2Deg) * calib;
            shadow.position = Kit.W(o.x, o.y, .003f); shadow.localScale = Vector3.one * (.9f - j * .8f);
            ShowRing(o.nefes, dt, M.reduce); ImzaIsik(o, M.reduce, dt);
            if (yay != null)
            {
                yay.emitting = o.sw > .3f && !M.reduce; float a = Mathf.Clamp01(o.sw) * .5f;
                // 2026-10-27: renk eğrisi her karede yeni Gradient + iki dizi idi; yalnız alfa değişince ve aynı nesneyle
                if (Mathf.Abs(a - yayA) > .002f) { yayA = a; yayS[0] = new GradientAlphaKey(a, 0); yayS[1] = new GradientAlphaKey(0, 1); yayGrad.SetKeys(YAY_RENK, yayS); yay.colorGradient = yayGrad; }
            }
            if (anim == null) { if (fbRacket != null) fbRacket.localRotation = Quaternion.Euler(0, 0, o.sw > 0 ? -140 * (1 - o.sw) + 40 : 20 - 40 * o.prepA); return; }
            if (D(clip) == null) clip = "idle";
            if (clip != cur)
            {
                bool takip = cur != null && Match.CLIP_HIT.ContainsKey(cur) && !Match.CLIP_HIT.ContainsKey(clip);   // vuruş → hazır duruş/ayak
                // Harmanlama sürerken gelen değişim (2026-10-27 performans turu 2; ölçüm tool/unity_test/GecisOlc, Core bot maçı; kural orada kopya): klip
                // değişimlerinin %22'si (120 Hz'de %25) önceki 0,08/0,2 sn'lik geçiş bitmeden geliyordu (split → chassé → kros,
                // fwd → back → chassé); eski kodda o anki karışımın "prev" payı (ortalama %46–52) tek karede düşüyordu: pop.
                // Şimdi (1) aynı klibe geri dönüşte geçiş ters çevrilir (blend → 1 − blend, ağırlıklar sürekli), (2) başka klipte
                // ekranın o anki iki pozu (cur ve prev) birlikte söner: prev2 = prev, payı karışım içindeki oranı. Dördüncü poz
                // gelirse en eskisi (prev2) düşer. Ölçüm: %25'ten büyük tek kare düşüşü 1198 → 80 (60 Hz), 1459 → 137 (120 Hz),
                // toplam düşen ağırlık −%94 / −%92.
                TaniDegisim++; if (prev != null && blend < 1) TaniAra++;
                if (prev != null && blend < 1 && clip == prev) { prev = cur; prevPh = lastPh; cur = clip; blend = 1 - blend; prev2 = null; }
                else
                {
                    if (prev != null && blend < 1)
                    {
                        float wc = Mathf.SmoothStep(0, 1, blend), wp = (1 - wc) * (1 - (prev2 != null ? prev2Pay : 0));
                        float dus = (1 - wc) * (prev2 != null ? prev2Pay : 0); TaniDusen += dus; if (dus > .25f) TaniPop++;
                        prev2 = prev; prev2Ph = prevPh; prev2Pay = wc + wp > 1e-4f ? wp / (wc + wp) : 0;
                    }
                    else prev2 = null;
                    prev = cur; prevPh = lastPh; cur = clip; blend = 0;
                }
                blendS = takip ? .2f : .08f;
            }
            blend = Mathf.Min(1, blend + dt / blendS); lastPh = ph; if (blend >= 1) prev2 = null;
            // Boşta duruşun genliği nefese bağlı (B4; hız Match.IdleIlerle'de): nefes %70'in altına inince yorgun klibi
            // (eller dizde) %35'e kadar karışır, beden soluklanır. Harmanlama bitmeden karıştırılmaz.
            if (cur == "idle" && blend >= 1 && o.nefes < .7f && D("yorgun") != null)
            { Sample("idle", ph, "yorgun", ph, 1 - .35f * Mathf.Clamp01((.7f - o.nefes) / .7f)); uzanVar = false; kalcaKay = Vector3.zero; FileItin(); AyakKilit(dt); Sevinc(M, dt); return; }
            {   // alt beden katmanı: vuruş/hazırlık klibinde koşarken bacaklar ayak işinden. Hangi klipte ve ne ağırlıkla:
                // Match.AltHedef (hamle yaklaşırken, servis temastan sonra, sıçrama yerdeyken; anlık hızla). 2026-10-26 gece Core'a
                // taşındı: ölçüm aracı (tool/unity_test/AnimOlc) aynı kuralı okur.
                if (o.yuru && o.ayakKlip != null) { altKlip = o.ayakKlip; altPh = o.runPh; }
                float hedef = altVar && altKlip != null ? Match.AltHedef(o, cur) : 0;
                altW += (hedef - altW) * (1 - Mathf.Exp(-dt * Match.ALT_HIZ)); if (altW < .01f && hedef == 0) altW = 0;   // üstel: 60/120 Hz aynı eğri
            }
            hazirKafa = UzanHazir(o, M) ? KafaVurusta(cur) : null;   // ana örneklemeden önce: önbellekte yoksa vuruş karesi örneklenir
            Sample(cur, ph, prev, prevPh, prev == null ? 1 : Mathf.SmoothStep(0, 1, blend), altKlip, altPh, altW, prev2, prev2Ph, prev2Pay);   // smoothstep (2026-10-26): doğrusal ağırlık geçişin başında ve sonunda hız kırığı veriyordu
            KalcaDonus(o);   // ana örneklemeden hemen sonra: uzanma kalçayı kaydırmadan, ayak kilidi IK'dan önce
            Uzan(o, M, j);   // kayma ve kalça: ayak kilidinden önce (kilit kalça kaymasını ayakta geri alır)
            FileItin(); AyakKilit(dt); HazirNefes(o, M); UzanKol(); ServisEller(); Sevinc(M, dt);
        }

        // ---- Sevinç (2026-11-01, Ar-Ge K3; yasin "maç sonunda da sevinç kategorisi kısmını gönder") ----
        // Sayıyı kazanan oyuncu sayılar arası beklemede kısa bir hareket yapar; maç bitince kazanan zafer hareketini, kaybeden
        // "iyi oyundu"yu (raketi rakibe kaldırıp başını eğer) döngüde yapar. Klip değil: örneklenen pozun üstüne kemik işlemi
        // (iki kemik IK kollar, omurga eğimi, sıçrama ve dönüş), bu yüzden dört modelde de yeni FBX'siz çalışır. Ağırlık 0→1→0
        // yumuşak; ralli başlayınca (between bitti) .15 sn'de söner, servis pozuna karışmaz. Hareketi azaltta sıçrama, dönüş ve
        // salınım yok, kollar yalnız kalkar. Oyuncunun seçimi Ayar.KOZ (Dolap › Sevinç); rakip hep varsayılanı yapar.
        // Raket her zaman sağ elde (kural): sağ kol raketi taşır, el sallama ve alkış sol elle.
        string sevId; float sevT = -1, sevW; bool sevDongu; int sevTop = -1;
        public const float SEVINC_SURE = 1.0f;
        void Sevinc(Match M, float dt)
        {
            if (hakem || anim == null) return;
            int top = M.sc[0] + M.sc[1];
            if (M.ogr != null) { sevT = -1; sevTop = top; return; }
            if (sevTop < 0 || top < sevTop) sevTop = top;   // ilk kare ya da yeni maç
            if (top > sevTop)
            {
                sevTop = top; bool benimki = isMe == (M.server == "me");
                if (M.matchDone) { bool kazandim = isMe ? M.sc[0] > M.sc[1] : M.sc[1] > M.sc[0]; sevId = kazandim ? (isMe ? Secim(Kozmetik.MAC_SONU) : "zafer") : "iyi_oyun"; sevDongu = true; sevT = 0; }
                else if (benimki) { sevId = isMe ? Secim(Kozmetik.SEVINC) : "yumruk"; sevDongu = false; sevT = 0; }
            }
            if (sevT < 0) { sevW = 0; return; }
            sevT += dt;
            bool surer = sevDongu ? M.matchDone : M.between && sevT < SEVINC_SURE;
            float hedef = surer ? 1 : 0; sevW = Mathf.MoveTowards(sevW, hedef, dt / (surer ? .15f : .15f));
            if (!surer && sevW <= 0) { sevT = -1; return; }
            SevincPoz(sevId, sevT, sevW, M.reduce);
        }
        static string Secim(string kat) { string v; return Ayar.KOZ.TryGetValue(kat, out v) && v != null ? v : Kozmetik.Varsayilan(kat); }
        // Vitrin önizlemesi (Dolap › Sevinç): boşta pozunun üstüne, döngüde.
        public void ShowSevinc(Vector3 pos, float yawDeg, string id, float t)
        {
            transform.position = pos; yaw.localRotation = Quaternion.Euler(0, yawDeg, 0) * calib;
            shadow.position = new Vector3(pos.x, .003f, pos.z);
            if (anim == null) return; Sample("idle", .25f, null, 0, 1); cur = "_sevinc";   // ShowStatic çıkışta boşta pozunu yeniden örneklesin
            float d = id == "iyi_oyun" || id == "zafer" || id == "selam" || id == "egil" ? 2.4f : 1.6f, tt = t % d;
            float w = Mathf.Clamp01(Mathf.Min(tt / .15f, (d - tt) / .25f));
            SevincPoz(id, tt, w, Ayar.reduce);
        }
        readonly Quaternion[] sevKayit = new Quaternion[6]; readonly Transform[] sevKem = new Transform[6];
        void SevincPoz(string id, float t, float w, bool rm)
        {
            if (kKol == null) ZincirKur(); if (kKol == null || w <= 0.001f) return;
            Transform SU = kKol[3], SE = kKol[4], SH = kKol[5], LU = kKol[0], LE = kKol[1], LH = kKol[2];
            Vector3 up = yaw.up, ri = yaw.right, fw = yaw.forward;
            float salla = rm ? 0 : 1;
            // sıçrama ve dönüş (kök): ağırlıkla
            float hop = 0, don = 0, egil = 0;
            switch (id)
            {
                case "kollar": hop = .06f * Mathf.Abs(Mathf.Sin(t * 7)); break;
                case "zafer": hop = .09f * Mathf.Abs(Mathf.Sin(t * 6)); break;
                case "zipla": hop = .22f * Mathf.Sin(Mathf.Clamp01(t / .6f) * Mathf.PI); don = 360 * Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .6f)); break;
                case "egil": egil = 32 * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t % 2.4f) / .5f)); break;
                case "iyi_oyun": egil = 10 * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .35f) / .4f)); break;
            }
            transform.position += up * hop * w * salla;
            if (don != 0 && salla > 0) yaw.localRotation = Quaternion.AngleAxis(don, Vector3.up) * yaw.localRotation;
            // kayıt (yerel dönüşler): IK tam ağırlıkla çözülür, sonra kayıtla harmanlanır
            var kem = sevKem; kem[0] = SU; kem[1] = SE; kem[2] = LU; kem[3] = LE; kem[4] = kOmurga.Count > 0 ? kOmurga[0] : null; kem[5] = null;   // kare başı ayırma yok
            for (int i = 0; i < kem.Length; i++) if (kem[i] != null) sevKayit[i] = kem[i].localRotation;
            if (egil > 0 && kOmurga.Count > 0) { var sp = kOmurga[0]; sp.rotation = Quaternion.AngleAxis(egil, ri) * sp.rotation; }
            float L = SU != null && SE != null && SH != null ? (SE.position - SU.position).magnitude + (SH.position - SE.position).magnitude : .5f;
            Vector3 sR = SU != null ? SU.position : Vector3.zero, sL = LU != null ? LU.position : Vector3.zero;
            float s = Mathf.Sin(t * 14) * salla;
            switch (id)
            {
                case "yumruk":   // raketli el yukarı itilir (iki kez), sol yumruk göğüste
                    IK(SU, SE, SH, sR + up * L * (.78f + .1f * Mathf.Max(0, s)) + ri * L * .12f + fw * L * .18f, ri - fw * .4f);
                    IK(LU, LE, LH, sL + fw * L * .4f - up * L * .25f + ri * L * .3f, -up - ri * .3f); break;
                case "kollar": case "zafer": case "zipla":   // iki kol V
                    IK(SU, SE, SH, sR + up * L * .82f + ri * L * .42f + fw * L * .05f, ri - fw * .3f);
                    IK(LU, LE, LH, sL + up * L * .82f - ri * L * .42f + fw * L * .05f, -ri - fw * .3f); break;
                case "el_salla": case "selam":   // sol el yukarıda sallanır, raket yanda; selam: raket de hafif kalkar
                    IK(LU, LE, LH, sL + up * L * .8f - ri * L * (.3f + .16f * Mathf.Sin(t * 10) * salla) + fw * L * .12f, -ri - fw * .3f);
                    if (id == "selam") IK(SU, SE, SH, sR + up * L * .55f + ri * L * .45f + fw * L * .15f, ri - fw * .3f); break;
                case "alkis":   // sol el raketin teline vurur (göğüs önünde)
                    { Vector3 orta = (sR + sL) * .5f + fw * L * .55f - up * L * .1f; float ac = L * (.1f + .1f * (1 + s) * .5f);
                      IK(SU, SE, SH, orta + ri * ac, -up + ri); IK(LU, LE, LH, orta - ri * ac, -up - ri); } break;
                case "egil":     // reverans: raketli kol göğse, sol kol yanda
                    IK(SU, SE, SH, (sR + sL) * .5f + fw * L * .3f - up * L * .15f, -up + ri); break;
                case "iyi_oyun": // raket rakibe doğru kalkar (fileden selam)
                    IK(SU, SE, SH, sR + fw * L * .7f + up * L * .45f + ri * L * .1f, ri - up * .3f); break;
            }
            if (w < .999f) for (int i = 0; i < kem.Length; i++) if (kem[i] != null) kem[i].localRotation = Quaternion.Slerp(sevKayit[i], kem[i].localRotation, w);
        }

        // Kalça–omuz ayrışması (2026-10-27, ekran/kosu): hazırlıkta koşarken gövde fileye döner (Athlete.donus), bacaklar
        // gidişte kalır (Athlete.donusAyak, ayak işi klibi o çerçevede seçildi). Fark leğene verilir, omurganın ilk iki
        // kemiğinde geri alınır: göğüs, kollar ve raket vuruş klibinin pozunda, bacaklar gidiş yönünde. Ağırlık bacakların
        // ayak işinden gelen payı (ayak işi klibi gösteriliyorsa 1, vuruş klibinde alt katman ağırlığı). Omurga ya da leğen
        // bulunamazsa (ya da omurga leğenin çocuğu değilse) hiçbir şey yapılmaz (bütün beden dönerdi).
        public float TaniKalca;
        void KalcaDonus(Athlete o)
        {
            TaniKalca = 0;
            if (hakem || kHip == null || kOmurga.Count == 0 || !kOmurga[0].IsChildOf(kHip)) return;   // omurga leğenin altında değilse geri alınamaz
            float w = cur != null && Match.ADIM_BOY.ContainsKey(cur) ? 1 : altKlip != null ? altW : 0;
            float d = (o.donusAyak - o.donus) * w; if (Mathf.Abs(d) < .05f) return;
            Vector3 up = yaw.up; kHip.rotation = Quaternion.AngleAxis(d, up) * kHip.rotation;
            int n = Mathf.Min(2, kOmurga.Count); for (int i = 0; i < n; i++) kOmurga[i].rotation = Quaternion.AngleAxis(-d / n, up) * kOmurga[i].rotation;
            TaniKalca = d;
        }

        // Uzanma katmanı (2026-10-27 gece; koordinatör kararı, ekran turu ölçümü: temas karesinde raket kafası topa 0,6–1,5 m
        // uzak, "kırık" okunuyordu). Vuruş menzili Core'da (ReachInfo 1,15–1,45 m, hamle 1,75–2,2 m) klibin raket yayından geniş;
        // kural değişmez, kol yetişir. Sağ kol iki kemik IK (omuz → dirsek → bilek) + omurgada küçük eğim; el dünya dönüşünü
        // korur, böylece raket kafası elin ötelemesi kadar kayar ve temas noktasına KAFA oturur (el değil).
        //  - Temastan önce: hedef Core'un uçuş tahmini (icpt), kalan süre τ = prepT. Kayma = hedef − (vuruş karesindeki kafa,
        //    gövdenin τ sonraki tahmini konumunda); ağırlık son UZAN_GIR sn'de 0→1 (smoothstep). Geri salınım pozu bozulmaz,
        //    yalnız bütün kol hedefe doğru ötelenir. Tahmin geçerse (vuruş olmadı) UZAN_CIK içinde söner.
        //  - Temasta: hedef gerçek temas noktası (Athlete.tm*, StartFlight); ilk görünen karede kayma = hedef − kafa, sonra
        //    sabit kayma UZAN_CIK sn'de söner (takip klibin kendi yayıyla sürer). Servis: kayma yok (top elden, poz servis klibi).
        //  - Yetişmezse: kol boyunu aşan pay UZAN_PAY0'dan UZAN_PAY1'e dek ağırlığı sıfıra indirir (gergin kol takılıp kalmaz).
        // Durum yok sayılır: ağırlık τ'nun ve temastan geçen sürenin fonksiyonu; tek bellek temas karesinde ölçülen kayma.
        const float UZAN_GIR = .15f, UZAN_CIK = .1f, UZAN_EGIM = 20f, UZAN_EGIM_PAY = .6f, UZAN_PAY0 = .45f, UZAN_PAY1 = .8f,
            UZAN_KALCA = .45f, UZAN_KALCA_MAX = .2f, UZAN_BILEK = 40f;
        Vector3? hazirKafa; int uzanN = -1; float uzanS0; Vector3 uzanD;
        readonly Dictionary<string, Vector3> vurusKafa = new Dictionary<string, Vector3>();
        public float TaniUzanW, TaniUzanOnce = -1, TaniUzanSonra = -1, TaniDirsek, TaniUzanPay;
        string Kim { get { return isMe ? "me" : "op"; } }
        bool UzanHazir(Athlete o, Match M)
        {
            return !hakem && kafa != null && o.sw <= 0 && !float.IsNaN(o.prepT) && o.prepT < UZAN_GIR && o.prepT > -UZAN_CIK
                && M.icpt != null && M.icpt.who == Kim && M.s != null && !M.s.dead && M.s.from != Kim && cur != null && Match.CLIP_HIT.ContainsKey(cur);
        }
        // Klibin vuruş karesinde raket kafasının yaw-yerel konumu (klip başına bir kez örneklenir; ardından ana örnekleme ezer).
        Vector3? KafaVurusta(string clip)
        {
            Vector3 v; if (vurusKafa.TryGetValue(clip, out v)) return v;
            float hit; if (anim == null || D(clip) == null || !Match.CLIP_HIT.TryGetValue(clip, out hit)) return null;
            Sample(clip, hit, null, 0, 1); v = yaw.InverseTransformPoint(kafa.position); vurusKafa[clip] = v; return v;
        }
        // Hazırlık tutuşu canlı (2026-10-27 gece, ölçüm: hazırlık pozu tek karede ~0,5 sn donuk, "takıldı" okunuyordu): tutuş
        // karesinde omurgada küçük nefes/yaylanma (öne-arkaya 1,5°, kupa burgusu 1°, 1,6 Hz). Genlik hazırlık ağırlığıyla gelir,
        // ileri salınım penceresinin (SALINIM) 0,1 sn öncesinden söner: salınım ve temas pozuna dokunmaz. Saat Core'dan
        // (M.clock): durum yok, 60/120 Hz aynı. Hareketi azaltta kapalı.
        void HazirNefes(Athlete o, Match M)
        {
            if (hakem || M.reduce || kOmurga.Count == 0 || o.sw > 0 || !(o.prepA > .08f) || o.prepClip == null || cur == null || !Match.CLIP_HIT.ContainsKey(cur)) return;
            float sal; if (!Match.SALINIM.TryGetValue(cur, out sal)) sal = Match.SALINIM_V;
            float g = float.IsNaN(o.prepT) ? 1 : Mathf.SmoothStep(0, 1, (o.prepT - sal - .05f) / .1f), A = Mathf.Min(1, o.prepA) * g; if (A < 1e-3f) return;
            float f = M.clock * 1.6f * 2 * Mathf.PI + (isMe ? 0 : 1.7f);
            var t = kOmurga[0]; t.rotation = Quaternion.AngleAxis(1.5f * A * Mathf.Sin(f), yaw.right) * Quaternion.AngleAxis(1f * A * Mathf.Sin(f * .5f), yaw.up) * t.rotation;
        }
        // 1. adım (ana örneklemenin hemen ardından, ayak kilidinden önce): kayma D ve ağırlık w; kalça kaymanın yatay payının
        // UZAN_KALCA'sı kadar (en çok UZAN_KALCA_MAX) hedefe, hedef alçaksa aşağı. Ayaklar AyakKilit'te kalça kaymasını geri
        // alan IK ile yerinde kalır (kalça önden gider, ayak basılı). Kol, omurga ve bilek 2. adımda (UzanKol).
        bool uzanVar; float uzanW; Vector3 uzanHedef, uzanKafaHedef, kalcaKay;
        void Uzan(Athlete o, Match M, float j)
        {
            uzanVar = false; kalcaKay = Vector3.zero;
            TaniUzanW = 0; TaniUzanOnce = TaniUzanSonra = -1; TaniDirsek = 0; TaniUzanPay = 0;
            if (hakem || kafa == null || model == null) return;
            if (kKol == null) ZincirKur();
            if (kKol[3] == null || kKol[4] == null || kKol[5] == null) return;
            Vector3 hedef, D; float w;
            bool servis = M.s != null && M.s.serve && M.s.from == Kim;
            if (o.sw > 0 && o.tmN > 0)
            {
                if (servis) return;
                float sig = (1 - o.sw) / Mathf.Max(.01f, o.swHiz); hedef = Kit.W(o.tmX, o.tmY, o.tmZ);
                if (o.tmN != uzanN) { uzanN = o.tmN; uzanS0 = sig; uzanD = hedef - kafa.position; TaniUzanOnce = uzanD.magnitude; }
                w = 1 - Mathf.SmoothStep(0, 1, (sig - uzanS0) / UZAN_CIK); D = uzanD;
            }
            else if (hazirKafa.HasValue && UzanHazir(o, M))
            {
                float tau = o.prepT; var I = M.icpt; hedef = Kit.W(I.x, I.y, I.z);
                w = tau >= 0 ? Mathf.SmoothStep(0, 1, 1 - tau / UZAN_GIR) : 1 - Mathf.SmoothStep(0, 1, -tau / UZAN_CIK);
                float t = Mathf.Max(0, tau);   // gövdenin temastaki tahmini yeri: yumuşatılmış hızla τ ileri
                Vector3 kayma = Kit.W(o.x + o.svx * t, o.y + o.svy * t, 0) + Vector3.up * j - transform.position;
                D = hedef - (yaw.TransformPoint(hazirKafa.Value) + kayma);
            }
            else return;
            if (w <= 1e-3f || D.sqrMagnitude < 1e-6f) return;
            uzanVar = true; uzanW = w; uzanHedef = hedef; uzanKafaHedef = kafa.position + D * w;
            if (kHip != null)
            {
                Vector3 y = new Vector3(D.x, 0, D.z) * (w * UZAN_KALCA); if (y.magnitude > UZAN_KALCA_MAX) y = y.normalized * UZAN_KALCA_MAX;
                kalcaKay = y + Vector3.up * Mathf.Max(-UZAN_KALCA_MAX, Mathf.Min(0, D.y * w * UZAN_KALCA));
                kHip.position += kalcaKay;
            }
        }
        // 2. adım: omurga eğimi (gereken açının UZAN_EGIM_PAY'ı, en çok UZAN_EGIM°), kol IK (el dünya dönüşünü korur), kalan
        // aralık için bilek: raket kafası el çevresinde hedefe döner (en çok UZAN_BILEK°; vuruşta bilek kırılması budur).
        void UzanKol()
        {
            if (!uzanVar) return;
            Transform S = kKol[3], E = kKol[4], H = kKol[5]; float w = uzanW; Vector3 kHedef = uzanKafaHedef;
            if (kOmurga.Count > 0)
            {
                Vector3 g = kOmurga[0].position, a = S.position - g, b = a + (kHedef - kafa.position); Vector3 ek = Vector3.Cross(a, b);
                float th = Mathf.Min(Vector3.Angle(a, b) * UZAN_EGIM_PAY, UZAN_EGIM);
                if (ek.sqrMagnitude > 1e-8f && th > .01f) { int n = Mathf.Min(2, kOmurga.Count); for (int i = 0; i < n; i++) kOmurga[i].rotation = Quaternion.AngleAxis(th / n, ek.normalized) * kOmurga[i].rotation; }
            }
            Vector3 H1 = H.position, Hd = H1 + (kHedef - kafa.position);
            float la = (E.position - S.position).magnitude, lb = (H.position - E.position).magnitude, pay = (Hd - S.position).magnitude - (la + lb);
            TaniUzanPay = pay;
            float f = pay > UZAN_PAY0 ? 1 - Mathf.SmoothStep(0, 1, (pay - UZAN_PAY0) / (UZAN_PAY1 - UZAN_PAY0)) : 1;   // yetişilmez: söner
            Hd = H1 + (Hd - H1) * f; kHedef = kafa.position + (kHedef - kafa.position) * f;
            Vector3 orta = (S.position + H1) * .5f, kutup = E.position - orta, ekO = (H1 - S.position).normalized;
            var rot = H.rotation;
            IK(S, E, H, Hd, kutup.sqrMagnitude > 1e-6f ? kutup : -yaw.up);
            H.rotation = rot;
            {   // bilek
                Vector3 v = kafa.position - H.position, v2 = kHedef - H.position; Vector3 ek = Vector3.Cross(v, v2);
                float aci = Mathf.Min(Vector3.Angle(v, v2), UZAN_BILEK);
                if (ek.sqrMagnitude > 1e-8f && aci > .01f) H.rotation = Quaternion.AngleAxis(aci, ek.normalized) * H.rotation;
            }
            // Dirsek dönmesi: kutup yönünün IK öncesi ve sonrası (omuz–el eksenine dik düzlemde) açısı; 90°'den büyükse ters büküm.
            Vector3 ekS = (H.position - S.position).normalized, k1 = Vector3.ProjectOnPlane(E.position - (S.position + H.position) * .5f, ekS), k0p = Vector3.ProjectOnPlane(kutup, ekO);
            TaniDirsek = k1.sqrMagnitude > 1e-8f && k0p.sqrMagnitude > 1e-8f ? Vector3.Angle(k0p, k1) : 0;
            TaniUzanW = w * f; TaniUzanSonra = (uzanHedef - kafa.position).magnitude;
        }

        // Kısa/flick servis duruşunda eller çeneden aşağıda (2026-10-28, ekran turu r44 model_mac_*): klipler ortak iskelette
        // göğüs hizasında tutuşla pişirilir; tıknaz oyuncakta (kısa gövde, büyük baş) aynı açılar sağ eli çeneye, raket sapını
        // yüzün önüne getiriyordu. El, sağ omuzdan SERVIS_EL_PAY (dünya m) aşağıya inene kadar iki el birlikte (tüy sol elde,
        // raket–top ilişkisi korunur) iki kemik IK ile indirilir; el dünya dönüşleri korunur. Gerçekçi ve Sade'de el zaten
        // aşağıda (inme 0). Yalnız görünüm: servis temas noktası ve süresi Core'da.
        const float SERVIS_EL_PAY = .12f; public float TaniServisIn;
        void ServisEller()
        {
            TaniServisIn = 0;
            if (hakem || kKol == null || (cur != "serve_bh" && cur != "serve_flick")) return;
            Transform rS = kKol[3], rE = kKol[4], rH = kKol[5]; if (rS == null || rE == null || rH == null) return;
            float d = rH.position.y - (rS.position.y - SERVIS_EL_PAY); if (d <= 1e-3f) return;
            Vector3 asagi = -yaw.up * d;
            for (int s = 1; s >= 0; s--)
            {
                Transform S = kKol[s * 3], E = kKol[s * 3 + 1], H = kKol[s * 3 + 2]; if (S == null || E == null || H == null) continue;
                Vector3 orta = (S.position + H.position) * .5f, kutup = E.position - orta; var rot = H.rotation;
                IK(S, E, H, H.position + asagi, kutup.sqrMagnitude > 1e-6f ? kutup : -yaw.up); H.rotation = rot;
            }
            TaniServisIn = d;
        }

        // İmza vuruşu hazırlığı (2026-10-26, Core/Imza; tasarım §3.3 kural 1): raket kafasında imzanın renginde ışık küresi +
        // nokta ışık (poz Match.Prep'te tutulur, ses Sfx.Imza'da: anlam yalnız renkle verilmez). Hareketi azaltta ışık sabit
        // kalır; parlama (ilk an büyüyüp küçülme) ve nabız kalkar. Mermi drive'da gövde 12 cm eğilir (yere eğilir).
        Transform imzaKure; Light imzaIsik; Material imzaMat; float imzaA, imzaYas; string imzaRenkId;
        void ImzaIsik(Athlete o, bool reduce, float dt)
        {
            bool on = !hakem && o.imzaT > 0 && o.imzaId != null;
            if (!on && imzaKure == null) return;
            if (imzaKure == null)
            {
                imzaMat = Kit.Additive(Color.white);
                var g = Kit.Prim(PrimitiveType.Sphere, "imza_isik", kafa != null ? kafa : (model != null ? model : transform), kafa != null ? Vector3.zero : new Vector3(.35f, 1.5f, 0), Vector3.one * .32f, imzaMat);
                var col = g.GetComponent<Collider>(); if (col != null) Destroy(col);
                var r = g.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                imzaKure = g.transform; if (kafa != null) imzaKure.localScale = Vector3.one * (.32f / Mathf.Max(.01f, kafa.lossyScale.x));
                imzaIsik = g.AddComponent<Light>(); imzaIsik.type = LightType.Point; imzaIsik.range = 2.2f; imzaIsik.shadows = LightShadows.None;
            }
            if (on && imzaRenkId != o.imzaId)
            {
                imzaRenkId = o.imzaId; var b = KariyerVeri.Imza(o.imzaId); var c = Kit.Hex(b != null ? b.renk : "#ffffff"); imzaMat.color = c; imzaIsik.color = c; imzaYas = 0;
            }
            imzaA = Mathf.MoveTowards(imzaA, on ? 1 : 0, dt / .08f); imzaYas += dt;
            bool gor = imzaA > 0.001f; imzaKure.gameObject.SetActive(gor); if (!gor) { imzaRenkId = null; return; }
            float puls = reduce ? 1 : 1 + .18f * Mathf.Sin(imzaYas * 18) + Mathf.Max(0, .6f * (1 - imzaYas / .15f));   // nabız + ilk parlama
            var cc = imzaMat.color; cc.a = .9f * imzaA; imzaMat.color = cc;
            float s0 = kafa != null ? .32f / Mathf.Max(.01f, kafa.lossyScale.x) : .32f; imzaKure.localScale = Vector3.one * s0 * puls * imzaA;
            imzaIsik.intensity = 2.2f * imzaA * (reduce ? 1 : puls);
            if (o.imzaId == "mermi" && on) transform.position += Vector3.down * .12f * imzaA;
        }

        // Ayak kilidi (2026-10-26 gece, build 40 incelemesi; 3d-animator "pin the feet"). Faz yolla ilerlese de yerdeki ayak
        // kayıyordu: (1) kliplerde basan ayağın geriye gidişi döngü boyunca sabit değil ve adım ölçüsünden ~%10 kısa (Blender'da
        // FBX'ten ölçüm: fwd sol 0,81 / sağ 0,69 birim/döngü, json 0,82), (2) boşta, hazırlık ve vuruş pozları yerinde durur,
        // gövde ise 0,3–1 m/sn kayabilir, (3) klip geçişlerinde harmanlama ayağı sürükler. Unity izinde (ekran turu r31) ayak
        // işindeki yerdeki ayak gövde hızının %20–50'siyle kayıyordu. Çözüm, spor oyunlarının olağanı: ayak yere değdiği karede
        // dünya konumu kilitlenir, yerde kaldıkça iki kemik IK (uyluk–baldır) bileği orada tutar; ayak kalkınca ya da kilit
        // KILIT_MAX'tan fazla gerilirse (bacak gerilip diz kilitlenmesin) bırakılır ve fark KILIT_BIRAK hızıyla söner (pop yok).
        // Bileğin dünya dönüşü animasyondaki gibi kalır (taban açısı klipten). Yerde sayılma: bilek, boşta pozundaki
        // yüksekliğinin KILIT_Y kadar üstüne kadar. Havadayken (sıçrama, split hop) bilek yükseldiği için kendiliğinden kapalı.
        // Oyun kuralı değil; vuruş menzili Core'da. Kemikler bulunamazsa hiçbir şey yapılmaz.
        const float KILIT_Y = .025f, KILIT_MAX = .28f, KILIT_BIRAK = 18f;
        float[] bilek0; readonly Vector3[] kilitP = new Vector3[2], kilitO = new Vector3[2]; readonly bool[] kilitli = new bool[2];
        void AyakKilit(float dt)
        {
            if (hakem || model == null || kThigh.Count == 0) return;
            if (kBacak == null) ZincirKur();
            if (bilek0 == null) return;
            for (int i = 0; i < 2; i++)
            {
                Transform th = kBacak[i * 3], kn = kBacak[i * 3 + 1], ft = kBacak[i * 3 + 2]; if (th == null || kn == null || ft == null) continue;
                Vector3 A = ft.position - kalcaKay; bool yerde = A.y < bilek0[i] + KILIT_Y;   // A: kalça kaymasından önceki (klibin) ayak yeri
                if (yerde && !kilitli[i] && kilitO[i].sqrMagnitude < 1e-6f) { kilitli[i] = true; kilitP[i] = A; }
                else if (yerde && !kilitli[i]) { kilitli[i] = true; kilitP[i] = A + kilitO[i]; }   // bırakılış sönmeden yeniden bastı: kaldığı yerden
                Vector3 O = Vector3.zero;
                if (kilitli[i])
                {
                    O = new Vector3(kilitP[i].x - A.x, 0, kilitP[i].z - A.z);
                    // Gerilince bırakılır, fark söner; yarım metreden büyükse gövde ışınlandı (servis dizilişi): ayak hemen yerine.
                    if (!yerde || O.magnitude > KILIT_MAX) { kilitli[i] = false; kilitO[i] = O.magnitude > .5f ? Vector3.zero : O; O = kilitO[i]; }
                    else kilitO[i] = O;
                }
                else if (!kilitli[i]) { kilitO[i] *= Mathf.Exp(-dt * KILIT_BIRAK); if (kilitO[i].sqrMagnitude < 1e-6f) kilitO[i] = Vector3.zero; O = kilitO[i]; }
                if (O.sqrMagnitude < 1e-8f && kalcaKay.sqrMagnitude < 1e-8f) continue;
                var rot = ft.rotation; Vector3 kutup = kn.position - (th.position + A) * .5f;
                IK(th, kn, ft, A + O, kutup.sqrMagnitude > 1e-6f ? kutup : yaw.forward);
                ft.rotation = rot;
            }
        }
        public float TaniKilit { get { return (kilitli[0] ? 1 : 0) + (kilitli[1] ? 2 : 0); } }

        // Tanı (yalnız ekran turu, EkranTuru animasyon izi; 2026-10-26 gece): gösterilen klip, alt katman ağırlığı, iki ayak
        // bileğinin ve sağ elin dünya konumu, raket sapının ele uzaklığı. Ayak kayması = yerdeki ayağın kareler arası yatay
        // hızı; raket kayması = sapın el kemiğinden uzaklığı (rijit parça elle birlikte gitmeli). Oyunda çağıran yok.
        public string TaniKlip { get { return cur; } }
        // Klip geçişi tanısı (2026-10-27 performans turu 2; ekran turu "perf_gecis"): değişim, geçiş sürerken değişim, tek karede
        // düşen poz ağırlığının toplamı ve %25'i aşan düşüş (pop) sayısı. Oyunda okuyan yok.
        public int TaniDegisim, TaniAra, TaniPop; public float TaniDusen;
        public float TaniAlt { get { return altW; } }
        public float TaniFaz { get { return lastPh; } }
        public Vector3 TaniKafa { get { return kafa != null ? kafa.position : Vector3.zero; } }   // raket kafası (tel yüzeyinin merkezi)
        Transform raketSap;
        public bool TaniAyak(out Vector3 sol, out Vector3 sag, out Vector3 el, out float sapEl)
        {
            sol = sag = el = Vector3.zero; sapEl = -1;
            if (model == null || kThigh.Count == 0) return false;
            if (kBacak == null) ZincirKur();
            if (raketSap == null) foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("raket_sap")) { raketSap = t; break; }
            if (kBacak[2] == null || kBacak[5] == null) return false;
            sol = kBacak[2].position; sag = kBacak[5].position;
            var h = kKol[5]; if (h != null) { el = h.position; if (raketSap != null) { var r = raketSap.GetComponent<Renderer>(); var c = r != null ? r.bounds.ClosestPoint(h.position) : raketSap.position; sapEl = (c - h.position).magnitude; } }
            return true;
        }

        // File düzlemi (2026-10-26, yasin: "objelerde iç içe geçme olmasın"): Core gövde kökünü fileden .25 m uzak tutar
        // (Match.Engel), ama hamle/file pozlarında ön ayak kökten ~1,5 m öne uzanıyor (oyun ölçeğinde 1,75×) ve fileye
        // ~1,5 m'den yakın vuruşta ayak/diz file düzlemini geçiyordu. Gerçekte file düzlemini gövdeyle geçmek faul; burada
        // örneklenen pozun deri kemiklerinden (raket rijit parça, sayılmaz: takip vuruşunda raketin fileyi aşması serbest)
        // fileye en yakını bulunur ve figür PAY kadar geride kalacak biçimde tümden geri itilir. Kök Core'da kalır (vuruş
        // menzili değişmez); yalnız görüntü kayar ve kayma pozun en ileri anında ~0–20 cm'dir.
        const float FILE_PAY_G = .05f; Transform[] deriKemik;
        void FileItin()
        {
            if (hakem || model == null) return;
            if (deriKemik == null)
            {
                var hs = new HashSet<Transform>();
                foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) foreach (var b in r.bones) if (b != null) hs.Add(b);
                deriKemik = new Transform[hs.Count]; hs.CopyTo(deriKemik);
            }
            if (deriKemik.Length == 0) return;
            // dünya z = NET − y: ben z < 0 tarafındayım, rakip z > 0
            float uc = isMe ? -1e9f : 1e9f; foreach (var b in deriKemik) { float z = b.position.z; uc = isMe ? Mathf.Max(uc, z) : Mathf.Min(uc, z); }
            float asim = isMe ? uc + FILE_PAY_G : FILE_PAY_G - uc;   // > 0: düzlemi PAY içinde geçen kısım
            if (asim > 0) transform.position += new Vector3(0, 0, isMe ? -asim : asim);
        }
    }
}
