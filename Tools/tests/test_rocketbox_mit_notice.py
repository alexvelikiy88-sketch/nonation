"""Regression: Unity WebGL builds must retain the exact Microsoft MIT notice.

Microsoft Rocketbox upstream:
https://github.com/microsoft/Microsoft-Rocketbox/tree/0943055db6ec570bcef9f2c8b41c9e5467c808f9
Builder may stage .md differently and Windows checkouts normalize line endings.
Never bypass actual MIT notice contents to make the pilot look successful.
"""
from pathlib import Path
import hashlib
import unittest

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Assets/MIR/Art/Rocketbox"
SOURCE = BASE / "LICENSE.md"
BUNDLED = BASE / "Resources/MIR/RocketboxMITNotice.txt"
EDITOR = BASE / "Editor/MirRocketboxPilotBuild.cs"
EXPECTED = "9bcfb3ece5301a55d3a41bbd00a029ab27d61d13"


def git_blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()


class MicrosoftMITBuildTests(unittest.TestCase):
    def test_exact_license_is_embedded_for_unity_webgl(self):
        original = SOURCE.read_bytes()
        bundled = BUNDLED.read_bytes()
        self.assertEqual(original, bundled)
        self.assertEqual(git_blob(original), EXPECTED)
        self.assertIn(b"Copyright (c) 2020 Microsoft", original)
        self.assertIn(b"THE SOFTWARE IS PROVIDED", original)

    def test_only_line_endings_can_be_normalized(self):
        original = SOURCE.read_bytes()
        crlf = original.replace(b"\n", b"\r\n")
        self.assertEqual(git_blob(crlf.replace(b"\r\n", b"\n")), EXPECTED)
        self.assertNotEqual(git_blob(crlf), EXPECTED)
        self.assertNotEqual(git_blob(original.replace(b"Microsoft", b"NotMicrosoft")),
                            EXPECTED)

    def test_editor_requires_source_if_present_and_bundled_notice_always(self):
        code = EDITOR.read_text(encoding="utf-8")
        for part in (
            'const string BundledLicense=Base+"/Resources/MIR/RocketboxMITNotice.txt";',
            "GitBlobNormalizedMIT(License)==SourceLicenseGitBlob",
            "GitBlobNormalizedMIT(BundledLicense)==SourceLicenseGitBlob",
            "Require(File.Exists(BundledLicense) &&",
            "mainLicensePresent",
            'text.Replace("\\r\\n","\\n")',
            "NONATION_ROCKETBOX_SOURCE_LICENSE_STAGED_OUT",
        ):
            self.assertIn(part, code)
        self.assertIn("GitBlob(Model)==SourceModelGitBlob", code)
        self.assertIn("Sha256(path)==entry.Value", code)
        self.assertIn("NONATION_ROCKETBOX_BLOCKED", code)
        self.assertNotIn("SkipLicenseCheck", code)
        self.assertNotIn("if(false)", code)


if __name__ == "__main__":
    unittest.main()
