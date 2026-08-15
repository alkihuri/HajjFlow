# План перехода на динамическую загрузку вопросов и карточек из Google Таблицы

## Текущая архитектура (AS-IS)

### Как устроено сейчас:
1. **Локализация** (`Assets/Resources/localization.csv`) — единый CSV-файл со всеми ключами:
   - UI-тексты (`UI_START_BUTTON`, `UI_NEXT_BUTTON`, ...)
   - Тексты теории (`THEORY_L0_UMRA_DEF_TITLE`, `THEORY_L0_UMRA_DEF_TEXT`, ...)
   - Тексты вопросов и вариантов (`Q0_UMRA_DEFINITION`, `Q0_OPT_1_MAJOR_PILGRIMAGE`, ...)
   - Пояснения к ответам (`Q0_EXPL_UMRA_DEF`, ...)
   - Уже есть механизм загрузки из Google Sheets (`LocalizationService.LoadFromGoogleSheets()`)

2. **Структура вопросов** (`Assets/Data/Quiz/0lvl.json` ... `6lvl.json`) — JSON-файлы с:
   - Метаданными уровня (LevelId, LevelName, Description, imagePath)
   - Массивом вопросов (QuestionText → ключ локализации, Options → ключи, CorrectAnswerIndex, Explanation, GemsReward)

3. **Структура теории** (`Assets/Data/Theory/0lvltheory.json` ... `6lvltheory.json`) — JSON-файлы с:
   - Id уровня
   - Карточками (Title → ключ локализации, Text → ключ локализации)

4. **ScriptableObjects**:
   - `LevelData` — хранит вопросы, ключи локализации, спрайты
   - `TheoryCardData` — Title, Description (ключи), Image (Sprite)
   - `TheoryCardContainer` — список TheoryCardData для уровня
   - `GameMainConfig` — главный конфиг со списком LevelEntry (LevelId + LevelData + TheoryContainer)

5. **Проблемы текущего подхода**:
   - Вопросы и теория захардкожены в JSON-файлах в билде
   - Для добавления нового уровня нужно: создать JSON, импортировать через Editor, пересобрать билд
   - Ключи к карточкам и теории дублируются (в JSON + в localization.csv), хотя вся информация уже есть в Google Таблице
   - Нет возможности обновить контент без нового билда

---

## Целевая архитектура (TO-BE)

**Принцип**: Google Таблица = единый источник правды для всего текстового контента (структура уровней, вопросы, теория, локализация). Изображения загружаются из Asset Bundles.

### Структура Google Таблицы (рекомендуемая):

**Лист 1: `localization`** (уже существует) — все ключи и переводы, формат `localization.csv`

**Лист 2: `levels`** — метаданные уровней:
| level_id | level_name_key | description_key | order | image_bundle_key |
|----------|----------------|-----------------|-------|-----------------|
| level_0  | LEVEL_0_TITLE  | LEVEL_0_DESC    | 0     | level_0_thumb   |

**Лист 3: `questions`** — вопросы:
| level_id | question_key | option_1_key | option_2_key | option_3_key | option_4_key | correct_index | explanation_key | gems_reward |
|----------|--------------|--------------|--------------|--------------|--------------|---------------|-----------------|-------------|
| level_0  | Q0_UMRA_DEFINITION | Q0_OPT_1_... | Q0_OPT_2_... | Q0_OPT_3_... | Q0_OPT_4_... | 1 | Q0_EXPL_UMRA_DEF | 10 |

**Лист 4: `theory`** — карточки теории:
| level_id | order | title_key | text_key | image_bundle_key |
|----------|-------|-----------|----------|-----------------|
| level_0  | 0     | THEORY_L0_UMRA_DEF_TITLE | THEORY_L0_UMRA_DEF_TEXT | theory_l0_umra |

> **Ключевая идея**: ключи к карточкам и к теории уже есть в Google Таблице в листе локализации. Листы `questions` и `theory` определяют только СТРУКТУРУ (какой вопрос к какому уровню, порядок, правильный ответ), а переводы текста берутся из единого листа `localization`.

---

## Пошаговый план миграции

### Этап 1: Подготовка Google Таблицы

1. **В существующей Google Таблице** добавить новые листы: `levels`, `questions`, `theory`
2. **Заполнить лист `levels`**: перенести метаданные из JSON-файлов (level_id, ключи названий, порядок)
3. **Заполнить лист `questions`**: перенести структуру вопросов из `0lvl.json`...`6lvl.json` (ключи, индексы правильных ответов, награды)
4. **Заполнить лист `theory`**: перенести структуру карточек из `0lvltheory.json`...`6lvltheory.json`
5. **Опубликовать** каждый лист как CSV (Google Sheets → Файл → Опубликовать → CSV) — получить URL для каждого листа

### Этап 2: Создание сервиса загрузки контента (`ContentLoaderService`)

1. Создать `Assets/Scripts/Services/ContentLoaderService.cs`:
   ```
   Ответственность:
   - Хранить URL'ы для каждого листа Google Таблицы
   - Скачивать CSV данные с помощью UnityWebRequest
   - Парсить CSV в рантайм-структуры данных
   - Кэшировать загруженные данные (PlayerPrefs/файловая система)
   - Предоставлять fallback на последние закэшированные данные при отсутствии интернета
   ```

