namespace TopSpeed.Speech
{
    internal readonly struct SpeechVoiceInfo
    {
        public SpeechVoiceInfo(int index, string name, string language)
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
