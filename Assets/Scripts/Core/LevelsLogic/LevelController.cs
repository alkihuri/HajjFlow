using System;
using Core.Theory;
using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Core.States;
using HajjFlow.UI;
using HajjFlow.Services;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Базовый класс для контроллеров уровней.
    /// Управляет блоком теории и квизом.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] public LevelData levelData;
        
        [Header("UI References")]
        [SerializeField] protected QuizUIController quizUIController;
        [SerializeField] protected TheoryCardsManager theoryCardsManager;

        protected QuizService quizService;

        public bool activeInHierarchy
        {
            
            get => gameObject.activeInHierarchy;
        }

        public string LevelId => levelData.LevelId;
        
 
        

        public void Init(LevelData level)
        { 
            levelData = level;
            quizUIController??= GetComponentInChildren<QuizUIController>(true);
            theoryCardsManager??= GetComponentInChildren<TheoryCardsManager>(true);
            if (theoryCardsManager != null)
            {
                theoryCardsManager.OnTheoryCardsCompleted.AddListener(OnTheoryCompleted);
            } 
            
            var levelId = levelData != null ? levelData.LevelId : "null";
            
            gameObject.name = $"Level {levelId} Controller";

            // Проверяем есть ли уже контейнер в levelData (для runtime моделей)
            TheoryCardContainer container = levelData.TheoryCardContainer;
            
            // Если контейнера нет - пытаемся загрузить из Resources (для статических моделей)
            if (container == null)
            {
                Debug.Log($"[{GetType().Name}] TheoryCardContainer not found in LevelData for '{levelId}'. Trying to load from Resources...");
                container = Resources.Load<TheoryCardContainer>($"SO/Theory/{levelId}/{levelId}_TheoryContainer");
                
                if (container == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] No TheoryCardContainer found for levelId '{levelId}' " +
                                     $"at path 'SO/Theory/{levelId}/{levelId}_TheoryContainer.asset' " +
                                     $"and not provided in LevelData. Make sure to create and assign a TheoryCardContainer for this level.");
                }
                else
                {
                    Debug.Log($"[{GetType().Name}] Loaded TheoryCardContainer from Resources for '{levelId}'");
                }
            }
            else
            {
                Debug.Log($"[{GetType().Name}] Using TheoryCardContainer from LevelData (runtime) for '{levelId}' with {container.Cards.Count} cards");
            }

            theoryCardsManager.CardContainer = container;
            
            Debug.Log($"[{GetType().Name}] Initialized LevelController for '{levelId}' with {(container?.Cards.Count ?? 0)} theory cards");
        }
        
        
      

        
        protected virtual void OnDestroy()
        {
            if (theoryCardsManager != null)
            {
                theoryCardsManager.OnTheoryCardsCompleted.RemoveListener(OnTheoryCompleted);
            }
        }

         

        /// <summary>
        /// Сбрасывает состояние уровня к начальному.
        /// </summary>
        public virtual void ResetLevel()
        {
            // Reset quiz UI
            if (quizUIController != null)
            {
                quizUIController.ResetUI();
            }
            
            // Reset theory cards and show them
            if (theoryCardsManager != null)
            {
                theoryCardsManager.ResetToStart();
                theoryCardsManager.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Показывает блок теории.
        /// </summary>
        public virtual void ShowTheory()
        { 
            if (theoryCardsManager != null)
            {
                theoryCardsManager.gameObject.SetActive(true);
                theoryCardsManager.ShowCard(0);
            }
        }

        /// <summary>
        /// Вызывается когда все карточки теории просмотрены.
        /// </summary>
        protected virtual void OnTheoryCompleted()
        {
            StartQuiz();
        }

        /// <summary>
        /// Начинает квиз после прохождения теории.
        /// </summary>
        protected virtual void StartQuiz()
        {
            if (levelData == null || levelData.Questions == null || levelData.Questions.Length == 0)
            {
                Debug.LogError($"[{GetType().Name}] No questions loaded for quiz!");
                return;
            }

            // Скрываем теорию
            if (theoryCardsManager != null)
            {
                theoryCardsManager.gameObject.SetActive(false);
            }

            // Получаем QuizService
            quizService = GameManager.Instance?.quizService;
            if (quizService == null)
            {
                Debug.LogError($"[{GetType().Name}] QuizService not found!");
                return;
            }

            // Проверяем QuizUIController
            if (quizUIController == null)
            {
                quizUIController = FindFirstObjectByType<QuizUIController>();
            }
            if (quizUIController == null)
            {
                Debug.LogError($"[{GetType().Name}] QuizUIController not found!");
                return;
            }

            // 1. Активируем UI квиза
            quizUIController.gameObject.SetActive(true);
            
            // 2. Инициализируем UI и подписываем события
            quizUIController.Init(quizService);
            
            // 3. Инициализируем квиз с вопросами
            quizService.InitializeQuiz(levelData.LevelId, levelData.Questions);
        }

        /// <summary>
        /// Вызывается из сцены при завершении этапа теории.
        /// Делегирует в текущее состояние уровня.
        /// </summary>
        public virtual void OnStageGameplayCompleted()
        {
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            var levelState = sm?.CurrentState as LevelState;
            if (levelState != null)
            {
                levelState.CompleteTheoryStage();
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] No active level state to notify.");
            }
        }

        public void SetActive(bool state)
        {
            gameObject.SetActive(state);
        }
    }
}
