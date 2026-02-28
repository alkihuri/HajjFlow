using System;
using System.Collections.Generic;
using System.Linq;
using HajjFlow.Core;
using HajjFlow.Services;
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

        public int CurentCardIndex = 0;

        private void Awake()
        {
            _cardsPool = gameObject.GetComponentsInChildren<TheoryCardBase>().ToList();


            foreach (var card in _data)
            {
                var cardInstance = GetFromPool();
                cardInstance.Initialize(card);
                cardInstance.gameObject.SetActive(true);
            }


            foreach (var theoryCardBase in _cardsPool)
            {
                theoryCardBase.gameObject.SetActive(false);
            }

            // subscribe to swipe event showing next card
            foreach (var card in _cardsPool)
            {
                card.SwipeLeft += () => ShowNextCard();
                card.SwipeRight += () => ShowPreviousCard();
            }


            ShowCard(0);
        }

        public void ShowNextCard()
        {
            ShowCard(++CurentCardIndex);
        }

        public void ShowPreviousCard()
        {
            ShowCard(--CurentCardIndex);
        }

        public void ShowCard(int index)
        {
            Debug.Log($"ShowCard({index})");
            // when last card is shown, invoke completion event
            if (index == _cardsPool.Count )
            {
                OnTheoryCardsCompleted?.Invoke();
            }
            index = Math.Clamp(index, 0, _cardsPool.Count - 1);
            OnCardChanged?.Invoke(index);
            
          
            
            
            if (index < 0 || index >= _cardsPool.Count)
            {
                Debug.LogError($"[TheoryCardsManager] Invalid card index: {index}");
                return;
            }

            

            _cardsPool[index].Show();
            CurentCardIndex = index;
 
            
        }
 

        private TheoryCardBase GetFromPool()
        {
            // Try to find an inactive card in the pool
            var card = _cardsPool.FirstOrDefault(c => !c.gameObject.activeInHierarchy);
            if (card != null)
                return card;
            else
            {
                var newCardObj = Instantiate(_cardPrefab, transform);
                var newCard = newCardObj.GetComponent<TheoryCardBase>();
                _cardsPool.Add(newCard);
                return newCard;
            }
        }
    }
}

    