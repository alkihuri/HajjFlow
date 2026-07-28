# GameMainConfig - Главный конфиг игры

## Описание

**GameMainConfig** - это главный конфиг, который управляет всеми уровнями игры и связывает их теорию и квизы в единую систему.

## Структура

```
GameMainConfig
├── Levels[]
│   ├── LevelId          (уникальный идентификатор)
│   ├── LevelData        (квиз - вопросы, ответы, награды)
│   └── TheoryContainer  (теория - карточки для изучения)
```

## Как использовать

### 1️⃣ Создание конфига

```
RMB на папке Assets/Configs → Create → Manasik → Game Main Config
```

Назовите его `GameMainConfig.asset` и разместите в удобном месте.

### 2️⃣ Импорт квизов (LevelData)

**Подготовка JSON файлов:**

Каждый JSON файл должен начинаться с метаданных уровня (первый элемент массива):

```json
[
  {
    "LevelId": "Warmup",
    "LevelName": "Подготовка",
    "Description": "Введение в Хадж"
  },
  {
    "QuestionText": "Какой месяц месяца Хаджа?",
    "Options": ["Зуль-Каада", "Зуль-Хиджа", "Мухаррам", "Сафар"],
    "CorrectAnswerIndex": 1,
    "Explanation": "Хадж совершается в месяц Зуль-Хиджа",
    "GemsReward": 5
  },
  {
    "QuestionText": "...",
    "Options": [...],
    "CorrectAnswerIndex": 0,
    "Explanation": "...",
    "GemsReward": 5
  }
]
```

**Импорт:**

1. Откройте `GameMainConfig` в Inspector
2. RMB → `Import All Quiz Files from Folder`
3. Выберите папку с JSON файлами квизов
4. Система автоматически создаст `LevelData` ассеты в папке `GameMainConfig/Levels/`

### 3️⃣ Импорт теории (TheoryCardContainer)

**Подготовка JSON файлов:**

Каждый JSON файл должен начинаться с идентификатора уровня:

```json
[
  {
    "Id": "Warmup"
  },
  {
    "Title": "Что такое Умра?",
    "Text": "Умра - это паломничество, которое можно совершать в любое время года..."
  },
  {
    "Title": "История Хаджа",
    "Text": "Хадж - это один из пяти столпов Ислама..."
  }
]
```

**Импорт:**

1. RMB → `Import All Theory Files from Folder`
2. Выберите папку с JSON файлами теории
3. Система автоматически создаст `TheoryCardContainer` и `TheoryCardData` ассеты в папке `GameMainConfig/Theory/`

### 4️⃣ Связывание теории с квизом

**Автоматическое связывание:**

1. RMB → `Link Theory to Quiz by LevelId`
2. Система найдёт совпадающие `LevelId` и свяжет их автоматически

**Результат:** Теперь каждый уровень имеет и квиз, и теорию!

### 5️⃣ Полный импорт (квиз + теория за раз)

1. RMB → `Import All Levels (Theory + Quiz)`
2. Выберите папку с квизами
3. Выберите папку с теорией
4. Система сделает всё сама: импортирует данные и свяжет их

## Структура папок после импорта

```
Assets/Configs/
├── GameMainConfig.asset
├── Levels/
│   ├── Warmup_LevelData.asset
│   ├── Miqat_LevelData.asset
│   └── ...
└── Theory/
    ├── Warmup/
    │   ├── Warmup_TheoryContainer.asset
    │   ├── Warmup_Card_00.asset
    │   ├── Warmup_Card_01.asset
    │   ├── Warmup_Card_02.asset
    │   └── ...
    ├── Miqat/
    │   ├── Miqat_TheoryContainer.asset
    │   ├── Miqat_Card_00.asset
    │   ├── Miqat_Card_01.asset
    │   └── ...
    ├── Tawaf/
    │   ├── Tawaf_TheoryContainer.asset
    │   ├── Tawaf_Card_00.asset
    │   └── ...
    └── ...
```

## Как получить данные из кода

