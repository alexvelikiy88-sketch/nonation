"""Source regressions for Unity's actual production WebGL HTML template.

Browser visual acceptance requires the exported index.html and Chromium
screenshot; these tests are NOT Unity compilation or a browser proof.
"""
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
TEMPLATE = ROOT / "Assets/WebGLTemplates/Nonation/index.html"
BUILD = ROOT / "Assets/MIR/Editor/MirCloudBuild.cs"
HOOK = ROOT / "Assets/MIR/Editor/MirNonationWebTemplatePrebuild.cs"


class ImmersiveWebGLTemplateTests(unittest.TestCase):
    def test_webgl_uses_project_template_with_no_default_white_frame(self):
        page = TEMPLATE.read_text(encoding="utf-8")
        code = BUILD.read_text(encoding="utf-8")
        self.assertIn('PlayerSettings.WebGL.template = "PROJECT:Nonation";', code)
        prebuild = HOOK.read_text(encoding="utf-8")
        self.assertIn("IPreprocessBuildWithReport", prebuild)
        self.assertIn("OnPreprocessBuild(BuildReport report)", prebuild)
        self.assertIn("report.summary.platform != BuildTarget.WebGL", prebuild)
        self.assertIn("NONATION_WEBGL_TEMPLATE_MISSING", prebuild)
        self.assertIn("NONATION_WEBGL_TEMPLATE_INVALID", prebuild)
        self.assertIn('PlayerSettings.WebGL.template = TemplateName;', prebuild)
        self.assertIn("NONATION_WEBGL_FULLSCREEN_TEMPLATE_SELECTED", prebuild)
        self.assertIn("NONATION_WEBGL_TEMPLATE_MISSING", code)
        self.assertIn("BuildPipeline.BuildPlayer(options)", code)
        self.assertIn('<canvas id="unity-canvas"', page)
        self.assertIn("position: fixed; inset: 0;", page)
        self.assertIn("width: 100vw; height: 100vh;", page)
        self.assertIn("height: 100dvh;", page)
        self.assertNotIn("unity-footer", page)
        self.assertNotIn("unity-logo", page)

    def test_exact_unity_preprocessor_symbols_and_failure_path(self):
        page = TEMPLATE.read_text(encoding="utf-8")
        for key in ("LOADER_FILENAME", "DATA_FILENAME", "FRAMEWORK_FILENAME",
                    "CODE_FILENAME", "COMPANY_NAME", "PRODUCT_NAME",
                    "PRODUCT_VERSION"):
            self.assertIn("{{{ " + key + " }}}", page)
        self.assertIn("createUnityInstance(canvas, config", page)
        self.assertIn('document.body.appendChild(script);', page)
        self.assertIn('script.onerror = function', page)
        self.assertIn(".catch(function (exception)", page)
        self.assertIn('role="alert"', page)
        self.assertIn('canvas.focus({ preventScroll: true })', page)
        # GitHub Pages host has its own HTTPS static content, no trackers.
        self.assertNotIn('https://', page)
        self.assertNotIn('http://', page)

    @unittest.skipUnless(shutil.which("node"), "Node.js is unavailable")
    def test_embedded_loader_javascript_has_valid_syntax(self):
        page = TEMPLATE.read_text(encoding="utf-8")
        matches = re.findall(r'<script>(.*?)</script>', page, flags=re.DOTALL)
        self.assertEqual(len(matches), 1)
        with tempfile.TemporaryDirectory() as temp:
            js = Path(temp) / "nonation_template.js"
            js.write_text(matches[0], encoding="utf-8")
            result = subprocess.run(
                [shutil.which("node"), "--check", str(js)],
                capture_output=True, text=True, timeout=15,
            )
            self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
