using System;
using System.Collections.Generic;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Машина состояний для управления уровнями.
    /// Управляет переходами между состояниями уровней и вызывает методы жизненного цикла.
    /// </summary>
    public class LevelStateMachine : MonoBehaviour
    {
        // ── События ──────────────────────────────────────────────────────────────

        /// <summary>Вызывается при смене состояния.</summary>
        public event Action<BaseLevelState> OnStateChanged;

        /// <summary>Вызывается при завершении уровня.</summary>
        public event Action<string, float> OnLevelCompleted; // (stateId, scorePercent)

        // ── Состояние ────────────────────────────────────────────────────────────

        private Dictionary<string, BaseLevelState> _states;
        private BaseLevelState _currentState;
        private bool _isPaused;

        // ── Свойства ─────────────────────────────────────────────────────────────

        public BaseLevelState CurrentState => _currentState;
        public bool IsPaused => _isPaused;

        // ── Инициализация ────────────────────────────────────────────────────────

        private void Awake()
        {
            _states = new Dictionary<string, BaseLevelState>();
            RegisterStates();
        }

        /// <summary>
        /// Регистрирует все доступные состояния уровней.
        /// </summary>
        private void RegisterStates()
        {
            RegisterState(new WarmupLevelState());
            RegisterState(new MiqatLevelState());
            RegisterState(new TawafLevelState());
        }

        /// <summary>
        /// Регистрирует состояние в машине.
        /// </summary>
        private void RegisterState(BaseLevelState state)
        {
            if (!_states.ContainsKey(state.StateId))
            {
                state.Initialize(this, null); // LevelData будет установлен при переходе
                _states.Add(state.StateId, state);
                Debug.Log($"[LevelStateMachine] Registered state: {state.StateId}");
            }
        }

        // ── Управление состояниями ───────────────────────────────────────────────

        /// <summary>
        /// Переходит в новое состояние по идентификатору.
        /// </summary>
        public void ChangeState(string stateId, LevelData levelData)
        {
            if (!_states.ContainsKey(stateId))
            {
                Debug.LogError($"[LevelStateMachine] State '{stateId}' not registered!");
                return;
            }

            // Выход из текущего состояния
            if (_currentState != null)
            {
                _currentState.Exit();
            }

            // Переход в новое состояние
            _currentState = _states[stateId];
            _currentState.Initialize(this, levelData);
            _currentState.Enter();

            Debug.Log($"[LevelStateMachine] State changed to: {stateId}");
            OnStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Запускает уровень с указанным состоянием.
        /// </summary>
        public void StartLevel(string stateId, LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[LevelStateMachine] Cannot start level with null LevelData!");
                return;
            }

            Debug.Log($"[LevelStateMachine] Starting level '{levelData.LevelName}' with state '{stateId}'");
            ChangeState(stateId, levelData);
        }

        // ── Update ───────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_isPaused || _currentState == null) return;
            _currentState.Update();
        }

        // ── Пауза ────────────────────────────────────────────────────────────────

        /// <summary>Ставит игру на паузу.</summary>
        public void Pause()
        {
            if (_isPaused) return;
            _isPaused = true;
            _currentState?.OnPause();
            Time.timeScale = 0f;
            Debug.Log("[LevelStateMachine] Paused");
        }

        /// <summary>Возобновляет игру.</summary>
        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _currentState?.OnResume();
            Time.timeScale = 1f;
            Debug.Log("[LevelStateMachine] Resumed");
        }

        // ── Завершение уровня ────────────────────────────────────────────────────

        /// <summary>
        /// Вызывается состоянием когда уровень завершен.
        /// </summary>
        public void CompleteLevel(float scorePercent)
        {
            if (_currentState == null) return;

            Debug.Log($"[LevelStateMachine] Level completed: {_currentState.StateId}, Score: {scorePercent:F1}%");
            OnLevelCompleted?.Invoke(_currentState.StateId, scorePercent);
        }

        // ── Очистка ──────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            _currentState?.Exit();
            _currentState = null;
        }
    }
}

