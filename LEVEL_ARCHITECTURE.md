# 🎮 Архитектура Уровней и Сервисов

## Обзор

Архитектура управления уровнями в HajjFlow состоит из следующих компонентов:

### 🏗️ Основные Компоненты

```
┌─────────────────────────────────────────────────────────┐
│              GameManager (Singleton)                    │
├─────────────────────────────────────────────────────────┤
│ ✓ StageCompletionService                                │
│ ✓ QuizService                                           │
│ ✓ ProgressService                                       │
│ ✓ UserProfileService                                    │
└─────────────────────────────────────────────────────────┘
         ↑
         │ (находит сервисы)
         │
┌─────────────────────────────────────────────────────────┐
│  Контроллеры Уровней (Точки входа)                      │
├─────────────────────────────────────────────────────────┤
│ • WarmupLevelController (3 блока теории)                │
│ • MiqatLevelController (2 блока теории)                 │
│ • TawafLevelController (2 блока теории)                 │
└─────────────────────────────────────────────────────────┘
         │
         ├─→ Блоки теории (Gameified Content)
         │
         └─→ Квиз (из LevelData.Questions)
```

---

## 📋 Подробное Описание

### 1. StageCompletionService
**Назначение:** Отслеживание и верификация прохождения блоков теории

**События:**
- `OnStageCompleted(levelId, stageIndex)` - блок успешно завершён
- `OnStageCompletionFailed(reason)` - ошибка при завершении блока

**Основной метод:**
```csharp
public bool CompleteStage(string levelId, int stageIndex)
```

**Поддерживаемые уровни:**
- `"Warmup"` - 3 блока теории (индексы 0, 1, 2)
- `"Miqat"` - 2 блока теории (индексы 0, 1)
- `"Tawaf"` - 2 блока теории (индексы 0, 1)

---

### 2. QuizService
**Назначение:** Управление вопросами, отслеживание ответов и прогресса

**События:**
- `OnQuestionDisplayed(question)` - отобразить новый вопрос
- `OnAnswerCorrect(gemsReward)` - правильный ответ
- `OnAnswerIncorrect(correctIndex)` - неправильный ответ
- `OnQuizCompleted(totalQuestions, correctAnswers)` - квиз завершён

**Основные методы:**
```csharp
// Инициализировать квиз
public void InitializeQuiz(QuizQuestion[] questions)

// Проверить ответ и перейти к следующему вопросу
public bool SubmitAnswer(int selectedAnswerIndex)

// Получить текущий вопрос
public QuizQuestion GetCurrentQuestion()

// Получить статистику
public int GetCurrentQuestionIndex()
public int GetTotalQuestions()
public int GetCorrectAnswerCount()

// Сбросить квиз
public void ResetQuiz()
```

---

### 3. Контроллеры Уровней (WarmupLevelController, MiqatLevelController, TawafLevelController)

**Назначение:** Точка входа для управления уровнем

**Основной метод:**
```csharp
public void StartLevel()
```

**Процесс:**
1. Инициализирует текущий блок теории (index = 0)
2. Подписывается на события сервисов
3. При завершении блока → вызывает `stageCompletionService.CompleteStage()`
4. Обрабатывает событие `OnStageCompleted` → переходит к следующему блоку
5. После всех блоков → инициализирует квиз через `quizService.InitializeQuiz()`
6. При завершении квиза → проверяет результат и начисляет награды

**Общий поток:**
```
StartLevel()
    ↓
[Блок 0] → OnStageGameplayCompleted() → CompleteStage() → OnStageCompleted()
    ↓
[Блок 1] → OnStageGameplayCompleted() → CompleteStage() → OnStageCompleted()
    ↓
[Блок 2] → OnStageGameplayCompleted() → CompleteStage() → OnStageCompleted()
    ↓
StartQuiz() → InitializeQuiz()
    ↓
[Вопрос 1] → SubmitAnswer() → [Вопрос 2] → ... → OnQuizCompleted()
    ↓
Проверка результата (%)
```

---

## 🔧 Как Использовать

### Сценарий 1: Запуск Уровня

```csharp
// Где-то в UI или LevelSelection скрипте
WarmupLevelController warmupController = GetComponent<WarmupLevelController>();
warmupController.StartLevel();
```

### Сценарий 2: Завершение Блока Теории (из мини-игры)

```csharp
// В скрипте мини-игры/теории когда она завершена
public void OnGameplayComplete()
{
    // Вызываем метод контроллера
    WarmupLevelController controller = FindFirstObjectByType<WarmupLevelController>();
    controller.OnStageGameplayCompleted();
}
```

