using System;
using System.Collections.Generic;

namespace Manasik.Data
{
    /// <summary>
    /// Represents the local user profile stored on device.
    /// No backend required — data is serialised via UserProfileService.
    /// </summary>
    [Serializable]
    public class UserProfile
    {
        public string FirstName = "Player";
        public string LastName  = "";

        /// <summary>Overall progress across all levels (0–100 %).</summary>
        public float TotalProgress = 0f;

        /// <summary>Gem currency earned through rewards.</summary>
        public int Gems = 0;

        /// <summary>Set of level IDs the player has fully completed.</summary>
        public List<string> CompletedLevelIds = new List<string>();

        /// <summary>Per-level progress percentages keyed by level ID.</summary>
        public SerializableDictionary<string, float> LevelProgress =
            new SerializableDictionary<string, float>();

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// Minimal serialisable dictionary wrapper for Unity's JsonUtility.
    /// Stores keys and values as parallel lists.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        public List<TKey>   Keys   = new List<TKey>();
        public List<TValue> Values = new List<TValue>();

        public void Set(TKey key, TValue value)
        {
            int index = Keys.IndexOf(key);
            if (index >= 0)
                Values[index] = value;
            else
            {
                Keys.Add(key);
                Values.Add(value);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = Keys.IndexOf(key);
            if (index >= 0)
            {
                value = Values[index];
                return true;
            }
            value = default;
            return false;
        }

        public bool ContainsKey(TKey key) => Keys.Contains(key);
    }
}
