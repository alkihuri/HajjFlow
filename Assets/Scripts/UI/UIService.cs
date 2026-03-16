using System.Collections.Generic;
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
        private WarmupLevelController _warmupLevelController;

        [SerializeField] private MiqatLevelController _miqatLevelController;
        [SerializeField] private TawafLevelController _tawafLevelController;

        [Header("Level Configuration")] [SerializeField]
        private LevelData[] _levels;

        private ProgressService _progressService;
        private List<LevelTileUI> _levelSelectButtons = new  List<LevelTileUI>();

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

        // Backward-compatible wrappers
        public void WarmUpLevelShow() => ShowLevelByStateId(GameStateIds.Warmup);
        public void MiqatLevelShow() => ShowLevelByStateId(GameStateIds.Miqat);
        public void TawafLevelShow() => ShowLevelByStateId(GameStateIds.Tawaf);

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
            string stateId = DetermineStateId(level.LevelId);

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

        private string DetermineStateId(string levelId)
        {
            string id = levelId.ToLower();

            if (id.Contains("warmup") || id.Contains("warm") || id.Contains("1"))
                return GameStateIds.Warmup;
            else if (id.Contains("miqat") || id.Contains("2"))
                return GameStateIds.Miqat;
            else if (id.Contains("tawaf") || id.Contains("3"))
                return GameStateIds.Tawaf;

            Debug.LogWarning($"[UIService] Unknown level id '{levelId}', defaulting to warmup");
            return GameStateIds.Warmup;
        }

        private void OnBackClicked()
        {
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            if (sm != null)
                sm.ChangeState(GameStateIds.MainMenu);
            else
                LevelManager.GoToMainMenu();
        }

        public void ShowUpQuizUI(QuizUIController quizUIController)
        {
            if (quizUIController == null)
            {
                Debug.LogError("[UIService] Cannot show quiz UI: QuizUIController reference is null!");
                return;
            }

            quizUIController.gameObject.SetActive(true);
        }

        /// <summary>
        /// Сбрасывает состояние всех уровней.
        /// </summary>
        public void ResetUI()
        {
            Debug.Log("[UIService] Resetting all level UIs");

            if (_warmupLevelController != null)
            {
                _warmupLevelController.ResetLevel();
            }

            if (_miqatLevelController != null)
            {
                _miqatLevelController.ResetLevel();
            }

            if (_tawafLevelController != null)
            {
                _tawafLevelController.ResetLevel();
            }
        }

        /// <summary>
        /// Показывает блок теории для Warmup уровня.
        /// </summary>
        public void ShowWarmUpTheoryUI()
        {
            if (_warmupLevelController != null)
            {
                _warmupLevelController.ShowTheory();
            }
            else
            {
                Debug.LogWarning("[UIService] WarmupLevelController is null!");
            }
        }

        /// <summary>
        /// Показывает блок теории для Miqat уровня.
        /// </summary>
        public void ShowMiqatTheoryUI()
        {
            if (_miqatLevelController != null)
            {
                _miqatLevelController.ShowTheory();
            }
            else
            {
                Debug.LogWarning("[UIService] MiqatLevelController is null!");
            }
        }

        /// <summary>
        /// Показывает блок теории для Tawaf уровня.
        /// </summary>
        public void ShowTawafTheoryUI()
        {
            if (_tawafLevelController != null)
            {
                _tawafLevelController.ShowTheory();
            }
            else
            {
                Debug.LogWarning("[UIService] TawafLevelController is null!");
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
        
        public void  UpdateLevelTileButtons(bool forceRefresh = false)
        {
            foreach (var tile in _levelSelectButtons)
            {
                tile.UpdateUiData(forceRefresh);
            }
        }
    }
}