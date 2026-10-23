using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using MIR;

namespace Nonation.Server;

// One authoritative world; all state mutations are serialized under the same lock.
public sealed class WorldAuthority : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true, WriteIndented = true };
    // Avoid reading arbitrarily large saved JSON into memory before validation.
    internal const long MaxSaveBytes = 32L * 1024 * 1024;
    private readonly object gate = new();
    // Serializes snapshot capture AND disk replacement across concurrent saves.
    private readonly object saveGate = new();
    // The lock file is intentionally persistent. Unlinking it while owned on
    // Unix could allow a second process to acquire a new inode at the same path.
    // FileShare.None holds the OS-managed lease until Dispose/process exit.
    private readonly FileStream worldLease;
    private bool disposed;
    private readonly string serverId = Guid.NewGuid().ToString("N");
    private long spectatorSequence;
    private readonly string savePath;
    private readonly ILogger<WorldAuthority> log;
    private readonly MirWorldState world;
    private readonly MirSimulationLoop simulation;
    private readonly MirHeadlessPopulation population;

    public WorldAuthority(ILogger<WorldAuthority> logger)
    {
        log = logger;
        savePath = Path.GetFullPath(Environment.GetEnvironmentVariable("NONATION_WORLD_FILE") ??
            Path.Combine(AppContext.BaseDirectory, "data", "world.json"));
        var lockPath = savePath + ".lock";
        string? directory = Path.GetDirectoryName(savePath);
        if (directory is not null) Directory.CreateDirectory(directory);
        // Acquire before even reading the save, so two servers cannot load a
        // stale copy and later race to write the same world.json / .tmp.
        try
        {
            worldLease = new FileStream(lockPath, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException error)
        {
            throw new IOException("nonation world save is already owned or locked: " +
                savePath, error);
        }

        try
        {
            world = Load();
            var economy = new MirEconomy(world);
            simulation = new MirSimulationLoop(world, economy);
            population = new MirHeadlessPopulation(world, economy);
            log.LogInformation("nonation authority loaded {Count} citizens", world.citizens.Count);
        }
        catch
        {
            // Invalid saves must not leak the lease or block later recovery.
            worldLease.Dispose();
            throw;
        }
    }

    private MirWorldState Load()
    {
        if (!File.Exists(savePath)) return MirWorldState.NewWorld();
        try
        {
            using var input = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (input.Length > MaxSaveBytes)
                throw new InvalidDataException("Saved world exceeds the supported 32 MiB limit");
            // Deserialize directly from a bounded-length stream, not a giant string.
            var candidate = JsonSerializer.Deserialize<MirWorldState>(input, Json);
            // Nullable '?.Count < 1' is false when the list is null: explicitly
            // reject missing collections before any simulation or ledger can run.
            if (candidate is null || !ValidSavedWorld(candidate))
                throw new InvalidDataException("Invalid world state or unsupported version");
            return candidate;
        }
        catch (Exception ex)
        {
            // Never overwrite corrupted saves silently.
            throw new InvalidDataException("Cannot load saved world: " + savePath, ex);
        }
    }

    // Saved JSON is outside the trusted transaction boundary: it may be
    // truncated, edited or from a buggy older runtime. Never resume a world
    // whose accounts or in-flight freight can mint credits or duplicate goods.
    private static bool ValidSavedWorld(MirWorldState state)
    {
        if (state.version != 1 || state.transactionSequence < 1 ||
            state.jobSequence < 1 || !float.IsFinite(state.gameMinutes) ||
            state.gameMinutes < 0f || state.cityTreasury < 0 ||
            state.externalMoney < 0 || state.citizens == null ||
            state.citizens.Count < 1 || state.businesses == null ||
            state.businesses.Count < 1 || state.transactions == null ||
            state.freight == null || state.events == null)
            return false;

        // Both kinds share one transfer namespace; city/external are reserved.
        var accountIds = new HashSet<string>(StringComparer.Ordinal) { "city", "external" };
        var businesses = new Dictionary<string, MirBusiness>(StringComparer.Ordinal);
        foreach (var business in state.businesses)
        {
            if (business == null || string.IsNullOrWhiteSpace(business.id) ||
                string.IsNullOrWhiteSpace(business.productId) ||
                !accountIds.Add(business.id) ||
                business.cash < 0 || business.capacity < 0 ||
                business.stock < 0 || business.stock > business.capacity ||
                business.price < 0 || business.water < 0 || business.seed < 0 ||
                !float.IsFinite(business.x) || !float.IsFinite(business.z))
                return false;
            businesses.Add(business.id, business);
        }

        foreach (var citizen in state.citizens)
        {
            if (citizen == null || string.IsNullOrWhiteSpace(citizen.id) ||
                string.IsNullOrWhiteSpace(citizen.homeId) ||
                !accountIds.Add(citizen.id) || citizen.cash < 0 ||
                citizen.inventoryFood < 0 || citizen.daysWorked < 0 ||
                citizen.skill < 0 || !float.IsFinite(citizen.x) ||
                !float.IsFinite(citizen.z) ||
                !float.IsFinite(citizen.gaitSeed) ||
                !float.IsFinite(citizen.nextWorkMinute) ||
                citizen.nextWorkMinute < 0f ||
                !float.IsFinite(citizen.energy) ||
                !float.IsFinite(citizen.satiety) ||
                !float.IsFinite(citizen.hydration) ||
                !float.IsFinite(citizen.mood))
                return false;
        }

        var transactions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var transaction in state.transactions)
        {
            if (transaction == null || string.IsNullOrWhiteSpace(transaction.id) ||
                !transactions.Add(transaction.id) ||
                string.IsNullOrWhiteSpace(transaction.from) ||
                string.IsNullOrWhiteSpace(transaction.to) ||
                transaction.from == transaction.to ||
                !accountIds.Contains(transaction.from) ||
                !accountIds.Contains(transaction.to) ||
                transaction.amount <= 0 || !float.IsFinite(transaction.minute) ||
                transaction.minute < 0f)
                return false;
        }

        var freightIds = new HashSet<string>(StringComparer.Ordinal);
        int activeOrders = 0;
        foreach (var order in state.freight)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.id) ||
                !freightIds.Add(order.id) || order.quantity <= 0 ||
                order.escrow < 0 || order.freightFee < 0 ||
                !float.IsFinite(order.progress) ||
                string.IsNullOrWhiteSpace(order.productId) ||
                string.IsNullOrWhiteSpace(order.supplierId) ||
                string.IsNullOrWhiteSpace(order.consumerId) ||
                order.supplierId == order.consumerId ||
                !businesses.TryGetValue(order.supplierId, out var supplier) ||
                !businesses.TryGetValue(order.consumerId, out var consumer) ||
                supplier.productId != order.productId ||
                consumer.productId != order.productId)
                return false;
            switch (order.status)
            {
                case MirOrderStatus.Created:
                case MirOrderStatus.Loading:
                case MirOrderStatus.Driving:
                    if (order.paid || !order.stockReserved ||
                        order.escrow <= 0 || order.freightFee <= 0)
                        return false;
                    if (++activeOrders > 1) return false;
                    break;
                case MirOrderStatus.Unloading:
                case MirOrderStatus.Returning:
                    if (!order.paid || order.stockReserved ||
                        order.escrow != 0 || order.freightFee != 0)
                        return false;
                    if (++activeOrders > 1) return false;
                    break;
                case MirOrderStatus.Completed:
                    if (!order.paid || order.stockReserved ||
                        order.escrow != 0 || order.freightFee != 0)
                        return false;
                    break;
                case MirOrderStatus.Cancelled:
                    if (order.paid || order.stockReserved ||
                        order.escrow != 0 || order.freightFee != 0)
                        return false;
                    break;
                default:
                    return false;
            }
        }

        foreach (var entry in state.events)
            if (entry == null || !float.IsFinite(entry.minute))
                return false;
        return true;
    }

    private void RequireActive()
    {
        if (disposed) throw new ObjectDisposedException(nameof(WorldAuthority));
    }

    public void Dispose()
    {
        lock (saveGate)
        {
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                worldLease.Dispose();
            }
        }
        GC.SuppressFinalize(this);
    }

    public void Advance(float elapsedSeconds)
    {
        lock (gate)
        {
            RequireActive();
            simulation.Advance(elapsedSeconds);
            population.Advance(elapsedSeconds);
        }
    }

    public MirWorldState Snapshot()
    {
        lock (gate)
        {
            RequireActive();
            var json = JsonSerializer.Serialize(world, Json);
            return JsonSerializer.Deserialize<MirWorldState>(json, Json)
                ?? throw new InvalidOperationException("Could not clone state");
        }
    }

    public MirSceneSnapshot SpectatorSnapshot()
    {
        lock (gate)
        {
            RequireActive();
            // Ordered at the same lock boundary as simulation advances.
            return MirSceneSnapshot.FromWorld(world, serverId, ++spectatorSequence);
        }
    }

    public void Save()
    {
        // Save order must match capture order. Holding only 'gate' during
        // serialization allows a slower, older writer to overwrite a newer save.
        lock (saveGate)
        {
            string content;
            lock (gate)
            {
                RequireActive();
                content = JsonSerializer.Serialize(world, Json);
            }
            string? directory = Path.GetDirectoryName(savePath);
            if (directory is not null) Directory.CreateDirectory(directory);
            string temp = savePath + ".tmp";
            try
            {
                // Stage and flush a complete JSON document before replacing the
                // canonical save. A failed stage must leave the old file intact.
                using (var stream = new FileStream(temp, FileMode.Create,
                    FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(content);
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(temp, savePath, true);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
