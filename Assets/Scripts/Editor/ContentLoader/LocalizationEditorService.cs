#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using HajjFlow.Data;

namespace HajjFlow.Editor.ContentLoader
{
    /// <summary>
    /// Static editor-only service for localization operations.
    /// Replaces MonoBehaviour-dependent LocalizationServiceLoader with a pure static class.
    /// </summary>
    public static class LocalizationEditorService
    {
        private const string GoogleSheetsCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vTX5Wh2iYEJWMZNxQqDw0rroPUyiGnJglnAG2WdxfVkj3kYEGHF27bYV6roA6mMpLS-_247HpV7K7JS/pub?gid=1439558173&single=true&output=csv";

        private const string CsvResourcePath = "localization";

        /// <summary>Parsed localization table: key → (Language → text).</summary>
        private static Dictionary<string, Dictionary<Language, string>> _table
            = new Dictionary<string, Dictionary<Language, string>>();

        /// <summary>List of language columns detected from CSV header.</summary>
        private static List<string> _detectedLanguages = new List<string>();

        /// <summary>Raw CSV content currently loaded.</summary>
        private static string _rawCsv = "";

        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Column header → Language mapping (matches LocalizationService)
        private static readonly Dictionary<string, Language> ColumnToLanguage
            = new Dictionary<string, Language>
            {
                { "ru", Language.Russian },
                { "bs", Language.Bosnian },
                { "al", Language.Albanian },
                { "tr", Language.Turkish },
                { "ar", Language.Arabic },
                { "id", Language.Indonesian },
                { "en", Language.English }
            };

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Number of localization keys currently loaded.</summary>
        public static int KeyCount => _table.Count;

        /// <summary>Detected language column headers from the CSV.</summary>
        public static List<string> DetectedLanguages => _detectedLanguages;

        /// <summary>All loaded keys.</summary>
        public static IEnumerable<string> Keys => _table.Keys;

        /// <summary>
        /// Downloads the localization CSV from Google Sheets asynchronously.
        /// Shows a progress bar in the Editor during download.
        /// </summary>
        public static async Task LoadFromGoogleTableAsync()
        {
            Debug.Log("[LocalizationEditorService] Downloading from Google Sheets...");

            EditorUtility.DisplayProgressBar("Localization", "Downloading from Google Sheets...", 0.2f);

            try
            {
                string csvContent = await SharedHttpClient.GetStringAsync(GoogleSheetsCsvUrl);

                if (string.IsNullOrWhiteSpace(csvContent))
                {
                    Debug.LogError("[LocalizationEditorService] Downloaded CSV is empty.");
                    return;
                }

                EditorUtility.DisplayProgressBar("Localization", "Parsing CSV...", 0.7f);

                _rawCsv = csvContent;
                ParseCsv(csvContent);
                SaveCsvToResources(csvContent);

                Debug.Log($"[LocalizationEditorService] Loaded {_table.Count} keys from Google Sheets.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationEditorService] Google Sheets download failed: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Loads and parses a local CSV file.
        /// </summary>
        /// <param name="path">Absolute path to the CSV file.</param>
        /// <returns>True if successfully parsed.</returns>
        public static bool LoadFromCsv(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[LocalizationEditorService] CSV path is null or empty.");
                return false;
            }

            if (!File.Exists(path))
            {
                Debug.LogError($"[LocalizationEditorService] File not found: {path}");
                return false;
            }

            string csvContent;
            try
            {
                csvContent = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationEditorService] Failed to read CSV: {ex.Message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                Debug.LogError("[LocalizationEditorService] CSV file is empty.");
                return false;
            }

            try
            {
                _rawCsv = csvContent;
                ParseCsv(csvContent);
                Debug.Log($"[LocalizationEditorService] Loaded {_table.Count} keys from CSV.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationEditorService] Failed to parse CSV: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves the currently loaded raw CSV content to Resources/localization.csv.
        /// </summary>
        public static void SaveCsvToResources()
        {
            if (string.IsNullOrWhiteSpace(_rawCsv))
            {
                Debug.LogError("[LocalizationEditorService] No CSV content loaded to save.");
                return;
            }

            SaveCsvToResources(_rawCsv);
        }

        /// <summary>
        /// Returns the translation for a given key and language.
        /// </summary>
        public static string GetTranslation(string key, string languageCode)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(languageCode))
                return "";

            string code = languageCode.Trim().ToLower();
            if (!ColumnToLanguage.TryGetValue(code, out var lang))
                return $"[unknown language: {languageCode}]";

            if (_table.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(lang, out var value))
                    return value;
            }

            return $"[key not found: {key}]";
        }

        /// <summary>
        /// Validates whether a CSV file at the given path can be parsed.
        /// </summary>
        public static bool ValidateCsv(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            try
            {
                string content = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2) // at least header + one data row
                    return false;

                var headers = SplitCsvLine(lines[0]);
                // Must have at least key + one language column
                return headers.Length >= 2;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads from the default Resources/localization.csv if it exists.
        /// </summary>
        public static void LoadFromResources()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "localization.csv");
            if (File.Exists(path))
            {
                LoadFromCsv(path);
            }
            else
            {
                Debug.LogWarning("[LocalizationEditorService] Resources/localization.csv not found.");
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static void SaveCsvToResources(string csvContent)
        {
            string path = Path.Combine(Application.dataPath, "Resources", "localization.csv");

            try
            {
                string resourcesFolder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(resourcesFolder) && !Directory.Exists(resourcesFolder))
                    Directory.CreateDirectory(resourcesFolder);

                File.WriteAllText(path, csvContent, Encoding.UTF8);
                AssetDatabase.Refresh();
                Debug.Log("[LocalizationEditorService] Saved localization.csv to Resources.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationEditorService] Failed to save CSV: {ex.Message}");
            }
        }

        private static void ParseCsv(string csv)
        {
            _table.Clear();
            _detectedLanguages.Clear();

            var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                Debug.LogError("[LocalizationEditorService] CSV has no lines.");
                return;
            }

            var headers = SplitCsvLine(lines[0]);
            var columnIndices = new Dictionary<int, Language>();

            for (int i = 1; i < headers.Length; i++)
            {
                string col = headers[i].Trim().ToLower();
                if (ColumnToLanguage.TryGetValue(col, out var lang))
                {
                    columnIndices[i] = lang;
                    _detectedLanguages.Add(col);
                }
            }

            for (int row = 1; row < lines.Length; row++)
            {
                var cols = SplitCsvLine(lines[row]);
                if (cols.Length == 0)
                    continue;

                string key = cols[0].Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                var translations = new Dictionary<Language, string>();
                foreach (var kvp in columnIndices)
                {
                    string value = kvp.Key < cols.Length ? cols[kvp.Key].Trim() : "";
                    translations[kvp.Value] = value;
                }

                _table[key] = translations;
            }
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
#endif
