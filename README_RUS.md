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
  Иммерсивные 2.5D-среды · Симуляции · Викторины · Аудио-обучение<br/>
  Шаг за шагом — каждый ритуал перед его реальным выполнением.
</p>

> 🇬🇧 [English version](README.md)

---

## 📑 Оглавление

- [Обзор проекта](#-обзор-проекта)
- [Архитектура](#-архитектура)
  - [Service Locator и Bootstrapper](#-service-locator-и-bootstrapper)
  - [Конечный автомат (FSM)](#-конечный-автомат-fsm)
  - [Жизненный цикл уровня](#-жизненный-цикл-уровня)
  - [Локализация](#-локализация)
  - [Персистентность данных](#-персистентность-данных)
  - [Система викторин](#-система-викторин)
  - [Система вознаграждений](#-система-вознаграждений)
- [Все сервисы](#-все-сервисы)
- [Структура проекта](#-структура-проекта)
- [Сцены](#-сцены)
- [Префабы](#-префабы)
- [Описание скриптов](#-описание-скриптов)
- [Вознаграждения по уровням](#-вознаграждения-по-уровням)
- [Архитектурные паттерны](#-архитектурные-паттерны)
- [Быстрый старт](#-быстрый-старт)

---

## 🎯 Обзор проекта

**HajjFlow** — Unity WebGL-приложение для обучения паломников основам Хаджа. Архитектура: **Service Locator + State Machine**.

| Характеристика | Значение |
|---|---|
| **Уровней** | 3 (Warmup → Miqat → Tawaf) |
| **Языков** | 7 (🇷🇺 RU · 🇧🇦 BS · 🇦🇱 AL · 🇹🇷 TR · 🇸🇦 AR · 🇮🇩 ID · 🇬🇧 EN) |
| **Сцен** | 4 (MainMenu + 3 уровня) |
| **C# скриптов** | 65+ |
| **Префабов** | 11 |
| **Платформа** | WebGL, Editor |

**Игровой цикл:** Меню → Выбор уровня → Теория (карточки) → Викторина → Результаты → Следующий уровень.

---

## 🏗 Архитектура

### 🔌 Service Locator и Bootstrapper

Паттерн **Service Locator** через синглтон `GameManager`:

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
│     ├── VerifyServices() → проверка всех сервисов                      │
│     └── ChangeState("main_menu") → старт                               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**`GameManager`** (`Core/GameManager.cs`) — центральный синглтон:
- `RegisterService<T>()` / `GetService<T>()` / `HasService<T>()` — регистрация и доступ
- `AddGems(amount)` — начисление гемов
- Backward-compatible свойства: `ProfileService`, `ProgressService`, `uiService` и др.

**`Bootstrapper`** (`Core/Bootstrapper.cs`) — точка входа `[DefaultExecutionOrder(100)]`:
- Через Inspector — MonoBehaviour-сервисы
- В коде — Plain C# сервисы

---

### 🔄 Конечный автомат (FSM)

Вся логика управляется **`GameStateMachine`**:

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
       │3 блока  │  │2 блока  │  │2 блока  │
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

**Состояния:**

| Класс | StateId | Описание |
|---|---|---|
| `BaseGameState` | — | Абстрактный базовый: `Enter()`, `Update()`, `Exit()`, `OnPause()`, `OnResume()` |
| `MainMenuState` | `"main_menu"` | Стартовый экран |
| `LevelSelectState` | `"level_select"` | Выбор уровней, прогресс |
| `BaseLevelState` | — | Базовый для уровней: теория → квиз → прогресс |
| `WarmupLevelState` | `"Warmup"` | 3 блока теории, без бонусов |
| `MiqatLevelState` | `"Miqat"` | 2 блока теории, бонусы за скорость и отличие |
| `TawafLevelState` | `"Tawaf"` | 2 блока теории, стрик-бонусы и круги |
| `ResultsState` | `"results"` | Счёт, гемы, «Далее»/«Повтор» |

**Управление:**
- `ChangeState(stateId)` / `ChangeState(stateId, levelData)` — переходы
- `CompleteLevel(scorePercent)` — завершение → `OnLevelCompleted` → Results
- `ReplayCurrentLevel()` — повтор
- `Pause()` / `Resume()` — через `Time.timeScale`

**Идентификаторы** в `GameStateIds` (`LevelStateIds.cs`):
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

`BaseLevelState` определяет единый цикл для всех уровней:

```
  Enter()
    │
    ├── ResetLevelState()            // Сброс счётчиков
    ├── ShowLevelUI()                // Абстрактный — свой UI
    ├── FindObjectOfType<QuizSystem>
    ├── FindObjectOfType<RewardSystem>
    ├── Subscribe(QuizSystem events)
    └── QuizSystem.Initialise(levelData)
         │
         ▼
  Фаза теории: CompleteTheoryStage() × N
    │  TheoryBlockCount: Warmup = 3, Miqat = 2, Tawaf = 2
    │
    ▼  Когда _currentStageIndex >= TheoryBlockCount
  StartQuiz()
    │
    ▼
  Цикл вопросов:
    HandleQuestionReady(question, num)
    HandleAnswerResult(wasCorrect)
    │
    ▼
  HandleQuizComplete(scorePercent)
    │  → _stateMachine.CompleteLevel(%)
    │
    ▼
  Exit()
    ├── SaveProgress()
    └── UnsubscribeQuizEvents()
```

---

### 🌍 Локализация

Кастомная CSV-система на 7 языков:

```
┌─ ИСТОЧНИКИ ─────────────────────────────────────────────────────────────┐
│  Основной: Google Sheets (публичный CSV)                                │
│  Фоллбэк:  Resources/localization.csv                                  │
└─────────────────────────────────────────────────────────────────────────┘

  CSV: key | ru | bs | al | tr | ar | id | en
```

| Компонент | Роль |
|---|---|
| `Language` (enum) | `Russian, Bosnian, Albanian, Turkish, Arabic, Indonesian, English` |
| `LocalizationService` | Загрузка CSV, `_table[key][language]`, смена языка, уведомление UI |
| `GameTextController` | Авто-обновление TMP_Text при смене языка |
| `LanguageSelectController` | UI выбора языка |

**Поток:**
```
Конструктор → LoadSavedLanguage() → LoadCsv() → ParseCsv()
ChangeLanguage() → обновить все GameTextController → OnLanguageChanged
GetText(key) → перевод или ключ как фоллбэк
```

---

### 💾 Персистентность данных

Многоуровневое сохранение с паттерном **Strategy**:

```
┌─ ProfileLoaderService ───────────────────────────────────────────────────┐
│  Приоритет: Backend (100) → PlayerPrefs (50) → File (10)                │
│  Load() → первый успешный провайдер → кеш                               │
│  Save() → все локальные; SaveAsync() → все включая Backend              │
└──────────────────────────────────────────────────────────────────────────┘

┌─ UserProfileService ─────────────────────────────────────────────────────┐
│  GetProfile() → загрузка/возврат UserProfile                             │
│  UpdateProfile() → изменение + сохранение                                │
└──────────────────────────────────────────────────────────────────────────┘

┌─ UserProfile ────────────────────────────────────────────────────────────┐
│  FirstName, LastName, TotalProgress, Gems, CompletedLevelIds,            │
│  LevelProgress (SerializableDictionary)                                  │
└──────────────────────────────────────────────────────────────────────────┘
```

---

### ❓ Система викторин

Два компонента:

| Система | Роль |
|---|---|
| `QuizSystem` (сцена) | Исполнение: `Initialise()`, `SubmitAnswer()`, `Advance()`, события `OnQuestionReady`, `OnAnswerResult`, `OnQuizComplete` |
| `QuizService` (сервис) | Управление: `InitializeQuiz()`, `SubmitAnswer()`, `MoveToNextQuestion()`, `GetLastScorePercent()` |

`BaseLevelState` подписывается на `QuizSystem`, инициализирует `QuizService`. Финальный счёт — из `QuizService.GetLastScorePercent()`.

---

### 🏆 Система вознаграждений

`RewardSystem` (MonoBehaviour) + логика в состояниях:

| Уровень | Бонусы |
|---|---|
| **Warmup** | Базовые гемы за правильные ответы |
| **Miqat** | + скорость (+2 < 3 мин) · + отличие (+50% при ≥ 90%) |
| **Tawaf** | + стрик (streak×2 с 3-го) · + идеальный круг (+20 за 7 подряд) · + идеальный Таваф (+50 за 100%) |

---

## 🔧 Все сервисы

| Сервис | Тип | Файл | Описание |
|---|---|---|---|
| **GameManager** | Singleton | `Core/GameManager.cs` | Service Locator, DontDestroyOnLoad |
| **GameStateMachine** | MonoBehaviour | `Core/States/GameStateMachine.cs` | FSM — управление состояниями |
| **LocalizationService** | Plain C# | `Services/LocalizationService.cs` | CSV-локализация, 7 языков |
| **UserProfileService** | Plain C# | `Services/UserProfileService.cs` | Профиль: PlayerPrefs + File |
| **ProfileLoaderService** | Plain C# | `Services/ProfileLoaderService.cs` | Мульти-провайдерная загрузка |
| **ProgressService** | Plain C# | `Services/ProgressService.cs` | Прогресс по уровням |
| **QuizService** | MonoBehaviour | `Services/QuizService.cs` | Управление викториной |
| **StageCompletionService** | MonoBehaviour | `Services/StageCompletionService.cs` | Верификация блоков теории |
| **AudioService** | MonoBehaviour | `Services/AudioService.cs` | Звуки: `PlayClick()`, `PlayWhoosh()` |
| **UIService** | MonoBehaviour | `UI/UIService.cs` | UI-панели, экраны, гемы |

---

## 📁 Структура проекта

```
Assets/
├── Art/Fonts/Comfortaa/                   Шрифты
├── Data/QuizData/                         JSON вопросов
├── DOTween/                               Анимации (3rd party)
├── Prefabs/
│   ├── App/                               App.prefab, Services.prefab
│   ├── UI/                                LevelBtn, QuestionController, StartScreen
│   │   ├── Levels/                        Canvas, LevelController, LevelSelect
│   │   └── Theory/                        CardViewer, WarmUpTheoryCardView
├── Resources/
│   ├── Audio/                             Звуки
│   ├── SO/Levels/, SO/Theory/             ScriptableObjects
│   └── localization.csv                   Локализация
├── Scenes/
│   ├── MainMenu.unity                     Главное меню
│   ├── 1lvl.unity                         Warmup
│   ├── 2lvl.unity                         Miqat
│   └── 3lvl.unity                         Tawaf
├── Scripts/
│   ├── Core/                              Bootstrapper, GameManager, States/
│   ├── Data/                              Language, LevelData, UserProfile, QuizQuestion
│   ├── Editor/                            Инструменты редактора
│   ├── Gameplay/                          QuizSystem, RewardSystem
│   ├── Services/                          Все сервисы + ProfileDataProvider/
│   └── UI/                                13 UI-контроллеров
├── Settings/                              Рендеринг
├── TextMesh Pro/                          TMP (3rd party)
└── UI Toolkit/                            UI Toolkit
```

---

## 🎬 Сцены

| Сцена | Файл | Содержимое |
|---|---|---|
| **MainMenu** | `MainMenu.unity` | Стартовый экран, выбор языка. `App.prefab` с `GameManager` и `Bootstrapper` |
| **Warmup** | `1lvl.unity` | Подготовка к Хаджу: 3 блока теории → викторина |
| **Miqat** | `2lvl.unity` | Микат: 2 блока теории → викторина с бонусами за скорость |
| **Tawaf** | `3lvl.unity` | Таваф: 2 блока теории → викторина со стрик-системой |

---

## 🧩 Префабы

### App

| Префаб | Описание |
|---|---|
| **App.prefab** | `GameManager` (Singleton) + `Bootstrapper`. В начальной сцене |
| **Services.prefab** | `AudioService`, `StageCompletionService`, `QuizService`, `GameStateMachine` |

### UI

| Префаб | Описание |
|---|---|
| **StartScreen.prefab** | Логотип, «Начать», выбор языка |
| **LevelBtn.prefab** | Кнопка уровня: миниатюра, прогресс-бар, замок |
| **QuestionCotroller.prefab** | Вопрос: текст, 4 ответа, фидбэк |
| **Canvas.prefab** | UI Canvas для сцен уровней |
| **LevelController.prefab** | Переключение теория ↔ квиз |
| **LevelSelect.prefab** | Список уровней, прогресс, гемы |
| **levels.prefab** | Контейнер кнопок уровней |

### Theory

| Префаб | Описание |
|---|---|
| **CardViewer.prefab** | Просмотр карточек: пролистывание, анимации |
| **WarmUpTheoryCardView.prefab** | Расширенная карточка для Warmup |

---

## 📝 Описание скриптов

### Core (4)

| Скрипт | Описание |
|---|---|
| `Bootstrapper.cs` | Точка входа. Регистрация сервисов, запуск `MainMenuState` |
| `GameManager.cs` | Singleton Service Locator, `DontDestroyOnLoad`, `GetService<T>()` |
| `GameplaySceneInitializer.cs` | Находит `QuizSystem`/`RewardSystem` в сцене, подключает к состоянию |
| `LevelManager.cs` | `StartLevel()`, `RestartLevel()`, `ShowResults()` |

### States (11)

| Скрипт | Описание |
|---|---|
| `BaseGameState.cs` | Абстрактный: `StateId`, `Enter()`, `Update()`, `Exit()`, `OnPause()`, `OnResume()` |
| `GameStateMachine.cs` | FSM: 6 состояний, переходы, пауза, события |
| `BaseLevelState.cs` | Базовый для уровней: теория → квиз → сохранение |
| `LevelStateIds.cs` | Константы: `MainMenu`, `LevelSelect`, `Results`, `Warmup`, `Miqat`, `Tawaf` |
| `LevelStateMachine.cs` | Legacy FSM (обратная совместимость) |
| `MainMenuState.cs` | `ui.ShowMainMenu()` |
| `LevelSelectState.cs` | `ui.ShowLevelSelect()`, прогресс и гемы |
| `WarmupLevelState.cs` | 3 блока теории, без бонусов |
| `MiqatLevelState.cs` | 2 блока, таймер, бонус скорости и за ≥90% |
| `TawafLevelState.cs` | 2 блока, стрик, круги (7 вопросов), бонусы |
| `ResultsState.cs` | `ui.ShowResults()` |

### Theory (7)

| Скрипт | Описание |
|---|---|
| `TheoryCardBase.cs` | Абстрактный базовый класс карточки |
| `SimpleTheoryCard.cs` | Простая карточка: текст + изображение |
| `WarmUpTheoryCard.cs` | Расширенная карточка для Warmup |
| `TheoryCardData.cs` | Данные: заголовок, текст, изображение, ключи локализации |
| `TheoryCardContainer.cs` | Контейнер карточек, навигация |
| `TheoryCardsManager.cs` | Жизненный цикл: загрузка, показ, скрытие |
| `TheoryToQuizIntegration.cs` | Мост: завершение теории → запуск квиза |

### Data (5)

| Скрипт | Описание |
|---|---|
| `Language.cs` | Enum: Russian, Bosnian, Albanian, Turkish, Arabic, Indonesian, English |
| `LevelData.cs` | SO уровня: `LevelId`, `Questions[]`, `CompletionBonusGems`, `PassThreshold` |
| `LevelProgress.cs` | Прогресс уровня |
| `QuizQuestion.cs` | Вопрос: `QuestionText`, `Options[4]`, `CorrectAnswerIndex`, `GemsReward` |
| `UserProfile.cs` | Профиль: имя, прогресс, гемы, пройденные уровни |

### Services (8)

| Скрипт | Описание |
|---|---|
| `LocalizationService.cs` | CSV-локализация, `GetText()`, `ChangeLanguage()`, `OnLanguageChanged` |
| `LocalizationServiceLoader.cs` | Утилита загрузки |
| `AudioService.cs` | `PlaySound()`, `PlayClick()`, `PlayWhoosh()` |
| `UserProfileService.cs` | Профиль: `GetProfile()`, `UpdateProfile()`, `Save()` |
| `ProfileLoaderService.cs` | Strategy: `Load()`, `Save()`, `SyncWithBackendAsync()` |
| `ProgressService.cs` | `RecordLevelProgress()`, `GetLevelProgress()`, `IsLevelCompleted()` |
| `QuizService.cs` | `InitializeQuiz()`, `SubmitAnswer()`, `GetLastScorePercent()` |
| `StageCompletionService.cs` | `CompleteStage()`, `RecordLevelResult()`, `GetLevelScore()` |

### Profile Data Providers (4)

| Скрипт | Описание |
|---|---|
| `IProfileDataProvider.cs` | Интерфейс: `Load()`, `Save()`, `Priority` |
| `PlayerPrefsProfileProvider.cs` | PlayerPrefs (JSON), приоритет 50 |
| `FileProfileProvider.cs` | JSON-файл, приоритет 10 |
| `BackendProfileProvider.cs` | REST API, приоритет 100 |

### Gameplay (2)

| Скрипт | Описание |
|---|---|
| `QuizSystem.cs` | Исполнитель квиза: `Initialise()`, `SubmitAnswer()`, `Advance()` |
| `RewardSystem.cs` | `AwardGems()` → `GameManager.AddGems()` |

### UI (13)

| Скрипт | Описание |
|---|---|
| `UIService.cs` | Центральный UI: `ShowMainMenu()`, `ShowLevelSelect()`, `ShowResults()` |
| `GameTextController.cs` | Локализованный TMP_Text, авто-обновление |
| `LanguageSelectController.cs` | Выбор языка |
| `GameplayUI.cs` | Панель геймплея |
| `QuizUIController.cs` | Вопрос, ответы, фидбэк |
| `ResultsUI.cs` | Счёт, гемы, «Далее»/«Повтор» |
| `SelectMenuUIController.cs` | Меню выбора уровней |
| `MainGameplayMenuUI.cs` | Пауза, рестарт, назад |
| `PauseMenuUI.cs` | Продолжить, рестарт, выход |
| `LevelTileUI.cs` | Плитка уровня: миниатюра, прогресс, замок |
| `StageGameplayController.cs` | Контроллер теории, «Далее» |
| `ButtonEffect.cs` | Эффекты кнопок (DOTween) |

### Level Logic (1)

| Скрипт | Описание |
|---|---|
| `LevelController.cs` | Переключение между теорией и квизом |

### Editor (3, `#if UNITY_EDITOR`)

| Скрипт | Описание |
|---|---|
| `ContentLoaderWindow.cs` | Загрузка контента из внешних источников |
| `DataLoader.cs` | Загрузка данных квизов/уровней |
| `LocalizationEditorService.cs` | Работа с локализацией в Editor |

---

## 🏆 Вознаграждения по уровням

```
┌─ WARMUP ─────────────────────────────────────────────────────┐
│  Базовые гемы: question.GemsReward                            │
│  Бонусы: нет  ·  Блоков теории: 3                            │
└───────────────────────────────────────────────────────────────┘

┌─ MIQAT ──────────────────────────────────────────────────────┐
│  Базовые гемы + скорость (+2 < 3 мин)                        │
│  + отличие (+50% CompletionBonusGems при ≥ 90%)              │
│  Блоков теории: 2                                             │
└───────────────────────────────────────────────────────────────┘

┌─ TAWAF ──────────────────────────────────────────────────────┐
│  Базовые гемы + стрик (streak×2 с 3-го подряд)               │
│  + идеальный круг (+20 за 7 подряд)                          │
│  + идеальный Таваф (+50 за 100%)                             │
│  Круги: каждые 7 вопросов  ·  Блоков теории: 2              │
└───────────────────────────────────────────────────────────────┘
```

---

## 🧱 Архитектурные паттерны

| Паттерн | Где | Файл |
|---|---|---|
| **Service Locator** | Доступ к сервисам | `GameManager.cs` |
| **Singleton** | Единственный GameManager | `GameManager.cs` |
| **State Machine** | Состояния игры | `GameStateMachine.cs` |
| **Template Method** | Жизненный цикл уровней | `BaseLevelState.cs` |
| **Strategy** | Мульти-провайдерная загрузка | `ProfileLoaderService.cs` |
| **Observer** | События квиза, языка, уровней | `QuizSystem`, `LocalizationService` |
| **Factory** | Создание состояний | `GameStateMachine.cs` |

---

## 🚀 Быстрый старт

1. Открыть проект в **Unity 2022.3+**
2. Открыть `Assets/Scenes/MainMenu.unity`
3. Убедиться что `App.prefab` на сцене с привязанными сервисами
4. **Play** — старт в `MainMenuState`

**Уровни:** `Assets/Resources/SO/Levels/` → Assets → Create → Manasik → Level Data

**Локализация:** `Assets/Resources/localization.csv` → Editor → Content Loader → Download from Google Sheets

**Новый уровень:**
1. Наследник `BaseLevelState` с уникальным `StateId`
2. Константа в `GameStateIds`
3. Регистрация в `GameStateMachine.RegisterDefaultStates()`
4. `LevelData` ScriptableObject + сцена с `QuizSystem` и `RewardSystem`
