using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HajjFlow.Core;

namespace HajjFlow.UI
{
    /// <summary>
    /// Controls the Main Menu scene.
    /// Attach to a Canvas GameObject named "MainMenuCanvas".
    ///
    /// Inspector wiring required:
    ///   - TitleText     → TextMeshProUGUI showing app title
    ///   - PlayButton    → Button that launches level selection
    ///   - SettingsButton → Button (placeholder)
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _greetingText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton; // placeholder — not yet implemented

        private void Start()
        {
            // Display a personalised greeting using the stored profile
            var profile = GameManager.Instance?.ProfileService.GetProfile();
            if (profile != null && _greetingText != null)
                _greetingText.text = $"Welcome, {profile.FullName}!";

            // Wire up button listeners
            _playButton?.onClick.AddListener(OnPlayClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDestroy()
        {
            _playButton?.onClick.RemoveListener(OnPlayClicked);
            _settingsButton?.onClick.RemoveListener(OnSettingsClicked);
        }

        // ── Button handlers ──────────────────────────────────────────────────────

        private void OnPlayClicked()
        {
            LevelManager.GoToLevelSelect();
        }

        private void OnSettingsClicked()
        {
            // TODO: open settings panel
            Debug.Log("[MainMenuUI] Settings button pressed (not yet implemented).");
        }
    }
}
