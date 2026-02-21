# State Machine Architecture - Class Diagram

## 📦 Namespace: HajjFlow.Core.States

```
┌─────────────────────────────────────────────────────────────┐
│                    BaseLevelState                           │
│                    (abstract class)                         │
├─────────────────────────────────────────────────────────────┤
│ # _stateMachine : LevelStateMachine                         │
│ # _levelData : LevelData                                    │
├─────────────────────────────────────────────────────────────┤
│ + StateId : string { get; } (abstract)                      │
│ + Initialize(stateMachine, levelData) : void                │
│ + Enter() : void (virtual)                                  │
│ + Update() : void (virtual)                                 │
│ + Exit() : void (virtual)                                   │
│ + OnPause() : void (virtual)                                │
│ + OnResume() : void (virtual)                               │
└─────────────────────────────────────────────────────────────┘
                            △
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        │                   │                   │
┌───────┴────────┐  ┌───────┴────────┐  ┌───────┴────────┐
│ WarmupLevel    │  │ MiqatLevel     │  │ TawafLevel     │
│ State          │  │ State          │  │ State          │
├────────────────┤  ├────────────────┤  ├────────────────┤
│ StateId:       │  │ StateId:       │  │ StateId:       │
│   "warmup"     │  │   "miqat"      │  │   "tawaf"      │
├────────────────┤  ├────────────────┤  ├────────────────┤
│ - _quizSystem  │  │ - _quizSystem  │  │ - _quizSystem  │
│ - _questions   │  │ - _reward      │  │ - _reward      │
│   Answered     │  │   System       │  │   System       │
│                │  │ - _startTime   │  │ - _startTime   │
│                │  │ - _correct     │  │ - _consecutive │
│                │  │   Answers      │  │   Correct      │
└────────────────┘  └────────────────┘  └────────────────┘


┌─────────────────────────────────────────────────────────────┐
│              LevelStateMachine                              │
│              (MonoBehaviour)                                │
├─────────────────────────────────────────────────────────────┤
│ - _states : Dictionary<string, BaseLevelState>              │
│ - _currentState : BaseLevelState                            │
│ - _isPaused : bool                                          │
├─────────────────────────────────────────────────────────────┤
│ + CurrentState : BaseLevelState { get; }                    │
│ + IsPaused : bool { get; }                                  │
├─────────────────────────────────────────────────────────────┤
│ + StartLevel(stateId, levelData) : void                     │
│ + ChangeState(stateId, levelData) : void                    │
│ + Pause() : void                                            │
│ + Resume() : void                                           │
│ + CompleteLevel(scorePercent) : void                        │
├─────────────────────────────────────────────────────────────┤
│ Events:                                                     │
│ + OnStateChanged : Action<BaseLevelState>                   │
│ + OnLevelCompleted : Action<string, float>                  │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Sequence Diagram - Starting a Level

```
Player          LevelSelectionUI      LevelManager      SceneManager      GameplayScene     LevelStateMachine    WarmupState
  │                    │                    │                  │                 │                │                │
  │  Click Level       │                    │                  │                 │                │                │
  ├───────────────────>│                    │                  │                 │                │                │
  │                    │                    │                  │                 │                │                │
  │              StartLevel(levelData, "warmup")               │                 │                │                │
  │                    ├───────────────────>│                  │                 │                │                │
  │                    │                    │                  │                 │                │                │
  │                    │           Set ActiveLevel            │                 │                │                │
  │                    │           Set ActiveStateId          │                 │                │                │
  │                    │                    │                  │                 │                │                │
  │                    │              LoadScene("Gameplay")    │                 │                │                │
  │                    │                    ├─────────────────>│                 │                │                │
  │                    │                    │                  │                 │                │                │
  │                    │                    │              Scene Loaded          │                │                │
  │                    │                    │                  ├────────────────>│                │                │
  │                    │                    │                  │                 │                │                │
  │                    │                    │                  │      Awake()    │                │                │
  │                    │                    │                  │     RegisterStateMachine()       │                │
  │                    │                    │<─────────────────┼─────────────────┤                │                │
  │                    │                    │                  │                 │                │                │
  │                    │                    │        StartLevel(ActiveStateId, ActiveLevel)       │                │
  │                    │                    ├─────────────────────────────────────>               │                │
  │                    │                    │                  │                 │                │                │
  │                    │                    │                  │                 │  Initialize()  │                │
  │                    │                    │                  │                 ├───────────────>│                │
  │                    │                    │                  │                 │                │                │
  │                    │                    │                  │                 │    Enter()     │                │
  │                    │                    │                  │                 ├───────────────>│                │
  │                    │                    │                  │                 │                │                │
  │                    │                    │                  │                 │  Setup Quiz    │                │
  │                    │                    │                  │                 │<───────────────┤                │
  │                    │                    │                  │                 │                │                │
  │                    │                    │                  │          Update Loop              │                │
  │                    │                    │                  │                 ├───────────────>│                │
  │                    │                    │                  │                 │                │ Update()       │
  │                    │                    │                  │                 │                │────┐           │
  │                    │                    │                  │                 │                │    │ (per frame)
  │                    │                    │                  │                 │                │<───┘           │
