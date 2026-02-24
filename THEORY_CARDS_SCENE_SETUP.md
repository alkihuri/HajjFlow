# 🎬 Визуальная инструкция: Настройка сцены с карточками

## 📐 Структура Hierarchy

```
Scene
├── Canvas (Canvas + Canvas Scaler + Graphic Raycaster)
│   ├── Background (Image) - фон сцены
│   │
│   ├── CardContainer (Empty GameObject)
│   │   └── [Карточки создаются динамически]
│   │
│   ├── UI_Elements
│   │   ├── ProgressBar (Slider) - прогресс
│   │   ├── CardCounter (TextMeshPro) - "5/10 карточек"
│   │   └── QuizButton (Button) - кнопка "Начать квиз"
│   │
│   └── Panels
│       ├── TheoryPanel (CanvasGroup) - панель с карточками
│       └── QuizPanel (CanvasGroup) - панель квиза
│
├── Managers (Empty GameObject)
│   ├── TheoryCardManager (TheoryCardManager script)
│   ├── QuizManager (QuizService script)
│   └── IntegrationManager (TheoryToQuizIntegration script)
│
├── EventSystem (EventSystem + Standalone Input Module)
│
└── Audio
    └── AudioSource (для звуков)
```

---

## 🎨 Настройка Canvas

### Canvas Component:
```
Render Mode: Screen Space - Overlay
Pixel Perfect: ✓ (включено)
```

### Canvas Scaler:
```
UI Scale Mode: Scale With Screen Size
Reference Resolution: 1080 x 1920
Screen Match Mode: Match Width Or Height
Match: 0.5
```

### Graphic Raycaster:
```
Ignore Reversed Graphics: ✓
Blocking Objects: None
Blocking Mask: Everything
```

---

## 🃏 Префаб карточки (TheoryCard)

### Иерархия префаба:
```
TheoryCard (Panel)
├── Shadow (Image) - тень под карточкой
├── CardBackground (Image) - фон карточки
├── ContentContainer (Vertical Layout Group)
│   ├── Header (Horizontal Layout Group)
│   │   ├── Icon (Image) - иконка
│   │   └── Title (TextMeshPro) - заголовок
│   │
│   ├── MainImage (Image) - основное изображение
│   │
│   ├── Description (TextMeshPro) - текст
│   │
│   └── Footer (Horizontal Layout Group)
│       ├── TagsContainer (Horizontal Layout Group)
│       │   └── Tag (TextMeshPro) - теги
│       └── PageNumber (TextMeshPro) - "1/10"
│
└── SwipeIndicators (Canvas Group)
    ├── LeftIndicator (Image + TextMeshPro) - "←"
    └── RightIndicator (Image + TextMeshPro) - "→"
```

### RectTransform карточки:
```
Width: 600
Height: 800
Anchors: Center (0.5, 0.5)
Pivot: (0.5, 0.5)
Position: (0, 0, 0)
Scale: (1, 1, 1)
```

### Компоненты карточки:
```
1. Canvas Group
   - Alpha: 1
   - Interactable: ✓
   - Block Raycasts: ✓
   
2. SimpleTheoryCard (script)
   [Swipe Settings]
   - Min Swipe Distance: 60
   - Max Swipe Time: 0.4
   
   [Animation Settings]
   - Swipe Animation Duration: 0.3
   - Swipe Distance Multiplier: 1.5
   - Rotation Angle: 15
   - Swipe Ease: OutQuad
   
   [Base UI]
   - Title: → Title (TextMeshPro)
   - Description: → Description (TextMeshPro)
   - Image: → MainImage (Image)
   - Canvas Group: → Canvas Group

3. Image (для фона)
   - Source Image: Card_Background_Sprite
   - Image Type: Sliced
   - Fill Center: ✓
```

---

## 🎯 Настройка TheoryCardManager

### GameObject: Managers/TheoryCardManager

### TheoryCardManager Component:
```
[References]
- Card Prefab: → TheoryCard (префаб)
- Card Container: → Canvas/CardContainer
- Card Data List: [массив ScriptableObject]
  ├── Element 0: CardData_Intention
  ├── Element 1: CardData_Purity
  ├── Element 2: CardData_Wudu
  └── ... (добавить все карточки)

[Card Stack Settings]
- Max Visible Cards: 3
- Card Offset: 10
- Card Scale Decrement: 0.05
```

---

## 🔗 Настройка TheoryToQuizIntegration

### GameObject: Managers/IntegrationManager

### TheoryToQuizIntegration Component:
```
[References]
- Card Manager: → TheoryCardManager
- Quiz Service: → QuizManager

[Quiz Data]
- Quiz Data: → QuizData_Theory (ScriptableObject)
- Required Cards For Quiz: 5

[UI]
- Quiz Button: → Canvas/UI_Elements/QuizButton
- Cards Panel: → Canvas/Panels/TheoryPanel
- Quiz Panel: → Canvas/Panels/QuizPanel
```

---

## 📦 ScriptableObject данные

### Создание TheoryCardData:

**Путь:** `Assets/Data/Theory/Cards/`

**Создание:**
```
RightClick → Create → ScriptableObjects → Theory → Theory Card Data
```

