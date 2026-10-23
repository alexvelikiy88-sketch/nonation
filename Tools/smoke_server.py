#!/usr/bin/env python3
"""Run only against a locally launched nonation .NET server in GitHub Actions."""
import json
import time
import urllib.error
import urllib.request

BASE = "http://127.0.0.1:5180"

def get_json(path):
    with urllib.request.urlopen(BASE + path, timeout=3) as response:
        assert response.status == 200, (path, response.status)
        return json.load(response)

def run():
    deadline = time.monotonic() + 15
    while True:
        try:
            health = get_json("/healthz")
            assert health.get("status") == "ok", health
            break
        except (urllib.error.URLError, TimeoutError, ConnectionResetError):
            if time.monotonic() > deadline:
                raise RuntimeError("nonation server never became ready on loopback")
            time.sleep(.25)

    spectator = get_json("/api/v1/spectator/scene")
    assert spectator["version"] == 2
    assert isinstance(spectator["sequence"], int) and spectator["sequence"] >= 1
    assert len(spectator["serverId"]) == 32
    assert len(spectator["citizens"]) == 39
    assert all(c["id"] != "player-001" for c in spectator["citizens"])
    assert all(set(c) == {"id", "x", "z"} for c in spectator["citizens"])
    assert set(spectator) == {"version", "serverId", "sequence", "gameMinutes", "citizens"}
    assert len(spectator["citizens"]) <= 256
    next_snapshot = get_json("/api/v1/spectator/scene")
    assert next_snapshot["serverId"] == spectator["serverId"]
    assert next_snapshot["sequence"] > spectator["sequence"]
    assert next_snapshot["gameMinutes"] >= spectator["gameMinutes"]
    assert spectator["gameMinutes"] >= 480

    # Private economic state is not served even to a local caller by default.
    try:
        get_json("/api/v1/world")
    except urllib.error.HTTPError as error:
        assert error.code == 404, "private state must not be exposed by default"
    else:
        raise AssertionError("full economic state is accessible without opt-in")

    request = urllib.request.Request(BASE + "/api/v1/spectator/scene", data=b"{}",
                                     method="POST", headers={"Content-Type": "application/json"})
    try:
        urllib.request.urlopen(request, timeout=3)
    except urllib.error.HTTPError as error:
        assert error.code == 405, "spectator mode must stay read-only"
    else:
        raise AssertionError("server unexpectedly accepted an unauthenticated write")

    # Explicit CORS allowlist, never wildcard.
    req = urllib.request.Request(BASE + "/api/v1/spectator/scene",
                                 headers={"Origin": "https://alexvelikiy88-sketch.github.io"})
    with urllib.request.urlopen(req, timeout=3) as response:
        assert response.headers.get("Access-Control-Allow-Origin") == (
            "https://alexvelikiy88-sketch.github.io"
        ), "expected origin must be allowed for the spectator API"
    req = urllib.request.Request(BASE + "/api/v1/spectator/scene",
                                 headers={"Origin": "https://untrusted.example"})
    with urllib.request.urlopen(req, timeout=3) as response:
        assert response.headers.get("Access-Control-Allow-Origin") is None, (
            "untrusted websites must not receive spectator CORS permission"
        )

    original_time = spectator["gameMinutes"]
    start_resident = spectator["citizens"][0]
    end = time.monotonic() + 4
    while time.monotonic() < end:
        time.sleep(.35)
        current = get_json("/api/v1/spectator/scene")
        resident = current["citizens"][0]
        if current["gameMinutes"] > original_time and (
            abs(resident["x"] - start_resident["x"]) > .04 or
            abs(resident["z"] - start_resident["z"]) > .04
        ):
            assert current["serverId"] == spectator["serverId"]
            assert current["sequence"] > next_snapshot["sequence"]
            print("PASS: public spectator API, readonly security, CORS and moving server NPCs")
            return
    raise AssertionError("server NPCs or world clock did not advance")

if __name__ == "__main__":
    run()
