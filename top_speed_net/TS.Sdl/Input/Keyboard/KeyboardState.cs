using System;

namespace TS.Sdl.Input
{
    public readonly unsafe struct KeyboardState
    {
        private readonly byte* _state;
        private readonly int _count;

        internal KeyboardState(IntPtr state, int count)
        {
            _state = (byte*)state;
            _count = count;
        }

        public int Count => _count;
        public bool IsValid => _state != null && _count > 0;

        public bool IsDown(Scancode scancode)
        {
            var index = (int)scancode;
            if (_state == null || index < 0 || index >= _count)
                return false;

            return _state[index] != 0;
        }
    }
}
