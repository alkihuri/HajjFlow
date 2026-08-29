using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HajjFlow.Core;
using HajjFlow.UI;
using UnityEngine;
using UnityEngine.Networking;

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
    public class ContentLoaderService : MonoBehaviour
    {
        private LocalizationService _localizationService;
        private bool _enableAutoLoad = true;
        private float _retryDelaySeconds = 5f;
        private int _maxRetries = 3;

        // URL'ы для Google Sheets (экспорт в CSV)
        private static class GoogleSheetsUrls
        {
            // Локализация (язык: ru, en, ar, etc.)
            public const string Localization =
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTX5Wh2iYEJWMZNxQqDw0rroPUyiGnJglnAG2WdxfVkj3kYEGHF27bYV6roA6mMpLS-_247HpV7K7JS/pub?gid=421214789&single=true&output=csv";
            // Уровни (LevelId, Name, Description, ImageUrl, Order)
            public const string Levels =
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTX5Wh2iYEJWMZNxQqDw0rroPUyiGnJglnAG2WdxfVkj3kYEGHF27bYV6roA6mMpLS-_247HpV7K7JS/pub?gid=2100351538&single=true&output=csv";

            // Вопросы (LevelId, QuestionKey, Option1-4, CorrectIndex, ExplanationKey, Gems)
            public const string Questions =
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTX5Wh2iYEJWMZNxQqDw0rroPUyiGnJglnAG2WdxfVkj3kYEGHF27bYV6roA6mMpLS-_247HpV7K7JS/pub?gid=1633543718&single=true&output=csv";

            // Теория (LevelId, Order, TitleKey, TextKey, ImageBundleKey)
            public const string Theory =
                "https://docs.google.com/spreadsheets/d/e/2PACX-1vTX5Wh2iYEJWMZNxQqDw0rroPUyiGnJglnAG2WdxfVkj3kYEGHF27bYV6roA6mMpLS-_247HpV7K7JS/pub?gid=365931188&single=true&output=csv";
        }

        // Рантайм-модели данных
        [Serializable]
        public class RuntimeLevelInfo
        {
            public string levelId;
            public string nameKey;
            public string descriptionKey;
            public int order;
            public string imageBundleKey;
        }

        [Serializable]
        public class RuntimeQuizQuestion
        {
            public string levelId;
            public string questionKey;
            public string[] optionKeys = new string[4];
            public int correctIndex;
            public string explanationKey;
            public int gemsReward;
        }

        [Serializable]
        public class RuntimeTheoryCard
        {
            public string levelId;
            public int order;
            public string titleKey;
            public string textKey;
            public string imageBundleKey;
        }

        // Коллекции рантайм-данных
        private List<RuntimeLevelInfo> _levels = new List<RuntimeLevelInfo>();
        private List<RuntimeQuizQuestion> _questions = new List<RuntimeQuizQuestion>();
        private List<RuntimeTheoryCard> _theoryCards = new List<RuntimeTheoryCard>();
        private Dictionary<string, Dictionary<string, string>> _localizationTable = new Dictionary<string, Dictionary<string, string>>();

        // Сырые CSV сохраняются в persistentDataPath. Это надёжнее JsonUtility для
        // локализации (JsonUtility не сериализует Dictionary) и одинаково работает
        // в WebGL persistent storage.
        private string _localizationCsv;
        private string _levelsCsv;
        private string _questionsCsv;
        private string _theoryCsv;

        // Кэширование
        private static class CacheKeys
        {
            public const string Localization = "Content_Localization";
            public const string Levels = "Content_Levels";
            public const string Questions = "Content_Questions";
            public const string Theory = "Content_Theory";
            public const string LoadTimestamp = "Content_LoadTimestamp";
        }

        // События
        public event Action<bool> OnLoadComplete;
        public event Action<float> OnLoadProgress; // 0-1

        private int _currentRetry;

        // ──────────────────────────────────────────────────────────────────

        private void Start()
        {
            var uiservice = GameManager.Instance?.GetService<UIService>();
            OnLoadComplete +=  uiservice.HideLoadingScreen;
            OnLoadComplete += uiservice.ShowRegistrasionScreen;
            
            if (_enableAutoLoad)
            {
                StartCoroutine(LoadAllContent());
            }
            
            
  
        }

        /// <summary>
        /// Главный метод загрузки всего контента.
        /// Параллельно загружает 4 листа, парсит, кэширует.
        /// </summary>
        public IEnumerator LoadAllContent()
        {
            Debug.Log("[ContentLoaderService] Starting content load...");
            OnLoadProgress?.Invoke(0f);

            // При повторных запусках не обращаемся к сети: полный валидный кэш
            // должен быть использован первым.
            if (LoadFromCache())
            {
                OnLoadProgress?.Invoke(1f);
                OnLoadComplete?.Invoke(true);
                Debug.Log("[ContentLoaderService] Content loaded from persistent cache.");
                yield break;
            }

            _currentRetry = 0;

            bool loadSuccess = false;
            while (_currentRetry < _maxRetries && !loadSuccess)
            {
                bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
                
                if (hasInternet)
                {
                    yield return StartCoroutine(LoadAllFromGoogle());
                    loadSuccess = IsAllDataLoaded();
                    if (!loadSuccess)
                    {
                        Debug.LogWarning($"[ContentLoaderService] Content download was incomplete (attempt {_currentRetry + 1}/{_maxRetries})");
                        _currentRetry++;
                        if (_currentRetry < _maxRetries)
                            yield return new WaitForSeconds(_retryDelaySeconds);
                    }
                }
                else
                {
                    Debug.LogWarning($"[ContentLoaderService] No internet connection (attempt {_currentRetry + 1}/{_maxRetries})");
                    _currentRetry++;

                    if (_currentRetry < _maxRetries)
                    {
                        yield return new WaitForSeconds(_retryDelaySeconds);
                    }
                }
            }

            // Если сеть стала недоступна во время первой загрузки, используем
            // возможный ранее сохранённый кэш, затем встроенный Resources fallback.
            if (!loadSuccess)
            {
                Debug.Log("[ContentLoaderService] Loading from cache...");
                if (!LoadFromCache())
                    LoadFromResources();
            }

            OnLoadProgress?.Invoke(1f);
            OnLoadComplete?.Invoke(true);

            Debug.Log("[ContentLoaderService] Content loading completed!");
            Debug.Log($"  - Localization keys: {_localizationTable.Count}");
            Debug.Log($"  - Levels: {_levels.Count}");
            Debug.Log($"  - Questions: {_questions.Count}");
            Debug.Log($"  - Theory cards: {_theoryCards.Count}");
        }

        /// <summary>
        /// Загружает все данные параллельно из Google Sheets.
        /// </summary>
        private IEnumerator LoadAllFromGoogle()
        {
            float progress = 0f;
            ResetLoadFlags();

            // Запускаем все загрузки параллельно
            StartCoroutine(LoadLocalizationFromGoogle());
            StartCoroutine(LoadLevelsFromGoogle());
            StartCoroutine(LoadQuestionsFromGoogle());
            StartCoroutine(LoadTheoryFromGoogle());

            // Ждём пока все загрузятся
            while (!IsAllRequestsFinished())
            {
                progress += 0.01f;
                OnLoadProgress?.Invoke(Mathf.Clamp01(progress));
                yield return new WaitForSeconds(0.1f);
            }

            // Кэшируем только полный, успешно полученный набор данных.
            if (IsAllDataLoaded())
                SaveToCache();
        }

        private bool _localizationLoaded;
        private bool _levelsLoaded;
        private bool _questionsLoaded;
        private bool _theoryLoaded;
        private bool _localizationRequestFinished;
        private bool _levelsRequestFinished;
        private bool _questionsRequestFinished;
        private bool _theoryRequestFinished;

        private void ResetLoadFlags()
        {
            _localizationLoaded = false;
            _levelsLoaded = false;
            _questionsLoaded = false;
            _theoryLoaded = false;
            _localizationRequestFinished = false;
            _levelsRequestFinished = false;
            _questionsRequestFinished = false;
            _theoryRequestFinished = false;
        }

        private bool IsAllDataLoaded()
        {
            return _localizationLoaded && _levelsLoaded && _questionsLoaded && _theoryLoaded;
        }

        private bool IsAllRequestsFinished()
        {
            return _localizationRequestFinished && _levelsRequestFinished &&
                   _questionsRequestFinished && _theoryRequestFinished;
        }

        /// <summary>
        /// Загружает локализацию из Google Sheets.
        /// </summary>
        private IEnumerator LoadLocalizationFromGoogle()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(GoogleSheetsUrls.Localization))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string csvContent = request.downloadHandler.text;
                    ParseLocalizationCsv(csvContent);
                    _localizationCsv = csvContent;
                    _localizationLoaded = true;
                    Debug.Log($"[ContentLoaderService] Localization loaded: {_localizationTable.Count} keys");
                }
                else
                {
                    Debug.LogError($"[ContentLoaderService] Failed to load localization: {request.error}");
                }
            }

            _localizationRequestFinished = true;
        }

        /// <summary>
        /// Загружает информацию об уровнях.
        /// CSV: LevelId, NameKey, DescriptionKey, Order, ImageBundleKey
        /// </summary>
        private IEnumerator LoadLevelsFromGoogle()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(GoogleSheetsUrls.Levels))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string csvContent = request.downloadHandler.text;
                    ParseLevelsCsv(csvContent);
                    _levelsCsv = csvContent;
                    _levelsLoaded = true;
                    Debug.Log($"[ContentLoaderService] Levels loaded: {_levels.Count} levels");
                }
                else
                {
                    Debug.LogError($"[ContentLoaderService] Failed to load levels: {request.error}");
                }
            }

            _levelsRequestFinished = true;
        }

        /// <summary>
        /// Загружает вопросы квиза.
        /// CSV: LevelId, QuestionKey, Option1Key, Option2Key, Option3Key, Option4Key, CorrectIndex, ExplanationKey, GemsReward
        /// </summary>
        private IEnumerator LoadQuestionsFromGoogle()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(GoogleSheetsUrls.Questions))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string csvContent = request.downloadHandler.text;
                    ParseQuestionsCsv(csvContent);
                    _questionsCsv = csvContent;
                    _questionsLoaded = true;
                    Debug.Log($"[ContentLoaderService] Questions loaded: {_questions.Count} questions");
                }
                else
                {
                    Debug.LogError($"[ContentLoaderService] Failed to load questions: {request.error}");
                }
            }

            _questionsRequestFinished = true;
        }

        /// <summary>
        /// Загружает карточки теории.
        /// CSV: LevelId, Order, TitleKey, TextKey, ImageBundleKey
        /// </summary>
        private IEnumerator LoadTheoryFromGoogle()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(GoogleSheetsUrls.Theory))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string csvContent = request.downloadHandler.text;
                    ParseTheoryCsv(csvContent);
                    _theoryCsv = csvContent;
                    _theoryLoaded = true;
                    Debug.Log($"[ContentLoaderService] Theory loaded: {_theoryCards.Count} cards");
                }
                else
                {
                    Debug.LogError($"[ContentLoaderService] Failed to load theory: {request.error}");
                }
            }

            _theoryRequestFinished = true;
        }

        // ──────────────────────────────────────────────────────────────────
        // CSV PARSERS
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Парсит CSV локализации.
        /// Первая строка - заголовки (ключи языков: ru, en, ar, etc.)
        /// Первая колонна - ключи локализации
        /// </summary>
        private void ParseLocalizationCsv(string csvContent)
        {
            _localizationTable.Clear();

            string[] lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
                return;

            // Парсим заголовок (названия языков)
            string[] headers = ParseCsvLine(lines[0]);
            var languageColumns = new Dictionary<int, string>(); // column index -> language code

            for (int i = 1; i < headers.Length; i++)
            {
                string langCode = headers[i].Trim().ToLower();
                if (!string.IsNullOrEmpty(langCode))
                {
                    languageColumns[i] = langCode;
                }
            }

            // Парсим строки данных
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] fields = ParseCsvLine(lines[i]);
                if (fields.Length < 2)
                    continue;

                string key = fields[0].Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                var translations = new Dictionary<string, string>();

                // Собираем переводы для этого ключа
                foreach (var kvp in languageColumns)
                {
                    int colIndex = kvp.Key;
                    string langCode = kvp.Value;

                    if (colIndex < fields.Length)
                    {
                        string value = fields[colIndex].Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            translations[langCode] = value;
                        }
                    }
                }

                _localizationTable[key] = translations;
            }
        }

        /// <summary>
        /// Парсит CSV уровней.
        /// CSV: LevelId, NameKey, DescriptionKey, Order, ImageBundleKey
        /// </summary>
        private void ParseLevelsCsv(string csvContent)
        {
            _levels.Clear();

            string[] lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
                return;

            // Пропускаем заголовок
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] fields = ParseCsvLine(lines[i]);
                if (fields.Length < 5)
                    continue;

                var level = new RuntimeLevelInfo
                {
                    levelId = fields[0].Trim(),
                    nameKey = fields[1].Trim(),
                    descriptionKey = fields[2].Trim(),
                    order = int.TryParse(fields[3], out int order) ? order : 0,
                    imageBundleKey = fields[4].Trim()
                };

                if (!string.IsNullOrEmpty(level.levelId))
                {
                    _levels.Add(level);
                }
            }

            // Сортируем по Order
            _levels = _levels.OrderBy(l => l.order).ToList();
        }

        /// <summary>
        /// Парсит CSV вопросов.
        /// CSV: LevelId, QuestionKey, Option1Key, Option2Key, Option3Key, Option4Key, CorrectIndex, ExplanationKey, GemsReward
        /// </summary>
        private void ParseQuestionsCsv(string csvContent)
        {
            _questions.Clear();

            string[] lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
                return;

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] fields = ParseCsvLine(lines[i]);
                if (fields.Length < 9)
                    continue;

                var question = new RuntimeQuizQuestion
                {
                    levelId = fields[0].Trim(),
                    questionKey = fields[1].Trim(),
                    optionKeys = new[]
                    {
                        fields[2].Trim(),
                        fields[3].Trim(),
                        fields[4].Trim(),
                        fields[5].Trim()
                    },
                    correctIndex = int.TryParse(fields[6], out int idx) ? idx : 0,
                    explanationKey = fields[7].Trim(),
                    gemsReward = int.TryParse(fields[8], out int gems) ? gems : 5
                };

                if (!string.IsNullOrEmpty(question.levelId) && !string.IsNullOrEmpty(question.questionKey))
                {
                    _questions.Add(question);
                }
            }
        }

        /// <summary>
        /// Парсит CSV теории.
        /// CSV: LevelId, Order, TitleKey, TextKey, ImageBundleKey
        /// </summary>
        private void ParseTheoryCsv(string csvContent)
        {
            _theoryCards.Clear();

            string[] lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
                return;

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] fields = ParseCsvLine(lines[i]);
                if (fields.Length < 5)
                    continue;

                var card = new RuntimeTheoryCard
                {
                    levelId = fields[0].Trim(),
                    order = int.TryParse(fields[1], out int order) ? order : 0,
                    titleKey = fields[2].Trim(),
                    textKey = fields[3].Trim(),
                    imageBundleKey = fields[4].Trim()
                };

                if (!string.IsNullOrEmpty(card.levelId) && !string.IsNullOrEmpty(card.titleKey))
                {
                    _theoryCards.Add(card);
                }
            }

            // Группируем и сортируем по уровню и Order
            _theoryCards = _theoryCards
                .GroupBy(c => c.levelId)
                .SelectMany(g => g.OrderBy(c => c.order))
                .ToList();
        }

        /// <summary>
        /// Парсит CSV-строку, учитывая кавычки.
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    insideQuotes = !insideQuotes;
                }
                else if (c == ',' && !insideQuotes)
                {
                    fields.Add(currentField.ToString().Trim('"'));
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString().Trim('"'));
            return fields.ToArray();
        }

        // ──────────────────────────────────────────────────────────────────
        // КЭШИРОВАНИЕ (файловая система + PlayerPrefs fallback)
        // ──────────────────────────────────────────────────────────────────

        private string CacheDirectory => System.IO.Path.Combine(Application.persistentDataPath, "ContentCache");

        private void SaveToCache()
        {
            try
            {
                // Создаём директорию если её нет
                if (!System.IO.Directory.Exists(CacheDirectory))
                    System.IO.Directory.CreateDirectory(CacheDirectory);

                // Храним исходные CSV, а не сериализованные runtime-модели.
                // JsonUtility не поддерживает Dictionary, поэтому иначе
                // локализация не восстанавливается из кэша.
                SaveCacheFile("localization.csv", _localizationCsv);
                SaveCacheFile("levels.csv", _levelsCsv);
                SaveCacheFile("questions.csv", _questionsCsv);
                SaveCacheFile("theory.csv", _theoryCsv);

                // Timestamp в PlayerPrefs для быстрой проверки актуальности
                PlayerPrefs.SetString(CacheKeys.LoadTimestamp, System.DateTime.UtcNow.Ticks.ToString());
                PlayerPrefs.Save();

                Debug.Log($"[ContentLoaderService] Data cached to: {CacheDirectory}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ContentLoaderService] Failed to save cache: {ex.Message}");
                // PlayerPrefs остаётся fallback для платформ, где файловое
                // хранилище недоступно.
                SaveToCachePlayerPrefs();
            }
        }

        private void SaveCacheFile(string fileName, string content)
        {
            string path = System.IO.Path.Combine(CacheDirectory, fileName);
            System.IO.File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        }

        private string LoadCacheFile(string fileName)
        {
            string path = System.IO.Path.Combine(CacheDirectory, fileName);
            if (System.IO.File.Exists(path))
                return System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            return null;
        }

        private void SaveToCachePlayerPrefs()
        {
            PlayerPrefs.SetString(CacheKeys.Localization, _localizationCsv ?? string.Empty);
            PlayerPrefs.SetString(CacheKeys.Levels, _levelsCsv ?? string.Empty);
            PlayerPrefs.SetString(CacheKeys.Questions, _questionsCsv ?? string.Empty);
            PlayerPrefs.SetString(CacheKeys.Theory, _theoryCsv ?? string.Empty);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Загружает только полный кэш. Частичный набор не используется, чтобы
        /// приложение не стартовало со смешанными версиями контента.
        /// </summary>
        private bool LoadFromCache()
        {
            try
            {
                string localizationCsv = LoadCacheFile("localization.csv");
                string levelsCsv = LoadCacheFile("levels.csv");
                string questionsCsv = LoadCacheFile("questions.csv");
                string theoryCsv = LoadCacheFile("theory.csv");

                if (!string.IsNullOrWhiteSpace(localizationCsv) &&
                    !string.IsNullOrWhiteSpace(levelsCsv) &&
                    !string.IsNullOrWhiteSpace(questionsCsv) &&
                    !string.IsNullOrWhiteSpace(theoryCsv))
                {
                    ParseLocalizationCsv(localizationCsv);
                    ParseLevelsCsv(levelsCsv);
                    ParseQuestionsCsv(questionsCsv);
                    ParseTheoryCsv(theoryCsv);
                    _localizationCsv = localizationCsv;
                    _levelsCsv = levelsCsv;
                    _questionsCsv = questionsCsv;
                    _theoryCsv = theoryCsv;
                    if (IsCachedContentValid())
                    {
                        Debug.Log("[ContentLoaderService] Data loaded from file cache");
                        return true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ContentLoaderService] File cache failed: {ex.Message}");
            }

            // В WebGL persistentDataPath может быть недоступен до инициализации
            // виртуальной FS, поэтому оставляем PlayerPrefs как запасной кэш.
            string localizationPlayerPrefs = PlayerPrefs.GetString(CacheKeys.Localization, "");
            string levelsPlayerPrefs = PlayerPrefs.GetString(CacheKeys.Levels, "");
            string questionsPlayerPrefs = PlayerPrefs.GetString(CacheKeys.Questions, "");
            string theoryPlayerPrefs = PlayerPrefs.GetString(CacheKeys.Theory, "");
            if (!string.IsNullOrWhiteSpace(localizationPlayerPrefs) &&
                !string.IsNullOrWhiteSpace(levelsPlayerPrefs) &&
                !string.IsNullOrWhiteSpace(questionsPlayerPrefs) &&
                !string.IsNullOrWhiteSpace(theoryPlayerPrefs))
            {
                ParseLocalizationCsv(localizationPlayerPrefs);
                ParseLevelsCsv(levelsPlayerPrefs);
                ParseQuestionsCsv(questionsPlayerPrefs);
                ParseTheoryCsv(theoryPlayerPrefs);
                _localizationCsv = localizationPlayerPrefs;
                _levelsCsv = levelsPlayerPrefs;
                _questionsCsv = questionsPlayerPrefs;
                _theoryCsv = theoryPlayerPrefs;
                if (IsCachedContentValid())
                {
                    Debug.Log("[ContentLoaderService] Data loaded from PlayerPrefs cache");
                    return true;
                }
            }

            return false;
        }

        private bool IsCachedContentValid()
        {
            return _localizationTable.Count > 0 && _levels.Count > 0 &&
                   _questions.Count > 0 && _theoryCards.Count > 0;
        }

        /// <summary>
        /// Fallback загрузка из Resources (статические данные вшитые в билд).
        /// </summary>
        private void LoadFromResources()
        {
            var localizationAsset = Resources.Load<TextAsset>("localization");
            if (localizationAsset != null)
            {
                ParseLocalizationCsv(localizationAsset.text);
                Debug.Log("[ContentLoaderService] Loaded localization from Resources fallback");
            }

            // Пытаемся загрузить levels/questions/theory из Resources если они там есть
            var levelsAsset = Resources.Load<TextAsset>("content_levels");
            if (levelsAsset != null)
                ParseLevelsCsv(levelsAsset.text);

            var questionsAsset = Resources.Load<TextAsset>("content_questions");
            if (questionsAsset != null)
                ParseQuestionsCsv(questionsAsset.text);

            var theoryAsset = Resources.Load<TextAsset>("content_theory");
            if (theoryAsset != null)
                ParseTheoryCsv(theoryAsset.text);

            Debug.Log("[ContentLoaderService] Resources fallback complete");
        }

        /// <summary>
        /// Возвращает timestamp последней успешной загрузки или null.
        /// </summary>
        public System.DateTime? GetLastLoadTimestamp()
        {
            string ticksStr = PlayerPrefs.GetString(CacheKeys.LoadTimestamp, "");
            if (long.TryParse(ticksStr, out long ticks))
                return new System.DateTime(ticks, System.DateTimeKind.Utc);
            return null;
        }

        /// <summary>
        /// Проверяет, нужно ли обновление (данные старше заданного интервала).
        /// </summary>
        public bool NeedsUpdate(System.TimeSpan maxAge)
        {
            var lastLoad = GetLastLoadTimestamp();
            if (!lastLoad.HasValue) return true;
            return (System.DateTime.UtcNow - lastLoad.Value) > maxAge;
        }

        private string SerializeLocalization()
        {
            var wrapper = new LocalizationWrapper { Table = _localizationTable };
            return JsonUtility.ToJson(wrapper);
        }

        private string SerializeLevels()
        {
            var wrapper = new LevelsWrapper { levels = _levels };
            return JsonUtility.ToJson(wrapper);
        }

        private string SerializeQuestions()
        {
            var wrapper = new QuestionsWrapper { questions = _questions };
            return JsonUtility.ToJson(wrapper);
        }

        private string SerializeTheory()
        {
            var wrapper = new TheoryWrapper { cards = _theoryCards };
            return JsonUtility.ToJson(wrapper);
        }

        private void DeserializeLocalization(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<LocalizationWrapper>(json);
                _localizationTable = wrapper?.Table ?? new Dictionary<string, Dictionary<string, string>>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentLoaderService] Failed to deserialize localization: {ex.Message}");
            }
        }

        private void DeserializeLevels(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<LevelsWrapper>(json);
                _levels = wrapper?.levels ?? new List<RuntimeLevelInfo>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentLoaderService] Failed to deserialize levels: {ex.Message}");
            }
        }

        private void DeserializeQuestions(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<QuestionsWrapper>(json);
                _questions = wrapper?.questions ?? new List<RuntimeQuizQuestion>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentLoaderService] Failed to deserialize questions: {ex.Message}");
            }
        }

        private void DeserializeTheory(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<TheoryWrapper>(json);
                _theoryCards = wrapper?.cards ?? new List<RuntimeTheoryCard>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentLoaderService] Failed to deserialize theory: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // JSON Wrappers для сериализации
        // ──────────────────────────────────────────────────────────────────

        [Serializable]
        private class LocalizationWrapper
        {
            public Dictionary<string, Dictionary<string, string>> Table;
        }

        [Serializable]
        private class LevelsWrapper
        {
            public List<RuntimeLevelInfo> levels;
        }

        [Serializable]
        private class QuestionsWrapper
        {
            public List<RuntimeQuizQuestion> questions;
        }

        [Serializable]
        private class TheoryWrapper
        {
            public List<RuntimeTheoryCard> cards;
        }

        // ──────────────────────────────────────────────────────────────────
        // PUBLIC API - Доступ к загруженным данным
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Получить все уровни.
        /// </summary>
        public List<RuntimeLevelInfo> GetAllLevels() => new List<RuntimeLevelInfo>(_levels);

        /// <summary>
        /// Получить вопросы для конкретного уровня.
        /// </summary>
        public List<RuntimeQuizQuestion> GetQuestionsForLevel(string levelId)
        {
            return _questions.Where(q => q.levelId == levelId).ToList();
        }

        /// <summary>
        /// Получить карточки теории для конкретного уровня.
        /// </summary>
        public List<RuntimeTheoryCard> GetTheoryCardsForLevel(string levelId)
        {
            return _theoryCards.Where(c => c.levelId == levelId).ToList();
        }

        /// <summary>
        /// Получить локализованный текст по ключу.
        /// </summary>
        public string GetLocalizedText(string key, string languageCode = "ru")
        {
            if (!_localizationTable.TryGetValue(key, out var translations))
                return key;

            if (translations.TryGetValue(languageCode, out var value))
                return value;

            // Fallback на первый доступный язык
            return translations.Values.FirstOrDefault() ?? key;
        }

        /// <summary>
        /// Очистить кэш.
        /// </summary>
        [ContextMenu("Clear Localization")]
        public void ClearCache()
        {
            PlayerPrefs.DeleteKey(CacheKeys.Localization);
            PlayerPrefs.DeleteKey(CacheKeys.Levels);
            PlayerPrefs.DeleteKey(CacheKeys.Questions);
            PlayerPrefs.DeleteKey(CacheKeys.Theory);
            PlayerPrefs.DeleteKey(CacheKeys.LoadTimestamp);
            PlayerPrefs.Save();

            try
            {
                if (System.IO.Directory.Exists(CacheDirectory))
                    System.IO.Directory.Delete(CacheDirectory, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentLoaderService] Failed to delete file cache: {ex.Message}");
            }

            _localizationTable.Clear();
            _levels.Clear();
            _questions.Clear();
            _theoryCards.Clear();

            Debug.Log("[ContentLoaderService] Cache cleared");
        }

        /// <summary>
        /// Удаляет кэш и запускает новую полную загрузку из сети.
        /// </summary>
        public void EraseCacheData()
        {
            ClearCache();
            StartCoroutine(LoadAllContent());
        }
    }
}
