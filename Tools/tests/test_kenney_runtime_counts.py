"""Regression tests for first-load and post-reload Kenney 3D counters.

Python discovers this suite in source-validation; Node executes the same pure
parser used by Chromium. No Unity Editor, mock canvas, or asset claims are made.
"""
from pathlib import Path
import shutil
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]


class KenneyRuntimeCounterTests(unittest.TestCase):
    @unittest.skipUnless(shutil.which("node"), "Node.js is unavailable")
    def test_real_counter_parser_accepts_only_complete_expected_counts(self):
        script = r"""
const assert = require('node:assert/strict');
const { requireKenneyCityReady: parse, EXPECTED } =
  require('./Tools/kenney_runtime_counts');
const valid = 'NONATION_KENNEY_CITY_READY buildings=12 trees=13 vehicles=3 props=2 (true glTF meshes)';
assert.deepEqual(parse(valid), EXPECTED);
const bad = [
  'NONATION_RUNTIME_READY buildings=12 trees=13 vehicles=3 props=2',
  'NONATION_KENNEY_CITY_READY buildings=11 trees=13 vehicles=3 props=2',
  'NONATION_KENNEY_CITY_READY buildings=12 trees=13 vehicles=3 props=1',
  'NONATION_KENNEY_CITY_READY buildings=12 trees=13 vehicles=3',
  'NONATION_KENNEY_CITY_READY buildings=12 trees=13 vehicles=three props=2'
];
for (const message of bad) assert.throws(() => parse(message));
"""
        done = subprocess.run(
            [shutil.which("node"), "-e", script],
            cwd=ROOT, capture_output=True, text=True, timeout=15,
        )
        self.assertEqual(done.returncode, 0, done.stderr)
        syntax = subprocess.run(
            [shutil.which("node"), "--check", "Tools/browser_smoke_preview.js"],
            cwd=ROOT, capture_output=True, text=True, timeout=15,
        )
        self.assertEqual(syntax.returncode, 0, syntax.stderr)

    def test_chromium_rechecks_art_after_saved_game_reload(self):
        code = (ROOT / "Tools/browser_smoke_preview.js").read_text(encoding="utf-8")
        self.assertIn("meta.kenneyArt = requireKenneyCityReady", code)
        self.assertIn("meta.kenneyArtReloaded = requireKenneyCityReady", code)
        self.assertLess(code.index("await page.reload("),
                        code.index("meta.kenneyArtReloaded = requireKenneyCityReady"))
        self.assertLess(code.index("messages.length = 0;"),
                        code.index("await page.reload("))


if __name__ == "__main__":
    unittest.main()
