// Karakter FBX içe aktarımı: Legacy animasyon, yeniden örnekleme ve sıkıştırma kapalı (3d-animator:
// "exact baked clips: resample off, compression off"), klipler tool/karakter/unity_fbx.py'nin
// yazdığı oyuncu_clips.json'daki kare aralıklarından bölünür. Malzeme içe alınmaz: renkleri
// PlayerView parça etiketinden verir (takım rengi oyunda seçilir).
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BirdieSmash.EditorTools
{
    public class KarakterImport : AssetPostprocessor
    {
        [System.Serializable] class Clip { public string name; public int first, last; public bool loop; }
        [System.Serializable] class Meta { public int fps; public float scale; public Clip[] clips; }

        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').EndsWith("Resources/karakter/oyuncu.fbx")) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true; mi.resampleCurves = false; mi.animationCompression = ModelImporterAnimationCompression.Off;
            mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.useFileScale = true; mi.globalScale = 1;
            mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
            string json = Path.Combine(Path.GetDirectoryName(assetPath), "oyuncu_clips.json");
            if (!File.Exists(json)) { Debug.LogWarning("Birdie Smash: oyuncu_clips.json yok, klipler bölünmedi"); return; }
            var meta = JsonUtility.FromJson<Meta>(File.ReadAllText(json));
            var arr = new ModelImporterClipAnimation[meta.clips.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var c = meta.clips[i];
                arr[i] = new ModelImporterClipAnimation { name = c.name, takeName = mi.defaultClipAnimations.Length > 0 ? mi.defaultClipAnimations[0].takeName : c.name, firstFrame = c.first, lastFrame = c.last, loopTime = c.loop, wrapMode = c.loop ? WrapMode.Loop : WrapMode.ClampForever };
            }
            mi.clipAnimations = arr;
        }
    }
}
