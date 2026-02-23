using UnityEngine;
using HajjFlow.Services;
using HajjFlow.UI;

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
            DontDestroyOnLoad(gameObject); // Persist across scene loads

            InitialiseServices();
        }

        // ── Services (read-only public properties) ───────────────────────────────

        public UserProfileService ProfileService;
        public ProgressService ProgressService;
        public UIService uiService;
        public StageCompletionService stageCompletionService;
        public QuizService quizService;

        // ── Initialisation ───────────────────────────────────────────────────────

        private void InitialiseServices()
        {
            ProfileService = new UserProfileService();
            ProgressService = new ProgressService(ProfileService);
            if (uiService == null)
                uiService = FindFirstObjectByType<UIService>();
            
            // Инициализируем сервисы уровней
            if (stageCompletionService == null)
                stageCompletionService = gameObject.AddComponent<StageCompletionService>();
            
            if (quizService == null)
                quizService = gameObject.AddComponent<QuizService>();
            
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