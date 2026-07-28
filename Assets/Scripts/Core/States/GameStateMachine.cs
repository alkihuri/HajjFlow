using System;
using System.Collections.Generic;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// The single, authoritative state machine for the entire game.
    /// Manages all states: menu screens, level gameplay, and results.
    /// All UI transitions and game-flow decisions are driven by state Enter / Exit.
    /// </summary>
    public class GameStateMachine : MonoBehaviour
    {
        
        // ── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired whenever the active state changes.</summary>
        public event Action<BaseGameState> OnStateChanged;

        /// <summary>Fired when a gameplay level is completed (stateId, scorePercent).</summary>
        public event Action<string, float> OnLevelCompleted;

        // ── Runtime state ────────────────────────────────────────────────────────

        private readonly Dictionary<string, BaseGameState> _states =
            new Dictionary<string, BaseGameState>();

        [SerializeField] private BaseGameState _currentState;
        private bool _isPaused;

        /// <summary>The level data for the currently active (or last-selected) level.</summary>
        public LevelData ActiveLevelData { get; private set; }

        /// <summary>The state id for the currently active level.</summary>
        public string ActiveLevelStateId { get; private set; }

        /// <summary>Currently active state.</summary>
        public BaseGameState CurrentState => _currentState;

        /// <summary>Whether the game is paused.</summary>
        public bool IsPaused => _isPaused;

        // ── Dynamic state creation ───────────────────────────────────────────────

        /// <summary>
        /// Creates a state instance by its id on demand.
        /// States are created dynamically on first transition and cached for reuse.
        /// </summary>
        private BaseGameState CreateState(string stateId)
        {
            switch (stateId)
            {
                case GameStateIds.MainMenu:
                    return new MainMenuState();
                case GameStateIds.LevelSelect:
                    return new LevelSelectState();
                case GameStateIds.Results:
                    return new ResultsState();
                default:
                    return new LevelState(stateId);
            }
        }

        /// <summary>
        /// Returns a cached state or creates and caches a new one dynamically.
        /// </summary>
        private BaseGameState GetOrCreateState(string stateId)
        {
            if (!_states.TryGetValue(stateId, out var state))
            {
                state = CreateState(stateId);
                state.Initialize(this);
                _states.Add(stateId, state);
                Debug.Log($"[GameStateMachine] Dynamically created state: {stateId}");
            }
            return state;
        }

        // ── State transitions ────────────────────────────────────────────────────

        /// <summary>
        /// Transitions to a new state by id.
        /// Use for non-level states (main_menu, level_select, results).
        /// </summary>
        public void ChangeState(string stateId)
        {
            ChangeStateInternal(stateId, null);
        }

        /// <summary>
        /// Transitions to a level-gameplay state, supplying the level data.
        /// </summary>
        public void ChangeState(string stateId, LevelData levelData)
        {
            ChangeStateInternal(stateId, levelData);
        }

        private void ChangeStateInternal(string stateId, LevelData levelData)
        {
            var newState = GetOrCreateState(stateId);

            // Exit current state
            _currentState?.Exit();

            // Enter new state
            _currentState = newState;

            if (_currentState is LevelState levelState && levelData != null)
            {
                ActiveLevelData = levelData;
                ActiveLevelStateId = stateId;
                levelState.InitializeWithLevel(this, levelData);
            }
            else
            {
                _currentState.Initialize(this);
            }

            _currentState.Enter();

            Debug.Log($"[GameStateMachine] → {stateId}");
            OnStateChanged?.Invoke(_currentState);
        }

        // ── Level helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Convenience method: stores the chosen level, then transitions to its state.
        /// Called from level-selection UI.
        /// </summary>
        public void StartLevel(LevelData levelData, string stateId)
        {
            if (levelData == null)
            {
                Debug.LogError("[GameStateMachine] Cannot start level with null LevelData!");
                return;
            }

            Debug.Log($"[GameStateMachine] Starting level '{levelData.LevelName}' ({stateId})");
            ChangeState(stateId, levelData);
        }

        /// <summary>
        /// Called by a level state when the level is finished.
        /// Fires the <see cref="OnLevelCompleted"/> event and transitions to Results.
        /// </summary>
        public void CompleteLevel(float scorePercent)
        {
            if (_currentState == null) return;

            string stateId = _currentState.StateId;
            Debug.Log($"[GameStateMachine] Level completed: {stateId}, Score: {scorePercent:F1}%");
            OnLevelCompleted?.Invoke(stateId, scorePercent);

            // Transition to results after a short delay for feedback
            Invoke(nameof(GoToResults), 2f);
        }

        /// <summary>
        /// Replays the current level: re-enters the same level state with the same data.
        /// Resets quiz, score, and stage progress without reloading the scene.
        /// </summary>
        public void ReplayCurrentLevel()
        {
            if (ActiveLevelData == null || string.IsNullOrEmpty(ActiveLevelStateId))
            {
                Debug.LogWarning("[GameStateMachine] Cannot replay — no active level.");
                return;
            }

            Debug.Log($"[GameStateMachine] Replaying level '{ActiveLevelStateId}'");
            ChangeState(ActiveLevelStateId, ActiveLevelData);
        }

        private void GoToResults()
        {
            ChangeState(GameStateIds.Results);
        }

        // ── Update ───────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_isPaused || _currentState == null) return;
            _currentState.Update();
        }

        // ── Pause / Resume ───────────────────────────────────────────────────────

        /// <summary>Pauses the game.</summary>
        public void Pause()
        {
            if (_isPaused) return;
            _isPaused = true;
            _currentState?.OnPause();
            Time.timeScale = 0f;
            Debug.Log("[GameStateMachine] Paused");
        }

        /// <summary>Resumes the game.</summary>
        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _currentState?.OnResume();
            Time.timeScale = 1f;
            Debug.Log("[GameStateMachine] Resumed");
        }

        // ── Cleanup ──────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            _currentState?.Exit();
            _currentState = null;
        }
    }
}