using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NETFRAMEWORK
using System.Speech.Synthesis;
#endif
using System.Threading;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Speech.ScreenReaders;

namespace TopSpeed.Speech
{
    internal sealed partial class SpeechService : IGameSpeech
    {
        public enum SpeakFlag
        {
            None,
            NoInterrupt,
            NoInterruptButStop,
            Interruptable,
            InterruptableButStop
        }

        private readonly Stopwatch _watch = new Stopwatch();
        private readonly IScreenReader _screenReader;
#if NETFRAMEWORK
        private SpeechSynthesizer? _sapi;
#endif
        private long _timeRequiredMs;
        private string _lastSpoken = string.Empty;
        private Func<bool>? _isInputHeld;
        private Action? _prepareForInterruptableSpeech;
        private bool _screenReaderReady;
        private float _speechRate = 0.5f;

        public SpeechService(Func<bool>? isInputHeld = null, Action? prepareForInterruptableSpeech = null)
        {
            _isInputHeld = isInputHeld;
            _prepareForInterruptableSpeech = prepareForInterruptableSpeech;
            _screenReader = Factory.Create();
            _screenReaderReady = InitializeScreenReader();
        }

        public bool IsAvailable => _screenReaderReady || IsSapiInitialized();

        public float ScreenReaderRateMs { get; set; }
        public SpeechOutputMode OutputMode { get; set; } = SpeechOutputMode.Speech;
        public bool ScreenReaderInterrupt { get; set; }
        public IReadOnlyList<SpeechBackendInfo> AvailableBackends => _screenReader.AvailableBackends;
        public IReadOnlyList<SpeechVoiceInfo> AvailableVoices => _screenReader.AvailableVoices;
        public ulong? ActiveBackendId => _screenReader.ActiveBackendId;
        public SpeechCapabilities ScreenReaderCapabilities => _screenReader.Capabilities;
        public string? ScreenReaderBackendName => _screenReader.ActiveBackendName;

        public float SpeechRate
        {
            get => _speechRate;
            set
            {
                var clamped = Math.Max(0f, Math.Min(1f, value));
                _speechRate = clamped;
                ApplySpeechRate();
            }
        }

        public ulong? PreferredBackendId
        {
            get => _screenReader.PreferredBackendId;
            set
            {
                if (_screenReader.PreferredBackendId == value)
                    return;

                _screenReader.PreferredBackendId = value;
                _screenReaderReady = InitializeScreenReader();
            }
        }

        public int? PreferredVoiceIndex
        {
            get => _screenReader.PreferredVoiceIndex;
            set => _screenReader.PreferredVoiceIndex = value;
        }

        public void BindInputProbe(Func<bool> isInputHeld)
        {
            _isInputHeld = isInputHeld;
        }

        public void BindInterruptPreparation(Action prepareForInterruptableSpeech)
        {
            _prepareForInterruptableSpeech = prepareForInterruptableSpeech;
        }

        public void Speak(string text)
        {
            SpeakInternal(text, SpeakFlag.None, allowConfiguredInterrupt: true);
        }

        public void Speak(string text, SpeakFlag flag)
        {
            SpeakInternal(text, flag, allowConfiguredInterrupt: false);
        }

        private void SpeakInternal(string text, SpeakFlag flag, bool allowConfiguredInterrupt)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var shouldInterruptCurrent = flag == SpeakFlag.NoInterruptButStop || flag == SpeakFlag.InterruptableButStop;
            var interruptSpeech = shouldInterruptCurrent || (allowConfiguredInterrupt && ScreenReaderInterrupt);
            if (interruptSpeech)
                Purge();

            text = text.Trim();
            text = LocalizationService.Translate(text);
            _lastSpoken = text;

            var spoke = false;
            if (_screenReaderReady)
            {
                spoke = TrySpeakWithScreenReader(text, interruptSpeech);
                if (spoke)
                    StartSpeakTimer(text);
            }

