<p align="center">
  <img src="https://img.shields.io/badge/Unity-6-black?logo=unity&logoColor=white" alt="Unity"/>
  <img src="https://img.shields.io/badge/C%23-10-239120?logo=csharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/Platform-WebGL-blue" alt="WebGL"/>
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License"/>
  <img src="https://img.shields.io/badge/Languages-7-orange" alt="7 Languages"/>
</p>

<h1 align="center">🕋 HajjFlow</h1>

<p align="center">
  <b>An interactive educational game that teaches pilgrims the fundamentals of Hajj.</b><br/>
  <i>Learn. Practice. Be prepared.</i>
</p>

<p align="center">
  Immersive 2.5D environments · Guided simulations · Theory cards · Quizzes · Audio learning<br/>
  Step by step — every ritual before its real performance.
</p>

> 🇷🇺 [Версия на русском (README_RUS.md)](README_RUS.md)

---

## Overview

HajjFlow is a Unity WebGL educational experience that presents the Hajj journey as a sequence of interactive levels. Each level combines theory, a guided gameplay stage, a quiz, and a saved result. The project is designed so that learning content can be changed outside the Unity scene: levels, questions, theory cards, and translations are loaded into runtime models from published Google Sheets CSV files.

### Core gameplay loop

1. Main menu
2. Level selection
3. Theory cards
4. Guided gameplay stage
5. Quiz
6. Results, rewards, and saved progress
7. Next unlocked level

### Project goals

- Make Hajj learning visual, sequential, and approachable.
- Keep gameplay code independent from day-to-day content edits.
- Support seven languages through runtime localization.
- Run in the browser while preserving player progress and downloaded content between sessions.
- Keep local ScriptableObject content as a fallback for offline or development use.

---

## Architecture at a glance

```text
Bootstrap scene
  -> GameManager (persistent service locator)
  -> Bootstrapper registers services
  -> GameStateMachine enters Main Menu
  -> ContentLoaderService resolves content
       -> persistent cache
       -> Google Sheets CSV
       -> Resources fallback
  -> RuntimeLevelFactory creates runtime LevelData
  -> UIService / TheoryCardsManager / QuizService present the level
  -> StageCompletionService + UserProfileService save the result
```

The architecture has four main layers:

| Layer | Responsibility | Main components |
| --- | --- | --- |
| Application | Starts the app and exposes shared services | `GameManager`, `Bootstrapper` |
| Flow | Controls screen and level transitions | `GameStateMachine`, `BaseGameState`, level states |
| Content | Downloads, caches, parses, and transforms learning data | `ContentLoaderService`, `RuntimeLevelFactory`, `AssetBundleService` |
| Presentation and progress | Shows UI, runs quizzes, and persists player state | `UIService`, `QuizService`, `TheoryCardsManager`, profile/progress services |

---

## Application bootstrap and services

`GameManager` is a persistent singleton (`DontDestroyOnLoad`) and the application service locator. `Bootstrapper` runs after it, creates plain C# services, registers scene-bound `MonoBehaviour` services from Inspector references, checks the required dependencies, and opens the main-menu state.

### Registered services

| Service | Purpose |
| --- | --- |
| `GameStateMachine` | Authoritative application and level-flow state machine |
| `UIService` | Builds and switches UI views, including runtime level selection |
| `ContentLoaderService` | Loads content CSV, owns the persistent content cache, and publishes loading events |
| `RuntimeLevelFactory` | Converts runtime records to temporary `LevelData` and theory objects understood by gameplay |
| `QuizService` | Displays questions, validates answers, tracks score, and emits quiz events |
| `StageCompletionService` | Validates completed theory stages and records level results |
| `LocalizationService` | Resolves keys to the active-language text and refreshes localized UI |
| `UserProfileService` | Loads and saves player profile data |
| `ProgressService` | Provides progress operations over the user profile |
| `ProfileLoaderService` | Supports multi-source profile persistence and optional backend synchronization |
| `AudioService` | Provides shared audio feedback |
| `AssetBundleService` | Loads optional visual assets from remote bundles or `StreamingAssets` |
| `GameMainConfig` | ScriptableObject configuration and static-content fallback settings |

Services are retrieved through `GameManager.Instance.GetService<T>()`. This keeps scene references concentrated in the bootstrap scene instead of distributing them throughout gameplay objects.

---

## Game flow and states

`GameStateMachine` is the single authority for transitions. It creates and caches state objects when they are first requested, calls `Exit()` on the outgoing state and `Enter()` on the new state, and publishes `OnStateChanged`.

```text
MainMenuState
  -> LevelSelectState
  -> LevelState(levelId)
       -> theory cards
       -> gameplay stage
       -> quiz
  -> ResultsState
  -> LevelSelectState or replay
```

