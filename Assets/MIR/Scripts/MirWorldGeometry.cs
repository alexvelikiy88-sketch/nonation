using System;

namespace MIR
{
    // Shared coordinates for Unity visuals and the backend navigation engine.
    public struct MirMapPoint
    {
        public float x, z;
        public MirMapPoint(float x, float z) { this.x=x; this.z=z; }
    }
    public static class MirWorldGeometry
    {
        public static MirMapPoint BusinessEntry(string id, MirBusiness business=null)
        {
            switch(id)
            {
                case "market": return new MirMapPoint(20,-28);
                case "cafe": return new MirMapPoint(56,12);
                case "garage": return new MirMapPoint(-58,-31);
                case "town": return new MirMapPoint(-20,-28);
                case "farm": return new MirMapPoint(-110,59);
                case "packing": return new MirMapPoint(-105,32);
                case "warehouse": return new MirMapPoint(-80,27);
                case "fuel": return new MirMapPoint(-80,-41);
                default: return business==null?new MirMapPoint(0,0):new MirMapPoint(business.x,business.z);
            }
        }
        public static MirMapPoint HomeEntry(MirWorldState world, MirCitizen citizen)
        {
            int index=world.citizens.IndexOf(citizen);
            if(index<0)return new MirMapPoint(citizen.x,citizen.z);
            int block=index%8;
            return new MirMapPoint(-66+(block%4)*36,56+(block/4)*38-8.5f);
        }
        public static MirMapPoint[] SidewalkRoute(float ox,float oz,float tx,float tz)
        {
            float startX=(float)Math.Round(ox/40f)*40f+6f;
            float startZ=(float)Math.Round(oz/40f)*40f+6f;
            float endX=(float)Math.Round(tx/40f)*40f+6f;
            float endZ=(float)Math.Round(tz/40f)*40f+6f;
            return new[]{
                new MirMapPoint(startX,oz),new MirMapPoint(startX,startZ),
                new MirMapPoint(endX,startZ),new MirMapPoint(endX,endZ),
                new MirMapPoint(tx,tz)
            };
        }
    }
}
