#!/usr/bin/env python3
"""Unity Build Automation API v2 -> verified WebGL folder for GitHub Pages.

No third-party GitHub Action handles Unity service-account credentials.
Runs only after the .NET/source preflight and only when GitHub secrets exist.
Unity Cloud and GitHub source revisions must match before publishing.
"""
from __future__ import annotations

import base64
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import sys
import tempfile
import time
from urllib.error import HTTPError, URLError
from urllib.parse import urljoin, urlparse
from urllib.request import Request, build_opener, HTTPRedirectHandler, urlopen
import zipfile

API = "https://build-automation.services.api.unity.com/v2"
ORG = "18968527430273"
PROJECT = "97c90bad-fafd-4410-8e4f-0ea68deb4232"
TARGET = "default-webgl"
ROOT = f"{API}/orgs/{ORG}/projects/{PROJECT}/buildtargets/{TARGET}"
CLOUD_HISTORY = f"https://cloud.unity.com/organizations/{ORG}/projects/{PROJECT}/cloud-build/history"
TERMINAL_GOOD = {"success"}
TERMINAL_BAD = {"failure", "failed", "canceled", "cancelled"}

class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, msg, headers, newurl):
        return None

def secret_header() -> str:
    key = os.environ.get("UNITY_SA_KEY_ID", "")
    secret = os.environ.get("UNITY_SA_SECRET_KEY", "")
    if not key or not secret:
        raise ValueError("UNITY_SA_KEY_ID / UNITY_SA_SECRET_KEY must be GitHub Actions secrets")
    return "Basic " + base64.b64encode(f"{key}:{secret}".encode()).decode("ascii")

def api(method: str, endpoint: str, payload=None):
    if not endpoint.startswith(ROOT) and endpoint != ROOT:
        raise ValueError("Refusing to send Unity credentials outside the selected project and target")
    data = None if payload is None else json.dumps(payload).encode("utf-8")
    request = Request(endpoint, method=method,
                      headers={"Authorization": secret_header(),
                               "Content-Type": "application/json",
                               "Accept": "application/json"},
                      data=data)
    try:
        with build_opener(NoRedirect()).open(request, timeout=35) as result:
            return result.status, json.loads(result.read().decode("utf-8") or "null")
    except HTTPError as err:
        if err.code == 303:
            return 303, json.loads(err.read().decode("utf-8"))
        try:
            detail = json.loads(err.read().decode("utf-8"))
            if isinstance(detail, dict):
                detail = detail.get("detail") or detail.get("error") or err.reason
        except (ValueError, UnicodeError):
            detail = str(err.reason)
        raise RuntimeError(f"Unity Cloud HTTP {err.code} on {method}: {detail}") from None

def build_number(value) -> int:
    if isinstance(value, list):
        if len(value) != 1:
            raise ValueError("Expected one Unity WebGL build, got an ambiguous response")
        value = value[0]
    if not isinstance(value, dict):
        raise ValueError("Invalid Unity build response")
    if value.get("error"):
        raise RuntimeError(f"Unity Cloud refused build: {value['error']}")
    number = value.get("build", value.get("buildNumber"))
    if not isinstance(number, int) or number < 1:
        raise ValueError("Unity API did not return a valid build number")
    return number

def matches_revision(build: dict, expected: str) -> bool:
    candidates = (build.get("requestedRevision"), build.get("lastBuiltRevision"))
    if any(str(item) == expected for item in candidates if item):
        return True
    return any(change.get("commitId") == expected for change in (build.get("changeset") or [])
               if isinstance(change, dict))

def wait_for_autobuild(revision: str) -> int:
    print("Unity Auto-build enabled; following its build instead of triggering a duplicate.")
    deadline = time.monotonic() + 180
    while time.monotonic() < deadline:
        _, records = api("GET", ROOT + "/builds?limit=20&offset=0")
        if isinstance(records, dict):
            records = records.get("builds", records.get("items", []))
        if isinstance(records, list):
            for item in records:
                if isinstance(item, dict) and matches_revision(item, revision):
                    return build_number(item)
        time.sleep(15)
    raise RuntimeError("Unity Auto-build did not expose a build for this commit. Check its SCM webhook and target settings.")

