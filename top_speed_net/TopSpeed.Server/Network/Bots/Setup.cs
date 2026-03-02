using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private RoomBot CreateBot(RaceRoom room)
        {
            var name = (_faker.Name.FirstName() ?? "Bot").Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = "Bot";
            if (name.Length > ProtocolConstants.MaxPlayerNameLength)
                name = name.Substring(0, ProtocolConstants.MaxPlayerNameLength);

            var car = (CarType)_random.Next((int)CarType.Vehicle1, (int)CarType.CustomVehicle);
            var bot = new RoomBot
            {
                Id = _nextBotId++,
                PlayerNumber = (byte)FindFreeRoomNumber(room),
                Name = name,
                Difficulty = (BotDifficulty)_random.Next(0, 3),
                AddedOrder = room.Bots.Count == 0 ? 1 : room.Bots.Max(b => b.AddedOrder) + 1,
                Car = car,
                AutomaticTransmission = _random.Next(0, 2) == 0
            };

            ApplyVehicleDimensions(bot, car);
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            return bot;
        }

        private static int GetRoomParticipantCount(RaceRoom room)
        {
            return room.PlayerIds.Count + room.Bots.Count;
        }

        private static string DifficultyLabel(BotDifficulty difficulty)
        {
            return difficulty switch
            {
                BotDifficulty.Easy => "easy",
                BotDifficulty.Hard => "hard",
                _ => "normal"
            };
        }

        private float GetBotReactionDelay(BotDifficulty difficulty)
        {
            return difficulty switch
            {
                BotDifficulty.Hard => 0.1f + (float)_random.NextDouble() * 0.4f,
                BotDifficulty.Normal => 1.0f + (float)_random.NextDouble() * 1.5f,
                _ => 2.5f + (float)_random.NextDouble() * 2.5f
            };
        }

        private static string FormatBotDisplayName(RoomBot bot)
        {
            var label = $"{FormatBotJoinName(bot)} ({DifficultyLabel(bot.Difficulty)})";
            if (label.Length > ProtocolConstants.MaxPlayerNameLength)
                return label.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            return label;
        }

        private static string FormatBotJoinName(RoomBot bot)
        {
            var label = $"Bot {bot.Name}";
            if (label.Length > ProtocolConstants.MaxPlayerNameLength)
                return label.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            return label;
        }

    }
}
