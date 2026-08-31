using System;
using System.Collections.Generic;
using System.Linq;
using Core;
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
        [SerializeField] private GameObject _loadingScreen;
        [SerializeField] private Button _resetProgressButton;

        [SerializeField] private GameObject _gameStartScree;
        [SerializeField] private GameObject _mainMenuScreen;
        [SerializeField] private GameObject _levelSelect;

        [SerializeField] private GameObject _levelsUiRoot;
        [SerializeField] private GameTextController _levelTitleText;

        [SerializeField] private TextMeshProUGUI _gemsCounterText;

        [SerializeField] RegistrationSceneUI _registrationSceneUI;


        
        /// <summary>
        ///  HARD CODED THUMBNAILS FOR LEVELS
        /// </summary>
        [System.Serializable]
        class LevelThumbnailData
        {
            public string LevelId;
            public Sprite Thumbnail;
        };
        
        [SerializeField] private List<LevelThumbnailData> _levelThumbnails = new List<LevelThumbnailData>();
      
      
        
        
        // - level Controllers 
        [Header("Level Controllers")] [SerializeField]
        private Transform _levelsControllersContainer;

        [SerializeField] private LevelController _levelControllerPrefab;
        [SerializeField] private List<LevelController> _levelControllers = new List<LevelController>();

        [Header("Main Configuration")] [SerializeField]
        private GameMainConfig _config;

        [SerializeField] private List<LevelData> _levels = new List<LevelData>();

        private ProgressService _progressService;
        private List<LevelTileUI> _levelSelectButtons = new List<LevelTileUI>();
        private bool _levelGridBuilt;


        private void Awake()
        {
            // Инициализируем контроллеры уровней из конфига
            InitializeLevelControllers(_config.Levels.Select(le => le.LevelData).ToList());
            ShowLoadingScrren();
        }

        public void ShowLoadingScrren()
        { 
            _loadingScreen?.SetActive(true);
        }

        public void HideLoadingScreen(bool load = true)
        {
            if (_loadingScreen != null && load)
                _loadingScreen.SetActive(false);
            else
            {
                Debug.LogWarning("[UIService] Loading screen is still active not correct loading data");
            }
            
        }

        /// <summary>
        /// Инициализирует LevelController'ы для списка уровней.
        /// Вызывается из Awake() или после загрузки runtime моделей.
        /// </summary>
        private void InitializeLevelControllers(List<LevelData> levels)
        {
            if (levels == null || levels.Count == 0)
            {
                Debug.LogWarning("[UIService] No levels provided for controller initialization");
                return;
            }

            _levels = levels;

            // Очищаем старые контроллеры если они есть
            foreach (var controller in _levelControllers)
            {
                if (controller != null)
                    Destroy(controller.gameObject);
            }

            _levelControllers.Clear();

            // Создаём новые контроллеры для каждого уровня
            foreach (var level in _levels)
            {
                if (_levelControllerPrefab != null && _levelsControllersContainer != null)
                {
                    var controllerObj = Instantiate(_levelControllerPrefab, _levelsControllersContainer);
                    var controller = controllerObj.GetComponent<LevelController>();
                    if (controller != null)
                    {
                        controller.Init(level);
                        _levelControllers.Add(controller);
                        Debug.Log($"[UIService] Created LevelController for '{level.LevelId}'");
                    }
                }
            }

            Debug.Log($"[UIService] Initialized {_levelControllers.Count} level controllers");
        }

        private void Start()
        {
            _progressService = GameManager.Instance?.ProgressService;

            _backButton?.onClick.AddListener(OnBackClicked);

            _backFromLevelsButton.onClick.AddListener(OnBackClicked);

            _gameStartButton?.onClick.AddListener(GameStartUI);

            _resetProgressButton?.onClick.AddListener(ResetGameProgress);

            _nextLevelButton?.onClick.AddListener(NextLevel);

            // Если используем remote контент - подписываемся на событие загрузки
            if (_config != null && _config.UseRemoteContent)
            {
                var contentLoader = GameManager.Instance?.GetService<ContentLoaderService>();
                if (contentLoader != null)
                {
                    Debug.Log("[UIService] Subscribing to ContentLoaderService.OnLoadComplete for BuildLevelGrid");
                    contentLoader.OnLoadComplete += (success) =>
                    {
                        if (success)
                        {
                            Debug.Log("[UIService] Content loaded, building level grid...");
                            BuildLevelGrid();
                        }
                        else
                        {
                            Debug.LogError("[UIService] Content loading failed!");
                        }
                    };

                    // A cached load completes synchronously in
                    // ContentLoaderService.Start(). In that case its event may
                    // have fired before this Start() method subscribes, so build
                    // the grid from the data that is already available.
                    var runtimeFactory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
                    if (runtimeFactory != null && runtimeFactory.IsContentAvailable)
                    {
                        Debug.Log("[UIService] Cached content is already available, building level grid...");
                        BuildLevelGrid();
                    }
                }
                else
                {
                    Debug.LogWarning("[UIService] UseRemoteContent is true but ContentLoaderService not found!");
                }
            }
            else
            {
                // Если используем static контент - сразу строим сетку
                Debug.Log("[UIService] Using static content, building level grid immediately");
                BuildLevelGrid();
            }
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

            if (!(levelIndex >= 0 && levelIndex < _levels.Count))
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

            if (!_levelControllers[levelIndex].activeInHierarchy)
            {
                _levelControllers[levelIndex].SetActive(true);
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

            // Находим индекс уровня в списке по LevelId
            int levelIndex = _levels.FindIndex(l => l.LevelId == stateId);
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

        [ContextMenu("Build Level Grid")]
        /// <summary>Instantiates one tile button per LevelData entry.</summary>
        private void BuildLevelGrid()
        {
            if (_levelGridBuilt)
            {
                Debug.Log("[UIService] Level grid is already built.");
                return;
            }

            if (_levelButtonPrefab == null || _levelButtonsContainer == null)
            {
                Debug.LogWarning("[UIService] Missing references — cannot build level grid.");
                return;
            }

            // Пытаемся получить данные уровней из RuntimeLevelFactory (удалённый контент)
            var runtimeFactory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
            bool useRuntime = _config != null && _config.UseRemoteContent && runtimeFactory != null;

            if (useRuntime)
            {
                Debug.Log("[UIService] BuildLevelGrid: using runtime level data from RuntimeLevelFactory");
                var runtimeLevelInfos = runtimeFactory.GetAllLevelInfos();
                var runtimeLevels = new List<LevelData>();

                foreach (var info in runtimeLevelInfos)
                {
                    var levelData = runtimeFactory.CreateLevelData(info.levelId);
                    if (levelData != null)
                    {
                        if (_levelThumbnails.Select(t => t.LevelId).Contains(info.levelId))
                        {
                            levelData.Thumbnail = _levelThumbnails.Find(t => t.LevelId == info.levelId).Thumbnail;
                        }
                        runtimeLevels.Add(levelData);
                    }
                }

                if (runtimeLevels.Count > 0)
                {
                    _levels = runtimeLevels;

                    // ✅ ВАЖНО: Инициализируем контроллеры для runtime моделей!
                    InitializeLevelControllers(_levels);
                }
                else
                {
                    Debug.LogWarning(
                        "[UIService] BuildLevelGrid: runtime data yielded no levels, falling back to static config");
                }
            }
            else
            {
                Debug.Log("[UIService] BuildLevelGrid: using static level data from GameMainConfig");
            }

            if (_levels == null || _levels.Count == 0)
            {
                Debug.LogWarning("[UIService] No levels available — cannot build level grid.");
                return;
            }

            _levels = _levels.OrderBy(l => l.LevelIndex).ToList();

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

            _levelGridBuilt = true;
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
            // load all LEvelData assets from the Resources/Levels folder if the list is empty (for convenience)
            if (_levels.Count == 0)
            {
                _levels = _config.Levels.Select(le => le.LevelData).ToList();
                if (_levels.Count == 0)
                {
                    Debug.LogWarning(
                        "[UIService] No levels found in configuration. Please assign levels in the GameMainConfig or place LevelData assets in Resources/SO/Levels.");
                    Debug.Log("[UIService] Attempting to load LevelData assets from Resources/SO/Levels...");
                    _levels = Resources.LoadAll<LevelData>("SO/Levels").ToList();
                }
            }
        }
#endif

        /// <summary>
        /// Инициализирует контроллеры из Runtime моделей (ContentLoaderService).
        /// Вызывается после загрузки контента из Google Sheets.
        /// </summary>
        public void InitializeControllersFromRuntime()
        {
            var runtimeFactory = GameManager.Instance?.GetService<RuntimeLevelFactory>();

            if (runtimeFactory == null)
            {
                Debug.LogError("[UIService] RuntimeLevelFactory service not found!");
                return;
            }

            if (!runtimeFactory.IsContentAvailable)
            {
                Debug.LogWarning(
                    "[UIService] Runtime content is not loaded yet. Wait for ContentLoaderService.OnLoadComplete");
                return;
            }

            var runtimeLevelInfos = runtimeFactory.GetAllLevelInfos();
            var runtimeLevels = new List<LevelData>();

            Debug.Log($"[UIService] Initializing controllers from {runtimeLevelInfos.Count} runtime levels...");

            foreach (var info in runtimeLevelInfos)
            {
                var levelData = runtimeFactory.CreateLevelData(info.levelId);
                if (levelData != null)
                {
                    runtimeLevels.Add(levelData);
                    Debug.Log($"[UIService] Created LevelData for '{info.levelId}' from runtime model");
                }
            }

            if (runtimeLevels.Count > 0)
            {
                Debug.Log($"[UIService] Initializing {runtimeLevels.Count} level controllers from runtime data");
                InitializeLevelControllers(runtimeLevels);
            }
            else
            {
                Debug.LogWarning("[UIService] No runtime levels were created!");
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

        /// <summary>
        /// Показывает блок теории для Warmup уровня.
        /// </summary>
        public void ShowWarmUpTheoryUI() => ShowTheoryUI(GameStateIds.Warmup);

        /// <summary>
        /// Показывает блок теории для Miqat уровня.
        /// </summary>
        public void ShowMiqatTheoryUI() => ShowTheoryUI(GameStateIds.Miqat);

        /// <summary>
        /// Показывает блок теории для Tawaf уровня.
        /// </summary>
        public void ShowTawafTheoryUI() => ShowTheoryUI(GameStateIds.Tawaf);

        /// <summary>
        /// Показывает блок теории для Sa3i уровня.
        /// </summary>
        public void ShowSa3iTheoryUI() => ShowTheoryUI(GameStateIds.Sa3i);

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

        
        [ContextMenu("Force Refresh Level Tile Buttons")]
        public void ForceRefreshLevelTileButtons()
        {
            UpdateLevelTileButtons(true);
        }
        public void UpdateLevelTileButtons(bool forceRefresh = false)
        {
            foreach (var tile in _levelSelectButtons)
            {
                tile.UpdateUiData(forceRefresh);
            }
        }

        public void ShowRegistrasionScreen(bool load)
        {
            if(load)
             _registrationSceneUI?.ShowRegistrationScreen();
        }
    }
}
