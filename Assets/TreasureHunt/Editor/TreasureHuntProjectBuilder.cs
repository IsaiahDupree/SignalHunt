using System;
using System.IO;
using SignalHunt.Backend;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TreasureHunt.Editor
{
    public static class TreasureHuntProjectBuilder
    {
        private const string MainScenePath = "Assets/TreasureHunt/Scenes/Main.unity";
        private const string ConfigAssetPath = "Assets/Resources/SignalHuntRuntimeConfig.asset";
        private const string AppIconPath = "Assets/Brand/AppIcons/TreasureHunter.png";

        [MenuItem("Treasure Hunter/Setup Project")]
        public static void SetupProject()
        {
            Directory.CreateDirectory("Assets/TreasureHunt/Scenes");
            if (!File.Exists(MainScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Treasure Hunter").AddComponent<TreasureHuntGame>();
                EditorSceneManager.SaveScene(scene, MainScenePath);
            }
            else
            {
                var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                if (UnityEngine.Object.FindAnyObjectByType<TreasureHuntGame>() == null)
                {
                    new GameObject("Treasure Hunter").AddComponent<TreasureHuntGame>();
                    EditorSceneManager.SaveScene(scene, MainScenePath);
                }
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
            PlayerSettings.companyName = "Isaiah Dupree";
            PlayerSettings.productName = "Treasure Hunter";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.isaiahdupree.treasurehunter");
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = true;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            SignalHunt.Editor.BrandIconUtility.ApplyIosIcon(AppIconPath);
            EnsureRuntimeShaders();
            AssetDatabase.SaveAssets();
            Debug.Log("Treasure Hunter project and scene configured.");
        }

        [MenuItem("Treasure Hunter/Build macOS Preview")]
        public static void BuildMacPreview()
        {
            SetupProject();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/TreasureHunter.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Treasure Hunter macOS build failed: {report.summary.result}");
            }
        }

        [MenuItem("Treasure Hunter/Build iOS")]
        public static void BuildIos()
        {
            SetupProject();
            var hasRuntimeConfig = TryCreateRuntimeConfig();
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MainScenePath },
                    locationPathName = "Builds/TreasureHunter-iOS",
                    target = BuildTarget.iOS,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException($"Treasure Hunter iOS build failed: {report.summary.result}");
                }
            }
            finally
            {
                if (hasRuntimeConfig)
                {
                    AssetDatabase.DeleteAsset(ConfigAssetPath);
                    AssetDatabase.Refresh();
                }
            }
        }

        private static bool TryCreateRuntimeConfig()
        {
            AssetDatabase.DeleteAsset(ConfigAssetPath);
            AssetDatabase.Refresh();
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SIGNAL_HUNT_SUPABASE_ANON_KEY") ??
                      Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
            if (!Uri.TryCreate(url, UriKind.Absolute, out _) || string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning(
                    "Building Treasure Hunter in honest offline mode. Set SUPABASE_URL and SIGNAL_HUNT_SUPABASE_ANON_KEY to enable the live leaderboard.");
                return false;
            }
            Directory.CreateDirectory("Assets/Resources");
            var config = ScriptableObject.CreateInstance<SignalHuntRuntimeConfig>();
            config.supabaseUrl = url;
            config.supabaseAnonKey = key;
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void EnsureRuntimeShaders()
        {
            var standard = Shader.Find("Standard");
            if (standard == null)
            {
                throw new BuildFailedException("Unity's built-in Standard shader is unavailable.");
            }
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            var serialized = new SerializedObject(assets[0]);
            var included = serialized.FindProperty("m_AlwaysIncludedShaders");
            for (var index = 0; index < included.arraySize; index++)
            {
                if (included.GetArrayElementAtIndex(index).objectReferenceValue == standard)
                {
                    return;
                }
            }
            included.InsertArrayElementAtIndex(included.arraySize);
            included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = standard;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
