# 📚 Индекс документации: Система свайп-карточек

## 🎯 С чего начать?

### Для быстрого старта:
1. **`THEORY_CARDS_QUICK_GUIDE.md`** ⚡ - Настройка за 5 минут

### Для детального понимания:
2. **`THEORY_CARDS_README.md`** 📖 - Полная документация системы
3. **`THEORY_CARDS_SETUP.md`** 🔧 - Подробное руководство по настройке
4. **`THEORY_CARDS_SCENE_SETUP.md`** 🎬 - Визуальная инструкция настройки сцены

---

## 📂 Структура файлов проекта

### 🎨 Скрипты системы карточек

```
Assets/Scripts/Core/Theory/
├── TheoryCardBase.cs              # Базовый абстрактный класс карточки
├── SimpleTheoryCard.cs            # Простая реализация карточки
├── TheoryCardManager.cs           # Менеджер стопки карточек
├── TheoryToQuizIntegration.cs     # Интеграция с системой квизов
└── SwipeDebugger.cs               # Дебаг-утилита для свайпов
```

### 📄 Документация

```
/
├── THEORY_CARDS_README.md         # Полная документация (MAIN)
├── THEORY_CARDS_QUICK_GUIDE.md    # Быстрый старт (START HERE)
├── THEORY_CARDS_SETUP.md          # Детальная настройка
├── THEORY_CARDS_SCENE_SETUP.md    # Настройка сцены Unity
└── THEORY_CARDS_INDEX.md          # Этот файл (навигация)
```

---

## 🗺 Карта документации

### Уровень 1: Быстрый старт (5 минут)
```
THEORY_CARDS_QUICK_GUIDE.md
├── Создание префаба карточки
├── Создание ScriptableObject
├── Настройка менеджера
└── Запуск и тест
```

### Уровень 2: Полное понимание (20 минут)
```
THEORY_CARDS_README.md
├── Обзор системы
├── Компоненты и их роли
├── API и события
├── Примеры использования
├── Оптимизация
└── Troubleshooting
```

### Уровень 3: Детальная настройка (30 минут)
```
THEORY_CARDS_SETUP.md
├── Подробная настройка каждого компонента
├── Настройка UI элементов
├── Интеграция с квизом
├── Расширение функционала
└── Примеры кастомизации
```

### Уровень 4: Визуальная настройка сцены (15 минут)
```
THEORY_CARDS_SCENE_SETUP.md
├── Структура Hierarchy
├── Настройка Canvas
├── Префаб карточки (детально)
├── Настройка менеджеров
├── UI элементы
└── Чек-лист настройки
```

---

## 🎓 Что читать для разных задач?

### Я хочу быстро запустить систему:
→ **`THEORY_CARDS_QUICK_GUIDE.md`**

### Я хочу понять как работает система:
→ **`THEORY_CARDS_README.md`** (секция "Компоненты")

### Я хочу настроить карточки в Unity:
→ **`THEORY_CARDS_SCENE_SETUP.md`**

### Я хочу кастомизировать поведение карточек:
→ **`THEORY_CARDS_SETUP.md`** (секция "Расширение функционала")

### Я хочу интегрировать с квизом:
→ **`THEORY_CARDS_README.md`** (Пример 2)  
→ Посмотреть код: `TheoryToQuizIntegration.cs`

### У меня проблемы/ошибки:
→ **`THEORY_CARDS_README.md`** (секция "Troubleshooting")

### Я хочу оптимизировать производительность:
→ **`THEORY_CARDS_README.md`** (секция "Оптимизация")

### Я хочу добавить дебаг свайпов:
→ Использовать: `SwipeDebugger.cs`  
→ Инструкция в комментариях скрипта

---

## 🔑 Ключевые концепции

### 1. Абстрактный класс TheoryCardBase
**Файл:** `Assets/Scripts/Core/Theory/TheoryCardBase.cs`  
**Документация:** `THEORY_CARDS_README.md` → "Компоненты" → "TheoryCardBase"

Базовый класс для всех карточек. Содержит:
- Логику детекции свайпов
- Анимацию с DOTween
- События для интеграции
- Визуальный фидбэк

### 2. Менеджер стопки TheoryCardManager
**Файл:** `Assets/Scripts/Core/Theory/TheoryCardManager.cs`  
**Документация:** `THEORY_CARDS_README.md` → "Компоненты" → "TheoryCardManager"

Управляет стопкой карточек:
- Позиционирование карточек
- Автоматическая загрузка следующих
- События завершения

### 3. ScriptableObject данные TheoryCardData
**Создание:** `RightClick → Create → ScriptableObjects → Theory → Theory Card Data`  
**Документация:** `THEORY_CARDS_SETUP.md` → "Создание ScriptableObject данных"

Хранит данные карточки:
- Title (заголовок)
- Description (описание)
- Image (изображение)
- Метаданные (ID, Order, Category, Tags)

### 4. События системы
**Документация:** `THEORY_CARDS_README.md` → "События"

Доступные события:
```csharp
SwipeDetected(SwipeDirection)  // Общее событие
SwipeLeft                      // Свайп влево
SwipeRight                     // Свайп вправо
SwipeUp                        // Свайп вверх
SwipeDown                      // Свайп вниз
```

