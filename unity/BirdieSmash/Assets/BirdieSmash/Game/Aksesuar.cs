// Şapka ve yüz aksesuarları (2026-10-30, Soyunma › Görünüm; yasin onayı, öneri artifact 2dg4HEiYCrqaPdwzHKP12S).
// Neden FBX parçası değil: aksesuarlar dört modelin hepsinde (Oyuncak 3B, Gerçekçi, Sade, Tüycük) görünmeli; her model için
// Blender'da ayrı parça oturtmak dört FBX'i yeniden üretmek demek (oyuncak tipi başına ~30 dk). Burada parçalar basit
// geometrilerden (küre, silindir, koni, halka, tüp) Unity'de kurulur ve kafa kemiğine bağlanır; ölçü her modelde aynı
// yoldan gelir: kafa bandı halkası (__bant, dört FBX'te de var, gizliyken de ağı durur) baş çevresini verir. Baş çerçevesi:
// merkez = bant halkasının ortası, yarıçap = halkanın ortalama yatay yarıçapı, yukarı = figürün dikeyi, ileri = bas__ →
// goz__ işareti. Parçalar bu çerçevede birim yarıçaplı başa göre yazılır (alın bandı y=0, tepe ~+0,95, göz ~-0,38).
// Yalnız görünüş: maçta hiçbir hesaba girmez (Core/Kozmetik güç alanını yasaklar).
using System.Collections.Generic;
using UnityEngine;
using BirdieSmash.Core;

namespace BirdieSmash.Game
{
    public static class Aksesuar
    {
        // Şapkanın altında hacimli saç kısa kesime döner (saç şapkanın içinden çıkmasın; Oyuncak 3B'de saç parçası seçilir).
        public static readonly HashSet<string> HACIMLI = new HashSet<string> { "afro", "topuz", "anime", "mohikan", "rasta", "kirpi", "ananas", "uzay", "pompadur", "atkuyrugu" };
        public static bool Var(string id) { return !string.IsNullOrEmpty(id) && id != "yok"; }

        // Baş çerçevesini ölçer ve aksesuarları kurar. Dönüş: kök (kafa kemiğinin çocuğu) ya da null.
        // 2026-10-30 r65: BakeMesh yolu baş merkezini modelin ölçeksiz yerel koordinatında verdi (oyuncak: merkez y .83, baş
        // gerçekte ~1,5 m; gerçekçi 1,01): aksesuar gövdenin içinde kaldı, hiçbir karede görünmedi. Şimdi çapa bas__isaret
        // (VekilDuzelt'ten sonra baş kemiğine oturan, kalibrasyonun kullandığı işaret) ve cetvel goz__isaret: Blender'da tam
        // .2 birim önde, aradaki dünya uzaklığı / .2 = model biriminin dünya karşılığı. Bant halkasının işarete göre yeri ve
        // yarıçapı _clips.json "bant" alanından (Blender ölçümü). Alan yoksa eski BakeMesh yolu yedek.
        public static Transform Kur(Transform model, Transform kafa, Vector3 dikey, PlayerView.BantMeta bant, string sapka, string yuz, Color forma, Color? sac)
        {
            if (model == null || kafa == null || (!Var(sapka) && !Var(yuz))) return null;
            Transform bas = null, goz = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) { if (t.name.StartsWith("bas__")) bas = t; else if (t.name.StartsWith("goz__")) goz = t; }
            Vector3 ileri = bas != null && goz != null ? Vector3.ProjectOnPlane(goz.position - bas.position, dikey) : Vector3.zero;
            if (ileri.sqrMagnitude < 1e-8f) ileri = Vector3.ProjectOnPlane(model.forward, dikey);
            Vector3 c; float r;
            if (bant != null && bas != null && goz != null)
            {
                float s = (goz.position - bas.position).magnitude / .2f;
                c = bas.position + dikey.normalized * bant.dz * s + ileri.normalized * bant.df * s; r = bant.r * s;
            }
            else if (!BantHalkasi(model, dikey, out c, out r)) return null;
            // Elipsoit (BantMeta yan/ust/on/arka): birim baş (yan x, tepe y, ön z) ölçülen başa gerilir, merkez öne/arkaya kayar.
            float yan = 1, ust = 1, on = 1, arka = 1;
            if (bant != null && bant.yan > 0 && bant.ust > 0 && bant.on > 0 && bant.arka > 0) { yan = bant.yan; ust = bant.ust; on = bant.on; arka = bant.arka; }
            c += ileri.normalized * r * (on - arka) * .5f;
            var kok = new GameObject("aksesuar").transform; kok.SetParent(kafa, true);
            kok.position = c; kok.rotation = Quaternion.LookRotation(ileri.normalized, dikey.normalized);
            float ls = kafa.lossyScale.x; kok.localScale = new Vector3(yan, ust, (on + arka) * .5f) * (r / Mathf.Max(1e-5f, ls));
            if (Var(sapka)) Sapka(kok, sapka, forma);
            if (Var(yuz)) Yuz(kok, yuz, forma, on > 1);   // elipsoit oranı yalnız Oyuncak'larda: tombul yüz, öne çıkan dudak
            foreach (var rd in kok.GetComponentsInChildren<Renderer>()) rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return kok;
        }

