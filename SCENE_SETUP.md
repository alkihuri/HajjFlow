# Руководство по Настройке Сцены Gameplay

## 🎯 Цель
Настроить сцену Gameplay для работы с машиной состояний уровней.

## 📋 Шаги Настройки

### 1. Создание GameController

1. Откройте сцену `Gameplay` в Unity Editor
2. Создайте пустой GameObject:
   - **Имя:** `GameController`
   - **Transform:** Position (0, 0, 0)
   
3. Добавьте компоненты:
   - Кликните **Add Component** → `LevelStateMachine`
   - Кликните **Add Component** → `GameplaySceneInitializer`

---

### 2. Настройка Systems

Убедитесь что в сцене есть:

#### GameObject: `QuizSystem`
- Компонент: `QuizSystem`

#### GameObject: `RewardSystem`
- Компонент: `RewardSystem`

---

### 3. Настройка UI

#### GameObject: `Canvas` (или UI root)

**Компонент: GameplayUI**

Назначьте в Inspector:

**[Top Bar]**
- `Level Name Text` → TextMeshProUGUI с именем уровня
- `Progress Text` → TextMeshProUGUI для прогресса (%)
- `Gems Text` → TextMeshProUGUI для отображения гемов

**[Quiz Panel]**
- `Question Text` → TextMeshProUGUI для вопроса
- `Option Buttons` → Массив из 4 Button (A, B, C, D)
- `Feedback Text` → TextMeshProUGUI для обратной связи
- `Next Button` → Button для перехода к следующему вопросу

**[Navigation]**
- `Back Button` → Button для возврата
- `Restart Button` → Button для перезапуска

**[Systems]**
- `Quiz System Ref` → Перетащите QuizSystem GameObject
- `Reward System Ref` → Перетащите RewardSystem GameObject

---

### 4. Создание LevelData ScriptableObjects

#### Warmup Level
1. В Project: Assets → Create → Manasik → Level Data
2. Назовите: `WarmupLevel`
3. Настройте:
   - **Level Id:** `"warmup"` или `"level_1_warmup"`
   - **Level Name:** `"Подготовка к Хаджу"`
   - **Description:** Краткое описание
   - **Thumbnail:** Иконка уровня
   - **Questions:** Добавьте вопросы
   - **Pass Threshold:** 60
   - **Completion Bonus Gems:** 20

#### Miqat Level
1. Assets → Create → Manasik → Level Data
2. Назовите: `MiqatLevel`
3. Настройте:
   - **Level Id:** `"miqat"` или `"level_2_miqat"`
   - **Level Name:** `"Miqat - Место Ихрама"`
   - **Pass Threshold:** 70
   - **Completion Bonus Gems:** 30

#### Tawaf Level
1. Assets → Create → Manasik → Level Data
2. Назовите: `TawafLevel`
3. Настройте:
   - **Level Id:** `"tawaf"` или `"level_3_tawaf"`
   - **Level Name:** `"Tawaf - Обход Каабы"`
   - **Pass Threshold:** 80
   - **Completion Bonus Gems:** 50

**Важно:** LevelId должен содержать ключевые слова: "warmup", "miqat" или "tawaf" для правильного определения StateId.

---

### 5. Настройка LevelSelection Scene

1. Откройте сцену `LevelSelection`
2. Найдите GameObject с компонентом `LevelSelectionUI`
3. В Inspector:
   - **Levels:** Назначьте массив из 3 LevelData (Warmup, Miqat, Tawaf)

---

## ✅ Проверка Настройки

### Тест 1: Запуск Warmup
1. Play Mode → Main Menu
2. Выберите уровень 1 (Warmup)
3. В Console должно появиться:
   ```
   [LevelManager] Starting level: Подготовка к Хаджу with state: warmup
   [GameplaySceneInitializer] Scene initialized
   [LevelStateMachine] State changed to: warmup
   [WarmupLevelState] Entering state: Подготовка к Хаджу
   ```

### Тест 2: Прохождение и Переход
1. Ответьте на вопросы
2. При правильном ответе должны начисляться гемы
3. После завершения викторины должен открыться экран Results

### Тест 3: Pause/Resume
Добавьте кнопку паузы:
```csharp
public void OnPauseClicked()
{
    if (LevelManager.StateMachine.IsPaused)
        LevelManager.StateMachine.Resume();
    else
        LevelManager.StateMachine.Pause();
}
```

---

## 🐛 Возможные Проблемы

### Проблема: "QuizSystem not found"
**Решение:** Убедитесь что в сцене Gameplay есть GameObject с компонентом QuizSystem

### Проблема: "No active level set"
**Решение:** Убедитесь что вызываете `LevelManager.StartLevel()` перед загрузкой сцены Gameplay

### Проблема: State не переключается
**Решение:** Проверьте что LevelId содержит правильные ключевые слова ("warmup", "miqat", "tawaf")

---

## 📐 Пример Иерархии Сцены Gameplay

```
Gameplay
├── GameController
│   └── Components:
│       - LevelStateMachine
│       - GameplaySceneInitializer
│
├── Systems
│   ├── QuizSystem (QuizSystem)
│   └── RewardSystem (RewardSystem)
│
├── Canvas
│   └── GameplayUI (GameplayUI)
│       ├── TopBar
│       │   ├── LevelNameText
│       │   ├── ProgressText
│       │   └── GemsText
│       │
│       ├── QuizPanel
│       │   ├── QuestionText
│       │   ├── OptionA
│       │   ├── OptionB
│       │   ├── OptionC
│       │   ├── OptionD
│       │   ├── FeedbackText
│       │   └── NextButton
│       │
│       └── Navigation
│           ├── BackButton
│           └── RestartButton
│
├── Environment
│   └── (3D модели, фоны и т.д.)
│
└── Lighting
```

---

## 🎨 Рекомендации по Визуальному Оформлению

### Warmup (Разминка)
- Светлая спокойная цветовая схема
- Простые иконки
- Минимум визуальных эффектов

### Miqat (Ихрам)
- Белые/светлые тона (цвет ихрама)
- Карта с местами Miqat
- Таймер в верхней части UI

### Tawaf (Обход)
- Изображение или 3D модель Каабы
- Визуализация кругов (7 кругов)
- Индикатор текущего круга
- Анимация движения вокруг Каабы
- Эффекты для streak bonus

---

*Последнее обновление: 21 февраля 2026*

