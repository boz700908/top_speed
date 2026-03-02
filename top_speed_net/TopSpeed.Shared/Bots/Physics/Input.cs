using TopSpeed.Data;

namespace TopSpeed.Bots
{
    public readonly struct BotPhysicsInput
    {
        public BotPhysicsInput(float elapsedSeconds, TrackSurface surface, int throttle, int brake, int steering)
        {
            ElapsedSeconds = elapsedSeconds;
            Surface = surface;
            Throttle = throttle;
            Brake = brake;
            Steering = steering;
        }

        public float ElapsedSeconds { get; }
        public TrackSurface Surface { get; }
        public int Throttle { get; }
        public int Brake { get; }
        public int Steering { get; }
    }
}
