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
        readonly MirSceneSnapshotCursor snapshotCursor = new MirSceneSnapshotCursor();
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
                        // The server caps NPC count; enforce a second byte limit before JSON parsing.
                        byte[] payload = request.downloadHandler == null ? null : request.downloadHandler.data;
                        if (payload == null || payload.Length == 0 ||
                            payload.Length > MirSceneSnapshot.MaxJsonBytes)
                        {
                            game.RemoteStatus = "Сервер: снимок превышает лимит";
                        }
                        else
                        {
                            MirSceneSnapshot snapshot = null;
                            try { snapshot = JsonUtility.FromJson<MirSceneSnapshot>(request.downloadHandler.text); }
                            catch (Exception error) { Debug.LogWarning("nonation spectator JSON: " + error.Message); }

                            if (Accept(snapshot))
                                game.RemoteStatus = "Сервер: подключено · NPC в реальном времени";
                            else
                                game.RemoteStatus = "Сервер: некорректные данные";
                        }
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
            // Reject invalid, stale or replayed packets before touching Unity objects.
            if (!snapshotCursor.CanAccept(snapshot)) return false;
            int knownAgents = 0;
            foreach (var resident in snapshot.citizens)
                if (agents.ContainsKey(resident.id)) knownAgents++;
            if (knownAgents == 0 || !snapshotCursor.Accept(snapshot)) return false;

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
            // Only spectator clock and visual NPC targets are updated, never money,
            // inventory, businesses or persisted local economic state.
            game.World.gameMinutes = snapshot.gameMinutes;
            return true;
        }
    }
}
