using UnityEngine;
using UnityEngine.SceneManagement;
using HajjFlow.Data;
using HajjFlow.Core.States;

namespace HajjFlow.Core
{
    /// <summary>
    /// Manages the currently active level using a State Machine pattern.
    /// Each level (Warmup, Miqat, Tawaf) is a separate state with Enter/Update/Exit lifecycle.
    /// Place one instance in the GameManager GameObject.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        // ── Scene name constants ─────────────────────────────────────────────────

        public const string SceneMainMenu      = "MainMenu";
        public const string SceneLevelSelect   = "LevelSelection";
        public const string SceneGameplay      = "Gameplay";
        public const string SceneResults       = "Results";

        // ── Runtime state ────────────────────────────────────────────────────────

        /// <summary>The level currently being played (set before loading the Gameplay scene).</summary>
        public static LevelData ActiveLevel { get; private set; }

        /// <summary>The state ID for the current level (warmup, miqat, tawaf).</summary>
        public static string ActiveStateId { get; private set; }

        /// <summary>Reference to the state machine (set when Gameplay scene loads).</summary>
        public static LevelStateMachine StateMachine { get; private set; }

        // ── Scene-loading helpers ─────────────────────────────────────────────────

        /// <summary>Stores the chosen level and state, then loads the Gameplay scene.</summary>
        public static void StartLevel(LevelData level, string stateId)
        {
            ActiveLevel = level;
            ActiveStateId = stateId;
            Debug.Log($"[LevelManager] Starting level: {level.LevelName} with state: {stateId}");
            SceneManager.LoadScene(SceneGameplay);
        }

        /// <summary>Legacy method - defaults to warmup state.</summary>
        public static void StartLevel(LevelData level)
        {
            StartLevel(level, "warmup");
        }

        /// <summary>
        /// Регистрирует машину состояний (вызывается из Gameplay сцены).
        /// </summary>
        public static void RegisterStateMachine(LevelStateMachine stateMachine)
        {
            StateMachine = stateMachine;
            
            // Запустить уровень с сохраненным состоянием
            if (ActiveLevel != null && !string.IsNullOrEmpty(ActiveStateId))
            {
                stateMachine.StartLevel(ActiveStateId, ActiveLevel);
            }
        }

        /// <summary>Returns to the level-selection map without saving additional progress.</summary>
        public static void GoToLevelSelect()
        {
            SceneManager.LoadScene(SceneLevelSelect);
        }

        /// <summary>Restarts the currently active level from scratch.</summary>
        public static void RestartLevel()
        {
            if (ActiveLevel == null)
            {
                Debug.LogWarning("[LevelManager] RestartLevel called but ActiveLevel is null.");
                return;
            }
            StartLevel(ActiveLevel, ActiveStateId);
        }

        /// <summary>Loads the Results screen after a level attempt finishes.</summary>
        public static void ShowResults()
        {
            SceneManager.LoadScene(SceneResults);
        }

        /// <summary>Returns to the Main Menu and clears the active level reference.</summary>
        public static void GoToMainMenu()
        {
            ActiveLevel = null;
            ActiveStateId = null;
            StateMachine = null;
            SceneManager.LoadScene(SceneMainMenu);
        }

        /// <summary>Unregisters the state machine (cleanup).</summary>
        public static void UnregisterStateMachine()
        {
            StateMachine = null;
        }
    }
}
