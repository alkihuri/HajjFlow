# Архитектура GameMainConfig

## Общая структура

```
┌─────────────────────────────────────────────────────────────────┐
│                      GameMainConfig (SO)                         │
│  Главный конфиг игры - единое место для управления всеми уровнями│
└─────────┬───────────────────────────────────────────────────────┘
          │
          │ содержит массив
          ▼
┌──────────────────────────────────────────────────────────────────┐
│                    LevelEntry[] - Список уровней                 │
├──────────────────────────────────────────────────────────────────┤
│  LevelEntry #1                                                    │
│  ├─ LevelId: "Warmup"                                             │
│  ├─ LevelData ────────────────────┐                              │
│  │  (Warmup_LevelData.asset)       │                             │
│  │  ├─ LevelId: "Warmup"           │                             │
│  │  ├─ LevelName: "Основы"         │                             │
│  │  ├─ Questions[]: [ 5 вопросов ] │                             │
│  │  └─ ...                          │                             │
│  │                                  ▼                             │
│  └─ TheoryContainer ──────────────────────────────────────┐     │
│     (Warmup_TheoryContainer.asset)                         │     │
│     ├─ LevelId: "Warmup"                                   │     │
│     ├─ Cards[]: [                                           │     │
│     │   Warmup_Card_00.asset ─┐                            │     │
│     │   {Title: "Что такое..."}│                            │     │
│     │                          │                            │     │
│     │   Warmup_Card_01.asset ─┼─ все связаны для          │     │
│     │   {Title: "История..."}  │  одного уровня Warmup     │     │
│     │                          │                            │     │
│     │   ... ещё карточки ...   │                            │     │
│     │ ]                         │                            │     │
│     └──────────────────────────┘                            │     │
│                                                              │     │
│  LevelEntry #2 (Miqat)                                      │     │
│  ├─ LevelId: "Miqat"                                        │     │
│  ├─ LevelData (Miqat_LevelData.asset) ─────────────┐       │     │
│  │  ├─ Questions[]: [ 7 вопросов ]                 │       │     │
│  │  └─ ...                                          │       │     │
│  └─ TheoryContainer (Miqat_TheoryContainer.asset)─┼───┐   │     │
│     ├─ Cards[]: [ 8 карточек для Miqat ]           │   │   │     │
│     └─ ...                                          │   │   │     │
│                                                      │   │   │     │
│  LevelEntry #N (...)                                │   │   │     │
│  ├─ LevelId: "..."                                  │   │   │     │
│  ├─ LevelData ────────────────────────────────────┘   │   │     │
│  └─ TheoryContainer ───────────────────────────────┘   │     │
│                                                        │     │
└────────────────────────────────────────────────────────┘     │
                                                                 │
                          СВЯЗЬ: каждому LevelData ◄───────────┘
                          соответствует одного уровня 
                          TheoryContainer через LevelId
```

## Поток данных при импорте

### 1. Импорт квизов

```
JSON файл (1warmup_quiz.json)
├─ Первый элемент (метаданные)
│  ├─ LevelId: "Warmup" ◄─────────────┐
│  ├─ LevelName: "Основы"             │
│  └─ Description: "..."              │
│                                     │
├─ Элементы 2-6 (вопросы)             │
│  └─ QuestionText, Options, etc.     │
│                                     │
└─ Создаётся LevelData                │
   ├─ LevelId = LevelId из JSON ◄────┘
   ├─ Questions = все вопросы из JSON
   └─ Сохраняется в Levels/Warmup_LevelData.asset
```

### 2. Импорт теории

```
JSON файл (1warmup_theory.json)
├─ Первый элемент (ID уровня)
│  └─ Id: "Warmup" ◄────────────────┐
│                                    │
├─ Элементы 2-N (карточки)          │
│  ├─ Title: "...", Text: "..."     │
│  ├─ Title: "...", Text: "..."     │
│  └─ ...                            │
│                                    │
└─ Создаётся TheoryCardContainer    │
   ├─ LevelId = "Warmup" ◄──────────┘
   ├─ Cards[] (список карточек):
   │  ├─ Card_00.asset {Title, Text}
   │  ├─ Card_01.asset {Title, Text}
   │  ├─ Card_02.asset {Title, Text}
   │  └─ ...
   └─ Сохраняется в Theory/Warmup_TheoryContainer.asset
```

### 3. Связывание

```
LevelData (Warmup_LevelData.asset)           TheoryContainer (Warmup_TheoryContainer.asset)
       │                                                  │
       │ LevelId: "Warmup"                               │ LevelId: "Warmup"
       │                                                  │
       └─────────────────────────────────────────────────┘
                          ▼
                    СВЯЗЫВАЮТСЯ!
                    (совпадают LevelId)
       ┌──────────────────────────────────┐
       │   LevelEntry                     │
       │   ├─ LevelId: "Warmup"          │
       │   ├─ LevelData ──────────────┐  │
       │   └─ TheoryContainer ────────┼──┤
       │                             │  │
       └─────────────────────────────┼──┘
                                     │
                    Теперь весь уровень
                    в одном месте!
```

