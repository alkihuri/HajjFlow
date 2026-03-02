using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

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
            
            // Создаём карточки - последняя в данных будет внизу стека
            for (int i = 0; i < totalCount; i++)
            {
                var cardObj = Instantiate(_cardPrefab, transform);
                var card = cardObj.GetComponent<TheoryCardBase>();
                
                if (card != null)
                {
                    card.Initialize(dataList[i]);
                    card.gameObject.SetActive(true);
                    
                    // Устанавливаем индекс карточки
                    card.CardIndex = i;
                    
                    // Позиция в стеке: первая карта сверху (index 0), остальные смещены
                    int stackPosition = i;
                    card.SetStackPosition(stackPosition, _stackOffsetX, _stackOffsetY);
                    
                    // Порядок в иерархии: первая карта поверх всех
                    card.transform.SetSiblingIndex(totalCount - 1 - i);
                    
                    // Только первая карта активна для свайпа
                    if (i == 0)
                    {
                        card.SetAsActiveCard();
                    }
                    else
                    {
                        card.SetAsInactiveCard();
                    }
                    
                    // Подписываемся на событие свайпа
                    card.SwipeLeft += () => OnCardSwiped(card);
                    
                    _cards.Add(card);
                }
            }
            
            Debug.Log($"[TheoryCardsManager] Created {_cards.Count} cards as deck");
        }

        private void OnCardSwiped(TheoryCardBase swipedCard)
        {
            int swipedIndex = swipedCard.CardIndex;
            int nextIndex = swipedIndex + 1;
            
            Debug.Log($"[TheoryCardsManager] Card {swipedIndex} swiped, next: {nextIndex}, total: {TotalCards}");
            
            // Деактивируем свайпнутую карту
            swipedCard.SetAsInactiveCard();
            
            // Проверяем завершение теории
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
            
            // Активируем следующую карту
            if (nextIndex < _cards.Count)
            {
                _cards[nextIndex].SetAsActiveCard();
                _cards[nextIndex].transform.SetAsLastSibling();
            }
            
            CurrentCardIndex = nextIndex;
            UpdateCounter();
            OnCardChanged?.Invoke(nextIndex);
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
                
                // Восстанавливаем позицию в стеке
                card.SetStackPosition(i, _stackOffsetX, _stackOffsetY);
                card.transform.SetSiblingIndex(totalCount - 1 - i);
                
                if (i == 0)
                {
                    card.SetAsActiveCard();
                }
                else
                {
                    card.SetAsInactiveCard();
                }
            }
            
            UpdateCounter();
        }

        public void ShowNextCard()
        {
            if (CurrentCardIndex < _cards.Count)
            {
                // Симулируем свайп текущей карты
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

            // Деактивируем все карты до нужного индекса
            for (int i = 0; i < index; i++)
            {
                if (i < _cards.Count)
                {
                    _cards[i].SetAsInactiveCard();
                    _cards[i].gameObject.SetActive(false);
                }
            }
            
            // Активируем нужную карту
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

    