using UnityEngine;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Gameplay state for the Miqat level.
    /// Adds speed bonuses and excellence rewards on top of base quiz flow.
    /// </summary>
    public class MiqatLevelState : BaseLevelState
    {
        public override string StateId => GameStateIds.Miqat;
        protected override int TheoryBlockCount => 2;

        private float _startTime;

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
            uiService.ShowMiqatTheoryUI();
            
            // Запоминаем время старта для бонусов
            _startTime = Time.time;
            
            // Квиз запустится автоматически после завершения теории 
            // через MiqatLevelController.OnTheoryCompleted
        }

        public override void Exit()
        {
            base.Exit();
            
            // Сбрасываем состояние при выходе
            GameManager.Instance?.uiService?.ResetUI();
        }

        public override void Update()
        {
           
        }

        public override void OnPause()
        {
            base.OnPause();
            Debug.Log("[MiqatLevelState] Level paused");
        }

        public override void OnResume()
        {
            base.OnResume();
            Debug.Log("[MiqatLevelState] Level resumed");
            _startTime += Time.unscaledDeltaTime;
        }

        protected override void HandleAnswerResult(bool wasCorrect, string explanation)
        {
            base.HandleAnswerResult(wasCorrect, explanation);

            if (wasCorrect)
            {
                // Speed bonus (answer within 3 minutes)
                float elapsedTime = Time.time - _startTime;
                if (elapsedTime < 180f)
                {
                    _rewardSystem?.AwardGems(2);
                    Debug.Log("[MiqatLevelState] Speed bonus: +2 gems");
                }
            }
        }

        protected override void HandleQuizComplete(float scorePercent)
        { 
            if (scorePercent >= 90f && _levelData != null)
            {
                _rewardSystem?.AwardGems(_levelData.CompletionBonusGems / 2);
                Debug.Log("[MiqatLevelState] Excellence bonus awarded!");
            }

            float elapsedTime = Time.time - _startTime;
            Debug.Log($"[MiqatLevelState] Quiz completed in {elapsedTime:F0}s");

            base.HandleQuizComplete(scorePercent);
        }
    }
}

