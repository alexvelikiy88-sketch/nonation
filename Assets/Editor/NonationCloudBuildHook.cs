#if UNITY_EDITOR
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
        MIR.Editor.MirUnitySetup.EnsureBuildScene();
        const string scenePath = "Assets/MIR/Scenes/MIR_PlayableDistrict.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            throw new BuildFailedException("MIR WebGL scene could not be imported: " + scenePath);
        bool included = false;
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled && scene.path == scenePath) { included = true; break; }
        if (!included)
            throw new BuildFailedException("MIR WebGL scene is not enabled in Build Settings");
        if (report.summary.platform == BuildTarget.WebGL)
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        Debug.Log("MIR preflight: playable scene registered for " + report.summary.platform);
    }
}
#endif
