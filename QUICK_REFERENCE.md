# 🚀 Quick Reference - State Machine API

## Основное Использование

### Запуск Уровня
```csharp
using HajjFlow.Core;
using HajjFlow.Core.States;

// Warmup
LevelManager.StartLevel(levelData, LevelStateIds.Warmup);

// Miqat
LevelManager.StartLevel(levelData, LevelStateIds.Miqat);

// Tawaf
LevelManager.StartLevel(levelData, LevelStateIds.Tawaf);
```

### Доступ к StateMachine
```csharp
LevelStateMachine sm = LevelManager.StateMachine;
BaseLevelState current = sm?.CurrentState;
```

### Pause/Resume
```csharp
LevelManager.StateMachine?.Pause();
LevelManager.StateMachine?.Resume();
bool paused = LevelManager.StateMachine?.IsPaused ?? false;
```

### Перезапуск/Навигация
```csharp
LevelManager.RestartLevel();          // Перезапуск текущего
LevelManager.GoToLevelSelect();       // Вернуться к выбору
LevelManager.GoToMainMenu();          // В главное меню
LevelManager.ShowResults();           // Показать результаты
```

---

## События

### LevelStateMachine Events
```csharp
// Смена состояния
stateMachine.OnStateChanged += (state) => {
    Debug.Log($"New state: {state.StateId}");
};

// Завершение уровня
stateMachine.OnLevelCompleted += (stateId, score) => {
    Debug.Log($"Level {stateId} completed: {score}%");
};
```

---

## Константы StateId

```csharp
using HajjFlow.Core.States;

LevelStateIds.Warmup    // "warmup"
LevelStateIds.Miqat     // "miqat"
LevelStateIds.Tawaf     // "tawaf"

// Helpers
string next = LevelStateIds.GetNextState("warmup");     // → "miqat"
string prev = LevelStateIds.GetPreviousState("tawaf");  // → "miqat"
bool valid = LevelStateIds.IsValid("warmup");           // → true
```

---

## Создание Нового Состояния

```csharp
using HajjFlow.Gameplay;

namespace HajjFlow.Core.States
{
    public class NewLevelState : BaseLevelState
    {
        public override string StateId => "new_level";

        public override void Enter()
        {
            base.Enter();
            // Setup
        }

        public override void Update()
        {
            base.Update();
            // Per-frame logic
        }

        public override void Exit()
        {
            base.Exit();
            // Cleanup
        }
    }
}
```

Затем зарегистрировать в `LevelStateMachine.RegisterStates()`:
```csharp
RegisterState(new NewLevelState());
```

---

## Награды по Уровням

| Level | Base | Bonuses |
|-------|------|---------|
| Warmup | 5💎/Q | +20💎 completion |
| Miqat | 5💎/Q | +2💎 speed, +15💎 excellence, +30💎 completion |
| Tawaf | 5💎/Q | streak×2💎, +20💎 circle, +50💎 perfect, +50💎 completion |

---

## Debug

### Console Logs
```
[LevelManager] Starting level: ... with state: warmup
[LevelStateMachine] State changed to: warmup
[WarmupLevelState] Entering state: ...
[WarmupLevelState] Question 1/5: ...
[GameManager] +5 gems. Total: 25
[WarmupLevelState] Correct answer!
[WarmupLevelState] Quiz completed with score: 80%
```

### Common Issues
```
"QuizSystem not found"     → Add QuizSystem to Gameplay scene
"State not registered"     → Check LevelStateMachine.RegisterStates()
"ActiveLevel is null"      → Call StartLevel() before loading scene
```

---

*Quick Reference - v1.0*

