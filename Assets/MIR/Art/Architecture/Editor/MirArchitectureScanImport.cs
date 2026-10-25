#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MIR.Architecture.Editor
{
    /// <summary>
    /// Imported ambientCG CC0 scans must use the correct color space and
    /// tangent map compression or the Unity Standard BRDF renders dull/flat
    /// surfaces. Keep mipmaps for distant architecture and WebGL memory.
    /// </summary>
    public sealed class MirArchitectureScanImport : AssetPostprocessor
    {
        const string ScanFolder =
            "Assets/MIR/Art/Architecture/Resources/Nonation/Architecture/Scans/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ScanFolder, StringComparison.OrdinalIgnoreCase))
                return;

            var texture = assetImporter as TextureImporter;
            if (texture == null) return;
            bool normal = assetPath.EndsWith("_NormalGL.jpg", StringComparison.OrdinalIgnoreCase)
                || assetPath.EndsWith("_NormalSmoothGL.jpg", StringComparison.OrdinalIgnoreCase);
            bool roughness = assetPath.EndsWith("_Roughness.jpg", StringComparison.OrdinalIgnoreCase);

            texture.textureType = normal ? TextureImporterType.NormalMap :
                TextureImporterType.Default;
            texture.sRGBTexture = !(normal || roughness);
            texture.mipmapEnabled = true;
            texture.maxTextureSize = 2048;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            if (normal) texture.convertToNormalmap = false;
        }
    }
}
#endif
