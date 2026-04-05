using System;
using System.Collections.Generic;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Input
{
    internal interface IControllerBackend : IDisposable
    {
        event Action? NoControllerDetected;

        bool ActiveControllerIsRacingWheel { get; }
        bool IgnoreAxesForMenuNavigation { get; }
        IVibrationDevice? VibrationDevice { get; }
        bool TryGetDisplayProfile(out ControllerDisplayProfile profile);

        void SetEnabled(bool enabled);
        void Update();
        bool TryGetState(out State state);
        bool TryPollState(out State state);
        bool IsAnyButtonHeld();
        bool TryGetPendingChoices(out IReadOnlyList<Choice> choices);
        bool TrySelect(Guid instanceGuid);
        void Suspend();
        void Resume();
    }
}

