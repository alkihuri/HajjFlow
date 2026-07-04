using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Services
{
    /// <summary>
    /// Сервис загрузки/сохранения профиля с поддержкой нескольких источников данных.
    /// Использует паттерн стратегии для выбора провайдера.
    /// 
    /// Архитектура:
    /// - Загрузка: пробует провайдеры по приоритету (Backend -> PlayerPrefs -> File)
    /// - Сохранение: сохраняет во все локальные провайдеры + в бекенд если доступен
    /// </summary>
    public class ProfileLoaderService
    {
        private readonly List<IProfileDataProvider> _providers = new List<IProfileDataProvider>();
        private UserProfile _cachedProfile;
        
        // Флаги конфигурации
        private bool _useBackend;

        /// <summary>
        /// Инициализирует сервис с локальными провайдерами.
        /// </summary>
        public ProfileLoaderService()
        {
            // Регистрируем локальные провайдеры
            RegisterProvider(new PlayerPrefsProfileProvider());
            //RegisterProvider(new FileProfileProvider());
            
            Debug.Log($"[ProfileLoaderService] Initialized with {_providers.Count} providers");
            
            // Сразу загружаем профиль чтобы он был доступен
            EnsureProfileLoaded();
        }
        
        /// <summary>
        /// Гарантирует что профиль загружен. Если нет - загружает.
        /// </summary>
        private void EnsureProfileLoaded()
        {
            if (_cachedProfile == null)
            {
                Load();
            }
        }

        /// <summary>
        /// Включает бекенд провайдер.
        /// </summary>
        public void EnableBackend(string apiUrl, string userId)
        {
            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("[ProfileLoaderService] Invalid backend config");
                return;
            }

            _useBackend = true;
            
            RegisterProvider(new BackendProfileProvider(apiUrl, userId));
            Debug.Log($"[ProfileLoaderService] Backend enabled: {apiUrl}");
        }

        /// <summary>
        /// Отключает бекенд провайдер.
        /// </summary>
        public void DisableBackend()
        {
            _providers.RemoveAll(p => p.ProviderName == "Backend");
            _useBackend = false;
            Debug.Log("[ProfileLoaderService] Backend disabled");
        }

        /// <summary>
        /// Регистрирует провайдер данных.
        /// </summary>
        public void RegisterProvider(IProfileDataProvider provider)
        {
            if (provider == null) return;
            
            // Удаляем дубликат если есть
            _providers.RemoveAll(p => p.ProviderName == provider.ProviderName);
            _providers.Add(provider);
            
            // Сортируем по приоритету (высший первый)
            _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// Загружает профиль из первого доступного провайдера.
        /// </summary>
        public UserProfile Load()
        {
            if (_cachedProfile != null)
            {
                Debug.Log("[ProfileLoaderService] Returning cached profile");
                return _cachedProfile;
            }

            foreach (var provider in _providers)
            {
                try
                {
                    if (provider.HasData())
                    {
                        var profile = provider.Load();
                        if (profile != null)
                        {
                            _cachedProfile = profile;
                            Debug.Log($"[ProfileLoaderService] Loaded from {provider.ProviderName}");
                            return profile;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ProfileLoaderService] Provider {provider.ProviderName} failed: {ex.Message}");
                }
            }

            // Ни один провайдер не вернул данные - создаём новый профиль
            _cachedProfile = new UserProfile();
            Debug.Log("[ProfileLoaderService] Created new default profile");
            return _cachedProfile;
        }

        /// <summary>
        /// Асинхронно загружает профиль (предпочтительно для бекенда).
        /// </summary>
        public async Task<UserProfile> LoadAsync()
        {
            if (_cachedProfile != null)
            {
                Debug.Log("[ProfileLoaderService] Returning cached profile");
                return _cachedProfile;
            }

            foreach (var provider in _providers)
            {
                try
                {
                    if (provider.HasData())
                    {
                        var profile = await provider.LoadAsync();
                        if (profile != null)
                        {
                            _cachedProfile = profile;
                            Debug.Log($"[ProfileLoaderService] Loaded async from {provider.ProviderName}");
                            return profile;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ProfileLoaderService] Provider {provider.ProviderName} async load failed: {ex.Message}");
                }
            }

            _cachedProfile = new UserProfile();
            Debug.Log("[ProfileLoaderService] Created new default profile");
            return _cachedProfile;
        }

        /// <summary>
        /// Сохраняет профиль во все локальные провайдеры.
        /// </summary>
        public void Save(UserProfile profile = null)
        {
            // Если профиль не передан - используем кешированный или загружаем
            if (profile == null)
            {
                EnsureProfileLoaded();
                profile = _cachedProfile;
            }
            
            // Дополнительная проверка после загрузки
            if (profile == null)
            {
                Debug.LogWarning("[ProfileLoaderService] No profile to save - creating default");
                profile = new UserProfile();
            }

            _cachedProfile = profile;

            // Сохраняем в локальные провайдеры (не бекенд - он асинхронный)
            foreach (var provider in _providers.Where(p => p.ProviderName != "Backend"))
            {
                try
                {
                    provider.Save(profile);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ProfileLoaderService] Save to {provider.ProviderName} failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Асинхронно сохраняет профиль во все провайдеры включая бекенд.
        /// </summary>
        public async Task SaveAsync(UserProfile profile = null)
        {
            // Если профиль не передан - используем кешированный или загружаем
            if (profile == null)
            {
                if (_cachedProfile == null)
                {
                    await LoadAsync();
                }
                profile = _cachedProfile;
            }
            
            // Дополнительная проверка после загрузки
            if (profile == null)
            {
                Debug.LogWarning("[ProfileLoaderService] No profile to save - creating default");
                profile = new UserProfile();
            }

            _cachedProfile = profile;

            var tasks = new List<Task>();
            foreach (var provider in _providers)
            {
                tasks.Add(SaveToProviderAsync(provider, profile));
            }

            await Task.WhenAll(tasks);
            Debug.Log("[ProfileLoaderService] Saved to all providers");
        }

        private async Task SaveToProviderAsync(IProfileDataProvider provider, UserProfile profile)
        {
            try
            {
                await provider.SaveAsync(profile);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileLoaderService] Async save to {provider.ProviderName} failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Возвращает закешированный профиль или загружает его.
        /// </summary>
        public UserProfile GetProfile()
        {
            return _cachedProfile ?? Load();
        }

        /// <summary>
        /// Обновляет профиль и сохраняет изменения.
        /// </summary>
        public void UpdateProfile(Action<UserProfile> updateAction)
        {
            var profile = GetProfile();
            updateAction?.Invoke(profile);
            Save(profile);
        }

        /// <summary>
        /// Асинхронно обновляет профиль и сохраняет во все провайдеры.
        /// </summary>
        public async Task UpdateProfileAsync(Action<UserProfile> updateAction)
        {
            var profile = GetProfile();
            updateAction?.Invoke(profile);
            await SaveAsync(profile);
        }

        /// <summary>
        /// Сбрасывает кеш профиля (принудительная перезагрузка при следующем обращении).
        /// </summary>
        public void InvalidateCache()
        {
            _cachedProfile = null;
            Debug.Log("[ProfileLoaderService] Cache invalidated");
        }

        /// <summary>
        /// Очищает все сохранённые данные профиля.
        /// </summary>
        public void ClearAllData()
        {
            foreach (var provider in _providers)
            {
                try
                {
                    provider.Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ProfileLoaderService] Clear {provider.ProviderName} failed: {ex.Message}");
                }
            }
            
            _cachedProfile = null;
            Debug.Log("[ProfileLoaderService] All data cleared");
        }

        /// <summary>
        /// Синхронизирует локальные данные с бекендом.
        /// </summary>
        public async Task SyncWithBackendAsync()
        {
            if (!_useBackend)
            {
                Debug.LogWarning("[ProfileLoaderService] Backend not enabled");
                return;
            }

            // Загружаем из бекенда
            var backendProvider = _providers.FirstOrDefault(p => p.ProviderName == "Backend");
            if (backendProvider == null) return;

            var backendProfile = await backendProvider.LoadAsync();
            if (backendProfile != null)
            {
                _cachedProfile = backendProfile;
                // Сохраняем локально
                foreach (var provider in _providers.Where(p => p.ProviderName != "Backend"))
                {
                    provider.Save(backendProfile);
                }
                Debug.Log("[ProfileLoaderService] Synced from backend");
            }
            else if (_cachedProfile != null)
            {
                // Если бекенд пустой, отправляем локальные данные
                await backendProvider.SaveAsync(_cachedProfile);
                Debug.Log("[ProfileLoaderService] Pushed local data to backend");
            }
        }

        public UserProfile GetNotCashProfile()
        { 
            foreach (var provider in _providers)
            {
                try
                {
                    if (provider.HasData())
                    {
                        var profile = provider.Load();
                        if (profile != null)
                        {
                            Debug.Log($"[ProfileLoaderService] Loaded from {provider.ProviderName} without cache");
                            return profile;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ProfileLoaderService] Provider {provider.ProviderName} failed: {ex.Message}");
                }
            }

            Debug.Log("[ProfileLoaderService] No profile found without cache - returning new default");
            return new UserProfile();
        }
    }
}

