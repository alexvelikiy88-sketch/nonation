using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR
{
    public static class MirRenderMaterials
    {
        const string RuntimeFallbackPath = "MIR/MirRuntimeFallback";
        static bool warnedFallback;

        static Shader Supported(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            return shader != null && shader.isSupported ? shader : null;
        }

        public static Shader LitShader()
        {
            // The scene creates most geometry and materials at runtime.
            // Shader.Find alone does NOT retain unreferenced shaders in WebGL:
            // a built-in or URP shader can be stripped from the player.
            bool usesSrp = GraphicsSettings.currentRenderPipeline != null;
            Shader lit = usesSrp
                ? Supported("Universal Render Pipeline/Lit")
                : Supported("Standard");
            if (lit == null)
                lit = usesSrp
                    ? Supported("Standard")
                    : Supported("Universal Render Pipeline/Lit");
            if (lit == null)
                lit = Supported("Unlit/Color");

            if (lit != null)
                return lit;

            // An actual Shader asset in Resources is included by Unity's
            // player build process; it is not only a string name lookup.
            // This minimal material is deliberately light on WebGL variants.
            var guaranteed = Resources.Load<Shader>(RuntimeFallbackPath);
            if (guaranteed == null || !guaranteed.isSupported)
                throw new InvalidOperationException(
                    "NONATION_RUNTIME_SHADER_MISSING: " + RuntimeFallbackPath +
                    " must be included and supported in the Unity WebGL build.");

            if (!warnedFallback)
            {
                warnedFallback = true;
                Debug.LogWarning("NONATION_SHADER_FALLBACK: bundled fallback shader used; " +
                                 "built-in or URP lit shader was stripped or unsupported.");
            }
            return guaranteed;
        }

        public static Material Create(Color color, float metallic = 0f, float smoothness = .12f)
        {
            var material = new Material(LitShader());
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            return material;
        }
    }
}
