using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GSheetsCommander;
using UnityEngine;
using HajjFlow.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HajjFlow.Services
{
    public class CreateUserResponse
    {
        [JsonProperty("row")]
        public int Row { get; set; }
    
        [JsonProperty("user")]
        public UserData User { get; set; }
        
        [JsonProperty("progress")]
        public ProgressData [] Progress { get; set; }
    }
    
    public class ProgressData
    {
        [JsonProperty("level")]
        public string level { get; set; }
    
        [JsonProperty("percent")]
        public string percent { get; set; }
    }
    
    public class UserData
    {
        [JsonProperty("UserId")]
        public string UserId { get; set; }
    
        [JsonProperty("PilgrimNumber")]
        public string PilgrimNumber { get; set; }
    
        [JsonProperty("FullName")]
        public string FullName { get; set; }
    
        [JsonProperty("GroupId")]
        public string GroupId { get; set; }
    
        [JsonProperty("CreatedAt")]
        public string CreatedAt { get; set; }
    
        [JsonProperty("UpdatedAt")]
        public string UpdatedAt { get; set; }
    
        [JsonProperty("Status")]
        public string Status { get; set; }
        
        /// <summary>
        /// LevelResult может быть либо:
        /// 1. Массивом объектов: [{LevelId, ScorePercent, CompletedAt}, ...]
        /// 2. JSON-строкой внутри строки: "{level_0={...}, level_1={...}}"
        /// Используем custom converter для обработки обоих случаев.
        /// </summary>
        [JsonProperty("LevelResult")]
        [JsonConverter(typeof(LevelResultConverter))]
        public LevelResult[] LevelResult { get; set; }
    }
    
    /// <summary>
    /// Custom JsonConverter для обработки LevelResult в разных форматах.
    /// Конвертирует как массивы объектов, так и строки вида "{level_0={...}, level_1={...}}"
    /// </summary>
    public class LevelResultConverter : JsonConverter<LevelResult[]>
    {
        public override LevelResult[] ReadJson(JsonReader reader, Type objectType, LevelResult[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            
            // Если это null, возвращаем пустой массив
            if (token.Type == JTokenType.Null)
                return Array.Empty<LevelResult>();
            
            // Случай 1: Это строка (JSON внутри строки)
            if (token.Type == JTokenType.String)
            {
                return ParseLevelResultFromString(token.Value<string>());
            }
            
            // Случай 2: Это уже объект или массив
            if (token.Type == JTokenType.Object)
            {
                var obj = token as JObject;
                return ParseLevelResultFromObject(obj);
            }
            
            if (token.Type == JTokenType.Array)
            {
                var array = token as JArray;
                return array.Select(item => item.ToObject<LevelResult>(serializer)).ToArray();
            }
            
            return Array.Empty<LevelResult>();
        }
        
        public override void WriteJson(JsonWriter writer, LevelResult[] value, JsonSerializer serializer)
        {
            // При сохранении преобразуем в простой массив объектов
            serializer.Serialize(writer, value);
        }
        
        /// <summary>
        /// Парсит LevelResult из строки вида: "{level_3={...}, level_0={...}, level_2={...}}"
        /// </summary>
        private LevelResult[] ParseLevelResultFromString(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return Array.Empty<LevelResult>();
            
            var results = new List<LevelResult>();
            
            try
            {
                // Пытаемся распарсить как обычный JSON массив первым дел��м
                if (jsonString.TrimStart().StartsWith("["))
                {
                    return JsonConvert.DeserializeObject<LevelResult[]>(jsonString) ?? Array.Empty<LevelResult>();
                }
                
                // Иначе пытаемся распарсить объект вида {key1=obj1, key2=obj2, ...}
                // Преобразуем в формат JSON
                var normalized = NormalizeCSharpDictionary(jsonString);
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(normalized);
                
                if (parsed != null)
                {
                    foreach (var kvp in parsed)
                    {
                        var levelResult = ConvertToLevelResult(kvp.Key, kvp.Value);
                        if (levelResult != null)
                            results.Add(levelResult);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelResultConverter] Failed to parse LevelResult string: {ex.Message}\nString: {jsonString}");
            }
            
            return results.ToArray();
        }
        
        /// <summary>
        /// Парсит LevelResult из JObject.
        /// Ожидает структуру вида: {level_id1: {...}, level_id2: {...}}
        /// </summary>
        private LevelResult[] ParseLevelResultFromObject(JObject obj)
        {
            if (obj == null)
                return Array.Empty<LevelResult>();
            
            var results = new List<LevelResult>();
            
            foreach (var property in obj.Properties())
            {
                var levelResult = ConvertToLevelResult(property.Name, property.Value);
                if (levelResult != null)
                    results.Add(levelResult);
            }
            
            return results.ToArray();
        }
        
        /// <summary>
        /// Конвертирует пару (levelId, data) в LevelResult.
        /// </summary>
        private LevelResult ConvertToLevelResult(string levelId, object data)
        {
            try
            {
                var levelResult = new LevelResult { LevelId = levelId };
                
                if (data is JObject jObj)
                {
                    // Пытаемся извлечь ScorePercent
                    if (jObj.TryGetValue("ScorePercent", StringComparison.OrdinalIgnoreCase, out var scoreToken))
                    {
                        if (float.TryParse(scoreToken.Value<string>(), out var score))
                            levelResult.ScorePercent = score;
                    }
                    
                    // Пытаемся извлечь CompletedAt
                    if (jObj.TryGetValue("CompletedAt", StringComparison.OrdinalIgnoreCase, out var dateToken))
                    {
                        if (DateTime.TryParse(dateToken.Value<string>(), out var date))
                            levelResult.CompletedAt = date;
                    }
                }
                else if (data is string dataStr)
                {
                    // Если это строка, пытаемся распарсить её как JSON
                    try
                    {
                        var innerObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                        if (innerObj != null && innerObj.TryGetValue("ScorePercent", out var score))
                        {
                            if (float.TryParse(score.ToString(), out var scoreVal))
                                levelResult.ScorePercent = scoreVal;
                        }
                    }
                    catch { }
                }
                
                return levelResult;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelResultConverter] Failed to convert to LevelResult: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Нормализует C# Dictionary строку в JSON объект.
        /// Преобразует: {key1=value1, key2=value2} → {"key1": "value1", "key2": "value2"}
        /// </summary>
        private string NormalizeCSharpDictionary(string input)
        {
            // Убираем внешние скобки
            input = input.Trim();
            if (input.StartsWith("{") && input.EndsWith("}"))
                input = input.Substring(1, input.Length - 2);
            
            // Простой парсинг пар key=value
            var parts = new List<string>();
            var currentPart = "";
            var depth = 0;
            
            foreach (var ch in input)
            {
                if (ch == '{' || ch == '[')
                    depth++;
                else if (ch == '}' || ch == ']')
                    depth--;
                else if (ch == ',' && depth == 0)
                {
                    if (!string.IsNullOrWhiteSpace(currentPart))
                        parts.Add(currentPart.Trim());
                    currentPart = "";
                    continue;
                }
                
                currentPart += ch;
            }
            
            if (!string.IsNullOrWhiteSpace(currentPart))
                parts.Add(currentPart.Trim());
            
            // Преобразуем каждую пару
            var jsonParts = new List<string>();
            foreach (var part in parts)
            {
                var eqIndex = part.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = part.Substring(0, eqIndex).Trim();
                    var value = part.Substring(eqIndex + 1).Trim();
                    
                    // Если value начинается с { или [, считаем его объектом/массивом
                    if ((value.StartsWith("{") || value.StartsWith("[")) && 
                        (value.EndsWith("}") || value.EndsWith("]")))
                    {
                        jsonParts.Add($"\"{key}\": {value}");
                    }
                    else
                    {
                        // Иначе заключаем в кавычки
                        jsonParts.Add($"\"{key}\": \"{value}\"");
                    }
                }
            }
            
            return "{" + string.Join(", ", jsonParts) + "}";
        }
    }
    
    /// <summary>
    /// Handles loading and saving the UserProfile.
    /// Uses ProfileLoaderService for multi-source persistence (PlayerPrefs + File + Backend).
    /// </summary>
    public class UserProfileService
    {
        private static readonly string SaveFileName = "user_profile.json";
        private static readonly string PlayerPrefsKey = "UserProfile_Data";

        // Full path on the device's persistent data directory
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // Cached profile instance
        private UserProfile _profile;

        // Loader service for advanced scenarios
        private ProfileLoaderService _loaderService;

        /// <summary>
        /// Optionally inject ProfileLoaderService for full multi-provider support.
        /// </summary>
        public void SetLoaderService(ProfileLoaderService loaderService)
        {
            _loaderService = loaderService;
        }

        /// <summary>Returns the current in-memory profile, loading it if necessary.</summary>
        public UserProfile GetProfile()
        {
            if (_profile == null)
                _profile = Load();
            return _profile;
        }

        /// <summary>Persists the current profile to disk AND PlayerPrefs.</summary>
        public void Save()
        {
            // Если профиль не загружен - загружаем
            if (_profile == null)
            {
                _profile = Load();
            }
            
            try
            {
                string json = JsonUtility.ToJson(_profile, prettyPrint: true);
                
                // Save to File
                File.WriteAllText(SavePath, json);
                Debug.Log($"[UserProfileService] Profile saved to {SavePath}");
                
                // Save to PlayerPrefs
                PlayerPrefs.SetString(PlayerPrefsKey, json);
                PlayerPrefs.Save();
                Debug.Log("[UserProfileService] Profile saved to PlayerPrefs");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserProfileService] Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Асинхронно сохраняет профиль во все источники включая бекенд.
        /// </summary>
        public async Task SaveAsync()
        {
            if (_loaderService != null)
            {
                await _loaderService.SaveAsync(_profile);
            }
            else
            {
                Save();
            }
        }

        /// <summary>
        /// Updates profile fields and immediately persists.
        /// Accepts an action delegate so callers can modify the profile in-place.
        /// </summary>
        public void UpdateProfile(Action<UserProfile> updateAction)
        {
            updateAction?.Invoke(GetProfile());
            Save();
        }

        /// <summary>
        /// Асинхронно обновляет профиль и сохраняет во все источники.
        /// </summary>
        public async Task UpdateProfileAsync(Action<UserProfile> updateAction)
        {
            updateAction?.Invoke(GetProfile());
            await SaveAsync();
        }

        /// <summary>
        /// Включает бекенд для синхронизации.
        /// </summary>
        public void EnableBackend(string apiUrl, string userId)
        {
            if (_loaderService == null)
            {
                _loaderService = new ProfileLoaderService();
            }
            _loaderService.EnableBackend(apiUrl, userId);
        }

        /// <summary>
        /// Включает Google Sheets для загрузки/сохранения профиля пользователя.
        /// </summary>
        public void EnableGoogleSheets(GoogleSheetsConfig config, string username, string groupName)
        {
            if (_loaderService == null)
            {
                _loaderService = new ProfileLoaderService();
            }
            _loaderService.EnableGoogleSheets(config, username, groupName);
            Debug.Log($"[UserProfileService] Google Sheets enabled for user '{username}'");
        }

        /// <summary>
        /// Синхронизирует данные с бекендом.
        /// </summary>
        public async Task SyncWithBackendAsync()
        {
            if (_loaderService != null)
            {
                await _loaderService.SyncWithBackendAsync();
                _profile = _loaderService.GetProfile();
            }
        }

        /// <summary>
        /// Загружает профиль пользователя из Google Sheets.
        /// </summary>
        public async Task LoadFromGoogleSheetsAsync()
        {
            if (_loaderService == null)
            {
                Debug.LogWarning("[UserProfileService] ProfileLoaderService not initialized. Initialize with EnableGoogleSheets first.");
                return;
            }

            // This method is called after EnableGoogleSheets. It deliberately
            // bypasses any PlayerPrefs cache until Google Sheets has responded.
            var profile = await _loaderService.LoadFromGoogleSheetsAsync();
            if (profile != null)
            {
                _profile = profile;
                Debug.Log("[UserProfileService] Profile loaded from Google Sheets successfully");
            }
            else
            {
                Debug.LogWarning("[UserProfileService] Failed to load profile from Google Sheets");
            }
        }

        /// <summary>
        /// Сохраняет профиль пользователя в Google Sheets.
        /// </summary>
        public async Task SaveToGoogleSheetsAsync()
        {
            if (_loaderService == null)
            {
                Debug.LogWarning("[UserProfileService] ProfileLoaderService not initialized. Initialize with EnableGoogleSheets first.");
                return;
            }

            var profile = GetProfile();
            await _loaderService.SaveAsync(profile);
            Debug.Log("[UserProfileService] Profile saved to Google Sheets successfully");
        }

        // ── Private ─────────────────────────────────────────────────────────────

        private UserProfile Load()
        {
            // Priority: PlayerPrefs -> File -> Default
            
            // Try PlayerPrefs first (faster)
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PlayerPrefsKey);
                    var profile = JsonUtility.FromJson<UserProfile>(json);
                    Debug.Log("[UserProfileService] Profile loaded from PlayerPrefs.");
                    return profile;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UserProfileService] PlayerPrefs load failed: {ex.Message}");
                }
            }
            
            // Try File
            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    var profile = JsonUtility.FromJson<UserProfile>(json);
                    Debug.Log("[UserProfileService] Profile loaded from disk.");
                    
                    // Sync to PlayerPrefs
                    PlayerPrefs.SetString(PlayerPrefsKey, json);
                    PlayerPrefs.Save();
                    
                    return profile;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UserProfileService] Disk load failed: {ex.Message}");
                }
            }

            // Return a fresh profile on first run
            Debug.Log("[UserProfileService] No save file found — creating default profile.");
            return new UserProfile();
        }

        /// <summary>
        /// Очищает все сохранённые данные профиля.
        /// </summary>
        public void ClearAllData()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
            
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
            
            _profile = null;
            Debug.Log("[UserProfileService] All profile data cleared");
        }

        public void ResetProgress()
        { 
            var profile = GetProfile();
            profile.ResetProgress();
            Save();
        }
    }
}
