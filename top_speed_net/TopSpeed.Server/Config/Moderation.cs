using System.Text.Json.Serialization;

namespace TopSpeed.Server.Config
{
    internal sealed class ServerModerationSettings
    {
        [JsonPropertyName("block_repeated_letters_in_name")]
        public bool BlockRepeatedLettersInName { get; set; } = true;

        [JsonPropertyName("max_name_length")]
        public int MaxNameLength { get; set; } = 40;

        [JsonPropertyName("allow_duplicate_names")]
        public bool AllowDuplicateNames { get; set; } = true;

        public ServerModerationSettings Clone()
        {
            return new ServerModerationSettings
            {
                BlockRepeatedLettersInName = BlockRepeatedLettersInName,
                MaxNameLength = MaxNameLength,
                AllowDuplicateNames = AllowDuplicateNames
            };
        }
    }
}
