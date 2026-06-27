# ✅ Чеклист перед запуском Unity Tracker

## 🔴 КРИТИЧЕСКИ ВАЖНО (иначе КРАШ!)

### 1️⃣ **ModelConfig ScriptableObject**

**Создай** в Assets:
```
Правый клик → Create → Universal Tracker → Model Config
```

**Настрой:**
```
Model Name: yolo26n
Model Asset: [перетащи yolo26n.onnx]
Backend: CPU  ← ВАЖНО! GPU может крашить
Input Size: 320
Confidence: 0.5
NMS: 0.45
```

**Привяжи** к Main Camera → Universal Tracker Manager:
```
Model Configs:
  Size = 1
  Element 0: [твой ModelConfig]
```

---

### 2️⃣ **WebCamInputProvider настройки**

**Main Camera → Web Cam Input Provider:**
```
⚠️ ИЗМЕНИ ЭТИ ЗНАЧЕНИЯ ВРУЧНУЮ В INSPECTOR:

Camera Index: 0
Requested Width: 320   ← БЫЛО 1920, ОБЯЗАТЕЛЬНО ИЗМЕНИ!
Requested Height: 240  ← БЫЛО 1080, ОБЯЗАТЕЛЬНО ИЗМЕНИ!
Requested FPS: 30
```

**ПОЧЕМУ:** Большие размеры (1920x1080) крашат GPU!

---

### 3️⃣ **Canvas + RawImage**

**Создай UI:**
```
Hierarchy → UI → Canvas
Canvas → UI → Raw Image
```

**Привяжи к RawImage:**
```
Add Component → UI Visualization Receiver

UI Visualization Receiver:
├── Display Image: [сам RawImage]
├── Overlay Canvas: [Canvas]
└── Is Enabled: ✅
```

**Привяжи к Manager:**
```
Main Camera → Universal Tracker Manager:

Manual UI Receiver: [RawImage → UI Visualization Receiver]
Manual Debug Receiver: [RawImage → Debug Output Receiver]
```

---

### 4️⃣ **Безопасные настройки Manager**

**Main Camera → Universal Tracker Manager:**
```
Target FPS: 15
Max Consecutive Errors: 50
Verbose Logging: ✅

Input Type: Web Cam
Custom Input Provider: [Main Camera → Web Cam Input Provider]
```

---

## 🎯 Минимальная конфигурация для ПЕРВОГО запуска

**Если всё крашит - попробуй ЭТО:**

### WebCamInputProvider:
```
Width: 160   ← МИНИМУМ!
Height: 120  ← МИНИМУМ!
```

### ModelConfig:
```
Backend: CPU  ← ОБЯЗАТЕЛЬНО CPU!
Input Size: 160  ← МИНИМУМ!
```

### UniversalTrackerManager:
```
Target FPS: 5  ← ОЧЕНЬ МЕДЛЕННО
```

**Если ДАЖЕ ЭТО крашит** → проблема в модели или драйверах!

---

## 🚀 Порядок запуска

1. ✅ Убедись что ВСЕ настройки выше сделаны
2. ✅ Проверь что ModelConfig привязан
3. ✅ Проверь что модель (.onnx) импортирована в Unity
4. ✅ **ИЗМЕНИ Width/Height в Inspector вручную!**
5. ▶️ Запусти игру
6. 📋 Смотри Console

---

## 📊 Что должно быть в Console

### ✅ Успешный запуск:
```
═══════════════════════════════════
🚀 [TrackerManager] НАЧАЛО ЗАПУСКА
📍 ШАГ 1/4: Инициализация Input Provider...
📷 [WebCam] Найдено камер: 2
✅ [WebCam] Камера запущена
   RenderTexture создан: 320x240  ← МАЛЕНЬКИЙ РАЗМЕР!
✅ ШАГ 1/4 ЗАВЕРШЁН
📍 ШАГ 2/4: Инициализация моделей...
✅ [BaseModel] Модель yolo26n загружена (Backend: CPU)
✅ ШАГ 2/4 ЗАВЕРШЁН
...
✅ ТРЕКИНГ УСПЕШНО ЗАПУЩЕН!

🎥 [WebCam] UpdateTexture #1: камера 640x360
   >>> Вызов Graphics.Blit() <<<
   >>> Graphics.Blit() завершился! <<<
   ✅ Скопировано → RT 320x240  ← МАЛЕНЬКИЙ!

🎬 [TrackerManager] ═══ КАДР #1 ═══
   ✓ Текстура получена: 320x240
   🧠 Запуск inference...
   ✅ Inference завершён!
```

### ❌ Если краш:
Последняя строка покажет ГДЕ:
```
>>> Вызов Graphics.Blit() <<<
[КРАШ] ← Проблема в Graphics.Blit

или

🧠 Запуск inference...
[КРАШ] ← Проблема в модели/inference
```

---

## 🆘 Всё ещё крашит?

### Попробуй БЕЗ модели:

**Временно отключи inference:**

В `UniversalTrackerManager.cs` закомментируй InitializeModels:
```csharp
// InitializeModels(); // ← ВРЕМЕННО ОТКЛЮЧИ
```

Это покажет камеру БЕЗ inference - если работает, значит проблема в модели!

---

## 🔧 Частые проблемы

### "RT 1920x1080" в логах
→ **НЕ ИЗМЕНИЛ Width/Height в Inspector!** Измени вручную!

### "Backend: GPUCompute" в логах
→ **НЕ ИЗМЕНИЛ Backend на CPU!** Пересоздай ModelConfig с CPU!

### "ModelConfig null"
→ **НЕ ПРИВЯЗАЛ ModelConfig к Manager!** Перетащи в Model Configs!

### Камера не запускается
→ Закрой другие приложения (Zoom, Skype)

---

**Попробуй СЕЙЧАС с этими настройками!** 🚀
