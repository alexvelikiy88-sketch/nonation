using UnityEngine;

namespace MIR
{
    public sealed class MirHUD : MonoBehaviour
    {
        MirGame game;
        GUIStyle title,body,small,button,box,eventStyle;
        Texture2D background;
        public void Init(MirGame g){game=g;}
        void MakeStyles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=22,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.95f,.95f,.87f);
            body=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};body.normal.textColor=new Color(.87f,.90f,.86f);
            small=new GUIStyle(body){fontSize=12};small.normal.textColor=new Color(.70f,.77f,.75f);
            eventStyle=new GUIStyle(body){fontSize=12,wordWrap=true};eventStyle.normal.textColor=new Color(.88f,.78f,.56f);
            button=new GUIStyle(GUI.skin.button){fontSize=13,fontStyle=FontStyle.Bold,fixedHeight=34};
            box=new GUIStyle(GUI.skin.box){padding=new RectOffset(13,13,13,13)};
            background=new Texture2D(1,1);background.SetPixel(0,0,new Color(.08f,.15f,.17f,.91f));background.Apply();
            box.normal.background=background;
        }
        void OnGUI()
        {
            if(game==null||game.World==null)return;
            MakeStyles();float w=Mathf.Min(Screen.width-24f,330f);
            var left=new Rect(12,12,w,Screen.height-24);
            GUI.Box(new Rect(left.x,left.y,w,390),GUIContent.none,box);
            GUILayout.BeginArea(new Rect(left.x+13,left.y+10,w-26,375));
            GUILayout.Label("NONATION",title);
            GUILayout.Label(game.RemoteSpectator?"SERVER · READ-ONLY":"UNITY 6 · OFFLINE PLAYABLE DISTRICT",small);
            GUILayout.Space(7);
            var resident=game.Player.Resident;
            GUILayout.Label("День "+game.World.Day+"  •  "+(int)game.World.Hour+":"+(((int)game.World.gameMinutes)%60).ToString("00")+"  •  жителей "+game.World.citizens.Count,body);
            if(game.RemoteSpectator)
            {
                GUILayout.Label("Просмотр серверных NPC и игрового времени.",body);
                GUILayout.Label(game.RemoteStatus,small);
                GUILayout.Label("Движение камеры и персонажа доступно. Покупки, транспорт и сохранение отключены.",small);
            }
            else
            {
                GUILayout.Label("Баланс: "+(resident.cash/100f).ToString("0.00")+" кредитов",body);
                GUILayout.Label("Еда: "+Mathf.RoundToInt(resident.satiety)+" / 100  •  энергия: "+Mathf.RoundToInt(resident.energy)+" / 100",small);
                GUILayout.Label("Инвентарь: "+resident.inventoryFood+" продукта(ов)",small);
                var place=game.City.Nearest(game.Player.transform.position,8f);
                GUILayout.Label("Рядом: "+(place!=null?place.name:"улица"),small);
                GUILayout.Space(8);
                if(GUILayout.Button("E  ·  взаимодействовать",button))game.Player.Interact();
                if(GUILayout.Button("F  ·  сесть / выйти из машины",button))game.Player.ToggleVehicle();
                if(GUILayout.Button("R  ·  поесть",button))game.Economy.Eat(resident);
                if(GUILayout.Button("Заказать поставку на рынок",button))game.Player.OrderFreight();
                if(GUILayout.Button(game.Paused?"Продолжить симуляцию":"Пауза",button))game.Paused=!game.Paused;
                if(GUILayout.Button("Сохранить мир",button)){game.Save();game.Notify("Сохранение записано.");}
            }
            GUILayout.EndArea();
            float h=140;
            GUI.Box(new Rect(12,Screen.height-h-12,w,h),GUIContent.none,box);
            GUILayout.BeginArea(new Rect(25,Screen.height-h,w-26,h-14));
            GUILayout.Label("УПРАВЛЕНИЕ",small);
            GUILayout.Label("WASD — идти / вести авто   Shift — бег",small);
            GUILayout.Label("Tab — камера   ПКМ — вращать   Колесо — масштаб",small);
            GUILayout.Label("E — действовать   F — авто   R — поесть",small);
            GUILayout.Label("Смена реального времени не зависит от камеры.",small);
            GUILayout.EndArea();
            float rw=Mathf.Min(Screen.width-24,325f);
            if(Screen.width>700)
            {
                GUI.Box(new Rect(Screen.width-rw-12,12,rw,230),GUIContent.none,box);
                GUILayout.BeginArea(new Rect(Screen.width-rw,rw>320?24:20,rw-26,206));
                GUILayout.Label("СОБЫТИЯ МИРА",body);
                var events=game.World.events;
                for(int i=events.Count-1;i>=Mathf.Max(0,events.Count-6);i--)
                    GUILayout.Label("• "+events[i].text,eventStyle);
                GUILayout.EndArea();
            }
        }
        void OnDestroy(){if(background!=null)Destroy(background);}
    }
}
