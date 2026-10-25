"""Regress the Unity Cloud #11 glTFast Built-in compiler blocker.

Cloud #11 failed with CS0246 in MirKenneyCityArt.cs: Unity glTFast 6.0.0
hides BuiltInMaterialGenerator from player builds when USING_URP is defined.
Its assembly versionDefine sets USING_URP whenever the *package* is installed,
even if no URP pipeline asset is actually selected.

These are source-contract checks, NOT a replacement for actual Unity compile.
"""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]


class BuiltinGltfPipelineContract(unittest.TestCase):
    def test_no_urp_package_without_a_configured_pipeline(self):
        manifest = json.loads(
            (ROOT / "Packages/manifest.json").read_text(encoding="utf-8")
        )
        deps = manifest["dependencies"]
        self.assertEqual(deps["com.unity.cloud.gltfast"], "6.0.0")
        self.assertNotIn(
            "com.unity.render-pipelines.universal", deps,
            "glTFast 6.0.0 compiles out BuiltInMaterialGenerator when "
            "USING_URP is defined by the package dependency, regardless of "
            "which Unity render pipeline is active.",
        )
        self.assertNotIn("com.unity.render-pipelines.high-definition", deps)
        self.assertFalse((ROOT / "ProjectSettings/GraphicsSettings.asset").exists())
        self.assertFalse((ROOT / "ProjectSettings/QualitySettings.asset").exists())

    def test_gltf_runtime_importer_keeps_builtin_materials_and_3d_meshes(self):
        importer = (
            ROOT / "Assets/MIR/Art/Kenney/MirKenneyCityArt.cs"
        ).read_text(encoding="utf-8")
        self.assertIn("new BuiltInMaterialGenerator()", importer)
        self.assertIn("new GltfImport(materialGenerator: materialGenerator)", importer)
        self.assertIn("GraphicsSettings.currentRenderPipeline == null", importer)
        self.assertIn("NONATION_KENNEY_CITY_READY", importer)
        self.assertIn("Resources.Load<GameObject>", importer)
        self.assertIn("GltfImport", importer)
        self.assertIn("TryBounds(instance, out bounds)", importer)

    def test_unity_shader_fallback_still_supports_builtin_pipeline(self):
        materials = (
            ROOT / "Assets/MIR/Scripts/MirRenderMaterials.cs"
        ).read_text(encoding="utf-8")
        self.assertIn('Supported("Standard")', materials)
        self.assertIn("GraphicsSettings.currentRenderPipeline", materials)
        self.assertIn("Resources.Load<Shader>", materials)


if __name__ == "__main__":
    unittest.main()
