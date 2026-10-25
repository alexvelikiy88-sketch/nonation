"""Regress one real 3D quarter on top of the proven Kenney WebGL baseline.

These are SOURCE tests. Passing them is not proof that Unity shaders,
geometry, framing, collision, input, or FPS are visually acceptable.
"""
from pathlib import Path
import shutil
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
CITY = ROOT / "Assets/MIR/Scripts/MirCity.cs"
PLAZA = ROOT / "Assets/MIR/Art/Architecture/MirCivicSquare.cs"
WATERFRONT = ROOT / "Assets/MIR/Art/Waterfront/MirWaterfrontQuarter.cs"
STREETS = ROOT / "Assets/MIR/Art/Street/MirQuarterStreetLife.cs"
CAMERA = ROOT / "Assets/MIR/Scripts/MirFollowCamera.cs"
BROWSER = ROOT / "Tools/browser_smoke_preview.js"
FLOW = ROOT / ".github/workflows/unity-webgl.yml"
ARTIFACT_FLOW = ROOT / ".github/workflows/webgl-artifact-browser-smoke.yml"


class HeroQuarterIntegrationContract(unittest.TestCase):
    def test_one_live_plaza_waterfront_streets_and_original_gameplay(self):
        city = CITY.read_text(encoding="utf-8")
        for call in ("MirCivicSquare.Build(root,Mat,lamps.Add)",
                     "MirWaterfrontQuarter.Build(root,Mat,lamps.Add)",
                     "MirQuarterStreetLife.Build(root,Mat)"):
            self.assertEqual(city.count(call), 1, call)
        self.assertNotIn("AsphaltRoads();MarketSquare();", city)
        self.assertIn("if(a>=0 && a<=1 && b>=0 && b<=1)continue;", city)
        self.assertIn("NONATION_HERO_QUARTER_READY plaza=1 waterfront=1 streets=1", city)
        self.assertIn("NONATION_HERO_QUARTER_ERROR", city)
        for existing in ("art.AddComponent<MirKenneyCityArt>();",
                         'AddBusiness("market"', 'AddBusiness("cafe"',
                         'AddBusiness("garage"', "Vehicles.Add(MirVehicle.Spawn",
                         "MirWorldGeometry.HomeEntry", "MirWorldGeometry.SidewalkRoute",
                         "BlockBuilding(\"Residential block \"+i"):
            self.assertIn(existing, city, existing)
        self.assertIn("MarketSquare(); // Playable technical fallback", city)

    def test_real_3d_geometry_and_only_one_set_of_trees(self):
        plaza = PLAZA.read_text(encoding="utf-8")
        water = WATERFRONT.read_text(encoding="utf-8")
        street = STREETS.read_text(encoding="utf-8")
        for source, label in ((plaza, "plaza"), (water, "waterfront"),
                              (street, "street")):
            self.assertIn("new Mesh()", source, label)
            self.assertIn("MeshRenderer", source, label)
            self.assertNotIn("Texture2D.LoadImage", source, label)
        self.assertIn("Plaza walkable ground collider", plaza)
        self.assertIn("Central fountain plinth / collider", plaza)
        self.assertIn("Walkable promenade surface", water)
        self.assertIn("Water-edge safety collider", water)
        self.assertIn("Open waterfront sea mesh", water)
        self.assertIn("registerLight(light)", water)
        self.assertIn("kit.HeroCrosswalks();", street)
        self.assertNotIn("Tree(13f,13f,.99f,0)", street)
        self.assertNotIn("Tree(53f,53f,1.02f,1)", street)

    def test_repeatable_real_showcase_camera_and_runtime_proof(self):
        camera = CAMERA.read_text(encoding="utf-8")
        browser = BROWSER.read_text(encoding="utf-8")
        self.assertIn("DistrictShowcase", camera)
        self.assertIn("KeyCode.V", camera)
        self.assertIn("KeyCode.B", camera)
        self.assertIn("WaterfrontShowcase", camera)
        self.assertIn("WaterfrontFocus=new Vector3(28f,1.5f,-108f)", camera)
        self.assertIn("ShowcaseFocus", camera)
        self.assertIn("NONATION_SHOWCASE_CAMERA_READY active=", camera)
        for marker in ("NONATION_HERO_QUARTER_READY",
                       "NONATION_SHOWCASE_CAMERA_READY active=1",
                       "NONATION_SHOWCASE_CAMERA_READY active=0",
                       "NONATION_HERO_QUARTER_VERIFIED phase=reload"):
            self.assertIn(marker, browser)
        self.assertIn("nonation-browser-showcase.png", browser)
        self.assertIn("nonation-browser-waterfront.png", browser)
        self.assertIn("NONATION_WATERFRONT_CAMERA_READY active=1", browser)
        self.assertIn("meta.heroQuarterReloaded = true", browser)
        self.assertIn("NONATION_HERO_QUARTER_ERROR", browser)
        for file in (FLOW, ARTIFACT_FLOW):
            s = file.read_text(encoding="utf-8")
            self.assertIn("nonation-browser-showcase.png", s)
            self.assertIn("nonation-browser-waterfront.png", s)
            self.assertIn("NONATION_REQUIRE_KENNEY_ART: '1'", s)

    @unittest.skipUnless(shutil.which("node"), "Node.js not available")
    def test_browser_javascript_is_syntactically_valid(self):
        run = subprocess.run(
            [shutil.which("node"), "--check", str(BROWSER)],
            cwd=ROOT, capture_output=True, text=True, timeout=15,
        )
        self.assertEqual(run.returncode, 0, run.stderr)


if __name__ == "__main__":
    unittest.main()
