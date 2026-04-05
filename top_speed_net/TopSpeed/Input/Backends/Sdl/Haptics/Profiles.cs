using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Input.Backends.Sdl
{
    internal enum HapticPlaybackMode
    {
        None = 0,
        Constant = 1,
        Periodic = 2,
        Condition = 3
    }

    internal struct FeedbackState
    {
        public float Low;
        public float High;
        public float LeftTrigger;
        public float RightTrigger;
        public int Gain;
        public bool RunPending;
    }
}
