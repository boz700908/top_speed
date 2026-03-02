using System;
using System.Collections.Generic;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal readonly struct PlayerLoadout
    {
        public PlayerLoadout(CarType car, bool automaticTransmission)
        {
            Car = car;
            AutomaticTransmission = automaticTransmission;
        }

        public CarType Car { get; }
        public bool AutomaticTransmission { get; }
    }

}
