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

        private BaseGameState _currentState;
        private bool _isPaused;

        /// <summary>The level data for the currently active (or last-selected) level.</summary>
        public LevelData ActiveLevelData { get; private set; }

        /// <summary>The state id for the currently active level.</summary>
        public string ActiveLevelStateId { get; private set; }

        /// <summary>Currently active state.</summary>
        public BaseGameState CurrentState => _currentState;

        /// <summary>Whether the game is paused.</summary>
        public bool IsPaused => _isPaused;

        // ── Initialization ───────────────────────────────────────────────────────

        private void Awake()
        {
            RegisterDefaultStates();
        }

        /// <summary>Registers the built-in set of states.</summary>
        private void RegisterDefaultStates()
        {
            // Game-flow states
            RegisterState(new MainMenuState());
            RegisterState(new LevelSelectState());
            RegisterState(new ResultsState());

            // Level-gameplay states
            RegisterState(new WarmupLevelState());
            RegisterState(new MiqatLevelState());
            RegisterState(new TawafLevelState());
        }

        /// <summary>Registers a state in the machine.</summary>
        public void RegisterState(BaseGameState state)
        {
            if (state == null) return;
            if (_states.ContainsKey(state.StateId))
            {
                Debug.LogWarning($"[GameStateMachine] State '{state.StateId}' already registered.");
                return;
            }

            state.Initialize(this);
            _states.Add(state.StateId, state);
            Debug.Log($"[GameStateMachine] Registered state: {state.StateId}");
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
            if (!_states.ContainsKey(stateId))
            {
                Debug.LogError($"[GameStateMachine] State '{stateId}' not registered!");
                return;
            }

            // Exit current state
            _currentState?.Exit();

            // Enter new state
            _currentState = _states[stateId];

            if (_currentState is BaseLevelState levelState && levelData != null)
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
