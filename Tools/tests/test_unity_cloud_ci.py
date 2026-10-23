#!/usr/bin/env python3
"""Offline regression checks for authenticated Unity Cloud artifact hand-off."""
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / "unity_cloud_ci.py"
spec = importlib.util.spec_from_file_location("unity_cloud_ci", SCRIPT)
cloud = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cloud)

class CloudBuildTests(unittest.TestCase):
    def test_single_build_number(self):
        self.assertEqual(cloud.build_number({"build": 42}), 42)
        self.assertEqual(cloud.build_number([{"build": 3}]), 3)
        with self.assertRaises(ValueError):
            cloud.build_number([{"build": 2}, {"build": 3}])

    def test_revision(self):
        sha = "a" * 40
        self.assertTrue(cloud.matches_revision({"requestedRevision": sha}, sha))
        self.assertTrue(cloud.matches_revision({"changeset": [{"commitId": sha}]}, sha))
        self.assertFalse(cloud.matches_revision({"requestedRevision": "b" * 40}, sha))

    def test_zip_unpack_and_validation(self):
        with tempfile.TemporaryDirectory() as work:
            w = Path(work)
            archive = w / "cloud.zip"
            with zipfile.ZipFile(archive, "w") as z:
                z.writestr("WebGL/index.html", "<html>game</html>")
                z.writestr("WebGL/Build/game.wasm", "wasm")
                z.writestr("WebGL/Build/game.data", "data")
                z.writestr("WebGL/Build/game.js", "js")
            cloud.unpack_webgl(archive, w / "out")
            self.assertTrue((w / "out" / "index.html").is_file())
            self.assertTrue((w / "out" / "Build" / "game.wasm").is_file())

    def test_no_empty_artifact(self):
        with tempfile.TemporaryDirectory() as work:
            w = Path(work)
            archive = w / "incomplete.zip"
            with zipfile.ZipFile(archive, "w") as z:
                z.writestr("index.html", "ok")
            with self.assertRaises(ValueError):
                cloud.unpack_webgl(archive, w / "out")

    def test_no_traversal(self):
        with tempfile.TemporaryDirectory() as work:
            w = Path(work)
            archive = w / "malicious.zip"
            with zipfile.ZipFile(archive, "w") as z:
                z.writestr("WebGL/index.html", "ok")
                z.writestr("WebGL/Build/game.wasm", "wasm")
                z.writestr("WebGL/Build/game.data", "data")
                z.writestr("WebGL/Build/game.js", "js")
                z.writestr("../outside.txt", "danger")
            with self.assertRaises(ValueError):
                cloud.unpack_webgl(archive, w / "out")
            self.assertFalse((w / "outside.txt").exists())

if __name__ == "__main__":
    unittest.main()
