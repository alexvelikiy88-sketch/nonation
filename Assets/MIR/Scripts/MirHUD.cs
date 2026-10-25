using UnityEngine;

namespace MIR
{
    /// <summary>
    /// A small readable playable UI. The full economic/action panel remains
    /// accessible with H, while the actual 3D frame stays unobstructed.
    /// Showroom view (V) hides UI, not simulation, for truthful comparison.
    /// Latin labels avoid the missing Cyrillic fallback in Unity WebGL IMGUI;
    /// proper localized fonts and final UGUI remain a dedicated visual task.
    /// </summary>
    public sealed class MirHUD : MonoBehaviour
    {
        MirGame game;
        bool fullPanel;
        GUIStyle panel, title, caption, body, button, heading;
        Texture2D panelTexture, buttonTexture, hoverTexture;

        public void Init(MirGame source) { game = source; }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                fullPanel = !fullPanel;
        }

        bool IsShowcase()
        {
            var camera = Camera.main;
            if (camera == null) return false;
            var driver = camera.GetComponent<MirFollowCamera>();
            return driver != null &&
                driver.CurrentView == MirFollowCamera.View.DistrictShowcase;
        }

        static Texture2D ColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }

        void EnsureStyles()
        {
            if (panel != null) return;
            panelTexture = ColorTexture(new Color(.075f, .125f, .15f, .83f));
            buttonTexture = ColorTexture(new Color(.17f, .28f, .32f, .97f));
            hoverTexture = ColorTexture(new Color(.24f, .37f, .41f, .99f));

            panel = new GUIStyle(GUI.skin.box);
            panel.normal.background = panelTexture;
            panel.padding = new RectOffset(10, 10, 10, 10);

            title = new GUIStyle(GUI.skin.label);
            title.fontSize = 20;
            title.fontStyle = FontStyle.Bold;
            title.normal.textColor = new Color(.96f, .96f, .91f);

            caption = new GUIStyle(GUI.skin.label);
            caption.fontSize = 11;
            caption.normal.textColor = new Color(.79f, .84f, .83f);

            heading = new GUIStyle(title);
            heading.fontSize = 14;

            body = new GUIStyle(caption);
            body.fontSize = 13;
            body.wordWrap = true;
            body.normal.textColor = new Color(.92f, .95f, .94f);

            button = new GUIStyle(GUI.skin.button);
            button.fontSize = 12;
            button.fontStyle = FontStyle.Normal;
            button.fixedHeight = 34f;
            button.alignment = TextAnchor.MiddleCenter;
            button.normal.background = buttonTexture;
            button.hover.background = hoverTexture;
            button.active.background = hoverTexture;
            button.normal.textColor = Color.white;
            button.hover.textColor = Color.white;
        }

        void OnGUI()
        {
            if (game == null || game.World == null || game.Player == null)
                return;

            // This is still the real 3D camera, not a composited concept
            // screenshot. Suppress overlay panels only for the scenic camera.
            if (IsShowcase()) return;

            EnsureStyles();
            float width = Mathf.Min(310f, Screen.width - 24f);
            GUI.Box(new Rect(12, 12, width, 70), GUIContent.none, panel);
            GUI.Label(new Rect(24, 17, width - 120, 29), "NONATION", title);
            string clock = ((int)game.World.Hour).ToString("00") + ":" +
                (((int)game.World.gameMinutes) % 60).ToString("00");
            GUI.Label(new Rect(24, 48, width - 115, 22),
                "DAY " + game.World.Day.ToString("00") + " | " + clock +
                " | PEOPLE " + game.World.citizens.Count, caption);
            if (GUI.Button(new Rect(12 + width - 82, 30, 72, 31),
                fullPanel ? "CLOSE" : "MENU", button))
                fullPanel = !fullPanel;

            if (!fullPanel) return;

            float available = Mathf.Max(220f, Screen.height - 106f);
            float height = Mathf.Min(470f, available);
            GUI.Box(new Rect(12, 90, width, height), GUIContent.none, panel);
            GUILayout.BeginArea(new Rect(24, 102, width - 24, height - 20));
            GUILayout.Label(game.RemoteSpectator ? "SPECTATOR MODE" : "CITY STATUS", heading);

            if (game.RemoteSpectator)
            {
                GUILayout.Label("Read-only live server world.", body);
                GUILayout.Label("Economy, saving and vehicle actions are disabled.", caption);
            }
            else
            {
                var resident = game.Player.Resident;
                if (resident != null)
                {
                    GUILayout.Label("Balance  " +
                        (resident.cash / 100f).ToString("0.00") + " credits", body);
                    GUILayout.Label("Food  " +
                        Mathf.RoundToInt(resident.satiety) + "/100   Energy  " +
                        Mathf.RoundToInt(resident.energy) + "/100", caption);
                    GUILayout.Label("Supplies  " + resident.inventoryFood, caption);
                }
                var place = game.City.Nearest(game.Player.transform.position, 8f);
                GUILayout.Label("Near  " + (place == null ? "Street" : place.id), caption);
                GUILayout.Space(10);
                if (GUILayout.Button("E   Interact", button)) game.Player.Interact();
                if (GUILayout.Button("F   Enter / exit vehicle", button))
                    game.Player.ToggleVehicle();
                if (resident != null && GUILayout.Button("R   Eat food", button))
                    game.Economy.Eat(resident);
                if (GUILayout.Button("Order freight", button))
                    game.Player.OrderFreight();
                if (GUILayout.Button(game.Paused ? "Resume simulation" : "Pause simulation", button))
                    game.Paused = !game.Paused;
                if (GUILayout.Button("Save world", button))
                {
                    game.Save();
                    game.Notify("World saved.");
                }
            }
            GUILayout.Space(8);
            GUILayout.Label("CONTROLS", heading);
            GUILayout.Label("WASD move   Shift run   Tab camera", caption);
            GUILayout.Label("V district view   Mouse look / zoom", caption);
            GUILayout.Label("H close this panel", caption);
            GUILayout.EndArea();
        }

        void OnDestroy()
        {
            if (panelTexture != null) Destroy(panelTexture);
            if (buttonTexture != null) Destroy(buttonTexture);
            if (hoverTexture != null) Destroy(hoverTexture);
        }
    }
}
