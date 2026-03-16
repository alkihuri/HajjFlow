# 🔍 Senior Unity Developer — Code Review: HajjFlow

> **Дата ревью:** Март 2026  
> **Тип:** Полное ревью архитектуры, доменной логики, персистенции и рекомендации по бекенду  
> **Статус:** Только рекомендации. Код не изменён.

---

## Содержание

1. [Общая архитектура](#1-общая-архитектура)
2. [Критические баги](#2-критические-баги)
3. [Математика подсчёта очков и верификация ответов](#3-математика-подсчёта-очков-и-верификация-ответов)
4. [Система прогресса — сохранение и загрузка](#4-система-прогресса--сохранение-и-загрузка)
5. [Стабильность и оптимизация](#5-стабильность-и-оптимизация)
6. [Предложение по Python-бекенду](#6-предложение-по-python-бекенду)
7. [Приоритетный план исправлений](#7-приоритетный-план-исправлений)

---

## 1. Общая архитектура

### ✅ Что хорошо

- **Service Locator** через `GameManager` — единый реестр сервисов, удобен для малого проекта
- **State Machine** для управления уровнями (`BaseLevelState` → `WarmupLevelState`, `MiqatLevelState`, `TawafLevelState`)
- **Strategy Pattern** в `IProfileDataProvider` с приоритизацией провайдеров (Backend → PlayerPrefs → File)
- **Event-driven** подход в квиз-системе через C# events
- Чёткое разделение Data/Services/UI/Core/Gameplay

### ⚠️ Проблемы

| Проблема | Файлы | Критичность |
|----------|-------|-------------|
| Дублирование квиз-систем (`QuizSystem` + `QuizService`) | `QuizSystem.cs`, `QuizService.cs` | 🔴 Высокая |
| Дублирование персистенции (`UserProfileService` + `ProfileLoaderService`) | `UserProfileService.cs`, `ProfileLoaderService.cs` | 🔴 Высокая |
| `FindObjectOfType` в горячем пути | `BaseLevelState.cs:52-53` | 🟡 Средняя |
| `Debug.Log` в `Update()` | `TawafLevelState.cs:52-57` | 🟡 Средняя |
| Отсутствие DI-контейнера (всё через `GameManager.Instance?`) | Весь проект | 🟡 Средняя |

### Рекомендации

1. **Убрать одну из квиз-систем.** `QuizSystem` (MonoBehaviour в сцене) и `QuizService` (зарегистрированный сервис) делают одно и то же. Рекомендация: оставить только `QuizService`, а `QuizSystem` удалить или сделать тонкой обёрткой для сцены, которая делегирует всё в `QuizService`.

2. **Объединить `UserProfileService` и `ProfileLoaderService`.** Оба управляют профилем, оба сохраняют. В `BaseLevelState.SaveProgress()` оба вызываются — это двойная запись. Рекомендация: `ProfileLoaderService` должен быть единственным менеджером профиля, а `UserProfileService` стать фасадом.

3. **Заменить `FindObjectOfType` на инъекцию через сцену или сервис-локатор:**
   ```csharp
   // Вместо:
   _quizSystem = Object.FindObjectOfType<QuizSystem>();
   
   // Использовать:
   _quizSystem = GameManager.Instance?.GetService<QuizSystem>();
   ```

---

## 2. Критические баги

### 🔴 BUG #1: Прогресс перезаписывается нулём при досрочном выходе

**Файл:** `BaseLevelState.cs:73-78`

```csharp
public override void Exit()
{
    SaveProgress();          // ← Всегда вызывается!
    UnsubscribeQuizEvents();
}
```

**Проблема:** `SaveProgress()` вызывается при КАЖДОМ выходе из состояния — даже если квиз не начинался. Начальное значение `_lastScorePercent = 0f`. Это значит:

1. Игрок завершает уровень с 85% → сохраняется 85%
2. Игрок заходит на тот же уровень, но выходит досрочно → сохраняется 0%
3. Предыдущий результат 85% **ПОТЕРЯН навсегда**

**Рекомендация:**
```csharp
protected virtual void SaveProgress()
{
    // Не сохранять, если квиз даже не начинался
    if (_questionsAnswered == 0) return;
    
    // Не перезаписывать лучший результат худшим
    float existingProgress = progressService.GetLevelProgress(_levelData.LevelId);
    if (_lastScorePercent <= existingProgress) return;
    
    // ... остальная логика
}
```

### 🔴 BUG #2: Уровень маркируется как «завершённый» без проверки порога

**Файл:** `BaseLevelState.cs:179-182`

```csharp
// Добавляем в завершённые если ещё нет
if (!profile.CompletedLevelIds.Contains(_levelData.LevelId))
{
    profile.CompletedLevelIds.Add(_levelData.LevelId);
}
```

**Проблема:** Уровень добавляется в `CompletedLevelIds` **без проверки `PassThreshold`**. Даже с 0% результатом уровень будет помечен как пройденный.

Это конфликтует с `ProgressService.RecordLevelProgress()` (строка 29), который ПРАВИЛЬНО проверяет порог:
```csharp
if (progressPercent >= passThreshold &&
    !profile.CompletedLevelIds.Contains(levelId))
```

Но в `SaveProgress()` после вызова `ProgressService` идёт ПРЯМАЯ запись в `ProfileLoaderService`, которая обходит эту проверку.

**Рекомендация:** Убрать дублирующую запись или добавить проверку:
```csharp
if (_lastScorePercent >= _levelData.PassThreshold &&
    !profile.CompletedLevelIds.Contains(_levelData.LevelId))
{
    profile.CompletedLevelIds.Add(_levelData.LevelId);
}
```

### 🔴 BUG #3: `BackendProfileProvider.Load()` — потенциальный deadlock

**Файл:** `BackendProfileProvider.cs:30-31`

```csharp
public UserProfile Load()
{
    return LoadAsync().GetAwaiter().GetResult();  // ← DEADLOCK!
}
```

**Проблема:** В Unity `SynchronizationContext` — это основной поток. Вызов `.GetAwaiter().GetResult()` заблокирует основной поток в ожидании задачи, которая может пытаться вернуться в тот же основной поток. Это **классический deadlock в Unity**.

**Рекомендация:** Использовать `UniTask` или паттерн callback:
```csharp
public UserProfile Load()
{
    Debug.LogWarning("[Backend] Sync load not supported. Return null.");
    return null;  // Backend всегда async-only
}
```

### 🟡 BUG #4: Двойная запись профиля в `SaveProgress()`

**Файл:** `BaseLevelState.cs:147-196`

```csharp
protected virtual void SaveProgress()
{
    // Запись 1: через ProgressService → UpdateProfile → Save
    progressService.RecordLevelProgress(...);
    
    // Запись 2: через ProfileLoaderService → Save
    profileLoader.Save(profile);
}
```

Профиль записывается **дважды** за один вызов. Это:
- Лишняя нагрузка на I/O (PlayerPrefs + File × 2)
- Потенциальные race condition при быстрых вызовах
- Данные `ProgressService` могут расходиться с `ProfileLoaderService`

---

## 3. Математика подсчёта очков и верификация ответов

### 3.1 Подсчёт score (процент правильных ответов)

**`QuizSystem.cs:86-88`:**
```csharp
float scorePercent = (_questions.Length > 0)
    ? (float)_correctCount / _questions.Length * 100f
    : 0f;
```

**`QuizService.cs:141`:**
```csharp
float scorePercent = (float)correctAnswerCount / currentQuestions.Length * 100;
```

**Анализ:**
- ✅ Формула `correct / total * 100` — математически корректна
- ✅ Защита от деления на ноль в `QuizSystem` (проверка `_questions.Length > 0`)
- ⚠️ **Нет защиты от деления на ноль в `QuizService`!** Если `currentQuestions.Length == 0`, будет `DivideByZeroException`
- ⚠️ Два разных места считают один и тот же процент — потенциальный source of truth конфликт

**Рекомендация:**
```csharp
// В QuizService.CompleteQuiz():
float scorePercent = currentQuestions.Length > 0 
    ? (float)correctAnswerCount / currentQuestions.Length * 100f 
    : 0f;
```

### 3.2 Верификация правильного ответа

**`QuizSystem.cs:67`:**
```csharp
bool correct = (selectedIndex == question.CorrectAnswerIndex);
```

**`QuizService.cs:91`:**
```csharp
bool isCorrect = selectedAnswerIndex == currentQuestion.CorrectAnswerIndex;
```

**Анализ:**
- ✅ Простое сравнение индекса — корректно
- ✅ `QuizService` валидирует границы: `selectedAnswerIndex < 0 || selectedAnswerIndex >= currentQuestion.Options.Length`
- ⚠️ `QuizSystem` **НЕ валидирует** границы `selectedIndex` — можно передать -1 или 999
- ⚠️ Нет валидации, что `CorrectAnswerIndex` самого вопроса находится в пределах массива `Options`

**Рекомендация:** добавить валидацию в оба места:
```csharp
public void SubmitAnswer(int selectedIndex)
{
    if (!_awaitingAnswer || _questions == null) return;
    
    var question = _questions[_currentIndex];
    
    // Валидация индекса
    if (selectedIndex < 0 || selectedIndex >= question.Options.Length) return;
    
    // Валидация данных вопроса
    if (question.CorrectAnswerIndex < 0 || 
        question.CorrectAnswerIndex >= question.Options.Length) 
    {
        Debug.LogError("Invalid CorrectAnswerIndex!");
        return;
    }
    // ...
}
```

### 3.3 Алгоритм ShuffleOptions (Fisher-Yates)

**`QuizQuestion.cs:47-63`:**
```csharp
public void ShuffleOptions()
{ 
    for (int i = Options.Length - 1; i > 0; i--)
    {
        int j = UnityEngine.Random.Range(0, i + 1);
        string tempOption = Options[i];
        Options[i] = Options[j];
        Options[j] = tempOption;

        if (i == CorrectAnswerIndex)
            CorrectAnswerIndex = j;
        else if (j == CorrectAnswerIndex)
            CorrectAnswerIndex = i;
    }
}
```

**Анализ:**
- ✅ Fisher-Yates реализован правильно (от конца к началу, `Random.Range(0, i+1)`)
- ✅ Отслеживание `CorrectAnswerIndex` при обмене — логически корректно
- ⚠️ **Мутирует оригинальный объект!** Поскольку `QuizQuestion[]` берётся из `LevelData` (ScriptableObject), повторные вызовы шаффла мутируют данные ScriptableObject в памяти. В редакторе это может привести к порче ассета.
- ⚠️ `ShuffleOptions()` вызывается в `QuizService.DisplayCurrentQuestion()` при КАЖДОМ показе вопроса. Если один и тот же вопрос отображается дважды, он перетасуется повторно (но это не ломает корректность благодаря правильному трекингу индекса).

**Рекомендация:** Работать с копией вопросов:
```csharp
public void InitializeQuiz(QuizQuestion[] questions)
{
    // Создаём глубокую копию чтобы не мутировать ScriptableObject
    currentQuestions = questions.Select(q => q.Clone()).ToArray();
    // ...
}
```

### 3.4 Система бонусов по уровням

#### Warmup Level
- Без бонусов — только базовые гемы за правильные ответы (по 5 за вопрос по дефолту)

#### Miqat Level (`MiqatLevelState.cs`)

| Бонус | Условие | Награда |
|-------|---------|---------|
| Speed bonus | Правильный ответ и `Time.time - _startTime < 180с` | +2 гема |
| Excellence bonus | `scorePercent >= 90%` | `CompletionBonusGems / 2` |

**Проблемы:**
1. Speed bonus проверяет **общее время от начала уровня**, а не время ответа на конкретный вопрос. Первые вопросы ВСЕГДА получают бонус, последние — НИКОГДА.
2. `_startTime` устанавливается в `Enter()`, но уровень начинается с теории. Время на чтение теории засчитывается в «быстроту» ответа на квиз.
3. `OnResume()` добавляет `Time.unscaledDeltaTime` (один кадр) вместо полной длительности паузы — таймер не корректируется правильно.

#### Tawaf Level (`TawafLevelState.cs`)

| Бонус | Условие | Награда |
|-------|---------|---------|
| Streak bonus | 3+ правильных подряд | `consecutiveCorrect * 2` гемов |
| Perfect circle | 7 правильных подряд | +20 гемов |
| Perfect Tawaf | 100% score | +50 гемов |

**Анализ streak бонуса (геометрически растущая награда):**

Для 7 подряд правильных ответов:
- Вопрос 1: 5 гемов (базово) → итого 5
- Вопрос 2: 5 → итого 10
- Вопрос 3: 5 + 6 (streak 3×2) → итого 21
- Вопрос 4: 5 + 8 → итого 34
- Вопрос 5: 5 + 10 → итого 49
- Вопрос 6: 5 + 12 → итого 66
- Вопрос 7: 5 + 14 + 20 (perfect circle) → итого 105

**105 гемов за 7 вопросов!** + 50 за perfect Tawaf = **155 гемов.** Это может быть слишком щедро в сравнении с другими уровнями, или сделано намеренно для мотивации.

**Рекомендация:** Либо сделать streak бонус фиксированным (например, +5 за каждый ответ после 3-го подряд), либо задокументировать экономику гемов.

---

## 4. Система прогресса — сохранение и загрузка

### 4.1 Архитектура персистенции (текущая)

```
                           ┌─────────────────────┐
                           │  BaseLevelState      │
                           │  .SaveProgress()     │
                           └──────┬──────┬────────┘
                                  │      │
                    Запись #1     │      │     Запись #2
                                  ▼      ▼
              ┌───────────────┐   ┌───────────────────┐
              │ProgressService│   │ProfileLoaderService│
              │               │   │                    │
              │ → UpdateProfile   │ → GetProfile()     │
              │   → Save()    │   │ → Set(...)         │
              └───────┬───────┘   │ → Save()           │
                      │           └────────┬───────────┘
                      ▼                    ▼
              ┌───────────────┐   ┌───────────────────┐
              │UserProfileSvc │   │ PlayerPrefsProvider│
              │               │   │ FileProvider       │
              │ → PlayerPrefs │   │ (BackendProvider)  │
              │ → File        │   └───────────────────┘
              └───────────────┘
```

**Проблема:** Два параллельных пути сохранения, потенциально с разными данными.

### 4.2 Проблемы при сохранении

#### Проблема 1: Кеш `ProfileLoaderService` vs `UserProfileService`

`UserProfileService` имеет свой `_profile` (строка 24), а `ProfileLoaderService` имеет свой `_cachedProfile` (строка 12). Это два разных объекта!

Сценарий:
1. `ProgressService.RecordLevelProgress()` → `UserProfileService.UpdateProfile()` → обновляет `_profile` объект A, сохраняет
2. `ProfileLoaderService.GetProfile()` → возвращает `_cachedProfile` объект B (может быть загружен ранее)
3. Данные в A и B **расходятся**

#### Проблема 2: `CalculateTotalProgress` — неверный знаменатель

```csharp
private float CalculateTotalProgress(UserProfile profile)
{
    var values = profile.LevelProgress.Values;
    return sum / values.Count;  // Делим на количество НАЧАТЫХ уровней
}
```

Если в игре 3 уровня (Warmup, Miqat, Tawaf), но игрок прошёл только 1:
- Ожидание: `TotalProgress = 100 / 3 = 33.3%`
- Факт: `TotalProgress = 100 / 1 = 100%` ← **неверно!**

**Рекомендация:**
```csharp
private const int TotalLevelCount = 3; // Или получать из конфига

private float CalculateTotalProgress(UserProfile profile)
{
    var values = profile.LevelProgress.Values;
    if (values == null || values.Count == 0) return 0f;
    float sum = 0f;
    foreach (float v in values) sum += v;
    return sum / TotalLevelCount;  // Делим на ОБЩЕЕ число уровней
}
```

#### Проблема 3: `ResetProgress()` не сбрасывает гемы

```csharp
public void ResetProgress()
{ 
    TotalProgress = 0f;
    CompletedLevelIds.Clear();
    LevelProgress.Keys.Clear();
    LevelProgress.Values.Clear();
    // Gems НЕ сбрасываются!
}
```

Если это сделано намеренно — нужен комментарий. Если нет — добавить `Gems = 0;`.

### 4.3 Проблемы при загрузке

#### Проблема 1: `StageCompletionService.LoadSavedResults()` — нет `CompletedAt`

```csharp
_levelResults[levelId] = new LevelResult
{
    LevelId = levelId,
    ScorePercent = progress,
    CompletedAt = DateTime.MinValue  // Нет реальной даты
};
```

`DateTime` не сериализуется через `JsonUtility`. Нужно хранить как `string` (ISO 8601) или `long` (ticks/unix timestamp).

#### Проблема 2: Приоритет загрузки в `UserProfileService`

```csharp
private UserProfile Load()
{
    // 1. Пробуем PlayerPrefs
    // 2. Пробуем File
    // 3. Создаём новый
}
```

Если PlayerPrefs повреждён (а это бывает на Android при обновлении), а файл содержит актуальные данные — PlayerPrefs будет возвращён как корректный (пустой), и файловые данные игнорируются.

**Рекомендация:** При загрузке сравнивать данные из обоих источников и брать более актуальные (например, по `TotalProgress` или количеству `CompletedLevelIds`).

#### Проблема 3: `ProfileLoaderService` — нет версионирования данных

При обновлении приложения структура `UserProfile` может измениться (новые поля, переименования). Без номера версии в JSON миграция данных невозможна.

**Рекомендация:**
```csharp
[Serializable]
public class UserProfile
{
    public int SchemaVersion = 1;  // ← Добавить
    // ...
}
```

### 4.4 Рекомендуемая архитектура персистенции

```
                     ┌──────────────┐
                     │ GameService   │
                     │ (фасад)      │
                     └──────┬───────┘
                            │
                     ┌──────▼───────┐
                     │ProfileManager│  ← Единственный source of truth
                     │              │
                     │ - _profile   │  ← Один кешированный профиль
                     │ - Save()     │
                     │ - Load()     │
                     └──────┬───────┘
                            │
              ┌─────────────┼─────────────┐
              ▼             ▼             ▼
        ┌──────────┐ ┌──────────┐ ┌──────────┐
        │PlayerPrefs│ │  File    │ │ Backend  │
        │ Provider  │ │ Provider │ │ Provider │
        └──────────┘ └──────────┘ └──────────┘
```

---

## 5. Стабильность и оптимизация

### 5.1 Производительность

| Проблема | Файл | Рекомендация |
|----------|------|--------------|
| `FindObjectOfType` каждый раз при входе в уровень | `BaseLevelState.cs:52-53` | Кешировать или регистрировать как сервис |
| `Debug.Log` в `Update()` | `TawafLevelState.cs:52-57` | Обернуть в `#if UNITY_EDITOR` или `[Conditional("UNITY_EDITOR")]` |
| `SerializableDictionary` — O(n) поиск через `IndexOf` | `UserProfile.cs:52` | Для 3 уровней не критично, но для масштабирования использовать `Dictionary` с кастомной сериализацией |
| `DebugLevelResults = _levelResults.Values.ToList()` создаёт аллокацию | `StageCompletionService.cs:72,133` | Обернуть в `#if UNITY_EDITOR` |
| `_providers.Sort()` при каждом `RegisterProvider()` | `ProfileLoaderService.cs:88` | Не критично при 3 провайдерах |

### 5.2 Потокобезопасность

- `UserProfile` — mutable объект, передаваемый между сервисами по ссылке. Любой сервис может изменить его без уведомления других.
- `ProfileLoaderService.SaveAsync()` использует `Task.WhenAll()` — параллельная запись в несколько провайдеров. Если один провайдер медленный, другие могут попытаться прочитать незаконченные данные.

**Рекомендация:** Использовать immutable profile или copy-on-write:
```csharp
public UserProfile GetProfileCopy()
{
    return JsonUtility.FromJson<UserProfile>(JsonUtility.ToJson(_cachedProfile));
}
```

### 5.3 Обработка ошибок

- `GameManager.Instance?.AddGems()` — null-propagation скрывает ошибки. Если `Instance` == null, гемы молча теряются.
- `QuizSystem.Advance()` не проверяет `_questions` на null (строка 84). Если вызвать до `Initialise()` — `NullReferenceException`.
- `QuizService.CompleteQuiz()` — нет защиты от деления на ноль.

### 5.4 Memory Leaks

- В `BaseLevelState.Exit()` подписки на события снимаются (`UnsubscribeQuizEvents()`). ✅
- Но если `Exit()` не вызывается (crash, force quit) — подписки остаются. Рекомендация: добавить `OnDestroy()` в `QuizSystem`:
```csharp
private void OnDestroy()
{
    OnQuestionReady = null;
    OnAnswerResult = null;
    OnQuizComplete = null;
}
```

### 5.5 WebGL-специфичные проблемы

Файл `index.html` и папка `TemplateData` говорят о WebGL билде. Для WebGL:
- `File.WriteAllText()` и `File.Exists()` используют `Application.persistentDataPath` — в WebGL это IndexedDB через Emscripten. Это работает, но асинхронно.
- `PlayerPrefs.Save()` в WebGL делает запись в IndexedDB — это асинхронная операция, а `Save()` синхронная. Данные могут не сохраниться при быстром закрытии вкладки.

**Рекомендация:** Для WebGL использовать `OnApplicationPause(true)` и `OnApplicationFocus(false)` для принудительного сохранения:
```csharp
private void OnApplicationPause(bool pause)
{
    if (pause) ForceSaveProfile();
}
```

---

## 6. Предложение по Python-бекенду

### 6.1 Архитектура

```
┌─────────────────┐     HTTPS/REST      ┌──────────────────────────┐
│  Unity Client   │◄───────────────────►│  Python Backend (FastAPI) │
│  (WebGL/Mobile) │                      │                          │
│                 │  JSON                │  ┌───────────────────┐   │
│ BackendProfile  │◄────────────────────►│  │ /api/v1/profiles  │   │
│ Provider        │                      │  │ /api/v1/progress  │   │
│                 │                      │  │ /api/v1/leaderboard│  │
└─────────────────┘                      │  └───────┬───────────┘   │
                                         │          │               │
                                         │  ┌───────▼───────────┐   │
                                         │  │   SQLite / Postgres│   │
                                         │  └───────────────────┘   │
                                         └──────────────────────────┘
```

### 6.2 Структура проекта Python

```
hajjflow-backend/
├── main.py                 # Точка входа FastAPI
├── requirements.txt
├── .env                    # Конфигурация
├── app/
│   ├── __init__.py
│   ├── config.py           # Настройки приложения
│   ├── database.py         # Подключение к БД
│   ├── models/
│   │   ├── __init__.py
│   │   ├── user.py         # Модель пользователя
│   │   ├── profile.py      # Модель профиля
│   │   └── progress.py     # Модель прогресса
│   ├── schemas/
│   │   ├── __init__.py
│   │   ├── profile.py      # Pydantic-схемы для API
│   │   └── progress.py
│   ├── routers/
│   │   ├── __init__.py
│   │   ├── auth.py         # Аутентификация
│   │   ├── profiles.py     # CRUD профиля
│   │   ├── progress.py     # Управление прогрессом
│   │   └── leaderboard.py  # Таблица лидеров
│   └── services/
│       ├── __init__.py
│       ├── auth_service.py
│       ├── profile_service.py
│       └── progress_service.py
└── tests/
    ├── test_profiles.py
    └── test_progress.py
```

### 6.3 Полный пример кода бекенда

#### `requirements.txt`

```
fastapi==0.115.0
uvicorn==0.30.0
sqlalchemy==2.0.30
pydantic==2.7.0
python-jose[cryptography]==3.3.0
passlib[bcrypt]==1.7.4
python-dotenv==1.0.1
alembic==1.13.1
```

#### `app/config.py`

```python
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    DATABASE_URL: str = "sqlite:///./hajjflow.db"
    SECRET_KEY: str = "change-me-in-production"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 60 * 24 * 7  # 7 дней
    
    class Config:
        env_file = ".env"


settings = Settings()
```

#### `app/database.py`

```python
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, DeclarativeBase

from app.config import settings

engine = create_engine(settings.DATABASE_URL, connect_args={"check_same_thread": False})
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)


class Base(DeclarativeBase):
    pass


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
```

#### `app/models/user.py`

```python
from sqlalchemy import Column, Integer, String, Float, DateTime, JSON
from sqlalchemy.sql import func

from app.database import Base


class User(Base):
    """Пользователь приложения HajjFlow."""
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    device_id = Column(String, unique=True, index=True, nullable=False)
    first_name = Column(String, default="Player")
    last_name = Column(String, default="")
    
    # Прогресс
    total_progress = Column(Float, default=0.0)
    gems = Column(Integer, default=0)
    
    # Завершённые уровни (JSON массив строк)
    completed_level_ids = Column(JSON, default=list)
    
    # Прогресс по уровням (JSON объект: {"Warmup": 85.5, "Miqat": 100.0})
    level_progress = Column(JSON, default=dict)
    
    # Метаданные
    schema_version = Column(Integer, default=1)
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())
```

#### `app/schemas/profile.py`

```python
from pydantic import BaseModel, Field
from typing import Optional


class LevelProgressEntry(BaseModel):
    """Прогресс по одному уровню."""
    level_id: str
    score_percent: float = Field(ge=0, le=100)


class ProfileResponse(BaseModel):
    """Ответ API — профиль пользователя."""
    device_id: str
    first_name: str = "Player"
    last_name: str = ""
    total_progress: float = Field(ge=0, le=100, default=0)
    gems: int = Field(ge=0, default=0)
    completed_level_ids: list[str] = []
    level_progress: dict[str, float] = {}
    schema_version: int = 1

    class Config:
        from_attributes = True


class ProfileUpdateRequest(BaseModel):
    """Запрос на обновление профиля (частичное обновление)."""
    first_name: Optional[str] = None
    last_name: Optional[str] = None
    gems: Optional[int] = Field(None, ge=0)


class ProgressSubmitRequest(BaseModel):
    """
    Запрос на сохранение результата прохождения уровня.
    Бекенд сам проверяет и пересчитывает.
    """
    level_id: str
    score_percent: float = Field(ge=0, le=100)
    correct_answers: int = Field(ge=0)
    total_questions: int = Field(ge=1)
    gems_earned: int = Field(ge=0)
    time_spent_seconds: float = Field(ge=0)


class ProgressSubmitResponse(BaseModel):
    """Ответ после сохранения прогресса."""
    accepted: bool
    new_total_progress: float
    new_gems: int
    level_completed: bool
    is_new_best: bool
    message: str = ""
```

#### `app/services/progress_service.py`

```python
"""
Сервис обработки прогресса — серверная валидация.

ВАЖНО: Вся критическая логика подсчёта должна быть на сервере,
чтобы предотвратить читерство. Клиент отправляет сырые данные,
сервер пересчитывает и верифицирует.
"""

from sqlalchemy.orm import Session
from app.models.user import User
from app.schemas.profile import ProgressSubmitRequest, ProgressSubmitResponse

# Конфигурация уровней (в production — из БД или конфига)
LEVEL_CONFIG = {
    "Warmup": {
        "pass_threshold": 60,
        "max_questions": 10,
        "base_gems_per_correct": 5,
        "completion_bonus": 20,
        "total_levels_in_game": 3,
    },
    "Miqat": {
        "pass_threshold": 60,
        "max_questions": 10,
        "base_gems_per_correct": 5,
        "completion_bonus": 20,
        "speed_bonus_per_answer": 2,
        "speed_threshold_seconds": 180,
        "excellence_threshold": 90,
        "total_levels_in_game": 3,
    },
    "Tawaf": {
        "pass_threshold": 60,
        "max_questions": 14,  # 2 круга по 7
        "base_gems_per_correct": 5,
        "completion_bonus": 20,
        "streak_threshold": 3,
        "streak_multiplier": 2,
        "perfect_circle_bonus": 20,
        "perfect_tawaf_bonus": 50,
        "total_levels_in_game": 3,
    },
}


def validate_and_save_progress(
    db: Session,
    user: User,
    request: ProgressSubmitRequest,
) -> ProgressSubmitResponse:
    """
    Серверная валидация и сохранение прогресса.
    
    Ключевые проверки:
    1. level_id существует в конфиге
    2. score_percent совпадает с correct/total
    3. gems_earned не превышает максимально возможные
    4. Не перезаписываем лучший результат худшим
    """
    config = LEVEL_CONFIG.get(request.level_id)
    if not config:
        return ProgressSubmitResponse(
            accepted=False,
            new_total_progress=user.total_progress,
            new_gems=user.gems,
            level_completed=False,
            is_new_best=False,
            message=f"Unknown level: {request.level_id}",
        )
    
    # ── Валидация 1: Пересчёт score ────────────────────────────────────
    expected_score = (request.correct_answers / request.total_questions) * 100
    if abs(expected_score - request.score_percent) > 0.5:
        return ProgressSubmitResponse(
            accepted=False,
            new_total_progress=user.total_progress,
            new_gems=user.gems,
            level_completed=False,
            is_new_best=False,
            message="Score verification failed",
        )
    
    # ── Валидация 2: Количество вопросов в пределах конфига ─────────────
    if request.total_questions > config["max_questions"]:
        return ProgressSubmitResponse(
            accepted=False,
            new_total_progress=user.total_progress,
            new_gems=user.gems,
            level_completed=False,
            is_new_best=False,
            message="Invalid question count",
        )
    
    # ── Валидация 3: Максимально возможные гемы ────────────────────────
    max_possible_gems = _calculate_max_gems(config, request)
    if request.gems_earned > max_possible_gems:
        return ProgressSubmitResponse(
            accepted=False,
            new_total_progress=user.total_progress,
            new_gems=user.gems,
            level_completed=False,
            is_new_best=False,
            message=f"Gem count exceeds maximum ({max_possible_gems})",
        )
    
    # ── Проверка: не перезаписываем лучший результат ───────────────────
    current_progress = user.level_progress or {}
    existing_score = current_progress.get(request.level_id, 0)
    is_new_best = request.score_percent > existing_score
    
    if not is_new_best:
        # Всё равно добавляем гемы, но не обновляем score
        user.gems += request.gems_earned
        db.commit()
        return ProgressSubmitResponse(
            accepted=True,
            new_total_progress=user.total_progress,
            new_gems=user.gems,
            level_completed=request.level_id in (user.completed_level_ids or []),
            is_new_best=False,
            message="Score not improved, gems still awarded",
        )
    
    # ── Сохранение ────────────────────────────────────────────────────
    # Обновляем прогресс уровня
    current_progress[request.level_id] = request.score_percent
    user.level_progress = current_progress
    
    # Проверяем прохождение
    level_completed = request.score_percent >= config["pass_threshold"]
    completed_ids = user.completed_level_ids or []
    if level_completed and request.level_id not in completed_ids:
        completed_ids.append(request.level_id)
        user.completed_level_ids = completed_ids
    
    # Пересчитываем общий прогресс (ПРАВИЛЬНЫЙ знаменатель!)
    total_levels = config.get("total_levels_in_game", 3)
    total_score = sum(current_progress.values())
    user.total_progress = min(total_score / total_levels, 100.0)
    
    # Добавляем гемы
    user.gems += request.gems_earned
    
    db.commit()
    db.refresh(user)
    
    return ProgressSubmitResponse(
        accepted=True,
        new_total_progress=user.total_progress,
        new_gems=user.gems,
        level_completed=level_completed,
        is_new_best=True,
        message="Progress saved successfully",
    )


def _calculate_max_gems(config: dict, request: ProgressSubmitRequest) -> int:
    """Рассчитывает максимально возможное количество гемов для античит-проверки."""
    base = request.correct_answers * config.get("base_gems_per_correct", 5)
    completion = config.get("completion_bonus", 0) if request.score_percent >= config["pass_threshold"] else 0
    
    # Бонусы Miqat
    speed = request.correct_answers * config.get("speed_bonus_per_answer", 0)
    excellence = config.get("completion_bonus", 0) // 2 if request.score_percent >= config.get("excellence_threshold", 999) else 0
    
    # Бонусы Tawaf (максимально возможные)
    streak_max = 0
    if "streak_threshold" in config:
        for i in range(config.get("streak_threshold", 3), request.correct_answers + 1):
            streak_max += i * config.get("streak_multiplier", 2)
    
    circles = request.correct_answers // 7
    circle_bonus = circles * config.get("perfect_circle_bonus", 0)
    perfect = config.get("perfect_tawaf_bonus", 0) if request.score_percent == 100 else 0
    
    return base + completion + speed + excellence + streak_max + circle_bonus + perfect
```

#### `app/routers/profiles.py`

```python
from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app.database import get_db
from app.models.user import User
from app.schemas.profile import (
    ProfileResponse,
    ProfileUpdateRequest,
    ProgressSubmitRequest,
    ProgressSubmitResponse,
)
from app.services.progress_service import validate_and_save_progress

router = APIRouter(prefix="/api/v1", tags=["profiles"])


@router.get("/profiles/{device_id}", response_model=ProfileResponse)
def get_profile(device_id: str, db: Session = Depends(get_db)):
    """
    Получить профиль по device_id.
    Если профиль не существует — создаём новый.
    """
    user = db.query(User).filter(User.device_id == device_id).first()
    
    if not user:
        user = User(device_id=device_id)
        db.add(user)
        db.commit()
        db.refresh(user)
    
    return ProfileResponse(
        device_id=user.device_id,
        first_name=user.first_name,
        last_name=user.last_name,
        total_progress=user.total_progress,
        gems=user.gems,
        completed_level_ids=user.completed_level_ids or [],
        level_progress=user.level_progress or {},
    )


@router.put("/profiles/{device_id}", response_model=ProfileResponse)
def update_profile(
    device_id: str,
    request: ProfileUpdateRequest,
    db: Session = Depends(get_db),
):
    """Обновить данные профиля (имя и т.д.)."""
    user = db.query(User).filter(User.device_id == device_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="Profile not found")
    
    if request.first_name is not None:
        user.first_name = request.first_name
    if request.last_name is not None:
        user.last_name = request.last_name
    
    db.commit()
    db.refresh(user)
    
    return ProfileResponse(
        device_id=user.device_id,
        first_name=user.first_name,
        last_name=user.last_name,
        total_progress=user.total_progress,
        gems=user.gems,
        completed_level_ids=user.completed_level_ids or [],
        level_progress=user.level_progress or {},
    )


@router.post(
    "/profiles/{device_id}/progress",
    response_model=ProgressSubmitResponse,
)
def submit_progress(
    device_id: str,
    request: ProgressSubmitRequest,
    db: Session = Depends(get_db),
):
    """
    Отправить результат прохождения уровня.
    
    Бекенд:
    1. Валидирует данные (score совпадает с correct/total)
    2. Проверяет gems на максимально возможные (античит)
    3. Не перезаписывает лучший результат худшим
    4. Пересчитывает TotalProgress с правильным знаменателем
    """
    user = db.query(User).filter(User.device_id == device_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="Profile not found")
    
    return validate_and_save_progress(db, user, request)


@router.delete("/profiles/{device_id}/progress")
def reset_progress(device_id: str, db: Session = Depends(get_db)):
    """Сбросить весь прогресс пользователя."""
    user = db.query(User).filter(User.device_id == device_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="Profile not found")
    
    user.total_progress = 0.0
    user.gems = 0
    user.completed_level_ids = []
    user.level_progress = {}
    
    db.commit()
    return {"message": "Progress reset successfully"}
```

#### `app/routers/leaderboard.py`

```python
from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session
from sqlalchemy import desc
from pydantic import BaseModel

from app.database import get_db
from app.models.user import User

router = APIRouter(prefix="/api/v1", tags=["leaderboard"])


class LeaderboardEntry(BaseModel):
    rank: int
    name: str
    total_progress: float
    gems: int
    levels_completed: int


@router.get("/leaderboard", response_model=list[LeaderboardEntry])
def get_leaderboard(
    limit: int = Query(default=50, le=100),
    sort_by: str = Query(default="progress", regex="^(progress|gems)$"),
    db: Session = Depends(get_db),
):
    """Таблица лидеров: по прогрессу или по гемам."""
    order_col = User.total_progress if sort_by == "progress" else User.gems
    
    users = (
        db.query(User)
        .order_by(desc(order_col))
        .limit(limit)
        .all()
    )
    
    return [
        LeaderboardEntry(
            rank=i + 1,
            name=f"{u.first_name} {u.last_name}".strip() or "Player",
            total_progress=u.total_progress,
            gems=u.gems,
            levels_completed=len(u.completed_level_ids or []),
        )
        for i, u in enumerate(users)
    ]
```

#### `main.py`

```python
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.database import engine, Base
from app.routers import profiles, leaderboard

# Создаём таблицы
Base.metadata.create_all(bind=engine)

app = FastAPI(
    title="HajjFlow Backend",
    description="Backend API для образовательного приложения HajjFlow",
    version="1.0.0",
)

# CORS — разрешаем запросы из WebGL билда
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # В production: указать конкретный домен
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(profiles.router)
app.include_router(leaderboard.router)


@app.get("/health")
def health_check():
    return {"status": "ok", "version": "1.0.0"}
```

### 6.4 Интеграция с Unity (BackendProfileProvider)

Пример реализации `BackendProfileProvider.LoadAsync()` с использованием `UnityWebRequest`:

```csharp
public async Task<UserProfile> LoadAsync()
{
    string url = $"{_apiBaseUrl}/api/v1/profiles/{_userId}";
    
    using var request = UnityWebRequest.Get(url);
    request.timeout = 10; // 10 секунд таймаут
    
    var operation = request.SendWebRequest();
    
    // Ждём завершения (Unity-compatible await)
    while (!operation.isDone)
        await Task.Yield();
    
    if (request.result != UnityWebRequest.Result.Success)
    {
        Debug.LogWarning($"[Backend] Load failed: {request.error}");
        return null;
    }
    
    try
    {
        // Десериализуем ответ API в Unity UserProfile
        string json = request.downloadHandler.text;
        var response = JsonUtility.FromJson<BackendProfileResponse>(json);
        return MapToUserProfile(response);
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Backend] Parse failed: {ex.Message}");
        return null;
    }
}

public async Task SaveProgressAsync(string levelId, float score, 
    int correct, int total, int gems, float timeSpent)
{
    string url = $"{_apiBaseUrl}/api/v1/profiles/{_userId}/progress";
    
    var body = new ProgressSubmitBody
    {
        level_id = levelId,
        score_percent = score,
        correct_answers = correct,
        total_questions = total,
        gems_earned = gems,
        time_spent_seconds = timeSpent
    };
    
    string json = JsonUtility.ToJson(body);
    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
    
    using var request = new UnityWebRequest(url, "POST");
    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");
    request.timeout = 10;
    
    var operation = request.SendWebRequest();
    while (!operation.isDone)
        await Task.Yield();
    
    if (request.result == UnityWebRequest.Result.Success)
    {
        Debug.Log("[Backend] Progress saved to server");
    }
    else
    {
        Debug.LogWarning($"[Backend] Save failed: {request.error}");
        // Помечаем для повторной отправки (offline queue)
    }
}
```

### 6.5 Стратегия синхронизации (Offline-First)

```
┌─────────┐          ┌──────────────┐          ┌─────────┐
│  Игрок  │──играет──│ Local Save   │──онлайн──│ Backend │
│         │          │ (PlayerPrefs │          │ (Python) │
│         │          │  + File)     │          │         │
└─────────┘          └──────┬───────┘          └────┬────┘
                            │                       │
                     При подключении:               │
                            │    ┌──────────┐       │
                            ├───►│  Merge   │◄──────┤
                            │    │  Logic   │       │
                            │    └────┬─────┘       │
                            │         │             │
                            │    «Берём лучший      │
                            │     результат по      │
                            │     каждому уровню»   │
                            │         │             │
                            ◄─────────┘─────────────►
```

Алгоритм мерджа:
```python
def merge_profiles(local: dict, remote: dict) -> dict:
    """Мердж локального и серверного профиля."""
    merged = {}
    
    # Для каждого уровня берём лучший score
    all_levels = set(local.get("level_progress", {}).keys()) | \
                 set(remote.get("level_progress", {}).keys())
    
    merged_progress = {}
    for level_id in all_levels:
        local_score = local.get("level_progress", {}).get(level_id, 0)
        remote_score = remote.get("level_progress", {}).get(level_id, 0)
        merged_progress[level_id] = max(local_score, remote_score)
    
    # Гемы — берём максимум (или сумму, зависит от дизайна)
    merged["gems"] = max(local.get("gems", 0), remote.get("gems", 0))
    merged["level_progress"] = merged_progress
    
    # Пересчитываем completed_level_ids
    merged["completed_level_ids"] = list(set(
        local.get("completed_level_ids", []) + 
        remote.get("completed_level_ids", [])
    ))
    
    return merged
```

---

## 7. Приоритетный план исправлений

### 🔴 P0 — Критические (делать первыми)

| # | Проблема | Файл | Суть |
|---|----------|------|------|
| 1 | Прогресс перезаписывается нулём | `BaseLevelState.cs` | Добавить проверку `_questionsAnswered == 0` и сравнение с существующим результатом |
| 2 | Уровень маркируется завершённым без проверки порога | `BaseLevelState.cs:179-182` | Добавить `_lastScorePercent >= _levelData.PassThreshold` |
| 3 | Двойная запись профиля | `BaseLevelState.SaveProgress()` | Убрать одну из двух записей |
| 4 | Нет защиты от деления на ноль | `QuizService.cs:141` | Добавить проверку `currentQuestions.Length > 0` |

### 🟡 P1 — Важные (следующий спринт)

| # | Проблема | Файл | Суть |
|---|----------|------|------|
| 5 | TotalProgress — неверный знаменатель | `ProgressService.cs:65` | Делить на общее число уровней, не на количество начатых |
| 6 | Мутация ScriptableObject при Shuffle | `QuizQuestion.cs` | Работать с копией вопросов |
| 7 | BackendProvider deadlock | `BackendProfileProvider.cs:30` | Убрать синхронный вызов async |
| 8 | Два кеша профиля (рассинхронизация) | `UserProfileService` + `ProfileLoaderService` | Унифицировать в один source of truth |
| 9 | DateTime не сериализуется через JsonUtility | `LevelResult` | Хранить как `long` (Unix timestamp) |

### 🟢 P2 — Оптимизации (backlog)

| # | Проблема | Файл | Суть |
|---|----------|------|------|
| 10 | `FindObjectOfType` в Enter() | `BaseLevelState.cs:52-53` | Зарегистрировать как сервис |
| 11 | `Debug.Log` в Update() | `TawafLevelState.cs:52` | Обернуть в `#if UNITY_EDITOR` |
| 12 | MiqatLevel speed bonus — неверная логика таймера | `MiqatLevelState.cs:73` | Перенести _startTime на начало квиза, не теории |
| 13 | MiqatLevel OnResume — неверная коррекция паузы | `MiqatLevelState.cs:64` | Считать pausedDuration и вычитать |
| 14 | Добавить schema_version в UserProfile | `UserProfile.cs` | Для миграций данных |
| 15 | WebGL: принудительное сохранение при смене фокуса | `GameManager.cs` | `OnApplicationPause` / `OnApplicationFocus` |
| 16 | Дублирование QuizSystem и QuizService | Оба файла | Объединить в одну систему |

---

> **Итого:** 4 критических бага, 5 важных проблем, 7 оптимизаций. Проект имеет хорошую базовую структуру, но нуждается в консолидации сервисов персистенции и исправлении логики сохранения прогресса.
