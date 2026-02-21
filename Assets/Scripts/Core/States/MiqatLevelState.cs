using UnityEngine;
using HajjFlow.Gameplay;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Состояние уровня Miqat (Место принятия ихрама).
    /// Второй уровень, обучающий игрока правилам Miqat.
    /// </summary>
    public class MiqatLevelState : BaseLevelState
    {
        public override string StateId => "miqat";

        private QuizSystem _quizSystem;
        private RewardSystem _rewardSystem;
        private int _questionsAnswered;
        private int _correctAnswers;
        private int _totalQuestions;
        private float _startTime;

        public override void Enter()
        {
            base.Enter();

            // Найти системы в сцене
            _quizSystem = Object.FindObjectOfType<QuizSystem>();
            _rewardSystem = Object.FindObjectOfType<RewardSystem>();

            if (_quizSystem == null)
            {
                Debug.LogError("[MiqatLevelState] QuizSystem not found in scene!");
                return;
            }

            // Подписаться на события
            _quizSystem.OnQuestionReady += OnQuestionReady;
            _quizSystem.OnAnswerResult += OnAnswerResult;
            _quizSystem.OnQuizComplete += OnQuizComplete;

            // Инициализация
            _quizSystem.Initialise(_levelData);
            _questionsAnswered = 0;
            _correctAnswers = 0;
            _totalQuestions = _levelData?.Questions?.Length ?? 0;
            _startTime = Time.time;

            Debug.Log($"[MiqatLevelState] Starting Miqat level with {_totalQuestions} questions");
        }

        public override void Update()
        {
            base.Update();
            
            // Логика специфичная для Miqat
            // Например, таймер или дополнительные проверки
            float elapsedTime = Time.time - _startTime;
            
            // Можно добавить бонус за быстрое прохождение
            if (elapsedTime > 300f) // 5 минут
            {
                // Предупреждение о времени
            }
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

            Debug.Log("[MiqatLevelState] Miqat level completed");
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
            // Скорректировать время если нужно
            _startTime += Time.unscaledDeltaTime;
        }

        // ── Обработчики событий ──────────────────────────────────────────────────

        private void OnQuestionReady(QuizQuestion question, int questionNumber)
        {
            Debug.Log($"[MiqatLevelState] Question {questionNumber}/{_totalQuestions}: {question.QuestionText}");
            
            // Специфичная логика для Miqat
            // Например, показать дополнительную информацию о Miqat
        }

        private void OnAnswerResult(bool wasCorrect, string explanation)
        {
            _questionsAnswered++;
            
            if (wasCorrect)
            {
                _correctAnswers++;
                Debug.Log($"[MiqatLevelState] Correct! ({_correctAnswers}/{_questionsAnswered})");
                
                // Дополнительная награда за правильный ответ в Miqat
                float elapsedTime = Time.time - _startTime;
                if (elapsedTime < 180f) // Бонус за быстрый ответ (3 минуты)
                {
                    _rewardSystem?.AwardGems(2); // Дополнительные гемы
                    Debug.Log("[MiqatLevelState] Speed bonus: +2 gems");
                }
            }
            else
            {
                Debug.Log($"[MiqatLevelState] Wrong answer. {explanation}");
            }
        }

        private void OnQuizComplete(float scorePercent)
        {
            Debug.Log($"[MiqatLevelState] Quiz completed with score: {scorePercent:F1}%");
            
            float elapsedTime = Time.time - _startTime;
            Debug.Log($"[MiqatLevelState] Completed in {elapsedTime:F0} seconds");

            // Сохранить прогресс
            if (GameManager.Instance?.ProgressService != null && _levelData != null)
            {
                //GameManager.Instance.ProgressService.UpdateLevelProgress(_levelData.LevelId, scorePercent);
                
                // Разблокировать Tawaf если прошли успешно
                if (scorePercent >= _levelData.PassThreshold)
                {
                   // GameManager.Instance.ProgressService.UnlockLevel("tawaf");
                    
                    // Бонус за отличный результат
                    if (scorePercent >= 90f)
                    {
                        _rewardSystem?.AwardGems(_levelData.CompletionBonusGems / 2);
                        Debug.Log("[MiqatLevelState] Excellence bonus awarded!");
                    }
                }
            }

            // Уведомить машину состояний о завершении
            _stateMachine?.CompleteLevel(scorePercent);
        }
    }
}

