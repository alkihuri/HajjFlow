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
        
        private List<TheoryCardBase> _cards = new List<TheoryCardBase>();

        public event Action<int> OnCardChanged;
        public UnityEvent OnTheoryCardsCompleted = new UnityEvent();

        public int CurrentCardIndex { get; private set; }
        public int TotalCards => _cards.Count;
        
        private bool _isInitialized;
        private bool _theoryCompleted;

        /// <summary>
        /// Возвращает список данных карточек (из контейнера или напрямую)
        /// </summary>
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
 
            
            // Создаём карточки сразу под все данные
            CreateCards();
            
            // Скрываем все карточки
            HideAllCards();
        }

        /// <summary>
        /// Создаёт карточки для каждого элемента данных
        /// </summary>
        private void CreateCards()
        {
            _cards.Clear();
            
            var dataList = CardDataList;
            
            for (int i = 0; i < dataList.Count; i++)
            {
                var cardObj = Instantiate(_cardPrefab, transform);
                var card = cardObj.GetComponent<TheoryCardBase>();
                
                if (card != null)
                {
                    card.Initialize(dataList[i]);
                    card.gameObject.SetActive(false);
                    
                    // Подписываемся на события свайпа
                    card.SwipeLeft += OnSwipeLeft;
                    card.SwipeRight += OnSwipeRight;
                    
                    _cards.Add(card);
                }
            }
            
            Debug.Log($"[TheoryCardsManager] Created {_cards.Count} cards");
        }

        private void HideAllCards()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    card.gameObject.SetActive(false);
                }
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
        /// </summary>
        public void ResetToStart()
        {
            Debug.Log("[TheoryCardsManager] Resetting to start");
            
            _theoryCompleted = false;
            CurrentCardIndex = 0;
            
            HideAllCards();
            
            if (_cards.Count > 0)
            {
                ShowCard(0);
            }
        }

        public void ShowNextCard()
        {
            int nextIndex = CurrentCardIndex + 1;
            
            // Проверяем достигли ли конца
            if (nextIndex >= _cards.Count)
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
            if (prevIndex < 0) return; // Не идём в минус
            ShowCard(prevIndex);
        }

        public void ShowCard(int index)
        {
            if (_cards.Count == 0)
            {
                Debug.LogWarning("[TheoryCardsManager] No cards available!");
                return;
            }

            if (index < 0 || index >= _cards.Count)
            {
                Debug.LogWarning($"[TheoryCardsManager] Invalid index: {index}");
                return;
            }

            Debug.Log($"[TheoryCardsManager] ShowCard({index}/{_cards.Count - 1})");

            _counterText.text = $"{index+1}/{_cards.Count}";
            
            // Скрываем текущую карточку
            if (CurrentCardIndex >= 0 && CurrentCardIndex < _cards.Count && CurrentCardIndex != index)
            {
                _cards[CurrentCardIndex]?.gameObject.SetActive(false);
            }

            // Показываем новую карточку
            var targetCard = _cards[index];
            if (targetCard != null)
            {
                targetCard.Show();
            }

            CurrentCardIndex = index;
            OnCardChanged?.Invoke(index);
        }

        private void OnDestroy()
        {
            foreach (var card in _cards)
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

    