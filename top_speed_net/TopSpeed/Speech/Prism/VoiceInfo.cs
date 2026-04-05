namespace TopSpeed.Speech.Prism
{
    internal readonly struct VoiceInfo
    {
        public VoiceInfo(int index, string name, string language)
        {
            Index = index;
            Name = name;
            Language = language;
        }

        public int Index { get; }
        public string Name { get; }
        public string Language { get; }
    }
}
