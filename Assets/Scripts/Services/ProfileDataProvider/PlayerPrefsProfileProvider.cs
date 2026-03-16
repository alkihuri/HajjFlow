using System;
using System.Threading.Tasks;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Services
{
    /// <summary>
    /// Провайдер данных профиля через PlayerPrefs.
    /// Быстрый и надёжный для локального хранения.
    /// </summary>
    public class PlayerPrefsProfileProvider : IProfileDataProvider
    {
        private const string ProfileKey = "UserProfile_Data";
        
        public int Priority => 10; // Средний приоритет
        public string ProviderName => "PlayerPrefs";

        public UserProfile Load()
        {
            if (!HasData())
            {
                Debug.Log($"[{ProviderName}] No saved data found, returning null");
                return null;
            }
            
            try
            {
                string json = PlayerPrefs.GetString(ProfileKey);
                var profile = JsonUtility.FromJson<UserProfile>(json);
                Debug.Log($"[{ProviderName}] Profile loaded successfully");
                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] Load failed: {ex.Message}");
                return null;
            }
        }

        public void Save(UserProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning($"[{ProviderName}] Cannot save null profile");
                return;
            }
            
            try
            {
                string json = JsonUtility.ToJson(profile, prettyPrint: false);
                PlayerPrefs.SetString(ProfileKey, json);
                PlayerPrefs.Save();
                Debug.Log($"[{ProviderName}] Profile saved");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] Save failed: {ex.Message}");
            }
        }

        public bool HasData()
        {
            return PlayerPrefs.HasKey(ProfileKey);
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(ProfileKey);
            PlayerPrefs.Save();
            Debug.Log($"[{ProviderName}] Data cleared");
        }

        // Async методы - для PlayerPrefs просто делегируем синхронным
        public Task<UserProfile> LoadAsync()
        {
            return Task.FromResult(Load());
        }

        public Task SaveAsync(UserProfile profile)
        {
            Save(profile);
            return Task.CompletedTask;
        }
    }
}

