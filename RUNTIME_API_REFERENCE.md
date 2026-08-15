# 📚 RUNTIME INITIALIZATION - COMPLETE API REFERENCE

## 🎯 Все методы для инициализации Runtime контента

---

## RuntimeLevelFactory API

### Создание данных уровня

#### `CreateLevelData(levelId: string) → LevelData`
**Что делает:** Создаёт LevelData из runtime моделей включая:
- ✅ Список вопросов квиза
- ✅ Контейнер с карточками теории
- ✅ Thumbnail из Asset Bundles (если доступен)

**Использование:**
```csharp
var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
var levelData = factory.CreateLevelData("Warmup");

Debug.Log($"Questions: {levelData.Questions.Length}");
Debug.Log($"Theory Cards: {levelData.TheoryCardContainer.Cards.Count}");
```

**Логи:**
```
[RuntimeLevelFactory] Built 7 quiz questions for 'Warmup'
[RuntimeLevelFactory] Built theory container with 3 cards for 'Warmup'
[RuntimeLevelFactory] Created LevelData for 'Warmup': 7 questions + 3 theory cards
```

---

### Инициализация компонентов

#### `InitializeTheoryForLevel(levelId: string, theoryManager: TheoryCardsManager) → void`
**Что делает:** Инициализирует карточки теории в TheoryCardsManager

**Использование:**
```csharp
var theoryManager = GetComponentInChildren<TheoryCardsManager>();
factory.InitializeTheoryForLevel("Warmup", theoryManager);

// Теперь theoryManager содержит созданные карточки
Debug.Log($"Total cards: {theoryManager.TotalCards}");
```

**Логи:**
```
[RuntimeLevelFactory] Initializing theory cards for 'Warmup'
[TheoryCardsManager] Initializing from runtime models: 3 cards for level 'Warmup'
[TheoryCardsManager] Creating 3 cards as deck
```

#### `InitializeQuizForLevel(levelId: string) → void`
**Что делает:** Инициализирует вопросы квиза в QuizService

**Использование:**
```csharp
factory.InitializeQuizForLevel("Warmup");

// Квиз готов с вопросами
var quizService = GameManager.Instance?.GetService<QuizService>();
quizService.DisplayCurrentQuestion();
```

**Логи:**
```
[RuntimeLevelFactory] Initializing quiz for 'Warmup'
[QuizService] Initialized from runtime questions: 7 questions for level 'Warmup'
```

---

### Получение отдельных компонентов

#### `BuildQuizQuestions(levelId: string) → QuizQuestion[]`
**Что делает:** Создаёт массив вопросов квиза

```csharp
var questions = factory.BuildQuizQuestions("Warmup");
Debug.Log($"Questions: {questions.Length}");
```

#### `BuildTheoryCards(levelId: string) → List<TheoryCardData>`
**Что делает:** Создаёт список карточек теории

```csharp
var cards = factory.BuildTheoryCards("Warmup");
Debug.Log($"Cards: {cards.Count}");

// Инициализировать вручную
theoryManager.InitializeFromRuntimeData(cards);
```

#### `BuildTheoryContainer(levelId: string) → TheoryCardContainer`
**Что делает:** Создаёт контейнер с карточками

```csharp
var container = factory.BuildTheoryContainer("Warmup");
Debug.Log($"Container cards: {container.Cards.Count}");
```

---

### Вспомогательные методы

#### `GetAllLevelInfos() → List<RuntimeLevelInfo>`
**Что делает:** Получить все загруженные уровни

```csharp
var levels = factory.GetAllLevelInfos();
foreach (var level in levels)
{
    Debug.Log($"Level: {level.levelId}");
}
```

#### `GetLevelInfo(levelId: string) → RuntimeLevelInfo`
**Что делает:** Получить информацию об одном уровне

```csharp
var levelInfo = factory.GetLevelInfo("Warmup");
Debug.Log($"Level name key: {levelInfo.nameKey}");
```

#### `IsContentAvailable → bool`
**Что делает:** Проверить загружен ли контент

```csharp
if (factory.IsContentAvailable)
{
    var levels = factory.GetAllLevelInfos();
}
```

#### `WaitForContentLoad(maxWaitSeconds: int) → IEnumerator`
**Что делает:** Дождаться загрузки контента (корутина)

```csharp
yield return factory.WaitForContentLoad(30);
Debug.Log("Content loaded!");
```

---

## QuizService API

### Инициализация

#### `InitializeQuiz(questions: QuizQuestion[]) → void`
**Что делает:** Инициализирует квиз со статическим массивом вопросов

```csharp
var questions = new QuizQuestion[] { ... };
quizService.InitializeQuiz(questions);
```

#### `InitializeFromRuntimeQuestions(levelId: string) → void`
**Что делает:** Инициализирует квиз из runtime моделей ContentLoaderService

