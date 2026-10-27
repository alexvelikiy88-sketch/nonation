"""Source-level assertions for the first genuinely textured tree/flower WebGL pass.

Does not certify rendered appearance: only the completed Unity Cloud Build and
unretouched Chromium screenshots may establish the real visual result.
"""
import importlib.util
import json
from pathlib import Path
import struct
import unittest

ROOT=Path(__file__).resolve().parents[2]
MANIFEST=ROOT/"Tools/hero_nature_manifest.json"
PACKER=ROOT/"Tools/fetch_hero_nature.py"
UNITY=ROOT/"Assets/MIR/Art/PremiumHero/MirPremiumNature.cs"
CLOUD=ROOT/"Tools/unity_cloud_prebuild.sh"
WORKFLOW=ROOT/".github/workflows/unity-webgl.yml"
CITY=ROOT/"Assets/MIR/Scripts/MirCity.cs"
CAMERA=ROOT/"Assets/MIR/Scripts/MirFollowCamera.cs"
LIGHT=ROOT/"Assets/MIR/Scripts/MirGame.cs"
BROWSER=ROOT/"Tools/browser_smoke_preview.js"


class PremiumNatureArtTest(unittest.TestCase):
    def test_creator_licensed_original_zip_and_all_source_hashes_pinned(self):
        m=json.loads(MANIFEST.read_text(encoding="utf8"))
        self.assertEqual(m["license"],"CC0-1.0")
        self.assertEqual(m["source_page"],
            "https://opengameart.org/content/stylized-nature-megakit")
        self.assertEqual(m["archive_url"],
            "https://opengameart.org/sites/default/files/stylized_nature_megakitstandard.zip")
        self.assertEqual(m["archive_sha256"],
            "298f6732b872e4cf7b30e6e7abf9641c7f6dc6b326df37ac089533ed7e3d58c9")
        self.assertEqual(m["archive_bytes"],104088529)
        self.assertEqual(len(m["members"]),21)
        self.assertEqual(len(set(x["path"] for x in m["members"])),21)
        for item in m["members"]:
            self.assertEqual(len(item["sha256"]),64)
            self.assertTrue(item["path"].startswith("glTF/"))
            self.assertNotIn("..",item["path"])
        self.assertEqual(m["model_names"],[
            "CommonTree_1","CommonTree_3","TwistedTree_2",
            "Bush_Common_Flowers","Flower_3_Group","Grass_Common_Short"
        ])

    def test_real_original_mesh_and_uv_conversion_correct_glb_layout(self):
        spec=importlib.util.spec_from_file_location("premium_nature",PACKER)
        assert spec is not None and spec.loader is not None
        module=importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        source={
            "glTF/CommonTree_1.gltf":json.dumps({
                "asset":{"version":"2.0"},
                "buffers":[{"uri":"CommonTree_1.bin","byteLength":4}],
                "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":4}],
                "meshes":[{"primitives":[{"attributes":{"POSITION":0}}]}],
                "materials":[{"name":"Original CC0 tree"}],
                "images":[{"uri":"Leaves_NormalTree_C.png"}],
                "textures":[{"source":0}],
                "nodes":[{"mesh":0}],
            }).encode("utf8"),
            "glTF/CommonTree_1.bin":b"ABCD",
            "glTF/Leaves_NormalTree_C.png":b"\x89PNG\r\n\x1a\n"+"Leaves".encode(),
        }
        glb=module.pack_glb(source,"CommonTree_1")
        module.validate_glb(glb)
        self.assertEqual(glb[:4],b"glTF")
        self.assertEqual(struct.unpack_from("<I",glb,8)[0],len(glb))
        json_size=struct.unpack_from("<I",glb,12)[0]
        d=json.loads(glb[20:20+json_size])
        self.assertEqual(d["buffers"][0]["uri"] if "uri" in d["buffers"][0] else None,None)
        self.assertEqual(d["images"][0]["mimeType"],"image/png")
        self.assertIn("bufferView",d["images"][0])
        self.assertEqual(d["meshes"][0]["primitives"][0]["attributes"]["POSITION"],0)
        self.assertEqual(d["bufferViews"][0]["byteOffset"],0)
        self.assertEqual(d["bufferViews"][0]["byteLength"],4)
        self.assertEqual(d["bufferViews"][1]["byteOffset"],4)
        self.assertEqual(d["bufferViews"][1]["byteLength"],14)
        with self.assertRaises(ValueError):
            module.pack_glb({"glTF/CommonTree_1.gltf":b'{"asset":{"version":"1.0"}}'},
                "CommonTree_1")

    def test_prefetch_and_unity_runtime_preserve_author_geometry(self):
        script=PACKER.read_text(encoding="utf8")
        code=UNITY.read_text(encoding="utf8")
        for needle in ("download(m)", "verified_members(download(m), m)",
                       "pack_glb(originals, name)", "validate_glb(payload)",
                       "bin", "archive_sha256", "no primitive"):
            if needle=="no primitive":
                self.assertNotIn("GameObject.CreatePrimitive",code)
            elif needle=="bin":
                self.assertIn('struct.pack("<I4s", len(binary), b"BIN\\0")',script)
            else:
                self.assertIn(needle,script)
        for token in ("GltfImport(","InstantiateMainSceneAsync(",
            "Application.streamingAssetsPath","BuiltInMaterialGenerator()",
            "source=Quaternius-2025-CC0",
            "NONATION_PREMIUM_NATURE_READY models=6 trees=7 bushes=12",
            "MirPremiumNature"):
            self.assertIn(token,code)
        self.assertIn("nature.AddComponent<MirPremiumNature>()",CITY.read_text(encoding="utf8"))
        for txt in (CLOUD.read_text(encoding="utf8"),
                    WORKFLOW.read_text(encoding="utf8")):
            self.assertIn("python3 Tools/fetch_hero_nature.py",txt)
            self.assertIn("python3 Tools/fetch_hero_nature.py --verify-only",txt)

    def test_reproducible_close_frame_and_real_daylight(self):
        camera=CAMERA.read_text(encoding="utf8")
        light=LIGHT.read_text(encoding="utf8")
        self.assertIn("ShowcaseFocus=new Vector3(30f,4f,3f)",camera)
        self.assertIn("ShowcaseRotation=Quaternion.Euler(40f,52f,0f)",camera)
        self.assertIn("ShowcaseDistance=66f",camera)
        self.assertIn("sun.shadows=LightShadows.Soft",light)
        for key in ("QualitySettings.antiAliasing=4",
                    "QualitySettings.shadowDistance=85f",
                    "sun.shadowStrength=.84f",
                    "NONATION_PREMIUM_LIGHT_READY"):
            self.assertIn(key,light)

    def test_browser_must_prove_real_asset_and_raw_shots_after_reload(self):
        s=BROWSER.read_text(encoding="utf8")
        self.assertIn("meta.premiumNature = nature",s)
        self.assertIn("meta.premiumNatureReloaded = nature",s)
        self.assertIn("NONATION_PREMIUM_NATURE_VERIFIED phase=initial",s)
        self.assertIn("NONATION_PREMIUM_NATURE_VERIFIED phase=reload",s)
        self.assertIn("NONATION_PREMIUM_NATURE_ERROR",s)
        self.assertIn("nonation-browser-showcase.png",s)
        self.assertIn("nonation-browser-waterfront.png",s)


if __name__=="__main__":
    unittest.main()