When a player selects a level, `GameStateMachine.StartLevel()` stores the selected `LevelData` as the active level and enters its dynamic `LevelState`. Completing the level emits `OnLevelCompleted`, records the score, and transitions to `ResultsState`. Replay re-enters the same level with the active runtime data rather than rebuilding the scene.

The state machine also owns pause/resume behavior and calls the active state's update loop only when the game is not paused.

---

## Runtime content architecture

The project supports two interchangeable sources of learning content:

- **Remote runtime content** — published Google Sheets CSV files.
- **Static fallback content** — `ScriptableObject` assets and CSV/TextAssets under `Assets/Resources`.

`GameMainConfig.UseRemoteContent` selects the runtime-first path. The static assets remain useful for authoring, editor workflows, and a final fallback when no valid remote cache is available.

### Content pipeline

```text
Google Sheets (CSV)
  -> UnityWebRequest
  -> ContentLoaderService parsers
  -> RuntimeLevelInfo / RuntimeQuizQuestion / RuntimeTheoryCard
  -> RuntimeLevelFactory
  -> temporary LevelData + TheoryCardContainer + TheoryCardData
  -> UI, theory flow, and quiz flow
```

`ContentLoaderService` requests four independent datasets:

| Dataset | Runtime model | Used by |
| --- | --- | --- |
| Localization | translation table | `LocalizationService`, localized UI |
| Levels | `RuntimeLevelInfo` | level-selection UI and runtime factory |
| Questions | `RuntimeQuizQuestion` | `QuizService` |
| Theory | `RuntimeTheoryCard` | `TheoryCardsManager` and runtime factory |

The requests are started in parallel. A remote download is treated as successful only when all four datasets finish successfully; partial content is never committed as a valid cache.

### Runtime model conversion

`RuntimeLevelFactory` is the boundary between external data and the existing gameplay API. For a chosen `levelId` it:

1. Reads level metadata from `ContentLoaderService`.
2. Creates an in-memory `LevelData` object — it is not saved as a Unity asset.
3. Converts quiz records to `QuizQuestion[]`.
4. Builds a `TheoryCardContainer` and its `TheoryCardData` records.
5. Resolves an optional thumbnail by `imageBundleKey` through `AssetBundleService`.

This lets the state machine, quiz, and theory code continue to work with familiar Unity data types while content authors work in spreadsheets.

---

## Content cache and WebGL behavior

Content loading is **cache-first**. The sequence is deliberately optimized for WebGL, where startup network requests are costly and persistent browser storage is important.

```text
App start / LoadAllContent()
  -> Is a complete valid cache available?
       -> yes: parse cached CSV and start immediately
       -> no: download all CSV datasets in parallel
                -> save the complete dataset to persistent storage
                -> if download fails, try cache and then Resources fallback
```

The cache lives under:

```text
Application.persistentDataPath/ContentCache/
  localization.csv
  levels.csv
  questions.csv
  theory.csv
```

Raw CSV is stored instead of serialized runtime models. This avoids `JsonUtility` limitations with nested dictionaries in the localization table and makes the cached representation match the source data. `PlayerPrefs` is retained as a fallback when file-based persistent storage is unavailable.

To discard content and force a new remote load:

```csharp
// Runtime component API
contentLoaderService.EraseCacheData();

// Editor helper; available while in Play Mode
HajjFlow.Editor.ContentLoader.DataLoader.EraseCacheData();
```

`EraseCacheData()` removes the CSV cache and PlayerPrefs entries, clears the in-memory content collections, then starts a new full load.

`ContentLoaderService` exposes:

- `OnLoadProgress` — a `0..1` loading-progress signal.
- `OnLoadComplete` — emitted after content resolves from cache, remote data, or static fallback.
- `GetAllLevels()`, `GetQuestionsForLevel(levelId)`, and `GetTheoryCardsForLevel(levelId)` — read access for dependent systems.

---

## Localization

Localization is key-based. Content records store keys such as a level title, question text, option, explanation, theory title, or theory body. `LocalizationService` maps each key to the text for the selected language and updates subscribed UI components when the language changes.

The runtime content loader can replace the translation table after the localization CSV is downloaded or restored from cache. This makes translation corrections and new languages content updates rather than code releases.

---

## Quiz, theory, and rewards

### Theory

Theory data is organized by `levelId` and `order`. `TheoryCardsManager` presents cards for the active level, while `StageCompletionService` validates and reports completed theory blocks.

### Quiz

`QuizService` receives a `QuizQuestion[]` from the runtime factory or reads runtime questions directly by level ID. It emits events for a displayed question, correct and incorrect answers, readiness for the next question, and final completion. Options are shuffled before display to avoid retaining the original correct-answer position.

### Results and progress