```csharp
quizService.InitializeFromRuntimeQuestions("Warmup");

// Квиз автоматически начинает показывать первый вопрос
// OnQuestionDisplayed событие срабатывает
```

**Эквивалент:**
```csharp
// Вместо этого:
quizService.InitializeFromRuntimeQuestions("Warmup");

// Вы можете сделать это:
var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
var questions = factory.BuildQuizQuestions("Warmup");
quizService.InitializeQuiz(questions);
```

---

## TheoryCardsManager API

### Инициализация

#### `InitializeTheory() → void`
**Что делает:** Явно инициализирует теорию из CardContainer

```csharp
theoryManager.InitializeTheory();
Debug.Log($"Total cards: {theoryManager.TotalCards}");
```

#### `InitializeFromRuntimeData(cards: List<TheoryCardData>) → void`
**Что делает:** Инициализирует из списка карточек

```csharp
var cards = factory.BuildTheoryCards("Warmup");
theoryManager.InitializeFromRuntimeData(cards);
```

#### `InitializeFromRuntimeModels(levelId: string) → void`
**Что делает:** Инициализирует из runtime моделей

```csharp
theoryManager.InitializeFromRuntimeModels("Warmup");
```

---

## 🎯 Типичные сценарии использования

### Сценарий 1: Полная инициализация уровня

```csharp
public override void OnEnter()
{
    var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
    
    // Создаём LevelData (включает вопросы И карточки)
    var levelData = factory.CreateLevelData(LevelId);
    
    // Инициализируем компоненты
    var theoryManager = GetComponentInChildren<TheoryCardsManager>();
    factory.InitializeTheoryForLevel(LevelId, theoryManager);
    
    factory.InitializeQuizForLevel(LevelId);
    
    Debug.Log($"Level {LevelId} initialized!");
}
```

### Сценарий 2: Отдельная инициализация

```csharp
public void ShowTheory()
{
    var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
    var theoryManager = GetComponentInChildren<TheoryCardsManager>();
    
    factory.InitializeTheoryForLevel(CurrentLevelId, theoryManager);
    theoryPanel.SetActive(true);
}

public void ShowQuiz()
{
    var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
    factory.InitializeQuizForLevel(CurrentLevelId);
    quizPanel.SetActive(true);
}
```

### Сценарий 3: Получение данных для UI

```csharp
public void ShowLevelInfo(string levelId)
{
    var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
    var levelData = factory.CreateLevelData(levelId);
    
    titleText.text = levelData.LevelName;
    descriptionText.text = levelData.Description;
    
    // Показываем количество вопросов и карточек
    infoText.text = $"Questions: {levelData.Questions.Length}, " +
                    $"Theory: {levelData.TheoryCardContainer.Cards.Count}";
}
```

---

## 🔍 Debug & Проверка

### Проверить загрузку контента
```csharp
var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();

Debug.Log($"Content available: {factory.IsContentAvailable}");
Debug.Log($"Total levels: {factory.GetAllLevelInfos().Count}");

if (factory.IsContentAvailable)
{
    foreach (var level in factory.GetAllLevelInfos())
    {
        Debug.Log($"- {level.levelId}: {level.nameKey}");
    }
}
```

### Проверить данные уровня
```csharp
var levelData = factory.CreateLevelData("Warmup");

Debug.Log($"Questions: {levelData.Questions?.Length}");
Debug.Log($"Theory cards: {levelData.TheoryCardContainer?.Cards.Count}");
Debug.Log($"Thumbnail: {levelData.Thumbnail != null ? "Yes" : "No"}");
```

### Проверить инициализацию теории
```csharp
var theoryManager = GetComponentInChildren<TheoryCardsManager>();
Debug.Log($"Total cards: {theoryManager.TotalCards}");
Debug.Log($"Current card: {theoryManager.CurrentCardIndex}");
```

### Проверить инициализацию квиза
```csharp
var quizService = GameManager.Instance?.GetService<QuizService>();
// После инициализации OnQuestionDisplayed событие должно сработать
```

---

## 🎓 Резюме методов

| Метод | Из | В | Статус |
|-------|----|----|--------|
| CreateLevelData() | RuntimeModels | LevelData (с вопросами + карточками) | ✅ |
| InitializeTheoryForLevel() | RuntimeModels | TheoryCardsManager | ✅ |
| InitializeQuizForLevel() | RuntimeModels | QuizService | ✅ |
| InitializeFromRuntimeQuestions() | ContentLoaderService | QuizService | ✅ |
| InitializeFromRuntimeModels() | RuntimeLevelFactory | TheoryCardsManager | ✅ |
| InitializeFromRuntimeData() | List<TheoryCardData> | TheoryCardsManager | ✅ |

---

**Status:** 🟢 **ALL METHODS READY TO USE**

Выбирайте метод в зависимости от вашего сценария!

