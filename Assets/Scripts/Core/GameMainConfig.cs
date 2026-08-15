using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HajjFlow.Data;
using Core.Theory;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Core
{
    /// <summary>
    /// Главный конфиг игры, который управляет всеми уровнями, их теорией и квизом.
    /// Позволяет импортировать уровни и карточки теории из JSON файлов.
    /// </summary>
    [CreateAssetMenu(fileName = "GameMainConfig", menuName = "Manasik/Game Main Config")]
    public class GameMainConfig : ScriptableObject
    {
        [System.Serializable]
        public class LevelEntry
        {
            [Tooltip("Уникальный идентификатор уровня (должен совпадать в теории и квизе)")]
            public string LevelId;

            [Tooltip("Данные квиза (вопросы, награды и т.д.)")]
            public LevelData LevelData;

            [Tooltip("Карточки теории для этого уровня")]
            public TheoryCardContainer TheoryContainer;
        }

        [Header("Game Levels")]
        [Tooltip("Список всех уровней с привязанными данными теории и квиза")]
        public List<LevelEntry> Levels = new List<LevelEntry>();

        [Header("Remote Content")]
        [Tooltip("Использовать удалённый контент из Google Sheets вместо статических данных")]
        public bool UseRemoteContent = true;

        [Tooltip("Таймаут ожидания загрузки удалённого контента (секунды) перед fallback на статику")]
        public float RemoteContentTimeout = 10f;

        [Header("Import Settings")]
        [Tooltip("Путь к папке с JSON файлами квизов (относительно Assets)")]
        public string QuizJsonFolderPath = "Data/Quiz";

        [Tooltip("Путь к папке с JSON файлами теории (относительно Assets)")]
        public string TheoryJsonFolderPath = "Data/Theory";

        [Tooltip("Автоматически создавать TheoryCardContainer для каждого импортированного квиза")]
        public bool AutoCreateTheoryContainers = true;

        /// <summary>
        /// Возвращает LevelEntry по LevelId
        /// </summary>
        public LevelEntry GetLevelEntry(string levelId)
        {
            return Levels.FirstOrDefault(l => l.LevelId == levelId);
        }

        /// <summary>
        /// Возвращает LevelData по LevelId
        /// </summary>
        public LevelData GetLevelData(string levelId)
        {
            var entry = GetLevelEntry(levelId);
            return entry?.LevelData;
        }

        /// <summary>
        /// Возвращает TheoryCardContainer по LevelId
        /// </summary>
        public TheoryCardContainer GetTheoryContainer(string levelId)
        {
            var entry = GetLevelEntry(levelId);
            return entry?.TheoryContainer;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Импортирует все JSON файлы квизов из указанной папки
        /// </summary>
        [ContextMenu("Import All Quiz Files from Folder")]
        public void ImportAllQuizFilesFromFolder()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Quiz JSON Folder", "Assets", "");
            
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.Log("[GameMainConfig] Import cancelled");
                return;
            }

            ImportQuizzesFromFolder(folderPath);
        }

        /// <summary>
        /// Импортирует все JSON файлы теории из указанной папки
        /// </summary>
        [ContextMenu("Import All Theory Files from Folder")]
        public void ImportAllTheoryFilesFromFolder()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Theory JSON Folder", "Assets", "");
            
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.Log("[GameMainConfig] Import cancelled");
                return;
            }

            ImportTheoriesFromFolder(folderPath);
        }
 
        
        /// <summary>
        /// Импортирует все JSON файлы (квизы и теория) из двух папок
        /// </summary>
        [ContextMenu("Import All Levels (Theory + Quiz)")]
        public void ImportAllLevels()
        {
            string quizPath = EditorUtility.OpenFolderPanel("Select Quiz JSON Folder", "Assets", "");
            
            if (string.IsNullOrEmpty(quizPath))
            {
                Debug.Log("[GameMainConfig] Import cancelled");
                return;
            }

            string theoryPath = EditorUtility.OpenFolderPanel("Select Theory JSON Folder", "Assets", "");
            
            if (string.IsNullOrEmpty(theoryPath))
            {
                Debug.Log("[GameMainConfig] Import cancelled");
                return;
            }

            Levels.Clear();

            ImportQuizzesFromFolder(quizPath);
            ImportTheoriesFromFolder(theoryPath);
            
            // Связываем теорию с квизом по LevelId
            LinkTheoryToQuiz();

            Debug.Log($"[GameMainConfig] Successfully imported {Levels.Count} complete level entries!");
        }

        /// <summary>
        /// Связывает импортированные теория и квиз по LevelId
        /// </summary>
        [ContextMenu("Link Theory to Quiz by LevelId")]
        public void LinkTheoryToQuiz()
        {
            int linkedCount = 0;

            foreach (var entry in Levels)
            {
                if (string.IsNullOrEmpty(entry.LevelId))
                {
                    Debug.LogWarning($"[GameMainConfig] Entry has no LevelId assigned!");
                    continue;
                }

                // Если TheoryContainer уже назначен, пропускаем
                if (entry.TheoryContainer != null)
                    continue;

                // Ищем TheoryCardContainer с совпадающим LevelId
                var allTheoryContainers = FindAllTheoryContainers();
                var matchingContainer = allTheoryContainers.FirstOrDefault(t => t.LevelId == entry.LevelId);

                if (matchingContainer != null)
                {
                    entry.TheoryContainer = matchingContainer;
                    linkedCount++;
                    Debug.Log($"[GameMainConfig] Linked {entry.LevelId} to theory container");
                }
                else
                {
                    Debug.LogWarning($"[GameMainConfig] No theory container found for LevelId: {entry.LevelId}");
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"[GameMainConfig] Successfully linked {linkedCount} levels!");
        }

        /// <summary>
        /// Очищает список уровней
        /// </summary>
        [ContextMenu("Clear All Levels")]
        public void ClearAllLevels()
        {
            if (!EditorUtility.DisplayDialog("Clear Levels", 
                "This will remove all level entries (but not delete the assets). Continue?", 
                "Yes", "No"))
            {
                return;
            }

            Levels.Clear();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log("[GameMainConfig] Cleared all levels");
        }

        /// <summary>
        /// Импортирует квизы из указанной папки
        /// </summary>
        private void ImportQuizzesFromFolder(string folderPath)
        {
            // Получаем все JSON файлы в папке
            string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

            if (jsonFiles.Length == 0)
            {
                Debug.LogWarning($"[GameMainConfig] No JSON files found in: {folderPath}");
                return;
            }

            Debug.Log($"[GameMainConfig] Found {jsonFiles.Length} quiz JSON files");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var metadata = LevelMetadata.ExtractFromJson(jsonContent);

                    if (metadata == null)
                    {
                        Debug.LogWarning($"[GameMainConfig] Could not extract metadata from: {Path.GetFileName(filePath)}");
                        continue;
                    }

                    // Проверяем, есть ли уже уровень с таким LevelId
                    var existingEntry = Levels.FirstOrDefault(l => l.LevelId == metadata.LevelId);
                    if (existingEntry == null)
                    {
                        existingEntry = new LevelEntry { LevelId = metadata.LevelId };
                        Levels.Add(existingEntry);
                    }

                    // Создаём или находим LevelData
                    LevelData levelData = null;
                    
                    if (existingEntry.LevelData != null)
                    {
                        levelData = existingEntry.LevelData;
                    }
                    else
                    {
                        // Создаём новый LevelData
                        levelData = CreateInstance<LevelData>();
                        levelData.name = metadata.LevelId;
                    }

                    // Заполняем данные из JSON
                    levelData.LevelId = metadata.LevelId;
                    levelData.LevelName = metadata.LevelName;
                    levelData.Description = metadata.Description;
                    levelData.LevelDescriptionKey = metadata.Description;
                    levelData.Questions = QuizQuestion.FromJsonArray(jsonContent);

                    // Сохраняем LevelData
                    string assetPath = GetOrCreateLevelDataPath(metadata.LevelId);
                    AssetDatabase.CreateAsset(levelData, assetPath);

                    existingEntry.LevelData = levelData;

                    Debug.Log($"[GameMainConfig] Imported quiz: {metadata.LevelId} with {levelData.Questions.Length} questions");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameMainConfig] Error importing {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Импортирует теорию из указанной папки
        /// </summary>
        private void ImportTheoriesFromFolder(string folderPath)
        {
            string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

            if (jsonFiles.Length == 0)
            {
                Debug.LogWarning($"[GameMainConfig] No JSON files found in: {folderPath}");
                return;
            }

            Debug.Log($"[GameMainConfig] Found {jsonFiles.Length} theory JSON files");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    string levelId = JsonHelper.ExtractLevelId(jsonContent);

                    if (string.IsNullOrEmpty(levelId))
                    {
                        Debug.LogWarning($"[GameMainConfig] Could not extract LevelId from: {Path.GetFileName(filePath)}");
                        continue;
                    }

                    // Проверяем, есть ли уже уровень с таким LevelId
                    var existingEntry = Levels.FirstOrDefault(l => l.LevelId == levelId);
                    if (existingEntry == null)
                    {
                        existingEntry = new LevelEntry { LevelId = levelId };
                        Levels.Add(existingEntry);
                    }

                    // Создаём TheoryCardContainer
                    TheoryCardContainer container = CreateInstance<TheoryCardContainer>();
                    container.name = $"{levelId}_TheoryCards";
                    container.LevelId = levelId;

                    // Заполняем карточки теории
                    var theoryCards = JsonHelper.GetTheoryCardsOnly(jsonContent);
                    if (theoryCards != null && theoryCards.Length > 0)
                    {
                        container.Cards.Clear();
                        for (int i = 0; i < theoryCards.Length; i++)
                        {
                            var jsonCard = theoryCards[i];
                            var cardData = CreateInstance<TheoryCardData>();
                            cardData.name = $"{levelId}_Card_{i:D2}";
                            cardData.LevelId = levelId;
                            cardData.Title = jsonCard.Title;
                            cardData.Description = jsonCard.Text;
                            cardData.Image = null;

                            // Сохраняем карточку в папке конкретного уровня
                            string levelTheoryFolderPath = GetOrCreateLevelTheoryFolder(levelId);
                            string cardPath = Path.Combine(levelTheoryFolderPath, $"{cardData.name}.asset");
                            cardPath = AssetDatabase.GenerateUniqueAssetPath(cardPath);

                            AssetDatabase.CreateAsset(cardData, cardPath);
                            container.Cards.Add(cardData);
                        }
                    }

                    // Сохраняем TheoryCardContainer в папку конкретного уровня
                    string levelTheoryPath = GetOrCreateLevelTheoryFolder(levelId);
                    string theoryAssetPath = Path.Combine(levelTheoryPath, $"{levelId}_TheoryContainer.asset");
                    theoryAssetPath = AssetDatabase.GenerateUniqueAssetPath(theoryAssetPath);
                    AssetDatabase.CreateAsset(container, theoryAssetPath);

                    existingEntry.TheoryContainer = container;

                    Debug.Log($"[GameMainConfig] Imported theory: {levelId} with {container.Cards.Count} cards");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameMainConfig] Error importing {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Получает путь до LevelData или создаёт папку если её нет
        /// </summary>
        private string GetOrCreateLevelDataPath(string levelId)
        {
            string basePath = AssetDatabase.GetAssetPath(this);
            string directory = Path.GetDirectoryName(basePath);
            string levelFolder = Path.Combine(directory, "Levels");

            if (!AssetDatabase.IsValidFolder(levelFolder))
            {
                AssetDatabase.CreateFolder(directory, "Levels");
            }

            return Path.Combine(levelFolder, $"{levelId}_LevelData.asset");
        }

        /// <summary>
        /// Получает путь до папки уровня в Theory или создаёт её если её нет
        /// Структура: Theory/{LevelId}/
        /// </summary>
        private string GetOrCreateLevelTheoryFolder(string levelId)
        {
            string basePath = AssetDatabase.GetAssetPath(this);
            string directory = Path.GetDirectoryName(basePath);
            string theoryFolder = Path.Combine(directory, "Theory");
            string levelFolder = Path.Combine(theoryFolder, levelId);

            // Создаём папку Theory если её нет
            if (!AssetDatabase.IsValidFolder(theoryFolder))
            {
                AssetDatabase.CreateFolder(directory, "Theory");
            }

            // Создаём папку для конкретного уровня если её нет
            if (!AssetDatabase.IsValidFolder(levelFolder))
            {
                AssetDatabase.CreateFolder(theoryFolder, levelId);
            }

            return levelFolder;
        }

        /// <summary>
        /// Находит все TheoryCardContainer ассеты в проекте
        /// </summary>
        private List<TheoryCardContainer> FindAllTheoryContainers()
        {
            var containers = new List<TheoryCardContainer>();
            string[] guids = AssetDatabase.FindAssets("t:TheoryCardContainer");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var container = AssetDatabase.LoadAssetAtPath<TheoryCardContainer>(path);
                if (container != null)
                {
                    containers.Add(container);
                }
            }

            return containers;
        }

#endif
    }

    /// <summary>
    /// Расширенная версия LevelMetadata с методом извлечения из JSON
    /// </summary>
    [System.Serializable]
    public class LevelMetadata
    {
        public string LevelId = "";
        public string LevelName = "";
        public string Description = "";
        public string imagePath = null;

        /// <summary>
        /// Извлекает метаданные из JSON строки (ищет первый элемент с LevelId)
        /// </summary>
        public static LevelMetadata ExtractFromJson(string json)
        {
            string wrappedJson = "{\"Items\":" + json + "}";
            LevelMetadataWrapper wrapper = JsonUtility.FromJson<LevelMetadataWrapper>(wrappedJson);

            if (wrapper?.Items == null || wrapper.Items.Length == 0)
                return null;

            // Ищем первый элемент с LevelId (метаданные)
            foreach (var item in wrapper.Items)
            {
                if (!string.IsNullOrEmpty(item.LevelId))
                {
                    return item;
                }
            }

            return null;
        }
    }

    [System.Serializable]
    public class LevelMetadataWrapper
    {
        public LevelMetadata[] Items;
    }
}