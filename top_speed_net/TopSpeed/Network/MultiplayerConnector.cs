using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed class MultiplayerConnector
    {
        public async Task<ConnectResult> ConnectAsync(string host, int port, string callSign, TimeSpan timeout, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(host))
                return ConnectResult.CreateFail("No server address was provided.");

            IPAddress? address = null;
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                foreach (var candidate in addresses)
                {
                    if (candidate.AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = candidate;
                        break;
                    }
                }

                address ??= addresses.Length > 0 ? addresses[0] : null;
            }
            catch (SocketException ex)
            {
                return ConnectResult.CreateFail($"Unable to resolve server address: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return ConnectResult.CreateFail($"Unable to resolve server address: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return ConnectResult.CreateFail($"Unable to resolve server address: {ex.Message}");
            }

            if (address == null)
                return ConnectResult.CreateFail("Unable to resolve server address.");

            if (port <= 0 || port > 65535)
                port = ClientProtocol.DefaultServerPort;

            var sanitizedCallSign = SanitizeCallSign(callSign);
            var endpoint = new IPEndPoint(address, port);

            var incoming = new ConcurrentQueue<IncomingPacket>();
            var listener = new EventBasedNetListener();
            NetPeer? connectedPeer = null;
            var disconnected = false;
            var disconnectReason = string.Empty;

            listener.PeerConnectedEvent += peer => connectedPeer = peer;
            listener.PeerDisconnectedEvent += (_, info) =>
            {
                disconnected = true;
                disconnectReason = info.Reason.ToString();
                incoming.Enqueue(new IncomingPacket(
                    Command.Disconnect,
                    new[] { ProtocolConstants.Version, (byte)Command.Disconnect },
                    DateTime.UtcNow.Ticks));
            };
            listener.NetworkReceiveEvent += (_, reader, _, _) =>
            {
                var data = reader.GetRemainingBytes();
                reader.Recycle();
                if (ClientPacketSerializer.TryReadHeader(data, out var command))
                    incoming.Enqueue(new IncomingPacket(command, data, DateTime.UtcNow.Ticks));
            };

            var manager = new NetManager(listener)
            {
                UpdateTime = 1,
                ChannelsCount = PacketStreams.Count
            };

            if (!manager.Start())
                return ConnectResult.CreateFail("Failed to initialize network client.");

            manager.Connect(endpoint.Address.ToString(), endpoint.Port, ProtocolConstants.ConnectionKey);

            var protocolHello = BuildProtocolHelloPacket();
            var hello = BuildPlayerHelloPacket(sanitizedCallSign);
            var initialState = BuildPlayerStatePacket();
            var keepAlive = new[] { ProtocolConstants.Version, (byte)Command.KeepAlive };
            var protocolHelloSent = false;
            var protocolNegotiated = false;
            var nextKeepAliveUtc = DateTime.UtcNow;
            byte? playerNumber = null;
            uint? playerId = null;
            string? motd = null;
            PacketProtocolWelcome? protocolWelcome = null;
            string? protocolFailureMessage = null;
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
            {
                manager.PollEvents();

                if (disconnected && connectedPeer == null)
                {
                    manager.Stop();
                    return ConnectResult.CreateFail($"Connection failed: {disconnectReason}");
                }

                if (!protocolHelloSent && connectedPeer != null && connectedPeer.ConnectionState == ConnectionState.Connected)
                {
                    try
                    {
                        connectedPeer.Send(protocolHello, DeliveryMethod.ReliableOrdered);
                        protocolHelloSent = true;
                        nextKeepAliveUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        manager.Stop();
                        return ConnectResult.CreateFail($"Failed to send handshake: {ex.Message}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        manager.Stop();
                        return ConnectResult.CreateFail($"Failed to send handshake: {ex.Message}");
                    }
                }

                if (protocolHelloSent && connectedPeer != null && DateTime.UtcNow >= nextKeepAliveUtc)
                {
                    try
                    {
                        connectedPeer.Send(keepAlive, DeliveryMethod.Unreliable);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore keepalive failures during connect.
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore keepalive failures during connect.
                    }

                    nextKeepAliveUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                }

                var disconnectedDuringPoll = false;
                while (incoming.TryDequeue(out var packet))
                {
                    if (packet.Command == Command.Disconnect)
                    {
                        if (ClientPacketSerializer.TryReadDisconnect(packet.Payload, out var disconnectMessage) &&
                            !string.IsNullOrWhiteSpace(disconnectMessage))
                        {
                            protocolFailureMessage = disconnectMessage;
                        }
                        disconnectedDuringPoll = true;
                        continue;
                    }

                    if (packet.Command == Command.PlayerNumber && ClientPacketSerializer.TryReadPlayer(packet.Payload, out var assigned))
                    {
                        if (!protocolNegotiated)
                        {
                            manager.Stop();
                            return ConnectResult.CreateFail("This server does not support required protocol negotiation. Please update your server.");
                        }

                        playerId = assigned.PlayerId;
                        playerNumber = assigned.PlayerNumber;
                        if (!string.IsNullOrWhiteSpace(motd))
                            return ConnectResult.CreateSuccess(manager, connectedPeer, endpoint, assigned.PlayerId, assigned.PlayerNumber, motd, sanitizedCallSign, incoming, protocolWelcome);
                    }
                    else if (packet.Command == Command.ServerInfo && ClientPacketSerializer.TryReadServerInfo(packet.Payload, out var info))
                    {
                        motd = info.Motd;
                        if (playerId.HasValue && playerNumber.HasValue)
                            return ConnectResult.CreateSuccess(manager, connectedPeer, endpoint, playerId.Value, playerNumber.Value, motd, sanitizedCallSign, incoming, protocolWelcome);
                    }
                    else if (packet.Command == Command.ProtocolWelcome && ClientPacketSerializer.TryReadProtocolWelcome(packet.Payload, out var welcome))
                    {
                        protocolWelcome = welcome;

                        if (!IsCompatibilityAccepted(welcome.Status))
                        {
                            protocolFailureMessage = string.IsNullOrWhiteSpace(welcome.Message)
                                ? "Connection refused due to protocol mismatch."
                                : welcome.Message;
                            disconnectedDuringPoll = true;
                            continue;
                        }

                        if (!protocolNegotiated && connectedPeer != null)
                        {
                            try
                            {
                                connectedPeer.Send(hello, DeliveryMethod.ReliableOrdered);
                                connectedPeer.Send(initialState, DeliveryMethod.ReliableOrdered);
                                protocolNegotiated = true;
                            }
                            catch (ObjectDisposedException ex)
                            {
                                manager.Stop();
                                return ConnectResult.CreateFail($"Failed to send handshake: {ex.Message}");
                            }
                            catch (InvalidOperationException ex)
                            {
                                manager.Stop();
                                return ConnectResult.CreateFail($"Failed to send handshake: {ex.Message}");
                            }
                        }
                    }
                    else if (packet.Command == Command.ProtocolMessage && ClientPacketSerializer.TryReadProtocolMessage(packet.Payload, out var protocolMessage))
                    {
                        // Surface immediate protocol errors during handshake.
                        if (protocolMessage.Code == ProtocolMessageCode.Failed)
                        {
                            protocolFailureMessage = string.IsNullOrWhiteSpace(protocolMessage.Message)
                                ? "Connection refused by server."
                                : protocolMessage.Message;
                            disconnectedDuringPoll = true;
                            continue;
                        }
                    }
                }

                if (disconnectedDuringPoll)
                {
                    manager.Stop();
                    if (!string.IsNullOrWhiteSpace(protocolFailureMessage))
                        return ConnectResult.CreateFail(protocolFailureMessage!);
                    if (protocolWelcome != null && !IsCompatibilityAccepted(protocolWelcome.Status))
                    {
                        var fallback = BuildProtocolRefusalFallback(protocolWelcome);
                        return ConnectResult.CreateFail(fallback);
                    }

                    var reason = string.IsNullOrWhiteSpace(disconnectReason)
                        ? "The server refused the connection."
                        : $"The server refused the connection. Details: {disconnectReason}.";
                    return ConnectResult.CreateFail(reason);
                }

                if (protocolNegotiated && playerId.HasValue && playerNumber.HasValue && connectedPeer != null)
                    return ConnectResult.CreateSuccess(manager, connectedPeer, endpoint, playerId.Value, playerNumber.Value, motd, sanitizedCallSign, incoming, protocolWelcome);

                await Task.Delay(10, token);
            }

            manager.Stop();
            if (protocolHelloSent && !protocolNegotiated)
                return ConnectResult.CreateFail("No protocol negotiation response from server. The server may be outdated or incompatible.");
            return ConnectResult.CreateFail("No response from server. The server may be offline or unreachable.");
        }

        private static string SanitizeCallSign(string callSign)
        {
            var trimmed = (callSign ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                trimmed = "Player";
            if (trimmed.Length > ProtocolConstants.MaxPlayerNameLength)
                trimmed = trimmed.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            return trimmed;
        }

        private static byte[] BuildPlayerHelloPacket(string callSign)
        {
            var buffer = new byte[2 + ProtocolConstants.MaxPlayerNameLength];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)Command.PlayerHello;
            var bytes = Encoding.ASCII.GetBytes(callSign ?? string.Empty);
            var count = Math.Min(bytes.Length, ProtocolConstants.MaxPlayerNameLength);
            Array.Copy(bytes, 0, buffer, 2, count);
            for (var i = 2 + count; i < buffer.Length; i++)
                buffer[i] = 0;
            return buffer;
        }

        private static byte[] BuildProtocolHelloPacket()
        {
            var packet = new PacketProtocolHello
            {
                ClientVersion = ProtocolProfile.Current,
                MinSupported = ProtocolProfile.ClientSupported.MinSupported,
                MaxSupported = ProtocolProfile.ClientSupported.MaxSupported
            };
            return ClientPacketSerializer.WriteProtocolHello(packet);
        }

        private static byte[] BuildPlayerStatePacket()
        {
            var buffer = new byte[2 + 4 + 1 + 1];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)Command.PlayerState;
            var idBytes = BitConverter.GetBytes(0u);
            buffer[2] = idBytes[0];
            buffer[3] = idBytes[1];
            buffer[4] = idBytes[2];
            buffer[5] = idBytes[3];
            buffer[6] = 0;
            buffer[7] = (byte)PlayerState.NotReady;
            return buffer;
        }

        private static bool IsCompatibilityAccepted(ProtocolCompatStatus status)
        {
            return status == ProtocolCompatStatus.Exact || status == ProtocolCompatStatus.CompatibleDowngrade;
        }

        private static string BuildProtocolRefusalFallback(PacketProtocolWelcome welcome)
        {
            var range = new ProtocolRange(welcome.ServerMinSupported, welcome.ServerMaxSupported);
            return $"Your client version is {ProtocolProfile.Current}. This server supports versions {range}.";
        }
    }

    internal readonly struct CompatibilityNotice
    {
        public CompatibilityNotice(ProtocolCompatStatus status, ProtocolVer clientVersion, ProtocolRange serverSupported, string message)
        {
            Status = status;
            ClientVersion = clientVersion;
            ServerSupported = serverSupported;
            Message = message ?? string.Empty;
        }

        public ProtocolCompatStatus Status { get; }
        public ProtocolVer ClientVersion { get; }
        public ProtocolRange ServerSupported { get; }
        public string Message { get; }
    }

    internal readonly struct ConnectResult
    {
        private ConnectResult(bool success, string message, MultiplayerSession? session, string? motd, CompatibilityNotice? compatibilityNotice)
        {
            Success = success;
            Message = message;
            Session = session;
            Address = session?.Address;
            Port = session?.Port ?? 0;
            PlayerNumber = session?.PlayerNumber ?? 0;
            PlayerId = session?.PlayerId ?? 0;
            Motd = motd ?? string.Empty;
            CompatibilityNotice = compatibilityNotice;
        }

        public bool Success { get; }
        public string Message { get; }
        public MultiplayerSession? Session { get; }
        public IPAddress? Address { get; }
        public int Port { get; }
        public byte PlayerNumber { get; }
        public uint PlayerId { get; }
        public string Motd { get; }
        public CompatibilityNotice? CompatibilityNotice { get; }
        public bool RequiresCompatibilityConfirmation => CompatibilityNotice.HasValue && CompatibilityNotice.Value.Status == ProtocolCompatStatus.CompatibleDowngrade;

        public static ConnectResult CreateSuccess(
            NetManager manager,
            NetPeer? peer,
            IPEndPoint endPoint,
            uint playerId,
            byte playerNumber,
            string? motd,
            string? playerName,
            ConcurrentQueue<IncomingPacket> incoming,
            PacketProtocolWelcome? protocolWelcome)
        {
            if (peer == null)
            {
                manager.Stop();
                return CreateFail("Connection lost before session initialization.");
            }

            var session = new MultiplayerSession(manager, peer, endPoint, playerId, playerNumber, motd, playerName, incoming);
            return new ConnectResult(true, "Connected.", session, motd, BuildCompatibilityNotice(protocolWelcome));
        }

        public static ConnectResult CreateFail(string message)
        {
            return new ConnectResult(false, message ?? "Connection failed.", null, null, null);
        }

        private static CompatibilityNotice? BuildCompatibilityNotice(PacketProtocolWelcome? welcome)
        {
            if (welcome == null)
                return null;
            if (welcome.Status != ProtocolCompatStatus.CompatibleDowngrade)
                return null;

            var range = new ProtocolRange(welcome.ServerMinSupported, welcome.ServerMaxSupported);
            var message = string.IsNullOrWhiteSpace(welcome.Message)
                ? "The server supports an older protocol range. You can continue, but some features may behave differently."
                : welcome.Message;
            return new CompatibilityNotice(
                welcome.Status,
                ProtocolProfile.Current,
                range,
                message);
        }
    }
}
