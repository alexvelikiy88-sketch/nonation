using System;
using System.Collections.Generic;

namespace MIR
{
    public enum MirPlaceKind { Home, Store, Cafe, Work, Farm, Packing, Warehouse, Garage, Fuel, Park, CityHall }
    public enum MirActivity { Idle, Walking, Running, Talking, Phone, Sitting, Driving, Working, Harvesting, Carrying, Eating, Sleeping }
    public enum MirOrderStatus { Created, Loading, Driving, Unloading, Returning, Completed, Cancelled }

    [Serializable] public class MirCitizen
    {
        public string id, name, homeId, workplaceId, profession, look = "default";
        public int cash = 12000, skill = 1, inventoryFood = 0, daysWorked = 0;
        public float satiety = 78f, energy = 80f, hydration = 78f, mood = 72f;
        public float x, z, gaitSeed, nextWorkMinute;
        public bool player;
    }
    [Serializable] public class MirBusiness
    {
        public string id, name, kind, productId;
        public int cash, stock, capacity, price, water = 50, seed = 80;
        public float x, z;
    }
    [Serializable] public class MirTransaction
    {
        public string id, from, to, cause;
        public int amount;
        public float minute;
    }
    [Serializable] public class MirFreightOrder
    {
        public string id, supplierId, consumerId, productId;
        public MirOrderStatus status;
        public int quantity, escrow, freightFee;
        public bool paid, stockReserved;
        public float progress;
    }
    [Serializable] public class MirEvent
    {
        public float minute;
        public string text;
    }
    [Serializable] public class MirWorldState
    {
        public int version = 1, seed = 81523, transactionSequence = 1, jobSequence = 1;
        public float gameMinutes = 8f * 60f;
        public bool nighttimeLights = true;
        public List<MirCitizen> citizens = new List<MirCitizen>();
        public List<MirBusiness> businesses = new List<MirBusiness>();
        public List<MirTransaction> transactions = new List<MirTransaction>();
        public List<MirFreightOrder> freight = new List<MirFreightOrder>();
        public List<MirEvent> events = new List<MirEvent>();
        public int cityTreasury = 100000, externalMoney = 1200000;

        public MirCitizen Citizen(string id) { return citizens.Find(p => p.id == id); }
        public MirBusiness Business(string id) { return businesses.Find(p => p.id == id); }
        public MirFreightOrder ActiveFreight() { return freight.Find(o => o.status != MirOrderStatus.Completed && o.status != MirOrderStatus.Cancelled); }
        public string NewId(string prefix)
        {
            // Never wrap the persisted 32-bit sequence: duplicate transaction
            // IDs could otherwise bypass replay protection or poison saves.
            if (transactionSequence <= 0 || transactionSequence == int.MaxValue)
                return String.Empty;
            return prefix + "-" + transactionSequence++;
        }
        public float Hour { get { return (gameMinutes / 60f) % 24f; } }
        public int Day { get { return 1 + (int)Math.Floor(gameMinutes / 1440f); } }
        public static MirWorldState NewWorld(int npcCount = 39)
        {
            var s = new MirWorldState();
            s.businesses.Add(new MirBusiness {id="market", name="Городской рынок", kind="store", productId="bread", price=380, cash=180000, stock=55, capacity=120, x=20,z=-20});
            s.businesses.Add(new MirBusiness {id="cafe", name="Кафе у площади", kind="cafe", productId="coffee", price=480, cash=78000, stock=35, capacity=60,x=20,z=20});
            s.businesses.Add(new MirBusiness {id="farm", name="Пригородные поля", kind="farm", productId="grain", price=180,cash=20000,stock=100,capacity=250,x=-108,z=80});
            s.businesses.Add(new MirBusiness {id="packing", name="Пищевой цех", kind="packing", productId="bread", price=290,cash=52000,stock=120,capacity=300,x=-105,z=43});
            s.businesses.Add(new MirBusiness {id="warehouse", name="Оптовый склад", kind="warehouse", productId="bread", price=260,cash=80000,stock=150,capacity=320,x=-80,z=40});
            s.businesses.Add(new MirBusiness {id="garage", name="Гараж и сервис", kind="garage", productId="parts", price=800,cash=44000,stock=16,capacity=100,x=-60,z=-20});
            s.businesses.Add(new MirBusiness {id="fuel", name="АЗС", kind="fuel", productId="fuel", price=100,cash=60000,stock=150,capacity=200,x=-80,z=-50});
            s.businesses.Add(new MirBusiness {id="town", name="Муниципалитет", kind="townhall", productId="service",price=0,cash=40000,stock=0,capacity=0,x=-20,z=-20});
            var random = new System.Random(s.seed);
            for (int i = 0; i <= npcCount; i++)
            {
                float hx = -65f + (i % 8) * 17f;
                float hz = 62f - ((i / 8) % 5) * 29f;
                MirCitizen r = new MirCitizen
                {
                    id = i == 0 ? "player-001" : "citizen-" + i.ToString("000"),
                    name = i == 0 ? "Мой персонаж" : "Житель " + i.ToString("000"),
                    homeId = "home-" + i.ToString("000"),
                    workplaceId = i % 7 == 0 ? "garage" : i % 5 == 0 ? "warehouse" : i % 3 == 0 ? "cafe" : "market",
                    profession = i % 7 == 0 ? "mechanic" : i % 5 == 0 ? "courier" : i % 3 == 0 ? "barista" : "clerk",
                    x = hx, z = hz, gaitSeed = (float)random.NextDouble(), cash = 9000 + random.Next(9500),
                    player = i == 0
                };
                if (i == 0) { r.x = 8f; r.z = 8f; r.cash = 18000; r.look="Maid"; }
                else if (i % 7==0) r.look="Pinkdress";
                else if (i % 8==0) r.look="GIGN";
                s.citizens.Add(r);
            }
            s.events.Add(new MirEvent { minute=s.gameMinutes, text="MIR Dockside: город и жители созданы." });
            return s;
        }
    }
}
