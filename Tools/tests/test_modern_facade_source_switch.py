"""Regression: 12 mutually exclusive modern facades, no duplicated gameplay volumes.

This is NOT WebGL acceptance. Real Unity compilation, actual mesh counts,
unedited fixed-angle screenshot, NPC/POI and performance must be checked.
"""
from pathlib import Path
import json
import shutil
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/MIR/Art"
CITY = ROOT / "Assets/MIR/Scripts/MirCity.cs"
INTEGRATOR = ART / "Architecture/MirModernDistrictIntegrator.cs"
ARCHITECTURE = ART / "Architecture/MirArchitectureQuarter.cs"
SHADER = ART / "Architecture/Resources/Nonation/Architecture/ArchitecturalLit.shader"
KENNEY = ART / "Kenney/MirKenneyCityArt.cs"
SMOKE = ROOT / "Tools/browser_smoke_preview.js"


class ModernFacadeSwitchTests(unittest.TestCase):
    def test_active_facades_reuse_exact_existing_business_and_home_footprints(self):
        code = INTEGRATOR.read_text(encoding="utf-8")
        source = ARCHITECTURE.read_text(encoding="utf-8")
        city = CITY.read_text(encoding="utf-8")
        self.assertIn("const int BuildingCount = 12", code)
        for label in ("Grocery store", "Corner café", "Auto workshop", "Town hall"):
            self.assertIn('"' + label + '"', code)
        self.assertIn('new Building("Grocery store"', source)
        self.assertIn('new Building("Corner cafe"', source)
        self.assertIn('new Building("Residential block "+i', source)
        self.assertIn("MirArchitectureQuarter.Build(holder.transform, false)", code)
        self.assertIn("if(createGameplayColliders)", source)
        self.assertIn("NONATION_MODULAR_CITY_READY buildings=12 meshes=", code)
        self.assertIn("NONATION_MODULAR_CITY_ERROR", code)
        self.assertIn("filter.sharedMesh.vertexCount", code)
        self.assertIn("renderer.enabled = false", code)
        self.assertIn("MirModernDistrictIntegrator.TryActivate(root)", city)
        self.assertIn("kenney.SkipBuildings=modularBuildings", city)
        for game in ('AddBusiness("market"', 'AddBusiness("cafe"',
                     "MirWorldGeometry.HomeEntry", "MirWorldGeometry.SidewalkRoute",
                     "art.AddComponent<MirKenneyCityArt>()"):
            self.assertIn(game, city)

    def test_shader_is_real_builtin_physically_lit_and_resources_backed(self):
        shader = SHADER.read_text(encoding="utf-8")
        self.assertIn('Shader "NONATION/ArchitecturalLit"', shader)
        self.assertIn("#pragma surface surf Standard fullforwardshadows", shader)
        self.assertIn("sampler2D _MainTex", shader)
        self.assertNotIn("Packages/com.unity.render-pipelines.universal/", shader)
        importer = ARCHITECTURE.read_text(encoding="utf-8")
        self.assertIn('Resources.Load<Shader>("Nonation/Architecture/ArchitecturalLit")', importer)
        self.assertIn("Texture2D(size,size,TextureFormat.RGBA32,true,false)", importer)
        self.assertNotIn("DownloadHandler", importer)

    def test_real_cc0_texture_blobs_not_git_lfs_placeholders(self):
        scan_root = ART / "Architecture/Resources/Nonation/Architecture/Scans"
        source = ARCHITECTURE.read_text(encoding="utf-8")
        attributes = (ROOT / ".gitattributes").read_text(encoding="utf-8")
        names = ("Bricks097_Color.jpg", "Planks023A_Color.jpg",
                 "Plaster007_ColorEven.jpg")
        for name in names:
            p = scan_root / name
            self.assertTrue(p.exists(), name)
            self.assertGreater(p.stat().st_size, 200_000, name)
            self.assertEqual(p.read_bytes()[:3], bytes.fromhex("ffd8ff"), name)
            self.assertIn(name + " -filter -diff -merge -text", attributes)
            self.assertIn('"' + name.rsplit(".", 1)[0] + '"', source)

    def test_kenney_is_still_loaded_for_trees_vehicles_props_and_fallback(self):
        kenney = KENNEY.read_text(encoding="utf-8")
        self.assertIn("public bool SkipBuildings", kenney)
        self.assertIn("if (!SkipBuildings)", kenney)
        self.assertIn("NONATION_KENNEY_CITY_READY", kenney)
        self.assertIn("MirGame.Instance.City.Vehicles", kenney)
        self.assertIn("nature-kit/tree-oak", kenney)
        self.assertIn("city-kit-roads/traffic-light", kenney)

    def test_release_requires_true_modular_and_kenney_counts_after_reload(self):
        browser = SMOKE.read_text(encoding="utf-8")
        self.assertIn("requireModernQuarterReady", browser)
        self.assertIn("meta.modernFacadesReloaded = requireModernQuarterReady", browser)
        self.assertIn("requireKenneyCityReady(line, true)", browser)
        self.assertIn("NONATION_MODERN_FACADES_VERIFIED phase=reload", browser)
        self.assertIn("NONATION_HERO_QUARTER_READY", browser)
        self.assertIn("nonation-browser-showcase.png", browser)

    @unittest.skipUnless(shutil.which("node"), "Node.js unavailable")
    def test_count_parser_and_browser_are_valid_javascript(self):
        for path in ["Tools/browser_smoke_preview.js",
                     "Tools/kenney_runtime_counts.js"]:
            result = subprocess.run([shutil.which("node"), "--check", path],
                cwd=ROOT, capture_output=True, text=True, timeout=15)
            self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
