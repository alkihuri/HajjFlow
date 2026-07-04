using System;
using System.Collections.Generic;
using UnityEngine;

namespace HajjFlow.Data
{
    /// <summary>
    /// Serializable data class that holds all configuration for a single game level.
    /// No longer a ScriptableObject — lives inside <see cref="GameConfig"/>.
    /// Levels are added/removed dynamically in a single GameConfig asset.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        [Header("Identity")]
        /// <summary>Unique identifier used to look up progress / completion state.</summary>
        public string LevelId = "";

        /// <summary>Display name shown in the UI (e.g. "Preparation for Hajj").</summary>
        public string LevelName = "";

        /// <summary>Short description shown on the level-selection map.</summary>
        [TextArea(2, 4)]
        public string Description = "";

        [Header("Localization Keys")]
        /// <summary>Localization key used to look up the level description via LocalizationService.</summary>
        public string LevelDescriptionKey = "";

        /// <summary>Localization keys for quiz question texts (parallel to Questions array).</summary>
        public List<string> QuestionTextKeys = new List<string>();

        [Header("Visuals")]
        /// <summary>Thumbnail displayed on the level-selection map tile.</summary>
        public Sprite Thumbnail;

        /// <summary>URL for remote image (used with Google Sheets integration). Cached locally after first download.</summary>
        public string ImageUrl = "";

        [Header("Theory")]
        /// <summary>Number of theory blocks this level requires before the quiz starts.</summary>
        public int TheoryBlockCount = 1;

        /// <summary>Path to TheoryCardContainer resource (e.g. "SO/Theory/Warmup/Warmup_TheoryContainer"). Set automatically or manually.</summary>
        public string TheoryContainerPath = "";

        [Header("Quiz")]
        /// <summary>Questions the player must answer to complete this level.</summary>
        public QuizQuestion[] Questions;

        /// <summary>Total number of questions configured for this level.</summary>
        public int QuestionCount => Questions != null ? Questions.Length : 0;

        [Header("Rewards")]
        /// <summary>Bonus gems awarded when the player completes the level for the first time.</summary>
        public int CompletionBonusGems = 20;

        /// <summary>Minimum percentage score needed to pass (0–100).</summary>
        [Range(0, 100)]
        public int PassThreshold = 60;

        /// <summary>Order index for sorting in level selection (lower = earlier).</summary>
        public int SortOrder = 0;
    }
}
