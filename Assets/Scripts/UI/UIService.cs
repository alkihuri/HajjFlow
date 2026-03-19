using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HajjFlow.Core;
using HajjFlow.Core.LevelsLogic;
using HajjFlow.Core.States;
using HajjFlow.Data;
using HajjFlow.Services;

namespace HajjFlow.UI
{
    /// <summary>
    /// Central UI service responsible for showing / hiding screen panels.
    /// Screen visibility is driven by the <see cref="GameStateMachine"/> states
    /// via the public Show… methods.
    /// </summary>
    public class UIService : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Transform _levelButtonsContainer;

        [SerializeField] private GameObject _levelButtonPrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _backFromLevelsButton;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _gameStartButton;
        [SerializeField] private Button _resetProgressButton;

        [SerializeField] private GameObject _gameStartScree;
        [SerializeField] private GameObject _mainMenuScreen;
        [SerializeField] private GameObject _levelSelect;

        [SerializeField] private GameObject _levelsUiRoot;
        [SerializeField] private GameObject[] _levelsUI;

        [SerializeField] private GameTextController _levelTitleText;

        [SerializeField] private TextMeshProUGUI _gemsCounterText;

        // - level Controllers 
        [Header("Level Controllers")] [SerializeField]
        private Transform _levelsControllersContainer;

        [SerializeField] private List<LevelController> _levelControllers = new List<LevelController>();

        [Header("Level Configuration")] [SerializeField]
        private List<LevelData> _levels = new List<LevelData>();

        private ProgressService _progressService;
        private List<LevelTileUI> _levelSelectButtons = new List<LevelTileUI>();

        private void Start()
        {
            _progressService = GameManager.Instance?.ProgressService;

            _backButton?.onClick.AddListener(OnBackClicked);

            _backFromLevelsButton.onClick.AddListener(OnBackClicked);

            _gameStartButton?.onClick.AddListener(GameStartUI);

            _resetProgressButton?.onClick.AddListener(ResetGameProgress);

            _nextLevelButton?.onClick.AddListener(NextLevel);

            BuildLevelGrid();
        }

        public void RegisterLevelData(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogWarning("[UIService] Attempted to register null LevelData");
                return;
            }

