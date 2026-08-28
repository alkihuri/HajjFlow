using System;
using UnityEngine;
using GSheetsCommander;
public class RegistrationService : MonoBehaviour
{
   [SerializeField] private RegistrationSceneUI _registrationSceneUI;

   [SerializeField] private GoogleSheetsConfig _googleSheetsConfig;
   
   private GoogleSheetsClient _googleSheetsClient;

   private void Awake()
   {
       _googleSheetsClient = new GoogleSheetsClient(_googleSheetsConfig);
   }


   public void RegisterUser(string username, string group)
   {  
       
       // Create a new row with the username and group
       var newRow = new string[] { username, "_" ,  "_", "_" };

        // create sheet 
        _googleSheetsClient.CreateSheetAsync(group);
        
        _googleSheetsClient.AppendRowAsync(group, newRow);
         
   }
}
