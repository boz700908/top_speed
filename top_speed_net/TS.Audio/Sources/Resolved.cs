using System.Numerics;

namespace TS.Audio
{
    internal readonly struct ResolvedSourceOptions
    {
        public bool Spatialize { get; }
        public bool UseHrtf { get; }
        public bool Loop { get; }
        public float FadeInSeconds { get; }
        public float Volume { get; }
        public float Pitch { get; }
        public float Pan { get; }
        public bool StereoWidening { get; }
        public Vector3? Position { get; }
        public Vector3? Velocity { get; }
        public float? CurveDistanceScaler { get; }
        public float? DopplerFactor { get; }
        public RoomAcoustics? RoomAcoustics { get; }
        public DistanceModel? DistanceModel { get; }
        public float RefDistance { get; }
        public float MaxDistance { get; }
        public float RollOff { get; }

        private ResolvedSourceOptions(
            bool spatialize,
            bool useHrtf,
            bool loop,
            float fadeInSeconds,
            float volume,
            float pitch,
            float pan,
            bool stereoWidening,
            Vector3? position,
            Vector3? velocity,
            float? curveDistanceScaler,
            float? dopplerFactor,
            RoomAcoustics? roomAcoustics,
            DistanceModel? distanceModel,
            float refDistance,
            float maxDistance,
            float rollOff)
        {
            Spatialize = spatialize;
            UseHrtf = useHrtf;
            Loop = loop;
            FadeInSeconds = fadeInSeconds;
            Volume = volume;
            Pitch = pitch;
            Pan = pan;
            StereoWidening = stereoWidening;
            Position = position;
            Velocity = velocity;
            CurveDistanceScaler = curveDistanceScaler;
            DopplerFactor = dopplerFactor;
            RoomAcoustics = roomAcoustics;
            DistanceModel = distanceModel;
            RefDistance = refDistance;
            MaxDistance = maxDistance;
            RollOff = rollOff;
        }

        public static ResolvedSourceOptions Merge(PlaybackPolicy? engineDefaults, PlaybackPolicy? busDefaults, PlaybackPolicy? overrides)
        {
            return new ResolvedSourceOptions(
                ResolveBool(engineDefaults?.Spatialize, busDefaults?.Spatialize, overrides?.Spatialize, false),
                ResolveBool(engineDefaults?.UseHrtf, busDefaults?.UseHrtf, overrides?.UseHrtf, false),
                ResolveBool(engineDefaults?.Loop, busDefaults?.Loop, overrides?.Loop, false),
                ResolveFloat(engineDefaults?.FadeInSeconds, busDefaults?.FadeInSeconds, overrides?.FadeInSeconds, 0f),
                ResolveFloat(engineDefaults?.Volume, busDefaults?.Volume, overrides?.Volume, 1f),
                ResolveFloat(engineDefaults?.Pitch, busDefaults?.Pitch, overrides?.Pitch, 1f),
                ResolveFloat(engineDefaults?.Pan, busDefaults?.Pan, overrides?.Pan, 0f),
                ResolveBool(engineDefaults?.StereoWidening, busDefaults?.StereoWidening, overrides?.StereoWidening, false),
                ResolveVector(engineDefaults?.Position, busDefaults?.Position, overrides?.Position),
                ResolveVector(engineDefaults?.Velocity, busDefaults?.Velocity, overrides?.Velocity),
                ResolveFloatNullable(engineDefaults?.CurveDistanceScaler, busDefaults?.CurveDistanceScaler, overrides?.CurveDistanceScaler),
                ResolveFloatNullable(engineDefaults?.DopplerFactor, busDefaults?.DopplerFactor, overrides?.DopplerFactor),
                ResolveRoom(engineDefaults?.RoomAcoustics, busDefaults?.RoomAcoustics, overrides?.RoomAcoustics),
                ResolveDistance(engineDefaults?.DistanceModel, busDefaults?.DistanceModel, overrides?.DistanceModel),
                ResolveFloat(engineDefaults?.RefDistance, busDefaults?.RefDistance, overrides?.RefDistance, 1f),
                ResolveFloat(engineDefaults?.MaxDistance, busDefaults?.MaxDistance, overrides?.MaxDistance, 200f),
                ResolveFloat(engineDefaults?.RollOff, busDefaults?.RollOff, overrides?.RollOff, 1f));
        }

        private static bool ResolveBool(bool? engineValue, bool? busValue, bool? overrideValue, bool fallback)
        {
            return overrideValue ?? busValue ?? engineValue ?? fallback;
        }

        private static float ResolveFloat(float? engineValue, float? busValue, float? overrideValue, float fallback)
        {
            return overrideValue ?? busValue ?? engineValue ?? fallback;
        }

        private static float? ResolveFloatNullable(float? engineValue, float? busValue, float? overrideValue)
        {
            return overrideValue ?? busValue ?? engineValue;
        }

        private static Vector3? ResolveVector(Vector3? engineValue, Vector3? busValue, Vector3? overrideValue)
        {
            return overrideValue ?? busValue ?? engineValue;
        }

        private static RoomAcoustics? ResolveRoom(RoomAcoustics? engineValue, RoomAcoustics? busValue, RoomAcoustics? overrideValue)
        {
            return overrideValue ?? busValue ?? engineValue;
        }

        private static DistanceModel? ResolveDistance(DistanceModel? engineValue, DistanceModel? busValue, DistanceModel? overrideValue)
        {
            return overrideValue ?? busValue ?? engineValue;
        }
    }
}
