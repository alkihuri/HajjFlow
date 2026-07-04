using System.Collections.Generic;
using System.Linq;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Constants for game-flow state identifiers and dynamic level state management.
    /// Level states are no longer hardcoded — they are registered dynamically from <see cref="GameConfig"/>.
    /// </summary>
    public static class GameStateIds
    {
        // ── Game-flow states (fixed) ────────────────────────────────────────────

        public const string MainMenu    = "main_menu";
        public const string LevelSelect = "level_select";
        public const string Results     = "results";

        // ── Dynamic level states ────────────────────────────────────────────────

        /// <summary>
        /// All level state IDs in progression order. Populated at runtime from GameConfig.
        /// </summary>
        public static readonly List<string> LevelStates = new List<string>();

        /// <summary>All state IDs (game-flow + level). Rebuilt when levels change.</summary>
        public static List<string> AllStates
        {
            get
            {
                var all = new List<string> { MainMenu, LevelSelect };
                all.AddRange(LevelStates);
                all.Add(Results);
                return all;
            }
        }

        /// <summary>
        /// Populates level state IDs from a GameConfig. Call once at startup.
        /// </summary>
        public static void InitializeFromConfig(GameConfig config)
        {
            LevelStates.Clear();
            if (config == null) return;

            foreach (var level in config.Levels)
            {
                if (!string.IsNullOrEmpty(level.LevelId))
                {
                    LevelStates.Add(level.LevelId);
                }
            }
        }

        /// <summary>
        /// Returns the next level state in the sequence, or null if at the end.
        /// </summary>
        public static string GetNextLevelState(string currentStateId)
        {
            int index = LevelStates.IndexOf(currentStateId);
            if (index < 0 || index >= LevelStates.Count - 1)
                return null;

            return LevelStates[index + 1];
        }

        /// <summary>
        /// Returns the previous level state in the sequence, or null if at the start.
        /// </summary>
        public static string GetPreviousLevelState(string currentStateId)
        {
            int index = LevelStates.IndexOf(currentStateId);
            if (index <= 0)
                return null;

            return LevelStates[index - 1];
        }

        /// <summary>Returns true when the id is a valid state id.</summary>
        public static bool IsValid(string stateId)
        {
            return stateId == MainMenu || stateId == LevelSelect || stateId == Results
                   || LevelStates.Contains(stateId);
        }

        /// <summary>Returns true when the id represents a gameplay level.</summary>
        public static bool IsLevelState(string stateId)
        {
            return LevelStates.Contains(stateId);
        }
    }

    // ── Backward compatibility ───────────────────────────────────────────────────

    /// <summary>
    /// Kept for backward compatibility. Use <see cref="GameStateIds"/> instead.
    /// Level IDs are now dynamic — these constants may not match actual configured levels.
    /// </summary>
    public static class LevelStateIds
    {
        public static readonly List<string> AllStates = GameStateIds.LevelStates;

        public static string GetNextState(string currentStateId) =>
            GameStateIds.GetNextLevelState(currentStateId);

        public static string GetPreviousState(string currentStateId) =>
            GameStateIds.GetPreviousLevelState(currentStateId);

        public static bool IsValid(string stateId) =>
            GameStateIds.IsLevelState(stateId);
    }
}

