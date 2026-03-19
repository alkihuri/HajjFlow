#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using HajjFlow.Data;
using Core.Theory;

namespace HajjFlow.Editor.ContentLoader
{
    /// <summary>
    /// Static helper class for JSON parsing and ScriptableObject data loading.
    /// Handles questions (LevelData) and theory cards (TheoryCardContainer).
    /// </summary>
    public static class DataLoader
    {
        // ── Questions (LevelData) ────────────────────────────────────────────

        /// <summary>
        /// Parses a JSON file and loads quiz questions into a LevelData asset.
        /// Creates or overwrites the asset at the default path.
        /// </summary>
        /// <param name="jsonPath">Absolute path to the JSON file.</param>
        /// <returns>The loaded/updated LevelData asset, or null on failure.</returns>
        public static LevelData LoadQuestionsFromJson(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogError("[DataLoader] JSON path is null or empty.");
                return null;
            }

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[DataLoader] File not found: {jsonPath}");
                return null;
            }

            string jsonContent;
            try
            {
                jsonContent = File.ReadAllText(jsonPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataLoader] Failed to read file: {ex.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                Debug.LogError("[DataLoader] JSON file is empty.");
                return null;
            }

            QuizQuestion[] questions;
            LevelMetadata metadata;
            try
            {
                questions = QuizQuestion.FromJsonArray(jsonContent);
                metadata = QuizQuestion.ExtractLevelMetadata(jsonContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataLoader] Failed to parse JSON: {ex.Message}");
                return null;
            }

            if (questions == null || questions.Length == 0)
            {
                Debug.LogError("[DataLoader] No questions parsed from JSON.");
                return null;
            }

            // Используем LevelId для именования файла, если он есть
            string assetName = (metadata != null && !string.IsNullOrEmpty(metadata.LevelId)) 
                ? $"{metadata.LevelId}_LevelData" 
                : "LoadedLevelData";

            // Find or create a LevelData asset
            const string assetFolder = "Assets/Resources/SO/Levels";
            string assetPath = $"{assetFolder}/{assetName}.asset";

            if (!AssetDatabase.IsValidFolder(assetFolder))
            {
                EnsureFolderExists(assetFolder);
            }

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (levelData == null)
            {
                levelData = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(levelData, assetPath);
                Debug.Log($"[DataLoader] Created new LevelData asset at {assetPath}");
            }

            // Заполняем метаданные уровня
            if (metadata != null)
            {
                levelData.LevelId = metadata.LevelId;
                levelData.LevelName = metadata.LevelName;
                levelData.Description = metadata.Description;
                levelData.LevelDescriptionKey = metadata.Description;
                Debug.Log($"[DataLoader] Loaded level metadata: Id={metadata.LevelId}, Name={metadata.LevelName}");
            }

            levelData.Questions = questions;
            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[DataLoader] Loaded {questions.Length} questions into {assetPath}");
            return levelData;
        }

        /// <summary>
        /// Counts total answers across all questions in a LevelData.
        /// </summary>
        public static int CountTotalAnswers(LevelData levelData)
        {
            if (levelData == null || levelData.Questions == null)
                return 0;

            int count = 0;
            foreach (var q in levelData.Questions)
            {
                if (q?.Options != null)
                    count += q.Options.Length;
            }
            return count;
        }

        // ── Theory Cards (TheoryCardContainer) ──────────────────────────────

        /// <summary>
        /// Parses a JSON file and loads theory cards into a TheoryCardContainer asset.
        /// Creates or overwrites the asset at the default path.
        /// </summary>
        /// <param name="jsonPath">Absolute path to the JSON file.</param>
        /// <returns>The loaded/updated TheoryCardContainer asset, or null on failure.</returns>
        public static TheoryCardContainer LoadTheoryFromJson(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogError("[DataLoader] JSON path is null or empty.");
                return null;
            }

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[DataLoader] File not found: {jsonPath}");
                return null;
            }

            string jsonContent;
            try
            {
                jsonContent = File.ReadAllText(jsonPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataLoader] Failed to read file: {ex.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                Debug.LogError("[DataLoader] JSON file is empty.");
                return null;
            }

            // Извлекаем LevelId из первого элемента JSON
            string levelId;
            TheoryCardJsonData[] jsonCards;
            try
            {
                levelId = JsonHelper.ExtractLevelId(jsonContent);
                jsonCards = JsonHelper.GetTheoryCardsOnly(jsonContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataLoader] Failed to parse theory JSON: {ex.Message}");
                return null;
            }

            if (jsonCards == null || jsonCards.Length == 0)
            {
                Debug.LogError("[DataLoader] No theory cards parsed from JSON.");
                return null;
            }

            // Используем LevelId для именования файлов, если он есть
            string assetName = !string.IsNullOrEmpty(levelId) ? $"{levelId}_TheoryContainer" : "LoadedTheoryContainer";
            
            // Find or create a TheoryCardContainer asset
             string assetFolder = $"Assets/Resources/SO/Theory/{levelId}";
            string containerPath = $"{assetFolder}/{assetName}.asset";

            if (!AssetDatabase.IsValidFolder(assetFolder))
            {
                EnsureFolderExists(assetFolder);
            }

            var container = AssetDatabase.LoadAssetAtPath<TheoryCardContainer>(containerPath);
            if (container == null)
            {
                container = ScriptableObject.CreateInstance<TheoryCardContainer>();
                AssetDatabase.CreateAsset(container, containerPath);
                Debug.Log($"[DataLoader] Created new TheoryCardContainer at {containerPath}");
            }

            // Устанавливаем LevelId для контейнера
            if (!string.IsNullOrEmpty(levelId))
            {
                container.LevelId = levelId;
                Debug.Log($"[DataLoader] Set LevelId: {levelId}");
            }

            // Create cards folder for individual card assets
            string folderName = !string.IsNullOrEmpty(levelId) ? $"{levelId}_Cards" : $"{container.name}_Cards";
            string cardsFolder = $"{assetFolder}/{folderName}";
            if (!AssetDatabase.IsValidFolder(cardsFolder))
            {
                AssetDatabase.CreateFolder(assetFolder, folderName);
            }

            container.Cards.Clear();

            for (int i = 0; i < jsonCards.Length; i++)
            {
                var jsonCard = jsonCards[i];
                var cardData = ScriptableObject.CreateInstance<TheoryCardData>();
                cardData.LevelId = levelId ?? container.LevelId ?? "";
                cardData.Title = jsonCard.Title ?? "";
                cardData.Description = jsonCard.Text ?? "";
                cardData.Image = null;

                // Используем LevelId в имени файла
                string prefix = !string.IsNullOrEmpty(levelId) ? levelId : "Card";
                string safeName = SanitizeFileName($"{prefix}_Card_{i:D2}_{cardData.Title}");
                string cardPath = Path.Combine(cardsFolder, $"{safeName}.asset");
                cardPath = AssetDatabase.GenerateUniqueAssetPath(cardPath);

                AssetDatabase.CreateAsset(cardData, cardPath);
                container.Cards.Add(cardData);
            }

            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DataLoader] Loaded {container.Cards.Count} theory cards into {containerPath}");
            return container;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            if (name.Length > 50)
                name = name.Substring(0, 50);

            return name;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
