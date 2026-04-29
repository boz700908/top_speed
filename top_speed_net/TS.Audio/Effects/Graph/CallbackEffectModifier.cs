using System;
using SoundFlow.Abstracts;

namespace TS.Audio
{
    internal sealed class CallbackEffectModifier : SoundModifier
    {
        private readonly AudioEffectProcessCallback _process;
        private float[] _scratch;

        public CallbackEffectModifier(AudioEffectProcessCallback process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _scratch = Array.Empty<float>();
        }

        public override float ProcessSample(float sample, int channel)
        {
            return sample;
        }

        public override void Process(Span<float> buffer, int channels)
        {
            if (!Enabled || buffer.IsEmpty)
                return;

            if (_scratch.Length < buffer.Length)
                _scratch = new float[buffer.Length];

            buffer.CopyTo(_scratch);
            _process(_scratch, channels);
            _scratch.AsSpan(0, buffer.Length).CopyTo(buffer);
        }
    }
}
