#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MIR.Rocketbox.Editor
{
    // All outputs are confined to the AGENT 03 owned Assets/MIR/Art/Rocketbox folder.
    // Does not modify AGENT 01's UnitySetup / Scenes / Packages / WebGL build tools.
    internal sealed class MirRocketboxSourcePostprocessor : AssetPostprocessor
    {
        const string ModelPath="Assets/MIR/Art/Rocketbox/Male_Adult_12.fbx";
        const string TexturesPath="Assets/MIR/Art/Rocketbox/Textures/";
        void OnPreprocessModel()
        {
            if(!string.Equals(assetPath,ModelPath,StringComparison.OrdinalIgnoreCase))return;
            var importer=assetImporter as ModelImporter;
            if(importer==null)return;
            // The Microsoft rig is a true 80-cluster skinned Biped. It must be
            // retargeted as a Humanoid, not the default Generic/T-pose.
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=false;
            importer.optimizeGameObjects=false; // Preserve skinned bones for validation.
            // Preserve source m007_body/m007_head material slot names. Our
            // verified MIT materials replace these imported placeholders.
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(TexturesPath,StringComparison.OrdinalIgnoreCase))return;
            var importer=assetImporter as TextureImporter;
            if(importer==null)return;
            bool normal=assetPath.EndsWith("_normal.tga",StringComparison.OrdinalIgnoreCase);
            importer.textureType=normal?TextureImporterType.NormalMap:
                TextureImporterType.Default;
            importer.sRGBTexture=!normal && assetPath.EndsWith("_color.tga",
                StringComparison.OrdinalIgnoreCase);
            importer.mipmapEnabled=true;
            importer.maxTextureSize=1024;
            importer.wrapMode=TextureWrapMode.Repeat;
            if(normal)importer.convertToNormalmap=false;
        }
    }

    public sealed class MirRocketboxPilotPrebuild : IPreprocessBuildWithReport
    {
        // Runs after normal scene preflight. Animator source clips are provided
        // by Tools/fetch_quaternius.py and imported before the player build.
        public int callbackOrder { get { return 9500; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.WebGL)return;
            MirRocketboxPilotBuild.Prepare(false);
        }
    }

    // Explicit Editor menu command and automatic WebGL preparation. A prefab is
    // written only after source SHA, human Avatar, 2 material slots, skinned
    // bone mapping and real Idle/Walk/Run clips all pass. No invented geometry.
    public static class MirRocketboxPilotBuild
    {
        const string Base="Assets/MIR/Art/Rocketbox";
        const string Model=Base+"/Male_Adult_12.fbx";
        const string License=Base+"/LICENSE.md";
        const string Textures=Base+"/Textures/";
        const string Output=Base+"/Resources/MIR/RocketboxPilot01.prefab";
        const string Materials=Base+"/Generated";
        const string ControllerPath=Materials+"/RocketboxLocomotion.controller";
        const string SourceA="Assets/MIR/Art/Quaternius/UAL1_Standard_Unity.fbx";
        const string SourceB="Assets/MIR/Art/Quaternius/UAL2_Standard_Unity.fbx";
        const string SourceModelGitBlob="15d4f8501d5703600cb3d8a94765a3de1029aaf6";
        const string SourceLicenseGitBlob="9bcfb3ece5301a55d3a41bbd00a029ab27d61d13";

        static bool editorAttempted;
        [InitializeOnLoadMethod]
        static void ScheduleEditorPilot()
        {
            // Play Mode should use the real person after assets load, not wait
            // until an explicit WebGL export. Missing CC0 clips are a normal
            // non-fatal state on a checkout before the fetch script runs.
            EditorApplication.delayCall+=()=>
            {
                if(editorAttempted || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                editorAttempted=true;
                if(!File.Exists(SourceA)||!File.Exists(SourceB))return;
                Prepare(false);
            };
        }

        static readonly Dictionary<string,string> TextureHashes=
            new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
            {
                {"m007_body_color.tga","e14e258e5b173f8e47f2cf3726191ae388b82daec2e4788c973793fd8f16e5b2"},
                {"m007_body_normal.tga","5bc98dd7d0590dd4b25efee6a62acb22e228c4097b65bf422cb24de31aea8487"},
                {"m007_body_specular.tga","055523b34fd4927d6723bc2d79d7d13ec8ce4908024145cc943cb30af422620c"},
                {"m007_head_color.tga","84bcb874c815833bf0de6bdb85237ca46bd771750ba30ce84b6d526982ee70e0"},
                {"m007_head_normal.tga","b0588b2294a993ea114ac029a55e6cd3c0e7315f803c6894344d732f1fb09810"},
                {"m007_head_specular.tga","a68c568465d06b4cf151d361c3e1c8f4ed9c09eb47640d7350228207b2e445cd"}
            };

        static string Hex(byte[] bytes)
        {
            var sb=new StringBuilder(bytes.Length*2);
            foreach(byte b in bytes)sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
        static string GitBlob(string path)
        {
            byte[] raw=File.ReadAllBytes(path);
            byte[] header=Encoding.ASCII.GetBytes("blob "+raw.Length+"\0");
            var all=new byte[header.Length+raw.Length];
            Buffer.BlockCopy(header,0,all,0,header.Length);
            Buffer.BlockCopy(raw,0,all,header.Length,raw.Length);
            using(var sha=SHA1.Create())return Hex(sha.ComputeHash(all));
        }
        static string Sha256(string path)
        {
            using(var sha=SHA256.Create())
            using(var stream=File.OpenRead(path))
                return Hex(sha.ComputeHash(stream));
        }
        static void Require(bool condition,string reason)
        {
            if(!condition)throw new InvalidOperationException("NONATION_ROCKETBOX_BLOCKED: "+reason);
        }
        static void VerifySources()
        {
            Require(File.Exists(Model),"original MIT FBX missing");
            Require(GitBlob(Model)==SourceModelGitBlob,"FBX differs from pinned Microsoft original");
            Require(File.Exists(License)&&GitBlob(License)==SourceLicenseGitBlob,
                "required Microsoft MIT copyright/license modified or missing");
            foreach(var entry in TextureHashes)
            {
                string path=Textures+entry.Key;
                Require(File.Exists(path),"missing "+entry.Key);
                Require(new FileInfo(path).Length==3145746,
                    "unexpected 1024x1024 TGA bytes: "+entry.Key);
                Require(Sha256(path)==entry.Value,
                    "TGA SHA-256 differs from verified source: "+entry.Key);
            }
        }
        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');
            if(!AssetDatabase.IsValidFolder(parent))Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static Texture2D LoadTexture(string file)
        {
            string path=Textures+file;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null)
            {
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            Require(texture!=null,"Unity TextureImporter rejected "+path);
            return texture;
        }
        static Material CreateMaterial(string section)
        {
            var baseMap=LoadTexture("m007_"+section+"_color.tga");
            var normal=LoadTexture("m007_"+section+"_normal.tga");
            var specular=LoadTexture("m007_"+section+"_specular.tga");
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null||!shader.isSupported)shader=Shader.Find("Standard");
            Require(shader!=null&&shader.isSupported,"no usable Unity lit/PBR shader");

            var mat=new Material(shader);
            mat.name="Microsoft Rocketbox / "+section+" / verified MIT";
            mat.color=Color.white;
            if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",Color.white);
            if(mat.HasProperty("_BaseMap"))mat.SetTexture("_BaseMap",baseMap);
            if(mat.HasProperty("_MainTex"))mat.SetTexture("_MainTex",baseMap);
            if(mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap",normal);
                mat.EnableKeyword("_NORMALMAP");
                if(mat.HasProperty("_BumpScale"))mat.SetFloat("_BumpScale",1f);
            }
            if(mat.HasProperty("_WorkflowMode"))mat.SetFloat("_WorkflowMode",0f);
            if(mat.HasProperty("_SpecGlossMap"))
            {
                mat.SetTexture("_SpecGlossMap",specular);
                mat.EnableKeyword("_SPECGLOSSMAP");
                mat.EnableKeyword("_SPECULAR_SETUP");
            }
            if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",.37f);
            mat.enableInstancing=true;
            string path=Materials+"/Rocketbox_"+section+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing==null)
            {
                AssetDatabase.CreateAsset(mat,path);
                return mat;
            }
            EditorUtility.CopySerialized(mat,existing);
            UnityEngine.Object.DestroyImmediate(mat);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        static AnimationClip FindClip(string[] tokens)
        {
            var clips=new List<AnimationClip>();
            foreach(string source in new[]{SourceA,SourceB})
            {
                if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(source)==null)continue;
                foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>())
                {
                    if(clip==null||clip.length<=.03f||clip.name.StartsWith("__"))continue;
                    if(clip.name.IndexOf("TPose",StringComparison.OrdinalIgnoreCase)>=0||
                       clip.name.IndexOf("T-Pose",StringComparison.OrdinalIgnoreCase)>=0)
                        continue;
                    clips.Add(clip);
                }
            }
            foreach(string token in tokens)
            {
                var found=clips.FirstOrDefault(clip=>
                    clip.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0);
                if(found!=null)return found;
            }
            return null;
        }
        static AnimatorController CreateLocomotion()
        {
            // Do not fall back to a T-pose, unrelated animation or empty clips.
            AnimationClip idle=FindClip(new[]{"Idle_Loop","Idle_FoldArms_Loop","Idle_Breathe"});
            AnimationClip walk=FindClip(new[]{"Walk_Loop","Walk_Formal_Loop"});
            AnimationClip run=FindClip(new[]{"Jog_Fwd_Loop","Sprint_Loop"});
            Require(idle!=null&&walk!=null&&run!=null,
                "verified Quaternius CC0 Idle/Walk/Run clips missing; run Tools/fetch_quaternius.py");
            Require(idle!=walk&&walk!=run&&run!=idle,"three distinct locomotion clips required");
            if(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)!=null)
                AssetDatabase.DeleteAsset(ControllerPath);
            var controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            Require(controller!=null,"cannot create pilot AnimatorController");
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var sm=controller.layers[0].stateMachine;
            var locomotion=sm.AddState("Locomotion");
            var tree=new BlendTree
            {
                name="Rocketbox / Idle Walk Run",
                blendType=BlendTreeType.Simple1D,
                blendParameter="Speed",
                useAutomaticThresholds=false
            };
            AssetDatabase.AddObjectToAsset(tree,controller);
            tree.AddChild(idle,0f);
            tree.AddChild(walk,.42f);
            tree.AddChild(run,1f);
            locomotion.motion=tree;
            sm.defaultState=locomotion;
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static Avatar FindAvatar()
        {
            return AssetDatabase.LoadAllAssetsAtPath(Model)
                .OfType<Avatar>().FirstOrDefault(a=>a!=null&&a.isValid&&a.isHuman);
        }

        static void ReimportHumanoidIfNeeded()
        {
            var importer=AssetImporter.GetAtPath(Model) as ModelImporter;
            Require(importer!=null,"ModelImporter missing for "+Model);
            if(importer.animationType!=ModelImporterAnimationType.Human ||
               importer.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel ||
               importer.optimizeGameObjects)
            {
                importer.animationType=ModelImporterAnimationType.Human;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation=false;
                importer.optimizeGameObjects=false;
                importer.SaveAndReimport();
            }
            if(FindAvatar()==null)
            {
                // The postprocessor may have been installed AFTER the FBX was
                // first cached as Generic; one forced reimport is necessary.
                AssetDatabase.ImportAsset(Model,ImportAssetOptions.ForceSynchronousImport|
                    ImportAssetOptions.ForceUpdate);
            }
            Require(FindAvatar()!=null,"Microsoft Rocketbox humanoid Avatar invalid; " +
                "requires Unity ModelImporter bone mapping and T-pose inspection");
        }
        static SkinnedMeshRenderer GetValidatedSkin(GameObject instance)
        {
            var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(s=>s!=null&&s.sharedMesh!=null&&s.sharedMesh.vertexCount>0)
                .ToArray();
            Require(skins.Length==1,"one real Microsoft skinned mesh required; got "+skins.Length);
            var skin=skins[0];
            Require(skin.bones!=null&&skin.bones.Length>=40,
                "source skin has missing/empty 80-bone hierarchy");
            Require(skin.sharedMesh.bindposes!=null&&
                skin.sharedMesh.bindposes.Length==skin.bones.Length,
                "skinned mesh bones/bindposes mismatch");
            Require(skin.sharedMesh.subMeshCount>=2,
                "two original materials (m007_body and m007_head) required");
            return skin;
        }
        static void ApplySkinMaterials(SkinnedMeshRenderer skin)
        {
            var body=CreateMaterial("body");
            var head=CreateMaterial("head");
            var old=skin.sharedMaterials;
            Require(old.Length==2,"two body/head material slots required, got "+old.Length);
            var assigned=new Material[2];
            for(int i=0;i<old.Length;i++)
            {
                string name=old[i]==null?"":old[i].name.ToLowerInvariant();
                if(name.Contains("body"))assigned[i]=body;
                else if(name.Contains("head"))assigned[i]=head;
                else throw new InvalidOperationException(
                    "NONATION_ROCKETBOX_BLOCKED: unrecognized source material slot "+
                    i+" ('"+name+"'); cannot safely guess head/body UV mapping");
            }
            Require(assigned[0]!=assigned[1],"head/body material slot distinction lost");
            skin.sharedMaterials=assigned;
            skin.enabled=true;
        }
        [MenuItem("NONATION/Graphics/01 — Build real Rocketbox humanoid")]
        public static void BuildFromMenu(){Prepare(true);}

        public static bool Prepare(bool manual)
        {
            GameObject instance=null;
            try
            {
                VerifySources();
                Folder(Materials);
                Folder(Base+"/Resources/MIR");
                ReimportHumanoidIfNeeded();
                var imported=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
                Require(imported!=null,"real Microsoft FBX did not import as a GameObject");
                var controller=CreateLocomotion();
                instance=UnityEngine.Object.Instantiate(imported);
                instance.name="RocketboxPilot01 / MIT skinned humanoid";
                var skin=GetValidatedSkin(instance);
                ApplySkinMaterials(skin);
                Avatar avatar=FindAvatar();
                var animator=instance.GetComponentInChildren<Animator>(true);
                if(animator==null)animator=instance.AddComponent<Animator>();
                animator.avatar=avatar;
                animator.runtimeAnimatorController=controller;
                animator.applyRootMotion=false;
                animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                Require(animator.avatar!=null && animator.avatar.isValid &&
                        animator.avatar.isHuman,"constructed Animator Avatar invalid");
                var output=PrefabUtility.SaveAsPrefabAsset(instance,Output);
                Require(output!=null,"Unity failed to save the validated real humanoid prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("NONATION_ROCKETBOX_READY source="+Model+
                    " prefab="+Output+" vertices="+skin.sharedMesh.vertexCount+
                    " bones="+skin.bones.Length+
                    " avatarHuman="+avatar.isHuman+
                    " controller="+ControllerPath+
                    " textures=6 MIT sha256=verified IdleWalkRun=real-CC0");
                return true;
            }
            catch(Exception ex)
            {
                // Never let stale prefab from a previous attempt masquerade as
                // today's verified rig if source, textures or clips failed.
                if(AssetDatabase.LoadAssetAtPath<GameObject>(Output)!=null)
                    AssetDatabase.DeleteAsset(Output);
                Debug.LogError(ex.ToString());
                if(manual)EditorUtility.DisplayDialog("NONATION Rocketbox Pilot blocked",
                    ex.Message,"OK");
                return false;
            }
            finally
            {
                if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
#endif
