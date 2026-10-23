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
import re
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
            if not actual:
                raise RuntimeError(
                    f"Unity Cloud build #{number} succeeded but provided no source revision; "
                    "refusing to publish an artifact with unverified provenance"
                )
            if str(actual) != revision:
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
    """Check every mandatory uncompressed file; structure is NOT a browser smoke test."""
    folder = root / "Build"
    index = root / "index.html"
    if not index.is_file() or index.stat().st_size == 0 or not folder.is_dir():
        raise ValueError("WebGL artifact has no nonempty index.html and Build/ directory")
    files = [p for p in folder.iterdir() if p.is_file()]
    if any(p.name.lower().endswith((".br", ".gz")) for p in files):
        raise ValueError(
            "WebGL artifact uses Brotli/Gzip files; GitHub Pages cannot set the required Content-Encoding header"
        )
    resources = [p.name.lower() for p in files if p.stat().st_size > 0]
    for extension in (".wasm", ".data", ".js"):
        if not any(name.endswith(extension) for name in resources):
            raise ValueError(f"Missing uncompressed Unity {extension} asset; check WebGL compression settings")
    print("PASS: nonempty index.html + uncompressed .wasm/.data/.js (not browser-verified)", flush=True)

def verify_hosted(root: Path, page_url: str):
    """Verify real HTTPS responses for every published file after Pages deploy.

    This proves hosting/asset retrieval, NOT Unity WebGL scene execution.
    """
    origin = urlparse(page_url)
    if (origin.scheme != "https" or not origin.hostname or
            origin.username or origin.password or origin.query or origin.fragment):
        raise ValueError("Deployed WebGL URL must be an HTTPS URL without credentials or query")
    check_webgl(root)
    base = page_url.rstrip("/") + "/"
    from urllib.parse import quote
    paths = sorted(p for p in root.rglob("*") if p.is_file())
    if not paths:
        raise ValueError("Nothing to verify on HTTPS hosting")
    for path in paths:
        relative = path.relative_to(root).as_posix()
        remote = urljoin(base, quote(relative, safe="/"))
        with urlopen(Request(remote, method="HEAD"), timeout=25) as response:
            resolved = urlparse(response.geturl())
            if resolved.scheme != "https" or resolved.hostname != origin.hostname:
                raise ValueError("Deployed asset redirected outside the expected HTTPS host: " + relative)
            if response.status != 200:
                raise ValueError("Deployed asset is not HTTP 200: " + relative)
            mime = response.headers.get("Content-Type", "").lower()
            if relative.lower().endswith((".wasm", ".data", ".js")) and "text/html" in mime:
                raise ValueError("Hosting serves HTML instead of Unity asset: " + relative)
        print("HTTPS 200:", relative, flush=True)
    print("PASS: hosted WebGL files are reachable over HTTPS (runtime NOT verified)", flush=True)


def read_cloud_log(number: int) -> str:
    """Read existing Unity Cloud log without triggering a new build or leaking auth."""
    if number < 1:
        raise ValueError("Build number must be positive")
    endpoint = f"{ROOT}/builds/{number}/log"
    request = Request(endpoint, method="GET", headers={
        "Authorization": secret_header(), "Accept": "text/plain"
    })
    maximum = 20 * 1024 * 1024
    try:
        with build_opener(NoRedirect()).open(request, timeout=35) as response:
            if response.status != 200:
                raise RuntimeError("Unity Cloud log returned HTTP " + str(response.status))
            data = response.read(maximum + 1)
    except HTTPError as error:
        if error.code != 303:
            raise RuntimeError("Unity Cloud build log is inaccessible: HTTP " + str(error.code)) from None
        # The API may return the signed storage URL as Location or JSON {url}.
        href = error.headers.get("Location")
        if not href:
            try:
                redirect = json.loads(error.read().decode("utf-8"))
                if isinstance(redirect, dict):
                    href = redirect.get("url")
            except (ValueError, UnicodeError):
                pass
        if not href:
            raise RuntimeError("Unity Cloud log redirect has no signed download URL")
        url, internal = artifact_link(href)
        if internal:
            raise RuntimeError("Unity Cloud log redirect remained inside the authenticated API")
        with urlopen(url, timeout=60) as response:
            data = response.read(maximum + 1)
    if len(data) > maximum:
        raise ValueError("Unity Cloud log exceeds 20 MiB diagnostic limit")
    return data.decode("utf-8", errors="replace")


