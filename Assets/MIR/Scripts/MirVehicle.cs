using UnityEngine;
using System.Collections.Generic;

namespace MIR
{
    public sealed class MirVehicle : MonoBehaviour
    {
        public bool HasDriver {get;set;}
        public bool IsTruck {get; private set;}
        public float Fuel {get;private set;}=55f;
        public float Speed {get;private set;}
        readonly List<Transform> wheelPivots=new List<Transform>();
        Transform steerVisual,driverDoor;
        Transform[] importedWheels;Quaternion[] importedWheelRotations;
        GameObject leftIndicator,rightIndicator,brakeLeft,brakeRight;
        Light leftHeadLight,rightHeadLight;
        Material body,glass,trim,tyre,lensWhite,lensRed,lensOrange;
        float throttle,steering,brakeFactor,spin;
        float signalClock;
        bool hazard;
        float doorAngle;
        public static MirVehicle Spawn(Vector3 at,bool truck,bool tryLicensedHonda=false)
        {
            var root=new GameObject(truck?"Cargo truck · supply economy":"Driveable street car");
            root.transform.position=at;root.transform.rotation=Quaternion.Euler(0,90,0);
            var vehicle=root.AddComponent<MirVehicle>();vehicle.Init(truck);
            if(!truck && tryLicensedHonda)vehicle.TryHondaArt();
            return vehicle;
        }
        Material NewMat(Color c,float metal=0f)
        {
            return MirRenderMaterials.Create(c,metal);
        }
        GameObject Cube(string n,Vector3 p,Vector3 size,Material mat,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(parent??transform,false);
            go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            Destroy(go.GetComponent<Collider>());return go;
        }
        void Init(bool truck)
        {
            IsTruck=truck;
            body=NewMat(truck?new Color(.64f,.70f,.63f):new Color(.20f,.37f,.48f),.5f);
            glass=NewMat(new Color(.12f,.23f,.28f),.14f);
            trim=NewMat(new Color(.10f,.13f,.14f),.4f);
            tyre=NewMat(new Color(.075f,.08f,.085f));
            lensWhite=NewMat(new Color(.95f,.92f,.77f));lensRed=NewMat(new Color(.75f,.11f,.09f));lensOrange=NewMat(new Color(.9f,.49f,.13f));
            float length=truck?7.4f:4.5f;
            Cube("Undercarriage",new Vector3(0,.82f,0),new Vector3(2.0f,.55f,length),trim);
            Cube("Body",new Vector3(0,1.15f,0),new Vector3(2.22f,.88f,length*.87f),body);
            Cube("Front hood",new Vector3(0,1.58f,length*.28f),new Vector3(2.14f,.35f,length*.25f),body);
            Cube("Windshield",new Vector3(0,2.04f,.67f),new Vector3(1.95f,.90f,.11f),glass);
            Cube("Cabin roof",new Vector3(0,2.44f,-.46f),new Vector3(2.1f,.25f,2.50f),body);
            Cube("Back glass",new Vector3(0,2.06f,-1.43f),new Vector3(1.93f,.8f,.08f),glass);
            if(truck)Cube("Cargo box",new Vector3(0,2.25f,-2.40f),new Vector3(2.40f,2.85f,3.5f),body);
            Cube("Dash",new Vector3(0,1.55f,.65f),new Vector3(1.9f,.23f,.48f),trim);
            for(int side=-1;side<=1;side+=2)
            {
                Cube("Seat back",new Vector3(side*.56f,1.58f,-.45f),new Vector3(.65f,.80f,.20f),trim);
                Cube("Seat cushion",new Vector3(side*.56f,1.13f,-.20f),new Vector3(.7f,.18f,.85f),trim);
                Cube("Wing mirror",new Vector3(side*1.24f,1.94f,.82f),new Vector3(.29f,.23f,.34f),trim);
            }
            steerVisual=new GameObject("Steering wheel pivot").transform;steerVisual.SetParent(transform,false);
            steerVisual.localPosition=new Vector3(-.60f,1.63f,.35f);
            var st=GameObject.CreatePrimitive(PrimitiveType.Cylinder);st.name="Steering wheel";
            st.transform.SetParent(steerVisual,false);st.transform.localScale=new Vector3(.27f,.04f,.27f);
            st.transform.localRotation=Quaternion.Euler(70,0,0);st.GetComponent<Renderer>().sharedMaterial=trim;Destroy(st.GetComponent<Collider>());
            driverDoor=new GameObject("Door hinge · driver").transform;driverDoor.SetParent(transform,false);
            driverDoor.localPosition=new Vector3(-1.14f,1.35f,.44f);
            Cube("Driver door",new Vector3(0,0,-.55f),new Vector3(.09f,.65f,1.20f),body,driverDoor);
            for(int i=0;i<4;i++)
            {
                bool front=i<2;int side=(i%2==0)?-1:1;
                var pivot=new GameObject("Wheel · "+(front?"front":"rear")+" · "+side).transform;
                pivot.SetParent(transform,false);pivot.localPosition=new Vector3(side*1.09f,.51f,front?length*.29f:-length*.28f);
                var cyl=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cyl.name="Tyre mesh";
                cyl.transform.SetParent(pivot,false);cyl.transform.localScale=new Vector3(.54f,.16f,.54f);
                cyl.transform.localRotation=Quaternion.Euler(0,0,90);
                cyl.GetComponent<Renderer>().sharedMaterial=tyre;Destroy(cyl.GetComponent<Collider>());
                wheelPivots.Add(pivot);
            }
            leftIndicator=Cube("Indicator left",new Vector3(-.87f,1.34f,length*.445f),new Vector3(.23f,.15f,.12f),lensOrange);
            rightIndicator=Cube("Indicator right",new Vector3(.87f,1.34f,length*.445f),new Vector3(.23f,.15f,.12f),lensOrange);
            brakeLeft=Cube("Rear stop L",new Vector3(-.85f,1.30f,-length*.445f),new Vector3(.35f,.18f,.10f),lensRed);
            brakeRight=Cube("Rear stop R",new Vector3(.85f,1.30f,-length*.445f),new Vector3(.35f,.18f,.10f),lensRed);
            leftHeadLight=Headlight(new Vector3(-.78f,1.35f,length*.43f));
            rightHeadLight=Headlight(new Vector3(.78f,1.35f,length*.43f));
            var collider=gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.42f,0);
            collider.size=new Vector3(2.25f,2.5f,length*.88f);
            // Kinematic street vehicle, not a claim of full tyre/suspension physics.
            var rb=gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        }
        Light Headlight(Vector3 pos)
        {
            var obj=new GameObject("Headlamp");obj.transform.SetParent(transform,false);obj.transform.localPosition=pos;
            var spot=obj.AddComponent<Light>();spot.type=LightType.Spot;spot.spotAngle=42;spot.range=25;
            spot.color=new Color(1f,.94f,.82f);spot.intensity=.0f;spot.shadows=LightShadows.None;
            return spot;
        }
        void TryHondaArt()
        {
            // The commercial build MUST NOT include this Creative Commons NonCommercial model.
            var prefab=Resources.Load<GameObject>("MIR/HondaCivic_NC");
            if(prefab==null)return;
            var art=Instantiate(prefab,transform);art.name="Honda Civic BTCC · NC study mesh";
            var renders=art.GetComponentsInChildren<Renderer>(true);
            if(renders.Length==0){Destroy(art);return;}
            Bounds bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
            float longitudinal=Mathf.Max(bounds.size.x,bounds.size.z);
            if(longitudinal<.01f){Destroy(art);return;}
            art.transform.localScale*=Mathf.Clamp(4.45f/longitudinal,.01f,80f);
            // Centre the art on the drivable collision body and align approximate wheel ground plane.
            bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
            art.transform.position+=new Vector3(transform.position.x-bounds.center.x,transform.position.y+.45f-bounds.min.y,transform.position.z-bounds.center.z);
            foreach(Transform t in transform)
            {
                if(t==art.transform)continue;
                if(t.name.Contains("Wheel"))
                {foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;continue;}
                if(t.name=="Body"||t.name=="Cabin roof"||t.name=="Front hood"||t.name=="Windshield"||t.name=="Back glass"||t.name=="Undercarriage")
                {var r=t.GetComponent<Renderer>();if(r!=null)r.enabled=false;}
            }
            string[] names={"WHEEL_LF_","WHEEL_RF_","WHEEL_LR_","WHEEL_RR_"};
            importedWheels=new Transform[4];importedWheelRotations=new Quaternion[4];
            var all=art.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<names.Length;i++)foreach(var t in all)
                if(t.name.StartsWith(names[i],System.StringComparison.OrdinalIgnoreCase))
                {importedWheels[i]=t;importedWheelRotations[i]=t.localRotation;break;}
        }
        public void SetPlayerControl(float accelerator,float turn,bool brake)
        {throttle=Mathf.Clamp(accelerator,-1,1);steering=Mathf.Clamp(turn,-1,1);brakeFactor=brake?1f:0f;}
        public void Autopilot(Vector3 goal,float dt)
        {
            if(HasDriver)return;
            Vector3 offset=goal-transform.position;offset.y=0;
            float distance=offset.magnitude;
            if(distance<1.2f){SetPlayerControl(0,0,true);return;}
            float signed=Vector3.SignedAngle(transform.forward,offset.normalized,Vector3.up);
            float steeringInput=Mathf.Clamp(signed/32f,-1f,1f);
            SetPlayerControl(distance<8f?.23f:.64f,steeringInput,false);
        }
        void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,.1f);
            if(dt<=0)return;
            Speed=Mathf.MoveTowards(Speed,throttle*(IsTruck?10f:17f),dt*(brakeFactor>0?21f:5.5f));
            if(Mathf.Abs(throttle)<.02f)Speed=Mathf.MoveTowards(Speed,0,dt*.9f);
            Fuel=Mathf.Max(0,Fuel-Mathf.Abs(Speed)*dt*.00018f);
            if(Fuel<=0)Speed=Mathf.MoveTowards(Speed,0,dt*12f);
            float steerAngle=steering*27f;
            float heading=steering*Mathf.Clamp(Speed/9f,-1,1)*35f*dt;
            transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y+heading,0);
            transform.position+=transform.forward*Speed*dt;
            spin+=Speed*dt/Mathf.Max(.1f,.54f)*Mathf.Rad2Deg;
            for(int i=0;i<wheelPivots.Count;i++)
                wheelPivots[i].localRotation=Quaternion.Euler(0,i<2?steerAngle:0,0)*Quaternion.Euler(spin,0,0);
            if(steerVisual!=null)steerVisual.localRotation=Quaternion.Euler(0,0,steering*-50f);
            if(importedWheels!=null)for(int i=0;i<importedWheels.Length;i++)
                if(importedWheels[i]!=null)importedWheels[i].localRotation=
                    importedWheelRotations[i]*Quaternion.Euler(0,i<2?steerAngle:0,0)*Quaternion.Euler(spin,0,0);
            doorAngle=Mathf.MoveTowards(doorAngle,HasDriver?0:0,dt*120);
            if(driverDoor!=null)driverDoor.localRotation=Quaternion.Euler(0,doorAngle,0);
            signalClock+=dt;
            bool blinking=Mathf.FloorToInt(signalClock*1.3f)%2==0;
            leftIndicator.SetActive(blinking&&(hazard||steering<-.3f));
            rightIndicator.SetActive(blinking&&(hazard||steering>.3f));
            bool stop=brakeFactor>.2f;
            brakeLeft.transform.localScale=new Vector3(.35f,stop?.25f:.18f,.10f);
            brakeRight.transform.localScale=brakeLeft.transform.localScale;
            float light=MirGame.Instance!=null&&(MirGame.Instance.World.Hour>=19||MirGame.Instance.World.Hour<6)?2.3f:0f;
            leftHeadLight.intensity=light;rightHeadLight.intensity=light;
        }
        public void Refuel(MirCitizen buyer)
        {
            var station=MirGame.Instance.World.Business("fuel");
            if(station==null||station.stock<1||buyer==null)return;
            int fuelAmount=Mathf.Min(50,station.stock);
            if(MirGame.Instance.Economy.Transfer(MirGame.Instance.World.NewId("fuel"),buyer.id,"fuel",fuelAmount*station.price,"fuel purchased"))
            {Fuel+=fuelAmount;station.stock-=fuelAmount;MirGame.Instance.Notify("Оплачено "+fuelAmount+" л топлива.");}
        }
        public void SetHazard(bool state){hazard=state;}
    }
}
