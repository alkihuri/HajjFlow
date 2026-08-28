using System;
using HajjFlow.Core;
using UnityEngine;
using UnityEngine.UI;

public class RegistrationSceneUI : MonoBehaviour
{
    
    [SerializeField] GameObject _registrationSceneUI;
    
    
    [SerializeField] Button _registrationButton;
    
    [SerializeField] Button skipButton;
    
    
    [SerializeField] InputField _usernameInput;
    [SerializeField] InputField _groupInput;

    private void Awake()
    {
        if (_registrationButton != null)
        {
            _registrationButton.onClick.AddListener(Register);
        }
        
    }

    private void Register()
    {
        string username = _usernameInput.text;
        string group = _groupInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(group))
        {
            Debug.LogWarning("Username or group is empty. Please fill in both fields.");
            return;
        }

        // Save the username and group to PlayerPrefs
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.SetString("Group", group);
        PlayerPrefs.Save();

        Debug.Log($"Registered with Username: {username}, Group: {group}");
        
        
        GameManager.Instance.GetService<RegistrationService>().RegisterUser(username, group);

        // Hide the registration screen after successful registration
        HideRegistrationScreen();
    }


    public void ShowRegistrationScreen()
    { 
        _registrationSceneUI.SetActive(true);
    }


    public void HideRegistrationScreen()
    {
        _registrationSceneUI.SetActive(false);
    }
}
