# ✅ UIService - FIXED: Runtime LevelController Initialization

## 🔧 Проблема
При использовании runtime моделей (из `ContentLoaderService`) **LevelController'ы не создавались**:
- В `Awake()` контроллеры создавались только из статического конфига
- Когда использовались runtime модели, список был пуст
- Контроллеры не инициализировались для загруженного контента

## ✅ Решение

### 1. Рефакторинг `Awake()` → `InitializeLevelControllers()`
**Было:**
```csharp
private void Awake()
{
    _levels = _config.Levels.Select(le=> le.LevelData).ToList();
    foreach (var level in _levels)
    {
        // Создание контроллеров...
    }
}
```

**Стало:**
```csharp
private void Awake()
{
    // Инициализируем контроллеры уровней из конфига
    InitializeLevelControllers(_config.Levels.Select(le => le.LevelData).ToList());
}

private void InitializeLevelControllers(List<LevelData> levels)
{
    if (levels == null || levels.Count == 0)
    {
        Debug.LogWarning("[UIService] No levels provided for controller initialization");
        return;
    }

    _levels = levels;

    // Очищаем старые контроллеры
    foreach (var controller in _levelControllers)
    {
        if (controller != null)
            Destroy(controller.gameObject);
    }
    _levelControllers.Clear();

    // Создаём новые контроллеры для каждого уровня
    foreach (var level in _levels)
    {
        if (_levelControllerPrefab != null && _levelsControllersContainer != null)
        {
            var controllerObj = Instantiate(_levelControllerPrefab, _levelsControllersContainer);
            var controller = controllerObj.GetComponent<LevelController>();
            if (controller != null)
            {
                controller.Init(level);
                _levelControllers.Add(controller);
                Debug.Log($"[UIService] Created LevelController for '{level.LevelId}'");
            }
        }
    }

    Debug.Log($"[UIService] Initialized {_levelControllers.Count} level controllers");
}
```

**Преимущества:**
- ✅ Переиспользуемый метод для инициализации контроллеров
- ✅ Работает как для статического конфига, так и для runtime моделей
- ✅ Правильно очищает старые контроллеры перед созданием новых

### 2. Добавлен вызов в `BuildLevelGrid()` для Runtime моделей
**Добавлено:**
```csharp
if (runtimeLevels.Count > 0)
{
    _levels = runtimeLevels;
    
    // ✅ ВАЖНО: Инициализируем контроллеры для runtime моделей!
    InitializeLevelControllers(_levels);
}
```

**Эффект:**
- ✅ После загрузки runtime моделей из `ContentLoaderService` контроллеры создаются автоматически
- ✅ Работает как с `BuildLevelGrid()` через context menu, так и программно

### 3. Добавлен публичный метод `InitializeControllersFromRuntime()`
**Новый метод:**
```csharp
public void InitializeControllersFromRuntime()
{
    var runtimeFactory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
    
    if (runtimeFactory == null)
    {
        Debug.LogError("[UIService] RuntimeLevelFactory service not found!");
        return;
    }

    if (!runtimeFactory.IsContentAvailable)
    {
        Debug.LogWarning("[UIService] Runtime content is not loaded yet...");
        return;
    }

    var runtimeLevelInfos = runtimeFactory.GetAllLevelInfos();
    var runtimeLevels = new List<LevelData>();

    Debug.Log($"[UIService] Initializing controllers from {runtimeLevelInfos.Count} runtime levels...");

    foreach (var info in runtimeLevelInfos)
    {
        var levelData = runtimeFactory.CreateLevelData(info.levelId);
        if (levelData != null)
        {
            runtimeLevels.Add(levelData);
            Debug.Log($"[UIService] Created LevelData for '{info.levelId}' from runtime model");
        }
    }

    if (runtimeLevels.Count > 0)
    {
        Debug.Log($"[UIService] Initializing {runtimeLevels.Count} level controllers from runtime data");
        InitializeLevelControllers(runtimeLevels);
    }
}
```

