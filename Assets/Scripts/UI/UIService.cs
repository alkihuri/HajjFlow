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
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _gameStartButton;

        [SerializeField] private GameObject _gameStartScree;
        [SerializeField] private GameObject _mainMenuScreen;
        [SerializeField] private GameObject _levelSelect;
        
        [SerializeField] private GameObject _levelsUiRoot;
        [SerializeField] private GameObject[] _levelsUI;

        [Header("Level Configuration")] [SerializeField]
        private LevelData[] _levels;

        private ProgressService _progressService;

        private void Start()
        {
            _progressService = GameManager.Instance?.ProgressService;

            _backButton?.onClick.AddListener(OnBackClicked);

            _gameStartButton?.onClick.AddListener(GameStartUI);

            _nextLevelButton?.onClick.AddListener(NextLevel);

            BuildLevelGrid();
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
            
            if(!_levelsUiRoot.activeInHierarchy)
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

        public void WarmUpLevelShow()
        {
            _mainMenuScreen?.SetActive(true);
            _gameStartScree?.SetActive(false);
            _levelSelect?.SetActive(false);

            ShowLevel(1);
        }

        public void MiqatLevelShow()
        {
            _mainMenuScreen?.SetActive(true);
            _gameStartScree?.SetActive(false);
            _levelSelect?.SetActive(false);

            ShowLevel(2);
        }

        public void TawafLevelShow()
        {
            _mainMenuScreen?.SetActive(true);
            _gameStartScree?.SetActive(false);
            _levelSelect?.SetActive(false);

            ShowLevel(3);
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
                }
            }
        }

        private void OnLevelTileClicked(LevelData level)
        {
            string stateId = DetermineStateId(level.LevelId);

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
    }
}