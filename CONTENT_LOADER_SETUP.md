# Этап 2: ContentLoaderService - Загрузка контента из Google Sheets

## Обзор

Реализован полнофункциональный сервис для загрузки контента (локализацию, уровни, вопросы, теорию) из Google Sheets в рантайм.

## Архитектура

```
┌─────────────────────────────────────────┐
│      Google Sheets (4 листа)            │
│  - Localization (gid=0)                 │
│  - Levels (gid=1)                       │
│  - Questions (gid=2)                    │
│  - Theory (gid=3)                       │
└────────────────┬────────────────────────┘
                 │
                 ▼
    ┌─────────────────────────────┐
    │  ContentLoaderService       │
    │  - UnityWebRequest загрузка │
    │  - CSV парсинг              │
    │  - Параллельная обработка   │
    │  - Кэширование (PlayerPrefs)│
    │  - Fallback на кэш          │
    └────────────────┬────────────┘
                 │
      ┌──────────┼──────────┬────────────┐
      ▼          ▼          ▼            ▼
  LocalizationService  Levels  Questions  Theory
  (обновляется)       (Models) (Models)   (Models)
```

## Основные компоненты

### 1. ContentLoaderService (MonoBehaviour)

**Файл:** `Assets/Scripts/Services/ContentLoaderService.cs`

**Возможности:**
- Загрузка 4 CSV листов параллельно из Google Sheets
- Автоматическая повторная попытка при отсутствии интернета (configurable)
- Кэширование в PlayerPrefs для работы offline
- Парсинг CSV с учётом кавычек и специальных символов
- Прогресс-трекинг (`OnLoadProgress` event)

**Рантайм-модели данных:**
```csharp
// Информация об уровне
public class RuntimeLevelInfo
{
    public string levelId;      // Уникальный ID
    public string nameKey;      // Ключ для локализации
    public string descriptionKey; // Ключ для локализации
    public int order;           // Порядок сортировки
    public string imageBundleKey; // Для загрузки изображений
}

// Вопрос квиза
public class RuntimeQuizQuestion
{
    public string levelId;
    public string questionKey;
    public string[] optionKeys; // 4 варианта ответа
    public int correctIndex;
    public string explanationKey;
    public int gemsReward;
}

// Карточка теории
public class RuntimeTheoryCard
{
    public string levelId;
    public int order;
    public string titleKey;
    public string textKey;
    public string imageBundleKey;
}
```

**Public API:**
```csharp
// Получить все уровни
List<RuntimeLevelInfo> GetAllLevels()

// Получить вопросы для уровня
List<RuntimeQuizQuestion> GetQuestionsForLevel(string levelId)

// Получить карточки теории для уровня
List<RuntimeTheoryCard> GetTheoryCardsForLevel(string levelId)

// Получить локализованный текст
string GetLocalizedText(string key, string languageCode = "ru")

// Очистить кэш
void ClearCache()
```

**События:**
```csharp
public event Action<bool> OnLoadComplete;      // true при успехе
public event Action<float> OnLoadProgress;     // 0-1
```

### 2. LocalizationService - Расширение

**Файл:** `Assets/Scripts/Services/LocalizationService.cs`

**Новый метод:**
```csharp
public void UpdateTranslationTable(Dictionary<string, Dictionary<string, string>> newTable)
```

Этот метод:
- Принимает таблицу переводов из ContentLoaderService (строковые коды языков)
- Конвертирует `"ru"`, `"en"`, `"ar"` в `Language` enum
- Обновляет внутреннюю таблицу `_table`
- Уведомляет все зарегистрированные GameTextController'ы

## Структура Google Sheets

### Лист 0: Локализация (Localization)
```
Key              | ru          | en        | ar        | ...
WARMUP_TITLE     | Разминка    | Warmup    | تدفئة     |
HOW_MANY_TIMES   | Сколько раз | How many  | كم مرة    |
...
```

### Лист 1: Уровни (Levels)
```
LevelId | NameKey      | DescriptionKey          | Order | ImageBundleKey
Warmup  | WARMUP_TITLE | WARMUP_DESCRIPTION_KEY  | 0     | warmup_img
Miqat   | MIQAT_TITLE  | MIQAT_DESCRIPTION_KEY   | 1     | miqat_img
...
```

### Лист 2: Вопросы (Questions)
```
LevelId | QuestionKey      | Option1Key | Option2Key | Option3Key | Option4Key | CorrectIndex | ExplanationKey      | GemsReward
Warmup  | Q_WARMUP_1       | OPT_1A     | OPT_1B     | OPT_1C     | OPT_1D     | 0            | EXP_WARMUP_1        | 5
...
```

### Лист 3: Теория (Theory)
```
LevelId | Order | TitleKey      | TextKey         | ImageBundleKey
Warmup  | 0     | THEORY_TITLE1 | THEORY_TEXT1    | theory_img_1
Warmup  | 1     | THEORY_TITLE2 | THEORY_TEXT2    | theory_img_2
...
```

