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

    world = get_json("/api/v1/world")
    assert world.get("version") == 1, "unexpected world protocol"
    assert len(world.get("citizens", [])) == 40, "missing city population"
    assert len(world.get("businesses", [])) >= 8, "businesses not serialized"
    assert world.get("gameMinutes", 0) >= 480, "invalid simulated clock"
    # This service is read-only until authoritative authenticated commands exist.
    req = urllib.request.Request(BASE + "/api/v1/world", data=b"{}",
                                 method="POST", headers={"Content-Type": "application/json"})
    try:
        urllib.request.urlopen(req, timeout=3)
    except urllib.error.HTTPError as error:
        assert error.code == 405, "write endpoint must be unavailable"
    else:
        raise AssertionError("unauthenticated world POST unexpectedly accepted")

    original_time = world["gameMinutes"]
    end = time.monotonic() + 4
    while time.monotonic() < end:
        time.sleep(.35)
        if get_json("/api/v1/world")["gameMinutes"] > original_time:
            print("PASS: headless server running, readonly API and live simulation tick")
            return
    raise AssertionError("server world clock did not advance")

if __name__ == "__main__":
    run()
