# ✅ TheoryCardsManager - FIXED: Runtime Theory Cards Initialization

## 🔧 Проблема
При использовании runtime моделей (из `ContentLoaderService`) **карточки теории не создавались**:
- В `Awake()` карточки создавались только из статического контейнера (ScriptableObject)
- Когда использовались runtime модели из Google Sheets, список был пуст
- Нет метода для инициализации из `RuntimeLevelFactory`

## ✅ Решение

### 1. Улучшен метод `CreateCards()`
**Добавлено:**
- Проверка на null и пустой список
- Полное логирование процесса создания
- Логирование каждой созданной карточки

```csharp
Debug.Log($"[TheoryCardsManager] Creating {totalCount} cards as deck");
// ... создание карточек ...
Debug.Log($"[TheoryCardsManager] Created card {i}: {dataList[i].Title}");
```

### 2. Рефакторинг `InitializeTheory()` метода
**Новый метод:** (вместо старого `Initialize()`)
```csharp
public void InitializeTheory()
{
    if (_isInitialized)
    {
        Debug.LogWarning("[TheoryCardsManager] Already initialized! Call Reset() to reinitialize.");
        return;
    }

    _audioService = GameManager.Instance?.GetService<AudioService>();
    
    if (_cardPrefab == null)
    {
        Debug.LogError("[TheoryCardsManager] Card prefab is not assigned!");
        return;
    }

    var dataList = CardDataList;
    if (dataList == null || dataList.Count == 0)
    {
        Debug.LogWarning("[TheoryCardsManager] No data to create cards!");
        return;
    }
    
    Debug.Log($"[TheoryCardsManager] Initializing with {dataList.Count} cards");
    CreateCards();
    UpdateCounter();
    _isInitialized = true;
}
```

**Преимущества:**
- ✅ Явная проверка инициализации
- ✅ Полная обработка ошибок
- ✅ Подробное логирование

### 3. Расширен метод `InitializeFromRuntimeData()`
**Улучшено:**
```csharp
public void InitializeFromRuntimeData(List<TheoryCardData> runtimeCards)
{
    if (runtimeCards == null || runtimeCards.Count == 0)
    {
        Debug.LogWarning("[TheoryCardsManager] No runtime cards provided!");
        return;
    }

    Debug.Log($"[TheoryCardsManager] Initializing from runtime data: {runtimeCards.Count} cards");

    // Очищаем предыдущие карточки
    foreach (var card in _cards)
    {
        if (card != null)
            Destroy(card.gameObject);
    }
    _cards.Clear();

    // Устанавливаем данные
    _data = runtimeCards;
    CardContainer = null;

    _isInitialized = false;
    _theoryCompleted = false;
    CurrentCardIndex = 0;

    Debug.Log($"[TheoryCardsManager] Reset state and preparing to create cards");
    CreateCards();
    UpdateCounter();
}
```

### 4. Добавлен новый метод `InitializeFromRuntimeModels()`
**Новый метод для RuntimeLevelFactory:**
```csharp
public void InitializeFromRuntimeModels(string levelId)
{
    var runtimeFactory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
    
    if (runtimeFactory == null)
    {
        Debug.LogError("[TheoryCardsManager] RuntimeLevelFactory service not found!");
        return;
    }

    if (!runtimeFactory.IsContentAvailable)
    {
        Debug.LogWarning("[TheoryCardsManager] Runtime content is not loaded yet...");
        return;
    }

    var runtimeTheoryCards = runtimeFactory.BuildTheoryCards(levelId);
    
    if (runtimeTheoryCards == null || runtimeTheoryCards.Count == 0)
    {
        Debug.LogWarning($"[TheoryCardsManager] No theory cards found for level '{levelId}'");
        return;
    }

    Debug.Log($"[TheoryCardsManager] Initializing from runtime models: {runtimeTheoryCards.Count} cards");

    // Очищаем предыдущие карточки
    foreach (var card in _cards)
    {
        if (card != null)
            Destroy(card.gameObject);
    }
    _cards.Clear();

    // Устанавливаем данные
    _data = runtimeTheoryCards;
    CardContainer = null;

    _isInitialized = false;
    _theoryCompleted = false;
    CurrentCardIndex = 0;

    Debug.Log($"[TheoryCardsManager] Creating {runtimeTheoryCards.Count} cards from runtime models");
    CreateCards();
    UpdateCounter();
}
```

---

## 🎯 Как это работает теперь

### Статический контейнер (как было)
```
1. Awake() вызывает InitializeTheory()
2. CardDataList возвращает данные из CardContainer (ScriptableObject)
3. CreateCards() создаёт карточки
4. Карточки готовы
```

### Runtime данные (новое)
```
1. ContentLoaderService загружает контент из Google Sheets
2. RuntimeLevelFactory преобразует данные в TheoryCardData
3. Вызываем: theoryManager.InitializeFromRuntimeModels(levelId)
4. Метод получает карточки из RuntimeLevelFactory
5. CreateCards() создаёт карточки
6. Карточки готовы
```

---

## 📊 Диаграмма потока

```
┌─────────────────────────────────────────────┐
│ TheoryCardsManager.Awake()                  │
├─────────────────────────────────────────────┤
│ InitializeTheory()                          │
│ └─ CreateCards() из CardContainer           │
│    └─ Карточки готовы                       │
└─────────────────────────────────────────────┘

                    ИЛИ

┌─────────────────────────────────────────────┐
│ ContentLoaderService.OnLoadComplete         │
├─────────────────────────────────────────────┤
│ Call: theoryManager.InitializeFromRuntimeModels(levelId) │
│ └─ GetService<RuntimeLevelFactory>()        │
│ └─ runtimeFactory.BuildTheoryCards(levelId) │
│ └─ CreateCards() из runtime данных          │
│    └─ Карточки готовы                       │
└─────────────────────────────────────────────┘
```

---

## 🧪 Тестирование

### Тест 1: Статический контейнер
```csharp
// 1. Убедитесь что CardContainer заполнен
// 2. Запустите сцену
// 3. В Console должно быть:
// [TheoryCardsManager] Initializing with X cards
// [TheoryCardsManager] Created card 0: CardTitle
// [TheoryCardsManager] Created X cards as deck
```

### Тест 2: Runtime модели
```csharp
// 1. Убедитесь что ContentLoaderService загружает контент
// 2. После OnLoadComplete вызовите:
theoryManager.InitializeFromRuntimeModels("Warmup");

// 3. В Console должно быть:
// [TheoryCardsManager] Initializing from runtime models: 3 cards
// [TheoryCardsManager] Created card 0: CardTitle
// [TheoryCardsManager] Created 3 cards as deck
```

---

## 🔍 Debug Логи

Теперь вы увидите детальное логирование:

```
[TheoryCardsManager] Initializing with 3 cards
[TheoryCardsManager] Creating 3 cards as deck
[TheoryCardsManager] Created card 0: Что такое Умра
[TheoryCardsManager] Created card 1: Типы Умры
[TheoryCardsManager] Created card 2: Отличия от Хаджа
[TheoryCardsManager] Created 3 cards as deck
```

---

## ✅ Статус

- ✅ Карточки теории теперь создаются для runtime моделей
- ✅ Поддержка обоих типов инициализации (статический + runtime)
- ✅ Полное логирование для отладки
- ✅ Правильная очистка старых карточек
- ✅ Готово к production use

**Файл:** `Assets/Scripts/Core/Theory/TheoryCardsManager.cs` ✅ FIXED

