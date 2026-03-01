using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Theory
{
    public class TheoryCardsManager : MonoBehaviour
    {
        [SerializeField] private List<TheoryCardData> _data = new();
        [SerializeField] private TheoryCardBase _cardPrefab;
        [SerializeField] private List<TheoryCardBase> _cardsPool = new List<TheoryCardBase>();

        public event Action<int> OnCardChanged;
        public UnityEvent OnTheoryCardsCompleted = new UnityEvent();

        public int CurrentCardIndex { get; private set; }
        
        private bool _isInitialized;
        private bool _theoryCompleted;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Get existing cards from children
            _cardsPool = GetComponentsInChildren<TheoryCardBase>(true).ToList();

            // Initialize cards from data
            for (int i = 0; i < _data.Count; i++)
            {
                var cardInstance = GetFromPool();
                if (cardInstance != null)
                {
                    cardInstance.Initialize(_data[i]);
                    cardInstance.gameObject.SetActive(false);
                }
            }

            // Subscribe to swipe events
            SubscribeToSwipeEvents();
        }

        private void SubscribeToSwipeEvents()
        {
            foreach (var card in _cardsPool)
            {
                if (card == null) continue;
                
                // Unsubscribe first to avoid duplicates
                card.SwipeLeft -= OnSwipeLeft;
                card.SwipeRight -= OnSwipeRight;
                
                // Subscribe
                card.SwipeLeft += OnSwipeLeft;
                card.SwipeRight += OnSwipeRight;
            }
        }

        private void OnSwipeLeft()
        {
            ShowNextCard();
        }

        private void OnSwipeRight()
        {
            ShowPreviousCard();
        }

        /// <summary>
        /// Сбрасывает состояние карточек к начальному.
        /// Скрывает все карточки и показывает первую.
        /// </summary>
        public void ResetToStart()
        {
            Debug.Log("[TheoryCardsManager] Resetting to start");
            
            _theoryCompleted = false;
            
            // Hide all cards
            foreach (var card in _cardsPool)
            {
                if (card != null)
                {
                    card.gameObject.SetActive(false);
                }
            }
            
            // Reset index
            CurrentCardIndex = 0;
            
            // Show first card
            if (_cardsPool.Count > 0)
            {
                ShowCard(0);
            }
        }

        public void ShowNextCard()
        {
            int nextIndex = CurrentCardIndex + 1;
            
            // Check if we've reached the end
            if (nextIndex >= _cardsPool.Count)
            {
                if (!_theoryCompleted)
                {
                    _theoryCompleted = true;
                    Debug.Log("[TheoryCardsManager] Theory completed!");
                    OnTheoryCardsCompleted?.Invoke();
                }
                return;
            }
            
            ShowCard(nextIndex);
        }

        public void ShowPreviousCard()
        {
            int prevIndex = CurrentCardIndex - 1;
            if (prevIndex < 0) prevIndex = 0;
            ShowCard(prevIndex);
        }

        public void ShowCard(int index)
        {
            Debug.Log($"[TheoryCardsManager] ShowCard({index})");

            if (_cardsPool.Count == 0)
            {
                Debug.LogWarning("[TheoryCardsManager] No cards in pool!");
                return;
            }

            // Clamp index
            index = Mathf.Clamp(index, 0, _cardsPool.Count - 1);

            // Hide current card
            if (CurrentCardIndex >= 0 && CurrentCardIndex < _cardsPool.Count)
            {
                var currentCard = _cardsPool[CurrentCardIndex];
                if (currentCard != null && currentCard != _cardsPool[index])
                {
                    currentCard.gameObject.SetActive(false);
                }
            }

            // Show new card
            var targetCard = _cardsPool[index];
            if (targetCard != null)
            {
                targetCard.Show();
            }

            CurrentCardIndex = index;
            OnCardChanged?.Invoke(index);
        }

        private TheoryCardBase GetFromPool()
        {
            // Try to find an inactive card in the pool
            var card = _cardsPool.FirstOrDefault(c => c != null && !c.gameObject.activeInHierarchy);
            if (card != null)
                return card;

            // Create new card if prefab exists
            if (_cardPrefab != null)
            {
                var newCardObj = Instantiate(_cardPrefab, transform);
                var newCard = newCardObj.GetComponent<TheoryCardBase>();
                if (newCard != null)
                {
                    _cardsPool.Add(newCard);
                    
                    // Subscribe to events for new card
                    newCard.SwipeLeft += OnSwipeLeft;
                    newCard.SwipeRight += OnSwipeRight;
                }
                return newCard;
            }

            return null;
        }

        private void OnDestroy()
        {
            // Unsubscribe from all events
            foreach (var card in _cardsPool)
            {
                if (card != null)
                {
                    card.SwipeLeft -= OnSwipeLeft;
                    card.SwipeRight -= OnSwipeRight;
                }
            }
        }
    }
}

    