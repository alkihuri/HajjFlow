using UnityEngine;
using HajjFlow.Core;
using HajjFlow.Data;
using HajjFlow.Services;

namespace HajjFlow.UI
{
    /// <summary>
    /// UI controller for the language-selection menu.
    /// Hook the public methods to UI buttons in the Inspector.
    /// </summary>
    public class LanguageSelectController : MonoBehaviour
    {
        public void OnRussianSelected()    => ChangeLanguage(Language.Russian);
        public void OnBosnianSelected()    => ChangeLanguage(Language.Bosnian);
        public void OnAlbanianSelected()   => ChangeLanguage(Language.Albanian);
        public void OnTurkishSelected()    => ChangeLanguage(Language.Turkish);
        public void OnArabicSelected()     => ChangeLanguage(Language.Arabic);
        public void OnIndonesianSelected() => ChangeLanguage(Language.Indonesian);

        private void ChangeLanguage(Language language)
        {
            var service = GameManager.Instance?.GetService<LocalizationService>();
            if (service != null)
                service.ChangeLanguage(language);
            else
                Debug.LogWarning("[LanguageSelectController] LocalizationService not found.");
        }
    }
}
