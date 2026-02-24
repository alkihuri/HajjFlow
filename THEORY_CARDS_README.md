# 🎴 Theory Cards System - Tinder-Style Swipeable Cards

## 📋 Обзор

Полнофункциональная система свайпаемых карточек в стиле Tinder для Unity. Карточки плавно анимируются, поворачиваются и затухают при свайпе с использованием DOTween. Система поддерживает как старый Unity Input, так и новый Input System.

---

## ✨ Возможности

- ✅ **Плавные анимации**: движение, поворот и затухание с DOTween
- ✅ **Визуальный фидбэк**: карточка следует за пальцем в реальном времени
- ✅ **4 направления свайпа**: влево, вправо, вверх, вниз
- ✅ **Стопка карточек**: автоматическое позиционирование и масштабирование
- ✅ **Настраиваемая анимация**: скорость, дистанция, угол поворота, тип easing
- ✅ **События**: C# events для интеграции с другими системами
- ✅ **Input System**: поддержка обеих систем ввода Unity
- ✅ **Расширяемость**: абстрактный базовый класс для кастомизации

---

## 📦 Компоненты

### 1. TheoryCardBase (Abstract)
Абстрактный базовый класс для всех карточек.

**Поля:**
- `TheoryCardData Data` - данные карточки
- Настройки свайпа (дистанция, время)
- Настройки анимации (длительность, угол, easing)
- UI компоненты (Title, Description, Image, CanvasGroup)

**События:**
```csharp
event Action<SwipeDirection> SwipeDetected;
event Action SwipeLeft;
event Action SwipeRight;
event Action SwipeUp;
event Action SwipeDown;
```

**Методы:**
```csharp
void Initialize(TheoryCardData data);
```

### 2. SimpleTheoryCard
Готовая реализация карточки с поддержкой звуков.

### 3. TheoryCardManager
Менеджер стопки карточек.

**Поля:**
- `TheoryCardBase CardPrefab` - префаб карточки
- `Transform CardContainer` - контейнер для карточек
- `List<TheoryCardData> CardDataList` - список данных карточек
- Настройки стопки (количество видимых, смещение, масштаб)

**Методы:**
```csharp
void ResetCards(); // Сбросить все карточки
```

### 4. TheoryToQuizIntegration
Пример интеграции карточек с системой квизов.

---

## 🎯 Использование

### Минимальная настройка:

```csharp
// Получить карточку
TheoryCardBase card = GetComponent<TheoryCardBase>();

// Инициализировать данными
card.Initialize(cardData);

// Подписаться на события
card.SwipeRight += () => Debug.Log("Swiped Right!");
card.SwipeLeft += () => Debug.Log("Swiped Left!");
```

### Создание кастомной карточки:

```csharp
using UnityEngine;

namespace Core.Theory
{
    public class MyCustomCard : TheoryCardBase
    {
        [SerializeField] private ParticleSystem particles;
        
        private void Start()
        {
            // Подписываемся на события
            SwipeRight += OnLiked;
            SwipeLeft += OnDisliked;
        }
        
        private void OnLiked()
        {
            Debug.Log("Liked!");
            particles?.Play();
        }
        
        private void OnDisliked()
        {
            Debug.Log("Disliked!");
        }
    }
}
```

### Интеграция с менеджером:

```csharp
TheoryCardManager manager = GetComponent<TheoryCardManager>();

// Сбросить карточки
manager.ResetCards();

// Получить доступ к текущей карточке через события
// события срабатывают автоматически при свайпе
```

---

## 🎨 Параметры анимации

### Swipe Settings:
| Параметр | Описание | Рекомендуемое значение |
|----------|----------|------------------------|
| Min Swipe Distance | Минимальная дистанция для свайпа (px) | 60 |
| Max Swipe Time | Максимальное время для свайпа (сек) | 0.4 |

### Animation Settings:
| Параметр | Описание | Рекомендуемое значение |
|----------|----------|------------------------|
| Swipe Animation Duration | Длительность анимации (сек) | 0.3 |
| Swipe Distance Multiplier | Множитель дистанции полета | 1.5 |
| Rotation Angle | Угол поворота карточки (градусы) | 15 |
| Swipe Ease | Тип анимации DOTween | OutQuad |

### Card Stack Settings (Manager):
| Параметр | Описание | Рекомендуемое значение |
|----------|----------|------------------------|
| Max Visible Cards | Количество видимых карточек | 3 |
| Card Offset | Смещение по Y между карточками | 10 |
| Card Scale Decrement | Уменьшение масштаба задних карточек | 0.05 |

