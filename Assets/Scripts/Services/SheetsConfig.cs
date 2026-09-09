namespace HajjFlow.Services
{
    /// <summary>
    /// Загружает контент из Google Sheets (локализацию, уровни, вопросы, теорию).
    /// Парсит CSV в рантайм-структуры данных.
    /// Кэширует данные локально (PlayerPrefs/файловая система).
    /// Предоставляет fallback при отсутствии интернета.
    /// 
    /// Архитектура:
    /// 1. Google Sheets → CSV (UnityWebRequest)
    /// 2. CSV Parser → RuntimeModels
    /// 3. LocalizationService обновляется
    /// 4. Кэш сохраняется на диск
    /// </summary>
    ///
    ///   [System.Serializable]
    public class SheetsConfig
    {
        public string LastModify;
        public string AppUrl;
        public string ApiKey;
        public int TimeoutSeconds;
        public bool EnableLogging;
        public bool UseGetRequest;
        public bool ClerDataOnRegister;
        
        public SheetsConfig()
        {
                 
        }
    }
}