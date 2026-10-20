// Proje ilk açıldığında demo sahnesini ve oyuncu ayarlarını kurar: boş Demo sahnesi (GameRoot her
// şeyi koddan kurar), dikey ekran, bundle com.birdiesmash.app, Standard shader yapıya dahil
// (Shader.Find cihazda null dönmesin), eski Input Manager açık (Input System paketi yok).
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BirdieSmash.EditorTools
{
    [InitializeOnLoad]
    public static class DemoKurulum
    {
        const string Scene = "Assets/Scenes/Demo.unity";
        static DemoKurulum() { EditorApplication.delayCall += () => { if (!File.Exists(Scene)) Kur(); }; }

        [MenuItem("Birdie Smash/Demo kurulumunu yenile")]
        public static void Kur()
        {
            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists(Scene))
            {
                var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(s, Scene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            if (SceneManager.GetActiveScene().path != Scene) EditorSceneManager.OpenScene(Scene);
            PlayerSettings.productName = "Birdie Smash"; PlayerSettings.companyName = "Birdie Smash";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.birdiesmash.app");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.birdiesmash.app");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            ShaderEkle("Standard");
            InputEski();
            AssetDatabase.SaveAssets();
            Debug.Log("Birdie Smash demo hazır: Assets/Scenes/Demo.unity açık, Play'e bas.");
        }

        static void ShaderEkle(string name)
        {
            var sh = Shader.Find(name); if (sh == null) return;
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (gs == null || gs.Length == 0) return;
            var so = new SerializedObject(gs[0]); var arr = so.FindProperty("m_AlwaysIncludedShaders"); if (arr == null) return;
            for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
            arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; so.ApplyModifiedProperties();
        }

        // Kod UnityEngine.Input kullanır; Input System paketi kurulu değilse yalnız eski girdi çalışır.
        static void InputEski()
        {
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps == null || ps.Length == 0) return;
            var so = new SerializedObject(ps[0]); var p = so.FindProperty("activeInputHandler");
            if (p != null && p.intValue == 1) { p.intValue = 2; so.ApplyModifiedProperties(); Debug.LogWarning("Birdie Smash: girdi 'Both' yapıldı; Unity yeniden başlatmanı isteyebilir."); }
        }
    }
}
