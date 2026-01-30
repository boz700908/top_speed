using System;
using SteamAudio;

namespace TopSpeed.Tracks.Acoustics
{
    internal sealed class TrackSteamAudioScene : IDisposable
    {
        private IPL.Scene _scene;
        private IPL.StaticMesh _staticMesh;

        public TrackSteamAudioScene(IPL.Scene scene, IPL.StaticMesh staticMesh)
        {
            _scene = scene;
            _staticMesh = staticMesh;
        }

        public IPL.Scene Scene => _scene;

        public void Dispose()
        {
            if (_staticMesh.Handle != IntPtr.Zero && _scene.Handle != IntPtr.Zero)
                IPL.StaticMeshRemove(_staticMesh, _scene);

            if (_staticMesh.Handle != IntPtr.Zero)
                IPL.StaticMeshRelease(ref _staticMesh);

            if (_scene.Handle != IntPtr.Zero)
                IPL.SceneRelease(ref _scene);
        }
    }
}
