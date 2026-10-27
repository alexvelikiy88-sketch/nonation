using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Logging;
using GLTFast.Materials;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR
{
    /// <summary>
    /// Real CC0 pre-authored Stylized Nature MegaKit scenes: trees, flowering
    /// shrubs, real petal and grass meshes with original glTF PBR textures/UVs.
    /// Six source glTFs are byte-verified and repackaged losslessly by the
    /// Unity Cloud/GitHub prebuild into StreamingAssets/NONATION/Nature/*.glb.
    /// All decorative, no new colliders/POIs; simulation and routes untouched.
    /// </summary>
    public sealed class MirPremiumNature : MonoBehaviour
    {
        const string Prefix = "/NONATION/Nature/";
        readonly Dictionary<string, GltfImport> imports = new Dictionary<string, GltfImport>();
        bool stopping;

        static readonly Vector3[] TreeA =
        {
            new Vector3(4f, .25f, 31f),
            new Vector3(35f, .25f, 38f),
            new Vector3(22f, .26f, -124f)
        };
        static readonly Vector3[] TreeB =
        {
            new Vector3(44f, .26f, 7f),
            new Vector3(8f, .26f, -41f)
        };
        static readonly Vector3[] Twisted =
        {
            new Vector3(41f, .26f, -37f),
            new Vector3(66f, .26f, 35f)
        };
        static readonly Vector3[] Bushes =
        {
            new Vector3(5.5f,.35f,17f),new Vector3(5.5f,.35f,20.1f),
            new Vector3(32.6f,.35f,28f),new Vector3(32.6f,.35f,30.2f),
            new Vector3(47f,.35f,6f),new Vector3(47f,.35f,8f),
            new Vector3(67.4f,.35f,12.1f),new Vector3(69f,.35f,15.3f),
            new Vector3(52.2f,.36f,-25f),new Vector3(67.6f,.36f,-25f),
            new Vector3(9f,.26f,-118f),new Vector3(13.2f,.26f,-120f)
        };
        static readonly Vector3[] Flowers =
        {
            new Vector3(7.9f,.70f,16.7f),new Vector3(8.4f,.70f,17.3f),
            new Vector3(8.9f,.70f,16.9f),new Vector3(9.5f,.70f,17.2f),
            new Vector3(28.1f,.70f,27.3f),new Vector3(28.6f,.70f,27.9f),
            new Vector3(29.1f,.70f,28.2f),new Vector3(29.6f,.70f,27.6f),
            new Vector3(54.8f,.42f,-20f),new Vector3(55.5f,.42f,-18.5f),
            new Vector3(56.8f,.42f,-16.9f),new Vector3(62.4f,.42f,-16.8f),
            new Vector3(64.1f,.42f,-18.7f),new Vector3(65.3f,.42f,-20.3f),
            new Vector3(4.5f,.42f,-123f),new Vector3(7.4f,.42f,-123f),
            new Vector3(34.6f,.42f,-123f),new Vector3(38.3f,.42f,-123f)
        };
        static readonly Vector3[] Grass =
        {
            new Vector3(54.5f,.36f,-23f),new Vector3(55.3f,.36f,-24.4f),
            new Vector3(56.7f,.36f,-26f),new Vector3(59f,.36f,-26.8f),
            new Vector3(61.3f,.36f,-26.4f),new Vector3(64f,.36f,-25f),
            new Vector3(65.4f,.36f,-23.2f),new Vector3(57f,.36f,-13.9f),
            new Vector3(60f,.36f,-13.5f),new Vector3(63f,.36f,-14.5f),
            new Vector3(3.7f,.27f,-122f),new Vector3(32f,.27f,-122f)
        };

        static void VerifyRetainedShaders()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                return;
            foreach (string name in new[]{"KenneyPbrMetallicRoughness",
                                          "KenneyPbrSpecularGlossiness","KenneyUnlit"})
            {
                var material=Resources.Load<Material>("MIR/GltfRuntime/"+name);
                if (material==null || material.shader==null || !material.shader.isSupported)
                    throw new InvalidOperationException(
                        "NONATION_PREMIUM_NATURE_ERROR: Built-in glTF WebGL shader missing "+name);
            }
        }

        async Task<GltfImport> Get(string name)
        {
            GltfImport source;
            if(imports.TryGetValue(name,out source))return source;
            // WebGL StreamingAssets is an HTTP URL; glTFast fetches same-origin
            // bytes inside the Unity WASM loader. No C# System.IO browser reads.
            string url=Application.streamingAssetsPath.TrimEnd('/')+Prefix+name+".glb";
            IMaterialGenerator converter=GraphicsSettings.currentRenderPipeline==null
                ? (IMaterialGenerator)new BuiltInMaterialGenerator()
                : MaterialGenerator.GetDefaultMaterialGenerator();
            source=new GltfImport(materialGenerator:converter,logger:new ConsoleLogger());
            bool loaded=await source.Load(url);
            if(!loaded || stopping)
            {
                source.Dispose();
                throw new InvalidOperationException(
                    "NONATION_PREMIUM_NATURE_ERROR: original CC0 glTF failed to load "+name);
            }
            imports.Add(name,source);
            return source;
        }

        static bool TryBounds(GameObject go,out Bounds bounds,out int vertices)
        {
            bounds=default(Bounds);
            vertices=0;
            bool has=false;
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter==null || filter.sharedMesh==null)continue;
                vertices+=filter.sharedMesh.vertexCount;
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null || !renderer.enabled)continue;
                if(!has){bounds=renderer.bounds;has=true;}
                else bounds.Encapsulate(renderer.bounds);
            }
            return has && vertices>150 && bounds.size.y>.05f && bounds.size.x>.01f;
        }

        async Task<int> Place(string model,Vector3 position,float height,int index)
        {
            var asset=await Get(model);
            var holder=new GameObject("Licensed CC0 foliage / "+model+" #"+index);
            holder.transform.SetParent(transform,false);
            bool ok=false;
            try
            {
                ok=await asset.InstantiateMainSceneAsync(holder.transform);
                if(!ok || stopping)
                    throw new InvalidOperationException("Original foliage scene instantiate failed: "+model);
                Bounds bounds;
                int vertices;
                if(!TryBounds(holder,out bounds,out vertices))
                    throw new InvalidOperationException("Original foliage mesh invalid: "+model);
                // Art-only, aspect-preserving uniform scaling. Never produce
                // a deformed 3D block by stretching xyz independently.
                float variation=1f+((index%5)-2)*.034f;
                float scale=height/bounds.size.y*variation;
                if(scale<.015f || scale>100f)
                    throw new InvalidOperationException("Foliage authored proportions invalid: "+model);
                holder.transform.localScale=Vector3.one*scale;
                holder.transform.rotation=Quaternion.Euler(0,(index*71)%360,0);
                if(!TryBounds(holder,out bounds,out vertices))
                    throw new InvalidOperationException("Foliage bounds after scaling failed: "+model);
                holder.transform.position+=new Vector3(
                    position.x-bounds.center.x,
                    position.y-bounds.min.y,
                    position.z-bounds.center.z);
                // Sources contain no gameplay physics. Explicit removal
                // prevents a newly imported collider breaking resident paths.
                foreach(var collider in holder.GetComponentsInChildren<Collider>(true))
                    Destroy(collider);
                return vertices;
            }
            catch
            {
                Destroy(holder);
                throw;
            }
        }

        async Task<int> Group(string model,Vector3[] sites,float height,int seed)
        {
            int total=0;
            for(int i=0;i<sites.Length && !stopping;i++)
                total+=await Place(model,sites[i],height,seed+i);
            return total;
        }

        async void Start()
        {
            try
            {
                VerifyRetainedShaders();
                int vertices=0;
                vertices+=await Group("CommonTree_1",TreeA,7.2f,101);
                vertices+=await Group("CommonTree_3",TreeB,7.0f,207);
                vertices+=await Group("TwistedTree_2",Twisted,7.5f,309);
                vertices+=await Group("Bush_Common_Flowers",Bushes,1.05f,403);
                vertices+=await Group("Flower_3_Group",Flowers,.49f,503);
                vertices+=await Group("Grass_Common_Short",Grass,.38f,607);
                if(stopping)return;
                if(vertices<8000)
                    throw new InvalidOperationException(
                        "Real CC0 foliage geometry too simple for hero showcase");
                Debug.Log("NONATION_PREMIUM_NATURE_READY models=6 trees=7 bushes=12 " +
                    "flowers=18 grass=12 vertices="+vertices+
                    " source=Quaternius-2025-CC0 (real textured GLB/WebGL)");
            }
            catch(Exception error)
            {
                Debug.LogError("NONATION_PREMIUM_NATURE_ERROR: "+error);
            }
        }

        void OnDestroy()
        {
            stopping=true;
            foreach(var value in imports.Values)
                value.Dispose();
            imports.Clear();
        }
    }
}
