#!/usr/bin/env python3
"""Build genuine Quaternius Stylized Nature MegaKit glTF -> GLB for NONATION.

Sources are exact creator-uploaded 2025 OpenGameArt CC0 (NOT an Asset Store
source-tier pack). The archive SHA256 and all 21 selected member digests are
pinned. No images are re-synthesized; original textures/UVs/PBR preserved.
A built GLB is a genuine mesh+texture container, never a procedural primitive.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import io
import json
from pathlib import Path
import struct
import tempfile
import os
from urllib.request import Request, urlopen
import zipfile

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Tools/hero_nature_manifest.json"
OUT = ROOT / "Assets/StreamingAssets/NONATION/Nature"
OUTPUT_RECORD = OUT / "verified-bundle-manifest.json"
MODEL_NAMES = (
    "CommonTree_1", "CommonTree_3", "TwistedTree_2",
    "Bush_Common_Flowers", "Flower_3_Group", "Grass_Common_Short",
)
MAX_ARCHIVE_BYTES = 120 * 1024 * 1024


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def align4(data: bytes, byte: bytes = b"\0") -> bytes:
    return data + byte * (-len(data) % 4)


def expected_source_manifest() -> dict:
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if data["schema"] != "nonation/stylized-nature-cc0/1" or data["license"] != "CC0-1.0":
        raise ValueError("Unapproved original nature source/license")
    if data["archive_url"] != (
        "https://opengameart.org/sites/default/files/stylized_nature_megakitstandard.zip"
    ):
        raise ValueError("Unapproved nature pack origin")
    if data["archive_bytes"] != 104088529 or data["archive_sha256"] != (
        "298f6732b872e4cf7b30e6e7abf9641c7f6dc6b326df37ac089533ed7e3d58c9"
    ):
        raise ValueError("Nature archive pin changed")
    if tuple(data["model_names"]) != MODEL_NAMES:
        raise ValueError("Source model names or quality scope changed")
    keys = [part["path"] for part in data["members"]]
    if len(keys) != 21 or len(set(keys)) != len(keys):
        raise ValueError("Incomplete/duplicate nature source members")
    for item in data["members"]:
        name = item["path"]
        if not name.startswith("glTF/") or "/" in name[5:] or "\\" in name or ".." in name:
            raise ValueError("Disallowed ZIP path: " + name)
        if not 0 < item["size"] < 8 * 1024 * 1024:
            raise ValueError("Disallowed size for " + name)
        if len(item["sha256"]) != 64:
            raise ValueError("Missing asset integrity hash")
    return data


def download(data: dict) -> bytes:
    req = Request(data["archive_url"], headers={
        "User-Agent": "Mozilla/5.0 NONATION-CC0-NatureAssetVerifier/1.0",
    })
    with urlopen(req, timeout=210) as response:
        blob = response.read(MAX_ARCHIVE_BYTES + 1)
    if len(blob) != data["archive_bytes"] or digest(blob) != data["archive_sha256"]:
        raise ValueError("Original creator-uploaded CC0 nature archive SHA/size differs")
    return blob


def verified_members(archive: bytes, data: dict) -> dict[str, bytes]:
    if len(archive) != data["archive_bytes"] or digest(archive) != data["archive_sha256"]:
        raise ValueError("Source nature ZIP invalid")
    result = {}
    with zipfile.ZipFile(io.BytesIO(archive)) as z:
        for item in data["members"]:
            content = z.read(item["path"])  # also verifies ZIP CRC
            if len(content) != item["size"] or digest(content) != item["sha256"]:
                raise ValueError("Nature CC0 member SHA256 mismatch: " + item["path"])
            result[item["path"]] = content
    return result


def pack_glb(source: dict, name: str) -> bytes:
    """Flatten one *original* glTF 2.0 scene and source PNG images into GLB2.

    Vertex streams are preserved byte-for-byte. Only buffer/image URI offsets
    change. No mesh, UV, skin, node, material or texture generation occurs.
    """
    if name not in MODEL_NAMES:
        raise ValueError("Unapproved original nature model")
    gltf = copy.deepcopy(json.loads(source["glTF/" + name + ".gltf"].decode("utf8")))
    if gltf.get("asset", {}).get("version") != "2.0":
        raise ValueError("Expected original glTF 2.0")
    buffers = gltf.get("buffers", [])
    if len(buffers) != 1 or buffers[0].get("uri") != name + ".bin":
        raise ValueError("Unexpected glTF source BIN")
    raw = source["glTF/" + name + ".bin"]
    if buffers[0]["byteLength"] != len(raw):
        raise ValueError("Original glTF BIN size mismatch")
    stream = bytearray(align4(raw))
    images = gltf.get("images", [])
    if not images:
        raise ValueError("Real imported foliage requires source textures")
    for image in images:
        uri = image.pop("uri", None)
        if not isinstance(uri, str) or not uri.endswith(".png") or "/" in uri:
            raise ValueError("Non-pinned glTF image URI")
        bytes_ = source.get("glTF/" + uri)
        if not bytes_ or bytes_[:8] != b"\x89PNG\r\n\x1a\n":
            raise ValueError("Actual original CC0 foliage texture missing: " + str(uri))
        offset = len(stream)
        stream.extend(align4(bytes_))
        view = len(gltf["bufferViews"])
        gltf["bufferViews"].append({
            "buffer": 0, "byteOffset": offset, "byteLength": len(bytes_)
        })
        image["bufferView"] = view
        image["mimeType"] = "image/png"
    gltf["buffers"] = [{"byteLength": len(stream)}]
    text = align4(json.dumps(gltf, separators=(",", ":"), ensure_ascii=False).encode(),
                  b" ")
    binary = align4(bytes(stream))
    total = 12 + 8 + len(text) + 8 + len(binary)
    if total >= MAX_ARCHIVE_BYTES:
        raise ValueError("Generated GLB unexpectedly too large")
    payload = (struct.pack("<4sII", b"glTF", 2, total) +
               struct.pack("<I4s", len(text), b"JSON") + text +
               struct.pack("<I4s", len(binary), b"BIN\0") + binary)
    validate_glb(payload)
    return payload


def validate_glb(payload: bytes) -> None:
    if len(payload) < 32:
        raise ValueError("Generated GLB is empty")
    magic, version, n = struct.unpack_from("<4sII", payload, 0)
    if magic != b"glTF" or version != 2 or n != len(payload):
        raise ValueError("Invalid glTF v2 GLB header")
    jslen, tag = struct.unpack_from("<I4s", payload, 12)
    if tag != b"JSON" or jslen <= 0:
        raise ValueError("Missing GLB JSON chunk")
    js = json.loads(payload[20:20 + jslen])
    if not js.get("meshes") or not js.get("materials") or not js.get("images"):
        raise ValueError("GLB contains no actual geometry/material/textures")


def output_valid(data: dict) -> bool:
    if not OUTPUT_RECORD.is_file():
        return False
    record = json.loads(OUTPUT_RECORD.read_text(encoding="utf8"))
    if record.get("source_sha256") != data["archive_sha256"]:
        return False
    if set(record.get("outputs", {})) != set(MODEL_NAMES):
        return False
    for name in MODEL_NAMES:
        p = OUT / (name + ".glb")
        if not p.is_file():
            return False
        content = p.read_bytes()
        validate_glb(content)
        if digest(content) != record["outputs"][name]["sha256"]:
            raise ValueError("Generated nature GLB checksum mismatch: " + name)
        if len(content) != record["outputs"][name]["bytes"]:
            raise ValueError("Generated nature GLB truncated: " + name)
    return True


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()
    m = expected_source_manifest()
    if output_valid(m):
        print("NONATION_PREMIUM_NATURE_ASSETS_OK models=6 cached=1")
        return
    if args.verify_only:
        raise RuntimeError("Real CC0 premium foliage GLBs unavailable; do not substitute cubes")
    originals = verified_members(download(m), m)
    converted = {name: pack_glb(originals, name) for name in MODEL_NAMES}
    OUT.mkdir(parents=True, exist_ok=True)
    # Stage all validated GLBs before publishing any named output.
    staged = []
    try:
        for name, binary in converted.items():
            fd, temp = tempfile.mkstemp(dir=str(OUT), prefix=".nonation-", suffix=".part")
            with os.fdopen(fd, "wb") as stream:
                stream.write(binary)
            staged.append((Path(temp), OUT / (name + ".glb")))
        for temp, destination in staged:
            os.replace(temp, destination)
    finally:
        for temp, _ in staged:
            temp.unlink(missing_ok=True)
    result = {
        "source_sha256": m["archive_sha256"],
        "license": "CC0-1.0",
        "outputs": {
            name: {"bytes": len(binary), "sha256": digest(binary)}
            for name, binary in converted.items()
        }
    }
    OUTPUT_RECORD.write_text(json.dumps(result, sort_keys=True, indent=2) + "\n",
                             encoding="utf8")
    if not output_valid(m):
        raise RuntimeError("Incomplete premium CC0 nature installation")
    print("NONATION_PREMIUM_NATURE_ASSETS_OK models=6 cached=0 " +
          "originalArchiveSha256=" + m["archive_sha256"])


if __name__ == "__main__":
    main()
