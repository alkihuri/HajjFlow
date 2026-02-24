using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Theory
{
    public class TheoryCardsManager : MonoBehaviour
    { 
         [SerializeField] private List<TheoryCardBase> _cards = new ();
         
         [SerializeField] private List<TheoryCardData> _data = new ();
         
         [SerializeField] private TheoryCardBase _cardPrefab;

         private List<TheoryCardBase> _cardsPool = new List<TheoryCardBase>();

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
             foreach (var card in _cardsPool)             {
                 card.SwipeLeft += () => ShowCard((_data.IndexOf(card.Data) + 1) % _data.Count);
                 card.SwipeRight += () => ShowCard((_data.IndexOf(card.Data) - 1 + _data.Count) % _data.Count);
             }
             
             
             ShowCard(1);
         }

         
         public void ShowCard(int index)
         {
             if (index < 0 || index >= _data.Count)
             {
                 Debug.LogError($"[TheoryCardsManager] Invalid card index: {index}");
                 return;
             }
             
             var cardData = _data[index];
             var cardInstance = GetFromPool();
             HideAllCards();
             cardInstance.Initialize(cardData);
             cardInstance.gameObject.SetActive(true);
         }

         private void HideAllCards()
         {
             foreach (var card in _cardsPool)
             {
                 card.gameObject.SetActive(false);  
             }
             
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
