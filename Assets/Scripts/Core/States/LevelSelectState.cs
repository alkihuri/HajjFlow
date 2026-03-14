using UnityEngine;
using HajjFlow.UI;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// State representing the level selection screen.
    /// Shows the level-selection UI on Enter.
    /// </summary>
    public class LevelSelectState : BaseGameState
    {
        public override string StateId => GameStateIds.LevelSelect;

        public override void Enter()
        {
            base.Enter();

            var ui = GameManager.Instance?.uiService;
            var profileService = GameManager.Instance?.ProfileService;
            if (ui != null)
            {
                ui.ResetUI();
                ui.ShowLevelSelect();
                ui.UpdateGemsCounter(profileService.GetProfile().Gems);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}