```csharp
// Получить весь уровень
GameMainConfig config = Resources.Load<GameMainConfig>("Configs/GameMainConfig");
var levelEntry = config.GetLevelEntry("Warmup");

// Получить только квиз
LevelData levelData = config.GetLevelData("Warmup");
QuizQuestion[] questions = levelData.Questions;

// Получить только теорию
TheoryCardContainer theory = config.GetTheoryContainer("Warmup");
List<TheoryCardData> cards = theory.Cards;

// Итерировать по всем уровням
foreach (var level in config.Levels)
{
    Debug.Log($"Уровень: {level.LevelId}");
    Debug.Log($"  - Вопросов: {level.LevelData.Questions.Length}");
    Debug.Log($"  - Карточек: {level.TheoryContainer.Cards.Count}");
}
```

## Функции очистки

| Функция | Описание |
|---------|---------|
| `Import All Quiz Files from Folder` | Импортирует квизы из папки |
| `Import All Theory Files from Folder` | Импортирует теорию из папки |
| `Import All Levels (Theory + Quiz)` | Импортирует всё сразу |
| `Link Theory to Quiz by LevelId` | Связывает теорию с квизом |
| `Clear All Levels` | Очищает список (ассеты остаются) |

## Что происходит при импорте?

### Импорт квизов:
1. ✅ Читает JSON файл
2. ✅ Извлекает метаданные из первого элемента (LevelId, LevelName, Description)
3. ✅ Парсит вопросы (пропускает первый элемент)
4. ✅ Создаёт ScriptableObject LevelData
5. ✅ Сохраняет в `GameMainConfig/Levels/{LevelId}_LevelData.asset`
6. ✅ Добавляет в список `Levels`

### Импорт теории:
1. ✅ Читает JSON файл
2. ✅ Извлекает LevelId из первого элемента (поле "Id")
3. ✅ Для каждой карточки (элементы с "Title"):
   - Создаёт TheoryCardData ScriptableObject
   - Сохраняет его в `GameMainConfig/Theory/{LevelId}_Card_XX.asset`
4. ✅ Создаёт TheoryCardContainer
5. ✅ Добавляет все карточки в контейнер
6. ✅ Сохраняет в `GameMainConfig/Theory/{LevelId}_TheoryContainer.asset`

### Связывание:
1. ✅ Для каждого LevelData ищет TheoryCardContainer с совпадающим LevelId
2. ✅ Если найден, связывает их через `LevelEntry.TheoryContainer`
3. ✅ Логирует результаты

## JSON формат - Квиз

```json
[
  {
    "LevelId": "Warmup",
    "LevelName": "Основы",
    "Description": "Введение в Хадж"
  },
  {
    "QuestionText": "Сколько столпов в Исламе?",
    "Options": ["3", "4", "5", "6"],
    "CorrectAnswerIndex": 2,
    "Explanation": "В Исламе 5 столпов",
    "GemsReward": 10
  }
]
```

## JSON формат - Теория

```json
[
  {
    "Id": "Warmup"
  },
  {
    "Title": "Карточка 1",
    "Text": "Текст карточки..."
  },
  {
    "Title": "Карточка 2",
    "Text": "Текст карточки..."
  }
]
```

## Ошибки и решения

| Ошибка | Причина | Решение |
|--------|---------|---------|
| No JSON files found | Папка пуста | Поместите JSON файлы в папку |
| Could not extract metadata | JSON неправильного формата | Проверьте, что первый элемент имеет LevelId |
| Could not extract LevelId from theory | JSON теории неправильного формата | Первый элемент должен иметь поле "Id" |
| No theory container found for LevelId | Теория не импортирована | Сначала импортируйте теорию, затем свяжите |

## Советы

📌 **Совет 1:** Давайте одинаковые LevelId в квизе и теории (например, "Warmup", "Miqat")

📌 **Совет 2:** Используйте функцию `Import All Levels` - она делает всё за один раз

📌 **Совет 3:** После импорта проверьте логи в Console - там будут подробности

📌 **Совет 4:** Ассеты сохраняются в папках рядом с GameMainConfig, подержите структуру аккуратной

## Примеры из кода

```csharp
// В GameStateMachine или любом сервисе
[SerializeField] GameMainConfig gameConfig;

void InitializeLevels()
{
    foreach (var level in gameConfig.Levels)
    {
        Debug.Log($"Загрузил уровень: {level.LevelId}");
        
        // Используем квиз
        var questions = level.LevelData.Questions;
        
        // Используем теорию
        var cards = level.TheoryContainer.Cards;
    }
}
```

---

**Версия:** 1.0  
**Дата:** 4 Июля 2026  
**Автор:** GameMainConfig System

