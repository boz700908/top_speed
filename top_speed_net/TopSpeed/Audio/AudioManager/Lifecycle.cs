namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager
    {
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopUpdateThread();
            _updateWake.Dispose();
            ClearCachedPaths();
            _engine.Dispose();
        }
    }
}

