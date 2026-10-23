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
        public bool RemoteSpectator {get;private set;}
        public string RemoteStatus {get;internal set;} = "Оффлайн";
        float saveClock;
        MirSimulationLoop simulation;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
            simulation=new MirSimulationLoop(World,Economy);
            City=new MirCity();City.Build();City.UpdateLighting(World.Hour);
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
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.67f,.73f,.77f);
            camObj.AddComponent<AudioListener>();camObj.AddComponent<MirFollowCamera>().Init(Player.transform);
            var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(42f,-38f,0f);sun.intensity=1.25f;
            var ui=new GameObject("MIR · HUD").AddComponent<MirHUD>();ui.Init(this);
            Freight=new GameObject("Supply Truck Controller").AddComponent<MirFreightDriver>();Freight.Init(this);
            Economy.Log("Открыт управляемый район nonation (Unity).");
            MirRemoteWorldSync.TryAttach(this);
        }
        void Update()
        {
            if(World==null||Paused||RemoteSpectator)return;
            if(simulation.Advance(Time.deltaTime)) City.UpdateLighting(World.Hour);
            saveClock+=Time.deltaTime;
            if(saveClock>=30f){saveClock=0;Save();}
        }
        public void ActivateRemoteSpectator()
        {
            if(RemoteSpectator)return;
            if(Player != null && Player.Driving != null)Player.ExitVehicle();
            RemoteSpectator = true;
            Paused = false;
            foreach(var npc in NPCs)if(npc!=null)npc.SetRemoteControl();
            Debug.Log("nonation: read-only authoritative NPC spectator mode enabled.");
        }
        public void Save()
        {
            // Remote snapshots must never overwrite the player's local save.
            if(World==null||RemoteSpectator)return;
            if(Player!=null)Player.WritePosition();
            foreach(var a in NPCs)if(a!=null)a.WritePosition();
            MirPersistence.Save(World);
        }
        void OnApplicationPause(bool value){if(value)Save();}
        void OnApplicationQuit(){Save();}
        public void Notify(string text){Economy.Log(text);}
    }
}
