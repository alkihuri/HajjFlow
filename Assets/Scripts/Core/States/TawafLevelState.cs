using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Gameplay state for the Tawaf level.
    /// Adds streak bonuses, perfect-circle rewards, and perfect-Tawaf bonus.
    /// </summary>
    public class TawafLevelState : BaseLevelState
    {
        public override string StateId => GameStateIds.Tawaf;
        protected override int TheoryBlockCount => 2;

        private int _consecutiveCorrect;
        private float _startTime;

        protected override void ShowLevelUI()
        {
            GameManager.Instance?.uiService?.ShowLevelByStateId(StateId);
        }

        public override void Enter()
        {
            base.Enter();
            _consecutiveCorrect = 0;
            _startTime = Time.time;
        }

        public override void Update()
        {
            float elapsedTime = Time.time - _startTime;

            if (Mathf.FloorToInt(elapsedTime) % 60 == 0 && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[TawafLevelState] Time elapsed: {elapsedTime:F0}s");
            }
        }

        public override void OnPause()
        {
            base.OnPause();
            Debug.Log("[TawafLevelState] Tawaf paused");
        }

        public override void OnResume()
        {
            base.OnResume();
            Debug.Log("[TawafLevelState] Tawaf resumed");
            _startTime += Time.unscaledDeltaTime;
        }

        protected override void HandleQuestionReady(QuizQuestion question, int questionNumber)
        {
            base.HandleQuestionReady(question, questionNumber);

            // Show Tawaf circle number (every 7 questions = 1 circle)
            int currentCircle = (questionNumber - 1) / 7 + 1;
            Debug.Log($"[TawafLevelState] Circle {currentCircle} of Tawaf");
        }

        protected override void HandleAnswerResult(bool wasCorrect, string explanation)
        {
            base.HandleAnswerResult(wasCorrect, explanation);

            if (wasCorrect)
            {
                _consecutiveCorrect++;

                Debug.Log($"[TawafLevelState] Streak: {_consecutiveCorrect}");

                // Streak bonus (3+ consecutive correct)
                if (_consecutiveCorrect >= 3)
                {
                    int bonusGems = _consecutiveCorrect * 2;
                    _rewardSystem?.AwardGems(bonusGems);
                    Debug.Log($"[TawafLevelState] Streak bonus: +{bonusGems} gems");
                }

                // Perfect circle bonus (7 consecutive correct)
                if (_consecutiveCorrect == 7)
                {
                    _rewardSystem?.AwardGems(20);
                    Debug.Log("[TawafLevelState] Perfect circle completed! +20 bonus gems");
                }
            }
            else
            {
                _consecutiveCorrect = 0;
            }
        }

        protected override void SaveProgress()
        {
            // Perfect score bonus
            if (_lastScorePercent == 100f)
            {
                _rewardSystem?.AwardGems(50);
                Debug.Log("[TawafLevelState] Perfect Tawaf! +50 gems");
            }

            base.SaveProgress();
        }
    }
}

