# Настройка системы свайп-карточек (Tinder-style)

## Обзор
Система позволяет создавать карточки с теорией, которые можно листать свайпами как в Tinder. Карточки анимируются с помощью DOTween с плавным движением, вращением и затуханием по альфе.

## Компоненты

### 1. TheoryCardBase (абстрактный класс)
Базовый класс для всех карточек с логикой свайпа.

**Основные возможности:**
- Детекция свайпов в 4 направлениях (лево, право, верх, низ)
- Визуальный фидбэк при перетаскивании
- Анимация движения и затухания с DOTween
- Поддержка нового Input System и старого Input

**События:**
- `SwipeDetected(SwipeDirection)` - срабатывает при любом свайпе
- `SwipeLeft` - свайп влево
- `SwipeRight` - свайп вправо
- `SwipeUp` - свайп вверх
- `SwipeDown` - свайп вниз

### 2. SimpleTheoryCard
Простая реализация карточки с дополнительными эффектами (звуки, партиклы).

### 3. TheoryCardManager
Менеджер стопки карточек, управляет навигацией и позиционированием.

---

## Настройка в сцене Unity

### Шаг 1: Создание префаба карточки

1. Создайте новый GameObject: `RightClick -> UI -> Canvas (если нет)`
2. Внутри Canvas создайте: `RightClick -> UI -> Panel` → назовите `TheoryCard`
3. Настройте RectTransform карточки:
   - Width: 600
   - Height: 800
   - Anchors: Center

4. Добавьте дочерние UI элементы:
   ```
   TheoryCard (Panel)
   ├── Background (Image) - фон карточки
   ├── Title (TextMeshPro - Text) - заголовок
   ├── Description (TextMeshPro - Text) - описание
   └── CardImage (Image) - основное изображение
   ```

5. Добавьте компоненты на `TheoryCard`:
   - `CanvasGroup` (Add Component -> Canvas Group)
   - `SimpleTheoryCard` скрипт (или наследник от TheoryCardBase)

6. В инспекторе `SimpleTheoryCard` назначьте:
   - **Data**: TheoryCardData (ScriptableObject)
   - **Title**: ссылка на Title TextMeshPro
   - **Description**: ссылка на Description TextMeshPro
   - **Image**: ссылка на CardImage
   - **Canvas Group**: ссылка на CanvasGroup

7. Настройте параметры свайпа:
   ```
   Swipe Settings:
   - Min Swipe Distance: 60 (минимальная дистанция для свайпа)
   - Max Swipe Time: 0.4 (максимальное время для свайпа)
   
   Animation Settings:
   - Swipe Animation Duration: 0.3 (длительность анимации)
   - Swipe Distance Multiplier: 1.5 (множитель дистанции)
   - Rotation Angle: 15 (угол поворота при свайпе)
   - Swipe Ease: OutQuad (тип анимации)
   ```

8. Сохраните как префаб: перетащите `TheoryCard` в папку Prefabs

### Шаг 2: Создание ScriptableObject данных

1. `RightClick -> Create -> ScriptableObjects -> Theory -> Theory Card Data`
2. Заполните данные:
   - Title: "Намерение (Ният)"
   - Description: "Ният - это искреннее намерение..."
   - Image: перетащите спрайт
3. Создайте несколько карточек с разным контентом

### Шаг 3: Настройка менеджера карточек

1. В сцене создайте пустой GameObject: `TheoryCardManager`
2. Добавьте скрипт `TheoryCardManager`
3. Создайте контейнер для карточек:
   - В Canvas создайте пустой GameObject: `CardContainer`
   - Настройте RectTransform:
     - Anchors: Stretch (все стороны)
     - Left/Right/Top/Bottom: 0

4. В инспекторе `TheoryCardManager` назначьте:
   - **Card Prefab**: префаб TheoryCard
   - **Card Container**: ссылка на CardContainer
   - **Card Data List**: массив ScriptableObject карточек (добавьте все созданные)
   
5. Настройте параметры стопки:
   ```
   Card Stack Settings:
   - Max Visible Cards: 3 (сколько карточек видно одновременно)
   - Card Offset: 10 (смещение по Y между карточками)
   - Card Scale Decrement: 0.05 (уменьшение масштаба для задних карточек)
   ```

### Шаг 4: Настройка Canvas

1. Выберите Canvas
2. Canvas Scaler настройки:
   ```
   UI Scale Mode: Scale With Screen Size
   Reference Resolution: 1080 x 1920 (для мобильных)
   Match: 0.5 (Width/Height)
   ```

