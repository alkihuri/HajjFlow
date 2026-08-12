using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HajjFlow.Data;
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
        public bool IsContentAvailable => _contentLoader != null && GetAllLevelInfos().Count > 0;

        /// <summary>
        /// Получить все доступные уровни (отсортированы по order).
        /// </summary>
        public List<ContentLoaderService.RuntimeLevelInfo> GetAllLevelInfos()
        {
            return _contentLoader?.GetAllLevels() ?? new List<ContentLoaderService.RuntimeLevelInfo>();
        }

        /// <summary>
        /// Получить информацию об уровне по levelId.
        /// </summary>
        public ContentLoaderService.RuntimeLevelInfo GetLevelInfo(string levelId)
        {
            return GetAllLevelInfos().FirstOrDefault(l => l.levelId == levelId);
        }

        /// <summary>
        /// Создать LevelData из рантайм-данных для указанного уровня.
        /// Это позволяет использовать существующий flow через LevelData без изменения GameStateMachine.
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

            // Генерируем вопросы
            levelData.Questions = BuildQuizQuestions(levelId);

            // Пытаемся загрузить thumbnail из Asset Bundles
            if (_assetBundleService != null && !string.IsNullOrEmpty(levelInfo.imageBundleKey))
            {
                levelData.Thumbnail = _assetBundleService.GetSprite(levelInfo.imageBundleKey);
            }

            Debug.Log($"[RuntimeLevelFactory] Created LevelData for '{levelId}': {levelData.Questions?.Length ?? 0} questions");
            return levelData;
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
        /// Возвращает количество доступных уровней.
        /// </summary>
        public int LevelCount => GetAllLevelInfos().Count;
    }
}
