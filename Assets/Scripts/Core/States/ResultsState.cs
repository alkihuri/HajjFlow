using UnityEngine;
using HajjFlow.UI;
using HajjFlow.Services;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// State representing the results screen after a level attempt.
    /// Displays score, gems earned, and navigation options.
    /// </summary>
    public class ResultsState : BaseGameState
    {
        public override string StateId => GameStateIds.Results;

        public override void Enter()
        {
            base.Enter();

            var ui = GameManager.Instance?.uiService;
            if (ui != null)
            {
                ui.ShowResults();
            }
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}
