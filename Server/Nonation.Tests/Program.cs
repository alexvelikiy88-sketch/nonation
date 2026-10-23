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
