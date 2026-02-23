using System;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Services
{
    /// <summary>
    /// Сервис для отслеживания и верификации прохождения блоков теории.
    /// Регистрируется в GameManager и контролирует завершение каждого блока.
    /// </summary>
    public class StageCompletionService : MonoBehaviour
    {
        /// <summary>Событие срабатывает при завершении блока теории</summary>
        public event Action<string, int> OnStageCompleted;
        
        /// <summary>Событие срабатывает при попытке завершить несуществующий блок</summary>
        public event Action<string> OnStageCompletionFailed;

        /// <summary>
        /// Проверяет и завершает блок теории по уровню и номеру блока.
        /// </summary>
        /// <param name="levelId">ID уровня (например: "Warmup", "Miqat", "Tawaf")</param>
        /// <param name="stageIndex">Индекс текущего блока теории (0, 1, 2...)</param>
        /// <returns>True если блок успешно завершён, False если ошибка</returns>
        public bool CompleteStage(string levelId, int stageIndex)
        {
            // Валидация параметров
            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogError("[StageCompletionService] LevelId is empty!");
                OnStageCompletionFailed?.Invoke("EmptyLevelId");
                return false;
            }

            if (stageIndex < 0)
            {
                Debug.LogError($"[StageCompletionService] Invalid stage index: {stageIndex}");
                OnStageCompletionFailed?.Invoke("InvalidStageIndex");
                return false;
            }

            // Верификация по типу уровня
            bool isValid = VerifyStageCompletion(levelId, stageIndex);

            if (isValid)
            {
                Debug.Log($"[StageCompletionService] Stage completed: {levelId} - Block {stageIndex}");
                OnStageCompleted?.Invoke(levelId, stageIndex);
                return true;
            }
            else
            {
                Debug.LogWarning($"[StageCompletionService] Stage verification failed: {levelId} - Block {stageIndex}");
                OnStageCompletionFailed?.Invoke($"{levelId}_{stageIndex}");
                return false;
            }
        }

        /// <summary>
        /// Верификация блока в зависимости от типа уровня.
        /// Каждый уровень может иметь свои требования для завершения.
        /// </summary>
        private bool VerifyStageCompletion(string levelId, int stageIndex)
        {
            return levelId switch
            {
                "Warmup" => VerifyWarmupStage(stageIndex),
                "Miqat" => VerifyMiqatStage(stageIndex),
                "Tawaf" => VerifyTawafStage(stageIndex),
                _ => false
            };
        }

        /// <summary>Верификация блока уровня Warmup (Подготовка к Хаджу)</summary>
        private bool VerifyWarmupStage(int stageIndex)
        {
            // Пример: Warmup может иметь 2-3 блока теории
            // stageIndex 0, 1, 2 - валидные индексы
            return stageIndex >= 0 && stageIndex < 3;
        }

        /// <summary>Верификация блока уровня Miqat (Микат)</summary>
        private bool VerifyMiqatStage(int stageIndex)
        {
            // Пример: Miqat может иметь 2 блока теории
            return stageIndex >= 0 && stageIndex < 2;
        }

        /// <summary>Верификация блока уровня Tawaf (Таваф)</summary>
        private bool VerifyTawafStage(int stageIndex)
        {
            // Пример: Tawaf может иметь 2 блока теории
            return stageIndex >= 0 && stageIndex < 2;
        }
    }
}

