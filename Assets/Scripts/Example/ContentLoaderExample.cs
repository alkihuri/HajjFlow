using System.Collections;
using UnityEngine;
using HajjFlow.Services;

namespace HajjFlow.Example
{
    /// <summary>
    /// Пример использования ContentLoaderService.
    /// Подключите этот скрипт к GameObject с ContentLoaderService.
    /// </summary>
    public class ContentLoaderExample : MonoBehaviour
    {
        private ContentLoaderService _contentLoader;

        private void Start()
        {
            _contentLoader = GetComponent<ContentLoaderService>();
            
            if (_contentLoader == null)
            {
                Debug.LogError("ContentLoaderService не найден!");
                return;
            }

            // Подпишитесь на события
            _contentLoader.OnLoadComplete += HandleLoadComplete;
            _contentLoader.OnLoadProgress += HandleLoadProgress;
        }

        private void HandleLoadComplete(bool success)
        {
            if (success)
            {
                Debug.Log("[Example] ✓ Контент успешно загружен!");
                
                // Выводим загруженные данные
                DisplayLoadedContent();
            }
            else
            {
                Debug.LogError("[Example] ✗ Ошибка загрузки контента");
            }
        }

        private void HandleLoadProgress(float progress)
        {
            Debug.Log($"[Example] Прогресс загрузки: {progress * 100:F1}%");
        }

        private void DisplayLoadedContent()
        {
            // Получаем все уровни
            var levels = _contentLoader.GetAllLevels();
            Debug.Log($"\n=== ЗАГРУЖЕННЫЕ УРОВНИ ({levels.Count}) ===");
            
            foreach (var level in levels)
            {
                Debug.Log($"  • {level.levelId}");
                Debug.Log($"    - Name Key: {level.nameKey}");
                Debug.Log($"    - Order: {level.order}");
                
                // Для каждого уровня получаем вопросы и теорию
                var questions = _contentLoader.GetQuestionsForLevel(level.levelId);
                var theory = _contentLoader.GetTheoryCardsForLevel(level.levelId);
                
                Debug.Log($"    - Questions: {questions.Count}");
                Debug.Log($"    - Theory cards: {theory.Count}");
            }

            // Пример: Получаем вопросы для первого уровня
            if (levels.Count > 0)
            {
                string firstLevelId = levels[0].levelId;
                var questions = _contentLoader.GetQuestionsForLevel(firstLevelId);
                
                Debug.Log($"\n=== ВОПРОСЫ ДЛЯ УРОВНЯ '{firstLevelId}' ===");
                foreach (var q in questions)
                {
                    Debug.Log($"  Q: {q.questionKey}");
                    Debug.Log($"    Options: {string.Join(", ", q.optionKeys)}");
                    Debug.Log($"    Correct: {q.correctIndex}");
                    Debug.Log($"    Gems: {q.gemsReward}");
                }

                // Пример: Получаем теорию для первого уровня
                var theory = _contentLoader.GetTheoryCardsForLevel(firstLevelId);
                Debug.Log($"\n=== ТЕОРИЯ ДЛЯ УРОВНЯ '{firstLevelId}' ===");
                foreach (var card in theory)
                {
                    Debug.Log($"  [{card.order}] {card.titleKey}");
                    Debug.Log($"      Text: {card.textKey}");
                }
            }

            // Пример: Получаем переведённый текст
            Debug.Log($"\n=== ПРИМЕРЫ ЛОКАЛИЗАЦИИ ===");
            Debug.Log($"'WARMUP_TITLE' (RU): {_contentLoader.GetLocalizedText("WARMUP_TITLE", "ru")}");
            Debug.Log($"'WARMUP_TITLE' (EN): {_contentLoader.GetLocalizedText("WARMUP_TITLE", "en")}");
        }

        /// <summary>
        /// Пример вручную запустить загрузку контента
        /// </summary>
        public void ManuallyLoadContent()
        {
            Debug.Log("[Example] Запуск ручной загрузки контента...");
            StartCoroutine(_contentLoader.LoadAllContent());
        }

        /// <summary>
        /// Пример очистить кэш и перезагрузить
        /// </summary>
        public void ClearCacheAndReload()
        {
            Debug.Log("[Example] Очистка кэша и перезагрузка...");
            _contentLoader.ClearCache();
            ManuallyLoadContent();
        }

        private void OnDestroy()
        {
            if (_contentLoader != null)
            {
                _contentLoader.OnLoadComplete -= HandleLoadComplete;
                _contentLoader.OnLoadProgress -= HandleLoadProgress;
            }
        }
    }
}

