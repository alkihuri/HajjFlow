using UnityEngine;

namespace HajjFlow.Data
{
    /// <summary>
    /// ScriptableObject that holds all configuration for a single game level.
    /// Create via: Assets → Create → Manasik → Level Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Manasik/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Unique identifier used to look up progress / completion state.</summary>
        public string LevelId = "";

        /// <summary>Display name shown in the UI (e.g. "Preparation for Hajj").</summary>
        public string LevelName = "";

        /// <summary>Short description shown on the level-selection map.</summary>
        [TextArea(2, 4)]
        public string Description = "";

        [Header("Visuals")]
        /// <summary>Thumbnail displayed on the level-selection map tile.</summary>
        public Sprite Thumbnail;

        [Header("Quiz")]
        /// <summary>Questions the player must answer to complete this level.</summary>
        public QuizQuestion[] Questions;

        [Header("Rewards")]
        /// <summary>Bonus gems awarded when the player completes the level for the first time.</summary>
        public int CompletionBonusGems = 20;

        /// <summary>Minimum percentage score needed to pass (0–100).</summary>
        [Range(0, 100)]
        public int PassThreshold = 60;
    }
}
