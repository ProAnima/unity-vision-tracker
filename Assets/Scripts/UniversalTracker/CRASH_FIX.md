# 🔧 Исправление краша Unity при запуске трекера

## ✅ Что исправлено:

### 1️⃣ **Проблема с WebCamTexture**
**Было:** Unity.InferenceEngine не мог напрямую конвертировать `WebCamTexture` → `Tensor`

**Стало:** Добавлен промежуточный `RenderTexture`:
```
WebCamTexture → RenderTexture → Tensor
```

### 2️⃣ **Снижена нагрузка по умолчанию**
- **WebCamInputProvider**:
  - Requested Width: `640` (было 1920)
  - Requested Height: `480` (было 1080)
  
- **ModelConfig**:
  - Input Size: `320` (было 640)

### 3️⃣ **Улучшена стабильность**
- Автоматическая очистка RenderTexture при Release
- Проверки валидности текстур перед inference
- Более детальное логирование

---

## 🎯 Рекомендованные настройки для первого запуска:

### Main Camera → Web Cam Input Provider:
```
Camera Index: 0
Requested Width: 640
Requested Height: 480
Requested FPS: 30
```

### Model Config (ScriptableObject):
```
Model Name: yolo26n
Model Asset: [yolo26n.onnx]
Backend: GPUCompute
Input Size: 320
Confidence Threshold: 0.5
NMS Threshold: 0.45
```

### Main Camera → Universal Tracker Manager:
```
Target FPS: 15
Max Consecutive Errors: 20
Verbose Logging: ✅ (для отладки)
```

---

## 🚀 Порядок запуска:

1. **Проверь настройки** выше
2. **Запусти игру**
3. **Смотри Console** - должны появиться логи:
   ```
   ✅ [WebCam] Камера запущена
   RenderTexture для inference: 640x480
   🎥 [WebCam] Кадр #1: 1280x720 → RT 640x480
   📊 [BaseModel] Создаём тензор [1, 3, 320, 320]...
   ✅ [BaseModel] Конвертация успешна
   ```

4. **RawImage** должен показать изображение с камеры

---

## ⚠️ Если ВСЁ ЕЩЁ крашится:

### Попробуй CPU Backend:
```
ModelConfig → Backend: CPU
```

### Уменьши разрешение ещё больше:
```
WebCamInputProvider:
├── Width: 320
└── Height: 240

ModelConfig:
└── Input Size: 160
```

### Попробуй другую модель:
Если `yolo26n.onnx` крашит, попробуй `yolo11n.onnx` (старее, но стабильнее)

---

## 📊 Что изменилось в коде:

### WebCamInputProvider.cs
- Добавлен `RenderTexture renderTexture`
- `CurrentTexture` возвращает `renderTexture` вместо `webCamTexture`
- `UpdateTexture()` копирует кадры через `Graphics.Blit()`
- `Release()` освобождает оба ресурса

### IInferenceModel.cs
- Дефолтный `inputSize = 320` (вместо 640)

---

## 🎬 Ожидаемый результат:

После исправлений Unity **НЕ ДОЛЖЕН крашится**, а должен:
1. ✅ Инициализировать камеру
2. ✅ Создать RenderTexture
3. ✅ Загрузить модель
4. ✅ Запустить inference без крашей
5. ✅ Показать видео на RawImage

---

## 🔍 Дебаг в реальном времени:

Включи `Verbose Logging` и смотри где останавливается:

```
🎥 [WebCam] Кадр #4  ← Камера работает
📊 [BaseModel] Создаём тензор  ← Модель готовится
🔄 [BaseModel] Конвертация...  ← КРИТИЧЕСКИЙ МОМЕНТ
✅ [BaseModel] Конвертация успешна  ← Если видишь это - всё ОК!
```

Если **крашнулось между** "Конвертация..." и "успешна" → проблема в `TextureConverter.ToTensor()` или драйверах GPU.

**Решение:** Попробуй CPU backend!

---

**Попробуй запустить СЕЙЧАС и покажи логи!** 🚀
