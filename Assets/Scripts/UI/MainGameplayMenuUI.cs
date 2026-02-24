using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HajjFlow.Core;
using HajjFlow.Core.States;

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
        
    }
}
