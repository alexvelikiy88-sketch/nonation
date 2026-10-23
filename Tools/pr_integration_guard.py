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
        for path in re.findall(r"\x60([^\x60]+)\x60", cells[2]):
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


def is_critical(path: str) -> bool:
    return (
        path.startswith(("Assets/MIR/Scripts/", "Assets/MIR/Scenes/",
                         "Server/", "Packages/", "ProjectSettings/"))
        and path.endswith((".cs", ".unity", ".prefab", ".asset", ".json",
                           ".asmdef", ".controller"))
    )


def public_symbols(code: str) -> set[str]:
    return set(PUBLIC_TYPE.findall(code)) | set(PUBLIC_METHOD.findall(code))


def missing_public_symbols(before: str, after: str) -> set[str]:
    """Advisory only. A changed return type with the same name is not deletion."""
    return public_symbols(before) - public_symbols(after)


def check_changes(
    changes: list[Change],
    branch: str,
    body: str,
    ownership: dict[str, list[str]],
    patch: str = "",
) -> tuple[list[str], list[str]]:
    errors, warnings = [], []
    if branch not in REPORTS and not branch.startswith("manager/"):
        errors.append(f"Unknown workstream {branch!r}; use an assigned agent/manager branch.")
    for change in changes:
        paths = [change.path]
        if change.previous_path and change.previous_path != change.path:
            paths.append(change.previous_path)
        for path in paths:
            owner = owner_of(path, ownership)
            if branch in REPORTS and owner != branch:
                errors.append(
                    f"{path}: owner is {owner or 'unassigned'}, not {branch}; "
                    "request integration via CROSS-OWNER REQUEST."
                )
            if branch.startswith("manager/"):
                if path in REPORTS.values():
                    errors.append(f"{path}: manager must not overwrite agent reports.")
                elif owner in REPORTS and (
                    f"INTEGRATION-EXCEPTION: {path}" not in body
                ):
                    errors.append(
                        f"{path}: require 'INTEGRATION-EXCEPTION: {path}' in manager "
                        "PR description for cross-owner edits."
                    )

        old_path = change.previous_path or change.path
        if change.status[0] == "D" and is_critical(old_path):
            if branch in REPORTS or f"DELETION-REVIEWED: {old_path}" not in body:
                errors.append(
                    f"{old_path}: critical deletion requires a separately reviewed "
                    "manager PR with DELETION-REVIEWED justification."
                )
        if change.status[0] == "R" and is_critical(old_path):
            warnings.append(f"{old_path}: rename of critical file; review references/GUIDs.")
        if change.path.startswith("Assets/") and BANNED_ASSET.search(change.path):
            errors.append(f"{change.path}: known restricted/unverified asset name.")
        if (change.path.startswith("Assets/") and
                Path(change.path).suffix.lower() in ASSET_SUFFIXES and
                change.status[0] in "AMRC" and
                "ASSET-LICENSE-EVIDENCE:" not in body):
            errors.append(
                f"{change.path}: describe origin, version, license and redistribution "
                "rights with ASSET-LICENSE-EVIDENCE: in PR."
            )

    for line in patch.splitlines():
        if line.startswith("+") and not line.startswith("+++"):
            if any(pattern.search(line[1:]) for pattern in SECRETS):
                errors.append("Potential secret/private key added; remove it and "
                              "rotate if real. Sensitive content was not logged.")
                break
    return list(dict.fromkeys(errors)), list(dict.fromkeys(warnings))


def git(*args: str) -> bytes:
    return subprocess.check_output(["git", *args], stderr=subprocess.PIPE)



def comparison_base_for(head: str) -> str:
    """Use the actual merge-base with trusted checked-out main.

    Old pull_request.base.sha stays stale when agent branches merge changes from
    manager/main. Comparing against that SHA falsely attributes TASKS.md to
    agents even when they merely synced main.
    """
    ancestor = git("merge-base", "HEAD", head).decode("ascii", "replace").strip()
    if not re.fullmatch(r"[0-9a-f]{40}", ancestor):
        raise RuntimeError("git merge-base produced no valid SHA")
    return ancestor


def api_warnings(changes: list[Change], base: str, head: str) -> list[str]:
    warnings = []
    for change in changes:
        if change.status[0] != "M" or not change.path.endswith(".cs"):
            continue
        before = git("show", f"{base}:{change.path}").decode("utf-8", "replace")
        after = git("show", f"{head}:{change.path}").decode("utf-8", "replace")
        removed = missing_public_symbols(before, after)
        if removed:
            names = ", ".join(sorted(removed))
            warnings.append(
                f"{change.path}: public/protected API disappeared ({names}); "
                "review dependent modules and tests."
            )
    return warnings


def report(errors: list[str], warnings: list[str], count: int) -> int:
    summary = [
        "## NONATION Integration Guard", "",
        f"Changed files: {count}. Errors: {len(errors)}. Warnings: {len(warnings)}.",
        "", "Ownership is read from AGENTS.md in the trusted base revision.",
        "This check is not Unity compilation, a WebGL test or license verification.",
    ]
    for title, issues, level in (
        ("Blocking findings", errors, "error"),
        ("Review warnings", warnings, "warning")
    ):
        if issues:
            summary.extend(["", "### " + title])
        for issue in issues:
            safe = issue.replace("\r", " ").replace("\n", " ")
            print(f"::{level}::{safe}")
            summary.append("- " + safe)
    if not errors:
        print("PASS: integration ownership and change integrity")
    output = os.environ.get("GITHUB_STEP_SUMMARY")
    if output:
        with open(output, "a", encoding="utf-8") as handle:
            handle.write("\n".join(summary) + "\n")
    return 1 if errors else 0


def main() -> int:
    event_path = os.environ.get("GITHUB_EVENT_PATH")
    if not event_path:
        raise RuntimeError("GITHUB_EVENT_PATH missing: guard runs in PR workflow")
    event = json.loads(Path(event_path).read_text(encoding="utf-8"))
    pr = event.get("pull_request")
    if not isinstance(pr, dict) or pr.get("base", {}).get("ref") != "main":
        raise RuntimeError("Expected a pull_request against main")
    base, head = pr["base"]["sha"], pr["head"]["sha"]
    if not all(re.fullmatch(r"[0-9a-f]{40}", s) for s in (base, head)):
        raise ValueError("Untrusted or missing SHA in PR event")
    branch = pr["head"]["ref"]
    body = pr.get("body") or ""
    policy = parse_ownership(Path("AGENTS.md").read_text(encoding="utf-8"))
    # The event base SHA is validated above but can be stale on long-lived PRs.
    # Only agent-specific changes since common ancestry are subject to ownership.
    comparison_base = comparison_base_for(head)
    changes = parse_name_status_zero(
        git("diff", "--name-status", "-z", "--find-renames",
            comparison_base, head)
    )
    patch = git("diff", "--no-ext-diff", "--unified=0",
                comparison_base, head).decode("utf-8", "replace")
    errors, warnings = check_changes(changes, branch, body, policy, patch)
    warnings.extend(api_warnings(changes, comparison_base, head))
    return report(errors, list(dict.fromkeys(warnings)), len(changes))


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, KeyError, OSError, RuntimeError,
            subprocess.CalledProcessError) as error:
        print(f"::error::Integration Guard could not complete: {error}")
        sys.exit(1)
