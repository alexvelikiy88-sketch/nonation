#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MIR.Rocketbox.Editor
{
    internal sealed class MirRocketboxCastImport : AssetPostprocessor
    {
        const string Prefix = "Assets/MIR/Art/Rocketbox/Cast/";
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Prefix,StringComparison.OrdinalIgnoreCase) ||
                !assetPath.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase))
                return;
            var importer=assetImporter as ModelImporter;
            if(importer==null)return;
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=false;
            importer.optimizeGameObjects=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        }
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Prefix,StringComparison.OrdinalIgnoreCase) ||
                !assetPath.EndsWith(".tga",StringComparison.OrdinalIgnoreCase))
                return;
            var importer=assetImporter as TextureImporter;
            if(importer==null)return;
            bool normal=assetPath.EndsWith("_normal.tga",StringComparison.OrdinalIgnoreCase);
            importer.textureType=normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture=assetPath.EndsWith("_color.tga",StringComparison.OrdinalIgnoreCase);
            importer.mipmapEnabled=true;
            importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Compressed;
            if(normal)importer.convertToNormalmap=false;
        }
    }

    public sealed class MirRocketboxCastPrebuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 9600; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.WebGL)return;
            if(MirRocketboxCastBuild.Prepare())return;
            Debug.LogWarning("NONATION_REAL_CAST_UNAVAILABLE: " +
                "3 different skinned adults NOT proven; visual acceptance OPEN");
            if(Environment.GetEnvironmentVariable("NONATION_REQUIRE_REAL_CAST")=="1")
                throw new BuildFailedException(
                    "NONATION_VISUAL_RELEASE_BLOCKED: real two-person " +
                    "Microsoft MIT skinned NPC cast and locomotion are required");
        }
    }

    /// <summary>
    /// First real skinned player is prepared by MirRocketboxPilotBuild.
    /// Adds two different Microsoft MIT real adults for the living NPC cast,
    /// each with the same REAL CC0 Idle/Walk/Run BlendTree. Reject empty
    /// avatars/material guesses, preserve all original source Git blob checks.
    /// </summary>
    public static class MirRocketboxCastBuild
    {
        const string CastRoot="Assets/MIR/Art/Rocketbox/Cast/";
        const string Root="Assets/MIR/Art/Rocketbox/";
        const string Resource="Assets/MIR/Art/Rocketbox/Resources/MIR/RocketboxCast/";
        const string Controller=Root+"Generated/RocketboxLocomotion.controller";

        static void Require(bool valid,string reason)
        {
            if(!valid)throw new InvalidOperationException("NONATION_ROCKETBOX_CAST_BLOCKED: "+reason);
        }
        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');
            if(!AssetDatabase.IsValidFolder(parent))Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static Texture2D Texture(string model,string file)
        {
            var path=CastRoot+model+"/"+file;
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(t==null)
            {
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            Require(t!=null,"real Microsoft MIT texture missing "+path);
            return t;
        }
        static Material Material(string model,string part,string prefix)
        {
            Shader shader=Shader.Find("Standard");
            if(shader==null || !shader.isSupported)
                shader=Resources.Load<Shader>("MIR/MirRuntimeFallback");
            Require(shader!=null&&shader.isSupported,"no supported built-in source texture shader");
            // Female_Adult_16 has one original f204_color/normal/specular
            // slot; m019 has separate original body/head material slots.
            string stem=prefix+(part=="single"?"":part+"_");
            var color=Texture(model,stem+"color.tga");
            var normal=Texture(model,stem+"normal.tga");
            var spec=Texture(model,stem+"specular.tga");
            var material=new Material(shader);
            material.name="Microsoft MIT / "+model+" / "+part;
            material.SetColor("_Color",Color.white);
            if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",color);
            if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",color);
            if(material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap",normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if(material.HasProperty("_SpecGlossMap"))
            {
                material.SetTexture("_SpecGlossMap",spec);
                material.EnableKeyword("_SPECGLOSSMAP");
                material.EnableKeyword("_SPECULAR_SETUP");
            }
            if(material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness",.36f);
            material.enableInstancing=true;
            string path=Resource+model+"_"+part+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing==null){AssetDatabase.CreateAsset(material,path);return material;}
            EditorUtility.CopySerialized(material,existing);
            UnityEngine.Object.DestroyImmediate(material);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static Avatar AvatarAt(string model)
        {
            string fbx=CastRoot+model+"/"+model+".fbx";
            var importer=AssetImporter.GetAtPath(fbx) as ModelImporter;
            Require(importer!=null,"no real ModelImporter "+fbx);
            if(importer.animationType!=ModelImporterAnimationType.Human ||
               importer.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.animationType=ModelImporterAnimationType.Human;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation=false;
                importer.optimizeGameObjects=false;
                importer.SaveAndReimport();
            }
            Avatar avatar=AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>()
                .FirstOrDefault(a=>a!=null&&a.isValid&&a.isHuman);
            if(avatar==null)
            {
                AssetDatabase.ImportAsset(fbx,ImportAssetOptions.ForceSynchronousImport|
                    ImportAssetOptions.ForceUpdate);
                avatar=AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>()
                    .FirstOrDefault(a=>a!=null&&a.isValid&&a.isHuman);
            }
            Require(avatar!=null,"Microsoft Humanoid Avatar mapping invalid: "+model);
            return avatar;
        }

        static int Build(string model,RuntimeAnimatorController controller)
        {
            string modelPath=CastRoot+model+"/"+model+".fbx";
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Require(original!=null,"real source FBX GameObject import missing "+model);
            Avatar avatar=AvatarAt(model);
            GameObject instance=UnityEngine.Object.Instantiate(original);
            try
            {
                instance.name="Microsoft MIT rigged citizen / "+model;
                var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(s=>s!=null && s.sharedMesh!=null && s.sharedMesh.vertexCount>1000)
                    .ToArray();
                Require(skins.Length>=1,"no actual weighted SkinnedMeshRenderer "+model);
                int vertices=0,bones=0;
                foreach(var skin in skins)
                {
                    vertices+=skin.sharedMesh.vertexCount;
                    int boneCount=skin.bones==null?0:skin.bones.Length;
                    Require(boneCount>=40 &&
                        skin.sharedMesh.bindposes.Length==boneCount,
                        "actual bone/bindpose mismatch: "+model);
                    bones+=boneCount;

                    var old=skin.sharedMaterials;
                    Material[] assigned=new Material[old.Length];
                    Require(old.Length>=1 && old.Length<=3,
                        "unrecognized original body material slots "+model);
                    if(model=="Female_Adult_16")
                    {
                        var m=Material(model,"single","f204_");
                        for(int i=0;i<assigned.Length;i++)assigned[i]=m;
                    }
                    else
                    {
                        var body=Material(model,"body","m019_");
                        var head=Material(model,"head","m019_");
                        Require(old.Length==2,"Microsoft m019 body/head slots required");
                        for(int i=0;i<old.Length;i++)
                        {
                            string oldName=old[i]==null?"":old[i].name.ToLowerInvariant();
                            if(oldName.Contains("body"))assigned[i]=body;
                            else if(oldName.Contains("head"))assigned[i]=head;
                            else throw new InvalidOperationException(
                                "NONATION_ROCKETBOX_CAST_BLOCKED: original material UV slot " +
                                i+" has no verifiable body/head mapping: "+oldName);
                        }
                        Require(assigned[0]!=assigned[1],"duplicate original body/head slots");
                    }
                    skin.sharedMaterials=assigned;
                    skin.enabled=true;
                }

                var animator=instance.GetComponentInChildren<Animator>(true);
                if(animator==null)animator=instance.AddComponent<Animator>();
                animator.avatar=avatar;
                animator.runtimeAnimatorController=controller;
                animator.applyRootMotion=false;
                animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                Require(animator.avatar!=null&&animator.avatar.isValid&&animator.avatar.isHuman,
                    "Animator Avatar not actually humanoid: "+model);
                Require(animator.runtimeAnimatorController!=null,
                    "no verified real Idle/Walk/Run BlendTree for "+model);
                var clipNames=controller.animationClips.Select(c=>c.name.ToLowerInvariant()).ToList();
                Require(clipNames.Any(n=>n.Contains("idle")) &&
                        clipNames.Any(n=>n.Contains("walk")) &&
                        clipNames.Any(n=>n.Contains("jog")||n.Contains("run")||n.Contains("sprint")),
                        "real Idle/Walk/Run clips missing for "+model);
                string path=Resource+(model=="Female_Adult_16"?"Female16":"Male16")+".prefab";
                var prefab=PrefabUtility.SaveAsPrefabAsset(instance,path);
                Require(prefab!=null,"Unity refused weighted Humanoid prefab "+model);
                Debug.Log("NONATION_ROCKETBOX_CAST_PREFAB_READY asset="+model+
                    " humanAvatar=true skinnedVertices="+vertices+
                    " bones="+bones+" locomotion=CC0-IdleWalkRun");
                return vertices;
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        public static bool Prepare()
        {
            try
            {
                var notice=AssetDatabase.LoadAssetAtPath<TextAsset>(
                    Root+"Resources/MIR/RocketboxMITNotice.txt");
                Require(notice!=null &&
                        notice.text.Contains("Copyright (c) 2020 Microsoft"),
                        "original Microsoft MIT copyright notice not bundled");
                var ctrl=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
                Require(ctrl!=null,"Rocketbox pilot locomotion controller not generated");
                Folder(Resource.TrimEnd('/'));
                int vertices=0;
                vertices+=Build("Male_Adult_16",ctrl);
                vertices+=Build("Female_Adult_16",ctrl);
                Require(vertices>=2000,"Skinned cast geometry invalid");
                AssetDatabase.SaveAssets();
                Debug.Log("NONATION_ROCKETBOX_CAST_READY models=2 realHumanoid=2 " +
                    "skinnedVertices="+vertices+" source=Microsoft-MIT");
                return true;
            }
            catch(Exception error)
            {
                // Never leave a stale partly generated citizen prefab after
                // a failed source/humanoid/material proof.
                foreach(var name in new[]{"Female16","Male16"})
                {
                    string path=Resource+name+".prefab";
                    if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)
                        AssetDatabase.DeleteAsset(path);
                }
                Debug.LogError(error.ToString());
                return false;
            }
        }
    }
}
#endif
