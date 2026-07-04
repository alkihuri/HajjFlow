using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Core.Theory
{
    [CreateAssetMenu(menuName = "Theory/CardContainer")]
    public class TheoryCardContainer : ScriptableObject
    {
        [Header("Level Info")]
        public string LevelId;
        
        [Header("Cards")]
        public List<TheoryCardData> Cards = new List<TheoryCardData>();

#if UNITY_EDITOR
        [ContextMenu("Import Cards from JSON")]
        private void ImportCardsFromJson()
        {
            string path = EditorUtility.OpenFilePanel("Select Theory JSON file", "Assets/Data", "json");
            
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("[TheoryCardContainer] Import cancelled");
                return;
            }

            string jsonContent = File.ReadAllText(path);
            ImportFromJson(jsonContent, path);
        }

        private void ImportFromJson(string jsonContent, string sourcePath)
        {
            // Извлекаем LevelId из первого элемента JSON
            string extractedLevelId = JsonHelper.ExtractLevelId(jsonContent);
            if (!string.IsNullOrEmpty(extractedLevelId))
            {
                LevelId = extractedLevelId;
                Debug.Log($"[TheoryCardContainer] Extracted LevelId: {LevelId}");
            }
            
            // Получаем только карточки теории (без метаданных)
            TheoryCardJsonData[] jsonCards = JsonHelper.GetTheoryCardsOnly(jsonContent);
            
            if (jsonCards == null || jsonCards.Length == 0)
            {
                Debug.LogError("[TheoryCardContainer] Failed to parse JSON or no theory cards found!");
                return;
            }

            // Get directory for saving ScriptableObjects
            string thisAssetPath = AssetDatabase.GetAssetPath(this);
            string directory = Path.GetDirectoryName(thisAssetPath);
            
            // Используем LevelId для названия папки
            string folderName = !string.IsNullOrEmpty(LevelId) ? $"{LevelId}_Cards" : $"{name}_Cards";
            string cardsFolder = Path.Combine(directory, folderName);
            
            // Create folder if needed
            if (!AssetDatabase.IsValidFolder(cardsFolder))
            {
                AssetDatabase.CreateFolder(directory, folderName);
            }

            // Clear existing cards
            Cards.Clear();

            // Create card for each JSON entry
            for (int i = 0; i < jsonCards.Length; i++)
            {
                var jsonCard = jsonCards[i];
                
                // Create new TheoryCardData
                var cardData = ScriptableObject.CreateInstance<TheoryCardData>();
                cardData.LevelId = LevelId;
                cardData.Title = jsonCard.Title;
                cardData.Description = jsonCard.Text;
                cardData.Image = null; // Can be assigned manually later
                
                // Save as asset - используем LevelId в имени файла
                string cardName = SanitizeFileName($"{LevelId}_Card_{i:D2}_{jsonCard.Title}");
                string cardPath = Path.Combine(cardsFolder, $"{cardName}.asset");
                cardPath = AssetDatabase.GenerateUniqueAssetPath(cardPath);
                
                AssetDatabase.CreateAsset(cardData, cardPath);
                
                // Add to list
                Cards.Add(cardData);
                
                Debug.Log($"[TheoryCardContainer] Created card: {cardData.Title}");
            }

            // Mark this asset as dirty and save
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TheoryCardContainer] Imported {Cards.Count} cards from JSON");
        }

        private string SanitizeFileName(string name)
        {
            // Remove invalid characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            
            // Limit length
            if (name.Length > 50)
            {
                name = name.Substring(0, 50);
            }
            
            return name;
        }

        [ContextMenu("Clear All Cards")]
        private void ClearAllCards()
        {
            if (!EditorUtility.DisplayDialog("Clear Cards", 
                "This will remove all card references (but not delete the assets). Continue?", 
                "Yes", "No"))
            {
                return;
            }
            
            Cards.Clear();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[TheoryCardContainer] Cleared all cards");
        }
#endif
    }

    /// <summary>
    /// Helper class for JSON deserialization
    /// </summary>
    [System.Serializable]
    public class TheoryCardJsonData
    {
        public string Id;       // Для первого элемента с идентификатором уровня
        public string LevelId;  // Альтернативное поле для LevelId
        public string Title;
        public string Text;
        
        /// <summary>
        /// Проверяет, является ли элемент метаданными уровня (содержит Id но не Title)
        /// </summary>
        public bool IsLevelMetadata => !string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(Title);
        
        /// <summary>
        /// Проверяет, является ли элемент карточкой теории
        /// </summary>
        public bool IsTheoryCard => !string.IsNullOrEmpty(Title);
    }

    /// <summary>
    /// Unity's JsonUtility doesn't support arrays at root level,
    /// so we need this helper
    /// </summary>
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            // Wrap array in object for Unity's JsonUtility
            string wrappedJson = "{\"Items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
            return wrapper?.Items;
        }
        
        /// <summary>
        /// Извлекает LevelId из первого элемента JSON массива (поле Id)
        /// </summary>
        public static string ExtractLevelId(string json)
        {
            var items = FromJson<TheoryCardJsonData>(json);
            if (items != null && items.Length > 0)
            {
                // Ищем элемент с Id но без Title (метаданные уровня)
                foreach (var item in items)
                {
                    if (item.IsLevelMetadata)
                    {
                        return item.Id;
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// Возвращает только карточки теории (фильтрует метаданные)
        /// </summary>
        public static TheoryCardJsonData[] GetTheoryCardsOnly(string json)
        {
            var items = FromJson<TheoryCardJsonData>(json);
            if (items == null) return null;
            
            var cards = new System.Collections.Generic.List<TheoryCardJsonData>();
            foreach (var item in items)
            {
                if (item.IsTheoryCard)
                {
                    cards.Add(item);
                }
            }
            return cards.ToArray();
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
}

