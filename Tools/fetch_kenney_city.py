#!/usr/bin/env python3
"""Fetch pinned, free Kenney CC0 glTF city assets for Unity's native glTFast importer.

Only the explicitly reviewed GLB files in kenney_city_manifest.json are downloaded.
Every payload must match its GitHub blob SHA-1, exact size and GLB v2 header.
An offline machine may keep playing the old geometry; runtime fallback also uses
the same immutable source revision. No licensed commercial Synty asset is fetched.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import struct
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Tools/kenney_city_manifest.json"
OUT = ROOT / "Assets/MIR/Resources/MIR/Kenney"
MAX_BYTES = 4 * 1024 * 1024


def checked_records():
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    assert data["schema"] == "nonation-kenney-city/1"
    assert data["upstream"] == "Hidencod/tge-assets"
    assert data["original_license"] == "CC0-1.0"
    assert len(data["revision"]) == 40 and all(c in "0123456789abcdef" for c in data["revision"])
    assert data["models"] and len(data["models"]) <= 40
    seen = set()
    for model in data["models"]:
        name, path, size = model["id"], PurePosixPath(model["path"]), model["size"]
        assert isinstance(name, str) and name + ".glb" == str(path).removeprefix("packs/")
        assert str(path).startswith("packs/") and len(path.parts) == 3
        assert all(p not in (".", "..") for p in path.parts)
        assert path.suffix == ".glb" and name not in seen
        assert isinstance(size, int) and 20 <= size <= MAX_BYTES
        sha = model["git_blob_sha1"]
        assert len(sha) == 40 and all(c in "0123456789abcdef" for c in sha)
        seen.add(name)
        yield data["revision"], model


def verify(data: bytes, model: dict):
    if len(data) != model["size"]:
        raise ValueError(f"size mismatch for {model['id']}: {len(data)} != {model['size']}")
    if data[:4] != b"glTF" or struct.unpack_from("<I", data, 4)[0] != 2:
        raise ValueError(f"invalid GLB v2 header: {model['id']}")
    if struct.unpack_from("<I", data, 8)[0] != len(data):
        raise ValueError(f"invalid GLB length: {model['id']}")
    git_sha = hashlib.sha1(f"blob {len(data)}\0".encode("ascii") + data).hexdigest()
    if git_sha != model["git_blob_sha1"]:
        raise ValueError(f"Git blob SHA mismatch for {model['id']}: {git_sha}")
    return True


def fetch(revision: str, model: dict):
    url = ("https://raw.githubusercontent.com/Hidencod/tge-assets/" +
           revision + "/" + model["path"])
    req = urllib.request.Request(url, headers={"User-Agent": "NONATION-CC0-Asset-Importer/1.0"})
    with urllib.request.urlopen(req, timeout=45) as response:
        data = response.read(model["size"] + 1)
    verify(data, model)
    return data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--verify-only", action="store_true",
                        help="Verify the assets already on disk without any network access")
    args = parser.parse_args()
    total = verified = downloaded = 0
    for revision, model in checked_records():
        path = OUT / (model["id"] + ".glb")
        data = path.read_bytes() if path.exists() else None
        if data is not None:
            try:
                verify(data, model)
                verified += 1
                print("VERIFIED:", model["id"])
                total += 1
                continue
            except ValueError:
                if args.verify_only:
                    raise
        if args.verify_only:
            raise FileNotFoundError(f"Pinned city GLB missing: {path}")
        data = fetch(revision, model)
        path.parent.mkdir(parents=True, exist_ok=True)
        # Atomically publish each validated asset: no half-written .glb is imported by Unity.
        with tempfile.NamedTemporaryFile(dir=path.parent, prefix=".nonation-cc0-",
                                         suffix=".part", delete=False) as tmp:
            tmp.write(data)
            temp_path = Path(tmp.name)
        temp_path.replace(path)
        downloaded += 1
        total += 1
        print("FETCHED:", model["id"], len(data), "bytes")
    print("NONATION_KENNEY_ASSETS_OK:", total, "models, fetched:", downloaded,
          "cached:", verified, "(verified Git blobs, CC0 Kenney)")


if __name__ == "__main__":
    main()
