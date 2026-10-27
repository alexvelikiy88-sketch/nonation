"""Regression for the complete sourced brick/plaster/wood PBR asset chain.

This validates immutable CC0 Git objects and source plumbing, not Unity shader
compilation, visible normal/roughness response or visual gate acceptance.
"""
from pathlib import Path
import hashlib
import unittest

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/MIR/Art/Architecture"
SCANS = ART / "Resources/Nonation/Architecture/Scans"
SHADER = ART / "Resources/Nonation/Architecture/ArchitecturalLit.shader"
CITY = ART / "MirArchitectureQuarter.cs"
IMPORT = ART / "Editor/MirArchitectureScanImport.cs"

# Existing AGENT 03 graphics Git objects, source ambientCG CC0,
# imported byte-for-byte from agent/graphics-ui@35d73180e22d...
SCANNED = {
    "Bricks097_Color.jpg": ("cb777108b6e9e14f7054d2a6d6318f560c57163a", 831880),
    "Bricks097_NormalGL.jpg": ("760bbc8c3c34f81e00ab3689aa4ef52025cf3bdb", 907097),
    "Bricks097_Roughness.jpg": ("e51c9f6931b1d03a52a655a08ff1aa1ef58555ed", 386766),
    "Planks023A_Color.jpg": ("e5e65712b9250546bceb146352aae4ea9d809691", 910213),
    "Planks023A_Roughness.jpg": ("6899cb7c995087b6a5d81253c1468c3d148ff7eb", 331601),
    "Plaster007_ColorEven.jpg": ("574d19144d1d0178b5b777eae02998e8da2b2403", 263321),
    "Plaster007_NormalSmoothGL.jpg": ("872c4d77d1e0d6b836fa90b459f287a6173af7ad", 731051),
    "Plaster007_Roughness.jpg": ("d27fd970cdef15c538ed6a24e88e88fa6fc34aec", 738576),
}


class ArchitecturePbrTests(unittest.TestCase):
    def test_all_eight_verified_ambientcg_maps_preserved(self):
        for name, (git_sha, length) in SCANNED.items():
            data = (SCANS / name).read_bytes()
            self.assertEqual(len(data), length, name)
            digest = hashlib.sha1(f"blob {len(data)}\0".encode() + data).hexdigest()
            self.assertEqual(digest, git_sha, name)
            self.assertTrue(data.startswith(b"\xff\xd8\xff"), name)

    def test_surface_shader_uses_real_tangent_normals_and_linear_roughness(self):
        code = SHADER.read_text(encoding="utf-8")
        for token in ("Shader \"NONATION/ArchitecturalLit\"",
                      "_NormalMap", "_RoughnessMap",
                      "_UseNormal", "_UseRoughness",
                      "UnpackNormal", "uv_NormalMap", "uv_RoughnessMap",
                      "SurfaceOutputStandard",
                      "#pragma surface surf Standard"):
            self.assertIn(token, code)
        self.assertIn("1.0h-rough", code)
        self.assertNotIn("Universal Render Pipeline/Lit", code)

    def test_editor_import_sets_linear_data_and_mipmaps(self):
        code = IMPORT.read_text(encoding="utf-8")
        self.assertIn("TextureImporterType.NormalMap", code)
        self.assertIn("texture.sRGBTexture = !(normal || roughness);", code)
        self.assertIn("texture.mipmapEnabled = true;", code)
        self.assertIn("texture.wrapMode = TextureWrapMode.Repeat;", code)
        self.assertIn("texture.maxTextureSize = 2048;", code)

    def test_runtime_binds_all_maps_without_distorting_uv_scale(self):
        code = CITY.read_text(encoding="utf-8")
        self.assertIn('Resources.Load<Texture2D>("Nonation/Architecture/Scans/"+name)', code)
        self.assertIn('found.SetTexture("_NormalMap",normal)', code)
        self.assertIn('found.SetTexture("_RoughnessMap",rough)', code)
        self.assertIn('found.SetTextureScale("_NormalMap",new Vector2(.62f,.62f))', code)
        self.assertIn('found.SetTextureScale("_RoughnessMap",new Vector2(.62f,.62f))', code)
        self.assertIn('found.SetFloat("_UseNormal",1f)', code)
        self.assertIn('found.SetFloat("_UseRoughness",1f)', code)
        self.assertIn("mesh.RecalculateTangents()", code)
        self.assertNotIn("GraphicsFormatUtility.IsSRGBFormat", code)


if __name__ == "__main__":
    unittest.main()
