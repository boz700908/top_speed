using System;

namespace TopSpeed.Speech.Prism
{
    internal interface IMethods
    {
        Config ConfigInit();
        IntPtr Init(ref Config config);
        void Shutdown(IntPtr context);
        int RegistryCount(IntPtr context);
        ulong RegistryIdAt(IntPtr context, int index);
        string? RegistryName(IntPtr context, ulong id);
        int RegistryPriority(IntPtr context, ulong id);
        bool RegistryExists(IntPtr context, ulong id);
        ulong RegistryId(IntPtr context, string name);
        IntPtr Create(IntPtr context, ulong id);
        IntPtr CreateBest(IntPtr context);
        IntPtr Acquire(IntPtr context, ulong id);
        IntPtr AcquireBest(IntPtr context);
        void FreeBackend(IntPtr backend);
        string? BackendName(IntPtr backend);
        Features BackendFeatures(IntPtr backend);
        Error InitializeBackend(IntPtr backend);
        Error Speak(IntPtr backend, string text, bool interrupt);
        Error Braille(IntPtr backend, string text);
        Error Output(IntPtr backend, string text, bool interrupt);
        Error Stop(IntPtr backend);
        Error Pause(IntPtr backend);
        Error Resume(IntPtr backend);
        Error IsSpeaking(IntPtr backend, out bool speaking);
        Error SetVolume(IntPtr backend, float volume);
        Error GetVolume(IntPtr backend, out float volume);
        Error SetRate(IntPtr backend, float rate);
        Error GetRate(IntPtr backend, out float rate);
        Error SetPitch(IntPtr backend, float pitch);
        Error GetPitch(IntPtr backend, out float pitch);
        Error RefreshVoices(IntPtr backend);
        Error CountVoices(IntPtr backend, out int count);
        Error GetVoiceName(IntPtr backend, int voiceIndex, out string? name);
        Error GetVoiceLanguage(IntPtr backend, int voiceIndex, out string? language);
        Error SetVoice(IntPtr backend, int voiceIndex);
        Error GetVoice(IntPtr backend, out int voiceIndex);
        Error GetChannels(IntPtr backend, out int channels);
        Error GetSampleRate(IntPtr backend, out int sampleRate);
        Error GetBitDepth(IntPtr backend, out int bitDepth);
        string? ErrorString(Error error);
    }
}
