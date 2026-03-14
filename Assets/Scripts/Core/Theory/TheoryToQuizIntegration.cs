using UnityEngine;
using HajjFlow.Services;
using HajjFlow.Data;

namespace Core.Theory
{
    /// <summary>
    /// Example integration of Theory Cards with Quiz System
    /// Shows how to connect card completion with quiz unlock
    /// </summary>
    public class TheoryToQuizIntegration : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TheoryCardsManager _cardManager;
        [SerializeField] private QuizService _quizService;
        
        [Header("Quiz Data")]
      //  [SerializeField] private QuizData _quizData;
        [SerializeField] private int _requiredCardsForQuiz = 5;
        
        [Header("UI")]
        [SerializeField] private GameObject _quizButton;
        [SerializeField] private GameObject _cardsPanel;
        [SerializeField] private GameObject _quizPanel;
        
        private int _completedCards = 0;

        private void Start()
        {
            if (_quizButton != null)
                _quizButton.SetActive(false);
                
            SubscribeToCardEvents();
        }

        private void SubscribeToCardEvents()
        {
            // Find all cards in the manager's container
            TheoryCardBase[] cards = _cardManager.GetComponentsInChildren<TheoryCardBase>(true);
            
            
        }

        private void OnCardCompleted(TheoryCardBase card)
        {
            _completedCards++;
            
            Debug.Log($"[TheoryToQuiz] Card completed: {card.Data?.Title} ({_completedCards}/{_requiredCardsForQuiz})");
            
            // Save progress
            SaveCardProgress(card);
            
            // Check if quiz should be unlocked
            if (_completedCards >= _requiredCardsForQuiz)
            {
                UnlockQuiz();
            }
        }

        private void SaveCardProgress(TheoryCardBase card)
        {
            if (card.Data == null) return;
            
            // Save to PlayerPrefs or your save system
            string key = $"Card_{card.Data.Title}_Completed";
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            
            Debug.Log($"[TheoryToQuiz] Progress saved: {key}");
        }

        private void UnlockQuiz()
        {
            Debug.Log("[TheoryToQuiz] Quiz unlocked!");
            
            if (_quizButton != null)
            {
                _quizButton.SetActive(true);
            }
        }
/*
        /// <summary>
        /// Call this from UI button
        /// </summary>
        public void StartQuiz()
        {
            if (_quizService == null || _quizData == null)
            {
                Debug.LogError("[TheoryToQuiz] Quiz service or data is missing!");
                return;
            }
            
            // Hide cards panel
            if (_cardsPanel != null)
                _cardsPanel.SetActive(false);
                
            // Show quiz panel
            if (_quizPanel != null)
                _quizPanel.SetActive(true);
            
            // Start quiz
            _quizService.StartQuiz(_quizData);
            
            // Subscribe to quiz completion
            _quizService.OnQuizCompleted += OnQuizCompleted;
        }

        private void OnQuizCompleted(int totalQuestions, int correctAnswers)
        {
            Debug.Log($"[TheoryToQuiz] Quiz completed! Score: {correctAnswers}/{totalQuestions}");
            
            // Unsubscribe
            _quizService.OnQuizCompleted -= OnQuizCompleted;
            
            // Calculate percentage
            float percentage = (float)correctAnswers / totalQuestions * 100f;
            
            if (percentage >= 70f)
            {
                OnQuizPassed();
            }
            else
            {
                OnQuizFailed();
            }
        }

        private void OnQuizPassed()
        {
            Debug.Log("[TheoryToQuiz] Quiz passed! Moving to next level...");
            
            // Unlock next level, award gems, etc.
            // You can integrate with your level system here
        }

        private void OnQuizFailed()
        {
            Debug.Log("[TheoryToQuiz] Quiz failed. Review the cards again.");
            
            // Reset cards or show review option
            if (_cardsPanel != null)
                _cardsPanel.SetActive(true);
                
            if (_quizPanel != null)
                _quizPanel.SetActive(false);
                
            // Optional: reset the card manager
            // _cardManager.ResetCards();
        }

        /// <summary>
        /// Call this to reset all progress
        /// </summary>
        public void ResetProgress()
        {
            _completedCards = 0;
            
            if (_quizButton != null)
                _quizButton.SetActive(false);
                
            // Reset card manager
            if (_cardManager != null)
                _cardManager.ResetCards();
                
            // Clear saved progress
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            Debug.Log("[TheoryToQuiz] Progress reset!");
        }
*/
        private void OnDestroy()
        {
            // Cleanup
            if (_quizService != null)
            {
               // _quizService.OnQuizCompleted -= OnQuizCompleted;
            }
        }
    }
}

