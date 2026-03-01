using UnityEngine;
using HajjFlow.Core.States;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Контроллер уровня "Miqat" (Микат).
    /// </summary>
    public class MiqatLevelController : LevelControllerBase
    {
        protected override string StateId => GameStateIds.Miqat;

        protected override void Awake()
        {
            Debug.Log("[MiqatLevelController] Awake");
            base.Awake();
        }

        protected override void OnDestroy()
        {
            Debug.Log("[MiqatLevelController] OnDestroy");
            base.OnDestroy();
        }

        public override void StartLevel()
        {
            Debug.Log("[MiqatLevelController] StartLevel");
            base.StartLevel();
        }

        public override void ResetLevel()
        {
            Debug.Log("[MiqatLevelController] ResetLevel");
            base.ResetLevel();
        }

        public override void ShowTheory()
        {
            Debug.Log("[MiqatLevelController] ShowTheory");
            base.ShowTheory();
        }

        protected override void OnTheoryCompleted()
        {
            Debug.Log("[MiqatLevelController] OnTheoryCompleted - Theory completed, starting quiz");
            base.OnTheoryCompleted();
        }

        protected override void StartQuiz()
        {
            Debug.Log($"[MiqatLevelController] StartQuiz - Questions: {levelData?.Questions?.Length ?? 0}");
            base.StartQuiz();
        }

        public override void OnStageGameplayCompleted()
        {
            Debug.Log("[MiqatLevelController] OnStageGameplayCompleted");
            base.OnStageGameplayCompleted();
        }
    }
}

