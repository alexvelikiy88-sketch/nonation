#!/usr/bin/env python3
"""Download only four genuine textured CC0 Quaternius FBX buildings for Unity.

Original author-uploaded 2021 OGA archive, exact archive and member SHA256.
Never accepts path traversal, partial mixed versions, unverified models,
Unity Asset Store source files, or Rockstar/GTASA proprietary resources.
Called BEFORE Unity imports assets in Unity Cloud Build Automation.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import tempfile
import time
from urllib.request import Request, urlopen
from zipfile import ZipFile, BadZipFile

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Tools" / "quaternius_hero_manifest.json"
DEST_ROOT = ROOT / "Assets" / "MIR" / "Art" / "QuaterniusHero" / "Resources" / "MIR" / "QuaterniusHero"


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def digest_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_manifest(path: Path = MANIFEST) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("schema") != "nonation/quaternius-hero-cc0/1":
        raise ValueError("Unrecognized Quaternius CC0 hero asset manifest")
    if data.get("license") != "CC0-1.0":
        raise ValueError("Only pinned original OpenGameArt CC0 source is allowed")
    if len(data.get("assets", [])) != 8:
        raise ValueError("Exactly four FBX and four original atlases are required")
    if sum(a["name"].endswith(".fbx") for a in data["assets"]) != 4:
        raise ValueError("Four nonprocedural architectural meshes required")
    if sum(a["name"].endswith(".png") for a in data["assets"]) != 4:
        raise ValueError("Four original palette textures required")
    for asset in data["assets"]:
        destination = ROOT / asset["destination"]
        if destination.parent.resolve() != DEST_ROOT.resolve():
            raise ValueError("Non-whitelisted Unity Resources output: " + str(destination))
        if destination.name != asset["name"]:
            raise ValueError("Output name is not an original pinned model name")
        if not asset["source_path"].startswith(
            "Ultimate Textured Building Pack - Dec 2019/"
        ):
            raise ValueError("Source archive path is not original Quaternius pack")
        if len(asset["sha256"]) != 64 or asset["size"] < 1:
            raise ValueError("Missing pin for " + asset["name"])
    return data


def check_local(data: dict, *, strict: bool) -> bool:
    ok = True
    for asset in data["assets"]:
        path = ROOT / asset["destination"]
        if not path.is_file():
            if strict:
                raise FileNotFoundError(str(path))
            ok = False
            continue
        if path.stat().st_size != asset["size"] or digest_file(path) != asset["sha256"]:
            raise ValueError("Modified Quaternius model/atlas detected: " + str(path))
    return ok


def download_archive(data: dict) -> bytes:
    url = data["archive_url"]
    if url != "https://opengameart.org/sites/default/files/ultimate_textured_building_pack_by_quaternius.zip":
        raise ValueError("Source URL is not the 2021 author-uploaded original")
    if data["archive_size"] != 29961262:
        raise ValueError("Unexpected Quaternius archive size")
    last = None
    for attempt in range(3):
        try:
            request = Request(url, headers={
                "User-Agent": "Mozilla/5.0 NONATION-CC0-Asset-Verifier/1.0",
                "Accept": "application/zip, application/octet-stream",
            })
            with urlopen(request, timeout=140) as response:
                blob = response.read(36000000)
                if response.read(1):
                    raise ValueError("Oversized Quaternius archive")
            if len(blob) != data["archive_size"]:
                raise ValueError("Unexpected bytes from OpenGameArt source")
            if sha256(blob) != data["archive_sha256"]:
                raise ValueError("CC0 Quaternius archive SHA-256 mismatch")
            return blob
        except (TimeoutError, OSError) as error:
            last = error
            time.sleep((attempt + 1) * 2)
    raise RuntimeError("Pinned archive unavailable; do not replace with unverified assets") from last


def extract_verified(data: dict, archive: bytes) -> dict[str, bytes]:
    if sha256(archive) != data["archive_sha256"]:
        raise ValueError("Archive SHA-256 mismatch")
    selected: dict[str, bytes] = {}
    with ZipFile(io.BytesIO(archive)) as z:
        for entry in data["assets"]:
            obj = z.read(entry["source_path"])  # zipfile verifies each member CRC
            if len(obj) != entry["size"] or sha256(obj) != entry["sha256"]:
                raise ValueError("CC0 asset is not pinned: " + entry["name"])
            if entry["name"].endswith(".fbx") and not (
                obj.startswith(b"Kaydara FBX Binary") or obj.startswith(b"; FBX")
            ):
                raise ValueError("Source architectural model is not an FBX")
            if entry["name"].endswith(".png") and not obj.startswith(b"\x89PNG\r\n\x1a\n"):
                raise ValueError("Source palette is not PNG")
            selected[entry["name"]] = obj
    if len(selected) != 8:
        raise ValueError("Incomplete CC0 imported architecture")
    return selected


def install(data: dict, files: dict[str, bytes]) -> None:
    DEST_ROOT.mkdir(parents=True, exist_ok=True)
    # Validate full archive *before* writing any model. Avoid a mixed model pack.
    for asset in data["assets"]:
        if asset["name"] not in files or sha256(files[asset["name"]]) != asset["sha256"]:
            raise ValueError("Unverified stage: " + asset["name"])
    staged = []
    try:
        for asset in data["assets"]:
            dst = DEST_ROOT / asset["name"]
            fd, tmp = tempfile.mkstemp(prefix=dst.name + ".", suffix=".part",
                                       dir=str(DEST_ROOT))
            with os.fdopen(fd, "wb") as f:
                f.write(files[asset["name"]])
                f.flush()
                os.fsync(f.fileno())
            staged.append((Path(tmp), dst))
        for tmp, dst in staged:
            os.replace(tmp, dst)
    finally:
        for tmp, _ in staged:
            tmp.unlink(missing_ok=True)
    if not check_local(data, strict=True):
        raise ValueError("CC0 stage verification failed")


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--verify-only", action="store_true")
    args = p.parse_args()
    data = read_manifest()
    if check_local(data, strict=args.verify_only):
        print("NONATION_QUATERNIUS_SOURCE_VERIFIED models=4 atlases=4 cached=1")
        return
    if args.verify_only:
        raise RuntimeError("CC0 hero models missing; build would use fake primitives")
    blobs = extract_verified(data, download_archive(data))
    install(data, blobs)
    print("NONATION_QUATERNIUS_SOURCE_VERIFIED models=4 atlases=4 "
          "zip_sha256=" + data["archive_sha256"])


if __name__ == "__main__":
    main()
