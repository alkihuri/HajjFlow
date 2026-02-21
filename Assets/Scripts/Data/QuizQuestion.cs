using System;

namespace HajjFlow.Data
{
    /// <summary>
    /// A single multiple-choice quiz question used inside a level.
    /// Stored as plain data so it can live inside a LevelData ScriptableObject.
    /// </summary>
    [Serializable]
    public class QuizQuestion
    {
        /// <summary>The question text shown to the player.</summary>
        public string QuestionText = "";

        /// <summary>Four possible answers. Index matches CorrectAnswerIndex.</summary>
        public string[] Options = new string[4];

        /// <summary>Zero-based index of the correct answer in Options.</summary>
        public int CorrectAnswerIndex = 0;

        /// <summary>Short explanation shown after the player answers.</summary>
        public string Explanation = "";

        /// <summary>Gems awarded for answering this question correctly.</summary>
        public int GemsReward = 5;
    }
}
