#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MIR.Editor
{
    /// <summary>GameCI and cloud entry point. Always generates the district before building.</summary>
    public static class MirCloudBuild
    {
        public static void Build()
        {
            MirUnitySetup.PrepareForCloud();
            const string scene = "Assets/MIR/Scenes/MIR_PlayableDistrict.unity";
            if (!File.Exists(scene)) throw new Exception("MIR scene was not generated: " + scene);
            var output = Path.Combine("build", "WebGL");
            Directory.CreateDirectory(output);
            // GitHub Pages does not allow custom Content-Encoding response headers.
            // Build uncompressed Unity Web assets to avoid a broken loading screen.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("MIR WebGL build failed: " + report.summary.result + " (" + report.summary.totalErrors + " errors)");
            Debug.Log("MIR WebGL output: " + Path.GetFullPath(output));
        }
    }
}
#endif
