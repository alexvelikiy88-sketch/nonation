using System;
namespace MIR
{
    // Shared fixed-step domain clock. No UnityEngine dependency.
    public sealed class MirSimulationLoop
    {
        const float StepSeconds = 0.1f;
        const float MinutesPerSecond = 0.5f;
        readonly MirWorldState world;
        readonly MirEconomy economy;
        float remainder;
        public MirSimulationLoop(MirWorldState state, MirEconomy service)
        {
            world = state ?? throw new ArgumentNullException(nameof(state));
            economy = service ?? throw new ArgumentNullException(nameof(service));
        }
        public bool Advance(float realSeconds)
        {
            if (realSeconds <= 0 || float.IsNaN(realSeconds) || float.IsInfinity(realSeconds)) return false;
            remainder += Math.Min(realSeconds, 0.25f);
            int previousMinute = (int)Math.Floor(world.gameMinutes);
            int steps = 0;
            while (remainder >= StepSeconds && steps < 4)
            {
                float minutes = StepSeconds * MinutesPerSecond;
                world.gameMinutes += minutes;
                economy.Tick(minutes);
                remainder -= StepSeconds;
                steps++;
            }
            return (int)Math.Floor(world.gameMinutes) != previousMinute;
        }
    }
}
