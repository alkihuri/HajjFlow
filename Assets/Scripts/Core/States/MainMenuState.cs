using UnityEngine;
using HajjFlow.UI;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// State representing the main menu screen.
    /// Shows the main menu UI on Enter and hides it on Exit.
    /// </summary>
    public class MainMenuState : BaseGameState
    {
        public override string StateId => GameStateIds.MainMenu;

        public override void Enter()
        {
            base.Enter();

            var ui = GameManager.Instance?.uiService;
            if (ui != null)
            {
                ui.ShowMainMenu();
            }
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}
