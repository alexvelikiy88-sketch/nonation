#!/usr/bin/env python3
"""Download only license-permitted Quaternius Standard Unity clips and validate bytes.

These source FBX files are CC0. No user-provided characters or NC assets are downloaded.
"""
import hashlib
import os
import pathlib
import shutil
import tempfile
import urllib.request
import zipfile

BASE = pathlib.Path(__file__).resolve().parents[1]
OUT = BASE / 'Assets/MIR/Art/Quaternius'
SOURCES = [
    (
        'https://opengameart.org/sites/default/files/universal_animation_librarystandard.zip',
        [('Unity/AnimationLibrary_Unity_Standard.fbx', 'UAL1_Standard_Unity.fbx', '42f16a4ed61d1e9f5fc7c2526894c5487148ac6ba82b64281ceb7943570f3c37')]
    ),
    (
        'https://opengameart.org/sites/default/files/universal_animation_library_2standard.zip',
        [
            ('/Unity/UAL2_Standard.fbx', 'UAL2_Standard_Unity.fbx', 'd4a2dd67bb12bf0c01891bc59ee697e04db679d26883d30bd937c2f3fb6fec90'),
            ('Female Mannequin/Unity/Mannequin_F.fbx', 'Mannequin_F.fbx', '890784432838b1f4f9194fd6011769888872a7ba7f3159689771e1998c96a42c'),
        ]
    ),
]

def checksum(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='mir-cc0-') as td:
        for index, (url, entries) in enumerate(SOURCES):
            if all((OUT / name).is_file() and checksum(OUT / name) == sha for _, name, sha in entries):
                print('CC0 animation pack already verified:', index + 1)
                continue
            target = pathlib.Path(td) / f'quaternius-{index + 1}.zip'
            req = urllib.request.Request(url, headers={'User-Agent': 'MIR-Dockside-AssetFetcher/1.0'})
            with urllib.request.urlopen(req, timeout=90) as response, target.open('wb') as out:
                shutil.copyfileobj(response, out)
            with zipfile.ZipFile(target) as z:
                for suffix, name, expected_sha in entries:
                    found = [x for x in z.namelist() if x.endswith(suffix)]
                    if len(found) != 1:
                        raise RuntimeError(f'Missing or ambiguous archive member {suffix!r}: {len(found)}')
                    destination = OUT / name
                    with z.open(found[0]) as inp, destination.open('wb') as out:
                        shutil.copyfileobj(inp, out)
                    digest = checksum(destination)
                    if digest != expected_sha:
                        destination.unlink(missing_ok=True)
                        raise RuntimeError(f'Unexpected checksum for {name}: {digest}')
                    print(f'Verified CC0 animation: {name} ({destination.stat().st_size} bytes)')
    print('Quaternius animation assets ready; personal-use and third-party models excluded.')

if __name__ == '__main__':
    main()
