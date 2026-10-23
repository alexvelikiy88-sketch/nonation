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
            // Unity cannot reliably switch compilation targets inside -batchmode
            // -executeMethod: the required script/domain reload cannot run mid-call.
            // GameCI / UBA must start the Editor with WebGL already selected.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new Exception(
                    "NONATION_BUILD_TARGET_MISMATCH: Editor started for " +
                    EditorUserBuildSettings.activeBuildTarget +
                    ". Launch Unity with -batchmode -buildTarget WebGL before -executeMethod.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new Exception("NONATION_WEBGL_MODULE_MISSING: install Unity Web Build Support.");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            Debug.Log("NONATION_BUILD_BEGIN unity=" + Application.unityVersion +
                      " activeTarget=" + EditorUserBuildSettings.activeBuildTarget +
                      " scene=" + scene);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report == null)
                throw new IOException("NONATION_BUILD_NO_REPORT: BuildPipeline returned null.");
            Debug.Log("NONATION_BUILD_SUMMARY result=" + report.summary.result +
                      " errors=" + report.summary.totalErrors +
                      " warnings=" + report.summary.totalWarnings +
                      " bytes=" + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors > 0)
                throw new Exception("NONATION_BUILD_FAILED: " + report.summary.result +
                                    " (" + report.summary.totalErrors + " errors)");
            string html = Path.Combine(output, "index.html");
            string buildFolder = Path.Combine(output, "Build");
            if (!File.Exists(html) || new FileInfo(html).Length == 0 ||
                !Directory.Exists(buildFolder))
                throw new IOException("NONATION_OUTPUT_INCOMPLETE: missing nonempty index.html or Build/.");
            foreach (string extension in new[] { ".wasm", ".data", ".js" })
            {
                bool found = false;
                foreach (string asset in Directory.GetFiles(buildFolder))
                    if (asset.EndsWith(extension, StringComparison.OrdinalIgnoreCase) &&
                        new FileInfo(asset).Length > 0) { found = true; break; }
                if (!found)
                    throw new IOException("NONATION_OUTPUT_INCOMPLETE: missing uncompressed " + extension);
            }
            Debug.Log("NONATION_BUILD_ARTIFACT_READY path=" + Path.GetFullPath(output));
        }
    }
}
#endif