def redact_log(line: str) -> str:
    """Never echo service-account secrets or signed storage URLs to CI logs."""
    for key in ("UNITY_SA_KEY_ID", "UNITY_SA_SECRET_KEY",
                "UNITY_LICENSE", "UNITY_SERIAL", "UNITY_EMAIL", "UNITY_PASSWORD"):
        value = os.environ.get(key)
        if value:
            line = line.replace(value, "[REDACTED]")
    line = re.sub(r"https://[^\s]+", "[URL]", line)
    return line[:500]


def primary_failure_lines(log: str):
    """Return first diagnostic error before any generic Player Export Failure line."""
    lines = log.splitlines()
    candidates = []
    for index, line in enumerate(lines):
        if "Unity Player Export Failure" in line:
            continue
        if re.search(r"(?i)(?:\berror\s+(?:CS|MSB|IL)\d+\b|"
                     r"\bBuildFailedException\b|"
                     r"\bException:\s|"
                     r"\b(?:error|fatal error)\s*:|"
                     r"\bfailed to (?:compile|import|resolve|load)\b)", line):
            candidates.append((index + 1, redact_log(line)))
    return candidates[:15]


def diagnose_build(number: int):
    """Read-only diagnostics of any existing Unity Cloud build (especially #3)."""
    status_code, data = api("GET", f"{ROOT}/builds/{number}")
    if status_code != 200 or not isinstance(data, dict):
        raise RuntimeError("Unity Cloud did not return existing build status")
    print("UNITY_CLOUD_DIAGNOSTIC build=" + str(number) +
          " status=" + str(data.get("buildStatus") or data.get("status")) +
          " revision=" + str(data.get("lastBuiltRevision") or data.get("requestedRevision")))
    log = read_cloud_log(number)
    print("Unity build log retrieved, lines=" + str(len(log.splitlines())))
    hits = primary_failure_lines(log)
    if hits:
        for line_number, line in hits:
            print(f"LOG ERROR line {line_number}: {line}")
    else:
        print("ROOT CAUSE UNKNOWN: no reliable pre-export error signature in the downloaded log.")
    if "Unity Player Export Failure" in log:
        print("Terminal Unity Player Export Failure also present; it is not a root cause.")
    return hits


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
    print("Verified Unity WebGL artifact:", destination.resolve(), "source_sha:", revision)
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
        if len(sys.argv) == 3 and sys.argv[1] == "--verify-dir":
            check_webgl(Path(sys.argv[2]))
        elif len(sys.argv) == 4 and sys.argv[1] == "--verify-hosted":
            verify_hosted(Path(sys.argv[2]), sys.argv[3])
        elif len(sys.argv) == 3 and sys.argv[1] == "--diagnose-build":
            if not sys.argv[2].isdigit() or int(sys.argv[2]) < 1:
                raise ValueError("Cloud build number must be a positive integer")
            diagnose_build(int(sys.argv[2]))
        elif len(sys.argv) == 1:
            run()
        else:
            raise ValueError(
                "Usage: unity_cloud_ci.py [--verify-dir DIR | --verify-hosted DIR HTTPS_URL | --diagnose-build NUMBER]"
            )
    except (ValueError, RuntimeError, TimeoutError, URLError, OSError, zipfile.BadZipFile) as error:
        print("ERROR:", str(error), file=sys.stderr)
        sys.exit(1)
