using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HajjFlow.Data
{
    /// <summary>
    /// ScriptableObject that holds all configuration for a single game level.
    /// Create via: Assets → Create → Manasik → Level Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Manasik/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Unique identifier used to look up progress / completion state.</summary>
        public string LevelId = "";

        /// <summary>Display name shown in the UI (e.g. "Preparation for Hajj").</summary>
        public string LevelName = "";

        /// <summary>Short description shown on the level-selection map.</summary>
        [TextArea(2, 4)]
        public string Description = "";

        [Header("Localization Keys")]
        /// <summary>Localization key used to look up the level description via LocalizationService.</summary>
        public string LevelDescriptionKey = "";

        /// <summary>Localization keys for quiz question texts (parallel to Questions array).</summary>
        public List<string> QuestionTextKeys = new List<string>();

        [Header("Visuals")]
        /// <summary>Thumbnail displayed on the level-selection map tile.</summary>
        public Sprite Thumbnail;

        [Header("Quiz")]
        /// <summary>Questions the player must answer to complete this level.</summary>
        public QuizQuestion[] Questions;

        /// <summary>Total number of questions configured for this level.</summary>
        public int QuestionCount => Questions != null ? Questions.Length : 0;

        [Header("Rewards")]
        /// <summary>Bonus gems awarded when the player completes the level for the first time.</summary>
        public int CompletionBonusGems = 20;

        /// <summary>Minimum percentage score needed to pass (0–100).</summary>
        [Range(0, 100)]
        public int PassThreshold = 60;

        /// <summary>
        /// Загружает вопросы из JSON файла через диалоговое окно выбора файла.
        /// Вызывается через контекст-меню инспектора.
        /// </summary>
        [ContextMenu("Load Questions from JSON")]
        public void LoadQuestionsFromJson()
        {
#if UNITY_EDITOR
            // Открываем диалог выбора файла
            string path = EditorUtility.OpenFilePanel("Select JSON Quiz File", "Assets", "json");
            
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("File selection cancelled");
                return;
            }

            try
            {
                // Читаем содержимое файла
                string jsonContent = System.IO.File.ReadAllText(path);
                
                // Извлекаем метаданные уровня из первого элемента
                var metadata = QuizQuestion.ExtractLevelMetadata(jsonContent);
                if (metadata != null)
                {
                    LevelId = metadata.LevelId;
                    LevelName = metadata.LevelName;
                    Description = metadata.Description;
                    LevelDescriptionKey = metadata.Description; // Используем Description как ключ локализации
                    Debug.Log($"[{name}] Loaded level metadata: Id={LevelId}, Name={LevelName}");
                }
                
                // Парсим вопросы (метод автоматически пропускает элементы без QuestionText)
                Questions = QuizQuestion.FromJsonArray(jsonContent);
                
                if (Questions == null || Questions.Length == 0)
                {
                    Debug.LogWarning($"[{name}] No questions loaded from file: {path}");
                    return;
                }

                Debug.Log($"[{name}] Successfully loaded {Questions.Length} questions from:\n{path}");
                
                // Выводим для проверки
                for (int i = 0; i < Questions.Length; i++)
                {
                    Debug.Log($"  Q{i+1}: {Questions[i].QuestionText}");
                }

                EditorUtility.SetDirty(this);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{name}] Failed to load questions: {ex.Message}\n{ex.StackTrace}");
            }
#else
            Debug.LogError("[" + name + "] This function only works in the Editor!");
#endif
        }
    }
}
