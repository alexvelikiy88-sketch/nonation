#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MIR.QuaterniusHero.Editor
{
    /// <summary>
    /// Import the author's original CC0 FBX/atlas bytes unchanged.
    /// The renderer uses a controlled NONATION Built-in material palette in
    /// runtime; do not generate sidecar Unity material guesses or alter UVs.
    /// </summary>
    internal sealed class MirQuaterniusHeroImport : AssetPostprocessor
    {
        const string Directory =
            "Assets/MIR/Art/QuaterniusHero/Resources/MIR/QuaterniusHero/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Directory, StringComparison.OrdinalIgnoreCase) ||
                !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                return;
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.globalScale = 1f;
            importer.isReadable = false;
            importer.generateSecondaryUV = false;
            Debug.Log("NONATION_CC0_FBX_IMPORT original=" + assetPath);
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Directory, StringComparison.OrdinalIgnoreCase) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return;
            var importer = assetImporter as TextureImporter;
            if (importer == null) return;
            // Original 32x32 Quaternius atlas: keep discrete swatches, avoid
            // mipmapped colour bleeding and arbitrary Unity compression.
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            Debug.Log("NONATION_CC0_ATLAS_IMPORT original=" + assetPath);
        }
    }
}
#endif
