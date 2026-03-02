namespace TopSpeed.Server.Network
{
    internal sealed class RaceServerConfig
    {
        public int Port { get; set; } = 28630;
        public int DiscoveryPort { get; set; } = 28631;
        public int MaxPlayers { get; set; } = 64;
        public string? Motd { get; set; }
    }
}