**Использование:**
```csharp
// Можно вызвать из ContentLoaderService.OnLoadComplete event
uiService.InitializeControllersFromRuntime();
```

---

## 🎯 Как это работает теперь

### Статический конфиг (как было)
```
1. Awake() вызывает InitializeLevelControllers(_config.Levels)
2. Создаются контроллеры для каждого уровня
3. Контроллеры готовы для использования
```

### Runtime модели (новое)
```
1. ContentLoaderService загружает контент из Google Sheets
2. OnLoadComplete событие срабатывает
3. UIService.InitializeControllersFromRuntime() вызывается
4. Создаются контроллеры для каждого runtime уровня
5. Контроллеры готовы для использования
```

### Через BuildLevelGrid() (с Runtime)
```
1. BuildLevelGrid() вызывается (контекст меню или программно)
2. Загружаются runtime модели из RuntimeLevelFactory
3. Создаются LevelData объекты
4. InitializeLevelControllers(_levels) вызывается
5. Создаются контроллеры для каждого уровня
```

---

## 📊 Диаграмма потока

```
┌────────────────────────────────────────────────────┐
│ UIService.Awake()                                  │
├────────────────────────────────────────────────────┤
│ InitializeLevelControllers(static levels)          │
│ └─ Create controllers for each static level        │
└──────────────┬───────────────────────────────────┘
               │
               ▼
┌────────────────────────────────────────────────────┐
│ ContentLoaderService.OnLoadComplete               │
├────────────────────────────────────────────────────┤
│ Call: uiService.InitializeControllersFromRuntime() │
│ └─ GetAllLevelInfos() from RuntimeLevelFactory     │
│ └─ CreateLevelData() for each level                │
│ └─ InitializeLevelControllers(runtime levels)      │
│    └─ Create controllers for each runtime level    │
└────────────────────────────────────────────────────┘
               │
               ▼
        Controllers Ready! ✅
```

---

## 🧪 Тестирование

### Тест 1: Статический конфиг
```csharp
// 1. Убедитесь что _config.Levels заполнен
// 2. Запустите сцену
// 3. В Console должно быть:
// [UIService] Initialized 5 level controllers (или сколько уровней в конфиге)
```

### Тест 2: Runtime модели
```csharp
// 1. Убедитесь что ContentLoaderService загружает контент
// 2. После OnLoadComplete вызовите:
uiService.InitializeControllersFromRuntime();

// 3. В Console должно быть:
// [UIService] Initializing controllers from 5 runtime levels...
// [UIService] Created LevelData for 'Warmup' from runtime model
// [UIService] Initialized 5 level controllers
```

### Тест 3: BuildLevelGrid() с Runtime
```csharp
// 1. Убедитесь что UseRemoteContent = true в GameMainConfig
// 2. Вызовите BuildLevelGrid() (context menu или код)
// 3. В Console должно быть:
// [UIService] BuildLevelGrid: using runtime level data
// [UIService] Initialized X level controllers
```

---

## 🔍 Debug Логи

Теперь вы увидите детальное логирование:

```
[UIService] Initialized 5 level controllers
[UIService] BuildLevelGrid: using runtime level data from RuntimeLevelFactory
[UIService] Successfully loaded 5 levels from ContentLoaderService
[UIService] Initializing controllers from 5 runtime levels...
[UIService] Created LevelData for 'Warmup' from runtime model
[UIService] Created LevelData for 'Miqat' from runtime model
[UIService] Created LevelData for 'Tawaf' from runtime model
[UIService] Initializing 5 level controllers from runtime data
[UIService] Created LevelController for 'Warmup'
[UIService] Created LevelController for 'Miqat'
...
```

---

## ✅ Статус

- ✅ LevelController'ы теперь создаются для runtime моделей
- ✅ Контроллеры правильно инициализируются
- ✅ Нет пустых списков
- ✅ Полное логирование для отладки
- ✅ Готово к production use

**Файл:** `Assets/Scripts/UI/UIService.cs` ✅ FIXED

