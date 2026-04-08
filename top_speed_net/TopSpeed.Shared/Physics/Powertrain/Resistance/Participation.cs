using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Physics.Powertrain
{
    public readonly struct TransmissionLossParticipation
    {
        public TransmissionLossParticipation(
            CouplingMode couplingMode,
            float drivelineDragParticipation,
            float engineBrakeParticipation)
        {
            CouplingMode = couplingMode;
            DrivelineDragParticipation = drivelineDragParticipation;
            EngineBrakeParticipation = engineBrakeParticipation;
        }

        public CouplingMode CouplingMode { get; }
        public float DrivelineDragParticipation { get; }
        public float EngineBrakeParticipation { get; }
        public bool IsFreeCoastEquivalent => DrivelineDragParticipation <= 0f && EngineBrakeParticipation <= 0f;
    }

    public static class TransmissionLossModel
    {
        private const float DisengagedThreshold = 0.05f;
        private const float LockedThreshold = 0.98f;
        private const float DctCarryBias = 0.20f;

        public static TransmissionLossParticipation Resolve(
            TransmissionType transmissionType,
            bool isNeutral,
            bool gearPathEngaged,
            float drivelineCouplingFactor)
        {
            if (isNeutral || !gearPathEngaged)
                return new TransmissionLossParticipation(CouplingMode.Disengaged, 0f, 0f);

            var coupling = Clamp01(drivelineCouplingFactor);
            if (transmissionType == TransmissionType.Manual)
            {
                if (coupling <= DisengagedThreshold)
                    return new TransmissionLossParticipation(CouplingMode.Disengaged, 1f, 0f);

                if (coupling >= LockedThreshold)
                    return new TransmissionLossParticipation(CouplingMode.Locked, 1f, 1f);

                return new TransmissionLossParticipation(CouplingMode.Blended, 1f, coupling);
            }

            if (coupling <= DisengagedThreshold)
                return new TransmissionLossParticipation(CouplingMode.Disengaged, 0f, 0f);

            if (coupling >= LockedThreshold)
                return new TransmissionLossParticipation(CouplingMode.Locked, 1f, 1f);

            switch (transmissionType)
            {
                case TransmissionType.Dct:
                    return new TransmissionLossParticipation(
                        CouplingMode.Blended,
                        Math.Min(1f, coupling + DctCarryBias),
                        coupling);

                case TransmissionType.Atc:
                case TransmissionType.Cvt:
                default:
                    return new TransmissionLossParticipation(CouplingMode.Blended, coupling, coupling);
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
