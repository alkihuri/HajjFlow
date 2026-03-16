using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Core;

namespace HajjFlow.Services
{
    /// <summary>
    /// Сервис для отслеживания и верификации прохождения блоков теории.
    /// Также хранит результаты прохождения уровней (score) для запроса по ключу.
    /// Регистрируется в GameManager и контролирует завершение каждого блока.
    /// </summary>
    public class StageCompletionService : MonoBehaviour
    {
        /// <summary>Событие срабатывает при завершении блока теории</summary>
        public event Action<string, int> OnStageCompleted;
        
        /// <summary>Событие срабатывает при попытке завершить несуществующий блок</summary>
        public event Action<string> OnStageCompletionFailed;

        // ── Level completion data ────────────────────────────────────────────────

        /// <summary>Stored level results keyed by levelId.</summary>
        private readonly Dictionary<string, LevelResult> _levelResults = new Dictionary<string, LevelResult>();

        
        //  DEBUG PURPOSE LEVEL RESUL SHOW IN INPECTOR
        [Header("Debug: Level Results")]
        public List<LevelResult> DebugLevelResults = new List<LevelResult>();
        
        /// <summary>
        /// Загружает сохранённые результаты уровней из UserProfileService.
        /// Вызывается при инициализации сервиса.
        /// </summary>
        public void LoadSavedResults()
        {
            var userProfileService = GameManager.Instance?.GetService<UserProfileService>();
            if (userProfileService == null)
            {
                Debug.LogWarning("[StageCompletionService] UserProfileService not found, cannot load saved results");
                return;
            }
            
            var profile = userProfileService.GetProfile();
            if (profile == null)
            {
                Debug.LogWarning("[StageCompletionService] Profile is null");
                return;
            }
            
            // Загружаем все сохранённые результаты уровней
            for (int i = 0; i < profile.LevelProgress.Keys.Count; i++)
            {
                string levelId = profile.LevelProgress.Keys[i];
                float progress = profile.LevelProgress.Values[i];
                
                // Добавляем только если ещё нет в памяти
                if (!_levelResults.ContainsKey(levelId))
                {
                    _levelResults[levelId] = new LevelResult
                    {
                        LevelId = levelId,
                        ScorePercent = progress,
                        CompletedAt = DateTime.MinValue // Нет точной даты из профиля
                    };
                    Debug.Log($"[StageCompletionService] Loaded saved result: {levelId} = {progress:F1}%");
                }
            }
            
            DebugLevelResults = _levelResults.Values.ToList();
            Debug.Log($"[StageCompletionService] Loaded {_levelResults.Count} saved level results");
        }
        
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

        // ── Level result storage ─────────────────────────────────────────────────

        /// <summary>
        /// Records (or overwrites) the completion result for a level.
        /// Called from BaseLevelState.SaveProgress().
        /// </summary>
        public void RecordLevelResult(string levelId, float scorePercent)
        {
            if (string.IsNullOrEmpty(levelId)) return;

            _levelResults[levelId] = new LevelResult
            {
                LevelId = levelId,
                ScorePercent = scorePercent,
                CompletedAt = DateTime.UtcNow
            };
            
            DebugLevelResults = _levelResults.Values.ToList(); // For inspector debugging

            Debug.Log($"[StageCompletionService] Level result recorded: {levelId} = {scorePercent:F1}%");
        }

        /// <summary>
        /// Returns the stored result for a level, or null if the level has never been completed.
        /// </summary>
        public LevelResult GetLevelResult(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            if (!_levelResults.TryGetValue(levelId, out var result))
            { 
                Debug.LogError($"[StageCompletionService] Level result not found: {levelId}");
            }
            
            return result;
        }

        /// <summary>
        /// Returns the stored score for a level, or 0 if never completed.
        /// </summary>
        public float GetLevelScore(string levelId)
        {
            return GetLevelResult(levelId)?.ScorePercent ?? 0f;
        }

        /// <summary>
        /// Returns true if the level has been completed at least once.
        /// </summary>
        public bool HasLevelResult(string levelId)
        {
            return !string.IsNullOrEmpty(levelId) && _levelResults.ContainsKey(levelId);
        }

        /// <summary>
        /// Clears the stored result for a level (useful for retries).
        /// </summary>
        public void ClearLevelResult(string levelId)
        {
            if (!string.IsNullOrEmpty(levelId))
                _levelResults.Remove(levelId);
        }

        // ── Verification ─────────────────────────────────────────────────────────

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
            return stageIndex >= 0 && stageIndex < 3;
        }

        /// <summary>Верификация блока уровня Miqat (Микат)</summary>
        private bool VerifyMiqatStage(int stageIndex)
        {
            return stageIndex >= 0 && stageIndex < 2;
        }

        /// <summary>Верификация блока уровня Tawaf (Таваф)</summary>
        private bool VerifyTawafStage(int stageIndex)
        {
            return stageIndex >= 0 && stageIndex < 2;
        }

        public float GetLevelPercent(string dataLevelId)
        {
            if (_levelResults.TryGetValue(dataLevelId, out var result))
            {
                return result.ScorePercent;
            }
            return 0f;
        }
    }

    /// <summary>
    /// Data class that stores a single level completion result.
    /// </summary>
    [Serializable]
    public class LevelResult
    {
        public string LevelId;
        public float ScorePercent;
        /// <summary>Completion timestamp in UTC.</summary>
        public DateTime CompletedAt;
    }
}

