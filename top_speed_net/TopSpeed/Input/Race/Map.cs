using System.Collections.Generic;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void SetLeft(JoystickAxisOrButton a)
        {
            _left = a;
            _settings.JoystickLeft = a;
        }

        public void SetLeft(Key key)
        {
            _kbLeft = key;
            _settings.KeyLeft = key;
        }

        public void SetRight(JoystickAxisOrButton a)
        {
            _right = a;
            _settings.JoystickRight = a;
        }

        public void SetRight(Key key)
        {
            _kbRight = key;
            _settings.KeyRight = key;
        }

        public void SetThrottle(JoystickAxisOrButton a)
        {
            _throttle = a;
            _settings.JoystickThrottle = a;
        }

        public void SetThrottle(Key key)
        {
            _kbThrottle = key;
            _settings.KeyThrottle = key;
        }

        public void SetBrake(JoystickAxisOrButton a)
        {
            _brake = a;
            _settings.JoystickBrake = a;
        }

        public void SetBrake(Key key)
        {
            _kbBrake = key;
            _settings.KeyBrake = key;
        }

        public void SetGearUp(JoystickAxisOrButton a)
        {
            _gearUp = a;
            _settings.JoystickGearUp = a;
        }

        public void SetGearUp(Key key)
        {
            _kbGearUp = key;
            _settings.KeyGearUp = key;
        }

        public void SetGearDown(JoystickAxisOrButton a)
        {
            _gearDown = a;
            _settings.JoystickGearDown = a;
        }

        public void SetGearDown(Key key)
        {
            _kbGearDown = key;
            _settings.KeyGearDown = key;
        }

        public void SetHorn(JoystickAxisOrButton a)
        {
            _horn = a;
            _settings.JoystickHorn = a;
        }

        public void SetHorn(Key key)
        {
            _kbHorn = key;
            _settings.KeyHorn = key;
        }

        public void SetRequestInfo(JoystickAxisOrButton a)
        {
            _requestInfo = a;
            _settings.JoystickRequestInfo = a;
        }

        public void SetRequestInfo(Key key)
        {
            _kbRequestInfo = key;
            _settings.KeyRequestInfo = key;
        }

        public void SetCurrentGear(JoystickAxisOrButton a)
        {
            _currentGear = a;
            _settings.JoystickCurrentGear = a;
        }

        public void SetCurrentGear(Key key)
        {
            _kbCurrentGear = key;
            _settings.KeyCurrentGear = key;
        }

        public void SetCurrentLapNr(JoystickAxisOrButton a)
        {
            _currentLapNr = a;
            _settings.JoystickCurrentLapNr = a;
        }

        public void SetCurrentLapNr(Key key)
        {
            _kbCurrentLapNr = key;
            _settings.KeyCurrentLapNr = key;
        }

        public void SetCurrentRacePerc(JoystickAxisOrButton a)
        {
            _currentRacePerc = a;
            _settings.JoystickCurrentRacePerc = a;
        }

        public void SetCurrentRacePerc(Key key)
        {
            _kbCurrentRacePerc = key;
            _settings.KeyCurrentRacePerc = key;
        }

        public void SetCurrentLapPerc(JoystickAxisOrButton a)
        {
            _currentLapPerc = a;
            _settings.JoystickCurrentLapPerc = a;
        }

        public void SetCurrentLapPerc(Key key)
        {
            _kbCurrentLapPerc = key;
            _settings.KeyCurrentLapPerc = key;
        }

        public void SetCurrentRaceTime(JoystickAxisOrButton a)
        {
            _currentRaceTime = a;
            _settings.JoystickCurrentRaceTime = a;
        }

        public void SetCurrentRaceTime(Key key)
        {
            _kbCurrentRaceTime = key;
            _settings.KeyCurrentRaceTime = key;
        }

        public void SetStartEngine(JoystickAxisOrButton a)
        {
            _startEngine = a;
            _settings.JoystickStartEngine = a;
        }

        public void SetStartEngine(Key key)
        {
            _kbStartEngine = key;
            _settings.KeyStartEngine = key;
        }

        public void SetReportDistance(JoystickAxisOrButton a)
        {
            _reportDistance = a;
            _settings.JoystickReportDistance = a;
        }

        public void SetReportDistance(Key key)
        {
            _kbReportDistance = key;
            _settings.KeyReportDistance = key;
        }

        public void SetReportSpeed(JoystickAxisOrButton a)
        {
            _reportSpeed = a;
            _settings.JoystickReportSpeed = a;
        }

        public void SetReportSpeed(Key key)
        {
            _kbReportSpeed = key;
            _settings.KeyReportSpeed = key;
        }

        public void SetTrackName(JoystickAxisOrButton a)
        {
            _trackName = a;
            _settings.JoystickTrackName = a;
        }

        public void SetTrackName(Key key)
        {
            _kbTrackName = key;
            _settings.KeyTrackName = key;
        }

        public void SetPause(JoystickAxisOrButton a)
        {
            _pause = a;
            _settings.JoystickPause = a;
        }

        public void SetPause(Key key)
        {
            _kbPause = key;
            _settings.KeyPause = key;
        }

        internal IReadOnlyList<InputActionDefinition> GetActionDefinitions()
        {
            return _actionDefinitions;
        }

        internal string GetActionLabel(InputAction action)
        {
            return _actionBindings.TryGetValue(action, out var binding)
                ? binding.Label
                : "Action";
        }

        internal Key GetKeyMapping(InputAction action)
        {
            return _actionBindings.TryGetValue(action, out var binding)
                ? binding.GetKey()
                : Key.Unknown;
        }

        internal JoystickAxisOrButton GetAxisMapping(InputAction action)
        {
            return _actionBindings.TryGetValue(action, out var binding)
                ? binding.GetAxis()
                : JoystickAxisOrButton.AxisNone;
        }

        internal void ApplyKeyMapping(InputAction action, Key key)
        {
            if (_actionBindings.TryGetValue(action, out var binding))
                binding.SetKey(key);
        }

        internal void ApplyAxisMapping(InputAction action, JoystickAxisOrButton axis)
        {
            if (_actionBindings.TryGetValue(action, out var binding))
                binding.SetAxis(axis);
        }
    }
}
