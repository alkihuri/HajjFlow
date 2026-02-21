using UnityEngine;
using UnityEngine.SceneManagement;
using HajjFlow.Data;

namespace HajjFlow.Core
{
    /// <summary>
    /// Manages the currently active level: holds a reference to the active LevelData,
    /// drives scene transitions, and broadcasts level-lifecycle events.
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

        // ── Scene-loading helpers ─────────────────────────────────────────────────

        /// <summary>Stores the chosen level then loads the Gameplay scene.</summary>
        public static void StartLevel(LevelData level)
        {
            ActiveLevel = level;
            Debug.Log($"[LevelManager] Starting level: {level.LevelName}");
            SceneManager.LoadScene(SceneGameplay);
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
            StartLevel(ActiveLevel);
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
            SceneManager.LoadScene(SceneMainMenu);
        }
    }
}
