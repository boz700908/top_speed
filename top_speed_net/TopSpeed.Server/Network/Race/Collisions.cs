using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static ulong MakePairKey(uint first, uint second)
        {
            if (first > second)
            {
                var swap = first;
                first = second;
                second = swap;
            }

            return ((ulong)first << 32) | second;
        }

        private void CheckForBumps()
        {
            foreach (var room in _rooms.Values)
            {
                var racers = room.PlayerIds.Where(id => _players.TryGetValue(id, out var p) && p.State == PlayerState.Racing)
                    .Select(id => _players[id]).ToList();
                var botRacers = room.Bots.Where(bot => bot.State == PlayerState.Racing).ToList();
                var activePairs = new HashSet<ulong>();

                for (var i = 0; i < racers.Count; i++)
                {
                    for (var j = i + 1; j < racers.Count; j++)
                    {
                        var player = racers[i];
                        var other = racers[j];
                        var xThreshold = (player.WidthM + other.WidthM) * 0.5f;
                        var yThreshold = (player.LengthM + other.LengthM) * 0.5f;
                        var dx = player.PositionX - other.PositionX;
                        var dy = player.PositionY - other.PositionY;
                        if (Math.Abs(dx) >= xThreshold || Math.Abs(dy) >= yThreshold)
                            continue;

                        var pairKey = MakePairKey(player.Id, other.Id);
                        activePairs.Add(pairKey);
                        if (room.ActiveBumpPairs.Contains(pairKey))
                            continue;

                        SendStream(player, PacketSerializer.WritePlayerBumped(new PacketPlayerBumped
                        {
                            PlayerId = player.Id,
                            PlayerNumber = player.PlayerNumber,
                            BumpX = dx,
                            BumpY = dy,
                            BumpSpeed = (ushort)Math.Max(0, player.Speed - other.Speed)
                        }), PacketStream.RaceEvent, PacketDeliveryKind.Sequenced);

                        SendStream(other, PacketSerializer.WritePlayerBumped(new PacketPlayerBumped
                        {
                            PlayerId = other.Id,
                            PlayerNumber = other.PlayerNumber,
                            BumpX = -dx,
                            BumpY = -dy,
                            BumpSpeed = (ushort)Math.Max(0, other.Speed - player.Speed)
                        }), PacketStream.RaceEvent, PacketDeliveryKind.Sequenced);
                        _bumpEventsHumanHuman++;
                    }
                }

                foreach (var player in racers)
                {
                    foreach (var bot in botRacers)
                    {
                        var xThreshold = (player.WidthM + bot.WidthM) * 0.5f;
                        var yThreshold = (player.LengthM + bot.LengthM) * 0.5f;
                        var dx = player.PositionX - bot.PositionX;
                        var dy = player.PositionY - bot.PositionY;
                        if (Math.Abs(dx) >= xThreshold || Math.Abs(dy) >= yThreshold)
                            continue;

                        var pairKey = MakePairKey(player.Id, bot.Id);
                        activePairs.Add(pairKey);
                        if (room.ActiveBumpPairs.Contains(pairKey))
                            continue;

                        var botSpeed = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, (int)Math.Round(bot.SpeedKph)));
                        SendStream(player, PacketSerializer.WritePlayerBumped(new PacketPlayerBumped
                        {
                            PlayerId = player.Id,
                            PlayerNumber = player.PlayerNumber,
                            BumpX = dx,
                            BumpY = dy,
                            BumpSpeed = (ushort)Math.Max(0, player.Speed - botSpeed)
                        }), PacketStream.RaceEvent, PacketDeliveryKind.Sequenced);

                        bot.PositionX -= 2f * dx;
                        bot.PositionY -= dy;
                        if (bot.PositionY < 0f)
                            bot.PositionY = 0f;
                        bot.SpeedKph = Math.Max(0f, bot.SpeedKph * 0.8f);
                        var state = bot.PhysicsState;
                        state.PositionX = bot.PositionX;
                        state.PositionY = bot.PositionY;
                        state.SpeedKph = bot.SpeedKph;
                        bot.PhysicsState = state;
                        TriggerBotHorn(bot, "bump", 0.2f);
                        _bumpEventsHumanBot++;
                    }
                }

                room.ActiveBumpPairs.RemoveWhere(key => !activePairs.Contains(key));
                foreach (var pairKey in activePairs)
                    room.ActiveBumpPairs.Add(pairKey);
            }
        }

    }
}
