namespace TopSpeed.Physics.Powertrain
{
    public readonly struct ResistanceBreakdown
    {
        public ResistanceBreakdown(
            float aerodynamicForceN,
            float rollingResistanceForceN,
            float wheelSideDragForceN,
            float coupledDrivelineDragForceN)
        {
            AerodynamicForceN = aerodynamicForceN;
            RollingResistanceForceN = rollingResistanceForceN;
            WheelSideDragForceN = wheelSideDragForceN;
            CoupledDrivelineDragForceN = coupledDrivelineDragForceN;
        }

        public float AerodynamicForceN { get; }
        public float RollingResistanceForceN { get; }
        public float WheelSideDragForceN { get; }
        public float CoupledDrivelineDragForceN { get; }
        public float TotalForceN => AerodynamicForceN + RollingResistanceForceN + WheelSideDragForceN + CoupledDrivelineDragForceN;
    }
}
