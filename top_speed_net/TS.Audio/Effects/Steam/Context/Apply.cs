using System;
using SteamAudio;

namespace TS.Audio
{
    internal sealed unsafe partial class SteamAudioRuntime
    {
        private static void ApplyRoomOnlyOutputs(AudioSourceHandle handle)
        {
            var direct = new IPL.DirectEffectParams
            {
                Occlusion = 1f
            };
            direct.AirAbsorption[0] = 1f;
            direct.AirAbsorption[1] = 1f;
            direct.AirAbsorption[2] = 1f;
            direct.Transmission[0] = 1f;
            direct.Transmission[1] = 1f;
            direct.Transmission[2] = 1f;
            ApplyDirectOutputs(handle, in direct);

            if (!handle.RoomAcoustics.HasRoom)
            {
                handle.ClearReverbSimulation();
                return;
            }

            var reflections = new IPL.ReflectionEffectParams();
            ApplyReverbOutputs(handle, in reflections);
        }

        private static void ApplyDirectOutputs(AudioSourceHandle handle, in IPL.DirectEffectParams direct)
        {
            var room = handle.RoomAcoustics;
            var hasRoom = room.HasRoom;

            var airLow = direct.AirAbsorption[0];
            var airMid = direct.AirAbsorption[1];
            var airHigh = direct.AirAbsorption[2];
            var transLow = direct.Transmission[0];
            var transMid = direct.Transmission[1];
            var transHigh = direct.Transmission[2];
            var occlusion = direct.Occlusion;

            if (room.OcclusionOverride.HasValue)
            {
                occlusion = Clamp01(room.OcclusionOverride.Value);
            }
            else if (hasRoom)
            {
                var scale = Clamp01(room.OcclusionScale);
                occlusion = Lerp(1f, occlusion, scale);
            }

            if (room.TransmissionOverrideLow.HasValue || room.TransmissionOverrideMid.HasValue || room.TransmissionOverrideHigh.HasValue)
            {
                if (room.TransmissionOverrideLow.HasValue) transLow = Clamp01(room.TransmissionOverrideLow.Value);
                if (room.TransmissionOverrideMid.HasValue) transMid = Clamp01(room.TransmissionOverrideMid.Value);
                if (room.TransmissionOverrideHigh.HasValue) transHigh = Clamp01(room.TransmissionOverrideHigh.Value);
            }
            else if (hasRoom)
            {
                var scale = Clamp01(room.TransmissionScale);
                transLow = Lerp(1f, transLow, scale);
                transMid = Lerp(1f, transMid, scale);
                transHigh = Lerp(1f, transHigh, scale);
            }

            if (room.AirAbsorptionOverrideLow.HasValue || room.AirAbsorptionOverrideMid.HasValue || room.AirAbsorptionOverrideHigh.HasValue)
            {
                if (room.AirAbsorptionOverrideLow.HasValue) airLow = Clamp01(room.AirAbsorptionOverrideLow.Value);
                if (room.AirAbsorptionOverrideMid.HasValue) airMid = Clamp01(room.AirAbsorptionOverrideMid.Value);
                if (room.AirAbsorptionOverrideHigh.HasValue) airHigh = Clamp01(room.AirAbsorptionOverrideHigh.Value);
            }
            else if (hasRoom)
            {
                var scale = Clamp01(room.AirAbsorptionScale);
                airLow = Lerp(1f, airLow, scale);
                airMid = Lerp(1f, airMid, scale);
                airHigh = Lerp(1f, airHigh, scale);
            }

            handle.ApplyDirectSimulation(occlusion, airLow, airMid, airHigh, transLow, transMid, transHigh);
        }

        private static void ApplyReverbOutputs(AudioSourceHandle handle, in IPL.ReflectionEffectParams reflections)
        {
            var room = handle.RoomAcoustics;
            if (!room.HasRoom)
            {
                handle.ClearReverbSimulation();
                return;
            }

            var timeLow = reflections.ReverbTimes[0];
            var timeMid = reflections.ReverbTimes[1];
            var timeHigh = reflections.ReverbTimes[2];
            var eqLow = reflections.Eq[0];
            var eqMid = reflections.Eq[1];
            var eqHigh = reflections.Eq[2];
            var delay = reflections.Delay;

            var roomTimeMid = Math.Max(0f, room.ReverbTimeSeconds);
            var roomTimeLow = roomTimeMid;
            var roomTimeHigh = roomTimeMid * Clamp01(room.HfDecayRatio);

            if (roomTimeMid > 0f)
            {
                timeLow = roomTimeLow;
                timeMid = roomTimeMid;
                timeHigh = roomTimeHigh;
                eqLow = 1f;
                eqMid = 1f;
                eqHigh = 1f;
                delay = Math.Max(0, delay);
            }

            handle.ApplyReverbSimulation(
                timeLow,
                timeMid,
                timeHigh,
                eqLow,
                eqMid,
                eqHigh,
                delay,
                GetReflectionWetScale(room));
        }

        private static float GetReflectionWetScale(RoomAcoustics room)
        {
            const float defaultWetScale = 0.35f;
            if (!room.HasRoom)
                return 0f;
            return defaultWetScale * Clamp01(room.ReverbGain);
        }
    }
}
