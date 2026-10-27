# CHAT 05 — Waterfront real FBX residential frontage (27.10.26)

## Implementation (unverified Unity)

This visual-only addition does not touch MirCity simulation, player home IDs,
economic POIs, routes, save files, scenes, packages, or Unity Cloud workflows.

- Two compact waterfront residential silhouettes at X=-20 and X=20, facade Z=-106.25.
- Four actual FBX instances: Quaternius `3Story_Small` twice;
  `2Story_Balcony` and `2Story_Sign` once each.
- Four original UV color atlases, shader `NONATION/ArchitecturalLit`,
  uniform-only scaling, shadows, two footprint BoxColliders.
- Atomic building creation: if pre-authored FBX, atlas or shader fails, the
  optional residence group is destroyed and an explicit warning logged;
  existing downtown hero buildings and economic locations are retained.
- New proof marker `NONATION_WATERFRONT_RESIDENCES_READY buildings=2 fbxInstances=4 colliders=2 vertices=...`.

## License provenance

No additional third-party bytes are added. This module reuses exactly the
pinned Quaternius source files from
`Documentation/REAL_CC0_HERO_BUILDINGS_RU.md` and
`Tools/quaternius_hero_manifest.json`.
Original publisher: Quaternius at OpenGameArt, *LowPoly Buildings Pack*,
18.06.2021, CC0 1.0.
Original ZIP SHA256:
`ff876502f53f6d4c35104378e8322ec8253fd0ee45c8ec28d9dc81df54b80e85`.
The existing hash-gated fetch and WebGL build gate remain unchanged.

## Verification status

- Source changes in branch; **NOT YET a verified Unity runtime result**.
- Existing successful Chromium 1280x720 baseline screenshot comes from
  [Actions #40521592293](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40521592293);
  it predates these residences, and does not prove this code works.
- Unity Cloud #19 remains reserved for the previous main SHA
  `5a262820eeff588f70fa5ffaa9216c495f61f8b4`; no new paid build requested.
- Source CI / Integration Guard and Unity compiler must be checked on this PR.
- After a conflict-free manager integration and source checks, make one
  deliberately authorized Unity build and capture unedited V/B 1280x720
  frames; compare with the source visual canon; measure frame times/tris.
- The live game still lacks demonstrated 3–6 rigged adults in WebGL.
  This contribution does **not** close VIS-03, VIS-06 or VIS-07.

## Integration and ownership

Changed `Assets/MIR/Art/QuaterniusHero/**` only (Graphics-owned domain).
Proposed by CHAT 05 for explicit manager review.
Do not merge independently into `main` or rewrite `TASKS.md`.
Avoid blindly merging the open draft Graphics PR #26.
