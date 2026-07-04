# Обновление архитектуры HajjFlow

## Обзор изменений

Проведён рефакторинг архитектуры проекта для перехода от захардкоженных уровней к полностью динамической и гибкой системе.

---

## 1. Единый GameConfig вместо множества ScriptableObject

### Было (старая архитектура)
- Каждый уровень = отдельный ScriptableObject файл (`Warmup_LevelData.asset`, `Miqat_LevelData.asset`, и т.д.)
- `LevelData` наследовался от `ScriptableObject`
- Уровни загружались через `Resources.LoadAll<LevelData>("SO/Levels")`
- UIService хранил список `[SerializeField] List<LevelData> _levels` с ссылками на каждый SO

### Стало (новая архитектура)
- **Один** `GameConfig` ScriptableObject содержит **все** уровни
- `LevelData` — обычный `[Serializable]` класс (не ScriptableObject)
- Все уровни хранятся в `GameConfig._levels` списке
- Добавление/удаление уровней — просто редактирование списка в одном файле

### Новые файлы
- `Assets/Scripts/Data/GameConfig.cs` — единый конфиг игры

### Изменённые файлы
- `Assets/Scripts/Data/LevelData.cs` — из ScriptableObject в [Serializable] класс

### Новые поля в LevelData
| Поле | Тип | Описание |
|------|-----|----------|
| `TheoryBlockCount` | int | Количество блоков теории перед квизом |
| `TheoryContainerPath` | string | Путь к TheoryCardContainer в Resources |
| `ImageUrl` | string | URL изображения для загрузки из Google Sheets |
| `SortOrder` | int | Порядок сортировки в списке уровней |

---

## 2. Убраны захардкоженные уровни

### Было
```csharp
// GameStateIds.cs — жёстко зашитые константы
public const string Warmup = "Warmup";
public const string Miqat  = "Miqat";
public const string Tawaf  = "Tawaf";
public const string Sa3i   = "Sa3i";
public const string Arafat = "Arafat";

// GameStateMachine.cs — регистрация каждого уровня вручную
RegisterState(new LevelState(GameStateIds.Warmup, theoryBlockCount: 1));
RegisterState(new LevelState(GameStateIds.Miqat, theoryBlockCount: 1));
RegisterState(new LevelState(GameStateIds.Tawaf, theoryBlockCount: 1));
// ...

// StageCompletionService.cs — switch по имени уровня
return levelId switch
{
    "Warmup" => VerifyWarmupStage(stageIndex),
    "Miqat" => VerifyMiqatStage(stageIndex),
    "Tawaf" => VerifyTawafStage(stageIndex),
    _ => false
};

// UIService.cs — отдельные методы для каждого уровня
public void ShowWarmUpTheoryUI() => ShowTheoryUI(GameStateIds.Warmup);
public void ShowMiqatTheoryUI() => ShowTheoryUI(GameStateIds.Miqat);
```

### Стало
```csharp
// GameStateIds.cs — динамический список из конфига
public static readonly List<string> LevelStates = new List<string>();

public static void InitializeFromConfig(GameConfig config)
{
    LevelStates.Clear();
    foreach (var level in config.Levels)
        LevelStates.Add(level.LevelId);
}

// GameStateMachine.cs — автоматическая регистрация из GameConfig
foreach (var level in _gameConfig.Levels)
{
    var levelState = new LevelState(level.LevelId, level.TheoryBlockCount);
    RegisterState(levelState);
}

// StageCompletionService.cs — проверка через GameConfig
var level = config.GetLevel(levelId);
return stageIndex < level.TheoryBlockCount;

// UIService.cs — всё через универсальный ShowTheoryUI(levelId)
```

