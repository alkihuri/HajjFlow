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
        [SerializeField] protected LevelData levelData;
        
        [Header("UI References")]
        [SerializeField] protected QuizUIController quizUIController;
        [SerializeField] protected TheoryCardsManager theoryCardsManager;

        protected QuizService quizService;
        
        public string LevelId => levelData.LevelId;
        

        [SerializeField] private UIService _uiService;
        
        
        #if UNITY_EDITOR

        private void OnValidate()
        {
            quizUIController??= GetComponentInChildren<QuizUIController>(true);
            theoryCardsManager??= GetComponentInChildren<TheoryCardsManager>(true);
            
            var levelId = levelData != null ? levelData.LevelId : "null";
            
            // load form Resource/SO/Theory/ scriptabel object with name matching levelId
            var container = Resources.Load<TheoryCardContainer>($"SO/Theory/{levelId}/{levelId}_TheoryContainer");
            if (container == null)
            {
                Debug.LogWarning($"[{GetType().Name}] No TheoryCardContainer found for levelId '{levelId}' at path 'SO/Theory/{levelId}/{levelId}_TheoryContainer.asset'. " +
                                 $"Make sure to create and assign a TheoryCardContainer for this level.");
            }
            else
            {
                theoryCardsManager.CardContainer = container;
            }
            
           
            if (_uiService != null)
            {
                _uiService.RegisterLevelData(levelData);
            } 
             
        }
#endif
        
        
        protected virtual void Awake()
        {
          
            // Подписываемся на завершение теории
            if (theoryCardsManager != null)
            {
                theoryCardsManager.OnTheoryCardsCompleted.AddListener(OnTheoryCompleted);
            } 
             
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
            quizService.InitializeQuiz(levelData.Questions);
        }

        /// <summary>
        /// Вызывается из сцены при завершении этапа теории.
        /// Делегирует в текущее состояние уровня.
        /// </summary>
        public virtual void OnStageGameplayCompleted()
        {
            var sm = GameManager.Instance?.GetService<GameStateMachine>();
            var levelState = sm?.CurrentState as BaseLevelState;
            if (levelState != null)
            {
                levelState.CompleteTheoryStage();
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] No active level state to notify.");
            }
        }
    }
}