**Настройка:**
```
Name: CardData_Intention

[Theory Card Data]
- Title: "Намерение (Ният)"
- Description: "Ният - это искреннее намерение, с которым мусульманин начинает любое богоугодное дело..."
- Image: → Intention_Sprite
- Id: "intention_001"
- Order: 1
- Category: "Основы"
- Tags: ["обязательное", "намерение", "ният"]
```

Создать 10-15 карточек с разным контентом.

---

## 🎨 UI элементы стилизации

### CardBackground (Image):
```
Color: #FFFFFF
Material: None
Raycast Target: ✓

Shadow Component:
- Effect Color: #00000080 (черный 50% прозрачности)
- Effect Distance: (5, -5)
- Use Graphic Alpha: ✓
```

### Title (TextMeshPro):
```
Font: Roboto-Bold
Font Size: 48
Color: #2C3E50
Alignment: Center
Auto Size: ✓
Min: 24, Max: 48
Wrapping: Enabled
Overflow: Ellipsis
```

### Description (TextMeshPro):
```
Font: Roboto-Regular
Font Size: 32
Color: #34495E
Alignment: Left
Line Spacing: 1.2
Wrapping: Enabled
Overflow: Truncate
```

---

## 🎮 Кнопка квиза

### QuizButton (Button):

**RectTransform:**
```
Width: 400
Height: 100
Anchors: Bottom Center
Position Y: 100
```

**Button Component:**
```
Interactable: ✓ (изначально false)
Transition: Color Tint
- Normal: #3498DB
- Highlighted: #2980B9
- Pressed: #1F618D
- Selected: #3498DB
- Disabled: #95A5A6
```

**Button Text (TextMeshPro):**
```
Text: "Начать квиз"
Font Size: 36
Color: #FFFFFF
Alignment: Center
```

**On Click Event:**
```
IntegrationManager.StartQuiz()
```

---

## 📊 ProgressBar (Slider)

**RectTransform:**
```
Width: 600
Height: 40
Anchors: Top Center
Position Y: -50
```

**Slider Component:**
```
Min Value: 0
Max Value: 1
Value: 0
Whole Numbers: ✗
```

**Visual:**
```
Background: серый (#CCCCCC)
Fill: зелёный (#2ECC71)
Handle: скрыть (Handle Rect = None)
```

---

## 🎬 Настройка анимаций (опционально)

### Animator для карточки:

**States:**
```
Idle → Entry State
Appear → при появлении
Disappear → при свайпе
Like → при свайпе вправо
Dislike → при свайпе влево
```

**Transitions:**
```
Idle → Appear (on Start)
Idle → Like (trigger: "SwipeRight")
Idle → Dislike (trigger: "SwipeLeft")
Like/Dislike → Disappear (automatic)
```

---

## 📱 Тестирование в редакторе

### Настройка Game View:

```
Aspect: 9:16 (Portrait)
или
Aspect: 16:9 (Landscape)

Resolution: 1080x1920 (Portrait)
Scale: 1x
```

### Режим симуляции Input:

**Window → Analysis → Input Debugger**

Или используйте:
- **Мышь** для симуляции тача
- **Unity Remote** для тестирования на устройстве

---

## ✅ Чек-лист настройки

### Префаб карточки:
- [ ] RectTransform настроен (600x800)
- [ ] Canvas Group добавлен
- [ ] SimpleTheoryCard скрипт добавлен
- [ ] UI элементы созданы (Title, Description, Image)
- [ ] UI элементы связаны в Inspector
- [ ] Префаб сохранён

### ScriptableObject данные:
- [ ] Создано 5+ TheoryCardData
- [ ] Заполнены Title, Description, Image
- [ ] Назначены уникальные ID
- [ ] Установлен Order для сортировки

### Менеджеры:
- [ ] TheoryCardManager добавлен на сцену
- [ ] Card Prefab назначен
- [ ] CardContainer создан и назначен
- [ ] Card Data List заполнен ScriptableObject
- [ ] Параметры стопки настроены

### Интеграция (опционально):
- [ ] TheoryToQuizIntegration добавлен
- [ ] Ссылки на менеджеры назначены
- [ ] QuizData назначен
- [ ] UI панели созданы и назначены

### UI элементы:
- [ ] QuizButton создан
- [ ] ProgressBar создан
- [ ] CardCounter создан
- [ ] Панели Theory/Quiz созданы

### Тестирование:
- [ ] Play Mode запускается без ошибок
- [ ] Карточки появляются в CardContainer
- [ ] Свайп влево/вправо работает
- [ ] Анимация плавная
- [ ] События срабатывают (проверить в Console)
- [ ] Кнопка квиза появляется после N карточек

---

## 🎓 Полезные горячие клавиши

```
F - Фокус на выбранном объекте
Ctrl + D - Дублировать объект
Ctrl + Shift + N - Создать пустой GameObject
Alt + Shift + A - Создать UI → Panel
```

---

## 📞 Помощь

Если что-то не работает:
1. Проверьте Console на ошибки (Ctrl+Shift+C)
2. Проверьте все ссылки в Inspector (не должно быть "None")
3. Убедитесь что DOTween импортирован
4. Проверьте что EventSystem присутствует в сцене
5. Откройте `THEORY_CARDS_SETUP.md` для детальной инструкции

---

**Готово!** Теперь можно тестировать систему в Play Mode! 🎉

