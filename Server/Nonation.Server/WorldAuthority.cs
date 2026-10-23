using System.Text.Json;
using MIR;

namespace Nonation.Server;

// One authoritative world; all state mutations are serialized under the same lock.
public sealed class WorldAuthority
{
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true, WriteIndented = true };
    private readonly object gate = new();
    private readonly string serverId = Guid.NewGuid().ToString("N");
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
            if (candidate is null || candidate.version != 1 || candidate.citizens?.Count < 1 ||
                candidate.businesses?.Count < 1)
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
        lock (gate) return MirSceneSnapshot.FromWorld(world, serverId);
    }

    public void Save()
    {
        string content;
        lock (gate) content = JsonSerializer.Serialize(world, Json);
        string? directory = Path.GetDirectoryName(savePath);
        if (directory is not null) Directory.CreateDirectory(directory);
        string temp = savePath + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, savePath, true);
    }
}
