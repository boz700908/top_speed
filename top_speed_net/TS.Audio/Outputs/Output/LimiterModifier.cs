using System;
using System.Threading;
using SoundFlow.Abstracts;

namespace TS.Audio
{
    internal sealed class LimiterModifier : SoundModifier
    {
        private readonly Func<float> _masterVolume;
        private readonly Action<float, float, float> _report;
        private float _limiterGain = 1f;

        public LimiterModifier(Func<float> masterVolume, Action<float, float, float> report)
        {
            _masterVolume = masterVolume ?? throw new ArgumentNullException(nameof(masterVolume));
            _report = report ?? throw new ArgumentNullException(nameof(report));
            Name = "output-limiter";
        }

        public override float ProcessSample(float sample, int channel)
        {
            return sample;
        }

        public override void Process(Span<float> buffer, int channels)
        {
            if (!Enabled || buffer.IsEmpty)
                return;

            var master = _masterVolume();
            if (master < 0f)
                master = 0f;
            else if (master > 1f)
                master = 1f;

            var preLimiterPeak = 0f;
            for (var i = 0; i < buffer.Length; i++)
            {
                var abs = Math.Abs(buffer[i] * master);
                if (abs > preLimiterPeak)
                    preLimiterPeak = abs;
            }

            var targetLimiterGain = preLimiterPeak > 1f ? 1f / preLimiterPeak : 1f;
            var limiterGain = Volatile.Read(ref _limiterGain);
            if (targetLimiterGain < limiterGain)
            {
                limiterGain = targetLimiterGain;
            }
            else
            {
                limiterGain += (targetLimiterGain - limiterGain) * 0.05f;
                if (limiterGain > 1f)
                    limiterGain = 1f;
            }

            Volatile.Write(ref _limiterGain, limiterGain);

            var postLimiterPeak = 0f;
            var gain = master * limiterGain;
            for (var i = 0; i < buffer.Length; i++)
            {
                var value = buffer[i] * gain;
                if (value > 1f)
                    value = 1f;
                else if (value < -1f)
                    value = -1f;

                buffer[i] = value;

                var abs = Math.Abs(value);
                if (abs > postLimiterPeak)
                    postLimiterPeak = abs;
            }

            _report(preLimiterPeak, postLimiterPeak, limiterGain);
        }
    }
}
