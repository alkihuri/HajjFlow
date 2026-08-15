# 🚀 Быстрый старт: ContentLoaderService

## За 5 минут до работающей системы

### Шаг 1️⃣: Подготовка сцены

1. Откройте главную сцену проекта
2. Создайте новый **Empty GameObject** → назовите его `ContentLoader`
3. Добавьте компонент **ContentLoaderService**

```
Hierarchy:
├── Canvas
├── GameManager
├── ContentLoader ← новый объект
│   └── ContentLoaderService (script)
└── ... остальное
```

### Шаг 2️⃣: Конфигурация

В Inspector компонента ContentLoaderService:
- ✅ **Enable Auto Load** = true (автоматическая загрузка при Start)
- ✅ **Retry Delay Seconds** = 5
- ✅ **Max Retries** = 3

Перетащите **LocalizationService** компонент в поле **Localization Service**.

```
ContentLoaderService
├── Enable Auto Load: ✓
├── Retry Delay Seconds: 5
├── Max Retries: 3
└── Localization Service: [LocalizationService]
```

### Шаг 3️⃣: Проверка работы

Запустите сцену и посмотрите в Console:

```
[ContentLoaderService] Starting content load...
[ContentLoaderService] Localization loaded: 150 keys
[ContentLoaderService] Levels loaded: 5 levels
[ContentLoaderService] Questions loaded: 35 questions
[ContentLoaderService] Theory loaded: 12 cards
[ContentLoaderService] Data cached successfully
[ContentLoaderService] Content loading completed!
```

✅ **Готово!** Контент загружен и закэширован.

---

## 📚 Основные операции

### Получить все уровни
```csharp
var contentLoader = GetComponent<ContentLoaderService>();
var levels = contentLoader.GetAllLevels();

foreach (var level in levels)
{
    Debug.Log($"Level: {level.levelId}");
}
```

### Получить вопросы для уровня
```csharp
var questions = contentLoader.GetQuestionsForLevel("Warmup");
// questions: List<ContentLoaderService.RuntimeQuizQuestion>

foreach (var q in questions)
{
    Debug.Log($"Q: {q.questionKey}");
    Debug.Log($"Options: {string.Join(", ", q.optionKeys)}");
    Debug.Log($"Correct: {q.correctIndex}");
}
```

### Получить теорию для уровня
```csharp
var theory = contentLoader.GetTheoryCardsForLevel("Warmup");
// theory: List<ContentLoaderService.RuntimeTheoryCard>

foreach (var card in theory)
{
    Debug.Log($"[{card.order}] {card.titleKey}");
    Debug.Log($"Text: {card.textKey}");
}
```

### Получить переведённый текст
```csharp
string title = contentLoader.GetLocalizedText("WARMUP_TITLE", "ru");
Debug.Log(title); // "Разминка"
```

---

## 🎯 Подписка на события

### Событие: Загрузка завершена
```csharp
contentLoader.OnLoadComplete += (success) =>
{
    if (success)
        Debug.Log("✓ Контент успешно загружен!");
    else
        Debug.LogError("✗ Ошибка загрузки контента");
};
```

### Событие: Прогресс загрузки
```csharp
contentLoader.OnLoadProgress += (progress) =>
{
    progressBar.value = progress; // 0-1
    Debug.Log($"Прогресс: {progress * 100:F0}%");
};
```

---

## 🔧 Продвинутое использование

### Вручную запустить загрузку
```csharp
StartCoroutine(contentLoader.LoadAllContent());
```

### Очистить кэш и перезагрузить
```csharp
contentLoader.ClearCache();
StartCoroutine(contentLoader.LoadAllContent());
```

### Проверить кэш
```csharp
// Данные сохранены в PlayerPrefs:
string cache = PlayerPrefs.GetString("Content_Localization", "");
if (string.IsNullOrEmpty(cache))
    Debug.Log("Кэш пуст - используется fallback");
```

---

## 📊 Структура данных

### RuntimeLevelInfo
```csharp
var level = levels[0];
// level.levelId           → "Warmup"
// level.nameKey           → "WARMUP_TITLE"
// level.descriptionKey    → "WARMUP_DESCRIPTION_KEY"
// level.order             → 0
// level.imageBundleKey    → "warmup_img"
```

### RuntimeQuizQuestion
```csharp
var question = questions[0];
// question.levelId         → "Warmup"
// question.questionKey     → "Q_WARMUP_1"
// question.optionKeys[]    → ["OPT_1A", "OPT_1B", "OPT_1C", "OPT_1D"]
// question.correctIndex    → 0 (первый вариант)
// question.explanationKey  → "EXP_WARMUP_1"
// question.gemsReward      → 5
```

