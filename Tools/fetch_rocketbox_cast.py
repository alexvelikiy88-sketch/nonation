#!/usr/bin/env python3
"""Fetch two distinct MIT-licensed REAL Microsoft Rocketbox skinned adults.

Every source FBX/TGA is pinned by the Git blob SHA1 at an immutable upstream
commit. Source textures are reduced to 1024x1024 by our previously tested pure
Python box-average TGA pipeline; normals are renormalized. Never ship models
without a verified original Microsoft MIT notice.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Tools/rocketbox_cast_manifest.json"
CAST_ROOT = ROOT / "Assets/MIR/Art/Rocketbox/Cast"
SOURCE_TOOLS = ROOT / "Assets/MIR/Art/Rocketbox"
sys.path.insert(0, str(SOURCE_TOOLS))
from fetch_textures import resize_tga_box, git_blob_hash, parse_tga_header  # noqa: E402


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def verify_manifest() -> dict:
    m = json.loads(MANIFEST.read_text(encoding="utf8"))
    if m.get("schema") != "nonation/microsoft-rocketbox-cast/1":
        raise ValueError("Unknown Microsoft MIT cast manifest")
    if m.get("source_repo") != "microsoft/Microsoft-Rocketbox":
        raise ValueError("Non-original avatar repository")
    if m.get("source_sha") != "0943055db6ec570bcef9f2c8b41c9e5467c808f9":
        raise ValueError("Unpinned Microsoft source")
    if m.get("license") != "MIT" or m.get("max_texture_edge") != 1024:
        raise ValueError("Microsoft MIT and approved 1024 atlas required")
    allowed={"Male_Adult_16", "Female_Adult_16"}
    if {c["id"] for c in m["characters"]} != allowed:
        raise ValueError("Expected two nonidentical Rocketbox characters")
    if len(m["files"]) != 11:
        raise ValueError("Expected two real FBX and nine verified TGAs")
    names=set()
    for f in m["files"]:
        if f["model"] not in allowed or f["kind"] not in ("fbx","tga"):
            raise ValueError("Unexpected Microsoft asset type")
        p=ROOT / f["output_path"]
        if p.parent.resolve() != (CAST_ROOT / f["model"]).resolve():
            raise ValueError("Invalid cast output path")
        if str(f["source_path"]).startswith("Assets/Avatars/Adults/"+f["model"]+"/") is False:
            raise ValueError("Source path is outside selected MIT avatar")
        if len(f["sha1"]) != 40 or f["source_size"] <= 0:
            raise ValueError("Unverified original Microsoft source")
        if f["output_path"] in names:
            raise ValueError("Duplicate target file")
        names.add(f["output_path"])
    license_path=ROOT / m["source_license_in_game"]
    if not license_path.is_file() or git_blob_hash(license_path.read_bytes()) != m["original_license_blob"]:
        raise ValueError("Exact original Microsoft MIT copyright notice missing")
    return m


def cache_valid(m: dict) -> bool:
    record=CAST_ROOT / "verified-cast-assets.json"
    if not record.is_file():
        return False
    report=json.loads(record.read_text(encoding="utf8"))
    if report.get("upstream") != m["source_sha"] or len(report.get("assets", {})) != 11:
        return False
    for f in m["files"]:
        path=ROOT / f["output_path"]
        if not path.is_file():
            return False
        info=report["assets"].get(f["output_path"])
        if not info or info["sha256"] != sha256(path.read_bytes()) or info["size"] != path.stat().st_size:
            raise ValueError("Modified original MIT cast data: " + f["output_path"])
    return True


def verified_download(m: dict, item: dict) -> bytes:
    rel=item["source_path"]
    url=("https://raw.githubusercontent.com/microsoft/Microsoft-Rocketbox/" +
         m["source_sha"] + "/" + urllib.parse.quote(rel, safe="/"))
    request=urllib.request.Request(url,headers={
        "User-Agent":"NONATION-MicrosoftMIT-CastVerifier/1.0",
    })
    with urllib.request.urlopen(request,timeout=95) as response:
        source=response.read(item["source_size"]+1)
    if len(source) != item["source_size"] or git_blob_hash(source) != item["sha1"]:
        raise ValueError("Original Microsoft MIT FBX/TGA Git blob SHA mismatch: "+rel)
    return source


def install(m: dict) -> None:
    CAST_ROOT.mkdir(parents=True,exist_ok=True)
    hashes={}
    # Stage all files in a temporary folder before touching Unity sources.
    with tempfile.TemporaryDirectory(prefix=".rocketbox-cast-",dir=str(CAST_ROOT)) as temp:
        stage=Path(temp)
        for item in m["files"]:
            binary=verified_download(m,item)
            if item["kind"]=="tga":
                parse_tga_header(binary)
                binary=resize_tga_box(binary,m["max_texture_edge"],
                    normal_map=item["output_path"].endswith("_normal.tga"))
            relative=Path(item["model"])/Path(item["output_path"]).name
            out=stage/relative
            out.parent.mkdir(parents=True,exist_ok=True)
            out.write_bytes(binary)
            hashes[item["output_path"]]={
                "sha256":sha256(binary),"size":len(binary),
                "upstream_blob_sha1":item["sha1"],
            }
        # No unverified source is moved into Unity prior to complete validation.
        for item in m["files"]:
            out=ROOT/item["output_path"]
            out.parent.mkdir(parents=True,exist_ok=True)
            (stage/item["model"]/out.name).replace(out)
        record={"upstream":m["source_sha"],"license":"Microsoft MIT",
                "textures_max_edge":m["max_texture_edge"],"assets":hashes}
        (CAST_ROOT/"verified-cast-assets.json").write_text(
            json.dumps(record,sort_keys=True,indent=2)+"\n",encoding="utf8")
    if not cache_valid(m):
        raise RuntimeError("Incomplete verified MIT Rocketbox rigged cast stage")


def main() -> None:
    parser=argparse.ArgumentParser()
    parser.add_argument("--verify-only",action="store_true")
    args=parser.parse_args()
    m=verify_manifest()
    if cache_valid(m):
        print("NONATION_ROCKETBOX_CAST_SOURCE_READY models=2 cached=1")
        return
    if args.verify_only:
        raise RuntimeError("No verified real Microsoft rigged adult cast staged")
    install(m)
    print("NONATION_ROCKETBOX_CAST_SOURCE_READY models=2 cached=0 "+
          "source="+m["source_sha"])


if __name__=="__main__":
    main()
