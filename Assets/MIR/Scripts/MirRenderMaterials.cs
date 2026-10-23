using UnityEngine;
using UnityEngine.Rendering;
namespace MIR
{
    public static class MirRenderMaterials
    {
        public static Shader LitShader()
        {
            // Installing URP does not enable it; read the actual active pipeline.
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader result = srp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            return result ?? Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
        }
        public static Material Create(Color color, float metallic = 0f, float smoothness = .12f)
        {
            var shader = LitShader();
            if (shader == null) throw new System.InvalidOperationException("Missing runtime shader");
            var material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }
    }
}
