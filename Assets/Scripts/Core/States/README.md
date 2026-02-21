# Level State Machine - Implementation Guide

## 🎯 Обзор

Эта папка содержит реализацию паттерна State Machine для управления уровнями игры HajjFlow.

## 📁 Файлы

### Core Classes
- **BaseLevelState.cs** - Абстрактный базовый класс для всех состояний
- **LevelStateMachine.cs** - Машина состояний, управляющая переходами
- **LevelStateIds.cs** - Константы для идентификаторов состояний

### Level States
- **WarmupLevelState.cs** - Состояние уровня "Разминка"
- **MiqatLevelState.cs** - Состояние уровня "Miqat"
- **TawafLevelState.cs** - Состояние уровня "Tawaf"

## 🚀 Быстрый Старт

### 1. Добавить в Gameplay сцену:
```
GameObject: GameController
├── LevelStateMachine (Component)
└── GameplaySceneInitializer (Component)
```

### 2. Запустить уровень из кода:
```csharp
using HajjFlow.Core;
using HajjFlow.Core.States;

// Запуск уровня Warmup
LevelManager.StartLevel(warmupLevelData, LevelStateIds.Warmup);

// Запуск уровня Miqat
LevelManager.StartLevel(miqatLevelData, LevelStateIds.Miqat);

// Запуск уровня Tawaf
LevelManager.StartLevel(tawafLevelData, LevelStateIds.Tawaf);
```

### 3. Доступ к текущему состоянию:
```csharp
var currentState = LevelManager.StateMachine?.CurrentState;
if (currentState != null)
{
    Debug.Log($"Current state: {currentState.StateId}");
}
```

## 🔧 Как Добавить Новое Состояние

### Шаг 1: Создать класс состояния
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
            // Инициализация
        }

        public override void Update()
        {
            base.Update();
            // Логика каждого кадра
        }

        public override void Exit()
        {
            base.Exit();
            // Очистка
        }
    }
}
```

### Шаг 2: Зарегистрировать в LevelStateMachine.cs
```csharp
private void RegisterStates()
{
    RegisterState(new WarmupLevelState());
    RegisterState(new MiqatLevelState());
    RegisterState(new TawafLevelState());
    RegisterState(new NewLevelState()); // Добавить здесь
}
```

### Шаг 3: Добавить константу в LevelStateIds.cs
```csharp
public const string NewLevel = "new_level";

public static readonly List<string> AllStates = new List<string>
{
    Warmup,
    Miqat,
    Tawaf,
    NewLevel // Добавить в список
};
```

## 📖 Жизненный Цикл Состояния

```
Initialize() → Enter() → Update() (loop) → Exit()
                  ↓                           ↑
              OnPause() ←→ OnResume()         │
                                              │
                        CompleteLevel() ──────┘
```

## 💡 Best Practices

1. **Всегда вызывайте base методы** в переопределенных методах
2. **Подписывайтесь на события в Enter()** и отписывайтесь в Exit()
3. **Используйте LevelStateIds константы** вместо строковых литералов
4. **Сохраняйте прогресс в OnQuizComplete()** через ProgressService
5. **Награждайте через RewardSystem**, не напрямую через GameManager

## ⚠️ Важные Замечания

- Каждое состояние находит QuizSystem и RewardSystem через `FindObjectOfType<T>()`
- Состояния не сохраняются между сценами (они пересоздаются при загрузке Gameplay)
- LevelStateMachine должен быть в сцене Gameplay для работы системы
- Не забудьте вызвать `_stateMachine.CompleteLevel()` когда уровень завершен

## 🔍 Debug Tips

Включите логирование для отслеживания переходов:
```csharp
// В LevelStateMachine добавьте детальное логирование
Debug.Log($"[StateMachine] Current: {_currentState?.StateId ?? "none"}");
```

Проверьте регистрацию состояний:
```csharp
// В Awake() LevelStateMachine
Debug.Log($"[StateMachine] Registered {_states.Count} states");
```

## 📚 Дополнительная Документация

- **ARCHITECTURE.md** - Полное описание архитектуры
- **SCENE_SETUP.md** - Руководство по настройке сцены
- **STATE_MACHINE_DIAGRAM.md** - Диаграммы и схемы

---

*Последнее обновление: 21 февраля 2026*

