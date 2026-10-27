using System;
using System.Collections.Generic;
using UnityEngine;

namespace MIR
{
    /// <summary>
    /// True author-modeled CC0 FBX facades from Quaternius, not generated cubes.
    /// Only the visual source is replaced: all original NONATION gameplay
    /// colliders, routes, home IDs and commercial POIs remain authoritative.
    /// The selection is atomic: never let half an art pack cover legacy meshes.
    /// </summary>
    internal static class MirQuaterniusHeroBuildings
    {
        const string Prefix = "MIR/QuaterniusHero/";
        static readonly Dictionary<string, Material> Materials =
            new Dictionary<string, Material>();

        static Material Palette(string atlas)
        {
            Material cached;
            if (Materials.TryGetValue(atlas, out cached)) return cached;
            var texture = Resources.Load<Texture2D>(Prefix + atlas);
            var shader = Resources.Load<Shader>(
                "Nonation/Architecture/ArchitecturalLit");
            if (texture == null || shader == null || !shader.isSupported)
                throw new InvalidOperationException(
                    "Missing CC0 atlas or supported lit shader: " + atlas);

            var material = new Material(shader);
            material.name = "NONATION / Quaternius CC0 " + atlas;
            material.SetColor("_Color", Color.white);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Metallic", .01f);
            material.SetFloat("_Smoothness", .22f);
            material.enableInstancing = true;
            Materials.Add(atlas, material);
            return material;
        }

        static bool BoundsOf(GameObject model, out Bounds result)
        {
            result = new Bounds();
            bool found = false;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null ||
                    filter.sharedMesh.vertexCount < 100)
                    continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled) continue;
                if (!found) { result = renderer.bounds; found = true; }
                else result.Encapsulate(renderer.bounds);
            }
            return found && result.size.x > .5f && result.size.y > .5f &&
                result.size.z > .5f;
        }

        static int Add(GameObject parent, string modelName, string atlas,
            float x, float z, float lotWidth, float lotDepth,
            float intendedHeight)
        {
            var original = Resources.Load<GameObject>(Prefix + modelName);
            if (original == null)
                throw new InvalidOperationException("Real CC0 FBX missing: " + modelName);
            var imported = UnityEngine.Object.Instantiate(original, parent.transform);
            imported.name = "CC0 Quaternius / " + modelName;
            imported.transform.localPosition = Vector3.zero;
            imported.transform.localRotation = Quaternion.identity;

            Bounds bounds;
            if (!BoundsOf(imported, out bounds))
                throw new InvalidOperationException(
                    "Source FBX has no valid real 3D mesh: " + modelName);
            // One UNIFORM scale for all axes preserves shop-door and window
            // proportions. Existing collision footprint never changes.
            float uniform = Mathf.Min(intendedHeight / bounds.size.y,
                Mathf.Min(lotWidth / bounds.size.x, lotDepth / bounds.size.z));
            if (uniform < .5f || uniform > 15f)
                throw new InvalidOperationException(
                    "CC0 model proportions incompatible with lot: " + modelName);
            imported.transform.localScale *= uniform;
            if (!BoundsOf(imported, out bounds))
                throw new InvalidOperationException("CC0 FBX became invalid: " + modelName);

            // Source author FBX faces the local -Z front. Align real doors to
            // original gameplay entry side (south) without moving business IDs.
            imported.transform.position += new Vector3(
                x - bounds.center.x,
                .012f - bounds.min.y,
                z - lotDepth * .5f - bounds.min.z);

            var material = Palette(atlas);
            int vertices = 0;
            foreach (var filter in imported.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                vertices += mesh.vertexCount;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                int count = Math.Max(1, mesh.subMeshCount);
                var materials = new Material[count];
                for (int i = 0; i < count; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (vertices < 250)
                throw new InvalidOperationException(
                    "Refusing fake architecture with too few vertices: " + modelName);
            // Only ORIGINAL legacy gameplay BoxColliders remain authoritative.
            foreach (var collision in imported.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.Destroy(collision);
            return vertices;
        }

        /// <summary>
        /// True visual replacement of grocery, cafe and civic hall (3 of 12
        /// existing gameplay slots). Others remain modular. No replacement
        /// occurs unless every source model and atlas successfully imports.
        /// </summary>
        internal static bool TryBuild(Transform parent)
        {
            if (parent == null) return false;
            GameObject group = null;
            try
            {
                group = new GameObject("NONATION / Real CC0 textured hero buildings");
                group.transform.SetParent(parent, false);
                int vertices = 0;
                int count = 0;
                vertices += Add(group, "2Story_Wide_2Doors", "Texture_Light",
                    20f, -20f, 12.7f, 13.5f, 6.7f);
                count++;
                // Two individually modeled, signed/balcony cafe shopfronts,
                // never a stretched single cube or photograph billboard.
                vertices += Add(group, "2Story_Sign", "Texture_Red",
                    53.4f, 20f, 5.45f, 11.2f, 7.0f);
                count++;
                vertices += Add(group, "2Story_Balcony", "Texture_Blue",
                    58.65f, 20f, 5.45f, 11.2f, 7.0f);
                count++;
                // A two-part town hall with contrasting stone facade.
                vertices += Add(group, "3Story_Small", "Texture_Grey",
                    -23.2f, -20f, 6.1f, 13.0f, 10.4f);
                count++;
                vertices += Add(group, "3Story_Small", "Texture_Light",
                    -16.9f, -20f, 6.1f, 13.0f, 10.4f);
                count++;

                if (count != 5 || vertices < 1200)
                    throw new InvalidOperationException(
                        "Incomplete real CC0 FBX hero quarter");
                Debug.Log("NONATION_QUATERNIUS_HERO_READY models=5 slots=3 " +
                    "atlas=4 vertices=" + vertices +
                    " (licensed FBX game meshes, no primitive facades)");
                return true;
            }
            catch (Exception error)
            {
                if (group != null) UnityEngine.Object.Destroy(group);
                Debug.LogWarning("NONATION_QUATERNIUS_HERO_ERROR: " + error);
                return false;
            }
        }
    }
}
