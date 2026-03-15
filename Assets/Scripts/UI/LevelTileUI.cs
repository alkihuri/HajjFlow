using System;
using HajjFlow.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HajjFlow.Data;
using HajjFlow.Services;

namespace HajjFlow.UI
{
    /// <summary>
    /// Represents a single level tile on the selection map.
    /// Attach to the LevelButtonPrefab.
    ///
    /// Inspector wiring:
    ///   - LevelNameText  → TextMeshProUGUI
    ///   - ProgressText   → TextMeshProUGUI ("32 %")
    ///   - Thumbnail      → Image
    ///   - CompletedBadge → GameObject shown when the level is completed
    ///   - SelectButton   → Button
    /// </summary>
    public class LevelTileUI : MonoBehaviour
    {
        [SerializeField] private GameTextController _levelNameText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private Image _thumbnail;
        [SerializeField] private GameObject _completedBadge;
        [SerializeField] private Button _selectButton;

        [SerializeField] private Image _progressImage;

        private Action<LevelData> _onSelected;
        private LevelData _levelData;


        [ContextMenu("Update UI Data")]
        public void UpdateUiData()
        { 
            var stageCompletionService = GameManager.Instance?.GetService<StageCompletionService>();
            if (stageCompletionService != null && _levelData != null)
            {
                var levelResult = stageCompletionService.GetLevelPercent(_levelData.LevelId);
                Debug.Log($"[LevelTileUI] Updated Level '{_levelData.LevelName}' progress: {levelResult}%");
                if (_progressImage != null)
                {
                    _progressImage.fillAmount = levelResult / 100f;
                }
                _progressText.text = $"{levelResult:F1}%";
            }
        }

        /// <summary>Populates the tile with level information.</summary>
        public void Setup(LevelData data, bool isCompleted, float progressPercent,
            Action<LevelData> onSelected)
        {
           

            _levelData = data;
            _onSelected = onSelected;

            if (_levelNameText != null)
            {
                var locService = GameManager.Instance?.GetService<LocalizationService>();
                if (locService != null && !string.IsNullOrEmpty(data.LevelDescriptionKey))
                    _levelNameText.text = locService.GetText(data.LevelDescriptionKey);
                else
                    _levelNameText.text = data.LevelName;
            }

            if (_progressText != null)
                _progressText.text = $"{progressPercent:F0}%";

            if (_thumbnail != null && data.Thumbnail != null)
                _thumbnail.sprite = data.Thumbnail;

            if (_completedBadge != null)
                _completedBadge.SetActive(isCompleted);

            _selectButton?.onClick.AddListener(OnSelectClicked);

             
        }

        private void OnDestroy()
        {
            _selectButton?.onClick.RemoveListener(OnSelectClicked);
        }

        private void OnSelectClicked()
        {
            _onSelected?.Invoke(_levelData);
        }
    }
}