using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace MIR
{
    [Serializable]
    public sealed class MirNetworkConfig
    {
        // Leave empty to keep the fully offline game. No credentials belong here.
        public string snapshotUrl = "";
    }

    // WebGL-friendly transport, intentionally READ ONLY. Player actions are not
    // forwarded to the server until identities and authenticated commands exist.
    public sealed class MirRemoteWorldSync : MonoBehaviour
    {
        MirGame game;
        string endpoint;
        readonly Dictionary<string, MirNpcAgent> agents = new Dictionary<string, MirNpcAgent>();
        float lastGameMinute = -1f;
        string lastServerId = "";
        bool activated;

        public static void TryAttach(MirGame world)
        {
            var asset = Resources.Load<TextAsset>("MIR/NetworkConfig");
            if (asset == null || string.IsNullOrWhiteSpace(asset.text)) return;

            MirNetworkConfig config;
            try { config = JsonUtility.FromJson<MirNetworkConfig>(asset.text); }
            catch (Exception error)
            {
                Debug.LogWarning("nonation network config is invalid: " + error.Message);
                return;
            }
            if (config == null || string.IsNullOrWhiteSpace(config.snapshotUrl)) return;

            Uri url;
            if (!Uri.TryCreate(config.snapshotUrl.Trim(), UriKind.Absolute, out url) ||
                !url.AbsolutePath.EndsWith("/api/v1/spectator/scene", StringComparison.Ordinal) ||
                !string.IsNullOrEmpty(url.UserInfo) || !string.IsNullOrEmpty(url.Fragment) ||
                !(url.Scheme == Uri.UriSchemeHttps ||
                 (Application.isEditor && url.Scheme == Uri.UriSchemeHttp &&
                  (url.Host == "localhost" || url.Host == "127.0.0.1"))))
            {
                Debug.LogError("nonation: spectator URL must use HTTPS and the public scene endpoint.");
                return;
            }

            var client = new GameObject("nonation · Remote spectator sync")
                .AddComponent<MirRemoteWorldSync>();
            client.game = world;
            client.endpoint = url.AbsoluteUri;
            foreach (var agent in world.NPCs)
                if (agent != null && !string.IsNullOrEmpty(agent.CitizenId))
                    client.agents[agent.CitizenId] = agent;
            world.RemoteStatus = "Подключение к серверу";
            client.StartCoroutine(client.Poll());
        }

        IEnumerator Poll()
        {
            while (game != null)
            {
                using (var request = UnityWebRequest.Get(endpoint))
                {
                    request.timeout = 8;
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success &&
                        request.responseCode == 200)
                    {
                        MirSceneSnapshot snapshot = null;
                        try { snapshot = JsonUtility.FromJson<MirSceneSnapshot>(request.downloadHandler.text); }
                        catch (Exception error) { Debug.LogWarning("nonation spectator JSON: " + error.Message); }

                        if (Accept(snapshot))
                            game.RemoteStatus = "Сервер: подключено · NPC в реальном времени";
                        else
                            game.RemoteStatus = "Сервер: некорректные данные";
                    }
                    else
                    {
                        game.RemoteStatus = "Сервер недоступен · повторное подключение";
                    }
                }
                yield return new WaitForSecondsRealtime(activated ? .5f : 3f);
            }
        }

        bool Accept(MirSceneSnapshot snapshot)
        {
            if (snapshot == null || snapshot.version != 1 || snapshot.citizens == null ||
                snapshot.citizens.Length == 0 || snapshot.citizens.Length > 10000 ||
                string.IsNullOrEmpty(snapshot.serverId) || snapshot.serverId.Length > 64 ||
                float.IsNaN(snapshot.gameMinutes) || float.IsInfinity(snapshot.gameMinutes) ||
                snapshot.gameMinutes < 0f ||
                (snapshot.serverId == lastServerId && snapshot.gameMinutes + 1f < lastGameMinute))
                return false;

            // Validate the complete response BEFORE changing any game object.
            var seen = new HashSet<string>();
            int knownAgents = 0;
            foreach (var resident in snapshot.citizens)
            {
                if (resident == null || string.IsNullOrEmpty(resident.id) ||
                    resident.id.Length > 128 ||
                    !seen.Add(resident.id) || float.IsNaN(resident.x) ||
                    float.IsNaN(resident.z) || float.IsInfinity(resident.x) ||
                    float.IsInfinity(resident.z) ||
                    Mathf.Abs(resident.x) > 10000f || Mathf.Abs(resident.z) > 10000f)
                    return false;
                if(agents.ContainsKey(resident.id))knownAgents++;
            }
            if (knownAgents == 0) return false;

            if (!activated)
            {
                game.ActivateRemoteSpectator();
                activated = true;
            }

            foreach (var resident in snapshot.citizens)
            {
                MirNpcAgent agent;
                if (agents.TryGetValue(resident.id, out agent) && agent != null)
                    agent.ApplyRemotePosition(resident.x, resident.z);
            }
            game.World.gameMinutes = snapshot.gameMinutes;
            lastServerId = snapshot.serverId;
            lastGameMinute = snapshot.gameMinutes;
            return true;
        }
    }
}
