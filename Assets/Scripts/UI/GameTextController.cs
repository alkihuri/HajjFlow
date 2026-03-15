using System;
using UnityEngine;
using TMPro;
using HajjFlow.Core;
using HajjFlow.Services;

namespace HajjFlow.UI
{
    /// <summary>
    /// Attach to any GameObject with a <see cref="TMP_Text"/> component.
    /// Set <see cref="_localizationKey"/> in the Inspector.
    /// The text is automatically updated when the active language changes.
    /// </summary>
    public class GameTextController : MonoBehaviour
    {
        [SerializeField, Tooltip("Localization key from localization.csv (e.g. MIQAT_LEVEL_DESCRIPTION_KEY)")]
        private string _localizationKey;

        private TMP_Text _textComponent;
        private LocalizationService _service;

        public string text
        {
            set
            {
                _localizationKey = value;
                UpdateText();
            }
            get => _localizationKey;
        }


        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            _service = GameManager.Instance?.GetService<LocalizationService>();
            if (_service != null)
            {
                _service.Register(this);
            }
        } 

        private void OnEnable()
        {
            UpdateText();
        }

        

        private void OnDestroy()
        {
            var service = GameManager.Instance?.GetService<LocalizationService>();
            service?.Unregister(this);
        }

        /// <summary>
        /// Refreshes the displayed text from the <see cref="LocalizationService"/>.
        /// Called automatically on language change and during Awake.
        /// </summary>
        [ContextMenu("UpdateText")]
        public void UpdateText()
        {
            if (_textComponent == null)
                return;

            var service = GameManager.Instance?.GetService<LocalizationService>();
            if (service != null)
                _textComponent.text = service.GetText(_localizationKey);
        }
    }
}