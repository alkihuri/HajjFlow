using System;
using System.Collections.Generic;
using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Core;

namespace HajjFlow.Gameplay
{
    /// <summary>
    /// Drives the multiple-choice quiz for the active level.
    /// Attach to a GameObject in the Gameplay scene.
    ///
    /// Usage:
    ///   1. Call Initialise(levelData) when the scene loads.
    ///   2. Subscribe to OnQuestionReady, OnAnswerResult, and OnQuizComplete.
    ///   3. Call SubmitAnswer(index) when the player picks an option.
    /// </summary>
    public class QuizSystem : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when a new question is ready to display.</summary>
        public event Action<QuizQuestion, int> OnQuestionReady;   // (question, questionNumber)

        /// <summary>Fired after the player answers. bool = wasCorrect.</summary>
        public event Action<bool, string> OnAnswerResult;         // (wasCorrect, explanation)

        /// <summary>Fired when all questions have been answered.</summary>
        public event Action<float> OnQuizComplete;                // (scorePercent)

        // ── State ────────────────────────────────────────────────────────────────

        private QuizQuestion[] _questions;
        private int   _currentIndex    = 0;
        private int   _correctCount    = 0;
        private bool  _awaitingAnswer  = false;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Prepares the quiz with questions from the supplied level data.</summary>
        public void Initialise(LevelData levelData)
        {
            if (levelData == null || levelData.Questions == null || levelData.Questions.Length == 0)
            {
                Debug.LogWarning("[QuizSystem] LevelData has no questions.");
                return;
            }

            _questions     = levelData.Questions;
            _currentIndex  = 0;
            _correctCount  = 0;
            _awaitingAnswer = true;

            ShowCurrentQuestion();
        }

        /// <summary>
        /// Call this when the player taps an answer button.
        /// Has no effect if no question is currently active.
        /// </summary>
        public void SubmitAnswer(int selectedIndex)
        {
            if (!_awaitingAnswer || _questions == null) return;

            _awaitingAnswer = false;
            var question = _questions[_currentIndex];
            bool correct = (selectedIndex == question.CorrectAnswerIndex);

            if (correct)
            {
                _correctCount++;
                // Award gems for a correct answer
                GameManager.Instance?.AddGems(question.GemsReward);
            }

            OnAnswerResult?.Invoke(correct, question.Explanation);
        }

        /// <summary>Advances to the next question or finalises the quiz.</summary>
        public void Advance()
        {
            _currentIndex++;

            if (_currentIndex >= _questions.Length)
            {
                float scorePercent = (_questions.Length > 0)
                    ? (float)_correctCount / _questions.Length * 100f
                    : 0f;

                Debug.Log($"[QuizSystem] Quiz complete. Score: {scorePercent:F1}%");
                OnQuizComplete?.Invoke(scorePercent);
            }
            else
            {
                _awaitingAnswer = true;
                ShowCurrentQuestion();
            }
        }

        /// <summary>Returns the total number of questions in this quiz.</summary>
        public int TotalQuestions => _questions?.Length ?? 0;

        /// <summary>Returns the 1-based number of the current question.</summary>
        public int CurrentQuestionNumber => _currentIndex + 1;

        // ── Private ──────────────────────────────────────────────────────────────

        private void ShowCurrentQuestion()
        {
            OnQuestionReady?.Invoke(_questions[_currentIndex], _currentIndex + 1);
        }
    }
}
