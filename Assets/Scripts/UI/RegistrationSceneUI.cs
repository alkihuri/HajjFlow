using System;
using HajjFlow.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RegistrationSceneUI : MonoBehaviour
{
    
    [SerializeField] GameObject _registrationSceneUI;
    
    [SerializeField] GameObject _loadingScreenUI;
    [SerializeField] Button _registrationButton;
    
    [FormerlySerializedAs("skipButton")] [SerializeField] Button _skipButton;
    
    
    [SerializeField] TMP_InputField _usernameInput;
    [SerializeField] TMP_InputField _groupInput;

    private void Awake()
    {
        if (_registrationButton != null)
        {
            _registrationButton.onClick.AddListener(Register);
        }

        if (_skipButton != null)
        {
            _skipButton.onClick.AddListener(SkipRegistration);
        }
        
    }

    private void SkipRegistration()
    {
        HideRegistrationScreen();
        Debug.Log("Skipping registration");
    }

    private async void Register()
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
        
        
        await GameManager.Instance.GetService<RegistrationService>().RegisterUserAsync(username, group,HideRegistrationScreen);
        
        PlayLoadingScreen();
 
    }

    private void PlayLoadingScreen()
    {
        // turn off btns 
        _registrationButton.interactable = false;
        _skipButton.interactable = true;
          
        _loadingScreenUI.SetActive(true); 
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