def poll_build(number: int, revision: str) -> dict:
    deadline = time.monotonic() + 65 * 60
    url = f"{ROOT}/builds/{number}"
    while time.monotonic() < deadline:
        _, result = api("GET", url)
        if not isinstance(result, dict):
            raise ValueError("Unity returned an unexpected build status response")
        status = str(result.get("buildStatus") or result.get("status") or "").lower()
        print(f"Unity Cloud build #{number}: {status or 'pending'}", flush=True)
        if status in TERMINAL_BAD:
            details = result.get("failureDetails") or []
            if details:
                for failure in details[:5]:
                    if isinstance(failure, dict):
                        print("Build error:", failure.get("publicMessage") or failure.get("displayName"),
                              "line:", failure.get("logline"))
            raise RuntimeError(f"Unity Cloud WebGL build #{number} failed ({status})")
        if status in TERMINAL_GOOD:
            actual = result.get("lastBuiltRevision") or result.get("requestedRevision")
            if actual and str(actual) != revision:
                raise RuntimeError(f"Unity built the wrong revision: expected {revision}, got {actual}")
            return result
        if result.get("finished"):
            raise RuntimeError(f"Unity finished with an unrecognized build status: {status}")
        time.sleep(30)
    raise TimeoutError(f"Unity build #{number} is not finished after 65 minutes; check {CLOUD_HISTORY}")

def artifact_link(href: str):
    """Resolve download URLs with a strict project-scoped Unity auth boundary.

    Unity build links can be absolute storage URLs, relative v2 API URLs or
    legacy /api/v1 download endpoints. They need different auth handling.
    """
    if not isinstance(href, str) or not href:
        raise ValueError("Unity artifact href is empty")
    url = urljoin(API + "/", href)
    parsed = urlparse(url)
    if parsed.scheme != "https" or not parsed.hostname or parsed.username or parsed.password:
        raise ValueError("Only HTTPS Unity build download links without credentials are accepted")
    internal = parsed.netloc.lower() == urlparse(API).netloc.lower()
    if internal:
        prefixes = (
            f"/v2/orgs/{ORG}/projects/{PROJECT}/buildtargets/{TARGET}/builds/",
            f"/api/v1/orgs/{ORG}/projects/{PROJECT}/buildtargets/{TARGET}/builds/",
        )
        if not any(parsed.path.startswith(prefix) for prefix in prefixes):
            raise ValueError("Unity download link escaped the selected project/build target")
    return url, internal


def authenticated_download_link(url: str):
    """Unity may return HTTP 303 with the signed URL in a JSON body, not Location."""
    href, internal = artifact_link(url)
    if not internal:
        raise ValueError("Only Unity service endpoints can receive service account credentials")
    request = Request(
        href,
        headers={"Authorization": secret_header(), "Accept": "application/json"},
        method="GET",
    )
    try:
        with build_opener(NoRedirect()).open(request, timeout=35) as response:
            raise ValueError(
                f"Expected Unity download redirect HTTP 303; got {response.status}"
            )
    except HTTPError as error:
        if error.code != 303:
            raise RuntimeError(f"Unity artifact request failed: HTTP {error.code}") from None
        return json.loads(error.read().decode("utf-8"))


def signed_url(primary):
    if not isinstance(primary, dict) or not primary.get("href"):
        raise ValueError("Successful Unity build has no primary artifact download URL")
    url, internal = artifact_link(primary["href"])
    if not internal:
        return url
    result = authenticated_download_link(url)
    if isinstance(result, dict):
        result = result.get("url")
    if not isinstance(result, str):
        raise ValueError("Unity artifact redirect did not contain a signed URL")
    redirect, is_internal = artifact_link(result)
    if is_internal:
        raise ValueError("Unity redirect did not return external signed storage")
    return redirect

