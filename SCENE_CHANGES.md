# Scene Changes Required After Refactoring

## Overview

After the state machine refactoring, the following changes need to be made in the Unity Editor.

---

## 1. StageGameplayController — Remove WarmupLevelController Reference

**File changed:** `Assets/Scripts/UI/StageGameplayController.cs`

`StageGameplayController` no longer references `WarmupLevelController` directly. It now communicates with the current `BaseLevelState` through the `GameStateMachine`.

**Action:**
- Open any scene that has `StageGameplayController` components
- The old `[SerializeField] WarmupLevelController levelController` field is removed — no Inspector assignment needed
- `StageGameplayController` now works automatically with **any** level state (Warmup, Miqat, Tawaf)

---

## 2. Level Controllers — Simplified (Optional Cleanup)

**Files changed:**
- `Assets/Scripts/Core/LevelsLogic/WarmupLevelController.cs`
- `Assets/Scripts/Core/LevelsLogic/MiqatLevelController.cs`
- `Assets/Scripts/Core/LevelsLogic/TawafLevelController.cs`

The controllers no longer hold references to `StageCompletionService`, `QuizService`, or `QuizUIController`.

**Action:**
- Open scenes where these controllers exist
- The old serialized fields (`StageCompletionService`, `QuizService`, `QuizUIController`) are removed
- Only `LevelData` needs to be assigned in the Inspector
- **If controllers are used only as button click handlers** in the scene, they still work
- **If controllers are not used at all** (all flow goes through the state machine), they can be safely removed from the scene

---

## 3. UIService — No Scene Changes Needed

The `UIService` changes are backward-compatible. The old methods `WarmUpLevelShow()`, `MiqatLevelShow()`, `TawafLevelShow()` still exist as wrappers around the new `ShowLevelByStateId()` method.

---

## 4. Level Replay

To allow replaying a level from the results screen or level select:
- Call `GameStateMachine.ReplayCurrentLevel()` from any button
- Or transition back to `LevelSelect` state, then re-enter the level — the state's `Enter()` method now fully resets all runtime state (quiz progress, score, stage index)

**Example button setup:**
- Add a "Replay" button in the results panel
- Connect it to call: `GameManager.Instance.GetService<GameStateMachine>().ReplayCurrentLevel()`

---

## 5. Query Level Results

To display previously completed level scores:

```csharp
var stageService = GameManager.Instance.stageCompletionService;

// Get score for a specific level
float score = stageService.GetLevelScore("Warmup");

// Check if level was completed
bool completed = stageService.HasLevelResult("Miqat");

// Get full result data
LevelResult result = stageService.GetLevelResult("Tawaf");
if (result != null)
{
    Debug.Log($"Score: {result.ScorePercent}%, Date: {result.CompletedAt}");
}
```

---

## Summary of Inspector Changes

| GameObject | Old Field | Action |
|---|---|---|
| StageGameplayController | `levelController` (WarmupLevelController) | **Removed** — no assignment needed |
| WarmupLevelController | `_quizUIController`, `stageCompletionService`, `quizService` | **Removed** — only `levelData` remains |
| MiqatLevelController | `stageCompletionService`, `quizService` | **Removed** — only `levelData` remains |
| TawafLevelController | `stageCompletionService`, `quizService` | **Removed** — only `levelData` remains |
