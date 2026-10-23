using MIR;
using Nonation.Server;
using Microsoft.Extensions.Logging.Abstractions;

// Used only by the cross-process file-lease regression. This child process
// exits without running the normal suite.
if (args.Length == 2 && args[0] == "--probe-world-lease")
{
    Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", args[1]);
    try
    {
        using var probe = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        Console.WriteLine("WORLD_LEASE_ACQUIRED");
    }
    catch (IOException)
    {
        Console.WriteLine("WORLD_LEASE_DENIED");
    }
    return;
}

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
        using var authority = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
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
        authority.Dispose();
        using var restoredAuthority = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
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
        restoredAuthority.Dispose();

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
        using var authority = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        authority.Save();
        string originalSave = File.ReadAllText(path);
        long originalMoney = new MirEconomy(authority.Snapshot()).TotalKnownMoney();

        // An abandoned or half-written stage must never be loaded as the world.
        File.WriteAllText(temp, "{\"version\":");
        authority.Dispose();
        using var resumed = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
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
        resumed.Dispose();
        using var reloaded = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        var restored = reloaded.Snapshot();
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
        using var loaded = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
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

static void PersistedIdentifierSequenceTests()
{
    var world = MirWorldState.NewWorld();
    var economy = new MirEconomy(world);
    var hero = world.Citizen("player-001")!;
    var shop = world.Business("market")!;
    world.transactionSequence = int.MaxValue - 1;
    Check(economy.BuyFood(hero, shop) &&
          world.transactionSequence == int.MaxValue,
        "last safe transaction ID can be issued without wraparound");
    long totalBefore = economy.TotalKnownMoney();
    int transactionCount = world.transactions.Count;
    int stockBefore = shop.stock, foodBefore = hero.inventoryFood;
    int cashBefore = hero.cash;
    Check(!economy.BuyFood(hero, shop) && !economy.Work(hero),
        "no new autogenerated purchase or salary when transaction IDs are exhausted");
    Check(world.transactionSequence == int.MaxValue &&
          world.transactions.Count == transactionCount &&
          hero.cash == cashBefore && hero.inventoryFood == foodBefore &&
          shop.stock == stockBefore && economy.TotalKnownMoney() == totalBefore,
        "exhausted transaction sequence cannot mutate cash or inventory");

    var shipment = MirWorldState.NewWorld();
    var carrier = new MirEconomy(shipment);
    shipment.jobSequence = int.MaxValue;
    long money = carrier.TotalKnownMoney();
    int supplierStock = shipment.Business("warehouse")!.stock;
    Check(carrier.CreateRestock(qty: 4) == null &&
          shipment.freight.Count == 0 && shipment.jobSequence == int.MaxValue &&
          shipment.Business("warehouse")!.stock == supplierStock &&
          carrier.TotalKnownMoney() == money,
        "exhausted freight IDs do not reserve stock or money");

    shipment.jobSequence = int.MaxValue - 1;
    var last = carrier.CreateRestock(qty: 4);
    Check(last != null && last.id == "freight-" + (int.MaxValue - 1) &&
          shipment.jobSequence == int.MaxValue,
        "last safe freight order can be created");
    Check(carrier.CancelFreight(last!) &&
          carrier.CreateRestock(qty: 4) == null &&
          carrier.TotalKnownMoney() == money &&
          shipment.Business("warehouse")!.stock == supplierStock,
        "once exhausted, cancelled freight cannot reuse an ID or lose escrow");
}
PersistedIdentifierSequenceTests();

