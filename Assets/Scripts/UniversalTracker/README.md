# 🎯 Universal Tracker System

Универсальная система трекинга для Unity с поддержкой различных нейросетевых моделей через Unity Sentis (InferenceEngine).

## 📦 Возможности

### 🧠 Поддерживаемые модели
- **YOLO26 Detection** ⭐ - новейшая версия (NMS-free, edge-optimized)
- **YOLO26 Pose** ⭐ - определение скелета с минимальной задержкой
- **YOLO11 Detection** - детекция объектов и людей (стабильная)
- **YOLO11 Pose** - определение скелета человека (17 keypoints)
- **YOLO12** - экспериментальная версия с attention механизмами
- **Segmentation** - сегментация объектов (YOLO-seg, FastSAM)
- **OBB** - ориентированные bounding boxes
- **Classification** - классификация объектов

**🚀 НОВИНКА**: Автоматическое определение версии модели! Просто назовите файл `yolo26n.onnx` - система сама выберет правильный парсер.

### 📥 Input Providers (источники)
- **WebCamInputProvider** - веб-камера
- **CameraInputProvider** - Unity Camera через RenderTexture
- **TextureInputProvider** - готовые текстуры
- **VideoInputProvider** - VideoPlayer
- Поддержка кастомных провайдеров

### 📤 Output Receivers (приёмники)
- **EventOutputReceiver** - UnityEvents для интеграции с другими системами
- **UIVisualizationReceiver** - отрисовка на UI Canvas (RawImage)
- **SceneVisualizationReceiver** - 3D маркеры в сцене
- **DebugOutputReceiver** - отладочная информация

### 🔄 Tracking (трекинг)
- **IOU Tracker** - простой трекер по Intersection over Union
- **SORT Tracker** - продвинутый трекер с предсказанием траектории
- Сохранение ID объектов между кадрами
- Фильтрация ложных срабатываний

## 🚀 Быстрый старт

### 1. Базовая настройка

```csharp
// Создайте GameObject и добавьте UniversalTrackerManager
var tracker = gameObject.AddComponent<UniversalTrackerManager>();

// Настройте через Inspector:
// - Input Type: WebCam / Camera / Texture / Video
// - Model Configs: назначьте .onnx модели
// - Output Receivers: включите нужные
```

### 2. Конфигурация через ScriptableObject

```csharp
// Создайте конфигурацию: Assets → Create → Universal Tracker → Tracker Config
// Назначьте её в TrackerManager
```

### 3. Подключение к событиям

```csharp
var eventReceiver = GetComponent<EventOutputReceiver>();

eventReceiver.OnDetectionsReceived.AddListener((detections) => 
{
    foreach (var box in detections)
    {
        Debug.Log($"Обнаружен: {box.className} (confidence: {box.confidence:F2})");
    }
});

eventReceiver.OnKeypointsReceived.AddListener((keypoints) =>
{
    Debug.Log($"Найдено скелетов: {keypoints.Length}");
});
```

## 📊 Архитектура

```
UniversalTrackerManager
    ├── IInputProvider (WebCam/Camera/Texture/Video)
    ├── IInferenceModel (YOLO11/YOLO12/Pose/Seg)
    ├── ITracker (IOU/SORT)
    └── IOutputReceiver[] (Events/UI/Scene/Debug)
```

## 🎨 Примеры использования

### Детекция людей с трекингом

```csharp
public class PersonCounter : MonoBehaviour
{
    void Start()
    {
        var tracker = GetComponent<UniversalTrackerManager>();
        tracker.useTracking = true;
        tracker.trackerType = TrackerType.SORT;
        tracker.StartTracking();
    }
    
    void Update()
    {
        var tracked = GetComponent<UniversalTrackerManager>().GetTrackedObjects();
        Debug.Log($"Людей в кадре: {tracked.Length}");
    }
}
```

### Pose Estimation для анимации

