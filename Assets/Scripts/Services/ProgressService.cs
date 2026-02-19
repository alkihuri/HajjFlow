using System.Collections.Generic;
using Manasik.Data;

namespace Manasik.Services
{
    /// <summary>
    /// Tracks and calculates progress across levels.
    /// Works in conjunction with UserProfileService to persist state.
    /// </summary>
    public class ProgressService
    {
        private readonly UserProfileService _profileService;

        public ProgressService(UserProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>
        /// Records progress for a specific level (0–100 %).
        /// Marks the level as completed when progress reaches the pass threshold.
        /// </summary>
        public void RecordLevelProgress(string levelId, float progressPercent, int passThreshold = 60)
        {
            _profileService.UpdateProfile(profile =>
            {
                profile.LevelProgress.Set(levelId, progressPercent);

                if (progressPercent >= passThreshold &&
                    !profile.CompletedLevelIds.Contains(levelId))
                {
                    profile.CompletedLevelIds.Add(levelId);
                }

                profile.TotalProgress = CalculateTotalProgress(profile);
            });
        }

        /// <summary>Returns the stored progress percentage for a level (0 if not started).</summary>
        public float GetLevelProgress(string levelId)
        {
            var profile = _profileService.GetProfile();
            return profile.LevelProgress.TryGetValue(levelId, out float pct) ? pct : 0f;
        }

        /// <summary>Returns true if the given level has been marked as completed.</summary>
        public bool IsLevelCompleted(string levelId)
        {
            return _profileService.GetProfile().CompletedLevelIds.Contains(levelId);
        }

        // ── Private ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes overall progress as the average of all recorded level percentages.
        /// Returns 0 when no levels have been started.
        /// </summary>
        private float CalculateTotalProgress(UserProfile profile)
        {
            var values = profile.LevelProgress.Values;
            if (values == null || values.Count == 0) return 0f;

            float sum = 0f;
            foreach (float v in values) sum += v;
            return sum / values.Count;
        }
    }
}
