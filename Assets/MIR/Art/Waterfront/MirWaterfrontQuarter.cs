using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR
{
    // An opt-in, geometry-only visual module. Gameplay locations and pathfinding
    // remain governed by MirWorldGeometry; no artificial NPCs or fake landmarks.
    // All decoration with the same material is baked into one runtime mesh.
    internal static class MirWaterfrontQuarter
    {
        sealed class Surface
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> triangles = new List<int>();

            public void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                uv.Add(new Vector2(a.x, a.z));
                uv.Add(new Vector2(b.x, b.z));
                uv.Add(new Vector2(c.x, c.z));
                triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                uv.Add(new Vector2(a.x, a.z)); uv.Add(new Vector2(b.x, b.z));
                uv.Add(new Vector2(c.x, c.z)); uv.Add(new Vector2(d.x, d.z));
                triangles.Add(i); triangles.Add(i+1); triangles.Add(i+2);
                triangles.Add(i); triangles.Add(i+2); triangles.Add(i+3);
            }

            public void Box(Vector3 center, Vector3 size)
            {
                Vector3 h=size*.5f;
                float l=center.x-h.x, r=center.x+h.x;
                float b=center.y-h.y, t=center.y+h.y;
                float n=center.z-h.z, f=center.z+h.z;
                Quad(new Vector3(l,b,n),new Vector3(l,t,n),new Vector3(r,t,n),new Vector3(r,b,n));
                Quad(new Vector3(r,b,f),new Vector3(r,t,f),new Vector3(l,t,f),new Vector3(l,b,f));
                Quad(new Vector3(l,b,f),new Vector3(l,t,f),new Vector3(l,t,n),new Vector3(l,b,n));
                Quad(new Vector3(r,b,n),new Vector3(r,t,n),new Vector3(r,t,f),new Vector3(r,b,f));
                Quad(new Vector3(l,t,n),new Vector3(l,t,f),new Vector3(r,t,f),new Vector3(r,t,n));
                Quad(new Vector3(l,b,f),new Vector3(l,b,n),new Vector3(r,b,n),new Vector3(r,b,f));
            }

            // Eight-sided aluminium/wooden tubes keep the waterfront rails and
            // street furniture rounded rather than using enlarged square blocks.
            public void Tube(Vector3 start, Vector3 end, float radius, int sides=8)
            {
                Vector3 axis=end-start;
                if(axis.sqrMagnitude<.0001f)return;
                Vector3 tangent=axis.normalized;
                Vector3 right=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?
                    Vector3.forward:Vector3.up).normalized;
                Vector3 up=Vector3.Cross(tangent,right);
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2f/sides, b=(j+1)*Mathf.PI*2f/sides;
                    Vector3 v=(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius;
                    Vector3 w=(right*Mathf.Cos(b)+up*Mathf.Sin(b))*radius;
                    Quad(start+v,start+w,end+w,end+v);
                }
            }

            // Faceted organic crowns use actual triangles and shared materials.
            public void Crown(Vector3 center, Vector3 radii, int sides=10, int rings=5)
            {
                for(int row=0;row<rings;row++)
                {
                    float lo=Mathf.PI*row/rings, hi=Mathf.PI*(row+1)/rings;
                    for(int side=0;side<sides;side++)
                    {
                        float a=side*Mathf.PI*2/sides, b=(side+1)*Mathf.PI*2/sides;
                        Vector3 p=Point(center,radii,lo,a),q=Point(center,radii,lo,b);
                        Vector3 r=Point(center,radii,hi,b),s=Point(center,radii,hi,a);
                        // Outward-facing winding of the latitude strips.
                        Quad(p,q,r,s);
                    }
                }
            }

            static Vector3 Point(Vector3 center,Vector3 radii,float lat,float lon)
            {
                return center+new Vector3(Mathf.Sin(lat)*Mathf.Cos(lon)*radii.x,
                    Mathf.Cos(lat)*radii.y,Mathf.Sin(lat)*Mathf.Sin(lon)*radii.z);
            }

            public void Umbrella(Vector3 center, float radius, bool alternate)
            {
                for(int j=0;j<12;j++)
                {
                    if((j%2==0)!=alternate)continue;
                    float a=j*Mathf.PI/6f,b=(j+1)*Mathf.PI/6f;
                    Vector3 top=center+Vector3.up*.58f;
                    Vector3 edgeA=center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                    Vector3 edgeB=center+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                    Triangle(top,edgeB,edgeA);
                }
            }

            public void Emit(Transform parent,string name,Material material,bool shadows)
            {
                if(vertices.Count==0)return;
                var mesh=new Mesh();
                mesh.name="NONATION / "+name;
                if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetUVs(0,uv);
                mesh.SetTriangles(triangles,0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var go=new GameObject(name);
                go.transform.SetParent(parent,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=material;
                renderer.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;
                renderer.receiveShadows=shadows;
            }
        }

        // Called once by MirCity.Build. All geometry is static and away from
        // existing mapped business entrances and NPC sidewalk routing.
        public static void Build(Transform city, Func<string,Color,float,float,Material> material,
            Action<Light> registerLight)
        {
            var group=new GameObject("NONATION / Waterfront hero-quarter");
            group.transform.SetParent(city,false);
            Transform parent=group.transform;
            var stone=new Surface();
            var stoneAccent=new Surface();
            var rail=new Surface();
            var deckWood=new Surface();
            var bark=new Surface();
            var foliage=new Surface();
            var lighterLeaves=new Surface();
            var sea=new Surface();
            var foam=new Surface();
            var cycle=new Surface();
            var canvas=new Surface();
            var canvasAlt=new Surface();

            // The current terrain ends at z=-130. The promenade overlaps the
            // final two metres of the existing N-S sidewalks (end at z=-110).
            // The sea is physically outside the terrain, not a picture plane on it.
            stone.Box(new Vector3(0,.12f,-118f),new Vector3(188,.24f,20f));
            // One contiguous static walkable collider, not 940 tile colliders.
            var promenadeWalk=new GameObject("Walkable promenade surface");
            promenadeWalk.transform.SetParent(parent,false);
            var promenadeCollider=promenadeWalk.AddComponent<BoxCollider>();
            promenadeCollider.center=new Vector3(0,.12f,-118f);
            promenadeCollider.size=new Vector3(188,.24f,20f);
            stone.Box(new Vector3(0,-.35f,-128.62f),new Vector3(190,1.20f,1.35f));
            stoneAccent.Box(new Vector3(0,.28f,-128.13f),new Vector3(190,.15f,.85f));

            // Repeated pavers are *vertices of two shared meshes*, not thousands
            // of renderer objects. 3 mm reveals separate the flagstones.
            for(int x=0;x<94;x++)for(int z=0;z<10;z++)
            {
                float left=-94f+x*2f+.055f, right=-94f+(x+1)*2f-.055f;
                float far=-126.8f+z*1.70f+.055f;
                float near=-126.8f+(z+1)*1.70f-.055f;
                var target=(x+z*3)%7==0?stoneAccent:stone;
                target.Quad(new Vector3(left,.247f,near),new Vector3(right,.247f,near),
                    new Vector3(right,.247f,far),new Vector3(left,.247f,far));
            }

            // Continuous separated bicycle strip and its understated edge.
            cycle.Quad(new Vector3(-94,.252f,-111.5f),new Vector3(94,.252f,-111.5f),
                new Vector3(94,.252f,-113.8f),new Vector3(-94,.252f,-113.8f));
            foam.Box(new Vector3(0,.256f,-114.15f),new Vector3(188,.018f,.075f));
            for(int x=-89;x<90;x+=9)
                foam.Box(new Vector3(x,.258f,-112.6f),new Vector3(3.4f,.012f,.06f));

            // A low seawall with cylindrical handrails. One continuous physical
            // collision barrier protects the water edge but no NPC POI/road.
            for(int i=0;i<=62;i++)
            {
                float x=-93f+i*3f;
                rail.Tube(new Vector3(x,.30f,-128.25f),
                    new Vector3(x,1.48f,-128.25f),.062f);
                if(i%4==0)
                    stoneAccent.Box(new Vector3(x,.30f,-128.62f),
                        new Vector3(.92f,.17f,1.35f));
            }
            rail.Tube(new Vector3(-93,1.46f,-128.25f),new Vector3(93,1.46f,-128.25f),.072f,10);
            rail.Tube(new Vector3(-93,.79f,-128.25f),new Vector3(93,.79f,-128.25f),.047f,10);
            var boundary=new GameObject("Water-edge safety collider");
            boundary.transform.SetParent(parent,false);
            var boundaryCollider=boundary.AddComponent<BoxCollider>();
            boundaryCollider.center=new Vector3(0,.42f,-128.70f);
            boundaryCollider.size=new Vector3(190,2.1f,1.15f);

            // Calm open sea with low-amplitude faceted waves; no CPU per-frame
            // surface updates, additional lights or expensive reflection probes.
            for(int x=0;x<56;x++)for(int z=0;z<12;z++)
            {
                float x0=-141f+x*282f/56f,x1=-141f+(x+1)*282f/56f;
                float z0=-129.5f-z*52f/12f,z1=-129.5f-(z+1)*52f/12f;
                sea.Quad(Wave(x0,z0),Wave(x1,z0),Wave(x1,z1),Wave(x0,z1));
            }
            for(int x=-92;x<92;x+=7)
                foam.Quad(new Vector3(x,-.57f,-130.2f),new Vector3(x+4,-.57f,-130.2f),
                    new Vector3(x+4,-.57f,-130.85f),new Vector3(x,-.57f,-130.85f));

            // Distinct foliage sizes and positions form a readable rhythm
            // without repeating one prefab every metre.
            for(int i=0;i<9;i++)
            {
                float x=-84f+i*21f;
                float z=-119.0f+(i%3)*.65f;
                bark.Tube(new Vector3(x,.25f,z),new Vector3(x,3.4f+(i%2)*.4f,z),.16f,8);
                for(int j=0;j<5;j++)
                {
                    float a=j*2.39996f+i*.35f;
                    Vector3 offset=new Vector3(Mathf.Cos(a)*1.04f,(j%3)*.25f,Mathf.Sin(a)*.76f);
                    (j%2==0?foliage:lighterLeaves).Crown(
                        new Vector3(x,4.02f,z)+offset,
                        new Vector3(1.26f,1.02f,1.21f),8,4);
                }
            }

            // Stone planters and hand-scaled shrubs on the waterside.
            for(int i=0;i<10;i++)
            {
                float x=-87f+i*19.4f;
                stoneAccent.Box(new Vector3(x,.48f,-124.7f),new Vector3(2.3f,.66f,.83f));
                for(int j=0;j<3;j++)
                    (j==1?lighterLeaves:foliage).Crown(
                        new Vector3(x+(j-1)*.62f,1.05f,-124.7f),
                        new Vector3(.63f,.53f,.57f),8,4);
            }

            // Timbers, steel legs and angled backs: all seats share 2 materials.
            for(int i=0;i<6;i++)
            {
                float x=-74f+i*29.6f,z=-121.5f;
                deckWood.Box(new Vector3(x,.70f,z),new Vector3(2.45f,.17f,.68f));
                deckWood.Box(new Vector3(x,1.12f,z+.31f),new Vector3(2.45f,.73f,.14f));
                for(int sign=-1;sign<=1;sign+=2)
                    rail.Tube(new Vector3(x+sign*.94f,.28f,z),
                        new Vector3(x+sign*.94f,.71f,z),.065f);
            }

            // Four restrained, shadowless promenade fixtures share the existing
            // MirCity night/day light switch. No per-frame animation or probes.
            for(int i=0;i<4;i++)
            {
                float x=-75f+i*50f;
                rail.Tube(new Vector3(x,.25f,-116.3f),
                    new Vector3(x,4.10f,-116.3f),.082f,10);
                rail.Box(new Vector3(x,4.14f,-116.3f),
                    new Vector3(.52f,.16f,.52f));
                foam.Box(new Vector3(x,4.035f,-116.3f),
                    new Vector3(.42f,.045f,.42f));
                var light=new GameObject("Waterfront warm path light").AddComponent<Light>();
                light.transform.SetParent(parent,false);
                light.transform.localPosition=new Vector3(x,4.02f,-116.3f);
                light.type=LightType.Point;
                light.range=14f;
                light.intensity=0f;
                light.shadows=LightShadows.None;
                light.color=new Color(1f,.78f,.57f);
                if(registerLight!=null)registerLight(light);
            }

            // A working-scale café patio stays to either side of the canonical
            // (56,12) cafe entry. It does not move the entry or add fake patrons.
            stone.Box(new Vector3(56,.09f,10.60f),new Vector3(26,.16f,6.8f));
            var patioWalk=new GameObject("Walkable cafe patio");
            patioWalk.transform.SetParent(parent,false);
            var patioCollider=patioWalk.AddComponent<BoxCollider>();
            patioCollider.center=new Vector3(56,.09f,10.60f);
            patioCollider.size=new Vector3(26,.16f,6.8f);
            for(int j=0;j<2;j++)
            {
                float x=j==0?46.5f:65.5f,z=10.9f;
                rail.Tube(new Vector3(x,.19f,z),new Vector3(x,3.02f,z),.07f,10);
                canvas.Umbrella(new Vector3(x,2.72f,z),2.65f,false);
                canvasAlt.Umbrella(new Vector3(x,2.72f,z),2.65f,true);
                deckWood.Box(new Vector3(x,1.04f,z),new Vector3(1.48f,.10f,1.48f));
                rail.Tube(new Vector3(x,.2f,z),new Vector3(x,1.00f,z),.083f);
                for(int side=-1;side<=1;side+=2)
                {
                    float sx=x+side*1.33f;
                    deckWood.Box(new Vector3(sx,.60f,z),
                        new Vector3(.62f,.12f,.66f));
                    rail.Tube(new Vector3(sx,.2f,z),new Vector3(sx,.58f,z),.054f);
                }
            }

            stone.Emit(parent,"Promenade stonework and cafe deck",
                material("waterfront-sandstone",new Color(.69f,.68f,.62f),0,.18f),true);
            stoneAccent.Emit(parent,"Cut stone seawall and paver accents",
                material("waterfront-stone-edge",new Color(.57f,.56f,.52f),.02f,.19f),true);
            rail.Emit(parent,"Continuous steel railing and cafe furniture",
                material("waterfront-rail",new Color(.20f,.27f,.29f),.66f,.46f),true);
            deckWood.Emit(parent,"Seating and cafe timber",
                material("waterfront-wood",new Color(.49f,.34f,.22f),0,.22f),true);
            bark.Emit(parent,"Promenade tree trunks",
                material("waterfront-bark",new Color(.39f,.29f,.20f),0,.13f),true);
            foliage.Emit(parent,"Tree and shrub foliage A",
                material("waterfront-leaves",new Color(.29f,.41f,.24f),0,.12f),true);
            lighterLeaves.Emit(parent,"Tree and shrub foliage B",
                material("waterfront-leaves-light",new Color(.39f,.50f,.29f),0,.12f),true);
            sea.Emit(parent,"Open waterfront sea mesh",
                material("waterfront-sea",new Color(.17f,.40f,.46f),.03f,.83f),false);
            foam.Emit(parent,"Foam and cycle lane demarcation",
                material("waterfront-highlights",new Color(.83f,.86f,.77f),0,.29f),false);
            cycle.Emit(parent,"Promenade cycle strip",
                material("waterfront-cycle",new Color(.47f,.31f,.29f),0,.15f),false);
            canvas.Emit(parent,"Cafe umbrellas ivory",
                material("waterfront-canvas",new Color(.88f,.83f,.71f),0,.3f),true);
            canvasAlt.Emit(parent,"Cafe umbrellas warm white",
                material("waterfront-canvas-alt",new Color(.96f,.91f,.82f),0,.3f),true);
        }

        static Vector3 Wave(float x,float z)
        {
            float y=-.63f+Mathf.Sin(x*.13f+z*.11f)*.028f+
                Mathf.Cos(x*.047f-z*.21f)*.018f;
            return new Vector3(x,y,z);
        }
    }
}
