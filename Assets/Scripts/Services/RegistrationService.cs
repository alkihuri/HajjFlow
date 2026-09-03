using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GSheetsCommander;
using HajjFlow.Core;
using HajjFlow.Services;
using UnityEngine;

public class RegistrationService : MonoBehaviour
{
   private const string UsernamePreferenceKey = "Username";
   private const string GroupPreferenceKey = "Group";
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
   public async Task LoadUserProgressOnStartupAsync()
   {
       string username = PlayerPrefs.GetString(UsernamePreferenceKey, "");
       string group = PlayerPrefs.GetString(GroupPreferenceKey, "");

       if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(group))
       {
           Debug.Log("[RegistrationService] No registered user found, skipping progress load.");
           return;
       }

       
       
       try
       {
           var userProfileService = GameManager.Instance.GetService<UserProfileService>();
           userProfileService.EnableGoogleSheets(_googleSheetsConfig, username, group);
           await userProfileService.LoadFromGoogleSheetsAsync();

           var allLevels = GameManager.Instance.GetService<RuntimeLevelFactory>().GetAllLevelInfos();
           int userRow = await FindUserRowAsync(group, username);

           if (userRow > 0)
           {
               await SyncProgressFromSheetAsync(group, userRow, allLevels);
               Debug.Log($"[RegistrationService] Loaded progress for user '{username}' from sheet '{group}'");
           }
           else
           {
               Debug.LogWarning($"[RegistrationService] User '{username}' not found in sheet '{group}'");
           }
       }
       catch (System.Exception ex)
       {
           Debug.LogError($"[RegistrationService] Failed to load user progress on startup: {ex.Message}");
       }
   }

   
   // callback function to be called after registration is complete
  public async Task RegisterUserAsync(string username, string group, Action doneCallback)
  {
      if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(group))
      {
          Debug.LogError("Username and group cannot be empty");
          return;
      }
  
      
      //await LoadUserProgressOnStartupAsync();
      
      try
      {
          
          
          // A group is a shared sheet, so it is created only for the first user.
          if (!await _googleSheetsClient.SheetExistsAsync(group))
          {
              await CreateSheetAndHeader(group);
          }

          var existingRow = await FindUserRowAsync(group, username);
          int sheetRow;
          if (existingRow == -1)
          {
              var currentprogress = GameManager.Instance.GetService<StageCompletionService>().GetLevelREsult;
              var allLevels = GameManager.Instance.GetService<RuntimeLevelFactory>().GetAllLevelInfos();
              var progressDict = currentprogress.ToDictionary(p => p.Key, p => p.Value.ScorePercent);
              var newRow = new List<string> { username };
              foreach (var level in allLevels)
              {
                  newRow.Add(progressDict.TryGetValue(level.levelId, out var score)
                      ? score.ToString("0.#", CultureInfo.InvariantCulture)
                      : string.Empty);
              }

              var createdRow = await _googleSheetsClient.AppendRowAsync(group, newRow.Cast<object>().ToArray());
              sheetRow = createdRow.row;
          }
          else
          {
              // Для существующего пользователя источником истины является таблица.
              // Не перезаписываем её значениями из PlayerPrefs.
              sheetRow = existingRow;
           }

          var userProfileService = GameManager.Instance.GetService<UserProfileService>(); 
          userProfileService.EnableGoogleSheets(_googleSheetsConfig, username, group);
          await userProfileService.LoadFromGoogleSheetsAsync();

          var allRegisteredLevels = GameManager.Instance.GetService<RuntimeLevelFactory>().GetAllLevelInfos();
          await SyncProgressFromSheetAsync(group, sheetRow, allRegisteredLevels);
          
          PlayerPrefs.SetInt(SheetRowPreferenceKey, sheetRow);
          PlayerPrefs.SetString(SheetNamePreferenceKey, group);
          PlayerPrefs.Save();
          
          doneCallback?.Invoke();
      }
      catch (Exception ex)
      {
          Debug.LogError($"Registration failed: {ex.Message}");
      }
  }

  /// <summary>
  /// Загружает прогресс пользователя из Google Sheets и синхронизирует с StageCompletionService.
  /// Вызывается при повторной регистрации (когда пользователь уже есть в таблице).
  /// </summary>
  private async Task SyncProgressFromSheetAsync(string group, int userRow, List<ContentLoaderService.RuntimeLevelInfo> allLevels)
  {
      try
      {
          var stageCompletionService = GameManager.Instance?.GetService<StageCompletionService>();
          if (stageCompletionService == null)
          {
              Debug.LogWarning("[RegistrationService] StageCompletionService not found, cannot sync progress from sheet.");
              return;
          }

          // Получаем строку пользователя из Google Sheets
          var range = await _googleSheetsClient.GetRangeAsync(group, $"A{userRow}:Z{userRow}");
          if (range?.values == null || range.values.Length == 0)
          {
              Debug.LogWarning($"[RegistrationService] User row {userRow} not found in sheet {group}");
              return;
          }

          object[] userRowData = range.values[0];

          // Удаляем прежние локальные значения перед применением данных таблицы:
          // пустая ячейка в Sheets также должна означать отсутствие прогресса.
          foreach (var level in allLevels)
          {
              stageCompletionService.ClearLevelResult(level.levelId);
          }

          // Синхронизируем каждый уровень из таблицы в StageCompletionService
          for (int i = 0; i < allLevels.Count; i++)
          {
              // Колонка A (индекс 0) - имя пользователя
              // Колонка B (индекс 1) - первый уровень, и т.д.
              int cellIndex = i + 1;

              if (cellIndex < userRowData.Length && userRowData[cellIndex] != null)
              {
                  string cellValue = userRowData[cellIndex].ToString().Trim();
                  
                  if (!string.IsNullOrEmpty(cellValue) && float.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var score))
                  {
                      // Обновляем результат уровня в StageCompletionService
                      stageCompletionService.RecordLevelResult(allLevels[i].levelId, score);
                      Debug.Log($"[RegistrationService] Synced progress: {allLevels[i].levelId} = {score:F1}% from sheet");
                  }
              }
          }

          Debug.Log($"[RegistrationService] Successfully synced progress from Google Sheets");
      }
      catch (System.Exception ex)
      {
          Debug.LogError($"[RegistrationService] Failed to sync progress from sheet: {ex.Message}");
      }
  }

  public async Task CreateSheetAndHeader(string group)
  {
      await _googleSheetsClient.CreateSheetAsync(group);
      var levels = GameManager.Instance.GetService<RuntimeLevelFactory>().GetAllLevelInfos();
      List<string> levelColumns = new List<string>(); 
      levelColumns.Clear();
      levelColumns.Add("ФИО ПАЛОМНИКА");
      // get localisation service 
      var localizationService = GameManager.Instance.GetService<LocalizationService>();
      // map level nameKey to localized string\
      var localizedNames = levels.Select(l => localizationService.GetText(l.nameKey));
 
      levelColumns.AddRange(localizedNames);
      await _googleSheetsClient.AppendRowAsync(group,levelColumns.ToArray());
  }

  /// <summary>
  /// Stores a completed level score in the registered user's row.
  /// Динамически определяет колонку на основе позиции уровня в списке.
  /// Использует маппинг по levelId для корректного соответствия колонкам.
  /// </summary>
  public async Task SaveLevelResultAsync(string levelId, float scorePercent)
  {
      var allLevels = GameManager.Instance.GetService<RuntimeLevelFactory>().GetAllLevelInfos();
      
      // Создаем маппинг levelId -> индекс колонки (как при регистрации)
      var levelColumn = allLevels.FindIndex(l => l.levelId == levelId);
      
      if (levelColumn < 0)
      {
          Debug.LogWarning($"[RegistrationService] Level '{levelId}' not found in config.");
          return;
      }
      
      // Колонка B = индекс 0, C = индекс 1, и т.д.
      // +1 потому что колонка A - это имя пользователя
      string column = GetColumnLetter(levelColumn + 2);// +2 потому что колонка A - это имя пользователя, а индекс начинается с 0

      string username = PlayerPrefs.GetString(UsernamePreferenceKey);
      string group = PlayerPrefs.GetString(GroupPreferenceKey);
      if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(group))
      {
          Debug.LogWarning("[RegistrationService] Level result was not sent: the user is not registered.");
          return;
      }

      try
      {
          // Ищем строку пользователя в Google Sheets
          int row = await FindUserRowAsync(group, username);
          if (row < 1)
          {
              Debug.LogWarning($"[RegistrationService] User '{username}' was not found in group '{group}'.");
              return;
          }


          string cell = $"{column}{row}";
          await _googleSheetsClient.SetCellAsync(
              group,
              cell,
              scorePercent.ToString("0.#", CultureInfo.InvariantCulture));

          Debug.Log($"[RegistrationService] Saved {levelId} result ({scorePercent:F1}%) to {group}!{cell}.");
      }
      catch (Exception ex)
      {
          // Network errors must not prevent the local progress from being saved.
          Debug.LogError($"[RegistrationService] Failed to save {levelId} result: {ex.Message}");
      }
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
