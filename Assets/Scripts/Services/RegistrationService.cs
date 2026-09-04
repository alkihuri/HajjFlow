using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GSheetsCommander;
using HajjFlow.Core;
using HajjFlow.Services;
using Newtonsoft.Json;
using UnityEngine;

public class RegistrationService : MonoBehaviour
{
   private const string UsernamePreferenceKey = "Username";
   private const string GroupPreferenceKey = "Group";
   private const string UserIdPreferenceKey = "UserID";
   private const string SheetRowPreferenceKey = "RegistrationSheetRow";
   private const string SheetNamePreferenceKey = "RegistrationSheetName";

   [SerializeField] private RegistrationSceneUI _registrationSceneUI;

   [SerializeField] private GoogleSheetsConfig _googleSheetsConfig;
   
   private GoogleSheetsClient _googleSheetsClient;

   private void Awake()
   {
       _googleSheetsClient = new GoogleSheetsClient(_googleSheetsConfig);
   }

   private async void Start()
   {
       // К этому моменту Bootstrapper уже зарегистрировал сервисы. Загружаем
       // серверные данные до того, как локальный кэш будет использоваться далее.
   }

   /// <summary>
   /// Загружает прогресс пользователя из Google Sheets при запуске приложения.
   /// Вызывается, если пользователь уже зарегистрирован.
   /// </summary>


   /// HARD CODE RE REGISTER
   [ContextMenu("UpdateData")]
   public async Task UpdateDataInGoogleSheets()
   {
       var username = PlayerPrefs.GetString(UsernamePreferenceKey);
         var group = PlayerPrefs.GetString(GroupPreferenceKey);

          // if either username or group is empty, log a warning and return
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(group))
            {
                Debug.LogError($"[RegistrationService] Username and group cannot be empty");
                return;
            }
            
            await RegisterUserAsync(username, group, () => Debug.Log("Re-registration complete"));
         
   }
   
   
   // callback function to be called after registration is complete
  public async Task RegisterUserAsync(string username, string group, Action doneCallback)
  {
      if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(group))
      {
          Debug.LogError("Username and group cannot be empty");
          return;
      }
    
      // get progress from StageCompletionService
        var stageCompletionService = GameManager.Instance.GetService<StageCompletionService>();
        var levelResults = stageCompletionService.GetAllLevelsResult();

// save username and group to PlayerPrefs
      PlayerPrefs.SetString(UsernamePreferenceKey, username);
      PlayerPrefs.SetString(GroupPreferenceKey, group);
      PlayerPrefs.Save();
      
      Debug.Log($"[RegistrationService] Registering user: {username}, group: {group}");

       
        
    try
    {
        var payload = new
        {
            UserId = Guid.NewGuid().ToString(),
            fullName = username,
            pilgrimNumber = username,
            groupId = group,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
            Status = "active",
            LevelResults = levelResults // Initialize with empty progress
        };
        
        PlayerPrefs.SetString(UserIdPreferenceKey, payload.UserId);
        
        Debug.Log($"[RegistrationService] Sending registration request: {JsonConvert.SerializeObject(payload)}");
        var result = await _googleSheetsClient.SendAsync<CreateUserResponse>("createUser", payload);
        Debug.Log($"[RegistrationService] Registration successful: {result}");
        doneCallback?.Invoke();
    }
    catch (GoogleSheetsException gex)
    {
        Debug.LogError($"[RegistrationService] GoogleSheets error - Code: {gex.Code}, Message: {gex.Message}, Details: {gex.Details}");
        
        
        
        
        doneCallback?.Invoke();
    }
    catch (Exception ex)
    {
        Debug.LogError($"[RegistrationService] Registration failed: {ex.Message}\n{ex.StackTrace}");
        doneCallback?.Invoke();
    }
  }

  /// <summary>
  /// Загружает прогресс пользователя из Google Sheets и синхронизирует с StageCompletionService.
  /// Вызывается при повторной регистрации (когда пользователь уже есть в таблице).
  /// </summary>
  private async Task SyncProgressFromSheetAsync(string group, int userRow, List<ContentLoaderService.RuntimeLevelInfo> allLevels)
  {
       
  }


  [ContextMenu("Test level 0 to 100%")]
  private async void TestSaveLevelResult()
  {
      await SaveLevelResultAsync("level_1", 100f, success => Debug.Log($"TestSaveLevelResult callback: {success}"));
  }
  /// <summary>
  /// Stores a completed level score in the registered user's row.
  /// Динамически определяет колонку на основе позиции уровня в списке.
  /// Использует маппинг по levelId для корректного соответствия колонкам.
  /// </summary>
  public async Task SaveLevelResultAsync(string levelId, float scorePercent, Action <bool> doneCallback=null)
  {
      
      
      var userId = PlayerPrefs.GetString(UserIdPreferenceKey);
      
      Debug.unityLogger.Log($"[RegistrationService] Syncing level results for {userId}");

      if (string.IsNullOrWhiteSpace(userId))
      {
          Debug.LogError($"[RegistrationService] UserId cannot be empty");
          return;
      }
       
       var payload = new
       {
           userId = userId, 
       };

       await _googleSheetsClient.SendAsync<CreateUserResponse>("updateUser", payload);
  }

  /// <summary>
  /// Преобразует индекс в буквенный адрес колонки (0->A, 1->B, 25->Z, 26->AA и т.д.).
  /// </summary>
  private static string GetColumnLetter(int columnIndex)
  {
      string columnName = "";
      while (columnIndex > 0)
      {
          columnIndex--;
          columnName = (char)('A' + (columnIndex % 26)) + columnName;
          columnIndex /= 26;
      }
      return columnName;
  }


  private async Task<int> FindUserRowAsync(string group, string username)
  {
      var range = await _googleSheetsClient.GetRangeAsync(group, "A2:A20");
      if (range.values == null) return -1;

      for (int index = 0; index < range.values.Length; index++)
      {
          object[] row = range.values[index];
          if (row != null && row.Length > 0 &&
              string.Equals(row[0]?.ToString()?.Trim(), username.Trim(), StringComparison.Ordinal))
              return index + 1; // Google Sheets row numbering starts at 1.
      }

      return -1;
  }
}
