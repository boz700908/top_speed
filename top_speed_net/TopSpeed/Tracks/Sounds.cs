using System;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        private void InitializeSounds()
        {
            var root = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _soundCrowd = CreateLegacySound(root, "crowd.wav");
            _soundOcean = CreateLegacySound(root, "ocean.wav");
            _soundRain = CreateLegacySound(root, "rain.wav");
            _soundWind = CreateLegacySound(root, "wind.wav");
            _soundStorm = CreateLegacySound(root, "storm.wav");
            _soundDesert = CreateLegacySound(root, "desert.wav");
            _soundAirport = CreateLegacySound(root, "airport.wav");
            _soundAirplane = CreateLegacySound(root, "airplane.wav");
            _soundClock = CreateLegacySound(root, "clock.wav");
            _soundJet = CreateLegacySound(root, "jet.wav");
            _soundThunder = CreateLegacySound(root, "thunder.wav");
            _soundPile = CreateLegacySound(root, "pile.wav");
            _soundConstruction = CreateLegacySound(root, "const.wav");
            _soundRiver = CreateLegacySound(root, "river.wav");
            _soundHelicopter = CreateLegacySound(root, "helicopter.wav");
            _soundOwl = CreateLegacySound(root, "owl.wav");
        }

        private Source? CreateLegacySound(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            var asset = _audio.LoadAsset(path, streamFromDisk: false);
            return _audio.CreateLoopingSource(asset, AudioEngineOptions.TrackBusName);
        }

        private void InitializeTrackSoundSources()
        {
            if (_soundDefinitions.Count == 0)
                return;

            foreach (var pair in _soundDefinitions)
            {
                var runtime = new RuntimeTrackSound(
                    _audio,
                    _sourceDirectory,
                    _random,
                    _soundDefinitions,
                    pair.Key,
                    pair.Value);
                _segmentTrackSounds[pair.Key] = runtime;
                _allTrackSounds.Add(runtime);

                if (pair.Value.Global && runtime.EnsureCreated(refreshRandomVariant: false, _ambientVolumeScale))
                    runtime.Play();
            }
        }

    }
}

