#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MIR.Editor
{
    public static class MirHondaImporter
    {
        // This asset is CC-BY-NC-SA: optional, strictly non-commercial test inclusion.
        [MenuItem("MIR/05 — Import Honda Test Asset (NON-COMMERCIAL)")]
        public static void Run()
        {
            if(!EditorUtility.DisplayDialog("Honda: non-commercial licence",
                "Honda Civic BTCC 2023 / vecarz is CC BY-NC-SA 4.0. Do not use this model commercially. Import for local prototype only?",
                "Import for non-commercial study","Cancel"))return;
            string dir=EditorUtility.OpenFolderPanel("Choose unzipped Honda_Civic_BTCC_2023_NC folder","","");
            if(String.IsNullOrEmpty(dir) || !File.Exists(Path.Combine(dir,"scene.gltf")))
            {Debug.LogWarning("MIR Honda: choose folder that contains scene.gltf + scene.bin + textures");return;}
            string dest="Assets/MIR/Art/Vehicles/Honda_NC";
            Directory.CreateDirectory(dest);
            foreach(var source in Directory.GetFiles(dir,"*",SearchOption.AllDirectories))
            {
                string path=source.Substring(dir.Length).TrimStart(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                string to=Path.Combine(dest,path);Directory.CreateDirectory(Path.GetDirectoryName(to));File.Copy(source,to,true);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>(dest+"/scene.gltf");
            if(imported==null)
            {Debug.LogWarning("MIR Honda: glTFast could not import scene.gltf; check Package Manager and Unity Console.");return;}
            var clone=UnityEngine.Object.Instantiate(imported);clone.name="HondaCivic_NC";
            // Keep NC art outside Resources; all Resources assets are included in builds.
            // The prefab is only for local Editor study, not public WebGL.
            PrefabUtility.SaveAsPrefabAsset(clone,dest+"/HondaCivic_NC.prefab");
            UnityEngine.Object.DestroyImmediate(clone);
            AssetDatabase.SaveAssets();
            Debug.Log("MIR Honda NC imported for local Editor study ONLY. Not included in public WebGL without redistribution rights.");
        }
    }
}
#endif
