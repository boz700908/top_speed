namespace TopSpeed.Vehicles.Control
{
    internal readonly struct CarControlIntent
    {
        public CarControlIntent(
            int steering,
            int throttle,
            int brake,
            bool horn,
            bool gearUp,
            bool gearDown,
            bool reverseRequested,
            bool forwardRequested)
        {
            Steering = ClampPercent(steering);
            Throttle = ClampPercent(throttle);
            Brake = ClampPercent(brake);
            Horn = horn;
            GearUp = gearUp;
            GearDown = gearDown;
            ReverseRequested = reverseRequested;
            ForwardRequested = forwardRequested;
        }

        public int Steering { get; }
        public int Throttle { get; }
        public int Brake { get; }
        public bool Horn { get; }
        public bool GearUp { get; }
        public bool GearDown { get; }
        public bool ReverseRequested { get; }
        public bool ForwardRequested { get; }

        public static CarControlIntent Neutral { get; } = new CarControlIntent(0, 0, 0, false, false, false, false, false);

        private static int ClampPercent(int value)
        {
            if (value > 100)
                return 100;
            if (value < -100)
                return -100;
            return value;
        }
    }
}
