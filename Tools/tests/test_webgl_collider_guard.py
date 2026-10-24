"""Regression contract for native Unity primitive colliders and real browser error gate.
Static XML + actual Node classifier tests; not a Unity/IL2CPP compilation.
"""
from pathlib import Path
import json
import shutil
import subprocess
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
LINK = ROOT / "Assets" / "MIR" / "link.xml"
DETECTOR = ROOT / "Tools" / "webgl_runtime_guard.js"
SMOKE = ROOT / "Tools" / "browser_smoke_preview.js"


class ColliderPreservationTests(unittest.TestCase):
    def test_linker_preserves_primitive_native_colliders(self):
        self.assertTrue(LINK.is_file())
        root = ET.parse(LINK).getroot()
        self.assertEqual(root.tag, "linker")
        assembly = root.find("./assembly[@fullname='UnityEngine.PhysicsModule']")
        self.assertIsNotNone(assembly)
        present = {
            el.attrib["fullname"]
            for el in assembly.findall("type")
            if el.attrib.get("preserve") == "all"
        }
        self.assertTrue(
            {"UnityEngine.BoxCollider", "UnityEngine.CapsuleCollider",
             "UnityEngine.SphereCollider"}.issubset(present)
        )

    def test_browser_smoke_consumes_both_console_and_page_errors(self):
        text = SMOKE.read_text(encoding="utf-8")
        self.assertIn("require('./webgl_runtime_guard')", text)
        self.assertIn("const allMessages = [];", text)
        self.assertIn("allMessages.push(line);", text)
        self.assertIn("criticalRuntimeMessages([...errors, ...allMessages])", text)
        self.assertIn("throw new Error('Fatal Unity browser error:", text)
        self.assertIn("NONATION_RUNTIME_READY", text)

    @unittest.skipUnless(shutil.which("node"), "Node.js unavailable")
    def test_real_node_classifier_rejects_known_unity_failures(self):
        js = ("const g=require(process.argv[1]);"
              "process.stdout.write(JSON.stringify(g.criticalRuntimeMessages(JSON.parse(process.argv[2]))));")
        samples = [
            "Can not add component 'SphereCollider' because it doesn't exist!",
            "Can't add component CapsuleCollider because the class doesn't exist.",
            "Failed to create BoxCollider component in IL2CPP.",
            "UnityEngine.CapsuleCollider has been stripped",
            "InvalidOperationException: Missing runtime shader",
            "wasm trap: unreachable",
            "NONATION_RUNTIME_READY scene=MIR_PlayableDistrict npcs=39",
            "normal traffic BoxCollider debug info",
        ]
        process = subprocess.run(
            ["node", "-e", js, str(DETECTOR), json.dumps(samples)],
            capture_output=True, text=True, check=True,
        )
        failures = json.loads(process.stdout)
        self.assertEqual(failures, samples[:6])

    @unittest.skipUnless(shutil.which("node"), "Node.js unavailable")
    def test_browser_script_has_valid_javascript_syntax(self):
        subprocess.run(["node", "--check", str(SMOKE)], check=True,
                       capture_output=True, text=True)


if __name__ == "__main__":
    unittest.main()
