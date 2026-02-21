# 📊 Структура Проекта HajjFlow - State Machine Architecture

## 🗂️ Полная Структура Файлов

```
HajjFlow/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                           
│   │   │   ├── GameManager.cs              ← Singleton, сервисы
│   │   │   ├── LevelManager.cs             ← ✨ Управление уровнями (обновлен)
│   │   │   ├── GameplaySceneInitializer.cs ← ✨ NEW: Инициализация сцены
│   │   │   └── States/                     ← ✨ NEW: Машина состояний
│   │   │       ├── BaseLevelState.cs       ← ✨ Базовый класс
│   │   │       ├── LevelStateMachine.cs    ← ✨ Машина состояний
│   │   │       ├── LevelStateIds.cs        ← ✨ Константы
│   │   │       ├── WarmupLevelState.cs     ← ✨ Состояние Warmup
│   │   │       ├── MiqatLevelState.cs      ← ✨ Состояние Miqat
│   │   │       ├── TawafLevelState.cs      ← ✨ Состояние Tawaf
│   │   │       └── README.md               ← Документация
│   │   │
│   │   ├── Data/
│   │   │   ├── LevelData.cs                ← ScriptableObject
│   │   │   ├── QuizQuestion.cs             ← Структура вопроса
│   │   │   └── UserProfile.cs              ← Профиль игрока
│   │   │
│   │   ├── Services/
│   │   │   ├── UserProfileService.cs       ← Управление профилем
│   │   │   └── ProgressService.cs          ← Прогресс и разблокировка
│   │   │
│   │   ├── Gameplay/
│   │   │   ├── QuizSystem.cs               ← Викторина
│   │   │   └── RewardSystem.cs             ← Награды
│   │   │
│   │   └── UI/
│   │       ├── MainMenuUI.cs               ← Главное меню
│   │       ├── LevelSelectionUI.cs         ← ✨ Выбор уровня (обновлен)
│   │       ├── LevelTileUI.cs              ← Плитка уровня
│   │       ├── GameplayUI.cs               ← Игровой UI
│   │       ├── PauseMenuUI.cs              ← ✨ NEW: Меню паузы
│   │       ├── ResultsUI.cs                ← Экран результатов
│   │       └── SelectMenuUIController.cs   ← Контроллер меню
│   │
│   ├── Scenes/
│   │   └── main.unity                      ← Главная сцена
│   │
│   └── ScriptableObjects/
│       └── Levels/
│           ├── lvl 1.asset                 → Warmup LevelData
│           ├── lvl 2.asset                 → Miqat LevelData
│           └── lvl 3.asset                 → Tawaf LevelData
│
├── ARCHITECTURE.md                          ← ✨ NEW: Архитектура
├── SCENE_SETUP.md                           ← ✨ NEW: Настройка сцены
├── STATE_MACHINE_DIAGRAM.md                 ← ✨ NEW: Диаграммы
└── STATE_MACHINE_SUMMARY.md                 ← ✨ NEW: Сводка
```

---

## 🎯 Ключевые Компоненты

### 1️⃣ GameManager (Singleton, DontDestroyOnLoad)
```
┌─────────────────────────────┐
│       GameManager           │
├─────────────────────────────┤
│ + Instance (static)         │
│ + ProfileService            │
│ + ProgressService           │
│ + AddGems(amount)           │
└─────────────────────────────┘
```

### 2️⃣ LevelManager (Static Helper)
```
┌─────────────────────────────────────┐
│          LevelManager               │
├─────────────────────────────────────┤
│ + ActiveLevel : LevelData           │
│ + ActiveStateId : string            │
│ + StateMachine : LevelStateMachine  │
├─────────────────────────────────────┤
│ + StartLevel(level, stateId)        │
│ + RestartLevel()                    │
│ + GoToLevelSelect()                 │
│ + GoToMainMenu()                    │
│ + ShowResults()                     │
└─────────────────────────────────────┘
```

### 3️⃣ LevelStateMachine (MonoBehaviour в Gameplay)
```
┌─────────────────────────────────────────┐
│       LevelStateMachine                 │
├─────────────────────────────────────────┤
│ - _states: Dictionary<string, State>    │
│ - _currentState: BaseLevelState         │
│ - _isPaused: bool                       │
├─────────────────────────────────────────┤
│ + StartLevel(stateId, levelData)        │
│ + ChangeState(stateId, levelData)       │
│ + Pause() / Resume()                    │
│ + CompleteLevel(scorePercent)           │
├─────────────────────────────────────────┤
│ Events:                                 │
│ - OnStateChanged                        │
│ - OnLevelCompleted                      │
└─────────────────────────────────────────┘
```

### 4️⃣ BaseLevelState (Abstract)
```
┌─────────────────────────────────────┐
│       BaseLevelState                │
├─────────────────────────────────────┤
│ # _stateMachine                     │
│ # _levelData                        │
├─────────────────────────────────────┤
│ + StateId : string (abstract)       │
│ + Initialize(sm, data)              │
│ + Enter()         (virtual)         │
│ + Update()        (virtual)         │
│ + Exit()          (virtual)         │
│ + OnPause()       (virtual)         │
│ + OnResume()      (virtual)         │
└─────────────────────────────────────┘
              ▲
              │
    ┌─────────┼─────────┐
    │         │         │
┌───┴───┐ ┌───┴───┐ ┌───┴───┐
│Warmup │ │Miqat  │ │Tawaf  │
│State  │ │State  │ │State  │
└───────┘ └───────┘ └───────┘
```

