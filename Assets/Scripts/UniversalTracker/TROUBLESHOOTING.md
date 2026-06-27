# 🆘 Решение проблем Unity Tracker

## 🔥 Unity крашится при запуске

### Симптом:
```
✅ [WebCam] Камера запущена
✅ [BaseModel] Модель загружена
📊 [TrackerManager] Трекинг запущен!
[Unity крашится без ошибок]
```

### Причина:
Краш происходит при первом `TextureConverter.ToTensor()` - конвертация WebCamTexture в тензор.

### Решения:

#### 1. Уменьшите разрешение камеры
```
WebCamInputProvider:
├── Requested Width: 640  (было 1920)
├── Requested Height: 480 (было 1080)
└── Requested FPS: 15     (было 30)
```

#### 2. Уменьшите размер входа модели
```
ModelConfig:
└── Input Size: 320  (было 640)
```

#### 3. Смените Backend
```
ModelConfig:
└── Backend: GPUPixel  (если GPUCompute крашит Unity)
```

Попробуйте варианты:
- `GPUPixel` (на некоторых ПК самый стабильный)
- `CPU` (самый предсказуемый, но медленно)
- `GPUCompute` (быстрее, но у тебя крашит Unity)

#### 4. Попробуйте другую камеру
```
WebCamInputProvider:
└── Camera Index: 1  (попробуйте 0, 1, 2...)
```

#### 5. Включите Verbose Logging
```
UniversalTrackerManager:
└── Verbose Logging: ✅
```

Это покажет на каком именно шаге происходит краш.

---

## ⚠️ Unity зависает (не крашится, но не отвечает)

### Причина:
Слишком большая модель или медленный inference.

### Решение:
```
1. Используйте nano модель: yolo26n.onnx
2. Уменьшите FPS: Target FPS = 10
3. Уменьшите Input Size = 320
```

---

## 🎥 Камера не запускается

### Симптом:
```
❌ [WebCam] Веб-камеры не найдены!
```

### Решение:
1. Проверьте что камера подключена
2. Закройте другие приложения использующие камеру (Zoom, Skype)
3. Перезапустите Unity

---

## 🧠 Модель не загружается

### Симптом:
```
❌ [BaseModel] ModelAsset null!
```

### Решение:
1. Убедитесь что .onnx файл импортирован в Unity
2. В ModelConfig назначьте Model Asset
3. Проверьте что файл не поврежден

---

## 🔴 Слишком много ошибок

### Симптом:
```
🛑 [TrackerManager] ПРЕВЫШЕН ЛИМИТ ОШИБОК (10/10)!
```

### Решение:
```
UniversalTrackerManager:
└── Max Consecutive Errors: 50  (увеличьте)
```

---

## 🐌 Медленная работа (< 10 FPS)

### Решения:

#### Используйте nano модель
```
yolo26n.onnx вместо yolo26m.onnx
```

#### Уменьшите разрешение
```
ModelConfig:
└── Input Size: 320
```

#### Используйте GPU
```
ModelConfig:
└── Backend: GPUCompute
```

#### Оптимизируйте FPS
```
UniversalTrackerManager:
└── Target FPS: 15
```

---

## 📊 Нет детекций

### Причина:
Слишком высокий порог confidence.

### Решение:
```
ModelConfig:
├── Confidence Threshold: 0.25  (было 0.5)
└── NMS Threshold: 0.45
```

---

## 🎯 Рекомендованная конфигурация для стабильности

```yaml
WebCamInputProvider:
  Camera Index: 0
  Width: 640
  Height: 480
  FPS: 15

ModelConfig:
  Model: yolo26n.onnx
  Backend: GPUCompute (или CPU если крашит)
  Input Size: 320
  Confidence: 0.5
  NMS: 0.45

UniversalTrackerManager:
  Target FPS: 15
  Max Errors: 20
  Verbose Logging: true
```

---

## 🔧 Для разработчиков

### Дебаг краша TextureConverter:

```csharp
// Проверьте размеры перед конвертацией
Debug.Log($"Texture: {tex.width}x{tex.height}");
Debug.Log($"Tensor: {tensor.shape}");

// Попробуйте конвертацию вручную
try {
    TextureConverter.ToTensor(tex, tensor);
} catch (Exception e) {
    Debug.LogError($"Краш здесь: {e}");
}
```

### Проверка WebCamTexture:
```csharp
// Дождитесь стабилизации
if (framesSinceStart < 10) return;

// Проверьте валидность
if (tex.width <= 0 || tex.height <= 0) {
    Debug.LogError("Invalid texture!");
    return;
}
```

---

## 📞 Всё ещё крашится?

1. Включите `Verbose Logging: true`
2. Скопируйте последние 20 строк из Console
3. Проверьте на каком шаге происходит краш:
   - При создании тензора?
   - При TextureConverter?
   - При worker.Schedule()?
   - При ReadbackAndClone()?

---

**Большинство крашей решаются уменьшением разрешения камеры и размера модели!**
