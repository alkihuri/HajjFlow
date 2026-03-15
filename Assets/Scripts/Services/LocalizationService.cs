using System;
using System.Collections.Generic;
using UnityEngine;
using HajjFlow.Data;
using HajjFlow.UI;

namespace HajjFlow.Services
{
    /// <summary>
    /// Manages localization: loads translations from a CSV file in Resources,
    /// stores the translation table, tracks the current language, and notifies
    /// registered UI text controllers when the language changes.
    /// </summary>
    public class LocalizationService
    {
        private const string CsvResourcePath = "localization";
        private const string PlayerPrefsKey = "SelectedLanguage";

        private readonly Dictionary<string, Dictionary<Language, string>> _table
            = new Dictionary<string, Dictionary<Language, string>>();

        private readonly HashSet<GameTextController> _registeredTexts
            = new HashSet<GameTextController>();

        private Language _currentLanguage;

        public Language CurrentLanguage => _currentLanguage;

        public event Action OnLanguageChanged;

        // ── Column header → Language mapping ─────────────────────────────────

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

        // ── Constructor ──────────────────────────────────────────────────────

        public LocalizationService()
        {
            LoadSavedLanguage();
            LoadCsv();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Changes the active language, refreshes all registered UI texts,
        /// and fires <see cref="OnLanguageChanged"/>.
        /// </summary>
        public void ChangeLanguage(Language language)
        {
            _currentLanguage = language;

            PlayerPrefs.SetInt(PlayerPrefsKey, (int)language);
            PlayerPrefs.Save();

            foreach (var text in _registeredTexts)
            {
                if (text != null)
                    text.UpdateText();
            }

            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Returns the translated string for <paramref name="key"/> in the current language.
        /// Falls back to the key itself when no translation is found.
        /// </summary>
        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key))
                return key;

            if (_table.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(_currentLanguage, out var value))
                    return value;
            }

            return key; // fallback
        }

        /// <summary>Registers a <see cref="GameTextController"/> so it is updated on language change.</summary>
        public void Register(GameTextController text)
        {
            if (text != null)
                _registeredTexts.Add(text);
        }

        /// <summary>Unregisters a <see cref="GameTextController"/>.</summary>
        public void Unregister(GameTextController text)
        {
            if (text != null)
                _registeredTexts.Remove(text);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void LoadSavedLanguage()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
                _currentLanguage = (Language)PlayerPrefs.GetInt(PlayerPrefsKey);
            else
                _currentLanguage = Language.Russian;
        }

        private void LoadCsv()
        {
            var asset = Resources.Load<TextAsset>(CsvResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[LocalizationService] localization.csv not found in Resources.");
                return;
            }

            ParseCsv(asset.text);
            Debug.Log($"[LocalizationService] Loaded {_table.Count} localization keys.");
        }

        private void ParseCsv(string csv)
        {
            var lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return;

            // Parse header to determine column indices
            var headers = SplitCsvLine(lines[0]);
            var columnIndices = new Dictionary<int, Language>();
            for (int i = 1; i < headers.Length; i++)
            {
                string col = headers[i].Trim().ToLower();
                if (ColumnToLanguage.TryGetValue(col, out var lang))
                    columnIndices[i] = lang;
            }

            // Parse data rows
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

        /// <summary>
        /// Splits a single CSV line by commas, respecting quoted fields that may
        /// contain commas themselves.
        /// </summary>
        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip the escaped quote
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
