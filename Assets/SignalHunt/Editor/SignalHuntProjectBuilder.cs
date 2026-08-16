using System;
using System.IO;
using SignalHunt.Backend;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SignalHunt.Editor
{
    public static class SignalHuntProjectBuilder
    {
        private const string MainScenePath = "Assets/SignalHunt/Scenes/Main.unity";
        private const string ConfigAssetPath = "Assets/Resources/SignalHuntRuntimeConfig.asset";

        [MenuItem("Signal Hunt/Setup Project")]
        public static void SetupProject()
        {
            Directory.CreateDirectory("Assets/SignalHunt/Scenes");
            if (!File.Exists(MainScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Signal Hunt").AddComponent<SignalHuntGame>();
                EditorSceneManager.SaveScene(scene, MainScenePath);
            }
            else
            {
                var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                if (UnityEngine.Object.FindFirstObjectByType<SignalHuntGame>() == null)
                {
                    new GameObject("Signal Hunt").AddComponent<SignalHuntGame>();
                    EditorSceneManager.SaveScene(scene, MainScenePath);
                }
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };

            PlayerSettings.companyName = "Isaiah Dupree";
            PlayerSettings.productName = "Signal Hunt";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.isaiahdupree.signalhunt");
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresPersistentWiFi = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = true;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EnsureRuntimeShaders();
            AssetDatabase.SaveAssets();
            Debug.Log("Signal Hunt project and Main scene configured.");
        }

        [MenuItem("Signal Hunt/Build iOS")]
        public static void BuildIos()
        {
            SetupProject();
            var hasRuntimeConfig = TryCreateRuntimeConfig();
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { MainScenePath },
                    locationPathName = "Builds/iOS",
                    target = BuildTarget.iOS,
                    options = BuildOptions.None
                };
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException($"Signal Hunt iOS build failed: {report.summary.result}");
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

        [MenuItem("Signal Hunt/Build macOS Preview")]
        public static void BuildMacPreview()
        {
            SetupProject();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/SignalHunt.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Signal Hunt macOS preview build failed: {report.summary.result}");
            }
        }

        private static bool TryCreateRuntimeConfig()
        {
            AssetDatabase.DeleteAsset(ConfigAssetPath);
            AssetDatabase.Refresh();
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var anonKey = Environment.GetEnvironmentVariable("SIGNAL_HUNT_SUPABASE_ANON_KEY") ??
                          Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
            if (!Uri.TryCreate(url, UriKind.Absolute, out _) || string.IsNullOrWhiteSpace(anonKey))
            {
                Debug.LogWarning(
                    "Building Signal Hunt in honest offline mode. Set SUPABASE_URL and SIGNAL_HUNT_SUPABASE_ANON_KEY to enable the live leaderboard.");
                return false;
            }

            Directory.CreateDirectory("Assets/Resources");
            var config = ScriptableObject.CreateInstance<SignalHuntRuntimeConfig>();
            config.supabaseUrl = url;
            config.supabaseAnonKey = anonKey;
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

            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettings.Length == 0)
            {
                throw new BuildFailedException("Project graphics settings could not be loaded.");
            }
            var serialized = new SerializedObject(graphicsSettings[0]);
            var included = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (included == null)
            {
                throw new BuildFailedException("Always Included Shaders setting could not be found.");
            }
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
