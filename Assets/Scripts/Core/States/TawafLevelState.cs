using UnityEngine;
using HajjFlow.Gameplay;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Gameplay state for the Tawaf level.
    /// Includes streak bonuses and perfect-circle rewards.
    /// UI is switched on Enter and cleaned up on Exit.
    /// </summary>
    public class TawafLevelState : BaseLevelState
    {
        public override string StateId => GameStateIds.Tawaf;

        private QuizSystem _quizSystem;
        private RewardSystem _rewardSystem;
        private int _questionsAnswered;
        private int _correctAnswers;
        private int _totalQuestions;
        private int _consecutiveCorrect;
        private float _startTime;
        private float _lastScorePercent;

        public override void Enter()
        {
            base.Enter();

            // Show level UI (all UI switching happens inside states)
            GameManager.Instance?.uiService?.TawafLevelShow();

            // Find systems in scene
            _quizSystem = Object.FindObjectOfType<QuizSystem>();
            _rewardSystem = Object.FindObjectOfType<RewardSystem>();

            if (_quizSystem == null)
            {
                Debug.LogError("[TawafLevelState] QuizSystem not found in scene!");
                return;
            }

            // Subscribe to events
            _quizSystem.OnQuestionReady += OnQuestionReady;
            _quizSystem.OnAnswerResult += OnAnswerResult;
            _quizSystem.OnQuizComplete += OnQuizComplete;

            // Initialize
            _quizSystem.Initialise(_levelData);
            _questionsAnswered = 0;
            _correctAnswers = 0;
            _totalQuestions = _levelData?.Questions?.Length ?? 0;
            _consecutiveCorrect = 0;
            _lastScorePercent = 0f;
            _startTime = Time.time;

            Debug.Log($"[TawafLevelState] Starting Tawaf level with {_totalQuestions} questions");
        }

        public override void Update()
        {
            float elapsedTime = Time.time - _startTime;

            if (Mathf.FloorToInt(elapsedTime) % 60 == 0 && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[TawafLevelState] Time elapsed: {elapsedTime:F0}s");
            }
        }

        public override void Exit()
        {
            // Save progress on exit
            SaveProgress();

            // Unsubscribe from events
            if (_quizSystem != null)
            {
                _quizSystem.OnQuestionReady -= OnQuestionReady;
                _quizSystem.OnAnswerResult -= OnAnswerResult;
                _quizSystem.OnQuizComplete -= OnQuizComplete;
            }

            base.Exit();
        }

        public override void OnPause()
        {
            base.OnPause();
            Debug.Log("[TawafLevelState] Tawaf paused");
        }

        public override void OnResume()
        {
            base.OnResume();
            Debug.Log("[TawafLevelState] Tawaf resumed");
            _startTime += Time.unscaledDeltaTime;
        }

        // ── Progress tracking ────────────────────────────────────────────────────

        private void SaveProgress()
        {
            var progressService = GameManager.Instance?.ProgressService;
            if (progressService == null || _levelData == null) return;

            progressService.RecordLevelProgress(_levelData.LevelId, _lastScorePercent, _levelData.PassThreshold);

            // Perfect score bonus
            if (_lastScorePercent == 100f)
            {
                _rewardSystem?.AwardGems(50);
                Debug.Log("[TawafLevelState] Perfect Tawaf! +50 gems");
            }

            Debug.Log($"[TawafLevelState] Progress saved: {_lastScorePercent:F1}%");
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void OnQuestionReady(QuizQuestion question, int questionNumber)
        {
            Debug.Log($"[TawafLevelState] Question {questionNumber}/{_totalQuestions}: {question.QuestionText}");

            // Show Tawaf circle number (every 7 questions = 1 circle)
            int currentCircle = (questionNumber - 1) / 7 + 1;
            Debug.Log($"[TawafLevelState] Circle {currentCircle} of Tawaf");
        }

        private void OnAnswerResult(bool wasCorrect, string explanation)
        {
            _questionsAnswered++;

            if (wasCorrect)
            {
                _correctAnswers++;
                _consecutiveCorrect++;

                Debug.Log($"[TawafLevelState] Correct answer! Streak: {_consecutiveCorrect}");

                // Streak bonus (3+ consecutive correct)
                if (_consecutiveCorrect >= 3)
                {
                    int bonusGems = _consecutiveCorrect * 2;
                    _rewardSystem?.AwardGems(bonusGems);
                    Debug.Log($"[TawafLevelState] Streak bonus: +{bonusGems} gems");
                }

                // Perfect circle bonus (7 consecutive correct)
                if (_consecutiveCorrect == 7)
                {
                    _rewardSystem?.AwardGems(20);
                    Debug.Log("[TawafLevelState] Perfect circle completed! +20 bonus gems");
                }
            }
            else
            {
                _consecutiveCorrect = 0;
                Debug.Log($"[TawafLevelState] Wrong answer. Streak reset. {explanation}");
            }
        }

        private void OnQuizComplete(float scorePercent)
        {
            _lastScorePercent = scorePercent;
            float elapsedTime = Time.time - _startTime;
            Debug.Log($"[TawafLevelState] Tawaf completed: {scorePercent:F1}% in {elapsedTime:F0}s");

            // Notify the state machine (will trigger transition to Results)
            _stateMachine?.CompleteLevel(scorePercent);
        }
    }
}

