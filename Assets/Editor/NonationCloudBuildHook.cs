#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Unity Build Automation pre-export callback. Configure this method in UBA
// Advanced settings to import optional CC0 motions before exporting WebGL.
public static class NonationCloudBuildHook
{
    public static void PreExport()
    {
        MIR.Editor.MirUnitySetup.PrepareForCloud();
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        Debug.Log("nonation: scene prepared and WebGL compression disabled for static hosting.");
    }
}

// This callback does not require a Unity Cloud configuration checkbox:
// Unity invokes it for every BuildPipeline.BuildPlayer, including Build Automation.
public sealed class NonationBuildPreflight : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return -10000; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        // Validate only. Scene generation must happen before Unity enumerates build scenes.
        const string scenePath = "Assets/MIR/Scenes/MIR_PlayableDistrict.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            throw new BuildFailedException("MIR WebGL scene could not be imported: " + scenePath);
        bool included = false;
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled && scene.path == scenePath) { included = true; break; }
        if (!included)
            throw new BuildFailedException("MIR WebGL scene is not enabled in Build Settings");
        if (report.summary.platform == BuildTarget.WebGL)
        {
            VerifyPublicAssets();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        }
        Debug.Log("MIR preflight: playable scene registered for " + report.summary.platform);
    }

    static void VerifyPublicAssets()
    {
        // Unity bundles enabled-scene dependencies AND everything under Resources.
        // Reject known non-commercial / rights-unverified sources before WebGL export.
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled) roots.Add(scene.path);
        foreach (var path in AssetDatabase.GetAllAssetPaths())
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0)
                roots.Add(path);
        if (roots.Count == 0) throw new BuildFailedException("MIR: no build assets were registered");
        string[] restricted = {
            "/honda_nc/", "hondacivic_nc", "honda_civic",
            "maid_skinned", "girl_highpoly", "floral_look",
            "casual_confidence", "gign_paranoia", "/gign/",
            "dressfor", "/pinkdress/", "/freya/"
        };
        foreach (var path in AssetDatabase.GetDependencies(roots.ToArray(), true))
        {
            foreach (var token in restricted)
                if (path.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new BuildFailedException(
                        "MIR public WebGL includes a non-commercial or rights-unverified model: " +
                        path + ". Replace with verified CC0 or obtain redistribution rights.");
        }
    }
}
#endif
