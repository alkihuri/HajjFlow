using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Services
{
    /// <summary>
    /// Провайдер данных профиля через файловую систему.
    /// Более надёжный для больших данных.
    /// </summary>
    public class FileProfileProvider : IProfileDataProvider
    {
        private const string SaveFileName = "user_profile.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        
        public int Priority => 5; // Низкий приоритет (fallback)
        public string ProviderName => "FileSystem";

        public UserProfile Load()
        {
            if (!HasData())
            {
                Debug.Log($"[{ProviderName}] No saved file found at {SavePath}");
                return null;
            }
            
            try
            {
                string json = File.ReadAllText(SavePath);
                var profile = JsonUtility.FromJson<UserProfile>(json);
                Debug.Log($"[{ProviderName}] Profile loaded from {SavePath}");
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
                string json = JsonUtility.ToJson(profile, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[{ProviderName}] Profile saved to {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] Save failed: {ex.Message}");
            }
        }

        public bool HasData()
        {
            return File.Exists(SavePath);
        }

        public void Clear()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log($"[{ProviderName}] File deleted: {SavePath}");
            }
        }

        // Async методы
        public async Task<UserProfile> LoadAsync()
        {
            if (!HasData())
            {
                Debug.Log($"[{ProviderName}] No saved file found");
                return null;
            }
            
            try
            {
                string json = await File.ReadAllTextAsync(SavePath);
                var profile = JsonUtility.FromJson<UserProfile>(json);
                Debug.Log($"[{ProviderName}] Profile loaded async from {SavePath}");
                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] Async load failed: {ex.Message}");
                return null;
            }
        }

        public async Task SaveAsync(UserProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning($"[{ProviderName}] Cannot save null profile");
                return;
            }
            
            try
            {
                string json = JsonUtility.ToJson(profile, prettyPrint: true);
                await File.WriteAllTextAsync(SavePath, json);
                Debug.Log($"[{ProviderName}] Profile saved async to {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ProviderName}] Async save failed: {ex.Message}");
            }
        }
    }
}

