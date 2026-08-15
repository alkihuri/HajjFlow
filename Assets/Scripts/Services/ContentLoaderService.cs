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
            if (_enableAutoLoad)
            {
                StartCoroutine(LoadAllContent());
            }
            
            
            var uiservice = GameManager.Instance?.GetService<UIService>();
            OnLoadComplete +=  uiservice.ShowStartButton;
        }

        /// <summary>
        /// Главный метод загрузки всего контента.
        /// Параллельно загружает 4 листа, парсит, кэширует.
        /// </summary>
        public IEnumerator LoadAllContent()
        {
            Debug.Log("[ContentLoaderService] Starting content load...");
            OnLoadProgress?.Invoke(0f);

            _currentRetry = 0;

            bool loadSuccess = false;
            while (_currentRetry < _maxRetries && !loadSuccess)
            {
                bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
                
                if (hasInternet)
                {
                    yield return StartCoroutine(LoadAllFromGoogle());
                    loadSuccess = true;
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

            // Если всё равно не загрузилось с интернета - используем кэш
            if (!loadSuccess)
            {
                Debug.Log("[ContentLoaderService] Loading from cache...");
                LoadFromCache();
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
            while (!IsAllDataLoaded())
            {
                progress += 0.01f;
                OnLoadProgress?.Invoke(Mathf.Clamp01(progress));
                yield return new WaitForSeconds(0.1f);
            }

            // Сохраняем в кэш
            SaveToCache();
        }

        private bool _localizationLoaded;
        private bool _levelsLoaded;
        private bool _questionsLoaded;
        private bool _theoryLoaded;

        private void ResetLoadFlags()
        {
            _localizationLoaded = false;
            _levelsLoaded = false;
            _questionsLoaded = false;
            _theoryLoaded = false;
        }

        private bool IsAllDataLoaded()
        {
            return _localizationLoaded && _levelsLoaded && _questionsLoaded && _theoryLoaded;
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
                    Debug.Log($"[ContentLoaderService] Localization loaded: {_localizationTable.Count} keys");
                }
                else
                {
                    Debug.LogError($"[ContentLoaderService] Failed to load localization: {request.error}");
                }
            }

            _localizationLoaded = true;
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
                    Debug.Log($"[ContentLoaderService] Levels loaded: {_levels.Count} levels");
                }
                else
                {
                    Debug.LogError($"[ContentLoaderService] Failed to load levels: {request.error}");
                }
            }

            _levelsLoaded = true;
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
                    Debug.Log($"[ContentLoaderService] Questions loaded: {_questions.Count} questions");
                }
                else
                {
                    Debug.LogError($"[ContentLoaderService] Failed to load questions: {request.error}");
                }
            }

            _questionsLoaded = true;
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
                    Debug.Log($"[ContentLoaderService] Theory loaded: {_theoryCards.Count} cards");
                }
                else
                {
                    Debug.LogError($"[ContentLoaderService] Failed to load theory: {request.error}");
                }
            }

            _theoryLoaded = true;
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

                // Сохраняем в файловую систему (надёжнее для больших данных)
                SaveCacheFile("localization.json", SerializeLocalization());
                SaveCacheFile("levels.json", SerializeLevels());
                SaveCacheFile("questions.json", SerializeQuestions());
                SaveCacheFile("theory.json", SerializeTheory());

                // Timestamp в PlayerPrefs для быстрой проверки актуальности
                PlayerPrefs.SetString(CacheKeys.LoadTimestamp, System.DateTime.UtcNow.Ticks.ToString());
                PlayerPrefs.Save();

                Debug.Log($"[ContentLoaderService] Data cached to: {CacheDirectory}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ContentLoaderService] Failed to save cache: {ex.Message}");
                // Fallback на PlayerPrefs
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
            PlayerPrefs.SetString(CacheKeys.Localization, SerializeLocalization());
            PlayerPrefs.SetString(CacheKeys.Levels, SerializeLevels());
            PlayerPrefs.SetString(CacheKeys.Questions, SerializeQuestions());
            PlayerPrefs.SetString(CacheKeys.Theory, SerializeTheory());
            PlayerPrefs.Save();
        }

        private void LoadFromCache()
        {
            bool loaded = false;

            // Попытка 1: файловая система
            try
            {
                string localizationJson = LoadCacheFile("localization.json");
                string levelsJson = LoadCacheFile("levels.json");
                string questionsJson = LoadCacheFile("questions.json");
                string theoryJson = LoadCacheFile("theory.json");

                if (!string.IsNullOrEmpty(levelsJson))
                {
                    if (!string.IsNullOrEmpty(localizationJson))
                        DeserializeLocalization(localizationJson);
                    DeserializeLevels(levelsJson);
                    if (!string.IsNullOrEmpty(questionsJson))
                        DeserializeQuestions(questionsJson);
                    if (!string.IsNullOrEmpty(theoryJson))
                        DeserializeTheory(theoryJson);
                    loaded = true;
                    Debug.Log("[ContentLoaderService] Data loaded from file cache");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ContentLoaderService] File cache failed: {ex.Message}");
            }

            // Попытка 2: PlayerPrefs (fallback)
            if (!loaded)
            {
                string localizationJson = PlayerPrefs.GetString(CacheKeys.Localization, "");
                string levelsJson = PlayerPrefs.GetString(CacheKeys.Levels, "");
                string questionsJson = PlayerPrefs.GetString(CacheKeys.Questions, "");
                string theoryJson = PlayerPrefs.GetString(CacheKeys.Theory, "");

                if (!string.IsNullOrEmpty(localizationJson))
                    DeserializeLocalization(localizationJson);
                if (!string.IsNullOrEmpty(levelsJson))
                    DeserializeLevels(levelsJson);
                if (!string.IsNullOrEmpty(questionsJson))
                    DeserializeQuestions(questionsJson);
                if (!string.IsNullOrEmpty(theoryJson))
                    DeserializeTheory(theoryJson);

                loaded = _levels.Count > 0;
                if (loaded)
                    Debug.Log("[ContentLoaderService] Data loaded from PlayerPrefs cache");
            }

            // Попытка 3: Resources fallback (статика вшитая в билд)
            if (!loaded)
            {
                LoadFromResources();
            }
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
        public void ClearCache()
        {
            PlayerPrefs.DeleteKey(CacheKeys.Localization);
            PlayerPrefs.DeleteKey(CacheKeys.Levels);
            PlayerPrefs.DeleteKey(CacheKeys.Questions);
            PlayerPrefs.DeleteKey(CacheKeys.Theory);
            PlayerPrefs.DeleteKey(CacheKeys.LoadTimestamp);
            PlayerPrefs.Save();

            _localizationTable.Clear();
            _levels.Clear();
            _questions.Clear();
            _theoryCards.Clear();

            Debug.Log("[ContentLoaderService] Cache cleared");
        }
    }
}