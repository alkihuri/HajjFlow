<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3+-black?logo=unity&logoColor=white" alt="Unity"/>
  <img src="https://img.shields.io/badge/C%23-10-239120?logo=csharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/Platform-WebGL-blue" alt="WebGL"/>
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License"/>
  <img src="https://img.shields.io/badge/Languages-7-orange" alt="7 Languages"/>
</p>

<h1 align="center">🕋 HajjFlow</h1>

<p align="center">
  <b>An interactive educational game that teaches pilgrims the fundamentals of Hajj</b><br/>
  <i>Learn. Practice. Be prepared.</i>
</p>

<p align="center">
  Immersive 2.5D environments · Guided simulations · Quizzes · Audio learning<br/>
  Step by step — every ritual before its real performance.
</p>

> 🇷🇺 [Версия на русском (README_RUS.md)](README_RUS.md)

---

## 📑 Table of Contents

- [Project Overview](#-project-overview)
- [Architecture](#-architecture)
  - [Service Locator & Bootstrapper](#-service-locator--bootstrapper)
  - [Finite State Machine (FSM)](#-finite-state-machine-fsm)
  - [Level Lifecycle](#-level-lifecycle)
  - [Localization](#-localization)
  - [Data Persistence](#-data-persistence)
  - [Quiz System](#-quiz-system)
  - [Reward System](#-reward-system)
- [All Services](#-all-services)
- [Project Structure](#-project-structure)
- [Scenes](#-scenes)
- [Prefabs](#-prefabs)
- [Script Descriptions](#-script-descriptions)
- [Level Rewards](#-level-rewards)
- [Architectural Patterns](#-architectural-patterns)
- [Quick Start](#-quick-start)

---

## 🎯 Project Overview

**HajjFlow** is a Unity WebGL application that teaches pilgrims the fundamentals of Hajj. Architecture: **Service Locator + State Machine**.

| Feature | Value |
|---|---|
| **Levels** | 3 (Warmup → Miqat → Tawaf) |
| **Languages** | 7 (🇷🇺 RU · 🇧🇦 BS · 🇦🇱 AL · 🇹🇷 TR · 🇸🇦 AR · 🇮🇩 ID · 🇬🇧 EN) |
| **Scenes** | 4 (MainMenu + 3 levels) |
| **C# Scripts** | 65+ |
| **Prefabs** | 11 |
| **Platform** | WebGL, Editor |

**Game Loop:** Menu → Level Select → Theory (cards) → Quiz → Results → Next Level.

---

## 🏗 Architecture

### 🔌 Service Locator & Bootstrapper

**Service Locator** pattern via the `GameManager` singleton:

```
┌─ BOOTSTRAP (startup order) ────────────────────────────────────────────┐
│                                                                         │
│  1. GameManager.Awake()  [ExecutionOrder = 0]                          │
│     └── Singleton + DontDestroyOnLoad                                  │
│                                                                         │
│  2. Bootstrapper.Awake()  [ExecutionOrder = 100]                       │
│     ├── RegisterServices(gm):                                          │
│     │   ├── AudioService           (MonoBehaviour, Inspector)          │
│     │   ├── ProfileLoaderService   (new, Plain C#)                     │
│     │   ├── LocalizationService    (new, Plain C#)                     │
│     │   ├── UserProfileService     (new, Plain C#)                     │
│     │   ├── ProgressService        (new, depends on UserProfile)       │
│     │   ├── UIService              (MonoBehaviour, Inspector)          │
│     │   ├── StageCompletionService (MonoBehaviour, Inspector)          │
│     │   ├── QuizService            (MonoBehaviour, Inspector)          │
│     │   └── GameStateMachine       (MonoBehaviour, Inspector)          │
│     ├── VerifyServices() → check all services                          │
│     └── ChangeState("main_menu") → start                               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**`GameManager`** (`Core/GameManager.cs`) — central singleton:
- `RegisterService<T>()` / `GetService<T>()` / `HasService<T>()` — registration & access
- `AddGems(amount)` — award gems
- Backward-compatible properties: `ProfileService`, `ProgressService`, `uiService`, etc.

**`Bootstrapper`** (`Core/Bootstrapper.cs`) — entry point `[DefaultExecutionOrder(100)]`:
- MonoBehaviour services via Inspector
- Plain C# services created in code

---

### 🔄 Finite State Machine (FSM)

All game logic is driven by **`GameStateMachine`**:

```
                    ┌──────────────┐
                    │  main_menu   │ ← entry point
                    │ MainMenuState│
                    └──────┬───────┘
                           │ Start
                           ▼
                    ┌──────────────┐
                    │ level_select │
                    │LevelSelectSt.│
                    └──────┬───────┘
                           │ Select level
              ┌────────────┼────────────┐
              ▼            ▼            ▼
       ┌─────────┐  ┌─────────┐  ┌─────────┐
       │ Warmup  │  │  Miqat  │  │  Tawaf  │
       │3 blocks │  │2 blocks │  │2 blocks │
       └────┬────┘  └────┬────┘  └────┬────┘
            │            │            │
            └────────────┼────────────┘
                         │ CompleteLevel(score%)
                         ▼
                  ┌──────────────┐
                  │   results    │
                  │ ResultsState │
                  └──────┬───────┘
                    ┌────┴────┐
                    ▼         ▼
               Replay    Level Select
```

**States:**

| Class | StateId | Description |
|---|---|---|
| `BaseGameState` | — | Abstract base: `Enter()`, `Update()`, `Exit()`, `OnPause()`, `OnResume()` |
| `MainMenuState` | `"main_menu"` | Start screen |
| `LevelSelectState` | `"level_select"` | Level selection, progress |
| `BaseLevelState` | — | Base for levels: theory → quiz → progress |
| `WarmupLevelState` | `"Warmup"` | 3 theory blocks, no bonuses |
| `MiqatLevelState` | `"Miqat"` | 2 theory blocks, speed & excellence bonuses |
| `TawafLevelState` | `"Tawaf"` | 2 theory blocks, streak bonuses & laps |
| `ResultsState` | `"results"` | Score, gems, Next / Replay |

**Controls:**
- `ChangeState(stateId)` / `ChangeState(stateId, levelData)` — transitions
- `CompleteLevel(scorePercent)` — completion → `OnLevelCompleted` → Results
- `ReplayCurrentLevel()` — replay
- `Pause()` / `Resume()` — via `Time.timeScale`

**Identifiers** in `GameStateIds` (`LevelStateIds.cs`):
```csharp
GameStateIds.MainMenu    = "main_menu"
GameStateIds.LevelSelect = "level_select"
GameStateIds.Results     = "results"
GameStateIds.Warmup      = "Warmup"
GameStateIds.Miqat       = "Miqat"
GameStateIds.Tawaf       = "Tawaf"
```

---

### 📚 Level Lifecycle

`BaseLevelState` defines a unified lifecycle for all levels:

```
  Enter()
    │
    ├── ResetLevelState()            // Reset counters
    ├── ShowLevelUI()                // Abstract — each level has its own UI
    ├── FindObjectOfType<QuizSystem>
    ├── FindObjectOfType<RewardSystem>
    ├── Subscribe(QuizSystem events)
    └── QuizSystem.Initialise(levelData)
         │
         ▼
  Theory phase: CompleteTheoryStage() × N
    │  TheoryBlockCount: Warmup = 3, Miqat = 2, Tawaf = 2
    │
    ▼  When _currentStageIndex >= TheoryBlockCount
  StartQuiz()
    │
    ▼
  Question loop:
    HandleQuestionReady(question, num)
    HandleAnswerResult(wasCorrect)
    │
    ▼
  HandleQuizComplete(scorePercent)
    │  → _stateMachine.CompleteLevel(%)
    │
    ▼
  Exit()
    ├── SaveProgress()
    └── UnsubscribeQuizEvents()
```

---

### 🌍 Localization

Custom CSV-based system supporting 7 languages:

```
┌─ SOURCES ───────────────────────────────────────────────────────────────┐
│  Primary:  Google Sheets (public CSV)                                   │
│  Fallback: Resources/localization.csv                                   │
└─────────────────────────────────────────────────────────────────────────┘

  CSV: key | ru | bs | al | tr | ar | id | en
```

| Component | Role |
|---|---|
| `Language` (enum) | `Russian, Bosnian, Albanian, Turkish, Arabic, Indonesian, English` |
| `LocalizationService` | Load CSV, `_table[key][language]`, switch language, notify UI |
| `GameTextController` | Auto-update TMP_Text on language change |
| `LanguageSelectController` | Language selection UI |

**Flow:**
```
Constructor → LoadSavedLanguage() → LoadCsv() → ParseCsv()
ChangeLanguage() → update all GameTextControllers → OnLanguageChanged
GetText(key) → translation or key as fallback
```

---

### 💾 Data Persistence

Multi-layer saving with the **Strategy** pattern:

```
┌─ ProfileLoaderService ───────────────────────────────────────────────────┐
│  Priority: Backend (100) → PlayerPrefs (50) → File (10)                 │
│  Load() → first successful provider → cache                             │
│  Save() → all local; SaveAsync() → all including Backend                │
└──────────────────────────────────────────────────────────────────────────┘

┌─ UserProfileService ─────────────────────────────────────────────────────┐
│  GetProfile() → load / return UserProfile                                │
│  UpdateProfile() → modify + save                                         │
└──────────────────────────────────────────────────────────────────────────┘

┌─ UserProfile ────────────────────────────────────────────────────────────┐
│  FirstName, LastName, TotalProgress, Gems, CompletedLevelIds,            │
│  LevelProgress (SerializableDictionary)                                  │
└──────────────────────────────────────────────────────────────────────────┘
```

---

### ❓ Quiz System

Two components:

| System | Role |
|---|---|
| `QuizSystem` (scene) | Execution: `Initialise()`, `SubmitAnswer()`, `Advance()`, events `OnQuestionReady`, `OnAnswerResult`, `OnQuizComplete` |
| `QuizService` (service) | Management: `InitializeQuiz()`, `SubmitAnswer()`, `MoveToNextQuestion()`, `GetLastScorePercent()` |

`BaseLevelState` subscribes to `QuizSystem` and initializes `QuizService`. Final score comes from `QuizService.GetLastScorePercent()`.

---

### 🏆 Reward System

`RewardSystem` (MonoBehaviour) + logic in level states:

| Level | Bonuses |
|---|---|
| **Warmup** | Base gems for correct answers |
| **Miqat** | + speed (+2 if < 3 min) · + excellence (+50% at ≥ 90%) |
| **Tawaf** | + streak (streak×2 from 3rd) · + perfect lap (+20 for 7 in a row) · + perfect Tawaf (+50 for 100%) |

---

## 🔧 All Services

| Service | Type | File | Description |
|---|---|---|---|
| **GameManager** | Singleton | `Core/GameManager.cs` | Service Locator, DontDestroyOnLoad |
| **GameStateMachine** | MonoBehaviour | `Core/States/GameStateMachine.cs` | FSM — state management |
| **LocalizationService** | Plain C# | `Services/LocalizationService.cs` | CSV localization, 7 languages |
| **UserProfileService** | Plain C# | `Services/UserProfileService.cs` | Profile: PlayerPrefs + File |
| **ProfileLoaderService** | Plain C# | `Services/ProfileLoaderService.cs` | Multi-provider loading |
| **ProgressService** | Plain C# | `Services/ProgressService.cs` | Level progress tracking |
| **QuizService** | MonoBehaviour | `Services/QuizService.cs` | Quiz management |
| **StageCompletionService** | MonoBehaviour | `Services/StageCompletionService.cs` | Theory block verification |
| **AudioService** | MonoBehaviour | `Services/AudioService.cs` | Sounds: `PlayClick()`, `PlayWhoosh()` |
| **UIService** | MonoBehaviour | `UI/UIService.cs` | UI panels, screens, gems |

---

## 📁 Project Structure

```
Assets/
├── Art/Fonts/Comfortaa/                   Fonts
├── Data/QuizData/                         Quiz JSON data
├── DOTween/                               Animation library (3rd party)
├── Prefabs/
│   ├── App/                               App.prefab, Services.prefab
│   ├── UI/                                LevelBtn, QuestionController, StartScreen
│   │   ├── Levels/                        Canvas, LevelController, LevelSelect
│   │   └── Theory/                        CardViewer, WarmUpTheoryCardView
├── Resources/
│   ├── Audio/                             Sound files
│   ├── SO/Levels/, SO/Theory/             ScriptableObjects
│   └── localization.csv                   Localization
├── Scenes/
│   ├── MainMenu.unity                     Main menu
│   ├── 1lvl.unity                         Warmup
│   ├── 2lvl.unity                         Miqat
│   └── 3lvl.unity                         Tawaf
├── Scripts/
│   ├── Core/                              Bootstrapper, GameManager, States/
│   ├── Data/                              Language, LevelData, UserProfile, QuizQuestion
│   ├── Editor/                            Editor tools
│   ├── Gameplay/                          QuizSystem, RewardSystem
│   ├── Services/                          All services + ProfileDataProvider/
│   └── UI/                                13 UI controllers
├── Settings/                              Rendering
├── TextMesh Pro/                          TMP (3rd party)
└── UI Toolkit/                            UI Toolkit
```

---

## 🎬 Scenes

| Scene | File | Contents |
|---|---|---|
| **MainMenu** | `MainMenu.unity` | Start screen, language selection. `App.prefab` with `GameManager` and `Bootstrapper` |
| **Warmup** | `1lvl.unity` | Hajj preparation: 3 theory blocks → quiz |
| **Miqat** | `2lvl.unity` | Miqat: 2 theory blocks → quiz with speed bonuses |
| **Tawaf** | `3lvl.unity` | Tawaf: 2 theory blocks → quiz with streak system |

---

## 🧩 Prefabs

### App

| Prefab | Description |
|---|---|
| **App.prefab** | `GameManager` (Singleton) + `Bootstrapper`. Placed in the initial scene |
| **Services.prefab** | `AudioService`, `StageCompletionService`, `QuizService`, `GameStateMachine` |

### UI

| Prefab | Description |
|---|---|
| **StartScreen.prefab** | Logo, "Start" button, language selection |
| **LevelBtn.prefab** | Level button: thumbnail, progress bar, lock |
| **QuestionCotroller.prefab** | Question: text, 4 answers, feedback |
| **Canvas.prefab** | UI Canvas for level scenes |
| **LevelController.prefab** | Theory ↔ quiz switching |
| **LevelSelect.prefab** | Level list, progress, gems |
| **levels.prefab** | Level buttons container |

### Theory

| Prefab | Description |
|---|---|
| **CardViewer.prefab** | Card viewer: swiping, animations |
| **WarmUpTheoryCardView.prefab** | Extended card for Warmup |

---

## 📝 Script Descriptions

### Core (4)

| Script | Description |
|---|---|
| `Bootstrapper.cs` | Entry point. Service registration, launches `MainMenuState` |
| `GameManager.cs` | Singleton Service Locator, `DontDestroyOnLoad`, `GetService<T>()` |
| `GameplaySceneInitializer.cs` | Finds `QuizSystem`/`RewardSystem` in scene, connects to current state |
| `LevelManager.cs` | `StartLevel()`, `RestartLevel()`, `ShowResults()` |

### States (11)

| Script | Description |
|---|---|
| `BaseGameState.cs` | Abstract: `StateId`, `Enter()`, `Update()`, `Exit()`, `OnPause()`, `OnResume()` |
| `GameStateMachine.cs` | FSM: 6 states, transitions, pause, events |
| `BaseLevelState.cs` | Base for levels: theory → quiz → save |
| `LevelStateIds.cs` | Constants: `MainMenu`, `LevelSelect`, `Results`, `Warmup`, `Miqat`, `Tawaf` |
| `LevelStateMachine.cs` | Legacy FSM (backward compatibility) |
| `MainMenuState.cs` | `ui.ShowMainMenu()` |
| `LevelSelectState.cs` | `ui.ShowLevelSelect()`, progress and gems |
| `WarmupLevelState.cs` | 3 theory blocks, no bonuses |
| `MiqatLevelState.cs` | 2 blocks, timer, speed & ≥90% bonuses |
| `TawafLevelState.cs` | 2 blocks, streak, laps (7 questions), bonuses |
| `ResultsState.cs` | `ui.ShowResults()` |

### Theory (7)

| Script | Description |
|---|---|
| `TheoryCardBase.cs` | Abstract base card class |
| `SimpleTheoryCard.cs` | Simple card: text + image |
| `WarmUpTheoryCard.cs` | Extended card for Warmup |
| `TheoryCardData.cs` | Data: title, text, image, localization keys |
| `TheoryCardContainer.cs` | Card container, navigation |
| `TheoryCardsManager.cs` | Lifecycle: load, show, hide |
| `TheoryToQuizIntegration.cs` | Bridge: theory completion → quiz launch |

### Data (5)

| Script | Description |
|---|---|
| `Language.cs` | Enum: Russian, Bosnian, Albanian, Turkish, Arabic, Indonesian, English |
| `LevelData.cs` | SO for level: `LevelId`, `Questions[]`, `CompletionBonusGems`, `PassThreshold` |
| `LevelProgress.cs` | Level progress |
| `QuizQuestion.cs` | Question: `QuestionText`, `Options[4]`, `CorrectAnswerIndex`, `GemsReward` |
| `UserProfile.cs` | Profile: name, progress, gems, completed levels |

### Services (8)

| Script | Description |
|---|---|
| `LocalizationService.cs` | CSV localization, `GetText()`, `ChangeLanguage()`, `OnLanguageChanged` |
| `LocalizationServiceLoader.cs` | Loading utility |
| `AudioService.cs` | `PlaySound()`, `PlayClick()`, `PlayWhoosh()` |
| `UserProfileService.cs` | Profile: `GetProfile()`, `UpdateProfile()`, `Save()` |
| `ProfileLoaderService.cs` | Strategy: `Load()`, `Save()`, `SyncWithBackendAsync()` |
| `ProgressService.cs` | `RecordLevelProgress()`, `GetLevelProgress()`, `IsLevelCompleted()` |
| `QuizService.cs` | `InitializeQuiz()`, `SubmitAnswer()`, `GetLastScorePercent()` |
| `StageCompletionService.cs` | `CompleteStage()`, `RecordLevelResult()`, `GetLevelScore()` |

### Profile Data Providers (4)

| Script | Description |
|---|---|
| `IProfileDataProvider.cs` | Interface: `Load()`, `Save()`, `Priority` |
| `PlayerPrefsProfileProvider.cs` | PlayerPrefs (JSON), priority 50 |
| `FileProfileProvider.cs` | JSON file, priority 10 |
| `BackendProfileProvider.cs` | REST API, priority 100 |

### Gameplay (2)

| Script | Description |
|---|---|
| `QuizSystem.cs` | Quiz executor: `Initialise()`, `SubmitAnswer()`, `Advance()` |
| `RewardSystem.cs` | `AwardGems()` → `GameManager.AddGems()` |

### UI (13)

| Script | Description |
|---|---|
| `UIService.cs` | Central UI: `ShowMainMenu()`, `ShowLevelSelect()`, `ShowResults()` |
| `GameTextController.cs` | Localized TMP_Text, auto-update |
| `LanguageSelectController.cs` | Language selection |
| `GameplayUI.cs` | Gameplay panel |
| `QuizUIController.cs` | Question, answers, feedback |
| `ResultsUI.cs` | Score, gems, Next / Replay |
| `SelectMenuUIController.cs` | Level select menu |
| `MainGameplayMenuUI.cs` | Pause, restart, back |
| `PauseMenuUI.cs` | Continue, restart, exit |
| `LevelTileUI.cs` | Level tile: thumbnail, progress, lock |
| `StageGameplayController.cs` | Theory controller, "Next" button |
| `ButtonEffect.cs` | Button effects (DOTween) |

### Level Logic (1)

| Script | Description |
|---|---|
| `LevelController.cs` | Switching between theory and quiz |

### Editor (3, `#if UNITY_EDITOR`)

| Script | Description |
|---|---|
| `ContentLoaderWindow.cs` | Content loading from external sources |
| `DataLoader.cs` | Quiz/level data loading |
| `LocalizationEditorService.cs` | Localization tools in Editor |

---

## 🏆 Level Rewards

```
┌─ WARMUP ─────────────────────────────────────────────────────┐
│  Base gems: question.GemsReward                               │
│  Bonuses: none  ·  Theory blocks: 3                          │
└───────────────────────────────────────────────────────────────┘

┌─ MIQAT ──────────────────────────────────────────────────────┐
│  Base gems + speed (+2 if < 3 min)                            │
│  + excellence (+50% CompletionBonusGems at ≥ 90%)            │
│  Theory blocks: 2                                             │
└───────────────────────────────────────────────────────────────┘

┌─ TAWAF ──────────────────────────────────────────────────────┐
│  Base gems + streak (streak×2 from 3rd consecutive)           │
│  + perfect lap (+20 for 7 in a row)                          │
│  + perfect Tawaf (+50 for 100%)                              │
│  Laps: every 7 questions  ·  Theory blocks: 2               │
└───────────────────────────────────────────────────────────────┘
```

---

## 🧱 Architectural Patterns

| Pattern | Where | File |
|---|---|---|
| **Service Locator** | Service access | `GameManager.cs` |
| **Singleton** | Single GameManager | `GameManager.cs` |
| **State Machine** | Game states | `GameStateMachine.cs` |
| **Template Method** | Level lifecycle | `BaseLevelState.cs` |
| **Strategy** | Multi-provider loading | `ProfileLoaderService.cs` |
| **Observer** | Quiz, language, level events | `QuizSystem`, `LocalizationService` |
| **Factory** | State creation | `GameStateMachine.cs` |

---

## 🚀 Quick Start

1. Open project in **Unity 2022.3+**
2. Open `Assets/Scenes/MainMenu.unity`
3. Ensure `App.prefab` is in the scene with services bound in `Bootstrapper`
4. **Play** — starts in `MainMenuState`

**Levels:** `Assets/Resources/SO/Levels/` → Assets → Create → Manasik → Level Data

**Localization:** `Assets/Resources/localization.csv` → Editor → Content Loader → Download from Google Sheets

**Adding a new level:**
1. Subclass `BaseLevelState` with a unique `StateId`
2. Add constant to `GameStateIds`
3. Register in `GameStateMachine.RegisterDefaultStates()`
4. Create `LevelData` ScriptableObject + scene with `QuizSystem` and `RewardSystem`
