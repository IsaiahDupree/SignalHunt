using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SignalHunt.Editor
{
    public static class BrandIconUtility
    {
        private static readonly string[] AppIconPaths =
        {
            "Assets/Brand/AppIcons/SignalHunt.png",
            "Assets/Brand/AppIcons/WaypointRally.png",
            "Assets/Brand/AppIcons/WaypointWings.png",
            "Assets/Brand/AppIcons/TreasureHunter.png"
        };

        public static void ApplyIosIcon(string assetPath)
        {
            var icon = LoadIcon(assetPath);

            var iconSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.iOS);
            var icons = new Texture2D[iconSizes.Length];
            for (var index = 0; index < icons.Length; index++)
            {
                icons[index] = icon;
            }

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, icons);
        }

        [MenuItem("Island Challenge/Validate App Icons")]
        public static void ValidateAppIcons()
        {
            foreach (var assetPath in AppIconPaths)
            {
                LoadIcon(assetPath);
            }

            Debug.Log($"Validated {AppIconPaths.Length} full-size app icons.");
        }

        private static Texture2D LoadIcon(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (icon == null)
            {
                throw new BuildFailedException($"App icon could not be loaded at {assetPath}.");
            }
            if (icon.width != 1024 || icon.height != 1024)
            {
                throw new BuildFailedException(
                    $"App icon at {assetPath} must be 1024 x 1024 pixels; found {icon.width} x {icon.height}.");
            }

            return icon;
        }
    }
}
