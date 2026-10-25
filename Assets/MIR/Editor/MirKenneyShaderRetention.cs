#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR.Editor
{
    /// <summary>
    /// glTFast resolves its Built-in shaders through Shader.Find in WebGL.
    /// Shaders referenced only by name are stripped from the player; retain
    /// actual shader dependencies via small generated Materials in Resources.
    /// No third-party shader source or binaries are copied into public Git.
    /// </summary>
    public sealed class MirKenneyShaderRetention : IPreprocessBuildWithReport
    {
        const string PackageShaderFolder =
            "Packages/com.unity.cloud.gltfast/Runtime/Shader/Built-In/";
        const string ResourceFolder = "Assets/MIR/Resources/MIR/GltfRuntime";

        public int callbackOrder { get { return -9000; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                throw new BuildFailedException(
                    "NONATION_KENNEY_SHADER_PIPELINE_MISMATCH: expected Built-in RP. " +
                    "A future URP migration requires explicit shader and asset QA.");

            if (!AssetDatabase.IsValidFolder(ResourceFolder))
                AssetDatabase.CreateFolder("Assets/MIR/Resources/MIR", "GltfRuntime");

            Retain("glTFPbrMetallicRoughness.shader", "glTF/PbrMetallicRoughness",
                "KenneyPbrMetallicRoughness");
            Retain("glTFPbrSpecularGlossiness.shader", "glTF/PbrSpecularGlossiness",
                "KenneyPbrSpecularGlossiness");
            Retain("glTFUnlit.shader", "glTF/Unlit", "KenneyUnlit");

            AssetDatabase.SaveAssets();
            // Make sure the just-created Resources dependencies are imported
            // before the WebGL player collects its asset graph.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("NONATION_KENNEY_SHADER_RETAINED count=3 pipeline=BuiltIn");
        }

        static void Retain(string packageFile, string shaderName, string materialName)
        {
            var source = PackageShaderFolder + packageFile;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(source);
            if (shader == null || shader.name != shaderName)
                throw new BuildFailedException(
                    "NONATION_KENNEY_SHADER_SOURCE_MISSING: " + source +
                    " (expected " + shaderName + "). Verify glTFast 6.0.0.");

            var path = ResourceFolder + "/" + materialName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                material.name = materialName;
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
        }
    }
}
#endif
