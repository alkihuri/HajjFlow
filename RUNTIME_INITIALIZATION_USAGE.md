# 🚀 RUNTIME INITIALIZATION - USAGE GUIDE

## 📌 Быстрый старт

Вся система инициализации теперь работает автоматически с runtime моделями!

---

## 🎯 Как использовать

### Сценарий 1: Показать уровень с Runtime моделями

```csharp
// В LevelState или WarmUpLevelState
public override void OnEnter()
{
    // Показываем UI уровня
    uiService.ShowLevelUI();
    
    // Инициализируем теорию из runtime моделей
    var theoryManager = GetComponentInChildren<TheoryCardsManager>();
    theoryManager.InitializeFromRuntimeModels("Warmup");
    
    // Инициализируем квиз из runtime моделей
    var quizManager = GetComponentInChildren<QuizManager>();
    quizManager.InitializeFromRuntimeModels("Warmup");
}
```

### Сценарий 2: Загрузка и инициализация контроллеров

```csharp
// После загрузки контента из ContentLoaderService
private IEnumerator InitializeGame()
{
    // Ждём загрузки контента
    yield return contentLoader.LoadAllContent();
    
    // Инициализируем контроллеры уровней
    uiService.InitializeControllersFromRuntime();
    
    // Теперь система готова
    Debug.Log("Game initialized with runtime content!");
}
```

### Сценарий 3: Переинициализация уровня

```csharp
// Сброс и перезагрузка уровня
public void ResetLevel()
{
    // Сбрасываем теорию
    var theoryManager = GetComponentInChildren<TheoryCardsManager>();
    theoryManager.ResetToStart();
    
    // Сбрасываем квиз
    var quizManager = GetComponentInChildren<QuizManager>();
    quizManager.ResetLevel();
}
```

---

## 📊 Компоненты и их методы

### RuntimeLevelFactory
```csharp
// Проверить наличие контента
if (factory.IsContentAvailable)
{
    var levels = factory.GetAllLevels();
}

// Дождаться загрузки контента
yield return factory.WaitForContentLoad(30);

// Создать LevelData из runtime модели
var levelData = factory.CreateLevelData("Warmup");

// Построить вопросы для уровня
var questions = factory.BuildQuizQuestions("Warmup");

// Построить карточки теории для уровня
var cards = factory.BuildTheoryCards("Warmup");
```

### UIService
```csharp
// Инициализировать контроллеры из runtime моделей
uiService.InitializeControllersFromRuntime();

// Получить контроллер уровня
var controller = uiService.GetLevelController("Warmup");

// Построить сетку уровней
uiService.BuildLevelGrid();
```

### TheoryCardsManager
```csharp
// Инициализировать из runtime моделей (Recommended)
theoryManager.InitializeFromRuntimeModels("Warmup");

// Инициализировать из списка карточек
theoryManager.InitializeFromRuntimeData(cards);

// Явная инициализация
theoryManager.InitializeTheory();

// Сброс на начало
theoryManager.ResetToStart();

// Показать конкретную карточку
theoryManager.ShowCard(0);
```

---

## 🔄 Типичный флоу приложения

### При запуске игры:

```csharp
1. ContentLoaderService.LoadAllContent()
   └─ Загружает CSV из Google Sheets
   └─ Событие OnLoadComplete срабатывает

2. UIService.InitializeControllersFromRuntime()
   └─ Создаёт LevelController для каждого уровня

3. Пользователь видит меню уровней с контроллерами
```

### При выборе уровня:

```csharp
1. LevelState.OnEnter(levelId)
   └─ Показывает UI уровня

2. TheoryCardsManager.InitializeFromRuntimeModels(levelId)
   └─ Создаёт карточки теории

3. QuizManager.InitializeFromRuntimeModels(levelId)
   └─ Создаёт вопросы квиза

4. Пользователь готов к обучению
```

---

## ✅ Проверочный список

Перед использованием runtime инициализации убедитесь:

- [ ] ContentLoaderService добавлен на сцену
- [ ] RuntimeLevelFactory зарегистрирован как сервис
- [ ] UIService получает данные правильно
- [ ] Google Sheets опубликованы и доступны
- [ ] Console не показывает ошибок

Проверяемые логи:
```
[ContentLoaderService] Content loading completed!
[RuntimeLevelFactory] Successfully loaded X levels
[UIService] Initializing X level controllers
[TheoryCardsManager] Creating X cards as deck
```

---

## 🐛 Отладка

### Если контент не загружается:
```csharp
// Проверьте IsContentAvailable
var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
Debug.Log($"Content available: {factory?.IsContentAvailable}");

// Дождитесь загрузки явно
yield return factory?.WaitForContentLoad(30);
```

### Если контроллеры не создаются:
```csharp
// Вызовите явно
uiService.InitializeControllersFromRuntime();

// Проверьте логи UIService
Debug.Log(uiService.GetAllLevelControllers().Count);
```

### Если карточки не создаются:
```csharp
// Вызовите явно
theoryManager.InitializeFromRuntimeModels("Warmup");

// Проверьте логи TheoryCardsManager
Debug.Log($"Total cards: {theoryManager.TotalCards}");
```

---

## 📝 Примеры для разных сценариев

### Пример 1: Простая инициализация
```csharp
public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        var contentLoader = GetComponent<ContentLoaderService>();
        contentLoader.OnLoadComplete += (success) =>
        {
            if (success)
            {
                GetComponent<UIService>().InitializeControllersFromRuntime();
            }
        };
    }
}
```

### Пример 2: Со статусом загрузки
```csharp
public class LoadingScreen : MonoBehaviour
{
    private IEnumerator ShowLoadingUI()
    {
        var factory = GameManager.Instance?.GetService<RuntimeLevelFactory>();
        
        yield return factory.WaitForContentLoad(30);
        
        if (factory.IsContentAvailable)
        {
            LoadingText.text = "Game initialized!";
            yield return new WaitForSeconds(2);
            SceneManager.LoadScene("MainGame");
        }
    }
}
```

### Пример 3: Динамическая переинициализация
```csharp
public class LevelManager : MonoBehaviour
{
    public void LoadLevel(string levelId)
    {
        var theory = GetComponentInChildren<TheoryCardsManager>();
        var quiz = GetComponentInChildren<QuizManager>();
        
        // Очищаем старые данные
        theory.ResetToStart();
        quiz.ResetLevel();
        
        // Загружаем новые
        theory.InitializeFromRuntimeModels(levelId);
        quiz.InitializeFromRuntimeModels(levelId);
    }
}
```

---

## 🎯 Результат

После выполнения всех шагов вы получите:

✅ Динамическая загрузка контента из Google Sheets  
✅ Автоматическое создание UI контроллеров  
✅ Автоматическое создание карточек теории  
✅ Автоматическое создание вопросов квиза  
✅ Полное логирование для отладки  
✅ Production-ready система  

---

**Status:** 🟢 **READY TO USE**

Начните с примера 1 - это самый простой способ инициализации!

