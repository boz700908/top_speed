namespace TopSpeed.Protocol
{
    public readonly struct ProtocolCompatResult
    {
        public ProtocolCompatResult(ProtocolCompatStatus status, ProtocolVer negotiatedVersion)
        {
            Status = status;
            NegotiatedVersion = negotiatedVersion;
        }

        public ProtocolCompatStatus Status { get; }
        public ProtocolVer NegotiatedVersion { get; }
        public bool IsCompatible => Status == ProtocolCompatStatus.Exact || Status == ProtocolCompatStatus.CompatibleDowngrade;
    }

    public static class ProtocolCompat
    {
        public static ProtocolCompatResult Resolve(ProtocolRange client, ProtocolRange server)
        {
            var overlapMin = client.MinSupported >= server.MinSupported ? client.MinSupported : server.MinSupported;
            var overlapMax = client.MaxSupported <= server.MaxSupported ? client.MaxSupported : server.MaxSupported;
            if (overlapMax < overlapMin)
            {
                if (client.MaxSupported < server.MinSupported)
                    return new ProtocolCompatResult(ProtocolCompatStatus.ClientTooOld, default);
                if (client.MinSupported > server.MaxSupported)
                    return new ProtocolCompatResult(ProtocolCompatStatus.ClientTooNew, default);
                return new ProtocolCompatResult(ProtocolCompatStatus.NoCommonVersion, default);
            }

            var negotiated = overlapMax;
            var status = negotiated == client.MaxSupported && negotiated == server.MaxSupported
                ? ProtocolCompatStatus.Exact
                : ProtocolCompatStatus.CompatibleDowngrade;
            return new ProtocolCompatResult(status, negotiated);
        }
    }

    public static class ProtocolProfile
    {
        public static readonly ProtocolVer Current =
            CreateVersion(ProtocolVersionInfo.CurrentYear, ProtocolVersionInfo.CurrentMonth, ProtocolVersionInfo.CurrentDay, ProtocolVersionInfo.CurrentRevision);

        public static readonly ProtocolRange ClientSupported = new ProtocolRange(
            CreateVersion(ProtocolVersionInfo.ClientMinYear, ProtocolVersionInfo.ClientMinMonth, ProtocolVersionInfo.ClientMinDay, ProtocolVersionInfo.ClientMinRevision),
            CreateVersion(ProtocolVersionInfo.ClientMaxYear, ProtocolVersionInfo.ClientMaxMonth, ProtocolVersionInfo.ClientMaxDay, ProtocolVersionInfo.ClientMaxRevision));

        public static readonly ProtocolRange ServerSupported = new ProtocolRange(
            CreateVersion(ProtocolVersionInfo.ServerMinYear, ProtocolVersionInfo.ServerMinMonth, ProtocolVersionInfo.ServerMinDay, ProtocolVersionInfo.ServerMinRevision),
            CreateVersion(ProtocolVersionInfo.ServerMaxYear, ProtocolVersionInfo.ServerMaxMonth, ProtocolVersionInfo.ServerMaxDay, ProtocolVersionInfo.ServerMaxRevision));

        private static ProtocolVer CreateVersion(ushort year, byte month, byte day, byte revision)
        {
            return new ProtocolVer(year, month, day, revision);
        }
    }
}
