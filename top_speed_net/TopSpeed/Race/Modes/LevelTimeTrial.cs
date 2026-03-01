using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Common;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Race.Events;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal sealed class LevelTimeTrial : Level
    {
        private const string HighscoreFile = "highscore.cfg";
        private bool _pauseKeyReleased = true;

        public LevelTimeTrial(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
            : base(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice)
        {
        }

        public void Initialize()
        {
            InitializeLevel();
            _soundTheme4 = LoadLanguageSound("music\\theme4", streamFromDisk: false);
            _soundPause = LoadLanguageSound("race\\pause");
            _soundUnpause = LoadLanguageSound("race\\unpause");
            _soundTheme4.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));
        }

        public void FinalizeLevelTimeTrial()
        {
            FinalizeLevel();
        }

        public void Run(float elapsed)
        {
            EnsureStartSequenceScheduled();
            ProcessDueEvents();

            UpdateVehiclePanels(elapsed);
            _car.Run(elapsed);
            _track.Run(_car.PositionY);
            var road = _track.RoadAtPosition(_car.PositionY);
            _car.Evaluate(road);
            UpdateAudioListener(elapsed);
            if (_track.NextRoad(_car.PositionY, _car.Speed, (int)_settings.CurveAnnouncement, out var nextRoad))
                CallNextRoad(nextRoad);

            if (_track.Lap(_car.PositionY) > _lap)
            {
                _lap = _track.Lap(_car.PositionY);
                if (_lap > _nrOfLaps)
                {
                    var finishSound = _randomSounds[(int)RandomSound.Finish][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Finish])];
                    if (finishSound != null)
                        Speak(finishSound, true);
                    _car.ManualTransmission = false;
                    _car.Quiet();
                    _car.Stop();
                    _raceTime = (int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs);
                    PushEvent(RaceEventType.RaceFinish, 2.0f);
                }
                else if (_settings.AutomaticInfo != AutomaticInfoMode.Off && _lap > 1 && _lap < _nrOfLaps + 1)
                {
                    Speak(_soundLaps[_nrOfLaps - _lap], true);
                }
            }

            HandleCoreRaceMetricsRequests(includeFinishedRaceTime: false);

            if (_input.TryGetPlayerInfo(out var player) && _acceptPlayerInfo && player == 0)
            {
                _acceptPlayerInfo = false;
                SpeakText(GetVehicleName());
                PushEvent(RaceEventType.AcceptPlayerInfo, 0.5f);
            }

            HandleGeneralInfoRequests(ref _pauseKeyReleased);

            if (CompleteFrame(elapsed))
                return;
        }

        protected override void OnRaceFinishEvent()
        {
            AppendDefaultRaceFinishAnnouncement();
            _highscore = ReadHighScore();
            if ((_raceTime < _highscore) || (_highscore == 0))
            {
                WriteHighScore();
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNewTime);
                _sayTimeLength += _soundNewTime.GetLengthSeconds();
            }
            else
            {
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundBestTime);
                _sayTimeLength += _soundBestTime.GetLengthSeconds() + 0.5f;
                SayTime(_highscore);
            }

            PushEvent(RaceEventType.RaceTimeFinalize, _sayTimeLength);
        }

        protected override void OnRaceTimeFinalizeEvent()
        {
            base.OnRaceTimeFinalizeEvent();
            RequestExitWhenQueueIdle();
        }

        public void Pause()
        {
            PauseVehiclePanels();
            FadeIn();
            _soundTheme4?.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));
            _soundTheme4?.Play(loop: true);
            _car.Pause();
            _soundPause?.Play(loop: false);
        }

        public void Unpause()
        {
            _car.Unpause();
            ResumeVehiclePanels();
            FadeOut();
            _soundTheme4?.Stop();
            _soundTheme4?.SeekToStart();
            _soundUnpause?.Play(loop: false);
        }

        private int ReadHighScore()
        {
            var path = Path.Combine(AppContext.BaseDirectory, HighscoreFile);
            if (!File.Exists(path))
                return 0;
            var key = $"{_track.TrackName};{_nrOfLaps}";
            foreach (var line in File.ReadLines(path))
            {
                var idx = line.IndexOf('=');
                if (idx <= 0)
                    continue;
                var field = line.Substring(0, idx).Trim();
                if (!string.Equals(field, key, StringComparison.OrdinalIgnoreCase))
                    continue;
                var valuePart = line.Substring(idx + 1).Trim();
                if (int.TryParse(valuePart, out var value))
                    return value;
            }
            return 0;
        }

        private void WriteHighScore()
        {
            var path = Path.Combine(AppContext.BaseDirectory, HighscoreFile);
            var key = $"{_track.TrackName};{_nrOfLaps}";
            var lines = new List<string>();
            var found = false;
            if (File.Exists(path))
            {
                foreach (var line in File.ReadLines(path))
                {
                    var idx = line.IndexOf('=');
                    if (idx <= 0)
                    {
                        lines.Add(line);
                        continue;
                    }
                    var field = line.Substring(0, idx).Trim();
                    if (string.Equals(field, key, StringComparison.OrdinalIgnoreCase))
                    {
                        lines.Add($"{key}={_raceTime}");
                        found = true;
                    }
                    else
                    {
                        lines.Add(line);
                    }
                }
            }

            if (!found)
                lines.Add($"{key}={_raceTime}");

            File.WriteAllLines(path, lines);
        }

        private AudioSourceHandle LoadCustomSound(string fileName)
        {
            var path = Path.IsPathRooted(fileName)
                ? fileName
                : Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(path))
                return LoadLegacySound("error.wav");
            return _audio.CreateSource(path, streamFromDisk: true);
        }

        private string GetVehicleName()
        {
            if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                return FormatVehicleName(_car.CustomFile);
            return _car.VehicleName;
        }
    }
}
