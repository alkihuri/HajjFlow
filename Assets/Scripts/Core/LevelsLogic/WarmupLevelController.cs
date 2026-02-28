using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Services;
using HajjFlow.UI;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Контроллер уровня "Warmup" (Подготовка к Хаджу).
    /// Управляет прохождением блоков теории и запуском квиза.
    /// Регистрируется как точка входа для первого уровня.
    /// </summary>
    public class WarmupLevelController : MonoBehaviour
    {
        [SerializeField]
        private LevelData levelData;

        private StageCompletionService stageCompletionService;
        private QuizService quizService;
        
        [SerializeField] private QuizUIController _quizUIController;
        
        private int currentStageIndex = 0;
        private const string LEVEL_ID = "Warmup";

        private void Awake()
        {
            // Получаем сервисы из GameManager или ищем в сцене
            if (!TryGetService<StageCompletionService>(out stageCompletionService))
            {
                stageCompletionService = GetComponent<StageCompletionService>();
            }

            if (!TryGetService<QuizService>(out quizService))
            {
                quizService = GetComponent<QuizService>();
            }

            if (stageCompletionService == null)
            {
                Debug.LogError("[WarmupLevelController] StageCompletionService not found!");
            }

            if (quizService == null)
            {
                Debug.LogError("[WarmupLevelController] QuizService not found!");
            }
        }

        private void OnEnable()
        {
            // Подписываемся на события сервисов
            if (stageCompletionService != null)
            {
                stageCompletionService.OnStageCompleted += HandleStageCompleted;
                stageCompletionService.OnStageCompletionFailed += HandleStageCompletionFailed;
            }

            if (quizService != null)
            {
                quizService.OnQuizCompleted += HandleQuizCompleted;
            }
        }

        private void OnDisable()
        {
            // Отписываемся от событий
            if (stageCompletionService != null)
            {
                stageCompletionService.OnStageCompleted -= HandleStageCompleted;
                stageCompletionService.OnStageCompletionFailed -= HandleStageCompletionFailed;
            }

            if (quizService != null)
            {
                quizService.OnQuizCompleted -= HandleQuizCompleted;
            }
        }

        /// <summary>
        /// Начинает прохождение уровня.
        /// Запускает первый блок теории.
        /// </summary>
        public void StartLevel()
        {
            if (levelData == null)
            {
                Debug.LogError("[WarmupLevelController] LevelData is not assigned!");
                return;
            }

            currentStageIndex = 0;
            Debug.Log($"[WarmupLevelController] Starting level: {levelData.LevelName}");
            
            StartCurrentStage();
        }

        /// <summary>
        /// Запускает текущий блок теории (мини-игра/интерактивный контент).
        /// </summary>
        private void StartCurrentStage()
        {
            Debug.Log($"[WarmupLevelController] Starting stage {currentStageIndex}");


           
        }

        /// <summary>
        /// Вызывается из мини-игры/блока теории когда он завершён.
        /// Верифицирует блок через сервис.
        /// </summary>
        public void OnStageGameplayCompleted()
        {
            Debug.Log($"[WarmupLevelController] Stage {currentStageIndex} gameplay completed");
            
            // Верифицируем завершение блока через сервис
            if (stageCompletionService != null)
            {
                stageCompletionService.CompleteStage(LEVEL_ID, currentStageIndex);
            }
        }

        /// <summary>
        /// Обработчик события завершения блока теории.
        /// Переходит к следующему блоку или к квизу.
        /// </summary>
        private void HandleStageCompleted(string levelId, int stageIndex)
        {
            if (levelId != LEVEL_ID)
            {
                return; // Событие от другого уровня
            }

            Debug.Log($"[WarmupLevelController] Stage {stageIndex} verified and completed");
            
            // Проверяем, остались ли ещё блоки теории
            currentStageIndex++;
            
            if (currentStageIndex < 3) // 3 блока теории для Warmup
            {
                // Переходим к следующему блоку теории
                StartCurrentStage();
            }
            else
            {
                // Все блоки теории пройдены, начинаем квиз
                StartQuiz();
            }
        }

        /// <summary>
        /// Обработчик события ошибки при завершении блока.
        /// </summary>
        private void HandleStageCompletionFailed(string reason)
        {
            Debug.LogWarning($"[WarmupLevelController] Stage completion failed: {reason}");
            // TODO: Показать ошибку пользователю или повторить попытку
        }

        /// <summary>
        /// Начинает квиз после прохождения всех блоков теории.
        /// </summary>
        private void StartQuiz()
        {
            if (levelData == null || levelData.Questions == null || levelData.Questions.Length == 0)
            {
                Debug.LogError("[WarmupLevelController] No questions loaded for quiz!");
                return;
            }

            Debug.Log($"[WarmupLevelController] Starting quiz with {levelData.Questions.Length} questions");
            
            if (quizService != null)
            {
                quizService.InitializeQuiz(levelData.Questions);
            }
            
            GameManager.Instance.GetService<UIService>().ShowUpQuizUI(_quizUIController);
            
            
            if (_quizUIController == null)
            {
                _quizUIController = FindObjectOfType<QuizUIController>();
            }
            if (_quizUIController == null)
            {
                Debug.LogError("[WarmupLevelController] QuizUIController not found in scene!");
                return;
            }
            
            _quizUIController.Init();
            // TODO: Переключить UI на экран квиза
        }

        /// <summary>
        /// Обработчик события завершения квиза.
        /// </summary>
        private void HandleQuizCompleted(int totalQuestions, int correctAnswers)
        {
            Debug.Log($"[WarmupLevelController] Quiz completed! Score: {correctAnswers}/{totalQuestions}");
            
            // Проверяем, пройден ли уровень (минимум passThreshold%)
            int percentage = (correctAnswers * 100) / totalQuestions;
            bool levelPassed = percentage >= levelData.PassThreshold;
            
            if (levelPassed)
            {
                Debug.Log($"[WarmupLevelController] Level PASSED! Score: {percentage}%");
                // TODO: Показать экран успеха, начислить награды
            }
            else
            {
                Debug.Log($"[WarmupLevelController] Level FAILED! Score: {percentage}% (need {levelData.PassThreshold}%)");
                // TODO: Показать экран повтора
            }
        }

        /// <summary>
        /// Вспомогательный метод для поиска сервиса в сцене.
        /// </summary>
        private bool TryGetService<T>(out T service) where T : MonoBehaviour
        {
            service = FindFirstObjectByType<T>();
            return service != null;
        }
    }
}
