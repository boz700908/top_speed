namespace TopSpeed.Speech.Prism
{
    internal enum Error
    {
        Ok = 0,
        NotInitialized,
        InvalidParam,
        NotImplemented,
        NoVoices,
        VoiceNotFound,
        SpeakFailure,
        MemoryFailure,
        RangeOutOfBounds,
        Internal,
        NotSpeaking,
        NotPaused,
        AlreadyPaused,
        InvalidUtf8,
        InvalidOperation,
        AlreadyInitialized,
        BackendNotAvailable,
        Unknown,
        InvalidAudioFormat,
        InternalBackendLimitExceeded,
        Count
    }
}
