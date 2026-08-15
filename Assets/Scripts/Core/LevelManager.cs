using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Core.States;
using HajjFlow.Services;
using Core;

namespace HajjFlow.Core
{
    /// <summary>
    /// Static helper that delegates navigation calls to the <see cref="GameStateMachine"/>.
    /// Kept for backward compatibility with existing UI scripts.
    /// All logic is now driven by the single <see cref="GameStateMachine"/>.
    /// </summary>
    public static class LevelManager
    {
        // ── Convenience accessors ────────────────────────────────────────────────

        private static GameStateMachine StateMachine =>
            GameManager.Instance?.GetService<GameStateMachine>();

        /// <summary>The level currently being played.</summary>
        public static LevelData ActiveLevel => StateMachine?.ActiveLevelData;

        /// <summary>The state ID for the current level.</summary>
        public static string ActiveStateId => StateMachine?.ActiveLevelStateId;

        // ── Navigation (delegates to GameStateMachine) ───────────────────────────

        /// <summary>Stores the chosen level and state, then transitions to the level state.</summary>
        public static void StartLevel(LevelData level, string stateId)
        {
            var sm = StateMachine;
            if (sm == null)
            {
                Debug.LogError("[LevelManager] GameStateMachine not available.");
                return;
            }

            sm.StartLevel(level, stateId);
        }

        /// <summary>Legacy overload — defaults to warmup state.</summary>
        public static void StartLevel(LevelData level)
        {
            StartLevel(level, GameStateIds.Warmup);
        }

        /// <summary>
        /// Запускает уровень по levelId, используя RuntimeLevelFactory для получения данных.
        /// Если удалённый контент недоступен — fallback на статические данные из GameMainConfig.
        /// </summary>
        public static void StartLevel(string levelId, string stateId = null)
        {
            var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();

            // Пробуем создать LevelData из рантайм-данных
            if (factory != null && factory.IsContentAvailable)
            {
                var levelData = factory.CreateLevelData(levelId);
                if (levelData != null)
                {
                    string resolvedStateId = stateId ?? GameStateIds.Warmup;
                    StartLevel(levelData, resolvedStateId);
                    return;
                }
            }

            // Fallback: ищем в статическом конфиге
            Debug.LogWarning($"[LevelManager] Falling back to static config for level: {levelId}");
            var config = GameManager.Instance?.GetService<GameMainConfig>();
            if (config != null)
            {
                var entry = config.GetLevelEntry(levelId);
                if (entry?.LevelData != null)
                {
                    string resolvedStateId = stateId ?? GameStateIds.Warmup;
                    StartLevel(entry.LevelData, resolvedStateId);
                    return;
                }
            }

            Debug.LogError($"[LevelManager] Could not start level '{levelId}' — no data found.");
        }

        /// <summary>Returns to the level-selection screen.</summary>
        public static void GoToLevelSelect()
        {
            StateMachine?.ChangeState(GameStateIds.LevelSelect);
        }

        /// <summary>Restarts the currently active level.</summary>
        public static void RestartLevel()
        {
            var sm = StateMachine;
            if (sm == null || sm.ActiveLevelData == null)
            {
                Debug.LogWarning("[LevelManager] Cannot restart — no active level.");
                return;
            }

            sm.StartLevel(sm.ActiveLevelData, sm.ActiveLevelStateId);
        }

        /// <summary>Loads the Results screen.</summary>
        public static void ShowResults()
        {
            StateMachine?.ChangeState(GameStateIds.Results);
        }

        /// <summary>Returns to the Main Menu.</summary>
        public static void GoToMainMenu()
        {
            StateMachine?.ChangeState(GameStateIds.MainMenu);
        }
    }
}

