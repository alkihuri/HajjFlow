using System;

namespace HajjFlow.Services
{
    [Serializable]
    public class RuntimeQuizQuestion
    {
        public string levelId;
        public string questionKey;
        public string[] optionKeys = new string[4];
        public int correctIndex;
        public string explanationKey;
        public int gemsReward;
    }
}