using UnityEngine;
using HajjFlow.Data;
using HajjFlow.Services;

namespace HajjFlow.Core.LevelsLogic
{
    /// <summary>
    /// Контроллер уровня "Miqat" (Микат).
    /// Управляет прохождением блоков теории и запуском квиза.
    /// </summary>
    public class MiqatLevelController : MonoBehaviour
    {
        [SerializeField]
        private LevelData levelData;

        private StageCompletionService stageCompletionService;
        private QuizService quizService;
        
        private int currentStageIndex = 0;
        private const string LEVEL_ID = "Miqat";

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
                Debug.LogError("[MiqatLevelController] StageCompletionService not found!");
            }

            if (quizService == null)
            {
                Debug.LogError("[MiqatLevelController] QuizService not found!");
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
                Debug.LogError("[MiqatLevelController] LevelData is not assigned!");
                return;
            }

            currentStageIndex = 0;
            Debug.Log($"[MiqatLevelController] Starting level: {levelData.LevelName}");
            
            StartCurrentStage();
        }

        private void StartCurrentStage()
        {
            Debug.Log($"[MiqatLevelController] Starting stage {currentStageIndex}");
        }

        public void OnStageGameplayCompleted()
        {
            Debug.Log($"[MiqatLevelController] Stage {currentStageIndex} gameplay completed");
            
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

            Debug.Log($"[MiqatLevelController] Stage {stageIndex} verified and completed");
            
            currentStageIndex++;
            
            if (currentStageIndex < 2) // 2 блока теории для Miqat
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
            Debug.LogWarning($"[MiqatLevelController] Stage completion failed: {reason}");
        }

        private void StartQuiz()
        {
            if (levelData == null || levelData.Questions == null || levelData.Questions.Length == 0)
            {
                Debug.LogError("[MiqatLevelController] No questions loaded for quiz!");
                return;
            }

            Debug.Log($"[MiqatLevelController] Starting quiz with {levelData.Questions.Length} questions");
            
            if (quizService != null)
            {
                quizService.InitializeQuiz(levelData.Questions);
            }
        }

        private void HandleQuizCompleted(int totalQuestions, int correctAnswers)
        {
            Debug.Log($"[MiqatLevelController] Quiz completed! Score: {correctAnswers}/{totalQuestions}");
            
            int percentage = (correctAnswers * 100) / totalQuestions;
            bool levelPassed = percentage >= levelData.PassThreshold;
            
            if (levelPassed)
            {
                Debug.Log($"[MiqatLevelController] Level PASSED! Score: {percentage}%");
            }
            else
            {
                Debug.Log($"[MiqatLevelController] Level FAILED! Score: {percentage}% (need {levelData.PassThreshold}%)");
            }
        }

        private bool TryGetService<T>(out T service) where T : MonoBehaviour
        {
            service = FindFirstObjectByType<T>();
            return service != null;
        }
    }
}

