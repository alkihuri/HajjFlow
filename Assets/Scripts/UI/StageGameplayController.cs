using UnityEngine;
using HajjFlow.Core;
using HajjFlow.Core.States;

namespace HajjFlow.UI
{
    /// <summary>
    /// Component for managing a theory block / mini-game stage.
    /// Attach to the theory block GameObject.
    /// When the block is completed, notifies the current level state via the state machine.
    /// Works with any level state (not tied to a specific controller).
    /// </summary>
    public class StageGameplayController : MonoBehaviour
    {
        /// <summary>
        /// Called when the theory block is completed (e.g. "Next" button or condition).
        /// Notifies the current BaseLevelState through the state machine.
        /// </summary>
        public void CompleteStage()
        {
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            if (sm == null)
            {
                Debug.LogError("[StageGameplayController] GameStateMachine not available!");
                return;
            }

            var levelState = sm.CurrentState as BaseLevelState;
            if (levelState == null)
            {
                Debug.LogError("[StageGameplayController] Current state is not a level state!");
                return;
            }

            Debug.Log("[StageGameplayController] Stage gameplay completed, notifying level state...");
            levelState.CompleteTheoryStage();
        }

        /// <summary>
        /// Button handler for completing the block.
        /// Connect to the "Next" button in the UI.
        /// </summary>
        public void OnNextButtonClicked()
        {
            Debug.Log("[StageGameplayController] 'Next' button clicked");
            CompleteStage();
        }

        /// <summary>
        /// Auto-completes after a delay (for testing).
        /// </summary>
        public void CompleteAfterDelay(float delay)
        {
            Invoke(nameof(CompleteStage), delay);
        }
    }
}

