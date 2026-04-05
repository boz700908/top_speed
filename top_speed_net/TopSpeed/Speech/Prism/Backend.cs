using System;
using System.Collections.Generic;

namespace TopSpeed.Speech.Prism
{
    internal sealed class Backend : IDisposable
    {
        private IntPtr _handle;

        public Backend(IntPtr handle, ulong requestedId)
        {
            _handle = handle;
            RequestedId = requestedId;
        }

        public ulong RequestedId { get; }

        public string Name
        {
            get
            {
                ThrowIfClosed();
                return Native.BackendName(_handle) ?? string.Empty;
            }
        }

        public Features Features => _handle == IntPtr.Zero ? Features.None : Native.BackendFeatures(_handle);

        public float? Volume
        {
            get => Supports(Features.GetVolume) && Native.GetVolume(_handle, out var value) == Error.Ok ? value : null;
            set
            {
                if (value.HasValue && Supports(Features.SetVolume))
                    ThrowIfError(Native.SetVolume(_handle, value.Value));
            }
        }

        public float? Rate
        {
            get => Supports(Features.GetRate) && Native.GetRate(_handle, out var value) == Error.Ok ? value : null;
            set
            {
                if (value.HasValue && Supports(Features.SetRate))
                    ThrowIfError(Native.SetRate(_handle, value.Value));
            }
        }

        public float? Pitch
        {
            get => Supports(Features.GetPitch) && Native.GetPitch(_handle, out var value) == Error.Ok ? value : null;
            set
            {
                if (value.HasValue && Supports(Features.SetPitch))
                    ThrowIfError(Native.SetPitch(_handle, value.Value));
            }
        }

        public bool IsSpeaking => Supports(Features.IsSpeaking) && Native.IsSpeaking(_handle, out var speaking) == Error.Ok && speaking;

        public int? CurrentVoiceIndex
        {
            get => Supports(Features.GetVoice) && Native.GetVoice(_handle, out var value) == Error.Ok ? value : null;
            set
            {
                if (value.HasValue && Supports(Features.SetVoice))
                    ThrowIfError(Native.SetVoice(_handle, value.Value));
            }
        }

        public IReadOnlyList<VoiceInfo> Voices
        {
            get
            {
                if (_handle == IntPtr.Zero)
                    return Array.Empty<VoiceInfo>();

                if (Supports(Features.RefreshVoices))
                    ThrowIfError(Native.RefreshVoices(_handle));

                if (!Supports(Features.CountVoices) || Native.CountVoices(_handle, out var count) != Error.Ok)
                    return Array.Empty<VoiceInfo>();

                var voices = new List<VoiceInfo>(count);
                for (var i = 0; i < count; i++)
                {
                    var name = string.Empty;
                    var language = string.Empty;
                    if (Supports(Features.GetVoiceName))
                        Native.GetVoiceName(_handle, i, out name);
                    if (Supports(Features.GetVoiceLanguage))
                        Native.GetVoiceLanguage(_handle, i, out language);
                    voices.Add(new VoiceInfo(i, name ?? string.Empty, language ?? string.Empty));
                }

                return voices;
            }
        }

        public int? Channels => Supports(Features.GetChannels) && Native.GetChannels(_handle, out var value) == Error.Ok ? value : null;
        public int? SampleRate => Supports(Features.GetSampleRate) && Native.GetSampleRate(_handle, out var value) == Error.Ok ? value : null;
        public int? BitDepth => Supports(Features.GetBitDepth) && Native.GetBitDepth(_handle, out var value) == Error.Ok ? value : null;

        public void Initialize()
        {
            ThrowIfError(Native.InitializeBackend(_handle));
        }

        public void Speak(string text, bool interrupt)
        {
            ThrowIfClosed();
            ThrowIfError(Native.Speak(_handle, text, interrupt));
        }

        public void Braille(string text)
        {
            ThrowIfClosed();
            ThrowIfError(Native.Braille(_handle, text));
        }

        public void Output(string text, bool interrupt)
        {
            ThrowIfClosed();
            ThrowIfError(Native.Output(_handle, text, interrupt));
        }

        public void Stop()
        {
            ThrowIfClosed();
            ThrowIfError(Native.Stop(_handle));
        }

        public void Pause()
        {
            ThrowIfClosed();
            ThrowIfError(Native.Pause(_handle));
        }

        public void Resume()
        {
            ThrowIfClosed();
            ThrowIfError(Native.Resume(_handle));
        }

        public bool Supports(Features feature)
        {
            return (Features & feature) == feature;
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
                return;

            Native.FreeBackend(_handle);
            _handle = IntPtr.Zero;
        }

        private void ThrowIfClosed()
        {
            if (_handle == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(Backend));
        }

        private static void ThrowIfError(Error error)
        {
            if (error != Error.Ok)
                throw new PrismException(error);
        }
    }
}
