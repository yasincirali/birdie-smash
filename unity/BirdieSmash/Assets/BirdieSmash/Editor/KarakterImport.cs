// Karakter FBX içe aktarımı (Resources/karakter/<model>.fbx: oyuncak = KayKit Rogue, rocketbox = Sports_Male_04):
// Legacy animasyon, yeniden örnekleme ve sıkıştırma kapalı (3d-animator: "exact baked clips: resample off,
// compression off"); klipler tool/karakter/unity_fbx.py'nin yazdığı <model>_clips.json'daki kare aralıklarından bölünür.
// Malzeme içe alınır (gömülü doku, InPrefab): deri modellerin yüz/saç dokusu modelin kendisinden gelir; PlayerView
// yalnız forma/çerçeve parçalarını boyar. Eski tek renkli prototipte (oyuncu.fbx) malzeme kapalıydı, gerek kalmadı.
// Klip bölme OnPreprocessAnimation'da: defaultClipAnimations (take adı) OnPreprocessModel'de henüz dolu olmayabilir.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BirdieSmash.EditorTools
{
    public class KarakterImport : AssetPostprocessor
    {
        [System.Serializable] class Clip { public string name; public int first, last; public bool loop; }
        [System.Serializable] class Meta { public int fps; public float scale; public Clip[] clips; }

        bool Karakter() { var p = assetPath.Replace('\\', '/'); return p.Contains("Resources/karakter/") && p.EndsWith(".fbx"); }

        void OnPreprocessModel()
        {
            if (!Karakter()) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true; mi.resampleCurves = false; mi.animationCompression = ModelImporterAnimationCompression.Off;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.useFileScale = true; mi.globalScale = 1;
            mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
        }

        void OnPreprocessAnimation()
        {
            if (!Karakter()) return;
            var mi = (ModelImporter)assetImporter;
            string json = Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath) + "_clips.json");
            if (!File.Exists(json)) { Debug.LogWarning("Birdie Smash: " + json + " yok, klipler bölünmedi"); return; }
            var meta = JsonUtility.FromJson<Meta>(File.ReadAllText(json));
            var def = mi.defaultClipAnimations; string take = def.Length > 0 ? def[0].takeName : null;
            var arr = new ModelImporterClipAnimation[meta.clips.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var c = meta.clips[i];
                arr[i] = new ModelImporterClipAnimation { name = c.name, takeName = take ?? c.name, firstFrame = c.first, lastFrame = c.last, loopTime = c.loop, wrapMode = c.loop ? WrapMode.Loop : WrapMode.ClampForever };
            }
            mi.clipAnimations = arr;
        }
    }
}
