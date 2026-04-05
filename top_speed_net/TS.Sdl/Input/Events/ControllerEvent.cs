namespace TS.Sdl.Input
{
    public readonly struct ControllerEvent
    {
        public ControllerEvent(
            ControllerEventKind kind,
            ControllerEventSource source,
            uint instanceId,
            ulong timestamp,
            int index = -1,
            int value = 0,
            bool down = false,
            PowerInfo power = default)
        {
            Kind = kind;
            Source = source;
            InstanceId = instanceId;
            Timestamp = timestamp;
            Index = index;
            Value = value;
            Down = down;
            Power = power;
        }

        public ControllerEventKind Kind { get; }
        public ControllerEventSource Source { get; }
        public uint InstanceId { get; }
        public ulong Timestamp { get; }
        public int Index { get; }
        public int Value { get; }
        public bool Down { get; }
        public PowerInfo Power { get; }
    }
}
