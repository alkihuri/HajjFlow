using UnityEngine;
using HajjFlow.Core.States;

namespace HajjFlow.Core
{
    /// <summary>
    /// Optional scene-level initializer that subscribes to <see cref="GameStateMachine"/>
    /// events for logging and scene-specific setup.
    /// Add this to a GameObject in the Gameplay scene if you need per-scene hooks.
    /// </summary>
    public class GameplaySceneInitializer : MonoBehaviour
    {
        private GameStateMachine _stateMachine;

        private void Awake()
        {
            _stateMachine = GameManager.Instance?.GetService<GameStateMachine>();

            if (_stateMachine == null)
            {
                Debug.LogWarning("[GameplaySceneInitializer] GameStateMachine not available.");
                return;
            }

            _stateMachine.OnStateChanged += OnStateChanged;
            _stateMachine.OnLevelCompleted += OnLevelCompleted;

            Debug.Log("[GameplaySceneInitializer] Subscribed to GameStateMachine events.");
        }

        private void OnDestroy()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= OnStateChanged;
                _stateMachine.OnLevelCompleted -= OnLevelCompleted;
            }
        }

        private void OnStateChanged(BaseGameState newState)
        {
            Debug.Log($"[GameplaySceneInitializer] State changed to: {newState.StateId}");
        }

        private void OnLevelCompleted(string stateId, float scorePercent)
        {
            Debug.Log($"[GameplaySceneInitializer] Level '{stateId}' completed with {scorePercent:F1}%");
        }
    }
}

