using UnityEngine;
using HajjFlow.Core.States;

namespace HajjFlow.Core
{
    /// <summary>
    /// Инициализирует машину состояний уровней в Gameplay сцене.
    /// Добавьте этот компонент на GameObject в сцене Gameplay.
    /// </summary>
    [RequireComponent(typeof(LevelStateMachine))]
    public class GameplaySceneInitializer : MonoBehaviour
    {
        private LevelStateMachine _stateMachine;

        private void Awake()
        {
            // Получить машину состояний
            _stateMachine = GetComponent<LevelStateMachine>();
            
            if (_stateMachine == null)
            {
                Debug.LogError("[GameplaySceneInitializer] LevelStateMachine component not found!");
                return;
            }

            // Зарегистрировать машину состояний в LevelManager
            LevelManager.RegisterStateMachine(_stateMachine);
            
            // Подписаться на события
            _stateMachine.OnStateChanged += OnStateChanged;
            _stateMachine.OnLevelCompleted += OnLevelCompleted;
            
            Debug.Log("[GameplaySceneInitializer] Scene initialized with state machine");
        }

        private void OnDestroy()
        {
            // Отписаться от событий
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= OnStateChanged;
                _stateMachine.OnLevelCompleted -= OnLevelCompleted;
            }
            
            // Отрегистрировать машину состояний
            LevelManager.UnregisterStateMachine();
        }

        // ── Обработчики событий ──────────────────────────────────────────────────

        private void OnStateChanged(BaseLevelState newState)
        {
            Debug.Log($"[GameplaySceneInitializer] State changed to: {newState.StateId}");
        }

        private void OnLevelCompleted(string stateId, float scorePercent)
        {
            Debug.Log($"[GameplaySceneInitializer] Level '{stateId}' completed with {scorePercent:F1}%");
            
            // Автоматический переход на экран результатов
            Invoke(nameof(ShowResults), 2f); // Задержка для показа финальной анимации
        }

        private void ShowResults()
        {
            LevelManager.ShowResults();
        }
    }
}