            if (!spoke)
            {
#if NETFRAMEWORK
                EnsureSapi();
                _sapi!.SpeakAsync(text);
                while (!IsSpeaking())
                {
                    Thread.Sleep(0);
                }
#endif
            }

            if (flag == SpeakFlag.None)
                return;

            var interruptable = flag == SpeakFlag.Interruptable || flag == SpeakFlag.InterruptableButStop;
            if (interruptable)
                PrepareForInterruptableSpeech();

            while (IsSpeaking())
            {
                if (interruptable)
                {
                    if (IsInputHeld())
                        break;
                }

                Thread.Sleep(10);
            }
        }

        public bool IsSpeaking()
        {
            if (_watch.IsRunning)
                return _watch.ElapsedMilliseconds < _timeRequiredMs;

            if (_screenReaderReady)
            {
                try
                {
                    if (_screenReader.IsSpeaking())
                        return true;
                }
                catch
                {
                }
            }

#if NETFRAMEWORK
            return _sapi != null && _sapi.State == SynthesizerState.Speaking;
#else
            return false;
#endif
        }

        public void Purge()
        {
            _watch.Reset();
            _timeRequiredMs = 0;
#if NETFRAMEWORK
            if (_sapi != null)
            {
                try
                {
                    _sapi.SpeakAsyncCancelAll();
                }
                catch (OperationCanceledException)
                {
                }

                while (IsSpeaking())
                {
                    Thread.Sleep(0);
                }
            }
#endif

            if (_screenReaderReady)
            {
                try
                {
                    _screenReader.Silence();
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
            Purge();
#if NETFRAMEWORK
            _sapi?.Dispose();
#endif

            try
            {
                _screenReader.Close();
            }
            catch
            {
            }
        }

        private bool InitializeScreenReader()
        {
            try
            {
                try
                {
                    _screenReader.Close();
                }
                catch
                {
                }

                _screenReader.TrySAPI(false);
                _screenReader.PreferSAPI(false);
                var initialized = _screenReader.Initialize();
                if (initialized)
                    ApplySpeechRate();
                return initialized;
            }
            catch
            {
                return false;
            }
        }

        private bool TrySpeakWithScreenReader(string text, bool interrupt)
        {
            try
            {
                switch (OutputMode)
                {
                    case SpeechOutputMode.Braille:
                        if (_screenReader.Braille(text))
                            return true;

                        if (_screenReader.Output(text, interrupt))
                            return true;

                        return _screenReader.Speak(text, interrupt);

                    case SpeechOutputMode.SpeechAndBraille:
                        if (_screenReader.Output(text, interrupt))
                            return true;

                        if (_screenReader.Speak(text, interrupt))
                        {
                            _screenReader.Braille(text);
                            return true;
                        }

                        return _screenReader.Braille(text);

                    default:
                        if (_screenReader.Speak(text, interrupt))
                            return true;

                        return _screenReader.Output(text, interrupt);
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsSapiInitialized()
        {
#if NETFRAMEWORK
            return _sapi != null;
#else
            return false;
#endif
        }

#if NETFRAMEWORK
        private void EnsureSapi()
        {
            if (_sapi == null)
                _sapi = new SpeechSynthesizer();
        }
#endif

        private void StartSpeakTimer(string text)
        {
            if (ScreenReaderRateMs <= 0f)
            {
                _watch.Reset();
                _timeRequiredMs = 0;
                return;
            }

            var words = CountWords(text);
            _timeRequiredMs = (long)(words * ScreenReaderRateMs);
            _watch.Reset();
            _watch.Start();
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private bool IsInputHeld()
        {
            try
            {
                return _isInputHeld != null && _isInputHeld();
            }
            catch
            {
                return false;
            }
        }

        private void PrepareForInterruptableSpeech()
        {
            try
            {
                _prepareForInterruptableSpeech?.Invoke();
            }
            catch
            {
            }
        }

        private void ApplySpeechRate()
        {
            if (!_screenReaderReady)
                return;

            try
            {
                _screenReader.SetRate(_speechRate);
            }
            catch
            {
            }
        }
    }
}
