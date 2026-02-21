using UnityEngine;
using HajjFlow.Services;

namespace HajjFlow.Core
{
    /// <summary>
    /// Central singleton that owns shared services and survives scene loads.
    /// Access via GameManager.Instance from any other script.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Persist across scene loads

            InitialiseServices();
        }

        // ── Services (read-only public properties) ───────────────────────────────

        public UserProfileService ProfileService  { get; private set; }
        public ProgressService    ProgressService { get; private set; }

        // ── Initialisation ───────────────────────────────────────────────────────

        private void InitialiseServices()
        {
            ProfileService  = new UserProfileService();
            ProgressService = new ProgressService(ProfileService);
            Debug.Log("[GameManager] Services initialised.");
        }

        // ── Convenience helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Adds gems to the player's profile and saves immediately.
        /// Call from RewardSystem or any other gem-granting code.
        /// </summary>
        public void AddGems(int amount)
        {
            ProfileService.UpdateProfile(p => p.Gems += amount);
            Debug.Log($"[GameManager] +{amount} gems. Total: {ProfileService.GetProfile().Gems}");
        }
    }
}
