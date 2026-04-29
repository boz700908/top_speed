using System;
using System.Collections.Generic;

namespace TS.Audio
{
    public sealed partial class AudioEngine
    {
        public void ConfigureDiagnostics(AudioDiagnosticConfig config)
        {
            Diagnostics.Configure(config);
        }

        public AudioDiagnosticSubscription SubscribeDiagnostics(Action<AudioDiagnosticEvent> handler, AudioDiagnosticFilter? filter = null)
        {
            return Diagnostics.Subscribe(handler, filter);
        }

        public void AddDiagnosticsSink(IAudioDiagnosticSink sink, AudioDiagnosticFilter? filter = null)
        {
            Diagnostics.AddSink(sink, filter);
        }

        public bool RemoveDiagnosticsSink(IAudioDiagnosticSink sink)
        {
            return Diagnostics.RemoveSink(sink);
        }

        public IReadOnlyList<AudioDiagnosticEvent> GetDiagnosticsHistory(AudioDiagnosticFilter? filter = null)
        {
            return Diagnostics.GetHistory(filter);
        }

        public void ClearDiagnosticsHistory()
        {
            Diagnostics.ClearHistory();
        }
    }
}
