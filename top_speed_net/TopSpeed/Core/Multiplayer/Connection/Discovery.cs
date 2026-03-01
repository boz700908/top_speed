using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Menu;
using TopSpeed.Network;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void StartServerDiscovery()
        {
            if (_discoveryTask != null && !_discoveryTask.IsCompleted)
                return;

            _speech.Speak("Please wait. Scanning for servers on the local network.");
            _discoveryCts?.Cancel();
            _discoveryCts?.Dispose();
            _discoveryCts = new CancellationTokenSource();
            _discoveryTask = Task.Run(async () =>
            {
                using var client = new DiscoveryClient();
                return await client.ScanAsync(ClientProtocol.DefaultDiscoveryPort, TimeSpan.FromSeconds(2), _discoveryCts.Token);
            }, _discoveryCts.Token);
        }

        private void HandleDiscoveryResult(IReadOnlyList<ServerInfo> servers)
        {
            if (servers == null || servers.Count == 0)
            {
                _speech.Speak("No servers were found on the local network. You can enter an address manually.");
                return;
            }

            var items = new List<MenuItem>();
            foreach (var server in servers)
            {
                var info = server;
                var label = $"{info.Address}:{info.Port}";
                items.Add(new MenuItem(label, MenuAction.None, onActivate: () => SelectDiscoveredServer(info), suppressPostActivateAnnouncement: true));
            }

            items.Add(new MenuItem("Go back", MenuAction.Back));
            _menu.UpdateItems("multiplayer_servers", items);
            _menu.Push("multiplayer_servers");
        }

        private void SelectDiscoveredServer(ServerInfo server)
        {
            _pendingServerAddress = server.Address.ToString();
            _pendingServerPort = server.Port;
            BeginCallSignInput();
        }
    }
}