        // Bant halkası: deri ağı o anki pozla pişirilir (BakeMesh), dünya noktalarına çevrilir; merkez ortalama, yarıçap dikeye
        // dik düzlemdeki ortalama uzaklık. Bant gizli olsa da (bant takılı değil) ağ okunur.
        static bool BantHalkasi(Transform model, Vector3 dikey, out Vector3 c, out float r)
        {
            c = Vector3.zero; r = 0; var pts = new List<Vector3>();
            foreach (var rd in model.GetComponentsInChildren<Renderer>(true))
            {
                if (PlayerView.Etiket(rd.gameObject.name) != "bant") continue;
                var t = rd.transform; Mesh m = null;
                var sk = rd as SkinnedMeshRenderer;
                if (sk != null && sk.sharedMesh != null) { m = new Mesh(); sk.BakeMesh(m, true); var M = Matrix4x4.TRS(t.position, t.rotation, Vector3.one); foreach (var v in m.vertices) pts.Add(M.MultiplyPoint3x4(v)); Object.Destroy(m); }
                else { var mf = rd.GetComponent<MeshFilter>(); if (mf != null && mf.sharedMesh != null) foreach (var v in mf.sharedMesh.vertices) pts.Add(t.TransformPoint(v)); }
            }
            if (pts.Count < 8) return false;
            foreach (var p in pts) c += p; c /= pts.Count;
            foreach (var p in pts) r += Vector3.ProjectOnPlane(p - c, dikey).magnitude; r /= pts.Count;
            return r > 1e-4f;
        }

        static Material Mat(Color c, float parlak = .25f) { return Kit.Lit(c, parlak); }
        static GameObject P(PrimitiveType t, Transform k, Vector3 p, Vector3 s, Material m, Vector3 aci = default(Vector3))
        {
            var g = Kit.Prim(t, "aks", k, p, s, m); g.transform.localRotation = Quaternion.Euler(aci); return g;
        }
        static GameObject Ag(Transform k, Mesh mesh, Material m, Vector3 p, Vector3 aci, Vector3 s)
        {
            var g = Kit.MeshObj("aks", k, mesh, m, true); g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(aci); g.transform.localScale = s; return g;
        }

