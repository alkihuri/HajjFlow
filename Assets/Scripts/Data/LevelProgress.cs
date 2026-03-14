using System.Collections.Generic;

namespace HajjFlow.Data
{
    /// <summary>
    /// Runtime model that tracks per-level progress.
    /// Stores a dictionary mapping level IDs to their progress percentages (0–100).
    /// </summary>
    public class LevelProgress
    {
        /// <summary>Per-level progress percentages keyed by level ID.</summary>
        private readonly Dictionary<string, float> _progressByLevelId = new Dictionary<string, float>();

        /// <summary>
        /// Sets or updates the progress percentage for a given level.
        /// </summary>
        /// <param name="levelId">Unique level identifier (must not be null or empty).</param>
        /// <param name="progressPercent">Progress value clamped to 0–100.</param>
        public void SetProgress(string levelId, float progressPercent)
        {
            if (string.IsNullOrEmpty(levelId)) return;

            if (progressPercent < 0f)   progressPercent = 0f;
            if (progressPercent > 100f) progressPercent = 100f;

            _progressByLevelId[levelId] = progressPercent;
        }

        /// <summary>
        /// Returns the stored progress for a level, or 0 if not found.
        /// </summary>
        public float GetProgress(string levelId)
        {
            return _progressByLevelId.TryGetValue(levelId, out float value) ? value : 0f;
        }

        /// <summary>
        /// Returns true if there is any recorded progress for the given level.
        /// </summary>
        public bool HasProgress(string levelId)
        {
            return _progressByLevelId.ContainsKey(levelId);
        }

        /// <summary>
        /// Returns a read-only view of all tracked level progress entries.
        /// </summary>
        public IReadOnlyDictionary<string, float> GetAll()
        {
            return _progressByLevelId;
        }

        /// <summary>
        /// Removes progress for a specific level.
        /// </summary>
        public bool RemoveProgress(string levelId)
        {
            return _progressByLevelId.Remove(levelId);
        }

        /// <summary>
        /// Clears all tracked progress.
        /// </summary>
        public void Clear()
        {
            _progressByLevelId.Clear();
        }
    }
}
