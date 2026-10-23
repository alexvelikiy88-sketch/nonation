using MIR;

static void Check(bool success, string description)
{
    if (!success) throw new Exception("FAIL: " + description);
    Console.WriteLine("PASS: " + description);
}
var state = MirWorldState.NewWorld();
Check(state.citizens.Count == 40, "player and 39 NPCs");
var economy = new MirEconomy(state);
int moneyBefore = economy.TotalKnownMoney();
var hero = state.Citizen("player-001")!;
var market = state.Business("market")!;
int stockBefore = market.stock;
Check(economy.BuyFood(hero, market), "purchase transfers money");
Check(hero.inventoryFood == 1 && market.stock == stockBefore - 1, "purchase changes inventory and stock");
Check(economy.Eat(hero) && hero.inventoryFood == 0, "food consumed once");
Check(economy.Work(hero), "paid work");
Check(!economy.Work(hero), "salary cooldown");
var order = economy.CreateRestock();
Check(order != null, "warehouse freight reservation");
Check(economy.SetFreightStage(order!, MirOrderStatus.Loading), "loading");
Check(economy.SetFreightStage(order!, MirOrderStatus.Driving), "driving");
Check(economy.SetFreightStage(order!, MirOrderStatus.Unloading), "unloading");
Check(!economy.SetFreightStage(order!, MirOrderStatus.Unloading), "no duplicate unloading");
Check(economy.SetFreightStage(order!, MirOrderStatus.Returning), "returning");
Check(economy.SetFreightStage(order!, MirOrderStatus.Completed), "delivery completed");
Check(economy.TotalKnownMoney() == moneyBefore, "money and escrow conserved");
var loop = new MirSimulationLoop(state, economy);
float minuteBefore = state.gameMinutes;
for (int i = 0; i < 20; ++i) loop.Advance(.1f);
Check(state.gameMinutes > minuteBefore, "fixed timestep advances simulated time");
Console.WriteLine("Shared simulation domain tests passed.");

static void InvariantAndRestartTests()
{
    var s = MirWorldState.NewWorld();
    var economy = new MirEconomy(s);
    var hero = s.Citizen("player-001")!;
    var shop = s.Business("market")!;
    int oldHero = hero.cash;
    int oldStore = shop.cash;
    Check(!economy.Transfer("self-transfer", hero.id, hero.id, 100, "invalid"), "self-transfers rejected");
    Check(!economy.Transfer("negative", hero.id, shop.id, -100, "invalid"), "negative transfer rejected");
    Check(economy.Transfer("test-unique-id", hero.id, shop.id, 100, "purchase"), "valid transfer");
    Check(!economy.Transfer("test-unique-id", hero.id, shop.id, 100, "replay"), "transaction replay rejected");
    Check(hero.cash == oldHero - 100 && shop.cash == oldStore + 100, "transaction changes balances exactly once");
    var loaded = new MirEconomy(s);
    Check(!loaded.Transfer("test-unique-id", hero.id, shop.id, 100, "reload replay"),
        "transaction idempotency survives an economy reload");

    var external = new MirCitizen { id = hero.id, cash = int.MaxValue };
    Check(!loaded.Eat(external) && !loaded.Work(external), "foreign citizen objects are rejected");
    var counterfeit = new MirBusiness { id = shop.id, stock = 1, price = 1, cash = 1 };
    Check(!loaded.BuyFood(hero, counterfeit), "foreign business objects are rejected");
    var forgedOrder = new MirFreightOrder
    {
        id = "forged", supplierId = "warehouse", consumerId = "market",
        productId = "bread", quantity = 99999, escrow = 100, freightFee = 200,
        stockReserved = true, status = MirOrderStatus.Driving
    };
    int moneyBefore = loaded.TotalKnownMoney(), stockBefore = shop.stock;
    Check(!loaded.SetFreightStage(forgedOrder, MirOrderStatus.Unloading) && !loaded.CancelFreight(forgedOrder),
        "nonexistent freight cannot mint goods or cash");
    Check(moneyBefore == loaded.TotalKnownMoney() && stockBefore == shop.stock, "forged order has no side effects");
    Check(loaded.CreateRestock(qty: int.MaxValue) == null, "overflow and impossible quantities refused");
    Check(loaded.CreateRestock(consumerId: "market", supplierId: "fuel", qty: 1) == null,
        "different product types cannot be shipped as restock");

    var order = loaded.CreateRestock(qty: 5);
    Check(order != null, "cancellable freight reserved");
    int warehouseReserved = s.Business("warehouse")!.stock;
    Check(loaded.CancelFreight(order!), "cancel unresolved shipment");
    Check(!loaded.CancelFreight(order!), "shipment cannot be cancelled twice");
    Check(s.Business("warehouse")!.stock == warehouseReserved + 5, "cancel returns real supplier inventory");
    Check(loaded.TotalKnownMoney() == moneyBefore, "escrow fully refunded on cancellation");

    var tickWorld = MirWorldState.NewWorld();
    var tickEconomy = new MirEconomy(tickWorld);
    var farm = tickWorld.Business("farm")!;
    var packing = tickWorld.Business("packing")!;
    int farmBefore = farm.stock, packingBefore = packing.stock;
    tickEconomy.Tick(0.05f);
    Check(farm.stock == farmBefore && packing.stock == packingBefore, "first simulation tick does not manufacture free products");
    tickWorld.gameMinutes = 720f;
    tickEconomy.Tick(0.05f);
    Check(farm.stock == farmBefore - 10 && packing.stock == packingBefore + 8,
        "production happens at an actual simulated six-hour boundary");
    var resumed = new MirEconomy(tickWorld);
    resumed.Tick(0.05f);
    Check(farm.stock == farmBefore - 10 && packing.stock == packingBefore + 8,
        "server restart does not duplicate manufacturing output");
}
InvariantAndRestartTests();
