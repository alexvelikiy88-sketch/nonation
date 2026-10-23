using System;
using System.Collections.Generic;

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
        // v2 introduces a monotonic sequence scoped to a process-unique serverId.
        // These limits keep public snapshots and Unity deserialization bounded.
        public const int CurrentVersion = 2;
        public const int MaxCitizens = 256;
        public const int MaxCitizenIdLength = 128;
        public const int MaxJsonBytes = 262144;
        public int version = CurrentVersion;
        public string serverId = "";
        public long sequence;
        public float gameMinutes;
        public MirSceneCitizen[] citizens = new MirSceneCitizen[0];

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // Shared by the Unity spectator and the headless .NET contract tests.
        // The same process must never move backwards in sequence or game time.
        public static bool IsValidNext(MirSceneSnapshot snapshot,
            string previousServerId, long previousSequence, float previousGameMinute)
        {
            if (snapshot == null || snapshot.version != CurrentVersion ||
                snapshot.sequence <= 0 || string.IsNullOrWhiteSpace(snapshot.serverId) ||
                snapshot.serverId.Length > 64 ||
                !Finite(snapshot.gameMinutes) || snapshot.gameMinutes < 0f ||
                snapshot.citizens == null || snapshot.citizens.Length == 0 ||
                snapshot.citizens.Length > MaxCitizens)
                return false;

            if (snapshot.serverId == previousServerId &&
                (snapshot.sequence <= previousSequence ||
                 snapshot.gameMinutes < previousGameMinute))
                return false;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var citizen in snapshot.citizens)
            {
                if (citizen == null || string.IsNullOrWhiteSpace(citizen.id) ||
                    citizen.id.Length > MaxCitizenIdLength || citizen.id == "player-001" ||
                    !seen.Add(citizen.id) || !Finite(citizen.x) || !Finite(citizen.z) ||
                    Math.Abs(citizen.x) > 10000f || Math.Abs(citizen.z) > 10000f)
                    return false;
            }
            return true;
        }

        public static MirSceneSnapshot FromWorld(MirWorldState world,
            string serverId = "", long sequence = 1)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.citizens == null) throw new ArgumentException("World has no citizens", nameof(world));
            var publicCitizens = new List<MirSceneCitizen>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var citizen in world.citizens)
            {
                if (publicCitizens.Count >= MaxCitizens) break;
                if (citizen == null || citizen.player ||
                    string.IsNullOrWhiteSpace(citizen.id) ||
                    citizen.id.Length > MaxCitizenIdLength || citizen.id == "player-001" ||
                    !Finite(citizen.x) || !Finite(citizen.z) ||
                    Math.Abs(citizen.x) > 10000f || Math.Abs(citizen.z) > 10000f ||
                    !seen.Add(citizen.id))
                    continue;

                publicCitizens.Add(new MirSceneCitizen
                {
                    id = citizen.id, x = citizen.x, z = citizen.z
                });
            }
            return new MirSceneSnapshot
            {
                serverId = serverId,
                sequence = sequence,
                gameMinutes = world.gameMinutes,
                citizens = publicCitizens.ToArray()
            };
        }
    }

    // A replaced server may restart its sequence at 1. Retire old serverIds to
    // reject late responses from an earlier process, without unbounded history.
    public sealed class MirSceneSnapshotCursor
    {
        const int MaxRetiredServers = 16;
        readonly HashSet<string> retired = new HashSet<string>(StringComparer.Ordinal);
        readonly Queue<string> retiredOrder = new Queue<string>();
        string lastServerId = "";
        long lastSequence;
        float lastGameMinute = -1f;

        public bool CanAccept(MirSceneSnapshot snapshot)
        {
            return MirSceneSnapshot.IsValidNext(snapshot, lastServerId, lastSequence, lastGameMinute)
                && !retired.Contains(snapshot.serverId);
        }

        public bool Accept(MirSceneSnapshot snapshot)
        {
            if (!CanAccept(snapshot)) return false;
            if (lastServerId.Length != 0 && lastServerId != snapshot.serverId)
            {
                retired.Add(lastServerId);
                retiredOrder.Enqueue(lastServerId);
                if (retiredOrder.Count > MaxRetiredServers)
                    retired.Remove(retiredOrder.Dequeue());
            }
            lastServerId = snapshot.serverId;
            lastSequence = snapshot.sequence;
            lastGameMinute = snapshot.gameMinutes;
            return true;
        }
    }
}
