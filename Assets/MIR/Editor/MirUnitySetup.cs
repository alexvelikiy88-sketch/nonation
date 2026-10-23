#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MIR.Editor
{
    public static class MirUnitySetup
    {
        const string Root="Assets/MIR";
        const string CatalogPath=Root+"/Resources/MIR/QuaterniusCatalog.asset";
        const string ControllerPath=Root+"/Resources/MIR/QuaterniusMotion.controller";
        const string ScenePath=Root+"/Scenes/MIR_PlayableDistrict.unity";
        const string SourceA=Root+"/Art/Quaternius/UAL1_Standard_Unity.fbx";
        const string SourceB=Root+"/Art/Quaternius/UAL2_Standard_Unity.fbx";
        const string Mannequin=Root+"/Art/Quaternius/Mannequin_F.fbx";
        [InitializeOnLoadMethod]
        static void InstallFirstRun()
        {
            EditorApplication.delayCall+=()=>
            {
                if(Application.isPlayingOrWillChangePlaymode)return;
                if(!File.Exists(ScenePath))
                {
                    try{Install(false);}catch(Exception e){Debug.LogWarning("MIR auto-setup: "+e.Message+". Run MIR > Rebuild Playable District.");}
                }
            };
        }
        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');
            if(!AssetDatabase.IsValidFolder(parent))Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static AnimationClip[] Clips(string path)
        {
            if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)==null)return new AnimationClip[0];
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c=>!c.name.StartsWith("__") && c.length>.03f).ToArray();
        }
        static AnimationClip Get(IEnumerable<AnimationClip> clips,params string[] keys)
        {
            foreach(string key in keys)
            {var c=clips.FirstOrDefault(x=>x.name.IndexOf(key,StringComparison.OrdinalIgnoreCase)>=0);
                if(c!=null)return c;}
            return clips.FirstOrDefault();
        }
        static void State(AnimatorStateMachine sm,string name,AnimationClip clip)
        {if(clip==null)return;var state=sm.AddState(name);state.motion=clip;state.writeDefaultValues=true;}
        static AnimatorController BuildController(AnimationClip[] all)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var ctrl=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var sm=ctrl.layers[0].stateMachine;
            var idle=Get(all,"Idle_Loop","Idle_FoldArms_Loop","A_TPose");
            var walk=Get(all,"Walk_Loop","Walk_Formal_Loop");
            var formal=Get(all,"Walk_Formal_Loop","Walk_Loop");
            var run=Get(all,"Jog_Fwd_Loop","Sprint_Loop");
            var carry=Get(all,"Walk_Carry_Loop","Walk_Loop");
            BlendTree Tree(string n,AnimationClip walking)
            {
                var t=new BlendTree{name=n,blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
                AssetDatabase.AddObjectToAsset(t,ctrl);
                if(idle!=null)t.AddChild(idle,0f);
                if(walking!=null)t.AddChild(walking,.42f);
                if(run!=null)t.AddChild(run,1f);
                return t;
            }
            var locomotion=sm.AddState("Locomotion");locomotion.motion=Tree("Locomotion Blend",walk);
            sm.defaultState=locomotion;
            var locFormal=sm.AddState("Locomotion_Formal");locFormal.motion=Tree("Formal Blend",formal);
            var locCarry=sm.AddState("Locomotion_Carry");locCarry.motion=Tree("Carry Blend",carry);
            State(sm,"MIR_Talk",Get(all,"Idle_Talking_Loop","Idle_TalkingPhone_Loop"));
            State(sm,"MIR_Phone",Get(all,"Idle_TalkingPhone_Loop","Idle_Rail_Call"));
            State(sm,"MIR_Sit",Get(all,"Sitting_Idle_Loop","Sitting_Talking_Loop"));
            State(sm,"MIR_Drive",Get(all,"Driving_Loop"));
            State(sm,"MIR_Work",Get(all,"Fixing_Kneeling","Interact"));
            State(sm,"MIR_Harvest",Get(all,"Farm_Harvest"));
            State(sm,"MIR_Eat",Get(all,"Consume"));
            var catalog=AssetDatabase.LoadAssetAtPath<MirAnimationCatalog>(CatalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<MirAnimationCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            catalog.entries.Clear();var unique=new HashSet<string>();
            int index=0;
            foreach(var clip in all)
            {
                if(clip.name.IndexOf("A_TPose",StringComparison.OrdinalIgnoreCase)>=0)continue;
                string stateName="Q_"+(index++).ToString("000");
                if(unique.Contains(clip.name))stateName+="_"+index;
                unique.Add(clip.name);
                State(sm,stateName,clip);
                catalog.entries.Add(new MirClipEntry{name=clip.name,stateName=stateName,clip=clip});
            }
            EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(ctrl);
            Debug.Log("MIR: imported "+catalog.entries.Count+" Quaternius clips into Unity Animator.");
            return ctrl;
        }
        static GameObject Prefab(string sourcePath,string saveName,RuntimeAnimatorController controller)
        {
            GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if(source==null){Debug.LogWarning("MIR art not imported yet: "+sourcePath);return null;}
            var instance=UnityEngine.Object.Instantiate(source);
            instance.name=saveName;
            var animator=instance.GetComponentInChildren<Animator>();
            if(animator==null)animator=instance.AddComponent<Animator>();
            if(animator.avatar==null || !animator.avatar.isValid || !animator.avatar.isHuman)
            {
                Debug.LogWarning("MIR: avatar NOT HUMANOID for "+sourcePath+". Skipping NPC prefab to prevent T-pose.");
                UnityEngine.Object.DestroyImmediate(instance);return null;
            }
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            string path=Root+"/Resources/MIR/"+saveName+".prefab";
            var prefab=PrefabUtility.SaveAsPrefabAsset(instance,path);
            UnityEngine.Object.DestroyImmediate(instance);return prefab;
        }
        // Cloud and CI entry point: create scene and controller without opening UI windows.
        public static void PrepareForCloud(){Install(false);}
        [MenuItem("MIR/01 — Rebuild Playable District")]
        public static void Run(){Install(true);}
        static void Install(bool open)
        {
            Folder(Root+"/Resources/MIR");Folder(Root+"/Scenes");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var clips=Clips(SourceA).Concat(Clips(SourceB)).ToArray();
            if(clips.Length==0)Debug.LogWarning("MIR: animation FBXs missing. Character falls back to articulated proxy.");
            AnimatorController ctrl=BuildController(clips);
            try
            {
                if(Prefab(Mannequin,"PlayableHuman",ctrl)==null)
                    Debug.LogWarning("MIR: mannequin Avatar invalid; use Rig > Humanoid Configure on Mannequin_F.fbx.");
            }
            catch(Exception ex){Debug.LogWarning("MIR mannequin: "+ex.Message+"; procedural fallback is available.");}
            try{Prefab(Root+"/Art/Characters/GIGN/gign_paranoia.fbx","GIGN",ctrl);}
            catch(Exception ex){Debug.LogWarning("MIR GIGN: "+ex.Message);}
            try{Prefab(Root+"/Art/Characters/Pinkdress/dressfor.fbx","Pinkdress",ctrl);}
            catch(Exception ex){Debug.LogWarning("MIR Pinkdress: "+ex.Message);}
            try{MirHumanoidFromGltf.TryCreate(Root+"/Art/Characters/Maid/maid_skinned_lod1.glb","Maid",ctrl);}
            catch(Exception ex){Debug.LogWarning("MIR Maid glTF: "+ex.Message);}
            // Not every user model is suitable for Humanoid retargeting; no invalid models enter the crowd.
            // Static glTF/GLB files are intentionally NOT assigned Animator controllers; rig them first.
            if(!File.Exists(ScenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var go=new GameObject("MIR Runtime Bootstrap (scene placeholder)");
                go.AddComponent<MirGame>();
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            if(open)EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("MIR Unity ready. Open Assets/MIR/Scenes/MIR_PlayableDistrict, press Play.");
        }
        [MenuItem("MIR/02 — Build WebGL (requires Unity Web Build Support)")]
        static void BuildWeb()
        {
            if(!File.Exists(ScenePath))Install(false);
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))
            {EditorUtility.DisplayDialog("WebGL module missing","Install Web Build Support from Unity Hub for this editor version.","OK");return;}
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL,BuildTarget.WebGL);
            string dest="Build/WebGL";Directory.CreateDirectory(dest);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=dest,target=BuildTarget.WebGL,options=BuildOptions.None});
            Debug.Log("MIR Web build: "+report.summary.result+" at "+Path.GetFullPath(dest));
        }
    }
}
#endif
