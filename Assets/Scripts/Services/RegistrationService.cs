using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using GSheetsCommander;
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
          var newRow = new string[] { username, "", "", "" };
          
          // A group is a shared sheet, so it is created only for the first user.
          if (!await _googleSheetsClient.SheetExistsAsync(group))
              await _googleSheetsClient.CreateSheetAsync(group);

          var createdRow = await _googleSheetsClient.AppendRowAsync(group, newRow);
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
  /// Columns B, C and D contain Warmup, Miqat and Tawaf scores respectively.
  /// </summary>
  public async Task SaveLevelResultAsync(string levelId, float scorePercent)
  {
      if (!TryGetLevelColumn(levelId, out string column))
      {
          Debug.LogWarning($"[RegistrationService] No Google Sheets column configured for level '{levelId}'.");
          return;
      }

      string username = PlayerPrefs.GetString(UsernamePreferenceKey);
      string group = PlayerPrefs.GetString(GroupPreferenceKey);
      if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(group))
      {
          Debug.LogWarning("[RegistrationService] Level result was not sent: the user is not registered.");
          return;
      }

      try
      {
          int row = GetRegisteredRow(group);
          if (row < 1)
          {
              row = await FindUserRowAsync(group, username);
              if (row < 1)
              {
                  Debug.LogWarning($"[RegistrationService] User '{username}' was not found in group '{group}'.");
                  return;
              }

              PlayerPrefs.SetInt(SheetRowPreferenceKey, row);
              PlayerPrefs.SetString(SheetNamePreferenceKey, group);
              PlayerPrefs.Save();
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

  private static bool TryGetLevelColumn(string levelId, out string column)
  {
      switch (levelId)
      {
          case "Warmup": column = "B"; return true;
          case "Miqat": column = "C"; return true;
          case "Tawaf": column = "D"; return true;
          default: column = null; return false;
      }
  }

  private static int GetRegisteredRow(string group)
  {
      return PlayerPrefs.GetString(SheetNamePreferenceKey) == group
          ? PlayerPrefs.GetInt(SheetRowPreferenceKey, -1)
          : -1;
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
