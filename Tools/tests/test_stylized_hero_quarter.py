"""Source contract for a compact, genuinely modeled (not image-backed) hero block.

Checks never assert screenshot quality, Avatar success or Unity compilation.
Those remain gated by the actual Cloud/WebGL/Chromium release.
"""
from pathlib import Path
import math
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
STREET = ROOT / "Assets/MIR/Art/Street/MirQuarterStreetLife.cs"
CITY = ROOT / "Assets/MIR/Scripts/MirCity.cs"
CAMERA = ROOT / "Assets/MIR/Scripts/MirFollowCamera.cs"
SHADER = ROOT / "Assets/MIR/Resources/MIR/MirRuntimeFallback.shader"
BROWSER = ROOT / "Tools/browser_smoke_preview.js"


def project(world, focus=(18, 4, 0), yaw=50, pitch=36, distance=62, fov=44):
    """Source-coordinate geometry check, not a rendered screen measurement."""
    y, t = math.radians(yaw), math.radians(pitch)
    sy, cy, st, ct = math.sin(y), math.cos(y), math.sin(t), math.cos(t)
    forward = (sy * ct, -st, cy * ct)
    right = (cy, 0, -sy)
    up = (sy * st, ct, cy * st)
    eye = tuple(focus[i] - distance * forward[i] for i in range(3))
    d = tuple(world[i] - eye[i] for i in range(3))
    z = sum(d[i] * forward[i] for i in range(3))
    assert z > 0
    x = sum(d[i] * right[i] for i in range(3))
    v = sum(d[i] * up[i] for i in range(3))
    scale = math.tan(math.radians(fov / 2))
    return (640 + 640 * x / (z * scale * 16 / 9),
            360 - 360 * v / (z * scale))


class PremiumDioramaHeroContract(unittest.TestCase):
    def test_plaza_store_and_cafe_fit_live_closeup_frame(self):
        camera = CAMERA.read_text(encoding="utf-8")
        for expected in (
            "ShowcaseFocus=new Vector3(18f,4f,0f)",
            "ShowcaseRotation=Quaternion.Euler(36f,50f,0f)",
            "ShowcaseDistance=62f",
            "ShowcaseFov=44f",
            "WaterfrontFocus=new Vector3(12f,1.8f,-120f)",
            "WaterfrontDistance=43f",
        ):
            self.assertIn(expected, camera)
        # Check only centers. Full margins and actual occlusion need screenshots.
        centers = ((20, .3, 20), (20, 3.5, -20), (56, 3.5, 20))
        for point in centers:
            x, y = project(point)
            self.assertGreater(x, 100, str(point))
            self.assertLess(x, 1200, str(point))
            self.assertGreater(y, 30, str(point))
            self.assertLess(y, 690, str(point))

    def test_foliage_and_shop_goods_are_true_reused_3d_meshes(self):
        street = STREET.read_text(encoding="utf-8")
        self.assertIn("public void PocketGarden(float x,float z,int seed)", street)
        self.assertIn("public void GroceryDelivery(float x,float z,int seed)", street)
        self.assertIn("PocketGarden(8.9f,17.0f,0);", street)
        self.assertIn("PocketGarden(29.1f,27.7f,7);", street)
        self.assertIn("GroceryDelivery(15.0f,-31.7f,0);", street)
        self.assertIn("GroceryDelivery(25.1f,-31.7f,1);", street)
        self.assertIn("NONATION_STYLIZED_STREET_READY gardens=2 deliveries=2", street)
        for token in ("new Mesh()", "MeshRenderer", "mesh.SetTriangles",
                      "petals.Crown(", "bark.Tube(", "woodPallet.Box("):
            self.assertIn(token, street)
        self.assertNotIn("Texture2D.LoadImage", street)
        self.assertNotIn("new BoxCollider()", street)
        self.assertNotIn(".height", street)  # guard against C# numeric typo
        # New props share existing by-material batches; no Unity primitives.
        self.assertNotIn("GameObject.CreatePrimitive", street)
        # The visible tree and flower crowns now have analytic normals
        # rather than one hard face normal per polygon.
        self.assertIn("static Vector3 EllipsoidNormal(", street)
        self.assertIn("void SmoothQuad(", street)
        self.assertIn("mesh.SetNormals(normals)", street)
        self.assertIn("normals.Add(EllipsoidNormal(a,center,radii))", street)

    def test_existing_gameplay_geometry_poi_and_routes_preserved(self):
        city = CITY.read_text(encoding="utf-8")
        for token in (
            'AddBusiness("market"', 'AddBusiness("cafe"',
            'AddBusiness("garage"', 'AddBusiness("town"',
            "MirWorldGeometry.SidewalkRoute",
            "MirWorldGeometry.HomeEntry",
            "Vehicles.Add(MirVehicle.Spawn",
            "MirKenneyCityArt",
            "MirModernDistrictIntegrator.TryActivate(root)",
            "MirCivicSquare.Build(root,Mat,lamps.Add)",
            "MirQuarterStreetLife.Build(root,Mat)",
        ):
            self.assertIn(token, city)
        self.assertIn('new Color(.41f,.55f,.34f)', city)
        self.assertIn('new Color(.77f,.755f,.70f)', city)
        self.assertIn('new Color(.235f,.265f,.270f)', city)
        self.assertIn('RenderSettings.fogDensity=night?0.006f:.0017f;', city)

    def test_surroundings_receive_real_per_pixel_light_shadows_and_roughness(self):
        s = SHADER.read_text(encoding="utf-8")
        for token in ("Shader \"MIR/RuntimeFallback\"", "_Color",
                      "_Metallic", "_Smoothness", "#pragma surface surf Standard",
                      "fullforwardshadows", "addshadow", "SurfaceOutputStandard",
                      "o.Metallic = saturate(_Metallic)",
                      "o.Smoothness = saturate(_Smoothness)"):
            self.assertIn(token, s)
        self.assertNotIn("#pragma vertex vert", s)
        self.assertNotIn("output.brightness =", s)

    def test_browser_qa_checks_real_3d_details_on_start_and_reload(self):
        s = BROWSER.read_text(encoding="utf-8")
        self.assertEqual(s.count("NONATION_STYLIZED_STREET_VERIFIED phase="), 2)
        self.assertIn("meta.stylizedStreet = true", s)
        self.assertIn("meta.stylizedStreetReloaded = true", s)
        self.assertIn("/\\bgardens=2\\b/", s)
        self.assertIn("/\\bdeliveries=2\\b/", s)
        self.assertIn("nonation-browser-showcase.png", s)


if __name__ == "__main__":
    unittest.main()
