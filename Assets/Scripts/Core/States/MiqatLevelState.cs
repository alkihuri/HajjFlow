using UnityEngine;
using HajjFlow.Gameplay;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Gameplay state for the Miqat level.
    /// Includes speed bonuses and excellence rewards.
    /// UI is switched on Enter and cleaned up on Exit.
    /// </summary>
    public class MiqatLevelState : BaseLevelState
    {
        public override string StateId => GameStateIds.Miqat;

        private QuizSystem _quizSystem;
        private RewardSystem _rewardSystem;
        private int _questionsAnswered;
        private int _correctAnswers;
        private int _totalQuestions;
        private float _startTime;
        private float _lastScorePercent;

        public override void Enter()
        {
            base.Enter();

            // Show level UI (all UI switching happens inside states)
            GameManager.Instance?.uiService?.MiqatLevelShow();

            // Find systems in scene
            _quizSystem = Object.FindObjectOfType<QuizSystem>();
            _rewardSystem = Object.FindObjectOfType<RewardSystem>();

            if (_quizSystem == null)
            {
                Debug.LogError("[MiqatLevelState] QuizSystem not found in scene!");
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
            _lastScorePercent = 0f;
            _totalQuestions = _levelData?.Questions?.Length ?? 0;
            _startTime = Time.time;

            Debug.Log($"[MiqatLevelState] Starting Miqat level with {_totalQuestions} questions");
        }

        public override void Update()
        {
            float elapsedTime = Time.time - _startTime;

            if (elapsedTime > 300f) // 5 minutes
            {
                // Time warning placeholder
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
            Debug.Log("[MiqatLevelState] Level paused");
        }

        public override void OnResume()
        {
            base.OnResume();
            Debug.Log("[MiqatLevelState] Level resumed");
            _startTime += Time.unscaledDeltaTime;
        }

        // ── Progress tracking ────────────────────────────────────────────────────

        private void SaveProgress()
        {
            var progressService = GameManager.Instance?.ProgressService;
            if (progressService == null || _levelData == null) return;

            progressService.RecordLevelProgress(_levelData.LevelId, _lastScorePercent, _levelData.PassThreshold);

            Debug.Log($"[MiqatLevelState] Progress saved: {_lastScorePercent:F1}%");
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void OnQuestionReady(QuizQuestion question, int questionNumber)
        {
            Debug.Log($"[MiqatLevelState] Question {questionNumber}/{_totalQuestions}: {question.QuestionText}");
        }

        private void OnAnswerResult(bool wasCorrect, string explanation)
        {
            _questionsAnswered++;

            if (wasCorrect)
            {
                _correctAnswers++;
                Debug.Log($"[MiqatLevelState] Correct! ({_correctAnswers}/{_questionsAnswered})");

                // Speed bonus (answer within 3 minutes)
                float elapsedTime = Time.time - _startTime;
                if (elapsedTime < 180f)
                {
                    _rewardSystem?.AwardGems(2);
                    Debug.Log("[MiqatLevelState] Speed bonus: +2 gems");
                }
            }
            else
            {
                Debug.Log($"[MiqatLevelState] Wrong answer. {explanation}");
            }
        }

        private void OnQuizComplete(float scorePercent)
        {
            _lastScorePercent = scorePercent;
            float elapsedTime = Time.time - _startTime;
            Debug.Log($"[MiqatLevelState] Quiz completed with score: {scorePercent:F1}% in {elapsedTime:F0}s");

            // Excellence bonus
            if (scorePercent >= 90f && _levelData != null)
            {
                _rewardSystem?.AwardGems(_levelData.CompletionBonusGems / 2);
                Debug.Log("[MiqatLevelState] Excellence bonus awarded!");
            }

            // Notify the state machine (will trigger transition to Results)
            _stateMachine?.CompleteLevel(scorePercent);
        }
    }
}

