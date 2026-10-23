using UnityEngine;

namespace MIR
{
    public static class MirHumanFactory
    {
        static Material Skin(int variant)
        {
            var tones=new[]{new Color(.80f,.59f,.45f),new Color(.56f,.39f,.28f),new Color(.94f,.72f,.58f),new Color(.43f,.30f,.23f)};
            return MirRenderMaterials.Create(tones[Mathf.Abs(variant)%tones.Length]);
        }
        static Material Cloth(int variant)
        {
            var c=new[]{new Color(.27f,.47f,.62f),new Color(.47f,.39f,.31f),new Color(.45f,.51f,.36f),new Color(.66f,.36f,.30f),new Color(.28f,.34f,.39f)};
            return MirRenderMaterials.Create(c[Mathf.Abs(variant)%c.Length]);
        }
        public static MirMotionDriver Attach(Transform holder,MirCitizen person)
        {
            int variant=Mathf.Abs(person.id.GetHashCode());
            var catalog=Resources.Load<MirAnimationCatalog>("MIR/QuaterniusCatalog");
            bool hasWalk=catalog!=null && catalog.entries!=null &&
                catalog.entries.Exists(x=>x!=null&&x.clip!=null&&x.name!=null&&
                    x.name.IndexOf("Walk",System.StringComparison.OrdinalIgnoreCase)>=0);
            GameObject source=hasWalk?Resources.Load<GameObject>("MIR/" +
                (person.look=="default" || string.IsNullOrEmpty(person.look)?"PlayableHuman":person.look)):null;
            if(source==null)source=Resources.Load<GameObject>("MIR/PlayableHuman");
            if(source!=null)
            {
                var instance=Object.Instantiate(source,holder);instance.name="Humanoid · Quaternius";
                instance.transform.localPosition=Vector3.zero;instance.transform.localRotation=Quaternion.identity;
                var a=instance.GetComponentInChildren<Animator>();
                if(a!=null && a.avatar!=null && a.avatar.isValid && a.avatar.isHuman)
                {
                    // Match human height to ~1.72m while preserving authored proportions.
                    var rends=instance.GetComponentsInChildren<Renderer>();
                    bool has=false;Bounds bounds=new Bounds(instance.transform.position,Vector3.zero);
                    foreach(var r in rends){if(!has){bounds=r.bounds;has=true;}else bounds.Encapsulate(r.bounds);}
                    if(has && bounds.size.y>.25f)instance.transform.localScale*=Mathf.Clamp(1.72f/bounds.size.y,.05f,10f);
                    var driver=instance.AddComponent<MirMotionDriver>();driver.Configure(person.gaitSeed);
                    return driver;
                }
                Object.Destroy(instance);
            }
            // Fail safe: no invalid rig, no T-pose. This articulated proxy actually walks.
            var proxy=new GameObject("MIR · Articulated Humanoid Fallback");proxy.transform.SetParent(holder,false);
            var fallback=proxy.AddComponent<MirFallbackWalker>();fallback.Initialize(variant);
            var motion=proxy.AddComponent<MirMotionDriver>();motion.Configure(person.gaitSeed);return motion;
        }
        public static GameObject Capsule(string name,Transform parent,Vector3 pos,Vector3 size,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Capsule);obj.name=name;
            obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;obj.transform.localScale=size;
            obj.GetComponent<MeshRenderer>().sharedMaterial=mat;
            Object.Destroy(obj.GetComponent<Collider>());return obj;
        }
    }
    // Used only when a humanoid Avatar could not be validated; designed to avoid broken T-poses.
    public sealed class MirFallbackWalker : MonoBehaviour
    {
        Transform leftArm,rightArm,leftLeg,rightLeg,body;
        float seed;
        public void Initialize(int variant)
        {
            var skin=GetMaterial(new Color(.75f,.58f,.44f));var clothes=GetMaterial(new[]{
                new Color(.24f,.38f,.49f),new Color(.62f,.43f,.32f),new Color(.37f,.52f,.41f)}[Mathf.Abs(variant)%3]);
            seed=(variant%997)/997f;
            body=new GameObject("Body").transform;body.SetParent(transform,false);
            MirHumanFactory.Capsule("Torso",body,new Vector3(0,1.22f,0),new Vector3(.32f,.29f,.21f),clothes);
            MirHumanFactory.Capsule("Head",body,new Vector3(0,1.70f,0),new Vector3(.14f,.145f,.14f),skin);
            MirHumanFactory.Capsule("Hair",body,new Vector3(0,1.85f,-.025f),new Vector3(.15f,.05f,.15f),GetMaterial(new Color(.22f,.16f,.12f)));
            leftArm=new GameObject("L arm pivot").transform;leftArm.SetParent(body,false);leftArm.localPosition=new Vector3(-.32f,1.48f,0);
            rightArm=new GameObject("R arm pivot").transform;rightArm.SetParent(body,false);rightArm.localPosition=new Vector3(.32f,1.48f,0);
            MirHumanFactory.Capsule("Left arm",leftArm,new Vector3(-.05f,-.29f,0),new Vector3(.09f,.32f,.09f),skin);
            MirHumanFactory.Capsule("Right arm",rightArm,new Vector3(.05f,-.29f,0),new Vector3(.09f,.32f,.09f),skin);
            leftLeg=new GameObject("L leg pivot").transform;leftLeg.SetParent(body,false);leftLeg.localPosition=new Vector3(-.16f,.95f,0);
            rightLeg=new GameObject("R leg pivot").transform;rightLeg.SetParent(body,false);rightLeg.localPosition=new Vector3(.16f,.95f,0);
            MirHumanFactory.Capsule("Left leg",leftLeg,new Vector3(0,-.43f,0),new Vector3(.12f,.46f,.12f),clothes);
            MirHumanFactory.Capsule("Right leg",rightLeg,new Vector3(0,-.43f,0),new Vector3(.12f,.46f,.12f),clothes);
        }
        Material GetMaterial(Color c)
        {return MirRenderMaterials.Create(c);}
        public void Pose(float normalizedSpeed,float t,MirActivity activity)
        {
            float speed=Mathf.Clamp01(normalizedSpeed);
            float phase=t*(6.5f+seed*1.4f);
            float sine=Mathf.Sin(phase)*speed;
            float arm=activity==MirActivity.Phone?0f:sine;
            if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(arm*20f,0,8f);
            if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(-arm*20f,0,-8f);
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(-sine*25f,0,0);
            if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(sine*25f,0,0);
            if(body!=null)body.localPosition=Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*speed*.025f);
        }
    }
}
