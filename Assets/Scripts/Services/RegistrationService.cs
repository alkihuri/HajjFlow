using System;
using System.Threading.Tasks;
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


   
   // callback function to be called after registration is complete
  public async Task RegisterUserAsync(string username, string group, Action doneCallback)
  {
      if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(group))
      {
          Debug.LogError("Username and group cannot be empty");
          return;
      }
  
      try
      {
          var newRow = new string[] { username, "", "", "" };
          
          await _googleSheetsClient.CreateSheetAsync(group);
          await _googleSheetsClient.AppendRowAsync(group, newRow);
          
          doneCallback?.Invoke();
      }
      catch (Exception ex)
      {
          Debug.LogError($"Registration failed: {ex.Message}");
      }
  }
}