---

## 🎮 События

### Доступные события:

```csharp
// Общее событие с направлением
card.SwipeDetected += (direction) => {
    Debug.Log($"Swiped: {direction}");
    
    switch(direction) {
        case SwipeDirection.Left: // ... break;
        case SwipeDirection.Right: // ... break;
        case SwipeDirection.Up: // ... break;
        case SwipeDirection.Down: // ... break;
    }
};

// Отдельные события по направлениям
card.SwipeLeft += () => Debug.Log("Left");
card.SwipeRight += () => Debug.Log("Right");
card.SwipeUp += () => Debug.Log("Up");
card.SwipeDown += () => Debug.Log("Down");
```

### Примеры использования:

**Сохранение прогресса:**
```csharp
card.SwipeUp += () => {
    PlayerPrefs.SetInt($"Card_{card.Data.Id}_Completed", 1);
    PlayerPrefs.Save();
};
```

**Звуки:**
```csharp
[SerializeField] private AudioClip swipeSound;
card.SwipeDetected += (dir) => {
    AudioSource.PlayClipAtPoint(swipeSound, Vector3.zero);
};
```

**Партиклы:**
```csharp
[SerializeField] private ParticleSystem likeParticles;
card.SwipeRight += () => likeParticles.Play();
```

**Интеграция с квизом:**
```csharp
int completedCards = 0;
card.SwipeRight += () => {
    completedCards++;
    if (completedCards >= 5) {
        quizService.StartQuiz(quizData);
    }
};
```

---

## 📱 Input Support

Система автоматически определяет активный Input System:

### Новый Input System (по умолчанию):
```csharp
#if ENABLE_INPUT_SYSTEM
    // Использует UnityEngine.InputSystem
    Touchscreen.current
    Mouse.current
#endif
```

### Старый Input:
```csharp
#else
    // Использует UnityEngine.Input
    Input.touchCount
    Input.GetMouseButton
#endif
```

---

## 🛠 Требования

- **Unity**: 2021.3+
- **DOTween**: Установлен в проекте
- **TextMeshPro**: Для UI текста
- **Input System**: Опционально (fallback на старый Input)

---

## 📂 Структура файлов

```
Assets/Scripts/Core/Theory/
├── TheoryCardBase.cs            # Базовый абстрактный класс
├── SimpleTheoryCard.cs          # Простая реализация
├── TheoryCardManager.cs         # Менеджер стопки карточек
└── TheoryToQuizIntegration.cs   # Пример интеграции с квизом

Assets/Scripts/Core/Theory/
└── TheoryCardData.cs            # ScriptableObject для данных

Documentation/
├── THEORY_CARDS_SETUP.md        # Полное руководство
├── THEORY_CARDS_QUICK_GUIDE.md  # Быстрый старт
└── THEORY_CARDS_README.md       # Этот файл
```

---

## 🚀 Быстрый старт

### 1. Создать префаб карточки
```
Canvas
└── TheoryCard (Panel + SimpleTheoryCard + CanvasGroup)
    ├── Background (Image)
    ├── Title (TextMeshPro)
    ├── Description (TextMeshPro)
    └── CardImage (Image)
```

### 2. Создать ScriptableObject
```
RightClick → Create → ScriptableObjects → Theory → Theory Card Data
```

### 3. Настроить менеджер
```
TheoryCardManager:
  - Card Prefab: префаб TheoryCard
  - Card Container: пустой GameObject
  - Card Data List: массив ScriptableObject
```

### 4. Запустить Play Mode! 🎉

Подробнее: `THEORY_CARDS_QUICK_GUIDE.md`

---

## 🎓 Примеры

### Пример 1: Базовое использование
```csharp
public class CardController : MonoBehaviour
{
    [SerializeField] private TheoryCardBase card;
    
    void Start()
    {
        card.SwipeRight += OnCardLiked;
        card.SwipeLeft += OnCardDisliked;
    }
    
    void OnCardLiked()
    {
        Debug.Log("User liked this card!");
    }
    
    void OnCardDisliked()
    {
        Debug.Log("User disliked this card!");
    }
}
```

