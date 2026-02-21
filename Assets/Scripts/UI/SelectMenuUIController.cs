using UnityEngine;
using UnityEngine.UI;

public class SelectMenuUIController : MonoBehaviour
{
   [SerializeField] private Button [] _buttons;

    private void Start()
    {
        // Wire up button listeners
        foreach (var button in _buttons)
        {
            button.onClick.AddListener(() => OnButtonClicked(button));
        }
    }

    private void OnDestroy()
    {
        // Unwire button listeners
        foreach (var button in _buttons)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    private void OnButtonClicked(Button clickedButton)
    {
        // Handle button click logic here
        Debug.Log($"Button '{clickedButton.name}' was clicked.");
    }
}
