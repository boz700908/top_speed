using System;
using System.Collections.Generic;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;
using TS.Sdl;
using TS.Sdl.Input;
using SdlRuntime = TS.Sdl.Runtime;

namespace TopSpeed.Input.Backends.Sdl
{
    internal sealed class Controller : IControllerBackend
    {
        private static readonly InitFlags RequiredFlags = InitFlags.Events | InitFlags.Joystick | InitFlags.Gamepad | InitFlags.Haptic;

        private readonly object _sync = new object();
        private Device? _device;
        private List<Choice>? _pendingChoices;
        private Guid _selectedInstanceGuid;
        private bool _noControllerRaised;
        private bool _enabled;
        private bool _disposed;

        public event Action? NoControllerDetected;

        public Controller()
        {
            if (!SdlRuntime.InitSubSystem(RequiredFlags) && (SdlRuntime.WasInit(RequiredFlags) & RequiredFlags) != RequiredFlags)
                throw new InvalidOperationException($"Unable to initialize SDL controller input: {SdlRuntime.GetError()}");
        }

        public bool ActiveControllerIsRacingWheel => _enabled && _device != null && _device.IsRacingWheel;
        public bool IgnoreAxesForMenuNavigation => ActiveControllerIsRacingWheel;
        public IVibrationDevice? VibrationDevice => _enabled ? _device : null;
        public bool TryGetDisplayProfile(out ControllerDisplayProfile profile)
        {
            var device = _device;
            if (!_enabled || device == null || !device.IsAvailable)
            {
                profile = default;
                return false;
            }

            profile = device.DisplayProfile;
            return true;
        }

        public void SetEnabled(bool enabled)
        {
            if (_disposed || _enabled == enabled)
                return;

            _enabled = enabled;
            if (!_enabled)
            {
                ClearDevice();
                lock (_sync)
                {
                    _pendingChoices = null;
                    _selectedInstanceGuid = Guid.Empty;
                    _noControllerRaised = false;
                }

                return;
            }

            lock (_sync)
            {
                _noControllerRaised = false;
            }

            RefreshFromSnapshot(notifyIfEmpty: true);
        }

        public void Update()
        {
            if (_disposed || !_enabled)
                return;

            PumpEvents();

            var device = _device;
            if (device != null && !device.Update())
                ClearDevice();
        }

        public bool TryGetState(out State state)
        {
            var device = _device;
            if (!_enabled || device == null || !device.IsAvailable)
            {
                state = default;
                return false;
            }

            state = device.State;
            return true;
        }

        public bool TryPollState(out State state)
        {
            if (_disposed || !_enabled)
            {
                state = default;
                return false;
            }

            PumpEvents();
            var device = _device;
            if (device == null || !device.Update())
            {
                if (device != null)
                    ClearDevice();

                state = default;
                return false;
            }

            state = device.State;
            return true;
        }

        public bool IsAnyButtonHeld()
        {
            return TryPollState(out var state) && state.HasAnyButtonDown();
        }

        public bool TryGetPendingChoices(out IReadOnlyList<Choice> choices)
        {
            lock (_sync)
            {
                if (_pendingChoices == null || _pendingChoices.Count == 0)
                {
                    choices = Array.Empty<Choice>();
                    return false;
                }

                choices = _pendingChoices.ToArray();
                return true;
            }
        }

        public bool TrySelect(Guid instanceGuid)
        {
            if (instanceGuid == Guid.Empty || _disposed)
                return false;

            var discovered = ChoiceMap.Discover();
            for (var i = 0; i < discovered.Count; i++)
            {
                var candidate = discovered[i];
                if (candidate.Choice.InstanceGuid != instanceGuid)
                    continue;

                if (!TryAttach(candidate))
                    return false;

                lock (_sync)
                {
                    _selectedInstanceGuid = instanceGuid;
                    _pendingChoices = null;
                    _noControllerRaised = false;
                }

                return true;
            }

            return false;
        }

        public void Suspend()
        {
        }

