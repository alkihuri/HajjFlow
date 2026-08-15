using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Core;
using Core.Theory;

namespace HajjFlow.Services
{
    /// <summary>
    /// Фабрика, которая генерирует рантайм-данные для уровней из ContentLoaderService.
    /// Преобразует RuntimeQuizQuestion → QuizQuestion[], RuntimeTheoryCard → TheoryCardData[].
    /// Привязывает изображения из AssetBundleService по ключу imageBundleKey.
    /// </summary>
    public class RuntimeLevelFactory
    {
        private readonly ContentLoaderService _contentLoader;
        private readonly AssetBundleService _assetBundleService;

        public RuntimeLevelFactory(ContentLoaderService contentLoader, AssetBundleService assetBundleService = null)
        {
            _contentLoader = contentLoader;
            _assetBundleService = assetBundleService;
        }

        /// <summary>
        /// Возвращает true если удалённый контент загружен и доступен.
        /// </summary>
        public bool IsContentAvailable
        {
            get
            {
                if (_contentLoader == null)
                {
                    Debug.LogWarning("[RuntimeLevelFactory] ContentLoaderService is null!");
                    return false;
                }

                var levels = _contentLoader.GetAllLevels();
                bool available = levels != null && levels.Count > 0;
                
                if (!available)
                    Debug.LogWarning("[RuntimeLevelFactory] No levels available from ContentLoaderService");
                
                return available;
            }
        }

        /// <summary>
        /// Получить все доступные уровни (отсортированы по order).
        /// Гарантирует что контент загружен перед возвратом.
        /// </summary>
        public List<ContentLoaderService.RuntimeLevelInfo> GetAllLevelInfos()
        {
            if (_contentLoader == null)
            {
                Debug.LogError("[RuntimeLevelFactory] ContentLoaderService is NULL! Cannot get levels.");
                return new List<ContentLoaderService.RuntimeLevelInfo>();
            }

            try
            {
                var levels = _contentLoader.GetAllLevels();
                
                if (levels == null || levels.Count == 0)
                {
                    Debug.LogWarning("[RuntimeLevelFactory] ContentLoaderService returned empty level list. Check if content is loaded.");
                    return new List<ContentLoaderService.RuntimeLevelInfo>();
                }

                Debug.Log($"[RuntimeLevelFactory] Successfully loaded {levels.Count} levels from ContentLoaderService");
                return levels;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RuntimeLevelFactory] Error getting levels: {ex.Message}");
                return new List<ContentLoaderService.RuntimeLevelInfo>();
            }
        }

        /// <summary>
        /// Получить информацию об уровне по levelId.
        /// </summary>
        public ContentLoaderService.RuntimeLevelInfo GetLevelInfo(string levelId)
        {
            return GetAllLevelInfos().FirstOrDefault(l => l.levelId == levelId);
        }

        /// <summary>
        /// Создаёт LevelData из рантайм-данных для указанного уровня.
        /// Это позволяет использовать существующий flow через LevelData без изменения GameStateMachine.
        /// Включает создание вопросов И карточек теории.
        /// </summary>
        public LevelData CreateLevelData(string levelId)
        {
            var levelInfo = GetLevelInfo(levelId);
            if (levelInfo == null)
            {
                Debug.LogWarning($"[RuntimeLevelFactory] Level not found: {levelId}");
                return null;
            }

            // Создаём временный LevelData (не сохраняется как ассет)
            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.LevelId = levelInfo.levelId;
            levelData.LevelName = levelInfo.nameKey;
            levelData.Description = levelInfo.descriptionKey;
            levelData.LevelDescriptionKey = levelInfo.descriptionKey;
            levelData.LevelIndex = levelInfo.order;

            // Генерируем вопросы для квиза
            levelData.Questions = BuildQuizQuestions(levelId);
            Debug.Log($"[RuntimeLevelFactory] Built {levelData.Questions?.Length ?? 0} quiz questions for '{levelId}'");

            // Генерируем контейнер с карточками теории
            var theoryContainer = BuildTheoryContainer(levelId);
            levelData.TheoryCardContainer = theoryContainer;
            Debug.Log($"[RuntimeLevelFactory] Built theory container with {theoryContainer.Cards.Count} cards for '{levelId}'");

            // Пытаемся загрузить thumbnail из Asset Bundles
            if (_assetBundleService != null && !string.IsNullOrEmpty(levelInfo.imageBundleKey))
            {
                levelData.Thumbnail = _assetBundleService.GetSprite(levelInfo.imageBundleKey);
            }

            Debug.Log($"[RuntimeLevelFactory] Created LevelData for '{levelId}': {levelData.Questions?.Length ?? 0} questions + {theoryContainer.Cards.Count} theory cards");
            return levelData;
        }