When a level completes, the flow records a score through `StageCompletionService`. `ProgressService` and `UserProfileService` keep progression and rewards available across sessions. `GameManager.AddGems()` updates the profile immediately and refreshes the UI counter.

---

## Persistence

There are two distinct persistence concerns:

| Data | Owner | Storage strategy |
| --- | --- | --- |
| Downloaded learning content | `ContentLoaderService` | CSV files in `Application.persistentDataPath`, with `PlayerPrefs` fallback |
| Player profile, progress, gems | `UserProfileService` | JSON file in `Application.persistentDataPath` and `PlayerPrefs` |

`ProfileLoaderService` and the profile-provider interfaces allow the profile flow to be expanded with backend synchronization without changing the gameplay systems that use the profile.

---

## Visual assets and bundles

Learning content can reference `imageBundleKey`. `AssetBundleService` resolves these assets from already loaded bundles and keeps in-memory caches for bundles and sprites.

Bundle resolution order:

1. Remote bundle URL, when configured.
2. `StreamingAssets/AssetBundles`, when the local fallback is enabled.

This separates heavy visual content from the CSV content model and allows images to be updated or delivered independently.

---

## Project structure

```text
Assets/
  Scenes/                         Unity scenes and bootstrap setup
  Prefabs/                        App, level, theory, and UI prefabs
  Resources/                      Static fallback CSV, ScriptableObjects, and audio
  Data/                           Source JSON used by editor import workflows
  WebGLTemplates/HajjflowPreloader/
                                  Custom WebGL loading template
  Scripts/
    Core/                         Bootstrap, GameManager, states, level logic, theory
    Data/                         Serializable game and content data types
    Gameplay/                     Quiz and reward presentation logic
    Services/                     Content, profile, localization, assets, and progress
    UI/                           View controllers and UI composition
    Editor/ContentLoader/         Editor tools for importing JSON content
```

### Key files

| Path | Role |
| --- | --- |
| `Assets/Scripts/Core/Bootstrapper.cs` | Registers and validates application services |
| `Assets/Scripts/Core/GameManager.cs` | Persistent service locator and global convenience API |
| `Assets/Scripts/Core/States/GameStateMachine.cs` | State transitions, active level, pause, and replay |
| `Assets/Scripts/Services/ContentLoaderService.cs` | Remote CSV loading, cache-first resolution, parsing, and fallback |
| `Assets/Scripts/Services/RuntimeLevelFactory.cs` | Runtime-model conversion to gameplay-compatible objects |
| `Assets/Scripts/Services/LocalizationService.cs` | Translation lookup and language updates |
| `Assets/Scripts/Services/AssetBundleService.cs` | Bundle and sprite loading/caching |
| `Assets/Scripts/Services/UserProfileService.cs` | Profile persistence |
| `Assets/Scripts/UI/UIService.cs` | UI initialization and runtime level-grid construction |
| `Assets/Scripts/Editor/ContentLoader/` | JSON import and editor content-management tools |

---

## Quick start

### Requirements

- Unity **6000.3.9f1** (Unity 6) — the version recorded in `ProjectSettings/ProjectVersion.txt`.
- A WebGL-capable Unity installation to create browser builds.

### Open and run

1. Clone the repository.
2. Open the project folder in Unity Hub with Unity 6.
3. Let Unity import packages and generated project files.
4. Open the bootstrap/start scene and verify the Inspector references on `Bootstrapper`.
5. Ensure `GameMainConfig` is assigned and choose whether `UseRemoteContent` is enabled.
6. Press Play. On the first remote run, the app downloads and caches all content; subsequent runs use the cache first.

### Build for WebGL

1. Install the **WebGL Build Support** module for the Unity editor version above.
2. In Unity, open **File → Build Settings**, select **WebGL**, then switch platform if needed.
3. Configure any required published Google Sheets URLs and remote asset-bundle URL in the relevant services.
4. Build and host the generated files on a web server that supports WebGL compression and the required response headers.

---

## Content-authoring notes

- Keep `levelId` stable across levels, questions, and theory records; it is the join key for the runtime pipeline.
- Keep `order` numeric for levels and theory cards.
- Use localization keys in CSV records rather than hardcoded display text.
- When changing remote content, clear the cache during testing with `EraseCacheData()` to verify a first-load path.
- Use the editor tools under **Tools → Hajj → Content Loader** to import JSON content into static ScriptableObject assets when working with the fallback pipeline.

---

## Notes

- Runtime content and player progress are intentionally stored separately: clearing the content cache does not erase player progression.
- The app always prefers a complete cache over a new network request. This prevents repeated downloads and gives WebGL users a faster repeat launch.
- If neither a valid cache nor remote content is available, the loader attempts to use static Resources content bundled with the application.
