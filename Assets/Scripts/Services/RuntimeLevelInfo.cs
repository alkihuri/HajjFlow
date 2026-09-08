using System;

namespace HajjFlow.Services
{
    [Serializable]
    public class RuntimeLevelInfo
    {
        public string levelId;
        public string nameKey;
        public string descriptionKey;
        public int order;
        public string imageBundleKey;
    }
}