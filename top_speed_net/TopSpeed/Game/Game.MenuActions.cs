using System;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Core;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        void IMenuActions.SaveMusicVolume(float volume) => SaveMusicVolume(volume);
        void IMenuActions.QueueRaceStart(RaceMode mode) => QueueRaceStart(mode);
        void IMenuActions.StartServerDiscovery() => _multiplayerCoordinator.StartServerDiscovery();
        void IMenuActions.OpenSavedServersManager() => _multiplayerCoordinator.OpenSavedServersManager();
        void IMenuActions.BeginManualServerEntry() => _multiplayerCoordinator.BeginManualServerEntry();
        void IMenuActions.SpeakMessage(string text) => _speech.Speak(text);
        void IMenuActions.SpeakNotImplemented() => _speech.Speak("Not implemented yet.");
        void IMenuActions.BeginServerPortEntry() => _multiplayerCoordinator.BeginServerPortEntry();
        void IMenuActions.RestoreDefaults() => RestoreDefaults();
        void IMenuActions.RecalibrateScreenReaderRate() => StartCalibrationSequence("options_game");
        void IMenuActions.SetDevice(InputDeviceMode mode) => SetDevice(mode);
        void IMenuActions.ToggleCurveAnnouncements() => ToggleCurveAnnouncements();
        void IMenuActions.ToggleSetting(Action update) => ToggleSetting(update);
        void IMenuActions.UpdateSetting(Action update) => UpdateSetting(update);
        void IMenuActions.ApplyAudioSettings() => ApplyAudioSettings();
        void IMenuActions.BeginMapping(InputMappingMode mode, InputAction action) => _inputMapping.BeginMapping(mode, action);
        string IMenuActions.FormatMappingValue(InputAction action, InputMappingMode mode) => _inputMapping.FormatMappingValue(action, mode);
    }
}
