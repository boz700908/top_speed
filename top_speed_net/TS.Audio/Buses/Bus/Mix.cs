using System.Collections.Generic;

namespace TS.Audio
{
    public sealed partial class AudioBus
    {
        public void SetVolume(float volume)
        {
            _localVolume = Clamp01(volume);
            _mixer.Volume = _muted ? 0f : _localVolume;
            RecalculateMix();
            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.BusVolumeChanged,
                AudioDiagnosticEntityType.Bus,
                _output.Name,
                Name,
                null,
                "Audio bus volume changed.",
                new Dictionary<string, object?>
                {
                    ["localVolume"] = _localVolume,
                    ["localVolumeDb"] = AudioMath.GainToDecibels(_localVolume),
                    ["effectiveVolume"] = _effectiveVolume,
                    ["effectiveVolumeDb"] = AudioMath.GainToDecibels(_effectiveVolume)
                },
                () => new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
        }

        public float GetVolume()
        {
            return _localVolume;
        }

        public float GetEffectiveVolume()
        {
            return _effectiveVolume;
        }

        public void SetMuted(bool muted)
        {
            _muted = muted;
            _mixer.Mute = muted;
            _mixer.Volume = _muted ? 0f : _localVolume;
            RecalculateMix();
            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.BusMuteChanged,
                AudioDiagnosticEntityType.Bus,
                _output.Name,
                Name,
                null,
                muted ? "Audio bus muted." : "Audio bus unmuted.",
                new Dictionary<string, object?>
                {
                    ["muted"] = _muted,
                    ["effectiveVolume"] = _effectiveVolume,
                    ["effectiveVolumeDb"] = AudioMath.GainToDecibels(_effectiveVolume)
                },
                () => new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
        }

        private void ConfigureSource(Source source, ResolvedSourceOptions options)
        {
            source.SetVolume(options.Volume);
            source.SetPitch(options.Pitch);
            source.SetPan(options.Pan);
            source.SetStereoWidening(options.StereoWidening);

            if (options.Position.HasValue)
                source.SetPosition(options.Position.Value);
            if (options.Velocity.HasValue)
                source.SetVelocity(options.Velocity.Value);
            if (options.DistanceModel.HasValue)
                source.SetDistanceModel(options.DistanceModel.Value, options.RefDistance, options.MaxDistance, options.RollOff);
            if (options.CurveDistanceScaler.HasValue)
                source.SetCurveDistanceScaler(options.CurveDistanceScaler.Value);
            if (options.DopplerFactor.HasValue)
                source.SetDopplerFactor(options.DopplerFactor.Value);
            if (options.RoomAcoustics.HasValue)
                source.SetRoomAcoustics(options.RoomAcoustics.Value);
        }

        private void RecalculateMix()
        {
            var parentVolume = _parent?._effectiveVolume ?? 1f;
            _effectiveVolume = (_muted ? 0f : Clamp01(_localVolume)) * parentVolume;

            for (var i = 0; i < _children.Count; i++)
                _children[i].RecalculateMix();
        }
    }
}
