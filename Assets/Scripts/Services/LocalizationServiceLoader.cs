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
    
    
    [ContextMenu("Log list of game text controllers")]
    public void LogGameTextControllers()
    {
        var service = GameManager.Instance?.GetService<LocalizationService>();
        if (service != null)
        {
            var controllers = service.GetRegisteredControllers();
            Debug.Log($"[LocalizationServiceLoader] Registered GameTextControllers ({controllers.Count}):");
            foreach (var controller in controllers)
            {
                Debug.Log($"-  {controller.text} on{controller.gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning("[LocalizationServiceLoader] LocalizationService not found.");
        }
    }
}
