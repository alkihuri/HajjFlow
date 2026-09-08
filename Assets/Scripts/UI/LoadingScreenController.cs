using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingScreenController : MonoBehaviour
{
     
     [SerializeField] TextMeshProUGUI loadingText;
     private void OnEnable()
     {
         StartCoroutine(Loading());
     }

     
     private IEnumerator Loading()
     {
         /// simple loadinf 3 dots animation
         
         var baseText = loadingText.text;
            int dotCount = 0;
            while (true)
            {
                dotCount = (dotCount + 1) % 4; // Cycle through 0, 1, 2, 3
                loadingText.text = baseText + new string('.', dotCount);
                yield return new WaitForSeconds(0.5f); // Update every half second
            }
     }

     private void OnDisable()
     {
         StopAllCoroutines();   
     }
}
