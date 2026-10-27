using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR
{
    /// <summary>
    /// Two small waterfront residential facades assembled solely from the
    /// original author-modeled, CC0 Quaternius FBX prefab meshes already
    /// verified and downloaded by the existing hero-architecture pipeline.
    /// These occupy two currently empty parcels north of the promenade,
    /// never road/sidewalk lanes or a game POI. This is real interactive
    /// world geometry, not a render, overlay or another primitive building.
    /// </summary>
    internal static class MirWaterfrontResidenceArt
    {
        const string Source = "MIR/QuaterniusHero/";
        const string LitShader = "Nonation/Architecture/ArchitecturalLit";
        const float FrontZ = -106.25f;
        const float PavementY = .255f;

        static readonly Dictionary<string, Material> Palettes =
            new Dictionary<string, Material>();

        static Material Atlas(string name)
        {
            Material existing;
            if (Palettes.TryGetValue(name, out existing)) return existing;
            var shader = Resources.Load<Shader>(LitShader);
            var texture = Resources.Load<Texture2D>(Source + name);
            if (shader == null || !shader.isSupported || texture == null)
                throw new InvalidOperationException(
                    "WebGL lit shader or pinned Quaternius CC0 atlas missing: " + name);
            var material = new Material(shader);
            material.name = "NONATION waterfront Quaternius CC0 / " + name;
            material.SetColor("_Color", Color.white);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Metallic", .02f);
            material.SetFloat("_Smoothness", .21f);
            material.enableInstancing = true;
            Palettes.Add(name, material);
            return material;
        }

        static bool WorldBounds(GameObject instance, out Bounds bounds)
        {
            bounds = new Bounds();
            bool found = false;
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null ||
                    filter.sharedMesh.vertexCount < 100) continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found && bounds.size.x > .2f &&
                bounds.size.y > .3f && bounds.size.z > .2f;
        }

        static int RealFacade(Transform parent, string model,
            string atlas, float centerX, float maxWidth, float height)
        {
            var source = Resources.Load<GameObject>(Source + model);
            if (source == null)
                throw new InvalidOperationException("CC0 FBX prefab not imported: " + model);
            var instance = UnityEngine.Object.Instantiate(source, parent);
            instance.name = "Original CC0 FBX / " + model + " / " + atlas;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            Bounds bounds;
            if (!WorldBounds(instance, out bounds))
                throw new InvalidOperationException("No authored FBX MeshFilter: " + model);

            // Preserve architecture proportions and UVs, never stretch XYZ
            // independently or substitute a generated box for real facades.
            float scale = Mathf.Min(height / bounds.size.y,
                Mathf.Min(maxWidth / bounds.size.x, 8.5f / bounds.size.z));
            if (scale < .5f || scale > 15f)
                throw new InvalidOperationException(
                    "Quaternius model outside credible architectural dimensions: " + model);
            instance.transform.localScale *= scale;
            if (!WorldBounds(instance, out bounds))
                throw new InvalidOperationException("Invalid scaled FBX bounds: " + model);
            instance.transform.position += new Vector3(
                centerX - bounds.center.x,
                PavementY - bounds.min.y,
                FrontZ - bounds.min.z);

            var material = Atlas(atlas);
            int vertices = 0;
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                vertices += mesh.vertexCount;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                var materials = new Material[Math.Max(1, mesh.subMeshCount)];
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (vertices < 250)
                throw new InvalidOperationException("Real FBX is empty or too simple: " + model);

            // The authored FBX is visual only; one conservative, explicit
            // residential footprint per building is added after all four
            // imports succeed. Never introduce duplicate mesh colliders.
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.Destroy(collider);
            return vertices;
        }

        static void Footprint(Transform parent, string name,
            float x, float height)
        {
            var physical = new GameObject(name);
            physical.transform.SetParent(parent, false);
            var box = physical.AddComponent<BoxCollider>();
            box.center = new Vector3(x, PavementY + height * .5f, -103.5f);
            box.size = new Vector3(12f, height, 5.5f);
        }

        public static bool TryBuild(Transform parent)
        {
            if (parent == null) return false;
            GameObject group = null;
            try
            {
                // Fail before creating either residence if a resource is missing.
                foreach (var name in new[] { "3Story_Small", "2Story_Balcony", "2Story_Sign" })
                    if (Resources.Load<GameObject>(Source + name) == null)
                        throw new InvalidOperationException("Missing pinned FBX: " + name);
                foreach (var name in new[]
                    { "Texture_Grey", "Texture_Light", "Texture_Red", "Texture_Blue" })
                    if (Resources.Load<Texture2D>(Source + name) == null)
                        throw new InvalidOperationException("Missing pinned UV atlas: " + name);

                group = new GameObject("NONATION / Two real FBX waterfront residences");
                group.transform.SetParent(parent, false);

                // Both houses sit in existing non-POI lots (-26..-14 and
                // 14..26 X, -106.25..-100 Z); primary E-W streets are
                // at -120 and -80 Z. River promenade starts at -108 Z.
                int vertices = 0;
                vertices += RealFacade(group.transform, "3Story_Small",
                    "Texture_Grey", -23.1f, 5.7f, 9.5f);
                vertices += RealFacade(group.transform, "3Story_Small",
                    "Texture_Light", -16.9f, 5.7f, 9.5f);
                vertices += RealFacade(group.transform, "2Story_Balcony",
                    "Texture_Red", 16.9f, 5.7f, 7.0f);
                vertices += RealFacade(group.transform, "2Story_Sign",
                    "Texture_Blue", 23.1f, 5.7f, 7.0f);
                if (vertices < 1000)
                    throw new InvalidOperationException("Four real architectural modules incomplete");

                Footprint(group.transform, "West residence / collision", -20f, 9.5f);
                Footprint(group.transform, "East residence / collision", 20f, 7.0f);
                Debug.Log("NONATION_WATERFRONT_RESIDENCES_READY buildings=2 " +
                    "fbxInstances=4 colliders=2 vertices=" + vertices +
                    " source=Quaternius-2021-CC0");
                return true;
            }
            catch (Exception error)
            {
                if (group != null) UnityEngine.Object.Destroy(group);
                Debug.LogWarning("NONATION_WATERFRONT_RESIDENCES_ERROR: " + error);
                return false;
            }
        }
    }
}