        /// <summary>
        /// Инициализирует карточки теории для уровня в TheoryCardsManager.
        /// Вызывается автоматически из LevelState при показе теории.
        /// </summary>
        public void InitializeTheoryForLevel(string levelId, TheoryCardsManager theoryManager)
        {
            if (theoryManager == null)
            {
                Debug.LogError($"[RuntimeLevelFactory] TheoryCardsManager is null!");
                return;
            }

            if (!IsContentAvailable)
            {
                Debug.LogWarning($"[RuntimeLevelFactory] Content is not loaded yet!");
                return;
            }

            Debug.Log($"[RuntimeLevelFactory] Initializing theory cards for '{levelId}'");
            theoryManager.InitializeFromRuntimeModels(levelId);
        }

        /// <summary>
        /// Собирает массив QuizQuestion[] для указанного уровня из рантайм-данных.
        /// </summary>
        public QuizQuestion[] BuildQuizQuestions(string levelId)
        {
            var runtimeQuestions = _contentLoader?.GetQuestionsForLevel(levelId);
            if (runtimeQuestions == null || runtimeQuestions.Count == 0)
            {
                Debug.LogWarning($"[RuntimeLevelFactory] No questions found for level: {levelId}");
                return new QuizQuestion[0];
            }

            var questions = new QuizQuestion[runtimeQuestions.Count];
            for (int i = 0; i < runtimeQuestions.Count; i++)
            {
                var rq = runtimeQuestions[i];
                questions[i] = new QuizQuestion
                {
                    QuestionText = rq.questionKey,
                    Options = rq.optionKeys != null ? (string[])rq.optionKeys.Clone() : new string[4],
                    CorrectAnswerIndex = rq.correctIndex,
                    Explanation = rq.explanationKey,
                    GemsReward = rq.gemsReward
                };
            }

            return questions;
        }

        /// <summary>
        /// Собирает массив TheoryCardData для указанного уровня из рантайм-данных.
        /// Создаёт ScriptableObject экземпляры в рантайме (не сохраняются как ассеты).
        /// </summary>
        public List<TheoryCardData> BuildTheoryCards(string levelId)
        {
            var runtimeCards = _contentLoader?.GetTheoryCardsForLevel(levelId);
            if (runtimeCards == null || runtimeCards.Count == 0)
            {
                Debug.LogWarning($"[RuntimeLevelFactory] No theory cards found for level: {levelId}");
                return new List<TheoryCardData>();
            }

            var cards = new List<TheoryCardData>();
            for (int i = 0; i < runtimeCards.Count; i++)
            {
                var rc = runtimeCards[i];
                var cardData = ScriptableObject.CreateInstance<TheoryCardData>();
                cardData.LevelId = rc.levelId;
                cardData.Title = rc.titleKey;
                cardData.Description = rc.textKey;

                // Пытаемся загрузить изображение из Asset Bundles
                if (_assetBundleService != null && !string.IsNullOrEmpty(rc.imageBundleKey))
                {
                    cardData.Image = _assetBundleService.GetSprite(rc.imageBundleKey);
                }

                cards.Add(cardData);
            }

            Debug.Log($"[RuntimeLevelFactory] Built {cards.Count} theory cards for '{levelId}'");
            return cards;
        }

        /// <summary>
        /// Создаёт TheoryCardContainer из рантайм-данных для указанного уровня.
        /// </summary>
        public TheoryCardContainer BuildTheoryContainer(string levelId)
        {
            var container = ScriptableObject.CreateInstance<TheoryCardContainer>();
            container.LevelId = levelId;
            container.Cards = BuildTheoryCards(levelId);
            return container;
        }

        /// <summary>
        /// Инициализирует вопросы квиза для уровня в QuizService.
        /// Вызывается автоматически из LevelState при показе квиза.
        /// </summary>
        public void InitializeQuizForLevel(string levelId)
        {
            if (!IsContentAvailable)
            {
                Debug.LogWarning($"[RuntimeLevelFactory] Content is not loaded yet!");
                return;
            }

            var quizService = GameManager.Instance?.GetService<QuizService>();
            if (quizService == null)
            {
                Debug.LogWarning($"[RuntimeLevelFactory] QuizService not found!");
                return;
            }

            Debug.Log($"[RuntimeLevelFactory] Initializing quiz for '{levelId}'");
            quizService.InitializeFromRuntimeQuestions(levelId);
        }

        /// <summary>
        /// Возвращает количество доступных уровней.
        /// </summary>
        public int LevelCount => GetAllLevelInfos().Count;

        /// <summary>
        /// Ждёт пока контент загрузится (корутина).
        /// Используйте перед первым обращением к GetAllLevelInfos().
        /// </summary>
        public System.Collections.IEnumerator WaitForContentLoad(int maxWaitSeconds = 30)
        {
            if (_contentLoader == null)
            {
                Debug.LogError("[RuntimeLevelFactory] ContentLoaderService is null!");
                yield break;
            }

            float elapsedTime = 0f;
            
            while (elapsedTime < maxWaitSeconds)
            {
                if (IsContentAvailable)
                {
                    Debug.Log($"[RuntimeLevelFactory] Content loaded successfully after {elapsedTime:F1} seconds");
                    yield break;
                }

                elapsedTime += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }

            Debug.LogError($"[RuntimeLevelFactory] Content loading timeout after {maxWaitSeconds} seconds!");
        }
    }
}
