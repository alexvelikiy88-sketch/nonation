using System;
using System.Collections.Generic;
using UnityEngine;
namespace MIR
{
    [Serializable]public class MirClipEntry
    {
        public string name,stateName;
        public AnimationClip clip;
    }
    public sealed class MirAnimationCatalog : ScriptableObject
    {
        public List<MirClipEntry> entries=new List<MirClipEntry>();
        public bool TryFind(string token,out MirClipEntry match)
        {
            match=entries.Find(x=>x.name!=null&&x.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0);
            return match!=null;
        }
    }
}
