using System;
using System.IO;
using System.Threading.Tasks;
using GSheetsCommander;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Services
{
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
