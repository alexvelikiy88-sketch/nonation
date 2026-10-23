using UnityEngine;
using System.Collections.Generic;

namespace MIR
{
    public sealed class MirFreightDriver : MonoBehaviour
    {
        MirGame game;
        MirVehicle vehicle;
        MirFreightOrder current;
        Vector3 warehouse,market;
        readonly Queue<Vector3> waypoints=new Queue<Vector3>();
        float stageHold;
        public void Init(MirGame g)
        {
            game=g;
            var from=g.City.Get("warehouse");var to=g.City.Get("market");
            warehouse=from.entry+new Vector3(0,0,-5);
            market=to.entry+new Vector3(0,0,-5);
            vehicle=MirVehicle.Spawn(warehouse,true);g.City.Vehicles.Add(vehicle);
            vehicle.name="MIR Cargo Truck · real stock delivery";
        }
        void Route(bool deliver)
        {
            waypoints.Clear();
            if(deliver)
            {
                // Explicit street-lane route, rather than cutting through houses with a straight line.
                foreach(var p in new[]{new Vector3(-80,0,0),new Vector3(-40,0,0),new Vector3(0,0,0),
                    new Vector3(0,0,-40),new Vector3(20,0,-40),market})waypoints.Enqueue(p);
            }
            else
            {
                foreach(var p in new[]{new Vector3(20,0,-40),new Vector3(0,0,-40),new Vector3(0,0,0),
                    new Vector3(-40,0,0),new Vector3(-80,0,0),warehouse})waypoints.Enqueue(p);
            }
        }
        bool MoveRoute(bool deliver)
        {
            if(waypoints.Count==0)Route(deliver);
            if(waypoints.Count==0)return true;
            Vector3 target=waypoints.Peek();
            target.y=vehicle.transform.position.y;
            float dist=Vector3.Distance(vehicle.transform.position,target);
            if(dist<3f)
            {
                waypoints.Dequeue();
                if(waypoints.Count==0){vehicle.SetPlayerControl(0,0,true);return true;}
            }
            else vehicle.Autopilot(target,Time.deltaTime);
            return false;
        }
        void Update()
        {
            if(game==null||game.Paused||vehicle==null)return;
            var next=game.World.ActiveFreight();
            if(next==null){vehicle.SetPlayerControl(0,0,true);waypoints.Clear();return;}
            if(current!=next){current=next;stageHold=0;waypoints.Clear();}
            stageHold+=Time.deltaTime;
            if(current.status==MirOrderStatus.Created)
            {
                if(Vector3.Distance(vehicle.transform.position,warehouse)>3)
                    vehicle.Autopilot(warehouse,Time.deltaTime);
                else{vehicle.SetPlayerControl(0,0,true);game.Economy.SetFreightStage(current,MirOrderStatus.Loading);stageHold=0;}
                return;
            }
            if(current.status==MirOrderStatus.Loading)
            {
                vehicle.SetPlayerControl(0,0,true);
                if(stageHold>2f){game.Economy.SetFreightStage(current,MirOrderStatus.Driving);stageHold=0;Route(true);}
                return;
            }
            if(current.status==MirOrderStatus.Driving)
            {
                if(MoveRoute(true))
                {game.Economy.SetFreightStage(current,MirOrderStatus.Unloading);stageHold=0;waypoints.Clear();}
                return;
            }
            if(current.status==MirOrderStatus.Unloading)
            {
                vehicle.SetPlayerControl(0,0,true);
                if(stageHold>3f){game.Economy.SetFreightStage(current,MirOrderStatus.Returning);stageHold=0;Route(false);}
                return;
            }
            if(current.status==MirOrderStatus.Returning)
            {
                if(MoveRoute(false))
                {game.Economy.SetFreightStage(current,MirOrderStatus.Completed);vehicle.SetPlayerControl(0,0,true);game.Notify("Рейс завершён, грузовик вернулся на базу.");}
            }
        }
    }
}
