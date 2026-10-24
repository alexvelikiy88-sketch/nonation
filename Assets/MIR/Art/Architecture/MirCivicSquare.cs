using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR
{
    // Replaces the demo cylinder fountain and flat slab with a genuine
    // batched 3D plaza. Gameplay coordinates and routes remain unchanged.
    internal static class MirCivicSquare
    {
        sealed class Surface
        {
            readonly List<Vector3> v=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<int> indices=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int i=v.Count;
                v.Add(a);v.Add(b);v.Add(c);v.Add(d);
                uv.Add(new Vector2(a.x,a.z));uv.Add(new Vector2(b.x,b.z));
                uv.Add(new Vector2(c.x,c.z));uv.Add(new Vector2(d.x,d.z));
                indices.Add(i);indices.Add(i+1);indices.Add(i+2);
                indices.Add(i);indices.Add(i+2);indices.Add(i+3);
            }
            public void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {
                int i=v.Count;
                v.Add(a);v.Add(b);v.Add(c);
                uv.Add(new Vector2(a.x,a.z));uv.Add(new Vector2(b.x,b.z));
                uv.Add(new Vector2(c.x,c.z));
                indices.Add(i);indices.Add(i+1);indices.Add(i+2);
            }
            public void Box(Vector3 center,Vector3 size)
            {
                if(size.x<=.001f||size.y<=.001f||size.z<=.001f)return;
                Vector3 h=size*.5f;
                float l=center.x-h.x,r=center.x+h.x;
                float b=center.y-h.y,t=center.y+h.y;
                float n=center.z-h.z,f=center.z+h.z;
                Quad(new Vector3(l,b,n),new Vector3(l,t,n),new Vector3(r,t,n),new Vector3(r,b,n));
                Quad(new Vector3(r,b,f),new Vector3(r,t,f),new Vector3(l,t,f),new Vector3(l,b,f));
                Quad(new Vector3(l,b,f),new Vector3(l,t,f),new Vector3(l,t,n),new Vector3(l,b,n));
                Quad(new Vector3(r,b,n),new Vector3(r,t,n),new Vector3(r,t,f),new Vector3(r,b,f));
                Quad(new Vector3(l,t,n),new Vector3(l,t,f),new Vector3(r,t,f),new Vector3(r,t,n));
                Quad(new Vector3(l,b,f),new Vector3(l,b,n),new Vector3(r,b,n),new Vector3(r,b,f));
            }
            public void Ring(Vector3 center,float inside,float outside,float height,int sides=48)
            {
                for(int i=0;i<sides;i++)
                {
                    float a=2*Mathf.PI*i/sides,b=2*Mathf.PI*(i+1)/sides;
                    Vector3 ai=new Vector3(center.x+Mathf.Cos(a)*inside,height,
                        center.z+Mathf.Sin(a)*inside);
                    Vector3 ao=new Vector3(center.x+Mathf.Cos(a)*outside,height,
                        center.z+Mathf.Sin(a)*outside);
                    Vector3 bi=new Vector3(center.x+Mathf.Cos(b)*inside,height,
                        center.z+Mathf.Sin(b)*inside);
                    Vector3 bo=new Vector3(center.x+Mathf.Cos(b)*outside,height,
                        center.z+Mathf.Sin(b)*outside);
                    Quad(ai,bi,bo,ao);
                }
            }
            public void Disk(Vector3 center,float radius,float height,int sides=48)
            {
                Vector3 c=new Vector3(center.x,height,center.z);
                for(int i=0;i<sides;i++)
                {
                    float a=2*Mathf.PI*i/sides,b=2*Mathf.PI*(i+1)/sides;
                    Triangle(c,
                        new Vector3(center.x+Mathf.Cos(b)*radius,height,center.z+Mathf.Sin(b)*radius),
                        new Vector3(center.x+Mathf.Cos(a)*radius,height,center.z+Mathf.Sin(a)*radius));
                }
            }
            public void VerticalWall(Vector3 center,float radius,float bottom,float top,int sides=48)
            {
                for(int i=0;i<sides;i++)
                {
                    float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;
                    Vector3 lowA=new Vector3(center.x+radius*Mathf.Cos(a),bottom,
                        center.z+radius*Mathf.Sin(a));
                    Vector3 lowB=new Vector3(center.x+radius*Mathf.Cos(b),bottom,
                        center.z+radius*Mathf.Sin(b));
                    Quad(lowA,new Vector3(lowA.x,top,lowA.z),
                        new Vector3(lowB.x,top,lowB.z),lowB);
                }
            }
            public void Tube(Vector3 from,Vector3 to,float radius,int sides=8)
            {
                Vector3 dir=(to-from).normalized;
                if(dir.sqrMagnitude<.001f)return;
                Vector3 right=Vector3.Cross(dir,Mathf.Abs(dir.y)>.92f?
                    Vector3.forward:Vector3.up).normalized;
                Vector3 up=Vector3.Cross(dir,right);
                for(int i=0;i<sides;i++)
                {
                    float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;
                    Vector3 offA=(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius;
                    Vector3 offB=(right*Mathf.Cos(b)+up*Mathf.Sin(b))*radius;
                    Quad(from+offA,from+offB,to+offB,to+offA);
                }
            }
            public void Crown(Vector3 center,Vector3 size)
            {
                const int sides=9,rings=4;
                for(int j=0;j<rings;j++)for(int i=0;i<sides;i++)
                {
                    float a=Mathf.PI*j/rings,b=Mathf.PI*(j+1)/rings;
                    float c=2*Mathf.PI*i/sides,d=2*Mathf.PI*(i+1)/sides;
                    Quad(Point(a,c),Point(a,d),Point(b,d),Point(b,c));
                }
                Vector3 Point(float lat,float lon)
                {
                    return center+new Vector3(Mathf.Sin(lat)*Mathf.Cos(lon)*size.x,
                        Mathf.Cos(lat)*size.y,Mathf.Sin(lat)*Mathf.Sin(lon)*size.z);
                }
            }
            public void Emit(Transform parent,string name,Material material)
            {
                if(v.Count==0)return;
                Mesh mesh=new Mesh();
                mesh.name="NONATION / Civic Square "+name;
                if(v.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
                var o=new GameObject(name);o.transform.SetParent(parent,false);
                o.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=o.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.On;
                renderer.receiveShadows=true;
            }
        }

        public static void Build(Transform world,Func<string,Color,float,float,Material> mat,
            Action<Light> registerLight)
        {
            var parent=new GameObject("NONATION / Real 3D central civic plaza").transform;
            parent.SetParent(world,false);
            Vector3 center=new Vector3(20,0,20);
            Surface paving=new Surface(),shade=new Surface(),rim=new Surface();
            Surface water=new Surface(),gold=new Surface(),wood=new Surface();
            Surface metal=new Surface(),leaves=new Surface(),flowers=new Surface();

            paving.Box(new Vector3(20,.12f,20),new Vector3(27,.23f,27));
            var ground=new GameObject("Plaza walkable ground collider");
            ground.transform.SetParent(parent,false);
            var walk=ground.AddComponent<BoxCollider>();
            walk.center=new Vector3(20,.12f,20);walk.size=new Vector3(27,.23f,27);

            // Real pavers add scale and surface variation; no individual meshes.
            for(int i=0;i<18;i++)for(int j=0;j<18;j++)
            {
                float x=6.65f+i*1.49f,z=6.65f+j*1.49f;
                Surface dest=(i*3+j)%7==0?shade:paving;
                dest.Quad(new Vector3(x,.243f,z+1.41f),
                    new Vector3(x+1.41f,.243f,z+1.41f),
                    new Vector3(x+1.41f,.243f,z),new Vector3(x,.243f,z));
            }

            // Layered stone basin with true curvature and a physical plinth.
            rim.Disk(center,3.7f,.31f);
            rim.VerticalWall(center,3.67f,.18f,.71f);
            rim.Ring(center,2.81f,3.70f,.72f);
            water.Disk(center,2.80f,.695f);
            shade.Ring(center,2.50f,2.84f,.72f);
            var fColl=new GameObject("Central fountain plinth / collider");
            fColl.transform.SetParent(parent,false);
            var fountainCollider=fColl.AddComponent<SphereCollider>();
            fountainCollider.center=new Vector3(20,1.0f,20);
            fountainCollider.radius=3.55f;

            // Four-sided sculptural metal N, actually built as a volume. From
            // the 3/4 shot it stays a legible marker rather than a PNG billboard.
            gold.Box(new Vector3(18.90f,2.15f,20),new Vector3(.46f,3.26f,.60f));
            gold.Box(new Vector3(21.10f,2.15f,20),new Vector3(.46f,3.26f,.60f));
            for(int i=0;i<10;i++)
            {
                float t=i/10f;
                float x=19.17f+(20.83f-19.17f)*t;
                float y=3.48f-(3.48f-1.02f)*t;
                gold.Box(new Vector3(x,y,20),new Vector3(.37f,.37f,.65f));
            }
            gold.Box(new Vector3(20,.73f,20),new Vector3(3.10f,.10f,.95f));

            // Curved social space with planted beds; the benches are normal
            // scale, not old giant axis-aligned debug rectangles.
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f+.785398f;
                float x=20+Mathf.Cos(a)*10.4f,z=20+Mathf.Sin(a)*10.4f;
                shade.Box(new Vector3(x,.49f,z),new Vector3(3.75f,.60f,1.65f));
                for(int j=0;j<5;j++)
                {
                    float dx=(j-2)*.68f;
                    (j%2==0?leaves:flowers).Crown(new Vector3(x+dx,1.08f,z),
                        new Vector3(.55f,.50f,.45f));
                }
                // Timber slat benches oriented tangentially to the fountain.
                float bx=20+Mathf.Cos(a)*7.6f,bz=20+Mathf.Sin(a)*7.6f;
                for(int slat=-2;slat<=2;slat++)
                    wood.Box(new Vector3(bx,.70f,bz+slat*.13f),
                        new Vector3(1.90f,.075f,.11f));
                for(int side=-1;side<=1;side+=2)
                {
                    metal.Tube(new Vector3(bx+side*.78f,.29f,bz),
                        new Vector3(bx+side*.78f,.70f,bz),.062f);
                }
            }

            // Sidewalk-scaled bollards around plaza approaches, outside the
            // fountain and old business entry/vehicle lanes.
            for(int i=0;i<4;i++)
            {
                float x=i%2==0?7.9f:32.1f;
                float z=i<2?8.1f:31.9f;
                metal.Tube(new Vector3(x,.24f,z),new Vector3(x,1.18f,z),.075f);
                rim.Box(new Vector3(x,1.18f,z),new Vector3(.22f,.10f,.22f));
            }

            // Understated warm sculptural lights, reusing existing night switch.
            for(int i=0;i<2;i++)
            {
                float x=i==0?13f:27f;
                var lamp=new GameObject("Plaza warm ambient accent").AddComponent<Light>();
                lamp.transform.SetParent(parent,false);
                lamp.transform.localPosition=new Vector3(x,1.22f,20);
                lamp.type=LightType.Point;lamp.intensity=0;
                lamp.range=8;lamp.shadows=LightShadows.None;
                lamp.color=new Color(1,.78f,.58f);
                if(registerLight!=null)registerLight(lamp);
            }

            paving.Emit(parent,"Sandstone plaza masonry",
                mat("civic-paving",new Color(.71f,.69f,.65f),0,.21f));
            shade.Emit(parent,"Plaza paver variation and planters",
                mat("civic-shade",new Color(.58f,.59f,.53f),0,.19f));
            rim.Emit(parent,"Sculpted fountain limestone",
                mat("civic-fountain-stone",new Color(.80f,.79f,.73f),0,.27f));
            water.Emit(parent,"Shallow fountain water",
                mat("civic-water",new Color(.25f,.56f,.63f),.10f,.87f));
            gold.Emit(parent,"Golden NONATION N monument",
                mat("civic-sculpture",new Color(.76f,.59f,.30f),.79f,.74f));
            wood.Emit(parent,"Outdoor timber seating",
                mat("civic-wood",new Color(.52f,.36f,.24f),0,.21f));
            metal.Emit(parent,"Steel street furnishings",
                mat("civic-steel",new Color(.25f,.29f,.30f),.61f,.46f));
            leaves.Emit(parent,"Public greenery",
                mat("civic-green",new Color(.31f,.47f,.26f),0,.13f));
            flowers.Emit(parent,"Flowering planting",
                mat("civic-flowers",new Color(.62f,.40f,.41f),0,.16f));
        }
    }
}
