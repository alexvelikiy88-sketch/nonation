"""Real distinct skinned Microsoft MIT adults, original source and locomotion gate.

Static QA does NOT claim human facial likeness/animation skin deformation or
browser visual acceptance, which requires a real Unity/WebGL screenshot.
"""
from pathlib import Path
import json
import unittest
import importlib.util

ROOT=Path(__file__).resolve().parents[2]
MANIFEST=ROOT/"Tools/rocketbox_cast_manifest.json"
FETCH=ROOT/"Tools/fetch_rocketbox_cast.py"
PREBUILD=ROOT/"Tools/unity_cloud_prebuild.sh"
CI=ROOT/".github/workflows/unity-webgl.yml"
CAST=ROOT/"Assets/MIR/Art/Rocketbox/Editor/MirRocketboxCastBuild.cs"
FACTORY=ROOT/"Assets/MIR/Scripts/MirHumanFactory.cs"
BROWSER=ROOT/"Tools/browser_smoke_preview.js"
PILOT=ROOT/"Assets/MIR/Art/Rocketbox/Editor/MirRocketboxPilotBuild.cs"

class TwoOriginalMITAdults(unittest.TestCase):
    def test_pin_source_fbx_skin_variants_and_mit_notice(self):
        m=json.loads(MANIFEST.read_text(encoding="utf8"))
        self.assertEqual(m["schema"],"nonation/microsoft-rocketbox-cast/1")
        self.assertEqual(m["source_repo"],"microsoft/Microsoft-Rocketbox")
        self.assertEqual(m["source_sha"],"0943055db6ec570bcef9f2c8b41c9e5467c808f9")
        self.assertEqual(m["license"],"MIT")
        self.assertEqual(m["max_texture_edge"],1024)
        self.assertEqual(m["original_license_blob"],
            "9bcfb3ece5301a55d3a41bbd00a029ab27d61d13")
        self.assertEqual({c["id"] for c in m["characters"]},
                         {"Male_Adult_16","Female_Adult_16"})
        self.assertEqual(len(m["files"]),11)
        lookup={f["source_path"]:f for f in m["files"]}
        self.assertEqual(
            lookup["Assets/Avatars/Adults/Male_Adult_16/Export/Male_Adult_16.fbx"]["sha1"],
            "c4e2ede0ba75005741094533e964c04a323a79b0")
        self.assertEqual(
            lookup["Assets/Avatars/Adults/Female_Adult_16/Export/Female_Adult_16.fbx"]["sha1"],
            "99e60c6d5e1f01edca7e48927f0c6baef7aa2489")
        self.assertEqual(sum(f["kind"]=="fbx" for f in m["files"]),2)
        self.assertEqual(sum(f["kind"]=="tga" for f in m["files"]),9)
        for f in m["files"]:
            self.assertEqual(len(f["sha1"]),40)
            self.assertTrue(f["output_path"].startswith(
                "Assets/MIR/Art/Rocketbox/Cast/"))

    def test_verified_source_not_unlicensed_random_character(self):
        script=FETCH.read_text(encoding="utf8")
        for token in (
            "git_blob_hash(source) != item",
            "resize_tga_box(binary,m",
            "normal_map=item",
            "cache_valid(m)",
            "source_sha",
            "original_license_blob",
            "parse_tga_header(binary)",
            "TemporaryDirectory(",
            "--verify-only",
        ):
            self.assertIn(token,script)
        self.assertNotIn("extractall(",script)
        self.assertNotIn("eval(",script)
        self.assertNotIn("SkipLicenseCheck",script)
        self.assertIn("GitBlobNormalizedMIT",PILOT.read_text(encoding="utf8"))

    def test_unity_creates_real_humanoid_skin_and_retargeted_animation(self):
        c=CAST.read_text(encoding="utf8")
        for token in (
            "ModelImporterAnimationType.Human",
            "ModelImporterAvatarSetup.CreateFromThisModel",
            "SkinnedMeshRenderer",
            "skin.sharedMesh.bindposes.Length==boneCount",
            "avatar.isValid&&avatar.isHuman",
            "animator.avatar.isValid&&animator.avatar.isHuman",
            "controller.animationClips",
            "locomotion=CC0-IdleWalkRun",
            'new[]{"Female16","Male16"}',
            "PrefabUtility.SaveAsPrefabAsset",
            "NONATION_ROCKETBOX_CAST_READY models=2 realHumanoid=2",
            "NONATION_REQUIRE_REAL_CAST",
            "NONATION_ROCKETBOX_CAST_BLOCKED",
        ):
            self.assertIn(token,c)
        self.assertNotIn("GameObject.CreatePrimitive",c)
        self.assertIn('part=="single"?"":part+"_"',c)
        self.assertIn('Material(model,"single","f204_")',c)
        self.assertIn('Material(model,"body","m019_")',c)

    def test_npcs_use_two_different_source_rigs_not_frozen_bystanders(self):
        s=FACTORY.read_text(encoding="utf8")
        for token in (
            'candidates.Add("MIR/RocketboxPilot01")',
            'MIR/RocketboxCast/Female16',
            'MIR/RocketboxCast/Male16',
            'NONATION_REAL_HUMAN_CAST_OK asset=',
            "HasPlayableRig(instance,animator,out reason)",
            "skinnedVertices=",
            "if(person.player)",
        ):
            self.assertIn(token,s)
        self.assertIn("MirMotionDriver",s)

    def test_ci_and_real_browser_check_three_different_people(self):
        for file in (CI,PREBUILD):
            s=file.read_text(encoding="utf8")
            self.assertIn("python3 Tools/fetch_rocketbox_cast.py",s)
            self.assertIn("python3 Tools/fetch_rocketbox_cast.py --verify-only",s)
        browser=BROWSER.read_text(encoding="utf8")
        self.assertIn("NONATION_PREMIUM_HUMANOIDS_VERIFIED uniqueAdults=3",browser)
        self.assertIn("VISUAL_GATE_BLOCKED: three distinct real skinned",browser)
        self.assertIn("meta.realHumanPilotReloaded",browser)
        self.assertIn("meta.realCastReloaded = verifiedRealCast()",browser)
        self.assertIn("nonation-browser-showcase.png",browser)


if __name__=="__main__":
    unittest.main()
