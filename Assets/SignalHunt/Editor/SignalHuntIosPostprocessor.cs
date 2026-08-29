using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace SignalHunt.Editor
{
    public static class SignalHuntIosPostprocessor
    {
        [PostProcessBuild(100)]
        public static void ConfigureNativeFrameworks(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "ReplayKit.framework", false);
            project.WriteToFile(projectPath);

            var plistPath = Path.Combine(buildPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSPhotoLibraryAddUsageDescription",
                "Save Treasure Hunter, Signal Hunt, Waypoint Rally, and Waypoint Wings replay videos.");
            plist.WriteToFile(plistPath);
        }
    }
}
