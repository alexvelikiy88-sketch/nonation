"""Static regression checks for NONATION's real-browser city-art release gate.

These checks verify that the hosted Chromium smoke *requires* the Kenney
runtime mesh marker, fails stripped native colliders, and uploads a screenshot.
They are not substitutes for a real browser/Unity graphics test.
"""
from pathlib import Path
import re
import shutil
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github/workflows/unity-webgl.yml"
PREVIEW = ROOT / "Tools/browser_smoke_preview.js"
GUARD = ROOT / "Tools/webgl_runtime_guard.js"


class KenneyWebglProofContract(unittest.TestCase):
    def test_release_checks_full_3d_city_and_retains_visual_proof(self):
        workflow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("browser-smoke:", workflow)
        section = workflow.split("  browser-smoke:", 1)[1]
        self.assertIn("actions/checkout@v4", section)
        self.assertIn("if: always() && needs.deploy.result == 'success'", section)
        self.assertIn("NONATION_REQUIRE_KENNEY_ART: '1'", section)
        self.assertIn("node Tools/browser_smoke_preview.js", section)
        self.assertIn("nonation-browser-proof.png", section)
        self.assertIn("nonation-browser-proof.json", section)
        self.assertIn("if: always()", section)

    def test_art_marker_and_exact_loaded_instance_counts(self):
        code = PREVIEW.read_text(encoding="utf-8")
        self.assertIn("NONATION_KENNEY_CITY_READY", code)
        self.assertIn("counts.buildings !== 12", code)
        self.assertIn("counts.trees !== 13", code)
        self.assertIn("counts.vehicles !== 3", code)
        self.assertIn("counts.props !== 2", code)
        self.assertIn("KENNEY_RUNTIME_3D_VERIFIED", code)
        self.assertIn("meta.kenneyArt = counts", code)

    def test_stripping_detection_includes_nonerror_unity_console(self):
        code = PREVIEW.read_text(encoding="utf-8")
        self.assertIn("require('./webgl_runtime_guard')", code)
        self.assertIn("allMessages.push(line)", code)
        self.assertIn("criticalRuntimeMessages([...errors, ...allMessages])", code)
        self.assertIn("Fatal Unity browser error", code)
        self.assertIn("BoxCollider", GUARD.read_text(encoding="utf-8"))
        self.assertIn("CapsuleCollider", GUARD.read_text(encoding="utf-8"))

    @unittest.skipUnless(shutil.which("node"), "Node.js unavailable")
    def test_chromium_script_syntax(self):
        subprocess.run(["node", "--check", str(PREVIEW)], check=True,
                       capture_output=True, text=True)


if __name__ == "__main__":
    unittest.main()
