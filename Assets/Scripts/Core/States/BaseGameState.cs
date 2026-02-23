using UnityEngine;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Abstract base class for all game states (menu, gameplay, results, etc.).
    /// Each state has a lifecycle: Initialize → Enter → Update → Exit.
    /// All UI switching and logic should happen inside Enter / Exit.
    /// </summary>
    public abstract class BaseGameState
    {
        protected GameStateMachine _stateMachine;

        /// <summary>Unique identifier for this state.</summary>
        public abstract string StateId { get; }

        /// <summary>Initializes the state with a reference to the owning machine.</summary>
        public virtual void Initialize(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        /// <summary>
        /// Called when the state becomes active.
        /// Set up UI, subscribe to events, load resources here.
        /// </summary>
        public virtual void Enter()
        {
            Debug.Log($"[{StateId}] Enter");
        }

        /// <summary>Called every frame while the state is active.</summary>
        public virtual void Update() { }

        /// <summary>
        /// Called when leaving this state.
        /// Clean up UI, unsubscribe from events, save progress here.
        /// </summary>
        public virtual void Exit()
        {
            Debug.Log($"[{StateId}] Exit");
        }

        /// <summary>Called when the game is paused.</summary>
        public virtual void OnPause() { }

        /// <summary>Called when the game is resumed.</summary>
        public virtual void OnResume() { }
    }
}
