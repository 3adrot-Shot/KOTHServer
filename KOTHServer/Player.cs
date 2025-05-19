using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KOTHServer
{
    public class Player // Успешно угнанна с Koth мода.
    {
        [JsonPropertyName("playerId")]
        public string PlayerId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("profileName")]
        public string ProfileName { get; set; }

        [JsonPropertyName("platformName")]
        public string PlatformName { get; set; }

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; }

        [JsonPropertyName("adapterName")]
        public string AdapterName { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("xp")]
        public int XP { get; set; }

        [JsonPropertyName("money")]
        public int Money { get; set; }

        [JsonPropertyName("kills")]
        public int Kills { get; set; }

        [JsonPropertyName("deaths")]
        public int Deaths { get; set; }

        [JsonPropertyName("friendlyKills")]
        public int FriendlyKills { get; set; }

        [JsonPropertyName("unlockedItems")]
        public List<string> UnlockedItems { get; set; } = new List<string>();

        [JsonPropertyName("playerPresets")]
        public List<string> PlayerPresets { get; set; } = new List<string>();
    }
}