### Пример 2: Интеграция с квизом
```csharp
public class LevelFlow : MonoBehaviour
{
    [SerializeField] private TheoryCardManager cardManager;
    [SerializeField] private QuizService quizService;
    
    private int cardsCompleted = 0;
    
    void Start()
    {
        var cards = cardManager.GetComponentsInChildren<TheoryCardBase>();
        foreach (var card in cards)
        {
            card.SwipeDetected += OnCardCompleted;
        }
    }
    
    void OnCardCompleted(SwipeDirection dir)
    {
        cardsCompleted++;
        
        if (cardsCompleted >= 5)
        {
            StartQuiz();
        }
    }
    
    void StartQuiz()
    {
        quizService.StartQuiz(quizData);
        quizService.OnQuizCompleted += OnQuizCompleted;
    }
    
    void OnQuizCompleted(int total, int correct)
    {
        float score = (float)correct / total;
        if (score >= 0.7f)
        {
            LoadNextLevel();
        }
    }
}
```

### Пример 3: Кастомная карточка с эффектами
```csharp
public class EnhancedTheoryCard : TheoryCardBase
{
    [SerializeField] private ParticleSystem likeParticles;
    [SerializeField] private ParticleSystem dislikeParticles;
    [SerializeField] private AudioClip swipeSound;
    [SerializeField] private Animator animator;
    
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        
        SwipeRight += OnSwipeRight;
        SwipeLeft += OnSwipeLeft;
        SwipeDetected += OnAnySwipe;
    }
    
    void OnSwipeRight()
    {
        likeParticles?.Play();
        animator?.SetTrigger("Like");
    }
    
    void OnSwipeLeft()
    {
        dislikeParticles?.Play();
        animator?.SetTrigger("Dislike");
    }
    
    void OnAnySwipe(SwipeDirection dir)
    {
        if (swipeSound != null)
            audioSource.PlayOneShot(swipeSound);
    }
}
```

---

## 🐛 Troubleshooting

### Карточка не двигается
- Проверьте наличие `RectTransform` компонента
- Убедитесь что `CanvasGroup` назначен
- Проверьте что Input System правильно настроен

### Анимация рывками
- Убедитесь что DOTween установлен
- Проверьте FPS: `Application.targetFrameRate = 60`
- Уменьшите количество одновременных анимаций

### Свайп не срабатывает
- Увеличьте `Max Swipe Time` (0.5-1.0)
- Уменьшите `Min Swipe Distance` (30-50)
- Проверьте что другие UI элементы не блокируют Raycast

### События не срабатывают
- Проверьте что вы подписались на события в `Start()` или `OnEnable()`
- Убедитесь что не произошла отписка в `OnDisable()`
- Проверьте логи на наличие ошибок

---

## 🔧 Оптимизация

### Performance Tips:
1. **Object Pooling**: Переиспользуйте карточки вместо Destroy/Instantiate
2. **Lazy Loading**: Загружайте изображения асинхронно
3. **Texture Atlasing**: Объедините спрайты в атлас
4. **Reduce Overdraw**: Отключайте невидимые задние карточки
5. **Batch Sprites**: Используйте Sprite Atlas для батчинга

### Memory Optimization:
```csharp
// Пример Object Pool
public class CardPool : MonoBehaviour
{
    private Queue<TheoryCardBase> pool = new Queue<TheoryCardBase>();
    
    public TheoryCardBase GetCard()
    {
        if (pool.Count > 0)
        {
            var card = pool.Dequeue();
            card.gameObject.SetActive(true);
            return card;
        }
        return Instantiate(cardPrefab);
    }
    
    public void ReturnCard(TheoryCardBase card)
    {
        card.gameObject.SetActive(false);
        pool.Enqueue(card);
    }
}
```

---

## 📝 TODO / Roadmap

- [ ] Object Pooling система
- [ ] Асинхронная загрузка изображений
- [ ] Поддержка жестов (pinch to zoom, double tap)
- [ ] Аналитика (tracking swipe directions)
- [ ] Accessibility поддержка (TalkBack, VoiceOver)
- [ ] Undo функция (вернуть последнюю карточку)
- [ ] Анимированные переходы между стеками

---

## 📞 Поддержка

Если у вас возникли вопросы или проблемы:

1. Проверьте документацию: `THEORY_CARDS_SETUP.md`
2. Посмотрите примеры в `TheoryToQuizIntegration.cs`
3. Проверьте Troubleshooting секцию выше

---

## 📄 Лицензия

Этот код является частью проекта HajjFlow.

---

## 🙏 Благодарности

- **DOTween** by Demigiant - для плавных анимаций
- **Unity Input System** - для современного ввода
- **TextMeshPro** - для красивого текста

---

**Версия:** 1.0.0  
**Последнее обновление:** 2024  
**Автор:** HajjFlow Team

