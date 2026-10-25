#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MIR.Editor
{
    /// <summary>
    /// Unity Build Automation can invoke BuildPipeline without MirCloudBuild.Build.
    /// Guarantee the actual Unity WebGL Player uses the same custom full-screen
    /// template regardless of whether GameCI, UBA, or our local builder runs it.
    /// </summary>
    public sealed class MirNonationWebTemplatePrebuild : IPreprocessBuildWithReport
    {
        const string TemplateFile = "Assets/WebGLTemplates/Nonation/index.html";
        const string TemplateName = "PROJECT:Nonation";
        public int callbackOrder { get { return -9500; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;

            if (!File.Exists(TemplateFile))
                throw new BuildFailedException(
                    "NONATION_WEBGL_TEMPLATE_MISSING: " + TemplateFile);

            // Refuse to produce a stock 960x600 Unity page if a template was
            // accidentally replaced with a generic loading shell.
            var html = File.ReadAllText(TemplateFile);
            if (!html.Contains("{{{ LOADER_FILENAME }}}") ||
                !html.Contains("{{{ DATA_FILENAME }}}") ||
                !html.Contains("{{{ CODE_FILENAME }}}") ||
                !html.Contains("{{{ FRAMEWORK_FILENAME }}}") ||
                !html.Contains("id=\"unity-canvas\"") ||
                !html.Contains("id=\"game-shell\""))
                throw new BuildFailedException(
                    "NONATION_WEBGL_TEMPLATE_INVALID: missing Unity loader macros or canvas.");

            PlayerSettings.WebGL.template = TemplateName;
            Debug.Log("NONATION_WEBGL_FULLSCREEN_TEMPLATE_SELECTED name=" + TemplateName);
        }
    }
}
#endif
