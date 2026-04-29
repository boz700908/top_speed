using System;
using TS.Sdl.Input;
using SdlRuntime = TS.Sdl.Runtime;

namespace TopSpeed.Input.Backends.Sdl
{
    internal sealed class Factory : IControllerBackendFactory, IBackendSupportDiagnostics
    {
        private readonly IControllerEventSource? _eventSource;

        public Factory(IControllerEventSource? eventSource = null)
        {
            _eventSource = eventSource;
        }

        public string Id => "sdl";
        public int Priority => 200;

        public bool IsSupported()
        {
            return SdlRuntime.IsAvailable;
        }

        public IControllerBackend Create(IntPtr windowHandle)
        {
            return new Controller(_eventSource);
        }

        public string? GetUnsupportedReason()
        {
            return SdlRuntime.GetError();
        }
    }
}
