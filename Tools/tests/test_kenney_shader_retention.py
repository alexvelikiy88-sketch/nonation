"""Prevent WebGL glTFast Built-in Shader.Find stripping regressions.

The online Chromium run 40194385409 compiled and deployed, but all Kenney
GLB instances failed with NullReferenceException in scene instantiation.
glTFast 6.0.0 BuiltInMaterialGenerator looks up shaders by string; ensure
real package shader references are retained in Unity Resources before build.
These tests are static: true Unity/WebGL validation remains mandatory.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
BUILD = ROOT / "Assets/MIR/Editor/MirKenneyShaderRetention.cs"
IMPORT = ROOT / "Assets/MIR/Art/Kenney/MirKenneyCityArt.cs"

SHADERS = {
    "glTFPbrMetallicRoughness.shader":
        ("glTF/PbrMetallicRoughness", "KenneyPbrMetallicRoughness"),
    "glTFPbrSpecularGlossiness.shader":
        ("glTF/PbrSpecularGlossiness", "KenneyPbrSpecularGlossiness"),
    "glTFUnlit.shader": ("glTF/Unlit", "KenneyUnlit"),
}


class KenneyShaderRetentionTests(unittest.TestCase):
    def test_prebuild_pins_all_gltfast_builtin_shader_dependencies(self):
        source = BUILD.read_text(encoding="utf-8")
        self.assertIn("IPreprocessBuildWithReport", source)
        self.assertIn("OnPreprocessBuild(BuildReport report)", source)
        self.assertIn("com.unity.cloud.gltfast/Runtime/Shader/Built-In/", source)
        self.assertIn('Assets/MIR/Resources/MIR/GltfRuntime', source)
        self.assertIn("AssetDatabase.LoadAssetAtPath<Shader>", source)
        self.assertIn("AssetDatabase.CreateAsset(material, path)", source)
        self.assertIn("AssetDatabase.SaveAssets()", source)
        self.assertIn("BuildFailedException", source)
        for file, (shader, material) in SHADERS.items():
            for token in (file, shader, material):
                self.assertIn('"' + token + '"', source)

    def test_runtime_uses_bundled_materials_before_glb_import(self):
        source = IMPORT.read_text(encoding="utf-8")
        self.assertIn("BuiltInShaderReferencesReady()", source)
        self.assertIn('Resources.Load<Material>(path)', source)
        self.assertIn("NONATION_KENNEY_SHADER_MISSING", source)
        self.assertIn("NONATION_KENNEY_CITY_ERROR", source)
        self.assertIn("new ConsoleLogger()", source)
        self.assertIn("NONATION_KENNEY_IMPORT_FAILED", source)
        self.assertLess(
            source.index("!BuiltInShaderReferencesReady()"),
            source.index("foreach (var building in BusinessBuildings)")
        )
        for _, (_, material) in SHADERS.items():
            self.assertIn('"' + material + '"', source)
        # Always retain existing city and gameplay integration when shaders fail
        self.assertIn("old.GetComponentsInChildren<Renderer>(true)", source)
        self.assertIn("MirGame.Instance.City.Vehicles", source)
        self.assertIn("NONATION_KENNEY_CITY_READY", source)

    def test_materials_are_generated_not_redistributed_unlicensed_assets(self):
        source = BUILD.read_text(encoding="utf-8")
        self.assertNotIn("Assets/AssetStore", source)
        self.assertNotIn("AssetDatabase.CopyAsset", source)
        self.assertIn('new Material(shader)', source)


if __name__ == "__main__":
    unittest.main()
