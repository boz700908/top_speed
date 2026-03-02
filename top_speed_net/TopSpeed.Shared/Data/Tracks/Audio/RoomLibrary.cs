using System;
using System.Collections.Generic;

namespace TopSpeed.Data
{
    public static class TrackRoomLibrary
    {
        private struct RoomValues
        {
            public float ReverbTimeSeconds;
            public float ReverbGain;
            public float HfDecayRatio;
            public float LateReverbGain;
            public float Diffusion;
            public float AirAbsorption;
            public float OcclusionScale;
            public float TransmissionScale;
        }

        private static readonly Dictionary<string, RoomValues> Presets =
            new Dictionary<string, RoomValues>(StringComparer.OrdinalIgnoreCase)
            {
                ["outdoor_open"] = R(0.35f, 0.08f, 0.85f, 0.08f, 0.20f, 0.65f, 0.35f, 0.75f),
                ["outdoor_field"] = R(0.45f, 0.10f, 0.82f, 0.10f, 0.25f, 0.62f, 0.38f, 0.72f),
                ["outdoor_urban"] = R(0.90f, 0.22f, 0.70f, 0.24f, 0.45f, 0.48f, 0.55f, 0.52f),
                ["outdoor_suburban"] = R(0.75f, 0.18f, 0.74f, 0.20f, 0.38f, 0.50f, 0.48f, 0.58f),
                ["outdoor_forest"] = R(0.70f, 0.16f, 0.52f, 0.18f, 0.32f, 0.82f, 0.60f, 0.60f),
                ["outdoor_mountains"] = R(1.80f, 0.34f, 0.52f, 0.36f, 0.42f, 0.42f, 0.55f, 0.52f),
                ["outdoor_desert"] = R(0.55f, 0.12f, 0.76f, 0.12f, 0.26f, 0.58f, 0.42f, 0.66f),
                ["outdoor_snowfield"] = R(0.90f, 0.20f, 0.62f, 0.24f, 0.34f, 0.70f, 0.45f, 0.62f),
                ["outdoor_coast"] = R(0.85f, 0.20f, 0.68f, 0.22f, 0.36f, 0.52f, 0.46f, 0.60f),
                ["outdoor_valley"] = R(1.60f, 0.30f, 0.56f, 0.34f, 0.46f, 0.46f, 0.58f, 0.52f),

                ["tunnel_short"] = R(1.10f, 0.48f, 0.62f, 0.52f, 0.72f, 0.22f, 0.80f, 0.32f),
                ["tunnel_medium"] = R(1.80f, 0.60f, 0.56f, 0.62f, 0.78f, 0.20f, 0.86f, 0.26f),
                ["tunnel_long"] = R(2.70f, 0.72f, 0.50f, 0.76f, 0.82f, 0.18f, 0.90f, 0.22f),
                ["tunnel_concrete"] = R(2.10f, 0.66f, 0.54f, 0.70f, 0.80f, 0.20f, 0.88f, 0.24f),
                ["tunnel_brick"] = R(1.70f, 0.58f, 0.58f, 0.62f, 0.76f, 0.22f, 0.84f, 0.30f),
                ["tunnel_metal"] = R(2.00f, 0.70f, 0.46f, 0.74f, 0.84f, 0.16f, 0.88f, 0.22f),
                ["tunnel_stone"] = R(2.40f, 0.68f, 0.50f, 0.72f, 0.80f, 0.18f, 0.90f, 0.24f),

                ["underpass_small"] = R(0.95f, 0.38f, 0.62f, 0.42f, 0.62f, 0.26f, 0.78f, 0.34f),
                ["underpass_large"] = R(1.35f, 0.46f, 0.56f, 0.52f, 0.68f, 0.24f, 0.82f, 0.30f),
                ["overhang"] = R(0.75f, 0.30f, 0.66f, 0.34f, 0.56f, 0.28f, 0.72f, 0.38f),
                ["bridge_truss"] = R(0.65f, 0.24f, 0.64f, 0.26f, 0.44f, 0.34f, 0.60f, 0.46f),

                ["garage_small"] = R(0.95f, 0.40f, 0.64f, 0.44f, 0.62f, 0.30f, 0.72f, 0.34f),
                ["garage_medium"] = R(1.30f, 0.48f, 0.60f, 0.52f, 0.68f, 0.28f, 0.74f, 0.30f),
                ["garage_large"] = R(1.80f, 0.56f, 0.58f, 0.60f, 0.72f, 0.26f, 0.76f, 0.28f),
                ["parking_open"] = R(0.70f, 0.20f, 0.70f, 0.22f, 0.38f, 0.40f, 0.48f, 0.58f),
                ["parking_covered"] = R(1.20f, 0.44f, 0.60f, 0.48f, 0.64f, 0.28f, 0.72f, 0.34f),
                ["parking_underground"] = R(1.90f, 0.62f, 0.54f, 0.66f, 0.76f, 0.22f, 0.84f, 0.24f),

                ["warehouse_small"] = R(1.10f, 0.38f, 0.62f, 0.42f, 0.66f, 0.30f, 0.70f, 0.34f),
                ["warehouse_medium"] = R(1.70f, 0.50f, 0.56f, 0.56f, 0.74f, 0.28f, 0.74f, 0.30f),
                ["warehouse_large"] = R(2.40f, 0.62f, 0.50f, 0.68f, 0.80f, 0.24f, 0.78f, 0.26f),
                ["factory_hall"] = R(2.20f, 0.60f, 0.48f, 0.66f, 0.78f, 0.24f, 0.80f, 0.26f),
                ["machine_shop"] = R(1.30f, 0.44f, 0.54f, 0.50f, 0.70f, 0.26f, 0.76f, 0.30f),

                ["hangar_small"] = R(2.00f, 0.56f, 0.54f, 0.60f, 0.76f, 0.24f, 0.72f, 0.30f),
                ["hangar_large"] = R(3.10f, 0.68f, 0.48f, 0.74f, 0.82f, 0.22f, 0.76f, 0.26f),
                ["airport_terminal"] = R(1.80f, 0.52f, 0.58f, 0.58f, 0.74f, 0.28f, 0.66f, 0.36f),
                ["subway_station"] = R(2.30f, 0.64f, 0.50f, 0.70f, 0.80f, 0.22f, 0.84f, 0.24f),
                ["rail_station"] = R(1.90f, 0.54f, 0.56f, 0.60f, 0.74f, 0.26f, 0.72f, 0.32f),

                ["corridor_short"] = R(0.85f, 0.36f, 0.62f, 0.40f, 0.58f, 0.30f, 0.72f, 0.34f),
                ["corridor_long"] = R(1.40f, 0.50f, 0.56f, 0.56f, 0.68f, 0.26f, 0.80f, 0.28f),
                ["stairwell_concrete"] = R(1.60f, 0.54f, 0.54f, 0.58f, 0.70f, 0.26f, 0.78f, 0.28f),
                ["basement_low"] = R(1.10f, 0.44f, 0.58f, 0.48f, 0.66f, 0.28f, 0.76f, 0.30f),
                ["basement_large"] = R(1.90f, 0.58f, 0.52f, 0.64f, 0.76f, 0.24f, 0.82f, 0.26f),
                ["bunker"] = R(2.10f, 0.64f, 0.46f, 0.70f, 0.80f, 0.20f, 0.90f, 0.20f),
                ["vault"] = R(2.60f, 0.72f, 0.42f, 0.78f, 0.84f, 0.18f, 0.92f, 0.18f),

                ["hall_small"] = R(1.10f, 0.40f, 0.64f, 0.44f, 0.70f, 0.30f, 0.68f, 0.34f),
                ["hall_medium"] = R(1.70f, 0.52f, 0.58f, 0.56f, 0.78f, 0.28f, 0.72f, 0.30f),
                ["hall_large"] = R(2.70f, 0.62f, 0.50f, 0.66f, 0.82f, 0.24f, 0.78f, 0.26f),
                ["arena_indoor"] = R(3.00f, 0.66f, 0.48f, 0.72f, 0.84f, 0.24f, 0.70f, 0.32f),
                ["stadium_open"] = R(1.50f, 0.45f, 0.60f, 0.50f, 0.70f, 0.40f, 0.40f, 0.60f),
                ["stadium_closed"] = R(2.80f, 0.64f, 0.50f, 0.70f, 0.82f, 0.28f, 0.68f, 0.34f),

                ["room_small"] = R(0.70f, 0.30f, 0.70f, 0.32f, 0.62f, 0.36f, 0.60f, 0.40f),
                ["room_medium"] = R(1.10f, 0.40f, 0.62f, 0.42f, 0.70f, 0.30f, 0.62f, 0.34f),
                ["room_large"] = R(1.80f, 0.50f, 0.54f, 0.54f, 0.76f, 0.26f, 0.68f, 0.30f),
                ["studio_dry"] = R(0.35f, 0.12f, 0.78f, 0.14f, 0.40f, 0.50f, 0.40f, 0.70f),
                ["studio_live"] = R(0.90f, 0.34f, 0.66f, 0.38f, 0.66f, 0.34f, 0.58f, 0.44f),
                ["broadcast_booth"] = R(0.28f, 0.08f, 0.82f, 0.10f, 0.30f, 0.58f, 0.45f, 0.70f),

                ["church_small"] = R(2.40f, 0.56f, 0.46f, 0.60f, 0.82f, 0.26f, 0.72f, 0.30f),
                ["church_large"] = R(3.80f, 0.70f, 0.40f, 0.76f, 0.86f, 0.22f, 0.78f, 0.24f),
                ["cathedral"] = R(5.40f, 0.78f, 0.34f, 0.84f, 0.90f, 0.20f, 0.82f, 0.20f),
                ["cave_small"] = R(2.60f, 0.62f, 0.46f, 0.66f, 0.74f, 0.20f, 0.86f, 0.24f),
                ["cave_large"] = R(4.50f, 0.78f, 0.34f, 0.84f, 0.84f, 0.16f, 0.92f, 0.16f),
                ["cave_ice"] = R(3.80f, 0.72f, 0.40f, 0.80f, 0.80f, 0.24f, 0.86f, 0.22f),
                ["canyon_narrow"] = R(2.10f, 0.54f, 0.46f, 0.58f, 0.52f, 0.34f, 0.64f, 0.42f),
                ["canyon_wide"] = R(2.90f, 0.60f, 0.44f, 0.64f, 0.48f, 0.36f, 0.56f, 0.48f),
                ["sewer_brick"] = R(2.10f, 0.62f, 0.50f, 0.68f, 0.78f, 0.22f, 0.86f, 0.22f),
                ["sewer_concrete"] = R(2.40f, 0.68f, 0.46f, 0.74f, 0.82f, 0.20f, 0.88f, 0.20f)
            };

