# 🎬 Инструкция по Установке Архитектуры Уровней в Сцене

## ✅ Пошаговая Инструкция

### Шаг 1: Подготовка GameManager (делается один раз)

1. **Проверь наличие GameManager в сцене**
   - Если его ещё нет, создай новый GameObject: `Ctrl+Shift+N`
   - Назови его `GameManager`
   - Добавь компонент `GameManager` (скрипт находится в `Assets/Scripts/Core/`)

2. **Убедись, что GameManager помечен как DontDestroyOnLoad**
   - Это происходит автоматически в `Awake()`
   - GameManager будет сохраняться при переходе между сценами

---

### Шаг 2: Создание Контейнера Уровня в Сцене

1. **Создай новый GameObject для контейнера уровня**
   ```
   Ctrl+Shift+N → "LevelContainer"
   ```

2. **Добавь один из контроллеров уровня**
   - Добавь компонент `WarmupLevelController` (для 1-го уровня)
   - **Или** `MiqatLevelController` (для 2-го уровня)
   - **Или** `TawafLevelController` (для 3-го уровня)

3. **Создай и загрузи LevelData**
   - Assets → Create → Manasik → Level Data (новый)
   - Или выбери существующий LevelData
   - Перетащи LevelData в инспектор контроллера

4. **Загрузи вопросы из JSON**
   - Выдели LevelData в инспекторе
   - Правый клик → "Load Questions from JSON"
   - Выбери файл JSON с вопросами (например: `Assets/Data/QuizData/1lvlquiz.json`)
   - Вопросы загружаются в массив `Questions[]`

---

### Шаг 3: Создание UI для Блоков Теории

1. **Создай Canvas для блока теории (если его ещё нет)**
   ```
   Right Click in Hierarchy → UI → Canvas
   ```

2. **Создай объект для первого блока теории**
   ```
   Right Click in Canvas → Create Empty
   Назови: "Stage0_Content"
   ```

3. **Добавь компонент StageGameplayController**
   - Выдели `Stage0_Content`
   - Добавь компонент `StageGameplayController`

4. **Создай кнопку "Next" для завершения блока**
   ```
   Right Click in Stage0_Content → UI → Button
   Назови: "NextButton"
   ```

5. **Привяжи кнопку к методу**
   - Выдели `NextButton`
   - В инспекторе найди компонент Button
   - В `On Click ()` добавь обработчик:
     - Drag `Stage0_Content` в поле
     - Функция: `StageGameplayController.OnNextButtonClicked()`

6. **Повтори шаги 2-5 для остальных блоков** (Stage1, Stage2, etc.)

---

### Шаг 4: Создание UI для Квиза

1. **Создай объект для UI квиза**
   ```
   Right Click in Canvas → Create Empty
   Назови: "QuizPanel"
   Изначально отключи: Uncheck Active
   ```

2. **Добавь компонент QuizUIController**
   - Выдели `QuizPanel`
   - Добавь компонент `QuizUIController`

3. **Создай элементы UI квиза**
   - **Текст вопроса:**
     ```
     Right Click in QuizPanel → UI → Text
     Назови: "QuestionText"
     ```
   - **Кнопки ответов (4 штуки):**
     ```
     Right Click in QuizPanel → UI → Button
     Назови: "AnswerButton_0", "AnswerButton_1", "AnswerButton_2", "AnswerButton_3"
     ```
   - **Прогресс:**
     ```
     Right Click in QuizPanel → UI → Text
     Назови: "ProgressText"
     ```
   - **Индикаторы результата:**
     ```
     Right Click in QuizPanel → UI → Image
     Назови: "CorrectIndicator" (зелёный цвет, изначально отключен)
     Right Click in QuizPanel → UI → Image
     Назови: "IncorrectIndicator" (красный цвет, изначально отключен)
     ```

