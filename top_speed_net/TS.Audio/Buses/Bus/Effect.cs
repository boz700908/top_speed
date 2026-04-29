using System;
using SoundFlow.Abstracts;

namespace TS.Audio
{
    public sealed class BusEffect : IDisposable
    {
        private AudioBus? _bus;
        private bool _enabled = true;
        private bool _disposed;

        internal BusEffect(AudioBus bus, SoundModifier modifier, string? name)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Modifier = modifier ?? throw new ArgumentNullException(nameof(modifier));
            Name = string.IsNullOrWhiteSpace(name) ? "effect" : name!;
            Modifier.Name = Name;
        }

        internal SoundModifier Modifier { get; }
        public string Name { get; }
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_disposed)
                    return;

                if (_enabled == value)
                    return;

                _enabled = value;
                _bus?.UpdateEffectState(this);
            }
        }

        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _bus?.RemoveEffect(this);
            _bus = null;
            _disposed = true;
        }

        internal void MarkDetached()
        {
            _bus = null;
            _disposed = true;
        }

        internal void ApplyBusState(bool effectsEnabled)
        {
            Modifier.Enabled = effectsEnabled && _enabled;
        }
    }
}
