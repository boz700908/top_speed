namespace TopSpeed.Speech.ScreenReaders
{
    internal static class Factory
    {
        public static IScreenReader Create()
        {
            return new Prism.Reader();
        }
    }
}
