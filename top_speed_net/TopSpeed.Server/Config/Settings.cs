using System.Text.Json.Serialization;

namespace TopSpeed.Server.Config
{
    internal sealed class ServerSettings
    {
        public string Language { get; set; } = "en";
        public int Port { get; set; } = 28630;
        public int DiscoveryPort { get; set; } = 28631;
        public int MaxPlayers { get; set; } = 64;
        public string Motd { get; set; } = string.Empty;
        [JsonPropertyName("features")]
        public ServerFeaturesSettings Features { get; set; } = new ServerFeaturesSettings();
        [JsonPropertyName("moderation")]
        public ServerModerationSettings Moderation { get; set; } = new ServerModerationSettings();
        public string UpdateRuntimeAssetTag { get; set; } = "auto";
        public bool CheckForUpdatesOnStartup { get; set; }
    }
}
