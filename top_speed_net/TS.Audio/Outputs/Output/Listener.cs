using System.Collections.Generic;
using System.Numerics;

namespace TS.Audio
{
    public sealed partial class AudioOutput
    {
        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            var snapshot = new ListenerStateSnapshot(
                position,
                velocity,
                NormalizeOrFallback(forward, new Vector3(0f, 0f, 1f)),
                NormalizeOrFallback(up, new Vector3(0f, 1f, 0f)));
            _listenerState = snapshot;
            _steamAudioRuntime?.UpdateListener(snapshot.Position, snapshot.Forward, snapshot.Up);
        }

        public void SetRoomAcoustics(RoomAcoustics acoustics)
        {
            _roomAcoustics = acoustics;
            AudioSourceHandle[] snapshot;
            lock (_sourceLock)
                snapshot = _sources.ToArray();

            for (var i = 0; i < snapshot.Length; i++)
                snapshot[i].SetRoomAcoustics(_roomAcoustics);

            _diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.OutputRoomAcousticsChanged,
                AudioDiagnosticEntityType.Output,
                Name,
                null,
                null,
                "Audio output room acoustics changed.",
                new Dictionary<string, object?>
                {
                    ["hasRoom"] = acoustics.HasRoom,
                    ["reverbTimeSeconds"] = acoustics.ReverbTimeSeconds,
                    ["reverbGain"] = acoustics.ReverbGain
                });
        }
    }
}
