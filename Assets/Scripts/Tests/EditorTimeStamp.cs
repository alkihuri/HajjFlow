using System;
using TMPro;
using UnityEngine;

public class EditorTimeStamp : MonoBehaviour
{
    
    [SerializeField] private string _version;
    [SerializeField] private TextMeshProUGUI _versionText;
   

    private void OnValidate()
    {
        if(!Application.isEditor)
            return;
        
        
         _versionText ??= GetComponent<TextMeshProUGUI>(); 
        _versionText.text = $"Version: {_version} | Build Time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
    }
 
}