### Изменённые файлы
| Файл | Что изменилось |
|------|---------------|
| `GameStateIds.cs` | Убраны константы Warmup/Miqat/Tawaf/Sa3i/Arafat. Добавлен `InitializeFromConfig()` |
| `GameStateMachine.cs` | Добавлено поле `GameConfig`. Уровни регистрируются динамически |
| `StageCompletionService.cs` | Убраны `VerifyWarmupStage`, `VerifyMiqatStage`, `VerifyTawafStage`. Верификация через GameConfig |
| `UIService.cs` | Убраны `ShowWarmUpTheoryUI`, `ShowMiqatTheoryUI`, `ShowTawafTheoryUI`, `ShowSa3iTheoryUI`. Убран `OnValidate` с загрузкой SO. Уровни берутся из GameConfig |
| `LevelManager.cs` | `StartLevel(level)` теперь использует `level.LevelId` вместо `GameStateIds.Warmup` |

### Удалённые файлы
| Файл | Причина |
|------|---------|
| `WarmUpTheoryCard.cs` | Уровне-специфичный класс. Заменён универсальным `SimpleTheoryCard` |

---

## 3. Подготовка к интеграции с Google Sheets

### Планируемый флоу
```
Google Sheets → JSON → GameConfig
```

1. В Google Sheets каждая страница (sheet) = один уровень
2. На странице указаны: вопросы, теория, ссылки на изображения
3. Данные экспортируются в JSON и импортируются в GameConfig
4. Изображения скачиваются по URL (`LevelData.ImageUrl`) и кэшируются локально

### Уже реализовано
- `LevelData.ImageUrl` — поле для URL изображения
- `GameConfig.ImportLevelFromJson()` — импорт уровня из JSON
- `GameConfig.ImportAllJsonFiles()` — массовый импорт всех JSON файлов
- Динамическая регистрация уровней без пересборки кода

### Следующие шаги
- Реализовать загрузку изображений по URL с кэшированием
- Добавить сервис синхронизации с Google Sheets API
- Автоматическая генерация JSON из Google Sheets

---

## 4. Диаграмма новой архитектуры

```
┌─────────────────────────────────────────────────────┐
│                    GameConfig (SO)                   │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌───────────┐ │
│  │ Level 1 │ │ Level 2 │ │ Level 3 │ │ Level N.. │ │
│  │ (data)  │ │ (data)  │ │ (data)  │ │ (data)    │ │
│  └─────────┘ └─────────┘ └─────────┘ └───────────┘ │
│  Добавляй/удаляй уровни свободно!                   │
└─────────────────────┬───────────────────────────────┘
                      │
          ┌───────────┴───────────┐
          ▼                       ▼
   GameStateMachine          UIService
   (динамическая             (динамическая
    регистрация               генерация
    состояний)                UI тайлов)
          │                       │
          ▼                       ▼
   LevelState (универсальный)  LevelController
   - theoryBlockCount из       - создаётся для каждого
     LevelData                   уровня из GameConfig
   - questions из LevelData    - загружает теорию и квиз
```

## 5. Как добавить новый уровень

1. Откройте `GameConfig` ассет в Inspector
2. Нажмите `+` в списке Levels
3. Заполните поля: `LevelId`, `LevelName`, `Description`, `Questions`
4. Или используйте `Context Menu → Import Level from JSON`
5. Готово! Уровень автоматически появится в игре

Код менять **не нужно**.

---

## 6. Список всех изменённых файлов

### Новые файлы
- `Assets/Scripts/Data/GameConfig.cs`
- `update_architecture.md` (этот файл)

### Изменённые файлы
- `Assets/Scripts/Data/LevelData.cs`
- `Assets/Scripts/Core/States/LevelStateIds.cs` (GameStateIds)
- `Assets/Scripts/Core/States/GameStateMachine.cs`
- `Assets/Scripts/Services/StageCompletionService.cs`
- `Assets/Scripts/UI/UIService.cs`
- `Assets/Scripts/Core/LevelManager.cs`

### Удалённые файлы
- `Assets/Scripts/Core/Theory/WarmUpTheoryCard.cs`

### Без изменений (но совместимы с новой архитектурой)
- `LevelState.cs` — работает с любым LevelId
- `LevelController.cs` — работает с любым LevelData
- `LevelTileUI.cs` — работает с любым LevelData
- `QuizSystem.cs`, `QuizService.cs` — не зависят от конкретных уровней
- `ProgressService.cs` — работает по LevelId строке
- `TheoryCardsManager.cs` — работает с любым TheoryCardContainer
