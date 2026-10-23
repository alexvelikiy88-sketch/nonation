using UnityEngine;
using System.Collections.Generic;

namespace MIR
{
    public sealed class MirGame : MonoBehaviour
    {
        public static MirGame Instance {get;private set;}
        public MirWorldState World {get;private set;}
        public MirEconomy Economy {get;private set;}
        public MirCity City {get;private set;}
        public MirPlayer Player {get;private set;}
        public MirFreightDriver Freight {get;private set;}
        public readonly List<MirNpcAgent> NPCs = new List<MirNpcAgent>();
        public bool Paused;
        float saveClock, simulationBuffer;
        int lastMinute=-1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoStart()
        {
            if(FindFirstObjectByType<MirGame>()==null)
                new GameObject("MIR — Persistent World").AddComponent<MirGame>();
        }
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
        }
        void Start()
        {
            Application.targetFrameRate=60;
            World=MirPersistence.Load();Economy=new MirEconomy(World);
            City=new MirCity();City.Build();
            var hero=World.citizens.Find(c=>c.player)??World.citizens[0];
            GameObject playerRoot=new GameObject("Player · "+hero.name);
            playerRoot.transform.position=new Vector3(hero.x,1.1f,hero.z);
            Player=playerRoot.AddComponent<MirPlayer>();Player.Init(hero);
            for(int i=0;i<World.citizens.Count;i++)
            {
                if(World.citizens[i]==hero)continue;
                var r=World.citizens[i];
                GameObject go=new GameObject("NPC · "+r.name);
                go.transform.position=new Vector3(r.x,0.4f,r.z);
                var npc=go.AddComponent<MirNpcAgent>();npc.Init(r,City);NPCs.Add(npc);
            }
            var camObj=new GameObject("MIR Camera");var camera=camObj.AddComponent<Camera>();camera.fieldOfView=58f;
            camObj.AddComponent<AudioListener>();camObj.AddComponent<MirFollowCamera>().Init(Player.transform);
            var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(42f,-38f,0f);sun.intensity=1.25f;
            var ui=new GameObject("MIR · HUD").AddComponent<MirHUD>();ui.Init(this);
            Freight=new GameObject("Supply Truck Controller").AddComponent<MirFreightDriver>();Freight.Init(this);
            Economy.Log("Открыт управляемый район MIR Dockside (Unity).");
        }
        void Update()
        {
            if(World==null||Paused)return;
            // Simulate at 0.5 in-game minute per second, independent from camera/player selection.
            float delta=Mathf.Min(Time.deltaTime,0.2f);
            World.gameMinutes+=delta*0.5f;
            simulationBuffer+=delta*0.5f;
            if(simulationBuffer>=0.1f){Economy.Tick(simulationBuffer);simulationBuffer=0;}
            int minute=Mathf.FloorToInt(World.gameMinutes);
            if(minute!=lastMinute){lastMinute=minute; City.UpdateLighting(World.Hour);}
            saveClock+=Time.deltaTime;
            if(saveClock>=30f){saveClock=0;Save();}
        }
        public void Save(){if(World==null)return; if(Player!=null)Player.WritePosition(); foreach(var a in NPCs) if(a!=null)a.WritePosition();MirPersistence.Save(World);}
        void OnApplicationPause(bool value){if(value)Save();}
        void OnApplicationQuit(){Save();}
        public void Notify(string text){Economy.Log(text);}
    }
}
