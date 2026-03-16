using System;
using System.Threading.Tasks;
using HajjFlow.Data;

namespace HajjFlow.Services
{
    /// <summary>
    /// Интерфейс провайдера данных профиля.
    /// Позволяет абстрагироваться от источника данных (PlayerPrefs, File, Backend).
    /// </summary>
    public interface IProfileDataProvider
    {
        /// <summary>Приоритет провайдера (чем выше, тем предпочтительнее)</summary>
        int Priority { get; }
        
        /// <summary>Название провайдера для логирования</summary>
        string ProviderName { get; }
        
        /// <summary>Загружает профиль</summary>
        UserProfile Load();
        
        /// <summary>Сохраняет профиль</summary>
        void Save(UserProfile profile);
        
        /// <summary>Проверяет, есть ли сохранённые данные</summary>
        bool HasData();
        
        /// <summary>Очищает сохранённые данные</summary>
        void Clear();
        
        /// <summary>Асинхронная загрузка (для будущего бекенда)</summary>
        Task<UserProfile> LoadAsync();
        
        /// <summary>Асинхронное сохранение (для будущего бекенда)</summary>
        Task SaveAsync(UserProfile profile);
    }
}

