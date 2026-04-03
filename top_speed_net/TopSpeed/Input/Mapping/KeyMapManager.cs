using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed class KeyMapManager
    {
        private readonly RaceInput _input;

        public KeyMapManager(RaceInput input)
        {
            _input = input;
        }

        public IReadOnlyList<InputActionDefinition> Actions => _input.GetActionDefinitions();

        public string GetLabel(InputAction action)
        {
            return _input.GetActionLabel(action);
        }

        public Key GetKey(InputAction action)
        {
            return _input.GetKeyMapping(action);
        }

        public AxisOrButton GetAxis(InputAction action)
        {
            return _input.GetAxisMapping(action);
        }

        public void ApplyKeyMapping(InputAction action, Key key)
        {
            _input.ApplyKeyMapping(action, key);
        }

        public void ApplyAxisMapping(InputAction action, AxisOrButton axis)
        {
            _input.ApplyAxisMapping(action, axis);
        }

        public bool IsKeyInUse(Key key, InputAction ignore)
        {
            foreach (var action in Actions)
            {
                if (action.Action == ignore)
                    continue;
                if (GetKey(action.Action) == key)
                    return true;
            }
            return false;
        }

        public bool IsAxisInUse(AxisOrButton axis, InputAction ignore)
        {
            foreach (var action in Actions)
            {
                if (action.Action == ignore)
                    continue;
                if (GetAxis(action.Action) == axis)
                    return true;
            }
            return false;
        }

        public static bool IsReservedKey(Key key)
        {
            if (key >= Key.F1 && key <= Key.F8)
                return true;
            if (key == Key.F11)
                return true;
            if (key >= Key.D1 && key <= Key.D8)
                return true;
            return key == Key.LeftAlt;
        }

        public static string FormatKey(Key key)
        {
            return InputDisplayText.Key(key);
        }

        public static string FormatAxis(AxisOrButton axis)
        {
            return InputDisplayText.Axis(axis);
        }

        public string GetMappingInstruction(bool keyboard, InputAction action)
        {
            var label = GetLabel(action).ToLowerInvariant();
            return keyboard
                ? LocalizationService.Format(LocalizationService.Mark("Press the new key for {0}."), label)
                : LocalizationService.Format(LocalizationService.Mark("Move or press the controller control for {0}."), label);
        }
    }
}