4. **Привяжи элементы к QuizUIController**
   - Выдели `QuizPanel`
   - В инспекторе найди `QuizUIController`
   - Drag элементы в соответствующие поля:
     - `questionText` ← `QuestionText`
     - `answerButtons[0]` ← `AnswerButton_0`
     - `answerButtons[1]` ← `AnswerButton_1`
     - `answerButtons[2]` ← `AnswerButton_2`
     - `answerButtons[3]` ← `AnswerButton_3`
     - `progressText` ← `ProgressText`
     - `correctIndicator` ← `CorrectIndicator`
     - `incorrectIndicator` ← `IncorrectIndicator`

---

### Шаг 5: Запуск Уровня

1. **Создай скрипт для запуска из UI (например, кнопка "Play")**
   ```csharp
   using UnityEngine;
   using HajjFlow.Core.LevelsLogic;
   
   public class PlayButtonHandler : MonoBehaviour
   {
       public void OnPlayButtonClicked()
       {
           WarmupLevelController controller = FindFirstObjectByType<WarmupLevelController>();
           if (controller != null)
               controller.StartLevel();
       }
   }
   ```

2. **Привяжи этот скрипт к кнопке "Play"**
   - Создай GameObject с этим скриптом
   - Выдели кнопку "Play"
   - В `On Click ()` добавь обработчик:
     - Функция: `PlayButtonHandler.OnPlayButtonClicked()`

---

## 📊 Структура Иерархии Сцены

```
LevelContainer (GameObject)
├── WarmupLevelController (Script)
└── LevelData (Reference)

Canvas
├── Stage0_Content (GameObject)
│   ├── StageGameplayController (Script)
│   └── NextButton (UI Button)
│
├── Stage1_Content (GameObject)
│   ├── StageGameplayController (Script)
│   └── NextButton (UI Button)
│
├── Stage2_Content (GameObject)
│   ├── StageGameplayController (Script)
│   └── NextButton (UI Button)
│
└── QuizPanel (GameObject)
    ├── QuizUIController (Script)
    ├── QuestionText (UI Text)
    ├── AnswerButton_0 (UI Button)
    ├── AnswerButton_1 (UI Button)
    ├── AnswerButton_2 (UI Button)
    ├── AnswerButton_3 (UI Button)
    ├── ProgressText (UI Text)
    ├── CorrectIndicator (UI Image)
    └── IncorrectIndicator (UI Image)
```

---

## 🧪 Тестирование

### Тест 1: Запуск первого блока теории
1. Нажми "Play"
2. Должен загрузиться Stage0_Content
3. В консоли: `[WarmupLevelController] Starting stage 0`

### Тест 2: Завершение блока
1. Нажми кнопку "Next"
2. Должен загрузиться Stage1_Content
3. В консоли: `[WarmupLevelController] Stage 0 verified and completed`

### Тест 3: Запуск квиза
1. Пройди все блоки (Stage0, Stage1, Stage2)
2. Должен загрузиться QuizPanel
3. В консоли: `[QuizService] Quiz initialized with X questions`

### Тест 4: Ответ на вопрос
1. Выбери ответ
2. Должно срабатывать событие (✓ Correct или ✗ Incorrect)
3. Должен загрузиться следующий вопрос

### Тест 5: Завершение квиза
1. Ответь на все вопросы
2. В консоли: `[QuizService] Quiz completed! Correct: X/Y`
3. Должен показаться экран результатов

---

## 🔗 Быстрые Ссылки

- **Сервисы:** `Assets/Scripts/Services/`
- **Контроллеры:** `Assets/Scripts/Core/LevelsLogic/`
- **UI компоненты:** `Assets/Scripts/UI/`
- **LevelData:** Assets → Create → Manasik → Level Data
- **JSON файлы:** `Assets/Data/QuizData/`

---

## 💡 Советы

✅ **Используй Prefabs** для блоков теории - сделает переиспользование проще
✅ **Тестируй сервисы в консоли** - добавь Debug.Log в критические точки
✅ **Создай разные LevelData для каждого уровня** - будет удобнее управлять
✅ **Используй события** вместо напрямую вызова методов - слабая связанность
✅ **Сохраняй прогресс** в ProfileService при завершении уровня


