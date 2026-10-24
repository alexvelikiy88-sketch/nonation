"""Offline regression tests for AGENT03's real Rocketbox Humanoid Unity pipeline.

These tests inspect source data and C# editor/runtime contract, NOT Unity import.
The real Avatar/Animator/skin/render result still requires Unity WebGL proof.
"""
import pathlib
import re
import unittest

BASE = pathlib.Path(__file__).resolve().parent
PROJECT = BASE.parents[3]
EDITOR = BASE / "Editor" / "MirRocketboxPilotBuild.cs"
FACTORY = PROJECT / "Assets" / "MIR" / "Scripts" / "MirHumanFactory.cs"
MOTION = PROJECT / "Assets" / "MIR" / "Scripts" / "MirMotionDriver.cs"

class RealHumanoidPipelineTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.editor = EDITOR.read_text(encoding="utf-8")
        cls.factory = FACTORY.read_text(encoding="utf-8")
        cls.motion = MOTION.read_text(encoding="utf-8")

    def test_real_source_model_is_not_procedural_geometry(self):
        self.assertTrue((BASE / "Male_Adult_12.fbx").is_file())
        self.assertIn('const string SourceModelGitBlob="15d4f8501d5703600cb3d8a94765a3de1029aaf6"',self.editor)
        self.assertIn('const string SourceLicenseGitBlob="9bcfb3ece5301a55d3a41bbd00a029ab27d61d13"',self.editor)
        self.assertIn('GitBlob(Model)==SourceModelGitBlob',self.editor)
        self.assertIn('GitBlob(License)==SourceLicenseGitBlob',self.editor)

    def test_six_real_tga_hashes_checked_before_prefab_output(self):
        self.assertEqual(self.editor.count(".tga\",\""),6)
        self.assertIn('Sha256(path)==entry.Value',self.editor)
        self.assertIn('new FileInfo(path).Length==3145746',self.editor)
        self.assertIn('CreateMaterial("body")',self.editor)
        self.assertIn('CreateMaterial("head")',self.editor)

    def test_humanoid_model_import_and_real_skinned_bones(self):
        self.assertIn('ModelImporterAnimationType.Human',self.editor)
        self.assertIn('ModelImporterAvatarSetup.CreateFromThisModel',self.editor)
        self.assertIn('ModelImporterMaterialImportMode.ImportStandard',self.editor)
        self.assertIn('skin.bones.Length>=40',self.editor)
        self.assertIn('skin.sharedMesh.bindposes.Length==skin.bones.Length',self.editor)
        self.assertIn('skins.Length==1',self.editor)
        self.assertIn('animator.avatar.isHuman',self.editor)

    def test_real_animation_clip_requires_idle_walk_run_not_tpose(self):
        self.assertIn('"Idle_Loop"',self.editor)
        self.assertIn('"Walk_Loop"',self.editor)
        self.assertIn('"Jog_Fwd_Loop"',self.editor)
        self.assertIn('three distinct locomotion clips required',self.editor)
        self.assertIn('IndexOf("TPose"',self.editor)
        self.assertIn('BlendTreeType.Simple1D',self.editor)
        self.assertIn('tree.AddChild(idle,0f)',self.editor)
        self.assertIn('tree.AddChild(walk,.42f)',self.editor)
        self.assertIn('tree.AddChild(run,1f)',self.editor)
        self.assertIn('animator.applyRootMotion=false',self.editor)

    def test_resources_prefab_built_automatically_and_playable_in_webgl(self):
        self.assertIn('Resources/MIR/RocketboxPilot01.prefab',self.editor)
        self.assertIn('IPreprocessBuildWithReport',self.editor)
        self.assertIn('report.summary.platform!=BuildTarget.WebGL',self.editor)
        self.assertIn('EditorApplication.delayCall',self.editor)
        self.assertIn('Prepare(false);',self.editor)
        self.assertIn('PrefabUtility.SaveAsPrefabAsset(instance,Output)',self.editor)
        self.assertIn('"MIR/RocketboxPilot01"',self.factory)
        self.assertIn('Resources.Load<GameObject>(candidate)',self.factory)
        self.assertLess(self.factory.index('"MIR/RocketboxPilot01"'),
                        self.factory.index('"MIR/PlayableHuman"',self.factory.index('string[] candidates=')))

    def test_failed_avatar_or_controller_not_accepted_as_real_person(self):
        self.assertIn('AssetDatabase.DeleteAsset(Output)',self.editor)
        self.assertIn('NONATION_ROCKETBOX_BLOCKED:',self.editor)
        self.assertIn('HasPlayableRig(instance,animator,out reason)',self.factory)
        self.assertIn('MIR · Articulated Humanoid Fallback',self.factory)
        self.assertIn('diagnostic',self.factory.lower())
        self.assertIn('AnimatorCullingMode.CullUpdateTransforms',self.motion)

    def test_scope_is_graphics_only_and_source_guard_first(self):
        self.assertIn('#if UNITY_EDITOR',self.editor)
        self.assertIn('namespace MIR.Rocketbox.Editor',self.editor)
        self.assertIn('Assets/MIR/Art/Rocketbox',self.editor)
        self.assertNotIn('Assets/MIR/Editor/',self.editor)
        self.assertNotIn('AssetDatabase.CreateAsset(',self.factory)
        self.assertLess(self.editor.index('VerifySources();'),
                        self.editor.index('CreateLocomotion();'))
        self.assertLess(self.editor.index('var controller=CreateLocomotion();'),
                        self.editor.index('PrefabUtility.SaveAsPrefabAsset(instance,Output)'))

if __name__=="__main__":
    unittest.main()
