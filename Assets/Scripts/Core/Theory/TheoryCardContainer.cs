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
            // Parse JSON array
            TheoryCardJsonData[] jsonCards = JsonHelper.FromJson<TheoryCardJsonData>(jsonContent);
            
            if (jsonCards == null || jsonCards.Length == 0)
            {
                Debug.LogError("[TheoryCardContainer] Failed to parse JSON or empty array!");
                return;
            }

            // Get directory for saving ScriptableObjects
            string thisAssetPath = AssetDatabase.GetAssetPath(this);
            string directory = Path.GetDirectoryName(thisAssetPath);
            string cardsFolder = Path.Combine(directory, $"{name}_Cards");
            
            // Create folder if needed
            if (!AssetDatabase.IsValidFolder(cardsFolder))
            {
                AssetDatabase.CreateFolder(directory, $"{name}_Cards");
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
                
                // Save as asset
                string cardName = SanitizeFileName($"Card_{i:D2}_{jsonCard.Title}");
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
        public string Title;
        public string Text;
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

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
}

