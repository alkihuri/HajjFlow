using UnityEngine;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Gameplay state for the Warmup (introductory) level.
    /// No special bonuses — uses base quiz flow only.
    /// </summary>
    public class WarmupLevelState : BaseLevelState
    {
        public override string StateId => GameStateIds.Warmup;
        protected override int TheoryBlockCount => 3;

        protected override void ShowLevelUI()
        {
            GameManager.Instance?.uiService?.ShowLevelByStateId(StateId);
        }
    }
}

