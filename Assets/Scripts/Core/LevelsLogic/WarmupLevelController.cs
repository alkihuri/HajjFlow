using Core.Theory;
using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Core.States;
using HajjFlow.UI;
using HajjFlow.Services;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Контроллер уровня "Warmup" (Подготовка к Хаджу).
    /// Управляет блоком теории и квизом для первого уровня.
    /// </summary>
    public class WarmupLevelController : MonoBehaviour
    {
        [SerializeField] private LevelData levelData;
        [SerializeField] private QuizUIController _quizUIController;
        [SerializeField] private TheoryCardsManager _theoryCardsManager;

        private QuizService _quizService;

        private void Awake()
        {
            // Подписываемся на завершение теории
            if (_theoryCardsManager != null)
            {
                _theoryCardsManager.OnTheoryCardsCompleted.AddListener(OnTheoryCompleted);
            }
        }

        private void OnDestroy()
        {
            if (_theoryCardsManager != null)
            {
                _theoryCardsManager.OnTheoryCardsCompleted.RemoveListener(OnTheoryCompleted);
            }
        }

        /// <summary>
        /// Сбрасывает состояние уровня к начальному.
        /// Вызывается при входе/выходе из WarmupState.
        /// </summary>
        public void ResetLevel()
        {
            Debug.Log("[WarmupLevelController] Resetting level state");
            
            // Reset quiz UI
            if (_quizUIController != null)
            {
                _quizUIController.ResetUI();
            }
            
            // Reset theory cards and show them
            if (_theoryCardsManager != null)
            {
                _theoryCardsManager.ResetToStart();
                _theoryCardsManager.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Показывает блок теории.
        /// </summary>
        public void ShowTheory()
        { 
            if (_theoryCardsManager != null)
            {
                _theoryCardsManager.gameObject.SetActive(true);
                _theoryCardsManager.ShowCard(0);
            }
        }

        /// <summary>
        /// Вызывается когда все карточки теории просмотрены.
        /// </summary>
        private void OnTheoryCompleted()
        {
            Debug.Log("[WarmupLevelController] Theory completed, starting quiz");
            StartQuiz();
        }

        /// <summary>
        /// Начинает квиз после прохождения теории.
        /// </summary>
        private void StartQuiz()
        {
            if (levelData == null || levelData.Questions == null || levelData.Questions.Length == 0)
            {
                Debug.LogError("[WarmupLevelController] No questions loaded for quiz!");
                return;
            }

            // Скрываем теорию
            if (_theoryCardsManager != null)
            {
                _theoryCardsManager.gameObject.SetActive(false);
            }

            // Получаем QuizService
            _quizService = GameManager.Instance?.quizService;
            if (_quizService == null)
            {
                Debug.LogError("[WarmupLevelController] QuizService not found!");
                return;
            }

            // Проверяем QuizUIController
            if (_quizUIController == null)
            {
                _quizUIController = FindFirstObjectByType<QuizUIController>();
            }
            if (_quizUIController == null)
            {
                Debug.LogError("[WarmupLevelController] QuizUIController not found!");
                return;
            }

            Debug.Log($"[WarmupLevelController] Starting quiz with {levelData.Questions.Length} questions");

            // 1. Активируем UI квиза
            _quizUIController.gameObject.SetActive(true);
            
            // 2. Инициализируем UI и подписываем события
            _quizUIController.Init(_quizService);
            
            // 3. Инициализируем квиз с вопросами (вызовет OnQuestionDisplayed)
            _quizService.InitializeQuiz(levelData.Questions);
        }
    }
}