static void FreightSequenceReuseTests()
{
    var state = MirWorldState.NewWorld();
    var economy = new MirEconomy(state);
    long moneyBefore = economy.TotalKnownMoney();
    var supplier = state.Business("warehouse")!;
    var buyer = state.Business("market")!;
    int initialSupplier = supplier.stock;
    int initialBuyer = buyer.stock;
    var first = economy.CreateRestock(qty: 3)!;
    Check(first != null && first.id == "freight-1" &&
          economy.CancelFreight(first),
        "first completed freight history remains available after cancellation");

    // Simulate a rolled-back persisted counter. The cancelled order no longer
    // counts as active, but its ID must never be used again.
    state.jobSequence = 1;
    int beforeCount = state.freight.Count;
    int beforeSupplier = supplier.stock, beforeBuyer = buyer.stock;
    Check(economy.CreateRestock(qty: 3) == null &&
          state.jobSequence == 1 && state.freight.Count == beforeCount &&
          supplier.stock == beforeSupplier && buyer.stock == beforeBuyer &&
          economy.TotalKnownMoney() == moneyBefore,
        "rolled-back freight sequence rejects duplicate ID without reserving funds or goods");

    var path = Path.Combine(Path.GetTempPath(),
        "nonation-freight-sequence-" + Guid.NewGuid().ToString("N") + ".json");
    var previous = Environment.GetEnvironmentVariable("NONATION_WORLD_FILE");
    var json = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
    try
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", path);
        string corrupted = System.Text.Json.JsonSerializer.Serialize(state, json);
        File.WriteAllText(path, corrupted);
        bool rejected = false;
        try { using var invalid = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected && File.ReadAllText(path) == corrupted,
            "restored freight history rejects rolled-back jobSequence without rewriting save");

        // Restore the previous next-ID. Existing terminal IDs are still
        // reserved, and a valid new shipment can be completed safely.
        state.jobSequence = 2;
        var second = economy.CreateRestock(qty: 3)!;
        Check(second != null && second.id == "freight-2" &&
              state.freight.Count == 2 && state.jobSequence == 3,
            "restored next freight ID is unique across terminal history");
        Check(economy.SetFreightStage(second, MirOrderStatus.Loading) &&
              economy.SetFreightStage(second, MirOrderStatus.Driving) &&
              economy.SetFreightStage(second, MirOrderStatus.Unloading) &&
              economy.SetFreightStage(second, MirOrderStatus.Returning) &&
              economy.SetFreightStage(second, MirOrderStatus.Completed),
            "unique freight order completes its escrow payout once");
        Check(economy.TotalKnownMoney() == moneyBefore &&
              supplier.stock == initialSupplier - 3 &&
              buyer.stock == initialBuyer + 3,
            "new delivery conserves money and moves exactly three real goods");

        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state, json));
        using var restored = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        var persisted = restored.Snapshot();
        Check(persisted.freight.Count == 2 &&
              persisted.freight[0].id != persisted.freight[1].id &&
              persisted.jobSequence == 3 &&
              new MirEconomy(persisted).TotalKnownMoney() == moneyBefore,
            "valid freight history and monotonic sequence survive server startup");
    }
    finally
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", previous);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        if (File.Exists(path + ".lock")) File.Delete(path + ".lock");
    }
}
FreightSequenceReuseTests();

static void SavedReceiptSequenceTests()
{
    var saveFile = Path.Combine(Path.GetTempPath(),
        "nonation-receipt-sequence-" + Guid.NewGuid().ToString("N") + ".json");
    string? previous = Environment.GetEnvironmentVariable("NONATION_WORLD_FILE");
    var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
    try
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", saveFile);
        void RejectOutdatedCounter(MirWorldState state, string label)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(state, options);
            File.WriteAllText(saveFile, json);
            bool refused = false;
            try { using var invalid = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
            catch (InvalidDataException) { refused = true; }
            Check(refused && File.ReadAllText(saveFile) == json,
                label + " is rejected without touching saved receipts or accounts");
        }

        var purchaseWorld = MirWorldState.NewWorld();
        var purchaseEconomy = new MirEconomy(purchaseWorld);
        var hero = purchaseWorld.Citizen("player-001")!;
        var market = purchaseWorld.Business("market")!;
        long initialMoney = purchaseEconomy.TotalKnownMoney();
        Check(purchaseEconomy.BuyFood(hero, market) &&
              purchaseWorld.transactions[0].id == "purchase-1" &&
              purchaseWorld.transactionSequence == 2,
            "genuine purchase receipt advances the shared sequence");
        purchaseWorld.transactionSequence = 1;
        RejectOutdatedCounter(purchaseWorld, "rolled-back purchase sequence");

        var salaryWorld = MirWorldState.NewWorld();
        var salaryEconomy = new MirEconomy(salaryWorld);
        var employee = salaryWorld.Citizen("player-001")!;
        Check(salaryEconomy.Work(employee) &&
              salaryWorld.transactions[0].id == "salary-1" &&
              salaryWorld.transactionSequence == 2,
            "genuine salary receipt advances the shared sequence");
        salaryWorld.transactionSequence = 1;
        RejectOutdatedCounter(salaryWorld, "rolled-back salary sequence");

        purchaseWorld.transactionSequence = 2;
        File.WriteAllText(saveFile,
            System.Text.Json.JsonSerializer.Serialize(purchaseWorld, options));
        using (var restoredAuthority = new WorldAuthority(NullLogger<WorldAuthority>.Instance))
        {
            var restored = restoredAuthority.Snapshot();
            var economy = new MirEconomy(restored);
            Check(restored.transactions.Count == 1 &&
                  economy.BuyFood(restored.Citizen("player-001")!,
                      restored.Business("market")!) &&
                  restored.transactions[1].id == "purchase-2" &&
                  restored.transactionSequence == 3,
                "repaired receipt sequence permits a distinct new purchase");
            Check(economy.TotalKnownMoney() == initialMoney &&
                  restored.transactions[0].id != restored.transactions[1].id,
                "restored purchases conserve credits and never reuse a receipt ID");
        }
    }
    finally
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", previous);
        if (File.Exists(saveFile)) File.Delete(saveFile);
        if (File.Exists(saveFile + ".tmp")) File.Delete(saveFile + ".tmp");
        if (File.Exists(saveFile + ".lock")) File.Delete(saveFile + ".lock");
    }
}
SavedReceiptSequenceTests();



