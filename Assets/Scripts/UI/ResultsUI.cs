using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HajjFlow.Core;
using HajjFlow.Data;

namespace HajjFlow.UI
{
    /// <summary>
    /// Controls the Results screen shown after a level attempt.
    ///
    /// Inspector wiring:
    ///   - LevelNameText   → TextMeshProUGUI
    ///   - ScoreText       → TextMeshProUGUI (e.g. "80%")
    ///   - GemsEarnedText  → TextMeshProUGUI (e.g. "+25 💎")
    ///   - PassFailText    → TextMeshProUGUI ("Well done!" or "Try again!")
    ///   - NextButton      → Button (go to next level or level select)
    ///   - RestartButton   → Button (replay this level)
    ///   - BackButton      → Button (back to level select)
    /// </summary>
    public class ResultsUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _levelNameText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _gemsEarnedText;
        [SerializeField] private TextMeshProUGUI _passFailText;

        [Header("Navigation")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _backButton;

        private void Start()
        {
            LevelData level   = LevelManager.ActiveLevel;
            var progressSvc   = GameManager.Instance?.ProgressService;

            if (level != null)
            {
                if (_levelNameText != null)
                    _levelNameText.text = level.LevelName;

                float score = progressSvc?.GetLevelProgress(level.LevelId) ?? 0f;

                if (_scoreText != null)
                    _scoreText.text = $"{score:F0}%";

                bool passed = score >= level.PassThreshold;
                if (_passFailText != null)
                    _passFailText.text = passed ? "Well done! 🎉" : "Keep trying! 💪";

                // Show gems earned during this session (total gems)
                int gems = GameManager.Instance?.ProfileService.GetProfile().Gems ?? 0;
                if (_gemsEarnedText != null)
                    _gemsEarnedText.text = $"💎 {gems}";
            }

            // Wire navigation buttons
            _nextButton?.onClick.AddListener(OnNextClicked);
            _restartButton?.onClick.AddListener(LevelManager.RestartLevel);
            _backButton?.onClick.AddListener(LevelManager.GoToLevelSelect);
        }

        private void OnDestroy()
        {
            _nextButton?.onClick.RemoveAllListeners();
            _restartButton?.onClick.RemoveAllListeners();
            _backButton?.onClick.RemoveAllListeners();
        }

        // ── Button handlers ──────────────────────────────────────────────────────

        private void OnNextClicked()
        {
            // In the MVP there is only one level — go back to level select
            LevelManager.GoToLevelSelect();
        }
    }
}
