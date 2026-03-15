using System;
using HajjFlow.Core;
using HajjFlow.Services;
using UnityEngine;

public class LocalizationServiceLoader : MonoBehaviour
{
   

    public bool IsLoaded = false;
    
    [ContextMenu("Load Localization from google sheet")]
    public void LoadLocalization()
    {
        Action <bool>loadedAction = (loaded) =>
        {
            IsLoaded = loaded;
            Debug.Log("[LocalizationServiceLoader] Localization loaded successfully.");
        };
        
        
        var service = GameManager.Instance?.GetService<LocalizationService>();
        if (service != null)
        { 
            StartCoroutine(service.LoadFromGoogleSheets(loadedAction));
            Debug.Log("[LocalizationServiceLoader] Localization loaded from Google Sheet.");
        }
        else
        {
            Debug.LogWarning("[LocalizationServiceLoader] LocalizationService not found.");
        }
    }
}
