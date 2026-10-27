#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MIR.QuaterniusHero.Editor
{
    /// <summary>
    /// Prevent a visually invalid Unity WebGL export from silently including
    /// the old block facades. Verify actual IMPORTED Quaternius FBX meshes,
    /// atlas UVs, textures and art shader, not just files existing on disk.
    /// Original gameplay physics/simulation are not touched.
    /// </summary>
    public sealed class MirQuaterniusHeroBuildGate : IPreprocessBuildWithReport
    {
        const string Folder =
            "Assets/MIR/Art/QuaterniusHero/Resources/MIR/QuaterniusHero/";
        const string ShaderPath =
            "Assets/MIR/Art/Architecture/Resources/Nonation/Architecture/ArchitecturalLit.shader";

        static readonly string[] Models = {
            "2Story_Wide_2Doors", "2Story_Balcony", "2Story_Sign", "3Story_Small"
        };
        static readonly string[] Atlases = {
            "Texture_Light", "Texture_Red", "Texture_Grey", "Texture_Blue"
        };

        public int callbackOrder { get { return 6500; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null || !shader.isSupported)
                throw new BuildFailedException(
                    "NONATION_CC0_HERO_BUILD_BLOCKED: real artist FBX shader unsupported");

            foreach (var atlas in Atlases)
            {
                var path = Folder + atlas + ".png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null || texture.width != 32 || texture.height != 32)
                    throw new BuildFailedException(
                        "NONATION_CC0_HERO_BUILD_BLOCKED: original pinned 32x32 atlas " + path);
            }

            int vertices = 0;
            foreach (var model in Models)
            {
                var path = Folder + model + ".fbx";
                var imported = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (imported == null)
                    throw new BuildFailedException(
                        "NONATION_CC0_HERO_BUILD_BLOCKED: real FBX not imported " + path);
                int meshVertices = 0;
                foreach (var filter in imported.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null || filter.sharedMesh == null) continue;
                    var mesh = filter.sharedMesh;
                    meshVertices += mesh.vertexCount;
                    if (!mesh.HasVertexAttribute(
                        UnityEngine.Rendering.VertexAttribute.TexCoord0))
                        throw new BuildFailedException(
                            "NONATION_CC0_HERO_BUILD_BLOCKED: authored atlas UV missing " + model);
                }
                if (meshVertices < 250)
                    throw new BuildFailedException(
                        "NONATION_CC0_HERO_BUILD_BLOCKED: fake or empty FBX " + model);
                vertices += meshVertices;
            }
            Debug.Log("NONATION_CC0_HERO_SOURCE_IMPORT_READY fbxs=4 atlases=4" +
                      " vertices=" + vertices + " (original CC0 game models)");
        }
    }
}
#endif
