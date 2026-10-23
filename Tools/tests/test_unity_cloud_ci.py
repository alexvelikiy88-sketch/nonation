#!/usr/bin/env python3
"""Offline regression checks for authenticated Unity Cloud artifact hand-off."""
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / "unity_cloud_ci.py"
spec = importlib.util.spec_from_file_location("unity_cloud_ci", SCRIPT)
cloud = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cloud)

class CloudBuildTests(unittest.TestCase):
    def test_single_build_number(self):
        self.assertEqual(cloud.build_number({"build": 42}), 42)
        self.assertEqual(cloud.build_number([{"build": 3}]), 3)
        with self.assertRaises(ValueError):
            cloud.build_number([{"build": 2}, {"build": 3}])

    def test_revision(self):
        sha = "a" * 40
        self.assertTrue(cloud.matches_revision({"requestedRevision": sha}, sha))
        self.assertTrue(cloud.matches_revision({"changeset": [{"commitId": sha}]}, sha))
        self.assertFalse(cloud.matches_revision({"requestedRevision": "b" * 40}, sha))

    def test_zip_unpack_and_validation(self):
        with tempfile.TemporaryDirectory() as work:
            w = Path(work)
            archive = w / "cloud.zip"
            with zipfile.ZipFile(archive, "w") as z:
                z.writestr("WebGL/index.html", "<html>game</html>")
                z.writestr("WebGL/Build/game.wasm", "wasm")
                z.writestr("WebGL/Build/game.data", "data")
                z.writestr("WebGL/Build/game.js", "js")
            cloud.unpack_webgl(archive, w / "out")
            self.assertTrue((w / "out" / "index.html").is_file())
            self.assertTrue((w / "out" / "Build" / "game.wasm").is_file())

    def test_missing_individual_webgl_component(self):
        for missing in ("wasm", "data", "js"):
            with self.subTest(missing=missing), tempfile.TemporaryDirectory() as work:
                root = Path(work)
                (root / "Build").mkdir()
                (root / "index.html").write_text("<html>Unity</html>")
                for ext in ("wasm", "data", "js"):
                    if ext != missing:
                        (root / "Build" / f"game.{ext}").write_bytes(b"test")
                with self.assertRaisesRegex(ValueError, "Missing uncompressed"):
                    cloud.check_webgl(root)

    def test_compressed_artifact_is_not_pages_safe(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "Build").mkdir()
            (root / "index.html").write_text("<html>Unity</html>")
            for ext in ("wasm", "data", "js"):
                (root / "Build" / f"game.{ext}").write_bytes(b"test")
            (root / "Build" / "game.wasm.br").write_bytes(b"compressed")
            with self.assertRaisesRegex(ValueError, "Brotli/Gzip"):
                cloud.check_webgl(root)

    def test_empty_html_is_rejected(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "Build").mkdir()
            (root / "index.html").write_bytes(b"")
            with self.assertRaisesRegex(ValueError, "nonempty index.html"):
                cloud.check_webgl(root)

    def test_hosted_verification_checks_all_files_without_browser_claim(self):
        from unittest.mock import patch

        class Response:
            status = 200
            headers = {"Content-Type": "application/octet-stream"}
            def __init__(self, request):
                self.request = request
            def geturl(self):
                return self.request.full_url
            def __enter__(self):
                return self
            def __exit__(self, *args):
                return False

        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "Build").mkdir()
            (root / "index.html").write_bytes(b"<html>Unity</html>")
            for ext in ("wasm", "data", "js"):
                (root / "Build" / f"game.{ext}").write_bytes(b"test")
            seen = []
            def fake_urlopen(request, timeout=0):
                seen.append(request.full_url)
                self.assertEqual(request.get_method(), "HEAD")
                return Response(request)
            with patch.object(cloud, "urlopen", side_effect=fake_urlopen):
                cloud.verify_hosted(root, "https://example.github.io/nonation/")
            self.assertEqual(len(seen), 4)
            self.assertIn("https://example.github.io/nonation/index.html", seen)
            self.assertIn("https://example.github.io/nonation/Build/game.wasm", seen)

    def test_hosted_verification_rejects_http(self):
        with tempfile.TemporaryDirectory() as work:
            with self.assertRaisesRegex(ValueError, "HTTPS URL"):
                cloud.verify_hosted(Path(work), "http://example.github.io/nonation/")

    def test_no_empty_artifact(self):
        with tempfile.TemporaryDirectory() as work:
            w = Path(work)
            archive = w / "incomplete.zip"
            with zipfile.ZipFile(archive, "w") as z:
                z.writestr("index.html", "ok")
            with self.assertRaises(ValueError):
                cloud.unpack_webgl(archive, w / "out")

    def test_no_traversal(self):
        with tempfile.TemporaryDirectory() as work:
            w = Path(work)
            archive = w / "malicious.zip"
            with zipfile.ZipFile(archive, "w") as z:
                z.writestr("WebGL/index.html", "ok")
                z.writestr("WebGL/Build/game.wasm", "wasm")
                z.writestr("WebGL/Build/game.data", "data")
                z.writestr("WebGL/Build/game.js", "js")
                z.writestr("../outside.txt", "danger")
            with self.assertRaises(ValueError):
                cloud.unpack_webgl(archive, w / "out")
            self.assertFalse((w / "outside.txt").exists())