2. Определить рантайм-модели данных (НЕ ScriptableObjects, а обычные C# классы):
   ```
   RuntimeLevelInfo: levelId, nameKey, descriptionKey, order, imageBundleKey
   RuntimeQuizQuestion: levelId, questionKey, optionKeys[], correctIndex, explanationKey, gemsReward
   RuntimeTheoryCard: levelId, order, titleKey, textKey, imageBundleKey
   ```

3. Добавить метод `LoadAllContent()` (корутина):
   - Параллельно загружает все 4 листа (localization, levels, questions, theory)
   - Парсит CSV → заполняет рантайм-коллекции
   - Обновляет таблицу локализации в `LocalizationService`
   - Сохраняет кэш на диск

### Этап 3: Генерация карточек и вопросов в рантайме

1. Создать `Assets/Scripts/Services/RuntimeLevelFactory.cs`:
   ```
   Ответственность:
   - По загруженным RuntimeLevelInfo создавать список уровней для UI выбора
   - По levelId собирать массив QuizQuestion[] из RuntimeQuizQuestion
   - По levelId собирать список карточек теории
   - Привязывать изображения из Asset Bundles по ключу imageBundleKey
   ```

2. Модифицировать `QuizService.InitializeQuiz()`:
   - Принимать `QuizQuestion[]` напрямую (уже так работает ✓)
   - Фабрика генерирует массив QuizQuestion из рантайм-данных

3. Модифицировать `TheoryCardsManager`:
   - Принимать список рантайм-карточек вместо `TheoryCardContainer`
   - Генерировать UI-карточки динамически из данных

### Этап 4: Asset Bundles для изображений

1. **Пометить спрайты** карточек как Asset Bundle (в Inspector → AssetBundle):
   - Группировать по уровням: `level_0_images`, `level_1_images`, ...
   - Или по типу: `theory_images`, `quiz_images`

2. Создать `Assets/Scripts/Services/AssetBundleService.cs`:
   ```
   Ответственность:
   - Загружать Asset Bundles из Remote URL или StreamingAssets (для оффлайн)
   - Кэшировать загруженные бандлы (Unity Caching)
   - Возвращать Sprite по ключу (imageBundleKey из таблицы)
   - Выгружать неиспользуемые бандлы
   ```

3. **Хостинг бандлов**: разместить на GitHub Pages / Firebase Storage / собственном сервере

### Этап 5: Интеграция и замена старого потока

1. **Модифицировать `Bootstrapper.cs`**:
   - При старте приложения запускать `ContentLoaderService.LoadAllContent()`
   - Показывать экран загрузки пока контент не готов
   - При ошибке — использовать кэш

2. **Модифицировать `GameMainConfig`**:
   - Сделать Levels-список опциональным (fallback на статические данные)
   - Добавить флаг `useRemoteContent = true`
   - Если true — данные берутся из ContentLoaderService

3. **Модифицировать UI выбора уровня**:
   - Брать список уровней из RuntimeLevelFactory
   - Динамически создавать карточки уровней (не из фиксированного массива)

4. **Модифицировать flow запуска уровня** (`LevelManager.StartLevel`):
   - Вместо `LevelData` ScriptableObject принимать `levelId` (строку)
   - RuntimeLevelFactory собирает данные по levelId
   - QuizService получает готовый QuizQuestion[]
   - TheoryCardsManager получает список карточек

### Этап 6: Кэширование и оффлайн-режим

1. **Первый запуск**: скачать всё → сохранить в `Application.persistentDataPath`
2. **Последующие запуски**: загрузить кэш → показать контент → в фоне проверить обновления
3. **Версионирование**: хранить хэш или timestamp последнего обновления, сравнивать с header'ом Google Sheets
4. **Fallback-цепочка**:
   - Попытка загрузить из Google Sheets (онлайн)
   - При ошибке — загрузить из кэша (persistentDataPath)
   - При отсутствии кэша — загрузить из Resources (статика, вшитая в билд)

### Этап 7: Удаление дублирующихся файлов

1. Убрать `Assets/Data/Quiz/*.json` — структура теперь в Google Таблице
2. Убрать `Assets/Data/Theory/*.json` — структура теперь в Google Таблице
3. Оставить `Assets/Resources/localization.csv` как fallback (но основной источник — Google Sheets)
4. ScriptableObjects `LevelData`, `TheoryCardData`, `TheoryCardContainer` остаются как fallback, но не являются основным источником данных

---

## Итоговый поток данных

```
Google Таблица (единый источник)
    │
    ├── Лист "localization" → LocalizationService (переводы)
    ├── Лист "levels"       → RuntimeLevelFactory (список уровней)
    ├── Лист "questions"    → RuntimeLevelFactory → QuizQuestion[] → QuizService
    └── Лист "theory"       → RuntimeLevelFactory → TheoryCards[] → TheoryCardsManager
                                    │
                                    ▼
                            AssetBundleService (изображения по ключу)
```

## Что это даёт

| До | После |
|----|-------|
| Добавление уровня = новый JSON + Editor импорт + новый билд | Добавление уровня = новая строка в Google Таблице |
| Исправление текста = правка CSV + новый билд | Исправление текста = правка ячейки в таблице |
| Добавление языка = правка CSV + новый билд | Добавление языка = новый столбец в таблице |
| Ключи дублируются в JSON и CSV | Ключи в одном месте (таблица) |
| Изображения вшиты в билд | Изображения подгружаются из Asset Bundles (обновляемы) |

---

## Приоритет реализации

1. 🔴 **Критично**: ContentLoaderService + парсинг листов questions/theory/levels
2. 🔴 **Критично**: RuntimeLevelFactory + генерация QuizQuestion[] в рантайме  
3. 🟡 **Важно**: Кэширование + оффлайн-режим
4. 🟡 **Важно**: Asset Bundles для изображений
5. 🟢 **Желательно**: Удаление старых JSON-файлов, полный переход

---

## Заметки

- `LocalizationService` уже умеет грузить из Google Sheets — используем тот же паттерн
- Формат `localization.csv` не меняется — все ключи остаются в том же виде
- Структура данных QuizQuestion не меняется — меняется только источник данных
- Текущие ScriptableObjects остаются как fallback на случай отсутствия сети при первом запуске
