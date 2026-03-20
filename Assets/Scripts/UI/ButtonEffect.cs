using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using HajjFlow.Core;

public class ButtonEffect : MonoBehaviour
{
    [SerializeField] private Button _button;

    private AudioService _audioService;
    
    private void Awake()
    {
        _button??= GetComponent<Button>();
        if(_button == null)
        {
            Debug.LogError("[ButtonEffect] No Button component found on the GameObject. Disabling ButtonEffect.");
            enabled = false;
            return;
        }
        _button.onClick.AddListener(Effect); 
        _audioService = GameManager.Instance?.GetService<AudioService>();
    }

    private void OnValidate()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
            if (_button == null)
            {
                // remove component if no button found
                DestroyImmediate(this);
            }
        }
    }

    private void OnEnable()
    {
        // appear from 0 to 1 alpha 
        _button.gameObject.transform.localScale = Vector3.zero;
        _button.gameObject.transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack);
    }

    private void Effect()
    {
        _button.gameObject.transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            _button.gameObject.transform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
        });
        
        _audioService?.PlayClick();
    }
}
