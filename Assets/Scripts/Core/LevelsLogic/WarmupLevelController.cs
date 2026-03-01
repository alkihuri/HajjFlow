using UnityEngine;
using HajjFlow.Core.States;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Контроллер уровня "Warmup" (Подготовка к Хаджу).
    /// </summary>
    public class WarmupLevelController : LevelControllerBase
    {
        protected override string StateId => GameStateIds.Warmup;

        protected override void Awake()
        {
            Debug.Log("[WarmupLevelController] Awake");
            base.Awake();
        }

        protected override void OnDestroy()
        {
            Debug.Log("[WarmupLevelController] OnDestroy");
            base.OnDestroy();
        }

        public override void StartLevel()
        {
            Debug.Log("[WarmupLevelController] StartLevel");
            base.StartLevel();
        }

        public override void ResetLevel()
        {
            Debug.Log("[WarmupLevelController] ResetLevel");
            base.ResetLevel();
        }

        public override void ShowTheory()
        {
            Debug.Log("[WarmupLevelController] ShowTheory");
            base.ShowTheory();
        }

        protected override void OnTheoryCompleted()
        {
            Debug.Log("[WarmupLevelController] OnTheoryCompleted - Theory completed, starting quiz");
            base.OnTheoryCompleted();
        }

        protected override void StartQuiz()
        {
            Debug.Log($"[WarmupLevelController] StartQuiz - Questions: {levelData?.Questions?.Length ?? 0}");
            base.StartQuiz();
        }

        public override void OnStageGameplayCompleted()
        {
            Debug.Log("[WarmupLevelController] OnStageGameplayCompleted");
            base.OnStageGameplayCompleted();
        }
    }
}