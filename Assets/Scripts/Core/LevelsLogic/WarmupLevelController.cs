using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Core.States;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Контроллер уровня "Warmup" (Подготовка к Хаджу).
    /// Thin wrapper that delegates to the state machine.
    /// All quiz/stage logic lives in WarmupLevelState.
    /// </summary>
    public class WarmupLevelController : MonoBehaviour
    {
        [SerializeField] private LevelData levelData;

        /// <summary>
        /// Starts the level via the state machine.
        /// </summary>
        public void StartLevel()
        {
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            if (sm == null || levelData == null)
            {
                Debug.LogError("[WarmupLevelController] StateMachine or LevelData missing!");
                return;
            }

            sm.StartLevel(levelData, GameStateIds.Warmup);
        }

        /// <summary>
        /// Called from scene when a theory block is completed.
        /// Delegates to the current level state.
        /// </summary>
        public void OnStageGameplayCompleted()
        {
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            var levelState = sm?.CurrentState as BaseLevelState;
            if (levelState != null)
            {
                levelState.CompleteTheoryStage();
            }
            else
            {
                Debug.LogWarning("[WarmupLevelController] No active level state to notify.");
            }
        }
    }
}