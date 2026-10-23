#!/usr/bin/env python3
"""Independent, fail-closed ownership and destructive-change guard.

Run this version from the trusted PR base commit, never from unreviewed PR code.
It is not a Unity build or proof of asset-license validity.
"""
from __future__ import annotations
import fnmatch
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

REPORTS = {
    "agent/unity-webgl": "docs/agents/reports/01_UNITY_WEBGL.md",
    "agent/backend-simulation": "docs/agents/reports/02_BACKEND_SIMULATION.md",
    "agent/graphics-ui": "docs/agents/reports/03_GRAPHICS_UI.md",
}
LABELS = {"01 UNITY": "agent/unity-webgl",
          "02 BACKEND": "agent/backend-simulation",
          "03 GRAPHICS": "agent/graphics-ui"}
MANAGER_FILES = {
    "TASKS.md", "NONATION_MANAGER.md", "AGENTS.md",
    "DEPLOYMENT_STATUS_RU.md",
    ".github/workflows/pr-validation.yml",
    ".github/workflows/integration-guard.yml",
    "Tools/pr_integration_guard.py",
    "Tools/tests/test_pr_integration_guard.py",
}
ASSET_SUFFIXES = {".fbx", ".obj", ".glb", ".gltf", ".blend",
                  ".png", ".jpg", ".jpeg", ".webp", ".psd", ".tga"}
BANNED_ASSET = re.compile(
    r"(honda_civic|gign_paranoia|maid_skinned|pinkdress|"
    r"girl_highpoly|floral_look|casual_confidence|freya)", re.I
)
SECRETS = [
    re.compile(r"gh[pousr]_[A-Za-z0-9_]{36,}"),
    re.compile(r"github_pat_[A-Za-z0-9_]{40,}"),
    re.compile(r"sk-proj-[A-Za-z0-9_-]{25,}"),
    re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
]
PUBLIC_TYPE = re.compile(
    r"^\s*(?:public|protected)\s+(?:(?:sealed|abstract|partial|static)\s+)*"
    r"(?:class|interface|struct|enum)\s+([A-Za-z_]\w*)\b", re.M
)
PUBLIC_METHOD = re.compile(
    r"^\s*(?:public|protected)\s+"
    r"(?:(?:static|virtual|override|abstract|sealed|async|new)\s+)*"
    r"[\w<>,.?\[\]]+\s+([A-Za-z_]\w*)\s*\(", re.M
)


@dataclass(frozen=True)
class Change:
    status: str
    path: str
    previous_path: str | None = None


def parse_ownership(markdown: str) -> dict[str, list[str]]:
    """Extract actual owner paths from the AGENTS.md table at the base SHA."""
    result = {branch: [] for branch in REPORTS}
    for line in markdown.splitlines():
        if not line.lstrip().startswith("|"):
            continue
        cells = [part.strip() for part in line.strip().strip("|").split("|")]
        if len(cells) < 3 or cells[0] not in LABELS:
            continue
        branch = LABELS[cells[0]]
        for path in re.findall(r"&#96;([^&#96;]+)&#96;", cells[2]):
            if path.startswith("Mir") and "/" not in path:
                path = "Assets/MIR/Scripts/" + path
            result[branch].append(path)
    if any(not paths for paths in result.values()):
        raise ValueError("Missing agent ownership rules in AGENTS.md")
    return result


def owner_of(path: str, rules: dict[str, list[str]]) -> str | None:
    for branch, report in REPORTS.items():
        if path == report:
            return branch
    matches = [
        branch for branch, patterns in rules.items()
        if any(fnmatch.fnmatchcase(path, pattern) for pattern in patterns)
    ]
    if len(matches) > 1:
        raise ValueError(f"Ambiguous file ownership: {path}")
    if matches:
        return matches[0]
    if (path in MANAGER_FILES or path.startswith(("docs/agents/", "Documentation/"))
            or path.startswith("README")):
        return "manager"
    return None


def parse_name_status_zero(data: bytes) -> list[Change]:
    fields = data.decode("utf-8", "surrogateescape").split("\x00")
    if fields and not fields[-1]:
        fields.pop()
    result = []
    i = 0
    while i < len(fields):
        status = fields[i]
        i += 1
        if not status or status[0] not in "ACDMRTU":
            raise ValueError(f"Unrecognized git change status: {status!r}")
        if status[0] in "RC":
            if i + 1 >= len(fields):
                raise ValueError("Truncated rename/copy record")
            old, path = fields[i:i + 2]
            result.append(Change(status, path, old))
            i += 2
        else:
            if i >= len(fields):
                raise ValueError("Truncated status record")
            result.append(Change(status, fields[i]))
            i += 1
    return result
