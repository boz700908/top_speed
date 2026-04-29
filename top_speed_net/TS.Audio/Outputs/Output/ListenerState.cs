using System.Numerics;

namespace TS.Audio
{
    internal sealed class ListenerStateSnapshot
    {
        public static readonly ListenerStateSnapshot Default = new ListenerStateSnapshot(
            Vector3.Zero,
            Vector3.Zero,
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 1f, 0f));

        public ListenerStateSnapshot(Vector3 position, Vector3 velocity, Vector3 forward, Vector3 up)
        {
            Position = position;
            Velocity = velocity;
            Forward = forward;
            Up = up;
        }

        public Vector3 Position { get; }
        public Vector3 Velocity { get; }
        public Vector3 Forward { get; }
        public Vector3 Up { get; }
    }
}
