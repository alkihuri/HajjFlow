using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Core.Theory
{
    public class TheoryCardsManager : MonoBehaviour
    {
        [Header("Data Source (use one)")]
        [SerializeField] private TheoryCardContainer _cardContainer;
        [SerializeField] private List<TheoryCardData> _data = new();
        [SerializeField] private TextMeshProUGUI _counterText;
        
        [Header("Prefab")]
        [SerializeField] private TheoryCardBase _cardPrefab;
        
        [Header("Stack Settings")]
        [SerializeField] private float _stackOffsetX = 8f;
        [SerializeField] private float _stackOffsetY = -8f;
        [SerializeField] private float _animationDuration = 0.25f;
        
        private List<TheoryCardBase> _cards = new List<TheoryCardBase>();

        public event Action<int> OnCardChanged;
        public UnityEvent OnTheoryCardsCompleted = new UnityEvent();

        public int CurrentCardIndex { get; private set; }
        public int TotalCards => CardDataList?.Count ?? 0;
        
        private bool _isInitialized;
        private bool _theoryCompleted;

        private List<TheoryCardData> CardDataList
        {
            get
            {
                if (_cardContainer != null && _cardContainer.Cards.Count > 0)
                {
                    return _cardContainer.Cards;
                }
                return _data;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        public void SkipTheory()
        {
            OnTheoryCardsCompleted?.Invoke();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            if (_cardPrefab == null)
            {
                Debug.LogError("[TheoryCardsManager] Card prefab is not assigned!");
                return;
            }

            var dataList = CardDataList;
            if (dataList == null || dataList.Count == 0)
            {
                Debug.LogWarning("[TheoryCardsManager] No data to create cards!");
                return;
            }
            
            Debug.Log($"[TheoryCardsManager] Initializing with {dataList.Count} cards from config");
            
            CreateCards();
            UpdateCounter();
        }

        private void CreateCards()
        {
            _cards.Clear();
            
            var dataList = CardDataList;
            int totalCount = dataList.Count;
            
            for (int i = 0; i < totalCount; i++)
            {
                var cardObj = Instantiate(_cardPrefab, transform);
                cardObj.name = $"Card_{i:D2}_{dataList[i].Title}";
                
                var card = cardObj.GetComponent<TheoryCardBase>();
                
                if (card != null)
                {
                    card.Initialize(dataList[i]);
                    card.gameObject.SetActive(true);
                    
                    card.CardIndex = i;
                    card.SetStackPosition(i, _stackOffsetX, _stackOffsetY);
                    
                    if (i == 0)
                    {
                        card.SetAsActiveCard();
                    }
                    else
                    {
                        card.SetAsInactiveCard();
                    }
                    
                    card.SwipeLeft += () => OnCardSwiped(card);
                    
                    _cards.Add(card);
                }
            }
            
            for (int i = totalCount - 1; i >= 0; i--)
            {
                _cards[i].transform.SetAsLastSibling();
            }
            
            Debug.Log($"[TheoryCardsManager] Created {_cards.Count} cards as deck");
        }

        private void OnCardSwiped(TheoryCardBase swipedCard)
        {
            int swipedIndex = swipedCard.CardIndex;
            int nextIndex = swipedIndex + 1;
            
            Debug.Log($"[TheoryCardsManager] Card {swipedIndex} swiped, next: {nextIndex}, total: {TotalCards}");
            
            swipedCard.SetAsInactiveCard();
            
            if (nextIndex >= TotalCards)
            {
                if (!_theoryCompleted)
                {
                    _theoryCompleted = true;
                    CurrentCardIndex = TotalCards;
                    UpdateCounter();
                    Debug.Log($"[TheoryCardsManager] Theory completed! Viewed all {TotalCards} cards.");
                    OnTheoryCardsCompleted?.Invoke();
                }
                return;
            }
            
            // Анимируем все оставшиеся карточки - сдвигаем их на одну позицию вверх в стеке
            AnimateStackShift(nextIndex);
            
            CurrentCardIndex = nextIndex;
            UpdateCounter();
            OnCardChanged?.Invoke(nextIndex);
        }

        /// <summary>
        /// Плавно сдвигает все карточки начиная с nextIndex на одну позицию вверх в стеке
        /// </summary>
        private void AnimateStackShift(int nextIndex)
        {
            for (int i = nextIndex; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null) continue;
                
                int newStackPosition = i - nextIndex; // Новая позиция в стеке (0 для следующей карты)
                
                // Анимируем перемещение на новую позицию
                card.AnimateToStackPosition(newStackPosition, _stackOffsetX, _stackOffsetY, _animationDuration);
                
                // Первая карта становится активной
                if (newStackPosition == 0)
                {
                    card.SetAsActiveCard();
                    card.transform.SetAsLastSibling();
                }
            }
        }

        private void UpdateCounter()
        {
            if (_counterText != null)
            {
                int total = TotalCards;
                int current = Mathf.Min(CurrentCardIndex + 1, total);
                _counterText.text = $"{current}/{total}";
            }
        }

        public void ResetToStart()
        {
            Debug.Log("[TheoryCardsManager] Resetting to start");
            
            _theoryCompleted = false;
            CurrentCardIndex = 0;
            
            int totalCount = _cards.Count;
            
            for (int i = 0; i < totalCount; i++)
            {
                var card = _cards[i];
                if (card == null) continue;
                
                card.gameObject.SetActive(true);
                card.ResetCardState();
                card.SetStackPosition(i, _stackOffsetX, _stackOffsetY);
                
                if (i == 0)
                {
                    card.SetAsActiveCard();
                }
                else
                {
                    card.SetAsInactiveCard();
                }
            }
            
            for (int i = totalCount - 1; i >= 0; i--)
            {
                _cards[i].transform.SetAsLastSibling();
            }
            
            UpdateCounter();
        }

        public void ShowNextCard()
        {
            if (CurrentCardIndex < _cards.Count)
            {
                OnCardSwiped(_cards[CurrentCardIndex]);
            }
        }

        public void ShowCard(int index)
        {
            if (index < 0 || index >= TotalCards)
            {
                Debug.LogWarning($"[TheoryCardsManager] Invalid index: {index}");
                return;
            }

            for (int i = 0; i < index; i++)
            {
                if (i < _cards.Count)
                {
                    _cards[i].SetAsInactiveCard();
                    _cards[i].gameObject.SetActive(false);
                }
            }
            
            if (index < _cards.Count)
            {
                _cards[index].SetAsActiveCard();
                _cards[index].transform.SetAsLastSibling();
            }
            
            CurrentCardIndex = index;
            UpdateCounter();
            OnCardChanged?.Invoke(index);
        }

        private void OnDestroy()
        {
            _cards.Clear();
        }
    }
}

    