def unpack_webgl(archive: Path, destination: Path):
    with zipfile.ZipFile(archive) as z:
        files = [item for item in z.infolist() if not item.is_dir()
                 and not item.filename.startswith("__MACOSX/")]
        files = [item for item in files if not PurePosixPath(item.filename).name.startswith(".")]
        indexes = [PurePosixPath(item.filename) for item in files
                   if PurePosixPath(item.filename).name == "index.html"]
        if len(indexes) != 1:
            raise ValueError("Expected exactly one Unity WebGL index.html in cloud archive")
        prefix = indexes[0].parent
        if destination.exists():
            shutil.rmtree(destination)
        destination.mkdir(parents=True)
        total = 0
        for item in files:
            name = PurePosixPath(item.filename)
            if name.is_absolute() or ".." in name.parts:
                raise ValueError("Unsafe ZIP archive entry")
            if prefix != PurePosixPath("."):
                if not name.is_relative_to(prefix):
                    continue
                name = name.relative_to(prefix)
            if not name.parts:
                continue
            # Do not permit archived symbolic links to overwrite the runner filesystem.
            if (item.external_attr >> 16) & 0o170000 == 0o120000:
                raise ValueError("ZIP contains a symlink")
            total += item.file_size
            if total > 2 * 1024**3:
                raise ValueError("Unity archive unexpectedly exceeds 2 GiB")
            path = destination.joinpath(*name.parts)
            path.parent.mkdir(parents=True, exist_ok=True)
            with z.open(item) as source, path.open("wb") as sink:
                shutil.copyfileobj(source, sink)
    check_webgl(destination)

def check_webgl(root: Path):
    folder = root / "Build"
    if not (root / "index.html").is_file() or not folder.is_dir():
        raise ValueError("Cloud artifact has no playable WebGL structure")
    resources = [p.name.lower() for p in folder.iterdir() if p.is_file() and p.stat().st_size > 0]
    for extension in (".wasm", ".data", ".js"):
        if not any(name.endswith(extension) for name in resources):
            raise ValueError(f"Missing uncompressed Unity {extension} asset; check WebGL compression settings")

def run():
    revision = os.environ.get("GITHUB_SHA", "")
    if len(revision) != 40 or not all(c in "0123456789abcdef" for c in revision):
        raise ValueError("GITHUB_SHA must be the full verified source revision")
    status, target = api("GET", ROOT)
    if status != 200 or not isinstance(target, dict):
        raise RuntimeError("Could not inspect Unity build target")
    if not target.get("enabled", True):
        raise RuntimeError("Unity build target 'default-webgl' is disabled")
    if target.get("settings", {}).get("autoBuild"):
        number = wait_for_autobuild(revision)
    else:
        status, created = api("POST", ROOT + "/builds", {"clean": False, "delay": 0, "branch": "main"})
        if status != 202:
            raise RuntimeError(f"Unity Cloud did not accept build request: HTTP {status}")
        number = build_number(created)
        print(f"Triggered Unity Cloud WebGL #{number}")
    result = poll_build(number, revision)
    primary = (result.get("links") or {}).get("download_primary")
    link = signed_url(primary)
    destination = Path("build/WebGL")
    with tempfile.TemporaryDirectory(prefix="nonation-webgl-") as temp:
        archive = Path(temp) / "webgl.zip"
        # Download a time-limited signed URL WITHOUT any Unity authentication header.
        with urlopen(link, timeout=120) as response, archive.open("wb") as sink:
            shutil.copyfileobj(response, sink)
        unpack_webgl(archive, destination)
    print("Verified Unity WebGL artifact:", destination.resolve())
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as out:
        out.write("webgl_ready=true\n")
        out.write(f"cloud_build_number={number}\n")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as out:
            out.write(f"Unity Cloud WebGL build #{number} passed and its artifact was verified.\n")
            out.write(f"Build history: {CLOUD_HISTORY}\n")

if __name__ == "__main__":
    try:
        run()
    except (ValueError, RuntimeError, TimeoutError, URLError, OSError, zipfile.BadZipFile) as error:
        print("ERROR:", str(error), file=sys.stderr)
        sys.exit(1)
