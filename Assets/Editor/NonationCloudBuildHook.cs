#if UNITY_EDITOR
using UnityEngine;

// Unity Cloud Build Automation's pre-export method.
// This is called after scripts compile, before Unity exports the Web build.
public static class NonationCloudBuildHook
{
    public static void PreExport()
    {
        MIR.Editor.MirUnitySetup.PrepareForCloud();
        Debug.Log("nonation: generated MIR playable district for cloud export.");
    }
}
#endif
