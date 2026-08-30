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
          
          
          // A group is a shared sheet, so it is created only for the first user.
          if (!await _googleSheetsClient.SheetExistsAsync(group))
          {
              
              await _googleSheetsClient.CreateSheetAsync(group);
              var levels = GameManager.Instance.GetService<RuntimeLevelFactory>().GetAllLevelInfos();
              List<string> levelColumns = new List<string>();
              levelColumns.Add("Name");
              levelColumns.AddRange(levels.Select(l=>l.levelId));
              await _googleSheetsClient.AppendRowAsync(group,levelColumns.ToArray());  
          }

          var currentprogress = GameManager.Instance.GetService<StageCompletionService>().GetLevelREsult;
          var allLevels = GameManager.Instance.GetService<RuntimeLevelFactory>().GetAllLevelInfos();
          
          // Создаем словарь пройденных уровней для быстрого поиска
          var progressDict = currentprogress.ToDictionary(p => p.Key, p => p.Value.ScorePercent);
          
          // Проходим по всем уровням в правильном порядке
          var newRow = new List<string> { username };
          foreach (var level in allLevels)
          {
              if (progressDict.TryGetValue(level.levelId, out var score))
              {
                  newRow.Add(score.ToString("0.#", CultureInfo.InvariantCulture));
              }
              else
              {
                  newRow.Add(""); // Пустая ячейка для непройденного уровня
              }
          }
          
          var createdRow = await _googleSheetsClient.AppendRowAsync(group, newRow.Cast<object>().ToArray());
          PlayerPrefs.SetInt(SheetRowPreferenceKey, createdRow.row);
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
      string column = GetColumnLetter(levelColumn + 1);

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
      var range = await _googleSheetsClient.GetRangeAsync(group, "A:A");
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
