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

        public override void Enter()
        {
            base.Enter();
            
            var uiService = GameManager.Instance?.uiService;
            if (uiService == null)
            {
                Debug.LogError($"[{StateId}] UIService not found!");
                return;
            }
            
            // Показываем UI уровня и начинаем с теории
            uiService.ShowLevelByStateId(StateId);
            uiService.ShowWarmUpTheoryUI();
            
            // Квиз запустится автоматически после завершения теории 
            // через WarmupLevelController.OnTheoryCompleted
        }

        public override void Exit()
        {
            base.Exit();
            
            // Сбрасываем состояние при выходе
            GameManager.Instance?.uiService?.ResetUI();
        }
    }
}

