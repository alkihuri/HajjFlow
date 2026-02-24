# Краткое руководство: Настройка свайп-карточек в сцене

## 🎯 Что получим
Карточки с теорией, которые можно листать свайпом влево/вправо как в Tinder с анимацией и затуханием.

## ⚡ Быстрая настройка (5 минут)

### 1️⃣ Создание префаба карточки

В **Hierarchy**:
```
Canvas (если нет - создать)
└── TheoryCard (UI Panel)
    ├── Background (Image) - фон
    ├── Title (TextMeshPro) - заголовок
    ├── Description (TextMeshPro) - текст
    └── CardImage (Image) - картинка
```

На `TheoryCard`:
- Добавить компонент: **CanvasGroup**
- Добавить компонент: **SimpleTheoryCard**
- RectTransform: Width=600, Height=800

В Inspector `SimpleTheoryCard`:
- Назначить Title, Description, Image, Canvas Group
- Настроить параметры:
  ```
  Min Swipe Distance: 60
  Max Swipe Time: 0.4
  Swipe Animation Duration: 0.3
  Rotation Angle: 15
  ```

Сохранить как **Prefab** в папку Prefabs.

---

### 2️⃣ Создание данных карточек

**RightClick → Create → ScriptableObjects → Theory → Theory Card Data**

Создать несколько карточек с разным контентом:
- Title: "Намерение"
- Description: "Ният - это искреннее намерение..."
- Image: назначить спрайт

---

### 3️⃣ Настройка менеджера

В **Hierarchy**:
```
Canvas
├── TheoryCardManager (Empty GameObject)
└── CardContainer (Empty GameObject)
```

На `TheoryCardManager`:
- Добавить компонент: **TheoryCardManager**
- Card Prefab: префаб TheoryCard
- Card Container: ссылка на CardContainer
- Card Data List: добавить все созданные ScriptableObject

Параметры:
```
Max Visible Cards: 3
Card Offset: 10
Card Scale Decrement: 0.05
```

---

### 4️⃣ Запустить и тестировать

**Play Mode** → Свайпайте мышкой карточки влево/вправо! 🎉

---

## 🎨 Настройка анимации

### Параметры свайпа (в карточке):
- **Swipe Animation Duration**: 0.3 = быстро, 0.5 = медленно
- **Swipe Distance Multiplier**: насколько далеко улетает (1.0-2.0)
- **Rotation Angle**: угол наклона при свайпе (10-20)
- **Swipe Ease**: тип анимации (OutQuad, OutCubic, Linear, OutBack)

### Визуальный фидбэк:
При перетаскивании карточка:
- ✅ Следует за пальцем
- ✅ Поворачивается
- ✅ Затухает по альфе

---

## 🎮 Управление

**Мышь/Touch**:
- Зажать и потянуть влево/вправо
- Отпустить для свайпа
- Если мало протянули - вернется обратно

**События в коде**:
```csharp
card.SwipeLeft += () => Debug.Log("Left!");
card.SwipeRight += () => Debug.Log("Right!");
card.SwipeUp += () => Debug.Log("Up!");
card.SwipeDown += () => Debug.Log("Down!");
```

---

## 🔧 Частые проблемы

**Карточка не двигается:**
- Проверь RectTransform на карточке
- Проверь Canvas Group
- Убедись, что Input System настроен

**Свайп не срабатывает:**
- Увеличь Max Swipe Time (до 0.6)
- Уменьши Min Swipe Distance (до 40)

**Анимация рывками:**
- Убедись, что DOTween установлен
- Проверь Application.targetFrameRate

---

## 📝 Чек-лист

- [ ] Префаб карточки создан с UI элементами
- [ ] SimpleTheoryCard компонент добавлен
- [ ] CanvasGroup назначен
- [ ] ScriptableObject данные созданы
- [ ] TheoryCardManager настроен
- [ ] CardContainer создан
- [ ] Префаб и данные назначены в менеджер
- [ ] Протестировано в Play Mode

---

## 🚀 Что дальше?

1. Добавить звуки свайпа
2. Добавить партиклы при свайпе
3. Интегрировать с системой квизов
4. Сохранять прогресс изученных карточек
5. Добавить статистику (сколько карточек изучено)

Подробная документация: `THEORY_CARDS_SETUP.md`

