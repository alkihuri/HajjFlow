# 🚀 Quick Start Guide

## Основное Использование

### Запуск Уровня
```csharp
using HajjFlow.Core;
using HajjFlow.Core.States;

LevelManager.StartLevel(levelData, LevelStateIds.Warmup);
LevelManager.StartLevel(levelData, LevelStateIds.Miqat);
LevelManager.StartLevel(levelData, LevelStateIds.Tawaf);
```

### Pause/Resume
```csharp
LevelManager.StateMachine?.Pause();
LevelManager.StateMachine?.Resume();
```

### Навигация
```csharp
LevelManager.RestartLevel();
LevelManager.GoToLevelSelect();
LevelManager.GoToMainMenu();
```

---

## 🎮 Scene Setup

### Gameplay Scene:
```
GameObject: GameController
├── LevelStateMachine
└── GameplaySceneInitializer
```

### LevelData:
```
1. WarmupLevel  → "level_1_warmup"
2. MiqatLevel   → "level_2_miqat"
3. TawafLevel   → "level_3_tawaf"
```

---

## 📖 Documentation

- **ARCHITECTURE.md** - Полное описание
- **SCENE_SETUP.md** - Настройка Unity
- **CHECKLIST.md** - План действий

**Status:** ✅ Ready

