using UnityEngine;
using HajjFlow.Gameplay;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Gameplay state for the Warmup (introductory) level.
    /// Handles quiz flow and progress tracking.
    /// UI is switched on Enter and cleaned up on Exit.
    /// </summary>
    public class WarmupLevelState : BaseLevelState
    {
        public override string StateId => GameStateIds.Warmup;

        private QuizSystem _quizSystem;
        private int _questionsAnswered;
        private int _totalQuestions;
        private float _lastScorePercent;

        public override void Enter()
        {
            base.Enter();

            // Show level UI via UIService (all UI switching happens inside states)
            GameManager.Instance?.uiService?.WarmUpLevelShow();

            // Find QuizSystem in scene
            _quizSystem = Object.FindObjectOfType<QuizSystem>();
            if (_quizSystem == null)
            {
                Debug.LogError("[WarmupLevelState] QuizSystem not found in scene!");
                return;
            }

            // Subscribe to events
            _quizSystem.OnQuestionReady += OnQuestionReady;
            _quizSystem.OnAnswerResult += OnAnswerResult;
            _quizSystem.OnQuizComplete += OnQuizComplete;

            // Initialize quiz
            _quizSystem.Initialise(_levelData);
            _questionsAnswered = 0;
            _lastScorePercent = 0f;
            _totalQuestions = _levelData?.Questions?.Length ?? 0;

            Debug.Log($"[WarmupLevelState] Starting warmup level with {_totalQuestions} questions");
        }

        public override void Exit()
        {
            // Save progress on exit (progress is tracked in the state machine flow)
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

        // ── Progress tracking ────────────────────────────────────────────────────

        private void SaveProgress()
        {
            var progressService = GameManager.Instance?.ProgressService;
            if (progressService == null || _levelData == null) return;

            progressService.RecordLevelProgress(_levelData.LevelId, _lastScorePercent, _levelData.PassThreshold);

            Debug.Log($"[WarmupLevelState] Progress saved: {_lastScorePercent:F1}%");
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void OnQuestionReady(QuizQuestion question, int questionNumber)
        {
            Debug.Log($"[WarmupLevelState] Question {questionNumber}/{_totalQuestions}: {question.QuestionText}");
        }

        private void OnAnswerResult(bool wasCorrect, string explanation)
        {
            _questionsAnswered++;

            if (wasCorrect)
            {
                Debug.Log($"[WarmupLevelState] Correct answer! {explanation}");
            }
            else
            {
                Debug.Log($"[WarmupLevelState] Wrong answer. {explanation}");
            }
        }

        private void OnQuizComplete(float scorePercent)
        {
            _lastScorePercent = scorePercent;
            Debug.Log($"[WarmupLevelState] Quiz completed with score: {scorePercent:F1}%");

            // Notify the state machine (will trigger transition to Results)
            _stateMachine?.CompleteLevel(scorePercent);
        }
    }
}

