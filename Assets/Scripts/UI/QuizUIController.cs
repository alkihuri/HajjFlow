using UnityEngine;
using UnityEngine.UI;
using HajjFlow.Data;
using HajjFlow.Services;
using HajjFlow.Core;

namespace HajjFlow.UI
{
    /// <summary>
    /// Пример UI контроллера квиза.
    /// Взаимодействует с QuizService для отображения вопросов и обработки ответов.
    /// </summary>
    public class QuizUIController : MonoBehaviour
    {
        [SerializeField] private Text questionText;
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private Text progressText;
        [SerializeField] private Image correctIndicator;
        [SerializeField] private Image incorrectIndicator;

        private QuizService quizService;

        private void OnEnable()
        {
            quizService = GameManager.Instance.quizService;
            
            if (quizService != null)
            {
                quizService.OnQuestionDisplayed += HandleQuestionDisplayed;
                quizService.OnAnswerCorrect += HandleAnswerCorrect;
                quizService.OnAnswerIncorrect += HandleAnswerIncorrect;
                quizService.OnQuizCompleted += HandleQuizCompleted;
            }
        }

        private void OnDisable()
        {
            if (quizService != null)
            {
                quizService.OnQuestionDisplayed -= HandleQuestionDisplayed;
                quizService.OnAnswerCorrect -= HandleAnswerCorrect;
                quizService.OnAnswerIncorrect -= HandleAnswerIncorrect;
                quizService.OnQuizCompleted -= HandleQuizCompleted;
            }
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

            // Отображаем текст вопроса
            questionText.text = question.QuestionText;

            // Отображаем варианты ответов
            for (int i = 0; i < answerButtons.Length && i < question.Options.Length; i++)
            {
                Text buttonText = answerButtons[i].GetComponentInChildren<Text>();
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
            gameObject.SetActive(false);
        }
    }
}

