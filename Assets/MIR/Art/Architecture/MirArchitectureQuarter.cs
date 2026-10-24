using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MIR
{
    // Replacement for the old single-box buildings in the first urban slice.
    // Architecture is assembled as editable real mesh geometry (including
    // actual front-wall apertures), not a decal of the approved concept image.
    // Coordinates and physics footprint agree with the original MirCity.
    internal sealed class MirArchitectureQuarter
    {
        const float FloorHeight = 3.5f;
        const float Epsilon = .001f;
        readonly Transform root;
        readonly Dictionary<Material, Surface> surfaces = new Dictionary<Material, Surface>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        struct Building
        {
            public string name, sign;
            public float x, z, width, depth;
            public int floors, style, index;
            public bool retail, terrace;
            public Building(string name, string sign, float x, float z, float w, float d,
                int floors, int style, int index, bool retail, bool terrace)
            {
                this.name=name; this.sign=sign; this.x=x; this.z=z;
                width=w; depth=d; this.floors=floors; this.style=style;
                this.index=index; this.retail=retail; this.terrace=terrace;
            }
        }

        sealed class Surface
        {
            readonly List<Vector3> points=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<int> indices=new List<int>();

            static Vector2 ProjectUV(Vector3 p, Vector3 n)
            {
                // World-scale UV mapping keeps brick/stone detail consistent
                // across buildings, floors and true-depth window surrounds.
                if(Mathf.Abs(n.y)>.55f)return new Vector2(p.x,p.z);
                return Mathf.Abs(n.x)>.55f ? new Vector2(p.z,p.y) : new Vector2(p.x,p.y);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                Vector3 normal=Vector3.Cross(b-a,d-a).normalized;
                int i=points.Count;
                points.Add(a);points.Add(b);points.Add(c);points.Add(d);
                uv.Add(ProjectUV(a,normal));uv.Add(ProjectUV(b,normal));
                uv.Add(ProjectUV(c,normal));uv.Add(ProjectUV(d,normal));
                indices.Add(i);indices.Add(i+1);indices.Add(i+2);
                indices.Add(i);indices.Add(i+2);indices.Add(i+3);
            }
            public void Box(float x,float y,float z,float w,float h,float d)
            {
                if(w<=Epsilon||h<=Epsilon||d<=Epsilon)return;
                float l=x-w*.5f,r=x+w*.5f;
                float b=y-h*.5f,t=y+h*.5f;
                float n=z-d*.5f,f=z+d*.5f;
                Quad(new Vector3(l,b,n),new Vector3(l,t,n),new Vector3(r,t,n),new Vector3(r,b,n));
                Quad(new Vector3(r,b,f),new Vector3(r,t,f),new Vector3(l,t,f),new Vector3(l,b,f));
                Quad(new Vector3(l,b,f),new Vector3(l,t,f),new Vector3(l,t,n),new Vector3(l,b,n));
                Quad(new Vector3(r,b,n),new Vector3(r,t,n),new Vector3(r,t,f),new Vector3(r,b,f));
                Quad(new Vector3(l,t,n),new Vector3(l,t,f),new Vector3(r,t,f),new Vector3(r,t,n));
                Quad(new Vector3(l,b,f),new Vector3(l,b,n),new Vector3(r,b,n),new Vector3(r,b,f));
            }
            public void Tube(Vector3 from,Vector3 to,float radius,int sides=6)
            {
                Vector3 axis=(to-from).normalized;
                if(axis.sqrMagnitude<Epsilon)return;
                Vector3 right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.91f?
                    Vector3.forward:Vector3.up).normalized;
                Vector3 up=Vector3.Cross(axis,right);
                for(int i=0;i<sides;i++)
                {
                    float a=2*Mathf.PI*i/sides,b=2*Mathf.PI*(i+1)/sides;
                    Vector3 va=(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius;
                    Vector3 vb=(right*Mathf.Cos(b)+up*Mathf.Sin(b))*radius;
                    Quad(from+va,from+vb,to+vb,to+va);
                }
            }
            public void Crown(Vector3 center,Vector3 radius,int sides=9,int levels=4)
            {
                for(int y=0;y<levels;y++)for(int i=0;i<sides;i++)
                {
                    float a=Mathf.PI*y/levels,b=Mathf.PI*(y+1)/levels;
                    float c=2*Mathf.PI*i/sides,d=2*Mathf.PI*(i+1)/sides;
                    Vector3 p=Point(center,radius,a,c),q=Point(center,radius,a,d);
                    Vector3 r=Point(center,radius,b,d),s=Point(center,radius,b,c);
                    Quad(p,q,r,s);
                }
            }
            static Vector3 Point(Vector3 c,Vector3 r,float a,float b)
            {
                return c+new Vector3(Mathf.Sin(a)*Mathf.Cos(b)*r.x,
                    Mathf.Cos(a)*r.y,Mathf.Sin(a)*Mathf.Sin(b)*r.z);
            }
            public GameObject Emit(Transform group,Material material,int layer)
            {
                if(points.Count==0)return null;
                Mesh mesh=new Mesh();
                mesh.name="NONATION Architectural Surface / "+material.name;
                if(points.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(points);mesh.SetUVs(0,uv);
                mesh.SetTriangles(indices,0);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject("MESH / "+material.name);
                go.layer=layer;go.transform.SetParent(group,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.On;
                renderer.receiveShadows=true;
                return go;
            }
        }

        MirArchitectureQuarter(Transform root){this.root=root;}
        Surface Mesh(Material material)
        {
            Surface surface;
            if(!surfaces.TryGetValue(material,out surface))
            {
                surface=new Surface();surfaces.Add(material,surface);
            }
            return surface;
        }
        static uint Noise(uint x,uint y,uint seed)
        {
            unchecked
            {
                uint h=x*374761393u+y*668265263u+seed*2246822519u;
                h=(h^(h>>13))*1274126177u;
                return h^(h>>16);
            }
        }
        // Entirely AGENT-03-authored small tileable albedo maps. They are not
        // photograph assets and have no external licensing or runtime downloads.
        Texture2D Texture(string id)
        {
            Texture2D existing;if(textures.TryGetValue(id,out existing))return existing;
            const int size=256;
            var pixels=new Color32[size*size];
            bool brick=id=="brick", stone=id=="stone", wood=id=="wood";
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                uint hash=Noise((uint)x,(uint)y,73);
                int grain=(int)(hash%21)-10;
                int r,g,b;
                if(brick)
                {
                    int row=y/22, offset=(row%2)*34;
                    bool seam=y%22<2 || (x+offset)%68<2;
                    int shade=(int)(Noise((uint)((x+offset)/68),(uint)row,5)%23)-11;
                    r=seam?102:161+grain+shade;
                    g=seam?98:118+grain+shade;
                    b=seam?92:91+grain+shade;
                }
                else if(stone)
                {
                    bool seam=y%52<2 || (x+(y/52)%2*56)%112<2;
                    int shade=(int)(Noise((uint)(x/112),(uint)(y/52),8)%17)-8;
                    r=seam?151:210+grain/2+shade;
                    g=seam?149:203+grain/2+shade;
                    b=seam?142:190+grain/2+shade;
                }
                else if(wood)
                {
                    int stripe=(x+3*(int)(Noise((uint)(y/9),17,8)%3))%18;
                    r=110+grain/2+(stripe<2?-21:0);
                    g=80+grain/2+(stripe<2?-15:0);
                    b=53+grain/2+(stripe<2?-9:0);
                }
                else
                {
                    bool joint=x%64<2||y%64<2;
                    r=joint?160:204+grain/3;
                    g=joint?161:201+grain/3;
                    b=joint?158:194+grain/3;
                }
                pixels[y*size+x]=new Color32(
                    (byte)Mathf.Clamp(r,0,255),
                    (byte)Mathf.Clamp(g,0,255),
                    (byte)Mathf.Clamp(b,0,255),255);
            }
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,true,false);
            tex.name="NONATION / authored "+id+" albedo";
            tex.wrapMode=TextureWrapMode.Repeat;tex.filterMode=FilterMode.Trilinear;
            tex.anisoLevel=2;tex.SetPixels32(pixels);tex.Apply(true,true);
            textures.Add(id,tex);
            return tex;
        }
        Material Mat(string name,Color tint,float metallic=0,float smoothness=.25f,
            string pattern=null)
        {
            Material found;if(materials.TryGetValue(name,out found))return found;
            Shader qualityShader=Resources.Load<Shader>("Nonation/Architecture/ArchitecturalLit");
            bool texturedShader=qualityShader!=null&&qualityShader.isSupported;
            if(texturedShader)
            {
                found=new Material(qualityShader);
                Color baseColor=pattern==null?tint:Color.white;
                found.SetColor("_BaseColor",baseColor);
                found.SetColor("_Color",baseColor);
                found.SetFloat("_Metallic",metallic);
                found.SetFloat("_Smoothness",smoothness);
            }
            else
            {
                // Fail visibly but safely if an URP material cannot load,
                // instead of producing a huge pure-white textured city.
                found=MirRenderMaterials.Create(tint,metallic,smoothness);
                if(materials.Count==0)
                    Debug.LogWarning("NONATION_ARCH_SHADER_FALLBACK: authored texture shader " +
                        "missing/unsupported; architecture will use untextured materials.");
            }
            found.name="NONATION / "+name;found.enableInstancing=true;
            if(pattern!=null)
            {
                Texture2D tex=Texture(pattern);
                if(found.HasProperty("_BaseMap"))found.SetTexture("_BaseMap",tex);
                if(found.HasProperty("_MainTex"))found.SetTexture("_MainTex",tex);
                if(found.HasProperty("_BaseMap"))found.SetTextureScale("_BaseMap",new Vector2(.62f,.62f));
                if(found.HasProperty("_MainTex"))found.SetTextureScale("_MainTex",new Vector2(.62f,.62f));
            }
            materials.Add(name,found);
            return found;
        }
        void Box(Material m,float x,float y,float z,float w,float h,float d)
        {Mesh(m).Box(x,y,z,w,h,d);}
        void Tube(Material m,Vector3 a,Vector3 b,float width,int sides=6)
        {Mesh(m).Tube(a,b,width,sides);}
        void Crown(Material m,Vector3 c,Vector3 r)
        {Mesh(m).Crown(c,r);}

        Material Facade(Building p)
        {
            switch(p.style)
            {
                case 0:return Mat("Warm brick",new Color(.63f,.48f,.39f),0,.22f,"brick");
                case 1:return Mat("Limestone panels",new Color(.82f,.80f,.75f),0,.22f,"stone");
                case 2:return Mat("Cream mineral cladding",new Color(.83f,.81f,.77f),0,.23f,"plaster");
                default:return Mat("Timber wall accents",new Color(.45f,.34f,.26f),0,.24f,"wood");
            }
        }

        void Structure(Building p,float front,float height,Material wall)
        {
            float halfX=p.width*.5f,halfZ=p.depth*.5f;
            var w=Mesh(wall);
            // No front-face polygon: actual holes are left by segmented walls,
            // with lit/deep window bays behind. The other five exterior faces
            // remain closed so buildings have proper silhouettes from the sky.
            w.Quad(new Vector3(p.x-halfX,0,p.z+halfZ),new Vector3(p.x+halfX,0,p.z+halfZ),
                new Vector3(p.x+halfX,height,p.z+halfZ),new Vector3(p.x-halfX,height,p.z+halfZ));
            w.Quad(new Vector3(p.x-halfX,0,p.z-halfZ),new Vector3(p.x-halfX,0,p.z+halfZ),
                new Vector3(p.x-halfX,height,p.z+halfZ),new Vector3(p.x-halfX,height,p.z-halfZ));
            w.Quad(new Vector3(p.x+halfX,0,p.z+halfZ),new Vector3(p.x+halfX,0,p.z-halfZ),
                new Vector3(p.x+halfX,height,p.z-halfZ),new Vector3(p.x+halfX,height,p.z+halfZ));
            w.Quad(new Vector3(p.x-halfX,height,p.z-halfZ),new Vector3(p.x-halfX,height,p.z+halfZ),
                new Vector3(p.x+halfX,height,p.z+halfZ),new Vector3(p.x+halfX,height,p.z-halfZ));

            // All upper storeys have shallow floor plates, visible through
            // glazing and strongly framing the façade rather than a cube shell.
            var concrete=Mat("Light architectural concrete",new Color(.75f,.73f,.67f),0,.19f);
            for(int level=1;level<p.floors;level++)
                Box(concrete,p.x,level*FloorHeight,p.z,p.width+.15f,.15f,p.depth+.1f);
            // Retain the exact earlier solid collision volume; POI entrances
            // remain visual-only until AGENT 01 implements interior traversal.
            var colliderObj=new GameObject(p.name+" / unchanged gameplay collision volume");
            colliderObj.transform.SetParent(root,false);
            var collision=colliderObj.AddComponent<BoxCollider>();
            collision.center=new Vector3(p.x,height*.5f,p.z);
            collision.size=new Vector3(p.width,height,p.depth);
        }

        void Front(Building p,float front,Material wall,float height)
        {
            bool retail=p.retail;
            var trim=Mat("Graphite anodized aluminium",new Color(.17f,.20f,.22f),.66f,.42f);
            var dark=Mat("Deep window shadows",new Color(.075f,.10f,.12f),0,.08f);
            var glass=Mat("Smoked teal double glazing",new Color(.19f,.32f,.38f),.06f,.82f);
            var lit=Mat("Warm interior glazing",new Color(.67f,.51f,.32f),0,.61f);
            var metal=Mat("Champagne window accents",new Color(.65f,.61f,.51f),.36f,.44f);
            var concrete=Mat("Light architectural concrete",new Color(.75f,.73f,.67f),0,.19f);
            int bays=(p.width>13.5f?4:3);
            float cell=p.width/bays;
            for(int floor=0;floor<p.floors;floor++)for(int bay=0;bay<bays;bay++)
            {
                float x=p.x-p.width*.5f+(bay+.5f)*cell;
                float bottom=floor*FloorHeight;
                bool shop=retail&&floor==0;
                float apertureW=cell*(shop?.82f:.62f);
                float apertureH=shop?2.83f:1.97f;
                float sill=shop?.28f:.85f;
                float top=sill+apertureH;
                float gap=cell-apertureW;
                // Four real wall panels form an open glazed bay.
                Box(wall,x-(cell*.5f-gap*.25f),bottom+FloorHeight*.5f,
                    front+.015f,gap*.5f,FloorHeight,.32f);
                Box(wall,x+(cell*.5f-gap*.25f),bottom+FloorHeight*.5f,
                    front+.015f,gap*.5f,FloorHeight,.32f);
                Box(wall,x,bottom+sill*.5f,front+.015f,
                    apertureW,sill,.32f);
                Box(wall,x,bottom+(top+FloorHeight)*.5f,front+.015f,
                    apertureW,FloorHeight-top,.32f);

                // Actual 0.35 m-deep dark bay behind the open wall face.
                Box(dark,x,bottom+sill+apertureH*.5f,front+.35f,
                    apertureW+.06f,apertureH+.04f,.10f);
                bool warm=((floor*11+bay*7+p.index*3)%5)<2;
                Material pane=warm?lit:glass;
                Box(pane,x,bottom+sill+apertureH*.5f,front+.10f,
                    apertureW-.08f,apertureH-.09f,.027f);
                // Reveal frames are real extruded thin profiles, not decals.
                Box(trim,x-apertureW*.5f,bottom+sill+apertureH*.5f,front-.16f,
                    .115f,apertureH+.10f,.20f);
                Box(trim,x+apertureW*.5f,bottom+sill+apertureH*.5f,front-.16f,
                    .115f,apertureH+.10f,.20f);
                Box(trim,x,bottom+sill-.03f,front-.16f,
                    apertureW+.12f,.105f,.20f);
                Box(trim,x,bottom+sill+apertureH+.03f,front-.16f,
                    apertureW+.12f,.105f,.20f);
                // Asymmetric transom reinforces the scale of the shopfront.
                if(shop)
                {
                    Box(metal,x,bottom+2.20f,front-.19f,
                        apertureW,.055f,.11f);
                    Box(trim,x,bottom+sill+apertureH*.49f,front-.19f,
                        .065f,apertureH,.11f);
                    if(bay==bays/2)
                        Box(metal,x+.18f,bottom+1.35f,front-.27f,.044f,.36f,.07f);
                    // Display shelves are inside the *actual* glazed opening.
                    // Geometry is visible from the street, not a picture of it.
                    Box(concrete,x,bottom+.71f,front+.60f,
                        apertureW*.70f,.12f,.58f);
                    for(int n=0;n<3;n++)
                        Box(n%2==0?metal:concrete,
                            x+(n-1)*apertureW*.21f,bottom+1.02f,
                            front+.60f,.18f,.51f,.20f);
                }
                else
                {
                    Box(concrete,x,bottom+sill-.10f,front-.30f,
                        apertureW+.43f,.13f,.40f);
                    Box(trim,x,bottom+sill+apertureH*.49f,front-.20f,
                        .042f,apertureH,.10f);
                }
            }
            // Vertical articulation and belt courses break the box profile.
            for(int level=1;level<p.floors;level++)
                Box(concrete,p.x,level*FloorHeight-.03f,front-.31f,
                    p.width+.24f,.24f,.38f);
            Box(concrete,p.x,height-.15f,front-.25f,p.width+.38f,.28f,.45f);
            if(retail)RetailDetail(p,front,height);
        }

        void RetailDetail(Building p,float front,float height)
        {
            var fascia=Mat("Retail fascia graphite",new Color(.13f,.17f,.19f),.2f,.36f);
            var gold=Mat("Retail lettering warm ivory",new Color(.94f,.85f,.68f),.04f,.34f);
            var soft=Mat("Shop canopy natural linen",new Color(.78f,.70f,.57f),0,.18f);
            var wood=Mat("Oak shop timber",Color.white,0,.25f,"wood");
            // Floating fascia in front of real recessed shop windows.
            Box(fascia,p.x,3.25f,front-.40f,p.width-.65f,.68f,.55f);
            DrawSign(p.sign,p.x,3.23f,front-.735f,
                Mathf.Min(.42f,(p.width-1.4f)/(p.sign.Length*1.4f)),gold);
            Box(soft,p.x,2.97f,front-.97f,p.width-.60f,.11f,1.40f);
            for(int i=-1;i<=1;i+=2)
                Box(wood,p.x+i*(p.width*.46f),2.8f,front-1.20f,
                    .11f,.19f,1.08f);
            var stone=Mat("Limestone panels",Color.white,0,.22f,"stone");
            var leaves=Mat("Planter leaves deep green",new Color(.29f,.39f,.24f));
            // Carefully set aside from the central entrance line.
            for(int i=-1;i<=1;i+=2)
            {
                float x=p.x+i*(p.width*.46f);
                Box(stone,x,.38f,front-1.12f,.66f,.67f,.70f);
                for(int j=0;j<3;j++)
                {
                    Crown(j%2==0?leaves:Mat("Planter leaves bright",new Color(.36f,.49f,.26f)),
                        new Vector3(x+(j-1)*.14f,.94f,front-1.12f),
                        new Vector3(.31f,.41f,.28f));
                }
            }
        }

        // Original upright-stroke glyphs: real 3D metal-lit tubing in the
        // façade, avoids dynamic OS fonts being unavailable in WebGL.
        static readonly Vector2[] GlyphFrom={
            new Vector2(0,1),new Vector2(0,1),new Vector2(1,1),
            new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(1,.5f),
            new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),
            new Vector2(0,.5f),new Vector2(1,.5f),
            new Vector2(.5f,1),new Vector2(.5f,.5f)};
        static readonly Vector2[] GlyphTo={
            new Vector2(1,1),new Vector2(0,.5f),new Vector2(1,.5f),
            new Vector2(1,.5f),new Vector2(0,0),new Vector2(1,0),
            new Vector2(1,0),new Vector2(1,.5f),new Vector2(0,.5f),
            new Vector2(1,0),new Vector2(0,0),
            new Vector2(.5f,.5f),new Vector2(.5f,0)};
        static string Letter(char c)
        {
            switch(char.ToUpperInvariant(c))
            {
                case 'A': return "0,1,2,3,4,5";
                case 'B': return "0,1,3,4,5,6,2";
                case 'C': return "0,1,4,6";
                case 'D': return "0,1,2,4,5,6";
                case 'E': return "0,1,3,4,6";
                case 'F': return "0,1,3,4";
                case 'G': return "0,1,4,5,6";
                case 'H': return "1,2,3,4,5";
                case 'I': return "0,6,11,12";
                case 'K': return "1,4,7,10";
                case 'L': return "1,4,6";
                case 'M': return "1,2,4,5,7,8";
                case 'N': return "1,2,4,5,7,9";
                case 'O': return "0,1,2,4,5,6";
                case 'P': return "0,1,2,3,4";
                case 'R': return "0,1,2,3,4,10";
                case 'S': return "0,1,3,5,6";
                case 'T': return "0,11,12";
                case 'U': return "1,2,4,5,6";
                case 'V': return "1,2,9,10";
                case 'Y': return "1,2,3,5,6";
                case 'Z': return "0,6,8,9";
                default: return "";
            }
        }
        void DrawSign(string text,float x,float y,float z,float unit,Material material)
        {
            if(string.IsNullOrEmpty(text))return;
            float step=unit*1.36f;
            float width=text.Length*step;
            float first=x-width*.5f+step*.18f;
            for(int i=0;i<text.Length;i++)
            {
                var strokes=Letter(text[i]);
                string[] tokens=strokes.Split(',');
                for(int n=0;n<tokens.Length;n++)
                {
                    int segment;
                    if(!int.TryParse(tokens[n],out segment)||
                       segment<0||segment>=GlyphFrom.Length)continue;
                    Vector2 a=GlyphFrom[segment],b=GlyphTo[segment];
                    Tube(material,new Vector3(first+i*step+a.x*unit,y+(a.y-.5f)*unit,z),
                        new Vector3(first+i*step+b.x*unit,y+(b.y-.5f)*unit,z),
                        unit*.035f,5);
                }
            }
        }

        void SideFenestration(Building p,Material wall)
        {
            var trim=Mat("Graphite anodized aluminium",new Color(.17f,.20f,.22f),.66f,.42f);
            var glass=Mat("Smoked teal double glazing",new Color(.19f,.32f,.38f),.06f,.82f);
            var stone=Mat("Light architectural concrete",new Color(.75f,.73f,.67f),0,.19f);
            for(int side=-1;side<=1;side+=2)
                for(int floor=1;floor<p.floors;floor++)for(int bay=0;bay<2;bay++)
                {
                    float x=p.x+side*p.width*.5f;
                    float z=p.z-p.depth*.5f+(bay+.65f)*p.depth/2.4f;
                    float y=floor*FloorHeight+2f;
                    Box(glass,x+side*.08f,y,z,.10f,1.65f,1.5f);
                    Box(trim,x+side*.13f,y-0.81f,z,.13f,.09f,1.65f);
                    Box(trim,x+side*.13f,y+0.81f,z,.13f,.09f,1.65f);
                    for(int edge=-1;edge<=1;edge+=2)
                        Box(trim,x+side*.13f,y,z+edge*.81f,.13f,1.7f,.09f);
                    Box(stone,x+side*.20f,y-.95f,z,.22f,.12f,1.80f);
                }
        }

        void Balcony(Building p,float front,int floor,int bay)
        {
            float w=p.width/3.9f;
            float x=p.x-p.width*.5f+(bay+.5f)*p.width/4f;
            float baseY=floor*FloorHeight+.84f;
            var concrete=Mat("Light architectural concrete",new Color(.75f,.73f,.67f),0,.19f);
            var iron=Mat("Graphite anodized aluminium",new Color(.17f,.20f,.22f),.66f,.42f);
            var stone=Mat("Limestone panels",Color.white,0,.22f,"stone");
            var leaves=Mat("Planter leaves bright",new Color(.36f,.49f,.26f));
            Box(concrete,x,baseY,front-.67f,w,.18f,1.40f);
            float z=front-1.33f;
            Tube(iron,new Vector3(x-w*.48f,baseY+.86f,z),
                new Vector3(x+w*.48f,baseY+.86f,z),.044f,8);
            for(int i=0;i<6;i++)
            {
                float xx=x+(i/5f-.5f)*w*.96f;
                Tube(iron,new Vector3(xx,baseY+.09f,z),
                    new Vector3(xx,baseY+.86f,z),.021f);
            }
            Box(stone,x+w*.26f,baseY+.18f,z+.22f,.65f,.28f,.28f);
            for(int i=0;i<2;i++)
                Crown(leaves,new Vector3(x+w*.15f+i*.22f,baseY+.46f,z+.22f),
                    new Vector3(.30f,.39f,.27f));
        }

        void Roof(Building p,float height,float front)
        {
            var stone=Mat("Limestone panels",Color.white,0,.22f,"stone");
            var concrete=Mat("Light architectural concrete",new Color(.75f,.73f,.67f),0,.19f);
            var iron=Mat("Graphite anodized aluminium",new Color(.17f,.20f,.22f),.66f,.42f);
            var timber=Mat("Roof pergola timber",Color.white,0,.3f,"wood");
            var leaves=Mat("Planter leaves deep green",new Color(.29f,.39f,.24f));
            var glass=Mat("Smoked teal double glazing",new Color(.19f,.32f,.38f),.06f,.82f);
            Box(stone,p.x,height+.13f,p.z,p.width+.42f,.27f,p.depth+.42f);
            for(int sign=-1;sign<=1;sign+=2)
            {
                Box(stone,p.x,height+.55f,p.z+sign*p.depth*.49f,
                    p.width+.45f,.75f,.30f);
                Box(stone,p.x+sign*p.width*.49f,height+.55f,p.z,
                    .30f,.75f,p.depth+.35f);
            }
            if(p.terrace)
            {
                // Actual pergola canopy over a roof terrace: top beams cast
                // shadows, openings allow sunlight, rooftop foliage grounds it.
                float roofY=height+.90f;
                for(int side=-1;side<=1;side+=2)
                    for(int end=-1;end<=1;end+=2)
                        Tube(iron,new Vector3(p.x+side*p.width*.33f,roofY,p.z+end*p.depth*.29f),
                            new Vector3(p.x+side*p.width*.33f,roofY+2.45f,
                                p.z+end*p.depth*.29f),.095f,8);
                for(int j=0;j<6;j++)
                {
                    float x=p.x-p.width*.33f+j*p.width*.132f;
                    Box(timber,x,roofY+2.48f,p.z,
                        .12f,.23f,p.depth*.67f);
                }
                for(int sign=-1;sign<=1;sign+=2)
                {
                    Box(stone,p.x+sign*p.width*.37f,roofY+.27f,p.z,
                        .64f,.49f,p.depth*.63f);
                    for(int j=0;j<5;j++)
                        Crown(leaves,new Vector3(p.x+sign*p.width*.37f,
                            roofY+.76f,p.z+(j-2)*p.depth*.105f),
                            new Vector3(.37f,.51f,.37f));
                }
            }
            else
            {
                for(int n=0;n<2;n++)
                {
                    float dx=(n==0?-.23f:.24f)*p.width;
                    Box(concrete,p.x+dx,height+.69f,p.z,.9f,.72f,1.40f);
                    Box(glass,p.x+dx,height+1.09f,p.z,.95f,.09f,1.46f);
                }
            }
        }

        void BuildBuilding(Building p)
        {
            float height=p.floors*FloorHeight;
            float front=p.z-p.depth*.5f;
            Material wall=Facade(p);
            Structure(p,front,height,wall);
            Front(p,front,wall,height);
            SideFenestration(p,wall);
            if(!p.retail && p.floors>=3)
                for(int floor=1;floor<p.floors;floor++)
                    if((floor+p.index)%2==0)
                        Balcony(p,front,floor,(floor+p.index)%4);
            Roof(p,height,front);
        }

        void Emit()
        {
            Transform parent=new GameObject("NONATION / Modern architectural quarter").transform;
            parent.SetParent(root,false);
            foreach(var pair in surfaces)
                pair.Value.Emit(parent,pair.Key,root.gameObject.layer);
        }

        public static void Build(Transform root)
        {
            if(root==null)throw new ArgumentNullException("root");
            var district=new MirArchitectureQuarter(root);
            // Coordinates, dimensions and stories exactly follow existing
            // MirCity POIs and home spawns. Only building art is replaced.
            var plans=new List<Building>{
                new Building("Grocery store","NOVA MART",20,-20,13,14,2,2,0,true,false),
                new Building("Corner cafe","NONATION CAFE",56,20,12,12,2,0,1,true,true),
                new Building("Auto workshop","AUTO SERVICE",-58,-22,15,15,2,3,2,true,false),
                new Building("Town hall","CITY HALL",-20,-20,14,14,3,1,3,false,true)
            };
            for(int i=0;i<8;i++)
            {
                int col=i%4,row=i/4;
                plans.Add(new Building("Residential block "+i,"",
                    -66+col*36,56+row*38,14,13,3+i%3,i%4,i+4,
                    false,i==2||i==5));
            }
            foreach(Building plan in plans)district.BuildBuilding(plan);
            district.Emit();
        }
    }
}
