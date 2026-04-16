using TopSpeed.Input.Devices.Controller;
using TS.Sdl.Input;

namespace TopSpeed.Input
{
    internal sealed partial class InputService
    {
        public void Update()
        {
            _previous.CopyFrom(_current);
            _current.Clear();

            if (_suspended || _disposed)
                return;

            if (!_keyboardBackend.TryPopulateState(_current))
                return;

            _controllerBackend.Update();
        }

        public bool WasPressed(InputKey key)
        {
            if (_suspended)
                return false;

            var index = (int)key;
            if (index < 0 || index >= _keyLatch.Length)
                return false;

            if (_keyboardBackend.IsDown(key))
            {
                if (_keyLatch[index])
                    return false;

                _keyLatch[index] = true;
                return true;
            }

            _keyLatch[index] = false;
            return false;
        }

        public void SubmitGesture(in GestureEvent value)
        {
            if (_suspended || _disposed)
                return;
            if (!GestureIntentMapper.TryMap(in value, out var intent))
                return;

            lock (_gestureSync)
            {
                if (_gesturePressCounts.TryGetValue(intent, out var count))
                    _gesturePressCounts[intent] = count + 1;
                else
                    _gesturePressCounts[intent] = 1;
            }
        }

        public bool WasGesturePressed(GestureIntent intent)
        {
            if (_suspended)
                return false;
            if (intent == GestureIntent.Unknown)
                return false;

            lock (_gestureSync)
            {
                if (!_gesturePressCounts.TryGetValue(intent, out var count) || count <= 0)
                    return false;

                if (count == 1)
                    _gesturePressCounts.Remove(intent);
                else
                    _gesturePressCounts[intent] = count - 1;
                return true;
            }
        }

        public bool TryGetControllerState(out State state)
        {
            return _controllerBackend.TryGetState(out state);
        }

        public void ResetState()
        {
            _current.Clear();
            _previous.Clear();
            for (var i = 0; i < _keyLatch.Length; i++)
                _keyLatch[i] = false;
            lock (_gestureSync)
                _gesturePressCounts.Clear();
        }
    }
}

