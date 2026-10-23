namespace MIR
{
    // Pure decision-making: Unity and the headless server use identical needs priorities.
    public static class MirCitizenDecision
    {
        public static string Destination(MirWorldState world,MirCitizen resident)
        {
            if(resident.satiety<40f)
            {
                if(resident.inventoryFood>0)return resident.homeId;
                var shop=world.Business("market");
                if(shop!=null && shop.stock>0 && shop.price>0 && resident.cash>=shop.price)
                    return "market";
            }
            if(resident.energy<25f)return resident.homeId;
            float hour=world.Hour;
            if(hour>=8f && hour<17f && world.Business(resident.workplaceId)!=null)
                return resident.workplaceId;
            if(resident.mood<45f && world.Business("cafe")!=null)return "cafe";
            if(hour>=17f && hour<21f && resident.gaitSeed>.53f)return "town";
            return resident.homeId;
        }
    }
}
