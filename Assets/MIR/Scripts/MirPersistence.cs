using System;
using System.IO;
using UnityEngine;

namespace MIR
{
    public static class MirPersistence
    {
        const string Key="MIR_UNITY_6_WORLD_V1";
        static string NativeFile {get{return Path.Combine(Application.persistentDataPath,"mir_world_v1.json");}}
        public static MirWorldState Load()
        {
            string data="";
            try
            {
                // WebGL PlayerPrefs persists through the browser's IndexedDB-backed Unity storage.
                if(PlayerPrefs.HasKey(Key))data=PlayerPrefs.GetString(Key);
                else if(Application.platform!=RuntimePlatform.WebGLPlayer && File.Exists(NativeFile))data=File.ReadAllText(NativeFile);
                if(!String.IsNullOrEmpty(data))
                {
                    var world=JsonUtility.FromJson<MirWorldState>(data);
                    if(world!=null && world.version==1 && world.citizens!=null && world.citizens.Count>0
                       && world.businesses!=null && world.businesses.Count>0)
                    {
                        Debug.Log("NONATION_SAVE_RESTORED citizens=" + world.citizens.Count);
                        return world;
                    }
                }
            }
            catch(Exception ex){Debug.LogWarning("MIR: existing save could not be loaded; backup remains untouched. "+ex.Message);}
            return MirWorldState.NewWorld();
        }
        public static bool Save(MirWorldState world)
        {
            try
            {
                string data=JsonUtility.ToJson(world);
                PlayerPrefs.SetString(Key,data);PlayerPrefs.Save();
                if(Application.platform!=RuntimePlatform.WebGLPlayer)
                {Directory.CreateDirectory(Application.persistentDataPath);File.WriteAllText(NativeFile,data);}
                Debug.Log("NONATION_SAVE_WRITTEN citizens=" + world.citizens.Count);
                return true;
            }
            catch(Exception ex){Debug.LogError("MIR save failed: "+ex);return false;}
        }
        public static string Export(MirWorldState world){return JsonUtility.ToJson(world,true);}
        public static bool Import(string json,out MirWorldState world)
        {
            world=null;
            try{
                var s=JsonUtility.FromJson<MirWorldState>(json);
                if(s==null||s.version!=1||s.citizens==null||s.citizens.Count<1||s.businesses==null)return false;
                world=s;return true;
            }catch{return false;}
        }
    }
}