static void WorldLeaseTests()
{
    var saveFile = Path.Combine(Path.GetTempPath(),
        "nonation-lease-" + Guid.NewGuid().ToString("N") + ".json");
    string? original = Environment.GetEnvironmentVariable("NONATION_WORLD_FILE");

    static string ChildProbe(string savePath)
    {
        var start = new System.Diagnostics.ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(typeof(WorldAuthority).Assembly.Location);
        start.ArgumentList.Add("--probe-world-lease");
        start.ArgumentList.Add(savePath);
        using var child = System.Diagnostics.Process.Start(start)
            ?? throw new Exception("World lease probe process could not start");
        string output = child.StandardOutput.ReadToEnd().Trim();
        if (!child.WaitForExit(15000))
        {
            child.Kill(entireProcessTree: true);
            throw new Exception("World lease probe timed out");
        }
        if (child.ExitCode != 0)
            throw new Exception("World lease probe failed: " + child.StandardError.ReadToEnd());
        return output;
    }

    try
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", saveFile);
        string saved;
        long expectedMoney;
        float expectedMinutes;
        float expectedX;
        using (var owner = new WorldAuthority(NullLogger<WorldAuthority>.Instance))
        {
            for (int i = 0; i < 80; i++) owner.Advance(.1f);
            owner.Save();
            var world = owner.Snapshot();
            expectedMoney = new MirEconomy(world).TotalKnownMoney();
            expectedMinutes = world.gameMinutes;
            expectedX = world.Citizen("citizen-001")!.x;
            saved = File.ReadAllText(saveFile);

            Check(ChildProbe(saveFile) == "WORLD_LEASE_DENIED",
                "second OS process cannot claim the same active world file");
            bool sameProcessDenied = false;
            try { using var second = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
            catch (IOException) { sameProcessDenied = true; }
            Check(sameProcessDenied, "second authority in same process cannot claim the world");
            Check(File.ReadAllText(saveFile) == saved && !File.Exists(saveFile + ".tmp"),
                "failed concurrent owner cannot replace saved money or NPC positions");
        }

        Check(File.Exists(saveFile + ".lock"),
            "persistent lease marker remains after owner shutdown");
        Check(ChildProbe(saveFile) == "WORLD_LEASE_ACQUIRED",
            "next OS process may safely recover the world after lease release");
        using (var reopened = new WorldAuthority(NullLogger<WorldAuthority>.Instance))
        {
            var state = reopened.Snapshot();
            Check(new MirEconomy(state).TotalKnownMoney() == expectedMoney &&
                  state.gameMinutes == expectedMinutes &&
                  state.Citizen("citizen-001")!.x == expectedX,
                "lease handoff preserves money, world time and NPC coordinates");
        }

        // The lock must also be released if a constructor fails during Load().
        File.WriteAllText(saveFile, "{\"version\":1,\"citizens\":null}");
        bool refused = false;
        try { using var invalid = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
        catch (InvalidDataException) { refused = true; }
        Check(refused, "invalid persisted world rejects construction");
        File.WriteAllText(saveFile, saved);
        Check(ChildProbe(saveFile) == "WORLD_LEASE_ACQUIRED",
            "failed world load does not leak the cross-process lease");
    }
    finally
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", original);
        if (File.Exists(saveFile + ".tmp")) File.Delete(saveFile + ".tmp");
        if (File.Exists(saveFile)) File.Delete(saveFile);
        if (File.Exists(saveFile + ".lock")) File.Delete(saveFile + ".lock");
    }
}
WorldLeaseTests();

