#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MIR.Editor
{
    public sealed class MirAssetImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            string p=assetPath.Replace('\\','/').ToLowerInvariant();
            if(!p.Contains("assets/mir/art/"))return;
            var imp=assetImporter as ModelImporter;
            if(imp==null)return;
            if(p.Contains("quaternius") || p.Contains("pinkdress") || p.Contains("gign"))
            {
                imp.animationType=ModelImporterAnimationType.Human;
                imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation=true;
                imp.resampleCurves=true;
            }
        }
    }
}
#endif
