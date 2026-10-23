"""Independent Unity client ↔ .NET spectator API source-contract checks.

They detect integration drift without touching agent-owned modules. This is a
static contract check, NOT a Unity C# compile, IL2CPP build, or browser test.
"""
import json
import re
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def source(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


class SpectatorContractTests(unittest.TestCase):
    def test_shared_dto_is_used_by_server_and_backend_tests(self):
        canonical = (ROOT / "Assets/MIR/Scripts/MirSceneSnapshot.cs").resolve()
        for project in ("Server/Nonation.Server/Nonation.Server.csproj",
                        "Server/Nonation.Tests/Nonation.Tests.csproj"):
            file = ROOT / project
            xml = ET.parse(file)
            referenced = [
                (file.parent / x.attrib["Include"]).resolve()
                for x in xml.findall(".//Compile")
                if x.attrib.get("Include", "").endswith("MirSceneSnapshot.cs")
            ]
            self.assertEqual(referenced, [canonical],
                             f"{project} must link the same shared spectator DTO")

    def test_public_route_and_unity_client_endpoint_match(self):
        server = source("Server/Nonation.Server/Program.cs")
        client = source("Assets/MIR/Scripts/MirRemoteWorldSync.cs")
        route = re.search(r'app\.MapGet\(\s*"(/api/v1/spectator/[^"]+)"', server)
        self.assertIsNotNone(route, "server lost the public spectator GET route")
        self.assertIn('EndsWith("' + route.group(1) + '"', client,
                      "Unity validates a different endpoint from the server")
        self.assertIn('.RequireCors("SpectatorOnly")', server)
        self.assertIn("app.UseCors();", server)

    def test_unity_client_defaults_to_offline_and_https(self):
        config = json.loads(source("Assets/MIR/Resources/MIR/NetworkConfig.json"))
        self.assertEqual(config.get("snapshotUrl"), "",
                         "public WebGL must not hardcode a remote backend")
        client = source("Assets/MIR/Scripts/MirRemoteWorldSync.cs")
        self.assertIn("Uri.UriSchemeHttps", client)
        self.assertIn("Application.isEditor", client)
        self.assertIn("Uri.UriSchemeHttp", client)
        self.assertIn("url.UserInfo", client)
        self.assertIn("UnityWebRequest.Get(endpoint)", client)
        self.assertNotIn("UnityWebRequest.Post(", client)
        self.assertNotIn("UnityWebRequest.Put(", client)

    def test_transport_and_backend_use_field_compatible_snapshot(self):
        dto = source("Assets/MIR/Scripts/MirSceneSnapshot.cs")
        client = source("Assets/MIR/Scripts/MirRemoteWorldSync.cs")
        server = source("Server/Nonation.Server/Program.cs")
        self.assertIn("JsonUtility.FromJson<MirSceneSnapshot>", client)
        self.assertIn("IncludeFields = true", server,
                      ".NET System.Text.Json must serialize Unity-compatible fields")
        for name in ("version", "serverId", "gameMinutes", "citizens",
                     "id", "x", "z"):
            self.assertRegex(dto, r"\bpublic\s+[\w.\[\]]+\s+" + name + r"\b",
                             f"snapshot DTO lost {name}")
        # Both v1's direct validation and v2's shared cursor are valid during migration.
        self.assertTrue("snapshot.version" in client or
                        "MirSceneSnapshotCursor" in client,
                        "client lost public snapshot validation")

    def test_readonly_endpoint_and_no_money_in_public_dto(self):
        server = source("Server/Nonation.Server/Program.cs")
        dto = source("Assets/MIR/Scripts/MirSceneSnapshot.cs")
        for verb in ("Post", "Put", "Patch", "Delete"):
            self.assertNotRegex(
                server, r'Map' + verb + r'\(\s*"/api/v1/spectator/scene"',
                "spectator endpoint must remain read-only")
        self.assertIn('NONATION_ENABLE_DEBUG_WORLD', server,
                      "debug world must remain an explicit opt-in")
        for private in ("cash", "balance", "transactions", "password",
                        "stock", "inventoryFood", "cityTreasury"):
            self.assertNotRegex(dto,
                                r"\bpublic\s+[\w.\[\]]+\s+" + private + r"\s*[=;]",
                                "private economic data exposed through public DTO")


if __name__ == "__main__":
    unittest.main()