class CloudLogDiagnosisTests(unittest.TestCase):
    def test_earliest_compile_error_excludes_terminal_export_failure(self):
        log = "\n".join((
            "Starting Unity Editor",
            "Importing assets",
            "Assets/MIR/Scripts/Test.cs(5,17): error CS0117: bad API",
            "Some stack trace",
            "Unity Player Export Failure",
        ))
        detected = cloud.primary_failure_lines(log)
        self.assertEqual(len(detected), 1)
        self.assertEqual(detected[0][0], 3)
        self.assertIn("error CS0117", detected[0][1])
        self.assertNotIn("Unity Player Export Failure", detected[0][1])

    def test_no_invented_error_when_only_export_failure_exists(self):
        self.assertEqual(cloud.primary_failure_lines("Unity Player Export Failure"), [])

    def test_log_redaction_removes_credentials_and_signed_url(self):
        import os
        from unittest.mock import patch
        with patch.dict(os.environ, {"UNITY_SA_SECRET_KEY": "secret-unit-test"}):
            redacted = cloud.redact_log(
                "Failed secret-unit-test https://storage.googleapis.com/test?token=123"
            )
        self.assertNotIn("secret-unit-test", redacted)
        self.assertNotIn("token=123", redacted)
        self.assertIn("[REDACTED]", redacted)