---

## Использование в коде

### Подписка на события карточки:

```csharp
TheoryCardBase card = GetComponent<TheoryCardBase>();

// Общее событие свайпа
card.SwipeDetected += (direction) => {
    Debug.Log($"Swiped: {direction}");
};

// Отдельные события
card.SwipeLeft += () => Debug.Log("Swiped Left!");
card.SwipeRight += () => Debug.Log("Swiped Right!");
card.SwipeUp += () => Debug.Log("Swiped Up!");
card.SwipeDown += () => Debug.Log("Swiped Down!");
```

### Инициализация карточки:

```csharp
TheoryCardBase card = Instantiate(cardPrefab);
card.Initialize(theoryCardData);
```

### Управление менеджером:

```csharp
TheoryCardManager manager = GetComponent<TheoryCardManager>();

// Сбросить все карточки
manager.ResetCards();
```

---

## Настройка анимаций

### Изменение поведения свайпа:

В инспекторе карточки можно настроить:

1. **Swipe Animation Duration** - скорость анимации (0.2-0.5 рекомендуется)
2. **Swipe Distance Multiplier** - как далеко улетает карточка (1.0-2.0)
3. **Rotation Angle** - угол наклона при свайпе (10-20 градусов)
4. **Swipe Ease** - тип анимации:
   - `OutQuad` - плавное замедление
   - `OutCubic` - более резкое замедление
   - `Linear` - равномерное движение
   - `OutBack` - "отскок" в конце

### Визуальный фидбэк во время драга:

При перетаскивании карточка:
- Следует за пальцем/мышью (с коэффициентом 0.5)
- Поворачивается в направлении свайпа
- Затухает по альфе в зависимости от расстояния

---

## Расширение функционала

### Создание своей карточки:

```csharp
using UnityEngine;

namespace Core.Theory
{
    public class CustomTheoryCard : TheoryCardBase
    {
        [SerializeField] private ParticleSystem _swipeParticles;
        
        private void Start()
        {
            // Подписываемся на события
            SwipeRight += OnSwipeRight;
            SwipeLeft += OnSwipeLeft;
        }
        
        private void OnSwipeRight()
        {
            // Карточка "понравилась"
            Debug.Log("Liked!");
            if (_swipeParticles != null)
                _swipeParticles.Play();
        }
        
        private void OnSwipeLeft()
        {
            // Карточка "не понравилась"
            Debug.Log("Disliked!");
        }
    }
}
```

### Добавление звуков:

```csharp
[SerializeField] private AudioClip _swipeSound;
private AudioSource _audioSource;

private void Start()
{
    _audioSource = gameObject.AddComponent<AudioSource>();
    SwipeDetected += (dir) => _audioSource.PlayOneShot(_swipeSound);
}
```

---

## Примеры использования

### Сценарий 1: Обучающие карточки
- Свайп вправо → следующая карточка
- Свайп влево → предыдущая карточка
- Свайп вверх → отметить как "изучено"

### Сценарий 2: Квиз после карточек
```csharp
private void OnAllCardsCompleted()
{
    // Показать квиз
    QuizService.Instance.StartQuiz(quizData);
}
```

### Сценарий 3: Сохранение прогресса
```csharp
card.SwipeUp += () => {
    SaveProgress(card.Data.Id, completed: true);
};
```

---

## Troubleshooting

### Карточка не двигается:
- Проверьте, что у GameObject есть `RectTransform`
- Убедитесь, что `Canvas Group` назначен
- Проверьте, что Input System настроен правильно

### Анимация не работает:
- Убедитесь, что DOTween импортирован в проект
- Проверьте, что нет ошибок компиляции
- Попробуйте увеличить `Swipe Animation Duration`

### Свайп не распознается:
- Увеличьте `Max Swipe Time` (0.5-1.0)
- Уменьшите `Min Swipe Distance` (30-50)
- Убедитесь, что UI элементы не блокируют Raycast

---

## Оптимизация

1. **Object Pooling**: Используйте пул объектов вместо Instantiate/Destroy
2. **Lazy Loading**: Загружайте карточки по мере необходимости
3. **Texture Atlasing**: Объедините спрайты карточек в атлас
4. **Reduce Overdraw**: Отключайте невидимые карточки в стеке

---

## Полезные ссылки

- DOTween Documentation: http://dotween.demigiant.com/documentation.php
- Unity Input System: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html