---

## 📊 Сценарии использования

### Сценарий 1: Обучающие карточки
**Описание:** Пользователь листает карточки с теорией  
**Документация:** `THEORY_CARDS_QUICK_GUIDE.md`

### Сценарий 2: Карточки → Квиз
**Описание:** После изучения 5 карточек открывается квиз  
**Документация:** `THEORY_CARDS_README.md` → Пример 2  
**Код:** `TheoryToQuizIntegration.cs`

### Сценарий 3: Кастомная карточка с эффектами
**Описание:** Карточка с партиклами и звуками  
**Документация:** `THEORY_CARDS_README.md` → Пример 3

### Сценарий 4: Дебаг свайпов
**Описание:** Визуализация траектории свайпа  
**Код:** `SwipeDebugger.cs`

---

## 🎨 Примеры кода

### Базовое использование:
```csharp
TheoryCardBase card = GetComponent<TheoryCardBase>();
card.SwipeRight += () => Debug.Log("Right!");
```
**Документация:** `THEORY_CARDS_README.md` → "Использование"

### Создание кастомной карточки:
```csharp
public class MyCard : TheoryCardBase {
    // Ваш код
}
```
**Документация:** `THEORY_CARDS_README.md` → "Использование" → "Создание кастомной карточки"

### Интеграция с квизом:
```csharp
card.SwipeRight += () => {
    completedCards++;
    if (completedCards >= 5) StartQuiz();
};
```
**Документация:** `THEORY_CARDS_README.md` → Пример 2  
**Полный код:** `TheoryToQuizIntegration.cs`

---

## ⚙️ Настройка параметров

### Параметры свайпа:
**Где настроить:** Inspector → SimpleTheoryCard → Swipe Settings  
**Документация:** `THEORY_CARDS_README.md` → "Параметры анимации"

```
Min Swipe Distance: 60 px
Max Swipe Time: 0.4 sec
```

### Параметры анимации:
**Где настроить:** Inspector → SimpleTheoryCard → Animation Settings  
**Документация:** `THEORY_CARDS_README.md` → "Параметры анимации"

```
Swipe Animation Duration: 0.3 sec
Swipe Distance Multiplier: 1.5
Rotation Angle: 15°
Swipe Ease: OutQuad
```

### Параметры стопки:
**Где настроить:** Inspector → TheoryCardManager → Card Stack Settings  
**Документация:** `THEORY_CARDS_SCENE_SETUP.md` → "Настройка TheoryCardManager"

```
Max Visible Cards: 3
Card Offset: 10
Card Scale Decrement: 0.05
```

---

## 🐛 Решение проблем

### Карточка не двигается
→ **`THEORY_CARDS_README.md`** → "Troubleshooting" → "Карточка не двигается"

### Анимация рывками
→ **`THEORY_CARDS_README.md`** → "Troubleshooting" → "Анимация рывками"

### Свайп не срабатывает
→ **`THEORY_CARDS_README.md`** → "Troubleshooting" → "Свайп не срабатывает"

### События не срабатывают
→ **`THEORY_CARDS_README.md`** → "Troubleshooting" → "События не срабатывают"

---

## 🚀 Следующие шаги

После настройки базовой системы:

1. **Добавить звуки:**  
   → `THEORY_CARDS_SETUP.md` → "Добавление звуков"

2. **Добавить партиклы:**  
   → `THEORY_CARDS_README.md` → Пример 3

3. **Интегрировать с квизом:**  
   → Использовать `TheoryToQuizIntegration.cs`

4. **Оптимизировать:**  
   → `THEORY_CARDS_README.md` → "Оптимизация"

5. **Добавить аналитику:**  
   → Подписаться на события и отправлять данные

---

## 📞 Справка

### Основная документация:
- **README:** `THEORY_CARDS_README.md`
- **Быстрый старт:** `THEORY_CARDS_QUICK_GUIDE.md`

### Техническая поддержка:
1. Проверьте секцию "Troubleshooting" в README
2. Откройте Console в Unity (Ctrl+Shift+C)
3. Проверьте все ссылки в Inspector

### Дополнительные ресурсы:
- DOTween: http://dotween.demigiant.com/documentation.php
- Unity Input System: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/

---

## ✅ Чек-лист завершения

- [ ] Прочитан `THEORY_CARDS_QUICK_GUIDE.md`
- [ ] Создан префаб карточки
- [ ] Созданы ScriptableObject данные
- [ ] Настроен TheoryCardManager
- [ ] Протестировано в Play Mode
- [ ] Свайпы работают корректно
- [ ] Прочитана полная документация `THEORY_CARDS_README.md`
- [ ] Интегрировано с системой квизов (опционально)
- [ ] Добавлены звуки/эффекты (опционально)

---

**Версия:** 1.0.0  
**Последнее обновление:** 2024  
**Проект:** HajjFlow

---

## 🎉 Готово!

Теперь у вас есть полная система свайп-карточек в стиле Tinder!

Начните с: **`THEORY_CARDS_QUICK_GUIDE.md`** ⚡

