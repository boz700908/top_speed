using System;
using TopSpeed.Common;
using TopSpeed.Input;
using TopSpeed.Race.Events;
using TopSpeed.Vehicles;

namespace TopSpeed.Race
{
    internal abstract partial class Level
    {
        protected void HandleEngineStartRequest()
        {
            if (_input.GetStartEngine() && _started && !_finished)
            {
                var canStart = !_engineStarted || _car.State == CarState.Crashed;
                if (canStart)
                {
                    _engineStarted = true;
                    if (_car.State == CarState.Crashed)
                        _car.RestartAfterCrash();
                    else
                        _car.Start();
                }
            }
        }

        protected void HandleCurrentGearRequest()
        {
            if (_input.GetCurrentGear() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var gear = _car.Gear;
                SpeakText(_car.InReverseGear ? "Gear reverse" : $"Gear {gear}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentLapNumberRequest()
        {
            if (_input.GetCurrentLapNr() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                SpeakText($"Lap {_lap}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentRacePercentageRequest()
        {
            if (_input.GetCurrentRacePerc() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var perc = (_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatPercentageText("Race percentage", units));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentLapPercentageRequest()
        {
            if (_input.GetCurrentLapPerc() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var perc = ((_car.PositionY - (_track.Length * (_lap - 1))) / _track.Length) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatPercentageText("Lap percentage", units));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentRaceTimeRequestActiveOnly()
        {
            if (_input.GetCurrentRaceTime() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var text = FormatTimeText((int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs), detailed: false);
                SpeakText($"Race time {text}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleCurrentRaceTimeRequestWithFinish()
        {
            if (_input.GetCurrentRaceTime() && _started && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                var timeMs = _lap <= _nrOfLaps
                    ? (int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs)
                    : _raceTime;
                var text = FormatTimeText(timeMs, detailed: false);
                SpeakText($"Race time {text}");
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleTrackNameRequest()
        {
            if (_input.GetTrackName() && _acceptCurrentRaceInfo)
            {
                _acceptCurrentRaceInfo = false;
                SpeakText(FormatTrackName(_track.TrackName));
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleSpeedReportRequest()
        {
            if (_input.GetSpeedReport() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var speedKmh = _car.SpeedKmh;
                var rpm = _car.EngineRpm;
                if (_settings.Units == UnitSystem.Imperial)
                {
                    var speedMph = speedKmh * KmToMiles;
                    SpeakText($"{speedMph:F0} miles per hour, {rpm:F0} RPM");
                }
                else
                {
                    SpeakText($"{speedKmh:F0} kilometers per hour, {rpm:F0} RPM");
                }
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandleDistanceReportRequest()
        {
            if (_input.GetDistanceReport() && _started && _acceptCurrentRaceInfo && _lap <= _nrOfLaps)
            {
                _acceptCurrentRaceInfo = false;
                var distanceM = _car.DistanceMeters;
                if (_settings.Units == UnitSystem.Imperial)
                {
                    var distanceMiles = distanceM / MetersPerMile;
                    if (distanceMiles >= 1f)
                        SpeakText($"{distanceMiles:F1} miles traveled");
                    else
                        SpeakText($"{distanceM * MetersToFeet:F0} feet traveled");
                }
                else
                {
                    var distanceKm = distanceM / 1000f;
                    if (distanceKm >= 1f)
                        SpeakText($"{distanceKm:F1} kilometers traveled");
                    else
                        SpeakText($"{distanceM:F0} meters traveled");
                }
                PushEvent(RaceEventType.AcceptCurrentRaceInfo, 0.5f);
            }
        }

        protected void HandlePauseRequest(ref bool pauseKeyReleased)
        {
            if (!_input.GetPause() && !pauseKeyReleased)
            {
                pauseKeyReleased = true;
            }
            else if (_input.GetPause() && pauseKeyReleased && _started && _lap <= _nrOfLaps && _car.State == CarState.Running)
            {
                pauseKeyReleased = false;
                PauseRequested = true;
            }
        }
    }
}
