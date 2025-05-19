using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace KOTHServer
{
    public class PlayerRepository
    {
        private readonly string _connectionString;

        public PlayerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Player GetPlayer(string playerId, string name)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var command = new MySqlCommand(
                    "SELECT * FROM players WHERE player_uid = @playerId OR (player_name = @name AND @name != '')",
                    connection);
                command.Parameters.AddWithValue("@playerId", playerId);
                command.Parameters.AddWithValue("@name", name ?? "");

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var player = new Player
                        {
                            PlayerId = reader.GetString("player_uid"),
                            Name = reader.GetString("player_name"),
                            ProfileName = reader.GetString("profile_name"),
                            PlatformName = reader.GetString("platform_name"),
                            MachineName = reader.GetString("machine_name"),
                            AdapterName = reader.GetString("adapter_name"),
                            Money = reader.GetInt32("money"),
                            Level = reader.GetInt32("level"),
                            XP = reader.GetInt32("xp"),
                            Kills = reader.GetInt32("kills"),
                            Deaths = reader.GetInt32("deaths"),
                            FriendlyKills = reader.GetInt32("friendly_kills"),
                            UnlockedItems = new List<string>(),
                            PlayerPresets = new List<string>()
                        };

                        // Загружаем unlockedItems
                        reader.Close();
                        var itemsCommand = new MySqlCommand(
                            "SELECT item_name FROM player_items WHERE player_uid = @playerId",
                            connection);
                        itemsCommand.Parameters.AddWithValue("@playerId", playerId);
                        using (var itemsReader = itemsCommand.ExecuteReader())
                        {
                            while (itemsReader.Read())
                            {
                                player.UnlockedItems.Add(itemsReader.GetString("item_name"));
                            }
                        }

                        return player;
                    }
                }
            }
            return null;
        }

        public Player CreateOrUpdatePlayer(Player player)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var command = new MySqlCommand(
                    @"INSERT INTO players (player_uid, player_name, profile_name, platform_name, machine_name, adapter_name, money, level, xp, kills, deaths, friendly_kills)
                      VALUES (@playerId, @name, @profileName, @platformName, @machineName, @adapterName, @money, @level, @xp, @kills, @deaths, @friendlyKills)
                      ON DUPLICATE KEY UPDATE
                      player_name = @name, 
                      profile_name = @profileName, 
                      platform_name = @platformName, 
                      machine_name = @machineName,
                      adapter_name = @adapterName, 
                      money = @money, 
                      level = @level, 
                      xp = @xp, 
                      kills = @kills, 
                      deaths = @deaths,
                      friendly_kills = @friendlyKills",
                    connection);
                command.Parameters.AddWithValue("@playerId", player.PlayerId);
                command.Parameters.AddWithValue("@name", player.Name ?? "");
                command.Parameters.AddWithValue("@profileName", player.ProfileName ?? "");
                command.Parameters.AddWithValue("@platformName", player.PlatformName ?? "");
                command.Parameters.AddWithValue("@machineName", player.MachineName ?? "");
                command.Parameters.AddWithValue("@adapterName", player.AdapterName ?? "");
                command.Parameters.AddWithValue("@money", player.Money);
                command.Parameters.AddWithValue("@level", player.Level);
                command.Parameters.AddWithValue("@xp", player.XP);
                command.Parameters.AddWithValue("@kills", player.Kills);
                command.Parameters.AddWithValue("@deaths", player.Deaths);
                command.Parameters.AddWithValue("@friendlyKills", player.FriendlyKills);
                command.ExecuteNonQuery();

                // Обновляем unlockedItems
                var deleteItemsCommand = new MySqlCommand(
                    "DELETE FROM player_items WHERE player_uid = @playerId",
                    connection);
                deleteItemsCommand.Parameters.AddWithValue("@playerId", player.PlayerId);
                deleteItemsCommand.ExecuteNonQuery();

                foreach (var item in player.UnlockedItems ?? new List<string>())
                {
                    var insertItemCommand = new MySqlCommand(
                        "INSERT INTO player_items (player_uid, item_name) VALUES (@playerId, @itemName)",
                        connection);
                    insertItemCommand.Parameters.AddWithValue("@playerId", player.PlayerId);
                    insertItemCommand.Parameters.AddWithValue("@itemName", item);
                    insertItemCommand.ExecuteNonQuery();
                }

                Console.WriteLine($"[REPO] Created/Updated player with playerId: {player.PlayerId}, Name: {player.Name}, ProfileName: {player.ProfileName}, PlatformName: {player.PlatformName}, MachineName: {player.MachineName}, AdapterName: {player.AdapterName}");
                return player;
            }
        }

        public void SavePreset(string playerId, Dictionary<string, object> presetData)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var command = new MySqlCommand(
                    @"INSERT INTO player_presets (player_uid, primary_weapon, secondary_weapon, optic_weapon, muzzle, launcher, throwable, range_finder, viperhood)
                      VALUES (@playerId, @primary, @secondary, @optic, @muzzle, @launcher, @throwable, @rangeFinder, @viperhood)
                      ON DUPLICATE KEY UPDATE
                      primary_weapon = @primary, secondary_weapon = @secondary, optic_weapon = @optic, muzzle = @muzzle,
                      launcher = @launcher, throwable = @throwable, range_finder = @rangeFinder, viperhood = @viperhood",
                    connection);
                command.Parameters.AddWithValue("@playerId", playerId);

                presetData.TryGetValue("m_primaryWeaponResource", out var primary);
                command.Parameters.AddWithValue("@primary", primary?.ToString() ?? "");

                presetData.TryGetValue("m_secondaryWeaponResource", out var secondary);
                command.Parameters.AddWithValue("@secondary", secondary?.ToString() ?? "");

                presetData.TryGetValue("m_opticWeaponResource", out var optic);
                command.Parameters.AddWithValue("@optic", optic?.ToString() ?? "");

                presetData.TryGetValue("m_muzzleResource", out var muzzle);
                command.Parameters.AddWithValue("@muzzle", muzzle?.ToString() ?? "");

                presetData.TryGetValue("m_launcherResource", out var launcher);
                command.Parameters.AddWithValue("@launcher", launcher?.ToString() ?? "");

                presetData.TryGetValue("m_throwableResource", out var throwable);
                command.Parameters.AddWithValue("@throwable", throwable != null ? JsonSerializer.Serialize(throwable) : "[]");

                presetData.TryGetValue("m_rangeFinderResource", out var rangeFinder);
                command.Parameters.AddWithValue("@rangeFinder", rangeFinder?.ToString() ?? "");

                presetData.TryGetValue("m_viperhoodResource", out var viperhood);
                command.Parameters.AddWithValue("@viperhood", viperhood?.ToString() ?? "");

                command.ExecuteNonQuery();

                Console.WriteLine($"[REPO] Saved preset for playerId: {playerId}");
            }
        }

        public List<Dictionary<string, object>> GetActiveBans()
        {
            var bans = new List<Dictionary<string, object>>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var command = new MySqlCommand("SELECT player_uid, ban_reason, ban_date FROM bans WHERE active = TRUE", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bans.Add(new Dictionary<string, object>
                        {
                            { "playerUID", reader.GetString("player_uid") },
                            { "banReason", reader.GetString("ban_reason") },
                            { "banDate", reader.GetDateTime("ban_date").ToString("yyyy-MM-dd HH:mm:ss") }
                        });
                    }
                }
            }
            return bans;
        }

        public void SaveStats(Dictionary<string, List<Dictionary<string, object>>> statsData)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var statsList = statsData["m_list"];
                foreach (var stats in statsList)
                {
                    if (!stats.TryGetValue("m_playerUID", out var playerUIDObj) || string.IsNullOrEmpty(playerUIDObj?.ToString()))
                    {
                        Console.WriteLine($"[REPO] Skipping stats with empty playerUID: {JsonSerializer.Serialize(stats)}");
                        continue;
                    }

                    string playerUID = playerUIDObj.ToString();
                    var command = new MySqlCommand(
                        @"INSERT INTO player_stats (
                            player_uid, bullets_shot, grenades_thrown, max_kill_streak, max_kill_distance,
                            insertion_bonus, kill_streak_x3, kill_streak_x5, kill_streak_x10, kill_streak_x20, kill_streak_x30
                          ) VALUES (
                            @playerUID, @bulletsShot, @grenadesThrown, @maxKillStreak, @maxKillDistance,
                            @insertionBonus, @killStreakX3, @killStreakX5, @killStreakX10, @killStreakX20, @killStreakX30
                          ) ON DUPLICATE KEY UPDATE
                            bullets_shot = bullets_shot + @bulletsShot,
                            grenades_thrown = grenades_thrown + @grenadesThrown,
                            max_kill_streak = GREATEST(max_kill_streak, @maxKillStreak),
                            max_kill_distance = GREATEST(max_kill_distance, @maxKillDistance),
                            insertion_bonus = insertion_bonus + @insertionBonus,
                            kill_streak_x3 = kill_streak_x3 + @killStreakX3,
                            kill_streak_x5 = kill_streak_x5 + @killStreakX5,
                            kill_streak_x10 = kill_streak_x10 + @killStreakX10,
                            kill_streak_x20 = kill_streak_x20 + @killStreakX20,
                            kill_streak_x30 = kill_streak_x30 + @killStreakX30",
                        connection);

                    command.Parameters.AddWithValue("@playerUID", playerUID);
                    command.Parameters.AddWithValue("@bulletsShot", stats.TryGetValue("m_bulletsShot", out var bullets) && int.TryParse(bullets?.ToString(), out var bs) ? bs : 0);
                    command.Parameters.AddWithValue("@grenadesThrown", stats.TryGetValue("m_grenadesThrown", out var grenades) && int.TryParse(grenades?.ToString(), out var gt) ? gt : 0);
                    command.Parameters.AddWithValue("@maxKillStreak", stats.TryGetValue("m_maxKillStreak", out var maxKS) && int.TryParse(maxKS?.ToString(), out var mks) ? mks : 0);
                    command.Parameters.AddWithValue("@maxKillDistance", stats.TryGetValue("m_maxKillDistance", out var maxKD) && int.TryParse(maxKD?.ToString(), out var mkd) ? mkd : 0);
                    command.Parameters.AddWithValue("@insertionBonus", stats.TryGetValue("m_insertionBonus", out var insBonus) && int.TryParse(insBonus?.ToString(), out var ib) ? ib : 0);
                    command.Parameters.AddWithValue("@killStreakX3", stats.TryGetValue("m_killStreakX3", out var ks3) && int.TryParse(ks3?.ToString(), out var k3) ? k3 : 0);
                    command.Parameters.AddWithValue("@killStreakX5", stats.TryGetValue("m_killStreakX5", out var ks5) && int.TryParse(ks5?.ToString(), out var k5) ? k5 : 0);
                    command.Parameters.AddWithValue("@killStreakX10", stats.TryGetValue("m_killStreakX10", out var ks10) && int.TryParse(ks10?.ToString(), out var k10) ? k10 : 0);
                    command.Parameters.AddWithValue("@killStreakX20", stats.TryGetValue("m_killStreakX20", out var ks20) && int.TryParse(ks20?.ToString(), out var k20) ? k20 : 0);
                    command.Parameters.AddWithValue("@killStreakX30", stats.TryGetValue("m_killStreakX30", out var ks30) && int.TryParse(ks30?.ToString(), out var k30) ? k30 : 0);

                    command.ExecuteNonQuery();
                    Console.WriteLine($"[REPO] Saved stats for playerUID: {playerUID}");
                }
            }
        }

        public PlayerStats GetPlayerStats(string playerUID)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var command = new MySqlCommand(
                    @"SELECT bullets_shot, grenades_thrown, max_kill_streak, max_kill_distance,
                             insertion_bonus, kill_streak_x3, kill_streak_x5, kill_streak_x10,
                             kill_streak_x20, kill_streak_x30
                      FROM player_stats
                      WHERE player_uid = @playerUID",
                    connection);
                command.Parameters.AddWithValue("@playerUID", playerUID);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new PlayerStats
                        {
                            PlayerUID = playerUID,
                            BulletsShot = reader.GetInt32("bullets_shot"),
                            GrenadesThrown = reader.GetInt32("grenades_thrown"),
                            MaxKillStreak = reader.GetInt32("max_kill_streak"),
                            MaxKillDistance = reader.GetInt32("max_kill_distance"),
                            InsertionBonus = reader.GetInt32("insertion_bonus"),
                            KillStreakX3 = reader.GetInt32("kill_streak_x3"),
                            KillStreakX5 = reader.GetInt32("kill_streak_x5"),
                            KillStreakX10 = reader.GetInt32("kill_streak_x10"),
                            KillStreakX20 = reader.GetInt32("kill_streak_x20"),
                            KillStreakX30 = reader.GetInt32("kill_streak_x30")
                        };
                    }
                }
            }
            return null;
        }

        public BonusCode GetPlayerBonus(string playerUID)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var command = new MySqlCommand(
                    @"SELECT bc.code, bc.name, bc.multiplier, bc.date_end
                      FROM bonus_codes bc
                      JOIN player_bonuses pb ON bc.code = pb.code
                      WHERE pb.player_uid = @playerUID AND bc.date_end > NOW() AND bc.uses < bc.max_uses",
                    connection);
                command.Parameters.AddWithValue("@playerUID", playerUID);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new BonusCode
                        {
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Multiplier = reader.GetDecimal("multiplier"),
                            DateEnd = reader.GetDateTime("date_end")
                        };
                    }
                }
            }
            return null;
        }

        public object ActivateBonusCode(string playerUID, string code)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                // Проверяю существование игрока
                var playerCheck = new MySqlCommand("SELECT COUNT(*) FROM players WHERE player_uid = @playerUID", connection);
                playerCheck.Parameters.AddWithValue("@playerUID", playerUID);
                long playerCount = (long)playerCheck.ExecuteScalar();
                if (playerCount == 0)
                {
                    return new { error = true, errorReason = "Player not found" };
                }

                // Проверяю валидность кода
                var codeCommand = new MySqlCommand(
                    "SELECT name, multiplier, date_end, max_uses, uses FROM bonus_codes WHERE code = @code",
                    connection);
                codeCommand.Parameters.AddWithValue("@code", code);
                using (var reader = codeCommand.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new { error = true, errorReason = "Invalid code" };
                    }

                    string name = reader.GetString("name");
                    decimal multiplier = reader.GetDecimal("multiplier");
                    DateTime dateEnd = reader.GetDateTime("date_end");
                    int maxUses = reader.GetInt32("max_uses");
                    int uses = reader.GetInt32("uses");

                    if (dateEnd < DateTime.Now)
                    {
                        return new { error = true, errorReason = "Code has expired" };
                    }

                    if (uses >= maxUses)
                    {
                        return new { error = true, errorReason = "Code usage limit reached" };
                    }
                }

                // Проверяю, не использовал ли игрок этот код
                var usageCheck = new MySqlCommand(
                    "SELECT COUNT(*) FROM player_bonuses WHERE player_uid = @playerUID AND code = @code",
                    connection);
                usageCheck.Parameters.AddWithValue("@playerUID", playerUID);
                usageCheck.Parameters.AddWithValue("@code", code);
                long usageCount = (long)usageCheck.ExecuteScalar();
                if (usageCount > 0)
                {
                    return new { error = true, errorReason = "Code already used by this player" };
                }

                // Активирую код, значит заслужил
                var insertCommand = new MySqlCommand(
                    "INSERT INTO player_bonuses (player_uid, code) VALUES (@playerUID, @code)",
                    connection);
                insertCommand.Parameters.AddWithValue("@playerUID", playerUID);
                insertCommand.Parameters.AddWithValue("@code", code);
                insertCommand.ExecuteNonQuery();

                // Увеличиваю счётчик использований, по хорошему это надо все в одном SQL запросе делать (ГовноКод)
                var updateCommand = new MySqlCommand(
                    "UPDATE bonus_codes SET uses = uses + 1 WHERE code = @code",
                    connection);
                updateCommand.Parameters.AddWithValue("@code", code);
                updateCommand.ExecuteNonQuery();

                // Получаю данные кода для ответа
                codeCommand = new MySqlCommand(
                    "SELECT name, multiplier, date_end FROM bonus_codes WHERE code = @code",
                    connection);
                codeCommand.Parameters.AddWithValue("@code", code);
                using (var reader = codeCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            name = reader.GetString("name"),
                            code = code,
                            playerUID = playerUID,
                            multiplier = reader.GetDecimal("multiplier").ToString("F1"),
                            dateEnd = reader.GetDateTime("date_end").ToString("yyyy-MM-dd HH:mm:ss"),
                            error = false,
                            errorReason = ""
                        };
                    }
                }

                return new { error = true, errorReason = "Failed to retrieve bonus details" };
            }
        }
    }

    public class BonusCode // Слишком мал чтобы быть в отдельном файле, посиди с большими некудышными дяденьками)
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Multiplier { get; set; }
        public DateTime DateEnd { get; set; }
    }
}