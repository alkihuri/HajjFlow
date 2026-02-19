# Manasik – Unity Project Setup Guide

> **Manasik** is a gamified Hajj-learning mobile application built with Unity (C# / URP).  
> This document walks you through creating the four required scenes and wiring up the
> placeholder UI prefabs from scratch.

---

## 1. Unity Version & Project Settings

| Setting | Value |
|---------|-------|
| Unity version | 2022.3 LTS (or newer) |
| Render pipeline | Universal Render Pipeline (URP) |
| Target platforms | Android / iOS |
| Scripting backend | IL2CPP |
| API compatibility | .NET Standard 2.1 |
| TextMeshPro | Required (install via Package Manager) |

> After opening the project in Unity, import **TextMeshPro Essentials** when prompted.

---

## 2. Folder Structure

```
Assets/
├── ScriptableObjects/
│   └── Levels/
│       └── Level_Preparation.asset   ← sample level (edit GUID after import)
└── Scripts/
    ├── Manasik.Scripts.asmdef
    ├── Core/
    │   ├── GameManager.cs
    │   └── LevelManager.cs
    ├── Data/
    │   ├── LevelData.cs              ← ScriptableObject definition
    │   ├── QuizQuestion.cs
    │   └── UserProfile.cs
    ├── Gameplay/
    │   ├── QuizSystem.cs
    │   └── RewardSystem.cs
    ├── Services/
    │   ├── ProgressService.cs
    │   └── UserProfileService.cs
    └── UI/
        ├── GameplayUI.cs
        ├── LevelSelectionUI.cs
        ├── LevelTileUI.cs
        ├── MainMenuUI.cs
        └── ResultsUI.cs
```

---

## 3. Build Settings – Scene Order

Add the following scenes in **File → Build Settings** (order matters for `SceneManager`):

| Index | Scene name | File path |
|-------|-----------|-----------|
| 0 | `MainMenu` | `Assets/Scenes/MainMenu.unity` |
| 1 | `LevelSelection` | `Assets/Scenes/LevelSelection.unity` |
| 2 | `Gameplay` | `Assets/Scenes/Gameplay.unity` |
| 3 | `Results` | `Assets/Scenes/Results.unity` |

---

## 4. Persistent GameManager GameObject

1. Create an **empty scene** (or open `MainMenu`).
2. Create an empty GameObject → rename to **`_GameManager`**.
3. Attach `GameManager` and `LevelManager` scripts.
4. `GameManager.Awake()` calls `DontDestroyOnLoad` automatically – no extra setup needed.

---

## 5. Scene: MainMenu

### Hierarchy
```
MainMenu (scene)
└── _GameManager          ← GameManager + LevelManager (see §4)
└── MainMenuCanvas (Canvas, ScreenSpace-Overlay)
    ├── Background (Image – full-screen)
    ├── TitleText (TextMeshProUGUI) → "Manasik"
    ├── GreetingText (TextMeshProUGUI) → populated at runtime
    ├── PlayButton (Button > TMP_Text "Play")
    └── SettingsButton (Button > TMP_Text "Settings")
```

### Component wiring – `MainMenuUI`
| Field | Target object |
|-------|--------------|
| `_titleText` | `TitleText` |
| `_greetingText` | `GreetingText` |
| `_playButton` | `PlayButton` |
| `_settingsButton` | `SettingsButton` |

---

## 6. Scene: LevelSelection (2.5D Isometric Placeholder)

### Hierarchy
```
LevelSelection (scene)
└── LevelSelectionCanvas (Canvas)
    ├── BackButton (Button > TMP_Text "← Back")
    ├── TitleText (TextMeshProUGUI) → "Choose Your Journey"
    └── LevelGrid (ScrollRect > Content)   ← LevelButtonsContainer
```

### LevelButtonPrefab  (`Assets/Prefabs/LevelTilePrefab.prefab`)
```
LevelTilePrefab (Button)
├── Thumbnail (Image)
├── LevelNameText (TextMeshProUGUI)
├── ProgressText (TextMeshProUGUI)
└── CompletedBadge (Image – star icon, hidden by default)
```
Attach **`LevelTileUI`** to the root of `LevelTilePrefab`.  
Wire `_selectButton` to the root Button component.

### Component wiring – `LevelSelectionUI`
| Field | Target |
|-------|--------|
| `_levelButtonsContainer` | `LevelGrid/Content` Transform |
| `_levelButtonPrefab` | `LevelTilePrefab` prefab |
| `_backButton` | `BackButton` |
| `_levels` | Drag `Level_Preparation.asset` (and future assets) here |

---

## 7. Scene: Gameplay

### Hierarchy
```
Gameplay (scene)
└── Systems
    ├── QuizSystem (empty GO + QuizSystem script)
    └── RewardSystem (empty GO + RewardSystem script)
└── GameplayCanvas (Canvas)
    ├── TopBar (HorizontalLayoutGroup)
    │   ├── LevelNameText (TextMeshProUGUI) – left
    │   ├── ProgressText (TextMeshProUGUI)  – center
    │   └── GemsText (TextMeshProUGUI)      – right
    ├── QuizPanel
    │   ├── QuestionText (TextMeshProUGUI)
    │   ├── OptionsGroup (VerticalLayoutGroup)
    │   │   ├── OptionButton_A (Button > TMP_Text)
    │   │   ├── OptionButton_B (Button > TMP_Text)
    │   │   ├── OptionButton_C (Button > TMP_Text)
    │   │   └── OptionButton_D (Button > TMP_Text)
    │   └── FeedbackText (TextMeshProUGUI – hidden initially)
    └── BottomBar
        ├── BackButton (Button > TMP_Text "← Back")
        ├── RestartButton (Button > TMP_Text "↺ Restart")
        └── NextButton (Button > TMP_Text "Next →")
```

### Component wiring – `GameplayUI` (attach to `GameplayCanvas`)
| Field | Target |
|-------|--------|
| `_levelNameText` | `TopBar/LevelNameText` |
| `_progressText` | `TopBar/ProgressText` |
| `_gemsText` | `TopBar/GemsText` |
| `_questionText` | `QuizPanel/QuestionText` |
| `_optionButtons` (array, size 4) | `OptionButton_A` … `OptionButton_D` |
| `_feedbackText` | `QuizPanel/FeedbackText` |
| `_nextButton` | `BottomBar/NextButton` |
| `_backButton` | `BottomBar/BackButton` |
| `_restartButton` | `BottomBar/RestartButton` |
| `_quizSystem` | `Systems/QuizSystem` |
| `_rewardSystem` | `Systems/RewardSystem` |

---

## 8. Scene: Results

### Hierarchy
```
Results (scene)
└── ResultsCanvas (Canvas)
    ├── LevelNameText (TextMeshProUGUI)
    ├── ScoreText (TextMeshProUGUI)
    ├── GemsEarnedText (TextMeshProUGUI)
    ├── PassFailText (TextMeshProUGUI)
    ├── NextButton (Button > TMP_Text "Next Level →")
    ├── RestartButton (Button > TMP_Text "↺ Try Again")
    └── BackButton (Button > TMP_Text "← Levels")
```

### Component wiring – `ResultsUI` (attach to `ResultsCanvas`)
| Field | Target |
|-------|--------|
| `_levelNameText` | `LevelNameText` |
| `_scoreText` | `ScoreText` |
| `_gemsEarnedText` | `GemsEarnedText` |
| `_passFailText` | `PassFailText` |
| `_nextButton` | `NextButton` |
| `_restartButton` | `RestartButton` |
| `_backButton` | `BackButton` |

---

## 9. LevelData ScriptableObject

A sample asset is provided at `Assets/ScriptableObjects/Levels/Level_Preparation.asset`.

> **⚠️ Important – GUID re-linking after import**
> The `.asset` file contains a placeholder script GUID (`0000000000000000000000000000BEEF`).
> Unity assigns a real GUID to `LevelData.cs` when the project is first opened.
> If the asset shows a **"Missing Script"** warning, follow these steps:
> 1. In the Project window, select `Level_Preparation.asset`.
> 2. In the Inspector, click the script reference field and assign `LevelData`.
> 3. Alternatively, open the `.asset` file in a text editor and replace the placeholder
>    GUID with the value found in `Assets/Scripts/Data/LevelData.cs.meta`.

To create additional levels:
1. Right-click in the Project window → **Create → Manasik → Level Data**.
2. Fill in `LevelId` (must be unique), `LevelName`, `Description`, and `PassThreshold`.
3. Add `QuizQuestion` entries in the `Questions` array.
4. Drag the new asset into the `_levels` array on `LevelSelectionUI`.

---

## 10. Mobile Build Checklist

- [ ] Set **Company Name** and **Product Name** in Project Settings.
- [ ] Configure **Android** or **iOS** module in Build Settings.
- [ ] Set screen orientation to **Portrait** (Player Settings → Default Orientation).
- [ ] Enable **IL2CPP** scripting backend for release builds.
- [ ] Test on a physical device: the persistent data directory path differs per platform.
- [ ] Replace placeholder sprites in `LevelTilePrefab` with real artwork before shipping.

---

## 11. Extending the Project

| Feature | Where to add |
|---------|-------------|
| More levels | New `LevelData` asset + drag into `_levels` |
| Audio feedback | `QuizSystem.OnAnswerResult` event |
| Animations | `GameplayUI` — react to `OnQuestionReady` |
| Leaderboard | New `LeaderboardService` in `/Services` |
| Analytics | Hook into `ProgressService.RecordLevelProgress` |
| Localisation | Unity Localization package; wrap all `TMP_Text.text` assignments |