### RuntimeTheoryCard
```csharp
var card = theory[0];
// card.levelId         → "Warmup"
// card.order           → 0
// card.titleKey        → "THEORY_TITLE1"
// card.textKey         → "THEORY_TEXT1"
// card.imageBundleKey  → "theory_img_1"
```

---

## ⚙️ Конфигурация

### Изменение параметров
```csharp
// В коде можно изменить поведение:
public class ContentLoaderService : MonoBehaviour
{
    private bool _enableAutoLoad = true;      // автозагрузка
    private float _retryDelaySeconds = 5f;    // задержка между retry
    private int _maxRetries = 3;              // количество попыток
}
```

### Изменение URL'ов Google Sheets
Если вам нужны свои листы, измените URL'ы в классе `GoogleSheetsUrls`:

```csharp
private static class GoogleSheetsUrls
{
    public const string Localization = 
        "ВАШ_URL?gid=0&single=true&output=csv";
    
    public const string Levels = 
        "ВАШ_URL?gid=1&single=true&output=csv";
    
    public const string Questions = 
        "ВАШ_URL?gid=2&single=true&output=csv";
    
    public const string Theory = 
        "ВАШ_URL?gid=3&single=true&output=csv";
}
```

---

## 🐛 Отладка проблем

### Проблема: Контент не загружается
```csharp
// Проверьте интернет соединение
if (Application.internetReachability == NetworkReachability.NotReachable)
    Debug.LogError("Нет интернета!");

// Проверьте Google Sheets доступны ли
// Попробуйте в браузере открыть URL из GoogleSheetsUrls
```

### Проблема: Кэш не используется
```csharp
// Проверьте PlayerPrefs
Debug.Log(PlayerPrefs.GetString("Content_Localization", "NO CACHE"));

// Проверьте разрешение на запись PlayerPrefs
PlayerPrefs.SetString("test", "test");
PlayerPrefs.DeleteKey("test");
```

### Проблема: Вопросы не загружаются
```csharp
// Убедитесь что CSV лист содержит:
// LevelId | QuestionKey | Option1Key | Option2Key | ...
// Warmup  | Q_1         | O1         | O2         | ...

// Проверьте в ConsoleContentLoader пытается ли парсить
```

---

## 📋 Пример полной интеграции

```csharp
using UnityEngine;
using HajjFlow.Services;

public class GameInitializer : MonoBehaviour
{
    private ContentLoaderService _contentLoader;

    private void Start()
    {
        _contentLoader = GetComponent<ContentLoaderService>();
        
        // Подписываемся на события
        _contentLoader.OnLoadComplete += OnContentLoaded;
        _contentLoader.OnLoadProgress += OnLoadProgress;
    }

    private void OnContentLoaded(bool success)
    {
        if (success)
        {
            Debug.Log("✓ Инициализация завершена!");
            
            // Используем загруженные данные
            var levels = _contentLoader.GetAllLevels();
            InitializeUI(levels);
        }
        else
        {
            Debug.LogError("✗ Ошибка инициализации");
        }
    }

    private void OnLoadProgress(float progress)
    {
        // Обновляем splash screen
        splashScreen.FillAmount = progress;
    }

    private void InitializeUI(List<ContentLoaderService.RuntimeLevelInfo> levels)
    {
        foreach (var level in levels)
        {
            string name = _contentLoader.GetLocalizedText(level.nameKey);
            CreateLevelButton(name, level.levelId);
        }
    }

    private void CreateLevelButton(string name, string levelId)
    {
        // Ваш код создания кнопки
    }
}
```

---

## ✅ Чек-лист готовности

- [ ] ContentLoaderService добавлен на сцену
- [ ] Enable Auto Load включен
- [ ] LocalizationService подключен
- [ ] Console показывает "Content loading completed!"
- [ ] Кэш сохранён в PlayerPrefs
- [ ] Вы можете получить список уровней
- [ ] Вы можете получить вопросы для уровня
- [ ] Локализованные тексты отображаются правильно

---

## 🎓 Дополнительные ресурсы

- **CONTENT_LOADER_SETUP.md** - полная документация
- **ContentLoaderExample.cs** - примеры кода
- **STAGE2_COMPLETION_CHECKLIST.md** - полный чек-лист

---

**Система готова! 🎉**

Любые вопросы → смотрите документацию или примеры кода.

