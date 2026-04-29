using System;

namespace TS.Sdl.Input
{
    public interface IControllerEventSource
    {
        event Action<ControllerEvent> ControllerEventRaised;
    }
}
