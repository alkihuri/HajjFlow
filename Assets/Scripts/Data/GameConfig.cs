using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HajjFlow.Data
{
    /// <summary>
    /// Single ScriptableObject that holds all game configuration: levels, quiz data, and settings.
    /// All levels are stored in one place — no separate ScriptableObject per level.
    /// Levels can be added/removed dynamically via the Inspector or loaded from JSON.
    ///
    /// Future: Will be populated from Google Sheets where each sheet = one level.
    /// Create via: Assets → Create → Manasik → Game Config
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Manasik/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Levels")]
        /// <summary>All game levels in progression order. Add or remove freely.</summary>
        [SerializeField] private List<LevelData> _levels = new List<LevelData>();

        [Header("Default Settings")]
        /// <summary>Default pass threshold for all levels (can be overridden per level).</summary>
        [Range(0, 100)]
        public int DefaultPassThreshold = 60;

        /// <summary>Default bonus gems for completing a level.</summary>
        public int DefaultCompletionBonusGems = 20;

        /// <summary>Default number of theory blocks before quiz.</summary>
        public int DefaultTheoryBlockCount = 1;

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Returns a read-only list of all levels in order.</summary>
        public IReadOnlyList<LevelData> Levels => _levels;

        /// <summary>Total number of levels configured.</summary>
        public int LevelCount => _levels.Count;

        /// <summary>Returns the level data for a given level ID, or null if not found.</summary>
        public LevelData GetLevel(string levelId)
        {
            return _levels.Find(l => l.LevelId == levelId);
        }

        /// <summary>Returns the level at the given index, or null if out of range.</summary>
        public LevelData GetLevelByIndex(int index)
        {
            if (index < 0 || index >= _levels.Count) return null;
            return _levels[index];
        }

        /// <summary>Returns the index of a level by its ID, or -1 if not found.</summary>
        public int GetLevelIndex(string levelId)
        {
            return _levels.FindIndex(l => l.LevelId == levelId);
        }

        /// <summary>Returns all level IDs in order.</summary>
        public List<string> GetAllLevelIds()
        {
            return _levels.Select(l => l.LevelId).ToList();
        }

        /// <summary>Returns the next level after the given ID, or null if last.</summary>
        public LevelData GetNextLevel(string currentLevelId)
        {
            int index = GetLevelIndex(currentLevelId);
            if (index < 0 || index >= _levels.Count - 1) return null;
            return _levels[index + 1];
        }

        /// <summary>Returns the previous level before the given ID, or null if first.</summary>
        public LevelData GetPreviousLevel(string currentLevelId)
        {
            int index = GetLevelIndex(currentLevelId);
            if (index <= 0) return null;
            return _levels[index - 1];
        }

        /// <summary>Returns true if the given ID corresponds to a configured level.</summary>
        public bool HasLevel(string levelId)
        {
            return _levels.Any(l => l.LevelId == levelId);
        }

        // ── JSON Import (Editor only) ───────────────────────────────────────────

#if UNITY_EDITOR
        /// <summary>
        /// Imports a level from a JSON quiz file. Creates a new LevelData entry
        /// or updates an existing one with the same LevelId.
        /// </summary>
        [ContextMenu("Import Level from JSON")]
        public void ImportLevelFromJson()
        {
            string path = EditorUtility.OpenFilePanel("Select JSON Quiz File", "Assets/Data/QuizData", "json");

            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("[GameConfig] File selection cancelled");
                return;
            }

            try
            {
                string jsonContent = System.IO.File.ReadAllText(path);

                // Extract metadata
                var metadata = QuizQuestion.ExtractLevelMetadata(jsonContent);
                if (metadata == null)
                {
                    Debug.LogWarning("[GameConfig] No level metadata found in JSON file.");
                    return;
                }

                // Parse questions
                var questions = QuizQuestion.FromJsonArray(jsonContent);

                // Find or create level entry
                var existingLevel = _levels.Find(l => l.LevelId == metadata.LevelId);
                if (existingLevel != null)
                {
                    // Update existing
                    existingLevel.LevelName = metadata.LevelName;
                    existingLevel.Description = metadata.Description;
                    existingLevel.LevelDescriptionKey = metadata.Description;
                    existingLevel.Questions = questions;
                    Debug.Log($"[GameConfig] Updated level '{metadata.LevelId}' with {questions?.Length ?? 0} questions");
                }
                else
                {
                    // Create new
                    var newLevel = new LevelData
                    {
                        LevelId = metadata.LevelId,
                        LevelName = metadata.LevelName,
                        Description = metadata.Description,
                        LevelDescriptionKey = metadata.Description,
                        Questions = questions,
                        CompletionBonusGems = DefaultCompletionBonusGems,
                        PassThreshold = DefaultPassThreshold,
                        TheoryBlockCount = DefaultTheoryBlockCount,
                        SortOrder = _levels.Count
                    };
                    _levels.Add(newLevel);
                    Debug.Log($"[GameConfig] Added new level '{metadata.LevelId}' with {questions?.Length ?? 0} questions");
                }

                EditorUtility.SetDirty(this);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameConfig] Failed to import: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [ContextMenu("Import All JSON Files from QuizData Folder")]
        public void ImportAllJsonFiles()
        {
            string folder = EditorUtility.OpenFolderPanel("Select QuizData Folder", "Assets/Data/QuizData", "");
            if (string.IsNullOrEmpty(folder)) return;

            var jsonFiles = System.IO.Directory.GetFiles(folder, "*.json")
                .Where(f => !f.Contains("theory"))
                .OrderBy(f => f)
                .ToArray();

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    string jsonContent = System.IO.File.ReadAllText(filePath);
                    var metadata = QuizQuestion.ExtractLevelMetadata(jsonContent);
                    if (metadata == null) continue;

                    var questions = QuizQuestion.FromJsonArray(jsonContent);

                    var existingLevel = _levels.Find(l => l.LevelId == metadata.LevelId);
                    if (existingLevel != null)
                    {
                        existingLevel.LevelName = metadata.LevelName;
                        existingLevel.Description = metadata.Description;
                        existingLevel.LevelDescriptionKey = metadata.Description;
                        existingLevel.Questions = questions;
                    }
                    else
                    {
                        _levels.Add(new LevelData
                        {
                            LevelId = metadata.LevelId,
                            LevelName = metadata.LevelName,
                            Description = metadata.Description,
                            LevelDescriptionKey = metadata.Description,
                            Questions = questions,
                            CompletionBonusGems = DefaultCompletionBonusGems,
                            PassThreshold = DefaultPassThreshold,
                            TheoryBlockCount = DefaultTheoryBlockCount,
                            SortOrder = _levels.Count
                        });
                    }

                    Debug.Log($"[GameConfig] Imported: {metadata.LevelId} ({questions?.Length ?? 0} questions)");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[GameConfig] Failed to import {filePath}: {ex.Message}");
                }
            }

            EditorUtility.SetDirty(this);
            Debug.Log($"[GameConfig] Import complete. Total levels: {_levels.Count}");
        }
#endif
    }
}