        static void Sapka(Transform k, string id, Color forma)
        {
            if (HazirAg(k, id)) return;
            var fm = Mat(forma, .3f); var koyu = Mat(Color.Lerp(forma, Color.black, .45f), .3f);
            switch (id)
            {
                case "kep": case "kep_ters":
                    {
                        // tepe kubbesi bandın hemen üstünde biter (bant düzleminde yarıçap 0,95 < baş); siper öne ya da (ters) arkaya
                        float yon = id == "kep" ? 1 : -1;
                        P(PrimitiveType.Sphere, k, new Vector3(0, .32f, 0), new Vector3(2.16f, 1.34f, 2.16f), fm);
                        P(PrimitiveType.Cylinder, k, new Vector3(0, .26f, .92f * yon), new Vector3(1.25f, .025f, 1.05f), koyu, new Vector3(-8 * yon, 0, 0));
                        P(PrimitiveType.Sphere, k, new Vector3(0, .98f, 0), Vector3.one * .16f, koyu);
                        break;
                    }
                case "bere":
                    P(PrimitiveType.Sphere, k, new Vector3(0, .26f, -.02f), new Vector3(2.2f, 1.55f, 2.2f), fm);
                    P(PrimitiveType.Cylinder, k, new Vector3(0, .1f, -.02f), new Vector3(2.16f, .16f, 2.16f), koyu);
                    P(PrimitiveType.Sphere, k, new Vector3(0, 1.08f, -.06f), Vector3.one * .42f, Mat(Color.white, .1f));
                    break;
                case "buyucu":
                    {
                        var mor = Mat(new Color(.33f, .2f, .6f), .3f); var sari = Mat(new Color(1, .82f, .3f), .5f);
                        P(PrimitiveType.Cylinder, k, new Vector3(0, .3f, 0), new Vector3(2.9f, .025f, 2.9f), mor);
                        Ag(k, Koni(20), mor, new Vector3(0, .28f, 0), new Vector3(-14, 0, 0), new Vector3(1.0f, 2.3f, 1.0f));
                        P(PrimitiveType.Cylinder, k, new Vector3(0, .36f, 0), new Vector3(2.02f, .08f, 2.02f), sari);
                        P(PrimitiveType.Sphere, k, new Vector3(.45f, .95f, .55f), Vector3.one * .16f, sari);
                        P(PrimitiveType.Sphere, k, new Vector3(-.35f, 1.35f, .2f), Vector3.one * .12f, sari);
                        break;
                    }
                case "sovalye":
                    {
                        var metal = Mat(new Color(.72f, .75f, .8f), .85f); var siyah = Mat(new Color(.05f, .05f, .07f), .1f);
                        P(PrimitiveType.Sphere, k, new Vector3(0, .02f, 0), new Vector3(2.28f, 2.36f, 2.3f), metal);
                        P(PrimitiveType.Cube, k, new Vector3(0, -.36f, 1.1f), new Vector3(1.25f, .11f, .12f), siyah);
                        P(PrimitiveType.Cube, k, new Vector3(0, -.62f, 1.06f), new Vector3(.08f, .42f, .12f), siyah);
                        Ag(k, Tup(new[] { new Vector3(0, .98f, -.05f), new Vector3(0, 1.4f, -.35f), new Vector3(0, 1.45f, -.85f), new Vector3(0, 1.1f, -1.25f) }, new[] { .14f, .22f, .2f, .05f }, 8), fm, Vector3.zero, Vector3.zero, Vector3.one);
                        break;
                    }
                case "kulaklik":
                    {
                        var siyah = Mat(new Color(.1f, .1f, .12f), .5f);
                        var yay = new List<Vector3>(); var rs = new List<float>();
                        for (int i = 0; i <= 12; i++) { float a = Mathf.PI * i / 12; yay.Add(new Vector3(Mathf.Cos(a) * 1.06f, -.25f + Mathf.Sin(a) * 1.27f, -.05f)); rs.Add(.09f); }
                        Ag(k, Tup(yay.ToArray(), rs.ToArray(), 8), siyah, Vector3.zero, Vector3.zero, Vector3.one);
                        foreach (int sx in new[] { -1, 1 })
                        {
                            P(PrimitiveType.Cylinder, k, new Vector3(1.08f * sx, -.45f, -.05f), new Vector3(.62f, .13f, .62f), siyah, new Vector3(0, 0, 90));
                            P(PrimitiveType.Cylinder, k, new Vector3(1.2f * sx, -.45f, -.05f), new Vector3(.4f, .04f, .4f), fm, new Vector3(0, 0, 90));
                        }
                        break;
                    }
                case "tavsan":
                    {
                        var beyaz = Mat(new Color(.97f, .95f, .93f), .2f); var pembe = Mat(new Color(1, .65f, .74f), .2f);
                        P(PrimitiveType.Cylinder, k, new Vector3(0, .42f, -.02f), new Vector3(2.06f, .05f, 2.06f), beyaz);
                        foreach (int sx in new[] { -1, 1 })
                        {
                            P(PrimitiveType.Capsule, k, new Vector3(.38f * sx, 1.45f, -.05f), new Vector3(.38f, .85f, .2f), beyaz, new Vector3(0, 0, -12 * sx));
                            P(PrimitiveType.Capsule, k, new Vector3(.385f * sx, 1.47f, .02f), new Vector3(.2f, .66f, .1f), pembe, new Vector3(0, 0, -12 * sx));
                        }
                        break;
                    }
            }
        }

