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
            
            // Загружаем напрямую из ProfileLoaderService
            var profileLoaderService = GameManager.Instance?.GetService<ProfileLoaderService>();
            if (profileLoaderService != null)
            {
                var profile = profileLoaderService.GetProfile();
                if (profile != null)
                {
                    // Получаем прогресс по levelId
                    if (profile.LevelProgress.TryGetValue(_levelData.LevelId, out float savedProgress))
                    {
                        levelResult = savedProgress;
                        Debug.Log($"[LevelTileUI] Got progress from ProfileLoaderService: {_levelData.LevelId} = {levelResult}%");
                    }
                    
                    // Проверяем завершён ли уровень
                    isCompleted = profile.CompletedLevelIds.Contains(_levelData.LevelId);
                }
            }
            else
            {
                // Fallback: UserProfileService
                var userProfileService = GameManager.Instance?.GetService<UserProfileService>();
                if (userProfileService != null)
                {
                    var profile = userProfileService.GetProfile();
                    if (profile != null)
                    {
                        if (profile.LevelProgress.TryGetValue(_levelData.LevelId, out float savedProgress))
                        {
                            levelResult = savedProgress;
                            Debug.Log($"[LevelTileUI] Got progress from UserProfileService: {_levelData.LevelId} = {levelResult}%");
                        }
                        isCompleted = profile.CompletedLevelIds.Contains(_levelData.LevelId);
                    }
                }
            }
            
            // Обновляем UI
            Debug.Log($"[LevelTileUI] Updated Level '{_levelData.LevelName}' (ID: {_levelData.LevelId}) progress: {levelResult}%");
            
            if (_progressImage != null)
            {
                _progressImage.fillAmount = levelResult / 100f;
            }
            
            if (_progressText != null)
            {
                _progressText.text = $"{levelResult:F0}%";
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