using System;

namespace TS.Sdl.Input
{
    public sealed class HapticEffectHandle : IDisposable
    {
        private HapticDevice? _device;

        internal HapticEffectHandle(HapticDevice device, int id)
        {
            _device = device;
            Id = id;
        }

        internal int Id { get; private set; }
        public bool IsValid => _device != null && Id >= 0;

        internal bool BelongsTo(HapticDevice device)
        {
            return ReferenceEquals(_device, device);
        }

        internal void Invalidate()
        {
            _device = null;
            Id = -1;
        }

        public void Dispose()
        {
            var device = _device;
            if (device == null)
                return;

            device.DestroyEffect(this);
        }
    }
}
