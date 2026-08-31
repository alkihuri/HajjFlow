using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GSheetsCommander;
using HajjFlow.Core;
using HajjFlow.Data;
using HajjFlow.Services;
using UnityEngine;

namespace HajjFlow.Services
{
    /// <summary>
    /// Провайдер профиля пользователя из Google Sheets.
    /// Интегрируется с RegistrationService для загрузки/сохранения данных.
    /// 
    /// Архитектура:
    /// - Загрузка: читает строку пользователя из Google Sheets
    /// - Сохранение: обновляет строку пользователя в Google Sheets
    /// - Синхронизация: двусторонняя синхронизация с таблицей
    /// </summary>
    public class GoogleSheetsProfileProvider : IProfileDataProvider
    {
        private readonly GoogleSheetsClient _googleSheetsClient;
        private readonly string _username;
        private readonly string _groupName;
        private int _userRowNumber = -1;

        public int Priority => 90; // Высокий приоритет, но ниже чем Backend
        public string ProviderName => "GoogleSheets";

        public GoogleSheetsProfileProvider(GoogleSheetsConfig config, string username, string groupName)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "GoogleSheetsConfig is required");
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException("Username and GroupName are required");
            }

            _googleSheetsClient = new GoogleSheetsClient(config);
            _username = username;
            _groupName = groupName;

            Debug.Log($"[{ProviderName}] Initialized for user '{username}' in group '{groupName}'");
        }

        /// <summary>
        /// Проверяет, есть ли данные пользователя в Google Sheets.
        /// </summary>
        public bool HasData()
        {
            try
            {
                var task = FindUserRowAsync(_groupName, _username);
                task.Wait(5000); // Максимум 5 секунд ожидания

                _userRowNumber = task.Result;
                return _userRowNumber > 0;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{ProviderName}] HasData check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Синхронная загрузка (не рекомендуется для сети).
        /// </summary>
        public UserProfile Load()
        {
            Debug.LogWarning($"[{ProviderName}] Sync load not recommended. Use LoadAsync()");
            return LoadAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Асинхронная загрузка профиля из Google Sheets.
        /// </summary>
        public async Task<UserProfile> LoadAsync()
        {
            try
            {
                Debug.Log($"[{ProviderName}] Loading profile for user '{_username}' from '{_groupName}'");

                // Находим строку пользователя
                if (_userRowNumber <= 0)
                {
                    _userRowNumber = await FindUserRowAsync(_groupName, _username);
                }

                if (_userRowNumber <= 0)
                {
                    Debug.LogWarning($"[{ProviderName}] User '{_username}' not found in sheet '{_groupName}'");
                    return null;
                }

                // Загружаем данные пользователя
                var profile = await LoadProfileFromSheetAsync(_groupName, _userRowNumber);

                if (profile != null)
                {
                    Debug.Log($"[{ProviderName}] Profile loaded successfully for user '{_username}'");
                }

                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] LoadAsync failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Синхронное сохранение (не рекомендуется для сети).
        /// </summary>
        public void Save(UserProfile profile)
        {
            Debug.LogWarning($"[{ProviderName}] Sync save not recommended. Use SaveAsync()");
            SaveAsync(profile).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Асинхронное сохранение профиля в Google Sheets.
        /// </summary>
        public async Task SaveAsync(UserProfile profile)
        {
            try
            {
                if (profile == null)
                {
                    Debug.LogWarning($"[{ProviderName}] Cannot save null profile");
                    return;
                }

                Debug.Log($"[{ProviderName}] Saving profile for user '{_username}' to '{_groupName}'");

                // Находим строку пользователя
                if (_userRowNumber <= 0)
                {
                    _userRowNumber = await FindUserRowAsync(_groupName, _username);
                }

                if (_userRowNumber <= 0)
                {
                    Debug.LogWarning($"[{ProviderName}] User '{_username}' not found in sheet '{_groupName}'");
                    return;
                }

                // Сохраняем профиль в таблицу
                await SaveProfileToSheetAsync(_groupName, _userRowNumber, profile);

                Debug.Log($"[{ProviderName}] Profile saved successfully for user '{_username}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] SaveAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Очищает данные пользователя (обычно не используется).
        /// </summary>
        public void Clear()
        {
            Debug.LogWarning($"[{ProviderName}] Clear not supported for Google Sheets");
        }

        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Находит номер строки пользователя в Google Sheets.
        /// </summary>
        private async Task<int> FindUserRowAsync(string groupName, string username)
        {
            try
            {
                var range = await _googleSheetsClient.GetRangeAsync(groupName, "A:A");
                if (range?.values == null || range.values.Length == 0)
                {
                    return -1;
                }

                for (int i = 0; i < range.values.Length; i++)
                {
                    object[] row = range.values[i];
                    if (row != null && row.Length > 0 &&
                        string.Equals(row[0]?.ToString()?.Trim(), username.Trim(), StringComparison.Ordinal))
                    {
                        return i + 1; // Google Sheets row numbering starts at 1
                    }
                }

                return -1;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] FindUserRowAsync failed: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Загружает профиль пользователя из строки Google Sheets.
        /// </summary>
        private async Task<UserProfile> LoadProfileFromSheetAsync(string groupName, int rowNumber)
        {
            try
            {
                // Получаем полную строку пользователя
                var range = await _googleSheetsClient.GetRangeAsync(groupName, $"A{rowNumber}:Z{rowNumber}");

                if (range?.values == null || range.values.Length == 0)
                {
                    return null;
                }

                object[] rowData = range.values[0];

                // Создаём профиль пользователя
                var profile = new UserProfile
                {
                    FirstName = rowData.Length > 0 ? rowData[0]?.ToString() : _username,
                    LastName = ""
                };

                // Получаем список уровней для маппинга колонок
                var gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    Debug.LogWarning($"[{ProviderName}] GameManager not found");
                    return profile;
                }

                var runtimeLevelFactory = gameManager.GetService<RuntimeLevelFactory>();
                if (runtimeLevelFactory == null)
                {
                    Debug.LogWarning($"[{ProviderName}] RuntimeLevelFactory not found, cannot map level progress");
                    return profile;
                }

                var allLevels = runtimeLevelFactory.GetAllLevelInfos();

                // Парсим результаты уровней из колонок (B, C, D, ...)
                for (int i = 0; i < allLevels.Count; i++)
                {
                    int cellIndex = i + 1; // Колонка B = индекс 1, C = 2, и т.д.

                    if (cellIndex < rowData.Length && rowData[cellIndex] != null)
                    {
                        string cellValue = rowData[cellIndex].ToString().Trim();

                        if (!string.IsNullOrEmpty(cellValue) &&
                            float.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var score))
                        {
                            profile.LevelProgress.Set(allLevels[i].levelId, score);
                            Debug.Log($"[{ProviderName}] Loaded progress: {allLevels[i].levelId} = {score:F1}%");
                        }
                    }
                }

                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] LoadProfileFromSheetAsync failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Сохраняет профиль пользователя в строку Google Sheets.
        /// </summary>
        private async Task SaveProfileToSheetAsync(string groupName, int rowNumber, UserProfile profile)
        {
            try
            {
                // Получаем список уровней для маппинга колонок
                var gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    Debug.LogWarning($"[{ProviderName}] GameManager not found");
                    return;
                }

                var runtimeLevelFactory = gameManager.GetService<RuntimeLevelFactory>();
                if (runtimeLevelFactory == null)
                {
                    Debug.LogWarning($"[{ProviderName}] RuntimeLevelFactory not found, cannot map level progress");
                    return;
                }

                var allLevels = runtimeLevelFactory.GetAllLevelInfos();

                // Подготавливаем данные для сохранения
                var rowData = new List<string> { profile.FirstName ?? _username };

                foreach (var level in allLevels)
                {
                    if (profile.LevelProgress.TryGetValue(level.levelId, out var score))
                    {
                        rowData.Add(score.ToString("0.#", CultureInfo.InvariantCulture));
                        Debug.Log($"[{ProviderName}] Saving progress: {level.levelId} = {score:F1}%");
                    }
                    else
                    {
                        rowData.Add(""); // Пустая ячейка для непройденного уровня
                    }
                }

                // Обновляем строку в Google Sheets
                await _googleSheetsClient.UpdateRowAsync(groupName, rowNumber, rowData.Cast<object>().ToArray());

                Debug.Log($"[{ProviderName}] Row {rowNumber} updated in sheet '{groupName}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] SaveProfileToSheetAsync failed: {ex.Message}");
            }
        }
    }
}
