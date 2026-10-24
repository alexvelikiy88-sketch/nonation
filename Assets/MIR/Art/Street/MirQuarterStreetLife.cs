using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR
{
    // Geometry authored specifically for the first VISUAL GATE quarter.
    // All details are real 3D meshes/road paint and are batched by material.
    // Nothing is a reference-image billboard or gameplay obstacle.
    internal static class MirQuarterStreetLife
    {
        sealed class Surface
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<int> triangles=new List<int>();
            static Vector2 Map(Vector3 p,Vector3 n)
            {
                if(Mathf.Abs(n.y)>.55f)return new Vector2(p.x,p.z);
                if(Mathf.Abs(n.x)>.55f)return new Vector2(p.z,p.y);
                return new Vector2(p.x,p.y);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                Vector3 normal=Vector3.Cross(b-a,d-a).normalized;
                int start=vertices.Count;
                vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                uv.Add(Map(a,normal));uv.Add(Map(b,normal));
                uv.Add(Map(c,normal));uv.Add(Map(d,normal));
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
                triangles.Add(start);triangles.Add(start+2);triangles.Add(start+3);
            }
            public void Tri(Vector3 a,Vector3 b,Vector3 c)
            {
                Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                int start=vertices.Count;
                vertices.Add(a);vertices.Add(b);vertices.Add(c);
                uv.Add(Map(a,normal));uv.Add(Map(b,normal));uv.Add(Map(c,normal));
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
            }
            public void Box(Vector3 p,Vector3 size)
            {
                if(size.x<=.001f||size.y<=.001f||size.z<=.001f)return;
                Vector3 h=size*.5f;
                float l=p.x-h.x,r=p.x+h.x,b=p.y-h.y,t=p.y+h.y,n=p.z-h.z,f=p.z+h.z;
                Quad(new Vector3(l,b,n),new Vector3(l,t,n),new Vector3(r,t,n),new Vector3(r,b,n));
                Quad(new Vector3(r,b,f),new Vector3(r,t,f),new Vector3(l,t,f),new Vector3(l,b,f));
                Quad(new Vector3(l,b,f),new Vector3(l,t,f),new Vector3(l,t,n),new Vector3(l,b,n));
                Quad(new Vector3(r,b,n),new Vector3(r,t,n),new Vector3(r,t,f),new Vector3(r,b,f));
                Quad(new Vector3(l,t,n),new Vector3(l,t,f),new Vector3(r,t,f),new Vector3(r,t,n));
                Quad(new Vector3(l,b,f),new Vector3(l,b,n),new Vector3(r,b,n),new Vector3(r,b,f));
            }
            public void Tube(Vector3 a,Vector3 b,float r,int sides=7)
            {
                Vector3 dir=(b-a).normalized;
                if(dir.sqrMagnitude<.001f)return;
                Vector3 right=Vector3.Cross(dir,Mathf.Abs(dir.y)>.92f?
                    Vector3.forward:Vector3.up).normalized;
                Vector3 up=Vector3.Cross(dir,right);
                for(int i=0;i<sides;i++)
                {
                    float t=i*2*Mathf.PI/sides,u=(i+1)*2*Mathf.PI/sides;
                    Vector3 o=(right*Mathf.Cos(t)+up*Mathf.Sin(t))*r;
                    Vector3 q=(right*Mathf.Cos(u)+up*Mathf.Sin(u))*r;
                    Quad(a+o,a+q,b+q,b+o);
                }
            }
            public void Crown(Vector3 center,Vector3 radius,int sides=10,int rings=5)
            {
                for(int j=0;j<rings;j++)for(int i=0;i<sides;i++)
                {
                    float a=Mathf.PI*j/rings,b=Mathf.PI*(j+1)/rings;
                    float c=2*Mathf.PI*i/sides,d=2*Mathf.PI*(i+1)/sides;
                    Quad(Sphere(a,c),Sphere(a,d),Sphere(b,d),Sphere(b,c));
                }
                Vector3 Sphere(float a,float b)
                {
                    return center+new Vector3(radius.x*Mathf.Sin(a)*Mathf.Cos(b),
                        radius.y*Mathf.Cos(a),
                        radius.z*Mathf.Sin(a)*Mathf.Sin(b));
                }
            }
            public void Emit(Transform parent,Material m)
            {
                if(vertices.Count==0)return;
                var mesh=new Mesh();
                mesh.name="NONATION street / "+m.name;
                if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(vertices);mesh.SetUVs(0,uv);
                mesh.SetTriangles(triangles,0);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject("Street Mesh / "+m.name);
                go.transform.SetParent(parent,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=m;renderer.shadowCastingMode=ShadowCastingMode.On;
                renderer.receiveShadows=true;
            }
        }
        sealed class Kit
        {
            readonly Transform root;
            readonly Func<string,Color,float,float,Material> mat;
            readonly Dictionary<Material,Surface> all=new Dictionary<Material,Surface>();
            Surface S(string name,Color c,float metal=0,float smooth=.15f)
            {
                Material m=mat("VIS-02/street/"+name,c,metal,smooth);
                Surface found;
                if(!all.TryGetValue(m,out found))
                {found=new Surface();all.Add(m,found);}
                return found;
            }
            readonly Surface white,yellow,paver,iron,wood,glass,bark,leaves,litLeaves,
                shadowLeaves,planter,linen,stone,flowers,rubber,cycleMetal;
            public Kit(Transform root,Func<string,Color,float,float,Material> mat)
            {
                this.root=root;this.mat=mat;
                white=S("road paint warm-white",new Color(.95f,.93f,.84f));
                yellow=S("traffic amber",new Color(.87f,.71f,.34f));
                paver=S("corner paving",new Color(.71f,.70f,.66f));
                iron=S("black iron",new Color(.19f,.22f,.23f),.47f,.38f);
                wood=S("outdoor hardwood",new Color(.52f,.35f,.22f),0,.23f);
                glass=S("smoked clear roof glass",new Color(.30f,.41f,.41f),.16f,.75f);
                bark=S("mature tree bark",new Color(.34f,.28f,.24f));
                leaves=S("foliage rich green",new Color(.30f,.44f,.27f));
                litLeaves=S("foliage sun green",new Color(.43f,.54f,.31f));
                shadowLeaves=S("foliage dark green",new Color(.22f,.34f,.23f));
                planter=S("garden limestone",new Color(.73f,.71f,.65f));
                linen=S("cafe canopy warm natural",new Color(.88f,.82f,.66f));
                stone=S("cafe and bench stone",new Color(.68f,.67f,.63f));
                flowers=S("flowers warm pink",new Color(.67f,.43f,.48f));
                rubber=S("bicycle tire charcoal",new Color(.11f,.12f,.13f),0,.2f);
                cycleMetal=S("bicycle steel",new Color(.64f,.66f,.63f),.68f,.48f);
            }
            public void Flush()
            {
                Transform holder=new GameObject("VIS-02 / detailed hero street meshes").transform;
                holder.SetParent(root,false);
                foreach(var pair in all)pair.Value.Emit(holder,pair.Key);
            }
            public void HeroCrosswalks()
            {
                // Original 25 grid intersections remain playable. Four crossings
                // around (20,20) receive a clean painted geometry instead of
                // overlaying duplicate old stripes. The caller omits those
                // old strips from AsphaltRoads and leaves all others unchanged.
                for(int gx=0;gx<=1;gx++)for(int gz=0;gz<=1;gz++)
                {
                    float x=gx*40,z=gz*40;
                    for(int k=-3;k<=3;k++)
                    {
                        float lateral=k*1.055f;
                        white.Quad(new Vector3(x+lateral-.36f,.073f,z+8.15f),
                            new Vector3(x+lateral+.36f,.073f,z+8.15f),
                            new Vector3(x+lateral+.36f,.073f,z+5.55f),
                            new Vector3(x+lateral-.36f,.073f,z+5.55f));
                        white.Quad(new Vector3(x-5.55f,.073f,z+lateral+.36f),
                            new Vector3(x-5.55f,.073f,z+lateral-.36f),
                            new Vector3(x-8.15f,.073f,z+lateral-.36f),
                            new Vector3(x-8.15f,.073f,z+lateral+.36f));
                    }
                    // Thin stop bars at actual road level, outside zebra.
                    white.Quad(new Vector3(x-4.10f,.075f,z+9.05f),
                        new Vector3(x+4.10f,.075f,z+9.05f),
                        new Vector3(x+4.10f,.075f,z+8.90f),
                        new Vector3(x-4.10f,.075f,z+8.90f));
                    yellow.Quad(new Vector3(x-9.02f,.075f,z-4.10f),
                        new Vector3(x-9.02f,.075f,z+4.10f),
                        new Vector3(x-8.90f,.075f,z+4.10f),
                        new Vector3(x-8.90f,.075f,z-4.10f));
                }
            }
            public void Tree(float x,float z,float scale,int variant)
            {
                float bottom=.30f;
                Vector3 basePos=new Vector3(x,bottom,z);
                float trunkHeight=3.50f*scale;
                bark.Tube(basePos,basePos+Vector3.up*trunkHeight,.19f*scale,9);
                for(int branch=0;branch<4;branch++)
                {
                    float a=(branch*1.5708f+variant*.43f);
                    Vector3 end=basePos+
                        new Vector3(Mathf.Cos(a)*1.12f*scale,4.58f*scale,
                            Mathf.Sin(a)*.95f*scale);
                    bark.Tube(basePos+Vector3.up*(2.74f*scale),end,.075f*scale,7);
                }
                // Layered irregular lobes visually articulate the crown from
                // the same 3/4 camera as the approved reference.
                for(int j=0;j<8;j++)
                {
                    float a=j*2.39996f+variant*.29f;
                    float distance=j==0?.0f:(.58f+(j%3)*.29f)*scale;
                    float height=(4.98f+(j%4)*.27f)*scale+bottom;
                    float xx=x+Mathf.Cos(a)*distance,zz=z+Mathf.Sin(a)*distance;
                    float width=(1.20f+(j%3)*.14f)*scale;
                    (j%3==0?litLeaves:j%3==1?leaves:shadowLeaves).Crown(
                        new Vector3(xx,height,zz),
                        new Vector3(width,1.22f*scale,width*.84f));
                }
                // Tree pit has exposed soil and stone trim; no collider that
                // could accidentally break NPC's sidewalk navigation.
                planter.Box(basePos+new Vector3(0,-.08f,0),
                    new Vector3(1.58f,.14f,1.58f));
                shadowLeaves.Box(basePos+new Vector3(0,.01f,0),
                    new Vector3(1.31f,.055f,1.31f));
            }
            public void Bike(float x,float z,float variation)
            {
                float y=.29f;
                Vector3 rear=new Vector3(x-.53f,y+.44f,z);
                Vector3 front=new Vector3(x+.55f,y+.44f,z);
                for(int w=0;w<2;w++)
                {
                    Vector3 hub=w==0?rear:front;
                    const int segments=20;
                    for(int k=0;k<segments;k++)
                    {
                        float a=k*2*Mathf.PI/segments,b=(k+1)*2*Mathf.PI/segments;
                        Vector3 v=hub+new Vector3(.39f*Mathf.Cos(a),.39f*Mathf.Sin(a),0);
                        Vector3 q=hub+new Vector3(.39f*Mathf.Cos(b),.39f*Mathf.Sin(b),0);
                        rubber.Tube(v,q,.030f,5);
                        if(k%4==0)
                            cycleMetal.Tube(hub,v,.009f,4);
                    }
                }
                Vector3 crank=new Vector3(x,y+.43f,z);
                Vector3 saddle=new Vector3(x-.18f,y+.95f,z);
                Vector3 head=new Vector3(x+.32f,y+1.00f,z);
                cycleMetal.Tube(rear,crank,.028f);
                cycleMetal.Tube(crank,saddle,.031f);
                cycleMetal.Tube(saddle,rear,.027f);
                cycleMetal.Tube(saddle,head,.028f);
                cycleMetal.Tube(head,crank,.031f);
                cycleMetal.Tube(head,front,.027f);
                iron.Tube(saddle,new Vector3(saddle.x-.18f,y+1.03f,z),.058f,6);
                iron.Tube(head,new Vector3(head.x+.11f,y+1.14f,z),.037f);
                iron.Tube(new Vector3(head.x+.11f,y+1.14f,z-.25f),
                    new Vector3(head.x+.11f,y+1.14f,z+.25f),.025f);
                wood.Box(new Vector3(x-.18f,y+1.015f,z),new Vector3(.33f,.065f,.19f));
                // Low-profile U-shaped bicycle rack; bicycle is decor until
                // AGENT 02 provides a proper cycling interaction.
                float rackX=x+.06f;
                iron.Tube(new Vector3(rackX,.29f,z-.42f),
                    new Vector3(rackX,1.12f,z-.42f),.045f);
                iron.Tube(new Vector3(rackX,.29f,z+.42f),
                    new Vector3(rackX,1.12f,z+.42f),.045f);
                iron.Tube(new Vector3(rackX,1.12f,z-.42f),
                    new Vector3(rackX,1.12f,z+.42f),.045f);
            }
            public void Patio(float x,float z)
            {
                float ground=.31f;
                // Pedestal tables, two seats per table. The centre entrance at
                // (56,12) is left unobstructed between the two patios.
                iron.Tube(new Vector3(x,ground,z),new Vector3(x,.91f,z),.065f);
                wood.Box(new Vector3(x,.91f,z),new Vector3(.90f,.065f,.90f));
                stone.Box(new Vector3(x,.37f,z),new Vector3(.47f,.075f,.47f));
                for(int side=-1;side<=1;side+=2)
                {
                    float sx=x+side*1.08f;
                    wood.Box(new Vector3(sx,.52f,z),new Vector3(.47f,.095f,.45f));
                    wood.Box(new Vector3(sx+side*.21f,.86f,z),new Vector3(.095f,.66f,.48f));
                    for(int dz=-1;dz<=1;dz+=2)
                    {
                        iron.Tube(new Vector3(sx+side*.15f,.31f,z+dz*.16f),
                            new Vector3(sx+side*.15f,.52f,z+dz*.16f),.034f);
                    }
                }
                // Linen parasol is a real polygon canopy with curved spokes.
                Vector3 apex=new Vector3(x,3.53f,z);
                iron.Tube(new Vector3(x,ground,z),apex,.058f,8);
                const int ribs=10;
                for(int i=0;i<ribs;i++)
                {
                    float a=i*2*Mathf.PI/ribs,b=(i+1)*2*Mathf.PI/ribs;
                    Vector3 rimA=new Vector3(x+Mathf.Cos(a)*2.25f,2.67f,
                        z+Mathf.Sin(a)*2.25f);
                    Vector3 rimB=new Vector3(x+Mathf.Cos(b)*2.25f,2.67f,
                        z+Mathf.Sin(b)*2.25f);
                    // Two winding directions make canopy visible from below,
                    // while cloth panels retain real depth and cast shadows.
                    linen.Tri(apex,rimA,rimB);
                    linen.Tri(apex,rimB,rimA);
                    iron.Tube(apex,rimA,.022f,6);
                }
                // Planting separates hospitality frontage from the street.
                for(int k=-1;k<=1;k++)
                {
                    float px=x+k*1.33f,pz=z-2.66f;
                    planter.Box(new Vector3(px,.48f,pz),new Vector3(.75f,.56f,.48f));
                    (k==0?flowers:litLeaves).Crown(new Vector3(px,.97f,pz),
                        new Vector3(.48f,.37f,.34f));
                }
            }
            public void Furniture()
            {
                // Original reference: tree-shaded crossroads, readable cafe
                // terraces, bicycle storage by market and benches. Do not
                // create actual fake NPCs just to populate a screenshot.
                Tree(13f,13f,.99f,0);    // replaces original block tree
                Tree(53f,53f,1.02f,1);  // replaces original block tree
                Tree(11f,28.3f,.82f,2);
                Tree(30.2f,11.4f,.89f,3);
                Tree(14f,-31.0f,.84f,4);
                Tree(63f,7.7f,.83f,5);
                Patio(50f,10.9f);
                Patio(62f,10.9f);
                for(int i=0;i<3;i++)
                    Bike(27.2f+i*1.62f,-31.0f,i*.25f);
                // 8 rounded-corner bollards around the focal plaza
                // (unobstructed visual markers, no physics collider).
                foreach(var p in new[]{
                    new Vector3(8.3f,.29f,7.6f),new Vector3(31.7f,.29f,7.6f),
                    new Vector3(8.3f,.29f,32.4f),new Vector3(31.7f,.29f,32.4f),
                    new Vector3(7.6f,.29f,8.3f),new Vector3(32.4f,.29f,8.3f),
                    new Vector3(7.6f,.29f,31.7f),new Vector3(32.4f,.29f,31.7f)})
                {
                    iron.Tube(p,p+Vector3.up*.85f,.068f,8);
                    white.Box(p+Vector3.up*.87f,new Vector3(.20f,.09f,.20f));
                }
            }
        }

        public static void Build(Transform root,
            Func<string,Color,float,float,Material> material)
        {
            if(root==null)throw new ArgumentNullException("root");
            if(material==null)throw new ArgumentNullException("material");
            var kit=new Kit(root,material);
            kit.HeroCrosswalks();
            kit.Furniture();
            kit.Flush();
        }
    }
}
