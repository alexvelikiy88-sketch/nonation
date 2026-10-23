#!/usr/bin/env sh
set -eu
# Configure this as Unity Cloud Build Automation's pre-build script.
# It supplies CC0 animations to the Editor import without committing large FBX assets.
python3 Tools/fetch_quaternius.py
