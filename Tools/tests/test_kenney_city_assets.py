"""Regressions for pinned Kenney CC0 city asset integration.

Static tests verify provenance, resource paths and a real GLB2/Git-blob hash
contract. They do NOT substitute for Unity Editor, WebGL or visual acceptance.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import unittest

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Tools/kenney_city_manifest.json"
FETCHER = ROOT / "Tools/fetch_kenney_city.py"


def get_fetcher():
    spec = importlib.util.spec_from_file_location("kenney_city_fetcher", FETCHER)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class KenneyCityAssetsTests(unittest.TestCase):
    def test_manifest_pins_immutable_known_glb_sources(self):
        manifest = json.loads(MANIFEST.read_text())
        self.assertEqual(manifest["schema"], "nonation-kenney-city/1")
        self.assertEqual(manifest["upstream"], "Hidencod/tge-assets")
        self.assertEqual(manifest["original_license"], "CC0-1.0")
        self.assertRegex(manifest["revision"], r"^[0-9a-f]{40}$")
        records = list(get_fetcher().checked_records())
        self.assertEqual(len(records), 14)
        self.assertEqual(len({x["id"] for _, x in records}), 14)
        categories = {x["id"].split("/")[0] for _, x in records}
        self.assertEqual(categories, {"city-kit-commercial", "nature-kit", "car-kit", "city-kit-roads"})
        self.assertTrue(all(isinstance(x["size"], int) and x["size"] > 12 for _, x in records))

    def test_glb_header_and_git_blob_sha_are_verified(self):
        fetcher = get_fetcher()
        body = b"glTF" + struct.pack("<I", 2) + struct.pack("<I", 20) + b"abcdefgh"
        blob_sha = hashlib.sha1(b"blob 20\0" + body).hexdigest()
        model = {"id": "city-kit-commercial/example", "size": 20, "git_blob_sha1": blob_sha}
        self.assertTrue(fetcher.verify(body, model))
        with self.assertRaises(ValueError):
            fetcher.verify(body[:4] + b"\x01\x00\x00\x00" + body[8:], model)
        with self.assertRaises(ValueError):
            fetcher.verify(body[:-1] + b"z", model)
        with self.assertRaises(ValueError):
            fetcher.verify(body[:-1], model)

    def test_runtime_dresses_but_keeps_existing_gameplay_and_colliders(self):
        city = (ROOT / "Assets/MIR/Scripts/MirCity.cs").read_text()
        art = (ROOT / "Assets/MIR/Art/Kenney/MirKenneyCityArt.cs").read_text()
        self.assertIn('art.AddComponent<MirKenneyCityArt>()', city)
        self.assertIn('Legacy building · ', city)
        self.assertIn('Legacy Tree ', city)
        self.assertIn('GetComponentsInChildren<Renderer>(true)', art)
        self.assertIn('GltfImport', art)
        self.assertIn('Resources.Load<GameObject>', art)
        self.assertIn('NONATION_KENNEY_CITY_READY', art)
        self.assertIn('MirGame.Instance.City.Vehicles', art)
        self.assertIn('car.transform', art)
        manifest = json.loads(MANIFEST.read_text())
        for model in manifest["models"]:
            self.assertIn('"'+model["id"]+'"', art, model["id"])

    def test_build_prepares_identical_assets_for_both_cloud_and_actions(self):
        prebuild = (ROOT / "Tools/unity_cloud_prebuild.sh").read_text()
        workflow = (ROOT / ".github/workflows/unity-webgl.yml").read_text()
        self.assertIn("python3 Tools/fetch_kenney_city.py", prebuild)
        self.assertIn("python3 Tools/fetch_kenney_city.py", workflow)
        self.assertIn("python3 Tools/fetch_kenney_city.py --verify-only", workflow)


if __name__ == "__main__":
    unittest.main()
