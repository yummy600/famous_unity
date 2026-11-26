using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace FamousGreatGame
{
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class ArenaSession : MonoBehaviour
    {
        private const int MaxPlayers = 8;
        private NetworkManager manager;
        private UnityTransport transport;
        private string address = "127.0.0.1";
        private string portText = "7777";
        private string status = "Ready to host or join.";
        private bool leaving;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            Application.runInBackground = true;
        }

        private void OnEnable()
        {
            manager.ConnectionApprovalCallback += Approve;
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
            manager.OnClientStopped += OnStopped;
        }

        private void OnDisable()
        {
            manager.ConnectionApprovalCallback -= Approve;
            manager.OnClientConnectedCallback -= OnConnected;
            manager.OnClientDisconnectCallback -= OnDisconnected;
            manager.OnClientStopped -= OnStopped;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 320, 235), GUI.skin.box);
            GUILayout.Label("FAMOUS GREAT PROJECT / ARENA");
            if (!manager.IsListening && !leaving)
            {
                GUILayout.Label("Host IPv4 address (for Join)");
                address = GUILayout.TextField(address, 32);
                GUILayout.Label("UDP port");
                portText = GUILayout.TextField(portText, 5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host")) StartSession(true);
                if (GUILayout.Button("Join")) StartSession(false);
                GUILayout.EndHorizontal();
            }
            else if (!leaving && GUILayout.Button(manager.IsHost ? "End session" : "Leave / cancel"))
            {
                leaving = true;
                status = "Closing session...";
                manager.Shutdown();
            }

            GUILayout.Label(status);
            GUILayout.Label("Move: WASD / Arrow keys\nCyan: you | Orange: other players");
            GUILayout.EndArea();
        }

        internal void StartSession(bool host)
        {
            if (!ushort.TryParse(portText, out ushort port) || port == 0)
            {
                status = "Enter a UDP port from 1 to 65535.";
                return;
            }

            if (host)
            {
                transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
                status = manager.StartHost() ? $"Hosting on UDP {port}." : "Failed to start host. Check the console and UDP port.";
                return;
            }

            if (!IPAddress.TryParse(address.Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            {
                status = "Enter the host's IPv4 address.";
                return;
            }

            status = $"Connecting to {ip}:{port}...";
            transport.SetConnectionData(ip.ToString(), port);
            if (!manager.StartClient())
                status = "Failed to start client. Check the console.";
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = manager.ConnectedClientsIds.Count < MaxPlayers;
            response.CreatePlayerObject = response.Approved;
            response.Reason = response.Approved ? string.Empty : "The arena is full.";
            response.Pending = false;
        }

        private void OnConnected(ulong id)
        {
            if (!manager.IsServer && id == manager.LocalClientId)
                status = "Connected.";
        }

        private void OnDisconnected(ulong id)
        {
            if (!manager.IsServer && !leaving)
                status = string.IsNullOrEmpty(manager.DisconnectReason)
                    ? "Disconnected from host."
                    : manager.DisconnectReason;
        }

        private void OnStopped(bool wasHost)
        {
            if (leaving || wasHost)
                status = "Session closed.";
            leaving = false;
        }
    }
}