        private static RoomValues R(
            float reverbTimeSeconds,
            float reverbGain,
            float hfDecayRatio,
            float lateReverbGain,
            float diffusion,
            float airAbsorption,
            float occlusionScale,
            float transmissionScale)
        {
            return new RoomValues
            {
                ReverbTimeSeconds = reverbTimeSeconds,
                ReverbGain = reverbGain,
                HfDecayRatio = hfDecayRatio,
                LateReverbGain = lateReverbGain,
                Diffusion = diffusion,
                AirAbsorption = airAbsorption,
                OcclusionScale = occlusionScale,
                TransmissionScale = transmissionScale
            };
        }

        public static bool IsPreset(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            return Presets.ContainsKey(name.Trim());
        }

        public static bool TryGetPreset(string name, out TrackRoomDefinition room)
        {
            room = null!;
            if (string.IsNullOrWhiteSpace(name))
                return false;
            if (!Presets.TryGetValue(name.Trim(), out var values))
                return false;

            var id = name.Trim();
            room = new TrackRoomDefinition(
                id,
                id,
                values.ReverbTimeSeconds,
                values.ReverbGain,
                values.HfDecayRatio,
                values.LateReverbGain,
                values.Diffusion,
                values.AirAbsorption,
                values.OcclusionScale,
                values.TransmissionScale);
            return true;
        }
    }
}
