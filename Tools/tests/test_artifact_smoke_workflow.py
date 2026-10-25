"""Contract for artifact-only Chromium proof: use exact main build, not old preview."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
FLOW = ROOT / ".github/workflows/webgl-artifact-browser-smoke.yml"


class ArtifactSmokeWorkflowTests(unittest.TestCase):
    def test_only_upstream_main_unity_workflow_supplies_artifact(self):
        data = FLOW.read_text(encoding="utf-8")
        trigger = data.split("on:", 1)[1].split("\npermissions:", 1)[0]
        self.assertIn("workflow_run:", trigger)
        self.assertIn("workflows: ['MIR Unity WebGL']", trigger)
        self.assertNotIn("\n  push:", trigger)
        self.assertIn('source_run="$UPSTREAM_RUN"', data)
        self.assertIn('source_sha="$UPSTREAM_SHA"', data)
        self.assertIn('git merge-base --is-ancestor "$source_sha" HEAD', data)
        self.assertIn("NONATION_REQUIRE_KENNEY_ART: '1'", data)
        self.assertNotIn("source_run=40015140424", data)
        self.assertNotIn("cb46b4960dd8bcd255880f569254dfe9876ea14b", data)

    def test_it_is_independent_of_pages_and_avoids_new_cloud_builds(self):
        data = FLOW.read_text(encoding="utf-8")
        self.assertIn("actions/download-artifact@v4", data)
        self.assertIn("node Tools/browser_smoke_preview.js", data)
        self.assertNotIn("tools/unity_cloud_ci.py --build", data)
        self.assertIn("nonation-browser-proof-", data)


if __name__ == "__main__":
    unittest.main()
