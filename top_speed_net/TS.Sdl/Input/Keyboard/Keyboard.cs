using System;
using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    public static class Keyboard
    {
        private const string LibraryName = "SDL3";

        public static KeyboardState GetState()
        {
            if (!Runtime.IsAvailable)
                return default;

            var state = SDL_GetKeyboardState(out var count);
            return new KeyboardState(state, count);
        }

        public static void Reset()
        {
            if (!Runtime.IsAvailable)
                return;

            SDL_ResetKeyboard();
        }

        public static bool StartTextInput(IntPtr window)
        {
            if (!Runtime.IsAvailable)
                return false;

            return SDL_StartTextInput(window);
        }

        public static bool StopTextInput(IntPtr window)
        {
            if (!Runtime.IsAvailable)
                return false;

            return SDL_StopTextInput(window);
        }

        public static bool IsTextInputActive(IntPtr window)
        {
            if (!Runtime.IsAvailable)
                return false;

            return SDL_TextInputActive(window);
        }

        [DllImport(LibraryName, EntryPoint = "SDL_GetKeyboardState", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetKeyboardState(out int numkeys);

        [DllImport(LibraryName, EntryPoint = "SDL_ResetKeyboard", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_ResetKeyboard();

        [DllImport(LibraryName, EntryPoint = "SDL_StartTextInput", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_StartTextInput(IntPtr window);

        [DllImport(LibraryName, EntryPoint = "SDL_StopTextInput", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_StopTextInput(IntPtr window);

        [DllImport(LibraryName, EntryPoint = "SDL_TextInputActive", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_TextInputActive(IntPtr window);
    }
}
