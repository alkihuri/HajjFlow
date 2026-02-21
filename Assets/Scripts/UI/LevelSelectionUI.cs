using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HajjFlow.Core;
using HajjFlow.Core.States;
using HajjFlow.Data;
using HajjFlow.Services;

namespace HajjFlow.UI
{
    /// <summary>
    /// Controls the Level Selection scene (2.5D isometric map placeholder).
    /// Attach to the LevelSelectionCanvas.
    ///
    /// Inspector wiring:
    ///   - LevelButtonsContainer → Transform that holds level tile buttons
    ///   - LevelButtonPrefab     → Prefab with a LevelTileUI component
    ///   - BackButton            → Returns to Main Menu
    ///   - LevelDataList         → Array of LevelData assets assigned in the Inspector
    /// </summary>
    public class LevelSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform  _levelButtonsContainer;
        [SerializeField] private GameObject _levelButtonPrefab;
        [SerializeField] private Button     _backButton;

        [Header("Level Configuration")]
        [SerializeField] private LevelData[] _levels;

        private ProgressService _progressService;

        private void Start()
        {
            _progressService = GameManager.Instance?.ProgressService;

            _backButton?.onClick.AddListener(OnBackClicked);

            BuildLevelGrid();
        }

        private void OnDestroy()
        {
            _backButton?.onClick.RemoveListener(OnBackClicked);
        }

        // ── Private ──────────────────────────────────────────────────────────────

        /// <summary>Instantiates one tile button per LevelData entry.</summary>
        private void BuildLevelGrid()
        {
            if (_levels == null || _levelButtonPrefab == null || _levelButtonsContainer == null)
            {
                Debug.LogWarning("[LevelSelectionUI] Missing references — cannot build level grid.");
                return;
            }

            foreach (var levelData in _levels)
            {
                GameObject tile = Instantiate(_levelButtonPrefab, _levelButtonsContainer);
                var tileUI = tile.GetComponent<LevelTileUI>();
                if (tileUI != null)
                {
                    bool completed = _progressService?.IsLevelCompleted(levelData.LevelId) ?? false;
                    float progress = _progressService?.GetLevelProgress(levelData.LevelId) ?? 0f;
                    tileUI.Setup(levelData, completed, progress, OnLevelTileClicked);
                }
            }
        }

        private void OnLevelTileClicked(LevelData level)
        {
            // Определить StateId на основе LevelId
            string stateId = DetermineStateId(level.LevelId);
            LevelManager.StartLevel(level, stateId);
        }

        /// <summary>
        /// Определяет StateId на основе LevelId.
        /// Можно настроить логику определения состояния здесь.
        /// </summary>
        private string DetermineStateId(string levelId)
        {
            // Простая логика: если LevelId содержит имя состояния
            string id = levelId.ToLower();
            
            if (id.Contains("warmup") || id.Contains("warm") || id.Contains("1"))
                return LevelStateIds.Warmup;
            else if (id.Contains("miqat") || id.Contains("2"))
                return LevelStateIds.Miqat;
            else if (id.Contains("tawaf") || id.Contains("3"))
                return LevelStateIds.Tawaf;
            
            // По умолчанию warmup
            Debug.LogWarning($"[LevelSelectionUI] Unknown level id '{levelId}', defaulting to warmup");
            return LevelStateIds.Warmup;
        }

        private void OnBackClicked()
        {
            LevelManager.GoToMainMenu();
        }
    }
}
