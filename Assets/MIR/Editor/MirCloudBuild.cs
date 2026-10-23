#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MIR.Editor
{
    // Cloud / GameCI build entry point.
    // Requires MIR.Editor.MirUnitySetup and the rest of the MIR Unity source.
    public static class MirCloudBuild
    {
        public static void Build()
        {
            MirUnitySetup.PrepareForCloud();
            const string scene = "Assets/MIR/Scenes/MIR_PlayableDistrict.unity";
            if (!File.Exists(scene))
                throw new Exception("MIR scene missing after setup: " + scene);
            const string output = "build/WebGL";
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new [] { scene },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("MIR WebGL build failed: " + report.summary.result);
            Debug.Log("MIR WebGL ready: " + Path.GetFullPath(output));
        }
    }
}
#endif
