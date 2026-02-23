using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Services;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Контроллер уровня "Tawaf" (Таваф).
    /// Управляет прохождением блоков теории и запуском квиза.
    /// </summary>
    public class TawafLevelController : MonoBehaviour
    {
        [SerializeField]
        private LevelData levelData;

        private StageCompletionService stageCompletionService;
        private QuizService quizService;
        
        private int currentStageIndex = 0;
        private const string LEVEL_ID = "Tawaf";

        private void Awake()
        {
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
                Debug.LogError("[TawafLevelController] StageCompletionService not found!");
            }

            if (quizService == null)
            {
                Debug.LogError("[TawafLevelController] QuizService not found!");
            }
        }

        private void OnEnable()
        {
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

        public void StartLevel()
        {
            if (levelData == null)
            {
                Debug.LogError("[TawafLevelController] LevelData is not assigned!");
                return;
            }

            currentStageIndex = 0;
            Debug.Log($"[TawafLevelController] Starting level: {levelData.LevelName}");
            
            StartCurrentStage();
        }

        private void StartCurrentStage()
        {
            Debug.Log($"[TawafLevelController] Starting stage {currentStageIndex}");
        }

        public void OnStageGameplayCompleted()
        {
            Debug.Log($"[TawafLevelController] Stage {currentStageIndex} gameplay completed");
            
            if (stageCompletionService != null)
            {
                stageCompletionService.CompleteStage(LEVEL_ID, currentStageIndex);
            }
        }

        private void HandleStageCompleted(string levelId, int stageIndex)
        {
            if (levelId != LEVEL_ID)
            {
                return;
            }

            Debug.Log($"[TawafLevelController] Stage {stageIndex} verified and completed");
            
            currentStageIndex++;
            
            if (currentStageIndex < 2) // 2 блока теории для Tawaf
            {
                StartCurrentStage();
            }
            else
            {
                StartQuiz();
            }
        }

        private void HandleStageCompletionFailed(string reason)
        {
            Debug.LogWarning($"[TawafLevelController] Stage completion failed: {reason}");
        }

        private void StartQuiz()
        {
            if (levelData == null || levelData.Questions == null || levelData.Questions.Length == 0)
            {
                Debug.LogError("[TawafLevelController] No questions loaded for quiz!");
                return;
            }

            Debug.Log($"[TawafLevelController] Starting quiz with {levelData.Questions.Length} questions");
            
            if (quizService != null)
            {
                quizService.InitializeQuiz(levelData.Questions);
            }
        }

        private void HandleQuizCompleted(int totalQuestions, int correctAnswers)
        {
            Debug.Log($"[TawafLevelController] Quiz completed! Score: {correctAnswers}/{totalQuestions}");
            
            int percentage = (correctAnswers * 100) / totalQuestions;
            bool levelPassed = percentage >= levelData.PassThreshold;
            
            if (levelPassed)
            {
                Debug.Log($"[TawafLevelController] Level PASSED! Score: {percentage}%");
            }
            else
            {
                Debug.Log($"[TawafLevelController] Level FAILED! Score: {percentage}% (need {levelData.PassThreshold}%)");
            }
        }

        private bool TryGetService<T>(out T service) where T : MonoBehaviour
        {
            service = FindFirstObjectByType<T>();
            return service != null;
        }
    }
}

