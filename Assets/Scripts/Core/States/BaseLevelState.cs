using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Base class for level-gameplay states (Warmup, Miqat, Tawaf).
    /// Extends <see cref="BaseGameState"/> with <see cref="LevelData"/> support.
    /// </summary>
    public abstract class BaseLevelState : BaseGameState
    {
        protected LevelData _levelData;

        /// <summary>
        /// Initializes the state with both the state machine and level-specific data.
        /// Called by <see cref="GameStateMachine.ChangeState(string, LevelData)"/>.
        /// </summary>
        public virtual void InitializeWithLevel(GameStateMachine stateMachine, LevelData levelData)
        {
            base.Initialize(stateMachine);
            _levelData = levelData;
        }

        public override void Enter()
        {
            Debug.Log($"[{StateId}] Entering state: {_levelData?.LevelName ?? "Unknown"}");
        }

        public override void Exit()
        {
            Debug.Log($"[{StateId}] Exiting state");
        }
    }
}

