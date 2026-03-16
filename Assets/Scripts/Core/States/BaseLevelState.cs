using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Gameplay;
using HajjFlow.Services;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Base class for level-gameplay states (Warmup, Miqat, Tawaf).
    /// Manages the full level lifecycle: theory blocks → quiz → completion.
    /// All common quiz/progress logic lives here; child states add only level-specific bonuses.
    /// </summary>
    public abstract class BaseLevelState : BaseGameState
    {
        protected LevelData _levelData;

        // ── Quiz tracking ────────────────────────────────────────────────────────
        protected QuizSystem _quizSystem;
        protected RewardSystem _rewardSystem;
        protected int _questionsAnswered;
        protected int _correctAnswers;
        protected int _totalQuestions;
        protected float _lastScorePercent;

        // ── Theory stage tracking ────────────────────────────────────────────────
        protected int _currentStageIndex;

        /// <summary>Number of theory blocks this level requires before the quiz.</summary>
        protected abstract int TheoryBlockCount { get; }

        /// <summary>
        /// Initializes the state with both the state machine and level-specific data.
        /// Called by <see cref="GameStateMachine.ChangeState(string, LevelData)"/>.
        /// </summary>
        public virtual void InitializeWithLevel(GameStateMachine stateMachine, LevelData levelData)
        {
            base.Initialize(stateMachine);
            _levelData = levelData;
        }

        public override void Enter()
        {
            Debug.Log($"[{StateId}] Entering state: {_levelData?.LevelName ?? "Unknown"}");

            // Reset all runtime state for clean replay
            ResetLevelState();

            // Show level-specific UI
            ShowLevelUI();

            // Find scene systems
            _quizSystem = Object.FindObjectOfType<QuizSystem>();
            _rewardSystem = Object.FindObjectOfType<RewardSystem>();

            if (_quizSystem == null)
            {
                Debug.LogError($"[{StateId}] QuizSystem not found in scene!");
                return;
            }

            // Subscribe to quiz events
            _quizSystem.OnQuestionReady += HandleQuestionReady;
            _quizSystem.OnAnswerResult += HandleAnswerResult;
            _quizSystem.OnQuizComplete += HandleQuizComplete;

            // Initialize quiz
            _quizSystem.Initialise(_levelData);
            _totalQuestions = _levelData?.Questions?.Length ?? 0;

            Debug.Log($"[{StateId}] Started with {_totalQuestions} questions");
        }

        public override void Exit()
        {
            SaveProgress();
            UnsubscribeQuizEvents();
            Debug.Log($"[{StateId}] Exiting state");
        }

        // ── UI ───────────────────────────────────────────────────────────────────

        /// <summary>Called on Enter to show level-specific UI panels.</summary>
        protected abstract void ShowLevelUI();

        // ── Theory stages ────────────────────────────────────────────────────────

        /// <summary>
        /// Call from scene (e.g. StageGameplayController) when a theory block is done.
        /// Verifies via StageCompletionService and advances or starts quiz.
        /// </summary>
        public void CompleteTheoryStage()
        {
            var stageService = GameManager.Instance?.stageCompletionService;
            if (stageService != null)
            {
                stageService.CompleteStage(_levelData?.LevelId ?? StateId, _currentStageIndex);
            }

            _currentStageIndex++;

            if (_currentStageIndex < TheoryBlockCount)
            {
                Debug.Log($"[{StateId}] Theory block {_currentStageIndex}/{TheoryBlockCount}");
            }
            else
            {
                Debug.Log($"[{StateId}] All theory blocks completed, starting quiz");
                StartQuiz();
            }
        }


        /// <summary>Starts the quiz portion of the level.</summary>
        protected virtual void StartQuiz()
        {
            if (_levelData == null || _levelData.Questions == null || _levelData.Questions.Length == 0)
            {
                Debug.LogError($"[{StateId}] No questions loaded for quiz!");
                return;
            }

            var quizService = GameManager.Instance?.quizService;
            if (quizService != null)
            {
                quizService.InitializeQuiz(_levelData.Questions);
            }

            Debug.Log($"[{StateId}] Quiz started with {_levelData.Questions.Length} questions");
        }

        // ── Reset ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resets all runtime state so the level can be replayed without scene reload.
        /// </summary>
        public void ResetLevelState()
        {
            _questionsAnswered = 0;
            _correctAnswers = 0;
            _totalQuestions = 0;
            _lastScorePercent = 0f;
            _currentStageIndex = 0;
        }

        // ── Progress ─────────────────────────────────────────────────────────────

        protected virtual void SaveProgress()
        {
            var progressService = GameManager.Instance?.ProgressService;
            var quizService = GameManager.Instance?.quizService;
            if (progressService == null || _levelData == null) return;

            progressService.RecordLevelProgress(
                _levelData.LevelId, _lastScorePercent, _levelData.PassThreshold);

            // Store in StageCompletionService for query by level key
            var stageService = GameManager.Instance?.stageCompletionService;
            _lastScorePercent = quizService.GetLastScorePercent();
            stageService?.RecordLevelResult(_levelData.LevelId, _lastScorePercent);


            var profileLoader = GameManager.Instance?.profileLoaderService;
            var profile = GameManager.Instance?.ProfileService.GetProfile();

            // get current level id from StageCompletionService or fallback to ProgressService

            profile.CompletedLevelIds.Add(_levelData.LevelId);

            if (profileLoader != null)
            {
                profileLoader.Save(profile); 
                Debug.Log($"[{StateId}] Profile saved: {profile.CompletedLevelIds.Count}");
            }
            else
            {
                 Debug.Log($"[{StateId}] No profile loaded");
            }


            Debug.Log($"[{StateId}] Progress saved: {_lastScorePercent:F1}%");
        }

        // ── Quiz event handlers (overridable for bonuses) ────────────────────────

        protected virtual void HandleQuestionReady(QuizQuestion question, int questionNumber)
        {
            Debug.Log($"[{StateId}] Question {questionNumber}/{_totalQuestions}: {question.QuestionText}");
        }

        /// <summary>
        /// Base handler for answer results. Tracks correct count.
        /// Override in child states to add bonuses, but always call base.
        /// </summary>
        protected virtual void HandleAnswerResult(bool wasCorrect, string explanation)
        {
            _questionsAnswered++;

            if (wasCorrect)
            {
                _correctAnswers++;
                Debug.Log($"[{StateId}] Correct! ({_correctAnswers}/{_questionsAnswered})");
            }
            else
            {
                Debug.Log($"[{StateId}] Wrong answer. {explanation}");
            }
        }

        /// <summary>
        /// Base handler for quiz completion. Saves score and signals state machine.
        /// Override in child states for completion bonuses, but always call base.
        /// </summary>
        protected virtual void HandleQuizComplete(float scorePercent)
        {
            _lastScorePercent = scorePercent;
            Debug.Log($"[{StateId}] Quiz completed with score: {scorePercent:F1}%");
            _stateMachine?.CompleteLevel(scorePercent);
        }

        // ── Cleanup ──────────────────────────────────────────────────────────────

        private void UnsubscribeQuizEvents()
        {
            if (_quizSystem != null)
            {
                _quizSystem.OnQuestionReady -= HandleQuestionReady;
                _quizSystem.OnAnswerResult -= HandleAnswerResult;
                _quizSystem.OnQuizComplete -= HandleQuizComplete;
            }
        }
    }
}