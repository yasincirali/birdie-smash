// Karakter FBX içe aktarımı (Resources/karakter/<model>.fbx: oyuncak = KayKit Rogue, rocketbox = Sports_Male_04):
// Legacy animasyon, anahtar azaltma (aşağıda 2026-11-12). Yeniden örnekleme AÇIK (2026-10-26, Unity ekran turu r8): kapalıyken Unity
// FBX'in Euler eğrilerini bileşen bileşen ara-değerliyor; Rocketbox (Biped) köprücük/kol açıları ±180° sınırında
// olduğundan anahtarlar arasında kollar başın arkasına savruluyordu (Blender'da aynı karede eller göğüs hizasında).
// Örnekleme her kareyi FBX SDK ile doğru sırada çözüp kuaterniyona çevirir; anahtar karelerde değer aynı kalır.
// Klipler tool/karakter/unity_fbx.py'nin yazdığı <model>_clips.json'daki kare aralıklarından bölünür.
// Malzeme içe alınır (gömülü doku, InPrefab): deri modellerin yüz/saç dokusu modelin kendisinden gelir; PlayerView
// yalnız forma/çerçeve parçalarını boyar. Eski tek renkli prototipte (oyuncu.fbx) malzeme kapalıydı, gerek kalmadı.
// 2026-11-12 (model thread, bellek): animasyon sıkıştırması Off → KeyframeReduction, hata payları 0.2 (Unity varsayılanı 0.5).
// Off'ta yeniden örneklenmiş 33 klip × 933 kare × 53 kemik × 10 kanal (konum/dönüş/ölçek) sabit olsa bile her karede saklanıyordu:
// model başına ~8 MB eğri; maçta 4 figür farklı tip FBX'i yükleyebildiği için ~30 MB. Kemiklerin çoğunda konum ve ölçek sabit,
// azaltma onları iki anahtara indirir; dönüşte 0.2° sınırı kol zincirinde raket ucunda ~1–2 cm eder (erişim IK p90 0.15 m'nin altında).
// Örnekleme açık kalır (Euler sınırı sorunu yukarıda); azaltma örneklenmiş kuaterniyon eğrisine uygulanır.
// 2026-11-21 (boyut, yasin: "oyun boyutunun makul düzeyde tutulması, küçültülmesi"): _clips.json'da klipKaynak başka bir modeli
// gösteriyorsa (maskotlar → tuycuk) bu FBX'in animasyonu içe aktarılmaz; PlayerView.KlipleriBagla kaynağın kliplerini ekler.
// Altı maskot × ~3,3 MB eğri build'den çıkar. Ağ sıkıştırması Low: yalnız paket boyutunu küçültür (çalışma belleği aynı),
// Low köşe konumunu en az nicemler; el ve yüz ayrıntısında görünür kayma beklenmez (ekran turu Dolap/Yüz karesi kontrol).
// GetVersion: ayar değişince Unity (CI Library önbelleği dahil) FBX'leri yeniden içe aktarsın.
// Klip bölme OnPreprocessAnimation'da: defaultClipAnimations (take adı) OnPreprocessModel'de henüz dolu olmayabilir.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BirdieSmash.EditorTools
{
    public class KarakterImport : AssetPostprocessor
    {
        [System.Serializable] class Clip { public string name; public int first, last; public bool loop; }
        [System.Serializable] class Meta { public int fps; public float scale; public Clip[] clips; public string klipKaynak; }

        // 4 (2026-11-21 akşam): maskotların klip paylaşımı geri alındı (ekran turu r227: kedi klip şeritlerinde altı karenin altısı
        // aynı pozdaydı, paylaşılan klipler örneklenmiyordu). _clips.json'dan klipKaynak kalktı; sürüm artışı CI Library
        // önbelleğinde animasyonsuz içe aktarılmış maskot FBX'lerini yeniden içe aktartır.
        // 5: animasyon sıkıştırması KeyframeReduction → Optimal.
        // 6: forma ağına desen UV'si (OnPostprocessModel). 7: ön yönü düzeltmesi.
        public override uint GetVersion() { return 7; }

        bool Karakter() { var p = assetPath.Replace('\\', '/'); return p.Contains("Resources/karakter/") && p.EndsWith(".fbx"); }

        // Resources/karakter dokuları: *_n = normal haritası (aksi hâlde sRGB renk sayılır, normaller bozuk çıkar).
        // *_mg = doğrusal, alfa smoothness'tır, saydamlık değil. En çok 1024 px (mobil bellek; 4 figür aynı dokuyu paylaşır).
        void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/'); if (!p.Contains("Resources/karakter/")) return;
            var ti = (TextureImporter)assetImporter; string ad = Path.GetFileNameWithoutExtension(p);
            ti.maxTextureSize = 1024; ti.mipmapEnabled = true;
            if (ad.EndsWith("_n")) { ti.textureType = TextureImporterType.NormalMap; ti.sRGBTexture = false; }
            else if (ad.EndsWith("_mg")) { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = false; ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = false; ti.maxTextureSize = 512; }
        }

        void OnPreprocessModel()
        {
            if (!Karakter()) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true; mi.resampleCurves = true; mi.animationCompression = ModelImporterAnimationCompression.Optimal;   // 2026-11-21 boyut: iOS raporunda Animations 36,6 MB; Optimal eğri başına yoğun/seyrek saklamayı seçer, hata payları aynı (.2)
            mi.animationRotationError = .2f; mi.animationPositionError = .2f; mi.animationScaleError = .2f;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.useFileScale = true; mi.globalScale = 1;
            mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
            mi.meshCompression = ModelImporterMeshCompression.Low;
            var meta = MetaOku(); if (meta != null && PaylasilanKlip(meta)) mi.importAnimation = false;
        }

        Meta MetaOku()
        {
            string json = Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath) + "_clips.json");
            return File.Exists(json) ? JsonUtility.FromJson<Meta>(File.ReadAllText(json)) : null;
        }
        bool PaylasilanKlip(Meta m) { return !string.IsNullOrEmpty(m.klipKaynak) && m.klipKaynak != Path.GetFileNameWithoutExtension(assetPath); }

        void OnPreprocessAnimation()
        {
            if (!Karakter()) return;
            var mi = (ModelImporter)assetImporter;
            string json = Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath) + "_clips.json");
            if (!File.Exists(json)) { Debug.LogWarning("Birdie Smash: " + json + " yok, klipler bölünmedi"); return; }
            var meta = JsonUtility.FromJson<Meta>(File.ReadAllText(json)); if (meta == null || PaylasilanKlip(meta)) return;
            var def = mi.defaultClipAnimations; string take = def.Length > 0 ? def[0].takeName : null;
            var arr = new ModelImporterClipAnimation[meta.clips.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var c = meta.clips[i];
                arr[i] = new ModelImporterClipAnimation { name = c.name, takeName = take ?? c.name, firstFrame = c.first, lastFrame = c.last, loopTime = c.loop, wrapMode = c.loop ? WrapMode.Loop : WrapMode.ClampForever };
            }
            mi.clipAnimations = arr;
        }

        // Forma deseni (2026-11-21, yasin: kıyafet katmanı "önce C, sonra A"): her modelin forma ağı (<önek>_forma__forma) içe aktarımda
        // desen UV'si alır. Oyuncak ve hayvan modellerinde forma düz renkti, UV'si gövde atlasında tek hücreye düşüyordu ve kullanılmıyordu
        // (PlayerView: atlas modelde düz takım rengi); o yüzden UV0 güvenle yeniden yazılır. Eşleme bağlanma pozunda silindirik ve
        // AYNALI: u = |açı| / π (0 = göğüs ortası, 1 = sırt ortası), v = forma yüksekliği (0 bel, 1 yaka). Aynalı olduğu için dikiş
        // yok (sağ ve sol yan aynı u'yu alır, üçgen dokunun boyunca gerilmez); bedeli: desen sağ/sol simetrik olur (sırt numarası gibi
        // yönlü yazı yok, çapraz kuşak V olur). Ön yönü ayakkabı ağlarının formaya göre konumundan, yoksa +Z (ekran turu r238: ilk işaret ters çıktı, ön desenler sırtta göründü; işaret çevrildi. Ayakkabı ağlarının kutusu topuk tarafında ağır basıyor).
        // Tüycük'ün tüy gövdesi (tc_tuy__forma) desen almaz.
        void OnPostprocessModel(GameObject g)
        {
            if (!Karakter()) return;
            var formalar = new System.Collections.Generic.List<(Mesh m, Matrix4x4 w)>(); Vector3 ayak = Vector3.zero; int ayakN = 0;
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                Mesh m = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
                if (m == null) continue; string ad = r.name;
                if (ad.EndsWith("__forma") && ad.Contains("_forma__")) formalar.Add((m, r.transform.localToWorldMatrix));
                else if (ad.EndsWith("__ayakkabi") || ad.EndsWith("__taban")) { ayak += r.transform.localToWorldMatrix.MultiplyPoint3x4(m.bounds.center); ayakN++; }
            }
            foreach (var (m, w) in formalar)
            {
                var v = m.vertices; if (v.Length == 0) continue; var p = new Vector3[v.Length]; Vector3 mn = Vector3.positiveInfinity, mx = Vector3.negativeInfinity;
                for (int i = 0; i < v.Length; i++) { p[i] = w.MultiplyPoint3x4(v[i]); mn = Vector3.Min(mn, p[i]); mx = Vector3.Max(mx, p[i]); }
                Vector3 c = (mn + mx) * .5f; float on = ayakN > 0 && ayak.z / ayakN > c.z ? -1 : 1, h = Mathf.Max(1e-4f, mx.y - mn.y);
                var uv = new Vector2[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    float a = Mathf.Abs(Mathf.Atan2(p[i].x - c.x, (p[i].z - c.z) * on)) / Mathf.PI;
                    uv[i] = new Vector2(a, (p[i].y - mn.y) / h);
                }
                m.uv = uv;
            }
            if (formalar.Count > 0) Debug.Log("Birdie Smash: desen UV " + Path.GetFileNameWithoutExtension(assetPath) + " · " + formalar.Count + " forma ağı · ön " + (ayakN > 0 ? "ayakkabıdan" : "+Z"));
        }
    }
}
