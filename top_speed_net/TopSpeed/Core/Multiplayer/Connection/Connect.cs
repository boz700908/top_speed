using System;
using System.Threading;
using TopSpeed.Network;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private bool HandleServerAddressInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _speech.Speak("Please enter a server address.");
                return false;
            }

            var host = trimmed;
            int? overridePort = null;
            var lastColon = trimmed.LastIndexOf(':');
            if (lastColon > 0 && lastColon < trimmed.Length - 1)
            {
                var portPart = trimmed.Substring(lastColon + 1);
                if (int.TryParse(portPart, out var parsedPort))
                {
                    host = trimmed.Substring(0, lastColon);
                    overridePort = parsedPort;
                }
            }

            _settings.LastServerAddress = host;
            _saveSettings();
            _pendingServerAddress = host;
            _pendingServerPort = overridePort ?? ResolveServerPort();
            BeginCallSignInput();
            return true;
        }

        private void BeginCallSignInput()
        {
            PromptCallSignInput(null);
        }

        private bool HandleCallSignInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _speech.Speak("Call sign cannot be empty.");
                return false;
            }

            _pendingCallSign = trimmed;
            AttemptConnect(_pendingServerAddress, _pendingServerPort, _pendingCallSign);
            return true;
        }

        private void AttemptConnect(string host, int port, string callSign)
        {
            _speech.Speak("Attempting to connect, please wait...");
            _clearSession();
            _pingPending = false;
            StartConnectingPulse();
            _connectCts?.Cancel();
            _connectCts?.Dispose();
            _connectCts = new CancellationTokenSource();
            _connectTask = _connector.ConnectAsync(host, port, callSign, TimeSpan.FromSeconds(3), _connectCts.Token);
        }

        private void HandleConnectResult(ConnectResult result)
        {
            StopConnectingPulse();
            if (result.Success && result.Session != null)
            {
                var session = result.Session;
                _setSession(session);
                _resetPendingState();

                OnSessionCleared();
                PlayNetworkSound("connected.ogg");

                var welcome = "Connected to server.";
                if (!string.IsNullOrWhiteSpace(result.Motd))
                    welcome += $" Message of the day: {result.Motd}.";
                _speech.Speak(welcome);
                _menu.FadeOutMenuMusic();
                _menu.ShowRoot(MultiplayerLobbyMenuId);
                _enterMenuState();
                return;
            }

            _speech.Speak($"Failed to connect: {result.Message}");
            _enterMenuState();
        }
    }
}
