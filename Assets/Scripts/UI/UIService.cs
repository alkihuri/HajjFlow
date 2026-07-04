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
    /// Levels are loaded dynamically from <see cref="GameConfig"/> via <see cref="GameStateMachine"/>.
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
        [SerializeField] private GameTextController _levelTitleText;

        [SerializeField] private TextMeshProUGUI _gemsCounterText;

        // - level Controllers 
        [Header("Level Controllers")] [SerializeField]
        private Transform _levelsControllersContainer;

        [SerializeField] private LevelController _levelControllerPrefab;
        [SerializeField] private List<LevelController> _levelControllers = new List<LevelController>();

        private ProgressService _progressService;
        private List<LevelTileUI> _levelSelectButtons = new List<LevelTileUI>();

        /// <summary>Returns the levels list from GameConfig.</summary>
        private IReadOnlyList<LevelData> Levels
        {
            get
            {
                var sm = GameManager.Instance?.GetService<GameStateMachine>();
                return sm?.GameConfig?.Levels;
            }
        }


        private void Awake()
        {
            var levels = Levels;
            if (levels == null || levels.Count == 0)
            {
                Debug.LogWarning("[UIService] No levels found in GameConfig. Make sure GameStateMachine has GameConfig assigned.");
                return;
            }

            foreach (var level in levels)
            {
                // Instantiate a LevelController for each level and parent it under _levelsControllersContainer
                if (_levelControllerPrefab != null && _levelsControllersContainer != null)
                {
                    var controllerObj = Instantiate(_levelControllerPrefab, _levelsControllersContainer);
                    var controller = controllerObj.GetComponent<LevelController>();
                    if (controller != null)
                    {
                        controller.Init(level);
                        _levelControllers.Add(controller);
                    }
                }
            }
        }

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
            var levels = Levels;

            if (levels == null || !(levelIndex >= 0 && levelIndex < levels.Count))
            {
                Debug.LogWarning($"[UIService] Unknown level UI: {levelIndex}");
                return;
            }


            Debug.Log($"[UIService] Showing level index {levelIndex}");

            if (!_levelsUiRoot.activeInHierarchy)
                _levelsUiRoot.SetActive(true);

            foreach (var lvl in _levelControllers)
            {
                lvl.SetActive(false);
            }

            if (levelIndex < _levelControllers.Count && !_levelControllers[levelIndex].activeInHierarchy)
            {
                _levelControllers[levelIndex].SetActive(true);
            }
        }

        /// <summary>
        /// Shows the UI for a level by its state ID.
        /// Works dynamically — no hardcoded level names.
        /// </summary>
        public void ShowLevelByStateId(string stateId)
        {
            _mainMenuScreen?.SetActive(true);
            _gameStartScree?.SetActive(false);
            _levelSelect?.SetActive(false);

            var levels = Levels;
            if (levels == null) return;

            // Find level index dynamically by LevelId
            int levelIndex = -1;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].LevelId == stateId)
                {
                    levelIndex = i;
                    break;
                }
            }

            int levelNumber = levelIndex + 1; // levelNumber = 1-based

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

        /// <summary>Instantiates one tile button per LevelData entry from GameConfig.</summary>
        private void BuildLevelGrid()
        {
            var levels = Levels;
            if (levels == null || _levelButtonPrefab == null || _levelButtonsContainer == null)
            {
                Debug.LogWarning("[UIService] Missing references — cannot build level grid.");
                return;
            }

            foreach (var levelData in levels)
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
        /// Works with any dynamically configured level.
        /// </summary>
        public void ShowTheoryUI(string levelId)
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