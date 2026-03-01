using System.Numerics;

namespace TopSpeed.Race
{
    internal abstract partial class Level
    {
        public void ClearPauseRequest()
        {
            PauseRequested = false;
        }

        public void StartStopwatchDiff()
        {
            _oldStopwatchMs = _stopwatch.ElapsedMilliseconds;
        }

        public void StopStopwatchDiff()
        {
            var now = _stopwatch.ElapsedMilliseconds;
            _stopwatchDiffMs += (now - _oldStopwatchMs);
        }

        protected void InitializeLevel()
        {
            _track.Initialize();
            _car.Initialize();
            _elapsedTotal = 0.0f;
            _oldStopwatchMs = 0;
            _stopwatchDiffMs = 0;
            _started = false;
            _finished = false;
            _engineStarted = false;
            _currentRoad.Surface = _track.InitialSurface;
            _car.ManualTransmission = _manualTransmission;
            _listenerInitialized = false;
            _lastListenerPosition = Vector3.Zero;
            ApplyActivePanelInputAccess();
            _panelManager.Resume();
        }

        protected void FinalizeLevel()
        {
            _panelManager.Pause();
            _car.FinalizeCar();
            _track.FinalizeTrack();
        }

        protected void RequestExitWhenQueueIdle()
        {
            _exitWhenQueueIdle = true;
        }

        protected void ScheduleDefaultStartSequence(float raceStartDelaySeconds = DefaultRaceStartDelaySeconds)
        {
            PushEvent(Events.RaceEventType.CarStart, DefaultCarStartDelaySeconds);
            PushEvent(Events.RaceEventType.RaceStart, raceStartDelaySeconds);
            PushEvent(Events.RaceEventType.PlaySound, DefaultStartCueDelaySeconds, _soundStart);
        }

        protected bool UpdateExitWhenQueueIdle()
        {
            if (!_exitWhenQueueIdle)
                return false;
            if (!_soundQueue.IsIdle)
                return false;
            ExitRequested = true;
            return true;
        }
    }
}
