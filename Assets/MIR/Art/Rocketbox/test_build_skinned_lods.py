"""Offline tests for LOD source provenance and safe failure; Blender is not required."""
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import build_skinned_lods as lod


class BlenderPipelineOfflineTests(unittest.TestCase):
    def test_known_git_blob(self):
        self.assertEqual(lod.git_blob_sha1(b"test\n"),
                         "9daeafb9864cf43055ae93beb0afd6c7d144bfa4")

    def test_monotonic_ratios(self):
        self.assertEqual(lod.validate_ratios((0.72, 0.48, 0.28)),
                         (0.72, 0.48, 0.28))
        for ratios in ((0.4, 0.6, 0.2), (0.5, 0.5, 0.2),
                       (0.5, 0.2), (0.8, 0.2, 0.08),
                       (0.8, 0.2, 0.99), (float("nan"), 0.4, 0.2)):
            with self.subTest(ratios=ratios), self.assertRaises(ValueError):
                lod.validate_ratios(ratios)

    @staticmethod
    def sample_folder(folder):
        (folder / "LICENSE.md").write_text("MIT License\nCopyright (c) 2020 Microsoft")
        (folder / "Textures").mkdir()
        details = {}
        for index, name in enumerate(lod.TEXTURE_NAMES):
            payload = bytes((index + 20,)) * 12
            (folder / "Textures" / name).write_bytes(payload)
            details[name] = {"sha256": hashlib.sha256(payload).hexdigest(),
                             "bytes": len(payload), "width": 1024, "height": 1024}
        meta = {"upstream_commit": "0943055db6ec570bcef9f2c8b41c9e5467c808f9",
                "textures": details}
        (folder / "Textures" / "textures_manifest.json").write_text(json.dumps(meta))
        source = folder / lod.SOURCE_NAME
        source.write_bytes(b"synthetic FBX fixture: no geometry, not a real FBX")
        return source

    def test_pinned_source_and_six_textures(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            source = self.sample_folder(folder)
            with patch.object(lod, "SOURCE_BLOB_SHA1", lod.git_blob_sha1(source.read_bytes())):
                result = lod.verify_source(folder, source)
                self.assertEqual(result["source_blob_sha1"],
                                 lod.git_blob_sha1(source.read_bytes()))
                self.assertEqual(len(result["texture_manifest_sha256"]), 64)

    def test_tampered_or_missing_texture_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            source = self.sample_folder(folder)
            with patch.object(lod, "SOURCE_BLOB_SHA1", lod.git_blob_sha1(source.read_bytes())):
                name = lod.TEXTURE_NAMES[0]
                (folder / "Textures" / name).write_bytes(b"tampered")
                with self.assertRaisesRegex(ValueError, "checksum mismatch"):
                    lod.verify_source(folder, source)
                (folder / "Textures" / name).unlink()
                with self.assertRaisesRegex(ValueError, "Missing texture"):
                    lod.verify_source(folder, source)

    def test_invalid_original_or_license_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            source = self.sample_folder(folder)
            with self.assertRaisesRegex(ValueError, "Git blob"):
                lod.verify_source(folder, source)
            with patch.object(lod, "SOURCE_BLOB_SHA1", lod.git_blob_sha1(source.read_bytes())):
                (folder / "LICENSE.md").unlink()
                with self.assertRaisesRegex(ValueError, "MIT license"):
                    lod.verify_source(folder, source)

    def test_no_blender_never_claims_generated_lods(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaisesRegex(RuntimeError, "Blender background executable"):
                lod.run_blender(Path(tmp) / lod.SOURCE_NAME,
                                Path(tmp) / "GeneratedLODs", lod.DEFAULT_RATIOS)


if __name__ == "__main__":
    unittest.main()