        static void Yuz(Transform k, string id, Color forma, bool oyuncak)
        {
            var siyah = Mat(new Color(.06f, .06f, .08f), .7f);
            switch (id)
            {
                case "gunes":
                    foreach (int sx in new[] { -1, 1 }) P(PrimitiveType.Cube, k, new Vector3(.34f * sx, -.4f, 1.0f), new Vector3(.5f, .3f, .06f), siyah, new Vector3(0, 14 * sx, 0));
                    P(PrimitiveType.Cube, k, new Vector3(0, -.36f, 1.04f), new Vector3(.3f, .06f, .05f), siyah);
                    foreach (int sx in new[] { -1, 1 }) P(PrimitiveType.Cube, k, new Vector3(.66f * sx, -.34f, .55f), new Vector3(.05f, .05f, .85f), siyah, new Vector3(0, -12 * sx, 0));
                    break;
                case "yuvarlak":
                    {
                        var cerceve = Mat(new Color(.55f, .38f, .18f), .6f);
                        foreach (int sx in new[] { -1, 1 }) Ag(k, Halka(24, 8, .16f, .032f), cerceve, new Vector3(.32f * sx, -.4f, 1.0f), new Vector3(0, 12 * sx, 0), Vector3.one);
                        P(PrimitiveType.Cube, k, new Vector3(0, -.36f, 1.04f), new Vector3(.18f, .04f, .04f), cerceve);
                        foreach (int sx in new[] { -1, 1 }) P(PrimitiveType.Cube, k, new Vector3(.64f * sx, -.36f, .55f), new Vector3(.04f, .04f, .85f), cerceve, new Vector3(0, -12 * sx, 0));
                        break;
                    }
                case "biyik":
                    {
                        var kahve = Mat(new Color(.22f, .13f, .07f), .15f);
                        // 2026-10-30 (ekran turu r70: bıyık ve boya yüzün içinde kalıyordu): dört Oyuncak FBX'inde yüz yüzeyi Blender'da
                        // birim başa göre ölçüldü (bpy, FBX'teki bas/goz işaretlerinden): burun ucu y −.78…−.84 z ≈1.08, dudak üstü x ±.18
                        // y −.9 z .98–1.04, yanak x .5 y −.62…−.74 z .81–.93. Bıyık dudak üstüne (yarıçap .08, arka yüzü yüzeyde), boya
                        // yanağın önüne. Gerçekçi'de yüz dar (yanak z ≈.68, dudak üstü ≈.85): eski bıyık yerinde kalır, boya içeri çekilir.
                        // Sade ve Tüycük ölçülemedi (yüz ayrı parça değil), Gerçekçi değerleriyle kalır.
                        float y = oyuncak ? -.9f : -.86f, z = oyuncak ? 1.06f : .94f;
                        foreach (int sx in new[] { -1, 1 }) P(PrimitiveType.Capsule, k, new Vector3(.2f * sx, y, z), new Vector3(.18f, .2f, .16f), kahve, new Vector3(0, 18 * sx, 72 * sx));
                        break;
                    }
                case "korsan":
                    P(PrimitiveType.Cylinder, k, new Vector3(-.33f, -.4f, .99f), new Vector3(.4f, .03f, .36f), siyah, new Vector3(90, -14, 0));
                    Ag(k, Halka(32, 6, 1.03f, .03f), siyah, new Vector3(0, -.05f, 0), new Vector3(-90 + 18, 0, 22), Vector3.one);
                    break;
                case "boya":
                    {
                        var c1 = Mat(forma, .1f); var c2 = Mat(Color.white, .1f); float z = oyuncak ? .92f : .74f;
                        foreach (int sx in new[] { -1, 1 })
                        {
                            P(PrimitiveType.Cube, k, new Vector3(.5f * sx, -.62f, z), new Vector3(.36f, .07f, .05f), c1, new Vector3(0, 32 * sx, 0));
                            P(PrimitiveType.Cube, k, new Vector3(.5f * sx, -.74f, z - .02f), new Vector3(.34f, .07f, .05f), c2, new Vector3(0, 32 * sx, 0));
                        }
                        break;
                    }
            }
        }

