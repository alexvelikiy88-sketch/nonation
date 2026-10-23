using UnityEngine;

namespace MIR
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MirPlayer : MonoBehaviour
    {
        public MirCitizen Resident {get;private set;}
        public MirVehicle Driving {get;private set;}
        public MirMotionDriver Motion {get;private set;}
        CharacterController controller;
        Transform avatarRoot;
        float verticalVelocity;
        public void Init(MirCitizen citizen)
        {
            Resident=citizen;controller=GetComponent<CharacterController>();
            controller.height=1.78f;controller.radius=.28f;controller.center=new Vector3(0,.9f,0);
            controller.stepOffset=.28f;controller.slopeLimit=45f;
            transform.position=new Vector3(citizen.x,.35f,citizen.z);
            avatarRoot=new GameObject("Animated Visual").transform;avatarRoot.SetParent(transform,false);
            Motion=MirHumanFactory.Attach(avatarRoot,citizen);
        }
        void Update()
        {
            if(Resident==null||MirGame.Instance==null||MirGame.Instance.Paused)return;
            if(Input.GetKeyDown(KeyCode.F))ToggleVehicle();
            if(Input.GetKeyDown(KeyCode.E))Interact();
            if(Input.GetKeyDown(KeyCode.R))MirGame.Instance.Economy.Eat(Resident);
            if(Driving!=null)
            {
                Driving.SetPlayerControl(Input.GetAxisRaw("Vertical"),Input.GetAxisRaw("Horizontal"),Input.GetKey(KeyCode.Space));
                Motion.Tick(0,MirActivity.Driving,false);
                return;
            }
            float h=Input.GetAxisRaw("Horizontal"),v=Input.GetAxisRaw("Vertical");
            Vector3 motion=new Vector3(h,0,v);float input=Mathf.Clamp01(motion.magnitude);
            var cam=Camera.main; if(cam!=null)
            {
                Vector3 forward=cam.transform.forward;forward.y=0;forward.Normalize();
                Vector3 right=cam.transform.right;right.y=0;right.Normalize();
                motion=(forward*v+right*h).normalized;
            }
            bool running=Input.GetKey(KeyCode.LeftShift)&&Resident.energy>10;
            float velocity=running?5.5f:2.7f;
            if(input>.01f)
            {
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(motion),Time.deltaTime*10);
                Resident.energy=Mathf.Max(0,Resident.energy-(running?.012f:.004f)*Time.deltaTime);
            }
            verticalVelocity+=-18f*Time.deltaTime;
            if(controller.isGrounded)verticalVelocity=-.55f;
            controller.Move((motion*velocity*input+Vector3.up*verticalVelocity)*Time.deltaTime);
            Motion.Tick(velocity*input,input>.01f?(running?MirActivity.Running:MirActivity.Walking):MirActivity.Idle,input>.01f);
        }
        public void WritePosition()
        {
            if(Resident==null)return;
            Vector3 p=Driving!=null?Driving.transform.position:transform.position;
            Resident.x=p.x;Resident.z=p.z;
        }
        public void ToggleVehicle()
        {
            if(Driving!=null){ExitVehicle();return;}
            MirVehicle near=null;float d=9f;
            foreach(var car in MirGame.Instance.City.Vehicles)
            {
                if(car==null || car.HasDriver)continue;
                float distance=Vector3.Distance(transform.position,car.transform.position);
                if(distance<d){d=distance;near=car;}
            }
            if(near==null){MirGame.Instance.Notify("Нет доступной машины рядом. Подойди ближе.");return;}
            controller.enabled=false;
            Driving=near;near.HasDriver=true;
            transform.SetParent(near.transform);transform.localPosition=new Vector3(-.37f,.43f,.01f);
            transform.localRotation=Quaternion.identity;
            MirGame.Instance.Notify("Посадка в автомобиль. W/S — газ/тормоз, A/D — руль, F — выход.");
        }
        public void ExitVehicle()
        {
            if(Driving==null)return;
            MirVehicle old=Driving;Driving=null;old.SetPlayerControl(0,0,true);old.HasDriver=false;
            transform.SetParent(null);transform.position=old.transform.TransformPoint(new Vector3(-2,.45f,-1));
            transform.rotation=old.transform.rotation;verticalVelocity=0;
            controller.enabled=true;Motion.ForceReset();
        }
        public void Interact()
        {
            var place=MirGame.Instance.City.Nearest(transform.position,7f);
            if(place==null){MirGame.Instance.Notify("Подойди к входу здания для взаимодействия.");return;}
            var economy=MirGame.Instance.Economy;
            switch(place.kind)
            {
                case MirPlaceKind.Store:case MirPlaceKind.Cafe:
                    if(economy.BuyFood(Resident,place.business))MirGame.Instance.Notify("Товар в инвентаре. Нажми R, чтобы употребить.");
                    else MirGame.Instance.Notify("Нет товара или денег.");break;
                case MirPlaceKind.Work:case MirPlaceKind.Packing:case MirPlaceKind.Garage:
                case MirPlaceKind.Warehouse:case MirPlaceKind.Farm:
                    if(Resident.workplaceId!=place.id){MirGame.Instance.Notify("Это не твоё рабочее место. Доступны оплачиваемые контракты.");break;}
                    if(!economy.Work(Resident))MirGame.Instance.Notify("Смена недоступна: деньги компании или энергия.");break;
                case MirPlaceKind.Home:
                    if(place.id==Resident.homeId){Resident.energy=Mathf.Min(100,Resident.energy+26);MirGame.Instance.Notify("Отдых дома. Восстановлена энергия.");}
                    else MirGame.Instance.Notify("Это чужой дом.");break;
                case MirPlaceKind.Fuel:
                    if(Driving!=null)Driving.Refuel(Resident);else MirGame.Instance.Notify("Чтобы заправиться, подъедь на машине.");break;
                default:MirGame.Instance.Notify("Осмотрено: "+place.name);break;
            }
        }
        public void OrderFreight()
        {
            if(MirGame.Instance.Economy.CreateRestock()!=null)MirGame.Instance.Notify("Оформлен рейс со склада на рынок.");
            else MirGame.Instance.Notify("Заказ недоступен: деньги, товар, вместимость или занят грузовик.");
        }
    }
}
