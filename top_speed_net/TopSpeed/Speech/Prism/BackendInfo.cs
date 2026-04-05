namespace TopSpeed.Speech.Prism
{
    internal readonly struct BackendInfo
    {
        public BackendInfo(ulong id, string name, int priority, bool isSupported)
        {
            Id = id;
            Name = name;
            Priority = priority;
            IsSupported = isSupported;
        }

        public ulong Id { get; }
        public string Name { get; }
        public int Priority { get; }
        public bool IsSupported { get; }
    }
}
