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
            if (_levelData == null)
            {
                Debug.LogWarning("[LevelTileUI] Cannot update - LevelData is null");
                return;
            }
            
            float levelResult = 0f;
            bool isCompleted = false;
            
            // Сначала пробуем получить данные из StageCompletionService (in-memory, текущая сессия)
            var stageCompletionService = GameManager.Instance?.GetService<StageCompletionService>();
            if (stageCompletionService != null && stageCompletionService.HasLevelResult(_levelData.LevelId))
            {
                levelResult = stageCompletionService.GetLevelPercent(_levelData.LevelId);
                Debug.Log($"[LevelTileUI] Got progress from StageCompletionService: {levelResult}%");
            }
            else
            {
                // Fallback: загружаем из ProgressService (persisted data)
                var progressService = GameManager.Instance?.GetService<ProgressService>();
                if (progressService != null)
                {
                    levelResult = progressService.GetLevelProgress(_levelData.LevelId);
                    isCompleted = progressService.IsLevelCompleted(_levelData.LevelId);
                    Debug.Log($"[LevelTileUI] Got progress from ProgressService: {levelResult}%, completed: {isCompleted}");
                }
                else
                {
                    // Последний fallback: UserProfileService напрямую
                    var userProfileService = GameManager.Instance?.GetService<UserProfileService>();
                    if (userProfileService != null)
                    {
                        var profile = userProfileService.GetProfile();
                        if (profile != null && profile.LevelProgress.TryGetValue(_levelData.LevelId, out float savedProgress))
                        {
                            levelResult = savedProgress;
                            isCompleted = profile.CompletedLevelIds.Contains(_levelData.LevelId);
                            Debug.Log($"[LevelTileUI] Got progress from UserProfileService: {levelResult}%");
                        }
                    }
                }
            }
            
            // Обновляем UI
            Debug.Log($"[LevelTileUI] Updated Level '{_levelData.LevelName}' progress: {levelResult}%");
            
            if (_progressImage != null)
            {
                _progressImage.fillAmount = levelResult / 100f;
            }
            
            if (_progressText != null)
            {
                _progressText.text = $"{levelResult:F1}%";
            }
            
            if (_completedBadge != null)
            {
                _completedBadge.SetActive(isCompleted || levelResult >= 60f);
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