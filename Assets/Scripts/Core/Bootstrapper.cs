using UnityEngine;
using HajjFlow.Core.States;
using HajjFlow.Services;
using HajjFlow.UI;

namespace HajjFlow.Core
{
    /// <summary>
    /// Bootstrapper that wires all services into <see cref="GameManager"/> and verifies
    /// that every required service is present.
    /// Attach to a GameObject in the initial scene and assign references via the Inspector.
    ///
    /// Execution order: Bootstrapper.Awake runs AFTER GameManager.Awake (set via
    /// Script Execution Order or DefaultExecutionOrder attribute).
    /// </summary>
    [DefaultExecutionOrder(100)] // Run after GameManager (default 0)
    public class Bootstrapper : MonoBehaviour
    {
        // ── Inspector references ─────────────────────────────────────────────────

        [Header("MonoBehaviour Services (assign in Inspector)")]
        [SerializeField] private UIService _uiService;
        [SerializeField] private StageCompletionService _stageCompletionService;
        [SerializeField] private QuizService _quizService;
        [SerializeField] private GameStateMachine _gameStateMachine;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[Bootstrapper] GameManager.Instance is null! " +
                               "Make sure GameManager exists and runs before Bootstrapper.");
                return;
            }

            RegisterServices(gm);

            if (!VerifyServices(gm))
            {
                Debug.LogError("[Bootstrapper] Service verification failed. " +
                               "Check that all required references are assigned in the Inspector.");
                return;
            }

            // Start the game in the MainMenu state
            if (_gameStateMachine != null)
            {
                _gameStateMachine.ChangeState(GameStateIds.MainMenu);
            }

            Debug.Log("[Bootstrapper] All services registered and verified.");
        }

        // ── Registration ─────────────────────────────────────────────────────────

        private void RegisterServices(GameManager gm)
        {
            // Plain-C# services (created here)
            var profileService = new UserProfileService();
            gm.RegisterService(profileService);

            var progressService = new ProgressService(profileService);
            gm.RegisterService(progressService);

            // MonoBehaviour services (wired via Inspector)
            if (_uiService != null)
                gm.RegisterService(_uiService);

            if (_stageCompletionService != null)
                gm.RegisterService(_stageCompletionService);

            if (_quizService != null)
                gm.RegisterService(_quizService);

            if (_gameStateMachine != null)
                gm.RegisterService(_gameStateMachine);
        }

        // ── Verification ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when every required service is present in GameManager.
        /// Logs a warning for each missing service.
        /// </summary>
        private bool VerifyServices(GameManager gm)
        {
            bool allValid = true;

            allValid &= Verify<UserProfileService>(gm);
            allValid &= Verify<ProgressService>(gm);
            allValid &= Verify<UIService>(gm);
            allValid &= Verify<StageCompletionService>(gm);
            allValid &= Verify<QuizService>(gm);
            allValid &= Verify<GameStateMachine>(gm);

            return allValid;
        }

        private bool Verify<T>(GameManager gm) where T : class
        {
            if (gm.HasService<T>())
                return true;

            Debug.LogWarning($"[Bootstrapper] Missing required service: {typeof(T).Name}");
            return false;
        }
    }
}
