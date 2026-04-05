using System;
using System.Runtime.InteropServices;
using TS.Sdl.Interop;

namespace TS.Sdl.Dialogs
{
    [Flags]
    public enum MessageBoxFlags : uint
    {
        Error = 0x00000010u,
        Warning = 0x00000020u,
        Information = 0x00000040u,
        ButtonsLeftToRight = 0x00000080u,
        ButtonsRightToLeft = 0x00000100u
    }

    public enum FileDialogType
    {
        OpenFile,
        SaveFile,
        OpenFolder
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void NativeDialogFileCallback(IntPtr userdata, IntPtr fileList, int filter);

    public readonly struct DialogFileFilter : IDisposable
    {
        internal readonly IntPtr Name;
        internal readonly IntPtr Pattern;

        public DialogFileFilter(string name, string pattern)
        {
            Name = Utf8.ToNative(name);
            Pattern = Utf8.ToNative(pattern);
        }

        public void Dispose()
        {
            if (Name != IntPtr.Zero)
                Marshal.FreeHGlobal(Name);
            if (Pattern != IntPtr.Zero)
                Marshal.FreeHGlobal(Pattern);
        }
    }

    public sealed class FileDialogResult
    {
        public FileDialogResult(string[] paths, int selectedFilter, bool wasCancelled, string? error)
        {
            Paths = paths ?? Array.Empty<string>();
            SelectedFilter = selectedFilter;
            WasCancelled = wasCancelled;
            Error = error;
        }

        public string[] Paths { get; }
        public int SelectedFilter { get; }
        public bool WasCancelled { get; }
        public string? Error { get; }
    }
}
