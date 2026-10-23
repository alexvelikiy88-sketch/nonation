using System.Collections.Generic;
using UnityEngine;

namespace MIR
{
    public sealed class MirNpcAgent : MonoBehaviour
    {
        MirCitizen resident;
        MirCity city;
        MirMotionDriver motion;
        readonly Queue<Vector3> route=new Queue<Vector3>();
        MirPlace destination;
        MirActivity activity=MirActivity.Idle;
        float nextPlanTime, nextWorkTime, waitSeconds, gait;
        public void Init(MirCitizen p,MirCity world)
        {
            resident=p;city=world;gait=p.gaitSeed;
            var visuals=new GameObject("NPC appearance").transform;visuals.SetParent(transform,false);
            motion=MirHumanFactory.Attach(visuals,p);
            nextPlanTime=MirGame.Instance.World.gameMinutes+gait*14f;
            transform.position=new Vector3(p.x,.30f,p.z);
            var home=city.Get(p.homeId);
            if(home!=null && Vector3.Distance(transform.position,home.entry)>17f)
            {transform.position=home.entry;resident.x=transform.position.x;resident.z=transform.position.z;}
        }
        void Plan()
        {
            var g=MirGame.Instance;
            float hour=g.World.Hour;
            string target=resident.homeId;
            if(resident.satiety<40 && resident.cash>400 && g.World.Business("market").stock>0)target="market";
            else if(resident.energy<25)target=resident.homeId;
            else if(hour>=8 && hour<17)target=resident.workplaceId;
            else if(resident.mood<45)target="cafe";
            else if(hour>=17 && hour<21 && gait>.53f)target="town";
            destination=city.Get(target)??city.Get(resident.homeId);
            route.Clear();
            if(destination!=null && Vector3.Distance(transform.position,destination.entry)>2.6f)
                foreach(var p in city.SidewalkRoute(transform.position,destination.entry))route.Enqueue(p);
            activity=MirActivity.Walking;
            nextPlanTime=g.World.gameMinutes+12f+gait*18f;
            waitSeconds=0;
        }
        void Update()
        {
            if(resident==null||MirGame.Instance==null||MirGame.Instance.Paused)return;
            float minutes=MirGame.Instance.World.gameMinutes;
            if(destination==null || (minutes>=nextPlanTime && route.Count==0))Plan();
            if(route.Count>0)
            {
                var goal=route.Peek();Vector3 towards=goal-transform.position;towards.y=0;
                if(towards.magnitude<.62f){route.Dequeue();return;}
                float speed=(1.3f+gait*.4f)*Mathf.Clamp01(Mathf.Max(.4f,resident.energy/65f));
                Vector3 d=towards.normalized;
                // Light personal-space avoidance: the camera does not affect agent speed or decisions.
                foreach(var other in MirGame.Instance.NPCs)
                {
                    if(other==null||other==this)continue;
                    Vector3 diff=transform.position-other.transform.position;diff.y=0;
                    if(diff.sqrMagnitude>0.1f && diff.sqrMagnitude<1.1f)d+=(diff.normalized*.36f);
                }
                d.Normalize();transform.position+=d*speed*Time.deltaTime;
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*8f);
                motion.Tick(speed,MirActivity.Walking);
                resident.energy=Mathf.Max(0,resident.energy-.004f*Time.deltaTime);
                return;
            }
            activity=MirActivity.Idle;
            waitSeconds+=Time.deltaTime;
            if(destination!=null)
            {
                switch(destination.kind)
                {
                    case MirPlaceKind.Home:
                        resident.energy=Mathf.Min(100,resident.energy+.020f*Time.deltaTime);
                        if(resident.satiety<35 && resident.inventoryFood>0)MirGame.Instance.Economy.Eat(resident);
                        activity=MirActivity.Sitting;break;
                    case MirPlaceKind.Store:
                        if(waitSeconds>2f && resident.satiety<60 && resident.cash>400)
                            {MirGame.Instance.Economy.BuyFood(resident,destination.business);waitSeconds=0;nextPlanTime=minutes;}
                        activity=MirActivity.Idle;break;
                    case MirPlaceKind.Cafe:
                        resident.mood=Mathf.Min(100,resident.mood+.018f*Time.deltaTime);
                        activity=MirActivity.Talking;break;
                    case MirPlaceKind.Farm:
                        activity=MirActivity.Harvesting;break;
                    default: activity=MirActivity.Working;break;
                }
                if(destination.id==resident.workplaceId && minutes>nextWorkTime && resident.energy>30)
                {
                    if(MirGame.Instance.Economy.Work(resident))nextWorkTime=minutes+420f; // One paid completed shift.
                    else nextWorkTime=minutes+90f;
                }
            }
            motion.Tick(0,activity,false);
        }
        public void WritePosition(){if(resident!=null){resident.x=transform.position.x;resident.z=transform.position.z;}}
    }
}
