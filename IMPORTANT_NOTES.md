# 🔔 IMPORTANT NOTES & NEXT STEPS

## ⚠️ Важные замечания

### 1. Google Sheets - требуемая структура

Убедитесь что Google Sheets содержат правильную структуру листов:

#### Лист 0: Localization (Локализация)
```
Первая строка (Header):
Key | ru | en | ar | bs | sq | tr | id

Данные:
WARMUP_TITLE | Разминка | Warmup | تدفئة | ... 
WARMUP_DESCRIPTION_KEY | Описание разминки | Warmup description | ...
...
```

#### Лист 1: Levels (Уровни)
```
Header:
LevelId | NameKey | DescriptionKey | Order | ImageBundleKey

Data:
Warmup | WARMUP_TITLE | WARMUP_DESCRIPTION_KEY | 0 | warmup_img
Miqat | MIQAT_TITLE | MIQAT_DESCRIPTION_KEY | 1 | miqat_img
...
```

#### Лист 2: Questions (Вопросы)
```
Header:
LevelId | QuestionKey | Option1Key | Option2Key | Option3Key | Option4Key | CorrectIndex | ExplanationKey | GemsReward

Data:
Warmup | Q_WARMUP_1 | OPT_1A | OPT_1B | OPT_1C | OPT_1D | 0 | EXP_WARMUP_1 | 5
Warmup | Q_WARMUP_2 | OPT_2A | OPT_2B | OPT_2C | OPT_2D | 2 | EXP_WARMUP_2 | 5
...
```

#### Лист 3: Theory (Теория)
```
Header:
LevelId | Order | TitleKey | TextKey | ImageBundleKey

Data:
Warmup | 0 | THEORY_TITLE_1 | THEORY_TEXT_1 | theory_img_1
Warmup | 1 | THEORY_TITLE_2 | THEORY_TEXT_2 | theory_img_2
...
```

### 2. Google Sheets - экспорт в CSV

⚠️ **ВАЖНО:** Используйте этот формат URL для экспорта:

```
https://docs.google.com/spreadsheets/d/e/YOUR_SHEET_ID/pub?gid=SHEET_ID&single=true&output=csv
```

**Где:**
- `YOUR_SHEET_ID` - ID вашей таблицы
- `SHEET_ID` - ID листа в таблице (0, 1, 2, 3, ...)
- `output=csv` - обязательный параметр

### 3. PlayerPrefs - ограничения

⚠️ **ВАЖНО:** PlayerPrefs имеет ограничения:

| Платформа | Лимит |
|-----------|-------|
| Windows/Mac | Нет ограничений |
| Android | ~1MB |
| iOS | ~1MB |
| WebGL | Нет (cookie-based) |

**Если контент > 1MB:**
- Используйте FileSystem для кэша
- Разделите на несколько файлов
- Реализуйте compression

### 4. Интернет - проверка соединения

ContentLoaderService использует `Application.internetReachability`:

```csharp
// Но это не 100% гарантирует доступ в интернет!
// Может быть Wi-Fi без доступа в интернет
// Рекомендуется добавить дополнительную проверку:

IEnumerator CheckRealInternetConnection()
{
    using (UnityWebRequest request = 
        UnityWebRequest.Head("https://www.google.com"))
    {
        yield return request.SendWebRequest();
        Debug.Log(request.result == UnityWebRequest.Result.Success 
            ? "Internet OK" 
            : "No Internet");
    }
}
```

---

## 🔧 Конфигурация перед запуском

### 1. Проверьте URL'ы Google Sheets

В `ContentLoaderService.cs` классе `GoogleSheetsUrls`:

```csharp
private static class GoogleSheetsUrls
{
    public const string Localization = 
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vTX5Wh2iYEJWMZNxQqDw0rroPUyiGnJglnAG2WdxfVkj3kYEGHF27bYV6roA6mMpLS-_247HpV7K7JS/pub?gid=0&single=true&output=csv";
    // ... остальные
}
```

**Убедитесь что:**
- ✅ URL'ы указаны правильно
- ✅ Листы опубликованы (Published to the web)
- ✅ Доступ общедоступный или вы авторизованы

### 2. Проверьте LocalizationService

Убедитесь что `LocalizationService` работает с вашими языками:

```csharp
private static readonly Dictionary<string, Language> ColumnToLanguage
    = new Dictionary<string, Language>
    {
        { "ru", Language.Russian },
        { "bs", Language.Bosnian },
        { "sq", Language.Albanian },
        { "tr", Language.Turkish },
        { "ar", Language.Arabic },
        { "id", Language.Indonesian },
        { "en", Language.English }
    };
```

Если нужны другие языки, добавьте их сюда!

### 3. Перед запуском игры

1. ✅ Проверьте сетевое соединение
2. ✅ Убедитесь что Google Sheets публичны
3. ✅ Проверьте формат CSV (нет конфликтов запятых)
4. ✅ Ключи локализации совпадают в разных листах

---

## 🚨 Обработка ошибок

### Сценарий: Нет интернета

```
ContentLoaderService автоматически:
1. Проверяет интернет
2. Делает 3 попытки загрузки (с задержками)
3. Падает на кэш из PlayerPrefs
4. Если кэш пуст → пустые коллекции + WARNING в логе
```

### Сценарий: Google Sheets недоступны

