using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Materials;
using GLTFast.Logging;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR
{
    /// <summary>
    /// Replaces the visible diagnostic blockout with real, free Kenney CC0 meshes.
    /// Original colliders, POIs, NPC routes and economic state stay untouched.
    /// Assets are pinned to a public immutable Git commit, with a build-time
    /// vendoring option; missing models leave the original scene playable.
    /// This is a playable art integration, NOT completion of VISUAL GATE.
    /// </summary>
    public sealed class MirKenneyCityArt : MonoBehaviour
    {
        const string ResourcesPrefix = "MIR/Kenney/";
        const string PinnedBase =
            "https://raw.githubusercontent.com/Hidencod/tge-assets/" +
            "7be9b573bc74f32ed766b44b3f46550d02ca83b5/packs/";

        sealed class Slot
        {
            public readonly string Name, Model;
            public readonly Vector3 Position, Size;
            public Slot(string name, string model, float x, float z, float width, float height, float depth)
            {
                Name = name; Model = model; Position = new Vector3(x, 0, z);
                Size = new Vector3(width, height, depth);
            }
        }

        static readonly Slot[] BusinessBuildings =
        {
            new Slot("Grocery store", "city-kit-commercial/building-e", 20, -20, 13, 7, 14),
            new Slot("Corner café", "city-kit-commercial/building-a", 56, 20, 12, 7, 12),
            new Slot("Auto workshop", "city-kit-commercial/building-d", -58, -22, 15, 7, 15),
            new Slot("Town hall", "city-kit-commercial/building-m", -20, -20, 14, 10.5f, 14)
        };

        readonly Dictionary<string, GltfImport> imported = new Dictionary<string, GltfImport>();
        readonly HashSet<string> failed = new HashSet<string>();
        bool stopping;

        static bool BuiltInShaderReferencesReady()
        {
            // Unity strips shaders used only through glTFast Shader.Find. The
            // Editor build hook retains three actual material references in
            // Resources; check them before loading any GLB.
            string[] keepAlive =
            {
                "KenneyPbrMetallicRoughness",
                "KenneyPbrSpecularGlossiness",
                "KenneyUnlit"
            };
            foreach (var name in keepAlive)
            {
                var path = "MIR/GltfRuntime/" + name;
                var material = Resources.Load<Material>(path);
                if (material == null || material.shader == null || !material.shader.isSupported)
                {
                    Debug.LogWarning("NONATION_KENNEY_SHADER_MISSING: " + path +
                        " (missing or unsupported WebGL glTF shader material)");
                    return false;
                }
            }
            return true;
        }

        async void Start()
        {
            try
            {
                if (GraphicsSettings.currentRenderPipeline == null &&
                    !BuiltInShaderReferencesReady())
                {
                    Debug.LogWarning("NONATION_KENNEY_CITY_ERROR: Built-in glTF shaders " +
                        "were stripped or not included in Resources.");
                    return; // Retain old geometry as a visible diagnostic fallback.
                }

                int buildings = 0, trees = 0, cars = 0, objects = 0;
                foreach (var building in BusinessBuildings)
                    if (await ReplaceBuilding(building)) buildings++;

                // Preserve the exact original spatial layout and business entry positions.
                string[] residentialModels =
                {
                    "city-kit-commercial/building-b",
                    "city-kit-commercial/building-h",
                    "city-kit-commercial/building-k",
                    "city-kit-commercial/building-n"
                };
                for (int i = 0; i < 8 && !stopping; i++)
                {
                    float x = -66 + (i % 4) * 36;
                    float z = 56 + (i / 4) * 38;
                    var slot = new Slot(
                        "Residential block " + i, residentialModels[i % residentialModels.Length],
                        x, z, 14, (3 + i % 3) * 3.5f, 13);
                    if (await ReplaceBuilding(slot)) buildings++;
                }

                // Replace the capsule-shaped tree canopies only after a real mesh loads.
                int treeIndex = 0;
                for (int i = -2; i <= 2 && !stopping; i++)
                for (int j = -2; j <= 2 && !stopping; j++)
                {
                    if ((i + j) % 2 != 0) continue;
                    var pos = new Vector3(i * 40 + 13, .2f, j * 40 + 13);
                    string model = (i + 2 * j) % 3 == 0 ? "nature-kit/tree-detailed" : "nature-kit/tree-oak";
                    var original = transform.parent.Find("Legacy Tree " + treeIndex++);
                    if (await Place(model, "Kenney tree", pos, new Vector3(3.4f, 5.8f, 3.4f), original))
                        trees++;
                }

                // The existing kinematic traffic and economy keep controlling each vehicle.
                if (MirGame.Instance != null && MirGame.Instance.City != null)
                {
                    var vehicles = MirGame.Instance.City.Vehicles;
                    string[] carModels = { "car-kit/sedan", "car-kit/taxi", "car-kit/delivery" };
                    for (int i = 0; i < vehicles.Count && i < carModels.Length && !stopping; i++)
                        if (vehicles[i] != null && await ReplaceVehicle(vehicles[i], carModels[i])) cars++;
                }

                // A few authored city-kit details supplement the existing road layout.
                if (await Place("city-kit-roads/traffic-light", "Kenney signal",
                    new Vector3(35, .25f, -35), new Vector3(.6f, 4.5f, .6f), null)) objects++;
                if (await Place("city-kit-roads/traffic-light", "Kenney signal",
                    new Vector3(35, .25f, 35), new Vector3(.6f, 4.5f, .6f), null)) objects++;

                if (!stopping)
                    Debug.Log("NONATION_KENNEY_CITY_READY buildings=" + buildings +
                        " trees=" + trees + " vehicles=" + cars + " props=" + objects +
                        " (true glTF meshes; visual acceptance remains pending)");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("NONATION_KENNEY_CITY_ERROR: " + ex.Message +
                    "; original playable city geometry preserved.");
            }
        }

        async Task<bool> ReplaceBuilding(Slot slot)
        {
            if (stopping) return false;
            var old = transform.parent.Find("Legacy building · " + slot.Name);
            return await Place(slot.Model, slot.Name, slot.Position, slot.Size, old);
        }

        async Task<bool> ReplaceVehicle(MirVehicle car, string model)
        {
            var holder = new GameObject("Kenney vehicle · " + model);
            holder.transform.SetParent(car.transform, false);
            var instance = await InstantiateModel(model, holder.transform);
            if (instance == null || stopping)
            {
                Destroy(holder);
                return false;
            }

            // Place the imported mesh over the already functioning kinematic car.
            // The original Collider, Rigidbody, route planner and fuel system stay.
            if (!Fit(instance, car.transform.position, new Vector3(2.15f, 1.9f,
                car.IsTruck ? 7.0f : 4.35f)))
            {
                Destroy(holder);
                return false;
            }
            foreach (var r in car.GetComponentsInChildren<MeshRenderer>(true))
                if (r != null && !r.transform.IsChildOf(holder.transform))
                    r.enabled = false;
            return true;
        }

        async Task<bool> Place(string model, string label, Vector3 position,
            Vector3 size, Transform old)
        {
            if (stopping) return false;
            var holder = new GameObject("Kenney CC0 · " + label);
            holder.transform.SetParent(transform, false);
            var instance = await InstantiateModel(model, holder.transform);
            if (instance == null || stopping || !Fit(instance, position, size))
            {
                Destroy(holder);
                return false;
            }
            // Preserve legacy colliders and POIs; replace only visible placeholder art.
            if (old != null)
                foreach (var renderer in old.GetComponentsInChildren<Renderer>(true))
                    if (renderer != null) renderer.enabled = false;
            return true;
        }

        async Task<GameObject> InstantiateModel(string path, Transform holder)
        {
            if (stopping || failed.Contains(path)) return null;
            var local = Resources.Load<GameObject>(ResourcesPrefix + path);
            if (local != null)
            {
                var instance = Instantiate(local, holder, false);
                instance.name = "Model " + path;
                return instance;
            }

            // Fallback when Cloud pre-build did not bundle the pinned GLB files.
            // raw.githubusercontent.com explicitly supports browser CORS.
            GltfImport gltf;
            if (!imported.TryGetValue(path, out gltf))
            {
                // This release uses Built-in rendering (URP is installed as a
                // package but no SRP asset is assigned in project settings).
                // glTFast's implicit MaterialGenerator discovery fails in
                // IL2CPP/WebGL for this setup; inject the Built-in converter.
                // Once a Scriptable Render Pipeline asset is configured, use
                // its corresponding glTFast converter instead.
                IMaterialGenerator materialGenerator =
                    GraphicsSettings.currentRenderPipeline == null
                    ? (IMaterialGenerator)new BuiltInMaterialGenerator()
                    : MaterialGenerator.GetDefaultMaterialGenerator();
                gltf = new GltfImport(materialGenerator: materialGenerator, logger: new ConsoleLogger());
                bool ok;
                try { ok = await gltf.Load(PinnedBase + path + ".glb"); }
                catch (Exception ex)
                {
                    Debug.LogWarning("NONATION_KENNEY_FETCH_FAILED: " + path + " " + ex);
                    ok = false;
                }
                if (!ok || stopping)
                {
                    gltf.Dispose();
                    failed.Add(path);
                    return null;
                }
                imported[path] = gltf;
            }
            var root = new GameObject("Model " + path);
            root.transform.SetParent(holder, false);
            bool created;
            try { created = await gltf.InstantiateMainSceneAsync(root.transform); }
            catch (Exception ex)
            {
                Debug.LogWarning("NONATION_KENNEY_IMPORT_FAILED: " + path + " " + ex);
                created = false;
            }
            if (!created || stopping)
            {
                if (!stopping)
                    Debug.LogWarning("NONATION_KENNEY_IMPORT_FAILED: " + path +
                        " InstantiateMainSceneAsync returned false.");
                Destroy(root);
                return null;
            }
            return root;
        }

        static bool Fit(GameObject instance, Vector3 groundCenter, Vector3 target)
        {
            Bounds bounds;
            if (!TryBounds(instance, out bounds) ||
                bounds.size.x < .005f || bounds.size.y < .005f || bounds.size.z < .005f)
                return false;
            // Kenney models use stylized units; axis-wise normalization makes
            // them match the existing gameplay footprints without moving POIs.
            float sx = Mathf.Clamp(.94f * target.x / bounds.size.x, .01f, 100f);
            float sy = Mathf.Clamp(.94f * target.y / bounds.size.y, .01f, 100f);
            float sz = Mathf.Clamp(.94f * target.z / bounds.size.z, .01f, 100f);
            instance.transform.localScale = Vector3.Scale(
                instance.transform.localScale, new Vector3(sx, sy, sz));
            if (!TryBounds(instance, out bounds)) return false;
            instance.transform.position += new Vector3(
                groundCenter.x - bounds.center.x,
                groundCenter.y - bounds.min.y,
                groundCenter.z - bounds.center.z);
            return true;
        }

        static bool TryBounds(GameObject target, out Bounds bounds)
        {
            bounds = default(Bounds);
            bool found = false;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        void OnDestroy()
        {
            stopping = true;
            foreach (var item in imported.Values)
                item.Dispose();
            imported.Clear();
        }
    }
}
