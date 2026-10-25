"""UI regression for the real Unity/HUD, not a rendered quality assertion."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
HUD = ROOT / "Assets/MIR/Scripts/MirHUD.cs"


class CompactHudTests(unittest.TestCase):
    def test_default_compact_and_hide_overlay_in_real_scenic_camera(self):
        content = HUD.read_text(encoding="utf-8")
        self.assertIn("bool fullPanel;", content)
        self.assertNotIn("bool fullPanel = true", content)
        self.assertIn("KeyCode.H", content)
        self.assertIn("if (IsShowcase()) return;", content)
        self.assertIn("MirFollowCamera.View.DistrictShowcase", content)
        self.assertIn("MirFollowCamera.View.WaterfrontShowcase", content)
        self.assertIn("NONATION", content)
        self.assertIn('fullPanel ? "CLOSE" : "MENU"', content)
        self.assertNotIn('GUI.Box(new Rect(Screen.width-rw', content)

    def test_all_gameplay_controls_are_still_available_with_panel_open(self):
        content = HUD.read_text(encoding="utf-8")
        for action in ("game.Player.Interact()", "game.Player.ToggleVehicle()",
                       "game.Economy.Eat(resident)", "game.Player.OrderFreight()",
                       "game.Paused = !game.Paused", "game.Save()"):
            self.assertIn(action, content)
        self.assertIn("game.RemoteSpectator", content)
        self.assertIn("game.City.Nearest", content)

    def test_webgl_latin_ui_does_not_depend_on_unbundled_cyrillic_fonts(self):
        content = HUD.read_text(encoding="utf-8")
        self.assertFalse(re.search("[\\u0400-\\u052f]", content))
        self.assertIn("ColorTexture", content)
        self.assertIn("B waterfront", content)
        self.assertIn("OnDestroy()", content)


if __name__ == "__main__":
    unittest.main()
