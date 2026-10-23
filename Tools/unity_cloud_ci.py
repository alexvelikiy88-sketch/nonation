#!/usr/bin/env python3
"""Unity Build Automation API v2 -> verified WebGL folder for GitHub Pages.

No third-party GitHub Action handles Unity service-account credentials.
Runs only after the .NET/source preflight and only when GitHub secrets exist.
Unity Cloud and GitHub source revisions must match before publishing.
"""
from __future__ import annotations

import base64
import gzip
import io
import json
import os
import zlib
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
        _, records = api("GET", ROOT + "/builds?per_page=20&page=1")
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
            try:
                diagnose_build(number)
            except Exception as diagnostic_error:
                print("Unable to download full Cloud failure log: " +
                      redact_log(str(diagnostic_error)), flush=True)
            raise RuntimeError(f"Unity Cloud WebGL build #{number} failed ({status})")
        if status in TERMINAL_GOOD:
            # requestedRevision is only an instruction, not evidence of the
            # source actually compiled by the builder. Publishing requires
            # the completed build's lastBuiltRevision to match GitHub exactly.
            actual = result.get("lastBuiltRevision")
            if not actual:
                raise RuntimeError(
                    f"Unity Cloud build #{number} lacks lastBuiltRevision; "
                    "refusing to publish a build with unverified source provenance"
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


def decode_cloud_log_bytes(data: bytes) -> str:
    """Decode Unity's compressed log objects without accepting binary as text.

    Unity log links can resolve to a gzip/ZIP object rather than text/plain.
    Limit decompressed bytes so a corrupt archive cannot exhaust the runner.
    """
    limit = 40 * 1024 * 1024
    if data.startswith(b"\x1f\x8b"):
        print("UNITY_CLOUD_LOG_ENCODING gzip", flush=True)
        with gzip.GzipFile(fileobj=io.BytesIO(data), mode="rb") as stream:
            data = stream.read(limit + 1)
    elif data.startswith(b"PK\x03\x04"):
        print("UNITY_CLOUD_LOG_ENCODING zip", flush=True)
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            options = [entry for entry in archive.infolist()
                       if not entry.is_dir()
                       and entry.filename.lower().endswith((".log", ".txt"))]
            if not options:
                raise ValueError("Unity Cloud ZIP has no .log/.txt entry")
            selected = max(options, key=lambda item: item.file_size)
            if selected.file_size > limit:
                raise ValueError("Unity Cloud ZIP log exceeds decompressed size cap")
            with archive.open(selected) as stream:
                data = stream.read(limit + 1)
    elif data[:2] in (b"\x78\x01", b"\x78\x9c", b"\x78\xda"):
        print("UNITY_CLOUD_LOG_ENCODING zlib", flush=True)
        inflater = zlib.decompressobj()
        decompressed = inflater.decompress(data, limit + 1)
        if not inflater.eof or inflater.unconsumed_tail:
            raise ValueError("Unity Cloud zlib log is incomplete or too large")
        data = decompressed
    else:
        print("UNITY_CLOUD_LOG_ENCODING text-or-unknown", flush=True)
    if len(data) > limit:
        raise ValueError("Unity Cloud decoded log exceeds 40 MiB")
    if b"\x00" in data[:1024] and not data.startswith((b"\xff\xfe", b"\xfe\xff")):
        raise ValueError("Unity Cloud log appears binary; magic=" + data[:8].hex())
    if data.startswith((b"\xff\xfe", b"\xfe\xff")):
        return data.decode("utf-16")
    try:
        return data.decode("utf-8-sig")
    except UnicodeDecodeError:
        raise ValueError("Unity Cloud log has unknown binary encoding; magic=" +
                         data[:8].hex()) from None


def read_cloud_log(number: int) -> str:
    """Read a completed UBA v2 log without submitting a new build.

    Documented v2 responses: 200 inline text, 202 JSON with signed URL
    (redirect=manual), or 307 HTTP redirect. Do not send Unity credentials
    to the signed storage URL.
    """
    if number < 1:
        raise ValueError("Build number must be positive")
    maximum = 20 * 1024 * 1024
    # Prefer native 307 Location redirect. The manual 202 payload format may
    # vary between UBA deployments and older archived builds.
    endpoint = f"{ROOT}/builds/{number}/log?redirect=auto"
    request = Request(endpoint, method="GET", headers={
        "Authorization": secret_header(), "Accept": "text/plain, application/json"
    })
    signed = None
    try:
        with build_opener(NoRedirect()).open(request, timeout=35) as response:
            if response.status == 200:
                data = response.read(maximum + 1)
            elif response.status == 202:
                # A 202 body contains a signed storage link rather than log text.
                payload = json.loads(response.read(65537).decode("utf-8"))
                signed = log_redirect_url(payload)
                data = None
            else:
                raise RuntimeError(
                    "Unexpected Unity Cloud log response: HTTP " + str(response.status)
                )
    except HTTPError as error:
        if error.code not in (303, 307):
            raise RuntimeError(
                "Unity Cloud build log is inaccessible: HTTP " + str(error.code)
            ) from None
        # Some API versions use Location (307), others return JSON (303).
        signed = error.headers.get("Location")
        if not signed:
            try:
                signed = log_redirect_url(json.loads(error.read(65537).decode("utf-8")))
            except (ValueError, UnicodeError):
                raise RuntimeError(
                    "Unity Cloud log redirect did not contain a valid download link"
                ) from None
        data = None
    if signed is not None:
        # Only a fresh, external HTTPS link is acceptable. No Authorization
        # headers are propagated to the storage endpoint.
        url, internal = artifact_link(signed)
        if internal:
            raise RuntimeError("Unity Cloud log redirect stayed inside the authenticated API")
        with urlopen(url, timeout=60) as response:
            data = response.read(maximum + 1)
    if data is None:
        raise RuntimeError("Unity Cloud API returned neither log text nor download URL")
    if len(data) > maximum:
        raise ValueError("Unity Cloud log exceeds 20 MiB compressed diagnostic limit")
    return decode_cloud_log_bytes(data)


def log_redirect_url(payload) -> str:
    """Unwrap a signed log link; never echo its contents or query token."""
    if isinstance(payload, str) and payload:
        return payload
    if isinstance(payload, dict):
        for name in ("url", "href", "signedUrl", "signed_url", "downloadUrl",
                     "download_url", "redirectUrl", "location"):
            candidate = payload.get(name)
            if isinstance(candidate, str) and candidate:
                return candidate
        for name in ("data", "result", "link", "download", "links"):
            candidate = payload.get(name)
            if isinstance(candidate, (str, dict)):
                try:
                    return log_redirect_url(candidate)
                except ValueError:
                    continue
        keys = [key for key in payload
                if isinstance(key, str) and re.fullmatch(r"[A-Za-z_]{1,32}", key)]
        raise ValueError(
            "Unity Cloud log response has no signed URL; safe fields: " +
            ", ".join(sorted(keys)[:12])
        )
    raise ValueError("Unity Cloud log response is not a URL or URL envelope")


def diagnostic_build_steps(number: int):
    """Read documented UBA step messages without downloading the full build log."""
    _, response = api("GET", f"{ROOT}/builds/{number}/steps")
    records = response if isinstance(response, list) else (
        response.get("steps", []) if isinstance(response, dict) else []
    )
    print("UNITY_CLOUD_STEP_DIAGNOSTIC build=" + str(number) +
          " steps=" + str(len(records)), flush=True)
    hits = []
    for index, step in enumerate(records):
        if not isinstance(step, dict):
            continue
        name = str(step.get("name") or "unknown")[:80]
        messages = step.get("messages") or []
        for message in messages:
            if not isinstance(message, dict):
                continue
            content = message.get("content")
            if not isinstance(content, str):
                continue
            for line in content.splitlines():
                if re.search(
                    r"(?i)(?:\berror\s+(?:CS|MSB|IL)\d+\b|"
                    r"\bBuildFailedException\b|\bException:\s|"
                    r"\b(?:error|fatal error)\s*:|"
                    r"\bfailed to (?:compile|import|resolve|load)\b)", line
                ):
                    hits.append((index, name, redact_log(line)))
                    break
        if len(hits) >= 12:
            break
    for index, name, excerpt in hits:
        print("UBA STEP ERROR index=" + str(index) + " name=" +
              redact_log(name) + " message=" + excerpt, flush=True)
    if not hits:
        for index, step in enumerate(records[-10:], start=max(0, len(records) - 10)):
            if isinstance(step, dict):
                messages = step.get("messages") or []
                print("UBA STEP SUMMARY index=" + str(index) +
                      " name=" + redact_log(str(step.get("name") or "unknown")) +
                      " message_count=" + str(len(messages)), flush=True)
    if not hits:
        print("No primary error signature found in structured UBA build steps.",
              flush=True)
    return hits


def redact_log(line: str) -> str:
    """Never echo service-account secrets or signed storage URLs to CI logs."""
    for key in ("UNITY_SA_KEY_ID", "UNITY_SA_SECRET_KEY",
                "UNITY_LICENSE", "UNITY_SERIAL", "UNITY_EMAIL", "UNITY_PASSWORD"):
        value = os.environ.get(key)
        if value:
            line = line.replace(value, "[REDACTED]")
    # Prevent accidently publishing Unity project SCM credentials or signed URLs.
    line = re.sub(r"(?i)\b(?:authorization|password|secret|token|api[-_]?key)\b\s*[:=]\s*\S+",
                  "[REDACTED_FIELD]", line)
    line = re.sub(r"https?://[^\s]+", "[URL]", line)
    line = re.sub(r"[\x00-\x1f\x7f]", " ", line)
    return line.replace("::", ": :")[:500]


def primary_failure_lines(log: str):
    """Return first diagnostic error before any generic Player Export Failure line."""
    lines = log.splitlines()
    candidates = []
    for index, line in enumerate(lines):
        if "Unity Player Export Failure" in line:
            # Diagnostics emitted after the terminal summary cannot establish
            # the first preceding failure that caused the export to abort.
            break
        if re.search(r"(?i)(?:\berror\s+(?:CS|MSB|IL)\d+\b|"
                     r"\bBuildFailedException\b|"
                     r"\bException:\s|"
                     r"\b(?:error|fatal error)\s*:|"
                     r"\bfailed to (?:compile|import|resolve|load)\b)", line):
            candidates.append((index + 1, redact_log(line)))
    return candidates[:15]


def diagnostic_log_signals(log: str):
    """Output bounded, scrubbed context for diagnosing a historical Unity build."""
    lines = log.splitlines()
    noisy = re.compile(
        r"(?i)\[AssemblyUpdater\] Failed to resolve type|"
        r"Native extension for .* target not found|"
        r"LOCALCOPY dir=|BuildScriptInfoCaches|RebuildScriptCaches|"
        r"LogAssemblyErrors \(\d+ms\)"
    )
    signal = re.compile(
        r"(?i)\b(?:error|failed|failure|exception|abort|unable|"
        r"cannot|missing|not found|crashed|nonzero|non-zero)\b|"
        r"buildscript|commandinvocationfailure|exit code|"
        r"\[error\]|compiler|compilation"
    )
    matches = [(i + 1, line) for i, line in enumerate(lines)
               if signal.search(line) and not noisy.search(line)
               and not re.search(r"(?i)\b(?:0 errors|no errors|0 failed)\b", line)]
    print("UBA_NONNOISY_LOG_SIGNALS total=" + str(len(matches)) +
          " source_lines=" + str(len(lines)), flush=True)
    selected = matches[:16] + matches[-34:]
    seen = set()
    for index, content in selected:
        if index in seen:
            continue
        seen.add(index)
        print("UBA LOG CANDIDATE line " + str(index) + ": " +
              redact_log(content), flush=True)
    terminal = next(
        (i for i, line in enumerate(lines)
         if "unity player export failed" in line.casefold()), None
    )
    if terminal is not None:
        print("UBA_TERMINAL_EXPORT_FAILURE line=" + str(terminal + 1),
              flush=True)
        for offset in range(max(0, terminal - 24), terminal + 1):
            if lines[offset].strip():
                print("UBA BEFORE EXPORT line " + str(offset + 1) + ": " +
                      redact_log(lines[offset]), flush=True)



def diagnose_build(number: int):
    """Read-only diagnostics of any existing Unity Cloud build (especially #3)."""
    status_code, data = api("GET", f"{ROOT}/builds/{number}")
    if status_code != 200 or not isinstance(data, dict):
        raise RuntimeError("Unity Cloud did not return existing build status")
    print("UNITY_CLOUD_DIAGNOSTIC build=" + str(number) +
          " status=" + str(data.get("buildStatus") or data.get("status")) +
          " revision=" + str(data.get("lastBuiltRevision") or data.get("requestedRevision")))
    details = data.get("failureDetails") or []
    for failure in details[:8] if isinstance(details, list) else []:
        if isinstance(failure, dict):
            message = failure.get("publicMessage") or failure.get("displayName") or ""
            print("UNITY_CLOUD_FAILURE_DETAIL: " + redact_log(str(message)), flush=True)
            if failure.get("logline") is not None:
                print("UNITY_CLOUD_FAILURE_LOGLINE: " +
                      redact_log(str(failure["logline"])), flush=True)
    # Structured build steps provide a secondary read-only diagnostic when
    # old jobs return nonstandard log redirects. Not a replacement for full log.
    try:
        log = read_cloud_log(number)
    except (ValueError, RuntimeError, HTTPError, URLError, OSError) as error:
        print("FULL_CLOUD_LOG_UNAVAILABLE: " + redact_log(str(error)), flush=True)
        try:
            diagnostic_build_steps(number)
        except (ValueError, RuntimeError, HTTPError, URLError, OSError) as step_error:
            print("UBA_STEP_DIAGNOSTICS_UNAVAILABLE: " +
                  redact_log(str(step_error)), flush=True)
        raise RuntimeError("Unity Cloud full log was not obtained; see safe step diagnostics above") from None
    print("Unity build log retrieved, lines=" + str(len(log.splitlines())))
    hits = primary_failure_lines(log)
    diagnostic_log_signals(log)
    if not hits:
        try:
            diagnostic_build_steps(number)
        except (ValueError, RuntimeError, HTTPError, URLError, OSError) as error:
            print("UBA_STEP_DIAGNOSTICS_UNAVAILABLE: " +
                  redact_log(str(error)), flush=True)
    if hits:
        for line_number, line in hits:
            print(f"LOG ERROR line {line_number}: {line}")
    else:
        print("ROOT CAUSE UNKNOWN: no reliable pre-export error signature in the downloaded log.")
    if "Unity Player Export Failure" in log:
        print("Terminal Unity Player Export Failure also present; it is not a root cause.")
    return hits


def cloud_build_payload(revision: str, branch: str) -> dict:
    """Pin a single UBA build to an exact Git SHA and authorized source branch."""
    if not re.fullmatch(r"[0-9a-f]{40}", revision):
        raise ValueError("Unity Cloud request needs a full immutable Git commit SHA")
    if branch not in ("main", "agent/unity-webgl"):
        raise ValueError("Unity Cloud build branch is not approved for NONATION")
    return {"clean": False, "delay": 0, "branch": branch, "commit": revision}


def run(preview: bool = False):
    revision = os.environ.get("GITHUB_SHA", "")
    branch = "agent/unity-webgl" if preview else "main"
    payload = cloud_build_payload(revision, branch)
    if preview and (
        os.environ.get("GITHUB_REF") != "refs/heads/agent/unity-webgl"
        or os.environ.get("GITHUB_EVENT_NAME") != "push"
        or os.environ.get("NONATION_ONE_SHOT_PREVIEW") != "1"
    ):
        raise ValueError("Agent preview requires explicitly gated branch push")
    status, target = api("GET", ROOT)
    if status != 200 or not isinstance(target, dict):
        raise RuntimeError("Could not inspect Unity build target")
    if not target.get("enabled", True):
        raise RuntimeError("Unity build target 'default-webgl' is disabled")
    settings = target.get("settings") or {}
    scm = settings.get("scm") if isinstance(settings.get("scm"), dict) else {}
    print("UNITY_CLOUD_TARGET enabled=True branch=" + str(scm.get("branch") or "unknown") +
          " autoBuild=" + str(bool(settings.get("autoBuild"))) +
          " preview=" + str(preview), flush=True)
    if not preview and settings.get("autoBuild"):
        number = wait_for_autobuild(revision)
    else:
        number = None
        if preview:
            # Keep the preview idempotent if an action is re-run. One source
            # revision means at most one new Cloud build request.
            _, existing = api("GET", ROOT + "/builds?per_page=20&page=1")
            if isinstance(existing, dict):
                existing = existing.get("builds", existing.get("items", []))
            if isinstance(existing, list):
                for item in existing:
                    if isinstance(item, dict) and matches_revision(item, revision):
                        number = build_number(item)
                        print("Reusing existing Unity Cloud preview #" + str(number), flush=True)
                        break
        if number is None:
            status, created = api("POST", ROOT + "/builds", payload)
            if status != 202:
                raise RuntimeError(f"Unity Cloud did not accept build request: HTTP {status}")
            number = build_number(created)
            print(f"Triggered ONE Unity Cloud WebGL #{number} branch={branch} sha={revision}")
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
        elif len(sys.argv) == 2 and sys.argv[1] == "--preview-build":
            run(preview=True)
        elif len(sys.argv) == 1:
            run()
        else:
            raise ValueError(
                "Usage: unity_cloud_ci.py [--verify-dir DIR | --verify-hosted DIR HTTPS_URL | --diagnose-build NUMBER | --preview-build]"
            )
    except (ValueError, RuntimeError, TimeoutError, URLError, OSError, zipfile.BadZipFile) as error:
        print("ERROR:", str(error), file=sys.stderr)
        sys.exit(1)