---

## 🔄 Поток Данных

### Запуск Уровня:
```
1. Player → clicks level tile
2. LevelSelectionUI → determines StateId from LevelId
3. LevelManager.StartLevel(levelData, stateId)
   ├── Set ActiveLevel
   ├── Set ActiveStateId
   └── Load Gameplay Scene
4. GameplaySceneInitializer.Awake()
   └── LevelManager.RegisterStateMachine(stateMachine)
5. LevelStateMachine.StartLevel(stateId, levelData)
   ├── Find state by stateId
   ├── state.Initialize(stateMachine, levelData)
   └── state.Enter()
6. State.Enter()
   ├── Find QuizSystem
   ├── Subscribe to events
   └── Initialize quiz
7. Game Loop: state.Update() (every frame)
8. Quiz Complete → state.OnQuizComplete()
   ├── Save progress
   ├── Unlock next level
   └── stateMachine.CompleteLevel()
9. Show Results
```

### Обработка Ответов:
```
Player → Clicks Answer
   ↓
GameplayUI.OnOptionSelected(index)
   ↓
QuizSystem.SubmitAnswer(index)
   ↓
QuizSystem.OnAnswerResult event
   ↓
State.OnAnswerResult(correct, explanation)
   ↓
├── Update stats
├── Award gems (if correct)
└── Check for bonuses (Miqat/Tawaf)
```

---

## 🎨 Иерархия Сцены Gameplay

```
Gameplay Scene
│
├── 🎮 GameController
│   ├── [LevelStateMachine]
│   └── [GameplaySceneInitializer]
│
├── ⚙️ Systems
│   ├── QuizSystem
│   │   └── [QuizSystem Component]
│   └── RewardSystem
│       └── [RewardSystem Component]
│
├── 🖼️ UI (Canvas)
│   ├── TopBar
│   │   ├── LevelName (TMP)
│   │   ├── Progress (TMP)
│   │   └── Gems (TMP)
│   │
│   ├── QuizPanel
│   │   ├── QuestionText (TMP)
│   │   ├── OptionsGroup
│   │   │   ├── OptionA (Button)
│   │   │   ├── OptionB (Button)
│   │   │   ├── OptionC (Button)
│   │   │   └── OptionD (Button)
│   │   ├── FeedbackText (TMP)
│   │   └── NextButton (Button)
│   │
│   ├── PauseMenu (initially hidden)
│   │   ├── PausePanel
│   │   ├── ResumeButton
│   │   ├── RestartButton
│   │   └── MainMenuButton
│   │
│   └── Navigation
│       ├── PauseButton
│       ├── BackButton
│       └── RestartButton
│
└── 🌍 Environment (Optional)
    ├── Background
    ├── 3D Models
    └── Lighting
```

### Inspector Assignments (GameplayUI):

**[Top Bar]**
- Level Name Text → TopBar/LevelName
- Progress Text → TopBar/Progress  
- Gems Text → TopBar/Gems

**[Quiz Panel]**
- Question Text → QuizPanel/QuestionText
- Option Buttons[0-3] → QuizPanel/OptionsGroup/OptionA-D
- Feedback Text → QuizPanel/FeedbackText
- Next Button → QuizPanel/NextButton

**[Navigation]**
- Back Button → Navigation/BackButton
- Restart Button → Navigation/RestartButton

**[Systems]**
- Quiz System Ref → Systems/QuizSystem
- Reward System Ref → Systems/RewardSystem

---

## 📈 Метрики и Аналитика

Каждое состояние логирует:
- Время входа/выхода
- Количество правильных/неправильных ответов
- Начисленные награды
- Время прохождения
- Результат (scorePercent)

Используйте эти данные для балансировки сложности и наград.

---

## 🏆 Progression System

```
Warmup (Level 1)
├── Complete with ≥60% → Unlock Miqat
└── Awards: 20💎 + question rewards

Miqat (Level 2)  
├── Complete with ≥70% → Unlock Tawaf
├── Speed Bonus: Answer in <3min → +2💎
├── Excellence: Score ≥90% → +15💎
└── Awards: 30💎 + question rewards + bonuses

Tawaf (Level 3)
├── Complete with ≥80% → Hajj Complete!
├── Streak Bonus: 3+ correct → streak×2💎
├── Perfect Circle: 7 correct in row → +20💎
├── Perfect Tawaf: 100% score → +50💎
└── Awards: 50💎 + question rewards + bonuses
```

---

## ✅ Проверка Реализации

Запустите проект и проверьте в Console:

### При запуске Warmup:
```
[LevelManager] Starting level: ... with state: warmup
[LevelStateMachine] Registered state: warmup
[LevelStateMachine] Registered state: miqat
[LevelStateMachine] Registered state: tawaf
[GameplaySceneInitializer] Scene initialized with state machine
[LevelStateMachine] State changed to: warmup
[WarmupLevelState] Entering state: ...
[WarmupLevelState] Starting warmup level with X questions
```

### При правильном ответе:
```
[WarmupLevelState] Question 1/5: ...
[GameManager] +5 gems. Total: 25
[WarmupLevelState] Correct answer! ...
```

### При завершении:
```
[WarmupLevelState] Quiz completed with score: 80%
[LevelStateMachine] Level completed: warmup, Score: 80%
[GameplaySceneInitializer] Level 'warmup' completed with 80%
```

---

*Все файлы созданы и готовы к использованию!* ✨

