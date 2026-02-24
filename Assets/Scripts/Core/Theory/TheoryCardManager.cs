using System.Collections.Generic;
using UnityEngine;

namespace Core.Theory
{
    /// <summary>
    /// Manages theory cards stack and handles card navigation via swipes
    /// </summary>
    public class TheoryCardManager : MonoBehaviour
    {
        [SerializeField] private TheoryCardBase _cardPrefab;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private List<TheoryCardData> _cardDataList;
        
        [Header("Card Stack Settings")]
        [SerializeField] private int _maxVisibleCards = 3;
        [SerializeField] private float _cardOffset = 10f;
        [SerializeField] private float _cardScaleDecrement = 0.05f;
        
        private Stack<TheoryCardBase> _cardStack = new Stack<TheoryCardBase>();
        private int _currentCardIndex = 0;

        private void Start()
        {
            InitializeCardStack();
        }

        private void InitializeCardStack()
        {
            if (_cardDataList == null || _cardDataList.Count == 0)
            {
                Debug.LogWarning("[TheoryCardManager] No card data provided!");
                return;
            }

            // Create initial visible cards
            int cardsToCreate = Mathf.Min(_maxVisibleCards, _cardDataList.Count);
            
            for (int i = cardsToCreate - 1; i >= 0; i--)
            {
                CreateCard(i);
            }
        }

        private void CreateCard(int dataIndex)
        {
            if (dataIndex >= _cardDataList.Count)
                return;

            TheoryCardBase card = Instantiate(_cardPrefab, _cardContainer);
            card.Initialize(_cardDataList[dataIndex]);
            
            // Subscribe to swipe events
            card.SwipeLeft += () => OnCardSwiped(card, SwipeDirection.Left);
            card.SwipeRight += () => OnCardSwiped(card, SwipeDirection.Right);
            card.SwipeUp += () => OnCardSwiped(card, SwipeDirection.Up);
            card.SwipeDown += () => OnCardSwiped(card, SwipeDirection.Down);
            
            // Position card in stack
            UpdateCardStackPosition(card, _cardStack.Count);
            
            _cardStack.Push(card);
        }

        private void UpdateCardStackPosition(TheoryCardBase card, int stackIndex)
        {
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            // Position cards behind the top one with offset
            Vector3 offset = new Vector3(0, -stackIndex * _cardOffset, stackIndex);
            rectTransform.localPosition = offset;
            
            // Scale down cards behind
            float scale = 1f - (stackIndex * _cardScaleDecrement);
            rectTransform.localScale = Vector3.one * scale;
            
            // Set sibling index for proper rendering order
            rectTransform.SetSiblingIndex(_cardStack.Count - stackIndex);
        }

        private void OnCardSwiped(TheoryCardBase card, SwipeDirection direction)
        {
            Debug.Log($"[TheoryCardManager] Card swiped {direction}: {card.Data.Title}");
            
            // Remove from stack
            if (_cardStack.Count > 0 && _cardStack.Peek() == card)
            {
                _cardStack.Pop();
            }
            
            // Move to next card
            _currentCardIndex++;
            
            // Create new card if available
            int nextCardIndex = _currentCardIndex + _cardStack.Count;
            if (nextCardIndex < _cardDataList.Count)
            {
                CreateCard(nextCardIndex);
            }
            
            // Update remaining cards positions
            UpdateStackPositions();
            
            // Check if stack is empty
            if (_cardStack.Count == 0)
            {
                OnAllCardsCompleted();
            }
        }

        private void UpdateStackPositions()
        {
            int index = 0;
            foreach (TheoryCardBase card in _cardStack)
            {
                UpdateCardStackPosition(card, index);
                index++;
            }
        }

        private void OnAllCardsCompleted()
        {
            Debug.Log("[TheoryCardManager] All cards completed!");
            // Here you can trigger quiz or next level
        }

        /// <summary>
        /// Reload all cards from the beginning
        /// </summary>
        public void ResetCards()
        {
            // Clear existing cards
            foreach (TheoryCardBase card in _cardStack)
            {
                Destroy(card.gameObject);
            }
            _cardStack.Clear();
            
            _currentCardIndex = 0;
            InitializeCardStack();
        }
    }
}

