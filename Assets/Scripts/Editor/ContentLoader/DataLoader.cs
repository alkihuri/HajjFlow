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
            try
            {
                questions = QuizQuestion.FromJsonArray(jsonContent);
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

            // Find or create a LevelData asset
            const string assetFolder = "Assets/ScriptableObjects/Levels";
            const string assetPath = "Assets/ScriptableObjects/Levels/LoadedLevelData.asset";

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

            TheoryCardJsonData[] jsonCards;
            try
            {
                jsonCards = JsonHelper.FromJson<TheoryCardJsonData>(jsonContent);
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

            // Find or create a TheoryCardContainer asset
            const string assetFolder = "Assets/ScriptableObjects/Theory";
            const string containerPath = "Assets/ScriptableObjects/Theory/LoadedTheoryContainer.asset";

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

            // Create cards folder for individual card assets
            string cardsFolder = $"Assets/ScriptableObjects/Theory/{container.name}_Cards";
            if (!AssetDatabase.IsValidFolder(cardsFolder))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/Theory", $"{container.name}_Cards");
            }

            container.Cards.Clear();

            for (int i = 0; i < jsonCards.Length; i++)
            {
                var jsonCard = jsonCards[i];
                var cardData = ScriptableObject.CreateInstance<TheoryCardData>();
                cardData.LevelId = container.LevelId;
                cardData.Title = jsonCard.Title ?? "";
                cardData.Description = jsonCard.Text ?? "";
                cardData.Image = null;

                string safeName = SanitizeFileName($"Card_{i:D2}_{cardData.Title}");
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