class UnityCloudV2ContractTests(unittest.TestCase):
    """The supported v2 log API responds with 200, 202 or 307 (not only 303)."""

    def _response(self, status, body):
        class Response:
            def __init__(self):
                self.status = status
            def read(self, amount=-1):
                return body[:amount] if amount >= 0 else body
            def __enter__(self):
                return self
            def __exit__(self, *args):
                return False
        return Response()

    def test_autobuild_uses_documented_pagination_and_follows_revision(self):
        from unittest.mock import patch
        sha = "c" * 40
        with patch.object(cloud, "api", return_value=(
            200, [{"build": 12, "requestedRevision": sha}]
        )) as mocked:
            self.assertEqual(cloud.wait_for_autobuild(sha), 12)
        mocked.assert_called_once_with(
            "GET", cloud.ROOT + "/builds?per_page=20&page=1"
        )

    def test_v2_inline_200_log_requires_no_signed_download(self):
        from types import SimpleNamespace
        from unittest.mock import Mock, patch
        response = self._response(200, b"Assets/Test.cs(1): error CS0117")
        opener = SimpleNamespace(open=Mock(return_value=response))
        with patch.object(cloud, "secret_header", return_value="Basic test"), \\
                patch.object(cloud, "build_opener", return_value=opener), \\
                patch.object(cloud, "urlopen") as storage:
            text = cloud.read_cloud_log(3)
            self.assertIn("error CS0117", text)
            storage.assert_not_called()
        request = opener.open.call_args.args[0]
        self.assertIn("/builds/3/log?redirect=manual", request.full_url)
        self.assertEqual(request.get_header("Authorization"), "Basic test")

    def test_v2_manual_202_signed_url_has_no_unity_auth(self):
        from types import SimpleNamespace
        from unittest.mock import Mock, patch
        import json
        signed = "https://storage.googleapis.com/log.txt?sig=fake"
        response = self._response(202, json.dumps({"url": signed}).encode())
        opener = SimpleNamespace(open=Mock(return_value=response))
        storage = self._response(200, b"first real error\nother line\n")
        with patch.object(cloud, "secret_header", return_value="Basic test"), \\
                patch.object(cloud, "build_opener", return_value=opener), \\
                patch.object(cloud, "urlopen", return_value=storage) as downloader:
            self.assertIn("first real error", cloud.read_cloud_log(3))
            downloader.assert_called_once_with(signed, timeout=60)

    def test_v2_307_location_is_handled_without_credentials_redirect(self):
        from types import SimpleNamespace
        from unittest.mock import Mock, patch
        from urllib.error import HTTPError
        from io import BytesIO
        signed = "https://storage.googleapis.com/log.txt?sig=another"
        redirect = HTTPError(
            cloud.ROOT + "/builds/3/log", 307, "Temporary Redirect",
            {"Location": signed}, BytesIO(b"")
        )
        opener = SimpleNamespace(open=Mock(side_effect=redirect))
        with patch.object(cloud, "secret_header", return_value="Basic test"), \\
                patch.object(cloud, "build_opener", return_value=opener), \\
                patch.object(cloud, "urlopen", return_value=self._response(
                    200, b"Unity Editor imported the scene"
                )) as downloader:
            self.assertIn("Unity Editor", cloud.read_cloud_log(3))
            downloader.assert_called_once_with(signed, timeout=60)

    def test_log_redirect_cannot_loop_back_to_authenticated_api(self):
        from types import SimpleNamespace
        from unittest.mock import Mock, patch
        import json
        internal = f"{cloud.ROOT}/builds/3/log"
        opener = SimpleNamespace(open=Mock(return_value=self._response(
            202, json.dumps({"url": internal}).encode()
        )))
        with patch.object(cloud, "secret_header", return_value="Basic test"), \\
                patch.object(cloud, "build_opener", return_value=opener), \\
                patch.object(cloud, "urlopen") as downloader:
            with self.assertRaisesRegex(RuntimeError, "authenticated API"):
                cloud.read_cloud_log(3)
            downloader.assert_not_called()


class UnityDownloadLinkTests(unittest.TestCase):
    def test_project_scoped_v2_artifact(self):
        url, internal = cloud.artifact_link(
            f"/v2/orgs/{cloud.ORG}/projects/{cloud.PROJECT}/buildtargets/"
            f"{cloud.TARGET}/builds/3/download"
        )
        self.assertTrue(internal)
        self.assertTrue(url.startswith(cloud.API))

    def test_legacy_project_scoped_artifact(self):
        url, internal = cloud.artifact_link(
            f"/api/v1/orgs/{cloud.ORG}/projects/{cloud.PROJECT}/buildtargets/"
            f"{cloud.TARGET}/builds/3/download"
        )
        self.assertTrue(internal)
        self.assertIn("/api/v1/", url)

    def test_external_signed_storage_without_unity_credentials(self):
        link, internal = cloud.artifact_link(
            "https://storage.googleapis.com/example/archive.zip?Signature=sample"
        )
        self.assertFalse(internal)
        self.assertEqual(cloud.signed_url({"href": link}), link)

    def test_cross_project_and_unsafe_scheme_are_rejected(self):
        for address in (
            "/v2/orgs/another-org/projects/other/buildtargets/no/builds/1/download",
            "http://storage.googleapis.com/example.zip",
            "https://malicious.invalid@storage.googleapis.com/example.zip",
        ):
            with self.subTest(address=address), self.assertRaises(ValueError):
                cloud.artifact_link(address)

    def test_303_json_url_result_without_network(self):
        from unittest.mock import patch
        link = f"/v2/orgs/{cloud.ORG}/projects/{cloud.PROJECT}/buildtargets/{cloud.TARGET}/builds/3/download"
        signed = "https://storage.googleapis.com/archive.zip?token=shortlived"
        with patch.object(cloud, "authenticated_download_link", return_value=signed) as mocked:
            self.assertEqual(cloud.signed_url({"href": link}), signed)
            mocked.assert_called_once()


if __name__ == "__main__":
    unittest.main()
