using System;
using System.Threading.Tasks;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Services
{
    /// <summary>
    /// Заглушка провайдера для бекенда.
    /// Реализуйте этот класс при подключении реального сервера.
    /// </summary>
    public class BackendProfileProvider : IProfileDataProvider
    {
        private readonly string _apiBaseUrl;
        private readonly string _userId;
        
        public int Priority => 100; // Высший приоритет - бекенд главнее
        public string ProviderName => "Backend";

        public BackendProfileProvider(string apiBaseUrl, string userId)
        {
            _apiBaseUrl = apiBaseUrl;
            _userId = userId;
        }

        public UserProfile Load()
        {
            // Синхронная загрузка не рекомендуется для сети
            Debug.LogWarning($"[{ProviderName}] Sync load not recommended for network. Use LoadAsync()");
            return LoadAsync().GetAwaiter().GetResult();
        }

        public void Save(UserProfile profile)
        {
            // Синхронное сохранение не рекомендуется для сети
            Debug.LogWarning($"[{ProviderName}] Sync save not recommended for network. Use SaveAsync()");
            SaveAsync(profile).GetAwaiter().GetResult();
        }

        public bool HasData()
        {
            // Для бекенда всегда пробуем загрузить
            return !string.IsNullOrEmpty(_userId);
        }

        public void Clear()
        {
            Debug.LogWarning($"[{ProviderName}] Clear not supported for backend");
        }

        public async Task<UserProfile> LoadAsync()
        {
            // TODO: Реализуйте HTTP запрос к бекенду
            // Пример:
            // var url = $"{_apiBaseUrl}/users/{_userId}/profile";
            // using var request = UnityWebRequest.Get(url);
            // await request.SendWebRequest();
            // return JsonUtility.FromJson<UserProfile>(request.downloadHandler.text);
            
            Debug.Log($"[{ProviderName}] LoadAsync - NOT IMPLEMENTED. Returning null.");
            await Task.Delay(100); // Имитация сетевой задержки
            return null;
        }

        public async Task SaveAsync(UserProfile profile)
        {
            // TODO: Реализуйте HTTP запрос к бекенду
            // Пример:
            // var url = $"{_apiBaseUrl}/users/{_userId}/profile";
            // var json = JsonUtility.ToJson(profile);
            // using var request = UnityWebRequest.Put(url, json);
            // request.SetRequestHeader("Content-Type", "application/json");
            // await request.SendWebRequest();
            
            Debug.Log($"[{ProviderName}] SaveAsync - NOT IMPLEMENTED");
            await Task.Delay(100); // Имитация сетевой задержки
        }
    }
}