static void BoundedWorldLoadTests()
{
    var path = Path.Combine(Path.GetTempPath(),
        "nonation-size-" + Guid.NewGuid().ToString("N") + ".json");
    string? original = Environment.GetEnvironmentVariable("NONATION_WORLD_FILE");
    try
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", path);
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            stream.SetLength(WorldAuthority.MaxSaveBytes + 1);

        bool refused = false;
        try { using var unsafeWorld = new WorldAuthority(NullLogger<WorldAuthority>.Instance); }
        catch (InvalidDataException) { refused = true; }
        Check(refused && new FileInfo(path).Length == WorldAuthority.MaxSaveBytes + 1,
            "oversized world is rejected before JSON parsing without modifying the save");

        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(
            MirWorldState.NewWorld(),
            new System.Text.Json.JsonSerializerOptions { IncludeFields = true }));
        using var healthy = new WorldAuthority(NullLogger<WorldAuthority>.Instance);
        var snapshot = healthy.Snapshot();
        Check(snapshot.citizens.Count == 40 &&
              new MirEconomy(snapshot).TotalKnownMoney() > 0,
            "server may load a valid bounded save after failed large-file startup");
    }
    finally
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", original);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        if (File.Exists(path + ".lock")) File.Delete(path + ".lock");
    }
}
BoundedWorldLoadTests();

static void BoundedTransactionHistoryTests()
{
    var world = MirWorldState.NewWorld();
    var economy = new MirEconomy(world);
    long initialTotal = economy.TotalKnownMoney();
    int limit = MirEconomy.MaxRecordedTransactions;

    // Every seeded receipt represents an actual transfer: the history and all
    // balances remain consistent while approaching the fixed journal limit.
    for (int i = 0; i < limit - 1; i++)
    {
        bool forward = i % 2 == 0;
        if (!economy.Transfer("ledger-" + i,
                forward ? "city" : "external",
                forward ? "external" : "city", 1, "history"))
            throw new Exception("FAIL: unable to seed valid ledger transaction " + i);
    }
    Check(world.transactions.Count == limit - 1 &&
          economy.TotalKnownMoney() == initialTotal,
        "journal can approach cap with money conserved and receipts retained");
    Check(economy.Transfer("last-valid-ledger", "city", "external", 1, "history") &&
          world.transactions.Count == limit,
        "last available journal slot accepts one real transfer");

    var hero = world.Citizen("player-001")!;
    var shop = world.Business("market")!;
    int playerCash = hero.cash, storeCash = shop.cash;
    int storeStock = shop.stock, food = hero.inventoryFood;
    int workCount = hero.daysWorked;
    float nextShift = hero.nextWorkMinute;
    int txSequence = world.transactionSequence;
    long afterLimit = economy.TotalKnownMoney();

    Check(!economy.Transfer("new-after-cap", "city", "external", 1, "blocked") &&
          !economy.Transfer("ledger-0", "city", "external", 1, "replayed"),
        "full journal rejects new transfers and any replay without erasing old IDs");
    Check(!economy.BuyFood(hero, shop) && !economy.Work(hero),
        "purchases and salaries fail closed when the ledger is full");
    Check(world.transactions.Count == limit && world.transactionSequence == txSequence &&
          hero.cash == playerCash && shop.cash == storeCash &&
          shop.stock == storeStock && hero.inventoryFood == food &&
          hero.daysWorked == workCount && hero.nextWorkMinute == nextShift &&
          economy.TotalKnownMoney() == afterLimit,
        "full ledger preserves credits, stock, work cooldown and generated ID sequence");

    var jsonOptions = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
    string json = System.Text.Json.JsonSerializer.Serialize(world, jsonOptions);
    var restored = System.Text.Json.JsonSerializer.Deserialize<MirWorldState>(json, jsonOptions)!;
    var restarted = new MirEconomy(restored);
    Check(restored.transactions.Count == limit &&
          !restarted.Transfer("ledger-0", "city", "external", 1, "replay") &&
          !restarted.Transfer("new-after-restart", "city", "external", 1, "blocked") &&
          restarted.TotalKnownMoney() == afterLimit,
        "after save/reload all original receipts remain and replay stays blocked");

    // The input-size budget bounds each new row as well as row count.
    var limitsWorld = MirWorldState.NewWorld();
    var limitsEconomy = new MirEconomy(limitsWorld);
    int initialCount = limitsWorld.transactions.Count;
    long initialMoney = limitsEconomy.TotalKnownMoney();
    Check(!limitsEconomy.Transfer(new string('X', MirEconomy.MaxTransactionIdLength + 1),
            "city", "external", 1, "oversize") &&
          !limitsEconomy.Transfer("valid-id", "city", "external", 1,
            new string('X', MirEconomy.MaxTransactionReasonLength + 1)) &&
          limitsWorld.transactions.Count == initialCount &&
          limitsEconomy.TotalKnownMoney() == initialMoney,
        "oversized receipt identifiers and reasons never change the ledger");
}
BoundedTransactionHistoryTests();

