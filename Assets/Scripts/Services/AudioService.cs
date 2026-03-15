using UnityEngine;

public class AudioService : MonoBehaviour
{
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            Debug.LogError("[AudioService] No AudioSource component found on this GameObject!");
        }
    }

    /// <summary>
    /// Plays the given audio clip.
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("[AudioService] Cannot play sound. " +
                             "AudioSource or AudioClip is null.");
        }
    }
    
    [ContextMenu("Whoosh click")]
    // play whoosh from resources/Audio
    public void PlayWhoosh()
    {
        AudioClip whooshClip = Resources.Load<AudioClip>("Audio/whoosh");
        if (whooshClip != null)
        {
            PlaySound(whooshClip);
        }
        else
        {
            Debug.LogWarning("[AudioService] Whoosh audio clip not found in Resources/Audio!");
        }
    }
    
    [ContextMenu("Play click")]
    // play click from resources/Audio
    public void PlayClick()
    {
        AudioClip clickClip = Resources.Load<AudioClip>("Audio/click");
        if (clickClip != null)
        {
            PlaySound(clickClip);
        }
        else
        {
            Debug.LogWarning("[AudioService] Click audio clip not found in Resources/Audio!");
        }
    }
}
