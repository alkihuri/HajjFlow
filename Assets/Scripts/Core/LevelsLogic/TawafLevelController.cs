using UnityEngine;
using HajjFlow.Core.States;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Контроллер уровня "Tawaf" (Таваф).
    /// </summary>
    public class TawafLevelController : LevelControllerBase
    {
        protected override string StateId => GameStateIds.Tawaf;

        protected override void Awake()
        {
            Debug.Log("[TawafLevelController] Awake");
            base.Awake();
        }

        protected override void OnDestroy()
        {
            Debug.Log("[TawafLevelController] OnDestroy");
            base.OnDestroy();
        }

        public override void StartLevel()
        {
            Debug.Log("[TawafLevelController] StartLevel");
            base.StartLevel();
        }

        public override void ResetLevel()
        {
            Debug.Log("[TawafLevelController] ResetLevel");
            base.ResetLevel();
        }

        public override void ShowTheory()
        {
            Debug.Log("[TawafLevelController] ShowTheory");
            base.ShowTheory();
        }

        protected override void OnTheoryCompleted()
        {
            Debug.Log("[TawafLevelController] OnTheoryCompleted - Theory completed, starting quiz");
            base.OnTheoryCompleted();
        }

        protected override void StartQuiz()
        {
            Debug.Log($"[TawafLevelController] StartQuiz - Questions: {levelData?.Questions?.Length ?? 0}");
            base.StartQuiz();
        }

        public override void OnStageGameplayCompleted()
        {
            Debug.Log("[TawafLevelController] OnStageGameplayCompleted");
            base.OnStageGameplayCompleted();
        }
    }
}

