using System;
using System.Collections.Generic;
using UnityEngine;

namespace MIR
{
    public sealed class MirPlace
    {
        public string id,name;
        public MirPlaceKind kind;
        public Vector3 entry;
        public MirBusiness business;
    }
    public sealed class MirCity
    {
        public readonly List<MirPlace> Places=new List<MirPlace>();
        public readonly List<MirVehicle> Vehicles=new List<MirVehicle>();
        readonly Dictionary<string,MirPlace> byId=new Dictionary<string,MirPlace>();
        readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
        readonly List<Light> lamps=new List<Light>();
        Transform root;
        public Transform Root{get{return root;}}
        public MirPlace Get(string id){MirPlace p;return id!=null&&byId.TryGetValue(id,out p)?p:null;}
        public MirPlace Nearest(Vector3 pos,float distance=5.5f)
        {
            MirPlace best=null;float nearest=distance*distance;
            foreach(var p in Places){float sq=(p.entry-pos).sqrMagnitude;if(sq<nearest){nearest=sq;best=p;}}
            return best;
        }
        Material Mat(string key,Color c,float metallic=0f,float smooth=0.12f)
        {
            Material m;if(mats.TryGetValue(key,out m))return m;
            m=MirRenderMaterials.Create(c,metallic,smooth);
            m.enableInstancing=true;mats[key]=m;return m;
        }
        GameObject Cube(string name,Vector3 pos,Vector3 scale,Material material,Transform parent=null,bool collider=false)
        {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent??root);
            go.transform.position=pos;go.transform.localScale=scale;
            var r=go.GetComponent<Renderer>();r.sharedMaterial=material;
            if(!collider) UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }
        void Place(MirPlace p){Places.Add(p);byId[p.id]=p;}
        // Optional pilot art pass. These are visual-only modules, not gameplay blockers.
        // They do not replace licensed facade meshes required by the visual benchmark.
        void RetailFront(Vector3 center,float w,float d)
        {
            float front=center.z-d*.5f;
            var frame=Mat("retail-metal",new Color(.18f,.21f,.22f),.45f,.25f);
            var pane=Mat("retail-glass",new Color(.16f,.27f,.30f),.06f,.7f);
            var canopy=Mat("retail-canopy",new Color(.38f,.33f,.29f),.07f,.2f);
            var planter=Mat("retail-planter",new Color(.43f,.43f,.39f),.08f,.14f);
            var foliage=Mat("retail-foliage",new Color(.27f,.40f,.26f));
            Cube("Retail fascia",new Vector3(center.x,3.29f,front-.18f),
                new Vector3(w-.65f,.52f,.30f),frame);
            for(int j=-1;j<=1;j++)
            {
                float x=center.x+j*w*.27f;
                float panelWidth=w*.26f;
                Cube("Storefront frame",new Vector3(x,1.70f,front-.20f),
                    new Vector3(panelWidth,2.55f,.16f),frame);
                Cube("Storefront glazed panel",new Vector3(x,1.70f,front-.31f),
                    new Vector3(panelWidth-.20f,2.34f,.055f),pane);
            }
            Cube("Storefront mullion L",new Vector3(center.x-w*.135f,1.7f,front-.34f),
                new Vector3(.10f,2.55f,.12f),frame);
            Cube("Storefront mullion R",new Vector3(center.x+w*.135f,1.7f,front-.34f),
                new Vector3(.10f,2.55f,.12f),frame);
            Cube("Retail entrance handle",new Vector3(center.x+.40f,1.53f,front-.39f),
                new Vector3(.075f,.43f,.07f),Mat("handles",new Color(.73f,.68f,.55f),.62f,.32f));
            Cube("Retail rain canopy",new Vector3(center.x,3.08f,front-.9f),
                new Vector3(w-.55f,.18f,1.35f),canopy);
            for(int side=-1;side<=1;side+=2)
            {
                float x=center.x+side*(w*.44f);
                Cube("Shopfront planter",new Vector3(x,.43f,front-1.08f),
                    new Vector3(.70f,.70f,.70f),planter);
                var bush=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bush.name="Shopfront planted shrub";
                bush.transform.SetParent(root);
                bush.transform.position=new Vector3(x,1.06f,front-1.08f);
                bush.transform.localScale=new Vector3(.95f,.80f,.95f);
                bush.GetComponent<Renderer>().sharedMaterial=foliage;
                UnityEngine.Object.Destroy(bush.GetComponent<Collider>());
            }
        }
        void Balcony(Vector3 center,float w,float d,int floor)
        {
            float front=center.z-d*.5f;
            float x=center.x+(floor%2==0?-1:1)*w*.27f;
            float height=floor*3.5f;
            var slab=Mat("balcony-stone",new Color(.68f,.66f,.60f));
            var rail=Mat("balcony-rail",new Color(.23f,.26f,.26f),.34f,.22f);
            var planter=Mat("balcony-planter",new Color(.41f,.43f,.38f));
            var leaves=Mat("balcony-green",new Color(.28f,.40f,.27f));
            Cube("Balcony slab",new Vector3(x,height+1.04f,front-.67f),
                new Vector3(2.55f,.18f,1.42f),slab);
            Cube("Balcony front handrail",new Vector3(x,height+1.97f,front-1.33f),
                new Vector3(2.50f,.10f,.12f),rail);
            for(int post=-1;post<=1;post++)
                Cube("Balcony rail post",new Vector3(x+post*1.06f,height+1.57f,front-1.33f),
                    new Vector3(.09f,.78f,.10f),rail);
            Cube("Balcony planter",new Vector3(x-.65f,height+1.24f,front-1.17f),
                new Vector3(.60f,.22f,.26f),planter);
            Cube("Balcony planted foliage",new Vector3(x-.65f,height+1.48f,front-1.17f),
                new Vector3(.62f,.33f,.28f),leaves);
        }
        void BlockBuilding(string label,Vector3 center,int floors,Color color,
            float w=14,float d=14,bool retail=false,bool balconies=false)
        {
            float h=floors*3.5f;
            var brick=Mat("facade-"+color.ToString(),color,0.03f,0.15f);
            Cube(label+" · facade",new Vector3(center.x,h/2f,center.z),new Vector3(w,h,d),brick,root,true);
            Cube("cornice",new Vector3(center.x,h+0.15f,center.z),new Vector3(w+.65f,.34f,d+.65f),Mat("cornice",new Color(.68f,.67f,.60f)));
            var glass=Mat("windows",new Color(.13f,.26f,.30f),.05f,.65f);
            var windowTrim=Mat("frames",new Color(.19f,.22f,.23f),.32f,.29f);
            for(int floor=0;floor<floors;floor++)for(int j=0;j<3;j++)
            {
                if(retail && floor==0)continue; // glazed shop replaces residential windows
                float wx=center.x+(j-1)*w*.27f;
                float wy=2.10f+floor*3.5f;
                Cube("Window frame",new Vector3(wx,wy,center.z-d/2-.13f),new Vector3(1.78f,1.75f,.18f),windowTrim);
                Cube("Glass",new Vector3(wx,wy,center.z-d/2-.245f),new Vector3(1.48f,1.48f,.04f),glass);
                Cube("Window sill",new Vector3(wx,wy-.86f,center.z-d/2-.34f),new Vector3(1.95f,.18f,.40f),Mat("sills",new Color(.76f,.72f,.63f)));
            }
            if(retail)RetailFront(center,w,d);
            else
            {
                Cube("Door",new Vector3(center.x,1.20f,center.z-d/2-.15f),new Vector3(2.1f,2.38f,.28f),Mat("doors",new Color(.24f,.31f,.30f)),root,false);
                Cube("Entrance awning",new Vector3(center.x,2.68f,center.z-d/2-.85f),new Vector3(4.1f,.20f,1.85f),Mat("awning",new Color(.33f,.38f,.34f)));
            }
            if(balconies)for(int floor=1;floor<floors;floor++)
                Balcony(center,w,d,floor);
        }
        void Tree(Vector3 p,float scale)
        {
            var trunk=Mat("tree-trunk",new Color(.36f,.29f,.21f));var foliage=Mat("leaf",new Color(.22f,.36f,.27f));
            Cube("tree trunk",p+Vector3.up*(2.2f*scale),new Vector3(.36f,4.4f,.36f)*scale,trunk);
            foreach(var dir in new[]{new Vector3(0,0,0),new Vector3(.8f,-.3f,.6f),new Vector3(-.8f,-.2f,.4f)})
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="tree crown";go.transform.SetParent(root);
                go.transform.position=p+new Vector3(0,4.8f*scale,0)+dir*scale;
                go.transform.localScale=new Vector3(3.4f,2.8f,3.4f)*scale;
                go.GetComponent<Renderer>().sharedMaterial=foliage;
                UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            }
        }
        void Bench(Vector3 p,float yaw)
        {
            var wood=Mat("wood",new Color(.48f,.29f,.17f));var metal=Mat("benchmetal",new Color(.13f,.19f,.20f),.5f);
            Transform group=new GameObject("Street bench").transform;group.SetParent(root);group.position=p;group.rotation=Quaternion.Euler(0,yaw,0);
            Cube("Seat",group.TransformPoint(new Vector3(0,.47f,0)),new Vector3(2.15f,.16f,.67f),wood,group);
            Cube("Back",group.TransformPoint(new Vector3(0,.93f,.28f)),new Vector3(2.15f,.91f,.16f),wood,group);
            for(int a=-1;a<=1;a+=2)Cube("Leg",group.TransformPoint(new Vector3(a*.83f,.25f,0)),new Vector3(.12f,.50f,.68f),metal,group);
        }
        void Lamp(Vector3 p,bool isBroken=false)
        {
            var m=Mat("streetlamp",new Color(.24f,.30f,.29f),.45f,.4f);
            Cube("Lamp post",p+Vector3.up*3.6f,new Vector3(.16f,7.2f,.16f),m);
            Cube("Lamp arm",p+new Vector3(.68f,7.13f,0),new Vector3(1.6f,.12f,.16f),m);
            Cube("Lamp housing",p+new Vector3(1.39f,7.05f,0),new Vector3(.8f,.15f,.4f),m);
            var l=new GameObject("Light").AddComponent<Light>();l.transform.SetParent(root);l.transform.position=p+new Vector3(1.4f,6.91f,0);
            l.type=LightType.Spot;l.transform.rotation=Quaternion.Euler(90,0,0);l.spotAngle=80;l.range=14;
            l.color=new Color(1f,.83f,.61f);l.intensity=0; l.shadows=LightShadows.None;
            lamps.Add(l);
            if(isBroken)l.gameObject.AddComponent<MirFlickerLamp>();
        }
        void AsphaltRoads()
        {
            var ground=Mat("ground",new Color(.29f,.35f,.28f));
            Cube("ground",new Vector3(0,-.35f,0),new Vector3(290,.65f,260),ground,root,true);
            var asphalt=Mat("asphalt",new Color(.18f,.21f,.23f),0,.11f);
            var pavement=Mat("pavement",new Color(.64f,.63f,.57f));
            var kerb=Mat("curb",new Color(.77f,.77f,.70f));
            var white=Mat("road-white",new Color(.92f,.88f,.76f));
            for(int n=-2;n<=2;n++)
            {
                int x=n*40;
                Cube("N-S road",new Vector3(x,.01f,0),new Vector3(9,.08f,220),asphalt);
                Cube("E-W road",new Vector3(0,.015f,x),new Vector3(220,.09f,9),asphalt);
                for(int side=-1;side<=1;side+=2)
                {
                    Cube("Sidewalk E-W",new Vector3(0,.16f,x+side*7f),new Vector3(220,.28f,4.3f),pavement,root,true);
                    Cube("Sidewalk N-S",new Vector3(x+side*7f,.16f,0),new Vector3(4.3f,.28f,220),pavement,root,true);
                    Cube("Curb",new Vector3(0,.25f,x+side*4.67f),new Vector3(220,.30f,.15f),kerb);
                    Cube("Curb",new Vector3(x+side*4.67f,.25f,0),new Vector3(.15f,.30f,220),kerb);
                }
                for(int z=-106;z<=106;z+=13)
                    Cube("Lane center dash",new Vector3(x,.09f,z),new Vector3(.14f,.02f,4f),white);
                for(int xx=-106;xx<=106;xx+=13)
                    Cube("Lane center dash",new Vector3(xx,.10f,x),new Vector3(4f,.02f,.14f),white);
            }
            // Crosswalks at intersections: actual marks, not large floating decals.
            for(int a=-2;a<=2;a++)for(int b=-2;b<=2;b++)
            {
                Vector3 p=new Vector3(a*40,0,b*40);
                for(int k=-3;k<=3;k++)
                {
                    Cube("Zebra N",p+new Vector3(k*1.05f,.11f,7f),new Vector3(.67f,.02f,2.4f),white);
                    Cube("Zebra W",p+new Vector3(-7f,.11f,k*1.05f),new Vector3(2.4f,.02f,.67f),white);
                }
            }
        }
        void MarketSquare()
        {
            var stone=Mat("square",new Color(.70f,.64f,.53f));
            Cube("Civic square",new Vector3(20,.10f,20),new Vector3(27,.23f,27),stone,root,true);
            var basin=Mat("basin",new Color(.69f,.68f,.60f));var water=Mat("water",new Color(.26f,.59f,.61f),.2f,.85f);
            var font=GameObject.CreatePrimitive(PrimitiveType.Cylinder);font.name="Fountain basin";font.transform.SetParent(root);
            font.transform.position=new Vector3(20,.41f,20);font.transform.localScale=new Vector3(3.1f,.29f,3.1f);
            font.GetComponent<Renderer>().sharedMaterial=basin;UnityEngine.Object.Destroy(font.GetComponent<Collider>());
            var wt=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wt.name="Fountain water";wt.transform.SetParent(root);
            wt.transform.position=new Vector3(20,.73f,20);wt.transform.localScale=new Vector3(2.65f,.06f,2.65f);
            wt.GetComponent<Renderer>().sharedMaterial=water;UnityEngine.Object.Destroy(wt.GetComponent<Collider>());
            for(int d=0;d<4;d++)Bench(new Vector3(20+Mathf.Cos(d*Mathf.PI/2)*9,.24f,20+Mathf.Sin(d*Mathf.PI/2)*9),d*90);
        }
        void Fields()
        {
            var soil=Mat("soil",new Color(.42f,.28f,.18f));var crop=Mat("crops",new Color(.55f,.67f,.24f));
            Cube("Farmland",new Vector3(-118,.045f,88),new Vector3(45,.1f,52),soil);
            for(int i=0;i<12;i++)for(int j=0;j<15;j++)
                Cube("Crop",new Vector3(-138+i*3.5f,.36f,65+j*3.1f),new Vector3(.68f,.70f,2.2f),crop);
            var wall=Mat("industrial-wall",new Color(.51f,.58f,.57f));
            Cube("Packing plant",new Vector3(-105,7,43),new Vector3(22,14,19),wall,root,true);
            Cube("Warehouse",new Vector3(-80,6.5f,40),new Vector3(18,13,18),Mat("warehouse",new Color(.45f,.49f,.51f)),root,true);
            var shutters=Mat("shutters",new Color(.24f,.33f,.35f),.4f);
            Cube("Warehouse loading gate",new Vector3(-80,4f,30.8f),new Vector3(7.5f,7.8f,.24f),shutters);
            Cube("Loading ramp",new Vector3(-80,.5f,27.5f),new Vector3(11,1,6),stoneMat());
            for(int k=0;k<3;k++)Lamp(new Vector3(-122+k*16,0,24));
        }
        Material stoneMat(){return Mat("industrial-paving",new Color(.60f,.57f,.49f));}
        void AddBusiness(string id,MirPlaceKind kind,string label)
        {
            var biz=MirGame.Instance.World.Business(id);
            var e=MirWorldGeometry.BusinessEntry(id,biz);
            Place(new MirPlace{id=id,kind=kind,name=label,entry=new Vector3(e.x,.2f,e.z),business=biz});
        }
        void FuelStation()
        {
            var red=Mat("fuel-brand",new Color(.70f,.29f,.20f));var metal=Mat("fuel-metal",new Color(.74f,.74f,.68f));
            Cube("Station roof",new Vector3(-80,6,-50),new Vector3(17,.75f,14),red);
            for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)
                Cube("Canopy pillar",new Vector3(-80+i*6,3,-50+j*5),new Vector3(.45f,6,.45f),metal);
            for(int i=0;i<2;i++)Cube("Fuel pump",new Vector3(-83+i*6,1.15f,-50),new Vector3(1.05f,2.3f,.82f),red);
        }
        public void Build()
        {
            root=new GameObject("MIR Dockside · Procedural District").transform;
            AsphaltRoads();MarketSquare();Fields();FuelStation();
            BlockBuilding("Grocery store",new Vector3(20,0,-20),2,new Color(.64f,.67f,.60f),13,14,retail:true);
            BlockBuilding("Corner café",new Vector3(56,0,20),2,new Color(.67f,.59f,.47f),12,12,retail:true);
            BlockBuilding("Auto workshop",new Vector3(-58,0,-22),2,new Color(.45f,.54f,.61f),15,15);
            BlockBuilding("Town hall",new Vector3(-20,0,-20),3,new Color(.76f,.72f,.63f),14,14);
            AddBusiness("market",MirPlaceKind.Store,"Магазин");
            AddBusiness("cafe",MirPlaceKind.Cafe,"Кафе");
            AddBusiness("garage",MirPlaceKind.Garage,"Автосервис");
            AddBusiness("town",MirPlaceKind.CityHall,"Муниципалитет");
            AddBusiness("farm",MirPlaceKind.Farm,"Поля");
            AddBusiness("packing",MirPlaceKind.Packing,"Пищевой цех");
            AddBusiness("warehouse",MirPlaceKind.Warehouse,"Склад");
            AddBusiness("fuel",MirPlaceKind.Fuel,"АЗС");
            for(int i=0;i<8;i++)
            {
                int gx=i%4;int gz=i/4;
                float x=-66+gx*36;float z=56+gz*38;
                BlockBuilding("Residential block "+i,new Vector3(x,0,z),3+(i%3),
                    new Color(.55f+(i%3)*.055f,.57f,.54f),14,13,balconies:i==2);
            }
            for(int i=0;i<MirGame.Instance.World.citizens.Count;i++)
            {
                MirCitizen c=MirGame.Instance.World.citizens[i];
                var home=MirWorldGeometry.HomeEntry(MirGame.Instance.World,c);
                Place(new MirPlace{id=c.homeId,name="Квартира · "+c.name,kind=MirPlaceKind.Home,entry=new Vector3(home.x,.3f,home.z)});
            }
            for(int i=-2;i<=2;i++)for(int j=-2;j<=2;j++)
            {
                if((i+j)%2==0)Tree(new Vector3(i*40+13,.2f,j*40+13),.8f);
                if((i+j)%3==0)Lamp(new Vector3(i*40+8,0,j*40+8),i==-1&&j==1);
            }
            // Keep an explicit index: the first parked vehicle may use the optional
            // licensed art if supplied locally; public builds use the standard mesh.
            var parkingSpawns=new[]{new Vector3(11,0,-31),new Vector3(-53,0,-35),new Vector3(71,0,32)};
            for(int i=0;i<parkingSpawns.Length;i++)
                Vehicles.Add(MirVehicle.Spawn(parkingSpawns[i],false,i==0));
            // ART-ONLY P0: coherent public promenade outside the southern map edge.
            // Stays off NPC POIs/sidewalk routes; no scene or Editor pipeline edits.
            MirWaterfrontQuarter.Build(root,Mat);
        }
        public List<Vector3> SidewalkRoute(Vector3 origin, Vector3 destination)
        {
            var points=MirWorldGeometry.SidewalkRoute(origin.x,origin.z,destination.x,destination.z);
            var route=new List<Vector3>(points.Length);
            foreach(var point in points)route.Add(new Vector3(point.x,.3f,point.z));
            return route;
        }
        public void UpdateLighting(float hour)
        {
            bool night=hour<6||hour>=19;
            RenderSettings.ambientLight=night?new Color(.25f,.30f,.36f):new Color(.64f,.66f,.68f);
            RenderSettings.fog=true;RenderSettings.fogColor=night?new Color(.13f,.17f,.22f):new Color(.65f,.71f,.69f);
            RenderSettings.fogDensity=night?0.006f:.0025f;
            for(int i=0;i<lamps.Count;i++)if(lamps[i]!=null)lamps[i].intensity=night?1.65f:0;
        }
    }
    public sealed class MirFlickerLamp : MonoBehaviour
    {
        Light l;
        void Awake(){l=GetComponent<Light>();}
        void Update(){if(l!=null&&l.intensity>0)l.intensity=((Mathf.FloorToInt(Time.time*3.5f)%9)==0?.12f:1.65f);}
    }
}