        // ---- Hazır 3B eşyalar (2026-11-01, yasin: "internette hazır bedava ürünleri kullanabilirsin"; karar: yalnız bedava) ----
        // KayKit Adventurers 1.0 ve Skeletons 1.0 (CC0) şapka ve pelerinleri, tool/karakter/kaykit_aksesuar.py ile birim
        // çerçevede Resources/karakter/aks/<id>.bytes olarak yazılır (biçim betiğin başında). Renk başına bir alt ağ, düz
        // malzeme: doku yok, sıkıştırma/mipmap paleti bulandırmaz, sahnedeki yordamsal aksesuarlarla aynı ışık.
        public class HazirVeri { public Mesh mesh; public Material[] mats; public int tur; public float altY; }
        static readonly Dictionary<string, HazirVeri> hazir = new Dictionary<string, HazirVeri>();
        public static HazirVeri Hazir(string id)
        {
            if (!Var(id)) return null;
            HazirVeri h; if (hazir.TryGetValue(id, out h)) return h;
            var ta = Resources.Load<TextAsset>("karakter/aks/" + id); h = null;
            if (ta != null)
            {
                try { h = Coz(ta.bytes); }
                catch (System.Exception e) { Debug.LogWarning("aksesuar " + id + ": " + e.Message); h = null; }
                Resources.UnloadAsset(ta);
            }
            if (h != null) Boya(h, Kozmetik.Bul(Kozmetik.SAPKA, id) ?? Kozmetik.Bul(Kozmetik.SIRT, id));
            hazir[id] = h; return h;
        }
        // Veri rengi (karakter.json "renk") en çok üçgenli alt ağın rengi olur; mistik öğede o alt ağ hafifçe kendinden ışır
        // (Standard _EMISSION, raket parlamasıyla aynı yol): gece renkli pelerin karanlık sahnede kaybolmasın, "büyülü" okunsun.
        static void Boya(HazirVeri h, KozmetikOge o)
        {
            if (o == null || h.mats.Length == 0) return;
            int ana = 0, enc = -1; for (int a = 0; a < h.mats.Length; a++) { int n = (int)h.mesh.GetIndexCount(a); if (n > enc) { enc = n; ana = a; } }
            if (o.renk != null) h.mats[ana].color = Kit.Hex(o.renk);
            if (o.nadir == "mistik") { var m = h.mats[ana]; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", m.color * .45f); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        }
        static HazirVeri Coz(byte[] b)
        {
            using (var r = new System.IO.BinaryReader(new System.IO.MemoryStream(b)))
            {
                if (new string(r.ReadChars(4)) != "BSA1") throw new System.Exception("biçim");
                var h = new HazirVeri { tur = r.ReadByte() }; int n = r.ReadUInt16();
                var v = new Vector3[n]; var nr = new Vector3[n]; float alt = 0;
                for (int i = 0; i < n; i++)
                {
                    v[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); nr[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    alt = Mathf.Min(alt, v[i].y);
                }
                int alt_n = r.ReadByte(); var tris = new List<int[]>(); var mats = new Material[alt_n];
                for (int a = 0; a < alt_n; a++)
                {
                    var c = new Color32(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte()); int tn = r.ReadUInt16(); var t = new int[tn * 3];
                    for (int i = 0; i < t.Length; i++) t[i] = r.ReadUInt16();
                    tris.Add(t); mats[a] = Mat(c, .2f);
                }
                // iki yüz: pelerinler tek düzlem, şapka içi de görünür olabilir (IkiYuz ile aynı kural, alt ağ başına)
                var vv = new List<Vector3>(v); var nn = new List<Vector3>(nr);
                for (int i = 0; i < n; i++) { vv.Add(v[i]); nn.Add(-nr[i]); }
                var m = new Mesh { name = "aks" }; m.SetVertices(vv); m.SetNormals(nn); m.subMeshCount = alt_n;
                for (int a = 0; a < alt_n; a++)
                {
                    var t = tris[a]; var o = new int[t.Length * 2];
                    for (int i = 0; i < t.Length; i += 3) { o[i] = t[i]; o[i + 1] = t[i + 1]; o[i + 2] = t[i + 2]; o[t.Length + i] = t[i] + n; o[t.Length + i + 1] = t[i + 2] + n; o[t.Length + i + 2] = t[i + 1] + n; }
                    m.SetTriangles(o, a);
                }
                m.RecalculateBounds(); h.mesh = m; h.mats = mats; h.altY = alt; return h;
            }
        }
        // PlayerView.Sol/Sag ile aynı ad kuralı (dört modelin kemik adları)
        static bool Sol(string n) { return n.EndsWith(".l") || n.EndsWith("_l") || n.Contains(" l ") || n.Contains("left"); }
        static bool Sag(string n) { return n.EndsWith(".r") || n.EndsWith("_r") || n.Contains(" r ") || n.Contains("right"); }
        static GameObject HazirObj(Transform k, HazirVeri h)
        {
            var g = Kit.MeshObj("aks", k, h.mesh, h.mats[0], true); g.GetComponent<MeshRenderer>().sharedMaterials = h.mats; return g;
        }
        static bool HazirAg(Transform k, string id)
        {
            var h = Hazir(id); if (h == null || h.tur != 0) return false; HazirObj(k, h); return true;
        }

        // Sırt eşyası (pelerin): göğüs kemiğine (boynun ebeveyni) bağlanır. Çerçeve: omuz çizgisi ortası (upperarm sol/sağ),
        // birim = omuz genişliği, z=0 sırt yüzeyi (omuz yüksekliğinde gövdenin en arka noktası; BakeMesh bir kez). KayKit
        // pelerini figürün topuğuna iner (3 baş boylu gövde); bizim modellerde koşuyu ve hamleyi kapatmasın diye boyu, alt ucu
        // omuz yüksekliğinin %45'inde (diz arkası) kalacak kadar kısaltılır; omuzluk gibi kısa parçalar olduğu gibi kalır.
        public static Transform Sirt(Transform model, Vector3 dikey, string id)
        {
            var h = Hazir(id); if (model == null || h == null || h.tur != 1) return null;
            Transform bas = null, goz = null, ol = null, or_ = null, boyun = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (t.name.StartsWith("bas__")) bas = t; else if (t.name.StartsWith("goz__")) goz = t;
                if (n.Contains("__")) continue;
                if (n.Contains("upperarm")) { if (Sol(n)) ol = t; else if (Sag(n)) or_ = t; }
                if (boyun == null && n.Contains("neck")) boyun = t;
            }
            if (ol == null || or_ == null || boyun == null || boyun.parent == null) return null;
            Vector3 up = dikey.normalized;
            Vector3 ileri = bas != null && goz != null ? Vector3.ProjectOnPlane(goz.position - bas.position, up) : Vector3.zero;
            if (ileri.sqrMagnitude < 1e-8f) ileri = Vector3.ProjectOnPlane(model.forward, up);
            ileri.Normalize(); var sag = Vector3.Cross(up, ileri);
            Vector3 om = (ol.position + or_.position) * .5f; float w = Vector3.ProjectOnPlane(ol.position - or_.position, up).magnitude;
            if (w < 1e-4f) return null;
            float arka = 0; bool bulundu = false;
            foreach (var rd in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!rd.enabled || rd.sharedMesh == null) continue;
                string et = PlayerView.Etiket(rd.gameObject.name); if (et == "bant" || et == "bilek" || rd.name.StartsWith("raket")) continue;
                var m = new Mesh(); rd.BakeMesh(m, true); var M = Matrix4x4.TRS(rd.transform.position, rd.transform.rotation, Vector3.one);
                foreach (var v in m.vertices)
                {
                    var d = M.MultiplyPoint3x4(v) - om;
                    if (Mathf.Abs(Vector3.Dot(d, up)) > .12f * w || Mathf.Abs(Vector3.Dot(d, sag)) > .3f * w) continue;
                    float z = Vector3.Dot(d, ileri); if (!bulundu || z < arka) { arka = z; bulundu = true; }
                }
                Object.Destroy(m);
            }
            if (!bulundu) arka = -.25f * w;
            float omuzY = Vector3.Dot(om - model.position, up);
            float boy = h.altY < -.8f ? Mathf.Min(1f, .55f * omuzY / (w * -h.altY)) : 1f;
            var kok = new GameObject("sirt").transform; kok.SetParent(boyun.parent, true);
            kok.position = om + ileri * (arka - .01f * w); kok.rotation = Quaternion.LookRotation(ileri, up);
            float ls = boyun.parent.lossyScale.x; kok.localScale = new Vector3(w, w * boy, w) / Mathf.Max(1e-5f, ls);
            HazirObj(kok, h);
            if (h.altY < -.8f) kok.gameObject.AddComponent<PelerinSalinim>();
            return kok;
        }

        // Sarım yönü elle tutturulmaz: her ağ iki yüzlü yazılır (arka yüz ters normalle ayrı köşeler), hangi taraftan bakılsa dolu.
        static Mesh IkiYuz(Mesh m)
        {
            var v = new List<Vector3>(m.vertices); var nr = new List<Vector3>(m.normals); var t = new List<int>(m.triangles); int n = v.Count, tn = t.Count;
            for (int i = 0; i < n; i++) { v.Add(v[i]); nr.Add(-nr[i]); }
            for (int i = 0; i < tn; i += 3) { t.Add(t[i] + n); t.Add(t[i + 2] + n); t.Add(t[i + 1] + n); }
            var o = new Mesh(); o.SetVertices(v); o.SetNormals(nr); o.SetTriangles(t, 0); o.RecalculateBounds(); return o;
        }
        // ---- yordamsal ağlar (birim ölçüde; kök ölçeği baş yarıçapı) ----
        static Mesh Koni(int n)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            v.Add(new Vector3(0, 1, 0)); for (int i = 0; i < n; i++) { float a = 2 * Mathf.PI * i / n; v.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); }
            for (int i = 0; i < n; i++) { int a = 1 + i, b = 1 + (i + 1) % n; t.Add(0); t.Add(b); t.Add(a); }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return IkiYuz(m);
        }
        // Halka: xy düzleminde yarıçap R, kesit yarıçapı rr (gözlük çerçevesi, korsan bandının kayışı).
        static Mesh Halka(int n, int k, float R, float rr)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a = 2 * Mathf.PI * i / n; var c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                for (int j = 0; j < k; j++) { float b = 2 * Mathf.PI * j / k; v.Add(c * (R + rr * Mathf.Cos(b)) + Vector3.forward * rr * Mathf.Sin(b)); }
            }
            for (int i = 0; i < n; i++) for (int j = 0; j < k; j++)
                {
                    int a = i * k + j, b = ((i + 1) % n) * k + j, c = ((i + 1) % n) * k + (j + 1) % k, d = i * k + (j + 1) % k;
                    t.Add(a); t.Add(b); t.Add(c); t.Add(a); t.Add(c); t.Add(d);
                }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return IkiYuz(m);
        }
        // Tüp: noktalar boyunca değişen yarıçap (kulaklık yayı, kask tüyü).
        static Mesh Tup(Vector3[] p, float[] r, int k)
        {
            var v = new List<Vector3>(); var t = new List<int>(); int n = p.Length;
            for (int i = 0; i < n; i++)
            {
                var d = (p[Mathf.Min(n - 1, i + 1)] - p[Mathf.Max(0, i - 1)]).normalized;
                var u = Vector3.Cross(d, Mathf.Abs(d.y) < .9f ? Vector3.up : Vector3.right).normalized; var w = Vector3.Cross(d, u);
                for (int j = 0; j < k; j++) { float a = 2 * Mathf.PI * j / k; v.Add(p[i] + (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * r[i]); }
            }
            for (int i = 0; i < n - 1; i++) for (int j = 0; j < k; j++)
                {
                    int a = i * k + j, b = (i + 1) * k + j, c = (i + 1) * k + (j + 1) % k, d = i * k + (j + 1) % k;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(a); t.Add(d); t.Add(c);
                }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return IkiYuz(m);
        }
    }
}
