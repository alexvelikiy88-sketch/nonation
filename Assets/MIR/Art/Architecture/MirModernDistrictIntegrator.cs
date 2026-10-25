using System;
using UnityEngine;

namespace MIR
{
    /// <summary>
    /// Atomic art-only replacement of 12 legacy facades with architecturally
    /// detailed, batched meshes. Keeps every original gameplay BoxCollider,
    /// home/business identifier, NPC route and vehicle untouched.
    /// In the event of any material/mesh failure, Kenney remains available
    /// as a playable technical fallback; no two building sources overlap.
    /// </summary>
    internal static class MirModernDistrictIntegrator
    {
        const int BuildingCount = 12;
        static readonly string[] Commercial =
        {
            "Grocery store", "Corner café", "Auto workshop", "Town hall"
        };

        public static bool TryActivate(Transform city)
        {
            if (city == null) return false;
            GameObject holder = null;
            try
            {
                var placeholders = new Transform[BuildingCount];
                for (int i = 0; i < BuildingCount; i++)
                {
                    string label = i < Commercial.Length
                        ? Commercial[i] : "Residential block " + (i - Commercial.Length);
                    var legacy = city.Find("Legacy building · " + label);
                    if (legacy == null)
                        throw new InvalidOperationException(
                            "Missing original physical footprint: " + label);
                    placeholders[i] = legacy;
                }

                var shader = Resources.Load<Shader>(
                    "Nonation/Architecture/ArchitecturalLit");
                if (shader == null || !shader.isSupported)
                    throw new InvalidOperationException(
                        "NONATION Built-in architectural shader missing or unsupported");

                holder = new GameObject("NONATION / Twelve Modern Facades");
                holder.transform.SetParent(city, false);

                // The existing legacy BoxColliders must remain authoritative.
                MirArchitectureQuarter.Build(holder.transform, false);

                int meshes = 0, vertices = 0;
                foreach (var filter in holder.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null || filter.sharedMesh == null) continue;
                    if (filter.sharedMesh.vertexCount <= 0) continue;
                    vertices += filter.sharedMesh.vertexCount;
                    meshes++;
                }
                int renderers = 0;
                foreach (var renderer in holder.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer != null && renderer.enabled) renderers++;

                // Geometry, shader and all 12 legacy footprints must be present
                // BEFORE rendering any of the old facades is disabled.
                if (meshes < 5 || vertices < 1000 || renderers < 5)
                    throw new InvalidOperationException(
                        "Insufficient live modular geometry: meshes=" + meshes +
                        " vertices=" + vertices + " renderers=" + renderers);

                foreach (var placeholder in placeholders)
                {
                    foreach (var renderer in placeholder.GetComponentsInChildren<Renderer>(true))
                        if (renderer != null) renderer.enabled = false;
                }

                Debug.Log("NONATION_MODULAR_CITY_READY buildings=12 meshes=" +
                    meshes + " vertices=" + vertices +
                    " (3D articulated facades, original gameplay colliders retained)");
                return true;
            }
            catch (Exception error)
            {
                if (holder != null) UnityEngine.Object.Destroy(holder);
                Debug.LogWarning("NONATION_MODULAR_CITY_ERROR: " + error);
                return false;
            }
        }
    }
}
