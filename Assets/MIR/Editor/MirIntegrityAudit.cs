#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MIR.Editor
{
    public static class MirIntegrityAudit
    {
        [MenuItem("MIR/03 — Audit Model and Economy")]
        public static void Execute()
        {
            List<string> lines=new List<string>();int failed=0;
            void Check(bool ok,string name)
            {
                lines.Add((ok?"PASS ":"FAIL ")+name);
                if(!ok)failed++;
            }
            string basePath="Assets/MIR/Art/Quaternius/";
            string[] sourceFiles={"UAL1_Standard_Unity.fbx","UAL2_Standard_Unity.fbx","Mannequin_F.fbx"};
            foreach(string file in sourceFiles)Check(File.Exists(basePath+file),"source imported: "+file);
            var lib=AssetDatabase.LoadAssetAtPath<MirAnimationCatalog>("Assets/MIR/Resources/MIR/QuaterniusCatalog.asset");
            Check(lib!=null,"motion catalog created");
            if(lib!=null)
            {
                Check(lib.entries.Count>=70,"motion catalog contains 70+ usable clips");
                Check(lib.entries.Exists(x=>x.name.Contains("Walk")),"walking clips");
                Check(lib.entries.Exists(x=>x.name.Contains("Driving")),"driving clip");
                Check(lib.entries.Exists(x=>x.name.Contains("Farm_Harvest")),"farming clip");
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MIR/Resources/MIR/PlayableHuman.prefab");
            var anim=prefab!=null?prefab.GetComponentInChildren<Animator>():null;
            Check(anim!=null&&anim.avatar!=null&&anim.avatar.isValid&&anim.avatar.isHuman,"playable Humanoid Avatar valid");
            var world=MirWorldState.NewWorld();
            var econ=new MirEconomy(world);
            // Domain money totals widened to long: keep Editor audit in sync.
            long before=econ.TotalKnownMoney();
            var player=world.Citizen("player-001");var store=world.Business("market");
            Check(player!=null && store!=null,"world citizens + business created");
            int old=store.stock;
            Check(econ.BuyFood(player,store),"purchase transfers real stock and money");
            Check(store.stock==old-1&&player.inventoryFood==1,"stock and bag updated");
            Check(econ.Eat(player)&&player.inventoryFood==0,"food consumed once");
            Check(econ.Work(player),"funded work completion");
            Check(!econ.Work(player),"work cooldown blocks repeated free salary");
            var freight=econ.CreateRestock();
            Check(freight!=null,"freight reservation of product+escrow");
            if(freight!=null)
            {
                Check(econ.SetFreightStage(freight,MirOrderStatus.Loading),"truck stage loading");
                Check(econ.SetFreightStage(freight,MirOrderStatus.Driving),"truck stage driving");
                Check(econ.SetFreightStage(freight,MirOrderStatus.Unloading),"physical delivery transfers goods+money");
                Check(!econ.SetFreightStage(freight,MirOrderStatus.Unloading),"double unload refused");
                Check(econ.SetFreightStage(freight,MirOrderStatus.Returning),"truck return");
                Check(econ.SetFreightStage(freight,MirOrderStatus.Completed),"completed freight");
            }
            Check(econ.TotalKnownMoney()==before,"total known money + escrow conserved");
            var blob=MirPersistence.Export(world);MirWorldState restored;
            Check(MirPersistence.Import(blob,out restored) && restored.citizens.Count==world.citizens.Count,"world serialized and restored");
            string path="BuildReports/MIR_Unity_Audit.txt";
            Directory.CreateDirectory("BuildReports");
            File.WriteAllLines(path,lines);
            Debug.Log("MIR audit completed: "+(lines.Count-failed)+"/"+lines.Count+" passed. Report: "+path);
            if(failed>0)Debug.LogWarning("MIR Audit failed " + failed+" tests. Check report before shipping.");
        }
    }
}
#endif
