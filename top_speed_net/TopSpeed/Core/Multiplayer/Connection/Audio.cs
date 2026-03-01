using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void StartConnectingPulse()
        {
            StopConnectingPulse();
            var handle = GetNetworkSound(ref _connectingSound, "connecting.ogg");
            if (handle == null)
                return;

            _connectingSoundCts = new CancellationTokenSource();
            var token = _connectingSoundCts.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        handle.Restart(loop: false);
                    }
                    catch
                    {
                    }

                    try
                    {
                        await Task.Delay(ConnectingPulseIntervalMs, token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        private void StopConnectingPulse()
        {
            _connectingSoundCts?.Cancel();
            _connectingSoundCts?.Dispose();
            _connectingSoundCts = null;
            try
            {
                _connectingSound?.Stop();
            }
            catch
            {
            }
        }

        private void PlayNetworkSound(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            AudioSourceHandle? handle;
            if (string.Equals(fileName, "online.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _onlineSound, fileName);
            else if (string.Equals(fileName, "offline.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _offlineSound, fileName);
            else if (string.Equals(fileName, "connecting.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _connectingSound, fileName);
            else if (string.Equals(fileName, "ping_start.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _pingStartSound, fileName);
            else if (string.Equals(fileName, "ping.ogg", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(fileName, "ping_stop.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _pingSound, fileName);
            else if (string.Equals(fileName, "room_created.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _roomCreatedSound, fileName);
            else if (string.Equals(fileName, "room_join.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _roomJoinSound, fileName);
            else if (string.Equals(fileName, "room_leave.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _roomLeaveSound, fileName);
            else if (string.Equals(fileName, "connected.ogg", StringComparison.OrdinalIgnoreCase))
                handle = GetNetworkSound(ref _connectedSound, fileName);
            else
                handle = null;

            if (handle == null)
                return;

            try
            {
                handle.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                handle.Restart(loop: false);
            }
            catch
            {
            }
        }

        private AudioSourceHandle? GetNetworkSound(ref AudioSourceHandle? cache, string fileName)
        {
            if (cache != null)
                return cache;

            var path = Path.Combine(AssetPaths.SoundsRoot, "network", fileName ?? string.Empty);
            if (!_audio.TryResolvePath(path, out var fullPath))
                return null;

            try
            {
                cache = _audio.AcquireCachedSource(fullPath, streamFromDisk: false);
                cache.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                return cache;
            }
            catch
            {
                return null;
            }
        }
    }
}
