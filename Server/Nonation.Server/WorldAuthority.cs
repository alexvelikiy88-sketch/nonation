using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using MIR;

namespace Nonation.Server;

// One authoritative world; all state mutations are serialized under the same lock.
public sealed class WorldAuthority
{
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true, WriteIndented = true };
    private readonly object gate = new();
    // Serializes snapshot capture AND disk replacement across concurrent saves.
    private readonly object saveGate = new();
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
        world = Load();
        var economy = new MirEconomy(world);
        simulation = new MirSimulationLoop(world, economy);
        population = new MirHeadlessPopulation(world, economy);
        log.LogInformation("nonation authority loaded {Count} citizens", world.citizens.Count);
    }

    private MirWorldState Load()
    {
        if (!File.Exists(savePath)) return MirWorldState.NewWorld();
        try
        {
            var candidate = JsonSerializer.Deserialize<MirWorldState>(File.ReadAllText(savePath), Json);
            // Nullable '?.Count < 1' is false when the list is null: explicitly
            // reject missing collections before any simulation or ledger can run.
            if (candidate is null || candidate.version != 1 ||
                candidate.citizens == null || candidate.citizens.Count < 1 ||
                candidate.businesses == null || candidate.businesses.Count < 1 ||
                candidate.transactions == null || candidate.freight == null ||
                candidate.events == null ||
                candidate.citizens.Any(c => c == null) ||
                candidate.businesses.Any(b => b == null) ||
                candidate.transactions.Any(t => t == null) ||
                candidate.freight.Any(o => o == null) ||
                candidate.events.Any(e => e == null))
                throw new InvalidDataException("Invalid world state or unsupported version");
            return candidate;
        }
        catch (Exception ex)
        {
            // Never overwrite corrupted saves silently.
            throw new InvalidDataException("Cannot load saved world: " + savePath, ex);
        }
    }

    public void Advance(float elapsedSeconds)
    {
        lock (gate)
        {
            simulation.Advance(elapsedSeconds);
            population.Advance(elapsedSeconds);
        }
    }

    public MirWorldState Snapshot()
    {
        lock (gate)
        {
            var json = JsonSerializer.Serialize(world, Json);
            return JsonSerializer.Deserialize<MirWorldState>(json, Json)
                ?? throw new InvalidOperationException("Could not clone state");
        }
    }

    public MirSceneSnapshot SpectatorSnapshot()
    {
        lock (gate)
        {
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
            lock (gate) content = JsonSerializer.Serialize(world, Json);
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
