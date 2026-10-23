#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Unity Build Automation pre-export callback. Generates the playable scene
// after scripts have compiled, and selects a format supported by static hosting.
public static class NonationCloudBuildHook
{
    public static void PreExport()
    {
        MIR.Editor.MirUnitySetup.PrepareForCloud();
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        Debug.Log("nonation: scene prepared and WebGL compression disabled for static hosting.");
    }
}
#endif