```
ContentLoaderService:
1. UnityWebRequest получит 403/404
2. Запишет ERROR в логе
3. Перейдёт на fallback кэш
4. Приложение продолжит работу
```

### Сценарий: CSV парсинг ошибка

```
ContentLoaderService:
1. Пропустит строку с ошибкой
2. Запишет WARNING с номером строки
3. Продолжит парсинг остальных строк
4. Данные частично загружены
```

---

## 📱 Platform-specific notes

### Windows / Mac / Linux
- ✅ Все работает как ожидается
- ✅ PlayerPrefs хранит в Registry/~/Library
- ✅ Размер кэша не ограничен

### Android
- ⚠️ PlayerPrefs ~1MB лимит
- ⚠️ Могут быть проблемы с сетью на некоторых устройствах
- ✅ Рекомендуется использовать FileSystem для большого контента

### iOS
- ⚠️ PlayerPrefs через NSUserDefaults (~1MB)
- ⚠️ Могут быть проблемы с TLS 1.2+
- ✅ Используйте HTTPS для Google Sheets

### WebGL
- ⚠️ PlayerPrefs работает через IndexedDB
- ⚠️ CORS ограничения при запросе к Google Sheets
- ⚠️ Может потребоваться proxy сервер

---

## 🔐 Security considerations

### 1. Google Sheets публичность

⚠️ Убедитесь:
- ✅ Лист "Published to the web" только для чтения
- ✅ Нет чувствительных данных в Google Sheets
- ✅ URL'ы не содержат приватную информацию

### 2. PlayerPrefs сохранение

⚠️ PlayerPrefs - не шифруется:
- ✅ Не сохраняйте пароли
- ✅ Не сохраняйте токены
- ✅ Только публичный контент

### 3. HTTPS

✅ Google Sheets использует HTTPS  
✅ UnityWebRequest требует сертификат

---

## 🐛 Debug tips

### Проверить что загружено

```csharp
var loader = GetComponent<ContentLoaderService>();

// Сколько уровней?
Debug.Log(loader.GetAllLevels().Count);

// Сколько вопросов в уровне?
Debug.Log(loader.GetQuestionsForLevel("Warmup").Count);

// Переведено ли ключ?
Debug.Log(loader.GetLocalizedText("WARMUP_TITLE", "ru"));

// Есть ли кэш?
Debug.Log(PlayerPrefs.HasKey("Content_Localization"));
```

### Очистить кэш и перезагрузить

```csharp
// Unity Editor Console
PlayerPrefs.DeleteKey("Content_Localization");
PlayerPrefs.DeleteKey("Content_Levels");
PlayerPrefs.DeleteKey("Content_Questions");
PlayerPrefs.DeleteKey("Content_Theory");
PlayerPrefs.Save();

// Или программно
loader.ClearCache();
StartCoroutine(loader.LoadAllContent());
```

### Смотреть логи загрузки

```
[ContentLoaderService] Starting content load...
[ContentLoaderService] Localization loaded: 150 keys
[ContentLoaderService] Levels loaded: 5 levels
[ContentLoaderService] Questions loaded: 35 questions
[ContentLoaderService] Theory loaded: 12 cards
[ContentLoaderService] Data cached successfully
[ContentLoaderService] Content loading completed!
```

---

## 🎯 Часто задаваемые вопросы

### Q: Как изменить URL Google Sheets?
A: Отредактируйте класс `GoogleSheetsUrls` в ContentLoaderService.cs

### Q: Как добавить новый язык?
A: Добавьте строку в `ColumnToLanguage` словарь в LocalizationService

### Q: Как добавить новый лист в Google Sheets?
A: Создайте лист, экспортируйте, обновите URL в `GoogleSheetsUrls`

### Q: Что если контент > 1MB?
A: Используйте FileSystem вместо PlayerPrefs (архитектура позволяет)

### Q: Как заставить перезагрузить контент?
A: Вызовите `loader.ClearCache()` затем `StartCoroutine(loader.LoadAllContent())`

### Q: Работает ли offline?
A: Да, fallback на PlayerPrefs кэш автоматический

---

## 📚 References

- **Google Sheets Export:** https://support.google.com/docs/answer/183965
- **Unity PlayerPrefs:** https://docs.unity3d.com/ScriptReference/PlayerPrefs.html
- **UnityWebRequest:** https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html
- **CSV Parsing:** RFC 4180

---

## ✅ Финальный чек-лист перед production

- [ ] Google Sheets структура проверена
- [ ] URL'ы правильные
- [ ] LocalizationService подключен
- [ ] Интернет соединение тестировано
- [ ] Offline режим тестирован
- [ ] PlayerPrefs размер в пределах лимита
- [ ] Логирование включено для отладки
- [ ] Cache очищен перед первым запуском
- [ ] Все ключи локализации согласованы
- [ ] Платформа-специфичные настройки проверены

---

## 🎓 Дополнительная помощь

- 📖 Смотрите `CONTENT_LOADER_SETUP.md` для архитектуры
- 🚀 Смотрите `QUICKSTART_CONTENTLOADER.md` для быстрого старта
- 📋 Смотрите `STAGE2_COMPLETION_CHECKLIST.md` для полного чек-листа
- 💡 Смотрите `ContentLoaderExample.cs` для примеров кода

---

**Версия:** 1.0  
**Дата:** 2026-08-12  
**Статус:** ✅ ГОТОВО К PRODUCTION

**Удачи! 🚀**

