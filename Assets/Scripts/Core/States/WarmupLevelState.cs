using UnityEngine;
using HajjFlow.Gameplay;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Состояние уровня Warmup (Разминка/Подготовка).
    /// Первый уровень для ознакомления с механикой игры.
    /// </summary>
    public class WarmupLevelState : BaseLevelState
    {
        public override string StateId => "warmup";

        private QuizSystem _quizSystem;
        private int _questionsAnswered;
        private int _totalQuestions;

        public override void Enter()
        {
            base.Enter();

            // Найти QuizSystem в сцене
            _quizSystem = Object.FindObjectOfType<QuizSystem>();
            if (_quizSystem == null)
            {
                Debug.LogError("[WarmupLevelState] QuizSystem not found in scene!");
                return;
            }

            // Подписаться на события
            _quizSystem.OnQuestionReady += OnQuestionReady;
            _quizSystem.OnAnswerResult += OnAnswerResult;
            _quizSystem.OnQuizComplete += OnQuizComplete;

            // Инициализировать викторину
            _quizSystem.Initialise(_levelData);
            _questionsAnswered = 0;
            _totalQuestions = _levelData?.Questions?.Length ?? 0;

            Debug.Log($"[WarmupLevelState] Starting warmup level with {_totalQuestions} questions");
        }

        public override void Update()
        {
            base.Update();
            // Дополнительная логика для warmup уровня
            // Например, подсказки для новичков, анимации и т.д.
        }

        public override void Exit()
        {
            base.Exit();

            // Отписаться от событий
            if (_quizSystem != null)
            {
                _quizSystem.OnQuestionReady -= OnQuestionReady;
                _quizSystem.OnAnswerResult -= OnAnswerResult;
                _quizSystem.OnQuizComplete -= OnQuizComplete;
            }

            Debug.Log("[WarmupLevelState] Warmup level completed");
        }

        // ── Обработчики событий ──────────────────────────────────────────────────

        private void OnQuestionReady(QuizQuestion question, int questionNumber)
        {
            Debug.Log($"[WarmupLevelState] Question {questionNumber}/{_totalQuestions}: {question.QuestionText}");
            // Здесь можно добавить специфичную логику для warmup (подсказки, анимации)
        }

        private void OnAnswerResult(bool wasCorrect, string explanation)
        {
            _questionsAnswered++;
            
            if (wasCorrect)
            {
                Debug.Log($"[WarmupLevelState] Correct answer! {explanation}");
                // Специальная награда или анимация для warmup
            }
            else
            {
                Debug.Log($"[WarmupLevelState] Wrong answer. {explanation}");
                // Можно показать дополнительную помощь для новичков
            }
        }

        private void OnQuizComplete(float scorePercent)
        {
            Debug.Log($"[WarmupLevelState] Quiz completed with score: {scorePercent:F1}%");
            
            // Сохранить прогресс
            if (GameManager.Instance?.ProgressService != null && _levelData != null)
            {
                //GameManager.Instance.ProgressService.UpdateLevelProgress(_levelData.LevelId, scorePercent);
                
                // Разблокировать следующий уровень если прошли
                if (scorePercent >= _levelData.PassThreshold)
                {
                   // GameManager.Instance.ProgressService.UnlockLevel("miqat");
                }
            }

            // Уведомить машину состояний о завершении
            _stateMachine?.CompleteLevel(scorePercent);
        }
    }
}

