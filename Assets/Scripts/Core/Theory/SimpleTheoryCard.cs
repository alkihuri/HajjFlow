using UnityEngine;

namespace Core.Theory
{
    /// <summary>
    /// Simple implementation of theory card
    /// </summary>
    public class SimpleTheoryCard : TheoryCardBase
    {
        // You can add card-specific functionality here
        // For example, playing sounds, particle effects, etc.
        
        [Header("Audio")]
        [SerializeField] private AudioClip _swipeSound;
        
        private AudioSource _audioSource;

        private void Start()
        {
            // Subscribe to swipe events for additional effects
          //  SwipeDetected += OnSwipeDetected;
            
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void OnDestroy()
        {
           // SwipeDetected -= OnSwipeDetected;
        }

        private void OnSwipeDetected(SwipeDirection direction)
        {
            // Play sound
            if (_audioSource != null && _swipeSound != null)
            {
                _audioSource.PlayOneShot(_swipeSound);
            }
            
            // You can add more effects here
            Debug.Log($"[SimpleTheoryCard] Swiped {direction}: {Data?.Title}");
        }
    }
}

