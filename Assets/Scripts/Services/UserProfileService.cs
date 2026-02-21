using System;
using System.IO;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Services
{
    /// <summary>
    /// Handles loading and saving the UserProfile to local device storage using JSON.
    /// This is a mock implementation — no backend required.
    /// </summary>
    public class UserProfileService
    {
        private static readonly string SaveFileName = "user_profile.json";

        // Full path on the device's persistent data directory
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // Cached profile instance
        private UserProfile _profile;

        /// <summary>Returns the current in-memory profile, loading it if necessary.</summary>
        public UserProfile GetProfile()
        {
            if (_profile == null)
                _profile = Load();
            return _profile;
        }

        /// <summary>Persists the current profile to disk.</summary>
        public void Save()
        {
            if (_profile == null) return;
            try
            {
                string json = JsonUtility.ToJson(_profile, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[UserProfileService] Profile saved to {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserProfileService] Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates profile fields and immediately persists.
        /// Accepts an action delegate so callers can modify the profile in-place.
        /// </summary>
        public void UpdateProfile(Action<UserProfile> updateAction)
        {
            updateAction?.Invoke(GetProfile());
            Save();
        }

        // ── Private ─────────────────────────────────────────────────────────────

        private UserProfile Load()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    var profile = JsonUtility.FromJson<UserProfile>(json);
                    Debug.Log("[UserProfileService] Profile loaded from disk.");
                    return profile;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UserProfileService] Load failed, using defaults: {ex.Message}");
                }
            }

            // Return a fresh profile on first run
            Debug.Log("[UserProfileService] No save file found — creating default profile.");
            return new UserProfile();
        }
    }
}
