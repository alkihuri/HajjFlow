using System;
using System.Collections.Generic;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Legacy state machine kept for backward compatibility.
    /// New code should use <see cref="GameStateMachine"/> instead.
    /// </summary>
    [System.Obsolete("Use GameStateMachine instead. This class is kept for scene compatibility.")]
    public class LevelStateMachine : MonoBehaviour
    {
        public event Action<BaseLevelState> OnStateChanged;
        public event Action<string, float> OnLevelCompleted;

        private Dictionary<string, BaseLevelState> _states;
        private BaseLevelState _currentState;
        private bool _isPaused;

        public BaseLevelState CurrentState => _currentState;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            _states = new Dictionary<string, BaseLevelState>();
        }

        public void ChangeState(string stateId, LevelData levelData)
        {
            // Delegate to GameStateMachine if available
            var gsm = GameManager.Instance?.GetService<GameStateMachine>();
            if (gsm != null)
            {
                gsm.ChangeState(stateId, levelData);
                return;
            }

            Debug.LogWarning("[LevelStateMachine] GameStateMachine not available, running in legacy mode.");
        }

        public void StartLevel(string stateId, LevelData levelData)
        {
            ChangeState(stateId, levelData);
        }

        public void CompleteLevel(float scorePercent)
        {
            var gsm = GameManager.Instance?.GetService<GameStateMachine>();
            if (gsm != null)
            {
                gsm.CompleteLevel(scorePercent);
                return;
            }

            OnLevelCompleted?.Invoke(_currentState?.StateId ?? "", scorePercent);
        }

        public void Pause()
        {
            var gsm = GameManager.Instance?.GetService<GameStateMachine>();
            gsm?.Pause();
        }

        public void Resume()
        {
            var gsm = GameManager.Instance?.GetService<GameStateMachine>();
            gsm?.Resume();
        }
    }
}

