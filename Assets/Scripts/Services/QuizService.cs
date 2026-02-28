using System;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Services
{
    /// <summary>
    /// Сервис для управления квизом (вопросами и проверкой ответов).
    /// Контролирует текущий вопрос, валидацию ответов и прогресс.
    /// </summary>
    public class QuizService : MonoBehaviour
    {
        /// <summary>Событие срабатывает при отображении нового вопроса</summary>
        public event Action<QuizQuestion> OnQuestionDisplayed;
        
        /// <summary>Событие срабатывает при правильном ответе</summary>
        public event Action<int> OnAnswerCorrect;
        
        /// <summary>Событие срабатывает при неправильном ответе</summary>
        public event Action<int> OnAnswerIncorrect;
        
        /// <summary>Событие срабатывает при завершении всех вопросов</summary>
        public event Action<int, int> OnQuizCompleted; // totalQuestions, correctAnswers

        private QuizQuestion[] currentQuestions;
        private int currentQuestionIndex = 0;
        private int correctAnswerCount = 0;

        /// <summary>Инициализирует квиз с массивом вопросов</summary>
        public void InitializeQuiz(QuizQuestion[] questions)
        {
            if (questions == null || questions.Length == 0)
            {
                Debug.LogError("[QuizService] Questions array is empty!");
                return;
            }

            currentQuestions = questions;
            currentQuestionIndex = 0;
            correctAnswerCount = 0;

            Debug.Log($"[QuizService] Quiz initialized with {questions.Length} questions");
            DisplayCurrentQuestion();
        }

        /// <summary>Отображает текущий вопрос</summary>
        public void DisplayCurrentQuestion()
        {
            if (currentQuestions == null || currentQuestionIndex >= currentQuestions.Length)
            {
                Debug.LogError("[QuizService] No questions available");
                return;
            }

            QuizQuestion question = currentQuestions[currentQuestionIndex];
            Debug.Log($"[QuizService] Question {currentQuestionIndex + 1}/{currentQuestions.Length}: {question.QuestionText}");
            OnQuestionDisplayed?.Invoke(question);
        }

        /// <summary>
        /// Проверяет ответ пользователя и переходит к следующему вопросу.
        /// </summary>
        /// <param name="selectedAnswerIndex">Индекс выбранного ответа (0-3)</param>
        /// <returns>True если ответ правильный, False если неправильный</returns>
        public bool SubmitAnswer(int selectedAnswerIndex)
        {
            if (currentQuestions == null || currentQuestionIndex >= currentQuestions.Length)
            {
                Debug.LogError("[QuizService] Quiz is not initialized or already completed");
                return false;
            }

            QuizQuestion currentQuestion = currentQuestions[currentQuestionIndex];

            // Проверка индекса ответа
            if (selectedAnswerIndex < 0 || selectedAnswerIndex >= currentQuestion.Options.Length)
            {
                Debug.LogWarning($"[QuizService] Invalid answer index: {selectedAnswerIndex}");
                return false;
            }

            bool isCorrect = selectedAnswerIndex == currentQuestion.CorrectAnswerIndex;

            if (isCorrect)
            {
                correctAnswerCount++;
                Debug.Log($"[QuizService] ✓ Correct! Gems: {currentQuestion.GemsReward}");
                OnAnswerCorrect?.Invoke(currentQuestion.GemsReward);
            }
            else
            {
                Debug.Log($"[QuizService] ✗ Incorrect. Correct answer: {currentQuestion.Options[currentQuestion.CorrectAnswerIndex]}");
                OnAnswerIncorrect?.Invoke(currentQuestion.CorrectAnswerIndex);
            }

            // Переход к следующему вопросу
            currentQuestionIndex++;

            if (currentQuestionIndex >= currentQuestions.Length)
            {
                // Квиз завершён
                CompleteQuiz();
            }
            else
            {
                DisplayCurrentQuestion();
            }

            return isCorrect;
        }

        /// <summary>Завершает квиз и отправляет события</summary>
        private void CompleteQuiz()
        {
            Debug.Log($"[QuizService] Quiz completed! Correct: {correctAnswerCount}/{currentQuestions.Length}");
            OnQuizCompleted?.Invoke(currentQuestions.Length, correctAnswerCount);
        }

        /// <summary>Возвращает текущий вопрос</summary>
        public QuizQuestion GetCurrentQuestion()
        {
            if (currentQuestions != null && currentQuestionIndex < currentQuestions.Length)
            {
                return currentQuestions[currentQuestionIndex];
            }
            return null;
        }

        /// <summary>Возвращает индекс текущего вопроса (0-based)</summary>
        public int GetCurrentQuestionIndex()
        {
            return currentQuestionIndex;
        }

        /// <summary>Возвращает общее количество вопросов</summary>
        public int GetTotalQuestions()
        {
            return currentQuestions?.Length ?? 0;
        }

        /// <summary>Возвращает количество правильных ответов</summary>
        public int GetCorrectAnswerCount()
        {
            return correctAnswerCount;
        }

        /// <summary>Сбрасывает квиз</summary>
        public void ResetQuiz()
        {
            currentQuestions = null;
            currentQuestionIndex = 0;
            correctAnswerCount = 0;
            Debug.Log("[QuizService] Quiz reset");
        }
    }
}

