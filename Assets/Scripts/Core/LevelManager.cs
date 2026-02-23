using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Core.States;

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

