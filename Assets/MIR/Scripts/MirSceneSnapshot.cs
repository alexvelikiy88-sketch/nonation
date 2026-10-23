using System;

namespace MIR
{
    // Portable, public, read-only spectator payload. Never includes cash, stock,
    // transaction history, personal account data, or game commands.
    [Serializable]
    public sealed class MirSceneCitizen
    {
        public string id = "";
        public float x;
        public float z;
    }

    [Serializable]
    public sealed class MirSceneSnapshot
    {
        public int version = 1;
        // Distinguish server restarts from a stale network response.
        public string serverId = "";
        public float gameMinutes;
        public MirSceneCitizen[] citizens = new MirSceneCitizen[0];

        public static MirSceneSnapshot FromWorld(MirWorldState world, string serverId = "")
        {
            if(world == null) throw new ArgumentNullException(nameof(world));
            int count = 0;
            foreach (var c in world.citizens)
                if(c != null && !c.player) count++;
            var snapshot = new MirSceneSnapshot
            {
                gameMinutes = world.gameMinutes,
                serverId = serverId,
                citizens = new MirSceneCitizen[count]
            };
            int i = 0;
            foreach (var c in world.citizens)
            {
                if(c == null || c.player) continue;
                snapshot.citizens[i++] = new MirSceneCitizen
                {
                    id = c.id,
                    x = c.x,
                    z = c.z
                };
            }
            return snapshot;
        }
    }
}
