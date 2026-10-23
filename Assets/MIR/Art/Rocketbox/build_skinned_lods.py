#!/usr/bin/env python3
"""Generate *real* skinned LOD1-3 FBX meshes from the pinned Microsoft Rocketbox pilot.

Run in Blender (not ordinary Python):
  blender --background --factory-startup --python \
    Assets/MIR/Art/Rocketbox/build_skinned_lods.py -- \
    --input Assets/MIR/Art/Rocketbox/Male_Adult_12.fbx \
    --output Assets/MIR/Art/Rocketbox/GeneratedLODs

No prefab, scene, Unity importer, Packages, or Resources are modified.
The derived meshes remain MIT-licensed; retain the original Rocketbox LICENSE.md.
Only a successful Blender import/export/reimport and later Unity QA can establish
that these are usable, animated, skin-preserving character LODs.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import sys
import tempfile

SOURCE_BLOB_SHA1 = "15d4f8501d5703600cb3d8a94765a3de1029aaf6"
SOURCE_NAME = "Male_Adult_12.fbx"
TEXTURE_NAMES = tuple(
    f"m007_{region}_{channel}.tga"
    for region in ("body", "head")
    for channel in ("color", "normal", "specular")
)
DEFAULT_RATIOS = (0.72, 0.48, 0.28)


def git_blob_sha1(data: bytes) -> str:
    return hashlib.sha1(f"blob {len(data)}\0".encode("ascii") + data).hexdigest()


def validate_ratios(values: tuple[float, ...]) -> tuple[float, float, float]:
    if len(values) != 3 or not all(0.10 <= x <= 0.90 for x in values):
        raise ValueError("Exactly three LOD ratios in [0.10, 0.90] required")
    if not (values[0] > values[1] > values[2]):
        raise ValueError("LOD ratios must strictly decrease")
    return values


def verify_source(folder: Path, file: Path) -> dict:
    if file.name != SOURCE_NAME or not file.is_file():
        raise ValueError("Unexpected or missing original Rocketbox source FBX")
    data = file.read_bytes()
    if git_blob_sha1(data) != SOURCE_BLOB_SHA1:
        raise ValueError("Original FBX does not match immutable Microsoft Git blob")
    license_file = folder / "LICENSE.md"
    if not license_file.is_file() or "MIT License" not in license_file.read_text(encoding="utf-8"):
        raise ValueError("Required original Microsoft MIT license missing")
    manifest_file = folder / "Textures" / "textures_manifest.json"
    if not manifest_file.is_file():
        raise ValueError("Verified source textures manifest missing")
    manifest = json.loads(manifest_file.read_text(encoding="utf-8"))
    if set(manifest.get("textures", {})) != set(TEXTURE_NAMES):
        raise ValueError("All six textures are required")
    if manifest.get("upstream_commit") != "0943055db6ec570bcef9f2c8b41c9e5467c808f9":
        raise ValueError("Unexpected Rocketbox texture upstream revision")
    for name in TEXTURE_NAMES:
        tex = folder / "Textures" / name
        if not tex.is_file():
            raise ValueError(f"Missing texture {name}")
        meta = manifest["textures"][name]
        raw = tex.read_bytes()
        if len(raw) != meta["bytes"] or hashlib.sha256(raw).hexdigest() != meta["sha256"]:
            raise ValueError(f"Texture bytes/checksum mismatch: {name}")
        if meta["width"] != 1024 or meta["height"] != 1024:
            raise ValueError(f"Texture {name} not the verified 1024-pixel version")
    return {"source_blob_sha1": SOURCE_BLOB_SHA1, "texture_manifest_sha256":
            hashlib.sha256(manifest_file.read_bytes()).hexdigest()}


def triangles(mesh) -> int:
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def mesh_audit(mesh_obj, armature, *, required_materials: int = 2) -> dict:
    if mesh_obj.type != "MESH" or armature.type != "ARMATURE":
        raise ValueError("Expected one skinned mesh and one armature")
    mesh = mesh_obj.data
    if mesh_obj.find_armature() != armature:
        raise ValueError(f"Mesh {mesh_obj.name} is not bound to the selected armature")
    if not mesh_obj.vertex_groups:
        raise ValueError("Skin vertex groups missing")
    if not mesh.uv_layers:
        raise ValueError("UV map missing")
    if len(mesh.materials) != required_materials:
        raise ValueError("Material slot count changed")
    if mesh.shape_keys and len(mesh.shape_keys.key_blocks) > 1:
        raise ValueError("Facial blendshapes require a separate validated pipeline")
    bone_names = {b.name for b in armature.data.bones}
    groups = {g.index: g.name for g in mesh_obj.vertex_groups}
    weighted = 0
    for vertex in mesh.vertices:
        weights = [group.weight for group in vertex.groups
                   if group.weight > 1e-5 and groups.get(group.group) in bone_names]
        if weights and sum(weights) >= 0.25:
            weighted += 1
    if len(mesh.vertices) == 0 or weighted != len(mesh.vertices):
        raise ValueError(f"Unskinned/weakly weighted vertices: {weighted}/{len(mesh.vertices)}")
    tri_count = triangles(mesh)
    if tri_count < 50 or len(mesh.vertices) < 100:
        raise ValueError("Unexpectedly empty/small mesh")
    if not all(0 <= polygon.material_index < required_materials for polygon in mesh.polygons):
        raise ValueError("Invalid polygon material slot")
    return {"vertices": len(mesh.vertices), "triangles": tri_count,
            "weighted_vertices": weighted, "bone_count": len(armature.data.bones),
            "materials": len(mesh.materials), "uv_channels": len(mesh.uv_layers)}


def choose_pilot_mesh(bpy):
    rigs = [obj for obj in bpy.data.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    if len(rigs) != 1 or len(meshes) != 1:
        raise ValueError(f"Rocketbox input must contain 1 armature and 1 mesh; "
                         f"found {len(rigs)} armatures, {len(meshes)} meshes")
    return meshes[0], rigs[0]


def select_only(bpy, *objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[-1]


def run_blender(source_file: Path, out_folder: Path, ratios: tuple[float, ...]) -> dict:
    try:
        import bpy
    except ImportError as exc:
        raise RuntimeError("Use the Blender background executable, not CPython") from exc
    if tuple(bpy.app.version) < (3, 6, 0):
        raise RuntimeError("Blender 3.6+ is required")
    ratios = validate_ratios(ratios)
    source_file = source_file.resolve()
    if not source_file.is_file():
        raise FileNotFoundError(source_file)
    provenance = verify_source(source_file.parent, source_file)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source_file), use_custom_normals=True)
    source_mesh, skeleton = choose_pilot_mesh(bpy)
    source_audit = mesh_audit(source_mesh, skeleton)
    output = {"source": str(source_file.name), **provenance,
              "blender_version": bpy.app.version_string, "LOD0": source_audit,
              "generated": {}}
    prev_tris = source_audit["triangles"]

    out_folder.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix=".rocketbox-lod-", dir=out_folder.parent) as temp_name:
        staging = Path(temp_name)
        for number, ratio in enumerate(ratios, start=1):
            lod = source_mesh.copy()
            lod.data = source_mesh.data.copy()
            lod.name = f"Male_Adult_12_LOD{number}"
            bpy.context.scene.collection.objects.link(lod)
            select_only(bpy, lod)
            modifier = lod.modifiers.new("NONATION_Decimate", "DECIMATE")
            modifier.decimate_type = "COLLAPSE"
            modifier.ratio = ratio
            modifier.use_collapse_triangulate = True
            bpy.ops.object.modifier_move_to_index(modifier=modifier.name, index=0)
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            stats = mesh_audit(lod, skeleton)
            if not stats["triangles"] < prev_tris:
                raise ValueError(f"LOD{number} failed to reduce triangles")
            if stats["bone_count"] != source_audit["bone_count"]:
                raise ValueError("Skeleton changed during decimation")
            prev_tris = stats["triangles"]
            name = f"Male_Adult_12_LOD{number}.fbx"
            # Export one skinned mesh plus the *original* armature, with no baked
            # animation. Unity AGENT 01 must retarget and validate the imported rig.
            select_only(bpy, skeleton, lod)
            file = staging / name
            bpy.ops.export_scene.fbx(
                filepath=str(file), use_selection=True,
                object_types={"ARMATURE", "MESH"}, add_leaf_bones=False,
                use_armature_deform_only=False, bake_anim=False,
                use_mesh_modifiers=True, path_mode="STRIP",
                embed_textures=False, mesh_smooth_type="FACE",
                axis_forward="-Z", axis_up="Y")
            if not file.is_file() or file.stat().st_size < 1000:
                raise ValueError(f"LOD{number} export missing/empty")
            stats["source_ratio"] = ratio
            stats["output_sha256"] = hashlib.sha256(file.read_bytes()).hexdigest()
            stats["bytes"] = file.stat().st_size
            output["generated"][name] = stats
            bpy.data.objects.remove(lod, do_unlink=True)

        # Re-import every exported FBX to detect broken bindings before publishing.
        for name, stats in output["generated"].items():
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.fbx(filepath=str(staging / name))
            loaded, rig = choose_pilot_mesh(bpy)
            loaded_stats = mesh_audit(loaded, rig)
            if loaded_stats["triangles"] != stats["triangles"]:
                raise ValueError(f"Reimport triangle mismatch in {name}")
            if loaded_stats["bone_count"] != source_audit["bone_count"]:
                raise ValueError(f"Reimport bone mismatch in {name}")
            stats["reimport_pass"] = True

        (staging / "lod_manifest.json").write_text(
            json.dumps(output, ensure_ascii=False, sort_keys=True, indent=2) + "\n",
            encoding="utf-8"
        )
        out_folder.mkdir(parents=True, exist_ok=True)
        for name in (*output["generated"].keys(), "lod_manifest.json"):
            shutil.move(str(staging / name), str(out_folder / name))
    return output


def main() -> int:
    arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    cli = argparse.ArgumentParser(description=__doc__)
    base = Path(__file__).resolve().parent
    cli.add_argument("--input", type=Path, default=base / SOURCE_NAME)
    cli.add_argument("--output", type=Path, default=base / "GeneratedLODs")
    cli.add_argument("--ratios", type=float, nargs=3, default=DEFAULT_RATIOS)
    opts = cli.parse_args(arguments)
    output = run_blender(opts.input, opts.output, tuple(opts.ratios))
    print("LOD_EXPORT_VALIDATED:", json.dumps(output, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (RuntimeError, ValueError, OSError) as error:
        print("LOD_EXPORT_BLOCKED:", error, file=sys.stderr)
        sys.exit(1)
