<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3+-black?logo=unity&logoColor=white" alt="Unity"/>
  <img src="https://img.shields.io/badge/C%23-10-239120?logo=csharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/Platform-WebGL-blue" alt="WebGL"/>
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License"/>
  <img src="https://img.shields.io/badge/Languages-7-orange" alt="7 Languages"/>
</p>

<h1 align="center">🕋 HajjFlow</h1>

<p align="center">
  <b>Интерактивная образовательная игра, обучающая паломников основам Хаджа</b><br/>
  <i>Learn. Practice. Be prepared.</i>
</p>

<p align="center">
  Иммерсивные 2.5D-среды · Управляемые симуляции · Викторины · Аудио-обучение<br/>
  Пользователи шаг за шагом осваивают каждый ритуал перед его реальным выполнением.
</p>

---

## 📑 Оглавление

- [Обзор проекта](#-обзор-проекта)
- [Архитектура](#-архитектура)
  - [Service Locator и Bootstrapper](#-service-locator-и-bootstrapper)
  - [Конечный автомат состояний (FSM)](#-конечный-автомат-состояний-fsm)
  - [Жизненный цикл уровня](#-жизненный-цикл-уровня)
  - [Система локализации](#-система-локализации)
  - [Система персистентности данных](#-система-персистентности-данных)
  - [Система викторин](#-система-викторин)
  - [Система вознаграждений](#-система-вознаграждений)
- [Все сервисы](#-все-сервисы)
- [Структура проекта](#-структура-проекта)
- [Сцены](#-сцены)
- [Префабы](#-префабы)
- [Описание скриптов](#-описание-скриптов)
- [Система вознаграждений по уровням](#-система-вознаграждений-по-уровням)
- [Архитектурные паттерны](#-архитектурные-паттерны)
- [Быстрый старт](#-быстрый-старт)

---

## 🎯 Обзор проекта

**HajjFlow** — это Unity WebGL-приложение, обучающее паломников основам Хаджа через интерактивный геймплей. Проект построен по архитектуре **Service Locator + State Machine** и включает:

| Характеристика | Значение |
|---|---|
| **Уровней** | 3 (Warmup → Miqat → Tawaf) |
| **Поддерживаемых языков** | 7 (🇷🇺 RU · 🇧🇦 BS · 🇦🇱 AL · 🇹🇷 TR · 🇸🇦 AR · 🇮🇩 ID · 🇬🇧 EN) |
| **Сцен** | 4 (MainMenu + 3 уровня) |
| **C# скриптов** | 65+ |
| **Префабов** | 11 |
| **Платформа** | WebGL (основная), Editor |

**Игровой цикл:** Главное меню → Выбор уровня → Теория (образовательные карточки) → Викторина → Результаты → Следующий уровень.

---

## 🏗 Архитектура

### 🔌 Service Locator и Bootstrapper

Проект использует паттерн **Service Locator** реализованный через синглтон `GameManager`:

```
┌─ BOOTSTRAP (порядок запуска) ──────────────────────────────────────────┐
│                                                                         │
│  1. GameManager.Awake()  [ExecutionOrder = 0]                          │
│     └── Singleton + DontDestroyOnLoad                                  │
│                                                                         │
│  2. Bootstrapper.Awake()  [ExecutionOrder = 100]                       │
│     ├── RegisterServices(gm):                                          │
│     │   ├── AudioService           (MonoBehaviour, Inspector)          │
│     │   ├── ProfileLoaderService   (new, Plain C#)                     │
│     │   ├── LocalizationService    (new, Plain C#)                     │
│     │   ├── UserProfileService     (new, Plain C#)                     │
│     │   ├── ProgressService        (new, зависит от UserProfile)       │
│     │   ├── UIService              (MonoBehaviour, Inspector)          │
│     │   ├── StageCompletionService (MonoBehaviour, Inspector)          │
│     │   ├── QuizService            (MonoBehaviour, Inspector)          │
│     │   └── GameStateMachine       (MonoBehaviour, Inspector)          │
│     ├── VerifyServices() → проверка наличия всех сервисов              │
│     └── ChangeState("main_menu") → старт игры                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**`GameManager`** (`Assets/Scripts/Core/GameManager.cs`) — центральный синглтон:
- `RegisterService<T>(service)` — регистрация сервиса по типу
- `GetService<T>()` — получение сервиса (возвращает `null` если не найден)
- `HasService<T>()` — проверка наличия сервиса
- `AddGems(amount)` — удобный метод для начисления гемов
- Backward-compatible свойства: `ProfileService`, `ProgressService`, `uiService`, `quizService`, `localizationService`, `profileLoaderService`

**`Bootstrapper`** (`Assets/Scripts/Core/Bootstrapper.cs`) — точка входа, `[DefaultExecutionOrder(100)]`:
- Через Inspector привязываются MonoBehaviour-сервисы: `UIService`, `StageCompletionService`, `QuizService`, `GameStateMachine`, `AudioService`
- Plain C# сервисы создаются в коде: `ProfileLoaderService`, `LocalizationService`, `UserProfileService`, `ProgressService`

---

### 🔄 Конечный автомат состояний (FSM)

Вся игровая логика управляется единым конечным автоматом **`GameStateMachine`**:

```
                    ┌──────────────┐
                    │  main_menu   │ ← точка входа
                    │ MainMenuState│
                    └──────┬───────┘
                           │ Start
                           ▼
                    ┌──────────────┐
                    │ level_select │
                    │LevelSelectSt.│
                    └──────┬───────┘
                           │ Выбор уровня
              ┌────────────┼────────────┐
              ▼            ▼            ▼
       ┌─────────┐  ┌─────────┐  ┌─────────┐
       │ Warmup  │  │  Miqat  │  │  Tawaf  │
       │(Туториал│  │(Микат)  │  │(Таваф)  │
       │3 блока) │  │2 блока  │  │2 блока  │
       └────┬────┘  └────┬────┘  └────┬────┘
            │            │            │
            └────────────┼────────────┘
                         │ CompleteLevel(score%)
                         ▼
                  ┌──────────────┐
                  │   results    │
                  │ ResultsState │
                  └──────┬───────┘
                    ┌────┴────┐
                    ▼         ▼
               Replay    Level Select
```

**Классы состояний:**

| Класс | StateId | Описание |
|---|---|---|
| `BaseGameState` | — | Абстрактный базовый: `Initialize()`, `Enter()`, `Update()`, `Exit()`, `OnPause()`, `OnResume()` |
| `MainMenuState` | `"main_menu"` | Показ стартового экрана |
| `LevelSelectState` | `"level_select"` | Показ экрана выбора уровней, обновление прогресса |
| `BaseLevelState` | — | Абстрактный базовый для уровней: теория → квиз → прогресс |
| `WarmupLevelState` | `"Warmup"` | Вводный уровень, 3 блока теории, без бонусов |
| `MiqatLevelState` | `"Miqat"` | Уровень Микат, 2 блока теории, бонусы за скорость и отличие |
| `TawafLevelState` | `"Tawaf"` | Уровень Таваф, 2 блока теории, стрик-бонусы и круги |
| `ResultsState` | `"results"` | Экран результатов: счёт, гемы, кнопки «Далее»/«Повтор» |

**Управление FSM:**
- `ChangeState(stateId)` — переход к состоянию (меню, результаты)
- `ChangeState(stateId, levelData)` — переход к уровню с данными
- `StartLevel(levelData, stateId)` — удобный метод запуска уровня
- `CompleteLevel(scorePercent)` — завершение уровня → событие `OnLevelCompleted` → переход к Results через 2 сек
- `ReplayCurrentLevel()` — повтор без перезагрузки сцены
- `Pause()` / `Resume()` — пауза через `Time.timeScale`
- **События:** `OnStateChanged`, `OnLevelCompleted`

**Идентификаторы** хранятся в `GameStateIds` (`LevelStateIds.cs`):
```csharp
GameStateIds.MainMenu    = "main_menu"
GameStateIds.LevelSelect = "level_select"
GameStateIds.Results     = "results"
GameStateIds.Warmup      = "Warmup"
GameStateIds.Miqat       = "Miqat"
GameStateIds.Tawaf       = "Tawaf"
```

---

### 📚 Жизненный цикл уровня

`BaseLevelState` определяет единый жизненный цикл для всех игровых уровней:

```
  Enter()
    │
    ├── ResetLevelState()            // Сброс счётчиков для чистого повтора
    ├── ShowLevelUI()                // Абстрактный — каждый уровень свой UI
    ├── FindObjectOfType<QuizSystem> // Поиск систем в сцене
    ├── FindObjectOfType<RewardSystem>
    ├── Subscribe(QuizSystem events) // OnQuestionReady, OnAnswerResult, OnQuizComplete
    └── QuizSystem.Initialise(levelData)
         │
         ▼
  Фаза теории: CompleteTheoryStage() × N
    │  (StageCompletionService верифицирует каждый блок)
    │  Количество блоков: TheoryBlockCount (abstract property)
    │     Warmup = 3, Miqat = 2, Tawaf = 2
    │
    ▼  Когда _currentStageIndex >= TheoryBlockCount
  StartQuiz()
    │  QuizService.InitializeQuiz(questions)
    │
    ▼
  Цикл вопросов:
    HandleQuestionReady(question, num)  ← переопределяется в TawafLevelState
    HandleAnswerResult(wasCorrect)      ← переопределяется в Miqat/Tawaf для бонусов
    │
    ▼
  HandleQuizComplete(scorePercent)
    │  → _stateMachine.CompleteLevel(%)
    │
    ▼
  Exit()
    ├── SaveProgress()               // ProgressService + StageCompletionService + ProfileLoaderService
    └── UnsubscribeQuizEvents()
```

---

### 🌍 Система локализации

HajjFlow использует собственную кастомную систему локализации на базе CSV:

```
┌─ ИСТОЧНИКИ ДАННЫХ ──────────────────────────────────────────────────────┐
│                                                                          │
│  Основной: Google Sheets (публичный CSV)                                │
│  └── URL: docs.google.com/spreadsheets/d/e/2PACX-.../pub?output=csv    │
│                                                                          │
│  Фоллбэк: Resources/localization.csv (TextAsset)                       │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘

┌─ CSV ФОРМАТ ────────────────────────────────────────────────────────────┐
│  key          │ ru        │ bs       │ al       │ tr    │ ar   │ id │ en │
│  WELCOME      │ Добро пож.│ Dobrodošli│ Mirësevini│ Hoş..│ مرحبا│ ...│ ...│
│  START_BTN    │ Начать    │ Počni    │ Fillo    │ Başla│ ابدأ │ ...│ ...│
└──────────────────────────────────────────────────────────────────────────┘
```

**Компоненты системы:**

| Компонент | Тип | Роль |
|---|---|---|
| `Language` (enum) | `Data/Language.cs` | `Russian, Bosnian, Albanian, Turkish, Arabic, Indonesian, English` |
| `LocalizationService` | Plain C# | Загрузка CSV, хранение таблицы `_table[key][language]`, смена языка, уведомление UI |
| `GameTextController` | MonoBehaviour | Компонент на TMP_Text, авто-обновление при смене языка |
| `LanguageSelectController` | MonoBehaviour | UI выбора языка, вызывает `LocalizationService.ChangeLanguage()` |
| `LocalizationEditorService` | Editor | Инструменты редактора для локализации |

**Поток данных:**

```
 Конструктор LocalizationService:
   1. LoadSavedLanguage()   ← PlayerPrefs["SelectedLanguage"]
   2. LoadCsv()             ← Resources.Load<TextAsset>("localization")
   3. ParseCsv(csv)         ← Разбор заголовков (ru,bs,al,tr,ar,id,en) → Dictionary<string, Dictionary<Language, string>>

 Смена языка: ChangeLanguage(Language)
   1. Обновление _currentLanguage
   2. PlayerPrefs.SetInt("SelectedLanguage", (int)language)
   3. Обход всех зарегистрированных GameTextController → UpdateText()
   4. Событие OnLanguageChanged

 Получение текста: GetText(key) → возвращает перевод или сам ключ как фоллбэк

 GameTextController (на каждом TMP_Text в сцене):
   Awake()     → Register(this) в LocalizationService
   OnEnable()  → UpdateText()
   OnDestroy() → Unregister(this)
   UpdateText() → _textComponent.text = service.GetText(_localizationKey)

 Загрузка из Google Sheets (Editor / Runtime):
   LoadFromGoogleSheets() → UnityWebRequest.Get(URL) → ParseCsv() → обновить все тексты
   В Editor → SaveCsvToResources() → File.WriteAllText("Resources/localization.csv")
```

---

### 💾 Система персистентности данных

Многоуровневая система сохранения с паттерном **Strategy**:

```
┌─ ProfileLoaderService (стратегия загрузки) ──────────────────────────────┐
│                                                                           │
│  Приоритет загрузки:  Backend (100) → PlayerPrefs (50) → File (10)      │
│                                                                           │
│  Провайдеры (IProfileDataProvider):                                      │
│  ┌────────────────────────┬──────────┬──────────────────────────────────┐│
│  │ Провайдер              │ Приоритет│ Описание                         ││
│  ├────────────────────────┼──────────┼──────────────────────────────────┤│
│  │ BackendProfileProvider │ 100      │ REST API (опционально)          ││
│  │ PlayerPrefsProfileProvider│ 50    │ Unity PlayerPrefs (по умолчанию)││
│  │ FileProfileProvider    │ 10       │ JSON-файл (отключён)            ││
│  └────────────────────────┴──────────┴──────────────────────────────────┘│
│                                                                           │
│  Загрузка:  Load() / LoadAsync() → первый успешный провайдер → кеш      │
│  Сохранение: Save() → все локальные; SaveAsync() → все включая Backend  │
│  Кеш: _cachedProfile, InvalidateCache() для принудительной перезагрузки │
│  Синхронизация: SyncWithBackendAsync() → двусторонняя синхронизация     │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ UserProfileService (обёртка для профиля) ───────────────────────────────┐
│  GetProfile() → загружает/возвращает UserProfile                         │
│  UpdateProfile(Action<UserProfile>) → изменение + немедленное сохранение │
│  Save() → файл + PlayerPrefs                                            │
│  ResetProgress() → сброс всего прогресса                                │
└───────────────────────────────────────────────────────────────────────────┘

┌─ UserProfile (модель данных) ────────────────────────────────────────────┐
│  FirstName, LastName → FullName                                          │
│  TotalProgress (0–100%)                                                  │
│  Gems (валюта)                                                           │
│  CompletedLevelIds (List<string>)                                        │
│  LevelProgress (SerializableDictionary<string, float>)                   │
│  ResetProgress() → очистка всех данных                                   │
└───────────────────────────────────────────────────────────────────────────┘
```

---

### ❓ Система викторин

Проект содержит две независимые системы викторин:

| Система | Тип | Роль |
|---|---|---|
| `QuizSystem` | MonoBehaviour (сцена) | Исполнение квиза: `Initialise(levelData)`, `SubmitAnswer()`, `Advance()`, события: `OnQuestionReady`, `OnAnswerResult`, `OnQuizComplete` |
| `QuizService` | MonoBehaviour (сервис) | Сервис квиза: `InitializeQuiz(questions)`, `SubmitAnswer(index)`, `MoveToNextQuestion()`, события: `OnQuestionDisplayed`, `OnAnswerCorrect`, `OnAnswerIncorrect`, `OnQuizCompleted` |

**`BaseLevelState`** подписывается на события **`QuizSystem`** (из сцены), но также инициализирует **`QuizService`** для передачи вопросов. Финальный счёт читается из `QuizService.GetLastScorePercent()`.

**Модель вопроса (`QuizQuestion`):**
```
QuestionText    — текст вопроса
Options[]       — 4 варианта ответа
CorrectAnswerIndex — индекс правильного
Explanation     — пояснение к ответу
GemsReward      — награда за правильный ответ
ShuffleOptions() — перемешивание вариантов
```

---

### 🏆 Система вознаграждений

Реализована через `RewardSystem` (MonoBehaviour в сцене) + логику в состояниях уровней:

| Уровень | Бонусы |
|---|---|
| **Warmup** | Только базовые гемы за правильные ответы (`question.GemsReward`) |
| **Miqat** | + **Бонус скорости:** +2 гема если ответ менее чем за 3 минуты<br/>+ **Бонус за отличие:** +50% от `CompletionBonusGems` при score ≥ 90% |
| **Tawaf** | + **Стрик-бонус:** +`streak × 2` гемов начиная с 3-го подряд правильного<br/>+ **Идеальный круг:** +20 гемов за 7 подряд правильных<br/>+ **Идеальный Таваф:** +50 гемов за 100% прохождение |

---

## 🔧 Все сервисы

| Сервис | Тип | Файл | Описание |
|---|---|---|---|
| **GameManager** | MonoBehaviour Singleton | `Core/GameManager.cs` | Service Locator, DontDestroyOnLoad, `GetService<T>()` |
| **GameStateMachine** | MonoBehaviour | `Core/States/GameStateMachine.cs` | Конечный автомат, управляет всеми состояниями игры |
| **LocalizationService** | Plain C# | `Services/LocalizationService.cs` | Мультиязычность: CSV из Google Sheets, 7 языков, фоллбэк из Resources |
| **UserProfileService** | Plain C# | `Services/UserProfileService.cs` | Загрузка/сохранение профиля (PlayerPrefs + File) |
| **ProfileLoaderService** | Plain C# | `Services/ProfileLoaderService.cs` | Мульти-провайдерная загрузка: Backend → PlayerPrefs → File |
| **ProgressService** | Plain C# | `Services/ProgressService.cs` | Отслеживание прогресса по уровням, расчёт общего прогресса |
| **QuizService** | MonoBehaviour | `Services/QuizService.cs` | Управление квизом: вопросы, проверка ответов, подсчёт результата |
| **StageCompletionService** | MonoBehaviour | `Services/StageCompletionService.cs` | Верификация завершения блоков теории, хранение результатов уровней |
| **AudioService** | MonoBehaviour | `Services/AudioService.cs` | Воспроизведение звуков: `PlaySound()`, `PlayClick()`, `PlayWhoosh()` |
| **UIService** | MonoBehaviour | `UI/UIService.cs` | Управление UI-панелями: показ/скрытие экранов, обновление гемов |

---

## 📁 Структура проекта

```
Assets/
├── Art/
│   └── Fonts/Comfortaa/                  Шрифты
├── Data/
│   └── QuizData/                         JSON-данные вопросов
├── DOTween/                              Библиотека анимаций (3rd party)
├── Prefabs/
│   ├── App/
│   │   ├── App.prefab                    Корневой контейнер приложения
│   │   └── Services.prefab               Контейнер сервисов
│   ├── UI/
│   │   ├── LevelBtn.prefab              Кнопка уровня
│   │   ├── QuestionCotroller.prefab     Контроллер вопроса
│   │   ├── StartScreen.prefab           Стартовый экран
│   │   ├── Levels/
│   │   │   ├── Canvas.prefab            UI Canvas
│   │   │   ├── LevelController.prefab   Контроллер уровня
│   │   │   ├── LevelSelect.prefab       Экран выбора уровней
│   │   │   └── levels.prefab            Контейнер уровней
│   │   └── Theory/
│   │       ├── CardViewer.prefab         Просмотрщик карточек теории
│   │       └── WarmUpTheoryCardView.prefab  Карточка теории Warmup
├── Resources/
│   ├── Audio/                            Звуковые файлы (click, whoosh)
│   ├── SO/
│   │   ├── Levels/                       ScriptableObjects уровней (LevelData)
│   │   └── Theory/                       ScriptableObjects карточек теории
│   └── localization.csv                  Локализация (7 языков)
├── Scenes/
│   ├── MainMenu.unity                    Главное меню
│   ├── 1lvl.unity                        Уровень 1 — Warmup (Подготовка)
│   ├── 2lvl.unity                        Уровень 2 — Miqat (Микат)
│   └── 3lvl.unity                        Уровень 3 — Tawaf (Таваф)
├── Scripts/
│   ├── Core/                             Ядро: Bootstrapper, GameManager, LevelManager
│   │   ├── States/                       Состояния FSM
│   │   ├── LevelsLogic/                  Логика уровней (LevelController)
│   │   └── Theory/                       Система теоретических карточек
│   ├── Data/                             Модели данных: Language, LevelData, UserProfile, QuizQuestion
│   ├── Editor/                           Инструменты редактора (ContentLoader, Localization)
│   ├── Gameplay/                         Игровые системы: QuizSystem, RewardSystem
│   ├── Services/                         Все сервисы + ProfileDataProvider/
│   └── UI/                               UI-контроллеры (13 скриптов)
├── Settings/                             Настройки рендеринга Unity
├── TextMesh Pro/                         Библиотека текста (3rd party)
└── UI Toolkit/                           Unity UI Toolkit ресурсы
```

---

## 🎬 Сцены

| Сцена | Файл | Содержимое |
|---|---|---|
| **MainMenu** | `Scenes/MainMenu.unity` | Главное меню, стартовый экран, кнопка «Начать», выбор языка. Содержит `App.prefab` с `GameManager` и `Bootstrapper` |
| **Level 1 — Warmup** | `Scenes/1lvl.unity` | Вводный уровень «Подготовка к Хаджу»: 3 блока теории → викторина |
| **Level 2 — Miqat** | `Scenes/2lvl.unity` | Уровень «Микат»: 2 блока теории → викторина с бонусами за скорость |
| **Level 3 — Tawaf** | `Scenes/3lvl.unity` | Уровень «Таваф»: 2 блока теории → викторина со стрик-системой и кругами |

---

## 🧩 Префабы

### App (корневые)

| Префаб | Путь | Описание |
|---|---|---|
| **App.prefab** | `Prefabs/App/` | Корневой GameObject приложения. Содержит `GameManager` (Singleton, DontDestroyOnLoad) и `Bootstrapper` для регистрации всех сервисов. Помещается в начальную сцену (MainMenu) |
| **Services.prefab** | `Prefabs/App/` | Контейнер MonoBehaviour-сервисов: `AudioService`, `StageCompletionService`, `QuizService`, `GameStateMachine`. Привязаны к `Bootstrapper` через Inspector |

### UI

| Префаб | Путь | Описание |
|---|---|---|
| **StartScreen.prefab** | `Prefabs/UI/` | Стартовый экран главного меню: логотип, кнопка «Начать», выбор языка. Управляется `MainMenuState` |
| **LevelBtn.prefab** | `Prefabs/UI/` | Шаблон кнопки уровня для экрана выбора. Содержит `LevelTileUI`: миниатюра, название, прогресс-бар, состояние блокировки. Инстанциируется `UIService.CreateLevelButtons()` |
| **QuestionCotroller.prefab** | `Prefabs/UI/` | Контроллер отображения вопроса викторины. Содержит `QuizUIController`: текст вопроса, 4 кнопки ответов, обратная связь (правильно/неправильно), пояснение |
| **Canvas.prefab** | `Prefabs/UI/Levels/` | UI Canvas для игровых сцен уровней. Содержит основные панели: геймплей, пауза, прогресс |
| **LevelController.prefab** | `Prefabs/UI/Levels/` | Контроллер потока уровня: переключение между теорией и квизом, управление `StageGameplayController`. Используется `LevelController.cs` |
| **LevelSelect.prefab** | `Prefabs/UI/Levels/` | Экран выбора уровней: список доступных уровней (через `LevelBtn.prefab`), отображение общего прогресса и гемов |
| **levels.prefab** | `Prefabs/UI/Levels/` | Контейнер для инстанциированных кнопок уровней |

### Theory (карточки теории)

| Префаб | Путь | Описание |
|---|---|---|
| **CardViewer.prefab** | `Prefabs/UI/Theory/` | Компонент просмотра теоретических карточек: пролистывание, анимации. Содержит `TheoryCardsManager` для управления жизненным циклом карточек |
| **WarmUpTheoryCardView.prefab** | `Prefabs/UI/Theory/` | Специализированная карточка теории для Warmup-уровня. Содержит `WarmUpTheoryCard` с расширенным контентом для вводного обучения |

---

## 📝 Описание скриптов

### Core (ядро — 4 скрипта)

| Скрипт | Описание |
|---|---|
| `Bootstrapper.cs` | Точка входа приложения. Регистрирует все сервисы в `GameManager`, верифицирует их наличие, запускает `MainMenuState` |
| `GameManager.cs` | Singleton Service Locator с `DontDestroyOnLoad`. Центральный хаб для доступа ко всем сервисам через `GetService<T>()` |
| `GameplaySceneInitializer.cs` | Инициализатор игровых сцен: находит `QuizSystem` и `RewardSystem` в сцене, подключает к текущему состоянию |
| `LevelManager.cs` | Управление жизненным циклом уровней: `StartLevel()`, `RestartLevel()`, `ShowResults()` |

### States (состояния FSM — 11 скриптов)

| Скрипт | Описание |
|---|---|
| `BaseGameState.cs` | Абстрактный базовый класс: `StateId`, `Initialize()`, `Enter()` (сбрасывает UI), `Update()`, `Exit()`, `OnPause()`, `OnResume()` |
| `GameStateMachine.cs` | Главный FSM: регистрация 6 состояний, переходы, пауза/возобновление, события `OnStateChanged` и `OnLevelCompleted` |
| `BaseLevelState.cs` | Абстрактный базовый для уровней: теория → квиз → сохранение. Подписка на `QuizSystem`, отслеживание ответов, `SaveProgress()` в три хранилища |
| `LevelStateIds.cs` | Константы идентификаторов (`GameStateIds`): `MainMenu`, `LevelSelect`, `Results`, `Warmup`, `Miqat`, `Tawaf`. Утилиты: `GetNextLevelState()`, `IsLevelState()` |
| `LevelStateMachine.cs` | Legacy-FSM для уровней (обратная совместимость) |
| `MainMenuState.cs` | Состояние главного меню: `ui.ShowMainMenu()` |
| `LevelSelectState.cs` | Состояние выбора уровня: `ui.ShowLevelSelect()`, обновление прогресса и гемов |
| `WarmupLevelState.cs` | Warmup: `TheoryBlockCount = 3`, без бонусов, показ теории через `uiService.ShowWarmUpTheoryUI()` |
| `MiqatLevelState.cs` | Miqat: `TheoryBlockCount = 2`, таймер `_startTime`, бонус скорости (+2 гема < 3 мин), бонус за ≥90% |
| `TawafLevelState.cs` | Tawaf: `TheoryBlockCount = 2`, стрик `_consecutiveCorrect`, круги (каждые 7 вопросов), бонус за идеальный круг (+20) и идеальный Таваф (+50) |
| `ResultsState.cs` | Показ результатов: `ui.ShowResults()` |

### Theory (система теории — 7 скриптов)

| Скрипт | Описание |
|---|---|
| `TheoryCardBase.cs` | Абстрактный базовый класс теоретической карточки |
| `SimpleTheoryCard.cs` | Простая реализация карточки с текстом и изображением |
| `WarmUpTheoryCard.cs` | Специализированная карточка для Warmup с расширенным контентом |
| `TheoryCardData.cs` | Модель данных карточки: заголовок, текст, изображение, ключи локализации |
| `TheoryCardContainer.cs` | Контейнер для группы карточек, управление навигацией |
| `TheoryCardsManager.cs` | Менеджер жизненного цикла карточек: загрузка, показ, скрытие |
| `TheoryToQuizIntegration.cs` | Мост между завершением теории и запуском квиза, вызывает `BaseLevelState.CompleteTheoryStage()` |

### Data (модели данных — 5 скриптов)

| Скрипт | Описание |
|---|---|
| `Language.cs` | Enum: `Russian`, `Bosnian`, `Albanian`, `Turkish`, `Arabic`, `Indonesian`, `English` |
| `LevelData.cs` | ScriptableObject уровня: `LevelId`, `LevelName`, `Description`, `Questions[]`, `CompletionBonusGems`, `PassThreshold`, `Thumbnail`. Создание: Assets → Create → Manasik → Level Data |
| `LevelProgress.cs` | Отслеживание прогресса по уровню |
| `QuizQuestion.cs` | Модель вопроса: `QuestionText`, `Options[4]`, `CorrectAnswerIndex`, `Explanation`, `GemsReward`, `ShuffleOptions()` |
| `UserProfile.cs` | Профиль игрока: `FirstName/LastName`, `TotalProgress`, `Gems`, `CompletedLevelIds`, `LevelProgress` (SerializableDictionary), `ResetProgress()` |

### Services (сервисы — 8 скриптов)

| Скрипт | Описание |
|---|---|
| `LocalizationService.cs` | CSV-локализация: загрузка из Resources/Google Sheets, `GetText(key)`, `ChangeLanguage()`, регистрация `GameTextController`, событие `OnLanguageChanged` |
| `LocalizationServiceLoader.cs` | Утилита загрузки локализации |
| `AudioService.cs` | Воспроизведение звуков через `AudioSource`: `PlaySound(clip)`, `PlayClick()`, `PlayWhoosh()` |
| `UserProfileService.cs` | Персистентность профиля: PlayerPrefs + File, `GetProfile()`, `UpdateProfile()`, `Save()`, `SaveAsync()` |
| `ProfileLoaderService.cs` | Мульти-провайдерная загрузка (Strategy): `Load()`, `LoadAsync()`, `Save()`, `SaveAsync()`, `SyncWithBackendAsync()`, `InvalidateCache()` |
| `ProgressService.cs` | Прогресс уровней: `RecordLevelProgress()`, `GetLevelProgress()`, `IsLevelCompleted()`, расчёт `TotalProgress` как среднее всех уровней |
| `QuizService.cs` | Управление квизом: `InitializeQuiz()`, `SubmitAnswer()`, `MoveToNextQuestion()`, `GetLastScorePercent()`. События: `OnQuestionDisplayed`, `OnAnswerCorrect/Incorrect`, `OnQuizCompleted` |
| `StageCompletionService.cs` | Верификация блоков теории: `CompleteStage(levelId, index)`, хранение результатов: `RecordLevelResult()`, `GetLevelResult()`, `GetLevelScore()`, загрузка из профиля `LoadSavedResults()` |

### Profile Data Providers (провайдеры данных — 4 скрипта)

| Скрипт | Описание |
|---|---|
| `IProfileDataProvider.cs` | Интерфейс: `HasData()`, `Load()`, `LoadAsync()`, `Save()`, `SaveAsync()`, `Clear()`, `ProviderName`, `Priority` |
| `PlayerPrefsProfileProvider.cs` | Хранение в Unity PlayerPrefs (JSON). Приоритет: 50 |
| `FileProfileProvider.cs` | Хранение в JSON-файле на диске. Приоритет: 10 (отключён по умолчанию) |
| `BackendProfileProvider.cs` | REST API интеграция для облачной синхронизации. Приоритет: 100 |

### Gameplay (игровые системы — 2 скрипта)

| Скрипт | Описание |
|---|---|
| `QuizSystem.cs` | Исполнитель квиза в сцене: `Initialise(levelData)`, `SubmitAnswer()`, `Advance()`. События: `OnQuestionReady`, `OnAnswerResult`, `OnQuizComplete` |
| `RewardSystem.cs` | Распределение наград: `AwardGems(amount)` → `GameManager.AddGems()` |

### UI (контроллеры интерфейса — 13 скриптов)

| Скрипт | Описание |
|---|---|
| `UIService.cs` | Центральный UI-менеджер: `ShowMainMenu()`, `ShowLevelSelect()`, `ShowResults()`, `ShowLevelByStateId()`, `ResetUI()`, `UpdateGemsCounter()`, `CreateLevelButtons()` |
| `GameTextController.cs` | Авто-обновляемый локализованный текст на TMP_Text. Указывается `_localizationKey` → текст обновляется при смене языка |
| `LanguageSelectController.cs` | UI выбора языка: кнопки языков → `LocalizationService.ChangeLanguage()` |
| `GameplayUI.cs` | Панель геймплея: отображение прогресса квиза |
| `QuizUIController.cs` | Отображение вопроса: текст, 4 кнопки, фидбэк, пояснение |
| `ResultsUI.cs` | Экран результатов: счёт, заработанные гемы, кнопки «Далее»/«Повтор» |
| `SelectMenuUIController.cs` | Меню выбора уровней |
| `MainGameplayMenuUI.cs` | Игровое меню: пауза, рестарт, назад |
| `PauseMenuUI.cs` | Меню паузы: продолжить, рестарт, выход |
| `LevelTileUI.cs` | Плитка уровня: миниатюра, название, прогресс-бар, замок |
| `StageGameplayController.cs` | Контроллер этапа теории: отображение контента, кнопка «Далее» |
| `ButtonEffect.cs` | Визуальные эффекты кнопок (анимации, звуки через DOTween) |

### Level Logic (1 скрипт)

| Скрипт | Описание |
|---|---|
| `LevelController.cs` | Управление потоком внутри уровня: переключение между блоками теории и квизом |

### Editor Tools (3 скрипта, `#if UNITY_EDITOR`)

| Скрипт | Описание |
|---|---|
| `ContentLoaderWindow.cs` | Editor-окно для загрузки контента из внешних источников |
| `DataLoader.cs` | Утилита загрузки данных квизов/уровней |
| `LocalizationEditorService.cs` | Editor-сервис для работы с локализацией |

---

## 🏆 Система вознаграждений по уровням

```
┌─ WARMUP (Подготовка) ────────────────────────────────────────┐
│  Базовые гемы: question.GemsReward за каждый правильный      │
│  Бонусы: нет                                                  │
│  Блоков теории: 3                                             │
└───────────────────────────────────────────────────────────────┘

┌─ MIQAT (Микат) ──────────────────────────────────────────────┐
│  Базовые гемы: question.GemsReward за каждый правильный      │
│  Бонус скорости: +2 гема если ответ < 3 минут от старта      │
│  Бонус за отличие: +50% CompletionBonusGems при score ≥ 90%  │
│  Блоков теории: 2                                             │
│  Пауза: учитывается Time.unscaledDeltaTime                   │
└───────────────────────────────────────────────────────────────┘

┌─ TAWAF (Таваф) ──────────────────────────────────────────────┐
│  Базовые гемы: question.GemsReward за каждый правильный      │
│  Стрик-бонус: +streak×2 гемов (начиная с 3-го подряд)        │
│  Идеальный круг: +20 гемов за 7 подряд правильных            │
│  Идеальный Таваф: +50 гемов за 100% прохождение              │
│  Круги: каждые 7 вопросов = 1 круг Тавафа                    │
│  Блоков теории: 2                                             │
└───────────────────────────────────────────────────────────────┘
```

---

## 🧱 Архитектурные паттерны

| Паттерн | Использование | Файл |
|---|---|---|
| **Service Locator** | Глобальный доступ к сервисам | `GameManager.cs` |
| **Singleton** | Единственный экземпляр GameManager | `GameManager.cs` |
| **State Machine (FSM)** | Управление состояниями игры | `GameStateMachine.cs`, `BaseGameState.cs` |
| **Template Method** | Жизненный цикл уровней, переопределяемые обработчики | `BaseLevelState.cs` |
| **Strategy** | Мульти-провайдерная загрузка профилей | `ProfileLoaderService.cs`, `IProfileDataProvider` |
| **Observer / Event-Driven** | Квиз-события, смена языка, завершение уровня | `QuizSystem`, `LocalizationService`, `GameStateMachine` |
| **Factory** | Создание состояний в `RegisterDefaultStates()` | `GameStateMachine.cs` |
| **DontDestroyOnLoad** | Сохранение сервисов между сценами | `GameManager.cs` |

---

## 🚀 Быстрый старт

1. **Открыть проект** в Unity 2022.3+
2. **Открыть сцену** `Assets/Scenes/MainMenu.unity`
3. **Убедиться** что на сцене есть `App.prefab` с привязанными сервисами в `Bootstrapper`
4. **Play** — игра стартует в `MainMenuState`

**Конфигурация уровней:**
- ScriptableObjects: `Assets/Resources/SO/Levels/`
- Создание: Assets → Create → Manasik → Level Data
- Заполнить: `LevelId`, `LevelName`, `Questions[]`, `CompletionBonusGems`, `PassThreshold`

**Локализация:**
- Основной файл: `Assets/Resources/localization.csv`
- Обновление: Editor → Content Loader → Download from Google Sheets
- Добавление ключа: добавить строку в CSV, использовать `GameTextController._localizationKey` на TMP_Text

**Добавление нового уровня:**
1. Создать класс-наследник `BaseLevelState` с уникальным `StateId`
2. Добавить константу в `GameStateIds`
3. Зарегистрировать в `GameStateMachine.RegisterDefaultStates()`
4. Создать `LevelData` ScriptableObject
5. Создать сцену уровня с `QuizSystem` и `RewardSystem`

---

<p align="center">
  <i>Этот документ может использоваться как контекстный промпт для LLM — он содержит полное описание архитектуры, всех сервисов, состояний и скриптов проекта HajjFlow.</i>
</p>