```

## 🏛️ Component Dependencies

```
GameManager (DontDestroyOnLoad)
    │
    ├── UserProfileService
    ├── ProgressService
    └── LevelManager (component)


Gameplay Scene:
    │
    ├── GameController
    │   ├── LevelStateMachine ──────┐
    │   └── GameplaySceneInitializer│
    │                                │
    ├── Systems                      │
    │   ├── QuizSystem ◄─────────────┼── Used by states
    │   └── RewardSystem ◄───────────┤
    │                                │
    └── UI                           │
        └── GameplayUI ──────────────┘
```

## 📊 State Transition Table

| From State | Event              | To State | Condition                     |
|------------|--------------------|---------|-----------------------------|
| (None)     | StartLevel         | Warmup  | LevelId contains "warmup"   |
| (None)     | StartLevel         | Miqat   | LevelId contains "miqat"    |
| (None)     | StartLevel         | Tawaf   | LevelId contains "tawaf"    |
| Warmup     | Quiz Complete      | Results | Score calculated            |
| Miqat      | Quiz Complete      | Results | Score calculated            |
| Tawaf      | Quiz Complete      | Results | Score calculated            |
| Any        | Pause Button       | (Paused)| Time.timeScale = 0          |
| (Paused)   | Resume Button      | Any     | Time.timeScale = 1          |
| Any        | Back Button        | LevelSelect | User exits level         |
| Any        | Restart Button     | Same    | Reload same state           |

## 🎁 Reward Logic by State

| State  | Base Reward | Special Bonuses                                    |
|--------|-------------|---------------------------------------------------|
| Warmup | Per Q: 5💎  | - Completion: 20💎                                |
| Miqat  | Per Q: 5💎  | - Speed (<3min): +2💎<br>- Excellence (≥90%): +15💎<br>- Completion: 30💎 |
| Tawaf  | Per Q: 5💎  | - Streak (3+): streak×2💎<br>- Perfect Circle (7): +20💎<br>- Perfect (100%): +50💎<br>- Completion: 50💎 |

---

## 🧪 Testing Checklist

### Unit Tests (Рекомендуется создать)
- [ ] BaseLevelState lifecycle methods
- [ ] LevelStateMachine state transitions
- [ ] LevelStateIds helper methods
- [ ] Reward calculations for each state

### Integration Tests
- [ ] Start Warmup level → Quiz works → Complete
- [ ] Start Miqat level → Speed bonus works
- [ ] Start Tawaf level → Streak bonus works
- [ ] Pause/Resume during gameplay
- [ ] Restart level maintains state
- [ ] Back to level select clears state

### UI Tests
- [ ] Top bar displays correct info
- [ ] Quiz questions display correctly
- [ ] Answer feedback shows properly
- [ ] Gems counter updates in real-time
- [ ] Progress bar updates
- [ ] Results screen shows after completion

---

*Generated: 21 Feb 2026*

