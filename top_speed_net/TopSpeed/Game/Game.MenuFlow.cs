using System;
using System.Diagnostics;
using TopSpeed.Menu;
using TopSpeed.Speech;
using TopSpeed.Windowing;
using TopSpeed.Core;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void HandleMenuAction(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.Exit:
                    ExitRequested?.Invoke();
                    break;
                case MenuAction.QuickStart:
                    PrepareQuickStart();
                    QueueRaceStart(RaceMode.QuickStart);
                    break;
                default:
                    break;
            }
        }

        private bool UpdateModalOperations()
        {
            return _multiplayerCoordinator.UpdatePendingOperations();
        }

        private void BeginPromptTextInput(
            string prompt,
            string? initialValue,
            SpeechService.SpeakFlag speakFlag,
            bool speakBeforeInput,
            Action<TextInputResult> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            if (_textInputPromptActive)
            {
                onCompleted(TextInputResult.CreateCancelled());
                return;
            }

            if (speakBeforeInput)
                _speech.Speak(prompt, speakFlag);

            _textInputPromptActive = true;
            _textInputPromptCallback = onCompleted;
            _input.Suspend();
            _window.ShowTextInput(initialValue);
        }

        private void UpdateTextInputPrompt()
        {
            if (!_textInputPromptActive)
                return;

            if (!_window.TryConsumeTextInput(out var result))
                return;

            var callback = _textInputPromptCallback;
            _textInputPromptCallback = null;
            _textInputPromptActive = false;
            _input.Resume();
            callback?.Invoke(result);
        }

        private void StartCalibrationSequence(string? returnMenuId = null)
        {
            _calibrationReturnMenuId = returnMenuId;
            _calibrationStopwatch = null;
            EnsureCalibrationMenus();
            _calibrationOverlay = !string.IsNullOrWhiteSpace(returnMenuId) && _menu.HasActiveMenu;
            if (_calibrationOverlay)
                _menu.Push(CalibrationIntroMenuId);
            else
                _menu.ShowRoot(CalibrationIntroMenuId);
            _state = AppState.Calibration;
        }

        private void EnsureCalibrationMenus()
        {
            if (_calibrationMenusRegistered)
                return;

            var introItems = new[]
            {
                new MenuItem("Ok", MenuAction.None, onActivate: BeginCalibrationSample)
            };
            var sampleItems = new[]
            {
                new MenuItem("Ok", MenuAction.None, onActivate: CompleteCalibration)
            };

            _menu.Register(_menu.CreateMenu(CalibrationIntroMenuId, introItems, CalibrationInstructions));
            _menu.Register(_menu.CreateMenu(CalibrationSampleMenuId, sampleItems, CalibrationSampleText));
            _calibrationMenusRegistered = true;
        }

        private void BeginCalibrationSample()
        {
            _calibrationStopwatch = Stopwatch.StartNew();
            if (_calibrationOverlay)
                _menu.ReplaceTop(CalibrationSampleMenuId);
            else
                _menu.ShowRoot(CalibrationSampleMenuId);
        }

        private void CompleteCalibration()
        {
            if (_calibrationStopwatch == null)
                return;

            var elapsedMs = _calibrationStopwatch.ElapsedMilliseconds;
            var words = CalibrationSampleText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var rate = words > 0 ? (float)elapsedMs / words : 0f;
            _settings.ScreenReaderRateMs = rate;
            _speech.ScreenReaderRateMs = rate;
            SaveSettings();

            _needsCalibration = false;
            var returnMenu = _calibrationReturnMenuId ?? "main";
            _calibrationReturnMenuId = null;
            if (_calibrationOverlay && _menu.CanPop)
                _menu.PopToPrevious();
            else
                _menu.ShowRoot(returnMenu);

            _calibrationOverlay = false;
            _menu.FadeInMenuMusic(force: true);
            _state = AppState.Menu;
        }

        private static bool IsCalibrationMenu(string? id)
        {
            return id == CalibrationIntroMenuId || id == CalibrationSampleMenuId;
        }

        private void EnterMenuState()
        {
            _state = AppState.Menu;
        }
    }
}