## Использование в коде

### Получение данных

```
GameMainConfig
    │
    ├─ Levels[0].LevelId = "Warmup"
    │
    ├─ GetLevelData("Warmup")
    │  └─ LevelData { Questions[], ... }
    │
    ├─ GetTheoryContainer("Warmup")
    │  └─ TheoryCardContainer { Cards[], ... }
    │
    └─ GetLevelEntry("Warmup")
       └─ LevelEntry { LevelId, LevelData, TheoryContainer }
```

### Пример кода

```csharp
// Получить конфиг
GameMainConfig config = Resources.Load<GameMainConfig>("Configs/GameMainConfig");

// Получить уровень "Warmup"
var levelEntry = config.GetLevelEntry("Warmup");

// Использовать квиз
QuizQuestion[] questions = levelEntry.LevelData.Questions;
foreach (var q in questions)
{
    Debug.Log($"Вопрос: {q.QuestionText}");
}

// Использовать теорию
List<TheoryCardData> cards = levelEntry.TheoryContainer.Cards;
foreach (var card in cards)
{
    Debug.Log($"Карточка: {card.Title}");
}
```

## Папки и файлы

```
Assets/Configs/
│
├─ GameMainConfig.asset ◄─── главный конфиг (берётся в коде)
│
├─ Levels/ ◄─── папка с квизами (создаётся автоматически)
│  ├─ Warmup_LevelData.asset
│  ├─ Miqat_LevelData.asset
│  ├─ Tawaf_LevelData.asset
│  └─ ...
│
└─ Theory/ ◄─── папка с теорией (создаётся автоматически)
   ├─ Warmup_TheoryContainer.asset
   ├─ Warmup_Card_00.asset
   ├─ Warmup_Card_01.asset
   ├─ Warmup_Card_02.asset
   │
   ├─ Miqat_TheoryContainer.asset
   ├─ Miqat_Card_00.asset
   ├─ Miqat_Card_01.asset
   └─ ...
```

## Алгоритм импорта

### Import All Quiz Files from Folder

```
FOR каждый *.json файл в папке:
  1. Прочитать содержимое файла
  2. Извлечь метаданные из первого элемента массива
     - LevelId (обязательно)
     - LevelName (опционально)
     - Description (опционально)
  3. Парсить вопросы (элементы массива с QuestionText)
  4. Проверить, есть ли LevelEntry с таким LevelId
     - Если есть: использовать существующий
     - Если нет: создать новый
  5. Создать или обновить LevelData
     - Установить LevelId, LevelName, Description
     - Заполнить массив Questions
  6. Сохранить в Levels/{LevelId}_LevelData.asset
  7. Добавить/обновить в Levels[] массив
  8. Залогировать результат
END FOR
Сохранить GameMainConfig.asset
```

### Import All Theory Files from Folder

```
FOR каждый *.json файл в папке:
  1. Прочитать содержимое файла
  2. Извлечь LevelId из первого элемента (поле "Id")
  3. Проверить, есть ли LevelEntry с таким LevelId
     - Если нет: создать новый
  4. Создать новый TheoryCardContainer
     - Установить LevelId
  5. FOR каждый элемент массива с Title и Text:
       a. Создать TheoryCardData
       b. Установить Title, Description, Image
       c. Сохранить в Theory/{LevelId}_Card_XX.asset
       d. Добавить в Cards[] контейнера
     END FOR
  6. Сохранить TheoryCardContainer в Theory/{LevelId}_TheoryContainer.asset
  7. Установить в LevelEntry.TheoryContainer
  8. Залогировать результат
END FOR
Сохранить GameMainConfig.asset
```

### Link Theory to Quiz by LevelId

```
FOR каждый LevelEntry в Levels[]:
  1. Если LevelEntry.TheoryContainer != null:
       SKIP (уже связан)
  2. Получить LevelEntry.LevelId
  3. Найти все TheoryCardContainer в проекте
  4. FOR каждый TheoryCardContainer:
       Если container.LevelId == LevelEntry.LevelId:
         LevelEntry.TheoryContainer = container
         BREAK
  5. Если не найден: залогировать warning
END FOR
Сохранить GameMainConfig.asset
```

## Проверка целостности данных

```
GameMainConfig
├─ ✅ Все LevelId уникальны?
├─ ✅ Каждый LevelEntry имеет LevelId?
├─ ✅ Каждый LevelEntry имеет LevelData?
├─ ✅ Каждый LevelEntry имеет TheoryContainer?
├─ ✅ Каждый LevelData.LevelId соответствует LevelEntry.LevelId?
├─ ✅ Каждый TheoryContainer.LevelId соответствует LevelEntry.LevelId?
├─ ✅ В LevelData есть хотя бы 1 вопрос?
└─ ✅ В TheoryContainer есть хотя бы 1 карточка?
```

---

**Дата:** 4 Июля 2026  
**Версия:** 1.0

