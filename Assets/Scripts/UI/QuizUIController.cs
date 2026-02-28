using UnityEngine;
using UnityEngine.UI;
using HajjFlow.Data;
using HajjFlow.Services;
using HajjFlow.Core;
using HajjFlow.Core.States;
using TMPro;
using Unity.VisualScripting;

namespace HajjFlow.UI
{
    /// <summary>
    /// Пример UI контроллера квиза.
    /// Взаимодействует с QuizService для отображения вопросов и обработки ответов.
    /// </summary>
    public class QuizUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image correctIndicator;
        [SerializeField] private Image incorrectIndicator;

        [SerializeField] private QuizService quizService;

        
        [SerializeField] private GameObject _resultsPanel;
        [SerializeField] private TextMeshProUGUI _resultsText;
        [SerializeField] private Button _retryButton;
        
        private void OnEnable()
        {
            Init();
        }

        private void OnDisable()
        {
            UnsubscribeFromQuizService();
        }

        private void Start()
        {
            // Инициализируем кнопки ответов
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int answerIndex = i;
                answerButtons[i].onClick.AddListener(() => OnAnswerButtonClicked(answerIndex));
            }
        }

        /// <summary>
        /// Обработчик события отображения вопроса
        /// </summary>
        private void HandleQuestionDisplayed(QuizQuestion question)
        {
            if (question == null)
                return;
            
            Debug.Log("[QuizUIController] Displaying question: " + question.QuestionText);

            // Отображаем текст вопроса
            questionText.text = question.QuestionText;

            // Отображаем варианты ответов
            for (int i = 0; i < answerButtons.Length && i < question.Options.Length; i++)
            {
                var  buttonText = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = question.Options[i];
                }

                // Включаем кнопку
                answerButtons[i].interactable = true;
            }

            // Обновляем прогресс
            int current = quizService.GetCurrentQuestionIndex() + 1;
            int total = quizService.GetTotalQuestions();
            progressText.text = $"{current} / {total}";

            // Скрываем индикаторы результата
            if (correctIndicator != null) correctIndicator.gameObject.SetActive(false);
            if (incorrectIndicator != null) incorrectIndicator.gameObject.SetActive(false);
        }

        /// <summary>
        /// Обработчик при клике на кнопку ответа
        /// </summary>
        private void OnAnswerButtonClicked(int answerIndex)
        {
            // Отключаем все кнопки во время проверки
            foreach (Button btn in answerButtons)
            {
                btn.interactable = false;
            }

            // Проверяем ответ
            quizService.SubmitAnswer(answerIndex);
        }

        /// <summary>
        /// Обработчик при правильном ответе
        /// </summary>
        private void HandleAnswerCorrect(int gemsReward)
        {
            Debug.Log($"✓ Correct! +{gemsReward} gems");

            // Показываем индикатор успеха
            if (correctIndicator != null)
            {
                correctIndicator.gameObject.SetActive(true);
            }

            // Добавляем геммы в профиль
            GameManager.Instance.AddGems(gemsReward);
        }

        /// <summary>
        /// Обработчик при неправильном ответе
        /// </summary>
        private void HandleAnswerIncorrect(int correctIndex)
        {
            Debug.Log($"✗ Incorrect! Correct answer index: {correctIndex}");

            // Показываем индикатор ошибки
            if (incorrectIndicator != null)
            {
                incorrectIndicator.gameObject.SetActive(true);
            }

            // Можно подсветить правильный ответ
            if (correctIndex >= 0 && correctIndex < answerButtons.Length)
            {
                // Например, изменить цвет кнопки
                ColorBlock colors = answerButtons[correctIndex].colors;
                colors.normalColor = Color.green;
                answerButtons[correctIndex].colors = colors;
            }
        }

        /// <summary>
        /// Обработчик при завершении квиза
        /// </summary>
        private void HandleQuizCompleted(int totalQuestions, int correctAnswers)
        {
            int percentage = (correctAnswers * 100) / totalQuestions;

            Debug.Log($"Quiz Completed! Score: {correctAnswers}/{totalQuestions} ({percentage}%)");

            // TODO: Показать экран результатов
            // Можно открыть отдельный UI с результатами, кнопкой "Retry" или "Next Level"
            
            _resultsPanel.SetActive(true);  
            _resultsText.text = $"Your Score: {correctAnswers} / {totalQuestions} ({percentage}%)";
                
            //gameObject.SetActive(false);
        }

        [ContextMenu("Init")]
        public void Init()
        {
            Init(GameManager.Instance.quizService);
            _retryButton.onClick.AddListener(() =>
            {
                
                    GameManager.Instance.GetService<GameStateMachine>().ChangeState(GameStateIds.LevelSelect);
                
            });
        }
        
        /// <summary>
        /// Инициализирует UI контроллер с переданным QuizService.
        /// Вызывается перед InitializeQuiz, чтобы события были подписаны.
        /// </summary>
        public void Init(QuizService service)
        {
            // Отписываемся от старого сервиса если был
            UnsubscribeFromQuizService();
            
            quizService = service;

            if (quizService != null)
            {
                quizService.OnQuestionDisplayed += HandleQuestionDisplayed;
                quizService.OnAnswerCorrect += HandleAnswerCorrect;
                quizService.OnAnswerIncorrect += HandleAnswerIncorrect;
                quizService.OnQuizCompleted += HandleQuizCompleted;
                Debug.Log("[QuizUIController] Subscribed to QuizService events");
            }
            else
            {
                Debug.LogError("[QuizUIController] QuizService is null!");
            }
            
            // НЕ вызываем DisplayCurrentQuestion здесь - это сделает InitializeQuiz
        }
        
        private void UnsubscribeFromQuizService()
        {
            if (quizService != null)
            {
                quizService.OnQuestionDisplayed -= HandleQuestionDisplayed;
                quizService.OnAnswerCorrect -= HandleAnswerCorrect;
                quizService.OnAnswerIncorrect -= HandleAnswerIncorrect;
                quizService.OnQuizCompleted -= HandleQuizCompleted;
            }
        }

        
    }
}