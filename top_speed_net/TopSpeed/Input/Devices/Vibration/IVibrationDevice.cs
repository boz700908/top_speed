using System;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input.Devices.Vibration
{
    internal interface IVibrationDevice : IDisposable
    {
        bool IsAvailable { get; }
        State State { get; }
        bool Update();
        
        bool ForceFeedbackCapable { get; }
        void PlayEffect(VibrationEffectType type, int intensity = 10000);
        void StopEffect(VibrationEffectType type);
        void Gain(VibrationEffectType type, int value);
    }
}

