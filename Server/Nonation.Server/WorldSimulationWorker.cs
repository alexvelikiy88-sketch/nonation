using System.Diagnostics;

namespace Nonation.Server;

public sealed class WorldSimulationWorker : BackgroundService
{
    private readonly WorldAuthority authority;
    private readonly ILogger<WorldSimulationWorker> log;

    public WorldSimulationWorker(WorldAuthority world, ILogger<WorldSimulationWorker> logger)
    {
        authority = world;
        log = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        long last = Stopwatch.GetTimestamp();
        int ticks = 0;
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                long now = Stopwatch.GetTimestamp();
                authority.Advance((float)Stopwatch.GetElapsedTime(last, now).TotalSeconds);
                last = now;
                if (++ticks >= 300)
                {
                    ticks = 0;
                    authority.Save();
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {}
        finally
        {
            try { authority.Save(); }
            catch (Exception ex) { log.LogError(ex, "Final world save failed"); }
        }
    }
}
