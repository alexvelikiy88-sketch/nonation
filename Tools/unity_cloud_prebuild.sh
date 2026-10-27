#!/usr/bin/env sh
set -eu
# Unity Cloud Build Automation pre-build hook. Validate and import pinned
# CC0 source assets before Unity Editor refresh / glTFast scripted import.
# Neither the old scene nor its game logic is removed if the visuals fail.
python3 Tools/fetch_quaternius.py
python3 Tools/fetch_kenney_city.py
# Four genuine, author-published CC0 textured FBX buildings, exact digest pinned.
python3 Tools/fetch_quaternius_hero.py
python3 Tools/fetch_quaternius_hero.py --verify-only
# Pack verified original 2025 CC0 glTF leaves/flowers into WebGL StreamingAssets.
python3 Tools/fetch_hero_nature.py
python3 Tools/fetch_hero_nature.py --verify-only