        public void Resume()
        {
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ClearDevice();
            lock (_sync)
            {
                _pendingChoices = null;
            }

            SdlRuntime.QuitSubSystem(RequiredFlags);
        }

        private void PumpEvents()
        {
            SdlRuntime.PumpEvents();
            while (ControllerEvents.TryPoll(out var controllerEvent))
                HandleEvent(controllerEvent);
        }

        private void HandleEvent(ControllerEvent controllerEvent)
        {
            var device = _device;
            if (controllerEvent.Kind == ControllerEventKind.BatteryUpdated)
            {
                if (device != null && controllerEvent.InstanceId == device.InstanceId)
                    device.SetPowerInfo(controllerEvent.Power);
                return;
            }

            switch (controllerEvent.Kind)
            {
                case ControllerEventKind.Added:
                case ControllerEventKind.Remapped:
                    if (device == null || HasPendingChoices())
                        RefreshFromSnapshot(notifyIfEmpty: false);
                    break;

                case ControllerEventKind.Removed:
                    if (device != null && controllerEvent.InstanceId == device.InstanceId)
                        ClearDevice();

                    RefreshFromSnapshot(notifyIfEmpty: false);
                    break;
            }
        }

        private void RefreshFromSnapshot(bool notifyIfEmpty)
        {
            if (_disposed || !_enabled)
                return;

            var discovered = ChoiceMap.Discover();
            if (discovered.Count == 0)
            {
                lock (_sync)
                {
                    _pendingChoices = null;
                    _selectedInstanceGuid = Guid.Empty;
                }

                if (notifyIfEmpty && !_noControllerRaised)
                {
                    _noControllerRaised = true;
                    NoControllerDetected?.Invoke();
                }

                return;
            }

            _noControllerRaised = false;

            if (KeepCurrentDevice(discovered))
            {
                ClearPendingChoices();
                return;
            }

            if (TryAttachSelectedDevice(discovered))
            {
                ClearPendingChoices();
                return;
            }

            if (discovered.Count == 1 && TryAttach(discovered[0]))
            {
                ClearPendingChoices();
                return;
            }

            ClearDevice();
            SetPendingChoices(discovered);
        }

        private bool KeepCurrentDevice(List<DiscoveredDevice> discovered)
        {
            var device = _device;
            if (device == null)
                return false;

            for (var i = 0; i < discovered.Count; i++)
            {
                if (discovered[i].InstanceId == device.InstanceId)
                    return true;
            }

            ClearDevice();
            return false;
        }

        private bool TryAttachSelectedDevice(List<DiscoveredDevice> discovered)
        {
            var selectedInstanceGuid = _selectedInstanceGuid;
            if (selectedInstanceGuid == Guid.Empty)
                return false;

            for (var i = 0; i < discovered.Count; i++)
            {
                if (discovered[i].Choice.InstanceGuid != selectedInstanceGuid)
                    continue;

                return TryAttach(discovered[i]);
            }

            _selectedInstanceGuid = Guid.Empty;
            return false;
        }

        private bool TryAttach(DiscoveredDevice discovered)
        {
            Device? next;
            try
            {
                next = Device.Open(discovered);
            }
            catch
            {
                return false;
            }

            if (next == null || !next.Update())
            {
                next?.Dispose();
                return false;
            }

            var previous = _device;
            _device = next;
            previous?.Dispose();
            return true;
        }

        private bool HasPendingChoices()
        {
            lock (_sync)
            {
                return _pendingChoices != null && _pendingChoices.Count > 0;
            }
        }

        private void SetPendingChoices(List<DiscoveredDevice> discovered)
        {
            var choices = new List<Choice>(discovered.Count);
            for (var i = 0; i < discovered.Count; i++)
                choices.Add(discovered[i].Choice);

            lock (_sync)
            {
                _pendingChoices = choices;
            }
        }

        private void ClearPendingChoices()
        {
            lock (_sync)
            {
                _pendingChoices = null;
            }
        }

        private void ClearDevice()
        {
            var device = _device;
            _device = null;
            device?.Dispose();
        }
    }
}