```csharp
public class SkeletonRetargeting : MonoBehaviour
{
    public Transform[] bones;
    
    void OnEnable()
    {
        var eventReceiver = GetComponent<EventOutputReceiver>();
        eventReceiver.OnKeypointsReceived.AddListener(UpdateSkeleton);
    }
    
    void UpdateSkeleton(KeypointData[] keypoints)
    {
        if (keypoints.Length == 0) return;
        
        var kp = keypoints[0];
        for (int i = 0; i < Mathf.Min(bones.Length, kp.points.Length); i++)
        {
            if (kp.confidences[i] > 0.5f)
            {
                // Преобразуем 2D координаты в 3D позицию
                bones[i].position = ProjectTo3D(kp.points[i]);
            }
        }
    }
}
```

### Множественные модели

```csharp
// Переключение между моделями в runtime
trackerManager.SwitchModel(0); // Detection
// ... обработка
trackerManager.SwitchModel(1); // Pose
// ... обработка
```

## ⚙️ Оптимизация

### Производительность
```csharp
// Уменьшите FPS для слабых устройств
tracker.targetFPS = 15;

// Используйте меньшие модели (nano, small)
modelConfig.inputSize = 320; // вместо 640

// Отключите визуализацию в продакшне
tracker.useUIVisualization = false;
```

### Точность
```csharp
// Настройте пороги
modelConfig.confidenceThreshold = 0.7f; // выше = меньше ложных срабатываний
modelConfig.nmsThreshold = 0.45f; // NMS агрессивность
```

## 🔧 Требования

- Unity 2021.3+
- Unity Sentis 2.0+
- Модели в формате .onnx (YOLO11/12 экспортированные)

## 📝 Экспорт моделей

```python
# Экспорт YOLO11 в ONNX
from ultralytics import YOLO

model = YOLO('yolo11n.pt')  # nano, s, m, l, x
model.export(format='onnx', opset=13, simplify=True)

# Для pose
model = YOLO('yolo11n-pose.pt')
model.export(format='onnx', opset=13, simplify=True)
```

## 🐛 Отладка

```csharp
// Включите Debug Output
tracker.useDebugOutput = true;

// Проверьте последний результат
var result = tracker.LastResult;
Debug.Log($"Inference time: {result.inferenceTime}ms");
Debug.Log($"Detections: {result.detectionCount}");

// FPS
Debug.Log($"Current FPS: {tracker.CurrentFPS}");
```

## 📚 Документация по моделям

- [YOLO11 Docs](https://docs.ultralytics.com/models/yolo11/)
- [YOLO12 Docs](https://docs.ultralytics.com/models/yolo12/)
- [Unity Sentis](https://docs.unity3d.com/Packages/com.unity.sentis@latest/)

## 🎯 Рекомендуемые модели

| Задача | Модель | Производительность | Точность | Рекомендация |
|--------|--------|-------------------|----------|--------------|
| Детекция (mobile/edge) | **YOLO26n** ⭐ | ⚡⚡⚡⚡ | ⭐⭐⭐ | **Лучший выбор!** |
| Детекция (desktop) | **YOLO26m** ⭐ | ⚡⚡⚡ | ⭐⭐⭐⭐ | **+43% скорость** |
| Детекция (стабильная) | YOLO11n | ⚡⚡⚡ | ⭐⭐⭐ | Проверенная |
| Pose (edge) | **YOLO26n-pose** ⭐ | ⚡⚡⚡⚡ | ⭐⭐⭐⭐ | **NMS-free** |
| Pose (стабильная) | YOLO11n-pose | ⚡⚡⚡ | ⭐⭐⭐ | Надежная |
| Pose (mobile) | BlazePose | ⚡⚡⚡ | ⭐⭐⭐ | Самый быстрый |
| Сегментация | FastSAM | ⚡⚡ | ⭐⭐⭐⭐ | Universal |

**🆕 YOLO26**: До +43% производительность на CPU! NMS-free inference!

---

Made with ❤️ for Universal Tracking