## Использование

### 1. Настройка в сцене

1. Создайте пустой GameObject: `ContentLoader`
2. Добавьте компонент `ContentLoaderService`
3. В Inspector установите `Enable Auto Load = true`
4. Перетащите существующий `LocalizationService` компонент в поле `_localizationService`

### 2. Загрузка контента вручную

```csharp
ContentLoaderService loader = GetComponent<ContentLoaderService>();
StartCoroutine(loader.LoadAllContent());

// Подпишитесь на события
loader.OnLoadComplete += (success) => 
{
    if (success)
        Debug.Log("Контент успешно загружен!");
};

loader.OnLoadProgress += (progress) =>
{
    progressBar.value = progress; // 0-1
};
```

### 3. Доступ к загруженным данным

```csharp
// Получить все уровни
var levels = loader.GetAllLevels();

// Получить вопросы для конкретного уровня
var questions = loader.GetQuestionsForLevel("Warmup");

// Получить теорию для уровня
var theory = loader.GetTheoryCardsForLevel("Warmup");

// Получить переведённый текст
string text = loader.GetLocalizedText("WARMUP_TITLE", "ru");
```

## Кэширование

**Где:** `PlayerPrefs`
- `Content_Localization` - JSON с таблицей локализации
- `Content_Levels` - JSON с информацией об уровнях
- `Content_Questions` - JSON с вопросами
- `Content_Theory` - JSON с теорией
- `Content_LoadTimestamp` - Метка времени последней загрузки

**Когда используется:**
1. Если нет интернета и все retry попытки исчерпаны
2. При offline запуске приложения

**Очистка кэша:**
```csharp
loader.ClearCache();
```

## Обработка ошибок

**Сценарии:**
1. **Нет интернета** → Повторные попытки (configurable) → Fallback на кэш
2. **Google Sheets недоступен** → Fallback на кэш
3. **CSV парсинг ошибка** → Log warning, данные пропускаются
4. **Пустой кэш** → Пустые коллекции

**Debug:**
```
[ContentLoaderService] Starting content load...
[ContentLoaderService] Localization loaded: 150 keys
[ContentLoaderService] Levels loaded: 5 levels
[ContentLoaderService] Questions loaded: 35 questions
[ContentLoaderService] Theory loaded: 12 cards
[ContentLoaderService] Data cached successfully
[ContentLoaderService] Content loading completed!
```

## Производительность

- **Параллельная загрузка:** 4 листа одновременно (не 4 последовательных запроса)
- **CSV парсинг:** На уровне игры (main thread)
- **Память:** ~500KB-1MB в памяти в зависимости от размера контента
- **PlayerPrefs:** ~2-3MB на кэш (в зависимости от объёма контента)

## Архитектурные решения

### 1. Почему рантайм-модели, а не ScriptableObjects?

- **Плюсы SO:** Инспектор, сохранение
- **Плюсы RuntimeModels:** Простая загрузка с интернета, JSON сериализация, нет привязки к Assets
- **Решение:** RuntimeModels для загрузки, SO создаются только при необходимости в Editor

### 2. Почему Dictionary<string, Dictionary<string, string>> в ContentLoaderService?

- CSV парсер работает со строками (ru, en, ar)
- LocalizationService работает с Language enum
- ContentLoaderService → UpdateTranslationTable конвертирует значения

### 3. Параллельная загрузка

```csharp
// Вместо:
yield return StartCoroutine(LoadL1());
yield return StartCoroutine(LoadL2());

// Делаем:
StartCoroutine(LoadL1());
StartCoroutine(LoadL2());
// Ждем все флаги
while (!IsAllDataLoaded()) yield return new WaitForSeconds(0.1f);
```

## Следующие шаги

1. **Загрузка изображений:** Добавить Image Download Manager с кэшированием
2. **Audio бандлы:** Поддержка загрузки звуков
3. **Версионирование:** Отслеживание версии контента для обновлений
4. **Delta updates:** Загрузка только изменённых данных
5. **Backend интеграция:** Вместо PlayerPrefs использовать Backend API

## Тестирование

### Unit Test шаблон:
```csharp
[Test]
public void TestCsvParsing()
{
    string csv = "Key,ru,en\nTEST,тест,test";
    var service = new ContentLoaderService();
    // Должны сработать парсеры...
}
```

### Integration Test:
```csharp
public IEnumerator TestFullLoad()
{
    var loader = GetComponent<ContentLoaderService>();
    
    bool loadComplete = false;
    loader.OnLoadComplete += (success) => loadComplete = success;
    
    yield return loader.LoadAllContent();
    
    Assert.IsTrue(loadComplete);
    Assert.Greater(loader.GetAllLevels().Count, 0);
}
```

---

**Версия:** 1.0  
**Дата:** 2026-08-12  
**Статус:** ✅ Готово к использованию