static void BoundedWorldSaveTests()
{
    // Uses UTF-8 bytes, not .NET UTF-16 character count: both limits must
    // agree on the exact file bytes for the existing no-BOM StreamWriter.
    string maxBytes = new string('\u00e9', (int)(WorldAuthority.MaxSaveBytes / 2));
    Check(WorldAuthority.CanPersistPayload(maxBytes),
        "world save accepts exactly 32 MiB of UTF-8 bytes");
    Check(!WorldAuthority.CanPersistPayload(maxBytes + "\u00e9") &&
          !WorldAuthority.CanPersistPayload(null!),
        "world save rejects oversized or null payloads before file mutation");

    var path = Path.Combine(Path.GetTempPath(),
        "nonation-write-size-" + Guid.NewGuid().ToString("N") + ".json");
    var old = Environment.GetEnvironmentVariable("NONATION_WORLD_FILE");
    try
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", path);
        using (var authority = new WorldAuthority(NullLogger<WorldAuthority>.Instance))
        {
            authority.Save();
            string durable = File.ReadAllText(path);
            Check(WorldAuthority.CanPersistPayload(durable) &&
                  new FileInfo(path).Length <= WorldAuthority.MaxSaveBytes,
                "ordinary saved world stays within the new read/write limit");
            authority.Advance(.1f);
            authority.Save();
            durable = File.ReadAllText(path);
            Check(File.Exists(path) && !File.Exists(path + ".tmp"),
                "saving a bounded snapshot still replaces the canonical file normally");

            // There is intentionally no public write API on WorldAuthority.
            // Test-only reflection creates the oversized in-memory snapshot,
            // exercising Save() failure and proving last durable bytes survive.
            var privateWorld = typeof(WorldAuthority).GetField("world",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            var live = (MirWorldState)(privateWorld?.GetValue(authority) ??
                throw new Exception("Cannot inspect authoritative world for size regression"));
            live.events.Add(new MirEvent
            {
                minute = live.gameMinutes,
                text = new string('X', (int)WorldAuthority.MaxSaveBytes)
            });
            bool refused = false;
            try { authority.Save(); }
            catch (InvalidDataException) { refused = true; }
            Check(refused && File.ReadAllText(path) == durable &&
                  !File.Exists(path + ".tmp"),
                "over-budget Save fails before staging and preserves previous good world");
        }
    }
    finally
    {
        Environment.SetEnvironmentVariable("NONATION_WORLD_FILE", old);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        if (File.Exists(path + ".lock")) File.Delete(path + ".lock");
    }
}
BoundedWorldSaveTests();









