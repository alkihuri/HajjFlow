#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using HajjFlow.Data;
using Core.Theory;

namespace HajjFlow.Editor.ContentLoader
{
    /// <summary>
    /// Unity Editor Tool — Content Loader window built with UI Toolkit.
    /// Provides three panels: Questions, Theory Cards, and Localization.
    /// Menu: Tools / Hajj / Content Loader
    /// </summary>
    public class ContentLoaderWindow : EditorWindow
    {
        // ── Cached data references ───────────────────────────────────────────

        private LevelData _levelData;
        private TheoryCardContainer _theoryContainer;
        private string _csvPath = "";
        private string _searchKey = "";
        private string _selectedLanguage = "";

        // ── Menu item ────────────────────────────────────────────────────────

        [MenuItem("Tools/Hajj/Content Loader")]
        public static void ShowWindow()
        {
            var window = GetWindow<ContentLoaderWindow>();
            window.titleContent = new GUIContent("Content Loader");
            window.minSize = new Vector2(420, 500);
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable()
        {
            // Try to load existing assets on open
            _levelData = FindFirstAsset<LevelData>();
            _theoryContainer = FindFirstAsset<TheoryCardContainer>();
            LocalizationEditorService.LoadFromResources();
            RebuildUI();
        }

        // ── Full UI rebuild ──────────────────────────────────────────────────

        private void RebuildUI()
        {
            rootVisualElement.Clear();

            // Root padding
            rootVisualElement.style.paddingTop = 4;
            rootVisualElement.style.paddingBottom = 4;
            rootVisualElement.style.paddingLeft = 6;
            rootVisualElement.style.paddingRight = 6;

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;

            scrollView.Add(BuildQuestionsPanel());
            scrollView.Add(BuildTheoryPanel());
            scrollView.Add(BuildLocalizationPanel());

            rootVisualElement.Add(scrollView);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PANEL 1 — QUESTIONS
        // ═════════════════════════════════════════════════════════════════════

        private VisualElement BuildQuestionsPanel()
        {
            var foldout = new Foldout { text = "1. Questions (LevelData)", value = true };
            foldout.style.marginBottom = 8;

            // ── Buttons row ─────────────────────────────────────────────
            var buttonsRow = Row();

            var loadBtn = new Button(() => OnLoadQuestionsJson()) { text = "Load JSON" };
            loadBtn.style.flexGrow = 1;
            buttonsRow.Add(loadBtn);

            var openBtn = new Button(() => OnOpenQuestionsFile()) { text = "Open Current File" };
            openBtn.style.flexGrow = 1;
            buttonsRow.Add(openBtn);

            foldout.Add(buttonsRow);

            // ── Stats ───────────────────────────────────────────────────
            if (_levelData != null)
            {
                int qCount = _levelData.QuestionCount;
                int aCount = DataLoader.CountTotalAnswers(_levelData);

                foldout.Add(MakeLabel($"Level: {_levelData.LevelName}  (ID: {_levelData.LevelId})"));
                foldout.Add(MakeLabel($"Questions: {qCount}   |   Total answers: {aCount}"));

                // ── Hierarchical dropdown ────────────────────────────────
                if (_levelData.Questions != null && _levelData.Questions.Length > 0)
                {
                    var choices = new List<string>();
                    for (int qi = 0; qi < _levelData.Questions.Length; qi++)
                    {
                        var q = _levelData.Questions[qi];
                        string qText = TruncateText(q.QuestionText, 50);
                        choices.Add($"Q{qi + 1}: {qText}");

                        if (q.Options != null)
                        {
                            for (int ai = 0; ai < q.Options.Length; ai++)
                            {
                                string mark = ai == q.CorrectAnswerIndex ? " ✓" : "";
                                string aText = TruncateText(q.Options[ai], 45);
                                choices.Add($"   A{ai + 1}: {aText}{mark}");
                            }
                        }
                    }

                    var dropdown = new DropdownField("Structure", choices, 0);
                    dropdown.style.marginTop = 4;
                    foldout.Add(dropdown);
                }
            }
            else
            {
                foldout.Add(MakeLabel("No LevelData loaded."));
            }

            return foldout;
        }

        private void OnLoadQuestionsJson()
        {
            string path = EditorUtility.OpenFilePanel("Select JSON Quiz File", "Assets", "json");
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("[ContentLoader] File selection cancelled.");
                return;
            }

            var result = DataLoader.LoadQuestionsFromJson(path);
            if (result != null)
                _levelData = result;

            RebuildUI();
        }

        private void OnOpenQuestionsFile()
        {
            if (_levelData == null)
            {
                Debug.LogWarning("[ContentLoader] No LevelData loaded to open.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(_levelData);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string fullPath = Path.GetFullPath(assetPath);
                Application.OpenURL(new Uri(fullPath).AbsoluteUri);
                Debug.Log($"[ContentLoader] Opened: {fullPath}");
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PANEL 2 — THEORY CARDS
        // ═════════════════════════════════════════════════════════════════════

        private VisualElement BuildTheoryPanel()
        {
            var foldout = new Foldout { text = "2. Theory Cards (TheoryCardContainer)", value = true };
            foldout.style.marginBottom = 8;

            // ── Buttons row ─────────────────────────────────────────────
            var buttonsRow = Row();

            var loadBtn = new Button(() => OnLoadTheoryJson()) { text = "Load JSON" };
            loadBtn.style.flexGrow = 1;
            buttonsRow.Add(loadBtn);

            var openBtn = new Button(() => OnOpenTheoryFile()) { text = "Open Current File" };
            openBtn.style.flexGrow = 1;
            buttonsRow.Add(openBtn);

            foldout.Add(buttonsRow);

            // ── Stats ───────────────────────────────────────────────────
            if (_theoryContainer != null)
            {
                int cardCount = _theoryContainer.Cards?.Count ?? 0;

                foldout.Add(MakeLabel($"Container: {_theoryContainer.name}  (Level: {_theoryContainer.LevelId})"));
                foldout.Add(MakeLabel($"Theory cards: {cardCount}"));

                // ── Hierarchical dropdown ────────────────────────────────
                if (_theoryContainer.Cards != null && _theoryContainer.Cards.Count > 0)
                {
                    var choices = new List<string>();
                    for (int i = 0; i < _theoryContainer.Cards.Count; i++)
                    {
                        var card = _theoryContainer.Cards[i];
                        if (card == null) continue;
                        string title = TruncateText(card.Title, 45);
                        string desc = TruncateText(card.Description, 40);
                        choices.Add($"Card {i + 1}: {title}");
                        choices.Add($"   Desc: {desc}");
                    }

                    if (choices.Count > 0)
                    {
                        var dropdown = new DropdownField("Cards", choices, 0);
                        dropdown.style.marginTop = 4;
                        foldout.Add(dropdown);
                    }
                }
            }
            else
            {
                foldout.Add(MakeLabel("No TheoryCardContainer loaded."));
            }

            return foldout;
        }

        private void OnLoadTheoryJson()
        {
            string path = EditorUtility.OpenFilePanel("Select Theory JSON File", "Assets", "json");
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("[ContentLoader] File selection cancelled.");
                return;
            }

            var result = DataLoader.LoadTheoryFromJson(path);
            if (result != null)
                _theoryContainer = result;

            RebuildUI();
        }

        private void OnOpenTheoryFile()
        {
            if (_theoryContainer == null)
            {
                Debug.LogWarning("[ContentLoader] No TheoryCardContainer loaded to open.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(_theoryContainer);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string fullPath = Path.GetFullPath(assetPath);
                Application.OpenURL(new Uri(fullPath).AbsoluteUri);
                Debug.Log($"[ContentLoader] Opened: {fullPath}");
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PANEL 3 — LOCALIZATION
        // ═════════════════════════════════════════════════════════════════════

        private VisualElement BuildLocalizationPanel()
        {
            var foldout = new Foldout { text = "3. Localization (CSV)", value = true };
            foldout.style.marginBottom = 8;

            // ── CSV path field ──────────────────────────────────────────
            var pathRow = Row();

            var pathField = new TextField("CSV Path") { value = _csvPath };
            pathField.style.flexGrow = 1;

            bool isValid = !string.IsNullOrEmpty(_csvPath)
                           && LocalizationEditorService.ValidateCsv(_csvPath);

            // Green border if valid, red if invalid (only when a path is entered)
            if (!string.IsNullOrEmpty(_csvPath))
            {
                var borderColor = isValid
                    ? new Color(0.2f, 0.8f, 0.2f)
                    : new Color(0.9f, 0.2f, 0.2f);
                pathField.style.borderBottomColor = borderColor;
                pathField.style.borderTopColor = borderColor;
                pathField.style.borderLeftColor = borderColor;
                pathField.style.borderRightColor = borderColor;
                pathField.style.borderBottomWidth = 2;
                pathField.style.borderTopWidth = 2;
                pathField.style.borderLeftWidth = 2;
                pathField.style.borderRightWidth = 2;
            }

            pathField.RegisterValueChangedCallback(evt =>
            {
                _csvPath = evt.newValue;
                RebuildUI();
            });
            pathRow.Add(pathField);

            var browseBtn = new Button(() =>
            {
                string path = EditorUtility.OpenFilePanel("Select CSV File", "Assets", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    _csvPath = path;
                    RebuildUI();
                }
            })
            { text = "..." };
            browseBtn.style.width = 30;
            pathRow.Add(browseBtn);

            foldout.Add(pathRow);

            // ── Action buttons ──────────────────────────────────────────
            var btnRow = Row();

            var loadCsvBtn = new Button(() => OnLoadCsv()) { text = "Load CSV" };
            loadCsvBtn.style.flexGrow = 1;
            btnRow.Add(loadCsvBtn);

            var reloadBtn = new Button(() => OnReloadLocalization()) { text = "Reload Localization" };
            reloadBtn.style.flexGrow = 1;
            btnRow.Add(reloadBtn);

            var googleBtn = new Button(() => OnLoadFromGoogle()) { text = "Load from Google Sheets" };
            googleBtn.style.flexGrow = 1;
            btnRow.Add(googleBtn);

            foldout.Add(btnRow);

            var saveBtn = new Button(() => OnSaveCsvToResources()) { text = "Save CSV to Resources" };
            saveBtn.style.marginTop = 2;
            foldout.Add(saveBtn);

            // ── Stats ───────────────────────────────────────────────────
            foldout.Add(MakeLabel($"Total keys: {LocalizationEditorService.KeyCount}"));

            // ── Languages dropdown ──────────────────────────────────────
            var languages = LocalizationEditorService.DetectedLanguages;
            if (languages.Count > 0)
            {
                if (string.IsNullOrEmpty(_selectedLanguage) || !languages.Contains(_selectedLanguage))
                    _selectedLanguage = languages[0];

                var langDropdown = new DropdownField("Language", languages, _selectedLanguage);
                langDropdown.RegisterValueChangedCallback(evt =>
                {
                    _selectedLanguage = evt.newValue;
                    RebuildUI();
                });
                foldout.Add(langDropdown);
            }

            // ── Search ──────────────────────────────────────────────────
            var searchField = new TextField("Search key") { value = _searchKey };
            searchField.RegisterValueChangedCallback(evt =>
            {
                _searchKey = evt.newValue;
                RebuildUI();
            });
            foldout.Add(searchField);

            if (!string.IsNullOrEmpty(_searchKey) && !string.IsNullOrEmpty(_selectedLanguage))
            {
                string translation = LocalizationEditorService.GetTranslation(_searchKey, _selectedLanguage);
                var resultField = new TextField("Translation") { value = translation };
                resultField.SetEnabled(false);
                foldout.Add(resultField);
            }

            return foldout;
        }

        private void OnLoadCsv()
        {
            if (string.IsNullOrEmpty(_csvPath))
            {
                string path = EditorUtility.OpenFilePanel("Select CSV File", "Assets", "csv");
                if (string.IsNullOrEmpty(path)) return;
                _csvPath = path;
            }

            LocalizationEditorService.LoadFromCsv(_csvPath);
            RebuildUI();
        }

        private void OnReloadLocalization()
        {
            if (!string.IsNullOrEmpty(_csvPath) && File.Exists(_csvPath))
            {
                LocalizationEditorService.LoadFromCsv(_csvPath);
            }
            else
            {
                LocalizationEditorService.LoadFromResources();
            }
            RebuildUI();
        }

        private async void OnLoadFromGoogle()
        {
            await LocalizationEditorService.LoadFromGoogleTableAsync();
            RebuildUI();
        }

        private void OnSaveCsvToResources()
        {
            LocalizationEditorService.SaveCsvToResources();
            RebuildUI();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  UI HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 2;
            row.style.marginBottom = 2;
            return row;
        }

        private static Label MakeLabel(string text)
        {
            var label = new Label(text);
            label.style.marginTop = 2;
            label.style.marginBottom = 2;
            return label;
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "(empty)";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "…";
        }

        private static T FindFirstAsset<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
#endif
