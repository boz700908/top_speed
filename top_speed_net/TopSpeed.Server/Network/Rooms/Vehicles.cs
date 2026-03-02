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
        private static CarType NormalizeNetworkCar(CarType car)
        {
            if (car < CarType.Vehicle1 || car >= CarType.CustomVehicle)
                return CarType.Vehicle1;
            return car;
        }

        private static void ApplyVehicleDimensions(PlayerConnection player, CarType car)
        {
            var dimensions = GetVehicleDimensions(car);
            player.WidthM = dimensions.WidthM;
            player.LengthM = dimensions.LengthM;
        }

        private static void ApplyVehicleDimensions(RoomBot bot, CarType car)
        {
            var dimensions = GetVehicleDimensions(car);
            bot.WidthM = dimensions.WidthM;
            bot.LengthM = dimensions.LengthM;
            bot.PhysicsConfig = BotPhysicsCatalog.Get(car);
            bot.AudioProfile = GetVehicleAudioProfile(car);
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            var state = bot.PhysicsState;
            if (state.Gear <= 0)
                state.Gear = 1;
            bot.PhysicsState = state;
        }

        private static VehicleDimensions GetVehicleDimensions(CarType car)
        {
            return car switch
            {
                CarType.Vehicle1 => new VehicleDimensions(1.895f, 4.689f),
                CarType.Vehicle2 => new VehicleDimensions(1.852f, 4.572f),
                CarType.Vehicle3 => new VehicleDimensions(1.627f, 3.546f),
                CarType.Vehicle4 => new VehicleDimensions(1.744f, 3.876f),
                CarType.Vehicle5 => new VehicleDimensions(1.811f, 4.760f),
                CarType.Vehicle6 => new VehicleDimensions(1.839f, 4.879f),
                CarType.Vehicle7 => new VehicleDimensions(2.030f, 4.780f),
                CarType.Vehicle8 => new VehicleDimensions(1.811f, 4.624f),
                CarType.Vehicle9 => new VehicleDimensions(2.019f, 5.931f),
                CarType.Vehicle10 => new VehicleDimensions(0.749f, 2.085f),
                CarType.Vehicle11 => new VehicleDimensions(0.806f, 2.110f),
                CarType.Vehicle12 => new VehicleDimensions(0.690f, 2.055f),
                _ => new VehicleDimensions(1.8f, 4.5f)
            };
        }

        private static BotAudioProfile GetVehicleAudioProfile(CarType car)
        {
            return car switch
            {
                CarType.Vehicle1 => new BotAudioProfile(22050, 55000, 26000),
                CarType.Vehicle2 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle3 => new BotAudioProfile(6000, 25000, 19000),
                CarType.Vehicle4 => new BotAudioProfile(6000, 27000, 20000),
                CarType.Vehicle5 => new BotAudioProfile(6000, 33000, 27500),
                CarType.Vehicle6 => new BotAudioProfile(7025, 40000, 32500),
                CarType.Vehicle7 => new BotAudioProfile(6000, 26000, 21000),
                CarType.Vehicle8 => new BotAudioProfile(10000, 45000, 34000),
                CarType.Vehicle9 => new BotAudioProfile(22050, 30550, 22550),
                CarType.Vehicle10 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle11 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle12 => new BotAudioProfile(22050, 27550, 23550),
                _ => new BotAudioProfile(22050, 55000, 26000)
            };
        }

    }
}
