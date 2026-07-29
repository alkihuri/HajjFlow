using System;
using HajjFlow.Core;
using HajjFlow.Data;
using HajjFlow.Services;
using UnityEngine;

namespace Core.Theory
{
    [CreateAssetMenu(menuName = "Theory/CardData")]
    public class TheoryCardData : ScriptableObject
    {
        public string LevelId;
        public string Title;
        public string Description;
        public Sprite Image;

        [Header("PREVIEW DATA")] public Preview preview;

        
        /// <summary>
        ///  HARD-CODED: This method is called by Unity when the ScriptableObject is modified in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            try
            {
                // get service
                var localizationService = GameManager.Instance?.GetService<LocalizationService>();
                if(localizationService==null)
                    return;
                
                if(preview == null)
                    preview = new Preview(Description, Title, localizationService.CurrentLanguage);
                localizationService.ChangeLanguage(preview.Lang);
                var localizedTitle = localizationService?.GetText(Title) ?? Title;
                var localizedDescription = localizationService?.GetText(Description) ?? Description;

                OnPreviewReady(localizedTitle, localizedDescription, localizationService.CurrentLanguage);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }


        public void OnPreviewReady(string localizedTitle, string localizedDescription, Language lang)
        {
            preview = new Preview(localizedTitle, localizedDescription, lang);
        }
    }


    [System.Serializable]
    public class Preview
    {
        public Preview(string description, string title, Language lang)
        {
            Title = title;
            Description = description;
            Lang = lang;
        }

        public Language Lang;
        public string Title;
        public string Description;
    }
}