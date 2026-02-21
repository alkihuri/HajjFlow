using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HajjFlow.Core;
using HajjFlow.Data;
using HajjFlow.Gameplay;

namespace HajjFlow.UI
{
    /// <summary>
    /// Controls the Gameplay scene HUD and quiz panel.
    ///
    /// Top bar layout:
    ///   [LevelName]   [Progress %]   [💎 Gems]
    ///
    /// Inspector wiring:
    ///   Top bar
    ///     - LevelNameText    → TextMeshProUGUI (top-left)
    ///     - ProgressText     → TextMeshProUGUI (top-center)
    ///     - GemsText         → TextMeshProUGUI (top-right)
    ///
    ///   Quiz panel
    ///     - QuestionText     → TextMeshProUGUI
    ///     - OptionButtons    → Array of 4 Buttons (A, B, C, D)
    ///     - FeedbackText     → TextMeshProUGUI (explanation / correct-wrong banner)
    ///     - NextButton       → Button (shown after an answer is given)
    ///
    ///   Navigation
    ///     - BackButton       → Button
    ///     - RestartButton    → Button
    ///
    ///   Systems
    ///     - QuizSystemRef    → QuizSystem component in this scene
    ///     - RewardSystemRef  → RewardSystem component in this scene
    /// </summary>
    public class GameplayUI : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI _levelNameText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _gemsText;

        [Header("Quiz Panel")]
        [SerializeField] private TextMeshProUGUI _questionText;
        [SerializeField] private Button[]   _optionButtons;    // exactly 4
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private Button          _nextButton;

        [Header("Navigation")]
        [SerializeField] private Button          _backButton;
        [SerializeField] private Button          _restartButton;

        [Header("Systems")]
        [SerializeField] private QuizSystem      _quizSystem;
        [SerializeField] private RewardSystem    _rewardSystem;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Start()
        {
            LevelData level = LevelManager.ActiveLevel;
            if (level == null)
            {
                Debug.LogError("[GameplayUI] No active level set in LevelManager.");
                return;
            }

            // Initialise top bar
            if (_levelNameText != null) _levelNameText.text = level.LevelName;
            RefreshGemsDisplay();
            UpdateProgressDisplay(GameManager.Instance?.ProgressService
                                              .GetLevelProgress(level.LevelId) ?? 0f);

            // Wire navigation buttons
            _backButton?.onClick.AddListener(LevelManager.GoToLevelSelect);
            _restartButton?.onClick.AddListener(LevelManager.RestartLevel);
            _nextButton?.onClick.AddListener(OnNextClicked);

            // Wire option buttons
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                int captured = i; // capture loop variable
                _optionButtons[i]?.onClick.AddListener(() => OnOptionSelected(captured));
            }

            // Wire reward system event
            if (_rewardSystem != null)
                _rewardSystem.OnGemsEarned += OnGemsEarned;

            // Wire quiz events and start
            if (_quizSystem != null)
            {
                _quizSystem.OnQuestionReady += OnQuestionReady;
                _quizSystem.OnAnswerResult  += OnAnswerResult;
                _quizSystem.OnQuizComplete  += OnQuizComplete;
                _quizSystem.Initialise(level);
            }

            // Hide feedback and next button until needed
            SetFeedbackVisible(false);
            _nextButton?.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _backButton?.onClick.RemoveAllListeners();
            _restartButton?.onClick.RemoveAllListeners();
            _nextButton?.onClick.RemoveAllListeners();
            foreach (var btn in _optionButtons)
                btn?.onClick.RemoveAllListeners();

            if (_quizSystem != null)
            {
                _quizSystem.OnQuestionReady -= OnQuestionReady;
                _quizSystem.OnAnswerResult  -= OnAnswerResult;
                _quizSystem.OnQuizComplete  -= OnQuizComplete;
            }

            if (_rewardSystem != null)
                _rewardSystem.OnGemsEarned -= OnGemsEarned;
        }

        // ── Quiz event handlers ──────────────────────────────────────────────────

        private void OnQuestionReady(QuizQuestion question, int questionNumber)
        {
            if (_questionText != null)
                _questionText.text = $"Q{questionNumber}: {question.QuestionText}";

            // Populate option buttons
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                bool hasOption = (i < question.Options.Length);
                _optionButtons[i]?.gameObject.SetActive(hasOption);
                if (hasOption)
                {
                    var label = _optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null) label.text = question.Options[i];
                }
                //_optionButtons[i].gameObject.interactable = true;
            }

            SetFeedbackVisible(false);
            _nextButton?.gameObject.SetActive(false);
        }

        private void OnAnswerResult(bool wasCorrect, string explanation)
        {
            // Disable option buttons while feedback is shown
            foreach (var btn in _optionButtons)
                if (btn != null) btn.interactable = false;

            if (_feedbackText != null)
            {
                _feedbackText.text  = wasCorrect
                    ? $"✓ Correct!\n{explanation}"
                    : $"✗ Incorrect.\n{explanation}";
                _feedbackText.color = wasCorrect ? Color.green : Color.red;
            }

            SetFeedbackVisible(true);
            _nextButton?.gameObject.SetActive(true);

            // Update progress display (approximate: questions answered / total)
            if (_quizSystem != null)
            {
                float pct = (float)_quizSystem.CurrentQuestionNumber /
                            _quizSystem.TotalQuestions * 100f;
                UpdateProgressDisplay(pct);
            }
        }

        private void OnQuizComplete(float scorePercent)
        {
            // Save progress, award completion bonus, then show results
            var level = LevelManager.ActiveLevel;
            if (level != null)
            {
                GameManager.Instance?.ProgressService
                           .RecordLevelProgress(level.LevelId, scorePercent, level.PassThreshold);

                if (scorePercent >= level.PassThreshold)
                    _rewardSystem?.GrantCompletionBonus(level);
            }

            LevelManager.ShowResults();
        }

        // ── Button handlers ──────────────────────────────────────────────────────

        private void OnOptionSelected(int index)
        {
            _quizSystem?.SubmitAnswer(index);
        }

        private void OnNextClicked()
        {
            _quizSystem?.Advance();
        }

        private void OnGemsEarned(int amount, int newTotal)
        {
            RefreshGemsDisplay();
        }

        // ── Display helpers ──────────────────────────────────────────────────────

        private void RefreshGemsDisplay()
        {
            int gems = GameManager.Instance?.ProfileService.GetProfile().Gems ?? 0;
            if (_gemsText != null) _gemsText.text = $"💎 {gems}";
        }

        private void UpdateProgressDisplay(float percent)
        {
            if (_progressText != null) _progressText.text = $"{percent:F0}%";
        }

        private void SetFeedbackVisible(bool visible)
        {
            _feedbackText?.gameObject.SetActive(visible);
        }
    }
}
