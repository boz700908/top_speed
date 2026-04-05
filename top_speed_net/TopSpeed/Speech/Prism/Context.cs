using System;
using System.Collections.Generic;

namespace TopSpeed.Speech.Prism
{
    internal sealed class Context : IDisposable
    {
        private IntPtr _handle;

        public Context()
        {
            var config = Native.ConfigInit();
            _handle = Native.Init(ref config);
            if (_handle == IntPtr.Zero)
                throw new PrismException(Error.NotInitialized);
        }

        public IReadOnlyList<BackendInfo> AvailableBackends
        {
            get
            {
                EnsureOpen();

                var backends = new List<BackendInfo>();
                var count = Native.RegistryCount(_handle);
                for (var i = 0; i < count; i++)
                {
                    var id = Native.RegistryIdAt(_handle, i);
                    if (id == Ids.Invalid)
                        continue;

                    backends.Add(new BackendInfo(
                        id,
                        Native.RegistryName(_handle, id) ?? string.Empty,
                        Native.RegistryPriority(_handle, id),
                        Native.RegistryExists(_handle, id)));
                }

                return backends;
            }
        }

        public Backend Acquire(ulong id)
        {
            EnsureOpen();
            return OpenBackend(Native.Acquire(_handle, id), id);
        }

        public Backend Create(ulong id)
        {
            EnsureOpen();
            return OpenBackend(Native.Create(_handle, id), id);
        }

        public Backend AcquireBest()
        {
            EnsureOpen();
            return OpenBackend(Native.AcquireBest(_handle), Ids.Invalid);
        }

        public Backend CreateBest()
        {
            EnsureOpen();
            return OpenBackend(Native.CreateBest(_handle), Ids.Invalid);
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
                return;

            Native.Shutdown(_handle);
            _handle = IntPtr.Zero;
        }

        private Backend OpenBackend(IntPtr handle, ulong requestedId)
        {
            if (handle == IntPtr.Zero)
                throw new PrismException(Error.BackendNotAvailable);

            var backend = new Backend(handle, requestedId);
            backend.Initialize();
            return backend;
        }

        private void EnsureOpen()
        {
            if (_handle == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(Context));
        }
    }
}
