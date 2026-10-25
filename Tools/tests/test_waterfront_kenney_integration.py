"""Check that the AGENT 03 waterfront is an independent, real 3D scene module.

This static regression guards the preservation of the existing simulation
contracts. Unity compile, WebGL output, FPS and visible geometry remain
separate required release gates.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
CITY = ROOT / "Assets/MIR/Scripts/MirCity.cs"
WATERFRONT = ROOT / "Assets/MIR/Art/Waterfront/MirWaterfrontQuarter.cs"


class WaterfrontIntegrationContract(unittest.TestCase):
    def test_city_calls_waterfront_once_and_keeps_kenney_and_economy(self):
        city = CITY.read_text(encoding="utf-8")
        self.assertEqual(
            city.count("MirWaterfrontQuarter.Build(root,Mat,lamps.Add)"), 1)
        self.assertIn("art.AddComponent<MirKenneyCityArt>();", city)
        self.assertIn('AddBusiness("market"', city)
        self.assertIn('AddBusiness("cafe"', city)
        self.assertIn("Vehicles.Add(MirVehicle.Spawn", city)
        self.assertIn("MirWorldGeometry.HomeEntry", city)
        self.assertIn("MirWorldGeometry.SidewalkRoute", city)

    def test_geometry_is_not_static_background_image(self):
        code = WATERFRONT.read_text(encoding="utf-8")
        self.assertIn("internal static class MirWaterfrontQuarter", code)
        self.assertIn("public static void Build(Transform city", code)
        self.assertIn('new GameObject("Walkable promenade surface")', code)
        self.assertIn('new GameObject("Water-edge safety collider")', code)
        self.assertIn("AddComponent<BoxCollider>()", code)
        self.assertIn('sea.Emit(parent,"Open waterfront sea mesh"', code)
        self.assertIn("new Mesh()", code)
        self.assertIn("registerLight(light)", code)
        self.assertNotIn("MirGame.Instance.World.Business", code)
        self.assertNotIn("Texture2D.LoadImage", code)


if __name__ == "__main__":
    unittest.main()
