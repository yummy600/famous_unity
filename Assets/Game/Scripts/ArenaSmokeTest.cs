#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace FamousGreatGame
{
    public sealed class ArenaSmokeTest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RunWhenRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool host = Array.IndexOf(args, "-arena-smoke-host") >= 0;
            if (!host && Array.IndexOf(args, "-arena-smoke-client") < 0)
                return;

            new GameObject("Arena smoke test").AddComponent<ArenaSmokeTest>().StartCoroutine(Run(host));
        }

        private static IEnumerator Run(bool host)
        {
            var session = FindAnyObjectByType<ArenaSession>();
            if (session == null)
            {
                Finish(host, false, "Arena session missing");
                yield break;
            }

            var manager = session.GetComponent<NetworkManager>();
            session.StartSession(host);
            if (!manager.IsListening)
            {
                Finish(host, false, "transport did not start");
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + 20f;
            ArenaPlayer client = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                var players = FindObjectsByType<ArenaPlayer>(FindObjectsSortMode.None);
                if (players.Length == 2 && players[0].IsSpawned && players[1].IsSpawned &&
                    players[0].IsOwner != players[1].IsOwner)
                {
                    client = players[0].IsOwner == !host ? players[0] : players[1];
                    break;
                }
                yield return null;
            }

            if (client == null)
            {
                Finish(host, false, "did not see a local and a remote player");
                yield break;
            }

            if (!host)
                client.SmokeInput = Vector2.right;

            while (client != null && client.transform.position.x < 4.5f && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (client == null || client.transform.position.x < 4.5f)
            {
                Finish(host, false, "client movement did not replicate");
                yield break;
            }

            if (!host)
                manager.Shutdown();

            while (Time.realtimeSinceStartup < deadline)
            {
                bool finished = host ? client == null && manager.ConnectedClientsIds.Count == 1 : !manager.IsListening;
                if (finished)
                {
                    Finish(host, true, "spawn, movement and disconnect confirmed");
                    yield break;
                }
                yield return null;
            }
            Finish(host, false, "client did not disconnect cleanly");
        }

        private static void Finish(bool host, bool passed, string message)
        {
            string result = $"ARENA_SMOKE {(passed ? "PASS" : "FAIL")} {(host ? "host" : "client")}: {message}";
            if (passed)
                Debug.Log(result);
            else
                Debug.LogError(result);
            Application.Quit(passed ? 0 : 1);
        }
    }
}
#endif
