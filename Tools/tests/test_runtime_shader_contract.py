"""Prevent Unity WebGL Shader.Find-only regressions in the procedural city.

Static source gate only; a browser smoke is required for runtime proof.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
SHADER = ROOT / "Assets/MIR/Resources/MIR/MirRuntimeFallback.shader"
META = SHADER.with_suffix(".shader.meta")
RESOLVER = ROOT / "Assets/MIR/Scripts/MirRenderMaterials.cs"


class WebGLShaderRetentionTests(unittest.TestCase):
    def test_shader_is_a_committed_resources_dependency(self):
        self.assertTrue(SHADER.is_file(), "Runtime shader must be under Resources")
        self.assertTrue(META.is_file(), "Runtime shader Unity GUID must be in Git")
        self.assertRegex(META.read_text(encoding="utf-8"),
                         r"(?m)^guid: [0-9a-f]{32}$")

    def test_shader_is_supported_webgl_colored_vertex_fragment(self):
        source = SHADER.read_text(encoding="utf-8")
        self.assertIn('Shader "MIR/RuntimeFallback"', source)
        self.assertIn('_Color ("Base Color", Color)', source)
        self.assertIn("#pragma vertex vert", source)
        self.assertIn("#pragma fragment frag", source)
        self.assertIn("UnityObjectToClipPos", source)
        self.assertIn("SV_Target", source)

    def test_material_resolver_uses_real_resources_shader_if_stripped(self):
        code = RESOLVER.read_text(encoding="utf-8")
        self.assertIn('const string RuntimeFallbackPath = "MIR/MirRuntimeFallback"', code)
        self.assertIn("Resources.Load<Shader>(RuntimeFallbackPath)", code)
        self.assertIn("shader.isSupported", code)
        self.assertIn("NONATION_RUNTIME_SHADER_MISSING", code)
        self.assertLess(code.index("Resources.Load<Shader>"),
                        code.index("new Material(LitShader())"))

    def test_color_contract_matches_runtime_material(self):
        code = RESOLVER.read_text(encoding="utf-8")
        shader = SHADER.read_text(encoding="utf-8")
        self.assertIn("material.color = color", code)
        self.assertIn("fixed4 _Color", shader)
        self.assertNotIn("new Material(Shader.Find(", code)


if __name__ == "__main__":
    unittest.main()