### Сценарий 3: Отправка Ответа в Квизе (из UI)

```csharp
// В скрипте UI кнопки ответа
public void OnAnswerButtonClicked(int answerIndex)
{
    QuizService quizService = GameManager.Instance.quizService;
    bool isCorrect = quizService.SubmitAnswer(answerIndex);
    
    // UI обновляется на основе событий OnAnswerCorrect/OnAnswerIncorrect
}
```

---

## 📊 Как Работает Сервис Проверки

### Верификация Блоков Теории

Каждый блок проходит верификацию в зависимости от типа уровня:

```csharp
// StageCompletionService.VerifyStageCompletion()
private bool VerifyStageCompletion(string levelId, int stageIndex)
{
    return levelId switch
    {
        "Warmup" => VerifyWarmupStage(stageIndex),      // 0-2
        "Miqat" => VerifyMiqatStage(stageIndex),        // 0-1
        "Tawaf" => VerifyTawafStage(stageIndex),        // 0-1
        _ => false
    };
}
```

**Можно расширить** для добавления дополнительных проверок:
- Проверка времени прохождения
- Проверка качества выполнения
- Проверка на жульничество
- Сохранение статистики

---

## 🎯 Установка в Сцене

### 1. Создай GameObject "GameManager" (если ещё нет)
   - Добавь компонент `GameManager`
   - Убедись, что это DontDestroyOnLoad

### 2. Создай GameObject "LevelContainer" в сцене уровня
   - Добавь один из контроллеров:
     - `WarmupLevelController` для первого уровня
     - `MiqatLevelController` для второго уровня
     - `TawafLevelController` для третьего уровня
   - Добавь `LevelData` в инспектор контроллера

### 3. В UI сцены (например, кнопка "Start Level")
   ```csharp
   public void StartLevel()
   {
       WarmupLevelController controller = FindFirstObjectByType<WarmupLevelController>();
       controller.StartLevel();
   }
   ```

---

## 📝 Примеры Событий

### Подписка на События

```csharp
// В UI контроллере квиза
private void OnEnable()
{
    QuizService quizService = GameManager.Instance.quizService;
    quizService.OnQuestionDisplayed += HandleQuestionDisplayed;
    quizService.OnAnswerCorrect += HandleAnswerCorrect;
    quizService.OnAnswerIncorrect += HandleAnswerIncorrect;
}

private void OnDisable()
{
    QuizService quizService = GameManager.Instance.quizService;
    quizService.OnQuestionDisplayed -= HandleQuestionDisplayed;
    quizService.OnAnswerCorrect -= HandleAnswerCorrect;
    quizService.OnAnswerIncorrect -= HandleAnswerIncorrect;
}

private void HandleQuestionDisplayed(QuizQuestion question)
{
    // Отобразить вопрос и варианты ответов
    Debug.Log(question.QuestionText);
}

private void HandleAnswerCorrect(int gems)
{
    // Показать анимацию успеха
    Debug.Log($"+{gems} gems!");
}

private void HandleAnswerIncorrect(int correctIndex)
{
    // Показать правильный ответ
    Debug.Log($"Correct answer was: {correctIndex}");
}
```

---

## 🚀 Расширение Архитектуры

### Добавить новый уровень

1. **Создай контроллер:**
   ```csharp
   public class NewLevelController : MonoBehaviour
   {
       private const string LEVEL_ID = "NewLevel";
       // ... остальной код (скопируй из WarmupLevelController)
   }
   ```

2. **Добавь верификацию в StageCompletionService:**
   ```csharp
   private bool VerifyStageCompletion(string levelId, int stageIndex)
   {
       return levelId switch
       {
           // ...
           "NewLevel" => VerifyNewLevelStage(stageIndex),  // Новая строка
           _ => false
       };
   }
   
   private bool VerifyNewLevelStage(int stageIndex)
   {
       return stageIndex >= 0 && stageIndex < 2;  // Например, 2 блока
   }
   ```

3. **Создай LevelData** в ScriptableObjects и загрузи вопросы из JSON

---

## 📌 Заметки

- ✅ Сервисы регистрируются в GameManager при инициализации
- ✅ Контроллеры находят сервисы автоматически через FindFirstObjectByType
- ✅ События позволяют слабую связанность между компонентами
- ✅ Каждый уровень может иметь разное количество блоков теории
- ✅ LevelData содержит все вопросы для уровня
- ✅ Награды (геммы) начисляются автоматически за правильные ответы


