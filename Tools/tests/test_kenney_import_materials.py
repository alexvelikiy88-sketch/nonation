"""Guard against the exact glTFast material generator failure seen in live WebGL.

Static C# assertions and real Node classifier tests; NOT a Unity compile
or confirmation that Kenney meshes now load in an actual browser.
"""
from pathlib import Path
import shutil
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/MIR/Art/Kenney/MirKenneyCityArt.cs"
SMOKE = ROOT / "Tools/browser_smoke_preview.js"
GUARD = ROOT / "Tools/webgl_runtime_guard.js"


class KenneyMaterialGenerationTests(unittest.TestCase):
    def test_builtin_pipeline_uses_explicit_gltfast_material_generator(self):
        art = ART.read_text(encoding="utf-8")
        self.assertIn("using GLTFast.Materials;", art)
        self.assertIn("using UnityEngine.Rendering;", art)
        self.assertIn("GraphicsSettings.currentRenderPipeline == null", art)
        self.assertIn("new BuiltInMaterialGenerator()", art)
        self.assertIn("MaterialGenerator.GetDefaultMaterialGenerator()", art)
        self.assertRegex(art, r"new\s+GltfImport\(materialGenerator:\s*materialGenerator,\s*logger:\s*new\s+ConsoleLogger\(\)\)")
        self.assertNotIn("gltf = new GltfImport();", art)

    def test_chromium_checks_unity_errors_during_marker_wait(self):
        smoke = SMOKE.read_text(encoding="utf-8")
        self.assertIn("Fatal Unity browser error awaiting ", smoke)
        self.assertIn("criticalRuntimeMessages([...errors, ...allMessages])", smoke)
        self.assertLess(smoke.index("const fatal = criticalRuntimeMessages"),
                        smoke.index("const value = messages.find"))

    @unittest.skipUnless(shutil.which("node"), "Node.js unavailable")
    def test_real_classifier_rejects_gltf_material_generator_error(self):
        script = r"""
const assert = require('node:assert/strict');
const { criticalRuntimeMessages } = require('./Tools/webgl_runtime_guard');
const bad = [
  'NONATION_KENNEY_CITY_ERROR: Could not determine default MaterialGenerator (render pipeline BuiltIn)',
  'NONATION_KENNEY_FETCH_FAILED: city-kit-commercial/building-a HTTP 404',
  'NONATION_KENNEY_IMPORT_FAILED: city-kit-commercial/building-b',
  "Can't add component because class 'CapsuleCollider' doesn't exist!",
  'InvalidOperationException: Missing runtime shader'
];
assert.equal(criticalRuntimeMessages(bad).length, bad.length);
assert.deepEqual(criticalRuntimeMessages([
  'NONATION_RUNTIME_READY scene=MIR_PlayableDistrict player=player-001 npcs=39',
  'NONATION_SHADER_FALLBACK: bundled fallback shader used'
]), []);
"""
        result = subprocess.run(
            [shutil.which("node"), "-e", script], cwd=ROOT,
            capture_output=True, text=True, timeout=15,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        for src in ("Tools/browser_smoke_preview.js",
                    "Tools/webgl_runtime_guard.js",
                    "Tools/kenney_runtime_counts.js"):
            check = subprocess.run(
                [shutil.which("node"), "--check", src], cwd=ROOT,
                capture_output=True, text=True, timeout=15,
            )
            self.assertEqual(check.returncode, 0, check.stderr)


if __name__ == "__main__":
    unittest.main()
