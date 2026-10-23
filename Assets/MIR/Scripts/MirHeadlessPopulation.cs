using System;
using System.Collections.Generic;

namespace MIR
{
    // Backend-only residents: travel real distances and transact only upon arrival.
    // Unity offline NPCs use the same intent/layout but drive their own 3D animation.
    public sealed class MirHeadlessPopulation
    {
        sealed class Plan
        {
            public string goal="";
            public MirMapPoint[] path=new MirMapPoint[0];
            public int waypoint;
            public float nextPlanMinute, nextActionMinute;
        }
        readonly MirWorldState world;
        readonly MirEconomy economy;
        readonly Dictionary<string,Plan> agents=new Dictionary<string,Plan>();
        public MirHeadlessPopulation(MirWorldState state,MirEconomy service)
        {
            world=state??throw new ArgumentNullException(nameof(state));
            economy=service??throw new ArgumentNullException(nameof(service));
        }
        static float Distance(MirCitizen c,MirMapPoint point)
        {
            float dx=c.x-point.x,dz=c.z-point.z;
            return (float)Math.Sqrt(dx*dx+dz*dz);
        }
        MirMapPoint Entry(string target,MirCitizen c)
        {
            return target==c.homeId ? MirWorldGeometry.HomeEntry(world,c)
                : MirWorldGeometry.BusinessEntry(target,world.Business(target));
        }
        void Choose(MirCitizen c,Plan plan)
        {
            plan.goal=MirCitizenDecision.Destination(world,c);
            var target=Entry(plan.goal,c);
            plan.path=Distance(c,target)<1.2f?new MirMapPoint[0]:
                MirWorldGeometry.SidewalkRoute(c.x,c.z,target.x,target.z);
            plan.waypoint=0;
            plan.nextPlanMinute=world.gameMinutes+1f+c.gaitSeed*2f;
        }
        bool Moving(MirCitizen c,Plan plan,float seconds)
        {
            float speed=1.3f+Math.Max(0f,Math.Min(1f,c.gaitSeed))*.4f;
            float budget=speed*seconds;
            while(budget>0f && plan.waypoint<plan.path.Length)
            {
                var point=plan.path[plan.waypoint];
                float dx=point.x-c.x,dz=point.z-c.z;
                float distance=(float)Math.Sqrt(dx*dx+dz*dz);
                if(distance<=.3f){plan.waypoint++;continue;}
                float step=Math.Min(distance,budget);
                c.x+=dx/distance*step;c.z+=dz/distance*step;
                budget-=step;
                if(step>=distance-.001f)plan.waypoint++;
            }
            return plan.waypoint<plan.path.Length;
        }
        void Act(MirCitizen c,Plan plan,float seconds)
        {
            if(plan.goal==c.homeId)
            {
                c.energy=Math.Min(100f,c.energy+seconds*.020f);
                if(c.satiety<40f && c.inventoryFood>0)economy.Eat(c);
                return;
            }
            if(plan.goal=="cafe")
            {
                c.mood=Math.Min(100f,c.mood+seconds*.018f);
                return;
            }
            if(world.gameMinutes<plan.nextActionMinute)return;
            if(plan.goal=="market" && c.inventoryFood==0 && c.satiety<60f)
            {
                var shop=world.Business("market");
                if(shop!=null && economy.BuyFood(c,shop))
                {
                    plan.nextPlanMinute=world.gameMinutes;
                    plan.nextActionMinute=world.gameMinutes+10f;
                }
            }
            else if(plan.goal==c.workplaceId && c.energy>25f)
            {
                economy.Work(c);
                plan.nextActionMinute=world.gameMinutes+2f;
            }
        }
        public void Advance(float seconds)
        {
            if(seconds<=0 || float.IsNaN(seconds) || float.IsInfinity(seconds))return;
            seconds=Math.Min(.25f,seconds);
            foreach(var c in world.citizens)
            {
                if(c.player)continue;
                Plan p;
                if(!agents.TryGetValue(c.id,out p)){p=new Plan();agents[c.id]=p;}
                if(p.goal.Length==0 || (p.waypoint>=p.path.Length &&
                   world.gameMinutes>=p.nextPlanMinute))Choose(c,p);
                if(Moving(c,p,seconds))continue;
                Act(c,p,seconds);
            }
        }
    }
}
