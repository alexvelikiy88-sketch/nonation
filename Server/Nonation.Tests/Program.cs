using MIR;
using Nonation.Server;
using Microsoft.Extensions.Logging.Abstractions;

static void Check(bool success, string description)
{
    if (!success) throw new Exception("FAIL: " + description);
    Console.WriteLine("PASS: " + description);
}
var state = MirWorldState.NewWorld();
Check(state.citizens.Count == 40, "player and 39 NPCs");
var economy = new MirEconomy(state);
long moneyBefore = economy.TotalKnownMoney();
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
    long moneyBefore = loaded.TotalKnownMoney();
    int stockBefore = shop.stock;
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

static void FreightStateRegressionTests()
{
    var world = MirWorldState.NewWorld();
    var economy = new MirEconomy(world);
    long totalBefore = economy.TotalKnownMoney();
    var supplier = world.Business("warehouse")!;
    var buyer = world.Business("market")!;
    int supplierBefore = supplier.stock, buyerBefore = buyer.stock;
    var order = economy.CreateRestock(qty: 4)!;
    Check(order != null, "freight created with real stock and escrow");
    int heldEscrow = order.escrow, heldFee = order.freightFee;
    int reservedSupplierStock = supplier.stock;

    // A corrupted saved order could previously be marked Completed without
    // unloading/settling escrow, leaving credits permanently stranded.
    order.status = MirOrderStatus.Returning;
    Check(!economy.SetFreightStage(order, MirOrderStatus.Completed),
        "unpaid Returning order cannot be completed");
    Check(!economy.CancelFreight(order),
        "corrupt unpaid Returning order cannot be refunded as valid transit");
    Check(order.status == MirOrderStatus.Returning && supplier.stock == reservedSupplierStock &&
          buyer.stock == buyerBefore && economy.TotalKnownMoney() == totalBefore,
        "rejected corrupt freight stage has no side effects");

    order.status = MirOrderStatus.Created;
    Check(economy.SetFreightStage(order, MirOrderStatus.Loading) &&
          economy.SetFreightStage(order, MirOrderStatus.Driving),
        "valid reservation progresses through loading and driving");
    order.paid = true;
    Check(!economy.SetFreightStage(order, MirOrderStatus.Unloading),
        "forged prepaid flag cannot skip goods and escrow settlement");
    order.paid = false;

    order.productId = "fuel";
    Check(!economy.SetFreightStage(order, MirOrderStatus.Unloading) &&
          !economy.CancelFreight(order), "mismatched product cannot settle or refund");
    order.productId = "bread";

    Check(economy.SetFreightStage(order, MirOrderStatus.Unloading),
        "validated transit pays and unloads exactly once");
    Check(buyer.stock == buyerBefore + 4 && supplier.stock == supplierBefore - 4 &&
          order.paid && !order.stockReserved &&
          order.escrow == 0 && order.freightFee == 0,
        "unloading consumes reserved stock and closes escrow");
    Check(!economy.CancelFreight(order) && !economy.SetFreightStage(order, MirOrderStatus.Unloading),
        "settled freight can neither refund nor unload a second time");
    Check(economy.SetFreightStage(order, MirOrderStatus.Returning) &&
          economy.SetFreightStage(order, MirOrderStatus.Completed),
        "settled freight may return and finish");
    Check(economy.TotalKnownMoney() == totalBefore, "complete freight conserves money");
    Check(heldEscrow > 0 && heldFee > 0, "freight reservation held actual funds");

    // A half-complete freight saved during loading can still finish after reload.
    var toResume = MirWorldState.NewWorld();
    var beforeResume = new MirEconomy(toResume);
    var reserved = beforeResume.CreateRestock(qty: 3)!;
    Check(reserved != null && beforeResume.SetFreightStage(reserved, MirOrderStatus.Loading),
        "freight can be persisted in Loading state");
    long pendingMoney = beforeResume.TotalKnownMoney();
    var json = System.Text.Json.JsonSerializer.Serialize(toResume,
        new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
    var resumedWorld = System.Text.Json.JsonSerializer.Deserialize<MirWorldState>(json,
        new System.Text.Json.JsonSerializerOptions { IncludeFields = true })!;
    var afterResume = new MirEconomy(resumedWorld);
    var resumedOrder = resumedWorld.freight[0];
    Check(afterResume.SetFreightStage(resumedOrder, MirOrderStatus.Driving) &&
          afterResume.SetFreightStage(resumedOrder, MirOrderStatus.Unloading),
        "restored pending delivery settles once");
    Check(!afterResume.SetFreightStage(resumedOrder, MirOrderStatus.Unloading) &&
          afterResume.TotalKnownMoney() == pendingMoney,
        "restored freight cannot repay escrow on replay");
}
FreightStateRegressionTests();


static void WideLedgerTests()
{
    var world = MirWorldState.NewWorld();
    // All account balances are valid int values, but their sum no longer fits into int.
    world.externalMoney = int.MaxValue;
    var economy = new MirEconomy(world);
    long expected = (long)world.cityTreasury + world.externalMoney;
    foreach (var citizen in world.citizens) expected += citizen.cash;
    foreach (var business in world.businesses) expected += business.cash;
    Check(expected > int.MaxValue, "valid world can have more money than Int32.MaxValue");
    Check(economy.TotalKnownMoney() == expected,
        "ledger aggregation keeps full 64-bit total without wrapping");

    var payer = world.Citizen("player-001")!;
    var market = world.Business("market")!;
    long before = economy.TotalKnownMoney();
    Check(economy.Transfer("wide-ledger-transfer", payer.id, market.id, 100, "wide ledger regression"),
        "wide-ledger world can perform a valid transfer");
    Check(economy.TotalKnownMoney() == before,
        "wide-ledger transfer conserves every credit");
    var resumed = new MirEconomy(world);
    Check(resumed.TotalKnownMoney() == before &&
          !resumed.Transfer("wide-ledger-transfer", payer.id, market.id, 100, "replay"),
        "wide-ledger conservation and transfer idempotency survive reinitialization");
}
WideLedgerTests();

static void ProductStockOverflowTests()
{
    var world = MirWorldState.NewWorld();
    var economy = new MirEconomy(world);
    var farm = world.Business("farm")!;
    var worker = world.Citizen("citizen-001")!;
    worker.workplaceId = farm.id;
    worker.skill = int.MaxValue;
    worker.daysWorked = int.MaxValue;
    farm.stock = int.MaxValue;
    farm.capacity = int.MaxValue;
    long moneyBefore = economy.TotalKnownMoney();
    Check(economy.Work(worker), "salary transfer remains possible at maximum stock");
    Check(farm.stock == int.MaxValue, "farm work cannot overflow stocked goods");
    Check(worker.skill == 999 && worker.daysWorked == int.MaxValue,
        "work counters saturate without signed wraparound");
    Check(economy.TotalKnownMoney() == moneyBefore,
        "work at stock boundaries still conserves money");

    var fullFactory = MirWorldState.NewWorld();
    var factoryEconomy = new MirEconomy(fullFactory);
    var supplier = fullFactory.Business("farm")!;
    var packing = fullFactory.Business("packing")!;
    int supplyBefore = supplier.stock;
    packing.stock = int.MaxValue;
    packing.capacity = int.MaxValue;
    fullFactory.gameMinutes = 720f;
    factoryEconomy.Tick(.05f);
    Check(packing.stock == int.MaxValue && supplier.stock == supplyBefore,
        "production at full int capacity cannot overflow output or consume input");

    var almostFull = MirWorldState.NewWorld();
    var normalProduction = new MirEconomy(almostFull);
    var normalFarm = almostFull.Business("farm")!;
    var normalPacking = almostFull.Business("packing")!;
    int normalInput = normalFarm.stock;
    normalPacking.stock = int.MaxValue - 8;
    normalPacking.capacity = int.MaxValue;
    almostFull.gameMinutes = 720f;
    normalProduction.Tick(.05f);
    Check(normalPacking.stock == int.MaxValue && normalFarm.stock == normalInput - 10,
        "exactly fitting factory batch is produced without false rejection");
}
ProductStockOverflowTests();



static void HeadlessPopulationTests()
{
    var state = MirWorldState.NewWorld();
    var economy = new MirEconomy(state);
    var sim = new MirSimulationLoop(state, economy);
    var brain = new MirHeadlessPopulation(state, economy);
    long beforeMoney = economy.TotalKnownMoney();
    float oldPlayerX = state.Citizen("player-001")!.x;
    float startX = state.Citizen("citizen-001")!.x;
    float startZ = state.Citizen("citizen-001")!.z;
    for (int i = 0; i < 900; ++i)
    {
        sim.Advance(0.1f);
        brain.Advance(0.1f);
    }
    var npc = state.Citizen("citizen-001")!;
    Check(Math.Abs(npc.x - startX) + Math.Abs(npc.z - startZ) > 3f,
        "headless citizens walk their world-space routes");
    Check(state.Citizen("player-001")!.x == oldPlayerX,
        "backend agent loop never takes control of the player");
    Check(economy.TotalKnownMoney() == beforeMoney, "headless NPC actions preserve all money");

    var hunger = MirWorldState.NewWorld();
    var buyer = hunger.Citizen("citizen-001")!;
    buyer.satiety = 10f; buyer.inventoryFood = 0;
    buyer.x = 20f; buyer.z = -28f;
    Check(MirCitizenDecision.Destination(hunger, buyer) == "market",
        "hungry resident seeks a stocked market");
    var business = hunger.Business("market")!;
    int originalStock = business.stock;
    var feeding = new MirHeadlessPopulation(hunger, new MirEconomy(hunger));
    feeding.Advance(.1f);
    Check(buyer.inventoryFood == 1 && business.stock == originalStock - 1,
        "server NPC buys one real inventory item at the market entrance");
    Check(MirCitizenDecision.Destination(hunger, buyer) == buyer.homeId,
        "NPC with unconsumed food returns home instead of buying repeatedly");
}
HeadlessPopulationTests();

// Public network DTO must never expose private state or the untrusted player.
static void PublicSnapshotTests()
{
    var world = MirWorldState.NewWorld();
    var publicView = MirSceneSnapshot.FromWorld(world, "test-server-id", 1);
    Check(publicView.version == MirSceneSnapshot.CurrentVersion &&
          publicView.sequence == 1 && publicView.serverId == "test-server-id" &&
          publicView.citizens.Length == 39,
        "public spectator snapshot includes exactly 39 non-player NPCs");
    Check(Array.TrueForAll(publicView.citizens, x => x.id != "player-001"),
        "player identity and position are not serialized as public NPC");
    var json = System.Text.Json.JsonSerializer.Serialize(publicView,
        new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
    Check(json.Contains("gameMinutes") && json.Contains("citizens") &&
          json.Contains("sequence"),
        "public spectator JSON uses the Unity-compatible field schema");
    Check(!json.Contains("cash") && !json.Contains("transactions") &&
          !json.Contains("stock") && !json.Contains("cityTreasury"),
        "public spectator JSON excludes financial and transaction details");
    publicView.citizens[0].x += 100f;
    Check(world.Citizen(publicView.citizens[0].id)!.x != publicView.citizens[0].x,
        "public spectator snapshot cannot mutate authoritative world state");
}
PublicSnapshotTests();

static void SnapshotOrderingTests()
{
    var world = MirWorldState.NewWorld();
    var cursor = new MirSceneSnapshotCursor();
    var initial = MirSceneSnapshot.FromWorld(world, "server-A", 1);
    Check(cursor.Accept(initial), "v2 snapshot starts a spectator session");
    Check(!cursor.Accept(initial), "replayed snapshot cannot rewind a client");

    world.gameMinutes += 0.25f;
    var next = MirSceneSnapshot.FromWorld(world, "server-A", 2);
    Check(cursor.Accept(next), "higher sequence with forward clock is accepted");
    Check(!cursor.Accept(initial), "out-of-order sequence is rejected");
    var duplicateSequence = MirSceneSnapshot.FromWorld(world, "server-A", 2);
    Check(!cursor.Accept(duplicateSequence), "duplicate sequence is rejected");

    var rolledClock = MirSceneSnapshot.FromWorld(world, "server-A", 3);
    rolledClock.gameMinutes = initial.gameMinutes - 0.5f;
    Check(!cursor.Accept(rolledClock), "clock rollback within one server is rejected");

    var wrongVersion = MirSceneSnapshot.FromWorld(world, "server-A", 3);
    wrongVersion.version = 1;
    Check(!cursor.Accept(wrongVersion), "old schema version is rejected");

    var duplicateCitizen = MirSceneSnapshot.FromWorld(world, "server-A", 3);
    duplicateCitizen.citizens[1].id = duplicateCitizen.citizens[0].id;
    Check(!cursor.Accept(duplicateCitizen), "duplicate resident IDs are rejected");

    var nonFiniteCitizen = MirSceneSnapshot.FromWorld(world, "server-A", 3);
    nonFiniteCitizen.citizens[0].x = float.NaN;
    Check(!cursor.Accept(nonFiniteCitizen), "nonfinite positions are rejected");

    var noSequence = MirSceneSnapshot.FromWorld(world, "server-A", 0);
    Check(!cursor.Accept(noSequence), "missing snapshot sequence is rejected");

    var reboot = MirSceneSnapshot.FromWorld(world, "server-B", 1);
    reboot.gameMinutes = initial.gameMinutes - 1f; // Last persisted tick can precede a crash.
    Check(cursor.Accept(reboot), "new server identity recovers from saved-time rollback");
    Check(!cursor.Accept(MirSceneSnapshot.FromWorld(world, "server-A", 99)),
        "retired server sessions cannot overwrite post-restart NPC positions");

    var populated = MirWorldState.NewWorld(320);
    var bounded = MirSceneSnapshot.FromWorld(populated, "server-large", 1);
    Check(bounded.citizens.Length == MirSceneSnapshot.MaxCitizens &&
          MirSceneSnapshot.IsValidNext(bounded, "", 0, -1f),
        "public snapshot caps NPC count and stays contract-valid");
}
SnapshotOrderingTests();

static void ServerPersistenceTests()
{
    var path = Path.Combine(Path.GetTempPath(),
        "nonation-authority-" + Guid.NewGuid().ToString("N") + ".json");
    string? former = Environment.GetEnvironmentVariable("NONATION_WORLD_FILE");
    try
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", path);
        var authority = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        var start = authority.Snapshot();
        long totalMoney = new MirEconomy(start).TotalKnownMoney();
        float startX = start.Citizen("citizen-001")!.x;
        float startZ = start.Citizen("citizen-001")!.z;
        for (int i = 0; i < 60; i++) authority.Advance(0.1f);
        var advanced = authority.Snapshot();
        var moving = advanced.Citizen("citizen-001")!;
        Check(Math.Abs(moving.x - startX) + Math.Abs(moving.z - startZ) > 0.2f,
            "authority tick advances an NPC through world coordinates");
        Check(new MirEconomy(advanced).TotalKnownMoney() == totalMoney,
            "authority ticks conserve total money");

        var first = authority.SpectatorSnapshot();
        var second = authority.SpectatorSnapshot();
        Check(first.serverId == second.serverId && second.sequence > first.sequence,
            "real server emits monotonic spectator sequences");
        authority.Save();
        var restoredAuthority = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        var restored = restoredAuthority.Snapshot();
        var restoredNpc = restored.Citizen("citizen-001")!;
        Check(restored.gameMinutes == advanced.gameMinutes &&
              restoredNpc.x == moving.x && restoredNpc.z == moving.z,
            "persisted NPC coordinates and clock survive authority restart");
        Check(new MirEconomy(restored).TotalKnownMoney() == totalMoney,
            "persisted financial ledger survives authority restart");
        var restartedSnapshot = restoredAuthority.SpectatorSnapshot();
        var cursor = new MirSceneSnapshotCursor();
        Check(cursor.Accept(second) && cursor.Accept(restartedSnapshot) &&
              restartedSnapshot.serverId != second.serverId &&
              restartedSnapshot.sequence == 1,
            "server restart rotates its identity and resets the snapshot sequence");

        File.WriteAllText(path,
            "{\"version\":1,\"citizens\":null,\"businesses\":[{\"id\":\"market\"}]}");
        bool rejectedNullCitizens = false;
        try { _ = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
        catch (InvalidDataException) { rejectedNullCitizens = true; }
        Check(rejectedNullCitizens, "explicit null citizens list is rejected on load");

        File.WriteAllText(path,
            "{\"version\":1,\"citizens\":[{\"id\":\"player-001\"}],\"businesses\":null}");
        bool rejectedNullBusinesses = false;
        try { _ = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
        catch (InvalidDataException) { rejectedNullBusinesses = true; }
        Check(rejectedNullBusinesses, "explicit null businesses list is rejected on load");

        File.WriteAllText(path,
            "{\"version\":1,\"citizens\":[{\"id\":\"player-001\"}],\"businesses\":[{\"id\":\"market\"}],\"transactions\":null}");
        bool rejectedNullTransactions = false;
        try { _ = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
        catch (InvalidDataException) { rejectedNullTransactions = true; }
        Check(rejectedNullTransactions, "explicit null transaction ledger is rejected on load");
    }
    finally
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", former);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    }
}
ServerPersistenceTests();

static void AtomicAuthoritySaveTests()
{
    var path = Path.Combine(Path.GetTempPath(),
        "nonation-atomic-" + Guid.NewGuid().ToString("N") + ".json");
    string? former = Environment.GetEnvironmentVariable("NONATION_WORLD_FILE");
    var temp = path + ".tmp";
    try
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", path);
        var authority = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        authority.Save();
        string originalSave = File.ReadAllText(path);
        long originalMoney = new MirEconomy(authority.Snapshot()).TotalKnownMoney();

        // An abandoned or half-written stage must never be loaded as the world.
        File.WriteAllText(temp, "{\"version\":");
        var resumed = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        Check(resumed.Snapshot().citizens.Count == 40 &&
              File.ReadAllText(path) == originalSave,
            "partial temporary file cannot replace a valid canonical world");
        resumed.Save();
        Check(!File.Exists(temp), "next successful save removes abandoned stage");

        // Make staging fail deterministically. The previous canonical save must
        // remain byte-for-byte intact, even though the in-memory world advanced.
        var durableSave = File.ReadAllText(path);
        Directory.CreateDirectory(temp);
        bool failedAsExpected = false;
        try
        {
            resumed.Advance(.1f);
            resumed.Save();
        }
        catch (IOException) { failedAsExpected = true; }
        catch (UnauthorizedAccessException) { failedAsExpected = true; }
        finally { Directory.Delete(temp); }
        Check(failedAsExpected && File.ReadAllText(path) == durableSave,
            "failed staging cannot corrupt or replace the previous save");

        // Concurrent Save callers and Advance always leave one parseable,
        // latest-in-memory persisted snapshot rather than interleaved writes.
        System.Threading.Tasks.Parallel.For(0, 24, i =>
        {
            resumed.Advance(.1f);
            resumed.Save();
        });
        var expected = resumed.Snapshot();
        var restored = new WorldAuthority(NullLogger<WorldAuthority>.Instance).Snapshot();
        Check(restored.gameMinutes == expected.gameMinutes &&
              restored.Citizen("citizen-001")!.x == expected.Citizen("citizen-001")!.x &&
              restored.Citizen("citizen-001")!.z == expected.Citizen("citizen-001")!.z,
            "concurrent saves preserve the latest world clock and NPC coordinates");
        Check(new MirEconomy(restored).TotalKnownMoney() == originalMoney &&
              !File.Exists(temp),
            "concurrent saves conserve money and leave no temporary save");
    }
    finally
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", former);
        if (File.Exists(temp)) File.Delete(temp);
        if (Directory.Exists(temp)) Directory.Delete(temp);
        if (File.Exists(path)) File.Delete(path);
    }
}
AtomicAuthoritySaveTests();

static void SavedWorldIntegrityTests()
{
    var saveFile = Path.Combine(Path.GetTempPath(),
        "nonation-integrity-" + Guid.NewGuid().ToString("N") + ".json");
    string? previousPath = Environment.GetEnvironmentVariable("NONATION_WORLD_FILE");
    var json = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
    try
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", saveFile);

        void MustReject(Action<MirWorldState> corrupt, string description)
        {
            var world = MirWorldState.NewWorld();
            corrupt(world);
            string persisted = System.Text.Json.JsonSerializer.Serialize(world, json);
            File.WriteAllText(saveFile, persisted);
            bool rejected = false;
            try { _ = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected && File.ReadAllText(saveFile) == persisted,
                description + " and original save remains intact");
        }

        MustReject(w => w.Citizen("player-001")!.cash = -1,
            "negative citizen balance is rejected on restart");
        MustReject(w => w.cityTreasury = -1,
            "negative city treasury is rejected on restart");
        MustReject(w => w.Business("market")!.stock = -1,
            "negative business inventory is rejected on restart");
        MustReject(w => w.Business("market")!.stock = w.Business("market")!.capacity + 1,
            "stock above capacity is rejected on restart");
        MustReject(w => w.citizens[1].id = w.citizens[0].id,
            "duplicate citizen transfer IDs are rejected");
        MustReject(w => w.Business("market")!.id = "player-001",
            "citizen and business cannot share a ledger account");
        MustReject(w => w.Business("market")!.id = "city",
            "reserved treasury account cannot be shadowed");
        MustReject(w => w.gameMinutes = -1f,
            "negative simulation clock is rejected");
        MustReject(w => w.transactionSequence = 0,
            "invalid transaction sequence is rejected");
        MustReject(w =>
        {
            var e = new MirEconomy(w);
            e.Transfer("saved-tx", "player-001", "market", 1, "test");
            w.transactions.Add(new MirTransaction
            {
                id = "saved-tx", from = "player-001", to = "market",
                amount = 1, minute = w.gameMinutes
            });
        }, "duplicate transaction IDs are rejected");
        MustReject(w =>
        {
            var o = new MirEconomy(w).CreateRestock(qty: 3)!;
            o.status = MirOrderStatus.Returning;
        }, "unpaid returning freight cannot be reloaded");
        MustReject(w =>
        {
            var o = new MirEconomy(w).CreateRestock(qty: 3)!;
            o.escrow = 0;
        }, "missing escrow for reserved freight cannot be reloaded");
        MustReject(w =>
        {
            var o = new MirEconomy(w).CreateRestock(qty: 3)!;
            w.freight.Add(new MirFreightOrder
            {
                id = "extra-order", supplierId = o.supplierId,
                consumerId = o.consumerId, productId = o.productId,
                quantity = o.quantity, escrow = o.escrow,
                freightFee = o.freightFee, stockReserved = true,
                status = MirOrderStatus.Created
            });
        }, "two simultaneous freight reservations cannot be reloaded");

        // A legitimate mid-delivery save still resumes and settles once.
        var valid = MirWorldState.NewWorld();
        var before = new MirEconomy(valid);
        var pending = before.CreateRestock(qty: 3)!;
        Check(pending != null && before.SetFreightStage(pending, MirOrderStatus.Loading),
            "legitimate freight is ready for restart");
        long money = before.TotalKnownMoney();
        File.WriteAllText(saveFile, System.Text.Json.JsonSerializer.Serialize(valid, json));
        var loaded = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        var restored = loaded.Snapshot();
        Check(restored.freight.Count == 1 &&
              restored.freight[0].status == MirOrderStatus.Loading &&
              new MirEconomy(restored).TotalKnownMoney() == money,
            "valid reserved shipment and escrow survive strict load validation");
        var resumedEconomy = new MirEconomy(restored);
        Check(resumedEconomy.SetFreightStage(restored.freight[0], MirOrderStatus.Driving) &&
              resumedEconomy.SetFreightStage(restored.freight[0], MirOrderStatus.Unloading) &&
              resumedEconomy.TotalKnownMoney() == money,
            "restored validated shipment settles once without changing total money");
    }
    finally
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", previousPath);
        if (File.Exists(saveFile)) File.Delete(saveFile);
        if (File.Exists(saveFile + ".tmp")) File.Delete(saveFile + ".tmp");
    }
}
SavedWorldIntegrityTests();




