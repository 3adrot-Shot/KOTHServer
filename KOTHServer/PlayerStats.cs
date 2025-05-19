using System.Text.Json.Serialization;

namespace KOTHServer
{
    public class PlayerStats // Стругура не успешно угнанная с Koth))) Не функцианирует в общем.
    {
        [JsonPropertyName("m_playerUID")]
        public string PlayerUID { get; set; }

        [JsonPropertyName("m_bulletsShot")]
        public int BulletsShot { get; set; }

        [JsonPropertyName("m_grenadesThrown")]
        public int GrenadesThrown { get; set; }

        [JsonPropertyName("m_maxKillStreak")]
        public int MaxKillStreak { get; set; }

        [JsonPropertyName("m_maxKillDistance")]
        public int MaxKillDistance { get; set; }

        [JsonPropertyName("m_insertionBonus")]
        public int InsertionBonus { get; set; }

        [JsonPropertyName("m_killStreakX3")]
        public int KillStreakX3 { get; set; }

        [JsonPropertyName("m_killStreakX5")]
        public int KillStreakX5 { get; set; }

        [JsonPropertyName("m_killStreakX10")]
        public int KillStreakX10 { get; set; }

        [JsonPropertyName("m_killStreakX20")]
        public int KillStreakX20 { get; set; }

        [JsonPropertyName("m_killStreakX30")]
        public int KillStreakX30 { get; set; }
    }
}