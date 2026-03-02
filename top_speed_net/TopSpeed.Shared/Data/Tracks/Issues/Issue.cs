namespace TopSpeed.Data
{
    public readonly struct TrackTsmIssue
    {
        public TrackTsmIssue(TrackTsmIssueSeverity severity, int lineNumber, string message)
        {
            Severity = severity;
            LineNumber = lineNumber;
            Message = message ?? string.Empty;
        }

        public TrackTsmIssueSeverity Severity { get; }
        public int LineNumber { get; }
        public string Message { get; }

        public override string ToString()
        {
            return LineNumber > 0
                ? $"{Severity} (line {LineNumber}): {Message}"
                : $"{Severity}: {Message}";
        }
    }
}
