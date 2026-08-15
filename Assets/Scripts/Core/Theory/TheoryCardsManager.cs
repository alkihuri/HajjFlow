using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using HajjFlow.Core;
using HajjFlow.Services;

namespace Core.Theory
{
    public class TheoryCardsManager : MonoBehaviour
    {
        [field: Header("Data Source (use one)")]
        [field: SerializeField]
        public TheoryCardContainer CardContainer { get; set; }

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
        private AudioService _audioService;

        private List<TheoryCardData> CardDataList
        {
            get
            {
                if (CardContainer != null && CardContainer.Cards.Count > 0)
                {
                    return CardContainer.Cards;
                }
                return _data;
            }
        }

        private void Awake()
        {
            // Не инициализируем здесь! Дождёмся установки CardContainer в LevelController.Init()
            Debug.Log("[TheoryCardsManager] Awake - waiting for CardContainer to be set by LevelController");
        }

        private void OnEnable()
        {
            // При включении объекта проверяем нужна ли инициализация
            if (!_isInitialized && CardContainer != null)
            {
                Debug.Log("[TheoryCardsManager] OnEnable - CardContainer is set, initializing now");
                InitializeTheory();
            }
            else if (!_isInitialized && _data.Count > 0)
            {
                Debug.Log("[TheoryCardsManager] OnEnable - No CardContainer but have runtime data, initializing");
                InitializeTheory();
            }
        }

        public void SkipTheory()
        {
            OnTheoryCardsCompleted?.Invoke();
        }

        /// <summary>
        /// Явно инициализирует теорию. Вызывается из Awake() автоматически,
        /// но может быть вызвана вручную для переинициализации.
        /// </summary>
        public void InitializeTheory()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[TheoryCardsManager] Already initialized! Call Reset() to reinitialize.");
                return;
            }

            _audioService = GameManager.Instance?.GetService<AudioService>();
            
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
            
            Debug.Log($"[TheoryCardsManager] Initializing with {dataList.Count} cards");
            CreateCards();
            UpdateCounter();
            _isInitialized = true;
        }

        private void CreateCards()
        {
            _cards.Clear();
            
            var dataList = CardDataList;
            if (dataList == null || dataList.Count == 0)
            {
                Debug.LogWarning("[TheoryCardsManager] No card data available!");
                return;
            }

            int totalCount = dataList.Count;
            
            Debug.Log($"[TheoryCardsManager] Creating {totalCount} cards as deck");
            
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
                    Debug.Log($"[TheoryCardsManager] Created card {i}: {dataList[i].Title}");
                }
            }
            
            // Устанавливаем z-order так чтобы новые карточки были сверху
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
            _audioService?.PlayWhoosh();
            
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

        /// <summary>
        /// Инициализирует менеджер карточек из рантайм-данных (без ScriptableObject контейнера).
        /// Вызывается RuntimeLevelFactory при использовании удалённого контента.
        /// </summary>
        public void InitializeFromRuntimeData(List<TheoryCardData> runtimeCards)
        {
            if (runtimeCards == null || runtimeCards.Count == 0)
            {
                Debug.LogWarning("[TheoryCardsManager] No runtime cards provided!");
                return;
            }

            Debug.Log($"[TheoryCardsManager] Initializing from runtime data: {runtimeCards.Count} cards");

            // Очищаем предыдущие карточки
            foreach (var card in _cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            _cards.Clear();

            // Устанавливаем данные
            _data = runtimeCards;
            CardContainer = null; // Сбрасываем контейнер, чтобы использовался _data

            _isInitialized = false;
            _theoryCompleted = false;
            CurrentCardIndex = 0;

            Debug.Log($"[TheoryCardsManager] Reset state and preparing to create cards");
            CreateCards();
            UpdateCounter();
        }

        /// <summary>
        /// Инициализирует менеджер карточек из RuntimeLevelFactory (для удалённого контента из Google Sheets).
        /// Вызывается после загрузки контента из ContentLoaderService.
        /// </summary>
        public void InitializeFromRuntimeModels(string levelId)
        {
            var runtimeFactory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
            
            if (runtimeFactory == null)
            {
                Debug.LogError("[TheoryCardsManager] RuntimeLevelFactory service not found!");
                return;
            }

            if (!runtimeFactory.IsContentAvailable)
            {
                Debug.LogWarning("[TheoryCardsManager] Runtime content is not loaded yet. Wait for ContentLoaderService.OnLoadComplete");
                return;
            }

            var runtimeTheoryCards = runtimeFactory.BuildTheoryCards(levelId);
            
            if (runtimeTheoryCards == null || runtimeTheoryCards.Count == 0)
            {
                Debug.LogWarning($"[TheoryCardsManager] No theory cards found for level '{levelId}'");
                return;
            }

            Debug.Log($"[TheoryCardsManager] Initializing from runtime models: {runtimeTheoryCards.Count} cards for level '{levelId}'");

            // Очищаем предыдущие карточки
            foreach (var card in _cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            _cards.Clear();

            // Устанавливаем данные
            _data = runtimeTheoryCards;
            CardContainer = null; // Сбрасываем контейнер, чтобы использовался _data

            _isInitialized = false;
            _theoryCompleted = false;
            CurrentCardIndex = 0;

            Debug.Log($"[TheoryCardsManager] Reset state and creating {runtimeTheoryCards.Count} cards from runtime models");
            CreateCards();
            UpdateCounter();
        }

        private void OnDestroy()
        {
            _cards.Clear();
        }
    }
}

    