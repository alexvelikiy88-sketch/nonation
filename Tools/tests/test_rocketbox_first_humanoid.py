"""VIS-03 pilot: validate Microsoft MIT source and a single real Humanoid path.

Static/source checks are not Unity Avatar, skin deformation or WebGL proof.
The explicit visual-release flag remains fail-closed if the pilot cannot import.
"""
from pathlib import Path
import hashlib
import unittest

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Assets/MIR/Art/Rocketbox"
EDITOR = BASE / "Editor/MirRocketboxPilotBuild.cs"
FACTORY = ROOT / "Assets/MIR/Scripts/MirHumanFactory.cs"
MODEL = BASE / "Male_Adult_12.fbx"
LICENSE = BASE / "LICENSE.md"

TEXTURE_HASHES = {
    "m007_body_color.tga": "e14e258e5b173f8e47f2cf3726191ae388b82daec2e4788c973793fd8f16e5b2",
    "m007_body_normal.tga": "5bc98dd7d0590dd4b25efee6a62acb22e228c4097b65bf422cb24de31aea8487",
    "m007_body_specular.tga": "055523b34fd4927d6723bc2d79d7d13ec8ce4908024145cc943cb30af422620c",
    "m007_head_color.tga": "84bcb874c815833bf0de6bdb85237ca46bd771750ba30ce84b6d526982ee70e0",
    "m007_head_normal.tga": "b0588b2294a993ea114ac029a55e6cd3c0e7315f803c6894344d732f1fb09810",
    "m007_head_specular.tga": "a68c568465d06b4cf151d361c3e1c8f4ed9c09eb47640d7350228207b2e445cd",
}


def git_blob(data):
    return hashlib.sha1(f"blob {len(data)}\0".encode() + data).hexdigest()


class RealHumanoidPilotTests(unittest.TestCase):
    def test_original_fbx_and_mit_notice_pinned(self):
        self.assertEqual(git_blob(MODEL.read_bytes()),
                         "15d4f8501d5703600cb3d8a94765a3de1029aaf6")
        self.assertEqual(git_blob(LICENSE.read_bytes()),
                         "9bcfb3ece5301a55d3a41bbd00a029ab27d61d13")
        self.assertEqual(len(TEXTURE_HASHES), 6)
        for name, digest in TEXTURE_HASHES.items():
            path = BASE / "Textures" / name
            self.assertEqual(path.stat().st_size, 3145746)
            self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),
                             digest, name)

    def test_humanoid_rig_and_animation_import_contract(self):
        s = EDITOR.read_text(encoding="utf-8")
        for token in (
            "ModelImporterAnimationType.Human",
            "ModelImporterAvatarSetup.CreateFromThisModel",
            "skin.bones.Length>=40",
            "skin.sharedMesh.bindposes.Length==skin.bones.Length",
            'CreateMaterial("body")',
            'CreateMaterial("head")',
            'tree.AddChild(idle,0f)',
            'tree.AddChild(walk,.42f)',
            'tree.AddChild(run,1f)',
            "PrefabUtility.SaveAsPrefabAsset(instance,Output)",
            "NONATION_ROCKETBOX_READY",
        ):
            self.assertIn(token, s)

    def test_visual_release_fails_closed_but_alpha_preserved(self):
        s = EDITOR.read_text(encoding="utf-8")
        self.assertIn("bool pilotReady = MirRocketboxPilotBuild.Prepare(false);", s)
        self.assertIn("NONATION_REQUIRE_REAL_HUMANOID", s)
        self.assertIn("throw new BuildFailedException(", s)
        self.assertIn("NONATION_REAL_HUMAN_PILOT_UNAVAILABLE", s)
        self.assertIn("AssetDatabase.DeleteAsset(Output)", s)

    def test_only_player_prefers_pilot_and_does_not_clone_npcs(self):
        s = FACTORY.read_text(encoding="utf-8")
        self.assertIn("if(person.player)", s)
        self.assertIn('candidates.Add("MIR/RocketboxPilot01")', s)
        self.assertIn("HasPlayableRig(instance,animator,out reason)", s)
        self.assertIn("NONATION_REAL_HUMAN_PILOT_OK", s)
        self.assertIn("skinnedVertices=", s)
        self.assertIn("lodGroups=", s)
        self.assertIn('new GameObject("MIR · Articulated Humanoid Fallback")', s)


if __name__ == "__main__":
    unittest.main()
