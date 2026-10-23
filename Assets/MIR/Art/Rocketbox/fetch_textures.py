#!/usr/bin/env python3
"""Fetch pinned, MIT-licensed Microsoft Rocketbox pilot textures with Git blob checks.

This utility is NOT a Unity importer and does not modify a scene, Resources or build.
Generate local reduced TGA files for an Editor-only pilot. Redistribute Microsoft MIT
license alongside resulting assets. Only tested input: original uncompressed 32-bit TGA.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import tempfile
import urllib.request

SOURCE_COMMIT = "0943055db6ec570bcef9f2c8b41c9e5467c808f9"
SOURCE_DIR = "Assets/Avatars/Adults/Male_Adult_12/Textures"
SOURCE_URL = f"https://raw.githubusercontent.com/microsoft/Microsoft-Rocketbox/{SOURCE_COMMIT}/{SOURCE_DIR}"
SOURCE_LENGTH = 12_582_956
TEXTURES = {
    "m007_body_color.tga": "1757150789a5fdcf733394ff21a60512530b4f1a",
    "m007_body_normal.tga": "5bc8f98e588d55964e2b6a2d0029778f4a9db9e6",
    "m007_body_specular.tga": "611bbd149179a52c6fbded07375904d462e49c19",
    "m007_head_color.tga": "e5b23c4f55e499893c2a92ec7e52d0252bee2ae3",
    "m007_head_normal.tga": "0a99ce9aa6014e94c3385555f66a14da7599b757",
    "m007_head_specular.tga": "81fe377939cf8eaedd5d14dfb5261650de86bf1e",
}


def git_blob_hash(payload: bytes) -> str:
    return hashlib.sha1(f"blob {len(payload)}\0".encode("ascii") + payload).hexdigest()


def parse_tga_header(payload: bytes) -> tuple[int, int, int, int]:
    if len(payload) < 18:
        raise ValueError("TGA header missing")
    id_length, palette_type, image_type = payload[:3]
    width, height, bit_depth, descriptor = struct.unpack_from("<HHBB", payload, 12)
    if palette_type or image_type != 2 or bit_depth != 32 or not width or not height:
        raise ValueError("Only uncompressed 32-bit true-color TGA is supported")
    offset = 18 + id_length
    if len(payload) < offset + width * height * 4:
        raise ValueError("Truncated TGA pixels")
    return width, height, offset, descriptor


def resize_tga_box(payload: bytes, max_edge: int, *, normal_map: bool = False) -> bytes:
    """Power-of-two box filter for BGRA pixels while retaining TGA orientation.

    Tangent-space normals are renormalized after filtering, not treated as colors.
    Unity material import and tangent-space correctness still require visual QA.
    """
    if not 64 <= max_edge <= 2048:
        raise ValueError("max_edge must be between 64 and 2048")
    width, height, offset, descriptor = parse_tga_header(payload)
    factor = 1
    while max(width, height) > factor * max_edge:
        factor *= 2
    if factor == 1:
        return payload
    if width % factor or height % factor:
        raise ValueError("Only exact power-of-two downscales are supported")
    out_width, out_height = width // factor, height // factor
    dst = bytearray(out_width * out_height * 4)
    view = memoryview(payload)[offset:offset + width * height * 4]
    area = factor * factor
    for y in range(out_height):
        first_row = y * factor * width * 4
        for x in range(out_width):
            sums = [0, 0, 0, 0]
            first = first_row + x * factor * 4
            for yy in range(factor):
                pos = first + yy * width * 4
                for xx in range(factor):
                    idx = pos + xx * 4
                    for ch in range(4):
                        sums[ch] += view[idx + ch]
            index = (y * out_width + x) * 4
            for ch in range(4):
                dst[index + ch] = (sums[ch] + area // 2) // area
            if normal_map:
                # TGA stores B,G,R,A; normalize the XYZ normal from RGB.
                nx = (sums[2] / area - 127.5) / 127.5
                ny = (sums[1] / area - 127.5) / 127.5
                nz = (sums[0] / area - 127.5) / 127.5
                length = math.sqrt(nx * nx + ny * ny + nz * nz)
                if length < 1e-7:
                    nx, ny, nz, length = 0.0, 0.0, 1.0, 1.0
                for channel, value in ((2, nx), (1, ny), (0, nz)):
                    dst[index + channel] = max(0, min(255, round((value / length * .5 + .5) * 255)))
    header = bytearray(payload[:18])
    header[0] = 0  # no image ID in generated texture
    struct.pack_into("<HH", header, 12, out_width, out_height)
    header[17] = descriptor
    return bytes(header) + bytes(dst)


def download_verified(name: str, expected_sha: str) -> bytes:
    request = urllib.request.Request(
        SOURCE_URL + "/" + name,
        headers={"User-Agent": "NONATION-RocketboxPilot/1.0"},
    )
    with urllib.request.urlopen(request, timeout=90) as response:
        raw = response.read(SOURCE_LENGTH + 1)
    if len(raw) != SOURCE_LENGTH or git_blob_hash(raw) != expected_sha:
        raise ValueError(f"Unverified or unexpected upstream texture: {name}")
    parse_tga_header(raw)
    return raw


def prepare_textures(output: Path, max_edge: int, downloader=download_verified) -> dict:
    """Stage and validate all six files before exposing any new texture.

    A source failure never leaves a partially downloaded batch in output.
    As with any multi-file replacement, disk failure during the final commit is
    not atomic; preserve the generated manifest for post-import verification.
    """
    output = Path(output)
    output.parent.mkdir(parents=True, exist_ok=True)
    details = {}
    with tempfile.TemporaryDirectory(prefix=".rocketbox-stage-", dir=output.parent) as staged:
        staged = Path(staged)
        for name, blob_sha in TEXTURES.items():
            data = resize_tga_box(
                downloader(name, blob_sha), max_edge, normal_map=name.endswith("_normal.tga")
            )
            width, height, _, _ = parse_tga_header(data)
            details[name] = {
                "source_blob_sha1": blob_sha,
                "sha256": hashlib.sha256(data).hexdigest(),
                "width": width,
                "height": height,
                "bytes": len(data),
            }
            (staged / name).write_bytes(data)
        manifest = {
            "upstream_commit": SOURCE_COMMIT,
            "license": "Microsoft Rocketbox MIT; retain LICENSE.md",
            "max_edge": max_edge,
            "normal_map_filter": "box-average then unit-vector normalization",
            "textures": details,
        }
        (staged / "textures_manifest.json").write_text(
            json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8"
        )
        output.mkdir(parents=True, exist_ok=True)
        for name in TEXTURES:
            (staged / name).replace(output / name)
        (staged / "textures_manifest.json").replace(output / "textures_manifest.json")
    return manifest


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parent / "Textures")
    parser.add_argument("--max-edge", type=int, choices=(512, 1024, 2048), default=1024)
    parser.add_argument("--dry-run", action="store_true", help="List pinned inputs without downloading")
    args = parser.parse_args()
    if args.dry_run:
        for name, blob_sha in TEXTURES.items():
            print(f"MIT {name}: {SOURCE_LENGTH} bytes, git-sha1={blob_sha}")
        return
    manifest = prepare_textures(args.output, args.max_edge)
    for name, details in manifest["textures"].items():
        print(f"OK: {name} {details['width']}x{details['height']}, "
              f"{details['bytes']} bytes (verified upstream MIT source)")
    print("Six textures and SHA-256 manifest prepared; Unity material/LOD QA still required.")


if __name__ == "__main__":
    main()
