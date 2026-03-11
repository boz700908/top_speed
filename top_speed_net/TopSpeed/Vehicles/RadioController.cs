using System;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Protocol;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed class VehicleRadioController : IDisposable
    {
        private readonly AudioManager _audio;
        private AudioSourceHandle? _source;
        private bool _desiredPlaying;
        private bool _pausedByGame;
        private string? _mediaPath;
        private string? _ownedTempFile;
        private uint _mediaId;
        private int _volumePercent = 100;
        private bool _loopPlayback = true;

        public VehicleRadioController(AudioManager audio)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        public uint MediaId => _mediaId;
        public bool HasMedia => _source != null;
        public bool IsPlaying => _source != null && _source.IsPlaying;
        public bool DesiredPlaying => _desiredPlaying;
        public string? MediaPath => _mediaPath;
        public int VolumePercent => _volumePercent;
        public bool LoopPlayback => _loopPlayback;

        public void SetVolumePercent(int volumePercent)
        {
            if (volumePercent < 0)
                volumePercent = 0;
            if (volumePercent > 100)
                volumePercent = 100;

            _volumePercent = volumePercent;
            _source?.SetVolumePercent(_volumePercent);
        }

        public void SetLoopPlayback(bool loopPlayback)
        {
            _loopPlayback = loopPlayback;
            if (_source == null || !_desiredPlaying || _pausedByGame || !_source.IsPlaying)
                return;

            _source.Stop();
            _source.Play(loop: _loopPlayback);
        }

        public bool TryLoadFromFile(string path, uint mediaId, bool preservePlaybackState, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "No media file selected.";
                return false;
            }

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                error = "The selected media file does not exist.";
                return false;
            }

            try
            {
                var wasPlaying = preservePlaybackState ? _desiredPlaying : false;
                DisposeSource();
                _source = _audio.CreateSpatialSource(fullPath, streamFromDisk: true, allowHrtf: true);
                _source.SetDopplerFactor(0f);
                _source.SetVolumePercent(_volumePercent);
                _mediaPath = fullPath;
                _mediaId = mediaId;
                _desiredPlaying = wasPlaying;
                if (_desiredPlaying && !_pausedByGame)
                    _source.Play(loop: _loopPlayback);
                return true;
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (NotSupportedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryLoadFromBytes(byte[] data, string extension, uint mediaId, bool preservePlaybackState, out string error)
        {
            error = string.Empty;
            if (data == null || data.Length == 0)
            {
                error = "Received media is empty.";
                return false;
            }

            try
            {
                var folder = Path.Combine(Path.GetTempPath(), "TopSpeed", "Radio");
                Directory.CreateDirectory(folder);
                var normalizedExtension = NormalizeExtension(extension);
                var path = Path.Combine(folder, $"radio_{mediaId}_{Guid.NewGuid():N}{normalizedExtension}");
                File.WriteAllBytes(path, data);
                if (TryLoadFromFile(path, mediaId, preservePlaybackState, out error))
                {
                    ReplaceOwnedTempFile(path);
                    return true;
                }

                SafeDelete(path);
                return false;
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (NotSupportedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public void SetPlayback(bool playing)
        {
            _desiredPlaying = playing;
            if (_source == null || _pausedByGame)
                return;

            if (playing)
            {
                if (!_source.IsPlaying)
                    _source.Play(loop: _loopPlayback);
            }
            else if (_source.IsPlaying)
            {
                _source.Stop();
            }
        }

        public void TogglePlayback()
        {
            SetPlayback(!_desiredPlaying);
        }

        public void PauseForGame()
        {
            _pausedByGame = true;
            if (_source != null && _source.IsPlaying)
                _source.Stop();
        }

        public void ResumeFromGame()
        {
            _pausedByGame = false;
            if (_source != null && _desiredPlaying && !_source.IsPlaying)
                _source.Play(loop: _loopPlayback);
        }

        public void ClearMedia()
        {
            _desiredPlaying = false;
            _mediaId = 0;
            _mediaPath = null;
            DisposeSource();
        }

        public void UpdateSpatial(float worldX, float worldZ, Vector3 worldVelocity)
        {
            if (_source == null)
                return;

            _source.SetPosition(AudioWorld.Position(worldX, worldZ));
            _source.SetVelocity(AudioWorld.ToMeters(worldVelocity));
        }

        public void Dispose()
        {
            DisposeSource();
        }

        private void DisposeSource()
        {
            if (_source != null)
            {
                _source.Stop();
                _source.Dispose();
                _source = null;
            }

            if (!string.IsNullOrWhiteSpace(_ownedTempFile))
            {
                SafeDelete(_ownedTempFile!);
                _ownedTempFile = null;
            }
        }

        private void ReplaceOwnedTempFile(string path)
        {
            if (!string.IsNullOrWhiteSpace(_ownedTempFile) && !string.Equals(_ownedTempFile, path, StringComparison.OrdinalIgnoreCase))
                SafeDelete(_ownedTempFile!);
            _ownedTempFile = path;
        }

        private static string NormalizeExtension(string extension)
        {
            var trimmed = (extension ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return ".bin";
            if (!trimmed.StartsWith(".", StringComparison.Ordinal))
                trimmed = "." + trimmed;
            if (trimmed.Length > ProtocolConstants.MaxMediaFileExtensionLength + 1)
                trimmed = trimmed.Substring(0, ProtocolConstants.MaxMediaFileExtensionLength + 1);
            return trimmed;
        }

        private static void SafeDelete(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort cleanup only.
            }
        }
    }
}
