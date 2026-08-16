using System;
using System.IO;
using SignalHunt.Backend;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WaypointRally.Editor
{
    public static class WaypointRallyProjectBuilder
    {
        private const string MainScenePath = "Assets/WaypointRally/Scenes/Main.unity";
        private const string ConfigAssetPath = "Assets/Resources/SignalHuntRuntimeConfig.asset";

        [MenuItem("Waypoint Rally/Setup Project")]
        public static void SetupProject()
        {
            Directory.CreateDirectory("Assets/WaypointRally/Scenes");
            if (!File.Exists(MainScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Waypoint Rally").AddComponent<WaypointRallyGame>();
                EditorSceneManager.SaveScene(scene, MainScenePath);
            }
            else
            {
                var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                if (UnityEngine.Object.FindFirstObjectByType<WaypointRallyGame>() == null)
                {
                    new GameObject("Waypoint Rally").AddComponent<WaypointRallyGame>();
                    EditorSceneManager.SaveScene(scene, MainScenePath);
                }
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
            PlayerSettings.companyName = "Isaiah Dupree";
            PlayerSettings.productName = "Waypoint Rally";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.isaiahdupree.waypointrally");
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = true;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EnsureRuntimeShaders();
            AssetDatabase.SaveAssets();
            Debug.Log("Waypoint Rally project and scene configured.");
        }

        [MenuItem("Waypoint Rally/Build macOS Preview")]
        public static void BuildMacPreview()
        {
            SetupProject();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/WaypointRally.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Waypoint Rally macOS build failed: {report.summary.result}");
            }
        }

        [MenuItem("Waypoint Rally/Build iOS")]
        public static void BuildIos()
        {
            SetupProject();
            CreateRuntimeConfig();
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MainScenePath },
                    locationPathName = "Builds/WaypointRally-iOS",
                    target = BuildTarget.iOS,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException($"Waypoint Rally iOS build failed: {report.summary.result}");
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(ConfigAssetPath);
                AssetDatabase.Refresh();
            }
        }

        private static void CreateRuntimeConfig()
        {
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SIGNAL_HUNT_SUPABASE_ANON_KEY") ??
                      Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
            if (!Uri.TryCreate(url, UriKind.Absolute, out _) || string.IsNullOrWhiteSpace(key))
            {
                throw new BuildFailedException(
                    "Set SUPABASE_URL and SIGNAL_HUNT_SUPABASE_ANON_KEY before building iOS.");
            }
            Directory.CreateDirectory("Assets/Resources");
            var config = ScriptableObject.CreateInstance<SignalHuntRuntimeConfig>();
            config.supabaseUrl = url;
            config.supabaseAnonKey = key;
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            AssetDatabase.SaveAssets();
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
