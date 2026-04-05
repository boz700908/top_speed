namespace TS.Sdl.Input
{
    public readonly struct PowerInfo
    {
        public PowerInfo(PowerState state, int percent)
        {
            State = state;
            Percent = percent;
        }

        public PowerState State { get; }
        public int Percent { get; }
    }
}
