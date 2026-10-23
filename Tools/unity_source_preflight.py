#!/usr/bin/env python3
"""Fast, license-free checks before real Unity compilation.

These checks do NOT replace Unity C# compilation, IL2CPP, or running WebGL.
"""
from __future__ import annotations
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
problems = []
notes = []

def require(ok: bool, message: str) -> None:
    if not ok:
        problems.append(message)

def mask_csharp(text: str) -> str:
    """Mask C# comments, strings and chars, retaining positions and newlines."""
    output = list(text)
    i, n = 0, len(text)
    while i < n:
        end = i
        found = False
        if text.startswith('//', i):
            end = text.find('\n', i)
            if end < 0:
                end = n
            found = True
        elif text.startswith('/*', i):
            end = text.find('*/', i + 2)
            if end < 0:
                problems.append('Unclosed C# block comment')
                end = n
            else:
                end += 2
            found = True
        elif text[i] == '"' or (text[i] in '@$' and i + 1 < n and text[i + 1] == '"') or text.startswith(('$@"', '@$"'), i):
            found = True
            verbatim = text[i] == '@' or text.startswith(('@$"', '$@"'), i)
            start = i + (1 if text[i] == '"' else 3 if text.startswith(('@$"', '$@"'), i) else 2)
            j = start
            while j < n:
                if text[j] == '"':
                    if verbatim and j + 1 < n and text[j + 1] == '"':
                        j += 2
                        continue
                    j += 1
                    break
                if not verbatim and text[j] == '\\':
                    j += 2
                    continue
                j += 1
            end = min(j, n)
        elif text[i] == "'":
            found = True
            j = i + 1
            while j < n:
                if text[j] == '\\':
                    j += 2
                    continue
                if text[j] == "'":
                    j += 1
                    break
                j += 1
            end = min(j, n)
        if found:
            for k in range(i, end):
                if output[k] not in '\r\n':
                    output[k] = ' '
            i = end
        else:
            i += 1
    return ''.join(output)

def check_source(path: pathlib.Path) -> None:
    rel = path.relative_to(ROOT).as_posix()
    src = path.read_text(encoding='utf-8-sig')
    code = mask_csharp(src)
    stack = []
    pairs = {')': '(', ']': '[', '}': '{'}
    for idx, c in enumerate(code):
        if c in '([{':
            stack.append((c, idx))
        elif c in ')]}':
            if not stack or stack[-1][0] != pairs[c]:
                problems.append(f'{rel}: mismatched bracket at offset {idx}')
                break
            stack.pop()
    else:
        require(not stack, f'{rel}: unclosed bracket(s)')
    depth = 0
    for line in src.splitlines():
        directive = re.match(r'^\s*#\s*(if|elif|else|endif)\b', line)
        if directive:
            kind = directive.group(1)
            if kind == 'if':
                depth += 1
            elif kind == 'endif':
                depth -= 1
                require(depth >= 0, f'{rel}: unmatched #endif')
            else:
                require(depth > 0, f'{rel}: #{kind} without #if')
    require(depth == 0, f'{rel}: unmatched #if')
    require(not re.search(r'(?<![\w])Application\s*\.\s*isPlayingOrWillChangePlaymode\b', code),
            f'{rel}: CS0117: use EditorApplication.isPlayingOrWillChangePlaymode')
    if '/Scripts/' in rel and 'using UnityEditor;' in code:
        problems.append(f'{rel}: runtime assembly directly references UnityEditor')

def main() -> int:
    all_cs = sorted((ROOT / 'Assets').rglob('*.cs'))
    require(len(all_cs) >= 20, f'Incomplete Unity source: {len(all_cs)} C# files')
    for file in all_cs:
        check_source(file)
    for name in ['Packages/manifest.json', 'Assets/MIR/Data/products_60.json',
                 'Assets/MIR/Data/professions_36.json', 'Assets/MIR/Data/systems.json']:
        file = ROOT / name
        if not file.is_file():
            problems.append(f'Missing required JSON: {name}')
            continue
        try:
            json.loads(file.read_text(encoding='utf-8-sig'))
        except (json.JSONDecodeError, UnicodeError) as err:
            problems.append(f'Invalid JSON {name}: {err}')
    required = {
        'Assets/MIR/Editor/MirUnitySetup.cs': ['EnsureBuildScene', 'EditorBuildSettings.scenes', 'PrepareForCloud'],
        'Assets/Editor/NonationCloudBuildHook.cs': ['IPreprocessBuildWithReport', 'PreExport', 'VerifyPublicAssets'],
        'Assets/MIR/Editor/MirCloudBuild.cs': ['BuildPipeline.BuildPlayer', 'BuildResult.Succeeded'],
        'Assets/MIR/Scripts/MirGame.cs': ['RuntimeInitializeOnLoadMethod', 'MirCity'],
        'Tools/fetch_quaternius.py': ['checksum(', 'hashlib.sha256', 'Quaternius']
    }
    for name, signatures in required.items():
        file = ROOT / name
        if not file.exists():
            problems.append(f'Missing build component: {name}')
            continue
        data = file.read_text(encoding='utf-8-sig')
        for marker in signatures:
            require(marker in data, f'{name}: missing build safety marker {marker!r}')
    scene = ROOT / 'Assets/MIR/Scenes/MIR_PlayableDistrict.unity'
    scene_meta = ROOT / 'Assets/MIR/Scenes/MIR_PlayableDistrict.unity.meta'
    game_meta = ROOT / 'Assets/MIR/Scripts/MirGame.cs.meta'
    build_settings = ROOT / 'ProjectSettings/EditorBuildSettings.asset'
    for artifact in (scene, scene_meta, game_meta, build_settings):
        require(artifact.is_file(), f'Missing committed Unity asset: {artifact.relative_to(ROOT)}')
    if all(x.is_file() for x in (scene, scene_meta, game_meta, build_settings)):
        scene_guid = scene_meta.read_text().split('guid: ')[1].splitlines()[0].strip()
        game_guid = game_meta.read_text().split('guid: ')[1].splitlines()[0].strip()
        require(len(scene_guid) == 32 and len(game_guid) == 32, 'Malformed scene/bootstrap GUID')
        require(scene_guid in build_settings.read_text(), 'Build Settings scene GUID mismatch')
        require(game_guid in scene.read_text(), 'Bootstrap script GUID mismatch')
    for shared in ('MirState.cs', 'MirEconomy.cs', 'MirSimulationLoop.cs',
                   'MirWorldGeometry.cs', 'MirCitizenDecision.cs', 'MirHeadlessPopulation.cs'):
        content = (ROOT / 'Assets/MIR/Scripts' / shared).read_text()
        require('using UnityEngine' not in content and 'Mathf.' not in content,
                f'{shared}: shared domain must not reference UnityEngine')
    assets = ROOT / 'Assets/MIR/Art/Quaternius'
    if not all((assets / name).exists() for name in (
        'Mannequin_F.fbx', 'UAL1_Standard_Unity.fbx', 'UAL2_Standard_Unity.fbx')):
        notes.append('Quaternius FBX not committed: use the Unity Cloud pre-build download hook')
    print(f'MIR preflight: {len(all_cs)} C# files, {len(problems)} errors, {len(notes)} notes')
    for note in notes:
        print('NOTE:', note)
    for item in problems:
        print('ERROR:', item, file=sys.stderr)
    if problems:
        print('Static preflight failed; real Unity compilation has NOT been run.', file=sys.stderr)
        return 1
    print('PASS: structural and known Unity API regression checks; NOT a Unity build')
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
