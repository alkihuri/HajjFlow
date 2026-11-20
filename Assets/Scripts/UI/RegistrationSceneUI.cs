using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] TextMeshProUGUI _loadingText;
    [SerializeField] TMP_Dropdown _groupDropdown;
    
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

        if (_groupDropdown != null)
        {
            // set to _groupInput text when dropdown value changes
            _groupDropdown.onValueChanged.AddListener((index) =>
            {
                if (_groupDropdown.options.Count > index)
                {
                    _groupInput.text = _groupDropdown.options[index].text;
                }

            });
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

        if (username == "")
        {
            username = _groupDropdown.options[_groupDropdown.value].text;
        }
        
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(group))
        {
            Debug.LogWarning("Username or group is empty. Please fill in both fields.");
            return;
        }

        // Save the username and group to PlayerPrefs
        
        Debug.Log($"Registered with Username: {username}, Group: {group}");
        
        PlayLoadingScreen();
        await GameManager.Instance.GetService<RegistrationService>().RegisterUserAsync(username, group,HideRegistrationScreen);
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.SetString("Group", group);
        PlayerPrefs.Save();

    }

    private void PlayLoadingScreen()
    {
        // turn off btns 
        _registrationButton.interactable = false;
        _skipButton.interactable = true;
          
        _loadingScreenUI.SetActive(true); 
        
        // add animated 3 dots to text 
        // save original text but add  0 1 2 3 dots to it
         StartCoroutine(LoadingAnimation());
    }

    private IEnumerator LoadingAnimation()
    { 
        
        string originalText = _loadingText.text;
        int dotCount = 0;

        while (_loadingScreenUI.activeSelf)
        {
            dotCount = (dotCount + 1) % 4; // Cycle through 0, 1, 2, 3
            _loadingText.text = originalText + new string('.', dotCount);
            yield return new WaitForSeconds(0.5f); // Update every half second
        }

        // Reset text when loading screen is hidden
        _loadingText.text = originalText;
    }


    public void ShowRegistrationScreen()
    { 
        _registrationSceneUI.SetActive(true);
    }


    public void HideRegistrationScreen()
    {
        _registrationSceneUI.SetActive(false);
        StopCoroutine(LoadingAnimation());
    }

    public void UpdateHeadsOfGroup(List<string> sheetConfigHeadsOfGroup)
    { 
        // if there is no head of froup hide the dropdown and show the input field
        if (sheetConfigHeadsOfGroup == null || sheetConfigHeadsOfGroup.Count == 0)
        {
            _groupDropdown.gameObject.SetActive(false);
            _groupInput.gameObject.SetActive(true);
            return;
        }  
        else
        {
            _groupDropdown.gameObject.SetActive(true);
            _groupInput.gameObject.SetActive(false);
        }
        _groupDropdown.options.Clear();
        foreach (var headsOfGroup in sheetConfigHeadsOfGroup)
        {
            _groupDropdown.options.Add(new TMP_Dropdown.OptionData(headsOfGroup));
        }
         
    }
}
