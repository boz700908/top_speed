using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static byte[] WriteLoadCustomTrack(PacketLoadCustomTrack track)
        {
            var maxLength = Math.Min(track.TrackLength, (ushort)ProtocolConstants.MaxMultiTrackLength);
            var definitionCount = Math.Min(track.Definitions.Length, maxLength);
            var payload = 1 + 12 + 1 + 1 + 2 + (definitionCount * (1 + 1 + 1 + 4));
            var buffer = WritePacketHeader(Command.LoadCustomTrack, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.LoadCustomTrack);
            writer.WriteByte(track.NrOfLaps);
            writer.WriteFixedString(track.TrackName, 12);
            writer.WriteByte((byte)track.TrackWeather);
            writer.WriteByte((byte)track.TrackAmbience);
            writer.WriteUInt16(maxLength);
            for (var i = 0; i < definitionCount; i++)
            {
                var def = track.Definitions[i];
                writer.WriteByte((byte)def.Type);
                writer.WriteByte((byte)def.Surface);
                writer.WriteByte((byte)def.Noise);
                writer.WriteSingle(def.Length);
            }
            return buffer;
        }

        public static byte[] WritePlayerJoined(PacketPlayerJoined joined)
        {
            var buffer = WritePacketHeader(Command.PlayerJoined, 4 + 1 + ProtocolConstants.MaxPlayerNameLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerJoined);
            writer.WriteUInt32(joined.PlayerId);
            writer.WriteByte(joined.PlayerNumber);
            writer.WriteFixedString(joined.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public static byte[] WriteRoomList(PacketRoomList list)
        {
            var count = Math.Min(list.Rooms.Length, ProtocolConstants.MaxRoomListEntries);
            var payload = 1 + (count * (4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12));
            var buffer = WritePacketHeader(Command.RoomList, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomList);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var room = list.Rooms[i];
                writer.WriteUInt32(room.RoomId);
                writer.WriteFixedString(room.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
                writer.WriteByte((byte)room.RoomType);
                writer.WriteByte(room.PlayerCount);
                writer.WriteByte(room.PlayersToStart);
                writer.WriteBool(room.RaceStarted);
                writer.WriteFixedString(room.TrackName ?? string.Empty, 12);
            }
            return buffer;
        }

        public static byte[] WriteRoomState(PacketRoomState state)
        {
            var count = Math.Min(state.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 1 + 12 + 1 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength));
            var buffer = WritePacketHeader(Command.RoomState, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomState);
            writer.WriteUInt32(state.RoomVersion);
            writer.WriteUInt32(state.RoomId);
            writer.WriteUInt32(state.HostPlayerId);
            writer.WriteFixedString(state.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)state.RoomType);
            writer.WriteByte(state.PlayersToStart);
            writer.WriteBool(state.InRoom);
            writer.WriteBool(state.IsHost);
            writer.WriteBool(state.RaceStarted);
            writer.WriteBool(state.PreparingRace);
            writer.WriteFixedString(state.TrackName ?? string.Empty, 12);
            writer.WriteByte(state.Laps);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var player = state.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.State);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            }
            return buffer;
        }

        public static byte[] WriteRoomGet(PacketRoomGet packet)
        {
            var count = Math.Min(packet.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 1 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12 + 1 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength));
            var buffer = WritePacketHeader(Command.RoomGet, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomGet);
            writer.WriteBool(packet.Found);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.HostPlayerId);
            writer.WriteFixedString(packet.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)packet.RoomType);
            writer.WriteByte(packet.PlayersToStart);
            writer.WriteBool(packet.RaceStarted);
            writer.WriteBool(packet.PreparingRace);
            writer.WriteFixedString(packet.TrackName ?? string.Empty, 12);
            writer.WriteByte(packet.Laps);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var player = packet.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.State);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            }

            return buffer;
        }

        public static byte[] WriteRoomEvent(PacketRoomEvent evt)
        {
            var payload = 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 12 + 1 +
                ProtocolConstants.MaxRoomNameLength + 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength;
            var buffer = WritePacketHeader(Command.RoomEvent, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomEvent);
            writer.WriteUInt32(evt.RoomId);
            writer.WriteUInt32(evt.RoomVersion);
            writer.WriteByte((byte)evt.Kind);
            writer.WriteUInt32(evt.HostPlayerId);
            writer.WriteByte((byte)evt.RoomType);
            writer.WriteByte(evt.PlayerCount);
            writer.WriteByte(evt.PlayersToStart);
            writer.WriteBool(evt.RaceStarted);
            writer.WriteBool(evt.PreparingRace);
            writer.WriteFixedString(evt.TrackName ?? string.Empty, 12);
            writer.WriteByte(evt.Laps);
            writer.WriteFixedString(evt.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteUInt32(evt.SubjectPlayerId);
            writer.WriteByte(evt.SubjectPlayerNumber);
            writer.WriteByte((byte)evt.SubjectPlayerState);
            writer.WriteFixedString(evt.SubjectPlayerName ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

    }
}