            if (!_levels.Contains(levelData))
                _levels.Add(levelData);
        }

        private void ResetGameProgress()
        {
            var userProfileService = GameManager.Instance?.GetService<UserProfileService>();
            if (userProfileService == null)
            {
                Debug.LogWarning("[UIService] UserProfileService not found, cannot reset progress");
                return;
            }

            userProfileService.ResetProgress();

            UpdateLevelTileButtons(true);
        }

        private void NextLevel()
        {
        }

        private void GameStartUI()
        {
            // Delegate to the state machine
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            sm?.ChangeState(GameStateIds.LevelSelect);
        }

        private void OnDestroy()
        {
            _backButton?.onClick.RemoveListener(OnBackClicked);
        }

        // ── Screen switching (called from game states) ───────────────────────────

        /// <summary>Shows the main-menu / game-start screen.</summary>
        public void ShowMainMenu()
        {
            _gameStartScree?.SetActive(true);
            _mainMenuScreen?.SetActive(true);
            _levelSelect?.SetActive(false);
            _levelsUiRoot?.SetActive(false);
        }

        /// <summary>Shows the level-selection screen.</summary>
        public void ShowLevelSelect()
        {
            _gameStartScree?.SetActive(false);
            _mainMenuScreen?.SetActive(false);
            _levelSelect?.SetActive(true);
            _levelsUiRoot?.SetActive(false);
        }

        /// <summary>Shows the results screen (placeholder — actual results UI is on a separate panel).</summary>
        public void ShowResults()
        {
            _gameStartScree?.SetActive(false);
            _mainMenuScreen?.SetActive(false);
            _levelSelect?.SetActive(false);
            _levelsUiRoot?.SetActive(false);

            Debug.Log("[UIService] Results screen shown");
        }

        // ── Level-specific screens ───────────────────────────────────────────────

        public void ShowLevel(int levelNumber)
        {
            int levelIndex = levelNumber - 1;

            Debug.Log($"[UIService] Showing level index {levelIndex}");

            if (!_levelsUiRoot.activeInHierarchy)
                _levelsUiRoot.SetActive(true);

            foreach (var lvl in _levelsUI)
            {
                lvl.SetActive(false);
            }

            if (levelIndex >= 0 && levelIndex < _levelsUI.Length)
            {
                _levelsUI[levelIndex].SetActive(true);
            }
        }

        /// <summary>
        /// Shows the UI for a level by its state ID.
        /// Replaces the per-level WarmUpLevelShow/MiqatLevelShow/TawafLevelShow methods.
        /// </summary>
        public void ShowLevelByStateId(string stateId)
        {
            _mainMenuScreen?.SetActive(true);
            _gameStartScree?.SetActive(false);
            _levelSelect?.SetActive(false);

            int levelNumber = stateId switch
            {
                GameStateIds.Warmup => 1,
                GameStateIds.Miqat => 2,
                GameStateIds.Tawaf => 3,
                _ => 0
            };

            if (levelNumber > 0)
            {
                ShowLevel(levelNumber);
            }
            else
            {
                Debug.LogWarning($"[UIService] Unknown state id for level UI: {stateId}");
            }
        }


        // ── Private ──────────────────────────────────────────────────────────────

        /// <summary>Instantiates one tile button per LevelData entry.</summary>
        private void BuildLevelGrid()
        {
            if (_levels == null || _levelButtonPrefab == null || _levelButtonsContainer == null)
            {
                Debug.LogWarning("[UIService] Missing references — cannot build level grid.");
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
                    _levelSelectButtons.Add(tileUI);
                }
            }
        }

        private void OnLevelTileClicked(LevelData level)
        {
            string stateId = level.LevelId;

            _levelTitleText.text = $"{level.LevelName}";

            // Delegate to the state machine instead of LevelManager directly
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            if (sm != null)
            {
                sm.StartLevel(level, stateId);
            }
            else
            {
                LevelManager.StartLevel(level, stateId);
            }
        }

        private void OnBackClicked()
        {
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            if (sm != null)
                sm.ChangeState(GameStateIds.MainMenu);
            else
                LevelManager.GoToMainMenu();
        }

        /// <summary>
        /// Сбрасывает состояние всех уровней.
        /// </summary>
        public void ResetUI()
        {
            Debug.Log("[UIService] Resetting all level UIs");

            foreach (var levelController in _levelControllers)
            {
                levelController.ResetLevel();
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_levelControllers.Count == 0 && _levelsControllersContainer != null)
            {
                _levelControllers = _levelsControllersContainer.GetComponentsInChildren<LevelController>(true)
                    .ToList();
            }
            // remove null entries that may have been left by prefab changes
            _levels = _levels.Where(l => l != null).ToList();
        }
#endif

        /// <summary>
        /// Получает контроллер уровня по его ID с ленивой инициализацией.
        /// </summary>
        private LevelController GetLevelController(string levelId)
        {
            // Ленивая инициализация - получаем контроллеры, если список пуст или контроллер не найден
            var controller = _levelControllers.Find(lc => lc.LevelId == levelId);

            if (controller == null && _levelsControllersContainer != null)
            {
                _levelControllers = _levelsControllersContainer.GetComponentsInChildren<LevelController>().ToList();
                controller = _levelControllers.Find(lc => lc.LevelId == levelId);
            }

            return controller;
        }

        /// <summary>
        /// Показывает блок теории для уровня по его ID.
        /// </summary>
        private void ShowTheoryUI(string levelId)
        {
            var controller = GetLevelController(levelId);

            if (controller != null)
            {
                controller.ShowTheory();
            }
            else
            {
                Debug.LogWarning($"[UIService] LevelController for '{levelId}' is null!");
            }
        }

        /// <summary>
        /// Показывает блок теории для Warmup уровня.
        /// </summary>
        public void ShowWarmUpTheoryUI() => ShowTheoryUI("Warmup");

        /// <summary>
        /// Показывает блок теории для Miqat уровня.
        /// </summary>
        public void ShowMiqatTheoryUI() => ShowTheoryUI("Miqat");

        /// <summary>
        /// Показывает блок теории для Tawaf уровня.
        /// </summary>
        public void ShowTawafTheoryUI() => ShowTheoryUI("Tawaf");

        public void UpdateGemsCounter(int gems, int totalGems = 0)
        {
            if (_gemsCounterText != null)
            {
                if (totalGems > 0)
                    _gemsCounterText.text = $"{gems} / {totalGems}";
                else
                    _gemsCounterText.text = $"{gems}";
            }
        }

        public void UpdateLevelTileButtons(bool forceRefresh = false)
        {
            foreach (var tile in _levelSelectButtons)
            {
                tile.UpdateUiData(forceRefresh);
            }
        }
    }
}