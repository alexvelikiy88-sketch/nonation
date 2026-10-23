#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MIR.Editor
{
    // Converts an already SKINNED glTF character into a Unity Humanoid Avatar.
    // Does NOT magically rig static GLB sources. Invalid avatars are rejected, never sent into crowd animation.
    public static class MirHumanoidFromGltf
    {
        static readonly Dictionary<string,string> BoneNames=new Dictionary<string,string>{
            {"Hips","pelvis"},{"Spine","spine"},{"Chest","chest"},{"Neck","neck"},{"Head","head"},
            {"LeftShoulder","clavicle_L"},{"LeftUpperArm","upperarm_L"},{"LeftLowerArm","forearm_L"},{"LeftHand","hand_L"},
            {"RightShoulder","clavicle_R"},{"RightUpperArm","upperarm_R"},{"RightLowerArm","forearm_R"},{"RightHand","hand_R"},
            {"LeftUpperLeg","thigh_L"},{"LeftLowerLeg","shin_L"},{"LeftFoot","foot_L"},{"LeftToes","toe_L"},
            {"RightUpperLeg","thigh_R"},{"RightLowerLeg","shin_R"},{"RightFoot","foot_R"},{"RightToes","toe_R"}
        };
        public static GameObject TryCreate(string sourcePath,string prefabName,RuntimeAnimatorController motions)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if(source==null){Debug.LogWarning("MIR glTF import not available: "+sourcePath);return null;}
            var clone=UnityEngine.Object.Instantiate(source);
            clone.name=prefabName;
            if(clone.GetComponentInChildren<SkinnedMeshRenderer>()==null)
            {
                UnityEngine.Object.DestroyImmediate(clone);
                Debug.LogWarning("MIR GLB source is STATIC, no skin: "+sourcePath);
                return null;
            }
            var transforms=clone.GetComponentsInChildren<Transform>(true);
            var byName=new Dictionary<string,Transform>();
            foreach(var t in transforms)if(!byName.ContainsKey(t.name))byName.Add(t.name,t);
            var humanBones=new List<HumanBone>();
            foreach(var pair in BoneNames)
            {
                if(!byName.ContainsKey(pair.Value))continue;
                var h=new HumanBone{humanName=pair.Key,boneName=pair.Value,limit=new HumanLimit{useDefaultValues=true}};
                humanBones.Add(h);
            }
            if(humanBones.Count<15)
            {
                UnityEngine.Object.DestroyImmediate(clone);
                Debug.LogWarning("MIR GLB missing humanoid bones: "+sourcePath);
                return null;
            }
            var skeletalBones=new List<SkeletonBone>();
            foreach(var t in transforms)
            {
                skeletalBones.Add(new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale});
            }
            var description=new HumanDescription{
                human=humanBones.ToArray(),skeleton=skeletalBones.ToArray(),
                upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,
                armStretch=.05f,legStretch=.05f,feetSpacing=0f,hasTranslationDoF=false
            };
            var avatar=AvatarBuilder.BuildHumanAvatar(clone,description);
            if(avatar==null||!avatar.isValid||!avatar.isHuman)
            {
                Debug.LogWarning("MIR: Unity rejected automatic Humanoid Avatar for "+sourcePath+". No T-pose assignment.");
                UnityEngine.Object.DestroyImmediate(clone);return null;
            }
            string avatarPath="Assets/MIR/Resources/MIR/"+prefabName+"Avatar.asset";
            AssetDatabase.DeleteAsset(avatarPath);
            AssetDatabase.CreateAsset(avatar,avatarPath);
            var animator=clone.GetComponent<Animator>()??clone.AddComponent<Animator>();
            animator.avatar=avatar;animator.runtimeAnimatorController=motions;animator.applyRootMotion=false;
            string prefabPath="Assets/MIR/Resources/MIR/"+prefabName+".prefab";
            var prefab=PrefabUtility.SaveAsPrefabAsset(clone,prefabPath);
            UnityEngine.Object.DestroyImmediate(clone);
            Debug.Log("MIR: skinned glTF Avatar imported as Humanoid: "+prefabName);
            return prefab;
        }
    }
}
#endif
