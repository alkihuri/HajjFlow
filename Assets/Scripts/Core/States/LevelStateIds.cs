using System.Collections.Generic;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Constants for all game state identifiers.
    /// Use these instead of string literals to avoid typos.
    /// </summary>
    public static class GameStateIds
    {
        // ── Game-flow states ─────────────────────────────────────────────────────

        public const string MainMenu    = "main_menu";
        public const string LevelSelect = "level_select";
        public const string Results     = "results";

        // ── Level-gameplay states ────────────────────────────────────────────────

        public const string Warmup = "Warmup";
        public const string Miqat  = "Miqat";
        public const string Tawaf  = "Tawaf";
        public const string Sa3i   = "Sa3i";

        /// <summary>All level state IDs in progression order.</summary>
        public static readonly List<string> LevelStates = new List<string>
        {
            Warmup,
            Miqat,
            Tawaf,
            Sa3i
        };

        /// <summary>All state IDs.</summary>
        public static readonly List<string> AllStates = new List<string>
        {
            MainMenu,
            LevelSelect,
            Warmup,
            Miqat,
            Tawaf,
            Sa3i,
            Results
        };

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
            return AllStates.Contains(stateId);
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
    /// </summary>
    public static class LevelStateIds
    {
        public const string Warmup = GameStateIds.Warmup;
        public const string Miqat  = GameStateIds.Miqat;
        public const string Tawaf  = GameStateIds.Tawaf;
        public const string Sa3i   = GameStateIds.Sa3i;

        public static readonly List<string> AllStates = GameStateIds.LevelStates;

        public static string GetNextState(string currentStateId) =>
            GameStateIds.GetNextLevelState(currentStateId);

        public static string GetPreviousState(string currentStateId) =>
            GameStateIds.GetPreviousLevelState(currentStateId);

        public static bool IsValid(string stateId) =>
            GameStateIds.IsLevelState(stateId);
    }
}

