"""NONATION hero buildings are real source models from original 2021 CC0 archive.

These source checks do NOT establish Unity FBX importer/material success, real
on-screen quality or safe frame rate. Those require Unity Cloud and Chromium.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Tools/quaternius_hero_manifest.json"
FETCH = ROOT / "Tools/fetch_quaternius_hero.py"
CLOUD_HOOK = ROOT / "Tools/unity_cloud_prebuild.sh"
MAIN_CI = ROOT / ".github/workflows/unity-webgl.yml"
IMPORT = ROOT / "Assets/MIR/Art/QuaterniusHero/Editor/MirQuaterniusHeroImport.cs"
BUILD_GATE = ROOT / "Assets/MIR/Art/QuaterniusHero/Editor/MirQuaterniusHeroBuildGate.cs"
RUNTIME = ROOT / "Assets/MIR/Art/QuaterniusHero/MirQuaterniusHeroBuildings.cs"
ARCH = ROOT / "Assets/MIR/Art/Architecture/MirArchitectureQuarter.cs"
INTEGRATOR = ROOT / "Assets/MIR/Art/Architecture/MirModernDistrictIntegrator.cs"
BROWSER = ROOT / "Tools/browser_smoke_preview.js"
COUNTERS = ROOT / "Tools/kenney_runtime_counts.js"

MODELS = {
    "2Story_Wide_2Doors.fbx":
        ("3f6589fe46e9074283af505d1decf4886f43f0f9bcd83556c81a5a02d5e0a08e", 223932),
    "2Story_Balcony.fbx":
        ("ef6b6bb610c41a4d1495c038d604b4a9a5f2212da9e85ff668ff81293d01afa4", 127116),
    "2Story_Sign.fbx":
        ("dcc663401ac7e7a7c8903f07aa50c49c12ea75a25aa8c494484c688c8228e129", 107180),
    "3Story_Small.fbx":
        ("2f7e8125ce66eda004faba7747e12a45e249f460e30c29a7a00a476767ca7bd9", 101916),
}
ATLAS = {
    "Texture_Light.png": ("ed8c7eea22f37be7ea98af3ed31078470e4cbb94a9724c0ce0f51ba5475899e8", 3212),
    "Texture_Red.png": ("7dd79085044dbe8c96d7a290447d01dd27859cc4cedcbdaa3b501e7260df4226", 3212),
    "Texture_Grey.png": ("183fd0711eb6a475e5f5eac93e184aab96fac5f8acd029f914e1da5657cc38d5", 3212),
    "Texture_Blue.png": ("2000b15f574f2b4202921fb8d88ba0343a9cdd5630632a823aa8eff378b52963", 3212),
}
ZIP_HASH = "ff876502f53f6d4c35104378e8322ec8253fd0ee45c8ec28d9dc81df54b80e85"


class RealCc0BuildingsTests(unittest.TestCase):
    def test_only_four_original_pretextured_models_selected(self):
        m = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual(m["archive_sha256"], ZIP_HASH)
        self.assertEqual(m["archive_size"], 29961262)
        self.assertEqual(m["license"], "CC0-1.0")
        self.assertEqual(m["source_url"], "https://opengameart.org/content/lowpoly-buildings-pack")
        self.assertEqual(m["archive_url"], "https://opengameart.org/sites/default/files/ultimate_textured_building_pack_by_quaternius.zip")
        assets = {a["name"]: (a["sha256"], a["size"]) for a in m["assets"]}
        self.assertEqual(assets, {**MODELS, **ATLAS})
        for a in m["assets"]:
            self.assertTrue(a["destination"].startswith(
                "Assets/MIR/Art/QuaterniusHero/Resources/MIR/QuaterniusHero/"))
            self.assertTrue(a["source_path"].startswith(
                "Ultimate Textured Building Pack - Dec 2019/"))
        self.assertEqual(len(m["assets"]), 8)

    def test_source_fetch_is_atomic_verified_and_no_path_traversal(self):
        code = FETCH.read_text(encoding="utf-8")
        for token in ("read_manifest", "sha256(blob)", "archive_sha256",
                      "DEST_ROOT.resolve()", "ZipFile", "sha256(obj)",
                      "os.replace(tmp, dst)", "--verify-only",
                      "Unverified stage:", "CC0 hero models missing"):
            self.assertIn(token, code)
        self.assertNotIn("subprocess.run(", code)
        self.assertNotIn("os.system(", code)
        self.assertNotIn("extractall(", code)
        self.assertNotIn("eval(", code)

    def test_github_and_unity_cloud_both_verify_exact_original_assets(self):
        prebuild = CLOUD_HOOK.read_text(encoding="utf-8")
        ci = MAIN_CI.read_text(encoding="utf-8")
        for code in (prebuild, ci):
            self.assertIn("python3 Tools/fetch_quaternius_hero.py", code)
            self.assertIn("python3 Tools/fetch_quaternius_hero.py --verify-only", code)
        self.assertIn("Tools/fetch_kenney_city.py", prebuild)

    def test_unity_resources_import_and_material_atlas_are_real_meshes(self):
        editor = IMPORT.read_text(encoding="utf-8")
        runtime = RUNTIME.read_text(encoding="utf-8")
        for item in ("ModelImporterAnimationType.None",
                     "ModelImporterMaterialImportMode.None",
                     "TextureImporterCompression.Uncompressed",
                     "importer.filterMode = FilterMode.Point"):
            self.assertIn(item, editor)
        for item in ("Resources.Load<GameObject>(Prefix + modelName)",
                     "Resources.Load<Texture2D>(Prefix + atlas)",
                     "Resources.Load<Shader>(", "True author-modeled CC0 FBX facades",
                     "sharedMesh.vertexCount", "mesh.subMeshCount",
                     "uniform", "renderer.receiveShadows = true",
                     "NONATION_QUATERNIUS_HERO_READY models=5 slots=3"):
            self.assertIn(item, runtime)
        self.assertNotIn("GameObject.CreatePrimitive", runtime)
        self.assertNotIn("Texture2D.LoadImage", runtime)

    def test_unity_editor_rejects_fake_fbx_rollback_for_visual_release(self):
        code = BUILD_GATE.read_text(encoding="utf-8")
        for token in (
            "IPreprocessBuildWithReport",
            "OnPreprocessBuild(BuildReport report)",
            "BuildTarget.WebGL",
            "AssetDatabase.LoadAssetAtPath<GameObject>(path)",
            "AssetDatabase.LoadAssetAtPath<Texture2D>(path)",
            "mesh.HasVertexAttribute(",
            "BuildFailedException",
            "NONATION_CC0_HERO_BUILD_BLOCKED",
            "NONATION_CC0_HERO_SOURCE_IMPORT_READY fbxs=4 atlases=4",
        ):
            self.assertIn(token, code)
        self.assertNotIn("GameObject.CreatePrimitive", code)

    def test_selected_real_art_never_duplicates_gameplay_colliders(self):
        importer = RUNTIME.read_text(encoding="utf-8")
        arch = ARCH.read_text(encoding="utf-8")
        integ = INTEGRATOR.read_text(encoding="utf-8")
        self.assertIn("MirQuaterniusHeroBuildings.TryBuild(holder.transform)", integ)
        self.assertIn("MirArchitectureQuarter.Build(holder.transform, false, true)", integ)
        self.assertIn("MirArchitectureQuarter.Build(holder.transform, false);", integ)
        self.assertIn("if(plan.index!=0 && plan.index!=1 && plan.index!=3)", arch)
        self.assertIn("foreach(Building plan in plans)district.BuildBuilding(plan);", arch)
        self.assertIn("UnityEngine.Object.Destroy(collision)", importer)
        self.assertIn("cc0Hero=", integ)
        self.assertIn("foreach (var placeholder in placeholders)", integ)
        self.assertIn("renderer.enabled = false;", integ)

    def test_chromium_requires_true_real_fbx_on_first_load_and_reload(self):
        browser = BROWSER.read_text(encoding="utf-8")
        helper = COUNTERS.read_text(encoding="utf-8")
        self.assertIn("requireCc0HeroReady", helper)
        self.assertIn("models: 5, slots: 3, atlas: 4", helper)
        self.assertIn("actual.vertices = Number(verts[1])", helper)
        self.assertIn("meta.cc0HeroBuildings = requireCc0HeroReady", browser)
        self.assertIn("meta.cc0HeroBuildingsReloaded = requireCc0HeroReady", browser)
        self.assertIn("cc0Hero=models", browser)
        self.assertIn("NONATION_QUATERNIUS_HERO_READY", browser)


if __name__ == "__main__":
    unittest.main()
