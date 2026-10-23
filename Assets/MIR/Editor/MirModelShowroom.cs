#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MIR.Editor
{
    // Opens a studio for all imported sources. Static GLBs are clearly labelled STATIC, NOT an animated NPC.
    public static class MirModelShowroom
    {
        [MenuItem("MIR/04 — Create Art Review Showroom")]
        public static void Create()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Art review floor";
            ground.transform.localScale=new Vector3(35,.2f,10);ground.transform.position=new Vector3(0,-.1f,0);
            var material=MirRenderMaterials.Create(new Color(.66f,.69f,.69f));
            ground.GetComponent<Renderer>().sharedMaterial=material;
            var candidates=new (string path,string label)[]{
                ("Assets/MIR/Art/Quaternius/Mannequin_F.fbx","QUATERNIUS · HUMANOID"),
                ("Assets/MIR/Art/Characters/Maid/maid_skinned_lod1.glb","MAID · SKINNED 64 BONES"),
                ("Assets/MIR/Art/Characters/Floral/Floral_Look.glb","FLORAL · STATIC (needs rig)"),
                ("Assets/MIR/Art/Characters/Casual/Casual_Confidence.glb","CASUAL · STATIC (needs rig)"),
                ("Assets/MIR/Art/Characters/Girl/Girl_HighPoly.glb","GIRL · STATIC (needs rig, HIGH POLY)"),
                ("Assets/MIR/Art/Characters/GIGN/gign_paranoia.fbx","GIGN · FBX"),
                ("Assets/MIR/Art/Characters/Pinkdress/dressfor.fbx","PINKDRESS · FBX")
            };
            for(int i=0;i<candidates.Length;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(candidates[i].path);
                if(source==null){Debug.LogWarning("MIR showroom: unable to instantiate "+candidates[i].path);continue;}
                var instance=UnityEngine.Object.Instantiate(source);
                instance.name=candidates[i].label;
                instance.transform.position=new Vector3((i-3)*4.2f,0,0);
                Renderer[] renderers=instance.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;
                    foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float factor=Mathf.Clamp(1.8f/Mathf.Max(.001f,bounds.size.y),.006f,7f);
                    instance.transform.localScale*=factor;
                    // Calculate post-scale bounds; anchor feet at y=0.
                    bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    instance.transform.position+=Vector3.up*(-bounds.min.y);
                }
                var text=new GameObject(candidates[i].label+" Label").AddComponent<TextMesh>();
                text.text=candidates[i].label;text.fontSize=36;text.characterSize=.038f;
                text.anchor=TextAnchor.MiddleCenter;
                text.transform.position=new Vector3((i-3)*4.2f,2.6f,-.2f);
            }
            var lamp=new GameObject("Studio light").AddComponent<Light>();lamp.type=LightType.Directional;
            lamp.intensity=1.4f;lamp.transform.rotation=Quaternion.Euler(45,-30,0);
            var camera=new GameObject("Review Camera").AddComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,6,-28);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=56;
            string file="Assets/MIR/Scenes/MIR_AssetShowroom.unity";
            EditorSceneManager.SaveScene(scene,file);
            Debug.Log("MIR showroom opened: "+file+". Static assets MUST be rigged before NPC use.");
        }
    }
}
#endif
