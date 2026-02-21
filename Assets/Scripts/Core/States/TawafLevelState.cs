using UnityEngine;
using HajjFlow.Gameplay;
using HajjFlow.Data;

namespace HajjFlow.Core.States
{
    /// <summary>
    /// Состояние уровня Tawaf (Обход Каабы).
    /// Третий уровень, самый сложный и важный ритуал Хаджа.
    /// </summary>
    public class TawafLevelState : BaseLevelState
    {
        public override string StateId => "tawaf";

        private QuizSystem _quizSystem;
        private RewardSystem _rewardSystem;
        private int _questionsAnswered;
        private int _correctAnswers;
        private int _totalQuestions;
        private int _consecutiveCorrect;
        private float _startTime;

        public override void Enter()
        {
            base.Enter();

            // Найти системы в сцене
            _quizSystem = Object.FindObjectOfType<QuizSystem>();
            _rewardSystem = Object.FindObjectOfType<RewardSystem>();

            if (_quizSystem == null)
            {
                Debug.LogError("[TawafLevelState] QuizSystem not found in scene!");
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
            _consecutiveCorrect = 0;
            _startTime = Time.time;

            Debug.Log($"[TawafLevelState] Starting Tawaf level - the sacred circumambulation with {_totalQuestions} questions");
        }

        public override void Update()
        {
            base.Update();
            
            // Специфичная логика для Tawaf
            // Можно добавить визуализацию кругов вокруг Каабы
            // или анимацию прогресса обхода
            
            float elapsedTime = Time.time - _startTime;
            
            // Логирование для отладки (можно удалить в продакшене)
            if (Mathf.FloorToInt(elapsedTime) % 60 == 0 && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[TawafLevelState] Time elapsed: {elapsedTime:F0}s");
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

            Debug.Log("[TawafLevelState] Tawaf level completed - May your pilgrimage be accepted");
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
            // Скорректировать время
            _startTime += Time.unscaledDeltaTime;
        }

        // ── Обработчики событий ──────────────────────────────────────────────────

        private void OnQuestionReady(QuizQuestion question, int questionNumber)
        {
            Debug.Log($"[TawafLevelState] Question {questionNumber}/{_totalQuestions}: {question.QuestionText}");
            
            // Показать номер круга Tawaf (каждые 7 вопросов = 1 круг)
            int currentCircle = (questionNumber - 1) / 7 + 1;
            Debug.Log($"[TawafLevelState] Circle {currentCircle} of Tawaf");
        }

        private void OnAnswerResult(bool wasCorrect, string explanation)
        {
            _questionsAnswered++;
            
            if (wasCorrect)
            {
                _correctAnswers++;
                _consecutiveCorrect++;
                
                Debug.Log($"[TawafLevelState] Correct answer! Streak: {_consecutiveCorrect}");
                
                // Бонус за серию правильных ответов (специфично для Tawaf)
                if (_consecutiveCorrect >= 3)
                {
                    int bonusGems = _consecutiveCorrect * 2;
                    _rewardSystem?.AwardGems(bonusGems);
                    Debug.Log($"[TawafLevelState] Streak bonus: +{bonusGems} gems");
                }
                
                // Специальный бонус за завершение полного круга (7 вопросов) без ошибок
                if (_consecutiveCorrect == 7)
                {
                    _rewardSystem?.AwardGems(20);
                    Debug.Log("[TawafLevelState] Perfect circle completed! +20 bonus gems");
                }
            }
            else
            {
                _consecutiveCorrect = 0;
                Debug.Log($"[TawafLevelState] Wrong answer. Streak reset. {explanation}");
            }
        }

        private void OnQuizComplete(float scorePercent)
        {
            float elapsedTime = Time.time - _startTime;
            Debug.Log($"[TawafLevelState] Tawaf completed with score: {scorePercent:F1}% in {elapsedTime:F0} seconds");
            
            // Сохранить прогресс
            if (GameManager.Instance?.ProgressService != null && _levelData != null)
            {
                //GameManager.Instance.ProgressService.UpdateLevelProgress(_levelData.LevelId, scorePercent);
                
                // Проверить на идеальное прохождение
                if (scorePercent == 100f)
                {
                    Debug.Log("[TawafLevelState] Perfect Tawaf! All circles completed flawlessly!");
                    _rewardSystem?.AwardGems(50); // Большой бонус за идеальное прохождение
                }
                else if (scorePercent >= _levelData.PassThreshold)
                {
                    Debug.Log("[TawafLevelState] Tawaf accepted");
                }
                else
                {
                    Debug.Log("[TawafLevelState] Tawaf needs to be repeated");
                }
            }

            // Уведомить машину состояний о завершении
            _stateMachine?.CompleteLevel(scorePercent);
        }
    }
}

