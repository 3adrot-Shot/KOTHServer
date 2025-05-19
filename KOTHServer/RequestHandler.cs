using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace KOTHServer
{
    public class RequestHandler
    {
        private readonly PlayerRepository _repository;
        private const string API_KEY = Settings._apiKey; // Токен для авторизации в API, от чужих лап подальше
        private static readonly ConsoleColor[] Colors = new[]
        {
            // Цветовая палитра для логов, чтобы глаза не офигевали. Отвечает за смену цвета текста каждого отдельного запроса
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Yellow,
            ConsoleColor.Cyan,
            ConsoleColor.Magenta
        };
        private static int _requestCounter = 0;

        public RequestHandler(PlayerRepository repository)
        {
            _repository = repository;
        }

        public void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // Читаю тело запроса один раз
            string requestBody = null;
            if (request.HasEntityBody)
            {
                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    requestBody = reader.ReadToEnd();
                }
            }

            // Логирую запрос с телом
            LogRequest(request, requestBody);

            try
            {
                string authToken = request.Headers["X-AUTH-TOKEN"];
                if (authToken != API_KEY)
                {
                    SendResponse(response, HttpStatusCode.Unauthorized, new { status = "error", message = "Invalid API key" });
                    return;
                }

                string path = request.Url.AbsolutePath;
                Console.WriteLine($"[REQUEST] {request.HttpMethod} {path}");

                if (path == "/apiprofile")
                {
                    if (request.HttpMethod == "GET")
                        HandleGetProfile(context);
                    else if (request.HttpMethod == "POST")
                        HandlePostProfile(context, requestBody);
                    else
                        SendResponse(response, HttpStatusCode.MethodNotAllowed, new { status = "error", message = "Method not allowed" });
                }
                else if (path == "/apiprofiles" && request.HttpMethod == "POST")
                {
                    HandlePostProfiles(context, requestBody);
                }
                else if (path.StartsWith("/apipreset/") && request.HttpMethod == "POST")
                {
                    HandlePostPreset(context, requestBody);
                }
                else if (path == "/apistats/playersstats" && request.HttpMethod == "POST")
                {
                    HandlePostStats(context, requestBody);
                }
                else if (path == "/apistats/playerstats" && request.HttpMethod == "GET")
                {
                    HandleGetPlayerStats(context);
                }
                else if (path == "/apiactiveBans" && request.HttpMethod == "GET")
                {
                    HandleGetBans(context);
                }
                else if (path == "/apibonus" && request.HttpMethod == "GET")
                {
                    HandleGetBonus(context);
                }
                else if (path == "/apibonusCode" && request.HttpMethod == "POST")
                {
                    HandlePostBonusCode(context, requestBody);
                }
                else
                {
                    SendResponse(response, HttpStatusCode.NotFound, new { status = "error", message = $"Endpoint {path} not found" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HANDLER ERROR] {ex.Message}");
                SendResponse(response, HttpStatusCode.InternalServerError, new { status = "error", message = "Internal server error" });
            }
        }

        private void LogRequest(HttpListenerRequest request, string requestBody)
        {
            ConsoleColor color = Colors[_requestCounter % Colors.Length];
            Console.ForegroundColor = color;
            _requestCounter++;

            Console.WriteLine($"[REQUEST] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Method: {request.HttpMethod}");
            Console.WriteLine($"Path: {request.Url.PathAndQuery}");
            Console.WriteLine($"Content-Type: {request.ContentType}");
            Console.WriteLine("Headers:");
            foreach (string key in request.Headers.AllKeys)
            {
                Console.WriteLine($"  {key}: {request.Headers[key]}");
            }

            Console.WriteLine($"Body: {(string.IsNullOrEmpty(requestBody) ? "<empty>" : requestBody)}");
            Console.WriteLine();
            Console.ResetColor();
        }

        private void HandleGetProfile(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string bohemiaUID = request.QueryString["bohemiaUID"];
            string playerId = request.QueryString["playerId"];
            string name = request.QueryString["name"];
            string authPlayerId = request.Headers["X-AUTH-PLAYERID"];

            Console.WriteLine($"[PROFILE GET] bohemiaUID: {bohemiaUID}, playerId: {playerId}, name: {name}, authPlayerId: {authPlayerId}");

            string searchId = bohemiaUID ?? playerId ?? authPlayerId;
            var player = _repository.GetPlayer(searchId, name);

            if (player == null)
            {
                Console.WriteLine("[PROFILE GET] Player not found, creating default");
                // Пробник
                player = _repository.CreateOrUpdatePlayer(new Player
                {
                    PlayerId = searchId ?? $"temp_{Guid.NewGuid().ToString()}",
                    Name = name ?? "Unknown",
                    Level = 1,
                    XP = 0,
                    Money = 1000
                });
            }

            var profileResponse = new
            {
                m_playerUID = player.PlayerId,
                m_playerName = player.Name,
                m_profileName = player.ProfileName ?? "",
                m_platformName = player.PlatformName ?? "",
                m_machineName = player.MachineName ?? "",
                m_adapterName = player.AdapterName ?? "",
                m_money = player.Money,
                m_level = player.Level,
                m_xp = player.XP,
                m_kills = player.Kills,
                m_deaths = player.Deaths,
                m_friendlyKills = player.FriendlyKills,
                m_unlockedItems = player.UnlockedItems ?? new List<string>(),
                m_playerPresets = new List<object>()
            };

            Console.WriteLine($"[PROFILE GET] Returning: {JsonSerializer.Serialize(profileResponse)}");
            SendResponse(response, HttpStatusCode.OK, profileResponse);
        }

        private string FixPlayerNameEncoding(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
                return "Unknown";

            // Логируем исходный ник
            Console.WriteLine($"[ENCODING] Original playerName: {playerName}");

            // Убираю лишние символы, оставляю только кириллицу, латиницу, цифры и пробелы. Чистка от мусора
            playerName = Regex.Replace(playerName, @"[^\p{IsCyrillic}\p{IsBasicLatin}\p{N}\s]", "").Trim();

            if (string.IsNullOrEmpty(playerName))
                return "Unknown";

            // Логирую очищенный ник
            Console.WriteLine($"[ENCODING] Cleaned playerName: {playerName}");

            try
            {
                // Проверяю, содержит ли строка кириллицу
                bool isLikelyCP1251 = playerName.Any(c => c >= 0x0410 && c <= 0x044F);
                if (isLikelyCP1251)
                {
                    // Пробуем декодировать из CP1251 в UTF-8. Гребанная Bohemia
                    byte[] cp1251Bytes = Encoding.GetEncoding("windows-1251").GetBytes(playerName);
                    string decoded = Encoding.UTF8.GetString(cp1251Bytes);
                    if (!string.IsNullOrEmpty(decoded) && !decoded.Contains("�"))
                    {
                        Console.WriteLine($"[ENCODING] Decoded playerName: {decoded}");
                        return decoded;
                    }
                }
                // Если декодирование не нужно или не удалось, возвращаем очищенный ник. Чтобы не обосратся)
                Console.WriteLine($"[ENCODING] Using cleaned playerName: {playerName}");
                return playerName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ENCODING] Failed to decode playerName: {playerName}, Error: {ex.Message}");
                return playerName;
            }
        }

        private void HandlePostProfile(HttpListenerContext context, string requestBody)
        {
            var response = context.Response;

            Console.WriteLine($"[PROFILE POST] Received: {requestBody}");

            try
            {
                var formData = HttpUtility.ParseQueryString(requestBody ?? "");
                string content = formData["content"];
                if (string.IsNullOrEmpty(content))
                {
                    SendResponse(response, HttpStatusCode.BadRequest, new { status = "error", message = "Content is required" });
                    return;
                }

                var profileData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                var newPlayer = new Player();

                if (profileData.TryGetValue("m_playerUID", out var playerUIDObj))
                    newPlayer.PlayerId = playerUIDObj?.ToString() ?? "";
                else
                    newPlayer.PlayerId = "";

                if (profileData.TryGetValue("m_playerName", out var playerNameObj))
                    newPlayer.Name = FixPlayerNameEncoding(playerNameObj?.ToString() ?? "");
                else
                    newPlayer.Name = "Unknown";

                if (profileData.TryGetValue("m_profileName", out var profileNameObj))
                    newPlayer.ProfileName = profileNameObj?.ToString() ?? "";
                else
                    newPlayer.ProfileName = "";

                if (profileData.TryGetValue("m_platformName", out var platformNameObj))
                    newPlayer.PlatformName = platformNameObj?.ToString() ?? "";
                else
                    newPlayer.PlatformName = "";

                if (profileData.TryGetValue("m_machineName", out var machineNameObj))
                    newPlayer.MachineName = machineNameObj?.ToString() ?? "";
                else
                    newPlayer.MachineName = "";

                if (profileData.TryGetValue("m_adapterName", out var adapterNameObj))
                    newPlayer.AdapterName = adapterNameObj?.ToString() ?? "";
                else
                    newPlayer.AdapterName = "";

                if (profileData.TryGetValue("m_money", out var moneyObj) && int.TryParse(moneyObj?.ToString(), out var money))
                    newPlayer.Money = money;
                else
                    newPlayer.Money = 0;

                if (profileData.TryGetValue("m_level", out var levelObj) && int.TryParse(levelObj?.ToString(), out var level))
                    newPlayer.Level = level;
                else
                    newPlayer.Level = 1;

                if (profileData.TryGetValue("m_xp", out var xpObj) && int.TryParse(xpObj?.ToString(), out var xp))
                    newPlayer.XP = xp;
                else
                    newPlayer.XP = 0;

                if (profileData.TryGetValue("m_kills", out var killsObj) && int.TryParse(killsObj?.ToString(), out var kills))
                    newPlayer.Kills = kills;
                else
                    newPlayer.Kills = 0;

                if (profileData.TryGetValue("m_deaths", out var deathsObj) && int.TryParse(deathsObj?.ToString(), out var deaths))
                    newPlayer.Deaths = deaths;
                else
                    newPlayer.Deaths = 0;

                if (profileData.TryGetValue("m_friendlyKills", out var friendlyKillsObj) && int.TryParse(friendlyKillsObj?.ToString(), out var friendlyKills))
                    newPlayer.FriendlyKills = friendlyKills;
                else
                    newPlayer.FriendlyKills = 0;

                if (profileData.TryGetValue("m_unlockedItems", out var unlockedItemsObj))
                    newPlayer.UnlockedItems = JsonSerializer.Deserialize<List<string>>(unlockedItemsObj?.ToString() ?? "[]");
                else
                    newPlayer.UnlockedItems = new List<string>();

                if (profileData.TryGetValue("m_playerPresets", out var playerPresetsObj))
                    newPlayer.PlayerPresets = JsonSerializer.Deserialize<List<string>>(playerPresetsObj?.ToString() ?? "[]");
                else
                    newPlayer.PlayerPresets = new List<string>();

                var player = _repository.CreateOrUpdatePlayer(newPlayer);

                var profileResponse = new
                {
                    m_playerUID = player.PlayerId,
                    m_playerName = player.Name,
                    m_profileName = player.ProfileName ?? "",
                    m_platformName = player.PlatformName ?? "",
                    m_machineName = player.MachineName ?? "",
                    m_adapterName = player.AdapterName ?? "",
                    m_money = player.Money,
                    m_level = player.Level,
                    m_xp = player.XP,
                    m_kills = player.Kills,
                    m_deaths = player.Deaths,
                    m_friendlyKills = player.FriendlyKills,
                    m_unlockedItems = player.UnlockedItems ?? new List<string>(),
                    m_playerPresets = new List<object>()
                };

                Console.WriteLine($"[PROFILE POST] Returning: {JsonSerializer.Serialize(profileResponse)}");
                SendResponse(response, HttpStatusCode.OK, profileResponse);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[PROFILE POST ERROR] Invalid JSON: {ex.Message}"); // Бисмиллях 
                SendResponse(response, HttpStatusCode.BadRequest, new { status = "error", message = "Invalid profile format" });
            }
        }

        private void HandlePostProfiles(HttpListenerContext context, string requestBody)
        {
            var response = context.Response;

            Console.WriteLine($"[PROFILES POST] Received: {requestBody}");

            try
            {
                var formData = HttpUtility.ParseQueryString(requestBody ?? "");
                string content = formData["content"];
                if (string.IsNullOrEmpty(content))
                {
                    SendResponse(response, HttpStatusCode.BadRequest, new { status = "error", message = "Content is required" });
                    return;
                }

                var data = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(content);
                var profiles = data["m_list"];
                var updatedProfiles = new List<object>();

                foreach (var profileData in profiles)
                {
                    if (!profileData.TryGetValue("m_playerUID", out var playerUIDObj) || string.IsNullOrEmpty(playerUIDObj?.ToString()))
                    {
                        Console.WriteLine($"[PROFILES POST] Skipping empty profile: {JsonSerializer.Serialize(profileData)}");
                        continue; // Пропускаем пустые профили
                    }

                    var newPlayer = new Player();

                    newPlayer.PlayerId = playerUIDObj.ToString();
                    newPlayer.Name = profileData.TryGetValue("m_playerName", out var playerNameObj) ? FixPlayerNameEncoding(playerNameObj?.ToString() ?? "") : "Unknown";
                    newPlayer.ProfileName = profileData.TryGetValue("m_profileName", out var profileNameObj) ? profileNameObj?.ToString() ?? "" : "";
                    newPlayer.PlatformName = profileData.TryGetValue("m_platformName", out var platformNameObj) ? platformNameObj?.ToString() ?? "" : "";
                    newPlayer.MachineName = profileData.TryGetValue("m_machineName", out var machineNameObj) ? machineNameObj?.ToString() ?? "" : "";
                    newPlayer.AdapterName = profileData.TryGetValue("m_adapterName", out var adapterNameObj) ? adapterNameObj?.ToString() ?? "" : "";
                    newPlayer.Money = profileData.TryGetValue("m_money", out var moneyObj) && int.TryParse(moneyObj?.ToString(), out var money) ? money : 0;
                    newPlayer.Level = profileData.TryGetValue("m_level", out var levelObj) && int.TryParse(levelObj?.ToString(), out var level) ? level : 1;
                    newPlayer.XP = profileData.TryGetValue("m_xp", out var xpObj) && int.TryParse(xpObj?.ToString(), out var xp) ? xp : 0;
                    newPlayer.Kills = profileData.TryGetValue("m_kills", out var killsObj) && int.TryParse(killsObj?.ToString(), out var kills) ? kills : 0;
                    newPlayer.Deaths = profileData.TryGetValue("m_deaths", out var deathsObj) && int.TryParse(deathsObj?.ToString(), out var deaths) ? deaths : 0;
                    newPlayer.FriendlyKills = profileData.TryGetValue("m_friendlyKills", out var friendlyKillsObj) && int.TryParse(friendlyKillsObj?.ToString(), out var friendlyKills) ? friendlyKills : 0;
                    newPlayer.UnlockedItems = profileData.TryGetValue("m_unlockedItems", out var unlockedItemsObj)
                        ? JsonSerializer.Deserialize<List<string>>(unlockedItemsObj?.ToString() ?? "[]")
                        : new List<string>();
                    newPlayer.PlayerPresets = profileData.TryGetValue("m_playerPresets", out var playerPresetsObj)
                        ? JsonSerializer.Deserialize<List<string>>(playerPresetsObj?.ToString() ?? "[]")
                        : new List<string>();

                    var player = _repository.CreateOrUpdatePlayer(newPlayer);
                    updatedProfiles.Add(new
                    {
                        m_playerUID = player.PlayerId,
                        m_playerName = player.Name,
                        m_profileName = player.ProfileName ?? "",
                        m_platformName = player.PlatformName ?? "",
                        m_machineName = player.MachineName ?? "",
                        m_adapterName = player.AdapterName ?? "",
                        m_money = player.Money,
                        m_level = player.Level,
                        m_xp = player.XP,
                        m_kills = player.Kills,
                        m_deaths = player.Deaths,
                        m_friendlyKills = player.FriendlyKills,
                        m_unlockedItems = player.UnlockedItems ?? new List<string>(),
                        m_playerPresets = new List<object>()
                    });
                }

                var responseData = new { m_list = updatedProfiles };
                Console.WriteLine($"[PROFILES POST] Returning: {JsonSerializer.Serialize(responseData)}");
                SendResponse(response, HttpStatusCode.OK, responseData);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[PROFILES POST ERROR] Invalid JSON: {ex.Message}"); // Бисмиллях 
                SendResponse(response, HttpStatusCode.BadRequest, new { status = "error", message = "Invalid profiles format" });
            }
        }

        private void HandlePostPreset(HttpListenerContext context, string requestBody)
        {
            var response = context.Response;
            string playerId = context.Request.Url.AbsolutePath.Replace("/apipreset/", "");

            Console.WriteLine($"[PRESET POST] Received for playerId: {playerId}, body: {requestBody}");

            try
            {
                var formData = HttpUtility.ParseQueryString(requestBody ?? "");
                string content = formData["content"];
                if (string.IsNullOrEmpty(content))
                {
                    SendResponse(response, HttpStatusCode.BadRequest, new { status = "error", message = "Content is required" });
                    return;
                }

                var presetData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                _repository.SavePreset(playerId, presetData);

                var responseData = new { status = "success", message = "Preset saved" };
                Console.WriteLine($"[PRESET POST] Returning: {JsonSerializer.Serialize(responseData)}");
                SendResponse(response, HttpStatusCode.OK, responseData);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[PRESET POST ERROR] Invalid JSON: {ex.Message}");
                SendResponse(response, HttpStatusCode.BadRequest, new { status = "error", message = "Invalid preset format" });
            }
        }

        private void HandlePostStats(HttpListenerContext context, string requestBody)
        {
            var response = context.Response;

            Console.WriteLine($"[STATS POST] Received: {requestBody}");

            try
            {
                var formData = HttpUtility.ParseQueryString(requestBody ?? "");
                string content = formData["content"];
                if (string.IsNullOrEmpty(content))
                {
                    SendResponse(response, HttpStatusCode.BadRequest, new { status = "error", message = "Content is required" });
                    return;
                }

                var statsData = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(content);
                _repository.SaveStats(statsData);

                var responseData = new { status = "success", message = "Stats received" };
                Console.WriteLine($"[STATS POST] Returning: {JsonSerializer.Serialize(responseData)}");
                SendResponse(response, HttpStatusCode.OK, responseData);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[STATS POST ERROR] Invalid JSON: {ex.Message}");
                SendResponse(response, HttpStatusCode.BadRequest, new { status = "error", message = "Invalid stats format" });
            }
        }

        private void HandleGetPlayerStats(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string bohemiaUID = request.QueryString["bohemiaUID"];
            Console.WriteLine($"[PLAYER STATS GET] bohemiaUID: {bohemiaUID}");

            if (string.IsNullOrEmpty(bohemiaUID))
            {
                SendResponse(response, HttpStatusCode.BadRequest, new { playerUID = "", stats = "", error = true, errorReason = "bohemiaUID is required" });
                return;
            }

            var stats = _repository.GetPlayerStats(bohemiaUID);
            if (stats == null)
            {
                SendResponse(response, HttpStatusCode.OK, new
                {
                    playerUID = bohemiaUID,
                    stats = "",
                    error = true,
                    errorReason = "No stats found for player"
                });
                return;
            }

            var statsJson = new
            {
                m_playerUID = stats.PlayerUID,
                m_bulletsShot = stats.BulletsShot,
                m_grenadesThrown = stats.GrenadesThrown,
                m_maxKillStreak = stats.MaxKillStreak,
                m_maxKillDistance = stats.MaxKillDistance,
                m_insertionBonus = stats.InsertionBonus,
                m_killStreakX3 = stats.KillStreakX3,
                m_killStreakX5 = stats.KillStreakX5,
                m_killStreakX10 = stats.KillStreakX10,
                m_killStreakX20 = stats.KillStreakX20,
                m_killStreakX30 = stats.KillStreakX30
            };

            var responseData = new
            {
                playerUID = bohemiaUID,
                stats = JsonSerializer.Serialize(statsJson),
                error = false,
                errorReason = ""
            };

            Console.WriteLine($"[PLAYER STATS GET] Returning: {JsonSerializer.Serialize(responseData)}");
            SendResponse(response, HttpStatusCode.OK, responseData);
        }

        private void HandleGetBans(HttpListenerContext context)
        {
            var response = context.Response;
            var bans = _repository.GetActiveBans();
            var responseData = new { m_list = bans };
            Console.WriteLine($"[BANS GET] Returning: {JsonSerializer.Serialize(responseData)}");
            SendResponse(response, HttpStatusCode.OK, responseData);
        }

        private void HandleGetBonus(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string bohemiaUID = request.QueryString["bohemiaUID"];
            Console.WriteLine($"[BONUS GET] bohemiaUID: {bohemiaUID}");

            if (string.IsNullOrEmpty(bohemiaUID))
            {
                SendResponse(response, HttpStatusCode.BadRequest, new { error = true, errorReason = "bohemiaUID is required" });
                return;
            }

            var bonus = _repository.GetPlayerBonus(bohemiaUID);
            if (bonus == null)
            {
                SendResponse(response, HttpStatusCode.OK, new
                {
                    error = true,
                    errorReason = "No active bonus found"
                });
                return;
            }

            var bonusResponse = new
            {
                name = bonus.Name,
                code = bonus.Code,
                playerUID = bohemiaUID,
                multiplier = bonus.Multiplier.ToString("F1"),
                dateEnd = bonus.DateEnd.ToString("yyyy-MM-dd HH:mm:ss"),
                error = false,
                errorReason = ""
            };

            Console.WriteLine($"[BONUS GET] Returning: {JsonSerializer.Serialize(bonusResponse)}");
            SendResponse(response, HttpStatusCode.OK, bonusResponse);
        }

        private void HandlePostBonusCode(HttpListenerContext context, string requestBody)
        {
            var response = context.Response;

            Console.WriteLine($"[BONUS POST] Received: {requestBody}");

            try
            {
                var formData = HttpUtility.ParseQueryString(requestBody ?? "");
                string content = formData["content"];
                if (string.IsNullOrEmpty(content))
                {
                    SendResponse(response, HttpStatusCode.BadRequest, new { error = true, errorReason = "Content is required" });
                    return;
                }

                var bonusData = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                if (!bonusData.TryGetValue("code", out var code) || string.IsNullOrEmpty(code) ||
                    !bonusData.TryGetValue("playerUID", out var playerUID) || string.IsNullOrEmpty(playerUID))
                {
                    SendResponse(response, HttpStatusCode.BadRequest, new { error = true, errorReason = "Code and playerUID are required" });
                    return;
                }

                var result = _repository.ActivateBonusCode(playerUID, code);
                Console.WriteLine($"[BONUS POST] Returning: {JsonSerializer.Serialize(result)}");
                SendResponse(response, HttpStatusCode.OK, result);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[BONUS POST ERROR] Invalid JSON: {ex.Message}");
                SendResponse(response, HttpStatusCode.BadRequest, new { error = true, errorReason = "Invalid bonus code format" });
            }
        }

        private void SendResponse(HttpListenerResponse response, HttpStatusCode statusCode, object data)
        {
            response.StatusCode = (int)statusCode;
            response.ContentType = "application/json; charset=utf-8";
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}

// Требуется рефакторинг, но так лень. Работает и хорошо)