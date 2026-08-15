<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3+-black?logo=unity&logoColor=white" alt="Unity"/>
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
  Immersive 2.5D environments · Guided simulations · Quizzes · Audio learning<br/>
  Step by step — every ritual before its real performance.
</p>

> 🇷🇺 [Версия на русском (README_RUS.md)](README_RUS.md)

---

## Overview

HajjFlow is a Unity WebGL experience for learning Hajj rituals through a state-driven gameplay loop. The project is designed around a modular service architecture, and one of its key features is that most of the learning content is loaded at runtime instead of being hardcoded into scenes.

### Core gameplay loop

1. Main menu
2. Level selection
3. Theory cards
4. Quiz
5. Results
6. Next level

### What is important in this repository

- The app uses a service locator style bootstrap flow.
- The content pipeline is runtime-first: levels, questions, theory cards, and localization are loaded from external CSV sources and transformed into runtime models.
- UI and gameplay are built around those runtime models, so content can be updated without rebuilding the whole game.

---

## Architecture: runtime content updates first

The architecture is centered on the idea that the experience can evolve without shipping a new build for every content change.

### 1. Bootstrap and service registration

`GameManager` acts as the central singleton container. During startup, `Bootstrapper` registers the services needed by the game:

- `UIService`
- `QuizService`
- `StageCompletionService`
- `GameStateMachine`
- `ContentLoaderService`
- `LocalizationService`
- `ProgressService`
- `UserProfileService`

This gives the application a single access point for runtime services and keeps the scene wiring simple.

### 2. Runtime content pipeline

The most important runtime flow is handled by `ContentLoaderService`.

- It loads CSV data from Google Sheets at runtime.
- It parses localization, levels, quiz questions, and theory cards into runtime models.
- It caches the data locally and can fall back to cache if network access is unavailable.
- It raises `OnLoadProgress` and `OnLoadComplete` events so the app can react when content is ready.

This is the place where content updates happen in production: the app can receive fresh content without changing code or rebuilding the scene assets.

### 3. Runtime model conversion

`RuntimeLevelFactory` converts the loaded runtime models into the runtime objects the gameplay systems already understand:

- `LevelData` for the level flow
- quiz question objects for the quiz system
- theory card objects for the theory UI

That means the rest of the app can remain mostly unchanged while the content source evolves.

### 4. UI reacts to content load events

`UIService` subscribes to `ContentLoaderService.OnLoadComplete` and builds the level selection UI after the content is available.

This creates a clear separation between:

- content loading
- content transformation
- UI rendering

### 5. Localization updates at runtime

`LocalizationService` works with the content loader so translations can be refreshed and applied dynamically. UI text is updated as the language changes, which makes localization part of the runtime experience rather than a static asset step.

---

## Main project components

- `Assets/Scripts/Core/Bootstrapper.cs` — wires services into the game at startup
- `Assets/Scripts/Core/GameManager.cs` — central service locator and global access point
- `Assets/Scripts/Core/States/` — state machine and game flow states
- `Assets/Scripts/Services/ContentLoaderService.cs` — loads and caches remote content at runtime
- `Assets/Scripts/Services/RuntimeLevelFactory.cs` — converts runtime content models into game objects/data
- `Assets/Scripts/UI/UIService.cs` — builds UI from the loaded runtime content
- `Assets/Scripts/Services/LocalizationService.cs` — runtime localization and translation updates

---

## Runtime content flow (high level)

```text
Google Sheets / CSV
  -> ContentLoaderService
  -> Runtime models
  -> RuntimeLevelFactory
  -> LevelData / Quiz / Theory cards
  -> UI and gameplay systems
```

This is the architectural core of the project: content is not only data, it is a runtime dependency that the application consumes and updates live.

---

## Quick start

1. Open `HajjFlow.Unity.slnx` in Unity 2022.3+.
2. Ensure the bootstrap scene contains the required services and references.
3. Run the project and let `Bootstrapper` register the services.
4. If remote content is enabled in `GameMainConfig`, the app will fetch and apply content at runtime automatically.

---

## Notes

- The repository has been consolidated around the runtime architecture described above.
- The older stage-specific markdown notes were removed in favor of a single source of truth for the current app design.
