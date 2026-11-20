using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingScreenController : MonoBehaviour
{
     
     private string []  loadingMessages = new string[]  
     {
         "Загрузка данных",
         "Подготовка ресурсов",
         "Инициализация системы",
         "Проверка обновлений",
         "Оптимизация производительности",
         "Настройка интерфейса",
         "Синхронизация с сервером",
         "Обработка пользовательских данных",
         "Загрузка текстур и моделей",
         "Финальная проверка"
     };
     [SerializeField] TextMeshProUGUI loadingText;
     private void OnEnable()
     {
         StartCoroutine(Loading());
     }

     
     private IEnumerator Loading()
     {
         /// simple loadinf 3 dots animation
          
            int dotCount = 0;
            while (true)
            {
                dotCount = (dotCount + 1) % 4; // Cycle through 0, 1, 2, 3 
                for(int i = 0; i < loadingMessages.Length; i++)
                {
                    for(int dot = 0; dot < 4; dot++)
                    {
                        loadingText.text = loadingMessages[i] + new string('.', dot);
                        yield return new WaitForSeconds(0.1f);
                    } 
                    yield return new WaitForSeconds(1f);
                }   
                yield return new WaitForSeconds(0.5f); // Update every half second
            }
     }

     private void OnDisable()
     {
         StopAllCoroutines();   
     }
}
